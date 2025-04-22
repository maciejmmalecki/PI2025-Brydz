using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public HandDisplay handDisplay;

    public List<string> playerHand = new List<string>();

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
        handDisplay.ShowHand(playerHand);
    }

    void DealCards()
    {
        // przykładowe rozdanie 13 kart
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        for (int i = 0; i < 13; i++)
        {
            playerHand.Add(deck[i]);
        }
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