using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Panel w UI wyświetlający informacje o statystykach rozdania:
/// historia licytacji, punkty, liczba lew, ostatnie karty.
/// </summary>
public class MatchStatsUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text biddingHistoryText;
    public TMP_Text pointsAboveLineText;
    public TMP_Text pointsBelowLineText;
    public TMP_Text tricksText;
    public TMP_Text lastCardsText;

    void Start() => panel.SetActive(false);

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) RequestStatsFromServer();
    }

    void UpdateDisplay()
    {
        var gm = MultiplayerGameManager.Instance;
        biddingHistoryText.text = string.Join("\n", gm.biddingHistory.ConvertAll(b => b.playerName + ": " + b.call));
        pointsAboveLineText.text= $"NS: {gm.GetPointsAboveLine(0)}  EW: {gm.GetPointsAboveLine(1)}";
        pointsBelowLineText.text= $"NS: {gm.GetPointsBelowLine(0)}  EW: {gm.GetPointsBelowLine(1)}";
        tricksText.text = $"Lewy oddane: {gm.GetDefendersTricks()}";
        lastCardsText.text = string.Join("\n", gm.GetLastPlayedCardsWithPlayers());
    }
    public void ReceiveStatsFromServer(List<string> bids, List<string> lastTrick, int defendersTricks, int nsBelow, int nsAbove, int ewBelow, int ewAbove)
    {
        biddingHistoryText.text = string.Join("\n", bids);
        lastCardsText.text = string.Join("\n", lastTrick);
        tricksText.text = $"Lewy oddane: {defendersTricks}";
        pointsAboveLineText.text= $"NS: {nsAbove}  EW: {ewAbove}";
        pointsBelowLineText.text= $"NS: {nsBelow}  EW: {ewBelow}";
    }
    public void RequestStatsFromServer()
    {
        var localPlayer = NetworkClient.connection.identity.GetComponent<NetworkPlayer>();
        if(localPlayer != null)
        {
            localPlayer.CmdRequestStats();
        }
    }
}