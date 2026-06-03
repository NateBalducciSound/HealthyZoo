using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Loading Screen")]
    [Tooltip("Assign the PaytoneOne TMP Font Asset here")]
    public TMP_FontAsset loadingFont;

    [Header("Menu Reveal Voice Line")]
    [SerializeField] private AudioSource revealAudioSource;
    [SerializeField] private LocalizedClip revealLine;
    [SerializeField] private LocalizedCaption revealCaption;
    [SerializeField] private GameObject revealCaptionBox;
    [SerializeField] private float revealCaptionLingerTime = 1f;

    private bool _isLoading = false;

    public void PlayRevealLine()
    {
        AudioClip clip = LanguageManager.Resolve(revealLine);
        if (clip == null || revealAudioSource == null) return;
        revealAudioSource.PlayOneShot(clip);

        if (revealCaptionBox != null && LanguageManager.ShowCaptions)
        {
            string text = LanguageManager.ResolveCaption(revealCaption);
            if (!string.IsNullOrEmpty(text))
            {
                var tmp = revealCaptionBox.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmp != null) tmp.text = text;
                revealCaptionBox.SetActive(true);
                StartCoroutine(ClearRevealCaption(clip.length + revealCaptionLingerTime));
            }
        }
    }

    IEnumerator ClearRevealCaption(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (revealCaptionBox != null) revealCaptionBox.SetActive(false);
    }

    public void PlayGame()
    {
        if (_isLoading) return;
        _isLoading = true;
        StartCoroutine(PlayGameRoutine());
    }

    IEnumerator PlayGameRoutine()
    {
        // Show overlay at full opacity immediately.
        BuildOverlay(out CanvasGroup cg, out Image bar);
        cg.alpha = 1f;
        bar.fillAmount = 1f;

        // Two frames so the overlay actually renders before the sync load freezes.
        yield return null;
        yield return null;

        SceneManager.LoadScene(DeviceDetector.GetSceneName("_02MiniGameSelect"));
    }

    public void ScannerLoad()
    {
        if (_isLoading) return;
        StartCoroutine(DoLoad("AR_Prototype"));
    }

    public void LoadHome()
    {
        if (_isLoading) return;
        StartCoroutine(DoLoad(0));
    }

    public void QuitButton()
    {
        Application.Quit();
    }


    Canvas BuildOverlay(out CanvasGroup cg, out Image barFill)
    {
        var bgColor  = new Color(254f/255f, 249f/255f, 243f/255f, 1f);
        var accent   = new Color(227f/255f, 153f/255f,  75f/255f, 1f);
        var trackCol = new Color(220f/255f, 200f/255f, 178f/255f, 1f);

        var go = new GameObject("MGSLoadOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)go.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1080, 1920);
        cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Background
        var bg = new GameObject("Bg"); bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = bgColor;
        Stretch(bg);

        // Loading label
        var label = new GameObject("Label"); label.transform.SetParent(go.transform, false);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Loading...";
        tmp.color     = accent;
        tmp.fontSize  = 72;
        tmp.alignment = TextAlignmentOptions.Center;
        if (loadingFont != null) tmp.font = loadingFont;
        SetAnchors(label, 0.1f, 0.52f, 0.9f, 0.62f);

        // Bar track
        var track = new GameObject("Track"); track.transform.SetParent(go.transform, false);
        var trackImg = track.AddComponent<Image>(); trackImg.color = trackCol;
        SetAnchors(track, 0.1f, 0.44f, 0.9f, 0.50f);

        // Bar fill
        var fill = new GameObject("Fill"); fill.transform.SetParent(track.transform, false);
        barFill = fill.AddComponent<Image>();
        barFill.color      = accent;
        barFill.type       = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = 0f;
        Stretch(fill);

        return canvas;
    }

    void Stretch(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    IEnumerator FadeOverlay(CanvasGroup cg, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    // ── Generic loads (no overlay needed) ────────────────────────────────────

    IEnumerator DoLoad(string sceneName)
    {
        _isLoading = true;
        while (AsyncSceneLoader.IsShowing) yield return null;
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null) { _isLoading = false; yield break; }
        while (!op.isDone) yield return null;
    }

    IEnumerator DoLoad(int buildIndex)
    {
        _isLoading = true;
        while (AsyncSceneLoader.IsShowing) yield return null;
        var op = SceneManager.LoadSceneAsync(buildIndex);
        if (op == null) { _isLoading = false; yield break; }
        while (!op.isDone) yield return null;
    }
}
