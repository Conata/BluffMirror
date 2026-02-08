using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using FPSTrump.AI.LLM;

namespace FPSTrump.Psychology
{
    /// <summary>
    /// 心理圧・セリフシステム (LLM統合版)
    /// プレイヤーの行動パターンに応じて動的にダイアログを生成し、心理的圧力をかける
    /// </summary>
    public class PsychologySystem : MonoBehaviour
    {
        public static PsychologySystem Instance { get; private set; }

        /// <summary>
        /// EmotionalStateManagerへのアクセス（BluffSystem等から使用）
        /// </summary>
        public EmotionalStateManager EmotionalState => emotionalStateManager;

        [Header("LLM Integration")]
        [SerializeField] private LLMManager llmManager;
        private EmotionalStateManager emotionalStateManager; // 非MonoBehaviour、Awake時にnew

        [Header("Behavior Analysis")]
        [SerializeField] private PlayerBehaviorAnalyzer behaviorAnalyzer;

        [Header("Floating Text (Phase 4)")]
        [SerializeField] private FloatingTextSystem floatingTextSystem;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool enableTTS = true;
        [SerializeField] private float ttsVolume = 0.8f;

        [Header("Pressure Settings")]
        [SerializeField] private float basePressureLevel = 0.0f;
        [SerializeField] private float maxPressureLevel = 3.0f;
        [SerializeField] private float pressureDecayRate = 0.1f;

        [Header("Timing Settings")]
        [SerializeField] private float hoverDialogueDelay = 0.5f;  // ホバー後、ダイアログまでの遅延
        [SerializeField] private float hoverDialogueCooldown = 4.0f;  // ダイアログ表示後のクールダウン時間
        [SerializeField] private bool enableHoverDialogue = true;  // ホバーダイアログの有効化

        private float currentPressureLevel;
        private bool isProcessingDialogue = false;
        private float lastHoverDialogueTime = -999f;  // 最後にホバーダイアログを表示した時刻

        [Header("Events")]
        public UnityEvent<float> OnPressureLevelChanged;
        public UnityEvent<string> OnDialogueGenerated;  // ダイアログ生成完了
        public UnityEvent<string> OnDialogueDisplayed;  // ダイアログ表示完了

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
        }

        private void Start()
        {
            ValidateComponents();

            currentPressureLevel = basePressureLevel;

            // 行動分析イベントのサブスクライブ
            if (behaviorAnalyzer != null)
            {
                behaviorAnalyzer.OnBehaviorAnalyzed.AddListener(HandleBehaviorAnalysis);
            }

            Debug.Log("[PsychologySystem] Initialized with LLM integration");
        }

        private void Update()
        {
            // 圧力レベルの自然減衰
            if (currentPressureLevel > basePressureLevel)
            {
                float previousLevel = currentPressureLevel;
                currentPressureLevel = Mathf.Max(
                    basePressureLevel,
                    currentPressureLevel - pressureDecayRate * Time.deltaTime
                );

                if (Mathf.Abs(previousLevel - currentPressureLevel) > 0.01f)
                {
                    OnPressureLevelChanged?.Invoke(currentPressureLevel);
                }
            }
        }

        /// <summary>
        /// コンポーネント検証
        /// </summary>
        private void ValidateComponents()
        {
            if (llmManager == null)
            {
                llmManager = LLMManager.Instance;
                if (llmManager == null)
                {
                    Debug.LogError("[PsychologySystem] LLMManager not found! Psychology system will not work.");
                }
            }

            if (emotionalStateManager == null)
            {
                emotionalStateManager = new EmotionalStateManager();
                Debug.Log("[PsychologySystem] EmotionalStateManager created");

                // LLMManagerからPersonalityProfileを取得して設定
                if (llmManager != null && llmManager.CurrentPlayerProfile != null)
                {
                    emotionalStateManager.SetPlayerProfile(llmManager.CurrentPlayerProfile);
                }
            }

            if (behaviorAnalyzer == null)
            {
                behaviorAnalyzer = GetComponent<PlayerBehaviorAnalyzer>();
                if (behaviorAnalyzer == null)
                {
                    Debug.LogWarning("[PsychologySystem] PlayerBehaviorAnalyzer not found. Behavior-based dialogue will not work.");
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // AudioSourceを自動追加
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.volume = ttsVolume;
                    Debug.Log("[PsychologySystem] AudioSource auto-created");
                }
            }

            if (floatingTextSystem == null)
            {
                floatingTextSystem = FindFirstObjectByType<FloatingTextSystem>();
                if (floatingTextSystem == null)
                {
                    Debug.LogWarning("[PsychologySystem] FloatingTextSystem not found. Floating text will not be displayed.");
                }
            }
        }

