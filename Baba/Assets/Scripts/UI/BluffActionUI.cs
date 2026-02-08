using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// ブラフアクションボタンパネル
/// 画面右端に縦並び4ボタン: Shuffle / Push⇔Pull / Wiggle / Spread⇔Close
/// </summary>
public class BluffActionUI : MonoBehaviour
{
    public static BluffActionUI Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private Button pushPullButton;
    [SerializeField] private Button wiggleButton;
    [SerializeField] private Button spreadCloseButton;
    [SerializeField] private Button cancelButton;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text shuffleLabel;
    [SerializeField] private TMP_Text pushPullLabel;
    [SerializeField] private TMP_Text wiggleLabel;
    [SerializeField] private TMP_Text spreadCloseLabel;

    [Header("Selection Mode")]
    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private TMP_Text selectionPromptText;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    private bool isShowing = false;
    private bool isInSelectionMode = false;
    private bool isPushMode = true;
    private bool isSpreadMode = true;
    private CanvasGroup panelCanvasGroup;

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

    private void InitializeComponents()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        // RectTransform scale修正 (0,0,0だとCanvasScalerが動作しても不可視)
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
        {
            rectTransform.localScale = Vector3.one;
        }

        var canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // ボタンパネルが未設定の場合、ランタイムで自動生成
        if (buttonPanel == null)
        {
            CreateDefaultUI();
        }

        if (buttonPanel != null)
        {
            panelCanvasGroup = buttonPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = buttonPanel.AddComponent<CanvasGroup>();
        }

        // Button listeners
        if (shuffleButton != null)
        {
            shuffleButton.onClick.RemoveAllListeners();
            shuffleButton.onClick.AddListener(OnShuffleClicked);
        }
        if (pushPullButton != null)
        {
            pushPullButton.onClick.RemoveAllListeners();
            pushPullButton.onClick.AddListener(OnPushPullClicked);
        }
        if (wiggleButton != null)
        {
            wiggleButton.onClick.RemoveAllListeners();
            wiggleButton.onClick.AddListener(OnWiggleClicked);
        }
        if (spreadCloseButton != null)
        {
            spreadCloseButton.onClick.RemoveAllListeners();
            spreadCloseButton.onClick.AddListener(OnSpreadCloseClicked);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }

