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
    //dev tool to remove type
    
    private const string AllCompleteShownKey = "AllCompleteShown";

    public static bool HasShownAllComplete()
    {
        return PlayerPrefs.GetInt(AllCompleteShownKey, 0) == 1;
    }

    public static void SetAllCompleteShown()
    {
        PlayerPrefs.SetInt(AllCompleteShownKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (GrabbableType type in System.Enum.GetValues(typeof(GrabbableType)))
        {
            PlayerPrefs.DeleteKey(type.ToString());
        }

        PlayerPrefs.DeleteKey(AllCompleteShownKey);
        PlayerPrefs.Save();
        Debug.Log("Mini-game progress RESET");
    }
}
