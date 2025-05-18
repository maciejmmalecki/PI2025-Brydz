using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// UI reprezentujące kartę w trybie multiplayer.
/// Obsługuje kliknięcie, wyrzucanie i synchronizację stanu.
/// </summary>
public class MultiplayerCardUI : MonoBehaviour
{
    public Image cardImage;
    public string cardID;
    public Sprite backSprite;
    public Sprite frontSprite;

    void Start()
    {
        var button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnCardClicked);
    }

    public void InitCard(Sprite sprite, string id)
    {
        frontSprite = sprite;
        cardImage.sprite = sprite;
        cardID = id;
    }

    public void SetFaceUp(bool faceUp)
    {
        cardImage.sprite = faceUp ? frontSprite : backSprite;
    }

    public void OnCardClicked()
    {
        if (!MultiplayerGameManager.Instance.CanPlayCard())
        {
            Debug.Log("Nie twoja tura");
            return;
        }

        var allPlayers = FindObjectsOfType<NetworkPlayer>();
        var localPlayer = allPlayers.FirstOrDefault(p => p.isLocalPlayer);

        if (localPlayer == null)
        {
            Debug.LogWarning("Nie znaleziono lokalnego gracza!");
            return;
        }
        var gm = MultiplayerGameManager.Instance;
        if(gm.currentPlayerIndex== gm.dummyIndex && localPlayer.playerIndex!= gm.winningBidderIndex){
            Debug.Log("Tylko wygrany licytacji moze grac za dummiego");
            return;
        }

        Debug.Log($"[{localPlayer.playerIndex}] Wysyłam CmdPlayCard({cardID})");
        localPlayer.CmdPlayCard(cardID);
    }
}