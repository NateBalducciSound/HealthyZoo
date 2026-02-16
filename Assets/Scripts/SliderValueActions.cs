using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class SliderValueActions : MonoBehaviour
{
    public Slider slider;

    public GameObject buttonPrefab;
    public Transform buttonParent;
    public GameObject playButtonPrefab;

    private GameObject currentButton;

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);
        OnSliderChanged(slider.value); // handle starting value
    }

    void OnSliderChanged(float value)
    {
        int v = Mathf.RoundToInt(value);

        if (v == 3)
        {
            SpawnButtonIfNeeded();
        }
        else
        {
            RemoveButton();
        }
    }

    void SpawnButtonIfNeeded()
    {
        if (currentButton != null) return;

        currentButton = Instantiate(playButtonPrefab, buttonParent);

        Button btn = currentButton.GetComponent<Button>();
        btn.onClick.AddListener(OnPlayPressed);
    }

    void RemoveButton()
    {
        if (currentButton == null) return;

        Destroy(currentButton);
        currentButton = null;
    }

    public void OnPlayPressed()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Giraffe);
        Debug.Log("COMPLETED: " + GrabbableType.Giraffe);
        SceneManager.LoadScene("Scenes/_02MiniGameSelect");
    }
}
