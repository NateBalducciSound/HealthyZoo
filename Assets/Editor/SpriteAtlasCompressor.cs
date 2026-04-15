using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Applies ASTC compression to all existing sprite atlases in Assets/SpriteAtlases/.
/// The original SpriteAtlasGenerator set these settings at creation time, but the
/// .spriteatlasv2 format requires them to be re-applied and the atlas reimported to take effect.
///
/// Background atlases (AnimalBackgroundImages) → ASTC 8x8
/// All other atlases                           → ASTC 6x6
///
/// Run via: Tools > HealthyZoo > Apply ASTC to Sprite Atlases
/// </summary>
public static class SpriteAtlasCompressor
{
    private const string AtlasFolder   = "Assets/SpriteAtlases";
    private const int    MaxSize       = 2048;

    [MenuItem("Tools/HealthyZoo/Apply ASTC to Sprite Atlases")]
    public static void ApplyAll()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Apply ASTC to Sprite Atlases",
            "Opens every sprite atlas in Assets/SpriteAtlases and sets:\n\n" +
            "  • AnimalBackgroundImages atlas → ASTC 8x8 (iOS + Android)\n" +
            "  • All other atlases            → ASTC 6x6 (iOS + Android)\n\n" +
            "This is the fix for atlases appearing uncompressed in the build.\n\n" +
            "Make sure your project is committed to git first.",
            "Yes, Apply ASTC",
            "Cancel"
        );
        if (!confirmed) return;

        // Find both .spriteatlas and .spriteatlasv2
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasFolder });
        int total   = guids.Length;
        int changed = 0;
        int failed  = 0;

        if (total == 0)
        {
            EditorUtility.DisplayDialog("No Atlases Found",
                $"No sprite atlases found in {AtlasFolder}.\nRun 'Generate Sprite Atlases' first.", "OK");
            return;
        }

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path  = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name  = System.IO.Path.GetFileNameWithoutExtension(path);

                EditorUtility.DisplayProgressBar(
                    "Applying ASTC to Atlases",
                    $"({i + 1}/{total}) {name}",
                    (float)i / total
                );

                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                {
                    Debug.LogWarning($"[SpriteAtlasCompressor] Could not load atlas: {path}");
                    failed++;
                    continue;
                }

                bool isBackground = path.Contains("AnimalBackground");
                TextureImporterFormat fmt = isBackground
                    ? TextureImporterFormat.ASTC_8x8
                    : TextureImporterFormat.ASTC_6x6;

                // iOS
                SetAtlasPlatform(atlas, "iPhone",  fmt);
                // Android
                SetAtlasPlatform(atlas, "Android", fmt);

                EditorUtility.SetDirty(atlas);
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                Debug.Log($"[SpriteAtlasCompressor] {name} → {fmt} (iOS + Android)");
                changed++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "ASTC Applied to Atlases",
            $"Done.\n\nAtlases updated: {changed}\nFailed: {failed}\n\n" +
            "Now run a fresh iOS build — atlas textures should be dramatically smaller.",
            "OK"
        );

        Debug.Log($"[SpriteAtlasCompressor] Complete. Changed: {changed}, Failed: {failed}");
    }

    static void SetAtlasPlatform(SpriteAtlas atlas, string platform, TextureImporterFormat format)
    {
        TextureImporterPlatformSettings s = atlas.GetPlatformSettings(platform);
        s.overridden         = true;
        s.format             = format;
        s.maxTextureSize     = MaxSize;
        s.textureCompression = TextureImporterCompression.Compressed;
        s.compressionQuality = 50;
        atlas.SetPlatformSettings(s);
    }
}
