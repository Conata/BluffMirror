using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerHandController : HandController
{
    [Header("Player Hand Specific")]
    public float fanAngle = 15f;              // 扇の広がり角度
    public float fanRadius = 0.6f;            // アーク半径
    public float cardTiltTowardCamera = 15f;  // カメラ方向への傾き
    public float maxAnglePerCard = 2.25f;     // 1枚あたりの最大角度（枚数少ない時の広がり制限）

    [Header("Bluff Action Settings")]
    [SerializeField] private float pushPullDistance = 0.08f;
    [SerializeField] private float wiggleAngle = 8f;
    [SerializeField] private float wiggleDuration = 0.5f;
    [SerializeField] private float spreadCloseStep = 5f;
    [SerializeField] private float maxFanAngleModifier = 15f;
    [SerializeField] private float minFanAngleModifier = -10f;

    public override void AddCard(CardObject card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(transform);
        card.isFaceUp = true;
        card.FlipCard(true);

        // Set layer to PlayerCards
        int playerLayer = LayerMask.NameToLayer("PlayerCards");
        if (playerLayer >= 0)
            card.gameObject.layer = playerLayer;
        else
            Debug.LogWarning("[PlayerHandController] Layer 'PlayerCards' not found. Add it in Edit > Project Settings > Tags and Layers.");

        ArrangeCards();

        RaiseOnCardAdded(card);
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
            Vector3 targetLocalPosition = CalculateFanPosition(i, cardsInHand.Count);
            Quaternion targetRotation = CalculateFanRotation(i, cardsInHand.Count);

            cardsInHand[i].transform.DOLocalMove(targetLocalPosition, 0.3f).SetEase(Ease.OutQuart);
            cardsInHand[i].transform.DOLocalRotateQuaternion(targetRotation, 0.3f).SetEase(Ease.OutQuart);

            // Set original local position for hover animation
            cardsInHand[i].SetOriginalPosition(targetLocalPosition);
        }
    }

    private Vector3 CalculateFanPosition(int index, int totalCards)
    {
        if (totalCards <= 1)
            return Vector3.zero;

        // カード枚数に応じてファン角度を調整（少ない時は狭く）
        float effectiveFanAngle = Mathf.Min(fanAngle + currentFanAngleModifier, (totalCards - 1) * maxAnglePerCard);

        float t = (float)index / (totalCards - 1) - 0.5f; // -0.5 ~ 0.5
        float angleRad = t * effectiveFanAngle * Mathf.Deg2Rad;

        // 円弧上の位置（中心が一番高い弧）
        float x = fanRadius * Mathf.Sin(angleRad);
        float y = fanRadius * (Mathf.Cos(angleRad) - 1f); // 中心=0, 端=負
        float z = -index * 0.002f; // 描画順のための微小Z offset

        return new Vector3(x, y, z);
    }

    private Quaternion CalculateFanRotation(int index, int totalCards)
    {
        if (totalCards <= 1)
            return Quaternion.Euler(90f - cardTiltTowardCamera, 0, 0);

        // カード枚数に応じてファン角度を調整
        float effectiveFanAngle = Mathf.Min(fanAngle + currentFanAngleModifier, (totalCards - 1) * maxAnglePerCard);
        float t = (float)index / (totalCards - 1) - 0.5f;
        float zRotation = -t * effectiveFanAngle; // 左カード→右傾き、右カード→左傾き

        return Quaternion.Euler(90f - cardTiltTowardCamera, 0, zRotation);
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

        DOVirtual.DelayedCall(0.35f, () => isBluffAnimating = false);
    }

    public override void PushCard(int cardIndex)
    {
        if (isBluffAnimating || cardIndex < 0 || cardIndex >= cardsInHand.Count) return;
        isBluffAnimating = true;

        CardObject card = cardsInHand[cardIndex];
        Vector3 targetPos = card.transform.localPosition + Vector3.forward * pushPullDistance;

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
        Vector3 targetPos = card.transform.localPosition - Vector3.forward * pushPullDistance;

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
        card.transform.DOShakeRotation(wiggleDuration, new Vector3(0, 0, wiggleAngle), 10, 90f, true, ShakeRandomnessMode.Harmonic)
            .OnComplete(() => isBluffAnimating = false);
    }

    public override void SpreadFan()
    {
        if (isBluffAnimating || cardsInHand.Count <= 1) return;
        isBluffAnimating = true;

        currentFanAngleModifier = Mathf.Clamp(currentFanAngleModifier + spreadCloseStep, minFanAngleModifier, maxFanAngleModifier);
        ArrangeCards();

        DOVirtual.DelayedCall(0.35f, () => isBluffAnimating = false);
    }

    public override void CloseFan()
    {
        if (isBluffAnimating || cardsInHand.Count <= 1) return;
        isBluffAnimating = true;

        currentFanAngleModifier = Mathf.Clamp(currentFanAngleModifier - spreadCloseStep, minFanAngleModifier, maxFanAngleModifier);
        ArrangeCards();

        DOVirtual.DelayedCall(0.35f, () => isBluffAnimating = false);
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
                        RemovePair(i, j);
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

    private void RemovePair(int index1, int index2)
    {
        // アニメーション付きでペア消去
        CardObject card1 = cardsInHand[index1];
        CardObject card2 = cardsInHand[index2];

        // カメラ制御は削除（GameManager が OnPairMatched イベントで処理）

        // 燃える・溶ける演出
        StartCoroutine(PlayPairDisappearEffect(card1, card2));

        // リストから削除
        cardsInHand.RemoveAt(Mathf.Max(index1, index2));
        cardsInHand.RemoveAt(Mathf.Min(index1, index2));

        RaiseOnPairMatched(card1, card2);

        ArrangeCards();
    }

    private IEnumerator PlayPairDisappearEffect(CardObject card1, CardObject card2)
    {
        // CardEffectsManagerを使用してパーティクルエフェクトを再生
        if (CardEffectsManager.Instance != null)
        {
            yield return StartCoroutine(CardEffectsManager.Instance.PlayPairDisappearEffect(card1, card2, () =>
            {
                // エフェクト完了後、DiscardPileに移動
                if (GameManager.Instance != null && GameManager.Instance.discardPile != null)
                {
                    GameManager.Instance.discardPile.AddCards(card1, card2);
                }
            }));
        }
        else
        {
            // フォールバック: CardEffectsManagerが無い場合
            yield return new WaitForSeconds(0.1f);

            // 消失アニメーション
            card1.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);
            card2.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack);

            yield return new WaitForSeconds(0.18f);

            // DiscardPileに移動
            if (GameManager.Instance != null && GameManager.Instance.discardPile != null)
            {
                GameManager.Instance.discardPile.AddCards(card1, card2);
            }
        }
    }
}
