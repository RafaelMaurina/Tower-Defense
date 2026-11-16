using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Estado visual: clique do mouse nao deixa o botao preso no sprite selecionado.
public class HudButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Image target;
    Button button;
    Sprite normal, selected, disabled;
    Image[] bars;
    Graphic[] graphics;
    AudioSource audioSource;
    AudioClip hoverClip;
    bool hovered, keyboardSelected, pressed;
    bool toggled;
    bool overlayFeedback;
    Image[] highlightTints, highlightRings;
    Image[] overlayBorders;

    public void ConfigureOverlay(Image image, params Image[] borders)
    {
        target = image; button = GetComponent<Button>();
        overlayFeedback = true; overlayBorders = borders; Refresh();
    }

    /// Realce em partes: brilho dourado nas partes (tints) e contorno (rings), so quando ativo.
    public void ConfigureHighlight(Image[] tints, Image[] rings)
    {
        button = GetComponent<Button>(); highlightTints = tints; highlightRings = rings; Refresh();
    }

    public void SetToggleState(bool active) { toggled = active; Refresh(); }

    public void Configure(Image image, Sprite idle, Sprite active)
    {
        target = image; button = GetComponent<Button>(); normal = idle; selected = active;
        Refresh();
    }
    public void SetSprites(Sprite idle, Sprite active, Sprite unavailable = null)
    {
        normal = idle; selected = active; disabled = unavailable;
        Refresh();
    }
    public void ConfigureBars(params Image[] images)
    {
        bars = images; button = GetComponent<Button>(); Refresh();
    }
    public void ConfigureGraphics(params Graphic[] elements)
    {
        graphics = elements; button = GetComponent<Button>(); Refresh();
    }
    public void ConfigureSound(AudioSource source, AudioClip clip)
    { audioSource = source; hoverClip = clip; }
    void OnEnable() { hovered = keyboardSelected = pressed = false; Refresh(); }
    void LateUpdate() => Refresh();
    void Refresh()
    {
        if (button == null) return;
        bool active = button.interactable && (hovered || keyboardSelected || toggled);
        if (highlightTints != null)
        {
            Color tint = active ? (pressed ? new Color(1f,.8f,.3f,.26f) : toggled ? new Color(1f,.86f,.45f,.20f) : new Color(1f,.92f,.6f,.12f)) : Color.clear;
            // torre escolhida: dourado (#FFD54A) pulsando de leve; so passando o mouse: dourado claro fixo
            float pulse = .78f + .22f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 6.2f));
            Color edge = toggled ? new Color(1f,.835f,.29f,pulse) : new Color(1f,.93f,.65f,.85f);
            foreach (var part in highlightTints) part.color = tint;
            foreach (var ring in highlightRings) { ring.enabled = active; ring.color = edge; }
            return;
        }
        if (overlayFeedback)
        {
            // dourado suave: brilho leve dentro e contorno claro; mais forte na torre escolhida
            target.color = active ? (pressed ? new Color(1f,.8f,.3f,.24f) : toggled ? new Color(1f,.86f,.45f,.18f) : new Color(1f,.92f,.6f,.10f)) : Color.clear;
            Color edge = toggled ? new Color(1f,.82f,.30f,1f) : new Color(1f,.93f,.65f,.85f);
            foreach (var border in overlayBorders) { border.enabled = active; border.color = edge; }
            return;
        }
        if (graphics != null)
        {
            Color color = !button.interactable ? Color.gray : active ? new Color32(255,205,76,255) : Color.white;
            if (pressed) color *= .85f;
            foreach (var graphic in graphics) graphic.color = color;
            return;
        }
        if (bars != null)
        {
            Color color = !button.interactable ? Color.gray : active ? new Color32(255,205,76,255) : new Color32(255,239,196,255);
            if (pressed) color *= .85f;
            foreach (var bar in bars) bar.color = color;
            return;
        }
        if (target == null) return;
        if (!button.interactable && disabled != null)
        {
            target.sprite = disabled;
            target.color = disabled == normal ? new Color(.6f, .6f, .6f, 1f) : Color.white;
            return;
        }
        target.sprite = active && selected != null ? selected : normal;
        target.color = !button.interactable ? new Color(.5f, .5f, .5f, 1f)
            : pressed ? new Color(.85f, .85f, .85f, 1f)
            : active && selected == normal ? new Color(.83f, 1f, .78f, 1f) : Color.white;
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; if (button != null && button.interactable && audioSource != null && hoverClip != null) audioSource.PlayOneShot(hoverClip); Refresh(); }
    public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { keyboardSelected = false; pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { keyboardSelected = !(e is PointerEventData); Refresh(); }
    public void OnDeselect(BaseEventData e) { keyboardSelected = false; Refresh(); }
}
