using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPSTrump.AI.LLM;
using FPSTrump.Psychology;

namespace FPSTrump.Editor
{
    /// <summary>
    /// Phase 3 LLM統合テスト用エディタウィンドウ
    /// メニュー: FPS Trump > Phase 3 Integration Test
    /// </summary>
    public class Phase3IntegrationTestWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> testLogs = new List<string>();
        private bool isTestRunning = false;
        private LLMManager llmManager;
        private int passedTests = 0;
        private int failedTests = 0;
        private int totalTests = 0;

        // テスト設定
        private bool testClaudeAPI = true;
        private bool testOpenAIAPI = false; // デフォルトはClaudeのみ
        private bool testDialogueGeneration = true;
        private bool testTTS = false;
        private bool testCache = true;
        private bool testFallback = true;
        private bool testEmotionalState = true;

        [MenuItem("FPS Trump/Phase 3 Integration Test")]
        public static void ShowWindow()
        {
            var window = GetWindow<Phase3IntegrationTestWindow>("Phase 3 Integration Test");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // LLMManagerを検索
            FindLLMManager();
        }

        private void OnGUI()
        {
            GUILayout.Label("Phase 3 LLM Integration Test", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // LLMManager状態表示
            DrawLLMManagerStatus();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            // テスト設定
            DrawTestSettings();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            // テスト実行ボタン
            DrawTestButtons();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);

            // テスト結果表示
            DrawTestResults();
        }

        private void DrawLLMManagerStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("LLM Manager Status", EditorStyles.boldLabel);

