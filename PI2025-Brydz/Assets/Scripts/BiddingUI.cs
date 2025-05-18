using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Obsługuje interfejs użytkownika licytacji: wyświetlanie przycisków i wybór oferty
/// </summary>
public class BiddingUI : MonoBehaviour
{
    public static BiddingUI Instance;
    public GameObject panel;
    private string selectedBid = "";
    public TMP_Text currentBidText;
    public GameObject bidButtonPrefab;
    public Transform buttonContainer;
    private List<GameObject> bidButtons = new List<GameObject>();
    private string currentHighestBid = ""; 

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        AddBiddingButtons();
    }

    /// <summary>
    /// Tworzy dynamicznie wszystkie przyciski licytacji
    /// </summary>
    void AddBiddingButtons()
    {  
        string[] suits = { "C", "D", "H", "S", "NT" };
    
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

    /// <summary>
    /// Pokazuje panel licytacji i ustawia dostępne oferty
    /// </summary>
    /// <param name="highestBid">Aktualnie najwyższa licytacja</param>
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

    /// <summary>
    /// Obsługuje kliknięcie przycisku licytacji i zapisuje wybór.
    /// </summary>
    /// <param name="bid">Licytacja wybrana przez gracza</param>
    public void OnBidButtonClicked(string bid)
    {
        selectedBid = bid;
        panel.SetActive(false);

        if (bid == "7NT")
        {
            GameManager.Instance.EndBiddingEarly(); // Tę metodę musisz dopisać
        }
    }

    /// <summary>
    /// Zwraca aktualnie wybraną licytację.
    /// </summary>
    public string GetSelectedBid()
    {
        return selectedBid;
    }

    /// <summary>
    /// Aktualizuje przyciski licytacyjne na podstawie aktualnej najwyższej licytacji.
    /// </summary>
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

    /// <summary>
    /// Sprawdza, czy nowa licytacja jest wyższa niż poprzednia.
    /// </summary>
    /// <param name="newBid">Nowa oferta</param>
    /// <param name="currentBid">Aktualna najwyższa</param>
    /// <returns>Czy nowa jest wyższa</returns>
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
