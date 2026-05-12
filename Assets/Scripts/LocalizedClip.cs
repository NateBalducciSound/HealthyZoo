using System;
using UnityEngine;

[Serializable]
public struct LocalizedClip
{
    public AudioClip english;
    public AudioClip spanish;
}

[Serializable]
public struct LocalizedCaption
{
    [TextArea] public string english;
    [TextArea] public string spanish;
}
