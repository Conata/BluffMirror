# 実装ガイド

## コンポーネント設計

### 1. カードシステム

#### Card.cs (基底クラス)
```csharp
[System.Serializable]
public class Card
{
    public CardSuit suit;
    public CardRank rank;
    public bool isJoker;
    public Sprite frontTexture;
    public Sprite backTexture;
    
    public bool IsMatchingPair(Card other)
    {
        return this.rank == other.rank && !this.isJoker && !other.isJoker;
    }
}

public enum CardSuit { Hearts, Diamonds, Clubs, Spades }
public enum CardRank { Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
```

#### CardObject.cs (Unity GameObjectコンポーネント)
```csharp
public class CardObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Card Data")]
    public Card cardData;
    
    [Header("Visual")]
    public Renderer cardRenderer;
    public Material frontMaterial;
    public Material backMaterial;
    public bool isFaceUp = true;
    
    [Header("Animation")]
    public float hoverHeight = 0.05f;
    public float hoverDuration = 0.12f;
    public AnimationCurve hoverCurve;
    
    private Vector3 originalPosition;
    private bool isHovering = false;
    private bool isDragging = false;
    
    public UnityEvent<CardObject> OnCardHovered;
    public UnityEvent<CardObject> OnCardSelected;
    public UnityEvent<CardObject> OnCardReleased;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging)
        {
            SetHoverState(true);
            OnCardHovered?.Invoke(this);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            SetHoverState(false);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        OnCardSelected?.Invoke(this);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        // スクリーン座標からワールド座標変換
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("DragPlane")))
        {
            transform.position = Vector3.Lerp(transform.position, hit.point, Time.deltaTime * 8f);
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        OnCardReleased?.Invoke(this);
    }
    
    private void SetHoverState(bool hover)
    {
        isHovering = hover;
        Vector3 targetPos = originalPosition + (hover ? Vector3.up * hoverHeight : Vector3.zero);
        
        DOTween.To(() => transform.position, x => transform.position = x, targetPos, hoverDuration)
               .SetEase(hoverCurve);
    }
    
    public void FlipCard(bool faceUp, float duration = 0.3f)
    {
        isFaceUp = faceUp;
        
        transform.DOScaleX(0, duration * 0.5f)
                 .OnComplete(() => {
                     cardRenderer.material = faceUp ? frontMaterial : backMaterial;
                     transform.DOScaleX(1, duration * 0.5f);
                 });
    }
}
```

### 2. 手札管理システム

#### HandController.cs (基底クラス)
```csharp
public abstract class HandController : MonoBehaviour
{
    [Header("Hand Settings")]
    public Transform[] cardSlots;
    public float cardSpacing = 0.1f;
    public AnimationCurve cardArcCurve;
    
    protected List<CardObject> cardsInHand = new List<CardObject>();
    
    public abstract void AddCard(CardObject card);
    public abstract CardObject RemoveCard(int index);
    public abstract void ArrangeCards();
    
    protected Vector3 CalculateCardPosition(int index, int totalCards)
    {
        if (totalCards <= 1) return transform.position;
        
        float t = (float)index / (totalCards - 1);
        Vector3 basePosition = Vector3.Lerp(cardSlots[0].position, cardSlots[cardSlots.Length - 1].position, t);
        
        // アーク形状の計算
        float arcHeight = cardArcCurve.Evaluate(t) * 0.1f;
        basePosition += Vector3.up * arcHeight;
        
        return basePosition;
    }
}
```

