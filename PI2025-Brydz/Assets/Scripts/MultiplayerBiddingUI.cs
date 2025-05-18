using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Komponent interfejsu użytkownika do licytacji w trybie multiplayer.
/// Obsługuje wyświetlanie możliwych ofert, przyciski i przesyłanie wybranej oferty.
/// </summary>
public class MultiplayerBiddingUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text currentBidText;
    public Transform buttonContainer;       
    public GameObject bidButtonPrefab;    
    private string currentBid = null;   

    private readonly string[] allBids =
    {
        "1♣", "1♦", "1♥", "1♠", "1NT",
        "2♣", "2♦", "2♥", "2♠", "2NT",
        "3♣", "3♦", "3♥", "3♠", "3NT",
        "4♣", "4♦", "4♥", "4♠", "4NT",
        "5♣", "5♦", "5♥", "5♠", "5NT",
        "6♣", "6♦", "6♥", "6♠", "6NT",
        "7♣", "7♦", "7♥", "7♠", "7NT"
    };

    void Start()
    {
        GenerateButtons();
        panel.SetActive(false);
    }

    void GenerateButtons()
    {
        foreach (string bid in allBids)
        {
            GameObject btnObj = Instantiate(bidButtonPrefab, buttonContainer);
            btnObj.GetComponentInChildren<TMP_Text>().text = bid;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnBidSelected(bid));
        }

        // Dodaj przycisk „Pas” na końcu
        GameObject passBtn = Instantiate(bidButtonPrefab, buttonContainer);
        passBtn.GetComponentInChildren<TMP_Text>().text = "Pas";
        passBtn.GetComponent<Button>().onClick.AddListener(OnPassSelected);
    }

    public void ShowBiddingUI(bool show, string currentBid=null)
    {
        this.currentBid = currentBid;
        panel.SetActive(show);

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        List<string> validCalls = GetAvailableCalls(currentBid);

        foreach (string call in validCalls)
        {
            GameObject btn = Instantiate(bidButtonPrefab, buttonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = call;
            btn.GetComponent<Button>().onClick.AddListener(() => OnBidSelected(call));
        }
    }

    public void UpdateCurrentBidText(string text)
    {
        if (currentBidText != null)
            currentBidText.text = text;
    }

    public void OnBidSelected(string bid)
    {
        var player = NetworkClient.connection.identity.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            BiddingData data = BiddingData.FromString(bid);
            player.CmdSubmitBid(data);
            ShowBiddingUI(false);
        }
    }

    public void OnPassSelected()
    {
        var player = NetworkClient.connection.identity.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            player.CmdSubmitBid(BiddingData.Pass());
            ShowBiddingUI(false);
        }
    }

    private List<string> GetAvailableCalls(string currentBid)
    {
        List<string> options = new();

        int currentIndex = System.Array.IndexOf(allBids, currentBid);
        for (int i = currentIndex + 1; i < allBids.Length; i++)
        {
            options.Add(allBids[i]);
        }

        options.Add("Pas");
        return options;
    }

    public void ShowFinalContract(int winningBidderIndex, string bid)
    {
        currentBidText.text= $"Gracz {winningBidderIndex}: {bid}";
    }

    public void ResetBiddingUI()
    {
        currentBid = null;
        currentBidText.text = "Aktualna: brak";
    }
}