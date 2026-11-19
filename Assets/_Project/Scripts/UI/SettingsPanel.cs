using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Painel unico de configuracoes, usado tanto pelo menu principal quanto pela pausa.
/// Audio e pre-visualizado; video so e aplicado ao confirmar em Aplicar.
public class SettingsPanel : MonoBehaviour
{
    const float VideoConfirmSeconds = 15f;
    static SettingsPanel instance;
    static int closedFrame = -1;

    static readonly Color32 Wood = new Color32(111, 55, 25, 255);
    static readonly Color32 DarkWood = new Color32(56, 27, 13, 255);
    static readonly Color32 Cream = new Color32(255, 239, 196, 255);
    static readonly Color32 Gold = new Color32(238, 166, 48, 255);

    Slider musicSlider, effectsSlider;
    TMP_Text musicValue, effectsValue, resolutionCaption;
    Image musicIcon, effectsIcon, fullscreenImage, windowedImage, applyImage;
    Button applyButton;
    Sprite fullscreenNormal, fullscreenSelected, windowedNormal, windowedSelected;
    TMP_Dropdown resolutionDropdown;
    List<Vector2Int> resolutions;
    GameSettings.Snapshot committed;
    Vector2Int pendingResolution;
    bool pendingFullscreen;
    AudioSource testSource;
    AudioClip testClip, hoverClip, clickClip;
    RectTransform backRect;
    System.Action onClosed;
    CanvasGroup blocked;
    CanvasGroup frameGroup;
    GameObject confirmation;
    TMP_Text confirmationTimer;
    float confirmationRemaining;
    bool closed;
    int altEnterFrame = -10;

    public static bool IsOpenOrJustClosed => instance != null || closedFrame == Time.frameCount;

    public static void Open(GameObject below = null, System.Action onClosed = null)
    {
        if (instance != null) return;
        var go = new GameObject("Configurações", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var panel = go.AddComponent<SettingsPanel>();
        panel.onClosed = onClosed;
        if (below != null)
        {
            panel.blocked = below.GetComponent<CanvasGroup>();
            if (panel.blocked == null) panel.blocked = below.AddComponent<CanvasGroup>();
            panel.blocked.interactable = false;
            panel.blocked.blocksRaycasts = false;
        }
        instance = panel;
        panel.Build();
    }

    public static void Close() { if (instance != null) instance.CloseSelf(); }

    void CloseSelf()
    {
        if (closed) return;
        closed = true;
        closedFrame = Time.frameCount;
        instance = null;
        if (confirmation != null) GameSettings.Restore(committed);
        GameSettings.PersistAudio();
        if (blocked != null) { blocked.interactable = true; blocked.blocksRaycasts = true; }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        onClosed?.Invoke();
        GetComponent<Canvas>().enabled = false;
        Destroy(gameObject, testClip != null ? testClip.length + .1f : 0f);
    }

    void OnDestroy() { if (instance == this) instance = null; }

    void Update()
    {
        if (closed) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.altKey.isPressed &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            altEnterFrame = Time.frameCount;
        if (confirmation == null && altEnterFrame > 0 && Time.frameCount >= altEnterFrame + 2)
        {
            altEnterFrame = -10;
            GameSettings.SyncFullScreen();
            committed.fullScreen = GameSettings.FullScreen;
            pendingFullscreen = committed.fullScreen;
            Refresh();
        }
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (confirmation != null) RevertVideo();
            else CloseSelf();
            return;
        }

        if (confirmation != null)
        {
            confirmationRemaining -= Time.unscaledDeltaTime;
            if (confirmationTimer != null)
                confirmationTimer.text = ConfirmationCountdownText(confirmationRemaining);
            if (confirmationRemaining <= 0f) RevertVideo();
        }
    }

