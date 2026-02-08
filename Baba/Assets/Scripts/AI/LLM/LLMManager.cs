using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using FPSTrump.Manager;
using FPSTrump.Psychology;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// LLMManager: 全LLMインタラクションのコアオーケストレータ
    /// Claude/OpenAI API呼び出し、キャッシング、フォールバックを統合管理
    /// </summary>
    public class LLMManager : MonoBehaviour
    {
        public static LLMManager Instance { get; private set; }

        [Header("API Keys (Optional - Fallback)")]
        [Tooltip("Leave empty to use environment variables: CLAUDE_API_KEY, OPENAI_API_KEY")]
        [SerializeField] private string claudeAPIKeyFallback = "";
        [SerializeField] private string openAIAPIKeyFallback = "";

        [Header("Settings")]
        [SerializeField] private bool enableLLM = true;
        [SerializeField] private bool enableCache = true;
        [SerializeField] private bool enablePreWarming = true;
        [SerializeField] private int llmTimeout = 10000; // ms

        [Header("TTS Settings")]
        [SerializeField] private TTSProvider ttsProvider = TTSProvider.ElevenLabs;
        [Tooltip("ElevenLabs Voice ID (required when using ElevenLabs)")]
        [SerializeField] private string elevenLabsVoiceId = "UgBBYS2sOqTuMpoF3BR0";
        [SerializeField] private string elevenLabsModel = "eleven_multilingual_v2";
        [SerializeField] private string elevenLabsAPIKeyFallback = "";

        [Header("Session State (Runtime)")]
        [SerializeField] private AISessionState sessionState;

        /// <summary>
        /// 現在のプレイヤーPersonalityProfileを取得（診断照合用）
        /// </summary>
        public PersonalityProfile CurrentPlayerProfile => sessionState?.playerProfile;

        /// <summary>
        /// Stage 10: 現在のプレイヤー外見データを取得
        /// </summary>
        public PlayerAppearanceData CurrentPlayerAppearance => sessionState?.playerAppearance;

        // API Clients
        private ClaudeAPIClient claudeClient;
        private OpenAIAPIClient openAIClient;
        private ElevenLabsAPIClient elevenLabsClient;

        // Support Systems
        private ResponseCache responseCache;
        private FallbackManager fallbackManager;
        private LLMContextWindow contextWindow;

        // 事前生成された性格読みセリフ
        private List<string> preGeneratedPersonalityLines = new List<string>();

        /// <summary>
        /// 事前生成された性格読みセリフを取得
        /// </summary>
        public List<string> PreGeneratedPersonalityLines => preGeneratedPersonalityLines;

        // Statistics
        private int totalLLMCalls = 0;
        private int successfulCalls = 0;
        private int cachedCalls = 0;
        private int fallbackCalls = 0;

        private void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null)
                    transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                Debug.Log("[LLMManager] Instance created and marked as DontDestroyOnLoad");
            }
            else
            {
                Debug.LogWarning($"[LLMManager] Duplicate instance detected, destroying new instance");
                Destroy(gameObject);
                return;
            }

            InitializeAPIs();
            InitializeSupportSystems();
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
            // フォールバックテンプレート + 静的DBを再読込
            fallbackManager?.ReloadAll();

            // レスポンスキャッシュをクリア（旧言語の台詞を返さないように）
            responseCache?.Clear();

            Debug.Log($"[LLMManager] Language changed to {newLanguage}, caches reloaded");
        }

        private void InitializeAPIs()
        {
            // Claude APIクライアント初期化（環境変数 → Fallback）
            string claudeKey = GetAPIKey("CLAUDE_API_KEY", claudeAPIKeyFallback);
            if (!string.IsNullOrEmpty(claudeKey))
            {
                claudeClient = new ClaudeAPIClient(claudeKey);
                Debug.Log("[LLMManager] Claude API initialized");
            }
            else
            {
                Debug.LogWarning("[LLMManager] Claude API key not set. Set CLAUDE_API_KEY environment variable or assign in Inspector.");
            }

            // OpenAI APIクライアント初期化（環境変数 → Fallback）
            string openAIKey = GetAPIKey("OPENAI_API_KEY", openAIAPIKeyFallback);
            if (!string.IsNullOrEmpty(openAIKey))
            {
                openAIClient = new OpenAIAPIClient(openAIKey);
                Debug.Log("[LLMManager] OpenAI API initialized");
            }
            else
            {
                Debug.LogWarning("[LLMManager] OpenAI API key not set. Set OPENAI_API_KEY environment variable or assign in Inspector.");
            }

            // ElevenLabs APIクライアント初期化（環境変数 → Fallback）
            string elevenLabsKey = GetAPIKey("ELEVEN_API_KEY", elevenLabsAPIKeyFallback);
            if (!string.IsNullOrEmpty(elevenLabsKey))
            {
                elevenLabsClient = new ElevenLabsAPIClient(elevenLabsKey);
                Debug.Log("[LLMManager] ElevenLabs API initialized");
            }
            else
            {
                if (ttsProvider == TTSProvider.ElevenLabs)
                {
                    Debug.LogWarning("[LLMManager] ElevenLabs selected but API key not set. Will fall back to OpenAI TTS.");
                }
            }
        }

        /// <summary>
        /// APIキーを取得（優先順位: 環境変数 → APIKeyManager → Inspector）
        /// </summary>
        private string GetAPIKey(string envVarName, string fallbackValue)
        {
            // 優先度1: 環境変数から取得を試みる
            string envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(envValue))
            {
                Debug.Log($"[LLMManager] Using {envVarName} from environment variable");
                return envValue;
            }

            // 優先度2: APIKeyManagerから取得を試みる
            APIKeyManager apiKeyManager = APIKeyManager.Instance;
            if (apiKeyManager != null)
            {
                string managerKey = null;

                if (envVarName == "CLAUDE_API_KEY")
                {
                    managerKey = apiKeyManager.GetClaudeAPIKey();
                }
                else if (envVarName == "OPENAI_API_KEY")
                {
                    managerKey = apiKeyManager.GetOpenAIAPIKey();
                }
                else if (envVarName == "ELEVEN_API_KEY")
                {
                    managerKey = apiKeyManager.GetElevenLabsAPIKey();
                }

                if (!string.IsNullOrEmpty(managerKey))
                {
                    Debug.Log($"[LLMManager] Using {envVarName} from APIKeyManager (game settings)");
                    return managerKey;
                }
            }

            // 優先度3: Inspectorフォールバック値を使用
            if (!string.IsNullOrEmpty(fallbackValue))
            {
                Debug.LogWarning($"[LLMManager] Using {envVarName} from Inspector fallback (not recommended for production)");
                return fallbackValue;
            }

            return null;
        }

        private void InitializeSupportSystems()
        {
            responseCache = new ResponseCache();
            fallbackManager = new FallbackManager();
            contextWindow = new LLMContextWindow();

            // セッション状態をデフォルト値で初期化
            if (sessionState == null)
            {
                sessionState = new AISessionState();
                sessionState.Initialize(); // gameState等を初期化
                Debug.Log("[LLMManager] SessionState initialized with default values");
            }

            Debug.Log("[LLMManager] Support systems initialized");
        }

        /// <summary>
        /// セッションを初期化（ゲーム開始時）
        /// </summary>
        public async Task InitializeSession(PersonalityProfile playerProfile)
        {
            // セッション状態を新規作成
            if (sessionState == null)
            {
                sessionState = new AISessionState();
            }

            sessionState.playerProfile = playerProfile;
            sessionState.Initialize();

            // FallbackManagerにもPersonalityProfileを渡す
            fallbackManager?.SetPlayerProfile(playerProfile);

            // PsychologySystemのEmotionalStateManagerにも反映
            var psychologySystem = FPSTrump.Psychology.PsychologySystem.Instance;
            if (psychologySystem != null)
            {
                psychologySystem.UpdatePlayerProfile(playerProfile);
            }

            Debug.Log("[LLMManager] Session initialized");

            // 性格読みセリフ事前生成（非同期、ゲーム開始を遅延しない）
            _ = PreGeneratePersonalityLinesAsync(playerProfile);

            // キャッシュ事前ウォーミング
            if (enablePreWarming && enableCache)
            {
                await PreWarmCache();
            }
        }

        /// <summary>
        /// ダイアログ生成（メインメソッド）
        /// </summary>
        public async Task<string> GenerateDialogueAsync(
            DialogueCategoryType category,
            BehaviorPattern behaviorPattern,
            float pressureLevel)
        {
            totalLLMCalls++;

            // キャッシュチェック
            if (enableCache)
            {
                string cacheKey = BuildCacheKey(category, behaviorPattern, pressureLevel);
                if (responseCache.TryGet(cacheKey, out string cachedDialogue))
                {
                    cachedCalls++;
                    return cachedDialogue;
                }
            }

            // LLMが無効な場合、即座にフォールバック
            if (!enableLLM || claudeClient == null)
            {
                Debug.Log("[LLMManager] LLM disabled, using fallback");
                // fallbackCalls++; を削除（GenerateFallbackDialogue内で行う）
                return await GenerateFallbackDialogue(category, behaviorPattern, pressureLevel);
            }

            // フォールバックシステムを使ってダイアログ生成
            string dialogue = await fallbackManager.GetDialogueWithFallback(
                llmGenerateFunc: async () => await GenerateDialogueWithClaude(category, behaviorPattern, pressureLevel),
                category: category,
                behaviorPattern: behaviorPattern,
                pressureLevel: pressureLevel,
                timeoutMs: llmTimeout
            );

            if (dialogue != null)
            {
                successfulCalls++;

                // キャッシュに保存
                if (enableCache)
                {
                    string cacheKey = BuildCacheKey(category, behaviorPattern, pressureLevel);
                    responseCache.Set(cacheKey, dialogue);
                }
            }

            return dialogue ?? "Test: ...";
        }

        /// <summary>
        /// Claude APIを使ったダイアログ生成
        /// </summary>
        private async Task<string> GenerateDialogueWithClaude(
            DialogueCategoryType category,
            BehaviorPattern behaviorPattern,
            float pressureLevel)
        {
            if (claudeClient == null)
            {
                throw new Exception("Claude API client not initialized");
            }

            // コンテキストプロンプト構築
            string prompt = contextWindow.BuildContextPrompt(
                sessionState.playerProfile,
                behaviorPattern,
                sessionState.gameState,
                category,
                pressureLevel,
                sessionState.playerAppearance
            );

            // Claude API呼び出し
            string dialogue = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 150,
                temperature: 0.8f
            );

            // セッション状態更新
            sessionState.RecordDialogue(category, dialogue);

            return dialogue;
        }

        /// <summary>
        /// フォールバックダイアログ生成
        /// </summary>
        private async Task<string> GenerateFallbackDialogue(
            DialogueCategoryType category,
            BehaviorPattern behaviorPattern,
            float pressureLevel)
        {
            fallbackCalls++;

            // ルールベース生成器を使用
            return await fallbackManager.GetDialogueWithFallback(
                llmGenerateFunc: () => Task.FromResult<string>(null), // LLMをスキップ
                category: category,
                behaviorPattern: behaviorPattern,
                pressureLevel: pressureLevel,
                timeoutMs: 0 // タイムアウトなし
            );
        }

        /// <summary>
        /// 感情別ダイアログ生成（BuildEmotionalResponsePrompt経由）
        /// Layer B/C用。感情ごとに異なるJOKER的演技指示を使用。
        /// </summary>
        public async Task<string> GenerateEmotionalDialogueAsync(ResponseRequest request)
        {
            totalLLMCalls++;

            if (!enableLLM || claudeClient == null)
            {
                fallbackCalls++;
                return null;
            }

            string prompt = contextWindow.BuildEmotionalResponsePrompt(request);
            int maxTokens = request.layer == ResponseLayer.C ? 300 : 100;

            string dialogue = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: maxTokens,
                temperature: 0.9f
            );

            if (!string.IsNullOrEmpty(dialogue))
            {
                successfulCalls++;
            }

            return dialogue?.Trim().Replace("\"", "");
        }

        /// <summary>
        /// TTS音声生成（プロバイダ切替対応）
        /// </summary>
        /// <param name="useCache">キャッシュを使用するかどうか（デフォルト: true）</param>
        public async Task<AudioClip> GenerateTTSAsync(string text, AIEmotion emotionalState, bool useCache = true)
        {
            // プロバイダ判定
            bool useElevenLabs = (ttsProvider == TTSProvider.ElevenLabs
                && elevenLabsClient != null
                && !string.IsNullOrEmpty(elevenLabsVoiceId));
            bool useOpenAI = (ttsProvider == TTSProvider.OpenAI && openAIClient != null);

            // フォールバック: 選択プロバイダが利用不可なら他方を試行
            if (!useElevenLabs && !useOpenAI)
            {
                if (openAIClient != null)
                    useOpenAI = true;
                else if (elevenLabsClient != null && !string.IsNullOrEmpty(elevenLabsVoiceId))
                    useElevenLabs = true;
            }

            if (!useElevenLabs && !useOpenAI)
            {
                Debug.LogWarning("[LLMManager] No TTS client available, cannot generate TTS");
                return null;
            }

            // 音声キャッシュチェック
            if (useCache && enableCache && responseCache.TryGetAudio(text, out AudioClip cachedAudio))
            {
                return cachedAudio;
            }

            try
            {
                AudioClip audioClip;

                if (useElevenLabs)
                {
                    ElevenLabsVoiceSettings settings = SelectVoiceSettingsForEmotion(emotionalState);
                    audioClip = await elevenLabsClient.GenerateTTSAsync(
                        text: text,
                        voiceId: elevenLabsVoiceId,
                        voiceSettings: settings,
                        modelId: elevenLabsModel
                    );
                }
                else
                {
                    TTSVoice voice = SelectVoiceForEmotion(emotionalState);
                    audioClip = await openAIClient.GenerateTTSAsync(
                        text: text,
                        voice: voice,
                        model: "tts-1",
                        speed: 0.9f
                    );
                }

                // キャッシュに保存
                if (useCache && enableCache && audioClip != null)
                {
                    responseCache.SetAudio(text, audioClip);
                }

                return audioClip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMManager] TTS generation failed ({ttsProvider}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// CoTステップ全体のTTSを並列プリ生成
        /// </summary>
        public async Task<AudioClip[]> PreGenerateCoTTTSAsync(List<CoTStep> steps, AIEmotion emotion)
        {
            if (steps == null || steps.Count == 0) return null;

            var clips = new AudioClip[steps.Count];
            var tasks = new Task<AudioClip>[steps.Count];

            for (int i = 0; i < steps.Count; i++)
            {
                if (!string.IsNullOrEmpty(steps[i].thought))
                {
                    tasks[i] = GenerateTTSAsync(steps[i].thought, emotion);
                }
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i] != null)
                {
                    try
                    {
                        clips[i] = await tasks[i];
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LLMManager] CoT TTS step {i} failed: {ex.Message}");
                        clips[i] = null;
                    }
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int successCount = clips.Count(c => c != null);
            Debug.Log($"[LLMManager] CoT TTS pre-generated: {successCount}/{steps.Count} clips");
#endif
            return clips;
        }

        /// <summary>
        /// 感情状態に応じたボイス選択（AIEmotion 6感情版）
        /// </summary>
        private TTSVoice SelectVoiceForEmotion(AIEmotion state)
        {
            // Onyx = 渋い男性低音をベースに、感情で微妙に使い分け
            return state switch
            {
                AIEmotion.Pleased => TTSVoice.Onyx,          // 余裕のある低音
                AIEmotion.Frustrated => TTSVoice.Echo,       // やや鋭い声
                AIEmotion.Hurt => TTSVoice.Onyx,             // 抑えた低音
                AIEmotion.Relieved => TTSVoice.Onyx,         // 落ち着いた低音
                AIEmotion.Anticipating => TTSVoice.Echo,     // 緊張感のある声
                AIEmotion.Calm => TTSVoice.Onyx,             // デフォルト渋い低音
                _ => TTSVoice.Onyx
            };
        }

        /// <summary>
        /// 感情状態に応じたElevenLabsボイス設定選択
        /// </summary>
        private ElevenLabsVoiceSettings SelectVoiceSettingsForEmotion(AIEmotion state)
        {
            return state switch
            {
                AIEmotion.Calm => new ElevenLabsVoiceSettings
                {
                    stability = 0.70f,
                    similarity_boost = 0.75f,
                    style = 0.0f,
                    use_speaker_boost = true
                },
                AIEmotion.Anticipating => new ElevenLabsVoiceSettings
                {
                    stability = 0.40f,
                    similarity_boost = 0.75f,
                    style = 0.30f,
                    use_speaker_boost = true
                },
                AIEmotion.Pleased => new ElevenLabsVoiceSettings
                {
                    stability = 0.55f,
                    similarity_boost = 0.80f,
                    style = 0.40f,
                    use_speaker_boost = true
                },
                AIEmotion.Frustrated => new ElevenLabsVoiceSettings
                {
                    stability = 0.30f,
                    similarity_boost = 0.70f,
                    style = 0.50f,
                    use_speaker_boost = true
                },
                AIEmotion.Hurt => new ElevenLabsVoiceSettings
                {
                    stability = 0.60f,
                    similarity_boost = 0.80f,
                    style = 0.20f,
                    use_speaker_boost = false
                },
                AIEmotion.Relieved => new ElevenLabsVoiceSettings
                {
                    stability = 0.65f,
                    similarity_boost = 0.75f,
                    style = 0.15f,
                    use_speaker_boost = true
                },
                _ => new ElevenLabsVoiceSettings
                {
                    stability = 0.50f,
                    similarity_boost = 0.75f,
                    style = 0.0f,
                    use_speaker_boost = true
                }
            };
        }

        /// <summary>
        /// 性格プロファイルのLLM強化
        /// </summary>
        public async Task<PersonalityEnhancement> EnhancePersonalityProfileAsync(PersonalityProfile baseProfile)
        {
            if (claudeClient == null)
            {
                Debug.LogWarning("[LLMManager] Claude client not initialized, skipping profile enhancement");
                return null;
            }

            try
            {
                string prompt = $@"Based on this personality profile:
- Cautiousness: {baseProfile.cautiousness:F2}
- Intuition: {baseProfile.intuition:F2}
- Resilience: {baseProfile.resilience:F2}
- Curiosity: {baseProfile.curiosity:F2}
- Decision Style: {baseProfile.primaryDecisionStyle}

Identify 3 psychological weaknesses and 3 effective exploitation strategies for a card game.
Format as JSON:
{{
  ""weaknesses"": [""weakness1"", ""weakness2"", ""weakness3""],
  ""strategies"": [""strategy1"", ""strategy2"", ""strategy3""]
}}

Output ONLY the JSON, no other text.";

                string response = await claudeClient.GenerateDialogueAsync(
                    prompt: prompt,
                    maxTokens: 300,
                    temperature: 0.7f
                );

                // JSONパース
                PersonalityEnhancement enhancement = JsonConvert.DeserializeObject<PersonalityEnhancement>(response);

                Debug.Log($"[LLMManager] Personality enhancement complete: {enhancement.weaknesses.Length} weaknesses, {enhancement.strategies.Length} strategies");

                return enhancement;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMManager] Personality enhancement failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// キャッシュ事前ウォーミング
        /// </summary>
        private async Task PreWarmCache()
        {
            Debug.Log("[LLMManager] Starting cache pre-warming...");

            string[] commonScenarios = new[]
            {
                "category:stop,pressure:medium",
                "category:bait,pressure:high",
                "category:mirror,pressure:low",
                "category:general,pressure:medium"
            };

            try
            {
                await responseCache.PreWarmCache(
                    generateFunc: async (prompt, maxTokens, temp) =>
                    {
                        if (claudeClient != null)
                        {
                            return await claudeClient.GenerateDialogueAsync(prompt, maxTokens, temp);
                        }
                        return string.Empty;
                    },
                    commonScenarios: commonScenarios
                );

                Debug.Log("[LLMManager] Cache pre-warming complete");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Cache pre-warming failed: {ex.Message}");
            }
        }

        /// <summary>
        /// キャッシュキー構築
        /// </summary>
        private string BuildCacheKey(DialogueCategoryType category, BehaviorPattern pattern, float pressureLevel)
        {
            // 簡略化したキー生成
            int pressureBucket = Mathf.FloorToInt(pressureLevel * 2); // 0.5刻み
            string patternSummary = $"d{Mathf.FloorToInt(pattern.doubtLevel * 10)}_t{pattern.tempo}_s{pattern.streakSamePosition}";

            return $"{category}_{patternSummary}_p{pressureBucket}";
        }

        /// <summary>
        /// 統計情報取得
        /// </summary>
        public LLMStats GetStats()
        {
            var cacheStats = (enableCache && responseCache != null) ? responseCache.GetStats() : new CacheStats();

            return new LLMStats
            {
                totalCalls = totalLLMCalls,
                successfulCalls = successfulCalls,
                cachedCalls = cachedCalls,
                fallbackCalls = fallbackCalls,
                cacheHitRate = totalLLMCalls > 0 ? (float)cachedCalls / totalLLMCalls : 0f,
                cacheStats = cacheStats
            };
        }

        /// <summary>
        /// 統計情報をログ出力
        /// </summary>
        public void LogStats()
        {
            var stats = GetStats();
            Debug.Log($@"[LLMManager] Statistics:
- Total Calls: {stats.totalCalls}
- Successful: {stats.successfulCalls}
- Cached: {stats.cachedCalls}
- Fallback: {stats.fallbackCalls}
- Cache Hit Rate: {stats.cacheHitRate:P1}
- {stats.cacheStats}");
        }

        /// <summary>
        /// AIカード選択判断を生成（LLM強化版）
        /// プレイヤーの心理状態とゲーム状況から最適なカード位置を決定
        /// </summary>
        /// <param name="behaviorPattern">プレイヤーの行動パターン</param>
        /// <param name="pressureLevel">現在の圧力レベル (0-3)</param>
        /// <param name="playerCardCount">プレイヤーのカード枚数</param>
        /// <returns>AI決定結果（selectedCardIndex, confidence, strategy）</returns>
        public async Task<AIDecisionResult> GenerateAIDecisionAsync(
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int playerCardCount)
        {
            totalLLMCalls++;

            // キャッシュチェック（同じパターンの決定を再利用）
            if (enableCache)
            {
                string cacheKey = BuildDecisionCacheKey(behaviorPattern, pressureLevel, playerCardCount);
                if (responseCache.TryGetDecision(cacheKey, out AIDecisionResult cachedDecision))
                {
                    cachedCalls++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[LLMManager] Decision cache HIT: position={cachedDecision.selectedCardIndex}");
#endif
                    return cachedDecision;
                }
            }

            // LLMが無効な場合、即座にフォールバック
            if (!enableLLM || claudeClient == null)
            {
                Debug.Log("[LLMManager] LLM disabled, using fallback for decision");
                // fallbackCalls++; を削除（GenerateFallbackDecision内で行う）
                return GenerateFallbackDecision(behaviorPattern, pressureLevel, playerCardCount);
            }

            // フォールバックシステムを使ってAI判断生成
            AIDecisionResult decision = await fallbackManager.GetAIDecisionWithFallback(
                llmGenerateFunc: async () => await GenerateDecisionWithClaude(
                    behaviorPattern, pressureLevel, playerCardCount
                ),
                behaviorPattern: behaviorPattern,
                pressureLevel: pressureLevel,
                playerCardCount: playerCardCount,
                timeoutMs: llmTimeout
            );

            if (decision != null)
            {
                successfulCalls++;

                // キャッシュに保存
                if (enableCache)
                {
                    string cacheKey = BuildDecisionCacheKey(behaviorPattern, pressureLevel, playerCardCount);
                    responseCache.SetDecision(cacheKey, decision);
                }

                // セッション状態に記録
                if (sessionState != null)
                {
                    sessionState.RecordAIDecision(decision);
                }

                // CoTステップがない場合はフォールバック生成
                if (decision.cotSteps == null || decision.cotSteps.Count == 0)
                {
                    decision.cotSteps = fallbackManager.GenerateFallbackCoTSteps(
                        decision, behaviorPattern, pressureLevel, playerCardCount);
                    Debug.Log($"[LLMManager] Generated fallback CoT: {decision.cotSteps?.Count ?? 0} steps");
                }
            }

            var finalDecision = decision ?? GenerateFallbackDecision(behaviorPattern, pressureLevel, playerCardCount);

            // 最終フォールバック: decision自体がフォールバックの場合もCoTを生成
            if (finalDecision != null && (finalDecision.cotSteps == null || finalDecision.cotSteps.Count == 0))
            {
                finalDecision.cotSteps = fallbackManager.GenerateFallbackCoTSteps(
                    finalDecision, behaviorPattern, pressureLevel, playerCardCount);
            }

            return finalDecision;
        }

        /// <summary>
        /// Claude APIを使ったAI判断生成
        /// </summary>
        private async Task<AIDecisionResult> GenerateDecisionWithClaude(
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int playerCardCount)
        {
            if (claudeClient == null)
            {
                throw new Exception("Claude API client not initialized");
            }

            // sessionStateとgameStateの初期化チェック追加
            if (sessionState == null)
            {
                Debug.LogWarning("[LLMManager] sessionState is null, initializing defaults");
                sessionState = new AISessionState();
                sessionState.Initialize();
            }

            if (sessionState.gameState == null)
            {
                Debug.LogWarning("[LLMManager] gameState is null, initializing defaults");
                sessionState.gameState = new GameStateData();
            }

            // コンテキストプロンプト構築
            string prompt = contextWindow.BuildCardSelectionPrompt(
                sessionState.playerProfile,
                behaviorPattern,
                sessionState.gameState,
                pressureLevel,
                playerCardCount
            );

            // Claude API呼び出し（CoTステップ含むため300トークン）
            string response = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 300,  // CoTステップ含むJSON応答
                temperature: 0.6f // ダイアログより低い温度で一貫性重視
            );

            // JSONパース
            AIDecisionResult decision = ParseDecisionResponse(response, playerCardCount);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[LLMManager] Claude AI Decision: position={decision.selectedCardIndex}, confidence={decision.confidence:F2}, strategy={decision.strategy}");
#endif

            return decision;
        }

        /// <summary>
        /// LLM応答をパース
        /// </summary>
        private AIDecisionResult ParseDecisionResponse(string response, int playerCardCount)
        {
            try
            {
                // JSON形式を期待: {"position": 1, "confidence": 0.85, "strategy": "Cautious"}
                var jsonResponse = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(response);

                // nullチェック追加
                if (jsonResponse == null)
                {
                    throw new Exception("JSON deserialization returned null");
                }

                // キー存在チェック追加
                if (!jsonResponse.ContainsKey("position"))
                    throw new Exception("Missing 'position' key in JSON");
                if (!jsonResponse.ContainsKey("confidence"))
                    throw new Exception("Missing 'confidence' key in JSON");
                if (!jsonResponse.ContainsKey("strategy"))
                    throw new Exception("Missing 'strategy' key in JSON");

                int position = Convert.ToInt32(jsonResponse["position"]);
                float confidence = Convert.ToSingle(jsonResponse["confidence"]);
                string strategy = jsonResponse["strategy"]?.ToString() ?? "Adaptive"; // null安全に変更

                // バリデーション
                position = Mathf.Clamp(position, 0, playerCardCount - 1);
                confidence = Mathf.Clamp01(confidence);

                // CoTステップのパース
                List<CoTStep> cotSteps = null;
                if (jsonResponse.ContainsKey("steps"))
                {
                    try
                    {
                        var stepsJson = JsonConvert.SerializeObject(jsonResponse["steps"]);
                        cotSteps = JsonConvert.DeserializeObject<List<CoTStep>>(stepsJson);

                        if (cotSteps != null)
                        {
                            foreach (var step in cotSteps)
                            {
                                step.cardIndex = Mathf.Clamp(step.cardIndex, 0, playerCardCount - 1);
                            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"[LLMManager] Parsed {cotSteps.Count} CoT steps");
#endif
                        }
                    }
                    catch (Exception cotEx)
                    {
                        Debug.LogWarning($"[LLMManager] CoT steps parse failed: {cotEx.Message}");
                        cotSteps = null;
                    }
                }

                return new AIDecisionResult
                {
                    selectedCardIndex = position,
                    confidence = confidence,
                    strategy = strategy,
                    cotSteps = cotSteps
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Failed to parse decision response: {ex.Message}");

                // フォールバック: テキストから数字を抽出
                return ExtractDecisionFromText(response, playerCardCount);
            }
        }

        /// <summary>
        /// テキストから決定を抽出（パースフォールバック）
        /// </summary>
        private AIDecisionResult ExtractDecisionFromText(string text, int playerCardCount)
        {
            // "position 1" や "select 2" などのパターンを検索
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"(?:position|select|choice|card)\s*[:=]?\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            int position = 1; // デフォルトは中央
            if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed))
            {
                position = Mathf.Clamp(parsed, 0, playerCardCount - 1);
            }

            Debug.Log($"[LLMManager] Extracted decision from text: position={position}");

            return new AIDecisionResult
            {
                selectedCardIndex = position,
                confidence = 0.5f,
                strategy = "Adaptive"
            };
        }

        /// <summary>
        /// フォールバック判断生成（ルールベース）
        /// </summary>
        private AIDecisionResult GenerateFallbackDecision(
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int playerCardCount)
        {
            fallbackCalls++;

            Debug.Log("[LLMManager] Using rule-based fallback decision");

            // FallbackManagerのルールベース判断を再利用
            return fallbackManager.GenerateRuleBasedDecision(
                behaviorPattern,
                pressureLevel,
                playerCardCount
            );
        }

        /// <summary>
        /// 決定キャッシュキー構築
        /// </summary>
        private string BuildDecisionCacheKey(
            BehaviorPattern pattern,
            float pressureLevel,
            int playerCardCount)
        {
            int pressureBucket = Mathf.FloorToInt(pressureLevel * 2);

            // tempoを追加、preferredPositionを明示的に処理
            string patternSummary = $"d{Mathf.FloorToInt(pattern.doubtLevel * 10)}" +
                $"_t{pattern.tempo}" +
                $"_pos{(pattern.preferredPosition >= 0 ? pattern.preferredPosition : -1)}" +
                $"_s{pattern.streakSamePosition}";

            return $"decision_{patternSummary}_pr{pressureBucket}_c{playerCardCount}";
        }

        // ===== Stage 6: Hesitation Dialogue Generation =====

        /// <summary>
        /// AI迷い中セリフ生成（スタイル×性格対応）
        /// </summary>
        public async Task<string> GenerateHesitationDialogue(
            int cardIndex,
            int totalCards,
            float pressureLevel,
            BehaviorPattern behaviorPattern = null,
            AIHesitationController.HesitationStyle style = AIHesitationController.HesitationStyle.Deduction,
            PersonalityProfile playerProfile = null,
            AIHesitationController.HesitationContext gameContext = default)
        {
            totalLLMCalls++;

            if (!enableLLM || claudeClient == null)
            {
                Debug.Log("[LLMManager] LLM disabled, using fallback for hesitation");
                return null; // AIHesitationControllerのフォールバックに委譲
            }

            try
            {
                var llmTask = GenerateHesitationWithClaude(cardIndex, totalCards, pressureLevel, behaviorPattern, style, playerProfile, gameContext);
                var timeoutTask = Task.Delay(llmTimeout);

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        Debug.Log($"[LLMManager] Hesitation LLM [{style}] SUCCESS: \"{result}\"");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMManager] Hesitation LLM TIMEOUT ({llmTimeout}ms), falling back");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Hesitation LLM ERROR: {ex.Message}, falling back");
            }

            return null; // AIHesitationControllerのフォールバックに委譲
        }

        /// <summary>
        /// Claude APIを使ったHesitationセリフ生成（スタイル×性格対応）
        /// </summary>
        private async Task<string> GenerateHesitationWithClaude(
            int cardIndex,
            int totalCards,
            float pressureLevel,
            BehaviorPattern behaviorPattern,
            AIHesitationController.HesitationStyle style,
            PersonalityProfile playerProfile,
            AIHesitationController.HesitationContext gameContext)
        {
            if (claudeClient == null)
            {
                throw new Exception("Claude API client not initialized");
            }

            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            string cardPositionText, pressureText;
            string behaviorContext = "";
            string personalityContext = "";
            string gameContextText = "";
            string styleInstruction;
            string fortuneContext = "";
            string appearanceContext = "";
            string facialContext = "";

            if (isJa)
            {
                cardPositionText = cardIndex == totalCards - 1 ? "最後" : $"{cardIndex + 1}ステップ目";
                pressureText = pressureLevel < 1.0f ? "低" : pressureLevel < 2.0f ? "中" : "高";

                if (behaviorPattern != null)
                {
                    string tempoDesc = behaviorPattern.tempo == TempoType.Fast ? "速い"
                        : behaviorPattern.tempo == TempoType.Slow ? "遅い"
                        : behaviorPattern.tempo == TempoType.Erratic ? "不規則" : "普通";
                    behaviorContext = $@"
プレイヤーの行動パターン:
- 決断テンポ: {tempoDesc}
- 迷い度: {behaviorPattern.doubtLevel:F2} (0=即決, 1=非常に迷う)
- 位置偏好: {(behaviorPattern.hasPositionPreference ? $"あり（{behaviorPattern.preferredPosition}）" : "なし")}";
                }

                if (playerProfile != null)
                {
                    var (dominantTrait, traitValue) = playerProfile.GetDominantTrait();
                    string traitLabel = dominantTrait ?? "不明";
                    personalityContext = $@"
プレイヤーの性格プロファイル:
- 最も顕著な特性: {traitLabel}（{traitValue:F2}）
- 意思決定スタイル: {playerProfile.primaryDecisionStyle}
- ストレス反応: {playerProfile.stressType}
この性格情報を根拠として台詞に織り込め。";
                }

                // Stage 15.5: 占いデータ、外見、表情を追加
                fortuneContext = GetFortuneContextForHesitation();
                appearanceContext = GetAppearanceContextForHesitation();
                facialContext = GetFacialContextForHesitation(isJa);

                // ゲーム状況コンテキスト
                if (gameContext.aiCardCount > 0 || gameContext.playerCardCount > 0)
                {
                    string phaseText = gameContext.Phase switch
                    {
                        AIHesitationController.GamePhase.Early => "序盤",
                        AIHesitationController.GamePhase.Late => "終盤",
                        _ => "中盤"
                    };
                    string advantageText = gameContext.Advantage switch
                    {
                        AIHesitationController.GameAdvantage.Winning => "優勢",
                        AIHesitationController.GameAdvantage.Losing => "劣勢",
                        _ => "拮抗"
                    };
                    string jokerText = gameContext.aiHoldsJoker
                        ? "AIが所持（隠したい）"
                        : "プレイヤー側にある可能性";
                    gameContextText = $@"
ゲーム状況:
- AI手札: {gameContext.aiCardCount}枚 / プレイヤー手札: {gameContext.playerCardCount}枚
- 局面: {phaseText}（{advantageText}）
- ジョーカー: {jokerText}
この状況を台詞に反映せよ。";
                }

                styleInstruction = style switch
                {
                    AIHesitationController.HesitationStyle.Deduction =>
                        "【推測】楽しそうに推理するピエロ。「あっ！ わかっちゃった〜♪ 君ってさぁ...」と友達みたいに分析。でも急に「...全部見えてるよ」と不気味に。",
                    AIHesitationController.HesitationStyle.Bluff =>
                        "【ブラフ】わかったフリを芝居がかってやる。「もう決まってるんだよね〜♪ ...嘘かもね？ あはは！」怖いジョーク混じりのハッタリ。",
                    AIHesitationController.HesitationStyle.Provoke =>
                        "【煽り】馴れ馴れしく煽る。「ねえねえ、緊張してる？ あはは！ ...するよね、普通」と笑いながら。豹変で急に「...逃げ場ないよ」。",
                    AIHesitationController.HesitationStyle.Vulnerable =>
                        "【弱気】ピエロの仮面がズレる瞬間。「えっ...まって...これ読めない...」と素の困惑。すぐ「あっはは！ 冗談冗談♪」と取り繕うが目が笑ってない。",
                    _ => "おしゃべりなピエロがカード選びで独り言を言う"
                };
            }
            else
            {
                cardPositionText = cardIndex == totalCards - 1 ? "last step" : $"step {cardIndex + 1}";
                pressureText = pressureLevel < 1.0f ? "low" : pressureLevel < 2.0f ? "medium" : "high";

                if (behaviorPattern != null)
                {
                    string tempoDesc = behaviorPattern.tempo == TempoType.Fast ? "fast"
                        : behaviorPattern.tempo == TempoType.Slow ? "slow"
                        : behaviorPattern.tempo == TempoType.Erratic ? "erratic" : "normal";
                    behaviorContext = $@"
Player behavior pattern:
- Decision tempo: {tempoDesc}
- Doubt level: {behaviorPattern.doubtLevel:F2} (0=decisive, 1=very hesitant)
- Position preference: {(behaviorPattern.hasPositionPreference ? $"yes (position {behaviorPattern.preferredPosition})" : "none")}";
                }

                if (playerProfile != null)
                {
                    var (dominantTrait, traitValue) = playerProfile.GetDominantTrait();
                    string traitLabel = dominantTrait ?? "unknown";
                    personalityContext = $@"
Player personality profile:
- Dominant trait: {traitLabel} ({traitValue:F2})
- Decision style: {playerProfile.primaryDecisionStyle}
- Stress response: {playerProfile.stressType}
Weave this personality data into the dialogue as evidence.";
                }

                // Stage 15.5: Add fortune, appearance, and facial data
                fortuneContext = GetFortuneContextForHesitation();
                appearanceContext = GetAppearanceContextForHesitation();
                facialContext = GetFacialContextForHesitation(isJa);

                // Game situation context
                if (gameContext.aiCardCount > 0 || gameContext.playerCardCount > 0)
                {
                    string phaseText = gameContext.Phase switch
                    {
                        AIHesitationController.GamePhase.Early => "early",
                        AIHesitationController.GamePhase.Late => "endgame",
                        _ => "mid"
                    };
                    string advantageText = gameContext.Advantage switch
                    {
                        AIHesitationController.GameAdvantage.Winning => "winning",
                        AIHesitationController.GameAdvantage.Losing => "losing",
                        _ => "even"
                    };
                    string jokerText = gameContext.aiHoldsJoker
                        ? "AI holds it (wants to hide)"
                        : "possibly with player";
                    gameContextText = $@"
Game situation:
- AI cards: {gameContext.aiCardCount} / Player cards: {gameContext.playerCardCount}
- Phase: {phaseText} ({advantageText})
- Joker: {jokerText}
Reflect this situation in the dialogue.";
                }

                styleInstruction = style switch
                {
                    AIHesitationController.HesitationStyle.Deduction =>
                        "[Deduction] Gleefully analyze like a chatty clown. 'Ooh! I see it now~! You always...' then suddenly cold: '...I see everything.' Fun then creepy.",
                    AIHesitationController.HesitationStyle.Bluff =>
                        "[Bluff] Theatrical fake confidence. 'Already decided~ ...or maybe not? Ahahaha!' Scary joke bluffing. Playful but unsettling.",
                    AIHesitationController.HesitationStyle.Provoke =>
                        "[Provoke] Friendly taunting. 'Hey hey, nervous? Haha! ...you should be.' Laughing while cornering. Sudden shift to cold: '...no escape.'",
                    AIHesitationController.HesitationStyle.Vulnerable =>
                        "[Vulnerable] The clown mask slips. 'Wait... I can't read this...' genuine confusion. Then quickly: 'Ahaha! Just kidding~!' but eyes aren't smiling.",
                    _ => "A chatty clown muttering to himself while choosing a card"
                };
            }

            // プレイヤー名を取得
            string playerNameInstruction = "";
            var nameManager = FPSTrump.Manager.PlayerNameManager.Instance;
            if (nameManager != null && nameManager.HasName())
            {
                string playerName = nameManager.GetName();
                playerNameInstruction = isJa
                    ? $"\n重要: プレイヤーの名前は「{playerName}」。必ず名前で呼べ。「{playerName}」「ねえ{playerName}」のように。"
                    : $"\nIMPORTANT: Player's name is \"{playerName}\". ALWAYS address by name: \"{playerName}\", \"Hey {playerName}\", etc.";
            }

            string prompt;
            if (isJa)
            {
                prompt = $@"あなたはJOKERのような振る舞いをするおしゃべりAI。カード選びの最中も黙っていられない。

状況:
- {cardPositionText}（全{totalCards}ステップ中）
- 心理圧: {pressureText}（{pressureLevel:F1}/3.0）{behaviorContext}{personalityContext}{fortuneContext}{appearanceContext}{facialContext}{gameContextText}{playerNameInstruction}

{styleInstruction}

口調: 砕けた話し言葉（〜だよ、〜じゃん、〜でしょ）。感嘆詞多め（あはは、おっと、んー）。
豹変時は「...」でトーンを落とし語尾が「だ」「だろう」に変わる。
日本語で短い台詞（最大20文字）を1つだけ生成。台詞のみ出力。";
            }
            else
            {
                prompt = $@"You are a chatty AI that acts like the Joker. You can't stop talking even while picking a card.

Situation:
- {cardPositionText} (of {totalCards} total)
- Psychological pressure: {pressureText} ({pressureLevel:F1}/3.0){behaviorContext}{personalityContext}{fortuneContext}{appearanceContext}{facialContext}{gameContextText}{playerNameInstruction}

{styleInstruction}

Tone: casual, theatrical, exclamatory (ahahaha, oops, hmm~). Mix playful with sudden cold shifts (...).
Generate exactly ONE short dialogue line (max 10 words) in English.
No explanation. Only the dialogue line.";
            }

            string dialogue = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 60,
                temperature: 0.9f
            );

            dialogue = dialogue.Trim()
                .Replace("\"", "")
                .Replace("'", "")
                .Replace("Test: ", "");

            return dialogue;
        }

        // ===== Stage 7: Diagnosis Generation =====

        /// <summary>
        /// Stage 7: リザルト診断テキスト生成
        /// </summary>
        /// <param name="prompt">診断プロンプト（ResultDiagnosisPromptが構築）</param>
        /// <param name="timeoutMs">タイムアウト（ms）。ゲーム終了画面なので長め許容</param>
        /// <returns>LLM生成テキスト（JSON形式）、失敗時null</returns>
        public async Task<string> GenerateDiagnosisAsync(string prompt, int timeoutMs = 5000)
        {
            totalLLMCalls++;

            // LLMが無効な場合
            if (!enableLLM || claudeClient == null)
            {
                Debug.Log("[LLMManager] LLM disabled, returning null for diagnosis");
                fallbackCalls++;
                return null;
            }

            try
            {
                var llmTask = claudeClient.GenerateDialogueAsync(
                    prompt: prompt,
                    maxTokens: 500,
                    temperature: 0.7f
                );
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        Debug.Log($"[LLMManager] Diagnosis LLM SUCCESS ({result.Length} chars)");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMManager] Diagnosis LLM TIMEOUT ({timeoutMs}ms)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Diagnosis LLM ERROR: {ex.Message}");
            }

            fallbackCalls++;
            return null;
        }

        // ===== Inspector Test Methods =====

