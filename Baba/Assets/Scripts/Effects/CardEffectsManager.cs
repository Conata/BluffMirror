using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// カードのビジュアルエフェクト管理
/// パーティクル、グロー、消失演出など
/// </summary>
public class CardEffectsManager : MonoBehaviour
{
    public static CardEffectsManager Instance { get; private set; }

    [Header("Particle Prefabs")]
    [SerializeField] private ParticleSystem cardDisappearEffectPrefab;
    [SerializeField] private ParticleSystem cardHoverAuraPrefab;
    [SerializeField] private ParticleSystem cardPickEffectPrefab;

    [Header("Effect Settings")]
    [SerializeField] private Color glowColor = new Color(0.83f, 0.69f, 0.22f, 1f); // Gold
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private Color aiConsideringColor = new Color(0.3f, 0.9f, 1f, 0.6f); // Cyan

    [Header("Material Settings")]
    [SerializeField] private Material glowMaterial;
    private Material originalMaterial;

    // AI検討中グローパルスのトラッキング
    private Dictionary<CardObject, Tweener> activeGlowTweens = new Dictionary<CardObject, Tweener>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// カードペア消失演出
    /// </summary>
    /// <param name="card1">カード1</param>
    /// <param name="card2">カード2</param>
    /// <param name="onComplete">完了時コールバック</param>
    public IEnumerator PlayPairDisappearEffect(CardObject card1, CardObject card2, System.Action onComplete = null)
    {
        if (card1 == null || card2 == null)
        {
            Debug.LogWarning("[CardEffects] Cannot play disappear effect - cards are null.");
            onComplete?.Invoke();
            yield break;
        }

        // Phase 1: Glow Buildup (0.1s)
        yield return StartCoroutine(PlayGlowBuildup(card1, card2));

        // Phase 2: Dissolve Sparkles (0.3s)
        PlayDissolveSparkles(card1.transform.position);
        PlayDissolveSparkles(card2.transform.position);

        // カードを縮小
        card1.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        card2.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.3f);

        // Phase 3: Final Flash (0.1s)
        PlayFinalFlash(card1.transform.position);
        PlayFinalFlash(card2.transform.position);

        yield return new WaitForSeconds(0.1f);

        onComplete?.Invoke();

