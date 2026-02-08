# AI・NPC挙動詳細仕様

## AI概要

### AI Persona（性格・役割）
```yaml
Name: "The Dealer" (ディーラー)
Personality:
  - Cold & Calculated（冷静・計算高い）
  - Psychologically Manipulative（心理操作的）
  - Mysteriously Knowledgeable（謎めいた洞察力）
  - Professional（プロフェッショナル）

Physical Appearance:
  - 仮面またはフード（顔は隠される）
  - 長い指（カードを扱いやすい）
  - 暗いスーツまたはローブ
  - 光る目（赤または青）

Voice Characteristics:
  - 低い声、落ち着いた口調
  - 時々皮肉めいた笑い
  - 重要な時は囁き声
  - 圧力をかける時は威圧的
```

## AI行動システム

### 1. 選択行動（AIターン）

#### AIHandController.cs - 拡張版
```csharp
public class AIHandController : HandController
{
    [Header("AI Behavior Settings")]
    public AIPersonality personality;
    public float thinkingTimeMin = 1.0f;
    public float thinkingTimeMax = 3.5f;
    public AnimationCurve decisionCurve;
    
    [Header("Visual Behavior")]
    public Transform aiHand;           // AI の手モデル
    public Transform aiFace;           // AI の顔/仮面
    public Light aiEyeLight;           // 目の光エフェクト
    
    [Header("Psychological Manipulation")]
    public float aggressionLevel = 0.5f;        // 攻撃性 0-1
    public float observationLevel = 0.8f;       // 観察力 0-1
    public float manipulationSkill = 0.7f;      // 心理操作スキル 0-1
    
    private AIDecisionMaker decisionMaker;
    private AIEmotionalState currentEmotion;
    private PlayerBehaviorAnalyzer behaviorAnalyzer;
    
    public UnityEvent<string> OnAIThought;      // AI の思考を外部に通知
    public UnityEvent<float> OnAggressionChanged;
    
    private void Start()
    {
        decisionMaker = new AIDecisionMaker(personality);
        behaviorAnalyzer = FindObjectOfType<PlayerBehaviorAnalyzer>();
        currentEmotion = AIEmotionalState.Neutral;
    }
    
    /// <summary>
    /// AI のターン実行（プレイヤーからカードを引く）
    /// </summary>
    public IEnumerator ExecuteAITurn(PlayerHandController playerHand)
    {
        // 1. 思考時間（演出）
        yield return StartCoroutine(ShowThinkingBehavior());
        
        // 2. プレイヤー分析
        BehaviorData playerBehavior = behaviorAnalyzer.GetCurrentBehavior();
        AnalyzePlayerState(playerBehavior);
        
        // 3. カード選択決定
        AIDecision decision = decisionMaker.MakeDecision(playerHand.cardsInHand, playerBehavior);
        
        // 4. 心理圧セリフ
        yield return StartCoroutine(DeliverPreDrawDialogue(decision));
        
        // 5. カード引き抜き演出
        yield return StartCoroutine(DrawCardWithAnimation(playerHand, decision.selectedIndex));
        
        // 6. 事後セリフ
        yield return StartCoroutine(DeliverPostDrawDialogue(decision));
        
        // 7. ペア判定・感情更新
        UpdateEmotionalState(decision.wasTargetCard);
    }
    
    /// <summary>
    /// 思考中の視覚的演出
    /// </summary>
    private IEnumerator ShowThinkingBehavior()
    {
        float thinkingTime = Random.Range(thinkingTimeMin, thinkingTimeMax);
        
        // 目の光が明滅
        StartCoroutine(BlinkEyeLight(thinkingTime));
        
        // 顔がプレイヤーの手札をスキャン
        yield return StartCoroutine(ScanPlayerCards(thinkingTime * 0.7f));
        
        // 指でテーブルを軽く叩く
        StartCoroutine(FingerTapping(thinkingTime * 0.3f));
        
        yield return new WaitForSeconds(thinkingTime);
    }
    
    /// <summary>
    /// カード引き抜きアニメーション
    /// </summary>
    private IEnumerator DrawCardWithAnimation(PlayerHandController playerHand, int cardIndex)
    {
        CardObject targetCard = playerHand.cardsInHand[cardIndex];
        Vector3 cardOriginalPos = targetCard.transform.position;
        
        // 1. AI の手が伸びる
        Vector3 handStartPos = aiHand.position;
        Vector3 handTargetPos = cardOriginalPos + Vector3.up * 0.1f;
        
        // 手の移動アニメーション（1.2秒）
        float handMoveTime = 1.2f;
        for (float t = 0; t < handMoveTime; t += Time.deltaTime)
        {
            float progress = decisionCurve.Evaluate(t / handMoveTime);
            aiHand.position = Vector3.Lerp(handStartPos, handTargetPos, progress);
            
            // 手がカードに近づくにつれて、カードが少し震える
            if (progress > 0.6f)
            {
                float shake = (progress - 0.6f) * 0.02f;
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shake, shake),
                    Random.Range(-shake, shake),
                    0
                );
                targetCard.transform.position = cardOriginalPos + shakeOffset;
            }
            
            yield return null;
        }
        
        // 2. カードを掴む（0.3秒）
        targetCard.transform.SetParent(aiHand);
        
        // 掴み音
        AudioManager.Instance.PlaySFX("card_grab");
        
        // カメラの微震動
        Camera.main.GetComponent<FPSCameraController>().ShakeCamera(0.02f, 0.2f);
        
        yield return new WaitForSeconds(0.3f);
        
        // 3. カードを AI 側に引き寄せる（0.8秒）
        Vector3 aiHandPos = transform.position + Vector3.back * 0.3f;
        float pullTime = 0.8f;
        
        for (float t = 0; t < pullTime; t += Time.deltaTime)
        {
            float progress = t / pullTime;
            targetCard.transform.position = Vector3.Lerp(handTargetPos, aiHandPos, progress);
            
            // カードを徐々に回転させて裏向きに
            float flipProgress = Mathf.Clamp01((progress - 0.3f) / 0.4f);
            targetCard.transform.rotation = Quaternion.Lerp(
                Quaternion.identity,
                Quaternion.Euler(0, 180, 0),
                flipProgress
            );
            
            yield return null;
        }
        
        // 4. 手を元の位置に戻す
        aiHand.DOMove(handStartPos, 0.6f).SetEase(Ease.OutQuart);
        
        // 5. カードを AI の手札に追加
        playerHand.cardsInHand.Remove(targetCard);
        playerHand.ArrangeCards();
        
        targetCard.isFaceUp = false;
        AddCard(targetCard);
        
        yield return new WaitForSeconds(0.6f);
    }
    
    /// <summary>
    /// プレイヤーの状態分析（AI の観察力を反映）
    /// </summary>
    private void AnalyzePlayerState(BehaviorData behavior)
    {
        // AI の観察力に応じて、プレイヤーの情報をより詳しく分析
        float analysisAccuracy = observationLevel * Random.Range(0.8f, 1.2f);
        
        // 分析結果を内部ログに記録（デバッグ用）
        string analysis = GenerateAnalysisReport(behavior, analysisAccuracy);
        OnAIThought?.Invoke(analysis);
        
        // 攻撃性レベルの動的調整
        if (behavior.doubtLevel > 0.7f && aggressionLevel < 0.8f)
        {
            aggressionLevel += 0.1f;  // プレイヤーが迷っているなら圧力を上げる
            OnAggressionChanged?.Invoke(aggressionLevel);
        }
    }
    
    private string GenerateAnalysisReport(BehaviorData behavior, float accuracy)
    {
        List<string> observations = new List<string>();
        
        if (accuracy > 0.7f)
        {
            if (behavior.streakSamePos >= 2)
                observations.Add($"Target prefers {GetPositionName(behavior.streakSamePos)} position - predictable");
                
            if (behavior.avgHoverTime > 2.0f)
                observations.Add("Target shows hesitation - exploitable");
                
            if (behavior.tempo == TempoType.Fast)
                observations.Add("Target is rushing - likely nervous");
        }
        
        if (accuracy > 0.9f)
        {
            if (behavior.doubtLevel > 0.6f)
                observations.Add("High doubt level detected - increase psychological pressure");
        }
        
        return string.Join(", ", observations);
    }
}

/// <summary>
/// AI 選択決定システム
/// </summary>
public class AIDecisionMaker
{
    private AIPersonality personality;
    private System.Random random;
    
    public AIDecisionMaker(AIPersonality personality)
    {
        this.personality = personality;
        this.random = new System.Random();
    }
    
    public AIDecision MakeDecision(List<CardObject> playerCards, BehaviorData playerBehavior)
    {
        // 1. 基本戦略の決定
        AIStrategy strategy = DetermineStrategy(playerBehavior);
        
        // 2. カード選択
        int selectedIndex = SelectCard(playerCards, strategy, playerBehavior);
        
        // 3. 心理圧戦術の決定
        PsychologyTactic tactic = ChoosePsychologyTactic(playerBehavior, strategy);
        
        return new AIDecision
        {
            selectedIndex = selectedIndex,
            strategy = strategy,
            tactic = tactic,
            confidence = CalculateConfidence(playerBehavior),
            wasTargetCard = false  // 引いた後に更新される
        };
    }
    
    private AIStrategy DetermineStrategy(BehaviorData playerBehavior)
    {
        // プレイヤーの行動パターンに応じて戦略を選択
        
        if (playerBehavior.doubtLevel > 0.7f)
            return AIStrategy.Aggressive;  // 迷いがあるなら圧力をかける
            
        if (playerBehavior.streakSamePos >= 3)
            return AIStrategy.Exploitative; // 癖があるなら利用する
            
        if (playerBehavior.tempo == TempoType.Fast)
            return AIStrategy.Calm;  // 焦りには冷静さで対抗
            
        return AIStrategy.Adaptive;  // デフォルト
    }
    
    private int SelectCard(List<CardObject> playerCards, AIStrategy strategy, BehaviorData behavior)
    {
        switch (strategy)
        {
            case AIStrategy.Aggressive:
                // 攻撃的：プレイヤーが避けたがる位置を選択
                return SelectMostAvoidedPosition(playerCards, behavior);
                
            case AIStrategy.Exploitative:
                // 搾取的：プレイヤーの癖を利用
                return SelectBasedOnPlayerHabit(playerCards, behavior);
                
            case AIStrategy.Calm:
                // 冷静：ランダム選択だが最適化
                return SelectOptimalRandom(playerCards);
                
            case AIStrategy.Adaptive:
            default:
                // 適応的：バランスの良い選択
                return SelectBalanced(playerCards, behavior);
        }
    }
    
    private int SelectMostAvoidedPosition(List<CardObject> playerCards, BehaviorData behavior)
    {
        // プレイヤーが最も選ばない位置を特定
        int[] positionCounts = behavior.positionCounts;
        int minCount = positionCounts.Min();
        int avoidedPosition = Array.IndexOf(positionCounts, minCount);
        
        // その位置に対応するカードインデックス
        return Mathf.Clamp(avoidedPosition * (playerCards.Count / 3), 0, playerCards.Count - 1);
    }
    
    private int SelectBasedOnPlayerHabit(List<CardObject> playerCards, BehaviorData behavior)
    {
        // プレイヤーが最も選ぶ位置の近くを避ける
        int[] positionCounts = behavior.positionCounts;
        int maxCount = positionCounts.Max();
        int preferredPosition = Array.IndexOf(positionCounts, maxCount);
        
        // 好みの位置を避けて選択
        int avoidIndex = preferredPosition * (playerCards.Count / 3);
        int selectedIndex = (avoidIndex + playerCards.Count / 2) % playerCards.Count;
        
        return Mathf.Clamp(selectedIndex, 0, playerCards.Count - 1);
    }
    
    private float CalculateConfidence(BehaviorData behavior)
    {
        float baseConfidence = 0.5f;
        
        // プレイヤーが迷っているほど AI の自信は上がる
        if (behavior.doubtLevel > 0.5f)
            baseConfidence += 0.3f;
            
        // プレイヤーに癖があるほど予測しやすい
        if (behavior.streakSamePos >= 2)
            baseConfidence += 0.2f;
            
        return Mathf.Clamp01(baseConfidence);
    }
}

/// <summary>
/// AI の感情状態
/// </summary>
public enum AIEmotionalState
{
    Neutral,        // 中立
    Confident,      // 自信満々
    Amused,         // 面白がっている
    Focused,        // 集中している
    Intimidating,   // 威圧的
    Calculating     // 計算中
}

/// <summary>
/// AI 戦略タイプ
/// </summary>
public enum AIStrategy
{
    Aggressive,     // 攻撃的
    Exploitative,   // 搾取的
    Calm,           // 冷静
    Adaptive        // 適応的
}

/// <summary>
/// 心理圧戦術
/// </summary>
public enum PsychologyTactic
{
    Intimidation,   // 威圧
    Misdirection,   // 誤誘導
    Encouragement,  // 誘導
    Silence,        // 沈黙
    Analysis        // 分析開示
}

/// <summary>
/// AI 決定データ
/// </summary>
[System.Serializable]
public struct AIDecision
{
    public int selectedIndex;
    public AIStrategy strategy;
    public PsychologyTactic tactic;
    public float confidence;
    public bool wasTargetCard;
    public string reasoning;  // 決定理由（デバッグ用）
}
```

