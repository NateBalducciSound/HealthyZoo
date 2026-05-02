using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;

/// Resizes large UI sprites by 30% using ImageMagick.
/// Targets files over 1000px in either dimension inside Assets/UI.
/// Run via: Tools > HealthyZoo > Resize Large UI Sprites (30%)
public static class UISpritesResizeTool
{
    private const string UIFolder   = "Assets/UI";
    private const float  Scale      = 0.30f;
    private const int    MinPixels  = 1000;

    [MenuItem("Tools/HealthyZoo/Resize Large UI Sprites (30%)")]
    public static void ResizeLargeUISprites()
    {
        string absFolder = Path.Combine(Application.dataPath, "../" + UIFolder).Replace("\\", "/");
        string[] files   = Directory.GetFiles(absFolder, "*.*", SearchOption.AllDirectories);

        var targets = new System.Collections.Generic.List<string>();
        foreach (var f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

            string result = Run("identify", $"-format \"%wx%h\" \"{f}\"");
            if (string.IsNullOrEmpty(result)) continue;

            string[] parts = result.Trim().Split('x');
            if (parts.Length != 2) continue;
            if (!int.TryParse(parts[0], out int w) || !int.TryParse(parts[1], out int h)) continue;

            if (w > MinPixels || h > MinPixels)
                targets.Add(f);
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Resize UI Sprites", "No UI sprites found over 1000px.", "OK");
            return;
        }

        string fileList = string.Join("\n", targets.ConvertAll(t => Path.GetFileName(t)));
        bool confirmed = EditorUtility.DisplayDialog(
            "Resize Large UI Sprites (30%)",
            $"Found {targets.Count} large UI sprite(s) to resize to 30% of original:\n\n{fileList}\n\n" +
            "This overwrites the originals. Make sure git is committed first.",
            "Resize", "Cancel");

        if (!confirmed) return;

        int done = 0;
        foreach (var f in targets)
        {
            EditorUtility.DisplayProgressBar("Resizing UI Sprites", Path.GetFileName(f), (float)done / targets.Count);

            string ext    = Path.GetExtension(f).ToLower();
            string args   = ext == ".jpg" || ext == ".jpeg"
                ? $"mogrify -resize {(int)(Scale * 100)}% \"{f}\""
                : $"mogrify -resize {(int)(Scale * 100)}% \"{f}\"";

            Run("mogrify", $"-resize {(int)(Scale * 100)}% \"{f}\"");
            done++;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Resize Complete",
            $"Resized {done} UI sprite(s) to 30% of original size.",
            "OK");
    }

    private static string Run(string cmd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(psi);
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch { return null; }
    }
}
