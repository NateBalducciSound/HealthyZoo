using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main controller for the Sloth blood-pressure mini-game.
///
/// Scene setup summary
/// -------------------
/// • Three "Sloth Position" GameObjects, each with a SpriteRenderer showing
///   the sloth at a distinct screen location.  Only one is active at a time.
/// • Three invisible SlothPositionZone colliders (can overlap the sloth sprites
///   or be separate tap-targets).
/// • A SlothDropZone child on every sloth position GameObject so the cuff can
///   be dropped on whichever one is currently visible.
/// • One Draggable2D object (the blood-pressure cuff).
/// • An Animator for the reward animation (optional).
/// • A congrats button that starts deactivated and gets re-activated on win.
/// </summary>
public class SlothController : MonoBehaviour
{
    [Header("Sloth Position GameObjects")]
    [Tooltip("Three GameObjects, each showing the sloth at a different location. " +
             "Index matches SlothPositionZone.positionIndex.")]
    public GameObject[] slothPositions; // length 3

    [Header("Position Settings")]
    [Tooltip("Which sloth is visible when the scene starts.")]
    public int startingPositionIndex = 1;

    [Tooltip("The positionIndex the sloth must be in for the cuff drop to succeed.")]
    public int correctPositionIndex = 1;

    [Header("Reward Animation")]
    [Tooltip("Animator that plays the reward sequence. Leave null if unused.")]
    public Animator rewardAnimator;

    [Header("Confetti")]
    public Animator confettiAnimator;
    public string confettiStateName = "Confetti";

    [Header("Congrats Button")]
    [Tooltip("Button that is hidden at start and shown after the reward plays.")]
    public GameObject congratsButton;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    [Header("Navigation")]
    public string menuSceneName = "_02MiniGameSelect";

    // ── State ─────────────────────────────────────────────────────────────────
    private int activeIndex = -1;
    private bool isFinished = false;
    private bool playerStarted = false;

    public bool IsInputLocked => dialogueController != null && dialogueController.IsInputLocked;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName(menuSceneName));

        // Hide all sloths, then activate the starting position
        foreach (var go in slothPositions)
            if (go != null) go.SetActive(false);

        if (startingPositionIndex >= 0 && startingPositionIndex < slothPositions.Length)
        {
            slothPositions[startingPositionIndex].SetActive(true);
            activeIndex = startingPositionIndex;
        }

        if (congratsButton != null)
            congratsButton.SetActive(false);
    }

    // ── Called by SlothPositionZone ───────────────────────────────────────────

    public void OnPositionTapped(int index)
    {
        if (isFinished) return;
        if (IsInputLocked) return;
        if (index == activeIndex) return;
        if (index < 0 || index >= slothPositions.Length) return;

        NotifyPlayerStarted();
        dialogueController?.ResetNudgeTimer();

        // Deactivate previous, activate new
        if (activeIndex >= 0 && activeIndex < slothPositions.Length)
            if (slothPositions[activeIndex] != null)
                slothPositions[activeIndex].SetActive(false);

        slothPositions[index].SetActive(true);
        activeIndex = index;
    }

    // ── Called by SlothDropZone ───────────────────────────────────────────────

    public void HandleCuffDrop(int dropPositionIndex, Draggable2D cuff)
    {
        if (isFinished) return;

        // Cuff must land on the currently visible sloth at the correct position
        bool correctPosition = dropPositionIndex == correctPositionIndex;
        bool slothIsHere     = activeIndex == dropPositionIndex;

        if (correctPosition && slothIsHere)
        {
            isFinished = true;
            cuff.MarkHandled(); // prevent Draggable2D.ProcessRelease from also acting
            StartCoroutine(SuccessSequence(cuff));
        }
        else
        {
            cuff.ReturnToStart();
            dialogueController?.ResetNudgeTimer();
        }
    }

    // ── Success ───────────────────────────────────────────────────────────────

    IEnumerator SuccessSequence(Draggable2D cuff)
    {
        // Hide the cuff
        cuff.gameObject.SetActive(false);

        // Save progress
        MiniGameProgress.SetCompleted(GrabbableType.Sloth);

        // Fire the reward trigger
        if (rewardAnimator != null)
            rewardAnimator.SetTrigger("Reward");

        yield return new WaitForSeconds(0.5f);

        // Confetti
        if (confettiAnimator != null)
            confettiAnimator.Play(confettiStateName);

        // Audio / dialogue
        AudioManager.PlayComplete();
        if (dialogueController != null)
            dialogueController.OnLevelCompleted();

        // Show congrats button
        if (congratsButton != null)
            congratsButton.SetActive(true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void NotifyPlayerStarted()
    {
        if (playerStarted) return;
        playerStarted = true;
        if (dialogueController != null)
            dialogueController.OnPlayerStarted();
    }

    public void OnBackToMenuPressed()
    {
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName(menuSceneName));
    }
}
