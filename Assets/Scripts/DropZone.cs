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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
