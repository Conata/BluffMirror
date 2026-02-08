using UnityEngine;

/// <summary>
/// 3点照明システムの自動セットアップ
/// 仕様書04-Art-Sound-Specification.mdに基づく
/// </summary>
public class LightingSetup : MonoBehaviour
{
    [Header("Lighting Configuration")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Light References")]
    [SerializeField] private Light keyLight;
    [SerializeField] private Light fillLight;
    [SerializeField] private Light rimLight;

    [Header("Environment Settings")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogColor = new Color(0.102f, 0.102f, 0.180f, 1f); // #1A1A2E
    [SerializeField] private float fogDensity = 0.06f;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupLighting();
            SetupEnvironment();
        }
    }

    /// <summary>
    /// 3点照明システムのセットアップ
    /// </summary>
    public void SetupLighting()
    {
        SetupKeyLight();
        SetupFillLight();
        SetupRimLight();
        SetupAmbientLighting();

        Debug.Log("[LightingSetup] 3-point lighting system configured successfully.");
    }

    /// <summary>
    /// Key Light (主光源) - Warm Orange Spot Light
    /// Position: (0.8, 2.8, -1.2), Rotation: (45, -30, 0)
    /// </summary>
    private void SetupKeyLight()
    {
        if (keyLight == null)
        {
            GameObject keyLightObj = new GameObject("KeyLight");
            keyLight = keyLightObj.AddComponent<Light>();
        }

        keyLight.type = LightType.Spot;
        keyLight.color = HexToColor("#FF8C42"); // Warm Orange
        keyLight.intensity = 2.0f;
        keyLight.spotAngle = 35f;
        keyLight.innerSpotAngle = 21f; // Penumbra 0.6 approximation
        keyLight.range = 10f;
        keyLight.shadows = LightShadows.Hard;
        keyLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High; // 2048

        keyLight.transform.position = new Vector3(0.8f, 2.8f, -1.2f);
        keyLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        Debug.Log("[LightingSetup] Key Light configured.");
    }

    /// <summary>
    /// Fill Light (環境光) - Cool Blue Area Light
    /// Position: (-0.6, 1.6, 1.6)
    /// </summary>
    private void SetupFillLight()
    {
        if (fillLight == null)
        {
            GameObject fillLightObj = new GameObject("FillLight");
            fillLight = fillLightObj.AddComponent<Light>();
        }

        // Unity's built-in Area Light is only available in baked lighting
        // Using Point Light as alternative for realtime
        fillLight.type = LightType.Point;
        fillLight.color = HexToColor("#6495ED"); // Cool Blue
        fillLight.intensity = 0.4f;
        fillLight.range = 4.0f;
        fillLight.shadows = LightShadows.None;

        fillLight.transform.position = new Vector3(-0.6f, 1.6f, 1.6f);

        Debug.Log("[LightingSetup] Fill Light configured.");
    }

    /// <summary>
    /// Rim Light (輪郭光) - Warm Gold Point Light
    /// Position: (0, 2.2, -2.2)
    /// </summary>
    private void SetupRimLight()
    {
        if (rimLight == null)
        {
            GameObject rimLightObj = new GameObject("RimLight");
            rimLight = rimLightObj.AddComponent<Light>();
        }

        rimLight.type = LightType.Point;
        rimLight.color = HexToColor("#D4AF37"); // Warm Gold
        rimLight.intensity = 0.8f;
        rimLight.range = 5.0f;
        rimLight.shadows = LightShadows.None;

        rimLight.transform.position = new Vector3(0f, 2.2f, -2.2f);

        Debug.Log("[LightingSetup] Rim Light configured.");
    }

    /// <summary>
    /// アンビエントライティング設定
    /// </summary>
    private void SetupAmbientLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = HexToColor("#2F2F3F");
        RenderSettings.ambientEquatorColor = HexToColor("#1F1F2F");
        RenderSettings.ambientGroundColor = HexToColor("#0F0F1F");

        Debug.Log("[LightingSetup] Ambient lighting configured.");
    }

    /// <summary>
    /// 環境設定（Fog等）
    /// </summary>
    private void SetupEnvironment()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        Debug.Log($"[LightingSetup] Environment configured. Fog: {enableFog}");
    }

    /// <summary>
    /// Hex color code を Unity Color に変換
    /// </summary>
    private Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");

        if (hex.Length == 6)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        Debug.LogWarning($"[LightingSetup] Invalid hex color: {hex}. Using white.");
        return Color.white;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Key Light gizmo
        if (keyLight != null)
        {
            Gizmos.color = new Color(1f, 0.55f, 0.26f, 0.5f); // Warm Orange
            Gizmos.DrawWireSphere(keyLight.transform.position, 0.2f);
            Gizmos.DrawLine(keyLight.transform.position, keyLight.transform.position + keyLight.transform.forward * 2f);
        }

        // Fill Light gizmo
        if (fillLight != null)
        {
            Gizmos.color = new Color(0.39f, 0.58f, 0.93f, 0.5f); // Cool Blue
            Gizmos.DrawWireSphere(fillLight.transform.position, 0.15f);
        }

        // Rim Light gizmo
        if (rimLight != null)
        {
            Gizmos.color = new Color(0.83f, 0.69f, 0.22f, 0.5f); // Warm Gold
            Gizmos.DrawWireSphere(rimLight.transform.position, 0.15f);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup All Lighting")]
    public void SetupAllLighting()
    {
        SetupLighting();
        SetupEnvironment();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    [ContextMenu("Reset to Default")]
    public void ResetToDefault()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.fog = false;
        Debug.Log("[LightingSetup] Reset to default settings.");
    }
#endif
}
