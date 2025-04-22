using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public Image cardImage;
    public string cardID;

    public void InitCard(Sprite sprite, string id)
    {
        cardImage.sprite = sprite;
        cardID = id;
    }

    public void OnCardClicked()
    {
        Debug.Log("Zagrano karte: " + cardID);
        // Przenieś kartę na stół
        GameManager.Instance.PlayCard(cardID, gameObject);
    }
}