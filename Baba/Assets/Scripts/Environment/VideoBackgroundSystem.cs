using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 動画背景システム
/// VideoPlayerでMP4をRenderTextureに描画し、3面のQuad（後方・左・右）に投影する
/// </summary>
public class VideoBackgroundSystem : MonoBehaviour
{
    public static VideoBackgroundSystem Instance { get; private set; }

    [Header("Video Settings")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool playOnAwake = true;

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] private float brightness = 0.4f;
    [SerializeField] private Color tintColor = new Color(0.8f, 0.8f, 1f, 1f);

    [Header("Quad Settings")]
    [SerializeField] private float quadDistance = 7f;
    [SerializeField] private Vector2 quadSize = new Vector2(16f, 5f);

    [Header("RenderTexture Settings")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Material videoMaterial;
    private GameObject[] quads;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupRenderTexture();
        SetupVideoPlayer();
        SetupMaterial();
        CreateBackgroundQuads();

        if (playOnAwake && videoClip != null)
        {
            videoPlayer.Play();
        }

        Debug.Log("[VideoBackgroundSystem] Setup complete.");
    }

    private void SetupRenderTexture()
    {
        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.Create();
    }

    private void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.skipOnDrop = true;
    }

    private void SetupMaterial()
    {
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            Debug.LogError("[VideoBackgroundSystem] URP Unlit shader not found!");
            return;
        }

        videoMaterial = new Material(unlitShader);
        videoMaterial.mainTexture = renderTexture;
        videoMaterial.color = tintColor * brightness;
        videoMaterial.renderQueue = 1900;
    }

    private void CreateBackgroundQuads()
    {
        quads = new GameObject[3];

        // BackWall: 後方
        quads[0] = CreateQuad("BackWall",
            new Vector3(0f, quadSize.y * 0.5f - 1f, -quadDistance),
            Quaternion.identity);

        // LeftWall: 左
        quads[1] = CreateQuad("LeftWall",
            new Vector3(-quadDistance, quadSize.y * 0.5f - 1f, 0f),
            Quaternion.Euler(0f, 90f, 0f));

        // RightWall: 右
        quads[2] = CreateQuad("RightWall",
            new Vector3(quadDistance, quadSize.y * 0.5f - 1f, 0f),
            Quaternion.Euler(0f, -90f, 0f));
    }

    private GameObject CreateQuad(string quadName, Vector3 position, Quaternion rotation)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.transform.SetParent(transform);
        quad.transform.localPosition = position;
        quad.transform.localRotation = rotation;
        quad.transform.localScale = new Vector3(quadSize.x, quadSize.y, 1f);

        // Collider is unnecessary for background
        Destroy(quad.GetComponent<Collider>());

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.material = videoMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return quad;
    }

    /// <summary>
    /// brightness/tintColor 変更時にマテリアルを更新
    /// </summary>
    public void UpdateVisuals()
    {
        if (videoMaterial != null)
        {
            videoMaterial.color = tintColor * brightness;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoMaterial != null)
        {
            Destroy(videoMaterial);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && videoMaterial != null)
        {
            UpdateVisuals();
        }
    }
#endif
}