## セリフ配信システム詳細

### AIDialogueController.cs
```csharp
public class AIDialogueController : MonoBehaviour
{
    [Header("Dialogue Timing")]
    public float preDrawDialogueDelay = 0.5f;
    public float postDrawDialogueDelay = 1.0f;
    public float silenceProbability = 0.2f;  // 時々黙る確率
    
    [Header("Voice Settings")]
    public AudioSource voiceSource;
    public float baseVoicePitch = 0.85f;
    public AudioMixerGroup voiceMixer;
    
    private DialogueDatabase dialogueDB;
    private AIPersonality currentPersonality;
    private Queue<string> pendingDialogues = new Queue<string>();
    
    public UnityEvent<string> OnDialogueSpoken;
    
    /// <summary>
    /// カード引き抜き前のセリフ
    /// </summary>
    public IEnumerator DeliverPreDrawDialogue(AIDecision decision)
    {
        if (ShouldStaysilent()) yield break;
        
        DialogueEntry dialogue = SelectPreDrawDialogue(decision);
        
        yield return StartCoroutine(SpeakDialogue(dialogue));
    }
    
    /// <summary>
    /// カード引き抜き後のセリフ
    /// </summary>
    public IEnumerator DeliverPostDrawDialogue(AIDecision decision)
    {
        // 引いたカードがジョーカーかどうかで反応を変える
        DialogueEntry dialogue = SelectPostDrawDialogue(decision);
        
        yield return StartCoroutine(SpeakDialogue(dialogue));
    }
    
    private DialogueEntry SelectPreDrawDialogue(AIDecision decision)
    {
        switch (decision.tactic)
        {
            case PsychologyTactic.Intimidation:
                return dialogueDB.GetRandomDialogue("intimidation_pre");
                
            case PsychologyTactic.Misdirection:
                return dialogueDB.GetRandomDialogue("misdirection_pre");
                
            case PsychologyTactic.Encouragement:
                return dialogueDB.GetRandomDialogue("encouragement_pre");
                
            case PsychologyTactic.Silence:
                return null;  // 無言
                
            case PsychologyTactic.Analysis:
                return GenerateAnalysisDialogue(decision);
                
            default:
                return dialogueDB.GetRandomDialogue("neutral_pre");
        }
    }
    
    private DialogueEntry GenerateAnalysisDialogue(AIDecision decision)
    {
        // AI が分析結果をプレイヤーに告知
        List<string> analysisLines = new List<string>
        {
            "君の癖は見透かしている",
            $"君は{GetPositionPreference()}を好む",
            "その迷い、興味深い",
            "計算通りだ"
        };
        
        string selectedLine = analysisLines[Random.Range(0, analysisLines.Count)];
        
        return new DialogueEntry
        {
            text = selectedLine,
            deliveryType = DialogueDeliveryType.Projection,
            audioClip = GenerateAnalysisAudio(selectedLine)
        };
    }
    
    /// <summary>
    /// セリフの音声合成・再生
    /// </summary>
    private IEnumerator SpeakDialogue(DialogueEntry dialogue)
    {
        if (dialogue == null) yield break;
        
        // 1. テキスト表示
        if (dialogue.deliveryType == DialogueDeliveryType.Projection)
        {
            ProjectionSystem.Instance.ShowProjection(dialogue);
        }
        else if (dialogue.deliveryType == DialogueDeliveryType.Whisper)
        {
            WhisperSystem.Instance.ShowWhisper(dialogue.text);
        }
        
        // 2. 音声再生
        if (dialogue.audioClip != null)
        {
            voiceSource.clip = dialogue.audioClip;
            voiceSource.pitch = baseVoicePitch + Random.Range(-0.1f, 0.1f);  // 微妙な変化
            voiceSource.Play();
            
            // 音声の長さだけ待機
            yield return new WaitForSeconds(dialogue.audioClip.length);
        }
        else
        {
            // 音声がない場合は短い停止
            yield return new WaitForSeconds(1.0f);
        }
        
        OnDialogueSpoken?.Invoke(dialogue.text);
    }
    
    private bool ShouldStaysilent()
    {
        return Random.value < silenceProbability;
    }
}
```

