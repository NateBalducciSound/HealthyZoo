using UnityEngine;

public static class DeviceDetector
{
    // Cached so we only calculate once
    private static bool? _isTablet;

    public static bool IsTablet
    {
        get
        {
            if (_isTablet.HasValue) return _isTablet.Value;

            // iOS: check model string directly
            if (SystemInfo.deviceModel.Contains("iPad"))
            {
                _isTablet = true;
                return true;
            }

            // Android + fallback: estimate physical screen diagonal in inches
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float widthInches  = Screen.width  / dpi;
            float heightInches = Screen.height / dpi;
            float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

            // 7 inches diagonal is the conventional phone/tablet cutoff
            _isTablet = diagonal >= 7f;
            return _isTablet.Value;
        }
    }

    // Returns the scene name with "_Ipad" appended if on tablet
    public static string GetSceneName(string baseName)
    {
        return IsTablet ? baseName + "_Ipad" : baseName;
    }
}
