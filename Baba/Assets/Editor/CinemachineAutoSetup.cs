using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// Cinemachineカメラシステムのセットアップ＆位置調整ツール
/// Tools > Baba > Setup Cinemachine Cameras から実行
///
/// 使い方:
/// 1. Scene Viewでカメラアングルを決める
/// 2. 「Paste SceneView」で位置を取り込む
/// 3. 「Apply to VCam」で適用（即座にシーンに反映）
/// 4. 繰り返して全VCamを調整
/// </summary>
public class CinemachineAutoSetup : EditorWindow
{
    // === デフォルト推奨位置 ===
    // Player=+Z側, AI=-Z側, テーブル中央=原点
    private static readonly Vector3 DEFAULT_PLAYER_TURN  = new Vector3(0f, 1.5f, 1.5f);
    private static readonly Vector3 DEFAULT_AI_TURN      = new Vector3(0f, 2.0f, -0.3f);
    private static readonly Vector3 DEFAULT_CARD_FOCUS   = new Vector3(0f, 1.3f, 0.5f);
    private static readonly Vector3 DEFAULT_AI_REACTION     = new Vector3(0f, 1.8f, -0.5f);
    private static readonly Vector3 DEFAULT_TABLE_OVERVIEW  = new Vector3(0f, 2.5f, 0.6f);

    // 各VCamの編集用位置
    private Vector3 posPlayerTurn;
    private Vector3 posAITurn;
    private Vector3 posCardFocus;
    private Vector3 posAIReaction;
    private Vector3 posTableOverview;

    // シーン参照
    private Transform aiHandTransform;
    private Transform playerHandTransform;
    private Transform aiFaceTransform;
    private GameManager gameManager;
    private Camera mainCamera;
    private CameraCinematicsSystem cameraSystem;

    // 検出されたVCam
    private CinemachineCamera vcamPlayerTurn;
    private CinemachineCamera vcamAITurn;
    private CinemachineCamera vcamCardFocus;
    private CinemachineCamera vcamAIReaction;
    private CinemachineCamera vcamTableOverview;
    private Transform camerasParent;

    private Vector2 scrollPos;
    private int selectedVCamIndex = 0;
    private readonly string[] vcamNames = {
        "VCam_PlayerTurn (AIの手札を見る)",
        "VCam_AITurn (Playerの手札を見る)",
        "VCam_CardFocus (カードズーム)",
        "VCam_AIReaction (AI顔リアクション)",
        "VCam_TableOverview (テーブル全景)"
    };

    [MenuItem("Tools/Baba/Setup Cinemachine Cameras")]
    public static void ShowWindow()
    {
        var window = GetWindow<CinemachineAutoSetup>("Cinemachine Setup");
        window.minSize = new Vector2(420, 600);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshSceneReferences();
        ReadCurrentPositions();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    /// <summary>
    /// シーン上のVCam現在位置を読み取ってフィールドに反映
    /// </summary>
    private void ReadCurrentPositions()
    {
        posPlayerTurn  = vcamPlayerTurn  != null ? vcamPlayerTurn.transform.position  : DEFAULT_PLAYER_TURN;
        posAITurn      = vcamAITurn      != null ? vcamAITurn.transform.position      : DEFAULT_AI_TURN;
        posCardFocus   = vcamCardFocus   != null ? vcamCardFocus.transform.position   : DEFAULT_CARD_FOCUS;
        posAIReaction  = vcamAIReaction  != null ? vcamAIReaction.transform.position  : DEFAULT_AI_REACTION;
        posTableOverview = vcamTableOverview != null ? vcamTableOverview.transform.position : DEFAULT_TABLE_OVERVIEW;
    }

    private void RefreshSceneReferences()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        cameraSystem = FindFirstObjectByType<CameraCinematicsSystem>();

        var aiHandCtrl = FindFirstObjectByType<AIHandController>();
        if (aiHandCtrl != null) aiHandTransform = aiHandCtrl.transform;

        var playerHandCtrl = FindFirstObjectByType<PlayerHandController>();
        if (playerHandCtrl != null) playerHandTransform = playerHandCtrl.transform;

        var aiFaceObj = GameObject.Find("AIFace");
        if (aiFaceObj != null) aiFaceTransform = aiFaceObj.transform;

        vcamPlayerTurn = FindVCamByName("VCam_PlayerTurn");
        vcamAITurn = FindVCamByName("VCam_AITurn");
        vcamCardFocus = FindVCamByName("VCam_CardFocus");
        vcamAIReaction = FindVCamByName("VCam_AIReaction");
        vcamTableOverview = FindVCamByName("VCam_TableOverview");

        var camerasObj = GameObject.Find("Cameras");
        if (camerasObj != null) camerasParent = camerasObj.transform;

        Repaint();
    }

