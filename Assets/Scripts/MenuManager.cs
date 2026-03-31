using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class MenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        AsyncSceneLoader.Load(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void ScannerLoad()
    {
        AsyncSceneLoader.Load("AR_Prototype");
    }
    
    public void LoadHome()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitButton()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }

}
