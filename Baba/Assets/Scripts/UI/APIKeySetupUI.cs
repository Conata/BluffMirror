using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FPSTrump.Manager;

namespace FPSTrump.UI
{
    /// <summary>
    /// APIキー設定UI
    /// ゲーム開始前にAPIキーを入力・保存する画面
    /// </summary>
    public class APIKeySetupUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField claudeAPIKeyInput;
        [SerializeField] private TMP_InputField openAIAPIKeyInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject birthdayPanel;
        [SerializeField] private GameObject readyPanel;

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Visual Feedback")]
        [SerializeField] private Color validColor = Color.green;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;

        private APIKeyManager apiKeyManager;

        private void Start()
        {
            // APIKeyManagerを取得または作成
            apiKeyManager = APIKeyManager.Instance;
            if (apiKeyManager == null)
            {
                GameObject managerObj = new GameObject("APIKeyManager");
                apiKeyManager = managerObj.AddComponent<APIKeyManager>();
            }

            // ボタンイベント設定
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);

            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipButtonClicked);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);

            // 既存のキーをロード
            LoadExistingKeys();

            // 初期状態チェック
            CheckAPIKeyStatus();
        }

        /// <summary>
        /// 既存のAPIキーを読み込み（マスク表示）
        /// </summary>
        private void LoadExistingKeys()
        {
            if (apiKeyManager.HasClaudeAPIKey())
            {
                string claudeKey = apiKeyManager.GetClaudeAPIKey();
                claudeAPIKeyInput.text = MaskAPIKey(claudeKey);
                claudeAPIKeyInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Claude API Key loaded";
            }

            if (apiKeyManager.HasOpenAIAPIKey())
            {
                string openAIKey = apiKeyManager.GetOpenAIAPIKey();
                openAIAPIKeyInput.text = MaskAPIKey(openAIKey);
                openAIAPIKeyInput.placeholder.GetComponent<TextMeshProUGUI>().text = "OpenAI API Key loaded";
            }
        }

        /// <summary>
        /// APIキーをマスク表示（セキュリティ）
        /// </summary>
        private string MaskAPIKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 10)
                return "****";

            // 最初の4文字と最後の4文字のみ表示
            return key.Substring(0, 4) + new string('*', key.Length - 8) + key.Substring(key.Length - 4);
        }

        /// <summary>
        /// 保存ボタンクリック
        /// </summary>
        private void OnSaveButtonClicked()
        {
            string claudeKey = claudeAPIKeyInput.text.Trim();
            string openAIKey = openAIAPIKeyInput.text.Trim();

            bool claudeValid = false;
            bool openAIValid = false;

            // Claude API Key検証・保存
            if (!string.IsNullOrEmpty(claudeKey) && !claudeKey.Contains("*"))
            {
                if (apiKeyManager.ValidateClaudeAPIKey(claudeKey))
                {
                    apiKeyManager.SaveClaudeAPIKey(claudeKey);
                    claudeValid = true;
                    Debug.Log("[APIKeySetupUI] Claude API Key saved");
                }
                else
                {
                    ShowStatus("Invalid Claude API Key format (should start with 'sk-ant-')", invalidColor);
                    return;
                }
            }
            else if (apiKeyManager.HasClaudeAPIKey())
            {
                claudeValid = true; // 既存のキーを使用
            }

            // OpenAI API Key検証・保存
            if (!string.IsNullOrEmpty(openAIKey) && !openAIKey.Contains("*"))
            {
                if (apiKeyManager.ValidateOpenAIAPIKey(openAIKey))
                {
                    apiKeyManager.SaveOpenAIAPIKey(openAIKey);
                    openAIValid = true;
                    Debug.Log("[APIKeySetupUI] OpenAI API Key saved");
                }
                else
                {
                    ShowStatus("Invalid OpenAI API Key format (should start with 'sk-')", invalidColor);
                    return;
                }
            }
            else if (apiKeyManager.HasOpenAIAPIKey())
            {
                openAIValid = true; // 既存のキーを使用
            }

            // 保存成功（どちらか1つでOK）
            if (claudeValid || openAIValid)
            {
                string message = "";
                if (claudeValid && openAIValid)
                {
                    message = "All API keys saved! ✅ Full features enabled";
                }
                else if (claudeValid)
                {
                    message = "Claude API key saved ✅ (TTS disabled without OpenAI)";
                }
                else if (openAIValid)
                {
                    message = "OpenAI API key saved ✅ (Dialogue generation limited)";
                }

                ShowStatus(message, validColor);
                StartCoroutine(ShowReadyPanel());
            }
            else
            {
                ShowStatus("No valid API keys provided", invalidColor);
            }

            CheckAPIKeyStatus();
        }

        /// <summary>
        /// スキップボタンクリック（オフラインモード）
        /// </summary>
        private void OnSkipButtonClicked()
        {
            ShowStatus("Skipped - Game will use fallback dialogue system ⚠️", warningColor);
            StartCoroutine(ShowReadyPanelWithWarning());
        }

        /// <summary>
        /// ゲーム開始ボタンクリック
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            Debug.Log("[APIKeySetupUI] Starting game...");

            // LLMManagerにAPIキーを適用
            apiKeyManager.ApplyAPIKeysToLLMManager();

            // ゲームシーンに遷移
            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("[APIKeySetupUI] Game scene name not set!");
            }
        }

        /// <summary>
        /// APIキー状態をチェック（どちらか1つでOK）
        /// </summary>
        private void CheckAPIKeyStatus()
        {
            bool hasClaude = apiKeyManager.HasClaudeAPIKey();
            bool hasOpenAI = apiKeyManager.HasOpenAIAPIKey();

            if (hasClaude || hasOpenAI)
            {
                // どちらか1つでもあれば次へ遷移
                string message = "";
                if (hasClaude && hasOpenAI)
                {
                    message = "All API keys loaded ✅ Full features enabled!";
                }
                else if (hasClaude)
                {
                    message = "Claude API ready ✅ (TTS disabled)";
                }
                else if (hasOpenAI)
                {
                    message = "OpenAI API ready ✅ (Limited dialogue)";
                }

                ShowStatus(message, validColor);

                if (setupPanel != null)
                    setupPanel.SetActive(false);

                // BirthdayPanelへ遷移（毎回表示、前回値が初期選択される）
                if (birthdayPanel != null)
                {
                    birthdayPanel.SetActive(true);
                    if (readyPanel != null)
                        readyPanel.SetActive(false);
                }
                else
                {
                    if (readyPanel != null)
                        readyPanel.SetActive(true);
                }
            }
            else
            {
                ShowStatus("No API keys configured - Please set at least one API key", warningColor);

                if (setupPanel != null)
                    setupPanel.SetActive(true);

                if (birthdayPanel != null)
                    birthdayPanel.SetActive(false);

                if (readyPanel != null)
                    readyPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 準備完了パネルを表示
        /// </summary>
        private IEnumerator ShowReadyPanel()
        {
            yield return new WaitForSeconds(1.0f);

            if (setupPanel != null)
                setupPanel.SetActive(false);

            // BirthdayPanelへ遷移（毎回表示）
            if (birthdayPanel != null)
            {
                birthdayPanel.SetActive(true);
                if (readyPanel != null)
                    readyPanel.SetActive(false);
            }
            else
            {
                if (readyPanel != null)
                    readyPanel.SetActive(true);
                ShowStatus("Ready to start! 🎮", validColor);
            }
        }

        /// <summary>
        /// 準備完了パネルを警告付きで表示
        /// </summary>
        private IEnumerator ShowReadyPanelWithWarning()
        {
            yield return new WaitForSeconds(1.0f);

            if (setupPanel != null)
                setupPanel.SetActive(false);

            // BirthdayPanelへ遷移（毎回表示）
            if (birthdayPanel != null)
            {
                birthdayPanel.SetActive(true);
                if (readyPanel != null)
                    readyPanel.SetActive(false);
            }
            else
            {
                if (readyPanel != null)
                    readyPanel.SetActive(true);
                ShowStatus("Offline mode - Limited dialogue features ⚠️", warningColor);
            }
        }

        /// <summary>
        /// ステータステキストを表示
        /// </summary>
        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }

            Debug.Log($"[APIKeySetupUI] {message}");
        }

        /// <summary>
        /// APIキー取得リンクを開く
        /// </summary>
        public void OpenClaudeAPIKeyURL()
        {
            Application.OpenURL("https://console.anthropic.com/");
        }

        public void OpenOpenAIAPIKeyURL()
        {
            Application.OpenURL("https://platform.openai.com/api-keys");
        }

        /// <summary>
        /// APIキーをクリア（デバッグ用）
        /// </summary>
        [ContextMenu("Clear All API Keys")]
        public void ClearAllAPIKeys()
        {
            apiKeyManager.ClearAPIKeys();
            claudeAPIKeyInput.text = "";
            openAIAPIKeyInput.text = "";
            ShowStatus("API Keys cleared", warningColor);
            CheckAPIKeyStatus();
        }

#if UNITY_EDITOR
        /// <summary>
        /// テスト用にダミーキーを設定
        /// </summary>
        [ContextMenu("Set Dummy API Keys (Test)")]
        public void SetDummyAPIKeys()
        {
            claudeAPIKeyInput.text = "sk-ant-api03-test-key-dummy";
            openAIAPIKeyInput.text = "sk-test-key-dummy-openai";
            ShowStatus("Dummy keys set (for testing UI only)", warningColor);
        }
#endif
    }
}
