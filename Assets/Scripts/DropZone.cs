using UnityEngine;

public enum DropZoneType
{
    Eyes,
    Hand,
    Nose,
    Mouth
}

public class DropZone2D : MonoBehaviour
{
    public DropZoneType zoneType;
    public AlligatorController alligatorController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Draggable2D draggable = other.GetComponent<Draggable2D>();
        if (draggable == null) return;

        alligatorController.HandleDrop(zoneType, draggable);
    }
}