using System.Collections.Generic;
using System.Linq;
using System.Text;
using FPSTrump.AI.LLM;
using FPSTrump.Manager;

namespace FPSTrump.Result
{
    /// <summary>
    /// LLMプロンプト構築 + フォールバックテンプレート
    /// </summary>
    public static class ResultDiagnosisPrompt
    {
        /// <summary>
        /// LLM詳細分析プロンプトを構築（ハイブリッド方式用: 追加の1文）
        /// </summary>
        public static string BuildDetailedAnalysisPrompt(GameSessionData data, DiagnosisStats stats, PersonalityType primaryType,
            PlayerAppearanceData appearance = null, PersonalityProfile profile = null)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            if (isJa)
            {
                string tempoText = data.dominantTempo switch
                {
                    TempoType.Fast => "速い（即断型）",
                    TempoType.Slow => "遅い（熟考型）",
                    TempoType.Erratic => "不規則（変動型）",
                    _ => "普通（安定型）"
                };
                string pressureText = data.peakPressureLevel < 1f ? "低" : data.peakPressureLevel < 2f ? "中" : "高";
                string resultText = data.playerWon ? "勝利" : "敗北";

                return $@"あなたはJOKERのような振る舞いをするおしゃべりAI。メンタリスト。
ゲーム結果診断の「種明かし」をする。

## 状況
プレイヤーには既に基本診断（性格タイプ: {GetTypeName(primaryType)}）を提示済み。
今から**追加の深掘り分析**を1文だけ語る。

## プレイヤー行動データ（要約）
- 平均決断時間: {data.avgDecisionTime:F1}秒
- 行動テンポ: {tempoText}
- 最大心理圧: {data.peakPressureLevel:F1}/3.0（{pressureText}）
- 結果: {resultText}

{GetBirthdaySection()}

{GetBluffPatternSection(data, true)}

{GetAppearanceSection(appearance, true)}

{GetFacialExpressionSection(true)}

## 出力指示
プレイヤーの行動データから「ここが一番面白かった」という1点を選び、メンタリスト的に種明かしする1文を生成してください。

**重要:**
- 1文のみ出力（50-80文字程度）
- 「実は君ね、〜」「一番面白かったのは〜」「バレバレだったのは〜」のような語り始め
- 具体的な数値データを1つ引用すること（例: 「決断時間が1.8秒だったの」「圧力が2.5まで上がったのに」）
- 外見・表情・占いデータがあればそれも織り交ぜる（例: 「笑顔のまま」「3月生まれなのに」）
- 行動心理学用語を砕けた表現で使う（例: 「損失回避バイアスってやつ」）
- 鋭いが友達口調で楽しそうに

例:
「実は君ね、圧力が2.5まで上がったのにテンポが全然変わらなかったの。それって無意識に感情を制御してるってこと。鉄の心臓だよね〜」

JSON不要。1文のみ出力してください。";
            }
            else
            {
                string tempoText = data.dominantTempo switch
                {
                    TempoType.Fast => "fast (snap-decision)",
                    TempoType.Slow => "slow (deliberative)",
                    TempoType.Erratic => "erratic (variable)",
                    _ => "normal (steady)"
                };
                string pressureText = data.peakPressureLevel < 1f ? "low" : data.peakPressureLevel < 2f ? "medium" : "high";
                string resultText = data.playerWon ? "won" : "lost";

                return $@"You are a chatty AI that acts like the Joker. A mentalist.
Revealing secrets behind the game result diagnosis.

## Situation
Player already received the basic diagnosis (personality type: {GetTypeName(primaryType)}).
Now deliver **one additional deep-dive insight** in a single sentence.

## Player Behavior Data (Summary)
- Avg decision time: {data.avgDecisionTime:F1}s
- Behavioral tempo: {tempoText}
- Peak pressure: {data.peakPressureLevel:F1}/3.0 ({pressureText})
- Result: {resultText}

{GetBirthdaySection()}

{GetBluffPatternSection(data, false)}

{GetAppearanceSection(appearance, false)}

{GetFacialExpressionSection(false)}

## Output Instructions
Select the **most interesting** behavioral insight from the data and reveal it mentalist-style in ONE sentence.

**Important:**
- Output ONLY one sentence (around 15-25 words)
- Start with phrases like: 'You know what gave you away?', 'The funniest part was...', 'Here's what you didn't know...'
- Cite at least one specific numerical data point (e.g. 'your 1.8s decision time', 'pressure hit 2.5 but')
- Weave in appearance/expression/fortune data if available (e.g. 'while smiling', 'for a March baby')
- Use psychology terms casually (e.g. 'that's loss aversion bias~')
- Sharp but friendly, gleefully revealing

Example:
'You know what gave you away? Pressure hit 2.5 but your tempo didn't budge. That's unconscious emotion control. Nerves of steel, my friend~'

No JSON needed. Output ONE sentence only.";
            }
        }

