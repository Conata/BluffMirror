using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Phase 4 - Stage 4: カード選択確認UI
/// 画面中央下部に表示されるScreen Space確認ダイアログ
/// 「引く」「やめる」ボタンを提供
/// </summary>
public class ConfirmUI : MonoBehaviour
{
    public static ConfirmUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button drawButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text drawButtonText;
    [SerializeField] private TMP_Text cancelButtonText;
    [SerializeField] private TMP_Text promptText;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private CardObject currentCard;
    private UnityAction<CardObject> onDrawCallback;
    private UnityAction onCancelCallback;
    private bool isShowing = false;

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

        InitializeComponents();
        Hide(instant: true);
    }

    private void OnEnable()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDisable()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void InitializeComponents()
    {
        // Canvasが未設定の場合、自動取得/作成
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
        }

        // Screen Space - Overlay に設定
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 他のUIより前面に表示

        // CanvasScaler（Screen Space用）
        var canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster必須（ボタンクリック用）
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // ボタンリスナー設定
        if (drawButton != null)
        {
            drawButton.onClick.RemoveAllListeners();
            drawButton.onClick.AddListener(OnDrawButtonClicked);
        }
        else
        {
            Debug.LogError("[ConfirmUI] drawButton is NOT assigned! Run Tools > Baba > Setup ConfirmUI System");
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        else
        {
            Debug.LogError("[ConfirmUI] cancelButton is NOT assigned! Run Tools > Baba > Setup ConfirmUI System");
        }

        if (uiPanel == null)
            Debug.LogError("[ConfirmUI] uiPanel is NOT assigned! Run Tools > Baba > Setup ConfirmUI System");
        if (promptText == null)
            Debug.LogWarning("[ConfirmUI] promptText is not assigned");

        UpdateLanguage();
    }

    private void UpdateLanguage()
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return;

        if (drawButtonText != null) drawButtonText.text = loc.Get("confirm_ui.draw_button");
        if (cancelButtonText != null) cancelButtonText.text = loc.Get("confirm_ui.cancel_button");
        if (promptText != null) promptText.text = loc.Get("confirm_ui.prompt");
    }

    private void OnLanguageChanged(GameSettings.GameLanguage newLanguage)
    {
        UpdateLanguage();
    }

    /// <summary>
    /// 確認UIを表示（Screen Space中央下部）
    /// </summary>
    public void Show(CardObject card, UnityAction<CardObject> onDraw, UnityAction onCancel)
    {
        if (card == null)
        {
            Debug.LogWarning("[ConfirmUI] Card is null, cannot show UI");
            return;
        }

        currentCard = card;
        onDrawCallback = onDraw;
        onCancelCallback = onCancel;

        // フェードインアニメーション
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);

            CanvasGroup canvasGroup = uiPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = uiPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            uiPanel.transform.localScale = Vector3.one * 0.8f;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));
            sequence.Join(uiPanel.transform.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack));
        }
        else
        {
            Debug.LogError("[ConfirmUI] uiPanel is null! Run Tools > Baba > Setup ConfirmUI System");
        }

        isShowing = true;
        Debug.Log($"[ConfirmUI] Show UI for card (drawButton={drawButton != null}, cancelButton={cancelButton != null}, uiPanel={uiPanel != null})");
    }

    /// <summary>
    /// 確認UIを非表示
    /// </summary>
    public void Hide(bool instant = false)
    {
        if (!isShowing && !instant) return;

        if (uiPanel != null)
        {
            if (instant)
            {
                uiPanel.SetActive(false);
            }
            else
            {
                CanvasGroup canvasGroup = uiPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
                    sequence.Join(uiPanel.transform.DOScale(Vector3.one * 0.8f, hideDuration).SetEase(Ease.InBack));
                    sequence.OnComplete(() => uiPanel.SetActive(false));
                }
                else
                {
                    uiPanel.SetActive(false);
                }
            }
        }

        isShowing = false;
        currentCard = null;
        onDrawCallback = null;
        onCancelCallback = null;

        Debug.Log("[ConfirmUI] Hide UI");
    }

    private void OnDrawButtonClicked()
    {
        Debug.Log("[ConfirmUI] Draw button clicked");

        var callback = onDrawCallback;
        var card = currentCard;

        Hide();

        callback?.Invoke(card);
    }

    private void OnCancelButtonClicked()
    {
        Debug.Log("[ConfirmUI] Cancel button clicked");

        if (currentCard != null)
        {
            currentCard.ResetInteractionState();
        }

        var callback = onCancelCallback;

        Hide();

        callback?.Invoke();
    }
}
