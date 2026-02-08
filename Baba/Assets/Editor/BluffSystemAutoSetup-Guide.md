# BluffSystem Auto Setup - 使用ガイド

## 概要

このエディタ拡張は、Stage 5のブラフシステムを自動的にセットアップします。

BluffSystemは、AIがプレイヤーの行動パターンに基づいてブラフ（偽の自信/偽の躊躇/挑発/注意逸らし）を行い、カードドロー後に心理的リアクションを生成するシステムです。

## 使用方法

### 1. エディタウィンドウを開く

Unityエディタのメニューから：
```
Tools > Baba > Setup BluffSystem
```

### 2. セットアップチェック

ウィンドウが開くと、自動的にセットアップチェックが実行されます。

**依存コンポーネント:**
- PsychologySystem（心理圧システム）
- PlayerBehaviorAnalyzer（行動分析）
- FloatingTextSystem（3Dテキスト表示）

**すべてのコンポーネントが見つかった場合:**
```
All dependencies found! Ready to setup.
```

**コンポーネントが不足している場合:**
```
一部の依存コンポーネントが見つかりません。
セットアップ実行時に自動作成されます。
```

不足しているコンポーネントはセットアップ実行時に自動的に作成されます。
PsychologySystemとPlayerBehaviorAnalyzerは同じGameObjectに配置されます。

### 3. ブラフ設定

**Base Bluff Chance** (デフォルト: 0.3)
- AIがブラフを仕掛ける基本確率
- 0.0 = ブラフなし、1.0 = 常にブラフ

**Max Bluff Chance** (デフォルト: 0.7)
- ブラフ確率の上限
- ゲーム進行に伴いこの値まで上昇

**Turns Before Bluffing** (デフォルト: 2)
- ブラフ開始までのターン数
- 序盤はブラフなし（行動データ収集期間）

### 4. セットアップ実行

「Setup BluffSystem」ボタンをクリックします。

確認ダイアログが表示されるので、「Yes」を選択します。

### 5. 作成されるもの

セットアップが完了すると、以下が自動作成されます：

1. **BluffSystem GameObject:**
   - BluffSystemコンポーネント
   - 依存コンポーネントへの自動参照設定
   - ブラフ設定値の適用

2. **PsychologySystem GameObject（未作成の場合）:**
   - PsychologySystemコンポーネント
   - PlayerBehaviorAnalyzerコンポーネント（同じGameObject）
   - behaviorAnalyzer参照の自動設定

3. **FloatingTextSystem GameObject（未作成の場合）:**
   - FloatingTextSystemコンポーネント

## ブラフシステムの仕組み

### ブラフ戦略（BluffIntent）

| 戦略 | 説明 | 有効な状況 |
|------|------|------------|
| Honest | 正直な反応 | 常に（序盤はこれのみ） |
| FakeConfidence | 自信があるフリ | プレイヤーが迷っている時 |
| FakeHesitation | 迷っているフリ | プレイヤーが即断する時 |
| Provoke | 挑発 | 中程度の圧力時 |
| Deflect | 注意逸らし | プレイヤーのパターン検出時 |

### ゲームフロー統合

**プレイヤーターン:**
1. PLAYER_TURN_COMMIT → `BluffSystem.DetermineAIIntent()` でAIのブラフ意図を決定
2. PLAYER_TURN_POST_REACT → `BluffSystem.EvaluateBluff()` でブラフ結果を評価
3. `PlayBluffReaction()` でセリフ表示・感情変化・圧力調整

**AIターン:**
1. AI_TURN_COMMIT → `BluffSystem.DetermineAIIntent()` でAIのブラフ意図を決定
2. AI_TURN_REACT → `BluffSystem.EvaluateBluff()` でブラフ結果を評価
3. `PlayBluffReaction()` でセリフ表示・感情変化・圧力調整

## 検証方法

### プレイモードテスト

1. **プレイモード開始**

2. **序盤（ターン1-2）を確認:**
   - コンソールに `intent=Honest` が出力される
   - ブラフは発生しない

3. **中盤（ターン3+）を確認:**
   - 非Honestのintentが時折出現する
   - FloatingTextでセリフが表示される

4. **ジョーカーを引いた場合:**
   - 高intensityリアクションが発生
   - カメラがAIリアクションビューに切り替わる
   - 圧力レベルが大きく変化する

5. **Debug GUI確認（左下）:**
   - Intent: 現在のブラフ意図
   - Turn: ターン数
   - Bluff Success: ブラフ成功率

## トラブルシューティング

### セリフが表示されない

- FloatingTextSystemがシーンに存在するか確認
- BluffSystemのInspectorでFloatingTextSystemへの参照が設定されているか確認
- コンソールで `[BluffSystem] FloatingTextSystem not found` の警告を確認

### ブラフが発生しない

- turnsBeforeBluffingの値を確認（デフォルト: 2）
- baseBluffChanceが0でないか確認
- コンソールで `[BluffSystem] Turn X: Too early for bluff` を確認

### 圧力レベルが変化しない

- PsychologySystemがシーンに存在するか確認
- BluffSystemのInspectorでPsychologySystemへの参照が設定されているか確認

### カメラリアクションが発生しない

- CameraCinematicsSystemがシーンに存在するか確認
- GameManagerのInspectorでCameraCinematicsSystemへの参照が設定されているか確認
- 高intensity（>0.5）のリアクション時のみカメラが切り替わる

## GameManagerとの統合

BluffSystemは以下のGameManagerメソッドで使用されます：

1. **ExecuteCardDraw(CardObject card)**
   - PLAYER_TURN_COMMIT: `BluffSystem.Instance.DetermineAIIntent()`
   - PLAYER_TURN_POST_REACT: `BluffSystem.Instance.EvaluateBluff(drawCtx)`

2. **AITurnSequence()**
   - AI_TURN_COMMIT: `BluffSystem.Instance.DetermineAIIntent()`
   - AI_TURN_REACT: `BluffSystem.Instance.EvaluateBluff(drawCtx)`

3. **PlayBluffReaction(BluffResult result)**
   - FloatingTextSystem.ShowText() でセリフ表示
   - EmotionalStateManager.ForceEmotionalState() で感情変化
   - PsychologySystem.SetPressureLevel() で圧力調整
   - CameraCinematicsSystem.ShowAIReactionView() でカメラ切替

## 次のステップ

Stage 5完了後、次はStage 6（AIバイタル - 指の迷いアニメーション）の実装に進みます。

---

**作成日**: 2026-02-07
**Stage 5**: BluffSystem Auto Setup
