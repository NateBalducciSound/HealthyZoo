using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BootstrapFromResources()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<AudioManager>("AudioManager");
        if (prefab != null) Instantiate(prefab);
    }

    [Header("Sound Effects")]
    public AudioClip buttonClickClip;
    public AudioClip levelCompleteClip;
    public AudioClip correctActionClip;
    public AudioClip incorrectActionClip;
    public AudioClip[] sliderNotchClips;
    public AudioClip scanButtonClip;

    [Range(0f, 1f)] public float clickVolume         = 1f;
    [Range(0f, 1f)] public float completeVolume       = 1f;
    [Range(0f, 1f)] public float correctActionVolume    = 1f;
    [Range(0f, 1f)] public float incorrectActionVolume  = 1f;
    [Range(0f, 1f)] public float notchVolume          = 1f;
    [Range(0f, 1f)] public float scanButtonVolume     = 1f;

    private int _notchIndex = 0;

    [Header("Music")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Mixer")]
    public AudioMixerGroup mixerGroup;

    private AudioSource _source;
    private AudioSource _musicSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        if (mixerGroup != null) _source.outputAudioMixerGroup = mixerGroup;

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        if (mixerGroup != null) _musicSource.outputAudioMixerGroup = mixerGroup;
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

    public static void PlayCorrect()
    {
        if (Instance == null || Instance.correctActionClip == null) return;
        Instance._source.PlayOneShot(Instance.correctActionClip, Instance.correctActionVolume);
    }

    public static void PlayIncorrect()
    {
        if (Instance == null || Instance.incorrectActionClip == null) return;
        Instance._source.PlayOneShot(Instance.incorrectActionClip, Instance.incorrectActionVolume);
    }

    public static void StartMusic(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        Instance._musicSource.clip = clip;
        Instance._musicSource.volume = Instance.musicVolume;
        Instance._musicSource.Play();
    }

    public static void StopMusic()
    {
        if (Instance == null) return;
        Instance._musicSource.Stop();
    }

    public static void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        Instance._source.PlayOneShot(clip, volume);
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
