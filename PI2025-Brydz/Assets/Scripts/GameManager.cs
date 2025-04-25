using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public HandDisplay playerHandDisplay, topPlayerHandDisplay, leftPlayerHandDisplay, rightPlayerHandDisplay;
    public GameObject cardPrefab;
    public Transform tablePanel;
    public Dictionary<string, Sprite> cardSpriteDict = new Dictionary<string, Sprite>();

    public List<string> playerHand = new List<string>();
    public List<string> topHand = new List<string>();
    public List<string> leftHand = new List<string>();
    public List<string> rightHand = new List<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        LoadCardSprites();
        DealCards();
        playerHandDisplay.ShowHand(playerHand, true);
        topPlayerHandDisplay.ShowHand(topHand, false);
        leftPlayerHandDisplay.ShowHand(leftHand, false);
        rightPlayerHandDisplay.ShowHand(rightHand, false);
    }

    void DealCards()
    {
        // przykładowe rozdanie 13 kart
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        playerHand = deck.GetRange(0, 13);
        topHand = deck.GetRange(13, 13);
        leftHand = deck.GetRange(26, 13);
        rightHand = deck.GetRange(39, 13);
    }
    void LoadCardSprites()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Cards");

        foreach (Sprite sprite in sprites)
        {
            cardSpriteDict[sprite.name] = sprite;
        }
    }

    List<string> GenerateDeck()
    {
        string[] suits = { "S", "H", "D", "C" };
        string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        List<string> deck = new List<string>();

        foreach (string suit in suits)
        {
            foreach (string value in values)
            {
                deck.Add(suit + value);
            }
        }

        return deck;
    }

    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            var temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
    public void PlayCard(string cardID)
    {

        if (!playerHand.Contains(cardID))
        {
            Debug.LogWarning("Gracz nie ma tej karty!");
            return;
        }

        // Usuń z ręki
        playerHand.Remove(cardID);
        playerHandDisplay.ShowHand(playerHand, true);

        // Stwórz obiekt na stole
        GameObject card = Instantiate(cardPrefab, tablePanel);
        Image image = card.GetComponent<Image>();

        if (cardSpriteDict.ContainsKey(cardID))
        {
            image.sprite = cardSpriteDict[cardID];
        }
        else
        {
            Debug.LogWarning("Brak sprite'a dla: " + cardID);
        }

        Debug.Log("Gracz zagrał: " + cardID);
    }
}