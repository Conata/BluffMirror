using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using FPSTrump.AI.LLM;

namespace FPSTrump.Psychology
{
    /// <summary>
    /// プレイヤー行動分析システム
    /// カード選択時の行動パターンを追跡・分析してBehaviorPatternを生成
    /// </summary>
    public class PlayerBehaviorAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private float behaviorWindowTime = 30f;  // 行動分析の時間窓
        [SerializeField] private int maxBehaviorHistory = 20;     // 保持する行動履歴数

        [Header("Debug Display")]
        [SerializeField] private bool showDebugOverlay = false;   // OnGUIデバッグ表示（デフォルトOFF）

        private Queue<PlayerAction> recentActions = new Queue<PlayerAction>();
        private BehaviorPattern currentBehavior = new BehaviorPattern { positionCounts = new int[3] };
        private int lastSelectedPosition = -1;
        private int consecutiveSamePos = 0;

        // Stage 16: Bluff action tracking
        private Queue<BluffAction> recentBluffActions = new Queue<BluffAction>();
        private Dictionary<BluffActionType, int> bluffActionCounts = new Dictionary<BluffActionType, int>();
        private float lastBluffActionTime = 0f;
        private BluffActionType lastBluffActionType = BluffActionType.Shuffle;

        [Header("Events")]
        public UnityEvent<BehaviorPattern> OnBehaviorAnalyzed;

        /// <summary>
        /// 現在の行動パターンを取得
        /// </summary>
        public BehaviorPattern CurrentBehavior => currentBehavior;

        /// <summary>
        /// プレイヤーのカード選択行動を記録
        /// </summary>
        /// <param name="selectedPosition">選択したカード位置（カードインデックス）</param>
        /// <param name="hoverDuration">ホバー時間（秒）</param>
        /// <param name="decisionTime">決断までの時間（秒）</param>
        /// <param name="totalCards">手札の総枚数（位置正規化用）</param>
        /// <param name="hoverCount">ターン内のホバー回数（迷い度分析用）</param>
        public void RecordPlayerAction(int selectedPosition, float hoverDuration, float decisionTime, int totalCards = 0, int hoverCount = 0)
        {
            // カードインデックスを3バケット（左/中央/右）に正規化
            int normalizedPos;
            if (totalCards <= 1)
            {
                normalizedPos = 1; // Center
            }
            else
            {
                normalizedPos = (int)(selectedPosition * 3f / totalCards);
                normalizedPos = Mathf.Clamp(normalizedPos, 0, 2);
            }

            PlayerAction action = new PlayerAction
            {
                position = normalizedPos,
                hoverDuration = hoverDuration,
                decisionTime = decisionTime,
                timestamp = Time.time,
                hoverCount = hoverCount
            };

            recentActions.Enqueue(action);

            // 古いデータの削除（時間窓外 or 最大履歴超過）
            while (recentActions.Count > 0)
            {
                PlayerAction oldestAction = recentActions.Peek();
                if (Time.time - oldestAction.timestamp > behaviorWindowTime ||
                    recentActions.Count > maxBehaviorHistory)
                {
                    recentActions.Dequeue();
                }
                else
                {
                    break;
                }
            }

            // 行動分析の更新
            AnalyzeBehavior();

            Debug.Log($"[PlayerBehaviorAnalyzer] Recorded action: pos={selectedPosition}, hover={hoverDuration:F2}s, decision={decisionTime:F2}s");
        }

        /// <summary>
        /// 現在の行動パターンを分析
        /// </summary>
        private void AnalyzeBehavior()
        {
            if (recentActions.Count == 0) return;

            // 1回でもアクションがあればホバー・基本分析は実行
            AnalyzeHoverPattern();

            // 位置パターンとテンポは2回以上必要
            if (recentActions.Count >= 2)
            {
                AnalyzePositionPattern();
                AnalyzeTempo();
            }

            CalculateDoubtLevel();

            // Stage 10: 表情データを注入
            UpdateFacialExpressionData();

            // イベント発火
            OnBehaviorAnalyzed?.Invoke(currentBehavior);

            Debug.Log($"[PlayerBehaviorAnalyzer] Analysis complete (actions={recentActions.Count}): doubt={currentBehavior.doubtLevel:F2}, tempo={currentBehavior.tempo}, streak={currentBehavior.streakSamePosition}");
        }

