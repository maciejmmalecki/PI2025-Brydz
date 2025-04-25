using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class HandDisplay : MonoBehaviour
{
    public Transform handPanel;
    public GameObject cardPrefab;
    public Sprite cardBackSprite;
    public Transform tablePanel;

    private Dictionary<string, Sprite> cardSpriteDict = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Cards"); // zakładamy folder: Resources/Cards

        foreach (var sprite in loadedSprites)
        {
            cardSpriteDict[sprite.name] = sprite;
        }
    }

    public void ShowHand(List<string> cards, bool isPlayer)
    {
        foreach (Transform child in handPanel)
        {
            Destroy(child.gameObject);
        }

        float offsetX = 35f;
        int index = 0;

        cards.Sort((card1, card2) => {
        char suit1 = card1[0];
        char suit2 = card2[0];

        int suitOrder1 = GetSuitOrder(suit1);
        int suitOrder2 = GetSuitOrder(suit2);

        if (suitOrder1 != suitOrder2)
        {
            return suitOrder1.CompareTo(suitOrder2);
        }

        int value1 = GetCardValue(card1.Substring(1));
        int value2 = GetCardValue(card2.Substring(1));

        return value1.CompareTo(value2);
    });

    int GetSuitOrder(char suit)
    {
        switch (suit)
        {
            case 'C': return 0;
            case 'D': return 1;
            case 'H': return 2;
            case 'S': return 3; 
            default: return 4;
        }
    }

    int GetCardValue(string value)
    {
        switch (value)
        {
            case "J": return 11;
            case "Q": return 12;
            case "K": return 13;
            case "A": return 14;
            default: return int.Parse(value);
        }
    }

        foreach (string cardID in cards)
        {
            GameObject card = Instantiate(cardPrefab, handPanel);
            Image image = card.GetComponent<Image>();
            if(isPlayer){

                if (cardSpriteDict.ContainsKey(cardID))
                {
                  image.sprite = cardSpriteDict[cardID];
                }
                else
                {
                    Debug.LogWarning("Brak sprite'a dla karty: " + cardID);
                }

                card.GetComponent<CardUI>().cardID = cardID;
            }
            else{
                image.sprite = cardBackSprite;
            }

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(index * offsetX, 0f);
            //Debug.Log("Ustawiam pozycję: " + (index * offsetX));
            index++;
        }
    }
}
