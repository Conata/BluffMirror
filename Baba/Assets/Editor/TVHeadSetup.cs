using UnityEditor;
using UnityEngine;
using Live2D.Cubism.Core;

/// <summary>
/// TVHead Live2Dモデルにアニメーションコンポーネントをセットアップするエディタツール。
/// </summary>
public static class TVHeadSetup
{
    [MenuItem("Tools/Baba/Setup TVHead Animator")]
    private static void Setup()
    {
        // シーン内のTVHeadモデルを検索
        var model = Object.FindFirstObjectByType<CubismModel>();
        if (model == null)
        {
            EditorUtility.DisplayDialog("TVHead Setup", "シーンにCubismModelが見つかりません。\nTVHead prefabをシーンに配置してください。", "OK");
            return;
        }

        Undo.RecordObject(model.gameObject, "Setup TVHead Animator");

        // TVHeadAnimatorを追加（既にあればスキップ）
        var animator = model.GetComponent<TVHeadAnimator>();
        if (animator == null)
        {
            animator = Undo.AddComponent<TVHeadAnimator>(model.gameObject);
        }

        EditorUtility.SetDirty(model.gameObject);

        Debug.Log($"TVHead Setup: TVHeadAnimator を {model.gameObject.name} に追加しました。");
        EditorUtility.DisplayDialog("TVHead Setup", $"セットアップ完了！\n{model.gameObject.name} に TVHeadAnimator を追加しました。\n\nPlay モードで動作を確認してください。", "OK");
    }
}
