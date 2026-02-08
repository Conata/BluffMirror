using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using FPSTrump.UI;

/// <summary>
/// BirthdaySetupUIに言語選択UIを自動追加
/// Tools > Baba > Setup Birthday Language UI から実行
/// </summary>
public class BirthdayLanguageUISetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup Birthday Language UI")]
    public static void ShowWindow()
    {
        var window = GetWindow<BirthdayLanguageUISetup>("Birthday Language UI Setup");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Birthday Language UI Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "BirthdaySetupUIに言語選択UIを自動追加します。\n" +
            "以下の要素を作成・設定します：\n" +
            "- 言語ラベル (Language / 言語)\n" +
            "- 言語ドロップダウン (English / 日本語)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Language UI", GUILayout.Height(40)))
        {
            SetupLanguageUI();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "セットアップ後、Inspectorで以下を確認：\n" +
            "- BirthdaySetupUI: Language Selection セクション\n" +
            "- languageLabel と languageDropdown が設定されているか\n" +
            "- 実行時に言語切替が動作するか確認してください",
            MessageType.Info);
    }

    private void SetupLanguageUI()
    {
        if (!EditorUtility.DisplayDialog("Birthday Language UI Setup",
            "BirthdaySetupUIに言語選択UIを追加します。\n既存の設定は上書きされます。\n\n続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Birthday Language UI");

        try
        {
            var birthdaySetupUI = FindFirstObjectByType<BirthdaySetupUI>();
            if (birthdaySetupUI == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "BirthdaySetupUIが見つかりません。\n" +
                    "シーンにBirthdaySetupUIコンポーネントが存在することを確認してください。",
                    "OK");
                return;
            }

            GameObject birthdayPanel = birthdaySetupUI.transform.Find("BirthdayPanel")?.gameObject;
            if (birthdayPanel == null)
            {
                birthdayPanel = birthdaySetupUI.gameObject;
                Debug.LogWarning("[BirthdayLanguageUISetup] BirthdayPanel not found, using root GameObject");
            }

            // Language Selection Area を作成または検索
            Transform languageArea = birthdayPanel.transform.Find("LanguageSelection");
            if (languageArea == null)
            {
                GameObject languageAreaObj = new GameObject("LanguageSelection");
                Undo.RegisterCreatedObjectUndo(languageAreaObj, "Create LanguageSelection");
                languageAreaObj.transform.SetParent(birthdayPanel.transform, false);

                RectTransform rect = languageAreaObj.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0, -80); // 名前入力の下あたり
                rect.sizeDelta = new Vector2(400, 40);

                languageArea = languageAreaObj.transform;
            }

            // Label を作成または検索
            TMP_Text languageLabel = CreateOrFindLanguageLabel(languageArea);

            // Dropdown を作成または検索
            TMP_Dropdown languageDropdown = CreateOrFindLanguageDropdown(languageArea);

            // BirthdaySetupUIのSerializedObjectで設定
            SerializedObject so = new SerializedObject(birthdaySetupUI);
            so.FindProperty("languageLabel").objectReferenceValue = languageLabel;
            so.FindProperty("languageDropdown").objectReferenceValue = languageDropdown;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(birthdaySetupUI);

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success",
                "言語選択UIのセットアップが完了しました！\n\n" +
                "作成された要素:\n" +
                "- LanguageSelection エリア\n" +
                "- 言語ラベル (Language / 言語)\n" +
                "- 言語ドロップダウン (English / 日本語)\n\n" +
                "BirthdaySetupUIのInspectorを確認してください。",
                "OK");

            Debug.Log("[BirthdayLanguageUISetup] Setup completed!");
            Selection.activeGameObject = birthdaySetupUI.gameObject;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BirthdayLanguageUISetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"エラーが発生しました:\n{e.Message}", "OK");
        }
    }

    private TMP_Text CreateOrFindLanguageLabel(Transform parent)
    {
        Transform existing = parent.Find("LanguageLabel");
        GameObject labelObj;

        if (existing != null)
        {
            labelObj = existing.gameObject;
            Debug.Log("[BirthdayLanguageUISetup] Found existing LanguageLabel");
        }
        else
        {
            labelObj = new GameObject("LanguageLabel");
            Undo.RegisterCreatedObjectUndo(labelObj, "Create LanguageLabel");
            labelObj.transform.SetParent(parent, false);

            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(100, 0);
            rect.sizeDelta = new Vector2(150, 30);
        }

        TMP_Text label = labelObj.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = Undo.AddComponent<TextMeshProUGUI>(labelObj);
        }

        Undo.RecordObject(label, "Set Label Properties");
        label.text = "Language / 言語";
        label.fontSize = 18;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.color = Color.white;

        return label;
    }

    private TMP_Dropdown CreateOrFindLanguageDropdown(Transform parent)
    {
        Transform existing = parent.Find("LanguageDropdown");
        GameObject dropdownObj;

        if (existing != null)
        {
            dropdownObj = existing.gameObject;
            Debug.Log("[BirthdayLanguageUISetup] Found existing LanguageDropdown");
        }
        else
        {
            dropdownObj = new GameObject("LanguageDropdown");
            Undo.RegisterCreatedObjectUndo(dropdownObj, "Create LanguageDropdown");
            dropdownObj.transform.SetParent(parent, false);

            RectTransform rect = dropdownObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(300, 0);
            rect.sizeDelta = new Vector2(180, 35);

            // Background
            Image bgImage = dropdownObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);
        }

        TMP_Dropdown dropdown = dropdownObj.GetComponent<TMP_Dropdown>();
        if (dropdown == null)
        {
            dropdown = Undo.AddComponent<TMP_Dropdown>(dropdownObj);
        }

        // Label (選択中の値を表示)
        Transform labelTransform = dropdownObj.transform.Find("Label");
        GameObject labelObj;

        if (labelTransform != null)
        {
            labelObj = labelTransform.gameObject;
        }
        else
        {
            labelObj = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(labelObj, "Create Dropdown Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 2);
            labelRect.offsetMax = new Vector2(-25, -2);
        }

        TMP_Text labelText = labelObj.GetComponent<TMP_Text>();
        if (labelText == null)
        {
            labelText = Undo.AddComponent<TextMeshProUGUI>(labelObj);
        }

        Undo.RecordObject(labelText, "Set Dropdown Label");
        labelText.text = "English";
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = Color.white;

        // Arrow
        Transform arrowTransform = dropdownObj.transform.Find("Arrow");
        GameObject arrowObj;

        if (arrowTransform != null)
        {
            arrowObj = arrowTransform.gameObject;
        }
        else
        {
            arrowObj = new GameObject("Arrow");
            Undo.RegisterCreatedObjectUndo(arrowObj, "Create Dropdown Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);

            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            arrowRect.sizeDelta = new Vector2(20, 20);

            Image arrowImage = arrowObj.AddComponent<Image>();
            arrowImage.color = Color.white;
            // Note: Unity標準のArrowスプライトが必要な場合は、Resourcesからロード可能
        }

        // Template (ドロップダウンリスト)
        Transform templateTransform = dropdownObj.transform.Find("Template");
        GameObject templateObj;

        if (templateTransform != null)
        {
            templateObj = templateTransform.gameObject;
        }
        else
        {
            templateObj = new GameObject("Template");
            Undo.RegisterCreatedObjectUndo(templateObj, "Create Dropdown Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);

            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 80);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.25f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            Undo.RegisterCreatedObjectUndo(viewportObj, "Create Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Content
            GameObject contentObj = new GameObject("Content");
            Undo.RegisterCreatedObjectUndo(contentObj, "Create Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 80);

            // Item
            GameObject itemObj = new GameObject("Item");
            Undo.RegisterCreatedObjectUndo(itemObj, "Create Item");
            itemObj.transform.SetParent(contentObj.transform, false);

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0, 35);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.isOn = true;

            // Item Background
            GameObject itemBgObj = new GameObject("Item Background");
            Undo.RegisterCreatedObjectUndo(itemBgObj, "Create Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);

            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;

            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.3f, 0.3f, 0.35f);

            // Item Checkmark
            GameObject checkmarkObj = new GameObject("Item Checkmark");
            Undo.RegisterCreatedObjectUndo(checkmarkObj, "Create Checkmark");
            checkmarkObj.transform.SetParent(itemObj.transform, false);

            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);
            checkmarkRect.sizeDelta = new Vector2(20, 20);

            Image checkmarkImage = checkmarkObj.AddComponent<Image>();
            checkmarkImage.color = Color.white;

            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label");
            Undo.RegisterCreatedObjectUndo(itemLabelObj, "Create Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);

            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(20, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -1);

            TMP_Text itemLabelText = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "English";
            itemLabelText.fontSize = 16;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabelText.color = Color.white;

            // Toggle設定
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = checkmarkImage;

            // ScrollRect設定
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            templateObj.SetActive(false);
        }

        // Dropdown設定
        Undo.RecordObject(dropdown, "Set Dropdown Properties");
        dropdown.captionText = labelText;
        dropdown.itemText = templateObj.transform.Find("Viewport/Content/Item/Item Label")?.GetComponent<TMP_Text>();
        dropdown.template = templateObj.GetComponent<RectTransform>();

        // オプション設定
        dropdown.ClearOptions();
        dropdown.options.Add(new TMP_Dropdown.OptionData("English"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("日本語"));
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
    }
}
