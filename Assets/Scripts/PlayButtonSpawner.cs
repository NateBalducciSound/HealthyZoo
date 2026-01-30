using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

            Button btn = currentButton.GetComponent<Button>();
            btn.onClick.AddListener (OnPlayPressed);
        }
    }
    public void OnPlayPressed ()
    {
        switch (selectedGrabbable)
        {
             case GrabbableType.Giraffe:
                SceneManager.LoadScene("Scenes/_03AGiraffeScene");
                break;

            case GrabbableType.Elephant:
                SceneManager.LoadScene("Scenes/_03BElephantScene");
                break;

            case GrabbableType.Dog:
                SceneManager.LoadScene("_03DDogScene");
                break;

            case GrabbableType.Lemur:
                SceneManager.LoadScene("Scenes/_03CLemurScene");
                break;
        }
    }
}
