# パーソナリティ分析システム

## システム概要

プレイヤーの生年月日から **行動心理学プロファイル** を生成し、AIの心理圧戦術を個人に最適化するシステム。四柱推命・数秘術の理論を基盤としつつ、科学的・分析的な表現で実装。

## 設計思想

### 基本方針
- **占い的表現の完全排除** - 「運勢」「運命」等の用語は使用しない
- **科学的アプローチ** - 「行動パターン分析」「心理傾向算出」等の表現
- **ゲーム体験向上** - より個人に刺さる心理圧を実現
- **プライバシー配慮** - 生年月日はローカル保存のみ、分析結果も暗号化

### 表現ガイドライン
```yaml
❌ 避ける表現:
  - "運勢", "運命", "吉凶"
  - "霊的", "神秘的", "超自然"  
  - "占い", "予言", "未来予知"

✅ 使用する表現:
  - "行動パターン分析"
  - "心理傾向算出" 
  - "性格特性評価"
  - "判断スタイル分類"
  - "ストレス反応予測"
```

## UI/UX設計

### ゲーム開始時の入力フロー

#### 1. テーブル上の書類UI
```
シーン: Table上にクリップボード配置
Position: テーブル左端 (-1.5, 1.05, 0.3)
Rotation: 軽く斜めに配置 (0, 15, -2)

GameObject構成:
├── PersonalityInputForm
│   ├── Clipboard (3Dモデル)
│   │   ├── ClipboardBase (木製)
│   │   ├── ClipboardClip (金属クリップ)
│   │   └── FormPaper (紙のテクスチャ)
│   │
│   ├── UI Canvas (World Space)
│   │   ├── FormTitle (Text): "行動パターン分析のための基本情報入力"
│   │   ├── BirthDateFields
│   │   │   ├── YearField (InputField): "生年 (西暦)"
│   │   │   ├── MonthField (Dropdown): "月"  
│   │   │   └── DayField (Dropdown): "日"
│   │   │
│   │   ├── AnalysisNote (Text): "※ 個人の判断傾向を分析し、より良いゲーム体験を提供します"
│   │   ├── PrivacyNote (Text): "※ 入力データはローカルに保存され、外部送信されません"
│   │   └── ConfirmButton (Button): "分析開始"
│   │
│   └── HandwrittenPen (3Dモデル, アニメーション付き)
```

#### 2. 入力アニメーション演出
```csharp
public class PersonalityInputController : MonoBehaviour
{
    [Header("UI Components")]
    public Transform clipboard;
    public Canvas formCanvas;
    public TMP_InputField yearField;
    public TMP_Dropdown monthField;
    public TMP_Dropdown dayField;
    public Button confirmButton;
    
    [Header("3D Components")]
    public Transform handwrittenPen;
    public ParticleSystem inkParticles;
    
    public void OnFormAppear()
    {
        // クリップボードが滑らかに登場
        clipboard.transform.DOMoveY(1.1f, 0.8f).SetEase(Ease.OutBack);
        
        // フォーム項目が順次表示
        StartCoroutine(ShowFormFieldsSequentially());
    }
    
    private IEnumerator ShowFormFieldsSequentially()
    {
        // 各フィールドを0.3秒間隔で表示
        formCanvas.alpha = 0;
        formCanvas.DOFade(1, 0.5f);
        
        yield return new WaitForSeconds(0.3f);
        
        // ペンがフォームの上を移動（記入しているような動き）
        StartCoroutine(AnimatePenWriting());
    }
    
    private IEnumerator AnimatePenWriting()
    {
        Vector3[] penPositions = {
            yearField.transform.position + Vector3.up * 0.02f,
            monthField.transform.position + Vector3.up * 0.02f,
            dayField.transform.position + Vector3.up * 0.02f
        };
        
        foreach (Vector3 pos in penPositions)
        {
            handwrittenPen.DOMove(pos, 0.5f);
            
            // インクパーティクル開始
            inkParticles.Play();
            yield return new WaitForSeconds(0.8f);
            inkParticles.Stop();
            
            yield return new WaitForSeconds(0.2f);
        }
    }
}
```

## パーソナリティ分析ロジック

