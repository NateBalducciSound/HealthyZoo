using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButtonSpawner : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playButtonPrefab;
    public Transform buttonParent;
    public Image targetImage;

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
        RefreshVisualState(); // ensures update after returning from mini-game
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
    /// Updates image color based on completion state
    /// </summary>
    void RefreshVisualState()
    {
        if (targetImage == null) return;

        bool completed = MiniGameProgress.IsCompleted(myGrabbable);
        targetImage.color = completed ? Color.green : Color.gray;
    }

    /// <summary>
    /// Loads the selected mini-game
    /// </summary>
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
