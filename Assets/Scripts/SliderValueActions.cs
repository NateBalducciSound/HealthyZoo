using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderValueActions : MonoBehaviour
{
    [Header("Slider Settings")]
    public Slider slider;
    public float[] snapValues;
    // New: Reference to the UI handle image to hide it
    public Image uiSliderHandle; 

    [Header("Character Sprites")]
    public SpriteRenderer characterRenderer;
    public Sprite[] stageSprites;

    [Header("1. Startup Animation")]
    public Animator startAnimator;
    public string startAnimationState;

    [Header("2. Loop Animation")]
    public Animator loopAnimator;
    public string loopAnimationState;
    
    [Header("Loop Displacement")]
    public Vector3 loopPositionOffset;

    [Header("3. Overlay Animation")]
    public Animator confettiAnimator;
    public string confettiAnimationState;
    [Tooltip("Seconds after loop animation starts before confetti plays")]
    public float confettiDelay = 1f;

    [Header("Completion Swap")]
    [Tooltip("The non-UI sprite copy of the handle that renders in front of the giraffe")]
    public GameObject worldSpaceHandleCopy;

    [Header("Dialogue")]
    public DialogueController dialogueController;
    [Tooltip("Seconds after startup animation begins before congrats button spawns and line plays")]
    public float congratsDelay = 2f;

    [Header("UI Transition")]
    public Transform buttonParent;
    public GameObject congratsPrefab;
    public Vector3 congratsScale = Vector3.one;
    public Vector3 congratsPosition = Vector3.zero;
    public GrabbableType grabbableType;

    private int currentStage = -1;
    private bool completionTriggered = false;

    void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName("_02MiniGameSelect"));

        if (startAnimator) startAnimator.gameObject.SetActive(false);
        if (loopAnimator) loopAnimator.gameObject.SetActive(false);
        if (confettiAnimator) confettiAnimator.gameObject.SetActive(false);
        if (worldSpaceHandleCopy) worldSpaceHandleCopy.SetActive(false);

        slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateCharacterSprite(GetStageFromSnapValues(slider.value));
    }

    void OnSliderChanged(float value)
    {
        if (dialogueController != null)
            dialogueController.OnPlayerStarted();

        int stage = GetStageFromSnapValues(value);
        if (stage == currentStage) return;

        currentStage = stage;
        UpdateCharacterSprite(stage);

        if (stage == stageSprites.Length - 1 && !completionTriggered)
        {
            completionTriggered = true;
            StartCoroutine(PlayAnimationSequence());
        }
    }

    IEnumerator PlayAnimationSequence()
    {
        // --- NEW SWAP LOGIC ---
        // 1. Hide the UI handle and show the World Space copy
        if (uiSliderHandle != null) uiSliderHandle.enabled = false;
        if (worldSpaceHandleCopy != null) worldSpaceHandleCopy.SetActive(true);

        // 2. Hide the static height-check sprite (The Giraffe)
        if (characterRenderer != null)
        {
            characterRenderer.gameObject.SetActive(false);
        }

        // 3. Play Startup Animation (Giraffe starts moving)
        // Also kick off the congrats spawn timer at the same moment
        if (startAnimator != null)
        {
            if (characterRenderer != null)
                startAnimator.transform.localScale = characterRenderer.transform.localScale;
            startAnimator.gameObject.SetActive(true);
            startAnimator.Play(startAnimationState, 0, 0f);
            StartCoroutine(DelayedCongrats());
            yield return StartCoroutine(WaitForAnimationFinish(startAnimator, startAnimationState));
        }

        // 4. SWITCH: Move Loop Animator, Start it, and HIDE Startup Animator
        if (loopAnimator != null)
        {
            if (characterRenderer != null)
                loopAnimator.transform.localScale = characterRenderer.transform.localScale;
            loopAnimator.transform.localPosition = loopPositionOffset;
            loopAnimator.gameObject.SetActive(true);
            loopAnimator.Play(loopAnimationState, 0, 0f);

            if (startAnimator != null) startAnimator.gameObject.SetActive(false);
        }

        // 5. Trigger Confetti overlay after a fixed delay
        if (confettiAnimator != null)
        {
            yield return new WaitForSeconds(confettiDelay);
            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiAnimationState, 0, 0f);
        }

        // SpawnButton and congrats dialogue are now handled by DelayedCongrats()
    }

    // ... (Keep existing WaitForAnimationFinish, GetStageFromSnapValues, UpdateCharacterSprite, SpawnButton, and OnPlayPressed)
    
    IEnumerator WaitForAnimationFinish(Animator animator, string stateName)
    {
        yield return new WaitForEndOfFrame();
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.98f) yield return null;
    }

    int GetStageFromSnapValues(float value)
    {
        int closest = 0;
        float smallest = Mathf.Abs(value - snapValues[0]);
        for (int i = 1; i < snapValues.Length; i++)
        {
            float dist = Mathf.Abs(value - snapValues[i]);
            if (dist < smallest) { smallest = dist; closest = i; }
        }
        return closest;
    }

    void UpdateCharacterSprite(int stage)
    {
        if (characterRenderer != null && stage < stageSprites.Length && characterRenderer.gameObject.activeSelf)
        {
            characterRenderer.sprite = stageSprites[stage];
        }
    }

    IEnumerator DelayedCongrats()
    {
        yield return new WaitForSeconds(congratsDelay);
        Debug.Log($"[Dialogue] DelayedCongrats fired — dialogueController assigned: {dialogueController != null}");
        AudioManager.PlayComplete();
        if (dialogueController != null) dialogueController.OnLevelCompleted();
        SpawnButton();
    }

    void SpawnButton()
    {
        if (congratsPrefab == null || buttonParent == null) return;

        MiniGameProgress.SetCompleted(grabbableType);

        GameObject spawned = Instantiate(congratsPrefab, buttonParent);
        if (spawned.TryGetComponent(out RectTransform rt))
        {
            rt.localPosition = congratsPosition;
            rt.localRotation = Quaternion.identity;
            rt.localScale = congratsScale;
        }

        if (spawned.TryGetComponent(out Button btn) && spawned.TryGetComponent(out SceneLoader loader))
            btn.onClick.AddListener(loader.OnPlayPressed);
    }
}