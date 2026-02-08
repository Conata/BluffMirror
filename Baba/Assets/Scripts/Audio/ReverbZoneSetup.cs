using UnityEngine;

/// <summary>
/// 空間音響設定（Reverb Zone）
/// テーブル周辺のリバーブ効果を設定
/// </summary>
[RequireComponent(typeof(AudioReverbZone))]
public class ReverbZoneSetup : MonoBehaviour
{
    [Header("Reverb Configuration")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Room;

    [Header("Custom Reverb Settings")]
    [SerializeField] private float minDistance = 1.0f;
    [SerializeField] private float maxDistance = 5.0f;
    [SerializeField] private int room = -1000;
    [SerializeField] private int roomHF = -600;
    [SerializeField] private float decayTime = 1.4f;
    [SerializeField] private float decayHFRatio = 0.8f;
    [SerializeField] private int reflections = -200;
    [SerializeField] private float reflectionsDelay = 0.02f;
    [SerializeField] private int reverb = -200;
    [SerializeField] private float reverbDelay = 0.04f;
    [SerializeField] private float hfReference = 3000f;
    [SerializeField] private float lfReference = 200f;
    [SerializeField] private float diffusion = 100f;
    [SerializeField] private float density = 80f;

    private AudioReverbZone reverbZone;

    private void Awake()
    {
        reverbZone = GetComponent<AudioReverbZone>();
    }

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupReverbZone();
        }
    }

    /// <summary>
    /// Reverb Zoneの設定を適用
    /// </summary>
    public void SetupReverbZone()
    {
        // reverbZoneがnullの場合、再取得を試みる
        if (reverbZone == null)
        {
            reverbZone = GetComponent<AudioReverbZone>();
        }

        if (reverbZone == null)
        {
            Debug.LogError("[ReverbZoneSetup] AudioReverbZone component not found! Please add an AudioReverbZone component to this GameObject.");
            return;
        }

        // 基本設定
        reverbZone.minDistance = minDistance;
        reverbZone.maxDistance = maxDistance;

        // カスタムリバーブ設定を適用
        if (reverbPreset == AudioReverbPreset.User)
        {
            reverbZone.reverbPreset = AudioReverbPreset.User;
            ApplyCustomReverbSettings();
        }
        else
        {
            // プリセットを使用
            reverbZone.reverbPreset = reverbPreset;
        }

        Debug.Log($"[ReverbZoneSetup] Reverb zone configured with preset: {reverbPreset}");
    }

    /// <summary>
    /// カスタムリバーブ設定を適用
    /// 仕様書に基づく詳細設定
    /// </summary>
    private void ApplyCustomReverbSettings()
    {
        reverbZone.room = room;
        reverbZone.roomHF = roomHF;
        reverbZone.decayTime = decayTime;
        reverbZone.decayHFRatio = decayHFRatio;
        reverbZone.reflections = reflections;
        reverbZone.reflectionsDelay = reflectionsDelay;
        reverbZone.reverb = reverb;
        reverbZone.reverbDelay = reverbDelay;
        reverbZone.HFReference = hfReference;
        reverbZone.LFReference = lfReference;
        reverbZone.diffusion = diffusion;
        reverbZone.density = density;

        Debug.Log("[ReverbZoneSetup] Custom reverb settings applied.");
    }

    /// <summary>
    /// Audio Listener設定を適用
    /// </summary>
    public static void SetupAudioListener(AudioListener listener)
    {
        if (listener == null)
        {
            Debug.LogWarning("[ReverbZoneSetup] AudioListener is null.");
            return;
        }

        // Doppler Factor (デフォルト値を維持)
        // Unity 2023以降、AudioListenerのDopplerFactorはProjectSettings経由で設定

        Debug.Log("[ReverbZoneSetup] AudioListener configured.");
    }

    private void OnDrawGizmos()
    {
        if (reverbZone == null) return;

        // Min Distance の可視化
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Max Distance の可視化
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Room Preset")]
    public void ApplyRoomPreset()
    {
        reverbPreset = AudioReverbPreset.Room;
        SetupReverbZone();
    }

    [ContextMenu("Apply Custom Settings (Spec)")]
    public void ApplySpecSettings()
    {
        // 仕様書の設定を適用
        reverbPreset = AudioReverbPreset.User;
        minDistance = 1.0f;
        maxDistance = 5.0f;
        room = -1000;
        roomHF = -600;
        decayTime = 1.4f;
        decayHFRatio = 0.8f;
        reflections = -200;
        reflectionsDelay = 0.02f;
        reverb = -200;
        reverbDelay = 0.04f;
        hfReference = 3000f;
        lfReference = 200f;
        diffusion = 100f;
        density = 80f;

        SetupReverbZone();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
#endif
}
