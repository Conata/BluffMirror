using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase 4 - Stage 4: 統合テストエディタ拡張
/// カード引き拒否→確認フローの動作確認
/// Tools > Baba > Test Stage 4 Integration から実行
/// </summary>
public class Stage4IntegrationTest : EditorWindow
{
    private GameManager gameManager;
    private ConfirmUI confirmUI;
    private CameraCinematicsSystem cameraSystem;
    private AIHandController aiHand;
    private PlayerHandController playerHand;

    private Vector2 scrollPos;
    private bool isTestRunning = false;

    // テスト結果
    private bool setupCheckPassed = false;
    private List<string> setupErrors = new List<string>();
    private List<string> testResults = new List<string>();

    [MenuItem("Tools/Baba/Test Stage 4 Integration")]
    public static void ShowWindow()
    {
        var window = GetWindow<Stage4IntegrationTest>("Stage 4 Test");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnEnable()
    {
        FindComponents();
        RunSetupCheck();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Phase 4 - Stage 4 Integration Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "このツールはStage 4の統合テストを実行します。\n" +
            "カード引き拒否→確認フローが正しく動作するか確認できます。",
            MessageType.Info);

        EditorGUILayout.Space();

        // セットアップチェック結果
        DrawSetupCheck();

        EditorGUILayout.Space();

        // コンポーネント参照
        DrawComponentReferences();

        EditorGUILayout.Space();

        // テストボタン
        DrawTestButtons();

        EditorGUILayout.Space();

        // テスト結果
        DrawTestResults();
    }

    private void DrawSetupCheck()
    {
        EditorGUILayout.LabelField("Setup Check", EditorStyles.boldLabel);

        if (setupCheckPassed)
        {
            EditorGUILayout.HelpBox("✅ All components found! Ready to test.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠️ Some components are missing. Please run auto-setup tools first.", MessageType.Warning);

            if (setupErrors.Count > 0)
            {
                EditorGUILayout.LabelField("Missing Components:", EditorStyles.boldLabel);
                foreach (var error in setupErrors)
                {
                    EditorGUILayout.LabelField($"• {error}", EditorStyles.helpBox);
                }
            }
        }

        if (GUILayout.Button("Refresh Setup Check"))
        {
            FindComponents();
            RunSetupCheck();
        }
    }

    private void DrawComponentReferences()
    {
        EditorGUILayout.LabelField("Component References", EditorStyles.boldLabel);

        GUI.enabled = false;
        EditorGUILayout.ObjectField("GameManager", gameManager, typeof(GameManager), true);
        EditorGUILayout.ObjectField("ConfirmUI", confirmUI, typeof(ConfirmUI), true);
        EditorGUILayout.ObjectField("CameraCinematicsSystem", cameraSystem, typeof(CameraCinematicsSystem), true);
        EditorGUILayout.ObjectField("AIHandController", aiHand, typeof(AIHandController), true);
        EditorGUILayout.ObjectField("PlayerHandController", playerHand, typeof(PlayerHandController), true);
        GUI.enabled = true;
    }

