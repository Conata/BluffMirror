using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using FPSTrump.AI.LLM;
using FPSTrump.Psychology;
using FPSTrump.Result;

/// <summary>
/// ゲームアウトロ演出: Result UI表示前にAIが性格を口頭で暴く
/// 3幕構成: Reflection → Reveal → Transition
/// GameIntroSequenceと対になる構造
/// </summary>
public class GameOutroSequence : MonoBehaviour
{
    public static GameOutroSequence Instance { get; private set; }

    [Header("System Dependencies")]
    [SerializeField] private CameraCinematicsSystem cameraSystem;
    [SerializeField] private TVHeadAnimator tvHeadAnimator;
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private LLMManager llmManager;

    [Header("Act 1: Reflection")]
    [SerializeField] private float act1PauseBefore = 1.0f;
    [SerializeField] private float act1DialogueWait = 3.5f;

    [Header("Act 2: Personality Reveal")]
    [SerializeField] private float act2PauseBefore = 0.5f;
    [SerializeField] private float act2RevealWait = 3.0f;
    [SerializeField] private float act2EvidenceWait = 3.5f;

    [Header("Act 3: Transition")]
    [SerializeField] private float act3PauseBefore = 0.5f;
    [SerializeField] private float act3TransitionWait = 2.0f;

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;
    private bool skipRequested;

    private DiagnosisResult diagnosis;
    private GameSessionData sessionData;

    // === Pre-generation cache ===
    private Task<string>[] pregenTextTasks;
    private Task<AudioClip>[] pregenTTSTasks;

