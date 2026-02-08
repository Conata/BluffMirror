using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 3D空間に浮遊するテキストシステム
/// カードホバー時の心理圧演出に使用
/// </summary>
public class FloatingTextSystem : MonoBehaviour
{
    public static FloatingTextSystem Instance { get; private set; }

    [Header("Text Prefab")]
    [SerializeField] private GameObject textPrefab; // TextMeshPro 3Dプレハブ

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;
    private Queue<GameObject> textPool;

    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 0.5f;
    [SerializeField] private float floatDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Pressure Color Settings")]
    [SerializeField] private Color lowPressureColor = Color.white;
    [SerializeField] private Color mediumPressureColor = new Color(1f, 0.8f, 0.3f); // Orange
    [SerializeField] private Color highPressureColor = new Color(1f, 0.2f, 0.2f); // Red

    [Header("Font Settings")]
    [SerializeField] private float fontSize = 1f;
    [SerializeField] [Tooltip("Required: Use NotoSansJP-VariableFont_wght SDF for Japanese support")]
    private TMP_FontAsset fontAsset;

    // Stage 6: 永続テキスト表示用
    private GameObject currentPersistentText;

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

        InitializePool();
    }

    /// <summary>
    /// オブジェクトプール初期化
    /// </summary>
    private void InitializePool()
    {
        textPool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObj = CreateTextObject();
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
        }

        Debug.Log($"[FloatingTextSystem] Initialized pool with {poolSize} text objects");
    }

    /// <summary>
    /// TextMeshPro 3Dオブジェクト作成
    /// </summary>
    private GameObject CreateTextObject()
    {
        GameObject textObj;

        if (textPrefab != null)
        {
            // プレハブから作成
            textObj = Instantiate(textPrefab, transform);
        }
        else
        {
            // プロシージャル生成
            textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(transform);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;

            // フォント設定（必須）
            if (fontAsset == null)
            {
                Debug.LogError("[FloatingTextSystem] Font Asset is not assigned! Please assign NotoSansJP SDF font in the Inspector. Text will appear garbled.");
            }
            else
            {
                tmp.font = fontAsset;
                // フォントアセットのマテリアルを使用（テクスチャとシェーダーを含む）
                if (fontAsset.material != null)
                {
                    tmp.fontSharedMaterial = fontAsset.material;
                }
            }
        }

        return textObj;
    }

    /// <summary>
    /// 3D空間にテキストを表示
    /// </summary>
    /// <param name="worldPosition">表示位置（ワールド座標）</param>
    /// <param name="text">表示テキスト</param>
    /// <param name="pressureLevel">心理圧レベル（0.0-3.0）</param>
    public void ShowText(Vector3 worldPosition, string text, float pressureLevel = 0f)
    {
        GameObject textObj = GetPooledText();
        if (textObj == null)
        {
            Debug.LogWarning("[FloatingTextSystem] No available text objects in pool");
            return;
        }

        var tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            Debug.LogError("[FloatingTextSystem] TextMeshPro component not found");
            return;
        }

        // テキスト設定
        tmp.text = text;

        // 色設定（心理圧に応じて変化）
        tmp.color = GetColorForPressure(pressureLevel);

        // 位置設定
        textObj.transform.position = worldPosition;
        textObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // アニメーション開始
        textObj.SetActive(true);
        StartFloatingAnimation(textObj, tmp);
    }

    /// <summary>
    /// 心理圧レベルに応じた色を取得
    /// </summary>
    private Color GetColorForPressure(float pressureLevel)
    {
        if (pressureLevel < 1.0f)
        {
            // Low pressure: white
            return Color.Lerp(lowPressureColor, mediumPressureColor, pressureLevel);
        }
        else if (pressureLevel < 2.0f)
        {
            // Medium pressure: white -> orange
            return Color.Lerp(mediumPressureColor, highPressureColor, pressureLevel - 1.0f);
        }
        else
        {
            // High pressure: orange -> red
            return Color.Lerp(highPressureColor, Color.red, Mathf.Clamp01((pressureLevel - 2.0f) / 1.0f));
        }
    }

    /// <summary>
    /// 浮遊アニメーション開始
    /// </summary>
    private void StartFloatingAnimation(GameObject textObj, TextMeshPro tmp)
    {
        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * floatHeight;

        // アニメーションシーケンス
        Sequence sequence = DOTween.Sequence();

        // Phase 1: Fade In (0.3s)
        Color startColor = tmp.color;
        startColor.a = 0f;
        tmp.color = startColor;

        sequence.Append(tmp.DOFade(1f, fadeInDuration));

        // Phase 2: Float Up (1.5s)
        sequence.Join(textObj.transform.DOMove(endPos, floatDuration).SetEase(Ease.OutQuad));

        // Phase 3: Fade Out (0.5s)
        sequence.Append(tmp.DOFade(0f, fadeOutDuration));

        // 完了後、プールに戻す
        sequence.OnComplete(() => ReturnToPool(textObj));
    }

    /// <summary>
    /// プールからテキストオブジェクトを取得
    /// </summary>
    private GameObject GetPooledText()
    {
        if (textPool.Count > 0)
        {
            return textPool.Dequeue();
        }

        // プールが空の場合、新規作成
        Debug.LogWarning("[FloatingTextSystem] Pool exhausted, creating new text object");
        GameObject newTextObj = CreateTextObject();
        newTextObj.SetActive(false);
        return newTextObj;
    }

    /// <summary>
    /// テキストオブジェクトをプールに戻す
    /// </summary>
    private void ReturnToPool(GameObject textObj)
    {
        if (textObj == null) return;

        textObj.SetActive(false);
        textPool.Enqueue(textObj);
    }

    /// <summary>
    /// カメラの方向を向くように回転を更新（オプション）
    /// </summary>
    public void UpdateTextRotations()
    {
        if (Camera.main == null) return;

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
            {
                child.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            }
        }
    }

    /// <summary>
    /// Stage 6: 永続テキストを表示（自動消滅しない）
    /// </summary>
    /// <param name="worldPosition">表示位置（ワールド座標）</param>
    /// <param name="text">表示テキスト</param>
    /// <param name="pressureLevel">心理圧レベル（0.0-3.0）</param>
    public void ShowPersistentText(Vector3 worldPosition, string text, float pressureLevel = 0f)
    {
        // 既存の永続テキストを隠す
        HidePersistentText();

        GameObject textObj = GetPooledText();
        if (textObj == null)
        {
            Debug.LogWarning("[FloatingTextSystem] No available text objects in pool");
            return;
        }

        var tmp = textObj.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            Debug.LogError("[FloatingTextSystem] TextMeshPro component not found");
            return;
        }

        // テキスト設定
        tmp.text = text;
        tmp.color = GetColorForPressure(pressureLevel);

        // 位置設定
        textObj.transform.position = worldPosition;
        textObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // アクティブ化とフェードイン
        textObj.SetActive(true);
        Color startColor = tmp.color;
        startColor.a = 0f;
        tmp.color = startColor;
        tmp.DOFade(1f, fadeInDuration);

        // 永続テキストとして保存
        currentPersistentText = textObj;

        Debug.Log($"[FloatingTextSystem] Persistent text shown: \"{text}\" at {worldPosition}");
    }

    /// <summary>
    /// Stage 6: 永続テキストの内容を更新（位置は保持）
    /// </summary>
    /// <param name="text">新しいテキスト</param>
    /// <param name="pressureLevel">心理圧レベル（0.0-3.0）</param>
    public void UpdatePersistentText(string text, float pressureLevel = 0f)
    {
        if (currentPersistentText == null)
        {
            Debug.LogWarning("[FloatingTextSystem] No persistent text to update");
            return;
        }

        var tmp = currentPersistentText.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            // テキストと色を更新
            tmp.text = text;
            Color targetColor = GetColorForPressure(pressureLevel);
            tmp.DOColor(targetColor, fadeInDuration);

            Debug.Log($"[FloatingTextSystem] Persistent text updated: \"{text}\"");
        }
    }

    /// <summary>
    /// Stage 6: 永続テキストを非表示にする
    /// </summary>
    public void HidePersistentText()
    {
        if (currentPersistentText == null) return;

        var tmp = currentPersistentText.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            // フェードアウトしてからプールに戻す
            tmp.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                ReturnToPool(currentPersistentText);
                currentPersistentText = null;
            });

            Debug.Log("[FloatingTextSystem] Persistent text hidden");
        }
        else
        {
            // TextMeshProが無い場合は即座にプールに戻す
            ReturnToPool(currentPersistentText);
            currentPersistentText = null;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Low Pressure Text")]
    private void TestLowPressureText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        if (textPool == null || textPool.Count == 0)
        {
            Debug.LogWarning("[FloatingTextSystem] Pool not initialized, initializing now...");
            InitializePool();
        }

        // カメラの前方にテキストを表示
        Vector3 testPosition = Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 2f + Camera.main.transform.up * -0.1f
            : Vector3.zero;

        ShowText(testPosition, "This is fine...", 0.5f);
    }

    [ContextMenu("Test: Medium Pressure Text")]
    private void TestMediumPressureText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        if (textPool == null || textPool.Count == 0)
        {
            Debug.LogWarning("[FloatingTextSystem] Pool not initialized, initializing now...");
            InitializePool();
        }

        Vector3 testPosition = Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 2f + Camera.main.transform.up * -0.1f
            : Vector3.zero;

        ShowText(testPosition, "Feeling watched...", 1.5f);
    }

    [ContextMenu("Test: High Pressure Text")]
    private void TestHighPressureText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        if (textPool == null || textPool.Count == 0)
        {
            Debug.LogWarning("[FloatingTextSystem] Pool not initialized, initializing now...");
            InitializePool();
        }

        Vector3 testPosition = Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 2f + Camera.main.transform.up * -0.1f
            : Vector3.zero;

        ShowText(testPosition, "They know!", 2.8f);
    }

    [ContextMenu("Test: Show Persistent Text")]
    private void TestShowPersistentText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        if (textPool == null || textPool.Count == 0)
        {
            InitializePool();
        }

        Vector3 testPosition = Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 2f
            : Vector3.up * 0.5f;

        ShowPersistentText(testPosition, "どれにしようか...", 1.0f);
    }

    [ContextMenu("Test: Update Persistent Text")]
    private void TestUpdatePersistentText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        UpdatePersistentText("これか...いや...", 1.5f);
    }

    [ContextMenu("Test: Hide Persistent Text")]
    private void TestHidePersistentText()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[FloatingTextSystem] Test must be run in Play Mode");
            return;
        }

        HidePersistentText();
    }
#endif
}