## AI視覚的演出システム

### AIVisualBehavior.cs
```csharp
public class AIVisualBehavior : MonoBehaviour
{
    [Header("Eye Behavior")]
    public Light leftEyeLight;
    public Light rightEyeLight;
    public Color normalEyeColor = Color.red;
    public Color focusedEyeColor = Color.orange;
    public Color amusedEyeColor = Color.cyan;
    
    [Header("Face Movement")]
    public Transform faceTransform;
    public float scanSpeed = 30f;
    public float focusIntensity = 2f;
    
    [Header("Hand Behavior")]
    public Transform handTransform;
    public Transform[] fingerTransforms;
    
    private Coroutine currentBehaviorCoroutine;
    
    /// <summary>
    /// プレイヤーのカードをスキャンする視線動作
    /// </summary>
    public IEnumerator ScanPlayerCards(float duration)
    {
        Vector3 originalRotation = faceTransform.eulerAngles;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // 左右にゆっくりと頭を動かす
            float scanProgress = Mathf.Sin((elapsed / duration) * Mathf.PI * 2) * 15f;
            Vector3 scanRotation = new Vector3(originalRotation.x, scanProgress, originalRotation.z);
            faceTransform.eulerAngles = scanRotation;
            
            // 目の光が強くなったり弱くなったり
            float intensity = 1.0f + Mathf.Sin((elapsed / duration) * Mathf.PI * 4) * 0.3f;
            leftEyeLight.intensity = intensity;
            rightEyeLight.intensity = intensity;
            
            yield return null;
        }
        
        // 元の位置に戻る
        faceTransform.DORotate(originalRotation, 0.5f);
        leftEyeLight.intensity = 1.0f;
        rightEyeLight.intensity = 1.0f;
    }
    
    /// <summary>
    /// 目の明滅（思考中）
    /// </summary>
    public IEnumerator BlinkEyeLight(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // 不規則な明滅
            if (Random.value < 0.1f)  // 10% chance per frame
            {
                StartCoroutine(SingleBlink());
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
            }
            
            yield return null;
        }
    }
    
    private IEnumerator SingleBlink()
    {
        // 消灯
        leftEyeLight.intensity = 0;
        rightEyeLight.intensity = 0;
        
        yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        
        // 点灯
        leftEyeLight.intensity = 1.0f;
        rightEyeLight.intensity = 1.0f;
    }
    
    /// <summary>
    /// 指でテーブルを叩く動作
    /// </summary>
    public IEnumerator FingerTapping(float duration)
    {
        float elapsed = 0f;
        Vector3 originalPos = handTransform.position;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // 0.8秒間隔で指を上下
            if (elapsed % 0.8f < 0.1f)
            {
                Vector3 tapPos = originalPos + Vector3.down * 0.02f;
                handTransform.DOMove(tapPos, 0.05f).SetLoops(2, LoopType.Yoyo);
                
                // タップ音
                AudioManager.Instance.PlaySFX("finger_tap");
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 感情に応じた視覚変化
    /// </summary>
    public void SetEmotionalState(AIEmotionalState emotion)
    {
        switch (emotion)
        {
            case AIEmotionalState.Confident:
                SetEyeColor(focusedEyeColor);
                SetEyeIntensity(1.5f);
                break;
                
            case AIEmotionalState.Amused:
                SetEyeColor(amusedEyeColor);
                StartCoroutine(AmusedEyeFlicker());
                break;
                
            case AIEmotionalState.Intimidating:
                SetEyeColor(Color.red);
                SetEyeIntensity(2.0f);
                StartCoroutine(IntimidatingStare());
                break;
                
            case AIEmotionalState.Focused:
                SetEyeColor(normalEyeColor);
                SetEyeIntensity(0.8f);
                break;
                
            default:
                SetEyeColor(normalEyeColor);
                SetEyeIntensity(1.0f);
                break;
        }
    }
    
    private void SetEyeColor(Color color)
    {
        leftEyeLight.color = color;
        rightEyeLight.color = color;
    }
    
    private void SetEyeIntensity(float intensity)
    {
        leftEyeLight.intensity = intensity;
        rightEyeLight.intensity = intensity;
    }
    
    private IEnumerator AmusedEyeFlicker()
    {
        for (int i = 0; i < 3; i++)
        {
            SetEyeIntensity(1.8f);
            yield return new WaitForSeconds(0.1f);
            SetEyeIntensity(1.0f);
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    private IEnumerator IntimidatingStare()
    {
        Vector3 playerDirection = Camera.main.transform.position - faceTransform.position;
        Quaternion stareRotation = Quaternion.LookRotation(playerDirection);
        
        faceTransform.DORotateQuaternion(stareRotation, 0.3f);
        
        // 3秒間見つめ続ける
        yield return new WaitForSeconds(3.0f);
        
        // 元に戻る
        faceTransform.DORotateQuaternion(Quaternion.identity, 0.5f);
    }
}
```

