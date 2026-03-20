using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

public class ARImageTracker : MonoBehaviour
{
    //tracking image ref files and display animations for AR
    private ARTrackedImageManager trackedImageManager;

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnChanged;
    }

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // When a new image is detected
        foreach (var newImage in eventArgs.added)
        {
            UpdateImage(newImage);
        }

        // When an image stays in view
        foreach (var updatedImage in eventArgs.updated)
        {
            UpdateImage(updatedImage);
        }
    }
    //instance
    void UpdateImage(ARTrackedImage trackedImage)
    {
        // Get the Master Prefab instance that was just spawned
        GameObject guiObject = trackedImage.transform.GetChild(0).gameObject;
        CharacterManager manager = guiObject.GetComponent<CharacterManager>();

        if (manager != null)
        {
            // Tell the manager to show the model matching the image name
            manager.ActivateCharacter(trackedImage.referenceImage.name);
        }
    }
}