        /// <summary>
        /// 行動分析への応答（非同期）
        /// </summary>
        private void HandleBehaviorAnalysis(BehaviorPattern behavior)
        {
            // 圧力レベル調整（維持 — BluffSystemの圧力計算に必要）
            AdjustPressureLevel(behavior);

            // 感情状態更新（維持 — 後続のBluffSystem判定に必要）
            UpdateEmotionalState(behavior);

            // ダイアログ生成は抑制 — BluffSystem Layer A/Bが処理するため衝突回避
            // GameManagerのメンタリストシステムがターン開始・長考時の発話を担当
            Debug.Log($"[PsychologySystem] Behavior analyzed: pressure={currentPressureLevel:F2}, dialogue suppressed (mentalist system)");
        }

        /// <summary>
        /// 圧力レベルを行動パターンに基づいて調整
        /// </summary>
        private void AdjustPressureLevel(BehaviorPattern behavior)
        {
            float pressureIncrease = 0f;

            // 同じ位置選択の連続
            if (behavior.streakSamePosition >= 2)
            {
                pressureIncrease += 0.5f * behavior.streakSamePosition;
                Debug.Log($"[PsychologySystem] Pressure +{0.5f * behavior.streakSamePosition:F2} (streak={behavior.streakSamePosition})");
            }

            // 優柔不断（ホバー時間が長い）
            if (behavior.avgHoverTime > 2.0f)
            {
                pressureIncrease += 0.3f;
                Debug.Log($"[PsychologySystem] Pressure +0.3 (hover={behavior.avgHoverTime:F2}s)");
            }

            // 疑念レベルが高い
            if (behavior.doubtLevel > 0.7f)
            {
                pressureIncrease += 0.4f;
                Debug.Log($"[PsychologySystem] Pressure +0.4 (doubt={behavior.doubtLevel:F2})");
            }

            // 不規則なテンポ
            if (behavior.tempo == TempoType.Erratic)
            {
                pressureIncrease += 0.2f;
                Debug.Log($"[PsychologySystem] Pressure +0.2 (erratic tempo)");
            }

            // Stage 10: 表情データによるブラフ検出（表情と行動の不一致）
            var facialAnalyzer = FacialExpressionAnalyzer.Instance;
            if (facialAnalyzer != null && facialAnalyzer.IsActive)
            {
                var facial = facialAnalyzer.CurrentState;
                if (facial.confidence > 0.5f) // 十分な信頼度がある場合のみ
                {
                    // 笑顔なのに迷いが大きい → ポーカーフェイス（余裕を装っている）
                    if (facial.currentExpression == FacialExpression.Happy && behavior.doubtLevel > 0.7f)
                    {
                        pressureIncrease += 0.3f;
                        Debug.Log("[PsychologySystem] Pressure +0.3 (facial bluff: smiling but hesitant)");
                    }
                    // 恐怖・不安なのに素早い決断 → パニック的な衝動
                    else if (facial.currentExpression == FacialExpression.Fearful && behavior.tempo == TempoType.Fast)
                    {
                        pressureIncrease += 0.2f;
                        Debug.Log("[PsychologySystem] Pressure +0.2 (facial stress: fearful + fast)");
                    }
                    // 表情変化が激しい → 動揺している
                    if (facial.expressionChangeRate > 0.6f)
                    {
                        pressureIncrease += 0.15f;
                        Debug.Log($"[PsychologySystem] Pressure +0.15 (facial instability: changeRate={facial.expressionChangeRate:F2})");
                    }
                }
            }

            // 圧力レベル更新
            if (pressureIncrease > 0)
            {
                currentPressureLevel = Mathf.Min(maxPressureLevel, currentPressureLevel + pressureIncrease);
                OnPressureLevelChanged?.Invoke(currentPressureLevel);

                Debug.Log($"[PsychologySystem] Pressure level: {currentPressureLevel:F2}/{maxPressureLevel:F2}");
            }
        }

