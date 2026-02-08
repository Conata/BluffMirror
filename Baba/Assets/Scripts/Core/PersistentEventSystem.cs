using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystemをシーン間で永続化させる
/// StartシーンのEventSystemにアタッチして使用
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class PersistentEventSystem : MonoBehaviour
{
    private static PersistentEventSystem instance;

    private void Awake()
    {
        // 既に別のEventSystemが存在する場合は、このGameObjectを破棄
        if (instance != null && instance != this)
        {
            Debug.Log("[PersistentEventSystem] Another EventSystem already exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // 最初のインスタンスを保持してシーン間で永続化
        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PersistentEventSystem] EventSystem persisted across scenes.");
    }
}
