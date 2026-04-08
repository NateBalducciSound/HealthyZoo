using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class MenuManager : MonoBehaviour
{
    void Start()
    {
        // Pre-warm MiniGameSelect while the player is on the main menu.
        // No other preloads run anywhere — this is the only one.
        AsyncSceneLoader.Preload(DeviceDetector.GetSceneName("_02MiniGameSelect"));
    }

    public void PlayGame()
    {
        AsyncSceneLoader.Load(DeviceDetector.GetSceneName("_02MiniGameSelect"));
    }

    public void ScannerLoad()
    {
        AsyncSceneLoader.Load("AR_Prototype");
    }
    
    public void LoadHome()
    {
        AsyncSceneLoader.Load(0);
    }

    public void QuitButton()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }

}
