# Baba（ババ抜き）開発 TODO リスト

## 🎯 現在のステータス

### ✅ 完了済み
- [x] **Phase 1**: 基本ゲームシステム実装
  - カード、デッキ、手札管理
  - ゲームステート管理
  - プレイヤー/AIターン制御

- [x] **Phase 2**: FPS体験向上
  - ライティングシステム（3点照明）
  - ポストプロセシング効果
  - カードビジュアルエフェクト
  - 音響システム（空間音響、リバーブ）
  - 言語システム（日英バイリンガル対応）

- [x] **Phase 3**: 心理システム基盤
  - PsychologySystem 実装
  - PlayerBehaviorAnalyzer 実装
  - AI選択ロジック統合

- [x] **Phase 4 Stage 1-2**: カメラシステム
  - GameState 拡張（7段階プレイヤーターン、6段階AIターン）
  - Cinemachine Virtual Camera システム（4カメラ）
  - カメラ自動セットアップツール

- [x] **Phase 4 Stage 3-4**: カード選択・確認UIシステム ⭐ NEW
  - CardObject インタラクション完全実装
  - ConfirmUI システム（WorldSpace Canvas）
  - 拒否アニメーション、確認ダイアログ
  - GameManager 統合完了

- [x] **Phase 6**: カメラコントロール一元管理
  - GameManager への一元化
  - イベント駆動アーキテクチャ
  - 競合リスク解消
  - focusPoint ライフサイクル管理

- [x] **Phase 4 Stage 5**: ブラフ心理戦システム
  - BluffSystem 実装
  - Intent データ構造（Confident, Nervous, Bluffing, Genuine）
  - PsychologySystem 連携
  - GameManager 統合完了

- [x] **Phase 4 Stage 6**: AI指の迷いアニメーション 🎉 NEW
  - 4システム統合（カメラ、UI、エフェクト、LLM）
  - AIHesitationController オーケストレーター
  - カードシーケンシャルフォーカス
  - LLM セリフ生成（3-tier fallback）
  - Live2D 表情連携
  - GameManager AI_TURN_HESITATE 統合完了

---

## 🔧 未完了タスク（Unity Editor作業）

### 動画背景セットアップ
コード実装は完了済み。Unity Editor上でのセットアップが必要。

- [ ] FPS_Trump_Scene を開き **Tools > Baba > Video Background Setup** → "Setup Video Background" を実行
- [ ] Play Mode で動画が3面Quadにループ再生されることを確認
- [ ] 各カメラアングル（PlayerTurn, AITurn, CardFocus, AIReaction）から背景が見えることを確認
- [ ] Inspector で brightness / tintColor を調整して雰囲気をチューニング
- [ ] StartMenuScen でも同様にセットアップ・確認

### VCam_TableOverview シーンセットアップ
コード実装は完了済み。Unity Editor上でのセットアップが必要。

- [ ] **Tools > Baba > Setup Cinemachine Cameras > Full Setup** で VCam_TableOverview を作成
- [ ] CameraCinematicsSystem の Inspector で `vcamTableOverview` フィールドにアサイン
- [ ] VCam_TableOverview の位置・回転を調整（デフォルト: `(0, 2.5, 0.6)`）
- [ ] Play Mode で Camera Monitor から5台全て表示されることを確認
- [ ] イントロ Act 3 で TableOverview に遷移することを確認
- [ ] ゲーム終了時に TableOverview が表示されることを確認

### カメラバグ修正（コード修正済み・テスト未実施）
- [ ] FocusOnPairAndReturn 競合修正の動作確認（DealInitialCards中のペア発見時）
- [ ] AIターン低intensity時のカメラ復帰確認（AIReactionView → AITurnView）
- [ ] Resolve Phase ペアフォーカス1.0s表示の確認（途中切断されないこと）

---

## 🚀 次に実装すべきタスク

### Phase 4: FPS体験完成（残りステージ）

#### ✅ **Stage 3-4: カード選択・確認UIシステム** 🎮 完成！
**実装済み** ✓

**実装されたファイル**:
- ✅ `CardObject.cs` - インタラクション完全実装
  - CardInteractionState enum
  - OnPointerDown/OnPointerUp
  - PlayInterruptAnimation()
  - SetCommitted()
  - SetSelectable()
  - ResetInteractionState()
  - GetInteractionState()

- ✅ `ConfirmUI.cs` - 確認UI完全実装
  - WorldSpace Canvas
  - Show/Hide アニメーション
  - 「引く」「やめる」ボタン
  - コールバック機能

- ✅ `GameManager.cs` - 既に統合済み
  - OnCardPointerDown()
  - HandleCardInterrupt()
  - OnConfirmDraw/OnConfirmAbort

**テスト推奨**:
- [ ] Play Mode で実際に動作確認
- [ ] カードクリック → 拒否アニメーション → 確認UI表示
- [ ] 「引く」「やめる」ボタンの動作確認
- [ ] カメラフォーカス連携確認

