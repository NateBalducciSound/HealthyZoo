using UnityEditor;
using UnityEngine;

/// <summary>
/// Optimizes audio import settings for iOS:
///   - Voice lines (longer clips) → Streaming, so they don't load fully into RAM
///   - Short SFX clips → Compressed In Memory (fast access, low RAM)
///   - All clips → Vorbis compression, quality 70
///   - Disables preloadAudioData on streaming clips
///
/// Run via: Tools > HealthyZoo > Optimize Audio Compression
/// </summary>
public static class AudioCompressionOptimizer
{
    // Clips shorter than this (seconds) are treated as SFX — loaded Compressed In Memory.
    // Clips longer are treated as voice/music — streamed from disk.
    private const float StreamingThresholdSeconds = 3.0f;
    private const float VorbisQuality = 0.7f; // 0.0 - 1.0

    [MenuItem("Tools/HealthyZoo/Optimize Audio Compression")]
    public static void OptimizeAll()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Optimize Audio Compression",
            "This will update audio import settings for all clips under Assets/Audio:\n\n" +
            "• Short SFX (< 3s): Compressed In Memory, Vorbis q70\n" +
            "• Voice / long clips (≥ 3s): Streaming, Vorbis q70\n\n" +
            "Make sure your project is committed to git before proceeding.",
            "Yes, Optimize",
            "Cancel"
        );

        if (!confirmed) return;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        int total   = guids.Length;
        int changed = 0;
        int skipped = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(
                    "Optimizing Audio",
                    $"({i + 1}/{total}) {System.IO.Path.GetFileName(path)}",
                    (float)i / total
                );

                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) { skipped++; continue; }

                // Determine clip length to pick load strategy
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                bool isLong = clip != null && clip.length >= StreamingThresholdSeconds;

                AudioImporterSampleSettings settings = importer.GetOverrideSampleSettings("iOS");
                bool hasOverride = importer.ContainsSampleSettingsOverride("iOS");

                AudioClipLoadType targetLoadType = isLong
                    ? AudioClipLoadType.Streaming
                    : AudioClipLoadType.CompressedInMemory;

                bool needsChange = !hasOverride
                    || settings.loadType        != targetLoadType
                    || settings.compressionFormat != AudioCompressionFormat.Vorbis
                    || Mathf.Abs(settings.quality - VorbisQuality) > 0.01f;

                if (needsChange)
                {
                    settings.loadType          = targetLoadType;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality           = VorbisQuality;

                    // Disable preloading on streaming clips — they don't need it
                    if (isLong)
                        settings.preloadAudioData = false;

                    importer.SetOverrideSampleSettings("iOS", settings);

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    changed++;

                    Debug.Log($"[AudioOptimizer] {System.IO.Path.GetFileName(path)} → {targetLoadType}, length: {clip?.length:F1}s");
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
            "Audio Compression Optimizer",
            $"Done.\n\nClips updated: {changed}\nAlready optimal / skipped: {skipped}",
            "OK"
        );

        Debug.Log($"[AudioCompressionOptimizer] Complete. Changed: {changed}, Skipped: {skipped}");
    }
}
