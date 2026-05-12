using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Resizes all PNG frames under Assets/Sprites/NeutralAnimations using ImageMagick,
/// then updates each .meta file to keep sprites the same world-space size in Unity.
///
/// Run via: Tools > HealthyZoo > Resize Neutral Animations > [percentage]
/// Safe to re-run — skips files already at or below the target size.
/// </summary>
public static class NeutralAnimationResizer
{
    private const string SourceFolder = "Assets/Sprites/NeutralAnimations";
    private const int    OriginalPPU  = 100;

    private static readonly string[] SearchPaths =
    {
        "/opt/homebrew/bin",
        "/usr/local/bin",
        "/usr/bin",
    };

    // ── Menu items ─────────────────────────────────────────────────────────

    [MenuItem("Tools/HealthyZoo/Resize Neutral Animations (25%)")]
    public static void Resize25() => ResizeAll(25);

    [MenuItem("Tools/HealthyZoo/Resize Neutral Animations (50%)")]
    public static void Resize50() => ResizeAll(50);

    [MenuItem("Tools/HealthyZoo/Resize Neutral Animations (65% — 35% reduction)")]
    public static void Resize65() => ResizeAll(65);

    [MenuItem("Tools/HealthyZoo/Resize Neutral Animations (70% — 30% reduction)")]
    public static void Resize70() => ResizeAll(70);

    // ── Core logic ─────────────────────────────────────────────────────────

    static void ResizeAll(int percent)
    {
        string tool = FindTool("magick") ?? FindTool("convert");

        if (tool == null)
        {
            EditorUtility.DisplayDialog(
                "ImageMagick Not Found",
                "ImageMagick must be installed.\n\nInstall via Homebrew:\n  brew install imagemagick\n\nChecked:\n" +
                string.Join("\n", SearchPaths),
                "OK");
            return;
        }

        string absoluteFolder = Path.GetFullPath(SourceFolder);
        string[] pngFiles = Directory.GetFiles(absoluteFolder, "*.png", SearchOption.AllDirectories);

        if (pngFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing to resize", "No PNG files found under " + SourceFolder, "OK");
            return;
        }

        int targetPPU     = Mathf.RoundToInt(OriginalPPU * (percent / 100f));
        int skipThreshold = Mathf.RoundToInt(1920 * (percent / 100f) * 0.75f);

        int resized = 0, skipped = 0, failed = 0;

        try
        {
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string png  = pngFiles[i];
                string meta = png + ".meta";

                EditorUtility.DisplayProgressBar(
                    $"Resizing Neutral Animations ({percent}%)",
                    Path.GetFileName(png),
                    (float)i / pngFiles.Length);

                var (w, h) = GetPngDimensions(png);
                if (w <= 0) { skipped++; continue; }

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
                    Debug.LogError($"[NeutralAnimationResizer] ImageMagick failed on: {png}");
                    failed++;
                    continue;
                }

                if (File.Exists(meta))
                    UpdateMetaPPU(meta, targetPPU);

                resized++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            $"Resize Complete — Neutral Animations ({percent}%)",
            $"Resized: {resized}\nSkipped (already small): {skipped}\nFailed: {failed}\n\n" +
            $"PPU updated: {OriginalPPU} → {targetPPU}\n\n" +
            "Wait for Unity to finish reimporting before building.",
            "OK");
    }

    static void UpdateMetaPPU(string metaPath, int newPPU)
    {
        string text  = File.ReadAllText(metaPath);
        var    regex = new Regex(@"(spritePixelsToUnits:\s*)\d+");
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
