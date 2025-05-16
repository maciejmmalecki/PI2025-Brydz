using Mirror;
using UnityEngine;
using System.Collections;

public class MyNetworkManager : NetworkManager
{
    public override void Awake()
    {
        base.Awake();
        maxConnections = 5;
    }
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
    }
} 