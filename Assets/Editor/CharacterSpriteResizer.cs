using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Resizes character sprite folders to 25% and updates spritePixelsToUnits proportionally
/// so world-space sizes remain unchanged.
///
/// Run via: Tools > HealthyZoo > Resize Character Sprites > [Character]
/// Safe to re-run — skips frames already at or below half original size.
/// </summary>
public static class CharacterSpriteResizer
{
    // ── Character definitions ──────────────────────────────────────────────
    // originalPPU: value currently in .meta files (used to calculate target)
    // skipThresholdWidth: frames at or below this width are already small enough
    struct CharacterConfig
    {
        public string folder;
        public int    originalPPU;
        public int    targetPPU;       // originalPPU / 4  (round to nearest int)
        public int    skipThresholdW;  // skip if w <= this (already resized)
    }

    static readonly CharacterConfig[] Characters =
    {
        new CharacterConfig { folder = "Assets/Sprites/GiraffeAssets",     originalPPU = 100, targetPPU = 25, skipThresholdW = 540 },
        new CharacterConfig { folder = "Assets/Sprites/HeronSpriteAssets", originalPPU = 250, targetPPU = 63, skipThresholdW = 540 },
        new CharacterConfig { folder = "Assets/Sprites/PandaAssets",       originalPPU = 300, targetPPU = 75, skipThresholdW = 540 },
        new CharacterConfig { folder = "Assets/Sprites/PorcupineAssets",   originalPPU = 300, targetPPU = 75, skipThresholdW = 540 },
        new CharacterConfig { folder = "Assets/Sprites/SlothAssets",       originalPPU = 250, targetPPU = 63, skipThresholdW = 540 },
    };

    // Common Homebrew paths — Unity editor doesn't inherit shell PATH
    static readonly string[] SearchPaths =
    {
        "/opt/homebrew/bin",
        "/usr/local/bin",
        "/usr/bin",
    };

    // ── Menu items ─────────────────────────────────────────────────────────

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/Giraffe (GiraffeAssets)")]
    static void ResizeGiraffe() => ResizeCharacter("Assets/Sprites/GiraffeAssets");

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/Heron (HeronSpriteAssets)")]
    static void ResizeHeron() => ResizeCharacter("Assets/Sprites/HeronSpriteAssets");

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/Panda (PandaAssets)")]
    static void ResizePanda() => ResizeCharacter("Assets/Sprites/PandaAssets");

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/Porcupine (PorcupineAssets)")]
    static void ResizePorcupine() => ResizeCharacter("Assets/Sprites/PorcupineAssets");

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/Sloth (SlothAssets)")]
    static void ResizeSloth() => ResizeCharacter("Assets/Sprites/SlothAssets");

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites/ALL Characters")]
    static void ResizeAll()
    {
        foreach (var c in Characters)
            ResizeCharacter(c.folder);
    }

    // ── Core logic ─────────────────────────────────────────────────────────

    static void ResizeCharacter(string assetFolder)
    {
        CharacterConfig config = default;
        bool found = false;
        foreach (var c in Characters)
        {
            if (c.folder == assetFolder) { config = c; found = true; break; }
        }
        if (!found)
        {
            Debug.LogError($"[CharacterSpriteResizer] No config for folder: {assetFolder}");
            return;
        }

        string tool = FindTool("magick") ?? FindTool("convert");
        if (tool == null)
        {
            EditorUtility.DisplayDialog("ImageMagick Not Found",
                "ImageMagick must be installed.\n\n  brew install imagemagick\n\nChecked:\n" +
                string.Join("\n", SearchPaths), "OK");
            return;
        }

        string absoluteFolder = Path.GetFullPath(assetFolder);
        if (!Directory.Exists(absoluteFolder))
        {
            EditorUtility.DisplayDialog("Folder Not Found", $"Could not find:\n{assetFolder}", "OK");
            return;
        }

        string[] pngFiles = Directory.GetFiles(absoluteFolder, "*.png", SearchOption.AllDirectories);
        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing to resize", $"No PNG files found in {assetFolder}", "OK");
            return;
        }

        int resized = 0, skipped = 0, failed = 0;
        string charName = Path.GetFileName(assetFolder);

        try
        {
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string png  = pngFiles[i];
                string meta = png + ".meta";

                EditorUtility.DisplayProgressBar(
                    $"Resizing {charName}",
                    Path.GetFileName(png),
                    (float)i / pngFiles.Length);

                var (w, h) = GetPngDimensions(png);
                if (w <= 0) { skipped++; continue; }

                // Skip if already at or below target size
                if (w <= config.skipThresholdW && h <= config.skipThresholdW)
                {
                    skipped++;
                    continue;
                }

                bool ok = Path.GetFileName(tool) == "magick"
                    ? RunCommand(tool, $"mogrify -resize 25% \"{png}\"")
                    : RunCommand(tool, $"\"{png}\" -resize 25% \"{png}\"");

                if (!ok)
                {
                    Debug.LogError($"[CharacterSpriteResizer] ImageMagick failed: {png}");
                    failed++;
                    continue;
                }

                if (File.Exists(meta))
                    UpdateMetaPPU(meta, config.originalPPU, config.targetPPU);

                resized++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            $"Resize Complete — {charName}",
            $"Resized:  {resized}\nSkipped (already small): {skipped}\nFailed:  {failed}\n\n" +
            $"PPU updated: {config.originalPPU} → {config.targetPPU}\n\n" +
            "Wait for Unity to finish reimporting before building.",
            "OK");
    }

    static void UpdateMetaPPU(string metaPath, int oldPPU, int newPPU)
    {
        string text = File.ReadAllText(metaPath);
        // Replace exact old PPU value (avoids accidentally replacing unrelated numbers)
        var regex = new Regex($@"(spritePixelsToUnits:\s*){oldPPU}");
        if (regex.IsMatch(text))
        {
            text = regex.Replace(text, $"${{1}}{newPPU}");
            File.WriteAllText(metaPath, text);
        }
    }

    static (int w, int h) GetPngDimensions(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);
            br.ReadBytes(8); br.ReadBytes(4); br.ReadBytes(4);
            byte[] wb = br.ReadBytes(4);
            byte[] hb = br.ReadBytes(4);
            if (System.BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(wb);
                System.Array.Reverse(hb);
            }
            return (System.BitConverter.ToInt32(wb, 0), System.BitConverter.ToInt32(hb, 0));
        }
        catch { return (-1, -1); }
    }

    static bool RunCommand(string cmd, string args)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };
        using var p = Process.Start(psi);
        p.WaitForExit();
        return p.ExitCode == 0;
    }

    static string FindTool(string name)
    {
        foreach (string dir in SearchPaths)
        {
            string full = Path.Combine(dir, name);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}
