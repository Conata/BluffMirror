using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// 3層フォールバックシステム
    /// Tier 1: LLM → Tier 2: ルールベース → Tier 3: 静的データベース
    /// </summary>
    public class FallbackManager
    {
        private RuleBasedDialogueGenerator ruleBasedGenerator;
        private StaticDialogueDatabase staticDatabase;

        public FallbackManager()
        {
            ruleBasedGenerator = new RuleBasedDialogueGenerator();
            staticDatabase = new StaticDialogueDatabase();
        }

        /// <summary>
        /// 言語変更時にテンプレートとデータベースを再読込
        /// </summary>
        public void ReloadAll()
        {
            ruleBasedGenerator.ReloadTemplates();
            staticDatabase.ReloadDatabase();
            Debug.Log("[FallbackManager] All templates and database reloaded for language change");
        }

        /// <summary>
        /// PersonalityProfileを設定（セッション開始時に呼び出し）
        /// </summary>
        public void SetPlayerProfile(PersonalityProfile profile)
        {
            ruleBasedGenerator.SetPlayerProfile(profile);
        }

        /// <summary>
        /// 3層フォールバックでダイアログを取得
        /// </summary>
        public async Task<string> GetDialogueWithFallback(
            Func<Task<string>> llmGenerateFunc,
            DialogueCategoryType category,
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int timeoutMs = 2000)
        {
            // Tier 1: LLM呼び出し（タイムアウト付き）
            try
            {
                Debug.Log("[FallbackManager] Tier 1: Attempting LLM generation...");

                var llmTask = llmGenerateFunc();
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    string result = await llmTask;
                    if (!string.IsNullOrEmpty(result))
                    {
                        Debug.Log("[FallbackManager] Tier 1 SUCCESS");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[FallbackManager] Tier 1 TIMEOUT ({timeoutMs}ms), falling back to Tier 2");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FallbackManager] Tier 1 ERROR: {ex.Message}, falling back to Tier 2");
            }

            // Tier 2: ルールベース生成
            try
            {
                Debug.Log("[FallbackManager] Tier 2: Attempting rule-based generation...");

                string ruleBasedDialogue = ruleBasedGenerator.Generate(
                    category,
                    behaviorPattern,
                    pressureLevel
                );

                if (!string.IsNullOrEmpty(ruleBasedDialogue))
                {
                    Debug.Log("[FallbackManager] Tier 2 SUCCESS");
                    return ruleBasedDialogue;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FallbackManager] Tier 2 ERROR: {ex.Message}, falling back to Tier 3");
            }

            // Tier 3: 静的データベース
            Debug.Log("[FallbackManager] Tier 3: Using static database...");
            string staticDialogue = staticDatabase.GetDialogue(category, pressureLevel);
            Debug.Log("[FallbackManager] Tier 3 SUCCESS (fallback)");
            return staticDialogue;
        }

        /// <summary>
        /// 3層フォールバックでAI判断を取得
        /// </summary>
        public async Task<AIDecisionResult> GetAIDecisionWithFallback(
            Func<Task<AIDecisionResult>> llmGenerateFunc,
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int playerCardCount,
            int timeoutMs = 2000)
        {
            // 入力バリデーション追加
            if (playerCardCount <= 0)
            {
                Debug.LogError($"[FallbackManager] Invalid playerCardCount: {playerCardCount}");
                return new AIDecisionResult
                {
                    selectedCardIndex = 0,
                    confidence = 0.0f,
                    strategy = "Invalid"
                };
            }

            // Tier 1: LLM呼び出し（タイムアウト付き）
            try
            {
                Debug.Log("[FallbackManager] Tier 1: Attempting LLM decision generation...");

                var llmTask = llmGenerateFunc();
                var timeoutTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(llmTask, timeoutTask);

                if (completedTask == llmTask)
                {
                    AIDecisionResult result = await llmTask;
                    if (result != null)
                    {
                        Debug.Log("[FallbackManager] Tier 1 Decision SUCCESS");
                        return result;
                    }
                }
                else
                {
                    Debug.LogWarning($"[FallbackManager] Tier 1 Decision TIMEOUT ({timeoutMs}ms)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FallbackManager] Tier 1 Decision ERROR: {ex.Message}");
            }

            // Tier 2: ルールベース判断生成
            try
            {
                Debug.Log("[FallbackManager] Tier 2: Attempting rule-based decision...");

                AIDecisionResult ruleBasedDecision = GenerateRuleBasedDecision(
                    behaviorPattern,
                    pressureLevel,
                    playerCardCount
                );

                if (ruleBasedDecision != null)
                {
                    Debug.Log($"[FallbackManager] Tier 2 SUCCESS - position: {ruleBasedDecision.selectedCardIndex}, confidence: {ruleBasedDecision.confidence:F2}");
                    return ruleBasedDecision;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FallbackManager] Tier 2 ERROR: {ex.Message}, falling back to Tier 3");
            }

            // Tier 3: ランダム選択
            Debug.Log("[FallbackManager] Tier 3: Using random selection...");
            AIDecisionResult randomDecision = new AIDecisionResult
            {
                selectedCardIndex = UnityEngine.Random.Range(0, playerCardCount),
                confidence = 0.3f,
                strategy = "Random"
            };
            Debug.Log($"[FallbackManager] Tier 3 SUCCESS (fallback) - position: {randomDecision.selectedCardIndex}");
            return randomDecision;
        }

        /// <summary>
        /// ルールベース判断生成（Tier 2）
        /// </summary>
        public AIDecisionResult GenerateRuleBasedDecision(
            BehaviorPattern pattern,
            float pressureLevel,
            int playerCardCount)
        {
            int selectedPosition = -1;
            float confidence = 0.6f;
            string strategy = "Cautious";

            // 戦略1: プレイヤーの位置好みを避ける
            if (pattern.hasPositionPreference &&
                pattern.preferredPosition >= 0 &&
                pattern.preferredPosition < playerCardCount &&
                pattern.streakSamePosition >= 2)
            {
                // 好みの位置以外からランダム選択
                List<int> availablePositions = new List<int>();
                for (int i = 0; i < playerCardCount; i++)
                {
                    if (i != pattern.preferredPosition)
                    {
                        availablePositions.Add(i);
                    }
                }

                if (availablePositions.Count > 0)
                {
                    selectedPosition = availablePositions[UnityEngine.Random.Range(0, availablePositions.Count)];
                    confidence = 0.7f;
                    strategy = "Adaptive";
                    Debug.Log($"[FallbackManager] Rule-based: Avoiding preferred position {pattern.preferredPosition}, selected {selectedPosition}");
                }
            }

            // 戦略2: 高疑念時は中央カード選択（プレイヤーが保護している可能性）
            if (selectedPosition == -1 && pattern.doubtLevel > 0.7f && playerCardCount >= 2)
            {
                selectedPosition = playerCardCount / 2;
                confidence = 0.65f;
                strategy = "Aggressive";
                Debug.Log($"[FallbackManager] Rule-based: High doubt detected, targeting center position {selectedPosition}");
            }

            // 戦略3: 高圧力時は端のカードを選択（リスク回避）
            if (selectedPosition == -1 && pressureLevel > 2.0f && playerCardCount >= 2)
            {
                selectedPosition = UnityEngine.Random.Range(0, 2) == 0 ? 0 : playerCardCount - 1;
                confidence = 0.55f;
                strategy = "Cautious";
                Debug.Log($"[FallbackManager] Rule-based: High pressure, selecting edge position {selectedPosition}");
            }

            // デフォルト: ランダム選択
            if (selectedPosition == -1)
            {
                selectedPosition = UnityEngine.Random.Range(0, playerCardCount);
                confidence = 0.5f;
                strategy = "Adaptive";
                Debug.Log($"[FallbackManager] Rule-based: Default random selection {selectedPosition}");
            }

            return new AIDecisionResult
            {
                selectedCardIndex = selectedPosition,
                confidence = confidence,
                strategy = strategy
            };
        }

        /// <summary>
        /// フォールバックCoTステップ生成（RuleBasedDialogueGeneratorへの委譲）
        /// </summary>
        public List<CoTStep> GenerateFallbackCoTSteps(
            AIDecisionResult decision,
            BehaviorPattern behaviorPattern,
            float pressureLevel,
            int playerCardCount)
        {
            return ruleBasedGenerator.GenerateFallbackCoTSteps(
                decision, behaviorPattern, pressureLevel, playerCardCount);
        }
    }

    // ===== ルールベースダイアログ生成器 =====

    public class RuleBasedDialogueGenerator
    {
        private Dictionary<string, string[]> templates;
        private Dictionary<string, int> usageCount;
        private PersonalityProfile playerProfile;

        // 事前生成された性格読みセリフ
        private List<string> personalityReadLines = new List<string>();
        private int personalityReadIndex = 0;

        public RuleBasedDialogueGenerator()
        {
            usageCount = new Dictionary<string, int>();
            templates = new Dictionary<string, string[]>();
        }

        private bool templatesLoaded = false;

        private void EnsureTemplatesLoaded()
        {
            if (!templatesLoaded && LocalizationManager.Instance != null)
            {
                InitializeTemplates();
                templatesLoaded = true;
            }
        }

        /// <summary>
        /// 言語変更時にテンプレートを再読込
        /// </summary>
        public void ReloadTemplates()
        {
            usageCount.Clear();
            InitializeTemplates();
            templatesLoaded = true;
            Debug.Log("[RuleBasedDialogueGenerator] Templates reloaded for language change");
        }

        /// <summary>
        /// プレイヤーのPersonalityProfileを設定
        /// </summary>
        public void SetPlayerProfile(PersonalityProfile profile)
        {
            playerProfile = profile;
        }

        /// <summary>
        /// 事前生成された性格読みセリフを設定
        /// </summary>
        public void SetPersonalityReadLines(List<string> lines)
        {
            personalityReadLines = lines ?? new List<string>();
            personalityReadIndex = 0;
            Debug.Log($"[RuleBasedDialogueGenerator] Personality read lines set: {personalityReadLines.Count}");
        }

        /// <summary>
        /// 重み付きランダムでテンプレートを選択
        /// 使用回数が少ないほど選ばれやすい（完全に決定的ではない）
        /// </summary>
        private string SelectWeightedRandom(string[] options)
        {
            if (options.Length == 0) return null;
            if (options.Length == 1)
            {
                string only = options[0];
                if (!usageCount.ContainsKey(only)) usageCount[only] = 0;
                usageCount[only]++;
                return only;
            }

            // 重み計算: weight = 1.0 / (usageCount + 1)
            float totalWeight = 0f;
            float[] weights = new float[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int count = usageCount.GetValueOrDefault(options[i], 0);
                weights[i] = 1.0f / (count + 1);
                totalWeight += weights[i];
            }

            // 重み付きランダム選択
            float random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < options.Length; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    string selected = options[i];
                    if (!usageCount.ContainsKey(selected)) usageCount[selected] = 0;
                    usageCount[selected]++;
                    return selected;
                }
            }

            // フォールバック
            string last = options[options.Length - 1];
            if (!usageCount.ContainsKey(last)) usageCount[last] = 0;
            usageCount[last]++;
            return last;
        }

        private void InitializeTemplates()
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                templates = loc.GetArrayDictionary("fallback.");
                Debug.Log($"[RuleBasedDialogueGenerator] Loaded {templates.Count} templates from LocalizationManager");
            }
            else
            {
                templates = new Dictionary<string, string[]>();
                Debug.LogWarning("[RuleBasedDialogueGenerator] LocalizationManager not available, using empty templates");
            }
        }

        public string Generate(
            DialogueCategoryType category,
            BehaviorPattern pattern,
            float pressureLevel)
        {
            EnsureTemplatesLoaded();

            // 25%の確率でPersonalityProfile対応セリフを使用
            if (playerProfile != null && UnityEngine.Random.value < 0.25f)
            {
                string personalityKey = GetPersonalityTemplateKey(playerProfile);
                if (personalityKey != null && templates.ContainsKey(personalityKey))
                {
                    string[] pOptions = templates[personalityKey];
                    return SelectWeightedRandom(pOptions);
                }
            }

            string templateKey = DetermineTemplateKey(category, pattern, pressureLevel);

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                string selected = SelectWeightedRandom(options);

                // パターン置換（{position}など）
                selected = ApplyPatternReplacement(selected, pattern);

                return selected;
            }

            // フォールバック: カテゴリデフォルト
            return GetCategoryDefault(category);
        }

        private string DetermineTemplateKey(
            DialogueCategoryType category,
            BehaviorPattern pattern,
            float pressureLevel)
        {
            switch (category)
            {
                case DialogueCategoryType.Stop:
                    if (pattern.doubtLevel > 0.7f)
                        return "stop_high_doubt";
                    else if (pressureLevel > 1.5f)
                        return "stop_medium";
                    else
                        return "stop_low";

                case DialogueCategoryType.Bait:
                    if (pattern.tempo == TempoType.Fast)
                        return "bait_fast_tempo";
                    else if (pressureLevel > 2.0f)
                        return "bait_high_pressure";
                    else
                        return "bait_medium";

                case DialogueCategoryType.Mirror:
                    if (pattern.streakSamePosition >= 2)
                        return "mirror_pattern_detected";
                    else if (pattern.hasPositionPreference)
                        return "mirror_position_preference";
                    else
                        return "mirror_pattern_detected";

                case DialogueCategoryType.Hesitation:
                    // Hesitation は専用メソッドを使用
                    if (pressureLevel < 1.0f)
                        return "hesitation_low_pressure";
                    else if (pressureLevel < 2.0f)
                        return "hesitation_medium_pressure";
                    else
                        return "hesitation_high_pressure";

                case DialogueCategoryType.General:
                default:
                    // Stage 10: 表情ベースのテンプレート優先
                    if (pattern.expressionConfidence > 0.5f)
                    {
                        if (pattern.lastExpression == FacialExpression.Fearful && pattern.doubtLevel > 0.5f)
                            return "general_nervous";
                        if (pattern.lastExpression == FacialExpression.Happy && pattern.doubtLevel < 0.3f)
                            return "general_player_confident";
                        if (pattern.lastExpression == FacialExpression.Surprise)
                            return "general_surprised";
                    }
                    if (pressureLevel > 1.5f)
                        return "general_confident";
                    else
                        return "general_neutral";
            }
        }

        /// <summary>
        /// Stage 6: Hesitation用テンプレートキー決定（カードインデックス付き）
        /// </summary>
        public string DetermineHesitationTemplateKey(
            int cardIndex,
            int totalCards,
            float pressureLevel)
        {
            // 最後のカード
            if (cardIndex == totalCards - 1)
            {
                return "hesitation_final_card";
            }

            // 途中のカード
            return "hesitation_mid_card";
        }

        /// <summary>
        /// Stage 6: Hesitation セリフ生成（カードインデックス付き）
        /// </summary>
        public string GenerateHesitation(
            int cardIndex,
            int totalCards,
            float pressureLevel,
            BehaviorPattern pattern = null)
        {
            string templateKey = DetermineHesitationTemplateKey(cardIndex, totalCards, pressureLevel);

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                return SelectWeightedRandom(options);
            }

            // フォールバック: pressure-based
            if (pressureLevel < 1.0f)
            {
                templateKey = "hesitation_low_pressure";
            }
            else if (pressureLevel < 2.0f)
            {
                templateKey = "hesitation_medium_pressure";
            }
            else
            {
                templateKey = "hesitation_high_pressure";
            }

            if (templates.TryGetValue(templateKey, out options))
            {
                string selected = options[UnityEngine.Random.Range(0, options.Length)];
                return selected;
            }

            var locHes = LocalizationManager.Instance;
            return locHes != null ? locHes.Get("fallback.category_default_hesitation") : "迷うな...";
        }

        private string ApplyPatternReplacement(string template, BehaviorPattern pattern)
        {
            // {position} の置換
            if (template.Contains("{position}"))
            {
                var loc = LocalizationManager.Instance;
                string positionText;
                if (loc != null)
                {
                    positionText = pattern.preferredPosition switch
                    {
                        0 => loc.Get("fallback.position_left"),
                        1 => loc.Get("fallback.position_center"),
                        2 => loc.Get("fallback.position_right"),
                        _ => loc.Get("fallback.position_default")
                    };
                }
                else
                {
                    positionText = pattern.preferredPosition switch
                    {
                        0 => "左",
                        1 => "中央",
                        2 => "右",
                        _ => "そこ"
                    };
                }

                template = template.Replace("{position}", positionText);
            }

            // Stage 10: {expression} の置換（言語対応）
            if (template.Contains("{expression}"))
            {
                bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
                string expressionText = isJa
                    ? FacialExpressionAnalyzer.GetExpressionNameJP(pattern.lastExpression)
                    : FacialExpressionAnalyzer.GetExpressionNameEN(pattern.lastExpression);
                template = template.Replace("{expression}", expressionText);
            }

            return template;
        }

        /// <summary>
        /// PersonalityProfileから最も顕著な特性のテンプレートキーを取得
        /// </summary>
        private string GetPersonalityTemplateKey(PersonalityProfile profile)
        {
            if (profile == null) return null;

            // 最も高い特性を選択
            float maxTrait = 0f;
            string key = null;

            if (profile.cautiousness > maxTrait) { maxTrait = profile.cautiousness; key = "personality_cautious"; }
            if (profile.intuition > maxTrait) { maxTrait = profile.intuition; key = "personality_intuitive"; }
            if (profile.resilience > maxTrait) { maxTrait = profile.resilience; key = "personality_resilient"; }
            if (profile.consistency > maxTrait) { maxTrait = profile.consistency; key = "personality_consistent"; }
            if (profile.adaptability > maxTrait) { maxTrait = profile.adaptability; key = "personality_adaptive"; }

            // 特性値が低すぎる場合は使用しない
            return maxTrait >= 0.4f ? key : null;
        }

        /// <summary>
        /// PersonalityProfileから最も顕著な特性のdeep読みテンプレートキーを取得
        /// </summary>
        private string GetPersonalityDeepReadKey(PersonalityProfile profile)
        {
            if (profile == null) return null;
            float maxTrait = 0f;
            string key = null;

            if (profile.cautiousness > maxTrait) { maxTrait = profile.cautiousness; key = "personality_read_cautious_deep"; }
            if (profile.intuition > maxTrait) { maxTrait = profile.intuition; key = "personality_read_intuitive_deep"; }
            if (profile.resilience > maxTrait) { maxTrait = profile.resilience; key = "personality_read_resilient_deep"; }
            if (profile.consistency > maxTrait) { maxTrait = profile.consistency; key = "personality_read_consistent_deep"; }
            if (profile.adaptability > maxTrait) { maxTrait = profile.adaptability; key = "personality_read_adaptive_deep"; }

            return maxTrait >= 0.4f ? key : null;
        }

        /// <summary>
        /// ターン開始時のメンタリスト挑発セリフを取得
        /// </summary>
        public string GetTurnStartDialogue(BehaviorPattern behavior, float pressureLevel, int turnCount)
        {
            // 序盤（ターン2以下）: 行動データ不要の観察テンプレート
            if (turnCount <= 2)
            {
                if (templates.TryGetValue("turn_start_observation_early", out string[] earlyOptions))
                {
                    return SelectWeightedRandom(earlyOptions);
                }
                if (templates.TryGetValue("turn_start_general_early", out string[] fallbackOptions))
                {
                    return SelectWeightedRandom(fallbackOptions);
                }
                var loc = LocalizationManager.Instance;
                return loc != null ? loc.Get("fallback.turn_start_early_fallback") : "さあ、見せてもらおう";
            }

            // 事前生成された性格読みセリフ（35%確率、turnCount >= 3）
            if (personalityReadLines.Count > 0 && turnCount >= 3 && UnityEngine.Random.value < 0.35f)
            {
                string line = personalityReadLines[personalityReadIndex % personalityReadLines.Count];
                personalityReadIndex++;
                return line;
            }

            // PersonalityProfile deep読み（15%確率、turnCount >= 4）
            if (playerProfile != null && turnCount >= 4 && UnityEngine.Random.value < 0.15f)
            {
                string deepKey = GetPersonalityDeepReadKey(playerProfile);
                if (deepKey != null && templates.ContainsKey(deepKey))
                {
                    return SelectWeightedRandom(templates[deepKey]);
                }
            }

            string templateKey;

            // Stage 10: 表情ベースの「読み」（20%確率、confidence十分）
            if (behavior != null && behavior.expressionConfidence > 0.5f && UnityEngine.Random.value < 0.2f)
            {
                if (behavior.lastExpression == FacialExpression.Fearful || behavior.lastExpression == FacialExpression.Sad)
                    templateKey = "turn_start_read_nervous";
                else if (behavior.lastExpression == FacialExpression.Happy)
                    templateKey = "turn_start_read_smiling";
                else if (behavior.lastExpression == FacialExpression.Surprise)
                    templateKey = "turn_start_read_surprised";
                else
                    templateKey = null; // 通常フローへ

                if (templateKey != null && templates.TryGetValue(templateKey, out string[] exprOptions))
                {
                    string selected = SelectWeightedRandom(exprOptions);
                    if (behavior != null) selected = ApplyPatternReplacement(selected, behavior);
                    return selected;
                }
            }

            // パターン検出済み → 「読み」系テンプレート優先
            if (behavior != null && behavior.hasPositionPreference && behavior.streakSamePosition >= 2)
            {
                templateKey = "turn_start_read_position";
            }
            else if (behavior != null && behavior.tempo == TempoType.Fast)
            {
                templateKey = "turn_start_read_tempo";
            }
            else if (behavior != null && behavior.doubtLevel > 0.5f)
            {
                templateKey = "turn_start_read_doubt";
            }
            else if (behavior != null && behavior.streakSamePosition >= 2)
            {
                templateKey = "turn_start_read_pattern";
            }
            // 予言（20%確率、turnCount >= 4）
            else if (turnCount >= 4 && UnityEngine.Random.value < 0.2f)
            {
                templateKey = "turn_start_predict";
            }
            // 一般挑発（ゲーム進行に応じて分岐）
            else if (turnCount <= 3)
            {
                templateKey = "turn_start_general_early";
            }
            else if (turnCount <= 8)
            {
                templateKey = "turn_start_general_mid";
            }
            else
            {
                templateKey = "turn_start_general_late";
            }

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                string selected = SelectWeightedRandom(options);

                if (behavior != null)
                {
                    selected = ApplyPatternReplacement(selected, behavior);
                }

                return selected;
            }

            var locFb = LocalizationManager.Instance;
            return locFb != null ? locFb.Get("fallback.turn_start_fallback") : "さあ、選べ";
        }

        /// <summary>
        /// AIターン開始時の理由付きセリフを取得
        /// AI判断の根拠（行動パターン、戦略）をプレイヤーに見せる
        /// </summary>
        public string GetAITurnReasoningDialogue(
            BehaviorPattern behavior,
            AIDecisionResult decision,
            float pressureLevel,
            int turnCount,
            int playerCardCount)
        {
            string templateKey;

            // 終盤（残り3枚以下）
            if (playerCardCount <= 3)
            {
                templateKey = "ai_turn_reason_endgame";
            }
            // 序盤（ターン3以下 or 行動データなし）
            else if (turnCount <= 3 || behavior == null)
            {
                templateKey = "ai_turn_reason_early";
            }
            // パターン検出時
            else if (behavior.hasPositionPreference && behavior.streakSamePosition >= 2)
            {
                templateKey = "ai_turn_reason_position";
            }
            // テンポが速い
            else if (behavior.tempo == TempoType.Fast)
            {
                templateKey = "ai_turn_reason_fast_tempo";
            }
            // 迷いが大きい
            else if (behavior.doubtLevel > 0.5f)
            {
                templateKey = "ai_turn_reason_doubt";
            }
            // 連続パターン検出
            else if (behavior.streakSamePosition >= 2)
            {
                templateKey = "ai_turn_reason_pattern";
            }
            // 一般
            else
            {
                templateKey = "ai_turn_reason_general";
            }

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                string selected = SelectWeightedRandom(options);

                if (behavior != null)
                {
                    selected = ApplyPatternReplacement(selected, behavior);
                }

                return selected;
            }

            var locAi = LocalizationManager.Instance;
            return locAi != null ? locAi.Get("fallback.ai_turn_general_fallback") : "僕のターンだ... さて、どれにしようか";
        }

        /// <summary>
        /// AIがカードを引いた直後のコメントを取得
        /// </summary>
        public string GetAIDrawComment(bool drawnIsJoker, bool formedPair, BehaviorPattern behavior = null)
        {
            string templateKey;
            if (drawnIsJoker)
                templateKey = "ai_draw_comment_joker";
            else if (formedPair)
                templateKey = "ai_draw_comment_pair";
            else
                templateKey = "ai_draw_comment_neutral";

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                string selected = SelectWeightedRandom(options);
                if (behavior != null)
                    selected = ApplyPatternReplacement(selected, behavior);
                return selected;
            }
            var locDraw = LocalizationManager.Instance;
            return locDraw != null ? locDraw.Get("fallback.draw_comment_fallback") : "ふん...";
        }

        /// <summary>
        /// ジョーカーティーズ台詞を取得
        /// </summary>
        public string GetJokerTeaseDialogue(bool isRealJoker)
        {
            string templateKey = isRealJoker ? "joker_tease_real" : "joker_tease_bluff";
            if (templates.TryGetValue(templateKey, out string[] options))
            {
                return SelectWeightedRandom(options);
            }
            var locJk = LocalizationManager.Instance;
            return locJk != null ? locJk.Get("fallback.joker_tease_fallback") : "ジョーカーはここだよ...";
        }

        /// <summary>
        /// 長考時の煽りセリフを取得
        /// </summary>
        public string GetIdleTauntDialogue(float pressureLevel, int idleIndex, BehaviorPattern behavior = null)
        {
            // Stage 12: 表情ベースの煽り（confidence十分な場合）
            if (behavior != null && behavior.expressionConfidence > 0.5f)
            {
                string exprKey = null;
                if (behavior.lastExpression == FacialExpression.Fearful || behavior.lastExpression == FacialExpression.Sad)
                    exprKey = "idle_taunt_expression_nervous";
                else if (behavior.lastExpression == FacialExpression.Angry)
                    exprKey = "idle_taunt_expression_angry";
                else if (behavior.lastExpression == FacialExpression.Happy)
                    exprKey = "idle_taunt_expression_smiling";

                if (exprKey != null && templates.TryGetValue(exprKey, out string[] exprOptions))
                {
                    return ApplyPatternReplacement(SelectWeightedRandom(exprOptions), behavior);
                }
            }

            string templateKey = idleIndex == 0 ? "idle_taunt_1" : "idle_taunt_2";

            if (templates.TryGetValue(templateKey, out string[] options))
            {
                string selected = SelectWeightedRandom(options);
                if (behavior != null)
                    selected = ApplyPatternReplacement(selected, behavior);
                return selected;
            }

            var locIdle = LocalizationManager.Instance;
            if (locIdle != null)
                return idleIndex == 0 ? locIdle.Get("fallback.idle_taunt_1_fallback") : locIdle.Get("fallback.idle_taunt_2_fallback");
            return idleIndex == 0 ? "手が止まったな..." : "そっちじゃない...";
        }

        private string GetCategoryDefault(DialogueCategoryType category)
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                return category switch
                {
                    DialogueCategoryType.Stop => loc.Get("fallback.category_default_stop"),
                    DialogueCategoryType.Bait => loc.Get("fallback.category_default_bait"),
                    DialogueCategoryType.Mirror => loc.Get("fallback.category_default_mirror"),
                    DialogueCategoryType.General => loc.Get("fallback.category_default_general"),
                    DialogueCategoryType.Hesitation => loc.Get("fallback.category_default_hesitation"),
                    _ => loc.Get("fallback.category_default_fallback")
                };
            }

            return category switch
            {
                DialogueCategoryType.Stop => "やめろ",
                DialogueCategoryType.Bait => "いいぞ",
                DialogueCategoryType.Mirror => "見えているぞ",
                DialogueCategoryType.General => "さあ",
                DialogueCategoryType.Hesitation => "迷うな...",
                _ => "..."
            };
        }

        /// <summary>
        /// ルールベースCoTステップ生成（LLMフォールバック用）
        /// メンタリストデータ（表情・行動パターン・圧力）からCoTを組み立て
        /// </summary>
        public List<CoTStep> GenerateFallbackCoTSteps(
            AIDecisionResult decision,
            BehaviorPattern behavior,
            float pressureLevel,
            int cardCount)
        {
            var steps = new List<CoTStep>();
            int targetCard = decision?.selectedCardIndex ?? 0;
            targetCard = Mathf.Clamp(targetCard, 0, Mathf.Max(0, cardCount - 1));
            bool isJP = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // Step 1 (Scan): targetでないカードを観察
            int scanCard = cardCount > 1 ? (targetCard + 1) % cardCount : 0;
            steps.Add(new CoTStep
            {
                cardIndex = scanCard,
                thought = GetScanThought(behavior, isJP)
            });

            // Step 2 (Deduce): 推理（行動パターンベース）
            int deduceCard = cardCount > 2 ? (targetCard + 2) % cardCount : scanCard;
            steps.Add(new CoTStep
            {
                cardIndex = deduceCard,
                thought = GetDeductionThought(behavior, pressureLevel, isJP)
            });

            // Step 3 (Narrow): cardCount > 2のとき、絞り込み
            if (cardCount > 2)
            {
                steps.Add(new CoTStep
                {
                    cardIndex = scanCard,
                    thought = GetNarrowingThought(behavior, isJP)
                });
            }

            // Step 4 (Lock): 最終確定
            steps.Add(new CoTStep
            {
                cardIndex = targetCard,
                thought = GetFinalThought(decision, isJP)
            });

            return steps;
        }

        private string GetScanThought(BehaviorPattern behavior, bool isJP)
        {
            if (behavior != null && behavior.lastExpression != FacialExpression.Neutral
                && behavior.expressionConfidence > 0.4f)
            {
                if (isJP)
                {
                    return behavior.lastExpression switch
                    {
                        FacialExpression.Happy => "その笑顔...何か隠してるだろ",
                        FacialExpression.Fearful => "怯えてる...面白い",
                        FacialExpression.Surprise => "おっ、動揺したね？",
                        FacialExpression.Angry => "怒ってる？焦ってるんだ",
                        FacialExpression.Sad => "悲しそうだな...追い詰められてる",
                        _ => "ふーん...お前の顔が何か言ってるよ"
                    };
                }
                return behavior.lastExpression switch
                {
                    FacialExpression.Happy => "That smile... hiding something?",
                    FacialExpression.Fearful => "Scared? Interesting...",
                    FacialExpression.Surprise => "Oh, flinched there?",
                    FacialExpression.Angry => "Angry? Must be cornered",
                    FacialExpression.Sad => "Looking sad... pressured huh?",
                    _ => "Your face says something..."
                };
            }

            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string[] thoughts = loc.GetArray("cot_thoughts.scan_generic");
                if (thoughts.Length > 0)
                {
                    return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                }
            }

            // Fallback
            if (isJP)
            {
                string[] thoughts = { "さて...どれかな", "お前の目を読むよ", "全部見えてるけどね" };
                return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
            }
            else
            {
                string[] thoughts = { "Let me see...", "Reading your eyes...", "I see everything though" };
                return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
            }
        }

        private string GetDeductionThought(BehaviorPattern behavior, float pressureLevel, bool isJP)
        {
            var loc = LocalizationManager.Instance;

            if (behavior != null)
            {
                if (behavior.hasPositionPreference && behavior.streakSamePosition >= 2)
                {
                    if (loc != null)
                        return loc.Get("cot_thoughts.scan_same_position");
                    return isJP
                        ? $"同じ場所ばかり...パターンが見える"
                        : "Same spot again... I see a pattern";
                }

                if (behavior.doubtLevel > 0.6f)
                {
                    if (loc != null)
                        return loc.Get("cot_thoughts.scan_high_doubt");
                    return isJP
                        ? "迷ってるな...守りたいカードがある"
                        : "Hesitating... protecting something?";
                }

                if (behavior.tempo == TempoType.Fast)
                {
                    if (loc != null)
                        return loc.Get("cot_thoughts.scan_fast_tempo");
                    return isJP
                        ? "速い...焦って判断してるな"
                        : "Fast moves... panicked decisions";
                }

                if (behavior.tempo == TempoType.Erratic)
                {
                    if (loc != null)
                        return loc.Get("cot_thoughts.scan_erratic");
                    return isJP
                        ? "リズムが乱れてる...動揺してるな"
                        : "Rhythm's off... you're shaken";
                }
            }

            if (pressureLevel > 2.0f)
            {
                if (loc != null)
                    return loc.Get("cot_thoughts.scan_high_pressure");
                return isJP ? "追い詰められてるな...余裕がない" : "Cornered... no room to breathe";
            }

            if (loc != null)
                return loc.Get("cot_thoughts.scan_default");
            return isJP ? "うーん...考えてるフリしよっか" : "Hmm... pretending to think here";
        }

        private string GetNarrowingThought(BehaviorPattern behavior, bool isJP)
        {
            var loc = LocalizationManager.Instance;

            if (behavior != null && behavior.lastExpression != FacialExpression.Neutral
                && behavior.expressionConfidence > 0.4f)
            {
                bool isMismatch = (behavior.lastExpression == FacialExpression.Happy && behavior.doubtLevel > 0.5f)
                    || (behavior.lastExpression == FacialExpression.Fearful && behavior.tempo == TempoType.Fast);

                if (isMismatch)
                {
                    if (loc != null)
                        return loc.Get("cot_thoughts.narrow_mismatch");
                    return isJP
                        ? "表情と行動が合ってない...嘘つき"
                        : "Face and actions don't match... liar";
                }
            }

            if (loc != null)
            {
                string[] thoughts = loc.GetArray("cot_thoughts.narrow_generic");
                if (thoughts.Length > 0)
                {
                    return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                }
            }

            // Fallback
            if (isJP)
            {
                string[] thoughts = { "こっちか...いや", "絞れてきた...2択だ", "もう少し...見えてきた" };
                return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
            }
            else
            {
                string[] thoughts = { "This one... no wait", "Narrowing down... two left", "Almost... getting clearer" };
                return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
            }
        }

        private string GetFinalThought(AIDecisionResult decision, bool isJP)
        {
            float confidence = decision?.confidence ?? 0.5f;
            var loc = LocalizationManager.Instance;

            if (confidence > 0.7f)
            {
                if (loc != null)
                {
                    string[] thoughts = loc.GetArray("cot_thoughts.final_high_confidence");
                    if (thoughts.Length > 0)
                    {
                        return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                    }
                }

                // Fallback
                if (isJP)
                {
                    string[] thoughts = { "...ここだ", "もうわかった。これだ", "答えは見えている" };
                    return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                }
                string[] enThoughts = { "...Right here", "Got it. This one", "The answer's clear" };
                return enThoughts[UnityEngine.Random.Range(0, enThoughts.Length)];
            }
            else
            {
                if (loc != null)
                {
                    string[] thoughts = loc.GetArray("cot_thoughts.final_low_confidence");
                    if (thoughts.Length > 0)
                    {
                        return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                    }
                }

                // Fallback
                if (isJP)
                {
                    string[] thoughts = { "...たぶんこれだ", "勘で行く...ここだ", "賭けてみるか" };
                    return thoughts[UnityEngine.Random.Range(0, thoughts.Length)];
                }
                string[] enThoughts = { "...Probably this", "Going with my gut", "Taking a gamble" };
                return enThoughts[UnityEngine.Random.Range(0, enThoughts.Length)];
            }
        }
    }

    // ===== 静的ダイアログデータベース =====

    public class StaticDialogueDatabase
    {
        private Dictionary<DialogueCategoryType, string[]> database;

        public StaticDialogueDatabase()
        {
            database = new Dictionary<DialogueCategoryType, string[]>();
        }

        private bool databaseLoaded = false;

        private void EnsureDatabaseLoaded()
        {
            if (!databaseLoaded && LocalizationManager.Instance != null)
            {
                InitializeDatabase();
                databaseLoaded = true;
            }
        }

        /// <summary>
        /// 言語変更時にデータベースを再読込
        /// </summary>
        public void ReloadDatabase()
        {
            InitializeDatabase();
            databaseLoaded = true;
            Debug.Log("[StaticDialogueDatabase] Database reloaded for language change");
        }

        private void InitializeDatabase()
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                database = new Dictionary<DialogueCategoryType, string[]>
                {
                    [DialogueCategoryType.Stop] = loc.GetArray("static_db.stop"),
                    [DialogueCategoryType.Bait] = loc.GetArray("static_db.bait"),
                    [DialogueCategoryType.Mirror] = loc.GetArray("static_db.mirror"),
                    [DialogueCategoryType.General] = loc.GetArray("static_db.general"),
                    [DialogueCategoryType.Hesitation] = loc.GetArray("static_db.hesitation")
                };
                Debug.Log($"[StaticDialogueDatabase] Loaded from LocalizationManager");
            }
            else
            {
                database = new Dictionary<DialogueCategoryType, string[]>();
                Debug.LogWarning("[StaticDialogueDatabase] LocalizationManager not available");
            }
        }

        public string GetDialogue(DialogueCategoryType category, float pressureLevel)
        {
            EnsureDatabaseLoaded();

            if (database.TryGetValue(category, out string[] options) && options.Length > 0)
            {
                int index = Mathf.Clamp(Mathf.FloorToInt(pressureLevel * 1.5f), 0, options.Length - 1);
                return options[index];
            }

            var loc = LocalizationManager.Instance;
            return loc != null ? loc.Get("static_db.fallback") : "...";
        }
    }

    // ===== データ構造 =====

    public enum DialogueCategoryType
    {
        Stop,       // 抑止（やめろ）
        Bait,       // 誘導（いいぞ）
        Mirror,     // 鏡写し（パターン指摘）
        General,    // 一般
        Hesitation  // Stage 6: AI迷い中セリフ
    }

    public enum TempoType
    {
        Slow,       // 慎重（>8秒）
        Normal,     // 普通（4-8秒）
        Fast,       // 急ぎ（<4秒）
        Erratic     // 不規則
    }

    [Serializable]
    public class BehaviorPattern
    {
        public float doubtLevel;            // 0-1
        public TempoType tempo;
        public int streakSamePosition;      // 連続同位置選択
        public bool hasPositionPreference;  // 位置好みがあるか
        public int preferredPosition;       // 0=左, 1=中央, 2=右
        public float avgHoverTime;          // 平均ホバー時間
        public float avgDecisionTime;       // 平均決断時間
        public int[] positionCounts;        // 位置別選択カウント [左, 中央, 右]

        // Stage 10: 表情データ（FacialExpressionAnalyzerから取得）
        public FacialExpression lastExpression = FacialExpression.Neutral;
        public float expressionConfidence;  // 0-1
    }
}
