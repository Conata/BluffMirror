using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stage 10: WebCamManagerー デバイスカメラのアクセス・フレームキャプチャ管理
/// Singletonパターン。イントロ時の1枚撮影 + ゲーム中の連続キャプチャ対応。
/// </summary>
public class WebCamManager : MonoBehaviour
{
    public static WebCamManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int requestedWidth = 640;
    [SerializeField] private int requestedHeight = 480;
    [SerializeField] private int requestedFPS = 15;
    [SerializeField] private int jpegQuality = 75;

    private WebCamTexture webCamTexture;
    private bool isCapturing;

    /// <summary>カメラが利用可能か</summary>
    public bool HasCamera => WebCamTexture.devices.Length > 0;

    /// <summary>カメラが稼働中か</summary>
    public bool IsCapturing => isCapturing && webCamTexture != null && webCamTexture.isPlaying;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        StopCapture();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// カメラ起動
    /// </summary>
    public bool StartCapture()
    {
        if (isCapturing) return true;

        if (!HasCamera)
        {
            Debug.LogWarning("[WebCamManager] No camera device found");
            return false;
        }

        try
        {
            WebCamDevice device = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(device.name, requestedWidth, requestedHeight, requestedFPS);
            webCamTexture.Play();
            isCapturing = true;
            Debug.Log($"[WebCamManager] Camera started: {device.name} ({webCamTexture.width}x{webCamTexture.height})");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebCamManager] Failed to start camera: {ex.Message}");
            isCapturing = false;
            return false;
        }
    }

    /// <summary>
    /// カメラ停止
    /// </summary>
    public void StopCapture()
    {
        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
                webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }
        isCapturing = false;
        Debug.Log("[WebCamManager] Camera stopped");
    }

    /// <summary>
    /// 現在のフレームをJPEG base64文字列として取得
    /// </summary>
    public string CaptureFrameAsBase64()
    {
        if (!IsCapturing)
        {
            Debug.LogWarning("[WebCamManager] Camera not capturing, cannot capture frame");
            return null;
        }

        try
        {
            Texture2D frame = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            frame.SetPixels(webCamTexture.GetPixels());
            frame.Apply();

            byte[] jpegData = frame.EncodeToJPG(jpegQuality);
            Destroy(frame);

            string base64 = Convert.ToBase64String(jpegData);
            Debug.Log($"[WebCamManager] Frame captured: {jpegData.Length} bytes, base64 length: {base64.Length}");
            return base64;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebCamManager] Frame capture failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 現在のフレームをTexture2Dとして取得（Sentis推論用）
    /// </summary>
    public Texture2D CaptureFrameAsTexture()
    {
        if (!IsCapturing)
        {
            Debug.LogWarning("[WebCamManager] Camera not capturing");
            return null;
        }

        try
        {
            Texture2D frame = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            frame.SetPixels(webCamTexture.GetPixels());
            frame.Apply();
            return frame;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebCamManager] Texture capture failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// カメラ起動して安定するまで待ってから1枚撮影しbase64を返す
    /// 失敗時はnullを返す（graceful fallback用）
    /// </summary>
    public async Task<string> CaptureOneFrameAsync(float stabilizationDelay = 0.5f)
    {
        if (!StartCapture())
            return null;

        // フレーム安定待ち
        float elapsed = 0f;
        while (elapsed < stabilizationDelay)
        {
            await Task.Yield();
            elapsed += Time.deltaTime;
        }

        // フレーム取得確認（WebCamTextureの初期化完了待ち）
        int retries = 0;
        while (!webCamTexture.didUpdateThisFrame && retries < 30)
        {
            await Task.Yield();
            retries++;
        }

        string base64 = CaptureFrameAsBase64();
        return base64;
    }
}
