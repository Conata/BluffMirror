using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 言語切り替えUIコントローラー
/// ボタンクリックで言語を切り替える
/// </summary>
public class LanguageSwitcher : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button switchButton;
    [SerializeField] private TMP_Text buttonLabel;

    [Header("Display Settings")]
    [SerializeField] private string englishText = "English";
    [SerializeField] private string japaneseText = "日本語";

    private void Start()
    {
        // ボタンのクリックイベントを登録
        if (switchButton != null)
        {
            switchButton.onClick.AddListener(OnLanguageSwitchClicked);
        }

        // GameSettings の言語変更イベントを購読
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged += UpdateButtonLabel;
        }

        // 初期表示を更新
        UpdateButtonLabel(GameSettings.Instance?.GetLanguage() ?? GameSettings.GameLanguage.English);
    }

    private void OnDestroy()
    {
        // イベント購読解除
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.OnLanguageChanged -= UpdateButtonLabel;
        }

        // ボタンのクリックイベントを解除
        if (switchButton != null)
        {
            switchButton.onClick.RemoveListener(OnLanguageSwitchClicked);
        }
    }

    /// <summary>
    /// 言語切り替えボタンがクリックされた時
    /// </summary>
    private void OnLanguageSwitchClicked()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ToggleLanguage();
            Debug.Log($"[LanguageSwitcher] Language toggled to: {GameSettings.Instance.GetLanguageName()}");
        }
    }

    /// <summary>
    /// ボタンのラベルを更新
    /// </summary>
    private void UpdateButtonLabel(GameSettings.GameLanguage language)
    {
        if (buttonLabel != null)
        {
            // 現在の言語を表示（切り替え先ではなく、現在選択されている言語）
            buttonLabel.text = language == GameSettings.GameLanguage.Japanese ? japaneseText : englishText;
        }
    }
}
