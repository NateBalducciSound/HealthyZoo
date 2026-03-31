using UnityEngine;
using System;
using System.Collections.Generic;

// Characters are instantiated on first scan and destroyed when hidden.
// Nothing is pre-loaded in the scene — assign Prefabs (not scene objects) in the Inspector.
public class CharacterManager : MonoBehaviour
{
    [Serializable]
    public struct CharacterMapping
    {
        public string imageName;
        public GameObject prefab;   // Assign prefab assets, NOT scene objects
    }

    [Tooltip("Map Reference Image names to character prefabs.")]
    public CharacterMapping[] characters;

    [Tooltip("Where to spawn the character (leave null to use this transform).")]
    public Transform spawnParent;

    private readonly Dictionary<string, GameObject> _instances = new();

    public void ActivateCharacter(string targetImageName, Vector3 spawnPosition)
    {
        foreach (var mapping in characters)
        {
            if (mapping.prefab == null) continue;

            bool isMatch = string.Equals(
                mapping.imageName.Trim(), targetImageName.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (isMatch)
            {
                if (!_instances.TryGetValue(mapping.imageName, out var obj) || obj == null)
                {
                    obj = Instantiate(mapping.prefab, spawnPosition, Quaternion.identity);
                    _instances[mapping.imageName] = obj;
                }

                if (!obj.activeSelf)
                    obj.SetActive(true);
            }
            else
            {
                if (_instances.TryGetValue(mapping.imageName, out var other) && other != null)
                {
                    Destroy(other);
                    _instances.Remove(mapping.imageName);
                }
            }
        }
    }

    public GameObject GetInstance(string imageName)
    {
        _instances.TryGetValue(imageName, out var obj);
        return obj;
    }

    public void HideAll()
    {
        foreach (var kvp in _instances)
            if (kvp.Value != null) Destroy(kvp.Value);
        _instances.Clear();
    }
}
