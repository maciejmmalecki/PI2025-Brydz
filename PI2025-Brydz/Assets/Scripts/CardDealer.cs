using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rozdaje karty czterem graczom.
/// </summary>

public class CardDealer : MonoBehaviour
{
    private List<Card>[] playerHands = new List<Card>[4];

    void Start()
    {
        Deck deck = new Deck();
        deck.Shuffle();

        for (int i = 0; i < 4; i++)
            playerHands[i] = new List<Card>();

        for (int i = 0; i < 52; i++)
        {
            Card card = deck.Draw();
            int player = i % 4;
            playerHands[player].Add(card);
        }
    }
}