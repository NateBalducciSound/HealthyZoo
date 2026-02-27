using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SliderValueActions : MonoBehaviour
{
    [Header("Slider")]
    public Slider slider;

    [Header("Snap Values (0–1, bottom to top)")]
    public float[] snapValues; // MUST match stageSprites length

    [Header("Character")]
    public SpriteRenderer characterRenderer;
    public Sprite[] stageSprites;

    [Header("Play Button")]
    public Transform buttonParent;
    public GameObject playButtonPrefab;

    private GameObject currentButton;
    private int currentStage = -1;

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

        if (stage == stageSprites.Length - 1)
            SpawnButtonIfNeeded();
        else
            RemoveButton();
    }

    int GetStageFromSnapValues(float value)
    {
        int closestIndex = 0;
        float closestDistance = Mathf.Abs(value - snapValues[0]);

        for (int i = 1; i < snapValues.Length; i++)
        {
            float distance = Mathf.Abs(value - snapValues[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
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

    void RemoveButton()
    {
        if (currentButton == null) return;

        Destroy(currentButton);
        currentButton = null;
    }

    public void OnPlayPressed()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Giraffe);
        Debug.Log("COMPLETED: " + GrabbableType.Giraffe);
        SceneManager.LoadScene("Scenes/_02MiniGameSelect");
    }
}