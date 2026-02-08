# FloatingText Font Validator ガイド

## 概要
FloatingTextSystemで日本語テキストが文字化けする問題を診断・修正するツール。

## 問題の原因
FloatingTextSystemのfontAssetフィールドに日本語対応フォント（NotoSansJP）が設定されていない場合、日本語テキストが □□□ や ??? のように文字化けします。

## 使用方法

### 1. バリデーターを起動
Unity Editor で: **Tools > Baba > Validate FloatingText Font**

### 2. 現在の状態を確認
ウィンドウに以下の情報が表示されます：
- FloatingTextSystemの存在確認
- 現在設定されているフォント
- 日本語対応状態（✓ または ⚠）
- 推奨フォント（NotoSansJP SDF）

### 3. 修正方法

#### 自動修正（推奨）
1. **「Fix: Apply Recommended Font」ボタンをクリック**
2. NotoSansJPフォントが自動的に適用されます
3. **File > Save Scene** でシーンを保存

#### 手動修正（代替方法）
1. Hierarchy で **FloatingTextSystem** を選択
2. Inspector の **Font Asset** フィールドに以下をドラッグ&ドロップ:
   - `Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset`
3. シーンを保存

## エラー対処

### "FloatingTextSystemがシーンに見つかりません"
→ **Tools > Baba > Setup FloatingText System** でセットアップを実行してください

### "NotoSansJPフォントが見つかりません"
1. `Assets/Fonts/` フォルダを確認
2. NotoSansJPフォントが存在しない場合：
   - フォントファイル（.ttf）を `Assets/Fonts/` に配置
   - Font Asset Creator で SDF Asset を生成

## 検証方法

### PlayMode テスト
1. FloatingTextSystemを選択
2. 右クリック > **Test: Show Persistent Text**
3. 「どれにしようか...」が正しく表示されることを確認

### ゲーム内テスト
1. ゲームを起動
2. プレイヤーターン中にカードにホバー
3. 日本語の心理圧テキスト（例: "これか...いや..."）が正しく表示されることを確認

## 技術詳細

### 修正箇所
- **FloatingTextSystem.cs:94-96** - fontAsset適用ロジック
- **FloatingTextAutoSetup.cs:30-49** - フォント検索ロジック（NotoSansJP優先）

### 関連ファイル
- `/Assets/Scripts/UI/FloatingTextSystem.cs` - メインシステム
- `/Assets/Editor/FloatingTextAutoSetup.cs` - セットアップツール
- `/Assets/Editor/FloatingTextValidator.cs` - 検証ツール（このツール）
- `/Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset` - 日本語フォントアセット

## トラブルシューティング

### 問題: 修正後も文字化けする
- PlayModeを一度停止してから再度起動
- プールされているTextオブジェクトが古い設定を保持している可能性があります

### 問題: フォント適用後もデフォルトフォントが使われる
- textPrefabフィールドがアサインされている場合、そのプレハブのフォント設定が優先されます
- FloatingTextSystem Inspector で textPrefab フィールドを確認してください
