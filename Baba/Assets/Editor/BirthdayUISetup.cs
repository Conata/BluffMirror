using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FPSTrump.UI;

/// <summary>
/// BirthdayPanel用のUIエレメントを自動生成するエディターツール
/// </summary>
public class BirthdayUISetup : EditorWindow
{
    [MenuItem("Tools/Baba/Setup Birthday UI Elements")]
    public static void ShowWindow()
    {
        var window = GetWindow<BirthdayUISetup>("Birthday UI Setup");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Birthday UI Element Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはBirthdayPanel内に不足しているUIエレメントを自動生成します：\n" +
            "- Name Input Field (TMP_InputField)\n" +
            "- Name Label (TextMeshProUGUI)",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Missing Name UI Elements", GUILayout.Height(50)))
        {
            GenerateNameUIElements();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate All Missing UI Elements", GUILayout.Height(50)))
        {
            GenerateAllMissingElements();
        }
    }

    private void GenerateNameUIElements()
    {
        BirthdaySetupUI birthdaySetupUI = FindFirstObjectByType<BirthdaySetupUI>();

        if (birthdaySetupUI == null)
        {
            EditorUtility.DisplayDialog("Error",
                "BirthdaySetupUI not found. Please open StartMenuScen.unity first.",
                "OK");
            return;
        }

        GameObject birthdayPanel = birthdaySetupUI.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(birthdayPanel, "Generate Name UI Elements");

        int createdCount = 0;

        // Name Label作成
        if (!FindChildByName(birthdayPanel.transform, "NameLabel"))
        {
            GameObject nameLabel = CreateTextLabel(birthdayPanel.transform, "NameLabel", "名前 / Name");
            SetRectTransform(nameLabel.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -50), new Vector2(200, 30));
            createdCount++;
        }

        // Name Input Field作成
        if (!FindChildByName(birthdayPanel.transform, "NameInput"))
        {
            GameObject nameInput = CreateInputField(birthdayPanel.transform, "NameInput", "Enter your name...");
            SetRectTransform(nameInput.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -90), new Vector2(400, 40));
            createdCount++;
        }

        // BirthdaySetupUIに参照を設定
        SerializedObject so = new SerializedObject(birthdaySetupUI);

        Transform nameLabelTransform = FindChildByName(birthdayPanel.transform, "NameLabel");
        if (nameLabelTransform != null)
        {
            SerializedProperty nameLabelProp = so.FindProperty("nameLabel");
            if (nameLabelProp != null)
            {
                nameLabelProp.objectReferenceValue = nameLabelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform nameInputTransform = FindChildByName(birthdayPanel.transform, "NameInput");
        if (nameInputTransform != null)
        {
            SerializedProperty nameInputProp = so.FindProperty("nameInputField");
            if (nameInputProp != null)
            {
                nameInputProp.objectReferenceValue = nameInputTransform.GetComponent<TMP_InputField>();
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(birthdaySetupUI);

        EditorUtility.DisplayDialog("Success",
            $"Created {createdCount} UI element(s) and assigned references to BirthdaySetupUI.",
            "OK");

        Debug.Log($"[BirthdayUISetup] Created {createdCount} Name UI elements");
    }

    private void GenerateAllMissingElements()
    {
        EditorUtility.DisplayDialog("Coming Soon",
            "全UIエレメント自動生成機能は開発中です。\n現在は Name UI Elements の生成のみ対応しています。",
            "OK");
    }

    private GameObject CreateTextLabel(Transform parent, string name, string text)
    {
        GameObject labelObj = new GameObject(name, typeof(RectTransform));
        labelObj.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = labelObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 24;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;

        return labelObj;
    }

    private GameObject CreateInputField(Transform parent, string name, string placeholder)
    {
        // Input Field GameObject
        GameObject inputFieldObj = new GameObject(name, typeof(RectTransform));
        inputFieldObj.transform.SetParent(parent, false);

        // Image (Background)
        Image bgImage = inputFieldObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // TMP_InputField
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();

        // Text Area
        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(inputFieldObj.transform, false);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = new Vector2(-20, -10);
        textAreaRect.anchoredPosition = Vector2.zero;

        // Placeholder Text
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
        placeholderObj.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        placeholderRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 20;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.fontStyle = FontStyles.Italic;

        // Input Text
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 20;
        inputText.color = Color.white;

        // Assign references to InputField
        inputField.textViewport = textAreaRect;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        inputField.characterLimit = 20;

        return inputFieldObj;
    }

    private void SetRectTransform(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
        }
        return null;
    }
}
