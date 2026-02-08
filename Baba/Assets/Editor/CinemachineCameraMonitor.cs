using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// Cinemachineカメラのアクティブ状態をリアルタイム表示するモニター
/// Tools > Baba > Camera Monitor から開く
/// Play/Edit両モードで動作し、どのVCamがアクティブかを一目で確認できる
/// </summary>
public class CinemachineCameraMonitor : EditorWindow
{
    private CameraCinematicsSystem cameraSystem;
    private CinemachineBrain brain;

    private CinemachineCamera vcamPlayerTurn;
    private CinemachineCamera vcamAITurn;
    private CinemachineCamera vcamCardFocus;
    private CinemachineCamera vcamAIReaction;
    private CinemachineCamera vcamTableOverview;

    private Vector2 scrollPos;
    private bool autoRefresh = true;
    private double lastRefreshTime;

    // スタイルキャッシュ
    private GUIStyle activeStyle;
    private GUIStyle inactiveStyle;
    private GUIStyle blendingStyle;
    private GUIStyle headerLabelStyle;
    private bool stylesInitialized;

    [MenuItem("Tools/Baba/Camera Monitor")]
    public static void ShowWindow()
    {
        var window = GetWindow<CinemachineCameraMonitor>("Camera Monitor");
        window.minSize = new Vector2(300, 340);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshReferences();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!autoRefresh) return;

        // Play中は毎フレーム、Edit中は0.5秒間隔でRepaint
        double interval = Application.isPlaying ? 0.05 : 0.5;
        if (EditorApplication.timeSinceStartup - lastRefreshTime > interval)
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        activeStyle = new GUIStyle(EditorStyles.helpBox);
        activeStyle.normal.textColor = Color.white;
        activeStyle.fontSize = 12;
        activeStyle.fontStyle = FontStyle.Bold;
        activeStyle.padding = new RectOffset(8, 8, 6, 6);

        inactiveStyle = new GUIStyle(EditorStyles.helpBox);
        inactiveStyle.fontSize = 11;
        inactiveStyle.padding = new RectOffset(8, 8, 6, 6);

        blendingStyle = new GUIStyle(EditorStyles.helpBox);
        blendingStyle.fontSize = 11;
        blendingStyle.fontStyle = FontStyle.Italic;
        blendingStyle.padding = new RectOffset(8, 8, 6, 6);

        headerLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        headerLabelStyle.fontSize = 13;