        /// <summary>
        /// 行動パターンに基づいてダイアログカテゴリを選択
        /// </summary>
        private DialogueCategoryType SelectDialogueCategory(BehaviorPattern behavior)
        {
            // 連続同位置選択 → Mirror（癖を指摘）
            if (behavior.streakSamePosition >= 3 || behavior.hasPositionPreference)
            {
                Debug.Log("[PsychologySystem] Selected category: Mirror");
                return DialogueCategoryType.Mirror;
            }

            // 高疑念レベル → Stop（止める）
            if (behavior.doubtLevel > 0.6f)
            {
                Debug.Log("[PsychologySystem] Selected category: Stop");
                return DialogueCategoryType.Stop;
            }

            // 焦りテンポ → Bait（釣る）
            if (behavior.tempo == TempoType.Fast)
            {
                Debug.Log("[PsychologySystem] Selected category: Bait");
                return DialogueCategoryType.Bait;
            }

            // デフォルト → General
            Debug.Log("[PsychologySystem] Selected category: General");
            return DialogueCategoryType.General;
        }

        /// <summary>
        /// ダイアログを生成して表示（非同期）
        /// </summary>
        private IEnumerator GenerateAndDisplayDialogue(DialogueCategoryType category, BehaviorPattern behavior)
        {
            if (isProcessingDialogue)
            {
                Debug.Log("[PsychologySystem] Already processing dialogue, skipping...");
                yield break;
            }

            isProcessingDialogue = true;

            // 感情状態を更新（行動パターンに基づく）
            UpdateEmotionalState(behavior);

            Debug.Log($"[PsychologySystem] Generating dialogue: category={category}, pressure={currentPressureLevel:F2}, emotion={emotionalStateManager.CurrentState}");

            // 非同期ダイアログ生成タスクを開始
            Task<string> dialogueTask = llmManager.GenerateDialogueAsync(
                category,
                behavior,
                currentPressureLevel
            );

            // タスク完了を待機（コルーチンで）
            while (!dialogueTask.IsCompleted)
            {
                yield return null;
            }

            // エラーチェック
            if (dialogueTask.IsFaulted)
            {
                Debug.LogError($"[PsychologySystem] Dialogue generation failed: {dialogueTask.Exception?.Message}");
                isProcessingDialogue = false;
                yield break;
            }

            string dialogue = dialogueTask.Result;

            if (!string.IsNullOrEmpty(dialogue))
            {
                Debug.Log($"[PsychologySystem] Dialogue generated: \"{dialogue}\"");

                // イベント発火
                OnDialogueGenerated?.Invoke(dialogue);

                // ダイアログ表示（TTS音声統合）
                yield return StartCoroutine(DisplayDialogue(dialogue, category));
            }
            else
            {
                Debug.LogWarning("[PsychologySystem] Generated dialogue is empty");
            }

            isProcessingDialogue = false;
        }

        /// <summary>
        /// 感情状態を更新
        /// </summary>
        private void UpdateEmotionalState(BehaviorPattern behavior)
        {
            if (emotionalStateManager == null) return;

            // 行動パターンに基づいてゲームイベントを推測
            GameEvent gameEvent = InferGameEvent(behavior);

            // ダミーAI決定結果（実際のAI行動がある場合は置き換える）
            AIDecisionResult dummyDecision = new AIDecisionResult
            {
                confidence = currentPressureLevel / maxPressureLevel,
                strategy = "Adaptive"
            };

            // 感情状態を更新
            AIEmotion newState = emotionalStateManager.UpdateEmotionalState(
                gameEvent,
                behavior,
                dummyDecision
            );

            Debug.Log($"[PsychologySystem] Emotional state: {newState}");
        }