        UpdateLabels();
    }

    private void Update()
    {
        if (!isShowing || isInSelectionMode) return;

        // クールダウン状態をボタンに反映
        bool canAct = BluffActionSystem.Instance != null && BluffActionSystem.Instance.CanPlayerAct();
        SetButtonsInteractable(canAct);
    }

    // =========================================
    // Show / Hide
    // =========================================

    public void Show()
    {
        if (isShowing) return;

        if (buttonPanel != null)
        {
            buttonPanel.SetActive(true);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
                panelCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
            }
        }

        if (selectionOverlay != null) selectionOverlay.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        isShowing = true;
    }

    public void Hide(bool instant = false)
    {
        if (!isShowing && !instant) return;

        // カード選択モード中なら先にキャンセル
        if (isInSelectionMode)
        {
            ExitCardSelectionMode();
            if (BluffActionSystem.Instance != null)
                BluffActionSystem.Instance.CancelCardSelection();
        }

        if (buttonPanel != null)
        {
            if (instant)
            {
                buttonPanel.SetActive(false);
            }
            else if (panelCanvasGroup != null)
            {
                panelCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
                    .OnComplete(() => buttonPanel.SetActive(false));
            }
            else
            {
                buttonPanel.SetActive(false);
            }
        }

        isShowing = false;
    }

    // =========================================
    // Button Handlers
    // =========================================

    private void OnShuffleClicked()
    {
        if (BluffActionSystem.Instance == null) return;
        BluffActionSystem.Instance.RequestPlayerShuffle();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardHover();
    }

    private void OnPushPullClicked()
    {
        if (BluffActionSystem.Instance == null) return;

        if (isPushMode)
            BluffActionSystem.Instance.RequestPlayerTargetedAction(BluffActionType.Push);
        else
            BluffActionSystem.Instance.RequestPlayerTargetedAction(BluffActionType.Pull);

        // トグル
        isPushMode = !isPushMode;
        UpdateLabels();
    }

    private void OnWiggleClicked()
    {
        if (BluffActionSystem.Instance == null) return;
        BluffActionSystem.Instance.RequestPlayerTargetedAction(BluffActionType.Wiggle);
    }

    private void OnSpreadCloseClicked()
    {
        if (BluffActionSystem.Instance == null) return;

        if (isSpreadMode)
            BluffActionSystem.Instance.RequestPlayerSpread();
        else
            BluffActionSystem.Instance.RequestPlayerClose();

        // トグル
        isSpreadMode = !isSpreadMode;
        UpdateLabels();
    }

    private void OnCancelClicked()
    {
        if (BluffActionSystem.Instance != null)
            BluffActionSystem.Instance.CancelCardSelection();
    }

    // =========================================
    // Card Selection Mode
    // =========================================

    public void EnterCardSelectionMode(string actionName)
    {
        isInSelectionMode = true;

        if (selectionOverlay != null) selectionOverlay.SetActive(true);
        if (selectionPromptText != null)
        {
            var loc = LocalizationManager.Instance;
            string prompt = loc != null
                ? loc.Get("bluff_action.select_card_prompt")
                : "Select a card";
            selectionPromptText.text = $"{prompt}: {actionName}";
        }
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);

        // アクションボタンを無効化
        SetActionButtonsInteractable(false);
    }

    public void ExitCardSelectionMode()
    {
        isInSelectionMode = false;

        if (selectionOverlay != null) selectionOverlay.SetActive(false);
        if (cancelButton != null) cancelButton.gameObject.SetActive(false);

        SetActionButtonsInteractable(true);
    }

    // =========================================
    // Utility
    // =========================================

    private void UpdateLabels()
    {
        var loc = LocalizationManager.Instance;

        if (shuffleLabel != null)
            shuffleLabel.text = loc != null ? loc.Get("bluff_action.shuffle") : "Shuffle";

        if (pushPullLabel != null)
        {
            if (isPushMode)
                pushPullLabel.text = loc != null ? loc.Get("bluff_action.push") : "Push";
            else
                pushPullLabel.text = loc != null ? loc.Get("bluff_action.pull") : "Pull";
        }

        if (wiggleLabel != null)
            wiggleLabel.text = loc != null ? loc.Get("bluff_action.wiggle") : "Wiggle";

        if (spreadCloseLabel != null)
        {
            if (isSpreadMode)
                spreadCloseLabel.text = loc != null ? loc.Get("bluff_action.spread") : "Spread";
            else
                spreadCloseLabel.text = loc != null ? loc.Get("bluff_action.close") : "Close";
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (shuffleButton != null) shuffleButton.interactable = interactable;
        if (pushPullButton != null) pushPullButton.interactable = interactable;
        if (wiggleButton != null) wiggleButton.interactable = interactable;
        if (spreadCloseButton != null) spreadCloseButton.interactable = interactable;
    }

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (shuffleButton != null) shuffleButton.interactable = interactable;
        if (pushPullButton != null) pushPullButton.interactable = interactable;
        if (wiggleButton != null) wiggleButton.interactable = interactable;
        if (spreadCloseButton != null) spreadCloseButton.interactable = interactable;
    }

    // =========================================
    // Runtime UI Auto-Creation
    // =========================================

    private void CreateDefaultUI()
    {
        // ButtonPanel — 画面右端、縦並び
        buttonPanel = new GameObject("ButtonPanel");
        buttonPanel.transform.SetParent(transform, false);
        RectTransform panelRect = buttonPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.92f, 0.3f);
        panelRect.anchorMax = new Vector2(0.99f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelCanvasGroup = buttonPanel.AddComponent<CanvasGroup>();

        Image panelBg = buttonPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.7f);

        var layout = buttonPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(6, 6, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // 4 Action Buttons + Cancel
        Color blueGray = new Color(0.2f, 0.25f, 0.35f);
        (shuffleButton, shuffleLabel) = CreateRuntimeButton("ShuffleButton", "Shuffle", blueGray);
        (pushPullButton, pushPullLabel) = CreateRuntimeButton("PushPullButton", "Push", blueGray);
        (wiggleButton, wiggleLabel) = CreateRuntimeButton("WiggleButton", "Wiggle", blueGray);
        (spreadCloseButton, spreadCloseLabel) = CreateRuntimeButton("SpreadCloseButton", "Spread", blueGray);
        (cancelButton, _) = CreateRuntimeButton("CancelButton", "Cancel", new Color(0.5f, 0.15f, 0.15f));
        cancelButton.gameObject.SetActive(false);

        // SelectionOverlay — 画面下部のカード選択プロンプト
        selectionOverlay = new GameObject("SelectionOverlay");
        selectionOverlay.transform.SetParent(transform, false);
        RectTransform overlayRect = selectionOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.3f, 0.02f);
        overlayRect.anchorMax = new Vector2(0.7f, 0.08f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayBg = selectionOverlay.AddComponent<Image>();
        overlayBg.color = new Color(0.1f, 0.1f, 0.2f, 0.85f);

        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(selectionOverlay.transform, false);
        RectTransform promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = Vector2.zero;
        promptRect.anchorMax = Vector2.one;
        promptRect.offsetMin = new Vector2(10, 0);
        promptRect.offsetMax = new Vector2(-10, 0);
        selectionPromptText = promptObj.AddComponent<TextMeshProUGUI>();
        selectionPromptText.fontSize = 18;
        selectionPromptText.alignment = TextAlignmentOptions.Center;
        selectionPromptText.color = new Color(1f, 0.85f, 0.4f);
        selectionOverlay.SetActive(false);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[BluffActionUI] Default UI auto-created at runtime");
#endif
    }

    private (Button button, TMP_Text label) CreateRuntimeButton(string name, string text, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(buttonPanel.transform, false);
        obj.AddComponent<RectTransform>();

        Image img = obj.AddComponent<Image>();
        img.color = color;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 16;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        return (btn, label);
    }
}
