using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButtonSpawner : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playButtonPrefab;
    public Transform buttonParent;
    public GameObject checkmarkObject; // ✅ green checkmark object

    [Header("Mini Game Identity")]
    public GrabbableType myGrabbable;

    private GrabbableType selectedGrabbable;
    private GameObject currentButton;

    void Start()
    {
        RefreshVisualState();
    }

    void OnEnable()
    {
        RefreshVisualState(); // update when returning from mini-game
    }

    /// <summary>
    /// Called when the image is clicked
    /// </summary>
    public void setSelectedGrabbable()
    {
        selectedGrabbable = myGrabbable;

        // Always spawn the play button
        if (currentButton == null)
        {
            currentButton = Instantiate(playButtonPrefab, buttonParent);
            Button btn = currentButton.GetComponent<Button>();
            btn.onClick.AddListener(OnPlayPressed);
        }
    }

    /// <summary>
    /// Shows or hides the checkmark based on completion
    /// </summary>
    void RefreshVisualState()
    {
        if (checkmarkObject == null)
        {
            Debug.LogWarning("Checkmark not assigned on " + gameObject.name);
            return;
        }

        bool completed = MiniGameProgress.IsCompleted(myGrabbable);
        checkmarkObject.SetActive(completed);
    }
    //Button press spawn button
    public void OnPlayPressed()
    {
        switch (selectedGrabbable)
        {
            case GrabbableType.Giraffe:
                SceneManager.LoadScene("Scenes/_03AGiraffeScene");
                break;

            case GrabbableType.Heron:
                SceneManager.LoadScene("Scenes/_03BHeronScene");
                break;

            case GrabbableType.Porcupine:
                SceneManager.LoadScene("Scenes/_03CPorcupineScene");
                break;

            case GrabbableType.Sloth:
                SceneManager.LoadScene("Scenes/_03DSlothScene");
                break;

            case GrabbableType.Panda:
                SceneManager.LoadScene("Scenes/_03EPandaScene");
                break;

            case GrabbableType.Alligator:
                SceneManager.LoadScene("Scenes/_03FAlligatorScene");
                break;
        }
    }
}
