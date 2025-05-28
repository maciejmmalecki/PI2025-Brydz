using Mirror;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Niestandardowy NetworkManager dla Mirror.
/// Ustawia maksymalną liczbę graczy i wywołuje zdarzenia połączenia.
/// </summary>
public class MyNetworkManager : NetworkManager
{
    private MultiplayerGameManager gameManager;

    public override void Awake()
    {
        base.Awake();
        maxConnections = 5;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameManager = FindObjectOfType<MultiplayerGameManager>();
        Debug.Log("✅ [SERVER] MultiplayerGameManager znaleziony w OnStartServer");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log($"[SERVER] Dodawanie gracza: connId={conn.connectionId}");
        base.OnServerAddPlayer(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.LogWarning("🔥🔥🔥 [SERVER] OnServerDisconnect WYWOŁANE 🔥🔥🔥");

        if (MultiplayerGameManager.Instance != null)
        {
            Debug.Log("[SERVER] Wywołuję OnPlayerDisconnected w MultiplayerGameManager");
            MultiplayerGameManager.Instance.OnPlayerDisconnected(conn);
        }
        else
        {
            Debug.LogWarning("⚠️ MultiplayerGameManager.Instance == null w OnServerDisconnect");
        }

        base.OnServerDisconnect(conn);
    }
}
