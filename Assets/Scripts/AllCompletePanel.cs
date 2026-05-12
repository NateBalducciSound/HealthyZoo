using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach to the AllCompletePanel GameObject (needs a CanvasGroup).
// Shows when every minigame is complete. Plays an optional voice line that
// blocks dismissal until it finishes — if no clip is assigned, tap closes immediately.
[RequireComponent(typeof(CanvasGroup))]
public class AllCompletePanel : MonoBehaviour
{
    [Header("Fade")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 0.5f;
    public float fadeDelay = 0.5f;

    [Header("Voice Line")]
    [Tooltip("Optional — blocks tap-to-close until the clip finishes.")]
    public LocalizedClip voiceLine;
    public AudioSource audioSource;

    [Header("Close Button")]
    [Tooltip("Assign a full-screen invisible Button to catch taps, or leave null to use OnTapped() directly.")]
    public Button closeButton;

    private static readonly GrabbableType[] AllTypes =
    {
        GrabbableType.Giraffe,
        GrabbableType.Heron,
        GrabbableType.Porcupine,
        GrabbableType.Sloth,
        GrabbableType.Panda,
        GrabbableType.Alligator,
    };

    private CanvasGroup _cg;
    private bool _canDismiss = false;
    private bool _dismissed = false;

    void Start()
    {
        _cg = GetComponent<CanvasGroup>();
        _cg.alpha = 0f;
        _cg.blocksRaycasts = false;

        if (closeButton != null)
            closeButton.onClick.AddListener(OnTapped);

        if (AllCompleted() && !MiniGameProgress.HasShownAllComplete())
            StartCoroutine(ShowPanel());
    }

    static bool AllCompleted()
    {
        foreach (var t in AllTypes)
            if (!MiniGameProgress.IsCompleted(t)) return false;
        return true;
    }

    // Wire this to the close button's OnClick, or call from a UI Button in the scene.
    public void OnTapped()
    {
        if (!_canDismiss || _dismissed) return;
        _dismissed = true;
        if (audioSource != null) audioSource.Stop();
        StartCoroutine(FadeOut());
    }

    IEnumerator ShowPanel()
    {
        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _cg.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        MiniGameProgress.SetAllCompleteShown();

        // Play voice line and lock dismissal until it finishes.
        AudioClip resolved = LanguageManager.Resolve(voiceLine);
        if (resolved != null && audioSource != null)
        {
            audioSource.PlayOneShot(resolved);
            yield return new WaitForSeconds(resolved.length);
        }

        _canDismiss = true;
    }

    IEnumerator FadeOut()
    {
        _cg.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        _cg.alpha = 0f;
        gameObject.SetActive(false);
    }
}
