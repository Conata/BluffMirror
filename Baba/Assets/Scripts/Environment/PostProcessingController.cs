using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// ポストプロセシング効果の管理
/// 心理圧演出、カメラエフェクト制御
/// </summary>
[RequireComponent(typeof(Volume))]
public class PostProcessingController : MonoBehaviour
{
    public static PostProcessingController Instance { get; private set; }

    [Header("Volume Components")]
    private Volume volume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private DepthOfField depthOfField;
    private LensDistortion lensDistortion;

    [Header("Base Settings")]
    [SerializeField] private float baseVignetteIntensity = 0.5f;
    [SerializeField] private float baseChromaticIntensity = 0.0f;
    [SerializeField] private float baseFilmGrainIntensity = 0.1f;

    [Header("Pressure Effect Settings")]
    [SerializeField] private float pressureVignetteIntensity = 0.7f;
    [SerializeField] private float pressureChromaticIntensity = 0.3f;
    [SerializeField] private float pressureFilmGrainIntensity = 0.3f;
    [SerializeField] private float pressureSaturationShift = -30f;
    [SerializeField] private float pressureContrastShift = 20f;

    [Header("Focus Effect Settings")]
    [SerializeField] private float focusDoFIntensity = 5f;
    [SerializeField] private float focusDuration = 0.3f;

