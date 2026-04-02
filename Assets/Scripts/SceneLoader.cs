using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public GrabbableType grabbableType;
    public Scene scene;
        public void OnPlayPressed()
    {
            MiniGameProgress.SetCompleted(grabbableType);
        Debug.Log("COMPLETED: " + grabbableType);
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName("_02MiniGameSelect"));
    }

}
        