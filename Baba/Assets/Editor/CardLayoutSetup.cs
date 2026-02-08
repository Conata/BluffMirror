using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// カードファン配置 + カメラ + シーンポジションの統合設定ツール
/// Tools > Baba > Card Layout Setup から開く
///
/// 設定項目:
/// - Player Hand: fanAngle, fanRadius, cardTiltTowardCamera
/// - AI Hand: aiFanAngle, aiFanRadius, aiCardTilt
/// - VCam: PlayerTurn/AITurn の Position/Rotation (固定FPS視点)
/// - Scene: PlayerHand/AIHand の Transform Position
/// </summary>
public class CardLayoutSetup : EditorWindow
{
    // === セクション折り畳み状態 ===
    private bool foldPlayerHand = true;
    private bool foldAIHand = true;
    private bool foldCamera = true;
    private bool foldScenePos = true;

    // === シーン参照 ===
    private PlayerHandController playerHand;
    private AIHandController aiHand;
    private CinemachineCamera vcamPlayerTurn;
    private CinemachineCamera vcamAITurn;
    private CinemachineCamera vcamCardFocus;
    private CinemachineCamera vcamAIReaction;
    private CinemachineCamera vcamTableOverview;

    // === VCam編集用 ===
    private Vector3 posPlayerTurn;
    private Vector3 rotPlayerTurn;
    private Vector3 posAITurn;
    private Vector3 rotAITurn;
    private Vector3 posCardFocus;
    private Vector3 posAIReaction;

    // === Scene Position編集用 ===
    private Vector3 posPlayerHandObj;
    private Vector3 posAIHandObj;

    private Vector2 scrollPos;
    private bool autoPreview = true;

    // デフォルト推奨値
    private static readonly Vector3 DEFAULT_VCAM_PLAYER_POS = new Vector3(0f, 1.35f, 1.5f);
    private static readonly Vector3 DEFAULT_VCAM_PLAYER_ROT = new Vector3(-20f, 0f, 0f);
    private static readonly Vector3 DEFAULT_VCAM_AI_POS = new Vector3(0f, 1.45f, 2.0f);
    private static readonly Vector3 DEFAULT_VCAM_AI_ROT = new Vector3(-30f, 0f, 0f);
    private static readonly Vector3 DEFAULT_PLAYER_HAND_POS = new Vector3(0f, 0.85f, 1.5f);
    private static readonly Vector3 DEFAULT_AI_HAND_POS = new Vector3(0f, 1.0f, -0.2f);

