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

    [Header("Character")]
    public SpriteRenderer characterRenderer;
    public Sprite[] stageSprites;

    [Header("Animation")]
    public Animator characterAnimator;
    public string startAnimationState = "Complete_Start";
    public string loopAnimationState = "Complete_Loop";

    [Header("Play Button")]
    public Transform buttonParent;
    public GameObject playButtonPrefab;

    private GameObject currentButton;
    private int currentStage = -1;
    private bool completionTriggered = false;

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
        // Play startup animation (once)
        characterAnimator.Play(startAnimationState);

        // Wait for startup animation to finish
        yield return WaitForAnimation(characterAnimator, startAnimationState);

        // Animator transitions automatically to loop state
        SpawnButtonIfNeeded();
    }

    IEnumerator WaitForAnimation(Animator animator, string stateName)
    {
        // Wait until we enter the state
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        // Wait until it finishes
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
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
        if (currentButton != null) return;

        currentButton = Instantiate(playButtonPrefab, buttonParent);
        currentButton.GetComponent<Button>().onClick.AddListener(OnPlayPressed);
    }

    public void OnPlayPressed()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Giraffe);
        SceneManager.LoadScene("Scenes/_02MiniGameSelect");
    }
}