# FPS Trump Game - 仕様書

**一人称視点心理戦ババ抜きゲーム**

## 📋 ドキュメント構成

| File | Description | Status |
|------|------------|---------|
| [00-Overview.md](00-Overview.md) | ゲーム概要・コンセプト | ✅ 完成 |
| [01-UI-UX-Design.md](01-UI-UX-Design.md) | UI/UX設計・FPS視点仕様 | ✅ 完成 |
| [02-Technical-Specification.md](02-Technical-Specification.md) | 技術仕様・アーキテクチャ | ✅ 完成 |
| [03-Implementation-Guide.md](03-Implementation-Guide.md) | 実装ガイド・コンポーネント設計 | ✅ 完成 |
| [04-Art-Sound-Specification.md](04-Art-Sound-Specification.md) | アート・サウンド仕様 | ✅ 完成 |
| [05-Psychology-Dialogue-System.md](05-Psychology-Dialogue-System.md) | 心理圧・セリフシステム | ✅ 完成 |
| [06-Development-Roadmap.md](06-Development-Roadmap.md) | 開発ロードマップ・スケジュール | ✅ 完成 |
| [07-AI-NPC-Behavior-Specification.md](07-AI-NPC-Behavior-Specification.md) | AI・NPC挙動詳細仕様 | ✅ 完成 |
| [08-Scene-Objects-Materials-Timeline.md](08-Scene-Objects-Materials-Timeline.md) | シーン構成・マテリアル・Timeline | ✅ 完成 |
| [09-GameManager-Implementation.md](09-GameManager-Implementation.md) | GameManager実装ガイド | ✅ 完成 |
| [10-Personality-Analysis-System.md](10-Personality-Analysis-System.md) | **パーソナリティ分析システム** | ✅ 完成 |

## 🎮 ゲーム概要

### コアコンセプト
「カードUI」ではなく「テーブルの向こうに相手がいる」FPS視点でのババ抜きゲーム。
AIのセリフが空間に浮き、プレイヤーの判断に心理的圧力をかける革新的な体験。

### 技術スタック
- **Engine**: Unity 2023.3 LTS + URP
- **Target**: Desktop (Windows/Mac/Linux)
- **Performance**: 60fps @ 1080p
- **Development Period**: 2週間

## 🔑 核心機能

### 1. FPS視点体験
- 座っているプレイヤーの目線
- テーブル上のカードを直接つまんで操作
- リアルタイムライティング・影響果

### 2. 心理圧システム
- **3層構造**: 囁き + 空間投影 + 画面歪み
- **行動分析**: プレイヤーの癖を学習
- **段階的圧力**: 行動パターンに応じた心理圧調整

### 3. 没入型UI
- 従来のゲームUIを排除
- テーブル上の小物で状態表現
- 最小限HUDで没入感維持

## 📁 プロジェクト構造

```
fps-trump-unity/
├── Doc/                    # 仕様書（このフォルダ）
│   ├── README.md          # このファイル
│   ├── 00-Overview.md     # ゲーム概要
│   ├── 01-UI-UX-Design.md # UI/UX仕様
│   ├── 02-Technical-Specification.md # 技術仕様
│   ├── 03-Implementation-Guide.md # 実装ガイド
│   ├── 04-Art-Sound-Specification.md # アート仕様
│   ├── 05-Psychology-Dialogue-System.md # 心理圧システム
│   └── 06-Development-Roadmap.md # 開発計画
│
├── Assets/                # Unity アセット
│   ├── Scripts/          # C# スクリプト
│   ├── Models/           # 3D モデル
│   ├── Materials/        # マテリアル
│   ├── Textures/         # テクスチャ
│   ├── Audio/            # 音響ファイル
│   ├── Scenes/           # Unity シーン
│   └── Prefabs/          # プレハブ
│
└── ProjectSettings/       # Unity プロジェクト設定
```

## 🚀 開発フェーズ

### Phase 1: Foundation (1-3日)
基本ゲームループ構築
- Unity プロジェクト設定
- 基本カード操作
- ターン管理システム

### Phase 2: Polish & Experience (4-6日)
FPS体験向上
- ライティング・マテリアル
- アニメーション・エフェクト
- 音響システム

### Phase 3: Psychology System (7-10日)
心理戦システム実装
- 行動パターン分析
- セリフ生成・表示
- 心理圧演出

### Phase 4: Polish & Release (11-14日)
リリース品質向上
- 品質保証・テスト
- パフォーマンス最適化
- 最終調整

## 💎 革新ポイント

### 1. UI革命
- 「画面」から「空間」へのパラダイムシフト
- 没入感を損なわない情報表示
- 物理操作による直感的インタラクション

### 2. 心理戦演出
- プレイヤー行動の学習・分析
- リアルタイム心理圧生成
- 空間音響による臨場感

### 3. 技術革新
- Unity URP による高品質ライティング
- 行動分析AI による動的コンテンツ
- 空間UI による新しいゲーム体験

## 🎯 成功指標

### プレイヤー体験
- 「もう一回」と自然に思う中毒性
- AIとの心理戦に夢中になる
- 従来UIに戻れなくなる革新性

### 技術指標
- 60fps安定動作
- <5秒起動時間
- <100ms入力遅延

## 📞 連絡先

**Development Team**: ネコ店長 + mekkezzo  
**Project Start**: 2026-02-07  
**Target Release**: 2026-02-21  

---

**このドキュメント群は実装のための完全な設計図です。**  
**各ドキュメントは独立して参照可能で、開発中の迷いを最小化するよう設計されています。**

### 📖 読み方ガイド

**開発開始前**: 全ドキュメント通読推奨  
**実装中**: 該当フェーズのドキュメント集中参照  
**デバッグ時**: Technical Specification + Implementation Guide  
**調整時**: Art/Sound Specification + Psychology System  

**Good Luck! 🎮✨**