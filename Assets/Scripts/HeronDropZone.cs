using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)] // runs before Draggable2D so MarkHandled fires first
public class HeronDropZone : MonoBehaviour
{
    public HeronLevelController heronController;

    private Draggable2D hoveredDraggable;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[HeronDropZone] OnTriggerEnter2D: {other.gameObject.name}");

        if (other.TryGetComponent(out Draggable2D draggable))
        {
            Debug.Log($"[HeronDropZone] Draggable2D found — tracking {other.gameObject.name}");
            hoveredDraggable = draggable;
        }
        else
        {
            Debug.Log($"[HeronDropZone] {other.gameObject.name} has no Draggable2D component.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[HeronDropZone] OnTriggerExit2D: {other.gameObject.name}");

        if (other.TryGetComponent(out Draggable2D draggable) && draggable == hoveredDraggable)
            hoveredDraggable = null;
    }

    private void Update()
    {
        if (hoveredDraggable == null) return;

        Debug.Log($"[HeronDropZone] Update — hovering: {hoveredDraggable.gameObject.name}, IsDragging: {hoveredDraggable.IsDragging}");

        bool released = false;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Debug.Log("[HeronDropZone] Mouse release detected");
            released = true;
        }
#endif
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            Debug.Log("[HeronDropZone] Touch release detected");
            released = true;
        }

        if (released)
        {
            Debug.Log($"[HeronDropZone] Drop detected on: {hoveredDraggable.gameObject.name}, IsDragging at release: {hoveredDraggable.IsDragging}");
            hoveredDraggable.MarkHandled();
            OnItemDropped(hoveredDraggable);
            hoveredDraggable = null;
        }
    }

    private void OnItemDropped(Draggable2D item)
    {
        Debug.Log($"[HeronDropZone] OnItemDropped: {item.gameObject.name}");

        if (heronController == null)
        {
            Debug.LogError("[HeronDropZone] heronController is not assigned!");
            return;
        }

        heronController.ProcessHeronDrop(item);
    }
}
