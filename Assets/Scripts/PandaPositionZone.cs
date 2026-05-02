using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PandaPositionZone : MonoBehaviour
{
    public PandaController pandaController;

    [Tooltip("0=Ground, 1=Feet, 2=Belly, 3=Eyes, 4=TopOfHead")]
    public int zoneIndex;

    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        HandleTouch();
#if UNITY_EDITOR
        HandleMouse();
#endif
    }

    void HandleTouch()
    {
        if (Touchscreen.current == null) return;
        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return;

        Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        if (IsTappingThis(screenPos))
            pandaController?.OnZoneTapped(zoneIndex);
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        if (IsTappingThis(screenPos))
            pandaController?.OnZoneTapped(zoneIndex);
    }
#endif

    bool IsTappingThis(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        world.z = 0f;

        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        foreach (var hit in hits)
            if (hit.gameObject == gameObject) return true;
        return false;
    }
}