    private CinemachineCamera FindVCamByName(string name)
    {
        var obj = GameObject.Find(name);
        return obj != null ? obj.GetComponent<CinemachineCamera>() : null;
    }

    // ================================================================
    // GUI
    // ================================================================
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader();
        DrawSceneInfo();
        EditorGUILayout.Space(8);
        DrawVCamPositioner();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DrawBulkActions();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Cinemachine Camera Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scene Viewでアングルを決めて「Paste SceneView」→「Apply」の流れで調整できます。",
            MessageType.Info);
        EditorGUILayout.Space(4);
    }

    private void DrawSceneInfo()
    {
        EditorGUILayout.LabelField("Scene Layout", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (playerHandTransform != null)
            EditorGUILayout.LabelField($"PlayerHand:  {playerHandTransform.position:F2}");
        if (aiHandTransform != null)
            EditorGUILayout.LabelField($"AIHand:      {aiHandTransform.position:F2}");
        if (aiFaceTransform != null)
            EditorGUILayout.LabelField($"AIFace:      {aiFaceTransform.position:F2}");

        // Cameras親オフセット警告
        if (camerasParent != null && camerasParent.localPosition.sqrMagnitude > 0.001f)
        {
            EditorGUILayout.HelpBox(
                $"Cameras親がズレています: {camerasParent.localPosition:F3}",
                MessageType.Warning);
            if (GUILayout.Button("Reset Cameras Parent to Origin"))
            {
                Undo.RecordObject(camerasParent, "Reset Cameras parent");
                camerasParent.localPosition = Vector3.zero;
                camerasParent.localRotation = Quaternion.identity;
            }
        }

        EditorGUI.indentLevel--;
    }

    // ================================================================
    // VCam個別位置調整UI
    // ================================================================
    private void DrawVCamPositioner()
    {
        EditorGUILayout.LabelField("VCam Position Editor", EditorStyles.boldLabel);

        // VCam選択タブ
        selectedVCamIndex = GUILayout.Toolbar(selectedVCamIndex, new[] { "Player", "AI", "Card", "React", "Table" });

        EditorGUILayout.Space(4);

        var vcam = GetSelectedVCam();
        string vcamLabel = vcamNames[selectedVCamIndex];

        EditorGUILayout.LabelField(vcamLabel, EditorStyles.miniBoldLabel);

        // 現在のVCam位置表示
        if (vcam != null)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Current Position", vcam.transform.position);
            string lookAtName = vcam.LookAt != null ? vcam.LookAt.name : "(none)";
            EditorGUILayout.TextField("LookAt", lookAtName);
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("VCamがシーンに見つかりません。Full Setupを実行してください。", MessageType.Warning);
        }

        EditorGUILayout.Space(4);

        // 編集フィールド
        ref Vector3 editPos = ref GetSelectedPositionRef();
        editPos = EditorGUILayout.Vector3Field("New Position", editPos);

        EditorGUILayout.Space(4);

        // --- ボタン行 ---
        EditorGUILayout.BeginHorizontal();

        // Scene Viewカメラ位置を取り込む
        if (GUILayout.Button("Paste SceneView", GUILayout.Height(28)))
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                editPos = sceneView.camera.transform.position;
                Repaint();
            }
        }

        // VCamの現在位置を読み込む
        GUI.enabled = vcam != null;
        if (GUILayout.Button("Read VCam", GUILayout.Height(28)))
        {
            editPos = vcam.transform.position;
            Repaint();
        }
        GUI.enabled = true;

        // デフォルト値に戻す
        if (GUILayout.Button("Default", GUILayout.Height(28)))
        {
            editPos = GetDefaultPosition(selectedVCamIndex);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 適用ボタン（大きく目立たせる）
        GUI.enabled = vcam != null;
        var origColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Apply to VCam", GUILayout.Height(32)))
        {
            ApplyToSelectedVCam(editPos);
        }
        GUI.backgroundColor = origColor;
        GUI.enabled = true;

        EditorGUILayout.Space(2);

        // VCamを選択してScene Viewで見る
        GUI.enabled = vcam != null;
        if (GUILayout.Button("Select & Focus in Scene"))
        {
            Selection.activeGameObject = vcam.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        GUI.enabled = true;
    }

    private CinemachineCamera GetSelectedVCam()
    {
        return selectedVCamIndex switch
        {
            0 => vcamPlayerTurn,
            1 => vcamAITurn,
            2 => vcamCardFocus,
            3 => vcamAIReaction,
            4 => vcamTableOverview,
            _ => null
        };
    }

    private ref Vector3 GetSelectedPositionRef()
    {
        switch (selectedVCamIndex)
        {
            case 0: return ref posPlayerTurn;
            case 1: return ref posAITurn;
            case 2: return ref posCardFocus;
            case 3: return ref posAIReaction;
            case 4: return ref posTableOverview;
            default: return ref posPlayerTurn;
        }
    }

    private Vector3 GetDefaultPosition(int index)
    {
        return index switch
        {
            0 => DEFAULT_PLAYER_TURN,
            1 => DEFAULT_AI_TURN,
            2 => DEFAULT_CARD_FOCUS,
            3 => DEFAULT_AI_REACTION,
            4 => DEFAULT_TABLE_OVERVIEW,
            _ => Vector3.zero
        };
    }

    private Transform GetLookAtTarget(int index)
    {
        return index switch
        {
            0 => aiHandTransform,
            1 => playerHandTransform,
            2 => null, // dynamic
            3 => aiFaceTransform,
            4 => null, // TableOverview: 固定回転
            _ => null
        };
    }

    private void ApplyToSelectedVCam(Vector3 newPos)
    {
        var vcam = GetSelectedVCam();
        if (vcam == null) return;

        Undo.RecordObject(vcam.transform, $"Move {vcam.name}");
        Undo.RecordObject(vcam, $"Set {vcam.name} targets");

        vcam.transform.position = newPos;
        vcam.transform.rotation = Quaternion.identity;
        vcam.Follow = null;
        vcam.LookAt = GetLookAtTarget(selectedVCamIndex);
        EnsureHardLookAt(vcam);

        EditorUtility.SetDirty(vcam);
        EditorUtility.SetDirty(vcam.transform);

        Debug.Log($"[CinemachineSetup] {vcam.name} → pos={newPos}, lookAt={vcam.LookAt?.name ?? "null"}");
    }

    // ================================================================
    // Scene View Gizmos
    // ================================================================
    private void OnSceneGUI(SceneView sceneView)
    {
        // 各VCamの位置とLookAtラインをGizmo表示
        DrawVCamGizmo(vcamPlayerTurn, posPlayerTurn, Color.cyan, "PlayerTurn");
        DrawVCamGizmo(vcamAITurn, posAITurn, Color.red, "AITurn");
        DrawVCamGizmo(vcamCardFocus, posCardFocus, Color.yellow, "CardFocus");
        DrawVCamGizmo(vcamAIReaction, posAIReaction, Color.magenta, "AIReaction");
        DrawVCamGizmo(vcamTableOverview, posTableOverview, new Color(0.5f, 1f, 0.5f), "TableOverview");

        // 参照ポイント表示
        if (playerHandTransform != null)
            DrawReferencePoint(playerHandTransform.position, Color.green, "PlayerHand");
        if (aiHandTransform != null)
            DrawReferencePoint(aiHandTransform.position, Color.red, "AIHand");
        if (aiFaceTransform != null)
            DrawReferencePoint(aiFaceTransform.position, Color.magenta, "AIFace");
    }

    private void DrawVCamGizmo(CinemachineCamera vcam, Vector3 editPos, Color color, string label)
    {
        if (vcam == null) return;

        Vector3 pos = vcam.transform.position;
        Handles.color = color;

        // カメラ位置のワイヤースフィア
        Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.1f, EventType.Repaint);
        Handles.Label(pos + Vector3.up * 0.15f, label, EditorStyles.boldLabel);

        // LookAtターゲットへのライン
        if (vcam.LookAt != null)
        {
            Handles.color = new Color(color.r, color.g, color.b, 0.5f);
            Handles.DrawDottedLine(pos, vcam.LookAt.position, 3f);
        }
    }

    private void DrawReferencePoint(Vector3 pos, Color color, string label)
    {
        Handles.color = color;
        Handles.DrawWireCube(pos, Vector3.one * 0.08f);
        Handles.Label(pos + Vector3.up * 0.12f, label);
    }

    // ================================================================
    // 一括操作
    // ================================================================
    private void DrawBulkActions()
    {
        EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Apply All Positions", GUILayout.Height(30)))
        {
            ApplyAllPositions();
        }

        if (GUILayout.Button("Read All from Scene", GUILayout.Height(30)))
        {
            ReadCurrentPositions();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Fix All Targets (LookAt)", GUILayout.Height(28)))
        {
            FixAllTargets();
        }

        EditorGUILayout.Space(4);

        // Full Setup
        GUI.enabled = mainCamera != null && aiHandTransform != null && playerHandTransform != null;
        if (GUILayout.Button("Full Setup (Create All)", GUILayout.Height(30)))
        {
            FullSetup();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Refresh", GUILayout.Height(22)))
        {
            RefreshSceneReferences();
            ReadCurrentPositions();
        }
    }

    private void ApplyAllPositions()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply All VCam Positions");

        // Cameras親リセット
        if (camerasParent != null && camerasParent.localPosition.sqrMagnitude > 0.001f)
        {
            Undo.RecordObject(camerasParent, "Reset Cameras parent");
            camerasParent.localPosition = Vector3.zero;
            camerasParent.localRotation = Quaternion.identity;
        }

        if (vcamPlayerTurn != null)  ApplyVCam(vcamPlayerTurn,  posPlayerTurn,  aiHandTransform);
        if (vcamAITurn != null)      ApplyVCam(vcamAITurn,      posAITurn,      playerHandTransform);
        if (vcamCardFocus != null)   ApplyVCam(vcamCardFocus,   posCardFocus,   null);
        if (vcamAIReaction != null)  ApplyVCam(vcamAIReaction,  posAIReaction,  aiFaceTransform);
        if (vcamTableOverview != null) ApplyVCam(vcamTableOverview, posTableOverview, null);

        if (cameraSystem != null) FixCameraSystemReferences();

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("[CinemachineSetup] All VCam positions applied");
    }

    private void ApplyVCam(CinemachineCamera vcam, Vector3 worldPos, Transform lookAt)
    {
        Undo.RecordObject(vcam.transform, $"Apply {vcam.name}");
        Undo.RecordObject(vcam, $"Set {vcam.name} targets");

        vcam.transform.position = worldPos;
        vcam.transform.rotation = Quaternion.identity;
        vcam.Follow = null;
        vcam.LookAt = lookAt;
        EnsureHardLookAt(vcam);

        EditorUtility.SetDirty(vcam);
        EditorUtility.SetDirty(vcam.transform);
    }

    private void FixAllTargets()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix All VCam Targets");

        // PlayerTurn/AITurn: FPS着席視点 → 固定回転（LookAt/HardLookAt不要）
        if (vcamPlayerTurn != null) { Undo.RecordObject(vcamPlayerTurn, "Fix targets"); vcamPlayerTurn.Follow = null; vcamPlayerTurn.LookAt = null; RemoveHardLookAt(vcamPlayerTurn); EditorUtility.SetDirty(vcamPlayerTurn); }
        if (vcamAITurn != null)     { Undo.RecordObject(vcamAITurn, "Fix targets");     vcamAITurn.Follow = null;     vcamAITurn.LookAt = null;     RemoveHardLookAt(vcamAITurn);     EditorUtility.SetDirty(vcamAITurn); }
        // CardFocus/AIReaction: 動的LookAt維持
        if (vcamCardFocus != null)  { Undo.RecordObject(vcamCardFocus, "Fix targets");  vcamCardFocus.Follow = null;  vcamCardFocus.LookAt = null;              EnsureHardLookAt(vcamCardFocus);  EditorUtility.SetDirty(vcamCardFocus); }
        if (vcamAIReaction != null) { Undo.RecordObject(vcamAIReaction, "Fix targets"); vcamAIReaction.Follow = null; vcamAIReaction.LookAt = aiFaceTransform;  EnsureHardLookAt(vcamAIReaction); EditorUtility.SetDirty(vcamAIReaction); }
        // TableOverview: 固定回転（PlayerTurn/AITurnと同パターン）
        if (vcamTableOverview != null) { Undo.RecordObject(vcamTableOverview, "Fix targets"); vcamTableOverview.Follow = null; vcamTableOverview.LookAt = null; RemoveHardLookAt(vcamTableOverview); EditorUtility.SetDirty(vcamTableOverview); }

        if (cameraSystem != null) FixCameraSystemReferences();

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log("[CinemachineSetup] All VCam targets fixed");
    }

    private void FixCameraSystemReferences()
    {
        SerializedObject so = new SerializedObject(cameraSystem);
        if (vcamPlayerTurn != null)  so.FindProperty("vcamPlayerTurn").objectReferenceValue = vcamPlayerTurn;
        if (vcamAITurn != null)      so.FindProperty("vcamAITurn").objectReferenceValue = vcamAITurn;
        if (vcamCardFocus != null)   so.FindProperty("vcamCardFocus").objectReferenceValue = vcamCardFocus;
        if (vcamAIReaction != null)  so.FindProperty("vcamAIReaction").objectReferenceValue = vcamAIReaction;
        if (vcamTableOverview != null) so.FindProperty("vcamTableOverview").objectReferenceValue = vcamTableOverview;
        if (aiHandTransform != null)     so.FindProperty("aiHandTransform").objectReferenceValue = aiHandTransform;
        if (playerHandTransform != null) so.FindProperty("playerHandTransform").objectReferenceValue = playerHandTransform;
        if (aiFaceTransform != null)     so.FindProperty("aiFaceTransform").objectReferenceValue = aiFaceTransform;
        so.ApplyModifiedProperties();
    }

    // ================================================================
    // Full Setup（一括作成）
    // ================================================================
    private void FullSetup()
    {
        if (!EditorUtility.DisplayDialog("Full Cinemachine Setup",
            "VCam・CameraCinematicsSystem・CinemachineBrainを一括作成/再設定します。\n続行しますか？",
            "Setup", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Full Cinemachine Setup");

        try
        {
            // CinemachineBrain
            if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
                Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);

            // Cameras親
            var camerasObj = GameObject.Find("Cameras");
            if (camerasObj == null)
            {
                camerasObj = new GameObject("Cameras");
                Undo.RegisterCreatedObjectUndo(camerasObj, "Create Cameras");
            }
            else
            {
                Undo.RecordObject(camerasObj.transform, "Reset Cameras");
            }
            camerasObj.transform.position = Vector3.zero;
            camerasObj.transform.rotation = Quaternion.identity;

            // VCam作成/更新
            vcamPlayerTurn = CreateOrUpdateVCam("VCam_PlayerTurn", posPlayerTurn, camerasObj.transform);
            vcamAITurn     = CreateOrUpdateVCam("VCam_AITurn",     posAITurn,     camerasObj.transform);
            vcamCardFocus  = CreateOrUpdateVCam("VCam_CardFocus",  posCardFocus,  camerasObj.transform);
            vcamAIReaction = CreateOrUpdateVCam("VCam_AIReaction", posAIReaction, camerasObj.transform);
            vcamTableOverview = CreateOrUpdateVCam("VCam_TableOverview", posTableOverview, camerasObj.transform);

            // ターゲット設定
            // PlayerTurn/AITurn/TableOverview: FPS着席視点 → 固定回転（LookAt不要）
            vcamPlayerTurn.LookAt = null;
            RemoveHardLookAt(vcamPlayerTurn);
            vcamAITurn.LookAt = null;
            RemoveHardLookAt(vcamAITurn);
            vcamTableOverview.LookAt = null;
            RemoveHardLookAt(vcamTableOverview);
            // CardFocus/AIReaction: 動的LookAt
            vcamAIReaction.LookAt = aiFaceTransform;

            // CameraCinematicsSystem
            var camSystem = FindFirstObjectByType<CameraCinematicsSystem>();
            if (camSystem == null)
            {
                var obj = new GameObject("CameraCinematicsSystem");
                Undo.RegisterCreatedObjectUndo(obj, "Create CameraCinematicsSystem");
                camSystem = obj.AddComponent<CameraCinematicsSystem>();
            }
            cameraSystem = camSystem;

            // 参照設定
            FixCameraSystemReferences();

            SerializedObject so = new SerializedObject(camSystem);
            so.FindProperty("defaultPriority").intValue = 10;
            so.FindProperty("activePriority").intValue = 15;
            so.FindProperty("aiFaceTransform").objectReferenceValue = aiFaceTransform ?? aiHandTransform;
            so.ApplyModifiedProperties();

            // GameManager接続
            if (gameManager != null)
            {
                SerializedObject gmSo = new SerializedObject(gameManager);
                var prop = gmSo.FindProperty("cameraSystem");
                if (prop != null) { prop.objectReferenceValue = camSystem; gmSo.ApplyModifiedProperties(); }
            }

            Undo.CollapseUndoOperations(undoGroup);
            RefreshSceneReferences();

            EditorUtility.DisplayDialog("Done", "Cinemachineセットアップ完了！", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CinemachineSetup] {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error", e.Message, "OK");
        }
    }

    private CinemachineCamera CreateOrUpdateVCam(string name, Vector3 worldPos, Transform parent)
    {
        var existingObj = GameObject.Find(name);
        CinemachineCamera vcam;

        if (existingObj != null)
        {
            vcam = existingObj.GetComponent<CinemachineCamera>();
            if (vcam == null) vcam = Undo.AddComponent<CinemachineCamera>(existingObj);
            Undo.RecordObject(existingObj.transform, $"Update {name}");
            if (existingObj.transform.parent != parent) existingObj.transform.SetParent(parent);
        }
        else
        {
            var obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            vcam = obj.AddComponent<CinemachineCamera>();
            obj.transform.SetParent(parent);
        }

        vcam.transform.position = worldPos;
        vcam.transform.rotation = Quaternion.identity;
        vcam.Priority.Value = 10;
        vcam.Follow = null;
        EnsureHardLookAt(vcam);

        var lens = vcam.Lens;
        lens.FieldOfView = 40f;
        lens.NearClipPlane = 0.1f;
        lens.FarClipPlane = 5000f;
        vcam.Lens = lens;

        EditorUtility.SetDirty(vcam);
        return vcam;
    }

    /// <summary>
    /// VCamにCinemachineHardLookAtが無ければ追加
    /// Cinemachine 3.xではこれが無いとLookAtターゲットが効かない
    /// </summary>
    private void EnsureHardLookAt(CinemachineCamera vcam)
    {
        if (vcam.GetComponent<CinemachineHardLookAt>() == null)
        {
            Undo.AddComponent<CinemachineHardLookAt>(vcam.gameObject);
            Debug.Log($"[CinemachineSetup] Added CinemachineHardLookAt to {vcam.name}");
        }
    }

    /// <summary>
    /// VCamからCinemachineHardLookAtを除去
    /// FPS着席視点では固定回転を使用するためHardLookAtは不要
    /// </summary>
    private void RemoveHardLookAt(CinemachineCamera vcam)
    {
        var hardLookAt = vcam.GetComponent<CinemachineHardLookAt>();
        if (hardLookAt != null)
        {
            Undo.DestroyObjectImmediate(hardLookAt);
            Debug.Log($"[CinemachineSetup] Removed CinemachineHardLookAt from {vcam.name}");
        }
    }
}
