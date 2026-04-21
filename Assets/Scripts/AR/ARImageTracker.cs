using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Prefabs must be placed inside a folder named "Resources/ARCharacters/" in your project.
// The imageName in each mapping must exactly match the prefab filename in that folder.

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageTracker : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Serializable]
    public struct ImagePrefabMapping
    {
        [Tooltip("Must match the Reference Image Library name AND the prefab filename in Resources/ARCharacters/")]
        public string imageName;
        [Tooltip("Override scale for this character. 0 = use global Spawn Scale")]
        public float scaleOverride;
    }

    public ImagePrefabMapping[] imagePrefabs;

    [Tooltip("Seconds of continuous tracking before the prefab appears.")]
    public float scanConfirmTime = 0f;

    [Tooltip("Seconds a trackable must be lost before it can be rescanned.")]
    public float rescanCooldown = 1.5f;

    [Tooltip("How far in front of the camera to spawn (metres).")]
    public float spawnDistance = 0.5f;

    [Tooltip("Base world size of the sprite at spawnDistance.")]
    public float spawnScale = 0.1f;

    [Tooltip("Assign the 'Rectangle' GameObject from the Canvas here — hidden when a character is active, shown when scanning.")]
    public GameObject scanningIndicator;

    // ── Scan Log ──────────────────────────────────────────────────────────────

    public static IReadOnlyCollection<string> ScannedImages => scannedImages;
    private static readonly HashSet<string> scannedImages = new();
    public static event Action<string> OnNewImageScanned;

    // ── Private ───────────────────────────────────────────────────────────────

    private const string ResourcesPath = "ARCharacters/";

    private ARTrackedImageManager trackedImageManager;
    private Transform arCamera;
    private GameObject _activeInstance;

    private enum ScanState { Idle, Scanning, Active }
    private ScanState _state = ScanState.Idle;

    private class TrackedState
    {
        public string imageName;
        public float scanTimer;
        public float lostTimer;
        // True only during the cooldown after this trackable just spawned a character,
        // preventing it from immediately re-triggering.
        public bool onCooldown;
        public GameObject loadedPrefab;
        public bool isLoading;
    }

    private readonly Dictionary<TrackableId, TrackedState> states = new();

    private Text statusText;
    private Image statusBackground;

    // ── On-Screen Debug Log ───────────────────────────────────────────────────
    // private Text _debugText;
    // private readonly System.Text.StringBuilder _debugLog = new();
    // private const int MaxDebugLines = 18;
    // private int _debugLineCount = 0;

    // void DebugLog(string msg, bool isWarning = false, bool isError = false)
    // {
    //     string prefix = isError ? "ERR " : isWarning ? "WRN " : "LOG ";
    //     string line = $"{prefix}{msg}";
    //     if (isError)        Debug.LogError($"[ARImageTracker] {msg}");
    //     else if (isWarning) Debug.LogWarning($"[ARImageTracker] {msg}");
    //     else                Debug.Log($"[ARImageTracker] {msg}");
    //     _debugLineCount++;
    //     if (_debugLineCount > MaxDebugLines)
    //     {
    //         int nl = _debugLog.ToString().IndexOf('\n');
    //         if (nl >= 0) _debugLog.Remove(0, nl + 1);
    //     }
    //     _debugLog.AppendLine(line);
    //     if (_debugText != null) _debugText.text = _debugLog.ToString();
    // }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        CreateStatusOverlay();
    }

    void Start()
    {
        var cam = GetComponentInChildren<Camera>();
        if (cam != null)              arCamera = cam.transform;
        else if (Camera.main != null) arCamera = Camera.main.transform;
        else
        {
            var found = FindFirstObjectByType<Camera>();
            if (found != null) arCamera = found.transform;
        }

        if (arCamera == null)
            Debug.LogWarning("[ARImageTracker] No camera found.");

        ApplyState(ScanState.Idle);
    }

    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnChanged;
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnChanged;
    }

    // ── AR Events ─────────────────────────────────────────────────────────────

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var img in eventArgs.added)
        {
            string name = img.referenceImage.name;
            // DebugLog($"DETECTED: \"{name}\"");

            // bool matched = false;
            // foreach (var m in imagePrefabs)
            //     if (string.Equals(m.imageName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
            //         matched = true;
            // if (!matched)
            //     DebugLog($"No imagePrefabs match for \"{name}\".", isWarning: true);

            states[img.trackableId] = new TrackedState { imageName = name };
        }

        foreach (var img in eventArgs.removed)
        {
            // if (states.TryGetValue(img.trackableId, out var s))
            //     DebugLog($"LOST: \"{s.imageName}\"");
            states.Remove(img.trackableId);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (trackedImageManager == null || arCamera == null) return;

        bool anyScanning = false;
        float bestProgress = 0f;

        foreach (var img in trackedImageManager.trackables)
        {
            if (!states.TryGetValue(img.trackableId, out var state)) continue;

            if (img.trackingState == TrackingState.Tracking)
            {
                state.lostTimer = 0f;

                // Still on cooldown after spawning — wait until the user points away first.
                if (state.onCooldown) continue;

                state.scanTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(state.scanTimer / scanConfirmTime);
                if (progress > bestProgress) bestProgress = progress;
                anyScanning = true;

                // Preload the prefab halfway through the scan timer.
                if (!state.isLoading && state.scanTimer >= scanConfirmTime * 0.5f)
                    StartCoroutine(LoadPrefabAsync(state));

                if (state.scanTimer >= scanConfirmTime)
                {
                    if (state.loadedPrefab != null)
                    {
                        Confirm(state);
                        anyScanning = false;
                        break;
                    }
                    // else: prefab still loading, Confirm() will be called from LoadPrefabAsync
                }
            }
            else
            {
                state.lostTimer += Time.deltaTime;

                // Once the image has been out of frame long enough, allow rescanning.
                if (state.onCooldown && state.lostTimer >= rescanCooldown)
                {
                    state.onCooldown = false;
                    state.scanTimer = 0f;
                    state.isLoading = false;
                    state.loadedPrefab = null;
                }
            }
        }

        if (anyScanning)
        {
            if (_state != ScanState.Scanning) ApplyState(ScanState.Scanning);
            SetStatusScanning(bestProgress);
        }
        else if (_activeInstance != null)
        {
            if (_state != ScanState.Active) ApplyState(ScanState.Active);
            SetStatusConfirmed();
        }
        else
        {
            if (_state != ScanState.Idle) ApplyState(ScanState.Idle);
            SetStatusIdle();
        }
    }

    // ── Async Load ────────────────────────────────────────────────────────────

    IEnumerator LoadPrefabAsync(TrackedState state)
    {
        state.isLoading = true;
        string path = ResourcesPath + state.imageName;

        GameObject prefab = Resources.Load<GameObject>(path);
        yield return null; // one-frame yield so this stays a coroutine

        if (prefab == null)
        {
            Debug.LogError($"[ARImageTracker] Failed to load Resources/{path}");
            yield break;
        }

        state.loadedPrefab = prefab;

        if (state.scanTimer >= scanConfirmTime && !state.onCooldown)
            Confirm(state);
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    void Confirm(TrackedState state)
    {
        // Put this trackable on cooldown so it won't immediately re-trigger.
        state.onCooldown = true;
        state.scanTimer = 0f;

        // Reset all other states so they can be rescanned fresh.
        foreach (var other in states.Values)
        {
            if (other == state) continue;
            other.scanTimer = 0f;
            other.lostTimer = 0f;
            other.isLoading = false;
            other.loadedPrefab = null;
            other.onCooldown = false;
        }

        DestroyActive();

        Vector3 spawnPos = arCamera.position + arCamera.forward * spawnDistance;
        float scale = GetScale(state.imageName);

        _activeInstance = Instantiate(state.loadedPrefab, spawnPos, Quaternion.identity);
        _activeInstance.transform.localScale = Vector3.one * scale;
        _activeInstance.AddComponent<ARCharacterDragger>();
        state.loadedPrefab = null;

        if (!scannedImages.Contains(state.imageName))
            OnNewImageScanned?.Invoke(state.imageName);
        scannedImages.Add(state.imageName);

        _confirmCount++;
        if (_confirmCount % 2 == 0)
            StartCoroutine(SilentCleanup());

        ApplyState(ScanState.Active);
    }

    private int _confirmCount = 0;

    IEnumerator SilentCleanup()
    {
        // Yield the unload as an async op so it spreads across frames with no hitch.
        yield return Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    // ── State ─────────────────────────────────────────────────────────────────

    void ApplyState(ScanState next)
    {
        _state = next;

        bool characterActive = next == ScanState.Active;

        // Rectangle: visible when idle or scanning, hidden when character is on screen.
        if (scanningIndicator != null)
            scanningIndicator.SetActive(!characterActive);

        // Status overlay: hidden when character is active and not being rescanned.
        statusBackground.gameObject.SetActive(next != ScanState.Active);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    float GetScale(string imageName)
    {
        foreach (var m in imagePrefabs)
            if (string.Equals(m.imageName.Trim(), imageName.Trim(), StringComparison.OrdinalIgnoreCase))
                return m.scaleOverride > 0f ? m.scaleOverride : spawnScale;
        return spawnScale;
    }

    void DestroyActive()
    {
        if (_activeInstance != null)
        {
            Destroy(_activeInstance);
            _activeInstance = null;
        }
    }

    // ── Status UI ─────────────────────────────────────────────────────────────

    void SetStatusIdle()
    {
        statusText.text = "Point camera at an image...";
        statusBackground.color = new Color(0f, 0f, 0f, 0.55f);
    }

    void SetStatusScanning(float progress)
    {
        int filled = Mathf.FloorToInt(progress * 10f);
        string bar = "[" + new string('|', filled) + new string('.', 10 - filled) + "]";
        statusText.text = $"Scanning... {bar}";
        statusBackground.color = new Color(0.7f, 0.5f, 0f, 0.75f);
    }

    void SetStatusConfirmed()
    {
        statusText.text = "Scan Complete!";
        statusBackground.color = new Color(0f, 0.55f, 0.1f, 0.75f);
    }

    void CreateStatusOverlay()
    {
        GameObject canvasGO = new GameObject("AR_StatusCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bgGO = new GameObject("StatusBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        statusBackground = bgGO.AddComponent<Image>();
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -40f);
        bgRect.sizeDelta = new Vector2(420f, 60f);

        GameObject textGO = new GameObject("StatusText");
        textGO.transform.SetParent(bgGO.transform, false);
        statusText = textGO.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 22;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);

        // On-screen debug log panel — commented out for release
        // GameObject debugBgGO = new GameObject("DebugLogBG");
        // debugBgGO.transform.SetParent(canvasGO.transform, false);
        // Image debugBg = debugBgGO.AddComponent<Image>();
        // debugBg.color = new Color(0f, 0f, 0f, 0.7f);
        // RectTransform debugBgRect = debugBgGO.GetComponent<RectTransform>();
        // debugBgRect.anchorMin = new Vector2(0f, 0f);
        // debugBgRect.anchorMax = new Vector2(1f, 0f);
        // debugBgRect.pivot = new Vector2(0.5f, 0f);
        // debugBgRect.anchoredPosition = new Vector2(0f, 0f);
        // debugBgRect.sizeDelta = new Vector2(0f, 320f);
        // GameObject debugTextGO = new GameObject("DebugLogText");
        // debugTextGO.transform.SetParent(debugBgGO.transform, false);
        // _debugText = debugTextGO.AddComponent<Text>();
        // _debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        // _debugText.fontSize = 18;
        // _debugText.alignment = TextAnchor.LowerLeft;
        // _debugText.color = Color.green;
        // _debugText.text = "[AR Debug Log]";
        // RectTransform debugTextRect = debugTextGO.GetComponent<RectTransform>();
        // debugTextRect.anchorMin = Vector2.zero;
        // debugTextRect.anchorMax = Vector2.one;
        // debugTextRect.offsetMin = new Vector2(8f, 6f);
        // debugTextRect.offsetMax = new Vector2(-8f, -6f);
    }
}
