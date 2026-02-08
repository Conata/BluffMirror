using UnityEngine;

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
