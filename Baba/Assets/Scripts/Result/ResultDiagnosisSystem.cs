using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using FPSTrump.AI.LLM;

namespace FPSTrump.Result
{
    /// <summary>
    /// 性格分類 + LLM診断生成
    /// </summary>
    public class ResultDiagnosisSystem : MonoBehaviour
    {
        public static ResultDiagnosisSystem Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private LLMManager llmManager;

[Header("Settings")]
        [SerializeField] private int llmTimeoutMs = 10000;

        // === Hybrid mode: background LLM task ===
        private Task<string> backgroundLLMTask;

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

        private void Start()
        {
            if (llmManager == null)
                llmManager = LLMManager.Instance;
        }

        /// <summary>
        /// 診断を生成（ハイブリッド方式: 即座にフォールバック返却 + LLM詳細分析をバックグラウンドで開始）
        /// </summary>
        public DiagnosisResult GenerateDiagnosisImmediate(GameSessionData data, PersonalityProfile baseProfile = null)
        {
            // Step 1: 5軸スタッツ算出
            DiagnosisStats stats = CalculateStats(data);

            // Step 2: 性格タイプ分類
            PersonalityType primaryType = ClassifyPersonality(stats, data);
            PersonalityType secondaryType = GetSecondaryType(stats, primaryType);

            Debug.Log($"[ResultDiagnosisSystem] Classification: primary={primaryType}, secondary={secondaryType}");

            // Step 3: 即座にフォールバック診断を生成（静的テンプレート）
            DiagnosisResult result = ResultDiagnosisPrompt.GenerateFallback(primaryType, secondaryType, stats);

            // Step 4: 種明かし証拠生成
            result.evidences = GenerateEvidences(data, stats);

            // Step 5: プロファイル照合（生年月日入力済みの場合）
            if (baseProfile != null)
            {
                result.profileComparisons = CompareWithProfile(baseProfile, stats);
                result.hasProfileComparison = true;
                Debug.Log($"[ResultDiagnosisSystem] Profile comparison generated: {result.profileComparisons.Count} traits");
            }

            // Step 6: LLM詳細分析をバックグラウンドで開始
            if (llmManager != null)
            {
                PlayerAppearanceData appearance = llmManager.CurrentPlayerAppearance;
                PersonalityProfile profile = llmManager.CurrentPlayerProfile;
                string prompt = ResultDiagnosisPrompt.BuildDetailedAnalysisPrompt(data, stats, primaryType, appearance, profile);
                backgroundLLMTask = llmManager.GenerateDiagnosisAsync(prompt, llmTimeoutMs);
                Debug.Log("[ResultDiagnosisSystem] Background LLM detailed analysis started");
            }

            Debug.Log("[ResultDiagnosisSystem] Immediate fallback diagnosis returned");
            return result;
        }

        /// <summary>
        /// バックグラウンドLLM詳細分析を取得（タイムアウト付き待機）
        /// </summary>
        public async Task<string> GetDetailedAnalysisAsync(int maxWaitMs = 8000)
        {
            if (backgroundLLMTask == null)
            {
                Debug.Log("[ResultDiagnosisSystem] No background LLM task, skipping detailed analysis");
                return null;
            }

            // 最大待機時間
            float waitTime = 0f;
            float maxWaitSeconds = maxWaitMs / 1000f;

            while (!backgroundLLMTask.IsCompleted && waitTime < maxWaitSeconds)
            {
                await Task.Delay(100);
                waitTime += 0.1f;
            }

            if (!backgroundLLMTask.IsCompleted || backgroundLLMTask.IsFaulted)
            {
                Debug.Log("[ResultDiagnosisSystem] LLM detailed analysis not ready or failed");
                return null;
            }

            string analysis = backgroundLLMTask.Result;
            if (string.IsNullOrEmpty(analysis))
            {
                Debug.Log("[ResultDiagnosisSystem] LLM detailed analysis returned empty");
                return null;
            }

            Debug.Log($"[ResultDiagnosisSystem] LLM detailed analysis ready: \"{analysis}\"");
            return analysis;
        }