        /// <summary>
        /// 行動パターンからゲームイベントを推測
        /// </summary>
        private GameEvent InferGameEvent(BehaviorPattern behavior)
        {
            // プレイヤーが迷っている
            if (behavior.doubtLevel > 0.7f || behavior.avgHoverTime > 3.0f)
            {
                return GameEvent.PlayerHesitating;
            }

            // プレイヤーのパターンが見えた
            if (behavior.streakSamePosition >= 3 || behavior.hasPositionPreference)
            {
                return GameEvent.PlayerShowingPattern;
            }

            // デフォルト
            return GameEvent.TurnStart;
        }

        /// <summary>
        /// ダイアログを表示（TTS音声統合版）
        /// </summary>
        private IEnumerator DisplayDialogue(string dialogue, DialogueCategoryType category)
        {
            Debug.Log($"[PsychologySystem] Displaying: \"{dialogue}\" (category: {category})");

            // TTS音声生成と再生
            if (enableTTS && audioSource != null && emotionalStateManager != null)
            {
                yield return StartCoroutine(PlayTTSDialogue(dialogue));
            }

            // Whisper/Projection/Distortion tier visual effects
            if (PostProcessingController.Instance != null)
            {
                PostProcessingController.Instance.ApplyDialogueVisualEffect(currentPressureLevel);
            }

            // Projection/Distortion tier: subtitle wobble
            if (currentPressureLevel >= 1.0f && SubtitleUI.Instance != null)
            {
                float wobbleStrength = Mathf.Clamp01((currentPressureLevel - 1.0f) / 2.0f);
                SubtitleUI.Instance.StartWobble(wobbleStrength);
            }

            OnDialogueDisplayed?.Invoke(dialogue);

            // 表示時間（音声再生時間に基づく）
            if (!enableTTS)
            {
                yield return new WaitForSeconds(2.0f);
            }
        }

        /// <summary>
        /// TTS音声を生成・再生
        /// </summary>
        private IEnumerator PlayTTSDialogue(string text)
        {
            // 現在の感情状態を取得
            AIEmotion emotionalState = emotionalStateManager.CurrentState;

            Debug.Log($"[PsychologySystem] Generating TTS: \"{text}\" (emotion: {emotionalState})");

            // 非同期TTS生成タスクを開始
            Task<AudioClip> ttsTask = llmManager.GenerateTTSAsync(text, emotionalState);

            // タスク完了を待機
            while (!ttsTask.IsCompleted)
            {
                yield return null;
            }

            // エラーチェック
            if (ttsTask.IsFaulted)
            {
                Debug.LogError($"[PsychologySystem] TTS generation failed: {ttsTask.Exception?.Message}");
                yield break;
            }

            AudioClip audioClip = ttsTask.Result;

            if (audioClip != null)
            {
                Debug.Log($"[PsychologySystem] Playing TTS audio (length: {audioClip.length:F2}s)");

                // 音声再生
                audioSource.clip = audioClip;
                audioSource.volume = ttsVolume;
                audioSource.Play();

                // 再生完了まで待機
                yield return new WaitForSeconds(audioClip.length);

                Debug.Log("[PsychologySystem] TTS playback complete");
            }
            else
            {
                Debug.LogWarning("[PsychologySystem] TTS audio clip is null");
            }
        }

        /// <summary>
        /// カードホバー時のダイアログ生成（即座反応）
        /// </summary>
        /// <param name="cardIndex">ホバー中のカードインデックス</param>
        public void OnCardHover(int cardIndex)
        {
            if (!enableHoverDialogue || isProcessingDialogue)
            {
                return;
            }

            // クールダウン中はスキップ
            if (Time.time - lastHoverDialogueTime < hoverDialogueCooldown)
            {
                return;
            }

            // 現在の行動パターンを取得
            BehaviorPattern behavior = behaviorAnalyzer != null
                ? behaviorAnalyzer.CurrentBehavior
                : new BehaviorPattern();

            // ホバー時は軽めのダイアログ（General）
            lastHoverDialogueTime = Time.time;
            StartCoroutine(GenerateHoverDialogue(cardIndex, behavior));
        }

