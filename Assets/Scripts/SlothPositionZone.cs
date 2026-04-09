using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to an invisible 2D collider that covers a "tap area" on screen.
/// When the player taps this zone, it tells SlothController to move the
/// sloth to this position index (0, 1, or 2).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SlothPositionZone : MonoBehaviour
{
    public SlothController slothController;

    [Tooltip("Which sloth sprite index this zone activates (0, 1, or 2).")]
    public int positionIndex;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        // Ensure collider is trigger so it doesn't block physics
        GetComponent<Collider2D>().isTrigger = true;
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

    // Returns true if the tap landed on a Draggable2D (the cuff) so we don't
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
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        world.z = 0f;

        // OverlapPointAll so layered colliders don't block each other
        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        foreach (var hit in hits)
            if (hit.gameObject == gameObject) return true;
        return false;
    }
}