        /// <summary>
        /// 診断を生成（旧フロー: LLM → フォールバック、互換性のため残す）
        /// </summary>
        public async Task<DiagnosisResult> GenerateDiagnosis(GameSessionData data, PersonalityProfile baseProfile = null)
        {
            // Step 1: 5軸スタッツ算出
            DiagnosisStats stats = CalculateStats(data);

            // Step 2: 性格タイプ分類
            PersonalityType primaryType = ClassifyPersonality(stats, data);
            PersonalityType secondaryType = GetSecondaryType(stats, primaryType);

            Debug.Log($"[ResultDiagnosisSystem] Classification: primary={primaryType}, secondary={secondaryType}");

            // Step 3: LLM診断を試みる
            DiagnosisResult result = null;
            if (llmManager != null)
            {
                try
                {
                    result = await GenerateLLMDiagnosis(data, stats, primaryType, secondaryType);
                    if (result != null)
                    {
                        Debug.Log("[ResultDiagnosisSystem] LLM diagnosis generated successfully");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ResultDiagnosisSystem] LLM diagnosis failed: {ex.Message}");
                }
            }

            // Step 4: フォールバック
            if (result == null)
            {
                Debug.Log("[ResultDiagnosisSystem] Using fallback diagnosis");
                result = ResultDiagnosisPrompt.GenerateFallback(primaryType, secondaryType, stats);
            }

            // Step 4.5: LLM結果の空フィールドをフォールバックで補完
            ResultDiagnosisPrompt.FillEmptyFields(result);

            // Step 4.75: SAFETY CHECK - Should never happen, but guarantees non-null return
            if (result == null)
            {
                Debug.LogError("[ResultDiagnosisSystem] CRITICAL: result is still null after fallback. Creating emergency diagnosis.");
                result = new DiagnosisResult
                {
                    primaryType = primaryType,
                    secondaryType = secondaryType,
                    personalityTitle = LocalizationManager.Instance?.Get($"diagnosis.type_{primaryType.ToString().ToLower()}") ?? "Unknown Player",
                    personalityDescription = "Unable to generate diagnosis",
                    psychologicalTendency = "Analysis failed",
                    behavioralInsight = "Please try again",
                    stats = stats,
                    isLLMGenerated = false,
                    evidences = new List<BehavioralEvidence>()
                };
            }

            // Step 5: 種明かし証拠生成（LLMが証拠を返さなかった場合のみルールベースで生成）
            if (result.evidences == null || result.evidences.Count == 0)
            {
                result.evidences = GenerateEvidences(data, stats);
            }

            // Step 6: プロファイル照合（生年月日入力済みの場合）
            if (baseProfile != null)
            {
                result.profileComparisons = CompareWithProfile(baseProfile, stats);
                result.hasProfileComparison = true;
                Debug.Log($"[ResultDiagnosisSystem] Profile comparison generated: {result.profileComparisons.Count} traits");
            }

            return result;
        }

        /// <summary>
        /// フォールバック診断のみ生成（LLMなし）
        /// </summary>
        public DiagnosisResult GenerateFallbackDiagnosis(GameSessionData data, PersonalityProfile baseProfile = null)
        {
            DiagnosisStats stats = CalculateStats(data);
            PersonalityType primaryType = ClassifyPersonality(stats, data);
            PersonalityType secondaryType = GetSecondaryType(stats, primaryType);
            var result = ResultDiagnosisPrompt.GenerateFallback(primaryType, secondaryType, stats);

            result.evidences = GenerateEvidences(data, stats);

            if (baseProfile != null)
            {
                result.profileComparisons = CompareWithProfile(baseProfile, stats);
                result.hasProfileComparison = true;
            }

            return result;
        }

        // ========================================
        // 5軸スタッツ計算
        // ========================================

        public DiagnosisStats CalculateStats(GameSessionData data)
        {
            return new DiagnosisStats
            {
                decisiveness = CalculateDecisiveness(data),
                consistency = CalculateConsistency(data),
                resilience = CalculateResilience(data),
                intuition = CalculateIntuition(data),
                adaptability = CalculateAdaptability(data)
            };
        }

