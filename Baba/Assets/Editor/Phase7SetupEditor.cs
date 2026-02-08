using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Phase 7: タイトル画面 + イントロ演出の自動セットアップツール
/// Unity Editor拡張
/// </summary>
public class Phase7SetupEditor : EditorWindow
{
    private enum SetupTarget
    {
        TitleScreen,
        IntroSequence,
        SubtitleUI,
        ResultUI,
        All
    }

    private SetupTarget setupTarget = SetupTarget.All;

    [MenuItem("Tools/Baba/Phase 7: Setup Title & Intro")]
    public static void ShowWindow()
    {
        var window = GetWindow<Phase7SetupEditor>("Phase 7 Setup");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Phase 7: Title Screen + Intro Sequence Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "このツールは Phase 7 のタイトル画面とイントロ演出を自動セットアップします。",
            MessageType.Info);

        GUILayout.Space(10);

        setupTarget = (SetupTarget)EditorGUILayout.EnumPopup("Setup Target", setupTarget);

        GUILayout.Space(20);

        // === タイトル画面セットアップ ===
        if (setupTarget == SetupTarget.TitleScreen || setupTarget == SetupTarget.All)
        {
            GUILayout.Label("1. Title Screen Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "StartMenuScen.unity に TitleUI GameObject を追加します。\n" +
                "Canvas, CanvasGroup, TitleUI component が自動作成されます。",
                MessageType.None);

            if (GUILayout.Button("Setup Title Screen in Current Scene", GUILayout.Height(30)))
            {
                SetupTitleScreen();
            }

            GUILayout.Space(10);
        }

        // === イントロ演出セットアップ ===
        if (setupTarget == SetupTarget.IntroSequence || setupTarget == SetupTarget.All)
        {
            GUILayout.Label("2. Intro Sequence Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FPS_Trump_Scene.unity に GameIntroSequence GameObject を追加します。\n" +
                "照明（AmbienceLight, TVHeadSpotlight, TableSpotlight）も自動作成されます。",
                MessageType.None);

            if (GUILayout.Button("Setup Intro Sequence in Current Scene", GUILayout.Height(30)))
            {
                SetupIntroSequence();
            }

            GUILayout.Space(10);
        }

        GUILayout.Space(20);

        // === 字幕UIセットアップ ===
        if (setupTarget == SetupTarget.SubtitleUI || setupTarget == SetupTarget.All)
        {
            GUILayout.Label("3. Subtitle UI Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FPS_Trump_Scene.unity に SubtitleUI GameObject を追加します。\\n" +
                "画面下部固定字幕システムが自動作成されます。",
                MessageType.None);

            if (GUILayout.Button("Setup Subtitle UI in Current Scene", GUILayout.Height(30)))
            {
                SetupSubtitleUI();
            }

            GUILayout.Space(10);
        }

        // === リザルトUIセットアップ ===
        if (setupTarget == SetupTarget.ResultUI || setupTarget == SetupTarget.All)
        {
            GUILayout.Label("4. Result UI Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FPS_Trump_Scene.unity に ResultUI GameObject を追加します。\\n" +
                "診断結果表示システム（テキスト、ボタン、スタッツバー）が自動作成されます。",
                MessageType.None);

            if (GUILayout.Button("Setup Result UI in Current Scene", GUILayout.Height(30)))
            {
                SetupResultUI();
            }

            GUILayout.Space(10);
        }

