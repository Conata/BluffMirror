# 開発ロードマップ

## プロジェクト概要

### 開発目標
**2週間でプレイ可能な心理戦ババ抜きゲーム完成**

### 成功指標
- プレイヤーが「もう一回」と思う中毒性
- AIとの心理戦に夢中になる没入感  
- 60fps安定動作
- 従来UIに戻れなくなる革新性

## MVP定義（Minimum Viable Product）

### Core MVP（第1週完成目標）
```yaml
必須機能:
  ✅ Basic Gameplay:
    - カード配布・描画
    - ホバー・ドラッグ操作
    - ペア判定・消去
    - 勝利条件判定
    
  ✅ FPS Experience:
    - テーブル視点カメラ
    - カード扇状配置
    - 基本ライティング
    - 簡単音響効果
    
  ✅ Minimal AI:
    - ランダム選択AI
    - ターン管理
    - 基本勝敗判定

除外項目（後回し）:
  ❌ 心理圧システム
  ❌ セリフ生成  
  ❌ 高度演出
  ❌ 設定UI
```

### Enhanced MVP（第2週完成目標）
```yaml
追加機能:
  ✅ Psychology System:
    - 行動パターン分析
    - 基本セリフ表示
    - ホバー時投影テキスト
    
  ✅ Audio Enhancement:
    - AI音声（5-10フレーズ）
    - 空間音響
    - 心理圧効果音
    
  ✅ Visual Polish:
    - パーティクルエフェクト
    - ポストプロセシング
    - 微細アニメーション
```

## 開発フェーズ

### Phase 1: Foundation（1-3日目）
**目標**: 基本ゲームループ構築

#### Day 1: Project Setup
```yaml
Morning (3h):
  - Unity プロジェクト作成（URP）
  - 必須パッケージ導入
  - フォルダ構成・命名規則確立
  - Git リポジトリ設定

Afternoon (4h):
  - Scene 基本構成
  - Camera 設定（FPS視点）
  - Table + Lighting 配置
  - Card プレハブ作成

Evening (2h):
  - 基本クラス構造設計
  - Card.cs + CardObject.cs 実装
  - テスト用 Scene 構築
```

#### Day 2: Core Gameplay
```yaml
Morning (3h):
  - HandController 基底クラス
  - PlayerHandController 実装
  - カード配置・扇状ロジック

Afternoon (4h):
  - AIHandController 実装
  - CardDeck システム
  - 初期配布ロジック

Evening (2h):
  - ペア判定ロジック
  - DiscardPile システム
  - 基本テスト実行
```

#### Day 3: Input & Interaction
```yaml
Morning (3h):
  - Input System 設定
  - Raycast カード選択
  - Hover 反応実装

Afternoon (4h):
  - Drag & Drop システム
  - カード移動アニメーション
  - 基本効果音

Evening (2h):
  - GameManager 統合
  - ターン管理
  - 勝利条件判定
```

**Phase 1 完成時**: 基本ババ抜きが動作

### Phase 2: Polish & Experience（4-6日目）
**目標**: FPS体験の向上

#### Day 4: Visual Enhancement
```yaml
Morning (3h):
  - ライティング調整
  - マテリアル作成
  - テクスチャ適用

Afternoon (4h):
  - カードアニメーション精密化
  - パーティクル基本効果
  - カメラワーク（寄り・微揺れ）

Evening (2h):
  - UI配置（極小HUD）
  - フォント・配色調整
  - エフェクト統合テスト
```

#### Day 5: Audio & Atmosphere
```yaml
Morning (3h):
  - Audio System 構築
  - 効果音実装（カード操作）
  - 空間音響設定

Afternoon (4h):
  - 環境音追加
  - AI音声準備（仮素材）
  - AudioMixer 設定

Evening (2h):
  - 音響バランス調整
  - パフォーマンステスト
  - バグ修正
```

#### Day 6: Core MVP Testing
```yaml
Morning (3h):
  - 統合テスト
  - パフォーマンス最適化
  - バグ修正

Afternoon (4h):
  - 操作性調整
  - 視覚効果バランス
  - AI行動パターン調整

Evening (2h):
  - Core MVP 完成確認
  - プレイテスト（内部）
  - 改善点洗い出し
```

**Phase 2 完成時**: 気持ち良く遊べるFPS trampゲーム

### Phase 3: Psychology System（7-10日目）
**目標**: 心理戦システム実装

#### Day 7: Behavior Analysis
```yaml
Morning (3h):
  - PlayerBehaviorAnalyzer 実装
  - 行動履歴記録システム
  - パターン分析ロジック

Afternoon (4h):
  - PsychologySystem 基盤
  - 圧力レベル管理
  - 段階的反応システム

Evening (2h):
  - 行動データ可視化（デバッグ）
  - 分析精度テスト
  - パラメータ調整
```