    void Build()
    {
        committed = GameSettings.Capture();
        pendingResolution = GameSettings.CurrentResolution();
        pendingFullscreen = GameSettings.FullScreen;
        resolutions = GameSettings.AvailableResolutions();
        if (!resolutions.Contains(pendingResolution)) resolutions.Add(pendingResolution);
        resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

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
        testSource.playOnAwake = false;
        testSource.spatialBlend = 0f;
        testSource.ignoreListenerPause = true;
        testClip = MakeTone();
        hoverClip = MakeTone(760f, 1120f, .11f, .22f);
        clickClip = MakeTone(260f, 520f, .10f, .30f);

        var dim = NewRect(transform, "Fundo", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        dim.color = new Color(.035f, .018f, .008f, .78f);

        // Usa a arte propria do painel e a moldura de pausa como alternativa.
        var frame = NewRect(transform, "Moldura de configurações", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        frame.pivot = new Vector2(.5f, .5f);
        frame.sizeDelta = new Vector2(1080f, 800f);
        frame.anchoredPosition = Vector2.zero;
        var frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.sprite = Resources.Load<Sprite>("SettingsMenu/configuracoes_moldura_principal");
        if (frameImage.sprite == null) frameImage.sprite = Resources.Load<Sprite>("hud/moldura_pausa");
        frameImage.color = frameImage.sprite != null ? Color.white : Wood;
        frameImage.preserveAspect = true;
        frameImage.raycastTarget = true;
        frameGroup = frame.gameObject.AddComponent<CanvasGroup>();
        frame.anchoredPosition = new Vector2(0f, 32f);

        MakeSection(frame, "ÁUDIO", .70f, .77f);
        musicSlider = AddAudioRow(frame, "MÚSICA", .60f, committed.music, "icone_musica", "icone_musica_mutado", out musicIcon, out musicValue);
        effectsSlider = AddAudioRow(frame, "EFEITOS", .48f, committed.sfx, "icone_efeitos", "icone_efeitos_mutado", out effectsIcon, out effectsValue);

        MakeSection(frame, "VÍDEO", .37f, .43f);
        AddResolutionRow(frame);
        AddDisplayModeButtons(frame);
        AddApplyButton(frame);
        AddBackButton(frame);

        Refresh();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    Slider AddAudioRow(RectTransform parent, string title, float centerY, float value,
        string iconName, string mutedIconName, out Image icon, out TMP_Text percentage)
    {
        var iconRect = NewRect(parent, "Ícone " + title, new Vector2(.105f, centerY - .045f), new Vector2(.175f, centerY + .045f));
        icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var normal = Resources.Load<Sprite>("SettingsMenu/" + iconName);
        var muted = Resources.Load<Sprite>("SettingsMenu/" + mutedIconName);

        MakeText(parent, title, .19f, .36f, centerY - .035f, centerY + .035f, 26, FontStyles.Bold).alignment = TextAlignmentOptions.MidlineLeft;
        percentage = MakeText(parent, "", .82f, .92f, centerY - .035f, centerY + .035f, 27, FontStyles.Bold);

        var root = NewRect(parent, "Controle " + title, new Vector2(.37f, centerY - .035f), new Vector2(.80f, centerY + .035f));
        var track = NewRect(root, "Trilho", new Vector2(0, .34f), new Vector2(1, .66f)).gameObject.AddComponent<Image>();
        track.color = DarkWood;
        var fillArea = NewRect(root, "Área de preenchimento", new Vector2(.01f, .34f), new Vector2(.99f, .66f));
        var fill = NewRect(fillArea, "Preenchimento", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        fill.color = Gold;
        var handleArea = NewRect(root, "Área do cursor", Vector2.zero, Vector2.one);
        var handle = NewRect(handleArea, "Cursor", Vector2.zero, new Vector2(0, 1)).gameObject.AddComponent<Image>();
        handle.color = Cream;
        handle.rectTransform.sizeDelta = new Vector2(30, 0);

        var slider = root.gameObject.AddComponent<Slider>();
        AddHover(root.gameObject);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.wholeNumbers = true;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.SetValueWithoutNotify(Mathf.Round(value * 100f));
        slider.onValueChanged.AddListener(_ => PreviewAudio());
        root.gameObject.AddComponent<SliderRelease>().onRelease = () => { GameSettings.PersistAudio(); PlayTest(); };

        Image iconRef = icon;
        TMP_Text percentageRef = percentage;
        slider.onValueChanged.AddListener(amount => UpdateAudioIndicator(iconRef, normal, muted, percentageRef, amount));
        UpdateAudioIndicator(icon, normal, muted, percentage, slider.value);
        return slider;
    }

    static void UpdateAudioIndicator(Image icon, Sprite normal, Sprite muted, TMP_Text percentage, float amount)
    {
        if (icon != null) icon.sprite = amount <= 0f ? muted : normal;
        if (percentage != null) percentage.text = Mathf.RoundToInt(amount) + "%";
    }

    void PreviewAudio()
    {
        if (musicSlider == null || effectsSlider == null) return;
        GameSettings.PreviewVolumes(musicSlider.value / 100f, effectsSlider.value / 100f);
        committed.music = musicSlider.value / 100f;
        committed.sfx = effectsSlider.value / 100f;
        UpdateApplyButton();
    }

    void AddResolutionRow(RectTransform parent)
    {
        var icon = NewRect(parent, "Ícone resolução", new Vector2(.105f, .255f), new Vector2(.175f, .345f)).gameObject.AddComponent<Image>();
        icon.sprite = Resources.Load<Sprite>("SettingsMenu/icone_resolucao");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        MakeText(parent, "RESOLUÇÃO", .19f, .48f, .265f, .335f, 25, FontStyles.Bold).alignment = TextAlignmentOptions.MidlineLeft;

        var fieldRect = NewRect(parent, "Seletor de resolução", new Vector2(.53f, .233f), new Vector2(.91f, .35f));
        var fieldImage = fieldRect.gameObject.AddComponent<Image>();
        fieldImage.sprite = Resources.Load<Sprite>("SettingsMenu/resolucao_dropdown_fechado");
        fieldImage.type = Image.Type.Simple;
        fieldImage.color = fieldImage.sprite != null ? Color.white : Wood;
        fieldImage.preserveAspect = true;

        resolutionCaption = MakeText(fieldRect, "", .04f, .84f, .12f, .88f, 25, FontStyles.Bold);
        resolutionDropdown = fieldRect.gameObject.AddComponent<TMP_Dropdown>();
        resolutionDropdown.targetGraphic = fieldImage;
        AddHover(fieldRect.gameObject);
        var soundTrigger = fieldRect.gameObject.AddComponent<EventTrigger>();
        var soundEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        soundEntry.callback.AddListener(_ => PlayClick());
        soundTrigger.triggers.Add(soundEntry);
        resolutionDropdown.captionText = resolutionCaption;
        resolutionDropdown.options.Clear();
        foreach (var resolution in resolutions)
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData($"{resolution.x} x {resolution.y}"));
        resolutionDropdown.value = Mathf.Max(0, resolutions.IndexOf(pendingResolution));
        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            if (index >= 0 && index < resolutions.Count)
            {
                pendingResolution = resolutions[index];
                resolutionDropdown.RefreshShownValue();
                UpdateApplyButton();
                PlayClick();
            }
        });
        BuildResolutionTemplate();
        resolutionDropdown.RefreshShownValue();
    }

    void BuildResolutionTemplate()
    {
        // O template precisa ficar dentro do objeto TMP_Dropdown. Assim o componente
        // consegue clonar a lista e posicioná-la corretamente sobre o restante do painel.
        var templateRect = NewRect(resolutionDropdown.transform, "Lista de resoluções", Vector2.zero, Vector2.one);
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(.5f, 1f);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0f, 212f);
        templateRect.gameObject.SetActive(false);
        var panelImage = templateRect.gameObject.AddComponent<Image>();
        panelImage.sprite = Resources.Load<Sprite>("SettingsMenu/resolucao_dropdown_aberto");
        panelImage.type = Image.Type.Sliced;
        panelImage.pixelsPerUnitMultiplier = 10f;
        panelImage.color = panelImage.sprite != null ? Color.white : DarkWood;
        var scroll = templateRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 32f;

        var viewportRect = NewRect(templateRect, "Viewport", new Vector2(.035f, .035f), new Vector2(.90f, .965f));
        var viewportImage = viewportRect.gameObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        var mask = viewportRect.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scroll.viewport = viewportRect;

        var scrollbarRect = NewRect(templateRect, "Barra de rolagem", new Vector2(.92f, .045f), new Vector2(.97f, .955f));
        var scrollbarImage = scrollbarRect.gameObject.AddComponent<Image>();
        scrollbarImage.color = DarkWood;
        var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var handle = NewRect(scrollbarRect, "Alça", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        handle.color = Gold;
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.numberOfSteps = 0;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalScrollbarSpacing = -2f;

        var content = NewRect(viewportRect, "Content", new Vector2(0, 1), new Vector2(1, 1));
        content.pivot = new Vector2(.5f, 1f);
        // TMP_Dropdown calcula a altura do conteúdo e posiciona as opções.
        // O item precisa ter altura real antes da primeira abertura da lista.
        content.sizeDelta = new Vector2(0f, 46f);
        scroll.content = content;

        var item = NewRect(content, "Opção", new Vector2(0f, .5f), new Vector2(1f, .5f));
        item.sizeDelta = new Vector2(0f, 46f);
        var itemBackground = item.gameObject.AddComponent<Image>();
        itemBackground.color = new Color32(92, 42, 18, 255);
        itemBackground.type = Image.Type.Sliced;
        itemBackground.pixelsPerUnitMultiplier = 10f;
        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        toggle.transition = Selectable.Transition.None;
        var label = MakeText(item, "Option", .08f, .78f, .10f, .90f, 23, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var check = MakeText(item, "✓", .80f, .96f, .10f, .90f, 26, FontStyles.Bold);
        check.color = Gold;
        toggle.graphic = check;
        toggle.isOn = false;
        item.gameObject.AddComponent<SettingsDropdownOptionVisual>();

        resolutionDropdown.template = templateRect;
        resolutionDropdown.itemText = label;
    }

    void AddDisplayModeButtons(RectTransform parent)
    {
        MakeText(parent, "MODO DE EXIBIÇÃO", .19f, .49f, .125f, .19f, 23, FontStyles.Bold)
            .alignment = TextAlignmentOptions.MidlineLeft;

        var modeIcon = NewRect(parent, "Ícone modo de exibição", new Vector2(.105f, .113f), new Vector2(.175f, .203f)).gameObject.AddComponent<Image>();
        modeIcon.sprite = Resources.Load<Sprite>("SettingsMenu/icone_tela_cheia");
        modeIcon.preserveAspect = true;
        modeIcon.raycastTarget = false;

        fullscreenNormal = Resources.Load<Sprite>("SettingsMenu/modo_tela_cheia_normal");
        fullscreenSelected = Resources.Load<Sprite>("SettingsMenu/modo_tela_cheia_selecionado");
        windowedNormal = Resources.Load<Sprite>("SettingsMenu/modo_janela_normal");
        windowedSelected = Resources.Load<Sprite>("SettingsMenu/modo_janela_selecionado");

        var fullscreenButton = AddDisplayModeButton(parent, "Modo tela cheia", .53f, .715f, fullscreenNormal);
        fullscreenImage = fullscreenButton.targetGraphic as Image;
        fullscreenButton.onClick.AddListener(() => SetPendingDisplayMode(true));

        var windowedButton = AddDisplayModeButton(parent, "Modo janela", .725f, .91f, windowedNormal);
        windowedImage = windowedButton.targetGraphic as Image;
        windowedButton.onClick.AddListener(() => SetPendingDisplayMode(false));
    }

    Button AddDisplayModeButton(RectTransform parent, string name, float xMin, float xMax, Sprite normal)
    {
        var rect = NewRect(parent, name, new Vector2(xMin, .095f), new Vector2(xMax, .221f));
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = normal;
        image.preserveAspect = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color32(255, 225, 170, 255),
            selectedColor = new Color32(255, 225, 170, 255),
            pressedColor = new Color32(255, 210, 140, 255),
            disabledColor = Color.gray,
            colorMultiplier = 1f,
            fadeDuration = .08f
        };
        AddHover(rect.gameObject);
        return button;
    }

    void SetPendingDisplayMode(bool useFullscreen)
    {
        pendingFullscreen = useFullscreen;
        Refresh();
        PlayClick();
    }

    void AddApplyButton(RectTransform frame)
    {
        var rect = NewRect(frame, "Aplicar", new Vector2(.16f, 0f), new Vector2(.49f, 0f));
        rect.offsetMin = new Vector2(0f, -82f);
        rect.offsetMax = new Vector2(0f, -12f);
        applyImage = rect.gameObject.AddComponent<Image>();
        var normal = Resources.Load<Sprite>("SettingsMenu/botao_aplicar_normal");
        var selected = Resources.Load<Sprite>("SettingsMenu/botao_aplicar_selecionado");
        applyImage.sprite = normal;
        applyImage.preserveAspect = true;
        applyImage.type = Image.Type.Simple;
        applyImage.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = applyImage;
        AddHover(rect.gameObject);
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { highlightedSprite = selected, selectedSprite = selected, pressedSprite = selected };
        button.onClick.AddListener(Apply);
        applyButton = button;
    }

    void AddBackButton(RectTransform frame)
    {
        var rect = NewRect(frame, "Voltar", new Vector2(.51f, 0f), new Vector2(.84f, 0f));
        rect.offsetMin = new Vector2(0f, -82f);
        rect.offsetMax = new Vector2(0f, -12f);
        backRect = rect;
        var image = rect.gameObject.AddComponent<Image>();
        Sprite normal = Resources.Load<Sprite>("hud/voltar_normal");
        Sprite selected = Resources.Load<Sprite>("hud/voltar_selecionado");
        image.sprite = normal;
        image.color = normal != null ? Color.white : Wood;
        image.preserveAspect = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddHover(rect.gameObject);
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { highlightedSprite = selected, selectedSprite = selected, pressedSprite = selected };
        button.onClick.AddListener(() => { PlayClick(); CloseSelf(); });
    }

    void Apply()
    {
        if (confirmation != null || !HasPendingChanges()) return;
        PlayClick();
        GameSettings.PreviewVolumes(musicSlider.value / 100f, effectsSlider.value / 100f);
        bool videoChanged = HasPendingVideoChanges();
        if (videoChanged)
        {
            Vector2Int previewResolution = committed.resolution == Vector2Int.zero &&
                pendingResolution == GameSettings.CurrentResolution() ? Vector2Int.zero : pendingResolution;
            GameSettings.PreviewScreen(previewResolution, pendingFullscreen);
            ShowVideoConfirmation();
        }
        else
        {
            GameSettings.Persist();
            committed = GameSettings.Capture();
            Refresh();
        }
    }

    void ShowVideoConfirmation()
    {
        if (confirmation != null) return;
        frameGroup.interactable = false;
        frameGroup.blocksRaycasts = false;
        confirmationRemaining = VideoConfirmSeconds;
        confirmation = NewRect(transform, "Confirmar vídeo", Vector2.zero, Vector2.one).gameObject;
        var overlay = confirmation.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, .72f);
        overlay.raycastTarget = true;

        var modal = NewRect(confirmation.transform, "Painel de confirmação", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        modal.pivot = new Vector2(.5f, .5f);
        modal.sizeDelta = new Vector2(1180f, 760f);
        modal.anchoredPosition = Vector2.zero;
        var modalImage = modal.gameObject.AddComponent<Image>();
        modalImage.sprite = Resources.Load<Sprite>("SettingsMenu/Confirmacao/confirmacao_moldura_principal");
        if (modalImage.sprite == null) modalImage.sprite = Resources.Load<Sprite>("hud/moldura_pausa");
        modalImage.color = modalImage.sprite != null ? Color.white : Wood;
        modalImage.preserveAspect = true;
        modalImage.raycastTarget = true;

        MakeText(modal, "Manter esta configuração?", .10f, .90f, .49f, .66f, 38, FontStyles.Bold);
        confirmationTimer = MakeText(modal, ConfirmationCountdownText(VideoConfirmSeconds), .10f, .90f, .36f, .48f, 29, FontStyles.Normal);
        confirmationTimer.alignment = TextAlignmentOptions.Center;

        var keep = MakeModalButton(modal, "confirmacao_manter_normal", "confirmacao_manter_selecionado",
            .075f, .475f, .12f, .31f, ConfirmVideo);
        MakeModalButton(modal, "confirmacao_reverter_normal", "confirmacao_reverter_selecionado",
            .525f, .925f, .12f, .31f, RevertVideo);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(keep.gameObject);
    }

    static string ConfirmationCountdownText(float seconds)
    {
        int remaining = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"A tela será revertida em <color=#FFC41E><b>{remaining} s</b></color>";
    }

    Button MakeModalButton(RectTransform parent, string normalName, string selectedName,
        float x0, float x1, float y0, float y1, UnityEngine.Events.UnityAction action)
    {
        var rect = NewRect(parent, normalName, new Vector2(x0, y0), new Vector2(x1, y1));
        var image = rect.gameObject.AddComponent<Image>();
        var normal = Resources.Load<Sprite>("SettingsMenu/Confirmacao/" + normalName);
        var selected = Resources.Load<Sprite>("SettingsMenu/Confirmacao/" + selectedName);
        image.sprite = normal;
        image.preserveAspect = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddHover(rect.gameObject);
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { highlightedSprite = selected, selectedSprite = selected, pressedSprite = selected };
        button.onClick.AddListener(action);
        return button;
    }

    void ConfirmVideo()
    {
        if (confirmation == null) return;
        if (confirmation != null) Destroy(confirmation);
        confirmation = null;
        confirmationTimer = null;
        frameGroup.interactable = true;
        frameGroup.blocksRaycasts = true;
        altEnterFrame = -10;
        GameSettings.Persist();
        committed = GameSettings.Capture();
        pendingResolution = committed.resolution.x > 0 ? committed.resolution : GameSettings.CurrentResolution();
        pendingFullscreen = committed.fullScreen;
        Refresh();
        PlayClick();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resolutionDropdown != null ? resolutionDropdown.gameObject : null);
    }

