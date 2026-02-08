using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// Stage 6: AIの注目先を示すWorldSpace UIマーカー
/// カード上に表示されるシアンリング/カーソル
/// スムーズ移動 + 微動（人間らしい手の揺れ）対応
/// Stage 13: CoTメンタリスト視覚ステート (Scanning/Focusing/Locked)
/// </summary>
public class AIAttentionMarker : MonoBehaviour
{
    public static AIAttentionMarker Instance { get; private set; }

    /// <summary>
    /// CoTメンタリスト視覚ステート
    /// </summary>
    public enum MarkerVisualState
    {
        Scanning,   // 通常シアン + 速いパルス + 通常ドリフト + 回転
        Focusing,   // 明るいシアン + 遅いパルス + ドリフト半減 + 遅い回転
        Locked      // 白シアン + パルスなし + ドリフトなし + 回転なし + 拡大
    }

    [Header("Canvas Settings")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RenderMode renderMode = RenderMode.WorldSpace;

    [Header("Marker Visual")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Image markerImage;
    [SerializeField] private TextMeshProUGUI markerText;
    [SerializeField] private Color markerColor = new Color(0.3f, 0.9f, 1f, 0.8f); // Cyan

    [Header("Position Settings")]
    [SerializeField] private Vector3 offsetFromCard = new Vector3(0, 0.2f, 0);
    [SerializeField] private float canvasScale = 0.01f;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.2f;

    [Header("Smooth Movement")]
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private float driftRadius = 0.008f;
    [SerializeField] private float driftSpeed = 1.5f;

    [Header("CoT Visual States")]
    [SerializeField] private Color scanningColor = new Color(0.3f, 0.9f, 1f, 0.6f);
    [SerializeField] private Color focusingColor = new Color(0.4f, 1f, 1f, 0.85f);
    [SerializeField] private Color lockedColor = new Color(0.6f, 1f, 1f, 1.0f);
    [SerializeField] private float scanningPulseSpeed = 3f;
    [SerializeField] private float focusingPulseSpeed = 1.5f;
    [SerializeField] private float lockedScale = 1.3f;

    private Transform currentTarget;
    private bool isShowing;
    private bool isMoving;
    private Tweener pulseTweener;
    private Tweener moveTweener;
    private float driftTimer;
    private MarkerVisualState currentVisualState = MarkerVisualState.Scanning;

    // デフォルト値の保持（リセット用）
    private float defaultRotationSpeed;
    private float defaultDriftRadius;
    private float defaultPulseSpeed;

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

        defaultRotationSpeed = rotationSpeed;
        defaultDriftRadius = driftRadius;
        defaultPulseSpeed = pulseSpeed;

        InitializeCanvas();
        Hide(instant: true);
    }

    private void InitializeCanvas()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
        }