### PersonalityAnalyzer.cs
```csharp
using System;
using UnityEngine;

[System.Serializable]
public struct BirthData
{
    public int year;
    public int month;
    public int day;
    
    public DateTime GetDateTime() => new DateTime(year, month, day);
    public int GetAge() => DateTime.Now.Year - year;
}

[System.Serializable]
public struct PersonalityProfile
{
    [Header("Core Traits (0-1)")]
    public float cautionsness;      // 慎重性 (低=衝動的, 高=慎重)
    public float intuition;         // 直感性 (低=論理的, 高=直感的)
    public float resilience;        // 回復力 (低=プレッシャーに弱い, 高=強い)
    public float curiosity;         // 好奇心 (低=保守的, 高=挑戦的)
    public float consistency;       // 一貫性 (低=変化しやすい, 高=一定)
    
    [Header("Decision Making Style")]
    public DecisionStyle primaryStyle;
    public float confidence;        // 判断への自信度
    public float adaptability;      // 適応力
    
    [Header("Stress Response")]
    public StressType stressType;
    public float pressureTolerance; // プレッシャー耐性
    public float recoverySpeed;     // 回復速度
    
    [Header("Behavioral Patterns")]
    public string[] predictedBehaviors; // 予測される行動パターン
    public string[] weaknesses;         // AIが突くべき弱点
    public string[] strengths;          // プレイヤーの強み
}

public enum DecisionStyle
{
    Analytical,     // 分析的 - データを重視
    Intuitive,      // 直感的 - 第一印象重視  
    Cautious,       // 慎重派 - リスク回避
    Aggressive,     // 積極的 - リスク許容
    Adaptive        // 適応的 - 状況に応じて変化
}

public enum StressType
{
    Shutdown,       // 固まってしまう
    Impulsive,      // 衝動的になる
    Analytical,     // 過度に分析する
    Avoidant,       // 回避行動を取る
    Confrontational // 攻撃的になる
}

public class PersonalityAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    [SerializeField] private bool enableDetailedLogging = false;
    [SerializeField] private PersonalityDatabase database;
    
    /// <summary>
    /// 生年月日からパーソナリティプロファイルを生成
    /// </summary>
    public PersonalityProfile AnalyzeBirthData(BirthData birthData)
    {
        PersonalityProfile profile = new PersonalityProfile();
        
        // 四柱推命ベースの分析
        FourPillarsAnalysis fourPillars = CalculateFourPillars(birthData);
        
        // 数秘術ベースの分析
        NumerologyAnalysis numerology = CalculateNumerology(birthData);
        
        // 統合プロファイル生成
        profile = CombineAnalysisResults(fourPillars, numerology);
        
        // ログ出力（デバッグ用）
        if (enableDetailedLogging)
        {
            LogAnalysisResults(birthData, profile);
        }
        
        return profile;
    }
    
    private FourPillarsAnalysis CalculateFourPillars(BirthData birthData)
    {
        FourPillarsAnalysis analysis = new FourPillarsAnalysis();
        
        // 年柱（性格の基盤）
        analysis.yearPillar = CalculateYearPillar(birthData.year);
        
        // 月柱（対人関係・感情傾向）
        analysis.monthPillar = CalculateMonthPillar(birthData.month);
        
        // 日柱（核となる性格）
        analysis.dayPillar = CalculateDayPillar(birthData.day);
        
        // 五行バランス分析
        analysis.elementBalance = CalculateElementBalance(analysis);
        
        return analysis;
    }
    
    private ElementType CalculateYearPillar(int year)
    {
        // 年の下一桁と五行の対応
        int lastDigit = year % 10;
        
        return lastDigit switch
        {
            0 or 1 => ElementType.Metal,    // 金
            2 or 3 => ElementType.Water,    // 水
            4 or 5 => ElementType.Wood,     // 木
            6 or 7 => ElementType.Fire,     // 火
            8 or 9 => ElementType.Earth,    // 土
            _ => ElementType.Earth
        };
    }
    
    private ElementType CalculateMonthPillar(int month)
    {
        return month switch
        {
            1 or 2 or 12 => ElementType.Water,  // 冬（水）
            3 or 4 or 5 => ElementType.Wood,    // 春（木）
            6 or 7 or 8 => ElementType.Fire,    // 夏（火）
            9 or 10 or 11 => ElementType.Metal, // 秋（金）
            _ => ElementType.Earth
        };
    }
    
    private ElementType CalculateDayPillar(int day)
    {
        // 日付を5で割った余りで五行を決定
        return (ElementType)(day % 5);
    }
    
    private NumerologyAnalysis CalculateNumerology(BirthData birthData)
    {
        NumerologyAnalysis analysis = new NumerologyAnalysis();
        
        // ライフパスナンバー計算
        analysis.lifePathNumber = CalculateLifePathNumber(birthData);
        
        // パーソナリティナンバー（月日から）
        analysis.personalityNumber = ReduceToSingleDigit(birthData.month + birthData.day);
        
        // ソウルナンバー（年から）
        analysis.soulNumber = ReduceToSingleDigit(
            birthData.year.ToString().Select(c => int.Parse(c.ToString())).Sum()
        );
        
        return analysis;
    }
    
    private int CalculateLifePathNumber(BirthData birthData)
    {
        int yearSum = birthData.year.ToString().Select(c => int.Parse(c.ToString())).Sum();
        int totalSum = yearSum + birthData.month + birthData.day;
        
        return ReduceToSingleDigit(totalSum);
    }
    
    private int ReduceToSingleDigit(int number)
    {
        while (number > 9 && number != 11 && number != 22 && number != 33)
        {
            number = number.ToString().Select(c => int.Parse(c.ToString())).Sum();
        }
        return number;
    }
    
    private PersonalityProfile CombineAnalysisResults(
        FourPillarsAnalysis fourPillars, 
        NumerologyAnalysis numerology)
    {
        PersonalityProfile profile = new PersonalityProfile();
        
        // 基本特性の算出
        profile.cautiousness = CalculateCautiousness(fourPillars, numerology);
        profile.intuition = CalculateIntuition(fourPillars, numerology);
        profile.resilience = CalculateResilience(fourPillars, numerology);
        profile.curiosity = CalculateCuriosity(fourPillars, numerology);
        profile.consistency = CalculateConsistency(fourPillars, numerology);
        
        // 判断スタイルの決定
        profile.primaryStyle = DetermineDecisionStyle(profile);
        
        // ストレス反応の予測
        profile.stressType = DetermineStressType(profile);
        
        // 行動パターン予測
        profile.predictedBehaviors = GenerateBehaviorPredictions(profile);
        profile.weaknesses = IdentifyWeaknesses(profile);
        profile.strengths = IdentifyStrengths(profile);
        
        return profile;
    }
    
    private float CalculateCautiousness(FourPillarsAnalysis fourPillars, NumerologyAnalysis numerology)
    {
        float base = 0.5f;
        
        // 土の要素が強い = 慎重
        if (fourPillars.elementBalance.earth > 0.3f)
            base += 0.2f;
            
        // 水の要素が強い = 慎重  
        if (fourPillars.elementBalance.water > 0.3f)
            base += 0.15f;
            
        // ライフパスナンバー4, 6, 8 = 慎重
        if (numerology.lifePathNumber == 4 || 
            numerology.lifePathNumber == 6 || 
            numerology.lifePathNumber == 8)
            base += 0.1f;
            
        return Mathf.Clamp01(base);
    }
    
    private DecisionStyle DetermineDecisionStyle(PersonalityProfile profile)
    {
        // 最も高い特性に基づいて判断スタイルを決定
        if (profile.cautiousness > 0.7f)
            return DecisionStyle.Cautious;
        else if (profile.intuition > 0.7f)
            return DecisionStyle.Intuitive;
        else if (profile.curiosity > 0.7f)
            return DecisionStyle.Aggressive;
        else if (profile.consistency < 0.3f)
            return DecisionStyle.Adaptive;
        else
            return DecisionStyle.Analytical;
    }
    
    private string[] GenerateBehaviorPredictions(PersonalityProfile profile)
    {
        List<string> predictions = new List<string>();
        
        if (profile.cautiousness > 0.7f)
        {
            predictions.Add("カード選択前に長時間迷う傾向");
            predictions.Add("リスクの高い選択を避けがち");
        }
        
        if (profile.intuition > 0.7f)
        {
            predictions.Add("第一印象で素早く判断する");
            predictions.Add("パターンよりも直感を重視");
        }
        
        if (profile.resilience < 0.3f)
        {
            predictions.Add("連続的なプレッシャーに弱い");
            predictions.Add("失敗後の判断精度が低下しやすい");
        }
        
        return predictions.ToArray();
    }
}

// 分析結果を格納する構造体
[System.Serializable] 
public struct FourPillarsAnalysis
{
    public ElementType yearPillar;
    public ElementType monthPillar;
    public ElementType dayPillar;
    public ElementBalance elementBalance;
}

[System.Serializable]
public struct ElementBalance
{
    public float wood;   // 木
    public float fire;   // 火  
    public float earth;  // 土
    public float metal;  // 金
    public float water;  // 水
}

[System.Serializable]
public struct NumerologyAnalysis  
{
    public int lifePathNumber;
    public int personalityNumber;
    public int soulNumber;
}

public enum ElementType
{
    Wood = 0,   // 木 - 成長・柔軟性
    Fire = 1,   // 火 - 情熱・行動力
    Earth = 2,  // 土 - 安定・慎重さ
    Metal = 3,  // 金 - 集中・完璧主義
    Water = 4   // 水 - 適応・直感
}
```

