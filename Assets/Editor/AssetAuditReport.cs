using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scans the project for three categories of bloat and writes a report to the Console
/// and optionally saves it as a .txt file in the project root.
///
///   1. Duplicate filenames  — same filename found in more than one folder.
///      Unity includes every copy, so each duplicate adds to the build.
///
///   2. Placeholder assets   — files in folders named "Placeholder", or filenames that
///      contain common placeholder keywords (placeholder, temp, test, old, unused, backup).
///
///   3. Unused textures      — texture/sprite assets that are not referenced by any
///      Scene, Prefab, ScriptableObject, Material, or Animator in the project.
///      NOTE: Resources.Load() references are runtime-only and cannot be detected
///      statically; any asset under a "Resources" folder is flagged as a special
///      note rather than marked unused.
///
/// Run via: Tools > HealthyZoo > Asset Audit Report
/// SAFE — read-only. Nothing is deleted or modified.
/// </summary>
public static class AssetAuditReport
{
    // ── Keyword lists ──────────────────────────────────────────────────────────

    private static readonly string[] PlaceholderFolderKeywords = new[]
    {
        "placeholder", "placeholders", "temp", "temporary", "test", "tests",
        "old", "unused", "backup", "backups", "archive", "deprecated"
    };

    private static readonly string[] PlaceholderFileKeywords = new[]
    {
        "placeholder", "temp", "test", "old_", "_old", "unused", "backup",
        "bak_", "_bak", "deprecated", "draft", "wip_", "_wip"
    };

    // Asset types to include in the unused-asset scan
    private static readonly string[] TextureExtensions = new[]
    {
        ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff", ".bmp", ".gif"
    };

    // Folders that are always excluded from the unused scan (engine / third-party)
    private static readonly string[] ExcludedRoots = new[]
    {
        "Assets/TextMesh Pro",
        "Assets/XR",
        "Assets/XRI",
        "Assets/Plugins",
    };

    // ── Entry point ────────────────────────────────────────────────────────────