    private void DrawTestButtons()
    {
        EditorGUILayout.LabelField("Tests", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ Tests must be run in Play Mode.", MessageType.Warning);
            GUI.enabled = false;
        }
        else if (isTestRunning)
        {
            EditorGUILayout.HelpBox("⏳ Test is running...", MessageType.Info);
            GUI.enabled = false;
        }
        else if (!setupCheckPassed)
        {
            EditorGUILayout.HelpBox("⚠️ Cannot run tests. Missing components.", MessageType.Warning);
            GUI.enabled = false;
        }

        // Individual Tests
        EditorGUILayout.LabelField("Individual Tests:", EditorStyles.boldLabel);

        if (GUILayout.Button("Test 1: CardObject Interrupt Animation", GUILayout.Height(30)))
        {
            RunTest(() => Test1_CardObjectInterruptAnimation());
        }

        if (GUILayout.Button("Test 2: ConfirmUI Show/Hide", GUILayout.Height(30)))
        {
            RunTest(() => Test2_ConfirmUIShowHide());
        }

        if (GUILayout.Button("Test 3: GameManager State Transition", GUILayout.Height(30)))
        {
            RunTest(() => Test3_GameManagerStateTransition());
        }

        if (GUILayout.Button("Test 4: Camera Focus on Card", GUILayout.Height(30)))
        {
            RunTest(() => Test4_CameraFocusOnCard());
        }

        EditorGUILayout.Space();

        // Full Integration Test
        EditorGUILayout.LabelField("Integration Test:", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Full Integration Test", GUILayout.Height(40)))
        {
            RunTest(() => TestFullIntegration());
        }

        GUI.enabled = true;
    }

    private void DrawTestResults()
    {
        EditorGUILayout.LabelField("Test Results", EditorStyles.boldLabel);

        if (testResults.Count == 0)
        {
            EditorGUILayout.LabelField("No tests run yet.", EditorStyles.helpBox);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

        foreach (var result in testResults)
        {
            if (result.StartsWith("✅"))
            {
                GUI.color = Color.green;
            }
            else if (result.StartsWith("❌"))
            {
                GUI.color = Color.red;
            }
            else if (result.StartsWith("⚠️"))
            {
                GUI.color = Color.yellow;
            }
            else
            {
                GUI.color = Color.white;
            }

            EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Clear Results"))
        {
            testResults.Clear();
        }
    }

    private void FindComponents()
    {
        gameManager = FindObjectOfType<GameManager>();
        confirmUI = FindObjectOfType<ConfirmUI>();
        cameraSystem = FindObjectOfType<CameraCinematicsSystem>();
        aiHand = FindObjectOfType<AIHandController>();
        playerHand = FindObjectOfType<PlayerHandController>();
    }

    private void RunSetupCheck()
    {
        setupErrors.Clear();
        setupCheckPassed = true;

        if (gameManager == null)
        {
            setupErrors.Add("GameManager not found");
            setupCheckPassed = false;
        }

        if (confirmUI == null)
        {
            setupErrors.Add("ConfirmUI not found (Run: Tools > Baba > Setup ConfirmUI System)");
            setupCheckPassed = false;
        }

        if (cameraSystem == null)
        {
            setupErrors.Add("CameraCinematicsSystem not found (Run: Tools > Baba > Setup Cinemachine Cameras)");
            setupCheckPassed = false;
        }

        if (aiHand == null)
        {
            setupErrors.Add("AIHandController not found");
            setupCheckPassed = false;
        }

        if (playerHand == null)
        {
            setupErrors.Add("PlayerHandController not found");
            setupCheckPassed = false;
        }

        Debug.Log($"[Stage4IntegrationTest] Setup check: {(setupCheckPassed ? "PASSED" : "FAILED")}");
    }

    private void RunTest(System.Action testAction)
    {
        isTestRunning = true;
        testResults.Clear();
        testResults.Add($"=== Test Started at {System.DateTime.Now:HH:mm:ss} ===");

        try
        {
            testAction?.Invoke();
        }
        catch (System.Exception e)
        {
            testResults.Add($"❌ Test failed with exception: {e.Message}");
            Debug.LogError($"[Stage4IntegrationTest] Test exception: {e}");
        }

        testResults.Add($"=== Test Completed at {System.DateTime.Now:HH:mm:ss} ===");
        isTestRunning = false;
        Repaint();
    }

    // ===================
    // Individual Tests
    // ===================

    private void Test1_CardObjectInterruptAnimation()
    {
        testResults.Add("--- Test 1: CardObject Interrupt Animation ---");

        // AIHandからカードを取得
        if (aiHand == null || aiHand.GetCards().Count == 0)
        {
            testResults.Add("❌ No cards in AI hand");
            return;
        }

        CardObject testCard = aiHand.GetCards()[0];
        if (testCard == null)
        {
            testResults.Add("❌ Card is null");
            return;
        }

        testResults.Add($"✅ Found test card: {testCard.cardData?.rank} {testCard.cardData?.suit}");

        // インタラクション状態を確認
        CardInteractionState initialState = testCard.GetInteractionState();
        testResults.Add($"   Initial state: {initialState}");

        // PointerDown状態に変更
        testCard.GetType().GetField("interactionState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testCard, CardInteractionState.PointerDown);

        // アニメーション実行
        testCard.PlayInterruptAnimation();
        testResults.Add($"✅ PlayInterruptAnimation() called");

        // 状態確認
        CardInteractionState afterAnimState = testCard.GetInteractionState();
        testResults.Add($"   State after animation start: {afterAnimState}");

        if (afterAnimState == CardInteractionState.Interrupting)
        {
            testResults.Add($"✅ Animation started correctly (state = Interrupting)");
        }
        else
        {
            testResults.Add($"⚠️ Expected state: Interrupting, Got: {afterAnimState}");
        }

        testResults.Add($"   Note: Animation will complete after {testCard.GetType().GetField("interruptDuration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(testCard)}s");
    }

    private void Test2_ConfirmUIShowHide()
    {
        testResults.Add("--- Test 2: ConfirmUI Show/Hide ---");

        if (confirmUI == null)
        {
            testResults.Add("❌ ConfirmUI is null");
            return;
        }

        // AIHandからカードを取得
        if (aiHand == null || aiHand.GetCards().Count == 0)
        {
            testResults.Add("❌ No cards in AI hand");
            return;
        }

        CardObject testCard = aiHand.GetCards()[0];
        if (testCard == null)
        {
            testResults.Add("❌ Card is null");
            return;
        }

        testResults.Add($"✅ Using test card: {testCard.cardData?.rank} {testCard.cardData?.suit}");

        // Show test
        confirmUI.Show(testCard, (card) =>
        {
            testResults.Add($"   Draw callback triggered for card: {card.cardData?.rank} {card.cardData?.suit}");
        }, () =>
        {
            testResults.Add($"   Cancel callback triggered");
        });

        testResults.Add($"✅ ConfirmUI.Show() called");
        testResults.Add($"   Position: {confirmUI.transform.position}");
        testResults.Add($"   Active: {confirmUI.gameObject.activeSelf}");

        testResults.Add($"⚠️ Manual check: Is ConfirmUI visible in the scene?");
        testResults.Add($"   You can manually click 'Draw' or 'Cancel' buttons to test callbacks");

        // Note: Hide will be called automatically when buttons are clicked
    }

    private void Test3_GameManagerStateTransition()
    {
        testResults.Add("--- Test 3: GameManager State Transition ---");

        if (gameManager == null)
        {
            testResults.Add("❌ GameManager is null");
            return;
        }

        // 現在の状態を取得
        var currentStateField = typeof(GameManager).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentStateField == null)
        {
            testResults.Add("❌ Cannot access currentState field");
            return;
        }

        GameState currentState = (GameState)currentStateField.GetValue(gameManager);
        testResults.Add($"✅ Current GameState: {currentState}");

        // AIHandからカードを取得
        if (aiHand == null || aiHand.GetCards().Count == 0)
        {
            testResults.Add("❌ No cards in AI hand");
            return;
        }

        CardObject testCard = aiHand.GetCards()[0];

        // OnCardPointerDownをシミュレート（PLAYER_TURN_PICKの場合のみ）
        if (currentState == GameState.PLAYER_TURN_PICK)
        {
            testResults.Add($"✅ Calling GameManager.OnCardPointerDown()");
            gameManager.OnCardPointerDown(testCard);

            // 状態が変わったか確認（少し待つ）
            EditorApplication.delayCall += () =>
            {
                GameState newState = (GameState)currentStateField.GetValue(gameManager);
                testResults.Add($"   State after OnCardPointerDown: {newState}");

                if (newState == GameState.PLAYER_TURN_INTERRUPT)
                {
                    testResults.Add($"✅ State transition to PLAYER_TURN_INTERRUPT successful");
                }
                else
                {
                    testResults.Add($"⚠️ Expected PLAYER_TURN_INTERRUPT, got {newState}");
                }

                Repaint();
            };
        }
        else
        {
            testResults.Add($"⚠️ Cannot test OnCardPointerDown: Current state is not PLAYER_TURN_PICK");
            testResults.Add($"   Start a new game to enter PLAYER_TURN_PICK state");
        }
    }

    private void Test4_CameraFocusOnCard()
    {
        testResults.Add("--- Test 4: Camera Focus on Card ---");

        if (cameraSystem == null)
        {
            testResults.Add("❌ CameraCinematicsSystem is null");
            return;
        }

        // AIHandからカードを取得
        if (aiHand == null || aiHand.GetCards().Count == 0)
        {
            testResults.Add("❌ No cards in AI hand");
            return;
        }

        CardObject testCard = aiHand.GetCards()[0];
        if (testCard == null)
        {
            testResults.Add("❌ Card is null");
            return;
        }

        testResults.Add($"✅ Using test card: {testCard.cardData?.rank} {testCard.cardData?.suit}");
        testResults.Add($"   Card position: {testCard.transform.position}");

        // FocusOnCard呼び出し
        cameraSystem.FocusOnCard(testCard.transform);
        testResults.Add($"✅ CameraCinematicsSystem.FocusOnCard() called");

        testResults.Add($"⚠️ Manual check: Did the camera focus on the card?");
        testResults.Add($"   Expected: VCam_CardFocus should be active with Priority=15");
    }

    // ===================
    // Full Integration Test
    // ===================

    private void TestFullIntegration()
    {
        testResults.Add("=== FULL INTEGRATION TEST ===");
        testResults.Add("");

        testResults.Add("This test simulates the complete Stage 4 flow:");
        testResults.Add("1. Player clicks AI card (OnPointerDown)");
        testResults.Add("2. Card plays interrupt animation (bounce + shake)");
        testResults.Add("3. GameManager transitions to PLAYER_TURN_INTERRUPT");
        testResults.Add("4. GameManager transitions to PLAYER_TURN_CONFIRM");
        testResults.Add("5. Camera focuses on selected card");
        testResults.Add("6. ConfirmUI appears with 'Draw'/'Cancel' buttons");
        testResults.Add("7a. If 'Draw' clicked: Card is committed, transition to PLAYER_TURN_COMMIT");
        testResults.Add("7b. If 'Cancel' clicked: Return to PLAYER_TURN_PICK, can select another card");
        testResults.Add("");

        // Check all components
        if (gameManager == null || confirmUI == null || cameraSystem == null || aiHand == null)
        {
            testResults.Add("❌ Missing required components. Cannot run full test.");
            return;
        }

        // Check game state
        var currentStateField = typeof(GameManager).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        GameState currentState = (GameState)currentStateField.GetValue(gameManager);

        if (currentState != GameState.PLAYER_TURN_PICK)
        {
            testResults.Add($"❌ Game is not in PLAYER_TURN_PICK state (current: {currentState})");
            testResults.Add($"   Please start a new game to enter PLAYER_TURN_PICK state");
            return;
        }

        // Get test card
        if (aiHand.GetCards().Count == 0)
        {
            testResults.Add("❌ No cards in AI hand");
            return;
        }

        CardObject testCard = aiHand.GetCards()[0];
        testResults.Add($"✅ Test card selected: {testCard.cardData?.rank} {testCard.cardData?.suit}");
        testResults.Add("");

        // Simulate OnPointerDown
        testResults.Add("Step 1: Simulating OnPointerDown...");
        gameManager.OnCardPointerDown(testCard);
        testResults.Add($"✅ OnCardPointerDown() called");
        testResults.Add("");

        // Wait and check state transitions
        testResults.Add("⏳ Waiting for state transitions...");
        testResults.Add($"   Expected sequence: PICK → INTERRUPT (0.5s) → CONFIRM");
        testResults.Add("");

        testResults.Add("⚠️ MANUAL VERIFICATION REQUIRED:");
        testResults.Add("1. Watch the card - it should bounce back and shake");
        testResults.Add("2. Camera should zoom in on the card");
        testResults.Add("3. ConfirmUI should appear with 'Draw' and 'Cancel' buttons");
        testResults.Add("4. Click 'Draw' to test OnConfirmDraw flow");
        testResults.Add("5. OR click 'Cancel' to test OnConfirmAbort flow (returns to PICK)");
        testResults.Add("");

        testResults.Add("✅ Full integration test initiated");
        testResults.Add("   Check Unity Scene view and Game view for visual confirmation");
    }
}