        /// <summary>
        /// ホバーダイアログ生成（キャッシュ優先）
        /// Phase 4: FloatingTextSystem統合
        /// </summary>
        private IEnumerator GenerateHoverDialogue(int cardIndex, BehaviorPattern behavior)
        {
            isProcessingDialogue = true;

            yield return new WaitForSeconds(hoverDialogueDelay);

            // 簡易ダイアログ（General カテゴリ）
            Task<string> dialogueTask = llmManager.GenerateDialogueAsync(
                DialogueCategoryType.General,
                behavior,
                currentPressureLevel
            );

            // タスク完了を待機
            while (!dialogueTask.IsCompleted)
            {
                yield return null;
            }

            if (!dialogueTask.IsFaulted && !string.IsNullOrEmpty(dialogueTask.Result))
            {
                string dialogue = dialogueTask.Result;
                Debug.Log($"[PsychologySystem] Hover dialogue: \"{dialogue}\"");

                OnDialogueGenerated?.Invoke(dialogue);
                OnDialogueDisplayed?.Invoke(dialogue);

                // LLM生成の長文はSubtitleUIで表示
                var subtitleUI = SubtitleUI.Instance;
                if (subtitleUI != null)
                {
                    subtitleUI.Show(dialogue, currentPressureLevel);
                }
                else if (floatingTextSystem != null)
                {
                    // SubtitleUI未設置時のフォールバック
                    Vector3 textPosition = CalculateCardHoverPosition(cardIndex);
                    floatingTextSystem.ShowText(textPosition, dialogue, currentPressureLevel);
                    Debug.Log($"[PsychologySystem] Floating text displayed at {textPosition}");
                }

                // TTS音声生成＆再生（Low優先度: 他の音声再生中はスキップ）
                if (llmManager != null && AudioManager.Instance != null
                    && !AudioManager.Instance.IsVoicePlaying())
                {
                    var emotion = emotionalStateManager?.CurrentState ?? FPSTrump.Psychology.AIEmotion.Calm;
                    var ttsTask = llmManager.GenerateTTSAsync(dialogue, emotion);
                    while (!ttsTask.IsCompleted) yield return null;
                    if (ttsTask.Result != null)
                    {
                        AudioManager.Instance.TryPlayVoice(ttsTask.Result, Vector3.zero, VoicePriority.Low, 1.0f);
                    }
                }
            }

            isProcessingDialogue = false;
        }

        /// <summary>
        /// カードインデックスからホバーテキスト表示位置を計算
        /// AIHandControllerからカード実体の位置を取得し、その上方にオフセット
        /// </summary>
        private Vector3 CalculateCardHoverPosition(int cardIndex)
        {
            // AIHandControllerから実際のカード位置を取得
            var aiHand = FindFirstObjectByType<AIHandController>();
            if (aiHand != null)
            {
                var cards = aiHand.GetCards();
                if (cardIndex >= 0 && cardIndex < cards.Count && cards[cardIndex] != null)
                {
                    Vector3 cardPos = cards[cardIndex].transform.position;
                    return cardPos + Vector3.up * 0.3f; // カードの少し上
                }
            }

            // フォールバック: 簡易計算
            float baseX = -0.2f + cardIndex * 0.1f;
            float baseY = 1.2f;
            float baseZ = 0.5f;
            return new Vector3(baseX, baseY, baseZ);
        }

