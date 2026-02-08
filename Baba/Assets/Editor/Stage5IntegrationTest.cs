using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;

/// <summary>
/// Stage 5: 感情AIシステム 統合テスト
/// Tools > Baba > Test Stage 5 Integration から実行
/// </summary>
public class Stage5IntegrationTest : EditorWindow
{
    private BluffSystem bluffSystem;
    private PsychologySystem psychologySystem;
    private PlayerBehaviorAnalyzer behaviorAnalyzer;
    private FloatingTextSystem floatingTextSystem;
    private GameManager gameManager;

    private Vector2 scrollPos;
    private bool isTestRunning = false;

    // テスト結果
    private bool setupCheckPassed = false;
    private List<string> setupErrors = new List<string>();
    private List<string> testResults = new List<string>();

    [MenuItem("Tools/Baba/Test Stage 5 Integration")]
    public static void ShowWindow()
    {
        var window = GetWindow<Stage5IntegrationTest>("Stage 5 Test");
        window.minSize = new Vector2(450, 650);
        window.Show();
    }

    private void OnEnable()
    {
        FindComponents();
        RunSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage 5 - Emotional AI Integration Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはStage 5の感情AIシステムの統合テストを実行します。\n" +
            "期待決定、感情マトリクス、Layer A台詞、リセットの動作を確認できます。",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawSetupCheck();

        EditorGUILayout.Space();

        DrawComponentReferences();

        EditorGUILayout.Space();

        DrawTestButtons();

        EditorGUILayout.Space();

        DrawTestResults();
    }

    // ===================
    // UI描画
    // ===================

    private void DrawSetupCheck()
    {
        EditorGUILayout.LabelField("Setup Check", EditorStyles.boldLabel);

        if (setupCheckPassed)
        {
            EditorGUILayout.HelpBox("All components found! Ready to test.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Some components are missing. Run auto-setup tools first.", MessageType.Warning);

            if (setupErrors.Count > 0)
            {
                EditorGUILayout.LabelField("Missing Components:", EditorStyles.boldLabel);
                foreach (var error in setupErrors)
                {
                    EditorGUILayout.LabelField($"  {error}", EditorStyles.helpBox);
                }
            }
        }

        if (GUILayout.Button("Refresh Setup Check"))
        {
            FindComponents();
            RunSetupCheck();
        }
    }

    private void DrawComponentReferences()
    {
        EditorGUILayout.LabelField("Component References", EditorStyles.boldLabel);

        GUI.enabled = false;
        EditorGUILayout.ObjectField("BluffSystem", bluffSystem, typeof(BluffSystem), true);
        EditorGUILayout.ObjectField("PsychologySystem", psychologySystem, typeof(PsychologySystem), true);
        EditorGUILayout.ObjectField("PlayerBehaviorAnalyzer", behaviorAnalyzer, typeof(PlayerBehaviorAnalyzer), true);
        EditorGUILayout.ObjectField("FloatingTextSystem", floatingTextSystem, typeof(FloatingTextSystem), true);
        EditorGUILayout.ObjectField("GameManager", gameManager, typeof(GameManager), true);
        GUI.enabled = true;
    }

    private void DrawTestButtons()
    {
        EditorGUILayout.LabelField("Tests", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Tests must be run in Play Mode.", MessageType.Warning);
            GUI.enabled = false;
        }
        else if (isTestRunning)
        {
            EditorGUILayout.HelpBox("Test is running...", MessageType.Info);
            GUI.enabled = false;
        }
        else if (!setupCheckPassed)
        {
            EditorGUILayout.HelpBox("Cannot run tests. Missing components.", MessageType.Warning);
            GUI.enabled = false;
        }

        EditorGUILayout.LabelField("Individual Tests:", EditorStyles.boldLabel);

        if (GUILayout.Button("Test 1: DetermineExpectation Early Turn", GUILayout.Height(28)))
        {
            RunTest(() => Test1_DetermineExpectationEarlyTurn());
        }

        if (GUILayout.Button("Test 2: DetermineExpectation Active Turn", GUILayout.Height(28)))
        {
            RunTest(() => Test2_DetermineExpectationActiveTurn());
        }

        if (GUILayout.Button("Test 3: Emotion Matrix (Player Turn)", GUILayout.Height(28)))
        {
            RunTest(() => Test3_EmotionMatrixPlayerTurn());
        }

        if (GUILayout.Button("Test 4: Emotion Matrix (AI Turn)", GUILayout.Height(28)))
        {
            RunTest(() => Test4_EmotionMatrixAITurn());
        }

        if (GUILayout.Button("Test 5: ResetSystem", GUILayout.Height(28)))
        {
            RunTest(() => Test5_ResetSystem());
        }

        if (GUILayout.Button("Test 6: Layer A Dialogue Coverage", GUILayout.Height(28)))
        {
            RunTest(() => Test6_LayerADialogueCoverage());
        }

        if (GUILayout.Button("Test 7: Turning Point Detection", GUILayout.Height(28)))
        {
            RunTest(() => Test7_TurningPointDetection());
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Run All:", EditorStyles.boldLabel);

        if (GUILayout.Button("Run All Tests", GUILayout.Height(40)))
        {
            RunTest(() => RunAllTests());
        }

        GUI.enabled = true;
    }

    private void DrawTestResults()
    {
        EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);

        if (testResults.Count == 0)
        {
            EditorGUILayout.LabelField("No tests run yet.", EditorStyles.helpBox);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));

