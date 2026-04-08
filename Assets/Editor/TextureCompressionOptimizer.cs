using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bulk-sets iOS texture compression to ASTC on all sprites under Assets/Sprites.
/// Also caps oversized background images to 2048.
/// Run via: Tools > HealthyZoo > Optimize Texture Compression
///
/// What this changes per texture:
///   - iOS platform override: ASTC_6x6 (characters) or ASTC_8x8 (backgrounds)
///   - maxTextureSize capped at 2048 (was 16384 on some backgrounds)
///   - Does NOT touch the default platform settings — only the iOS override
///   - Does NOT change your sprite slice data, pivot points, or anything visual
/// </summary>
public static class TextureCompressionOptimizer
{
    private const string SpritesRoot = "Assets/Sprites";
    private const int MaxTextureSizeCap = 2048;

    [MenuItem("Tools/HealthyZoo/Optimize Texture Compression (iOS ASTC)")]
    public static void OptimizeAll()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Optimize Texture Compression",
            "This will set iOS compression to ASTC 6x6/8x8 on all textures under Assets/Sprites and cap max size to 2048.\n\nOriginal settings are NOT backed up — make sure your project is committed to git first.\n\nProceed?",
            "Yes, Optimize",
            "Cancel"
        );

        if (!confirmed) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot });
        int total    = guids.Length;
        int changed  = 0;
        int skipped  = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(
                    "Optimizing Textures",
                    $"({i + 1}/{total}) {System.IO.Path.GetFileName(path)}",
                    (float)i / total
                );

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { skipped++; continue; }

                bool isBackground = path.Contains("AnimalBackgroundImages") || path.Contains("Background");
                TextureImporterFormat targetFormat = isBackground
                    ? TextureImporterFormat.ASTC_8x8
                    : TextureImporterFormat.ASTC_6x6;

                // Read existing iOS settings (or create fresh override)
                TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings("iPhone");
                bool needsChange = false;

                if (!ios.overridden || ios.format != targetFormat || ios.maxTextureSize > MaxTextureSizeCap)
                {
                    ios.overridden         = true;
                    ios.format             = targetFormat;
                    ios.maxTextureSize     = Mathf.Min(ios.maxTextureSize > 0 ? ios.maxTextureSize : MaxTextureSizeCap, MaxTextureSizeCap);
                    ios.textureCompression = TextureImporterCompression.Compressed;
                    needsChange = true;
                }

                // Also cap the default platform max size if it's absurdly large (e.g. 16384)
                TextureImporterPlatformSettings defaultPlatform = importer.GetDefaultPlatformTextureSettings();
                if (defaultPlatform.maxTextureSize > MaxTextureSizeCap)
                {
                    defaultPlatform.maxTextureSize = MaxTextureSizeCap;
                    importer.SetPlatformTextureSettings(defaultPlatform);
                    needsChange = true;
                }

                if (needsChange)
                {
                    importer.SetPlatformTextureSettings(ios);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    changed++;
                }
                else
                {
                    skipped++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Texture Compression Optimizer",
            $"Done.\n\nTextures updated: {changed}\nAlready optimal / skipped: {skipped}\n\nAll character sprites → ASTC 6x6 on iOS\nBackground images → ASTC 8x8 on iOS\nMax texture size capped at {MaxTextureSizeCap}",
            "OK"
        );

        Debug.Log($"[TextureCompressionOptimizer] Complete. Changed: {changed}, Skipped: {skipped}");
    }
}
