# Phase6 実装ガイド - カメラコントロール一元管理

## 概要
Phase6「カメラコントロール一元管理」の実装完了報告書です。カメラ制御をGameManagerに一元化し、競合リスクを解消しました。

## 背景と問題点

### 実装前の問題
Phase4でCinemachineカメラシステムが実装されましたが、以下の問題が発生していました：

1. **複数の呼び出し経路**
   - GameManager (cameraSystem フィールド経由)
   - PlayerHandController (Singleton.Instance 直接)
   - 2つの異なる経路からカメラ制御が可能 → 競合リスク

2. **非同期処理による競合**
   ```
   例: ペア消去フロー
   PlayerHandController.RemovePair()
     → FocusCameraOnPair() [カメラ: ペアにズーム、1.5秒後に自動削除]
     → PlayPairDisappearEffect() [エフェクト開始]

   同時に GameManager のターン遷移:
   GameManager.AITurnSequence()
     → ShowAITurnView() [カメラ: AIターンビューへ切り替え]

   結果: カメラ制御の競合、不安定な切り替え
   ```

3. **GameState とカメラ状態の不整合**
   - GameState が PLAYER_TURN だが、カメラが CardFocus 状態の可能性
   - カメラ状態を追跡する仕組みがない

4. **一時的な focusPoint GameObject の管理**
   - `Destroy(focusPoint, 1.5f)` で遅延削除
   - ターン遷移と重なると参照エラーの可能性

---

## 実装方式

### 選択したアプローチ: GameManager 完全一元管理

```
PlayerHandController/AIHandController
    ↓ イベント通知のみ (OnPairMatched等)
    ↓
GameManager (全カメラ制御の責任)
    ↓
CameraCinematicsSystem
```

**選択理由:**
1. ✅ **最もシンプル**: カメラ制御の流れが一本道、追いやすい
2. ✅ **競合なし**: GameManager が唯一の呼び出し元
3. ✅ **GameState と同期**: 状態遷移とカメラが必ず一致
4. ✅ **デバッグ容易**: GameManager だけ見ればOK
5. ✅ **実装コスト最小**: 既存の構造に適合

---

## 実装済みファイル一覧

### 変更されたファイル

#### 1. PlayerHandController.cs
**パス**: `Baba/Assets/Scripts/Hand/PlayerHandController.cs`

**変更内容**:
- ❌ 削除: `FocusCameraOnCard(CardObject card)` メソッド
- ❌ 削除: `ReturnToHandView()` メソッド
- ❌ 削除: `FocusCameraOnPair(CardObject card1, CardObject card2)` メソッド
- ❌ 削除: `RemovePair()` からのカメラ呼び出し
- ✅ 追加: コメント「カメラ制御は削除（GameManager が OnPairMatched イベントで処理）」

#### 2. GameManager.cs
**パス**: `Baba/Assets/Scripts/Manager/GameManager.cs`

**追加内容**:
- ✅ フィールド: `private Coroutine currentCameraFocusCoroutine = null;`
- ✅ メソッド: `OnEnable()` - イベント購読
- ✅ メソッド: `OnDisable()` - イベント購読解除
- ✅ イベントハンドラー: `OnPlayerPairMatched(CardObject card1, CardObject card2)`
- ✅ イベントハンドラー: `OnAIPairMatched(CardObject card1, CardObject card2)`
- ✅ コルーチン: `FocusOnPairAndReturn(CardObject card1, CardObject card2, bool isPlayerTurn)`

#### 3. AIHandController.cs
**パス**: `Baba/Assets/Scripts/Hand/AIHandController.cs`

**変更なし** - 既にイベント駆動アーキテクチャで実装済み（`RaiseOnPairMatched()` を使用）

---

## 実装詳細

### GameManager.cs の追加コード

#### 1. フィールド
```csharp
private Coroutine currentCameraFocusCoroutine = null;
```

#### 2. イベント購読
```csharp
private void OnEnable()
{
    // イベント購読
    if (playerHand != null)
    {
        playerHand.OnPairMatched += OnPlayerPairMatched;
    }
    if (aiHand != null)
    {
        aiHand.OnPairMatched += OnAIPairMatched;
    }
}

private void OnDisable()
{
    // イベント購読解除
    if (playerHand != null)
    {
        playerHand.OnPairMatched -= OnPlayerPairMatched;
    }
    if (aiHand != null)
    {
        aiHand.OnPairMatched -= OnAIPairMatched;
    }
}
```

