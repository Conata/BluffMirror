using UnityEditor;
using UnityEngine;

/// <summary>
/// ResultUIのスタッツバーを強制的に非表示にするツール
/// </summary>
public class ResultUIStatsBarDisabler : EditorWindow
{
    [MenuItem("Tools/Baba/Disable ResultUI Stats Bars")]
    public static void DisableStatsBar()
    {
        // ResultUIを探す
        var resultUI = FindFirstObjectByType<ResultUI>();

        if (resultUI == null)
        {
            Debug.LogWarning("[ResultUIStatsBarDisabler] ResultUI not found in scene");
            EditorUtility.DisplayDialog("ResultUI Stats Bar", "ResultUI not found in scene", "OK");
            return;
        }

        // ResultCanvasの子オブジェクトからStatsContainerを探す
        Transform resultCanvasTransform = resultUI.transform.Find("ResultCanvas");
        if (resultCanvasTransform == null)
        {
            Debug.LogWarning("[ResultUIStatsBarDisabler] ResultCanvas not found");
            EditorUtility.DisplayDialog("ResultUI Stats Bar", "ResultCanvas not found", "OK");
            return;
        }

        // ResultCanvas > Background > ScrollView > Viewport > Content > StatsContainer
        Transform[] allChildren = resultCanvasTransform.GetComponentsInChildren<Transform>(true);
        bool foundStatsContainer = false;

        foreach (Transform child in allChildren)
        {
            if (child.name == "StatsContainer")
            {
                Undo.RecordObject(child.gameObject, "Disable Stats Container");
                child.gameObject.SetActive(false);
                EditorUtility.SetDirty(child.gameObject);
                foundStatsContainer = true;
                Debug.Log($"[ResultUIStatsBarDisabler] Disabled StatsContainer at path: {GetGameObjectPath(child.gameObject)}");
            }
        }

        if (foundStatsContainer)
        {
            EditorUtility.DisplayDialog(
                "ResultUI Stats Bar",
                "Stats Container has been disabled",
                "OK"
            );
        }
        else
        {
            Debug.LogWarning("[ResultUIStatsBarDisabler] StatsContainer not found in ResultCanvas hierarchy");
            EditorUtility.DisplayDialog(
                "ResultUI Stats Bar",
                "StatsContainer not found. It may be created at runtime.",
                "OK"
            );
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
