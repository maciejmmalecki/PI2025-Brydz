using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tworzy i tasuje talię w trybie multiplayer.
/// Zwraca listę zakodowanych kart (np. "S10", "D5").
/// </summary>
public static class MultiplayerDeck
{
    public static List<string> CreateDeck()
    {
        string[] suits = { "S", "H", "D", "C" };
        string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        List<string> deck = new();
        foreach (string suit in suits)
        {
            foreach (string value in values)
            {
                deck.Add(suit + value); // np. "S10", "HQ"
            }
        }

        return deck;
    }

    public static void Shuffle(List<string> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int rand = Random.Range(i, deck.Count);
            (deck[i], deck[rand]) = (deck[rand], deck[i]);
        }
    }
}