        stylesInitialized = true;
    }

    private void RefreshReferences()
    {
        cameraSystem = FindFirstObjectByType<CameraCinematicsSystem>();
        brain = FindFirstObjectByType<CinemachineBrain>();

        vcamPlayerTurn = FindVCamByName("VCam_PlayerTurn");
        vcamAITurn = FindVCamByName("VCam_AITurn");
        vcamCardFocus = FindVCamByName("VCam_CardFocus");
        vcamAIReaction = FindVCamByName("VCam_AIReaction");
        vcamTableOverview = FindVCamByName("VCam_TableOverview");
    }

    private CinemachineCamera FindVCamByName(string name)
    {
        var obj = GameObject.Find(name);
        return obj != null ? obj.GetComponent<CinemachineCamera>() : null;
    }

    private void OnGUI()
    {
        InitStyles();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ヘッダー
        EditorGUILayout.LabelField("Camera Monitor", headerLabelStyle);

        EditorGUILayout.BeginHorizontal();
        autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", autoRefresh, GUILayout.Width(100));
        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshReferences();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Brain状態
        DrawBrainStatus();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 各VCam状態
        DrawVCamStatus("PlayerTurn", "AI手札を見る", vcamPlayerTurn, Color.cyan);
        DrawVCamStatus("AITurn", "Player手札を見る", vcamAITurn, new Color(1f, 0.4f, 0.4f));
        DrawVCamStatus("CardFocus", "カードズーム", vcamCardFocus, Color.yellow);
        DrawVCamStatus("AIReaction", "AI顔リアクション", vcamAIReaction, Color.magenta);
        DrawVCamStatus("TableOverview", "テーブル全景", vcamTableOverview, new Color(0.5f, 1f, 0.5f));

        // Play Mode時のみカメラ切り替えボタン
        if (Application.isPlaying && cameraSystem != null)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            DrawSwitchButtons();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawBrainStatus()
    {
        EditorGUILayout.LabelField("CinemachineBrain", EditorStyles.boldLabel);

        if (brain == null)
        {
            EditorGUILayout.HelpBox("CinemachineBrain が見つかりません", MessageType.Warning);
            return;
        }

        var activeVCam = brain.ActiveVirtualCamera as CinemachineCamera;
        string activeName = activeVCam != null ? activeVCam.name : "(none)";

        // ブレンド中かどうか
        bool isBlending = brain.IsBlending;

        EditorGUI.indentLevel++;

        // アクティブカメラ名を大きく表示
        var origColor = GUI.contentColor;

        if (isBlending)
        {
            GUI.contentColor = new Color(1f, 0.85f, 0.3f);
            EditorGUILayout.LabelField($"Active:  {activeName}  (Blending...)", EditorStyles.boldLabel);
        }
        else
        {
            GUI.contentColor = new Color(0.3f, 1f, 0.3f);
            EditorGUILayout.LabelField($"Active:  {activeName}", EditorStyles.boldLabel);
        }
        GUI.contentColor = origColor;

        EditorGUI.indentLevel--;
    }

    private void DrawVCamStatus(string shortName, string description, CinemachineCamera vcam, Color themeColor)
    {
        if (vcam == null)
        {
            EditorGUILayout.BeginHorizontal(inactiveStyle);
            EditorGUILayout.LabelField($"  {shortName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("NOT FOUND", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            return;
        }

        int priority = vcam.Priority != null ? vcam.Priority.Value : 0;
        bool isActive = IsActiveCamera(vcam);
        bool isBlendTarget = IsBlendingTo(vcam);

        // 背景色
        var origBg = GUI.backgroundColor;
        if (isActive)
            GUI.backgroundColor = new Color(themeColor.r * 0.5f + 0.2f, themeColor.g * 0.5f + 0.2f, themeColor.b * 0.5f + 0.2f, 1f);
        else if (isBlendTarget)
            GUI.backgroundColor = new Color(themeColor.r * 0.3f + 0.1f, themeColor.g * 0.3f + 0.1f, themeColor.b * 0.3f + 0.1f, 0.8f);

        var style = isActive ? activeStyle : (isBlendTarget ? blendingStyle : inactiveStyle);
        EditorGUILayout.BeginHorizontal(style);

        // 状態インジケーター
        string indicator = isActive ? ">>>" : (isBlendTarget ? " ~>" : "   ");
        var origContent = GUI.contentColor;
        if (isActive) GUI.contentColor = themeColor;
        EditorGUILayout.LabelField(indicator, GUILayout.Width(28));
        GUI.contentColor = origContent;

        // カメラ名
        EditorGUILayout.LabelField($"{shortName}", isActive ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(90));

        // Priority
        string priorityLabel = $"P:{priority}";
        EditorGUILayout.LabelField(priorityLabel, GUILayout.Width(40));

        // 説明
        EditorGUILayout.LabelField(description, EditorStyles.miniLabel);

        // 選択ボタン
        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(48)))
        {
            Selection.activeGameObject = vcam.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = origBg;
    }

    private bool IsActiveCamera(CinemachineCamera vcam)
    {
        if (brain != null)
        {
            var active = brain.ActiveVirtualCamera as CinemachineCamera;
            if (active == vcam) return true;
        }

        // Brain未接続時はPriorityで判断
        if (vcam.Priority != null && vcam.Priority.Value >= 15) return true;

        return false;
    }

    private bool IsBlendingTo(CinemachineCamera vcam)
    {
        if (brain == null || !brain.IsBlending) return false;

        // ブレンド中のターゲットカメラかどうか
        var activeBlend = brain.ActiveBlend;
        if (activeBlend != null)
        {
            var camB = activeBlend.CamB as CinemachineCamera;
            if (camB == vcam) return true;
        }
        return false;
    }

    private void DrawSwitchButtons()
    {
        EditorGUILayout.LabelField("Camera Switch (Play Mode)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("PlayerTurn", GUILayout.Height(26)))
            cameraSystem.ShowPlayerTurnView();
        if (GUILayout.Button("AITurn", GUILayout.Height(26)))
            cameraSystem.ShowAITurnView();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("AIReaction", GUILayout.Height(26)))
            cameraSystem.ShowAIReactionView();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("TableOverview", GUILayout.Height(26)))
            cameraSystem.ShowTableOverview();

        // CardFocusは動的ターゲットが必要なのでボタンなし
        GUI.enabled = false;
        GUILayout.Button("CardFocus (auto)", GUILayout.Height(26));
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }
}
