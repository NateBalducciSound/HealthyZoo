using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Draggable2D : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Optional: Drop an empty GameObject here to define a custom respawn location.")]
    public Transform spawnPoint;

    [Header("Constraints")]
    [Tooltip("When enabled, the X position is locked to the spawn/start X so the object only moves vertically.")]
    public bool lockXAxis = false;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    private Camera mainCamera;
    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Vector3 offset;
    public bool IsDragging { get; private set; }
    private bool isDragging { get => IsDragging; set => IsDragging = value; }
    private bool wasHandled = false;
    private bool _hasNotifiedDialogue = false;
    private DropZone2D currentHoverZone;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        
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

            if (IsTouchingObject(worldPos) && !IsDialogueLocked())
            {
                isDragging = true;
                if (rb != null) rb.isKinematic = true;
                offset = transform.position - worldPos;
                wasHandled = false;
                NotifyDialogueStarted();
            }
        }

        if (Touchscreen.current.primaryTouch.press.isPressed && isDragging)
        {
            Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Vector3 worldPos = ScreenToWorld(screenPos);
            Vector3 newPos = worldPos + offset;
            if (lockXAxis) newPos.x = startPosition.x;
            transform.position = newPos;
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
            if (IsTouchingObject(world) && !IsDialogueLocked())
            {
                isDragging = true;
                if (rb != null) rb.isKinematic = true;
                offset = transform.position - world;
                wasHandled = false;
                NotifyDialogueStarted();
            }
        }

        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            Vector3 world = ScreenToWorld(Mouse.current.position.ReadValue());
            Vector3 newPos = world + offset;
            if (lockXAxis) newPos.x = startPosition.x;
            transform.position = newPos;
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
        if (wasHandled) return;

        // Physics triggers can lag a frame behind transform movement, so do an
        // immediate overlap check at the current position to catch fast drops.
        DropZone2D zone = currentHoverZone;
        if (zone == null)
        {
            Collider2D hit = Physics2D.OverlapPoint(transform.position);
            if (hit != null) hit.TryGetComponent(out zone);
        }

        if (zone != null)
            zone.ConfirmDrop(this);
        else
            ReturnToStart();
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
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        foreach (var hit in hits)
            if (hit.gameObject == gameObject) return true;
        return false;
    }

    bool IsDialogueLocked() => dialogueController != null && dialogueController.IsInputLocked;

    void NotifyDialogueStarted()
    {
        if (_hasNotifiedDialogue) return;
        _hasNotifiedDialogue = true;
        if (dialogueController != null) dialogueController.OnPlayerStarted();
    }

    public void Hide()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
    }

    public void Show()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = true;
    }

    public void ShowAtStart()
    {
        transform.position = spawnPoint != null ? spawnPoint.position : startPosition;
        wasHandled = false;
        Show();
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