# Cinemachine Auto Setup - 使用ガイド

## 概要

このエディタ拡張は、Phase 4 Stage 2のCinemachineカメラシステムを自動的にセットアップします。

## 使用方法

### 1. エディタウィンドウを開く

Unityエディタのメニューから：
```
Tools > Baba > Setup Cinemachine Cameras
```

### 2. 必要なコンポーネントを設定

エディタウィンドウが開いたら、以下のフィールドを設定します：

**Required Components（必須）:**
- **Main Camera**: シーンのメインカメラ（自動検索されます）
- **Game Manager**: GameManagerオブジェクト（自動検索されます）
- **AI Hand Transform**: AIの手札Transform（AIHandController）（自動検索されます）
- **Player Hand Transform**: プレイヤーの手札Transform（PlayerHandController）（自動検索されます）

**Optional Components（オプション）:**
- **AI Face Transform**: AIの顔Transform（存在する場合）
  - 存在しない場合は自動的にAI Hand Transformが使用されます

### 3. セットアップ実行

「Setup Cinemachine System」ボタンをクリックします。

確認ダイアログが表示されるので、「Yes」を選択します。

### 4. 作成されるもの

セットアップが完了すると、以下が自動作成されます：

1. **4つのVirtual Camera:**
   - `VCam_PlayerTurn` - プレイヤーターン時、AI手札を見下ろす
   - `VCam_AITurn` - AIターン時、プレイヤー手札を見下ろす
   - `VCam_CardFocus` - カード選択時、選択されたカードにズームイン
   - `VCam_AIReaction` - AI反応時、AIの顔/反応にフォーカス

2. **CameraCinematicsSystem GameObject:**
   - 4つのVirtual Cameraへの参照を保持
   - カメラ切り替えロジックを管理

3. **Main CameraにCinemachine Brain追加:**
   - 既に存在する場合はスキップ

4. **GameManagerへの接続:**
   - CameraCinematicsSystemへの参照が自動的に設定されます

### 5. 手動調整が必要な項目

自動セットアップ後、以下の項目は手動で調整してください：

#### 各Virtual Cameraの詳細設定

**VCam_PlayerTurn:**
- Body: Follow（None推奨）、Body設定を追加する場合は「Position Composer」や「Orbital Follow」など
- Aim: 「Look At Target」でAI Handを見るように設定

**VCam_AITurn:**
- Body: Follow（None推奨）
- Aim: 「Look At Target」でPlayer Handを見るように設定

**VCam_CardFocus:**
- Body: 「Position Composer」推奨
  - Follow Offset: (0, 0.3, 0.3) - カードの斜め上後ろ
  - Damping: (0.8, 0.8, 0.8) - 滑らかな移動
- Aim: 「Look At Target」
- Follow/LookAtは動的に設定されます（CameraCinematicsSystem.FocusOnCard()で設定）

**VCam_AIReaction:**
- Body: Follow（None推奨）
- Aim: 「Look At Target」でAI Face（またはAI Hand）を見るように設定

#### Cinemachine Brainの設定

Main CameraのCinemachine Brainコンポーネントで：
- **Default Blend**: `Ease In Out`, Time: `1.0` 秒
- **Update Method**: `Smart Update`
- **Blend Update Method**: `Late Update`

## トラブルシューティング

### カメラが切り替わらない

- Main CameraにCinemachine Brainがアタッチされているか確認
- 各Virtual CameraのPriorityが正しく設定されているか確認（Default: 10）
- CameraCinematicsSystemのInspectorで全てのVirtual Cameraが正しくアサインされているか確認

### カメラのフォーカスがずれている

- 各Virtual CameraのLookAt Transformが正しく設定されているか確認
- Transform位置とRotationを調整

### GameManagerへの接続が失敗

- GameManagerがシーンに存在するか確認
- シーンを保存してから再度セットアップを実行

## 既存のカメラがある場合

既存のVirtual CameraやCameraCinematicsSystemがシーンに存在する場合、自動セットアップは：
- 既存のオブジェクトを検索して再利用
- 設定を上書き更新

新規作成する必要がある場合は、既存のオブジェクトを削除してからセットアップを実行してください。

## Undo機能

セットアップは全てUndo対応しています。
間違えた場合は `Ctrl+Z` (Windows) / `Cmd+Z` (Mac) で元に戻せます。

---

**作成日**: 2026-02-07
**Phase 4 - Stage 2**: Cinemachine Auto Setup
