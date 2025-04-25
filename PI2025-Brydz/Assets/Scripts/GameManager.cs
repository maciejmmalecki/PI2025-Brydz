using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public HandDisplay playerHandDisplay, topPlayerHandDisplay, leftPlayerHandDisplay, rightPlayerHandDisplay;

    public List<string> playerHand = new List<string>();
    public List<string> topHand = new List<string>();
    public List<string> leftHand = new List<string>();
    public List<string> rightHand = new List<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        DealCards();
        playerHandDisplay.ShowHand(playerHand, true);
        topPlayerHandDisplay.ShowHand(topHand, false);
        leftPlayerHandDisplay.ShowHand(leftHand, false);
        rightPlayerHandDisplay.ShowHand(rightHand, false);
    }

    void DealCards()
    {
        // przykładowe rozdanie 13 kart
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        playerHand = deck.GetRange(0, 13);
        topHand = deck.GetRange(13, 13);
        leftHand = deck.GetRange(26, 13);
        rightHand = deck.GetRange(39, 13);
    }

    List<string> GenerateDeck()
    {
        string[] suits = { "S", "H", "D", "C" };
        string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        List<string> deck = new List<string>();

        foreach (string suit in suits)
        {
            foreach (string value in values)
            {
                deck.Add(suit + value);
            }
        }

        return deck;
    }

    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            var temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    public void PlayCard(string cardID, GameObject cardObject)
    {
        Debug.Log("Zagrano kartę: " + cardID);
        Destroy(cardObject);
    }
}