using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem;

// Attach to the home button RectTransform in the AR scene.
// Detects touch directly via Input System, bypassing EventSystem/XR conflicts.
public class ARHomeButton : MonoBehaviour
{
    private RectTransform _rect;
    private Canvas _canvas;
    private bool _loading;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (_loading) return;

        Vector2 touchPos;
        bool tapped = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            tapped = true;
        }
#if UNITY_EDITOR
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            touchPos = Mouse.current.position.ReadValue();
            tapped = true;
        }
#endif
        else return;

        if (tapped && RectTransformUtility.RectangleContainsScreenPoint(_rect, touchPos, null))
        {
            _loading = true;
            Debug.Log("[ARHomeButton] Tapped — going home");
            StartCoroutine(GoHomeRoutine());
        }
    }

    IEnumerator GoHomeRoutine()
    {
        var arSession = FindObjectOfType<ARSession>();
        if (arSession != null)
        {
            arSession.enabled = false;
            yield return null;
        }

        SceneManager.LoadScene("_01MainMenu");
    }
}
