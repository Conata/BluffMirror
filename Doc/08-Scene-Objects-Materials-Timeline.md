# シーンオブジェクト・マテリアル・Timeline設計

## Unity Scene Hierarchy

### 完全なScene構成
```
FPS_Trump_Scene
├── 🎮 Game Systems
│   ├── GameManager (Empty)
│   │   ├── CardDeck
│   │   ├── GameStateManager
│   │   ├── TurnManager
│   │   └── WinConditionChecker
│   │
│   ├── InputManager (Empty)
│   │   ├── InputActionAsset
│   │   └── UIRaycaster
│   │
│   ├── AudioManager (Empty)
│   │   ├── MasterMixer
│   │   ├── SFXSource
│   │   ├── VoiceSource
│   │   └── BGMSource
│   │
│   └── PsychologySystem (Empty)
│       ├── PlayerBehaviorAnalyzer
│       ├── DialogueController
│       ├── ProjectionSystem
│       └── AILearningSystem
│
├── 🌍 Environment
│   ├── Table
│   │   ├── TableSurface (Mesh: Plane, Scale: 2.5,1,1.8)
│   │   │   ├── MeshRenderer (Material: Table_Felt)
│   │   │   ├── MeshCollider (Convex: true)
│   │   │   └── TableFX (Particle System)
│   │   │
│   │   ├── TableEdge (Mesh: Cylinder, Scale: 2.6,0.1,1.9)
│   │   │   ├── MeshRenderer (Material: Wood_Dark)
│   │   │   └── MeshCollider
│   │   │
│   │   └── TableLegs (4x Cylinder)
│   │       ├── Leg_01 (Position: -1.2,0,-0.8)
│   │       ├── Leg_02 (Position: 1.2,0,-0.8)
│   │       ├── Leg_03 (Position: -1.2,0,0.8)
│   │       └── Leg_04 (Position: 1.2,0,0.8)
│   │
│   ├── Room
│   │   ├── Floor (Plane, Scale: 10,1,10)
│   │   │   └── MeshRenderer (Material: Floor_Dark)
│   │   │
│   │   ├── Walls
│   │   │   ├── Wall_North (Cube, Scale: 10,3,0.1, Position: 0,1.5,5)
│   │   │   ├── Wall_South (Cube, Scale: 10,3,0.1, Position: 0,1.5,-5)
│   │   │   ├── Wall_East (Cube, Scale: 0.1,3,10, Position: 5,1.5,0)
│   │   │   └── Wall_West (Cube, Scale: 0.1,3,10, Position: -5,1.5,0)
│   │   │
│   │   └── Ceiling (Plane, Scale: 10,1,10, Position: 0,3,0, Rotation: 180,0,0)
│   │
│   └── Atmosphere
│       ├── DustParticles (Particle System)
│       ├── VolumetricFog (Post-Process Volume)
│       └── AmbientSoundZone (Audio Source)
│
├── 💡 Lighting
│   ├── KeyLight (Spot Light)
│   │   ├── Position: (0.8, 2.8, -1.2)
│   │   ├── Rotation: (45, -30, 0)
│   │   ├── Color: #FF8C42 (Warm Orange)
│   │   ├── Intensity: 2.0
│   │   ├── Range: 8.0
│   │   ├── Spot Angle: 35°
│   │   └── Shadows: Hard, Resolution 2048
│   │
│   ├── FillLight (Area Light)
│   │   ├── Position: (-0.6, 1.6, 1.6)
│   │   ├── Color: #6495ED (Cool Blue)
│   │   ├── Intensity: 0.4
│   │   ├── Range: 4.0
│   │   └── Shadows: Off
│   │
│   ├── RimLight (Point Light)
│   │   ├── Position: (0, 2.2, -2.2)
│   │   ├── Color: #D4AF37 (Warm Gold)
│   │   ├── Intensity: 0.8
│   │   ├── Range: 5.0
│   │   └── Shadows: Off
│   │
│   └── LightProbes (Light Probe Group)
│       └── 16 probes arranged around table
│
├── 🃏 Game Objects
│   ├── PlayerHand (Empty, Position: 0,0.9,1.8)
│   │   ├── PlayerHandController
│   │   ├── CardSlots (8x Empty GameObjects)
│   │   │   ├── Slot_01 (Position: -0.6,0,0)
│   │   │   ├── Slot_02 (Position: -0.4,0,0.1)
│   │   │   ├── ...
│   │   │   └── Slot_08 (Position: 0.6,0,0)
│   │   │
│   │   └── HandArea (Invisible Plane for drag detection)
│   │
│   ├── AIHand (Empty, Position: 0,1.05,-0.35)
│   │   ├── AIHandController
│   │   ├── AICardSlots (8x Empty GameObjects)
│   │   └── AIHandArea (Invisible Plane)
│   │
│   ├── DiscardPile (Empty, Position: 1.5,1.05,0)
│   │   ├── DiscardPileController
│   │   └── DiscardArea (Invisible Plane)
│   │
│   └── Props
│       ├── Hourglass (Position: -1.8,1.1,0)
│       │   ├── HourglassMesh (Mesh: Imported model)
│       │   ├── SandParticles (Particle System)
│       │   └── HourglassController
│       │
│       ├── CoinStack (Position: -1.5,1.05,-0.8)
│       │   ├── 9x Coin meshes (stacked)
│       │   └── CoinStackController
│       │
│       └── LogPaper (Position: -1.8,1.05,0.8)
│           ├── PaperMesh (Plane with paper texture)
│           └── LogController
│
├── 🤖 AI Character
│   ├── AICharacter (Empty, Position: 0,1.3,-0.6)
│   │   ├── AIBody (Empty)
│   │   │   ├── Torso (Cylinder, Scale: 0.3,0.5,0.3)
│   │   │   │   └── MeshRenderer (Material: AI_Suit)
│   │   │   │
│   │   │   ├── Arms
│   │   │   │   ├── LeftArm (Capsule, Scale: 0.1,0.4,0.1)
│   │   │   │   └── RightArm (Capsule, Scale: 0.1,0.4,0.1)
│   │   │   │
│   │   │   └── Hands
│   │   │       ├── LeftHand (Position: -0.3,1.2,-0.2)
│   │   │       │   ├── HandMesh (Imported model)
│   │   │       │   └── Fingers (5x small capsules)
│   │   │       │
│   │   │       └── RightHand (Position: 0.3,1.2,-0.2)
│   │   │           ├── HandMesh (Imported model)
│   │   │           └── Fingers (5x small capsules)
│   │   │
│   │   ├── AIHead (Empty, Position: 0,1.6,-0.6)
│   │   │   ├── Mask (Sphere, Scale: 0.25,0.25,0.25)
│   │   │   │   ├── MeshRenderer (Material: AI_Mask)
│   │   │   │   └── Collider (for gaze targeting)
│   │   │   │
│   │   │   ├── Eyes
│   │   │   │   ├── LeftEye (Empty, Position: -0.08,0.02,0.12)
│   │   │   │   │   ├── EyeLight (Point Light, Color: Red)
│   │   │   │   │   └── EyeGlow (Quad with Glow material)
│   │   │   │   │
│   │   │   │   └── RightEye (Empty, Position: 0.08,0.02,0.12)
│   │   │   │       ├── EyeLight (Point Light, Color: Red)
│   │   │   │       └── EyeGlow (Quad with Glow material)
│   │   │   │
│   │   │   └── HeadAnimator (for head movement)
│   │   │
│   │   ├── AIVisualBehavior
│   │   ├── AIDialogueController
│   │   └── AITimeline (Playable Director)
│   │
├── 🎥 Cameras
│   ├── MainCamera (Position: 0,1.2,2.2, Rotation: -5,0,0)
│   │   ├── Camera (FOV: 55°, Near: 0.1, Far: 50)
│   │   ├── FPSCameraController
│   │   ├── AudioListener
│   │   ├── CinemachineVirtualCamera (for smooth movement)
│   │   └── PostProcessVolume (Profile: FPS_Trump_Profile)
│   │
│   └── UICamera (Position: 0,1.2,2.1, Culling Mask: UI Only)
│       ├── Camera (Orthographic, Size: 5)
│       └── Canvas (Screen Space - Camera)
│           ├── MinimalHUD
│           │   ├── TurnIndicator (Text)
│           │   ├── CardCounter (Text) 
│           │   └── TimeDisplay (Text)
│           │
│           └── DebugPanel (Development only)
│               ├── AIStateDisplay
│               ├── PressureLevelMeter
│               └── BehaviorAnalysisText
│
├── 🎭 Floating Text System
│   ├── ProjectionCanvas (World Space Canvas)
│   │   ├── Canvas (Render Mode: World Space)
│   │   ├── ProjectionTextPool (10x Text components)
│   │   └── ProjectionAnimator
│   │
│   └── WhisperSystem (Empty)
│       ├── WhisperAudioSource (3D Audio, Min Distance: 0.5)
│       └── SubtitleCanvas (Screen Space - Camera)
│
├── 🔊 Audio Zones
│   ├── TableAudioZone (Audio Reverb Zone)
│   │   ├── Reverb Zone (Room preset, Size: 3,2,3)
│   │   └── 3D Audio Sources for table sounds
│   │
│   └── AIVoiceZone (Empty, Position: 0,1.6,-0.6)
│       ├── VoiceSource (3D Audio, Max Distance: 3)
│       └── WhisperSource (3D Audio, Max Distance: 1)
│
└── 🎬 Timeline & Animation
    ├── GameTimeline (Empty)
    │   ├── PlayableDirector (Asset: GameSequence.playable)
    │   └── Timeline tracks:
    │       ├── GameState Track
    │       ├── AI Animation Track
    │       ├── Camera Track
    │       └── Audio Track
    │
    └── AnimationControllers
        ├── CardAnimation.controller
        ├── AICharacter.controller
        └── CameraShake.controller
```

