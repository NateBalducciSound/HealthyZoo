using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable2D : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Optional: Drop an empty GameObject here to define a custom respawn location.")]
    public Transform spawnPoint; 

    private Camera mainCamera;
    private Vector3 startPosition;
    private Vector3 offset;
    private bool isDragging = false;
    private bool wasHandled = false;
    private DropZone2D currentHoverZone;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // --- NEW SPAWN LOGIC ---
        // If a spawn point is assigned, use it. Otherwise, use current position.
        if (spawnPoint != null)
        {
            startPosition = spawnPoint.position;
            transform.position = startPosition; // Snap to spawn point on start
        }
        else
        {
            startPosition = transform.position;
        }
    }

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

        if (Touchscreen.current.primaryTouch.press.isPressed && isDragging)
        {
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = ScreenToWorld(screenPos);
            transform.position = worldPos + offset;
        }

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

    void ProcessRelease()
    {
        if (currentHoverZone != null)
        {
            currentHoverZone.ConfirmDrop(this);
            // We don't call MarkHandled here automatically anymore because 
            // the Heron Controller might decide it was a "Wrong Position" drop.
        }
        else
        {
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
        CancelInvoke(nameof(ResetPosition));
        Invoke(nameof(ResetPosition), delay);
    }

    void ResetPosition()
    {
        // --- UPDATED RESET LOGIC ---
        // Always check the spawn point's latest position if it exists
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }
        else
        {
            transform.position = startPosition;
        }
        
        wasHandled = false;
    }
}