using UnityEngine;

// Attach to the overlay GameObject that has the Animator.
// Fires the idle trigger if this character's minigame is completed.
[RequireComponent(typeof(Animator))]
public class CharacterSelectAnimator : MonoBehaviour
{
    public GrabbableType minigame;
    public string idleTrigger = "PlayIdle";

    void Start()
    {
        if (MiniGameProgress.IsCompleted(minigame))
            GetComponent<Animator>().SetTrigger(idleTrigger);
    }
}
