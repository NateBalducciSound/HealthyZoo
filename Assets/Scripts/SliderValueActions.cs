using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SliderValueActions : MonoBehaviour
{
    [Header("Slider")]
    public Slider slider;

    [Header("Snap Values")]
    public float[] snapValues;

    [Header("Character Sprites")]
    public SpriteRenderer characterRenderer;
    public Sprite[] stageSprites;

    [Header("Start / Congrats Animator")]
    public Animator startAnimator;
    [Tooltip("Plays once when completion is reached")]
    public string startAnimationState;

    [Header("Loop Animator")]
    public Animator loopAnimator;
    [Tooltip("Loops forever after the start animation")]
    public string loopAnimationState;

    [Header("Confetti Animator")]
    public Animator confettiAnimator;
    [Tooltip("Plays once over the loop")]
    public string confettiAnimationState;

    [Range(0f, 1f)]
    [Tooltip("When confetti triggers during the FIRST loop (0.5 = halfway)")]
    public float confettiTriggerPoint = 0.5f;

    [Header("Play Button")]
    public Transform buttonParent;
    public GameObject playButtonPrefab;

    private GameObject currentButton;
    private int currentStage = -1;
    private bool completionTriggered = false;
    private bool confettiPlayed = false;

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);
        OnSliderChanged(slider.value);
    }

    void OnSliderChanged(float value)
    {
        int stage = GetStageFromSnapValues(value);

        if (stage == currentStage)
            return;

        currentStage = stage;
        UpdateCharacterSprite(stage);

        if (stage == stageSprites.Length - 1 && !completionTriggered)
        {
            completionTriggered = true;
            StartCoroutine(PlayCompletionSequence());
        }
    }

    IEnumerator PlayCompletionSequence()
    {
        // 1️⃣ Play start / congrats animation
        if (startAnimator != null && !string.IsNullOrEmpty(startAnimationState))
        {
            startAnimator.gameObject.SetActive(true);
            startAnimator.Play(startAnimationState);
            yield return WaitForAnimationToFinish(startAnimator, startAnimationState);
        }

        // 2️⃣ Start looping animation
        if (loopAnimator != null && !string.IsNullOrEmpty(loopAnimationState))
        {
            loopAnimator.gameObject.SetActive(true);
            loopAnimator.Play(loopAnimationState);
        }

        // 3️⃣ Wait until halfway through first loop
        if (loopAnimator != null)
        {
            yield return WaitForLoopProgress(loopAnimator, loopAnimationState, confettiTriggerPoint);
        }

        // 4️⃣ Play confetti ONCE
        if (!confettiPlayed && confettiAnimator != null && !string.IsNullOrEmpty(confettiAnimationState))
        {
            confettiPlayed = true;
            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiAnimationState);
        }

        SpawnButtonIfNeeded();
    }

    IEnumerator WaitForAnimationToFinish(Animator animator, string stateName)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
    }

    IEnumerator WaitForLoopProgress(Animator animator, string stateName, float progress)
    {
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < progress)
            yield return null;
    }

    int GetStageFromSnapValues(float value)
    {
        int closest = 0;
        float smallest = Mathf.Abs(value - snapValues[0]);

        for (int i = 1; i < snapValues.Length; i++)
        {
            float dist = Mathf.Abs(value - snapValues[i]);
            if (dist < smallest)
            {
                smallest = dist;
                closest = i;
            }
        }

        return closest;
    }

    void UpdateCharacterSprite(int stage)
    {
        if (stageSprites == null || stageSprites.Length == 0)
            return;

        characterRenderer.sprite = stageSprites[stage];
    }

    void SpawnButtonIfNeeded()
    {
        if (currentButton != null)
            return;

        currentButton = Instantiate(playButtonPrefab, buttonParent);
        currentButton.GetComponent<Button>().onClick.AddListener(OnPlayPressed);
    }

    public void OnPlayPressed()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Giraffe);
        SceneManager.LoadScene("Scenes/_02MiniGameSelect");
    }
}