        private float CalculateDecisiveness(GameSessionData data)
        {
            float score = 1f - (data.avgDecisionTime / 10f) - (data.avgDoubtLevel * 0.3f);
            return Mathf.Clamp01(score);
        }

        private float CalculateConsistency(GameSessionData data)
        {
            float score = 1f - (data.tempoVariance / 5f);
            if (data.longestPositionStreak > 3) score += 0.2f;
            return Mathf.Clamp01(score);
        }

        private float CalculateResilience(GameSessionData data)
        {
            float score = data.pressureResponseScore;
            if (data.dominantTempo != TempoType.Erratic)
                score += 0.1f;
            else
                score -= 0.2f;
            return Mathf.Clamp01(score);
        }

        private float CalculateIntuition(GameSessionData data)
        {
            float score = 0f;
            if (data.dominantTempo == TempoType.Fast) score += 0.4f;
            if (data.avgHoverTime < 1.5f) score += 0.3f;
            if (data.avgDoubtLevel < 0.3f) score += 0.3f;
            return Mathf.Clamp01(score);
        }

        private float CalculateAdaptability(GameSessionData data)
        {
            float score = 0f;

            // テンポに変化があるか（Erratic != 一貫）
            if (data.tempoVariance > 1.0f && data.dominantTempo != TempoType.Erratic) score += 0.4f;

            // 位置偏りがない
            if (!data.hadPositionPreference) score += 0.3f;

            // 感情が4種以上出現
            if (data.emotionFrequency != null && data.emotionFrequency.Count >= 4) score += 0.3f;

            return Mathf.Clamp01(score);
        }

        // ========================================
        // 性格タイプ分類
        // ========================================

        public PersonalityType ClassifyPersonality(DiagnosisStats stats, GameSessionData data)
        {
            // 順序依存の分類ルール
            if (stats.consistency > 0.7f && stats.resilience > 0.6f)
                return PersonalityType.Stoic;

            if (stats.intuition > 0.7f && stats.decisiveness > 0.6f)
                return PersonalityType.Intuitive;

            if (stats.decisiveness < 0.4f && data.avgHoverTime > 3.0f)
                return PersonalityType.Cautious;

            if (data.dominantTempo == TempoType.Erratic && stats.decisiveness > 0.5f)
                return PersonalityType.Gambler;

            if (stats.adaptability > 0.6f)
                return PersonalityType.Adapter;

            return PersonalityType.Analyst;
        }

        /// <summary>
        /// 2番目に高いタイプを取得
        /// </summary>
        private PersonalityType GetSecondaryType(DiagnosisStats stats, PersonalityType primary)
        {
            var scores = new (PersonalityType type, float score)[]
            {
                (PersonalityType.Analyst, (stats.decisiveness + stats.consistency) / 2f),
                (PersonalityType.Intuitive, (stats.intuition + stats.decisiveness) / 2f),
                (PersonalityType.Cautious, 1f - stats.decisiveness),
                (PersonalityType.Gambler, 1f - stats.consistency),
                (PersonalityType.Adapter, stats.adaptability),
                (PersonalityType.Stoic, (stats.consistency + stats.resilience) / 2f)
            };

            return scores
                .Where(s => s.type != primary)
                .OrderByDescending(s => s.score)
                .First()
                .type;
        }

        // ========================================
        // LLM診断生成
        // ========================================

        private async Task<DiagnosisResult> GenerateLLMDiagnosis(
            GameSessionData data,
            DiagnosisStats stats,
            PersonalityType primaryType,
            PersonalityType secondaryType)
        {
            // Stage 12: 外見・プロファイルデータを注入
            PlayerAppearanceData appearance = llmManager?.CurrentPlayerAppearance;
            PersonalityProfile profile = llmManager?.CurrentPlayerProfile;
            string prompt = ResultDiagnosisPrompt.BuildDiagnosisPrompt(data, stats, primaryType, appearance, profile);

            string response = await llmManager.GenerateDiagnosisAsync(prompt, llmTimeoutMs);

            if (string.IsNullOrEmpty(response))
                return null;

            // JSONパース
            return ParseLLMResponse(response, primaryType, secondaryType, stats);
        }

