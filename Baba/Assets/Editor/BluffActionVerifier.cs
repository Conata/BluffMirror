using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Text;

/// <summary>
/// ブラフアクションシステムの設定検証ツール
/// Tools > Baba > Verify BluffAction Setup から実行
/// </summary>
public class BluffActionVerifier : EditorWindow
{
    private Vector2 scrollPosition;
    private StringBuilder report;
    private bool hasErrors = false;
    private bool hasWarnings = false;

    [MenuItem("Tools/Baba/Verify BluffAction Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<BluffActionVerifier>("BluffAction Verifier");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BluffAction Setup Verifier", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "ブラフアクションシステムの設定を検証します。\n" +
            "問題があれば詳細レポートを表示します。",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Run Verification", GUILayout.Height(40)))
        {
            RunVerification();
        }

        EditorGUILayout.Space();

        if (report != null && report.Length > 0)
        {
            // サマリー表示
            if (hasErrors)
            {
                EditorGUILayout.HelpBox("❌ エラーが見つかりました。修正が必要です。", MessageType.Error);
            }
            else if (hasWarnings)
            {
                EditorGUILayout.HelpBox("⚠️ 警告があります。確認してください。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✅ すべて正常です！", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Verification Report:", EditorStyles.boldLabel);

            // スクロール可能なレポート表示
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(report.ToString(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (hasErrors)
            {
                if (GUILayout.Button("Run Setup Tool to Fix", GUILayout.Height(35)))
                {
                    BluffActionUIAutoSetup.ShowWindow();
                }
            }
        }
    }

    private void RunVerification()
    {
        report = new StringBuilder();
        hasErrors = false;
        hasWarnings = false;

        report.AppendLine("=== BluffAction Setup Verification ===");
        report.AppendLine($"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // 1. BluffActionSystem検証
        VerifyBluffActionSystem();

        // 2. BluffActionUI検証
        VerifyBluffActionUI();

        // 3. HandController検証
        VerifyHandControllers();

        // 4. GameManager連携検証
        VerifyGameManagerIntegration();

        report.AppendLine();
        report.AppendLine("=== Verification Complete ===");

        if (!hasErrors && !hasWarnings)
        {
            report.AppendLine("✅ All checks passed!");
        }
        else
        {
            report.AppendLine($"Errors: {(hasErrors ? "YES" : "NO")}, Warnings: {(hasWarnings ? "YES" : "NO")}");
        }

        Debug.Log($"[BluffActionVerifier] Verification complete. Errors: {hasErrors}, Warnings: {hasWarnings}");
    }

    private void VerifyBluffActionSystem()
    {
        report.AppendLine("--- BluffActionSystem ---");

        var system = FindFirstObjectByType<BluffActionSystem>();

        if (system == null)
        {
            report.AppendLine("❌ ERROR: BluffActionSystem not found in scene!");
            report.AppendLine("   → Run 'Tools > Baba > Setup BluffAction UI' to create it.");
            hasErrors = true;
            report.AppendLine();
            return;
        }

        report.AppendLine($"✅ BluffActionSystem found: {system.gameObject.name}");

        // playerHand/aiHand チェック
        var so = new SerializedObject(system);
        var playerHandProp = so.FindProperty("playerHand");
        var aiHandProp = so.FindProperty("aiHand");

        if (playerHandProp.objectReferenceValue == null)
        {
            report.AppendLine("⚠️  WARNING: playerHand is not assigned.");
            hasWarnings = true;
        }
        else
        {
            report.AppendLine($"   playerHand: {playerHandProp.objectReferenceValue.name}");
        }

        if (aiHandProp.objectReferenceValue == null)
        {
            report.AppendLine("⚠️  WARNING: aiHand is not assigned.");
            hasWarnings = true;
        }
        else
        {
            report.AppendLine($"   aiHand: {aiHandProp.objectReferenceValue.name}");
        }

        report.AppendLine();
    }

    private void VerifyBluffActionUI()
    {
        report.AppendLine("--- BluffActionUI ---");

        var ui = FindFirstObjectByType<BluffActionUI>();

        if (ui == null)
        {
            report.AppendLine("❌ ERROR: BluffActionUI not found in scene!");
            report.AppendLine("   → Run 'Tools > Baba > Setup BluffAction UI' to create it.");
            hasErrors = true;
            report.AppendLine();
            return;
        }

        report.AppendLine($"✅ BluffActionUI found: {ui.gameObject.name}");

        // Canvas チェック
        var canvas = ui.GetComponent<Canvas>();
        if (canvas == null)
        {
            report.AppendLine("❌ ERROR: Canvas component missing!");
            hasErrors = true;
        }
        else
        {
            report.AppendLine($"   Canvas: RenderMode={canvas.renderMode}, SortingOrder={canvas.sortingOrder}");
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                report.AppendLine("⚠️  WARNING: Canvas RenderMode should be ScreenSpaceOverlay.");
                hasWarnings = true;
            }
        }

        // Button チェック
        var so = new SerializedObject(ui);
        CheckButton(so, "shuffleButton", "Shuffle Button");
        CheckButton(so, "pushPullButton", "Push-Pull Button");
        CheckButton(so, "wiggleButton", "Wiggle Button");
        CheckButton(so, "spreadCloseButton", "Spread-Close Button");
        CheckButton(so, "cancelButton", "Cancel Button");

        // Overlay チェック
        var overlayProp = so.FindProperty("selectionOverlay");
        if (overlayProp.objectReferenceValue == null)
        {
            report.AppendLine("⚠️  WARNING: selectionOverlay is not assigned.");
            hasWarnings = true;
        }
        else
        {
            report.AppendLine($"   Selection Overlay: {overlayProp.objectReferenceValue.name}");
        }

        report.AppendLine();
    }

    private void CheckButton(SerializedObject so, string propName, string displayName)
    {
        var prop = so.FindProperty(propName);
        if (prop.objectReferenceValue == null)
        {
            report.AppendLine($"⚠️  WARNING: {displayName} is not assigned.");
            hasWarnings = true;
        }
        else
        {
            var button = prop.objectReferenceValue as Button;
            if (button == null)
            {
                report.AppendLine($"❌ ERROR: {displayName} is not a Button component!");
                hasErrors = true;
            }
            else
            {
                report.AppendLine($"   {displayName}: OK");
            }
        }
    }

    private void VerifyHandControllers()
    {
        report.AppendLine("--- Hand Controllers ---");

        var playerHand = FindFirstObjectByType<PlayerHandController>();
        var aiHand = FindFirstObjectByType<AIHandController>();

        if (playerHand == null)
        {
            report.AppendLine("❌ ERROR: PlayerHandController not found in scene!");
            hasErrors = true;
        }
        else
        {
            report.AppendLine($"✅ PlayerHandController found: {playerHand.gameObject.name}");
        }

        if (aiHand == null)
        {
            report.AppendLine("❌ ERROR: AIHandController not found in scene!");
            hasErrors = true;
        }
        else
        {
            report.AppendLine($"✅ AIHandController found: {aiHand.gameObject.name}");
        }

        report.AppendLine();
    }

    private void VerifyGameManagerIntegration()
    {
        report.AppendLine("--- GameManager Integration ---");

        var gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            report.AppendLine("⚠️  WARNING: GameManager not found in scene.");
            hasWarnings = true;
            report.AppendLine();
            return;
        }

        report.AppendLine($"✅ GameManager found: {gameManager.gameObject.name}");

        // GameManagerのコードが正しくBluffActionSystemを呼んでいるか確認（スクリプト解析）
        var script = MonoScript.FromMonoBehaviour(gameManager);
        if (script != null)
        {
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string scriptContent = System.IO.File.ReadAllText(scriptPath);

            bool hasStartAIBluff = scriptContent.Contains("BluffActionSystem.Instance?.StartAIBluffMonitor()") ||
                                   scriptContent.Contains("BluffActionSystem.Instance.StartAIBluffMonitor()");
            bool hasShowUI = scriptContent.Contains("BluffActionUI.Instance?.Show()") ||
                             scriptContent.Contains("BluffActionUI.Instance.Show()");

            if (hasStartAIBluff)
            {
                report.AppendLine("   ✅ GameManager calls StartAIBluffMonitor()");
            }
            else
            {
                report.AppendLine("   ❌ ERROR: GameManager does NOT call StartAIBluffMonitor()!");
                hasErrors = true;
            }

            if (hasShowUI)
            {
                report.AppendLine("   ✅ GameManager calls BluffActionUI.Show()");
            }
            else
            {
                report.AppendLine("   ⚠️  WARNING: GameManager does NOT call BluffActionUI.Show()");
                hasWarnings = true;
            }
        }

        report.AppendLine();
    }
}
