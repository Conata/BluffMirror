using UnityEngine;
using UnityEditor;
using FPSTrump.AI.LLM;
using FPSTrump.Psychology;

/// <summary>
/// TTS テスト & 設定ウィンドウ
/// Tools > Baba > TTS Test & Settings から起動
/// ElevenLabs/OpenAI TTS の音声テスト生成、プロバイダ切替、Voice ID設定を行う
/// </summary>
public class TTSTestWindow : EditorWindow
{
    // === References ===
    private LLMManager llmManager;
    private SerializedObject serializedLLM;

    // === Test Parameters ===
    private string testText = "お前の心理は、もう読み終えた";
    private AIEmotion testEmotion = AIEmotion.Calm;

    // === State ===
    private bool isGenerating = false;
    private string statusMessage = "";
    private Color statusColor = Color.white;
    private AudioClip lastGeneratedClip;
    private float lastResponseTime;

    // === Scroll ===
    private Vector2 scrollPos;

    // === SerializedProperty cache ===
    private SerializedProperty propTtsProvider;
    private SerializedProperty propVoiceId;
    private SerializedProperty propModel;
    private SerializedProperty propElevenLabsKeyFallback;

    [MenuItem("Tools/Baba/TTS Test && Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<TTSTestWindow>("TTS Test");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnEnable()
    {
        FindLLMManager();
    }

    private void FindLLMManager()
    {
        llmManager = FindFirstObjectByType<LLMManager>();

        if (llmManager != null)
        {
            serializedLLM = new SerializedObject(llmManager);
            propTtsProvider = serializedLLM.FindProperty("ttsProvider");
            propVoiceId = serializedLLM.FindProperty("elevenLabsVoiceId");
            propModel = serializedLLM.FindProperty("elevenLabsModel");
            propElevenLabsKeyFallback = serializedLLM.FindProperty("elevenLabsAPIKeyFallback");
        }
        else
        {
            serializedLLM = null;
        }
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader();
        EditorGUILayout.Space(8);

        if (llmManager == null)
        {
            DrawNoManagerWarning();
            EditorGUILayout.EndScrollView();
            return;
        }

        serializedLLM.Update();

        DrawProviderSettings();
        EditorGUILayout.Space(12);
        DrawElevenLabsSettings();
        EditorGUILayout.Space(12);
        DrawTestSection();
        EditorGUILayout.Space(12);
        DrawStatusSection();
        EditorGUILayout.Space(12);
        DrawVoiceSettingsPreview();

        serializedLLM.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();
    }

    // ===== Header =====
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("TTS Test & Settings", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            FindLLMManager();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ===== No Manager Warning =====
    private void DrawNoManagerWarning()
    {
        EditorGUILayout.HelpBox(
            "LLMManager not found in scene.\nPlay Mode or add LLMManager to scene.",
            MessageType.Warning);

        if (GUILayout.Button("Retry Find LLMManager"))
        {
            FindLLMManager();
        }
    }

    // ===== Provider Settings =====
    private void DrawProviderSettings()
    {
        EditorGUILayout.LabelField("TTS Provider", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(propTtsProvider, new GUIContent("Provider"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedLLM.ApplyModifiedProperties();
        }

        // Status indicators
        TTSProvider currentProvider = (TTSProvider)propTtsProvider.enumValueIndex;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Active:", GUILayout.Width(50));

        GUI.color = currentProvider == TTSProvider.OpenAI ? Color.green : Color.gray;
        GUILayout.Label("OpenAI", EditorStyles.miniButton, GUILayout.Width(70));

        GUI.color = currentProvider == TTSProvider.ElevenLabs ? Color.green : Color.gray;
        GUILayout.Label("ElevenLabs", EditorStyles.miniButton, GUILayout.Width(90));

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        // API Key status
        string openAIKeyStatus = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
            ? "ENV" : "Not set";
        string elevenKeyStatus = !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ELEVEN_API_KEY"))
            ? "ENV" : "Not set";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"OpenAI Key: {openAIKeyStatus}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"ElevenLabs Key: {elevenKeyStatus}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ===== ElevenLabs Settings =====
    private void DrawElevenLabsSettings()
    {
        EditorGUILayout.LabelField("ElevenLabs Settings", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(propVoiceId, new GUIContent("Voice ID"));

        if (string.IsNullOrEmpty(propVoiceId.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Voice ID is required for ElevenLabs.\nGet it from elevenlabs.io > Voices > Voice ID",
                MessageType.Info);
        }

        EditorGUILayout.PropertyField(propModel, new GUIContent("Model"));

        // Model presets
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Presets:", GUILayout.Width(55));

        if (GUILayout.Button("Multilingual v2", EditorStyles.miniButton))
        {
            propModel.stringValue = "eleven_multilingual_v2";
            serializedLLM.ApplyModifiedProperties();
        }
        if (GUILayout.Button("Turbo v2.5", EditorStyles.miniButton))
        {
            propModel.stringValue = "eleven_turbo_v2_5";
            serializedLLM.ApplyModifiedProperties();
        }
        if (GUILayout.Button("Flash v2.5", EditorStyles.miniButton))
        {
            propModel.stringValue = "eleven_flash_v2_5";
            serializedLLM.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(propElevenLabsKeyFallback,
            new GUIContent("API Key (Fallback)", "Inspector fallback. Prefer ELEVEN_API_KEY env var."));

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Open ElevenLabs Voice Library"))
        {
            Application.OpenURL("https://elevenlabs.io/app/voice-library");
        }
    }

    // ===== Test Section =====
    private void DrawTestSection()
    {
        EditorGUILayout.LabelField("Test Generation", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Test Text:");
        testText = EditorGUILayout.TextArea(testText, GUILayout.Height(50));

        testEmotion = (AIEmotion)EditorGUILayout.EnumPopup("Emotion", testEmotion);

        EditorGUILayout.Space(4);

        // Quick text presets
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Presets:", GUILayout.Width(55));

        if (GUILayout.Button("Calm", EditorStyles.miniButton))
        {
            testText = "お前の心理は、もう読み終えた";
            testEmotion = AIEmotion.Calm;
        }
        if (GUILayout.Button("Taunt", EditorStyles.miniButton))
        {
            testText = "ジョーカーはここだよ。引いてみるか？";
            testEmotion = AIEmotion.Pleased;
        }
        if (GUILayout.Button("Frustrated", EditorStyles.miniButton))
        {
            testText = "くっ... まさか、そう来るとは";
            testEmotion = AIEmotion.Frustrated;
        }
        if (GUILayout.Button("Long", EditorStyles.miniButton))
        {
            testText = "お前の行動パターンは全て記録している。慎重に見えて実は直感に頼りがちだ。その弱点を突かせてもらう";
            testEmotion = AIEmotion.Anticipating;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Generate button
        EditorGUI.BeginDisabledGroup(isGenerating || !Application.isPlaying);
        if (GUILayout.Button(isGenerating ? "Generating..." : "Generate & Play", GUILayout.Height(35)))
        {
            GenerateAndPlay();
        }
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode required for TTS generation.", MessageType.Info);
        }
    }

    // ===== Status Section =====
    private void DrawStatusSection()
    {
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUI.color = statusColor;
            EditorGUILayout.HelpBox(statusMessage, statusColor == Color.red ? MessageType.Error : MessageType.Info);
            GUI.color = Color.white;
        }

        if (lastGeneratedClip != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Last Generated Audio", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Duration: {lastGeneratedClip.length:F2}s");
            EditorGUILayout.LabelField($"Channels: {lastGeneratedClip.channels}");
            EditorGUILayout.LabelField($"Frequency: {lastGeneratedClip.frequency} Hz");
            EditorGUILayout.LabelField($"Response Time: {lastResponseTime:F2}s");

            if (GUILayout.Button("Replay"))
            {
                PlayClip(lastGeneratedClip);
            }

            if (GUILayout.Button("Stop"))
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopVoice();
            }
            EditorGUILayout.EndVertical();
        }
    }

    // ===== Voice Settings Preview =====
    private void DrawVoiceSettingsPreview()
    {
        TTSProvider currentProvider = (TTSProvider)propTtsProvider.enumValueIndex;
        if (currentProvider != TTSProvider.ElevenLabs) return;

        EditorGUILayout.LabelField("ElevenLabs Voice Settings Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Shows what voice_settings will be sent for each AIEmotion.\nAdjust values in LLMManager.SelectVoiceSettingsForEmotion().",
            MessageType.None);

        DrawSettingsRow("Calm", 0.70f, 0.75f, 0.00f, true);
        DrawSettingsRow("Anticipating", 0.40f, 0.75f, 0.30f, true);
        DrawSettingsRow("Pleased", 0.55f, 0.80f, 0.40f, true);
        DrawSettingsRow("Frustrated", 0.30f, 0.70f, 0.50f, true);
        DrawSettingsRow("Hurt", 0.60f, 0.80f, 0.20f, false);
        DrawSettingsRow("Relieved", 0.65f, 0.75f, 0.15f, true);
    }

    private void DrawSettingsRow(string emotionName, float stability, float similarity, float style, bool boost)
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField(emotionName, GUILayout.Width(85));

        GUI.color = Color.Lerp(Color.red, Color.green, stability);
        EditorGUILayout.LabelField($"stab:{stability:F2}", EditorStyles.miniLabel, GUILayout.Width(70));

        GUI.color = Color.Lerp(Color.red, Color.green, similarity);
        EditorGUILayout.LabelField($"sim:{similarity:F2}", EditorStyles.miniLabel, GUILayout.Width(65));

        GUI.color = Color.Lerp(Color.white, Color.cyan, style);
        EditorGUILayout.LabelField($"style:{style:F2}", EditorStyles.miniLabel, GUILayout.Width(70));

        GUI.color = boost ? Color.green : Color.gray;
        EditorGUILayout.LabelField(boost ? "BOOST" : "off", EditorStyles.miniLabel, GUILayout.Width(45));

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    // ===== Generation Logic =====
    private async void GenerateAndPlay()
    {
        if (llmManager == null)
        {
            statusMessage = "LLMManager not found";
            statusColor = Color.red;
            return;
        }

        if (string.IsNullOrEmpty(testText))
        {
            statusMessage = "Test text is empty";
            statusColor = Color.red;
            return;
        }

        isGenerating = true;
        statusMessage = "Generating TTS...";
        statusColor = Color.yellow;
        Repaint();

        float startTime = Time.realtimeSinceStartup;

        try
        {
            AudioClip clip = await llmManager.GenerateTTSAsync(testText, testEmotion);
            lastResponseTime = Time.realtimeSinceStartup - startTime;

            if (clip != null)
            {
                lastGeneratedClip = clip;

                TTSProvider provider = (TTSProvider)propTtsProvider.enumValueIndex;
                statusMessage = $"Success! ({provider}, {lastResponseTime:F2}s, {clip.length:F2}s audio)";
                statusColor = Color.green;

                PlayClip(clip);
            }
            else
            {
                statusMessage = $"Generation returned null ({lastResponseTime:F2}s). Check Console for errors.";
                statusColor = Color.red;
            }
        }
        catch (System.Exception ex)
        {
            lastResponseTime = Time.realtimeSinceStartup - startTime;
            statusMessage = $"Error: {ex.Message}";
            statusColor = Color.red;
            Debug.LogError($"[TTSTestWindow] {ex}");
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVoice(clip, Vector3.zero, 1.0f);
        }
        else
        {
            Debug.LogWarning("[TTSTestWindow] AudioManager not found, cannot play audio");
        }
    }
}
