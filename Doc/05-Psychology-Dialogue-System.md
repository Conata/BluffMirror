# 心理圧・セリフシステム仕様

## システム概要

### 基本原則
- **ゲームロジックと分離**: セリフはゲーム結果に影響しない（演出のみ）
- **リアルタイム性**: ホバー時の即座反応（遅延<100ms）
- **心理的効果**: プレイヤーの判断に心理圧をかける
- **段階的圧力**: 行動パターンに応じて圧力レベル調整

### 心理圧の3層構造

#### 1. 近距離囁き（Whisper Layer）
- **位置**: プレイヤーの耳元（空間音響）
- **タイミング**: カード引く直前0.4秒間のみ
- **特徴**: 字幕なし または 極小字幕
- **目的**: 直感的な不安感を演出

#### 2. 空間投影（Projection Layer）  
- **位置**: ホバー中のカードの上に浮遊
- **表示**: 薄く滲む文字（透明度0.85）
- **特徴**: 微振動で読みにくくする
- **目的**: 選択への迷いを誘発

#### 3. HUDノイズ（Distortion Layer）
- **位置**: 画面全体（ポストエフェクト）
- **発動**: 同じ行動パターンの繰り返し時
- **特徴**: 画面端の赤滲み、断定的テキスト
- **目的**: 行動の癖を指摘

## 実装アーキテクチャ

### PsychologySystem.cs (メインコントローラー)
```csharp
public class PsychologySystem : MonoBehaviour
{
    [Header("Pressure Settings")]
    public float basePressureLevel = 0.0f;
    public float maxPressureLevel = 3.0f;
    public float pressureDecayRate = 0.1f;
    
    [Header("Components")]
    public WhisperSystem whisperSystem;
    public ProjectionSystem projectionSystem;
    public DistortionSystem distortionSystem;
    
    private PlayerBehaviorAnalyzer behaviorAnalyzer;
    private DialogueDatabase dialogueDB;
    private float currentPressureLevel;
    
    public UnityEvent<float> OnPressureLevelChanged;
    
    private void Start()
    {
        behaviorAnalyzer = GetComponent<PlayerBehaviorAnalyzer>();
        dialogueDB = Resources.Load<DialogueDatabase>("DialogueDatabase");
        
        // プレイヤー行動の監視開始
        behaviorAnalyzer.OnBehaviorAnalyzed += HandleBehaviorAnalysis;
    }
    
    private void Update()
    {
        // 圧力レベルの自然減衰
        if (currentPressureLevel > basePressureLevel)
        {
            currentPressureLevel = Mathf.Max(basePressureLevel, 
                currentPressureLevel - pressureDecayRate * Time.deltaTime);
            OnPressureLevelChanged?.Invoke(currentPressureLevel);
        }
    }
    
    private void HandleBehaviorAnalysis(BehaviorData behavior)
    {
        // 行動パターンに応じた圧力調整
        AdjustPressureLevel(behavior);
        
        // 適切なセリフの選択と実行
        ExecutePsychologicalResponse(behavior);
    }
    
    private void AdjustPressureLevel(BehaviorData behavior)
    {
        float pressureIncrease = 0f;
        
        // 同じ位置選択の連続
        if (behavior.streakSamePos >= 2)
            pressureIncrease += 0.5f * behavior.streakSamePos;
            
        // 優柔不断（ホバー時間が長い）
        if (behavior.avgHoverTime > 2.0f)
            pressureIncrease += 0.3f;
            
        // 疑念レベルが高い
        if (behavior.doubtLevel > 0.7f)
            pressureIncrease += 0.4f;
            
        currentPressureLevel = Mathf.Min(maxPressureLevel, 
            currentPressureLevel + pressureIncrease);
            
        OnPressureLevelChanged?.Invoke(currentPressureLevel);
    }
    
    private void ExecutePsychologicalResponse(BehaviorData behavior)
    {
        DialogueEntry entry = dialogueDB.GetAppropriateDialogue(behavior, currentPressureLevel);
        
        switch (entry.deliveryType)
        {
            case DialogueDeliveryType.Whisper:
                whisperSystem.PlayWhisper(entry);
                break;
                
            case DialogueDeliveryType.Projection:
                projectionSystem.ShowProjection(entry);
                break;
                
            case DialogueDeliveryType.Distortion:
                distortionSystem.TriggerDistortion(entry);
                break;
        }
    }
}
```

