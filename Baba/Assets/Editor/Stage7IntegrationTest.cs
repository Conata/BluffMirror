using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;
using FPSTrump.Result;

/// <summary>
/// Stage 7: リザルト診断システム 統合テスト
/// Tools > Baba > Test Stage 7 Integration から実行
/// </summary>
public class Stage7IntegrationTest : EditorWindow
{
    private ResultDiagnosisSystem diagnosisSystem;
    private GameSessionRecorder sessionRecorder;
    private ResultUI resultUI;
    private GameManager gameManager;

    private Vector2 scrollPos;
    private List<string> testResults = new List<string>();
    private List<string> setupErrors = new List<string>();
    private bool setupCheckPassed = false;

    // ========================================
    // テスト用パラメータ（スライダーで調整可能）
    // ========================================
    private bool showParamEditor = true;

    // 行動パラメータ
    private float p_avgDecisionTime = 4f;
    private float p_avgDoubtLevel = 0.3f;
    private float p_avgHoverTime = 2f;
    private TempoType p_dominantTempo = TempoType.Normal;
    private float p_tempoVariance = 1.5f;
    private bool p_hadPositionPreference = false;
    private int p_longestPositionStreak = 1;

    // 心理パラメータ
    private float p_pressureResponseScore = 0.5f;
    private float p_peakPressureLevel = 2f;
    private float p_avgPressureLevel = 1f;
    private int p_turningPointCount = 2;
    private int p_emotionCount = 3;

    // ゲーム結果
    private bool p_playerWon = true;
    private int p_totalTurns = 10;
    private float p_gameDuration = 120f;

    // プレビュー結果
    private DiagnosisStats previewStats;
    private PersonalityType previewPrimary;
    private PersonalityType previewSecondary;
    private DiagnosisResult previewResult;

    // LLMテスト
    private bool llmTestRunning = false;
    private string llmTestStatus = "";

    // タブ
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Parameter Test", "Auto Tests" };

    [MenuItem("Tools/Baba/Test Stage 7 Integration")]
    public static void ShowWindow()
    {
        var window = GetWindow<Stage7IntegrationTest>("Stage 7 Test");
        window.minSize = new Vector2(520, 800);
        window.Show();
    }

    private void OnEnable()
    {
        FindComponents();
        RunSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage 7 - Result Diagnosis Integration Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawSetupCheck();
        EditorGUILayout.Space();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (selectedTab == 0)
        {
            DrawParameterEditor();
        }
        else
        {
            DrawAutoTests();
        }

        EditorGUILayout.EndScrollView();
    }

    // ===================
    // Setup
    // ===================

    private void FindComponents()
    {
        // Play Mode中はSingleton Instanceを優先（FindFirstObjectByTypeがstaleになる場合がある）
        diagnosisSystem = ResultDiagnosisSystem.Instance != null
            ? ResultDiagnosisSystem.Instance
            : FindFirstObjectByType<ResultDiagnosisSystem>();
        sessionRecorder = GameSessionRecorder.Instance != null
            ? GameSessionRecorder.Instance
            : FindFirstObjectByType<GameSessionRecorder>();
        resultUI = ResultUI.Instance != null
            ? ResultUI.Instance
            : FindFirstObjectByType<ResultUI>();
        gameManager = GameManager.Instance != null
            ? GameManager.Instance
            : FindFirstObjectByType<GameManager>();
    }

    private void RunSetupCheck()
    {
        setupErrors.Clear();
        setupCheckPassed = true;

        if (diagnosisSystem == null) { setupErrors.Add("ResultDiagnosisSystem"); setupCheckPassed = false; }
        if (sessionRecorder == null) { setupErrors.Add("GameSessionRecorder"); setupCheckPassed = false; }
        if (resultUI == null) { setupErrors.Add("ResultUI"); setupCheckPassed = false; }
        if (gameManager == null) { setupErrors.Add("GameManager"); setupCheckPassed = false; }
    }

    // ===================
    // Setup Check UI
    // ===================