        canvas.renderMode = renderMode;
        canvas.worldCamera = Camera.main;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null && renderMode != RenderMode.WorldSpace)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        if (markerPrefab == null && markerImage == null)
        {
            CreateMarkerProcedurally();
        }
        else if (markerPrefab != null && markerImage == null)
        {
            GameObject markerObj = Instantiate(markerPrefab, transform);
            markerImage = markerObj.GetComponent<Image>();
        }

        if (markerImage != null)
        {
            markerImage.color = markerColor;
        }
    }

    private void CreateMarkerProcedurally()
    {
        GameObject markerObj = new GameObject("Marker");
        markerObj.transform.SetParent(transform);
        markerObj.transform.localPosition = Vector3.zero;
        markerObj.transform.localScale = Vector3.one;

        markerImage = markerObj.AddComponent<Image>();
        markerImage.color = markerColor;
        markerImage.raycastTarget = false;

        markerImage.sprite = Resources.Load<Sprite>("UI/Skin/Knob");
        if (markerImage.sprite == null)
        {
            Debug.LogWarning("[AIAttentionMarker] Default sprite not found, using solid color");
        }

        RectTransform rect = markerImage.rectTransform;
        rect.sizeDelta = new Vector2(8f, 8f);

#if UNITY_EDITOR
        Debug.Log("[AIAttentionMarker] Procedurally created marker");
#endif
    }

    /// <summary>
    /// マーカーを表示（初回表示 or 即座に表示）
    /// </summary>
    public void Show(Transform cardTransform)
    {
        if (cardTransform == null)
        {
            Debug.LogWarning("[AIAttentionMarker] Cannot show marker - card transform is null");
            return;
        }

        // 既に表示中なら MoveTo で移動
        if (isShowing && currentTarget != null)
        {
            MoveTo(cardTransform);
            return;
        }

        currentTarget = cardTransform;
        isShowing = true;
        isMoving = false;
        driftTimer = 0f;

        // 即座に位置を設定
        SetPositionImmediate();

        canvas.enabled = true;

        if (markerImage != null)
        {
            Color targetColor = markerColor;
            markerImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);
            markerImage.DOFade(targetColor.a, fadeInDuration);
        }

        if (markerText != null)
        {
            markerText.DOFade(1f, fadeInDuration);
        }

        StartPulseAnimation();
    }

    /// <summary>
    /// 別のカードにスムーズ移動
    /// </summary>
    public void MoveTo(Transform newTarget, float duration = -1f)
    {
        if (newTarget == null) return;

        currentTarget = newTarget;
        float dur = duration > 0 ? duration : moveDuration;

        // 既存の移動Tweenを停止
        moveTweener?.Kill();
        isMoving = true;

        Vector3 targetPos = currentTarget.position + offsetFromCard;

        moveTweener = transform.DOMove(targetPos, dur)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => isMoving = false);
    }

    /// <summary>
    /// マーカーを非表示
    /// </summary>
    public void Hide(bool instant = false)
    {
        isShowing = false;
        currentTarget = null;
        isMoving = false;

        moveTweener?.Kill();
        moveTweener = null;

        StopPulseAnimation();
        ResetVisualState();

        if (instant)
        {
            canvas.enabled = false;
            if (markerImage != null)
            {
                markerImage.color = new Color(markerColor.r, markerColor.g, markerColor.b, 0f);
            }
            if (markerText != null)
            {
                markerText.color = new Color(markerText.color.r, markerText.color.g, markerText.color.b, 0f);
            }
        }
        else
        {
            if (markerImage != null)
            {
                markerImage.DOFade(0f, fadeOutDuration).OnComplete(() => canvas.enabled = false);
            }
            else
            {
                canvas.enabled = false;
            }

            if (markerText != null)
            {
                markerText.DOFade(0f, fadeOutDuration);
            }
        }
    }

    private void StartPulseAnimation()
    {
        StopPulseAnimation();

        if (markerImage != null)
        {
            Transform markerTransform = markerImage.transform;
            markerTransform.localScale = Vector3.one;

            pulseTweener = markerTransform.DOScale(Vector3.one * pulseScale, 1f / pulseSpeed)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StopPulseAnimation()
    {
        if (pulseTweener != null)
        {
            pulseTweener.Kill();
            pulseTweener = null;
        }

        if (markerImage != null)
        {
            markerImage.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// CoTメンタリスト視覚ステートを設定
    /// Scanning → Focusing → Locked と段階的に確信度を表現
    /// </summary>
    public void SetVisualState(MarkerVisualState state)
    {
        if (currentVisualState == state) return;
        currentVisualState = state;

        StopPulseAnimation();

        switch (state)
        {
            case MarkerVisualState.Scanning:
                if (markerImage != null)
                    markerImage.DOColor(scanningColor, 0.3f);
                pulseSpeed = scanningPulseSpeed;
                rotationSpeed = defaultRotationSpeed;
                driftRadius = defaultDriftRadius;
                StartPulseAnimation();
                break;

            case MarkerVisualState.Focusing:
                if (markerImage != null)
                {
                    markerImage.DOColor(focusingColor, 0.3f);
                    markerImage.transform.DOScale(Vector3.one * 1.1f, 0.3f);
                }
                pulseSpeed = focusingPulseSpeed;
                rotationSpeed = 20f;
                driftRadius = defaultDriftRadius * 0.5f;
                StartPulseAnimation();
                break;

            case MarkerVisualState.Locked:
                if (markerImage != null)
                {
                    markerImage.DOColor(lockedColor, 0.2f);
                    markerImage.transform.DOScale(Vector3.one * lockedScale, 0.3f)
                        .SetEase(Ease.OutBack);
                }
                rotationSpeed = 0f;
                driftRadius = 0f;
                break;
        }
    }

    /// <summary>
    /// 視覚ステートをデフォルト（Scanning）にリセット
    /// </summary>
    private void ResetVisualState()
    {
        currentVisualState = MarkerVisualState.Scanning;
        rotationSpeed = defaultRotationSpeed;
        driftRadius = defaultDriftRadius;
        pulseSpeed = defaultPulseSpeed;
    }

    private void LateUpdate()
    {
        if (isShowing && currentTarget != null)
        {
            if (!isMoving)
            {
                // 微動（人間の手の揺れを再現）
                ApplyDrift();
            }
            UpdateRotation();
        }
    }

    /// <summary>
    /// 即座に位置を設定
    /// </summary>
    private void SetPositionImmediate()
    {
        if (currentTarget == null) return;

        Vector3 targetPosition = currentTarget.position + offsetFromCard;
        transform.position = targetPosition;
        transform.localScale = Vector3.one * canvasScale;
    }

    /// <summary>
    /// 微動: カード上で小さくランダムに揺れる（人間の手が完全に静止しないように）
    /// </summary>
    private void ApplyDrift()
    {
        if (currentTarget == null) return;

        driftTimer += Time.deltaTime * driftSpeed;

        // Perlin noiseで自然な揺れを生成
        float driftX = (Mathf.PerlinNoise(driftTimer, 0f) - 0.5f) * 2f * driftRadius;
        float driftY = (Mathf.PerlinNoise(0f, driftTimer + 100f) - 0.5f) * 2f * driftRadius;

        Vector3 basePosition = currentTarget.position + offsetFromCard;
        transform.position = basePosition + new Vector3(driftX, driftY, 0f);
        transform.localScale = Vector3.one * canvasScale;
    }

    private void UpdateRotation()
    {
        if (Camera.main == null) return;

        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        if (markerImage != null && rotationSpeed > 0f)
        {
            markerImage.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Show Marker")]
    private void TestShowMarker()
    {
        var card = FindFirstObjectByType<CardObject>();
        if (card != null)
        {
            Show(card.transform);
        }
        else
        {
            Debug.LogWarning("[AIAttentionMarker] No CardObject found in scene for testing.");
        }
    }

    [ContextMenu("Test: Hide Marker")]
    private void TestHideMarker()
    {
        Hide();
    }
#endif
}
