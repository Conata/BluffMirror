using UnityEditor;
using UnityEngine;
using FPSTrump.Psychology;

/// <summary>
/// PlayerBehaviorAnalyzerのshowDebugOverlayを一括でfalseに設定するツール
/// </summary>
public class BehaviorAnalysisDebugToggle : EditorWindow
{
    [MenuItem("Tools/Baba/Disable Behavior Analysis Debug Overlay")]
    public static void DisableAllDebugOverlays()
    {
        var analyzers = FindObjectsByType<PlayerBehaviorAnalyzer>(FindObjectsSortMode.None);

        if (analyzers.Length == 0)
        {
            Debug.LogWarning("[BehaviorAnalysisDebugToggle] No PlayerBehaviorAnalyzer found in scene");
            return;
        }

        foreach (var analyzer in analyzers)
        {
            var serializedObject = new SerializedObject(analyzer);
            var property = serializedObject.FindProperty("showDebugOverlay");

            if (property != null)
            {
                Undo.RecordObject(analyzer, "Disable Debug Overlay");
                property.boolValue = false;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(analyzer);

                Debug.Log($"[BehaviorAnalysisDebugToggle] Set showDebugOverlay=false on {analyzer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[BehaviorAnalysisDebugToggle] showDebugOverlay property not found on {analyzer.gameObject.name}");
            }
        }

        EditorUtility.DisplayDialog(
            "Behavior Analysis Debug Overlay",
            $"Disabled debug overlay for {analyzers.Length} PlayerBehaviorAnalyzer(s)",
            "OK"
        );
    }
}