### PlayerBehaviorAnalyzer.cs
```csharp
[System.Serializable]
public struct BehaviorData
{
    public int streakSamePos;          // 同じ位置選択連続回数
    public float avgHoverTime;         // 平均ホバー時間
    public float doubtLevel;           // 疑念レベル (0-1)
    public TempoType tempo;            // 行動テンポ
    public int[] positionCounts;       // 位置別選択回数
    public float totalGameTime;        // 総ゲーム時間
}

public enum TempoType
{
    Slow,      // 慎重
    Normal,    // 普通  
    Fast,      // 焦り
    Erratic    // 不規則
}

public class PlayerBehaviorAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    public float behaviorWindowTime = 30f; // 行動分析の時間窓
    public int maxBehaviorHistory = 20;    // 保持する行動履歴数
    
    private Queue<PlayerAction> recentActions = new Queue<PlayerAction>();
    private BehaviorData currentBehavior;
    private int lastSelectedPosition = -1;
    private int consecutiveSamePos = 0;
    
    public UnityEvent<BehaviorData> OnBehaviorAnalyzed;
    
    public void RecordPlayerAction(int selectedPosition, float hoverDuration)
    {
        PlayerAction action = new PlayerAction
        {
            position = selectedPosition,
            hoverDuration = hoverDuration,
            timestamp = Time.time
        };
        
        recentActions.Enqueue(action);
        
        // 古いデータの削除
        while (recentActions.Count > maxBehaviorHistory)
            recentActions.Dequeue();
            
        // 行動分析の更新
        AnalyzeBehavior();
    }
    
    private void AnalyzeBehavior()
    {
        if (recentActions.Count < 3) return; // 最低3回の行動が必要
        
        AnalyzePositionPattern();
        AnalyzeHoverPattern();
        AnalyzeTempo();
        CalculateDoubtLevel();
        
        OnBehaviorAnalyzed?.Invoke(currentBehavior);
    }
    
    private void AnalyzePositionPattern()
    {
        currentBehavior.positionCounts = new int[3]; // Left, Center, Right
        consecutiveSamePos = 1;
        
        PlayerAction[] actions = recentActions.ToArray();
        
        // 位置別カウント
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i].position >= 0 && actions[i].position < 3)
                currentBehavior.positionCounts[actions[i].position]++;
        }
        
        // 連続同位置の検出
        for (int i = 1; i < actions.Length; i++)
        {
            if (actions[i].position == actions[i-1].position)
                consecutiveSamePos++;
            else
                break;
        }
        
        currentBehavior.streakSamePos = consecutiveSamePos;
    }
    
    private void AnalyzeHoverPattern()
    {
        float totalHoverTime = 0f;
        int validHoverCount = 0;
        
        foreach (PlayerAction action in recentActions)
        {
            if (action.hoverDuration > 0.1f) // 有効なホバーのみ
            {
                totalHoverTime += action.hoverDuration;
                validHoverCount++;
            }
        }
        
        currentBehavior.avgHoverTime = validHoverCount > 0 
            ? totalHoverTime / validHoverCount 
            : 0f;
    }
    
    private void AnalyzeTempo()
    {
        if (recentActions.Count < 2) return;
        
        PlayerAction[] actions = recentActions.ToArray();
        List<float> intervals = new List<float>();
        
        for (int i = 1; i < actions.Length; i++)
        {
            intervals.Add(actions[i].timestamp - actions[i-1].timestamp);
        }
        
        float avgInterval = intervals.Average();
        float variance = intervals.Select(x => Mathf.Pow(x - avgInterval, 2)).Average();
        float stdDev = Mathf.Sqrt(variance);
        
        // テンポ分類
        if (stdDev > avgInterval * 0.5f)
            currentBehavior.tempo = TempoType.Erratic;
        else if (avgInterval < 2.0f)
            currentBehavior.tempo = TempoType.Fast;
        else if (avgInterval > 8.0f)
            currentBehavior.tempo = TempoType.Slow;
        else
            currentBehavior.tempo = TempoType.Normal;
    }
    
    private void CalculateDoubtLevel()
    {
        float doubt = 0f;
        
        // ホバー時間が長い = 迷い
        if (currentBehavior.avgHoverTime > 3.0f)
            doubt += 0.4f;
            
        // 位置選択の偏り = 迷い
        int maxPosCount = currentBehavior.positionCounts.Max();
        int minPosCount = currentBehavior.positionCounts.Min();
        if (maxPosCount - minPosCount > 3)
            doubt += 0.3f;
            
        // 不規則テンポ = 迷い
        if (currentBehavior.tempo == TempoType.Erratic)
            doubt += 0.3f;
            
        currentBehavior.doubtLevel = Mathf.Clamp01(doubt);
    }
}

[System.Serializable]
public struct PlayerAction
{
    public int position;
    public float hoverDuration;
    public float timestamp;
}
```

