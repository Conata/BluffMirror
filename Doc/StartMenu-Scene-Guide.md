# スタートメニューシーン実装ガイド

## 概要
ゲーム開始前にAPIキーを入力・保存できるスタート画面を実装します。

## シーン構成

### 1. 新しいシーンを作成
```
File → New Scene → 2D
名前: "StartMenuScene"
保存先: Assets/Scenes/StartMenuScene.unity
```

### 2. Canvas設定
```
Hierarchy で右クリック → UI → Canvas

Canvas設定:
- Render Mode: Screen Space - Overlay
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1920x1080
```

### 3. EventSystem追加
```
Hierarchy で右クリック → UI → Event System
（Canvasと一緒に自動生成される場合もあります）
```

## UI構造

### Setup Panel（APIキー入力画面）
```
Canvas
├── APIKeyManager (Empty GameObject)
│   └── APIKeyManager.cs
├── SetupPanel (Panel)
│   ├── Title (TextMeshPro)
│   │   - Text: "FPS Trump Game - API Setup"
│   │   - Font Size: 48
│   │   - Alignment: Center
│   │
│   ├── ClaudeKeySection (Vertical Layout Group)
│   │   ├── ClaudeLabel (TextMeshPro)
│   │   │   - Text: "Claude API Key:"
│   │   ├── ClaudeInputField (TMP_InputField)
│   │   │   - Placeholder: "sk-ant-api03-..."
│   │   │   - Content Type: Password (optional)
│   │   └── ClaudeHelpButton (Button)
│   │       - Text: "Get Claude API Key"
│   │       - OnClick: APIKeySetupUI.OpenClaudeAPIKeyURL()
│   │
│   ├── OpenAIKeySection (Vertical Layout Group)
│   │   ├── OpenAILabel (TextMeshPro)
│   │   │   - Text: "OpenAI API Key:"
│   │   ├── OpenAIInputField (TMP_InputField)
│   │   │   - Placeholder: "sk-..."
│   │   │   - Content Type: Password (optional)
│   │   └── OpenAIHelpButton (Button)
│   │       - Text: "Get OpenAI API Key"
│   │       - OnClick: APIKeySetupUI.OpenOpenAIAPIKeyURL()
│   │
│   ├── ButtonGroup (Horizontal Layout Group)
│   │   ├── SaveButton (Button)
│   │   │   - Text: "Save"
│   │   │   - OnClick: APIKeySetupUI.OnSaveButtonClicked()
│   │   └── SkipButton (Button)
│   │       - Text: "Skip (Offline Mode)"
│   │       - OnClick: APIKeySetupUI.OnSkipButtonClicked()
│   │
│   └── StatusText (TextMeshPro)
│       - Text: ""
│       - Font Size: 24
│       - Alignment: Center
```

### Ready Panel（ゲーム開始画面）
```
Canvas
├── ReadyPanel (Panel)
│   ├── Title (TextMeshPro)
│   │   - Text: "Ready to Play!"
│   │   - Font Size: 60
│   │
│   ├── StatusText (TextMeshPro)
│   │   - Text: "All API keys loaded ✅"
│   │   - Font Size: 28
│   │
│   └── StartGameButton (Button)
│       - Text: "Start Game"
│       - Font Size: 36
│       - OnClick: APIKeySetupUI.OnStartGameButtonClicked()
```

## スクリプト設定

### APIKeySetupUI.cs設定
```
1. SetupPanel に APIKeySetupUI.cs をアタッチ

2. Inspector で以下を設定:
   - Claude API Key Input: ClaudeInputField
   - OpenAI API Key Input: OpenAIInputField
   - Save Button: SaveButton
   - Skip Button: SkipButton
   - Start Game Button: StartGameButton
   - Status Text: StatusText
   - Setup Panel: SetupPanel
   - Ready Panel: ReadyPanel
   - Game Scene Name: "GameScene"
```

## Scene Build設定

### Build Settingsに追加
```
File → Build Settings

Scenes In Build:
1. StartMenuScene (index 0)
2. GameScene (index 1)
```

## スタイリング（オプション）

### Background
```
SetupPanel:
- Image Component追加
- Color: 半透明黒 (RGBA: 0, 0, 0, 200)

ReadyPanel:
- Image Component追加
- Color: 半透明緑 (RGBA: 0, 100, 0, 200)
```

### Buttons
```
Normal Color: White
Highlighted Color: Light Blue
Pressed Color: Dark Blue
Selected Color: Blue
```

### InputFields
```
Background: 半透明白 (RGBA: 255, 255, 255, 50)
Text Color: White
Placeholder Color: Gray
```

## テスト手順

### 1. Scene実行
```
1. StartMenuScene を開く
2. Play モード開始
3. APIキーを入力
4. Save ボタンをクリック
5. Start Game ボタンが表示される
6. ゲームシーンに遷移
```

### 2. 永続化テスト
```
1. APIキーを保存
2. Play モード終了
3. Play モード再開始
4. APIキーが自動読み込みされる（マスク表示）
```

### 3. オフラインモード
```
1. Skip ボタンをクリック
2. 警告メッセージ表示
3. フォールバックシステム使用でゲーム開始
```

## セキュリティ

### APIキー保護
- PlayerPrefsに暗号化保存（XOR暗号化）
- 画面表示時はマスク（最初4文字 + *** + 最後4文字）
- Inspector fallbackは.gitignoreで除外

### .gitignore設定
```
# Unity scenes with API keys
Assets/Scenes/StartMenuScene.unity
*.unity.meta

# PlayerPrefs保存ファイル
~/Library/Preferences/
```

## トラブルシューティング

### APIキーが保存されない
→ PlayerPrefs.Save() が正しく呼ばれているか確認

### GameSceneが見つからない
→ Build Settings で GameScene が追加されているか確認

### ボタンが反応しない
→ EventSystem が存在するか確認

---

**実装完了後、StartMenuSceneからゲームを起動してください！**
