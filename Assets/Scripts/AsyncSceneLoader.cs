using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Singleton loading screen — persists between scenes.
// Call AsyncSceneLoader.Load("SceneName") from anywhere instead of SceneManager.LoadScene.
// Shows a fullscreen overlay with a progress bar while the scene loads async.
public class AsyncSceneLoader : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Background color of the loading overlay (#FEF9F3)")]
    public Color backgroundColor = new Color(254f/255f, 249f/255f, 243f/255f, 1f);
    [Tooltip("Accent color used for the bar fill and text (#E3994B)")]
    public Color accentColor = new Color(227f/255f, 153f/255f, 75f/255f, 1f);
    [Tooltip("Seconds to fade the overlay in/out")]
    public float fadeDuration = 0.3f;

    [Header("Font")]
    [Tooltip("Assign the PaytoneOne TMP Font Asset here")]
    public TMP_FontAsset loadingFont;

    public static AsyncSceneLoader Instance { get; private set; }

    // True while the loading overlay is on screen.
    public static bool IsShowing => Instance != null && Instance._loading;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Image _barFill;
    private TextMeshProUGUI _loadingText;
    private bool _loading = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        _canvasGroup.alpha = 0f;
        _canvas.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static void Load(string sceneName)
    {
        EnsureInstance();
        Instance.StartLoad(sceneName, -1);
    }

    public static void Load(int buildIndex)
    {
        EnsureInstance();
        Instance.StartLoad(null, buildIndex);
    }

    // Silently pre-warms one scene in the background.
    // Only called from MenuManager.Start() — do not add calls elsewhere.
    public static void Preload(string sceneName)
    {
        EnsureInstance();
        var inst = Instance;
        if (inst._preloadedSceneName == sceneName) return;
        if (inst._preloadOp != null) return;
        inst._preloadedSceneName = sceneName;
        inst._preloadOp = SceneManager.LoadSceneAsync(sceneName);
        inst._preloadOp.allowSceneActivation = false;
        inst.StartCoroutine(inst.PreloadRoutine());
    }

    private AsyncOperation _preloadOp;
    private string _preloadedSceneName;

    IEnumerator PreloadRoutine()
    {
        while (_preloadOp != null && _preloadOp.progress < 0.9f)
            yield return null;
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("AsyncSceneLoader");
        Instance = go.AddComponent<AsyncSceneLoader>();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void StartLoad(string sceneName, int buildIndex)
    {
        if (_loading) return;
        _loading = true;
        StartCoroutine(LoadRoutine(sceneName, buildIndex));
    }

    IEnumerator LoadRoutine(string sceneName, int buildIndex)
    {
        _canvas.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));

        // Start unloading previous scene's assets immediately — runs in background
        // in parallel with the new scene load so cleanup doesn't block the player.
        Resources.UnloadUnusedAssets();

        // Use preloaded op if it matches, otherwise fresh load.
        AsyncOperation op;
        if (sceneName != null && sceneName == _preloadedSceneName && _preloadOp != null)
        {
            op = _preloadOp;
            _preloadOp = null;
            _preloadedSceneName = null;
        }
        else
        {
            // If a preload is pending for a different scene, Unity queues any new
            // LoadSceneAsync behind it. We must activate and drain it first.
            // The loading overlay is already fully visible so the player won't see it.
            if (_preloadOp != null)
            {
                _preloadOp.allowSceneActivation = true;
                while (!_preloadOp.isDone) yield return null;
                _preloadOp = null;
                _preloadedSceneName = null;
            }

            op = buildIndex >= 0
                ? SceneManager.LoadSceneAsync(buildIndex)
                : SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
        }

        while (op.progress < 0.9f)
        {
            _barFill.fillAmount = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        // Scene is ready — activate immediately and reveal it.
        // Asset cleanup from the previous scene continues in the background.
        _barFill.fillAmount = 1f;
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        yield return null; // one frame for initial GPU uploads

        yield return StartCoroutine(Fade(1f, 0f));
        _canvas.gameObject.SetActive(false);
        _loading = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("LoadingCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = backgroundColor;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var labelGO = new GameObject("LoadingLabel");
        labelGO.transform.SetParent(canvasGO.transform, false);
        _loadingText = labelGO.AddComponent<TextMeshProUGUI>();
        _loadingText.text = "Loading...";
        _loadingText.color = accentColor;
        _loadingText.fontSize = 72;
        _loadingText.alignment = TextAlignmentOptions.Center;
        if (loadingFont != null) _loadingText.font = loadingFont;
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.1f, 0.52f);
        labelRect.anchorMax = new Vector2(0.9f, 0.62f);
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        var track = new GameObject("BarTrack");
        track.transform.SetParent(canvasGO.transform, false);
        var trackImg = track.AddComponent<Image>();
        trackImg.color = new Color(220f/255f, 200f/255f, 178f/255f, 1f);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.1f, 0.44f);
        trackRect.anchorMax = new Vector2(0.9f, 0.50f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

        var fill = new GameObject("BarFill");
        fill.transform.SetParent(track.transform, false);
        _barFill = fill.AddComponent<Image>();
        _barFill.color = accentColor;
        _barFill.type = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillAmount = 0f;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
    }
}
