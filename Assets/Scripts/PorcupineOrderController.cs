using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PorcupineOrderController : MonoBehaviour
{
    // ─── Data Types ───────────────────────────────────────────────────────────

    [System.Serializable]
    public class OrderItem
    {
        public SpriteRenderer renderer;
        [Tooltip("Which slot index (0, 1, 2) this item belongs in when correct")]
        public int correctSlotIndex;
        [HideInInspector] public int currentSlotIndex;
        [HideInInspector] public bool isLocked;
    }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Items (assign all 3)")]
    public OrderItem[] items; // length 3

    [Header("Slots (assign 3 world-space Transforms, left to right)")]
    public Transform[] slots; // length 3

    [Header("Drag Settings")]
    [Tooltip("Z offset so dragged item renders above others")]
    public float dragSortingOffset = 1f;

    [Header("Startup Animation")]
    public Animator startupAnimator;
    public string startupStateName = "Startup";

    [Header("Loop Animation")]
    public Animator loopAnimator;
    public string loopStateName = "Loop";

    [Header("Confetti Animation")]
    public Animator confettiAnimator;
    public string confettiStateName = "Confetti";

    [Header("Congrats Prefab")]
    public GameObject congratsPrefab;
    public Transform buttonParent;
    public Vector3 congratsPosition = Vector3.zero;
    public Vector3 congratsScale = Vector3.one;

    // ─── Private State ────────────────────────────────────────────────────────

    private Camera mainCamera;
    private OrderItem draggedItem;
    private Vector3 dragOffset;
    private int draggedOriginalSlot;
    private bool gameComplete;

    // ─── Unity ───────────────────────────────────────────────────────────────

    void Start()
    {
        mainCamera = Camera.main;

        if (startupAnimator) startupAnimator.gameObject.SetActive(false);
        if (loopAnimator)    loopAnimator.gameObject.SetActive(false);
        if (confettiAnimator) confettiAnimator.gameObject.SetActive(false);

        // Assign starting slot indices based on initial world positions
        for (int i = 0; i < items.Length; i++)
            items[i].currentSlotIndex = i;
    }

    void Update()
    {
        if (gameComplete) return;

        HandleTouch();
#if UNITY_EDITOR
        HandleMouse();
#endif
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    void HandleTouch()
    {
        if (Touchscreen.current == null) return;

        if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TryBeginDrag(ScreenToWorld(Touchscreen.current.primaryTouch.position.ReadValue()));

        if (Touchscreen.current.primaryTouch.press.isPressed && draggedItem != null)
            ContinueDrag(ScreenToWorld(Touchscreen.current.primaryTouch.position.ReadValue()));

        if (Touchscreen.current.primaryTouch.press.wasReleasedThisFrame && draggedItem != null)
            EndDrag();
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryBeginDrag(ScreenToWorld(Mouse.current.position.ReadValue()));

        if (Mouse.current.leftButton.isPressed && draggedItem != null)
            ContinueDrag(ScreenToWorld(Mouse.current.position.ReadValue()));

        if (Mouse.current.leftButton.wasReleasedThisFrame && draggedItem != null)
            EndDrag();
    }
#endif

    // ─── Drag Logic ───────────────────────────────────────────────────────────

    void TryBeginDrag(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        foreach (var item in items)
        {
            if (item.isLocked) continue;
            if (item.renderer == null) continue;
            if (hit.gameObject != item.renderer.gameObject) continue;

            draggedItem = item;
            draggedOriginalSlot = item.currentSlotIndex;
            dragOffset = item.renderer.transform.position - worldPos;

            // Lift above others visually
            var pos = item.renderer.transform.position;
            pos.z -= dragSortingOffset;
            item.renderer.transform.position = pos;
            break;
        }
    }

    void ContinueDrag(Vector3 worldPos)
    {
        var pos = worldPos + dragOffset;
        pos.z = draggedItem.renderer.transform.position.z;
        draggedItem.renderer.transform.position = pos;
    }

    void EndDrag()
    {
        int fromSlot = draggedOriginalSlot;
        int toSlot = NearestSlot(draggedItem.renderer.transform.position);

        if (fromSlot != toSlot)
        {
            // Shift all items between fromSlot and toSlot one step toward fromSlot
            int direction = (toSlot > fromSlot) ? 1 : -1;
            for (int slot = fromSlot; slot != toSlot; slot += direction)
            {
                OrderItem shifter = ItemInSlot(slot + direction);
                if (shifter != null && shifter != draggedItem && !shifter.isLocked)
                {
                    shifter.currentSlotIndex = slot;
                    SnapToSlot(shifter, slot);
                }
            }
        }

        // Place dragged item in destination slot
        draggedItem.currentSlotIndex = toSlot;
        SnapToSlot(draggedItem, toSlot);

        // Reset z
        var pos = draggedItem.renderer.transform.position;
        pos.z += dragSortingOffset;
        draggedItem.renderer.transform.position = pos;

        draggedItem = null;

        // Lock any newly correct items
        CheckAndLockCorrectItems();

        // Check overall completion
        if (AllLocked())
        {
            gameComplete = true;
            StartCoroutine(SuccessSequence());
        }
    }

    // ─── Slot Helpers ─────────────────────────────────────────────────────────

    int NearestSlot(Vector3 worldPos)
    {
        int nearest = 0;
        float best = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            float dist = Vector2.Distance(worldPos, slots[i].position);
            if (dist < best) { best = dist; nearest = i; }
        }
        return nearest;
    }

    OrderItem ItemInSlot(int slotIndex)
    {
        foreach (var item in items)
            if (item.currentSlotIndex == slotIndex) return item;
        return null;
    }

    void SnapToSlot(OrderItem item, int slotIndex)
    {
        var pos = slots[slotIndex].position;
        pos.z = item.renderer.transform.position.z;
        item.renderer.transform.position = pos;
    }

    void CheckAndLockCorrectItems()
    {
        foreach (var item in items)
        {
            if (!item.isLocked && item.currentSlotIndex == item.correctSlotIndex)
                item.isLocked = true;
        }
    }

    bool AllLocked()
    {
        foreach (var item in items)
            if (!item.isLocked) return false;
        return true;
    }

    // ─── Success Sequence ─────────────────────────────────────────────────────

    IEnumerator SuccessSequence()
    {
        MiniGameProgress.SetCompleted(GrabbableType.Porcupine);

        // Startup animation
        if (startupAnimator != null)
        {
            startupAnimator.gameObject.SetActive(true);
            startupAnimator.Play(startupStateName);

            yield return null;
            float clipLength = startupAnimator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(clipLength);
        }

        // Loop animation
        if (loopAnimator != null)
        {
            if (startupAnimator) startupAnimator.gameObject.SetActive(false);
            loopAnimator.gameObject.SetActive(true);
            loopAnimator.Play(loopStateName);
        }

        // Confetti
        if (confettiAnimator != null)
        {
            confettiAnimator.gameObject.SetActive(true);
            confettiAnimator.Play(confettiStateName);
        }

        // Congrats prefab
        SpawnCongrats();
    }

    void SpawnCongrats()
    {
        if (congratsPrefab == null || buttonParent == null) return;

        GameObject spawned = Instantiate(congratsPrefab, buttonParent);
        if (spawned.TryGetComponent(out RectTransform rt))
        {
            rt.localPosition = congratsPosition;
            rt.localRotation = Quaternion.identity;
            rt.localScale = congratsScale;
        }

        if (spawned.TryGetComponent(out Button btn) && spawned.TryGetComponent(out SceneLoader loader))
            btn.onClick.AddListener(loader.OnPlayPressed);
    }

    // ─── Utility ──────────────────────────────────────────────────────────────

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
        world.z = 0;
        return world;
    }
}
