using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// ConfirmUIの自動セットアップ（Screen Space - Overlay版）
/// Tools > Baba > Setup ConfirmUI System から実行
/// </summary>
public class ConfirmUIAutoSetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup ConfirmUI System")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConfirmUIAutoSetup>("ConfirmUI Setup");
        window.minSize = new Vector2(400, 250);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ConfirmUI System Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはConfirmUIを自動セットアップします。\n" +
            "Screen Space - Overlay Canvas + 画面中央下部のボタンUIを作成します。",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup ConfirmUI System", GUILayout.Height(40)))
        {
            SetupConfirmUI();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "セットアップ後、ConfirmUIのInspectorで以下を確認してください：\n" +
            "- Canvas Render Mode: Screen Space - Overlay\n" +
            "- Draw Button, Cancel Button, Prompt Text が正しく設定されているか",
            MessageType.Info);
    }

    private void SetupConfirmUI()
    {
        if (!EditorUtility.DisplayDialog("ConfirmUI Setup",
            "ConfirmUIをセットアップします。\n既存の設定は上書きされます。\n\n続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup ConfirmUI System");

        try
        {
            GameObject confirmUIObj = SetupConfirmUIGameObject();

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success",
                "ConfirmUIのセットアップが完了しました！\n\n" +
                "作成されたオブジェクト:\n" +
                "- ConfirmUI GameObject (Screen Space Overlay)\n" +
                "- UI Panel with Draw/Cancel buttons (画面中央下部)\n",
                "OK");

            Debug.Log("[ConfirmUIAutoSetup] Setup completed successfully!");

            Selection.activeGameObject = confirmUIObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ConfirmUIAutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
        }
    }

    private GameObject SetupConfirmUIGameObject()
    {
        var existingConfirmUI = FindObjectOfType<ConfirmUI>();
        GameObject confirmUIObj;

        if (existingConfirmUI != null)
        {
            confirmUIObj = existingConfirmUI.gameObject;
            Debug.Log("[ConfirmUIAutoSetup] Found existing ConfirmUI");
        }
        else
        {
            confirmUIObj = new GameObject("ConfirmUI");
            Undo.RegisterCreatedObjectUndo(confirmUIObj, "Create ConfirmUI");
            Debug.Log("[ConfirmUIAutoSetup] Created new ConfirmUI");
        }

        // Canvas追加/取得 → Screen Space - Overlay
        Canvas canvas = confirmUIObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = Undo.AddComponent<Canvas>(confirmUIObj);
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // CanvasScaler（画面サイズ追従）
        CanvasScaler canvasScaler = confirmUIObj.GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = Undo.AddComponent<CanvasScaler>(confirmUIObj);
        }
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster
        if (confirmUIObj.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(confirmUIObj);
        }

        // UI Panel作成
        GameObject uiPanel = CreateUIPanel(confirmUIObj.transform);

        // ConfirmUIコンポーネント追加/取得
        var confirmUIComponent = confirmUIObj.GetComponent<ConfirmUI>();
        if (confirmUIComponent == null)
        {
            confirmUIComponent = Undo.AddComponent<ConfirmUI>(confirmUIObj);
        }

        // SerializedObjectで設定を適用
        SerializedObject so = new SerializedObject(confirmUIComponent);

        so.FindProperty("canvas").objectReferenceValue = canvas;
        so.FindProperty("uiPanel").objectReferenceValue = uiPanel;

        Button drawButton = uiPanel.transform.Find("DrawButton")?.GetComponent<Button>();
        Button cancelButton = uiPanel.transform.Find("CancelButton")?.GetComponent<Button>();
        TMP_Text promptText = uiPanel.transform.Find("PromptText")?.GetComponent<TMP_Text>();

        if (drawButton != null)
        {
            so.FindProperty("drawButton").objectReferenceValue = drawButton;
            TMP_Text drawButtonText = drawButton.GetComponentInChildren<TMP_Text>();
            so.FindProperty("drawButtonText").objectReferenceValue = drawButtonText;
        }

        if (cancelButton != null)
        {
            so.FindProperty("cancelButton").objectReferenceValue = cancelButton;
            TMP_Text cancelButtonText = cancelButton.GetComponentInChildren<TMP_Text>();
            so.FindProperty("cancelButtonText").objectReferenceValue = cancelButtonText;
        }

        if (promptText != null)
        {
            so.FindProperty("promptText").objectReferenceValue = promptText;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(confirmUIComponent);

        return confirmUIObj;
    }

    private GameObject CreateUIPanel(Transform parent)
    {
        Transform existingPanel = parent.Find("UIPanel");
        if (existingPanel != null)
        {
            return existingPanel.gameObject;
        }

        // UI Panel — 画面中央下部に配置
        GameObject uiPanel = new GameObject("UIPanel");
        Undo.RegisterCreatedObjectUndo(uiPanel, "Create UI Panel");
        uiPanel.transform.SetParent(parent, false);

        RectTransform panelRect = uiPanel.AddComponent<RectTransform>();
        // 画面下部中央にアンカー
        panelRect.anchorMin = new Vector2(0.5f, 0.15f);
        panelRect.anchorMax = new Vector2(0.5f, 0.15f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 140);
        panelRect.anchoredPosition = Vector2.zero;

        uiPanel.AddComponent<CanvasGroup>();

        // 背景Image（角丸風ダーク背景）
        Image panelImage = uiPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Prompt Text — パネル上部
        GameObject promptTextObj = new GameObject("PromptText");
        Undo.RegisterCreatedObjectUndo(promptTextObj, "Create Prompt Text");
        promptTextObj.transform.SetParent(uiPanel.transform, false);

        RectTransform promptRect = promptTextObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0.55f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.offsetMin = new Vector2(10, 0);
        promptRect.offsetMax = new Vector2(-10, -8);

        TMP_Text promptTmp = promptTextObj.AddComponent<TextMeshProUGUI>();
        promptTmp.text = "このカードを引きますか？";
        promptTmp.fontSize = 24;
        promptTmp.alignment = TextAlignmentOptions.Center;
        promptTmp.color = Color.white;

        // Draw Button — 左側
        GameObject drawButtonObj = CreateButton("DrawButton", "引く",
            new Vector2(0.15f, 0.05f), new Vector2(0.48f, 0.5f),
            new Color(0.15f, 0.65f, 0.15f));
        drawButtonObj.transform.SetParent(uiPanel.transform, false);

        // Cancel Button — 右側
        GameObject cancelButtonObj = CreateButton("CancelButton", "やめる",
            new Vector2(0.52f, 0.05f), new Vector2(0.85f, 0.5f),
            new Color(0.65f, 0.15f, 0.15f));
        cancelButtonObj.transform.SetParent(uiPanel.transform, false);

        return uiPanel;
    }

    private GameObject CreateButton(string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(buttonObj, $"Create {name}");

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // ホバー色設定
        var colors = button.colors;
        colors.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
        button.colors = colors;

        // ボタンテキスト
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
        buttonText.fontSize = 22;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return buttonObj;
    }
}
