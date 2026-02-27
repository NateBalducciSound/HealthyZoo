using UnityEngine;
using UnityEngine.UI;

public class VerticalSliderSnapPoints : MonoBehaviour
{
    public Slider slider;

    // Custom snap points (0–1 range)
    public float[] snapPoints = { 0.05f, 0.50f, 0.70f, 0.75f };

    void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);

        // Snap immediately to closest point on start
        SnapToClosest(slider.value);
    }

    void OnSliderChanged(float value)
    {
        SnapToClosest(value);
    }

    void SnapToClosest(float value)
    {
        float closest = snapPoints[0];
        float minDistance = Mathf.Abs(value - closest);

        for (int i = 1; i < snapPoints.Length; i++)
        {
            float distance = Mathf.Abs(value - snapPoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = snapPoints[i];
            }
        }

        slider.SetValueWithoutNotify(closest);
    }
}