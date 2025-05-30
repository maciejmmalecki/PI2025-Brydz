// kcp client logic abstracted into a class.
// for use in Mirror, DOTSNET, testing, etc.
using System;
using System.Net;
using System.Net.Sockets;

namespace kcp2k
{
    public class KcpClient
    {
        // kcp
        // public so that bandwidth statistics can be accessed from the outside
        public KcpPeer peer;

        // IO
        protected Socket socket;
        public EndPoint remoteEndPoint;

        // raw receive buffer always needs to be of 'MTU' size, even if
        // MaxMessageSize is larger. kcp always sends in MTU segments and having
        // a buffer smaller than MTU would silently drop excess data.
        // => we need the MTU to fit channel + message!
        // => protected because someone may overwrite RawReceive but still wants
        //    to reuse the buffer.
        protected readonly byte[] rawReceiveBuffer = new byte[Kcp.MTU_DEF];

        // callbacks
        // even for errors, to allow liraries to show popups etc.
        // instead of logging directly.
        // (string instead of Exception for ease of use and to avoid user panic)
        //
        // events are readonly, set in constructor.
        // this ensures they are always initialized when used.
        // fixes https://github.com/MirrorNetworking/Mirror/issues/3337 and more
        readonly Action OnConnected;
        readonly Action<ArraySegment<byte>, KcpChannel> OnData;
        readonly Action OnDisconnected;
        readonly Action<ErrorCode, string> OnError;

        // state
        public bool connected;

        public KcpClient(Action OnConnected,
                         Action<ArraySegment<byte>, KcpChannel> OnData,
                         Action OnDisconnected,
                         Action<ErrorCode, string> OnError)
        {
            // initialize callbacks first to ensure they can be used safely.
            this.OnConnected = OnConnected;
            this.OnData = OnData;
            this.OnDisconnected = OnDisconnected;
            this.OnError = OnError;
        }

        public void Connect(string address, ushort port, KcpConfig config)
        {
            if (connected)
            {
                Log.Warning("KcpClient: already connected!");
                return;
            }

            // resolve host name before creating peer.
            // fixes: https://github.com/MirrorNetworking/Mirror/issues/3361
            if (!Common.ResolveHostname(address, out IPAddress[] addresses))
            {
                // pass error to user callback. no need to log it manually.
                OnError(ErrorCode.DnsResolve, $"Failed to resolve host: {address}");
                OnDisconnected();
                return;
            }

            // create fresh peer for each new session
            peer = new KcpPeer(RawSend, OnAuthenticatedWrap, OnData, OnDisconnectedWrap, OnError, config);

            // some callbacks need to wrapped with some extra logic
            void OnAuthenticatedWrap()
            {
                Log.Info($"KcpClient: OnConnected");
                connected = true;
                OnConnected();
            }
            void OnDisconnectedWrap()
            {
                Log.Info($"KcpClient: OnDisconnected");
                connected = false;
                peer = null;
                socket?.Close();
                socket = null;
                remoteEndPoint = null;
                OnDisconnected();
            }

            Log.Info($"KcpClient: connect to {address}:{port}");

            // create socket
            remoteEndPoint = new IPEndPoint(addresses[0], port);
            socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // recv & send are called from main thread.
            // need to ensure this never blocks.
            // even a 1ms block per connection would stop us from scaling.
            socket.Blocking = false;

            // configure buffer sizes
            Common.ConfigureSocketBuffers(socket, config.RecvBufferSize, config.SendBufferSize);

            // bind to endpoint so we can use send/recv instead of sendto/recvfrom.
            socket.Connect(remoteEndPoint);

            // client should send handshake to server as very first message
            peer.SendHandshake();
        }

        // io - input.
        // virtual so it may be modified for relays, etc.
        // call this while it returns true, to process all messages this tick.
        // returned ArraySegment is valid until next call to RawReceive.
        protected virtual bool RawReceive(out ArraySegment<byte> segment)
        {
            segment = default;
            if (socket == null) return false;

            try
            {
                // ReceiveFrom allocates. we used bound Receive.
                // returns amount of bytes written into buffer.
                // throws SocketException if datagram was larger than buffer.
                // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receive?view=net-6.0
                int msgLength = socket.Receive(rawReceiveBuffer);

                //Log.Debug($"KCP: client raw recv {msgLength} bytes = {BitConverter.ToString(buffer, 0, msgLength)}");
                segment = new ArraySegment<byte>(rawReceiveBuffer, 0, msgLength);
                return true;
            }
            // for non-blocking sockets, Receive throws WouldBlock if there is
            // no message to read. that's okay. only log for other errors.
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    // the other end closing the connection is not an 'error'.
                    // but connections should never just end silently.
                    // at least log a message for easier debugging.
                    // for example, his can happen when connecting without a server.
                    // see test: ConnectWithoutServer().
                    Log.Info($"KcpClient: looks like the other end has closed the connection. This is fine: {e}");
                    peer.Disconnect();
                }
                // WouldBlock indicates there's no data yet, so return false.
                return false;
            }
        }

        // io - output.
        // virtual so it may be modified for relays, etc.
        protected virtual void RawSend(ArraySegment<byte> data)
        {
            try
            {
                socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
            }
            // for non-blocking sockets, SendTo may throw WouldBlock.
            // in that case, simply drop the message. it's UDP, it's fine.
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    Log.Error($"KcpClient: Send failed: {e}");
                }
            }
        }

        public void Send(ArraySegment<byte> segment, KcpChannel channel)
        {
            if (!connected)
            {
                Log.Warning("KcpClient: can't send because not connected!");
                return;
            }

            peer.SendData(segment, channel);
        }

        public void Disconnect()
        {
            // only if connected
            // otherwise we end up in a deadlock because of an open Mirror bug:
            // https://github.com/vis2k/Mirror/issues/2353
            if (!connected) return;

            // call Disconnect and let the connection handle it.
            // DO NOT set it to null yet. it needs to be updated a few more
            // times first. let the connection handle it!
            peer?.Disconnect();
        }

        // process incoming messages. should be called before updating the world.
        public void TickIncoming()
        {
            // recv on socket first, then process incoming
            // (even if we didn't receive anything. need to tick ping etc.)
            // (connection is null if not active)
            if (peer != null)
            {

                while (RawReceive(out ArraySegment<byte> segment))
                    peer.RawInput(segment);
            }

            // RawReceive may have disconnected peer. null check again.
            peer?.TickIncoming();
        }

        // process outgoing messages. should be called after updating the world.
        public void TickOutgoing()
        {
            // process outgoing
            // (connection is null if not active)
            peer?.TickOutgoing();
        }

        // process incoming and outgoing for convenience
        // => ideally call ProcessIncoming() before updating the world and
        //    ProcessOutgoing() after updating the world for minimum latency
        public void Tick()
        {
            TickIncoming();
            TickOutgoing();
        }
    }
}