## AIパーソナリティシステム

### AIPersonality.cs (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "AIPersonality", menuName = "AI/Personality")]
public class AIPersonality : ScriptableObject
{
    [Header("Core Traits")]
    [Range(0, 1)] public float aggression = 0.5f;       // 攻撃性
    [Range(0, 1)] public float intelligence = 0.8f;     // 知性
    [Range(0, 1)] public float patience = 0.6f;         // 忍耐力
    [Range(0, 1)] public float manipulation = 0.7f;     // 心理操作傾向
    
    [Header("Behavioral Preferences")]
    public bool prefersDirectConfrontation = false;     // 直接対決を好むか
    public bool usesPsychologicalWarfare = true;       // 心理戦を使うか
    public bool adaptsToPlayerStyle = true;            // プレイヤーに適応するか
    public bool showsEmotions = false;                 // 感情を表に出すか
    
    [Header("Dialogue Preferences")]
    public DialogueStyle primaryStyle = DialogueStyle.Calculating;
    public float verbosity = 0.6f;  // おしゃべり度 0=寡黙 1=雄弁
    public bool usesMetaphors = true;  // 比喩を使うか
    public bool revealsThoughts = false;  // 思考を明かすか
    
    [Header("Strategic Tendencies")]
    public float riskTolerance = 0.5f;  // リスク許容度
    public float bluffFrequency = 0.3f;  // ブラフの頻度
    public float adaptationSpeed = 0.7f;  // 適応速度
    
