using UnityEngine;
using UnityEngine.EventSystems;


public class DropZone : MonoBehaviour, IDropHandler
{
    public PlayButtonSpawner playButtonSpawner;
    public void OnDrop(PointerEventData eventData)
    {
        DragAndDrop dragged = eventData.pointerDrag?.GetComponent<DragAndDrop>();
        if (dragged == null ) return;
        //tell spawner to create button
        playButtonSpawner.setSelectedGrabbable(dragged.grabbableType); 
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
