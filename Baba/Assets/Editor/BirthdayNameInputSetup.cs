using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using FPSTrump.UI;

/// <summary>
/// BirthdaySetupUIに名前入力フィールドを追加するセットアップツール
/// Tools > Baba > Setup Birthday Name Input から実行
/// BirthdayPanelが存在するシーンで実行すること
/// </summary>
public class BirthdayNameInputSetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup Birthday Name Input")]
    public static void ShowWindow()
    {
        var window = GetWindow<BirthdayNameInputSetup>("Birthday Name Input Setup");
        window.minSize = new Vector2(420, 320);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Birthday Name Input Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "BirthdayPanelに名前入力フィールドを追加します。\n\n" +
            "実行前:\n" +
            "- BirthdayPanelが存在するシーンを開いてください\n" +
            "- BirthdaySetupUIコンポーネントがアタッチされている必要があります\n\n" +
            "実行後:\n" +
            "- Name Input Field (TMP_InputField)\n" +
            "- Name Label (TextMeshProUGUI)\n" +
            "が追加され、BirthdaySetupUIにアサインされます。",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Name Input Field", GUILayout.Height(40)))
        {
            SetupNameInput();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "既に名前入力フィールドが存在する場合は、\n" +
            "再作成するか確認ダイアログが表示されます。",
            MessageType.None);
    }

    private void SetupNameInput()
    {
        // BirthdaySetupUIを検索
        BirthdaySetupUI birthdaySetupUI = FindFirstObjectByType<BirthdaySetupUI>();
        if (birthdaySetupUI == null)
        {
            EditorUtility.DisplayDialog("Error",
                "BirthdaySetupUIが見つかりません。\n" +
                "BirthdayPanelが存在するシーンを開いてください。",
                "OK");
            return;
        }

        GameObject birthdayPanel = birthdaySetupUI.gameObject;

        // 既存の名前入力フィールドをチェック
        Transform existingNameGroup = birthdayPanel.transform.Find("NameInputGroup");
        if (existingNameGroup != null)
        {
            if (!EditorUtility.DisplayDialog("確認",
                "名前入力フィールドは既に存在します。再作成しますか？",
                "再作成", "キャンセル"))
                return;
            Undo.DestroyObjectImmediate(existingNameGroup.gameObject);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Birthday Name Input");

        try
        {
            // フォントアセット取得
            TMP_FontAsset fontAsset = LoadJapaneseFont();

            // 名前入力グループ作成
            GameObject nameGroup = CreateNameInputGroup(birthdayPanel.transform, fontAsset);

            // BirthdaySetupUIに参照をアサイン
            AssignReferencesToBirthdaySetupUI(birthdaySetupUI, nameGroup);

            // PlayerNameManager作成（存在しない場合）
            SetupPlayerNameManager();

            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = nameGroup;
            EditorGUIUtility.PingObject(nameGroup);

            EditorUtility.DisplayDialog("完了",
                "名前入力フィールドのセットアップが完了しました！\n\n" +
                "作成されたオブジェクト:\n" +
                "- NameInputGroup\n" +
                "  ├─ NameLabel (TextMeshProUGUI)\n" +
                "  └─ NameInputField (TMP_InputField)\n\n" +
                "BirthdaySetupUIの以下のフィールドに自動アサインしました:\n" +
                "- nameLabel\n" +
                "- nameInputField\n\n" +
                "PlayerNameManagerも作成済みです。",
                "OK");

            Debug.Log("[BirthdayNameInputSetup] Setup completed!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BirthdayNameInputSetup] Setup failed: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラー:\n{e.Message}", "OK");
        }
    }

    private GameObject CreateNameInputGroup(Transform parent, TMP_FontAsset fontAsset)
    {
        // NameInputGroup (Vertical Layout)
        GameObject nameGroup = new GameObject("NameInputGroup");
        Undo.RegisterCreatedObjectUndo(nameGroup, "Create NameInputGroup");
        nameGroup.transform.SetParent(parent, false);

        RectTransform nameGroupRect = nameGroup.AddComponent<RectTransform>();
        nameGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameGroupRect.pivot = new Vector2(0.5f, 0.5f);
        nameGroupRect.sizeDelta = new Vector2(600, 120);
        nameGroupRect.anchoredPosition = new Vector2(0, 220); // 誕生日ドロップダウンの上に配置

        VerticalLayoutGroup vLayout = nameGroup.AddComponent<VerticalLayoutGroup>();
        vLayout.childControlWidth = false;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = false;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing = 10;
        vLayout.padding = new RectOffset(20, 20, 20, 20);

        // Name Label
        GameObject nameLabel = CreateNameLabel(nameGroup.transform, fontAsset);

        // Name InputField
        GameObject nameInputField = CreateNameInputField(nameGroup.transform, fontAsset);

        return nameGroup;
    }

    private GameObject CreateNameLabel(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject labelObj = new GameObject("NameLabel");
        Undo.RegisterCreatedObjectUndo(labelObj, "Create NameLabel");
        labelObj.transform.SetParent(parent, false);

        RectTransform rect = labelObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560, 30);

        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "名前（任意）";
        label.font = fontAsset;
        label.fontSize = 24;
        label.color = new Color(0.9f, 0.9f, 0.9f);
        label.alignment = TextAlignmentOptions.Left;

        return labelObj;
    }

    private GameObject CreateNameInputField(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject inputFieldObj = new GameObject("NameInputField");
        Undo.RegisterCreatedObjectUndo(inputFieldObj, "Create NameInputField");
        inputFieldObj.transform.SetParent(parent, false);

        RectTransform rect = inputFieldObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560, 50);

        // Image component (background)
        Image bgImage = inputFieldObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // TMP_InputField component
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
        inputField.transition = Selectable.Transition.ColorTint;
        inputField.characterLimit = 20;

        // Text Area (child)
        GameObject textArea = new GameObject("Text Area");
        Undo.RegisterCreatedObjectUndo(textArea, "Create Text Area");
        textArea.transform.SetParent(inputFieldObj.transform, false);

        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -6);

        RectMask2D mask = textArea.AddComponent<RectMask2D>();

        // Placeholder (child of Text Area)
        GameObject placeholder = new GameObject("Placeholder");
        Undo.RegisterCreatedObjectUndo(placeholder, "Create Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);

        RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "プレイヤー名を入力...";
        placeholderText.font = fontAsset;
        placeholderText.fontSize = 20;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholderText.fontStyle = FontStyles.Italic;

        // Text (child of Text Area)
        GameObject text = new GameObject("Text");
        Undo.RegisterCreatedObjectUndo(text, "Create Text");
        text.transform.SetParent(textArea.transform, false);

        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.font = fontAsset;
        textComponent.fontSize = 20;
        textComponent.color = Color.white;

        // Assign references to TMP_InputField
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;

        return inputFieldObj;
    }

    private void AssignReferencesToBirthdaySetupUI(BirthdaySetupUI birthdaySetupUI, GameObject nameGroup)
    {
        SerializedObject so = new SerializedObject(birthdaySetupUI);

        // Find the components
        TextMeshProUGUI nameLabel = nameGroup.transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
        TMP_InputField nameInputField = nameGroup.transform.Find("NameInputField")?.GetComponent<TMP_InputField>();

        if (nameLabel != null)
        {
            SerializedProperty nameLabelProp = so.FindProperty("nameLabel");
            if (nameLabelProp != null)
            {
                nameLabelProp.objectReferenceValue = nameLabel;
            }
        }

        if (nameInputField != null)
        {
            SerializedProperty nameInputFieldProp = so.FindProperty("nameInputField");
            if (nameInputFieldProp != null)
            {
                nameInputFieldProp.objectReferenceValue = nameInputField;
            }
        }

        so.ApplyModifiedProperties();

        Debug.Log("[BirthdayNameInputSetup] References assigned to BirthdaySetupUI");
    }

    private void SetupPlayerNameManager()
    {
        // Check if PlayerNameManager already exists
        FPSTrump.Manager.PlayerNameManager existingManager = FindFirstObjectByType<FPSTrump.Manager.PlayerNameManager>();
        if (existingManager != null)
        {
            Debug.Log("[BirthdayNameInputSetup] PlayerNameManager already exists");
            return;
        }

        // Create PlayerNameManager GameObject
        GameObject managerObj = new GameObject("PlayerNameManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create PlayerNameManager");

        FPSTrump.Manager.PlayerNameManager manager = managerObj.AddComponent<FPSTrump.Manager.PlayerNameManager>();

        Debug.Log("[BirthdayNameInputSetup] PlayerNameManager created");
    }

    private TMP_FontAsset LoadJapaneseFont()
    {
        string[] guids = AssetDatabase.FindAssets("NotoSansJP-VariableFont_wght t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                Debug.Log($"[BirthdayNameInputSetup] Font loaded: {path}");
                return font;
            }
        }

        Debug.LogWarning("[BirthdayNameInputSetup] NotoSansJP font not found, using Arial");
        return TMP_Settings.defaultFontAsset;
    }
}
