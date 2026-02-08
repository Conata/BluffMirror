using System.Collections.Generic;
using UnityEngine;

public class DiscardPile : MonoBehaviour
{
    private List<CardObject> discardedCards = new List<CardObject>();

    public void AddCards(CardObject card1, CardObject card2)
    {
        // ペアとして追加
        card1.gameObject.SetActive(false);
        card2.gameObject.SetActive(false);

        card1.transform.SetParent(transform);
        card2.transform.SetParent(transform);

        discardedCards.Add(card1);
        discardedCards.Add(card2);

        Debug.Log($"Pair discarded: {card1.cardData.rank} and {card2.cardData.rank}. Total discarded: {discardedCards.Count}");
    }

    public void Clear()
    {
        foreach (var card in discardedCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        discardedCards.Clear();
    }

    public int Count => discardedCards.Count;
}