    // === LLM detailed analysis ===
    private Task<string> llmAnalysisTask;
    private Task<AudioClip> llmAnalysisTTSTask;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (cameraSystem == null)
            cameraSystem = FindFirstObjectByType<CameraCinematicsSystem>();
        if (tvHeadAnimator == null)
            tvHeadAnimator = FindFirstObjectByType<TVHeadAnimator>();
        if (subtitleUI == null)
            subtitleUI = SubtitleUI.Instance;
        if (llmManager == null)
            llmManager = LLMManager.Instance;
    }

    private void Update()
    {
        if (allowSkip && Input.GetKeyDown(KeyCode.Escape))
        {
            skipRequested = true;
        }
    }

    /// <summary>
    /// アウトロシーケンス実行（GameManagerから呼ばれる）
    /// </summary>
    public IEnumerator PlayOutroSequence(DiagnosisResult diagnosis, GameSessionData sessionData)
    {
        Debug.Log("[GameOutro] === OUTRO SEQUENCE START ===");
        this.diagnosis = diagnosis;
        this.sessionData = sessionData;
        skipRequested = false;

        // 全ダイアログのLLM生成 + TTSをプリ生成（イントロと同じパターン）
        StartPreGeneration(sessionData.playerWon, sessionData);
        Debug.Log("[GameOutro] Pre-generation started (4 dialogues + TTS, LLM先行)");

        // LLM詳細分析をバックグラウンドで生成開始（ハイブリッド方式）
        if (llmManager != null)
        {
            llmAnalysisTask = llmManager.GenerateOutroDetailedAnalysis(diagnosis, sessionData);
            Debug.Log("[GameOutro] LLM detailed analysis started in background");
        }

        // === ACT 1: REFLECTION ===
        yield return StartCoroutine(PlayAct1_Reflection());
        if (skipRequested) { SkipToEnd(); yield break; }

        // === ACT 2: PERSONALITY REVEAL ===
        yield return StartCoroutine(PlayAct2_Reveal());
        if (skipRequested) { SkipToEnd(); yield break; }

        // === ACT 3: TRANSITION ===
        yield return StartCoroutine(PlayAct3_Transition());

        Debug.Log("[GameOutro] === OUTRO SEQUENCE COMPLETE ===");
    }

    // ========================================
    // Act 1: Reflection - ゲームを振り返る
    // ========================================

    private IEnumerator PlayAct1_Reflection()
    {
        Debug.Log("[GameOutro] Act 1: Reflection");

        // Camera: AIの顔にフォーカス
        if (cameraSystem != null)
        {
            cameraSystem.ShowAIReactionView();
        }

        yield return new WaitForSeconds(act1PauseBefore);

        // TVHead: Neutral → Curious
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        // セリフ: ゲームの感想（プリ生成済み slot 0）
        yield return StartCoroutine(ShowPregenOutroDialogue(0, 0.2f));

        yield return new WaitForSeconds(1.0f);

        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetCurious();
        }

        yield return new WaitForSeconds(act1DialogueWait - 1.0f);

        HideDialogue();
        yield return new WaitForSeconds(0.3f);
    }

    // ========================================
    // Act 2: Reveal - 性格を暴く
    // ========================================

    private IEnumerator PlayAct2_Reveal()
    {
        Debug.Log("[GameOutro] Act 2: Personality Reveal");

        // TVHead: Smirk（ニヤリ）
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetSmirk();
        }

        yield return new WaitForSeconds(act2PauseBefore);

        // セリフ1: 性格タイプの宣告（プリ生成済み slot 1）
        yield return StartCoroutine(ShowPregenOutroDialogue(1, 0.5f));

        yield return new WaitForSeconds(act2RevealWait);

        HideDialogue();
        yield return new WaitForSeconds(0.3f);

        // セリフ2: 証拠の提示（プリ生成済み slot 2）
        yield return StartCoroutine(ShowPregenOutroDialogue(2, 0.4f));

        yield return new WaitForSeconds(act2EvidenceWait);

        HideDialogue();
        yield return new WaitForSeconds(0.3f);

        // === LLM詳細分析を追加表示（ハイブリッド方式） ===
        yield return StartCoroutine(ShowLLMDetailedAnalysis());
    }

    // ========================================
    // Act 3: Transition - Result UIへの橋渡し
    // ========================================

    private IEnumerator PlayAct3_Transition()
    {
        Debug.Log("[GameOutro] Act 3: Transition");

        // TVHead: Neutral
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        // Camera: テーブル俯瞰に戻す
        if (cameraSystem != null)
        {
            cameraSystem.ShowTableOverview();
        }

        yield return new WaitForSeconds(act3PauseBefore);

        // セリフ: 結果画面への導入（プリ生成済み slot 3）
        yield return StartCoroutine(ShowPregenOutroDialogue(3, 0.1f));

        yield return new WaitForSeconds(act3TransitionWait);

        HideDialogue();
        yield return new WaitForSeconds(0.3f);
    }

    // ========================================
    // Pre-generation (LLM + TTS)
    // ========================================

    /// <summary>
    /// アウトロ台詞のプリ生成（イントロと同じパターン）
    /// PlayOutroSequence開始直後に呼び出し
    /// </summary>
    private void StartPreGeneration(bool playerWon, GameSessionData sessionData)
    {
        if (llmManager == null)
        {
            // Fallback: 静的テキストのみ
            pregenTextTasks = new Task<string>[4];
            pregenTTSTasks = new Task<AudioClip>[4];
            pregenTextTasks[0] = Task.FromResult(GetReflectionDialogue(playerWon));
            pregenTextTasks[1] = Task.FromResult(GetRevealDialogue(diagnosis.personalityTitle));
            pregenTextTasks[2] = Task.FromResult(GetEvidenceTeaseDialogue());
            pregenTextTasks[3] = Task.FromResult(GetTransitionDialogue());
            return;
        }

        Debug.Log("[GameOutro] Pre-generation started (LLM先行試行)");

        pregenTextTasks = new Task<string>[4];
        pregenTTSTasks = new Task<AudioClip>[4];

        for (int i = 0; i < 4; i++)
        {
            int index = i;
            pregenTextTasks[i] = GetOutroDialogueAsync(index, playerWon, sessionData);
            pregenTTSTasks[i] = ChainTTSAsync(pregenTextTasks[i]);
        }
    }

    private async Task<string> GetOutroDialogueAsync(
        int index,
        bool playerWon,
        GameSessionData sessionData)
    {
        // LLM先行試行
        if (llmManager != null)
        {
            string llmDialogue = await llmManager.GenerateOutroBaseDialogue(index, playerWon, sessionData);
            if (!string.IsNullOrEmpty(llmDialogue))
            {
                return llmDialogue;
            }
        }

        // フォールバック: 既存の静的テーブル
        return GetOutroDialogueFallback(index, playerWon);
    }

    private string GetOutroDialogueFallback(int index, bool playerWon)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return "...";

        switch (index)
        {
            case 0:
                return GetReflectionDialogue(playerWon);
            case 1:
                string personalityTitle = diagnosis?.personalityTitle ?? "...";
                return GetRevealDialogue(personalityTitle);
            case 2:
                return GetEvidenceTeaseDialogue();
            case 3:
                return GetTransitionDialogue();
            default:
                return "...";
        }
    }

    private async Task<AudioClip> ChainTTSAsync(Task<string> textTask)
    {
        string text = await textTask;
        if (string.IsNullOrEmpty(text)) return null;

        if (llmManager == null) return null;

        return await llmManager.GenerateTTSAsync(text, AIEmotion.Calm, useCache: false);
    }

    // ========================================
    // Skip
    // ========================================

    private void SkipToEnd()
    {
        Debug.Log("[GameOutro] Outro sequence skipped");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopVoice();
        }

        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }

        if (cameraSystem != null)
        {
            cameraSystem.ShowTableOverview();
        }

        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }
    }

    // ========================================
    // Pre-gen Dialogue Display + TTS
    // ========================================

    /// <summary>
    /// プリ生成済みテキスト＋TTS音声を使ってセリフ表示
    /// </summary>
    private IEnumerator ShowPregenOutroDialogue(int slot, float pressureLevel)
    {
        if (pregenTextTasks == null || slot >= pregenTextTasks.Length) yield break;

        // テキスト待機（LLM生成完了まで、最大10秒タイムアウト）
        float textWait = 0f;
        while (!pregenTextTasks[slot].IsCompleted && textWait < 10f)
        {
            textWait += Time.deltaTime;
            yield return null;
        }

        if (skipRequested) yield break;

        string text = null;
        if (pregenTextTasks[slot].IsCompleted && !pregenTextTasks[slot].IsFaulted)
        {
            text = pregenTextTasks[slot].Result;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[GameOutro] Dialogue slot {slot} is empty or faulted, using fallback");
            text = GetOutroDialogueFallback(slot, sessionData.playerWon);
        }

        if (string.IsNullOrEmpty(text)) yield break;

        if (subtitleUI != null)
        {
            subtitleUI.Show(text, pressureLevel);
        }

        // TTS再生（5秒タイムアウト）
        if (pregenTTSTasks != null && slot < pregenTTSTasks.Length &&
            pregenTTSTasks[slot] != null && AudioManager.Instance != null)
        {
            float ttsWait = 0f;
            while (!pregenTTSTasks[slot].IsCompleted && ttsWait < 5f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (skipRequested) yield break;

            if (pregenTTSTasks[slot].IsCompleted && !pregenTTSTasks[slot].IsFaulted &&
                pregenTTSTasks[slot].Result != null)
            {
                var audioClip = pregenTTSTasks[slot].Result;
                Debug.Log($"[GameOutro] Slot {slot} - About to play TTS: clip={audioClip != null}, length={audioClip?.length ?? 0}s, AudioManager={AudioManager.Instance != null}");
                AudioManager.Instance.PlayVoice(audioClip, Vector3.zero, 1.0f);
                Debug.Log($"[GameOutro] Slot {slot} - TTS PlayVoice called");
            }
            else if (pregenTTSTasks[slot].IsFaulted)
            {
                Debug.LogWarning($"[GameOutro] Slot {slot} - TTS task faulted: {pregenTTSTasks[slot].Exception?.Message}");
            }
            else if (!pregenTTSTasks[slot].IsCompleted)
            {
                Debug.LogWarning($"[GameOutro] Slot {slot} - TTS timeout after {ttsWait:F2}s");
            }
            else
            {
                Debug.LogWarning($"[GameOutro] Slot {slot} - TTS result is null");
            }
        }
    }

    private void HideDialogue()
    {
        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }
    }

    // ========================================
    // Dialogue Templates
    // ========================================

    private string GetReflectionDialogue(bool playerWon)
    {
        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            string key = playerWon ? "outro.reflection_won" : "outro.reflection_lost";
            string[] options = loc.GetArray(key);
            Debug.Log($"[GameOutro] Reflection dialogue: key={key}, options.Length={options.Length}, playerWon={playerWon}");
            if (options.Length > 0)
            {
                string selected = options[Random.Range(0, options.Length)];
                Debug.Log($"[GameOutro] Selected reflection: {selected}");
                return selected;
            }
        }
        Debug.LogWarning("[GameOutro] LocalizationManager not found or no reflection options");
        return "...";
    }

    private string GetRevealDialogue(string personalityTitle)
    {
        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            string[] templates = loc.GetArray("outro.reveal");
            if (templates.Length > 0)
            {
                string template = templates[Random.Range(0, templates.Length)];
                return LocalizationManager.ApplyVars(template, ("personalityTitle", personalityTitle));
            }
        }
        return personalityTitle;
    }

    private string GetEvidenceTeaseDialogue()
    {
        string interpretation = null;
        if (diagnosis.evidences != null && diagnosis.evidences.Count > 0)
        {
            interpretation = diagnosis.evidences[0].interpretation;
        }
        if (string.IsNullOrEmpty(interpretation))
        {
            interpretation = diagnosis.behavioralInsight;
        }

        // Re-resolve if interpretation looks like an unresolved localization key
        if (!string.IsNullOrEmpty(interpretation) && interpretation.StartsWith("evidence.") && !interpretation.Contains(" "))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string resolved = loc.Get(interpretation);
                if (resolved != interpretation)
                {
                    interpretation = resolved;
                    Debug.Log($"[GameOutroSequence] Re-resolved evidence key: {interpretation}");
                }
                else
                {
                    Debug.LogWarning($"[GameOutroSequence] Evidence key still unresolved: {interpretation}");
                }
            }
        }

        var loc2 = LocalizationManager.Instance;
        if (loc2 != null)
        {
            string[] templates = loc2.GetArray("outro.evidence_tease");
            if (templates.Length > 0)
            {
                string template = templates[Random.Range(0, templates.Length)];
                return LocalizationManager.ApplyVars(template, ("interpretation", interpretation ?? "..."));
            }
        }
        return interpretation ?? "...";
    }

    private string GetTransitionDialogue()
    {
        var loc = LocalizationManager.Instance;
        if (loc != null)
        {
            string[] options = loc.GetArray("outro.transition");
            if (options.Length > 0)
                return options[Random.Range(0, options.Length)];
        }
        return "...";
    }

    // ========================================
    // LLM Detailed Analysis (Hybrid Mode)
    // ========================================

    /// <summary>
    /// LLM詳細分析を追加表示（ハイブリッド方式）
    /// バックグラウンドで生成中のLLM分析が完了したら追加表示
    /// </summary>
    private IEnumerator ShowLLMDetailedAnalysis()
    {
        if (llmAnalysisTask == null)
        {
            Debug.Log("[GameOutro] No LLM analysis task, skipping detailed analysis");
            yield break;
        }

        // LLM生成完了を待つ（最大5秒、タイムアウトしても続行）
        float waitTime = 0f;
        while (!llmAnalysisTask.IsCompleted && waitTime < 5f)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (skipRequested) yield break;

        if (!llmAnalysisTask.IsCompleted || llmAnalysisTask.IsFaulted)
        {
            Debug.Log("[GameOutro] LLM analysis not ready or failed, skipping");
            yield break;
        }

        string analysis = llmAnalysisTask.Result;
        if (string.IsNullOrEmpty(analysis))
        {
            Debug.Log("[GameOutro] LLM analysis returned empty, skipping");
            yield break;
        }

        Debug.Log($"[GameOutro] LLM detailed analysis ready: \"{analysis}\"");

        // TTS生成（即座に開始、完了待ちは短め、キャッシュ無効）
        if (llmManager != null && AudioManager.Instance != null)
        {
            llmAnalysisTTSTask = llmManager.GenerateTTSAsync(analysis, FPSTrump.Psychology.AIEmotion.Calm, useCache: false);
        }

        // TVHead: Focused（分析モード）
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetFocused();
        }

        yield return new WaitForSeconds(0.5f);

        // テキスト表示
        if (subtitleUI != null)
        {
            subtitleUI.Show(analysis, 0.6f);
        }

        // TTS再生（3秒タイムアウト）
        if (llmAnalysisTTSTask != null)
        {
            float ttsWait = 0f;
            while (!llmAnalysisTTSTask.IsCompleted && ttsWait < 3f)
            {
                ttsWait += Time.deltaTime;
                yield return null;
            }

            if (skipRequested) yield break;

            if (llmAnalysisTTSTask.IsCompleted && !llmAnalysisTTSTask.IsFaulted &&
                llmAnalysisTTSTask.Result != null && AudioManager.Instance != null)
            {
                var audioClip = llmAnalysisTTSTask.Result;
                Debug.Log($"[GameOutro] LLM Analysis - About to play TTS: clip={audioClip != null}, length={audioClip?.length ?? 0}s");
                AudioManager.Instance.PlayVoice(audioClip, Vector3.zero, 1.0f);
                Debug.Log($"[GameOutro] LLM Analysis - TTS PlayVoice called");
            }
            else
            {
                Debug.LogWarning($"[GameOutro] LLM Analysis TTS failed: completed={llmAnalysisTTSTask.IsCompleted}, faulted={llmAnalysisTTSTask.IsFaulted}, result={(llmAnalysisTTSTask.Result != null ? "OK" : "NULL")}, AudioManager={AudioManager.Instance != null}");
            }
        }

        yield return new WaitForSeconds(3.5f);

        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }

        yield return new WaitForSeconds(0.3f);
    }
}
