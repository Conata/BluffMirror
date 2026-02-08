using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// 性格永続化システム
    /// プレイヤーの性格プロファイルを維持し、実際の行動との差異に基づいて適応
    /// </summary>
    public class PersonalityPersistenceSystem
    {
        private PersonalityProfile baseProfile;         // 基本プロファイル（生年月日ベース）
        private PersonalityProfile adaptedProfile;      // 適応済みプロファイル

        private const float ADAPTATION_RATE = 0.15f;    // 学習率
        private const float ADAPTATION_THRESHOLD = 0.2f; // 適応を開始する差異の閾値

        /// <summary>
        /// 基本プロファイルを取得
        /// </summary>
        public PersonalityProfile BaseProfile => baseProfile;

        /// <summary>
        /// 適応済みプロファイルを取得
        /// </summary>
        public PersonalityProfile AdaptedProfile => adaptedProfile;

        /// <summary>
        /// セッションを初期化
        /// </summary>
        public void InitializeSession(PersonalityProfile profile)
        {
            baseProfile = profile;
            adaptedProfile = profile.Clone();

            Debug.Log("[PersonalityPersistenceSystem] Session initialized with base profile");
        }

        /// <summary>
        /// LLMでプロファイルを強化（非同期）
        /// </summary>
        public async Task<LLMManager.PersonalityEnhancement> EnhanceWithLLM(LLMManager llmManager)
        {
            if (baseProfile == null)
            {
                Debug.LogWarning("[PersonalityPersistenceSystem] Base profile not set");
                return null;
            }

            try
            {
                Debug.Log("[PersonalityPersistenceSystem] Enhancing profile with LLM...");

                LLMManager.PersonalityEnhancement enhancement = await llmManager.EnhancePersonalityProfileAsync(baseProfile);

                if (enhancement != null)
                {
                    Debug.Log($"[PersonalityPersistenceSystem] Profile enhanced: {enhancement.weaknesses.Length} weaknesses, {enhancement.strategies.Length} strategies");
                }

                return enhancement;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PersonalityPersistenceSystem] LLM enhancement failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 観測行動に基づいてプロファイルを適応
        /// </summary>
        public void AdaptProfileBasedOnBehavior(BehaviorPattern observedBehavior)
        {
            if (adaptedProfile == null)
            {
                Debug.LogWarning("[PersonalityPersistenceSystem] Adapted profile not initialized");
                return;
            }

            bool adapted = false;

            // 慎重性の適応
            float observedCautiousness = CalculateCautiousnessFromBehavior(observedBehavior);
            if (Mathf.Abs(adaptedProfile.cautiousness - observedCautiousness) > ADAPTATION_THRESHOLD)
            {
                float oldValue = adaptedProfile.cautiousness;
                adaptedProfile.cautiousness = Mathf.Lerp(
                    adaptedProfile.cautiousness,
                    observedCautiousness,
                    ADAPTATION_RATE
                );

                Debug.Log($"[PersonalityPersistenceSystem] Adapted cautiousness: {oldValue:F2} → {adaptedProfile.cautiousness:F2}");
                adapted = true;
            }

            // 直感性の適応
            float observedIntuition = CalculateIntuitionFromBehavior(observedBehavior);
            if (Mathf.Abs(adaptedProfile.intuition - observedIntuition) > ADAPTATION_THRESHOLD)
            {
                float oldValue = adaptedProfile.intuition;
                adaptedProfile.intuition = Mathf.Lerp(
                    adaptedProfile.intuition,
                    observedIntuition,
                    ADAPTATION_RATE
                );

                Debug.Log($"[PersonalityPersistenceSystem] Adapted intuition: {oldValue:F2} → {adaptedProfile.intuition:F2}");
                adapted = true;
            }

            // 回復力の適応（圧力への耐性）
            float observedResilience = CalculateResilienceFromBehavior(observedBehavior);
            if (Mathf.Abs(adaptedProfile.resilience - observedResilience) > ADAPTATION_THRESHOLD)
            {
                float oldValue = adaptedProfile.resilience;
                adaptedProfile.resilience = Mathf.Lerp(
                    adaptedProfile.resilience,
                    observedResilience,
                    ADAPTATION_RATE
                );

                Debug.Log($"[PersonalityPersistenceSystem] Adapted resilience: {oldValue:F2} → {adaptedProfile.resilience:F2}");
                adapted = true;
            }

            if (!adapted)
            {
                Debug.Log("[PersonalityPersistenceSystem] No significant deviation detected, no adaptation needed");
            }
        }

        /// <summary>
        /// 行動から慎重性を計算
        /// </summary>
        private float CalculateCautiousnessFromBehavior(BehaviorPattern behavior)
        {
            // 慎重性 = ホバー時間 + 疑念レベル
            float hoverScore = Mathf.Clamp01(behavior.avgHoverTime / 5.0f); // 5秒を最大とする
            float doubtScore = behavior.doubtLevel;

            return (hoverScore + doubtScore) / 2.0f;
        }

        /// <summary>
        /// 行動から直感性を計算
        /// </summary>
        private float CalculateIntuitionFromBehavior(BehaviorPattern behavior)
        {
            // 直感性 = 速いテンポ + 低い疑念レベル
            float tempoScore = behavior.tempo == TempoType.Fast ? 1.0f
                            : behavior.tempo == TempoType.Normal ? 0.5f
                            : 0.0f;

            float quickDecisionScore = 1.0f - behavior.doubtLevel;

            return (tempoScore + quickDecisionScore) / 2.0f;
        }

        /// <summary>
        /// 行動から回復力を計算
        /// </summary>
        private float CalculateResilienceFromBehavior(BehaviorPattern behavior)
        {
            // 回復力 = 圧力下でも安定したテンポ
            if (behavior.tempo == TempoType.Erratic)
            {
                return 0.3f; // 不規則 = 低回復力
            }
            else if (behavior.doubtLevel < 0.5f)
            {
                return 0.8f; // 圧力に強い
            }
            else
            {
                return 0.5f; // 中程度
            }
        }

        /// <summary>
        /// 適応状況を取得（LLMコンテキスト用）
        /// </summary>
        public string GetAdaptationStatus()
        {
            if (baseProfile == null || adaptedProfile == null)
            {
                return "No profile loaded";
            }

            float cautiousnessDiff = Mathf.Abs(adaptedProfile.cautiousness - baseProfile.cautiousness);
            float intuitionDiff = Mathf.Abs(adaptedProfile.intuition - baseProfile.intuition);
            float resilienceDiff = Mathf.Abs(adaptedProfile.resilience - baseProfile.resilience);

            if (cautiousnessDiff < 0.1f && intuitionDiff < 0.1f && resilienceDiff < 0.1f)
            {
                return "Profile stable - prediction matches behavior";
            }
            else
            {
                return $"Profile adapted - Cautiousness: {cautiousnessDiff:F2}, Intuition: {intuitionDiff:F2}, Resilience: {resilienceDiff:F2}";
            }
        }

        /// <summary>
        /// プロファイルをリセット（新しいプレイヤー用）
        /// </summary>
        public void Reset()
        {
            baseProfile = null;
            adaptedProfile = null;
            Debug.Log("[PersonalityPersistenceSystem] Reset");
        }
    }
}