## AI心理圧戦術への統合

### AIPersonalityAdaptor.cs
```csharp
public class AIPersonalityAdaptor : MonoBehaviour
{
    [Header("Adaptation Settings")]
    [SerializeField] private float adaptationStrength = 0.7f;
    [SerializeField] private PersonalityProfile playerProfile;
    
    private AIHandController aiController;
    private PsychologySystem psychologySystem;
    
    public void SetPlayerProfile(PersonalityProfile profile)
    {
        playerProfile = profile;
        AdaptAIBehavior();
    }
    
    private void AdaptAIBehavior()
    {
        // AI戦術の調整
        AdaptAIStrategy();
        
        // セリフ選択の調整
        AdaptDialogueSelection();
        
        // 圧力レベルの調整
        AdaptPressureTiming();
    }
    
    private void AdaptAIStrategy()
    {
        AIPersonality aiPersonality = aiController.GetPersonality();
        
        // プレイヤーの弱点を突く戦術に調整
        if (playerProfile.cautiousness > 0.7f)
        {
            // 慎重なプレイヤーには時間圧力
            aiPersonality.timePresssureMultiplier = 1.5f;
            aiPersonality.preferredTactic = AITactic.TimePressure;
        }
        else if (playerProfile.resilience < 0.3f)
        {
            // プレッシャーに弱いプレイヤーには連続攻撃
            aiPersonality.aggressionLevel += 0.2f;
            aiPersonality.preferredTactic = AITactic.ContinuousPressure;
        }
        
        aiController.UpdatePersonality(aiPersonality);
    }
    
    private void AdaptDialogueSelection()
    {
        DialogueDatabase database = psychologySystem.GetDialogueDatabase();
        
        // プレイヤータイプ別のセリフ重み調整
        switch (playerProfile.primaryStyle)
        {
            case DecisionStyle.Analytical:
                database.SetCategoryWeight("logical_pressure", 1.5f);
                database.SetCategoryWeight("data_confusion", 1.3f);
                break;
                
            case DecisionStyle.Intuitive:
                database.SetCategoryWeight("doubt_seeds", 1.5f);
                database.SetCategoryWeight("pattern_disruption", 1.3f);
                break;
                
            case DecisionStyle.Cautious:
                database.SetCategoryWeight("time_pressure", 1.6f);
                database.SetCategoryWeight("risk_emphasis", 1.4f);
                break;
        }
    }
    
    /// <summary>
    /// プレイヤーの行動を分析して、パーソナリティプロファイルを動的更新
    /// </summary>
    public void UpdateProfileFromBehavior(BehaviorData behavior)
    {
        // 実際の行動がプロファイル予測と異なる場合は調整
        if (behavior.avgHoverTime > 3.0f && playerProfile.cautiousness < 0.5f)
        {
            // 予測より慎重だった場合
            playerProfile.cautiousness = Mathf.Min(1.0f, playerProfile.cautiousness + 0.1f);
            Debug.Log("Player showing more caution than predicted - adjusting profile");
        }
        
        if (behavior.doubtLevel < 0.3f && playerProfile.confidence < 0.5f)
        {
            // 予測より自信があった場合
            playerProfile.confidence = Mathf.Min(1.0f, playerProfile.confidence + 0.1f);
            Debug.Log("Player showing more confidence than predicted - adjusting profile");
        }
        
        // 調整後のプロファイルでAI戦術を再調整
        AdaptAIBehavior();
    }
}
```

