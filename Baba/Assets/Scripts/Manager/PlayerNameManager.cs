using UnityEngine;

namespace FPSTrump.Manager
{
    /// <summary>
    /// プレイヤー名の保存・取得・バリデーション
    /// PlayerPrefsで永続化（PlayerBirthdayManagerと同じパターン）
    /// </summary>
    public class PlayerNameManager : MonoBehaviour
    {
        public static PlayerNameManager Instance { get; private set; }

        private const string KEY_PLAYER_NAME = "PlayerName";
        private const int MAX_NAME_LENGTH = 20;

        private string playerName;
        private bool hasData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadName();
        }

        /// <summary>
        /// プレイヤー名を保存
        /// </summary>
        public void SaveName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("[PlayerNameManager] Cannot save empty name");
                return;
            }

            // 長さ制限
            if (name.Length > MAX_NAME_LENGTH)
            {
                name = name.Substring(0, MAX_NAME_LENGTH);
            }

            playerName = name.Trim();
            hasData = true;

            PlayerPrefs.SetString(KEY_PLAYER_NAME, playerName);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerNameManager] Name saved: {playerName}");
        }

        /// <summary>
        /// 保存済みの名前をロード
        /// </summary>
        private void LoadName()
        {
            if (PlayerPrefs.HasKey(KEY_PLAYER_NAME))
            {
                playerName = PlayerPrefs.GetString(KEY_PLAYER_NAME);
                hasData = !string.IsNullOrWhiteSpace(playerName);

                if (hasData)
                    Debug.Log($"[PlayerNameManager] Name loaded: {playerName}");
            }
            else
            {
                playerName = "";
                hasData = false;
            }
        }

        /// <summary>
        /// 名前が設定済みか
        /// </summary>
        public bool HasName() => hasData && !string.IsNullOrWhiteSpace(playerName);

        /// <summary>
        /// プレイヤー名を取得
        /// </summary>
        public string GetName()
        {
            return hasData ? playerName : "";
        }

        /// <summary>
        /// 名前をクリア（デバッグ用）
        /// </summary>
        [ContextMenu("Clear Name")]
        public void ClearName()
        {
            PlayerPrefs.DeleteKey(KEY_PLAYER_NAME);
            PlayerPrefs.Save();
            playerName = "";
            hasData = false;
            Debug.Log("[PlayerNameManager] Name cleared");
        }
    }
}