        /// <summary>
        /// カードホバー時にFloatingTextを直接表示（CardObjectから呼び出し用）
        /// Phase 4: FloatingTextSystem統合
        /// </summary>
        /// <param name="cardPosition">カードのワールド座標</param>
        /// <param name="text">表示するテキスト</param>
        public void ShowFloatingTextAtCard(Vector3 cardPosition, string text = null)
        {
            if (floatingTextSystem == null) return;

            // テキストが指定されていない場合、現在の圧力レベルに応じたデフォルトテキストを使用
            if (string.IsNullOrEmpty(text))
            {
                text = GetDefaultFloatingText(currentPressureLevel);
            }

            // カードの少し上に表示
            Vector3 textPosition = cardPosition + Vector3.up * 0.15f;

            floatingTextSystem.ShowText(textPosition, text, currentPressureLevel);
            Debug.Log($"[PsychologySystem] Floating text at card: \"{text}\" (pressure={currentPressureLevel:F2})");
        }

        /// <summary>
        /// 圧力レベルに応じたデフォルトFloatingTextを取得
        /// </summary>
        private string GetDefaultFloatingText(float pressureLevel)
        {
            if (pressureLevel < 1.0f)
            {
                return "Interesting choice...";
            }
            else if (pressureLevel < 2.0f)
            {
                return "Are you sure?";
            }
            else
            {
                return "I can see your hesitation.";
            }
        }

        /// <summary>
        /// 圧力レベルを手動設定（デバッグ用）
        /// </summary>
        public void SetPressureLevel(float level)
        {
            currentPressureLevel = Mathf.Clamp(level, 0, maxPressureLevel);
            OnPressureLevelChanged?.Invoke(currentPressureLevel);
            Debug.Log($"[PsychologySystem] Pressure level manually set to {currentPressureLevel:F2}");
        }

        /// <summary>
        /// 圧力レベルを取得
        /// </summary>
        public float GetPressureLevel()
        {
            return currentPressureLevel;
        }

        /// <summary>
        /// PersonalityProfileをEmotionalStateManagerに反映（セッション初期化後に呼び出し）
        /// </summary>
        public void UpdatePlayerProfile(PersonalityProfile profile)
        {
            if (emotionalStateManager != null && profile != null)
            {
                emotionalStateManager.SetPlayerProfile(profile);
                Debug.Log("[PsychologySystem] Player profile updated in EmotionalStateManager");
            }
        }

        /// <summary>
        /// システムをリセット（新しいゲーム用）
        /// </summary>
        public void ResetSystem()
        {
            currentPressureLevel = basePressureLevel;
            isProcessingDialogue = false;

            if (behaviorAnalyzer != null)
            {
                behaviorAnalyzer.ClearHistory();
            }

            Debug.Log("[PsychologySystem] System reset");
        }

        /// <summary>
        /// 統計情報を取得（デバッグ用）
        /// </summary>
        public string GetStatistics()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Psychology System Stats ===");
            sb.AppendLine($"Pressure Level: {currentPressureLevel:F2}/{maxPressureLevel:F2}");
            sb.AppendLine($"Processing Dialogue: {isProcessingDialogue}");

            if (llmManager != null)
            {
                var stats = llmManager.GetStats();
                sb.AppendLine($"LLM Calls: {stats.totalCalls}");
                sb.AppendLine($"Cache Hit Rate: {stats.cacheHitRate:P0}");
            }

            return sb.ToString();
        }

#if UNITY_EDITOR
        private bool showDebugUI = false;

        /// <summary>
        /// デバッグUI（エディタのみ、F2キーでON/OFF）
        /// </summary>
        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F2)
            {
                showDebugUI = !showDebugUI;
                Event.current.Use();
            }

            if (!showDebugUI) return;

            GUILayout.BeginArea(new Rect(320, 10, 300, 200));
            GUILayout.Label(GetStatistics());

            if (GUILayout.Button("Reset Pressure"))
            {
                SetPressureLevel(basePressureLevel);
            }

            if (GUILayout.Button("Max Pressure"))
            {
                SetPressureLevel(maxPressureLevel);
            }

            GUILayout.EndArea();
        }
#endif

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (behaviorAnalyzer != null)
            {
                behaviorAnalyzer.OnBehaviorAnalyzed.RemoveListener(HandleBehaviorAnalysis);
            }
        }
    }
}
