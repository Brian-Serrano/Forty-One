using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerManager : MonoBehaviour
{
    public List<TMP_Text> playersNameMatchmaking;
    public List<TMP_Text> playersName;
    public ConfigHandler configHandler;
    public GameObject cardPrefab;
    public RectTransform deckParent;
    public RectTransform discardedParent;
    public List<RectTransform> playerParents;
    public Sprite faceDownCardSprite;
    public Canvas canvas;
    public Animator crossfade;

    public Transform matchmakingPanel;
    public Transform resultPanel;
    public List<TMP_Text> playersScoreResult;
    public TMP_Text winnerTxt;
    public Transform disconnectedPanel;
    public Transform pausePanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    public AudioSource backgroundMusic;
    public AudioSource cardSfx;
    public AudioSource buttonClickSfx;

    public AudioClip cardSfxClip;

    public AudioMixer audioMixer;

    private SocketManager socketManager;
    private PlayerData playerData;
    private GameState gameState;
    private Dictionary<int, int> playerMapping;
    private List<int> disconnectedPlayerIds;
    private Queue<GameEvent> gameEvents;
    private ToastManager toastManager;
    private InterstitialAdManager interstitialAdManager;

    private int playerId;
    private int activeAnimations = 0;
    private bool isProcessing = false;

    class GameEvent
    {
        public string message;
        public SocketIOResponse response;

        public GameEvent(string message, SocketIOResponse response)
        {
            this.message = message;
            this.response = response;
        }
    }

    [System.Serializable]
    class RoomPlayersData
    {
        public int room_id;
        public List<string> players;
    }

    [System.Serializable]
    class StartGameData
    {
        public List<string> cards;
        public int player_turn;
        public List<Player> players;
        public string turn_type;

        [System.Serializable]
        public class Player
        {
            public int id;
            public string name;
            public List<string> cards;
        }
    }

    [System.Serializable]
    class DrawAction
    {
        public string action;
        public string turn_type;
        public int player_turn;
        public int player_id;

        public DrawAction(string action, string turn_type, int player_turn, int player_id)
        {
            this.action = action;
            this.turn_type = turn_type;
            this.player_turn = player_turn;
            this.player_id = player_id;
        }
    }

    [System.Serializable]
    class DiscardAction
    {
        public string card;
        public string turn_type;
        public int player_turn;
        public int player_id;

        public DiscardAction(string card, string turn_type, int player_turn, int player_id)
        {
            this.card = card;
            this.turn_type = turn_type;
            this.player_turn = player_turn;
            this.player_id = player_id;
        }
    }

    [System.Serializable]
    class WinData
    {
        public int player_id;
        public int winner;
        public string card;
        public List<Result> result;
    }

    [System.Serializable]
    class DisconnectData
    {
        public int room_id;
        public int player_id;
        public int player_turn;
        public string turn_type;
        public bool players_turn_that_disconnected;
    }

    [System.Serializable]
    class OnePlayerWinData
    {
        public int winner;
        public List<Result> result;
    }

    [System.Serializable]
    class Result
    {
        public int score;
        public int id;
        public string name;
    }

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        gameState = GameState.MATCHMAKING;
        playerMapping = new Dictionary<int, int>();
        disconnectedPlayerIds = new List<int>();
        gameEvents = new Queue<GameEvent>();
        toastManager = GetComponent<ToastManager>();
        interstitialAdManager = InterstitialAdManager.GetInstance();

        BannerAdManager.GetInstance().EnsureBannerVisible();

        socketManager = SocketManager.GetInstance(playerData.playersName[0]);
    }

    private async void Start()
    {
        matchmakingPanel.gameObject.SetActive(true);
        matchmakingPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

        foreach (TMP_Text playerName in playersNameMatchmaking)
        {
            playerName.text = "";
        }

        UpdateMusicVolume();
        UpdateSfxVolume();

        socketManager.onServerEvent += HandleServerEvent;

        if (!socketManager.socket.Connected)
        {
            await socketManager.socket.ConnectAsync();
        }
    }

    private void HandleServerEvent(string eventName, SocketIOResponse response)
    {
        gameEvents.Enqueue(new GameEvent(eventName, response));
    }

    private void Update()
    {
        if (!isProcessing && gameEvents.Count > 0)
        {
            StartCoroutine(ProcessEvents(gameEvents.Dequeue()));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (gameState)
            {
                case GameState.MATCHMAKING:
                    HomeButton();
                    break;
                case GameState.PLAYING:
                    Pause();
                    break;
                case GameState.PAUSE:
                    HomeButton();
                    break;
                case GameState.RESULT:
                    HomeButton();
                    break;
            }
        }
    }

    private IEnumerator ProcessEvents(GameEvent gameEvent)
    {
        isProcessing = true;

        switch (gameEvent.message)
        {
            case "room_players":
                RoomPlayersData roomPlayersData = gameEvent.response.GetValue<RoomPlayersData>();

                for (int i = 0; i < playersNameMatchmaking.Count; i++)
                {
                    playersNameMatchmaking[i].text = roomPlayersData.players.Count > i ? roomPlayersData.players[i] : "";
                }

                break;
            case "player_index":
                int id = gameEvent.response.GetValue<int>();

                playerId = id;

                playerMapping = Enumerable.Range(0, 4)
                    .Select(x => PositiveMod(x - playerId, 4))
                    .Select((value, index) => new { index, value })
                    .ToDictionary(p => p.index, p => p.value);

                break;
            case "start_game":
                StartGameData startGameData = gameEvent.response.GetValue<StartGameData>();

                playerData.multiplayerGamesPlayed++;
                playerData.SaveData();

                gameState = GameState.PLAYING;

                yield return HandleStartGame(startGameData);

                break;
            case "after_draw_card":
                DrawAction drawAction = gameEvent.response.GetValue<DrawAction>();

                yield return HandleDrawCard(drawAction);

                break;
            case "after_discard_card":
                DiscardAction discardAction = gameEvent.response.GetValue<DiscardAction>();

                yield return HandleDiscardCard(discardAction);

                break;
            case "win":
                WinData winData = gameEvent.response.GetValue<WinData>();

                yield return HandleWin(winData);

                break;
            case "disconnect_on_game":
                DisconnectData disconnectData = gameEvent.response.GetValue<DisconnectData>();

                if (disconnectData.player_id != playerId)
                {
                    playersName[playerMapping[disconnectData.player_id]].color = Color.gray;

                    disconnectedPlayerIds.Add(disconnectData.player_id);

                    if (disconnectData.players_turn_that_disconnected)
                    {
                        OnPlayerTurn(disconnectData.player_turn, disconnectData.turn_type);
                    }

                    toastManager.ShowToast(playersName[playerMapping[disconnectData.player_id]].text + " Disconnected");
                }

                break;
            case "one_player_win":
                OnePlayerWinData onePlayerWinData = gameEvent.response.GetValue<OnePlayerWinData>();

                yield return Win(onePlayerWinData.winner, onePlayerWinData.result);

                break;
            case "disconnected":
                if (gameState == GameState.PLAYING || gameState == GameState.PAUSE)
                {
                    yield return DisconnectPlayer();
                }

                break;
        }

        isProcessing = false;
    }

    private int PositiveMod(int value, int modulus)
    {
        return ((value % modulus) + modulus) % modulus;
    }

    private async void OnApplicationQuit()
    {
        await DisconnectSocket();

        socketManager.onServerEvent -= HandleServerEvent;
    }

    private Task DisconnectSocket()
    {
        if (socketManager.socket != null && socketManager.socket.Connected)
        {
            return socketManager.socket.DisconnectAsync();
        }

        return Task.CompletedTask;
    }

    private CardData FindCard(string serverCard)
    {
        foreach (CardData data in configHandler.deck)
        {
            if (data.rank == serverCard[..^1] && data.suit[..1] == serverCard[^1..])
            {
                return data;
            }
        }

        return null;
    }

    private CardInfo FindCard(string serverCard, int playerTurn)
    {
        for (int i = 0; i < playerParents[playerMapping[playerTurn]].childCount; i++)
        {
            CardInfo cardInfo = playerParents[playerMapping[playerTurn]].GetChild(i).GetComponent<CardInfo>();

            if (cardInfo.rank == serverCard[..^1] && cardInfo.suit[..1] == serverCard[^1..])
            {
                return cardInfo;
            }
        }

        return null;
    }

    private IEnumerator HandleStartGame(StartGameData startGameData)
    {
        matchmakingPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);

        yield return new WaitForSeconds(0.2f);

        matchmakingPanel.gameObject.SetActive(false);

        for (int i = 0; i < startGameData.players.Count; i++)
        {
            playersName[playerMapping[i]].text = startGameData.players[i].name;
        }

        foreach (string card in startGameData.cards)
        {
            GameObject instance = Instantiate(cardPrefab, deckParent);

            CardInfo cardInfo = instance.GetComponent<CardInfo>();

            CardData cardData = FindCard(card);

            cardInfo.Initialize(cardData.suit, cardData.value, cardData.sprite, cardData.rank);

            UpdateFace(cardInfo);
        }

        for (int i = 0; i < startGameData.players.Count; i++)
        {
            for (int j = 0; j < startGameData.players[i].cards.Count; j++)
            {
                GameObject instance = Instantiate(cardPrefab, canvas.transform);

                instance.GetComponent<RectTransform>().position = deckParent.position;

                instance.transform.SetSiblingIndex(canvas.transform.childCount - 7);

                CardInfo cardInfo = instance.GetComponent<CardInfo>();

                CardData cardData = FindCard(startGameData.players[i].cards[j]);

                cardInfo.Initialize(cardData.suit, cardData.value, cardData.sprite, cardData.rank);

                cardSfx.PlayOneShot(cardSfxClip);

                cardInfo.isFaceUp = i == playerId;

                UpdateFace(cardInfo);

                yield return MoveToTarget(playerParents[playerMapping[i]].position, instance.GetComponent<RectTransform>());

                instance.transform.SetParent(playerParents[playerMapping[i]]);
            }
        }

        OnPlayerTurn(startGameData.player_turn, startGameData.turn_type);
    }

    private IEnumerator HandleDrawCard(DrawAction drawAction)
    {
        string action = drawAction.action;

        if (action == "draw_from_deck")
        {
            if (deckParent.childCount > 0)
            {
                RectTransform card = deckParent.GetChild(deckParent.childCount - 1).GetComponent<RectTransform>();

                CardInfo cardInfo = card.GetComponent<CardInfo>();

                cardSfx.PlayOneShot(cardSfxClip);

                cardInfo.isFaceUp = drawAction.player_id == playerId;

                UpdateFace(cardInfo);

                card.SetParent(canvas.transform);
                card.SetSiblingIndex(canvas.transform.childCount - 7);

                yield return MoveToTarget(playerParents[playerMapping[drawAction.player_id]].position, card);

                card.SetParent(playerParents[playerMapping[drawAction.player_id]]);
            }
        }

        if (action == "draw_discarded")
        {
            if (discardedParent.childCount > 0)
            {
                RectTransform card = discardedParent.GetChild(discardedParent.childCount - 1).GetComponent<RectTransform>();

                CardInfo cardInfo = card.GetComponent<CardInfo>();

                cardSfx.PlayOneShot(cardSfxClip);

                cardInfo.isFaceUp = drawAction.player_id == playerId;

                UpdateFace(cardInfo);

                card.SetParent(canvas.transform);
                card.SetSiblingIndex(canvas.transform.childCount - 7);

                yield return MoveToTarget(playerParents[playerMapping[drawAction.player_id]].position, card);

                card.SetParent(playerParents[playerMapping[drawAction.player_id]]);
            }
        }

        OnPlayerTurn(drawAction.player_turn, drawAction.turn_type);
    }

    private IEnumerator HandleDiscardCard(DiscardAction discardAction)
    {
        yield return DiscardCard(discardAction.card, discardAction.player_id);

        OnPlayerTurn(discardAction.player_turn, discardAction.turn_type);
    }

    private IEnumerator HandleWin(WinData winData)
    {
        yield return DiscardCard(winData.card, winData.player_id);

        yield return Win(winData.winner, winData.result);
    }

    private IEnumerator Win(int winnerIndex, List<Result> results)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;

        FaceUpAllCards();

        backgroundMusic.Stop();

        gameState = GameState.RESULT;

        Time.timeScale = 0f;

        winnerTxt.text = results.Find(r => r.id == winnerIndex).name + " Wins";

        if (winnerIndex == playerId)
        {
            playerData.multiplayerGamesWon++;
            playerData.SaveData();
        }

        foreach (Result result in results)
        {
            playersScoreResult[result.id].text = $"{result.name}: {result.score}";
        }

        var task = DisconnectSocket();

        yield return new WaitUntil(() => task.IsCompleted);

        socketManager.onServerEvent -= HandleServerEvent;

        yield return new WaitForSecondsRealtime(3f);

        toastManager.PauseToasts();

        interstitialAdManager.ShowInterstitial(() =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                toastManager.ResumeToasts();

                crossfade.GetComponent<CanvasGroup>().blocksRaycasts = false;

                resultPanel.gameObject.SetActive(true);
                resultPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

                StartCoroutine(SetPauseAfterAd());
            });
        });
    }

    private IEnumerator DiscardCard(string serverCard, int playerId)
    {
        for (int i = 0; i < discardedParent.childCount; i++)
        {
            Destroy(discardedParent.GetChild(i).gameObject);
        }

        CardInfo cardInfo = FindCard(serverCard, playerId);

        RectTransform card = cardInfo.GetComponent<RectTransform>();

        cardSfx.PlayOneShot(cardSfxClip);

        cardInfo.isFaceUp = true;

        UpdateFace(cardInfo);

        card.SetParent(canvas.transform);
        card.SetSiblingIndex(canvas.transform.childCount - 7);

        yield return MoveToTarget(discardedParent.position, card);

        card.SetParent(discardedParent);
    }

    private void OnPlayerTurn(int playerTurn, string turnType)
    {
        for (int i = 0; i < playersName.Count; i++)
        {
            if (!disconnectedPlayerIds.Contains(i))
            {
                playersName[playerMapping[i]].color = i == playerTurn ? Color.green : Color.white;
            }
        }

        if (playerTurn == playerId)
        {
            if (turnType == "draw")
            {
                List<Button> buttonsClickable = new List<Button>();

                Transform topCardInDeck = deckParent.GetChild(deckParent.childCount - 1);

                Button topCardButton = topCardInDeck.GetComponent<Button>();

                buttonsClickable.Add(topCardButton);

                topCardButton.onClick.AddListener(() =>
                {
                    if (activeAnimations == 0 && gameState == GameState.PLAYING && !isProcessing)
                    {
                        socketManager.socket.Emit("draw_card", new DrawAction("draw_from_deck", turnType, playerTurn, playerId));

                        foreach (Button button in buttonsClickable)
                        {
                            button.onClick.RemoveAllListeners();
                        }
                    }
                });

                if (discardedParent.childCount > 0)
                {
                    Transform discarded = discardedParent.GetChild(discardedParent.childCount - 1);

                    Button discardedButton = discarded.GetComponent<Button>();

                    buttonsClickable.Add(discardedButton);

                    discardedButton.onClick.AddListener(() =>
                    {
                        if (activeAnimations == 0 && gameState == GameState.PLAYING && !isProcessing)
                        {
                            socketManager.socket.Emit("draw_card", new DrawAction("draw_discarded", turnType, playerTurn, playerId));

                            foreach (Button button in buttonsClickable)
                            {
                                button.onClick.RemoveAllListeners();
                            }
                        }
                    });
                }
            }
            if (turnType == "discard")
            {
                List<Button> buttonsClickable = new List<Button>();

                RectTransform playerCards = playerParents[playerMapping[playerTurn]];

                for (int i = 0; i < playerCards.childCount; i++)
                {
                    Button cardButton = playerCards.GetChild(i).GetComponent<Button>();

                    buttonsClickable.Add(cardButton);

                    cardButton.onClick.AddListener(() =>
                    {
                        if (activeAnimations == 0 && gameState == GameState.PLAYING && !isProcessing)
                        {
                            CardInfo cardInfo = cardButton.GetComponent<CardInfo>();

                            socketManager.socket.Emit("discard_card", new DiscardAction(CardToString(cardInfo), turnType, playerTurn, playerId));

                            foreach (Button button in buttonsClickable)
                            {
                                button.onClick.RemoveAllListeners();
                            }
                        }
                    });
                }
            }
        }
    }

    private void FaceUpAllCards()
    {
        for (int i = 1; i < 4; i++)
        {
            for (int j = 0; j < playerParents[i].childCount; j++)
            {
                CardInfo cardInfo = playerParents[i].GetChild(j).GetComponent<CardInfo>();

                cardInfo.isFaceUp = true;

                UpdateFace(cardInfo);
            }
        }
    }

    private string CardToString(CardInfo cardInfo)
    {
        return $"{cardInfo.rank}{cardInfo.suit[0]}";
    }

    private void UpdateFace(CardInfo card)
    {
        card.GetComponent<Image>().sprite = card.isFaceUp ? card.sprite : faceDownCardSprite;
    }

    private IEnumerator DisconnectPlayer()
    {
        if (pausePanel.gameObject.activeSelf)
        {
            pausePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
            playerData.SaveData();
            yield return DelayedUnpause();
        }

        if (matchmakingPanel.gameObject.activeSelf)
        {
            matchmakingPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
            yield return new WaitForSeconds(0.2f);
            matchmakingPanel.gameObject.SetActive(false);
        }

        Time.timeScale = 0f;
        backgroundMusic.Stop();
        gameState = GameState.RESULT;

        disconnectedPanel.gameObject.SetActive(true);
        disconnectedPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    private IEnumerator MoveToTarget(Vector3 targetPos, RectTransform rect, float duration = 0.1f)
    {
        activeAnimations++;

        Vector3 startPos = rect.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rect.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.position = targetPos;

        activeAnimations--;
    }

    private void UpdateMusicVolume()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(playerData.musicVolume) * 20);
    }

    private void UpdateSfxVolume()
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(playerData.sfxVolume) * 20);
    }

    public void OnMusicVolumeChange(float value)
    {
        playerData.musicVolume = value;
        UpdateMusicVolume();
    }

    public void OnSfxVolumeChange(float value)
    {
        playerData.sfxVolume = value;
        UpdateSfxVolume();
    }

    public void Pause()
    {
        pausePanel.gameObject.SetActive(true);
        pausePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        backgroundMusic.Pause();
        buttonClickSfx.Play();
        gameState = GameState.PAUSE;

        musicVolumeSlider.value = playerData.musicVolume;
        sfxVolumeSlider.value = playerData.sfxVolume;
    }

    public void Continue()
    {
        pausePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        playerData.SaveData();
        buttonClickSfx.Play();
        StartCoroutine(DelayedUnpause());
    }

    private IEnumerator DelayedUnpause()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        pausePanel.gameObject.SetActive(false);
        backgroundMusic.UnPause();
        gameState = GameState.PLAYING;
    }

    public void HomeButton()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Menu"));
    }

    public void RetryButton()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Multiplayer Game"));
    }

    private IEnumerator SetPauseAfterAd()
    {
        yield return null; // Wait 1 frame so SDK finishes its reset
        Time.timeScale = 0f;
    }

    private IEnumerator SwitchScene(string name)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;
        crossfade.SetBool("isOpen", true);

        gameState = GameState.RESULT;

        var task = DisconnectSocket();
        yield return new WaitUntil(() => task.IsCompleted);

        socketManager.onServerEvent -= HandleServerEvent;

        yield return new WaitForSecondsRealtime(0.3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
    }
}
