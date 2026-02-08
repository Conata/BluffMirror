/// <summary>
/// ブラフアクションの種類
/// </summary>
public enum BluffActionType
{
    Shuffle,
    Push,
    Pull,
    Wiggle,
    Spread,
    Close
}

/// <summary>
/// アクション実行者
/// </summary>
public enum BluffActionSource
{
    Player,
    AI
}

/// <summary>
/// ブラフアクションの記録
/// </summary>
[System.Serializable]
public struct BluffActionRecord
{
    public BluffActionType actionType;
    public BluffActionSource source;
    public int targetCardIndex; // -1 = 全体アクション (Shuffle/Spread/Close)
    public float timestamp;
}
