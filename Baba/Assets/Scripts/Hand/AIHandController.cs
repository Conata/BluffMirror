using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;

public class AIHandController : HandController
{
    [Header("AI Hand Specific")]
    public Transform aiPosition;
    public float cardHeight = 0.05f;

    [Header("AI Fan Layout")]
    public float aiFanAngle = 25f;
    public float aiFanRadius = 0.9f;
    public float aiCardTilt = 10f;

    [Header("Bluff Action Settings")]
    [SerializeField] private float aiPushPullDistance = 0.06f;
    [SerializeField] private float aiWiggleAngle = 6f;
    [SerializeField] private float aiWiggleDuration = 0.4f;
    [SerializeField] private float aiSpreadCloseStep = 4f;
    [SerializeField] private float aiMaxFanAngleModifier = 12f;
    [SerializeField] private float aiMinFanAngleModifier = -8f;

    [Header("Psychology Integration")]
    [SerializeField] private PsychologySystem psychologySystem;
    [SerializeField] private PlayerBehaviorAnalyzer behaviorAnalyzer;
    [SerializeField] private LLMManager llmManager;

    public override void AddCard(CardObject card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(transform);
        card.isFaceUp = false;
        card.FlipCard(false);

        // Set layer to AICards so it can be clicked by player
        int aiLayer = LayerMask.NameToLayer("AICards");
        if (aiLayer >= 0)
            card.gameObject.layer = aiLayer;
        else
            Debug.LogWarning("[AIHandController] Layer 'AICards' not found. Add it in Edit > Project Settings > Tags and Layers.");

        // Add click/hover listeners for player interaction
        card.OnCardClicked.AddListener((clickedCard) => OnAICardClicked(clickedCard));
        card.OnCardHovered.AddListener((hoveredCard) => OnAICardHovered(hoveredCard));

        ArrangeCards();
        RaiseOnCardAdded(card);
    }

    public override CardObject RemoveCard(int index)
    {
        if (index < 0 || index >= cardsInHand.Count) return null;

        CardObject removedCard = cardsInHand[index];
        cardsInHand.RemoveAt(index);

        // Remove click listener when card is removed from AI hand
        removedCard.OnCardClicked.RemoveAllListeners();

        ArrangeCards();

        return removedCard;
    }

    public override void ArrangeCards()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Vector3 targetLocalPosition = CalculateAICardPosition(i);
            Quaternion targetRotation = CalculateAIFanRotation(i, cardsInHand.Count);

            cardsInHand[i].transform.DOLocalMove(targetLocalPosition, 0.25f).SetEase(Ease.OutQuart);
            cardsInHand[i].transform.DOLocalRotateQuaternion(targetRotation, 0.25f).SetEase(Ease.OutQuart);