    void RevertVideo()
    {
        if (confirmation == null) return;
        PlayClick();
        if (confirmation != null) Destroy(confirmation);
        confirmation = null;
        confirmationTimer = null;
        frameGroup.interactable = true;
        frameGroup.blocksRaycasts = true;
        altEnterFrame = -10;
        GameSettings.Restore(committed);
        pendingResolution = GameSettings.CurrentResolution();
        pendingFullscreen = committed.fullScreen;
        musicSlider.SetValueWithoutNotify(Mathf.Round(committed.music * 100f));
        effectsSlider.SetValueWithoutNotify(Mathf.Round(committed.sfx * 100f));
        resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, resolutions.IndexOf(pendingResolution)));
        resolutionDropdown.RefreshShownValue();
        Refresh();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resolutionDropdown != null ? resolutionDropdown.gameObject : null);
    }

    void Refresh()
    {
        if (resolutionCaption != null)
        {
            resolutionCaption.text = $"{pendingResolution.x} x {pendingResolution.y}";
            resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, resolutions.IndexOf(pendingResolution)));
            resolutionDropdown.RefreshShownValue();
        }
        if (fullscreenImage != null) fullscreenImage.sprite = pendingFullscreen ? fullscreenSelected : fullscreenNormal;
        if (windowedImage != null) windowedImage.sprite = pendingFullscreen ? windowedNormal : windowedSelected;
        UpdateApplyButton();
    }

    void UpdateApplyButton()
    {
        if (applyButton == null) return;
        bool showApply = HasPendingVideoChanges();
        applyButton.gameObject.SetActive(showApply);
        applyButton.interactable = showApply;
        if (backRect != null)
        {
            backRect.anchorMin = new Vector2(showApply ? .51f : .335f, 0f);
            backRect.anchorMax = new Vector2(showApply ? .84f : .665f, 0f);
        }
        applyImage.color = Color.white;
    }

    bool HasPendingChanges()
    {
        if (musicSlider == null || effectsSlider == null) return false;
        return Mathf.RoundToInt(musicSlider.value) != Mathf.RoundToInt(committed.music * 100f)
            || Mathf.RoundToInt(effectsSlider.value) != Mathf.RoundToInt(committed.sfx * 100f)
            || HasPendingVideoChanges();
    }

    bool HasPendingVideoChanges()
    {
        Vector2Int savedResolution = committed.resolution.x > 0 && committed.resolution.y > 0
            ? committed.resolution : GameSettings.CurrentResolution();
        return pendingResolution != savedResolution || pendingFullscreen != committed.fullScreen;
    }

    void MakeSection(RectTransform parent, string title, float y0, float y1)
    {
        MakeText(parent, title, .11f, .89f, y0, y1, 30, FontStyles.Bold);
        AddSeparator(parent, y0 - .012f);
    }

    void AddSeparator(RectTransform parent, float y)
    {
        var line = NewRect(parent, "Separador", new Vector2(.11f, y), new Vector2(.89f, y + .003f)).gameObject.AddComponent<Image>();
        line.color = new Color32(188, 112, 48, 210);
        line.raycastTarget = false;
    }

    void PlayTest()
    {
        if (testSource != null && testClip != null) testSource.PlayOneShot(testClip, GameSettings.Sfx);
    }

    void PlayClick()
    {
        if (testSource != null && clickClip != null) testSource.PlayOneShot(clickClip, GameSettings.Sfx);
    }

    void AddHover(GameObject target)
    {
        target.AddComponent<MenuButtonHoverSound>().Initialize(testSource, hoverClip);
    }

    static RectTransform NewRect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    static TMP_Text MakeText(Transform parent, string value, float x0, float x1, float y0, float y1, int size, FontStyles style)
    {
        var rect = NewRect(parent, "Texto " + value, new Vector2(x0, y0), new Vector2(x1, y1));
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.text = value;
        text.color = Cream;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(14, size - 8);
        text.fontSizeMax = size;
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(49, 25, 12, 235);
        shadow.effectDistance = new Vector2(1f, -2f);
        return text;
    }

    static AudioClip MakeTone(float from = 520f, float to = 780f, float duration = .12f, float volume = .5f)
    {
        const int rate = 44100;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float progress = t / duration;
            samples[i] = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(from, to, progress) * t) * Mathf.Sin(Mathf.PI * progress) * volume;
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
