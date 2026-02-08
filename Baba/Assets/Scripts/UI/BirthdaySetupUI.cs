using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FPSTrump.Manager;

namespace FPSTrump.UI
{
    /// <summary>
    /// 生年月日 + 名前入力パネルのUI制御
    /// TMP_InputFieldで名前、3つのTMP_Dropdownで年/月/日を選択
    /// </summary>
    public class BirthdaySetupUI : MonoBehaviour
    {
        [Header("Name Input")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TextMeshProUGUI nameLabel;

        [Header("Language Selection")]
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private TextMeshProUGUI languageLabel;

        [Header("Dropdown References")]
        [SerializeField] private TMP_Dropdown yearDropdown;
        [SerializeField] private TMP_Dropdown monthDropdown;
        [SerializeField] private TMP_Dropdown dayDropdown;

        [Header("Button References")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button startGameButton;

        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI nextButtonText;
        [SerializeField] private TextMeshProUGUI skipButtonText;
        [SerializeField] private TextMeshProUGUI yearLabel;
        [SerializeField] private TextMeshProUGUI monthLabel;
        [SerializeField] private TextMeshProUGUI dayLabel;

        [Header("Panel References")]
        [SerializeField] private GameObject birthdayPanel;
        [SerializeField] private GameObject readyPanel;

        [Header("Settings")]
        [SerializeField] private int minYear = 1940;
        [SerializeField] private int maxYear = 2020;

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "FPS_Trump_Scene";

        private PlayerBirthdayManager birthdayManager;
        private PlayerNameManager nameManager;

        private void Start()
        {
            // GameSettings の確認と作成
            if (GameSettings.Instance == null)
            {
                GameObject gameSettingsObj = new GameObject("GameSettings");
                gameSettingsObj.AddComponent<GameSettings>();
                Debug.Log("[BirthdaySetupUI] Created GameSettings instance");
            }

            // LocalizationManager の確認と作成
            if (LocalizationManager.Instance == null)
            {
                GameObject locManagerObj = new GameObject("LocalizationManager");
                locManagerObj.AddComponent<LocalizationManager>();
                Debug.Log("[BirthdaySetupUI] Created LocalizationManager instance");
            }

            birthdayManager = PlayerBirthdayManager.Instance;
            if (birthdayManager == null)
            {
                GameObject managerObj = new GameObject("PlayerBirthdayManager");
                birthdayManager = managerObj.AddComponent<PlayerBirthdayManager>();
            }

            nameManager = PlayerNameManager.Instance;
            if (nameManager == null)
            {
                GameObject nameManagerObj = new GameObject("PlayerNameManager");
                nameManager = nameManagerObj.AddComponent<PlayerNameManager>();
            }

            // ReadyPanelの自動検索（Inspector未設定の場合）
            if (readyPanel == null)
            {
                readyPanel = GameObject.Find("ReadyPanel");
                if (readyPanel != null)
                {
                    Debug.Log("[BirthdaySetupUI] Auto-found ReadyPanel");
                }
                else
                {
                    Debug.LogWarning("[BirthdaySetupUI] ReadyPanel not found in scene! Transition will not work.");
                }
            }

            // BirthdayPanelの自動検索（Inspector未設定の場合）
            if (birthdayPanel == null)
            {
                birthdayPanel = GameObject.Find("BirthdayPanel");
                if (birthdayPanel != null)
                {
                    Debug.Log("[BirthdaySetupUI] Auto-found BirthdayPanel");
                }
            }

            SetupLanguageDropdown();
            SetupDropdowns();
            SetupButtons();
            LoadExistingBirthday();
            LoadExistingName();
            UpdateLanguage();

            // GameSettings 作成後にイベントを購読
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged; // 重複購読防止
                GameSettings.Instance.OnLanguageChanged += OnLanguageChanged;
                Debug.Log("[BirthdaySetupUI] Subscribed to OnLanguageChanged event");
            }

            // LocalizationManager のイベント購読を確認し、正しい言語を読み込む
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.SubscribeToLanguageChanges();
                LocalizationManager.Instance.Reload();
                Debug.Log("[BirthdaySetupUI] Setup LocalizationManager event subscription");
            }
        }