        Debug.Log("[CardEffects] Pair disappear effect completed.");
    }

    /// <summary>
    /// Phase 1: グロービルドアップ
    /// </summary>
    private IEnumerator PlayGlowBuildup(CardObject card1, CardObject card2)
    {
        // カードにグローマテリアルを適用（疑似的）
        // 実際にはEmissionを使用
        ApplyGlowToCard(card1, glowIntensity);
        ApplyGlowToCard(card2, glowIntensity);

        yield return new WaitForSeconds(0.1f);

        RemoveGlowFromCard(card1);
        RemoveGlowFromCard(card2);
    }

    /// <summary>
    /// Phase 2: 溶解スパークル
    /// </summary>
    private void PlayDissolveSparkles(Vector3 position)
    {
        if (cardDisappearEffectPrefab == null)
        {
            // プリファブが無い場合、プロシージャル生成
            CreateDissolveSparkleProcedurally(position);
            return;
        }

        ParticleSystem effect = Instantiate(cardDisappearEffectPrefab, position, Quaternion.identity);
        Destroy(effect.gameObject, 2f);
    }

    /// <summary>
    /// プロシージャルなスパークルエフェクト生成
    /// </summary>
    private void CreateDissolveSparkleProcedurally(Vector3 position)
    {
        GameObject particleObj = new GameObject("DissolveSparkles");
        particleObj.transform.position = position;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // URP 互換マテリアルを設定
        ParticleSystemRenderer renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.5f;
        main.startSize = 0.01f;
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.maxParticles = 200;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 200f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(glowColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ps.Play();
        Destroy(particleObj, 2f);
    }

    /// <summary>
    /// Phase 3: 最終フラッシュ
    /// </summary>
    private void PlayFinalFlash(Vector3 position)
    {
        GameObject flashObj = new GameObject("FinalFlash");
        flashObj.transform.position = position;

        ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();

        // URP 互換マテリアルを設定
        ParticleSystemRenderer renderer = flashObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.startLifetime = 0.1f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = Color.white;
        main.maxParticles = 20;
        main.loop = false;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ps.Play();
        Destroy(flashObj, 1f);
    }

    /// <summary>
    /// カードホバー時のオーラエフェクト
    /// </summary>
    /// <param name="card">対象カード</param>
    public ParticleSystem PlayHoverAura(CardObject card)
    {
        if (card == null) return null;

        if (cardHoverAuraPrefab == null)
        {
            // プロシージャル生成
            return CreateHoverAuraProcedurally(card.transform);
        }

        ParticleSystem aura = Instantiate(cardHoverAuraPrefab, card.transform);
        return aura;
    }

    /// <summary>
    /// プロシージャルなホバーオーラ生成
    /// </summary>
    private ParticleSystem CreateHoverAuraProcedurally(Transform parent)
    {
        GameObject auraObj = new GameObject("HoverAura");
        auraObj.transform.SetParent(parent);
        auraObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = auraObj.AddComponent<ParticleSystem>();

        // URP 互換マテリアルを設定
        ParticleSystemRenderer renderer = auraObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.05f;
        main.startSize = 0.01f;
        main.startColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
        main.maxParticles = 50;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.063f, 0.088f, 0.01f); // Card size

        return ps;
    }

    /// <summary>
    /// ホバーオーラを停止
    /// </summary>
    /// <param name="aura">停止するオーラ</param>
    public void StopHoverAura(ParticleSystem aura)
    {
        if (aura == null) return;

        aura.Stop();
        Destroy(aura.gameObject, 1f);
    }

    /// <summary>
    /// Stage 6: AI思考中のカードオーラ（シアン色）+ パルスアウトライングロー
    /// </summary>
    /// <param name="card">対象カード</param>
    /// <returns>生成されたオーラのParticleSystem</returns>
    public ParticleSystem PlayAIConsideringAura(CardObject card)
    {
        if (card == null) return null;

        ParticleSystem aura;

        if (cardHoverAuraPrefab == null)
        {
            // プロシージャル生成
            aura = CreateAIConsideringAuraProcedurally(card.transform);
        }
        else
        {
            // Prefabを使用
            aura = Instantiate(cardHoverAuraPrefab, card.transform);
            var main = aura.main;
            main.startColor = aiConsideringColor;
        }

        // パルスアウトライングロー開始
        StartPulsingGlow(card);

        return aura;
    }

    /// <summary>
    /// パルスアニメーション付きアウトライングロー開始
    /// </summary>
    private void StartPulsingGlow(CardObject card)
    {
        if (card == null || card.cardRenderer == null) return;

        // 既存のグローTweenを停止
        StopPulsingGlow(card);

        Material mat = card.cardRenderer.material;
        if (!mat.HasProperty("_EmissionColor")) return;

        mat.EnableKeyword("_EMISSION");

        // DOTweenでEmission強度をパルスさせる（0.3x ～ 1.0x）
        float pulseValue = 0.3f;
        Tweener tweener = DOTween.To(
            () => pulseValue,
            x =>
            {
                pulseValue = x;
                if (card != null && card.cardRenderer != null)
                {
                    card.cardRenderer.material.SetColor("_EmissionColor", aiConsideringColor * (0.4f * x));
                }
            },
            1.0f,
            0.6f
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);

        activeGlowTweens[card] = tweener;
    }

    /// <summary>
    /// パルスグローを停止してEmissionをリセット
    /// </summary>
    private void StopPulsingGlow(CardObject card)
    {
        if (card == null) return;

        if (activeGlowTweens.TryGetValue(card, out Tweener tweener))
        {
            tweener?.Kill();
            activeGlowTweens.Remove(card);
        }

        RemoveGlowFromCard(card);
    }

    /// <summary>
    /// Stage 6: AI検討中オーラを停止
    /// </summary>
    /// <param name="aura">停止するオーラ</param>
    /// <param name="card">対象カード（グロー除去用）</param>
    public void StopAIConsideringAura(ParticleSystem aura, CardObject card)
    {
        if (aura != null)
        {
            aura.Stop();
            Destroy(aura.gameObject, 1f);
        }

        if (card != null)
        {
            StopPulsingGlow(card);
        }
    }

    /// <summary>
    /// Stage 6: プロシージャルなAI検討中オーラ生成
    /// </summary>
    private ParticleSystem CreateAIConsideringAuraProcedurally(Transform parent)
    {
        GameObject auraObj = new GameObject("AIConsideringAura");
        auraObj.transform.SetParent(parent);
        auraObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = auraObj.AddComponent<ParticleSystem>();

        // URP 互換マテリアルを設定
        ParticleSystemRenderer renderer = auraObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));

        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.05f;
        main.startSize = 0.01f;
        main.startColor = aiConsideringColor;
        main.maxParticles = 50;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.063f, 0.088f, 0.01f); // Card size

        return ps;
    }

    /// <summary>
    /// Stage 6: AI検討中のシアングローを適用
    /// </summary>
    private void ApplyAIConsideringGlow(CardObject card, float intensity)
    {
        if (card == null || card.cardRenderer == null) return;

        Material mat = card.cardRenderer.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", aiConsideringColor * intensity);
        }
    }

    /// <summary>
    /// カードにグロー効果を適用
    /// </summary>
    private void ApplyGlowToCard(CardObject card, float intensity)
    {
        if (card == null || card.cardRenderer == null) return;

        Material mat = card.cardRenderer.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * intensity);
        }
    }

    /// <summary>
    /// カードからグロー効果を除去
    /// </summary>
    private void RemoveGlowFromCard(CardObject card)
    {
        if (card == null || card.cardRenderer == null) return;

        Material mat = card.cardRenderer.material;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Dissolve Sparkles")]
    public void TestDissolveSparkles()
    {
        PlayDissolveSparkles(Vector3.zero);
    }

    [ContextMenu("Test Final Flash")]
    public void TestFinalFlash()
    {
        PlayFinalFlash(Vector3.zero);
    }

    [ContextMenu("Test AI Considering Aura")]
    public void TestAIConsideringAura()
    {
        var card = FindFirstObjectByType<CardObject>();
        if (card != null)
        {
            PlayAIConsideringAura(card);
            Debug.Log("[CardEffects] AI Considering Aura test started on: " + card.name);
        }
        else
        {
            Debug.LogWarning("[CardEffects] No CardObject found in scene for testing.");
        }
    }
#endif
}
