# BirthdayLanguageUISetup エディタ拡張ガイド

## 概要
BirthdaySetupUIに言語選択UI（ドロップダウン + ラベル）を自動追加するエディタツールです。

## 使い方

### 1. エディタツールを開く
Unity Editor のメニューバーから：
```
Tools > Baba > Setup Birthday Language UI
```

### 2. セットアップを実行
1. エディタウィンドウが開いたら、`Setup Language UI` ボタンをクリック
2. 確認ダイアログで `Yes` をクリック
3. セットアップが完了すると、成功メッセージが表示されます

### 3. セットアップ内容
以下の要素が自動的に作成・設定されます：

#### 作成されるオブジェクト構造
```
BirthdayPanel (または BirthdaySetupUI root)
└── LanguageSelection (新規作成)
    ├── LanguageLabel (TextMeshProUGUI)
    │   └── "Language / 言語" ラベル
    └── LanguageDropdown (TMP_Dropdown)
        ├── Label (選択中の言語表示)
        ├── Arrow (ドロップダウンアイコン)
        └── Template (ドロップダウンリスト)
            └── 2つのオプション: "English" / "日本語"
```

#### BirthdaySetupUI への設定
- `languageLabel` フィールド → LanguageLabel が自動アサイン
- `languageDropdown` フィールド → LanguageDropdown が自動アサイン

## セットアップ後の確認

### Inspector 確認事項
1. BirthdaySetupUI コンポーネントを選択
2. **Language Selection** セクションを確認：
   - `Language Label`: LanguageLabel が設定されているか
   - `Language Dropdown`: LanguageDropdown が設定されているか

### 実行時の動作確認
1. Play モードに入る
2. 言語ドロップダウンで "English" ↔ "日本語" を切り替え
3. UI全体が選択した言語に更新されることを確認

## レイアウト調整

セットアップ後、必要に応じて位置を調整できます：

### LanguageSelection の位置調整
```
Inspector > LanguageSelection > Rect Transform
- Anchored Position: (0, -80) ← 名前入力の下に配置される
- Size Delta: (400, 40)
```

### ラベルの位置調整
```
Inspector > LanguageLabel > Rect Transform
- Anchored Position: (100, 0)
- Size Delta: (150, 30)
```

### ドロップダウンの位置調整
```
Inspector > LanguageDropdown > Rect Transform
- Anchored Position: (300, 0)
- Size Delta: (180, 35)
```

## トラブルシューティング

### エラー: "BirthdaySetupUIが見つかりません"
- シーンに BirthdaySetupUI コンポーネントが存在するか確認
- 正しいシーンが開いているか確認（通常は StartMenu シーン）

### 言語切替が動作しない
1. GameSettings インスタンスがシーンに存在するか確認
2. LocalizationManager が正しく初期化されているか確認
3. Console でエラーログを確認

### ドロップダウンが表示されない
1. Canvas の Render Mode が正しいか確認
2. LanguageDropdown の RectTransform が正しく設定されているか確認
3. Template オブジェクトが非アクティブになっているか確認

## 手動セットアップとの違い

このツールを使用すると以下のメリットがあります：
- ✅ ドロップダウンの複雑な階層構造を自動生成
- ✅ 適切な RectTransform 設定を自動適用
- ✅ BirthdaySetupUI への参照を自動設定
- ✅ "English" / "日本語" オプションをプリセット
- ✅ Undo 対応（Ctrl+Z で元に戻せる）

## 関連ファイル

- エディタスクリプト: `Assets/Editor/BirthdayLanguageUISetup.cs`
- ランタイムスクリプト: `Assets/Scripts/UI/BirthdaySetupUI.cs`
- ローカライゼーション: `Assets/Resources/Localization/ja.json`, `en.json`
- 設定マネージャー: `Assets/Scripts/Core/GameSettings.cs`

## 更新履歴

- **2026-02-08**: 初版作成
  - 言語選択UI自動セットアップ機能を実装
  - English / 日本語 2言語対応