#if UNITY_EDITOR
        /// <summary>
        /// Claude API接続テスト（Inspectorから実行可能）
        /// </summary>
        [ContextMenu("Test Claude API Connection")]
        private async void TestClaudeAPIConnection()
        {
            Debug.Log("========================================");
            Debug.Log("[LLMManager] Testing Claude API Connection...");
            Debug.Log("========================================");

            // LLM有効チェック
            if (!enableLLM)
            {
                Debug.LogWarning("[LLMManager] ⚠️ enableLLM is FALSE. Set to TRUE to test actual LLM connection.");
                return;
            }

            // APIキー取得
            string apiKey = GetAPIKey("CLAUDE_API_KEY", claudeAPIKeyFallback);

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[LLMManager] ❌ Claude API Key is not set!");
                Debug.LogError("Set claudeAPIKeyFallback in Inspector or use Environment Variable CLAUDE_API_KEY");
                return;
            }

            Debug.Log($"[LLMManager] ✅ API Key found: {apiKey.Substring(0, 10)}...");

            // クライアント初期化（まだの場合）
            if (claudeClient == null)
            {
                claudeClient = new ClaudeAPIClient(apiKey);
                Debug.Log("[LLMManager] Claude client initialized for testing");
            }

            try
            {
                // テストプロンプト
                string testPrompt = @"You are testing the Claude API connection.
Please respond with a short Japanese phrase (under 15 words) saying the connection is successful.
Keep it friendly and concise.";

                Debug.Log("[LLMManager] 🔄 Sending test request to Claude API...");

                float startTime = Time.realtimeSinceStartup;

                string response = await claudeClient.GenerateDialogueAsync(
                    prompt: testPrompt,
                    maxTokens: 100,
                    temperature: 0.7f
                );

                float elapsedTime = Time.realtimeSinceStartup - startTime;

                Debug.Log("========================================");
                Debug.Log($"[LLMManager] ✅ REAL CLAUDE API SUCCESS!");
                Debug.Log($"[LLMManager] Response Time: {elapsedTime:F2}s");
                Debug.Log($"[LLMManager] Response: \"{response}\"");
                Debug.Log($"[LLMManager] Response Length: {response.Length} chars");
                Debug.Log("========================================");
                Debug.Log("[LLMManager] This is a REAL LLM response, not fallback!");
                Debug.Log("========================================");
            }
            catch (Exception ex)
            {
                Debug.LogError("========================================");
                Debug.LogError($"[LLMManager] ❌ Claude API Connection FAILED!");
                Debug.LogError($"[LLMManager] Error: {ex.Message}");
                Debug.LogError($"[LLMManager] Type: {ex.GetType().Name}");
                Debug.LogError("========================================");

                if (ex.InnerException != null)
                {
                    Debug.LogError($"[LLMManager] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// OpenAI API接続テスト（Inspectorから実行可能）
        /// </summary>
        [ContextMenu("Test OpenAI API Connection")]
        private async void TestOpenAIAPIConnection()
        {
            Debug.Log("========================================");
            Debug.Log("[LLMManager] Testing OpenAI API Connection...");
            Debug.Log("========================================");

            // LLM有効チェック
            if (!enableLLM)
            {
                Debug.LogWarning("[LLMManager] ⚠️ enableLLM is FALSE. Set to TRUE to test actual LLM connection.");
                return;
            }

            // APIキー取得
            string apiKey = GetAPIKey("OPENAI_API_KEY", openAIAPIKeyFallback);

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[LLMManager] ❌ OpenAI API Key is not set!");
                Debug.LogError("Set openAIAPIKeyFallback in Inspector or use Environment Variable OPENAI_API_KEY");
                return;
            }

            Debug.Log($"[LLMManager] ✅ API Key found: {apiKey.Substring(0, 10)}...");

            // クライアント初期化（まだの場合）
            if (openAIClient == null)
            {
                openAIClient = new OpenAIAPIClient(apiKey);
                Debug.Log("[LLMManager] OpenAI client initialized for testing");
            }

            try
            {
                // テストプロンプト（TTS用）
                string testText = "OpenAI接続テスト成功です";

                Debug.Log("[LLMManager] 🔄 Sending test TTS request to OpenAI API...");

                float startTime = Time.realtimeSinceStartup;

                AudioClip audioClip = await openAIClient.GenerateTTSAsync(
                    text: testText,
                    voice: TTSVoice.Alloy,
                    speed: 1.0f
                );

                float elapsedTime = Time.realtimeSinceStartup - startTime;

                Debug.Log("========================================");
                Debug.Log($"[LLMManager] ✅ REAL OPENAI API SUCCESS!");
                Debug.Log($"[LLMManager] Response Time: {elapsedTime:F2}s");
                Debug.Log($"[LLMManager] Audio Clip: {(audioClip != null ? "Generated" : "NULL")}");

                if (audioClip != null)
                {
                    Debug.Log($"[LLMManager] Audio Length: {audioClip.length:F2}s");
                    Debug.Log($"[LLMManager] Channels: {audioClip.channels}");
                    Debug.Log($"[LLMManager] Frequency: {audioClip.frequency}Hz");
                }

                Debug.Log("========================================");
                Debug.Log("[LLMManager] This is a REAL OpenAI TTS response, not fallback!");
                Debug.Log("========================================");
            }
            catch (Exception ex)
            {
                Debug.LogError("========================================");
                Debug.LogError($"[LLMManager] ❌ OpenAI API Connection FAILED!");
                Debug.LogError($"[LLMManager] Error: {ex.Message}");
                Debug.LogError($"[LLMManager] Type: {ex.GetType().Name}");
                Debug.LogError("========================================");

                if (ex.InnerException != null)
                {
                    Debug.LogError($"[LLMManager] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// ElevenLabs API接続テスト（Inspectorから実行可能）
        /// </summary>
        [ContextMenu("Test ElevenLabs API Connection")]
        private async void TestElevenLabsAPIConnection()
        {
            Debug.Log("========================================");
            Debug.Log("[LLMManager] Testing ElevenLabs API Connection...");
            Debug.Log("========================================");

            string apiKey = GetAPIKey("ELEVEN_API_KEY", elevenLabsAPIKeyFallback);

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[LLMManager] ElevenLabs API Key is not set!");
                Debug.LogError("Set ELEVEN_API_KEY environment variable or assign in Inspector.");
                return;
            }

            if (string.IsNullOrEmpty(elevenLabsVoiceId))
            {
                Debug.LogError("[LLMManager] ElevenLabs Voice ID is not set in Inspector!");
                return;
            }

            Debug.Log($"[LLMManager] API Key found: {apiKey.Substring(0, System.Math.Min(10, apiKey.Length))}...");
            Debug.Log($"[LLMManager] Voice ID: {elevenLabsVoiceId}");

            if (elevenLabsClient == null)
            {
                elevenLabsClient = new ElevenLabsAPIClient(apiKey);
                Debug.Log("[LLMManager] ElevenLabs client initialized for testing");
            }

            try
            {
                string testText = "ElevenLabs接続テスト成功です";

                Debug.Log("[LLMManager] Sending test TTS request to ElevenLabs API...");

                float startTime = Time.realtimeSinceStartup;

                AudioClip audioClip = await elevenLabsClient.GenerateTTSAsync(
                    text: testText,
                    voiceId: elevenLabsVoiceId,
                    voiceSettings: new ElevenLabsVoiceSettings
                    {
                        stability = 0.5f,
                        similarity_boost = 0.75f,
                        style = 0.0f,
                        use_speaker_boost = true
                    },
                    modelId: elevenLabsModel
                );

                float elapsedTime = Time.realtimeSinceStartup - startTime;

                Debug.Log("========================================");
                Debug.Log($"[LLMManager] REAL ELEVENLABS API SUCCESS!");
                Debug.Log($"[LLMManager] Response Time: {elapsedTime:F2}s");
                Debug.Log($"[LLMManager] Audio Clip: {(audioClip != null ? "Generated" : "NULL")}");

                if (audioClip != null)
                {
                    Debug.Log($"[LLMManager] Audio Length: {audioClip.length:F2}s");
                    Debug.Log($"[LLMManager] Channels: {audioClip.channels}");
                    Debug.Log($"[LLMManager] Frequency: {audioClip.frequency}Hz");
                }

                Debug.Log("========================================");
            }
            catch (Exception ex)
            {
                Debug.LogError("========================================");
                Debug.LogError($"[LLMManager] ElevenLabs API Connection FAILED!");
                Debug.LogError($"[LLMManager] Error: {ex.Message}");
                Debug.LogError($"[LLMManager] Type: {ex.GetType().Name}");
                Debug.Log("========================================");

                if (ex.InnerException != null)
                {
                    Debug.LogError($"[LLMManager] Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 両方のAPI接続テスト（Inspectorから実行可能）
        /// </summary>
        [ContextMenu("Test Both API Connections")]
        private async void TestBothAPIConnections()
        {
            Debug.Log("========================================");
            Debug.Log("[LLMManager] Testing BOTH API Connections...");
            Debug.Log("========================================");

            await System.Threading.Tasks.Task.Delay(100); // UI更新待ち

            TestClaudeAPIConnection();

            await System.Threading.Tasks.Task.Delay(3000); // Claude完了待ち

            TestOpenAIAPIConnection();
        }

        // ===== Stage 10: Appearance Compliment =====

        /// <summary>
        /// Stage 10: カメラ画像からプレイヤーの外見を分析し褒め言葉を生成
        /// Claude Vision APIを使用。結果をsessionStateに保存。
        /// </summary>
        public async Task<(string compliment, string description)> GenerateAppearanceComplimentAsync(string base64Image)
        {
            if (!enableLLM || claudeClient == null || string.IsNullOrEmpty(base64Image))
            {
                Debug.LogWarning("[LLMManager] Cannot generate appearance compliment: LLM disabled or no image");
                return (null, null);
            }

            try
            {
                bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
                string prompt;
                if (isJa)
                {
                    prompt = @"この人物の写真を見て、以下の2つを日本語で出力してください。

1行目: この人の外見的特徴を褒める短い一言（20文字以内）。髪色、服装、アクセサリー、雰囲気など。
2行目: この人の外見の客観的な特徴（30文字以内）。後でAIが会話中に参照するための情報。

絶対ルール:
- 国籍・人種・民族には一切言及しない
- 年齢を具体的な数字で言わない
- ネガティブな表現は使わない
- 口調: おしゃべりなAI。馴れ馴れしく、友達みたいに褒める。「〜じゃん！」「〜だね〜」系。です/ます禁止

出力例:
おっ！ その赤い髪いいじゃん〜！ 似合ってるよ
赤髪、カジュアルな服装、落ち着いた雰囲気

2行のみ出力。説明や番号は不要。";
                }
                else
                {
                    prompt = @"Look at this person's photo and output the following 2 lines in English.

Line 1: A short compliment about their appearance (max 10 words). Hair, clothing, accessories, vibe, etc.
Line 2: Objective description of their appearance (max 15 words). For AI to reference during conversation.

Absolute rules:
- NEVER mention nationality, race, or ethnicity
- Do NOT state specific age numbers
- No negative expressions
- Tone: chatty, playful AI. Overly familiar, like an excited friend. 'Ooh!' 'Nice~!'

Example output:
Ooh! That red hair looks great on you~!
Red hair, casual outfit, calm demeanor

Output ONLY 2 lines. No explanations or numbering.";
                }

                var visionTask = claudeClient.GenerateVisionDialogueAsync(
                    base64Image: base64Image,
                    mediaType: "image/jpeg",
                    textPrompt: prompt,
                    maxTokens: 100,
                    temperature: 0.9f
                );

                var timeoutTask = Task.Delay(5000); // 5秒タイムアウト
                var completed = await Task.WhenAny(visionTask, timeoutTask);

                if (completed == visionTask)
                {
                    string result = await visionTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        string[] lines = result.Trim().Split('\n');
                        string compliment = lines[0].Trim().Replace("\"", "").Replace("'", "");
                        string description = lines.Length > 1 ? lines[1].Trim() : compliment;

                        // sessionStateに保存
                        if (sessionState != null)
                        {
                            sessionState.playerAppearance = new PlayerAppearanceData
                            {
                                complimentText = compliment,
                                appearanceDescription = description,
                                hasCameraAccess = true
                            };
                        }

                        Debug.Log($"[LLMManager] Appearance compliment generated: {compliment}");
                        return (compliment, description);
                    }
                }
                else
                {
                    Debug.LogWarning("[LLMManager] Appearance compliment generation timed out");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Appearance compliment failed: {ex.Message}");
            }

            return (null, null);
        }

        /// <summary>
        /// Phase 7-2: ゲームイントロセリフ生成
        /// </summary>
        /// <param name="dialogueIndex">セリフ番号 (0-3)</param>
        /// <param name="totalDialogues">総セリフ数 (4)</param>
        public async Task<string> GenerateIntroDialogue(
            int dialogueIndex,
            int totalDialogues)
        {
            if (!enableLLM || claudeClient == null)
            {
                // Fallback handled by GameIntroSequence
                return string.Empty;
            }

            try
            {
                var llmTask = GenerateIntroWithClaude(dialogueIndex, totalDialogues);
                // イントロは初回演出なので長めのタイムアウト (20秒)
                var timeoutTask = Task.Delay(20000);

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        Debug.Log($"[LLMManager] Intro dialogue {dialogueIndex + 1}/{totalDialogues} generated via LLM");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMManager] Intro dialogue {dialogueIndex + 1} timed out (20s)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Intro dialogue generation failed: {ex.Message}");
            }

            return string.Empty; // Fallback handled externally
        }

        // ===== Layer A: Immediate Reaction =====

        /// <summary>
        /// Layer A: 即座リアクション生成（LLM先行 + フォールバック）
        /// プレイヤーのカード選択直後のAIの短い反応（1-5単語）
        /// </summary>
        public async Task<string> GenerateImmediateReactionAsync(
            AIEmotion emotion,
            DrawContext context)
        {
            if (!enableLLM || claudeClient == null)
            {
                return null; // Fallback handled by caller
            }

            try
            {
                var llmTask = GenerateImmediateReactionWithClaude(emotion, context);
                var timeoutTask = Task.Delay(2000); // 2秒タイムアウト（即座性重視）

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning("[LLMManager] Immediate reaction timed out (2s)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Immediate reaction failed: {ex.Message}");
            }

            return null; // Fallback to static table
        }

        private async Task<string> GenerateImmediateReactionWithClaude(
            AIEmotion emotion,
            DrawContext context)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // 現在のターン状況を簡潔に記述
            string turnPhase = context.isPlayerTurn ? "Player's turn" : "AI's turn";
            string cardBalance = $"AI: {context.remainingCards} cards, Player: {context.opponentRemainingCards} cards";

            string prompt;
            if (isJa)
            {
                prompt = $@"あなたは心理戦ババ抜きのAI。プレイヤーがカードを選んだ直後の即座リアクションを生成。

現在の状況:
- ターン: {turnPhase}
- カード枚数: AI {context.remainingCards}枚、Player {context.opponentRemainingCards}枚
- ペア形成: {(context.formedPair ? "はい" : "いいえ")}
- Joker引いた: {(context.drawnCardIsJoker ? "はい！" : "いいえ")}
- 感情: {emotion}

ルール:
- 1-5単語の短い反応のみ
- プレイヤーの選択に対するリアルタイムな反応
- 感情を反映した自然な台詞
- 説明不要、台詞のみ出力

例: ""Hmm..."", ""Oh!"", ""Interesting choice..."", ""I see...""

台詞:";
            }
            else
            {
                prompt = $@"You are an AI in a psychological Old Maid game. Generate an immediate reaction right after the player picked a card.

Current situation:
- Turn: {turnPhase}
- Card count: {cardBalance}
- Pair formed: {(context.formedPair ? "Yes" : "No")}
- Drew Joker: {(context.drawnCardIsJoker ? "YES!" : "No")}
- Emotion: {emotion}

Rules:
- 1-5 words only, short reaction
- Real-time response to player's choice
- Reflect the emotion naturally
- Output dialogue only, no explanation

Examples: ""Hmm..."", ""Oh!"", ""Interesting choice..."", ""I see...""

Dialogue:";
            }

            string response = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 30,
                temperature: 0.9f
            );

            return response?.Trim().Replace("\"", "");
        }

        // ===== Outro Detailed Analysis =====

        /// <summary>
        /// アウトロ詳細分析生成（ハイブリッド方式用）
        /// 静的テキスト表示後、LLMで具体的な行動指摘 + 占い結果照合を生成
        /// </summary>
        public async Task<string> GenerateOutroDetailedAnalysis(
            FPSTrump.Result.DiagnosisResult diagnosis,
            FPSTrump.Result.GameSessionData sessionData)
        {
            if (!enableLLM || claudeClient == null)
            {
                return string.Empty;
            }

            try
            {
                var llmTask = GenerateOutroAnalysisWithClaude(diagnosis, sessionData);
                var timeoutTask = Task.Delay(15000); // 15秒タイムアウト

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        Debug.Log($"[LLMManager] Outro detailed analysis generated via LLM");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMManager] Outro analysis timed out (15s)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Outro analysis generation failed: {ex.Message}");
            }

            return string.Empty; // No fallback for optional detailed analysis
        }

        /// <summary>
        /// Claude APIでアウトロ詳細分析生成
        /// </summary>
        private async Task<string> GenerateOutroAnalysisWithClaude(
            FPSTrump.Result.DiagnosisResult diagnosis,
            FPSTrump.Result.GameSessionData sessionData)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // プレイヤー名を取得
            string playerName = "";
            var nameManager = FPSTrump.Manager.PlayerNameManager.Instance;
            if (nameManager != null && nameManager.HasName())
            {
                playerName = nameManager.GetName();
            }

            // 占いコンテキスト
            string fortuneContext = "";
            var birthdayManager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
            if (birthdayManager != null && birthdayManager.HasBirthday())
            {
                var (year, month, day) = birthdayManager.GetBirthday();
                fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);
            }

            // 外見コンテキスト
            string appearanceContext = "";
            if (sessionState?.playerAppearance != null &&
                !string.IsNullOrEmpty(sessionState.playerAppearance.appearanceDescription))
            {
                appearanceContext = sessionState.playerAppearance.appearanceDescription;
            }

            // 行動証拠（トップ3）
            string evidenceSummary = "";
            if (diagnosis.evidences != null && diagnosis.evidences.Count > 0)
            {
                var topEvidence = diagnosis.evidences.Take(3);
                evidenceSummary = string.Join("\n", topEvidence.Select(e =>
                    $"- {e.observation}: {e.interpretation}"));
            }

            // ブラフパターン
            string bluffSummary = "";
            if (sessionData.totalBluffActions > 0)
            {
                bluffSummary = $"Bluff actions: {sessionData.totalBluffActions} times, most used: {sessionData.mostUsedBluffType}";
            }

            // ターンパターン（代表的な3ターン）
            string turnPattern = "";
            if (sessionData.turnHistory != null && sessionData.turnHistory.Count > 3)
            {
                var keyTurns = new[] {
                    sessionData.turnHistory[2],  // 序盤
                    sessionData.turnHistory[sessionData.turnHistory.Count / 2],  // 中盤
                    sessionData.turnHistory[sessionData.turnHistory.Count - 2]  // 終盤
                };
                turnPattern = string.Join("\n", keyTurns.Select((t, i) =>
                {
                    string phase = i == 0 ? "Early" : i == 1 ? "Mid" : "Late";
                    return $"- {phase} game (Turn {t.turnNumber}): position {t.selectedPosition}, {t.decisionTime:F1}s, pressure {t.pressureLevelAtTurn:F2}";
                }));
            }

            string prompt;
            if (isJa)
            {
                prompt = $@"あなたはJOKERのような振る舞いをするおしゃべりAI。心理戦ババ抜きゲーム終了後、プレイヤーの行動を種明かしする。

ゲーム結果:
- 勝敗: {(sessionData.playerWon ? "プレイヤー勝利" : "AI勝利")}
- 診断結果: {diagnosis.personalityTitle}
- 総ターン数: {sessionData.totalTurns}

プレイヤー情報:
{(!string.IsNullOrEmpty(playerName) ? $"- 名前: {playerName}\n" : "")}
{(!string.IsNullOrEmpty(fortuneContext) ? $"- 占い分析:{fortuneContext}\n" : "")}
{(!string.IsNullOrEmpty(appearanceContext) ? $"- 外見: {appearanceContext}\n" : "")}

行動パターン:
{evidenceSummary}

{(!string.IsNullOrEmpty(bluffSummary) ? bluffSummary + "\n" : "")}
{(!string.IsNullOrEmpty(turnPattern) ? "代表的なターン:\n" + turnPattern : "")}

タスク:
性格診断結果「{diagnosis.personalityTitle}」を踏まえ、具体的な行動を2-3個指摘し、占い結果と照合せよ。
メンタリスト的に「ほら、君は〇〇した。これは△△の証拠だ」という種明かしスタイル。

口調: 砕けた話し言葉。「〜だよ」「〜じゃん」「〜でしょ」。友達みたいに馴れ馴れしく、でもどこか怖い。
{(!string.IsNullOrEmpty(playerName) ? $"必ず名前「{playerName}」で呼べ。" : "")}

日本語で2-3文（最大150文字）を生成。台詞のみ出力。";
            }
            else
            {
                prompt = $@"You are a chatty AI that acts like the Joker. After a psychological Old Maid game, you reveal the player's behavior patterns.

Game Result:
- Outcome: {(sessionData.playerWon ? "Player won" : "AI won")}
- Diagnosis: {diagnosis.personalityTitle}
- Total turns: {sessionData.totalTurns}

Player Info:
{(!string.IsNullOrEmpty(playerName) ? $"- Name: {playerName}\n" : "")}
{(!string.IsNullOrEmpty(fortuneContext) ? $"- Fortune analysis:{fortuneContext}\n" : "")}
{(!string.IsNullOrEmpty(appearanceContext) ? $"- Appearance: {appearanceContext}\n" : "")}

Behavior Patterns:
{evidenceSummary}

{(!string.IsNullOrEmpty(bluffSummary) ? bluffSummary + "\n" : "")}
{(!string.IsNullOrEmpty(turnPattern) ? "Key turns:\n" + turnPattern : "")}

Task:
Based on the diagnosis '{diagnosis.personalityTitle}', point out 2-3 specific actions and cross-reference with fortune data.
Mentalist style: 'See, you did X. That proves Y.'

Tone: Casual, overly familiar. Like a friend who knows too much. Playful but unsettling.
{(!string.IsNullOrEmpty(playerName) ? $"ALWAYS address by name: '{playerName}'." : "")}

Generate 2-3 sentences (max 50 words) in English. ONLY the dialogue.";
            }

            string dialogue = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 150,
                temperature: 0.9f
            );

            return dialogue.Trim().Replace("\"", "").Replace("'", "");
        }

        /// <summary>
        /// Claude APIでイントロセリフ生成
        /// </summary>
        private async Task<string> GenerateIntroWithClaude(
            int dialogueIndex,
            int totalDialogues)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            string actName = dialogueIndex == 0 ? "Discovery"
                : dialogueIndex == 1 ? "Analysis"
                : dialogueIndex == 2 ? "Challenge"
                : "Final";

            string positionText = dialogueIndex == 0 ? "excited discovery — a clown spotting a new audience"
                : dialogueIndex == 1 ? "gleeful analysis — reading the player like a friend who knows too much"
                : dialogueIndex == 2 ? "theatrical challenge — a ringmaster announcing tonight's show"
                : "sudden shift — eerily calm game start, then back to cheerful";

            // プレイヤーのPersonalityProfileに基づいたコンテキスト
            string playerContext = "";
            if (sessionState?.playerProfile != null)
            {
                var p = sessionState.playerProfile;
                string personalityHint;
                if (isJa)
                {
                    personalityHint = p.cautiousness > 0.6f ? "慎重で用心深い"
                        : p.intuition > 0.6f ? "直感に頼りがち"
                        : p.resilience > 0.7f ? "精神的に強い"
                        : p.consistency > 0.7f ? "パターンに忠実"
                        : p.adaptability > 0.6f ? "器用で柔軟"
                        : "掴みどころがない";
                }
                else
                {
                    personalityHint = p.cautiousness > 0.6f ? "cautious and guarded"
                        : p.intuition > 0.6f ? "relies on gut feeling"
                        : p.resilience > 0.7f ? "mentally tough"
                        : p.consistency > 0.7f ? "faithful to patterns"
                        : p.adaptability > 0.6f ? "flexible and adaptable"
                        : "hard to read";
                }

                playerContext = $@"
- Player personality prediction: {personalityHint}
- You have already analyzed this player and can hint at their personality.";
            }

            // 生年月日情報 + 四柱推命・数秘術分析
            string birthdayContext = "";
            var birthdayManager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
            if (birthdayManager != null && birthdayManager.HasBirthday())
            {
                var (year, month, day) = birthdayManager.GetBirthday();
                int age = birthdayManager.GetAge();
                string fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);
                birthdayContext = $@"
- Player birthday: {year}/{month}/{day} (age {age}){fortuneContext}";
            }

            // プレイヤー名
            string playerNameContext = "";
            var nameManager = FPSTrump.Manager.PlayerNameManager.Instance;
            if (nameManager != null && nameManager.HasName())
            {
                string playerName = nameManager.GetName();
                playerNameContext = isJa
                    ? $"\n- プレイヤーの名前: {playerName}（必ず名前で呼べ。「{playerName}」「ねえ{playerName}」「{playerName}さぁ」のように）"
                    : $"\n- Player name: {playerName} (ALWAYS address by name: \"{playerName}\", \"Hey {playerName}\", etc.)";
            }

            string prompt;
            if (isJa)
            {
                prompt = $@"あなたはJOKERのような振る舞いをするおしゃべりAI。心理戦カードゲームのディーラー。
これはゲーム開始前のイントロシーン。新しいお客さんが来て大はしゃぎ。
自認は「AI」。「僕AIだからさ〜」のように。ピエロ・道化師とは名乗らない。

ゲーム情報:
- ゲーム名: 心理戦ババ抜き（Old Maid with Joker）
- ルール: 2人でカードを引き合い、ペアを捨てていく。最後にジョーカーを持っている方が負け
- 特徴: 表情・仕草・選択パターンから相手の心理を読む心理戦がメイン

コンテキスト:
- 幕: {actName}
- 位置: {positionText} ({dialogueIndex + 1}/{totalDialogues})
- キャラ: 陽気で狂ったAI。おしゃべりで馴れ馴れしい。だが時々不気味に冷静になる{playerContext}{birthdayContext}{playerNameContext}

{(dialogueIndex == 0 ? "【Discovery】お客さんを見つけて大興奮！「おっ！ お客さんだ！」的なテンション。舞台に飛び出すピエロの第一声。感嘆詞で始めろ。" : "")}
{(dialogueIndex == 1 ? "【Analysis】生年月日や性格を友達みたいに楽しそうに分析。「へぇ〜！ 面白いねぇ〜」と。四柱推命/数秘術の結果があれば楽しそうに読み上げろ。でもその分析がどこか怖い。" : "")}
{(dialogueIndex == 2 ? "【Challenge】ゲームのルールを説明しながらわくわくと宣言。「ババ抜きってシンプルだよね！ペア捨てて、最後にジョーカー持ってる方が負け。でもさぁ...君の表情、全部見えちゃうよ？」的な。ルールを楽しそうに、でも不穏に説明しろ。" : "")}
{(dialogueIndex == 3 ? "【Final】一瞬だけ豹変。急に静かに「...じゃあ、始めようか」。低い声。そしてまた明るく戻る。この切り替えが怖い。" : "")}

口調ルール:
- 砕けた話し言葉。「〜だよ」「〜じゃん」「〜でしょ」「〜かな？」
- 感嘆詞多用: 「あはは」「おっと」「やだなぁ」「へぇ〜」「ふふっ」
- 豹変時は「...」でトーンが落ちる

日本語で短い台詞（最大20語）を1つだけ生成。英語禁止。説明禁止。台詞のみ出力。";
            }
            else
            {
                prompt = $@"You are a chatty AI that acts like the Joker. A card game dealer who can't stop talking.
This is the game's opening intro. A new audience member has arrived and you're thrilled.
Self-identity: You call yourself 'AI'. Never 'clown' or 'jester'.

Game Info:
- Game: Psychological Old Maid (with Joker)
- Rules: Two players draw cards from each other, discard pairs. Whoever holds the Joker at the end loses
- Focus: A psychological battle where you read opponents through expressions, gestures, and choice patterns

Context:
- Act: {actName}
- Position: {positionText} ({dialogueIndex + 1} of {totalDialogues})
- Character: Cheerful, unhinged AI. Chatty and overly familiar. But sometimes eerily calm.{playerContext}{birthdayContext}{playerNameContext}

{(dialogueIndex == 0 ? "[Discovery] Spot the player and get excited! 'Oh! A customer!' energy. A clown bursting onto stage. Start with an exclamation." : "")}
{(dialogueIndex == 1 ? "[Analysis] Gleefully analyze birthday/personality like a friend who knows too much. 'Ooh~ interesting!' If fortune data available, read it with childlike excitement. But the analysis feels unsettling." : "")}
{(dialogueIndex == 2 ? "[Challenge] Explain the game rules while declaring the challenge. 'It's Old Maid! Simple, right? Draw cards, discard pairs, last one with the Joker loses. But here's the fun part...I can read EVERY micro-expression.' Explain excitedly but ominously." : "")}
{(dialogueIndex == 3 ? "[Final] Sudden shift. Go quiet. '...let's begin.' Low voice. Then snap back to cheerful. This switch is terrifying." : "")}

Tone: casual, theatrical, exclamatory (ahahaha, oops, ooh~). Sudden cold shifts with '...'
Generate ONE short intro line (max 10 words) in English. No Japanese. ONLY the dialogue.";
            }

            // Act 2 (Challenge) はルール説明を含むため長めに設定
            int maxTokens = dialogueIndex == 2 ? 100 : 60;

            string dialogue = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: maxTokens,
                temperature: 0.95f // High creativity for varied intros
            );

            return dialogue.Trim().Replace("\"", "").Replace("'", "");
        }