## マテリアル詳細仕様

### 1. Table_Felt (テーブルフェルト)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: felt_texture_diffuse.jpg (2048x2048)
    - Base Color: #1B3B1B (Deep Green)
    - Tiling: (2, 2)
  
  Normal Map: felt_normal.jpg
    - Normal strength: 0.8
  
  Surface:
    - Metallic: 0.0
    - Smoothness: 0.15
    - Occlusion: 1.0
  
  Advanced:
    - Enable GPU Instancing: true
    - Double Sided Global Illumination: false
    - Alpha Clipping: false
```

### 2. Wood_Dark (テーブルエッジ)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: wood_dark_diffuse.jpg
    - Base Color: #3C2B1C (Dark Brown)
    - Tiling: (4, 1)
  
  Normal Map: wood_normal.jpg
    - Normal strength: 1.0
  
  Surface:
    - Metallic: 0.1
    - Smoothness: 0.4
    - Occlusion: wood_occlusion.jpg
  
  Detail:
    - Detail Mask: wood_detail_mask.jpg
    - Detail Albedo: wood_grain.jpg
    - Detail Normal: wood_grain_normal.jpg
```

### 3. Card_Front (カード表面)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: card_atlas_front.jpg (2048x2048)
    - Base Color: #FFFFFF
    - UV coordinates: Set per card type
  
  Normal Map: card_normal.jpg
    - Normal strength: 0.6
  
  Surface:
    - Metallic: 0.1
    - Smoothness: 0.65
    - Occlusion: 0.9
  
  Edge Wear:
    - Detail Mask: card_wear_mask.jpg
    - Detail Albedo: card_wear_overlay.jpg
