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
    [Tooltip("Assign the PaytoneOne TMP Font Asset here (generate it from the TTF via Window > TextMeshPro > Font Asset Creator)")]
    public TMP_FontAsset loadingFont;

    public static AsyncSceneLoader Instance { get; private set; }

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
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // Full-screen background (#FEF9F3)
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = backgroundColor;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // "Loading..." label — centered, slightly above the bar
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

        // Bar track — warm tint of the background
        var track = new GameObject("BarTrack");
        track.transform.SetParent(canvasGO.transform, false);
        var trackImg = track.AddComponent<Image>();
        trackImg.color = new Color(220f/255f, 200f/255f, 178f/255f, 1f);
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.1f, 0.44f);
        trackRect.anchorMax = new Vector2(0.9f, 0.50f);
        trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;

        // Bar fill (#E3994B)
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