        private DiagnosisResult ParseLLMResponse(
            string response,
            PersonalityType primaryType,
            PersonalityType secondaryType,
            DiagnosisStats stats)
        {
            try
            {
                // JSON抽出（前後の余計なテキスト除去）
                int jsonStart = response.IndexOf('{');
                int jsonEnd = response.LastIndexOf('}');
                if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
                    return null;

                string json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonConvert.DeserializeObject<LLMDiagnosisResponse>(json);

                if (parsed == null) return null;

                var result = new DiagnosisResult
                {
                    primaryType = primaryType,
                    secondaryType = secondaryType,
                    personalityTitle = parsed.title ?? ResultDiagnosisPrompt.GetTypeNameJa(primaryType),
                    personalityDescription = parsed.description ?? "",
                    psychologicalTendency = parsed.tendency ?? "",
                    behavioralInsight = parsed.insight ?? "",
                    stats = stats,
                    isLLMGenerated = true
                };

                // LLMから証拠が返された場合はそれも取り込む
                if (parsed.evidences != null && parsed.evidences.Length > 0)
                {
                    result.evidences = new List<BehavioralEvidence>();
                    foreach (var ev in parsed.evidences)
                    {
                        result.evidences.Add(new BehavioralEvidence
                        {
                            observation = ev.observation ?? "",
                            interpretation = ev.interpretation ?? "",
                            statConnection = "",
                            impactScore = 0.8f
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ResultDiagnosisSystem] JSON parse failed: {ex.Message}");
                return null;
            }
        }

        [Serializable]
        private class LLMDiagnosisResponse
        {
            public string title;
            public string description;
            public string tendency;
            public string insight;
            public LLMEvidenceResponse[] evidences;
        }

        [Serializable]
        private class LLMEvidenceResponse
        {
            public string observation;
            public string interpretation;
        }

        // ========================================
        // 種明かし証拠生成
        // ========================================

        /// <summary>
        /// 行動データと5軸スコアから種明かし証拠を生成（impactScore順に上位3-5個）
        /// </summary>
        public List<BehavioralEvidence> GenerateEvidences(GameSessionData data, DiagnosisStats stats)
        {
            var loc = LocalizationManager.Instance;
            var candidates = new List<BehavioralEvidence>();

            string ResolveKey(string key)
            {
                if (loc != null)
                {
                    string resolved = loc.Get(key);
                    if (resolved != key) return resolved;
                }
                // LocalizationManager unavailable or key not found - try to re-acquire
                var freshLoc = LocalizationManager.Instance;
                if (freshLoc != null && freshLoc != loc)
                {
                    string resolved = freshLoc.Get(key);
                    if (resolved != key) return resolved;
                }
                Debug.LogWarning($"[ResultDiagnosisSystem] Localization key unresolved: {key}");
                return null;
            }

            void AddEvidence(string obsKey, string interpKey, string stat, float impact)
            {
                string obs = ResolveKey(obsKey);
                string interp = ResolveKey(interpKey);
                // Skip evidence if localization completely failed
                if (obs == null && interp == null) return;

                candidates.Add(new BehavioralEvidence
                {
                    observation = obs ?? "",
                    interpretation = interp ?? "",
                    statConnection = stat,
                    impactScore = impact
                });
            }

            if (stats.decisiveness > 0.7f)
                AddEvidence("evidence.decisiveness_high_obs", "evidence.decisiveness_high_interp", "decisiveness", stats.decisiveness);
            else if (stats.decisiveness < 0.3f)
                AddEvidence("evidence.decisiveness_low_obs", "evidence.decisiveness_low_interp", "decisiveness", 1f - stats.decisiveness);

            if (stats.consistency > 0.7f)
                AddEvidence("evidence.consistency_high_obs", "evidence.consistency_high_interp", "consistency", stats.consistency);
            else if (stats.consistency < 0.3f)
                AddEvidence("evidence.consistency_low_obs", "evidence.consistency_low_interp", "consistency", 1f - stats.consistency);

            if (stats.resilience > 0.7f)
                AddEvidence("evidence.resilience_high_obs", "evidence.resilience_high_interp", "resilience", stats.resilience);
            else if (stats.resilience < 0.3f)
                AddEvidence("evidence.resilience_low_obs", "evidence.resilience_low_interp", "resilience", 1f - stats.resilience);

            if (stats.intuition > 0.7f)
                AddEvidence("evidence.intuition_high_obs", "evidence.intuition_high_interp", "intuition", stats.intuition);
            else if (stats.intuition < 0.3f)
                AddEvidence("evidence.intuition_low_obs", "evidence.intuition_low_interp", "intuition", 1f - stats.intuition);

            if (stats.adaptability > 0.6f)
                AddEvidence("evidence.adaptability_high_obs", "evidence.adaptability_high_interp", "adaptability", stats.adaptability);

            if (data.hadPositionPreference)
                AddEvidence("evidence.position_pref_obs", "evidence.position_pref_interp", "consistency", 0.6f);
            else if (data.totalTurns > 4)
                AddEvidence("evidence.position_no_pref_obs", "evidence.position_no_pref_interp", "adaptability", 0.5f);

            if (data.dominantTempo == TempoType.Erratic)
                AddEvidence("evidence.tempo_erratic_obs", "evidence.tempo_erratic_interp", "adaptability", 0.7f);

            if (data.avgDoubtLevel > 0.5f)
                AddEvidence("evidence.doubt_high_obs", "evidence.doubt_high_interp", "decisiveness", data.avgDoubtLevel * 0.8f);

            candidates.Sort((a, b) => b.impactScore.CompareTo(a.impactScore));
            int count = Mathf.Clamp(candidates.Count, 0, 5);
            count = Mathf.Max(count, Mathf.Min(3, candidates.Count));

            var result = candidates.GetRange(0, count);
            Debug.Log($"[ResultDiagnosisSystem] Generated {result.Count} behavioral evidences");
            return result;
        }

        // ========================================
        // プロファイル照合
        // ========================================

        /// <summary>
        /// 生年月日ベースのPersonalityProfileとDiagnosisStatsを照合
        /// </summary>
        public List<ProfileComparison> CompareWithProfile(PersonalityProfile baseProfile, DiagnosisStats stats)
        {
            var loc = LocalizationManager.Instance;
            var comparisons = new List<ProfileComparison>();

            string GetTrait(string key, string fallback) => loc != null ? loc.Get(key) : fallback;

            AddComparison(comparisons, GetTrait("diagnosis.trait_cautiousness", "Cautiousness"), baseProfile.cautiousness, 1f - stats.decisiveness);
            AddComparison(comparisons, GetTrait("diagnosis.trait_intuition", "Intuition"), baseProfile.intuition, stats.intuition);
            AddComparison(comparisons, GetTrait("diagnosis.trait_resilience", "Resilience"), baseProfile.resilience, stats.resilience);
            AddComparison(comparisons, GetTrait("diagnosis.trait_consistency", "Consistency"), baseProfile.consistency, stats.consistency);
            AddComparison(comparisons, GetTrait("diagnosis.trait_adaptability", "Adaptability"), baseProfile.curiosity, stats.adaptability);

            return comparisons;
        }

        private void AddComparison(List<ProfileComparison> list, string traitName, float predicted, float actual)
        {
            var loc = LocalizationManager.Instance;
            float diff = Mathf.Abs(predicted - actual);
            string commentary;
            if (diff <= 0.15f)
                commentary = loc != null ? loc.Get("diagnosis.comparison_match") : "Match";
            else if (diff <= 0.3f)
                commentary = loc != null ? loc.Get("diagnosis.comparison_close") : "Close match";
            else if (actual > predicted)
                commentary = loc != null ? loc.Get("diagnosis.comparison_higher") : "Unexpected — Higher";
            else
                commentary = loc != null ? loc.Get("diagnosis.comparison_lower") : "Unexpected — Lower";

            list.Add(new ProfileComparison
            {
                traitName = traitName,
                predicted = predicted,
                actual = actual,
                commentary = commentary
            });
        }
    }
}
