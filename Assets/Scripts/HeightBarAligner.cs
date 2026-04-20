using UnityEngine;
using UnityEngine.UI;

// Attach to the height bar world-space GameObject.
// Anchors the bottom of the height bar to the bottom of the slider rect,
// keeping X alignment and bottom overlap consistent across all screen sizes.
public class HeightBarAligner : MonoBehaviour
{
    [Header("References")]
    public RectTransform sliderRect;
    public Canvas canvas;
    public Camera cam;

    [Header("X Alignment (from slider)")]
    public bool alignX = true;
    public float offsetX = 0f;

    [Header("Y — Giraffe Anchor")]
    [Tooltip("The giraffe SpriteRenderer. Bar bottom is pinned to giraffe bottom.")]
    public SpriteRenderer giraffeRenderer;
    [Tooltip("World-unit offset between giraffe bottom and bar bottom. " +
             "Negative = bar bottom sits below giraffe bottom (overlap). " +
             "Positive = bar bottom sits above giraffe bottom.")]
    public float barBottomOffset = 0f;

    private Vector3[] _corners = new Vector3[4];

    void LateUpdate()
    {
        if (sliderRect == null || cam == null || canvas == null) return;

        sliderRect.GetWorldCorners(_corners);

        Vector3 worldBottom = CanvasCornerToWorld(_corners[0]);
        Vector3 worldTop    = CanvasCornerToWorld(_corners[1]);
        Vector3 center      = (worldBottom + worldTop) * 0.5f;

        Vector3 pos = transform.position;

        // X only — unchanged from working version.
        if (alignX) pos.x = center.x + offsetX;

        // Y — pin bar bottom to giraffe bottom in world space.
        // Bar scale is left alone so it stays a fixed world size.
        if (giraffeRenderer != null)
        {
            float barHalfHeight = GetComponent<SpriteRenderer>() != null
                ? GetComponent<SpriteRenderer>().bounds.extents.y
                : 0f;
            float barBottomY = giraffeRenderer.bounds.min.y + barBottomOffset;
            pos.y = barBottomY + barHalfHeight;
        }

        transform.position = pos;
    }

    Vector3 CanvasCornerToWorld(Vector3 corner)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screen = corner;
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }
        return corner;
    }
}
