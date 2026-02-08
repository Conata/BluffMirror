using UnityEngine;

/// <summary>
/// ゲーム設定の管理
/// 言語設定、音量設定などを一元管理
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Language Settings")]
    [SerializeField] private GameLanguage currentLanguage = GameLanguage.English;

    // PlayerPrefs Keys
    private const string LANGUAGE_KEY = "GameLanguage";
    private const string FIRST_LAUNCH_KEY = "FirstLaunch";

    public enum GameLanguage
    {
        English,
        Japanese
    }

    // イベント: 言語が変更されたときに通知
    public System.Action<GameLanguage> OnLanguageChanged;

    private void Awake()
    {
        // Singleton パターン
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 設定を初期化
    /// </summary>
    private void InitializeSettings()
    {
        // 初回起動チェック
        if (!PlayerPrefs.HasKey(FIRST_LAUNCH_KEY))
        {
            // 初回起動: デフォルトは English
            currentLanguage = GameLanguage.English;
            SaveLanguage();
            PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log($"[GameSettings] First launch detected. Language set to: {currentLanguage} (default)");
        }
        else
        {
            // 2回目以降: 保存された言語を読み込み
            LoadLanguage();
        }
    }

    /// <summary>
    /// システム言語を検出
    /// </summary>
    private void DetectSystemLanguage()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Japanese:
                currentLanguage = GameLanguage.Japanese;
                break;
            default:
                currentLanguage = GameLanguage.English;
                break;
        }

        SaveLanguage();
        Debug.Log($"[GameSettings] System language detected: {Application.systemLanguage} → {currentLanguage}");
    }

    /// <summary>
    /// 言語を設定
    /// </summary>
    /// <param name="language">設定する言語</param>
    public void SetLanguage(GameLanguage language)
    {
        if (currentLanguage != language)
        {
            currentLanguage = language;
            SaveLanguage();
            OnLanguageChanged?.Invoke(currentLanguage);
            Debug.Log($"[GameSettings] Language changed to: {currentLanguage}");
        }
    }

    /// <summary>
    /// 現在の言語を取得
    /// </summary>
    public GameLanguage GetLanguage()
    {
        return currentLanguage;
    }

    /// <summary>
    /// 言語が日本語かどうか
    /// </summary>
    public bool IsJapanese()
    {
        return currentLanguage == GameLanguage.Japanese;
    }

    /// <summary>
    /// 言語が英語かどうか
    /// </summary>
    public bool IsEnglish()
    {
        return currentLanguage == GameLanguage.English;
    }

    /// <summary>
    /// 言語設定を保存
    /// </summary>
    private void SaveLanguage()
    {
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)currentLanguage);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 言語設定を読み込み
    /// </summary>
    private void LoadLanguage()
    {
        int languageValue = PlayerPrefs.GetInt(LANGUAGE_KEY, (int)GameLanguage.English);
        currentLanguage = (GameLanguage)languageValue;
        Debug.Log($"[GameSettings] Language loaded: {currentLanguage}");
    }

    /// <summary>
    /// 言語を切り替え（トグル）
    /// </summary>
    public void ToggleLanguage()
    {
        GameLanguage newLanguage = currentLanguage == GameLanguage.English
            ? GameLanguage.Japanese
            : GameLanguage.English;
        SetLanguage(newLanguage);
    }

    /// <summary>
    /// 言語名を取得（表示用）
    /// </summary>
    public string GetLanguageName()
    {
        return currentLanguage == GameLanguage.Japanese ? "日本語" : "English";
    }

    /// <summary>
    /// すべての設定をリセット（デバッグ用）
    /// </summary>
    [ContextMenu("Reset All Settings")]
    public void ResetAllSettings()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        DetectSystemLanguage();
        Debug.Log("[GameSettings] All settings reset.");
    }

#if UNITY_EDITOR
    [ContextMenu("Test - Switch to English")]
    private void TestSwitchToEnglish()
    {
        SetLanguage(GameLanguage.English);
    }

    [ContextMenu("Test - Switch to Japanese")]
    private void TestSwitchToJapanese()
    {
        SetLanguage(GameLanguage.Japanese);
    }

    [ContextMenu("Test - Toggle Language")]
    private void TestToggleLanguage()
    {
        ToggleLanguage();
    }
#endif
}
