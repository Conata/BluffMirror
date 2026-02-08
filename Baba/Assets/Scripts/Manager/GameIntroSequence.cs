using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using FPSTrump.AI.LLM;
using FPSTrump.Manager;
using FPSTrump.Psychology;

/// <summary>
/// Phase 7-2: 没入型ゲームイントロ演出
/// 3幕構成: Discovery → Analysis → Challenge
/// </summary>
public class GameIntroSequence : MonoBehaviour
{
    public static GameIntroSequence Instance { get; private set; }

    [Header("System Dependencies")]
    [SerializeField] private CameraCinematicsSystem cameraSystem;
    [SerializeField] private TVHeadAnimator tvHeadAnimator;
    [SerializeField] private FloatingTextSystem floatingTextSystem;
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private LLMManager llmManager;

    [Header("Lighting (Optional)")]
    [SerializeField] private Light ambienceLight;
    [SerializeField] private Light tvHeadSpotlight;
    [SerializeField] private Light tableSpotlight;

    [Header("Act 1: Discovery (0-4s)")]
    [SerializeField] private float act1Duration = 4.0f;
    [SerializeField] private float cameraDollyInDuration = 3.0f;
    [SerializeField] private float ambienceLightStart = 0.3f;

    [Header("Act 2: Analysis (4-10s)")]
    [SerializeField] private float act2Duration = 6.0f;
    [SerializeField] private float dialogue1Duration = 2.5f;
    [SerializeField] private float dialogue2Duration = 3.0f;
    [SerializeField] private float tvSpotlightIntensity = 1.0f;
    [SerializeField] private Color tvSpotlightColor = new Color(0.3f, 0.9f, 1f); // Cyan

    [Header("Act 3: Challenge (10-15s)")]
    [SerializeField] private float act3Duration = 5.0f;
    [SerializeField] private float cameraDollyOutDuration = 4.0f;
    [SerializeField] private float dialogue3Duration = 3.0f;
    [SerializeField] private float dialogue4Duration = 2.0f;
    [SerializeField] private float tableSpotlightIntensity = 0.8f;

    public enum TextDisplayMode
    {
        FloatingText,    // 3D空間テキスト
        Subtitle,        // 画面下部字幕
        Both             // 両方表示
    }

    [Header("Text Display Settings")]
    [SerializeField] private TextDisplayMode textDisplayMode = TextDisplayMode.Subtitle;
    [SerializeField] private Vector3 dialoguePosition = new Vector3(0, 1.5f, 2f);

    [Header("Optional Skip")]
    [SerializeField] private bool allowSkip = true;
    private bool skipRequested = false;

    // === Pre-generation cache ===
    private bool pregenIsBirthday;
    private Task<string>[] pregenTextTasks;
    private Task<AudioClip>[] pregenTTSTasks;

    // === Stage 10: Camera appearance compliment ===
    private Task<(string compliment, string description)> appearanceTask;
    private Task<AudioClip> appearanceTTSTask;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Auto-find dependencies
        if (cameraSystem == null)
            cameraSystem = FindFirstObjectByType<CameraCinematicsSystem>();
        if (tvHeadAnimator == null)
            tvHeadAnimator = FindFirstObjectByType<TVHeadAnimator>();
        if (floatingTextSystem == null)
            floatingTextSystem = FloatingTextSystem.Instance;
        if (subtitleUI == null)
            subtitleUI = SubtitleUI.Instance;
        if (llmManager == null)
            llmManager = FPSTrump.AI.LLM.LLMManager.Instance;

