using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Make the 2D sprite always look at the phone camera
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                         mainCameraTransform.rotation * Vector3.up);
    }
}