using Mirror;
using UnityEngine;
using System.Collections;

/// <summary>
/// Niestandardowy NetworkManager dla Mirror.
/// Ustawia maksymalną liczbę graczy i wywołuje zdarzenia połączenia.
/// </summary>
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