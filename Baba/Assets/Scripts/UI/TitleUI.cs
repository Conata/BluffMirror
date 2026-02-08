using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using System.Collections;

/// <summary>
/// Phase 7-1: タイトル画面UI管理
/// シンプルなStart/Settings/Exit構成
/// </summary>
public class TitleUI : MonoBehaviour
{
    public static TitleUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private CanvasGroup titlePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Title Panel Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Title Panel Button Texts")]
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI settingsButtonText;
    [SerializeField] private TextMeshProUGUI exitButtonText;

    [Header("Settings Panel")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button languageToggleButton;
    [SerializeField] private TextMeshProUGUI languageButtonText;
    [SerializeField] private Button settingsBackButton;

    [Header("Settings Panel Texts")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI bgmVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
    [SerializeField] private TextMeshProUGUI settingsBackButtonText;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "FPS_Trump_Scene";
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Animation Settings")]
    [SerializeField] private float panelTransitionDuration = 0.3f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeButtons();
        InitializeSettings();
    }

    private void OnEnable()
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Start()
    {
        UpdateLanguage();
        ShowTitlePanel();
    }

    private void OnLanguageChanged(GameSettings.GameLanguage language)
    {
        UpdateLanguage();
    }

    private void InitializeButtons()
    {
        // Title Panel
        startButton?.onClick.AddListener(OnStartClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        exitButton?.onClick.AddListener(OnExitClicked);

        // Settings Panel
        masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmVolumeSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
        languageToggleButton?.onClick.AddListener(OnLanguageToggle);
        settingsBackButton?.onClick.AddListener(OnBackFromSettings);
    }

    private void InitializeSettings()
    {
        // AudioManager から現在の音量を取得
        if (AudioManager.Instance != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
            if (bgmVolumeSlider != null)
                bgmVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
        }

        // 言語ボタンテキスト更新
        UpdateLanguageButtonText();
    }

    private void ShowTitlePanel()
    {
        if (titlePanel == null) return;

        settingsPanel?.SetActive(false);
        titlePanel.gameObject.SetActive(true);
        titlePanel.alpha = 0f;
        titlePanel.DOFade(1f, fadeInDuration);
    }

    private void ShowSettingsPanel()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);
        CanvasGroup settingsCG = settingsPanel.GetComponent<CanvasGroup>();
        if (settingsCG != null)
        {
            settingsCG.alpha = 0f;
            settingsCG.DOFade(1f, panelTransitionDuration);
        }
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel == null) return;

        CanvasGroup settingsCG = settingsPanel.GetComponent<CanvasGroup>();
        if (settingsCG != null)
        {
            settingsCG.DOFade(0f, panelTransitionDuration).OnComplete(() =>
            {
                settingsPanel.SetActive(false);
            });
        }
        else
        {
            settingsPanel.SetActive(false);
        }
    }

    // === Button Handlers ===

    private void OnStartClicked()
    {
        Debug.Log("[TitleUI] Game Start clicked");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPick(); // ButtonClick SFX として代用
        }

        StartCoroutine(LoadGameScene());
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[TitleUI] Settings clicked");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPick(); // ButtonClick SFX として代用
        }

        ShowSettingsPanel();
    }

    private void OnExitClicked()
    {
        Debug.Log("[TitleUI] Exit clicked");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPick(); // ButtonClick SFX として代用
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnBackFromSettings()
    {
        Debug.Log("[TitleUI] Back from settings");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPick(); // ButtonClick SFX として代用
        }

        HideSettingsPanel();
    }

    // === Settings Handlers ===

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    private void OnLanguageToggle()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPick(); // ButtonClick SFX として代用
        }

        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ToggleLanguage();
            // UpdateLanguage() は OnLanguageChanged イベントで自動的に呼ばれる
        }
    }

    /// <summary>
    /// 全UIテキストを現在の言語に更新
    /// </summary>
    private void UpdateLanguage()
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return;

        // Title Panel Buttons
        if (startButtonText != null)
            startButtonText.text = loc.Get("title_ui.start_button");
        if (settingsButtonText != null)
            settingsButtonText.text = loc.Get("title_ui.settings_button");
        if (exitButtonText != null)
            exitButtonText.text = loc.Get("title_ui.exit_button");

        // Settings Panel Labels
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = loc.Get("title_ui.master_volume");
        if (bgmVolumeLabel != null)
            bgmVolumeLabel.text = loc.Get("title_ui.bgm_volume");
        if (sfxVolumeLabel != null)
            sfxVolumeLabel.text = loc.Get("title_ui.sfx_volume");
        if (settingsBackButtonText != null)
            settingsBackButtonText.text = loc.Get("title_ui.back_button");

        // Language Button
        UpdateLanguageButtonText();
    }

    private void UpdateLanguageButtonText()
    {
        if (languageButtonText == null) return;

        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
            languageButtonText.text = loc.Get(isJapanese ? "title_ui.language_label_ja" : "title_ui.language_label_en");
        }
        else
        {
            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
            languageButtonText.text = isJapanese ? "Language: 日本語" : "Language: English";
        }
    }

    // === Scene Loading ===

    private IEnumerator LoadGameScene()
    {
        // シーン名検証
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[TitleUI] Game scene name not set!");
            yield break;
        }

        // Fade out
        if (titlePanel != null)
        {
            yield return titlePanel.DOFade(0f, fadeOutDuration).WaitForCompletion();
        }

        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Load Game Scene")]
    private void TestLoadGameScene()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[TitleUI] Test must be run in Play Mode");
            return;
        }

        StartCoroutine(LoadGameScene());
    }
#endif
}