#### PlayerHandController.cs
```csharp
public class PlayerHandController : HandController
{
    [Header("Player Hand Specific")]
    public float fanAngle = 25f;  // 扇状の角度
    public float cardDistance = 0.8f;  // カメラからの距離
    
    public override void AddCard(CardObject card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(transform);
        card.isFaceUp = true;
        card.FlipCard(true);
        
        ArrangeCards();
        
        // ペア判定
        CheckForPairs();
    }
    
    public override CardObject RemoveCard(int index)
    {
        if (index < 0 || index >= cardsInHand.Count) return null;
        
        CardObject removedCard = cardsInHand[index];
        cardsInHand.RemoveAt(index);
        
        ArrangeCards();
        
        return removedCard;
    }
    
    public override void ArrangeCards()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Vector3 targetPosition = CalculateFanPosition(i, cardsInHand.Count);
            Quaternion targetRotation = CalculateFanRotation(i, cardsInHand.Count);
            
            cardsInHand[i].transform.DOMove(targetPosition, 0.3f).SetEase(Ease.OutQuart);
            cardsInHand[i].transform.DORotateQuaternion(targetRotation, 0.3f).SetEase(Ease.OutQuart);
        }
    }
    
    private Vector3 CalculateFanPosition(int index, int totalCards)
    {
        if (totalCards <= 1)
            return transform.position;
        
        float angleStep = fanAngle / (totalCards - 1);
        float currentAngle = -fanAngle * 0.5f + angleStep * index;
        
        Vector3 direction = new Vector3(Mathf.Sin(Mathf.Deg2Rad * currentAngle), 0, Mathf.Cos(Mathf.Deg2Rad * currentAngle));
        return transform.position + direction * cardDistance;
    }
    
    private Quaternion CalculateFanRotation(int index, int totalCards)
    {
        if (totalCards <= 1)
            return transform.rotation;
        
        float angleStep = fanAngle / (totalCards - 1);
        float currentAngle = -fanAngle * 0.5f + angleStep * index;
        
        return transform.rotation * Quaternion.Euler(0, currentAngle * 0.3f, 0);
    }
    
    private void CheckForPairs()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            for (int j = i + 1; j < cardsInHand.Count; j++)
            {
                if (cardsInHand[i].cardData.IsMatchingPair(cardsInHand[j].cardData))
                {
                    RemovePair(i, j);
                    return; // 一度に一ペアのみ処理
                }
            }
        }
    }
    
    private void RemovePair(int index1, int index2)
    {
        // アニメーション付きでペア消去
        CardObject card1 = cardsInHand[index1];
        CardObject card2 = cardsInHand[index2];
        
        // 燃える・溶ける演出
        StartCoroutine(PlayPairDisappearEffect(card1, card2));
        
        // リストから削除
        cardsInHand.RemoveAt(Mathf.Max(index1, index2));
        cardsInHand.RemoveAt(Mathf.Min(index1, index2));
        
        ArrangeCards();
    }
    
    private IEnumerator PlayPairDisappearEffect(CardObject card1, CardObject card2)
    {
        // グロー効果
        // TODO: パーティクルエフェクト追加
        
        yield return new WaitForSeconds(0.1f);
        
        // 消失アニメーション
        card1.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);
        card2.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);
        
        yield return new WaitForSeconds(0.18f);
        
        // DiscardPileに移動
        GameManager.Instance.discardPile.AddCards(card1, card2);
    }
}
```

#### AIHandController.cs
```csharp
public class AIHandController : HandController
{
    [Header("AI Hand Specific")]
    public Transform aiPosition;
    public float cardHeight = 0.05f;
    
    public override void AddCard(CardObject card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(transform);
        card.isFaceUp = false;
        card.FlipCard(false);
        
        ArrangeCards();
    }
    
    public override CardObject RemoveCard(int index)
    {
        if (index < 0 || index >= cardsInHand.Count) return null;
        
        CardObject removedCard = cardsInHand[index];
        cardsInHand.RemoveAt(index);
        
        ArrangeCards();
        
        return removedCard;
    }
    
    public override void ArrangeCards()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Vector3 targetPosition = CalculateAICardPosition(i);
            cardsInHand[i].transform.DOMove(targetPosition, 0.25f).SetEase(Ease.OutQuart);
        }
    }
    
    private Vector3 CalculateAICardPosition(int index)
    {
        Vector3 basePosition = aiPosition.position;
        basePosition.x += index * cardSpacing - (cardsInHand.Count - 1) * cardSpacing * 0.5f;
        basePosition.y += index * cardHeight * 0.1f; // 少し重ねる
        
        return basePosition;
    }
    
    public CardObject DrawFromPlayer(PlayerHandController playerHand)
    {
        if (playerHand.cardsInHand.Count == 0) return null;
        
        // AI選択ロジック（後で詳細化）
        int selectedIndex = UnityEngine.Random.Range(0, playerHand.cardsInHand.Count);
        
        return playerHand.RemoveCard(selectedIndex);
    }
}
```

### 3. ゲーム状態管理

