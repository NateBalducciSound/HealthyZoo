using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Effects")]
    public AudioClip buttonClickClip;
    public AudioClip levelCompleteClip;
    public AudioClip[] sliderNotchClips;
    public AudioClip scanButtonClip;

    [Range(0f, 1f)] public float clickVolume      = 1f;
    [Range(0f, 1f)] public float completeVolume   = 1f;
    [Range(0f, 1f)] public float notchVolume      = 1f;
    [Range(0f, 1f)] public float scanButtonVolume = 1f;

    private int _notchIndex = 0;

    [Header("Mixer")]
    public AudioMixerGroup mixerGroup;

    private AudioSource _source;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        if (mixerGroup != null) _source.outputAudioMixerGroup = mixerGroup;
    }

    public static void PlayClick()
    {
        if (Instance == null || Instance.buttonClickClip == null) return;
        Instance._source.PlayOneShot(Instance.buttonClickClip, Instance.clickVolume);
    }

    public static void PlayComplete()
    {
        if (Instance == null || Instance.levelCompleteClip == null) return;
        Instance._source.PlayOneShot(Instance.levelCompleteClip, Instance.completeVolume);
    }

    public static void PlayScanButton()
    {
        if (Instance == null || Instance.scanButtonClip == null) return;
        Instance._source.PlayOneShot(Instance.scanButtonClip, Instance.scanButtonVolume);
    }

    public static void PlayNotch()
    {
        if (Instance == null || Instance.sliderNotchClips == null || Instance.sliderNotchClips.Length == 0) return;
        AudioClip clip = Instance.sliderNotchClips[Instance._notchIndex % Instance.sliderNotchClips.Length];
        if (clip != null) Instance._source.PlayOneShot(clip, Instance.notchVolume);
        Instance._notchIndex++;
    }
}