        // === 完全セットアップ ===
        if (setupTarget == SetupTarget.All)
        {
            EditorGUILayout.HelpBox(
                "注意: 両方のシーンを開いて実行する必要があります。",
                MessageType.Warning);

            if (GUILayout.Button("Setup All (Current Scene Only)", GUILayout.Height(40)))
            {
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName.Contains("StartMenu") || sceneName.Contains("StartScene"))
                {
                    SetupTitleScreen();
                }
                else if (sceneName.Contains("FPS_Trump_Scene"))
                {
                    SetupIntroSequence();
                    SetupSubtitleUI();
                    SetupResultUI();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error",
                        "現在のシーンが StartMenuScen または FPS_Trump_Scene ではありません。",
                        "OK");
                }
            }
        }
    }

    /// <summary>
    /// タイトル画面セットアップ
    /// </summary>
    private void SetupTitleScreen()
    {
        Debug.Log("[Phase7Setup] Setting up Title Screen...");

        // TitleUI が既に存在するか確認
        TitleUI existingTitleUI = FindFirstObjectByType<TitleUI>();
        if (existingTitleUI != null)
        {
            if (!EditorUtility.DisplayDialog("TitleUI Already Exists",
                "TitleUI は既に存在します。削除して新規作成しますか？",
                "はい（削除して作成）", "いいえ（キャンセル）"))
            {
                return;
            }

            Undo.DestroyObjectImmediate(existingTitleUI.gameObject);
        }

        // TitleUI GameObject作成
        GameObject titleUIObj = new GameObject("TitleUI");
        Undo.RegisterCreatedObjectUndo(titleUIObj, "Create TitleUI");

        // Canvas作成
        Canvas canvas = titleUIObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Undo.RecordObject(canvas, "Setup Canvas");

        UnityEngine.UI.CanvasScaler scaler = titleUIObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Undo.RecordObject(scaler, "Setup CanvasScaler");

        titleUIObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // CanvasGroup作成
        CanvasGroup titlePanel = titleUIObj.AddComponent<CanvasGroup>();
        Undo.RecordObject(titlePanel, "Add CanvasGroup");

        // TitleUI component追加
        TitleUI titleUI = titleUIObj.AddComponent<TitleUI>();
        Undo.RecordObject(titleUI, "Add TitleUI");

        // Settings Panel作成
        GameObject settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(titleUIObj.transform, false);
        Undo.RegisterCreatedObjectUndo(settingsPanel, "Create SettingsPanel");

        RectTransform settingsRect = settingsPanel.AddComponent<RectTransform>();
        settingsRect.anchorMin = Vector2.zero;
        settingsRect.anchorMax = Vector2.one;
        settingsRect.sizeDelta = Vector2.zero;

        CanvasGroup settingsCG = settingsPanel.AddComponent<CanvasGroup>();
        Undo.RecordObject(settingsCG, "Add SettingsPanel CanvasGroup");

        settingsPanel.SetActive(false);

        // TitleUI にパネルを割り当て（Serialized Fieldなのでreflectionで設定）
        SerializedObject serializedTitleUI = new SerializedObject(titleUI);
        serializedTitleUI.FindProperty("titlePanel").objectReferenceValue = titlePanel;
        serializedTitleUI.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serializedTitleUI.FindProperty("gameSceneName").stringValue = "FPS_Trump_Scene";
        serializedTitleUI.ApplyModifiedProperties();

        EditorUtility.SetDirty(titleUIObj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[Phase7Setup] ✅ Title Screen setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "TitleUI GameObject が作成されました。\n\n" +
            "次のステップ:\n" +
            "1. TitleUI Inspector で UI要素を手動で配置\n" +
            "2. ボタンとスライダーを追加\n" +
            "3. TitleUI の SerializedField に割り当て",
            "OK");
    }

    /// <summary>
    /// イントロ演出セットアップ
    /// </summary>
    private void SetupIntroSequence()
    {
        Debug.Log("[Phase7Setup] Setting up Intro Sequence...");

        // GameIntroSequence が既に存在するか確認
        GameIntroSequence existingIntro = FindFirstObjectByType<GameIntroSequence>();
        if (existingIntro != null)
        {
            if (!EditorUtility.DisplayDialog("GameIntroSequence Already Exists",
                "GameIntroSequence は既に存在します。削除して新規作成しますか？",
                "はい（削除して作成）", "いいえ（キャンセル）"))
            {
                return;
            }

            Undo.DestroyObjectImmediate(existingIntro.gameObject);
        }

        // GameIntroSequence GameObject作成
        GameObject introObj = new GameObject("GameIntroSequence");
        Undo.RegisterCreatedObjectUndo(introObj, "Create GameIntroSequence");

        GameIntroSequence intro = introObj.AddComponent<GameIntroSequence>();
        Undo.RecordObject(intro, "Add GameIntroSequence");

        // 照明セットアップ
        SetupLighting();

        EditorUtility.SetDirty(introObj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[Phase7Setup] ✅ Intro Sequence setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "GameIntroSequence GameObject が作成されました。\n\n" +
            "照明も自動作成されました:\n" +
            "- AmbienceLight (Directional)\n" +
            "- TVHeadSpotlight (Spot, Cyan)\n" +
            "- TableSpotlight (Spot, Warm)\n\n" +
            "GameIntroSequence は依存関係を自動検索します。",
            "OK");
    }

    /// <summary>
    /// 照明セットアップ
    /// </summary>
    private void SetupLighting()
    {
        Debug.Log("[Phase7Setup] Setting up lighting...");

        // AmbienceLight作成
        Light ambienceLight = GameObject.Find("AmbienceLight")?.GetComponent<Light>();
        if (ambienceLight == null)
        {
            GameObject ambienceObj = new GameObject("AmbienceLight");
            Undo.RegisterCreatedObjectUndo(ambienceObj, "Create AmbienceLight");

            ambienceLight = ambienceObj.AddComponent<Light>();
            ambienceLight.type = LightType.Directional;
            ambienceLight.intensity = 0.7f;
            ambienceLight.color = Color.white;

            ambienceObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Debug.Log("[Phase7Setup] Created AmbienceLight");
        }
        else
        {
            Debug.Log("[Phase7Setup] AmbienceLight already exists");
        }

        // TVHeadSpotlight作成
        Light tvSpotlight = GameObject.Find("TVHeadSpotlight")?.GetComponent<Light>();
        if (tvSpotlight == null)
        {
            GameObject tvSpotObj = new GameObject("TVHeadSpotlight");
            Undo.RegisterCreatedObjectUndo(tvSpotObj, "Create TVHeadSpotlight");

            tvSpotlight = tvSpotObj.AddComponent<Light>();
            tvSpotlight.type = LightType.Spot;
            tvSpotlight.intensity = 0f; // 初期は0（イントロ中に点灯）
            tvSpotlight.color = new Color(0.3f, 0.9f, 1f); // Cyan
            tvSpotlight.range = 10f;
            tvSpotlight.spotAngle = 30f;

            // TVHead の位置を探す
            TVHeadAnimator tvHead = FindFirstObjectByType<TVHeadAnimator>();
            if (tvHead != null)
            {
                tvSpotObj.transform.position = tvHead.transform.position + Vector3.up * 2f + Vector3.back * 1f;
                tvSpotObj.transform.LookAt(tvHead.transform);
                Debug.Log("[Phase7Setup] TVHeadSpotlight positioned at TVHead");
            }
            else
            {
                tvSpotObj.transform.position = new Vector3(0, 2, -1);
                tvSpotObj.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                Debug.LogWarning("[Phase7Setup] TVHead not found - TVHeadSpotlight at default position");
            }

            Debug.Log("[Phase7Setup] Created TVHeadSpotlight");
        }
        else
        {
            Debug.Log("[Phase7Setup] TVHeadSpotlight already exists");
        }

        // TableSpotlight作成
        Light tableSpotlight = GameObject.Find("TableSpotlight")?.GetComponent<Light>();
        if (tableSpotlight == null)
        {
            GameObject tableSpotObj = new GameObject("TableSpotlight");
            Undo.RegisterCreatedObjectUndo(tableSpotObj, "Create TableSpotlight");

            tableSpotlight = tableSpotObj.AddComponent<Light>();
            tableSpotlight.type = LightType.Spot;
            tableSpotlight.intensity = 0f; // 初期は0（イントロ中に点灯）
            tableSpotlight.color = new Color(1f, 0.95f, 0.8f); // Warm white
            tableSpotlight.range = 10f;
            tableSpotlight.spotAngle = 60f;

            tableSpotObj.transform.position = new Vector3(0, 3, 0);
            tableSpotObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Debug.Log("[Phase7Setup] Created TableSpotlight");
        }
        else
        {
            Debug.Log("[Phase7Setup] TableSpotlight already exists");
        }
    }

    /// <summary>
    /// 字幕UIセットアップ
    /// </summary>
    private void SetupSubtitleUI()
    {
        Debug.Log("[Phase7Setup] Setting up Subtitle UI...");

        // SubtitleUI が既に存在するか確認
        SubtitleUI existingSubtitleUI = FindFirstObjectByType<SubtitleUI>();
        if (existingSubtitleUI != null)
        {
            if (!EditorUtility.DisplayDialog("SubtitleUI Already Exists",
                "SubtitleUI は既に存在します。削除して新規作成しますか？",
                "はい（削除して作成）", "いいえ（キャンセル）"))
            {
                return;
            }

            Undo.DestroyObjectImmediate(existingSubtitleUI.gameObject);
        }

        // SubtitleUI GameObject作成
        GameObject subtitleUIObj = new GameObject("SubtitleUI");
        Undo.RegisterCreatedObjectUndo(subtitleUIObj, "Create SubtitleUI");

        // Canvas作成
        Canvas canvas = subtitleUIObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 最前面
        Undo.RecordObject(canvas, "Setup Canvas");

        UnityEngine.UI.CanvasScaler scaler = subtitleUIObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Undo.RecordObject(scaler, "Setup CanvasScaler");

        subtitleUIObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Subtitle Panel 作成（画面下部 15-20%）
        GameObject panelObj = new GameObject("SubtitlePanel");
        panelObj.transform.SetParent(subtitleUIObj.transform, false);
        Undo.RegisterCreatedObjectUndo(panelObj, "Create SubtitlePanel");

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.2f); // 下部20%
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        CanvasGroup panelCG = panelObj.AddComponent<CanvasGroup>();
        panelCG.alpha = 0f; // 初期非表示
        Undo.RecordObject(panelCG, "Add CanvasGroup");

        // 背景パネル作成
        UnityEngine.UI.Image bgImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // 半透明黒
        Undo.RecordObject(bgImage, "Add Background Image");

        // テキスト作成
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(panelObj.transform, false);
        Undo.RegisterCreatedObjectUndo(textObj, "Create SubtitleText");

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-40, -20); // Padding
        textRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmpText.fontSize = 32;
        tmpText.alignment = TMPro.TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.text = "";
        tmpText.enableWordWrapping = true;
        Undo.RecordObject(tmpText, "Setup Text");

        // SubtitleUI component追加
        SubtitleUI subtitleUI = subtitleUIObj.AddComponent<SubtitleUI>();
        Undo.RecordObject(subtitleUI, "Add SubtitleUI");

        // SerializedField に割り当て
        SerializedObject serializedSubtitleUI = new SerializedObject(subtitleUI);
        serializedSubtitleUI.FindProperty("subtitlePanel").objectReferenceValue = panelCG;
        serializedSubtitleUI.FindProperty("subtitleText").objectReferenceValue = tmpText;
        serializedSubtitleUI.FindProperty("backgroundPanel").objectReferenceValue = bgImage;
        serializedSubtitleUI.ApplyModifiedProperties();

        EditorUtility.SetDirty(subtitleUIObj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[Phase7Setup] ✅ Subtitle UI setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "SubtitleUI GameObject が作成されました。\\n\\n" +
            "画面下部に固定字幕UIが自動作成されました。\\n" +
            "GameIntroSequence と AIHesitationController の\\n" +
            "textDisplayMode を 'Subtitle' または 'Both' に設定してください。",
            "OK");
    }

    /// <summary>
    /// リザルトUIセットアップ
    /// </summary>
    private void SetupResultUI()
    {
        Debug.Log("[Phase7Setup] Setting up Result UI...");

        // ResultUI が既に存在するか確認
        ResultUI existingResultUI = FindFirstObjectByType<ResultUI>();
        if (existingResultUI != null)
        {
            if (!EditorUtility.DisplayDialog("ResultUI Already Exists",
                "ResultUI は既に存在します。削除して新規作成しますか？",
                "はい（削除して作成）", "いいえ（キャンセル）"))
            {
                return;
            }

            Undo.DestroyObjectImmediate(existingResultUI.gameObject);
        }

        // === ResultUI GameObject作成 ===
        GameObject resultUIObj = new GameObject("ResultUI");
        Undo.RegisterCreatedObjectUndo(resultUIObj, "Create ResultUI");

        // Canvas作成
        Canvas canvas = resultUIObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // 最前面
        Undo.RecordObject(canvas, "Setup Canvas");

        UnityEngine.UI.CanvasScaler scaler = resultUIObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Undo.RecordObject(scaler, "Setup CanvasScaler");

        resultUIObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // === Background Group 作成 ===
        GameObject bgGroupObj = new GameObject("BackgroundGroup");
        bgGroupObj.transform.SetParent(resultUIObj.transform, false);
        Undo.RegisterCreatedObjectUndo(bgGroupObj, "Create BackgroundGroup");

        RectTransform bgRect = bgGroupObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        CanvasGroup bgCanvasGroup = bgGroupObj.AddComponent<CanvasGroup>();
        bgCanvasGroup.alpha = 0f;
        Undo.RecordObject(bgCanvasGroup, "Add BackgroundGroup CanvasGroup");

        UnityEngine.UI.Image bgImage = bgGroupObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.667f); // Semi-transparent black
        Undo.RecordObject(bgImage, "Add BackgroundGroup Image");

        // === Container Rect 作成 ===
        GameObject containerObj = new GameObject("Container");
        containerObj.transform.SetParent(bgGroupObj.transform, false);
        Undo.RegisterCreatedObjectUndo(containerObj, "Create Container");

        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.1f);
        containerRect.anchorMax = new Vector2(0.9f, 0.9f);
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;

        // === Header Text 作成 ===
        GameObject headerObj = new GameObject("HeaderText");
        headerObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(headerObj, "Create HeaderText");

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.9f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.sizeDelta = new Vector2(0, 60);
        headerRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI headerText = headerObj.AddComponent<TMPro.TextMeshProUGUI>();
        headerText.text = "行動パターン分析結果";
        headerText.fontSize = 48;
        headerText.alignment = TMPro.TextAlignmentOptions.Center;
        headerText.color = new Color(1f, 0.843f, 0f); // Gold
        Undo.RecordObject(headerText, "Setup HeaderText");

        // === Title Text 作成 ===
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(titleObj, "Create TitleText");

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.75f);
        titleRect.anchorMax = new Vector2(1f, 0.85f);
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "";
        titleText.fontSize = 60;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.843f, 0f); // Gold
        Undo.RecordObject(titleText, "Setup TitleText");

        // === Description Text 作成 ===
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(descObj, "Create DescriptionText");

        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.55f);
        descRect.anchorMax = new Vector2(0.95f, 0.72f);
        descRect.sizeDelta = Vector2.zero;
        descRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI descText = descObj.AddComponent<TMPro.TextMeshProUGUI>();
        descText.text = "";
        descText.fontSize = 28;
        descText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        descText.color = Color.white;
        descText.enableWordWrapping = true;
        Undo.RecordObject(descText, "Setup DescriptionText");

        // === Tendency Text 作成 ===
        GameObject tendencyObj = new GameObject("TendencyText");
        tendencyObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(tendencyObj, "Create TendencyText");

        RectTransform tendencyRect = tendencyObj.AddComponent<RectTransform>();
        tendencyRect.anchorMin = new Vector2(0.05f, 0.35f);
        tendencyRect.anchorMax = new Vector2(0.95f, 0.52f);
        tendencyRect.sizeDelta = Vector2.zero;
        tendencyRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI tendencyText = tendencyObj.AddComponent<TMPro.TextMeshProUGUI>();
        tendencyText.text = "";
        tendencyText.fontSize = 26;
        tendencyText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        tendencyText.color = Color.white;
        tendencyText.enableWordWrapping = true;
        Undo.RecordObject(tendencyText, "Setup TendencyText");

        // === Insight Text 作成 ===
        GameObject insightObj = new GameObject("InsightText");
        insightObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(insightObj, "Create InsightText");

        RectTransform insightRect = insightObj.AddComponent<RectTransform>();
        insightRect.anchorMin = new Vector2(0.05f, 0.15f);
        insightRect.anchorMax = new Vector2(0.95f, 0.32f);
        insightRect.sizeDelta = Vector2.zero;
        insightRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI insightText = insightObj.AddComponent<TMPro.TextMeshProUGUI>();
        insightText.text = "";
        insightText.fontSize = 26;
        insightText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        insightText.color = new Color(1f, 0.843f, 0f); // Gold
        insightText.enableWordWrapping = true;
        Undo.RecordObject(insightText, "Setup InsightText");

        // === Stat Bars 作成 (5個) ===
        GameObject statBarsParent = new GameObject("StatBars");
        statBarsParent.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(statBarsParent, "Create StatBars");

        RectTransform statBarsRect = statBarsParent.AddComponent<RectTransform>();
        statBarsRect.anchorMin = new Vector2(0.05f, 0.35f);
        statBarsRect.anchorMax = new Vector2(0.45f, 0.72f);
        statBarsRect.sizeDelta = Vector2.zero;
        statBarsRect.anchoredPosition = Vector2.zero;

        string[] statNames = { "決断力", "一貫性", "耐圧性", "直感力", "適応力" };
        UnityEngine.UI.Image[] statBarFills = new UnityEngine.UI.Image[5];
        TMPro.TextMeshProUGUI[] statBarLabels = new TMPro.TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            // StatBar GameObject
            GameObject statBarObj = new GameObject($"StatBar_{i}");
            statBarObj.transform.SetParent(statBarsParent.transform, false);
            Undo.RegisterCreatedObjectUndo(statBarObj, $"Create StatBar_{i}");

            RectTransform statBarRect = statBarObj.AddComponent<RectTransform>();
            float yPos = 1f - (i * 0.2f) - 0.1f;
            statBarRect.anchorMin = new Vector2(0f, yPos - 0.05f);
            statBarRect.anchorMax = new Vector2(1f, yPos + 0.05f);
            statBarRect.sizeDelta = Vector2.zero;
            statBarRect.anchoredPosition = Vector2.zero;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(statBarObj.transform, false);
            Undo.RegisterCreatedObjectUndo(labelObj, $"Create Label_{i}");

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.3f, 1f);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;

            TMPro.TextMeshProUGUI labelText = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            labelText.text = statNames[i];
            labelText.fontSize = 20;
            labelText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;
            statBarLabels[i] = labelText;
            Undo.RecordObject(labelText, $"Setup Label_{i}");

            // Bar Background
            GameObject barBgObj = new GameObject("BarBackground");
            barBgObj.transform.SetParent(statBarObj.transform, false);
            Undo.RegisterCreatedObjectUndo(barBgObj, $"Create BarBg_{i}");

            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.32f, 0.2f);
            barBgRect.anchorMax = new Vector2(1f, 0.8f);
            barBgRect.sizeDelta = Vector2.zero;
            barBgRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image barBgImage = barBgObj.AddComponent<UnityEngine.UI.Image>();
            barBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Undo.RecordObject(barBgImage, $"Setup BarBg_{i}");

            // Bar Fill
            GameObject barFillObj = new GameObject("BarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            Undo.RegisterCreatedObjectUndo(barFillObj, $"Create BarFill_{i}");

            RectTransform barFillRect = barFillObj.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.sizeDelta = Vector2.zero;
            barFillRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image barFillImage = barFillObj.AddComponent<UnityEngine.UI.Image>();
            barFillImage.type = UnityEngine.UI.Image.Type.Filled;
            barFillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            barFillImage.fillAmount = 0f;
            barFillImage.color = new Color(0f, 0.8f, 0.9f); // Cyan
            statBarFills[i] = barFillImage;
            Undo.RecordObject(barFillImage, $"Setup BarFill_{i}");
        }

        // === Replay Button 作成 ===
        GameObject replayBtnObj = new GameObject("ReplayButton");
        replayBtnObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(replayBtnObj, "Create ReplayButton");

        RectTransform replayRect = replayBtnObj.AddComponent<RectTransform>();
        replayRect.anchorMin = new Vector2(0.3f, 0.02f);
        replayRect.anchorMax = new Vector2(0.48f, 0.1f);
        replayRect.sizeDelta = Vector2.zero;
        replayRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image replayBtnImage = replayBtnObj.AddComponent<UnityEngine.UI.Image>();
        replayBtnImage.color = new Color(0f, 0.8f, 0.9f); // Cyan
        Undo.RecordObject(replayBtnImage, "Add ReplayButton Image");

        UnityEngine.UI.Button replayButton = replayBtnObj.AddComponent<UnityEngine.UI.Button>();
        Undo.RecordObject(replayButton, "Add ReplayButton Button");

        GameObject replayTextObj = new GameObject("Text");
        replayTextObj.transform.SetParent(replayBtnObj.transform, false);
        Undo.RegisterCreatedObjectUndo(replayTextObj, "Create ReplayButton Text");

        RectTransform replayTextRect = replayTextObj.AddComponent<RectTransform>();
        replayTextRect.anchorMin = Vector2.zero;
        replayTextRect.anchorMax = Vector2.one;
        replayTextRect.sizeDelta = Vector2.zero;
        replayTextRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI replayText = replayTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        replayText.text = "もう一度";
        replayText.fontSize = 28;
        replayText.alignment = TMPro.TextAlignmentOptions.Center;
        replayText.color = Color.white;
        Undo.RecordObject(replayText, "Setup ReplayButton Text");

        // === Menu Button 作成 ===
        GameObject menuBtnObj = new GameObject("MenuButton");
        menuBtnObj.transform.SetParent(containerObj.transform, false);
        Undo.RegisterCreatedObjectUndo(menuBtnObj, "Create MenuButton");

        RectTransform menuRect = menuBtnObj.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.52f, 0.02f);
        menuRect.anchorMax = new Vector2(0.7f, 0.1f);
        menuRect.sizeDelta = Vector2.zero;
        menuRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image menuBtnImage = menuBtnObj.AddComponent<UnityEngine.UI.Image>();
        menuBtnImage.color = new Color(0.5f, 0.5f, 0.5f); // Gray
        Undo.RecordObject(menuBtnImage, "Add MenuButton Image");

        UnityEngine.UI.Button menuButton = menuBtnObj.AddComponent<UnityEngine.UI.Button>();
        Undo.RecordObject(menuButton, "Add MenuButton Button");

        GameObject menuTextObj = new GameObject("Text");
        menuTextObj.transform.SetParent(menuBtnObj.transform, false);
        Undo.RegisterCreatedObjectUndo(menuTextObj, "Create MenuButton Text");

        RectTransform menuTextRect = menuTextObj.AddComponent<RectTransform>();
        menuTextRect.anchorMin = Vector2.zero;
        menuTextRect.anchorMax = Vector2.one;
        menuTextRect.sizeDelta = Vector2.zero;
        menuTextRect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI menuText = menuTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        menuText.text = "メニュー";
        menuText.fontSize = 28;
        menuText.alignment = TMPro.TextAlignmentOptions.Center;
        menuText.color = Color.white;
        Undo.RecordObject(menuText, "Setup MenuButton Text");

        // === ResultUI component追加 ===
        ResultUI resultUI = resultUIObj.AddComponent<ResultUI>();
        Undo.RecordObject(resultUI, "Add ResultUI");

        // === SerializedField に割り当て ===
        SerializedObject serializedResultUI = new SerializedObject(resultUI);
        serializedResultUI.FindProperty("resultCanvas").objectReferenceValue = canvas;
        serializedResultUI.FindProperty("backgroundGroup").objectReferenceValue = bgCanvasGroup;
        serializedResultUI.FindProperty("containerRect").objectReferenceValue = containerRect;
        serializedResultUI.FindProperty("headerText").objectReferenceValue = headerText;
        serializedResultUI.FindProperty("titleText").objectReferenceValue = titleText;
        serializedResultUI.FindProperty("descriptionText").objectReferenceValue = descText;
        serializedResultUI.FindProperty("tendencyText").objectReferenceValue = tendencyText;
        serializedResultUI.FindProperty("insightText").objectReferenceValue = insightText;
        serializedResultUI.FindProperty("replayButton").objectReferenceValue = replayButton;
        serializedResultUI.FindProperty("menuButton").objectReferenceValue = menuButton;

        // Stat Bars 配列を設定
        SerializedProperty statBarFillsProp = serializedResultUI.FindProperty("statBarFills");
        statBarFillsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            statBarFillsProp.GetArrayElementAtIndex(i).objectReferenceValue = statBarFills[i];
        }

        SerializedProperty statBarLabelsProp = serializedResultUI.FindProperty("statBarLabels");
        statBarLabelsProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            statBarLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = statBarLabels[i];
        }

        serializedResultUI.ApplyModifiedProperties();

        EditorUtility.SetDirty(resultUIObj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[Phase7Setup] ✅ Result UI setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "ResultUI GameObject が作成されました。\\n\\n" +
            "以下が自動作成されました:\\n" +
            "- Canvas (ScreenSpace Overlay)\\n" +
            "- 背景 + コンテナ\\n" +
            "- テキスト要素 (Header, Title, Description, Tendency, Insight)\\n" +
            "- ボタン (Replay, Menu)\\n" +
            "- スタッツバー x5 (決断力, 一貫性, 耐圧性, 直感力, 適応力)\\n\\n" +
            "ResultUI は依存関係を自動検索します。",
            "OK");
    }
}
