using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

/// <summary>
/// 音声再生の優先度レベル
/// </summary>
public enum VoicePriority
{
    Low = 0,      // ホバー台詞 — 他が再生中なら再生しない
    Medium = 1,   // メンタリスト台詞（挑発、Idle、Jokerティーズ）
    High = 2      // 感情リアクション、イントロ、アウトロ、迷い演出
}

/// <summary>
/// 音響システムの管理
/// 効果音、環境音、AI音声の再生制御
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup voiceGroup;

    [Header("AI Voice Clips (English)")]
    [SerializeField] private AudioClip[] gameStartVoices_EN;
    [SerializeField] private AudioClip[] cardDrawVoices_EN;
    [SerializeField] private AudioClip[] pairMatchVoices_EN;
    [SerializeField] private AudioClip[] victoryVoices_EN;
    [SerializeField] private AudioClip[] defeatVoices_EN;
    [SerializeField] private AudioClip[] pressureVoices_EN;

    [Header("AI Voice Clips (Japanese)")]
    [SerializeField] private AudioClip[] gameStartVoices_JA;
    [SerializeField] private AudioClip[] cardDrawVoices_JA;
    [SerializeField] private AudioClip[] pairMatchVoices_JA;
    [SerializeField] private AudioClip[] victoryVoices_JA;
    [SerializeField] private AudioClip[] defeatVoices_JA;
    [SerializeField] private AudioClip[] pressureVoices_JA;

    [Header("Card Sound Effects")]
    [SerializeField] private AudioClip cardHoverSound;
    [SerializeField] private AudioClip cardPickSound;
    [SerializeField] private AudioClip cardPlaceSound;
    [SerializeField] private AudioClip[] cardFlipSounds; // Multiple for variation

    [Header("Environment Sounds")]
    [SerializeField] private AudioClip roomAmbienceSound;
    [SerializeField] private AudioClip feltSlideSound;

    [Header("Music")]
    [SerializeField] private AudioClip bgmClip;        // メインBGM (DeepMode.mp3)
    [SerializeField] private float bgmVolume = 0.4f;

    [Header("Psychology Sound Effects")]
    [SerializeField] private AudioClip heartbeatNormalSound;   // 通常の心臓音
    [SerializeField] private AudioClip heartbeatIntenseSound;  // カードが少なくなった時の心臓音
    [SerializeField] private AudioClip whisperAmbienceSound;
    [SerializeField] private int cardThresholdForIntenseHeartbeat = 5; // この枚数以下で緊迫音に切り替え

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource heartbeatSource;

    [Header("Voice Reverb")]
    [SerializeField] private bool enableVoiceReverb = true;
    [SerializeField] private AudioReverbPreset voiceReverbPreset = AudioReverbPreset.Room;
    [SerializeField] [Range(-10000f, 0f)] private float reverbRoom = -400f;
    [SerializeField] [Range(0.1f, 20f)] private float reverbDecayTime = 2.0f;
    [SerializeField] [Range(-10000f, 2000f)] private float reverbLevel = -600f;
    private AudioReverbFilter voiceReverbFilter;

    [Header("Voice Distortion (電子ノイズ)")]
    [SerializeField] private bool enableVoiceDistortion = true;
    [SerializeField] [Range(0f, 1f)] private float distortionLevel = 0.15f;
    private AudioDistortionFilter voiceDistortionFilter;

    [Header("Voice Low Pass (こもり感)")]
    [SerializeField] private bool enableVoiceLowPass = true;
    [SerializeField] [Range(100f, 22000f)] private float lowPassCutoff = 4000f;
    [SerializeField] [Range(1f, 10f)] private float lowPassResonance = 1.2f;
    private AudioLowPassFilter voiceLowPassFilter;

    [Header("Volume Settings")]
    [SerializeField] private float masterVolume = 1.0f;
    [SerializeField] private float sfxVolume = 0.8f;
    [SerializeField] private float ambienceVolume = 0.3f;
    [SerializeField] private float voiceVolume = 0.7f;

    // 音声優先度トラッキング
    private VoicePriority currentVoicePriority = VoicePriority.Low;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad requires a root GameObject
            if (transform.parent != null)
                transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("[AudioManager] Instance created and marked as DontDestroyOnLoad");
        }
        else
        {
            Debug.LogWarning("[AudioManager] Duplicate instance detected, destroying new instance");
            Destroy(gameObject);
            return;
        }

        InitializeAudioSources();
    }

    private void Start()
    {
        // 環境音のループ再生開始
        PlayAmbience();
        // BGM再生開始
        PlayMusic();
    }

    private void Update()
    {
        // 音声再生完了時に優先度をリセット
        if (voiceSource != null && !voiceSource.isPlaying && currentVoicePriority != VoicePriority.Low)
        {
            currentVoicePriority = VoicePriority.Low;
        }
    }

    /// <summary>
    /// AudioSourceの初期化
    /// </summary>
    private void InitializeAudioSources()
    {
        // SFX Source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.outputAudioMixerGroup = sfxGroup;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D sound

        // Music Source
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = bgmVolume;

        // Ambience Source
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
        }
        ambienceSource.outputAudioMixerGroup = ambienceGroup;
        ambienceSource.loop = true;
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.volume = ambienceVolume;

        // Voice Source
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
        }
        voiceSource.outputAudioMixerGroup = voiceGroup;
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f; // 2D sound - always audible regardless of position

        // Voice Reverb Filter
        voiceReverbFilter = voiceSource.gameObject.GetComponent<AudioReverbFilter>();
        if (voiceReverbFilter == null)
        {
            voiceReverbFilter = voiceSource.gameObject.AddComponent<AudioReverbFilter>();
        }
        ApplyVoiceReverbSettings();

        // Voice Distortion Filter
        voiceDistortionFilter = voiceSource.gameObject.GetComponent<AudioDistortionFilter>();
        if (voiceDistortionFilter == null)
        {
            voiceDistortionFilter = voiceSource.gameObject.AddComponent<AudioDistortionFilter>();
        }
        ApplyVoiceDistortionSettings();

        // Voice Low Pass Filter
        voiceLowPassFilter = voiceSource.gameObject.GetComponent<AudioLowPassFilter>();
        if (voiceLowPassFilter == null)
        {
            voiceLowPassFilter = voiceSource.gameObject.AddComponent<AudioLowPassFilter>();
        }
        ApplyVoiceLowPassSettings();

        // Heartbeat Source
        if (heartbeatSource == null)
        {
            heartbeatSource = gameObject.AddComponent<AudioSource>();
        }
        heartbeatSource.outputAudioMixerGroup = sfxGroup;
        heartbeatSource.loop = false;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f;

        Debug.Log("[AudioManager] Audio sources initialized.");
    }

    #region Card Sound Effects

    /// <summary>
    /// カードホバー音を再生
    /// </summary>
    public void PlayCardHover()
    {
        if (cardHoverSound != null)
        {
            PlaySFX(cardHoverSound, 0.5f, Random.Range(0.95f, 1.05f));
        }
    }

    /// <summary>
    /// カードピック音を再生
    /// </summary>
    /// <param name="position">3D空間の位置（オプション）</param>
    public void PlayCardPick(Vector3? position = null)
    {
        if (cardPickSound != null)
        {
            if (position.HasValue)
            {
                PlaySFXAtPoint(cardPickSound, position.Value, 0.6f);
            }
            else
            {
                PlaySFX(cardPickSound, 0.6f);
            }
        }
    }

    /// <summary>
    /// カード配置音を再生
    /// </summary>
    /// <param name="position">3D空間の位置（オプション）</param>
    public void PlayCardPlace(Vector3? position = null)
    {
        if (cardPlaceSound != null)
        {
            if (position.HasValue)
            {
                PlaySFXAtPoint(cardPlaceSound, position.Value, 0.7f);
            }
            else
            {
                PlaySFX(cardPlaceSound, 0.7f);
            }
        }
    }

    /// <summary>
    /// カードフリップ音を再生（ランダムバリエーション）
    /// </summary>
    /// <param name="flipSpeed">フリップの速度（ピッチに影響）</param>
    public void PlayCardFlip(float flipSpeed = 1.0f)
    {
        if (cardFlipSounds != null && cardFlipSounds.Length > 0)
        {
            AudioClip randomFlip = cardFlipSounds[Random.Range(0, cardFlipSounds.Length)];
            float pitch = Mathf.Lerp(0.9f, 1.1f, flipSpeed);
            PlaySFX(randomFlip, 0.6f, pitch);
        }
    }

    #endregion

    #region Music

    /// <summary>
    /// BGM再生
    /// </summary>
    public void PlayMusic()
    {
        if (bgmClip != null && musicSource != null)
        {
            musicSource.clip = bgmClip;
            StartCoroutine(FadeInAudioSource(musicSource, bgmVolume, 2.0f));
            Debug.Log("[AudioManager] BGM started.");
        }
    }

    /// <summary>
    /// BGM停止（フェードアウト）
    /// </summary>
    public void StopMusic(float fadeDuration = 1.5f)
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutAudioSource(musicSource, fadeDuration));
        }
    }

    #endregion

    #region Environment Sounds

    /// <summary>
    /// 環境音（ルームアンビエンス）を再生
    /// </summary>
    public void PlayAmbience()
    {
        if (roomAmbienceSound != null && ambienceSource != null)
        {
            ambienceSource.clip = roomAmbienceSound;
            ambienceSource.Play();
            Debug.Log("[AudioManager] Room ambience started.");
        }
    }

    /// <summary>
    /// フェルトスライド音を再生
    /// </summary>
    /// <param name="dragSpeed">ドラッグの速度（ピッチに影響）</param>
    public void PlayFeltSlide(float dragSpeed = 1.0f)
    {
        if (feltSlideSound != null)
        {
            float pitch = Mathf.Lerp(0.8f, 1.2f, dragSpeed);
            PlaySFX(feltSlideSound, 0.4f, pitch);
        }
    }

    #endregion

    #region Psychology Sound Effects

    /// <summary>
    /// ハートビート音を再生（心理圧演出）
    /// </summary>
    /// <param name="intensity">圧力の強さ（0-1）</param>
    /// <param name="remainingCards">残りカード枚数（自動切り替え用）</param>
    public void PlayHeartbeat(float intensity = 0.5f, int remainingCards = 10)
    {
        if (heartbeatSource == null) return;

        // 残りカード枚数に応じて心臓音を選択
        AudioClip targetClip = remainingCards <= cardThresholdForIntenseHeartbeat
            ? heartbeatIntenseSound
            : heartbeatNormalSound;

        if (targetClip == null)
        {
            Debug.LogWarning("[AudioManager] Heartbeat sound is not assigned!");
            return;
        }

        // 現在のクリップと異なる場合、クロスフェードで切り替え
        if (heartbeatSource.isPlaying && heartbeatSource.clip != targetClip)
        {
            StartCoroutine(CrossfadeHeartbeat(targetClip, intensity, 1.0f));
        }
        else
        {
            // 新規再生
            heartbeatSource.clip = targetClip;
            heartbeatSource.volume = Mathf.Lerp(0.2f, 0.8f, intensity);
            heartbeatSource.loop = true;
            heartbeatSource.Play();

            Debug.Log($"[AudioManager] Heartbeat started - Type: {(remainingCards <= cardThresholdForIntenseHeartbeat ? "Intense" : "Normal")}, Intensity: {intensity}");
        }
    }

    /// <summary>
    /// ハートビートをクロスフェードで切り替え
    /// </summary>
    private IEnumerator CrossfadeHeartbeat(AudioClip newClip, float intensity, float duration)
    {
        float startVolume = heartbeatSource.volume;
        float elapsed = 0f;

        // フェードアウト
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            heartbeatSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration * 0.5f));
            yield return null;
        }

        // クリップを切り替え
        heartbeatSource.clip = newClip;
        heartbeatSource.Play();

        // フェードイン
        elapsed = 0f;
        float targetVolume = Mathf.Lerp(0.2f, 0.8f, intensity);
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.deltaTime;
            heartbeatSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration * 0.5f));
            yield return null;
        }

        heartbeatSource.volume = targetVolume;

        Debug.Log($"[AudioManager] Heartbeat crossfaded to: {newClip.name}");
    }

    /// <summary>
    /// ハートビート音を停止
    /// </summary>
    public void StopHeartbeat()
    {
        if (heartbeatSource != null)
        {
            StartCoroutine(FadeOutAudioSource(heartbeatSource, 1.0f));
        }
    }

    /// <summary>
    /// 心臓音の強度のみを変更（クリップは変更しない）
    /// </summary>
    public void SetHeartbeatIntensity(float intensity)
    {
        if (heartbeatSource != null && heartbeatSource.isPlaying)
        {
            float targetVolume = Mathf.Lerp(0.2f, 0.8f, intensity);
            StartCoroutine(FadeToVolume(heartbeatSource, targetVolume, 0.5f));
        }
    }

    /// <summary>
    /// AudioSourceを指定音量にフェード
    /// </summary>
    private IEnumerator FadeToVolume(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    /// <summary>
    /// 囁き環境音を再生
    /// </summary>
    public void PlayWhisperAmbience()
    {
        if (whisperAmbienceSound != null)
        {
            PlaySFX(whisperAmbienceSound, 0.3f);
        }
    }

    #endregion

    #region Voice Playback

    /// <summary>
    /// AI音声を2Dサウンドとして再生（空間音響なし）
    /// </summary>
    /// <param name="voiceClip">再生する音声クリップ</param>
    /// <param name="position">3D空間の位置（2Dサウンドなので無視される）</param>
    /// <param name="volume">音量</param>
    public void PlayVoice(AudioClip voiceClip, Vector3 position, float volume = 1.0f)
    {
        Debug.Log($"[AudioManager] PlayVoice called: clip={voiceClip != null}, voiceSource={voiceSource != null}, volume={volume}, voiceVolume={voiceVolume}");

        // voiceSourceがnullの場合、再初期化を試みる（シーン遷移時の対策）
        if (voiceSource == null)
        {
            Debug.LogWarning("[AudioManager] voiceSource is null, attempting to reinitialize...");
            InitializeAudioSources();
        }

        if (voiceClip != null && voiceSource != null)
        {
            // 2D sound (spatialBlend=0) - position is ignored
            // voiceSource.transform.position = position;
            voiceSource.clip = voiceClip;
            voiceSource.volume = volume * voiceVolume;
            voiceSource.Play();
            currentVoicePriority = VoicePriority.High;

            Debug.Log($"[AudioManager] Voice played (High, 2D), actual volume={voiceSource.volume}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] PlayVoice FAILED: voiceClip is {(voiceClip == null ? "NULL" : "OK")}, voiceSource is {(voiceSource == null ? "NULL" : "OK")}");
        }
    }

    /// <summary>
    /// 優先度付き音声再生（2Dサウンド）。現在再生中の音声より優先度が低い場合は再生しない。
    /// </summary>
    /// <returns>true: 再生開始, false: 優先度不足で再生拒否</returns>
    public bool TryPlayVoice(AudioClip voiceClip, Vector3 position, VoicePriority priority, float volume = 1.0f)
    {
        // voiceSourceがnullの場合、再初期化を試みる（シーン遷移時の対策）
        if (voiceSource == null)
        {
            Debug.LogWarning("[AudioManager] voiceSource is null in TryPlayVoice, attempting to reinitialize...");
            InitializeAudioSources();
        }

        if (voiceClip == null || voiceSource == null) return false;

        if (voiceSource.isPlaying && priority < currentVoicePriority)
        {
            Debug.Log($"[AudioManager] Voice rejected: {priority} < current {currentVoicePriority}");
            return false;
        }

        if (voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }

        currentVoicePriority = priority;
        // 2D sound (spatialBlend=0) - position is ignored
        // voiceSource.transform.position = position;
        voiceSource.clip = voiceClip;
        voiceSource.volume = volume * voiceVolume;
        voiceSource.Play();

        Debug.Log($"[AudioManager] Voice played (priority={priority}, 2D)");
        return true;
    }

    /// <summary>
    /// 現在再生中の音声を停止
    /// </summary>
    public void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
        currentVoicePriority = VoicePriority.Low;
    }

    /// <summary>
    /// 音声が再生中かどうか
    /// </summary>
    public bool IsVoicePlaying()
    {
        return voiceSource != null && voiceSource.isPlaying;
    }

    /// <summary>
    /// ゲーム開始ボイスを再生
    /// </summary>
    public void PlayGameStartVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(gameStartVoices_EN, gameStartVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// カードドローボイスを再生
    /// </summary>
    public void PlayCardDrawVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(cardDrawVoices_EN, cardDrawVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// ペアマッチボイスを再生
    /// </summary>
    public void PlayPairMatchVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(pairMatchVoices_EN, pairMatchVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// 勝利ボイスを再生
    /// </summary>
    public void PlayVictoryVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(victoryVoices_EN, victoryVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// 敗北ボイスを再生
    /// </summary>
    public void PlayDefeatVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(defeatVoices_EN, defeatVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// 心理圧ボイスを再生
    /// </summary>
    public void PlayPressureVoice(Vector3 position)
    {
        AudioClip clip = GetRandomVoiceClip(pressureVoices_EN, pressureVoices_JA);
        if (clip != null) PlayVoice(clip, position);
    }

    /// <summary>
    /// 言語設定に応じてランダムな音声クリップを取得
    /// </summary>
    private AudioClip GetRandomVoiceClip(AudioClip[] englishClips, AudioClip[] japaneseClips)
    {
        // GameSettings が存在しない場合は英語をデフォルトに
        bool isJapanese = GameSettings.Instance != null && GameSettings.Instance.IsJapanese();

        AudioClip[] targetClips = isJapanese ? japaneseClips : englishClips;

        if (targetClips == null || targetClips.Length == 0)
        {
            Debug.LogWarning($"[AudioManager] No voice clips assigned for {(isJapanese ? "Japanese" : "English")}");
            return null;
        }

        return targetClips[Random.Range(0, targetClips.Length)];
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 効果音を再生
    /// </summary>
    private void PlaySFX(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }
    }

    /// <summary>
    /// 3D空間の特定位置で効果音を再生
    /// </summary>
    private void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
        }
    }

    /// <summary>
    /// AudioSourceをフェードアウト
    /// </summary>
    private IEnumerator FadeOutAudioSource(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }

        source.volume = 0;
        source.Stop();
    }

    /// <summary>
    /// AudioSourceをフェードイン
    /// </summary>
    private IEnumerator FadeInAudioSource(AudioSource source, float targetVolume, float duration)
    {
        source.volume = 0;
        source.Play();

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(0, targetVolume, t / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    #endregion

    #region Voice Reverb Control

    /// <summary>
    /// リバーブ設定を適用
    /// </summary>
    private void ApplyVoiceReverbSettings()
    {
        if (voiceReverbFilter == null) return;

        voiceReverbFilter.enabled = enableVoiceReverb;

        if (enableVoiceReverb)
        {
            voiceReverbFilter.reverbPreset = voiceReverbPreset;

            // プリセット適用後にカスタム値で上書き
            if (voiceReverbPreset == AudioReverbPreset.User)
            {
                voiceReverbFilter.room = reverbRoom;
                voiceReverbFilter.decayTime = reverbDecayTime;
                voiceReverbFilter.reverbLevel = reverbLevel;
            }

            Debug.Log($"[AudioManager] Voice reverb enabled: preset={voiceReverbPreset}");
        }
    }

    /// <summary>
    /// リバーブのON/OFF切替
    /// </summary>
    public void SetVoiceReverbEnabled(bool enabled)
    {
        enableVoiceReverb = enabled;
        if (voiceReverbFilter != null)
        {
            voiceReverbFilter.enabled = enabled;
        }
    }

    /// <summary>
    /// リバーブプリセットを変更
    /// </summary>
    public void SetVoiceReverbPreset(AudioReverbPreset preset)
    {
        voiceReverbPreset = preset;
        ApplyVoiceReverbSettings();
    }

    /// <summary>
    /// ディストーション設定を適用
    /// </summary>
    private void ApplyVoiceDistortionSettings()
    {
        if (voiceDistortionFilter == null) return;

        voiceDistortionFilter.enabled = enableVoiceDistortion;
        if (enableVoiceDistortion)
        {
            voiceDistortionFilter.distortionLevel = distortionLevel;
            Debug.Log($"[AudioManager] Voice distortion enabled: level={distortionLevel:F2}");
        }
    }

    /// <summary>
    /// ディストーションのON/OFF切替
    /// </summary>
    public void SetVoiceDistortionEnabled(bool enabled)
    {
        enableVoiceDistortion = enabled;
        if (voiceDistortionFilter != null)
        {
            voiceDistortionFilter.enabled = enabled;
        }
    }

    /// <summary>
    /// ローパスフィルタ設定を適用
    /// </summary>
    private void ApplyVoiceLowPassSettings()
    {
        if (voiceLowPassFilter == null) return;

        voiceLowPassFilter.enabled = enableVoiceLowPass;
        if (enableVoiceLowPass)
        {
            voiceLowPassFilter.cutoffFrequency = lowPassCutoff;
            voiceLowPassFilter.lowpassResonanceQ = lowPassResonance;
            Debug.Log($"[AudioManager] Voice low pass enabled: cutoff={lowPassCutoff}Hz");
        }
    }

    /// <summary>
    /// ローパスのON/OFF切替
    /// </summary>
    public void SetVoiceLowPassEnabled(bool enabled)
    {
        enableVoiceLowPass = enabled;
        if (voiceLowPassFilter != null)
        {
            voiceLowPassFilter.enabled = enabled;
        }
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// マスターボリュームを設定
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", LinearToDecibel(masterVolume));
        }
    }

    /// <summary>
    /// SFXボリュームを設定
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", LinearToDecibel(sfxVolume));
        }
    }

    /// <summary>
    /// 音楽ボリュームを設定
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", LinearToDecibel(Mathf.Clamp01(volume)));
        }
    }

    /// <summary>
    /// リニア値をデシベルに変換
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        if (linear <= 0)
            return -80f;

        return Mathf.Log10(linear) * 20f;
    }

    /// <summary>
    /// Phase 7-1: TitleUI用のvolume getter
    /// </summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public float GetMusicVolume()
    {
        if (audioMixer != null && audioMixer.GetFloat("MusicVolume", out float musicDb))
        {
            // デシベルからリニア値に変換
            return Mathf.Pow(10, musicDb / 20f);
        }
        return 1.0f; // デフォルト値
    }

    public float GetVoiceVolume()
    {
        return voiceVolume;
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Test Card Hover")]
    public void TestCardHover()
    {
        PlayCardHover();
    }

    [ContextMenu("Test Card Pick")]
    public void TestCardPick()
    {
        PlayCardPick();
    }

    [ContextMenu("Test Heartbeat - Normal (10 cards)")]
    public void TestHeartbeatNormal()
    {
        PlayHeartbeat(0.5f, 10);
    }

    [ContextMenu("Test Heartbeat - Intense (3 cards)")]
    public void TestHeartbeatIntense()
    {
        PlayHeartbeat(0.7f, 3);
    }

    [ContextMenu("Test Heartbeat Crossfade (Normal → Intense)")]
    public void TestHeartbeatCrossfade()
    {
        StartCoroutine(TestCrossfadeSequence());
    }

    private IEnumerator TestCrossfadeSequence()
    {
        Debug.Log("[AudioManager] Starting Normal heartbeat...");
        PlayHeartbeat(0.5f, 10);
        yield return new WaitForSeconds(3f);

        Debug.Log("[AudioManager] Crossfading to Intense heartbeat...");
        PlayHeartbeat(0.7f, 3);
    }

    [ContextMenu("Stop Heartbeat")]
    public void TestStopHeartbeat()
    {
        StopHeartbeat();
    }
#endif
}
