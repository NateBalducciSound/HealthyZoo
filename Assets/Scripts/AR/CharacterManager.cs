using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterMapping
    {
        public string imageName; // MUST match the exact name in your Reference Image Library
        public GameObject modelObject;
    }

    [Tooltip("Map your 6 Reference Image names to your 6 3D Models here.")]
    public CharacterMapping[] characters;

    // Turns on the correct model, turns off the rest
    public void ActivateCharacter(string targetImageName)
    {
        foreach (var character in characters)
        {
            if (character.modelObject == null) continue;

            // Check for a match (ignoring case and accidental spaces)
            bool isMatch = string.Equals(character.imageName.Trim(), targetImageName.Trim(), StringComparison.OrdinalIgnoreCase);
            
            if (isMatch)
            {
                if (!character.modelObject.activeSelf)
                {
                    character.modelObject.SetActive(true);
                    
                    // Trigger spawn animation if you have one
                    var anim = character.modelObject.GetComponent<Animator>();
                    if (anim != null) anim.SetTrigger("Spawn");
                }
            }
            else
            {
                // Ensure non-matching models are hidden
                character.modelObject.SetActive(false);
            }
        }
    }

    // New method: Clears the screen when the camera looks away
    public void HideAll()
    {
        foreach (var character in characters)
        {
            if (character.modelObject != null)
            {
                character.modelObject.SetActive(false);
            }
        }
    }
}