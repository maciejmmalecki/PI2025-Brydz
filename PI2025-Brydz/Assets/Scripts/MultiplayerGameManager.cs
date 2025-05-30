using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Główna klasa zarządzająca grą w trybie multiplayer.
/// Odpowiada za rozdawanie kart, rozpoczęcie licytacji i fazy rozgrywki, obsługę tur, punktację i komunikację przez sieć z użyciem Mirror.
/// </summary>
public class MultiplayerGameManager : NetworkBehaviour
{
    public static MultiplayerGameManager Instance{ get; private set; }
    public List<NetworkPlayer> players = new();

    public MultiplayerHandDisplay playerHandDisplay, topPlayerHandDisplay, leftPlayerHandDisplay, rightPlayerHandDisplay;
    public GameObject MultiplayerCardPrefab;
    public Transform tablePanel;
    public GameObject endGamePanel;
    public TextMeshProUGUI endGameText;
    public TextMeshProUGUI pointsBelowText;
    public TextMeshProUGUI pointsAboveText;

    public enum PlayerTurn { Bottom, Left, Top, Right }
    public PlayerTurn currentTurn = PlayerTurn.Bottom;

    public List<string> playerHand = new();
    public List<string> topHand = new();
    public List<string> leftHand = new();
    public List<string> rightHand = new();

    public string leadingSuit = null;
    public List<PlayedCard> currentTrick = new();
    public int startingPlayerIndex;
    [SyncVar(hook = nameof(OnPlayerTurnChanged))]
    public int currentPlayerIndex;
    private int[] tricksWonByPlayer = new int[4];
    private int[] partsWon = new int[2];
    private int[] pointsBelowLine = new int[2];
    private int[] pointsAboveLine = new int[2];
    private int[] points = new int[2];

    private bool isEndOfTurn = false;
    [SyncVar]
    public int winningBidderIndex;
    [SyncVar]
    public int dummyIndex;
    public GameObject cardBackPrefab;

    public int highestBidIndex;
    private string currentHighestBid = null;
    private string trumpSuit = null;
    private bool biddingInProgress = false;
    private int startingBidderIndex;
    private BiddingData pendingBid = null;
    private int trickNumber;
    public int GetPoints(int teamIndex) => pointsBelowLine[teamIndex] + pointsAboveLine[teamIndex];
    public int GetTrickCount() => trickNumber;
    public List<string> GetLastPlayedCards() => lastTrick.ConvertAll(p => p.cardID);
    public List<string> GetLastPlayedCardsWithPlayers() => lastTrick.ConvertAll(p=> $"{players[p.playerIndex].name}: {p.cardID}").ToList();
    public int totalCardsPlayed;
    private List<PlayedCard> lastTrick = new();
    public int GetPointsAboveLine(int teamIndex) => pointsAboveLine[teamIndex];
    public int GetPointsBelowLine(int teamIndex) => pointsBelowLine[teamIndex];

    public string[] possibleCalls = {
        "1♣", "1♦", "1♥", "1♠", "1NT",
        "2♣", "2♦", "2♥", "2♠", "2NT",
        "3♣", "3♦", "3♥", "3♠", "3NT",
        "4♣", "4♦", "4♥", "4♠", "4NT",
        "5♣", "5♦", "5♥", "5♠", "5NT",
        "6♣", "6♦", "6♥", "6♠", "6NT",
        "7♣", "7♦", "7♥", "7♠", "7NT"
    };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if(pointsAboveText == null)
            pointsAboveText = GameObject.Find("PointsAboveLine")?.GetComponent<TextMeshProUGUI>();
        if(pointsBelowText == null)
            pointsBelowText = GameObject.Find("PointsBelowLine")?.GetComponent<TextMeshProUGUI>();

