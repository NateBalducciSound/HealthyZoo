using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class WorldSpaceSliderHandle : MonoBehaviour
{
    public Slider slider;
    public Transform bottomAnchor;
    public Transform topAnchor;
    private Camera cam;
    private Collider2D col;
    private bool dragging;

    void Awake()
    {
        cam = Camera.main;
        col = GetComponent<Collider2D>();

        if (slider != null)
            slider.interactable = false;
    }

    void Start()
    {
        if (bottomAnchor != null && topAnchor != null && slider != null)
        {
            float y = Mathf.Lerp(bottomAnchor.position.y, topAnchor.position.y, slider.value);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
    }

    void Update()
    {
        Vector2 screenPos;
        bool pressedThisFrame;
        bool isHeld;
        bool releasedThisFrame;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPos        = touch.position;
            pressedThisFrame = touch.phase == TouchPhase.Began;
            isHeld           = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            releasedThisFrame = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else
        {
            screenPos        = Input.mousePosition;
            pressedThisFrame = Input.GetMouseButtonDown(0);
            isHeld           = Input.GetMouseButton(0);
            releasedThisFrame = Input.GetMouseButtonUp(0);
        }

        if (releasedThisFrame) { dragging = false; return; }

        if (pressedThisFrame)
        {
            Vector2 world = ScreenToWorld(screenPos);
            dragging = col.OverlapPoint(world);
            return;
        }

        if (!dragging || !isHeld) return;

        float minY   = bottomAnchor.position.y;
        float maxY   = topAnchor.position.y;
        float minScr = cam.WorldToScreenPoint(bottomAnchor.position).y;
        float maxScr = cam.WorldToScreenPoint(topAnchor.position).y;

        float t    = Mathf.Clamp01(Mathf.InverseLerp(minScr, maxScr, screenPos.y));
        float newY = Mathf.Lerp(minY, maxY, t);

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        slider.value = t;

    }

    Vector2 ScreenToWorld(Vector2 screenPos)
    {
        float depth = Mathf.Abs(cam.transform.position.z);
        return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
    }
}
