using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Klasa reprezentująca talię kart.
/// </summary>

public class Deck
{
    private List<Card> cards = new();

    /// <summary>
    /// Tworzy nową, posortowaną talię 52 kart.
    /// </summary>

    public Deck()
    {
        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                cards.Add(new Card(suit, rank));
            }
        }
    }

    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int rnd = Random.Range(i, cards.Count);
            (cards[i], cards[rnd]) = (cards[rnd], cards[i]);
        }
    }

    public Card Draw()
    {
        if (cards.Count == 0) return null;
        Card card = cards[0];
        cards.RemoveAt(0);
        return card;
    }

    public int Count => cards.Count;
}