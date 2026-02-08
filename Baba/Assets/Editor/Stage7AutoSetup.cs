using UnityEngine;
using UnityEditor;
using FPSTrump.AI.LLM;
using FPSTrump.Result;

/// <summary>
/// Stage 7 リザルト診断システムの自動セットアップ
/// Tools > Baba > Setup Stage 7 から実行
/// </summary>
public class Stage7AutoSetup : EditorWindow
{
    private bool hasGameManager = false;
    private bool hasGameSessionRecorder = false;
    private bool hasResultDiagnosisSystem = false;
    private bool hasResultUI = false;
    private bool hasLLMManager = false;
    private bool setupCheckDone = false;

    [MenuItem("Tools/Baba/Setup Stage 7")]
    public static void ShowWindow()
    {
        var window = GetWindow<Stage7AutoSetup>("Stage 7 Setup");
        window.minSize = new Vector2(420, 350);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage 7 Auto Setup (Result Diagnosis)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはStage 7のリザルト診断システムを自動セットアップします。\n" +
            "GameSessionRecorder, ResultDiagnosisSystem, ResultUI を作成し、\n" +
            "GameManagerへの参照を自動設定します。",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawSetupCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        GUI.enabled = setupCheckDone && hasGameManager;
        if (GUILayout.Button("Setup Stage 7", GUILayout.Height(40)))
        {
            SetupStage7();
        }
        GUI.enabled = true;

        if (!hasGameManager)
        {
            EditorGUILayout.HelpBox("GameManagerがシーンに存在しません。先にゲームシーンを開いてください。", MessageType.Error);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Setup Check"))
        {
            RefreshSetupCheck();
        }
    }

    private void DrawSetupCheck()
    {
        EditorGUILayout.LabelField("Setup Check", EditorStyles.boldLabel);

        DrawCheckItem("GameManager", hasGameManager, "OK", "見つかりません");
        DrawCheckItem("GameSessionRecorder", hasGameSessionRecorder, "既存あり（スキップ）", "自動作成されます");
        DrawCheckItem("ResultDiagnosisSystem", hasResultDiagnosisSystem, "既存あり（スキップ）", "自動作成されます");
        DrawCheckItem("ResultUI", hasResultUI, "既存あり（スキップ）", "自動作成されます");
        DrawCheckItem("LLMManager", hasLLMManager, "OK（LLM診断有効）", "なし（フォールバックのみ）");
    }

    private void DrawCheckItem(string name, bool found, string foundMsg, string notFoundMsg)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"  {name}", EditorStyles.label);

        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.normal.textColor = found ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.7f, 0.1f);
        EditorGUILayout.LabelField(found ? foundMsg : notFoundMsg, style);

        EditorGUILayout.EndHorizontal();
    }

    private void RefreshSetupCheck()
    {
        hasGameManager = FindFirstObjectByType<GameManager>() != null;
        hasGameSessionRecorder = FindFirstObjectByType<GameSessionRecorder>() != null;
        hasResultDiagnosisSystem = FindFirstObjectByType<ResultDiagnosisSystem>() != null;
        hasResultUI = FindFirstObjectByType<ResultUI>() != null;
        hasLLMManager = FindFirstObjectByType<LLMManager>() != null;
        setupCheckDone = true;
        Repaint();
    }

    private void SetupStage7()
    {
        if (!EditorUtility.DisplayDialog("Stage 7 Setup",
            "Stage 7のリザルト診断システムをセットアップします。\n\n" +
            "以下が作成/設定されます:\n" +
            "- GameSessionRecorder\n" +
            "- ResultDiagnosisSystem\n" +
            "- ResultUI\n" +
            "- GameManager.resultUI参照\n\n" +
            "続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Stage 7");

        try
        {
            string resultMessage = "Stage 7セットアップ完了！\n\n";

            // 1. GameSessionRecorder
            var recorder = FindFirstObjectByType<GameSessionRecorder>();
            if (recorder == null)
            {
                var recorderObj = new GameObject("GameSessionRecorder");
                Undo.RegisterCreatedObjectUndo(recorderObj, "Create GameSessionRecorder");
                recorder = Undo.AddComponent<GameSessionRecorder>(recorderObj);
                resultMessage += "- GameSessionRecorder 作成\n";
            }
            else
            {
                resultMessage += "- GameSessionRecorder 既存（スキップ）\n";
            }

            // 2. ResultDiagnosisSystem
            var diagnosisSystem = FindFirstObjectByType<ResultDiagnosisSystem>();
            if (diagnosisSystem == null)
            {
                var diagObj = new GameObject("ResultDiagnosisSystem");
                Undo.RegisterCreatedObjectUndo(diagObj, "Create ResultDiagnosisSystem");
                diagnosisSystem = Undo.AddComponent<ResultDiagnosisSystem>(diagObj);
                resultMessage += "- ResultDiagnosisSystem 作成\n";
            }
            else
            {
                resultMessage += "- ResultDiagnosisSystem 既存（スキップ）\n";
            }

            // ResultDiagnosisSystem に LLMManager参照を設定
            var llmManager = FindFirstObjectByType<LLMManager>();
            if (llmManager != null)
            {
                var diagSO = new SerializedObject(diagnosisSystem);
                diagSO.FindProperty("llmManager").objectReferenceValue = llmManager;
                diagSO.ApplyModifiedProperties();
                resultMessage += "- LLMManager参照 設定済み\n";
            }

            // 3. ResultUI
            var resultUI = FindFirstObjectByType<ResultUI>();
            if (resultUI == null)
            {
                var resultUIObj = new GameObject("ResultUI");
                Undo.RegisterCreatedObjectUndo(resultUIObj, "Create ResultUI");
                resultUI = Undo.AddComponent<ResultUI>(resultUIObj);
                resultMessage += "- ResultUI 作成\n";
            }
            else
            {
                resultMessage += "- ResultUI 既存（スキップ）\n";
            }

            // 4. GameManager.resultUI 参照設定
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                var gmSO = new SerializedObject(gameManager);
                gmSO.FindProperty("resultUI").objectReferenceValue = resultUI;
                gmSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameManager);
                resultMessage += "- GameManager.resultUI 参照設定済み\n";
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success", resultMessage, "OK");
            Debug.Log("[Stage7AutoSetup] Setup completed successfully!");

            RefreshSetupCheck();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Stage7AutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
        }
    }
}
