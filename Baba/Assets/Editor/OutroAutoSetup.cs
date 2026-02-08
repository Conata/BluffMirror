using UnityEngine;
using UnityEditor;

/// <summary>
/// GameOutroSequenceの自動セットアップ
/// Tools > Baba > Setup Outro Sequence から実行
/// </summary>
public class OutroAutoSetup : EditorWindow
{
    private bool hasGameManager;
    private bool hasGameOutroSequence;
    private bool hasGameIntroSequence;
    private bool hasCameraSystem;
    private bool hasTVHeadAnimator;
    private bool hasSubtitleUI;
    private bool setupCheckDone;

    [MenuItem("Tools/Baba/Setup Outro Sequence")]
    public static void ShowWindow()
    {
        var window = GetWindow<OutroAutoSetup>("Outro Setup");
        window.minSize = new Vector2(420, 320);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Outro Sequence Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "GameOutroSequenceをシーンに自動セットアップします。\n" +
            "ゲーム終了時にAIが性格を口頭で暴くOutro演出を追加します。\n" +
            "依存コンポーネントの参照は自動で設定されます。",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawSetupCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        GUI.enabled = setupCheckDone && hasGameManager;
        if (GUILayout.Button("Setup Outro Sequence", GUILayout.Height(40)))
        {
            SetupOutro();
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
        DrawCheckItem("GameOutroSequence", hasGameOutroSequence, "既存あり（スキップ）", "自動作成されます");
        DrawCheckItem("GameIntroSequence", hasGameIntroSequence, "OK（同階層に配置）", "なし（ルートに作成）");
        DrawCheckItem("CameraCinematicsSystem", hasCameraSystem, "OK（自動参照）", "なし（Outro中カメラ演出なし）");
        DrawCheckItem("TVHeadAnimator", hasTVHeadAnimator, "OK（自動参照）", "なし（Outro中表情演出なし）");
        DrawCheckItem("SubtitleUI", hasSubtitleUI, "OK（自動参照）", "なし（テキスト表示なし）");
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
        hasGameOutroSequence = FindFirstObjectByType<GameOutroSequence>() != null;
        hasGameIntroSequence = FindFirstObjectByType<GameIntroSequence>() != null;
        hasCameraSystem = FindFirstObjectByType<CameraCinematicsSystem>() != null;
        hasTVHeadAnimator = FindFirstObjectByType<TVHeadAnimator>() != null;
        hasSubtitleUI = FindFirstObjectByType<SubtitleUI>() != null;
        setupCheckDone = true;
        Repaint();
    }

    private void SetupOutro()
    {
        if (!EditorUtility.DisplayDialog("Outro Setup",
            "GameOutroSequenceをセットアップします。\n\n" +
            "以下が作成/設定されます:\n" +
            "- GameOutroSequence コンポーネント\n" +
            "- 依存コンポーネント参照の自動設定\n\n" +
            "続行しますか？",
            "Yes", "Cancel"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Outro Sequence");

        try
        {
            string resultMessage = "Outroセットアップ完了！\n\n";

            // GameOutroSequence
            var outroSequence = FindFirstObjectByType<GameOutroSequence>();
            if (outroSequence == null)
            {
                // GameIntroSequenceと同じ親に配置（あれば）
                var introSequence = FindFirstObjectByType<GameIntroSequence>();
                GameObject outroObj;

                if (introSequence != null && introSequence.transform.parent != null)
                {
                    outroObj = new GameObject("GameOutroSequence");
                    Undo.RegisterCreatedObjectUndo(outroObj, "Create GameOutroSequence");
                    outroObj.transform.SetParent(introSequence.transform.parent);
                    resultMessage += $"- GameOutroSequence 作成（{introSequence.transform.parent.name} 配下）\n";
                }
                else if (introSequence != null)
                {
                    // IntroSequenceと同階層（ルート）に配置
                    outroObj = new GameObject("GameOutroSequence");
                    Undo.RegisterCreatedObjectUndo(outroObj, "Create GameOutroSequence");
                    resultMessage += "- GameOutroSequence 作成（IntroSequenceと同階層）\n";
                }
                else
                {
                    outroObj = new GameObject("GameOutroSequence");
                    Undo.RegisterCreatedObjectUndo(outroObj, "Create GameOutroSequence");
                    resultMessage += "- GameOutroSequence 作成（ルート）\n";
                }

                outroSequence = Undo.AddComponent<GameOutroSequence>(outroObj);
            }
            else
            {
                resultMessage += "- GameOutroSequence 既存（スキップ）\n";
            }

            // SerializedObjectで依存参照を設定
            var so = new SerializedObject(outroSequence);

            var cameraSystem = FindFirstObjectByType<CameraCinematicsSystem>();
            if (cameraSystem != null)
            {
                so.FindProperty("cameraSystem").objectReferenceValue = cameraSystem;
                resultMessage += "- CameraCinematicsSystem 参照設定済み\n";
            }

            var tvHead = FindFirstObjectByType<TVHeadAnimator>();
            if (tvHead != null)
            {
                so.FindProperty("tvHeadAnimator").objectReferenceValue = tvHead;
                resultMessage += "- TVHeadAnimator 参照設定済み\n";
            }

            var subtitleUI = FindFirstObjectByType<SubtitleUI>();
            if (subtitleUI != null)
            {
                so.FindProperty("subtitleUI").objectReferenceValue = subtitleUI;
                resultMessage += "- SubtitleUI 参照設定済み\n";
            }

            var llmManager = FindFirstObjectByType<FPSTrump.AI.LLM.LLMManager>();
            if (llmManager != null)
            {
                so.FindProperty("llmManager").objectReferenceValue = llmManager;
                resultMessage += "- LLMManager 参照設定済み\n";
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(outroSequence);

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("Success", resultMessage, "OK");
            Debug.Log("[OutroAutoSetup] Setup completed successfully!");

            RefreshSetupCheck();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OutroAutoSetup] Setup failed: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"セットアップ中にエラーが発生しました:\n{e.Message}", "OK");
        }
    }
}
