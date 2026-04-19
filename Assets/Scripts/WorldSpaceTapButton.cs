using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// World-space tap button. Add a SpriteRenderer and Collider2D to the same GameObject.
/// Wire onClick exactly like a UI Button's OnClick event in the Inspector.
[RequireComponent(typeof(Collider2D))]
public class WorldSpaceTapButton : MonoBehaviour
{
    public UnityEvent onClick;

    private Camera cam;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (!TappedThisFrame(out Vector2 screenPos)) return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        world.z = 0;

        foreach (var hit in Physics2D.OverlapPointAll(world))
        {
            if (hit.gameObject == gameObject)
            {
                onClick.Invoke();
                return;
            }
        }
    }

    bool TappedThisFrame(out Vector2 pos)
    {
        pos = default;
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
#endif
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        return false;
    }
}
