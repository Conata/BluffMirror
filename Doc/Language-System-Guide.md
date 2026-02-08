# 言語システム セットアップガイド

このガイドでは、日英バイリンガル対応の音声システムをUnity Editorでセットアップする方法を説明します。

## 📋 システム概要

### 機能
1. **自動言語検出**: 初回起動時、OSの言語設定を自動検出
2. **手動切り替え**: StartSceneで言語を切り替え可能
3. **設定の永続化**: PlayerPrefsで言語設定を保存
4. **リアルタイム反映**: 言語変更が即座にゲーム内で反映

### 対応言語
- 英語 (English)
- 日本語 (Japanese)

---

## 🚀 Unity Editor セットアップ

### 自動セットアップ（推奨）

**メニュー: Tools → Setup Audio System** を使用すると、以下が自動で実行されます：

1. ✅ GameSettings オブジェクトの作成
2. ✅ AudioManager への音声ファイル自動割り当て（28個）
3. ✅ 言語切り替えボタンの作成（Canvas右上）

**使い方:**
1. Unity Editor で **Tools → Setup Audio System** を開く
2. **🚀 自動セットアップを実行** ボタンをクリック
3. 完了！

---

### 手動セットアップ（オプション）

自動セットアップを使わない場合、以下の手順で手動設定できます：

#### Step 1: GameSettings オブジェクトの作成

1. **Hierarchyで右クリック** → Create Empty
2. 名前を **"GameSettings"** に変更
3. **GameSettings.cs** スクリプトをアタッチ

**重要な設定:**
- **Don't Destroy On Load**: ✓（スクリプト内で自動設定）
- このオブジェクトは全シーンで永続化されます

---

#### Step 2: AudioManager の音声クリップ設定

#### 2-1. Hierarchyで AudioManager を選択

#### 2-2. AI Voice Clips (English) セクション

| フィールド | 設定するファイル | 配列サイズ |
|----------|----------------|---------|
| **Game Start Voices EN** | `game_start_1.mp3`, `game_start_2.mp3` | 2 |
| **Card Draw Voices EN** | `card_draw_1.mp3`, `card_draw_2.mp3`, `card_draw_3.mp3` | 3 |
| **Pair Match Voices EN** | `pair_match_1.mp3`, `pair_match_2.mp3` | 2 |
| **Victory Voices EN** | `victory_1.mp3`, `victory_2.mp3` | 2 |
| **Defeat Voices EN** | `defeat_1.mp3`, `defeat_2.mp3` | 2 |
| **Pressure Voices EN** | `pressure_1.mp3`, `pressure_2.mp3`, `pressure_3.mp3` | 3 |

#### 2-3. AI Voice Clips (Japanese) セクション

| フィールド | 設定するファイル | 配列サイズ |
|----------|----------------|---------|
| **Game Start Voices JA** | `game_start_1_ja.mp3`, `game_start_2_ja.mp3` | 2 |
| **Card Draw Voices JA** | `card_draw_1_ja.mp3`, `card_draw_2_ja.mp3`, `card_draw_3_ja.mp3` | 3 |
| **Pair Match Voices JA** | `pair_match_1_ja.mp3`, `pair_match_2_ja.mp3` | 2 |
| **Victory Voices JA** | `victory_1_ja.mp3`, `victory_2_ja.mp3` | 2 |
| **Defeat Voices JA** | `defeat_1_ja.mp3`, `defeat_2_ja.mp3` | 2 |
| **Pressure Voices JA** | `pressure_1_ja.mp3`, `pressure_2_ja.mp3`, `pressure_3_ja.mp3` | 3 |

---

#### Step 3: StartScene UI の設定（自動セットアップで作成済み）

**自動セットアップを使用した場合、このステップはスキップできます。**

以下は手動で作成する場合の手順です：

**3-1. Language Switcher ボタンの作成**

1. **Hierarchy** → StartScene Canvas
2. 右クリック → UI → Button - TextMeshPro
3. 名前を **"LanguageButton"** に変更

**3-2. ボタンの配置**

- **Anchor**: 右上 (Top-Right)
- **Position**: 右上から少しオフセット（-20, -20）
- **Size**: `Width: 150, Height: 50`

**3-3. LanguageSwitcher スクリプトのアタッチ**

1. **LanguageButton** を選択
2. **Add Component** → LanguageSwitcher
3. Inspector で以下を設定:
   - **Switch Button**: LanguageButton（自分自身）をドラッグ
   - **Button Label**: LanguageButton の Text (TMP) をドラッグ
   - **English Text**: "English" (デフォルト)
   - **Japanese Text**: "日本語" (デフォルト)

---

## 🎮 使い方（ゲーム内）

### プレイヤー視点

#### 初回起動
1. ゲームを起動
2. システム言語が自動検出される
   - 日本語OS → 日本語音声
   - その他 → 英語音声

