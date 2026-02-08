# Stage 4 Integration Test - 使用ガイド

## 概要

このエディタ拡張は、Phase 4 Stage 4の統合テストを実行します。

カード引き拒否→確認フローが正しく動作するか、各コンポーネントの状態を確認できます。

## 使用方法

### 1. エディタウィンドウを開く

Unityエディタのメニューから：
```
Tools > Baba > Test Stage 4 Integration
```

### 2. セットアップチェック

ウィンドウが開くと、自動的にセットアップチェックが実行されます。

**必要なコンポーネント:**
- ✅ GameManager
- ✅ ConfirmUI
- ✅ CameraCinematicsSystem
- ✅ AIHandController
- ✅ PlayerHandController

**すべてのコンポーネントが見つかった場合:**
```
✅ All components found! Ready to test.
```

**コンポーネントが不足している場合:**
```
⚠️ Some components are missing. Please run auto-setup tools first.
```

不足しているコンポーネントのリストが表示されるので、以下のツールを実行してください：
- `Tools > Baba > Setup Cinemachine Cameras` (CameraCinematicsSystem)
- `Tools > Baba > Setup ConfirmUI System` (ConfirmUI)

### 3. テストの実行

**重要**: すべてのテストは**プレイモード**で実行する必要があります。

#### Individual Tests（個別テスト）

**Test 1: CardObject Interrupt Animation**
- CardObjectの拒否アニメーションが正しく動作するか確認
- 跳ね返り + 揺れアニメーションの実行
- CardInteractionStateの遷移確認

**Test 2: ConfirmUI Show/Hide**
- ConfirmUIが正しく表示されるか確認
- カード上に配置されるか
- 「引く」「やめる」ボタンのコールバック確認

**Test 3: GameManager State Transition**
- GameManagerの状態遷移が正しく動作するか確認
- PLAYER_TURN_PICK → PLAYER_TURN_INTERRUPT への遷移
- OnCardPointerDown()の動作確認

**Test 4: Camera Focus on Card**
- CameraCinematicsSystemが選択されたカードにフォーカスするか確認
- VCam_CardFocusのアクティブ化
- カメラブレンドの確認

#### Full Integration Test（統合テスト）

Stage 4の完全なフローをシミュレートします：

1. プレイヤーがAIカードをクリック（OnPointerDown）
2. カードが拒否アニメーション再生（跳ね返り + 揺れ）
3. GameManagerがPLAYER_TURN_INTERRUPT状態に遷移
4. GameManagerがPLAYER_TURN_CONFIRM状態に遷移
5. カメラが選択されたカードにフォーカス
6. ConfirmUIが「引く」「やめる」ボタンとともに表示
7a. 「引く」クリック時: カードがCommitted状態に、PLAYER_TURN_COMMITへ遷移
7b. 「やめる」クリック時: PLAYER_TURN_PICK状態に戻る、別カード選択可能

### 4. テスト結果の確認

テスト結果は以下の形式で表示されます：

- ✅ 緑色: テスト成功
- ❌ 赤色: テスト失敗
- ⚠️ 黄色: 手動確認が必要

**手動確認が必要な項目:**
- アニメーションの滑らかさ
- カメラブレンドの視覚的な確認
- ConfirmUIの位置と見た目
- ボタンのクリック可能性

### 5. Refresh Setup Check

コンポーネントを追加した後、「Refresh Setup Check」ボタンをクリックすると、セットアップチェックが再実行されます。

## テストの前提条件

### プレイモードで実行

すべてのテストはプレイモードで実行する必要があります。

Edit Modeでテストを実行しようとすると、以下の警告が表示されます：
```
⚠️ Tests must be run in Play Mode.
```

### ゲーム状態

**Full Integration Testを実行する前に:**
- ゲームを開始してPLAYER_TURN_PICK状態にする必要があります
- AIHandに少なくとも1枚のカードが必要です

PLAYER_TURN_PICK状態でない場合、以下のメッセージが表示されます：
```
❌ Game is not in PLAYER_TURN_PICK state (current: XXX)
   Please start a new game to enter PLAYER_TURN_PICK state
```

## トラブルシューティング

### テストが実行できない

**症状**: テストボタンがグレーアウトしている

**原因と対処法:**
1. **Edit Modeで実行しようとしている**
   - プレイモードに切り替えてください

2. **コンポーネントが不足している**
   - セットアップチェックを確認し、必要なツールを実行してください

3. **テストが実行中**
   - 前のテストが完了するまで待ってください

### テスト結果に「❌」が表示される

**Test 1: CardObject Interrupt Animation**
- CardObjectがInterrupting状態に遷移しない
  - CardObjectのinterruptDuration設定を確認
  - DOTweenがインストールされているか確認

**Test 2: ConfirmUI Show/Hide**
- ConfirmUIが表示されない
  - ConfirmUIのCanvas Render Modeが「World Space」になっているか確認
  - offsetFromCard設定を確認

**Test 3: GameManager State Transition**
- 状態遷移が発生しない
  - GameManagerのPlayerTurnSequence()コルーチンが実行されているか確認
  - currentStateフィールドが正しく更新されているか確認

**Test 4: Camera Focus on Card**
- カメラがフォーカスしない
  - CameraCinematicsSystemのVCam_CardFocusが存在するか確認
  - VCam_CardFocusのPriorityが15に設定されているか確認

