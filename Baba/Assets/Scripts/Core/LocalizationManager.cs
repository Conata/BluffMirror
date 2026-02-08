using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>
/// JSON ベースの軽量ローカライズマネージャー
/// Resources/Localization/ja.json, en.json を読み込み、
/// ドット記法キーで文字列を取得する
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public bool IsJapanese => GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

    private Dictionary<string, object> localizedData = new Dictionary<string, object>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SubscribeToLanguageChanges();
    }

    private void OnDisable()
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    /// <summary>
    /// GameSettings の言語変更イベントを購読
    /// GameSettings が後から作成された場合でも呼び出せる
    /// </summary>
    public void SubscribeToLanguageChanges()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged; // 重複購読防止
            GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
            Debug.Log("[LocalizationManager] Subscribed to OnLanguageChanged event");
        }
        else
        {
            Debug.LogWarning("[LocalizationManager] Cannot subscribe: GameSettings.Instance is null");
        }
    }

    private void OnLanguageChanged(GameSettings.GameLanguage newLanguage)
    {
        Reload();
    }

    /// <summary>
    /// 現在の言語の JSON を再読込
    /// </summary>
    public void Reload()
    {
        LoadLocalization();
        Debug.Log($"[LocalizationManager] Reloaded for language: {GameSettings.Instance?.GetLanguage()}");
    }

    private void LoadLocalization()
    {
        string langCode = IsJapanese ? "ja" : "en";
        var textAsset = Resources.Load<TextAsset>($"Localization/{langCode}");
        if (textAsset == null)
        {
            Debug.LogError($"[LocalizationManager] Failed to load Localization/{langCode}.json");
            return;
        }

        localizedData.Clear();
        var root = JObject.Parse(textAsset.text);
        FlattenJson(root, "", localizedData);
        Debug.Log($"[LocalizationManager] Loaded {localizedData.Count} keys for '{langCode}'");
    }

    private void FlattenJson(JObject obj, string prefix, Dictionary<string, object> output)
    {
        foreach (var property in obj.Properties())
        {
            string fullKey = string.IsNullOrEmpty(prefix) ? property.Name : prefix + "." + property.Name;

            if (property.Value is JObject childObj)
            {
                FlattenJson(childObj, fullKey, output);
            }
            else
            {
                output[fullKey] = property.Value;
            }
        }
    }

    /// <summary>
    /// 単一文字列を取得。テンプレート変数を置換可能。
    /// Get("key", ("name", "value")) → {name} を value に置換
    /// </summary>
    public string Get(string key, params (string name, string value)[] vars)
    {
        if (localizedData.TryGetValue(key, out object value))
        {
            string result;
            if (value is JValue jVal)
                result = jVal.ToString();
            else
                result = value.ToString();

            foreach (var (name, val) in vars)
            {
                result = result.Replace($"{{{name}}}", val);
            }
            return result;
        }

        Debug.LogWarning($"[LocalizationManager] Missing key: {key}");
        return key;
    }

    /// <summary>
    /// 文字列配列を取得
    /// </summary>
    public string[] GetArray(string key)
    {
        if (localizedData.TryGetValue(key, out object value) && value is JArray jArray)
        {
            string[] result = new string[jArray.Count];
            for (int i = 0; i < jArray.Count; i++)
            {
                result[i] = jArray[i].ToString();
            }
            return result;
        }

        Debug.LogWarning($"[LocalizationManager] Missing array key: {key}");
        return new string[0];
    }

    /// <summary>
    /// 指定プレフィックスのキーを全取得し、プレフィックスを除去した Dictionary を返す。
    /// 値が配列のキーのみ対象。
    /// GetArrayDictionary("fallback.") → {"stop_high_doubt": string[], ...}
    /// </summary>
    public Dictionary<string, string[]> GetArrayDictionary(string prefix)
    {
        var result = new Dictionary<string, string[]>();
        foreach (var kvp in localizedData)
        {
            if (kvp.Key.StartsWith(prefix) && kvp.Value is JArray jArray)
            {
                string shortKey = kvp.Key.Substring(prefix.Length);
                string[] arr = new string[jArray.Count];
                for (int i = 0; i < jArray.Count; i++)
                {
                    arr[i] = jArray[i].ToString();
                }
                result[shortKey] = arr;
            }
        }
        return result;
    }

    /// <summary>
    /// 指定プレフィックスのキーを全取得し、プレフィックスを除去した Dictionary を返す。
    /// 値が文字列のキーのみ対象。
    /// </summary>
    public Dictionary<string, string> GetStringDictionary(string prefix)
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in localizedData)
        {
            if (kvp.Key.StartsWith(prefix) && kvp.Value is JValue jVal)
            {
                string shortKey = kvp.Key.Substring(prefix.Length);
                result[shortKey] = jVal.ToString();
            }
        }
        return result;
    }

    /// <summary>
    /// テンプレート変数を指定文字列に適用
    /// </summary>
    public static string ApplyVars(string text, params (string name, string value)[] vars)
    {
        foreach (var (name, val) in vars)
        {
            text = text.Replace($"{{{name}}}", val);
        }
        return text;
    }
}