            // Set original local position for hover animation
            cardsInHand[i].SetOriginalPosition(targetLocalPosition);
        }
    }

    private Vector3 CalculateAICardPosition(int index)
    {
        int totalCards = cardsInHand.Count;
        if (totalCards <= 1)
            return Vector3.zero;

        // カード枚数に応じてファン角度を調整（少ない時は狭く）
        float effectiveFanAngle = Mathf.Min(aiFanAngle + currentFanAngleModifier, (totalCards - 1) * 4.5f);
        float t = (float)index / (totalCards - 1) - 0.5f;
        float angleRad = t * effectiveFanAngle * Mathf.Deg2Rad;

        // 円弧上の位置
        float x = aiFanRadius * Mathf.Sin(angleRad);
        float y = aiFanRadius * (Mathf.Cos(angleRad) - 1f);
        float z = index * 0.002f; // プレイヤーとは逆のZ順

        return new Vector3(x, y, z);
    }

    private Quaternion CalculateAIFanRotation(int index, int totalCards)
    {
        if (totalCards <= 1)
            return Quaternion.Euler(90f - aiCardTilt, 180f, 0);

        float effectiveFanAngle = Mathf.Min(aiFanAngle + currentFanAngleModifier, (totalCards - 1) * 4.5f);
        float t = (float)index / (totalCards - 1) - 0.5f;
        float zRotation = -t * effectiveFanAngle;

        return Quaternion.Euler(90f - aiCardTilt, 180f, zRotation);
    }

    // === Bluff Action Overrides ===

    public override void ShuffleCards()
    {
        if (isBluffAnimating || cardsInHand.Count <= 1) return;
        isBluffAnimating = true;

        ShuffleCardList();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCardFlip();

        ArrangeCards();

        DOVirtual.DelayedCall(0.3f, () => isBluffAnimating = false);
    }

    public override void PushCard(int cardIndex)
    {
        if (isBluffAnimating || cardIndex < 0 || cardIndex >= cardsInHand.Count) return;
        isBluffAnimating = true;

        CardObject card = cardsInHand[cardIndex];
        Vector3 targetPos = card.transform.localPosition + Vector3.forward * aiPushPullDistance;

        card.transform.DOLocalMove(targetPos, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                card.SetOriginalPosition(targetPos);
                isBluffAnimating = false;
            });
    }

    public override void PullCard(int cardIndex)
    {
        if (isBluffAnimating || cardIndex < 0 || cardIndex >= cardsInHand.Count) return;
        isBluffAnimating = true;

        CardObject card = cardsInHand[cardIndex];
        Vector3 targetPos = card.transform.localPosition - Vector3.forward * aiPushPullDistance;

        card.transform.DOLocalMove(targetPos, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                card.SetOriginalPosition(targetPos);
                isBluffAnimating = false;
            });
    }

    public override void WiggleCard(int cardIndex)
    {
        if (isBluffAnimating || cardIndex < 0 || cardIndex >= cardsInHand.Count) return;
        isBluffAnimating = true;

        CardObject card = cardsInHand[cardIndex];
        card.transform.DOShakeRotation(aiWiggleDuration, new Vector3(0, 0, aiWiggleAngle), 10, 90f, true, ShakeRandomnessMode.Harmonic)
            .OnComplete(() => isBluffAnimating = false);
    }

    public override void SpreadFan()
    {
        if (isBluffAnimating || cardsInHand.Count <= 1) return;
        isBluffAnimating = true;

        currentFanAngleModifier = Mathf.Clamp(currentFanAngleModifier + aiSpreadCloseStep, aiMinFanAngleModifier, aiMaxFanAngleModifier);
        ArrangeCards();

        DOVirtual.DelayedCall(0.3f, () => isBluffAnimating = false);
    }

    public override void CloseFan()
    {
        if (isBluffAnimating || cardsInHand.Count <= 1) return;
        isBluffAnimating = true;

        currentFanAngleModifier = Mathf.Clamp(currentFanAngleModifier - aiSpreadCloseStep, aiMinFanAngleModifier, aiMaxFanAngleModifier);
        ArrangeCards();

        DOVirtual.DelayedCall(0.3f, () => isBluffAnimating = false);
    }

    public CardObject DrawFromPlayer(PlayerHandController playerHand)
    {
        if (playerHand.GetCardCount() == 0) return null;

        // ===== AI選択ロジック =====
        // Phase 1: ランダム選択
        // Phase 3B: 行動パターン分析統合（将来拡張）
        // TODO: LLM強化 - プレイヤーの癖を学習し、最適なカードを選択
        //       - 行動パターン（behaviorAnalyzer.CurrentBehavior）を分析
        //       - 圧力レベル（psychologySystem.GetPressureLevel()）を考慮
        //       - LLMManager.GenerateAIDecisionAsync() を呼び出し

        int selectedIndex;

        if (behaviorAnalyzer != null)
        {
            // 簡易的な行動パターンベース選択（Phase 3B）
            BehaviorPattern pattern = behaviorAnalyzer.CurrentBehavior;

            // プレイヤーに位置好みがある場合、その位置を避ける
            if (pattern.hasPositionPreference && pattern.preferredPosition >= 0 && pattern.preferredPosition < playerHand.GetCardCount())
            {
                // 好みの位置以外からランダム選択
                selectedIndex = UnityEngine.Random.Range(0, playerHand.GetCardCount());
                if (selectedIndex == pattern.preferredPosition)
                {
                    selectedIndex = (selectedIndex + 1) % playerHand.GetCardCount();
                }
                Debug.Log($"[AIHandController] Avoiding player's preferred position {pattern.preferredPosition}, selected {selectedIndex}");
            }
            else
            {
                // 通常のランダム選択
                selectedIndex = UnityEngine.Random.Range(0, playerHand.GetCardCount());
            }
        }
        else
        {
            // フォールバック: ランダム選択
            selectedIndex = UnityEngine.Random.Range(0, playerHand.GetCardCount());
        }

        Debug.Log($"[AIHandController] Selected player card index: {selectedIndex}");

        return playerHand.RemoveCard(selectedIndex);
    }

    /// <summary>
    /// LLM強化版: プレイヤーからカードを引く（非同期）
    /// Phase 3B: プレイヤーの心理状態を分析し、最適なカードを選択
    /// </summary>
    public async Task<CardObject> DrawFromPlayerAsync(PlayerHandController playerHand)
    {
        if (playerHand.GetCardCount() == 0) return null;

        int selectedIndex = 0;
        float confidence = 0.5f;
        string strategy = "Random";

        // ===== LLM強化判断 =====
        if (llmManager != null && behaviorAnalyzer != null && psychologySystem != null)
        {
            try
            {
                // 行動パターンと圧力レベルを取得
                BehaviorPattern pattern = behaviorAnalyzer.CurrentBehavior;
                float pressureLevel = psychologySystem.GetPressureLevel();
                int playerCardCount = playerHand.GetCardCount();

                Debug.Log($"[AIHandController] Requesting LLM decision: doubt={pattern.doubtLevel:F2}, pressure={pressureLevel:F1}, cards={playerCardCount}");

                // LLM判断を取得（3層フォールバック付き）
                AIDecisionResult decision = await llmManager.GenerateAIDecisionAsync(
                    pattern,
                    pressureLevel,
                    playerCardCount
                );

                if (decision != null)
                {
                    selectedIndex = decision.selectedCardIndex;
                    confidence = decision.confidence;
                    strategy = decision.strategy;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[AIHandController] LLM decision received: index={selectedIndex}, confidence={confidence:F2}, strategy={strategy}");
#endif

                    // 範囲チェック
                    selectedIndex = Mathf.Clamp(selectedIndex, 0, playerCardCount - 1);
                }
                else
                {
                    Debug.LogWarning("[AIHandController] LLM decision was null, using fallback");
                    selectedIndex = UnityEngine.Random.Range(0, playerCardCount);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AIHandController] Error during LLM decision: {ex.Message}");
                selectedIndex = UnityEngine.Random.Range(0, playerHand.GetCardCount());
            }
        }
        else
        {
            // コンポーネントがない場合のフォールバック
            Debug.LogWarning("[AIHandController] LLM components not available, using random selection");
            selectedIndex = UnityEngine.Random.Range(0, playerHand.GetCardCount());
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AIHandController] Final selection: index={selectedIndex}, confidence={confidence:F2}, strategy={strategy}");
#endif

        // カードを引く
        return playerHand.RemoveCard(selectedIndex);
    }

    public IEnumerator ExecuteAITurn(PlayerHandController playerHand)
    {
        Debug.Log("[AIHandController] AI Turn started");

        // ===== Phase 3B: ターン前分析 =====
        if (behaviorAnalyzer != null)
        {
            // プレイヤーの行動パターンを取得
            BehaviorPattern playerBehavior = behaviorAnalyzer.CurrentBehavior;
            Debug.Log($"[AIHandController] Analyzing player behavior: doubt={playerBehavior.doubtLevel:F2}");
        }

        // AI思考時間（演出）
        float thinkingTime = UnityEngine.Random.Range(0.8f, 1.5f);
        Debug.Log($"[AIHandController] AI thinking for {thinkingTime:F2}s...");
        yield return new WaitForSeconds(thinkingTime);

        // プレイヤーからカードを引く（非同期LLM判断）
        Task<CardObject> drawTask = DrawFromPlayerAsync(playerHand);

        // 非同期タスクの完了を待機（UIブロッキングなし）
        while (!drawTask.IsCompleted)
        {
            yield return null;
        }

        // タスク例外ハンドリング追加
        CardObject drawnCard = null;
        if (drawTask.IsCompletedSuccessfully)
        {
            drawnCard = drawTask.Result;
        }
        else if (drawTask.IsFaulted)
        {
            Debug.LogError($"[AIHandController] DrawFromPlayerAsync failed: {drawTask.Exception?.InnerException?.Message}");
            Debug.LogError($"[AIHandController] Full exception: {drawTask.Exception}");
            // フォールバック: 同期版を使用
            drawnCard = DrawFromPlayer(playerHand);
        }
        else if (drawTask.IsCanceled)
        {
            Debug.LogWarning("[AIHandController] DrawFromPlayerAsync was cancelled");
            // フォールバック: 同期版を使用
            drawnCard = DrawFromPlayer(playerHand);
        }

        if (drawnCard != null)
        {
            Debug.Log($"[AIHandController] Drew card from player: {drawnCard.cardData.suit} {drawnCard.cardData.rank}");

            AddCard(drawnCard);

            // ===== Phase 3B: ドロー後リアクション =====
            yield return StartCoroutine(PlayDrawReaction(drawnCard));

            // ペア判定（AIは自動でペアを消去）
            int pairsRemoved = CheckForPairs();

            if (pairsRemoved > 0)
            {
                Debug.Log($"[AIHandController] Removed {pairsRemoved} pair(s)");
            }
        }
        else
        {
            Debug.LogWarning("[AIHandController] Failed to draw card from player");
        }

        Debug.Log("[AIHandController] AI Turn completed");
    }

    /// <summary>
    /// カードドロー後のリアクション（Phase 3B統合）
    /// </summary>
    private IEnumerator PlayDrawReaction(CardObject drawnCard)
    {
        if (psychologySystem == null)
        {
            yield break; // PsychologySystemがない場合はスキップ
        }

        // カードの種類に応じてリアクション
        bool isJoker = drawnCard.cardData.isJoker;

        if (isJoker)
        {
            // ジョーカーを引いた = AIにとって悪い結果
            Debug.Log("[AIHandController] Drew Joker - Neutral/Focused reaction");
            // PsychologySystemの圧力レベルを下げる（プレイヤーに有利）
            float currentPressure = psychologySystem.GetPressureLevel();
            psychologySystem.SetPressureLevel(Mathf.Max(0, currentPressure - 0.5f));
        }
        else
        {
            // 通常カードを引いた
            Debug.Log("[AIHandController] Drew normal card");

            // ペアが揃うかチェック（将来的にLLM強化判断に使用）
            bool willFormPair = CheckIfCardFormsPair(drawnCard);

            if (willFormPair)
            {
                Debug.Log("[AIHandController] Card forms a pair - Confident reaction");
                // PsychologySystemの圧力レベルを上げる
                float currentPressure = psychologySystem.GetPressureLevel();
                psychologySystem.SetPressureLevel(Mathf.Min(3.0f, currentPressure + 0.3f));
            }
        }

        // リアクション演出の時間を確保
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// カードがペアを形成するかチェック
    /// </summary>
    private bool CheckIfCardFormsPair(CardObject card)
    {
        foreach (var handCard in cardsInHand)
        {
            if (handCard != card && handCard.cardData.IsMatchingPair(card.cardData))
            {
                return true;
            }
        }
        return false;
    }

    public override int CheckForPairs()
    {
        int pairsRemoved = 0;
        bool foundPair = true;

        // すべてのペアを削除するまでループ
        while (foundPair)
        {
            foundPair = false;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                for (int j = i + 1; j < cardsInHand.Count; j++)
                {
                    if (cardsInHand[i].cardData.IsMatchingPair(cardsInHand[j].cardData))
                    {
                        RemoveAIPair(i, j);
                        pairsRemoved++;
                        foundPair = true;
                        break; // ペアを削除したら、最初からやり直す
                    }
                }
                if (foundPair) break;
            }
        }

        return pairsRemoved;
    }

    private void RemoveAIPair(int index1, int index2)
    {
        CardObject card1 = cardsInHand[index1];
        CardObject card2 = cardsInHand[index2];

        StartCoroutine(PlayAIPairDisappearEffect(card1, card2));

        // リストから削除
        cardsInHand.RemoveAt(Mathf.Max(index1, index2));
        cardsInHand.RemoveAt(Mathf.Min(index1, index2));

        RaiseOnPairMatched(card1, card2);

        ArrangeCards();
    }

    private IEnumerator PlayAIPairDisappearEffect(CardObject card1, CardObject card2)
    {
        yield return new WaitForSeconds(0.1f);

        // 消失アニメーション
        card1.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);
        card2.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.18f);

        // DiscardPileに移動
        GameManager.Instance.discardPile.AddCards(card1, card2);
    }

    private void OnAICardHovered(CardObject hoveredCard)
    {
        if (hoveredCard != null && PsychologySystem.Instance != null)
        {
            int cardIndex = cardsInHand.IndexOf(hoveredCard);
            if (cardIndex >= 0)
            {
                PsychologySystem.Instance.OnCardHover(cardIndex);
            }
        }
    }

    private void OnAICardClicked(CardObject clickedCard)
    {
        if (clickedCard != null && GameManager.Instance != null)
        {
            GameManager.Instance.OnCardPointerDown(clickedCard);
        }
    }

    public void EnableCardSelection(bool enabled)
    {
        foreach (var card in cardsInHand)
        {
            card.SetSelectable(enabled);
            if (enabled)
            {
                card.ResetInteractionState();
            }
        }
    }
}
