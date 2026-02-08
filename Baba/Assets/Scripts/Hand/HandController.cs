using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class HandController : MonoBehaviour
{
    [Header("Hand Settings")]
    public Transform[] cardSlots;
    public float cardSpacing = 0.1f;
    public AnimationCurve cardArcCurve;

    protected List<CardObject> cardsInHand = new List<CardObject>();

    // Bluff action state
    protected bool isBluffAnimating = false;
    protected float currentFanAngleModifier = 0f;

    public bool IsBluffAnimating => isBluffAnimating;

    // Events
    public event Action<CardObject> OnCardAdded;
    public event Action<CardObject, CardObject> OnPairMatched;

    public abstract void AddCard(CardObject card);
    public abstract CardObject RemoveCard(int index);
    public abstract void ArrangeCards();

    // Additional methods required by the plan
    public int GetCardCount() => cardsInHand.Count;

    public List<CardObject> GetCards() => new List<CardObject>(cardsInHand);

    public CardObject GetCardAtIndex(int index)
    {
        if (index < 0 || index >= cardsInHand.Count) return null;
        return cardsInHand[index];
    }

    public void ClearHand()
    {
        foreach (var card in cardsInHand)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        cardsInHand.Clear();
    }

    public virtual int CheckForPairs()
    {
        // Base implementation - override in derived classes
        return 0;
    }

    // Protected methods to raise events from derived classes
    protected void RaiseOnCardAdded(CardObject card)
    {
        OnCardAdded?.Invoke(card);
    }

    protected void RaiseOnPairMatched(CardObject card1, CardObject card2)
    {
        OnPairMatched?.Invoke(card1, card2);
    }

    // === Bluff Action Methods (virtual, override in derived classes) ===

    public virtual void ShuffleCards() { }
    public virtual void PushCard(int cardIndex) { }
    public virtual void PullCard(int cardIndex) { }
    public virtual void WiggleCard(int cardIndex) { }
    public virtual void SpreadFan() { }
    public virtual void CloseFan() { }

    public void ResetFanModifier()
    {
        currentFanAngleModifier = 0f;
        ArrangeCards();
    }

    /// <summary>
    /// Fisher-Yates shuffle on cardsInHand list
    /// </summary>
    protected void ShuffleCardList()
    {
        for (int i = cardsInHand.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (cardsInHand[i], cardsInHand[j]) = (cardsInHand[j], cardsInHand[i]);
        }
    }

    /// <summary>
    /// Jokerカードのインデックスを返す (-1 = 未所持)
    /// </summary>
    public int GetJokerIndex()
    {
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (cardsInHand[i].cardData != null && cardsInHand[i].cardData.isJoker)
                return i;
        }
        return -1;
    }

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
