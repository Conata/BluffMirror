# ConfirmUI Auto Setup - 使用ガイド

## 概要

このエディタ拡張は、Phase 4 Stage 4のConfirmUIを自動的にセットアップします。

ConfirmUIは、プレイヤーがAIカードを選択したときに表示される確認ダイアログで、「引く」「やめる」の選択肢を提供します。

## 使用方法

### 1. エディタウィンドウを開く

Unityエディタのメニューから：
```
Tools > Baba > Setup ConfirmUI System
```

### 2. 設定項目

**Settings:**
- **Offset From Card**: カードからのオフセット（推奨: (0, 0.15, 0)）
  - Y軸でカードの上に表示される高さを調整
- **Scale Multiplier**: WorldSpace Canvasのスケール倍率（推奨: 0.01）
  - 3D空間でのUIサイズを調整

### 3. セットアップ実行

「Setup ConfirmUI System」ボタンをクリックします。

確認ダイアログが表示されるので、「Yes」を選択します。

### 4. 作成されるもの

セットアップが完了すると、以下が自動作成されます：

1. **ConfirmUI GameObject:**
   - WorldSpace Canvas
   - CanvasScaler (Dynamic Pixels Per Unit: 100)
   - GraphicRaycaster（ボタンクリック検知用）

2. **UIPanel:**
   - 半透明の暗い背景（色: 0.1, 0.1, 0.1, 0.9）
   - サイズ: 300x150
   - CanvasGroup（フェードイン/アウト用）

3. **PromptText:**
   - 「このカードを引きますか？」というテキスト
   - フォントサイズ: 18
   - 中央揃え

4. **DrawButton（引くボタン）:**
   - 緑色（0.2, 0.8, 0.2）
   - サイズ: 120x50
   - テキスト: 「引く」

5. **CancelButton（やめるボタン）:**
   - 赤色（0.8, 0.2, 0.2）
   - サイズ: 120x50
   - テキスト: 「やめる」

### 5. 手動調整が必要な項目

自動セットアップ後、以下の項目は手動で調整してください：

#### ConfirmUIの詳細設定

**Offset From Card:**
- カードからのオフセットを調整
- Y軸: カードの上にどれくらい浮かせるか
- X/Z軸: 左右/前後の位置調整

**Scale Multiplier:**
- WorldSpace CanvasのスケールをUIの見やすさに合わせて調整
- デフォルト: 0.01（推奨）
- 大きすぎる場合: 0.005に変更
- 小さすぎる場合: 0.015に変更

**Animation:**
- Show Duration: 表示アニメーションの長さ（推奨: 0.25秒）
- Hide Duration: 非表示アニメーションの長さ（推奨: 0.15秒）

#### UIの見た目調整

**UIPanel Background:**
- Image色を変更して背景の見た目を調整
- 透明度（Alpha）を変更して背景の濃さを調整

**Button Colors:**
- DrawButtonの色を変更（デフォルト: 緑）
- CancelButtonの色を変更（デフォルト: 赤）

**Text:**
- PromptTextの文言を変更
- DrawButton/CancelButtonのテキストを変更
- フォントサイズを調整

## 検証方法

### プレイモードテスト

1. **プレイモード開始**
2. **プレイヤーターン時にAIカードをクリック:**
   - カードが跳ね返り（拒否演出）
   - 0.5秒後にConfirmUIが表示される
   - カメラが選択されたカードにズームイン

3. **「引く」ボタンをクリック:**
   - ConfirmUIがフェードアウト
   - カードを引くアニメーション開始
   - PLAYER_TURN_DRAW状態に遷移

4. **「やめる」ボタンをクリック:**
   - ConfirmUIがフェードアウト
   - PLAYER_TURN_PICK状態に戻る
   - 別のAIカードを選択可能

5. **UI表示確認:**
   - ConfirmUIがカードの上に正しく配置されているか
   - カメラの方を向いているか（LateUpdateで回転更新）
   - フェードイン/アウトアニメーションが滑らかか

## トラブルシューティング

### ConfirmUIが表示されない

- ConfirmUI GameObjectがシーンに存在するか確認
- GameManagerのInspectorでConfirmUIへの参照が設定されているか確認（自動的にInstance経由でアクセス）
- Canvas Render Modeが「World Space」になっているか確認

### ボタンがクリックできない

- GraphicRaycasterがConfirmUI GameObjectにアタッチされているか確認
- CanvasのWorldCameraがMain Cameraに設定されているか確認
- UIPanel、DrawButton、CancelButtonが正しくConfirmUIコンポーネントに参照されているか確認

### ConfirmUIがカードから離れている

- Offset From Cardの値を調整
- Y軸を増やす: カードの上に移動
- Y軸を減らす: カードの下に移動

### ConfirmUIが大きすぎる/小さすぎる

- Scale Multiplierの値を調整
- 大きすぎる場合: 0.01 → 0.005に変更
- 小さすぎる場合: 0.01 → 0.015に変更

### カメラの方を向かない

- ConfirmUI.cs の LateUpdate() が正しく動作しているか確認
- Camera.main が正しく設定されているか確認

## GameManagerとの統合

ConfirmUIは以下のGameManagerメソッドで使用されます：

1. **OnCardPointerDown(CardObject card)**
   - CardObjectから呼び出し
   - HandleCardInterrupt()コルーチンを開始

2. **HandleCardInterrupt(CardObject card)**
   - PLAYER_TURN_INTERRUPT状態で拒否アニメーション再生
   - PLAYER_TURN_CONFIRM状態でConfirmUI表示
   - ConfirmUI.Instance.Show(card, OnConfirmDraw, OnConfirmAbort);

3. **OnConfirmDraw(CardObject card)**
   - 「引く」ボタンのコールバック
   - ExecuteCardDraw()コルーチンを開始

4. **OnConfirmAbort()**
   - 「やめる」ボタンのコールバック
   - PLAYER_TURN_PICK状態に戻る

## CardObjectとの統合

ConfirmUIは以下のCardObjectメソッドと連携します：

1. **OnPointerDown(PointerEventData eventData)**
   - CardInteractionState.PointerDown状態に変更
   - GameManager.OnCardPointerDown(this)を呼び出し

2. **PlayInterruptAnimation()**
   - CardInteractionState.Interrupting状態に変更
   - 跳ね返りと揺れアニメーション再生
   - アニメーション完了後、CardInteractionState.AwaitingConfirm状態に変更

3. **ResetInteractionState()**
   - 「やめる」選択時に呼び出し
   - CardInteractionState.Idle状態に戻す

4. **SetCommitted()**
   - 「引く」選択時に呼び出し
   - CardInteractionState.Committed状態に変更

## 次のステップ

Stage 4完了後、次はStage 5（ブラフシステム）の実装に進みます。

---

**作成日**: 2026-02-07
**Phase 4 - Stage 4**: ConfirmUI Auto Setup
