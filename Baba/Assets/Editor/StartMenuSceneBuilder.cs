using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using FPSTrump.Manager;
using FPSTrump.UI;

/// <summary>
/// StartMenuScene自動構築ツール
/// メニューから実行してStartMenuSceneを自動生成
/// </summary>
public class StartMenuSceneBuilder : EditorWindow
{
    [MenuItem("FPS Trump/Build Start Menu Scene")]
    public static void BuildStartMenuScene()
    {
        // 確認ダイアログ
        if (!EditorUtility.DisplayDialog(
            "Build Start Menu Scene",
            "このツールは現在のシーンに StartMenu UI を構築します。\n続行しますか？",
            "はい", "キャンセル"))
        {
            return;
        }

        Debug.Log("=== Building Start Menu Scene ===");

        // 1. Canvas作成
        GameObject canvasObj = CreateCanvas();

        // 2. EventSystem作成
        CreateEventSystem();

        // 3. APIKeyManager作成
        GameObject apiKeyManagerObj = CreateAPIKeyManager();

        // 4. Setup Panel作成
        GameObject setupPanelObj = CreateSetupPanel(canvasObj.transform);

        // 5. Ready Panel作成
        GameObject readyPanelObj = CreateReadyPanel(canvasObj.transform);

        // 6. APIKeySetupUI スクリプト設定
        ConfigureAPIKeySetupUI(setupPanelObj, readyPanelObj);

        Debug.Log("=== Start Menu Scene Built Successfully! ===");
        Debug.Log("次のステップ: File → Save As で 'StartMenuScene' として保存してください");

        // シーンをダーティマーク（保存が必要な状態）
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        // Canvasを選択
        Selection.activeGameObject = canvasObj;
    }

