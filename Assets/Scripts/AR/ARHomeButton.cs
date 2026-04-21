using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

// Attach to the home/back button in the AR_Prototype scene.
// Disables the AR session before loading so ARCore releases the camera
// cleanly on Android (skipping this step can cause a hang or black screen).
public class ARHomeButton : MonoBehaviour
{
    public void GoHome()
    {
        StartCoroutine(GoHomeRoutine());
    }

    IEnumerator GoHomeRoutine()
    {
        // Disable the AR session — releases the ARCore camera on Android.
        var arSession = FindObjectOfType<ARSession>();
        if (arSession != null)
        {
            arSession.enabled = false;
            yield return null; // one frame for AR subsystem cleanup
        }

        SceneManager.LoadScene(0);
    }
}
