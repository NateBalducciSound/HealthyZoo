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
    public string orangeGlowStateName      = "OrangeButton";
    public string greenGlowStateName       = "GreenButton";
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

    [Header("Zone 4 (Top of Head) — Y Threshold Override")]
    [Tooltip("If the scope's world Y position reaches or exceeds this value while dragging, zone 4 activates regardless of trigger colliders. Set this to just above the top of zone 3's collider.")]
    public float zone4YThreshold = 2f;

    private const int EyeZoneIndex = 3;

    private int  currentZone   = -1;
    private bool isFinished    = false;
    private bool playerStarted = false;
    private bool _zone4Active  = false;

    void Start()
    {
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName(menuSceneName));

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

        if (scopeDraggable != null && dialogueController != null)
            scopeDraggable.dialogueController = dialogueController;

        scopeDraggable?.Show();
    }

    void Update()
    {
        if (isFinished || scopeDraggable == null) return;

        bool aboveThreshold = scopeDraggable.transform.position.y >= zone4YThreshold;

        if (aboveThreshold && !_zone4Active)
        {
            _zone4Active = true;
            currentZone  = 4;
            NotifyPlayerStarted();
            scopeDraggable.Hide();
            scopeDraggable.MarkHandled();
            for (int i = 0; i < combinedSprites.Length; i++)
                if (combinedSprites[i] != null)
                    combinedSprites[i].SetActive(i == 4);
        }
        else if (!aboveThreshold && _zone4Active)
        {
            _zone4Active = false;
            currentZone  = -1;
            scopeDraggable.Show();
            if (combinedSprites.Length > 0 && combinedSprites[0] != null)
                combinedSprites[0].SetActive(true);
            for (int i = 1; i < combinedSprites.Length; i++)
                if (combinedSprites[i] != null)
                    combinedSprites[i].SetActive(false);
        }
    }

    // ── Called by PandaPositionZone ───────────────────────────────────────────

    public void OnScopeEnterZone(int zoneIndex)
    {
        if (isFinished) return;
        if (zoneIndex == 4) return;
        if (_zone4Active) return;
        NotifyPlayerStarted();

        currentZone = zoneIndex;

        if (zoneIndex == 0)
            scopeDraggable?.Show();
        else
            scopeDraggable?.Hide();

        int spriteToShow = (zoneIndex < combinedSprites.Length) ? zoneIndex : 0;
        if (zoneIndex >= combinedSprites.Length)
            Debug.LogWarning($"[PandaController] zoneIndex {zoneIndex} is out of range — combinedSprites has {combinedSprites.Length} elements. Showing sprite 0 as fallback.");

        for (int i = 0; i < combinedSprites.Length; i++)
            if (combinedSprites[i] != null)
                combinedSprites[i].SetActive(i == spriteToShow);

        if (zoneIndex == EyeZoneIndex)
        {
            pandaAnimator?.Play(orangeGlowStateName);
            if (scanButton != null) scanButton.SetActive(true);
        }
    }

    public void OnScopeExitZone(int zoneIndex)
    {
        if (isFinished) return;
        if (zoneIndex == 4) return;
        if (currentZone != zoneIndex) return;

        currentZone = -1;
        scopeDraggable?.Show();

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
        if (zoneIndex == 4) return;

        scope.MarkHandled();

        if (zoneIndex == EyeZoneIndex) return;
    }

    // ── Scan button (UI Button OnClick) ──────────────────────────────────────

    public void OnScanButtonPressed()
    {
        Debug.Log($"[PandaController] OnScanButtonPressed — isFinished:{isFinished} currentZone:{currentZone} EyeZoneIndex:{EyeZoneIndex}");
        if (isFinished) return;
        if (dialogueController != null && dialogueController.IsInputLocked) return;
        if (currentZone != EyeZoneIndex) return;

        isFinished = true;
        if (scanButton != null) scanButton.SetActive(false);
        scopeDraggable?.MarkHandled();

        MiniGameProgress.SetCompleted(GrabbableType.Panda);
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        if (confettiAnimator != null) confettiAnimator.Play(confettiStateName);
        pandaAnimator?.Play(greenGlowStateName);
        AudioManager.PlayComplete();

        yield return new WaitForSeconds(1f);
        if (dialogueController != null) dialogueController.OnLevelCompleted();
        yield return new WaitForSeconds(dialogueController != null ? dialogueController.CompletionLineDuration : 0f);

        if (congratsButton != null) congratsButton.SetActive(true);
    }

    void NotifyPlayerStarted()
    {
        if (dialogueController == null) return;
        if (!playerStarted)
        {
            playerStarted = true;
            dialogueController.OnPlayerStarted();
        }
        else
        {
            dialogueController.ResetNudgeTimer();
        }
    }

    public void OnBackToMenuPressed()
    {
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName(menuSceneName));
    }
}
