using UnityEngine;

public static class MiniGameProgress
{   
    public static bool IsCompleted(GrabbableType type)
    {
        return PlayerPrefs.GetInt(type.ToString(), 0) == 1;
    }

    public static void SetCompleted(GrabbableType type)
    {
        PlayerPrefs.SetInt(type.ToString(), 1);
        PlayerPrefs.Save();
    }
}