        CalculateScore();

    }

    private int lastPlayerCount = -1;

    void Update()
    {
        if (!isServer) return;
        int currentPlayerCount = NetworkServer.connections.Count;
        if (lastPlayerCount == -1)
        {
            lastPlayerCount = currentPlayerCount;
        }
        else if (currentPlayerCount < lastPlayerCount)
        {
            Debug.Log("Wykryto spadek liczby graczy");
            StartCoroutine(EndGameAfterDelay());
        }
        lastPlayerCount = currentPlayerCount;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        endGamePanel.SetActive(false);
        playerHandDisplay.LoadCardSprites();
        DealCards();
        StartCoroutine(StartBidding());
    }
    private void OnPlayerTurnChanged(int oldIndex, int newIndex){
        Debug.Log($"[SyncVar] currentPlayerIndex changed: {oldIndex} -> {newIndex}");
        currentTurn= (PlayerTurn)newIndex;
        if(isClient){
            RpcHighlightCurrentTurn(newIndex);
        }
    }

    /// <summary>
    /// Rozdaje wszystkim graczom po 13 kart i synchronizuje je przez sieć.
    /// </summary>
    [Server] public void DealCards()
    {
        List<string> deck = GenerateDeck();
        Shuffle(deck);

        playerHand = deck.GetRange(0, 13);
        leftHand = deck.GetRange(13, 13);
        topHand = deck.GetRange(26, 13);
        rightHand = deck.GetRange(39, 13);

        Debug.Log("Rozdano wszystkie karty");

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var conn = player.connectionToClient;

            var hand = GetHandByIndex(i).ToArray();

            if (conn != null)
            {
                player.TargetShowHand(conn, hand);
                player.TargetShowOpponentsAllBack(conn, i);
                Debug.Log($"Wysłano rękę do gracza {i}, 13 kart");
            }
            else if (player.isLocalPlayer)
            {
                var handDisplay = GameObject.Find("PlayerHand")?.GetComponent<HandDisplay>();
                if (handDisplay != null)
                {
                    handDisplay.ShowHand(new List<string>(hand), true);
                    players[i].TargetShowOpponents(conn, i);
                    Debug.Log($"(HOST) Pokazano rękę lokalnie dla gracza {i}");
                }
            }
            else
            {
                Debug.LogError($"Brak connectionToClient dla gracza {i}");
            }
        }
    }

    [ClientRpc] void RpcReceiveHand(int playerIndex, string[] hand)
    {
        GetHandByIndex(playerIndex).Clear();
        GetHandByIndex(playerIndex).AddRange(hand);
        switch (playerIndex)
        {
            case 0: playerHandDisplay.ShowHand(hand.ToList(), true); break;
            case 1: leftPlayerHandDisplay.ShowHand(hand.ToList(), false); break;
            case 2: topPlayerHandDisplay.ShowHand(hand.ToList(), false); break;
            case 3: rightPlayerHandDisplay.ShowHand(hand.ToList(), false); break;
        }
    }

    [ClientRpc] public void RpcForceShowDummy(string[] dummyHand, int dummyIndex)
    {
        Debug.Log($"[CLIENT RPC] Recznie pokazuje dummy {dummyIndex} - {dummyHand.Length} kart");
        var localPlayer = NetworkClient.connection.identity.GetComponent<NetworkPlayer>();
        MultiplayerGameManager.Instance.ShowHandForPlayer(dummyIndex, true, dummyHand.ToList());
    }

    List<string> GenerateDeck()
    {
        string[] suits = { "S", "H", "D", "C" };
        string[] values = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        List<string> deck = new();
        foreach (string suit in suits)
            foreach (string value in values)
                deck.Add(suit + value);
        return deck;
    }

    void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
    [Client]
    public void ShowHandForPlayer(int handOwnerIndex, bool faceUp, List<string> overrideCards= null)
    {
        Debug.Log($"Show hand for player {handOwnerIndex}, {faceUp}");
        var localPlayer = NetworkClient.connection.identity.GetComponent<NetworkPlayer>();
        if(localPlayer == null) return;

        int relativeIndex = (handOwnerIndex - localPlayer.playerIndex + 4) %4;
        string panelName = relativeIndex switch
        {
            0=>"PlayerHand",
            1=>"LeftHand",
            2=>"TopHand",
            3=>"RightHand",
            _ => null
        };
        var panel = GameObject.Find(panelName);
        if(panel==null){ 
            Debug.Log($"Nie znaleziono panelu {panelName}");
            return;
        }
        var display = panel.GetComponent<MultiplayerHandDisplay>();
        if(display == null){
            Debug.Log($"Panel {panelName} nie ma multiplayerhanddisplay");
            return;
        } 
        
        List<string> cards;
        if(overrideCards != null)
        {
            cards = overrideCards;
        }
        else if(faceUp)
        {
            var possibleHand = GetHandByIndex(handOwnerIndex);
            if(possibleHand == null || possibleHand.Count == 0)
            {
                Debug.Log("Proba pokazania pustej reki gracza");
                return;
            }
            cards = new List<string>(possibleHand);
        }
        else{
            cards= Enumerable.Repeat("BACK", 13).ToList();
        }
        display.ShowHand(cards, faceUp);
    }

    [Server] public void CmdPlayCard(string cardID, NetworkConnectionToClient conn)
    {
        Debug.Log($"📥 CmdPlayCard od connId={conn.connectionId}, card: {cardID}");
        var player = conn.identity.GetComponent<NetworkPlayer>();
        int playerIndex = player.playerIndex;
        bool isPlayingOwnTurn = playerIndex == currentPlayerIndex;
        bool isDummyTurn = currentPlayerIndex == dummyIndex && playerIndex == winningBidderIndex;

        if (!isPlayingOwnTurn && !isDummyTurn)
        {
            Debug.LogWarning($"Gracz {playerIndex} próbował zagrać nie w swojej turze.");
            return;
        }

        int actualPlayerIndex = currentPlayerIndex;
        var hand = GetHandByIndex(actualPlayerIndex);
        if (!hand.Contains(cardID)) return;

        if (string.IsNullOrEmpty(leadingSuit))
            leadingSuit = cardID.Substring(0, 1);

        if(!string.IsNullOrEmpty(leadingSuit)){
            bool hasLeadingSuit= hand.Exists(card => card.StartsWith(leadingSuit));
            if(hasLeadingSuit && cardID.Substring(0,1)!=leadingSuit){
                return;
            }
        }
        hand.Remove(cardID);
        currentTrick.Add(new PlayedCard(cardID, actualPlayerIndex));
        totalCardsPlayed++;
        RpcSpawnCardOnTable(cardID, actualPlayerIndex);
        if(actualPlayerIndex == dummyIndex){
            MultiplayerGameManager.Instance.RefreshOpponentsForAll();
            RpcUpdateDummyHand(GetHandByIndex(dummyIndex).ToArray());
        }else{
            player.TargetUpdatePlayerHand(conn, hand.ToArray());
        }
        Debug.Log($"[CmdPlayCard] Wywołuję AdvanceTurn z currentPlayerIndex={currentPlayerIndex}");
        AdvanceTurn();
    }

    [ClientRpc]
    public void RpcUpdateDummyHand(string[] dummyCards)
    {
        ShowHandForPlayer(dummyIndex, true, dummyCards.ToList());
    }

    [Server] void AdvanceTurn()
    {
        isEndOfTurn=false;
        Debug.Log($"[AdvanceTurn] Obecny gracz: {currentPlayerIndex}");
        currentPlayerIndex = (currentPlayerIndex + 1) % 4;
        currentTurn = (PlayerTurn)currentPlayerIndex;
        Debug.Log($"[AdvanceTurn] Następny gracz: {currentPlayerIndex}");
        if (currentTrick.Count >= 4)
        {
            leadingSuit = null;
            isEndOfTurn = true;
            StartCoroutine(EvaluateTrickWinner());
            return;
        }
        RpcHighlightCurrentTurn(currentPlayerIndex);
    }

    [ClientRpc] void RpcSpawnCardOnTable(string cardID, int serverPlayerIndex)
    {
        Debug.Log($"[RpcSpawnCardOnTable] Gracz {serverPlayerIndex} zagrał {cardID}");

        GameObject card = Instantiate(MultiplayerCardPrefab, tablePanel);
        var ui= card.GetComponent<MultiplayerCardUI>();
        ui.cardID= cardID;
        var localPlayer = NetworkClient.connection?.identity?.GetComponent<NetworkPlayer>();
        int localIndex = localPlayer != null ? localPlayer.playerIndex:-1;

        int relativeIndex = (serverPlayerIndex - localIndex + 4) % 4;

        Vector2 pos = relativeIndex switch
        {
            0 => new Vector2(-35, -70),
            1 => new Vector2(-250, 0),
            2 => new Vector2(-35, 70),
            3 => new Vector2(200, 0),
            _ => Vector2.zero
        };
        
        bool isDummyCard= serverPlayerIndex==dummyIndex;
        bool isWinningPlayer= localIndex==winningBidderIndex;
        if((serverPlayerIndex==localIndex)||(isDummyCard && isWinningPlayer))
        {
            card.GetComponent<Button>().onClick.AddListener(ui.OnCardClicked);
        }
        var allDisplays = FindObjectsOfType<MultiplayerHandDisplay>();
        Dictionary<string, Sprite> spriteDict = null;
        foreach (var d in allDisplays)
        {
            if(d.cardSpriteDict.Count>0){
                spriteDict = d.cardSpriteDict;
                break;
            }
        }
        if (spriteDict.TryGetValue(cardID, out Sprite sprite))
        {
            card.GetComponent<Image>().sprite = sprite;
        }
        else
        {
            Debug.LogWarning($"Nie znaleziono sprite'a dla {cardID}");
        }

        card.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    [TargetRpc]
    void TargetUpdatePlayerHand(NetworkConnection target, string[] hand)
    {
        Debug.Log($"[TargetUpdatePlayerHand] pokazuję {hand.Length} kart");

        var panel = GameObject.Find("PlayerHand");
        if (panel != null)
        {
            var display = panel.GetComponent<MultiplayerHandDisplay>();
            display.ShowHand(new List<string>(hand), true);
        }
    }

    [ClientRpc] void RpcHighlightCurrentTurn(int playerIndex)
    {
        Debug.Log("Teraz gra gracz: " + playerIndex);
    }

    public bool CanPlayCard()
    {
        if(isEndOfTurn){
            return false;
        }
        var identity = NetworkClient.connection?.identity;
        if (identity == null) return false;

        var localPlayer = identity.GetComponent<NetworkPlayer>();
        if (localPlayer == null) return false;

        Debug.Log($"[CanPlayCard] Local playerIndex = {localPlayer.playerIndex}, currentPlayerIndex = {currentPlayerIndex}");

        if (currentPlayerIndex==dummyIndex)
        {
            return localPlayer.playerIndex==winningBidderIndex;
        }

        return localPlayer.playerIndex == currentPlayerIndex;
    }

    /// <summary>
    /// Zwraca aktualną rękę gracza na podstawie jego indeksu.
    /// </summary>
    public List<string> GetHandByIndex(int index) => index switch
    {
        0 => playerHand,
        1 => leftHand,
        2 => topHand,
        3 => rightHand,
        _ => null
    };

    /// <summary>
    /// Logika wykonywana po zagraniu karty przez gracza.
    /// Sprawdza kompletność lewy, aktualizuje stan gry i punktację.
    /// </summary>
    [Server]
    IEnumerator EvaluateTrickWinner()
    {
        yield return new WaitForSeconds(1.5f); // krótka pauza zanim ocenisz lewę

        while (currentTrick.Count < 4)
        {
            Debug.Log("Nie można ocenić lewy — liczba kart != 4.");
            yield return null;
        }

        string leadSuit = currentTrick[0].cardID.Substring(0, 1);
        PlayedCard winningCard = currentTrick[0];
        int highestValue = GetCardValue(winningCard.cardID);
        string winningSuit = leadSuit;

        foreach (var played in currentTrick)
        {
            string suit = played.cardID.Substring(0, 1);
            int value = GetCardValue(played.cardID);
            bool isTrump = (trumpSuit != null && suit == trumpSuit);
            bool winningIsTrump = (trumpSuit != null && winningSuit == trumpSuit);

            if (isTrump && !winningIsTrump)
            {
                winningCard = played;
                highestValue = value;
                winningSuit = suit;
            }
            else if (suit == winningSuit && value > highestValue)
            {
                winningCard = played;
                highestValue = value;
            }
        }
        RpcClearTable();
        Debug.Log($"Lewę wygrywa gracz {winningCard.playerIndex}");

        tricksWonByPlayer[winningCard.playerIndex]++;
        lastTrick = new List<PlayedCard>(currentTrick);
        currentTrick.Clear();
        leadingSuit = null;
        currentPlayerIndex = winningCard.playerIndex;
        currentTurn = (PlayerTurn)currentPlayerIndex;
        RpcHighlightCurrentTurn(currentPlayerIndex);
        isEndOfTurn = false;
        trickNumber++;

        if (totalCardsPlayed >= 52)
        {
            CalculateScore();
        }
        RpcSendStatsToClients(
            biddingHistory.Select(b => $"{b.playerName}: {b.call}").ToArray(),
            lastTrick.Select(p => $"Gracz {p.playerIndex}: {p.cardID}").ToArray(),
            GetDefendersTricks(),
            pointsBelowLine[0], pointsAboveLine[0],
            pointsBelowLine[1], pointsAboveLine[1]
        );
    }
    int GetCardValue(string cardID)
    {
        string valuePart = cardID.Substring(1);
        return valuePart switch
        {
            "2" => 2,
            "3" => 3,
            "4" => 4,
            "5" => 5,
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => 0
        };
    }

    [Server] void CalculateScore()
    {
        int winningTeam = winningBidderIndex % 2;
        int level = int.Parse(currentHighestBid.Substring(0, 1));
        int requiredTricks = level + 6;
        int tricksTaken = 0;
        string rawSuit = currentHighestBid.Substring(1);
        string suit = rawSuit switch
        {
            "♣" => "C",
            "♦" => "D",
            "♥" => "H",
            "♠" => "S",
            "NT" => "NT",
            _ => rawSuit
        };
        
        for (int i = 0; i < 4; i++)
            if (i % 2 == winningTeam) tricksTaken += tricksWonByPlayer[i];

        if (tricksTaken >= requiredTricks)
        {
            int score = suit switch
            {
                "C" or "D" => 20 * level,
                "H" or "S" => 30 * level,
                "NT" => 40 + 30 * (level - 1),
                _ => 0
            };
            pointsBelowLine[winningTeam] += score;
            int overtricks = tricksTaken - requiredTricks;
            int overtrickPoints = (suit == "C" || suit == "D") ? 20 * overtricks : 30 * overtricks;
            pointsAboveLine[winningTeam] += overtrickPoints;
            if (level == 6) pointsAboveLine[winningTeam] += 500;
            if (level == 7) pointsAboveLine[winningTeam] += 1000;
        }
        else
        {
            int down = requiredTricks - tricksTaken;
            int penalty = down * 50;
            pointsAboveLine[1 - winningTeam] += penalty;
        }

        UpdateScoreUI();
        RpcUpdateScoreUI(pointsBelowLine[0], pointsBelowLine[1], pointsAboveLine[0], pointsAboveLine[1]);
        RpcSendStatsToClients(
            biddingHistory.Select(b => $"{b.playerName}: {b.call}").ToArray(),
            lastTrick.Select(p => $"Gracz {p.playerIndex}: {p.cardID}").ToArray(),
            GetDefendersTricks(),
            pointsBelowLine[0], pointsAboveLine[0],
            pointsBelowLine[1], pointsAboveLine[1]
        );

        if (pointsBelowLine[winningTeam] >= 100)
        {
            partsWon[winningTeam]++;
            if (partsWon[winningTeam] >= 2)
            {
                points[0] = pointsBelowLine[0] + pointsAboveLine[0];
                points[1] = pointsBelowLine[1] + pointsAboveLine[1];
                points[winningTeam] += partsWon[1 - winningTeam] == 0 ? 750 : 500;
                int winner = points[0] > points[1] ? 0 : 1;
                EndGame(winner);
                return;
            }
            pointsAboveLine[0] += pointsBelowLine[0];
            pointsAboveLine[1] += pointsBelowLine[1];
            pointsBelowLine[0] = 0;
            pointsBelowLine[1] = 0;
        }
        EndPlayPhase();
    }

    [ClientRpc] void RpcUpdateScoreUI(int belowNS, int belowEW, int aboveNS, int aboveEW)
    {
        if (pointsBelowText != null) pointsBelowText.text = $"NS: {belowNS}   EW: {belowEW}";
        if (pointsAboveText != null) pointsAboveText.text = $"NS: {aboveNS}  EW: {aboveEW}";
    }

    void EndPlayPhase()
    {
        RpcClearTable();
        trickNumber = 0;
        lastTrick.Clear();
        currentTrick.Clear();
        for (int i = 0; i < 4; i++) tricksWonByPlayer[i] = 0;
        ResetForNextDeal();
    }

    [ClientRpc] void RpcClearTable()
    {
        foreach (Transform child in tablePanel)
            Destroy(child.gameObject);
    }

    [Server] public void ServerRestartMatch()
    {
        endGamePanel.SetActive(false);
        points = new int[2];
        pointsAboveLine = new int[2];
        pointsBelowLine = new int[2];
        partsWon = new int[2];

        RestartGame();
        RpcRestartGame();
        ResetForNextDeal();
    }

    [ClientRpc] void RpcResetScoreUI()
    {
        pointsBelowText.text = "NS: 0   EW: 0";
        pointsAboveText.text = "NS: 0   EW: 0";
    }

    void EndGame(int winningTeam)
    {
        endGamePanel.SetActive(true);
        endGameText.text = $"Team {(winningTeam == 0 ? "NS" : "EW")} won!";
    }

    public class PlayedCard
    {
        public string cardID;
        public int playerIndex;
        public PlayedCard(string id, int idx) { cardID = id; playerIndex = idx; }
    }

    [Server] public void ReceiveBidFromPlayer(BiddingData bid, NetworkPlayer sender)
    {
        int bidderIndex = players.IndexOf(sender);
        if (bidderIndex != currentPlayerIndex) return;

        string bidStr = bid.ToString();
        biddingHistory.Add(new Bid("Gracz " + bidderIndex, bidStr));
        RpcAnnounceBid(bidderIndex, bidStr);

        int bidIndex = System.Array.IndexOf(possibleCalls, bidStr);
        if (bidStr != "Pas" && bidIndex > highestBidIndex)
        {
            highestBidIndex = bidIndex;
            currentHighestBid = bidStr;
            winningBidderIndex = bidderIndex;
        }

        pendingBid = bid;
    }

    [ClientRpc] void RpcAnnounceBid(int bidderIndex, string bidStr)
    {
        MultiplayerBiddingUI ui = FindObjectOfType<MultiplayerBiddingUI>();
        if (ui != null)
            ui.UpdateCurrentBidText($"Gracz {bidderIndex}: {bidStr}");
    }

    /// <summary>
    /// Inicjuje fazę licytacji i zarządza przebiegiem licytacji.
    /// </summary>
    public IEnumerator StartBidding()
    {
        Debug.Log("Liczba graczy: " + players.Count);
        if (players.Count < 4)
        {
            Debug.Log("Zbyt mało graczy – licytacja nie może się rozpocząć.");
            yield break;
        }
        biddingInProgress = true;
        int passesInARow = 0;
        int bidderIndex = startingBidderIndex;

        var ui = FindObjectOfType<MultiplayerBiddingUI>();
        if (ui != null)
        {
            ui.ResetBiddingUI();
        }

        while (biddingInProgress)
        {
            currentPlayerIndex= bidderIndex;
            pendingBid = null;
            NetworkPlayer bidder = players[bidderIndex];
            ShowOwnHand();
            if (bidder.connectionToClient == null)
            {
                Debug.Log("Brak connectionToClient u gracza " + bidderIndex);
                yield break;
            }
            bidder.TargetStartBidding(bidder.connectionToClient, currentHighestBid);

            yield return new WaitUntil(() => pendingBid != null);

            string bidStr = pendingBid.ToString();

            if (bidStr == "Pas")
            {
                passesInARow++;
                if (passesInARow >= 3 && !string.IsNullOrEmpty(currentHighestBid))
                {
                    biddingInProgress = false;
                    RpcAnnounceFinalContract(winningBidderIndex, currentHighestBid);
                    StartPlayPhase();
                    break;
                }
            }
            else
            {
                passesInARow = 0;
                if (bidStr == "7NT")
                {
                    biddingInProgress = false;
                    winningBidderIndex = bidderIndex;
                    RpcAnnounceFinalContract(winningBidderIndex, currentHighestBid);
                    StartPlayPhase();
                    break;
                }
            }

            bidderIndex = (bidderIndex + 1) % players.Count;
            currentPlayerIndex = bidderIndex;
            RpcSendStatsToClients(
                biddingHistory.Select(b => $"{b.playerName}: {b.call}").ToArray(),
                lastTrick.Select(p => $"Gracz {p.playerIndex}: {p.cardID}").ToArray(),
                GetDefendersTricks(),
                pointsBelowLine[0], pointsAboveLine[0],
                pointsBelowLine[1], pointsAboveLine[1]
            );
        }
    }
    void ShowOwnHand()
    {
        var connId = NetworkClient.connection.connectionId;
        var player = players.FirstOrDefault(p => p.connectionToClient?.connectionId == connId);

        if (player != null)
        {
            var hand = GetHandByIndex(player.playerIndex).ToArray();
            player.TargetShowHand(player.connectionToClient, hand);
        }
    }

    /// <summary>
    /// Rozpoczyna właściwą fazę gry po zakończonej licytacji.
    /// Ustawia rozgrywającego, dummy i inicjuje pierwszą turę.
    /// </summary>
    void StartPlayPhase()
    {
        isEndOfTurn = false;
        leadingSuit = "";
        trickNumber = 0;
        lastTrick.Clear();
        currentTrick.Clear();
        dummyIndex = (winningBidderIndex + 2) % 4;
        totalCardsPlayed = 0;

        string suitSymbol = currentHighestBid.Substring(1);
        trumpSuit = suitSymbol switch
        {
            "♣"=>"C",
            "♦"=>"D",
            "♥"=>"H",
            "♠"=>"S",
            "NT"=>null,
            _ => null
        };
        currentPlayerIndex = (winningBidderIndex + 1) % 4;
        currentTurn = (PlayerTurn)currentPlayerIndex;
        StartCoroutine(DelayedHighlightTurn(currentPlayerIndex));
        RpcForceShowDummy(GetHandByIndex(dummyIndex).ToArray(), dummyIndex);
        StartCoroutine(DelayedShowOpponentsForAll());
    }

    private IEnumerator DelayedShowOpponentsForAll()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (var kvp in NetworkServer.connections)
        {
            NetworkConnectionToClient conn = kvp.Value;
            if(conn.identity != null)
            {
                var np=conn.identity.GetComponent<NetworkPlayer>();
                var hand= GetHandByIndex(np.playerIndex).ToArray();
                np.TargetShowHand(conn, hand);
                np.TargetShowOpponents(conn, np.playerIndex);
            }
        }
    }
    public void RefreshOpponentsForAll()
    {
        StartCoroutine(DelayedOpponentRefresh());
    }

    private IEnumerator DelayedOpponentRefresh()
    {
        yield return new WaitForSeconds(0.25f); // pozwól na SyncVar

        foreach (var p in players)
        {
            if (p.connectionToClient != null)
            {
                p.TargetShowOpponents(p.connectionToClient, p.playerIndex);
            }
        }
    }
    private IEnumerator DelayedHighlightTurn(int playerIndex)
    {
        yield return new WaitForSeconds(0.3f);
        RpcHighlightCurrentTurn(playerIndex);
    }
    void RestartGame()
    {
        Debug.Log("RestartGame wywołane — czyszczenie danych");
    }
    [ClientRpc]
    void RpcRestartGame()
    {
        UpdateScoreUI();
    }
    void ResetForNextDeal()
    {
        Debug.Log("Nowe rozdanie...");
        biddingHistory.Clear();
        currentHighestBid = null;
        highestBidIndex = -1;
        pendingBid = null;
        biddingInProgress = false;
        startingBidderIndex = (startingBidderIndex+1)%4;
        lastTrick.Clear();
        currentTrick.Clear();
        trickNumber = 0;
        tricksWonByPlayer = new int[4];
        var ui = FindObjectOfType<MultiplayerBiddingUI>();
        if(ui != null)
        {
            ui.ResetBiddingUI();
        }
        
        DealCards();
        StartCoroutine(StartBidding());
    }

    [ClientRpc]
    void UpdateScoreUI()
    {
        if (pointsBelowText != null)
            pointsBelowText.text = $"NS: {pointsBelowLine[0]}   EW: {pointsBelowLine[1]}";

        if (pointsAboveText != null)
            pointsAboveText.text = $"NS: {pointsAboveLine[0]}   EW: {pointsAboveLine[1]}";
    }

    Vector2 GetCardPosition(int playerIndex, int cardIndex)
    {
        float spacing = 25f;
        Vector2 basePos;

        switch (playerIndex)
        {
            case 1:
                basePos = new Vector2(-300, 0);
                return basePos + new Vector2(0, cardIndex * spacing);
            case 2:
                basePos = new Vector2(-150 + cardIndex * spacing, 150);
                return basePos;
            case 3:
                basePos = new Vector2(300, 0);
                return basePos + new Vector2(0, cardIndex * spacing);
            default:
                return Vector2.zero;
        }
    }

    [ClientRpc]
    public void RpcAnnounceFinalContract(int winningBidderIndex, string bid)
    {
        var ui =FindObjectOfType<MultiplayerBiddingUI>();
        if(ui != null)
        {
            ui.ShowFinalContract(winningBidderIndex, bid);
        }
    }

    public int GetDefendersTricks()
    {
        int defendersTricks = 0;
        int defendersTeam = (winningBidderIndex + 1)%2;

        for(int i =0; i<4; i++)
        {
            if(i%2 == defendersTeam)
            {
                defendersTricks += tricksWonByPlayer[i];
            }
        }
        return defendersTricks;
    }

    [ClientRpc]
    public void RpcSendStatsToClients(string[] bids, string[] lastTrick, int tricksGiven, int nsBelow, int nsAbove, int ewBelow, int ewAbove)
    {
        var statsUI = FindObjectOfType<MatchStatsUI>();
        if(statsUI != null)
        {
            statsUI.ReceiveStatsFromServer(bids.ToList(), lastTrick.ToList(), tricksGiven, nsBelow, nsAbove, ewBelow, ewAbove);
        }
    }

    [TargetRpc]
    public void TargetReceiveStats(NetworkConnection target, string[] bids, string[] lastTrick, int tricksGiven, int nsBelow, int nsAbove, int ewBelow, int ewAbove)
    {
        var statsUI = FindObjectOfType<MatchStatsUI>();
        if(statsUI != null)
        {
            statsUI.ReceiveStatsFromServer(bids.ToList(), lastTrick.ToList(), tricksGiven, nsBelow, nsAbove, ewBelow, ewAbove);
        }
    }
    [Server]
    public void SendStatsToSingleClient(NetworkConnection conn)
    {
        TargetReceiveStats(conn,
            biddingHistory.Select(b => $"{b.playerName}: {b.call}").ToArray(),
            lastTrick.Select(p => $"{players[p.playerIndex].name}: {p.cardID}").ToArray(),
            GetDefendersTricks(),
            pointsBelowLine[0], pointsAboveLine[0],
            pointsBelowLine[1], pointsAboveLine[1]
        );
    }

    private IEnumerator EndGameAfterDelay()
    {
        foreach (var p in players)
        {
            if (p != null && p.connectionToClient != null)
            {
                p.TargetShowPopup(p.connectionToClient, "Gracz opuścił grę. Gra zakończy się za chwilę.");
            }
        }
        yield return new WaitForSeconds(3f);

        if (isServer)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();

        SceneManager.LoadScene("StartScene"); // Zmień na swoją nazwę sceny początkowej
    }

    public void OnPlayerDisconnected(NetworkConnectionToClient conn)
    {
        Debug.Log("OnPlayerDisconnected wywolane");
        StartCoroutine(EndGameAfterDelay());
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

    public List<Bid> biddingHistory = new();
}