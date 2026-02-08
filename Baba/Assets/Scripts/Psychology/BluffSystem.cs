using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using FPSTrump.AI.LLM;

namespace FPSTrump.Psychology
{
    /// <summary>
    /// 感情AIシステム（旧BluffSystem）
    /// 「AIはカードを見ていない。でもプレイヤーを信じたり、裏切られたりする」
    /// 感情は「予測 vs 現実のギャップ」から生まれる
    /// </summary>
    public class BluffSystem : MonoBehaviour
    {
        public static BluffSystem Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private PsychologySystem psychologySystem;
        [SerializeField] private PlayerBehaviorAnalyzer behaviorAnalyzer;
        [SerializeField] private FloatingTextSystem floatingTextSystem;
        [SerializeField] private LLMManager llmManager;

        [Header("Expectation Settings")]
        [SerializeField] [Range(0f, 1f)] public float baseExpectationChance = 0.3f;
        [SerializeField] [Range(0f, 1f)] public float maxExpectationChance = 0.7f;
        [SerializeField] public int turnsBeforeExpectation = 2;

        // 内部状態
        private AIEmotion currentEmotion = AIEmotion.Calm;
        private AIExpectation currentExpectation = AIExpectation.Neutral;
        private int turnCount = 0;
        private List<EmotionalResult> reactionHistory = new List<EmotionalResult>();

        // Layer A 静的セリフテーブル
        private Dictionary<AIEmotion, string[]> emotionDialogues;

        // 感情トリガー説明テーブル（LLMプロンプト用: カード情報を含まない）
        private Dictionary<AIEmotion, string[]> emotionTriggerDescriptions;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeDialogues();
            InitializeEmotionTriggers();
        }

        private void Start()
        {
            ValidateDependencies();
        }