#### 言語切り替え
1. StartSceneで右上の言語ボタンをクリック
2. 言語が切り替わる（English ⇔ 日本語）
3. 次回起動時も設定が維持される

---

## 💻 プログラマー向け情報

### AudioManager の音声再生API

```csharp
// ゲーム開始ボイス
AudioManager.Instance.PlayGameStartVoice(aiPosition);

// カードドローボイス
AudioManager.Instance.PlayCardDrawVoice(aiPosition);

// ペアマッチボイス
AudioManager.Instance.PlayPairMatchVoice(aiPosition);

// 勝利ボイス
AudioManager.Instance.PlayVictoryVoice(aiPosition);

// 敗北ボイス
AudioManager.Instance.PlayDefeatVoice(aiPosition);

// 心理圧ボイス
AudioManager.Instance.PlayPressureVoice(aiPosition);
```

### GameSettings の言語取得API

```csharp
// 現在の言語を取得
GameSettings.GameLanguage lang = GameSettings.Instance.GetLanguage();

// 日本語かどうか判定
bool isJapanese = GameSettings.Instance.IsJapanese();

// 言語を設定
GameSettings.Instance.SetLanguage(GameSettings.GameLanguage.Japanese);

// 言語を切り替え
GameSettings.Instance.ToggleLanguage();

// 言語変更イベントを購読
GameSettings.Instance.OnLanguageChanged += (language) => {
    Debug.Log($"Language changed to: {language}");
};
```

---

## 🧪 テスト方法

### Unity Editor でのテスト

#### 1. GameSettings のテスト

1. **GameSettings** オブジェクトを選択
2. Inspector の Context Menu（⋮）から以下を実行:
   - "Test - Switch to English"
   - "Test - Switch to Japanese"
   - "Test - Toggle Language"

#### 2. AudioManager のテスト

1. **AudioManager** を選択
2. Context Menu から音声再生をテスト:
   - "Test Card Hover"
   - "Test Card Pick"
   - "Test Heartbeat - Normal"

3. **GameSettings で言語を切り替えてから**、音声再生テストを実行
4. 言語に応じて異なる音声が再生されることを確認

#### 3. 言語切り替えUIのテスト

1. Play Mode に入る
2. StartScene で言語ボタンをクリック
3. ボタンのテキストが切り替わることを確認
4. Play Mode を停止→再起動
5. 言語設定が保持されていることを確認

---

## 🔧 トラブルシューティング

### 問題: 音声が再生されない

**原因**: 音声クリップが割り当てられていない

**解決策**:
1. AudioManager を選択
2. Inspector で該当する Voice Clips が空でないか確認
3. Assets/Audio/Voice/ から該当ファイルをドラッグ

### 問題: 言語が切り替わらない

**原因**: GameSettings が初期化されていない

**解決策**:
1. Hierarchy に GameSettings オブジェクトがあるか確認
2. GameSettings.cs がアタッチされているか確認
3. Console で `[GameSettings]` のログを確認

### 問題: ボタンが機能しない

**原因**: LanguageSwitcher の参照が設定されていない

**解決策**:
1. LanguageButton を選択
2. Inspector → LanguageSwitcher:
   - Switch Button にボタン自身が設定されているか確認
   - Button Label に Text (TMP) が設定されているか確認

### 問題: 設定がリセットされる

**原因**: PlayerPrefs が正しく保存されていない

**解決策**:
```csharp
// 手動で設定をリセット
GameSettings.Instance.ResetAllSettings();
```

---

## 📝 TODO（オプション）

将来的に追加可能な機能:

1. **言語選択ダイアログ**
   - 初回起動時に言語選択画面を表示
   - スキップ可能にして、システム言語を使用

2. **追加言語対応**
   - 中国語、韓国語など
   - 言語ファイルの外部化（JSON/CSV）

3. **字幕システム**
   - AI音声に字幕を追加
   - 言語に応じて字幕を切り替え

4. **設定画面の統合**
   - 音量調整と言語設定を1つの画面に
   - UIをより洗練されたデザインに

---

## ✅ チェックリスト

セットアップが完了したら、以下を確認してください:

- [ ] GameSettings オブジェクトが Hierarchy に存在
- [ ] AudioManager に英語音声クリップが全て割り当て済み（14個）
- [ ] AudioManager に日本語音声クリップが全て割り当て済み（14個）
- [ ] StartScene に言語切り替えボタンが配置済み
- [ ] LanguageSwitcher スクリプトが正しく設定済み
- [ ] Play Mode で言語切り替えが動作する
- [ ] 再起動後も言語設定が保持される
- [ ] AudioManager のテストメニューで音声が再生される

---

完了です！これで日英バイリンガル対応の音声システムが動作します 🎉
