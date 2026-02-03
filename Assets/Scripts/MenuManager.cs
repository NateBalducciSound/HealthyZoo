using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class MenuManager : MonoBehaviour
{
    public void PlayGame ()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void ScannerLoad(){ 
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
    // Add get component later for Number of objects to track CPU usage and make sure no memory leaks happen from
    // recurring animations
    Debug.Log("Scanner Running: " + "INSERT ITEM NUMBER HERE");
        
    
    }
    public void QuitButton()
    {   
        Debug.Log("QUIT");
        Application.Quit();
    }

}
