using UnityEngine;
using UnityEngine.UI;

// Attach this to any Button GameObject to play the click sound automatically.
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(AudioManager.PlayClick);
    }
}
