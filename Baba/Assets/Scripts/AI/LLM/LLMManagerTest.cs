using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// LLMManager簡易テストスクリプト
    /// Unity Editor の Context Menu から各テストを実行可能
    /// </summary>
    public class LLMManagerTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LLMManager llmManager;

        [Header("Test Settings")]
        [SerializeField] private bool autoRunTestsOnStart = false;

        private void Start()
        {
            if (autoRunTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// すべてのテストを順次実行
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTestsMenu()
        {
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("=== Phase 3A LLM Manager Tests ===");

            // LLMManagerの初期化を待つ（Awake完了まで）
            yield return new WaitForSeconds(0.1f);

            if (llmManager == null)
            {
                Debug.LogError("=== Tests ABORTED: LLMManager not assigned ===");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            TestInitialization();

            yield return new WaitForSeconds(0.5f);
            TestFallbackSystem();

            yield return new WaitForSeconds(0.5f);
            TestCacheSystem();

            yield return new WaitForSeconds(0.5f);
            TestEmotionalStateManager();

            yield return new WaitForSeconds(0.5f);
            TestAIMemoryManager();

            Debug.Log("=== All Tests Complete ===");
        }

        /// <summary>
        /// Test 1: 初期化テスト
        /// </summary>
        [ContextMenu("Test 1: Initialization")]
        public void TestInitialization()
        {
            Debug.Log("[Test 1] Testing LLMManager initialization...");

            if (llmManager == null)
            {
                Debug.LogError("[Test 1] FAILED: LLMManager not assigned!");
                return;
            }

            // Awake()の実行を確認するため、フレームを待つ
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Test 1] Test should be run in Play mode for proper initialization");
            }

            // 統計情報を取得して初期化を確認
            var stats = llmManager.GetStats();
            Debug.Log($"[Test 1] LLM Stats: {stats.totalCalls} calls, Cache hit rate: {stats.cacheHitRate:P0}");
            Debug.Log($"[Test 1] Cache: {stats.cacheStats}");

            Debug.Log("[Test 1] PASSED: Initialization successful");
        }

        /// <summary>
        /// Test 2: フォールバックシステムテスト
        /// </summary>
        [ContextMenu("Test 2: Fallback System")]
        public void TestFallbackSystem()
        {
            Debug.Log("[Test 2] Testing Fallback System...");

            FallbackManager fallbackManager = new FallbackManager();

            // ルールベース生成器のテスト
            RuleBasedDialogueGenerator generator = new RuleBasedDialogueGenerator();

            BehaviorPattern testPattern = new BehaviorPattern
            {
                doubtLevel = 0.8f,
                tempo = TempoType.Slow,
                streakSamePosition = 3,
                hasPositionPreference = true,
                preferredPosition = 0,
                avgHoverTime = 3.5f
            };

            string dialogue = generator.Generate(
                DialogueCategoryType.Stop,
                testPattern,
                1.5f
            );

            Debug.Log($"[Test 2] Generated dialogue (Stop, high doubt): \"{dialogue}\"");

            if (string.IsNullOrEmpty(dialogue))
            {
                Debug.LogError("[Test 2] FAILED: Dialogue generation returned empty string");
                return;
            }

            Debug.Log("[Test 2] PASSED: Fallback system working");
        }

        /// <summary>
        /// Test 3: キャッシュシステムテスト
        /// </summary>
        [ContextMenu("Test 3: Cache System")]
        public void TestCacheSystem()
        {
            Debug.Log("[Test 3] Testing Cache System...");

            ResponseCache cache = new ResponseCache();

            string testKey = "test_category_behavior_pressure1";
            string testDialogue = "これはテストダイアログです";

            // キャッシュに保存
            cache.Set(testKey, testDialogue);
            Debug.Log($"[Test 3] Cached dialogue: \"{testDialogue}\"");

            // キャッシュから取得
            if (cache.TryGet(testKey, out string retrieved))
            {
                Debug.Log($"[Test 3] Retrieved from cache: \"{retrieved}\"");

                if (retrieved == testDialogue)
                {
                    Debug.Log("[Test 3] PASSED: Cache working correctly");
                }
                else
                {
                    Debug.LogError($"[Test 3] FAILED: Retrieved dialogue doesn't match. Expected: \"{testDialogue}\", Got: \"{retrieved}\"");
                }
            }
            else
            {
                Debug.LogError("[Test 3] FAILED: Cache retrieval failed");
            }

            // 統計情報
            var stats = cache.GetStats();
            Debug.Log($"[Test 3] Cache stats: {stats}");
        }

        /// <summary>
        /// Test 4: 感情状態マネージャーテスト
        /// </summary>
        [ContextMenu("Test 4: Emotional State Manager")]
        public void TestEmotionalStateManager()
        {
            Debug.Log("[Test 4] Testing Emotional State Manager...");

            EmotionalStateManager emotionalManager = new EmotionalStateManager();

            // 初期状態確認
            Debug.Log($"[Test 4] Initial state: {emotionalManager.CurrentState}");

            BehaviorPattern testBehavior = new BehaviorPattern
            {
                doubtLevel = 0.7f,
                tempo = TempoType.Slow,
                avgHoverTime = 3.0f
            };

            AIDecisionResult testDecision = new AIDecisionResult
            {
                selectedCardIndex = 1,
                confidence = 0.8f,
                strategy = "Aggressive"
            };

            // 感情状態更新テスト
            var newState = emotionalManager.UpdateEmotionalState(
                GameEvent.PlayerHesitating,
                testBehavior,
                testDecision
            );

            Debug.Log($"[Test 4] State after PlayerHesitating event: {newState}");

            // 感情コンテキスト取得
            string context = emotionalManager.GetEmotionalContext();
            Debug.Log($"[Test 4] Emotional context:\n{context}");

            Debug.Log("[Test 4] PASSED: Emotional State Manager working");
        }

        /// <summary>
        /// Test 5: AIメモリマネージャーテスト
        /// </summary>
        [ContextMenu("Test 5: AI Memory Manager")]
        public void TestAIMemoryManager()
        {
            Debug.Log("[Test 5] Testing AI Memory Manager...");

            AIMemoryManager memoryManager = new AIMemoryManager();

            // メモリ読み込み
            memoryManager.LoadSessionMemory();

            // 永続プロファイル確認
            var profile = memoryManager.GetPersistentProfile();
            if (profile != null)
            {
                Debug.Log($"[Test 5] Found persistent profile: Cautiousness={profile.cautiousness:F2}, Intuition={profile.intuition:F2}");
            }
            else
            {
                Debug.Log("[Test 5] No persistent profile found (expected on first run)");
            }

            // 最近のセッション確認
            var recentSessions = memoryManager.GetRecentSessions();
            Debug.Log($"[Test 5] Recent sessions count: {recentSessions.Count}");

            Debug.Log("[Test 5] PASSED: AI Memory Manager working");
        }

        /// <summary>
        /// Test 6: セッション初期化テスト（非同期）
        /// </summary>
        [ContextMenu("Test 6: Session Initialization")]
        public void TestSessionInitialization()
        {
            Debug.Log("[Test 6] Testing Session Initialization...");

            if (llmManager == null)
            {
                Debug.LogError("[Test 6] FAILED: LLMManager not assigned!");
                return;
            }

            StartCoroutine(TestSessionInitializationCoroutine());
        }

        private IEnumerator TestSessionInitializationCoroutine()
        {
            // テスト用プロファイル作成
            PersonalityProfile testProfile = new PersonalityProfile
            {
                cautiousness = 0.6f,
                intuition = 0.5f,
                resilience = 0.7f,
                curiosity = 0.4f,
                consistency = 0.5f,
                primaryDecisionStyle = DecisionStyle.Analytical,
                confidence = 0.6f,
                adaptability = 0.5f,
                stressType = StressType.Analytical,
                pressureTolerance = 0.6f,
                recoverySpeed = 0.5f
            };

            Debug.Log("[Test 6] Initializing session with test profile...");

            // 非同期初期化を実行（タスクを開始）
            Task initTask = llmManager.InitializeSession(testProfile);

            // タスクの完了を待機
            while (!initTask.IsCompleted)
            {
                yield return null;
            }

            if (initTask.IsFaulted)
            {
                Debug.LogError($"[Test 6] FAILED: Session initialization error: {initTask.Exception?.Message}");
            }
            else
            {
                Debug.Log("[Test 6] PASSED: Session initialization completed");

                // 統計情報確認
                llmManager.LogStats();
            }
        }

        /// <summary>
        /// すべてのメモリをクリア（リセット用）
        /// </summary>
        [ContextMenu("Clear All Memory (Reset)")]
        public void ClearAllMemory()
        {
            Debug.Log("[Reset] Clearing all memory...");

            AIMemoryManager memoryManager = new AIMemoryManager();
            memoryManager.ClearAllMemory();

            Debug.Log("[Reset] All memory cleared");
        }
    }
}
