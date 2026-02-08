# Cinemachine Camera System - Unity Scene Setup Instructions

## Phase 4 - Stage 2: Cinemachineカメラシステムセットアップ

このドキュメントでは、Cinemachineカメラシステムの Unity シーン設定手順を説明します。

---

## 必要なパッケージ

**Cinemachine パッケージ**がインストールされていることを確認してください。

- Window > Package Manager > Cinemachine (com.unity.cinemachine) を検索してインストール

---

## 手順 1: Cinemachine Brain の設定

1. **メインカメラ**に `Cinemachine Brain` コンポーネントを追加
   - Hierarchy で `Main Camera` を選択
   - Inspector > Add Component > Cinemachine > Cinemachine Brain

2. **Cinemachine Brain 設定**
   - Default Blend: `EaseInOut`
   - Default Blend Time: `1.0` 秒
   - Update Method: `Smart Update` (推奨)
   - Blend Update Method: `Late Update` (推奨)

---

## 手順 2: 4つの Virtual Camera を作成

シーンに以下の4つの Cinemachine Virtual Camera を作成します。

### 2-1. VCam_PlayerTurn（プレイヤーターン：AI手札を見下ろす）

1. Hierarchy で右クリック > Cinemachine > Virtual Camera
2. 名前を `VCam_PlayerTurn` に変更
3. Transform 設定:
   - Position: `(0, 1.5, 0.2)`
   - Rotation: `(-30, 0, 0)`
4. Cinemachine Virtual Camera コンポーネント設定:
   - Priority: `10`
   - Follow: `None`（空欄）
   - Look At: `AIHand` の Transform（AI手札のルートオブジェクト）
   - Body: `Transposer`
     - Binding Mode: `World Space`
     - Follow Offset: `(0, 1.5, 0.2)`
     - Damping: `(1, 1, 1)`
   - Aim: `Composer`
     - Tracked Object Offset: `(0, 0, 0)`
     - Dead Zone Width/Height: `0.1`
     - Soft Zone Width/Height: `0.8`
     - Damping: `(0.5, 0.5, 0.5)`

### 2-2. VCam_AITurn（AIターン：プレイヤー手札を見下ろす）

1. Hierarchy で右クリック > Cinemachine > Virtual Camera
2. 名前を `VCam_AITurn` に変更
3. Transform 設定:
   - Position: `(0, 1.5, 2.0)`
   - Rotation: `(-20, 0, 0)`
4. Cinemachine Virtual Camera コンポーネント設定:
   - Priority: `10`
   - Follow: `None`（空欄）
   - Look At: `PlayerHand` の Transform（プレイヤー手札のルートオブジェクト）
   - Body: `Transposer`
     - Binding Mode: `World Space`
     - Follow Offset: `(0, 1.5, 2.0)`
     - Damping: `(1, 1, 1)`
   - Aim: `Composer`
     - Tracked Object Offset: `(0, 0, 0)`
     - Dead Zone Width/Height: `0.1`
     - Soft Zone Width/Height: `0.8`
     - Damping: `(0.5, 0.5, 0.5)`

### 2-3. VCam_CardFocus（カード選択時：選択されたカードにズームイン）

1. Hierarchy で右クリック > Cinemachine > Virtual Camera
2. 名前を `VCam_CardFocus` に変更
3. Transform 設定:
   - Position: `(0, 0, 0)` （初期位置、Follow/LookAtで動的に変更される）
4. Cinemachine Virtual Camera コンポーネント設定:
   - Priority: `10` （非アクティブ時）
   - Follow: `None`（空欄、CameraCinematicsSystem.FocusOnCard()で動的に設定）
   - Look At: `None`（空欄、CameraCinematicsSystem.FocusOnCard()で動的に設定）
   - Body: `Transposer`
     - Binding Mode: `Lock To Target With World Up`
     - Follow Offset: `(0, 0.3, 0.3)`（カードの斜め上後ろ）
     - Damping: `(0.8, 0.8, 0.8)`（滑らかな移動）
   - Aim: `Composer`
     - Tracked Object Offset: `(0, 0, 0)`
     - Dead Zone Width/Height: `0`
     - Soft Zone Width/Height: `1.0`
     - Damping: `(0.5, 0.5, 0.5)`

### 2-4. VCam_AIReaction（AI反応時：AIの顔/反応にフォーカス）

1. Hierarchy で右クリック > Cinemachine > Virtual Camera
2. 名前を `VCam_AIReaction` に変更
3. Transform 設定:
   - Position: `(0, 1.3, 0.5)`
   - Rotation: `(-10, 0, 0)`
