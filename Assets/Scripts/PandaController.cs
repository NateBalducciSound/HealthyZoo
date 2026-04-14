using System.Collections;
using UnityEngine;

public class PandaController : MonoBehaviour
{
    [Header("Scope Draggable")]
    public Draggable2D scopeDraggable;

    [Header("Panda+Scope Combined Sprites")]
    [Tooltip("Index 0=Feet, 1=Belly, 2=Chest, 3=Eyes, 4=TopOfHead.")]
    public GameObject[] combinedSprites;

    [Header("Sprite Rendering")]
    [Tooltip("Sorting layer all combined sprites will be forced onto at runtime.")]
    public string sortingLayerName = "Default";
    [Tooltip("Sorting order for all combined sprites.")]
    public int sortingOrder = 0;

    [Header("Scan Button")]
    public GameObject scanButton;

    [Header("Panda Animator")]
    public Animator pandaAnimator;
    public string orangeGlowStateName     = "OrangeButton";
    public string greenGlowStateName      = "GreenButton";
    public string loopCelebrationStateName = "PingLoopAnimation";

    [Header("Confetti")]
    public Animator confettiAnimator;
    public string confettiStateName = "Confetti";

    [Header("Congrats Button")]
    public GameObject congratsButton;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    [Header("Navigation")]
    public string menuSceneName = "_02MiniGameSelect";

    private const int EyeZoneIndex = 3;

    private int  currentZone   = -1;
    private bool isFinished    = false;
    private bool playerStarted = false;

    void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName(menuSceneName));

        // Normalise all combined sprites to the same sorting layer/order
        // and ensure only sprite 0 is active at the start
        for (int i = 0; i < combinedSprites.Length; i++)
        {
            if (combinedSprites[i] == null) continue;

            foreach (var sr in combinedSprites[i].GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder     = sortingOrder;
            }

            combinedSprites[i].SetActive(i == 0);
        }

        if (scanButton     != null) scanButton.SetActive(false);
        if (congratsButton != null) congratsButton.SetActive(false);

        scopeDraggable?.Show();
    }

    // ── Called by PandaPositionZone ───────────────────────────────────────────

    public void OnScopeEnterZone(int zoneIndex)
    {
        if (isFinished) return;
        NotifyPlayerStarted();

        currentZone = zoneIndex;
        scopeDraggable?.Hide();

        // Show only the sprite for this zone; keep sprite 0 as fallback when zoneIndex == 0
        for (int i = 0; i < combinedSprites.Length; i++)
            if (combinedSprites[i] != null)
                combinedSprites[i].SetActive(i == zoneIndex);

        if (zoneIndex == EyeZoneIndex)
        {
            pandaAnimator?.Play(orangeGlowStateName);
            if (scanButton != null) scanButton.SetActive(true);
        }
    }

    public void OnScopeExitZone(int zoneIndex)
    {
        if (isFinished) return;
        if (currentZone != zoneIndex) return;

        currentZone = -1;
        scopeDraggable?.Show();

        // Hide the active zone sprite and restore sprite 0
        if (zoneIndex < combinedSprites.Length && combinedSprites[zoneIndex] != null)
            combinedSprites[zoneIndex].SetActive(false);

        if (combinedSprites.Length > 0 && combinedSprites[0] != null)
            combinedSprites[0].SetActive(true);

        if (zoneIndex == EyeZoneIndex)
            if (scanButton != null) scanButton.SetActive(false);
    }

    public void OnScopeReleasedInZone(int zoneIndex, Draggable2D scope)
    {
        if (isFinished) return;

        scope.MarkHandled();

        // Eye zone: scope stays hidden, player must press the scan button
        if (zoneIndex == EyeZoneIndex) return;

        // Non-eye zones: scope stays hidden at this zone position so the player
        // can pick it up again from here rather than going all the way back to start.
        // currentZone and the combined sprite remain as-is — OnScopeExitZone will
        // clean up when the player grabs and drags out of this zone.
    }

    // ── Scan button (UI Button OnClick) ──────────────────────────────────────

    public void OnScanButtonPressed()
    {
        Debug.Log($"[PandaController] OnScanButtonPressed — isFinished:{isFinished} currentZone:{currentZone} EyeZoneIndex:{EyeZoneIndex}");
        if (isFinished) return;
        if (currentZone != EyeZoneIndex) return;

        isFinished = true;
        if (scanButton != null) scanButton.SetActive(false);
        scopeDraggable?.MarkHandled();

        MiniGameProgress.SetCompleted(GrabbableType.Panda);
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        if (dialogueController != null) dialogueController.OnLevelCompleted();

        // GreenButton — Animator's exit-time transitions chain the rest:
        // GreenButton → PandaSideways Animation → PingQRAnimations → PingLoopAnimation
        pandaAnimator?.Play(greenGlowStateName);

        // Wait until the looping celebration state is reached
        yield return new WaitUntil(() =>
            pandaAnimator != null &&
            pandaAnimator.GetCurrentAnimatorStateInfo(0).IsName(loopCelebrationStateName));

        // Wait one full loop cycle then spawn congrats
        yield return new WaitForSeconds(pandaAnimator.GetCurrentAnimatorStateInfo(0).length);

        AudioManager.PlayComplete();
        if (confettiAnimator != null) confettiAnimator.Play(confettiStateName);
        if (congratsButton   != null) congratsButton.SetActive(true);
    }

    void NotifyPlayerStarted()
    {
        if (playerStarted) return;
        playerStarted = true;
        if (dialogueController != null) dialogueController.OnPlayerStarted();
    }

    public void OnBackToMenuPressed()
    {
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName(menuSceneName));
    }
}
