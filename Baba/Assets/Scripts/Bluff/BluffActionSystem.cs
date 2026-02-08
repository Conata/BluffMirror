using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FPSTrump.Psychology;

/// <summary>
/// ブラフアクションの中央コーディネーター
/// プレイヤーAPI + AI自律ブラフロジック + アクション履歴管理
/// </summary>
public class BluffActionSystem : MonoBehaviour
{
    public static BluffActionSystem Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PlayerHandController playerHand;
    [SerializeField] private AIHandController aiHand;

    [Header("Player Cooldown")]
    [SerializeField] private float playerActionCooldown = 1.0f;

    [Header("AI Bluff Settings")]
    [SerializeField] private float aiActionCooldown = 4.0f;
    [SerializeField] private float aiMinDelay = 2.0f;
    [SerializeField] private float aiMaxDelay = 8.0f;
    [SerializeField] [Range(0f, 1f)] private float aiBaseBluffChance = 0.8f; // 0.6 → 0.8 (頻度UP)
    [SerializeField] [Range(0f, 1f)] private float aiMaxBluffChance = 0.95f; // 0.9 → 0.95

    // State
    private float lastPlayerActionTime = -10f;
    private float lastAIActionTime = -10f;
    private bool isPlayerSelectingCard = false;
    private BluffActionType pendingPlayerAction;
    private List<BluffActionRecord> actionHistory = new List<BluffActionRecord>();
    private Coroutine aiBluffCoroutine;
    private bool playerBluffedRecently = false;
    private BluffActionType lastAIAction = BluffActionType.Shuffle; // 連続同一アクション防止用

