using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Menu de configuracoes: volume da musica, volume dos efeitos, mudo, resolucao e modo de tela.
/// Abre por cima de qualquer tela (menu principal ou pausa do jogo) num Canvas proprio.
/// Os valores ficam em GameSettings (PlayerPrefs) e valem na hora, sem botao de aplicar.
public class SettingsPanel : MonoBehaviour
{
    static SettingsPanel instance;
    static int closedFrame = -1;

    /// Aberto agora, ou fechado neste mesmo quadro (o Esc que fecha nao deve tambem pausar o jogo).
    public static bool IsOpenOrJustClosed => instance != null || closedFrame == Time.frameCount;

    static readonly Color Wood = new Color32(111, 68, 41, 255), Cream = new Color32(255, 239, 196, 255),
        TrackColor = new Color32(49, 25, 12, 255), FillColor = new Color32(232, 168, 56, 255),
        HandleColor = new Color32(255, 248, 215, 255);

    Slider musicSlider, sfxSlider;
    Text musicValue, sfxValue, muteLabel, resolutionLabel, modeLabel;
    List<Vector2Int> resolutions;
    int resolutionIndex;
    AudioSource testSource;
    AudioClip testClip;
    System.Action onClosed;
    bool building;

    public static void Open(System.Action onClosed = null)
    {
        if (instance != null) return;
        var go = new GameObject("Configurações", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var panel = go.AddComponent<SettingsPanel>();
        panel.onClosed = onClosed;
        instance = panel;
        panel.Build();
    }

    public static void Close() { if (instance != null) instance.CloseSelf(); }

    void CloseSelf()
    {
        closedFrame = Time.frameCount;
        instance = null;
        onClosed?.Invoke();
        Destroy(gameObject);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseSelf();
    }

    // ------------------------------------------------------------------ montagem

    void Build()
    {
        building = true;
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        testSource = gameObject.AddComponent<AudioSource>();
        testSource.playOnAwake = false; testSource.spatialBlend = 0f; testSource.ignoreListenerPause = true;
        testClip = MakeTone();

        var dim = NewRect(transform, "Fundo", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        dim.color = new Color(.06f, .035f, .015f, .8f);

        var frame = NewRect(transform, "Moldura", new Vector2(.25f, .08f), new Vector2(.75f, .92f)).gameObject.AddComponent<Image>();
        frame.sprite = Resources.Load<Sprite>("hud/moldura_pausa");
        frame.color = frame.sprite != null ? Color.white : Wood;
        frame.preserveAspect = false;
        frame.raycastTarget = true;

        MakeText(frame.transform, "CONFIGURAÇÕES", .13f, .87f, .86f, .95f, 46);

        musicSlider = AddSliderRow(frame.transform, "Música", .76f, GameSettings.MusicVolume, out musicValue,
            v => GameSettings.MusicVolume = v);
        sfxSlider = AddSliderRow(frame.transform, "Efeitos", .60f, GameSettings.SfxVolume, out sfxValue,
            v => GameSettings.SfxVolume = v);

        muteLabel = AddButton(frame.transform, "", .30f, .70f, .385f, .475f, ToggleMute);

        resolutions = GameSettings.AvailableResolutions();
        resolutionIndex = Mathf.Max(0, resolutions.IndexOf(GameSettings.CurrentResolution()));
        AddButton(frame.transform, "<", .13f, .21f, .27f, .355f, () => StepResolution(-1));
        AddButton(frame.transform, ">", .79f, .87f, .27f, .355f, () => StepResolution(1));
        resolutionLabel = MakeText(frame.transform, "", .22f, .78f, .27f, .355f, 36);
        modeLabel = AddButton(frame.transform, "", .30f, .70f, .165f, .25f, ToggleMode);

        AddButton(frame.transform, "Voltar", .36f, .64f, .045f, .14f, CloseSelf);

        building = false;
        Refresh();
    }

    Slider AddSliderRow(Transform parent, string title, float top, float value, out Text valueText, UnityEngine.Events.UnityAction<float> onChange)
    {
        MakeText(parent, title, .12f, .40f, top - .02f, top + .045f, 34).alignment = TextAnchor.MiddleLeft;
        valueText = MakeText(parent, "", .78f, .88f, top - .06f, top + .0f, 32);

        var root = NewRect(parent, "Slider " + title, new Vector2(.14f, top - .105f), new Vector2(.76f, top - .03f));
        var track = NewRect(root, "Trilho", new Vector2(0, .32f), new Vector2(1, .68f)).gameObject.AddComponent<Image>();
        track.color = TrackColor;
        var fillArea = NewRect(root, "Area do preenchimento", new Vector2(0, .32f), new Vector2(1, .68f));
        var fill = NewRect(fillArea, "Preenchimento", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        fill.color = FillColor;
        var handleArea = NewRect(root, "Area do cursor", Vector2.zero, Vector2.one);
        var handle = NewRect(handleArea, "Cursor", Vector2.zero, new Vector2(0, 1)).gameObject.AddComponent<Image>();
        handle.color = HandleColor;
        handle.rectTransform.sizeDelta = new Vector2(34, 0);

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = true;
        slider.minValue = 0; slider.maxValue = 100;
        slider.SetValueWithoutNotify(Mathf.Round(value * 100f));
        slider.onValueChanged.AddListener(v => { if (!building) { onChange(v / 100f); Refresh(); } });
        root.gameObject.AddComponent<SliderRelease>().onRelease = PlayTest;
        return slider;
    }

    Text AddButton(Transform parent, string label, float x0, float x1, float y0, float y1, UnityEngine.Events.UnityAction action)
    {
        var rect = NewRect(parent, "Botão " + label, new Vector2(x0, y0), new Vector2(x1, y1));
        var image = rect.gameObject.AddComponent<Image>();
        Sprite normal = Resources.Load<Sprite>("hud/botao_normal"), selected = Resources.Load<Sprite>("hud/botao_selecionado");
        image.sprite = normal; image.color = normal != null ? Color.white : Wood;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (normal != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = selected, selectedSprite = selected, pressedSprite = selected };
        }
        button.onClick.AddListener(() => { PlayTest(); action(); });
        Text caption = MakeText(rect, label, .06f, .94f, .12f, .88f, 32);
        caption.resizeTextForBestFit = true; caption.resizeTextMinSize = 20; caption.resizeTextMaxSize = 34;
        return caption;
    }

    // ------------------------------------------------------------------ acoes

    void ToggleMute() { GameSettings.Muted = !GameSettings.Muted; Refresh(); }

    void ToggleMode() { GameSettings.SetFullScreen(!GameSettings.FullScreen); Refresh(); }

    void StepResolution(int step)
    {
        resolutionIndex = (resolutionIndex + step + resolutions.Count) % resolutions.Count;
        GameSettings.SetResolution(resolutions[resolutionIndex]);
        Refresh();
    }

    void Refresh()
    {
        musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
        sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
        muteLabel.text = GameSettings.Muted ? "Som: mudo" : "Som: ligado";
        Vector2Int r = resolutions[resolutionIndex];
        resolutionLabel.text = r.x + " x " + r.y;
        modeLabel.text = GameSettings.FullScreen ? "Tela cheia" : "Janela";
    }

    /// Som de teste dos efeitos: ao soltar o slider e nos botoes.
    void PlayTest()
    {
        if (testSource != null && testClip != null) testSource.PlayOneShot(testClip, GameSettings.Sfx);
    }

    // ------------------------------------------------------------------ utilitarios

    static RectTransform NewRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    static Text MakeText(Transform parent, string value, float x0, float x1, float y0, float y1, int size)
    {
        var t = NewRect(parent, "Texto " + value, new Vector2(x0, y0), new Vector2(x1, y1)).gameObject.AddComponent<Text>();
        t.text = value; UiTypography.Apply(t, size);
        t.color = Cream; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(49, 25, 12, 240); shadow.effectDistance = new Vector2(1, -2);
        return t;
    }

    static AudioClip MakeTone()
    {
        const int rate = 44100; const float duration = .12f;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, progress = t / duration;
            samples[i] = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 780f, progress) * t) * Mathf.Sin(Mathf.PI * progress) * .5f;
        }
        var clip = AudioClip.Create("Teste de volume", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    sealed class SliderRelease : MonoBehaviour, IPointerUpHandler
    {
        public System.Action onRelease;
        public void OnPointerUp(PointerEventData eventData) => onRelease?.Invoke();
    }
}
