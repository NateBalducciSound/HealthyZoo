using System.Collections;
using UnityEngine;

public class PandaController : MonoBehaviour
{
    [Header("Scope")]
    [Tooltip("The scope sprite — visible only when zone 1 (Feet) is active.")]
    public GameObject scopeObject;

    [Header("Panda+Scope Combined Sprites")]
    [Tooltip("Index 0=Ground, 1=Feet, 2=Belly, 3=Eyes, 4=TopOfHead.")]
    public GameObject[] combinedSprites;

    [Header("Sprite Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    [Header("Scan Button")]
    public GameObject scanButton;

    [Header("Panda Animator")]
    public Animator pandaAnimator;
    public string orangeGlowStateName      = "OrangeButton";
    public string greenGlowStateName       = "GreenButton";

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

    private int  activeIndex   = 0;
    private bool isFinished    = false;
    private bool playerStarted = false;

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
        if (scopeObject    != null) scopeObject.SetActive(true);
    }

    // ── Called by PandaPositionZone ───────────────────────────────────────────

    public void OnZoneTapped(int zoneIndex)
    {
        if (isFinished) return;
        if (dialogueController != null && dialogueController.IsInputLocked) return;
        if (scanButton != null && scanButton.activeSelf) return;
        if (zoneIndex == activeIndex) return;
        if (zoneIndex < 0 || zoneIndex >= combinedSprites.Length) return;

        NotifyPlayerStarted();

        if (activeIndex >= 0 && activeIndex < combinedSprites.Length && combinedSprites[activeIndex] != null)
            combinedSprites[activeIndex].SetActive(false);

        combinedSprites[zoneIndex].SetActive(true);
        activeIndex = zoneIndex;

        if (scopeObject != null) scopeObject.SetActive(zoneIndex == 0);

        bool atEyes = zoneIndex == EyeZoneIndex;
        if (atEyes) pandaAnimator?.Play(orangeGlowStateName);
        if (scanButton != null) scanButton.SetActive(atEyes);
    }

    // ── Scan button (UI Button OnClick) ──────────────────────────────────────

    public void OnScanButtonPressed()
    {
        if (isFinished) return;
        if (dialogueController != null && dialogueController.IsInputLocked) return;

        isFinished = true;
        if (scanButton != null) scanButton.SetActive(false);

        AudioManager.PlayScanButton();
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
