using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the heron drop zone Image on the canvas.
// The stethoscope (UIDraggable) will be caught here on drop.
public class UIHeronDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public HeronLevelController heronController;

    public void OnPointerEnter(PointerEventData e) { }
    public void OnPointerExit(PointerEventData e) { }

    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag == null) return;

        if (!e.pointerDrag.TryGetComponent(out UIDraggable draggable)) return;

        // Cancel the default return-to-start that fires in OnEndDrag
        draggable.ReturnToStart();

        if (heronController != null)
            heronController.ProcessHeronDrop(draggable);
    }
}