    /// <summary>
    /// このパーソナリティに基づいてセリフを選択
    /// </summary>
    public string SelectDialogue(DialogueCategory category, float pressureLevel)
    {
        var suitableDialogues = category.entries
            .Where(d => IsDialogueSuitable(d, pressureLevel))
            .ToList();
            
        if (suitableDialogues.Count == 0)
            return category.entries[0].text;
            
        // パーソナリティに最も適したセリフを選択
        var bestMatch = suitableDialogues
            .OrderByDescending(d => CalculateDialogueFit(d))
            .First();
            
        return bestMatch.text;
    }
    
    private bool IsDialogueSuitable(DialogueEntry dialogue, float pressureLevel)
    {
        // 圧力レベルチェック
        if (pressureLevel < dialogue.minPressureLevel || pressureLevel > dialogue.maxPressureLevel)
            return false;
            
        // パーソナリティ適合チェック
        if (dialogue.requiresHighAggression && aggression < 0.7f)
            return false;
            
        if (dialogue.requiresSubtlety && aggression > 0.8f)
            return false;
            
        return true;
    }
    
    private float CalculateDialogueFit(DialogueEntry dialogue)
    {
        float fit = 1.0f;
        
        // 攻撃性との適合度
        if (dialogue.isAggressive && aggression > 0.7f)
            fit += 0.3f;
        else if (!dialogue.isAggressive && aggression < 0.3f)
            fit += 0.2f;
            
        // 知性との適合度
        if (dialogue.requiresIntelligence && intelligence > 0.8f)
            fit += 0.2f;
            
        // 心理操作との適合度
        if (dialogue.isPsychological && manipulation > 0.6f)
            fit += 0.3f;
            
        return fit;
    }
}

