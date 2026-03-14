using UnityEngine;

public class AlligatorController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Gameplay Objects")]
    public GameObject mouthDropZone;
    public GameObject confetti;
    public GameObject backButton;

    private bool mouthIsOpen = false;
    private bool mouthTaskComplete = false;

    public void HandleDrop(DropZoneType zone, Draggable2D item)
    {
        // Don't process anything if the main task is already done
        if (mouthTaskComplete) return;

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
        animator.SetTrigger("OpenMouth"); // Transition to 'MouthOpenIdle' state
        mouthDropZone.SetActive(true);
        item.ReturnToStart(0.2f);
    }

    private void HandleMouth(Draggable2D item)
    {
        if (!mouthIsOpen || mouthTaskComplete) return;

        mouthTaskComplete = true;
        animator.SetTrigger("CongratsStart");
        
        // Return item quickly so it doesn't block the view
        item.ReturnToStart(0.2f);

        // We delay the confetti slightly to match the 'startup' animation finishing
        // or you can call this via an Animation Event for perfect timing
        Invoke(nameof(StartCelebration), 1.0f); 
    }

    private void StartCelebration()
    {
        confetti.SetActive(true);
        backButton.SetActive(true);
    }
}