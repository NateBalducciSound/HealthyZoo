using UnityEngine;
using UnityEngine.InputSystem;

// Automatically added to AR characters at spawn.
// Drag the character by tapping near it and moving your finger.
public class ARCharacterDragger : MonoBehaviour
{
    [Tooltip("How many pixels from the character's screen centre counts as a hit.")]
    public float tapRadiusPixels = 1000f;

    private Camera _cam;
    private bool _dragging;
    private float _dragDepth;
    private Vector3 _dragOffset;

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        if (Touchscreen.current != null)
            HandleTouch();
#if UNITY_EDITOR
        else
            HandleMouse();
#endif
    }

    void HandleTouch()
    {
        var touch = Touchscreen.current.primaryTouch;

        if (touch.press.wasPressedThisFrame)
            TryBeginDrag(touch.position.ReadValue());
        else if (touch.press.isPressed && _dragging)
            DragTo(touch.position.ReadValue());
        else if (touch.press.wasReleasedThisFrame)
            _dragging = false;
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag(Mouse.current.position.ReadValue());
        else if (Mouse.current.leftButton.isPressed && _dragging)
            DragTo(Mouse.current.position.ReadValue());
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
            _dragging = false;
    }
#endif

    void TryBeginDrag(Vector2 screenPos)
    {
        // Project the character's world position to screen space and check distance.
        Vector2 charScreen = _cam.WorldToScreenPoint(transform.position);
        float dist = Vector2.Distance(screenPos, charScreen);

        if (dist > tapRadiusPixels) return;

        _dragging = true;
        _dragDepth = Vector3.Distance(_cam.transform.position, transform.position);
        Vector3 worldPos = ScreenToWorld(screenPos);
        _dragOffset = transform.position - worldPos;
    }

    void DragTo(Vector2 screenPos)
    {
        transform.position = ScreenToWorld(screenPos) + _dragOffset;
    }

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        return _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, _dragDepth));
    }
}