public enum DialogueStyle
{
    Calculating,    // 計算高い
    Intimidating,   // 威圧的
    Subtle,         // 微妙
    Direct,         // 直接的
    Mysterious      // 神秘的
}
```

## AI学習・適応システム

### AILearningSystem.cs
```csharp
public class AILearningSystem : MonoBehaviour
{
    [Header("Learning Settings")]
    public float learningRate = 0.1f;
    public int memoryCapacity = 50;  // 記憶できる行動履歴数
    
    private Dictionary<string, float> playerPatterns = new Dictionary<string, float>();
    private Queue<PlayerActionRecord> actionHistory = new Queue<PlayerActionRecord>();
    private AIPersonality adaptivePersonality;
    
    private void Start()
    {
        // ベースのパーソナリティをコピーして適応型に変換
        adaptivePersonality = ScriptableObject.CreateInstance<AIPersonality>();
        LoadBasePersonality();
    }
    
    /// <summary>
    /// プレイヤーの行動を学習
    /// </summary>
    public void LearnFromPlayerAction(int selectedPosition, float hoverTime, bool wasCorrectGuess)
    {
        PlayerActionRecord record = new PlayerActionRecord
        {
            position = selectedPosition,
            hoverTime = hoverTime,
            timestamp = Time.time,
            wasCorrectGuess = wasCorrectGuess
        };
        
        actionHistory.Enqueue(record);
        
        // メモリ容量超過時は古い記録を削除
        while (actionHistory.Count > memoryCapacity)
            actionHistory.Dequeue();
            
        // パターン学習
        UpdatePlayerPatterns();
        
        // パーソナリティ適応
        AdaptPersonality();
    }
    
