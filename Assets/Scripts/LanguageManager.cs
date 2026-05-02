using UnityEngine;

public static class LanguageManager
{
    private const string LangKey     = "Language";
    private const string CaptionsKey = "ShowCaptions";

    public enum Language { English = 0, Spanish = 1 }

    public static Language CurrentLanguage
    {
        get => (Language)PlayerPrefs.GetInt(LangKey, (int)Language.English);
        set { PlayerPrefs.SetInt(LangKey, (int)value); PlayerPrefs.Save(); }
    }

    public static bool ShowCaptions
    {
        get => PlayerPrefs.GetInt(CaptionsKey, 0) == 1;
        set { PlayerPrefs.SetInt(CaptionsKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // Returns the Spanish clip if the language is Spanish and the clip exists, otherwise English.
    public static AudioClip Resolve(LocalizedClip clip)
    {
        if (CurrentLanguage == Language.Spanish && clip.spanish != null)
            return clip.spanish;
        return clip.english;
    }

    // Returns the caption string for the current language. Empty string if not available.
    public static string ResolveCaption(LocalizedCaption caption)
    {
        if (CurrentLanguage == Language.Spanish && !string.IsNullOrEmpty(caption.spanish))
            return caption.spanish;
        return caption.english ?? string.Empty;
    }
}