        private void OnEnable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameSettings.GameLanguage newLanguage)
        {
            InitializeDialogues();
            InitializeEmotionTriggers();
        }

        private void ValidateDependencies()
        {
            if (psychologySystem == null)
                psychologySystem = FindFirstObjectByType<PsychologySystem>();
            if (behaviorAnalyzer == null)
                behaviorAnalyzer = FindFirstObjectByType<PlayerBehaviorAnalyzer>();
            if (floatingTextSystem == null)
                floatingTextSystem = FindFirstObjectByType<FloatingTextSystem>();
            if (llmManager == null)
                llmManager = LLMManager.Instance;

            if (psychologySystem == null)
                Debug.LogWarning("[BluffSystem] PsychologySystem not found");
            if (behaviorAnalyzer == null)
                Debug.LogWarning("[BluffSystem] PlayerBehaviorAnalyzer not found");
            if (floatingTextSystem == null)
                Debug.LogWarning("[BluffSystem] FloatingTextSystem not found");
            if (llmManager == null)
                Debug.Log("[BluffSystem] LLMManager not found (Layer B/C disabled, using Layer A only)");
        }

        // ========================================
        // PUBLIC API (GameManagerから呼ばれる)
        // ========================================

        /// <summary>
        /// COMMIT状態で呼ばれる: AIの期待を決定する
        /// ドロー前にAIが「期待」を持つ（Stop/Bait/Neutral）
        /// </summary>
        public AIExpectation DetermineExpectation()
        {
            turnCount++;

            // ドロー前は必ずAnticipatingに遷移
            currentEmotion = AIEmotion.Anticipating;

            // 序盤: 期待なし（行動データ収集期間）
            if (turnCount <= turnsBeforeExpectation)
            {
                currentExpectation = AIExpectation.Neutral;
                Debug.Log($"[BluffSystem] Turn {turnCount}: Too early, expectation=Neutral (Anticipating)");
                return currentExpectation;
            }

            BehaviorPattern behavior = GetCurrentBehavior();
            float pressureLevel = GetCurrentPressure();

            float expectationChance = CalculateExpectationChance(behavior, pressureLevel);

            if (Random.value < expectationChance)
            {
                currentExpectation = SelectExpectation(behavior, pressureLevel);
                Debug.Log($"[BluffSystem] Turn {turnCount}: expectation={currentExpectation} (chance={expectationChance:F2}, Anticipating)");
            }
            else
            {
                currentExpectation = AIExpectation.Neutral;
                Debug.Log($"[BluffSystem] Turn {turnCount}: expectation=Neutral (chance={expectationChance:F2}, Anticipating)");
            }

            return currentExpectation;
        }

        /// <summary>
        /// REACT/POST_REACT状態で呼ばれる: 感情リアクションを評価する
        /// 「予測 vs 現実のギャップ」から感情が生まれる
        /// </summary>
        public async Task<EmotionalResult> EvaluateReaction(DrawContext context)
        {
            float pressureLevel = GetCurrentPressure();

            // 感情決定: 予測 × 現実のマトリクス
            AIEmotion emotion = ResolveEmotion(context);
            currentEmotion = emotion;

            EmotionalResult result = new EmotionalResult
            {
                expectation = currentExpectation,
                emotion = emotion,
                reactionIntensity = CalculateReactionIntensity(context, pressureLevel),
                immediateDialogue = await GenerateImmediateDialogue(emotion, context),
                pressureDelta = CalculatePressureDelta(context, emotion),
                isTurningPoint = IsTurningPoint(context)
            };

            reactionHistory.Add(result);
            if (reactionHistory.Count > 20) reactionHistory.RemoveAt(0);

            // EmotionalStateManagerへ同期（BluffSystemが感情の正）
            SyncEmotionToStateManager(emotion, context);

            Debug.Log($"[BluffSystem] Reaction: expectation={result.expectation}, emotion={result.emotion}, " +
                      $"intensity={result.reactionIntensity:F2}, dialogue=\"{result.immediateDialogue}\", " +
                      $"turningPoint={result.isTurningPoint}");

            return result;
        }

        /// <summary>
        /// Layer B/C: LLM台詞の非同期生成
        /// カード情報はLLMに渡さない。感情とプレイヤー行動パターンのみ。
        /// </summary>
        public async Task<string> GenerateResponseAsync(ResponseRequest request)
        {
            if (llmManager == null)
            {
                Debug.Log("[BluffSystem] LLMManager not available, skipping Layer B/C");
                return null;
            }

            try
            {
                // 感情別プロンプトを使用（BuildEmotionalResponsePrompt経由）
                string dialogue = await llmManager.GenerateEmotionalDialogueAsync(request);

                if (!string.IsNullOrEmpty(dialogue))
                {
                    Debug.Log($"[BluffSystem] Layer {request.layer} LLM response: \"{dialogue}\"");

                    // Stage 15: 会話履歴に記録
                    var gameManager = GameManager.Instance;
                    int turn = gameManager != null ? gameManager.GetTurnCounter() : 0;
                    string context = $"Layer {request.layer} ({request.emotion})";
                    LLMContextWindow.AddDialogueToHistory(turn, dialogue, context);
                }

                return dialogue;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BluffSystem] Layer {request.layer} LLM failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ゲームリセット
        /// </summary>
        public void ResetSystem()
        {
            currentEmotion = AIEmotion.Calm;
            currentExpectation = AIExpectation.Neutral;
            turnCount = 0;
            reactionHistory.Clear();
            Debug.Log("[BluffSystem] System reset");
        }

        public AIEmotion GetCurrentEmotion() => currentEmotion;
        public AIExpectation GetCurrentExpectation() => currentExpectation;
        public int GetTurnCount() => turnCount;

        /// <summary>
        /// EmotionalStateManagerに感情を同期（BluffSystemが感情の正）
        /// </summary>
        private void SyncEmotionToStateManager(AIEmotion emotion, DrawContext context)
        {
            if (psychologySystem == null || psychologySystem.EmotionalState == null) return;

            FPSTrump.AI.LLM.GameEvent trigger;
            if (context.isPlayerTurn)
                trigger = context.drawnCardIsJoker
                    ? FPSTrump.AI.LLM.GameEvent.PlayerDrawJoker
                    : FPSTrump.AI.LLM.GameEvent.PlayerDrawSuccessful;
            else
                trigger = context.drawnCardIsJoker
                    ? FPSTrump.AI.LLM.GameEvent.AIDrawJoker
                    : FPSTrump.AI.LLM.GameEvent.AIDrawSuccessful;

            if (context.formedPair)
                trigger = FPSTrump.AI.LLM.GameEvent.PairMatched;

            psychologySystem.EmotionalState.ForceEmotionalState(emotion, trigger);
        }

        // ========================================
        // 感情決定: 予測 × 現実のマトリクス
        // ========================================

        /// <summary>
        /// 予測と現実のギャップから感情を決定
        /// AIはカードの内容を知らない。期待が裏切られたかどうかだけを感じる。
        /// </summary>
        private AIEmotion ResolveEmotion(DrawContext context)
        {
            if (context.isPlayerTurn)
            {
                return ResolvePlayerTurnEmotion(context);
            }
            else
            {
                return ResolveAITurnEmotion(context);
            }
        }

        /// <summary>
        /// プレイヤーターン: プレイヤーがAIの手札から引いた後
        /// </summary>
        private AIEmotion ResolvePlayerTurnEmotion(DrawContext context)
        {
            switch (currentExpectation)
            {
                case AIExpectation.Stop:
                    if (context.drawnCardIsJoker)
                        return AIEmotion.Pleased;       // 忠告が正しかった
                    else if (context.formedPair)
                        return AIEmotion.Frustrated;    // 無視された上に相手が得した
                    else
                        return AIEmotion.Frustrated;    // 無視された

                case AIExpectation.Bait:
                    if (context.drawnCardIsJoker)
                        return AIEmotion.Hurt;          // 罠にかけたつもりが失敗
                    else if (context.formedPair)
                        return AIEmotion.Frustrated;    // 予想外に相手が得した
                    else
                        return AIEmotion.Pleased;       // 誘導成功

                case AIExpectation.Neutral:
                default:
                    if (context.drawnCardIsJoker)
                        return AIEmotion.Relieved;      // 自分に来なくて良かった
                    else
                        return AIEmotion.Calm;          // 平静維持
            }
        }

        /// <summary>
        /// AIターン: AIがプレイヤーの手札から引いた後
        /// </summary>
        private AIEmotion ResolveAITurnEmotion(DrawContext context)
        {
            if (context.drawnCardIsJoker)
                return AIEmotion.Frustrated;    // ジョーカーを引いてしまった
            else if (context.formedPair)
                return AIEmotion.Pleased;       // ペア成立、完璧
            else
                return AIEmotion.Calm;          // 問題なし
        }

        // ========================================
        // 期待確率・選択
        // ========================================

        private float CalculateExpectationChance(BehaviorPattern behavior, float pressureLevel)
        {
            float chance = baseExpectationChance;

            // ゲーム進行: 中盤以降は期待を持ちやすくなる
            float progressionBonus = Mathf.Clamp01((turnCount - turnsBeforeExpectation) / 10f) * 0.2f;
            chance += progressionBonus;

            // プレイヤーがパターンを見せている → 期待しやすい
            if (behavior != null && behavior.hasPositionPreference)
                chance += 0.1f;

            // プレイヤーが迷っている → 期待しやすい
            if (behavior != null && behavior.doubtLevel > 0.5f)
                chance += 0.1f;

            // 圧力が高い → 期待控えめ（逆効果防止）
            if (pressureLevel > 2.0f)
                chance -= 0.1f;

            return Mathf.Clamp(chance, 0.05f, maxExpectationChance);
        }

        /// <summary>
        /// Stop/Bait の2択で期待を選択（重み付き）
        /// </summary>
        private AIExpectation SelectExpectation(BehaviorPattern behavior, float pressureLevel)
        {
            // Stop: プレイヤーが迷っている時に「引くな」
            float stopWeight = 1f;
            if (behavior != null && behavior.doubtLevel > 0.5f) stopWeight += 2f;
            if (pressureLevel > 1.5f) stopWeight += 1f;

            // Bait: プレイヤーが即断する時に「引け」
            float baitWeight = 1f;
            if (behavior != null && behavior.tempo == TempoType.Fast) baitWeight += 2f;
            if (behavior != null && behavior.hasPositionPreference) baitWeight += 1f;

            float total = stopWeight + baitWeight;
            float roll = Random.value * total;

            return roll < stopWeight ? AIExpectation.Stop : AIExpectation.Bait;
        }

        // ========================================
        // 反応強度・圧力計算
        // ========================================

        private float CalculateReactionIntensity(DrawContext context, float pressureLevel)
        {
            float intensity = 0.3f;

            if (context.drawnCardIsJoker) intensity += 0.4f;
            if (context.formedPair) intensity += 0.2f;
            intensity += pressureLevel * 0.1f;
            if (currentExpectation != AIExpectation.Neutral) intensity += 0.15f;

            // 終盤は反応が強くなる
            if (context.remainingCards <= 3 || context.opponentRemainingCards <= 3)
                intensity += 0.2f;

            return Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 感情に基づく圧力変化
        /// </summary>
        private float CalculatePressureDelta(DrawContext context, AIEmotion emotion)
        {
            float delta = emotion switch
            {
                AIEmotion.Pleased => context.isPlayerTurn ? 0.3f : 0.3f,
                AIEmotion.Frustrated => context.isPlayerTurn ? -0.1f : -0.4f,
                AIEmotion.Hurt => 0.3f,
                AIEmotion.Relieved => -0.3f,   // 安堵 = 圧力解放
                AIEmotion.Calm => -0.1f,        // 自然減衰
                _ => 0f
            };

            // ジョーカー時は圧力変化が大きい
            if (context.drawnCardIsJoker)
            {
                delta += context.isPlayerTurn ? 0.2f : -0.2f;
            }

            // 毎ターン自然減衰（緊張→弛緩のリズム）
            delta -= 0.05f;

            return delta;
        }

        /// <summary>
        /// ターニングポイント判定（Layer C発動条件）
        /// </summary>
        public bool IsTurningPoint(DrawContext context)
        {
            // ジョーカーを引いた
            if (context.drawnCardIsJoker) return true;

            // 終盤（残り3枚以下）
            if (context.remainingCards <= 3 || context.opponentRemainingCards <= 3) return true;

            return false;
        }

        // ========================================
        // Layer A: 即座台詞テーブル（感情ベース）
        // ========================================

        private async Task<string> GenerateImmediateDialogue(AIEmotion emotion, DrawContext context)
        {
            // 0. LLM先行試行（最優先、ただしJokerオーバーリアクションを除く）
            if (llmManager != null && !context.drawnCardIsJoker)
            {
                string llmReaction = await llmManager.GenerateImmediateReactionAsync(emotion, context);
                if (!string.IsNullOrEmpty(llmReaction))
                {
                    return llmReaction;
                }
            }

            // 1. Jokerオーバーリアクション（LLM失敗時も最優先維持）
            if (context.aiHoldsJoker && !context.isPlayerTurn && context.drawnCardIsJoker)
            {
                return GetJokerOverReaction();
            }

            // 2. 決着リアクション（Jokerの次に最優先、100%発火）
            string decisive = GetGameDecisiveDialogue(context);
            if (decisive != null) return decisive;

            // 3. ゲーム状況リアクション（30%）
            if (Random.value < 0.3f)
            {
                string situational = GetSituationalDialogue(emotion, context);
                if (situational != null) return situational;
            }

            // 4. 行動パターンベース（30%）
            BehaviorPattern behavior = GetCurrentBehavior();
            if (Random.value < 0.3f)
            {
                string behaviorDialogue = GetBehaviorAwareDialogue(emotion, behavior);
                if (behaviorDialogue != null) return behaviorDialogue;
            }

            // 5. 感情テーブル
            if (emotionDialogues.TryGetValue(emotion, out string[] options))
            {
                return options[Random.Range(0, options.Length)];
            }
            return "...";
        }

        /// <summary>
        /// AIがJokerを引いた時のオーバーリアクション
        /// </summary>
        private string GetJokerOverReaction()
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string[] options = loc.GetArray("bluff.joker_overreaction");
                if (options.Length > 0) return options[Random.Range(0, options.Length)];
            }
            return "JOKER...!";
        }

        /// <summary>
        /// ゲーム決着時の台詞（勝利/敗北確定時に100%発火）
        /// ペア除去前の時点で判定:
        /// - formedPair && remainingCards == 2 → 除去後0枚 = 勝者確定
        /// - formedPair && remainingCards == 3 && ドロー側がJoker保持 → 除去後Jokerのみ = 敗者確定
        /// - opponentRemainingCards == 0 → 相手が勝利確定
        /// </summary>
        private string GetGameDecisiveDialogue(DrawContext ctx)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return null;

            bool drawerWillWin = ctx.formedPair && ctx.remainingCards == 2;

            // ドロー側がペア形成後にJokerのみ残る → ドロー側敗北
            bool drawerOnlyJokerLeft = ctx.formedPair && ctx.remainingCards == 3 &&
                ((ctx.isPlayerTurn && !ctx.aiHoldsJoker) || (!ctx.isPlayerTurn && ctx.aiHoldsJoker));

            bool opponentEmptied = ctx.opponentRemainingCards == 0 && !drawerWillWin && !drawerOnlyJokerLeft;

            if (!drawerWillWin && !drawerOnlyJokerLeft && !opponentEmptied) return null;

            // AI視点で勝敗を判定
            bool aiWins = (ctx.isPlayerTurn && opponentEmptied) ||           // AI手札0 → AI勝利
                          (!ctx.isPlayerTurn && drawerWillWin) ||            // AIドローで0枚 → AI勝利
                          (ctx.isPlayerTurn && drawerOnlyJokerLeft);         // プレイヤーJokerのみ → AI勝利

            string key = aiWins ? "bluff.decisive_ai_wins" : "bluff.decisive_player_wins";
            string[] options = loc.GetArray(key);
            if (options.Length > 0) return options[Random.Range(0, options.Length)];

            return null;
        }

        /// <summary>
        /// ゲーム進行状況に応じたピエロ的リアクション
        /// </summary>
        private string GetSituationalDialogue(AIEmotion emotion, DrawContext ctx)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return null;

            int totalCards = ctx.remainingCards + ctx.opponentRemainingCards;

            // エンドゲーム（残り5枚以下）
            if (totalCards <= 5)
            {
                string key = emotion switch
                {
                    AIEmotion.Pleased => "bluff.situation_endgame_pleased",
                    AIEmotion.Frustrated => "bluff.situation_endgame_frustrated",
                    _ => "bluff.situation_endgame_default"
                };
                return loc.Get(key);
            }

            // AI手札が少ない（優勢）— AIターンのみ
            if (!ctx.isPlayerTurn && ctx.remainingCards < ctx.opponentRemainingCards)
            {
                string key = emotion switch
                {
                    AIEmotion.Pleased => "bluff.situation_winning_pleased",
                    AIEmotion.Calm => "bluff.situation_winning_calm",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            // AI手札が多い（劣勢）— AIターンのみ
            if (!ctx.isPlayerTurn && ctx.remainingCards > ctx.opponentRemainingCards)
            {
                string key = emotion switch
                {
                    AIEmotion.Frustrated => "bluff.situation_losing_frustrated",
                    AIEmotion.Calm => "bluff.situation_losing_calm",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            return null;
        }

        /// <summary>
        /// プレイヤーの行動パターンに応じた台詞を返す（nullなら汎用テーブルにフォールバック）
        /// </summary>
        private string GetBehaviorAwareDialogue(AIEmotion emotion, BehaviorPattern behavior)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return null;

            // 速いテンポのプレイヤー
            if (behavior.tempo == TempoType.Fast)
            {
                string key = emotion switch
                {
                    AIEmotion.Calm => "bluff.behavior_fast_calm",
                    AIEmotion.Pleased => "bluff.behavior_fast_pleased",
                    AIEmotion.Anticipating => "bluff.behavior_fast_anticipating",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            // 遅いテンポ（慎重型）
            if (behavior.tempo == TempoType.Slow)
            {
                string key = emotion switch
                {
                    AIEmotion.Calm => "bluff.behavior_slow_calm",
                    AIEmotion.Frustrated => "bluff.behavior_slow_frustrated",
                    AIEmotion.Anticipating => "bluff.behavior_slow_anticipating",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            // 高い迷い度
            if (behavior.doubtLevel > 0.6f)
            {
                string key = emotion switch
                {
                    AIEmotion.Pleased => "bluff.behavior_doubt_pleased",
                    AIEmotion.Anticipating => "bluff.behavior_doubt_anticipating",
                    AIEmotion.Calm => "bluff.behavior_doubt_calm",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            // 位置偏好あり
            if (behavior.hasPositionPreference)
            {
                string key = emotion switch
                {
                    AIEmotion.Pleased => "bluff.behavior_position_pleased",
                    AIEmotion.Calm => "bluff.behavior_position_calm",
                    _ => null
                };
                return key != null ? loc.Get(key) : null;
            }

            return null;
        }

        /// <summary>
        /// AIEmotion → DialogueCategoryType マッピング（LLMManager用）
        /// </summary>
        public DialogueCategoryType MapEmotionToCategory(AIEmotion emotion)
        {
            return emotion switch
            {
                AIEmotion.Pleased => DialogueCategoryType.Bait,      // 誘導的・余裕
                AIEmotion.Frustrated => DialogueCategoryType.Stop,   // 抑制的・苛立ち
                AIEmotion.Hurt => DialogueCategoryType.Mirror,       // 裏切りの指摘
                AIEmotion.Relieved => DialogueCategoryType.General,  // 安堵の一般表現
                AIEmotion.Anticipating => DialogueCategoryType.General, // 期待・警戒
                AIEmotion.Calm => DialogueCategoryType.General,      // 平静
                _ => DialogueCategoryType.General
            };
        }

        /// <summary>
        /// 感情トリガーの抽象説明を取得（LLMプロンプト用）
        /// カード情報は含まない
        /// </summary>
        public string GetEmotionTriggerDescription(AIEmotion emotion, AIExpectation expectation)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return "...";

            string locKey = (emotion, expectation) switch
            {
                (AIEmotion.Pleased, AIExpectation.Stop) => "bluff.trigger_pleased_stop",
                (AIEmotion.Pleased, AIExpectation.Bait) => "bluff.trigger_pleased_bait",
                (AIEmotion.Pleased, _) => "bluff.trigger_pleased_default",
                (AIEmotion.Frustrated, AIExpectation.Stop) => "bluff.trigger_frustrated_stop",
                (AIEmotion.Frustrated, AIExpectation.Bait) => "bluff.trigger_frustrated_bait",
                (AIEmotion.Frustrated, _) => "bluff.trigger_frustrated_default",
                (AIEmotion.Hurt, _) => "bluff.trigger_hurt_default",
                (AIEmotion.Relieved, _) => "bluff.trigger_relieved_default",
                (AIEmotion.Anticipating, _) => "bluff.trigger_anticipating_default",
                (AIEmotion.Calm, _) => "bluff.trigger_calm_default",
                _ => "bluff.trigger_default"
            };
            return loc.Get(locKey);
        }

        // ========================================
        // ヘルパー
        // ========================================

        private BehaviorPattern GetCurrentBehavior()
        {
            if (behaviorAnalyzer != null)
                return behaviorAnalyzer.CurrentBehavior ?? new BehaviorPattern();
            return new BehaviorPattern();
        }

        private float GetCurrentPressure()
        {
            if (psychologySystem != null)
                return psychologySystem.GetPressureLevel();
            return 0f;
        }

        // ========================================
        // セリフテーブル初期化
        // ========================================

        private void InitializeDialogues()
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                emotionDialogues = new Dictionary<AIEmotion, string[]>
                {
                    [AIEmotion.Calm] = loc.GetArray("bluff.emotion_calm"),
                    [AIEmotion.Anticipating] = loc.GetArray("bluff.emotion_anticipating"),
                    [AIEmotion.Pleased] = loc.GetArray("bluff.emotion_pleased"),
                    [AIEmotion.Frustrated] = loc.GetArray("bluff.emotion_frustrated"),
                    [AIEmotion.Hurt] = loc.GetArray("bluff.emotion_hurt"),
                    [AIEmotion.Relieved] = loc.GetArray("bluff.emotion_relieved")
                };
            }
            else
            {
                emotionDialogues = new Dictionary<AIEmotion, string[]>();
            }
        }

        /// <summary>
        /// 感情トリガー説明の初期化（LLMプロンプト用）
        /// </summary>
        private void InitializeEmotionTriggers()
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                emotionTriggerDescriptions = new Dictionary<AIEmotion, string[]>
                {
                    [AIEmotion.Calm] = loc.GetArray("bluff.trigger_calm"),
                    [AIEmotion.Anticipating] = loc.GetArray("bluff.trigger_anticipating"),
                    [AIEmotion.Pleased] = loc.GetArray("bluff.trigger_pleased"),
                    [AIEmotion.Frustrated] = loc.GetArray("bluff.trigger_frustrated"),
                    [AIEmotion.Hurt] = loc.GetArray("bluff.trigger_hurt"),
                    [AIEmotion.Relieved] = loc.GetArray("bluff.trigger_relieved")
                };
            }
            else
            {
                emotionTriggerDescriptions = new Dictionary<AIEmotion, string[]>();
            }
        }

        // ========================================
        // Bluff Action Evaluation
        // ========================================

        /// <summary>
        /// プレイヤーのブラフアクションに対するAI台詞を返す (null = 反応なし)
        /// </summary>
        public string EvaluateBluffAction(BluffActionRecord record)
        {
            if (record.source != BluffActionSource.Player) return null;

            // 反応確率: 40% base + emotion補正
            float reactionChance = 0.4f;
            if (currentEmotion == AIEmotion.Frustrated) reactionChance += 0.2f;
            else if (currentEmotion == AIEmotion.Pleased) reactionChance += 0.1f;

            if (Random.value > reactionChance) return null;

            // ローカライズ対応: キーを生成してLocalizationManagerから取得
            string locKey = record.actionType switch
            {
                BluffActionType.Shuffle => "bluff_action.reaction_shuffle",
                BluffActionType.Push => "bluff_action.reaction_push",
                BluffActionType.Pull => "bluff_action.reaction_pull",
                BluffActionType.Wiggle => "bluff_action.reaction_wiggle",
                BluffActionType.Spread => "bluff_action.reaction_spread",
                BluffActionType.Close => "bluff_action.reaction_close",
                _ => null
            };

            if (locKey == null) return null;

            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string[] dialogues = loc.GetArray(locKey);
                if (dialogues != null && dialogues.Length > 0)
                    return dialogues[Random.Range(0, dialogues.Length)];
            }

            // フォールバック: ハードコード台詞
            return GetFallbackBluffReaction(record.actionType);
        }

        private string GetFallbackBluffReaction(BluffActionType actionType)
        {
            var loc = LocalizationManager.Instance;
            if (loc == null) return null;

            string key = actionType switch
            {
                BluffActionType.Shuffle => "bluff_action.reaction_shuffle",
                BluffActionType.Push => "bluff_action.reaction_push",
                BluffActionType.Pull => "bluff_action.reaction_pull",
                BluffActionType.Wiggle => "bluff_action.reaction_wiggle",
                BluffActionType.Spread => "bluff_action.reaction_spread",
                BluffActionType.Close => "bluff_action.reaction_close",
                _ => null
            };

            if (key == null) return null;

            string[] options = loc.GetArray(key);
            if (options.Length == 0) return null;
            return options[Random.Range(0, options.Length)];
        }

        // ========================================
        // Debug GUI
        // ========================================

#if UNITY_EDITOR
        // Debug表示を非表示化
        //private void OnGUI()
        //{
        //    GUILayout.BeginArea(new Rect(10, 430, 300, 100));
        //    GUILayout.Label("=== Emotional AI ===");
        //    GUILayout.Label($"Emotion: {currentEmotion}");
        //    GUILayout.Label($"Expectation: {currentExpectation}");
        //    GUILayout.Label($"Turn: {turnCount}");
        //    GUILayout.EndArea();
        //}
#endif
    }
}
