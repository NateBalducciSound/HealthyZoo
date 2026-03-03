using UnityEngine;

public class AlligatorController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Gameplay Objects")]
    public GameObject mouthDropZone;
    public GameObject confetti;
    public GameObject backButton;

    private bool noseActivated = false;
    private bool firstLoopCompleted = false;

    public void HandleDrop(DropZoneType zone, DraggableItem item)
    {
        switch (zone)
        {
            case DropZoneType.Hand:
                HandleHand(item);
                break;

            case DropZoneType.Eyes:
                HandleEyes(item);
                break;

            case DropZoneType.Nose:
                HandleNose(item);
                break;

            case DropZoneType.Mouth:
                HandleMouth(item);
                break;
        }
    }

    private void HandleHand(DraggableItem item)
    {
        animator.SetTrigger("HandSlap");
        item.ReturnToStart(0.5f);
    }

    private void HandleEyes(DraggableItem item)
    {
        animator.SetTrigger("EyeReject");
        item.ReturnToStart(0.3f);
    }

    private void HandleNose(DraggableItem item)
    {
        if (noseActivated) return;

        noseActivated = true;
        animator.SetTrigger("OpenMouth");
        mouthDropZone.SetActive(true);
        item.ReturnToStart(0.2f);
    }

    private void HandleMouth(DraggableItem item)
    {
        animator.SetTrigger("CongratsStart");
        confetti.SetActive(true);

        if (!firstLoopCompleted)
        {
            Invoke(nameof(SpawnBackButton), 2f);
            firstLoopCompleted = true;
        }

        item.ReturnToStart(0.2f);
    }

    private void SpawnBackButton()
    {
        backButton.SetActive(true);
    }
}