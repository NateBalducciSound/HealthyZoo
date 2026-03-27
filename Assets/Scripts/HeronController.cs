using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

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

    [Header("UI & Navigation")]
    public GameObject congratsPrefab;
    public Transform buttonParent;
    public Vector3 congratsScale = Vector3.one;
    public Vector3 congratsPosition = Vector3.zero;
    public string menuSceneName = "_02MiniGameSelect";

    private bool isFinished = false;
    private int currentSliderIndex = -1;
    private Coroutine crossfadeCoroutine;

    void Start()
    {
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

        int index = Mathf.RoundToInt(value);
        if (index == currentSliderIndex) return;
        currentSliderIndex = index;

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

    // ─── Sprite Crossfade ──────────────────────────────────────────────────────

    IEnumerator CrossfadeSprites(int index)
    {
        if (spriteSets == null || spriteSets.Length == 0) yield break;

        float halfDuration = crossfadeDuration * 0.5f;

        // Fade out from wherever alpha currently is
        float startAlpha = spriteSets[0].renderer ? spriteSets[0].renderer.color.a : 1f;
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, t / halfDuration);
            foreach (var set in spriteSets)
                if (set.renderer) SetAlpha(set.renderer, alpha);
            yield return null;
        }

        // Swap sprites at zero alpha
        foreach (var set in spriteSets)
        {
            if (set.renderer)
            {
                set.renderer.sprite = set.GetSprite(index);
                SetAlpha(set.renderer, 0f);
            }
        }

        // Fade in
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / halfDuration);
            foreach (var set in spriteSets)
                if (set.renderer) SetAlpha(set.renderer, alpha);
            yield return null;
        }

        foreach (var set in spriteSets)
            if (set.renderer) SetAlpha(set.renderer, 1f);
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
        Debug.Log($"[HeronController] ProcessHeronDrop called. Slider value: {positionSlider.value}, isFinished: {isFinished}");
        if (Mathf.RoundToInt(positionSlider.value) == 1 && !isFinished)
        {
            isFinished = true;
            StartCoroutine(SuccessSequence(item));
        }
        else
        {
            item.ReturnToStart();
            Debug.Log($"[HeronController] Incorrect position (slider={positionSlider.value}) or already finished.");
        }
    }

    IEnumerator SuccessSequence(Draggable2D item)
    {
        item.gameObject.SetActive(false);
        positionSlider.interactable = false;

        // Lock both sprites to state 1 (just right) — animator drives them from here
        foreach (var set in spriteSets)
            if (set.renderer) set.renderer.sprite = set.GetSprite(1);

        // Save progress
        MiniGameProgress.SetCompleted(GrabbableType.Heron);

        // Enable animator and play startup directly
        stethoscopeAnimator.enabled = true;
        stethoscopeAnimator.Play(startupStateName);

        // Wait one frame for animator to register the Play call
        yield return null;

        // Read the clip length and wait for it to finish
        float clipLength = stethoscopeAnimator.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log($"[HeronController] Startup clip length: {clipLength}s");
        yield return new WaitForSeconds(clipLength);

        stethoscopeAnimator.Play(loopStateName);
        Debug.Log("[HeronController] Loop started");

        // Confetti plays once when loop begins
        if (confettiAnimator != null)
            confettiAnimator.Play(confettiStateName);

        // Spawn back-to-menu button
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
        SceneManager.LoadScene(menuSceneName);
    }
}
