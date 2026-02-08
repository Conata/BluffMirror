using UnityEngine;

/// <summary>
/// マテリアルのプロパティ設定
/// 仕様書04-Art-Sound-Specification.mdに基づく
/// </summary>
public class MaterialSetup : MonoBehaviour
{
    [Header("Material References")]
    [SerializeField] private Material tableFeltMaterial;
    [SerializeField] private Material cardFrontMaterial;
    [SerializeField] private Material cardBackMaterial;
    [SerializeField] private Material aiMaskMaterial;
    [SerializeField] private Material floorMaterial;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupAllMaterials();
        }
    }

    /// <summary>
    /// 全マテリアルをセットアップ
    /// </summary>
    public void SetupAllMaterials()
    {
        SetupTableFelt();
        SetupCardFront();
        SetupCardBack();
        SetupAIMask();
        SetupFloor();

        Debug.Log("[MaterialSetup] All materials configured according to specifications.");
    }

    /// <summary>
    /// テーブル（フェルト）マテリアル設定
    /// </summary>
    private void SetupTableFelt()
    {
        if (tableFeltMaterial == null)
        {
            Debug.LogWarning("[MaterialSetup] Table Felt material is not assigned.");
            return;
        }

        // Albedo: Deep Green (#1B3B1B)
        tableFeltMaterial.SetColor("_BaseColor", HexToColor("#1B3B1B"));

        // Metallic & Smoothness
        tableFeltMaterial.SetFloat("_Metallic", 0.0f);
        tableFeltMaterial.SetFloat("_Smoothness", 0.15f);

        // Tiling
        tableFeltMaterial.SetTextureScale("_BaseMap", new Vector2(2f, 2f));

        Debug.Log("[MaterialSetup] Table Felt material configured.");
    }

    /// <summary>
    /// カード（表面）マテリアル設定
    /// </summary>
    private void SetupCardFront()
    {
        if (cardFrontMaterial == null)
        {
            Debug.LogWarning("[MaterialSetup] Card Front material is not assigned.");
            return;
        }

        // Metallic & Smoothness
        cardFrontMaterial.SetFloat("_Metallic", 0.1f);
        cardFrontMaterial.SetFloat("_Smoothness", 0.65f);

        // カードの色は Card Texture Atlas で設定されるため、ここでは設定しない
        // 基本色を白に設定（テクスチャの色をそのまま反映）
        cardFrontMaterial.SetColor("_BaseColor", Color.white);

        Debug.Log("[MaterialSetup] Card Front material configured.");
    }

    /// <summary>
    /// カード（裏面）マテリアル設定
    /// </summary>
    private void SetupCardBack()
    {
        if (cardBackMaterial == null)
        {
            Debug.LogWarning("[MaterialSetup] Card Back material is not assigned.");
            return;
        }

        // Albedo: Classic Pattern (#000080 base)
        cardBackMaterial.SetColor("_BaseColor", HexToColor("#000080"));

        // Metallic & Smoothness
        cardBackMaterial.SetFloat("_Metallic", 0.05f);
        cardBackMaterial.SetFloat("_Smoothness", 0.7f);

        Debug.Log("[MaterialSetup] Card Back material configured.");
    }

    /// <summary>
    /// AI顔部分（仮面）マテリアル設定
    /// </summary>
    private void SetupAIMask()
    {
        if (aiMaskMaterial == null)
        {
            Debug.LogWarning("[MaterialSetup] AI Mask material is not assigned. Skipping.");
            return;
        }

        // Albedo: Dark Metal (#2F2F2F)
        aiMaskMaterial.SetColor("_BaseColor", HexToColor("#2F2F2F"));

        // Metallic & Smoothness
        aiMaskMaterial.SetFloat("_Metallic", 0.8f);
        aiMaskMaterial.SetFloat("_Smoothness", 0.9f);

        // Emission: Eyes Glow (#FF0000, Intensity: 0.5)
        aiMaskMaterial.EnableKeyword("_EMISSION");
        aiMaskMaterial.SetColor("_EmissionColor", HexToColor("#FF0000") * 0.5f);

        Debug.Log("[MaterialSetup] AI Mask material configured.");
    }

    /// <summary>
    /// 床マテリアル設定
    /// </summary>
    private void SetupFloor()
    {
        if (floorMaterial == null)
        {
            Debug.LogWarning("[MaterialSetup] Floor material is not assigned. Skipping.");
            return;
        }

        // Albedo: Dark Brown (#3C2B1C)
        floorMaterial.SetColor("_BaseColor", HexToColor("#0D0129"));

        // Metallic & Smoothness
        floorMaterial.SetFloat("_Metallic", 0.0f);
        floorMaterial.SetFloat("_Smoothness", 0.3f);

        Debug.Log("[MaterialSetup] Floor material configured.");
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

        Debug.LogWarning($"[MaterialSetup] Invalid hex color: {hex}. Using white.");
        return Color.white;
    }

#if UNITY_EDITOR
    [ContextMenu("Setup All Materials")]
    public void SetupAllMaterialsEditor()
    {
        SetupAllMaterials();
        UnityEditor.EditorUtility.SetDirty(tableFeltMaterial);
        UnityEditor.EditorUtility.SetDirty(cardFrontMaterial);
        UnityEditor.EditorUtility.SetDirty(cardBackMaterial);
        if (aiMaskMaterial != null) UnityEditor.EditorUtility.SetDirty(aiMaskMaterial);
        if (floorMaterial != null) UnityEditor.EditorUtility.SetDirty(floorMaterial);

        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("[MaterialSetup] Materials saved to disk.");
    }

    [ContextMenu("Find Materials in Scene")]
    public void FindMaterialsInScene()
    {
        // テーブルのマテリアルを探す
        GameObject table = GameObject.Find("Table");
        if (table != null)
        {
            Renderer renderer = table.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                tableFeltMaterial = renderer.sharedMaterial;
                Debug.Log($"[MaterialSetup] Found Table material: {tableFeltMaterial.name}");
            }
        }

        // カードのマテリアルを探す
        CardObject[] cards = FindObjectsByType<CardObject>(FindObjectsSortMode.None);
        if (cards.Length > 0)
        {
            if (cards[0].frontMaterial != null)
            {
                cardFrontMaterial = cards[0].frontMaterial;
                Debug.Log($"[MaterialSetup] Found Card Front material: {cardFrontMaterial.name}");
            }
            if (cards[0].backMaterial != null)
            {
                cardBackMaterial = cards[0].backMaterial;
                Debug.Log($"[MaterialSetup] Found Card Back material: {cardBackMaterial.name}");
            }
        }
    }
#endif
}
