using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Estado visual: clique do mouse nao deixa o botao preso no sprite selecionado.
public class HudButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Image target;
    Button button;
    Sprite normal, selected;
    Image[] bars;
    AudioSource audioSource;
    AudioClip hoverClip;
    bool hovered, keyboardSelected, pressed;

    public void Configure(Image image, Sprite idle, Sprite active)
    {
        target = image; button = GetComponent<Button>(); normal = idle; selected = active;
        Refresh();
    }
    public void ConfigureBars(params Image[] images)
    {
        bars = images; button = GetComponent<Button>(); Refresh();
    }
    public void ConfigureSound(AudioSource source, AudioClip clip)
    { audioSource = source; hoverClip = clip; }
    void OnEnable() { hovered = keyboardSelected = pressed = false; Refresh(); }
    void LateUpdate() => Refresh();
    void Refresh()
    {
        if (button == null) return;
        bool active = button.interactable && (hovered || keyboardSelected);
        if (bars != null)
        {
            Color color = !button.interactable ? Color.gray : active ? new Color32(255,205,76,255) : new Color32(255,239,196,255);
            if (pressed) color *= .85f;
            foreach (var bar in bars) bar.color = color;
            return;
        }
        if (target == null) return;
        target.sprite = active && selected != null ? selected : normal;
        target.color = !button.interactable ? new Color(.5f, .5f, .5f, 1f) : pressed ? new Color(.85f, .85f, .85f, 1f) : Color.white;
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; if (button != null && button.interactable && audioSource != null && hoverClip != null) audioSource.PlayOneShot(hoverClip); Refresh(); }
    public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { keyboardSelected = false; pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { keyboardSelected = !(e is PointerEventData); Refresh(); }
    public void OnDeselect(BaseEventData e) { keyboardSelected = false; Refresh(); }
}
