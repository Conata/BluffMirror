using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using FPSTrump.AI.LLM;

/// <summary>
/// AI迷いアニメーションのオーケストレーター
/// カメラ、UI、LLM、Live2Dの4システムを統合制御
/// 人間らしい行き来パターン + 可変タイミング + 発話スタイルバリエーション
/// </summary>
public class AIHesitationController : MonoBehaviour
{
    /// <summary>
    /// 発話スタイル: 推測・ブラフ・煽り・弱気
    /// </summary>
    public enum HesitationStyle
    {
        Deduction,   // 推測: プレイヤーの行動から推理
        Bluff,       // ブラフ: わかったフリをする
        Provoke,     // 煽り: プレイヤーを挑発
        Vulnerable   // 弱気: 不安・迷いを見せる
    }

    public enum GamePhase { Early, Mid, Late }
    public enum GameAdvantage { Winning, Even, Losing }

    /// <summary>
    /// ゲーム状況コンテキスト（台詞の状況反映に使用）
    /// </summary>
    public struct HesitationContext
    {
        public int aiCardCount;
        public int playerCardCount;
        public bool aiHoldsJoker;
        public int turnNumber;

        public GamePhase Phase
        {
            get
            {
                if (turnNumber <= 2) return GamePhase.Early;
                if (aiCardCount + playerCardCount <= 5) return GamePhase.Late;
                return GamePhase.Mid;
            }
        }

        public GameAdvantage Advantage
        {
            get
            {
                if (aiCardCount < playerCardCount) return GameAdvantage.Winning;
                if (aiCardCount > playerCardCount) return GameAdvantage.Losing;
                return GameAdvantage.Even;
            }
        }
    }

    [Header("System Dependencies")]
    [SerializeField] private FloatingTextSystem floatingTextSystem;
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private AIAttentionMarker attentionMarker;
    [SerializeField] private CardEffectsManager effectsManager;
    [SerializeField] private TVHeadAnimator tvHeadAnimator;
    [SerializeField] private FPSTrump.AI.LLM.LLMManager llmManager;
    [SerializeField] private FPSTrump.Psychology.PlayerBehaviorAnalyzer behaviorAnalyzer;

    [Header("Timing Configuration")]
    [SerializeField] private float minDwellTime = 0.3f;
    [SerializeField] private float maxDwellTime = 1.2f;

    [Header("Hesitation Behavior")]
    [SerializeField] private int minCardsToConsider = 2;
    [SerializeField] private int maxCardsToConsider = 4;
    [SerializeField] private int baseVisitSteps = 4;
    [SerializeField] private int maxExtraSteps = 4;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // 現在のゲーム状況コンテキスト（シーケンス実行中のみ有効）
    private HesitationContext currentContext;

    public enum TextDisplayMode
    {
        FloatingText,
        Subtitle,
        Both
    }

    [Header("Text Display Settings")]
    [SerializeField] private TextDisplayMode textDisplayMode = TextDisplayMode.Subtitle;

    private void Awake()
    {
        if (floatingTextSystem == null)
            floatingTextSystem = FloatingTextSystem.Instance;
        if (subtitleUI == null)
            subtitleUI = SubtitleUI.Instance;
        if (attentionMarker == null)
            attentionMarker = AIAttentionMarker.Instance;
        if (effectsManager == null)
            effectsManager = CardEffectsManager.Instance;
        if (tvHeadAnimator == null)
            tvHeadAnimator = FindFirstObjectByType<TVHeadAnimator>();
        if (llmManager == null)
            llmManager = FPSTrump.AI.LLM.LLMManager.Instance;
        if (behaviorAnalyzer == null)
            behaviorAnalyzer = FindFirstObjectByType<FPSTrump.Psychology.PlayerBehaviorAnalyzer>();
    }

    /// <summary>
    /// 迷いアニメーションシーケンス（人間らしい行き来パターン）
    /// </summary>
    public IEnumerator PlayHesitationSequence(
        List<CardObject> playerCards,
        float pressureLevel = 0f,
        HesitationContext gameContext = default)
    {
        currentContext = gameContext;
        if (playerCards == null || playerCards.Count == 0)
        {
            LogDebug("No player cards available - skipping hesitation");
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
            yield break;
        }

        if (playerCards.Count == 1)
        {
            yield return HandleSingleCardCase(playerCards[0], pressureLevel);
            yield break;
        }

        LogDebug($"Starting hesitation sequence for {playerCards.Count} cards (Pressure: {pressureLevel}, Phase: {currentContext.Phase}, Advantage: {currentContext.Advantage}, Joker: {currentContext.aiHoldsJoker})");

        // === Phase 1: 準備 ===
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNervous();
        }

