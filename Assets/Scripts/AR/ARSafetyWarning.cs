using TMPro;
using UnityEngine;

/// <summary>
/// Displays a localized safety warning overlay when the AR scene loads.
/// The AR session runs normally behind the panel so the camera is ready
/// the moment the user dismisses it.
///
/// Setup:
///   1. Add this component to a GameObject in your AR scene.
///   2. Create a full-screen Canvas Panel (warningPanel) with three TMP_Text objects:
///        - titleText   (e.g. "Safety Warning")
///        - bodyText    (the bullet-point warning)
///        - buttonText  (the "I Understand" button label)
///      and a Button wired to OnAcknowledged().
///   3. Assign all references in the Inspector.
///   4. The panel must start ACTIVE in the scene so it shows immediately.
/// </summary>
public class ARSafetyWarning : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The full-screen warning panel. Must be active on scene load.")]
    public GameObject warningPanel;

    [Header("Text Fields")]
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public TMP_Text buttonText;

    [Tooltip("Changa font asset — assigned to all text fields when language is Spanish.")]
    public TMP_FontAsset spanishFont;

    [Tooltip("Font size for the body text when language is Spanish. 0 = keep default.")]
    public float spanishBodyFontSize = 0f;

    [Header("Blocked Until Acknowledged")]
    [Tooltip("GameObjects to keep disabled until the warning is dismissed (e.g. ARMusicPlayer). Do NOT put the AR session here — it must run in the background so the camera is ready.")]
    public GameObject[] extraObjectsToBlock;

    // ── Localized strings ─────────────────────────────────────────────────────

    const string TitleEN = "Safety Warning";
    const string TitleES = "Advertencia de Seguridad";

    const string BodyEN =
        "This app uses Augmented Reality (AR).\n" +
        "• <b>Be aware of your surroundings.</b> Watch for obstacles, stairs, and other hazards.\n" +
        "• <b>Do not use while moving.</b> Never walk, drive, or operate machinery during AR.\n" +
        "• <b>Children must be supervised.</b> This app should be used under adult supervision at all times.\n" +
        "• <b>Take breaks.</b> Stop if you feel discomfort, dizziness, or disorientation.";

    const string BodyES =
        "Esta aplicación utiliza Realidad Aumentada (RA).\n" +
        "• <b>Mantente consciente de tu entorno.</b> Ten cuidado con obstáculos, escaleras y otros peligros.\n" +
        "• <b>No uses la app mientras te mueves.</b> No camines, manejes ni operes maquinaria durante la RA.\n" +
        "• <b>Los niños deben estar supervisados.</b> Esta app debe usarse bajo la supervisión de un adulto en todo momento.\n" +
        "• <b>Toma descansos.</b> Detente si sientes malestar, mareos o desorientación.";

    const string ButtonEN = "I Understand";
    const string ButtonES = "Entendido";

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        ApplyLanguage();

        if (warningPanel != null)
            warningPanel.SetActive(true);

        foreach (var obj in extraObjectsToBlock)
            if (obj != null) obj.SetActive(false);
    }

    void ApplyLanguage()
    {
        bool spanish = LanguageManager.CurrentLanguage == LanguageManager.Language.Spanish;

        if (titleText  != null) titleText.text  = spanish ? TitleES  : TitleEN;
        if (bodyText   != null) bodyText.text   = spanish ? BodyES   : BodyEN;
        if (buttonText != null) buttonText.text = spanish ? ButtonES : ButtonEN;

        if (spanish && spanishFont != null)
        {
            if (titleText  != null) titleText.font  = spanishFont;
            if (bodyText   != null) bodyText.font   = spanishFont;
            if (buttonText != null) buttonText.font = spanishFont;
        }

        if (spanish && bodyText != null && spanishBodyFontSize > 0f)
            bodyText.fontSize = spanishBodyFontSize;
    }

    /// <summary>
    /// Wire this to the "I Understand" button's OnClick event in the Inspector.
    /// </summary>
    public void OnAcknowledged()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        foreach (var obj in extraObjectsToBlock)
            if (obj != null) obj.SetActive(true);
    }
}
