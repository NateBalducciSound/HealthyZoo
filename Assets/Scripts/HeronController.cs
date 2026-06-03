using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeronLevelController : MonoBehaviour
{
    [System.Serializable]
    public class HeronSpriteSet
    {
        public SpriteRenderer renderer;
        public Sprite stateZero;
        public Sprite stateOne;
        public Sprite stateTwo;

        public Sprite GetSprite(int index)
        {
            switch (index)
            {
                case 0: return stateZero;
                case 1: return stateOne;
                case 2: return stateTwo;
                default: return stateOne;
            }
        }
    }

    [Header("Slider Settings")]
    public Slider positionSlider; // Min: 0, Max: 2, Whole Numbers: True

    [Header("Sprites (2 sets, 3 states each)")]
    public HeronSpriteSet[] spriteSets;     // Assign 2 in inspector
    [Min(0f)]
    public float crossfadeDuration = 0.3f;  // Seconds per crossfade

    [Header("Heartbeat Sensor Animation")]
    public Animator heartbeatAnimator;
    [Tooltip("Animator speed when slider = 0 (too fast)")]
    public float heartbeatSpeedFast   = 2.0f;
    [Tooltip("Animator speed when slider = 1 (just right)")]
    public float heartbeatSpeedNormal = 1.0f;
    [Tooltip("Animator speed when slider = 2 (too slow)")]
    public float heartbeatSpeedSlow   = 0.4f;

    [Header("Heartbeat Audio")]
    public AudioClip heartbeatClipFast;
    public AudioClip heartbeatClipNormal;
    public AudioClip heartbeatClipSlow;
    [Range(0f, 1f)] public float heartbeatVolume = 1f;
    [Range(0f, 1f)]
    [Tooltip("Normalized position in the animation cycle to play the sound (0=start, 0.5=middle, 1=end)")]
    public float heartbeatAudioOffset = 0.5f;

    [Header("Stethoscope Drop Animation")]
    public Animator stethoscopeAnimator;
    [Tooltip("Exact name of the Startup state in the Animator Controller")]
    public string startupStateName = "Startup";
    [Tooltip("Exact name of the Loop state in the Animator Controller")]
    public string loopStateName = "Loop";

    [Header("Confetti")]
    public Animator confettiAnimator;
    [Tooltip("Exact name of the Confetti state in the Animator Controller")]
    public string confettiStateName = "Confetti";

    [Header("Dialogue")]
    public DialogueController dialogueController;

    [Header("Toolbar")]
    public Image toolbarImage;
    public SpriteRenderer toolbarSprite;
    [Min(0f)]
    public float toolbarFadeDuration = 0.5f;

    [Header("UI & Navigation")]
    public GameObject congratsPrefab;
    public Transform buttonParent;
    public Vector3 congratsScale = Vector3.one;
    public Vector3 congratsPosition = Vector3.zero;
    public string menuSceneName = "_02MiniGameSelect";

    private bool isFinished = false;
    private bool _playerStarted = false;
    private int currentSliderIndex = -1;
    private Coroutine crossfadeCoroutine;
    private Coroutine heartbeatCoroutine;
    private SpriteRenderer[] overlayRenderers;
    private AudioSource _heartbeatSource;

    void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName(menuSceneName));

        _heartbeatSource = gameObject.AddComponent<AudioSource>();
        _heartbeatSource.playOnAwake = false;

        // Disable animator so it doesn't overwrite sprites while slider is active
        if (stethoscopeAnimator) stethoscopeAnimator.enabled = false;

        if (positionSlider != null)
        {
            positionSlider.onValueChanged.AddListener(OnSliderChanged);

            // Set initial sprites instantly with no fade
            int startIndex = Mathf.RoundToInt(positionSlider.value);
            currentSliderIndex = startIndex;
            foreach (var set in spriteSets)
                if (set.renderer) set.renderer.sprite = set.GetSprite(startIndex);
        }

        if (dialogueController != null)
        {
            positionSlider.interactable = false;
            dialogueController.OnInputUnlocked += () => positionSlider.interactable = true;
        }


        // Create overlay renderers for crossfading
        overlayRenderers = new SpriteRenderer[spriteSets.Length];
        for (int i = 0; i < spriteSets.Length; i++)
        {
            var src = spriteSets[i].renderer;
            if (src == null) continue;
            var go = new GameObject("OverlayRenderer_" + i);
            go.transform.SetParent(src.transform.parent);
            go.transform.localPosition = src.transform.localPosition;
            go.transform.localScale = src.transform.localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder + 1;
            sr.color = new Color(1f, 1f, 1f, 0f);
            overlayRenderers[i] = sr;
        }
    }

    void OnDestroy()
    {
        if (positionSlider != null)
            positionSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    // ─── Slider ───────────────────────────────────────────────────────────────

    void OnSliderChanged(float value)
    {
        if (isFinished) return;

        if (!_playerStarted)
        {
            _playerStarted = true;
            if (dialogueController != null) dialogueController.OnPlayerStarted();
        }

        int index = Mathf.RoundToInt(value);
        if (index == currentSliderIndex) return;
        currentSliderIndex = index;

        if (index == 1) AudioManager.PlayCorrect();
        else            AudioManager.PlayIncorrect();

        // Restart heartbeat audio loop for new slider position
        if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop(index));

        // Crossfade sprites
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeSprites(index));

        // Update heartbeat animation speed
        if (heartbeatAnimator != null)
        {
            heartbeatAnimator.speed = index == 0 ? heartbeatSpeedFast
                                    : index == 2 ? heartbeatSpeedSlow
                                    : heartbeatSpeedNormal;
        }
    }

    // ─── Heartbeat Audio Loop ──────────────────────────────────────────────────

    IEnumerator HeartbeatLoop(int sliderIndex)
    {
        AudioClip clip = sliderIndex == 0 ? heartbeatClipFast
                       : sliderIndex == 2 ? heartbeatClipSlow
                       : heartbeatClipNormal;

        if (clip == null || heartbeatAnimator == null) yield break;

        bool triggered = true; // skip until the next natural cycle crossing
        while (true)
        {
            float norm = heartbeatAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;

            if (!triggered && norm >= heartbeatAudioOffset)
            {
                _heartbeatSource.PlayOneShot(clip, heartbeatVolume);
                triggered = true;
            }
            else if (triggered && norm < heartbeatAudioOffset)
            {
                triggered = false;
            }

            yield return null;
        }
    }

    // ─── Sprite Crossfade ──────────────────────────────────────────────────────

    IEnumerator CrossfadeSprites(int index)
    {
        if (spriteSets == null || spriteSets.Length == 0) yield break;

        // Load new sprite onto overlays at zero alpha
        for (int i = 0; i < spriteSets.Length; i++)
        {
            if (overlayRenderers[i] == null) continue;
            overlayRenderers[i].sprite = spriteSets[i].GetSprite(index);
            SetAlpha(overlayRenderers[i], 0f);
        }

        // Fade old out, new in simultaneously
        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / crossfadeDuration);
            for (int i = 0; i < spriteSets.Length; i++)
            {
                if (spriteSets[i].renderer) SetAlpha(spriteSets[i].renderer, 1f - progress);
                if (overlayRenderers[i] != null) SetAlpha(overlayRenderers[i], progress);
            }
            yield return null;
        }

        // Finalise — move new sprite onto primary renderer, clear overlay
        for (int i = 0; i < spriteSets.Length; i++)
        {
            if (spriteSets[i].renderer)
            {
                spriteSets[i].renderer.sprite = spriteSets[i].GetSprite(index);
                SetAlpha(spriteSets[i].renderer, 1f);
            }
            if (overlayRenderers[i] != null) SetAlpha(overlayRenderers[i], 0f);
        }
    }

    static void SetAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    // ─── Drop / Success ────────────────────────────────────────────────────────

    public void ProcessHeronDrop(Draggable2D item)
    {
        if (Mathf.RoundToInt(positionSlider.value) == 1 && !isFinished)
        {
            isFinished = true;
            StartCoroutine(SuccessSequence(item.gameObject));
        }
        else
        {
            item.ReturnToStart();
            AudioManager.PlayIncorrect();
        }
    }

    public void ProcessHeronDrop(UIDraggable item)
    {
        if (Mathf.RoundToInt(positionSlider.value) == 1 && !isFinished)
        {
            isFinished = true;
            StartCoroutine(SuccessSequence(item.gameObject));
        }
        else
        {
            item.ReturnToStart();
            AudioManager.PlayIncorrect();
        }
    }

    IEnumerator FadeOutToolbar()
    {
        if (toolbarImage == null && toolbarSprite == null) yield break;

        float elapsed = 0f;
        float imageStart  = toolbarImage  != null ? toolbarImage.color.a  : 0f;
        float spriteStart = toolbarSprite != null ? toolbarSprite.color.a : 0f;

        while (elapsed < toolbarFadeDuration)
        {
            elapsed += Time.deltaTime;
            if (toolbarImage != null)
            {
                Color c = toolbarImage.color;
                c.a = Mathf.Lerp(imageStart, 0f, elapsed / toolbarFadeDuration);
                toolbarImage.color = c;
            }
            if (toolbarSprite != null)
            {
                Color c = toolbarSprite.color;
                c.a = Mathf.Lerp(spriteStart, 0f, elapsed / toolbarFadeDuration);
                toolbarSprite.color = c;
            }
            yield return null;
        }

        if (toolbarImage  != null) toolbarImage.gameObject.SetActive(false);
        if (toolbarSprite != null) toolbarSprite.gameObject.SetActive(false);
    }

    IEnumerator SuccessSequence(GameObject itemGO)
    {
        if (heartbeatCoroutine != null) { StopCoroutine(heartbeatCoroutine); heartbeatCoroutine = null; }
        itemGO.SetActive(false);
        positionSlider.interactable = false;
        StartCoroutine(FadeOutToolbar());

        foreach (var set in spriteSets)
            if (set.renderer) set.renderer.sprite = set.GetSprite(1);

        MiniGameProgress.SetCompleted(GrabbableType.Heron);

        if (confettiAnimator != null) confettiAnimator.Play(confettiStateName);
        AudioManager.PlayComplete();

        stethoscopeAnimator.enabled = true;
        stethoscopeAnimator.Play(startupStateName);

        // Dialogue fires 1s after win (runs alongside animations).
        StartCoroutine(DialogueThenButton());

        yield return null;
        float clipLength = stethoscopeAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        stethoscopeAnimator.Play(loopStateName);
    }

    IEnumerator DialogueThenButton()
    {
        yield return new WaitForSeconds(1f);
        if (dialogueController != null) dialogueController.OnLevelCompleted();
        yield return new WaitForSeconds(dialogueController != null ? dialogueController.CompletionLineDuration : 0f);
        SpawnMenuButton();
    }

    void SpawnMenuButton()
    {
        if (congratsPrefab == null || buttonParent == null) return;

        GameObject spawned = Instantiate(congratsPrefab, buttonParent);
        if (spawned.TryGetComponent(out RectTransform rt))
        {
            rt.localPosition = congratsPosition;
            rt.localRotation = Quaternion.identity;
            rt.localScale = congratsScale;
        }

        // Wire up the button to SceneLoader if present
        if (spawned.TryGetComponent(out Button btn) && spawned.TryGetComponent(out SceneLoader loader))
            btn.onClick.AddListener(loader.OnPlayPressed);
    }

    public void OnBackToMenuPressed()
    {
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName(menuSceneName));
    }
}