        foreach (var result in testResults)
        {
            if (result.Contains("PASS"))
            {
                GUI.color = Color.green;
            }
            else if (result.Contains("FAIL"))
            {
                GUI.color = Color.red;
            }
            else if (result.Contains("WARN"))
            {
                GUI.color = Color.yellow;
            }
            else
            {
                GUI.color = Color.white;
            }

            EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Clear Results"))
        {
            testResults.Clear();
        }
    }

    // ===================
    // Setup
    // ===================

    private void FindComponents()
    {
        bluffSystem = FindFirstObjectByType<BluffSystem>();
        psychologySystem = FindFirstObjectByType<PsychologySystem>();
        behaviorAnalyzer = FindFirstObjectByType<PlayerBehaviorAnalyzer>();
        floatingTextSystem = FindFirstObjectByType<FloatingTextSystem>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void RunSetupCheck()
    {
        setupErrors.Clear();
        setupCheckPassed = true;

        if (bluffSystem == null)
        {
            setupErrors.Add("BluffSystem not found (Run: Tools > Baba > Setup BluffSystem)");
            setupCheckPassed = false;
        }

        if (psychologySystem == null)
        {
            setupErrors.Add("PsychologySystem not found");
            setupCheckPassed = false;
        }

        if (behaviorAnalyzer == null)
        {
            setupErrors.Add("PlayerBehaviorAnalyzer not found");
            setupCheckPassed = false;
        }

        if (floatingTextSystem == null)
        {
            setupErrors.Add("FloatingTextSystem not found");
            setupCheckPassed = false;
        }

        if (gameManager == null)
        {
            setupErrors.Add("GameManager not found");
            setupCheckPassed = false;
        }

        Debug.Log($"[Stage5IntegrationTest] Setup check: {(setupCheckPassed ? "PASSED" : "FAILED")}");
    }

    private void RunTest(System.Action testAction)
    {
        isTestRunning = true;
        testResults.Clear();
        testResults.Add($"=== Test Started at {System.DateTime.Now:HH:mm:ss} ===");

        try
        {
            testAction?.Invoke();
        }
        catch (System.Exception e)
        {
            testResults.Add($"[FAIL] Test failed with exception: {e.Message}");
            Debug.LogError($"[Stage5IntegrationTest] Test exception: {e}");
        }

        testResults.Add($"=== Test Completed at {System.DateTime.Now:HH:mm:ss} ===");
        isTestRunning = false;
        Repaint();
    }

    // ===================
    // Individual Tests
    // ===================

    private void Test1_DetermineExpectationEarlyTurn()
    {
        testResults.Add("--- Test 1: DetermineExpectation Early Turn ---");

        bluffSystem.ResetSystem();

        int turnsBeforeExpectation = bluffSystem.turnsBeforeExpectation;
        testResults.Add($"   turnsBeforeExpectation = {turnsBeforeExpectation}");

        bool allNeutral = true;
        for (int i = 0; i < turnsBeforeExpectation; i++)
        {
            AIExpectation expectation = bluffSystem.DetermineExpectation();
            testResults.Add($"   Turn {i + 1}: expectation = {expectation}");

            if (expectation != AIExpectation.Neutral)
            {
                allNeutral = false;
                testResults.Add($"[FAIL] Expected Neutral at turn {i + 1}, got {expectation}");
            }
        }

        // Verify emotion is Anticipating after DetermineExpectation
        AIEmotion emotionAfter = bluffSystem.GetCurrentEmotion();
        testResults.Add($"   Emotion after DetermineExpectation: {emotionAfter}");

        if (emotionAfter != AIEmotion.Anticipating)
        {
            testResults.Add($"[FAIL] Expected Anticipating emotion, got {emotionAfter}");
            allNeutral = false;
        }

        if (allNeutral)
        {
            testResults.Add($"[PASS] All {turnsBeforeExpectation} early turns returned Neutral + Anticipating");
        }

        bluffSystem.ResetSystem();
    }

    private void Test2_DetermineExpectationActiveTurn()
    {
        testResults.Add("--- Test 2: DetermineExpectation Active Turn ---");

        int trials = 100;
        int nonNeutralCount = 0;
        int stopCount = 0;
        int baitCount = 0;
        int turnsBeforeExpectation = bluffSystem.turnsBeforeExpectation;

        for (int t = 0; t < trials; t++)
        {
            bluffSystem.ResetSystem();

            // Skip past early turns
            for (int i = 0; i < turnsBeforeExpectation; i++)
            {
                bluffSystem.DetermineExpectation();
            }

            // Now test expectation-eligible turn
            AIExpectation expectation = bluffSystem.DetermineExpectation();
            if (expectation != AIExpectation.Neutral)
            {
                nonNeutralCount++;
                if (expectation == AIExpectation.Stop) stopCount++;
                if (expectation == AIExpectation.Bait) baitCount++;
            }
        }

        testResults.Add($"   {trials} trials: {nonNeutralCount} non-Neutral ({(float)nonNeutralCount / trials:P0})");
        testResults.Add($"   Stop: {stopCount}, Bait: {baitCount}");

        if (nonNeutralCount > 0)
        {
            testResults.Add($"[PASS] Expectation occurred in {nonNeutralCount}/{trials} trials");
        }
        else
        {
            testResults.Add($"[FAIL] No expectations occurred in {trials} trials (baseExpectationChance={bluffSystem.baseExpectationChance})");
        }

        if (stopCount > 0 && baitCount > 0)
        {
            testResults.Add($"[PASS] Both Stop and Bait expectations observed");
        }
        else
        {
            testResults.Add($"[WARN] Only one type of expectation observed (Stop={stopCount}, Bait={baitCount})");
        }

        bluffSystem.ResetSystem();
    }

    private void Test3_EmotionMatrixPlayerTurn()
    {
        testResults.Add("--- Test 3: Emotion Matrix (Player Turn) ---");

        bool allPass = true;

        // Test all 9 combinations: 3 expectations x 3 card types
        var testCases = new[]
        {
            // (expectation to force, joker, pair, expected emotion)
            new { Exp = AIExpectation.Stop, Joker = true, Pair = false, Expected = AIEmotion.Pleased },
            new { Exp = AIExpectation.Stop, Joker = false, Pair = false, Expected = AIEmotion.Frustrated },
            new { Exp = AIExpectation.Stop, Joker = false, Pair = true, Expected = AIEmotion.Frustrated },
            new { Exp = AIExpectation.Bait, Joker = true, Pair = false, Expected = AIEmotion.Hurt },
            new { Exp = AIExpectation.Bait, Joker = false, Pair = false, Expected = AIEmotion.Pleased },
            new { Exp = AIExpectation.Bait, Joker = false, Pair = true, Expected = AIEmotion.Frustrated },
            new { Exp = AIExpectation.Neutral, Joker = true, Pair = false, Expected = AIEmotion.Relieved },
            new { Exp = AIExpectation.Neutral, Joker = false, Pair = false, Expected = AIEmotion.Calm },
            new { Exp = AIExpectation.Neutral, Joker = false, Pair = true, Expected = AIEmotion.Calm },
        };

        foreach (var tc in testCases)
        {
            bluffSystem.ResetSystem();

            // Force expectation via reflection
            var expField = typeof(BluffSystem).GetField("currentExpectation", BindingFlags.NonPublic | BindingFlags.Instance);
            expField.SetValue(bluffSystem, tc.Exp);

            DrawContext ctx = new DrawContext
            {
                isPlayerTurn = true,
                drawnCardIsJoker = tc.Joker,
                formedPair = tc.Pair,
                remainingCards = 5,
                opponentRemainingCards = 5
            };

            EmotionalResult result = bluffSystem.EvaluateReaction(ctx).GetAwaiter().GetResult();

            string cardType = tc.Joker ? "Joker" : (tc.Pair ? "Pair" : "Normal");
            string status = result.emotion == tc.Expected ? "OK" : "MISMATCH";

            testResults.Add($"   {tc.Exp}+{cardType} → {result.emotion} (expected {tc.Expected}) [{status}]");

            if (result.emotion != tc.Expected)
            {
                testResults.Add($"[FAIL] {tc.Exp}+{cardType}: got {result.emotion}, expected {tc.Expected}");
                allPass = false;
            }

            // Verify dialogue is not empty
            if (string.IsNullOrEmpty(result.immediateDialogue))
            {
                testResults.Add($"[FAIL] {tc.Exp}+{cardType}: immediateDialogue is empty");
                allPass = false;
            }
        }

        if (allPass)
        {
            testResults.Add($"[PASS] All 9 emotion matrix combinations correct");
        }

        bluffSystem.ResetSystem();
    }

    private void Test4_EmotionMatrixAITurn()
    {
        testResults.Add("--- Test 4: Emotion Matrix (AI Turn) ---");

        bool allPass = true;

        var testCases = new[]
        {
            new { Joker = true, Pair = false, Expected = AIEmotion.Frustrated },
            new { Joker = false, Pair = false, Expected = AIEmotion.Calm },
            new { Joker = false, Pair = true, Expected = AIEmotion.Pleased },
        };

        foreach (var tc in testCases)
        {
            bluffSystem.ResetSystem();

            DrawContext ctx = new DrawContext
            {
                isPlayerTurn = false,
                drawnCardIsJoker = tc.Joker,
                formedPair = tc.Pair,
                remainingCards = 5,
                opponentRemainingCards = 5
            };

            EmotionalResult result = bluffSystem.EvaluateReaction(ctx).GetAwaiter().GetResult();

            string cardType = tc.Joker ? "Joker" : (tc.Pair ? "Pair" : "Normal");
            string status = result.emotion == tc.Expected ? "OK" : "MISMATCH";

            testResults.Add($"   AI+{cardType} → {result.emotion} (expected {tc.Expected}) [{status}]");

            if (result.emotion != tc.Expected)
            {
                testResults.Add($"[FAIL] AI+{cardType}: got {result.emotion}, expected {tc.Expected}");
                allPass = false;
            }
        }

        if (allPass)
        {
            testResults.Add($"[PASS] All 3 AI turn emotion cases correct");
        }

        bluffSystem.ResetSystem();
    }

    private void Test5_ResetSystem()
    {
        testResults.Add("--- Test 5: ResetSystem ---");

        // Accumulate some state
        for (int i = 0; i < 5; i++)
        {
            bluffSystem.DetermineExpectation();
        }

        AIExpectation expectationBefore = bluffSystem.GetCurrentExpectation();
        AIEmotion emotionBefore = bluffSystem.GetCurrentEmotion();
        int turnCountBefore = bluffSystem.GetTurnCount();

        testResults.Add($"   Before reset: expectation={expectationBefore}, emotion={emotionBefore}, turnCount={turnCountBefore}");

        // Reset
        bluffSystem.ResetSystem();

        AIExpectation expectationAfter = bluffSystem.GetCurrentExpectation();
        AIEmotion emotionAfter = bluffSystem.GetCurrentEmotion();
        int turnCountAfter = bluffSystem.GetTurnCount();

        testResults.Add($"   After reset: expectation={expectationAfter}, emotion={emotionAfter}, turnCount={turnCountAfter}");

        bool pass = true;

        if (expectationAfter != AIExpectation.Neutral)
        {
            testResults.Add($"[FAIL] Expectation should be Neutral after reset, got {expectationAfter}");
            pass = false;
        }

        if (emotionAfter != AIEmotion.Calm)
        {
            testResults.Add($"[FAIL] Emotion should be Calm after reset, got {emotionAfter}");
            pass = false;
        }

        if (turnCountAfter != 0)
        {
            testResults.Add($"[FAIL] TurnCount should be 0 after reset, got {turnCountAfter}");
            pass = false;
        }

        if (pass)
        {
            testResults.Add($"[PASS] ResetSystem cleared all state correctly");
        }
    }

    private void Test6_LayerADialogueCoverage()
    {
        testResults.Add("--- Test 6: Layer A Dialogue Coverage ---");

        // Access private emotionDialogues via reflection
        var dialoguesField = typeof(BluffSystem).GetField("emotionDialogues", BindingFlags.NonPublic | BindingFlags.Instance);
        if (dialoguesField == null)
        {
            testResults.Add("[FAIL] Cannot access emotionDialogues field");
            return;
        }

        var dialogues = (Dictionary<AIEmotion, string[]>)dialoguesField.GetValue(bluffSystem);
        if (dialogues == null)
        {
            testResults.Add("[FAIL] emotionDialogues is null");
            return;
        }

        testResults.Add($"   Total emotion keys: {dialogues.Count}");

        // All 6 emotions should have entries
        AIEmotion[] allEmotions = {
            AIEmotion.Calm, AIEmotion.Anticipating, AIEmotion.Pleased,
            AIEmotion.Frustrated, AIEmotion.Hurt, AIEmotion.Relieved
        };

        int found = 0;
        int totalDialogues = 0;

        foreach (AIEmotion emotion in allEmotions)
        {
            if (dialogues.TryGetValue(emotion, out string[] options) && options != null && options.Length > 0)
            {
                found++;
                totalDialogues += options.Length;
                testResults.Add($"   {emotion}: {options.Length} dialogues");
            }
            else
            {
                testResults.Add($"   [WARN] {emotion}: no dialogues found");
            }
        }

        testResults.Add($"   Coverage: {found}/6 emotions, {totalDialogues} total dialogues");

        if (found == 6)
        {
            testResults.Add($"[PASS] All 6 emotions have Layer A dialogues ({totalDialogues} total)");
        }
        else
        {
            testResults.Add($"[FAIL] Missing dialogues for {6 - found} emotion(s)");
        }
    }

    private void Test7_TurningPointDetection()
    {
        testResults.Add("--- Test 7: Turning Point Detection ---");

        bool pass = true;

        // Joker should be turning point
        DrawContext jokerCtx = new DrawContext
        {
            isPlayerTurn = true,
            drawnCardIsJoker = true,
            formedPair = false,
            remainingCards = 5,
            opponentRemainingCards = 5
        };

        bool jokerTP = bluffSystem.IsTurningPoint(jokerCtx);
        testResults.Add($"   Joker: isTurningPoint = {jokerTP}");
        if (!jokerTP)
        {
            testResults.Add("[FAIL] Joker should be a turning point");
            pass = false;
        }

        // End game (<=3 cards) should be turning point
        DrawContext endGameCtx = new DrawContext
        {
            isPlayerTurn = true,
            drawnCardIsJoker = false,
            formedPair = false,
            remainingCards = 2,
            opponentRemainingCards = 5
        };

        bool endGameTP = bluffSystem.IsTurningPoint(endGameCtx);
        testResults.Add($"   EndGame (2 cards): isTurningPoint = {endGameTP}");
        if (!endGameTP)
        {
            testResults.Add("[FAIL] End game should be a turning point");
            pass = false;
        }

        // Normal mid-game should not be turning point
        DrawContext normalCtx = new DrawContext
        {
            isPlayerTurn = true,
            drawnCardIsJoker = false,
            formedPair = false,
            remainingCards = 8,
            opponentRemainingCards = 8
        };

        bool normalTP = bluffSystem.IsTurningPoint(normalCtx);
        testResults.Add($"   Normal mid-game: isTurningPoint = {normalTP}");
        if (normalTP)
        {
            testResults.Add("[FAIL] Normal mid-game should not be a turning point");
            pass = false;
        }

        if (pass)
        {
            testResults.Add("[PASS] Turning point detection works correctly");
        }
    }

    // ===================
    // Run All
    // ===================

    private void RunAllTests()
    {
        testResults.Add("=== RUNNING ALL TESTS ===");
        testResults.Add("");

        Test1_DetermineExpectationEarlyTurn();
        testResults.Add("");

        Test2_DetermineExpectationActiveTurn();
        testResults.Add("");

        Test3_EmotionMatrixPlayerTurn();
        testResults.Add("");

        Test4_EmotionMatrixAITurn();
        testResults.Add("");

        Test5_ResetSystem();
        testResults.Add("");

        Test6_LayerADialogueCoverage();
        testResults.Add("");

        Test7_TurningPointDetection();
        testResults.Add("");

        // Summary
        int passCount = 0;
        int failCount = 0;
        int warnCount = 0;

        foreach (var r in testResults)
        {
            if (r.Contains("[PASS]")) passCount++;
            if (r.Contains("[FAIL]")) failCount++;
            if (r.Contains("[WARN]")) warnCount++;
        }

        testResults.Add("=== SUMMARY ===");
        testResults.Add($"   PASS: {passCount}, FAIL: {failCount}, WARN: {warnCount}");

        if (failCount == 0)
        {
            testResults.Add("[PASS] All tests passed!");
        }
        else
        {
            testResults.Add($"[FAIL] {failCount} test(s) failed");
        }
    }
}