### ProjectionSystem.cs (空間投影システム)
```csharp
public class ProjectionSystem : MonoBehaviour
{
    [Header("Projection Settings")]
    public GameObject projectionTextPrefab;
    public float projectionHeight = 0.08f;
    public float fadeInDuration = 0.12f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.3f;
    
    [Header("Text Effects")]
    public float vibrationIntensity = 0.002f;
    public float vibrationSpeed = 12f;
    
    private Dictionary<Transform, GameObject> activeProjections = new Dictionary<Transform, GameObject>();
    
    public void ShowProjection(DialogueEntry dialogueEntry)
    {
        Transform targetCard = GameManager.Instance.GetHoveredCard()?.transform;
        if (targetCard == null) return;
        
        // 既存の投影があれば削除
        if (activeProjections.ContainsKey(targetCard))
        {
            Destroy(activeProjections[targetCard]);
            activeProjections.Remove(targetCard);
        }
        
        // 新しい投影テキストを作成
        GameObject projectionObj = Instantiate(projectionTextPrefab);
        TextMeshPro textMesh = projectionObj.GetComponent<TextMeshPro>();
        
        // 位置設定（カードの少し上）
        Vector3 projectionPos = targetCard.position + Vector3.up * projectionHeight;
        projectionObj.transform.position = projectionPos;
        projectionObj.transform.LookAt(Camera.main.transform);
        projectionObj.transform.Rotate(0, 180, 0); // テキストを正面向きに
        
        // テキスト設定
        textMesh.text = dialogueEntry.text;
        textMesh.color = dialogueEntry.textColor;
        textMesh.fontSize = dialogueEntry.fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // アニメーション開始
        StartCoroutine(PlayProjectionAnimation(projectionObj, textMesh, targetCard));
        
        activeProjections[targetCard] = projectionObj;
    }
    
    private IEnumerator PlayProjectionAnimation(GameObject projectionObj, TextMeshPro textMesh, Transform targetCard)
    {
        Vector3 originalPos = projectionObj.transform.position;
        Color originalColor = textMesh.color;
        
        // フェードイン
        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(0, originalColor.a, t / fadeInDuration);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // 表示中（振動効果）
        float displayTimer = 0f;
        while (displayTimer < displayDuration)
        {
            displayTimer += Time.deltaTime;
            
            // 微振動
            Vector3 vibration = new Vector3(
                Mathf.Sin(Time.time * vibrationSpeed) * vibrationIntensity,
                Mathf.Sin(Time.time * vibrationSpeed * 1.3f) * vibrationIntensity * 0.5f,
                0
            );
            
            projectionObj.transform.position = originalPos + vibration;
            yield return null;
        }
        
        // フェードアウト
        for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(originalColor.a, 0, t / fadeOutDuration);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // クリーンアップ
        activeProjections.Remove(targetCard);
        Destroy(projectionObj);
    }
    
    public void ClearAllProjections()
    {
        foreach (var projection in activeProjections.Values)
        {
            if (projection != null)
                Destroy(projection);
        }
        activeProjections.Clear();
    }
}
```

## セリフデータベース

