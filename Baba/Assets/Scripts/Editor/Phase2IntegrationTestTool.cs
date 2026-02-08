using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Phase2 統合テストツール
/// メニュー: Tools → Phase2 Integration Test
/// </summary>
public class Phase2IntegrationTestTool : EditorWindow
{
    [MenuItem("Tools/Phase2 Integration Test")]
    public static void ShowWindow()
    {
        var window = GetWindow<Phase2IntegrationTestTool>("Phase2 Test");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }

    private Vector2 scrollPosition;
    private TestResults testResults = new TestResults();
    private bool testsRun = false;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Phase2 統合テストツール", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Phase2システムの自動検証ツールです。\n" +
            "52個のテスト項目を自動実行し、セットアップの完全性を確認します。",
            MessageType.Info);

        GUILayout.Space(10);

        // 全テスト実行ボタン
        if (GUILayout.Button("🚀 すべてのテストを実行", GUILayout.Height(40)))
        {
            RunAllTests();
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("個別テスト", EditorStyles.boldLabel);

        if (GUILayout.Button("必須コンポーネント検証 (8項目)"))
        {
            testResults.Clear();
            TestRequiredComponents();
            testsRun = true;
        }

        if (GUILayout.Button("参照検証 - AudioClip等 (28項目)"))
        {
            testResults.Clear();
            TestReferences();
            testsRun = true;
        }

        if (GUILayout.Button("設定値妥当性検証 (12項目)"))
        {
            testResults.Clear();
            TestConfigurationValues();
            testsRun = true;
        }

        if (GUILayout.Button("シングルトン検証 (4項目)"))
        {
            testResults.Clear();
            TestSingletons();
            testsRun = true;
        }

        GUILayout.Space(20);

        // テスト結果表示
        if (testsRun)
        {
            DisplayTestResults();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 全テストを実行
    /// </summary>
    private void RunAllTests()
    {
        testResults.Clear();
        Debug.Log("=== Phase2 Integration Test Started ===");

        TestRequiredComponents();
        TestReferences();
        TestConfigurationValues();
        TestSingletons();

        testsRun = true;
        Debug.Log("=== Phase2 Integration Test Completed ===");

        // 結果ダイアログ
        string message = $"テスト完了！\n\n" +
                        $"成功: {testResults.PassedCount}/{testResults.TotalCount}\n" +
                        $"失敗: {testResults.FailedCount}\n" +
                        $"警告: {testResults.WarningCount}\n\n" +
                        $"成功率: {testResults.SuccessRate:F1}%";

        if (testResults.SuccessRate == 100f)
        {
            EditorUtility.DisplayDialog("✓ テスト成功", message, "OK");
        }
        else if (testResults.SuccessRate >= 80f)
        {
            EditorUtility.DisplayDialog("⚠ 警告あり", message, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("✗ エラー検出", message, "OK");
        }
    }

    /// <summary>
    /// 必須コンポーネント検証
    /// </summary>
    private void TestRequiredComponents()
    {
        testResults.AddCategory("必須コンポーネント");

        // GameSettings
        var gameSettings = FindObjectOfType<GameSettings>();
        testResults.AddTest("GameSettings", gameSettings != null,
            gameSettings == null ? "GameSettings オブジェクトが見つかりません" : null);

        // AudioManager
        var audioManager = FindObjectOfType<AudioManager>();
        testResults.AddTest("AudioManager", audioManager != null,
            audioManager == null ? "AudioManager オブジェクトが見つかりません" : null);

        // PostProcessingController
        var postProcessing = FindObjectOfType<PostProcessingController>();
        testResults.AddTest("PostProcessingController", postProcessing != null,
            postProcessing == null ? "PostProcessingController が見つかりません" : null);

        // Volume コンポーネント
        if (postProcessing != null)
        {
            var volume = postProcessing.GetComponent<Volume>();
            testResults.AddTest("PostProcessing Volume", volume != null,
                volume == null ? "Volume コンポーネントが見つかりません" : null);
        }

        // CardEffectsManager
        var cardEffects = FindObjectOfType<CardEffectsManager>();
        testResults.AddTest("CardEffectsManager", cardEffects != null,
            cardEffects == null ? "CardEffectsManager が見つかりません" : null);

        // LightingSetup
        var lighting = FindObjectOfType<LightingSetup>();
        testResults.AddTest("LightingSetup", lighting != null,
            lighting == null ? "LightingSetup が見つかりません" : null);

        // MaterialSetup
        var materials = FindObjectOfType<MaterialSetup>();
        testResults.AddTest("MaterialSetup", materials != null,
            materials == null ? "MaterialSetup が見つかりません" : null);

        // Main Camera + AudioListener
        var mainCamera = Camera.main;
        testResults.AddTest("Main Camera", mainCamera != null,
            mainCamera == null ? "Main Camera が見つかりません" : null);

        if (mainCamera != null)
        {
            var listener = mainCamera.GetComponent<AudioListener>();
            testResults.AddTest("AudioListener", listener != null,
                listener == null ? "AudioListener が Main Camera にありません" : null);
        }
    }

    /// <summary>
    /// 参照検証
    /// </summary>
    private void TestReferences()
    {
        testResults.AddCategory("参照検証");

        var audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            testResults.AddTest("AudioManager", false, "AudioManager が見つかりません");
            return;
        }

        SerializedObject so = new SerializedObject(audioManager);

        // AudioMixer 参照
        testResults.AddTest("AudioMixer",
            ValidateReference(so, "audioMixer"),
            "AudioMixer が割り当てられていません");

        // AudioMixerGroup 参照
        testResults.AddTest("SFX Group",
            ValidateReference(so, "sfxGroup"),
            "SFX Group が割り当てられていません");
        testResults.AddTest("Music Group",
            ValidateReference(so, "musicGroup"),
            "Music Group が割り当てられていません");
        testResults.AddTest("Ambience Group",
            ValidateReference(so, "ambienceGroup"),
            "Ambience Group が割り当てられていません");
        testResults.AddTest("Voice Group",
            ValidateReference(so, "voiceGroup"),
            "Voice Group が割り当てられていません");

        // 英語AI音声 (14個)
        testResults.AddTest("Game Start Voices EN",
            ValidateAudioClipArray(so, "gameStartVoices_EN", 2),
            "Game Start Voices EN が不完全です");
        testResults.AddTest("Card Draw Voices EN",
            ValidateAudioClipArray(so, "cardDrawVoices_EN", 3),
            "Card Draw Voices EN が不完全です");
        testResults.AddTest("Pair Match Voices EN",
            ValidateAudioClipArray(so, "pairMatchVoices_EN", 2),
            "Pair Match Voices EN が不完全です");
        testResults.AddTest("Victory Voices EN",
            ValidateAudioClipArray(so, "victoryVoices_EN", 2),
            "Victory Voices EN が不完全です");
        testResults.AddTest("Defeat Voices EN",
            ValidateAudioClipArray(so, "defeatVoices_EN", 2),
            "Defeat Voices EN が不完全です");
        testResults.AddTest("Pressure Voices EN",
            ValidateAudioClipArray(so, "pressureVoices_EN", 3),
            "Pressure Voices EN が不完全です");

        // 日本語AI音声 (14個)
        testResults.AddTest("Game Start Voices JA",
            ValidateAudioClipArray(so, "gameStartVoices_JA", 2),
            "Game Start Voices JA が不完全です");
        testResults.AddTest("Card Draw Voices JA",
            ValidateAudioClipArray(so, "cardDrawVoices_JA", 3),
            "Card Draw Voices JA が不完全です");
        testResults.AddTest("Pair Match Voices JA",
            ValidateAudioClipArray(so, "pairMatchVoices_JA", 2),
            "Pair Match Voices JA が不完全です");
        testResults.AddTest("Victory Voices JA",
            ValidateAudioClipArray(so, "victoryVoices_JA", 2),
            "Victory Voices JA が不完全です");
        testResults.AddTest("Defeat Voices JA",
            ValidateAudioClipArray(so, "defeatVoices_JA", 2),
            "Defeat Voices JA が不完全です");
        testResults.AddTest("Pressure Voices JA",
            ValidateAudioClipArray(so, "pressureVoices_JA", 3),
            "Pressure Voices JA が不完全です");

        // SFX (8個)
        testResults.AddTest("Card Hover Sound",
            ValidateReference(so, "cardHoverSound"),
            "Card Hover Sound が割り当てられていません");
        testResults.AddTest("Card Pick Sound",
            ValidateReference(so, "cardPickSound"),
            "Card Pick Sound が割り当てられていません");
        testResults.AddTest("Card Place Sound",
            ValidateReference(so, "cardPlaceSound"),
            "Card Place Sound が割り当てられていません");
        testResults.AddTest("Card Flip Sounds",
            ValidateAudioClipArray(so, "cardFlipSounds", 3),
            "Card Flip Sounds が不完全です");

        // 環境音 (2個)
        testResults.AddTest("Room Ambience Sound",
            ValidateReference(so, "roomAmbienceSound"),
            "Room Ambience Sound が割り当てられていません");
        testResults.AddTest("Felt Slide Sound",
            ValidateReference(so, "feltSlideSound"),
            "Felt Slide Sound が割り当てられていません");

        // 心理音 (3個)
        testResults.AddTest("Heartbeat Normal Sound",
            ValidateReference(so, "heartbeatNormalSound"),
            "Heartbeat Normal Sound が割り当てられていません");
        testResults.AddTest("Heartbeat Intense Sound",
            ValidateReference(so, "heartbeatIntenseSound"),
            "Heartbeat Intense Sound が割り当てられていません");
        testResults.AddTest("Whisper Ambience Sound",
            ValidateReference(so, "whisperAmbienceSound"),
            "Whisper Ambience Sound が割り当てられていません");
    }

    /// <summary>
    /// 設定値妥当性検証
    /// </summary>
    private void TestConfigurationValues()
    {
        testResults.AddCategory("設定値妥当性");

        // AudioManager 設定値
        var audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            SerializedObject so = new SerializedObject(audioManager);

            float masterVolume = so.FindProperty("masterVolume").floatValue;
            testResults.AddTest("Master Volume",
                masterVolume >= 0f && masterVolume <= 1f,
                masterVolume < 0f || masterVolume > 1f ? $"Master Volume が範囲外です: {masterVolume}" : null);

            float sfxVolume = so.FindProperty("sfxVolume").floatValue;
            testResults.AddTest("SFX Volume",
                sfxVolume >= 0f && sfxVolume <= 1f,
                sfxVolume < 0f || sfxVolume > 1f ? $"SFX Volume が範囲外です: {sfxVolume}" : null);

            float ambienceVolume = so.FindProperty("ambienceVolume").floatValue;
            testResults.AddTest("Ambience Volume",
                ambienceVolume >= 0f && ambienceVolume <= 1f,
                ambienceVolume < 0f || ambienceVolume > 1f ? $"Ambience Volume が範囲外です: {ambienceVolume}" : null);

            float voiceVolume = so.FindProperty("voiceVolume").floatValue;
            testResults.AddTest("Voice Volume",
                voiceVolume >= 0f && voiceVolume <= 1f,
                voiceVolume < 0f || voiceVolume > 1f ? $"Voice Volume が範囲外です: {voiceVolume}" : null);

            int cardThreshold = so.FindProperty("cardThresholdForIntenseHeartbeat").intValue;
            testResults.AddTest("Card Threshold",
                cardThreshold >= 0,
                cardThreshold < 0 ? $"Card Threshold が負の値です: {cardThreshold}" : null);
        }

        // PostProcessingController 設定値
        var postProcessing = FindObjectOfType<PostProcessingController>();
        if (postProcessing != null)
        {
            SerializedObject so = new SerializedObject(postProcessing);

            float baseVignette = so.FindProperty("baseVignetteIntensity").floatValue;
            testResults.AddTest("Base Vignette Intensity",
                baseVignette >= 0f && baseVignette <= 1f,
                baseVignette < 0f || baseVignette > 1f ? $"Base Vignette が範囲外です: {baseVignette}" : null);

            float pressureVignette = so.FindProperty("pressureVignetteIntensity").floatValue;
            testResults.AddTest("Pressure Vignette Intensity",
                pressureVignette >= 0f && pressureVignette <= 1f,
                pressureVignette < 0f || pressureVignette > 1f ? $"Pressure Vignette が範囲外です: {pressureVignette}" : null);

            float baseChromaticAberration = so.FindProperty("baseChromaticIntensity").floatValue;
            testResults.AddTest("Base Chromatic Intensity",
                baseChromaticAberration >= 0f && baseChromaticAberration <= 1f,
                baseChromaticAberration < 0f || baseChromaticAberration > 1f ? $"Base Chromatic が範囲外です: {baseChromaticAberration}" : null);

            float focusDoF = so.FindProperty("focusDoFIntensity").floatValue;
            testResults.AddTest("Focus DoF Intensity",
                focusDoF > 0f,
                focusDoF <= 0f ? $"Focus DoF Intensity が0以下です: {focusDoF}" : null);

            float focusDuration = so.FindProperty("focusDuration").floatValue;
            testResults.AddTest("Focus Duration",
                focusDuration > 0f,
                focusDuration <= 0f ? $"Focus Duration が0以下です: {focusDuration}" : null);
        }

        // GameSettings 設定値
        var gameSettings = FindObjectOfType<GameSettings>();
        if (gameSettings != null)
        {
            SerializedObject so = new SerializedObject(gameSettings);
            int languageValue = so.FindProperty("currentLanguage").enumValueIndex;
            testResults.AddTest("Current Language",
                languageValue == 0 || languageValue == 1,
                languageValue != 0 && languageValue != 1 ? $"Invalid language enum: {languageValue}" : null);
        }
    }

    /// <summary>
    /// シングルトン検証
    /// </summary>
    private void TestSingletons()
    {
        testResults.AddCategory("シングルトン検証");

        testResults.AddTest("AudioManager Singleton",
            ValidateSingletonUniqueness<AudioManager>(),
            "AudioManager の重複インスタンスが検出されました");

        testResults.AddTest("PostProcessingController Singleton",
            ValidateSingletonUniqueness<PostProcessingController>(),
            "PostProcessingController の重複インスタンスが検出されました");

        testResults.AddTest("CardEffectsManager Singleton",
            ValidateSingletonUniqueness<CardEffectsManager>(),
            "CardEffectsManager の重複インスタンスが検出されました");

        testResults.AddTest("GameSettings Singleton",
            ValidateSingletonUniqueness<GameSettings>(),
            "GameSettings の重複インスタンスが検出されました");
    }

    /// <summary>
    /// テスト結果を表示
    /// </summary>
    private void DisplayTestResults()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("テスト結果", EditorStyles.boldLabel);

        // サマリー
        GUIStyle summaryStyle = new GUIStyle(EditorStyles.helpBox);
        summaryStyle.richText = true;

        string summaryColor = testResults.SuccessRate == 100f ? "green" :
                              testResults.SuccessRate >= 80f ? "yellow" : "red";

        EditorGUILayout.TextArea(
            $"<b>Total:</b> <color={summaryColor}>{testResults.PassedCount}/{testResults.TotalCount}</color> tests passed ({testResults.SuccessRate:F1}%)\n" +
            $"<b>Success:</b> {testResults.PassedCount}  |  " +
            $"<b>Failed:</b> {testResults.FailedCount}  |  " +
            $"<b>Warnings:</b> {testResults.WarningCount}",
            summaryStyle,
            GUILayout.Height(60));

        GUILayout.Space(10);

        // 詳細結果
        foreach (var category in testResults.Categories)
        {
            EditorGUILayout.LabelField($"[{category.Name}] ({category.PassedCount}/{category.TotalCount})", EditorStyles.boldLabel);

            foreach (var test in category.Tests)
            {
                GUIStyle testStyle = new GUIStyle(EditorStyles.label);
                testStyle.richText = true;

                string icon = test.Passed ? "✓" : "✗";
                string color = test.Passed ? "green" : "red";

                EditorGUILayout.LabelField(
                    $"  <color={color}>{icon}</color> {test.Name}",
                    testStyle);

                if (!test.Passed && !string.IsNullOrEmpty(test.ErrorMessage))
                {
                    EditorGUILayout.HelpBox($"    → {test.ErrorMessage}", MessageType.Warning);
                }
            }

            GUILayout.Space(5);
        }
    }

    // ===== ヘルパーメソッド =====

    private bool ValidateReference(SerializedObject so, string propertyName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[Test] Property not found: {propertyName}");
            return false;
        }

        bool isValid = prop.objectReferenceValue != null;
        if (!isValid)
        {
            Debug.LogWarning($"[Test] {propertyName} is NULL");
        }
        return isValid;
    }

    private bool ValidateAudioClipArray(SerializedObject so, string propertyName, int expectedSize)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[Test] Property not found: {propertyName}");
            return false;
        }

        if (prop.arraySize != expectedSize)
        {
            Debug.LogWarning($"[Test] {propertyName} array size mismatch. Expected: {expectedSize}, Actual: {prop.arraySize}");
            return false;
        }

        for (int i = 0; i < expectedSize; i++)
        {
            if (prop.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                Debug.LogWarning($"[Test] {propertyName}[{i}] is NULL");
                return false;
            }
        }

        return true;
    }

    private bool ValidateSingletonUniqueness<T>() where T : MonoBehaviour
    {
        var instances = FindObjectsOfType<T>();
        if (instances.Length == 0)
        {
            Debug.LogError($"[Test] {typeof(T).Name} not found in scene");
            return false;
        }
        if (instances.Length > 1)
        {
            Debug.LogError($"[Test] Multiple {typeof(T).Name} instances detected ({instances.Length})");
            return false;
        }
        return true;
    }

    // ===== データ構造 =====

    private class TestResults
    {
        public List<TestCategory> Categories = new List<TestCategory>();
        private TestCategory currentCategory;

        public int TotalCount => Categories.Sum(c => c.TotalCount);
        public int PassedCount => Categories.Sum(c => c.PassedCount);
        public int FailedCount => Categories.Sum(c => c.FailedCount);
        public int WarningCount => Categories.Sum(c => c.WarningCount);
        public float SuccessRate => TotalCount == 0 ? 0f : (PassedCount * 100f / TotalCount);

        public void Clear()
        {
            Categories.Clear();
            currentCategory = null;
        }

        public void AddCategory(string name)
        {
            currentCategory = new TestCategory { Name = name };
            Categories.Add(currentCategory);
        }

        public void AddTest(string name, bool passed, string errorMessage = null)
        {
            if (currentCategory == null)
            {
                AddCategory("Uncategorized");
            }

            currentCategory.Tests.Add(new TestResult
            {
                Name = name,
                Passed = passed,
                ErrorMessage = errorMessage
            });
        }
    }

    private class TestCategory
    {
        public string Name;
        public List<TestResult> Tests = new List<TestResult>();

        public int TotalCount => Tests.Count;
        public int PassedCount => Tests.Count(t => t.Passed);
        public int FailedCount => Tests.Count(t => !t.Passed);
        public int WarningCount => Tests.Count(t => !t.Passed && !string.IsNullOrEmpty(t.ErrorMessage));
    }

    private class TestResult
    {
        public string Name;
        public bool Passed;
        public string ErrorMessage;
    }
}

// List 拡張メソッド
public static class ListExtensions
{
    public static int Sum<T>(this List<T> list, System.Func<T, int> selector)
    {
        int sum = 0;
        foreach (var item in list)
        {
            sum += selector(item);
        }
        return sum;
    }

    public static int Count<T>(this List<T> list, System.Func<T, bool> predicate)
    {
        int count = 0;
        foreach (var item in list)
        {
            if (predicate(item))
            {
                count++;
            }
        }
        return count;
    }
}
