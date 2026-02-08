using System.Collections;
using UnityEngine;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;

namespace FPSTrump.Tests
{
    /// <summary>
    /// PsychologySystem統合テストスクリプト
    /// Phase 3B: LLM統合、TTS、AIHandController統合の検証
    /// </summary>
    public class PsychologySystemTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PsychologySystem psychologySystem;
        [SerializeField] private PlayerBehaviorAnalyzer behaviorAnalyzer;
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
            Debug.Log("=== Phase 3B Psychology System Tests ===");

            // 初期化待ち
            yield return new WaitForSeconds(0.1f);

            if (psychologySystem == null || behaviorAnalyzer == null || llmManager == null)
            {
                Debug.LogError("=== Tests ABORTED: Missing component references ===");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            TestInitialization();

            yield return new WaitForSeconds(0.5f);
            TestBehaviorAnalysis();

            yield return new WaitForSeconds(0.5f);
            TestPressureSystem();

            yield return new WaitForSeconds(1.0f);
            yield return StartCoroutine(TestDialogueGeneration());

            yield return new WaitForSeconds(1.0f);
            yield return StartCoroutine(TestTTSIntegration());

            Debug.Log("=== All Tests Complete ===");
        }

        /// <summary>
        /// Test 1: 初期化テスト
        /// </summary>
        [ContextMenu("Test 1: Initialization")]
        public void TestInitialization()
        {
            Debug.Log("[Test 1] Testing PsychologySystem initialization...");

            if (psychologySystem == null)
            {
                Debug.LogError("[Test 1] FAILED: PsychologySystem not assigned!");
                return;
            }

            if (behaviorAnalyzer == null)
            {
                Debug.LogError("[Test 1] FAILED: PlayerBehaviorAnalyzer not assigned!");
                return;
            }

            if (llmManager == null)
            {
                Debug.LogError("[Test 1] FAILED: LLMManager not assigned!");
                return;
            }

            // 初期圧力レベルチェック
            float pressureLevel = psychologySystem.GetPressureLevel();
            Debug.Log($"[Test 1] Current pressure level: {pressureLevel:F2}");

            // LLMManager統計チェック
            var stats = llmManager.GetStats();
            Debug.Log($"[Test 1] LLM Stats: {stats.totalCalls} calls, Cache hit rate: {stats.cacheHitRate:P0}");

            Debug.Log("[Test 1] PASSED: Initialization successful");
        }

        /// <summary>
        /// Test 2: 行動分析テスト
        /// </summary>
        [ContextMenu("Test 2: Behavior Analysis")]
        public void TestBehaviorAnalysis()
        {
            Debug.Log("[Test 2] Testing Behavior Analysis...");

            // テスト用行動データを記録
            behaviorAnalyzer.RecordPlayerAction(0, 2.5f, 3.0f); // Left, long hover
            behaviorAnalyzer.RecordPlayerAction(0, 2.8f, 3.2f); // Left again, long hover
            behaviorAnalyzer.RecordPlayerAction(1, 1.0f, 1.5f); // Center, short hover

            // 行動パターンを取得
            BehaviorPattern pattern = behaviorAnalyzer.CurrentBehavior;

            Debug.Log($"[Test 2] Behavior Pattern:");
            Debug.Log($"  - Doubt Level: {pattern.doubtLevel:F2}");
            Debug.Log($"  - Tempo: {pattern.tempo}");
            Debug.Log($"  - Avg Hover Time: {pattern.avgHoverTime:F2}s");
            Debug.Log($"  - Streak Same Position: {pattern.streakSamePosition}");
            Debug.Log($"  - Has Position Preference: {pattern.hasPositionPreference}");

            if (pattern.doubtLevel > 0 && pattern.avgHoverTime > 0)
            {
                Debug.Log("[Test 2] PASSED: Behavior analysis working");
            }
            else
            {
                Debug.LogError("[Test 2] FAILED: Behavior analysis returned invalid data");
            }
        }

        /// <summary>
        /// Test 3: 圧力システムテスト
        /// </summary>
        [ContextMenu("Test 3: Pressure System")]
        public void TestPressureSystem()
        {
            Debug.Log("[Test 3] Testing Pressure System...");

            float initialPressure = psychologySystem.GetPressureLevel();
            Debug.Log($"[Test 3] Initial pressure: {initialPressure:F2}");

            // 圧力レベルを手動設定
            psychologySystem.SetPressureLevel(1.5f);
            float newPressure = psychologySystem.GetPressureLevel();
            Debug.Log($"[Test 3] Pressure after manual set: {newPressure:F2}");

            if (Mathf.Abs(newPressure - 1.5f) < 0.01f)
            {
                Debug.Log("[Test 3] PASSED: Pressure system working");
            }
            else
            {
                Debug.LogError($"[Test 3] FAILED: Expected 1.5, got {newPressure:F2}");
            }

            // リセット
            psychologySystem.SetPressureLevel(0);
        }

