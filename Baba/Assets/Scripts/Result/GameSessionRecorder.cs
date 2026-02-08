using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FPSTrump.Psychology;
using FPSTrump.AI.LLM;

namespace FPSTrump.Result
{
    /// <summary>
    /// ゲームセッション中のイベントを記録し、最終的にGameSessionDataを生成する
    /// </summary>
    public class GameSessionRecorder : MonoBehaviour
    {
        public static GameSessionRecorder Instance { get; private set; }

        private List<TurnRecord> turnRecords = new List<TurnRecord>();
        private List<float> pressureSamples = new List<float>();
        private List<TempoType> tempoSamples = new List<TempoType>();

        private float sessionStartTime;
        private int pairsFormedCount;
        private float peakPressure;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// セッション開始（NewGameSequenceから呼ぶ）
        /// </summary>
        public void StartSession()
        {
            turnRecords.Clear();
            pressureSamples.Clear();
            tempoSamples.Clear();
            pairsFormedCount = 0;
            peakPressure = 0f;
            sessionStartTime = Time.time;

            Debug.Log("[GameSessionRecorder] Session started");
        }

        /// <summary>
        /// ターン記録を追加
        /// </summary>
        public void RecordTurn(TurnRecord record)
        {
            turnRecords.Add(record);

            // 圧力ピーク更新
            if (record.pressureLevelAtTurn > peakPressure)
                peakPressure = record.pressureLevelAtTurn;

            // サンプリング
            pressureSamples.Add(record.pressureLevelAtTurn);
            tempoSamples.Add(record.tempoAtTurn);

            Debug.Log($"[GameSessionRecorder] Turn {record.turnNumber} recorded " +
                      $"(player={record.isPlayerTurn}, decision={record.decisionTime:F2}s, " +
                      $"pressure={record.pressureLevelAtTurn:F2})");
        }

        /// <summary>
        /// ペア成立を記録
        /// </summary>
        public void RecordPairFormed()
        {
            pairsFormedCount++;
        }

        /// <summary>
        /// Stage 15.5: 最近のターン記録を取得（メンタリスト用）
        /// </summary>
        public List<TurnRecord> GetRecentTurns(int count = 5, bool playerOnly = true)
        {
            if (turnRecords.Count == 0) return new List<TurnRecord>();

            var source = playerOnly
                ? turnRecords.Where(t => t.isPlayerTurn).ToList()
                : turnRecords;

            return source.TakeLast(count).ToList();
        }

        /// <summary>
        /// セッション終了、GameSessionDataを生成
        /// </summary>
        public GameSessionData FinalizeSession(bool playerWon)
        {
            GameSessionData data = new GameSessionData();

            // 結果
            data.playerWon = playerWon;
            data.totalTurns = turnRecords.Count;
            data.gameDurationSeconds = Time.time - sessionStartTime;
            data.pairsFormed = pairsFormedCount;

            // プレイヤーターンのみ抽出
            var playerTurns = turnRecords.Where(t => t.isPlayerTurn).ToList();

            if (playerTurns.Count > 0)
            {
                // 行動データ
                data.avgDoubtLevel = CalculateAvgDoubt(playerTurns);
                data.avgHoverTime = playerTurns.Average(t => t.hoverTime);
                data.avgDecisionTime = playerTurns.Average(t => t.decisionTime);
                data.dominantTempo = GetDominantTempo();
                CalculatePositionStats(playerTurns, data);

                // 圧力耐性
                data.tempoVariance = CalculateTempoVariance(playerTurns);
                data.pressureResponseScore = CalculatePressureResponse(playerTurns);
            }

            // 心理データ（全ターン）
            data.peakPressureLevel = peakPressure;
            data.avgPressureLevel = pressureSamples.Count > 0
                ? pressureSamples.Average()
                : 0f;
            data.turningPointCount = turnRecords.Count(t => t.wasTurningPoint);
            data.totalReactions = turnRecords.Count;
            data.avgReactionIntensity = turnRecords.Count > 0
                ? turnRecords.Average(t => t.reactionIntensity)
                : 0f;

            // 感情頻度
            data.emotionFrequency = new Dictionary<AIEmotion, int>();
            foreach (var turn in turnRecords)
            {
                if (data.emotionFrequency.ContainsKey(turn.emotionAfterTurn))
                    data.emotionFrequency[turn.emotionAfterTurn]++;
                else
                    data.emotionFrequency[turn.emotionAfterTurn] = 1;
            }

            // Stage 14: ターン履歴をコピー（診断プロンプト用）
            data.turnHistory = new List<TurnRecord>(turnRecords);

            // Stage 15: ブラフ・ホバーパターン統計
            CalculateBluffStats(playerTurns, data);
            CalculateHoverStats(playerTurns, data);

            Debug.Log($"[GameSessionRecorder] Session finalized: " +
                      $"turns={data.totalTurns}, duration={data.gameDurationSeconds:F1}s, " +
                      $"playerWon={data.playerWon}, pairs={data.pairsFormed}, " +
                      $"bluffs={data.totalBluffActions}, avgHovers={data.avgHoverEventsPerTurn:F1}");

            return data;
        }

        // ========================================
        // 内部計算
        // ========================================

