# Birthday Setup Validator - 使用ガイド

## 概要
BirthdaySetupUIコンポーネントの参照を検証・自動修復するエディターツールです。

## 使用方法

### 1. ツールを開く
1. Unity Editorで `StartMenuScen.unity` を開く
2. メニューから `Tools > Baba > Validate Birthday Setup` を選択
3. ツールウィンドウが表示される

### 2. BirthdaySetupUIを検索
1. ウィンドウ上部の `Find BirthdaySetupUI in Scene` ボタンをクリック
2. シーン内のBirthdaySetupUIが自動的に検索され、選択される

### 3. 検証結果を確認
ツールウィンドウに以下の情報が表示されます：

- ✅ **緑色** = 正しくアサインされている
- ❌ **赤色** = 参照が欠落している

検証対象：
- **Dropdowns**: Year, Month, Day
- **Buttons**: Next, Skip, Start Game
- **Text Fields**: Status, Title, Subtitle, Labels
- **Panels**: Birthday Panel, Ready Panel

### 4. 自動修復（オプション）
欠落している参照がある場合：

1. ウィンドウ下部の `Auto-Fix Missing References` ボタンをクリック
2. ツールが自動的に子オブジェクトを検索して参照を設定
3. 修復完了ダイアログが表示される
4. Inspectorで結果を確認

## トラブルシューティング

### "BirthdaySetupUI not found"エラー
- `StartMenuScen.unity` が開いているか確認
- シーン内に BirthdaySetupUI コンポーネントが存在するか確認

### Auto-Fixで修復できない場合
以下を手動で確認：
1. ヒエラルキーで各UIオブジェクトの名前が正しいか
2. 必要なコンポーネント（Button, TMP_Dropdown等）がアタッチされているか
3. オブジェクトが無効化されていないか

### 推奨される命名規則
Auto-Fixが正しく動作するための命名：
- `YearDropdown`, `MonthDropdown`, `DayDropdown`
- `NextButton`, `SkipButton`, `StartButton`
- `TitleText`, `SubtitleText`, `StatusText`
- `BirthdayPanel`, `ReadyPanel`

## 注意事項
- Auto-Fixは既存の参照を上書きしません（nullの場合のみ設定）
- 修正後は必ずInspectorで結果を確認してください
- Undoで修正を取り消すことができます（Ctrl/Cmd + Z）
