using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SliderValueActions : MonoBehaviour
{
    [Header("Slider Settings")]
    public Slider slider;
    public float[] snapValues;

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
    [Tooltip("The exact position the loop animator should move to")]
    public Vector3 loopPositionOffset;

    [Header("3. Overlay Animation")]
    public Animator confettiAnimator;
    public string confettiAnimationState;
    [Range(0f, 1f)]
    public float confettiTriggerPoint = 0.5f;

    [Header("UI Transition")]
    public Transform buttonParent;
    public GameObject playButtonPrefab;

    private int currentStage = -1;
    private bool completionTriggered = false;

    void Start()
    {
        if (startAnimator) startAnimator.gameObject.SetActive(false);
        if (loopAnimator) loopAnimator.gameObject.SetActive(false);
        if (confettiAnimator) confettiAnimator.gameObject.SetActive(false);

        slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateCharacterSprite(GetStageFromSnapValues(slider.value));
    }

    void OnSliderChanged(float value)
    {
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
        // 1. Hide the static height-check sprite
        if (characterRenderer != null)
        {
            characterRenderer.gameObject.SetActive(false);
        }

        // 2. Play Startup Animation
        if (startAnimator != null)
        {
            startAnimator.gameObject.SetActive(true);
            startAnimator.Play(startAnimationState, 0, 0f);
            yield return StartCoroutine(WaitForAnimationFinish(startAnimator, startAnimationState));
        }

        // 3. SWITCH: Move Loop Animator, Start it, and HIDE Startup Animator
        if (loopAnimator != null)
        {
            // Set position before enabling
            loopAnimator.transform.localPosition = loopPositionOffset;
            
            loopAnimator.gameObject.SetActive(true);
            loopAnimator.Play(loopAnimationState, 0, 0f);

            // Hide the startup animation now that loop has taken over
            if (startAnimator != null)
            {
                startAnimator.gameObject.SetActive(false);
            }
        }

        // 4. Trigger Confetti overlay during the loop
        if (loopAnimator != null && confettiAnimator != null)
        {
            yield return new WaitUntil(() => 
                loopAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= confettiTriggerPoint);

            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiAnimationState, 0, 0f);
        }

        SpawnButton();
    }

    IEnumerator WaitForAnimationFinish(Animator animator, string stateName)
    {
        yield return new WaitForEndOfFrame();
        
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.98f)
            yield return null;
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

    void SpawnButton()
    {
        if (playButtonPrefab != null)
        {
            GameObject btn = Instantiate(playButtonPrefab, buttonParent);
            btn.GetComponent<Button>().onClick.AddListener(OnPlayPressed);
        }
    }

    public void OnPlayPressed()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Giraffe);
        SceneManager.LoadScene("Scenes/_02MiniGameSelect");
    }
}