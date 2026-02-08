# Birthday UI Setup - 使用ガイド

## 概要
BirthdayPanel内に不足しているUIエレメントを自動生成するエディターツールです。

## 使用方法

### 1. ツールを開く
1. Unity Editorで `StartMenuScen.unity` を開く
2. メニューから `Tools > Baba > Setup Birthday UI Elements` を選択

### 2. Name UI Elementsを生成
1. ウィンドウ内の `Generate Missing Name UI Elements` ボタンをクリック
2. 以下のUIエレメントが自動生成されます：
   - **NameLabel** (TextMeshProUGUI) - 「名前 / Name」ラベル
   - **NameInput** (TMP_InputField) - 名前入力フィールド
3. BirthdaySetupUIコンポーネントに自動的に参照が設定されます

### 3. 生成されるUI構造

```
BirthdayPanel
├── NameLabel (TextMeshProUGUI)
│   └── Text: "名前 / Name"
│   └── Position: 上部中央 (0, -50)
│   └── Size: 200x30
└── NameInput (TMP_InputField)
    ├── Text Area
    │   ├── Placeholder ("Enter your name...")
    │   └── Text (実際の入力テキスト)
    └── Position: NameLabelの下 (0, -90)
    └── Size: 400x40
```

### 4. 生成後の確認

**Hierarchy:**
- BirthdayPanel内に `NameLabel` と `NameInput` が作成されているか確認

**Inspector (BirthdaySetupUI):**
- `Name Input Field` フィールドに `NameInput` が設定されているか確認
- `Name Label` フィールドに `NameLabel` が設定されているか確認

## カスタマイズ

生成後、以下をInspectorでカスタマイズできます：

### NameLabel
- **Text**: ラベルテキストの変更
- **Font Size**: フォントサイズ（デフォルト: 24）
- **Color**: テキスト色（デフォルト: 白）
- **Position**: RectTransformで位置調整

### NameInput
- **Placeholder**: プレースホルダーテキスト
- **Character Limit**: 文字数制限（デフォルト: 20）
- **Font Size**: フォントサイズ（デフォルト: 20）
- **Background Color**: 背景色
- **Position & Size**: RectTransformで調整

## トラブルシューティング

### "BirthdaySetupUI not found"エラー
- `StartMenuScen.unity` が開いているか確認
- BirthdayPanel内にBirthdaySetupUIコンポーネントがあるか確認

### UIが正しい位置に表示されない
- BirthdayPanelのRectTransformサイズを確認
- 生成後、手動でPosition/Sizeを調整可能

### 既存のUIと重なる
- 他のUIエレメントの位置を確認
- RectTransformのY座標を調整（-50, -90から変更可能）

## 注意事項
- 既に同名のオブジェクトが存在する場合、新規作成はスキップされます
- Undoで元に戻すことができます（Ctrl/Cmd + Z）
- 生成後、Validatorツールで全ての参照を確認することをお勧めします

## 推奨ワークフロー
1. `Setup Birthday UI Elements` でUIを生成
2. `Validate Birthday Setup` で全参照を確認
3. Play Modeで動作確認