---

#### ✅ **Stage 5: ブラフシステム** 🎭 完成！
**実装済み** ✓

**実装内容**:
1. **Intent データ構造**
   ```csharp
   public enum IntentType { Confident, Nervous, Bluffing, Genuine }
   public class Intent
   {
       public IntentType type;
       public float confidence; // 0.0 - 1.0
       public string description;
   }
   ```

2. **BluffResult データ構造**
   ```csharp
   public class BluffResult
   {
       public bool wasBluffing;
       public bool playerFellForIt;
       public IntentType aiIntent;
       public float psychologicalImpact; // -1.0 to 1.0
   }
   ```

3. **BluffSystem 実装**
   - `DetermineAIIntent()` - AI の Intent 決定
   - `EvaluateBluff()` - ブラフ判定ロジック
   - PsychologySystem との連携
   - プレイヤー行動パターン分析統合

4. **GameManager 統合**
   - プレイヤーターン: COMMIT 時に Intent 確定
   - AIターン: COMMIT 時に Intent 確定
   - POST_REACT/REACT ステージでブラフ判定実行
   - リアクション演出再生

**TODOコメント箇所**:
- `GameManager.cs:314` - ブラフIntent確定（プレイヤーターン）
- `GameManager.cs:335` - ブラフ判定（プレイヤーターン）
- `GameManager.cs:424` - ブラフIntent決定（AIターン）
- `GameManager.cs:456` - ブラフリアクション（AIターン）

**テスト項目**:
- [ ] AI Intent が適切に決定される
- [ ] ブラフ判定ロジックが正しく動作
- [ ] PsychologySystem の圧力レベルが更新される
- [ ] リアクション演出が再生される

---

#### ✅ **Stage 6: AI指の迷いアニメーション** 🤖 完成！
**実装済み** ✓ | **実装期間**: Week 1-3 (5日間)

**実装された機能**:
1. **4システム統合オーケストレーション**
   - カメラ: CameraCinematicsSystem（カードシーケンシャルフォーカス）
   - UI: AIAttentionMarker（WorldSpace シアンリング + パルス）
   - エフェクト: CardEffectsManager（シアンオーラ + グロー）
   - セリフ: LLMManager + FallbackManager（3-tier fallback）
   - Live2D: TVHeadAnimator（Nervous ↔ Neutral 表情）

2. **LLMセリフ生成システム**
   - FallbackManager: Hesitation カテゴリー追加（9テンプレート）
   - LLMManager: GenerateHesitationDialogue() 実装
   - 3-tier fallback: LLM → Rule-based → Static DB

3. **永続テキストシステム**
   - FloatingTextSystem 拡張（ShowPersistentText, UpdatePersistentText, HidePersistentText）
   - Pressure-based カラーコーディング
   - DOTween フェードイン/アウト

**実装されたファイル**:
- ✅ `AIHesitationController.cs` - NEW（4システムオーケストレーター）
- ✅ `AIAttentionMarker.cs` - NEW（WorldSpace UIマーカー）
- ✅ `CardEffectsManager.cs` - 拡張（PlayAIConsideringAura, StopAIConsideringAura）
- ✅ `FloatingTextSystem.cs` - 拡張（永続テキスト機能）
- ✅ `CameraCinematicsSystem.cs` - 拡張（FocusOnCardSequence）
- ✅ `FallbackManager.cs` - 拡張（Hesitation category + templates）
- ✅ `LLMManager.cs` - 拡張（GenerateHesitationDialogue）
- ✅ `GameManager.cs` - 統合（AI_TURN_HESITATE: line 559-580）

**タイムライン** (2-4秒):
```
t=0.0s  AI_TURN_HESITATE
        Live2D: SetNervous(), LLM: 初期セリフ
t=0.3s  Card 1 focus (Camera + UI + Effect)
t=1.1s  Card 2 focus + セリフ更新
t=2.2s  Card 3 focus (FINAL) + 最終セリフ
t=3.3s  Cleanup (Camera restore, Hide marker, SetNeutral)
```

**テスト項目**:
- [x] カメラシーケンシャルフォーカス動作
- [x] UIマーカー表示・追従・パルス
- [x] シアンオーラ + グローエフェクト
- [x] LLMセリフ生成（fallback含む）
- [x] Live2D表情変化
- [x] GameManager統合
- [x] Edge case: 1枚カード処理
- [x] Fallback: システム欠損時の動作

**テスト項目**:
- [ ] 指の迷いアニメーションが再生される
- [ ] 思考時間とアニメーションが連動
- [ ] カメラアングルが適切

---

#### **Stage 7: リザルト診断システム** 📊
**優先度**: 中 | **推定工数**: 5-6時間

