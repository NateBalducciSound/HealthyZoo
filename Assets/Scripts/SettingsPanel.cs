using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Slider volumeSlider;
    public GameObject panel;

    const string PrefKey = "SettingsVolume";

    void Start()
    {
        float saved = PlayerPrefs.GetFloat(PrefKey, 1f);
        volumeSlider.value = saved;
        AudioListener.volume = saved;
        volumeSlider.onValueChanged.AddListener(OnSliderChanged);
        panel.SetActive(false);
    }

    public void TogglePanel() => panel.SetActive(!panel.activeSelf);

    void OnSliderChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PrefKey, value);
    }
}
