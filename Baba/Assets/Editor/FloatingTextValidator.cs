using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// FloatingTextSystemのフォント設定を検証・修正するツール
/// Tools > Baba > Validate FloatingText Font から実行
/// </summary>
public class FloatingTextValidator : EditorWindow
{
    private FloatingTextSystem floatingTextSystem;
    private TMP_FontAsset currentFont;
    private TMP_FontAsset recommendedFont;

    [MenuItem("Tools/Baba/Validate FloatingText Font")]
    public static void ShowWindow()
    {
        var window = GetWindow<FloatingTextValidator>("FloatingText Font Validator");
        window.minSize = new Vector2(450, 300);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        // シーンからFloatingTextSystemを検索
        floatingTextSystem = FindObjectOfType<FloatingTextSystem>();

        if (floatingTextSystem != null)
        {
            SerializedObject so = new SerializedObject(floatingTextSystem);
            SerializedProperty fontProp = so.FindProperty("fontAsset");
            currentFont = fontProp.objectReferenceValue as TMP_FontAsset;
        }

        // 推奨フォント（NotoSansJP）を検索
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("NotoSansJP") || path.Contains("NotoSans"))
            {
                recommendedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                break;
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("FloatingText Font Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (floatingTextSystem == null)
        {
            EditorGUILayout.HelpBox(
                "FloatingTextSystemがシーンに見つかりません。\n\n" +
                "Tools > Baba > Setup FloatingText System でセットアップしてください。",
                MessageType.Error);
            return;
        }

        // 現在の状態を表示
        EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("FloatingTextSystem:", floatingTextSystem.name);

        if (currentFont != null)
        {
            EditorGUILayout.LabelField("Current Font:", currentFont.name);

            // フォント名から日本語対応をチェック
            bool isJapaneseFont = currentFont.name.Contains("NotoSansJP") ||
                                 currentFont.name.Contains("NotoSans") ||
                                 currentFont.name.Contains("Japanese");

            if (isJapaneseFont)
            {
                EditorGUILayout.HelpBox("✓ 日本語対応フォントが設定されています。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "⚠ 日本語非対応フォントが設定されています。\n" +
                    "日本語テキストが文字化けする可能性があります。",
                    MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "✗ フォントが設定されていません！\n" +
                "FloatingTextが文字化けします。必ずフォントを設定してください。",
                MessageType.Error);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // 推奨フォント情報
        EditorGUILayout.LabelField("Recommended Font", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (recommendedFont != null)
        {
            EditorGUILayout.LabelField("Recommended:", recommendedFont.name);
            EditorGUILayout.LabelField("Path:", AssetDatabase.GetAssetPath(recommendedFont));

            EditorGUILayout.Space();

            // 修正ボタン
            if (currentFont != recommendedFont)
            {
                if (GUILayout.Button("Fix: Apply Recommended Font", GUILayout.Height(35)))
                {
                    ApplyRecommendedFont();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ 推奨フォントが既に適用されています。", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "NotoSansJPフォントが見つかりません。\n" +
                "Fonts/フォルダにNotoSansJP SDF Assetが存在するか確認してください。",
                MessageType.Error);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // リフレッシュボタン
        if (GUILayout.Button("Refresh Status"))
        {
            RefreshStatus();
            Repaint();
        }
    }

    private void ApplyRecommendedFont()
    {
        if (floatingTextSystem == null || recommendedFont == null)
        {
            EditorUtility.DisplayDialog("Error", "フォントの適用に失敗しました。", "OK");
            return;
        }

        Undo.RecordObject(floatingTextSystem, "Apply Recommended Font");

        SerializedObject so = new SerializedObject(floatingTextSystem);
        SerializedProperty fontProp = so.FindProperty("fontAsset");
        fontProp.objectReferenceValue = recommendedFont;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(floatingTextSystem);

        Debug.Log($"[FloatingTextValidator] Applied font: {recommendedFont.name}");

        EditorUtility.DisplayDialog("Success",
            $"推奨フォント「{recommendedFont.name}」を適用しました。\n\n" +
            "変更を保存するには、シーンを保存してください。",
            "OK");

        RefreshStatus();
        Repaint();
    }
}