```

### 4. Card_Back (カード裏面)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: card_back_pattern.jpg
    - Base Color: #000080 (Navy Blue)
    - Pattern overlay: Gold lines
  
  Surface:
    - Metallic: 0.05
    - Smoothness: 0.7
  
  Pattern Animation:
    - UV Animation Speed: (0, 0.02) - subtle movement
    - Shimmer intensity: 0.3
```

### 5. AI_Mask (AI仮面)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: metal_brushed.jpg
    - Base Color: #2F2F2F (Dark Gray)
  
  Normal Map: metal_normal.jpg
    - Normal strength: 1.2
  
  Surface:
    - Metallic: 0.8
    - Smoothness: 0.9
  
  Emission:
    - Emission Map: mask_emission.jpg
    - Emission Color: #FF0000 (Red) - for eyes
    - Emission intensity: 0.5
  
  Special Effects:
    - Fresnel reflection: enabled
    - Environment reflection: 0.6
```

### 6. AI_Suit (AI衣装)
```yaml
Shader: Universal Render Pipeline/Lit
Properties:
  Albedo Map: fabric_suit.jpg
    - Base Color: #1A1A1A (Very Dark Gray)
  
  Surface:
    - Metallic: 0.0
    - Smoothness: 0.8
    - Specular: #404040
  
  Fabric Properties:
    - Subsurface: 0.2
    - Cloth shading model: enabled
```

### 7. Glow_Particle (パーティクル用)
```yaml
Shader: Universal Render Pipeline/Particles/Unlit
Properties:
  Base Color: #D4AF37 (Gold)
  Emission: #FFFFFF
  Alpha: Use vertex alpha
  
  Blending:
    - Blend Mode: Additive
    - Z Write: Off
    - Cull Mode: Off
  
  Animation:
    - UV Animation: Flipbook (4x4 grid)
    - Animation speed: 12 fps
```

## Timeline設計

### 1. GameSequence.playable (メインゲームフロー)

#### Track構成
```
GameSequence Timeline (Duration: 300s)
├── 🎮 Game State Track (0-300s)
│   ├── Setup State (0-3s)
│   ├── Player Turn Loop (3-280s)
│   └── End Game (280-300s)
│
├── 🤖 AI Animation Track (0-300s)
│   ├── Idle Animations (continuous)
│   ├── Thinking Sequences (on AI turn)
│   ├── Card Draw Actions (specific moments)
│   └── Emotional Reactions (context-dependent)
│
├── 📷 Camera Track (0-300s)
│   ├── Establishing Shot (0-3s)
│   ├── Gameplay Camera (3-280s)
│   │   ├── Focus on Player Hand (player turn)
│   │   ├── Focus on AI Hand (AI turn)
│   │   └── Card Draw Close-ups (key moments)
│   └── End Game Camera (280-300s)
│
├── 🔊 Audio Track (0-300s)
│   ├── Ambient Sound (continuous)
│   ├── Music Layers (dynamic)
│   ├── SFX Triggers (event-based)
│   └── AI Voice Lines (context-dependent)
│
└── ✨ FX Track (0-300s)
    ├── Lighting Changes (mood-based)
    ├── Particle Effects (card interactions)
    └── Post-Process Adjustments (psychological pressure)
