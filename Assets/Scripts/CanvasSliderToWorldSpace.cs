using UnityEngine;
using UnityEngine.UI;

// Matches the canvas slider's screen position to the world-space object's screen position,
// with a manual offset to fine-tune alignment.
public class CanvasSliderToWorldSpace : MonoBehaviour
{
    public RectTransform canvasSliderRect;
    public Transform worldHandle;
    public Vector2 offset;

    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (canvasSliderRect == null || worldHandle == null) return;

        Vector2 screenPos = _cam.WorldToScreenPoint(worldHandle.position);
        canvasSliderRect.position = new Vector3(screenPos.x + offset.x, screenPos.y + offset.y, 0f);
    }
}