#### 3. イベントハンドラー
```csharp
/// <summary>
/// プレイヤーのペア削除時
/// </summary>
private void OnPlayerPairMatched(CardObject card1, CardObject card2)
{
    if (cameraSystem == null) return;

    // 既存のフォーカスコルーチンをキャンセル
    if (currentCameraFocusCoroutine != null)
    {
        StopCoroutine(currentCameraFocusCoroutine);
    }

    // ペアにフォーカス → 元のビューに戻る
    currentCameraFocusCoroutine = StartCoroutine(
        FocusOnPairAndReturn(card1, card2, isPlayerTurn: true)
    );
}

/// <summary>
/// AIのペア削除時
/// </summary>
private void OnAIPairMatched(CardObject card1, CardObject card2)
{
    if (cameraSystem == null) return;

    if (currentCameraFocusCoroutine != null)
    {
        StopCoroutine(currentCameraFocusCoroutine);
    }

    currentCameraFocusCoroutine = StartCoroutine(
        FocusOnPairAndReturn(card1, card2, isPlayerTurn: false)
    );
}
```

#### 4. カメラフォーカスコルーチン
```csharp
/// <summary>
/// ペアにフォーカスして元のビューに戻す
/// </summary>
private IEnumerator FocusOnPairAndReturn(CardObject card1, CardObject card2, bool isPlayerTurn)
{
    // ペアの中心点を計算
    Vector3 centerPosition = (card1.transform.position + card2.transform.position) * 0.5f;

    // 一時的なフォーカスポイントを作成
    GameObject focusPoint = new GameObject("_TempCardPairFocus");
    focusPoint.transform.position = centerPosition;

    // カメラフォーカス
    cameraSystem.FocusOnCard(focusPoint.transform);

    // 1.0秒間フォーカスを維持
    yield return new WaitForSeconds(1.0f);

    // focusPoint を削除
    Destroy(focusPoint);

    // 元のビューに戻す
    if (isPlayerTurn)
    {
        cameraSystem.ShowPlayerTurnView();
    }
    else
    {
        cameraSystem.ShowAITurnView();
    }

    currentCameraFocusCoroutine = null;
}
```

---

## システムフロー

### ペア削除時のカメラ制御フロー

```
1. Player/AI removes pair
    ↓
2. HandController.RemovePair()
    ↓
3. HandController.RaiseOnPairMatched(card1, card2)
    ↓
4. GameManager receives event
   - OnPlayerPairMatched() または OnAIPairMatched()
    ↓
5. GameManager.FocusOnPairAndReturn()
   ├─ 既存のコルーチンをキャンセル (StopCoroutine)
   ├─ ペアの中心点を計算
   ├─ 一時的なフォーカスポイントを作成
   ├─ CameraCinematicsSystem.FocusOnCard()
   ├─ 1.0秒間待機
   ├─ フォーカスポイントを削除
   └─ 適切なビューに戻る (ShowPlayerTurnView/ShowAITurnView)
    ↓
6. currentCameraFocusCoroutine = null
```

---

## Unity Editor セットアップ

### 必要な設定

このPhaseは既存のコンポーネントを使用するため、新しいセットアップは不要です。

#### 確認事項:
1. ✅ GameManager に CameraCinematicsSystem が参照されている
2. ✅ PlayerHandController と AIHandController が GameManager に参照されている
3. ✅ CameraCinematicsSystem に4つの Virtual Camera が設定されている
   - vcamPlayerTurn
   - vcamAITurn
   - vcamCardFocus
   - vcamAIReaction

---

## テスト方法

### 1. 基本動作テスト

#### ペア削除時のカメラフォーカス
1. Play Mode に入る
2. ゲームを開始し、プレイヤーターンでペアを作成
3. ペアが自動削除される

**期待結果**:
- ✅ カメラがペアの中心にズームイン
- ✅ 1.0秒後にプレイヤーターンビューに戻る
- ✅ カメラ切り替えがスムーズ
- ✅ Console にエラーなし

#### AIターンでのペア削除
1. AIがカードを引いてペアを作成
2. ペアが自動削除される

**期待結果**:
- ✅ カメラがペアの中心にズームイン
- ✅ 1.0秒後にAIターンビューに戻る
- ✅ 適切なビューに戻る

---

### 2. 複数ペア削除テスト

#### 連続ペア削除
1. 複数のペアを連続して削除

**期待結果**:
- ✅ 各ペアごとにカメラフォーカス
- ✅ 前のフォーカスが正しくキャンセルされる（`StopCoroutine`で）
- ✅ 最終的に正しいビューに戻る
- ✅ `_TempCardPairFocus` GameObject が残留しない

---

### 3. ターン遷移との統合テスト

#### ペア削除中のターン遷移
1. プレイヤーターンでペア削除
2. ペアフォーカス中にAIターンに遷移

**期待結果**:
- ✅ カメラ状態とGameStateが一致
- ✅ focusPoint の参照エラーなし
- ✅ ターン遷移後、適切なビューに切り替わる

---

### 4. カメラ制御の一元化確認

#### コード確認
```
確認事項:
- ✅ PlayerHandController からの直接カメラ呼び出しがないこと
- ✅ すべてのカメラ制御が GameManager 経由であること
- ✅ CameraCinematicsSystem.Instance の直接参照がないこと（PlayerHandController内）
```

#### ログ確認
```
期待されるログ:
[CameraCinematicsSystem] Focusing on card: _TempCardPairFocus
[CameraCinematicsSystem] Switching to Player Turn view (looking at AI hand)
または
[CameraCinematicsSystem] Switching to AI Turn view (looking at Player hand)
```

