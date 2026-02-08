using System;
using System.Collections.Generic;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;

namespace FPSTrump.Result
{
    /// <summary>
    /// 性格タイプ（6分類）
    /// </summary>
    public enum PersonalityType
    {
        Analyst,    // 冷静な分析者 — 一貫した思考、低迷い度
        Intuitive,  // 直感の決断者 — 高速判断、低ホバー
        Cautious,   // 慎重な観察者 — 長ホバー、高迷い度
        Gambler,    // 衝動の挑戦者 — Erraticテンポ、高変動
        Adapter,    // 柔軟な戦略家 — 位置偏りなし、戦略変化
        Stoic       // 不動の意志 — 圧力下でも安定テンポ
    }

    /// <summary>
    /// ゲームセッション中に蓄積するデータ
    /// </summary>
    [Serializable]
    public class GameSessionData
    {
        // 結果
        public bool playerWon;
        public int totalTurns;
        public float gameDurationSeconds;

        // 行動（PlayerBehaviorAnalyzer由来）
        public float avgDoubtLevel;         // 0-1
        public float avgHoverTime;          // 秒
        public float avgDecisionTime;       // 秒
        public TempoType dominantTempo;
        public bool hadPositionPreference;
        public int longestPositionStreak;
        public int[] totalPositionCounts;   // [左, 中, 右]

        // 心理（PsychologySystem / BluffSystem由来）
        public float peakPressureLevel;     // 0-3
        public float avgPressureLevel;      // 0-3
        public int turningPointCount;
        public int totalReactions;
        public Dictionary<AIEmotion, int> emotionFrequency;
        public float avgReactionIntensity;
        public int pairsFormed;

        // 圧力耐性
        public float tempoVariance;
        public float pressureResponseScore; // 0=崩壊, 1=安定

        // Stage 14: ターン履歴（診断プロンプト用）
        public List<TurnRecord> turnHistory;

        // Stage 15: プレイヤー行動パターン統計
        public Dictionary<string, int> bluffActionFrequency;  // ブラフタイプごとの使用回数
        public int totalBluffActions;                         // 総ブラフ回数
        public float avgBluffsPerTurn;                        // ターンあたりブラフ回数
        public float avgHoverEventsPerTurn;                   // ターンあたりホバー回数
        public float avgBackAndForthPerTurn;                  // ターンあたり往復回数
        public string mostUsedBluffType;                      // 最頻ブラフタイプ

        public GameSessionData()
        {
            totalPositionCounts = new int[3];
            emotionFrequency = new Dictionary<AIEmotion, int>();
            turnHistory = new List<TurnRecord>();
            bluffActionFrequency = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 診断結果
    /// </summary>
    [Serializable]
    public class DiagnosisResult
    {
        public PersonalityType primaryType;
        public PersonalityType secondaryType;
        public string personalityTitle;       // 「冷静な分析者」
        public string personalityDescription; // 行動パターン分析 100-150字
        public string psychologicalTendency;  // 心理傾向 80-120字
        public string behavioralInsight;      // 「あなたは〜な人です」30字以内
        public DiagnosisStats stats;
        public bool isLLMGenerated;

        // Stage 7.5: 種明かし
        public List<BehavioralEvidence> evidences;           // 行動証拠（3-5個）
        public List<ProfileComparison> profileComparisons;   // 生年月日照合（0-5個）
        public bool hasProfileComparison;                    // 生年月日入力済みか
    }

    /// <summary>
    /// 5軸スタッツ（0-1）
    /// </summary>
    [Serializable]
    public class DiagnosisStats
    {
        public float decisiveness;  // 決断力
        public float consistency;   // 一貫性
        public float resilience;    // 耐圧性
        public float intuition;     // 直感力
        public float adaptability;  // 適応力
    }

    /// <summary>
    /// 行動証拠（種明かし用）
    /// </summary>
    [Serializable]
    public class BehavioralEvidence
    {
        public string observation;     // 「あなたの平均決断時間は1.8秒でした」
        public string interpretation;  // 「これは直感的な判断力を示しています」
        public string statConnection;  // どの5軸に関連するか（"decisiveness"等）
        public float impactScore;      // 0-1: この証拠がどれだけ分類に影響したか
    }

    /// <summary>
    /// 生年月日プロファイル照合
    /// </summary>
    [Serializable]
    public class ProfileComparison
    {
        public string traitName;       // 「慎重性」
        public float predicted;        // 四柱推命による予測値
        public float actual;           // 実際の行動値
        public string commentary;      // 「予測通り」or「予想外」
    }

    /// <summary>
    /// ターン単位の記録データ
    /// </summary>
    [Serializable]
    public struct TurnRecord
    {
        public int turnNumber;
        public bool isPlayerTurn;
        public float decisionTime;
        public float hoverTime;
        public int selectedPosition;        // -1 = AI turn
        public float pressureLevelAtTurn;
        public TempoType tempoAtTurn;
        public AIEmotion emotionAfterTurn;
        public float reactionIntensity;
        public bool wasTurningPoint;
        public bool formedPair;

        // Stage 14: 追加データ
        public int playerCardCount;         // このターン時のプレイヤー手札数
        public int aiCardCount;             // このターン時のAI手札数
        public bool aiHeldJoker;            // このターン時AIがJokerを持っていたか

        // Stage 15: プレイヤー行動パターン
        public int bluffActionCount;        // このターンで行ったブラフ回数
        public string dominantBluffType;    // 最も使ったブラフタイプ（Shuffle/Push/Wiggle等）
        public int hoverEventCount;         // カーソルホバー回数
        public float avgHoverDuration;      // 平均ホバー滞在時間（秒）
        public int backAndForthCount;       // カード間を行ったり来たりした回数
    }

    /// <summary>
    /// Stage 15: ホバーパターン詳細
    /// </summary>
    [Serializable]
    public class HoverPattern
    {
        public int cardIndex;               // ホバーされたカード位置
        public float totalHoverTime;        // 累積ホバー時間
        public int hoverCount;              // ホバー回数
        public bool wasSelected;            // 最終的に選ばれたか
    }
}
