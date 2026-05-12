using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent singleton — place in any scene where music should start.
// Fades out when leaving a scene and fades back in when returning to a music scene.
[RequireComponent(typeof(AudioSource))]
public class ARMusicPlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip musicClip;
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Header("Fade")]
    public float fadeInDuration  = 1f;
    public float fadeOutDuration = 0.8f;

    [Header("Trigger")]
    [Tooltip("Play immediately when this scene loads, ignoring scan state.")]
    public bool playOnSceneLoad = false;
    [Tooltip("Scene names (partial match) where music should fade back in on load. E.g. '02MiniGameSelect'")]
    public string[] musicSceneNames = new[] { "02MiniGameSelect" };

    // ── Singleton ─────────────────────────────────────────────────────────────

    private static ARMusicPlayer _instance;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        Debug.Log($"[ARMusicPlayer] Awake — audioSource:{audioSource != null}  clip:{musicClip != null}  playOnSceneLoad:{playOnSceneLoad}");
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        Debug.Log($"[ARMusicPlayer] Start — isSingleton:{_instance == this}  playOnSceneLoad:{playOnSceneLoad}");
        if (_instance != this) return;
        if (playOnSceneLoad || ARImageTracker.ScannedImages.Count > 0)
            TriggerFadeIn();
    }

    void OnEnable()
    {
        ARImageTracker.OnNewImageScanned  += OnFirstScan;
        AsyncSceneLoader.OnBeforeLoad     += OnBeforeLoad;
        SceneManager.sceneLoaded          += OnSceneLoaded;
    }

    void OnDisable()
    {
        ARImageTracker.OnNewImageScanned  -= OnFirstScan;
        AsyncSceneLoader.OnBeforeLoad     -= OnBeforeLoad;
        SceneManager.sceneLoaded          -= OnSceneLoaded;
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    void OnFirstScan(string imageName) => TriggerFadeIn();

    void OnBeforeLoad() => TriggerFadeOut();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var name in musicSceneNames)
            if (scene.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
            { TriggerFadeIn(); return; }

        // Scene isn't in the music list — fade out and stop.
        TriggerFadeOut();
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    private Coroutine _fadeRoutine;

    void TriggerFadeIn()
    {
        Debug.Log($"[ARMusicPlayer] TriggerFadeIn — audioSource:{audioSource != null}  clip:{musicClip != null}  isPlaying:{audioSource != null && audioSource.isPlaying}");
        if (audioSource == null || musicClip == null) return;
        if (!audioSource.isPlaying || audioSource.clip != musicClip)
        {
            audioSource.Stop();
            audioSource.clip = musicClip;
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.Play();
        }
        SetFade(audioSource.volume, musicVolume, fadeInDuration);
    }

    void TriggerFadeOut()
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        SetFade(audioSource.volume, 0f, fadeOutDuration);
    }

    void SetFade(float from, float to, float duration)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(from, to, duration));
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        audioSource.volume = to;
        if (to <= 0f) audioSource.Stop();
    }
}
