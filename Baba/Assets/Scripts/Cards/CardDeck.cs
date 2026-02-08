using System.Collections.Generic;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [Header("Deck Settings")]
    public CardObject cardPrefab;
    public int jokerCount = 1;

    private List<CardObject> deck = new List<CardObject>();

    public void Initialize()
    {
        Debug.Log("[CardDeck] Initialize() called");

        // Clear existing deck
        ClearDeck();

        if (cardPrefab == null)
        {
            Debug.LogError("[CardDeck] cardPrefab is NULL! Cannot create cards.");
            return;
        }

        Debug.Log($"[CardDeck] Creating standard 52 cards + {jokerCount} joker(s)");

        // Generate 52 cards (13 ranks x 4 suits)
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                CardObject cardObj = Instantiate(cardPrefab, transform.position, Quaternion.identity, transform);
                cardObj.cardData = new Card
                {
                    suit = suit,
                    rank = rank,
                    isJoker = false,
                    frontTexture = null,  // Phase 1: テクスチャは後で追加
                    backTexture = null
                };
                deck.Add(cardObj);
            }
        }

        // Add Joker(s)
        for (int i = 0; i < jokerCount; i++)
        {
            CardObject jokerObj = Instantiate(cardPrefab, transform.position, Quaternion.identity, transform);
            jokerObj.cardData = new Card
            {
                suit = CardSuit.Hearts,  // ジョーカーのスートは無関係
                rank = CardRank.Ace,     // ジョーカーのランクは無関係
                isJoker = true,
                frontTexture = null,
                backTexture = null
            };
            deck.Add(jokerObj);
        }

        // Shuffle the deck
        ShuffleDeck();

        Debug.Log($"CardDeck initialized with {deck.Count} cards");
    }

    public CardObject DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.LogWarning("CardDeck is empty!");
            return null;
        }

        CardObject drawnCard = deck[0];
        deck.RemoveAt(0);

        return drawnCard;
    }

    public void ShuffleDeck()
    {
        // Fisher-Yates shuffle algorithm
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            CardObject temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public void ClearDeck()
    {
        foreach (var card in deck)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        deck.Clear();
    }

    public int RemainingCards => deck.Count;
}