    private Tween currentFocusTween;
    private Coroutine currentPressureCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeVolume();
    }

    private void Start()
    {
        ResetToBaseSettings();
    }

    /// <summary>
    /// Volume コンポーネントの初期化
    /// </summary>
    private void InitializeVolume()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            volume = gameObject.AddComponent<Volume>();
        }

        // プロファイルが無ければ作成
        if (volume.profile == null)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // 各エフェクトコンポーネントを取得または作成
        if (!volume.profile.TryGet(out vignette))
        {
            vignette = volume.profile.Add<Vignette>(true);
        }

        if (!volume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = volume.profile.Add<ChromaticAberration>(true);
        }

        if (!volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
        }

        if (!volume.profile.TryGet(out filmGrain))
        {
            filmGrain = volume.profile.Add<FilmGrain>(true);
        }

        if (!volume.profile.TryGet(out depthOfField))
        {
            depthOfField = volume.profile.Add<DepthOfField>(true);
            depthOfField.active = false; // デフォルトでOFF
        }

        if (!volume.profile.TryGet(out lensDistortion))
        {
            lensDistortion = volume.profile.Add<LensDistortion>(true);
        }

        Debug.Log("[PostProcessing] Volume initialized.");
    }

    /// <summary>
    /// ベース設定にリセット
    /// </summary>
    public void ResetToBaseSettings()
    {
        if (vignette != null)
        {
            vignette.intensity.value = baseVignetteIntensity;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = baseChromaticIntensity;
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = baseFilmGrainIntensity;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0f;
            colorAdjustments.contrast.value = 0f;
            colorAdjustments.saturation.value = 0f;
        }

        if (depthOfField != null)
        {
            depthOfField.active = false;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0f;
        }

        Debug.Log("[PostProcessing] Reset to base settings.");
    }

    /// <summary>
    /// 心理圧演出を適用
    /// </summary>
    /// <param name="intensity">圧力の強さ (0-1)</param>
    /// <param name="duration">効果の適用時間</param>
    public void ApplyPressureEffect(float intensity, float duration = 0.5f)
    {
        intensity = Mathf.Clamp01(intensity);

        // 既存のコルーチンをキャンセル
        if (currentPressureCoroutine != null)
        {
            StopCoroutine(currentPressureCoroutine);
        }

        // 目標値の計算
        float targetVignette = Mathf.Lerp(baseVignetteIntensity, pressureVignetteIntensity, intensity);
        float targetChromatic = Mathf.Lerp(baseChromaticIntensity, pressureChromaticIntensity, intensity);
        float targetGrain = Mathf.Lerp(baseFilmGrainIntensity, pressureFilmGrainIntensity, intensity);
        float targetSaturation = Mathf.Lerp(0f, pressureSaturationShift, intensity);
        float targetContrast = Mathf.Lerp(0f, pressureContrastShift, intensity);

        // コルーチンでスムーズにアニメーション
        currentPressureCoroutine = StartCoroutine(AnimatePressureEffect(targetVignette, targetChromatic, targetGrain, targetSaturation, targetContrast, duration));

        Debug.Log($"[PostProcessing] Pressure effect applied: {intensity * 100}%");
    }

    /// <summary>
    /// 心理圧エフェクトをアニメーション
    /// </summary>
    private IEnumerator AnimatePressureEffect(float targetVignette, float targetChromatic, float targetGrain, float targetSaturation, float targetContrast, float duration)
    {
        // 開始値を保存
        float startVignette = vignette != null ? vignette.intensity.value : 0f;
        float startChromatic = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float startGrain = filmGrain != null ? filmGrain.intensity.value : 0f;
        float startSaturation = colorAdjustments != null ? colorAdjustments.saturation.value : 0f;
        float startContrast = colorAdjustments != null ? colorAdjustments.contrast.value : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // EaseOutQuadカーブを適用
            float easedT = 1f - (1f - t) * (1f - t);

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, easedT);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = Mathf.Lerp(startChromatic, targetChromatic, easedT);
            }

            if (filmGrain != null)
            {
                filmGrain.intensity.value = Mathf.Lerp(startGrain, targetGrain, easedT);
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, easedT);
                colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetContrast, easedT);
            }

            yield return null;
        }

        // 最終値を確実に設定
        if (vignette != null) vignette.intensity.value = targetVignette;
        if (chromaticAberration != null) chromaticAberration.intensity.value = targetChromatic;
        if (filmGrain != null) filmGrain.intensity.value = targetGrain;
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = targetSaturation;
            colorAdjustments.contrast.value = targetContrast;
        }
    }

    /// <summary>
    /// 心理圧演出を解除
    /// </summary>
    /// <param name="duration">解除のフェード時間</param>
    public void ReleasePressureEffect(float duration = 1.0f)
    {
        ApplyPressureEffect(0f, duration);
        Debug.Log("[PostProcessing] Pressure effect released.");
    }

    /// <summary>
    /// カードドロー時のフォーカス効果
    /// </summary>
    public void ApplyFocusEffect()
    {
        if (depthOfField == null) return;

        currentFocusTween?.Kill();

        depthOfField.active = true;
        depthOfField.focusDistance.value = focusDoFIntensity;

        // フォーカス → 解除のループ
        currentFocusTween = DOTween.Sequence()
            .Append(DOTween.To(() => depthOfField.focusDistance.value, x => depthOfField.focusDistance.value = x, 2f, focusDuration))
            .Append(DOTween.To(() => depthOfField.focusDistance.value, x => depthOfField.focusDistance.value = x, focusDoFIntensity, focusDuration))
            .OnComplete(() => depthOfField.active = false);

        Debug.Log("[PostProcessing] Focus effect applied.");
    }

    /// <summary>
    /// 画面シェイク効果（強い心理圧時）
    /// </summary>
    /// <param name="intensity">シェイクの強さ</param>
    /// <param name="duration">シェイクの時間</param>
    public void ShakeScreen(float intensity, float duration)
    {
        // カメラシェイクは FPSCameraController で実装
        Camera.main?.transform.DOShakePosition(duration, intensity, 30, 90, false, true);

        Debug.Log($"[PostProcessing] Screen shake: intensity={intensity}, duration={duration}");
    }

    /// <summary>
    /// カラーフラッシュ効果（重要イベント時）
    /// </summary>
    /// <param name="color">フラッシュの色</param>
    /// <param name="intensity">フラッシュの強さ</param>
    /// <param name="duration">フラッシュの持続時間</param>
    public void FlashColor(Color color, float intensity, float duration)
    {
        if (colorAdjustments == null) return;

        float originalExposure = colorAdjustments.postExposure.value;

        DOTween.Sequence()
            .Append(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, intensity, duration * 0.3f))
            .Append(DOTween.To(() => colorAdjustments.postExposure.value, x => colorAdjustments.postExposure.value = x, originalExposure, duration * 0.7f));

        Debug.Log($"[PostProcessing] Color flash: {color}, intensity={intensity}");
    }

    /// <summary>
    /// ダイアログ表示時の圧力ティア視覚効果
    /// 0-1: Whisper (vignetteパルス)
    /// 1-2: Projection (vignette + chromatic aberration)
    /// 2-3: Distortion (film grain + lens distortion + screen shake)
    /// </summary>
    public void ApplyDialogueVisualEffect(float pressureLevel, float duration = 1.5f)
    {
        if (pressureLevel < 1.0f)
        {
            // === Whisper tier: subtle vignette pulse ===
            float t = pressureLevel;
            float peakVignette = Mathf.Lerp(baseVignetteIntensity + 0.05f, baseVignetteIntensity + 0.15f, t);

            if (vignette != null)
            {
                DOTween.Sequence()
                    .Append(DOTween.To(() => vignette.intensity.value,
                        x => vignette.intensity.value = x, peakVignette, duration * 0.4f)
                        .SetEase(Ease.InOutSine))
                    .Append(DOTween.To(() => vignette.intensity.value,
                        x => vignette.intensity.value = x, baseVignetteIntensity, duration * 0.6f)
                        .SetEase(Ease.InOutSine));
            }
        }
        else if (pressureLevel < 2.0f)
        {
            // === Projection tier: vignette + chromatic aberration ===
            float t = pressureLevel - 1.0f;

            if (vignette != null)
            {
                float peakVignette = Mathf.Lerp(0.35f, 0.45f, t);
                DOTween.Sequence()
                    .Append(DOTween.To(() => vignette.intensity.value,
                        x => vignette.intensity.value = x, peakVignette, duration * 0.3f))
                    .Append(DOTween.To(() => vignette.intensity.value,
                        x => vignette.intensity.value = x, baseVignetteIntensity, duration * 0.7f));
            }

            if (chromaticAberration != null)
            {
                float peakChromatic = Mathf.Lerp(0.05f, 0.15f, t);
                DOTween.Sequence()
                    .Append(DOTween.To(() => chromaticAberration.intensity.value,
                        x => chromaticAberration.intensity.value = x, peakChromatic, duration * 0.3f))
                    .Append(DOTween.To(() => chromaticAberration.intensity.value,
                        x => chromaticAberration.intensity.value = x, baseChromaticIntensity, duration * 0.7f));
            }
        }
        else
        {
            // === Distortion tier: film grain + lens distortion + screen shake ===
            float t = Mathf.Clamp01(pressureLevel - 2.0f);
            float peakDuration = 0.5f;
            float recoveryDuration = Mathf.Max(duration - peakDuration * 2, 0.3f);

            if (filmGrain != null)
            {
                float peakGrain = Mathf.Lerp(baseFilmGrainIntensity + 0.15f, baseFilmGrainIntensity + 0.3f, t);
                DOTween.Sequence()
                    .Append(DOTween.To(() => filmGrain.intensity.value,
                        x => filmGrain.intensity.value = x, peakGrain, peakDuration))
                    .AppendInterval(peakDuration)
                    .Append(DOTween.To(() => filmGrain.intensity.value,
                        x => filmGrain.intensity.value = x, baseFilmGrainIntensity, recoveryDuration));
            }

            if (lensDistortion != null)
            {
                float peakDistortion = Mathf.Lerp(0.1f, 0.2f, t);
                DOTween.Sequence()
                    .Append(DOTween.To(() => lensDistortion.intensity.value,
                        x => lensDistortion.intensity.value = x, peakDistortion, peakDuration))
                    .AppendInterval(peakDuration)
                    .Append(DOTween.To(() => lensDistortion.intensity.value,
                        x => lensDistortion.intensity.value = x, 0f, recoveryDuration));
            }

            float shakeIntensity = Mathf.Lerp(0.01f, 0.02f, t);
            ShakeScreen(shakeIntensity, peakDuration);
        }

        Debug.Log($"[PostProcessing] Dialogue visual effect: pressure={pressureLevel:F2}, tier={(pressureLevel < 1f ? "Whisper" : pressureLevel < 2f ? "Projection" : "Distortion")}");
    }

    private void OnDestroy()
    {
        if (currentPressureCoroutine != null)
        {
            StopCoroutine(currentPressureCoroutine);
        }
        currentFocusTween?.Kill();
    }

