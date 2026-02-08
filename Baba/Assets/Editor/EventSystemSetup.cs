using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystemのセットアップを自動化するエディタツール
/// Tools > Baba > Setup Event System
/// </summary>
public static class EventSystemSetup
{
    [MenuItem("Tools/Baba/Setup Event System")]
    public static void SetupEventSystem()
    {
        // 現在のシーンでEventSystemを検索
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogWarning("[EventSystemSetup] No EventSystem found in the current scene.");
            EditorUtility.DisplayDialog(
                "EventSystem Not Found",
                "No EventSystem found in the current scene. Please add an EventSystem first.",
                "OK"
            );
            return;
        }

        // PersistentEventSystemコンポーネントが既に追加されているか確認
        PersistentEventSystem persistent = eventSystem.GetComponent<PersistentEventSystem>();

        if (persistent != null)
        {
            Debug.Log("[EventSystemSetup] PersistentEventSystem already attached.");
            EditorUtility.DisplayDialog(
                "Already Setup",
                "PersistentEventSystem is already attached to the EventSystem.",
                "OK"
            );
            return;
        }

        // Undo記録
        Undo.RecordObject(eventSystem.gameObject, "Add Persistent Event System");

        // PersistentEventSystemを追加
        persistent = eventSystem.gameObject.AddComponent<PersistentEventSystem>();

        // シーンを保存状態にマーク
        EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);

        Debug.Log("[EventSystemSetup] Successfully added PersistentEventSystem to EventSystem.");
        EditorUtility.DisplayDialog(
            "Setup Complete",
            "PersistentEventSystem has been added to the EventSystem.\n\nThe EventSystem will now persist across scene transitions.",
            "OK"
        );
    }

    [MenuItem("Tools/Baba/Setup Event System", true)]
    private static bool ValidateSetupEventSystem()
    {
        // Playモード中は実行不可
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
