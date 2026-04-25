using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Resizes all PNG frames under Assets/Sprites/NeutralAnimations to 25% of their
/// original size using ImageMagick, then updates each .meta file to keep the
/// sprites the same world-space size in Unity (spritePixelsToUnits 100 → 25).
///
/// Run via: Tools > HealthyZoo > Resize Neutral Animations (25%)
/// Safe to re-run — skips files that are already at target size.
/// </summary>
public static class NeutralAnimationResizer
{
    private const string SourceFolder  = "Assets/Sprites/NeutralAnimations";
    private const float  ScaleFactor   = 0.25f;
    private const int    OriginalPPU   = 100;
    private const int    TargetPPU     = 25; // OriginalPPU * ScaleFactor

    // Common Homebrew install paths Unity can't find via PATH
    private static readonly string[] SearchPaths =
    {
        "/opt/homebrew/bin",   // Apple Silicon
        "/usr/local/bin",       // Intel Mac
        "/usr/bin",
    };

    private static string FindTool(string name)
    {
        foreach (string dir in SearchPaths)
        {
            string full = Path.Combine(dir, name);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    [MenuItem("Tools/HealthyZoo/Resize Neutral Animations (25%)")]
    public static void ResizeAll()
    {
        string magick  = FindTool("magick");
        string convert = FindTool("convert");

        if (magick == null && convert == null)
        {
            EditorUtility.DisplayDialog(
                "ImageMagick Not Found",
                "ImageMagick must be installed.\n\nInstall via Homebrew:\n  brew install imagemagick\n\nChecked:\n" +
                string.Join("\n", SearchPaths),
                "OK");
            return;
        }

        string tool = magick ?? convert;
        string absoluteFolder = Path.GetFullPath(SourceFolder);

        string[] pngFiles = Directory.GetFiles(absoluteFolder, "*.png", SearchOption.AllDirectories);

        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing to resize", "No PNG files found under " + SourceFolder, "OK");
            return;
        }

        int resized = 0;
        int skipped = 0;
        int failed  = 0;

        try
        {
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string png  = pngFiles[i];
                string meta = png + ".meta";

                EditorUtility.DisplayProgressBar(
                    "Resizing Neutral Animations",
                    Path.GetFileName(png),
                    (float)i / pngFiles.Length);

                // Check if already resized by reading current dimensions
                var (w, h) = GetPngDimensions(png);
                if (w <= 0)
                {
                    Debug.LogWarning($"[NeutralAnimationResizer] Could not read dimensions, skipping: {png}");
                    skipped++;
                    continue;
                }

                // Skip if already at or below target size — do NOT update PPU either,
                // since the sprites are already correctly sized and placed in scenes.
                // Original frames are 1920x1080 — target is 480x270.
                if (w <= 960 && h <= 540)
                {
                    skipped++;
                    continue;
                }

                // Resize in-place via ImageMagick
                // `magick mogrify` edits in-place; legacy `convert` writes to same path explicitly
                bool ok = Path.GetFileName(tool) == "magick"
                    ? RunCommand(tool, $"mogrify -resize 25% \"{png}\"")
                    : RunCommand(tool, $"\"{png}\" -resize 25% \"{png}\"");

                if (!ok)
                {
                    Debug.LogError($"[NeutralAnimationResizer] ImageMagick failed on: {png}");
                    failed++;
                    continue;
                }

                // Update .meta: set spritePixelsToUnits to TargetPPU
                if (File.Exists(meta))
                    UpdateMetaPPU(meta);

                resized++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Resize Complete",
            $"Resized: {resized}\nSkipped (already small): {skipped}\nFailed: {failed}\n\n" +
            "Unity is reimporting. Wait for the progress bar to finish before building.",
            "OK");
    }

    private static void UpdateMetaPPU(string metaPath)
    {
        string text = File.ReadAllText(metaPath);

        // Replace spritePixelsToUnits value
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(
            @"(spritePixelsToUnits:\s*)\d+");

        if (regex.IsMatch(text))
        {
            text = regex.Replace(text, $"${{1}}{TargetPPU}");
            File.WriteAllText(metaPath, text);
        }
    }

    private static (int w, int h) GetPngDimensions(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);
            br.ReadBytes(8);  // PNG signature
            br.ReadBytes(4);  // IHDR chunk length
            br.ReadBytes(4);  // "IHDR"
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

    private static bool RunCommand(string cmd, string args)
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

}
