using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Klasa zarządzająca stanem gracza w sieci (Mirror).
/// Zajmuje się wysyłaniem komend (Cmd), odbiorem danych i synchronizacją UI.
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex;
    public MultiplayerHandDisplay handDisplay;
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (connectionToClient == null)
        {
            Debug.Log("Pomijam serwerowego NetworkPlayer (ghost, connId: 0)");
            return;
        }
        var manager = MultiplayerGameManager.Instance;
        if (manager == null){ 
            Debug.LogError("MultiplayerGameManager.Instance == null");
            return;
        }
        if (!MultiplayerGameManager.Instance.players.Contains(this))
        {
            MultiplayerGameManager.Instance.players.Add(this);
            playerIndex = MultiplayerGameManager.Instance.players.Count - 1;
            Debug.Log($"Dodano gracza {playerIndex} (connId: {connectionToClient?.connectionId ?? -1})");
        }
        handDisplay = FindObjectOfType<MultiplayerHandDisplay>();
        //Debug.Log($"NetworkPlayer.StartClient() >> isLocalPlayer: {isLocalPlayer}, connectionId: {connectionToClient?.connectionId ?? -1}");
        if (manager.players.Count == 4)
        {
            Debug.Log("4 gracz dołączony — start gry!");
            for (int i = 0; i < manager.players.Count; i++)
            {
                manager.players[i].playerIndex = i;
            }
            manager.StartCoroutine(DelayedStart(manager));
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!isLocalPlayer) return;

        GameObject panel = GameObject.Find("PlayerHand");
        if (panel != null)
        {
            handDisplay = panel.GetComponent<MultiplayerHandDisplay>();
            Debug.Log("Przypisano handDisplay na kliencie");
        }
        else
        {
            Debug.LogWarning("Nie znaleziono PlayerHand w OnStartClient");
        }
        Debug.Log($"[CLIENT] playerIndex={playerIndex},winningBidderIndex={MultiplayerGameManager.Instance.winningBidderIndex}");
    }

    private IEnumerator DelayedStart(MultiplayerGameManager manager)
    {
        yield return new WaitForSeconds(0.5f);
        manager.DealCards();
        manager.StartCoroutine(manager.StartBidding());
    }

    [Command]
    public void CmdPlayCard(string cardID)
    {
        MultiplayerGameManager.Instance.CmdPlayCard(cardID, connectionToClient);
    }

    [Command]
    public void CmdSubmitBid(BiddingData bid)
    {
        MultiplayerGameManager.Instance.ReceiveBidFromPlayer(bid, this);
    }
    [TargetRpc]
    public void TargetStartBidding(NetworkConnection target, string currentHighestBid)
    {
        MultiplayerBiddingUI ui = FindObjectOfType<MultiplayerBiddingUI>();
        if (ui != null)
        {
            ui.ShowBiddingUI(true,currentHighestBid);
            ui.UpdateCurrentBidText("Aktualna: " + (string.IsNullOrEmpty(currentHighestBid) ? "brak" : currentHighestBid));
        }
    }
    [Command]
    public void CmdRestartMatch()
    {
        MultiplayerGameManager.Instance.ServerRestartMatch();
    }
    [TargetRpc]
    public void TargetShowHand(NetworkConnection target, string[] hand)
    {
        Debug.Log($"[TargetShowHand] Wywołano dla gracza {playerIndex}, liczba kart: {hand.Length}");
        var panel = GameObject.Find("PlayerHand");
        if (panel == null)
        {
            Debug.LogWarning("Nie znaleziono PlayerHand");
            return;
        }

        var handDisplay = panel.GetComponent<MultiplayerHandDisplay>();
        if (handDisplay != null){
            handDisplay.ClearHand();
            handDisplay.ShowHand(new List<string>(hand), true);
            Debug.Log($"Rodzic: {handDisplay.transform.name}, Child count: {handDisplay.transform.childCount}");
        }
        else
            Debug.LogWarning("Brak komponentu HandDisplay na PlayerHand");
    }
    [TargetRpc]
    public void TargetShowOpponents(NetworkConnection target, int localPlayerIndex)
    {
        Debug.Log($"[CLIENT] Pokazuję przeciwników, localPlayerIndex = {localPlayerIndex}, dummy= {MultiplayerGameManager.Instance.dummyIndex}");
        int dummyIndex = MultiplayerGameManager.Instance.dummyIndex;
        for (int i = 0; i < 4; i++)
        {
            bool skip = i == localPlayerIndex && i!=MultiplayerGameManager.Instance.dummyIndex;
            if (skip) continue;
            bool isDummy = i== dummyIndex;
            bool faceUp = isDummy;
            Debug.Log($"[Client] Rysuje gracza {i}, faceUp={faceUp}");
            MultiplayerGameManager.Instance.ShowHandForPlayer(i, faceUp);
        }
    }
    [TargetRpc]
    public void TargetUpdatePlayerHand(NetworkConnection target, string[] hand)
    {
        Debug.Log($"[TargetUpdatePlayerHand] Ręka: {hand.Length} kart");

        var panel = GameObject.Find("PlayerHand");
        var display = panel?.GetComponent<MultiplayerHandDisplay>();
        if (display != null)
        {
            display.ShowHand(new List<string>(hand), true);
        }
        else
        {
            Debug.LogWarning("Brak MultiplayerHandDisplay przy aktualizacji ręki");
        }
    }
    void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"[Client] Przypisano playerIndex = {newIndex}");
    }
    [Command]
    public void CmdRequestStats()
    {
        MultiplayerGameManager.Instance.SendStatsToSingleClient(connectionToClient);
    }
    
}