    private void UpdatePlayerPatterns()
    {
        var recentActions = actionHistory.TakeLast(20).ToArray();
        
        // 位置選択パターンの学習
        var positionPreferences = recentActions
            .GroupBy(a => a.position)
            .ToDictionary(g => $"position_{g.Key}", g => (float)g.Count() / recentActions.Length);
            
        foreach (var pattern in positionPreferences)
        {
            if (playerPatterns.ContainsKey(pattern.Key))
                playerPatterns[pattern.Key] = Mathf.Lerp(playerPatterns[pattern.Key], pattern.Value, learningRate);
            else
                playerPatterns[pattern.Key] = pattern.Value;
        }
        
        // 迷い時間パターンの学習
        float avgHoverTime = recentActions.Average(a => a.hoverTime);
        string hesitationLevel = avgHoverTime > 2.0f ? "high_hesitation" : "low_hesitation";
        
        if (playerPatterns.ContainsKey(hesitationLevel))
            playerPatterns[hesitationLevel] = Mathf.Lerp(playerPatterns[hesitationLevel], 1.0f, learningRate);
        else
            playerPatterns[hesitationLevel] = 0.5f;
    }
    
    private void AdaptPersonality()
    {
        // プレイヤーが迷いがちなら攻撃性を上げる
        if (playerPatterns.ContainsKey("high_hesitation") && playerPatterns["high_hesitation"] > 0.6f)
        {
            adaptivePersonality.aggression = Mathf.Min(1.0f, adaptivePersonality.aggression + learningRate * 0.5f);
        }
        
        // プレイヤーに強い偏りがあるなら適応度を上げる
        var maxPositionPref = playerPatterns
            .Where(p => p.Key.StartsWith("position_"))
            .Max(p => p.Value);
            
        if (maxPositionPref > 0.7f)
        {
            adaptivePersonality.intelligence = Mathf.Min(1.0f, adaptivePersonality.intelligence + learningRate * 0.3f);
        }
        
        // 学習結果をAIシステムに反映
        GetComponent<AIHandController>().UpdatePersonality(adaptivePersonality);
    }
    
