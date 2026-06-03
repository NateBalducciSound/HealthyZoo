using UnityEngine;
using UnityEngine.UI;

public class PlayButtonFlash : MonoBehaviour
{
    [Tooltip("How much the button grows at peak (e.g. 0.08 = 8% larger).")]
    public float scaleAmount = 0.08f;

    [Tooltip("Pulses per second.")]
    public float flashSpeed = 1.2f;

    [Tooltip("Color to tint toward at peak.")]
    public Color peakColor = new Color(0.88f, 0.722f, 0.353f, 1f); // matches caption box border gold

    private Vector3 baseScale;
    private Image image;

    void Start()
    {
        baseScale = transform.localScale;
        image = GetComponent<Image>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * flashSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

        transform.localScale = baseScale * (1f + scaleAmount * t);

        if (image != null)
            image.color = Color.Lerp(Color.white, peakColor, t);
    }
}
