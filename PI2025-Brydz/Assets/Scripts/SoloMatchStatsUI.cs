using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel w UI wyświetlający informacje o statystykach rozdania w trybie z botami:
/// historia licytacji, punkty, liczba lew, ostatnie karty.
/// </summary>
public class SoloMatchStatsUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text biddingHistoryText;
    public TMP_Text tricksText;
    public TMP_Text lastCardsText;
    public TMP_Text pointsAboveLineText;
    public TMP_Text pointsBelowLineText;

    void Start() => panel.SetActive(false);

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) UpdateDisplay();
    }

    void UpdateDisplay()
    {
        var gm = GameManager.Instance;
        biddingHistoryText.text = string.Join("\n", gm.biddingHistory.ConvertAll(b => b.playerName + ": " + b.call));
        tricksText.text = $"Lewy oddane: {gm.GetDefendersTricks()}";
        lastCardsText.text = string.Join("\n", gm.GetLastPlayedCardsWithPlayers());
        pointsAboveLineText.text= $"NS: {gm.GetPointsAboveLine(0)}  EW: {gm.GetPointsAboveLine(1)}";
        pointsBelowLineText.text= $"NS: {gm.GetPointsBelowLine(0)}  EW: {gm.GetPointsBelowLine(1)}";
    }
}