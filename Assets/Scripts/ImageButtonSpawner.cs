using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ImageButtonSpawner : MonoBehaviour, IPointerClickHandler
{
    public GrabbableType grabbableType;
    public PlayButtonSpawner spawner;

    public void OnPointerClick(PointerEventData eventData)
    {
        spawner.setSelectedGrabbable(grabbableType);
    }
}
