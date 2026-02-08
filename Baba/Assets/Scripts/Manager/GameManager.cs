using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using DG.Tweening;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;
using FPSTrump.Result;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core Game Components")]
    [SerializeField] private PlayerHandController playerHand;
    [SerializeField] private AIHandController aiHand;
    [SerializeField] public DiscardPile discardPile;
    [SerializeField] private CardDeck cardDeck;

    [Header("Camera System (Phase 4)")]
    [SerializeField] private CameraCinematicsSystem cameraSystem;

    [Header("Result System (Stage 7)")]
    [SerializeField] private ResultUI resultUI;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Setup;
    public GameState CurrentState => currentState;
    [SerializeField] private int currentPlayerTurn = 0; // 0 = Player, 1 = AI
    [SerializeField] private int turnCounter = 0;

    // Stage 15: ターン番号を外部から取得可能に（会話履歴記録用）
    public int GetTurnCounter() => turnCounter;

    // Stage 16: Public accessors for bluff system
    public PlayerHandController PlayerHand => playerHand;
    public AIHandController AIHand => aiHand;

    [Header("Confirm UI")]
    [SerializeField] private int confirmCardThreshold = 5; // AI手札がこの枚数以下で確認UI表示

    [Header("Timing")]
    [SerializeField] private float turnTransitionDelay = 0.5f;
    [SerializeField] private float dealCardDelay = 0.3f;

    public UnityEvent<GameState> OnGameStateChanged;
    public UnityEvent<int> OnTurnChanged;
    public UnityEvent<string> OnGameEnded;

    private bool isGameActive = false;
    private bool isProcessingTurn = false;
    private Coroutine currentCameraFocusCoroutine = null;
    private GameObject currentFocusPoint = null;
    private float playerTurnStartTime;

    // Mentalist dialogue system
    private Coroutine idleCommentaryCoroutine;
    private bool isMentalistSpeaking = false;
    private RuleBasedDialogueGenerator mentalistDialogueGen;

    // Cross-turn narrative context
    private System.Text.StringBuilder narrativeSummary = new System.Text.StringBuilder();

    // Cached reference (avoid FindFirstObjectByType every turn)
    private LLMManager cachedLLMManager;

    // Filler TTS pool (pre-generated during game start)
    private AudioClip[] fillerTTSPool;
    private string[] fillerTexts;

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = false;

    private void Awake()
    {
        DOTween.SetTweensCapacity(500, 50);

        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ValidateComponents();
        EnsureEventSystem();
        EnsureBluffActionSystem();
    }

    /// <summary>
    /// EventSystemが存在しない場合に自動生成（シーン遷移でStartMenuのEventSystemが破棄されるため）
    /// </summary>
    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[GameManager] EventSystem created (not found in scene)");
        }
    }

    /// <summary>
    /// BluffActionSystemが存在しない場合に自動生成（AIブラフ監視に必須）
    /// </summary>
    private void EnsureBluffActionSystem()
    {
        if (BluffActionSystem.Instance == null)
        {
            var systemObj = new GameObject("BluffActionSystem");
            systemObj.AddComponent<BluffActionSystem>();
            // BluffActionSystemは自身のAwake()でplayerHand/aiHandを自動検出
            Debug.Log("[GameManager] BluffActionSystem auto-created");
        }
    }

    private void Start()
    {
        StartNewGame();
    }

    private void OnEnable()
    {
        // イベント購読
        if (playerHand != null)
        {
            playerHand.OnPairMatched += OnPlayerPairMatched;
        }
        if (aiHand != null)
        {
            aiHand.OnPairMatched += OnAIPairMatched;
        }

        // 言語変更イベント購読
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDisable()
    {
        // イベント購読解除
        if (playerHand != null)
        {
            playerHand.OnPairMatched -= OnPlayerPairMatched;
        }
        if (aiHand != null)
        {
            aiHand.OnPairMatched -= OnAIPairMatched;
        }

        // 言語変更イベント購読解除
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(GameSettings.GameLanguage newLanguage)
    {
        RefreshFillerTexts();
    }

    /// <summary>
    /// Refresh filler texts to current language
    /// </summary>
    private void RefreshFillerTexts()
    {
        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            fillerTexts = loc.GetArray("bluff.filler_thinking");
        }
        if (fillerTexts == null || fillerTexts.Length == 0)
        {
            fillerTexts = new[] { "...", "Hmm...", "Well..." };
        }

        // Regenerate TTS pool for new language
        _ = PregenFillerPool();

        Debug.Log($"[GameManager] Filler texts refreshed to {(loc != null && loc.IsJapanese ? "Japanese" : "English")}");
    }

    private void ValidateComponents()
    {
        if (playerHand == null)
            Debug.LogError("GameManager: PlayerHandController is not assigned!");
        if (aiHand == null)
            Debug.LogError("GameManager: AIHandController is not assigned!");
        if (discardPile == null)
            Debug.LogError("GameManager: DiscardPile is not assigned!");
        if (cardDeck == null)
            Debug.LogError("GameManager: CardDeck is not assigned!");
    }

    public void StartNewGame()
    {
        Debug.Log("[GameManager] StartNewGame() called");

        // 前回ゲームのコルーチンを全停止
        StopAllCoroutines();

        // 状態フラグリセット
        isProcessingTurn = false;
        isGameActive = false;
        isMentalistSpeaking = false;

        // 音声・UI停止
        AudioManager.Instance?.StopVoice();
        AudioManager.Instance?.StopMusic();
        AudioManager.Instance?.StopHeartbeat();
        if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
            SubtitleUI.Instance.Hide();

        // カメラフォーカス解除
        StopCameraFocusCoroutine();

        Debug.Log("[GameManager] Starting NewGameSequence coroutine");
        StartCoroutine(NewGameSequence());
    }

    private IEnumerator NewGameSequence()
    {
        Debug.Log("[GameManager] NewGameSequence started");

        // 1. Setup状態に変更
        ChangeState(GameState.Setup);

        // 2. コンポーネント初期化（シーン遷移時にnullになる可能性があるため動的検索）
        if (playerHand == null)
        {
            playerHand = FindFirstObjectByType<PlayerHandController>();
            Debug.Log($"[GameManager] PlayerHandController dynamically found: {playerHand != null}");
        }
        if (aiHand == null)
        {
            aiHand = FindFirstObjectByType<AIHandController>();
            Debug.Log($"[GameManager] AIHandController dynamically found: {aiHand != null}");
        }
        if (discardPile == null)
        {
            discardPile = FindFirstObjectByType<DiscardPile>();
            Debug.Log($"[GameManager] DiscardPile dynamically found: {discardPile != null}");
        }

        if (playerHand != null) playerHand.ClearHand();
        if (aiHand != null) aiHand.ClearHand();
        if (discardPile != null) discardPile.Clear();
        turnCounter = 0;
        narrativeSummary.Clear();

        // ブラフアクションシステム自動生成（シーンに存在しない場合）
        bool createdBluffSystems = false;
        if (BluffActionSystem.Instance == null)
        {
            var bluffSystemObj = new GameObject("BluffActionSystem");
            bluffSystemObj.AddComponent<BluffActionSystem>();
            Debug.Log("[GameManager] BluffActionSystem auto-created");
            createdBluffSystems = true;
        }
        if (BluffActionUI.Instance == null)
        {
            var bluffUIObj = new GameObject("BluffActionUI");
            bluffUIObj.AddComponent<BluffActionUI>();
            Debug.Log("[GameManager] BluffActionUI auto-created");
            createdBluffSystems = true;
        }

        // 自動生成した場合は、Awake/Start完了を待つため1フレーム待機
        if (createdBluffSystems)
        {
            yield return null;
        }

        // ブラフアクションシステムリセット
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.ResetSystem();

        // Stage 7: セッション記録開始
        if (GameSessionRecorder.Instance != null)
            GameSessionRecorder.Instance.StartSession();

        // Stage 15: 会話履歴クリア
        LLMContextWindow.ClearHistory();

        // メンタリストダイアログ生成器を初期化
        mentalistDialogueGen = new RuleBasedDialogueGenerator();

        // PersonalityProfile & 事前生成セリフをメンタリストに接続（LLMManagerキャッシュ）
        cachedLLMManager = LLMManager.Instance;
        if (cachedLLMManager != null)
        {
            if (cachedLLMManager.CurrentPlayerProfile != null)
            {
                mentalistDialogueGen.SetPlayerProfile(cachedLLMManager.CurrentPlayerProfile);
            }
            if (cachedLLMManager.PreGeneratedPersonalityLines != null && cachedLLMManager.PreGeneratedPersonalityLines.Count > 0)
            {
                mentalistDialogueGen.SetPersonalityReadLines(cachedLLMManager.PreGeneratedPersonalityLines);
            }
        }

        // フィラーTTSプール生成（ゲーム開始前に全TTSキャッシュ）
        RefreshFillerTexts();

        // 3. カードデッキ初期化
        Debug.LogWarning("========================================");
        Debug.LogWarning("[GameManager] About to initialize cardDeck");

        // シーン遷移時にcardDeckがnullになる場合があるため、動的に検索
        if (cardDeck == null)
        {
            Debug.LogWarning("[GameManager] cardDeck is null, searching in scene...");
            cardDeck = FindFirstObjectByType<CardDeck>();

            if (cardDeck == null)
            {
                Debug.LogError("[GameManager] CardDeck not found in scene! Cannot initialize deck.");
                Debug.LogWarning("========================================");
                yield break;
            }
            else
            {
                Debug.LogWarning("[GameManager] CardDeck found and assigned");
            }
        }

        Debug.LogWarning("[GameManager] cardDeck exists, calling Initialize()");
        cardDeck.Initialize();
        Debug.LogWarning($"[GameManager] After Initialize() - RemainingCards: {cardDeck.RemainingCards}");
        Debug.LogWarning("========================================");

        yield return new WaitForSeconds(0.5f);

        // === Phase 7-2: 没入型ゲームイントロシーケンス ===
        GameIntroSequence introSequence = GameIntroSequence.Instance;
        if (introSequence != null)
        {
            yield return StartCoroutine(introSequence.PlayIntroSequence());
        }
        else
        {
            Debug.LogWarning("[GameManager] GameIntroSequence not found, skipping intro");
        }

        // イントロ完了後にPlayerProfile再取得（イントロ中に生成される場合がある）
        if (mentalistDialogueGen != null)
        {
            var llmPost = cachedLLMManager;
            if (llmPost != null && llmPost.CurrentPlayerProfile != null)
            {
                mentalistDialogueGen.SetPlayerProfile(llmPost.CurrentPlayerProfile);
                Debug.Log("[GameManager] PlayerProfile updated on mentalistDialogueGen (post-intro)");
            }
        }

        Debug.Log("[GameManager] Intro sequence completed, proceeding to card dealing");

        // 4. カード配布
        yield return StartCoroutine(DealInitialCards());

        // 5. 心臓音開始（BGMはAudioManager.Startでシーンロード時に開始済み）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHeartbeat(0.3f, playerHand != null ? playerHand.GetCardCount() : 10);
        }

        // 6. ブラフアクションUI表示
        if (BluffActionUI.Instance != null)
            BluffActionUI.Instance.Show();

        // 7. プレイヤーターン開始（Phase 4: PLAYER_TURN_PICK状態）
        isGameActive = true;
        StartCoroutine(PlayerTurnSequence());

        Debug.Log("Game started! Player's turn.");
    }

    private IEnumerator DealInitialCards()
    {
        Debug.LogWarning("========================================");
        Debug.LogWarning("[GameManager] DealInitialCards STARTED");
        Debug.LogWarning($"[GameManager] cardDeck is null: {cardDeck == null}");
        if (cardDeck != null)
        {
            Debug.LogWarning($"[GameManager] cardDeck.RemainingCards: {cardDeck.RemainingCards}");
        }
        Debug.LogWarning("========================================");

        // 全カードを交互に配布（ババ抜き標準ルール）
        int cardIndex = 0;
        while (cardDeck != null && cardDeck.RemainingCards > 0)
        {
            // 交互に配布（偶数はプレイヤー、奇数はAI）
            if (cardIndex % 2 == 0)
            {
                if (playerHand != null)
                {
                    CardObject playerCard = cardDeck.DrawCard();
                    if (playerCard != null)
                        playerHand.AddCard(playerCard);
                }
            }
            else
            {
                if (aiHand != null)
                {
                    CardObject aiCard = cardDeck.DrawCard();
                    if (aiCard != null)
                        aiHand.AddCard(aiCard);
                }
            }

            cardIndex++;

            // 配布SFX（数枚おきに鳴らす）
            if (cardIndex % 3 == 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayCardPlace();

            // アニメーション用の待機（高速化）
            yield return new WaitForSeconds(dealCardDelay * 0.3f);
        }

        Debug.LogWarning($"[GameManager] Loop completed. Cards dealt: {cardIndex}");
        Debug.Log($"All cards dealt. Player: {playerHand?.GetCardCount()}, AI: {aiHand?.GetCardCount()}");

        // 配布後、初期ペアを削除
        yield return new WaitForSeconds(0.5f);

        if (playerHand != null)
        {
            int playerPairs = playerHand.CheckForPairs();
            Debug.Log($"Player initial pairs removed: {playerPairs}");
        }

        yield return new WaitForSeconds(0.3f);

        if (aiHand != null)
        {
            int aiPairs = aiHand.CheckForPairs();
            Debug.Log($"AI initial pairs removed: {aiPairs}");
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"After pair removal - Player: {playerHand?.GetCardCount()}, AI: {aiHand?.GetCardCount()}");
    }

    // ====================
    // Phase 1のコード（Phase 4で置き換え済み）
    // ====================

    /*
    public void ExecutePlayerCardDraw(int aiCardIndex)
    {
        if (currentState != GameState.PlayerTurn || isProcessingTurn) return;
        if (playerHand == null || aiHand == null) return;

        isProcessingTurn = true;

        CardObject drawnCard = aiHand.RemoveCard(aiCardIndex);
        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);
            Debug.Log($"Player drew a card from AI. Player cards: {playerHand.GetCardCount()}, AI cards: {aiHand.GetCardCount()}");

            StartCoroutine(EndPlayerTurn());
        }
        else
        {
            isProcessingTurn = false;
        }
    }

    private IEnumerator EndPlayerTurn()
    {
        yield return new WaitForSeconds(turnTransitionDelay);

        if (!CheckGameEndConditions())
        {
            NextTurn();
        }

        isProcessingTurn = false;
    }
    */

    // ====================
    // Phase 4: 新しい状態遷移コルーチン
    // ====================

    /// <summary>
    /// プレイヤーターンの7段階フロー
    /// </summary>
    private IEnumerator PlayerTurnSequence()
    {
        // Stage 1: PICK（AIカードホバー待機）
        ChangeState(GameState.PLAYER_TURN_PICK);
        playerTurnStartTime = Time.time;
        CardObject.ResetHoverCount();
        if (aiHand != null) aiHand.EnableCardSelection(true);

        // ペアフォーカス中のカメラ上書きを防止
        StopCameraFocusCoroutine();

        // Phase 4: カメラをAI手札にフォーカス
        if (cameraSystem != null)
        {
            cameraSystem.ShowPlayerTurnView();
        }

        // === メンタリスト: ターン開始挑発（turnCounter > 1のみ、イントロ直後はスキップ） ===
        if (turnCounter > 1)
        {
            yield return StartCoroutine(PlayTurnStartProvocation());
        }

        // === AIジョーカーティーズ（ターン3以降、20%確率） ===
        if (turnCounter > 3 && Random.value < 0.2f)
        {
            yield return StartCoroutine(PlayJokerTease());
        }

        // === メンタリスト: アイドルコメンタリー開始 ===
        idleCommentaryCoroutine = StartCoroutine(IdleCommentaryLoop());

        // === AIブラフアクションモニター開始 ===
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.StartAIBluffMonitor();

        // ユーザー入力待機（CardObjectからイベントが来るまで）
        yield return new WaitUntil(() => currentState != GameState.PLAYER_TURN_PICK);

        // === AIブラフアクションモニター停止 ===
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.StopAIBluffMonitor();

        // === メンタリスト: アイドルコメンタリー停止 ===
        StopIdleCommentary();

        // Stage 2-7: HandleCardInterrupt()からトリガーされる
    }

    /// <summary>
    /// CardObjectのOnPointerDownから呼ばれる
    /// </summary>
    /// <returns>true: 処理受理, false: 拒否（CardObject側でinteractionStateをリセットする）</returns>
    public bool OnCardPointerDown(CardObject card)
    {
        if (currentState != GameState.PLAYER_TURN_PICK || isProcessingTurn) return false;

        // AI手札に属するカードのみ引ける（自分の手札は引けない）
        if (aiHand == null || !aiHand.GetCards().Contains(card)) return false;

        // メンタリスト: アイドルコメンタリー即停止
        StopIdleCommentary();

        // 行動データ記録（BluffSystem行動ベース分岐に必要）
        float decisionTime = Time.time - playerTurnStartTime;
        float hoverDuration = card != null ? card.LastHoverDuration : 0f;
        int cardIndex = aiHand.GetCards().IndexOf(card);

        if (PsychologySystem.Instance != null)
        {
            var analyzer = PsychologySystem.Instance.GetComponent<PlayerBehaviorAnalyzer>();
            int hoverCount = CardObject.HoverCountThisTurn;
            analyzer?.RecordPlayerAction(cardIndex, hoverDuration, decisionTime, aiHand.GetCardCount(), hoverCount);
            CardObject.ResetHoverCount();
        }

        isProcessingTurn = true;
        StartCoroutine(HandleCardInterrupt(card));
        return true;
    }

    /// <summary>
    /// カード引き拒否→確認フロー
    /// AIの残り手札が3枚以下の場合のみ確認UIを表示、それ以外は即引き取り
    /// </summary>
    private IEnumerator HandleCardInterrupt(CardObject card)
    {
        // 確認フロー発動条件: 残り5枚以下 or 圧力レベル1.5以上
        float confirmPressure = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;
        bool showConfirm = aiHand != null && (aiHand.GetCardCount() <= confirmCardThreshold || confirmPressure >= 1.5f);

        if (showConfirm)
        {
            // Stage 2: INTERRUPT（拒否演出）
            ChangeState(GameState.PLAYER_TURN_INTERRUPT);

            // Phase 4 Stage 4: 拒否アニメーション再生
            if (card != null)
            {
                card.PlayInterruptAnimation();
            }

            yield return new WaitForSeconds(0.5f); // アニメーション待機

            // Stage 3: CONFIRM（確認UI表示）
            ChangeState(GameState.PLAYER_TURN_CONFIRM);

            // Phase 4: カメラを選択されたカードにフォーカス
            if (cameraSystem != null && card != null)
            {
                cameraSystem.FocusOnCard(card.transform);
            }

            // Phase 4 Stage 4: 確認UI表示
            if (ConfirmUI.Instance != null)
            {
                ConfirmUI.Instance.Show(card, OnConfirmDraw, OnConfirmAbort);

                // タイムアウト安全機構: 15秒以内に選択がなければ自動引き取り
                float confirmStartTime = Time.time;
                yield return new WaitUntil(() =>
                    currentState != GameState.PLAYER_TURN_CONFIRM ||
                    Time.time - confirmStartTime > 15f
                );

                // タイムアウト時: 自動引き取り
                if (currentState == GameState.PLAYER_TURN_CONFIRM)
                {
                    Debug.LogWarning("[GameManager] ConfirmUI timeout (15s) - auto-drawing card");
                    ConfirmUI.Instance.Hide();
                    OnConfirmDraw(card);
                }
            }
            else
            {
                // ConfirmUIなし: 即引き取りにフォールバック
                Debug.LogWarning("[GameManager] ConfirmUI.Instance is null - falling back to immediate draw");
                if (card != null) card.SetCommitted();
                StartCoroutine(ExecuteCardDraw(card));
            }
        }
        else
        {
            // 即引き取り（確認UIスキップ）
            if (card != null)
            {
                card.SetCommitted();
            }
            StartCoroutine(ExecuteCardDraw(card));
        }
    }

    /// <summary>
    /// 確認UI「引く」選択時
    /// </summary>
    public void OnConfirmDraw(CardObject card)
    {
        Debug.Log("[GameManager] Player confirmed card draw");

        // カードを選択確定状態に変更
        if (card != null)
        {
            card.SetCommitted();
        }

        StartCoroutine(ExecuteCardDraw(card));
    }

    /// <summary>
    /// 確認UI「やめる」選択時
    /// </summary>
    public void OnConfirmAbort()
    {
        Debug.Log("[GameManager] Player aborted card selection, returning to PICK state");

        // Phase 4: カメラをプレイヤーターンビューに戻す
        if (cameraSystem != null)
        {
            cameraSystem.ShowPlayerTurnView();
        }

        // PICK状態に戻る（別カード選択可能）
        ChangeState(GameState.PLAYER_TURN_PICK);

        // AIカード選択を再度有効化
        if (aiHand != null)
        {
            aiHand.EnableCardSelection(true);
        }

        isProcessingTurn = false;

        // メンタリスト: アイドルコメンタリー再開
        playerTurnStartTime = Time.time;
        idleCommentaryCoroutine = StartCoroutine(IdleCommentaryLoop());

        // AIブラフアクションモニター再開
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.StartAIBluffMonitor();
    }

    /// <summary>
    /// カードを引く実行
    /// </summary>
    private IEnumerator ExecuteCardDraw(CardObject card)
    {
        // Stage 4: COMMIT（実行直前）
        ChangeState(GameState.PLAYER_TURN_COMMIT);

        // Stage 5: AI期待決定（ドロー前にAnticipatingに遷移）
        if (BluffSystem.Instance != null)
        {
            AIExpectation expectation = BluffSystem.Instance.DetermineExpectation();
            Debug.Log($"[GameManager] AI expectation (player turn): {expectation}");
        }

        yield return new WaitForSeconds(0.15f); // 静止時間

        // Stage 5: DRAW（引くアニメーション）
        ChangeState(GameState.PLAYER_TURN_DRAW);

        // PostProcessフォーカスエフェクト
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.ApplyFocusEffect();

        // カードを引く
        int cardIndex = aiHand.GetCards().IndexOf(card);
        CardObject drawnCard = aiHand.RemoveCard(cardIndex);
        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);

            // カードドローSFX
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCardFlip();

            // プレイヤーがJokerを引いた場合、AIが嘲笑してシャッフル
            if (drawnCard.cardData.isJoker)
            {
                yield return new WaitForSeconds(0.8f);

                // 嘲笑台詞を表示
                var loc = LocalizationManager.Instance;
                if (loc != null)
                {
                    string[] mockDialogues = loc.GetArray("bluff_action.ai_mock_player_drew_joker");
                    if (mockDialogues != null && mockDialogues.Length > 0)
                    {
                        string mockDialogue = mockDialogues[Random.Range(0, mockDialogues.Length)];
                        if (SubtitleUI.Instance != null)
                        {
                            float pressureLevel = PsychologySystem.Instance != null
                                ? PsychologySystem.Instance.GetPressureLevel()
                                : 0f;
                            SubtitleUI.Instance.Show(mockDialogue, pressureLevel);
                        }
                    }
                }

                yield return new WaitForSeconds(1.5f);

                // AIがシャッフル実行
                if (aiHand != null)
                {
                    aiHand.ShuffleCards();
                }

                yield return new WaitForSeconds(0.8f);

                // 挑発台詞を表示
                if (loc != null)
                {
                    string[] tauntDialogues = loc.GetArray("bluff_action.ai_taunt_after_shuffle");
                    if (tauntDialogues != null && tauntDialogues.Length > 0)
                    {
                        string tauntDialogue = tauntDialogues[Random.Range(0, tauntDialogues.Length)];
                        if (SubtitleUI.Instance != null)
                        {
                            float pressureLevel = PsychologySystem.Instance != null
                                ? PsychologySystem.Instance.GetPressureLevel()
                                : 0f;
                            SubtitleUI.Instance.Show(tauntDialogue, pressureLevel);
                        }
                    }
                }

                Debug.Log("[GameManager] Player drew Joker, AI mocked and shuffled");
                yield return new WaitForSeconds(1.2f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Stage 6: POST_REACT（ブラフ判定）
        ChangeState(GameState.PLAYER_TURN_POST_REACT);

        // Stage 5: 感情リアクション評価（予測 vs 現実）
        if (BluffSystem.Instance != null && drawnCard != null)
        {
            bool drawnIsJoker = drawnCard.cardData.isJoker;
            bool formedPair = false;
            foreach (var handCard in playerHand.GetCards())
            {
                if (handCard != drawnCard && handCard.cardData.IsMatchingPair(drawnCard.cardData))
                {
                    formedPair = true;
                    break;
                }
            }

            bool aiHasJoker = false;
            foreach (var c in aiHand.GetCards())
                if (c.cardData.isJoker) { aiHasJoker = true; break; }

            // Stage 16: Get player bluff behavior summary
            string bluffSummary = "";
            var behaviorAnalyzer = FindFirstObjectByType<FPSTrump.Psychology.PlayerBehaviorAnalyzer>();
            if (behaviorAnalyzer != null)
            {
                bluffSummary = behaviorAnalyzer.GetBluffBehaviorSummary();
            }

            DrawContext drawCtx = new DrawContext
            {
                isPlayerTurn = true,
                drawnCardIsJoker = drawnIsJoker,
                formedPair = formedPair,
                remainingCards = playerHand.GetCardCount(),
                opponentRemainingCards = aiHand.GetCardCount(),
                aiHoldsJoker = aiHasJoker,
                playerBluffSummary = bluffSummary
            };

            // EvaluateReaction is now async, await it via Task
            System.Threading.Tasks.Task<EmotionalResult> evaluateTask = BluffSystem.Instance.EvaluateReaction(drawCtx);
            yield return new WaitUntil(() => evaluateTask.IsCompleted);
            EmotionalResult result = evaluateTask.Result;
            yield return StartCoroutine(PlayEmotionalReaction(result, drawCtx));
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Stage 7: RESOLVE（ペア判定・勝敗判定）
        yield return StartCoroutine(PlayerTurnResolve());
    }

    /// <summary>
    /// プレイヤーターンのリゾルブ処理
    /// </summary>
    private IEnumerator PlayerTurnResolve()
    {
        ChangeState(GameState.PLAYER_TURN_RESOLVE);
        turnCounter++;

        // ペア判定
        bool pairFormed = false;
        if (playerHand != null)
        {
            int pairsFormed = playerHand.CheckForPairs();
            pairFormed = pairsFormed > 0;
            Debug.Log($"Player pairs formed: {pairsFormed}");

            // ペアマッチSFX
            if (pairFormed && AudioManager.Instance != null)
                AudioManager.Instance.PlayCardPlace();

            // Stage 7: ペア記録
            if (pairFormed && GameSessionRecorder.Instance != null)
                GameSessionRecorder.Instance.RecordPairFormed();
        }

        // Stage 7: ターン記録
        RecordCurrentTurn(isPlayerTurn: true, pairFormed);

        // 心臓音の強度更新（残りカード数に応じて）
        UpdateHeartbeat();

        yield return new WaitForSeconds(0.3f);

        // ペアフォーカスコルーチン完了を待機（1.0s演出が途中で切られるのを防止）
        if (currentCameraFocusCoroutine != null)
        {
            yield return currentCameraFocusCoroutine;
        }

        // 勝敗判定
        if (CheckGameEndConditions())
        {
            isProcessingTurn = false;
            yield break;
        }

        // 次のターン（AIターン）へ
        isProcessingTurn = false;
        StartCoroutine(AITurnSequence());
    }

    /// <summary>
    /// ゲーム決着判定（BluffSystem.GetGameDecisiveDialogue と同じロジック）
    /// </summary>
    private bool IsGameDecisive(DrawContext ctx)
    {
        bool drawerWillWin = ctx.formedPair && ctx.remainingCards == 2;

        // ドロー側がペア形成後にJokerのみ残る → ドロー側敗北
        bool drawerOnlyJokerLeft = ctx.formedPair && ctx.remainingCards == 3 &&
            ((ctx.isPlayerTurn && !ctx.aiHoldsJoker) || (!ctx.isPlayerTurn && ctx.aiHoldsJoker));

        bool opponentEmptied = ctx.opponentRemainingCards == 0 && !drawerWillWin && !drawerOnlyJokerLeft;

        return drawerWillWin || drawerOnlyJokerLeft || opponentEmptied;
    }

    /// <summary>
    /// Stage 5: 感情リアクションの演出（3層レスポンス）
    /// Layer A: 即座の台詞 → Layer B: LLM感情理由付け → Layer C: ターニングポイント演出
    /// </summary>
    private IEnumerator PlayEmotionalReaction(EmotionalResult result, DrawContext drawCtx)
    {
        float pressureLevel = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;

        Camera cam = Camera.main;
        Vector3 dialoguePosition = cam != null
            ? cam.transform.position + cam.transform.forward * 2f + cam.transform.up * -0.1f
            : Vector3.up * 1.5f;

        // === 決着判定: ゲーム終了直前の場合はフィラー + Layer B/C をスキップ ===
        bool isGameDecisive = IsGameDecisive(drawCtx);

        // === Filler: 即座にフィラー再生（Layer A TTS待ち時間をカバー） ===
        System.Threading.Tasks.Task<AudioClip> layerATtsTask = null;
        if (!isGameDecisive && fillerTTSPool != null && !string.IsNullOrEmpty(result.immediateDialogue))
        {
            int fillerIdx = Random.Range(0, fillerTexts.Length);
            if (FloatingTextSystem.Instance != null)
                FloatingTextSystem.Instance.ShowText(dialoguePosition, fillerTexts[fillerIdx], pressureLevel);
            if (fillerTTSPool[fillerIdx] != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayVoice(fillerTTSPool[fillerIdx], Vector3.zero, 1.0f);

            // フィラー再生中にLayer A TTS生成を並行開始
            var llm = cachedLLMManager;
            if (llm != null)
                layerATtsTask = llm.GenerateTTSAsync(result.immediateDialogue, result.emotion);

            yield return new WaitForSeconds(1.0f);
        }

        // === Layer A: 即座の台詞表示 + TTS ===
        if (!string.IsNullOrEmpty(result.immediateDialogue))
        {
            if (FloatingTextSystem.Instance != null)
            {
                FloatingTextSystem.Instance.ShowText(dialoguePosition, result.immediateDialogue, pressureLevel);
            }

            // フィラーがなかった場合のみTTS発火（フィラー有りの場合は上で既に発火済み）
            if (layerATtsTask == null)
            {
                var llm = cachedLLMManager;
                if (llm != null)
                {
                    layerATtsTask = llm.GenerateTTSAsync(result.immediateDialogue, result.emotion);
                }
            }
        }

        // Layer A TTS再生（5秒タイムアウト付き）
        if (layerATtsTask != null)
        {
            float ttsWait = 0f;
            while (!layerATtsTask.IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }
            if (layerATtsTask.IsCompleted && !layerATtsTask.IsFaulted && layerATtsTask.Result != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopVoice(); // 音声重複防止
                AudioManager.Instance.PlayVoice(layerATtsTask.Result, Vector3.zero, 1.0f);
            }
        }

        // 決着時は Layer A のみ表示してすぐゲーム終了に進む（Layer B/C スキップ）
        if (isGameDecisive)
        {
            yield return new WaitForSeconds(0.5f); // 決着台詞を短く表示
            yield break;
        }

        // === Layer B: LLM感情理由付け（非同期、900msタイムアウト） + TTS ===
        if (BluffSystem.Instance != null)
        {
            BehaviorPattern behavior = null;
            if (PsychologySystem.Instance != null)
            {
                var analyzer = PsychologySystem.Instance.GetComponent<PlayerBehaviorAnalyzer>();
                if (analyzer != null) behavior = analyzer.CurrentBehavior;
            }

            ResponseRequest request = new ResponseRequest
            {
                expectation = result.expectation,
                emotion = result.emotion,
                layer = ResponseLayer.B,
                pressureLevel = pressureLevel,
                playerBehavior = behavior,
                turnCount = BluffSystem.Instance.GetTurnCount(),
                isPlayerTurn = drawCtx.isPlayerTurn,
                isTurningPoint = result.isTurningPoint,
                playerAppearance = cachedLLMManager?.CurrentPlayerAppearance,
                playerBluffSummary = drawCtx.playerBluffSummary  // Stage 16
            };

            System.Threading.Tasks.Task<string> llmTask = BluffSystem.Instance.GenerateResponseAsync(request);
            float elapsed = 0f;
            while (!llmTask.IsCompleted && elapsed < 3.0f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (llmTask.IsCompleted && !llmTask.IsFaulted && !string.IsNullOrEmpty(llmTask.Result))
            {
                // Layer A音声を停止してLayer Bに切替
                AudioManager.Instance?.StopVoice();

                // Layer B: LLM長文はSubtitleUIで画面下部に表示
                if (SubtitleUI.Instance != null)
                {
                    SubtitleUI.Instance.Show(llmTask.Result, pressureLevel);
                }
                else if (FloatingTextSystem.Instance != null)
                {
                    Vector3 layerBPosition = dialoguePosition + Vector3.up * 0.3f;
                    FloatingTextSystem.Instance.ShowText(layerBPosition, llmTask.Result, pressureLevel);
                }

                TriggerMentalistVisualEffects(pressureLevel);

                // Layer B TTS生成＆再生（5秒タイムアウト付き）
                var llm = cachedLLMManager;
                if (llm != null && AudioManager.Instance != null)
                {
                    var layerBTtsTask = llm.GenerateTTSAsync(llmTask.Result, result.emotion);
                    float layerBTtsWait = 0f;
                    while (!layerBTtsTask.IsCompleted && layerBTtsWait < 5f)
                    {
                        layerBTtsWait += Time.deltaTime;
                        yield return null;
                    }
                    if (layerBTtsTask.IsCompleted && !layerBTtsTask.IsFaulted && layerBTtsTask.Result != null)
                    {
                        AudioManager.Instance.PlayVoice(layerBTtsTask.Result, Vector3.zero, 1.0f);
                    }
                }
            }
        }

        // === 高intensity時: カメラリアクション ===
        if (result.reactionIntensity > 0.5f)
        {
            if (cameraSystem != null)
            {
                cameraSystem.ShowAIReactionView();
            }
        }

        // === 圧力レベル調整 ===
        if (result.pressureDelta != 0f && PsychologySystem.Instance != null)
        {
            float newPressure = Mathf.Clamp(
                PsychologySystem.Instance.GetPressureLevel() + result.pressureDelta,
                0f, 3f);
            PsychologySystem.Instance.SetPressureLevel(newPressure);

            // PostProcess心理圧エフェクト適用（0-3を0-1に正規化）
            if (PostProcessingController.Instance != null)
                PostProcessingController.Instance.ApplyPressureEffect(newPressure / 3f);
        }

        // === TTS再生完了を待つ ===
        // 最低でもintensity連動の待機時間は確保
        float waitTime = Mathf.Lerp(0.3f, 0.8f, result.reactionIntensity);
        yield return new WaitForSeconds(waitTime);

        // TTS音声が再生中なら完了まで待つ（状態遷移を遅延）
        if (AudioManager.Instance != null && AudioManager.Instance.IsVoicePlaying())
        {
            yield return new WaitUntil(() => !AudioManager.Instance.IsVoicePlaying());
        }

        // SubtitleUIを非表示 + 音声停止（安全のため）
        AudioManager.Instance?.StopVoice();
        if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
        {
            SubtitleUI.Instance.Hide();
        }

        // リアクション後、元のFPS着席ビューに戻す
        // AIターン時はAI_TURN_REACTで常にAIReactionViewに入るため、intensity問わず復帰必須
        // プレイヤーターン時はCardFocusのままで意図通り（引いたカードを見ている）なので高intensity時のみ復帰
        if (cameraSystem != null && (result.reactionIntensity > 0.5f || !drawCtx.isPlayerTurn))
        {
            if (drawCtx.isPlayerTurn)
                cameraSystem.ShowPlayerTurnView();
            else
                cameraSystem.ShowAITurnView();
        }

        Debug.Log($"[GameManager] Emotional reaction complete: emotion={result.emotion}, " +
                  $"\"{result.immediateDialogue}\" (intensity={result.reactionIntensity:F2})");
    }

    /*
    private void NextTurn()
    {
        currentPlayerTurn = 1 - currentPlayerTurn;
        turnCounter++;

        if (currentPlayerTurn == 0)
        {
            ChangeState(GameState.PlayerTurn);
            if (aiHand != null) aiHand.EnableCardSelection(true);
            Debug.Log("Player's turn.");
        }
        else
        {
            ChangeState(GameState.AITurn);
            if (aiHand != null) aiHand.EnableCardSelection(false);
            Debug.Log("AI's turn.");
            StartCoroutine(AITurnSequence());
        }

        OnTurnChanged?.Invoke(currentPlayerTurn);
    }
    */

    /// <summary>
    /// AIターンの5段階フロー（Phase 4）
    /// </summary>
    private IEnumerator AITurnSequence()
    {
        if (aiHand == null || playerHand == null) yield break;

        // Stage 1: APPROACH（思考開始）
        ChangeState(GameState.AI_TURN_APPROACH);

        // AIブラフモニター開始（AIターン中もブラフアクション実行）
        BluffActionSystem.Instance?.StartAIBluffMonitor();

        // ペアフォーカス中のカメラ上書きを防止
        StopCameraFocusCoroutine();

        // Phase 4: カメラをプレイヤー手札にフォーカス
        if (cameraSystem != null)
        {
            cameraSystem.ShowAITurnView();
        }

        // === AI判断生成（行動分析ベース） ===
        AIDecisionResult aiDecision = null;
        BehaviorPattern aiBehavior = GetMentalistBehavior();
        float aiPressure = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;

        var aiLlm = cachedLLMManager;
        if (aiLlm != null)
        {
            var decisionTask = aiLlm.GenerateAIDecisionAsync(
                aiBehavior ?? new BehaviorPattern(),
                aiPressure,
                playerHand.GetCardCount()
            );

            // タイムアウト付き待機（3秒）
            float decisionWait = 0f;
            while (!decisionTask.IsCompleted && decisionWait < 3f)
            {
                decisionWait += Time.deltaTime;
                yield return null;
            }

            if (decisionTask.IsCompleted && !decisionTask.IsFaulted && decisionTask.Result != null)
            {
                aiDecision = decisionTask.Result;
                // インデックス範囲チェック
                if (aiDecision.selectedCardIndex < 0 || aiDecision.selectedCardIndex >= playerHand.GetCardCount())
                {
                    aiDecision.selectedCardIndex = Random.Range(0, playerHand.GetCardCount());
                }
                Debug.Log($"[GameManager] AI decision: index={aiDecision.selectedCardIndex}, " +
                          $"confidence={aiDecision.confidence:F2}, strategy={aiDecision.strategy}, " +
                          $"cotSteps={aiDecision.cotSteps?.Count ?? 0}");
            }
        }

        // === Stage 13: CoT TTS並列プリ生成 ===
        bool hasCoT = aiDecision?.cotSteps != null && aiDecision.cotSteps.Count > 0;
        Task<AudioClip[]> cotTTSTask = null;
        if (hasCoT && aiLlm != null)
        {
            var cotEmotion = BluffSystem.Instance != null
                ? BluffSystem.Instance.GetCurrentEmotion()
                : AIEmotion.Anticipating;
            cotTTSTask = aiLlm.PreGenerateCoTTTSAsync(aiDecision.cotSteps, cotEmotion);
            Debug.Log($"[GameManager][CoT] Started TTS pre-generation for {aiDecision.cotSteps.Count} steps");
        }

        // === メンタリスト: AIターン理由台詞 (CoTがある場合はスキップ) ===
        if (!hasCoT && mentalistDialogueGen != null)
        {
            string reasonDialogue = mentalistDialogueGen.GetAITurnReasoningDialogue(
                aiBehavior ?? new BehaviorPattern(),
                aiDecision ?? new AIDecisionResult { selectedCardIndex = 0, confidence = 0.5f, strategy = "Neutral" },
                aiPressure,
                turnCounter,
                playerHand.GetCardCount()
            );

            if (!string.IsNullOrEmpty(reasonDialogue))
            {
                isMentalistSpeaking = true;

                // SubtitleUI表示
                if (SubtitleUI.Instance != null)
                {
                    SubtitleUI.Instance.Show(reasonDialogue, aiPressure);
                }

                TriggerMentalistVisualEffects(aiPressure);

                Debug.Log($"[GameManager][Mentalist] AI reason: \"{reasonDialogue}\"");

                // TTS生成 & 再生
                if (aiLlm != null && AudioManager.Instance != null)
                {
                    var emotion = BluffSystem.Instance != null
                        ? BluffSystem.Instance.GetCurrentEmotion()
                        : AIEmotion.Calm;

                    var ttsTask = aiLlm.GenerateTTSAsync(reasonDialogue, emotion);

                    float ttsWait = 0f;
                    while (!ttsTask.IsCompleted && ttsWait < 5f)
                    {
                        ttsWait += Time.deltaTime;
                        yield return null;
                    }

                    if (ttsTask.IsCompleted && !ttsTask.IsFaulted && ttsTask.Result != null)
                    {
                        AudioManager.Instance.TryPlayVoice(ttsTask.Result, Vector3.zero, VoicePriority.Medium, 1.0f);

                        // TTS再生完了を待つ（最大4秒）
                        float maxWait = 4.0f;
                        float waited = 0f;
                        while (AudioManager.Instance.IsVoicePlaying() && waited < maxWait)
                        {
                            waited += Time.deltaTime;
                            yield return null;
                        }
                        AudioManager.Instance.StopVoice();
                    }
                    else if (!ttsTask.IsCompleted)
                    {
                        Debug.LogWarning("[GameManager][Mentalist] AI reason TTS timeout, skipping voice");
                        yield return new WaitForSeconds(1.0f);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                }

                // SubtitleUI非表示
                if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
                {
                    SubtitleUI.Instance.Hide();
                }

                isMentalistSpeaking = false;
            }
        }

        yield return new WaitForSeconds(0.4f);

        // Stage 2: HESITATE（指の迷いアニメーション）
        ChangeState(GameState.AI_TURN_HESITATE);

        // Stage 6: AI迷いアニメーションシステム
        // AIAttentionMarkerがシーンに存在しない場合は自動生成
        if (AIAttentionMarker.Instance == null)
        {
            var markerObj = new GameObject("AIAttentionMarker");
            markerObj.AddComponent<AIAttentionMarker>();
            Debug.Log("[GameManager] AIAttentionMarker auto-created");
        }

        // AIHesitationControllerがaiHandに存在しない場合は自動追加
        AIHesitationController hesitationController = aiHand.GetComponent<AIHesitationController>();
        if (hesitationController == null)
        {
            hesitationController = aiHand.gameObject.AddComponent<AIHesitationController>();
            Debug.Log("[GameManager] AIHesitationController auto-added to aiHand");
        }

        if (playerHand != null)
        {
            var playerCards = playerHand.GetCards();
            float pressureLevel = PsychologySystem.Instance != null
                ? PsychologySystem.Instance.GetPressureLevel()
                : 0f;

            // ゲーム状況コンテキストを構築
            var hesitationCtx = new AIHesitationController.HesitationContext
            {
                aiCardCount = aiHand.GetCardCount(),
                playerCardCount = playerHand.GetCardCount(),
                aiHoldsJoker = aiHand.GetCards().Exists(c => c.cardData.isJoker),
                turnNumber = turnCounter
            };

            // Stage 13: CoT駆動 vs 従来の迷いシーケンス分岐
            if (hasCoT)
            {
                // CoT TTSプリ生成の完了を待機（最大3秒）
                AudioClip[] cotTTSClips = null;
                if (cotTTSTask != null)
                {
                    float ttsWaitElapsed = 0f;
                    while (!cotTTSTask.IsCompleted && ttsWaitElapsed < 3f)
                    {
                        ttsWaitElapsed += Time.deltaTime;
                        yield return null;
                    }
                    if (cotTTSTask.IsCompleted && !cotTTSTask.IsFaulted)
                    {
                        cotTTSClips = cotTTSTask.Result;
                        Debug.Log($"[GameManager][CoT] TTS pre-generation complete");
                    }
                    else
                    {
                        Debug.LogWarning("[GameManager][CoT] TTS pre-generation timeout or failed");
                    }
                }

                yield return StartCoroutine(
                    hesitationController.PlayCoTHesitationSequence(
                        playerCards, aiDecision.cotSteps, cotTTSClips,
                        pressureLevel, hesitationCtx)
                );
            }
            else
            {
                // 従来の迷いシーケンス（フォールバック）
                yield return StartCoroutine(
                    hesitationController.PlayHesitationSequence(playerCards, pressureLevel, hesitationCtx)
                );
            }
        }
        else
        {
            // Fallback: 従来通りの固定待機
            Debug.LogWarning("[GameManager] PlayerHand not found, using fallback delay");
            yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
        }

        // Stage 3: COMMIT（選択確定）
        ChangeState(GameState.AI_TURN_COMMIT);

        // AIブラフモニター停止（カードドロー前に停止）
        BluffActionSystem.Instance?.StopAIBluffMonitor();

        // Stage 5: AI期待決定
        if (BluffSystem.Instance != null)
        {
            AIExpectation expectation = BluffSystem.Instance.DetermineExpectation();
            Debug.Log($"[GameManager] AI expectation (AI turn): {expectation}");
        }

        yield return new WaitForSeconds(0.15f);

        // Stage 4: DRAW（引くアニメーション）
        ChangeState(GameState.AI_TURN_DRAW);

        // PostProcessフォーカスエフェクト
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.ApplyFocusEffect();

        // AIがプレイヤーからカードを引く
        if (playerHand.GetCardCount() > 0)
        {
            int selectedIndex = aiDecision != null
                ? aiDecision.selectedCardIndex
                : Random.Range(0, playerHand.GetCardCount());
            CardObject drawnCard = playerHand.RemoveCard(selectedIndex);
            if (drawnCard != null)
            {
                aiHand.AddCard(drawnCard);
                Debug.Log($"AI drew a card from player. Index: {selectedIndex}");

                // カードドローSFX
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayCardFlip();

                // Jokerを引いた場合、即座にシャッフル（心理的ブラフ）
                if (drawnCard.cardData.isJoker)
                {
                    yield return new WaitForSeconds(0.5f); // 少し間を置いてから

                    // シャッフル台詞を表示
                    var loc = LocalizationManager.Instance;
                    if (loc != null)
                    {
                        string[] shuffleDialogues = loc.GetArray("bluff_action.ai_shuffle_after_joker_draw");
                        if (shuffleDialogues != null && shuffleDialogues.Length > 0)
                        {
                            string dialogue = shuffleDialogues[Random.Range(0, shuffleDialogues.Length)];
                            if (SubtitleUI.Instance != null)
                            {
                                float pressureLevel = PsychologySystem.Instance != null
                                    ? PsychologySystem.Instance.GetPressureLevel()
                                    : 0f;
                                SubtitleUI.Instance.Show(dialogue, pressureLevel);
                            }
                        }
                    }

                    // シャッフル実行
                    if (aiHand != null)
                    {
                        aiHand.ShuffleCards();
                    }

                    Debug.Log("[GameManager] AI drew Joker and shuffled cards immediately");
                    yield return new WaitForSeconds(0.8f); // シャッフルアニメーション完了待ち
                }
            }
        }

        yield return new WaitForSeconds(0.35f);

        // === AI Post-Draw Comment ===
        if (mentalistDialogueGen != null && aiHand != null)
        {
            var aiCards = aiHand.GetCards();
            CardObject lastDrawnForComment = aiCards.Count > 0 ? aiCards[aiCards.Count - 1] : null;
            bool commentDrawnIsJoker = lastDrawnForComment != null && lastDrawnForComment.cardData.isJoker;
            bool commentWillFormPair = false;
            if (lastDrawnForComment != null)
            {
                foreach (var handCard in aiCards)
                {
                    if (handCard != lastDrawnForComment && handCard.cardData.IsMatchingPair(lastDrawnForComment.cardData))
                    {
                        commentWillFormPair = true;
                        break;
                    }
                }
            }

            // Stage 12: 表情データ付きBehaviorPatternを渡す
            string drawComment = mentalistDialogueGen.GetAIDrawComment(commentDrawnIsJoker, commentWillFormPair, GetMentalistBehavior());
            if (!string.IsNullOrEmpty(drawComment))
            {
                if (SubtitleUI.Instance != null)
                {
                    float drawPressure = PsychologySystem.Instance?.GetPressureLevel() ?? 0f;
                    SubtitleUI.Instance.Show(drawComment, drawPressure);
                }

                Debug.Log($"[GameManager][Mentalist] AI draw comment: \"{drawComment}\"");
                yield return new WaitForSeconds(0.8f);

                if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
                {
                    SubtitleUI.Instance.Hide();
                }
            }
        }

        // Stage 5: REACT（リアクション）
        ChangeState(GameState.AI_TURN_REACT);

        // Phase 4: カメラをAIの顔/反応にフォーカス
        if (cameraSystem != null)
        {
            cameraSystem.ShowAIReactionView();
        }

        // Stage 5: 感情リアクション評価（予測 vs 現実）
        if (BluffSystem.Instance != null && aiHand != null)
        {
            var aiCards = aiHand.GetCards();
            CardObject lastDrawn = aiCards.Count > 0 ? aiCards[aiCards.Count - 1] : null;
            bool drawnIsJoker = lastDrawn != null && lastDrawn.cardData.isJoker;
            bool formedPair = false;
            if (lastDrawn != null)
            {
                foreach (var handCard in aiCards)
                {
                    if (handCard != lastDrawn && handCard.cardData.IsMatchingPair(lastDrawn.cardData))
                    {
                        formedPair = true;
                        break;
                    }
                }
            }

            bool aiHasJokerNow = false;
            foreach (var c in aiCards)
                if (c.cardData.isJoker) { aiHasJokerNow = true; break; }

            // Stage 16: Get player bluff behavior summary
            string bluffSummary2 = "";
            var behaviorAnalyzer2 = FindFirstObjectByType<FPSTrump.Psychology.PlayerBehaviorAnalyzer>();
            if (behaviorAnalyzer2 != null)
            {
                bluffSummary2 = behaviorAnalyzer2.GetBluffBehaviorSummary();
            }

            DrawContext drawCtx = new DrawContext
            {
                isPlayerTurn = false,
                drawnCardIsJoker = drawnIsJoker,
                formedPair = formedPair,
                remainingCards = aiHand.GetCardCount(),
                opponentRemainingCards = playerHand.GetCardCount(),
                aiHoldsJoker = aiHasJokerNow,
                playerBluffSummary = bluffSummary2
            };

            // EvaluateReaction is now async, await it via Task
            System.Threading.Tasks.Task<EmotionalResult> evaluateTask = BluffSystem.Instance.EvaluateReaction(drawCtx);
            yield return new WaitUntil(() => evaluateTask.IsCompleted);
            EmotionalResult result = evaluateTask.Result;
            yield return StartCoroutine(PlayEmotionalReaction(result, drawCtx));
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Stage 6: RESOLVE（ペア判定・勝敗判定）
        yield return StartCoroutine(AITurnResolve());
    }

    /// <summary>
    /// AIターンのリゾルブ処理
    /// </summary>
    private IEnumerator AITurnResolve()
    {
        ChangeState(GameState.AI_TURN_RESOLVE);
        turnCounter++;

        // ペア判定（AIは自動でペアを消去）
        bool pairFormed = false;
        if (aiHand != null)
        {
            int pairsFormed = aiHand.CheckForPairs();
            pairFormed = pairsFormed > 0;
            Debug.Log($"AI pairs formed: {pairsFormed}");

            // ペアマッチSFX
            if (pairFormed && AudioManager.Instance != null)
                AudioManager.Instance.PlayCardPlace();

            // Stage 7: ペア記録
            if (pairFormed && GameSessionRecorder.Instance != null)
                GameSessionRecorder.Instance.RecordPairFormed();
        }

        // Stage 7: ターン記録
        RecordCurrentTurn(isPlayerTurn: false, pairFormed);

        // 心臓音の強度更新
        UpdateHeartbeat();

        yield return new WaitForSeconds(0.3f);

        // ペアフォーカスコルーチン完了を待機（1.0s演出が途中で切られるのを防止）
        if (currentCameraFocusCoroutine != null)
        {
            yield return currentCameraFocusCoroutine;
        }

        // 勝敗判定
        if (CheckGameEndConditions())
        {
            yield break;
        }

        // 次のターン（プレイヤーターン）へ
        StartCoroutine(PlayerTurnSequence());
    }

    private bool CheckGameEndConditions()
    {
        if (playerHand == null || aiHand == null) return false;

        // プレイヤーの手札が0枚 → プレイヤー勝利
        if (playerHand.GetCardCount() == 0)
        {
            EndGame("Player");
            return true;
        }

        // AIの手札が0枚 → AI勝利
        if (aiHand.GetCardCount() == 0)
        {
            EndGame("AI");
            return true;
        }

        // プレイヤーがジョーカーのみ残り → AI勝利
        if (playerHand.GetCardCount() == 1)
        {
            var cards = playerHand.GetCards();
            if (cards.Count > 0 && cards[0].cardData.isJoker)
            {
                EndGame("AI");
                return true;
            }
        }

        // AIがジョーカーのみ残り → プレイヤー勝利
        if (aiHand.GetCardCount() == 1)
        {
            var cards = aiHand.GetCards();
            if (cards.Count > 0 && cards[0].cardData.isJoker)
            {
                EndGame("Player");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Stage 7: 現在のターンをセッションレコーダーに記録
    /// </summary>
    private void RecordCurrentTurn(bool isPlayerTurn, bool pairFormed)
    {
        if (GameSessionRecorder.Instance == null) return;

        float pressureLevel = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;

        BehaviorPattern behavior = null;
        if (PsychologySystem.Instance != null)
        {
            var analyzer = PsychologySystem.Instance.GetComponent<PlayerBehaviorAnalyzer>();
            if (analyzer != null) behavior = analyzer.CurrentBehavior;
        }

        AIEmotion emotion = BluffSystem.Instance != null
            ? BluffSystem.Instance.GetCurrentEmotion()
            : AIEmotion.Calm;

        // Stage 14: カード数とJoker保持情報を記録
        int playerCards = playerHand != null ? playerHand.GetCardCount() : 0;
        int aiCards = aiHand != null ? aiHand.GetCardCount() : 0;
        bool aiHasJoker = aiHand != null && aiHand.GetCards().Exists(c => c.cardData.isJoker);

        // Stage 15: ブラフ・ホバーパターン情報を記録
        int bluffCount = BluffActionSystem.Instance != null ? BluffActionSystem.Instance.GetRecentPlayerBluffCount(1) : 0;
        string dominantBluff = "";
        if (BluffActionSystem.Instance != null && bluffCount > 0)
        {
            var mostUsed = BluffActionSystem.Instance.GetMostUsedPlayerBluffType();
            dominantBluff = mostUsed.ToString();
        }
        // TODO: BehaviorPatternにhoverEventCount/backAndForthCountを追加するか、別途取得
        int hoverEvents = 0; // 将来的にPlayerBehaviorAnalyzerから取得
        float avgHoverDur = behavior?.avgHoverTime ?? 0f;

        TurnRecord record = new TurnRecord
        {
            turnNumber = turnCounter,
            isPlayerTurn = isPlayerTurn,
            decisionTime = behavior?.avgDecisionTime ?? 0f,
            hoverTime = behavior?.avgHoverTime ?? 0f,
            selectedPosition = isPlayerTurn ? (behavior?.preferredPosition ?? -1) : -1,
            pressureLevelAtTurn = pressureLevel,
            tempoAtTurn = behavior?.tempo ?? TempoType.Normal,
            emotionAfterTurn = emotion,
            reactionIntensity = 0f,
            wasTurningPoint = BluffSystem.Instance != null &&
                BluffSystem.Instance.GetCurrentEmotion() == AIEmotion.Frustrated,
            formedPair = pairFormed,
            playerCardCount = playerCards,
            aiCardCount = aiCards,
            aiHeldJoker = aiHasJoker,
            bluffActionCount = bluffCount,
            dominantBluffType = dominantBluff,
            hoverEventCount = hoverEvents,
            avgHoverDuration = avgHoverDur,
            backAndForthCount = 0  // TODO: 実装後に追加
        };

        GameSessionRecorder.Instance.RecordTurn(record);

        // ナラティブ文脈蓄積（プレイヤーターンのみ）
        if (isPlayerTurn)
        {
            string narrativeEntry = BuildNarrativeEntry(behavior, pairFormed, pressureLevel);
            if (!string.IsNullOrEmpty(narrativeEntry))
            {
                narrativeSummary.AppendLine(narrativeEntry);
            }
        }
    }

    /// <summary>
    /// ターンのナラティブエントリを構築
    /// </summary>
    private string BuildNarrativeEntry(BehaviorPattern behavior, bool pairFormed, float pressureLevel)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (behavior != null)
        {
            if (behavior.hasPositionPreference)
            {
                string posName = behavior.preferredPosition == 0 ? "left"
                    : behavior.preferredPosition == 1 ? "center" : "right";
                parts.Add($"position bias toward {posName}");
            }
            if (behavior.tempo == TempoType.Fast)
                parts.Add("fast tempo");
            else if (behavior.tempo == TempoType.Slow)
                parts.Add("slow tempo");
            if (behavior.doubtLevel > 0.6f)
                parts.Add("high doubt");
        }
        if (pairFormed)
            parts.Add("pair formed");
        if (pressureLevel > 2.0f)
            parts.Add("under high pressure");

        if (parts.Count == 0) return null;
        return $"Turn {turnCounter}: Player {string.Join(", ", parts)}.";
    }

    /// <summary>
    /// ナラティブ文脈サマリーを取得（LLMプロンプト注入用）
    /// </summary>
    public string GetNarrativeSummary()
    {
        return narrativeSummary.Length > 0 ? narrativeSummary.ToString() : null;
    }

    private void EndGame(string winner)
    {
        isGameActive = false;

        // BGM・心臓音停止
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            AudioManager.Instance.StopHeartbeat();
        }

        // PostProcess心理圧エフェクト解除
        if (PostProcessingController.Instance != null)
            PostProcessingController.Instance.ReleasePressureEffect();

        // ブラフアクションUI非表示 & モニター停止
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.StopAIBluffMonitor();
        if (BluffActionUI.Instance != null)
            BluffActionUI.Instance.Hide();

        Debug.Log($"Game Over! Winner: {winner}");
        OnGameEnded?.Invoke(winner);

        // Result診断表示
        StartCoroutine(ShowResultDiagnosis(winner));
    }

    /// <summary>
    /// 心臓音の強度をゲーム状況に応じて更新
    /// </summary>
    private void UpdateHeartbeat()
    {
        if (AudioManager.Instance == null) return;

        int totalRemaining = (playerHand?.GetCardCount() ?? 0) + (aiHand?.GetCardCount() ?? 0);
        float pressureLevel = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;

        // 残りカード数と圧力レベルから強度算出
        float intensity = Mathf.Clamp01(1f - (totalRemaining / 20f) + pressureLevel * 0.3f);
        AudioManager.Instance.PlayHeartbeat(intensity, totalRemaining);
    }

    /// <summary>
    /// Result診断表示（Stage 7 + Outro演出）
    /// </summary>
    private IEnumerator ShowResultDiagnosis(string winner)
    {
        bool playerWon = winner == "Player";
        Debug.Log($"=== GAME RESULT ===\nWinner: {winner}\nTurns: {turnCounter}");

        // テーブル全景カメラでジョーカー公開演出
        if (cameraSystem != null)
        {
            cameraSystem.ShowTableOverview();
        }

        // セッションデータ取得 + 診断生成を即座に開始（テーブル俯瞰と並行）
        GameSessionData sessionData = null;
        if (GameSessionRecorder.Instance != null)
        {
            sessionData = GameSessionRecorder.Instance.FinalizeSession(playerWon);
        }
        else
        {
            Debug.LogWarning("[GameManager] GameSessionRecorder not found, using empty session data");
            sessionData = new GameSessionData { playerWon = playerWon, totalTurns = turnCounter };
        }

        PersonalityProfile baseProfile = null;
        var llmManager = cachedLLMManager;
        if (llmManager != null && llmManager.CurrentPlayerProfile != null)
        {
            baseProfile = llmManager.CurrentPlayerProfile;
            Debug.Log("[GameManager] PersonalityProfile retrieved for diagnosis comparison");
        }

        // ハイブリッド方式: 診断を即座に生成（静的フォールバック + LLM詳細分析はバックグラウンドで開始）
        DiagnosisResult diagnosis = null;
        if (ResultDiagnosisSystem.Instance != null)
        {
            diagnosis = ResultDiagnosisSystem.Instance.GenerateDiagnosisImmediate(sessionData, baseProfile);
            Debug.Log("[GameManager] Immediate diagnosis generated (LLM detailed analysis running in background)");
        }
        else
        {
            Debug.LogWarning("[GameManager] ResultDiagnosisSystem not found");
        }

        // テーブル俯瞰を2秒見せる（この間にLLM詳細分析が進む）
        yield return new WaitForSeconds(2f);

        // Diagnostic logging before outro sequence
        Debug.Log($"[GameManager] Pre-outro status check: " +
                  $"GameOutroSequence={(GameOutroSequence.Instance != null ? "OK" : "NULL")}, " +
                  $"diagnosis={(diagnosis != null ? "OK" : "NULL")}");

        // === OUTRO演出: AIが性格を口頭で暴く ===
        ChangeState(GameState.OUTRO);

        if (GameOutroSequence.Instance != null && diagnosis != null)
        {
            yield return StartCoroutine(
                GameOutroSequence.Instance.PlayOutroSequence(diagnosis, sessionData));
        }
        else
        {
            // Detailed component status reporting
            if (GameOutroSequence.Instance == null)
                Debug.LogWarning("[GameManager] OUTRO SKIPPED: GameOutroSequence.Instance is null");
            if (diagnosis == null)
                Debug.LogError("[GameManager] OUTRO SKIPPED: diagnosis is null (THIS SHOULD NEVER HAPPEN - check fallback logic)");

            yield return new WaitForSeconds(1f);
        }

        // === RESULT状態へ遷移 ===
        ChangeState(GameState.RESULT);

        // resultUI が Inspector 未アサインの場合、自動取得
        if (resultUI == null)
        {
            resultUI = ResultUI.Instance;
        }
        if (resultUI == null)
        {
            resultUI = FindFirstObjectByType<ResultUI>();
        }

        if (diagnosis == null)
        {
            Debug.LogWarning("[GameManager] diagnosis is null, skipping result display");
        }
        if (resultUI == null)
        {
            Debug.LogWarning("[GameManager] resultUI is null, skipping result display");
        }

        if (diagnosis != null && resultUI != null)
        {
            resultUI.OnReplayRequested = () =>
            {
                Debug.Log("[GameManager] OnReplayRequested callback invoked");
                resultUI.Hide();
                Debug.Log("[GameManager] Starting new game...");
                StartNewGame();
            };
            resultUI.OnMenuRequested = () =>
            {
                Debug.Log("[GameManager] OnMenuRequested callback invoked");
                resultUI.Hide();
                ChangeState(GameState.GameEnd);
            };

            resultUI.ShowDiagnosis(diagnosis, sessionData);
            Debug.Log($"[GameManager] Diagnosis shown: {diagnosis.personalityTitle} " +
                      $"(LLM={diagnosis.isLLMGenerated})");
        }
        else
        {
            yield return new WaitForSeconds(3f);
            ChangeState(GameState.GameEnd);
        }
    }

    private void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;

        // Stage 10: 表情分析のキーモーメント切替
        bool isKeyMoment = newState == GameState.PLAYER_TURN_PICK
                        || newState == GameState.PLAYER_TURN_CONFIRM;
        FacialExpressionAnalyzer.Instance?.SetKeyMoment(isKeyMoment);

        Debug.Log($"Game State: {previousState} → {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// カメラフォーカスコルーチンを安全に停止（focusPointリーク防止）
    /// </summary>
    private void StopCameraFocusCoroutine()
    {
        if (currentCameraFocusCoroutine != null)
        {
            StopCoroutine(currentCameraFocusCoroutine);
            currentCameraFocusCoroutine = null;
        }
        if (currentFocusPoint != null)
        {
            Destroy(currentFocusPoint);
            currentFocusPoint = null;
        }
    }

    /// <summary>
    /// プレイヤーのペア削除時
    /// </summary>
    private void OnPlayerPairMatched(CardObject card1, CardObject card2)
    {
        if (cameraSystem == null) return;

        StopCameraFocusCoroutine();

        // ペアにフォーカス → 元のビューに戻る
        currentCameraFocusCoroutine = StartCoroutine(
            FocusOnPairAndReturn(card1, card2, isPlayerTurn: true)
        );
    }

    /// <summary>
    /// AIのペア削除時
    /// </summary>
    private void OnAIPairMatched(CardObject card1, CardObject card2)
    {
        if (cameraSystem == null) return;

        StopCameraFocusCoroutine();

        currentCameraFocusCoroutine = StartCoroutine(
            FocusOnPairAndReturn(card1, card2, isPlayerTurn: false)
        );
    }

    /// <summary>
    /// ペアにフォーカスして元のビューに戻す
    /// </summary>
    private IEnumerator FocusOnPairAndReturn(CardObject card1, CardObject card2, bool isPlayerTurn)
    {
        // ペアの中心点を計算
        Vector3 centerPosition = (card1.transform.position + card2.transform.position) * 0.5f;

        // 一時的なフォーカスポイントを作成（フィールドに保持して中断時もDestroy可能に）
        currentFocusPoint = new GameObject("_TempCardPairFocus");
        currentFocusPoint.transform.position = centerPosition;

        // カメラフォーカス
        cameraSystem.FocusOnCard(currentFocusPoint.transform);

        // 1.0秒間フォーカスを維持
        yield return new WaitForSeconds(1.0f);

        // focusPoint を削除
        if (currentFocusPoint != null)
        {
            Destroy(currentFocusPoint);
            currentFocusPoint = null;
        }

        // 元のビューに戻す
        if (isPlayerTurn)
        {
            cameraSystem.ShowPlayerTurnView();
        }
        else
        {
            cameraSystem.ShowAITurnView();
        }

        currentCameraFocusCoroutine = null;
    }

    // ====================
    // Mentalist Dialogue System
    // ====================

    /// <summary>
    /// ターン開始時のメンタリスト挑発を再生
    /// テンプレートベース → FloatingText + TTS
    /// </summary>
    private IEnumerator PlayTurnStartProvocation()
    {
        if (mentalistDialogueGen == null) yield break;

        BehaviorPattern behavior = GetMentalistBehavior();
        float pressureLevel = PsychologySystem.Instance != null
            ? PsychologySystem.Instance.GetPressureLevel()
            : 0f;

        string dialogue = mentalistDialogueGen.GetTurnStartDialogue(
            behavior ?? new BehaviorPattern(),
            pressureLevel,
            turnCounter
        );
        if (string.IsNullOrEmpty(dialogue)) yield break;

        isMentalistSpeaking = true;

        // FloatingText表示（カメラ前方に配置）
        Camera cam = Camera.main;
        Vector3 dialoguePosition = cam != null
            ? cam.transform.position + cam.transform.forward * 2f + cam.transform.up * -0.1f
            : Vector3.up * 1.5f;

        if (FloatingTextSystem.Instance != null)
        {
            FloatingTextSystem.Instance.ShowText(dialoguePosition, dialogue, pressureLevel);
        }

        TriggerMentalistVisualEffects(pressureLevel);

        Debug.Log($"[GameManager][Mentalist] Turn-start: \"{dialogue}\"");

        // TTS生成 & 再生（タイムアウト付き）
        var llm = cachedLLMManager;
        if (llm != null && AudioManager.Instance != null)
        {
            var emotion = BluffSystem.Instance != null
                ? BluffSystem.Instance.GetCurrentEmotion()
                : AIEmotion.Calm;

            var ttsTask = llm.GenerateTTSAsync(dialogue, emotion);

            // TTS生成タイムアウト: 5秒
            float ttsWait = 0f;
            while (!ttsTask.IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (ttsTask.IsCompleted && !ttsTask.IsFaulted && ttsTask.Result != null)
            {
                AudioManager.Instance.TryPlayVoice(ttsTask.Result, Vector3.zero, VoicePriority.Medium, 1.0f);

                // TTS再生完了を待つ（最大3秒）
                float maxWait = 3.0f;
                float waited = 0f;
                while (AudioManager.Instance.IsVoicePlaying() && waited < maxWait)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                AudioManager.Instance.StopVoice();
            }
            else if (!ttsTask.IsCompleted)
            {
                Debug.LogWarning("[GameManager][Mentalist] TTS generation timeout (5s), skipping voice");
                // テキストは既に表示済みなので短い待機のみ
                yield return new WaitForSeconds(1.0f);
            }
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        isMentalistSpeaking = false;
    }

    /// <summary>
    /// 長考時の煽りコルーチン
    /// PLAYER_TURN_PICK中、7秒後に1回目、19秒後に2回目（最大2回/ターン）
    /// </summary>
    private IEnumerator IdleCommentaryLoop()
    {
        if (mentalistDialogueGen == null) yield break;

        float[] triggerTimings = { 7.0f, 19.0f };
        int idleCount = 0;

        while (idleCount < 2 && currentState == GameState.PLAYER_TURN_PICK)
        {
            // 次のトリガー時刻まで待機
            float targetTime = triggerTimings[idleCount];
            yield return new WaitUntil(() =>
                (Time.time - playerTurnStartTime >= targetTime) ||
                currentState != GameState.PLAYER_TURN_PICK
            );

            if (currentState != GameState.PLAYER_TURN_PICK) yield break;

            // メンタリスト発話中なら待機
            if (isMentalistSpeaking)
            {
                yield return new WaitUntil(() => !isMentalistSpeaking);
                if (currentState != GameState.PLAYER_TURN_PICK) yield break;
            }

            float pressureLevel = PsychologySystem.Instance != null
                ? PsychologySystem.Instance.GetPressureLevel()
                : 0f;

            // Stage 12: 表情データ付きBehaviorPatternを取得
            BehaviorPattern idleBehavior = GetMentalistBehavior();

            string dialogue = mentalistDialogueGen.GetIdleTauntDialogue(pressureLevel, idleCount, idleBehavior);
            if (string.IsNullOrEmpty(dialogue))
            {
                idleCount++;
                continue;
            }

            isMentalistSpeaking = true;

            // SubtitleUI表示
            if (SubtitleUI.Instance != null)
            {
                SubtitleUI.Instance.Show(dialogue, pressureLevel);
            }

            TriggerMentalistVisualEffects(pressureLevel);

            Debug.Log($"[GameManager][Mentalist] Idle taunt #{idleCount + 1}: \"{dialogue}\"");

            // TTS生成 & 再生（タイムアウト付き）
            var llm = cachedLLMManager;
            if (llm != null && AudioManager.Instance != null)
            {
                var emotion = BluffSystem.Instance != null
                    ? BluffSystem.Instance.GetCurrentEmotion()
                    : AIEmotion.Calm;

                var ttsTask = llm.GenerateTTSAsync(dialogue, emotion);

                // TTS生成タイムアウト: 5秒
                float ttsWait = 0f;
                while (!ttsTask.IsCompleted && ttsWait < 5f
                       && currentState == GameState.PLAYER_TURN_PICK)
                {
                    ttsWait += Time.deltaTime;
                    yield return null;
                }

                if (ttsTask.IsCompleted && !ttsTask.IsFaulted
                    && ttsTask.Result != null && currentState == GameState.PLAYER_TURN_PICK)
                {
                    AudioManager.Instance.TryPlayVoice(ttsTask.Result, Vector3.zero, VoicePriority.Medium, 1.0f);

                    float maxWait = 4.0f;
                    float waited = 0f;
                    while (AudioManager.Instance.IsVoicePlaying() && waited < maxWait
                           && currentState == GameState.PLAYER_TURN_PICK)
                    {
                        waited += Time.deltaTime;
                        yield return null;
                    }
                }
                else if (!ttsTask.IsCompleted)
                {
                    Debug.LogWarning("[GameManager][Mentalist] Idle TTS timeout, skipping voice");
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            // SubtitleUI非表示
            if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
            {
                SubtitleUI.Instance.Hide();
            }
            AudioManager.Instance?.StopVoice();

            isMentalistSpeaking = false;
            idleCount++;
        }
    }

    /// <summary>
    /// メンタリスト発話時の視覚効果をトリガー
    /// </summary>
    private void TriggerMentalistVisualEffects(float pressureLevel)
    {
        if (PostProcessingController.Instance != null)
        {
            PostProcessingController.Instance.ApplyDialogueVisualEffect(pressureLevel);
        }
        if (pressureLevel >= 1.0f && SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
        {
            float wobbleStrength = Mathf.Clamp01((pressureLevel - 1.0f) / 2.0f);
            SubtitleUI.Instance.StartWobble(wobbleStrength);
        }
    }

    /// <summary>
    /// アイドルコメンタリーを停止
    /// </summary>
    private void StopIdleCommentary()
    {
        if (idleCommentaryCoroutine != null)
        {
            StopCoroutine(idleCommentaryCoroutine);
            idleCommentaryCoroutine = null;
        }

        if (isMentalistSpeaking)
        {
            AudioManager.Instance?.StopVoice();
            if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
            {
                SubtitleUI.Instance.Hide();
            }
            isMentalistSpeaking = false;
        }
    }

    /// <summary>
    /// フィラーTTSプールを非同期プリ生成
    /// </summary>
    private async Task PregenFillerPool()
    {
        if (cachedLLMManager == null) return;
        fillerTTSPool = new AudioClip[fillerTexts.Length];
        var tasks = new Task<AudioClip>[fillerTexts.Length];
        for (int i = 0; i < fillerTexts.Length; i++)
            tasks[i] = cachedLLMManager.GenerateTTSAsync(fillerTexts[i], AIEmotion.Calm);
        for (int i = 0; i < tasks.Length; i++)
        {
            try { fillerTTSPool[i] = await tasks[i]; }
            catch { fillerTTSPool[i] = null; }
        }
        int ready = 0;
        foreach (var c in fillerTTSPool) if (c != null) ready++;
        Debug.Log($"[GameManager] Filler TTS pool ready: {ready}/{fillerTTSPool.Length}");
    }

    /// <summary>
    /// PlayerBehaviorAnalyzer.CurrentBehaviorを安全に取得
    /// </summary>
    private BehaviorPattern GetMentalistBehavior()
    {
        if (PsychologySystem.Instance == null) return null;
        var analyzer = PsychologySystem.Instance.GetComponent<PlayerBehaviorAnalyzer>();
        return analyzer?.CurrentBehavior;
    }

    /// <summary>
    /// AIジョーカーティーズ: カードを持ち上げて挑発
    /// 50%の確率で本物のJoker、50%でブラフ
    /// </summary>
    private IEnumerator PlayJokerTease()
    {
        if (aiHand == null || mentalistDialogueGen == null) yield break;

        var aiCards = aiHand.GetCards();
        if (aiCards.Count < 2) yield break;

        // Jokerを探す
        CardObject teaseCard = null;
        bool isRealJoker = false;

        foreach (var card in aiCards)
        {
            if (card.cardData.isJoker)
            {
                teaseCard = card;
                isRealJoker = true;
                break;
            }
        }

        // 50%で本物Joker、50%でブラフ（ランダムカード）
        if (teaseCard != null && Random.value < 0.5f)
        {
            // 本物Jokerを見せる
        }
        else
        {
            teaseCard = aiCards[Random.Range(0, aiCards.Count)];
            isRealJoker = false;
        }

        if (teaseCard == null) yield break;

        isMentalistSpeaking = true;

        // カードを持ち上げるアニメーション
        Vector3 originalLocalPos = teaseCard.transform.localPosition;
        Quaternion originalLocalRot = teaseCard.transform.localRotation;

        teaseCard.transform.DOLocalMoveY(originalLocalPos.y + 0.15f, 0.4f).SetEase(Ease.OutQuad);
        teaseCard.transform.DOLocalRotate(
            new Vector3(originalLocalRot.eulerAngles.x - 10f, originalLocalRot.eulerAngles.y, originalLocalRot.eulerAngles.z + 5f),
            0.4f).SetEase(Ease.OutQuad);

        // 挑発台詞表示
        string taunt = mentalistDialogueGen.GetJokerTeaseDialogue(isRealJoker);
        float pressureLevel = PsychologySystem.Instance?.GetPressureLevel() ?? 0f;

        if (SubtitleUI.Instance != null)
        {
            SubtitleUI.Instance.Show(taunt, pressureLevel);
        }

        Debug.Log($"[GameManager][Mentalist] Joker tease (real={isRealJoker}): \"{taunt}\"");

        // TTS生成 & 再生
        var llm = cachedLLMManager;
        if (llm != null && AudioManager.Instance != null)
        {
            var emotion = BluffSystem.Instance?.GetCurrentEmotion() ?? AIEmotion.Pleased;
            var ttsTask = llm.GenerateTTSAsync(taunt, emotion);

            float ttsWait = 0f;
            while (!ttsTask.IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (ttsTask.IsCompleted && !ttsTask.IsFaulted && ttsTask.Result != null)
            {
                AudioManager.Instance.TryPlayVoice(ttsTask.Result, Vector3.zero, VoicePriority.Medium, 1.0f);

                float waited = 0f;
                while (AudioManager.Instance.IsVoicePlaying() && waited < 3f)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
                AudioManager.Instance.StopVoice();
            }
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // SubtitleUI非表示
        if (SubtitleUI.Instance != null && SubtitleUI.Instance.IsVisible())
        {
            SubtitleUI.Instance.Hide();
        }

        // カードを元に戻す
        teaseCard.transform.DOLocalMove(originalLocalPos, 0.3f).SetEase(Ease.InQuad);
        teaseCard.transform.DOLocalRotateQuaternion(originalLocalRot, 0.3f).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(0.3f);

        isMentalistSpeaking = false;
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        // F1キーでデバッグUI表示切替
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
        {
            showDebugUI = !showDebugUI;
            Event.current.Use();
        }

        if (!showDebugUI) return;

        // Debug Info Box
        GUI.Box(new Rect(10, 10, 200, 120), "Debug Info");
        GUI.Label(new Rect(20, 30, 180, 20), $"State: {currentState}");
        GUI.Label(new Rect(20, 50, 180, 20), $"Turn: {turnCounter}");
        GUI.Label(new Rect(20, 70, 180, 20), $"Player Cards: {playerHand?.GetCardCount() ?? 0}");
        GUI.Label(new Rect(20, 90, 180, 20), $"AI Cards: {aiHand?.GetCardCount() ?? 0}");
        GUI.Label(new Rect(20, 110, 180, 20), $"Discarded: {discardPile?.Count ?? 0}");

        // Turn Indicator (Center Top) - Phase 4対応
        if (isGameActive)
        {
            GUIStyle turnStyle = new GUIStyle(GUI.skin.label);
            turnStyle.fontSize = 32;
            turnStyle.fontStyle = FontStyle.Bold;
            turnStyle.alignment = TextAnchor.MiddleCenter;

            string turnText = "";
            Color turnColor = Color.white;

            // プレイヤーターン（PICK, INTERRUPT, CONFIRM, COMMIT, DRAW, POST_REACT, RESOLVE）
            if (currentState == GameState.PLAYER_TURN_PICK)
            {
                turnText = "YOUR TURN - Click AI card to draw";
                turnColor = new Color(0.2f, 1f, 0.2f); // Bright green
            }
            else if (currentState == GameState.PLAYER_TURN_CONFIRM)
            {
                turnText = "CONFIRM - Draw or Abort?";
                turnColor = new Color(1f, 1f, 0.2f); // Yellow
            }
            else if (currentState >= GameState.PLAYER_TURN_INTERRUPT && currentState <= GameState.PLAYER_TURN_RESOLVE)
            {
                turnText = "YOUR TURN - Processing...";
                turnColor = new Color(0.2f, 1f, 0.2f);
            }
            // AIターン（APPROACH, HESITATE, COMMIT, DRAW, REACT, RESOLVE）
            else if (currentState >= GameState.AI_TURN_APPROACH && currentState <= GameState.AI_TURN_RESOLVE)
            {
                turnText = "AI TURN - Thinking...";
                turnColor = new Color(1f, 0.3f, 0.3f); // Bright red
            }

            GUI.color = turnColor;
            GUI.Label(new Rect(Screen.width / 2 - 300, 20, 600, 40), turnText, turnStyle);
            GUI.color = Color.white;
        }
    }
#endif
}
