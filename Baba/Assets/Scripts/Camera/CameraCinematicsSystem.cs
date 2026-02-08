using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Virtual Camera切り替え管理
/// GameManagerの状態遷移に応じてカメラブレンド
/// </summary>
public class CameraCinematicsSystem : MonoBehaviour
{
    public static CameraCinematicsSystem Instance { get; private set; }

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera vcamPlayerTurn;    // AIの手札を見下ろす
    [SerializeField] private CinemachineCamera vcamAITurn;        // プレイヤーの手札を見下ろす
    [SerializeField] private CinemachineCamera vcamCardFocus;     // 選択されたカードにズームイン
    [SerializeField] private CinemachineCamera vcamAIReaction;    // AIの顔/反応にフォーカス
    [SerializeField] private CinemachineCamera vcamTableOverview; // テーブル全景俯瞰（イントロ・リザルト用）

    [Header("Priority Settings")]
    [SerializeField] private int defaultPriority = 10;
    [SerializeField] private int activePriority = 15;

    [Header("References")]
    [SerializeField] private Transform aiHandTransform;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private Transform aiFaceTransform; // AI顔がない場合はAIHand使用

    private CinemachineCamera currentActiveCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初期化：全カメラを非アクティブ化
        DeactivateAllCameras();
    }

    private void Start()
    {
        // VCamのFollow/LookAtターゲットをコードから正しく設定
        SetupCameraTargets();

        // ゲーム開始時はプレイヤーターンカメラをアクティブにする
        ActivateCamera(vcamPlayerTurn);

        if (vcamTableOverview == null)
            Debug.LogWarning("[CameraCinematicsSystem] vcamTableOverview is not assigned in Inspector. Table overview shots will be skipped.");
    }

    /// <summary>
    /// 各VCamのFollow/LookAtターゲットと回転パイプラインを設定
    /// PlayerTurn/AITurnはFPS着席視点（固定回転）のためLookAt/HardLookAt不要
    /// CardFocus/AIReactionは動的LookAtを維持
    /// </summary>
    private void SetupCameraTargets()
    {
        // PlayerTurn: FPS着席視点（固定回転 - Inspector値で制御）
        if (vcamPlayerTurn != null)
        {
            vcamPlayerTurn.Follow = null;
            vcamPlayerTurn.LookAt = null;
            RemoveHardLookAt(vcamPlayerTurn);
        }

        // AITurn: FPS着席視点（固定回転 - Inspector値で制御）
        if (vcamAITurn != null)
        {
            vcamAITurn.Follow = null;
            vcamAITurn.LookAt = null;
            RemoveHardLookAt(vcamAITurn);
        }

        // CardFocus: FocusOnCard()で動的に設定
        if (vcamCardFocus != null)
        {
            vcamCardFocus.Follow = null;
            vcamCardFocus.LookAt = null;
            EnsureHardLookAt(vcamCardFocus);
        }

        // AIReaction: AIの顔にフォーカス
        if (vcamAIReaction != null)
        {
            vcamAIReaction.Follow = null;
            vcamAIReaction.LookAt = aiFaceTransform;
            EnsureHardLookAt(vcamAIReaction);
        }

        // TableOverview: テーブル全景俯瞰（固定回転 - Inspector値で制御）
        if (vcamTableOverview != null)
        {
            vcamTableOverview.Follow = null;
            vcamTableOverview.LookAt = null;
            RemoveHardLookAt(vcamTableOverview);
        }
    }

    /// <summary>
    /// VCamにCinemachineHardLookAtが無ければ追加する
    /// これが無いとLookAtターゲットを設定しても回転が反映されない
    /// </summary>
    private void EnsureHardLookAt(CinemachineCamera vcam)
    {
        if (vcam.GetComponent<CinemachineHardLookAt>() == null)
        {
            vcam.gameObject.AddComponent<CinemachineHardLookAt>();
            Debug.Log($"[CameraCinematicsSystem] Added CinemachineHardLookAt to {vcam.name}");
        }
    }

    /// <summary>
    /// VCamからCinemachineHardLookAtを除去する
    /// FPS着席視点では固定回転を使用するためHardLookAtは不要
    /// </summary>
    private void RemoveHardLookAt(CinemachineCamera vcam)
    {
        var hardLookAt = vcam.GetComponent<CinemachineHardLookAt>();
        if (hardLookAt != null)
        {
            Destroy(hardLookAt);
            Debug.Log($"[CameraCinematicsSystem] Removed CinemachineHardLookAt from {vcam.name}");
        }
    }

    /// <summary>
    /// プレイヤーターン開始時：AIの手札を見下ろす
    /// </summary>
    public void ShowPlayerTurnView()
    {
        Debug.Log("[CameraCinematicsSystem] Switching to Player Turn view (looking at AI hand)");
        ActivateCamera(vcamPlayerTurn);
    }

    /// <summary>
    /// AIターン開始時：プレイヤーの手札を見下ろす
    /// </summary>
    public void ShowAITurnView()
    {
        Debug.Log("[CameraCinematicsSystem] Switching to AI Turn view (looking at Player hand)");
        ActivateCamera(vcamAITurn);
    }

    /// <summary>
    /// カード選択時：選択されたカードにズームイン
    /// </summary>
    /// <param name="cardTransform">選択されたカードのTransform</param>
    public void FocusOnCard(Transform cardTransform)
    {
        if (vcamCardFocus == null || cardTransform == null)
        {
            Debug.LogWarning("[CameraCinematicsSystem] Cannot focus on card - camera or card transform is null");
            return;
        }

        Debug.Log($"[CameraCinematicsSystem] Focusing on card: {cardTransform.name}");

        // CardFocusカメラのLookAtを選択されたカードに設定
        // Follow は設定しない（Bodyコンポーネントがないためカメラ位置が不安定になる）
        vcamCardFocus.LookAt = cardTransform;

        ActivateCamera(vcamCardFocus);
    }

    /// <summary>
    /// AI反応時：AIの顔/反応にフォーカス
    /// </summary>
    public void ShowAIReactionView()
    {
        Debug.Log("[CameraCinematicsSystem] Switching to AI Reaction view");
        ActivateCamera(vcamAIReaction);
    }

    /// <summary>
    /// テーブル全景俯瞰：イントロAct3、ゲーム終了演出で使用
    /// </summary>
    public void ShowTableOverview()
    {
        Debug.Log("[CameraCinematicsSystem] Switching to Table Overview");
        ActivateCamera(vcamTableOverview);
    }

    /// <summary>
    /// Stage 6: カードシーケンシャルフォーカス（複数カードを順番に見る）
    /// </summary>
    /// <param name="cardTransforms">フォーカスするカードのリスト</param>
    /// <param name="focusDuration">各カードのフォーカス時間（秒）</param>
    /// <param name="blendTime">次のカードへの遷移時間（秒）</param>
    /// <param name="onCardFocused">各カードフォーカス時のコールバック（カードインデックス）</param>
    /// <returns></returns>
    public IEnumerator FocusOnCardSequence(
        System.Collections.Generic.List<Transform> cardTransforms,
        float focusDuration,
        float blendTime,
        System.Action<int> onCardFocused = null)
    {
        if (cardTransforms == null || cardTransforms.Count == 0)
        {
            Debug.LogWarning("[CameraCinematicsSystem] Cannot focus on card sequence - list is null or empty");
            yield break;
        }

        Debug.Log($"[CameraCinematicsSystem] Starting card focus sequence for {cardTransforms.Count} cards");

        for (int i = 0; i < cardTransforms.Count; i++)
        {
            Transform cardTransform = cardTransforms[i];

            if (cardTransform == null)
            {
                Debug.LogWarning($"[CameraCinematicsSystem] Card transform at index {i} is null, skipping");
                continue;
            }

            // カードにフォーカス
            FocusOnCard(cardTransform);

            // コールバック実行
            onCardFocused?.Invoke(i);

            // フォーカス時間待機
            yield return new WaitForSeconds(focusDuration);

            // 最後のカード以外の場合、次のカードへのブレンド時間を待つ
            if (i < cardTransforms.Count - 1)
            {
                yield return new WaitForSeconds(blendTime);
            }
        }

        Debug.Log("[CameraCinematicsSystem] Card focus sequence completed");
    }

    /// <summary>
    /// 指定したカメラをアクティブ化（他のカメラを非アクティブ化）
    /// </summary>
    private void ActivateCamera(CinemachineCamera camera)
    {
        if (camera == null)
        {
            Debug.LogWarning("[CameraCinematicsSystem] Cannot activate null camera");
            return;
        }

        // 全カメラを非アクティブ化
        DeactivateAllCameras();

        // 指定されたカメラをアクティブ化
        camera.Priority.Value = activePriority;
        currentActiveCamera = camera;
    }

    /// <summary>
    /// 全カメラを非アクティブ化
    /// </summary>
    private void DeactivateAllCameras()
    {
        if (vcamPlayerTurn != null) vcamPlayerTurn.Priority.Value = defaultPriority;
        if (vcamAITurn != null) vcamAITurn.Priority.Value = defaultPriority;
        if (vcamCardFocus != null) vcamCardFocus.Priority.Value = defaultPriority;
        if (vcamAIReaction != null) vcamAIReaction.Priority.Value = defaultPriority;
        if (vcamTableOverview != null) vcamTableOverview.Priority.Value = defaultPriority;
    }

    /// <summary>
    /// 現在のアクティブカメラを取得
    /// </summary>
    public CinemachineCamera GetActiveCamera()
    {
        return currentActiveCamera;
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Player Turn View")]
    private void TestPlayerTurnView()
    {
        ShowPlayerTurnView();
    }

    [ContextMenu("Test: AI Turn View")]
    private void TestAITurnView()
    {
        ShowAITurnView();
    }

    [ContextMenu("Test: AI Reaction View")]
    private void TestAIReactionView()
    {
        ShowAIReactionView();
    }

    [ContextMenu("Test: Table Overview")]
    private void TestTableOverview()
    {
        ShowTableOverview();
    }

    [ContextMenu("Test: Card Focus Sequence")]
    private void TestCardFocusSequence()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CameraCinematicsSystem] Test must be run in Play Mode");
            return;
        }

        // シーン内のカードを取得
        var cards = FindObjectsByType<CardObject>(FindObjectsSortMode.None);
        if (cards.Length == 0)
        {
            Debug.LogWarning("[CameraCinematicsSystem] No CardObjects found in scene");
            return;
        }

        // 最大3枚まで取得
        var cardList = new System.Collections.Generic.List<Transform>();
        int maxCards = Mathf.Min(3, cards.Length);
        for (int i = 0; i < maxCards; i++)
        {
            cardList.Add(cards[i].transform);
        }

        // シーケンス開始
        StartCoroutine(FocusOnCardSequence(
            cardList,
            0.8f, // focusDuration
            0.3f, // blendTime
            (cardIndex) => Debug.Log($"[CameraCinematicsSystem] Focused on card {cardIndex}")
        ));
    }
#endif
}
