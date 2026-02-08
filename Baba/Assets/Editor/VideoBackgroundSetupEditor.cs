using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// 動画背景の自動セットアップツール
/// Unity Editor拡張
/// </summary>
public class VideoBackgroundSetupEditor : EditorWindow
{
    private const string VIDEO_FILENAME = "osushidaisuki_a_Background_image_for_a_mental_casino_religiou_4ff5a0a0-e427-4b6a-b59a-14d8fa43d47a_2";

    [MenuItem("Tools/Baba/Video Background Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<VideoBackgroundSetupEditor>("Video Background Setup");
        window.minSize = new Vector2(400, 200);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Background Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "現在のシーンに動画背景システムを追加します。\n" +
            "3面のQuad（後方・左・右）にMP4動画をループ再生します。",
            MessageType.Info);

        GUILayout.Space(20);

        if (GUILayout.Button("Setup Video Background", GUILayout.Height(40)))
        {
            SetupVideoBackground();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Remove Video Background", GUILayout.Height(30)))
        {
            RemoveVideoBackground();
        }
    }

    private void SetupVideoBackground()
    {
        Debug.Log("[VideoBackgroundSetup] Setting up Video Background...");

        // Check if already exists
        VideoBackgroundSystem existing = FindFirstObjectByType<VideoBackgroundSystem>();
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("VideoBackgroundSystem Already Exists",
                "VideoBackgroundSystem は既に存在します。削除して新規作成しますか？",
                "はい（削除して作成）", "いいえ（キャンセル）"))
            {
                return;
            }

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // Find video clip
        string[] guids = AssetDatabase.FindAssets(VIDEO_FILENAME + " t:VideoClip");
        UnityEngine.Video.VideoClip videoClip = null;

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            videoClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);
            Debug.Log($"[VideoBackgroundSetup] Found video clip: {path}");
        }
        else
        {
            Debug.LogWarning("[VideoBackgroundSetup] Video clip not found! Searching all mp4 files...");
            string[] allGuids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Movie" });
            if (allGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(allGuids[0]);
                videoClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(path);
                Debug.Log($"[VideoBackgroundSetup] Using fallback video clip: {path}");
            }
        }

        if (videoClip == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Assets/Movie/ に動画ファイルが見つかりません。",
                "OK");
            return;
        }

        // Create VideoBackgroundSystem GameObject
        GameObject bgObj = new GameObject("VideoBackgroundSystem");
        Undo.RegisterCreatedObjectUndo(bgObj, "Create VideoBackgroundSystem");

        VideoBackgroundSystem bgSystem = bgObj.AddComponent<VideoBackgroundSystem>();
        Undo.RecordObject(bgSystem, "Add VideoBackgroundSystem");

        // Assign video clip via SerializedObject
        SerializedObject serializedBg = new SerializedObject(bgSystem);
        serializedBg.FindProperty("videoClip").objectReferenceValue = videoClip;
        serializedBg.ApplyModifiedProperties();

        EditorUtility.SetDirty(bgObj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[VideoBackgroundSetup] Video Background setup complete!");
        EditorUtility.DisplayDialog("Setup Complete",
            "VideoBackgroundSystem が作成されました。\n\n" +
            "Play モードで動画背景が3面のQuadに表示されます。\n" +
            "Inspector で brightness や tintColor を調整できます。",
            "OK");
    }

    private void RemoveVideoBackground()
    {
        VideoBackgroundSystem existing = FindFirstObjectByType<VideoBackgroundSystem>();
        if (existing == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "VideoBackgroundSystem が見つかりません。",
                "OK");
            return;
        }

        Undo.DestroyObjectImmediate(existing.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[VideoBackgroundSetup] Video Background removed.");
        EditorUtility.DisplayDialog("Removed",
            "VideoBackgroundSystem を削除しました。",
            "OK");
    }
}
