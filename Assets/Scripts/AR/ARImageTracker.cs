using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageTracker : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Serializable]
    public struct ImagePrefabMapping
    {
        public string imageName;
        public GameObject prefab;
    }

    public ImagePrefabMapping[] imagePrefabs;

    [Tooltip("Seconds of continuous tracking before the prefab appears.")]
    public float scanConfirmTime = 1.5f;

    [Tooltip("Seconds of lost tracking before the prefab is destroyed.")]
    public float lostGraceTime = 0.8f;

    [Tooltip("How far in front of the camera to spawn (metres).")]
    public float spawnDistance = 0.5f;

    [Tooltip("Base world size of the sprite at spawnDistance.")]
    public float spawnScale = 0.1f;

    [Tooltip("How smoothly the scale adjusts to distance changes.")]
    public float scaleSmoothSpeed = 6f;

    // ── Scan Log ──────────────────────────────────────────────────────────────

    public static IReadOnlyCollection<string> ScannedImages => scannedImages;
    private static readonly HashSet<string> scannedImages = new();
    public static event Action<string> OnNewImageScanned;

    // ── Private ───────────────────────────────────────────────────────────────

    private ARTrackedImageManager trackedImageManager;
    private Transform arCamera;

    private class TrackedState
    {
        public string imageName;
        public float scanTimer;
        public float lostTimer;
        public bool confirmed;
        public GameObject instance;
        public float currentScale;
    }

    private readonly Dictionary<TrackableId, TrackedState> states = new();

    private Text statusText;
    private Image statusBackground;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        CreateStatusOverlay();
    }

    void Start()
    {
        // AR cameras are often not tagged MainCamera — search children of XR Origin first
        var cam = GetComponentInChildren<Camera>();
        if (cam != null)                              arCamera = cam.transform;
        else if (Camera.main != null)                 arCamera = Camera.main.transform;
        else
        {
            var found = FindFirstObjectByType<Camera>();
            if (found != null) arCamera = found.transform;
        }

        if (arCamera == null)
            Debug.LogWarning("[ARImageTracker] No camera found — spawning will not work.");
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
            states[img.trackableId] = new TrackedState { imageName = img.referenceImage.name };

        foreach (var img in eventArgs.removed)
        {
            if (states.TryGetValue(img.trackableId, out var s))
            {
                DestroyInstance(s);
                states.Remove(img.trackableId);
            }
            RefreshStatusUI();
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (trackedImageManager == null || arCamera == null) return;

        bool anyScanning = false;
        bool anyConfirmed = false;
        string confirmedName = null;
        float bestProgress = 0f;

        foreach (var img in trackedImageManager.trackables)
        {
            if (!states.TryGetValue(img.trackableId, out var state)) continue;

            if (img.trackingState == TrackingState.Tracking)
            {
                state.lostTimer = 0f;
                state.scanTimer += Time.deltaTime;

                if (state.confirmed && state.instance != null)
                {
                    // Scale smoothly based on distance from camera
                    float dist = Vector3.Distance(arCamera.position, state.instance.transform.position);
                    float targetScale = spawnScale * (dist / Mathf.Max(spawnDistance, 0.01f));
                    state.currentScale = Mathf.Lerp(state.currentScale, targetScale, scaleSmoothSpeed * Time.deltaTime);
                    state.instance.transform.localScale = Vector3.one * Mathf.Max(state.currentScale, 0.001f);

                    anyConfirmed = true;
                    confirmedName = state.imageName;
                }
                else if (!state.confirmed)
                {
                    float progress = Mathf.Clamp01(state.scanTimer / scanConfirmTime);
                    if (progress > bestProgress) bestProgress = progress;
                    anyScanning = true;

                    if (state.scanTimer >= scanConfirmTime)
                        Confirm(state);
                }
            }
            else
            {
                state.lostTimer += Time.deltaTime;

                if (state.lostTimer >= lostGraceTime && state.confirmed)
                {
                    DestroyInstance(state);
                    state.confirmed = false;
                    state.scanTimer = 0f;
                }
            }
        }

        if (anyConfirmed)       SetStatusConfirmed(confirmedName);
        else if (anyScanning)   SetStatusScanning(bestProgress);
        else                    SetStatusIdle();
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    void Confirm(TrackedState state)
    {
        state.confirmed = true;

        GameObject prefab = FindPrefab(state.imageName);
        if (prefab == null)
        {
            Debug.LogWarning($"[ARImageTracker] No prefab mapped for '{state.imageName}'");
            return;
        }

        // Spawn centered in front of the camera
        Vector3 spawnPos = arCamera.position + arCamera.forward * spawnDistance;
        state.instance = Instantiate(prefab, spawnPos, Quaternion.identity);
        state.currentScale = spawnScale;
        state.instance.transform.localScale = Vector3.one * spawnScale;
        state.instance.SetActive(true);

        if (scannedImages.Add(state.imageName))
            OnNewImageScanned?.Invoke(state.imageName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void DestroyInstance(TrackedState state)
    {
        if (state.instance != null)
        {
            Destroy(state.instance);
            state.instance = null;
        }
    }

    GameObject FindPrefab(string name)
    {
        foreach (var m in imagePrefabs)
            if (string.Equals(m.imageName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                return m.prefab;
        return null;
    }

    // ── Status UI ─────────────────────────────────────────────────────────────

    void RefreshStatusUI() => SetStatusIdle();

    void SetStatusIdle()
    {
        statusText.text = scannedImages.Count > 0
            ? $"Scanned: {string.Join(", ", scannedImages)}"
            : "Point camera at an image...";
        statusBackground.color = new Color(0f, 0f, 0f, 0.55f);
    }

    void SetStatusScanning(float progress)
    {
        int filled = Mathf.FloorToInt(progress * 10f);
        string bar = "[" + new string('|', filled) + new string('.', 10 - filled) + "]";
        statusText.text = $"Scanning... {bar}";
        statusBackground.color = new Color(0.7f, 0.5f, 0f, 0.75f);
    }

    void SetStatusConfirmed(string imageName)
    {
        statusText.text = $"Showing: {imageName}  ({scannedImages.Count} scanned)";
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

        SetStatusIdle();
    }
}
