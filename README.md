# BLUFF MIRROR

[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)

> A psychological card game where AI reads you, not the cards.

[🇯🇵 日本語版はこちら](#日本語版)

---

## Challenge Track

**AI Game**

This project is an entirely new game that integrates Generative AI (GenAI) not just in the development process, but as the core of the game experience itself. The AI is designed as the central element of the experience, not as an assistant or NPC.

---

## Concept

**"This isn't a game where you beat the AI—it's a game where the AI reads you."**

While based on the card game Old Maid, the real opponent you face isn't luck with the cards.

The AI never sees the cards. Instead, it continuously observes the player's:

- Hesitation patterns
- Habit of repeating choices
- Reactions when interrupted
- Decision speed

And the AI throws emotionally-charged **assertions (bluffs)** like:
"I knew you'd pick that one."

---

## The Experience

**The Ultimate Emotion We Want Players to Feel**

> "The AI didn't trick me.
> It read how I choose."

Players gradually become aware, not of "which card is dangerous," but of "what kind of person am I being when I choose?"

At the end of the game, the AI performs a "playstyle diagnosis" based on action logs from the session.

This isn't a score—it's **a result as self-understanding**.

---

## Why Only AI Can Do This

**Why This Is Impossible with Traditional Game AI or Scripts**

- AI doesn't manipulate card results
- AI doesn't present probabilities or optimal solutions
- AI doesn't try to win

Instead, the AI:

1. Observes player actions in real-time
2. Gives meaning to those actions
3. Returns them as emotionally-charged assertions

This loop of "observation → interpretation → emotional assertion" is only possible with Generative AI (LLM).

A human GM would be exhausted,
Rule-based AI cannot generate meaning,
Scripts cannot create player-specific variations.

---

## Design Philosophy: The Bluff System

**"AI lies, but never cheats"**

The core design principle is the AI's bluff system:

- AI never lies about card facts
- AI never predicts the future

AI only lies about **its own emotions and expectations**.

Examples:
- "I thought you wouldn't draw that card."
- "I expected you to stop there."
- "I didn't see that coming."

How players react to these "emotional lies" determines whether the AI:

- Rejoices
- Gets angry
- Feels sad
- Shows frustration

Not the card results, but **player choices move AI emotions**—this structure is the core design commitment.

---

## Technical Approach (Summary)

- FPS-perspective immersive card game built in Unity
- Generative AI (LLM) is used only for:
  - Bluff verbalization
  - Emotional reactions
  - Result diagnosis generation
- Card dealing, win/loss, and discard logic are entirely rule-based
  → No manipulation or cheating
- AI voice, expressions, and pauses (including silence) are combined to create the illusion of "an AI thinking while speaking"

---

## AI Integration Details

### AI Tools & Technologies Used

This project combines multiple AI technologies to create the game experience:

#### 1. Claude API (Anthropic)
**Implementation**: `Scripts/AI/LLM/ClaudeAPIClient.cs`, `Scripts/AI/LLM/LLMManager.cs`

**Usage**:
- **Chain-of-Thought (CoT) Reasoning**: AI observes player behavior patterns and deduces "which card they'll likely pick" step-by-step
- **Emotional Bluff Generation**: Creates emotionally-charged reaction dialogues based on player actions
- **Appearance Cold Reading (Vision API)**: Analyzes player's appearance via webcam at game start and reflects it in the first dialogue
- **Result Diagnosis**: Generates personality diagnosis report from complete action logs at game end

**Design Intent**:
- LLM's contextual understanding integrates player behavior history, emotional states, and birthday data to realize an AI that "observes the player"
- Vision API enables cold reading experience by incorporating appearance information
- CoT format visualizes AI's thought process, enhancing credibility of "thinking AI"

#### 2. OpenAI API
**Implementation**: `Scripts/AI/LLM/OpenAIAPIClient.cs`

**Usage**:
- **TTS Voice Synthesis**: Audio generation using model `tts-1` with voices `Onyx`/`Echo`
- **LLM Response Backup**: Fallback when Claude API fails

**Design Intent**:
- Real-time voice generation instantly vocalizes AI dialogues
- OpenAI TTS has fast response time, maintaining game tempo

#### 3. ElevenLabs API (Optional)
**Implementation**: `Scripts/AI/LLM/ElevenLabsAPIClient.cs`, `Scripts/AI/LLM/ElevenLabsVoiceSettings.cs`

**Usage**:
- **Emotional Expression TTS**: Dynamically adjusts `stability`, `similarity_boost`, `style` parameters based on emotional state (`AIEmotion`)
- Model: `eleven_multilingual_v2`

**Design Intent**:
- Higher quality than OpenAI TTS with richer emotional inflection
- Reflects AI's emotional changes (joy, anger, sadness) in voice, enhancing immersion
- Automatically falls back to OpenAI TTS if not configured (not required)

#### 4. Unity Sentis + FERPlus-8 (ONNX)
**Implementation**: `Scripts/Camera/FacialExpressionAnalyzer.cs`

**Usage**:
- **Real-time Facial Expression Recognition**: Classifies player expressions into 8 categories via webcam
  - Categories: Neutral, Happy, Surprise, Sad, Angry, Disgusted, Fearful, Contempt
- **Behavior-Expression Mismatch Detection**: Evaluates contradictions between expressions and choices as psychological pressure

**Design Intent**:
- AI reads mismatches like "smiling while picking Joker" or "anxious expression with quick decision"
- Expression data is injected into LLM prompts for mentalist-style dialogue generation
- Fully local operation (ONNX model) ensures both privacy protection and response speed

#### 5. WebCam + Unity Sentis Integration
**Implementation**: `Scripts/Camera/WebCamManager.cs`

**Usage**:
- **Camera Capture**: Captures player at game start and key moments
- **Claude Vision API Integration**: Sends Base64-encoded images to Claude for appearance comments
- **Real-time Expression Analysis**: Supplies textures to Sentis worker

**Design Intent**:
- Enables "first-meeting cold reading" experience at game start
- Continuously monitors player's facial changes to estimate psychological state

---

### AI Usage Map

| **Game Phase** | **AI Technology** | **Implementation Class** | **Purpose** |
|:---|:---|:---|:---|
| **Title Screen** | - | - | No AI (static UI) |
| **Birthday Input** | Four Pillars & Numerology (Rule-based) | `BirthdayFortuneUtil.cs` | Generate initial player personality prediction data |
| **Game Start (Intro)** | Claude API (Vision) | `GameIntroSequence.cs` | Analyze player appearance via webcam, generate cold reading dialogue |
| **Game Start (Intro)** | Claude API + OpenAI/ElevenLabs TTS | `GameIntroSequence.cs` | Pre-generate 4 intro dialogues in parallel before game starts |
| **AI Turn Start** | Claude API (CoT) + Unity Sentis | `LLMManager.cs`, `AIHesitationController.cs` | Deduce "which card player will pick" from expression/action history in CoT format |
| **AI Turn - Reasoning Display** | OpenAI/ElevenLabs TTS | `AIHesitationController.cs` | Generate TTS per CoT step, display reasoning process via voice + subtitles |
| **Player Turn - Card Selection** | Unity Sentis (FERPlus-8) | `FacialExpressionAnalyzer.cs` | Analyze player expression every 0.5 seconds |
| **Player Turn - Confirmation** | Claude API | `BluffSystem.cs` | Generate emotional reaction dialogue from player's choice, expression, pressure level |
| **Player Turn - Confirmation** | OpenAI/ElevenLabs TTS | `BluffSystem.cs` | Replay cached Layer A (filler), dynamically generate Layer B/C |
| **Bluff Action** | Claude API | `BluffActionSystem.cs` | AI reacts to player's bluff actions (card shuffle, etc.) |
| **Game End (Outro)** | Claude API + OpenAI/ElevenLabs TTS | `GameOutroSequence.cs` | Pre-generate 4 dialogues based on win/loss right after game ends |
| **Result Diagnosis** | Claude API | `ResultDiagnosisSystem.cs` | Integrate all turn history, bluff stats, expression data, birthday to generate personality diagnosis report |
| **Result Diagnosis - Detailed Analysis** | Claude API | `ResultDiagnosisSystem.cs` | After static diagnosis display, generate additional LLM analysis in background (hybrid approach) |

---

### Three AI Design Principles

#### 1. AI Never Sees the Cards
- Card contents or positions are never passed to AI
- AI only observes **player behavior patterns**
- Maintains game fairness while achieving psychological warfare credibility

#### 2. AI Lies Only About Emotions
- Never lies about card facts
- AI only lies about "its own expectations and emotions"
- Example: "I didn't expect you to do that" (might have actually predicted it)
- Creates structure where player actions move AI emotions

#### 3. Hybrid Approach for Zero Wait Time
- **Intro/Outro**: Pre-generate 4 dialogues in parallel right after game start/end
- **AI Reactions**: Pool filler TTS at game start, play immediately during LLM generation
- **Result Diagnosis**: Display static diagnosis instantly + add LLM analysis in background
- Maintains tempo without exposing LLM generation delay

---

### Privacy & Data Handling

**Local Processing**:
- Facial recognition (Unity Sentis): Fully local, no external image data transmission
- Birthday calculation (Four Pillars & Numerology): Local computation only

**External API Data Transmission**:
- Claude Vision API: Sends only 1 image at game start (for appearance comment generation)
- Claude/OpenAI LLM: Sends only player action logs (card selection position, timing, expression classification results)
  - **Card contents are NOT sent** (card numbers and Joker positions are excluded)
- TTS API: Sends only generated dialogue text

**Data Storage**:
- API keys managed locally in `.env` file (not committed to Git)
- Game session data stored only in local memory (no external storage)

---

## What We Challenged with This Project

- Not making AI an enemy or target to conquer
- Making AI an "interpreting presence"
- Creating a game experience that uses the player themselves, not just win/loss

---

## In One Sentence

> "This is a game where the AI doesn't read the cards—it reads you."

---

## About Open Source

This project is published under the MIT License and is designed as a reference implementation for placing Generative AI at the center of a game experience, intended for reuse and extension by other developers.

---

## Setup Guide

### Requirements

- **Unity**: 6000.0.x (Unity 6 LTS) or higher
- **Universal Render Pipeline (URP)**: 17.0 or higher
- **Cinemachine**: 3.x
- **Unity Sentis**: 2.1.3 or higher
- **Live2D Cubism SDK**: 5-r.5-beta.3
- **API Keys**:
  - Claude API (Required)
  - OpenAI API (Required)
  - ElevenLabs API (Optional - for high-quality TTS)

### Obtaining API Keys

This game uses three API services:

1. **Claude API** (Anthropic)
   - Purpose: AI personality, appearance recognition (Vision API), result diagnosis
   - How to get: Create an API key at [Anthropic Console](https://console.anthropic.com/)
   - Format: `sk-ant-api03-...`

2. **OpenAI API**
   - Purpose: LLM response generation, TTS voice synthesis
   - How to get: Create an API key at [OpenAI Platform](https://platform.openai.com/api-keys)
   - Format: `sk-proj-...` or `sk-...`

3. **ElevenLabs API** (Optional)
   - Purpose: High-quality emotional TTS
   - How to get: Create an account at [ElevenLabs](https://elevenlabs.io/) and get an API key
   - Format: 32-character alphanumeric string
   - Note: Falls back to OpenAI TTS if not configured

### Installation Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/Conata/BluffMirror.git
   cd BluffMirror
   ```

2. **Configure API Keys**

   Create a `.env` file in the project root:
   ```bash
   cp .env.example .env
   ```

   Edit the `.env` file and add your API keys:
   ```env
   # Claude API Key (Required)
   CLAUDE_API_KEY=sk-ant-api03-YOUR_KEY_HERE

   # OpenAI API Key (Required)
   OPENAI_API_KEY=sk-proj-YOUR_KEY_HERE

   # ElevenLabs API Key (Optional)
   ELEVEN_API_KEY=YOUR_KEY_HERE
   ```

   **Important**: The `.env` file is included in `.gitignore` and will not be committed to Git.

3. **Open the project in Unity**
   - Open the `Baba/Baba` folder in Unity Hub
   - Use Unity 6 LTS (6000.0.x) or higher

4. **Open the scene**
   - Open `Assets/Scenes/StartMenuScene.unity`

5. **Run the game**
   - Press the Play button in Unity Editor to start the game
   - API keys will be automatically loaded on first launch

### Verifying API Keys

During gameplay, the API key status is displayed in the bottom-left corner:
- ✅ Loaded: Key is successfully loaded
- ❌ Not set: Key is not configured

### Troubleshooting

**If API keys are not loading**:
1. Ensure the `.env` file is in the project root directory
2. Verify the `.env` file format is correct (`KEY=VALUE` format, no quotes needed)
3. Restart Unity Editor

**If the camera doesn't start**:
- macOS: Check System Preferences > Security & Privacy > Camera for Unity permissions
- Windows: Check Settings > Privacy > Camera for Unity permissions

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div id="日本語版"></div>

# 日本語版

## 選択したチャレンジパス

**AIゲーム**

このプロジェクトは、開発プロセスだけでなくゲーム体験そのものに生成AI（GenAI）を組み込んだ完全に新しいゲームです。
AIは補助ツールやNPCではなく、ゲーム体験の中心的存在として設計されています。

---

## コンセプト

**「AIに勝つゲームではなく、AIに"当てられてしまう"ゲーム」**

このゲームはカードゲーム（ババ抜き）をベースにしていますが、
プレイヤーが対峙する本当の相手はカード運ではありません。

AIはカードの中身を一切見ません。
代わりに、プレイヤーの

- 迷い方
- 同じ選択を繰り返す癖
- 止められたときの反応
- 決断の速度

といった行動パターンだけを観察し続けます。

そしてAIは、
「君ならこう選ぶはずだ」
という**断定（ブラフ）**を感情付きで投げかけてきます。

---

## 提供する体験

**ユーザーに起きてほしい最高の感情**

> 「AIに騙されたのではない。
> 自分の選び方を見抜かれた。」

プレイヤーは次第に、
「どのカードが危険か」よりも
「自分は今、どういう人間として選んでいるのか」
を意識し始めます。

ゲームの終わりには、
プレイ中の行動ログをもとに
AIがプレイヤーの"プレイスタイル診断"を行います。

これはスコアではなく、
**自己理解としてのリザルト**です。

---

## AIにしかできない理由

**なぜこれは従来のゲームAIや台本では不可能か**

- AIはカード結果を操作しない
- AIは確率や最適解を提示しない
- AIは勝とうとしない

代わりにAIは、

1. プレイヤーの行動をリアルタイムで観察し
2. その行動に意味を与え
3. 感情を伴った断定として返し続けます

この「観察 → 解釈 → 感情的断定」のループは、生成AI（LLM）でなければ成立しません。

人間GMでは疲弊し、
ルールベースAIでは意味を生成できず、
台本ではプレイヤーごとの差分が作れません。

---

## ブラフシステムへのこだわり

**AIは「嘘をつく」が、「ズルはしない」**

本作の最大のこだわりは、AIのブラフ設計です。

- AIはカードの事実について嘘をつきません
- AIは未来を予言しません

AIが嘘をつくのは、**自分の感情や期待についてだけ**です。

例：
- 「君なら、引かないと思った」
- 「今はやめるはずだと思った」
- 「そう来るとは思わなかった」

この"感情の嘘"に対して
プレイヤーがどう行動するかで、

- AIは喜び
- 怒り
- 悲しみ
- 悔しがります

カード結果ではなく、
**プレイヤーの選択がAIの感情を動かす**
という構造に強くこだわりました。

---

## 技術的アプローチ（要点）

- UnityによるFPS視点の没入型カードゲーム
- 生成AI（LLM）は以下のみに使用
  - ブラフの言語化
  - 感情リアクション
  - リザルト診断生成
- カード配布・勝敗・アウト判定は完全にロジックで管理
  → 不正・操作は一切なし
- AIの音声・表情・間（沈黙含む）を組み合わせ、
  「考えながら話しているAI」を演出

---

## AI統合の詳細

### 使用しているAIツール・技術

本プロジェクトでは、複数のAI技術を組み合わせてゲーム体験を構築しています：

#### 1. Claude API (Anthropic)
**実装箇所**: `Scripts/AI/LLM/ClaudeAPIClient.cs`, `Scripts/AI/LLM/LLMManager.cs`

**用途**:
- **Chain-of-Thought (CoT) 推理**: AIがプレイヤーの行動パターンを観察し、「どのカードを選びそうか」を段階的に推理
- **感情付きブラフ生成**: プレイヤーの行動に対する感情的なリアクション台詞の生成
- **外見コールドリーディング (Vision API)**: ゲーム開始時にWebカメラでプレイヤーの外見を分析し、初回台詞に反映
- **リザルト診断**: ゲーム終了後、全行動ログから性格診断レポートを生成

**意図**:
- LLMの文脈理解能力により、プレイヤーの行動履歴・感情状態・生年月日データを統合し、「プレイヤーを観察している存在」としてのAIを実現
- Vision APIで外見情報を含めることで、初対面でのコールドリーディング体験を再現
- CoT形式でAIの思考プロセスを可視化し、「考えながら話すAI」の説得力を向上

#### 2. OpenAI API
**実装箇所**: `Scripts/AI/LLM/OpenAIAPIClient.cs`

**用途**:
- **TTS音声合成**: モデル `tts-1`、ボイス `Onyx`/`Echo` による音声生成
- **LLMバックアップ**: Claude API障害時のフォールバック

**意図**:
- リアルタイム音声生成により、AIの台詞を即座に音声化
- OpenAI TTSはレスポンスが速く、ゲームテンポを維持しやすい

#### 3. ElevenLabs API (オプション)
**実装箇所**: `Scripts/AI/LLM/ElevenLabsAPIClient.cs`, `Scripts/AI/LLM/ElevenLabsVoiceSettings.cs`

**用途**:
- **感情表現TTS**: 感情状態 (`AIEmotion`) に応じて `stability`, `similarity_boost`, `style` パラメータを動的調整
- モデル: `eleven_multilingual_v2`

**意図**:
- OpenAI TTSより高品質で感情の起伏が豊かな音声を生成
- 喜び・怒り・悲しみなどAIの感情変化を音声表現に反映し、没入感を向上
- 未設定時はOpenAI TTSに自動フォールバック（必須ではない）

#### 4. Unity Sentis + FERPlus-8 (ONNX)
**実装箇所**: `Scripts/Camera/FacialExpressionAnalyzer.cs`

**用途**:
- **リアルタイム表情認識**: Webカメラでプレイヤーの表情を8クラス分類
  - 分類: Neutral, Happy, Surprise, Sad, Angry, Disgusted, Fearful, Contempt
- **行動・表情の不一致検出**: 表情と選択行動の矛盾を心理圧力として評価

**意図**:
- プレイヤーが「笑いながらJokerを選ぶ」「焦った表情で素早く決断」など、表情と行動の一致/不一致をAIが読み取る
- LLMプロンプトに表情データを注入し、メンタリスト的な台詞生成に活用
- 完全にローカル動作（ONNXモデル）のため、プライバシー保護とレスポンス速度を両立

#### 5. WebCam + Unity Sentis統合
**実装箇所**: `Scripts/Camera/WebCamManager.cs`

**用途**:
- **カメラキャプチャ**: ゲーム開始時とキーモーメントでプレイヤーをキャプチャ
- **Claude Vision API連携**: Base64エンコードされた画像をClaudeに送信し、外見コメント生成
- **リアルタイム表情解析**: Sentisワーカーへのテクスチャ供給

**意図**:
- ゲーム開始時の「初対面コールドリーディング」体験を実現
- プレイヤーの表情変化を継続的に監視し、心理状態の推定に利用

---

### AI使用箇所マップ

| **ゲームフェーズ** | **AI技術** | **実装クラス** | **目的** |
|:---|:---|:---|:---|
| **タイトル画面** | - | - | AI未使用（静的UI） |
| **生年月日入力** | 四柱推命・数秘術 (ルールベース) | `BirthdayFortuneUtil.cs` | プレイヤー性格予測の初期データ生成 |
| **ゲーム開始 (イントロ)** | Claude API (Vision) | `GameIntroSequence.cs` | Webカメラでプレイヤーの外見を分析し、コールドリーディング台詞を生成 |
| **ゲーム開始 (イントロ)** | Claude API + OpenAI/ElevenLabs TTS | `GameIntroSequence.cs` | 4つのイントロ台詞をプリ生成（ゲーム開始前に並列生成） |
| **AIターン開始** | Claude API (CoT) + Unity Sentis | `LLMManager.cs`, `AIHesitationController.cs` | プレイヤーの表情・行動履歴から「どのカードを選びそうか」をCoT形式で推理 |
| **AIターン - 推理表示** | OpenAI/ElevenLabs TTS | `AIHesitationController.cs` | CoTステップごとにTTS生成し、推理プロセスを音声＋字幕で表示 |
| **プレイヤーターン - カード選択中** | Unity Sentis (FERPlus-8) | `FacialExpressionAnalyzer.cs` | プレイヤーの表情を0.5秒間隔で分析 |
| **プレイヤーターン - 確認時** | Claude API | `BluffSystem.cs` | プレイヤーの選択・表情・圧力レベルから感情リアクション台詞を生成 |
| **プレイヤーターン - 確認時** | OpenAI/ElevenLabs TTS | `BluffSystem.cs` | Layer A（フィラー）をキャッシュ再生、Layer B/Cを動的生成 |
| **ブラフアクション時** | Claude API | `BluffActionSystem.cs` | プレイヤーのブラフ行動（カードシャッフル等）にAIがリアクション |
| **ゲーム終了 (アウトロ)** | Claude API + OpenAI/ElevenLabs TTS | `GameOutroSequence.cs` | 勝敗に応じた4つの台詞をプリ生成（ゲーム終了直後に並列生成） |
| **リザルト診断** | Claude API | `ResultDiagnosisSystem.cs` | 全ターン履歴・ブラフ統計・表情データ・生年月日を統合し、性格診断レポート生成 |
| **リザルト診断 - 詳細分析** | Claude API | `ResultDiagnosisSystem.cs` | 静的診断表示後、バックグラウンドでLLM詳細分析を追加生成（ハイブリッド方式） |

---

### AI設計の3つの原則

#### 1. AIはカードを見ない
- カードの中身や配置を一切AIに渡さない
- AIが観察するのは**プレイヤーの行動パターンのみ**
- ゲーム公平性を保ちながら、心理戦の説得力を実現

#### 2. AIは感情についてのみ嘘をつく
- カード事実については嘘をつかない
- AIが嘘をつくのは「自分の期待や感情」のみ
- 例: 「そう来るとは思わなかった」（実際は予測していたかもしれない）
- プレイヤー行動がAI感情を動かす構造を作る

#### 3. ハイブリッド方式で待機時間をゼロ化
- **イントロ/アウトロ**: ゲーム開始・終了直後に4台詞を並列プリ生成
- **AIリアクション**: フィラーTTSをゲーム開始時にプール、LLM生成中に即再生
- **リザルト診断**: 静的診断を即座に表示 + バックグラウンドでLLM分析を追加
- LLM生成の遅延を感じさせず、テンポを維持

---

### プライバシーとデータ取り扱い

**ローカル処理**:
- 表情認識 (Unity Sentis): 完全にローカル動作、画像データは外部送信なし
- 生年月日計算 (四柱推命・数秘術): ローカル計算のみ

**外部API送信データ**:
- Claude Vision API: ゲーム開始時のみ1枚の画像を送信（外見コメント生成用）
- Claude/OpenAI LLM: プレイヤーの行動ログ（カード選択位置、時間、表情分類結果）のみ送信
  - **カード内容は送信しない**（カード番号やJoker位置は含まれない）
- TTS API: 生成した台詞テキストのみ送信

**データ保存**:
- APIキーは `.env` ファイルでローカル管理（Gitにコミットされない）
- ゲームセッションデータはローカルメモリのみ（外部保存なし）

---

## このプロジェクトで挑戦したこと

- AIを敵や攻略対象にしない
- AIを"解釈する存在"にする
- 勝ち負けではなく、プレイヤー自身を素材にしたゲーム体験を作ること

---

## 一文で表すなら

> 「このゲームは、AIがカードを読むのではなく、
> プレイヤーを読んでしまうゲームです。」

---

## オープンソースについて

本プロジェクトはMITライセンスのもとで公開され、
生成AIをゲーム体験の中心に据える設計例として
他の開発者が再利用・拡張できることを意識しています。

---

## セットアップガイド

### 必要環境

- **Unity**: 6000.0.x (Unity 6 LTS) 以上
- **Universal Render Pipeline (URP)**: 17.0 以上
- **Cinemachine**: 3.x
- **Unity Sentis**: 2.1.3 以上
- **Live2D Cubism SDK**: 5-r.5-beta.3
- **API Keys**:
  - Claude API (必須)
  - OpenAI API (必須)
  - ElevenLabs API (オプション - 高品質TTS用)

### APIキーの取得

このゲームは3つのAPIサービスを使用します：

1. **Claude API** (Anthropic)
   - 用途: AIの性格形成、外見認識（Vision API）、リザルト診断
   - 取得方法: [Anthropic Console](https://console.anthropic.com/) でAPIキーを作成
   - フォーマット: `sk-ant-api03-...`

2. **OpenAI API**
   - 用途: LLM応答生成、TTS音声合成
   - 取得方法: [OpenAI Platform](https://platform.openai.com/api-keys) でAPIキーを作成
   - フォーマット: `sk-proj-...` または `sk-...`

3. **ElevenLabs API** (オプション)
   - 用途: 高品質な感情表現TTS
   - 取得方法: [ElevenLabs](https://elevenlabs.io/) でアカウント作成後、APIキーを取得
   - フォーマット: 32文字の英数字
   - 注意: 設定しない場合はOpenAI TTSにフォールバックします

### インストール手順

1. **リポジトリをクローン**
   ```bash
   git clone https://github.com/Conata/BluffMirror.git
   cd BluffMirror
   ```

2. **APIキーを設定**

   プロジェクトルートに `.env` ファイルを作成してください：
   ```bash
   cp .env.example .env
   ```

   `.env` ファイルを編集し、取得したAPIキーを設定：
   ```env
   # Claude API Key (必須)
   CLAUDE_API_KEY=sk-ant-api03-YOUR_KEY_HERE

   # OpenAI API Key (必須)
   OPENAI_API_KEY=sk-proj-YOUR_KEY_HERE

   # ElevenLabs API Key (オプション)
   ELEVEN_API_KEY=YOUR_KEY_HERE
   ```

   **重要**: `.env` ファイルは `.gitignore` に含まれており、Gitにコミットされません。

3. **Unityでプロジェクトを開く**
   - Unity Hub で `Baba/Baba` フォルダを開く
   - Unity 6 LTS (6000.0.x) 以上を使用してください

4. **シーンを開く**
   - `Assets/Scenes/StartMenuScene.unity` を開いてください

5. **ゲームを起動**
   - Unityエディタで再生ボタンを押してゲームを開始
   - 初回起動時、APIキーが自動的に読み込まれます

### APIキーの確認

ゲーム実行中、左下にAPIキーの読み込み状態が表示されます：
- ✅ Loaded: キーが正常に読み込まれています
- ❌ Not set: キーが設定されていません

### トラブルシューティング

**APIキーが読み込まれない場合**:
1. `.env` ファイルがプロジェクトルートにあることを確認
2. `.env` ファイルのフォーマットが正しいことを確認（`KEY=VALUE` 形式、クォート不要）
3. Unityエディタを再起動

**カメラが起動しない場合**:
- macOS: システム環境設定 > セキュリティとプライバシー > カメラ で Unity の許可を確認
- Windows: 設定 > プライバシー > カメラ で Unity の許可を確認

---

## ライセンス

本プロジェクトはMITライセンスの下で公開されています。詳細は [LICENSE](LICENSE) ファイルをご覧ください。

---

## Third-Party Assets & Licenses

This project includes the following third-party assets and libraries:

### Libraries & SDKs

#### DOTween
- **Copyright**: (c) 2014-2018 Daniele Giardini - Demigiant
- **License**: [DOTween License](http://dotween.demigiant.com/license.php)
- **Usage**: Animation library (free version, used unmodified)
- **Location**: `Baba/Assets/Plugins/Demigiant/DOTween/`

#### Live2D Cubism SDK
- **Version**: 5-r.5-beta.3
- **License**: [Live2D Open Software License](https://www.live2d.com/en/terms/live2d-open-software-license-agreement/)
- **Usage**: Character animation and rendering
- **Location**: `Baba/Assets/Live2D/Cubism/`

#### FERPlus-8 ONNX Model
- **Copyright**: Microsoft Corporation
- **License**: MIT License
- **Source**: [microsoft/FERPlus](https://github.com/microsoft/FERPlus)
- **Usage**: Facial expression recognition (8-class classification)
- **Location**: `Baba/Assets/StreamingAssets/Models/emotion-ferplus-8.onnx`

### Unity Packages

The following Unity packages are used in this project:
- Unity 6 LTS (6000.0.x)
- Universal Render Pipeline (URP) 17.0.3
- Cinemachine 3.1.5
- Unity Sentis 2.1.3
- Newtonsoft.Json 3.2.1
- Input System 1.11.2
- Timeline 1.8.7
- AI Navigation 2.0.4
- Other standard Unity modules

All Unity packages are subject to their respective Unity licenses.

### External APIs

This project uses the following external APIs:
- **Claude API** (Anthropic) - LLM, Vision API, personality generation
- **OpenAI API** - LLM, TTS (text-to-speech)
- **ElevenLabs API** (Optional) - High-quality emotional TTS

API usage is subject to each provider's terms of service.

### AI-Generated Assets

The following assets were generated using AI tools during the hackathon:
- Music files (`darkmode.mp3`, `DeepMode.mp3`, `breath.mp3`)
- Sound effects (card interaction sounds)
- 3D models (TVHead character model)
- Card textures (`back.png`, `backred.png`)

---

## サードパーティライセンス

本プロジェクトは以下のサードパーティライブラリ・アセットを使用しています：

### ライブラリ・SDK

#### DOTween
- **著作権**: (c) 2014-2018 Daniele Giardini - Demigiant
- **ライセンス**: [DOTween License](http://dotween.demigiant.com/license.php)
- **用途**: アニメーションライブラリ（無料版、未修正で使用）
- **場所**: `Baba/Assets/Plugins/Demigiant/DOTween/`

#### Live2D Cubism SDK
- **バージョン**: 5-r.5-beta.3
- **ライセンス**: [Live2D Open Software License](https://www.live2d.com/ja/terms/live2d-open-software-license-agreement/)
- **用途**: キャラクターアニメーション・レンダリング
- **場所**: `Baba/Assets/Live2D/Cubism/`

#### FERPlus-8 ONNX モデル
- **著作権**: Microsoft Corporation
- **ライセンス**: MIT License
- **ソース**: [microsoft/FERPlus](https://github.com/microsoft/FERPlus)
- **用途**: 表情認識（8クラス分類）
- **場所**: `Baba/Assets/StreamingAssets/Models/emotion-ferplus-8.onnx`

### Unity パッケージ

以下のUnityパッケージを使用しています：
- Unity 6 LTS (6000.0.x)
- Universal Render Pipeline (URP) 17.0.3
- Cinemachine 3.1.5
- Unity Sentis 2.1.3
- Newtonsoft.Json 3.2.1
- Input System 1.11.2
- Timeline 1.8.7
- AI Navigation 2.0.4
- その他の標準Unityモジュール

全てのUnityパッケージは各々のUnityライセンスに準拠しています。

### 外部API

本プロジェクトは以下の外部APIを使用しています：
- **Claude API** (Anthropic) - LLM、Vision API、性格生成
- **OpenAI API** - LLM、TTS（音声合成）
- **ElevenLabs API**（オプション） - 高品質感情表現TTS

API使用は各プロバイダーの利用規約に準拠しています。

### AI生成アセット

以下のアセットはハッカソン期間中にAIツールを使用して生成されました：
- 音楽ファイル（`darkmode.mp3`, `DeepMode.mp3`, `breath.mp3`）
- 効果音（カード操作音など）
- 3Dモデル（TVHeadキャラクターモデル）
- カードテクスチャ（`back.png`, `backred.png`）
