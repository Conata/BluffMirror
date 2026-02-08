# 技術仕様書

## 技術スタック

### Unity構成
- **Unity Version**: 2023.3 LTS (推奨)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Scripting Backend**: IL2CPP (パフォーマンス重視)
- **API Compatibility**: .NET Standard 2.1

### 必須パッケージ
```
com.unity.render-pipelines.universal    (URP)
com.unity.postprocessing                (ポストエフェクト)
com.unity.cinemachine                   (カメラ制御)
com.unity.timeline                      (アニメーション)
com.unity.addressables                  (アセット管理)
com.unity.audio.mixing                  (サウンド)
com.unity.inputsystem                   (新入力システム)
```

### 外部ライブラリ
- **DOTween**: アニメーション制御
- **Odin Inspector**: エディタ拡張（開発効率化）
- **TextMeshPro**: テキスト描画（標準搭載）

## システムアーキテクチャ

### 全体構成
```
GameManager (Singleton)
├── SceneController
├── GameLogicManager
├── UIManager
├── AudioManager
├── InputManager
└── AIManager

Scene Hierarchy
├── Environment
│   ├── Table
│   ├── Lighting
│   └── PostProcessing
├── GameObjects
│   ├── PlayerHandController
│   ├── AIHandController
│   ├── CardManager
│   └── DiscardPile
├── UI
│   ├── HUD (Minimal)
│   ├── FloatingTextSystem
│   └── DebugPanel (Development only)
├── Audio
│   ├── MasterMixer
│   ├── SFXSources
│   └── BGMSource
└── Cameras
    ├── MainCamera (Cinemachine)
    └── UICamera
```

### データフロー
```
Input → InputManager → GameLogicManager → UI/Audio更新
                                       → AI判断
                                       → Scene更新
```

## レンダリング仕様

### URP設定
```yaml
Renderer Features:
  - Screen Space Ambient Occlusion
  - Decal Renderer Feature (カード演出用)
  - Render Objects (アウトライン用)

Pipeline Asset設定:
  - HDR: On
  - MSAA: 4x
  - Shadow Distance: 10
  - Shadow Resolution: 2048
  - Additional Lights: Per Pixel
```

### ライティング設計
```
Key Light (Spot): AI側から斜め
  - Position: (0.8, 2.8, -1.2)
  - Angle: 35°
  - Penumbra: 0.6
  - Intensity: 1.5
  - Shadows: Hard

Fill Light (Area): 環境光補填
  - Position: (-0.6, 1.6, 1.6)  
  - Range: 4.0
  - Intensity: 0.4
  - Shadows: Off

Rim Light (Point): 背後から輪郭
  - Position: (0, 2.2, -2.2)
  - Range: 5.0
  - Intensity: 0.8
  - Color: 暖色系
```

### ポストプロセシング
```
Volume Profile:
  - Vignette: Intensity 0.3, Smoothness 0.4
  - Chromatic Aberration: Intensity 0.1 (心理圧時に増加)
  - Color Grading: 
    * Temperature: -10 (クールな雰囲気)
    * Contrast: +15
    * Saturation: -20 (リアル寄り)
  - Bloom: Threshold 1.1, Intensity 0.3
  - Film Grain: Intensity 0.1 (質感向上)
```

## パフォーマンス仕様

### 目標フレームレート
- **Desktop**: 60fps (1080p)
- **Mobile**: 30fps (720p) ※将来対応時

### 最適化戦略
```
Draw Call削減:
  - Static Batching (テーブル・環境)
  - GPU Instancing (同種カード)
  - Texture Atlas (カードテクスチャ)

メモリ最適化:
  - Addressables によるアセット管理
  - Object Pooling (パーティクル・UI)
  - Texture Streaming

処理最適化:
  - Coroutine活用（重処理の分散）
  - FixedUpdate分離（物理演算）
  - 非同期処理（AI応答・ファイルIO）
```

### 制約事項
- **影生成**: Key Light 1つのみ
- **リフレクション**: Reflection Probe 固定
- **パーティクル**: 同時50個以下
- **ポリゴン**: Scene全体で5K tri以下

## 入力システム設計

### Unity Input System
```csharp
// Input Action Asset 構成
[InputActionMap("Gameplay")]
public class GameplayActions
{
    public InputAction Look;      // マウス移動
    public InputAction Click;     // クリック（カード選択）
    public InputAction Drag;      // ドラッグ（カード移動）
    public InputAction Cancel;    // ESC（メニュー）
}
```

### レイキャスト仕様
```csharp
// カード選択用レイキャスト
LayerMask cardLayer = 1 << 8;  // Cards layer
float maxDistance = 10f;
bool usePhysicsRaycaster = true; // 3D衝突判定
```

## セーブシステム

### データ構造
```json
{
  "gameData": {
    "playerStats": {
      "gamesPlayed": 0,
      "gamesWon": 0,
      "averageGameTime": 0
    },
    "settings": {
      "masterVolume": 0.8,
      "sfxVolume": 0.9,
      "cameraShake": true,
      "tutorialCompleted": false
    },
    "aiPersonality": {
      "aggressionLevel": 0.5,
      "vocabularySet": "standard"
    }
  }
}
```

### 保存タイミング
- ゲーム終了時
- 設定変更時
- プレイ統計更新時

## デバッグ・開発ツール

### コンソールコマンド
```csharp
[Console]
public static void SkipToEnd() { ... }

[Console]  
public static void SetAIPersonality(float aggression) { ... }

[Console]
public static void ShowCardInfo() { ... }
```

### 開発用UI
- FPS表示
- Draw Call カウンタ
- メモリ使用量
- AI状態ビューア
- ゲーム状態デバッガ

## ビルド設定

### Development Build
```
Configuration: Debug
Script Debugging: On
Deep Profiling: On
IL2CPP Code Generation: Faster Runtime
```

### Release Build  
```
Configuration: Master
Script Debugging: Off
IL2CPP Code Generation: Faster Build
Stripping Level: Medium
```

## 拡張性考慮

### 将来的な追加要素
- マルチプレイヤー対応 (Netcode for GameObjects)
- VR対応 (XR Toolkit)
- モバイル対応 (タッチ入力)
- AI難易度調整システム

### モジュラー設計
各システムを独立性高く設計し、機能追加時の影響を最小化

---
**Document Version**: 1.0  
**Last Updated**: 2026-02-07  
**Next**: 実装ガイド作成