using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PlayButtonSpawner : MonoBehaviour, IPointerClickHandler
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

            case GrabbableType.Heron:
                SceneManager.LoadScene("Scenes/_03BHeronScene");
                break;

            case GrabbableType.Porcupine:
                SceneManager.LoadScene("Scenes/_03CPorcupineScene");
                break;

            case GrabbableType.Sloth:
                SceneManager.LoadScene("Scenes/_03DSlothScene");
                break;
            case GrabbableType.Panda:
                SceneManager.LoadScene("Scenes/_03EPandaScene");
                break;
            case GrabbableType.Alligator:
                SceneManager.LoadScene("Scenes/_03FAlligatorScene");
                break;
                
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}