        // 検討するカード数を決定
        int cardsToConsider = Mathf.Min(
            Random.Range(minCardsToConsider, maxCardsToConsider + 1),
            playerCards.Count
        );
        List<CardObject> selectedCards = GetRandomCards(playerCards, cardsToConsider);

        // PersonalityProfileを取得
        PersonalityProfile playerProfile = llmManager?.CurrentPlayerProfile;

        // 訪問ステップ数を事前計算（スタイル判定の一貫性のため）
        int extraSteps = Mathf.RoundToInt(Mathf.Lerp(0, maxExtraSteps, pressureLevel / 3f));
        int estimatedTotalSteps = Mathf.Max(baseVisitSteps + extraSteps, selectedCards.Count);

        // 初期の発話スタイルを決定（estimatedTotalStepsベース）
        HesitationStyle initialStyle = DetermineHesitationStyle(pressureLevel, 0, estimatedTotalSteps);

        // 初期セリフ表示 + TTS（fire-and-forget: Phase 2をブロックしない）
        Vector3 textPosition = GetTextPosition(selectedCards[0]);
        var dialogueTask = GetStyledDialogueAsync(0, estimatedTotalSteps, pressureLevel, initialStyle, playerProfile);
        yield return new WaitUntil(() => dialogueTask.IsCompleted);
        string initialDialogue = dialogueTask.IsCompletedSuccessfully ? dialogueTask.Result : "...";
        ShowText(textPosition, initialDialogue, pressureLevel);
        LogDebug($"[{initialStyle}/{currentContext.Phase}/{currentContext.Advantage}] Dialogue: \"{initialDialogue}\"");

        // TTS生成＆再生（バックグラウンド: 完了を待たずPhase 2へ進む）
        if (llmManager != null && AudioManager.Instance != null && !string.IsNullOrEmpty(initialDialogue))
        {
            _ = PlayTTSInBackground(initialDialogue, FPSTrump.Psychology.AIEmotion.Calm);
        }

        yield return new WaitForSeconds(0.3f);

        // === Phase 2: 行き来パターンでカードフォーカス ===
        yield return StartCoroutine(FocusOnCardsWithHumanPattern(selectedCards, pressureLevel, playerProfile));

        // === Phase 3: クリーンアップ ===
        LogDebug("Cleaning up hesitation sequence");

        if (attentionMarker != null)
        {
            attentionMarker.Hide();
        }

