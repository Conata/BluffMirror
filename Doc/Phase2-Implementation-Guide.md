# Phase2 実装ガイド

## 概要
Phase2「FPS体験向上」の実装手順書です。スクリプトは既に実装済みのため、Unity Editorでの設定手順を説明します。

## 実装済みスクリプト一覧

### Environment（環境）
- ✅ `LightingSetup.cs` - 3点照明システムの自動セットアップ
- ✅ `PostProcessingController.cs` - ポストプロセシング効果管理
- ✅ `MaterialSetup.cs` - マテリアルプロパティ設定

### Effects（エフェクト）
- ✅ `CardEffectsManager.cs` - カードビジュアルエフェクト管理

### Audio（音響）
- ✅ `AudioManager.cs` - 音響システム管理
- ✅ `ReverbZoneSetup.cs` - 空間音響設定

### 既存スクリプト更新
- ✅ `PlayerHandController.cs` - CardEffectsManagerと統合
- ✅ `CardObject.cs` - ホバーオーラエフェクト追加
- ✅ `FPSCameraController.cs` - 既に実装済み（呼吸、フォーカス、シェイク）

---

## Unity Editor セットアップ手順

### Step 1: シーンにManager GameObjectを配置

1. **Hierarchy** ウィンドウで右クリック → `Create Empty`
2. 名前を `_Managers` に変更
3. 以下の子オブジェクトを作成：

```
_Managers/
├── LightingManager (Empty GameObject)
├── PostProcessingVolume (Empty GameObject)
├── MaterialManager (Empty GameObject)
├── AudioManager (Empty GameObject)
├── CardEffectsManager (Empty GameObject)
└── ReverbZone (Empty GameObject)
```

---

### Step 2: ライティングシステムの設定

#### 2.1 LightingSetup コンポーネント追加

1. `LightingManager` GameObject を選択
2. **Inspector** で `Add Component` → `Lighting Setup` を追加
3. 以下の設定を行う：
   - ✅ `Auto Setup On Start` をチェック
   - ✅ `Show Debug Gizmos` をチェック（開発中のみ）

#### 2.2 ライトの自動生成

- プレイモード開始時に自動的に3つのライトが生成されます：
  - **Key Light** (Spot Light) - Warm Orange
  - **Fill Light** (Point Light) - Cool Blue
  - **Rim Light** (Point Light) - Warm Gold

#### 2.3 手動でライトを配置する場合

1. **Hierarchy** で右クリック → `Light` → `Spot Light` を3つ作成
2. `LightingManager` の **Inspector** で各ライトを参照設定：
   - `Key Light` → 作成したSpot Light
   - `Fill Light` → 作成したPoint Light
   - `Rim Light` → 作成したPoint Light
3. **Context Menu** (右クリック) → `Setup All Lighting` を実行

---

### Step 3: ポストプロセシングの設定

#### 3.1 Volume コンポーネント追加

1. `PostProcessingVolume` GameObject を選択
2. `Add Component` → `Volume` を追加
3. `Add Component` → `Post Processing Controller` を追加

#### 3.2 Volume Profile 作成

1. **Project** ウィンドウで右クリック
2. `Create` → `Volume Profile` を選択
3. 名前を `MainVolumeProfile` に変更
4. `PostProcessingVolume` の `Volume` コンポーネントで `Profile` に割り当て

#### 3.3 Volume設定

- `Is Global` をチェック ✅
- `Priority` を `1` に設定

**注意**: PostProcessingControllerが自動的に以下のエフェクトを追加します：
- Vignette
- Chromatic Aberration
- Color Adjustments
- Film Grain
- Depth of Field

---

### Step 4: マテリアルの設定

#### 4.1 MaterialSetup コンポーネント追加

1. `MaterialManager` GameObject を選択
2. `Add Component` → `Material Setup` を追加

#### 4.2 マテリアルの参照設定

1. **Project** ウィンドウで各マテリアルを探す：
   - `Assets/Materials/Environment/Table_Felt.mat`
   - `Assets/Materials/Cards/Card_Front.mat`
   - `Assets/Materials/Cards/Card_Back.mat`
   - `Assets/Materials/Environment/Floor_Dark.mat`

2. MaterialSetup コンポーネントに各マテリアルをドラッグ＆ドロップ

3. **Context Menu** → `Setup All Materials` を実行

#### 4.3 AI Mask マテリアル作成（オプション）

AIキャラクターを実装する場合：

1. **Project** → `Assets/Materials` フォルダで右クリック
2. `Create` → `Material` → 名前を `AI_Mask` に変更
3. Shader を `Universal Render Pipeline/Lit` に設定
4. MaterialSetup の `Ai Mask Material` に割り当て

---

### Step 5: エフェクトマネージャーの設定

#### 5.1 CardEffectsManager コンポーネント追加

1. `CardEffectsManager` GameObject を選択
2. `Add Component` → `Card Effects Manager` を追加

#### 5.2 エフェクト設定

- `Disappear Duration` = `0.5`
- `Glow Color` = `#D4AF37` (Warm Gold)
- `Glow Intensity` = `2.0`

