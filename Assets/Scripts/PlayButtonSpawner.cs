using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayButtonSpawner : MonoBehaviour
{
   public GameObject playButtonPrefab;
   public Transform buttonParent;

   private GrabbableType selectedGrabbable;
   private GameObject currentButton;

   public void setSelectedGrabbable (GrabbableType grabbable)
    {
        selectedGrabbable = grabbable;
        if(currentButton  == null)
        {
            currentButton = Instantiate (playButtonPrefab, buttonParent);
        }
    }
    public void OnPlayPressed ()
    {
        switch (selectedGrabbable)
        {
             case GrabbableType.Giraffe:
                SceneManager.LoadScene("GiraffeScene");
                break;

            case GrabbableType.Elephant:
                SceneManager.LoadScene("ElephantScene");
                break;

            case GrabbableType.Dog:
                SceneManager.LoadScene("DogScene");
                break;

            case GrabbableType.Lemur:
                SceneManager.LoadScene("LemurScene");
                break;
        }
    }
}