        AudioManager.Instance?.StopVoice();
        HideText();

        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        yield return new WaitForSeconds(0.2f);
        LogDebug("Hesitation sequence completed");
    }

    /// <summary>
    /// CoT駆動の迷いアニメーションシーケンス
    /// LLM/フォールバックが生成したCoTステップに従い、カードを順に検討
    /// 各ステップ: マーカー移動 → テキスト表示 → TTS再生 → 待機
    /// </summary>
    public IEnumerator PlayCoTHesitationSequence(
        List<CardObject> playerCards,
        List<CoTStep> cotSteps,
        UnityEngine.AudioClip[] pregenTTSClips,
        float pressureLevel,
        HesitationContext gameContext = default)
    {
        currentContext = gameContext;

        if (playerCards == null || playerCards.Count == 0 || cotSteps == null || cotSteps.Count == 0)
        {
            LogDebug("No player cards or CoT steps - skipping CoT hesitation");
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
            yield break;
        }

        if (playerCards.Count == 1)
        {
            yield return HandleSingleCardCase(playerCards[0], pressureLevel);
            yield break;
        }

        LogDebug($"Starting CoT hesitation: {cotSteps.Count} steps for {playerCards.Count} cards");

        // Phase 1: 準備
        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNervous();
        }

        ParticleSystem currentAura = null;
        CardObject currentCard = null;

        for (int step = 0; step < cotSteps.Count; step++)
        {
            CoTStep cot = cotSteps[step];
            int cardIdx = Mathf.Clamp(cot.cardIndex, 0, playerCards.Count - 1);
            CardObject card = playerCards[cardIdx];
            if (card == null) continue;

            bool isLast = (step == cotSteps.Count - 1);

            // === 視覚ステート決定 ===
            AIAttentionMarker.MarkerVisualState visualState;
            if (isLast)
                visualState = AIAttentionMarker.MarkerVisualState.Locked;
            else if (step >= cotSteps.Count / 2)
                visualState = AIAttentionMarker.MarkerVisualState.Focusing;
            else
                visualState = AIAttentionMarker.MarkerVisualState.Scanning;

            // === マーカー移動 + 視覚ステート設定 ===
            if (attentionMarker != null)
            {
                attentionMarker.Show(card.transform);
                attentionMarker.SetVisualState(visualState);
            }

            // === エフェクト切り替え ===
            if (effectsManager != null && card != currentCard)
            {
                if (currentAura != null && currentCard != null)
                {
                    effectsManager.StopAIConsideringAura(currentAura, currentCard);
                }
                currentAura = effectsManager.PlayAIConsideringAura(card);
                currentCard = card;
            }

            // === CoT推理テキスト表示 ===
            if (!string.IsNullOrEmpty(cot.thought))
            {
                Vector3 textPos = GetTextPosition(card);
                if (step == 0)
                    ShowText(textPos, cot.thought, pressureLevel);
                else
                    UpdateText(cot.thought, pressureLevel);
            }

            LogDebug($"CoT Step {step}: card={cardIdx}, state={visualState}, thought=\"{cot.thought}\"");

            // === TTS再生 ===
            UnityEngine.AudioClip ttsClip = (pregenTTSClips != null && step < pregenTTSClips.Length)
                ? pregenTTSClips[step]
                : null;

            if (ttsClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopVoice();
                AudioManager.Instance.PlayVoice(ttsClip, Vector3.zero, 1.0f);
            }

            // === 待機: TTS長さ or 最低滞在時間 ===
            float baseDwell = CalculateDwellTime(step, cotSteps.Count, pressureLevel, false);
            if (ttsClip != null)
            {
                float waitTime = Mathf.Max(baseDwell, ttsClip.length + 0.3f);
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                yield return new WaitForSeconds(baseDwell);
            }

            // ステップ間: 前のTTS停止
            if (!isLast)
            {
                AudioManager.Instance?.StopVoice();
            }
        }

        // Phase 3: クリーンアップ
        if (currentAura != null && currentCard != null && effectsManager != null)
        {
            effectsManager.StopAIConsideringAura(currentAura, currentCard);
        }

        if (attentionMarker != null)
        {
            attentionMarker.Hide();
        }

        AudioManager.Instance?.StopVoice();
        HideText();

        if (tvHeadAnimator != null)
        {
            tvHeadAnimator.SetNeutral();
        }

        yield return new WaitForSeconds(0.2f);
        LogDebug("CoT hesitation sequence completed");
    }

    /// <summary>
    /// 人間らしい行き来パターンでカードをフォーカス
    /// A→B→A→C→B→D のように同じカードに複数回戻る
    /// </summary>
    private IEnumerator FocusOnCardsWithHumanPattern(
        List<CardObject> cards, float pressureLevel, PersonalityProfile playerProfile)
    {
        // 訪問パターンを生成
        List<int> visitPattern = GenerateVisitPattern(cards.Count, pressureLevel);
        LogDebug($"Visit pattern: [{string.Join(", ", visitPattern)}] ({visitPattern.Count} steps)");

        ParticleSystem currentAura = null;
        CardObject currentCard = null;
        int prevCardIndex = -1;

        for (int step = 0; step < visitPattern.Count; step++)
        {
            int cardIndex = visitPattern[step];
            CardObject card = cards[cardIndex];
            if (card == null) continue;

            bool isLastStep = (step == visitPattern.Count - 1);
            bool isRevisit = (cardIndex == prevCardIndex);

            // === マーカー移動（スムーズ） ===
            if (attentionMarker != null)
            {
                attentionMarker.Show(card.transform);
            }

            // === エフェクト切り替え ===
            if (effectsManager != null && cardIndex != prevCardIndex)
            {
                if (currentAura != null && currentCard != null)
                {
                    effectsManager.StopAIConsideringAura(currentAura, currentCard);
                }
                currentAura = effectsManager.PlayAIConsideringAura(card);
                currentCard = card;
            }

            // === 発話更新（最初のステップ以降） ===
            if (step > 0 && !isRevisit)
            {
                HesitationStyle style = DetermineHesitationStyle(pressureLevel, step, visitPattern.Count);
                var dialogueTask = GetStyledDialogueAsync(step, visitPattern.Count, pressureLevel, style, playerProfile);
                yield return new WaitUntil(() => dialogueTask.IsCompleted);

                string dialogue = dialogueTask.IsCompletedSuccessfully ? dialogueTask.Result : "...";
                UpdateText(dialogue, pressureLevel);
                LogDebug($"[{style}/{currentContext.Phase}/{currentContext.Advantage}] Step {step}: \"{dialogue}\"");

                // 最後のステップのみTTS再生（バックグラウンド）
                if (isLastStep && llmManager != null && AudioManager.Instance != null
                    && !string.IsNullOrEmpty(dialogue))
                {
                    AudioManager.Instance.StopVoice();
                    _ = PlayTTSInBackground(dialogue, FPSTrump.Psychology.AIEmotion.Anticipating);
                }
            }

            // === 可変滞在時間 ===
            float dwellTime = CalculateDwellTime(step, visitPattern.Count, pressureLevel, isRevisit);
            yield return new WaitForSeconds(dwellTime);

            prevCardIndex = cardIndex;
        }

        // 最後のオーラを停止
        if (currentAura != null && currentCard != null && effectsManager != null)
        {
            effectsManager.StopAIConsideringAura(currentAura, currentCard);
        }
    }

    /// <summary>
    /// 行き来パターンを生成
    /// 例: cards=3, pressure=1.5 → [0, 1, 0, 2, 1, 2]
    /// </summary>
    private List<int> GenerateVisitPattern(int cardCount, float pressureLevel)
    {
        // 圧力に応じて追加ステップ数を決定
        int extraSteps = Mathf.RoundToInt(Mathf.Lerp(0, maxExtraSteps, pressureLevel / 3f));
        int totalSteps = baseVisitSteps + extraSteps;
        totalSteps = Mathf.Max(totalSteps, cardCount); // 最低でも全カードを1回は訪問

        List<int> pattern = new List<int>();

        // まず全カードを順番に1回ずつ（シャッフル）
        List<int> initialOrder = new List<int>();
        for (int i = 0; i < cardCount; i++) initialOrder.Add(i);
        ShuffleList(initialOrder);

        foreach (int idx in initialOrder)
        {
            pattern.Add(idx);
        }

        // 追加ステップ: 既に訪問したカードに戻る（行き来）
        int remaining = totalSteps - pattern.Count;
        for (int i = 0; i < remaining; i++)
        {
            // 前のカードと違うカードを選ぶ（連続同じを避ける）
            int next;
            int attempts = 0;
            do
            {
                next = Random.Range(0, cardCount);
                attempts++;
            } while (next == pattern[pattern.Count - 1] && attempts < 10);

            pattern.Add(next);
        }

        return pattern;
    }

    /// <summary>
    /// 可変滞在時間を計算
    /// </summary>
    private float CalculateDwellTime(int step, int totalSteps, float pressureLevel, bool isRevisit)
    {
        // 基本: ランダム範囲
        float baseTime = Random.Range(minDwellTime, maxDwellTime);

        // 再訪問は短め（さっと通過）
        if (isRevisit) baseTime *= 0.6f;

        // 最後のステップは少し長め（決断の瞬間）
        if (step == totalSteps - 1) baseTime *= 1.3f;

        // 圧力が高いほど全体的に遅い
        float pressureMultiplier = 1f + (pressureLevel / 3f) * 0.3f;
        baseTime *= pressureMultiplier;

        return Mathf.Clamp(baseTime, minDwellTime, maxDwellTime * 1.5f);
    }

    /// <summary>
    /// 発話スタイルを状況に応じて決定（ゲーム状況バイアス付き）
    /// </summary>
    private HesitationStyle DetermineHesitationStyle(float pressureLevel, int step, int totalSteps)
    {
        var phase = currentContext.Phase;
        var advantage = currentContext.Advantage;

        // 最初のステップ
        if (step == 0)
        {
            // 序盤: 様子見（推測/弱気）
            if (phase == GamePhase.Early)
                return Random.value < 0.6f ? HesitationStyle.Deduction : HesitationStyle.Vulnerable;
            // 終盤+劣勢: 焦り
            if (phase == GamePhase.Late && advantage == GameAdvantage.Losing)
                return Random.value < 0.6f ? HesitationStyle.Vulnerable : HesitationStyle.Deduction;
            return Random.value < 0.5f ? HesitationStyle.Vulnerable : HesitationStyle.Deduction;
        }

        // 最後のステップ
        if (step == totalSteps - 1)
        {
            // 終盤+優勢: 余裕の煽り
            if (phase == GamePhase.Late && advantage == GameAdvantage.Winning)
                return Random.value < 0.5f ? HesitationStyle.Provoke : HesitationStyle.Bluff;
            return Random.value < 0.4f ? HesitationStyle.Bluff : HesitationStyle.Deduction;
        }

        // 中間ステップ: ゲーム状況バイアス
        // ジョーカー所持 → ブラフ増加（隠したい）
        if (currentContext.aiHoldsJoker && Random.value < 0.35f)
            return HesitationStyle.Bluff;

        // 終盤+優勢 → 煽り増加
        if (phase == GamePhase.Late && advantage == GameAdvantage.Winning && Random.value < 0.4f)
            return HesitationStyle.Provoke;

        // 終盤+劣勢 → 弱気/推測増加
        if (phase == GamePhase.Late && advantage == GameAdvantage.Losing && Random.value < 0.4f)
            return Random.value < 0.5f ? HesitationStyle.Vulnerable : HesitationStyle.Deduction;

        // 通常の圧力ベース
        if (pressureLevel >= 1.5f && Random.value < 0.4f)
            return HesitationStyle.Provoke;

        if (Random.value < 0.25f)
            return HesitationStyle.Bluff;

        if (pressureLevel < 0.5f && Random.value < 0.3f)
            return HesitationStyle.Vulnerable;

        return HesitationStyle.Deduction;
    }

    /// <summary>
    /// スタイル×性格対応のセリフ取得（LLM with fallback）
    /// </summary>
    private async Task<string> GetStyledDialogueAsync(
        int stepIndex, int totalSteps, float pressureLevel,
        HesitationStyle style, PersonalityProfile playerProfile)
    {
        if (llmManager != null)
        {
            try
            {
                string dialogue = await llmManager.GenerateHesitationDialogue(
                    cardIndex: stepIndex,
                    totalCards: totalSteps,
                    pressureLevel: pressureLevel,
                    behaviorPattern: behaviorAnalyzer?.CurrentBehavior,
                    style: style,
                    playerProfile: playerProfile,
                    gameContext: currentContext
                );

                if (!string.IsNullOrEmpty(dialogue))
                {
                    // Stage 15: 会話履歴に記録
                    string context = $"Hesitation {style} (step {stepIndex + 1}/{totalSteps})";
                    FPSTrump.AI.LLM.LLMContextWindow.AddDialogueToHistory(currentContext.turnNumber, dialogue, context);

                    return dialogue;
                }
            }
            catch (System.Exception ex)
            {
                LogDebug($"LLM dialogue generation failed: {ex.Message}");
            }
        }

        return GetStyledDialogueFallback(stepIndex, totalSteps, pressureLevel, style, playerProfile);
    }

    /// <summary>
    /// スタイル×性格対応フォールバックセリフ（ゲーム状況反映）
    /// </summary>
    private string GetStyledDialogueFallback(
        int stepIndex, int totalSteps, float pressureLevel,
        HesitationStyle style, PersonalityProfile profile)
    {
        // 最後のステップ: 決断
        if (stepIndex == totalSteps - 1)
        {
            return GetFinalStepDialogue(style, profile);
        }

        // 性格タイプ判定
        string traitName = GetDominantTraitName(profile);

        // 状況台詞を50%の確率で優先使用
        string situational = GetSituationalDialogue(style);
        if (situational != null && Random.value < 0.5f)
        {
            return situational;
        }

        switch (style)
        {
            case HesitationStyle.Deduction:
                return GetDeductionDialogue(traitName, stepIndex);
            case HesitationStyle.Bluff:
                return GetBluffDialogue(traitName);
            case HesitationStyle.Provoke:
                return GetProvokeDialogue(traitName);
            case HesitationStyle.Vulnerable:
                return GetVulnerableDialogue(traitName);
            default:
                var loc = LocalizationManager.Instance;
                return loc != null ? loc.Get("hesitation_dialogue.default_fallback") : "Hmm...";
        }
    }

    /// <summary>
    /// ゲーム状況に応じた台詞を返す（マッチしなければnull）
    /// </summary>
    private string GetSituationalDialogue(HesitationStyle style)
    {
        var phase = currentContext.Phase;
        var advantage = currentContext.Advantage;
        var loc = LocalizationManager.Instance;
        if (loc == null) return null;

        switch (style)
        {
            case HesitationStyle.Deduction:
                if (phase == GamePhase.Late)
                    return loc.Get("hesitation_dialogue.situational.late_deduction");
                if (advantage == GameAdvantage.Winning)
                    return loc.Get("hesitation_dialogue.situational.winning_deduction");
                if (advantage == GameAdvantage.Losing && currentContext.aiHoldsJoker)
                    return loc.Get("hesitation_dialogue.situational.losing_joker_deduction");
                break;

            case HesitationStyle.Bluff:
                if (currentContext.aiHoldsJoker)
                    return loc.Get("hesitation_dialogue.situational.bluff_has_joker");
                if (phase == GamePhase.Late)
                    return loc.Get("hesitation_dialogue.situational.late_bluff");
                break;

            case HesitationStyle.Provoke:
                if (phase == GamePhase.Late && advantage == GameAdvantage.Winning)
                    return loc.Get("hesitation_dialogue.situational.late_winning_provoke");
                if (advantage == GameAdvantage.Losing)
                    return loc.Get("hesitation_dialogue.situational.losing_provoke");
                break;

            case HesitationStyle.Vulnerable:
                if (phase == GamePhase.Late)
                    return loc.Get("hesitation_dialogue.situational.late_vulnerable");
                if (advantage == GameAdvantage.Losing)
                    return loc.Get("hesitation_dialogue.situational.losing_vulnerable");
                break;
        }

        return null;
    }

    private string GetDominantTraitName(PersonalityProfile profile)
    {
        if (profile == null) return null;
        return profile.GetDominantTrait().name;
    }

    private string GetDeductionDialogue(string traitName, int stepIndex)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null)
        {
            // Fallback to English if LocalizationManager is not available
            if (traitName != null)
            {
                string[] fallbackWithTrait = {
                    $"{traitName} types...play defensive",
                    $"{traitName} huh...I see the pattern",
                    $"{traitName}...suspicious area",
                    $"Your {traitName} nature gives it away"
                };
                return fallbackWithTrait[Random.Range(0, fallbackWithTrait.Length)];
            }
            string[] fallbackGeneric = { "Something's off...", "Your eyes tell the story...", "I sense it..." };
            return fallbackGeneric[Random.Range(0, fallbackGeneric.Length)];
        }

        if (traitName != null)
        {
            string[] withTrait = loc.GetArray("hesitation_dialogue.deduction_with_trait");
            if (withTrait.Length > 0)
            {
                string template = withTrait[Random.Range(0, withTrait.Length)];
                return LocalizationManager.ApplyVars(template, ("traitName", traitName));
            }
        }

        string[] generic = loc.GetArray("hesitation_dialogue.deduction_generic");
        if (generic.Length > 0)
        {
            return generic[Random.Range(0, generic.Length)];
        }

        return "Something's off...";
    }

    private string GetBluffDialogue(string traitName)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null)
        {
            // Fallback to English
            if (traitName != null)
            {
                string[] fallbackWithTrait = {
                    $"I've read your {traitName} tells",
                    $"{traitName} types are predictable",
                    $"{traitName} huh...this card then"
                };
                return fallbackWithTrait[Random.Range(0, fallbackWithTrait.Length)];
            }
            string[] fallbackGeneric = { "I already know...", "The answer's clear", "I'm certain...right here" };
            return fallbackGeneric[Random.Range(0, fallbackGeneric.Length)];
        }

        if (traitName != null)
        {
            string[] withTrait = loc.GetArray("hesitation_dialogue.bluff_with_trait");
            if (withTrait.Length > 0)
            {
                string template = withTrait[Random.Range(0, withTrait.Length)];
                return LocalizationManager.ApplyVars(template, ("traitName", traitName));
            }
        }

        string[] generic = loc.GetArray("hesitation_dialogue.bluff_generic");
        if (generic.Length > 0)
        {
            return generic[Random.Range(0, generic.Length)];
        }

        return "I already know...";
    }

    private string GetProvokeDialogue(string traitName)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null)
        {
            // Fallback to English
            if (traitName != null)
            {
                string[] fallbackWithTrait = {
                    $"{traitName} dulls when you panic, right?",
                    $"Your {traitName} ends here",
                    $"Pretending to be {traitName} won't work"
                };
                return fallbackWithTrait[Random.Range(0, fallbackWithTrait.Length)];
            }
            string[] fallbackGeneric = { "Doesn't matter which...", "Your luck ran out", "You're shaking..." };
            return fallbackGeneric[Random.Range(0, fallbackGeneric.Length)];
        }

        if (traitName != null)
        {
            string[] withTrait = loc.GetArray("hesitation_dialogue.provoke_with_trait");
            if (withTrait.Length > 0)
            {
                string template = withTrait[Random.Range(0, withTrait.Length)];
                return LocalizationManager.ApplyVars(template, ("traitName", traitName));
            }
        }

        string[] generic = loc.GetArray("hesitation_dialogue.provoke_generic");
        if (generic.Length > 0)
        {
            return generic[Random.Range(0, generic.Length)];
        }

        return "Doesn't matter which...";
    }

    private string GetVulnerableDialogue(string traitName)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null)
        {
            // Fallback to English
            if (traitName != null)
            {
                string[] fallbackWithTrait = {
                    $"Your {traitName} nature is troublesome...",
                    $"{traitName} type...tough opponent",
                    $"This {traitName} trait...unexpected..."
                };
                return fallbackWithTrait[Random.Range(0, fallbackWithTrait.Length)];
            }
            string[] fallbackGeneric = { "I don't know...", "Going with my gut...", "Can't tell...all look suspicious" };
            return fallbackGeneric[Random.Range(0, fallbackGeneric.Length)];
        }

        if (traitName != null)
        {
            string[] withTrait = loc.GetArray("hesitation_dialogue.vulnerable_with_trait");
            if (withTrait.Length > 0)
            {
                string template = withTrait[Random.Range(0, withTrait.Length)];
                return LocalizationManager.ApplyVars(template, ("traitName", traitName));
            }
        }

        string[] generic = loc.GetArray("hesitation_dialogue.vulnerable_generic");
        if (generic.Length > 0)
        {
            return generic[Random.Range(0, generic.Length)];
        }

        return "I don't know...";
    }

    private string GetFinalStepDialogue(HesitationStyle style, PersonalityProfile profile)
    {
        var phase = currentContext.Phase;
        var advantage = currentContext.Advantage;
        string traitName = GetDominantTraitName(profile);
        var loc = LocalizationManager.Instance;

        // Fallback to English if LocalizationManager is not available
        if (loc == null)
        {
            if (phase == GamePhase.Late)
            {
                if (advantage == GameAdvantage.Winning) return "Finishing blow";
                return "This card decides it...";
            }

            switch (style)
            {
                case HesitationStyle.Deduction:
                    if (traitName != null) return $"{traitName} one...right here";
                    return "I have my reasons...here";
                case HesitationStyle.Bluff:
                    return "Knew it all along";
                case HesitationStyle.Provoke:
                    return "Brace yourself...this card";
                case HesitationStyle.Vulnerable:
                    return "...This'll do. Probably";
                default:
                    return "This one";
            }
        }

        // 終盤専用の決断台詞
        if (phase == GamePhase.Late)
        {
            if (advantage == GameAdvantage.Winning)
                return loc.Get("hesitation_dialogue.final_step.late_winning");
            return loc.Get("hesitation_dialogue.final_step.late_default");
        }

        switch (style)
        {
            case HesitationStyle.Deduction:
                if (traitName != null)
                {
                    string template = loc.Get("hesitation_dialogue.final_step.deduction_with_trait");
                    return LocalizationManager.ApplyVars(template, ("traitName", traitName));
                }
                return loc.Get("hesitation_dialogue.final_step.deduction_generic");
            case HesitationStyle.Bluff:
                return loc.Get("hesitation_dialogue.final_step.bluff");
            case HesitationStyle.Provoke:
                return loc.Get("hesitation_dialogue.final_step.provoke");
            case HesitationStyle.Vulnerable:
                return loc.Get("hesitation_dialogue.final_step.vulnerable");
            default:
                return loc.Get("hesitation_dialogue.final_step.default");
        }
    }

    /// <summary>
    /// TTS生成＆再生（バックグラウンド: 呼び出し元をブロックしない）
    /// </summary>
    private async Task PlayTTSInBackground(string text, FPSTrump.Psychology.AIEmotion emotion)
    {
        try
        {
            var clip = await llmManager.GenerateTTSAsync(text, emotion);
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVoice(clip, Vector3.zero, 1.0f);
            }
        }
        catch (System.Exception ex)
        {
            LogDebug($"TTS background failed: {ex.Message}");
        }
    }

    // === ヘルパーメソッド ===

    private IEnumerator HandleSingleCardCase(CardObject card, float pressureLevel)
    {
        LogDebug("Single card case - showing immediate decision");

        string dialogue = "選択肢がないな...";

        if (card != null)
        {
            Vector3 textPos = card.transform.position + Vector3.up * 0.4f;
            ShowText(textPos, dialogue, pressureLevel);
        }

        if (llmManager != null && AudioManager.Instance != null)
        {
            _ = PlayTTSInBackground(dialogue, FPSTrump.Psychology.AIEmotion.Calm);
        }

        yield return new WaitForSeconds(0.8f);

        AudioManager.Instance?.StopVoice();
        HideText();
    }

    private List<CardObject> GetRandomCards(List<CardObject> allCards, int count)
    {
        List<CardObject> shuffled = new List<CardObject>(allCards);
        ShuffleList(shuffled);

        List<CardObject> selected = new List<CardObject>();
        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            selected.Add(shuffled[i]);
        }
        return selected;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private Vector3 GetTextPosition(CardObject card)
    {
        if (card == null) return Vector3.zero;
        return card.transform.position + Vector3.up * 0.4f;
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AIHesitationController] {message}");
        }
    }

    private void ShowText(Vector3 worldPosition, string text, float pressureLevel)
    {
        switch (textDisplayMode)
        {
            case TextDisplayMode.FloatingText:
                floatingTextSystem?.ShowPersistentText(worldPosition, text, pressureLevel);
                break;
            case TextDisplayMode.Subtitle:
                subtitleUI?.Show(text, pressureLevel);
                break;
            case TextDisplayMode.Both:
                floatingTextSystem?.ShowPersistentText(worldPosition, text, pressureLevel);
                subtitleUI?.Show(text, pressureLevel);
                break;
        }
    }

    private void UpdateText(string text, float pressureLevel)
    {
        switch (textDisplayMode)
        {
            case TextDisplayMode.FloatingText:
                floatingTextSystem?.UpdatePersistentText(text, pressureLevel);
                break;
            case TextDisplayMode.Subtitle:
                subtitleUI?.UpdateText(text, pressureLevel);
                break;
            case TextDisplayMode.Both:
                floatingTextSystem?.UpdatePersistentText(text, pressureLevel);
                subtitleUI?.UpdateText(text, pressureLevel);
                break;
        }
    }

    private void HideText()
    {
        floatingTextSystem?.HidePersistentText();
        subtitleUI?.Hide();
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Play Hesitation Sequence (Low Pressure)")]
    private void TestHesitationLowPressure()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[AIHesitationController] Test must be run in Play Mode"); return; }
        StartTestWithPressure(0.5f);
    }

    [ContextMenu("Test: Play Hesitation Sequence (Medium Pressure)")]
    private void TestHesitationMediumPressure()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[AIHesitationController] Test must be run in Play Mode"); return; }
        StartTestWithPressure(1.5f);
    }

    [ContextMenu("Test: Play Hesitation Sequence (High Pressure)")]
    private void TestHesitationHighPressure()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[AIHesitationController] Test must be run in Play Mode"); return; }
        StartTestWithPressure(2.8f);
    }

    private void StartTestWithPressure(float pressure)
    {
        var playerHand = FindFirstObjectByType<PlayerHandController>();
        if (playerHand != null)
        {
            StartCoroutine(PlayHesitationSequence(playerHand.GetCards(), pressure));
        }
        else
        {
            var cards = FindObjectsByType<CardObject>(FindObjectsSortMode.None);
            if (cards.Length > 0)
            {
                List<CardObject> cardList = new List<CardObject>(cards);
                StartCoroutine(PlayHesitationSequence(cardList, pressure));
            }
            else
            {
                Debug.LogWarning("[AIHesitationController] No cards found in scene");
            }
        }
    }
#endif
}
