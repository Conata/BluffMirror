using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// BluffActionUIの自動セットアップ
/// Tools > Baba > Setup BluffAction UI から実行
/// </summary>
public class BluffActionUIAutoSetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup BluffAction UI")]
    public static void ShowWindow()
    {
        var window = GetWindow<BluffActionUIAutoSetup>("BluffAction UI Setup");
        window.minSize = new Vector2(400, 250);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BluffAction UI Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "BluffActionUIを自動セットアップします。\n" +
            "画面右端にブラフアクションボタンパネルを作成します。\n" +
            "(Shuffle / Push-Pull / Wiggle / Spread-Close)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup BluffAction UI", GUILayout.Height(40)))
        {
            SetupBluffActionUI();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "セットアップ後、Inspectorで以下を確認：\n" +
            "- BluffActionUI: ボタン参照が正しく設定されているか\n" +
            "- BluffActionSystem: playerHand / aiHand が設定されているか",
            MessageType.Info);
    }

    private void SetupBluffActionUI()
    {
        if (!EditorUtility.DisplayDialog("BluffAction UI Setup",
            "BluffActionUIをセットアップします。\n既存の設定は上書きされます。\n\n続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup BluffAction UI");

        try
        {
            GameObject bluffUIObj = SetupBluffActionUIGameObject();
            SetupBluffActionSystem();

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success",
                "BluffActionUIのセットアップが完了しました！\n\n" +
                "作成されたオブジェクト:\n" +
                "- BluffActionUI (Screen Space Overlay)\n" +
                "- BluffActionSystem (Coordinator)\n" +
                "- 4 Action Buttons + Cancel Button\n",
                "OK");

            Debug.Log("[BluffActionUIAutoSetup] Setup completed!");
            Selection.activeGameObject = bluffUIObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BluffActionUIAutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"エラーが発生しました:\n{e.Message}", "OK");
        }
    }

    private GameObject SetupBluffActionUIGameObject()
    {
        var existing = FindObjectOfType<BluffActionUI>();
        GameObject bluffUIObj;

        if (existing != null)
        {
            bluffUIObj = existing.gameObject;
            Debug.Log("[BluffActionUIAutoSetup] Found existing BluffActionUI");
        }
        else
        {
            bluffUIObj = new GameObject("BluffActionUI");
            Undo.RegisterCreatedObjectUndo(bluffUIObj, "Create BluffActionUI");
        }

        // Canvas
        Canvas canvas = bluffUIObj.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(bluffUIObj);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        // RectTransform scale確保 (ScreenSpaceOverlayでも必要)
        RectTransform rectTransform = bluffUIObj.GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = bluffUIObj.AddComponent<RectTransform>();
        Undo.RecordObject(rectTransform, "Set Canvas Scale");
        rectTransform.localScale = Vector3.one;

        // CanvasScaler
        CanvasScaler canvasScaler = bluffUIObj.GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = Undo.AddComponent<CanvasScaler>(bluffUIObj);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster
        if (bluffUIObj.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(bluffUIObj);

        // Button Panel
        GameObject buttonPanel = CreateButtonPanel(bluffUIObj.transform);

        // Selection Overlay
        GameObject selectionOverlay = CreateSelectionOverlay(bluffUIObj.transform);

        // BluffActionUIコンポーネント
        var bluffUIComponent = bluffUIObj.GetComponent<BluffActionUI>();
        if (bluffUIComponent == null)
            bluffUIComponent = Undo.AddComponent<BluffActionUI>(bluffUIObj);

        // SerializedObjectで設定
        SerializedObject so = new SerializedObject(bluffUIComponent);
        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("buttonPanel").objectReferenceValue = buttonPanel;

        so.FindProperty("shuffleButton").objectReferenceValue =
            buttonPanel.transform.Find("ShuffleButton")?.GetComponent<Button>();
        so.FindProperty("pushPullButton").objectReferenceValue =
            buttonPanel.transform.Find("PushPullButton")?.GetComponent<Button>();
        so.FindProperty("wiggleButton").objectReferenceValue =
            buttonPanel.transform.Find("WiggleButton")?.GetComponent<Button>();
        so.FindProperty("spreadCloseButton").objectReferenceValue =
            buttonPanel.transform.Find("SpreadCloseButton")?.GetComponent<Button>();
        so.FindProperty("cancelButton").objectReferenceValue =
            buttonPanel.transform.Find("CancelButton")?.GetComponent<Button>();

        so.FindProperty("shuffleLabel").objectReferenceValue =
            buttonPanel.transform.Find("ShuffleButton/Text")?.GetComponent<TMP_Text>();
        so.FindProperty("pushPullLabel").objectReferenceValue =
            buttonPanel.transform.Find("PushPullButton/Text")?.GetComponent<TMP_Text>();
        so.FindProperty("wiggleLabel").objectReferenceValue =
            buttonPanel.transform.Find("WiggleButton/Text")?.GetComponent<TMP_Text>();
        so.FindProperty("spreadCloseLabel").objectReferenceValue =
            buttonPanel.transform.Find("SpreadCloseButton/Text")?.GetComponent<TMP_Text>();

        so.FindProperty("selectionOverlay").objectReferenceValue = selectionOverlay;
        so.FindProperty("selectionPromptText").objectReferenceValue =
            selectionOverlay.transform.Find("PromptText")?.GetComponent<TMP_Text>();

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bluffUIComponent);

        return bluffUIObj;
    }

    private void SetupBluffActionSystem()
    {
        var existing = FindObjectOfType<BluffActionSystem>();
        if (existing != null) return; // 既存あればスキップ

        GameObject systemObj = new GameObject("BluffActionSystem");
        Undo.RegisterCreatedObjectUndo(systemObj, "Create BluffActionSystem");

        var system = Undo.AddComponent<BluffActionSystem>(systemObj);

        // PlayerHandController / AIHandController を探して設定
        SerializedObject so = new SerializedObject(system);
        var playerHand = FindObjectOfType<PlayerHandController>();
        var aiHand = FindObjectOfType<AIHandController>();

        if (playerHand != null)
            so.FindProperty("playerHand").objectReferenceValue = playerHand;
        if (aiHand != null)
            so.FindProperty("aiHand").objectReferenceValue = aiHand;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(system);
    }

    private GameObject CreateButtonPanel(Transform parent)
    {
        Transform existingPanel = parent.Find("ButtonPanel");
        if (existingPanel != null) return existingPanel.gameObject;

        // Button Panel — 画面右端、縦並び
        GameObject buttonPanel = new GameObject("ButtonPanel");
        Undo.RegisterCreatedObjectUndo(buttonPanel, "Create Button Panel");
        buttonPanel.transform.SetParent(parent, false);

        RectTransform panelRect = buttonPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.92f, 0.3f);
        panelRect.anchorMax = new Vector2(0.99f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        buttonPanel.AddComponent<CanvasGroup>();

        // 背景
        Image panelBg = buttonPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.7f);

        // VerticalLayoutGroup
        var layout = buttonPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(6, 6, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // 4 Action Buttons + Cancel
        Color blueGray = new Color(0.2f, 0.25f, 0.35f);
        CreateActionButton("ShuffleButton", "Shuffle", blueGray, buttonPanel.transform);
        CreateActionButton("PushPullButton", "Push", blueGray, buttonPanel.transform);
        CreateActionButton("WiggleButton", "Wiggle", blueGray, buttonPanel.transform);
        CreateActionButton("SpreadCloseButton", "Spread", blueGray, buttonPanel.transform);

        // Cancel button (初期非表示)
        GameObject cancelObj = CreateActionButton("CancelButton", "Cancel",
            new Color(0.5f, 0.15f, 0.15f), buttonPanel.transform);
        cancelObj.SetActive(false);

        return buttonPanel;
    }

    private GameObject CreateActionButton(string name, string text, Color color, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(buttonObj, $"Create {name}");
        buttonObj.transform.SetParent(parent, false);

        buttonObj.AddComponent<RectTransform>();

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        var colors = button.colors;
        colors.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
        button.colors = colors;

        // Text
        GameObject textObj = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(textObj, $"Create {name} Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 16;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return buttonObj;
    }

    private GameObject CreateSelectionOverlay(Transform parent)
    {
        Transform existingOverlay = parent.Find("SelectionOverlay");
        if (existingOverlay != null) return existingOverlay.gameObject;

        // 画面下部のカード選択プロンプト
        GameObject overlay = new GameObject("SelectionOverlay");
        Undo.RegisterCreatedObjectUndo(overlay, "Create Selection Overlay");
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.3f, 0.02f);
        overlayRect.anchorMax = new Vector2(0.7f, 0.08f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0.1f, 0.1f, 0.2f, 0.85f);

        // Prompt Text
        GameObject promptObj = new GameObject("PromptText");
        Undo.RegisterCreatedObjectUndo(promptObj, "Create Prompt Text");
        promptObj.transform.SetParent(overlay.transform, false);

        RectTransform promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = Vector2.zero;
        promptRect.anchorMax = Vector2.one;
        promptRect.offsetMin = new Vector2(10, 0);
        promptRect.offsetMax = new Vector2(-10, 0);

        TMP_Text promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "Select a card";
        promptText.fontSize = 18;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(1f, 0.85f, 0.4f);

        overlay.SetActive(false);

        return overlay;
    }
}
