using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// AIセッション状態管理
    /// ゲームセッション通じて全状態を維持し、LLMコンテキスト構築に使用
    /// </summary>
    [Serializable]
    public class AISessionState
    {
        [Header("Personality")]
        public PersonalityProfile playerProfile;
        public AIPersonality aiPersonality;

        [Header("Runtime Behavior Tracking")]
        public BehaviorHistory behaviorHistory;
        public List<DialogueMemory> dialogueMemory = new List<DialogueMemory>();

        [Header("Game State")]
        public GameStateData gameState;

        [Header("Adaptive Parameters")]
        public float currentPressureLevel = 0f;
        public Dictionary<string, float> learnedPatterns = new Dictionary<string, float>();
        public List<string> adaptationNotes = new List<string>();

        [Header("AI Decision History")]
        public List<AIDecisionMemory> decisionHistory = new List<AIDecisionMemory>();

        [Header("Player Appearance (Stage 10)")]
        public PlayerAppearanceData playerAppearance;

        private const int MAX_DIALOGUE_HISTORY = 6;
        private const int MAX_ADAPTATION_NOTES = 10;
        private const int MAX_DECISION_HISTORY = 10;

        /// <summary>
        /// セッション初期化
        /// </summary>
        public void Initialize()
        {
            behaviorHistory = new BehaviorHistory();
            dialogueMemory.Clear();
            gameState = new GameStateData();
            currentPressureLevel = 0f;
            learnedPatterns.Clear();
            adaptationNotes.Clear();

            Debug.Log("[AISessionState] Session initialized");
        }

        /// <summary>
        /// ダイアログを記録
        /// </summary>
        public void RecordDialogue(DialogueCategoryType category, string text)
        {
            dialogueMemory.Add(new DialogueMemory
            {
                category = category,
                text = text,
                timestamp = DateTime.Now,
                turnNumber = gameState.turnNumber
            });

            // 履歴サイズ制限
            if (dialogueMemory.Count > MAX_DIALOGUE_HISTORY)
            {
                dialogueMemory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 行動データを記録
        /// </summary>
        public void RecordBehavior(PlayerAction action)
        {
            behaviorHistory.AddAction(action);
        }

        /// <summary>
        /// 適応メモを追加
        /// </summary>
        public void AddAdaptationNote(string note)
        {
            adaptationNotes.Add($"[Turn {gameState.turnNumber}] {note}");

            if (adaptationNotes.Count > MAX_ADAPTATION_NOTES)
            {
                adaptationNotes.RemoveAt(0);
            }
        }

        /// <summary>
        /// 圧力レベルを更新
        /// </summary>
        public void UpdatePressureLevel(float delta)
        {
            currentPressureLevel = Mathf.Clamp(currentPressureLevel + delta, 0f, 3f);
        }

        /// <summary>
        /// 圧力レベルを自然減衰
        /// </summary>
        public void DecayPressure(float deltaTime)
        {
            currentPressureLevel = Mathf.Max(0f, currentPressureLevel - deltaTime * 0.1f);
        }

        /// <summary>
        /// AI判断結果を記録
        /// </summary>
        public void RecordAIDecision(AIDecisionResult decision)
        {
            if (decision == null) return;

            // gameStateのnullチェック追加
            if (gameState == null)
            {
                Debug.LogWarning("[AISessionState] gameState is null, cannot record decision with turnNumber");
                // gameStateを初期化
                gameState = new GameStateData();
            }

            // 決定履歴に追加
            decisionHistory.Add(new AIDecisionMemory
            {
                selectedCardIndex = decision.selectedCardIndex,
                confidence = decision.confidence,
                strategy = decision.strategy,
                turnNumber = gameState.turnNumber,
                timestamp = DateTime.Now
            });

            // 履歴サイズ制限
            if (decisionHistory.Count > MAX_DECISION_HISTORY)
            {
                decisionHistory.RemoveAt(0);
            }

            // 適応ノートにも記録
            AddAdaptationNote($"AI decided index={decision.selectedCardIndex}, confidence={decision.confidence:F2}, strategy={decision.strategy}");
        }

        /// <summary>
        /// 最近のAI決定を取得
        /// </summary>
        public List<AIDecisionMemory> GetRecentDecisions(int count)
        {
            return decisionHistory.TakeLast(count).ToList();
        }

        /// <summary>
        /// 最近のダイアログを取得
        /// </summary>
        public List<DialogueMemory> GetRecentDialogues(int count)
        {
            return dialogueMemory.TakeLast(count).ToList();
        }
    }

    // ===== データ構造 =====

    [Serializable]
    public class PersonalityProfile
    {
        [Header("Core Traits (0-1)")]
        public float cautiousness;      // 慎重性
        public float intuition;         // 直感性
        public float resilience;        // 回復力
        public float curiosity;         // 好奇心
        public float consistency;       // 一貫性

        [Header("Decision Making")]
        public DecisionStyle primaryDecisionStyle;
        public float confidence;
        public float adaptability;

        [Header("Stress Response")]
        public StressType stressType;
        public float pressureTolerance;
        public float recoverySpeed;

        /// <summary>
        /// 最も顕著な性格特性の日本語名と値を取得
        /// </summary>
        public (string name, float value) GetDominantTrait()
        {
            float max = 0f;
            string trait = null;

            if (cautiousness > max) { max = cautiousness; trait = "慎重"; }
            if (intuition > max) { max = intuition; trait = "直感的"; }
            if (resilience > max) { max = resilience; trait = "冷静"; }
            if (consistency > max) { max = consistency; trait = "一貫性"; }
            if (adaptability > max) { max = adaptability; trait = "適応的"; }

            return max >= 0.4f ? (trait, max) : (null, max);
        }

        public PersonalityProfile Clone()
        {
            return new PersonalityProfile
            {
                cautiousness = this.cautiousness,
                intuition = this.intuition,
                resilience = this.resilience,
                curiosity = this.curiosity,
                consistency = this.consistency,
                primaryDecisionStyle = this.primaryDecisionStyle,
                confidence = this.confidence,
                adaptability = this.adaptability,
                stressType = this.stressType,
                pressureTolerance = this.pressureTolerance,
                recoverySpeed = this.recoverySpeed
            };
        }
    }

    public enum DecisionStyle
    {
        Analytical,     // 分析的
        Intuitive,      // 直感的
        Cautious,       // 慎重
        Aggressive,     // 攻撃的
        Adaptive        // 適応的
    }

    public enum StressType
    {
        Shutdown,       // シャットダウン
        Impulsive,      // 衝動的
        Analytical,     // 分析的
        Avoidant,       // 回避的
        Confrontational // 対決的
    }

    [Serializable]
    public class AIPersonality
    {
        public string name = "The Dealer";
        public float aggression = 0.5f;
        public float intelligence = 0.8f;
        public float patience = 0.6f;
        public float manipulation = 0.7f;
        public string primaryStyle = "Adaptive";
    }

    [Serializable]
    public class BehaviorHistory
    {
        private List<PlayerAction> actions = new List<PlayerAction>();
        private const int MAX_HISTORY = 10;

        public void AddAction(PlayerAction action)
        {
            actions.Add(action);

            if (actions.Count > MAX_HISTORY)
            {
                actions.RemoveAt(0);
            }
        }

        public List<PlayerAction> GetRecentActions(int count)
        {
            return actions.TakeLast(count).ToList();
        }

        public List<PlayerAction> GetAllActions()
        {
            return new List<PlayerAction>(actions);
        }

        public int Count => actions.Count;
    }

    [Serializable]
    public class PlayerAction
    {
        public int selectedPosition;     // 0=左, 1=中央, 2=右
        public float hoverDuration;      // ホバー時間（秒）
        public float decisionTime;       // 決断までの時間
        public DateTime timestamp;
        public int turnNumber;
    }

    [Serializable]
    public class DialogueMemory
    {
        public DialogueCategoryType category;
        public string text;
        public DateTime timestamp;
        public int turnNumber;
    }

    [Serializable]
    public class AIDecisionMemory
    {
        public int selectedCardIndex;
        public float confidence;
        public string strategy;
        public int turnNumber;
        public DateTime timestamp;
    }

    /// <summary>
    /// Stage 10: プレイヤーの外見データ（カメラキャプチャから取得）
    /// </summary>
    [Serializable]
    public class PlayerAppearanceData
    {
        public string complimentText;        // 生成された褒め言葉（AIの第一声用）
        public string appearanceDescription; // 外見の特徴テキスト（LLMプロンプト注入用）
        public bool hasCameraAccess;         // カメラ利用可否
    }

    [Serializable]
    public class GameStateData
    {
        public int turnNumber = 0;
        public int playerCardCount = 0;
        public int aiCardCount = 0;
        public GamePhase currentPhase = GamePhase.Early;
        public bool jokerInPlay = true;

        public void UpdatePhase()
        {
            int totalCards = playerCardCount + aiCardCount;

            if (turnNumber < 5)
                currentPhase = GamePhase.Early;
            else if (totalCards < 8)
                currentPhase = GamePhase.EndGame;
            else
                currentPhase = GamePhase.Mid;
        }
    }

    public enum GamePhase
    {
        Early,
        Mid,
        EndGame
    }
}