        /// <summary>
        /// 平均迷い度（ホバー時間と決断時間から推定）
        /// </summary>
        private float CalculateAvgDoubt(List<TurnRecord> playerTurns)
        {
            float totalDoubt = 0f;
            foreach (var turn in playerTurns)
            {
                float doubt = 0f;
                if (turn.hoverTime > 3f) doubt += 0.4f;
                else if (turn.hoverTime > 2f) doubt += 0.2f;
                if (turn.decisionTime > 5f) doubt += 0.3f;
                totalDoubt += Mathf.Clamp01(doubt);
            }
            return totalDoubt / playerTurns.Count;
        }

        /// <summary>
        /// 支配的なテンポを取得
        /// </summary>
        private TempoType GetDominantTempo()
        {
            if (tempoSamples.Count == 0) return TempoType.Normal;
            return tempoSamples
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }

        /// <summary>
        /// 位置統計を計算
        /// </summary>
        private void CalculatePositionStats(List<TurnRecord> playerTurns, GameSessionData data)
        {
            data.totalPositionCounts = new int[3];
            int longestStreak = 0;
            int currentStreak = 1;
            int lastPos = -1;

            foreach (var turn in playerTurns)
            {
                int pos = Mathf.Clamp(turn.selectedPosition, 0, 2);
                data.totalPositionCounts[pos]++;

                if (pos == lastPos)
                {
                    currentStreak++;
                    if (currentStreak > longestStreak) longestStreak = currentStreak;
                }
                else
                {
                    currentStreak = 1;
                }
                lastPos = pos;
            }

            data.longestPositionStreak = longestStreak;

            // 50%以上の偏りがあれば位置偏好あり
            int maxCount = data.totalPositionCounts.Max();
            data.hadPositionPreference = playerTurns.Count > 2 && maxCount > playerTurns.Count / 2;
        }

        /// <summary>
        /// テンポ分散を計算
        /// </summary>
        private float CalculateTempoVariance(List<TurnRecord> playerTurns)
        {
            if (playerTurns.Count < 2) return 0f;

            float avgDecision = playerTurns.Average(t => t.decisionTime);
            float variance = playerTurns
                .Select(t => Mathf.Pow(t.decisionTime - avgDecision, 2))
                .Average();
            return Mathf.Sqrt(variance);
        }

        /// <summary>
        /// 圧力耐性スコア（0=崩壊, 1=安定）
        /// 圧力が上がってもテンポが変わらない → 高スコア
        /// </summary>
        private float CalculatePressureResponse(List<TurnRecord> playerTurns)
        {
            if (playerTurns.Count < 3) return 0.5f;

            // 低圧力時と高圧力時のテンポを比較
            var lowPressureTurns = playerTurns.Where(t => t.pressureLevelAtTurn < 1.0f).ToList();
            var highPressureTurns = playerTurns.Where(t => t.pressureLevelAtTurn >= 1.5f).ToList();

            if (lowPressureTurns.Count == 0 || highPressureTurns.Count == 0)
                return 0.5f; // データ不足

            float lowAvgDecision = lowPressureTurns.Average(t => t.decisionTime);
            float highAvgDecision = highPressureTurns.Average(t => t.decisionTime);

            // テンポ差が小さいほど耐性が高い
            float tempoShift = Mathf.Abs(highAvgDecision - lowAvgDecision);
            float score = Mathf.Clamp01(1f - tempoShift / 5f);

            return score;
        }

        /// <summary>
        /// Stage 15: ブラフアクション統計を計算
        /// </summary>
        private void CalculateBluffStats(List<TurnRecord> playerTurns, GameSessionData data)
        {
            if (playerTurns.Count == 0) return;

            // 総ブラフ回数
            data.totalBluffActions = playerTurns.Sum(t => t.bluffActionCount);

            // ターンあたりブラフ回数
            data.avgBluffsPerTurn = (float)data.totalBluffActions / playerTurns.Count;

            // ブラフタイプ別頻度
            data.bluffActionFrequency = new Dictionary<string, int>();
            foreach (var turn in playerTurns)
            {
                if (!string.IsNullOrEmpty(turn.dominantBluffType))
                {
                    if (!data.bluffActionFrequency.ContainsKey(turn.dominantBluffType))
                        data.bluffActionFrequency[turn.dominantBluffType] = 0;
                    data.bluffActionFrequency[turn.dominantBluffType] += turn.bluffActionCount;
                }
            }

            // 最頻ブラフタイプ
            if (data.bluffActionFrequency.Count > 0)
            {
                data.mostUsedBluffType = data.bluffActionFrequency
                    .OrderByDescending(kvp => kvp.Value)
                    .First()
                    .Key;
            }
            else
            {
                data.mostUsedBluffType = "";
            }
        }

        /// <summary>
        /// Stage 15: ホバーパターン統計を計算
        /// </summary>
        private void CalculateHoverStats(List<TurnRecord> playerTurns, GameSessionData data)
        {
            if (playerTurns.Count == 0) return;

            // ターンあたりホバー回数
            data.avgHoverEventsPerTurn = (float)playerTurns.Average(t => t.hoverEventCount);

            // ターンあたり往復回数（将来実装）
            data.avgBackAndForthPerTurn = (float)playerTurns.Average(t => t.backAndForthCount);
        }
    }
}