    [MenuItem("Tools/HealthyZoo/Asset Audit Report")]
    public static void RunAudit()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Asset Audit Report",
            "Scans the entire project for:\n\n" +
            "• Duplicate filenames (same name, different paths)\n" +
            "• Placeholder / temp / test assets\n" +
            "• Unused textures & sprites\n\n" +
            "Read-only — nothing will be deleted or changed.\n" +
            "Results print to the Console and can be saved as a .txt file.\n\n" +
            "This may take 30–60 seconds on large projects.",
            "Run Audit",
            "Cancel"
        );
        if (!confirmed) return;

        EditorUtility.DisplayProgressBar("Asset Audit", "Collecting all assets...", 0f);

        try
        {
            var report = new StringBuilder();
            report.AppendLine("=======================================================");
            report.AppendLine("  HEALTHYZOO ASSET AUDIT REPORT");
            report.AppendLine($"  Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("=======================================================\n");

            // 1. Collect every texture asset path in the project
            string[] allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            var allPaths = allGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !ExcludedRoots.Any(root => p.StartsWith(root)))
                .ToList();

            EditorUtility.DisplayProgressBar("Asset Audit", "Finding duplicates...", 0.1f);
            var duplicates  = FindDuplicates(allPaths);

            EditorUtility.DisplayProgressBar("Asset Audit", "Finding placeholder assets...", 0.3f);
            var placeholders = FindPlaceholders(allPaths);

            EditorUtility.DisplayProgressBar("Asset Audit", "Building dependency map (this takes a moment)...", 0.5f);
            var unused = FindUnused(allPaths);

            // ── Section 1: Duplicates ──────────────────────────────────────────
            report.AppendLine("───────────────────────────────────────────────────────");
            report.AppendLine($"  SECTION 1 — DUPLICATE FILENAMES ({duplicates.Count} groups)");
            report.AppendLine("  Same filename exists in multiple locations.");
            report.AppendLine("  Unity includes EVERY copy in the build.");
            report.AppendLine("───────────────────────────────────────────────────────\n");

            if (duplicates.Count == 0)
            {
                report.AppendLine("  No duplicates found.\n");
            }
            else
            {
                long totalDuplicateBytes = 0;
                foreach (var kvp in duplicates.OrderByDescending(k => k.Value.Count))
                {
                    report.AppendLine($"  [{kvp.Key}]  ({kvp.Value.Count} copies)");
                    foreach (string path in kvp.Value)
                    {
                        long bytes = GetFileSize(path);
                        totalDuplicateBytes += bytes;
                        report.AppendLine($"    {path}  ({FormatBytes(bytes)})");
                    }
                    // Wasted space = all copies minus one (you need to keep one)
                    long wastedBytes = kvp.Value
                        .Select(GetFileSize)
                        .OrderByDescending(b => b)
                        .Skip(1)
                        .Sum();
                    report.AppendLine($"    → Potential saving: {FormatBytes(wastedBytes)}\n");
                }
                long totalWasted = duplicates.Values
                    .SelectMany(v => v.Select(GetFileSize).OrderByDescending(b => b).Skip(1))
                    .Sum();
                report.AppendLine($"  Total potential saving from deduplication: {FormatBytes(totalWasted)}\n");
            }

            // ── Section 2: Placeholders ────────────────────────────────────────
            report.AppendLine("───────────────────────────────────────────────────────");
            report.AppendLine($"  SECTION 2 — PLACEHOLDER / TEMP ASSETS ({placeholders.Count} files)");
            report.AppendLine("  Detected by folder name or filename keywords:");
            report.AppendLine("  placeholder, temp, test, old, unused, backup, wip...");
            report.AppendLine("───────────────────────────────────────────────────────\n");

            if (placeholders.Count == 0)
            {
                report.AppendLine("  No placeholder assets found.\n");
            }
            else
            {
                long totalPlaceholderBytes = 0;
                foreach (var entry in placeholders.OrderBy(e => e.path))
                {
                    long bytes = GetFileSize(entry.path);
                    totalPlaceholderBytes += bytes;
                    report.AppendLine($"  [{entry.reason}]  {entry.path}  ({FormatBytes(bytes)})");
                }
                report.AppendLine($"\n  Total: {FormatBytes(totalPlaceholderBytes)}\n");
            }

            // ── Section 3: Unused ──────────────────────────────────────────────
            report.AppendLine("───────────────────────────────────────────────────────");
            report.AppendLine($"  SECTION 3 — UNUSED TEXTURES ({unused.normalUnused.Count} files)");
            report.AppendLine("  Not referenced by any Scene, Prefab, Material,");
            report.AppendLine("  ScriptableObject, or Animator in the project.");
            report.AppendLine("  NOTE: Assets under a Resources/ folder are listed");
            report.AppendLine("  separately — they may be loaded via Resources.Load()");
            report.AppendLine("  at runtime and cannot be statically verified.");
            report.AppendLine("───────────────────────────────────────────────────────\n");

            if (unused.normalUnused.Count == 0)
            {
                report.AppendLine("  No unused textures found outside Resources folders.\n");
            }
            else
            {
                long totalUnusedBytes = 0;
                foreach (string path in unused.normalUnused.OrderBy(p => p))
                {
                    long bytes = GetFileSize(path);
                    totalUnusedBytes += bytes;
                    report.AppendLine($"  {path}  ({FormatBytes(bytes)})");
                }
                report.AppendLine($"\n  Total: {FormatBytes(totalUnusedBytes)}\n");
            }

            if (unused.resourcesUnused.Count > 0)
            {
                report.AppendLine($"  RESOURCES FOLDER TEXTURES ({unused.resourcesUnused.Count} files)");
                report.AppendLine("  These are in a Resources/ folder. Unity always includes them");
                report.AppendLine("  in the build regardless of scene references.\n");
                long totalResBytes = 0;
                foreach (string path in unused.resourcesUnused.OrderBy(p => p))
                {
                    long bytes = GetFileSize(path);
                    totalResBytes += bytes;
                    report.AppendLine($"  {path}  ({FormatBytes(bytes)})");
                }
                report.AppendLine($"\n  Total in Resources folders: {FormatBytes(totalResBytes)}\n");
            }

            // ── Summary ────────────────────────────────────────────────────────
            report.AppendLine("=======================================================");
            report.AppendLine("  SUMMARY");
            report.AppendLine("=======================================================");
            report.AppendLine($"  Duplicate groups:       {duplicates.Count}");
            report.AppendLine($"  Placeholder files:      {placeholders.Count}");
            report.AppendLine($"  Unused textures:        {unused.normalUnused.Count}");
            report.AppendLine($"  Resources folder items: {unused.resourcesUnused.Count}");
            report.AppendLine("=======================================================\n");

            string reportText = report.ToString();

            // Print to console (click the log entry to see full text)
            Debug.Log("[AssetAuditReport]\n" + reportText);

            // Ask to save
            bool save = EditorUtility.DisplayDialog(
                "Asset Audit Complete",
                $"Found:\n" +
                $"  • {duplicates.Count} duplicate filename groups\n" +
                $"  • {placeholders.Count} placeholder/temp assets\n" +
                $"  • {unused.normalUnused.Count} unused textures\n" +
                $"  • {unused.resourcesUnused.Count} Resources folder textures\n\n" +
                "Full results printed to the Console.\n\n" +
                "Save report as a .txt file?",
                "Save .txt",
                "No thanks"
            );

            if (save)
            {
                string savePath = EditorUtility.SaveFilePanel(
                    "Save Audit Report",
                    Application.dataPath + "/..",
                    $"AssetAuditReport_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                    "txt"
                );
                if (!string.IsNullOrEmpty(savePath))
                {
                    File.WriteAllText(savePath, reportText);
                    Debug.Log($"[AssetAuditReport] Report saved to: {savePath}");
                    EditorUtility.DisplayDialog("Saved", $"Report saved to:\n{savePath}", "OK");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ── Duplicate detection ────────────────────────────────────────────────────

    static Dictionary<string, List<string>> FindDuplicates(List<string> paths)
    {
        var byFilename = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            string filename = Path.GetFileName(path);
            if (!byFilename.ContainsKey(filename))
                byFilename[filename] = new List<string>();
            byFilename[filename].Add(path);
        }

        return byFilename
            .Where(kvp => kvp.Value.Count > 1)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    // ── Placeholder detection ──────────────────────────────────────────────────

    static List<(string path, string reason)> FindPlaceholders(List<string> paths)
    {
        var results = new List<(string, string)>();

        foreach (string path in paths)
        {
            string lowerPath     = path.ToLowerInvariant();
            string lowerFilename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            // Check each folder component in the path
            string folderMatch = PlaceholderFolderKeywords
                .FirstOrDefault(k => lowerPath.Split('/')
                    .Any(segment => segment == k || segment.Contains(k)));

            if (folderMatch != null)
            {
                results.Add((path, $"folder contains '{folderMatch}'"));
                continue;
            }

            // Check filename keywords
            string fileMatch = PlaceholderFileKeywords
                .FirstOrDefault(k => lowerFilename.Contains(k));

            if (fileMatch != null)
                results.Add((path, $"filename contains '{fileMatch}'"));
        }

        return results;
    }

    // ── Unused asset detection ─────────────────────────────────────────────────

    static (List<string> normalUnused, List<string> resourcesUnused) FindUnused(List<string> allTexturePaths)
    {
        // Build the set of all assets that are referenced (depended upon) by
        // Scenes, Prefabs, Materials, ScriptableObjects, and Animators.
        var referencedGuids = new HashSet<string>();

        string[] referenceSourceTypes = new[]
        {
            "t:Scene", "t:Prefab", "t:Material", "t:ScriptableObject", "t:AnimatorController"
        };

        int typeCount = referenceSourceTypes.Length;
        for (int t = 0; t < typeCount; t++)
        {
            EditorUtility.DisplayProgressBar(
                "Asset Audit",
                $"Scanning dependencies ({referenceSourceTypes[t]})...",
                0.5f + 0.4f * t / typeCount
            );

            string[] sourceGuids = AssetDatabase.FindAssets(referenceSourceTypes[t], new[] { "Assets" });
            foreach (string guid in sourceGuids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                string[] deps = AssetDatabase.GetDependencies(sourcePath, recursive: true);
                foreach (string dep in deps)
                {
                    string depGuid = AssetDatabase.AssetPathToGUID(dep);
                    if (!string.IsNullOrEmpty(depGuid))
                        referencedGuids.Add(depGuid);
                }
            }
        }

        EditorUtility.DisplayProgressBar("Asset Audit", "Comparing against all textures...", 0.95f);

        var normalUnused    = new List<string>();
        var resourcesUnused = new List<string>();

        foreach (string path in allTexturePaths)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (referencedGuids.Contains(guid)) continue;

            // Check if it lives inside a Resources folder — those are always included
            // by Unity regardless of scene references, so separate them out.
            bool inResources = path.Contains("/Resources/");
            if (inResources)
                resourcesUnused.Add(path);
            else
                normalUnused.Add(path);
        }

        return (normalUnused, resourcesUnused);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static long GetFileSize(string assetPath)
    {
        string fullPath = Path.Combine(
            Application.dataPath.Replace("Assets", ""),
            assetPath
        );
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
    }

    static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024f * 1024f):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024f:F0} KB";
        return $"{bytes} B";
    }
}
