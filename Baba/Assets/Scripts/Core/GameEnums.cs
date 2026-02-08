public enum GameState
{
    // 基本状態
    Menu,
    Setup,
    Paused,

    // Phase 4: プレイヤーターン詳細化
    PLAYER_TURN_PICK,         // ホバー中（AIカードを見ている）
    PLAYER_TURN_INTERRUPT,    // 拒否演出（カード跳ね返り）
    PLAYER_TURN_CONFIRM,      // 確認UI表示（引く/やめる）
    PLAYER_TURN_COMMIT,       // 実行直前（ブラフIntent確定）
    PLAYER_TURN_DRAW,         // 引くアニメーション
    PLAYER_TURN_POST_REACT,   // 引いた後リアクション（ブラフ判定）
    PLAYER_TURN_RESOLVE,      // ペア判定・勝敗判定

    // Phase 4: AIターン詳細化
    AI_TURN_APPROACH,         // AI思考開始
    AI_TURN_HESITATE,         // 指の迷いアニメーション
    AI_TURN_COMMIT,           // カード選択確定
    AI_TURN_DRAW,             // 引くアニメーション
    AI_TURN_REACT,            // 引いた後リアクション
    AI_TURN_RESOLVE,          // ペア判定・勝敗判定

    // Phase 4: 特殊状態
    OUTRO,                    // アウトロ演出
    RESULT,                   // 診断表示

    // 終了
    GameEnd
}

public enum GameDifficulty
{
    Easy,
    Normal,
    Hard,
    Expert
}

public enum InputMode
{
    Disabled,
    Menu,
    Gameplay
}
