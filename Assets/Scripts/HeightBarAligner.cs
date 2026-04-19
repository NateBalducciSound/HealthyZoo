using UnityEngine;
using UnityEngine.UI;

// Attach to the Slider GameObject.
// Each frame, snaps the Slider's canvas X position so it lines up with the
// world-space height bar, compensating for different screen/device widths.
[RequireComponent(typeof(RectTransform))]
public class HeightBarAligner : MonoBehaviour
{
    [Tooltip("The world-space SpriteRenderer that is the height bar (base+pole).")]
    public SpriteRenderer heightBar;

    [Tooltip("Pixel offset applied after the world-to-canvas conversion. Use to fine-tune which edge of the bar the slider sits on.")]
    public float xOffset = 0f;

    [Tooltip("Leave empty to use Camera.main.")]
    public Camera cam;

    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform canvasRect;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (heightBar == null || canvas == null || cam == null) return;

        // Project the bar center into screen space (X only matters).
        Vector2 screenPos = cam.WorldToScreenPoint(heightBar.bounds.center);

        // Convert screen X to canvas-local position.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null, // ScreenSpaceOverlay: no camera
            out Vector2 localPoint
        );

        // Only update X — leave Y and sizeDelta untouched.
        Vector2 ap = rectTransform.anchoredPosition;
        ap.x = localPoint.x + xOffset;
        rectTransform.anchoredPosition = ap;
    }
}