## セキュリティ・プライバシー

### データ保護実装
```csharp
public class PersonalityDataManager : MonoBehaviour
{
    private const string PROFILE_KEY = "encrypted_personality_profile";
    private const string ENCRYPTION_KEY = "fps_trump_personality_2026";
    
    /// <summary>
    /// パーソナリティプロファイルの暗号化保存
    /// </summary>
    public void SaveProfile(PersonalityProfile profile, BirthData birthData)
    {
        // 生年月日は保存しない（分析後は破棄）
        var saveData = new ProfileSaveData
        {
            profile = profile,
            creationDate = DateTime.Now,
            version = "1.0"
        };
        
        string jsonData = JsonUtility.ToJson(saveData);
        string encryptedData = SimpleEncrypt(jsonData, ENCRYPTION_KEY);
        
        PlayerPrefs.SetString(PROFILE_KEY, encryptedData);
        PlayerPrefs.Save();
        
        Debug.Log("Personality profile saved (encrypted, birth data discarded)");
    }
    
    /// <summary>
    /// 暗号化されたプロファイルの読み込み
    /// </summary>
    public PersonalityProfile LoadProfile()
    {
        if (!PlayerPrefs.HasKey(PROFILE_KEY))
            return new PersonalityProfile(); // デフォルトプロファイル
        
        string encryptedData = PlayerPrefs.GetString(PROFILE_KEY);
        string jsonData = SimpleDecrypt(encryptedData, ENCRYPTION_KEY);
        
        ProfileSaveData saveData = JsonUtility.FromJson<ProfileSaveData>(jsonData);
        
        return saveData.profile;
    }
    
    /// <summary>
    /// 簡易暗号化（XOR暗号）
    /// </summary>
    private string SimpleEncrypt(string text, string key)
    {
        StringBuilder result = new StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }
        
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
    }
    
    private string SimpleDecrypt(string encryptedText, string key)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        string text = Encoding.UTF8.GetString(data);
        
        StringBuilder result = new StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }
        
        return result.ToString();
    }
}

[System.Serializable]
private class ProfileSaveData
{
    public PersonalityProfile profile;
    public DateTime creationDate;
    public string version;
}
```