4. Cinemachine Virtual Camera コンポーネント設定:
   - Priority: `10`
   - Follow: `None`（空欄）
   - Look At: `AIHand` の Transform（AI顔オブジェクトがあればそちらを優先）
     - **注意**: AI顔オブジェクト（AIFaceなど）がシーンに存在する場合はそちらを設定してください
     - 存在しない場合は `AIHand` を設定（フォールバック）
   - Body: `Transposer`
     - Binding Mode: `World Space`
     - Follow Offset: `(0, 1.3, 0.5)`
     - Damping: `(1, 1, 1)`
   - Aim: `Composer`
     - Tracked Object Offset: `(0, 0.1, 0)`（顔の少し上を見る）
     - Dead Zone Width/Height: `0.1`
     - Soft Zone Width/Height: `0.8`
     - Damping: `(0.5, 0.5, 0.5)`

---

## 手順 3: CameraCinematicsSystem GameObject 作成

1. Hierarchy で空のGameObject を作成
   - 右クリック > Create Empty
   - 名前を `CameraCinematicsSystem` に変更

2. `CameraCinematicsSystem` コンポーネントをアタッチ
   - Inspector > Add Component > Scripts > CameraCinematicsSystem

3. **CameraCinematicsSystem コンポーネントの Inspector 設定**:
   - Virtual Cameras:
     - `Vcam Player Turn`: `VCam_PlayerTurn` をドラッグ&ドロップ
     - `Vcam AITurn`: `VCam_AITurn` をドラッグ&ドロップ
     - `Vcam Card Focus`: `VCam_CardFocus` をドラッグ&ドロップ
     - `Vcam AIReaction`: `VCam_AIReaction` をドラッグ&ドロップ

   - Priority Settings:
     - `Default Priority`: `10`
     - `Active Priority`: `15`

   - References:
     - `Ai Hand Transform`: `AIHand` のTransform（AI手札のルートオブジェクト）
     - `Player Hand Transform`: `PlayerHand` のTransform（プレイヤー手札のルートオブジェクト）
     - `Ai Face Transform`: AI顔オブジェクトがあればそのTransform（なければAIHandでOK）

---

## 手順 4: GameManager への CameraCinematicsSystem 接続

1. Hierarchy で `GameManager` を選択
2. Inspector の `Camera System (Phase 4)` セクション:
   - `Camera System`: 先ほど作成した `CameraCinematicsSystem` GameObject をドラッグ&ドロップ

---

## 検証方法

### 実行時の確認

1. **プレイモード開始**
   - ゲームが開始されたら、カメラがAI手札を見下ろすビュー（VCam_PlayerTurn）に切り替わることを確認

2. **プレイヤーターン時**
   - AIのカードをクリック → カメラが選択されたカードにズームイン（VCam_CardFocus）

3. **AIターン開始時**
   - AIターンに遷移 → カメラがプレイヤー手札を見下ろすビュー（VCam_AITurn）に切り替わることを確認

4. **AI反応時**
   - AIがカードを引いた後 → カメラがAIにフォーカス（VCam_AIReaction）

5. **ブレンドの滑らかさ**
   - カメラ切り替え時のブレンドが1.0秒（または0.5秒）でスムーズに行われることを確認

### デバッグ用ログ確認

Console に以下のログが出力されることを確認:
- `[CameraCinematicsSystem] Switching to Player Turn view (looking at AI hand)`
- `[CameraCinematicsSystem] Focusing on card: <CardName>`
- `[CameraCinematicsSystem] Switching to AI Turn view (looking at Player hand)`
- `[CameraCinematicsSystem] Switching to AI Reaction view`

### Context Menu テスト

CameraCinematicsSystem の Inspector で右クリック > Context Menu から以下を実行してテスト:
- `Test: Player Turn View`
- `Test: AI Turn View`
- `Test: AI Reaction View`

---

## トラブルシューティング

### カメラが切り替わらない

- Cinemachine Brain が Main Camera についているか確認
- 各 Virtual Camera の Priority が正しく設定されているか確認
- CameraCinematicsSystem の Inspector で全ての Virtual Camera が正しくアサインされているか確認

### カメラのブレンドが急すぎる/遅すぎる

- Cinemachine Brain の `Default Blend Time` を調整（推奨: 1.0秒）
- 各 Virtual Camera の Damping 値を調整（推奨: 0.5-1.0）

### VCam_CardFocus がカードに正しくフォーカスしない

- CardObject の Transform が正しく設定されているか確認
- CameraCinematicsSystem.FocusOnCard() が呼ばれているか Console で確認
- Follow Offset と Damping を調整（推奨: Follow Offset (0, 0.3, 0.3), Damping (0.8, 0.8, 0.8)）

### VCam_AIReaction がAIを見ない

- `Ai Face Transform` が正しくアサインされているか確認
- AI顔オブジェクトがない場合は AIHand を設定（フォールバック）

---

## 次のStage

Stage 2完了後、次はStage 3（FloatingTextSystem）の実装に進みます。

- FloatingTextSystem.cs の実装
- PsychologySystem との統合
- TextMeshPro 3D 浮遊テキスト表示

---

**作成日**: 2026-02-07
**Phase 4 - Stage 2**: Cinemachineカメラシステム
