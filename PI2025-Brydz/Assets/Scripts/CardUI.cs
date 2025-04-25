using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public Image cardImage;
    public string cardID;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => OnCardClicked());
    }

    public void InitCard(Sprite sprite, string id)
    {
        cardImage.sprite = sprite;
        cardID = id;
    }

    public void OnCardClicked()
    {
        Debug.Log("Zagrano karte: " + cardID);
        GameManager.Instance.PlayCard(cardID);
    }
}