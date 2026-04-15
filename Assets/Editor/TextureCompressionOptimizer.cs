using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bulk-sets iOS AND Android texture compression to ASTC on all sprites under Assets/Sprites.
/// Also caps oversized background images to 2048.
/// Run via: Tools > HealthyZoo > Optimize Texture Compression (iOS + Android ASTC)
///
/// What this changes per texture:
///   - iOS override:     ASTC_6x6 (characters), ASTC_8x8 (backgrounds)
///   - Android override: ASTC_6x6 (characters), ASTC_8x8 (backgrounds)
///     All target devices (API 30 / Android 11+) support ASTC natively.
///   - maxTextureSize capped at 2048 on all platforms
///   - Does NOT change sprite slice data, pivot points, or any visual settings
/// </summary>
public static class TextureCompressionOptimizer
{
    private static readonly string[] ScanRoots = new[] { "Assets/Sprites", "Assets/UI" };
    private const int MaxTextureSizeCap = 2048;

    [MenuItem("Tools/HealthyZoo/Optimize Texture Compression (iOS + Android ASTC)")]
    public static void OptimizeAll()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Optimize Texture Compression",
            "Sets iOS and Android compression to ASTC 6x6/8x8 on all textures under Assets/Sprites AND Assets/UI. Caps max size to 2048.\n\nMake sure your project is committed to git first.\n\nProceed?",
            "Yes, Optimize",
            "Cancel"
        );

        if (!confirmed) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", ScanRoots);
        int total   = guids.Length;
        int changed = 0;
        int skipped = 0;

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

                bool isBackground = path.Contains("AnimalBackgroundImages") || path.Contains("Background") || path.Contains("Assets/UI/");
                TextureImporterFormat targetFormat = isBackground
                    ? TextureImporterFormat.ASTC_8x8
                    : TextureImporterFormat.ASTC_6x6;

                bool needsChange = false;

                // iOS
                needsChange |= ApplyPlatformSettings(importer, "iPhone",  targetFormat);
                // Android — API 30+ (Android 11+) has universal ASTC hardware support
                needsChange |= ApplyPlatformSettings(importer, "Android", targetFormat);

                // Cap default platform max size if absurdly large
                TextureImporterPlatformSettings def = importer.GetDefaultPlatformTextureSettings();
                if (def.maxTextureSize > MaxTextureSizeCap)
                {
                    def.maxTextureSize = MaxTextureSizeCap;
                    importer.SetPlatformTextureSettings(def);
                    needsChange = true;
                }

                if (needsChange)
                {
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
            $"Done.\n\nTextures updated: {changed}\nAlready optimal / skipped: {skipped}\n\n" +
            "iOS + Android → ASTC 6x6 (characters), ASTC 8x8 (backgrounds)\nMax size capped at 2048\n\n" +
            "REMINDER: Also set Managed Stripping Level to Medium in\nEdit > Project Settings > Player > Other Settings.",
            "OK"
        );

        Debug.Log($"[TextureCompressionOptimizer] Complete. Changed: {changed}, Skipped: {skipped}");
    }

    static bool ApplyPlatformSettings(TextureImporter importer, string platform, TextureImporterFormat format)
    {
        TextureImporterPlatformSettings s = importer.GetPlatformTextureSettings(platform);
        if (s.overridden && s.format == format && s.maxTextureSize <= MaxTextureSizeCap)
            return false;

        s.overridden         = true;
        s.format             = format;
        s.maxTextureSize     = Mathf.Min(s.maxTextureSize > 0 ? s.maxTextureSize : MaxTextureSizeCap, MaxTextureSizeCap);
        s.textureCompression = TextureImporterCompression.Compressed;
        importer.SetPlatformTextureSettings(s);
        return true;
    }
}