    /// <summary>
    /// 学習データの可視化（デバッグ用）
    /// </summary>
    public string GetLearningReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("=== AI Learning Report ===");
        
        foreach (var pattern in playerPatterns.OrderByDescending(p => p.Value))
        {
            report.AppendLine($"{pattern.Key}: {pattern.Value:F2}");
        }
        
        report.AppendLine($"Current Aggression: {adaptivePersonality.aggression:F2}");
        report.AppendLine($"Current Intelligence: {adaptivePersonality.intelligence:F2}");
        
        return report.ToString();
    }
}

[System.Serializable]
public struct PlayerActionRecord
{
    public int position;
    public float hoverTime;
    public float timestamp;
    public bool wasCorrectGuess;
}
```

## セリフデータ拡張版

### 状況別セリフデータベース
```yaml
# プレイヤーカード引く前（AIの予測・挑発）
intimidation_pre:
  - "そのカード、君には重すぎる"
  - "選択を誤るな"
  - "後悔することになる"
  - "考え直すなら今だ"
  
misdirection_pre:
  - "そこが正解だ"
  - "迷うことはない"
  - "君の直感を信じろ"
  - "良い選択だ"
  
analysis_pre:
  - "君は右端を好む"
  - "その癖、見透かしている"
  - "3秒の迷い、興味深い"
  - "計算通りだ"

# AI がプレイヤーからカード引く前
ai_pre_draw:
  confidence_high:
    - "君のどれを選ぼうか"
    - "見せてもらう"
    - "隠し事はできない"
    
  confidence_low:
    - "運に任せるしかないようだ"
    - "どれも同じか"
    - "選択を迫られている"

# AI がカードを引いた後
ai_post_draw:
  got_good_card:
    - "期待通りだ"
    - "ありがとう"
    - "君らしい選択だった"
    
  got_bad_card:
    - "なるほど"
    - "想定の範囲だ"
    - "興味深い"
    
  got_joker:
    - "これが運命か"
    - "ゲームは続く"
    - "面白くなってきた"

# プレイヤーがジョーカーを引いた時
player_got_joker:
  - "哀れだな"
  - "見えていた結末だ"
  - "君の表情が全てを語っている"
  - "そう、それがジョーカーだ"

# ゲーム終盤
endgame_close:
  ai_winning:
    - "終わりが見えてきた"
    - "逃れられない"
    - "受け入れるしかない"
    
  player_winning:
    - "まだ終わりではない"
    - "最後まで分からない"
    - "油断は禁物だ"
    
# 勝利・敗北時
victory:
  ai_wins:
    - "当然の結果だ"
    - "君の負けだ"
    - "またの機会を楽しみにしている"
    
  player_wins:
    - "今度は君の勝ちか"
    - "なかなかやる"
    - "次は負けない"
```

この詳細な仕様により、AIは以下のように動作するニャ：

## 🤖 AI の完全な行動サイクル

1. **観察フェーズ**: プレイヤーの行動パターンを分析
2. **思考フェーズ**: 視覚的な思考演出（目の明滅、スキャン）
3. **心理圧フェーズ**: 適切なセリフで圧力をかける
4. **行動フェーズ**: 戦略的にカードを選択・引き抜き
5. **反応フェーズ**: 結果に応じた感情表現
6. **学習フェーズ**: 次回のために行動データを蓄積

これで **人間らしい知能と狡猾さを持ったAI** が完成するニャ！🎭✨