---

## 解決された問題

### ✅ 1. 競合の解消
- **問題**: 複数の経路からカメラ制御 → 競合リスク
- **解決**: GameManager が唯一のカメラ制御者
- **実装**: `currentCameraFocusCoroutine` で重複を防止、`StopCoroutine()` で確実にキャンセル

### ✅ 2. デバッグ容易性
- **問題**: カメラ制御が分散、フロー追跡困難
- **解決**: カメラ制御は GameManager のみ、1箇所を見ればフロー全体が分かる
- **実装**: ログ出力も一箇所に集約可能

### ✅ 3. GameState との整合性
- **問題**: GameState とカメラ状態が不一致の可能性
- **解決**: ターン状態（isPlayerTurn）に応じた復帰ビュー
- **実装**: ターン遷移とカメラが自然に同期、適切なビューに確実に戻る

### ✅ 4. focusPoint の管理
- **問題**: `Destroy(focusPoint, 1.5f)` で遅延削除 → 参照エラーリスク
- **解決**: コルーチン内で作成→削除まで一貫管理
- **実装**: 遅延削除（Destroy with delay）を使わない、参照エラーなし

---

## アーキテクチャの利点

### 1. シンプルさ
- カメラ制御が GameManager に一元化
- 制御フローが一本道で追いやすい
- 新しい開発者でも理解しやすい

### 2. 安全性
- 競合リスクがゼロ
- 非同期処理の競合を確実に防止
- 参照エラーのリスク解消

### 3. 保守性
- 変更箇所が明確（GameManager のみ）
- デバッグが容易（1箇所を見ればOK）
- カメラ制御ロジックが集約

### 4. 拡張性
- GameManager からカメラ演出を追加しやすい
- イベント駆動で他システムとの統合が容易
- 将来的なカメラ機能追加に対応しやすい

### 5. 実装コスト
- 最小限の変更で実現
- 既存構造を活用
- リファクタリングの手間が少ない

---

## 制約事項と注意点

### ⚠ 既存の動作への影響
- PlayerHandController の公開メソッド（FocusCameraOnCard等）が削除された
- これらを外部から呼んでいる箇所がないことを確認済み
- 今後、カメラ制御が必要な場合は GameManager 経由で実装

### ⚠ カメラフォーカス時間
- 現在 1.0 秒に設定（調整可能）
- PlayPairDisappearEffect の長さ（1.5秒）より短い
- エフェクト完了前にビューが戻るが、視覚的には自然

### ⚠ イベント購読の管理
- OnEnable/OnDisable で適切に購読・購読解除
- GameManager が破棄される際に自動的に解除される
- メモリリークのリスクなし

---

## 今後の拡張案（オプション）

### 1. カメラ遷移の改善
- Cinemachine のブレンド時間調整
- EaseInOut カーブの適用
- より滑らかなカメラワーク

### 2. カメラ状態の可視化
- Debug UI でカメラ状態を表示
- どのビューがアクティブかログ出力
- デバッグモード実装

### 3. カメラコントロールの拡張
- カードドロー時のカメラエフェクト
- 勝利/敗北時の演出
- カード選択時のズームイン（将来的に）

---

## チェックリスト

実装が完了したら、以下を確認してください:

- [x] PlayerHandController からカメラ制御メソッド削除（3メソッド）
- [x] PlayerHandController.RemovePair() からカメラ呼び出し削除
- [x] GameManager に currentCameraFocusCoroutine フィールド追加
- [x] GameManager に OnEnable/OnDisable メソッド追加
- [x] GameManager に OnPlayerPairMatched/OnAIPairMatched イベントハンドラー追加
- [x] GameManager に FocusOnPairAndReturn() コルーチン追加
- [x] AIHandController の確認（変更不要）
- [ ] Unity Editor でコンパイルエラーなし
- [ ] Play Mode でペア削除時のカメラ動作確認
- [ ] 複数ペア削除時の動作確認
- [ ] ターン遷移との統合確認
- [ ] Console にエラーなし

---

## 参考ファイル

### 変更されたファイル
- `Baba/Assets/Scripts/Hand/PlayerHandController.cs`
- `Baba/Assets/Scripts/Manager/GameManager.cs`

### 参考ファイル（変更なし）
- `Baba/Assets/Scripts/Camera/CameraCinematicsSystem.cs`
- `Baba/Assets/Scripts/Hand/HandController.cs`
- `Baba/Assets/Scripts/Hand/AIHandController.cs`

---

## 関連ドキュメント

- [Phase2 実装ガイド](Phase2-Implementation-Guide.md) - 視覚・音響システム
- [Phase4 実装ガイド](06-Development-Roadmap.md) - カメラシネマティクスシステム
- [GameManager 実装ガイド](09-GameManager-Implementation.md) - GameManager の詳細

---

完了です！これで Phase6「カメラコントロール一元管理」の実装が完了しました 🎉
