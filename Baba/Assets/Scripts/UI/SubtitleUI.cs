using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Phase 7-3: 画面下部固定字幕システム
/// ゲームイントロ・AI心理戦セリフ表示用
/// </summary>
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup subtitlePanel;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image backgroundPanel;

    [Header("Appearance Settings")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f); // 半透明黒
    [SerializeField] private float fontSize = 32f;
    [SerializeField] private TMP_FontAsset japaneseFont;

    [Header("Pressure Color Settings")]
    [SerializeField] private Color lowPressureColor = Color.white;
    [SerializeField] private Color mediumPressureColor = new Color(1f, 0.8f, 0.3f); // Orange
    [SerializeField] private Color highPressureColor = new Color(1f, 0.2f, 0.2f); // Red

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Typewriter Settings")]
    [SerializeField] private float charsPerSecond = 20f;

    [Header("Wobble Effect")]
    [SerializeField] private float wobbleIntensity = 2f;
    [SerializeField] private float wobbleSpeed = 12f;

    private Tween wobbleTween;
    private Tween typewriterTween;
    private Vector2 subtitleTextOriginalPos;
    private bool isWobbling = false;
    private bool isVisible = false;

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

        InitializeUI();
    }

    private void InitializeUI()
    {
        // 初期状態を非表示に
        if (subtitlePanel != null)
        {
            subtitlePanel.alpha = 0f;
            subtitlePanel.gameObject.SetActive(false);
        }

        // 背景色設定
        if (backgroundPanel != null)
        {
            backgroundPanel.color = backgroundColor;
        }

        // フォント設定
        if (subtitleText != null)
        {
            if (japaneseFont != null)
            {
                subtitleText.font = japaneseFont;
            }
            subtitleText.fontSize = fontSize;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.text = "";
        }

        Debug.Log("[SubtitleUI] Initialized");
    }

    /// <summary>
    /// 字幕を表示（新規テキスト）
    /// </summary>
    /// <param name="text">表示テキスト</param>
    /// <param name="pressureLevel">心理圧レベル（0.0-3.0）</param>
    public void Show(string text, float pressureLevel = 0f)
    {
        if (subtitlePanel == null || subtitleText == null)
        {
            Debug.LogWarning("[SubtitleUI] UI components not assigned");
            return;
        }

        // 既存のタイプライターアニメーションをキャンセル
        typewriterTween?.Kill();

        // テキスト設定（全文セットし、maxVisibleCharactersで制御）
        subtitleText.text = text;
        subtitleText.maxVisibleCharacters = 0;
        subtitleText.color = GetColorForPressure(pressureLevel);

        // パネルをアクティブ化
        if (!subtitlePanel.gameObject.activeSelf)
        {
            subtitlePanel.gameObject.SetActive(true);
        }

        // フェードイン
        subtitlePanel.DOKill();
        subtitlePanel.DOFade(1f, fadeInDuration);

        // タイプライター演出
        int totalChars = text.Length;
        float duration = totalChars / charsPerSecond;
        int current = 0;
        typewriterTween = DOTween.To(() => current, x =>
        {
            current = x;
            subtitleText.maxVisibleCharacters = current;
        }, totalChars, duration).SetEase(Ease.Linear);

        isVisible = true;

        Debug.Log($"[SubtitleUI] Show: \"{text}\" (Pressure: {pressureLevel})");
    }

    /// <summary>
    /// 字幕の内容を更新（既に表示中の場合）
    /// </summary>
    /// <param name="text">新しいテキスト</param>
    /// <param name="pressureLevel">心理圧レベル（0.0-3.0）</param>
    public void UpdateText(string text, float pressureLevel = 0f)
    {
        if (subtitleText == null)
        {
            Debug.LogWarning("[SubtitleUI] SubtitleText not assigned");
            return;
        }

        if (!isVisible)
        {
            // 非表示の場合は通常のShow
            Show(text, pressureLevel);
            return;
        }

        // 既存のタイプライターアニメーションをキャンセル
        typewriterTween?.Kill();

        // テキストと色を更新（タイプライター演出付き）
        subtitleText.text = text;
        subtitleText.maxVisibleCharacters = 0;
        Color targetColor = GetColorForPressure(pressureLevel);
        subtitleText.DOColor(targetColor, fadeInDuration * 0.5f);

        int totalChars = text.Length;
        float duration = totalChars / charsPerSecond;
        int current = 0;
        typewriterTween = DOTween.To(() => current, x =>
        {
            current = x;
            subtitleText.maxVisibleCharacters = current;
        }, totalChars, duration).SetEase(Ease.Linear);

        Debug.Log($"[SubtitleUI] UpdateText: \"{text}\" (Pressure: {pressureLevel})");
    }

    /// <summary>
    /// 字幕を非表示
    /// </summary>
    public void Hide()
    {
        if (subtitlePanel == null) return;

        if (!isVisible) return; // 既に非表示

        StopWobble();
        typewriterTween?.Kill();
        typewriterTween = null;

        // フェードアウト
        subtitlePanel.DOKill();
        subtitlePanel.DOFade(0f, fadeOutDuration).OnComplete(() =>
        {
            subtitlePanel.gameObject.SetActive(false);
            if (subtitleText != null)
            {
                subtitleText.text = "";
            }
        });

        isVisible = false;

        Debug.Log("[SubtitleUI] Hide");
    }

    /// <summary>
    /// 圧力レベルに応じた色を取得
    /// </summary>
    private Color GetColorForPressure(float pressureLevel)
    {
        if (pressureLevel < 1.0f)
        {
            // Low pressure
            return lowPressureColor;
        }
        else if (pressureLevel < 2.0f)
        {
            // Medium pressure
            float t = (pressureLevel - 1.0f) / 1.0f; // 1.0-2.0 を 0-1 に正規化
            return Color.Lerp(lowPressureColor, mediumPressureColor, t);
        }
        else
        {
            // High pressure
            float t = Mathf.Clamp01((pressureLevel - 2.0f) / 1.0f); // 2.0-3.0 を 0-1 に正規化
            return Color.Lerp(mediumPressureColor, highPressureColor, t);
        }
    }

    /// <summary>
    /// テキスト微振動を開始（Projection/Distortion tier用）
    /// </summary>
    /// <param name="intensity">振動の強さ (0-1)</param>
    public void StartWobble(float intensity)
    {
        if (subtitleText == null || isWobbling) return;

        isWobbling = true;
        subtitleTextOriginalPos = subtitleText.rectTransform.anchoredPosition;
        float actualIntensity = wobbleIntensity * Mathf.Clamp01(intensity);

        wobbleTween = subtitleText.rectTransform
            .DOShakeAnchorPos(999f, actualIntensity, (int)wobbleSpeed, 90f, false, true)
            .SetLoops(-1, LoopType.Restart);
    }

    /// <summary>
    /// テキスト微振動を停止
    /// </summary>
    public void StopWobble()
    {
        if (!isWobbling) return;

        wobbleTween?.Kill();
        wobbleTween = null;

        if (subtitleText != null)
        {
            subtitleText.rectTransform.anchoredPosition = subtitleTextOriginalPos;
        }

        isWobbling = false;
    }

    /// <summary>
    /// 字幕が表示中かどうか
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Show Subtitle (Low Pressure)")]
    private void TestShowLowPressure()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleUI] Test must be run in Play Mode");
            return;
        }

        Show("どれにしようか...", 0.5f);
    }

    [ContextMenu("Test: Show Subtitle (Medium Pressure)")]
    private void TestShowMediumPressure()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleUI] Test must be run in Play Mode");
            return;
        }

        Show("君の目を見ていると... 緊張してるね", 1.5f);
    }

    [ContextMenu("Test: Show Subtitle (High Pressure)")]
    private void TestShowHighPressure()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleUI] Test must be run in Play Mode");
            return;
        }

        Show("さあ、君の心の内を暴いてあげよう", 2.8f);
    }

    [ContextMenu("Test: Update Subtitle")]
    private void TestUpdateSubtitle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleUI] Test must be run in Play Mode");
            return;
        }

        UpdateText("これで行こう", 1.0f);
    }

    [ContextMenu("Test: Hide Subtitle")]
    private void TestHideSubtitle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SubtitleUI] Test must be run in Play Mode");
            return;
        }

        Hide();
    }
#endif
}
