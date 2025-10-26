using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public ConfigHandler configHandler;
    public GameObject cardPrefab;
    public List<RectTransform> playerParents;
    public RectTransform deckParent;
    public RectTransform discardParent;
    public Sprite faceDownCardSprite;
    public List<TMP_Text> playersName;
    public Canvas canvas;
    public Animator crossfade;

    public Transform pausePanel;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Transform resultPanel;
    public List<TMP_Text> playersScoreResult;
    public TMP_Text winnerTxt;

    public AudioSource backgroundMusic;
    public AudioSource cardSfx;
    public AudioSource buttonClickSfx;

    public AudioClip cardSfxClip;

    public AudioMixer audioMixer;

    private List<RectTransform> deck;
    private TurnType turnType;
    private GameState gameState;
    private PlayerData playerData;
    private InterstitialAdManager interstitialAdManager;

    private int activeAnimations = 0;
    private int playerTurn;
    private int playerFirstTurn;

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        turnType = TurnType.DRAW;
        gameState = GameState.PLAYING;
        interstitialAdManager = InterstitialAdManager.GetInstance();

        BannerAdManager.GetInstance().EnsureBannerVisible();

        for (int i = 0; i < playersName.Count; i++)
        {
            playersName[i].text = playerData.playersName[i];
        }

        deck = configHandler.deck.Select(d =>
        {
            GameObject instance = Instantiate(cardPrefab, deckParent);

            CardInfo cardInfo = instance.GetComponent<CardInfo>();

            cardInfo.Initialize(d.suit, d.value, d.sprite, d.rank);

            instance.GetComponent<Image>().sprite = cardInfo.isFaceUp ? d.sprite : faceDownCardSprite;

            Button cardButton = instance.GetComponent<Button>();

            cardButton.onClick.AddListener(() =>
            {
                if (activeAnimations == 0 && playerTurn == 0 && gameState == GameState.PLAYING)
                {
                    switch (turnType)
                    {
                        case TurnType.DRAW:
                            if (instance.transform.parent == deckParent)
                            {
                                StartCoroutine(PlayerDrawCard());
                            }
                            if (instance.transform.parent == discardParent)
                            {
                                StartCoroutine(PlayerDrawDiscardedCard());
                            }
                            break;
                        case TurnType.DISCARD:
                            if (instance.transform.parent == playerParents[playerTurn])
                            {
                                StartCoroutine(PlayerDiscardCard(instance.GetComponent<RectTransform>()));
                            }
                            break;
                    }
                }
            });

            return instance.GetComponent<RectTransform>();

        }).ToList();

        ShuffleDeck();

        StartCoroutine(InitialDrawCard());

        playerData.computerGamesPlayed++;
        playerData.SaveData();
    }

    private void Start()
    {
        UpdateMusicVolume();
        UpdateSfxVolume();
    }

    private void Update()
    {
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

    private IEnumerator ComputerTurn()
    {
        yield return new WaitForSeconds(0.5f);

        int discardOption = discardParent.childCount > 0 ? GetBestScore(playerParents[playerTurn], discardParent.GetChild(discardParent.childCount - 1).GetComponent<RectTransform>()) : 0;

        int deckOption = EstimateDeckDraw(playerParents[playerTurn], deck);

        if (discardOption > deckOption)
        {
            yield return DrawDiscardedCard(playerTurn);
        }
        else
        {
            yield return DrawCard(playerTurn);
        }

        yield return new WaitForSeconds(0.5f);
        
        int bestIndexToDiscard = GetBestToDiscard();

        yield return DiscardCard(playerParents[playerTurn].GetChild(bestIndexToDiscard).GetComponent<RectTransform>());

        CheckWin();
    }

    private int GetBestToDiscard()
    {
        int bestScore = 0;
        int bestIndexToDiscard = 0;

        for (int i = 0; i < playerParents[playerTurn].childCount; i++)
        {
            var hypotheticalHand = new List<Card>();

            for (int j = 0; j < playerParents[playerTurn].childCount; j++)
            {
                if (i != j)
                {
                    CardInfo cardInfo = playerParents[playerTurn].GetChild(j).GetComponent<CardInfo>();

                    hypotheticalHand.Add(new Card(cardInfo.suit, cardInfo.value));
                }
            }

            int score = GetBestSuitScore(hypotheticalHand);

            if (score > bestScore)
            {
                bestScore = score;
                bestIndexToDiscard = i;
            }
        }

        return bestIndexToDiscard;
    }

    private int GetBestScore(RectTransform players, RectTransform drawn)
    {
        int bestScore = 0;

        for (int i = 0; i < players.childCount; i++)
        {
            var hypotheticalHand = new List<Card>();

            for (int j = 0; j < players.childCount; j++)
            {
                CardInfo cardInfo = players.GetChild(j).GetComponent<CardInfo>();

                hypotheticalHand.Add(new Card(cardInfo.suit, cardInfo.value));
            }

            CardInfo drawnInfo = drawn.GetComponent<CardInfo>();

            hypotheticalHand.Add(new Card(drawnInfo.suit, drawnInfo.value));

            hypotheticalHand.RemoveAt(i);

            int score = GetBestSuitScore(hypotheticalHand);

            bestScore = Mathf.Max(bestScore, score);
        }

        return bestScore;
    }

    private int EstimateDeckDraw(RectTransform players, List<RectTransform> remainingDeck)
    {
        int simulations = Mathf.Min(5, remainingDeck.Count);
        int totalScore = 0;

        for (int i = 0; i < simulations; i++)
        {
            RectTransform randomCard = remainingDeck[Random.Range(0, remainingDeck.Count)];

            int score = GetBestScore(players, randomCard);
            totalScore += score;
        }

        return totalScore / simulations;
    }

    private IEnumerator InitialDrawCard()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                yield return DrawCard(i);
            }
        }

        playerTurn = Random.Range(0, 4);
        playerFirstTurn = playerTurn;

        if (playerTurn > 0) StartCoroutine(ComputerTurn());
    }

    private IEnumerator PlayerDrawCard()
    {
        yield return DrawCard(playerTurn);

        turnType = TurnType.DISCARD;
    }

    private IEnumerator PlayerDrawDiscardedCard()
    {
        yield return DrawDiscardedCard(playerTurn);

        turnType = TurnType.DISCARD;
    }

    private IEnumerator PlayerDiscardCard(RectTransform rect)
    {
        yield return DiscardCard(rect);

        turnType = TurnType.DRAW;

        CheckWin();
    }

    private IEnumerator DrawCard(int playerIndex, float duration = 0.1f)
    {
        if (deck.Count > 0)
        {
            cardSfx.PlayOneShot(cardSfxClip);

            RectTransform card = deck.Last();

            deck.RemoveAt(deck.Count - 1);

            CardInfo cardInfo = card.GetComponent<CardInfo>();

            cardInfo.isFaceUp = playerIndex == 0;

            UpdateFace(cardInfo);

            card.SetParent(canvas.transform);
            card.SetSiblingIndex(canvas.transform.childCount - 4);

            yield return MoveToTarget(playerParents[playerIndex].position, card, duration);

            card.SetParent(playerParents[playerIndex]);
        }
    }

    private IEnumerator DrawDiscardedCard(int playerIndex, float duration = 0.1f)
    {
        if (discardParent.childCount > 0)
        {
            cardSfx.PlayOneShot(cardSfxClip);

            RectTransform card = discardParent.GetChild(discardParent.childCount - 1).GetComponent<RectTransform>();

            CardInfo cardInfo = card.GetComponent<CardInfo>();

            cardInfo.isFaceUp = playerIndex == 0;

            UpdateFace(cardInfo);

            card.SetParent(canvas.transform);
            card.SetSiblingIndex(canvas.transform.childCount - 4);

            yield return MoveToTarget(playerParents[playerIndex].position, card, duration);

            card.SetParent(playerParents[playerIndex]);
        }
    }

    private IEnumerator DiscardCard(RectTransform card, float duration = 0.1f)
    {
        for (int i = 0; i < discardParent.childCount; i++)
        {
            Destroy(discardParent.GetChild(i).gameObject);
        }

        cardSfx.PlayOneShot(cardSfxClip);

        CardInfo cardInfo = card.GetComponent<CardInfo>();

        cardInfo.isFaceUp = true;

        UpdateFace(cardInfo);

        card.SetParent(canvas.transform);
        card.SetSiblingIndex(canvas.transform.childCount - 4);

        yield return MoveToTarget(discardParent.position, card, duration);

        card.SetParent(discardParent);
    }

    private void CheckWin()
    {
        if (Check41(playerTurn))
        {
            StartCoroutine(Win(playerTurn));
        }
        else
        {
            if (deck.Count <= 0)
            {
                StartCoroutine(Win(CheckHighest()));
            }
            else
            {
                SwitchPlayerTurn();
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

    private bool Check41(int playerIndex)
    {
        List<Card> hand = new List<Card>();

        for (int i = 0; i < playerParents[playerIndex].childCount; i++)
        {
            CardInfo cardInfo = playerParents[playerIndex].GetChild(i).GetComponent<CardInfo>();

            hand.Add(new Card(cardInfo.suit, cardInfo.value));
        }

        int bestScore = GetBestSuitScore(hand);

        return bestScore == 41;
    }

    private int CheckHighest()
    {
        int highestScore = 0;
        List<int> scores = new List<int>();

        for (int i = 0; i < playerParents.Count; i++)
        {
            List<Card> hand = new List<Card>();

            for (int j = 0; j < playerParents[i].childCount; j++)
            {
                CardInfo cardInfo = playerParents[i].GetChild(j).GetComponent<CardInfo>();

                hand.Add(new Card(cardInfo.suit, cardInfo.value));
            }

            int score = GetBestSuitScore(hand);

            scores.Add(score);
            highestScore = Mathf.Max(highestScore, score);
        }

        List<int> winners = scores.Select((s, i) => (s, i)).Where(s => s.s == highestScore).Select(s => s.i).ToList();

        for (int i = playerFirstTurn; i < playerFirstTurn + 4; i++)
        {
            if (winners.Contains(i % 4))
            {
                return i % 4;
            }
        }

        return winners.Last();
    }

    private int GetBestSuitScore(List<Card> hand)
    {
        var grouped = hand.GroupBy(c => c.suit);
        int maxScore = 0;

        foreach (var suitGroup in grouped)
        {
            maxScore = Mathf.Max(maxScore, suitGroup.Sum(c => c.value));
        }

        return maxScore;
    }

    private void UpdateFace(CardInfo card)
    {
        card.GetComponent<Image>().sprite = card.isFaceUp ? card.sprite : faceDownCardSprite;
    }

    private void ShuffleDeck()
    {
        deck = deck.OrderBy(d => Random.value).ToList();
    }

    private void SwitchPlayerTurn()
    {
        playerTurn = playerTurn >= 3 ? 0 : playerTurn + 1;

        if (playerTurn > 0) StartCoroutine(ComputerTurn());
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

    private IEnumerator Win(int winnerIndex)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;

        FaceUpAllCards();

        backgroundMusic.Stop();

        gameState = GameState.RESULT;

        Time.timeScale = 0f;

        winnerTxt.text = playerData.playersName[winnerIndex] + " Wins";

        if (winnerIndex == 0)
        {
            playerData.computerGamesWon++;
            playerData.SaveData();
        }

        for (int i = 0; i < playerParents.Count; i++)
        {
            List<Card> hand = new List<Card>();

            for (int j = 0; j < playerParents[i].childCount; j++)
            {
                CardInfo cardInfo = playerParents[i].GetChild(j).GetComponent<CardInfo>();

                hand.Add(new Card(cardInfo.suit, cardInfo.value));
            }

            playersScoreResult[i].text = $"{playerData.playersName[i]}: {GetBestSuitScore(hand)}";
        }

        yield return new WaitForSecondsRealtime(3f);

        interstitialAdManager.ShowInterstitial(() =>
        {
            crossfade.GetComponent<CanvasGroup>().blocksRaycasts = false;

            resultPanel.gameObject.SetActive(true);
            resultPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

            StartCoroutine(SetPauseAfterAd());
        });
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

        Time.timeScale = 0f;
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
        Time.timeScale = 1f;
    }

    public void HomeButton()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Menu"));
    }

    public void RetryButton()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Computer Game"));
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
        yield return new WaitForSecondsRealtime(0.3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
    }
}
