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
        if(GameManager.Instance.CanPlayCard()){
            GameManager.Instance.PlayCard(cardID);
        }
    }

    void OnMouseDown()
    {
        if (!GameManager.Instance.CanPlayCard()){
            return;
        }
        if (GameManager.Instance.leadingSuit == null){
            GameManager.Instance.leadingSuit = cardID.Substring(0, 1);
        }
        transform.SetParent(GameManager.Instance.tablePanel);
        GameManager.Instance.playerHand.Remove(cardID);
        GameManager.Instance.playerHandDisplay.ShowHand(GameManager.Instance.playerHand, true);
        GameManager.Instance.EndTurn();
    }
}