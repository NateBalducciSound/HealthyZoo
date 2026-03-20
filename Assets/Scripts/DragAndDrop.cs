using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable2D : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 startPosition;
    private Vector3 offset;
    private bool isDragging = false;
    private bool wasHandled = false;
    private DropZone2D currentHoverZone;

    private void Awake()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
    }

    // THIS WAS MISSING: The method for the DropZone to talk to this script
    public void SetCurrentHoverZone(DropZone2D zone)
    {
        currentHoverZone = zone;
    }

    private void Update()
    {
        HandleTouch();
#if UNITY_EDITOR
        HandleMouse(); 
#endif
    }

    void HandleTouch()
    {
        if (Touchscreen.current == null) return;

        // START DRAG
        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = ScreenToWorld(screenPos);

            if (IsTouchingObject(worldPos))
            {
                isDragging = true;
                offset = transform.position - worldPos;
                wasHandled = false;
            }
        }

        // DRAGGING
        if (Touchscreen.current.primaryTouch.press.isPressed && isDragging)
        {
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = ScreenToWorld(screenPos);
            transform.position = worldPos + offset;
        }

        // END DRAG (RELEASE)
        if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            ProcessRelease();
        }
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 world = ScreenToWorld(Mouse.current.position.ReadValue());
            if (IsTouchingObject(world))
            {
                isDragging = true;
                offset = transform.position - world;
                wasHandled = false;
            }
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            Vector3 world = ScreenToWorld(Mouse.current.position.ReadValue());
            transform.position = world + offset;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            ProcessRelease();
        }
    }
#endif

    // Shared logic for both Touch and Mouse release
    void ProcessRelease()
    {
        if (currentHoverZone != null)
        {
            // Successfully dropped!
            currentHoverZone.ConfirmDrop(this);
            MarkHandled();
        }
        else
        {
            // Dropped in the middle of nowhere
            ReturnToStart();
        }
    }

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z))
        );
        world.z = 0;
        return world;
    }

    bool IsTouchingObject(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        return hit != null && hit.gameObject == gameObject;
    }

    public void MarkHandled()
    {
        wasHandled = true;
    }

    public void ReturnToStart(float delay = 0f)
    {
        // Cancel any existing resets to prevent double-teleporting
        CancelInvoke(nameof(ResetPosition));
        Invoke(nameof(ResetPosition), delay);
    }

    void ResetPosition()
    {
        transform.position = startPosition;
        wasHandled = false;
    }
}