#if UNITY_EDITOR
    [ContextMenu("Test Pressure Effect (50%)")]
    public void TestPressure50()
    {
        ApplyPressureEffect(0.5f, 0.5f);
    }

    [ContextMenu("Test Pressure Effect (100%)")]
    public void TestPressure100()
    {
        ApplyPressureEffect(1f, 0.5f);
    }

    [ContextMenu("Release Pressure Effect")]
    public void TestRelease()
    {
        ReleasePressureEffect(1f);
    }

    [ContextMenu("Test Focus Effect")]
    public void TestFocus()
    {
        ApplyFocusEffect();
    }

    [ContextMenu("Test Direct Value Set (No Animation)")]
    public void TestDirectValueSet()
    {
        Debug.Log("=== Testing Direct Value Assignment ===");

        if (vignette != null)
        {
            vignette.intensity.value = 0.65f;
            Debug.Log($"Vignette intensity set to: {vignette.intensity.value}");
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0.3f;
            Debug.Log($"Chromatic Aberration intensity set to: {chromaticAberration.intensity.value}");
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = 0.3f;
            Debug.Log($"Film Grain intensity set to: {filmGrain.intensity.value}");
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = -30f;
            colorAdjustments.contrast.value = 20f;
            Debug.Log($"Color Adjustments - Saturation: {colorAdjustments.saturation.value}, Contrast: {colorAdjustments.contrast.value}");
        }

        Debug.Log("=== Direct values set - Check if visual effect appears ===");
    }

    [ContextMenu("Check Post-processing Setup")]
    public void CheckSetup()
    {
        Debug.Log("=== Post-processing Setup Check ===");

        // Volume check
        if (volume != null)
        {
            Debug.Log($"✓ Volume found. Is Global: {volume.isGlobal}, Priority: {volume.priority}");
            Debug.Log($"  Profile assigned: {(volume.profile != null ? "Yes" : "No")}");

            // Check current values
            if (vignette != null)
            {
                Debug.Log($"  Current Vignette intensity: {vignette.intensity.value}");
            }
            if (chromaticAberration != null)
            {
                Debug.Log($"  Current Chromatic Aberration intensity: {chromaticAberration.intensity.value}");
            }
        }
        else
        {
            Debug.LogError("✗ Volume component not found!");
        }

        // Effects check
        Debug.Log($"  Vignette: {(vignette != null ? "OK" : "Missing")}");
        Debug.Log($"  Chromatic Aberration: {(chromaticAberration != null ? "OK" : "Missing")}");
        Debug.Log($"  Color Adjustments: {(colorAdjustments != null ? "OK" : "Missing")}");
        Debug.Log($"  Film Grain: {(filmGrain != null ? "OK" : "Missing")}");
        Debug.Log($"  Depth of Field: {(depthOfField != null ? "OK" : "Missing")}");
        Debug.Log($"  Lens Distortion: {(lensDistortion != null ? "OK" : "Missing")}");

        // Camera check
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var cameraData = mainCam.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                Debug.Log($"✓ Camera post-processing: {(cameraData.renderPostProcessing ? "Enabled" : "Disabled")}");
            }
        }
        else
        {
            Debug.LogWarning("Main Camera not found!");
        }

        Debug.Log("=================================");
    }
#endif
}