        // Auto-find lights
        if (ambienceLight == null)
            ambienceLight = GameObject.Find("AmbienceLight")?.GetComponent<Light>();
        if (tvHeadSpotlight == null)
            tvHeadSpotlight = GameObject.Find("TVHeadSpotlight")?.GetComponent<Light>();
        if (tableSpotlight == null)
            tableSpotlight = GameObject.Find("TableSpotlight")?.GetComponent<Light>();
    }

    private void Update()
    {
        // Escキーでスキップ
        if (allowSkip && Input.GetKeyDown(KeyCode.Escape))
        {
            skipRequested = true;
        }
    }

    /// <summary>
    /// Phase 7-2: 3幕構成イントロシーケンス実行
    /// </summary>
    public IEnumerator PlayIntroSequence()
    {
        Debug.Log("[GameIntro] === INTRO SEQUENCE START ===");
        skipRequested = false;

        // シーン遷移時にllmManagerがnullになる可能性があるため、再取得
        if (llmManager == null)
        {
            llmManager = FPSTrump.AI.LLM.LLMManager.Instance;
            Debug.Log($"[GameIntro] LLMManager re-acquired: {llmManager != null}");
        }

        // Stage 10: カメラキャプチャ + 外見褒め生成を非同期開始
        StartAppearanceCapture();

        // Act 1中に完了するようプリ生成を開始
        StartPreGeneration();

        // === ACT 1: DISCOVERY (0-4s) ===
        yield return StartCoroutine(PlayAct1_Discovery());
        if (skipRequested) { SkipToEnd(); yield break; }

        // === ACT 2: ANALYSIS (4-10s) ===
        yield return StartCoroutine(PlayAct2_Analysis());
        if (skipRequested) { SkipToEnd(); yield break; }

        // === ACT 3: CHALLENGE (10-15s) ===
        yield return StartCoroutine(PlayAct3_Challenge());

        // === CLEANUP ===
        yield return StartCoroutine(Cleanup());

        Debug.Log("[GameIntro] === INTRO SEQUENCE COMPLETE ===");
    }

    /// <summary>
    /// Act 1: Discovery - AIがプレイヤーの存在に気づく
    /// </summary>
    private IEnumerator PlayAct1_Discovery()
    {
        Debug.Log("[GameIntro] Act 1: Discovery");

        // 照明: やや暗め
        SetLightIntensity(ambienceLight, ambienceLightStart);
        SetLightIntensity(tvHeadSpotlight, 0f);
        SetLightIntensity(tableSpotlight, 0f);

        // TVHead: Neutral
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        // Camera: Dolly In (3秒)
        if (cameraSystem != null)
        {
            // ShowAIReactionView を使用、またはカスタムカメラ移動
            cameraSystem.ShowAIReactionView();
        }

        // 音響: イントロ用の不気味な囁き環境音
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWhisperAmbience();
        }

        // デバッグ: プリ生成完了を確実に待つため、Act 1を8秒に延長
        yield return new WaitForSeconds(7.0f);

        // TVHead: Curious (目が動く)
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetCurious(); // NEW expression
        }

        yield return new WaitForSeconds(1.0f); // Total: 8秒
    }

    /// <summary>
    /// Act 2: Analysis - AIがプレイヤーを観察・分析
    /// 占いデータはLLMプロンプトに注入済み、自然な会話劇として表現される
    /// </summary>
    private IEnumerator PlayAct2_Analysis()
    {
        Debug.Log("[GameIntro] Act 2: Analysis");

        // TVHead: Focused
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetFocused();
        }

        // Spotlight: TVHead に cyan spotlight
        if (tvHeadSpotlight != null)
        {
            tvHeadSpotlight.color = tvSpotlightColor;
            DOTween.To(() => tvHeadSpotlight.intensity, x => tvHeadSpotlight.intensity = x,
                tvSpotlightIntensity, 1.0f);
        }

        // === Stage 10: 外見褒め表示（Act 2冒頭） ===
        yield return StartCoroutine(ShowAppearanceCompliment());

        // === プリ生成済みLLMダイアログの表示（統一フロー） ===
        // Dialogue 1: Discovery/Analysis開始
        yield return StartCoroutine(ShowPregenDialogue(0, 0f, 0));
        yield return new WaitForSeconds(dialogue1Duration);
        HideDialogue();

        if (tvHeadAnimator != null)
            tvHeadAnimator.SetSmirk();

        yield return new WaitForSeconds(0.5f);

        // Dialogue 2: 分析結果の提示（占いデータが自然に織り込まれる）
        yield return StartCoroutine(ShowPregenDialogue(1, 0.5f, 1));
        yield return new WaitForSeconds(dialogue2Duration);
        HideDialogue();

        Debug.Log("[GameIntro] Act 2 completed");
    }

    /// <summary>
    /// Act 3: Challenge - 心理戦宣言とゲーム開始
    /// </summary>
    private IEnumerator PlayAct3_Challenge()
    {
        Debug.Log("[GameIntro] Act 3: Challenge");

        // Camera: テーブル全景へ切り替え
        if (cameraSystem != null)
        {
            cameraSystem.ShowTableOverview();
        }

        // Lighting: Table spotlight点灯
        if (tableSpotlight != null)
        {
            DOTween.To(() => tableSpotlight.intensity, x => tableSpotlight.intensity = x,
                tableSpotlightIntensity, 2.0f);
        }

        // 音響: カードシャッフル音 + BGM再生確認
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardFlip(0.8f);
            AudioManager.Instance.PlayMusic();
        }

        yield return new WaitForSeconds(1.0f);

        // === Dialogue 3 (Challenge, プリ生成済み) ===
        yield return StartCoroutine(ShowPregenDialogue(2, 1.0f, 2));
        yield return new WaitForSeconds(dialogue3Duration);
        HideDialogue();

        // TVHead: Neutral (真剣)
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        yield return new WaitForSeconds(0.5f);

        // === Dialogue 4 (Final, プリ生成済み) ===
        yield return StartCoroutine(ShowPregenDialogue(3, 0f, 3));
        yield return new WaitForSeconds(dialogue4Duration);

        Debug.Log("[GameIntro] Act 3 completed");
    }

    /// <summary>
    /// Cleanup: 演出終了処理
    /// </summary>
    private IEnumerator Cleanup()
    {
        Debug.Log("[GameIntro] Cleanup started");
        HideDialogue();

        // Stage 10: イントロ用カメラキャプチャ完了
        // Note: Part B（リアルタイム表情分析）実装後はここでカメラを止めない
        // 現時点ではイントロ終了時にカメラを停止しない（Part Bで継続使用）

        // Camera: ShowPlayerTurnView
        if (cameraSystem != null)
        {
            cameraSystem.ShowPlayerTurnView();
        }

        // Lighting: 通常の明るさへ
        if (ambienceLight != null)
        {
            DOTween.To(() => ambienceLight.intensity, x => ambienceLight.intensity = x,
                0.7f, 1.0f);
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log("[GameIntro] Cleanup completed");
    }

    /// <summary>
    /// スキップ処理
    /// </summary>
    private void SkipToEnd()
    {
        Debug.Log("[GameIntro] Intro sequence skipped");

        // Stage 10: カメラ停止
        if (WebCamManager.Instance != null)
        {
            WebCamManager.Instance.StopCapture();
        }

        // 音声停止
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
        }

        // すべてのテキスト非表示
        if (floatingTextSystem != null)
        {
            floatingTextSystem.HidePersistentText();
        }
        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }

        // カメラを通常視点へ
        if (cameraSystem != null)
        {
            cameraSystem.ShowPlayerTurnView();
        }

        // TVHeadをNeutralへ
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        // 照明を通常へ
        SetLightIntensity(ambienceLight, 0.7f);
        SetLightIntensity(tvHeadSpotlight, 0f);
        SetLightIntensity(tableSpotlight, 0.8f);
    }

    // ========================================
    // Stage 10: Camera Appearance System
    // ========================================

    /// <summary>
    /// Stage 10: カメラキャプチャ + 外見褒め生成を非同期開始
    /// Act 1中に並行して実行される
    /// </summary>
    private void StartAppearanceCapture()
    {
        if (llmManager == null) return;

        var webCam = WebCamManager.Instance;
        if (webCam == null || !webCam.HasCamera)
        {
            Debug.Log("[GameIntro] No camera available, skipping appearance capture");
            return;
        }

        appearanceTask = CaptureAndAnalyzeAppearanceAsync();
    }

    /// <summary>
    /// カメラキャプチャ → Vision API → 褒め言葉生成の非同期パイプライン
    /// </summary>
    private async Task<(string compliment, string description)> CaptureAndAnalyzeAppearanceAsync()
    {
        try
        {
            var webCam = WebCamManager.Instance;
            if (webCam == null) return (null, null);

            // カメラ起動 + 安定待ち + 1枚撮影
            string base64 = await webCam.CaptureOneFrameAsync(0.5f);
            if (string.IsNullOrEmpty(base64))
            {
                Debug.LogWarning("[GameIntro] Camera capture returned null");
                return (null, null);
            }

            // Vision API で外見褒め生成
            var result = await llmManager.GenerateAppearanceComplimentAsync(base64);

            // TTS生成もチェーン
            if (!string.IsNullOrEmpty(result.compliment))
            {
                appearanceTTSTask = llmManager.GenerateTTSAsync(result.compliment, AIEmotion.Calm);
            }

            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameIntro] Appearance capture pipeline failed: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Stage 10: 外見褒めコメントをAct 2冒頭で表示 + TTS再生
    /// appearanceTaskが未完了または失敗の場合はスキップ（graceful degradation）
    /// </summary>
    private IEnumerator ShowAppearanceCompliment()
    {
        Debug.Log("[GameIntro] ShowAppearanceCompliment started");

        if (appearanceTask == null)
        {
            Debug.Log("[GameIntro] appearanceTask is null, skipping");
            yield break;
        }

        // 結果待ち（最大2秒。Act 1中に大半処理済みなのでここでは短い）
        float waitTime = 0f;
        while (!appearanceTask.IsCompleted && waitTime < 2f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (!appearanceTask.IsCompleted || appearanceTask.IsFaulted)
        {
            Debug.Log($"[GameIntro] Appearance compliment not ready (completed={appearanceTask.IsCompleted}, faulted={appearanceTask.IsFaulted}), skipping");
            yield break;
        }

        var (compliment, _) = appearanceTask.Result;
        if (string.IsNullOrEmpty(compliment))
        {
            Debug.Log("[GameIntro] Appearance compliment text is empty, skipping");
            yield break;
        }

        Debug.Log($"[GameIntro] Appearance compliment text received ({compliment.Length} chars)");

        // Stage 15: 会話履歴に記録（イントロはターン0）
        FPSTrump.AI.LLM.LLMContextWindow.AddDialogueToHistory(0, compliment, "Intro - Appearance Compliment");

        // TVHead: Smirk（褒めるときの表情）
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetSmirk();
        }

        // テキスト表示
        ShowDialogue(compliment, 0.3f);

        // TTS再生（5秒タイムアウト）
        if (appearanceTTSTask != null && AudioManager.Instance != null)
        {
            // 音声停止（前のAct 1の音声と重複防止）
            AudioManager.Instance.StopVoice();

            float ttsWait = 0f;
            while (!appearanceTTSTask.IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (skipRequested) yield break;

            if (appearanceTTSTask.IsCompleted && !appearanceTTSTask.IsFaulted &&
                appearanceTTSTask.Result != null)
            {
                AudioManager.Instance.PlayVoice(appearanceTTSTask.Result, Vector3.zero, 1.0f);
            }
        }

        yield return new WaitForSeconds(2.5f);
        HideDialogue();
        yield return new WaitForSeconds(0.3f);

        // TVHead: Focused（通常の分析モードに戻す）
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetFocused();
        }

        Debug.Log("[GameIntro] ShowAppearanceCompliment completed");
    }

    // ========================================
    // Pre-generation System
    // ========================================

    /// <summary>
    /// プリ生成開始: Act 1中に全ダイアログのテキスト＋TTS生成を並列実行
    /// 全てLLM生成に統一（占いデータはプロンプトに注入済み）
    /// 4 slots: Act2 dialogue1, Act2 dialogue2, Act3 dialogue3, Act3 dialogue4
    /// </summary>
    private void StartPreGeneration()
    {
        if (llmManager == null) return;

        // 生年月日の有無を記録（ログ用のみ）
        var birthdayManager = PlayerBirthdayManager.Instance;
        pregenIsBirthday = birthdayManager != null && birthdayManager.HasBirthday();

        // 全てLLM生成の統一フロー（占いデータは既にLLMプロンプトに注入されている）
        pregenTextTasks = new Task<string>[4];
        pregenTTSTasks = new Task<AudioClip>[4];

        // Slots 0-3: 全ダイアログをLLM生成（テキスト→TTSチェーン）
        for (int i = 0; i < 4; i++)
        {
            pregenTextTasks[i] = GetIntroDialogueAsync(i, 4);
            pregenTTSTasks[i] = ChainTTSAsync(pregenTextTasks[i]);
        }

        Debug.Log($"[GameIntro] Pre-generation started (birthday={pregenIsBirthday}, slots={pregenTextTasks.Length}, all LLM-generated)");
    }

    /// <summary>
    /// テキスト生成完了後にTTS生成をチェーン実行
    /// </summary>
    private async Task<AudioClip> ChainTTSAsync(Task<string> textTask)
    {
        try
        {
            string text = await textTask;
            if (string.IsNullOrEmpty(text) || llmManager == null) return null;
            return await llmManager.GenerateTTSAsync(text, AIEmotion.Calm);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameIntro] ChainTTS failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// プリ生成済みテキスト＋TTS音声を使ってセリフ表示
    /// テキストは即座に表示、TTS完了待ちは5秒タイムアウト付き
    /// </summary>
    private IEnumerator ShowPregenDialogue(int slot, float pressureLevel, int fallbackIndex)
    {
        Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} started");

        if (pregenTextTasks == null || slot >= pregenTextTasks.Length)
        {
            Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - invalid slot or null tasks");
            yield break;
        }

        // テキスト取得（10秒タイムアウトに延長）
        float waitTime = 0f;
        while (!pregenTextTasks[slot].IsCompleted && waitTime < 10f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        string text = null;
        if (pregenTextTasks[slot].IsCompleted && !pregenTextTasks[slot].IsFaulted)
        {
            text = pregenTextTasks[slot].Result;
            Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - LLM text received ({text?.Length ?? 0} chars)");
        }
        else if (pregenTextTasks[slot].IsFaulted)
        {
            Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - LLM task faulted: {pregenTextTasks[slot].Exception?.Message}");
        }
        else
        {
            Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - LLM text timeout after {waitTime:F2}s");
        }

        // フォールバック
        if (string.IsNullOrEmpty(text) && fallbackIndex >= 0)
        {
            text = GetIntroDialogueFallback(fallbackIndex, 4);
            Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - using fallback text ({text?.Length ?? 0} chars)");
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - no text available, skipping");
            yield break;
        }

        // テキスト即座表示
        ShowDialogue(text, pressureLevel);
        Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - dialogue shown");

        // TTS再生（10秒タイムアウトに延長）
        if (pregenTTSTasks != null && slot < pregenTTSTasks.Length &&
            pregenTTSTasks[slot] != null && AudioManager.Instance != null)
        {
            float ttsWait = 0f;
            while (!pregenTTSTasks[slot].IsCompleted && ttsWait < 10f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (skipRequested)
            {
                Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - skip requested");
                yield break;
            }

            if (pregenTTSTasks[slot].IsCompleted && !pregenTTSTasks[slot].IsFaulted &&
                pregenTTSTasks[slot].Result != null)
            {
                var audioClip = pregenTTSTasks[slot].Result;
                Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - About to play TTS: clip={audioClip != null}, length={audioClip?.length ?? 0}s, AudioManager={AudioManager.Instance != null}");
                AudioManager.Instance.PlayVoice(audioClip, Vector3.zero, 1.0f);
                Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} - TTS playing");
            }
            else if (pregenTTSTasks[slot].IsFaulted)
            {
                Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - TTS task faulted: {pregenTTSTasks[slot].Exception?.Message}");
            }
            else if (!pregenTTSTasks[slot].IsCompleted)
            {
                Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - TTS timeout after {ttsWait:F2}s");
            }
            else
            {
                Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - TTS result is null");
            }
        }
        else
        {
            Debug.LogWarning($"[GameIntro] ShowPregenDialogue slot={slot} - TTS not available (tasks={pregenTTSTasks != null}, AudioManager={AudioManager.Instance != null})");
        }

        Debug.Log($"[GameIntro] ShowPregenDialogue slot={slot} completed");
    }

    /// <summary>
    /// LLMでイントロセリフ生成 (fallback付き)
    /// </summary>
    private async Task<string> GetIntroDialogueAsync(int dialogueIndex, int totalDialogues)
    {
        if (llmManager != null)
        {
            try
            {
                string dialogue = await llmManager.GenerateIntroDialogue(
                    dialogueIndex,
                    totalDialogues
                );

                if (!string.IsNullOrEmpty(dialogue))
                {
                    // Stage 15: 会話履歴に記録（イントロはターン0）
                    string context = $"Intro Act {dialogueIndex + 2} (dialogue {dialogueIndex + 1}/{totalDialogues})";
                    FPSTrump.AI.LLM.LLMContextWindow.AddDialogueToHistory(0, dialogue, context);

                    return dialogue;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameIntro] LLM failed: {ex.Message}");
            }
        }

        // フォールバック: 静的セリフ
        return GetIntroDialogueFallback(dialogueIndex, totalDialogues);
    }

    /// <summary>
    /// 静的イントロセリフ（fallback）
    /// Act 2では生年月日と性格予測を匂わせる
    /// </summary>
    private string GetIntroDialogueFallback(int dialogueIndex, int totalDialogues)
    {
        var loc = LocalizationManager.Instance;

        // Act 2: 生年月日＋性格予測を匂わせるセリフを動的生成
        if (dialogueIndex == 1)
        {
            string personalityDialogue = BuildPersonalityHintDialogue();
            if (personalityDialogue != null) return personalityDialogue;
            // nullの場合はLocalizationにフォールバック
        }

        // Act 3: 性格を暴くことを宣言
        if (dialogueIndex == 2)
        {
            string challengeDialogue = BuildChallengeDialogue();
            if (challengeDialogue != null) return challengeDialogue;
            // nullの場合はLocalizationにフォールバック
        }

        // デフォルトの静的セリフ（JSONから読み込み）
        if (loc != null)
        {
            string key = $"intro.dialogue_act{dialogueIndex}";
            string[] options = loc.GetArray(key);
            if (options.Length > 0)
            {
                string selected = options[Random.Range(0, options.Length)];
                Debug.Log($"[GameIntro] Using fallback dialogue for act{dialogueIndex}: \"{selected}\"");
                return selected;
            }
        }

        Debug.LogWarning($"[GameIntro] No fallback dialogue found for act{dialogueIndex}");
        return "...";
    }

    /// <summary>
    /// Act 2用: 生年月日＋性格予測を匂わせるセリフ生成
    /// </summary>
    private string BuildPersonalityHintDialogue()
    {
        var loc = LocalizationManager.Instance;
        var birthdayManager = PlayerBirthdayManager.Instance;
        bool hasBirthday = birthdayManager != null && birthdayManager.HasBirthday();

        // PersonalityProfileから性格ヒントを取得
        string personalityHint = null;
        if (llmManager != null && llmManager.CurrentPlayerProfile != null && loc != null)
        {
            var p = llmManager.CurrentPlayerProfile;
            personalityHint = p.cautiousness > 0.6f ? loc.Get("intro.personality_hint_cautious")
                : p.intuition > 0.6f ? loc.Get("intro.personality_hint_intuitive")
                : p.resilience > 0.7f ? loc.Get("intro.personality_hint_resilient")
                : p.consistency > 0.7f ? loc.Get("intro.personality_hint_consistent")
                : p.adaptability > 0.6f ? loc.Get("intro.personality_hint_adaptive")
                : null;
        }

        if (loc == null) return null;

        // 生年月日あり＋性格ヒントあり → 最も具体的なセリフ
        if (hasBirthday && personalityHint != null)
        {
            var (_, month, day) = birthdayManager.GetBirthday();
            string[] templates = loc.GetArray("intro.birthday_personality");
            if (templates.Length > 0)
            {
                string template = templates[Random.Range(0, templates.Length)];
                return LocalizationManager.ApplyVars(template, ("month", month.ToString()), ("day", day.ToString()), ("personalityHint", personalityHint));
            }
        }

        // 生年月日のみ
        if (hasBirthday)
        {
            var (_, month, day) = birthdayManager.GetBirthday();
            string[] templates = loc.GetArray("intro.birthday_only");
            if (templates.Length > 0)
            {
                string template = templates[Random.Range(0, templates.Length)];
                return LocalizationManager.ApplyVars(template, ("month", month.ToString()), ("day", day.ToString()));
            }
        }

        // 性格ヒントのみ
        if (personalityHint != null)
        {
            string[] templates = loc.GetArray("intro.personality_only");
            if (templates.Length > 0)
            {
                string template = templates[Random.Range(0, templates.Length)];
                return LocalizationManager.ApplyVars(template, ("personalityHint", personalityHint));
            }
        }

        return null;
    }

    /// <summary>
    /// Act 3用: 性格を暴くことを宣言するセリフ
    /// </summary>
    private string BuildChallengeDialogue()
    {
        if (llmManager == null || llmManager.CurrentPlayerProfile == null)
            return null;

        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            string[] options = loc.GetArray("intro.challenge");
            if (options.Length > 0)
                return options[Random.Range(0, options.Length)];
        }

        return null;
    }

    /// <summary>
    /// セリフ表示
    /// </summary>
    private void ShowDialogue(string text, float pressureLevel)
    {
        // 表示方法に応じてテキストを表示
        switch (textDisplayMode)
        {
            case TextDisplayMode.FloatingText:
                if (floatingTextSystem != null)
                {
                    floatingTextSystem.ShowPersistentText(dialoguePosition, text, pressureLevel);
                }
                break;

            case TextDisplayMode.Subtitle:
                if (subtitleUI != null)
                {
                    subtitleUI.Show(text, pressureLevel);
                }
                break;

            case TextDisplayMode.Both:
                if (floatingTextSystem != null)
                {
                    floatingTextSystem.ShowPersistentText(dialoguePosition, text, pressureLevel);
                }
                if (subtitleUI != null)
                {
                    subtitleUI.Show(text, pressureLevel);
                }
                break;
        }

        // 音声はShowPregenDialogue()経由でTTS再生される
    }

    /// <summary>
    /// セリフ非表示
    /// </summary>
    private void HideDialogue()
    {
        // 両方のシステムを非表示（どちらが使われていても安全）
        if (floatingTextSystem != null)
        {
            floatingTextSystem.HidePersistentText();
        }
        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }
    }

    /// <summary>
    /// 照明の強度設定
    /// </summary>
    private void SetLightIntensity(Light light, float intensity)
    {
        if (light != null)
        {
            light.intensity = intensity;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Play Intro Sequence")]
    private void TestPlayIntro()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameIntro] Test must be run in Play Mode");
            return;
        }

        StartCoroutine(PlayIntroSequence());
    }
#endif
}
