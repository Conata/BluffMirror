using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FPSTrump.Psychology;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// 感情状態管理システム
    /// AIの感情状態を追跡し、一貫性のある感情遷移を管理
    /// AIEmotion（6感情: Calm/Anticipating/Pleased/Frustrated/Hurt/Relieved）ベース
    ///
    /// 権限: ドロー結果の感情はBluffSystemが正（ForceEmotionalStateで同期される）
    /// このクラスは行動ベースのイベント（PlayerHesitating等）のみ独自判定する
    /// </summary>
    public class EmotionalStateManager
    {
        private AIEmotion currentState = AIEmotion.Calm;
        private Queue<EmotionalTransition> transitionHistory = new Queue<EmotionalTransition>();

        [Range(0f, 1f)]
        private float emotionalInertia = 0.7f; // 感情慣性（急激な変化を抑制）
        private float baseEmotionalInertia = 0.7f;

        private const int MAX_TRANSITION_HISTORY = 5;

        private PersonalityProfile playerProfile;

        /// <summary>
        /// 現在の感情状態を取得
        /// </summary>
        public AIEmotion CurrentState => currentState;

        /// <summary>
        /// プレイヤーのPersonalityProfileを設定し、感情遷移パラメータを調整
        /// </summary>
        public void SetPlayerProfile(PersonalityProfile profile)
        {
            playerProfile = profile;
            if (profile != null)
            {
                // プレイヤーのresilienceが高い → AI感情の慣性を下げる（もっと感情的に揺さぶる）
                // プレイヤーのresilienceが低い → AI感情の慣性を上げる（安定して圧をかける）
                baseEmotionalInertia = Mathf.Lerp(0.5f, 0.85f, 1f - profile.resilience);
                emotionalInertia = baseEmotionalInertia;

                Debug.Log($"[EmotionalStateManager] Profile set: resilience={profile.resilience:F2}, inertia adjusted to {emotionalInertia:F2}");
            }
        }

        /// <summary>
        /// 感情状態を更新
        /// </summary>
        public AIEmotion UpdateEmotionalState(
            GameEvent gameEvent,
            BehaviorPattern playerBehavior,
            AIDecisionResult lastDecision)
        {
            // ターゲット感情を計算
            AIEmotion targetState = CalculateTargetEmotion(
                gameEvent,
                playerBehavior,
                lastDecision
            );

            // 感情慣性を適用（急激な変化を抑制）
            if (targetState != currentState)
            {
                float transitionProbability = CalculateTransitionProbability(
                    currentState,
                    targetState,
                    emotionalInertia
                );

                // 確率的に遷移を決定
                if (UnityEngine.Random.value < transitionProbability)
                {
                    // 遷移を記録
                    RecordTransition(currentState, targetState, gameEvent);

                    // 状態を更新
                    currentState = targetState;

                    Debug.Log($"[EmotionalStateManager] State transition: {currentState} → {targetState} (trigger: {gameEvent})");
                }
                else
                {
                    Debug.Log($"[EmotionalStateManager] Transition suppressed by inertia ({emotionalInertia:F2})");
                }
            }

            return currentState;
        }

        /// <summary>
        /// ターゲット感情を計算
        /// ドロー結果系イベント（AIDrawSuccessful等）はBluffSystem.ForceEmotionalStateで上書きされるため、
        /// ここではフォールバック値のみ返す。行動ベースイベントのみ独自判定。
        /// </summary>
        private AIEmotion CalculateTargetEmotion(
            GameEvent gameEvent,
            BehaviorPattern playerBehavior,
            AIDecisionResult lastDecision)
        {
            switch (gameEvent)
            {
                // === 行動ベースイベント（このクラスが独自判定） ===
                case GameEvent.PlayerHesitating:
                    // 慎重なプレイヤーの迷いには余裕を見せる（Pleased）
                    if (playerProfile != null && playerProfile.cautiousness > 0.6f)
                        return AIEmotion.Pleased;
                    return AIEmotion.Anticipating;

                case GameEvent.PlayerShowingPattern:
                    return AIEmotion.Pleased;

                case GameEvent.GameNearEnd:
                    // 直感的プレイヤーには終盤で警戒（Anticipating → 通常通り）
                    // 一貫性の高いプレイヤーには自信（Pleased）
                    if (playerProfile != null && playerProfile.consistency > 0.7f)
                        return AIEmotion.Pleased;
                    return AIEmotion.Anticipating;

                // === ドロー結果系イベント（BluffSystemが正。フォールバック値） ===
                case GameEvent.AIDrawSuccessful:
                    return AIEmotion.Pleased;

                case GameEvent.AIDrawJoker:
                    // 耐圧性の高いプレイヤー相手だと焦りが増す
                    if (playerProfile != null && playerProfile.resilience > 0.7f)
                        return AIEmotion.Hurt;
                    return AIEmotion.Frustrated;

                case GameEvent.PlayerDrawSuccessful:
                    return (playerBehavior != null && playerBehavior.doubtLevel > 0.6f)
                        ? AIEmotion.Hurt
                        : AIEmotion.Frustrated;

                case GameEvent.PlayerDrawJoker:
                    return AIEmotion.Relieved;

                case GameEvent.PairMatched:
                    return AIEmotion.Calm;

                case GameEvent.TurnStart:
                default:
                    if (lastDecision != null && lastDecision.confidence > 0.7f)
                        return AIEmotion.Pleased;
                    else
                        return AIEmotion.Calm;
            }
        }

        /// <summary>
        /// 遷移確率を計算
        /// </summary>
        private float CalculateTransitionProbability(
            AIEmotion from,
            AIEmotion to,
            float inertia)
        {
            // 基本遷移確率
            float baseProbability = 1.0f - inertia;

            // 遷移の自然さに基づく調整
            float naturalness = GetTransitionNaturalness(from, to);

            // 最終確率
            return Mathf.Clamp01(baseProbability * naturalness);
        }

        /// <summary>
        /// 遷移の自然さを取得（0-1）
        /// </summary>
        private float GetTransitionNaturalness(AIEmotion from, AIEmotion to)
        {
            // 同じ状態への遷移は最も自然
            if (from == to)
                return 1.0f;

            // 自然な遷移パス定義（6感情版）
            var naturalTransitions = new Dictionary<AIEmotion, AIEmotion[]>
            {
                [AIEmotion.Calm] = new[] {
                    AIEmotion.Anticipating,
                    AIEmotion.Pleased
                },
                [AIEmotion.Anticipating] = new[] {
                    AIEmotion.Pleased,
                    AIEmotion.Frustrated,
                    AIEmotion.Hurt,
                    AIEmotion.Relieved
                },
                [AIEmotion.Pleased] = new[] {
                    AIEmotion.Calm,
                    AIEmotion.Anticipating
                },
                [AIEmotion.Frustrated] = new[] {
                    AIEmotion.Calm,
                    AIEmotion.Hurt,
                    AIEmotion.Anticipating
                },
                [AIEmotion.Hurt] = new[] {
                    AIEmotion.Frustrated,
                    AIEmotion.Calm
                },
                [AIEmotion.Relieved] = new[] {
                    AIEmotion.Calm,
                    AIEmotion.Pleased
                }
            };

            // 自然な遷移かチェック
            if (naturalTransitions.TryGetValue(from, out var validTargets))
            {
                if (validTargets.Contains(to))
                    return 1.0f;  // 自然な遷移
            }

            return 0.3f;  // 不自然な遷移（低確率）
        }

        /// <summary>
        /// 遷移を記録
        /// </summary>
        private void RecordTransition(AIEmotion from, AIEmotion to, GameEvent trigger)
        {
            transitionHistory.Enqueue(new EmotionalTransition
            {
                from = from,
                to = to,
                trigger = trigger,
                timestamp = Time.time
            });

            // 履歴サイズ制限
            if (transitionHistory.Count > MAX_TRANSITION_HISTORY)
            {
                transitionHistory.Dequeue();
            }
        }

        /// <summary>
        /// LLMコンテキスト用の感情履歴を取得
        /// </summary>
        public string GetEmotionalContext()
        {
            if (transitionHistory.Count == 0)
            {
                return $"Current emotional state: {currentState} (no transitions yet)";
            }

            var recent = transitionHistory.TakeLast(3);
            System.Text.StringBuilder context = new System.Text.StringBuilder();

            context.AppendLine($"Current emotional state: {currentState}");
            context.AppendLine("Recent emotional transitions:");

            foreach (var transition in recent)
            {
                context.AppendLine($"- {transition.from} → {transition.to} (trigger: {transition.trigger})");
            }

            return context.ToString();
        }

        /// <summary>
        /// 感情慣性を設定（0-1）
        /// </summary>
        public void SetEmotionalInertia(float value)
        {
            emotionalInertia = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 強制的に感情状態を変更（演出用）
        /// </summary>
        public void ForceEmotionalState(AIEmotion state, GameEvent trigger)
        {
            if (currentState != state)
            {
                RecordTransition(currentState, state, trigger);
                currentState = state;
                Debug.Log($"[EmotionalStateManager] Forced state change: {state}");
            }
        }

        /// <summary>
        /// 感情状態をリセット
        /// </summary>
        public void Reset()
        {
            currentState = AIEmotion.Calm;
            transitionHistory.Clear();
            Debug.Log("[EmotionalStateManager] Reset to Calm");
        }
    }

    // ===== データ構造 =====

    [Serializable]
    public class EmotionalTransition
    {
        public AIEmotion from;
        public AIEmotion to;
        public GameEvent trigger;
        public float timestamp;
    }

    public enum GameEvent
    {
        TurnStart,              // ターン開始
        AIDrawSuccessful,       // AI有利なドロー
        AIDrawJoker,            // AIがジョーカーをドロー
        PlayerDrawSuccessful,   // プレイヤー有利なドロー
        PlayerDrawJoker,        // プレイヤーがジョーカーをドロー
        PlayerHesitating,       // プレイヤーが迷っている
        PlayerShowingPattern,   // プレイヤーのパターンが見えた
        PairMatched,            // ペアが揃った
        GameNearEnd             // ゲーム終盤
    }

    /// <summary>
    /// CoTメンタリスト推理の1ステップ
    /// </summary>
    [Serializable]
    public class CoTStep
    {
        [Newtonsoft.Json.JsonProperty("card")]
        public int cardIndex;           // 注目するカードインデックス (0-based)
        public string thought;          // メンタリスト推理台詞
    }

    [Serializable]
    public class AIDecisionResult
    {
        public int selectedCardIndex;
        public float confidence;        // 0-1
        public string strategy;         // "Aggressive", "Cautious", etc.
        public List<CoTStep> cotSteps;  // CoT推理ステップ (null可)
    }
}
