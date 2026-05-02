using UnityEngine;
using UnityEngine.UI;

// Attach to the settings panel.
//
// Inspector setup:
//   slotRects        — the 4 empty checkbox RectTransforms, in order:
//                      [0] English Voice  [1] English Captions
//                      [2] Spanish Voice  [3] Spanish Captions
//   slotButtons      — the Button on each of those same GameObjects (same order)
//   checkmark        — the single custom checkmark Image that moves between slots

public class LanguageSettingsPanel : MonoBehaviour
{
    [Header("Checkbox Slots (order: EN Voice, EN Captions, ES Voice, ES Captions)")]
    public RectTransform[] slotRects;
    public Button[] slotButtons;

    [Header("Checkmark")]
    public RectTransform checkmark;

    void OnEnable()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int captured = i;
            slotButtons[i].onClick.AddListener(() => OnSlotPressed(captured));
        }

        RefreshCheckmark();
    }

    void OnDisable()
    {
        foreach (var btn in slotButtons)
            btn.onClick.RemoveAllListeners();
    }

    void OnSlotPressed(int index)
    {
        switch (index)
        {
            case 0: Apply(LanguageManager.Language.English, false); break;
            case 1: Apply(LanguageManager.Language.English, true);  break;
            case 2: Apply(LanguageManager.Language.Spanish, false); break;
            case 3: Apply(LanguageManager.Language.Spanish, true);  break;
        }

        MoveCheckmark(index);
    }

    void Apply(LanguageManager.Language language, bool captions)
    {
        LanguageManager.CurrentLanguage = language;
        LanguageManager.ShowCaptions    = captions;
    }

    void RefreshCheckmark()
    {
        bool isSpanish  = LanguageManager.CurrentLanguage == LanguageManager.Language.Spanish;
        bool isCaptions = LanguageManager.ShowCaptions;

        int activeIndex = (isSpanish ? 2 : 0) + (isCaptions ? 1 : 0);
        MoveCheckmark(activeIndex);
    }

    void MoveCheckmark(int index)
    {
        if (checkmark == null || index >= slotRects.Length) return;
        checkmark.position = slotRects[index].position;
    }
}
