using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Place on a UI RectTransform (child of a Screen Space canvas) that covers a
/// tap area.  When the player taps inside this rect, it tells SlothController
/// to move the sloth to this position index (0, 1, or 2).
///
/// Using a canvas RectTransform instead of a world-space Collider2D means the
/// zones stay correctly anchored on every screen aspect ratio, and they can
/// remain active even while the sloth GameObjects they correspond to are disabled.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SlothPositionZone : MonoBehaviour
{
    public SlothController slothController;

    [Tooltip("Which sloth sprite index this zone activates (0, 1, or 2).")]
    public int positionIndex;

    private RectTransform rectTransform;
    private Camera mainCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleTouch();
#if UNITY_EDITOR
        HandleMouse();
#endif
    }

    void HandleTouch()
    {
        if (Touchscreen.current == null) return;
        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return;

        Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        if (IsTappingThis(screenPos) && !IsTappingDraggable(screenPos))
            slothController?.OnPositionTapped(positionIndex);
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        if (IsTappingThis(screenPos) && !IsTappingDraggable(screenPos))
            slothController?.OnPositionTapped(positionIndex);
    }
#endif

    // Returns true if the tap landed on the Draggable2D (the cuff) so we don't
    // accidentally switch sloth position while the player is grabbing the cuff.
    bool IsTappingDraggable(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        world.z = 0f;
        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        foreach (var hit in hits)
            if (hit.TryGetComponent<Draggable2D>(out _)) return true;
        return false;
    }

    bool IsTappingThis(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, null);
    }
}
