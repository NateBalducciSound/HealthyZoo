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

    [Header("Sprite Assets (B&W / Default) — Phone")]
    public Sprite mainSpriteBW;
    public Sprite overlaySpriteBW;

    [Header("Sprite Assets (Colored / Completed) — Phone")]
    public Sprite mainCompletedSprite;
    public Sprite overlayCompletedSprite;

    [Header("Sprite Assets (B&W / Default) — iPad (falls back to Phone if empty)")]
    public Sprite mainSpriteBW_iPad;
    public Sprite overlaySpriteBW_iPad;

    [Header("Sprite Assets (Colored / Completed) — iPad (falls back to Phone if empty)")]
    public Sprite mainCompletedSprite_iPad;
    public Sprite overlayCompletedSprite_iPad;

    [Header("Play Button Settings")]
    public GameObject playButtonPrefab;
    public Transform buttonParent;

    [Header("Completed Animation")]
    [Tooltip("Animator on the character Image GameObject. Enabled only when minigame is completed.")]
    public Animator idleAnimator;
    [Tooltip("Trigger parameter name in the Animator Controller that starts the idle loop.")]
    public string idleTrigger = "PlayIdle";

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
        bool isTablet = DeviceDetector.IsTablet;

        Sprite bw      = (isTablet && mainSpriteBW_iPad      != null) ? mainSpriteBW_iPad      : mainSpriteBW;
        Sprite bwOver  = (isTablet && overlaySpriteBW_iPad   != null) ? overlaySpriteBW_iPad   : overlaySpriteBW;
        Sprite cl      = (isTablet && mainCompletedSprite_iPad    != null) ? mainCompletedSprite_iPad    : mainCompletedSprite;
        Sprite clOver  = (isTablet && overlayCompletedSprite_iPad != null) ? overlayCompletedSprite_iPad : overlayCompletedSprite;

        if (completed)
        {
            overlayImageComponent.sprite = clOver;
            if (idleAnimator != null)
            {
                idleAnimator.enabled = true;
                if (!string.IsNullOrEmpty(idleTrigger))
                    idleAnimator.SetTrigger(idleTrigger);
            }
            else
            {
                mainImageComponent.sprite = cl;
            }
        }
        else
        {
            if (idleAnimator != null) idleAnimator.enabled = false;
            mainImageComponent.sprite = bw;
            overlayImageComponent.sprite = bwOver;
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

    private string GetSceneNameForGrabbable()
    {
        switch (myGrabbable)
        {
            case GrabbableType.Giraffe:   return "_03AGiraffeScene";
            case GrabbableType.Heron:     return "_03BHeronScene";
            case GrabbableType.Porcupine: return "_03CPorcupineScene";
            case GrabbableType.Sloth:     return "_03DSlothScene";
            case GrabbableType.Panda:     return "_03EPandaScene";
            case GrabbableType.Alligator: return "_03FAlligatorScene";
            default:                      return "";
        }
    }

    public void OnPlayPressed()
    {
        string sceneName = GetSceneNameForGrabbable();
        if (!string.IsNullOrEmpty(sceneName))
        {
            sceneName = DeviceDetector.GetSceneName(sceneName);
            Debug.Log("This is the scene loaded from MiniGameSelect:" + sceneName);
            AsyncSceneLoader.Load(sceneName);
        }
    }
}