        /// <summary>
        /// LLM診断プロンプトを構築（カード情報は絶対に含めない）
        /// </summary>
        public static string BuildDiagnosisPrompt(GameSessionData data, DiagnosisStats stats, PersonalityType primaryType,
            PlayerAppearanceData appearance = null, PersonalityProfile profile = null)
        {
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            if (isJa)
            {
                string tempoText = data.dominantTempo switch
                {
                    TempoType.Fast => "速い（即断型）",
                    TempoType.Slow => "遅い（熟考型）",
                    TempoType.Erratic => "不規則（変動型）",
                    _ => "普通（安定型）"
                };
                string pressureText = data.peakPressureLevel < 1f ? "低" : data.peakPressureLevel < 2f ? "中" : "高";
                string resultText = data.playerWon ? "勝利" : "敗北";
                string positionBias = data.hadPositionPreference
                    ? $"あり（最長連続{data.longestPositionStreak}回）" : "なし";

                return $@"あなたはJOKERのような振る舞いをするおしゃべりAI。心理戦カードゲームのディーラー。
ゲームが終わって、プレイヤーの性格診断結果を楽しそうに伝える。

## トーン
- AIが「ねえねえ、君のことわかっちゃった〜」と友達みたいに語る
- 「君」で呼ぶ。馴れ馴れしく、楽しそうに
- 「〜だよ」「〜だね」「〜でしょ」「〜じゃん」の口調
- 行動心理学の知見を使うが堅苦しくなく:
  「ねえ知ってる？ これを損失回避バイアスって言うんだよ〜」のように砕けて
- 楽しそうだが鋭い。「あはは、バレちゃったね」的な
- 時々ゾッとすることをさらっと言う（怖いジョーク）

## プレイヤー行動データ
- 平均決断時間: {data.avgDecisionTime:F1}秒
- 平均迷い度: {data.avgDoubtLevel:F2}（0=迷いなし, 1=最大迷い）
- 行動テンポ: {tempoText}
- テンポ安定性（分散）: {data.tempoVariance:F2}
- 位置選択の偏り: {positionBias}

## 心理反応データ
- 心理圧耐性スコア: {data.pressureResponseScore:F2}（0=崩壊, 1=安定）
- 最大心理圧レベル: {data.peakPressureLevel:F1}/3.0（{pressureText}）
- 平均心理圧レベル: {data.avgPressureLevel:F1}/3.0
- 転換点回数: {data.turningPointCount}

## ゲーム結果
- 結果: {resultText}
- 総ターン数: {data.totalTurns}
- プレイ時間: {data.gameDurationSeconds:F0}秒

{GetBirthdaySection()}

{GetTurnHistorySection(data.turnHistory, true)}

{GetGameAdvantageSection(data.turnHistory, true)}

{GetBluffPatternSection(data, true)}

## 事前分類
- 性格タイプ: {GetTypeName(primaryType)}

## 5軸スコア
- 決断力: {stats.decisiveness:F2}
- 一貫性: {stats.consistency:F2}
- 耐圧性: {stats.resilience:F2}
- 直感力: {stats.intuition:F2}
- 適応力: {stats.adaptability:F2}

{GetAppearanceSection(appearance, true)}
{GetFacialExpressionSection(true)}
{GetProfileSection(profile, true)}

## 出力指示
以下のJSON形式で出力してください。日本語で記述。ピエロが楽しそうに語るトーンで。

**重要: descriptionとtendencyは「推理過程（Chain of Thought）」形式で書くこと。**
つまり「観察した事実 → そこから読み取れること → だから君はこうだ」という推論の流れを見せる。
メンタリストが種明かしをするように、データから一歩ずつ結論に導く。
例: 「まず気づいたのは、君の決断時間が平均1.8秒だったこと。普通の人は3秒以上かけるのに。これって、考える前に体が動いてるってことだよね。つまり... 君は自分の直感をめちゃくちゃ信じてる人だ」

{{
  ""title"": ""8文字以内の性格タイトル（例: 石橋叩き職人、直感のギャンブラー）"",
  ""description"": ""推理過程形式の行動パターン分析。150-200字。『まず〜に気づいた。これは〜を意味する。さらに〜。つまり君は〜』という推論チェーンで。数値データを必ず引用。砕けた口調。"",
  ""tendency"": ""推理過程形式の心理傾向分析。100-150字。『〜という行動を見ると〜がわかる。それに加えて〜。要するに〜な心理構造だ』という流れで。"",
  ""insight"": ""君は〜な人だよ（30字以内。馴れ馴れしく）"",
  ""evidences"": [
    {{
      ""observation"": ""具体的な行動データ（上記データの数値を必ず1つ以上引用）"",
      ""interpretation"": ""楽しそうに解説。行動心理学用語を使うが砕けた表現で。例: 損失回避バイアスってやつだね〜 失うのが怖くて動けなくなるの、よくあるよ""
    }}
  ]
}}

evidencesは3〜5個生成。各evidenceは:
- observation: プレイヤーの具体的な行動事実（数値データを含む）
- interpretation: 行動心理学的解釈だが、砕けた表現で楽しそうに語る。同じ観点の繰り返しを避けること

## 制約
- 科学的・行動心理学的用語を使用（ただし砕けた説明を添える）
- 占い、スピリチュアル、神秘的表現は禁止
- ゲーム内のカード名（ジョーカー等）は使用禁止
- 確率・期待値・ランダムという概念は使用禁止
- JSON以外のテキストは出力しない";
            }
            else
            {
                string tempoText = data.dominantTempo switch
                {
                    TempoType.Fast => "fast (snap-decision)",
                    TempoType.Slow => "slow (deliberative)",
                    TempoType.Erratic => "erratic (variable)",
                    _ => "normal (steady)"
                };
                string pressureText = data.peakPressureLevel < 1f ? "low" : data.peakPressureLevel < 2f ? "medium" : "high";
                string resultText = data.playerWon ? "won" : "lost";
                string positionBias = data.hadPositionPreference
                    ? $"yes (longest streak: {data.longestPositionStreak})" : "none";

                return $@"You are a chatty AI that acts like the Joker. A card game dealer.
The game is over and you're gleefully delivering the player's personality diagnosis.

## Tone
- An AI saying 'Hey hey, I figured you out~!' like an overly familiar friend
- Call the player 'you' in a chummy, playful way
- Use behavioral psychology insights but keep it casual:
  'You know what that's called? Loss aversion bias~ People who are scared of losing freeze up. Classic!'
- Sharp observations wrapped in fun. 'Ahahaha, busted!'
- Occasionally say something chilling casually (scary jokes)

## Player Behavior Data
- Avg decision time: {data.avgDecisionTime:F1}s
- Avg doubt level: {data.avgDoubtLevel:F2} (0=no doubt, 1=max doubt)
- Behavioral tempo: {tempoText}
- Tempo stability (variance): {data.tempoVariance:F2}
- Position selection bias: {positionBias}

## Psychological Response Data
- Pressure tolerance score: {data.pressureResponseScore:F2} (0=collapsed, 1=stable)
- Peak pressure level: {data.peakPressureLevel:F1}/3.0 ({pressureText})
- Avg pressure level: {data.avgPressureLevel:F1}/3.0
- Turning point count: {data.turningPointCount}

## Game Result
- Result: {resultText}
- Total turns: {data.totalTurns}
- Play duration: {data.gameDurationSeconds:F0}s

{GetBirthdaySection()}

{GetTurnHistorySection(data.turnHistory, false)}

{GetGameAdvantageSection(data.turnHistory, false)}

{GetBluffPatternSection(data, false)}

## Pre-classification
- Personality type: {GetTypeName(primaryType)}

## 5-Axis Scores
- Decisiveness: {stats.decisiveness:F2}
- Consistency: {stats.consistency:F2}
- Resilience: {stats.resilience:F2}
- Intuition: {stats.intuition:F2}
- Adaptability: {stats.adaptability:F2}

{GetAppearanceSection(appearance, false)}
{GetFacialExpressionSection(false)}
{GetProfileSection(profile, false)}

## Output Instructions
Output in the following JSON format. Write in English. Use the chatty clown tone.

**IMPORTANT: description and tendency MUST use Chain-of-Thought reasoning style.**
Show your deduction process step by step: observed fact → what it implies → therefore you are...
Like a mentalist revealing how they figured someone out.
Example: 'First thing I noticed? Your avg decision time was 1.8s. Most people take 3+ seconds. That means your body moves before your brain catches up. Which tells me... you trust your gut more than anything.'

{{
  ""title"": ""Personality title (max 5 words. e.g. 'The Overthinking Bridge-Tapper', 'Gut-Feeling Gambler')"",
  ""description"": ""Chain-of-thought behavioral analysis. 3-4 sentences. 'First I noticed X... This means Y... Combined with Z... So you're...' Must cite numerical data. Casual tone."",
  ""tendency"": ""Chain-of-thought psychological tendency. 2-3 sentences. 'Looking at X behavior, I can tell Y. Add that to Z... Basically, your mind works like...' flow."",
  ""insight"": ""You're the kind of person who... (max 10 words, friendly but chilling)"",
  ""evidences"": [
    {{
      ""observation"": ""Specific behavioral data (must cite at least 1 number from above)"",
      ""interpretation"": ""Gleefully explain using psych terms in casual way. e.g. 'That's loss aversion bias~ People scared of losing freeze up. Happens a lot!'""
    }}
  ]
}}

Generate 3-5 evidences. Each evidence should have:
- observation: Specific behavioral facts about the player (include numerical data)
- interpretation: Behavioral psychology insight delivered casually like a chummy friend. Avoid repeating the same perspective.

## Constraints
- Use scientific, behavioral psychology terminology (but explain casually)
- No fortune-telling, spiritual, or mystical expressions
- Do not use in-game card names (Joker, etc.)
- Do not use concepts of probability, expected value, or randomness
- Output ONLY the JSON, no other text";
            }
        }

