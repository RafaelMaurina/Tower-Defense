using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Fundo verde da resolução ativa, com realce em navegação e mouse.
public class SettingsDropdownOptionVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    static readonly Color NormalWood = new Color32(92, 42, 18, 255);
    static readonly Color HoverWood = new Color32(125, 68, 27, 255);
    static readonly Color SelectedGreen = new Color32(75, 116, 30, 255);

    Toggle toggle;
    Image background;
    bool hovered, focused;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        background = GetComponent<Image>();
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnDestroy()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { focused = !(eventData is PointerEventData); Refresh(); }
    public void OnDeselect(BaseEventData eventData) { focused = false; Refresh(); }

    void Refresh()
    {
        if (toggle == null || background == null) return;
        background.sprite = null;
        background.color = toggle.isOn ? SelectedGreen : hovered || focused ? HoverWood : NormalWood;
    }

    void OnToggleChanged(bool isOn) => Refresh();
}
