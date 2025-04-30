using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BiddingUI : MonoBehaviour
{
    public static BiddingUI Instance;
    public GameObject panel;
    private string selectedBid = "";
    public TMP_Text currentBidText; // Komponent wyświetlający aktualną licytację
    public GameObject bidButtonPrefab; // Prefab przycisku do licytacji
    public Transform buttonContainer; // Kontener, w którym będą tworzone przyciski
    private List<GameObject> bidButtons = new List<GameObject>();
    private string currentHighestBid = ""; 

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false); // Ukryj panel na start
    }

    private void Start()
    {
        // Dodaj przyciski dynamicznie w oparciu o możliwe licytacje
        AddBiddingButtons();
    }

    // Dodaj przyciski do panelu
    void AddBiddingButtons()
    {
        // Lista możliwych licytacji (przykładowe licytacje)
        string[] suits = { "C", "D", "H", "S", "NT" };
    
        // Dodaj wszystkie możliwe licytacje od 1C do 7NT
        for (int level = 1; level <= 7; level++)
        {
            foreach (string suit in suits)
            {
                string bid = level + suit;
                GameObject newButton = Instantiate(bidButtonPrefab, buttonContainer);
                Button button = newButton.GetComponent<Button>();
                TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
                buttonText.text = bid;
                button.onClick.AddListener(() => OnBidButtonClicked(bid));
                bidButtons.Add(newButton);
            }
        }

        // Dodaj osobny przycisk "Pass"
        GameObject passButton = Instantiate(bidButtonPrefab, buttonContainer);
        Button passBtn = passButton.GetComponent<Button>();
        TMP_Text passText = passButton.GetComponentInChildren<TMP_Text>();
        passText.text = "Pas";
        passBtn.onClick.AddListener(() => OnBidButtonClicked("Pas"));
        bidButtons.Add(passButton);
        }

    public void ShowBiddingPanel(string highestBid)
    {
        selectedBid = "";
        currentHighestBid = highestBid; 
        panel.SetActive(true);
        UpdateBidButtons();
        // Ustawienie początkowego tekstu
        if (currentBidText != null)
            currentBidText.text = (highestBid == "" ? "brak" : "Highest bid: "+highestBid);
    }

    public void OnBidButtonClicked(string bid)
    {
        selectedBid = bid;
        panel.SetActive(false);

        // Aktualizacja tekstu w UI
        if (currentBidText != null)
            //currentBidText.text = "Twój wybór: " + bid;

        if (bid == "7NT")
        {
            GameManager.Instance.EndBiddingEarly(); // Tę metodę musisz dopisać
        }
    }

    public string GetSelectedBid()
    {
        return selectedBid;
    }

    void UpdateBidButtons()
    {
        foreach (var btn in bidButtons)
        {
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
            string bid = btnText.text;

            if (bid == "Pas")
            {
                btn.SetActive(true);
                continue;
            }

            bool isLegal = IsBidHigher(bid, currentHighestBid);
            btn.SetActive(isLegal);
        }
    }

    bool IsBidHigher(string newBid, string currentBid)
    {
        if (newBid == "Pas" || string.IsNullOrEmpty(newBid)) return false;
        if (string.IsNullOrEmpty(currentBid)) return true;

        int GetBidValue(string bid)
        {
            if (bid == "Pas") return -1;

            string[] suits = { "C", "D", "H", "S", "NT" };
            int level = int.Parse(bid[0].ToString());
            string suit = bid.Substring(1);

            int suitValue = System.Array.IndexOf(suits, suit);
            return level * 5 + suitValue;
        }

        return GetBidValue(newBid) > GetBidValue(currentBid);
    }
}
