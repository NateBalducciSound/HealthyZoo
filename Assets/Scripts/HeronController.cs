using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class HeronLevelController : MonoBehaviour
{
    [Header("Slider Settings")]
    public Slider positionSlider; // Min: 0, Max: 2, Whole Numbers: True
    public SpriteRenderer staticHeronRenderer;
    public Sprite[] stageSprites; 

    [Header("Animations")]
    public Animator startupAnimator;
    public Animator loopAnimator;

    [Header("UI & Navigation")]
    public GameObject playButtonPrefab;
    public Transform buttonParent;
    public string menuSceneName = "_02MiniGameSelect";

    private bool isFinished = false;

    void Start()
    {
        // Link the slider to the sprite swapper
        if (positionSlider != null)
        {
            positionSlider.onValueChanged.AddListener(UpdateVisuals);
            UpdateVisuals(positionSlider.value);
        }

        // Initialize animators to off
        if (startupAnimator) startupAnimator.gameObject.SetActive(false);
        if (loopAnimator) loopAnimator.gameObject.SetActive(false);
    }

    public void UpdateVisuals(float value)
    {
        if (isFinished) return;

        int index = Mathf.RoundToInt(value);
        if (index >= 0 && index < stageSprites.Length)
        {
            staticHeronRenderer.sprite = stageSprites[index];
        }
    }

    public void ProcessHeronDrop(Draggable2D item)
    {
        // Check if slider is exactly in the middle
        if (Mathf.RoundToInt(positionSlider.value) == 1 && !isFinished)
        {
            isFinished = true;
            StartCoroutine(SuccessSequence(item));
        }
        else
        {
            // Reset item to its internal spawn point if wrong position
            item.ReturnToStart();
            Debug.Log("Incorrect Position. Try middle (1).");
        }
    }

    IEnumerator SuccessSequence(Draggable2D item)
    {
        // 1. Logic & Persistence
        item.gameObject.SetActive(false);
        positionSlider.interactable = false;
        
        // Save progress (Adjust 'GrabbableType' to match your enum name)
        // MiniGameProgress.SetCompleted(GrabbableType.Heron, 1); 

        // 2. Animation Swap
        staticHeronRenderer.gameObject.SetActive(false);
        startupAnimator.gameObject.SetActive(true);
        startupAnimator.Play("Startup");

        // 3. Wait for startup clip to finish
        yield return new WaitForEndOfFrame();
        while (startupAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f) 
            yield return null;

        // 4. Enter Loop
        startupAnimator.gameObject.SetActive(false);
        loopAnimator.gameObject.SetActive(true);
        loopAnimator.Play("Loop");

        // 5. Spawn Back-to-Menu Button
        SpawnMenuButton();
    }

    void SpawnMenuButton()
    {
        if (playButtonPrefab != null && buttonParent != null)
        {
            GameObject btn = Instantiate(playButtonPrefab, buttonParent);
            btn.GetComponent<Button>().onClick.AddListener(OnBackToMenuPressed);
        }
    }

    public void OnBackToMenuPressed()
    {
        SceneManager.LoadScene("_02MiniGameSelect");
    }
}