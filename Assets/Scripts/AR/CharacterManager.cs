using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterMapping
    {
        public string imageName; // Must match the name in your Reference Library
        public GameObject modelObject;
    }

    public CharacterMapping[] characters;

    public void ActivateCharacter(string name)
    {
        foreach (var character in characters)
        {
            // If the name matches, turn it on. Otherwise, turn it off.
            bool isActive = (character.imageName == name);
            character.modelObject.SetActive(isActive);
            
            if (isActive) 
            {
                // Trigger animation if it has an Animator
                var anim = character.modelObject.GetComponent<Animator>();
                if(anim != null) anim.SetTrigger("Spawn");
            }
        }
    }
}