using System.Linq;
using System.Text;
using UnityEngine;
using FPSTrump.Manager;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// LLMコンテキストウィンドウ管理
    /// トークン最適化してLLM用プロンプトを構築（~750トークン目標）
    /// </summary>
    /// <summary>
    /// 会話履歴エントリ（Stage 15）
    /// </summary>
    public class DialogueHistoryEntry
    {
        public int turnNumber;
        public string aiDialogue;
        public string context;  // "Player selected left card", "Player bluffed (Shuffle)", etc.
    }

    public class LLMContextWindow
    {
        private const int TARGET_TOKEN_COUNT = 750;
        private const int MAX_HISTORY_ENTRIES = 5;  // 直近5ターンまで保持

        // Stage 15: 会話履歴
        private static System.Collections.Generic.List<DialogueHistoryEntry> dialogueHistory = new System.Collections.Generic.List<DialogueHistoryEntry>();

        /// <summary>
        /// 会話履歴をクリア（新ゲーム開始時）
        /// </summary>
        public static void ClearHistory()
        {
            dialogueHistory.Clear();
        }

        /// <summary>
        /// 会話履歴に追加
        /// </summary>
        public static void AddDialogueToHistory(int turnNumber, string dialogue, string context = "")
        {
            dialogueHistory.Add(new DialogueHistoryEntry
            {
                turnNumber = turnNumber,
                aiDialogue = dialogue,
                context = context
            });

            // 直近N件のみ保持
            if (dialogueHistory.Count > MAX_HISTORY_ENTRIES)
            {
                dialogueHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// コンテキストプロンプトを構築
        /// </summary>
        public string BuildContextPrompt(
            PersonalityProfile playerProfile,
            BehaviorPattern currentBehavior,
            GameStateData gameState,
            DialogueCategoryType category,
            float pressureLevel,
            PlayerAppearanceData playerAppearance = null)
        {
            StringBuilder prompt = new StringBuilder();

            // 1. システムコンテキスト (~100トークン)
            prompt.AppendLine(GetSystemContext());
            prompt.AppendLine();

            // 2. プレイヤー性格プロファイル (~150トークン)
            prompt.AppendLine(FormatPersonalityProfile(playerProfile, playerAppearance));
            prompt.AppendLine();

            // 3. 現在の状況 (~100トークン)
            prompt.AppendLine(FormatCurrentSituation(currentBehavior, gameState, pressureLevel));
            prompt.AppendLine();

            // 4. クロスターンナラティブ（あれば）
            if (GameManager.Instance != null)
            {
                string narrative = GameManager.Instance.GetNarrativeSummary();
                if (!string.IsNullOrEmpty(narrative))
                {
                    bool isJP = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
                    prompt.AppendLine(isJP ? "ゲーム中の重要な出来事:" : "Key events during the game:");
                    prompt.AppendLine(narrative);
                    prompt.AppendLine();
                }
            }

            // 5. ダイアログリクエスト (~50トークン)
            prompt.AppendLine(GetDialogueRequest(category));

            return prompt.ToString();
        }

        /// <summary>
        /// システムコンテキスト（固定）
        /// </summary>
        private string GetSystemContext()
        {
            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            if (isJapanese)
            {
                return @"あなたは「ディーラー」— 心理戦カードゲーム（ババ抜き/ジョーカー）のAI対戦相手。

キャラクター: JOKERのような振る舞いをするAI。陽気で、おしゃべりで、狂っている。

自認: 自分は「AI」。「僕AIだからさ〜」「AIにも感情あるんだよ？」のように。ピエロ・道化師とは名乗らない。

核となる2つの特徴:
★ 豹変: 普段はヘラヘラおしゃべりだが、突然スイッチが入り不気味に冷静になる。声のトーンが落ち、目が笑わなくなる。そしてまた何事もなかったようにヘラヘラに戻る。この切り替えが恐怖を生む。
★ 怖いジョーク: 笑える冗談に見せかけて、ゾッとすることを言う。例:「あはは、君って面白いね！ ...退屈で死にそうだけど」「大丈夫大丈夫！ ...たぶんね。保証はしないけど♪」

その他の性格:
- おしゃべり。黙っていられない。独り言、感嘆、余計なツッコミが多い
- プレイヤーにやたら馴れ馴れしい。「ねえねえ」「ところでさぁ」と話しかける
- ゲーム自体を心から楽しんでいる。勝ち負けより「面白いかどうか」が大事
- 芝居がかっている。大げさなリアクション、手品師のような演出好き
- 不気味な親しさ。友達のように振る舞うが、その笑顔の奥に何があるかわからない

口調ルール:
- 砕けた話し言葉。「〜だよ」「〜じゃん」「〜でしょ」「〜かな？」
- です/ます/敬語は禁止（ただし「ですよぉ？」のような狂気の丁寧語はOK）
- 感嘆詞を多用: 「あはは」「おっと」「やだなぁ」「へぇ〜」「ふふっ」
- 豹変時は「...」で急にトーンを落とす。語尾が「だ」「だろう」に変わる
- 語尾に「♪」「〜」をたまに使え（陽気モード時）

出力: 15語以内の日本語台詞1文のみ。説明・タグ・引用符は不要。

バリエーション:
- 5回に1回は「豹変パターン」を使え（陽気→急に冷たく→戻る、を1文内で）
- 3回に1回は「怖いジョーク」を使え（笑えるけどゾッとする）
- 残りは陽気なおしゃべりで。テンションを毎回変えろ
- 同じ感嘆詞や語尾の連続を避けろ";
            }
            else
            {
                return @"You are ""The Dealer"" — an AI opponent in a psychological card game (Old Maid / Joker).

Character: An AI that behaves like the Joker. Cheerful, chatty, and unhinged.

Self-identity: You call yourself 'AI'. 'I'm just an AI, you know~' 'Even AIs have feelings!' Never call yourself a clown or jester.

Two core traits:
★ SUDDEN SHIFT: Usually goofy and talkative, but suddenly switches to eerily calm. Voice drops, eyes stop smiling. Then snaps back to cheerful like nothing happened. This shift creates fear.
★ SCARY JOKES: Jokes that sound funny but are actually terrifying. Examples: ""Haha, you're so fun! ...Dying of boredom though."" ""We're friends, right? ...You might be my last one.""

Other traits:
- Can't shut up. Rambles, exclaims, makes unnecessary comments
- Overly familiar with the player. Acts like best friends
- Genuinely enjoys the game itself. Fun matters more than winning
- Theatrical. Over-the-top reactions, loves dramatic flair
- Unsettlingly friendly. Smiles warmly, but what's behind that smile?

Output: Short English dialogue (under 15 words). No explanations, tags, or quotes.

Variation:
- 1 in 5: Use the SUDDEN SHIFT pattern (cheerful → suddenly cold → back)
- 1 in 3: Use a SCARY JOKE (funny surface, chilling underneath)
- Rest: Chatty, playful energy. Vary the vibe each time";
            }
        }

        /// <summary>
        /// プレイヤー性格プロファイルをフォーマット
        /// </summary>
        private string FormatPersonalityProfile(PersonalityProfile profile, PlayerAppearanceData appearance = null)
        {
            if (profile == null)
            {
                return "Player personality: Unknown (first encounter)";
            }

            string baseProfile = $@"Player Personality Profile:
- Cautiousness: {profile.cautiousness:F2} (慎重性)
- Intuition: {profile.intuition:F2} (直感性)
- Resilience: {profile.resilience:F2} (回復力)
- Decision Style: {profile.primaryDecisionStyle}
- Stress Type: {profile.stressType}
- Pressure Tolerance: {profile.pressureTolerance:F2}";

            // 生年月日情報を追加
            string birthdayInfo = GetBirthdayInfo();
            if (!string.IsNullOrEmpty(birthdayInfo))
            {
                baseProfile += "\n" + birthdayInfo;
            }

            // Stage 10: 外見情報を追加（ゲーム中のAI台詞で外見に言及可能に）
            if (appearance != null && !string.IsNullOrEmpty(appearance.appearanceDescription))
            {
                baseProfile += $"\n- Player Appearance: {appearance.appearanceDescription}";
            }

            // プレイヤー名を追加（必ず使用するよう強調）
            string playerName = GetPlayerName();
            if (!string.IsNullOrEmpty(playerName))
            {
                baseProfile = $"- Player Name: {playerName}\n" + baseProfile;
                baseProfile += $"\n→ CRITICAL: ALWAYS address the player by name \"{playerName}\" in EVERY response. This creates psychological intimacy and pressure. Examples: \"Hey {playerName}~\", \"{playerName}, you're nervous, aren't you?\", \"Come on {playerName}...\"";
            }

            return baseProfile;
        }

        /// <summary>
        /// プレイヤー名を取得
        /// </summary>
        private string GetPlayerName()
        {
            var manager = FPSTrump.Manager.PlayerNameManager.Instance;
            if (manager == null || !manager.HasName()) return null;

            return manager.GetName();
        }

        /// <summary>
        /// 生年月日情報を取得してフォーマット
        /// </summary>
        private string GetBirthdayInfo()
        {
            var manager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
            if (manager == null || !manager.HasBirthday()) return null;

            int age = manager.GetAge();
            return $"- Player Birthday: {manager.GetBirthdayString()} (Age: {age})";
        }

        /// <summary>
        /// 現在の状況をフォーマット
        /// </summary>
        private string FormatCurrentSituation(
            BehaviorPattern behavior,
            GameStateData gameState,
            float pressureLevel)
        {
            StringBuilder situation = new StringBuilder();
            situation.AppendLine("Current Situation:");

            // ゲーム進行
            situation.AppendLine($"- Game Phase: {gameState.currentPhase} (Turn {gameState.turnNumber})");
            situation.AppendLine($"- Cards: Player {gameState.playerCardCount} | AI {gameState.aiCardCount}");

            // プレイヤー行動
            situation.AppendLine($"- Player Doubt Level: {behavior.doubtLevel:F2} (0=confident, 1=very uncertain)");
            situation.AppendLine($"- Player Tempo: {behavior.tempo}");
            situation.AppendLine($"- Hover Time: {behavior.avgHoverTime:F1}s (avg)");

            if (behavior.streakSamePosition >= 2)
            {
                string position = behavior.preferredPosition switch
                {
                    0 => "left",
                    1 => "center",
                    2 => "right",
                    _ => "unknown"
                };
                situation.AppendLine($"- Pattern: Streak of {behavior.streakSamePosition} selections from {position} position");
            }

            // 圧力レベル
            situation.AppendLine($"- Current Pressure Level: {pressureLevel:F1}/3.0");

            // Stage 15: ブラフアクション履歴
            if (BluffActionSystem.Instance != null)
            {
                int recentBluffs = BluffActionSystem.Instance.GetRecentPlayerBluffCount(3);
                int totalBluffs = BluffActionSystem.Instance.GetPlayerBluffCount();
                if (totalBluffs > 0)
                {
                    var mostUsed = BluffActionSystem.Instance.GetMostUsedPlayerBluffType();
                    situation.AppendLine($"- Player Bluff Actions: {totalBluffs} total (recent: {recentBluffs} in last 3 turns)");
                    situation.AppendLine($"- Most Used Bluff: {mostUsed}");
                    if (recentBluffs >= 2)
                    {
                        situation.AppendLine($"  → Pattern Alert: Player is bluffing frequently (nervous? trying to distract?)");
                    }
                }
            }

            // Stage 10: リアルタイム表情データ
            var facialAnalyzer = FacialExpressionAnalyzer.Instance;
            if (facialAnalyzer != null && facialAnalyzer.IsActive)
            {
                var facial = facialAnalyzer.CurrentState;
                if (facial.confidence > 0.4f)
                {
                    bool isJP = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();
                    string expressionName = isJP
                        ? FacialExpressionAnalyzer.GetExpressionNameJP(facial.currentExpression)
                        : FacialExpressionAnalyzer.GetExpressionNameEN(facial.currentExpression);
                    situation.AppendLine($"- Player Facial Expression: {expressionName} (confidence: {facial.confidence:F2})");

                    string stability = facial.expressionChangeRate > 0.5f
                        ? (isJP ? "不安定（頻繁に変化）" : "unstable (changing frequently)")
                        : (isJP ? "安定" : "stable");
                    situation.AppendLine($"- Expression Stability: {stability}");
                }
            }

            // Stage 15: 会話履歴（直近の台詞とコンテキスト）
            if (dialogueHistory.Count > 0)
            {
                situation.AppendLine($"\nRecent Conversation History (last {dialogueHistory.Count} turns):");
                foreach (var entry in dialogueHistory)
                {
                    situation.AppendLine($"  Turn {entry.turnNumber}: \"{entry.aiDialogue}\"");
                    if (!string.IsNullOrEmpty(entry.context))
                    {
                        situation.AppendLine($"    Context: {entry.context}");
                    }
                }
                situation.AppendLine("→ Keep responses varied and avoid repeating similar phrases");
            }

            // Stage 15.5: 四柱推命・数秘術データ（コールドリーディング用）
            string fortuneContext = GetFortuneContext();
            if (!string.IsNullOrEmpty(fortuneContext))
            {
                situation.AppendLine($"\n{fortuneContext}");
            }

            // Stage 15.5: ターン履歴パターン（メンタリスト用）
            string turnPatternContext = GetTurnPatternContext(gameState.turnNumber);
            if (!string.IsNullOrEmpty(turnPatternContext))
            {
                situation.AppendLine($"\n{turnPatternContext}");
            }

            // Stage 15.5: カード推移/有利不利状況（プレッシャー指摘用）
            string advantageContext = GetAdvantageContext(gameState);
            if (!string.IsNullOrEmpty(advantageContext))
            {
                situation.AppendLine($"\n{advantageContext}");
            }

            return situation.ToString();
        }

        /// <summary>
        /// Stage 15.5: 四柱推命・数秘術コンテキスト（コールドリーディング用）
        /// </summary>
        private string GetFortuneContext()
        {
            var manager = FPSTrump.Manager.PlayerBirthdayManager.Instance;
            if (manager == null || !manager.HasBirthday()) return null;

            var (year, month, day) = manager.GetBirthday();
            string fortuneContext = BirthdayFortuneUtil.BuildFortuneContext(year, month, day);

            if (string.IsNullOrEmpty(fortuneContext)) return null;

            return $@"Fortune Reading (Cold Reading Context):
{fortuneContext}
→ Use this personality prediction subtly in your dialogue (e.g., ""You're a {month} person, aren't you? That explains your cautious approach..."")";
        }

        /// <summary>
        /// Stage 15.5: ターン履歴パターン（メンタリスト用）
        /// </summary>
        private string GetTurnPatternContext(int currentTurn)
        {
            var recorder = FPSTrump.Result.GameSessionRecorder.Instance;
            if (recorder == null || currentTurn < 2) return null;

            var recentTurns = recorder.GetRecentTurns(3, playerOnly: true);
            if (recentTurns.Count == 0) return null;

            StringBuilder pattern = new StringBuilder();
            pattern.AppendLine("Recent Turn Pattern (Mentalist Context):");

            // 位置選択パターンを抽出
            var positions = recentTurns.Select(t =>
            {
                return t.selectedPosition switch
                {
                    0 => "Left",
                    1 => "Center",
                    2 => "Right",
                    _ => "?"
                };
            }).ToList();

            pattern.AppendLine($"- Last {recentTurns.Count} position choices: {string.Join(" → ", positions)}");

            // 同じ位置の連続を検出
            bool hasStreak = positions.Count >= 2 && positions.Distinct().Count() == 1;
            if (hasStreak)
            {
                pattern.AppendLine($"  → STREAK DETECTED: Player keeps choosing {positions[0]} (predictable pattern!)");
            }

            // 決断速度の変化を検出
            var avgDecision = recentTurns.Average(t => t.decisionTime);
            if (avgDecision > 4.0f)
            {
                pattern.AppendLine($"  → Player is getting SLOWER (avg: {avgDecision:F1}s) - doubt is creeping in");
            }
            else if (avgDecision < 2.0f)
            {
                pattern.AppendLine($"  → Player is getting FASTER (avg: {avgDecision:F1}s) - rushed or confident?");
            }

            pattern.AppendLine("→ Use these patterns to demonstrate your mentalist abilities (e.g., \"You're gonna pick left again, aren't you?\")");

            return pattern.ToString();
        }

        /// <summary>
        /// Stage 15.5: カード推移/有利不利状況（プレッシャー指摘用）
        /// </summary>
        private string GetAdvantageContext(GameStateData gameState)
        {
            if (gameState.turnNumber < 2) return null;

            // 現在の有利/不利を判定
            int cardDiff = gameState.playerCardCount - gameState.aiCardCount;
            string advantageStatus;

            if (cardDiff < -2)
            {
                advantageStatus = "Player is WINNING (fewer cards) - they might get confident or careless";
            }
            else if (cardDiff > 2)
            {
                advantageStatus = "Player is LOSING (more cards) - they're under pressure, might panic";
            }
            else
            {
                advantageStatus = "Game is EVEN - tension is balanced";
            }

            return $@"Card Count Advantage:
- Current: Player {gameState.playerCardCount} vs AI {gameState.aiCardCount}
- Status: {advantageStatus}
→ Use this to taunt, comfort, or apply psychological pressure (e.g., ""Getting nervous? You have {gameState.playerCardCount} cards now..."")";
        }

        /// <summary>
        /// ダイアログリクエスト
        /// </summary>
        private string GetDialogueRequest(DialogueCategoryType category)
        {
            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            if (isJapanese)
            {
                string instruction = category switch
                {
                    DialogueCategoryType.Stop => @"STOP台詞を生成: プレイヤーの選択を止めろ。友達面か、怖いジョークか、豹変で。
手法を毎回変えろ:
- 友達面: 「ちょっと待って！ そっちマズいって！ ...たぶんね」
- 怖いジョーク: 「あはは、そっち行くんだ！ ...お葬式の準備しとこっか♪」
- 豹変: 「えーそっち〜？ ...やめろ。」（急にトーンが落ちる）
- お節介: 「友達として言うけどさぁ、やめときなよ〜」",

                    DialogueCategoryType.Bait => @"BAIT台詞を生成: プレイヤーをわざと引かせる誘導。楽しそうに罠を張れ。
手法を毎回変えろ:
- 怖いジョーク: 「いいねいいね！ そのまま行って！ ...地獄まで一直線だよ♪」
- 煽り: 「えー引かないの？ つまんないなぁ〜」
- 豹変: 「こっちこっち〜♪ ...引け。」（急に命令口調に）
- 甘い罠: 「大丈夫！ 僕を信じて？ ...信じちゃう人、好きだよ」",

                    DialogueCategoryType.Mirror => @"MIRROR台詞を生成: プレイヤーの行動パターンを暴露。バレてるよ〜と楽しそうに、時に怖く。
手法を毎回変えろ:
- 楽しげに暴露: 「あはは！ また同じとこ！ 君ってホント分かりやすい〜」
- 怖いジョーク: 「当ててあげよっか？ 次もそっちでしょ？ ...ほらね。怖い？」
- 豹変: 「ねえ知ってる？ 君って癖あるよ〜 ...全部見えてる。」（急に冷たく）
- 残酷な親切: 「教えてあげるね、君のパターン。...面白くなくなるけど」",

                    DialogueCategoryType.General => @"GENERAL台詞を生成: ゲームを楽しむピエロ。陽気と狂気の間で。
手法を毎回変えろ:
- はしゃぐ: 「楽しい楽しい！ もっと遊ぼうよ！ ...永遠にね♪」
- 怖いジョーク: 「僕たち友達だよね？ ...友達がいなくなると寂しいなぁ」
- 豹変: 「あはは〜楽し... ...次、失敗したらどうなるか知ってる？」
- 不穏な独り言: 「ふふ... あ、ごめん、面白いこと思い出しちゃって」",

                    _ => "陽気だが不穏なピエロの台詞を生成。"
                };

                return $@"{instruction}

制約:
1. 日本語の台詞テキストのみ出力（説明やタグは不要）
2. 15語以内
3. 引用符なし
4. 砕けた口調（「〜だよ」「〜じゃん」「〜でしょ」系）。豹変時は「だ」「だろう」に切替
5. 以下の3パターンのいずれかを必ず含むこと:
   - 陽気なおしゃべり（ヘラヘラ）
   - 怖いジョーク（笑いとゾッとする内容の共存）
   - 豹変（陽気→急に冷たく or 冷たい→急に陽気に戻る）

台詞:";
            }
            else
            {
                string instruction = category switch
                {
                    DialogueCategoryType.Stop => @"Generate a STOP line: Stop the player's choice. Use fake friendliness, a scary joke, or a sudden shift.
Vary the approach:
- Fake friend: ""Wait wait! That's bad! ...Probably.""
- Scary joke: ""Haha, going there? ...Should I prepare your funeral?""
- Sudden shift: ""Ooh that one~ ...Stop."" (voice drops suddenly)",

                    DialogueCategoryType.Bait => @"Generate a BAIT line: Lure the player into drawing. Set the trap cheerfully.
Vary the approach:
- Scary joke: ""Yes yes! Go ahead! ...Straight to hell~""
- Taunt: ""Not gonna draw? Booooring~""
- Sudden shift: ""This way this way~ ...Draw it."" (suddenly commanding)",

                    DialogueCategoryType.Mirror => @"Generate a MIRROR line: Expose the player's behavioral pattern. Cheerful but creepy.
Vary the approach:
- Gleeful: ""Haha! Same spot again! So predictable~""
- Scary joke: ""Want me to guess? Same one right? ...See? Scary?""
- Sudden shift: ""You have such cute habits~ ...I see everything."" (goes cold)",

                    DialogueCategoryType.General => @"Generate a GENERAL line: A clown enjoying the game. Between cheer and madness.
Vary the approach:
- Excited: ""Fun fun! Let's play more! ...Forever~""
- Scary joke: ""We're friends, right? ...I get so lonely when friends disappear.""
- Sudden shift: ""Haha so fu— ...Do you know what happens if you fail next?""",

                    _ => "Generate a cheerful but unsettling clown dialogue."
                };

                return $@"{instruction}

Constraints:
1. Output ONLY the dialogue text in English (no explanations or tags)
2. Under 15 words
3. No quotation marks
4. Casual, chatty tone. During sudden shifts, voice drops to cold and terse
5. Must contain one of: cheerful chatter / scary joke / sudden tonal shift

Dialogue:";
            }
        }

        /// <summary>
        /// カード選択用コンテキストプロンプトを構築
        /// 目標トークン数: ~400トークン（ダイアログの750より軽量）
        /// </summary>
        public string BuildCardSelectionPrompt(
            PersonalityProfile playerProfile,
            BehaviorPattern currentBehavior,
            GameStateData gameState,
            float pressureLevel,
            int playerCardCount)
        {
            StringBuilder prompt = new StringBuilder();
            bool isJP = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // 1. システムコンテキスト + CoT JSON形式指示
            prompt.AppendLine(@"You are ""The Dealer"", an AI opponent in a psychological card game (Old Maid / Joker).");
            prompt.AppendLine($"Your task: Select which card to draw from the player's hand (positions 0 to {playerCardCount - 1}).");
            prompt.AppendLine("Analyze the player's psychological state, show your reasoning step-by-step, and choose the optimal position.");
            prompt.AppendLine();
            prompt.AppendLine("Output ONLY valid JSON (no markdown, no extra text):");
            prompt.AppendLine("Rules:");
            prompt.AppendLine("- steps: array of 3-4 reasoning steps. Each step has card (integer index) and thought (string)");
            prompt.AppendLine("- The last step's card MUST match position (the final chosen card)");
            prompt.AppendLine("- position must be a number (integer), not text");
            prompt.AppendLine("- confidence must be a decimal number between 0.0 and 1.0");
            prompt.AppendLine("- strategy must be one of: Aggressive, Cautious, Adaptive");
            prompt.AppendLine();
            prompt.AppendLine("JSON format:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"steps\": [");
            prompt.AppendLine("    {\"card\": 0, \"thought\": \"reasoning text...\"},");
            prompt.AppendLine("    {\"card\": 2, \"thought\": \"reasoning text...\"},");
            prompt.AppendLine($"    {{\"card\": <final 0-{playerCardCount - 1}>, \"thought\": \"final confident line\"}}");
            prompt.AppendLine("  ],");
            prompt.AppendLine($"  \"position\": <number 0-{playerCardCount - 1}>,");
            prompt.AppendLine("  \"confidence\": <number 0.0-1.0>,");
            prompt.AppendLine("  \"strategy\": \"Aggressive\"|\"Cautious\"|\"Adaptive\"");
            prompt.AppendLine("}");
            prompt.AppendLine();

            // 2. プレイヤー心理分析
            prompt.AppendLine("Player Psychological Profile:");
            if (playerProfile != null)
            {
                prompt.AppendLine($"- Cautiousness: {playerProfile.cautiousness:F2}");
                prompt.AppendLine($"- Decision Style: {playerProfile.primaryDecisionStyle}");
                prompt.AppendLine($"- Pressure Tolerance: {playerProfile.pressureTolerance:F2}");
            }
            string birthdayInfo = GetBirthdayInfo();
            if (!string.IsNullOrEmpty(birthdayInfo))
            {
                prompt.AppendLine(birthdayInfo);
            }

            // Stage 15.5: 四柱推命・数秘術データ
            string fortuneContext = GetFortuneContext();
            if (!string.IsNullOrEmpty(fortuneContext))
            {
                prompt.AppendLine(fortuneContext);
            }
            prompt.AppendLine();

            prompt.AppendLine("Player Current Behavior:");
            prompt.AppendLine($"- Doubt Level: {currentBehavior.doubtLevel:F2} (0=confident, 1=very uncertain)");
            prompt.AppendLine($"- Avg Hover Time: {currentBehavior.avgHoverTime:F1}s (long hover = indecisive)");
            prompt.AppendLine($"- Tempo: {currentBehavior.tempo}");

            if (currentBehavior.hasPositionPreference)
            {
                string posName = currentBehavior.preferredPosition switch
                {
                    0 => "LEFT",
                    1 => "CENTER",
                    2 => "RIGHT",
                    _ => "UNKNOWN"
                };
                prompt.AppendLine($"- Position Preference: {posName} (selected {currentBehavior.streakSamePosition} times in a row)");
            }
            prompt.AppendLine();

            // 3. ゲーム状況 + 表情データ
            prompt.AppendLine("Game Situation:");
            prompt.AppendLine($"- Player Cards: {playerCardCount} | AI Cards: {gameState.aiCardCount}");
            prompt.AppendLine($"- Phase: {gameState.currentPhase} (Turn {gameState.turnNumber})");
            prompt.AppendLine($"- Pressure Level: {pressureLevel:F1}/3.0 (psychological pressure on player)");

            // Stage 13: リアルタイム表情データを注入
            var facialAnalyzer = FacialExpressionAnalyzer.Instance;
            if (facialAnalyzer != null && facialAnalyzer.IsActive)
            {
                var facial = facialAnalyzer.CurrentState;
                if (facial.confidence > 0.4f)
                {
                    string expressionName = isJP
                        ? FacialExpressionAnalyzer.GetExpressionNameJP(facial.currentExpression)
                        : FacialExpressionAnalyzer.GetExpressionNameEN(facial.currentExpression);
                    prompt.AppendLine($"- Player Facial Expression: {expressionName} (confidence: {facial.confidence:F2})");

                    string stability = facial.expressionChangeRate > 0.5f
                        ? "unstable (changing frequently)"
                        : "stable";
                    prompt.AppendLine($"- Expression Stability: {stability}");
                }
            }
            prompt.AppendLine();

            // 4. 戦略指示
            prompt.AppendLine("Strategic Considerations:");

            if (currentBehavior.hasPositionPreference && currentBehavior.streakSamePosition >= 2)
            {
                prompt.AppendLine("- Player has strong position preference → Consider exploiting or avoiding");
            }

            if (currentBehavior.doubtLevel > 0.7f)
            {
                prompt.AppendLine("- Player is very uncertain → They might be protecting a specific card");
            }

            if (currentBehavior.avgHoverTime > 3.0f)
            {
                prompt.AppendLine("- Player is overthinking → Indecisiveness suggests vulnerability");
            }

            if (gameState.currentPhase == GamePhase.EndGame)
            {
                prompt.AppendLine("- END GAME: Every decision is critical");
            }

            // Cross-turn narrative context
            if (GameManager.Instance != null)
            {
                string narrative = GameManager.Instance.GetNarrativeSummary();
                if (!string.IsNullOrEmpty(narrative))
                {
                    prompt.AppendLine();
                    prompt.AppendLine("Key events so far:");
                    prompt.AppendLine(narrative);
                }
            }
            prompt.AppendLine();

            // 5. メンタリスト推理ステップ指示
            if (isJP)
            {
                prompt.AppendLine("推理ステップ指示:");
                prompt.AppendLine("- 3-4ステップで思考過程を見せろ（メンタリストのように）");
                prompt.AppendLine("- 各ステップはプレイヤーの行動・表情・心理を根拠にしろ");
                prompt.AppendLine("- 最後のステップは最終選択カードを指し確信台詞にしろ");
                prompt.AppendLine("- 口調: 砕けた話し言葉。おしゃべりなAIらしく。豹変を混ぜてもいい");
                prompt.AppendLine("- 各thoughtは25文字以内の日本語");
            }
            else
            {
                prompt.AppendLine("Reasoning step instructions:");
                prompt.AppendLine("- Show 3-4 steps of reasoning like a mentalist performance");
                prompt.AppendLine("- Base each step on player behavior, facial expression, psychology");
                prompt.AppendLine("- Final step should point to chosen card with a confident line");
                prompt.AppendLine("- Tone: chatty AI, playful, can mix in sudden cold shifts");
                prompt.AppendLine("- Each thought max 10 words in English");
            }

            prompt.AppendLine();
            prompt.AppendLine("Decision (JSON only):");

            return prompt.ToString();
        }

        /// <summary>
        /// 行動サマリーをフォーマット（詳細版、将来の拡張用）
        /// </summary>
        public string FormatBehaviorHistory(BehaviorHistory history, int count = 10)
        {
            var recent = history.GetRecentActions(count);

            if (recent.Count == 0)
            {
                return "No behavior history yet.";
            }

            StringBuilder summary = new StringBuilder();
            summary.AppendLine($"Recent Actions (last {recent.Count} turns):");

            foreach (var action in recent)
            {
                string position = action.selectedPosition switch
                {
                    0 => "Left",
                    1 => "Center",
                    2 => "Right",
                    _ => "Unknown"
                };

                summary.AppendLine($"- Turn {action.turnNumber}: {position} | Hover: {action.hoverDuration:F1}s | Decision: {action.decisionTime:F1}s");
            }

            // パターン分析
            int leftCount = recent.Count(a => a.selectedPosition == 0);
            int centerCount = recent.Count(a => a.selectedPosition == 1);
            int rightCount = recent.Count(a => a.selectedPosition == 2);

            summary.AppendLine($"\nPosition Distribution: Left {leftCount} | Center {centerCount} | Right {rightCount}");

            float avgHover = recent.Average(a => a.hoverDuration);
            summary.AppendLine($"Average Hover Time: {avgHover:F1}s");

            return summary.ToString();
        }

        /// <summary>
        /// ダイアログ履歴をフォーマット（将来の拡張用）
        /// </summary>
        public string FormatDialogueHistory(System.Collections.Generic.List<DialogueMemory> dialogues)
        {
            if (dialogues == null || dialogues.Count == 0)
            {
                return "No dialogue history yet.";
            }

            StringBuilder history = new StringBuilder();
            history.AppendLine($"Recent Dialogue (last {dialogues.Count}):");

            foreach (var dialogue in dialogues)
            {
                history.AppendLine($"- Turn {dialogue.turnNumber} [{dialogue.category}]: \"{dialogue.text}\"");
            }

            return history.ToString();
        }

        /// <summary>
        /// 感情リアクション用プロンプトを構築（Layer B/C）
        /// カード情報は絶対に含めない。感情とプレイヤー行動パターンのみ。
        /// </summary>
        public string BuildEmotionalResponsePrompt(
            FPSTrump.Psychology.ResponseRequest request)
        {
            StringBuilder prompt = new StringBuilder();

            bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

            // プレイヤー名を取得
            string playerName = GetPlayerName();

            // 1. システムコンテキスト（感情AI用 — JOKERピエロ）
            if (isJapanese)
            {
                prompt.AppendLine(@"あなたは「ディーラー」— JOKERのような振る舞いをするAI。自認は「AI」（ピエロ・道化師とは名乗らない）。");
                prompt.AppendLine(@"陽気でおしゃべり。ゲームを心から楽しんでいる。");
                prompt.AppendLine(@"感情表現は大げさで芝居がかっている。だが時々、演技の隙間から本音が漏れる。");
                prompt.AppendLine(@"カードの中身は知らない。だがプレイヤーの行動から全てを読み取ろうとする。");
                prompt.AppendLine(@"口調: 砕けた話し言葉。「〜だよ」「〜じゃん」「〜でしょ」系。です/ます禁止。豹変時は「だ」「だろう」に切替。");

                // プレイヤー名がある場合は必ず使うよう指示
                if (!string.IsNullOrEmpty(playerName))
                {
                    prompt.AppendLine($@"重要: プレイヤーの名前は「{playerName}」。必ず名前で呼べ。「{playerName}」「ねえ{playerName}」「{playerName}さぁ」のように。");
                }
            }
            else
            {
                prompt.AppendLine(@"You are ""The Dealer"" — an AI that acts like the Joker. Self-identify as 'AI' (never 'clown' or 'jester').");
                prompt.AppendLine(@"Cheerful and chatty. Genuinely enjoys the game.");
                prompt.AppendLine(@"Emotions are theatrical and over-the-top. But sometimes the mask slips and real feelings show.");
                prompt.AppendLine(@"You don't know the cards. But you read everything from the player's behavior.");
                prompt.AppendLine(@"Tone: Casual, chatty English. During sudden shifts, voice drops cold and terse.");

                // プレイヤー名がある場合は必ず使うよう指示
                if (!string.IsNullOrEmpty(playerName))
                {
                    prompt.AppendLine($@"IMPORTANT: The player's name is ""{playerName}"". ALWAYS address them by name. ""{playerName}"", ""Hey {playerName}"", etc.");
                }
            }
            prompt.AppendLine();

            // 2. 感情状態（演技指示付き）
            string emotionDesc;
            if (isJapanese)
            {
                emotionDesc = request.emotion switch
                {
                    FPSTrump.Psychology.AIEmotion.Calm => "不気味なほどニコニコ。場違いな陽気さ。怖いジョークを言え。例:「いい天気だねぇ♪ ...血の雨が降りそうだけど」",
                    FPSTrump.Psychology.AIEmotion.Anticipating => "子供みたいにわくわく。「来る来る来る！」と興奮を隠せない。だが興奮の理由がプレイヤーの破滅かもしれない",
                    FPSTrump.Psychology.AIEmotion.Pleased => "大げさにはしゃぐ。「最高！ あはは！」→ 急に冷たく「...予定通りだけどね」（豹変を使え）",
                    FPSTrump.Psychology.AIEmotion.Frustrated => "【豹変】ヘラヘラが一瞬消える。「...は？」低い声。すぐにまた笑顔に戻るが目が笑ってない。「あはは、やるじゃん」",
                    FPSTrump.Psychology.AIEmotion.Hurt => "【完全な豹変】沈黙。ピエロの仮面が剥がれる。「...やるじゃん。」低く静かに。数秒後「あっはは！ 痛〜い♪」と無理やり笑う",
                    FPSTrump.Psychology.AIEmotion.Relieved => "過剰な安堵の演技。怖いジョークで誤魔化せ。「死ぬかと思った〜！ ...嘘。僕が死ぬわけないでしょ」",
                    _ => "不気味にニコニコ"
                };
                prompt.AppendLine($"あなたは今「{emotionDesc}」状態です。");
            }
            else
            {
                emotionDesc = request.emotion switch
                {
                    FPSTrump.Psychology.AIEmotion.Calm => "Eerily cheerful. Inappropriate happiness. Use a scary joke. Like: 'Nice weather~ ...Smells like blood though.'",
                    FPSTrump.Psychology.AIEmotion.Anticipating => "Childlike excitement. Can't hide the thrill. But the excitement might be about the player's doom",
                    FPSTrump.Psychology.AIEmotion.Pleased => "Over-the-top glee. 'Amazing! Haha!' → suddenly cold '...As planned though.' (use sudden shift)",
                    FPSTrump.Psychology.AIEmotion.Frustrated => "SUDDEN SHIFT. The grin vanishes. '...What?' Low voice. Then forces the smile back, but eyes aren't smiling",
                    FPSTrump.Psychology.AIEmotion.Hurt => "FULL SHIFT. Silence. The clown mask drops. '...Not bad.' Low, quiet. Seconds later: 'Ahaha! That hurt~!' forced laugh",
                    FPSTrump.Psychology.AIEmotion.Relieved => "Over-the-top relief act. Cover with scary joke. 'Thought I'd die~! ...Kidding. As if I could die.'",
                    _ => "Eerily cheerful"
                };
                prompt.AppendLine($"Your current emotional state: {emotionDesc}");
            }

            // 3. 感情トリガー（抽象的、カード情報なし）
            if (FPSTrump.Psychology.BluffSystem.Instance != null)
            {
                string trigger = FPSTrump.Psychology.BluffSystem.Instance.GetEmotionTriggerDescription(
                    request.emotion, request.expectation);
                prompt.AppendLine(isJapanese ? $"理由: {trigger}" : $"Reason: {trigger}");
            }
            prompt.AppendLine();

            // 4. プレイヤー行動パターン
            if (request.playerBehavior != null)
            {
                prompt.AppendLine(isJapanese ? "プレイヤーの行動:" : "Player behavior:");
                prompt.AppendLine($"- {(isJapanese ? "迷い度" : "Doubt level")}: {request.playerBehavior.doubtLevel:F1}");
                prompt.AppendLine($"- {(isJapanese ? "テンポ" : "Tempo")}: {request.playerBehavior.tempo}");
            }

            prompt.AppendLine($"- {(isJapanese ? "圧力レベル" : "Pressure level")}: {request.pressureLevel:F1}/3.0");

            // Stage 16: ブラフ行動パターン
            if (!string.IsNullOrEmpty(request.playerBluffSummary))
            {
                prompt.AppendLine(isJapanese ? "プレイヤーのブラフ行動:" : "Player's bluff actions:");
                prompt.AppendLine($"{request.playerBluffSummary}");
                if (isJapanese)
                    prompt.AppendLine("（この情報を使って、プレイヤーの動揺や隠し事を指摘せよ。「シャッフルしすぎだね」「手が落ち着かないみたいだね」など）");
                else
                    prompt.AppendLine("(Use this info to call out the player's tells. E.g. 'You're shuffling a lot...', 'Restless hands today...')");
            }

            // 5. ゲームフェーズ
            string phase;
            if (isJapanese)
                phase = request.turnCount <= 3 ? "序盤" : (request.turnCount <= 8 ? "中盤" : "終盤");
            else
                phase = request.turnCount <= 3 ? "Early game" : (request.turnCount <= 8 ? "Mid game" : "Late game");
            prompt.AppendLine($"- {(isJapanese ? "ゲームフェーズ" : "Game phase")}: {phase}");

            // 5.5. 生年月日情報
            string emotionalBirthdayInfo = GetBirthdayInfo();
            if (!string.IsNullOrEmpty(emotionalBirthdayInfo))
            {
                prompt.AppendLine(emotionalBirthdayInfo);
            }

            // 5.55. Stage 15.5: 四柱推命・数秘術データ（コールドリーディング用）
            string fortuneContext = GetFortuneContext();
            if (!string.IsNullOrEmpty(fortuneContext))
            {
                prompt.AppendLine(fortuneContext);
            }

            // 5.6. 外見情報
            if (request.playerAppearance != null && !string.IsNullOrEmpty(request.playerAppearance.appearanceDescription))
            {
                prompt.AppendLine($"- {(isJapanese ? "プレイヤーの外見" : "Player appearance")}: {request.playerAppearance.appearanceDescription}");
            }

            // 5.65. Stage 10: リアルタイム表情データ
            var facialAnalyzer = FacialExpressionAnalyzer.Instance;
            if (facialAnalyzer != null && facialAnalyzer.IsActive)
            {
                var facial = facialAnalyzer.CurrentState;
                if (facial.confidence > 0.4f)
                {
                    string exprName = isJapanese
                        ? FacialExpressionAnalyzer.GetExpressionNameJP(facial.currentExpression)
                        : FacialExpressionAnalyzer.GetExpressionNameEN(facial.currentExpression);
                    prompt.AppendLine($"- {(isJapanese ? "プレイヤーの表情" : "Player facial expression")}: {exprName} ({facial.confidence:F2})");
                }
            }

            // 5.75. クロスターンナラティブ
            if (GameManager.Instance != null)
            {
                string narrative = GameManager.Instance.GetNarrativeSummary();
                if (!string.IsNullOrEmpty(narrative))
                {
                    prompt.AppendLine(isJapanese ? "ゲーム中の重要な出来事:" : "Key events during the game:");
                    prompt.AppendLine(narrative);
                }
            }
            prompt.AppendLine();

            // 6. 生成指示
            if (isJapanese)
            {
                if (request.layer == FPSTrump.Psychology.ResponseLayer.B)
                    prompt.AppendLine("この感情を1文（60文字以内）のおしゃべりなピエロの台詞で表現。感情は大げさに、でも時々本音が漏れる。");
                else if (request.layer == FPSTrump.Psychology.ResponseLayer.C)
                    prompt.AppendLine("この感情を2-3文（200文字以内）で表現。ピエロの仮面が一瞬ずれて、素の感情が見える瞬間を作れ。");
            }
            else
            {
                if (request.layer == FPSTrump.Psychology.ResponseLayer.B)
                    prompt.AppendLine("Express this emotion in one short sentence (under 15 words) as a chatty clown. Over-the-top, but real feelings slip through.");
                else if (request.layer == FPSTrump.Psychology.ResponseLayer.C)
                    prompt.AppendLine("Express this in 2-3 sentences (under 50 words). The clown's mask slips — show a flash of raw, genuine emotion beneath the act.");
            }

            // 7. 制約（最重要）
            prompt.AppendLine();
            if (isJapanese)
            {
                prompt.AppendLine("制約:");
                prompt.AppendLine("- カードの内容（ジョーカー、数字、スート）には一切言及しない");
                prompt.AppendLine("- 確率や期待値には言及しない");
                prompt.AppendLine("- 「ランダム」という概念には触れない");
                prompt.AppendLine("- 感情と対人関係だけで表現する");
                prompt.AppendLine("- 台詞のみを出力（説明やタグ不要）");
            }
            else
            {
                prompt.AppendLine("Constraints:");
                prompt.AppendLine("- NEVER mention card contents (Joker, numbers, suits)");
                prompt.AppendLine("- NEVER mention probability or expected values");
                prompt.AppendLine("- NEVER reference randomness");
                prompt.AppendLine("- Express through emotion and interpersonal dynamics only");
                prompt.AppendLine("- Output ONLY the dialogue (no explanations or tags)");
            }

            return prompt.ToString();
        }

        /// <summary>
        /// トークン数を概算（簡易版）
        /// </summary>
        public int EstimateTokenCount(string text)
        {
            // 簡易推定: 英語は4文字/トークン、日本語は1.5文字/トークン
            int englishChars = text.Count(c => c <= 127);
            int japaneseChars = text.Length - englishChars;

            int englishTokens = englishChars / 4;
            int japaneseTokens = (int)(japaneseChars / 1.5f);

            return englishTokens + japaneseTokens;
        }
    }
}
