using UnityEngine;
using UnityEngine.UI;

///<summary>
/// Obsługuje pojedynczą kartę w UI, przypisuje grafikę i reaguje na kliknięcia
/// </summary>

public class CardUI : MonoBehaviour
{
    ///<summary>
    /// Obrazek karty do wyświetlenia
    /// </summary>
    public Image cardImage;
    public string cardID;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => OnCardClicked());
    }

    /// <summary>
    /// Inicjalizuje kartę z odpowiednią grafiką i ID.
    /// </summary>
    /// <param name="sprite">Sprite karty.</param>
    /// <param name="id">ID karty.</param>

    public void InitCard(Sprite sprite, string id)
    {
        cardImage.sprite = sprite;
        cardID = id;
    }

    /// <summary>
    /// Wywoływane przy kliknięciu karty.
    /// Przekazuje zagranie do GameManagera.
    /// </summary>
    
    public void OnCardClicked()
    {
        Debug.Log("Zagrano karte: " + cardID);
        Debug.Log("Czy karta dummy? " + GameManager.Instance.players[GameManager.Instance.dummyIndex].hand.Contains(cardID));
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