    // Events
    public event System.Action<BluffActionRecord> OnBluffActionPerformed;

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
    }

    private void Start()
    {
        if (playerHand == null)
            playerHand = FindFirstObjectByType<PlayerHandController>();
        if (aiHand == null)
            aiHand = FindFirstObjectByType<AIHandController>();
    }

    // =========================================
    // Player API
    // =========================================

    public bool CanPlayerAct()
    {
        if (playerHand == null || playerHand.IsBluffAnimating) return false;
        if (isPlayerSelectingCard) return false;
        if (Time.time - lastPlayerActionTime < playerActionCooldown) return false;

        // ゲームがアクティブな状態でのみ許可
        if (GameManager.Instance == null) return false;
        var state = GameManager.Instance.CurrentState;
        return state == GameState.PLAYER_TURN_PICK
            || state == GameState.AI_TURN_APPROACH
            || state == GameState.AI_TURN_HESITATE
            || state == GameState.AI_TURN_COMMIT;
    }

    public bool IsPlayerSelectingCard => isPlayerSelectingCard;

    public void RequestPlayerShuffle()
    {
        if (!CanPlayerAct()) return;

        playerHand.ShuffleCards();
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Shuffle, BluffActionSource.Player, -1);
    }

    // Stage 16: Direct card action methods (no UI button required)
    public void ShuffleCards()
    {
        RequestPlayerShuffle();
    }

    public void PushCard(int cardIndex)
    {
        if (!CanPlayerAct()) return;
        if (cardIndex < 0 || cardIndex >= playerHand.GetCardCount()) return;

        playerHand.PushCard(cardIndex);
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Push, BluffActionSource.Player, cardIndex);
    }

    public void PullCard(int cardIndex)
    {
        if (!CanPlayerAct()) return;
        if (cardIndex < 0 || cardIndex >= playerHand.GetCardCount()) return;

        playerHand.PullCard(cardIndex);
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Pull, BluffActionSource.Player, cardIndex);
    }

    public void WiggleCard(int cardIndex)
    {
        if (!CanPlayerAct()) return;
        if (cardIndex < 0 || cardIndex >= playerHand.GetCardCount()) return;

        playerHand.WiggleCard(cardIndex);
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Wiggle, BluffActionSource.Player, cardIndex);
    }

    public void RequestPlayerSpread()
    {
        if (!CanPlayerAct()) return;

        playerHand.SpreadFan();
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Spread, BluffActionSource.Player, -1);
    }

    public void RequestPlayerClose()
    {
        if (!CanPlayerAct()) return;

        playerHand.CloseFan();
        lastPlayerActionTime = Time.time;
        RecordAction(BluffActionType.Close, BluffActionSource.Player, -1);
    }

    /// <summary>
    /// カード選択モードに入る (Push/Pull/Wiggle用)
    /// </summary>
    public void RequestPlayerTargetedAction(BluffActionType actionType)
    {
        if (!CanPlayerAct()) return;
        if (actionType != BluffActionType.Push && actionType != BluffActionType.Pull && actionType != BluffActionType.Wiggle)
            return;

        isPlayerSelectingCard = true;
        pendingPlayerAction = actionType;

        // プレイヤー手札をブラフターゲット可能にする
        var cards = playerHand.GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetBluffTargetable(true);
            cards[i].OnCardClickedForBluff.AddListener(OnPlayerCardSelectedForBluff);
        }

        if (BluffActionUI.Instance != null)
            BluffActionUI.Instance.EnterCardSelectionMode(GetActionDisplayName(actionType));
    }

    public void CancelCardSelection()
    {
        if (!isPlayerSelectingCard) return;

        ClearBluffTargetable();
        isPlayerSelectingCard = false;

        if (BluffActionUI.Instance != null)
            BluffActionUI.Instance.ExitCardSelectionMode();
    }

    private void OnPlayerCardSelectedForBluff(CardObject card)
    {
        if (!isPlayerSelectingCard) return;

        var cards = playerHand.GetCards();
        int cardIndex = cards.IndexOf(card);
        if (cardIndex < 0) return;

        ClearBluffTargetable();
        isPlayerSelectingCard = false;

        if (BluffActionUI.Instance != null)
            BluffActionUI.Instance.ExitCardSelectionMode();

        // アクション実行
        switch (pendingPlayerAction)
        {
            case BluffActionType.Push:
                playerHand.PushCard(cardIndex);
                break;
            case BluffActionType.Pull:
                playerHand.PullCard(cardIndex);
                break;
            case BluffActionType.Wiggle:
                playerHand.WiggleCard(cardIndex);
                break;
        }

        lastPlayerActionTime = Time.time;
        RecordAction(pendingPlayerAction, BluffActionSource.Player, cardIndex);
    }

    private void ClearBluffTargetable()
    {
        if (playerHand == null) return;
        var cards = playerHand.GetCards();
        foreach (var card in cards)
        {
            card.SetBluffTargetable(false);
            card.OnCardClickedForBluff.RemoveListener(OnPlayerCardSelectedForBluff);
        }
    }

    // =========================================
    // AI Autonomous Bluff
    // =========================================

    public void StartAIBluffMonitor()
    {
        StopAIBluffMonitor();
        aiBluffCoroutine = StartCoroutine(AIBluffCoroutine());
    }

    public void StopAIBluffMonitor()
    {
        if (aiBluffCoroutine != null)
        {
            StopCoroutine(aiBluffCoroutine);
            aiBluffCoroutine = null;
        }
    }

    private IEnumerator AIBluffCoroutine()
    {
        if (aiHand == null) yield break;

        // 初回アクションまでの待機（序盤は短く、終盤は長く）
        int totalCards = aiHand.GetCardCount() + (playerHand != null ? playerHand.GetCardCount() : 0);
        float initialDelay = totalCards >= 15 ? Random.Range(1.0f, 2.5f) : Random.Range(1.5f, 4.0f); // より早く開始
        yield return new WaitForSeconds(initialDelay);

        int actionCount = 0;
        // 序盤: 6-9回、中盤: 4-6回、終盤: 2-4回（頻度UP）
        int maxActions = totalCards >= 15 ? Random.Range(6, 10) : totalCards >= 8 ? Random.Range(4, 7) : Random.Range(2, 5);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[BluffActionSystem] AI Bluff Monitor started: maxActions={maxActions}, totalCards={totalCards}");
#endif

        while (actionCount < maxActions)
        {
            if (GameManager.Instance == null || !IsBluffAllowedState(GameManager.Instance.CurrentState))
                yield break;

            // アニメーション完了待ち
            while (aiHand.IsBluffAnimating)
                yield return null;

            // 動的な発動確率計算
            float chance = CalculateAIBluffChance();

            if (Random.value < chance)
            {
                BluffActionType action = DecideAIBluffAction();
                int targetIndex = DecideAITargetCard(action);
                ExecuteAIBluffAction(action, targetIndex);
                lastAIActionTime = Time.time;
                playerBluffedRecently = false;
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[BluffActionSystem] AI skipped bluff action (chance={chance:F2})");
#endif
            }

            actionCount++;

            // 動的なクールダウン: 序盤は短く（1-2秒）、終盤は長く（3-4.5秒）
            int currentTotalCards = aiHand.GetCardCount() + (playerHand != null ? playerHand.GetCardCount() : 0);
            float cooldown = currentTotalCards >= 15 ? Random.Range(1.0f, 2.0f) :
                             currentTotalCards >= 8 ? Random.Range(1.5f, 3.0f) :
                             Random.Range(3.0f, 4.5f);

            yield return new WaitForSeconds(cooldown);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[BluffActionSystem] AI Bluff Monitor ended: {actionCount} actions performed");
#endif
    }

    private float CalculateAIBluffChance()
    {
        float chance = aiBaseBluffChance;

        // ゲームフェーズ補正
        int totalCards = aiHand.GetCardCount() + (playerHand != null ? playerHand.GetCardCount() : 0);
        if (totalCards >= 15)
        {
            // 序盤: 積極的（+0.15）
            chance += 0.15f;
        }
        else if (totalCards <= 7)
        {
            // 終盤: やや慎重（-0.10）
            chance -= 0.10f;
        }

        // 感情補正
        if (BluffSystem.Instance != null)
        {
            var emotion = BluffSystem.Instance.GetCurrentEmotion();
            if (emotion == AIEmotion.Frustrated) chance += 0.15f;
            else if (emotion == AIEmotion.Pleased) chance += 0.12f;
            else if (emotion == AIEmotion.Anticipating) chance += 0.08f; // 警戒時も少しブラフ増加
            else if (emotion == AIEmotion.Hurt) chance += 0.10f;
        }

        // 圧力補正
        if (PsychologySystem.Instance != null)
        {
            float pressureLevel = PsychologySystem.Instance.GetPressureLevel();
            if (pressureLevel > 2.0f) chance += 0.15f;
            else if (pressureLevel > 1.5f) chance += 0.10f;
            else if (pressureLevel < 0.5f) chance += 0.05f; // 余裕時も少しブラフ
        }

        // プレイヤーがbluffした直後の反応（対抗心）
        if (playerBluffedRecently) chance += 0.20f;

        // Joker所持時の心理的ブラフ
        if (aiHand.GetJokerIndex() >= 0)
            chance += 0.10f; // Joker持ちは隠すためにブラフ増加

        return Mathf.Clamp(chance, 0.35f, aiMaxBluffChance); // 最低35%、最大95%（何もしない確率を減少）
    }

    private BluffActionType DecideAIBluffAction()
    {
        bool hasJoker = aiHand.GetJokerIndex() >= 0;
        int aiCardCount = aiHand.GetCardCount();
        int playerCardCount = playerHand != null ? playerHand.GetCardCount() : 0;
        int totalCards = aiCardCount + playerCardCount;
        float pressureLevel = PsychologySystem.Instance != null ? PsychologySystem.Instance.GetPressureLevel() : 0f;
        var emotion = BluffSystem.Instance != null ? BluffSystem.Instance.GetCurrentEmotion() : AIEmotion.Calm;

        // ===== ゲームフェーズ判定 =====
        // 序盤: 総カード15枚以上 → シャッフル多用で記憶撹乱
        // 中盤: 総カード8-14枚 → バランス型、状況適応
        // 終盤: 総カード7枚以下 → ターゲット型、Joker隠蔽

        float shuffleWeight, pushWeight, pullWeight, wiggleWeight, spreadWeight, closeWeight;

        if (totalCards >= 15)
        {
            // 序盤: シャッフル主体戦略 + Push/Wiggle増加
            shuffleWeight = 4.0f;
            pushWeight = 1.5f; // 0.5 → 1.5 (3倍)
            pullWeight = 0.8f; // 0.3 → 0.8
            wiggleWeight = 1.2f; // 0.4 → 1.2 (3倍)
            spreadWeight = 0.7f;
            closeWeight = 0.3f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BluffActionSystem] Early game strategy: Shuffle-heavy (total cards: {totalCards})");
#endif
        }
        else if (totalCards >= 8)
        {
            // 中盤: 状況適応型 + Push/Wiggle強化
            shuffleWeight = 1.5f;
            pushWeight = hasJoker ? 3.5f : 2.0f; // Joker有: 2.0→3.5, 無: 1.0→2.0
            pullWeight = 1.2f; // 0.8 → 1.2
            wiggleWeight = 2.0f; // 1.0 → 2.0 (2倍)
            spreadWeight = 0.7f;
            closeWeight = 0.5f;

            // 感情による戦略変更
            if (emotion == AIEmotion.Frustrated)
            {
                // イライラ → Wiggle/Push多用（カード上げて挑発）
                wiggleWeight += 2.5f; // 1.5 → 2.5
                pushWeight += 2.0f; // 1.0 → 2.0
            }
            else if (emotion == AIEmotion.Pleased)
            {
                // 余裕 → Spread/Shuffle多用（見せびらかし）
                spreadWeight += 1.2f;
                shuffleWeight += 0.8f;
            }
            else if (emotion == AIEmotion.Anticipating || emotion == AIEmotion.Hurt)
            {
                // 警戒/不安 → Close/Pull多用（隠蔽）
                closeWeight += 1.0f;
                pullWeight += 0.8f;
            }

            // 圧力レベルによる補正
            if (pressureLevel > 2.0f)
            {
                // 高圧力 → シャッフル/Wiggle増加（動揺演出）
                shuffleWeight += 1.0f;
                wiggleWeight += 0.8f;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BluffActionSystem] Mid game strategy: Adaptive (emotion: {emotion}, pressure: {pressureLevel:F1})");
#endif
        }
        else
        {
            // 終盤: ターゲット型戦略（Joker隠蔽・ミスディレクション）「Jokerはここだよ〜」多用
            shuffleWeight = 0.8f;
            pushWeight = hasJoker ? 5.0f : 2.0f; // Joker有: 3.0→5.0 (大幅UP!)
            pullWeight = hasJoker ? 0.8f : 2.5f; // Jokerなし→Pull多用（偽装）
            wiggleWeight = hasJoker ? 4.0f : 2.5f; // Joker有: 2.0→4.0 (2倍!)
            spreadWeight = 0.3f;
            closeWeight = hasJoker ? 1.5f : 0.5f; // Joker持ち→Close多用

            // プレイヤーのカード数が少ない（エンドゲーム）
            if (playerCardCount <= 3)
            {
                // 最終フェーズ: より攻撃的・心理的「Jokerはここだよ〜」ブラフ連打
                pushWeight += 3.0f; // 1.5 → 3.0
                wiggleWeight += 2.5f; // 1.0 → 2.5
                shuffleWeight = 0.3f; // シャッフル減らす（もう意味ない）
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[BluffActionSystem] Late game strategy: Targeted (hasJoker: {hasJoker}, AI: {aiCardCount}, Player: {playerCardCount})");
#endif
        }

        // プレイヤーの直近行動に対するカウンター戦略
        if (playerBluffedRecently && actionHistory.Count > 0)
        {
            var lastPlayerAction = actionHistory[actionHistory.Count - 1];
            if (lastPlayerAction.source == BluffActionSource.Player)
            {
                // プレイヤーがShuffleした直後 → AIもShuffleで対抗
                if (lastPlayerAction.actionType == BluffActionType.Shuffle)
                    shuffleWeight += 2.0f;
                // プレイヤーがSpreadした直後 → AIはClose/Shuffleで対抗
                else if (lastPlayerAction.actionType == BluffActionType.Spread)
                {
                    closeWeight += 1.5f;
                    shuffleWeight += 1.0f;
                }
            }
        }

        // 連続同一アクション防止（前回と同じアクションは重み50%減）
        float samePenalty = 0.5f;
        if (lastAIAction == BluffActionType.Shuffle) shuffleWeight *= samePenalty;
        else if (lastAIAction == BluffActionType.Push) pushWeight *= samePenalty;
        else if (lastAIAction == BluffActionType.Pull) pullWeight *= samePenalty;
        else if (lastAIAction == BluffActionType.Wiggle) wiggleWeight *= samePenalty;
        else if (lastAIAction == BluffActionType.Spread) spreadWeight *= samePenalty;
        else if (lastAIAction == BluffActionType.Close) closeWeight *= samePenalty;

        float totalWeight = shuffleWeight + pushWeight + pullWeight + wiggleWeight + spreadWeight + closeWeight;
        float roll = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        cumulative += shuffleWeight; if (roll < cumulative) return BluffActionType.Shuffle;
        cumulative += pushWeight;    if (roll < cumulative) return BluffActionType.Push;
        cumulative += pullWeight;    if (roll < cumulative) return BluffActionType.Pull;
        cumulative += wiggleWeight;  if (roll < cumulative) return BluffActionType.Wiggle;
        cumulative += spreadWeight;  if (roll < cumulative) return BluffActionType.Spread;
        return BluffActionType.Close;
    }

    private int DecideAITargetCard(BluffActionType action)
    {
        // Shuffle/Spread/Closeは全体アクション
        if (action == BluffActionType.Shuffle || action == BluffActionType.Spread || action == BluffActionType.Close)
            return -1;

        int cardCount = aiHand.GetCardCount();
        if (cardCount == 0) return -1;

        int jokerIndex = aiHand.GetJokerIndex();
        int totalCards = cardCount + (playerHand != null ? playerHand.GetCardCount() : 0);

        // ===== ゲームフェーズ別ターゲット戦略 =====

        if (totalCards >= 15)
        {
            // 序盤: Joker以外をターゲット（ミスディレクション）
            if (jokerIndex >= 0)
            {
                // Joker以外のカードからランダム選択
                int targetIndex;
                int attempts = 0;
                do
                {
                    targetIndex = Random.Range(0, cardCount);
                    attempts++;
                } while (targetIndex == jokerIndex && attempts < 5); // 最大5回試行

                return targetIndex;
            }
            else
            {
                return Random.Range(0, cardCount);
            }
        }
        else if (totalCards >= 8)
        {
            // 中盤: バランス型（50-50）
            if (jokerIndex >= 0 && Random.value < 0.5f)
                return jokerIndex;

            return Random.Range(0, cardCount);
        }
        else
        {
            // 終盤: Joker多用（95%でJokerアピール - "ここにJokerあるよ"ブラフ）
            if (jokerIndex >= 0 && Random.value < 0.95f)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[BluffActionSystem] Late game: Targeting JOKER (misdirection bluff)");
#endif
                return jokerIndex;
            }

            return Random.Range(0, cardCount);
        }
    }

    private void ExecuteAIBluffAction(BluffActionType action, int targetIndex)
    {
        switch (action)
        {
            case BluffActionType.Shuffle:
                aiHand.ShuffleCards();
                break;
            case BluffActionType.Push:
                aiHand.PushCard(targetIndex);
                break;
            case BluffActionType.Pull:
                aiHand.PullCard(targetIndex);
                break;
            case BluffActionType.Wiggle:
                aiHand.WiggleCard(targetIndex);
                break;
            case BluffActionType.Spread:
                aiHand.SpreadFan();
                break;
            case BluffActionType.Close:
                aiHand.CloseFan();
                break;
        }

        lastAIAction = action; // 連続同一アクション防止用
        RecordAction(action, BluffActionSource.AI, targetIndex);

        // AIブラフ台詞を表示
        string dialogue = GetAIBluffDialogue(action, targetIndex);
        if (!string.IsNullOrEmpty(dialogue))
        {
            // Jokerタント判定（終盤 + Jokerターゲット）
            bool isJokerTaunt = false;
            if (aiHand != null && targetIndex >= 0)
            {
                int totalCards = aiHand.GetCardCount() + (playerHand != null ? playerHand.GetCardCount() : 0);
                bool isLateGame = totalCards <= 7;
                bool isJokerTarget = aiHand.GetJokerIndex() == targetIndex;
                isJokerTaunt = isLateGame && isJokerTarget;
            }

            ShowAIBluffDialogue(dialogue, isJokerTaunt);

            // Jokerタント時、カードにジャンプアニメーション追加
            if (isJokerTaunt)
            {
                CardObject jokerCard = aiHand.GetCardAtIndex(targetIndex);
                if (jokerCard != null)
                {
                    Vector3 currentPos = jokerCard.transform.localPosition;
                    jokerCard.transform.DOLocalJump(currentPos, 0.15f, 1, 0.6f)
                        .SetEase(Ease.OutQuad);
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[BluffActionSystem] AI performed {action}" + (targetIndex >= 0 ? $" on card {targetIndex}" : "") + $" - \"{dialogue}\"");
#endif
    }

    /// <summary>
    /// AIブラフアクションの台詞を取得
    /// </summary>
    private string GetAIBluffDialogue(BluffActionType action, int targetIndex)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return "";

        // ターゲットがJokerかチェック（Push/Wiggle/Shuffleの場合、Joker専用台詞がある）
        bool isJokerTarget = targetIndex >= 0 && aiHand != null && aiHand.GetJokerIndex() == targetIndex;
        int totalCards = (aiHand != null ? aiHand.GetCardCount() : 0) + (playerHand != null ? playerHand.GetCardCount() : 0);
        bool isLateGame = totalCards <= 7;

        string[] candidates = null;

        // 終盤 + Jokerターゲット → Joker専用台詞を優先
        if (isLateGame && isJokerTarget)
        {
            switch (action)
            {
                case BluffActionType.Shuffle:
                    candidates = loc.GetArray("bluff_action.ai_shuffle_joker");
                    break;
                case BluffActionType.Push:
                    candidates = loc.GetArray("bluff_action.ai_push_joker");
                    break;
                case BluffActionType.Wiggle:
                    candidates = loc.GetArray("bluff_action.ai_wiggle_joker");
                    break;
            }

            // Joker専用台詞がある場合はそれを使用
            if (candidates != null && candidates.Length > 0)
            {
                return candidates[Random.Range(0, candidates.Length)];
            }
        }

        // 通常台詞（またはJoker専用台詞がない場合のフォールバック）
        switch (action)
        {
            case BluffActionType.Shuffle:
                candidates = loc.GetArray("bluff_action.ai_shuffle");
                break;
            case BluffActionType.Push:
                candidates = loc.GetArray("bluff_action.ai_push");
                break;
            case BluffActionType.Pull:
                candidates = loc.GetArray("bluff_action.ai_pull");
                break;
            case BluffActionType.Wiggle:
                candidates = loc.GetArray("bluff_action.ai_wiggle");
                break;
            case BluffActionType.Spread:
                candidates = loc.GetArray("bluff_action.ai_spread");
                break;
            case BluffActionType.Close:
                candidates = loc.GetArray("bluff_action.ai_close");
                break;
        }

        if (candidates == null || candidates.Length == 0)
            return "";

        return candidates[Random.Range(0, candidates.Length)];
    }

    /// <summary>
    /// AIブラフ台詞を表示
    /// </summary>
    private void ShowAIBluffDialogue(string dialogue, bool isJokerTaunt = false)
    {
        if (SubtitleUI.Instance != null)
        {
            float pressureLevel = PsychologySystem.Instance != null ? PsychologySystem.Instance.GetPressureLevel() : 0f;

            // Jokerタントの場合、特別な演出を追加
            if (isJokerTaunt)
            {
                // 圧力レベルを一時的に上昇（+0.2）
                pressureLevel = Mathf.Min(pressureLevel + 0.2f, 1.0f);

                // AIAttentionMarkerをLockedステートに（視線が固定される演出）
                if (AIAttentionMarker.Instance != null)
                {
                    AIAttentionMarker.Instance.SetVisualState(AIAttentionMarker.MarkerVisualState.Locked);
                    StartCoroutine(ResetMarkerAfterDelay(2.5f));
                }

                // Jokerタント時は表示時間を少し長く（4秒）
                SubtitleUI.Instance.Show(dialogue, pressureLevel);
                StartCoroutine(HideSubtitleAfterDelay(4f));
            }
            else
            {
                // 通常のブラフ台詞
                SubtitleUI.Instance.Show(dialogue, pressureLevel);
                StartCoroutine(HideSubtitleAfterDelay(3f));
            }
        }
    }

    /// <summary>
    /// AIAttentionMarkerをScanningステートにリセット
    /// </summary>
    private IEnumerator ResetMarkerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AIAttentionMarker.Instance != null)
        {
            AIAttentionMarker.Instance.SetVisualState(AIAttentionMarker.MarkerVisualState.Scanning);
        }
    }

    // =========================================
    // Recording & Reactions
    // =========================================

    private void RecordAction(BluffActionType actionType, BluffActionSource source, int targetIndex)
    {
        var record = new BluffActionRecord
        {
            actionType = actionType,
            source = source,
            targetCardIndex = targetIndex,
            timestamp = Time.time
        };

        actionHistory.Add(record);
        OnBluffActionPerformed?.Invoke(record);

        // プレイヤーのアクションにAIが反応
        if (source == BluffActionSource.Player)
        {
            playerBluffedRecently = true;

            // Stage 16: Record bluff action in behavior analyzer
            var behaviorAnalyzer = FindFirstObjectByType<FPSTrump.Psychology.PlayerBehaviorAnalyzer>();
            if (behaviorAnalyzer != null)
            {
                behaviorAnalyzer.RecordBluffAction(actionType, targetIndex);
            }

            if (BluffSystem.Instance != null)
            {
                string reaction = BluffSystem.Instance.EvaluateBluffAction(record);
                if (!string.IsNullOrEmpty(reaction))
                {
                    ShowAIReaction(reaction);
                }
            }
        }
    }

    private void ShowAIReaction(string dialogue)
    {
        if (SubtitleUI.Instance != null)
        {
            float pressureLevel = PsychologySystem.Instance != null ? PsychologySystem.Instance.GetPressureLevel() : 0f;
            SubtitleUI.Instance.Show(dialogue, pressureLevel);

            // 2秒後に自動非表示
            StartCoroutine(HideSubtitleAfterDelay(2f));
        }
    }

    private IEnumerator HideSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (SubtitleUI.Instance != null)
            SubtitleUI.Instance.Hide();
    }

    // =========================================
    // Utility
    // =========================================

    private bool IsBluffAllowedState(GameState state)
    {
        return state == GameState.PLAYER_TURN_PICK
            || state == GameState.AI_TURN_APPROACH
            || state == GameState.AI_TURN_HESITATE
            || state == GameState.AI_TURN_COMMIT;
    }

    // =========================================
    // Stage 15: ブラフ統計取得
    // =========================================

    /// <summary>
    /// プレイヤーのブラフ回数を取得
    /// </summary>
    public int GetPlayerBluffCount()
    {
        return actionHistory.FindAll(r => r.source == BluffActionSource.Player).Count;
    }

    /// <summary>
    /// プレイヤーのブラフタイプ別頻度を取得
    /// </summary>
    public Dictionary<BluffActionType, int> GetPlayerBluffFrequency()
    {
        var freq = new Dictionary<BluffActionType, int>();
        foreach (var record in actionHistory)
        {
            if (record.source == BluffActionSource.Player)
            {
                if (!freq.ContainsKey(record.actionType))
                    freq[record.actionType] = 0;
                freq[record.actionType]++;
            }
        }
        return freq;
    }

    /// <summary>
    /// 最も使われたプレイヤーブラフタイプを取得
    /// </summary>
    public BluffActionType GetMostUsedPlayerBluffType()
    {
        var freq = GetPlayerBluffFrequency();
        if (freq.Count == 0) return BluffActionType.Shuffle;

        BluffActionType mostUsed = BluffActionType.Shuffle;
        int maxCount = 0;
        foreach (var kvp in freq)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostUsed = kvp.Key;
            }
        }
        return mostUsed;
    }

    /// <summary>
    /// 直近Nターンのプレイヤーブラフ回数を取得
    /// </summary>
    public int GetRecentPlayerBluffCount(int lastNActions = 5)
    {
        int count = 0;
        int checkCount = Mathf.Min(lastNActions, actionHistory.Count);
        for (int i = actionHistory.Count - 1; i >= actionHistory.Count - checkCount; i--)
        {
            if (actionHistory[i].source == BluffActionSource.Player)
                count++;
        }
        return count;
    }

    public void ResetSystem()
    {
        StopAIBluffMonitor();
        CancelCardSelection();
        actionHistory.Clear();
        lastPlayerActionTime = -10f;
        lastAIActionTime = -10f;
        playerBluffedRecently = false;

        if (playerHand != null) playerHand.ResetFanModifier();
        if (aiHand != null) aiHand.ResetFanModifier();
    }

    public List<BluffActionRecord> GetActionHistory() => new List<BluffActionRecord>(actionHistory);

    private string GetActionDisplayName(BluffActionType actionType)
    {
        return actionType switch
        {
            BluffActionType.Push => "Push",
            BluffActionType.Pull => "Pull",
            BluffActionType.Wiggle => "Wiggle",
            _ => actionType.ToString()
        };
    }
}
