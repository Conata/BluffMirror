# アート・サウンド仕様書

## ビジュアルデザイン

### 全体コンセプト
**"暗い照明のプライベートゲーム"**
- 映画「Casino Royale」のポーカーシーンの緊張感
- フィルムノワールの陰影コントラスト
- 高級クラブの秘密性・排他性

### カラーパレット
```
Primary Colors:
  - Deep Green (Felt): #1B3B1B
  - Warm Gold (Accent): #D4AF37
  - Dark Brown (Table): #3C2B1C
  
Secondary Colors:
  - Cool Blue (Shadows): #1E3A5F
  - Warm Orange (Key Light): #FF8C42
  - Deep Red (Danger/Pressure): #8B0000
  
UI Colors:
  - Text Light: #F5F5DC
  - Text Dark: #2F2F2F
  - Warning: #FF6B6B
```

## ライティング設計

### 3点照明システム

#### Key Light (主光源)
```yaml
Type: Spot Light
Position: (0.8, 2.8, -1.2)
Rotation: (45, -30, 0)
Color: Warm Orange (#FF8C42)
Intensity: 2.0
Angle: 35°
Penumbra: 0.6
Shadow Type: Hard Shadows
Shadow Resolution: 2048
```

#### Fill Light (環境光)
```yaml
Type: Area Light  
Position: (-0.6, 1.6, 1.6)
Color: Cool Blue (#6495ED)
Intensity: 0.4
Range: 4.0
Shadow: Off
```

#### Rim Light (輪郭光)
```yaml
Type: Point Light
Position: (0, 2.2, -2.2)
Color: Warm Gold (#D4AF37)
Intensity: 0.8
Range: 5.0
Shadow: Off
```

### 環境光設定
```yaml
Ambient Lighting:
  Source: Gradient
  Sky Color: #2F2F3F
  Equator Color: #1F1F2F
  Ground Color: #0F0F1F
  
Fog Settings:
  Type: Exponential Squared
  Color: #1A1A2E
  Density: 0.06
  Start Distance: 1.0
  End Distance: 15.0
```

## マテリアル仕様

### テーブル（フェルト）
```yaml
Material: Table_Felt
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo: Deep Green (#1B3B1B)
  Metallic: 0.0
  Smoothness: 0.15
  Normal Map: Felt_Normal (subtle fabric texture)
  Emission: Off
  Tiling: (2, 2)
```

### カード（表面）
```yaml
Material: Card_Front
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo: Card Texture Atlas
  Metallic: 0.1
  Smoothness: 0.65
  Normal Map: Card_Normal (paper texture)
  Edge Wear Map: Card_Wear (角の摩耗表現)
```

### カード（裏面）
```yaml
Material: Card_Back
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo: Classic Pattern (#000080 base)
  Metallic: 0.05
  Smoothness: 0.7
  Normal Map: Card_Normal
  Detail Mask: Pattern_Detail
```

### AI顔部分（仮面）
```yaml
Material: AI_Mask
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo: Dark Metal (#2F2F2F)
  Metallic: 0.8
  Smoothness: 0.9
  Normal Map: Metal_Brushed
  Emission: Eyes_Glow (#FF0000, Intensity: 0.5)
```

## パーティクルエフェクト

### カードペア消失演出
```yaml
System: Card_Disappear_Effect
Duration: 0.5s
Components:
  1. Glow_Buildup:
     - Duration: 0.1s
     - Emission: 50 particles/sec
     - Color: Gold to White
     - Size: 0.02 to 0.05
     
  2. Dissolve_Sparkles:
     - Duration: 0.3s
     - Emission: 200 particles/sec
     - Color: White to Transparent
     - Velocity: Upward spiral
     - Size: 0.005 to 0.001
     
  3. Final_Flash:
     - Duration: 0.1s
     - Emission: Burst 20 particles
     - Color: Bright White
     - Size: 0.1 to 0
```

### カードホバー演出
```yaml
System: Card_Hover_Aura
Duration: Continuous (while hovering)
Emission: 10 particles/sec
Color: Soft Gold (#D4AF37, Alpha: 0.3)
Shape: Card outline
Movement: Gentle floating
Size: 0.01 constant
```

### 心理圧演出（画面効果）
```yaml
System: Pressure_Distortion
Trigger: High pressure events
Components:
  1. Screen_Distortion:
     - Chromatic Aberration: +0.3
     - Vignette: +0.4
     - Film Grain: +0.2
     
  2. Color_Shift:
     - Temperature: -20 (colder)
     - Saturation: -30
     - Contrast: +20
```

## テクスチャアトラス設計

### カードテクスチャアトラス (2048x2048)
```
Layout:
+--------+--------+--------+--------+
| Hearts | Hearts | Hearts | Hearts |
| A-3    | 4-6    | 7-9    | 10-K   |
+--------+--------+--------+--------+
| Diamonds        | Clubs   | Spades |
| A-K             | A-K     | A-K    |
+--------+--------+--------+--------+
| Joker  | Back   | Special | Wear   |
| Red    | Pattern| States  | Maps   |
+--------+--------+--------+--------+
```

