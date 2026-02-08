using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Sentis;

/// <summary>
/// Stage 10 Part B: リアルタイム表情分析（Unity Sentis + FERPlus-8 ONNX）
/// WebCamManagerからフレームを取得し、Sentis推論で表情を分類する。
/// Singletonパターン。
/// </summary>
public class FacialExpressionAnalyzer : MonoBehaviour
{
    public static FacialExpressionAnalyzer Instance { get; private set; }

    [Header("Model")]
    [SerializeField] private ModelAsset emotionModelAsset;

    [Header("Inference Settings")]
    [SerializeField] private float normalInterval = 2.0f;
    [SerializeField] private float keyMomentInterval = 0.5f;
    [SerializeField] private BackendType backendType = BackendType.GPUCompute;

    private Model runtimeModel;
    private Worker worker;
    private float lastInferenceTime;
    private bool isKeyMoment;
    private bool isInitialized;

    // FERPlus-8 output class labels
    private static readonly string[] EmotionLabels = {
        "Neutral", "Happy", "Surprise", "Sad",
        "Angry", "Disgusted", "Fearful", "Contempt"
    };

    // 表情変化追跡用
    private readonly Queue<FacialExpression> recentExpressions = new Queue<FacialExpression>();
    private const int EXPRESSION_HISTORY_WINDOW = 10;
    private readonly Dictionary<FacialExpression, int> expressionCounts = new Dictionary<FacialExpression, int>();

    /// <summary>現在のプレイヤー表情状態</summary>
    public PlayerFacialState CurrentState { get; private set; } = new PlayerFacialState();

