using UnityEngine;

public class HeronDropZone : MonoBehaviour 
{
    public HeronLevelController heronController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Draggable2D looks for 'DropZone2D', so we find it on this object
        if (other.TryGetComponent(out Draggable2D draggable))
        {
            DropZone2D bridge = GetComponent<DropZone2D>();
            draggable.SetCurrentHoverZone(bridge);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Draggable2D draggable))
        {
            draggable.SetCurrentHoverZone(null);
        }
    }

    // This is the method that Draggable2D will call on the DropZone2D script.
    // We will place a redirect inside DropZone2D to call this.
    public void OnItemDropped(Draggable2D item)
    {
        heronController.ProcessHeronDrop(item);
    }
}