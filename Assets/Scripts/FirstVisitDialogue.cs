using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Plays a voice line the first time the player ever enters this scene.
// Uses PlayerPrefs to remember if it has already played — won't repeat on future visits.
// Attach to any GameObject in the scene (e.g. DialogueManager).

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

    private EventSystem _eventSystem;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _eventSystem = EventSystem.current;

        if (PlayerPrefs.GetInt(prefsKey, 0) == 0)
            StartCoroutine(PlayFirstVisit());
    }

    IEnumerator PlayFirstVisit()
    {
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();

        if (lockInputDuringLine && _eventSystem != null)
            _eventSystem.enabled = false;

        audioSource.PlayOneShot(firstVisitLine);
        yield return new WaitForSeconds(firstVisitLine.length);

        if (lockInputDuringLine && _eventSystem != null)
            _eventSystem.enabled = true;
    }

    // Call this from the Editor or a debug button to reset so the line plays again
    [ContextMenu("Reset First Visit")]
    public void ResetFirstVisit()
    {
        PlayerPrefs.DeleteKey(prefsKey);
        Debug.Log($"[FirstVisit] Reset key: {prefsKey}");
    }
}
