using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public HandDisplay playerHandDisplay, topPlayerHandDisplay, leftPlayerHandDisplay, rightPlayerHandDisplay;
    public GameObject cardPrefab;
    public Transform tablePanel;
    //public static Dictionary<string, Sprite> cardSpriteDict = new Dictionary<string, Sprite>();

    public List<string> playerHand = new List<string>();
    public List<string> topHand = new List<string>();
    public List<string> leftHand = new List<string>();
    public List<string> rightHand = new List<string>();

    public enum PlayerTurn { Bottom, Left, Top, Right }
    public PlayerTurn currentTurn = PlayerTurn.Bottom;

    public string leadingSuit = null;

    public List<PlayedCard> currentTrick = new List<PlayedCard>();
    public int startingPlayerIndex = 0;
    public int currentPlayerIndex = 0;

    private bool isEndOfTurn= false;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        playerHandDisplay.LoadCardSprites();
        topPlayerHandDisplay.LoadCardSprites();
        leftPlayerHandDisplay.LoadCardSprites();
        rightPlayerHandDisplay.LoadCardSprites();
        DealCards();
        playerHandDisplay.ShowHand(playerHand, true);
        topPlayerHandDisplay.ShowHand(topHand, false);
        leftPlayerHandDisplay.ShowHand(leftHand, false);
        rightPlayerHandDisplay.ShowHand(rightHand, false);
    }

    void DealCards()
    {
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        playerHand = deck.GetRange(0, 13);
        topHand = deck.GetRange(13, 13);
        leftHand = deck.GetRange(26, 13);
        rightHand = deck.GetRange(39, 13);
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
        if(isEndOfTurn){
            return;
        }
        
        if (!playerHand.Contains(cardID))
        {
            Debug.LogWarning("Gracz nie ma tej karty!");
            return;
        }
        if (!string.IsNullOrEmpty(leadingSuit))
    {
        bool hasSuit = playerHand.Exists(card => card.StartsWith(leadingSuit));
        if (hasSuit && !cardID.StartsWith(leadingSuit))
        {
            Debug.LogWarning("Musisz zagrać kartę w prowadzonym kolorze: " + leadingSuit);
            return;
        }
    }
        playerHand.Remove(cardID);
        playerHandDisplay.ShowHand(playerHand, true);
        //currentTurn = (PlayerTurn)(((int)currentTurn + 1) % 4);
        //currentPlayerIndex = (currentPlayerIndex + 1) % 4;

        GameObject card = Instantiate(cardPrefab, tablePanel);
        Image image = card.GetComponent<Image>();

        if (playerHandDisplay.cardSpriteDict.ContainsKey(cardID))
        {
            image.sprite = playerHandDisplay.cardSpriteDict[cardID];
            currentTrick.Add(new PlayedCard(cardID, currentPlayerIndex));
        }
        else
        {
            Debug.LogWarning("Brak sprite'a dla: " + cardID);
        }
        if (string.IsNullOrEmpty(leadingSuit))
        {
            leadingSuit = cardID.Substring(0, 1);
            Debug.Log("Prowadzący kolor: " + leadingSuit);
        }

        Debug.Log("Gracz zagrał: " + cardID);
        EndTurn();
    }

    public bool CanPlayCard()
    {
        return currentTurn == PlayerTurn.Bottom;
    }

    public void EndTurn()
    {
        int cardsOnTable = tablePanel.childCount;

        if (cardsOnTable >= 4)
        {
            leadingSuit = null;
            isEndOfTurn = true;
            StartCoroutine(EvaluateTrickWinner());
            Debug.Log("Prowadzący kolor został zresetowany");
            return;
        }

        currentTurn = (PlayerTurn)(((int)currentTurn + 1) % 4);
        currentPlayerIndex = (currentPlayerIndex + 1) % 4;
        Debug.Log("Tura kończy się. Teraz tura gracza: " + currentTurn);

        if (currentTurn != PlayerTurn.Bottom){
            StartCoroutine(PlayAICard());
        }
    }

    IEnumerator PlayAICard()
    {
        if(isEndOfTurn){
            yield break;
        }

        yield return new WaitForSeconds(1.0f);

        List<string> aiHand = GetCurrentAIHand();
        string chosenCard = ChooseValidCard(aiHand);

        aiHand.Remove(chosenCard);
        SpawnCardOnTable(chosenCard);
        currentTrick.Add(new PlayedCard(chosenCard, currentPlayerIndex));

        if (string.IsNullOrEmpty(leadingSuit))
        {
            leadingSuit = chosenCard.Substring(0, 1);
            Debug.Log("Prowadzący kolor: " + leadingSuit);
        }

        UpdateAIHandDisplay();

        EndTurn();
    }

    string ChooseValidCard(List<string> hand)
    {
        if (leadingSuit != null)
        {
            List<string> matchingSuit = hand.FindAll(card => card.StartsWith(leadingSuit));
            if (matchingSuit.Count > 0)
            {
                Debug.Log("AI wybiera kartę pasującą do koloru: " + leadingSuit);
                return matchingSuit[Random.Range(0, matchingSuit.Count)];
            }
        }
        Debug.Log("AI nie ma kart pasujących do koloru, wybiera losową kartę.");
        return hand[Random.Range(0, hand.Count)];
    }

    List<string> GetCurrentAIHand()
    {
        switch (currentTurn)
        {
            case PlayerTurn.Left: return leftHand;
            case PlayerTurn.Top: return topHand;
            case PlayerTurn.Right: return rightHand;
        }
        return null;
    }

    void SpawnCardOnTable(string cardID)
    {
        GameObject card = Instantiate(playerHandDisplay.cardPrefab, tablePanel);
        card.GetComponent<CardUI>().cardID = cardID;
        Image image = card.GetComponent<Image>();
        if (playerHandDisplay.cardSpriteDict.ContainsKey(cardID)){
            image.sprite = playerHandDisplay.cardSpriteDict[cardID];
        }
        else{
            Debug.LogWarning("Brak sprite'a dla karty AI: " + cardID);
        }

        Vector2 position = Vector2.zero;
        switch (currentTurn)
        {
            case PlayerTurn.Bottom: position = new Vector2(-35, -70); break;
            case PlayerTurn.Top: position = new Vector2(-35, 70); break;
            case PlayerTurn.Left: position = new Vector2(-200, 0); break;
            case PlayerTurn.Right: position = new Vector2(200, 0); break;
        }

        card.GetComponent<RectTransform>().anchoredPosition = position;
    }

    void UpdateAIHandDisplay()
    {
        topPlayerHandDisplay.ShowHand(topHand, false);
        leftPlayerHandDisplay.ShowHand(leftHand, false);
        rightPlayerHandDisplay.ShowHand(rightHand, false);
    }

    void ClearTable()
    {
        isEndOfTurn= false;

        foreach (Transform child in tablePanel)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Stół został wyczyszczony.");
    }

    public class PlayedCard
    {
        public string cardID;
        public int playerIndex;

        public PlayedCard(string cardID, int playerIndex)
        {
            this.cardID = cardID;
            this.playerIndex = playerIndex;
        }
    }

    IEnumerator EvaluateTrickWinner()
    {
        string leadSuit = currentTrick[0].cardID.Substring(0, 1);

        PlayedCard winningCard = currentTrick[0];
        int highestValue = GetCardValue(winningCard.cardID);

        foreach (var played in currentTrick)
        {
            string suit = played.cardID.Substring(0, 1);
            int value = GetCardValue(played.cardID);

            if (suit == leadSuit && value > highestValue)
            {
                winningCard = played;
                highestValue = value;
            }
        }

        Debug.Log("Lewę wygrał gracz: " + winningCard.playerIndex);

        startingPlayerIndex = winningCard.playerIndex;
        currentPlayerIndex = startingPlayerIndex;
        currentTurn = (PlayerTurn)startingPlayerIndex;
        yield return new WaitForSeconds(2.0f);
        ClearTable();
        leadingSuit = "";
        currentTrick.Clear();

        if (currentPlayerIndex == 0)
        {
            Debug.Log("Twoja kolej!");
        }
        else
        {
            StartCoroutine(PlayAICard());
        }
    }

    int GetCardValue(string cardID)
    {
        string valuePart = cardID.Substring(1);
        switch (valuePart)
        {
            case "2": return 2;
            case "3": return 3;
            case "4": return 4;
            case "5": return 5;
            case "6": return 6;
            case "7": return 7;
            case "8": return 8;
            case "9": return 9;
            case "10": return 10;
            case "J": return 11;
            case "Q": return 12;
            case "K": return 13;
            case "A": return 14;
            default: return 0;
        }
    }
}