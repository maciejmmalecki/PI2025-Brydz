using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel w UI wyświetlający informacje o statystykach rozdania:
/// historia licytacji, punkty, liczba lew, ostatnie karty.
/// </summary>
public class MatchStatsUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text biddingHistoryText;
    public TMP_Text scoreText;
    public TMP_Text tricksText;
    public TMP_Text lastCardsText;

    void Start() => panel.SetActive(false);

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) UpdateDisplay();
    }

    void UpdateDisplay()
    {
        var gm = MultiplayerGameManager.Instance;
        biddingHistoryText.text = string.Join("\n", gm.biddingHistory.ConvertAll(b => b.playerName + ": " + b.call));
        scoreText.text = $"NS: {gm.GetPoints(0)}    EW: {gm.GetPoints(1)}";
        tricksText.text = $"Lewy oddane: {gm.GetTrickCount()}";
        lastCardsText.text = string.Join(", ", gm.GetLastPlayedCards());
    }
}