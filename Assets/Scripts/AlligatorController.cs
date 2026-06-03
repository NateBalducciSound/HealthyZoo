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

    [Header("Zone-Specific Incorrect Sounds")]
    public AudioClip eyesIncorrectClip;
    public float eyesIncorrectDelay = 0.6f;
    public AudioClip feetIncorrectClip;
    public float feetIncorrectDelay = 0.6f;
    [Range(0f, 1f)] public float incorrectVolume = 1f;

    [Header("Nose Voice Line")]
    public LocalizedClip noseVoiceLine;
    public LocalizedCaption noseCaption;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    private bool mouthIsOpen = false;
    private bool mouthTaskComplete = false;

    void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName("_02MiniGameSelect"));
    }

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
                StartCoroutine(PlayClipDelayed(feetIncorrectClip, feetIncorrectDelay));
                StartCoroutine(ReturnAfterAnimation(item));
                break;

            case DropZoneType.Eyes:
                animator.SetTrigger("EyeReject");
                StartCoroutine(PlayClipDelayed(eyesIncorrectClip, eyesIncorrectDelay));
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
        AudioManager.PlayCorrect();
        animator.SetTrigger("OpenMouth");
        mouthDropZone.SetActive(true);
        item.ShowAtStart();

        foreach (var zone in dropZonesToDisableAfterNose)
            if (zone != null) zone.SetActive(false);

        if (dialogueController != null)
        {
            dialogueController.StopNudge();
            dialogueController.PlayVoiceLine(noseVoiceLine, noseCaption);
        }
    }

    private void HandleMouth(Draggable2D item)
    {
        if (!mouthIsOpen || mouthTaskComplete) return;
        mouthTaskComplete = true;
        animator.SetTrigger("CongratsStart");
        item.Hide();
        StartCoroutine(CelebrationSequence());
    }

    // Waits one frame for the Animator to enter the new state, then waits for
    // most of the clip before snapping the item back — slightly early so there
    // is no visible gap between the animation ending and the item reappearing.
    [Tooltip("Seconds before the end of the animation clip to respawn the tongue depressor")]
    public float respawnEarlyBy = 0.15f;

    private IEnumerator PlayClipDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.PlayClip(clip, incorrectVolume);
    }

    private IEnumerator ReturnAfterAnimation(Draggable2D item)
    {
        yield return null; // let the Animator transition into the new state
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float waitTime = Mathf.Max(0f, state.length - respawnEarlyBy);
        yield return new WaitForSeconds(waitTime);
        if (mouthTaskComplete) yield break;
        item.ShowAtStart();
    }

    IEnumerator CelebrationSequence()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Alligator);

        if (confettiAnimator != null)
        {
            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiStateName);
        }

        AudioManager.PlayComplete();

        yield return new WaitForSeconds(1f);
        if (dialogueController != null) dialogueController.OnLevelCompleted();
        yield return new WaitForSeconds(dialogueController != null ? dialogueController.CompletionLineDuration : 0f);

        backButton.SetActive(true);
    }
}