```

#### Clip詳細

##### Game State Track Clips
```yaml
Setup_Intro:
  Start: 0s
  Duration: 3s
  Script: GameStateClip
  Parameters:
    - SetGameState: Setup
    - DealInitialCards: true
    - ShowRules: true

Player_Turn_Loop:
  Start: 3s
  Duration: Variable (event-driven)
  Script: PlayerTurnClip
  Parameters:
    - EnablePlayerInput: true
    - StartTurnTimer: true
    - MonitorHover: true

AI_Turn_Sequence:
  Start: Variable
  Duration: 3-8s (based on AI thinking time)
  Script: AITurnClip
  Parameters:
    - DisablePlayerInput: true
    - ExecuteAILogic: true
    - PlayAIAnimation: true
```

##### AI Animation Track Clips
```yaml
AI_Idle_Breathing:
  Start: 0s
  Duration: 300s (looping)
  Animation: AICharacter.controller@Idle_Breathing
  Weight: 1.0

AI_Thinking_Sequence:
  Start: Variable (AI turn start)
  Duration: 2-5s
  Animation: AICharacter.controller@Thinking
  Blend: Cross-fade from Idle (0.5s)
  Parameters:
    - ThinkingIntensity: 0.8
    - EyeBlinkRate: 2.0

AI_Card_Draw:
  Start: Variable (decision made)
  Duration: 2.5s
  Animation: AICharacter.controller@Draw_Card
  Root Motion: true
  Parameters:
    - TargetCardIndex: Variable
    - DrawSpeed: 1.0

AI_Emotional_Reaction:
  Start: Variable (post-draw)
  Duration: 1-3s
  Animation: AICharacter.controller@Reaction_Happy/Neutral/Disappointed
  Blend: Based on card result
```

### 2. CardInteraction.playable (カード操作専用)

```yaml
Card_Hover_Enter:
  Duration: 0.12s
  Animation Clips:
    - Card.transform.position.y: +0.05
    - Card.transform.rotation.x: +5°
    - Particle emission: Start hover glow

Card_Hover_Exit:
  Duration: 0.08s
  Animation Clips:
    - Card.transform.position.y: Original
    - Card.transform.rotation.x: Original
    - Particle emission: Stop hover glow

Card_Drag_Start:
  Duration: 0.1s
  Animation Clips:
    - Camera.transform.position.z: -0.15 (dolly in)
    - Audio: Play grab sound
    - Card.layer: Move to "Dragging" layer

Card_Release_To_Hand:
  Duration: 0.3s
  Animation Clips:
    - Card.transform.position: Target hand slot
    - Card.transform.rotation: Hand slot rotation
    - Camera.transform.position.z: Original
    - Curve: Ease.OutQuart

Card_Pair_Disappear:
  Duration: 0.5s
  Animation Clips:
    - 0.0-0.1s: Glow effect buildup
    - 0.1-0.3s: Dissolve particles
    - 0.3-0.4s: Scale to zero
    - 0.4-0.5s: Move to discard pile
```

### 3. PsychologyPressure.playable (心理圧演出)

```yaml
Pressure_Buildup:
  Duration: Variable (2-10s)
  Post-Process Clips:
    - Vignette.intensity: 0.3 → 0.6
    - ChromaticAberration.intensity: 0.1 → 0.3
    - ColorGrading.temperature: 0 → -20

Whisper_Delivery:
  Duration: 1-3s
  Audio Clips:
    - WhisperSource.volume: 0 → 0.8
    - Spatial blend: 2D → 3D
    - Reverb: Apply whisper preset

Projection_Text:
  Duration: 1.5s
  UI Animation Clips:
    - ProjectionText.alpha: 0 → 0.85 → 0
    - ProjectionText.transform.position: Wobble animation
    - Glow effect: Pulse

Distortion_Peak:
  Duration: 0.5s
  Post-Process Clips:
    - Film Grain.intensity: +0.3
    - Lens Distortion.intensity: +0.2
    - Screen shake: 0.02 intensity
