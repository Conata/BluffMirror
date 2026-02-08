using UnityEngine;

/// <summary>
/// 旧カメラ制御（現在はCinemachineが制御するため無効化）
/// Cinemachine VCamのNoiseプロファイルで呼吸エフェクトを実現すること
/// </summary>
public class FPSCameraController : MonoBehaviour
{
    // CinemachineBrainと競合するため、このスクリプトは無効化
    // 呼吸エフェクト → CinemachineのBasicMultiChannelPerlinで代替
    // カメラシェイク → CinemachineImpulseで代替

    private void Awake()
    {
        // Cinemachineとの競合を防ぐため自動的に無効化
        Debug.LogWarning("[FPSCameraController] This component conflicts with CinemachineBrain. Disabling. Use Cinemachine Noise instead.");
        enabled = false;
    }
}
