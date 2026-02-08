using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FPSTrump.UI;

/// <summary>
/// BirthdaySetupUIの参照を検証・修復するエディターツール
/// </summary>
public class BirthdaySetupValidator : EditorWindow
{
    private BirthdaySetupUI targetUI;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Baba/Validate Birthday Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<BirthdaySetupValidator>("Birthday Setup Validator");
        window.minSize = new Vector2(400, 600);
    }

    private void OnGUI()
    {
        GUILayout.Label("Birthday Setup UI Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 自動検索ボタン
        if (GUILayout.Button("Find BirthdaySetupUI in Scene", GUILayout.Height(30)))
        {
            FindBirthdaySetupUI();
        }

        EditorGUILayout.Space();

        // ターゲットUI表示
        EditorGUI.BeginDisabledGroup(true);
        targetUI = (BirthdaySetupUI)EditorGUILayout.ObjectField("Target UI", targetUI, typeof(BirthdaySetupUI), true);
        EditorGUI.EndDisabledGroup();

        if (targetUI == null)
        {
            EditorGUILayout.HelpBox("No BirthdaySetupUI found. Please open StartMenuScen.unity and click 'Find BirthdaySetupUI in Scene'.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // 検証結果表示
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DisplayValidationResults();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 自動修復ボタン
        if (GUILayout.Button("Auto-Fix Missing References", GUILayout.Height(40)))
        {
            AutoFixReferences();
        }
    }

    private void FindBirthdaySetupUI()
    {
        // 非アクティブなオブジェクトも含めて検索
        BirthdaySetupUI[] allInstances = FindObjectsByType<BirthdaySetupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (allInstances != null && allInstances.Length > 0)
        {
            targetUI = allInstances[0];
            Debug.Log($"[BirthdaySetupValidator] Found BirthdaySetupUI on GameObject: {targetUI.gameObject.name}");
            Selection.activeGameObject = targetUI.gameObject;
        }
        else
        {
            EditorUtility.DisplayDialog("Not Found",
                "BirthdaySetupUI not found in current scene. Please open StartMenuScen.unity first.",
                "OK");
        }
    }

    private void DisplayValidationResults()
    {
        GUILayout.Label("Validation Results", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        SerializedObject so = new SerializedObject(targetUI);

        // Name Input
        EditorGUILayout.LabelField("Name Input", EditorStyles.boldLabel);
        CheckField(so, "nameInputField", "Name Input Field");
        CheckField(so, "nameLabel", "Name Label");
        EditorGUILayout.Space();

        // Dropdowns
        EditorGUILayout.LabelField("Dropdowns", EditorStyles.boldLabel);
        CheckField(so, "yearDropdown", "Year Dropdown");
        CheckField(so, "monthDropdown", "Month Dropdown");
        CheckField(so, "dayDropdown", "Day Dropdown");
        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);
        CheckField(so, "nextButton", "Next Button");
        CheckField(so, "skipButton", "Skip Button");
        CheckField(so, "startGameButton", "Start Game Button");
        EditorGUILayout.Space();

        // Text Fields
        EditorGUILayout.LabelField("Text Fields", EditorStyles.boldLabel);
        CheckField(so, "statusText", "Status Text");
        CheckField(so, "titleText", "Title Text");
        CheckField(so, "subtitleText", "Subtitle Text");
        CheckField(so, "nextButtonText", "Next Button Text");
        CheckField(so, "skipButtonText", "Skip Button Text");
        CheckField(so, "yearLabel", "Year Label");
        CheckField(so, "monthLabel", "Month Label");
        CheckField(so, "dayLabel", "Day Label");
        EditorGUILayout.Space();

        // Panels
        EditorGUILayout.LabelField("Panels", EditorStyles.boldLabel);
        CheckField(so, "birthdayPanel", "Birthday Panel");
        CheckField(so, "readyPanel", "Ready Panel");
    }

    private void CheckField(SerializedObject so, string propertyName, string displayName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            bool isNull = prop.objectReferenceValue == null;
            Color prevColor = GUI.backgroundColor;

            if (isNull)
            {
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Light red
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("❌ " + displayName, GUILayout.Width(200));
                EditorGUILayout.LabelField("MISSING", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); // Light green
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("✅ " + displayName, GUILayout.Width(200));
                EditorGUILayout.ObjectField(prop.objectReferenceValue, typeof(Object), false);
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = prevColor;
        }
    }

    private void AutoFixReferences()
    {
        if (targetUI == null)
        {
            EditorUtility.DisplayDialog("Error", "No BirthdaySetupUI target found.", "OK");
            return;
        }

        Undo.RecordObject(targetUI, "Auto-Fix BirthdaySetupUI References");

        SerializedObject so = new SerializedObject(targetUI);
        Transform root = targetUI.transform;
        int fixedCount = 0;

        // BirthdayPanel（親オブジェクト）
        GameObject birthdayPanel = root.gameObject;

        // Name Inputを検索
        fixedCount += TryFindAndAssign<TMP_InputField>(so, "nameInputField", birthdayPanel, "NameInput");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "nameLabel", birthdayPanel, "NameLabel");

        // Dropdownsを検索
        fixedCount += TryFindAndAssign<TMP_Dropdown>(so, "yearDropdown", birthdayPanel, "YearDropdown");
        fixedCount += TryFindAndAssign<TMP_Dropdown>(so, "monthDropdown", birthdayPanel, "MonthDropdown");
        fixedCount += TryFindAndAssign<TMP_Dropdown>(so, "dayDropdown", birthdayPanel, "DayDropdown");

        // Buttonsを検索
        fixedCount += TryFindAndAssign<Button>(so, "nextButton", birthdayPanel, "NextButton");
        fixedCount += TryFindAndAssign<Button>(so, "skipButton", birthdayPanel, "SkipButton");

        // StartGameButtonはReadyPanel内にあるはず
        GameObject readyPanelObj = FindGameObjectByName("ReadyPanel");
        if (readyPanelObj != null)
        {
            // ReadyPanel内のボタンを検索（様々な名前の可能性）
            Button[] buttonsInReady = readyPanelObj.GetComponentsInChildren<Button>(true);
            SerializedProperty startGameBtnProp = so.FindProperty("startGameButton");
            if (startGameBtnProp != null && startGameBtnProp.objectReferenceValue == null && buttonsInReady.Length > 0)
            {
                // ReadyPanel内の最初のボタンをStartGameButtonとして使用
                startGameBtnProp.objectReferenceValue = buttonsInReady[0];
                fixedCount++;
                Debug.Log($"[BirthdaySetupValidator] Assigned startGameButton = {buttonsInReady[0].name}");
            }
        }

        // TextMeshProUGUIを検索
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "statusText", birthdayPanel, "StatusText");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "titleText", birthdayPanel, "TitleText");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "subtitleText", birthdayPanel, "SubtitleText");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "yearLabel", birthdayPanel, "YearLabel");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "monthLabel", birthdayPanel, "MonthLabel");
        fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "dayLabel", birthdayPanel, "DayLabel");

        // ボタン内のテキストを検索
        Button nextBtn = so.FindProperty("nextButton").objectReferenceValue as Button;
        if (nextBtn != null)
        {
            fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "nextButtonText", nextBtn.gameObject, "Text");
        }

        Button skipBtn = so.FindProperty("skipButton").objectReferenceValue as Button;
        if (skipBtn != null)
        {
            fixedCount += TryFindAndAssign<TextMeshProUGUI>(so, "skipButtonText", skipBtn.gameObject, "Text");
        }

        // Panelsを検索（必ず再アサインする - nullの可能性が高いため）
        SerializedProperty birthdayPanelProp = so.FindProperty("birthdayPanel");
        SerializedProperty readyPanelProp = so.FindProperty("readyPanel");

        // BirthdayPanel（通常は自分自身）
        if (birthdayPanelProp != null)
        {
            GameObject bPanel = FindGameObjectByName("BirthdayPanel");
            if (bPanel != null)
            {
                birthdayPanelProp.objectReferenceValue = bPanel;
                fixedCount++;
                Debug.Log($"[BirthdaySetupValidator] Assigned birthdayPanel = BirthdayPanel");
            }
        }

        // ReadyPanel（重要！）
        if (readyPanelProp != null)
        {
            GameObject rPanel = FindGameObjectByName("ReadyPanel");
            if (rPanel != null)
            {
                readyPanelProp.objectReferenceValue = rPanel;
                fixedCount++;
                Debug.Log($"[BirthdaySetupValidator] Assigned readyPanel = ReadyPanel");
            }
            else
            {
                Debug.LogWarning("[BirthdaySetupValidator] ReadyPanel not found in scene!");
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetUI);

        EditorUtility.DisplayDialog("Auto-Fix Complete",
            $"Fixed {fixedCount} reference(s).\n\nPlease review the Inspector to verify all references are correct.",
            "OK");

        Debug.Log($"[BirthdaySetupValidator] Auto-fix complete. Fixed {fixedCount} references.");
    }

    private int TryFindAndAssign<T>(SerializedObject so, string propertyName, GameObject parent, string childName) where T : Component
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || prop.objectReferenceValue != null)
            return 0;

        Transform child = parent.transform.Find(childName);
        if (child != null)
        {
            T component = child.GetComponent<T>();
            if (component != null)
            {
                prop.objectReferenceValue = component;
                Debug.Log($"[BirthdaySetupValidator] Assigned {propertyName} = {childName}");
                return 1;
            }
        }

        // 再帰的に子オブジェクトを検索
        T foundComponent = parent.GetComponentInChildren<T>(true);
        if (foundComponent != null && foundComponent.gameObject.name.Contains(childName.Replace("Dropdown", "").Replace("Button", "").Replace("Text", "").Replace("Label", "")))
        {
            prop.objectReferenceValue = foundComponent;
            Debug.Log($"[BirthdaySetupValidator] Assigned {propertyName} = {foundComponent.gameObject.name}");
            return 1;
        }

        return 0;
    }

    private int TryFindAndAssignGameObject(SerializedObject so, string propertyName, string objectName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || prop.objectReferenceValue != null)
            return 0;

        // 非アクティブなオブジェクトも含めて検索
        GameObject obj = FindGameObjectByName(objectName);
        if (obj != null)
        {
            prop.objectReferenceValue = obj;
            Debug.Log($"[BirthdaySetupValidator] Assigned {propertyName} = {objectName}");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// 非アクティブなオブジェクトも含めてGameObjectを名前で検索
    /// </summary>
    private GameObject FindGameObjectByName(string name)
    {
        // シーン内の全てのTransformを取得（非アクティブも含む）
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            // シーン内のオブジェクトのみ（Prefabは除外）
            if (t.gameObject.scene.name != null && t.name == name)
            {
                Debug.Log($"[BirthdaySetupValidator] Found {name} at path: {GetGameObjectPath(t.gameObject)}");
                return t.gameObject;
            }
        }
        Debug.LogWarning($"[BirthdaySetupValidator] Could not find GameObject with name: {name}");
        return null;
    }

    /// <summary>
    /// GameObjectのヒエラルキーパスを取得
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