#### GameManager.cs
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Components")]
    public PlayerHandController playerHand;
    public AIHandController aiHand;
    public DiscardPile discardPile;
    public CardDeck cardDeck;
    
    [Header("Game State")]
    public GameState currentState;
    public int currentPlayerTurn; // 0 = Player, 1 = AI
    public int turnCounter;
    
    public UnityEvent<GameState> OnGameStateChanged;
    public UnityEvent<int> OnTurnChanged;
    public UnityEvent<string> OnGameEnded; // Winner
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartNewGame();
    }
    
    public void StartNewGame()
    {
        ChangeState(GameState.Setup);
        
        // カードデッキ初期化
        cardDeck.Initialize();
        
        // 初期配布
        DealInitialCards();
        
        ChangeState(GameState.PlayerTurn);
    }
    
    private void DealInitialCards()
    {
        // 各プレイヤーに7枚配布
        for (int i = 0; i < 7; i++)
        {
            playerHand.AddCard(cardDeck.DrawCard());
            aiHand.AddCard(cardDeck.DrawCard());
        }
    }
    
    public void ExecutePlayerTurn(int aiCardIndex)
    {
        if (currentState != GameState.PlayerTurn) return;
        
        CardObject drawnCard = aiHand.RemoveCard(aiCardIndex);
        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);
            
            CheckGameEndCondition();
            NextTurn();
        }
    }
    
    public void ExecuteAITurn()
    {
        if (currentState != GameState.AITurn) return;
        
        CardObject drawnCard = aiHand.DrawFromPlayer(playerHand);
        if (drawnCard != null)
        {
            aiHand.AddCard(drawnCard);
            
            CheckGameEndCondition();
            NextTurn();
        }
    }
    
    private void NextTurn()
    {
        currentPlayerTurn = 1 - currentPlayerTurn;
        turnCounter++;
        
        ChangeState(currentPlayerTurn == 0 ? GameState.PlayerTurn : GameState.AITurn);
        OnTurnChanged?.Invoke(currentPlayerTurn);
    }
    
    private void CheckGameEndCondition()
    {
        if (playerHand.cardsInHand.Count == 0)
        {
            EndGame("Player");
        }
        else if (aiHand.cardsInHand.Count == 0)
        {
            EndGame("AI");
        }
        else if (playerHand.cardsInHand.Count == 1 && playerHand.cardsInHand[0].cardData.isJoker)
        {
            EndGame("AI"); // プレイヤーがジョーカーで負け
        }
        else if (aiHand.cardsInHand.Count == 1 && aiHand.cardsInHand[0].cardData.isJoker)
        {
            EndGame("Player"); // AIがジョーカーで負け
        }
    }
    
    private void EndGame(string winner)
    {
        ChangeState(GameState.GameEnd);
        OnGameEnded?.Invoke(winner);
        
        // 終了演出
        StartCoroutine(PlayEndGameSequence(winner));
    }
    
    private IEnumerator PlayEndGameSequence(string winner)
    {
        // ジョーカー公開演出など
        yield return new WaitForSeconds(2f);
        
        // リザルト表示
        // TODO: リザルトUI表示
    }
    
    private void ChangeState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
}

public enum GameState
{
    Setup,
    PlayerTurn,
    AITurn,
    GameEnd
}
```

### 4. カメラ制御

#### FPSCameraController.cs
```csharp
public class FPSCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float fov = 55f;
    public Vector3 basePosition = new Vector3(0, 1.2f, 2.2f);
    public Vector3 lookAtTarget = new Vector3(0, 1.05f, 0);
    
    [Header("Breathing Effect")]
    public float breathingStrength = 0.003f;
    public float breathingSpeed = 0.6f;
    
    [Header("Draw Focus")]
    public float focusZoomAmount = 0.15f;
    public float focusZoomDuration = 0.18f;
    
    private Camera cam;
    private Vector3 originalPosition;
    private float time;
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fov;
        originalPosition = basePosition;
        transform.position = basePosition;
        transform.LookAt(lookAtTarget);
    }
    
    private void Update()
    {
        time += Time.deltaTime;
        
        // 呼吸による微揺れ
        Vector3 breathingOffset = new Vector3(
            Mathf.Sin(time * breathingSpeed) * breathingStrength,
            Mathf.Sin(time * breathingSpeed * 0.7f) * breathingStrength * 0.5f,
            0
        );
        
        transform.position = originalPosition + breathingOffset;
    }
    
    public void FocusOnCardDraw()
    {
        // カード引く瞬間の寄り
        Vector3 focusPosition = originalPosition + Vector3.forward * focusZoomAmount;
        
        transform.DOMove(focusPosition, focusZoomDuration)
                 .SetEase(Ease.OutQuart)
                 .SetLoops(2, LoopType.Yoyo);
    }
    
    public void ShakeCamera(float intensity, float duration)
    {
        // 衝撃演出
        transform.DOShakePosition(duration, intensity, 30, 90, false, true);
    }
}
```

## 実装フロー

### Phase 1: 基盤構築
1. シーンセットアップ（テーブル・ライティング・カメラ）
2. CardObjectの基本実装
3. HandControllerの基本機能

### Phase 2: ゲームロジック
1. カード配布システム
2. ターン管理
3. ペア消去ロジック
4. 勝利条件判定

### Phase 3: UI・演出
1. FloatingTextSystem
2. パーティクルエフェクト
3. サウンドシステム
4. ポストプロセシング

### Phase 4: AI・心理圧
1. AI選択ロジック
2. セリフ生成システム
3. 心理圧演出
4. バランス調整

---
**Document Version**: 1.0  
**Last Updated**: 2026-02-07  
**Next**: アート仕様作成