#### Day 8: Dialogue System
```yaml
Morning (3h):
  - DialogueDatabase 作成
  - セリフカテゴリ分類
  - 基本セリフ作成（30個）

Afternoon (4h):
  - ProjectionSystem 実装
  - 空間テキスト表示
  - 振動・フェード効果

Evening (2h):
  - WhisperSystem 基礎
  - 音声再生システム
  - 空間音響配置
```

#### Day 9: Integration & Testing
```yaml
Morning (3h):
  - Psychology + Dialogue 統合
  - セリフトリガー調整
  - 圧力レベル連動

Afternoon (4h):
  - AI音声録音・編集
  - 音声ファイル統合
  - 表示タイミング調整

Evening (2h):
  - 心理圧効果テスト
  - バランス調整
  - パフォーマンス確認
```

#### Day 10: Advanced Effects
```yaml
Morning (3h):
  - DistortionSystem 実装
  - ポストエフェクト連動
  - 画面歪み演出

Afternoon (4h):
  - セリフ内容拡充（50個）
  - シチュエーション別調整
  - AI personality 微調整

Evening (2h):
  - Enhanced MVP テスト
  - 総合バランス調整
  - リリース準備
```

**Phase 3 完成時**: 心理戦を楽しめる完成品

### Phase 4: Polish & Release（11-14日目）
**目標**: リリース品質向上

#### Day 11-12: Quality Assurance
```yaml
Day 11:
  - 全機能統合テスト
  - パフォーマンス最適化
  - メモリリーク対策
  
Day 12:
  - プレイテスト（外部）
  - フィードバック収集
  - 改善優先順位決定
```

#### Day 13-14: Final Polish
```yaml
Day 13:
  - 重要バグ修正
  - UI/UX微調整
  - セリフ品質向上
  
Day 14:
  - 最終ビルド作成
  - リリース準備
  - ドキュメント整備
```

## リスク管理

### 技術リスク

#### 高リスク項目
```yaml
1. パフォーマンス問題:
   軽減策: 
     - 早期プロファイリング
     - LOD システム
     - 段階的最適化
   
2. 心理圧システムの複雑性:
   軽減策:
     - シンプルなルールベースから開始
     - 段階的機能追加
     - A/B テスト

3. AI音声品質:
   軽減策:
     - 複数音声エンジンの検討
     - 音声なしフォールバック
     - テキストのみモード
```

#### 中リスク項目  
```yaml
1. 操作性の調整:
   軽減策: 早期プレイテスト、段階的改善

2. アート品質:
   軽減策: アセットストア活用、MVP重視

3. サウンド統合:
   軽減策: フリー素材活用、段階的実装
```

### スケジュールリスク

#### 遅延対応策
```yaml
1週間遅延時:
  - 心理圧システム簡易化
  - セリフ数削減（20個まで）
  - 高度エフェクト除外

2週間遅延時:  
  - Core MVP のみでリリース
  - 心理圧機能を将来アップデート
  - 基本ゲーム性重視
```

## 品質保証

### テストスケジュール
```yaml
毎日: 
  - 基本動作確認
  - ビルドテスト
  - パフォーマンスチェック

Phase完了時:
  - 統合テスト
  - プレイテスト
  - フィードバック収集

最終週:
  - 外部テスター参加
  - 本格的QAテスト
  - リリース判定
```

### 完成度指標
```yaml
Core MVP (70%): 基本ゲームが完動
Enhanced MVP (85%): 心理戦が楽しい
Polish版 (95%): リリース品質
Perfect版 (100%): 期待を超える完成度
```

## 成果物

### 開発成果物
- Unity プロジェクト（フルソース）
- ビルド済み実行ファイル
- 技術ドキュメント
- プレイヤーマニュアル

### 追加成果物
- 開発ブログ記事
- プレイテスト結果レポート
- 心理圧システム技術解説
- 将来機能ロードマップ

## ポストリリース計画

### アップデート計画（1ヶ月以内）
```yaml
v1.1 - Performance Update:
  - 最適化改善
  - バグ修正
  - 設定UI追加

v1.2 - Content Update:
  - セリフ追加（100個）
  - AI難易度調整
  - 新しい心理圧パターン

v1.3 - Experience Update:
  - 追加演出
  - サウンド拡充
  - アクセシビリティ向上
```

### 長期展望（6ヶ月）
- マルチプレイヤー対応
- VR版開発
- モバイル移植
- AI学習システム

---
**Document Version**: 1.0  
**Last Updated**: 2026-02-07  
**Project Status**: 開発準備完了