        private void OnEnable()
        {
            // Start() でイベント購読するため、ここでは何もしない
        }

        private void OnDisable()
        {
            if (GameSettings.Instance != null)
                GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(GameSettings.GameLanguage language)
        {
            UpdateLanguage();
        }

        private bool IsJapanese()
        {
            return GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
        }

        private void UpdateLanguage()
        {
            var loc = LocalizationManager.Instance;
            if (loc == null)
            {
                Debug.LogWarning("[BirthdaySetupUI] LocalizationManager.Instance is null!");
                return;
            }

            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
            Debug.Log($"[BirthdaySetupUI] UpdateLanguage called, isJapanese={isJapanese}");

            if (titleText != null)
                titleText.text = loc.Get("birthday_ui.title");

            if (subtitleText != null)
                subtitleText.text = loc.Get("birthday_ui.subtitle");

            if (nextButtonText != null)
                nextButtonText.text = loc.Get("birthday_ui.next_button");

            if (skipButtonText != null)
                skipButtonText.text = loc.Get("birthday_ui.skip_button");

            if (nameLabel != null)
                nameLabel.text = loc.Get("birthday_ui.name_label");

            if (languageLabel != null)
                languageLabel.text = loc.Get("birthday_ui.language_label");

            if (yearLabel != null)
                yearLabel.text = loc.Get("birthday_ui.year_label");

            if (monthLabel != null)
                monthLabel.text = loc.Get("birthday_ui.month_label");

            if (dayLabel != null)
                dayLabel.text = loc.Get("birthday_ui.day_label");

            // 言語ドロップダウンのオプション表示を更新
            UpdateLanguageDropdownOptions();

            // 保存済みの場合、ステータスも更新
            if (birthdayManager != null && birthdayManager.HasBirthday())
            {
                ShowStatus($"{loc.Get("birthday_ui.saved")}: {birthdayManager.GetBirthdayString()}", Color.green);
            }
        }

        /// <summary>
        /// 言語選択ドロップダウンの初期化
        /// </summary>
        private void SetupLanguageDropdown()
        {
            if (languageDropdown == null)
            {
                Debug.LogWarning("[BirthdaySetupUI] languageDropdown is null!");
                return;
            }

            if (GameSettings.Instance == null)
            {
                Debug.LogError("[BirthdaySetupUI] GameSettings.Instance is null in SetupLanguageDropdown!");
                return;
            }

            languageDropdown.ClearOptions();
            var languageOptions = new List<string> { "English", "日本語" };
            languageDropdown.AddOptions(languageOptions);

            // 現在の言語を選択状態にする
            LoadCurrentLanguage();

            // 言語変更リスナーを追加
            languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
        }

        /// <summary>
        /// 現在の言語設定をドロップダウンに反映
        /// </summary>
        private void LoadCurrentLanguage()
        {
            if (languageDropdown == null || GameSettings.Instance == null) return;

            // English = 0, Japanese = 1
            int index = GameSettings.Instance.IsJapanese() ? 1 : 0;
            languageDropdown.value = index;
        }

        /// <summary>
        /// 言語ドロップダウンのオプション表示を更新（言語切替時）
        /// </summary>
        private void UpdateLanguageDropdownOptions()
        {
            if (languageDropdown == null) return;

            // リスナーを一時的に削除（値変更時のイベント発火を防ぐ）
            languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);

            // 現在の言語に合わせた値を設定
            int correctValue = GameSettings.Instance != null && GameSettings.Instance.IsJapanese() ? 1 : 0;

            // オプションを再設定（表示テキストは変更しない - 常に"English", "日本語"のまま）
            languageDropdown.ClearOptions();
            var languageOptions = new List<string> { "English", "日本語" };
            languageDropdown.AddOptions(languageOptions);

            // 正しい値を設定
            languageDropdown.value = correctValue;

            // リスナーを再追加
            languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);