    /// <summary>
    /// Canvas作成
    /// </summary>
    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("✓ Canvas created");
        return canvasObj;
    }

    /// <summary>
    /// EventSystem作成
    /// </summary>
    private static void CreateEventSystem()
    {
        // 既存のEventSystemをチェック
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            Debug.Log("✓ EventSystem already exists");
            return;
        }

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Debug.Log("✓ EventSystem created");
    }

    /// <summary>
    /// APIKeyManager作成
    /// </summary>
    private static GameObject CreateAPIKeyManager()
    {
        GameObject managerObj = new GameObject("APIKeyManager");
        managerObj.AddComponent<APIKeyManager>();

        Debug.Log("✓ APIKeyManager created");
        return managerObj;
    }

    /// <summary>
    /// Setup Panel作成
    /// </summary>
    private static GameObject CreateSetupPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("SetupPanel", typeof(RectTransform));
        panelObj.transform.SetParent(parent, false);

        // Panel背景
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        // RectTransform設定（全画面）
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Vertical Layout Group追加
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(100, 100, 100, 100);
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // 子要素作成
        CreateTitle(panelObj.transform, "FPS Trump Game - API Setup", 48);
        CreateSubtitle(panelObj.transform, "At least one API key required (Claude or OpenAI)", 20);
        GameObject claudeSection = CreateAPIKeySection(panelObj.transform, "Claude", "sk-ant-api03-...");
        GameObject openAISection = CreateAPIKeySection(panelObj.transform, "OpenAI", "sk-...");
        GameObject buttonGroup = CreateButtonGroup(panelObj.transform);
        GameObject statusText = CreateStatusText(panelObj.transform);

        // タグ付け（後で参照するため）
        claudeSection.name = "ClaudeKeySection";
        openAISection.name = "OpenAIKeySection";
        buttonGroup.name = "ButtonGroup";
        statusText.name = "StatusText";

        Debug.Log("✓ Setup Panel created");
        return panelObj;
    }

    /// <summary>
    /// Ready Panel作成
    /// </summary>
    private static GameObject CreateReadyPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("ReadyPanel", typeof(RectTransform));
        panelObj.transform.SetParent(parent, false);

        // Panel背景
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0.4f, 0, 0.8f);

        // RectTransform設定（全画面）
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Vertical Layout Group追加
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(100, 100, 200, 200);
        layout.spacing = 50;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // 子要素作成
        CreateTitle(panelObj.transform, "Ready to Play!", 60);
        GameObject readyStatusText = CreateStatusText(panelObj.transform, "All API keys loaded ✅");
        readyStatusText.name = "ReadyStatusText";

        GameObject startButton = CreateButton(panelObj.transform, "Start Game", 36);
        startButton.name = "StartGameButton";

        // 初期状態は非表示
        panelObj.SetActive(false);

        Debug.Log("✓ Ready Panel created");
        return panelObj;
    }

    /// <summary>
    /// タイトルテキスト作成
    /// </summary>
    private static GameObject CreateTitle(Transform parent, string text, float fontSize)
    {
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmpText = titleObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        RectTransform rect = titleObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 100);

        return titleObj;
    }

    /// <summary>
    /// サブタイトルテキスト作成
    /// </summary>
    private static GameObject CreateSubtitle(Transform parent, string text, float fontSize)
    {
        GameObject subtitleObj = new GameObject("Subtitle", typeof(RectTransform));
        subtitleObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmpText = subtitleObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(0.8f, 0.8f, 0.8f, 1f); // グレー

        RectTransform rect = subtitleObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 40);

        return subtitleObj;
    }

    /// <summary>
    /// APIキーセクション作成
    /// </summary>
    private static GameObject CreateAPIKeySection(Transform parent, string keyName, string placeholder)
    {
        GameObject sectionObj = new GameObject($"{keyName}KeySection", typeof(RectTransform));
        sectionObj.transform.SetParent(parent, false);

        // Vertical Layout Group
        VerticalLayoutGroup layout = sectionObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        RectTransform rect = sectionObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 150);

        // Label
        GameObject labelObj = new GameObject($"{keyName}Label", typeof(RectTransform));
        labelObj.transform.SetParent(sectionObj.transform, false);
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = $"{keyName} API Key:";
        label.fontSize = 28;
        label.color = Color.white;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(800, 40);

        // InputField
        GameObject inputObj = CreateInputField(sectionObj.transform, $"{keyName}InputField", placeholder);

        // Help Button
        GameObject helpButtonObj = CreateButton(sectionObj.transform, $"Get {keyName} API Key", 20);
        helpButtonObj.name = $"{keyName}HelpButton";

        return sectionObj;
    }

    /// <summary>
    /// InputField作成
    /// </summary>
    private static GameObject CreateInputField(Transform parent, string name, string placeholder)
    {
        // UI要素として作成（RectTransform付き）
        GameObject inputObj = new GameObject(name, typeof(RectTransform));
        inputObj.transform.SetParent(parent, false);

        // 背景Image
        Image bgImage = inputObj.AddComponent<Image>();
        bgImage.color = new Color(1, 1, 1, 0.2f);

        // TMP_InputField
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

        // Text Area（UI要素として作成）
        GameObject textAreaObj = new GameObject("Text Area", typeof(RectTransform));
        textAreaObj.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textAreaObj.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -7);

        // Placeholder（UI要素として作成）
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 24;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;

        // Text（UI要素として作成）
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(textAreaObj.transform, false);
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 24;
        inputText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // InputField設定
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(800, 50);

        return inputObj;
    }

    /// <summary>
    /// ボタングループ作成
    /// </summary>
    private static GameObject CreateButtonGroup(Transform parent)
    {
        GameObject groupObj = new GameObject("ButtonGroup", typeof(RectTransform));
        groupObj.transform.SetParent(parent, false);

        // Horizontal Layout Group
        HorizontalLayoutGroup layout = groupObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform rect = groupObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 80);

        // Save Button
        GameObject saveButton = CreateButton(groupObj.transform, "Save", 28);
        saveButton.name = "SaveButton";

        // Skip Button
        GameObject skipButton = CreateButton(groupObj.transform, "Skip (Offline Mode)", 28);
        skipButton.name = "SkipButton";

        return groupObj;
    }

    /// <summary>
    /// ボタン作成
    /// </summary>
    private static GameObject CreateButton(Transform parent, string text, float fontSize)
    {
        GameObject buttonObj = new GameObject("Button", typeof(RectTransform));
        buttonObj.transform.SetParent(parent, false);

        // Button Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        // Button Component
        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.5f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.6f, 1f);
        button.colors = colors;

        // Text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = fontSize;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(350, 60);

        return buttonObj;
    }

    /// <summary>
    /// ステータステキスト作成
    /// </summary>
    private static GameObject CreateStatusText(Transform parent, string initialText = "")
    {
        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform));
        statusObj.transform.SetParent(parent, false);

        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = initialText;
        statusText.fontSize = 24;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.yellow;

        RectTransform rect = statusObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800, 50);

        return statusObj;
    }

    /// <summary>
    /// APIKeySetupUI スクリプト設定
    /// </summary>
    private static void ConfigureAPIKeySetupUI(GameObject setupPanel, GameObject readyPanel)
    {
        APIKeySetupUI setupUI = setupPanel.AddComponent<APIKeySetupUI>();

        // SerializedObjectを使って参照を設定
        SerializedObject so = new SerializedObject(setupUI);

        // InputFields
        Transform claudeSection = setupPanel.transform.Find("ClaudeKeySection");
        Transform openAISection = setupPanel.transform.Find("OpenAIKeySection");

        TMP_InputField claudeInput = claudeSection.Find("ClaudeInputField").GetComponent<TMP_InputField>();
        TMP_InputField openAIInput = openAISection.Find("OpenAIInputField").GetComponent<TMP_InputField>();

        so.FindProperty("claudeAPIKeyInput").objectReferenceValue = claudeInput;
        so.FindProperty("openAIAPIKeyInput").objectReferenceValue = openAIInput;

        // Buttons
        Transform buttonGroup = setupPanel.transform.Find("ButtonGroup");
        Button saveButton = buttonGroup.Find("SaveButton").GetComponent<Button>();
        Button skipButton = buttonGroup.Find("SkipButton").GetComponent<Button>();

        so.FindProperty("saveButton").objectReferenceValue = saveButton;
        so.FindProperty("skipButton").objectReferenceValue = skipButton;

        // Ready Panel
        Button startGameButton = readyPanel.transform.Find("StartGameButton").GetComponent<Button>();
        TextMeshProUGUI readyStatusText = readyPanel.transform.Find("ReadyStatusText").GetComponent<TextMeshProUGUI>();

        so.FindProperty("startGameButton").objectReferenceValue = startGameButton;

        // Status Text
        TextMeshProUGUI statusText = setupPanel.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
        so.FindProperty("statusText").objectReferenceValue = statusText;

        // Panels
        so.FindProperty("setupPanel").objectReferenceValue = setupPanel;
        so.FindProperty("readyPanel").objectReferenceValue = readyPanel;

        // Game Scene Name
        so.FindProperty("gameSceneName").stringValue = "FPS_Trump_Scene";

        so.ApplyModifiedProperties();

        Debug.Log("✓ APIKeySetupUI configured");
    }
}
