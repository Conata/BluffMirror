using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// AIメモリ管理システム
    /// プレイヤー性格プロファイルと最近のゲームセッションを永続化
    /// </summary>
    public class AIMemoryManager
    {
        private const string PLAYER_PROFILE_KEY = "AI_PlayerProfile";
        private const string RECENT_SESSIONS_KEY = "AI_RecentSessions";
        private const string ENCRYPTION_KEY = "fps_trump_personality_2026";
        private const int MAX_RECENT_SESSIONS = 5;

        // 短期メモリ（RAM）
        private AISessionState currentSession;

        // 中期メモリ（PlayerPrefs）
        private Queue<SessionSummary> recentSessions = new Queue<SessionSummary>();

        // 長期メモリ（永続プロファイル）
        private PersonalityProfile persistentProfile;

        /// <summary>
        /// 現在のセッション状態を設定
        /// </summary>
        public void SetCurrentSession(AISessionState session)
        {
            currentSession = session;
        }

        /// <summary>
        /// セッションメモリを保存（ゲーム終了時）
        /// </summary>
        public void SaveSessionMemory()
        {
            if (currentSession == null)
            {
                Debug.LogWarning("[AIMemoryManager] No current session to save");
                return;
            }

            try
            {
                // セッションをサマリーに圧縮
                SessionSummary summary = CompressSessionToSummary(currentSession);

                // 最近のセッションに追加
                recentSessions.Enqueue(summary);

                // サイズ制限
                if (recentSessions.Count > MAX_RECENT_SESSIONS)
                {
                    recentSessions.Dequeue();
                }

                // PlayerPrefsに保存（暗号化）
                SaveRecentSessions();

                // プレイヤープロファイルも保存
                SavePlayerProfile(currentSession.playerProfile);

                Debug.Log($"[AIMemoryManager] Session memory saved. Total sessions: {recentSessions.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIMemoryManager] Failed to save session memory: {ex.Message}");
            }
        }

        /// <summary>
        /// セッションメモリを読み込み（ゲーム開始時）
        /// </summary>
        public void LoadSessionMemory()
        {
            try
            {
                // 最近のセッションを読み込み
                LoadRecentSessions();

                // プレイヤープロファイルを読み込み
                persistentProfile = LoadPlayerProfile();

                Debug.Log($"[AIMemoryManager] Session memory loaded. Sessions: {recentSessions.Count}, Profile: {(persistentProfile != null ? "Found" : "Not found")}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIMemoryManager] Failed to load session memory: {ex.Message}");
            }
        }

        /// <summary>
        /// 永続プロファイルを取得
        /// </summary>
        public PersonalityProfile GetPersistentProfile()
        {
            return persistentProfile;
        }

        /// <summary>
        /// 最近のセッションサマリーを取得
        /// </summary>
        public List<SessionSummary> GetRecentSessions(int count = -1)
        {
            if (count <= 0 || count > recentSessions.Count)
            {
                return recentSessions.ToList();
            }

            return recentSessions.TakeLast(count).ToList();
        }

        /// <summary>
        /// セッションをサマリーに圧縮
        /// </summary>
        private SessionSummary CompressSessionToSummary(AISessionState session)
        {
            var summary = new SessionSummary
            {
                timestamp = DateTime.Now,
                turnCount = session.gameState.turnNumber,
                playerWon = session.gameState.playerCardCount == 0,
                finalPressureLevel = session.currentPressureLevel
            };

            // 行動パターンの統計を計算
            var actions = session.behaviorHistory.GetAllActions();
            if (actions.Count > 0)
            {
                summary.avgHoverTime = actions.Average(a => a.hoverDuration);
                summary.avgDecisionTime = actions.Average(a => a.decisionTime);

                // 位置選択の傾向
                int leftCount = actions.Count(a => a.selectedPosition == 0);
                int centerCount = actions.Count(a => a.selectedPosition == 1);
                int rightCount = actions.Count(a => a.selectedPosition == 2);

                summary.positionPreference = leftCount > centerCount && leftCount > rightCount ? "left"
                                           : centerCount > rightCount ? "center"
                                           : "right";
            }

            // 効果的だった戦略を記録
            summary.effectiveStrategies = IdentifyEffectiveStrategies(session);

            // プレイヤーの反応を記録
            summary.playerReactions = SummarizePlayerReactions(session);

            return summary;
        }

        /// <summary>
        /// 効果的だった戦略を特定
        /// </summary>
        private List<string> IdentifyEffectiveStrategies(AISessionState session)
        {
            var strategies = new List<string>();

            // 高圧力時の反応
            if (session.currentPressureLevel > 2.0f)
            {
                strategies.Add("high_pressure_effective");
            }

            // セリフカテゴリ別の効果（簡易版）
            var dialogues = session.dialogueMemory;
            if (dialogues.Any(d => d.category == DialogueCategoryType.Mirror))
            {
                strategies.Add("mirror_dialogue_used");
            }

            return strategies;
        }

        /// <summary>
        /// プレイヤーの反応をサマリー化
        /// </summary>
        private string SummarizePlayerReactions(AISessionState session)
        {
            var actions = session.behaviorHistory.GetAllActions();
            if (actions.Count == 0)
            {
                return "no_data";
            }

            float avgDoubt = actions.Average(a => a.hoverDuration) > 2.0f ? 1.0f : 0.5f;

            if (avgDoubt > 0.7f)
                return "highly_susceptible_to_pressure";
            else if (avgDoubt > 0.4f)
                return "moderately_susceptible";
            else
                return "resistant_to_pressure";
        }

        /// <summary>
        /// 最近のセッションを保存
        /// </summary>
        private void SaveRecentSessions()
        {
            var data = new RecentSessionsData
            {
                sessions = recentSessions.ToList(),
                version = "1.0"
            };

            string json = JsonConvert.SerializeObject(data);
            string encrypted = EncryptionUtil.Encrypt(json, ENCRYPTION_KEY);

            PlayerPrefs.SetString(RECENT_SESSIONS_KEY, encrypted);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 最近のセッションを読み込み
        /// </summary>
        private void LoadRecentSessions()
        {
            if (!PlayerPrefs.HasKey(RECENT_SESSIONS_KEY))
            {
                return;
            }

            string encrypted = PlayerPrefs.GetString(RECENT_SESSIONS_KEY);
            string json = EncryptionUtil.Decrypt(encrypted, ENCRYPTION_KEY);

            var data = JsonConvert.DeserializeObject<RecentSessionsData>(json);

            if (data != null && data.sessions != null)
            {
                recentSessions = new Queue<SessionSummary>(data.sessions);
            }
        }

        /// <summary>
        /// プレイヤープロファイルを保存
        /// </summary>
        private void SavePlayerProfile(PersonalityProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            string json = JsonConvert.SerializeObject(profile);
            string encrypted = EncryptionUtil.Encrypt(json, ENCRYPTION_KEY);

            PlayerPrefs.SetString(PLAYER_PROFILE_KEY, encrypted);
            PlayerPrefs.Save();

            persistentProfile = profile;
        }

        /// <summary>
        /// プレイヤープロファイルを読み込み
        /// </summary>
        private PersonalityProfile LoadPlayerProfile()
        {
            if (!PlayerPrefs.HasKey(PLAYER_PROFILE_KEY))
            {
                return null;
            }

            string encrypted = PlayerPrefs.GetString(PLAYER_PROFILE_KEY);
            string json = EncryptionUtil.Decrypt(encrypted, ENCRYPTION_KEY);

            return JsonConvert.DeserializeObject<PersonalityProfile>(json);
        }

        /// <summary>
        /// すべてのメモリをクリア
        /// </summary>
        public void ClearAllMemory()
        {
            PlayerPrefs.DeleteKey(RECENT_SESSIONS_KEY);
            PlayerPrefs.DeleteKey(PLAYER_PROFILE_KEY);
            PlayerPrefs.Save();

            recentSessions.Clear();
            persistentProfile = null;
            currentSession = null;

            Debug.Log("[AIMemoryManager] All memory cleared");
        }
    }

    // ===== データ構造 =====

    [Serializable]
    public class SessionSummary
    {
        public DateTime timestamp;
        public int turnCount;
        public bool playerWon;
        public float finalPressureLevel;
        public float avgHoverTime;
        public float avgDecisionTime;
        public string positionPreference;       // "left", "center", "right"
        public List<string> effectiveStrategies;
        public string playerReactions;
    }

    [Serializable]
    public class RecentSessionsData
    {
        public string version;
        public List<SessionSummary> sessions;
    }

    // ===== 暗号化ユーティリティ =====

    public static class EncryptionUtil
    {
        /// <summary>
        /// 簡易XOR暗号化
        /// </summary>
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] encryptedBytes = new byte[plainBytes.Length];

            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 簡易XOR復号化
        /// </summary>
        public static string Decrypt(string encryptedText, string key)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] decryptedBytes = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return System.Text.Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
