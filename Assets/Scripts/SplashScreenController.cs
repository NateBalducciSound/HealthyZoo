using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Attach to the SplashPanel GameObject.
// The panel covers the full screen on scene load, plays a voice line,
// then lets the player tap anywhere to dismiss it and reveal the main menu.

public class SplashScreenController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LocalizedClip introLine;

    [Header("UI")]
    [SerializeField] private Button tapToContinueButton;
    [Tooltip("Optional — if assigned, this text/image fades in after the voice line finishes")]
    [SerializeField] private GameObject continuePrompt;

    [Header("Device Sprites")]
    [SerializeField] private Image splashImage;
    [SerializeField] private Sprite phoneSprite;
    [SerializeField] private Sprite ipadSprite;

    [Header("Inactivity Prompt")]
    [SerializeField] private LocalizedClip inactivityPrompt;
    [Tooltip("Seconds of silence after intro finishes before playing inactivity prompt")]
    [SerializeField] private float inactivityDelay = 2f;
    [Tooltip("If true, repeats the inactivity prompt on this interval until tapped")]
    [SerializeField] private bool loopInactivityPrompt = true;

    [Header("Settings")]
    [Tooltip("Player can tap to dismiss before the voice line finishes")]
    [SerializeField] private bool allowSkip = true;
    [Tooltip("Seconds to fade out the splash panel when dismissed")]
    [SerializeField] private float fadeDuration = 0.5f;

    // Static: survives scene loads within the same app session, resets on cold start
    private static bool _shownThisSession = false;

    private CanvasGroup _canvasGroup;
    private bool _dismissed = false;

    void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Skip if already shown this session (e.g. returning from a mini game)
        if (_shownThisSession)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            return;
        }

        _shownThisSession = true;

        // Swap to the correct sprite for this device.
        if (splashImage != null)
        {
            var sprite = DeviceDetector.IsTablet ? ipadSprite : phoneSprite;
            if (sprite != null) splashImage.sprite = sprite;
        }

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        if (tapToContinueButton == null)
        {
            Debug.LogError("[Splash] tapToContinueButton is not assigned in the Inspector");
            return;
        }

        tapToContinueButton.onClick.AddListener(OnTapped);
        Debug.Log("[Splash] Button listener registered");

        if (allowSkip)
            tapToContinueButton.interactable = true;
        else
            tapToContinueButton.interactable = false;

        Debug.Log($"[Splash] AudioSource: {audioSource != null} | IntroLine: {introLine.english != null}");
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        // Wait for the loading overlay to fully fade out before playing audio.
        while (AsyncSceneLoader.IsShowing)
            yield return null;

        // Only play if the splash sprite is actually visible on screen.
        bool spriteVisible = splashImage != null
                             && splashImage.enabled
                             && splashImage.gameObject.activeInHierarchy
                             && _canvasGroup.alpha > 0f;

        AudioClip resolvedIntro = LanguageManager.Resolve(introLine);
        if (resolvedIntro != null && spriteVisible)
        {
            audioSource.PlayOneShot(resolvedIntro);
            yield return new WaitForSeconds(resolvedIntro.length);
        }

        // Voice line finished — show continue prompt and enable tap if skip was off
        if (continuePrompt != null)
            continuePrompt.SetActive(true);

        tapToContinueButton.interactable = true;

        if (LanguageManager.Resolve(inactivityPrompt) != null)
            StartCoroutine(InactivityLoop());

    }

    IEnumerator InactivityLoop()
    {
        do
        {
            yield return new WaitForSeconds(inactivityDelay);
            if (_dismissed) yield break;
            AudioClip resolved = LanguageManager.Resolve(inactivityPrompt);
            if (resolved != null)
            {
                audioSource.PlayOneShot(resolved);
                if (loopInactivityPrompt)
                    yield return new WaitForSeconds(resolved.length);
            }
        }
        while (loopInactivityPrompt && !_dismissed);
    }

    void OnTapped()
    {
        Debug.Log("[Splash] Tapped");
        if (_dismissed) return;
        _dismissed = true;

        audioSource.Stop();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}
