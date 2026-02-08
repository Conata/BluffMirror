using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

/// <summary>
/// オーディオシステムの自動セットアップ
/// メニュー: Tools → Setup Audio System
/// </summary>
public class AudioSystemSetup : EditorWindow
{
    private static readonly string VOICE_PATH = "Assets/Audio/Voice";
    private static readonly string SFX_PATH = "Assets/Audio/SFX";
    private static readonly string HEARTBEAT_PATH = "Assets/Music/Heartbeat";

    [MenuItem("Tools/Setup Audio System")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioSystemSetup>("Audio Setup");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private Vector2 scrollPosition;
    private bool setupComplete = false;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Audio System Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("このツールは以下を自動的にセットアップします:\n" +
                                "1. GameSettings オブジェクトの作成\n" +
                                "2. AudioManager への音声ファイル割り当て\n" +
                                "3. 必要なコンポーネントの確認", MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("🚀 自動セットアップを実行", GUILayout.Height(40)))
        {
            RunAutoSetup();
        }

        GUILayout.Space(10);

        if (setupComplete)
        {
            EditorGUILayout.HelpBox("✓ セットアップが完了しました！", MessageType.Info);
        }

        GUILayout.Space(20);
        EditorGUILayout.LabelField("個別セットアップ", EditorStyles.boldLabel);

        if (GUILayout.Button("GameSettings オブジェクトを作成"))
        {
            CreateGameSettings();
        }

        if (GUILayout.Button("AudioManager に音声ファイルを割り当て"))
        {
            AssignAudioClipsToAudioManager();
        }

        if (GUILayout.Button("言語切り替えボタンを作成（StartScene）"))
        {
            CreateLanguageButton();
        }

        if (GUILayout.Button("フォルダ構造を確認"))
        {
            CheckFolderStructure();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 自動セットアップを実行
    /// </summary>
    private void RunAutoSetup()
    {
        Debug.Log("=== Audio System Auto Setup Started ===");

        // 1. フォルダ構造を確認
        CheckFolderStructure();

        // 2. GameSettings を作成
        CreateGameSettings();

        // 3. AudioManager に音声ファイルを割り当て
        AssignAudioClipsToAudioManager();

        // 4. 言語切り替えボタンを作成
        CreateLanguageButton();

        setupComplete = true;
        Debug.Log("=== Audio System Auto Setup Completed ===");

        EditorUtility.DisplayDialog("Setup Complete",
            "オーディオシステムのセットアップが完了しました！\n\n" +
            "完了した項目:\n" +
            "✓ GameSettings オブジェクト作成\n" +
            "✓ AudioManager 音声ファイル割り当て\n" +
            "✓ 言語切り替えボタン作成（右上）\n\n" +
            "次のステップ:\n" +
            "Play Mode でテストしてください",
            "OK");
    }

    /// <summary>
    /// GameSettings オブジェクトを作成
    /// </summary>
    private static void CreateGameSettings()
    {
        // 既存の GameSettings を検索
        GameSettings existingSettings = FindObjectOfType<GameSettings>();

        if (existingSettings != null)
        {
            Debug.Log("[Setup] GameSettings は既に存在します。");
            EditorUtility.DisplayDialog("GameSettings", "GameSettings は既にシーンに存在します。", "OK");
            return;
        }

        // 新しい GameSettings オブジェクトを作成
        GameObject settingsObject = new GameObject("GameSettings");
        settingsObject.AddComponent<GameSettings>();

        // シーンに登録
        Undo.RegisterCreatedObjectUndo(settingsObject, "Create GameSettings");
        Selection.activeGameObject = settingsObject;

        Debug.Log("[Setup] GameSettings オブジェクトを作成しました。");
        EditorUtility.DisplayDialog("GameSettings Created", "GameSettings オブジェクトが作成されました。", "OK");
    }

    /// <summary>
    /// AudioManager に音声ファイルを自動割り当て
    /// </summary>
    private static void AssignAudioClipsToAudioManager()
    {
        // AudioManager を検索
        AudioManager audioManager = FindObjectOfType<AudioManager>();

        if (audioManager == null)
        {
            Debug.LogError("[Setup] AudioManager が見つかりません。シーンに AudioManager を追加してください。");
            EditorUtility.DisplayDialog("Error", "AudioManager が見つかりません。\nシーンに AudioManager を追加してください。", "OK");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(audioManager);

        // === AI Voice Clips (English) ===
        AssignVoiceClips(serializedManager, "gameStartVoices_EN", VOICE_PATH, new[] { "game_start_1", "game_start_2" });
        AssignVoiceClips(serializedManager, "cardDrawVoices_EN", VOICE_PATH, new[] { "card_draw_1", "card_draw_2", "card_draw_3" });
        AssignVoiceClips(serializedManager, "pairMatchVoices_EN", VOICE_PATH, new[] { "pair_match_1", "pair_match_2" });
        AssignVoiceClips(serializedManager, "victoryVoices_EN", VOICE_PATH, new[] { "victory_1", "victory_2" });
        AssignVoiceClips(serializedManager, "defeatVoices_EN", VOICE_PATH, new[] { "defeat_1", "defeat_2" });
        AssignVoiceClips(serializedManager, "pressureVoices_EN", VOICE_PATH, new[] { "pressure_1", "pressure_2", "pressure_3" });

        // === AI Voice Clips (Japanese) ===
        AssignVoiceClips(serializedManager, "gameStartVoices_JA", VOICE_PATH, new[] { "game_start_1_ja", "game_start_2_ja" });
        AssignVoiceClips(serializedManager, "cardDrawVoices_JA", VOICE_PATH, new[] { "card_draw_1_ja", "card_draw_2_ja", "card_draw_3_ja" });
        AssignVoiceClips(serializedManager, "pairMatchVoices_JA", VOICE_PATH, new[] { "pair_match_1_ja", "pair_match_2_ja" });
        AssignVoiceClips(serializedManager, "victoryVoices_JA", VOICE_PATH, new[] { "victory_1_ja", "victory_2_ja" });
        AssignVoiceClips(serializedManager, "defeatVoices_JA", VOICE_PATH, new[] { "defeat_1_ja", "defeat_2_ja" });
        AssignVoiceClips(serializedManager, "pressureVoices_JA", VOICE_PATH, new[] { "pressure_1_ja", "pressure_2_ja", "pressure_3_ja" });

        // === Card Sound Effects ===
        AssignSingleClip(serializedManager, "cardHoverSound", SFX_PATH, "card_hover");
        AssignSingleClip(serializedManager, "cardPickSound", SFX_PATH, "card_pick");
        AssignSingleClip(serializedManager, "cardPlaceSound", SFX_PATH, "card_place");
        AssignVoiceClips(serializedManager, "cardFlipSounds", SFX_PATH, new[] { "card_flip_1", "card_flip_2", "card_flip_3" });

        // === Environment Sounds ===
        AssignSingleClip(serializedManager, "roomAmbienceSound", SFX_PATH, "room_ambience");
        AssignSingleClip(serializedManager, "feltSlideSound", SFX_PATH, "felt_slide");

        // === Psychology Sound Effects ===
        AssignSingleClip(serializedManager, "heartbeatNormalSound", HEARTBEAT_PATH, "11L-heartbeat-33485568");
        AssignSingleClip(serializedManager, "heartbeatIntenseSound", HEARTBEAT_PATH, "11L-heartbeat-40282434");
        AssignSingleClip(serializedManager, "whisperAmbienceSound", SFX_PATH, "whisper_ambience");

        serializedManager.ApplyModifiedProperties();

        Debug.Log("[Setup] AudioManager に音声ファイルを割り当てました。");
        EditorUtility.DisplayDialog("Audio Clips Assigned", "AudioManager に音声ファイルが割り当てられました。", "OK");
    }

    /// <summary>
    /// 音声クリップ配列を割り当て
    /// </summary>
    private static void AssignVoiceClips(SerializedObject serializedObject, string propertyName, string folderPath, string[] fileNames)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"[Setup] プロパティが見つかりません: {propertyName}");
            return;
        }

        property.arraySize = fileNames.Length;

        for (int i = 0; i < fileNames.Length; i++)
        {
            string assetPath = $"{folderPath}/{fileNames[i]}.mp3";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip != null)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                Debug.Log($"[Setup] ✓ {propertyName}[{i}] = {fileNames[i]}.mp3");
            }
            else
            {
                Debug.LogWarning($"[Setup] ✗ 音声ファイルが見つかりません: {assetPath}");
            }
        }
    }

    /// <summary>
    /// 単一の音声クリップを割り当て
    /// </summary>
    private static void AssignSingleClip(SerializedObject serializedObject, string propertyName, string folderPath, string fileName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"[Setup] プロパティが見つかりません: {propertyName}");
            return;
        }

        string assetPath = $"{folderPath}/{fileName}.mp3";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

        if (clip != null)
        {
            property.objectReferenceValue = clip;
            Debug.Log($"[Setup] ✓ {propertyName} = {fileName}.mp3");
        }
        else
        {
            Debug.LogWarning($"[Setup] ✗ 音声ファイルが見つかりません: {assetPath}");
        }
    }

    /// <summary>
    /// 言語切り替えボタンを作成（StartScene用）
    /// </summary>
    private static void CreateLanguageButton()
    {
        Debug.Log("[Setup] Creating Language Button...");

        // Canvas を検索または作成
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            Debug.Log("[Setup] Canvas created.");
        }

        // 既存の LanguageButton を検索
        Transform existingButton = canvas.transform.Find("LanguageButton");
        if (existingButton != null)
        {
            Debug.Log("[Setup] LanguageButton already exists.");
            EditorUtility.DisplayDialog("Language Button", "言語切り替えボタンは既にシーンに存在します。", "OK");
            return;
        }

        // Button を作成
        GameObject buttonObject = new GameObject("LanguageButton");
        buttonObject.transform.SetParent(canvas.transform, false);

        // RectTransform の設定（右上に配置）
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1); // 右上アンカー
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-20, -20); // 右上から少しオフセット
        buttonRect.sizeDelta = new Vector2(150, 50);

        // Image コンポーネント（ボタンの背景）
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Button コンポーネント
        Button button = buttonObject.AddComponent<Button>();

        // テキストオブジェクトを作成
        GameObject textObject = new GameObject("Text (TMP)");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TMP_Text buttonText = textObject.AddComponent<TextMeshProUGUI>();
        buttonText.text = "English";
        buttonText.fontSize = 18;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        // LanguageSwitcher スクリプトをアタッチ
        LanguageSwitcher switcher = buttonObject.AddComponent<LanguageSwitcher>();

        // SerializedObject を使って参照を設定
        SerializedObject serializedSwitcher = new SerializedObject(switcher);
        serializedSwitcher.FindProperty("switchButton").objectReferenceValue = button;
        serializedSwitcher.FindProperty("buttonLabel").objectReferenceValue = buttonText;
        serializedSwitcher.FindProperty("englishText").stringValue = "English";
        serializedSwitcher.FindProperty("japaneseText").stringValue = "日本語";
        serializedSwitcher.ApplyModifiedProperties();

        // Undo 登録
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create Language Button");
        Selection.activeGameObject = buttonObject;

        Debug.Log("[Setup] LanguageButton created successfully.");
        EditorUtility.DisplayDialog("Language Button Created",
            "言語切り替えボタンが作成されました。\n\n" +
            "場所: Canvas/LanguageButton (右上)",
            "OK");
    }

    /// <summary>
    /// フォルダ構造を確認
    /// </summary>
    private static void CheckFolderStructure()
    {
        Debug.Log("=== Checking Folder Structure ===");

        CheckFolder(VOICE_PATH, "AI Voice");
        CheckFolder(SFX_PATH, "Sound Effects");
        CheckFolder(HEARTBEAT_PATH, "Heartbeat");

        Debug.Log("=== Folder Check Complete ===");
    }

    /// <summary>
    /// フォルダの存在を確認
    /// </summary>
    private static void CheckFolder(string path, string displayName)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            var files = Directory.GetFiles(path, "*.mp3");
            Debug.Log($"[Setup] ✓ {displayName} フォルダ: {path} ({files.Length} files)");
        }
        else
        {
            Debug.LogWarning($"[Setup] ✗ {displayName} フォルダが見つかりません: {path}");
        }
    }
}
