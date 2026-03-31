using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CustomEditor(typeof(LightweightSpriteAnimator))]
public class LightweightSpriteAnimatorEditor : Editor
{
    private Object _startupFolder;
    private Object _loopFolder;
    private int _stride = 2;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Auto-populate from folders ──", EditorStyles.boldLabel);

        _startupFolder = EditorGUILayout.ObjectField("Startup Folder", _startupFolder, typeof(DefaultAsset), false);
        _loopFolder    = EditorGUILayout.ObjectField("Loop Folder",    _loopFolder,    typeof(DefaultAsset), false);
        _stride = EditorGUILayout.IntSlider("Use every Nth frame", _stride, 1, 10);

        if (_stride > 1)
            EditorGUILayout.HelpBox(
                $"Stride {_stride}: uses every {_stride}{Ordinal(_stride)} frame — " +
                $"~{Mathf.RoundToInt((1f - 1f / _stride) * 100)}% memory reduction.",
                MessageType.Info);

        EditorGUILayout.Space(4);

        bool canLoad = _startupFolder != null || _loopFolder != null;
        EditorGUI.BeginDisabledGroup(!canLoad);
        if (GUILayout.Button("Load Frames"))
        {
            Undo.RecordObject(target, "Load Sprite Frames");
            var anim = (LightweightSpriteAnimator)target;

            if (_startupFolder != null)
                anim.startupFrames = LoadFromFolder(_startupFolder, _stride);
            if (_loopFolder != null)
                anim.loopFrames = LoadFromFolder(_loopFolder, _stride);

            EditorUtility.SetDirty(target);
        }
        EditorGUI.EndDisabledGroup();
    }

    static Sprite[] LoadFromFolder(Object folder, int stride)
    {
        string path = AssetDatabase.GetAssetPath(folder);
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning($"[LightweightSpriteAnimator] '{path}' is not a folder.");
            return null;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { path });
        var sprites = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .OrderBy(s => Path.GetFileNameWithoutExtension(s.name))
            .ToList();

        var strided = new List<Sprite>();
        for (int i = 0; i < sprites.Count; i += stride)
            strided.Add(sprites[i]);

        Debug.Log($"[LightweightSpriteAnimator] Loaded {strided.Count} frames " +
                  $"(stride {stride}, from {sprites.Count} total) from '{path}'");
        return strided.ToArray();
    }

    static string Ordinal(int n)
    {
        if (n % 100 >= 11 && n % 100 <= 13) return "th";
        return (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }
}
