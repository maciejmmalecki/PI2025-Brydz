using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wyświetla rękę gracza w trybie multiplayer.
/// Obsługuje sortowanie, układanie i aktualizację widoku.
/// </summary>
public class MultiplayerHandDisplay : MonoBehaviour
{
    [Header("Prefab karty i ustawienia")]
    public GameObject MultiplayerCardPrefab;
    public bool isFaceUp = true;

    [Header("Zasoby")]
    public Dictionary<string, Sprite> cardSpriteDict = new();

    [Header("Sprite do rewersu")]
    public Sprite backSprite;

    private List<string> currentCards = new();

    void Awake()
    {
        LoadCardSprites();
    }

    public void LoadCardSprites()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Cards");

        foreach (Sprite sprite in sprites)
        {
            if (!cardSpriteDict.ContainsKey(sprite.name))
            {
                cardSpriteDict[sprite.name] = sprite;
            }
        }

        Debug.Log($"Załadowano {cardSpriteDict.Count} sprite’ów kart.");
    }

    /// <summary>
    /// Pokazuje rękę (listę kart jako stringi), odkrytą lub zakrytą.
    /// </summary>
    public void ShowHand(List<string> cards, bool isPlayer)
    {
        LoadCardSprites();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // sortowanie według koloru i wartości
        cards.Sort((card1, card2) =>
        {
            char suit1 = card1[0];
            char suit2 = card2[0];

            int suitOrder1 = GetSuitOrder(suit1);
            int suitOrder2 = GetSuitOrder(suit2);

            if (suitOrder1 != suitOrder2)
                return suitOrder1.CompareTo(suitOrder2);

            int value1 = GetCardValue(card1.Substring(1));
            int value2 = GetCardValue(card2.Substring(1));

            return value1.CompareTo(value2);
        });

        float offsetX = 27f;
        int index = 0;

        foreach (string cardID in cards)
        {
            GameObject card = Instantiate(MultiplayerCardPrefab, transform);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(index * offsetX, 0);

            var ui = card.GetComponent<MultiplayerCardUI>();
            if (ui != null)
            {
                Sprite sprite = cardID == "BACK"? backSprite : (cardSpriteDict.ContainsKey(cardID) ? cardSpriteDict[cardID] : null);
                ui.InitCard(sprite, cardID);
                ui.SetFaceUp(isPlayer && cardID != "BACK");
                if(sprite == null){
                    Debug.Log($"Nie znaleziono spritea dla {cardID}");
                }
            }

            index++;
        }
    }

    public void ClearHand()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        currentCards.Clear();
    }

    public List<string> GetCurrentCards()
    {
        return new List<string>(currentCards);
    }
    private int GetSuitOrder(char suit)
    {
        switch (suit)
        {
            case 'C': return 0; // trefl
            case 'D': return 1; // karo
            case 'H': return 2; // kier
            case 'S': return 3; // pik
            default: return 4;
        }
    }

    private int GetCardValue(string value)
    {
        return value switch
        {
            "2" => 2,
            "3" => 3,
            "4" => 4,
            "5" => 5,
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => 0
        };
    }
}