### DialogueDatabase.cs (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Game/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [Header("Dialogue Categories")]
    public DialogueCategory[] categories;
    
    public DialogueEntry GetAppropriateDialogue(BehaviorData behavior, float pressureLevel)
    {
        // 行動パターンに基づいてカテゴリ選択
        DialogueCategory category = SelectCategory(behavior, pressureLevel);
        
        // 圧力レベルに応じてエントリ選択
        var suitableEntries = category.entries.Where(e => 
            pressureLevel >= e.minPressureLevel && 
            pressureLevel <= e.maxPressureLevel).ToArray();
            
        if (suitableEntries.Length == 0)
            return category.entries[0]; // フォールバック
            
        return suitableEntries[Random.Range(0, suitableEntries.Length)];
    }
    
    private DialogueCategory SelectCategory(BehaviorData behavior, float pressureLevel)
    {
        // 連続同位置選択
        if (behavior.streakSamePos >= 2)
            return GetCategoryByType(DialogueCategoryType.Mirror);
            
        // 高疑念レベル
        if (behavior.doubtLevel > 0.6f)
            return GetCategoryByType(DialogueCategoryType.Stop);
            
        // 焦りテンポ
        if (behavior.tempo == TempoType.Fast)
            return GetCategoryByType(DialogueCategoryType.Bait);
            
        // デフォルト
        return GetCategoryByType(DialogueCategoryType.General);
    }
    
    private DialogueCategory GetCategoryByType(DialogueCategoryType type)
    {
        return categories.FirstOrDefault(c => c.type == type) ?? categories[0];
    }
}

[System.Serializable]
public class DialogueCategory
{
    public DialogueCategoryType type;
    public string categoryName;
    public DialogueEntry[] entries;
}

[System.Serializable]
public class DialogueEntry
{
    [Header("Content")]
    public string text;
    public AudioClip voiceClip;
    
    [Header("Delivery")]
    public DialogueDeliveryType deliveryType;
    public float minPressureLevel;
    public float maxPressureLevel;
    
    [Header("Visual")]
    public Color textColor = Color.white;
    public float fontSize = 2.5f;
    
    [Header("Audio")]
    public float volume = 0.8f;
    public float pitch = 1.0f;
}

public enum DialogueCategoryType
{
    Stop,     // 止める
    Bait,     // 釣る・煽る
    Mirror,   // 癖を指摘
    General   // 一般的圧力
}

public enum DialogueDeliveryType
{
    Whisper,    // 囁き
    Projection, // 空間投影
    Distortion  // 画面歪み
}
```

### セリフ例データ

#### Stop（止める）カテゴリ
```yaml
Stop_Low_Pressure:
  - "そっちじゃない"
  - "手が止まったな"
  - "考え直せ"
  
Stop_Medium_Pressure:
  - "それを選ぶべきではない"
  - "今の判断は間違っている"
  - "やめておけ"
  
Stop_High_Pressure:
  - "その選択は破滅だ"
  - "引き返せ、まだ間に合う"
  - "愚かな選択をするな"
```

#### Bait（釣る）カテゴリ
```yaml
Bait_Low_Pressure:
  - "そこだ"
  - "いい選択だ"
  - "その調子だ"
  
Bait_Medium_Pressure:
  - "選べ。君なら選べる"
  - "勇気を出せ"
  - "恐れることはない"
  
Bait_High_Pressure:
  - "その度胸、見せてもらおう"
  - "逃げるのか？"
  - "今しかない"
```

#### Mirror（癖指摘）カテゴリ
```yaml
Mirror_Pattern:
  - "また端だ"
  - "癖が出た"
  - "変えられない"
  - "君はいつも同じだ"
  - "読みやすすぎる"
  - "その癖は見えている"
```

## パフォーマンス考慮

### 最適化戦略
- **セリフキャッシュ**: 使用頻度の高いセリフを事前読み込み
- **音声圧縮**: OGGフォーマットで高圧縮率
- **テキスト表示**: Object Pooling適用
- **行動分析**: フレーム分散処理

### メモリ管理
- **音声メモリ**: 最大50MB制限
- **テキストテクスチャ**: 動的生成・破棄
- **履歴データ**: 定期的なクリーンアップ

---
**Document Version**: 1.0  
**Last Updated**: 2026-02-07  
**Status**: Core system specification