**実装内容**:
1. **DiagnosisResult データ構造**
   ```csharp
   public class DiagnosisResult
   {
       public string title;           // 診断タイトル
       public string description;     // 診断内容
       public string psychoAnalysis;  // 心理分析
       public float aiConfidence;     // AI信頼度
       public List<string> keyMoments; // ハイライトシーン
   }
   ```

2. **ResultDiagnosisSystem 実装**
   - ゲーム履歴の収集
   - プレイヤー行動パターン分析
   - LLM API 統合（診断テキスト生成）
   - 診断結果UI表示

3. **UI実装**
   - Result Canvas/Panel
   - 診断テキスト表示（タイプライター効果）
   - 統計情報（ターン数、ペア削除数など）
   - 「もう一度」「メニューに戻る」ボタン

4. **GameManager 統合**
   - `ShowResultDiagnosis()` 完成
   - RESULT 状態への遷移
   - LLM API 呼び出し
   - UI表示制御

**TODOコメント箇所**:
- `GameManager.cs:556` - Result診断生成・表示

**テスト項目**:
- [ ] ゲーム終了時に診断が生成される
- [ ] LLM API が正しく動作
- [ ] 診断UIが適切に表示される
- [ ] 統計情報が正確
- [ ] ボタンが機能する

---

## 🔮 Phase 5 以降（将来実装）

### Phase 5: LLM統合・高度なAI
**推定工数**: 8-10時間

- [ ] **LLMManager 実装**
  - OpenAI API 統合
  - プロンプトエンジニアリング
  - 非同期処理（async/await）

- [ ] **AI意思決定の強化**
  - プレイヤー癖の学習
  - 最適カード選択
  - ブラフ戦略の向上

- [ ] **動的対話システム**
  - ゲーム中のAIセリフ生成
  - 状況に応じた反応
  - 自然な会話フロー

---

### Phase 7: UI/UXポリッシュ
**推定工数**: 6-8時間

- [ ] **StartScene 完成**
  - タイトル画面
  - 難易度選択
  - 設定画面（音量、言語）

- [ ] **ゲーム内UI改善**
  - 手札カウンター
  - ターン表示（現在はDebug UIのみ）
  - プログレスバー

- [ ] **アクセシビリティ**
  - 字幕システム
  - カラーブラインド対応
  - キーボードショートカット

---

### Phase 8: 最終調整・パフォーマンス
**推定工数**: 4-5時間

- [ ] **パフォーマンス最適化**
  - オブジェクトプーリング
  - LOD設定
  - ガベージコレクション最適化

- [ ] **バグ修正**
  - エッジケース対応
  - メモリリーク修正
  - クラッシュ防止

- [ ] **ビルド・デプロイ**
  - プラットフォーム別ビルド設定
  - アセットバンドル最適化
  - リリース準備

---

## 📝 開発ノート

### 実装順序の推奨理由

1. **確認UIシステム（Stage 3-4）を優先**
   - ゲームの基本フローを完成させる
   - プレイヤー体験の核となる機能
   - 他のステージの土台となる

2. **ブラフシステム（Stage 5）**
   - ゲームの独自性を生む重要機能
   - 心理システムとの連携が必要
   - 後回しにすると統合が困難

3. **AIバイタル（Stage 6）**
   - 視覚的演出の追加
   - ゲームプレイには影響しない
   - 比較的独立した実装

4. **リザルト診断（Stage 7）**
   - ゲーム終了後の体験
   - LLM統合が必要
   - デモ・テストに有用

### テスト戦略

- 各ステージ完成後に統合テストを実施
- Play Mode で実際にプレイして動作確認
- Console エラーの確認
- パフォーマンス測定（FPS、メモリ使用量）

### ドキュメント更新

実装完了後、以下を更新:
- [ ] 該当 Phase のガイド作成
- [ ] README.md の進捗更新
- [ ] API ドキュメント（必要に応じて）

---

## 🎉 マイルストーン

### Milestone 1: プレイアブルデモ（現在）
- [x] Phase 1-3 完了
- [x] Phase 4 Stage 1-2 完了
- [x] Phase 4 Stage 3-4 完了 ⭐
- [x] Phase 6 完了
- [ ] Phase 4 Stage 5 ブラフシステム ← **NEXT**

### Milestone 2: コア機能完成
- [ ] Phase 4 全ステージ完了
- [ ] Phase 5 LLM統合完了

### Milestone 3: 製品版
- [ ] Phase 7 UI/UXポリッシュ完了
- [ ] Phase 8 最終調整完了
- [ ] リリース準備完了

---

## 🔗 関連ドキュメント

- [Phase2 実装ガイド](Phase2-Implementation-Guide.md)
- [Phase6 実装ガイド](Phase6-Camera-Centralization-Guide.md)
- [言語システムガイド](Language-System-Guide.md)
- [開発ロードマップ](06-Development-Roadmap.md)
- [GameManager 実装ガイド](09-GameManager-Implementation.md)

---

最終更新: 2026-02-07 | Phase6 完了後
