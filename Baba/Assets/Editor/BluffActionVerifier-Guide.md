# BluffActionVerifier - 使い方ガイド

## 概要
BluffActionVerifierは、ブラフアクションシステムの設定を自動検証するEditorツールです。

## 実行方法
Unity Editorで以下のメニューから実行：
```
Tools > Baba > Verify BluffAction Setup
```

## 検証項目

### 1. BluffActionSystem
- ✅ シーンにBluffActionSystemが存在するか
- ✅ playerHandが正しく設定されているか
- ✅ aiHandが正しく設定されているか

### 2. BluffActionUI
- ✅ シーンにBluffActionUIが存在するか
- ✅ Canvas設定（RenderMode: ScreenSpaceOverlay推奨）
- ✅ 各ボタンコンポーネント（Shuffle/Push-Pull/Wiggle/Spread-Close/Cancel）
- ✅ Selection Overlay設定

### 3. HandControllers
- ✅ PlayerHandControllerがシーンに存在するか
- ✅ AIHandControllerがシーンに存在するか

### 4. GameManager連携
- ✅ GameManagerがシーンに存在するか
- ✅ StartAIBluffMonitor()の呼び出し確認
- ✅ BluffActionUI.Show()の呼び出し確認

## レポート表示

### ✅ 正常
すべてのチェックが通過した場合、緑色のメッセージが表示されます。

### ⚠️ 警告
設定が推奨と異なるが、動作には影響しない可能性がある項目です。
- playerHand/aiHandの未設定
- Canvas RenderModeの違い
- 一部コンポーネントの未設定

### ❌ エラー
ブラフアクションが正常に動作しない重大な問題です。
- BluffActionSystem/BluffActionUIの欠落
- 必須コンポーネントの欠落
- GameManager連携コードの欠落

## エラーが見つかった場合

### 自動修復
エラーがある場合、ウィンドウ下部に「Run Setup Tool to Fix」ボタンが表示されます。
クリックすると、BluffActionUIAutoSetupツールが起動し、自動セットアップが可能です。

### 手動修復
1. **BluffActionSystem/UI欠落**
   - `Tools > Baba > Setup BluffAction UI` を実行

2. **playerHand/aiHand未設定**
   - BluffActionSystemのInspectorで手動設定

3. **GameManager連携コード欠落**
   - GameManager.csのコードを確認（通常は自動で正しく設定済み）

## トラブルシューティング

### 「ブラフアクションが動いてないように見える」
→ このツールを実行して、エラー項目をすべて修正してください。

### プレイモードでボタンが表示されない
→ BluffActionUI.Show()が呼ばれているか確認（このツールで検証可能）

### AIがブラフしない
→ BluffActionSystem.StartAIBluffMonitor()が呼ばれているか確認

## 推奨ワークフロー
1. シーン作成後、まず`Setup BluffAction UI`を実行
2. 定期的に`Verify BluffAction Setup`で検証
3. エラーが出たら自動修復または手動修正
4. すべて✅になったらプレイテスト

## 関連ツール
- **Tools > Baba > Setup BluffAction UI**: 初期セットアップ
- **Tools > Baba > Verify BluffAction Setup**: 検証ツール（このツール）