            Debug.Log($"[BirthdaySetupUI] Dropdown synced to language: {(correctValue == 1 ? "Japanese" : "English")}");
        }

        /// <summary>
        /// 言語ドロップダウンの選択が変更されたときの処理
        /// </summary>
        private void OnLanguageDropdownChanged(int index)
        {
            // GameSettings が null または破棄されている場合は再作成
            if (GameSettings.Instance == null || !GameSettings.Instance)
            {
                Debug.LogError("[BirthdaySetupUI] GameSettings.Instance is null or destroyed! Recreating...");
                GameObject gameSettingsObj = new GameObject("GameSettings");
                gameSettingsObj.AddComponent<GameSettings>();

                // 再作成後も null の場合はエラー
                if (GameSettings.Instance == null)
                {
                    Debug.LogError("[BirthdaySetupUI] Failed to recreate GameSettings!");
                    return;
                }
            }

            // 0 = English, 1 = Japanese
            GameSettings.GameLanguage newLanguage = index == 1
                ? GameSettings.GameLanguage.Japanese
                : GameSettings.GameLanguage.English;

            GameSettings.GameLanguage currentLanguage = GameSettings.Instance.GetLanguage();
            Debug.Log($"[BirthdaySetupUI] Dropdown changed: index={index}, newLanguage={newLanguage}, currentLanguage={currentLanguage}");

            // 既に同じ言語の場合はスキップ（無限ループ防止）
            if (currentLanguage == newLanguage)
            {
                Debug.Log($"[BirthdaySetupUI] Language already set to {newLanguage}, skipping");
                return;
            }

            GameSettings.Instance.SetLanguage(newLanguage);
            Debug.Log($"[BirthdaySetupUI] Language changed to: {newLanguage}");
        }

        private void SetupDropdowns()
        {
            // 年ドロップダウン
            yearDropdown.ClearOptions();
            var yearOptions = new List<string> { "----" };
            for (int y = maxYear; y >= minYear; y--)
                yearOptions.Add(y.ToString());
            yearDropdown.AddOptions(yearOptions);

            // 月ドロップダウン
            monthDropdown.ClearOptions();
            var monthOptions = new List<string> { "--" };
            for (int m = 1; m <= 12; m++)
                monthOptions.Add(m.ToString());
            monthDropdown.AddOptions(monthOptions);

            // 日ドロップダウン（初期は31日まで）
            UpdateDayDropdown();

            // 月・年変更時に日を更新
            yearDropdown.onValueChanged.AddListener(_ => UpdateDayDropdown());
            monthDropdown.onValueChanged.AddListener(_ => UpdateDayDropdown());
        }