    [MenuItem("Tools/Baba/Card Layout Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<CardLayoutSetup>("Card Layout Setup");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshReferences();
        ReadCurrentValues();
    }

    private void RefreshReferences()
    {
        playerHand = FindFirstObjectByType<PlayerHandController>();
        aiHand = FindFirstObjectByType<AIHandController>();

        vcamPlayerTurn = FindVCam("VCam_PlayerTurn");
        vcamAITurn = FindVCam("VCam_AITurn");
        vcamCardFocus = FindVCam("VCam_CardFocus");
        vcamAIReaction = FindVCam("VCam_AIReaction");
        vcamTableOverview = FindVCam("VCam_TableOverview");
    }

    private CinemachineCamera FindVCam(string name)
    {
        var obj = GameObject.Find(name);
        return obj != null ? obj.GetComponent<CinemachineCamera>() : null;
    }

    private void ReadCurrentValues()
    {
        // VCam
        if (vcamPlayerTurn != null)
        {
            posPlayerTurn = vcamPlayerTurn.transform.position;
            rotPlayerTurn = vcamPlayerTurn.transform.eulerAngles;
            // -180~180 range に正規化
            if (rotPlayerTurn.x > 180f) rotPlayerTurn.x -= 360f;
            if (rotPlayerTurn.y > 180f) rotPlayerTurn.y -= 360f;
            if (rotPlayerTurn.z > 180f) rotPlayerTurn.z -= 360f;
        }
        else
        {
            posPlayerTurn = DEFAULT_VCAM_PLAYER_POS;
            rotPlayerTurn = DEFAULT_VCAM_PLAYER_ROT;
        }

        if (vcamAITurn != null)
        {
            posAITurn = vcamAITurn.transform.position;
            rotAITurn = vcamAITurn.transform.eulerAngles;
            if (rotAITurn.x > 180f) rotAITurn.x -= 360f;
            if (rotAITurn.y > 180f) rotAITurn.y -= 360f;
            if (rotAITurn.z > 180f) rotAITurn.z -= 360f;
        }
        else
        {
            posAITurn = DEFAULT_VCAM_AI_POS;
            rotAITurn = DEFAULT_VCAM_AI_ROT;
        }

        if (vcamCardFocus != null) posCardFocus = vcamCardFocus.transform.position;
        if (vcamAIReaction != null) posAIReaction = vcamAIReaction.transform.position;

        // Scene positions
        if (playerHand != null) posPlayerHandObj = playerHand.transform.position;
        else posPlayerHandObj = DEFAULT_PLAYER_HAND_POS;

        if (aiHand != null) posAIHandObj = aiHand.transform.position;
        else posAIHandObj = DEFAULT_AI_HAND_POS;
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー
        EditorGUILayout.LabelField("Card Layout Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "カードファン配置・カメラ・シーンポジションを一括設定します。\n" +
            "値を変更後「Apply All」で適用、またはセクション毎に個別適用できます。",
            MessageType.Info);
        EditorGUILayout.Space(4);

        // === Player Hand ===
        DrawPlayerHandSection();
        EditorGUILayout.Space(4);

        // === AI Hand ===
        DrawAIHandSection();
        EditorGUILayout.Space(4);

        // === Camera ===
        DrawCameraSection();
        EditorGUILayout.Space(4);

        // === Scene Positions ===
        DrawScenePositionSection();
        EditorGUILayout.Space(8);

        // === 一括操作 ===
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DrawBulkActions();

        EditorGUILayout.EndScrollView();
    }

    // ================================================================
    // Player Hand Section
    // ================================================================
    private void DrawPlayerHandSection()
    {
        foldPlayerHand = EditorGUILayout.BeginFoldoutHeaderGroup(foldPlayerHand, "Player Hand Fan");
        if (foldPlayerHand)
        {
            if (playerHand == null)
            {
                EditorGUILayout.HelpBox("PlayerHandController がシーンに見つかりません", MessageType.Warning);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUI.indentLevel++;

            SerializedObject so = new SerializedObject(playerHand);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("fanAngle"), new GUIContent("Fan Angle", "扇の広がり角度（度）"));
            EditorGUILayout.PropertyField(so.FindProperty("fanRadius"), new GUIContent("Fan Radius", "アーク半径（大=緩やか、小=急カーブ）"));
            EditorGUILayout.PropertyField(so.FindProperty("cardTiltTowardCamera"), new GUIContent("Card Tilt", "カメラ方向への傾き角度"));

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                so.ApplyModifiedProperties();
                if (Application.isPlaying) playerHand.ArrangeCards();
            }
            if (GUILayout.Button("Reset Default"))
            {
                so.FindProperty("fanAngle").floatValue = 40f;
                so.FindProperty("fanRadius").floatValue = 2.0f;
                so.FindProperty("cardTiltTowardCamera").floatValue = 15f;
                so.ApplyModifiedProperties();
                if (Application.isPlaying) playerHand.ArrangeCards();
            }
            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = playerHand.gameObject;
            }
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ================================================================
    // AI Hand Section
    // ================================================================
    private void DrawAIHandSection()
    {
        foldAIHand = EditorGUILayout.BeginFoldoutHeaderGroup(foldAIHand, "AI Hand Fan");
        if (foldAIHand)
        {
            if (aiHand == null)
            {
                EditorGUILayout.HelpBox("AIHandController がシーンに見つかりません", MessageType.Warning);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUI.indentLevel++;

            SerializedObject so = new SerializedObject(aiHand);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("aiFanAngle"), new GUIContent("Fan Angle", "扇の広がり角度（度）"));
            EditorGUILayout.PropertyField(so.FindProperty("aiFanRadius"), new GUIContent("Fan Radius", "アーク半径"));
            EditorGUILayout.PropertyField(so.FindProperty("aiCardTilt"), new GUIContent("Card Tilt", "プレイヤー方向への傾き角度"));

            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                so.ApplyModifiedProperties();
                if (Application.isPlaying) aiHand.ArrangeCards();
            }
            if (GUILayout.Button("Reset Default"))
            {
                so.FindProperty("aiFanAngle").floatValue = 35f;
                so.FindProperty("aiFanRadius").floatValue = 1.5f;
                so.FindProperty("aiCardTilt").floatValue = 10f;
                so.ApplyModifiedProperties();
                if (Application.isPlaying) aiHand.ArrangeCards();
            }
            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = aiHand.gameObject;
            }
            EditorGUILayout.EndHorizontal();

            so.ApplyModifiedProperties();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ================================================================
    // Camera Section
    // ================================================================
    private void DrawCameraSection()
    {
        foldCamera = EditorGUILayout.BeginFoldoutHeaderGroup(foldCamera, "Camera (Cinemachine VCam)");
        if (foldCamera)
        {
            EditorGUI.indentLevel++;

            // --- VCam_PlayerTurn ---
            EditorGUILayout.LabelField("VCam_PlayerTurn (FPS着席視点 - AI手札を見る)", EditorStyles.miniBoldLabel);
            bool hasPlayerTurn = vcamPlayerTurn != null;
            GUI.enabled = hasPlayerTurn;

            posPlayerTurn = EditorGUILayout.Vector3Field("Position", posPlayerTurn);
            rotPlayerTurn = EditorGUILayout.Vector3Field("Rotation", rotPlayerTurn);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paste SceneView"))
            {
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                {
                    posPlayerTurn = sv.camera.transform.position;
                    var euler = sv.camera.transform.eulerAngles;
                    if (euler.x > 180f) euler.x -= 360f;
                    if (euler.y > 180f) euler.y -= 360f;
                    if (euler.z > 180f) euler.z -= 360f;
                    rotPlayerTurn = euler;
                }
            }
            if (GUILayout.Button("Default"))
            {
                posPlayerTurn = DEFAULT_VCAM_PLAYER_POS;
                rotPlayerTurn = DEFAULT_VCAM_PLAYER_ROT;
            }
            if (GUILayout.Button("Apply") && hasPlayerTurn)
            {
                ApplyVCamFixed(vcamPlayerTurn, posPlayerTurn, rotPlayerTurn);
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.Space(6);

            // --- VCam_AITurn ---
            EditorGUILayout.LabelField("VCam_AITurn (FPS着席視点 - Player手札を見る)", EditorStyles.miniBoldLabel);
            bool hasAITurn = vcamAITurn != null;
            GUI.enabled = hasAITurn;

            posAITurn = EditorGUILayout.Vector3Field("Position", posAITurn);
            rotAITurn = EditorGUILayout.Vector3Field("Rotation", rotAITurn);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paste SceneView"))
            {
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                {
                    posAITurn = sv.camera.transform.position;
                    var euler = sv.camera.transform.eulerAngles;
                    if (euler.x > 180f) euler.x -= 360f;
                    if (euler.y > 180f) euler.y -= 360f;
                    if (euler.z > 180f) euler.z -= 360f;
                    rotAITurn = euler;
                }
            }
            if (GUILayout.Button("Default"))
            {
                posAITurn = DEFAULT_VCAM_AI_POS;
                rotAITurn = DEFAULT_VCAM_AI_ROT;
            }
            if (GUILayout.Button("Apply") && hasAITurn)
            {
                ApplyVCamFixed(vcamAITurn, posAITurn, rotAITurn);
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
            EditorGUILayout.Space(6);

            // --- VCam_CardFocus / VCam_AIReaction (参考表示のみ) ---
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("VCam_CardFocus (動的 - コードで制御)", EditorStyles.miniLabel);
            if (vcamCardFocus != null)
                EditorGUILayout.Vector3Field("Position", vcamCardFocus.transform.position);

            EditorGUILayout.LabelField("VCam_AIReaction (動的 - LookAt制御)", EditorStyles.miniLabel);
            if (vcamAIReaction != null)
                EditorGUILayout.Vector3Field("Position", vcamAIReaction.transform.position);

            EditorGUILayout.LabelField("VCam_TableOverview (固定 - テーブル全景)", EditorStyles.miniLabel);
            if (vcamTableOverview != null)
                EditorGUILayout.Vector3Field("Position", vcamTableOverview.transform.position);
            EditorGUI.EndDisabledGroup();

            if (!hasPlayerTurn || !hasAITurn)
            {
                EditorGUILayout.HelpBox("VCamがシーンに見つかりません。Tools > Baba > Setup Cinemachine Cameras で作成してください。", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ================================================================
    // Scene Position Section
    // ================================================================
    private void DrawScenePositionSection()
    {
        foldScenePos = EditorGUILayout.BeginFoldoutHeaderGroup(foldScenePos, "Scene Positions");
        if (foldScenePos)
        {
            EditorGUI.indentLevel++;

            // PlayerHand
            EditorGUILayout.LabelField("PlayerHand Transform", EditorStyles.miniBoldLabel);
            posPlayerHandObj = EditorGUILayout.Vector3Field("Position", posPlayerHandObj);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Default"))
                posPlayerHandObj = DEFAULT_PLAYER_HAND_POS;
            GUI.enabled = playerHand != null;
            if (GUILayout.Button("Apply"))
            {
                Undo.RecordObject(playerHand.transform, "Move PlayerHand");
                playerHand.transform.position = posPlayerHandObj;
                EditorUtility.SetDirty(playerHand.transform);
                if (Application.isPlaying) playerHand.ArrangeCards();
            }
            if (GUILayout.Button("Read"))
                posPlayerHandObj = playerHand.transform.position;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // AIHand
            EditorGUILayout.LabelField("AIHand Transform", EditorStyles.miniBoldLabel);
            posAIHandObj = EditorGUILayout.Vector3Field("Position", posAIHandObj);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Default"))
                posAIHandObj = DEFAULT_AI_HAND_POS;
            GUI.enabled = aiHand != null;
            if (GUILayout.Button("Apply"))
            {
                Undo.RecordObject(aiHand.transform, "Move AIHand");
                aiHand.transform.position = posAIHandObj;
                EditorUtility.SetDirty(aiHand.transform);
                if (Application.isPlaying) aiHand.ArrangeCards();
            }
            if (GUILayout.Button("Read"))
                posAIHandObj = aiHand.transform.position;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ================================================================
    // Bulk Actions
    // ================================================================
    private void DrawBulkActions()
    {
        EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);

        // Apply All
        var origColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Apply All", GUILayout.Height(36)))
        {
            ApplyAll();
        }
        GUI.backgroundColor = origColor;

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All to Defaults", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Reset All",
                "全ての値をデフォルトにリセットしますか？", "Reset", "Cancel"))
            {
                ResetAllToDefaults();
            }
        }
        if (GUILayout.Button("Read All from Scene", GUILayout.Height(28)))
        {
            RefreshReferences();
            ReadCurrentValues();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Refresh References", GUILayout.Height(22)))
        {
            RefreshReferences();
            ReadCurrentValues();
            Repaint();
        }
    }

    // ================================================================
    // Apply Methods
    // ================================================================

    /// <summary>
    /// VCamに固定Position/Rotationを適用（LookAt/HardLookAtなし）
    /// </summary>
    private void ApplyVCamFixed(CinemachineCamera vcam, Vector3 pos, Vector3 eulerAngles)
    {
        if (vcam == null) return;

        Undo.RecordObject(vcam.transform, $"Move {vcam.name}");
        Undo.RecordObject(vcam, $"Set {vcam.name} settings");

        vcam.transform.position = pos;
        vcam.transform.eulerAngles = eulerAngles;
        vcam.Follow = null;
        vcam.LookAt = null;

        // HardLookAtがあれば除去（固定回転を使うため）
        var hardLookAt = vcam.GetComponent<CinemachineHardLookAt>();
        if (hardLookAt != null)
        {
            Undo.DestroyObjectImmediate(hardLookAt);
        }

        EditorUtility.SetDirty(vcam);
        EditorUtility.SetDirty(vcam.transform);

        Debug.Log($"[CardLayoutSetup] {vcam.name} → pos={pos}, rot={eulerAngles}");
    }

    private void ApplyAll()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply All Card Layout Settings");

        // Player Hand params
        if (playerHand != null)
        {
            SerializedObject so = new SerializedObject(playerHand);
            so.ApplyModifiedProperties();
        }

        // AI Hand params
        if (aiHand != null)
        {
            SerializedObject so = new SerializedObject(aiHand);
            so.ApplyModifiedProperties();
        }

        // VCam positions
        if (vcamPlayerTurn != null)
            ApplyVCamFixed(vcamPlayerTurn, posPlayerTurn, rotPlayerTurn);
        if (vcamAITurn != null)
            ApplyVCamFixed(vcamAITurn, posAITurn, rotAITurn);

        // Scene positions
        if (playerHand != null)
        {
            Undo.RecordObject(playerHand.transform, "Move PlayerHand");
            playerHand.transform.position = posPlayerHandObj;
            EditorUtility.SetDirty(playerHand.transform);
        }
        if (aiHand != null)
        {
            Undo.RecordObject(aiHand.transform, "Move AIHand");
            aiHand.transform.position = posAIHandObj;
            EditorUtility.SetDirty(aiHand.transform);
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Play中ならカード再配置
        if (Application.isPlaying)
        {
            if (playerHand != null) playerHand.ArrangeCards();
            if (aiHand != null) aiHand.ArrangeCards();
        }

        Debug.Log("[CardLayoutSetup] All settings applied");
    }

    private void ResetAllToDefaults()
    {
        // Player Hand
        if (playerHand != null)
        {
            SerializedObject so = new SerializedObject(playerHand);
            so.FindProperty("fanAngle").floatValue = 40f;
            so.FindProperty("fanRadius").floatValue = 2.0f;
            so.FindProperty("cardTiltTowardCamera").floatValue = 15f;
            so.ApplyModifiedProperties();
        }

        // AI Hand
        if (aiHand != null)
        {
            SerializedObject so = new SerializedObject(aiHand);
            so.FindProperty("aiFanAngle").floatValue = 35f;
            so.FindProperty("aiFanRadius").floatValue = 1.5f;
            so.FindProperty("aiCardTilt").floatValue = 10f;
            so.ApplyModifiedProperties();
        }

        // VCam
        posPlayerTurn = DEFAULT_VCAM_PLAYER_POS;
        rotPlayerTurn = DEFAULT_VCAM_PLAYER_ROT;
        posAITurn = DEFAULT_VCAM_AI_POS;
        rotAITurn = DEFAULT_VCAM_AI_ROT;

        // Scene positions
        posPlayerHandObj = DEFAULT_PLAYER_HAND_POS;
        posAIHandObj = DEFAULT_AI_HAND_POS;

        Repaint();
    }
}
