using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// Claude API通信ラッパー
    /// Claude API (Anthropic API) との通信を管理
    /// </summary>
    public class ClaudeAPIClient
    {
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string API_VERSION = "2023-06-01";

        private readonly string apiKey;
        private readonly string model;

        public ClaudeAPIClient(string apiKey, string model = "claude-sonnet-4-5-20250929")
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }

            this.apiKey = apiKey;
            this.model = model;
        }

        /// <summary>
        /// Claude APIにリクエストを送信してダイアログを生成
        /// </summary>
        public async Task<ClaudeResponse> SendRequestAsync(ClaudeRequest request)
        {
            try
            {
                string jsonRequest = JsonConvert.SerializeObject(request);

                using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    // ヘッダー設定
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("x-api-key", apiKey);
                    www.SetRequestHeader("anthropic-version", API_VERSION);

                    // リクエスト送信
                    var operation = www.SendWebRequest();

                    // 非同期待機
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    // エラーハンドリング
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        string errorMessage = $"Claude API Error: {www.error}\nResponse: {www.downloadHandler.text}";
                        Debug.LogError(errorMessage);
                        throw new ClaudeAPIException(errorMessage, www.responseCode);
                    }

                    // レスポンスパース
                    string responseText = www.downloadHandler.text;
                    ClaudeResponse response = JsonConvert.DeserializeObject<ClaudeResponse>(responseText);

                    if (response == null || response.content == null || response.content.Length == 0)
                    {
                        throw new ClaudeAPIException("Invalid response from Claude API", www.responseCode);
                    }

                    return response;
                }
            }
            catch (ClaudeAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in Claude API request: {ex.Message}");
                throw new ClaudeAPIException($"Unexpected error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// ダイアログ生成のヘルパーメソッド
        /// </summary>
        public async Task<string> GenerateDialogueAsync(
            string prompt,
            int maxTokens = 150,
            float temperature = 0.8f)
        {
            ClaudeRequest request = new ClaudeRequest
            {
                model = this.model,
                max_tokens = maxTokens,
                temperature = temperature,
                messages = new ClaudeMessage[]
                {
                    new ClaudeMessage
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            ClaudeResponse response = await SendRequestAsync(request);

            if (response.content != null && response.content.Length > 0)
            {
                return response.content[0].text;
            }

            return string.Empty;
        }

        /// <summary>
        /// Vision対応ダイアログ生成（画像+テキストプロンプト）
        /// Claude Messages APIのcontent配列形式を使用
        /// </summary>
        public async Task<string> GenerateVisionDialogueAsync(
            string base64Image,
            string mediaType,
            string textPrompt,
            int maxTokens = 200,
            float temperature = 0.8f)
        {
            // Vision用はcontent配列形式でリクエストを手動構築
            var requestObj = new
            {
                model = this.model,
                max_tokens = maxTokens,
                temperature = temperature,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image",
                                source = new
                                {
                                    type = "base64",
                                    media_type = mediaType,
                                    data = base64Image
                                }
                            },
                            new
                            {
                                type = "text",
                                text = textPrompt
                            }
                        }
                    }
                }
            };

            try
            {
                string jsonRequest = JsonConvert.SerializeObject(requestObj);

                using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("x-api-key", apiKey);
                    www.SetRequestHeader("anthropic-version", API_VERSION);

                    var operation = www.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        string errorMessage = $"Claude Vision API Error: {www.error}\nResponse: {www.downloadHandler.text}";
                        Debug.LogError(errorMessage);
                        throw new ClaudeAPIException(errorMessage, www.responseCode);
                    }

                    string responseText = www.downloadHandler.text;
                    ClaudeResponse response = JsonConvert.DeserializeObject<ClaudeResponse>(responseText);

                    if (response?.content != null && response.content.Length > 0)
                    {
                        return response.content[0].text;
                    }

                    return string.Empty;
                }
            }
            catch (ClaudeAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in Claude Vision API request: {ex.Message}");
                throw new ClaudeAPIException($"Unexpected error: {ex.Message}", 0);
            }
        }
    }

    // ===== データ構造 =====

    [Serializable]
    public class ClaudeRequest
    {
        public string model;
        public int max_tokens;
        public float temperature;
        public ClaudeMessage[] messages;
    }

    [Serializable]
    public class ClaudeMessage
    {
        public string role; // "user" or "assistant"
        public string content;
    }

    [Serializable]
    public class ClaudeResponse
    {
        public string id;
        public string type;
        public string role;
        public ClaudeContent[] content;
        public string model;
        public string stop_reason;
        public ClaudeUsage usage;
    }

    [Serializable]
    public class ClaudeContent
    {
        public string type; // "text"
        public string text;
    }

    [Serializable]
    public class ClaudeUsage
    {
        public int input_tokens;
        public int output_tokens;
    }

    // ===== 例外クラス =====

    public class ClaudeAPIException : Exception
    {
        public long ResponseCode { get; }

        public ClaudeAPIException(string message, long responseCode) : base(message)
        {
            ResponseCode = responseCode;
        }
    }
}
