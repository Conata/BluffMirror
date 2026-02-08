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
    /// OpenAI API通信ラッパー（TTS特化）
    /// Text-to-Speech および フォールバック用ダイアログ生成
    /// </summary>
    public class OpenAIAPIClient
    {
        private const string TTS_API_URL = "https://api.openai.com/v1/audio/speech";
        private const string CHAT_API_URL = "https://api.openai.com/v1/chat/completions";

        private readonly string apiKey;

        public OpenAIAPIClient(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }

            this.apiKey = apiKey;
        }

        /// <summary>
        /// Text-to-Speech: テキストから音声を生成
        /// </summary>
        public async Task<AudioClip> GenerateTTSAsync(
            string text,
            TTSVoice voice = TTSVoice.Alloy,
            string model = "tts-1",
            float speed = 0.9f)
        {
            try
            {
                // リクエスト構築
                TTSRequest request = new TTSRequest
                {
                    model = model,
                    input = text,
                    voice = voice.ToString().ToLower(),
                    speed = speed
                };

                string jsonRequest = JsonConvert.SerializeObject(request);

                using (UnityWebRequest www = new UnityWebRequest(TTS_API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    // ヘッダー設定
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

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
                        string errorMessage = $"OpenAI TTS Error: {www.error}\nResponse: {www.downloadHandler.text}";
                        Debug.LogError(errorMessage);
                        throw new OpenAIAPIException(errorMessage, www.responseCode);
                    }

                    // MP3データをAudioClipに変換
                    byte[] audioData = www.downloadHandler.data;
                    AudioClip audioClip = await ConvertMP3ToAudioClip(audioData);

                    return audioClip;
                }
            }
            catch (OpenAIAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in OpenAI TTS request: {ex.Message}");
                throw new OpenAIAPIException($"Unexpected error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// フォールバック用ダイアログ生成（Chat API）
        /// </summary>
        public async Task<string> GenerateDialogueAsync(
            string prompt,
            int maxTokens = 150,
            float temperature = 0.8f)
        {
            try
            {
                ChatRequest request = new ChatRequest
                {
                    model = "gpt-4o-mini",
                    max_tokens = maxTokens,
                    temperature = temperature,
                    messages = new ChatMessage[]
                    {
                        new ChatMessage
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                };

                string jsonRequest = JsonConvert.SerializeObject(request);

                using (UnityWebRequest www = new UnityWebRequest(CHAT_API_URL, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    // ヘッダー設定
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

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
                        string errorMessage = $"OpenAI Chat Error: {www.error}\nResponse: {www.downloadHandler.text}";
                        Debug.LogError(errorMessage);
                        throw new OpenAIAPIException(errorMessage, www.responseCode);
                    }

                    // レスポンスパース
                    string responseText = www.downloadHandler.text;
                    ChatResponse response = JsonConvert.DeserializeObject<ChatResponse>(responseText);

                    if (response?.choices != null && response.choices.Length > 0)
                    {
                        return response.choices[0].message.content;
                    }

                    throw new OpenAIAPIException("Invalid response from OpenAI Chat API", www.responseCode);
                }
            }
            catch (OpenAIAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in OpenAI Chat request: {ex.Message}");
                throw new OpenAIAPIException($"Unexpected error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// MP3バイトデータをAudioClipに変換
        /// Note: UnityはネイティブではMP3デコードに対応していないため、
        /// 実際にはWAVに変換するか、外部プラグインを使用する必要があります。
        /// ここでは簡易実装として、一時ファイル経由でロードします。
        /// </summary>
        private async Task<AudioClip> ConvertMP3ToAudioClip(byte[] mp3Data)
        {
            // 一時ファイルに保存
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"tts_{Guid.NewGuid()}.mp3");

            try
            {
                await System.IO.File.WriteAllBytesAsync(tempPath, mp3Data);

                // UnityWebRequestでMP3をロード
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file://{tempPath}", AudioType.MPEG))
                {
                    var operation = www.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        throw new OpenAIAPIException($"Failed to load audio clip: {www.error}", www.responseCode);
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    return clip;
                }
            }
            finally
            {
                // クリーンアップ
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }

    // ===== TTS データ構造 =====

    [Serializable]
    public class TTSRequest
    {
        public string model;
        public string input;
        public string voice;
        public float speed;
    }

    public enum TTSVoice
    {
        Alloy,      // Neutral
        Echo,       // Confident, clear
        Fable,      // British accent
        Onyx,       // Deep, authoritative
        Nova,       // Light, playful
        Shimmer     // Soft, warm
    }

    // ===== Chat データ構造 =====

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public int max_tokens;
        public float temperature;
        public ChatMessage[] messages;
    }

    [Serializable]
    public class ChatMessage
    {
        public string role; // "user" or "assistant"
        public string content;
    }

    [Serializable]
    public class ChatResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public ChatChoice[] choices;
        public ChatUsage usage;
    }

    [Serializable]
    public class ChatChoice
    {
        public int index;
        public ChatMessage message;
        public string finish_reason;
    }

    [Serializable]
    public class ChatUsage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    // ===== 例外クラス =====

    public class OpenAIAPIException : Exception
    {
        public long ResponseCode { get; }

        public OpenAIAPIException(string message, long responseCode) : base(message)
        {
            ResponseCode = responseCode;
        }
    }
}
