using UnityEngine;

public class DropZone2D : MonoBehaviour
{
    public AlligatorController alligatorController;
    public DropZoneType dropZoneType; // This now works!

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Draggable2D draggable))
        {
            draggable.SetCurrentHoverZone(this);
            Debug.Log($"<color=yellow>Hovering:</color> {dropZoneType}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Draggable2D draggable))
        {
            draggable.SetCurrentHoverZone(null);
            Debug.Log($"<color=white>Exited:</color> {dropZoneType}");
        }
    }

    public Vector3 SnapPosition => new Vector3(transform.position.x, transform.position.y, 0f);

    public void ConfirmDrop(Draggable2D item)
    {
        if (alligatorController == null) return;
        alligatorController.HandleDrop(dropZoneType, item);
    }
}