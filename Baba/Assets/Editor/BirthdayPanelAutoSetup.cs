using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using FPSTrump.UI;
using FPSTrump.Manager;

/// <summary>
/// BirthdayPanel自動セットアップ
/// Tools > Baba > Setup Birthday Panel から実行
/// StartMenuScenシーンを開いた状態で実行すること
/// </summary>
public class BirthdayPanelAutoSetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup Birthday Panel")]
    public static void ShowWindow()
    {
        var window = GetWindow<BirthdayPanelAutoSetup>("Birthday Panel Setup");
        window.minSize = new Vector2(420, 300);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Birthday Panel Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "StartMenuScenシーンを開いた状態で実行してください。\n" +
            "Canvas内にBirthdayPanelを自動作成し、\n" +
            "APIKeySetupUIのbirthdayPanelフィールドを設定します。",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Birthday Panel", GUILayout.Height(40)))
        {
            SetupBirthdayPanel();
        }
    }

    private void SetupBirthdayPanel()
    {
        // Canvas検索
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Canvasが見つかりません。StartMenuScenシーンを開いてください。", "OK");
            return;
        }

        // 既存チェック
        Transform existing = canvas.transform.Find("BirthdayPanel");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("確認",
                "BirthdayPanelは既に存在します。再作成しますか？",
                "再作成", "キャンセル"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Birthday Panel");

        try
        {
            // フォントアセット取得
            TMP_FontAsset fontAsset = LoadJapaneseFont();

            // BirthdayPanel作成
            GameObject birthdayPanel = CreateBirthdayPanel(canvas.transform, fontAsset);

            // PlayerBirthdayManager作成
            SetupPlayerBirthdayManager();

            // APIKeySetupUIにbirthdayPanel参照を設定
            LinkToAPIKeySetupUI(birthdayPanel);

            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = birthdayPanel;

            EditorUtility.DisplayDialog("完了",
                "BirthdayPanelのセットアップが完了しました！\n\n" +
                "作成されたオブジェクト:\n" +
                "- BirthdayPanel (年/月/日 ドロップダウン)\n" +
                "- PlayerBirthdayManager\n\n" +
                "APIKeySetupUIのbirthdayPanelフィールドも設定済みです。",
                "OK");

            Debug.Log("[BirthdayPanelAutoSetup] Setup completed!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BirthdayPanelAutoSetup] Setup failed: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラー:\n{e.Message}", "OK");
        }
    }

    private TMP_FontAsset LoadJapaneseFont()
    {
        string fontPath = "Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null)
        {
            Debug.LogWarning("[BirthdayPanelAutoSetup] NotoSansJP SDF not found, using TMP default font");
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    private GameObject CreateBirthdayPanel(Transform canvasTransform, TMP_FontAsset fontAsset)
    {
        // BirthdayPanel本体
        GameObject panel = new GameObject("BirthdayPanel");
        Undo.RegisterCreatedObjectUndo(panel, "Create BirthdayPanel");
        panel.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        // タイトルテキスト
        GameObject titleObj = CreateTMPText("TitleText", panel.transform, fontAsset,
            "生年月日を入力してください", 36,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(600, 60));

        // サブタイトルテキスト
        GameObject subtitleObj = CreateTMPText("SubtitleText", panel.transform, fontAsset,
            "AIの応答に活用されます（スキップ可能）", 20,
            new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(600, 40));
        subtitleObj.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f, 1f);

        // ドロップダウンコンテナ
        GameObject dropdownContainer = new GameObject("DropdownContainer");
        dropdownContainer.transform.SetParent(panel.transform, false);
        RectTransform containerRect = dropdownContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(700, 80);

        // 年ラベル + ドロップダウン
        CreateTMPText("YearLabel", dropdownContainer.transform, fontAsset,
            "年", 24,
            new Vector2(0.14f, 0.5f), new Vector2(0.14f, 0.5f), new Vector2(50, 40));

        GameObject yearDropdown = CreateDropdown("YearDropdown", dropdownContainer.transform, fontAsset,
            new Vector2(0.05f, 0.5f), new Vector2(180, 50));

        // 月ラベル + ドロップダウン
        CreateTMPText("MonthLabel", dropdownContainer.transform, fontAsset,
            "月", 24,
            new Vector2(0.47f, 0.5f), new Vector2(0.47f, 0.5f), new Vector2(50, 40));

        GameObject monthDropdown = CreateDropdown("MonthDropdown", dropdownContainer.transform, fontAsset,
            new Vector2(0.38f, 0.5f), new Vector2(130, 50));

        // 日ラベル + ドロップダウン
        CreateTMPText("DayLabel", dropdownContainer.transform, fontAsset,
            "日", 24,
            new Vector2(0.77f, 0.5f), new Vector2(0.77f, 0.5f), new Vector2(50, 40));

        GameObject dayDropdown = CreateDropdown("DayDropdown", dropdownContainer.transform, fontAsset,
            new Vector2(0.68f, 0.5f), new Vector2(130, 50));

        // ステータステキスト
        GameObject statusText = CreateTMPText("BirthdayStatusText", panel.transform, fontAsset,
            "", 20,
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(500, 40));

        // ボタンコンテナ
        GameObject nextButton = CreateButton("NextButton", "次へ", panel.transform, fontAsset,
            new Vector2(0.35f, 0.22f), new Vector2(200, 55), new Color(0.2f, 0.6f, 0.9f, 1f));

        GameObject skipButton = CreateButton("SkipButton", "スキップ", panel.transform, fontAsset,
            new Vector2(0.65f, 0.22f), new Vector2(200, 55), new Color(0.4f, 0.4f, 0.4f, 1f));

        // BirthdaySetupUIコンポーネントをアタッチ
        BirthdaySetupUI birthdayUI = panel.AddComponent<BirthdaySetupUI>();

        // SerializedObjectで参照を設定
        SerializedObject so = new SerializedObject(birthdayUI);
        so.FindProperty("yearDropdown").objectReferenceValue = yearDropdown.GetComponent<TMP_Dropdown>();
        so.FindProperty("monthDropdown").objectReferenceValue = monthDropdown.GetComponent<TMP_Dropdown>();
        so.FindProperty("dayDropdown").objectReferenceValue = dayDropdown.GetComponent<TMP_Dropdown>();
        so.FindProperty("nextButton").objectReferenceValue = nextButton.GetComponent<Button>();
        so.FindProperty("skipButton").objectReferenceValue = skipButton.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("birthdayPanel").objectReferenceValue = panel;

        // ローカライズ用テキスト参照
        so.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("subtitleText").objectReferenceValue = subtitleObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("nextButtonText").objectReferenceValue = nextButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("skipButtonText").objectReferenceValue = skipButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();

        // ラベル参照
        Transform container = panel.transform.Find("DropdownContainer");
        if (container != null)
        {
            so.FindProperty("yearLabel").objectReferenceValue = container.Find("YearLabel")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("monthLabel").objectReferenceValue = container.Find("MonthLabel")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("dayLabel").objectReferenceValue = container.Find("DayLabel")?.GetComponent<TextMeshProUGUI>();
        }

        // ReadyPanelを検索して設定
        Transform readyPanel = canvasTransform.Find("ReadyPanel");
        if (readyPanel != null)
        {
            so.FindProperty("readyPanel").objectReferenceValue = readyPanel.gameObject;
        }
        else
        {
            Debug.LogWarning("[BirthdayPanelAutoSetup] ReadyPanel not found in Canvas");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(birthdayUI);

        // 初期状態は非アクティブ
        panel.SetActive(false);

        return panel;
    }

    private void SetupPlayerBirthdayManager()
    {
        PlayerBirthdayManager existing = FindFirstObjectByType<PlayerBirthdayManager>();
        if (existing != null)
        {
            Debug.Log("[BirthdayPanelAutoSetup] PlayerBirthdayManager already exists");
            return;
        }

        GameObject managerObj = new GameObject("PlayerBirthdayManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create PlayerBirthdayManager");
        managerObj.AddComponent<PlayerBirthdayManager>();
        Debug.Log("[BirthdayPanelAutoSetup] Created PlayerBirthdayManager");
    }

    private void LinkToAPIKeySetupUI(GameObject birthdayPanel)
    {
        APIKeySetupUI apiKeyUI = FindFirstObjectByType<APIKeySetupUI>(FindObjectsInactive.Include);
        if (apiKeyUI == null)
        {
            Debug.LogWarning("[BirthdayPanelAutoSetup] APIKeySetupUI not found");
            return;
        }

        SerializedObject so = new SerializedObject(apiKeyUI);
        so.FindProperty("birthdayPanel").objectReferenceValue = birthdayPanel;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(apiKeyUI);
        Debug.Log("[BirthdayPanelAutoSetup] Linked birthdayPanel to APIKeySetupUI");
    }

    private GameObject CreateTMPText(string name, Transform parent, TMP_FontAsset font,
        string text, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (font != null)
            tmp.font = font;

        return obj;
    }

    private GameObject CreateDropdown(string name, Transform parent, TMP_FontAsset font,
        Vector2 anchorPos, Vector2 size)
    {
        // TMP_Dropdownを含むGameObjectを手動構築
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent, false);

        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image dropdownImage = dropdownObj.AddComponent<Image>();
        dropdownImage.color = new Color(0.15f, 0.15f, 0.25f, 1f);

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

        // CaptionText
        GameObject captionObj = new GameObject("Label");
        captionObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform captionRect = captionObj.AddComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 2);
        captionRect.offsetMax = new Vector2(-25, -2);

        TextMeshProUGUI captionText = captionObj.AddComponent<TextMeshProUGUI>();
        captionText.fontSize = 20;
        captionText.alignment = TextAlignmentOptions.Left;
        captionText.color = Color.white;
        if (font != null)
            captionText.font = font;

        dropdown.captionText = captionText;

        // Arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-5, 0);
        Image arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        // Template (ドロップダウンリスト)
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0, 200);

        Image templateImage = templateObj.AddComponent<Image>();
        templateImage.color = new Color(0.12f, 0.12f, 0.2f, 1f);

        ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportObj.AddComponent<Image>().color = Color.white;
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 28);

        Toggle itemToggle = itemObj.AddComponent<Toggle>();

        // ItemLabel
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 1);
        itemLabelRect.offsetMax = new Vector2(-10, -2);

        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 18;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.color = Color.white;
        if (font != null)
            itemLabel.font = font;

        // Toggle / ScrollRect / Dropdown の参照設定
        itemToggle.targetGraphic = itemObj.AddComponent<Image>();
        ((Image)itemToggle.targetGraphic).color = new Color(0.2f, 0.2f, 0.35f, 1f);
        itemToggle.isOn = true;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;

        templateObj.SetActive(false);

        return dropdownObj;
    }

    private GameObject CreateButton(string name, string text, Transform parent, TMP_FontAsset font,
        Vector2 anchorPos, Vector2 size, Color bgColor)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = bgColor;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // ボタンテキスト
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (font != null)
            tmp.font = font;

        return buttonObj;
    }
}
