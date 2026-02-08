using UnityEngine;
using UnityEditor;
using FPSTrump.Psychology;

/// <summary>
/// BluffSystemの自動セットアップ
/// Tools > Baba > Setup BluffSystem から実行
/// </summary>
public class BluffSystemAutoSetup : EditorWindow
{
    private float baseBluffChance = 0.3f;
    private float maxBluffChance = 0.7f;
    private int turnsBeforeBluffing = 2;

    // セットアップチェック結果
    private bool hasPsychologySystem = false;
    private bool hasBehaviorAnalyzer = false;
    private bool hasFloatingTextSystem = false;
    private bool hasExistingBluffSystem = false;
    private bool hasLLMManager = false;
    private bool setupCheckDone = false;

    [MenuItem("Tools/Baba/Setup BluffSystem")]
    public static void ShowWindow()
    {
        var window = GetWindow<BluffSystemAutoSetup>("BluffSystem Setup");
        window.minSize = new Vector2(420, 400);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BluffSystem Auto Setup (Stage 5)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはStage 5のブラフシステムを自動セットアップします。\n" +
            "BluffSystem GameObjectを作成し、依存コンポーネントへの参照を自動設定します。",
            MessageType.Info);

        EditorGUILayout.Space();

        // === セットアップチェック ===
        DrawSetupCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // === ブラフ設定 ===
        EditorGUILayout.LabelField("Bluff Settings", EditorStyles.boldLabel);

        baseBluffChance = EditorGUILayout.Slider("Base Bluff Chance", baseBluffChance, 0f, 1f);
        maxBluffChance = EditorGUILayout.Slider("Max Bluff Chance", maxBluffChance, 0f, 1f);
        turnsBeforeBluffing = EditorGUILayout.IntSlider("Turns Before Bluffing", turnsBeforeBluffing, 0, 10);

        EditorGUILayout.Space();

        // === セットアップボタン ===
        GUI.enabled = setupCheckDone;
        if (GUILayout.Button("Setup BluffSystem", GUILayout.Height(40)))
        {
            SetupBluffSystem();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        // === リフレッシュ ===
        if (GUILayout.Button("Refresh Setup Check"))
        {
            RefreshSetupCheck();
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "セットアップ後、プレイモードで動作確認：\n" +
            "- ターン1-2: intent=Honest（序盤）\n" +
            "- ターン3+: ブラフが時折発生\n" +
            "- POST_REACT/REACT: FloatingTextでセリフ表示\n" +
            "- Debug GUI（左下）にブラフ統計表示",
            MessageType.Info);
    }

    private void DrawSetupCheck()
    {
        EditorGUILayout.LabelField("Setup Check", EditorStyles.boldLabel);

        DrawCheckItem("BluffSystem", hasExistingBluffSystem, "既存のBluffSystemが見つかりました（更新されます）", "新規作成されます");
        DrawCheckItem("PsychologySystem", hasPsychologySystem, "OK", "自動作成されます");
        DrawCheckItem("PlayerBehaviorAnalyzer", hasBehaviorAnalyzer, "OK", "自動作成されます");
        DrawCheckItem("FloatingTextSystem", hasFloatingTextSystem, "OK", "自動作成されます");
        DrawCheckItem("LLMManager", hasLLMManager, "OK（Layer B/C有効）", "なし（Layer Aのみ）");

        EditorGUILayout.Space();

        bool allFound = hasPsychologySystem && hasBehaviorAnalyzer && hasFloatingTextSystem;
        if (allFound)
        {
            EditorGUILayout.HelpBox("All dependencies found! Ready to setup.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "一部の依存コンポーネントが見つかりません。\n" +
                "セットアップ実行時に自動作成されます。",
                MessageType.Info);
        }
    }