        /// <summary>
        /// 位置選択パターンを分析
        /// </summary>
        private void AnalyzePositionPattern()
        {
            currentBehavior.positionCounts = new int[3]; // Left, Center, Right
            consecutiveSamePos = 1;

            PlayerAction[] actions = recentActions.ToArray();

            // 位置別カウント
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].position >= 0 && actions[i].position < 3)
                {
                    currentBehavior.positionCounts[actions[i].position]++;
                }
            }

            // 連続同位置の検出（最新から遡る）
            for (int i = actions.Length - 1; i > 0; i--)
            {
                if (actions[i].position == actions[i - 1].position)
                {
                    consecutiveSamePos++;
                }
                else
                {
                    break;
                }
            }

            currentBehavior.streakSamePosition = consecutiveSamePos;

            // 位置好みの検出
            int maxCount = currentBehavior.positionCounts.Max();
            int maxIndex = Array.IndexOf(currentBehavior.positionCounts, maxCount);

            if (maxCount > actions.Length / 2) // 50%以上の偏り
            {
                currentBehavior.hasPositionPreference = true;
                currentBehavior.preferredPosition = maxIndex;
            }
            else
            {
                currentBehavior.hasPositionPreference = false;
                currentBehavior.preferredPosition = -1;
            }

            lastSelectedPosition = actions[actions.Length - 1].position;
        }

        /// <summary>
        /// ホバーパターンを分析
        /// </summary>
        private void AnalyzeHoverPattern()
        {
            float totalHoverTime = 0f;
            float totalDecisionTime = 0f;
            int validCount = 0;

            foreach (PlayerAction action in recentActions)
            {
                if (action.hoverDuration > 0.1f) // 有効なホバーのみ
                {
                    totalHoverTime += action.hoverDuration;
                    totalDecisionTime += action.decisionTime;
                    validCount++;
                }
            }

            currentBehavior.avgHoverTime = validCount > 0
                ? totalHoverTime / validCount
                : 0f;

            currentBehavior.avgDecisionTime = validCount > 0
                ? totalDecisionTime / validCount
                : 0f;
        }

        /// <summary>
        /// テンポを分析
        /// </summary>
        private void AnalyzeTempo()
        {
            if (recentActions.Count < 2) return;

            PlayerAction[] actions = recentActions.ToArray();
            List<float> intervals = new List<float>();

            // ターン間隔を計算
            for (int i = 1; i < actions.Length; i++)
            {
                intervals.Add(actions[i].timestamp - actions[i - 1].timestamp);
            }

            float avgInterval = intervals.Average();
            float variance = intervals.Select(x => Mathf.Pow(x - avgInterval, 2)).Average();
            float stdDev = Mathf.Sqrt(variance);

            // テンポ分類
            if (stdDev > avgInterval * 0.5f)
            {
                currentBehavior.tempo = TempoType.Erratic; // 不規則
            }
            else if (avgInterval < 2.0f)
            {
                currentBehavior.tempo = TempoType.Fast; // 速い
            }
            else if (avgInterval > 8.0f)
            {
                currentBehavior.tempo = TempoType.Slow; // 遅い
            }
            else
            {
                currentBehavior.tempo = TempoType.Normal; // 通常
            }
        }

        /// <summary>
        /// 疑念レベルを計算（0-1）
        /// </summary>
        private void CalculateDoubtLevel()
        {
            float doubt = 0f;

            // ホバー時間が長い = 迷い
            if (currentBehavior.avgHoverTime > 3.0f)
            {
                doubt += 0.4f;
            }
            else if (currentBehavior.avgHoverTime > 2.0f)
            {
                doubt += 0.2f;
            }

            // 位置選択の偏り = 迷い（逆説的に、選べない）
            if (currentBehavior.positionCounts.Length >= 3)
            {
                int maxPosCount = currentBehavior.positionCounts.Max();
                int minPosCount = currentBehavior.positionCounts.Min();

                if (maxPosCount - minPosCount > 3)
                {
                    doubt += 0.3f;
                }
            }

            // 不規則テンポ = 迷い
            if (currentBehavior.tempo == TempoType.Erratic)
            {
                doubt += 0.3f;
            }

            // ホバー回数が多い = 迷い（複数カードを行き来）
            if (recentActions.Count > 0)
            {
                float avgHoverCount = (float)recentActions.Average(a => a.hoverCount);
                if (avgHoverCount > 3)
                {
                    doubt += 0.2f;
                }
            }

            currentBehavior.doubtLevel = Mathf.Clamp01(doubt);
        }

        /// <summary>
        /// Stage 10: FacialExpressionAnalyzerから最新の表情データをBehaviorPatternに注入
        /// </summary>
        private void UpdateFacialExpressionData()
        {
            var analyzer = FacialExpressionAnalyzer.Instance;
            if (analyzer == null || !analyzer.IsActive) return;

            var state = analyzer.CurrentState;
            currentBehavior.lastExpression = state.currentExpression;
            currentBehavior.expressionConfidence = state.confidence;
        }

        /// <summary>
        /// 行動履歴をクリア（新しいゲーム用）
        /// </summary>
        public void ClearHistory()
        {
            recentActions.Clear();
            currentBehavior = new BehaviorPattern();
            lastSelectedPosition = -1;
            consecutiveSamePos = 0;

            // Stage 16: Clear bluff history
            recentBluffActions.Clear();
            bluffActionCounts.Clear();
            lastBluffActionTime = 0f;

            Debug.Log("[PlayerBehaviorAnalyzer] History cleared");
        }

        /// <summary>
        /// Stage 16: ブラフアクションを記録
        /// </summary>
        public void RecordBluffAction(BluffActionType actionType, int cardIndex = -1)
        {
            BluffAction action = new BluffAction
            {
                actionType = actionType,
                timestamp = Time.time,
                cardIndex = cardIndex
            };

            recentBluffActions.Enqueue(action);

            // カウント更新
            if (!bluffActionCounts.ContainsKey(actionType))
            {
                bluffActionCounts[actionType] = 0;
            }
            bluffActionCounts[actionType]++;

            // 古いデータの削除（60秒より古いものを削除）
            while (recentBluffActions.Count > 0)
            {
                BluffAction oldestAction = recentBluffActions.Peek();
                if (Time.time - oldestAction.timestamp > 60f)
                {
                    recentBluffActions.Dequeue();
                }
                else
                {
                    break;
                }
            }

            lastBluffActionTime = Time.time;
            lastBluffActionType = actionType;

            Debug.Log($"[PlayerBehaviorAnalyzer] Recorded bluff action: {actionType} (total: {recentBluffActions.Count})");
        }

        /// <summary>
        /// Stage 16: ブラフ行動パターンのサマリーを生成（LLMプロンプト用）
        /// </summary>
        public string GetBluffBehaviorSummary()
        {
            if (recentBluffActions.Count == 0)
            {
                return "No bluff actions yet.";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 総アクション数と頻度
            float timeWindow = 60f; // 過去60秒
            sb.Append($"Bluff actions in last minute: {recentBluffActions.Count}. ");

            // アクション種別ごとの統計
            if (bluffActionCounts.Count > 0)
            {
                var sortedCounts = bluffActionCounts.OrderByDescending(kvp => kvp.Value);
                var mostFrequent = sortedCounts.First();

                if (mostFrequent.Value >= 3)
                {
                    sb.Append($"Frequently using {mostFrequent.Key} ({mostFrequent.Value} times). ");
                }

                // 連続同一アクション検出
                int consecutiveSame = 1;
                BluffAction[] actions = recentBluffActions.ToArray();
                for (int i = actions.Length - 1; i > 0; i--)
                {
                    if (actions[i].actionType == actions[i - 1].actionType)
                    {
                        consecutiveSame++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (consecutiveSame >= 2)
                {
                    sb.Append($"Repeated {lastBluffActionType} {consecutiveSame} times consecutively. ");
                }
            }

            // アクション間隔（落ち着きのなさ）
            if (recentBluffActions.Count >= 3)
            {
                BluffAction[] actions = recentBluffActions.ToArray();
                float totalInterval = actions[actions.Length - 1].timestamp - actions[0].timestamp;
                float avgInterval = totalInterval / (actions.Length - 1);

                if (avgInterval < 5f)
                {
                    sb.Append("Actions are very frequent (restless hands). ");
                }
                else if (avgInterval > 20f)
                {
                    sb.Append("Actions are spaced out (deliberate). ");
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// 統計情報を取得（デバッグ用）
        /// </summary>
        public string GetStatistics()
        {
            if (recentActions == null || recentActions.Count == 0 || currentBehavior == null)
            {
                return "No behavior data yet";
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Recorded Actions: {recentActions.Count}");
            sb.AppendLine($"Doubt Level: {currentBehavior.doubtLevel:F2}");
            sb.AppendLine($"Tempo: {currentBehavior.tempo}");
            sb.AppendLine($"Avg Hover: {currentBehavior.avgHoverTime:F2}s");
            sb.AppendLine($"Streak Same Position: {currentBehavior.streakSamePosition}");

            if (currentBehavior.hasPositionPreference)
            {
                string posName = currentBehavior.preferredPosition == 0 ? "Left"
                              : currentBehavior.preferredPosition == 1 ? "Center"
                              : "Right";
                sb.AppendLine($"Preferred Position: {posName}");
            }

            return sb.ToString();
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグ表示（エディタのみ、Inspector上でshowDebugOverlay=trueで有効化）
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            if (recentActions == null || recentActions.Count == 0) return;

            GUILayout.BeginArea(new Rect(10, 220, 300, 200));
            try
            {
                GUILayout.Label("=== Behavior Analysis ===");
                GUILayout.Label(GetStatistics());
            }
            finally
            {
                GUILayout.EndArea();
            }
        }
#endif
    }

    /// <summary>
    /// プレイヤーの単一行動記録
    /// </summary>
    [Serializable]
    public struct PlayerAction
    {
        public int position;           // カード位置（0-2、正規化済み）
        public float hoverDuration;    // ホバー時間
        public float decisionTime;     // 決断までの時間
        public float timestamp;        // 記録時刻
        public int hoverCount;         // ターン内ホバー回数
    }

    /// <summary>
    /// Stage 16: プレイヤーのブラフアクション記録
    /// </summary>
    [Serializable]
    public struct BluffAction
    {
        public BluffActionType actionType;
        public float timestamp;
        public int cardIndex; // -1 if non-targeted action
    }
}