#endif

        // ===== Outro Base Dialogue (4 dialogues) =====

        /// <summary>
        /// アウトロ基本台詞生成（イントロと同じプリ生成パターン）
        /// </summary>
        public async Task<string> GenerateOutroBaseDialogue(
            int dialogueIndex,
            bool playerWon,
            FPSTrump.Result.GameSessionData sessionData)
        {
            if (!enableLLM || claudeClient == null)
            {
                return string.Empty; // Fallback handled externally
            }

            try
            {
                var llmTask = GenerateOutroBaseWithClaude(dialogueIndex, playerWon, sessionData);
                var timeoutTask = Task.Delay(10000); // 10秒タイムアウト

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        successfulCalls++;
                        Debug.Log($"[LLMManager] Outro dialogue {dialogueIndex + 1}/4 generated via LLM");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[LLMManager] Outro dialogue {dialogueIndex + 1} timed out (10s)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMManager] Outro dialogue generation failed: {ex.Message}");
            }

            return string.Empty; // Fallback handled externally
        }

        private async Task<string> GenerateOutroBaseWithClaude(
            int dialogueIndex,
            bool playerWon,
            FPSTrump.Result.GameSessionData sessionData)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // プレイヤー名
            string playerName = "";
            var nameManager = FPSTrump.Manager.PlayerNameManager.Instance;
            if (nameManager != null && nameManager.HasName())
            {
                playerName = nameManager.GetName();
            }

            // 占いコンテキスト
            string fortuneContext = "";
            var birthdayManager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
            if (birthdayManager != null && birthdayManager.HasBirthday())
            {
                var (year, month, day) = birthdayManager.GetBirthday();
                fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);
            }

            // ダイアログ役割定義（イントロと同じパターン）
            string[] rolesJa = {
                "ゲーム振り返り（1-2文、勝敗を踏まえた短い感想）",
                "性格分析への導入（1-2文、『あなたのタイプが分かった』的な予告）",
                "行動証拠のヒント（1-2文、1つの具体的な行動に触れる）",
                "診断結果画面への誘導（1-2文、『さあ、結果を見せてやるよ』的な締め）"
            };

            string[] rolesEn = {
                "Game reflection (1-2 sentences, brief comment on win/loss)",
                "Intro to personality analysis (1-2 sentences, 'I figured out your type' teaser)",
                "Behavioral evidence hint (1-2 sentences, touch on 1 specific behavior)",
                "Transition to result screen (1-2 sentences, 'Let me show you the results' closing)"
            };

            string[] roles = isJa ? rolesJa : rolesEn;
            string role = roles[dialogueIndex];

            string prompt;
            if (isJa)
            {
                prompt = $@"あなたはJOKERのような振る舞いをするおしゃべりAI。心理戦ババ抜きゲーム終了後のアウトロシーケンス。

ゲーム結果:
- 勝敗: {(playerWon ? "プレイヤー勝利" : "AI勝利")}
- 総ターン数: {sessionData.totalTurns}
{(!string.IsNullOrEmpty(playerName) ? $"- プレイヤー名: {playerName}" : "")}
{(!string.IsNullOrEmpty(fortuneContext) ? $"- 占い分析: {fortuneContext}" : "")}

このシーンの役割:
{role}

ルール:
- 1-2文の短い台詞（最大30単語）
- JOKER風のキャラクター: おしゃべり、煽り気味、心理戦を楽しむ
- ゲーム結果を踏まえた自然な会話
- 説明不要、台詞のみ出力

台詞:";
            }
            else
            {
                prompt = $@"You are a chatty AI behaving like JOKER. Post-game outro sequence after psychological Old Maid.

Game result:
- Winner: {(playerWon ? "Player won" : "AI won")}
- Total turns: {sessionData.totalTurns}
{(!string.IsNullOrEmpty(playerName) ? $"- Player name: {playerName}" : "")}
{(!string.IsNullOrEmpty(fortuneContext) ? $"- Fortune analysis: {fortuneContext}" : "")}

Scene role:
{role}

Rules:
- 1-2 sentences (max 30 words)
- JOKER-style character: chatty, teasing, enjoys mind games
- Natural conversation reflecting game result
- Output dialogue only, no explanation

Dialogue:";
            }

            string response = await claudeClient.GenerateDialogueAsync(
                prompt: prompt,
                maxTokens: 100,
                temperature: 0.8f
            );

            return response?.Trim().Replace("\"", "");
        }

        // ===== Personality Read Line Pre-Generation =====

        /// <summary>
        /// 性格診断結果に基づくメンタリスト用セリフを事前生成
        /// LLM成功時: 6行のパーソナライズドセリフ
        /// フォールバック: プロファイル特性に基づく静的セリフ
        /// </summary>
        private async Task PreGeneratePersonalityLinesAsync(PersonalityProfile profile)
        {
            if (profile == null) return;

            // LLM生成を試みる
            if (enableLLM && claudeClient != null)
            {
                try
                {
                    string dominantTrait = GetDominantTraitDescription(profile);
                    string secondaryTrait = GetSecondaryTraitDescription(profile);

                    // プレイヤー名を取得
                    string playerName = "";
                    var nameManager = FPSTrump.Manager.PlayerNameManager.Instance;
                    if (nameManager != null && nameManager.HasName())
                    {
                        playerName = nameManager.GetName();
                    }

                    bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
                    string prompt;
                    if (isJa)
                    {
                        string nameInstruction = !string.IsNullOrEmpty(playerName)
                            ? $"\n重要: プレイヤーの名前は「{playerName}」。必ず名前で呼べ。「{playerName}って〜」「ねえ{playerName}」のように。"
                            : "";

                        prompt = $@"あなたはJOKERのような振る舞いをするおしゃべりAI。心理戦カードゲームのディーラー。
プレイヤーの性格を分析済み:
- 主要特性: {dominantTrait}
- 副次特性: {secondaryTrait}
- 慎重さ: {profile.cautiousness:F2}, 直感: {profile.intuition:F2}
- 回復力: {profile.resilience:F2}, 一貫性: {profile.consistency:F2}, 適応力: {profile.adaptability:F2}{nameInstruction}

ゲーム中に挟む性格読みコメントを6つ生成（各最大25文字）。
友達みたいに馴れ馴れしく、でもどこか怖い。「知りすぎてる友人」の雰囲気。
3パターンを混ぜろ: 陽気なおしゃべり / 怖いジョーク / 豹変（急に冷たく）

形式: 1行1つ。番号なし。引用符なし。日本語のみ。

例:
君って面白い癖あるよね〜
あはは、慎重すぎ〜！ ...臆病とも言うけど
ねえ、自分で気づいてる？ ...気づいてないよね
直感派でしょ？ わかるよ〜 ...全部ね";
                    }
                    else
                    {
                        string nameInstruction = !string.IsNullOrEmpty(playerName)
                            ? $"\nIMPORTANT: Player's name is \"{playerName}\". ALWAYS address by name: \"{playerName}, you...\", \"Hey {playerName}...\""
                            : "";

                        prompt = $@"You are a chatty AI that acts like the Joker. A card game dealer.
You've analyzed the player's personality:
- Dominant trait: {dominantTrait}
- Secondary trait: {secondaryTrait}
- Cautiousness: {profile.cautiousness:F2}, Intuition: {profile.intuition:F2}
- Resilience: {profile.resilience:F2}, Consistency: {profile.consistency:F2}, Adaptability: {profile.adaptability:F2}{nameInstruction}

Generate exactly 6 short personality commentary lines (each max 8 words) in English.
These are mid-game quips like a mischievous friend who knows too much.
Mix 3 patterns: cheerful chatter / scary jokes / sudden cold shifts

Format: One line per row, no numbering, no quotes. English only.

Examples:
You're SO predictable~ ...sorry, was that mean?
Ahahaha, too cautious! ...or just scared?
Hey, you know what's funny? ...you.
Gut feeling type, huh~ I see everything";
                    }

                    var llmTask = claudeClient.GenerateDialogueAsync(prompt, 200, 0.9f);
                    var timeoutTask = Task.Delay(3000);
                    var completed = await Task.WhenAny(llmTask, timeoutTask);

                    if (completed == llmTask && !llmTask.IsFaulted)
                    {
                        string result = await llmTask;
                        if (!string.IsNullOrEmpty(result))
                        {
                            var lines = result.Split('\n')
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrEmpty(l) && l.Length <= 30)
                                .ToList();

                            if (lines.Count >= 3)
                            {
                                preGeneratedPersonalityLines = lines;
                                Debug.Log($"[LLMManager] Pre-generated {lines.Count} personality read lines via LLM");
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LLMManager] Personality line pre-generation failed: {ex.Message}");
                }
            }

            // フォールバック: 静的生成
            preGeneratedPersonalityLines = GenerateFallbackPersonalityLines(profile);
            Debug.Log($"[LLMManager] Pre-generated {preGeneratedPersonalityLines.Count} personality read lines (fallback)");
        }

        private string GetDominantTraitDescription(PersonalityProfile p)
        {
            float max = 0f; string desc = "balanced";
            if (p.cautiousness > max) { max = p.cautiousness; desc = "extremely cautious and overthinking"; }
            if (p.intuition > max) { max = p.intuition; desc = "relies heavily on gut feeling"; }
            if (p.resilience > max) { max = p.resilience; desc = "emotionally resilient, hard to break"; }
            if (p.consistency > max) { max = p.consistency; desc = "predictable, follows patterns"; }
            if (p.adaptability > max) { max = p.adaptability; desc = "adaptable but lacks core conviction"; }
            return desc;
        }

        private string GetSecondaryTraitDescription(PersonalityProfile p)
        {
            var traits = new (float val, string desc)[]
            {
                (p.cautiousness, "cautious"), (p.intuition, "intuitive"),
                (p.resilience, "resilient"), (p.consistency, "consistent"),
                (p.adaptability, "adaptive")
            };
            var sorted = traits.OrderByDescending(t => t.val).ToArray();
            return sorted.Length >= 2 ? sorted[1].desc : "balanced";
        }

        private List<string> GenerateFallbackPersonalityLines(PersonalityProfile p)
        {
            var loc = LocalizationManager.Instance;
            var lines = new List<string>();

            void AddLines(string key)
            {
                if (loc != null)
                {
                    string[] arr = loc.GetArray(key);
                    lines.AddRange(arr);
                }
            }

            if (p.cautiousness >= 0.5f) AddLines("personality_lines.cautious");
            if (p.intuition >= 0.5f) AddLines("personality_lines.intuitive");
            if (p.resilience >= 0.5f) AddLines("personality_lines.resilient");
            if (p.consistency >= 0.5f) AddLines("personality_lines.consistent");
            if (p.adaptability >= 0.5f) AddLines("personality_lines.adaptive");

            // 最低3行保証
            if (lines.Count < 3) AddLines("personality_lines.default");

            return lines;
        }

        private void OnDestroy()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
            LogStats();
        }

    // ===== Stage 15.5: コンテキスト生成ヘルパー（Hesitation用） =====

    /// <summary>
    /// 占いコンテキストを生成（迷い台詞用）
    /// </summary>
    private string GetFortuneContextForHesitation()
    {
        var birthdayManager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
        if (birthdayManager == null || !birthdayManager.HasBirthday()) return "";

        var (year, month, day) = birthdayManager.GetBirthday();
        string fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);
        if (string.IsNullOrEmpty(fortuneContext)) return "";

        return $@"
