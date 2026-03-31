using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayButtonSpawner : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("The Image component on the UI that shows the animal/icon")]
    public Image mainImageComponent; 
    [Tooltip("The Image component on the UI that shows the overlay")]
    public Image overlayImageComponent;

    [Header("Sprite Assets (B&W / Default)")]
    public Sprite mainSpriteBW;           
    public Sprite overlaySpriteBW;        

    [Header("Sprite Assets (Colored / Completed)")]
    public Sprite mainCompletedSprite;
    public Sprite overlayCompletedSprite;

    [Header("Play Button Settings")]
    public GameObject playButtonPrefab;
    public Transform buttonParent;

    [Header("Mini Game Identity")]
    public GrabbableType myGrabbable;

    private GameObject currentButton;

    void Start()
    {
        RefreshVisualState();
    }

    void OnEnable()
    {
        RefreshVisualState(); 
    }

    /// <summary>
    /// Swaps the sprites based on whether the game is finished.
    /// </summary>
    void RefreshVisualState()
    {
        if (mainImageComponent == null || overlayImageComponent == null)
        {
            Debug.LogWarning($"Missing Image components on {gameObject.name}. Please assign them in the Inspector.");
            return;
        }

        bool completed = MiniGameProgress.IsCompleted(myGrabbable);

        if (completed)
        {
            mainImageComponent.sprite = mainCompletedSprite;
            overlayImageComponent.sprite = overlayCompletedSprite;
        }
        else
        {
            mainImageComponent.sprite = mainSpriteBW;
            overlayImageComponent.sprite = overlaySpriteBW;
        }
    }

    public void setSelectedGrabbable()
    {
        // Spawns the play button if it doesn't exist yet
        if (currentButton == null && playButtonPrefab != null)
        {
            currentButton = Instantiate(playButtonPrefab, buttonParent);
            Button btn = currentButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(OnPlayPressed);
            }
        }
    }

    public void OnPlayPressed()
    {
        string sceneName = "";

        switch (myGrabbable)
        {
            case GrabbableType.Giraffe:   sceneName = "_03AGiraffeScene"; break;
            case GrabbableType.Heron:     sceneName = "_03BHeronScene"; break;
            case GrabbableType.Porcupine: sceneName = "_03CPorcupineScene"; break;
            case GrabbableType.Sloth:     sceneName = "_03DSlothScene"; break;
            case GrabbableType.Panda:     sceneName = "_03EPandaScene"; break;
            case GrabbableType.Alligator: sceneName = "_03FAlligatorScene"; break;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log("This is the scene loaded from MiniGameSelect:" + sceneName);
            AsyncSceneLoader.Load(sceneName);
        }
    }
}