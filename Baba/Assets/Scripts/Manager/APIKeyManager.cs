using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using FPSTrump.AI.LLM;

namespace FPSTrump.Manager
{
    /// <summary>
    /// APIキー管理システム
    /// ゲーム内でAPIキーを安全に保存・読み込み
    /// </summary>
    public class APIKeyManager : MonoBehaviour
    {
        private static APIKeyManager _instance;
        public static APIKeyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<APIKeyManager>();
                }
                return _instance;
            }
        }

        private const string CLAUDE_KEY_PREF = "EncryptedClaudeAPIKey";
        private const string OPENAI_KEY_PREF = "EncryptedOpenAIAPIKey";
        private const string ELEVEN_KEY_PREF = "EncryptedElevenLabsAPIKey";
        private const string ENCRYPTION_KEY = "fps_trump_api_keys_2026";

        private string claudeAPIKey;
        private string openAIAPIKey;
        private string elevenLabsAPIKey;

        [Header("Status")]
        [SerializeField] private bool claudeKeyLoaded = false;
        [SerializeField] private bool openAIKeyLoaded = false;
        [SerializeField] private bool elevenLabsKeyLoaded = false;

        private Dictionary<string, string> envVariables = new Dictionary<string, string>();

        private void Awake()
        {
            // シングルトン設定
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // .envファイルを読み込み
            LoadDotEnvFile();

            // APIキーを読み込み
            LoadAPIKeys();
        }

        /// <summary>
        /// .envファイルを読み込み
        /// </summary>
        private void LoadDotEnvFile()
        {
            // プロジェクトルートの.envファイルを探す
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.dataPath, "..", ".env"),
                Path.Combine(Application.dataPath, "..", "..", ".env")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Debug.Log($"[APIKeyManager] Loading .env file from: {path}");

                    try
                    {
                        string[] lines = File.ReadAllLines(path);
                        foreach (string line in lines)
                        {
                            // 空行やコメント行をスキップ
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;

                            // KEY=VALUE形式をパース
                            int separatorIndex = line.IndexOf('=');
                            if (separatorIndex > 0)
                            {
                                string key = line.Substring(0, separatorIndex).Trim();
                                string value = line.Substring(separatorIndex + 1).Trim();

                                // クォート削除
                                value = value.Trim('"', '\'');

                                envVariables[key] = value;
                                Debug.Log($"[APIKeyManager] Loaded {key} from .env file ({value.Length} chars)");
                            }
                        }

                        Debug.Log($"[APIKeyManager] Successfully loaded {envVariables.Count} environment variables from .env file");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[APIKeyManager] Failed to read .env file: {ex.Message}");
                    }
                }
            }

            Debug.LogWarning("[APIKeyManager] No .env file found in project root");
        }

        /// <summary>
        /// APIキーを読み込み（.envファイル → 環境変数 → PlayerPrefs の優先順位）
        /// </summary>
        public void LoadAPIKeys()
        {
            // Claude API Key
            claudeAPIKey = LoadAPIKey("CLAUDE_API_KEY", CLAUDE_KEY_PREF);
            claudeKeyLoaded = !string.IsNullOrEmpty(claudeAPIKey);

            // OpenAI API Key
            openAIAPIKey = LoadAPIKey("OPENAI_API_KEY", OPENAI_KEY_PREF);
            openAIKeyLoaded = !string.IsNullOrEmpty(openAIAPIKey);

            // ElevenLabs API Key
            elevenLabsAPIKey = LoadAPIKey("ELEVEN_API_KEY", ELEVEN_KEY_PREF);
            elevenLabsKeyLoaded = !string.IsNullOrEmpty(elevenLabsAPIKey);

            Debug.Log($"[APIKeyManager] Keys loaded - Claude: {claudeKeyLoaded}, OpenAI: {openAIKeyLoaded}, ElevenLabs: {elevenLabsKeyLoaded}");
        }

        /// <summary>
        /// 単一のAPIキーを読み込み（.envファイル → 環境変数 → PlayerPrefs）
        /// </summary>
        private string LoadAPIKey(string envVarName, string prefKey)
        {
            // 優先度1: .envファイル
            if (envVariables.ContainsKey(envVarName))
            {
                string envValue = envVariables[envVarName];
                if (!string.IsNullOrEmpty(envValue))
                {
                    Debug.Log($"[APIKeyManager] Using {envVarName} from .env file ({envValue.Length} chars)");
                    return envValue;
                }
            }

            // 優先度2: システム環境変数
            string sysEnvValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(sysEnvValue))
            {
                Debug.Log($"[APIKeyManager] Using {envVarName} from system environment variable ({sysEnvValue.Length} chars)");
                return sysEnvValue;
            }

            // 優先度3: PlayerPrefs（暗号化）
            if (PlayerPrefs.HasKey(prefKey))
            {
                string encrypted = PlayerPrefs.GetString(prefKey);
                string decrypted = EncryptionUtil.Decrypt(encrypted, ENCRYPTION_KEY);

                if (!string.IsNullOrEmpty(decrypted))
                {
                    Debug.Log($"[APIKeyManager] Using {envVarName} from PlayerPrefs ({decrypted.Length} chars)");
                    return decrypted;
                }
            }

            Debug.LogWarning($"[APIKeyManager] {envVarName} not found");
            return null;
        }

        /// <summary>
        /// Claude APIキーを保存
        /// </summary>
        public void SaveClaudeAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[APIKeyManager] Cannot save empty Claude API key");
                return;
            }

            string encrypted = EncryptionUtil.Encrypt(key, ENCRYPTION_KEY);
            PlayerPrefs.SetString(CLAUDE_KEY_PREF, encrypted);
            PlayerPrefs.Save();

            claudeAPIKey = key;
            claudeKeyLoaded = true;

            Debug.Log($"[APIKeyManager] Claude API key saved ({key.Length} chars)");
        }

        /// <summary>
        /// OpenAI APIキーを保存
        /// </summary>
        public void SaveOpenAIAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[APIKeyManager] Cannot save empty OpenAI API key");
                return;
            }

            string encrypted = EncryptionUtil.Encrypt(key, ENCRYPTION_KEY);
            PlayerPrefs.SetString(OPENAI_KEY_PREF, encrypted);
            PlayerPrefs.Save();

            openAIAPIKey = key;
            openAIKeyLoaded = true;

            Debug.Log($"[APIKeyManager] OpenAI API key saved ({key.Length} chars)");
        }

        /// <summary>
        /// Claude APIキーを取得
        /// </summary>
        public string GetClaudeAPIKey()
        {
            return claudeAPIKey;
        }

        /// <summary>
        /// OpenAI APIキーを取得
        /// </summary>
        public string GetOpenAIAPIKey()
        {
            return openAIAPIKey;
        }

        /// <summary>
        /// Claude APIキーが設定されているかチェック
        /// </summary>
        public bool HasClaudeAPIKey()
        {
            return claudeKeyLoaded && !string.IsNullOrEmpty(claudeAPIKey);
        }

        /// <summary>
        /// OpenAI APIキーが設定されているかチェック
        /// </summary>
        public bool HasOpenAIAPIKey()
        {
            return openAIKeyLoaded && !string.IsNullOrEmpty(openAIAPIKey);
        }

        /// <summary>
        /// ElevenLabs APIキーを保存
        /// </summary>
        public void SaveElevenLabsAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[APIKeyManager] Cannot save empty ElevenLabs API key");
                return;
            }

            string encrypted = EncryptionUtil.Encrypt(key, ENCRYPTION_KEY);
            PlayerPrefs.SetString(ELEVEN_KEY_PREF, encrypted);
            PlayerPrefs.Save();

            elevenLabsAPIKey = key;
            elevenLabsKeyLoaded = true;

            Debug.Log($"[APIKeyManager] ElevenLabs API key saved ({key.Length} chars)");
        }

        /// <summary>
        /// ElevenLabs APIキーを取得
        /// </summary>
        public string GetElevenLabsAPIKey()
        {
            return elevenLabsAPIKey;
        }

        /// <summary>
        /// ElevenLabs APIキーが設定されているかチェック
        /// </summary>
        public bool HasElevenLabsAPIKey()
        {
            return elevenLabsKeyLoaded && !string.IsNullOrEmpty(elevenLabsAPIKey);
        }

        /// <summary>
        /// ElevenLabs APIキー検証（フォーマットチェック）
        /// </summary>
        public bool ValidateElevenLabsAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return key.Length > 20;
        }

        /// <summary>
        /// すべてのAPIキーが設定されているかチェック
        /// </summary>
        public bool HasAllAPIKeys()
        {
            return HasClaudeAPIKey() && HasOpenAIAPIKey();
        }

        /// <summary>
        /// APIキーをクリア
        /// </summary>
        public void ClearAPIKeys()
        {
            PlayerPrefs.DeleteKey(CLAUDE_KEY_PREF);
            PlayerPrefs.DeleteKey(OPENAI_KEY_PREF);
            PlayerPrefs.DeleteKey(ELEVEN_KEY_PREF);
            PlayerPrefs.Save();

            claudeAPIKey = null;
            openAIAPIKey = null;
            elevenLabsAPIKey = null;
            claudeKeyLoaded = false;
            openAIKeyLoaded = false;
            elevenLabsKeyLoaded = false;

            Debug.Log("[APIKeyManager] API keys cleared");
        }

        /// <summary>
        /// APIキー検証（フォーマットチェック）
        /// </summary>
        public bool ValidateClaudeAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // Claude API Key format: sk-ant-api03-...
            return key.StartsWith("sk-ant-") && key.Length > 20;
        }

        /// <summary>
        /// OpenAI APIキー検証（フォーマットチェック）
        /// </summary>
        public bool ValidateOpenAIAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            // OpenAI API Key format: sk-...
            return key.StartsWith("sk-") && key.Length > 20;
        }

        /// <summary>
        /// LLMManagerにAPIキーを設定（静的メソッド経由）
        /// </summary>
        public void ApplyAPIKeysToLLMManager()
        {
            if (!HasAllAPIKeys())
            {
                Debug.LogWarning("[APIKeyManager] Not all API keys are set. LLMManager may use fallback.");
                return;
            }

            // LLMManagerは環境変数を優先するため、
            // ここでは何もしない。LLMManagerのAwake()で
            // APIKeyManagerから読み込むようにLLMManagerを修正する必要がある。

            Debug.Log("[APIKeyManager] API keys are ready for LLMManager");
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public string GetStatus()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== API Key Manager Status ===");
            sb.AppendLine($"Claude API Key: {(claudeKeyLoaded ? "✅ Loaded" : "❌ Not set")}");
            sb.AppendLine($"OpenAI API Key: {(openAIKeyLoaded ? "✅ Loaded" : "❌ Not set")}");
            sb.AppendLine($"ElevenLabs API Key: {(elevenLabsKeyLoaded ? "✅ Loaded" : "❌ Not set")}");

            if (claudeKeyLoaded)
                sb.AppendLine($"  Claude Key Length: {claudeAPIKey?.Length} chars");

            if (openAIKeyLoaded)
                sb.AppendLine($"  OpenAI Key Length: {openAIAPIKey?.Length} chars");

            if (elevenLabsKeyLoaded)
                sb.AppendLine($"  ElevenLabs Key Length: {elevenLabsAPIKey?.Length} chars");

            return sb.ToString();
        }
    }
}
