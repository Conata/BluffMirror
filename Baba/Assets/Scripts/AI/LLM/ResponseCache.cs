using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// LLMレスポンスのキャッシングシステム
    /// パフォーマンス最適化とオフライン機能を提供
    /// </summary>
    public class ResponseCache
    {
        private Dictionary<string, List<CachedDialogue>> cache = new Dictionary<string, List<CachedDialogue>>();
        private Dictionary<string, int> usageCount = new Dictionary<string, int>();
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

        private const int MAX_CACHE_ENTRIES_PER_KEY = 5;
        private const int MAX_TOTAL_CACHE_SIZE = 100;

        /// <summary>
        /// キャッシュからダイアログを取得を試みる
        /// </summary>
        public bool TryGet(string key, out string dialogue)
        {
            if (cache.TryGetValue(key, out var entries) && entries.Count > 0)
            {
                // 最も使われていないエントリを選択（LFU: Least Frequently Used）
                var selected = entries.OrderBy(e => usageCount.GetValueOrDefault(e.text, 0)).First();

                dialogue = selected.text;

                // 使用回数を更新
                if (!usageCount.ContainsKey(dialogue))
                {
                    usageCount[dialogue] = 0;
                }
                usageCount[dialogue]++;

                Debug.Log($"[ResponseCache] Cache HIT for key: {key} | Dialogue: {dialogue.Substring(0, Math.Min(30, dialogue.Length))}...");
                return true;
            }

            dialogue = null;
            Debug.Log($"[ResponseCache] Cache MISS for key: {key}");
            return false;
        }

        /// <summary>
        /// ダイアログをキャッシュに追加
        /// </summary>
        public void Set(string key, string dialogue)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(dialogue))
            {
                return;
            }

            if (!cache.ContainsKey(key))
            {
                cache[key] = new List<CachedDialogue>();
            }

            // 既に存在する場合は追加しない
            if (cache[key].Any(e => e.text == dialogue))
            {
                return;
            }

            // キャッシュサイズ制限
            if (cache[key].Count >= MAX_CACHE_ENTRIES_PER_KEY)
            {
                // 最も古いエントリを削除
                cache[key].RemoveAt(0);
            }

            cache[key].Add(new CachedDialogue
            {
                text = dialogue,
                timestamp = DateTime.Now
            });

            Debug.Log($"[ResponseCache] Cached dialogue for key: {key}");

            // 全体のキャッシュサイズ制限
            EnforceCacheSizeLimit();
        }

        /// <summary>
        /// 複数のバリエーションをキャッシュに追加
        /// </summary>
        public void SetVariations(string key, string[] variations)
        {
            foreach (var variation in variations)
            {
                Set(key, variation);
            }
        }

        /// <summary>
        /// 音声クリップをキャッシュに追加
        /// </summary>
        public void SetAudio(string dialogueText, AudioClip audioClip)
        {
            if (string.IsNullOrEmpty(dialogueText) || audioClip == null)
            {
                return;
            }

            // 既存のエントリがある場合、古いAudioClipを削除
            if (audioCache.ContainsKey(dialogueText))
            {
                var oldClip = audioCache[dialogueText];
                if (oldClip != null && oldClip != audioClip)
                {
                    // Unity リソースを適切に解放
                    UnityEngine.Object.Destroy(oldClip);
                    Debug.Log($"[ResponseCache] Destroyed old audio clip for: {dialogueText.Substring(0, Math.Min(30, dialogueText.Length))}...");
                }
            }

            audioCache[dialogueText] = audioClip;
            Debug.Log($"[ResponseCache] Cached audio for dialogue: {dialogueText.Substring(0, Math.Min(30, dialogueText.Length))}...");
        }

        /// <summary>
        /// 音声クリップの取得を試みる
        /// </summary>
        public bool TryGetAudio(string dialogueText, out AudioClip audioClip)
        {
            if (audioCache.TryGetValue(dialogueText, out audioClip))
            {
                Debug.Log($"[ResponseCache] Audio cache HIT");
                return true;
            }

            Debug.Log($"[ResponseCache] Audio cache MISS");
            return false;
        }

        /// <summary>
        /// キャッシュを事前にウォーミング
        /// ゲーム開始時によくあるシナリオのダイアログを事前生成
        /// </summary>
        public async Task PreWarmCache(
            Func<string, int, float, Task<string>> generateFunc,
            string[] commonScenarios)
        {
            Debug.Log($"[ResponseCache] Pre-warming cache with {commonScenarios.Length} scenarios...");

            int successCount = 0;
            int failCount = 0;

            foreach (var scenario in commonScenarios)
            {
                try
                {
                    // 各シナリオで3バリエーション生成
                    string prompt = $"{scenario}\n\nGenerate 3 different dialogue variations, each on a new line:";
                    string response = await generateFunc(prompt, 200, 0.8f);

                    // バリエーションを分割
                    string[] variations = response
                        .Split('\n')
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 5) // 短すぎるものは除外
                        .ToArray();

                    if (variations.Length > 0)
                    {
                        SetVariations(scenario, variations);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        Debug.LogWarning($"[ResponseCache] No valid variations for scenario: {scenario}");
                    }

                    // レート制限対策: 少し待機
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    failCount++;
                    Debug.LogWarning($"[ResponseCache] Pre-warming failed for scenario '{scenario}': {ex.Message}");
                }
            }

            Debug.Log($"[ResponseCache] Pre-warming complete. Success: {successCount}, Failed: {failCount}");
        }

        // ===== AIDecisionResult キャッシュ =====

        private Dictionary<string, AIDecisionResult> decisionCache = new Dictionary<string, AIDecisionResult>();

        /// <summary>
        /// キャッシュからAI決定結果を取得を試みる
        /// </summary>
        public bool TryGetDecision(string key, out AIDecisionResult decision)
        {
            if (decisionCache.TryGetValue(key, out decision))
            {
                Debug.Log($"[ResponseCache] Decision cache HIT for key: {key}");
                return true;
            }

            Debug.Log($"[ResponseCache] Decision cache MISS for key: {key}");
            return false;
        }

        /// <summary>
        /// AI決定結果をキャッシュに保存
        /// </summary>
        public void SetDecision(string key, AIDecisionResult decision)
        {
            if (string.IsNullOrEmpty(key) || decision == null) return;

            decisionCache[key] = decision;
            Debug.Log($"[ResponseCache] Cached decision for key: {key}");
        }

        /// <summary>
        /// キャッシュをクリア
        /// </summary>
        public void Clear()
        {
            cache.Clear();
            usageCount.Clear();
            audioCache.Clear();
            decisionCache.Clear();
            Debug.Log("[ResponseCache] Cache cleared");
        }

        /// <summary>
        /// キャッシュ統計情報を取得
        /// </summary>
        public CacheStats GetStats()
        {
            int totalEntries = cache.Sum(kvp => kvp.Value.Count);
            int totalKeys = cache.Count;
            int totalAudioClips = audioCache.Count;

            return new CacheStats
            {
                totalKeys = totalKeys,
                totalEntries = totalEntries,
                totalAudioClips = totalAudioClips
            };
        }

        /// <summary>
        /// キャッシュサイズ制限を適用
        /// </summary>
        private void EnforceCacheSizeLimit()
        {
            int totalEntries = cache.Sum(kvp => kvp.Value.Count);

            if (totalEntries > MAX_TOTAL_CACHE_SIZE)
            {
                // 最も古いエントリを持つキーを削除
                var oldestKey = cache
                    .OrderBy(kvp => kvp.Value.Min(e => e.timestamp))
                    .First()
                    .Key;

                cache.Remove(oldestKey);
                Debug.Log($"[ResponseCache] Removed oldest cache key: {oldestKey} (size limit exceeded)");
            }
        }
    }

    // ===== データ構造 =====

    [Serializable]
    public class CachedDialogue
    {
        public string text;
        public DateTime timestamp;
    }

    [Serializable]
    public class CacheStats
    {
        public int totalKeys;
        public int totalEntries;
        public int totalAudioClips;

        public override string ToString()
        {
            return $"Cache Stats - Keys: {totalKeys}, Entries: {totalEntries}, Audio Clips: {totalAudioClips}";
        }
    }
}
