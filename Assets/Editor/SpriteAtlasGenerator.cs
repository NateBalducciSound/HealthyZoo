using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Creates one Sprite Atlas per top-level character folder under Assets/Sprites.
/// Run via: Tools > HealthyZoo > Generate Sprite Atlases
/// Safe to re-run — skips atlases that already exist.
/// </summary>
public static class SpriteAtlasGenerator
{
    // These folders get their own atlas. Add or remove entries as needed.
    private static readonly string[] CharacterFolders =
    {
        "Assets/Sprites/AlligatorAssets",
        "Assets/Sprites/GiraffeAssets",
        "Assets/Sprites/HeronSpriteAssets",
        "Assets/Sprites/PandaAssets",
        "Assets/Sprites/PorcupineAssets",
        "Assets/Sprites/SlothAssets",
        "Assets/Sprites/CharacterDefaultSprites",
        "Assets/Sprites/AnimalBackgroundImages",
        "Assets/Sprites/UI",
        "Assets/Sprites/ScanImages",
    };

    private const string AtlasOutputFolder = "Assets/SpriteAtlases";

    [MenuItem("Tools/HealthyZoo/Generate Sprite Atlases")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(AtlasOutputFolder))
            AssetDatabase.CreateFolder("Assets", "SpriteAtlases");

        int created = 0;
        int skipped = 0;

        foreach (string folder in CharacterFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[SpriteAtlasGenerator] Folder not found, skipping: {folder}");
                continue;
            }

            string atlasName = Path.GetFileName(folder) + "_Atlas";
            string atlasPath = $"{AtlasOutputFolder}/{atlasName}.spriteatlas";

            if (File.Exists(atlasPath))
            {
                Debug.Log($"[SpriteAtlasGenerator] Already exists, skipping: {atlasPath}");
                skipped++;
                continue;
            }

            SpriteAtlas atlas = new SpriteAtlas();

            // --- Packing settings ---
            SpriteAtlasPackingSettings packSettings = new SpriteAtlasPackingSettings
            {
                blockOffset    = 1,
                enableRotation = false,   // keep false for sprites — rotation breaks UV expectations
                enableTightPacking = false,
                padding        = 4,
            };
            atlas.SetPackingSettings(packSettings);

            // --- Texture settings (shared across platforms) ---
            SpriteAtlasTextureSettings texSettings = new SpriteAtlasTextureSettings
            {
                readable        = false,
                generateMipMaps = false,
                sRGB            = true,
                filterMode      = FilterMode.Bilinear,
            };
            atlas.SetTextureSettings(texSettings);

            // --- Default platform settings ---
            TextureImporterPlatformSettings defaultPlatform = new TextureImporterPlatformSettings
            {
                name             = "DefaultTexturePlatform",
                maxTextureSize   = 2048,
                format           = TextureImporterFormat.Automatic,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                overridden       = false,
            };
            atlas.SetPlatformSettings(defaultPlatform);

            // --- iOS platform override: ASTC 6x6 ---
            TextureImporterPlatformSettings iosPlatform = new TextureImporterPlatformSettings
            {
                name             = "iPhone",
                maxTextureSize   = 2048,
                format           = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                overridden       = true,
            };

            // Background images are large flat art — use more aggressive ASTC 8x8
            if (folder.Contains("AnimalBackgroundImages"))
                iosPlatform.format = TextureImporterFormat.ASTC_8x8;

            atlas.SetPlatformSettings(iosPlatform);

            // --- Add the entire folder as a packing source ---
            Object folderObject = AssetDatabase.LoadAssetAtPath<Object>(folder);
            atlas.Add(new Object[] { folderObject });

            // --- Save ---
            AssetDatabase.CreateAsset(atlas, atlasPath);
            Debug.Log($"[SpriteAtlasGenerator] Created: {atlasPath}");
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Sprite Atlas Generator",
            $"Done.\n\nCreated: {created}\nSkipped (already existed): {skipped}\n\nAtlases saved to: {AtlasOutputFolder}\n\nNext step: go to Edit > Project Settings > Editor and set Sprite Packer mode to 'Sprite Atlas V2 - Enabled'. Then do a test build.",
            "OK"
        );
    }
}
