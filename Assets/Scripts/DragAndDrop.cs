using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDrop : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{

    public GrabbableType grabbableType;
    // Might need to workshop for later implementation
    private RectTransform rectTransform;
    private Image grabbableObject;

    private Canvas canvas;

// all "drag" are implemented to not throw errors

    public void OnBeginDrag(PointerEventData eventData)
    {
        grabbableObject.color = new Color32 (255,255,255,170);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //using this one because final won't have mouse location
       rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        //or

        // transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        grabbableObject.color = new Color32 (255, 255, 255, 255);
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        grabbableObject = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();

    }

    // Update is called once per frame
    void Update()
    {
    }
}