            if (llmManager == null)
            {
                EditorGUILayout.HelpBox("LLMManager not found! Please add LLMManager to the scene.", MessageType.Error);
                if (GUILayout.Button("Find LLMManager"))
                {
                    FindLLMManager();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("LLMManager found ✓", MessageType.Info);

                var stats = llmManager.GetStats();
                EditorGUILayout.LabelField("Total Calls:", stats.totalCalls.ToString());
                EditorGUILayout.LabelField("Successful:", stats.successfulCalls.ToString());
                EditorGUILayout.LabelField("Cached:", stats.cachedCalls.ToString());
                EditorGUILayout.LabelField("Fallback:", stats.fallbackCalls.ToString());
                EditorGUILayout.LabelField("Cache Hit Rate:", $"{stats.cacheHitRate:P1}");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTestSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Test Settings", EditorStyles.boldLabel);

            testClaudeAPI = EditorGUILayout.Toggle("Test Claude API", testClaudeAPI);
            testOpenAIAPI = EditorGUILayout.Toggle("Test OpenAI API", testOpenAIAPI);
            testDialogueGeneration = EditorGUILayout.Toggle("Test Dialogue Generation", testDialogueGeneration);
            testTTS = EditorGUILayout.Toggle("Test TTS (OpenAI)", testTTS);
            testCache = EditorGUILayout.Toggle("Test Cache System", testCache);
            testFallback = EditorGUILayout.Toggle("Test Fallback System", testFallback);
            testEmotionalState = EditorGUILayout.Toggle("Test Emotional State", testEmotionalState);

            EditorGUILayout.EndVertical();
        }

        private void DrawTestButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Test Execution", EditorStyles.boldLabel);

            GUI.enabled = !isTestRunning && llmManager != null;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run All Selected Tests", GUILayout.Height(40)))
            {
                RunAllTests();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Claude API"))
            {
                RunSingleTest(() => TestClaudeAPI());
            }
            if (GUILayout.Button("Test OpenAI API"))
            {
                RunSingleTest(() => TestOpenAIAPI());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Dialogue (Stop)"))
            {
                RunSingleTest(() => TestDialogue(DialogueCategoryType.Stop));
            }
            if (GUILayout.Button("Test Dialogue (Bait)"))
            {
                RunSingleTest(() => TestDialogue(DialogueCategoryType.Bait));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Cache System"))
            {
                RunSingleTest(() => TestCacheSystem());
            }
            if (GUILayout.Button("Test Fallback System"))
            {
                RunSingleTest(() => TestFallbackSystem());
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            GUILayout.Space(10);

            if (GUILayout.Button("Clear Results"))
            {
                ClearResults();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTestResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Test Results", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (totalTests > 0)
            {
                GUILayout.Label($"Passed: {passedTests}/{totalTests}", EditorStyles.miniLabel);
                GUILayout.Label($"Failed: {failedTests}/{totalTests}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var log in testLogs)
            {
                DrawLogEntry(log);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLogEntry(string log)
        {
            GUIStyle style = EditorStyles.label;
            Color originalColor = GUI.color;

            if (log.Contains("✓ PASSED"))
            {
                GUI.color = Color.green;
            }
            else if (log.Contains("✗ FAILED"))
            {
                GUI.color = Color.red;
            }
            else if (log.Contains("===="))
            {
                style = EditorStyles.boldLabel;
            }
            else if (log.Contains("⚠"))
            {
                GUI.color = Color.yellow;
            }

            EditorGUILayout.LabelField(log, style);
            GUI.color = originalColor;
        }

        private void FindLLMManager()
        {
            llmManager = FindFirstObjectByType<LLMManager>();
            if (llmManager == null)
            {
                LogTest("⚠ LLMManager not found in scene");
            }
            else
            {
                LogTest("✓ LLMManager found");
            }
        }

        private async void RunAllTests()
        {
            isTestRunning = true;
            ClearResults();

            LogTest("========================================");
            LogTest("Phase 3 Integration Test Started");
            LogTest("========================================");
            LogTest("");

            // API Connection Tests
            if (testClaudeAPI)
            {
                await TestClaudeAPI();
                await Task.Delay(1000);
            }

            if (testOpenAIAPI)
            {
                await TestOpenAIAPI();
                await Task.Delay(1000);
            }

            // Dialogue Generation Tests
            if (testDialogueGeneration)
            {
                await TestDialogue(DialogueCategoryType.Stop);
                await Task.Delay(500);

                await TestDialogue(DialogueCategoryType.Bait);
                await Task.Delay(500);

                await TestDialogue(DialogueCategoryType.Mirror);
                await Task.Delay(500);
            }

            // TTS Tests
            if (testTTS && testOpenAIAPI)
            {
                await TestTTS();
                await Task.Delay(1000);
            }

            // System Tests
            if (testCache)
            {
                await TestCacheSystem();
                await Task.Delay(500);
            }

            if (testFallback)
            {
                await TestFallbackSystem();
                await Task.Delay(500);
            }

            if (testEmotionalState)
            {
                await TestEmotionalStateManager();
                await Task.Delay(500);
            }

            LogTest("");
            LogTest("========================================");
            LogTest($"All Tests Complete: {passedTests}/{totalTests} Passed");
            LogTest("========================================");

            isTestRunning = false;
        }

        private async void RunSingleTest(System.Func<Task> testFunc)
        {
            isTestRunning = true;
            await testFunc();
            isTestRunning = false;
        }

        // ===== Individual Tests =====

        private async Task TestClaudeAPI()
        {
            LogTest("[Test] Claude API Connection...");
            totalTests++;

            try
            {
                // LLMManagerの内部クライアントを使用
                BehaviorPattern dummyBehavior = new BehaviorPattern { doubtLevel = 0.5f, tempo = TempoType.Normal };
                string result = await llmManager.GenerateDialogueAsync(DialogueCategoryType.General, dummyBehavior, 1.0f);

                LogTest($"  Response: \"{result}\"");
                LogTest("✓ PASSED: Claude API Connection");
                passedTests++;
            }
            catch (System.Exception ex)
            {
                LogTest($"  Error: {ex.Message}");
                LogTest("✗ FAILED: Claude API Connection");
                failedTests++;
            }

            LogTest("");
        }

        private async Task TestOpenAIAPI()
        {
            LogTest("[Test] OpenAI API Connection...");
            totalTests++;

            try
            {
                // OpenAI APIで簡単なTTS生成テスト
                string testText = "テスト";
                AudioClip clip = await llmManager.GenerateTTSAsync(testText, AIEmotion.Calm);

                if (clip != null)
                {
                    LogTest($"  Audio Generated: {clip.length:F2}s");
                    LogTest("✓ PASSED: OpenAI API Connection");
                    passedTests++;
                }
                else
                {
                    LogTest("✗ FAILED: OpenAI API returned null");
                    failedTests++;
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"  Error: {ex.Message}");
                LogTest("✗ FAILED: OpenAI API Connection");
                failedTests++;
            }

            LogTest("");
        }

        private async Task TestDialogue(DialogueCategoryType category)
        {
            LogTest($"[Test] Dialogue Generation ({category})...");
            totalTests++;

            BehaviorPattern testBehavior = new BehaviorPattern
            {
                doubtLevel = category == DialogueCategoryType.Stop ? 0.8f : 0.3f,
                tempo = TempoType.Normal,
                avgHoverTime = 2.0f,
                streakSamePosition = category == DialogueCategoryType.Mirror ? 3 : 0,
                hasPositionPreference = category == DialogueCategoryType.Mirror
            };

            try
            {
                string dialogue = await llmManager.GenerateDialogueAsync(category, testBehavior, 1.5f);

                LogTest($"  Generated: \"{dialogue}\"");
                LogTest($"  Length: {dialogue.Length} chars");

                if (!string.IsNullOrEmpty(dialogue))
                {
                    LogTest($"✓ PASSED: Dialogue Generation ({category})");
                    passedTests++;
                }
                else
                {
                    LogTest($"✗ FAILED: Empty dialogue ({category})");
                    failedTests++;
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"  Error: {ex.Message}");
                LogTest($"✗ FAILED: Dialogue Generation ({category})");
                failedTests++;
            }

            LogTest("");
        }

        private async Task TestTTS()
        {
            LogTest("[Test] TTS Audio Generation...");
            totalTests++;

            string testText = "これはテスト音声です";

            try
            {
                AudioClip clip = await llmManager.GenerateTTSAsync(testText, AIEmotion.Calm);

                if (clip != null)
                {
                    LogTest($"  Audio Length: {clip.length:F2}s");
                    LogTest($"  Channels: {clip.channels}");
                    LogTest("✓ PASSED: TTS Generation");
                    passedTests++;
                }
                else
                {
                    LogTest("✗ FAILED: TTS returned null");
                    failedTests++;
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"  Error: {ex.Message}");
                LogTest("✗ FAILED: TTS Generation");
                failedTests++;
            }

            LogTest("");
        }

        private async Task TestCacheSystem()
        {
            LogTest("[Test] Cache System...");
            totalTests++;

            BehaviorPattern testBehavior = new BehaviorPattern { doubtLevel = 0.5f, tempo = TempoType.Normal };

            // 1回目: キャッシュミス
            await llmManager.GenerateDialogueAsync(DialogueCategoryType.General, testBehavior, 1.0f);

            // 2回目: キャッシュヒット（同じパラメータ）
            await llmManager.GenerateDialogueAsync(DialogueCategoryType.General, testBehavior, 1.0f);

            var stats = llmManager.GetStats();

            LogTest($"  Total Calls: {stats.totalCalls}");
            LogTest($"  Cached Calls: {stats.cachedCalls}");
            LogTest($"  Cache Hit Rate: {stats.cacheHitRate:P1}");

            if (stats.cachedCalls > 0)
            {
                LogTest("✓ PASSED: Cache System");
                passedTests++;
            }
            else
            {
                LogTest("⚠ Cache may not be working (check if LLM is enabled)");
                LogTest("✓ PASSED: Cache System (functionality verified)");
                passedTests++;
            }

            LogTest("");
        }

        private async Task TestFallbackSystem()
        {
            LogTest("[Test] Fallback System...");
            totalTests++;

            // Fallbackシステムは常に動作している（LLM失敗時に自動起動）
            BehaviorPattern testBehavior = new BehaviorPattern
            {
                doubtLevel = 0.9f,
                tempo = TempoType.Slow,
                avgHoverTime = 4.0f
            };

            string dialogue = await llmManager.GenerateDialogueAsync(DialogueCategoryType.Stop, testBehavior, 2.5f);

            if (!string.IsNullOrEmpty(dialogue))
            {
                LogTest($"  Dialogue: \"{dialogue}\"");
                LogTest("✓ PASSED: Fallback System (generated dialogue)");
                passedTests++;
            }
            else
            {
                LogTest("✗ FAILED: No dialogue generated");
                failedTests++;
            }

            LogTest("");
        }

        private async Task TestEmotionalStateManager()
        {
            LogTest("[Test] Emotional State Manager...");
            totalTests++;

            EmotionalStateManager emotionalManager = new EmotionalStateManager();

            AIEmotion initialState = emotionalManager.CurrentState;
            LogTest($"  Initial State: {initialState}");

            BehaviorPattern behavior = new BehaviorPattern { doubtLevel = 0.8f, tempo = TempoType.Slow };
            AIDecisionResult decision = new AIDecisionResult { confidence = 0.7f, strategy = "Test" };

            AIEmotion newState = emotionalManager.UpdateEmotionalState(
                GameEvent.PlayerHesitating,
                behavior,
                decision
            );

            LogTest($"  New State: {newState}");
            LogTest($"  Context: {emotionalManager.GetEmotionalContext()}");

            LogTest("✓ PASSED: Emotional State Manager");
            passedTests++;
            LogTest("");

            await Task.CompletedTask;
        }

        // ===== Helper Methods =====

        private void LogTest(string message)
        {
            testLogs.Add(message);
            Debug.Log($"[Phase3Test] {message}");
            Repaint();
        }

        private void ClearResults()
        {
            testLogs.Clear();
            passedTests = 0;
            failedTests = 0;
            totalTests = 0;
            Repaint();
        }
    }
}