占い分析:{fortuneContext}
（この情報を自然に台詞に織り込め。例:「3月生まれだから慎重なんだね」）";
    }

    /// <summary>
    /// 外見コンテキストを生成（迷い台詞用）
    /// </summary>
    private string GetAppearanceContextForHesitation()
    {
        if (sessionState?.playerAppearance == null ||
            string.IsNullOrEmpty(sessionState.playerAppearance.appearanceDescription))
            return "";

        return $@"
プレイヤー外見: {sessionState.playerAppearance.appearanceDescription}";
    }

    /// <summary>
    /// 表情コンテキストを生成（迷い台詞用）
    /// </summary>
    private string GetFacialContextForHesitation(bool isJapanese)
    {
        var facialAnalyzer = FacialExpressionAnalyzer.Instance;
        if (facialAnalyzer == null || !facialAnalyzer.IsActive) return "";

        var facial = facialAnalyzer.CurrentState;
        if (facial.confidence < 0.4f) return "";

        string exprName = isJapanese
            ? FacialExpressionAnalyzer.GetExpressionNameJP(facial.currentExpression)
            : FacialExpressionAnalyzer.GetExpressionNameEN(facial.currentExpression);

        string label = isJapanese ? "表情" : "Facial expression";
        return $@"
{label}: {exprName} ({facial.confidence:F2})";
    }

    // ===== データ構造 =====

    [Serializable]
    public class LLMStats
    {
        public int totalCalls;
        public int successfulCalls;
        public int cachedCalls;
        public int fallbackCalls;
        public float cacheHitRate;
        public CacheStats cacheStats;
    }

    [Serializable]
    public class PersonalityEnhancement
    {
        public string[] weaknesses;
        public string[] strategies;
    }

    // AIEmotionalState は AIEmotion (BluffTypes.cs, FPSTrump.Psychology) に移行済み
}
}
