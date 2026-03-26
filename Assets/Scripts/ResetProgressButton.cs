using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to a GameObject in _02MiniGameSelect.
// Assign the reset Button reference in the inspector — it will only be visible
// when every mini game has been completed.
public class ResetProgressButton : MonoBehaviour
{
    [Tooltip("The reset button to show/hide.")]
    public Button resetButton;

    void Start()
    {
        if (resetButton == null)
        {
            Debug.LogWarning("[ResetProgressButton] No button assigned.");
            return;
        }

        resetButton.onClick.AddListener(OnResetPressed);
        RefreshVisibility();
    }

    void OnEnable()
    {
        RefreshVisibility();
    }

    void RefreshVisibility()
    {
        if (resetButton == null) return;
        resetButton.gameObject.SetActive(AllGamesComplete());
    }

    void OnResetPressed()
    {
        MiniGameProgress.ResetAll();
        // Reload the scene so all character images switch back to B&W
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static bool AllGamesComplete()
    {
        foreach (GrabbableType type in System.Enum.GetValues(typeof(GrabbableType)))
        {
            if (!MiniGameProgress.IsCompleted(type))
                return false;
        }
        return true;
    }
}
