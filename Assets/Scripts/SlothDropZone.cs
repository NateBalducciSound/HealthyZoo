using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach one of these to each sloth sprite (as a child collider or on the
/// sprite itself).  When the blood-pressure cuff is dropped here, it notifies
/// SlothController which position index received the drop.
/// </summary>
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Collider2D))]
public class SlothDropZone : MonoBehaviour
{
    public SlothController slothController;

    [Tooltip("Which sloth position index this drop zone belongs to (0, 1, or 2).")]
    public int positionIndex;

    private Draggable2D hoveredDraggable;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Draggable2D draggable))
            hoveredDraggable = draggable;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Draggable2D draggable) && draggable == hoveredDraggable)
            hoveredDraggable = null;
    }

    private void Update()
    {
        if (hoveredDraggable == null || !hoveredDraggable.IsDragging) return;

        bool released = false;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            released = true;
#endif
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            released = true;

        if (released)
        {
            var item = hoveredDraggable;
            hoveredDraggable = null;
            // Let SlothController decide — only mark handled if it's a valid drop
            slothController?.HandleCuffDrop(positionIndex, item);
        }
    }
}
