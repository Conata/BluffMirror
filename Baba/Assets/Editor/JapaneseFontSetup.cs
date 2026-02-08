using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

/// <summary>
/// Editor utility to create/fix NotoSansJP SDF font asset for Japanese character support.
/// Deletes the old SDF asset and recreates it with Dynamic mode + 4096x4096 readable atlas.
/// Usage: Tools > Setup Japanese Font
/// </summary>
public class JapaneseFontSetup
{
    [MenuItem("Tools/Setup Japanese Font")]
    public static void SetupJapaneseFont()
    {
        string fontPath = "Assets/Fonts/NotoSansJP-VariableFont_wght.ttf";
        string sdfPath = "Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset";

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("Japanese Font Setup",
                $"Source font not found at:\n{fontPath}", "OK");
            return;
        }

        // Delete existing broken SDF asset
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath) != null)
        {
            AssetDatabase.DeleteAsset(sdfPath);
            Debug.Log("[JapaneseFontSetup] Deleted old SDF asset.");
        }

        // Create new Dynamic SDF font asset
        // Parameters: sourceFont, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight
        TMP_FontAsset newFontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, 36, 9,
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
            4096, 4096,
            AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true
        );

        if (newFontAsset == null)
        {
            EditorUtility.DisplayDialog("Japanese Font Setup",
                "Failed to create SDF font asset.", "OK");
            return;
        }

        // Save as asset
        AssetDatabase.CreateAsset(newFontAsset, sdfPath);

        // Save atlas texture as sub-asset
        if (newFontAsset.atlasTexture != null)
        {
            newFontAsset.atlasTexture.name = "NotoSansJP-VariableFont_wght SDF Atlas";
            AssetDatabase.AddObjectToAsset(newFontAsset.atlasTexture, newFontAsset);
        }

        // Save material as sub-asset
        if (newFontAsset.material != null)
        {
            newFontAsset.material.name = "NotoSansJP-VariableFont_wght SDF Material";
            AssetDatabase.AddObjectToAsset(newFontAsset.material, newFontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[JapaneseFontSetup] Created new Dynamic SDF font asset at {sdfPath}");

        // Configure as fallback
        ConfigureFallback(newFontAsset);

        EditorUtility.DisplayDialog("Japanese Font Setup",
            "NotoSansJP SDF font asset created:\n\n" +
            "- AtlasPopulationMode: Dynamic\n" +
            "- Atlas: 4096x4096 (readable)\n" +
            "- Render Mode: SDFAA\n" +
            "- Multi Atlas Support: enabled\n\n" +
            "Japanese characters will render dynamically at runtime.",
            "OK");
    }

    private static void ConfigureFallback(TMP_FontAsset japaneseSdf)
    {
        // Find LiberationSans SDF
        string[] sdfGuids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
        TMP_FontAsset liberationSans = null;

        foreach (string guid in sdfGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Fallback")) continue;
            liberationSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (liberationSans != null) break;
        }

        if (liberationSans != null)
        {
            if (liberationSans.fallbackFontAssetTable == null)
                liberationSans.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

            bool alreadyAdded = liberationSans.fallbackFontAssetTable.Any(f => f != null && f.name == japaneseSdf.name);
            if (!alreadyAdded)
            {
                liberationSans.fallbackFontAssetTable.Add(japaneseSdf);
                EditorUtility.SetDirty(liberationSans);
                AssetDatabase.SaveAssets();
                Debug.Log($"[JapaneseFontSetup] Added {japaneseSdf.name} as fallback to LiberationSans SDF.");
            }
        }

        // Also add to TMP Settings default fallback list
        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty fallbackList = so.FindProperty("m_fallbackFontAssets");

            bool inSettingsFallback = false;
            for (int i = 0; i < fallbackList.arraySize; i++)
            {
                if (fallbackList.GetArrayElementAtIndex(i).objectReferenceValue == japaneseSdf)
                {
                    inSettingsFallback = true;
                    break;
                }
            }

            if (!inSettingsFallback)
            {
                fallbackList.InsertArrayElementAtIndex(fallbackList.arraySize);
                fallbackList.GetArrayElementAtIndex(fallbackList.arraySize - 1).objectReferenceValue = japaneseSdf;
                so.ApplyModifiedProperties();
                Debug.Log($"[JapaneseFontSetup] Added {japaneseSdf.name} to TMP Settings fallback list.");
            }
        }
    }
}
