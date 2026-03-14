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
        // Debug 1: Confirm the physics engine detected the overlap
        Debug.Log("Something entered the trigger: " + other.name);

        Draggable2D draggable = other.GetComponent<Draggable2D>();
        
        if (draggable == null) 
        {
            // Debug 2: The object hit the zone, but it's not our draggable item
            Debug.LogWarning(other.name + " does not have a Draggable2D component!");
            return;
        }

        // Debug 3: Success! This confirms the specific enum type being sent
        Debug.Log("Successfully dropped item on zone: " + zoneType);
        
        alligatorController.HandleDrop(zoneType, draggable);
    }
}