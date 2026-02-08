# EventSystemSetup - Setup Guide

## Purpose
このツールは、EventSystemをシーン間で永続化させるための `PersistentEventSystem` コンポーネントを自動的に追加します。

## Problem
StartシーンからFPS_Trump_Sceneに遷移すると、EventSystemが破棄されるため：
- Raycastが効かない
- カードが引けない
- ボタンが押せない

## Solution
StartシーンのEventSystemに `PersistentEventSystem` を追加して、シーン間で保持します。

## Usage

### Method 1: Automatic Setup (Recommended)
1. StartMenuScen を開く
2. メニューバーから **Tools > Baba > Setup Event System** を選択
3. "Setup Complete" ダイアログが表示されたら完了
4. シーンを保存（Ctrl+S / Cmd+S）

### Method 2: Manual Setup
1. StartMenuScen を開く
2. Hierarchy で EventSystem を選択
3. Inspector で "Add Component" をクリック
4. "Persistent Event System" を検索して追加
5. シーンを保存（Ctrl+S / Cmd+S）

## Validation
セットアップ後、以下を確認してください：
1. Play モードで StartMenuScen を実行
2. "Start" ボタンをクリックして FPS_Trump_Scene に遷移
3. カードが引ける、ボタンが押せることを確認

## Technical Details
- `PersistentEventSystem.cs` がEventSystemに追加される
- `DontDestroyOnLoad()` によりシーン間でEventSystemが保持される
- 複数のEventSystemが存在する場合、古いものが自動的に破棄される

## Notes
- FPS_Trump_Scene には EventSystem を追加しないでください（重複防止）
- StartMenuScen の EventSystem のみに PersistentEventSystem を追加してください