    private void DrawCheckItem(string name, bool found, string foundMsg, string notFoundMsg)
    {
        EditorGUILayout.BeginHorizontal();

        if (found)
        {
            EditorGUILayout.LabelField($"  {name}", EditorStyles.label);
            GUIStyle greenStyle = new GUIStyle(EditorStyles.label);
            greenStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            EditorGUILayout.LabelField(foundMsg, greenStyle);
        }
        else
        {
            EditorGUILayout.LabelField($"  {name}", EditorStyles.label);
            GUIStyle yellowStyle = new GUIStyle(EditorStyles.label);
            yellowStyle.normal.textColor = new Color(0.9f, 0.7f, 0.1f);
            EditorGUILayout.LabelField(notFoundMsg, yellowStyle);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshSetupCheck()
    {
        hasPsychologySystem = FindFirstObjectByType<PsychologySystem>() != null;
        hasBehaviorAnalyzer = FindFirstObjectByType<PlayerBehaviorAnalyzer>() != null;
        hasFloatingTextSystem = FindFirstObjectByType<FloatingTextSystem>() != null;
        hasExistingBluffSystem = FindFirstObjectByType<BluffSystem>() != null;
        hasLLMManager = FindFirstObjectByType<FPSTrump.AI.LLM.LLMManager>() != null;
        setupCheckDone = true;

        Repaint();
    }

    private void SetupBluffSystem()
    {
        if (!EditorUtility.DisplayDialog("BluffSystem Setup",
            "BluffSystemをセットアップします。\n\n" +
            (hasExistingBluffSystem ? "既存のBluffSystemが更新されます。" : "新しいBluffSystem GameObjectが作成されます。") +
            "\n\n続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup BluffSystem");

        try
        {
            GameObject bluffSystemObj = SetupBluffSystemGameObject();

            Undo.CollapseUndoOperations(undoGroup);

            string resultMessage = "BluffSystemのセットアップが完了しました！\n\n" +
                "作成/更新されたオブジェクト:\n" +
                "- BluffSystem GameObject\n";

            if (!hasPsychologySystem) resultMessage += "- PsychologySystem GameObject（自動作成）\n";
            if (!hasBehaviorAnalyzer) resultMessage += "- PlayerBehaviorAnalyzer（自動作成）\n";
            if (!hasFloatingTextSystem) resultMessage += "- FloatingTextSystem GameObject（自動作成）\n";

            resultMessage += "\n設定済み参照:\n" +
                "- PsychologySystem\n" +
                "- PlayerBehaviorAnalyzer\n" +
                "- FloatingTextSystem\n";

            EditorUtility.DisplayDialog("Success", resultMessage, "OK");

            Debug.Log("[BluffSystemAutoSetup] Setup completed successfully!");

            Selection.activeGameObject = bluffSystemObj;
            RefreshSetupCheck();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BluffSystemAutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
        }
    }

    private GameObject SetupBluffSystemGameObject()
    {
        // 既存のBluffSystemを検索
        var existingBluffSystem = FindFirstObjectByType<BluffSystem>();
        GameObject bluffSystemObj;

        if (existingBluffSystem != null)
        {
            bluffSystemObj = existingBluffSystem.gameObject;
            Debug.Log("[BluffSystemAutoSetup] Found existing BluffSystem, updating...");
        }
        else
        {
            bluffSystemObj = new GameObject("BluffSystem");
            Undo.RegisterCreatedObjectUndo(bluffSystemObj, "Create BluffSystem");
            Debug.Log("[BluffSystemAutoSetup] Created new BluffSystem GameObject");
        }

        // BluffSystemコンポーネント追加/取得
        var bluffSystemComponent = bluffSystemObj.GetComponent<BluffSystem>();
        if (bluffSystemComponent == null)
        {
            bluffSystemComponent = Undo.AddComponent<BluffSystem>(bluffSystemObj);
        }

        // === 依存コンポーネントの自動作成 ===

        // PsychologySystem（無い場合は作成）
        var psychologySystem = FindFirstObjectByType<PsychologySystem>();
        if (psychologySystem == null)
        {
            var psyObj = new GameObject("PsychologySystem");
            Undo.RegisterCreatedObjectUndo(psyObj, "Create PsychologySystem");
            psychologySystem = Undo.AddComponent<PsychologySystem>(psyObj);
            // PlayerBehaviorAnalyzerも同じGameObjectに追加（PsychologySystem.ValidateComponents()がGetComponent<>()で検出するため）
            var analyzer = Undo.AddComponent<PlayerBehaviorAnalyzer>(psyObj);
            // PsychologySystemのbehaviorAnalyzer参照を設定
            var psySO = new SerializedObject(psychologySystem);
            psySO.FindProperty("behaviorAnalyzer").objectReferenceValue = analyzer;
            psySO.ApplyModifiedProperties();
            Debug.Log("[BluffSystemAutoSetup] Created PsychologySystem + PlayerBehaviorAnalyzer");
        }

        // PlayerBehaviorAnalyzer（PsychologySystemはあるが単独で不足の場合）
        var behaviorAnalyzer = FindFirstObjectByType<PlayerBehaviorAnalyzer>();
        if (behaviorAnalyzer == null && psychologySystem != null)
        {
            behaviorAnalyzer = Undo.AddComponent<PlayerBehaviorAnalyzer>(psychologySystem.gameObject);
            var psySO = new SerializedObject(psychologySystem);
            psySO.FindProperty("behaviorAnalyzer").objectReferenceValue = behaviorAnalyzer;
            psySO.ApplyModifiedProperties();
            Debug.Log("[BluffSystemAutoSetup] Added PlayerBehaviorAnalyzer to PsychologySystem");
        }

        // FloatingTextSystem（無い場合は作成）
        var floatingTextSystem = FindFirstObjectByType<FloatingTextSystem>();
        if (floatingTextSystem == null)
        {
            var ftsObj = new GameObject("FloatingTextSystem");
            Undo.RegisterCreatedObjectUndo(ftsObj, "Create FloatingTextSystem");
            floatingTextSystem = Undo.AddComponent<FloatingTextSystem>(ftsObj);
            Debug.Log("[BluffSystemAutoSetup] Created FloatingTextSystem");
        }

        // === BluffSystemの参照設定 ===
        SerializedObject so = new SerializedObject(bluffSystemComponent);

        so.FindProperty("psychologySystem").objectReferenceValue = psychologySystem;
        so.FindProperty("behaviorAnalyzer").objectReferenceValue = behaviorAnalyzer ?? FindFirstObjectByType<PlayerBehaviorAnalyzer>();
        so.FindProperty("floatingTextSystem").objectReferenceValue = floatingTextSystem;

        // LLMManager参照設定（存在する場合のみ）
        var llmManager = FindFirstObjectByType<FPSTrump.AI.LLM.LLMManager>();
        if (llmManager != null)
        {
            so.FindProperty("llmManager").objectReferenceValue = llmManager;
            Debug.Log("[BluffSystemAutoSetup] LLMManager reference set (Layer B/C enabled)");
        }
        else
        {
            Debug.Log("[BluffSystemAutoSetup] LLMManager not found (Layer A only mode)");
        }

        // 期待設定の適用
        so.FindProperty("baseExpectationChance").floatValue = baseBluffChance;
        so.FindProperty("maxExpectationChance").floatValue = maxBluffChance;
        so.FindProperty("turnsBeforeExpectation").intValue = turnsBeforeBluffing;

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(bluffSystemComponent);

        return bluffSystemObj;
    }
}
