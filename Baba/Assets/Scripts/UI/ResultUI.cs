using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using FPSTrump.Result;

/// <summary>
/// リザルト診断UI（ScreenSpace Overlay）
/// タイプライター演出 + DOTweenアニメーション
/// Stage 7.5: 種明かしパート + プロファイル照合
/// </summary>
public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance { get; private set; }

    [Header("System Dependencies")]
    [SerializeField] private FPSTrump.AI.LLM.LLMManager llmManager;

    [Header("UI References (auto-created if null)")]
    [SerializeField] private Canvas resultCanvas;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI tendencyText;
    [SerializeField] private TextMeshProUGUI insightText;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;

    [Header("Stat Bars")]
    [SerializeField] private Image[] statBarFills;
    [SerializeField] private TextMeshProUGUI[] statBarLabels;

    [Header("Animation Settings")]
    [SerializeField] private float typewriterSpeedDescription = 40f;
    [SerializeField] private float typewriterSpeedTendency = 30f;
    [SerializeField] private float typewriterSpeedEvidence = 35f;

    [Header("Colors")]
    [SerializeField] private Color goldColor = new Color(1f, 0.843f, 0f);
    [SerializeField] private Color barColor = new Color(0f, 0.8f, 0.9f);
    [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.667f);
    [SerializeField] private Color evidenceHeaderColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private Color observationColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color interpretationColor = new Color(0.7f, 0.85f, 0.7f);
    [SerializeField] private Color matchColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color mismatchColor = new Color(1f, 0.6f, 0.2f);

    // コールバック
    public System.Action OnReplayRequested;
    public System.Action OnMenuRequested;

    private Sequence animationSequence;
    private bool isShowing;

    // 種明かしUI要素
    private TextMeshProUGUI evidenceHeaderText;
    private List<TextMeshProUGUI> evidenceObservationTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> evidenceInterpretationTexts = new List<TextMeshProUGUI>();

    // プロファイル照合UI要素
    private TextMeshProUGUI profileHeaderText;
    private List<TextMeshProUGUI> profileTraitLabels = new List<TextMeshProUGUI>();
    private List<Image> profilePredictedBars = new List<Image>();
    private List<Image> profileActualBars = new List<Image>();
    private List<TextMeshProUGUI> profileCommentTexts = new List<TextMeshProUGUI>();

    // LLM詳細分析UI要素（ハイブリッドモード）
    private GameObject llmAnalysisContainer;
    private TextMeshProUGUI llmAnalysisText;

    // コンテナ参照
    private GameObject evidenceContainer;
    private GameObject profileContainer;

    private string[] GetStatNames()
    {
        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            var names = loc.GetArray("result_ui.stat_names");
            if (names.Length == 5) return names;
        }
        return new[] { "決断力", "一貫性", "耐圧性", "直感力", "適応力" };
    }

    /// <summary>
    /// Re-resolve text if it looks like an unresolved localization key
    /// </summary>
    private string ResolveIfKey(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // If text looks like a localization key (starts with "evidence." and no spaces), try to resolve
        if (text.StartsWith("evidence.") && !text.Contains(" "))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string resolved = loc.Get(text);
                if (resolved != text)
                {
                    Debug.Log($"[ResultUI] Re-resolved evidence key: {text} -> {resolved}");
                    return resolved;
                }
                Debug.LogWarning($"[ResultUI] Evidence key still unresolved: {text}");
            }
        }
        return text;
    }

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

        if (llmManager == null)
            llmManager = FPSTrump.AI.LLM.LLMManager.Instance;

        if (resultCanvas == null)
            CreateUI();

        resultCanvas.gameObject.SetActive(false);
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

    private void OnLanguageChanged(GameSettings.GameLanguage newLanguage)
    {
        // Delegate to RefreshButtonLabels for consistency
        RefreshButtonLabels();
    }

    /// <summary>
    /// Refresh button labels to current language
    /// </summary>
    private void RefreshButtonLabels()
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return;

        if (replayButton != null)
        {
            var text = replayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = loc.Get("result_ui.replay_button");
            }
        }
        if (menuButton != null)
        {
            var text = menuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = loc.Get("result_ui.menu_button");
            }
        }
    }

    /// <summary>
    /// 診断結果を表示
    /// </summary>
    public void ShowDiagnosis(DiagnosisResult diagnosis, GameSessionData sessionData)
    {
        if (isShowing) return;
        isShowing = true;

        Debug.Log($"[ResultUI] ShowDiagnosis called - Canvas active: {resultCanvas.gameObject.activeInHierarchy}, SortingOrder: {resultCanvas.sortingOrder}");
        var raycaster = resultCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Debug.Log($"[ResultUI] GraphicRaycaster enabled: {raycaster.enabled}");
        }
        else
        {
            Debug.LogWarning("[ResultUI] GraphicRaycaster is missing!");
        }

        // EventSystem確認
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[ResultUI] EventSystem is missing! UI clicks will not work!");
        }
        else
        {
            Debug.Log($"[ResultUI] EventSystem found: {eventSystem.name}");
        }

        // テキスト初期化
        headerText.text = "";
        titleText.text = "";
        descriptionText.text = "";
        tendencyText.text = "";
        insightText.text = "";

        // スタッツバー初期化
        if (statBarFills != null)
        {
            for (int i = 0; i < statBarFills.Length; i++)
            {
                if (statBarFills[i] != null)
                    statBarFills[i].fillAmount = 0f;
            }
        }

        // ボタン非表示
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (menuButton != null) menuButton.gameObject.SetActive(false);

        // 種明かしセクション: 動的に生成
        SetupEvidenceSection(diagnosis);
        SetupProfileSection(diagnosis);
        SetupLLMAnalysisSection();

        // Canvas表示
        resultCanvas.gameObject.SetActive(true);
        if (backgroundGroup != null) backgroundGroup.alpha = 0f;

        // Refresh button labels to current language
        RefreshButtonLabels();

        PlayAnimation(diagnosis, sessionData);
    }

    /// <summary>
    /// UI非表示
    /// </summary>
    public void Hide()
    {
        if (animationSequence != null)
        {
            animationSequence.Kill();
            animationSequence = null;
        }

        if (resultCanvas != null)
            resultCanvas.gameObject.SetActive(false);

        isShowing = false;
    }

    // ========================================
    // 種明かしセクション動的セットアップ
    // ========================================

    private void SetupEvidenceSection(DiagnosisResult diagnosis)
    {
        // 既存の証拠テキストをクリア
        foreach (var t in evidenceObservationTexts) { if (t != null) Destroy(t.gameObject); }
        foreach (var t in evidenceInterpretationTexts) { if (t != null) Destroy(t.gameObject); }
        evidenceObservationTexts.Clear();
        evidenceInterpretationTexts.Clear();

        if (evidenceHeaderText != null) evidenceHeaderText.text = "";

        if (diagnosis.evidences == null || diagnosis.evidences.Count == 0)
        {
            if (evidenceContainer != null) evidenceContainer.SetActive(false);
            return;
        }

        if (evidenceContainer == null) return;
        evidenceContainer.SetActive(true);

        foreach (var evidence in diagnosis.evidences)
        {
            var obsText = CreateText(evidenceContainer.transform, "EvidenceObs", "",
                16, observationColor, TextAlignmentOptions.Left, 30f);
            evidenceObservationTexts.Add(obsText);

            var interpText = CreateText(evidenceContainer.transform, "EvidenceInterp", "",
                14, interpretationColor, TextAlignmentOptions.Left, 28f);
            evidenceInterpretationTexts.Add(interpText);
        }
    }

    private void SetupProfileSection(DiagnosisResult diagnosis)
    {
        // 既存の照合UIをクリア
        foreach (var t in profileTraitLabels) { if (t != null) Destroy(t.gameObject); }
        foreach (var b in profilePredictedBars) { if (b != null) Destroy(b.transform.parent.gameObject); }
        foreach (var b in profileActualBars) { if (b != null) Destroy(b.transform.parent.gameObject); }
        foreach (var t in profileCommentTexts) { if (t != null) Destroy(t.gameObject); }
        profileTraitLabels.Clear();
        profilePredictedBars.Clear();
        profileActualBars.Clear();
        profileCommentTexts.Clear();

        if (profileHeaderText != null) profileHeaderText.text = "";

        if (!diagnosis.hasProfileComparison || diagnosis.profileComparisons == null || diagnosis.profileComparisons.Count == 0)
        {
            if (profileContainer != null) profileContainer.SetActive(false);
            return;
        }

        if (profileContainer == null) return;
        profileContainer.SetActive(true);

        foreach (var comp in diagnosis.profileComparisons)
        {
            CreateProfileComparisonRow(profileContainer.transform, comp);
        }
    }

    private void SetupLLMAnalysisSection()
    {
        // LLM分析テキストをクリア（バックグラウンド生成完了まで非表示）
        if (llmAnalysisText != null)
        {
            llmAnalysisText.text = "";
        }

        if (llmAnalysisContainer != null)
        {
            llmAnalysisContainer.SetActive(false);
        }
    }

    private void CreateProfileComparisonRow(Transform parent, ProfileComparison comp)
    {
        // Trait row container
        GameObject rowObj = CreateUIElement("ProfileRow", parent);
        rowObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
        VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 2;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        // Label + Comment
        var label = CreateText(rowObj.transform, "TraitLabel", "", 14, Color.white, TextAlignmentOptions.Left, 20f);
        profileTraitLabels.Add(label);

        // Predicted bar row
        GameObject predRow = CreateUIElement("PredRow", rowObj.transform);
        predRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 14);
        HorizontalLayoutGroup predLayout = predRow.AddComponent<HorizontalLayoutGroup>();
        predLayout.spacing = 5;
        predLayout.childAlignment = TextAnchor.MiddleLeft;
        predLayout.childControlWidth = false;
        predLayout.childControlHeight = true;
        predLayout.childForceExpandWidth = false;

        var loc = LocalizationManager.Instance;
        var predLabel = CreateText(predRow.transform, "PredLabel",
            loc != null ? loc.Get("result_ui.predicted_label") : "予測",
            11, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Left, 14f);
        predLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 14);
        predLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;

        // Predicted bar
        GameObject predBarBg = CreateUIElement("PredBarBg", predRow.transform);
        predBarBg.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 12);
        predBarBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        predBarBg.AddComponent<LayoutElement>().preferredWidth = 300;

        GameObject predFill = CreateUIElement("PredFill", predBarBg.transform);
        RectTransform predFillRect = predFill.GetComponent<RectTransform>();
        predFillRect.anchorMin = Vector2.zero;
        predFillRect.anchorMax = Vector2.one;
        predFillRect.offsetMin = Vector2.zero;
        predFillRect.offsetMax = Vector2.zero;
        Image predFillImg = predFill.AddComponent<Image>();
        predFillImg.color = new Color(0.5f, 0.5f, 0.8f, 0.7f);
        predFillImg.type = Image.Type.Filled;
        predFillImg.fillMethod = Image.FillMethod.Horizontal;
        predFillImg.fillAmount = 0f;
        profilePredictedBars.Add(predFillImg);

        // Actual bar row
        GameObject actRow = CreateUIElement("ActRow", rowObj.transform);
        actRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 14);
        HorizontalLayoutGroup actLayout = actRow.AddComponent<HorizontalLayoutGroup>();
        actLayout.spacing = 5;
        actLayout.childAlignment = TextAnchor.MiddleLeft;
        actLayout.childControlWidth = false;
        actLayout.childControlHeight = true;
        actLayout.childForceExpandWidth = false;

        var actLabel = CreateText(actRow.transform, "ActLabel",
            loc != null ? loc.Get("result_ui.actual_label") : "実際",
            11, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Left, 14f);
        actLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 14);
        actLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;

        GameObject actBarBg = CreateUIElement("ActBarBg", actRow.transform);
        actBarBg.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 12);
        actBarBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        actBarBg.AddComponent<LayoutElement>().preferredWidth = 300;

        GameObject actFill = CreateUIElement("ActFill", actBarBg.transform);
        RectTransform actFillRect = actFill.GetComponent<RectTransform>();
        actFillRect.anchorMin = Vector2.zero;
        actFillRect.anchorMax = Vector2.one;
        actFillRect.offsetMin = Vector2.zero;
        actFillRect.offsetMax = Vector2.zero;
        Image actFillImg = actFill.AddComponent<Image>();
        actFillImg.color = barColor;
        actFillImg.type = Image.Type.Filled;
        actFillImg.fillMethod = Image.FillMethod.Horizontal;
        actFillImg.fillAmount = 0f;
        profileActualBars.Add(actFillImg);

        // Comment
        bool isMatch = Mathf.Abs(comp.predicted - comp.actual) <= 0.15f;
        var commentText = CreateText(rowObj.transform, "Comment", "", 12,
            isMatch ? matchColor : mismatchColor, TextAlignmentOptions.Right, 18f);
        profileCommentTexts.Add(commentText);
    }

    // ========================================
    // アニメーション
    // ========================================

    private void PlayAnimation(DiagnosisResult diagnosis, GameSessionData sessionData)
    {
        animationSequence?.Kill();
        animationSequence = DOTween.Sequence();

        // t=0.0s 背景フェードイン
        if (backgroundGroup != null)
        {
            animationSequence.Append(backgroundGroup.DOFade(1f, 0.5f));
        }

        // t=0.3s ヘッダーフェードイン（レイアウト制御と競合しないようfadeで）
        headerText.alpha = 0f;
        animationSequence.InsertCallback(0.3f, () =>
        {
            var loc = LocalizationManager.Instance;
            headerText.text = loc != null ? loc.Get("result_ui.header") : "行動パターン分析結果";
        });
        animationSequence.Insert(0.3f, headerText.DOFade(1f, 0.4f));

        // t=0.8s タイトル拡大
        animationSequence.InsertCallback(0.8f, () =>
        {
            titleText.text = diagnosis.personalityTitle;
            titleText.color = goldColor;
        });
        titleText.transform.localScale = Vector3.zero;
        animationSequence.Insert(0.8f,
            titleText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutElastic));

        // t=1.5s Description タイプライター
        animationSequence.InsertCallback(1.5f, () =>
        {
            StartCoroutine(TypewriterEffect(descriptionText, diagnosis.personalityDescription, typewriterSpeedDescription));
        });

        // t=3.0s Tendency タイプライター（スタッツバー削除により繰り上げ）
        animationSequence.InsertCallback(3.0f, () =>
        {
            StartCoroutine(TypewriterEffect(tendencyText, diagnosis.psychologicalTendency, typewriterSpeedTendency));
        });

        // === 種明かしパート ===
        float evidenceStartTime = 4.0f;

        if (diagnosis.evidences != null && diagnosis.evidences.Count > 0)
        {
            // t=5.0s セクションヘッダー
            animationSequence.InsertCallback(evidenceStartTime, () =>
            {
                if (evidenceHeaderText != null)
                {
                    var loc = LocalizationManager.Instance;
                    evidenceHeaderText.text = loc != null ? loc.Get("result_ui.evidence_header") : "なぜそう判断したか";
                    evidenceHeaderText.color = new Color(evidenceHeaderColor.r, evidenceHeaderColor.g, evidenceHeaderColor.b, 0f);
                }
            });
            if (evidenceHeaderText != null)
            {
                animationSequence.Insert(evidenceStartTime,
                    evidenceHeaderText.DOFade(1f, 0.4f));
            }

            // 各証拠のアニメーション
            for (int i = 0; i < diagnosis.evidences.Count; i++)
            {
                int idx = i;
                float startTime = evidenceStartTime + 0.5f + i * 1.5f;
                var evidence = diagnosis.evidences[i];

                // observation タイプライター
                animationSequence.InsertCallback(startTime, () =>
                {
                    if (idx < evidenceObservationTexts.Count && evidenceObservationTexts[idx] != null)
                    {
                        StartCoroutine(TypewriterEffect(
                            evidenceObservationTexts[idx],
                            $"  {ResolveIfKey(evidence.observation)}",
                            typewriterSpeedEvidence));
                    }
                });

                // interpretation スライドイン（0.8s後）
                animationSequence.InsertCallback(startTime + 0.8f, () =>
                {
                    if (idx < evidenceInterpretationTexts.Count && evidenceInterpretationTexts[idx] != null)
                    {
                        StartCoroutine(TypewriterEffect(
                            evidenceInterpretationTexts[idx],
                            $"    → {ResolveIfKey(evidence.interpretation)}",
                            typewriterSpeedEvidence));
                    }
                });
            }

            evidenceStartTime += 0.5f + diagnosis.evidences.Count * 1.5f + 0.5f;
        }

        // === プロファイル照合パート ===
        float profileStartTime = evidenceStartTime;

        if (diagnosis.hasProfileComparison && diagnosis.profileComparisons != null && diagnosis.profileComparisons.Count > 0)
        {
            // セクションヘッダー
            animationSequence.InsertCallback(profileStartTime, () =>
            {
                if (profileHeaderText != null)
                {
                    var loc = LocalizationManager.Instance;
                    profileHeaderText.text = loc != null ? loc.Get("result_ui.profile_header") : "生年月日プロファイルとの照合";
                    profileHeaderText.color = new Color(evidenceHeaderColor.r, evidenceHeaderColor.g, evidenceHeaderColor.b, 0f);
                }
            });
            if (profileHeaderText != null)
            {
                animationSequence.Insert(profileStartTime,
                    profileHeaderText.DOFade(1f, 0.4f));
            }

            // 各特性の照合バーアニメ
            for (int i = 0; i < diagnosis.profileComparisons.Count; i++)
            {
                int idx = i;
                float startTime = profileStartTime + 0.5f + i * 0.4f;
                var comp = diagnosis.profileComparisons[i];

                // ラベル表示
                animationSequence.InsertCallback(startTime, () =>
                {
                    if (idx < profileTraitLabels.Count && profileTraitLabels[idx] != null)
                        profileTraitLabels[idx].text = comp.traitName;
                });

                // 予測バー
                if (idx < profilePredictedBars.Count && profilePredictedBars[idx] != null)
                {
                    animationSequence.Insert(startTime,
                        profilePredictedBars[idx].DOFillAmount(comp.predicted, 0.5f).SetEase(Ease.OutCubic));
                }

                // 実際バー（少し遅延）
                if (idx < profileActualBars.Count && profileActualBars[idx] != null)
                {
                    animationSequence.Insert(startTime + 0.15f,
                        profileActualBars[idx].DOFillAmount(comp.actual, 0.5f).SetEase(Ease.OutCubic));
                }

                // コメント表示
                animationSequence.InsertCallback(startTime + 0.3f, () =>
                {
                    if (idx < profileCommentTexts.Count && profileCommentTexts[idx] != null)
                        profileCommentTexts[idx].text = comp.commentary;
                });
            }

            profileStartTime += 0.5f + diagnosis.profileComparisons.Count * 0.4f + 0.5f;
        }

        // === LLM詳細分析パート（ハイブリッドモード） ===
        float llmAnalysisTime = profileStartTime + 0.5f;

        // バックグラウンドLLM分析を非同期で待機・表示
        animationSequence.InsertCallback(llmAnalysisTime, () =>
        {
            StartCoroutine(ShowLLMDetailedAnalysis(sessionData));
        });

        // === 締め ===
        float insightTime = llmAnalysisTime + 0.5f;

        // Insight フェードイン+拡大
        animationSequence.InsertCallback(insightTime, () =>
        {
            insightText.text = diagnosis.behavioralInsight;
            insightText.color = new Color(goldColor.r, goldColor.g, goldColor.b, 0f);
        });
        insightText.transform.localScale = Vector3.one * 0.5f;
        animationSequence.Insert(insightTime,
            insightText.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack));
        animationSequence.Insert(insightTime,
            insightText.DOFade(1f, 0.6f));

        // ボタン表示
        float buttonTime = insightTime + 0.7f;
        animationSequence.InsertCallback(buttonTime, () =>
        {
            if (replayButton != null)
            {
                replayButton.gameObject.SetActive(true);
                replayButton.interactable = true;
                replayButton.transform.localScale = Vector3.zero;
                replayButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                Debug.Log($"[ResultUI] Replay button activated - Active:{replayButton.gameObject.activeInHierarchy}, Interactable:{replayButton.interactable}, HasCallback:{OnReplayRequested != null}");
            }
            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(true);
                menuButton.interactable = true;
                menuButton.transform.localScale = Vector3.zero;
                menuButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(0.1f);
                Debug.Log($"[ResultUI] Menu button activated - Active:{menuButton.gameObject.activeInHierarchy}, Interactable:{menuButton.interactable}");
            }
        });
    }

    /// <summary>
    /// LLM詳細分析を待機して表示（ハイブリッドモード）
    /// </summary>
    private IEnumerator ShowLLMDetailedAnalysis(GameSessionData sessionData)
    {
        var diagnosisSystem = ResultDiagnosisSystem.Instance;
        if (diagnosisSystem == null)
        {
            Debug.Log("[ResultUI] ResultDiagnosisSystem not found, skipping LLM analysis");
            yield break;
        }

        // 最大8秒待機（非ブロッキング）
        var analysisTask = diagnosisSystem.GetDetailedAnalysisAsync(8000);

        while (!analysisTask.IsCompleted)
        {
            yield return null;
        }

        string analysis = analysisTask.Result;
        if (string.IsNullOrEmpty(analysis))
        {
            Debug.Log("[ResultUI] LLM detailed analysis not available, skipping");
            yield break;
        }

        Debug.Log($"[ResultUI] LLM detailed analysis ready: \"{analysis}\"");

        // TTS生成（即座に開始）
        Task<AudioClip> ttsTask = null;
        if (llmManager != null && AudioManager.Instance != null)
        {
            ttsTask = llmManager.GenerateTTSAsync(analysis, FPSTrump.Psychology.AIEmotion.Calm);
            Debug.Log("[ResultUI] TTS generation started for LLM analysis");
        }
        else
        {
            Debug.LogWarning($"[ResultUI] Cannot generate TTS - llmManager={llmManager != null}, AudioManager={AudioManager.Instance != null}");
        }

        yield return new WaitForSeconds(0.5f);

        // コンテナ表示
        if (llmAnalysisContainer != null)
        {
            llmAnalysisContainer.SetActive(true);
        }

        // テキスト表示（タイプライター効果）
        if (llmAnalysisText != null)
        {
            yield return StartCoroutine(TypewriterEffect(llmAnalysisText, analysis, 35f));
        }

        // TTS再生（5秒タイムアウト、GameOutroSequenceより少し長め）
        if (ttsTask != null)
        {
            float ttsWait = 0f;
            while (!ttsTask.IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (ttsTask.IsCompleted && !ttsTask.IsFaulted)
            {
                if (ttsTask.Result != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayVoice(ttsTask.Result, Vector3.zero, 1.0f);
                    Debug.Log("[ResultUI] TTS playback started for LLM analysis");
                }
                else
                {
                    Debug.LogWarning($"[ResultUI] TTS completed but cannot play - Result={ttsTask.Result != null}, AudioManager={AudioManager.Instance != null}");
                }
            }
            else
            {
                Debug.LogWarning($"[ResultUI] TTS generation timeout or failed - Completed={ttsTask.IsCompleted}, Faulted={ttsTask.IsFaulted}");
            }
        }

        yield return new WaitForSeconds(2.5f);
    }

    /// <summary>
    /// タイプライター効果
    /// </summary>
    private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string fullText, float charsPerSecond)
    {
        if (textComponent == null || string.IsNullOrEmpty(fullText)) yield break;

        // テキスト全体をセットしてレイアウト高さを確定させ、表示文字数で制御
        textComponent.text = fullText;
        textComponent.maxVisibleCharacters = 0;
        float interval = 1f / charsPerSecond;

        for (int i = 1; i <= fullText.Length; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(interval);
        }
    }

    // ========================================
    // UI生成（プログラマティック）
    // ========================================

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("ResultCanvas");
        canvasObj.transform.SetParent(transform);
        resultCanvas = canvasObj.AddComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        resultCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bgObj = CreateUIElement("Background", canvasObj.transform);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        StretchFill(bgRect);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = bgColor;
        backgroundGroup = bgObj.AddComponent<CanvasGroup>();

        // ScrollView
        GameObject scrollObj = CreateUIElement("ScrollView", bgObj.transform);
        StretchFill(scrollObj.GetComponent<RectTransform>());
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        // Viewport
        GameObject viewportObj = CreateUIElement("Viewport", scrollObj.transform);
        StretchFill(viewportObj.GetComponent<RectTransform>());
        viewportObj.AddComponent<Image>().color = Color.clear;
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        // Content container (scrollable)
        GameObject contentObj = CreateUIElement("Content", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(800, 0);
        contentRect.anchoredPosition = Vector2.zero;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        containerRect = contentRect;

        // Vertical Layout
        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.padding = new RectOffset(30, 30, 50, 50);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Header
        headerText = CreateText(contentObj.transform, "Header", "", 22, Color.white, TextAlignmentOptions.Center, 40f);
        headerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

        // Title
        titleText = CreateText(contentObj.transform, "Title", "", 42, goldColor, TextAlignmentOptions.Center, 60f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

        // Description (auto-height via TMP preferredHeight)
        descriptionText = CreateText(contentObj.transform, "Description", "", 18, Color.white, TextAlignmentOptions.Left, 0f);
        descriptionText.overflowMode = TextOverflowModes.Overflow;

        // Stats Container
        GameObject statsObj = CreateUIElement("StatsContainer", contentObj.transform);
        statsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 180);
        statsObj.AddComponent<LayoutElement>().preferredHeight = 180;
        VerticalLayoutGroup statsLayout = statsObj.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 5;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = true;

        statBarFills = new Image[5];
        statBarLabels = new TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            CreateStatBar(statsObj.transform, i);
        }

        // Stats container を非表示（データ計算は維持、表示のみOFF）
        statsObj.SetActive(false);

        // Tendency
        tendencyText = CreateText(contentObj.transform, "Tendency", "", 16,
            new Color(0.8f, 0.8f, 0.8f), TextAlignmentOptions.Left, 0f);
        tendencyText.overflowMode = TextOverflowModes.Overflow;

        // === 種明かしセクション ===
        // Separator
        CreateSeparator(contentObj.transform);

        evidenceContainer = CreateUIElement("EvidenceContainer", contentObj.transform);
        VerticalLayoutGroup evidenceLayout = evidenceContainer.AddComponent<VerticalLayoutGroup>();
        evidenceLayout.spacing = 8;
        evidenceLayout.childControlWidth = true;
        evidenceLayout.childControlHeight = false;
        evidenceLayout.childForceExpandWidth = true;
        evidenceLayout.childForceExpandHeight = false;

        evidenceHeaderText = CreateText(evidenceContainer.transform, "EvidenceHeader", "",
            20, evidenceHeaderColor, TextAlignmentOptions.Left, 35f);
        evidenceHeaderText.fontStyle = FontStyles.Bold;

        // === プロファイル照合セクション ===
        profileContainer = CreateUIElement("ProfileContainer", contentObj.transform);
        VerticalLayoutGroup profileLayout = profileContainer.AddComponent<VerticalLayoutGroup>();
        profileLayout.spacing = 5;
        profileLayout.childControlWidth = true;
        profileLayout.childControlHeight = false;
        profileLayout.childForceExpandWidth = true;
        profileLayout.childForceExpandHeight = false;

        profileHeaderText = CreateText(profileContainer.transform, "ProfileHeader", "",
            20, evidenceHeaderColor, TextAlignmentOptions.Left, 35f);
        profileHeaderText.fontStyle = FontStyles.Bold;

        // === LLM詳細分析セクション（ハイブリッドモード） ===
        llmAnalysisContainer = CreateUIElement("LLMAnalysisContainer", contentObj.transform);
        VerticalLayoutGroup llmLayout = llmAnalysisContainer.AddComponent<VerticalLayoutGroup>();
        llmLayout.spacing = 5;
        llmLayout.childControlWidth = true;
        llmLayout.childControlHeight = false;
        llmLayout.childForceExpandWidth = true;
        llmLayout.childForceExpandHeight = false;

        llmAnalysisText = CreateText(llmAnalysisContainer.transform, "LLMAnalysis", "",
            16, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Left, 0f);
        llmAnalysisText.fontStyle = FontStyles.Italic;
        llmAnalysisText.overflowMode = TextOverflowModes.Overflow;

        // === 締め ===
        CreateSeparator(contentObj.transform);

        // Insight
        insightText = CreateText(contentObj.transform, "Insight", "", 24, goldColor, TextAlignmentOptions.Center, 0f);
        insightText.fontStyle = FontStyles.Bold;
        insightText.overflowMode = TextOverflowModes.Overflow;

        // Buttons Container
        GameObject buttonsObj = CreateUIElement("Buttons", contentObj.transform);
        buttonsObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        buttonsObj.AddComponent<LayoutElement>().preferredHeight = 50;
        HorizontalLayoutGroup btnLayout = buttonsObj.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 20;
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.childControlWidth = false;
        btnLayout.childControlHeight = false;

        var locBtn = LocalizationManager.Instance;
        string replayLabel = locBtn != null ? locBtn.Get("result_ui.replay_button") : "もう一度遊ぶ";
        string menuLabel = locBtn != null ? locBtn.Get("result_ui.menu_button") : "メニューに戻る";
        replayButton = CreateButton(buttonsObj.transform, "ReplayButton", replayLabel, () =>
        {
            Debug.Log("[ResultUI] Replay button clicked, invoking callback...");
            OnReplayRequested?.Invoke();
        });
        menuButton = CreateButton(buttonsObj.transform, "MenuButton", menuLabel, () =>
        {
            Debug.Log("[ResultUI] Menu button clicked, invoking callback...");
            OnMenuRequested?.Invoke();
        });
    }

    private void CreateSeparator(Transform parent)
    {
        GameObject sep = CreateUIElement("Separator", parent);
        sep.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
        Image sepImage = sep.AddComponent<Image>();
        sepImage.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        sep.AddComponent<LayoutElement>().preferredHeight = 2;
    }

    private void CreateStatBar(Transform parent, int index)
    {
        GameObject barRow = CreateUIElement($"StatBar_{index}", parent);
        HorizontalLayoutGroup rowLayout = barRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        // Label
        statBarLabels[index] = CreateText(barRow.transform, "Label", GetStatNames()[index], 14, Color.white, TextAlignmentOptions.Left, 30f);
        statBarLabels[index].GetComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
        LayoutElement labelLE = statBarLabels[index].gameObject.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 80;

        // Bar Background
        GameObject barBg = CreateUIElement("BarBg", barRow.transform);
        barBg.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 20);
        Image barBgImage = barBg.AddComponent<Image>();
        barBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        LayoutElement barBgLE = barBg.AddComponent<LayoutElement>();
        barBgLE.preferredWidth = 400;
        barBgLE.preferredHeight = 20;

        // Bar Fill
        GameObject fillObj = CreateUIElement("Fill", barBg.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        statBarFills[index] = fillObj.AddComponent<Image>();
        statBarFills[index].color = barColor;
        statBarFills[index].type = Image.Type.Filled;
        statBarFills[index].fillMethod = Image.FillMethod.Horizontal;
        statBarFills[index].fillAmount = 0f;
    }

    // ========================================
    // UIヘルパー
    // ========================================

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void StretchFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment, float height)
    {
        GameObject obj = CreateUIElement(name, parent);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, height);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement(name, parent);
        btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 45);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.interactable = true;

        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        TextMeshProUGUI btnText = CreateText(btnObj.transform, "Text", label, 18, Color.white, TextAlignmentOptions.Center, 45f);
        StretchFill(btnText.GetComponent<RectTransform>());

        btn.onClick.AddListener(onClick);
        Debug.Log($"[ResultUI] Button created: {name}, Label: {label}, Listener count: {btn.onClick.GetPersistentEventCount()}");
        return btn;
    }

    private void OnDestroy()
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.OnLanguageChanged -= OnLanguageChanged;
        animationSequence?.Kill();
    }
}
