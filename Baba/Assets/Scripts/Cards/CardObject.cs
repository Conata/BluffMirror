using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

/// <summary>
/// カードのインタラクション状態
/// </summary>
public enum CardInteractionState
{
    Idle,              // 通常状態
    PointerDown,       // ポインタ押下
    Interrupting,      // 拒否演出中
    AwaitingConfirm,   // 確認UI待機中
    Committed          // 選択確定
}

public class CardObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Card Data")]
    public Card cardData;

    [Header("Visual")]
    public Renderer cardRenderer;
    public Material frontMaterial;
    public Material backMaterial;
    public TMP_Text cardLabel;  // カードのランク・スート表示用
    public bool isFaceUp = true;

    [Header("Animation")]
    public float hoverHeight = 0.05f;
    public float hoverDuration = 0.12f;
    public AnimationCurve hoverCurve;

    [Header("Interrupt Animation (Phase 4)")]
    [SerializeField] private float interruptBounceHeight = 0.08f;
    [SerializeField] private float interruptShakeAngle = 5f;
    [SerializeField] private float interruptDuration = 0.5f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private bool isHovering = false;
    private bool isDragging = false;
    private bool isSelectable = true;
    private Camera mainCamera;
    private ParticleSystem hoverAura;
    private CardInteractionState interactionState = CardInteractionState.Idle;

    // ホバー時間計測（行動分析用）
    private float hoverStartTime;
    public float LastHoverDuration { get; private set; }

    // ターン内ホバーカウント（迷い度分析用）
    public static int HoverCountThisTurn { get; private set; }
    public static void ResetHoverCount() { HoverCountThisTurn = 0; }

    public UnityEvent<CardObject> OnCardHovered;
    public UnityEvent<CardObject> OnCardSelected;
    public UnityEvent<CardObject> OnCardReleased;
    public UnityEvent<CardObject> OnCardClicked;
    public UnityEvent<CardObject> OnCardClickedForBluff;

    // Bluff action card selection (legacy - will be deprecated)
    private bool isBluffTargetable = false;
    public bool IsBluffTargetable => isBluffTargetable;

    public void SetBluffTargetable(bool targetable)
    {
        isBluffTargetable = targetable;
    }

    // Direct bluff interaction (Stage 16)
    private bool isPushed = false;
    private float pointerDownTime = 0f;
    private float lastClickTime = 0f;
    private int clickCount = 0;
    private Coroutine clickResetCoroutine;

    [Header("Bluff Interaction Timing")]
    [SerializeField] private float longPressDuration = 1.0f;
    [SerializeField] private float doubleClickWindow = 0.3f;

    public bool IsPushed => isPushed;

    private void Awake()
    {
        mainCamera = Camera.main;
        originalScale = transform.localScale;
        originalRotation = transform.rotation;

        // Auto-assign cardRenderer if not set
        if (cardRenderer == null)
        {
            cardRenderer = GetComponent<Renderer>();
        }

        // Set default hover curve if not assigned
        if (hoverCurve == null || hoverCurve.keys.Length == 0)
        {
            hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }

    private void Start()
    {
        UpdateCardLabel();
    }

    private void UpdateCardLabel()
    {
        if (cardLabel == null || cardData == null) return;

        // ジョーカーの場合
        if (cardData.isJoker)
        {
            cardLabel.text = "JOKER";
            cardLabel.color = Color.red;
            cardLabel.fontSize = 18f;
            cardLabel.gameObject.SetActive(isFaceUp);
            return;
        }

        // スート記号の取得
        string suitSymbol = GetSuitSymbol(cardData.suit);

        // ランク表示（数字またはA,J,Q,K）
        string rankText = GetRankText(cardData.rank);

        // テキスト設定
        cardLabel.text = $"{rankText}{suitSymbol}";

        // スートに応じた色設定
        cardLabel.color = (cardData.suit == CardSuit.Hearts || cardData.suit == CardSuit.Diamonds)
            ? new Color(0.8f, 0.1f, 0.1f) // Red
            : Color.black; // Black

        cardLabel.fontSize = 20f;

        // 表示状態を isFaceUp に合わせる
        cardLabel.gameObject.SetActive(isFaceUp);
    }

    private string GetSuitSymbol(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Spades: return "♠";
            case CardSuit.Hearts: return "♥";
            case CardSuit.Diamonds: return "♦";
            case CardSuit.Clubs: return "♣";
            default: return "";
        }
    }

    private string GetRankText(CardRank rank)
    {
        switch (rank)
        {
            case CardRank.Ace: return "A";
            case CardRank.Jack: return "J";
            case CardRank.Queen: return "Q";
            case CardRank.King: return "K";
            case CardRank.Two: return "2";
            case CardRank.Three: return "3";
            case CardRank.Four: return "4";
            case CardRank.Five: return "5";
            case CardRank.Six: return "6";
            case CardRank.Seven: return "7";
            case CardRank.Eight: return "8";
            case CardRank.Nine: return "9";
            case CardRank.Ten: return "10";
            default: return "";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && isSelectable)
        {
            HoverCountThisTurn++;
            hoverStartTime = Time.time;
            SetHoverState(true);
            OnCardHovered?.Invoke(this);

            // ホバーSFX
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCardHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && isSelectable)
        {
            LastHoverDuration = Time.time - hoverStartTime;
            SetHoverState(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isSelectable) return;

        isDragging = true;
        OnCardSelected?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isSelectable) return;

        // スクリーン座標からワールド座標変換
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("DragPlane")))
        {
            transform.position = Vector3.Lerp(transform.position, hit.point, Time.deltaTime * 8f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isSelectable) return;

        isDragging = false;
        OnCardReleased?.Invoke(this);
    }

    /// <summary>
    /// Phase 4: ポインタ押下時（拒否演出開始 + ブラフインタラクション開始）
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Stage 16: Direct bluff interaction tracking
        pointerDownTime = Time.time;

        // Bluff action card selection takes priority (legacy)
        if (isBluffTargetable)
        {
            OnCardClickedForBluff?.Invoke(this);
            return;
        }

        if (!isSelectable || interactionState != CardInteractionState.Idle) return;

        interactionState = CardInteractionState.PointerDown;

        // カード選択SFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardPick(transform.position);

        // GameManagerに通知（拒否された場合はIdle状態に戻す）
        if (GameManager.Instance != null)
        {
            bool accepted = GameManager.Instance.OnCardPointerDown(this);
            if (!accepted)
            {
                interactionState = CardInteractionState.Idle;
            }
        }
        else
        {
            interactionState = CardInteractionState.Idle;
        }
    }

    /// <summary>
    /// Phase 4: ポインタ解放時（ブラフインタラクション判定）
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        // Stage 16: Detect bluff interaction type
        float pressDuration = Time.time - pointerDownTime;

        // Only process bluff actions during player's turn or AI turn
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.CurrentState;
        bool isBluffAllowed = state == GameState.PLAYER_TURN_PICK
            || state == GameState.AI_TURN_APPROACH
            || state == GameState.AI_TURN_HESITATE
            || state == GameState.AI_TURN_COMMIT;

        if (!isBluffAllowed) return;

        // Long Press: Shuffle entire hand
        if (pressDuration >= longPressDuration)
        {
            TriggerBluffAction(BluffActionType.Shuffle, -1);
            return;
        }

        // Track clicks for double-click detection
        clickCount++;
        float timeSinceLastClick = Time.time - lastClickTime;
        lastClickTime = Time.time;

        if (clickResetCoroutine != null)
            StopCoroutine(clickResetCoroutine);

        if (clickCount == 1)
        {
            // Wait for potential second click
            clickResetCoroutine = StartCoroutine(ResetClickCountAfterDelay());
        }
        else if (clickCount == 2 && timeSinceLastClick <= doubleClickWindow)
        {
            // Double-click: Wiggle
            clickCount = 0;
            if (clickResetCoroutine != null)
            {
                StopCoroutine(clickResetCoroutine);
                clickResetCoroutine = null;
            }
            TriggerBluffAction(BluffActionType.Wiggle, GetCardIndex());
        }
    }

    private IEnumerator ResetClickCountAfterDelay()
    {
        yield return new WaitForSeconds(doubleClickWindow);

        // Single click confirmed: Push or Pull
        if (clickCount == 1)
        {
            if (isPushed)
            {
                // Pull back
                TriggerBluffAction(BluffActionType.Pull, GetCardIndex());
                isPushed = false;
            }
            else
            {
                // Push forward
                TriggerBluffAction(BluffActionType.Push, GetCardIndex());
                isPushed = true;
            }
        }

        clickCount = 0;
    }

    private void TriggerBluffAction(BluffActionType actionType, int cardIndex)
    {
        if (BluffActionSystem.Instance == null)
        {
            Debug.LogWarning("[CardObject] BluffActionSystem not found");
            return;
        }

        switch (actionType)
        {
            case BluffActionType.Shuffle:
                BluffActionSystem.Instance.ShuffleCards();
                break;
            case BluffActionType.Push:
                BluffActionSystem.Instance.PushCard(cardIndex);
                break;
            case BluffActionType.Pull:
                BluffActionSystem.Instance.PullCard(cardIndex);
                break;
            case BluffActionType.Wiggle:
                BluffActionSystem.Instance.WiggleCard(cardIndex);
                break;
        }
    }

    private int GetCardIndex()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerHand == null)
            return -1;

        var cards = GameManager.Instance.PlayerHand.GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == this)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Phase 4: カード引き拒否アニメーション
    /// カードが跳ね返り、揺れる演出
    /// </summary>
    public void PlayInterruptAnimation()
    {
        if (interactionState != CardInteractionState.PointerDown) return;

        interactionState = CardInteractionState.Interrupting;

        // DOTweenシーケンス
        Sequence sequence = DOTween.Sequence();

        // Phase 1: 跳ね返り（手前に移動）
        Vector3 bouncePos = transform.localPosition + Vector3.back * interruptBounceHeight;
        sequence.Append(transform.DOLocalMove(bouncePos, interruptDuration * 0.3f).SetEase(Ease.OutQuad));

        // Phase 2: 元の位置に戻る + 揺れ
        sequence.Append(transform.DOLocalMove(originalPosition, interruptDuration * 0.4f).SetEase(Ease.InOutQuad));

        // 同時に左右揺れ（Z軸回転）
        sequence.Join(transform.DORotate(
            new Vector3(originalRotation.eulerAngles.x, originalRotation.eulerAngles.y, interruptShakeAngle),
            interruptDuration * 0.15f, RotateMode.Fast)
            .SetEase(Ease.InOutSine)
            .SetLoops(4, LoopType.Yoyo)); // 左右に2回揺れる

        // Phase 3: 静止（確認UI待機状態へ）
        sequence.OnComplete(() =>
        {
            // 回転をリセット
            transform.rotation = originalRotation;
            interactionState = CardInteractionState.AwaitingConfirm;
        });
    }

    /// <summary>
    /// Phase 4: インタラクション状態をリセット
    /// </summary>
    public void ResetInteractionState()
    {
        interactionState = CardInteractionState.Idle;
    }

    /// <summary>
    /// Phase 4: 選択確定状態に変更
    /// </summary>
    public void SetCommitted()
    {
        interactionState = CardInteractionState.Committed;
    }

    /// <summary>
    /// Phase 4: 現在のインタラクション状態を取得
    /// </summary>
    public CardInteractionState GetInteractionState()
    {
        return interactionState;
    }

    private void SetHoverState(bool hover)
    {
        isHovering = hover;
        Vector3 targetPos = originalPosition + (hover ? Vector3.up * hoverHeight : Vector3.zero);

        DOTween.To(() => transform.localPosition, x => transform.localPosition = x, targetPos, hoverDuration)
               .SetEase(hoverCurve);

        // ホバーオーラエフェクト
        if (hover)
        {
            if (CardEffectsManager.Instance != null && hoverAura == null)
            {
                hoverAura = CardEffectsManager.Instance.PlayHoverAura(this);
            }
        }
        else
        {
            if (hoverAura != null)
            {
                if (CardEffectsManager.Instance != null)
                {
                    CardEffectsManager.Instance.StopHoverAura(hoverAura);
                }
                else
                {
                    Destroy(hoverAura.gameObject);
                }
                hoverAura = null;
            }
        }
    }

    public void FlipCard(bool faceUp, float duration = 0.3f)
    {
        isFaceUp = faceUp;

        // フリップSFX
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardFlip(1.0f / duration);

        Vector3 targetScale = new Vector3(0, originalScale.y, originalScale.z);
        transform.DOScale(targetScale, duration * 0.5f)
                 .OnComplete(() => {
                     if (cardRenderer != null)
                     {
                         cardRenderer.material = faceUp ? frontMaterial : backMaterial;
                     }

                     // ラベルの表示/非表示
                     if (cardLabel != null)
                     {
                         cardLabel.gameObject.SetActive(faceUp);
                     }

                     transform.DOScale(originalScale, duration * 0.5f);
                 });
    }

    public void SetOriginalPosition(Vector3 pos)
    {
        originalPosition = pos;
    }

    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;

        // 選択不可の場合、ホバー状態をリセット
        if (!selectable && isHovering)
        {
            SetHoverState(false);
        }
    }
}
