using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// FloatingTextSystemの自動セットアップ
/// Tools > Baba > Setup FloatingText System から実行
/// </summary>
public class FloatingTextAutoSetup : EditorWindow
{
    private GameObject psychologySystemObj;
    private int poolSize = 10;
    private TMP_FontAsset fontAsset;
    private float fontSize = 3f;

    [MenuItem("Tools/Baba/Setup FloatingText System")]
    public static void ShowWindow()
    {
        var window = GetWindow<FloatingTextAutoSetup>("FloatingText Setup");
        window.minSize = new Vector2(400, 350);
        window.Show();
    }

    private void OnEnable()
    {
        // シーンから既存のコンポーネントを検索
        FindExistingComponents();
    }

    private void FindExistingComponents()
    {
        // PsychologySystemを検索
        var psychologySystem = FindObjectOfType<FPSTrump.Psychology.PsychologySystem>();
        if (psychologySystem != null)
        {
            psychologySystemObj = psychologySystem.gameObject;
        }

        // 日本語対応フォントを優先的に検索
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");

        // まずNotoSansJPを探す
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("NotoSansJP") || path.Contains("NotoSans"))
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Debug.Log($"[FloatingTextAutoSetup] Found Japanese font: {path}");
                return;
            }
        }

        // NotoSansJPが見つからない場合は、最初のフォントを使用
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Debug.LogWarning($"[FloatingTextAutoSetup] NotoSansJP not found, using fallback font: {path}");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("FloatingText System Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはPhase 4 Stage 3のFloatingTextSystemを自動セットアップします。\n" +
            "FloatingTextSystem GameObjectと、PsychologySystemとの接続を行います。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 設定項目
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        poolSize = EditorGUILayout.IntField("Pool Size", poolSize);
        fontSize = EditorGUILayout.FloatField("Font Size", fontSize);
        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset (Required)", fontAsset, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        psychologySystemObj = (GameObject)EditorGUILayout.ObjectField("Psychology System", psychologySystemObj, typeof(GameObject), true);

        EditorGUILayout.Space();

        // セットアップボタン
        GUI.enabled = poolSize > 0 && fontSize > 0 && fontAsset != null;

        if (GUILayout.Button("Setup FloatingText System", GUILayout.Height(40)))
        {
            SetupFloatingTextSystem();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        if (poolSize <= 0 || fontSize <= 0)
        {
            EditorGUILayout.HelpBox("Pool SizeとFont Sizeは0より大きい値を設定してください。", MessageType.Warning);
        }

        if (fontAsset == null)
        {
            EditorGUILayout.HelpBox("Font Assetは必須です。NotoSansJP-VariableFont_wght SDF を設定してください。", MessageType.Error);
        }
    }

    private void SetupFloatingTextSystem()
    {
        if (!EditorUtility.DisplayDialog("FloatingText Setup",
            "FloatingTextSystemをセットアップします。\n既存の設定は上書きされます。\n\n続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup FloatingText System");

        try
        {
            // 1. FloatingTextSystem GameObjectを作成/取得
            GameObject floatingTextObj = SetupFloatingTextGameObject();

            // 2. PsychologySystemと接続
            if (psychologySystemObj != null)
            {
                ConnectToPsychologySystem(floatingTextObj);
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success",
                "FloatingTextSystemのセットアップが完了しました！\n\n" +
                "作成されたオブジェクト:\n" +
                "- FloatingTextSystem GameObject\n" +
                (psychologySystemObj != null ? "- PsychologySystem接続完了\n" : "") +
                "\n" +
                "FloatingTextSystemのInspectorで詳細設定を確認してください。",
                "OK");

            Debug.Log("[FloatingTextAutoSetup] Setup completed successfully!");

            // FloatingTextSystemを選択状態にする
            Selection.activeGameObject = floatingTextObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FloatingTextAutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
        }
    }

    private GameObject SetupFloatingTextGameObject()
    {
        // 既存のFloatingTextSystemを検索
        var existingSystem = FindObjectOfType<FloatingTextSystem>();
        GameObject floatingTextObj;

        if (existingSystem != null)
        {
            floatingTextObj = existingSystem.gameObject;
            Debug.Log("[FloatingTextAutoSetup] Found existing FloatingTextSystem");
        }
        else
        {
            // 新規作成
            floatingTextObj = new GameObject("FloatingTextSystem");
            Undo.RegisterCreatedObjectUndo(floatingTextObj, "Create FloatingTextSystem");
            Debug.Log("[FloatingTextAutoSetup] Created new FloatingTextSystem");
        }

        // FloatingTextSystemコンポーネントを追加/取得
        var floatingTextSystem = floatingTextObj.GetComponent<FloatingTextSystem>();
        if (floatingTextSystem == null)
        {
            floatingTextSystem = Undo.AddComponent<FloatingTextSystem>(floatingTextObj);
        }

        // SerializedObjectで設定を適用
        SerializedObject so = new SerializedObject(floatingTextSystem);

        so.FindProperty("poolSize").intValue = poolSize;
        so.FindProperty("fontSize").floatValue = fontSize;
        so.FindProperty("fontAsset").objectReferenceValue = fontAsset;

        // アニメーション設定のデフォルト値
        so.FindProperty("floatHeight").floatValue = 0.5f;
        so.FindProperty("floatDuration").floatValue = 1.5f;
        so.FindProperty("fadeInDuration").floatValue = 0.3f;
        so.FindProperty("fadeOutDuration").floatValue = 0.5f;

        // 色設定のデフォルト値
        so.FindProperty("lowPressureColor").colorValue = Color.white;
        so.FindProperty("mediumPressureColor").colorValue = new Color(1f, 0.8f, 0.3f); // Orange
        so.FindProperty("highPressureColor").colorValue = new Color(1f, 0.2f, 0.2f); // Red

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(floatingTextSystem);

        return floatingTextObj;
    }

    private void ConnectToPsychologySystem(GameObject floatingTextObj)
    {
        var psychologySystem = psychologySystemObj.GetComponent<FPSTrump.Psychology.PsychologySystem>();
        if (psychologySystem == null)
        {
            Debug.LogWarning("[FloatingTextAutoSetup] PsychologySystem component not found on GameObject");
            return;
        }

        var floatingTextSystem = floatingTextObj.GetComponent<FloatingTextSystem>();
        if (floatingTextSystem == null)
        {
            Debug.LogError("[FloatingTextAutoSetup] FloatingTextSystem component not found");
            return;
        }

        // PsychologySystemのSerializedObjectを取得
        SerializedObject so = new SerializedObject(psychologySystem);

        // floatingTextSystemフィールドに参照を設定
        SerializedProperty floatingTextProp = so.FindProperty("floatingTextSystem");
        if (floatingTextProp != null)
        {
            floatingTextProp.objectReferenceValue = floatingTextSystem;
            so.ApplyModifiedProperties();

            Debug.Log("[FloatingTextAutoSetup] Connected FloatingTextSystem to PsychologySystem");
        }
        else
        {
            Debug.LogWarning("[FloatingTextAutoSetup] Could not find floatingTextSystem field in PsychologySystem");
        }

        EditorUtility.SetDirty(psychologySystem);
    }
}
