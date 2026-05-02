using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class DialogueController : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Captions")]
    [Tooltip("Caption box placed in the scene. Leave null to disable captions.")]
    [SerializeField] private GameObject captionBox;
    [Tooltip("Seconds the caption stays visible after a line finishes.")]
    [SerializeField] private float captionLingerTime = 1f;

    private TMP_Text _captionText;

    [Header("Opening")]
    [Tooltip("Plays once automatically when the scene loads — input is blocked until it finishes")]
    [SerializeField] private LocalizedClip openingLine;
    [SerializeField] private LocalizedCaption openingCaption;

    [Header("Completion")]
    [Tooltip("Plays once when the congrats button spawns — cuts off anything currently playing")]
    [SerializeField] private LocalizedClip completionLine;
    [SerializeField] private LocalizedCaption completionCaption;

    [Header("Nudge")]
    [Tooltip("When enabled, nudge timer starts as soon as the opening line finishes and is never reset by player interaction.")]
    [SerializeField] private bool nudgeOnFixedTimer = false;
    [Tooltip("Seconds after opening line finishes before first nudge")]
    [SerializeField] private float nudgeDelay = 5f;
    [Tooltip("Seconds between each nudge after the first")]
    [SerializeField] private float nudgeInterval = 6f;
    [Tooltip("Cycles through these in order, then loops back to the first")]
    [SerializeField] private LocalizedClip[] nudgeLines;
    [SerializeField] private LocalizedCaption[] nudgeCaptions;

    public bool IsInputLocked { get; private set; } = false;
    public System.Action OnInputUnlocked;
    public float CompletionLineDuration => LanguageManager.Resolve(completionLine) != null ? LanguageManager.Resolve(completionLine).length + captionLingerTime : 0f;

    private bool _playerStarted = false;
    private bool _finished = false;
    private int _nudgeIndex = 0;
    private Coroutine _nudgeRoutine;
    private EventSystem _eventSystem;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _eventSystem = EventSystem.current;

        ShowCaption(string.Empty);

        AudioClip opening = LanguageManager.Resolve(openingLine);
        if (opening != null)
        {
            SetInputLocked(true);
            StartCoroutine(PlayOpeningThenNudge(opening));
        }
        else if (nudgeLines.Length > 0)
            _nudgeRoutine = StartCoroutine(NudgeLoop(nudgeDelay));
    }

    void SetInputLocked(bool locked)
    {
        IsInputLocked = locked;
        if (_eventSystem != null)
            _eventSystem.enabled = !locked;
        if (!locked)
            OnInputUnlocked?.Invoke();
    }

    public void OnPlayerStarted()
    {
        if (_playerStarted) return;
        _playerStarted = true;
        audioSource.Stop();
        ShowCaption(string.Empty);
        if (!nudgeOnFixedTimer)
            ResetNudgeTimer();
    }

    public void ResetNudgeTimer()
    {
        if (_finished) return;
        if (nudgeOnFixedTimer) return;
        if (_nudgeRoutine != null) StopCoroutine(_nudgeRoutine);
        if (nudgeLines.Length > 0)
            _nudgeRoutine = StartCoroutine(NudgeLoop(nudgeDelay));
    }

    public void OnLevelCompleted()
    {
        _finished = true;
        if (_nudgeRoutine != null) StopCoroutine(_nudgeRoutine);
        audioSource.Stop();

        AudioClip clip = LanguageManager.Resolve(completionLine);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            ShowCaption(LanguageManager.ResolveCaption(completionCaption));
            StartCoroutine(ClearCaptionAfter(clip.length));
        }
        else
        {
            ShowCaption(string.Empty);
        }
    }

    IEnumerator PlayOpeningThenNudge(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
        ShowCaption(LanguageManager.ResolveCaption(openingCaption));
        yield return new WaitForSeconds(clip.length);

        StartCoroutine(ClearCaptionAfter(0f));
        SetInputLocked(false);

        if (nudgeLines.Length > 0 && (!_playerStarted || nudgeOnFixedTimer))
            _nudgeRoutine = StartCoroutine(NudgeLoop(nudgeDelay));
    }

    IEnumerator NudgeLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (!_finished)
        {
            if (nudgeLines.Length > 0)
            {
                AudioClip clip = LanguageManager.Resolve(nudgeLines[_nudgeIndex]);
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);

                    string caption = nudgeCaptions != null && _nudgeIndex < nudgeCaptions.Length
                        ? LanguageManager.ResolveCaption(nudgeCaptions[_nudgeIndex])
                        : string.Empty;
                    ShowCaption(caption);

                    yield return new WaitForSeconds(clip.length);
                    StartCoroutine(ClearCaptionAfter(0f));
                }

                _nudgeIndex = (_nudgeIndex + 1) % nudgeLines.Length;
            }

            yield return new WaitForSeconds(nudgeInterval);
        }
    }

    IEnumerator ClearCaptionAfter(float delay)
    {
        yield return new WaitForSeconds(delay + captionLingerTime);
        ShowCaption(string.Empty);
    }

    void ShowCaption(string text)
    {
        if (captionBox == null) return;
        bool hasText = !string.IsNullOrEmpty(text) && LanguageManager.ShowCaptions;

        if (!hasText)
        {
            captionBox.SetActive(false);
            return;
        }

        if (_captionText == null)
            _captionText = captionBox.GetComponentInChildren<TMP_Text>();

        captionBox.SetActive(true);
        if (_captionText != null)
            _captionText.text = text;
    }
}
