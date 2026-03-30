using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class UniversalAdaptiveCanvas : MonoBehaviour
{
    [Header("Scaling Settings")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080, 1920);

    [Header("Background")]
    [Tooltip("Assign the Background UI Image from your Canvas hierarchy")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite phoneBackground;
    [SerializeField] private Sprite tabletBackground;

    [Header("Tablet UI Adjustments")]
    [Tooltip("Scale multiplier applied to this transform's children on tablet. 1 = no change.")]
    [SerializeField] private float tabletUIScale = 1.25f;
    [Tooltip("Assign any RectTransforms that should be scaled up on tablet (buttons, panels, etc.)")]
    [SerializeField] private RectTransform[] tabletScaleTargets;

    private CanvasScaler _scaler;

    void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    void Apply()
    {
        float aspect = (float)Screen.width / Screen.height;
        bool isTablet = aspect > 0.65f;

        ApplyCanvasScaling(isTablet);
        ApplyBackground(isTablet);
        ApplyTabletUIScale(isTablet);
    }

    void ApplyCanvasScaling(bool isTablet)
    {
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler.referenceResolution = referenceResolution;

        if (isTablet)
        {
            // Match height on tablet: canvas is always exactly 1920 units tall so
            // nothing near the top/bottom gets cut off. The canvas will be wider
            // than 1080 units but that's fine — the background Image stretches to
            // fill it, and center-anchored content stays centered.
            _scaler.matchWidthOrHeight = 1f;
        }
        else
        {
            // Match 0.5 on phones as user confirmed this worked
            _scaler.matchWidthOrHeight = 0.5f;
        }
    }

    void ApplyBackground(bool isTablet)
    {
        if (backgroundImage == null) return;

        // Swap sprite if a tablet-specific one is assigned, otherwise keep phone bg
        if (isTablet && tabletBackground != null)
            backgroundImage.sprite = tabletBackground;
        else if (phoneBackground != null)
            backgroundImage.sprite = phoneBackground;
    }

    void ApplyTabletUIScale(bool isTablet)
    {
        if (!isTablet || tabletScaleTargets == null) return;

        foreach (RectTransform rt in tabletScaleTargets)
        {
            if (rt == null) continue;
            rt.localScale = new Vector3(tabletUIScale, tabletUIScale, 1f);
        }
    }

#if UNITY_EDITOR
    void Update() => Apply();
#endif
}