## パーソナライズされたセリフ例

### プレイヤータイプ別セリフデータベース
```yaml
# 慎重派プレイヤー向け（cautiousness > 0.7）
cautious_player_pressure:
  time_pressure:
    - "時間は有限だ"
    - "考えすぎは判断を鈍らせる"  
    - "決断の時だ"
    
  risk_emphasis:
    - "その選択にはリスクが伴う"
    - "安全な道などない"
    - "慎重すぎると機会を逃す"

# 直感派プレイヤー向け（intuition > 0.7）  
intuitive_player_pressure:
  doubt_seeds:
    - "その直感、本当に正しいか？"
    - "感情に流されているのでは？"
    - "データは別のことを示している"
    
  pattern_disruption:
    - "いつものパターンは通用しない"
    - "予想外の展開だ"
    - "直感を信じすぎるな"

# 回復力が低いプレイヤー向け（resilience < 0.3）
fragile_player_pressure:
  continuous_pressure:
    - "まだ続きがある"
    - "これで終わりではない"
    - "次はもっと難しくなる"
    
  confidence_attack:
    - "本当にそれでいいのか？"
    - "自信を失っているな"
    - "もう諦めた方がいい"

# 好奇心旺盛プレイヤー向け（curiosity > 0.7）
curious_player_pressure:
  mystery_bait:
    - "興味深い選択だ"
    - "その先に何があるか見てみたいな"
    - "もっと面白いものがあるかもしれない"
    
  challenge_provocation:
    - "それくらいで満足か？"
    - "本当のゲームはこれからだ"
    - "君にはまだ見えていない"
```

## 実装統合

### GameManagerとの統合
```csharp
// GameManager.cs に追加
[Header("🧠 Personality Analysis")]
[SerializeField] private PersonalityInputController personalityInput;
[SerializeField] private PersonalityAnalyzer personalityAnalyzer;
[SerializeField] private AIPersonalityAdaptor aiAdaptor;

private PersonalityProfile currentPlayerProfile;

private IEnumerator NewGameSequence()
{
    // 1. パーソナリティ入力フェーズ
    ChangeState(GameState.PersonalityInput);
    yield return StartCoroutine(CollectPersonalityData());
    
    // 2. 通常のゲーム開始処理
    ChangeState(GameState.Setup);
    yield return StartCoroutine(InitializeGameComponents());
    
    // 以下既存処理...
}

private IEnumerator CollectPersonalityData()
{
    // クリップボード登場
    personalityInput.ShowInputForm();
    
    // プレイヤー入力待ち
    bool inputCompleted = false;
    personalityInput.OnInputCompleted += (birthData) => {
        // パーソナリティ分析実行
        currentPlayerProfile = personalityAnalyzer.AnalyzeBirthData(birthData);
        
        // AI戦術調整
        aiAdaptor.SetPlayerProfile(currentPlayerProfile);
        
        inputCompleted = true;
    };
    
    // 入力完了まで待機
    yield return new WaitUntil(() => inputCompleted);
    
    // クリップボード退場
    personalityInput.HideInputForm();
}
```

この **パーソナリティ分析システム** により：

## 🧠 革新的な個人適応AI

✅ **科学的アプローチ** - 占い表現を完全排除  
✅ **四柱推命＋数秘術** - 伝統的理論の現代的活用  
✅ **個人最適化** - プレイヤー毎に異なる心理圧戦術  
✅ **プライバシー保護** - 生年月日は分析後破棄、結果暗号化  
✅ **動的学習** - 実行動でプロファイル調整  

これで **「このAIは私のことを理解している」** という驚愕体験が実現できるニャ！🎯✨