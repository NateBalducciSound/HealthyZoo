using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Plays a voice line once per app session each time the player enters this scene.
// Resets when the app is closed and reopened. Attach to any GameObject in the scene.

public class FirstVisitDialogue : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip firstVisitLine;

    [Header("Settings")]
    [Tooltip("Unique key for this scene — must be different for every scene that uses this script")]
    [SerializeField] private string prefsKey = "FirstVisit_MiniGameSelect";
    [Tooltip("Block all input while the line plays")]
    [SerializeField] private bool lockInputDuringLine = true;

    // Tracks which keys have already played this session (resets on app restart).
    private static readonly System.Collections.Generic.HashSet<string> _playedThisSession = new();

    private EventSystem _eventSystem;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _eventSystem = EventSystem.current;

        if (!_playedThisSession.Contains(prefsKey))
            StartCoroutine(PlayFirstVisit());
    }

    IEnumerator PlayFirstVisit()
    {
        _playedThisSession.Add(prefsKey);

        if (lockInputDuringLine && _eventSystem != null)
            _eventSystem.enabled = false;

        audioSource.PlayOneShot(firstVisitLine);
        yield return new WaitForSeconds(firstVisitLine.length);

        if (lockInputDuringLine && _eventSystem != null)
            _eventSystem.enabled = true;
    }

    // Call this from the Editor or a debug button to force it to play again this session.
    [ContextMenu("Reset First Visit")]
    public void ResetFirstVisit()
    {
        _playedThisSession.Remove(prefsKey);
        Debug.Log($"[FirstVisit] Reset key: {prefsKey}");
    }
}