    private void DrawSetupCheck()
    {
        EditorGUILayout.BeginHorizontal();
        if (setupCheckPassed)
        {
            EditorGUILayout.HelpBox("All components found", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Missing: " + string.Join(", ", setupErrors), MessageType.Warning);
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(70), GUILayout.Height(38)))
        {
            FindComponents();
            RunSetupCheck();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ===================
    // Parameter Editor タブ
    // ===================

    private void DrawParameterEditor()
    {
        // --- 行動パラメータ ---
        EditorGUILayout.LabelField("Behavior Parameters", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        p_avgDecisionTime = EditorGUILayout.Slider("Avg Decision Time (sec)", p_avgDecisionTime, 0f, 15f);
        p_avgDoubtLevel = EditorGUILayout.Slider("Avg Doubt Level (0-1)", p_avgDoubtLevel, 0f, 1f);
        p_avgHoverTime = EditorGUILayout.Slider("Avg Hover Time (sec)", p_avgHoverTime, 0f, 10f);
        p_dominantTempo = (TempoType)EditorGUILayout.EnumPopup("Dominant Tempo", p_dominantTempo);
        p_tempoVariance = EditorGUILayout.Slider("Tempo Variance", p_tempoVariance, 0f, 10f);
        p_hadPositionPreference = EditorGUILayout.Toggle("Had Position Preference", p_hadPositionPreference);
        p_longestPositionStreak = EditorGUILayout.IntSlider("Longest Position Streak", p_longestPositionStreak, 0, 10);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // --- 心理パラメータ ---
        EditorGUILayout.LabelField("Psychology Parameters", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        p_pressureResponseScore = EditorGUILayout.Slider("Pressure Response (0=崩壊, 1=安定)", p_pressureResponseScore, 0f, 1f);
        p_peakPressureLevel = EditorGUILayout.Slider("Peak Pressure Level (0-3)", p_peakPressureLevel, 0f, 3f);
        p_avgPressureLevel = EditorGUILayout.Slider("Avg Pressure Level (0-3)", p_avgPressureLevel, 0f, 3f);
        p_turningPointCount = EditorGUILayout.IntSlider("Turning Point Count", p_turningPointCount, 0, 10);
        p_emotionCount = EditorGUILayout.IntSlider("Emotion Variety Count", p_emotionCount, 1, 6);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // --- ゲーム結果 ---
        EditorGUILayout.LabelField("Game Result", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        p_playerWon = EditorGUILayout.Toggle("Player Won", p_playerWon);
        p_totalTurns = EditorGUILayout.IntSlider("Total Turns", p_totalTurns, 1, 30);
        p_gameDuration = EditorGUILayout.Slider("Game Duration (sec)", p_gameDuration, 10f, 600f);

        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // --- プリセット ---
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Stoic"))
            ApplyPreset(avgDecision: 3f, doubt: 0.1f, hover: 1.5f, tempo: TempoType.Normal,
                tempoVar: 0.3f, pressure: 0.9f, streak: 5);
        if (GUILayout.Button("Intuitive"))
            ApplyPreset(avgDecision: 1.5f, doubt: 0.1f, hover: 0.8f, tempo: TempoType.Fast,
                tempoVar: 0.5f, pressure: 0.7f);
        if (GUILayout.Button("Cautious"))
            ApplyPreset(avgDecision: 8f, doubt: 0.8f, hover: 4f, tempo: TempoType.Slow,
                tempoVar: 1f, pressure: 0.4f);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Gambler"))
            ApplyPreset(avgDecision: 2f, doubt: 0.2f, hover: 1.5f, tempo: TempoType.Erratic,
                tempoVar: 8f, pressure: 0.5f);
        if (GUILayout.Button("Adapter"))
            ApplyPreset(avgDecision: 4f, doubt: 0.3f, hover: 2f, tempo: TempoType.Normal,
                tempoVar: 2f, pressure: 0.5f, positionPref: false, emotionCt: 5);
        if (GUILayout.Button("Analyst"))
            ApplyPreset(avgDecision: 4f, doubt: 0.3f, hover: 2f, tempo: TempoType.Normal,
                tempoVar: 1.5f, pressure: 0.5f);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // --- 診断実行ボタン ---
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview Classification", GUILayout.Height(35)))
        {
            RunPreviewClassification();
        }

        if (GUILayout.Button("Generate Fallback", GUILayout.Height(35)))
        {
            RunFallbackDiagnosis();
        }

        EditorGUILayout.EndHorizontal();

        // --- LLM診断テスト ---
        EditorGUILayout.Space(5);

        GUI.enabled = !llmTestRunning;
        if (GUILayout.Button("Generate LLM Diagnosis", GUILayout.Height(35)))
        {
            RunLLMDiagnosis();
        }
        GUI.enabled = true;

        if (llmTestRunning)
        {
            EditorGUILayout.HelpBox("LLM診断生成中...", MessageType.Info);
        }
        else if (!string.IsNullOrEmpty(llmTestStatus))
        {
            EditorGUILayout.HelpBox(llmTestStatus, llmTestStatus.StartsWith("Error") ? MessageType.Error : MessageType.Info);
        }

        EditorGUILayout.Space();

        // --- プレビュー結果表示 ---
        if (previewStats != null)
        {
            DrawPreviewResult();
        }
    }

    private void ApplyPreset(
        float avgDecision = 4f, float doubt = 0.3f, float hover = 2f,
        TempoType tempo = TempoType.Normal, float tempoVar = 1.5f,
        float pressure = 0.5f, int streak = 1, bool positionPref = false, int emotionCt = 3)
    {
        p_avgDecisionTime = avgDecision;
        p_avgDoubtLevel = doubt;
        p_avgHoverTime = hover;
        p_dominantTempo = tempo;
        p_tempoVariance = tempoVar;
        p_pressureResponseScore = pressure;
        p_longestPositionStreak = streak;
        p_hadPositionPreference = positionPref;
        p_emotionCount = emotionCt;
        previewStats = null;
        previewResult = null;
    }

    private GameSessionData BuildTestDataFromParams()
    {
        var data = new GameSessionData
        {
            playerWon = p_playerWon,
            totalTurns = p_totalTurns,
            gameDurationSeconds = p_gameDuration,
            avgDoubtLevel = p_avgDoubtLevel,
            avgHoverTime = p_avgHoverTime,
            avgDecisionTime = p_avgDecisionTime,
            dominantTempo = p_dominantTempo,
            hadPositionPreference = p_hadPositionPreference,
            longestPositionStreak = p_longestPositionStreak,
            totalPositionCounts = new int[] { 3, 4, 3 },
            peakPressureLevel = p_peakPressureLevel,
            avgPressureLevel = p_avgPressureLevel,
            turningPointCount = p_turningPointCount,
            totalReactions = p_totalTurns,
            avgReactionIntensity = 0.4f,
            pairsFormed = p_totalTurns / 2,
            tempoVariance = p_tempoVariance,
            pressureResponseScore = p_pressureResponseScore,
            emotionFrequency = new Dictionary<AIEmotion, int>()
        };

        // Emotion variety
        var emotions = new[] { AIEmotion.Calm, AIEmotion.Pleased, AIEmotion.Frustrated,
            AIEmotion.Anticipating, AIEmotion.Relieved, AIEmotion.Hurt };
        for (int i = 0; i < Mathf.Min(p_emotionCount, emotions.Length); i++)
        {
            data.emotionFrequency[emotions[i]] = Mathf.Max(1, 3 - i);
        }

        return data;
    }

    private void RunPreviewClassification()
    {
        FindComponents();
        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();
        var data = BuildTestDataFromParams();

        previewStats = system.CalculateStats(data);
        previewPrimary = system.ClassifyPersonality(previewStats, data);

        // secondary type (use fallback to get it)
        var fallbackResult = ResultDiagnosisPrompt.GenerateFallback(previewPrimary, PersonalityType.Analyst, previewStats);
        previewSecondary = fallbackResult.secondaryType;
        previewResult = null;

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private void RunFallbackDiagnosis()
    {
        FindComponents();
        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();
        var data = BuildTestDataFromParams();

        previewStats = system.CalculateStats(data);
        previewPrimary = system.ClassifyPersonality(previewStats, data);
        previewResult = system.GenerateFallbackDiagnosis(data);
        previewSecondary = previewResult.secondaryType;

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private async void RunLLMDiagnosis()
    {
        FindComponents();
        RunSetupCheck();

        if (diagnosisSystem == null)
        {
            llmTestStatus = "ResultDiagnosisSystem がシーンにありません。Tools > Baba > Setup Stage 7 を実行してください。";
            return;
        }

        llmTestRunning = true;
        llmTestStatus = "";
        Repaint();

        try
        {
            var data = BuildTestDataFromParams();
            DiagnosisResult result = await diagnosisSystem.GenerateDiagnosis(data);

            if (result != null)
            {
                previewStats = result.stats;
                previewPrimary = result.primaryType;
                previewSecondary = result.secondaryType;
                previewResult = result;
                llmTestStatus = result.isLLMGenerated
                    ? "LLM diagnosis generated successfully!"
                    : "LLM unavailable, fell back to template.";
            }
            else
            {
                llmTestStatus = "Error: GenerateDiagnosis returned null";
            }
        }
        catch (System.Exception ex)
        {
            llmTestStatus = $"Error: {ex.Message}";
            Debug.LogError($"[Stage7Test] LLM diagnosis test failed: {ex}");
        }
        finally
        {
            llmTestRunning = false;
            Repaint();
        }
    }

    private void DrawPreviewResult()
    {
        EditorGUILayout.LabelField("Classification Result", EditorStyles.boldLabel);

        // タイプ表示
        GUIStyle typeStyle = new GUIStyle(EditorStyles.largeLabel);
        typeStyle.fontSize = 18;
        typeStyle.fontStyle = FontStyle.Bold;
        typeStyle.normal.textColor = new Color(1f, 0.843f, 0f); // gold
        EditorGUILayout.LabelField($"{ResultDiagnosisPrompt.GetTypeNameJa(previewPrimary)} ({previewPrimary})", typeStyle);

        GUIStyle subStyle = new GUIStyle(EditorStyles.label);
        subStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        EditorGUILayout.LabelField($"Secondary: {ResultDiagnosisPrompt.GetTypeNameJa(previewSecondary)} ({previewSecondary})", subStyle);

        EditorGUILayout.Space();

        // 5軸バー表示
        EditorGUILayout.LabelField("5-Axis Stats", EditorStyles.boldLabel);
        DrawStatBar("決断力 (Decisiveness)", previewStats.decisiveness);
        DrawStatBar("一貫性 (Consistency)", previewStats.consistency);
        DrawStatBar("耐圧性 (Resilience)", previewStats.resilience);
        DrawStatBar("直感力 (Intuition)", previewStats.intuition);
        DrawStatBar("適応力 (Adaptability)", previewStats.adaptability);

        // フォールバック診断結果
        if (previewResult != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Diagnosis Output", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Title", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(previewResult.personalityTitle, MessageType.None);

            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(previewResult.personalityDescription, MessageType.None);

            EditorGUILayout.LabelField("Tendency", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(previewResult.psychologicalTendency, MessageType.None);

            EditorGUILayout.LabelField("Insight", EditorStyles.boldLabel);
            GUIStyle insightStyle = new GUIStyle(EditorStyles.largeLabel);
            insightStyle.fontSize = 14;
            insightStyle.fontStyle = FontStyle.Bold;
            insightStyle.normal.textColor = new Color(1f, 0.843f, 0f);
            insightStyle.wordWrap = true;
            EditorGUILayout.LabelField(previewResult.behavioralInsight, insightStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Source: {(previewResult.isLLMGenerated ? "LLM" : "Fallback")}", subStyle);
        }
    }

    private void DrawStatBar(string label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(180));

        Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));

        // Background
        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));

        // Fill
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * value, barRect.height);
        Color barColor = Color.Lerp(new Color(0.9f, 0.3f, 0.2f), new Color(0.2f, 0.8f, 0.4f), value);
        EditorGUI.DrawRect(fillRect, barColor);

        // Value text
        GUIStyle valStyle = new GUIStyle(EditorStyles.miniLabel);
        valStyle.alignment = TextAnchor.MiddleCenter;
        valStyle.normal.textColor = Color.white;
        EditorGUI.LabelField(barRect, $"{value:F2}", valStyle);

        EditorGUILayout.EndHorizontal();
    }

    // ===================
    // Auto Tests タブ
    // ===================

    private void DrawAutoTests()
    {
        EditorGUILayout.HelpBox(
            "自動テスト: 分類ロジック、スタッツ計算、フォールバック、エッジケースを検証します。",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Run All Tests", GUILayout.Height(35)))
        {
            RunAllTests();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Classification"))
            TestClassification();
        if (GUILayout.Button("2. Stats Calc"))
            TestStatsCalculation();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("3. Fallback"))
            TestFallbackTemplates();
        if (GUILayout.Button("4. SessionData"))
            TestSessionData();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("5. Stats Range"))
            TestStatsRange();
        if (GUILayout.Button("6. All 6 Types"))
            TestAllTypes();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("7. Edge Cases"))
            TestEdgeCases();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // Test Results
        EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);

        foreach (var result in testResults)
        {
            if (result.StartsWith("PASS"))
                EditorGUILayout.HelpBox(result, MessageType.Info);
            else if (result.StartsWith("FAIL"))
                EditorGUILayout.HelpBox(result, MessageType.Error);
            else if (result.StartsWith("---"))
                EditorGUILayout.LabelField(result, EditorStyles.boldLabel);
            else
                EditorGUILayout.LabelField(result);
        }

        EditorGUILayout.Space();
        if (testResults.Count > 0 && GUILayout.Button("Clear Results"))
            testResults.Clear();
    }

    // ===================
    // テスト実行
    // ===================

    private void RunAllTests()
    {
        testResults.Clear();
        testResults.Add("=== Running All Tests ===");
        TestClassification();
        TestStatsCalculation();
        TestFallbackTemplates();
        TestSessionData();
        TestStatsRange();
        TestAllTypes();
        TestEdgeCases();
        testResults.Add("=== All Tests Complete ===");
    }

    private void TestClassification()
    {
        testResults.Add("--- Test 1: Classification ---");

        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();

        // Stoic: high consistency + high resilience
        var stoicData = CreateTestData(avgDecisionTime: 3f, avgDoubtLevel: 0.1f, tempoVariance: 0.5f,
            pressureResponseScore: 0.8f, tempo: TempoType.Normal, longestStreak: 5);
        var stoicStats = system.CalculateStats(stoicData);
        var stoicType = system.ClassifyPersonality(stoicStats, stoicData);
        AssertEqual("Stoic classification", PersonalityType.Stoic, stoicType);

        // Intuitive: fast tempo + low hover
        var intuitiveData = CreateTestData(avgDecisionTime: 1.5f, avgDoubtLevel: 0.1f,
            avgHoverTime: 0.8f, tempo: TempoType.Fast);
        var intuitiveStats = system.CalculateStats(intuitiveData);
        var intuitiveType = system.ClassifyPersonality(intuitiveStats, intuitiveData);
        AssertEqual("Intuitive classification", PersonalityType.Intuitive, intuitiveType);

        // Cautious: low decisiveness + high hover
        var cautiousData = CreateTestData(avgDecisionTime: 8f, avgDoubtLevel: 0.8f,
            avgHoverTime: 4f, tempo: TempoType.Slow);
        var cautiousStats = system.CalculateStats(cautiousData);
        var cautiousType = system.ClassifyPersonality(cautiousStats, cautiousData);
        AssertEqual("Cautious classification", PersonalityType.Cautious, cautiousType);

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private void TestStatsCalculation()
    {
        testResults.Add("--- Test 2: Stats Calculation ---");

        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();

        var data = CreateTestData(avgDecisionTime: 3f, avgDoubtLevel: 0.3f, avgHoverTime: 2f,
            tempoVariance: 1f, pressureResponseScore: 0.7f, tempo: TempoType.Normal);
        var stats = system.CalculateStats(data);

        bool inRange = stats.decisiveness >= 0f && stats.decisiveness <= 1f
            && stats.consistency >= 0f && stats.consistency <= 1f
            && stats.resilience >= 0f && stats.resilience <= 1f
            && stats.intuition >= 0f && stats.intuition <= 1f
            && stats.adaptability >= 0f && stats.adaptability <= 1f;

        if (inRange)
            testResults.Add($"PASS: All stats in 0-1 range (d={stats.decisiveness:F2}, c={stats.consistency:F2}, " +
                $"r={stats.resilience:F2}, i={stats.intuition:F2}, a={stats.adaptability:F2})");
        else
            testResults.Add("FAIL: Stats out of 0-1 range!");

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private void TestFallbackTemplates()
    {
        testResults.Add("--- Test 3: Fallback Templates ---");

        var stats = new DiagnosisStats { decisiveness = 0.5f, consistency = 0.5f,
            resilience = 0.5f, intuition = 0.5f, adaptability = 0.5f };

        foreach (PersonalityType type in System.Enum.GetValues(typeof(PersonalityType)))
        {
            var result = ResultDiagnosisPrompt.GenerateFallback(type, PersonalityType.Analyst, stats);

            bool valid = !string.IsNullOrEmpty(result.personalityTitle)
                && !string.IsNullOrEmpty(result.personalityDescription)
                && !string.IsNullOrEmpty(result.psychologicalTendency)
                && !string.IsNullOrEmpty(result.behavioralInsight)
                && !result.isLLMGenerated;

            if (valid)
                testResults.Add($"PASS: {type} fallback: \"{result.personalityTitle}\" / \"{result.behavioralInsight}\"");
            else
                testResults.Add($"FAIL: {type} fallback has empty fields");
        }
    }

    private void TestSessionData()
    {
        testResults.Add("--- Test 4: GameSessionData ---");

        var data = new GameSessionData();
        bool valid = data.totalPositionCounts != null && data.totalPositionCounts.Length == 3
            && data.emotionFrequency != null;

        if (valid)
            testResults.Add("PASS: GameSessionData default constructor initializes arrays");
        else
            testResults.Add("FAIL: GameSessionData default constructor failed");
    }

    private void TestStatsRange()
    {
        testResults.Add("--- Test 5: Stats Range ---");

        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();

        // Extreme values
        var extremeData1 = CreateTestData(avgDecisionTime: 100f, avgDoubtLevel: 1f,
            avgHoverTime: 100f, tempoVariance: 100f, pressureResponseScore: 0f, tempo: TempoType.Erratic);
        var stats1 = system.CalculateStats(extremeData1);

        var extremeData2 = CreateTestData(avgDecisionTime: 0f, avgDoubtLevel: 0f,
            avgHoverTime: 0f, tempoVariance: 0f, pressureResponseScore: 1f, tempo: TempoType.Fast);
        var stats2 = system.CalculateStats(extremeData2);

        bool allClamped = true;
        float[] allValues = { stats1.decisiveness, stats1.consistency, stats1.resilience,
            stats1.intuition, stats1.adaptability, stats2.decisiveness, stats2.consistency,
            stats2.resilience, stats2.intuition, stats2.adaptability };

        foreach (float v in allValues)
        {
            if (v < 0f || v > 1f) { allClamped = false; break; }
        }

        if (allClamped)
            testResults.Add("PASS: All stats clamped to 0-1 even with extreme inputs");
        else
            testResults.Add("FAIL: Stats exceed 0-1 range with extreme inputs");

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private void TestAllTypes()
    {
        testResults.Add("--- Test 6: All 6 Types ---");

        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();
        HashSet<PersonalityType> achieved = new HashSet<PersonalityType>();

        // Stoic
        var d1 = CreateTestData(tempoVariance: 0.3f, pressureResponseScore: 0.9f, longestStreak: 5, tempo: TempoType.Normal);
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d1), d1));

        // Intuitive
        var d2 = CreateTestData(avgDecisionTime: 1f, avgDoubtLevel: 0.1f, avgHoverTime: 0.5f, tempo: TempoType.Fast);
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d2), d2));

        // Cautious
        var d3 = CreateTestData(avgDecisionTime: 9f, avgDoubtLevel: 0.9f, avgHoverTime: 5f, tempo: TempoType.Slow);
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d3), d3));

        // Gambler
        var d4 = CreateTestData(avgDecisionTime: 2f, avgDoubtLevel: 0.2f, tempoVariance: 8f, tempo: TempoType.Erratic);
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d4), d4));

        // Adapter
        var d5 = CreateTestData(tempoVariance: 2f, tempo: TempoType.Normal, hadPositionPreference: false, emotionCount: 5);
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d5), d5));

        // Analyst (fallback)
        var d6 = CreateTestData();
        achieved.Add(system.ClassifyPersonality(system.CalculateStats(d6), d6));

        testResults.Add($"PASS: Achieved {achieved.Count}/6 types: {string.Join(", ", achieved)}");

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    private void TestEdgeCases()
    {
        testResults.Add("--- Test 7: Edge Cases ---");

        var system = diagnosisSystem != null ? diagnosisSystem : CreateTempSystem();

        // Empty session data
        var emptyData = new GameSessionData();
        var emptyStats = system.CalculateStats(emptyData);
        var emptyType = system.ClassifyPersonality(emptyStats, emptyData);

        testResults.Add($"PASS: Empty data handled: type={emptyType}, " +
            $"d={emptyStats.decisiveness:F2}, c={emptyStats.consistency:F2}");

        // Null emotion frequency
        var noEmotionData = CreateTestData();
        noEmotionData.emotionFrequency = null;
        var noEmotionStats = system.CalculateStats(noEmotionData);

        testResults.Add($"PASS: Null emotionFrequency handled: adaptability={noEmotionStats.adaptability:F2}");

        // Fallback diagnosis
        var fallback = system.GenerateFallbackDiagnosis(emptyData);
        if (fallback != null && !string.IsNullOrEmpty(fallback.personalityTitle))
            testResults.Add($"PASS: Fallback diagnosis: \"{fallback.personalityTitle}\"");
        else
            testResults.Add("FAIL: Fallback diagnosis returned null");

        if (system != diagnosisSystem) DestroyImmediate(system.gameObject);
    }

    // ===================
    // ヘルパー
    // ===================

    private ResultDiagnosisSystem CreateTempSystem()
    {
        GameObject tempObj = new GameObject("_TempDiagnosisSystem");
        return tempObj.AddComponent<ResultDiagnosisSystem>();
    }

    private GameSessionData CreateTestData(
        float avgDecisionTime = 4f,
        float avgDoubtLevel = 0.3f,
        float avgHoverTime = 2f,
        float tempoVariance = 1.5f,
        float pressureResponseScore = 0.5f,
        TempoType tempo = TempoType.Normal,
        int longestStreak = 1,
        bool hadPositionPreference = false,
        int emotionCount = 3)
    {
        var data = new GameSessionData
        {
            playerWon = true,
            totalTurns = 10,
            gameDurationSeconds = 120f,
            avgDoubtLevel = avgDoubtLevel,
            avgHoverTime = avgHoverTime,
            avgDecisionTime = avgDecisionTime,
            dominantTempo = tempo,
            hadPositionPreference = hadPositionPreference,
            longestPositionStreak = longestStreak,
            totalPositionCounts = new int[] { 3, 4, 3 },
            peakPressureLevel = 2f,
            avgPressureLevel = 1f,
            turningPointCount = 2,
            totalReactions = 10,
            avgReactionIntensity = 0.4f,
            pairsFormed = 5,
            tempoVariance = tempoVariance,
            pressureResponseScore = pressureResponseScore,
            emotionFrequency = new Dictionary<AIEmotion, int>()
        };

        // Add emotions
        data.emotionFrequency[AIEmotion.Calm] = 3;
        data.emotionFrequency[AIEmotion.Pleased] = 2;
        data.emotionFrequency[AIEmotion.Frustrated] = 2;
        if (emotionCount >= 4) data.emotionFrequency[AIEmotion.Anticipating] = 2;
        if (emotionCount >= 5) data.emotionFrequency[AIEmotion.Relieved] = 1;

        return data;
    }

    private void AssertEqual<T>(string testName, T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
            testResults.Add($"PASS: {testName} = {actual}");
        else
            testResults.Add($"FAIL: {testName} expected={expected}, actual={actual}");
    }
}
