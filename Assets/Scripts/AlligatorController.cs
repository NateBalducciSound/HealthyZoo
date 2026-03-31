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

        switch (zone)
        {
            case DropZoneType.Hand:
                animator.SetTrigger("HandSlap");
                item.ReturnToStart(0.5f);
                break;

            case DropZoneType.Eyes:
                animator.SetTrigger("EyeReject");
                item.ReturnToStart(0.3f);
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
        item.ReturnToStart(0.2f);

        foreach (var zone in dropZonesToDisableAfterNose)
            if (zone != null) zone.SetActive(false);
    }

    private void HandleMouth(Draggable2D item)
    {
        if (!mouthIsOpen || mouthTaskComplete) return;
        mouthTaskComplete = true;
        animator.SetTrigger("CongratsStart");
        item.ReturnToStart(0.2f);
        Invoke(nameof(StartCelebration), 1.0f); 
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