**注意**: パーティクルプリファブは不要です。スクリプトがプロシージャルに生成します。

---

### Step 6: オーディオシステムの設定

#### 6.1 AudioManager コンポーネント追加

1. `AudioManager` GameObject を選択
2. `Add Component` → `Audio Manager` を追加

#### 6.2 Audio Mixer 作成

1. **Project** ウィンドウで `Assets/Audio` フォルダを作成
2. 右クリック → `Create` → `Audio Mixer`
3. 名前を `MainAudioMixer` に変更

#### 6.3 Audio Mixer Groups 設定

1. `MainAudioMixer` を開く
2. 以下のGroupsを作成：
   - `Master`
     - `SFX`
     - `Music`
     - `Ambience`
     - `Voice`

3. AudioManager の Inspector で各Groupを割り当て

#### 6.4 音声ファイルの準備（オプション）

音声ファイルがある場合は `Assets/Audio/SFX` フォルダに配置し、AudioManagerの対応するフィールドに割り当てます。

**必要な音声ファイル:**
- `card_hover.wav`
- `card_pick.wav`
- `card_place.wav`
- `card_flip_01.wav`, `card_flip_02.wav`
- `room_ambience.wav`
- `felt_slide.wav`
- `heartbeat_subtle.wav`
- `whisper_base.wav`

**注意**: 音声ファイルが無くてもゲームは動作します。後で追加可能です。

---

### Step 7: Reverb Zone の設定

#### 7.1 ReverbZoneSetup コンポーネント追加

1. `ReverbZone` GameObject を選択
2. `Add Component` → `Audio Reverb Zone` を追加
3. `Add Component` → `Reverb Zone Setup` を追加

#### 7.2 位置設定

- Transform Position を `(0, 1.0, 0)` に設定（テーブルの中心）

#### 7.3 Reverb設定

- **Context Menu** → `Apply Custom Settings (Spec)` を実行

これで仕様書通りのリバーブ設定が適用されます。

---

### Step 8: シーン内のGameObjectへの統合

#### 8.1 GameManager にエフェクト連携

`GameManager` GameObject を選択し、必要に応じて以下を参照：
- PostProcessingController
- CardEffectsManager
- AudioManager

#### 8.2 カメラにPostProcessing Volume追加

1. `Main Camera` GameObject を選択
2. 既に `Post Processing Controller` がシーンにあれば不要

#### 8.3 Audio Listener確認

- `Main Camera` に `Audio Listener` コンポーネントがあることを確認
- 無ければ追加

---

## テスト手順

### ライティングテスト

1. プレイモードに入る
2. Scene ビューで3つのライトが配置されているか確認
3. Game ビューで陰影が適切か確認

### エフェクトテスト

1. プレイモードでカードにホバー → ホバーオーラが表示されるか
2. カードペアを作成 → 消失エフェクトが再生されるか

### ポストプロセシングテスト

1. PostProcessingController の Inspector を開く
2. **Context Menu** → `Test Pressure Effect (50%)` を実行
3. 画面にビネット、色調変化が適用されるか確認
4. **Context Menu** → `Release Pressure Effect` で元に戻るか確認

### オーディオテスト

1. AudioManager の Inspector を開く
2. **Context Menu** → `Test Card Hover` を実行
3. 音が再生されるか確認（音声ファイルがある場合）

---

## 最適化設定

### Quality Settings

1. **Edit** → **Project Settings** → **Quality**
2. 以下の設定を推奨：
   - `Anti Aliasing` → `4x Multi Sampling`
   - `Anisotropic Textures` → `Per Texture`
   - `Shadow Resolution` → `High Resolution`
   - `Shadow Distance` → `20`

### URP Asset Settings

1. **Project Settings** → **Graphics**
2. `Scriptable Render Pipeline Settings` で URP Asset を確認
3. 推奨設定：
   - `Rendering` → `Render Scale` → `1.0`
   - `Lighting` → `Main Light` → Cast Shadows ✅
   - `Post-processing` → Enabled ✅

---

## トラブルシューティング

### ライトが表示されない

- LightingSetup の `Auto Setup On Start` がチェックされているか確認
- Console でエラーメッセージを確認

### ポストプロセシングが効かない

- Volume の `Is Global` がチェックされているか確認
- URP Asset で Post-processing が有効か確認

### 音が鳴らない

- AudioManager の各AudioSourceが正しく初期化されているか確認
- Audio Mixer Groups が正しく割り当てられているか確認
- 音声ファイルが割り当てられているか確認

### パーティクルが表示されない

- CardEffectsManager GameObject が Active か確認
- Console でエラーメッセージを確認

---

## 次のステップ

Phase2 が完了したら、以下を確認：

✅ ライティングが適切に設定され、雰囲気のある画面になっている
✅ カードのホバーエフェクトが動作している
✅ カードペア消失エフェクトが動作している
✅ ポストプロセシングが正しく適用されている
✅ 音響システムが初期化されている（音声ファイルは後で追加可）

**Phase3** (心理圧システム) に進む準備が整いました！

---

**Document Version**: 1.0
**Created**: 2026-02-07
**Status**: Phase2 Implementation Complete
