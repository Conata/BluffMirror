# FloatingText Auto Setup - 使用ガイド

## 概要

このエディタ拡張は、Phase 4 Stage 3のFloatingTextSystemを自動的にセットアップします。

## 使用方法

### 1. エディタウィンドウを開く

Unityエディタのメニューから：
```
Tools > Baba > Setup FloatingText System
```

### 2. 設定項目

**Settings:**
- **Pool Size**: テキストオブジェクトのプールサイズ（推奨: 10）
- **Font Size**: TextMeshProのフォントサイズ（推奨: 3.0）
- **Font Asset (Optional)**: TextMeshPro フォントアセット（オプション）
  - 指定しない場合、デフォルトフォントが自動検索されます

**References:**
- **Psychology System**: PsychologySystem GameObject（自動検索されます）
  - 手動で設定することも可能

### 3. セットアップ実行

「Setup FloatingText System」ボタンをクリックします。

確認ダイアログが表示されるので、「Yes」を選択します。

### 4. 作成されるもの

セットアップが完了すると、以下が自動作成されます：

1. **FloatingTextSystem GameObject:**
   - FloatingTextSystemコンポーネント
   - Pool Size: 設定した値
   - Font Size: 設定した値
   - アニメーション設定:
     - Float Height: 0.5
     - Float Duration: 1.5s
     - Fade In Duration: 0.3s
     - Fade Out Duration: 0.5s
   - 色設定:
     - Low Pressure: White
     - Medium Pressure: Orange (1, 0.8, 0.3)
     - High Pressure: Red (1, 0.2, 0.2)

2. **PsychologySystemとの接続:**
   - PsychologySystemのfloatingTextSystemフィールドに自動接続

### 5. 手動調整が必要な項目

自動セットアップ後、以下の項目は手動で調整してください：

#### FloatingTextSystemの詳細設定

**Text Prefab（オプション）:**
- カスタムTextMeshPro 3Dプレハブを使用する場合は設定
- 設定しない場合、プロシージャル生成されます

**アニメーション設定の微調整:**
- Float Height: テキストが浮遊する高さ
- Float Duration: 浮遊アニメーションの長さ
- Fade In/Out Duration: フェードイン/アウトの時間

**色設定の微調整:**
- Low/Medium/High Pressure Color: 心理圧レベルに応じた色

## 検証方法

### Context Menu テスト

FloatingTextSystemのInspectorで右クリック > Context Menu:
- **Test: Low Pressure Text** - 低圧力テキスト表示
- **Test: Medium Pressure Text** - 中圧力テキスト表示
- **Test: High Pressure Text** - 高圧力テキスト表示

### 実行時テスト

1. **プレイモード開始**
2. **PsychologySystemでテスト:**
   - カードホバー時にテキストが表示されることを確認
   - 心理圧レベルに応じて色が変化することを確認

3. **色変化確認:**
   - PsychologySystemのInspectorで「Max Pressure」ボタンをクリック
   - FloatingTextの色が赤に変化することを確認

## トラブルシューティング

### FloatingTextが表示されない

- FloatingTextSystem GameObjectがシーンに存在するか確認
- PsychologySystemのfloatingTextSystemフィールドが正しく設定されているか確認
- Text Prefabが設定されている場合、プレハブが正しいか確認

### テキストの色が変わらない

- PsychologySystemのPressure Levelを変更してテスト
- FloatingTextSystemのLow/Medium/High Pressure Colorが正しく設定されているか確認

### テキストが浮遊しない

- Float Height が0より大きいか確認（推奨: 0.5）
- Float Duration が0より大きいか確認（推奨: 1.5）
- DOTweenパッケージがインストールされているか確認

### PsychologySystemとの接続が失敗

- PsychologySystemがシーンに存在するか確認
- PsychologySystemにfloatingTextSystemフィールドが存在するか確認
- シーンを保存してから再度セットアップを実行

## 既存のFloatingTextSystemがある場合

既存のFloatingTextSystemがシーンに存在する場合、自動セットアップは：
- 既存のオブジェクトを検索して再利用
- 設定を上書き更新

新規作成する必要がある場合は、既存のオブジェクトを削除してからセットアップを実行してください。

## Undo機能

セットアップは全てUndo対応しています。
間違えた場合は `Ctrl+Z` (Windows) / `Cmd+Z` (Mac) で元に戻せます。

## PsychologySystemとの統合

FloatingTextSystemは以下のPsychologySystemメソッドで使用されます：

1. **ShowFloatingTextAtCard(Vector3 cardPosition, string text)**
   - CardObjectから直接呼び出し
   - カードホバー時にテキスト表示

2. **GenerateHoverDialogue(int cardIndex, BehaviorPattern behavior)**
   - LLM生成テキストをFloatingTextで表示
   - 心理圧レベルに応じた色変化

## TextMeshPro設定

FloatingTextSystemはTextMeshPro 3Dを使用します。

**必要なパッケージ:**
- TextMeshPro（Unity標準パッケージ）

**初回セットアップ:**
1. Window > TextMeshPro > Import TMP Essential Resources
2. フォントアセットを作成（オプション）

## 次のステップ

Stage 3完了後、次はStage 4（カード引き拒否→確認フロー）の実装に進みます。

---

**作成日**: 2026-02-07
**Phase 4 - Stage 3**: FloatingText Auto Setup
