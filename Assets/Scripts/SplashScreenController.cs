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
    [SerializeField] private AudioClip introLine;

    [Header("UI")]
    [SerializeField] private Button tapToContinueButton;
    [Tooltip("Optional — if assigned, this text/image fades in after the voice line finishes")]
    [SerializeField] private GameObject continuePrompt;

    [Header("Settings")]
    [Tooltip("Player can tap to dismiss before the voice line finishes")]
    [SerializeField] private bool allowSkip = true;
    [Tooltip("Seconds to fade out the splash panel when dismissed")]
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private bool _dismissed = false;

    void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

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

        Debug.Log($"[Splash] AudioSource: {audioSource != null} | IntroLine: {introLine != null}");
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        if (introLine != null)
        {
            audioSource.PlayOneShot(introLine);
            yield return new WaitForSeconds(introLine.length);
        }

        // Voice line finished — show continue prompt and enable tap if skip was off
        if (continuePrompt != null)
            continuePrompt.SetActive(true);

        tapToContinueButton.interactable = true;
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
