using FPSTrump.AI.LLM;

namespace FPSTrump.Psychology
{
    /// <summary>
    /// AIの期待（ドロー前に決定）
    /// AIはカードの中身を知らない。プレイヤーの行動に対する「期待」を持つだけ。
    /// </summary>
    public enum AIExpectation
    {
        Neutral,    // 特に期待なし（序盤・データ不足）
        Stop,       // 「引くな」（プレイヤーが引かないことを期待）
        Bait        // 「引け」（プレイヤーが引くことを期待）
    }

    /// <summary>
    /// AIの感情（ドロー後に決定）
    /// 「予測 vs 現実のギャップ」から生まれる感情
    /// </summary>
    public enum AIEmotion
    {
        Calm,           // 平静（デフォルト、序盤）
        Anticipating,   // 期待・警戒（ドロー前、期待を持っている）
        Pleased,        // 満足（期待通りの結果）
        Frustrated,     // 苛立ち（期待を裏切られた）
        Hurt,           // 傷つき（信頼を裏切られた感覚）
        Relieved        // 安堵（最悪を免れた）
    }

    /// <summary>
    /// レスポンスレイヤー（3層レイテンシ管理）
    /// </summary>
    public enum ResponseLayer
    {
        A,  // Immediate (0ms): 静的テーブル / キャッシュ済み
        B,  // Short LLM (300-900ms): 1文 最大60文字。感情の理由付け
        C   // Long LLM (ターニングポイントのみ): 100-200文字
    }

    /// <summary>
    /// レスポンス生成リクエスト
    /// LLMに渡すコンテキスト（カード情報は絶対に含めない）
    /// </summary>
    public struct ResponseRequest
    {
        public AIExpectation expectation;       // ドロー前のAIの期待
        public AIEmotion emotion;               // 決定された感情
        public ResponseLayer layer;             // 要求レイヤー
        public float pressureLevel;             // 現在の圧力 (0-3)
        public BehaviorPattern playerBehavior;  // プレイヤー行動パターン
        public int turnCount;                   // 現在のターン数
        public bool isPlayerTurn;               // プレイヤーターンか
        public bool isTurningPoint;             // ターニングポイントか（Layer C判定用）
        public PlayerAppearanceData playerAppearance; // Stage 10: プレイヤー外見データ
        public string playerBluffSummary;       // Stage 16: プレイヤーのブラフ行動サマリー
    }

    /// <summary>
    /// 感情リアクション結果（旧BluffResult）
    /// </summary>
    public struct EmotionalResult
    {
        public AIExpectation expectation;       // AIの期待
        public AIEmotion emotion;               // 結果の感情
        public float reactionIntensity;         // 反応強度 (0-1)
        public string immediateDialogue;        // Layer A: 即座の台詞
        public float pressureDelta;             // 圧力変化量
        public bool isTurningPoint;             // Layer C発動判定
    }

    /// <summary>
    /// ドロー結果コンテキスト（維持: 事実情報として必要）
    /// </summary>
    public struct DrawContext
    {
        public bool isPlayerTurn;               // プレイヤーのターンか
        public bool drawnCardIsJoker;           // 引いたカードがジョーカーか
        public bool formedPair;                 // ペアができたか
        public int remainingCards;              // 引いた側の残りカード数
        public int opponentRemainingCards;      // 相手の残りカード数
        public bool aiHoldsJoker;              // AI手札にジョーカーがあるか
        public string playerBluffSummary;       // Stage 16: プレイヤーのブラフ行動パターン（LLM用）
    }
}
