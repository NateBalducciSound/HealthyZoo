using UnityEngine;
using UnityEngine.EventSystems;

// Attach to the stethoscope Image on the canvas.
// Requires a CanvasGroup on the same GameObject (add one if missing).
[RequireComponent(typeof(CanvasGroup))]
public class UIDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Dialogue")]
    public DialogueController dialogueController;

    private RectTransform _rect;
    private Canvas _canvas;
    private CanvasGroup _cg;
    private Vector2 _startAnchoredPos;
    private bool _hasNotifiedDialogue = false;

    public bool IsDragging { get; private set; }

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _startAnchoredPos = _rect.anchoredPosition;

    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (IsDialogueLocked()) { e.pointerDrag = null; return; }

        IsDragging = true;
        _cg.blocksRaycasts = false; // allow drop zone to receive pointer events

        if (!_hasNotifiedDialogue)
        {
            _hasNotifiedDialogue = true;
            dialogueController?.OnPlayerStarted();
        }
        else
        {
            dialogueController?.ResetNudgeTimer();
        }
    }

    public void OnDrag(PointerEventData e)
    {
        _rect.anchoredPosition += e.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        IsDragging = false;
        _cg.blocksRaycasts = true;
        ReturnToStart();
    }

    public void ReturnToStart()
    {
        _rect.anchoredPosition = _startAnchoredPos;
    }

    bool IsDialogueLocked() => dialogueController != null && dialogueController.IsInputLocked;
}