### UI要素アトラス (1024x1024)
```
Layout:
+--------+--------+
| Icons  | Buttons|
| 32x32  | 128x64 |
+--------+--------+
| Text   | Effects|
| Frames | Glows  |
+--------+--------+
```

## 3Dアセット仕様

### テーブルモデル
```yaml
Geometry:
  - Vertices: <2000
  - Triangles: <3000
  - UV Channels: 2 (Diffuse + Lightmap)
  
Components:
  - Table Surface: Plane with beveled edges
  - Table Edge: Rounded wooden trim
  - Table Legs: Simple cylindrical (mostly hidden)
  
LOD Levels:
  - LOD0: Full detail (0-5m)
  - LOD1: Reduced detail (5-10m)
  - LOD2: Very low detail (10m+)
```

### カードモデル
```yaml
Geometry:
  - Vertices: 24
  - Triangles: 44
  - Dimensions: 0.063 x 0.088 x 0.001 (Standard playing card ratio)
  
Features:
  - Slightly rounded corners
  - Beveled edges for light catching
  - Proper UV mapping for front/back textures
  
Optimization:
  - Shared mesh for all cards
  - Material variations for suits/ranks
```

## サウンドデザイン

### 効果音ライブラリ

#### カード操作音
```yaml
Card_Hover:
  - File: card_hover.wav
  - Format: 16bit, 48kHz
  - Volume: -20dB
  - Pitch Variation: ±0.1 semitones
  
Card_Pick:
  - File: card_pick.wav
  - Format: 16bit, 48kHz  
  - Volume: -15dB
  - 3D Positional: Yes
  
Card_Place:
  - File: card_place.wav
  - Format: 16bit, 48kHz
  - Volume: -18dB
  - Reverb: Table surface reflection
  
Card_Flip:
  - Files: card_flip_01.wav, card_flip_02.wav
  - Random selection
  - Pitch: Based on flip speed
```

#### 環境音
```yaml
Room_Ambience:
  - File: room_ambience.wav
  - Format: 16bit, 48kHz, Stereo
  - Volume: -30dB
  - Loop: Seamless
  - Content: Subtle air conditioning, distant city
  
Table_Felt_Friction:
  - File: felt_slide.wav
  - Trigger: Card dragging on table
  - Volume: -25dB
  - Pitch: Based on drag speed
```

#### 心理圧音響効果
```yaml
Heartbeat:
  - File: heartbeat_subtle.wav
  - Trigger: High pressure moments
  - Volume: -22dB
  - Filter: Low-pass, gradually increase cutoff
  - Spatial: Center chest position
  
Whisper_Ambience:
  - File: whisper_base.wav
  - Volume: -35dB
  - Processing: Heavy reverb, spatial positioning
  - Trigger: AI dialogue moments
```

### AI音声仕様

#### 音声特性
```yaml
Voice Profile:
  Gender: Androgynous/Male-leaning
  Age: 30-40 equivalent
  Accent: Neutral with slight formality
  Tone: Calm, calculated, occasionally menacing
  
Technical Specs:
  Sample Rate: 48kHz
  Bit Depth: 16bit
  Format: WAV (uncompressed)
  Processing: Minimal compression, subtle reverb
```

#### セリフカテゴリ別音響処理
```yaml
Hover_Comments:
  - Volume: -20dB
  - Spatial: Close positioning (1m from player ear)
  - Processing: Subtle whisper reverb
  
Turn_Dialogue:
  - Volume: -15dB  
  - Spatial: Across table (2m distance)
  - Processing: Room reverb, clear articulation
  
Pressure_Lines:
  - Volume: -10dB
  - Spatial: Surround positioning
  - Processing: Echo, slight distortion for unease
```

### 空間音響設定

#### Audio Listener Settings
```yaml
Doppler Factor: 1.0
Speed of Sound: 343 (default)
Volume Rolloff: Logarithmic
Max Distance: 10.0
```

#### Reverb Zone (Table Area)
```yaml
Reverb Preset: Room
Dry Level: 0dB
Room: -1000
Room HF: -600
Room Rolloff: 0.0
Decay Time: 1.4s
Decay HF Ratio: 0.8
Reverb Level: -200
Reverb Delay: 0.02s
HF Reference: 3000Hz
LF Reference: 200Hz
Diffusion: 100%
Density: 80%
```

## パフォーマンス最適化

### テクスチャ最適化
- カードアトラス: ASTC 6x6 (モバイル) / BC7 (PC)
- UI要素: ASTC 4x4 (モバイル) / BC7 (PC)  
- ノーマルマップ: BC5 (PC) / ASTC 5x5 (モバイル)
- ミップマップ: 全テクスチャで有効

### メッシュ最適化
- LOD自動生成: Unity ProBuilder
- オクルージョンカリング: 有効
- フラスタムカリング: 自動
- バッチング: Static Batching (環境) + Dynamic Batching (カード)

### ライト最適化
- リアルタイムライト: 最大3個
- ベイクドライト: 環境光のみ
- ライトマップ解像度: 512x512 (十分)
- シャドウカスケード: 2段階

---
**Document Version**: 1.0  
**Last Updated**: 2026-02-07  
**Next**: 心理圧・セリフシステム仕様