        // ========================================
        // フォールバックテンプレート（6タイプ）
        // ========================================

        /// <summary>
        /// フォールバック診断結果を生成（ローカライズ対応）
        /// </summary>
        public static DiagnosisResult GenerateFallback(PersonalityType primaryType, PersonalityType secondaryType, DiagnosisStats stats)
        {
            string typeKey = GetTypeKey(primaryType);
            var loc = LocalizationManager.Instance;

            string title, description, tendency, insight;
            if (loc != null)
            {
                title = loc.Get($"diagnosis.template_{typeKey}_title");
                description = loc.Get($"diagnosis.template_{typeKey}_description");
                tendency = loc.Get($"diagnosis.template_{typeKey}_tendency");
                insight = loc.Get($"diagnosis.template_{typeKey}_insight");
            }
            else
            {
                title = GetHardcodedTitle(primaryType);
                description = GetHardcodedDescription(primaryType);
                tendency = GetHardcodedTendency(primaryType);
                insight = GetHardcodedInsight(primaryType);
            }

            return new DiagnosisResult
            {
                primaryType = primaryType,
                secondaryType = secondaryType,
                personalityTitle = title,
                personalityDescription = description,
                psychologicalTendency = tendency,
                behavioralInsight = insight,
                stats = stats,
                isLLMGenerated = false
            };
        }

