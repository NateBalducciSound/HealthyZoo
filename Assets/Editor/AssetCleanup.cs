using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deletes confirmed duplicate and placeholder assets identified in the Asset Audit Report.
///
/// WHAT THIS DELETES:
///   Duplicates:
///     - Assets/Sprites/AlligatorAssets/AlligatorNewExportTD/  (96 frames identical to ArloStartUp)
///     - Assets/UI/UI_MiniBackgrounds/healthyzoo_minigame-Arlobg.png  (keep Sprites version)
///     - Assets/UI/UI_MiniBackgrounds/healthyzoo_minigame-gigibg.png  (keep Sprites version)
///     - Assets/UI/UI_ButtonsBase/Splashscreen.png              (keep Sprites/UI version)
///
///   Placeholders:
///     - Assets/Sprites/UI/Placeholder/AnimalPics/*  (10 placeholder photo files)
///     - Assets/Sprites/UI/Placeholder/WoodenFramePlaceHolder.png
///
/// WHAT THIS DOES NOT TOUCH:
///   - loop000XX.png files in Heron/Porcupine/Sloth — same filename but DIFFERENT content per animal
///   - Any unused assets (those were flagged but you chose to keep them)
///   - Assets/Settings/Project Configuration/TemplateImage.png — Unity settings, not a game asset
///
/// Run via: Tools > HealthyZoo > Run Asset Cleanup (Duplicates + Placeholders)
/// Uses AssetDatabase.DeleteAsset — .meta files are cleaned up automatically.
/// </summary>
public static class AssetCleanup
{
    // Entire folder to delete (AlligatorNewExportTD is a full duplicate of ArloStartUp)
    private static readonly string[] FoldersToDelete = new[]
    {
        "Assets/Sprites/AlligatorAssets/AlligatorNewExportTD",
    };

    // Individual files to delete — duplicate copies where another path is the canonical one
    private static readonly string[] DuplicateFilesToDelete = new[]
    {
        // Keep: Assets/Sprites/AnimalBackgroundImages/healthyzoo_minigame-Arlobg.png
        "Assets/UI/UI_MiniBackgrounds/healthyzoo_minigame-Arlobg.png",

        // Keep: Assets/Sprites/AnimalBackgroundImages/healthyzoo_minigame-gigibg.png
        "Assets/UI/UI_MiniBackgrounds/healthyzoo_minigame-gigibg.png",

        // Keep: Assets/Sprites/UI/Splashscreen.png
        "Assets/UI/UI_ButtonsBase/Splashscreen.png",
    };

    // Placeholder files — not real game assets, safe to remove
    private static readonly string[] PlaceholderFilesToDelete = new[]
    {
        "Assets/Sprites/UI/Placeholder/AnimalPics/AlligatorPlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/DogPlaceholder.png",
        "Assets/Sprites/UI/Placeholder/AnimalPics/ElephantPlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/GiraffePlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/HeronPlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/LemurPlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/PandaPlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/PorcupinePlaceholder.jpg",
        "Assets/Sprites/UI/Placeholder/AnimalPics/SlothImage.jpg",
        "Assets/Sprites/UI/Placeholder/WoodenFramePlaceHolder.png",
    };

    [MenuItem("Tools/HealthyZoo/Run Asset Cleanup (Duplicates + Placeholders)")]
    public static void RunCleanup()
    {
        // Build a summary for the confirmation dialog
        int folderCount    = FoldersToDelete.Length;
        int dupFileCount   = DuplicateFilesToDelete.Length;
        int phFileCount    = PlaceholderFilesToDelete.Length;

        bool confirmed = EditorUtility.DisplayDialog(
            "Run Asset Cleanup",
            $"This will permanently delete:\n\n" +
            $"  • {folderCount} duplicate folder(s)\n" +
            $"    - AlligatorNewExportTD/ (96 frames, identical to ArloStartUp)\n\n" +
            $"  • {dupFileCount} duplicate file(s)\n" +
            $"    - Arlobg.png (UI copy — Sprites copy kept)\n" +
            $"    - gigibg.png (UI copy — Sprites copy kept)\n" +
            $"    - Splashscreen.png (UI copy — Sprites/UI copy kept)\n\n" +
            $"  • {phFileCount} placeholder file(s)\n" +
            $"    - All files in Sprites/UI/Placeholder/AnimalPics/\n" +
            $"    - WoodenFramePlaceHolder.png\n\n" +
            $"loop000XX.png files are NOT touched — they are different content per animal.\n\n" +
            $"Make sure your project is committed to git before proceeding.",
            "Yes, Delete",
            "Cancel"
        );

        if (!confirmed) return;

        int deleted = 0;
        int skipped = 0;
        var log = new System.Text.StringBuilder();

        // Delete folders
        foreach (string folder in FoldersToDelete)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                bool ok = AssetDatabase.DeleteAsset(folder);
                if (ok)
                {
                    deleted++;
                    log.AppendLine($"  [DELETED folder] {folder}");
                    Debug.Log($"[AssetCleanup] Deleted folder: {folder}");
                }
                else
                {
                    skipped++;
                    log.AppendLine($"  [FAILED folder] {folder}");
                    Debug.LogWarning($"[AssetCleanup] Failed to delete folder: {folder}");
                }
            }
            else
            {
                skipped++;
                log.AppendLine($"  [NOT FOUND folder] {folder}");
                Debug.Log($"[AssetCleanup] Folder not found (already deleted?): {folder}");
            }
        }

        // Delete duplicate files
        foreach (string path in DuplicateFilesToDelete)
            DeleteFile(path, "duplicate", ref deleted, ref skipped, log);

        // Delete placeholder files
        foreach (string path in PlaceholderFilesToDelete)
            DeleteFile(path, "placeholder", ref deleted, ref skipped, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AssetCleanup] Complete.\n{log}");

        EditorUtility.DisplayDialog(
            "Asset Cleanup Complete",
            $"Deleted: {deleted} items\nNot found / skipped: {skipped}\n\n" +
            "Full log printed to the Console.\n\n" +
            "Recommend rebuilding the iOS build now to verify the size reduction.",
            "OK"
        );
    }

    static void DeleteFile(string path, string category, ref int deleted, ref int skipped, System.Text.StringBuilder log)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (!string.IsNullOrEmpty(guid))
        {
            bool ok = AssetDatabase.DeleteAsset(path);
            if (ok)
            {
                deleted++;
                log.AppendLine($"  [DELETED {category}] {path}");
                Debug.Log($"[AssetCleanup] Deleted {category}: {path}");
            }
            else
            {
                skipped++;
                log.AppendLine($"  [FAILED {category}] {path}");
                Debug.LogWarning($"[AssetCleanup] Failed to delete: {path}");
            }
        }
        else
        {
            skipped++;
            log.AppendLine($"  [NOT FOUND {category}] {path}");
            Debug.Log($"[AssetCleanup] Not found (already deleted?): {path}");
        }
    }
}
