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

    private const string ResourcesPath = "ARCharacters/";

    private ARTrackedImageManager trackedImageManager;
    private Transform arCamera;
    private GameObject _activeInstance;

    private class TrackedState
    {
        public string imageName;
        public float scanTimer;
        public float lostTimer;
        public bool confirmed;
        public float currentScale;
        public float anchorScale;       // scale at the moment of spawn
        public float anchorDistance;    // camera distance at the moment of spawn
        public Vector3 anchorPosition;  // world position locked at spawn
        public GameObject loadedPrefab;
        public bool isLoading;
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
                if (s.confirmed) DestroyActive();
                states.Remove(img.trackableId);
            }
            RefreshStatusUI();
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    // Tracks estimated camera displacement using accelerometer when image is lost
    private Vector3 _accelVelocity = Vector3.zero;
    private Vector3 _accelDisplacement = Vector3.zero;
    private bool _accelTracking = false;

    void Update()
    {
        if (trackedImageManager == null || arCamera == null) return;

        bool anyScanning  = false;
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
                _accelTracking = false;
                _accelVelocity = Vector3.zero;
                _accelDisplacement = Vector3.zero;

                if (state.confirmed && _activeInstance != null)
                {
                    // Use the actual tracked image world position for accurate distance
                    _activeInstance.transform.position = state.anchorPosition;
                    float dist = Vector3.Distance(arCamera.position, img.transform.position);
                    float targetScale = state.anchorScale * (dist / Mathf.Max(state.anchorDistance, 0.001f));
                    state.currentScale = Mathf.Lerp(state.currentScale, targetScale, scaleSmoothSpeed * Time.deltaTime);
                    _activeInstance.transform.localScale = Vector3.one * Mathf.Max(state.currentScale, 0.001f);
                    anyConfirmed = true;
                    confirmedName = state.imageName;
                }
                else if (!state.confirmed)
                {
                    float progress = Mathf.Clamp01(state.scanTimer / scanConfirmTime);
                    if (progress > bestProgress) bestProgress = progress;
                    anyScanning = true;

                    if (!state.isLoading && state.scanTimer >= scanConfirmTime * 0.5f)
                        StartCoroutine(LoadPrefabAsync(state));

                    if (state.scanTimer >= scanConfirmTime && state.loadedPrefab != null)
                        Confirm(state);
                }
            }
            else
            {
                state.lostTimer += Time.deltaTime;

                if (state.confirmed && _activeInstance != null)
                {
                    // Use accelerometer to estimate distance change while card is out of frame
                    if (!_accelTracking)
                    {
                        _accelTracking = true;
                        _accelVelocity = Vector3.zero;
                        _accelDisplacement = Vector3.zero;
                    }

                    // Remove gravity component, integrate to get velocity then displacement
                    Vector3 accel = Input.acceleration - Physics.gravity.normalized * Vector3.Dot(Input.acceleration, Physics.gravity.normalized);
                    _accelVelocity    += accel * Time.deltaTime;
                    _accelDisplacement += _accelVelocity * Time.deltaTime;

                    float estimatedDist = Mathf.Max(state.anchorDistance + _accelDisplacement.magnitude, 0.01f);
                    float targetScale   = state.anchorScale * (estimatedDist / Mathf.Max(state.anchorDistance, 0.001f));
                    state.currentScale  = Mathf.Lerp(state.currentScale, targetScale, scaleSmoothSpeed * Time.deltaTime);
                    _activeInstance.transform.localScale = Vector3.one * Mathf.Max(state.currentScale, 0.001f);

                    anyConfirmed = true;
                    confirmedName = state.imageName;

                    // Original kill logic — destroy after grace time
                    if (state.lostTimer >= lostGraceTime)
                    {
                        DestroyActive();
                        state.confirmed = false;
                        state.scanTimer = 0f;
                    }
                }
                else if (!state.confirmed)
                {
                    if (state.lostTimer >= lostGraceTime)
                        state.scanTimer = 0f;
                }
            }
        }

if (anyConfirmed)     SetStatusConfirmed(confirmedName);
        else if (anyScanning) SetStatusScanning(bestProgress);
        else                  SetStatusIdle();
    }

    // ── Async Load ────────────────────────────────────────────────────────────

    IEnumerator LoadPrefabAsync(TrackedState state)
    {
        state.isLoading = true;
        string path = ResourcesPath + state.imageName;
        Debug.Log($"[ARImageTracker] Starting async load: Resources/{path}");

        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
        yield return request;

        if (request.asset == null)
        {
            Debug.LogError($"[ARImageTracker] FAILED to load 'Resources/{path}'. " +
                           $"Ensure the prefab exists at Assets/Resources/{path}.prefab " +
                           $"and the name matches exactly (case-sensitive).");
            yield break;
        }

        state.loadedPrefab = (GameObject)request.asset;
        Debug.Log($"[ARImageTracker] Successfully loaded '{path}'. " +
                  $"scanTimer={state.scanTimer:F2}, scanConfirmTime={scanConfirmTime:F2}");

        // If the timer already passed while loading, confirm immediately
        if (state.scanTimer >= scanConfirmTime && !state.confirmed)
        {
            Debug.Log($"[ARImageTracker] Timer already passed — confirming immediately.");
            Confirm(state);
        }
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    void Confirm(TrackedState state)
    {
        state.confirmed = true;
        DestroyActive();

        // Snapshot the world position and distance at the moment of scan
        Vector3 spawnPos = arCamera.position + arCamera.forward * spawnDistance;
        float scale = GetScale(state.imageName);

        float actualDist = arCamera != null
            ? Vector3.Distance(arCamera.position, spawnPos)
            : spawnDistance;

        state.anchorPosition = spawnPos;
        state.anchorDistance = Mathf.Max(actualDist, 0.01f);
        state.anchorScale    = scale;
        state.currentScale   = scale;

        _activeInstance = Instantiate(state.loadedPrefab, spawnPos, Quaternion.identity);
        _activeInstance.transform.localScale = Vector3.one * scale;

        if (scannedImages.Add(state.imageName))
            OnNewImageScanned?.Invoke(state.imageName);
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