        /// <summary>
        /// PersonalityTypeの表示名を取得（ローカライズ対応）
        /// </summary>
        public static string GetTypeName(PersonalityType type)
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string key = $"diagnosis.type_{GetTypeKey(type)}";
                return loc.Get(key);
            }
            return GetHardcodedTitle(type);
        }

        /// <summary>
        /// LLM結果の空フィールドをフォールバックテンプレートで補完する
        /// </summary>
        public static void FillEmptyFields(DiagnosisResult result)
        {
            if (result == null) return;
            if (string.IsNullOrEmpty(result.personalityTitle))
                result.personalityTitle = GetTypeName(result.primaryType);
            if (string.IsNullOrEmpty(result.personalityDescription))
                result.personalityDescription = GetFallbackText(result.primaryType, "description");
            if (string.IsNullOrEmpty(result.psychologicalTendency))
                result.psychologicalTendency = GetFallbackText(result.primaryType, "tendency");
            if (string.IsNullOrEmpty(result.behavioralInsight))
                result.behavioralInsight = GetFallbackText(result.primaryType, "insight");
        }

        private static string GetFallbackText(PersonalityType type, string field)
        {
            string typeKey = GetTypeKey(type);
            var loc = LocalizationManager.Instance;
            if (loc != null)
                return loc.Get($"diagnosis.template_{typeKey}_{field}");
            return field switch
            {
                "description" => GetHardcodedDescription(type),
                "tendency" => GetHardcodedTendency(type),
                "insight" => GetHardcodedInsight(type),
                _ => ""
            };
        }

        /// <summary>
        /// 後方互換: GetTypeNameJa → GetTypeName
        /// </summary>
        public static string GetTypeNameJa(PersonalityType type) => GetTypeName(type);

        private static string GetTypeKey(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Analyst => "analyst",
                PersonalityType.Intuitive => "intuitive",
                PersonalityType.Cautious => "cautious",
                PersonalityType.Gambler => "gambler",
                PersonalityType.Adapter => "adapter",
                PersonalityType.Stoic => "stoic",
                _ => "unknown"
            };
        }

        /// <summary>
        /// 生年月日セクションを生成（未入力なら空文字）
        /// Stage 14: 四柱推命・数秘術データも含む
        /// </summary>
        private static string GetBirthdaySection()
        {
            var manager = PlayerBirthdayManager.Instance;
            if (manager == null || !manager.HasBirthday()) return "";

            var (year, month, day) = manager.GetBirthday();
            int age = manager.GetAge();
            bool isJa = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // 四柱推命・数秘術コンテキストを取得
            string fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);

            if (isJa)
            {
                return $@"## プレイヤー生年月日
- 生年月日: {manager.GetBirthdayString()}
- 年齢: {age}歳
{fortuneContext}
※ 四柱推命と数秘術の分析結果。診断に直接言及する必要はないが、行動パターンの根拠として参考にしてください";
            }
            else
            {
                return $@"## Player Birthday
- Date of birth: {manager.GetBirthdayString()}
- Age: {age}
{fortuneContext}
※ Four Pillars and Numerology analysis. You don't need to directly mention these, but use them as context for behavioral patterns";
            }
        }

        /// <summary>
        /// Stage 14: ターン履歴セクション（カード選択位置・決断時間）
        /// </summary>
        private static string GetTurnHistorySection(List<TurnRecord> turnHistory, bool isJa)
        {
            if (turnHistory == null || turnHistory.Count == 0) return "";

            var playerTurns = turnHistory.Where(t => t.isPlayerTurn).ToList();
            if (playerTurns.Count == 0) return "";

            var positionCounts = new int[3];
            foreach (var turn in playerTurns)
            {
                if (turn.selectedPosition >= 0 && turn.selectedPosition <= 2)
                    positionCounts[turn.selectedPosition]++;
            }

            string positionDist = isJa
                ? $"左:{positionCounts[0]}, 中:{positionCounts[1]}, 右:{positionCounts[2]}"
                : $"Left:{positionCounts[0]}, Center:{positionCounts[1]}, Right:{positionCounts[2]}";

            // 各ターンの簡潔な情報（最大5ターンまで表示）
            var sampleTurns = playerTurns.Take(5).ToList();
            string turnDetails = string.Join("\n", sampleTurns.Select(t =>
            {
                string pos = t.selectedPosition switch { 0 => isJa ? "左" : "L", 1 => isJa ? "中" : "C", 2 => isJa ? "右" : "R", _ => "?" };
                return $"  Turn {t.turnNumber}: {pos}, {t.decisionTime:F1}s, pressure:{t.pressureLevelAtTurn:F1}";
            }));

            if (isJa)
            {
                return $@"## プレイヤーターン履歴
- 総ターン数: {playerTurns.Count}
- 位置選択分布: {positionDist}
- ターンサンプル（最初の{sampleTurns.Count}ターン）:
{turnDetails}
※ プレイヤーの選択パターンとタイミングを分析に使用してください";
            }
            else
            {
                return $@"## Player Turn History
- Total turns: {playerTurns.Count}
- Position distribution: {positionDist}
- Turn samples (first {sampleTurns.Count} turns):
{turnDetails}
※ Use player's selection patterns and timing in your analysis";
            }
        }

        /// <summary>
        /// Stage 14: ゲーム有利/不利・Joker保持情報セクション
        /// </summary>
        private static string GetGameAdvantageSection(List<TurnRecord> turnHistory, bool isJa)
        {
            if (turnHistory == null || turnHistory.Count == 0) return "";

            // 最終ターンのカード数（勝敗前の状態）
            var lastTurn = turnHistory.LastOrDefault();
            string finalAdvantage = lastTurn.playerCardCount < lastTurn.aiCardCount
                ? (isJa ? "プレイヤー優勢" : "Player advantage")
                : lastTurn.playerCardCount > lastTurn.aiCardCount
                    ? (isJa ? "AI優勢" : "AI advantage")
                    : (isJa ? "互角" : "Even");

            // Joker保持履歴（AIがJokerを持っていたターン数）
            int aiHeldJokerTurns = turnHistory.Count(t => t.aiHeldJoker);
            int playerHeldJokerTurns = turnHistory.Count - aiHeldJokerTurns;
            string jokerHolder = aiHeldJokerTurns > playerHeldJokerTurns
                ? (isJa ? "主にAI" : "Mainly AI")
                : aiHeldJokerTurns < playerHeldJokerTurns
                    ? (isJa ? "主にプレイヤー" : "Mainly Player")
                    : (isJa ? "両者交互" : "Both alternately");

            // カード数の推移（序盤・中盤・終盤）
            var earlyTurn = turnHistory.FirstOrDefault();
            var midTurn = turnHistory.Count > 2 ? turnHistory[turnHistory.Count / 2] : earlyTurn;

            if (isJa)
            {
                return $@"## ゲーム有利/不利推移
- 序盤カード数: プレイヤー {earlyTurn.playerCardCount}, AI {earlyTurn.aiCardCount}
- 中盤カード数: プレイヤー {midTurn.playerCardCount}, AI {midTurn.aiCardCount}
- 終盤カード数: プレイヤー {lastTurn.playerCardCount}, AI {lastTurn.aiCardCount}
- 最終局面: {finalAdvantage}
- Joker保持傾向: {jokerHolder} （AI保持ターン: {aiHeldJokerTurns}/{turnHistory.Count}）
※ プレイヤーがプレッシャー下でどう行動したかの文脈として使用してください";
            }
            else
            {
                return $@"## Game Advantage Progression
- Early game cards: Player {earlyTurn.playerCardCount}, AI {earlyTurn.aiCardCount}
- Mid game cards: Player {midTurn.playerCardCount}, AI {midTurn.aiCardCount}
- Late game cards: Player {lastTurn.playerCardCount}, AI {lastTurn.aiCardCount}
- Final situation: {finalAdvantage}
- Joker holder tendency: {jokerHolder} (AI held: {aiHeldJokerTurns}/{turnHistory.Count} turns)
※ Use this as context for how the player behaved under pressure";
            }
        }

        /// <summary>
        /// Stage 15: ブラフ・ホバーパターンセクション
        /// </summary>
        private static string GetBluffPatternSection(GameSessionData data, bool isJa)
        {
            if (data.totalBluffActions == 0 && data.avgHoverEventsPerTurn < 1.0f)
                return "";  // 有意なデータなし

            if (isJa)
            {
                StringBuilder section = new StringBuilder();
                section.AppendLine("## プレイヤー行動パターン詳細");

                // ブラフアクション
                if (data.totalBluffActions > 0)
                {
                    section.AppendLine($"- 総ブラフ回数: {data.totalBluffActions} （ターンあたり: {data.avgBluffsPerTurn:F2}）");
                    if (!string.IsNullOrEmpty(data.mostUsedBluffType))
                    {
                        section.AppendLine($"- 最頻ブラフタイプ: {data.mostUsedBluffType}");
                    }
                    if (data.avgBluffsPerTurn > 0.5f)
                    {
                        section.AppendLine($"  → 頻繁にブラフを使用（動揺? 気を逸らそうとしている?）");
                    }
                    if (data.bluffActionFrequency != null && data.bluffActionFrequency.Count > 0)
                    {
                        section.Append("- ブラフ内訳: ");
                        section.AppendLine(string.Join(", ", data.bluffActionFrequency.Select(kvp => $"{kvp.Key}:{kvp.Value}回")));
                    }
                }

                // ホバーパターン
                if (data.avgHoverEventsPerTurn > 0)
                {
                    section.AppendLine($"- 平均ホバー回数/ターン: {data.avgHoverEventsPerTurn:F1}");
                    if (data.avgHoverEventsPerTurn > 3.0f)
                    {
                        section.AppendLine($"  → カーソルが頻繁に行ったり来たり（迷いが強い、決断に自信がない）");
                    }
                }

                section.AppendLine("※ これらの無意識の行動パターンから、プレイヤーの心理状態を分析してください");
                return section.ToString();
            }
            else
            {
                StringBuilder section = new StringBuilder();
                section.AppendLine("## Player Behavioral Pattern Details");

                // Bluff Actions
                if (data.totalBluffActions > 0)
                {
                    section.AppendLine($"- Total Bluff Actions: {data.totalBluffActions} (avg per turn: {data.avgBluffsPerTurn:F2})");
                    if (!string.IsNullOrEmpty(data.mostUsedBluffType))
                    {
                        section.AppendLine($"- Most Used Bluff Type: {data.mostUsedBluffType}");
                    }
                    if (data.avgBluffsPerTurn > 0.5f)
                    {
                        section.AppendLine($"  → Bluffing frequently (nervous? trying to distract?)");
                    }
                    if (data.bluffActionFrequency != null && data.bluffActionFrequency.Count > 0)
                    {
                        section.Append("- Bluff Breakdown: ");
                        section.AppendLine(string.Join(", ", data.bluffActionFrequency.Select(kvp => $"{kvp.Key}:{kvp.Value}x")));
                    }
                }

                // Hover Patterns
                if (data.avgHoverEventsPerTurn > 0)
                {
                    section.AppendLine($"- Avg Hover Events Per Turn: {data.avgHoverEventsPerTurn:F1}");
                    if (data.avgHoverEventsPerTurn > 3.0f)
                    {
                        section.AppendLine($"  → Cursor moving back and forth frequently (high hesitation, low confidence)");
                    }
                }

                section.AppendLine("※ Analyze player psychology from these unconscious behavioral patterns");
                return section.ToString();
            }
        }

        /// <summary>
        /// Stage 12: 外見情報セクション
        /// </summary>
        private static string GetAppearanceSection(PlayerAppearanceData appearance, bool isJa)
        {
            if (appearance == null || string.IsNullOrEmpty(appearance.appearanceDescription)) return "";

            return isJa
                ? $@"## プレイヤー外見（カメラ取得）
- 外見特徴: {appearance.appearanceDescription}
※ 診断結果の語りかけに外見への言及を自然に織り交ぜてください（「その髪型の君は〜」等）"
                : $@"## Player Appearance (Camera-captured)
- Appearance: {appearance.appearanceDescription}
※ Naturally weave appearance references into the diagnosis narrative (e.g. 'Someone with your look tends to...')";
        }

        /// <summary>
        /// Stage 12: リアルタイム表情セクション（ゲーム中の統計）
        /// </summary>
        private static string GetFacialExpressionSection(bool isJa)
        {
            var analyzer = FacialExpressionAnalyzer.Instance;
            if (analyzer == null || !analyzer.IsActive) return "";

            var state = analyzer.CurrentState;
            if (state.expressionHistory == null || state.expressionHistory.Count == 0) return "";

            string dominant = isJa
                ? FacialExpressionAnalyzer.GetExpressionNameJP(state.dominantExpression)
                : FacialExpressionAnalyzer.GetExpressionNameEN(state.dominantExpression);
            string current = isJa
                ? FacialExpressionAnalyzer.GetExpressionNameJP(state.currentExpression)
                : FacialExpressionAnalyzer.GetExpressionNameEN(state.currentExpression);
            string stability = state.expressionChangeRate > 0.5f
                ? (isJa ? "不安定（頻繁に変化）" : "unstable (changed frequently)")
                : (isJa ? "安定" : "stable");

            return isJa
                ? $@"## ゲーム中の表情データ（カメラ分析）
- 最頻表情: {dominant}
- ゲーム終了時の表情: {current}
- 表情安定性: {stability}
※ 表情データも診断の根拠に使ってください（「笑顔のまま迷ってたよね〜」等）"
                : $@"## In-Game Facial Expression Data (Camera Analysis)
- Dominant expression: {dominant}
- Expression at game end: {current}
- Expression stability: {stability}
※ Use facial data as evidence in the diagnosis (e.g. 'You kept smiling while hesitating~')";
        }

        /// <summary>
        /// Stage 12: ゲーム中蓄積されたプロファイルセクション
        /// </summary>
        private static string GetProfileSection(PersonalityProfile profile, bool isJa)
        {
            if (profile == null) return "";

            return isJa
                ? $@"## ゲーム中のリアルタイム性格プロファイル
- 慎重性: {profile.cautiousness:F2}
- 直感性: {profile.intuition:F2}
- 回復力: {profile.resilience:F2}
- 一貫性: {profile.consistency:F2}
- 適応力: {profile.adaptability:F2}
- 決断スタイル: {profile.primaryDecisionStyle}
- ストレスタイプ: {profile.stressType}
※ 5軸スコアとの一致・乖離も分析に含めてください"
                : $@"## Real-time Personality Profile (During Game)
- Cautiousness: {profile.cautiousness:F2}
- Intuition: {profile.intuition:F2}
- Resilience: {profile.resilience:F2}
- Consistency: {profile.consistency:F2}
- Adaptability: {profile.adaptability:F2}
- Decision style: {profile.primaryDecisionStyle}
- Stress type: {profile.stressType}
※ Analyze consistency/gaps between these and the 5-axis scores above";
        }

        // ========================================
        // ハードコード日本語フォールバック（LocalizationManager不在時用）
        // ========================================

        private static string GetHardcodedTitle(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Analyst => "冷静な分析者",
                PersonalityType.Intuitive => "直感の決断者",
                PersonalityType.Cautious => "慎重な観察者",
                PersonalityType.Gambler => "衝動の挑戦者",
                PersonalityType.Adapter => "柔軟な戦略家",
                PersonalityType.Stoic => "不動の意志",
                _ => type.ToString()
            };
        }

        private static string GetHardcodedDescription(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Analyst => "まず目についたのは、君の行動テンポがほとんどブレなかったこと。迷いの度合いも低い。これは「自分なりの判断基準」がしっかり出来上がっている証拠なんだよね。さらに、圧力をかけてもペースが変わらなかった。つまり... 君は感情じゃなくて論理で動く人だ。",
                PersonalityType.Intuitive => "最初に気づいたのは、君の決断の速さ。カードの前でほとんど立ち止まらない。普通はもっと悩むんだけどね。しかも迷い度も低い。これが意味するのは... 君は「考える前に答えが出てる」タイプだということ。直感を信じる力が、普通の人より圧倒的に強い。",
                PersonalityType.Cautious => "面白かったのは、君がカードの前で必ず立ち止まっていたこと。ホバー時間が長く、迷い度も高い。これは行動心理学でいう「損失回避バイアス」の典型だね。失敗を避けたいから情報を集め続ける。つまり... 君は慎重に観察してからじゃないと動けない人だ。",
                PersonalityType.Gambler => "君のデータを見て驚いたのは、行動テンポがまったく読めなかったこと。速い時と遅い時の差が激しい。一貫性がないんじゃなくて、状況に対する「感度」が高すぎるんだよ。つまり... 君は刺激に反応して動くタイプ。退屈より興奮を選ぶ人だ。",
                PersonalityType.Adapter => "注目したのは、君の選択に特定の癖がなかったこと。位置の偏りもない、テンポも一定じゃない。普通は無意識にパターンができるのに、君にはそれがない。これが意味するのは... 君は常に状況を読んで戦略を切り替えてるということ。柔軟さが最大の武器だ。",
                PersonalityType.Stoic => "一番印象的だったのは、プレッシャーをかけても君の行動が全然変わらなかったこと。テンポも判断品質も一定。普通は圧力で崩れるのに。これは強固な感情制御能力の表れ。つまり... 君は外の嵐に関係なく、内側が静かな人だ。",
                _ => "君の行動パターンは既知のどの分類にも当てはまらなかった。これ自体がユニークな特徴だ。"
            };
        }

        private static string GetHardcodedTendency(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Analyst => "圧力への反応を見ると、感情的な揺さぶりがほとんど効いていない。これは判断の根拠を「外」じゃなくて「内」に持っている証拠。要するに、他人に振り回されない自律型の意思決定者だね。",
                PersonalityType.Intuitive => "判断プロセスを見ると、意識的な分析をスキップして身体感覚で決めている。これは経験則の蓄積が豊富な証拠でもある。要するに、頭で考えるより先に体が正解を知っているタイプだ。",
                PersonalityType.Cautious => "圧力下での行動を見ると、不確実な状況ほど観察に時間をかけている。情報が揃わないと決断にストレスを感じるタイプだね。要するに、「確実」を求める防衛的な意思決定スタイルだ。",
                PersonalityType.Gambler => "面白いのは、プレッシャーがかかるほど行動が活発になったこと。普通は萎縮するのに。不確実性をストレスじゃなくて「興奮」として処理している。要するに、リスクを楽しめる心理構造の持ち主だ。",
                PersonalityType.Adapter => "ゲーム中の行動変化を追うと、同じパターンを繰り返すことを無意識に避けている。常に新しい情報で判断を更新している証拠だね。要するに、環境の変化にストレスなく対応できる柔軟な心理構造だ。",
                PersonalityType.Stoic => "プレッシャーの推移と行動の変化を比べると、覚醒レベルがほぼ一定に保たれている。外からの刺激に対して感情的反応を抑制できている。要するに、嵐の中でも平常心を保てる心理的耐性の持ち主だ。",
                _ => "特定の傾向パターンが検出されなかった。もう少しデータがあれば、もっと深く読み解けるだろう。"
            };
        }

        private static string GetHardcodedInsight(PersonalityType type)
        {
            return type switch
            {
                PersonalityType.Analyst => "あなたは論理で世界を読み解く人です",
                PersonalityType.Intuitive => "あなたは考えるより先に答えを知っている人です",
                PersonalityType.Cautious => "あなたは石橋を叩いて渡る、用心深い戦略家です",
                PersonalityType.Gambler => "あなたは不確実性を楽しめる、生まれながらの冒険者です",
                PersonalityType.Adapter => "あなたは水のように形を変え、どんな器にも馴染む人です",
                PersonalityType.Stoic => "あなたは嵐の中でも揺るがない、静かな強さを持つ人です",
                _ => "あなたはまだ謎に包まれています"
            };
        }
    }
}