    /// <summary>分析が有効か（カメラ＋モデル両方利用可能）</summary>
    public bool IsActive => isInitialized && WebCamManager.Instance != null && WebCamManager.Instance.IsCapturing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeModel();
    }

    private void OnDestroy()
    {
        worker?.Dispose();
        if (Instance == this) Instance = null;
    }

    private void InitializeModel()
    {
        if (emotionModelAsset == null)
        {
            Debug.LogWarning("[FacialExpressionAnalyzer] No model asset assigned. Assign emotion-ferplus-8 in Inspector.");
            return;
        }

        try
        {
            runtimeModel = ModelLoader.Load(emotionModelAsset);
            worker = new Worker(runtimeModel, backendType);
            isInitialized = true;
            Debug.Log("[FacialExpressionAnalyzer] Model loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FacialExpressionAnalyzer] Failed to load model: {ex.Message}");
            isInitialized = false;
        }
    }

    private void Update()
    {
        if (!IsActive) return;

        float interval = isKeyMoment ? keyMomentInterval : normalInterval;
        if (Time.time - lastInferenceTime < interval) return;

        lastInferenceTime = Time.time;
        RunInference();
    }

    /// <summary>
    /// キーモーメント（カード選択中など）の推論頻度を変更
    /// </summary>
    public void SetKeyMoment(bool active)
    {
        isKeyMoment = active;
    }

    /// <summary>
    /// Sentis推論を実行して表情を分類
    /// </summary>
    private void RunInference()
    {
        var webCam = WebCamManager.Instance;
        if (webCam == null) return;

        Texture2D frame = webCam.CaptureFrameAsTexture();
        if (frame == null) return;

        try
        {
            // グレースケール64x64に変換
            Texture2D grayscale = ConvertToGrayscale64(frame);
            Destroy(frame);

            // テンソル作成（1x1x64x64）
            using Tensor<float> inputTensor = TextureConverter.ToTensor(grayscale, width: 64, height: 64, channels: 1);
            Destroy(grayscale);

            // 推論実行
            worker.Schedule(inputTensor);

            // 出力取得（同期・軽量モデルなので問題なし）
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            if (outputTensor == null) return;

            using var cpuTensor = outputTensor.ReadbackAndClone();
            float[] probabilities = cpuTensor.DownloadToArray();

            // softmax（モデルが生logitsを出力する場合）
            float[] softmaxed = Softmax(probabilities);

            // 最大クラスを取得
            int maxIndex = 0;
            float maxProb = softmaxed[0];
            for (int i = 1; i < softmaxed.Length; i++)
            {
                if (softmaxed[i] > maxProb)
                {
                    maxProb = softmaxed[i];
                    maxIndex = i;
                }
            }

            FacialExpression detected = (FacialExpression)maxIndex;

            // 履歴更新
            UpdateExpressionHistory(detected);

            // 状態更新
            CurrentState = new PlayerFacialState
            {
                currentExpression = detected,
                confidence = maxProb,
                expressionChangeRate = CalculateChangeRate(),
                dominantExpression = GetDominantExpression(),
                expressionHistory = new Dictionary<FacialExpression, int>(expressionCounts)
            };

            Debug.Log($"[FacialExpressionAnalyzer] {EmotionLabels[maxIndex]}: {maxProb:F2}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FacialExpressionAnalyzer] Inference failed: {ex.Message}");
            Destroy(frame);
        }
    }

    /// <summary>
    /// RGB Texture2Dをグレースケール64x64に変換
    /// </summary>
    private Texture2D ConvertToGrayscale64(Texture2D source)
    {
        // リサイズ用RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(64, 64);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D resized = new Texture2D(64, 64, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        resized.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // グレースケール変換
        Color[] pixels = resized.GetPixels();
        Texture2D grayscale = new Texture2D(64, 64, TextureFormat.R8, false);
        Color[] grayPixels = new Color[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            float gray = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
            grayPixels[i] = new Color(gray, gray, gray, 1f);
        }
        grayscale.SetPixels(grayPixels);
        grayscale.Apply();

        Destroy(resized);
        return grayscale;
    }

    /// <summary>
    /// Softmax計算（数値安定版）
    /// </summary>
    private float[] Softmax(float[] logits)
    {
        float max = logits.Max();
        float[] exps = logits.Select(x => Mathf.Exp(x - max)).ToArray();
        float sum = exps.Sum();
        return exps.Select(x => x / sum).ToArray();
    }

    private void UpdateExpressionHistory(FacialExpression expression)
    {
        recentExpressions.Enqueue(expression);
        if (recentExpressions.Count > EXPRESSION_HISTORY_WINDOW)
        {
            recentExpressions.Dequeue();
        }

        if (!expressionCounts.ContainsKey(expression))
            expressionCounts[expression] = 0;
        expressionCounts[expression]++;
    }

    /// <summary>
    /// 直近の表情変化頻度（0=安定, 1=頻繁に変化）
    /// </summary>
    private float CalculateChangeRate()
    {
        if (recentExpressions.Count < 2) return 0f;

        var list = recentExpressions.ToArray();
        int changes = 0;
        for (int i = 1; i < list.Length; i++)
        {
            if (list[i] != list[i - 1]) changes++;
        }

        return (float)changes / (list.Length - 1);
    }

    private FacialExpression GetDominantExpression()
    {
        if (expressionCounts.Count == 0) return FacialExpression.Neutral;
        return expressionCounts.OrderByDescending(kv => kv.Value).First().Key;
    }

    /// <summary>
    /// 表情の日本語名を取得（AIの台詞生成用）
    /// </summary>
    public static string GetExpressionNameJP(FacialExpression expression)
    {
        return expression switch
        {
            FacialExpression.Neutral => "無表情",
            FacialExpression.Happy => "笑顔",
            FacialExpression.Surprise => "驚き",
            FacialExpression.Sad => "悲しみ",
            FacialExpression.Angry => "怒り",
            FacialExpression.Disgusted => "嫌悪",
            FacialExpression.Fearful => "恐怖",
            FacialExpression.Contempt => "軽蔑",
            _ => "不明"
        };
    }

    /// <summary>
    /// 表情の英語名を取得
    /// </summary>
    public static string GetExpressionNameEN(FacialExpression expression)
    {
        return expression switch
        {
            FacialExpression.Neutral => "neutral",
            FacialExpression.Happy => "smiling",
            FacialExpression.Surprise => "surprised",
            FacialExpression.Sad => "sad",
            FacialExpression.Angry => "angry",
            FacialExpression.Disgusted => "disgusted",
            FacialExpression.Fearful => "fearful",
            FacialExpression.Contempt => "contemptuous",
            _ => "unknown"
        };
    }
}

// ===== データ構造 =====

/// <summary>
/// 表情分類（FERPlus-8の8クラス）
/// </summary>
public enum FacialExpression
{
    Neutral,    // 無表情
    Happy,      // 笑顔
    Surprise,   // 驚き
    Sad,        // 悲しみ
    Angry,      // 怒り
    Disgusted,  // 嫌悪
    Fearful,    // 恐怖
    Contempt    // 軽蔑
}

/// <summary>
/// プレイヤーの表情状態（リアルタイム）
/// </summary>
[Serializable]
public class PlayerFacialState
{
    public FacialExpression currentExpression = FacialExpression.Neutral;
    public float confidence;
    public float expressionChangeRate;
    public FacialExpression dominantExpression = FacialExpression.Neutral;
    public Dictionary<FacialExpression, int> expressionHistory = new Dictionary<FacialExpression, int>();
}
