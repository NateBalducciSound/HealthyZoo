using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(Collider2D))]
public class PandaPositionZone : MonoBehaviour
{
    public PandaController pandaController;

    [Tooltip("0=Feet, 1=Belly, 2=Chest, 3=Eyes, 4=TopOfHead")]
    public int zoneIndex;

    private Draggable2D hoveredDraggable;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Draggable2D draggable)) return;
        if (!draggable.IsDragging) return;

        hoveredDraggable = draggable;
        pandaController?.OnScopeEnterZone(zoneIndex);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out Draggable2D draggable)) return;
        if (draggable != hoveredDraggable) return;

        hoveredDraggable = null;
        pandaController?.OnScopeExitZone(zoneIndex);
    }

    void Update()
    {
        if (hoveredDraggable == null || !hoveredDraggable.IsDragging) return;

        bool released = false;
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            released = true;
#endif
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            released = true;

        if (!released) return;

        var item = hoveredDraggable;
        hoveredDraggable = null;
        pandaController?.OnScopeReleasedInZone(zoneIndex, item);
    }
}