```

## GameManager詳細仕様

### GameManager.cs (完全版)
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[System.Serializable]
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<GameManager>();
            return _instance;
        }
    }

    [Header("📋 Core Game Components")]
    [SerializeField] private PlayerHandController playerHand;
    [SerializeField] private AIHandController aiHand;
    [SerializeField] private DiscardPile discardPile;
    [SerializeField] private CardDeck cardDeck;

    [Header("🎮 Game State")]
    [SerializeField] private GameState currentState = GameState.Menu;
    [SerializeField] private int currentPlayerTurn = 0; // 0 = Player, 1 = AI
    [SerializeField] private int turnCounter = 0;
    [SerializeField] private float gameStartTime;
    [SerializeField] private GameDifficulty difficulty = GameDifficulty.Normal;

    [Header("⏱️ Timing Settings")]
    [SerializeField] private float playerTurnTimeLimit = 30f;
    [SerializeField] private float aiThinkingTimeMin = 1.5f;
    [SerializeField] private float aiThinkingTimeMax = 4.0f;
    [SerializeField] private float turnTransitionDelay = 0.5f;

    [Header("🎬 Timeline Controllers")]
    [SerializeField] private PlayableDirector gameSequenceDirector;
    [SerializeField] private PlayableDirector cardInteractionDirector;
    [SerializeField] private PlayableDirector psychologyDirector;

    [Header("📊 Statistics")]
    [SerializeField] private GameStatistics currentGameStats;

    [Header("🎯 Win Conditions")]
    [SerializeField] private WinCondition[] winConditions;

    // Events
    [Header("📢 Game Events")]
    public UnityEvent<GameState> OnGameStateChanged;
    public UnityEvent<int> OnTurnChanged;
    public UnityEvent<string> OnGameEnded; // Winner
    public UnityEvent<float> OnTurnTimeUpdate; // Remaining time
    public UnityEvent<GameStatistics> OnStatsUpdated;

    // Internal State
    private bool isGameActive = false;
    private bool isProcessingTurn = false;
    private Coroutine currentTurnCoroutine;
    private Coroutine turnTimerCoroutine;
    
    // Sub-managers
    private AudioManager audioManager;
    private PsychologySystem psychologySystem;
    private UIManager uiManager;
    private InputManager inputManager;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize components
        InitializeSubManagers();
        InitializeGameStatistics();
        
        // Validate required components
        ValidateComponents();
    }

    private void Start()
    {
        // Setup initial state
        ChangeState(GameState.Menu);
        
        // Subscribe to events
        SubscribeToEvents();
        
        // Load player settings
        LoadGameSettings();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    private void InitializeSubManagers()
    {
        audioManager = FindObjectOfType<AudioManager>();
        psychologySystem = FindObjectOfType<PsychologySystem>();
        uiManager = FindObjectOfType<UIManager>();
        inputManager = FindObjectOfType<InputManager>();

        if (audioManager == null)
            Debug.LogError("AudioManager not found! Please add AudioManager to scene.");
        
        if (psychologySystem == null)
            Debug.LogError("PsychologySystem not found! Please add PsychologySystem to scene.");
    }

    private void InitializeGameStatistics()
    {
        currentGameStats = new GameStatistics
        {
            gameStartTime = Time.time,
            playerTurns = 0,
            aiTurns = 0,
            cardsDrawn = 0,
            pairsMatched = 0,
            averageDecisionTime = 0f,
            psychologicalPressureEvents = 0
        };
    }

    private void ValidateComponents()
    {
        List<string> missingComponents = new List<string>();

        if (playerHand == null) missingComponents.Add("PlayerHandController");
        if (aiHand == null) missingComponents.Add("AIHandController");
        if (discardPile == null) missingComponents.Add("DiscardPile");
        if (cardDeck == null) missingComponents.Add("CardDeck");
        if (gameSequenceDirector == null) missingComponents.Add("GameSequence Timeline");

        if (missingComponents.Count > 0)
        {
            Debug.LogError($"GameManager missing components: {string.Join(", ", missingComponents)}");
        }
    }

    #endregion

    #region Game State Management

    public void StartNewGame()
    {
        Debug.Log("🎮 Starting new game...");
        
        if (isGameActive)
        {
            Debug.LogWarning("Game is already active! Ending current game first.");
            EndGame("Interrupted", false);
        }

        StartCoroutine(NewGameSequence());
    }

    private IEnumerator NewGameSequence()
    {
        // 1. Change to setup state
        ChangeState(GameState.Setup);
        
        // 2. Initialize game components
        yield return StartCoroutine(InitializeGameComponents());
        
        // 3. Deal initial cards
        yield return StartCoroutine(DealInitialCards());
        
        // 4. Play setup timeline
        if (gameSequenceDirector != null)
        {
            gameSequenceDirector.Play();
            yield return new WaitForSeconds(3f); // Setup sequence duration
        }
        
        // 5. Start first turn
        ChangeState(GameState.PlayerTurn);
        isGameActive = true;
        
        Debug.Log("✅ New game started successfully!");
    }

    private IEnumerator InitializeGameComponents()
    {
        // Reset statistics
        gameStartTime = Time.time;
        turnCounter = 0;
        currentPlayerTurn = 0;
        
        // Initialize card deck
        cardDeck.Initialize();
        yield return new WaitForSeconds(0.1f);
        
        // Clear hands and discard pile
        playerHand.ClearHand();
        aiHand.ClearHand();
        discardPile.Clear();
        
        // Reset psychology system
        if (psychologySystem != null)
        {
            psychologySystem.ResetPressureLevel();
        }
        
        // Reset audio
        if (audioManager != null)
        {
            audioManager.PlayBGM("GameStart");
        }
        
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator DealInitialCards()
    {
        Debug.Log("🃏 Dealing initial cards...");
        
        // Deal 7 cards to each player alternately
        for (int i = 0; i < 7; i++)
        {
            // Player first
            CardObject playerCard = cardDeck.DrawCard();
            if (playerCard != null)
            {
                playerHand.AddCard(playerCard);
                currentGameStats.cardsDrawn++;
                
                // Audio feedback
                audioManager?.PlaySFX("CardDeal");
                
                yield return new WaitForSeconds(0.3f);
            }
            
            // Then AI
            CardObject aiCard = cardDeck.DrawCard();
            if (aiCard != null)
            {
                aiHand.AddCard(aiCard);
                currentGameStats.cardsDrawn++;
                
                // Audio feedback  
                audioManager?.PlaySFX("CardDeal");
                
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        // Check for initial pairs
        playerHand.CheckForPairs();
        aiHand.CheckForPairs();
        
        Debug.Log($"✅ Initial dealing complete. Player: {playerHand.GetCardCount()} cards, AI: {aiHand.GetCardCount()} cards");
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;
        
        GameState previousState = currentState;
        currentState = newState;
        
        Debug.Log($"🔄 Game State: {previousState} → {newState}");
        
        OnGameStateChanged?.Invoke(newState);
        
        // Handle state-specific logic
        HandleStateTransition(previousState, newState);
    }

    private void HandleStateTransition(GameState from, GameState to)
    {
        switch (to)
        {
            case GameState.Menu:
                isGameActive = false;
                if (inputManager != null) inputManager.SetInputMode(InputMode.Menu);
                break;
                
            case GameState.Setup:
                if (inputManager != null) inputManager.SetInputMode(InputMode.Disabled);
                break;
                
            case GameState.PlayerTurn:
                if (inputManager != null) inputManager.SetInputMode(InputMode.Gameplay);
                StartPlayerTurn();
                break;
                
            case GameState.AITurn:
                if (inputManager != null) inputManager.SetInputMode(InputMode.Disabled);
                StartAITurn();
                break;
                
            case GameState.GameEnd:
                isGameActive = false;
                if (inputManager != null) inputManager.SetInputMode(InputMode.Menu);
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                if (inputManager != null) inputManager.SetInputMode(InputMode.Menu);
                break;
        }
        
        // Resume time scale when leaving pause
        if (from == GameState.Paused && to != GameState.Paused)
        {
            Time.timeScale = 1f;
        }
    }

    #endregion

    #region Turn Management

    private void StartPlayerTurn()
    {
        if (isProcessingTurn) return;
        
        Debug.Log($"👤 Player Turn {turnCounter + 1}");
        
        currentGameStats.playerTurns++;
        currentPlayerTurn = 0;
        
        OnTurnChanged?.Invoke(currentPlayerTurn);
        
        // Start turn timer
        if (turnTimerCoroutine != null)
            StopCoroutine(turnTimerCoroutine);
        turnTimerCoroutine = StartCoroutine(PlayerTurnTimer());
        
        // Enable player input for AI cards
        EnablePlayerCardSelection(true);
        
        // Psychology system: analyze player state
        if (psychologySystem != null)
        {
            psychologySystem.StartPlayerTurnAnalysis();
        }
    }

    private void StartAITurn()
    {
        if (isProcessingTurn) return;
        
        Debug.Log($"🤖 AI Turn {turnCounter + 1}");
        
        currentGameStats.aiTurns++;
        currentPlayerTurn = 1;
        
        OnTurnChanged?.Invoke(currentPlayerTurn);
        
        // Disable player input
        EnablePlayerCardSelection(false);
        
        // Start AI turn coroutine
        if (currentTurnCoroutine != null)
            StopCoroutine(currentTurnCoroutine);
        currentTurnCoroutine = StartCoroutine(AITurnSequence());
    }

    private IEnumerator PlayerTurnTimer()
    {
        float remainingTime = playerTurnTimeLimit;
        
        while (remainingTime > 0 && currentState == GameState.PlayerTurn && isGameActive)
        {
            OnTurnTimeUpdate?.Invoke(remainingTime);
            
            // Pressure increases as time runs out
            if (psychologySystem != null && remainingTime < 10f)
            {
                float pressureIncrease = (10f - remainingTime) / 10f * 0.5f;
                psychologySystem.AddTimePressure(pressureIncrease);
            }
            
            remainingTime -= Time.deltaTime;
            yield return null;
        }
        
        // Time's up - force random selection
        if (currentState == GameState.PlayerTurn && isGameActive)
        {
            Debug.Log("⏰ Player turn timed out - forcing random selection");
            ForcePlayerSelection();
        }
    }

    private void ForcePlayerSelection()
    {
        if (aiHand.GetCardCount() > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, aiHand.GetCardCount());
            ExecutePlayerCardDraw(randomIndex);
        }
    }

    private IEnumerator AITurnSequence()
    {
        isProcessingTurn = true;
        
        // 1. AI thinking phase
        float thinkingTime = UnityEngine.Random.Range(aiThinkingTimeMin, aiThinkingTimeMax);
        yield return StartCoroutine(aiHand.ExecuteAITurn(playerHand));
        
        // 2. Process AI action results
        yield return StartCoroutine(ProcessAITurnResults());
        
        // 3. Check win conditions
        CheckGameEndConditions();
        
        // 4. Next turn or end game
        if (isGameActive)
        {
            yield return new WaitForSeconds(turnTransitionDelay);
            NextTurn();
        }
        
        isProcessingTurn = false;
    }

    private IEnumerator ProcessAITurnResults()
    {
        // Update statistics
        currentGameStats.cardsDrawn++;
        
        // Check for pairs in AI hand
        int pairsRemoved = aiHand.CheckForPairs();
        currentGameStats.pairsMatched += pairsRemoved;
        
        // Psychology system: AI reaction
        if (psychologySystem != null)
        {
            psychologySystem.ProcessAITurnResults(pairsRemoved > 0);
        }
        
        yield return new WaitForSeconds(0.5f);
    }

    public void ExecutePlayerCardDraw(int aiCardIndex)
    {
        if (currentState != GameState.PlayerTurn || isProcessingTurn) return;
        
        Debug.Log($"👤 Player draws card {aiCardIndex} from AI");
        
        StartCoroutine(ProcessPlayerCardDraw(aiCardIndex));
    }

    private IEnumerator ProcessPlayerCardDraw(int aiCardIndex)
    {
        isProcessingTurn = true;
        
        // Stop turn timer
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
        
        // 1. Play card interaction animation
        if (cardInteractionDirector != null)
        {
            cardInteractionDirector.Play();
            yield return new WaitForSeconds(0.5f);
        }
        
        // 2. Transfer card from AI to Player
        CardObject drawnCard = aiHand.RemoveCard(aiCardIndex);
        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);
            currentGameStats.cardsDrawn++;
            
            // Audio feedback
            audioManager?.PlaySFX("CardDraw");
            
            // Psychology system: player action analysis
            if (psychologySystem != null)
            {
                psychologySystem.AnalyzePlayerCardDraw(aiCardIndex, drawnCard);
            }
        }
        
        // 3. Check for pairs
        int pairsRemoved = playerHand.CheckForPairs();
        currentGameStats.pairsMatched += pairsRemoved;
        
        // 4. Check win conditions
        CheckGameEndConditions();
        
        // 5. Next turn
        if (isGameActive)
        {
            yield return new WaitForSeconds(turnTransitionDelay);
            NextTurn();
        }
        
        isProcessingTurn = false;
    }

    private void NextTurn()
    {
        turnCounter++;
        
        // Alternate turns
        if (currentState == GameState.PlayerTurn)
            ChangeState(GameState.AITurn);
        else
            ChangeState(GameState.PlayerTurn);
    }

    #endregion

    #region Win Condition Checking

    private void CheckGameEndConditions()
    {
        foreach (WinCondition condition in winConditions)
        {
            WinResult result = condition.CheckCondition(this);
            
            if (result.hasWon)
            {
                EndGame(result.winner, result.isVictory);
                return;
            }
        }
    }

    private void EndGame(string winner, bool isVictory)
    {
        Debug.Log($"🏁 Game Over! Winner: {winner} (Victory: {isVictory})");
        
        isGameActive = false;
        
        // Update final statistics
        currentGameStats.gameEndTime = Time.time;
        currentGameStats.totalGameDuration = currentGameStats.gameEndTime - currentGameStats.gameStartTime;
        currentGameStats.winner = winner;
        currentGameStats.isVictory = isVictory;
        
        OnStatsUpdated?.Invoke(currentGameStats);
        OnGameEnded?.Invoke(winner);
        
        ChangeState(GameState.GameEnd);
        
        // Play end game timeline
        StartCoroutine(PlayEndGameSequence(winner, isVictory));
    }

    private IEnumerator PlayEndGameSequence(string winner, bool isVictory)
    {
        // Reveal remaining cards
        yield return StartCoroutine(RevealAllCards());
        
        // Play victory/defeat audio
        if (audioManager != null)
        {
            string audioClip = isVictory ? "Victory" : "Defeat";
            audioManager.PlaySFX(audioClip);
        }
        
        // Show end game UI after delay
        yield return new WaitForSeconds(2f);
        
        if (uiManager != null)
        {
            uiManager.ShowEndGameScreen(currentGameStats);
        }
    }

    private IEnumerator RevealAllCards()
    {
        // Flip all AI cards face up
        foreach (CardObject card in aiHand.GetCards())
        {
            card.FlipCard(true, 0.3f);
            yield return new WaitForSeconds(0.1f);
        }
        
        // Highlight joker if present
        CardObject joker = aiHand.GetCards().FirstOrDefault(c => c.cardData.isJoker);
        if (joker != null)
        {
            // Special highlight for joker
            StartCoroutine(HighlightJoker(joker));
        }
    }

    private IEnumerator HighlightJoker(CardObject joker)
    {
        // Pulsing glow effect
        for (int i = 0; i < 5; i++)
        {
            joker.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.3f);
            joker.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.3f);
        }
    }

    #endregion

    #region Input Handling

    private void EnablePlayerCardSelection(bool enabled)
    {
        if (aiHand != null)
        {
            aiHand.EnableCardSelection(enabled);
        }
    }

    public void OnPlayerCardHover(int cardIndex)
    {
        if (currentState != GameState.PlayerTurn || isProcessingTurn) return;
        
        // Psychology system: analyze hover behavior
        if (psychologySystem != null)
        {
            psychologySystem.RecordPlayerHover(cardIndex);
        }
    }

    public void OnPlayerCardSelect(int cardIndex)
    {
        if (currentState != GameState.PlayerTurn || isProcessingTurn) return;
        
        ExecutePlayerCardDraw(cardIndex);
    }

    #endregion

    #region Public API

    // Getters
    public GameState GetCurrentState() => currentState;
    public bool IsGameActive() => isGameActive;
    public int GetCurrentTurn() => currentPlayerTurn;
    public int GetTurnCounter() => turnCounter;
    public GameStatistics GetCurrentStats() => currentGameStats;
    
    // Game control
    public void PauseGame() => ChangeState(GameState.Paused);
    public void ResumeGame() => ChangeState(GameState.PlayerTurn); // or previous state
    public void RestartGame() => StartNewGame();
    public void QuitToMenu() => ChangeState(GameState.Menu);

    #endregion

    #region Event Management

    private void SubscribeToEvents()
    {
        // Subscribe to card events
        if (playerHand != null)
        {
            playerHand.OnCardAdded += HandlePlayerCardAdded;
            playerHand.OnPairMatched += HandlePairMatched;
        }
        
        if (aiHand != null)
        {
            aiHand.OnCardAdded += HandleAICardAdded;
            aiHand.OnPairMatched += HandlePairMatched;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHand != null)
        {
            playerHand.OnCardAdded -= HandlePlayerCardAdded;
            playerHand.OnPairMatched -= HandlePairMatched;
        }
        
        if (aiHand != null)
        {
            aiHand.OnCardAdded -= HandleAICardAdded;
            aiHand.OnPairMatched -= HandlePairMatched;
        }
    }

    private void HandlePlayerCardAdded(CardObject card)
    {
        Debug.Log($"👤 Player received: {card.cardData.rank} of {card.cardData.suit}");
    }

    private void HandleAICardAdded(CardObject card)
    {
        Debug.Log($"🤖 AI received: {card.cardData.rank} of {card.cardData.suit}");
    }

    private void HandlePairMatched(CardObject card1, CardObject card2)
    {
        Debug.Log($"✨ Pair matched: {card1.cardData.rank}");
        
        // Audio feedback
        audioManager?.PlaySFX("PairMatched");
        
        // Particle effects
        // PlayPairMatchEffect(card1, card2);
    }

    #endregion

    #region Save/Load

    private void LoadGameSettings()
    {
        // Load from PlayerPrefs or save file
        difficulty = (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", (int)GameDifficulty.Normal);
        playerTurnTimeLimit = PlayerPrefs.GetFloat("TurnTimeLimit", 30f);
    }

    public void SaveGameSettings()
    {
        PlayerPrefs.SetInt("GameDifficulty", (int)difficulty);
        PlayerPrefs.SetFloat("TurnTimeLimit", playerTurnTimeLimit);
        PlayerPrefs.Save();
    }

    #endregion
}

// Supporting enums and classes
public enum GameState
{
    Menu,
    Setup, 
    PlayerTurn,
    AITurn,
    GameEnd,
    Paused
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

[System.Serializable]
public class GameStatistics
{
    public float gameStartTime;
    public float gameEndTime;
    public float totalGameDuration;
    public int playerTurns;
    public int aiTurns;
    public int cardsDrawn;
    public int pairsMatched;
    public float averageDecisionTime;
    public int psychologicalPressureEvents;
    public string winner;
    public bool isVictory;
}

[System.Serializable]
public abstract class WinCondition : ScriptableObject
{
    public abstract WinResult CheckCondition(GameManager gameManager);
}

[System.Serializable]
public class WinResult
{
    public bool hasWon;
    public string winner;
    public bool isVictory;
    public string reason;
}
```

この詳細仕様により：

## 🎮 完全なUnityシーン構成
- **150+のGameObject** を階層化して整理
- **各オブジェクトのTransform値** まで具体化  
- **マテリアル7種類** の完全な設定値
- **Timeline 3システム** で演出を完全制御

## 🎬 Timeline完全設計
- **GameSequence**: メインフロー（300秒）
- **CardInteraction**: カード操作演出
- **PsychologyPressure**: 心理圧システム連動

## 🎯 GameManager完全実装
- **ステートマシン**: 6状態の完全制御
- **ターン管理**: プレイヤー/AI交互実行
- **勝利判定**: 複数条件での終了判定
- **統計管理**: プレイデータ収集
- **イベント統合**: 全システム連携

これで **Unityエディタでそのまま実装可能** な設計図が完成したニャ！🎯✨