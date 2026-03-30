using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Generalized dialogue system for all animal/minigame scenes.
//
// Flow:
//   1. Scene loads → all input blocked, opening line plays automatically
//   2. Opening line finishes → input unlocked, wait [nudgeDelay] seconds
//   3. If player hasn't started → play first nudge line
//   4. Every [nudgeInterval] seconds after that → cycle through nudge lines
//   5. Call OnPlayerStarted() from your gameplay script to stop all nudging
//
// Setup:
//   - Attach to any GameObject in the scene (e.g. a DialogueManager object)
//   - Assign an AudioSource on the same or child GameObject
//   - Fill in openingLine and nudgeLines in the Inspector

public class DialogueController : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Opening")]
    [Tooltip("Plays once automatically when the scene loads — input is blocked until it finishes")]
    [SerializeField] private AudioClip openingLine;

    [Header("Completion")]
    [Tooltip("Plays once when the congrats button spawns — cuts off anything currently playing")]
    [SerializeField] private AudioClip completionLine;

    [Header("Nudge")]
    [Tooltip("Seconds after opening line finishes before first nudge")]
    [SerializeField] private float nudgeDelay = 5f;
    [Tooltip("Seconds between each nudge after the first")]
    [SerializeField] private float nudgeInterval = 6f;
    [Tooltip("Cycles through these in order, then loops back to the first")]
    [SerializeField] private AudioClip[] nudgeLines;

    private bool _playerStarted = false;
    private int _nudgeIndex = 0;
    private Coroutine _nudgeRoutine;
    private EventSystem _eventSystem;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _eventSystem = EventSystem.current;

        if (openingLine != null)
        {
            SetInputLocked(true);
            StartCoroutine(PlayOpeningThenNudge());
        }
        else if (nudgeLines.Length > 0)
            _nudgeRoutine = StartCoroutine(NudgeLoop(nudgeDelay));
    }

    void SetInputLocked(bool locked)
    {
        if (_eventSystem != null)
            _eventSystem.enabled = !locked;
    }

    // Call this from your gameplay script the moment the player interacts
    public void OnPlayerStarted()
    {
        if (_playerStarted) return;
        _playerStarted = true;

        if (_nudgeRoutine != null)
            StopCoroutine(_nudgeRoutine);

        audioSource.Stop();
    }

    // Call this from SpawnButton() when the congrats button appears
    public void OnLevelCompleted()
    {
        Debug.Log($"[Dialogue] OnLevelCompleted called — completionLine assigned: {completionLine != null}");
        if (completionLine == null) return;

        if (_nudgeRoutine != null)
            StopCoroutine(_nudgeRoutine);

        audioSource.Stop();
        audioSource.PlayOneShot(completionLine);
    }

    IEnumerator PlayOpeningThenNudge()
    {
        Debug.Log($"[Dialogue] Playing opening line: {openingLine.name} ({openingLine.length}s) — input locked");
        audioSource.PlayOneShot(openingLine);
        yield return new WaitForSeconds(openingLine.length);

        SetInputLocked(false);
        bool esEnabled = EventSystem.current != null && EventSystem.current.enabled;
        Debug.Log($"[Dialogue] Opening line finished — input unlocked. EventSystem enabled: {esEnabled}");

        if (!_playerStarted && nudgeLines.Length > 0)
            _nudgeRoutine = StartCoroutine(NudgeLoop(nudgeDelay));
    }

    IEnumerator NudgeLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (!_playerStarted)
        {
            if (nudgeLines.Length > 0)
            {
                AudioClip clip = nudgeLines[_nudgeIndex];
                audioSource.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
                _nudgeIndex = (_nudgeIndex + 1) % nudgeLines.Length;
            }

            yield return new WaitForSeconds(nudgeInterval);
        }
    }
}