        private void SetupButtons()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipClicked);
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        /// <summary>
        /// 保存済みの生年月日を復元
        /// </summary>
        private void LoadExistingBirthday()
        {
            if (birthdayManager == null || !birthdayManager.HasBirthday()) return;

            var (year, month, day) = birthdayManager.GetBirthday();

            // 年を選択（降順なのでインデックス計算: maxYear - year + 1）
            int yearIndex = maxYear - year + 1;
            if (yearIndex > 0 && yearIndex < yearDropdown.options.Count)
                yearDropdown.value = yearIndex;

            // 月を選択
            if (month >= 1 && month <= 12)
                monthDropdown.value = month;

            // 日ドロップダウンを更新してから日を選択
            UpdateDayDropdown();
            if (day >= 1 && day < dayDropdown.options.Count)
                dayDropdown.value = day;
        }

        /// <summary>
        /// 保存済みの名前を復元
        /// </summary>
        private void LoadExistingName()
        {
            if (nameManager == null || !nameManager.HasName()) return;

            string savedName = nameManager.GetName();
            if (nameInputField != null)
            {
                nameInputField.text = savedName;
            }
        }

        /// <summary>
        /// 日ドロップダウンを月・年に応じて更新
        /// </summary>
        private void UpdateDayDropdown()
        {
            int currentDayValue = dayDropdown.value;

            int daysInMonth = 31;
            int selectedYear = GetSelectedYear();
            int selectedMonth = GetSelectedMonth();

            if (selectedYear > 0 && selectedMonth > 0)
            {
                daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            }

            dayDropdown.ClearOptions();
            var dayOptions = new List<string> { "--" };
            for (int d = 1; d <= daysInMonth; d++)
                dayOptions.Add(d.ToString());
            dayDropdown.AddOptions(dayOptions);

            // 以前の選択を維持（範囲内なら）
            if (currentDayValue > 0 && currentDayValue < dayDropdown.options.Count)
                dayDropdown.value = currentDayValue;
            else if (currentDayValue >= dayDropdown.options.Count)
                dayDropdown.value = dayDropdown.options.Count - 1;
        }

        private int GetSelectedYear()
        {
            if (yearDropdown.value == 0) return 0;
            return maxYear - yearDropdown.value + 1;
        }

        private int GetSelectedMonth()
        {
            return monthDropdown.value; // 0="--", 1=1月, ..., 12=12月
        }

        private int GetSelectedDay()
        {
            return dayDropdown.value; // 0="--", 1=1日, ..., 31=31日
        }

        private void OnNextClicked()
        {
            var loc = LocalizationManager.Instance;

            // 名前のバリデーション（任意だが、入力されている場合は保存）
            string enteredName = nameInputField != null ? nameInputField.text.Trim() : "";
            if (!string.IsNullOrWhiteSpace(enteredName))
            {
                nameManager.SaveName(enteredName);
            }

            // 生年月日のバリデーション
            int year = GetSelectedYear();
            int month = GetSelectedMonth();
            int day = GetSelectedDay();

            if (year == 0 || month == 0 || day == 0)
            {
                ShowStatus(
                    loc != null ? loc.Get("birthday_ui.validation_error") : "Please select year, month, and day",
                    Color.yellow);
                return;
            }

            birthdayManager.SaveBirthday(year, month, day);

            // 保存完了メッセージ
            string saved = loc != null ? loc.Get("birthday_ui.saved_message") : "Saved";
            string statusMsg = nameManager.HasName()
                ? $"{saved}: {nameManager.GetName()} ({birthdayManager.GetBirthdayString()})"
                : $"{saved}: {birthdayManager.GetBirthdayString()}";
            ShowStatus(statusMsg, Color.green);

            TransitionToReadyPanel();
        }

        private void OnSkipClicked()
        {
            Debug.Log("[BirthdaySetupUI] Skipped birthday input");
            TransitionToReadyPanel();
        }

        private void TransitionToReadyPanel()
        {
            Debug.Log("[BirthdaySetupUI] TransitionToReadyPanel called");
            Debug.Log($"[BirthdaySetupUI] birthdayPanel = {(birthdayPanel != null ? birthdayPanel.name : "NULL")}");
            Debug.Log($"[BirthdaySetupUI] readyPanel = {(readyPanel != null ? readyPanel.name : "NULL")}");

            if (birthdayPanel != null)
                birthdayPanel.SetActive(false);
            if (readyPanel != null)
            {
                readyPanel.SetActive(true);
                Debug.Log("[BirthdaySetupUI] ReadyPanel activated!");
            }
            else
            {
                Debug.LogError("[BirthdaySetupUI] ReadyPanel is NULL! Please assign it in the Inspector.");
            }
        }

        private void OnStartGameClicked()
        {
            Debug.Log("[BirthdaySetupUI] Starting game...");

            // APIKeyManagerにAPIキーを適用
            if (APIKeyManager.Instance != null)
            {
                APIKeyManager.Instance.ApplyAPIKeysToLLMManager();
            }

            // 誕生日データをロード（既に保存されているはず）
            if (birthdayManager != null && birthdayManager.HasBirthday())
            {
                var (year, month, day) = birthdayManager.GetBirthday();
                Debug.Log($"[BirthdaySetupUI] Birthday loaded for game: {year}/{month}/{day}");
            }

            // ゲームシーンに遷移（直接呼び出し）
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("[BirthdaySetupUI] Game scene name not set!");
                return;
            }

            Debug.Log($"[BirthdaySetupUI] Loading scene: {gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }

        private void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
    }
}
