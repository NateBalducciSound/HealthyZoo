using UnityEngine;

public class MiniGameMusic : MonoBehaviour
{
    public AudioClip musicClip;

    void Start()
    {
        AudioManager.StartMusic(musicClip);
    }

    void OnDestroy()
    {
        AudioManager.StopMusic();
    }
}
