using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandDisplay : MonoBehaviour
{
    public Transform handPanel;
    public GameObject cardPrefab;

    private Dictionary<string, Sprite> cardSpriteDict = new Dictionary<string, Sprite>();

    private void Awake()
    {
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Cards"); // zakładamy folder: Resources/Cards

        foreach (var sprite in loadedSprites)
        {
            cardSpriteDict[sprite.name] = sprite;
        }
    }

    public void ShowHand(List<string> cards)
    {
        foreach (Transform child in handPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (string cardID in cards)
        {
            GameObject card = Instantiate(cardPrefab, handPanel);
            Image image = card.GetComponent<Image>();

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
    }
}
