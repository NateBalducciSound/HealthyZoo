using UnityEngine;

/// Moves a UI RectTransform so it stays centered over a world-space target each frame.
/// Attach to the ScanButton. Set worldTarget to the eye zone Transform.
[RequireComponent(typeof(RectTransform))]
public class WorldToUIFollow : MonoBehaviour
{
    [Tooltip("The world-space transform to follow (e.g. the eye zone).")]
    public Transform worldTarget;

    [Tooltip("Optional pixel offset applied after the world-to-screen conversion.")]
    public Vector2 screenOffset = Vector2.zero;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Camera cam;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (worldTarget == null || canvas == null || cam == null) return;

        Vector2 screenPos = cam.WorldToScreenPoint(worldTarget.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
            out Vector2 localPoint
        );

        rectTransform.anchoredPosition = localPoint + screenOffset;
    }
}
