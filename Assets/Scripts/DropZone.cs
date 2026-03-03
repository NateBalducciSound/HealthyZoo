using UnityEngine;
using UnityEngine.EventSystems;

public enum DropZoneType
{
    Eyes,
    Hand,
    Nose,
    Mouth
}

public class DropZone : MonoBehaviour, IDropHandler
{
    public DropZoneType zoneType;
    public AlligatorController alligatorController;

    public void OnDrop(PointerEventData eventData)
    {
        DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable == null) return;

        alligatorController.HandleDrop(zoneType, draggable);
    }
}