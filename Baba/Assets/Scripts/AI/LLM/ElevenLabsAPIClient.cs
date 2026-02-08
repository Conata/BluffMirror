using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// TTSプロバイダ選択
    /// </summary>
    public enum TTSProvider
    {
        OpenAI,
        ElevenLabs
    }

    /// <summary>
    /// ElevenLabs API通信ラッパー（TTS特化）
    /// </summary>
    public class ElevenLabsAPIClient
    {
        private const string TTS_API_URL_TEMPLATE = "https://api.elevenlabs.io/v1/text-to-speech/{0}";

        private readonly string apiKey;

        public ElevenLabsAPIClient(string apiKey)
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
            string voiceId,
            ElevenLabsVoiceSettings voiceSettings,
            string modelId = "eleven_multilingual_v2")
        {
            if (string.IsNullOrEmpty(voiceId))
            {
                throw new ArgumentException("Voice ID cannot be null or empty", nameof(voiceId));
            }

            try
            {
                string url = string.Format(TTS_API_URL_TEMPLATE, voiceId);

                ElevenLabsTTSRequest request = new ElevenLabsTTSRequest
                {
                    text = text,
                    model_id = modelId,
                    voice_settings = voiceSettings
                };

                string jsonRequest = JsonConvert.SerializeObject(request);

                using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new DownloadHandlerBuffer();

                    // ElevenLabs ヘッダー設定
                    www.SetRequestHeader("xi-api-key", apiKey);
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("Accept", "audio/mpeg");

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
                        string errorMessage = $"ElevenLabs TTS Error: {www.error}\nResponse: {www.downloadHandler.text}";
                        Debug.LogError(errorMessage);
                        throw new ElevenLabsAPIException(errorMessage, www.responseCode);
                    }

                    // MP3データをAudioClipに変換
                    byte[] audioData = www.downloadHandler.data;
                    AudioClip audioClip = await ConvertMP3ToAudioClip(audioData);

                    return audioClip;
                }
            }
            catch (ElevenLabsAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in ElevenLabs TTS request: {ex.Message}");
                throw new ElevenLabsAPIException($"Unexpected error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// MP3バイトデータをAudioClipに変換
        /// </summary>
        private async Task<AudioClip> ConvertMP3ToAudioClip(byte[] mp3Data)
        {
            string tempPath = System.IO.Path.Combine(
                Application.temporaryCachePath, $"tts_el_{Guid.NewGuid()}.mp3");

            try
            {
                await System.IO.File.WriteAllBytesAsync(tempPath, mp3Data);

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
                    $"file://{tempPath}", AudioType.MPEG))
                {
                    var operation = www.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        throw new ElevenLabsAPIException(
                            $"Failed to load audio clip: {www.error}", www.responseCode);
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    return clip;
                }
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }

    // ===== ElevenLabs データ構造 =====

    [Serializable]
    public class ElevenLabsVoiceSettings
    {
        public float stability;
        public float similarity_boost;
        public float style;
        public bool use_speaker_boost;
    }

    [Serializable]
    public class ElevenLabsTTSRequest
    {
        public string text;
        public string model_id;
        public ElevenLabsVoiceSettings voice_settings;
    }

    // ===== 例外クラス =====

    public class ElevenLabsAPIException : Exception
    {
        public long ResponseCode { get; }

        public ElevenLabsAPIException(string message, long responseCode) : base(message)
        {
            ResponseCode = responseCode;
        }
    }
}
