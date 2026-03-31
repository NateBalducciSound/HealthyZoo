using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Singleton loading screen — persists between scenes.
// Call AsyncSceneLoader.Load("SceneName") from anywhere instead of SceneManager.LoadScene.
// Shows a fullscreen overlay with a progress bar while the scene loads async.
public class AsyncSceneLoader : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("Background color of the loading overlay")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
    [Tooltip("Fill color of the progress bar")]
    public Color barColor = new Color(0.2f, 0.75f, 0.35f, 1f);
    [Tooltip("Seconds to fade the overlay in/out")]
    public float fadeDuration = 0.3f;

    public static AsyncSceneLoader Instance { get; private set; }

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Image _barFill;
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
        Instance.StartLoad(sceneName);
    }

    public static void Load(int buildIndex)
    {
        EnsureInstance();
        Instance.StartLoad(buildIndex);
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("AsyncSceneLoader");
        Instance = go.AddComponent<AsyncSceneLoader>();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void StartLoad(string sceneName) => StartLoad(sceneName, -1);
    void StartLoad(int buildIndex) => StartLoad(null, buildIndex);

    void StartLoad(string sceneName, int buildIndex)
    {
        if (_loading) return;
        _loading = true;
        StartCoroutine(LoadRoutine(sceneName, buildIndex));
    }

    IEnumerator LoadRoutine(string sceneName, int buildIndex)
    {
        // Show overlay
        _canvas.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));

        // Begin async load — hold at 0.9 until we allow activation
        AsyncOperation op = buildIndex >= 0
            ? SceneManager.LoadSceneAsync(buildIndex)
            : SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            _barFill.fillAmount = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        // Snap bar to full, brief pause so it registers visually
        _barFill.fillAmount = 1f;
        yield return new WaitForSecondsRealtime(0.15f);

        // Activate the scene
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // Fade out overlay
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
        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Full-screen background
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = backgroundColor;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // Bar background track
        var track = new GameObject("BarTrack");
        track.transform.SetParent(canvasGO.transform, false);
        var trackImg = track.AddComponent<Image>();
        trackImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.1f, 0.45f);
        trackRect.anchorMax = new Vector2(0.9f, 0.55f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

        // Bar fill
        var fill = new GameObject("BarFill");
        fill.transform.SetParent(track.transform, false);
        _barFill = fill.AddComponent<Image>();
        _barFill.color = barColor;
        _barFill.type = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillAmount = 0f;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
    }
}
