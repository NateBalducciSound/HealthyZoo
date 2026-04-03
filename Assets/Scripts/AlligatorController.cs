using System.Collections;
using UnityEngine;

// Moving this OUTSIDE the class makes it accessible to DropZone2D
public enum DropZoneType
{
    Eyes,
    Hand,
    Nose,
    Mouth
}

public class AlligatorController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Gameplay Objects")]
    public GameObject mouthDropZone;
    public GameObject backButton;

    [Header("Confetti")]
    public Animator confettiAnimator;
    public string confettiStateName = "Confetti";

    [Header("Drop Zones to Disable After Nose")]
    [Tooltip("Assign the Eyes, Feet, and Nose DropZone GameObjects here")]
    public GameObject[] dropZonesToDisableAfterNose;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    private bool mouthIsOpen = false;
    private bool mouthTaskComplete = false;

    public void HandleDrop(DropZoneType zone, Draggable2D item)
    {
        if (mouthTaskComplete) return;

        // Ensure the item knows it was dropped so it doesn't snap back instantly
        item.MarkHandled();

        item.Hide();

        switch (zone)
        {
            case DropZoneType.Hand:
                animator.SetTrigger("HandSlap");
                StartCoroutine(ReturnAfterAnimation(item));
                break;

            case DropZoneType.Eyes:
                animator.SetTrigger("EyeReject");
                StartCoroutine(ReturnAfterAnimation(item));
                break;

            case DropZoneType.Nose:
                HandleNose(item);
                break;

            case DropZoneType.Mouth:
                HandleMouth(item);
                break;
        }
    }

    private void HandleNose(Draggable2D item)
    {
        if (mouthIsOpen) return;
        mouthIsOpen = true;
        animator.SetTrigger("OpenMouth");
        mouthDropZone.SetActive(true);
        item.ShowAtStart();

        foreach (var zone in dropZonesToDisableAfterNose)
            if (zone != null) zone.SetActive(false);
    }

    private void HandleMouth(Draggable2D item)
    {
        if (!mouthIsOpen || mouthTaskComplete) return;
        mouthTaskComplete = true;
        animator.SetTrigger("CongratsStart");
        item.ShowAtStart();
        Invoke(nameof(StartCelebration), 1.0f);
    }

    // Waits one frame for the Animator to enter the new state, then waits for
    // most of the clip before snapping the item back — slightly early so there
    // is no visible gap between the animation ending and the item reappearing.
    [Tooltip("Seconds before the end of the animation clip to respawn the tongue depressor")]
    public float respawnEarlyBy = 0.15f;

    private IEnumerator ReturnAfterAnimation(Draggable2D item)
    {
        yield return null; // let the Animator transition into the new state
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float waitTime = Mathf.Max(0f, state.length - respawnEarlyBy);
        yield return new WaitForSeconds(waitTime);
        item.ShowAtStart();
    }

    private void StartCelebration()
    {
        AudioManager.PlayComplete();
        if (dialogueController != null) dialogueController.OnLevelCompleted();
        if (confettiAnimator != null)
        {
            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiStateName);
        }
        backButton.SetActive(true);
    }
}