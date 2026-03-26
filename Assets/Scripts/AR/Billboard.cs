using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("[Billboard] No main camera found in scene. Searching for camera...");
            mainCameraTransform = FindFirstObjectByType<Camera>()?.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null)
        {
            // Try to find the camera again if it was missing at Start
            if (Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }
            else
            {
                return; // Can't billboard without a camera
            }
        }

        // Make the 2D sprite always look at the phone camera
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                         mainCameraTransform.rotation * Vector3.up);
    }
}