### AIHandにカードがない

**症状**:
```
❌ No cards in AI hand
```

**対処法:**
- ゲームを再起動してカード配布を行ってください
- GameManagerのStartNewGame()が正しく呼ばれているか確認

## テスト結果の読み方

### Test 1: CardObject Interrupt Animation

**成功例:**
```
--- Test 1: CardObject Interrupt Animation ---
✅ Found test card: King Hearts
   Initial state: Idle
✅ PlayInterruptAnimation() called
   State after animation start: Interrupting
✅ Animation started correctly (state = Interrupting)
   Note: Animation will complete after 0.5s
```

**期待される動作:**
- CardInteractionStateがIdleからInterruptingに遷移
- 0.5秒後にAwaitingConfirm状態に遷移（自動）

### Test 2: ConfirmUI Show/Hide

**成功例:**
```
--- Test 2: ConfirmUI Show/Hide ---
✅ Using test card: Ace Spades
✅ ConfirmUI.Show() called
   Position: (0.15, 1.35, 0.5)
   Active: True
⚠️ Manual check: Is ConfirmUI visible in the scene?
   You can manually click 'Draw' or 'Cancel' buttons to test callbacks
```

**手動確認:**
- Scene ViewまたはGame ViewでConfirmUIが表示されているか
- カードの上（Y軸 +0.15）に配置されているか
- 「引く」「やめる」ボタンが表示されているか
- ボタンをクリックしてコールバックが発火するか

### Test 3: GameManager State Transition

**成功例:**
```
--- Test 3: GameManager State Transition ---
✅ Current GameState: PLAYER_TURN_PICK
✅ Calling GameManager.OnCardPointerDown()
   State after OnCardPointerDown: PLAYER_TURN_INTERRUPT
✅ State transition to PLAYER_TURN_INTERRUPT successful
```

**期待される動作:**
- PLAYER_TURN_PICK → PLAYER_TURN_INTERRUPT への即座の遷移
- 0.5秒後にPLAYER_TURN_CONFIRMへ遷移（自動）

### Test 4: Camera Focus on Card

**成功例:**
```
--- Test 4: Camera Focus on Card ---
✅ Using test card: Queen Diamonds
   Card position: (0.2, 1.0, 0.5)
✅ CameraCinematicsSystem.FocusOnCard() called
⚠️ Manual check: Did the camera focus on the card?
   Expected: VCam_CardFocus should be active with Priority=15
```

**手動確認:**
- Game Viewでカメラがカードにズームインしているか
- カメラブレンドが滑らか（1.0秒）か
- VCam_CardFocusのPriorityが15になっているか（Hierarchy > VCam_CardFocus > Inspectorで確認）

### Full Integration Test

**成功例:**
```
=== FULL INTEGRATION TEST ===

This test simulates the complete Stage 4 flow:
1. Player clicks AI card (OnPointerDown)
2. Card plays interrupt animation (bounce + shake)
3. GameManager transitions to PLAYER_TURN_INTERRUPT
4. GameManager transitions to PLAYER_TURN_CONFIRM
5. Camera focuses on selected card
6. ConfirmUI appears with 'Draw'/'Cancel' buttons
7a. If 'Draw' clicked: Card is committed, transition to PLAYER_TURN_COMMIT
7b. If 'Cancel' clicked: Return to PLAYER_TURN_PICK, can select another card

✅ Test card selected: Jack Clubs

Step 1: Simulating OnPointerDown...
✅ OnCardPointerDown() called

⏳ Waiting for state transitions...
   Expected sequence: PICK → INTERRUPT (0.5s) → CONFIRM

⚠️ MANUAL VERIFICATION REQUIRED:
1. Watch the card - it should bounce back and shake
2. Camera should zoom in on the card
3. ConfirmUI should appear with 'Draw' and 'Cancel' buttons
4. Click 'Draw' to test OnConfirmDraw flow
5. OR click 'Cancel' to test OnConfirmAbort flow (returns to PICK)

✅ Full integration test initiated
   Check Unity Scene view and Game view for visual confirmation
```

**手動確認手順:**

1. **カードアニメーション確認:**
   - カードが手前に跳ね返る（0.3秒）
   - 元の位置に戻る（0.4秒）
   - 左右に2回揺れる（0.15秒 × 4回）

2. **カメラ確認:**
   - カメラが選択されたカードにズームイン
   - ブレンドが滑らか

3. **ConfirmUI確認:**
   - カードの上に表示される
   - 「引く」（緑）「やめる」（赤）ボタンが見える
   - カメラの方を向いている

4. **「引く」ボタンテスト:**
   - クリック → ConfirmUIがフェードアウト
   - カードがCommitted状態に変更
   - PLAYER_TURN_COMMIT状態に遷移
   - カードを引くアニメーション開始

5. **「やめる」ボタンテスト:**
   - クリック → ConfirmUIがフェードアウト
   - PLAYER_TURN_PICK状態に戻る
   - 別のAIカードを選択可能

## 次のステップ

Stage 4のテストが全て成功したら、次はStage 5（ブラフシステム）の実装に進みます。

---

**作成日**: 2026-02-07
**Phase 4 - Stage 4**: Integration Test Tool
