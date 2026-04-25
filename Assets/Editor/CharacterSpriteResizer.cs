using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Resizes character sprite folders (25% or 50%) and updates spritePixelsToUnits
/// proportionally so world-space sizes remain unchanged.
///
/// Run via: Tools > HealthyZoo > Resize Character Sprites (25%) > [Character]
///          Tools > HealthyZoo > Resize Character Sprites (50%) > [Character]
/// Safe to re-run — skips frames already at or below the target size.
/// </summary>
public static class CharacterSpriteResizer
{
    // ── Character definitions ──────────────────────────────────────────────
    struct CharacterConfig
    {
        public string folder;
        public int    originalPPU;
    }

    static readonly CharacterConfig[] Characters =
    {
        new CharacterConfig { folder = "Assets/Sprites/GiraffeAssets",     originalPPU = 100 },
        new CharacterConfig { folder = "Assets/Sprites/HeronSpriteAssets", originalPPU = 250 },
        new CharacterConfig { folder = "Assets/Sprites/PandaAssets",       originalPPU = 300 },
        new CharacterConfig { folder = "Assets/Sprites/PorcupineAssets",   originalPPU = 300 },
        new CharacterConfig { folder = "Assets/Sprites/SlothAssets",       originalPPU = 250 },
    };

    // Common Homebrew paths — Unity editor doesn't inherit shell PATH
    static readonly string[] SearchPaths =
    {
        "/opt/homebrew/bin",
        "/usr/local/bin",
        "/usr/bin",
    };

    // ── 25% menu items ─────────────────────────────────────────────────────

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/Giraffe (GiraffeAssets)")]
    static void ResizeGiraffe25() => ResizeCharacter("Assets/Sprites/GiraffeAssets", 25);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/Heron (HeronSpriteAssets)")]
    static void ResizeHeron25() => ResizeCharacter("Assets/Sprites/HeronSpriteAssets", 25);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/Panda (PandaAssets)")]
    static void ResizePanda25() => ResizeCharacter("Assets/Sprites/PandaAssets", 25);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/Porcupine (PorcupineAssets)")]
    static void ResizePorcupine25() => ResizeCharacter("Assets/Sprites/PorcupineAssets", 25);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/Sloth (SlothAssets)")]
    static void ResizeSloth25() => ResizeCharacter("Assets/Sprites/SlothAssets", 25);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (25%)/ALL Characters")]
    static void ResizeAll25() { foreach (var c in Characters) ResizeCharacter(c.folder, 25); }

    // ── 50% menu items ─────────────────────────────────────────────────────

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/Giraffe (GiraffeAssets)")]
    static void ResizeGiraffe50() => ResizeCharacter("Assets/Sprites/GiraffeAssets", 50);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/Heron (HeronSpriteAssets)")]
    static void ResizeHeron50() => ResizeCharacter("Assets/Sprites/HeronSpriteAssets", 50);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/Panda (PandaAssets)")]
    static void ResizePanda50() => ResizeCharacter("Assets/Sprites/PandaAssets", 50);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/Porcupine (PorcupineAssets)")]
    static void ResizePorcupine50() => ResizeCharacter("Assets/Sprites/PorcupineAssets", 50);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/Sloth (SlothAssets)")]
    static void ResizeSloth50() => ResizeCharacter("Assets/Sprites/SlothAssets", 50);

    [MenuItem("Tools/HealthyZoo/Resize Character Sprites (50%)/ALL Characters")]
    static void ResizeAll50() { foreach (var c in Characters) ResizeCharacter(c.folder, 50); }


    // ── Core logic ─────────────────────────────────────────────────────────

    static void ResizeCharacter(string assetFolder, int percent)
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

        // Calculate target PPU: scale proportionally so world-space size is preserved
        int targetPPU = Mathf.RoundToInt(config.originalPPU * (percent / 100f));
        // Skip frames already at or near the target size (threshold = 75% of original short edge)
        int skipThreshold = Mathf.RoundToInt(1080 * (percent / 100f) * 0.75f);

        int resized = 0, skipped = 0, failed = 0;
        string charName = Path.GetFileName(assetFolder);

        try
        {
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string png  = pngFiles[i];
                string meta = png + ".meta";

                EditorUtility.DisplayProgressBar(
                    $"Resizing {charName} ({percent}%)",
                    Path.GetFileName(png),
                    (float)i / pngFiles.Length);

                var (w, h) = GetPngDimensions(png);
                if (w <= 0) { skipped++; continue; }

                // Skip if already at or below target size
                if (w <= skipThreshold && h <= skipThreshold)
                {
                    skipped++;
                    continue;
                }

                bool ok = Path.GetFileName(tool) == "magick"
                    ? RunCommand(tool, $"mogrify -resize {percent}% \"{png}\"")
                    : RunCommand(tool, $"\"{png}\" -resize {percent}% \"{png}\"");

                if (!ok)
                {
                    Debug.LogError($"[CharacterSpriteResizer] ImageMagick failed: {png}");
                    failed++;
                    continue;
                }

                if (File.Exists(meta))
                    UpdateMetaPPU(meta, config.originalPPU, targetPPU);

                resized++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            $"Resize Complete — {charName} ({percent}%)",
            $"Resized:  {resized}\nSkipped (already small): {skipped}\nFailed:  {failed}\n\n" +
            $"PPU updated: {config.originalPPU} → {targetPPU}\n\n" +
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
