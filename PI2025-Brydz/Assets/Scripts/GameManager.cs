using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Główny menedżer gry. Zarządza rozgrywką, kolejnością tur, rękami graczy oraz stołem.
/// </summary>

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// Wyświetlacze rąk gracza i AI.
    /// </summary>

    public HandDisplay playerHandDisplay, topPlayerHandDisplay, leftPlayerHandDisplay, rightPlayerHandDisplay;
    public GameObject cardPrefab;
    public Transform tablePanel;
    public GameObject endGamePanel;
    public TextMeshProUGUI endGameText;

    /// <summary>
    /// Listy kart posiadanych przez graczy.
    /// </summary>
    
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
    public List<Player> players = new List<Player>();
    private int winningBidderIndex = -1;
    private int[] partsWon = new int[2];
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        endGamePanel.SetActive(false);
        players = new List<Player>
        {
            new Player("Player", false),
            new Player("Left Player", true),
            new Player("Top Player", true),
            new Player("Right Player", true)
        };
        
        playerHandDisplay.LoadCardSprites();
        topPlayerHandDisplay.LoadCardSprites();
        leftPlayerHandDisplay.LoadCardSprites();
        rightPlayerHandDisplay.LoadCardSprites();
        DealCards();
        StartCoroutine(StartBidding());
    }

    /// <summary>
    /// Tasuje i rozdaje karty graczom.
    /// </summary>

    void DealCards()
    {
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        playerHand = deck.GetRange(0, 13);
        topHand = deck.GetRange(13, 13);
        leftHand = deck.GetRange(26, 13);
        rightHand = deck.GetRange(39, 13);
        players[0].hand=playerHand;
        players[1].hand=leftHand;
        players[2].hand=topHand;
        players[3].hand=rightHand;
        playerHandDisplay.ShowHand(playerHand, true);
        topPlayerHandDisplay.ShowHand(topHand, false);
        leftPlayerHandDisplay.ShowHand(leftHand, false);
        rightPlayerHandDisplay.ShowHand(rightHand, false);
    }

    /// <summary>
    /// Tworzy nową talię 52 kart.
    /// </summary>

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

    /// <summary>
    /// Tasuje listę kart.
    /// </summary>

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

    /// <summary>
    /// Gracz zagrywa kartę.
    /// </summary>

    public void PlayCard(string cardID)
    {
        if(!biddingInProgress){
        if(isEndOfTurn){
            return;
        }
        Debug.Log("Czy isEndOfTurn? " + isEndOfTurn);
        bool isDummyTurn = currentTurn == (PlayerTurn)dummyIndex;
        currentPlayerIndex = (int)currentTurn;
        bool isDeclarerPlayingDummy = (currentPlayerIndex == dummyIndex) && ((highestBidIndex + 2) % 4 == dummyIndex);
        List<string> handToCheck = isDeclarerPlayingDummy ? players[dummyIndex].hand : players[currentPlayerIndex].hand;
        
        
        if (!handToCheck.Contains(cardID))
        {
            Debug.LogWarning("Gracz nie ma tej karty!");
            return;
        }
        Debug.Log("Czy gracz ma kartę w ręku? " + handToCheck.Contains(cardID));
        if (!string.IsNullOrEmpty(leadingSuit))
        {
            bool hasSuit = handToCheck.Exists(card => card.StartsWith(leadingSuit));
            if (hasSuit && !cardID.StartsWith(leadingSuit))
            {
                Debug.LogWarning("Musisz zagrać kartę w prowadzonym kolorze: " + leadingSuit);
                return;
            }
        }
        handToCheck.Remove(cardID);
        bool removed= handToCheck.Remove(cardID);
        Debug.Log("Usunięto kartę z ręki: " + removed);
        if (currentPlayerIndex == 0)
        {
            playerHandDisplay.ShowHand(handToCheck, true);
        }
        else if (currentPlayerIndex == dummyIndex && isDeclarerPlayingDummy)
        {
            topPlayerHandDisplay.ShowHand(handToCheck, true);
        }
        int actualPlayerIndex = isDeclarerPlayingDummy ? dummyIndex : currentPlayerIndex;
        SpawnCardOnTable(cardID, actualPlayerIndex);
        currentTrick.Add(new PlayedCard(cardID, actualPlayerIndex));

        if (string.IsNullOrEmpty(leadingSuit))
        {
            leadingSuit = cardID.Substring(0, 1);
            Debug.Log("Prowadzący kolor: " + leadingSuit);
        }
        if (isDummyTurn && isDeclarerPlayingDummy)
        {
            currentTurn = (PlayerTurn)winningBidderIndex;
            isPlayingDummy = true;
        }
        else
        {
            isPlayingDummy = false;
        }
        UpdateAIHandDisplay();

        Debug.Log("Gracz zagrał: " + cardID);
        EndTurn();
        }
    }

    /// <summary>
    /// Sprawdza, czy gracz może teraz zagrać kartę (czy jest jego kolej).
    /// </summary>

    public bool CanPlayCard()
    {
        if (currentTurn == (PlayerTurn)dummyIndex)
        {
            return dummyIndex==2;
        }
        else if(dummyIndex!=0)
        {
            return currentTurn == PlayerTurn.Bottom;
        }else{
            return false;
        }
    }

    /// <summary>
    /// Obsługuje zakończenie jednej tury.
    /// </summary>

    public void EndTurn()
    {
        int cardsOnTable = tablePanel.childCount;
        if(biddingInProgress) return;

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

        if (players[currentPlayerIndex].IsAI || (currentTurn == (PlayerTurn)dummyIndex && currentPlayerIndex == winningBidderIndex)){
            StartCoroutine(PlayAICard());
        }
    }

    /// <summary>
    /// AI zagrywa kartę.
    /// </summary>

    IEnumerator PlayAICard()
    {
        if(isEndOfTurn){
            yield break;
        }
        string chosenCard;
        bool isDummyTurn = currentTurn == (PlayerTurn)dummyIndex;
        bool isDeclarerPlayingDummy = (currentPlayerIndex == dummyIndex) && (highestBidIndex == 0);
        List<string> handToCheck = isDeclarerPlayingDummy ? players[dummyIndex].hand : players[currentPlayerIndex].hand;
        if (isDeclarerPlayingDummy)
        {
            yield break;
        }
        else
        {
            isPlayingDummy = false;
        }
        if (currentPlayerIndex == dummyIndex && winningBidderIndex % 2 == 0 && winningBidderIndex==0)
        {
            yield break;
        }
        if (currentPlayerIndex == 0)
        {
            playerHandDisplay.ShowHand(handToCheck, true);
        }
        else if (currentPlayerIndex == dummyIndex && isDeclarerPlayingDummy)
        {
            topPlayerHandDisplay.ShowHand(handToCheck, true);
        }

        yield return new WaitForSeconds(1.0f);

        List<string> aiHand = GetCurrentAIHand();
        chosenCard = ChooseValidCard(handToCheck);


        handToCheck.Remove(chosenCard);
        SpawnCardOnTable(chosenCard,currentPlayerIndex);
        currentTrick.Add(new PlayedCard(chosenCard, currentPlayerIndex));

        if (string.IsNullOrEmpty(leadingSuit))
        {
            leadingSuit = chosenCard.Substring(0, 1);
            Debug.Log("Prowadzący kolor: " + leadingSuit);
        }

        UpdateAIHandDisplay();
        playerHandDisplay.ShowHand(playerHand, true);
        EndTurn();
    }

    /// <summary>
    /// Wybiera poprawną kartę do zagrania przez AI, respektując prowadzony kolor.
    /// </summary>

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

    /// <summary>
    /// Zwraca rękę AI aktualnie wykonującego ruch.
    /// </summary>

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

    /// <summary>
    /// Pozwala wystawić kartę na stole.
    /// </summary>

    void SpawnCardOnTable(string cardID, int playerIndex)
    {
        GameObject card = Instantiate(playerHandDisplay.cardPrefab, tablePanel);
        card.GetComponent<CardUI>().cardID = cardID;
        Image image = card.GetComponent<Image>();

        if (playerHandDisplay.cardSpriteDict.ContainsKey(cardID))
        {
            image.sprite = playerHandDisplay.cardSpriteDict[cardID];
        }
        else
        {
            Debug.LogWarning("Brak sprite'a dla karty: " + cardID);
        }
        Debug.Log("Spawnuję kartę: " + cardID + " od gracza: " + playerIndex);
        Vector2 position = Vector2.zero;
        PlayerTurn turn = (PlayerTurn)playerIndex;

        switch (turn)
        {
            case PlayerTurn.Bottom: position = new Vector2(-35, -70); break;
            case PlayerTurn.Top: position = new Vector2(-35, 70); break;
            case PlayerTurn.Left: position = new Vector2(-250, 0); break;
            case PlayerTurn.Right: position = new Vector2(200, 0); break;
        }

        card.GetComponent<RectTransform>().anchoredPosition = position;
    }

    void UpdateAIHandDisplay()
    {
        if(dummyIndex==1){
            leftPlayerHandDisplay.ShowHand(leftHand, true);
            topPlayerHandDisplay.ShowHand(topHand, false);
            rightPlayerHandDisplay.ShowHand(rightHand, false);
        }else if(dummyIndex==2){
            topPlayerHandDisplay.ShowHand(topHand, true);
            leftPlayerHandDisplay.ShowHand(leftHand, false);
            rightPlayerHandDisplay.ShowHand(rightHand, false);
        }else if(dummyIndex==3){
            rightPlayerHandDisplay.ShowHand(rightHand, true);
            topPlayerHandDisplay.ShowHand(topHand, false);
            leftPlayerHandDisplay.ShowHand(leftHand, false);
        }else{
            topPlayerHandDisplay.ShowHand(topHand, false);
            leftPlayerHandDisplay.ShowHand(leftHand, false);
            rightPlayerHandDisplay.ShowHand(rightHand, false);
        }
    }

    /// <summary>
    /// Czyści wszystkie karty ze stołu po zakończeniu lewy.
    /// </summary>

    void ClearTable()
    {
        isEndOfTurn= false;

        foreach (Transform child in tablePanel)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Stół został wyczyszczony.");
    }

    /// <summary>
    /// Klasa reprezentująca zagrane karty i przypisanych do nich graczy.
    /// </summary>

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

    /// <summary>
    /// Ocenia zwycięzcę lewy i przygotowuje następną turę.
    /// </summary>

    int trickNumber=0;
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
        tricksWonByPlayer[winningCard.playerIndex]++;
        currentPlayerIndex = startingPlayerIndex;
        currentTurn = (PlayerTurn)startingPlayerIndex;
        yield return new WaitForSeconds(2.0f);
        ClearTable();
        trickNumber++;
        leadingSuit = "";
        currentTrick.Clear();
        if (trickNumber >= 13)
        {
            Debug.Log("Wszystkie lewy rozegrane!");
            CalculateScore();
        }else{
            bool isDeclarer = (winningBidderIndex % 2 == 0);
            bool isDummy = currentPlayerIndex == dummyIndex;
            bool isDeclarerControllingDummy = isDummy && isDeclarer;
            bool isHumanPlaying = false;
            if(winningBidderIndex!=2){
                isHumanPlaying = (currentPlayerIndex == 0) || isDeclarerControllingDummy;
            }

            if (!isHumanPlaying)
            {
                StartCoroutine(PlayAICard());
            }
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

    public class Bid
    {
        public string playerName;
        public string call;

        public Bid(string name, string call)
        {
            playerName = name;
            this.call = call;
        }
    }

    public List<Bid> biddingHistory = new List<Bid>();
    public string[] possibleCalls = {
        "1♣", "1♦", "1♥", "1♠", "1NT",
        "2♣", "2♦", "2♥", "2♠", "2NT",
        "3♣", "3♦", "3♥", "3♠", "3NT",
        "4♣", "4♦", "4♥", "4♠", "4NT",
        "5♣", "5♦", "5♥", "5♠", "5NT",
        "6♣", "6♦", "6♥", "6♠", "6NT",
        "7♣", "7♦", "7♥", "7♠", "7NT"
    };

    private bool AllPlayersPassed()
    {
        int passCount = 0;
        foreach (var player in players)
        {
            if (player.currentBid == "Pas")
                passCount++;
        }
        return passCount >= 3;
    }
    public int highestBidIndex = -1;
    private string currentHighestBid=null;
    bool biddingInProgress = false;
    private IEnumerator StartBidding()
    {
        biddingInProgress = true;
        Debug.Log("Licytacja rozpoczęta.");

        int bidderIndex = 0;
        int passesInARow=0;

        while (biddingInProgress)
        {
            Player currentPlayer = players[bidderIndex];
            string bid = "";

            if (bidderIndex == 0)
            {
                BiddingUI.Instance.ShowBiddingPanel(currentHighestBid);
                yield return new WaitUntil(() => !string.IsNullOrEmpty(BiddingUI.Instance.GetSelectedBid()));
                bid = BiddingUI.Instance.GetSelectedBid();

                if (BiddingUI.Instance.currentBidText != null)
                {
                    BiddingUI.Instance.currentBidText.text = $"{currentPlayer.name}: {bid}";
                }
                yield return new WaitForSeconds(1.5f);
                if (bid=="Pas"){
                    passesInARow++;
                    if (passesInARow >= 3 && currentHighestBid != "")
                    {
                        biddingInProgress = false;
                        StartPlayPhase();
                        break;
                    }
                }else{
                    currentHighestBid = bid;
                    passesInARow = 0;

                    if (bid == "7NT")
                    {
                        biddingInProgress = false;
                        winningBidderIndex = bidderIndex;
                        StartPlayPhase();
                        break;
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
                bid = currentPlayer.MakeBid();
                if (BiddingUI.Instance.currentBidText != null)
                {
                    BiddingUI.Instance.currentBidText.text = $"{currentPlayer.name}: {bid}";
                }
                yield return new WaitForSeconds(1.5f);
                if(bid=="Pas"){
                    passesInARow++;
                    if (passesInARow >= 3 && currentHighestBid != "")
                    {
                        biddingInProgress = false;
                        StartPlayPhase();
                        break;
                    }
                }else{
                    currentHighestBid = bid;
                    passesInARow = 0;

                    if (bid == "7NT")
                    {
                        biddingInProgress = false;
                        winningBidderIndex = bidderIndex;
                        StartPlayPhase();
                        break;
                    } 
                 }
            }

            Debug.Log($"{bid}");
            currentPlayer.currentBid = bid;
            biddingHistory.Add(new Bid(currentPlayer.name, bid));

            int bidIndex = System.Array.IndexOf(possibleCalls, bid);
            if (bid != "Pas" && bidIndex > highestBidIndex)
            {
                highestBidIndex = bidIndex;
                winningBidderIndex= bidderIndex;
            }

            if (AllPlayersPassed())
            {
                biddingInProgress = false;
                StartPlayPhase();
            }

            bidderIndex = (bidderIndex + 1) % players.Count;
        }
        biddingInProgress = false;
        Debug.Log("Licytacja zakończona.");
    }

 public void EndBiddingEarly()
    {
        biddingInProgress = false;
        Debug.Log("Licytacja zakończona: 7NT");
    }

    private void StartPlayPhase()
    {
        isEndOfTurn = false;
        leadingSuit = "";
        trickNumber = 0;
        currentTrick.Clear();
        dummyIndex = (winningBidderIndex + 2) % 4;
        players[dummyIndex].IsAI = true;
        if(dummyIndex!=0){
            players[0].IsAI = false;
        }
        GetPlayableHand();
        Debug.Log("Faza rozgrywki rozpoczęta.");
        string winnerBidName= players[winningBidderIndex].name;
        BiddingUI.Instance.currentBidText.text = $"{winnerBidName}: {currentHighestBid}";

        currentPlayerIndex = winningBidderIndex;
        currentTurn = (PlayerTurn)winningBidderIndex;

        if (players[currentPlayerIndex].IsAI || 
            (currentTurn == (PlayerTurn)dummyIndex && currentPlayerIndex == winningBidderIndex))
        {
            StartCoroutine(PlayAICard());
        }
        else
        {
            Debug.Log("Czeka na ruch gracza: " + players[currentPlayerIndex].name);
        }
    }
    private int[] teamPoints = new int[2];
    private int[] tricksWonByPlayer = new int[4];
    private int[] pointsBelowLine = new int[2];
    private int[] pointsAboveLine = new int[2];
    private int[] points= new int[2];
    public TextMeshProUGUI pointsBelowText;
    public TextMeshProUGUI pointsAboveText;
    private void CalculateScore()
    {
        int winningTeam = winningBidderIndex % 2;
        int level = int.Parse(currentHighestBid.Substring(0, 1));
        int requiredTricks = level + 6;
        int tricksTaken = 0;
        string suit = currentHighestBid.Substring(1).ToUpper();
        for (int i = 0; i < 4; i++)
        {
            if (i % 2 == winningTeam)
                tricksTaken += tricksWonByPlayer[i];
        }

        if (tricksTaken >= requiredTricks)
        {
            int score = 0;

            switch (suit)
            {
                case "C":
                case "D":
                    score = 20 * level;
                    break;
                case "H":
                case "S":
                    score = 30 * level;
                    break;
                case "NT":
                    score = 40 + 30 * (level - 1);
                    break;
            }

            pointsBelowLine[winningTeam]+=score;
            teamPoints[winningTeam] += score;
            Debug.Log($"Zespół {winningTeam} zdobywa {score} punktów!");
        }
        else
        {
            int down = requiredTricks - tricksTaken;
            int penalty = down * 50;
            pointsAboveLine[1-winningTeam]+=penalty;
            teamPoints[1 - winningTeam] += penalty;
            Debug.Log($"Zespół {1 - winningTeam} zdobywa {penalty} punktów za niewykonanie kontraktu.");
        }
        UpdateScoreUI();
        if (pointsBelowLine[winningTeam] >= 100)
        {
            partsWon[winningTeam]++;

            if (partsWon[winningTeam] >= 2)
            {
                points[0]=pointsAboveLine[0]+pointsBelowLine[0];
                points[1]=pointsAboveLine[1]+pointsBelowLine[1];
                if(partsWon[1-winningTeam]==0){
                    points[winningTeam]+=750;
                }else{
                    points[winningTeam]+=500;
                }
                if(points[1-winningTeam]>points[winningTeam]){
                    Debug.Log($"Zespół {1-winningTeam} wygrywa cały mecz z wynikiem {(points[1-winningTeam]-points[winningTeam])/100}");
                    EndGame(1-winningTeam);
                }else{
                    Debug.Log($"Zespół {winningTeam} wygrywa cały mecz z wynikiem {(points[winningTeam]-points[1-winningTeam])/100}");
                    EndGame(winningTeam);
                }
                return;
            }
            
            pointsAboveLine[0] += pointsBelowLine[0];
            pointsAboveLine[1] += pointsBelowLine[1]; 
            pointsBelowLine[0] = 0;
            pointsBelowLine[1] = 0;
        }
        ResetForNextDeal();
    }

    private void UpdateScoreUI()
    {
        Debug.Log("UpdateScoreUI called");
        if (pointsBelowText != null)
            pointsBelowText.text = $"NS: {pointsBelowLine[0]}   EW: {pointsBelowLine[1]}";

        if (pointsAboveText != null)
            pointsAboveText.text = $"NS: {pointsAboveLine[0]}  EW: {pointsAboveLine[1]}";
    }
    private bool isPlayingDummy = false;
    public int dummyIndex = -1;
    List<string> GetPlayableHand()
    {
        if (dummyIndex == 2)
        {
            topPlayerHandDisplay.ShowHand(topHand, true);
        }else if(dummyIndex == 1){
            leftPlayerHandDisplay.ShowHand(leftHand, true);
        }else if(dummyIndex == 3){
            rightPlayerHandDisplay.ShowHand(rightHand, true);
        }else if(dummyIndex == 0){
            playerHandDisplay.ShowHand(playerHand, true);
        }
        if (isPlayingDummy)
            return players[dummyIndex].hand;
        else
            return players[(int)currentTurn].hand;
    }
    private void ResetForNextDeal()
    {
        Debug.Log("Przygotowanie nowego rozdania...");
        DealCards();
        foreach (var player in players)
        {
            player.currentBid = "";
        }
        biddingHistory.Clear();
        highestBidIndex = -1;
        currentHighestBid = null;
        tricksWonByPlayer = new int[4];
        dummyIndex = -1;
        currentTrick.Clear();
        trickNumber = 0;

        StartCoroutine(StartBidding());
    }
    private void EndGame(int winningTeam)
    {
        Debug.Log($"KONIEC GRY! Zespół {winningTeam} wygrywa cały mecz.");
        endGamePanel.SetActive(true);
        endGameText.text = $"Zespół {(winningTeam == 0 ? "NS" : "EW")} wygrywa cały mecz!";
    }
    public void RestartGame()
    {
        endGamePanel.SetActive(false);
        
        teamPoints = new int[2];
        pointsAboveLine = new int[2];
        pointsBelowLine = new int[2];
        partsWon = new int[2];
        UpdateScoreUI();

        ResetForNextDeal();
    }
}

public class Player
{
    public string name;
    public string currentBid = "";
    public bool IsAI;
    public List<string> hand = new List<string>();

    public Player(string name, bool isAI)
    {
        this.name = name;
        IsAI = isAI;
    }

    public string MakeBid()
    {
        List<string> validCalls = new List<string> { "Pas" };
        for (int i = GameManager.Instance.highestBidIndex + 1; i < GameManager.Instance.possibleCalls.Length; i++)
        {
            validCalls.Add(GameManager.Instance.possibleCalls[i]);
        }

        return validCalls[Random.Range(0, validCalls.Count)];
    }
}