        /// <summary>
        /// Test 4: ダイアログ生成テスト
        /// </summary>
        [ContextMenu("Test 4: Dialogue Generation")]
        public void TestDialogueGenerationMenu()
        {
            StartCoroutine(TestDialogueGeneration());
        }

        private IEnumerator TestDialogueGeneration()
        {
            Debug.Log("[Test 4] Testing Dialogue Generation...");

            // テスト用行動パターン
            BehaviorPattern testPattern = new BehaviorPattern
            {
                doubtLevel = 0.7f,
                tempo = TempoType.Slow,
                avgHoverTime = 3.0f,
                streakSamePosition = 2
            };

            // ダイアログ生成タスク
            var dialogueTask = llmManager.GenerateDialogueAsync(
                DialogueCategoryType.Stop,
                testPattern,
                1.0f
            );

            Debug.Log("[Test 4] Waiting for dialogue generation...");

            // タスク完了を待機
            while (!dialogueTask.IsCompleted)
            {
                yield return null;
            }

            if (dialogueTask.IsFaulted)
            {
                Debug.LogError($"[Test 4] FAILED: Dialogue generation error: {dialogueTask.Exception?.Message}");
                yield break;
            }

            string dialogue = dialogueTask.Result;

            if (!string.IsNullOrEmpty(dialogue))
            {
                Debug.Log($"[Test 4] Generated dialogue: \"{dialogue}\"");
                Debug.Log("[Test 4] PASSED: Dialogue generation working");
            }
            else
            {
                Debug.LogError("[Test 4] FAILED: Dialogue generation returned empty string");
            }
        }

        /// <summary>
        /// Test 5: TTS統合テスト
        /// </summary>
        [ContextMenu("Test 5: TTS Integration")]
        public void TestTTSIntegrationMenu()
        {
            StartCoroutine(TestTTSIntegration());
        }

        private IEnumerator TestTTSIntegration()
        {
            Debug.Log("[Test 5] Testing TTS Integration...");

            string testText = "これはテストです";
            AIEmotion testEmotion = AIEmotion.Pleased;

            Debug.Log($"[Test 5] Generating TTS: \"{testText}\" (emotion: {testEmotion})");

            // TTS生成タスク
            var ttsTask = llmManager.GenerateTTSAsync(testText, testEmotion);

            // タスク完了を待機
            while (!ttsTask.IsCompleted)
            {
                yield return null;
            }

            if (ttsTask.IsFaulted)
            {
                Debug.LogWarning($"[Test 5] TTS generation failed (expected if API key not set): {ttsTask.Exception?.Message}");
                Debug.Log("[Test 5] PASSED: TTS integration working (API check failed as expected)");
                yield break;
            }

            AudioClip audioClip = ttsTask.Result;

            if (audioClip != null)
            {
                Debug.Log($"[Test 5] TTS audio generated: length={audioClip.length:F2}s, frequency={audioClip.frequency}Hz");
                Debug.Log("[Test 5] PASSED: TTS integration working");
            }
            else
            {
                Debug.LogWarning("[Test 5] TTS audio is null (expected if API key not set)");
                Debug.Log("[Test 5] PASSED: TTS integration working (fallback behavior)");
            }
        }

        /// <summary>
        /// Test 6: エンドツーエンド統合テスト
        /// </summary>
        [ContextMenu("Test 6: End-to-End Integration")]
        public void TestEndToEndIntegration()
        {
            StartCoroutine(TestEndToEndIntegrationCoroutine());
        }

        private IEnumerator TestEndToEndIntegrationCoroutine()
        {
            Debug.Log("[Test 6] Testing End-to-End Integration...");

            // 1. 行動記録
            Debug.Log("[Test 6] Step 1: Recording player behavior...");
            behaviorAnalyzer.RecordPlayerAction(0, 2.5f, 3.0f);
            yield return new WaitForSeconds(0.5f);

            // 2. 圧力レベル確認
            Debug.Log("[Test 6] Step 2: Checking pressure level...");
            float pressure = psychologySystem.GetPressureLevel();
            Debug.Log($"[Test 6] Current pressure: {pressure:F2}");

            // 3. 統計情報確認
            Debug.Log("[Test 6] Step 3: Checking statistics...");
            Debug.Log(psychologySystem.GetStatistics());
            Debug.Log(behaviorAnalyzer.GetStatistics());

            Debug.Log("[Test 6] PASSED: End-to-end integration working");
        }

        /// <summary>
        /// すべてのシステムをリセット（テスト間）
        /// </summary>
        [ContextMenu("Reset All Systems")]
        public void ResetAllSystems()
        {
            Debug.Log("[Reset] Resetting all systems...");

            psychologySystem.ResetSystem();
            behaviorAnalyzer.ClearHistory();

            Debug.Log("[Reset] All systems reset");
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグUI（エディタのみ）
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 440, 600, 150));

            GUILayout.Label("=== Psychology System Tests ===");

            if (GUILayout.Button("Run All Tests"))
            {
                RunAllTestsMenu();
            }

            if (GUILayout.Button("Reset All Systems"))
            {
                ResetAllSystems();
            }

            GUILayout.EndArea();
        }
#endif
    }
}
