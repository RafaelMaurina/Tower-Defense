using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

using UnityEngine.UI;

/// Interface de colheita: valores dinamicos, pausa, construcao e resumo da partida.
public class GameHUD : MonoBehaviour
{
    sealed class Action
    {
        public string text, art, icon, detail;
        public UnityEngine.Events.UnityAction callback;
        public int cost;
        public bool enabled;
        public Action(string text, string art, string icon, UnityEngine.Events.UnityAction callback, int cost = -1, string detail = null, bool enabled = true)
        { this.text = text; this.art = art; this.icon = icon; this.callback = callback; this.cost = cost; this.detail = detail; this.enabled = enabled; }
    }
    sealed class Purchase { public Button button; public int cost; }
    static GameHUD current;
    static Sprite rangeSprite;
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    readonly List<Sprite> generatedSprites = new List<Sprite>();
    readonly List<Purchase> purchases = new List<Purchase>();
    Text coins, life, waves, threat, heading, description;
    RectTransform rows, stats;
    BuildPlot selectedPlot;
    TowerData[] catalog;
    GameObject catalogView;
    GameObject overlay;
    Button settingsButton, buildPauseButton;
    Button startWaveButton, autoWaveButton, speedButton;
    Text startWaveText, autoWaveText, speedText;
    Image buildPauseIcon;
    WaveSpawner waveSpawner;
    SpriteRenderer rangeRenderer;
    BuildPlot hoverPlot;
    bool selectedRange, decisionOpen;
    Image panelImage;
    bool towerPanel;
    GameObject upgradeView;
    GameObject resultView;
    AudioSource uiAudio;
    AudioClip hoverSound, clickSound;


    Sprite Art(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite))
        {
            if (name.StartsWith("v2/")) sprite = NewHudArt(name.Substring(3));
            else if (name.StartsWith("prioridade_")) sprite = PriorityArt(name);
            else if (name == "moldura_atributos_nova") sprite = NewAttributesFrame();
            else sprite = Resources.Load<Sprite>("hud/" + ArtPath(name));
            sprites[name] = sprite;
        }
        return sprite;
    }
    Sprite PriorityArt(string name)
    {
        var source = Resources.Load<Sprite>("hud/melhorar_submenu/" + name);
        if (source == null) return null;
        // Ambos os estados compartilham o mesmo recorte para nao mudar de tamanho no hover.
        var crop = new Rect(0,80,2172,550);
        var sprite = Sprite.Create(source.texture,crop,new Vector2(.5f,.5f),source.pixelsPerUnit,
            0,SpriteMeshType.FullRect);
        sprite.name = name;
        generatedSprites.Add(sprite);
        return sprite;
    }
    Sprite NewAttributesFrame()
    {
        var source = Resources.Load<Sprite>("hud/melhorar_submenu/moldura_atributos_nova");
        if (source == null) return null;
        // Remove a margem transparente do PNG sem alterar a arte original.
        var sprite = Sprite.Create(source.texture,new Rect(0,214,2172,305),
            new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        sprite.name = "moldura_atributos_nova";
        generatedSprites.Add(sprite);
        return sprite;
    }
    static string ArtPath(string name)
    {
        if (name.StartsWith("final_")) return "fianis_submenu/" + name.Substring(6);
        if (name == "moldura_pausa_titulo" || name.StartsWith("continuar_") ||
            name.StartsWith("recomecar_") || name.StartsWith("menu_"))
            return "jogo_pausado_submenu/" + name;
        if (name.StartsWith("melhorar_") || name.StartsWith("vender_") ||
            name.StartsWith("prioridade_") || name == "moldura_melhorar" ||
            name.StartsWith("moldura_retrato_torre") || name.StartsWith("moldura_atributos") ||
            name == "icone_vender" || name == "icone_dano" ||
            name == "icone_alcance" || name == "icone_cadencia" || name == "icone_aereos")
            return "melhorar_submenu/" + name;
        if (name == "icone_auto" || name == "icone_configuracoes" ||
            name == "icone_continuar" || name == "icone_moedas" ||
            name == "icone_pausa" || name == "icone_rodadas" ||
            name == "icone_vida" || name == "status_frame" || name == "square_button")
            return "hud_superior/" + name;
        return name;
    }
    Sprite NewHudArt(string name)
    {
        var path = "hud/hud_superior/" + (name == "status_frame" ? "moldura_recursos_fazenda" :
            name == "action_button" ? "moldura_acoes_fazenda" :
            name == "square_button" ? "moldura_controle_fazenda" : "v2/" + name);
        var source = Resources.Load<Sprite>(path);
        if (source == null) return null;
        Rect crop;
        switch (name)
        {
            // Recortes das margens transparentes; preservam o celeiro e as folhas.
            case "status_frame": crop = new Rect(24, 101, 2125, 556); break;
            case "action_button": crop = new Rect(21, 136, 2112, 500); break;
            case "square_button": crop = new Rect(143, 142, 964, 964); break;
            case "icon_life": crop = new Rect(40, 54, 1180, 1100); break;
            case "icon_coins": crop = new Rect(100, 24, 1050, 1150); break;
            case "icon_rounds": crop = new Rect(300, 0, 650, 1254); break;
            case "icon_pause": crop = new Rect(300, 224, 654, 800); break;
            case "icon_play": crop = new Rect(180, 104, 900, 1000); break;
            case "icon_auto": crop = new Rect(180, 84, 1200, 820); break;
            default: crop = new Rect(0, 0, source.texture.width, source.texture.height); break;
        }
        var sprite = Sprite.Create(source.texture, crop, new Vector2(.5f, .5f), source.pixelsPerUnit,
            0, SpriteMeshType.FullRect);
        sprite.name = name;
        generatedSprites.Add(sprite);
        return sprite;
    }
    void Awake()
    {
        current = this;
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.ignoreListenerPause = true;
        hoverSound = UiTone("HUD Hover", .11f, 760f, 1120f, .22f);
        clickSound = UiTone("HUD Click", .10f, 260f, 520f, .30f);
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        CreateCanvas();
    }
    static AudioClip UiTone(string name, float duration, float from, float to, float volume)
    {
        const int rate = 44100;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, progress = t / duration;
            float frequency = Mathf.Lerp(from, to, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress) * Mathf.Exp(-progress * 1.3f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    void PlayHover() { if (uiAudio != null && hoverSound != null) uiAudio.PlayOneShot(hoverSound); }
    void PlayClick() { if (uiAudio != null && clickSound != null) uiAudio.PlayOneShot(clickSound); }
    void OnEnable()
    {
        GameManager.OnMoneyChanged += UpdateMoney;
        GameManager.OnBaseLifeChanged += UpdateLife;
        GameManager.OnWaveChanged += UpdateWave;
        GameManager.OnWaveCompositionChanged += UpdateComposition;
        GameManager.OnStateChanged += StateChanged;
        GameManager.OnGameEnded += Result;
    }
    public static bool IsOverlayOpen => current != null && current.overlay != null && current.overlay.activeSelf;
    void Start()
    {
        UpdateMoney(GameManager.Money); UpdateLife(GameManager.BaseLife);
        UpdateWave(GameManager.StateRound, 0);
        waveSpawner = FindAnyObjectByType<WaveSpawner>();
        UpdateWaveControls();
    }
    void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnBaseLifeChanged -= UpdateLife;
        GameManager.OnWaveChanged -= UpdateWave;
        GameManager.OnWaveCompositionChanged -= UpdateComposition;
        GameManager.OnStateChanged -= StateChanged;
        GameManager.OnGameEnded -= Result;
        EndDecisionPause();
        if (rangeRenderer != null) Destroy(rangeRenderer.gameObject);
        if (current == this) current = null;
    }
    void OnDestroy()
    {
        foreach (var sprite in generatedSprites)
            if (sprite != null) Destroy(sprite);
    }
    void Update()
    {
        UpdateWaveControls();
        if (Keyboard.current == null) return;
        if (overlay.activeSelf && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
        {
            foreach (Button option in overlay.GetComponentsInChildren<Button>())
                if (option.interactable) { EventSystem.current.SetSelectedGameObject(option.gameObject); break; }
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (towerPanel && overlay.activeSelf) Back();
        else if (GameManager.State == GameState.Victory || GameManager.State == GameState.Defeat) return;
        else GameManager.Instance?.TogglePause();
    }
    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    Image Picture(Transform parent, string name, string art, Vector2 min, Vector2 max, bool aspect = false)
    {
        var image = Rect(parent, name, min, max).gameObject.AddComponent<Image>();
        image.sprite = Art(art); image.preserveAspect = aspect; image.raycastTarget = false;
        image.color = image.sprite != null ? Color.white : new Color(.28f, .14f, .07f, 1);
        return image;
    }
    static Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size, bool withShadow = true)
    {
        var t = Rect(parent, text, min, max).gameObject.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = new Color32(255, 239, 196, 255);
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        if (withShadow)
        {
            var shadow = t.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color32(49, 25, 12, 240);
            shadow.effectDistance = new Vector2(1, -2);
        }
        return t;
    }
    Button MakeButton(Transform parent, Action action, Vector2 min, Vector2 max)
    {
        bool baked = action.art != "construir";
        var holder = Rect(parent, action.text, min, max);
        var image = Picture(holder, "Sprite", action.art + "_normal", new Vector2(0, string.IsNullOrEmpty(action.detail) ? 0 : .28f), Vector2.one, true);
        image.raycastTarget = true;
        var b = holder.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        b.transition = Selectable.Transition.None;
        var feedback = holder.gameObject.AddComponent<HudButtonFeedback>();
        feedback.Configure(image, Art(action.art + "_normal"), Art(action.art + "_selecionado"));
        feedback.ConfigureSound(uiAudio, hoverSound);
        if (!baked)
        {
            if (!string.IsNullOrEmpty(action.icon))
            {
                var iconImage = Picture(image.transform, action.icon, "icone_" + action.icon,
                    new Vector2(.06f,.23f), new Vector2(.20f,.77f), true);
                if (iconImage.sprite == null && action.icon == "auto")
                { iconImage.sprite = Art("icone_continuar"); iconImage.color = Color.white; }
            }
            var caption = Label(image.transform, action.text,
                new Vector2(string.IsNullOrEmpty(action.icon) ? .08f : .22f,.15f), new Vector2(.92f,.85f), 32);
            caption.resizeTextForBestFit = true; caption.resizeTextMinSize = 22; caption.resizeTextMaxSize = 32;
        }
        if (!string.IsNullOrEmpty(action.detail))
            Label(holder, action.detail, Vector2.zero, new Vector2(1,.27f), 26);
        b.interactable = action.enabled && (action.cost < 0 || GameManager.Money >= action.cost);
        b.onClick.AddListener(PlayClick);
        b.onClick.AddListener(action.callback);
        if (action.enabled && action.cost >= 0) purchases.Add(new Purchase { button = b, cost = action.cost });
        return b;
    }
    Button MakeTopWaveButton(Transform parent, string name, string art, string icon, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction callback)
    {
        var holder = Rect(parent, name, min, max);
        string plateArt = art.StartsWith("v2/") ? art : art + "_normal";
        var plate = Picture(holder, "Placa", plateArt, Vector2.zero, Vector2.one, true);
        plate.raycastTarget = true;
        var button = holder.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.None;
        if (!string.IsNullOrEmpty(icon))
            Picture(holder, "Ícone", icon.StartsWith("v2/") ? icon : "icone_" + icon,
                new Vector2(.16f,.24f), new Vector2(.27f,.67f), true);
        var caption = Label(holder, name,
            new Vector2(string.IsNullOrEmpty(icon) ? .16f : .29f,.23f), new Vector2(.90f,.68f), 32);
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 18;
        caption.resizeTextMaxSize = 32;
        var feedback = holder.gameObject.AddComponent<HudButtonFeedback>();
        feedback.Configure(plate, Art(plateArt),
            Art(art.StartsWith("v2/") ? art : art + "_selecionado"));
        feedback.ConfigureSound(uiAudio, hoverSound);
        button.onClick.AddListener(PlayClick);
        button.onClick.AddListener(callback);
        return button;
    }
    void CreateCanvas()
    {
        var root = new GameObject("HUD da Colheita", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        var resourceArea = Rect(root.transform, "Área dos recursos", new Vector2(.34f,.865f), new Vector2(.66f,.99f));
        var resources = Rect(resourceArea, "Recursos", Vector2.zero, Vector2.one);
        var resourceFit = resources.gameObject.AddComponent<AspectRatioFitter>();
        resourceFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        resourceFit.aspectRatio = 2125f / 556f;
        Picture(resources, "Moldura dos recursos", "v2/status_frame", Vector2.zero, Vector2.one);
        life = Counter(resources, "vida", .10f, .35f);
        coins = Counter(resources, "moedas", .38f, .62f);
        waves = Counter(resources, "rodadas", .66f, .90f);
        buildPauseButton = TopControl(root.transform, "Pausar construção", null, "pausa", "v2/square_button",
            new Vector2(.83f,.917f), new Vector2(.862f,.974f), () =>
                GameManager.Instance?.SetBuildPause(!GameManager.IsBuildPaused));
        buildPauseIcon = buildPauseButton.transform.Find("Ícone").GetComponent<Image>();
        speedButton = TopControl(root.transform, "Velocidade", "1x", null, "v2/square_button",
            new Vector2(.8915f,.917f), new Vector2(.9235f,.974f), CycleSpeed);
        speedText = speedButton.GetComponentInChildren<Text>();
        settingsButton = TopControl(root.transform, "Configurações", "⚙", "configuracoes", "v2/square_button",
            new Vector2(.953f,.917f), new Vector2(.985f,.974f), () => GameManager.Instance?.TogglePause());
        threat = Label(root.transform, "", new Vector2(.28f,.745f), new Vector2(.72f,.79f), 23);
        threat.color = new Color32(255,222,126,255);

        startWaveButton = MakeTopWaveButton(root.transform, "Iniciar Onda", "v2/action_button", null,
            new Vector2(.015f,.914f), new Vector2(.175f,.974f), () => waveSpawner?.StartNextWave());
        startWaveButton.GetComponent<HudButtonFeedback>().SetSprites(
            Art("v2/action_button"), Art("v2/action_button"), Art("v2/action_button"));
        autoWaveButton = MakeTopWaveButton(root.transform, "Auto: desligado", "v2/action_button", "auto",
            new Vector2(.83f,.85f), new Vector2(.985f,.91f), () => {
                if (waveSpawner != null) waveSpawner.SetAutoStart(!waveSpawner.AutoStart);
            });
        startWaveText = startWaveButton.GetComponentInChildren<Text>();
        autoWaveText = autoWaveButton.GetComponentInChildren<Text>();

        overlay = Rect(root.transform, "Submenu", Vector2.zero, Vector2.one).gameObject;
        var dim = overlay.AddComponent<Image>(); dim.color = new Color(.06f,.035f,.015f,.72f);
        var panel = Picture(overlay.transform, "Moldura da pausa", "moldura_pausa", new Vector2(.23f,.13f), new Vector2(.77f,.87f));
        panelImage = panel;
        heading = Label(panel.transform, "", new Vector2(.13f,.82f), new Vector2(.87f,.93f), 40);
        description = Label(panel.transform, "", new Vector2(.12f,.64f), new Vector2(.88f,.81f), 28);
        stats = Rect(panel.transform,"Atributos",new Vector2(.12f,.53f),new Vector2(.88f,.64f));
        rows = Rect(panel.transform,"Opcoes",new Vector2(.16f,.16f),new Vector2(.84f,.53f));
        MakeButton(panel.transform, new Action("Voltar", "voltar", "voltar", Back), new Vector2(.34f,.055f), new Vector2(.66f,.15f));
        overlay.SetActive(false);
    }
    void UpdateWaveControls()
    {
        if (startWaveButton == null) return;
        if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();
        bool playing = GameManager.State == GameState.Playing;
        startWaveButton.interactable = waveSpawner != null && waveSpawner.CanStartNextWave;
        autoWaveButton.interactable = waveSpawner != null && playing;
        settingsButton.interactable = playing || GameManager.State == GameState.Paused;
        speedButton.interactable = playing;
        buildPauseButton.interactable = playing;
        speedText.text = Mathf.Approximately(GameManager.GameSpeed, 1.5f) ? "1,5x" :
            Mathf.Approximately(GameManager.GameSpeed, 2f) ? "2x" : "1x";
        buildPauseIcon.sprite = Art(GameManager.IsBuildPaused ? "icone_continuar" : "icone_pausa");
        buildPauseButton.GetComponent<HudButtonFeedback>().SetToggleState(GameManager.IsBuildPaused);
        if (waveSpawner != null)
        {
            startWaveText.text = "Iniciar Onda";
            UpdateWave(waveSpawner.CurrentWave, waveSpawner.TotalWaves);
            autoWaveText.text = waveSpawner.AutoStart ? "Auto: ligado" : "Auto: desligado";
            autoWaveButton.GetComponent<HudButtonFeedback>().SetToggleState(waveSpawner.AutoStart);
        }
    }
    void CycleSpeed()
    {
        float currentSpeed = GameManager.GameSpeed;
        GameManager.Instance?.SetGameSpeed(Mathf.Approximately(currentSpeed, 1f) ? 1.5f :
            Mathf.Approximately(currentSpeed, 1.5f) ? 2f : 1f);
    }
    Button TopControl(Transform parent, string name, string caption, string icon, string art, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction callback)
    {
        var holder = Rect(parent, name, min, max);
        string plateArt = art.StartsWith("v2/") ? art : art + "_normal";
        var plate = Picture(holder, "Placa", plateArt, Vector2.zero, Vector2.one, true);
        plate.raycastTarget = true;
        var button = holder.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;
        button.transition = Selectable.Transition.None;
        var feedback = holder.gameObject.AddComponent<HudButtonFeedback>();
        feedback.Configure(plate, Art(plateArt),
            Art(art.StartsWith("v2/") ? art : art + "_selecionado"));
        feedback.ConfigureSound(uiAudio, hoverSound);
        if (icon != null)
        {
            var iconImage = Picture(holder, "Ícone", icon.StartsWith("v2/") ? icon : "icone_" + icon,
                new Vector2(.27f,.27f), new Vector2(.73f,.77f), true);
            if (iconImage.sprite == null)
            {
                iconImage.gameObject.SetActive(false);
                var fallback = Label(holder, caption, new Vector2(.05f,.08f), new Vector2(.95f,.92f), 32);
                fallback.resizeTextForBestFit = true;
            }
        }
        else if (!string.IsNullOrEmpty(caption))
        {
            var label = Label(holder, caption, new Vector2(.16f,.25f), new Vector2(.84f,.77f), 32);
            label.resizeTextForBestFit = true;
        }
        button.onClick.AddListener(PlayClick);
        button.onClick.AddListener(callback);
        return button;
    }
    Text Counter(Transform parent, string icon, float left, float right)
    {
        var badge = Rect(parent, "Indicador " + icon, new Vector2(left,.24f), new Vector2(right,.53f));
        string art = "icone_" + icon;
        Picture(badge, icon, art, new Vector2(.04f,.13f), new Vector2(.31f,.87f), true);
        var value = Label(badge, "0", new Vector2(.32f,.05f), new Vector2(.99f,.95f), 32, false);
        value.resizeTextForBestFit = true;
        return value;
    }
    void Clear(Transform parent)
    {
        foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }
    void Show(string title, string body, bool isTower, params Action[] actions)
        => ShowWithLayout(title, body, isTower, false, actions);
    void ShowWithLayout(string title, string body, bool isTower, bool horizontalActions, params Action[] actions)
    {
        if (upgradeView != null) upgradeView.SetActive(false);
        panelImage.gameObject.SetActive(true);
        rows.gameObject.SetActive(true);
        stats.gameObject.SetActive(true);
        if (catalogView != null) catalogView.SetActive(false);
        Clear(rows); Clear(stats); purchases.Clear(); towerPanel = isTower;
        heading.text = title; description.text = body; overlay.SetActive(true);
        description.rectTransform.anchorMin = new Vector2(.12f,horizontalActions ? .73f : .64f);
        description.rectTransform.anchorMax = new Vector2(.88f,horizontalActions ? .82f : .81f);
        bool paused = GameManager.State == GameState.Paused && !isTower;
        overlay.GetComponent<Image>().color = new Color(.06f,.035f,.015f,isTower ? .43f : .72f);
        panelImage.sprite = Art(paused ? "moldura_pausa_titulo" : "moldura_pausa");
        heading.gameObject.SetActive(!paused);
        stats.anchorMin = new Vector2(.12f,horizontalActions ? .56f : .53f);
        stats.anchorMax = new Vector2(.88f,horizontalActions ? .70f : .64f);
        rows.anchorMin = new Vector2(horizontalActions ? .09f : isTower ? .16f : .24f,
            horizontalActions ? .20f : .19f);
        rows.anchorMax = new Vector2(horizontalActions ? .91f : isTower ? .84f : .76f,
            horizontalActions ? .47f : isTower ? .53f : .60f);
        int n = Mathf.Max(1,actions.Length);
        for (int i=0;i<actions.Length;i++)
            MakeButton(rows,actions[i],horizontalActions
                ? new Vector2(i/(float)n+.008f,.02f)
                : new Vector2(0,1f-(i+1f)/n+.015f),horizontalActions
                ? new Vector2((i+1f)/n-.008f,.98f)
                : new Vector2(1,1f-i/(float)n-.015f));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    void Close()
    {
        overlay.SetActive(false);
        if (resultView != null) { Destroy(resultView); resultView = null; }
        if (upgradeView != null) { Destroy(upgradeView); upgradeView = null; }
        panelImage.gameObject.SetActive(true);
        if (catalogView != null) catalogView.SetActive(false);
        towerPanel = false;
        selectedPlot = null;
        purchases.Clear();
        selectedRange = false;
        hoverPlot = null;
        if (rangeRenderer != null) rangeRenderer.enabled = false;
        EndDecisionPause();
    }
    void Back()
    {
        if (towerPanel && catalogView != null && catalogView.activeSelf) Close();
        else if (towerPanel && selectedPlot != null && selectedPlot.Placed == null) ShowTowerChoices(selectedPlot, catalog);
        else if (towerPanel) Close();
        else if (GameManager.State == GameState.Paused) GameManager.Instance?.TogglePause();
        else if (GameManager.State == GameState.Victory || GameManager.State == GameState.Defeat) ToMenu();
        else Close();
    }
    void ToMenu()
    {
        Close();
        GameManager.Instance?.SetState(GameState.Menu);
        MenuController.LoadMenu();
    }
    public static void ShowTowerChoices(BuildPlot plot, TowerData[] choices)
    {
        if (current == null || plot == null) return;
        current.selectedPlot = plot;
        current.catalog = choices;
        current.BeginDecisionPause();
        current.ShowCatalog();
    }
    void ShowCatalog()
    {
        if (catalogView != null) { catalogView.SetActive(false); Destroy(catalogView); catalogView = null; }
        Show("ESCOLHA UMA TORRE", "Ordenadas pelo custo inicial. Selecione uma placa para ver os atributos.", true);
        HideSelectedRange();
        description.rectTransform.anchorMin = new Vector2(.13f,.70f);
        rows.gameObject.SetActive(false);
        stats.gameObject.SetActive(false);
        catalogView = Rect(panelImage.transform,"Lista de torres",new Vector2(.12f,.16f),new Vector2(.88f,.70f)).gameObject;
        var viewport = Rect(catalogView.transform,"Area visivel",Vector2.zero,Vector2.one);
        // RectMask2D corta pelo retangulo. O Mask com imagem quase transparente escondia
        // a lista inteira: alpha 0,001 vira 0 em 8 bits e a mascara nao desenha nada.
        viewport.gameObject.AddComponent<RectMask2D>();
        // imagem transparente so para a rolagem do mouse funcionar no espaco vazio
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
        var content = Rect(viewport,"Conteudo",new Vector2(0,1),new Vector2(1,1));
        content.pivot = new Vector2(.5f,1);
        var scroll = catalogView.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55;
        var sorted = new List<TowerData>();
        foreach (TowerData data in catalog ?? new TowerData[0])
            if (data != null && data.towerPrefab != null && data.levels != null && data.levels.Length > 0) sorted.Add(data);
        sorted.Sort((a,b) => {
            int cmp = a.levels[0].cost.CompareTo(b.levels[0].cost);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.displayName,b.displayName);
        });
        const float cardHeight = 112f, gap = 10f;
        content.sizeDelta = new Vector2(0, sorted.Count * (cardHeight + gap));
        for (int i = 0; i < sorted.Count; i++)
        {
            TowerData data = sorted[i];
            var card = Rect(content,data.displayName,new Vector2(0,1),new Vector2(1,1));
            card.pivot = new Vector2(.5f,1);
            card.anchoredPosition = new Vector2(0,-i * (cardHeight + gap));
            card.sizeDelta = new Vector2(-12,cardHeight);
            var image = card.gameObject.AddComponent<Image>();
            image.sprite = Art("botao_normal"); image.type = Image.Type.Sliced;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var feedback = card.gameObject.AddComponent<HudButtonFeedback>();
            feedback.Configure(image,Art("botao_normal"),Art("botao_selecionado"));
            feedback.ConfigureSound(uiAudio,hoverSound);
            button.onClick.AddListener(PlayClick);
            button.onClick.AddListener(() => ShowTowerDetail(data));
            var portrait = Rect(card,"Retrato",new Vector2(.035f,.17f),new Vector2(.17f,.83f)).gameObject.AddComponent<Image>();
            portrait.sprite = data.levels[0].sprite;
            if (portrait.sprite == null) portrait.sprite = data.towerPrefab.GetComponent<SpriteRenderer>()?.sprite;
            portrait.preserveAspect = true; portrait.raycastTarget = false;
            Label(card,data.displayName,new Vector2(.19f,.48f),new Vector2(.79f,.89f),30);
            string target = data.hitsFlying ? "Aéreos e terrestres" : "Terrestres";
            Label(card,target,new Vector2(.19f,.11f),new Vector2(.65f,.49f),21);
            Label(card,$"{Mathf.Max(0,data.levels[0].cost)} moedas",new Vector2(.70f,.13f),new Vector2(.96f,.77f),24);
        }
        if (sorted.Count == 0) Label(content,"Nenhuma torre configurada no catálogo.",new Vector2(0,0),Vector2.one,26);
    }
    void ShowTowerDetail(TowerData data)
    {
        if (data == null || selectedPlot == null) return;
        if (catalogView != null) catalogView.SetActive(false);
        int cost = Mathf.Max(0,data.levels[0].cost);
        string target = data.hitsFlying ? "Aéreos e terrestres" : "Somente terrestres";
        Show(data.displayName.ToUpperInvariant(),$"Custo inicial: {cost} moedas · {target}\nMelhorias: {Mathf.Max(0,data.levels.Length-1)} níveis",true,
            new Action($"Construir · {cost} moedas","construir","melhorar",()=> {
                if (selectedPlot.Build(data)) Close();
                else description.text = "Construção indisponível. Confira saldo e canteiro.";
            },cost));
        rows.gameObject.SetActive(true);
        stats.gameObject.SetActive(true);
        rows.anchorMin = new Vector2(.20f,.20f); rows.anchorMax = new Vector2(.80f,.34f);
        stats.anchorMin = new Vector2(.12f,.40f); stats.anchorMax = new Vector2(.88f,.54f);
        TowerLevel level = data.levels[0];
        ShowSelectedRange(selectedPlot, level.range);
        Stat(0,"dano",$"Dano\n{level.damage:0.#}");
        Stat(1,"alcance",$"Alcance\n{level.range:0.#}");
        Stat(2,"cadencia",$"Tiros/s\n{level.fireRate:0.#}");
        Stat(3,"aereos",data.hitsFlying?"Aéreos\nSIM":"Aéreos\nNÃO");
    }
    public static void ShowTowerActions(BuildPlot plot)
    {
        if (current == null || plot == null || plot.Placed == null) return;
        current.selectedPlot = null;
        current.BeginDecisionPause();
        current.ShowUpgrade(plot);
    }
    Sprite TowerSprite(Tower tower, int level)
    {
        var sprite = tower.data.levels[level].sprite;
        if (sprite == null && level == tower.Level)
            sprite = tower.GetComponent<SpriteRenderer>()?.sprite;
        if (sprite == null && (level == tower.Level || level == 0))
            sprite = tower.data.towerPrefab?.GetComponent<SpriteRenderer>()?.sprite;
        return sprite;
    }
    string TowerLevelFrame(int level)
    {
        // Os tres primeiros niveis tem letreiro incorporado no sprite.
        return level < 3 ? "moldura_retrato_torre_nivel_" + (level + 1) :
            "moldura_retrato_torre";
    }
    Button UpgradeButton(Transform parent, string art, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction callback, bool enabled = true, int cost = -1)
    {
        var holder = Rect(parent, art, min, max);
        var image = Picture(holder, "Placa", art + "_normal", Vector2.zero, Vector2.one, true);
        image.raycastTarget = true;
        var button = holder.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        var feedback = holder.gameObject.AddComponent<HudButtonFeedback>();
        feedback.Configure(image, Art(art + "_normal"), Art(art + "_selecionado"));
        feedback.ConfigureSound(uiAudio, hoverSound);
        button.interactable = enabled && (cost < 0 || GameManager.Money >= cost);
        button.onClick.AddListener(PlayClick);
        button.onClick.AddListener(callback);
        if (enabled && cost >= 0) purchases.Add(new Purchase { button = button, cost = cost });
        return button;
    }
    void UpgradeStat(Transform parent, int index, string icon, string title, string value)
    {
        float left = index * .25f;
        Picture(parent, title, "icone_" + icon, new Vector2(left + .005f,.18f),
            new Vector2(left + .075f,.82f), true);
        var label = Label(parent, title + "\n" + value,
            new Vector2(left + .08f,0), new Vector2(left + .245f,1), 22);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 15;
        label.resizeTextMaxSize = 22;
    }
    void ShowUpgrade(BuildPlot plot)
    {
        Tower tower = plot.Placed;
        if (tower == null || tower.data == null || tower.data.levels == null) return;
        if (catalogView != null) catalogView.SetActive(false);
        if (upgradeView != null) { upgradeView.SetActive(false); Destroy(upgradeView); }
        purchases.Clear();
        towerPanel = true;
        overlay.SetActive(true);
        overlay.GetComponent<Image>().color = new Color(.06f,.035f,.015f,.6f);
        panelImage.gameObject.SetActive(false);
        upgradeView = Rect(overlay.transform,"Melhorar torre",new Vector2(.11f,.025f),
            new Vector2(.89f,.975f)).gameObject;
        var board = Rect(upgradeView.transform,"Painel de melhoria",Vector2.zero,Vector2.one);
        var fit = board.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1.46f;
        Picture(board,"Moldura","moldura_melhorar",Vector2.zero,Vector2.one);
        var title = Label(board,tower.data.displayName.ToUpperInvariant(),
            new Vector2(.20f,.695f),new Vector2(.80f,.775f),42);
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 26;
        title.resizeTextMaxSize = 42;

        var portrait = Rect(board,"Torre atual",new Vector2(.12f,.335f),
            new Vector2(.345f,.70f));
        var portraitFrame = portrait.gameObject.AddComponent<Image>();
        portraitFrame.sprite = Art(TowerLevelFrame(tower.Level));
        portraitFrame.raycastTarget = false;
        var currentSprite = Rect(portrait,"Sprite da torre",new Vector2(.16f,.23f),
            new Vector2(.84f,.81f)).gameObject.AddComponent<Image>();
        currentSprite.sprite = TowerSprite(tower,tower.Level);
        currentSprite.preserveAspect = true;
        currentSprite.raycastTarget = false;
        if (tower.Level >= 3)
            Label(portrait,$"Nível {tower.Level+1}",new Vector2(.07f,.04f),
                new Vector2(.93f,.17f),27);

        var attributesFrame = Rect(board,"Moldura dos atributos",
            new Vector2(.37f,.565f),new Vector2(.89f,.69f));
        var attributesImage = attributesFrame.gameObject.AddComponent<Image>();
        attributesImage.sprite = Art("moldura_atributos_nova");
        attributesImage.raycastTarget = false;
        var attributes = Rect(attributesFrame,"Atributos",new Vector2(.045f,.13f),
            new Vector2(.955f,.87f));
        UpgradeStat(attributes,0,"dano","Dano",$"{tower.Stats.damage:0.#}");
        UpgradeStat(attributes,1,"alcance","Alcance",$"{tower.Stats.range:0.#}");
        UpgradeStat(attributes,2,"cadencia","Tiros/s",$"{tower.Stats.fireRate:0.#}");
        UpgradeStat(attributes,3,"aereos","Aéreos",tower.data.hitsFlying?"SIM":"NÃO");

        int levels = tower.data.levels.Length;
        var levelStrip = Rect(board,"Níveis",new Vector2(.37f,.33f),
            new Vector2(.89f,.55f));
        for (int i = 0; i < levels; i++)
        {
            float start = i / (float)levels;
            var card = Rect(levelStrip,"Nível " + (i+1),new Vector2(start+.021f,.015f),
                new Vector2((i+1f)/levels-.021f,.985f));
            var backdrop = card.gameObject.AddComponent<Image>();
            backdrop.sprite = Art(TowerLevelFrame(i));
            backdrop.color = i == tower.Level ? Color.white : new Color(.78f,.78f,.78f,1f);
            backdrop.raycastTarget = false;
            if (i >= 3)
                Label(card,$"Nível {i+1}",new Vector2(.08f,.04f),new Vector2(.92f,.18f),20);
            var icon = Rect(card,"Sprite nível " + (i+1),new Vector2(.18f,.23f),
                new Vector2(.82f,.81f)).gameObject.AddComponent<Image>();
            icon.sprite = TowerSprite(tower,i);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (icon.sprite == null)
            {
                icon.gameObject.SetActive(false);
                Label(card,"Arte pendente",new Vector2(.10f,.24f),new Vector2(.90f,.70f),18);
            }
        }

        int upgradeCost = tower.CanUpgrade ? plot.GetNextUpgradeCost() : -1;
        UpgradeButton(board,"melhorar",new Vector2(.13f,.22f),
            new Vector2(.36f,.305f),()=> { if (plot.Upgrade()) ShowTowerActions(plot); },
            tower.CanUpgrade,upgradeCost);
        UpgradeButton(board,"vender",new Vector2(.385f,.22f),
            new Vector2(.615f,.305f),()=> { if (plot.Sell()) Close(); });
        UpgradeButton(board,"prioridade",new Vector2(.64f,.22f),
            new Vector2(.87f,.305f),()=> {
                tower.mode = tower.mode == TargetMode.Strongest ? TargetMode.First : TargetMode.Strongest;
                ShowTowerActions(plot);
            });
        Label(board,tower.CanUpgrade ? $"Custo: {upgradeCost} moedas" : "Nível máximo",
            new Vector2(.13f,.17f),new Vector2(.36f,.215f),22);
        Label(board,$"Receber: {plot.GetSellRefund()} moedas",
            new Vector2(.385f,.17f),new Vector2(.615f,.215f),22);
        Label(board,tower.mode == TargetMode.Strongest ? "Alvo: Mais vida" : "Alvo: Primeiro",
            new Vector2(.64f,.17f),new Vector2(.87f,.215f),22);
        UpgradeButton(board,"voltar",new Vector2(.39f,.055f),
            new Vector2(.61f,.15f),Back);
        ShowSelectedRange(plot,tower.Stats.range);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    void BeginDecisionPause()
    {
        if (decisionOpen) return;
        decisionOpen = true;
        GameManager.Instance?.SetDecisionPause(true);
    }
    void EndDecisionPause()
    {
        if (!decisionOpen) return;
        decisionOpen = false;
        GameManager.Instance?.SetDecisionPause(false);
    }
    static Sprite RangeSprite()
    {
        if (rangeSprite != null) return rangeSprite;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63f;
                pixels[y * size + x] = radius > 1f ? Color.clear :
                    new Color(1f, 1f, 1f, radius > .965f ? .78f : .14f);
            }
        texture.SetPixels(pixels); texture.Apply();
        rangeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64f);
        return rangeSprite;
    }
    void DrawRange(BuildPlot plot, float radius, bool selected)
    {
        if (plot == null || radius <= 0f) return;
        if (rangeRenderer == null)
        {
            rangeRenderer = new GameObject("Alcance da torre").AddComponent<SpriteRenderer>();
            rangeRenderer.sprite = RangeSprite();
        }
        rangeRenderer.transform.position = plot.transform.position;
        rangeRenderer.transform.localScale = Vector3.one * radius;
        rangeRenderer.sortingOrder = DrawOrder.ForY(plot.transform.position.y) - 1;
        rangeRenderer.color = selected ? new Color(1f, .87f, .39f, .86f) : new Color(.7f, .92f, 1f, .6f);
        rangeRenderer.enabled = true;
    }
    void ShowSelectedRange(BuildPlot plot, float radius)
    {
        selectedRange = true;
        hoverPlot = null;
        DrawRange(plot, radius, true);
    }
    void HideSelectedRange()
    {
        selectedRange = false;
        if (rangeRenderer != null) rangeRenderer.enabled = false;
    }
    public static void ShowHoverRange(BuildPlot plot)
    {
        if (current == null || current.selectedRange || plot == null || plot.Placed == null || IsOverlayOpen) return;
        if (current.hoverPlot == plot) return;
        current.hoverPlot = plot;
        current.DrawRange(plot, plot.Placed.Stats.range, false);
    }
    public static void HideHoverRange(BuildPlot plot)
    {
        if (current == null || current.selectedRange || current.hoverPlot != plot) return;
        current.hoverPlot = null;
        if (current.rangeRenderer != null) current.rangeRenderer.enabled = false;
    }
    void Stat(int column, string icon, string text)
    {
        float x=column*.25f;
        Picture(stats,icon,"icone_"+icon,new Vector2(x,.2f),new Vector2(x+.07f,.8f),true);
        Label(stats,text,new Vector2(x+.075f,0),new Vector2(x+.245f,1),18);
    }
    void StateChanged(GameState state)
    {
        settingsButton.interactable = state == GameState.Playing || state == GameState.Paused;
        if (state == GameState.Paused)
        {
            EndDecisionPause();
            HideSelectedRange();
            Show("JOGO PAUSADO","A colheita pode esperar um pouquinho.\nRetome a defesa quando estiver pronto.",false,
                new Action("Continuar","continuar","continuar",()=>GameManager.Instance?.TogglePause()),
                new Action("Recomeçar nível","recomecar","voltar",()=>GameManager.Instance?.RestartCurrentLevel()),
                new Action("Voltar ao menu","menu","voltar",ToMenu));
        }
        else if (state == GameState.Playing) Close();
    }
    void Result(GameResult result)
    {
        EndDecisionPause();
        HideSelectedRange();
        towerPanel = false;
        purchases.Clear();
        if (catalogView != null) catalogView.SetActive(false);
        if (upgradeView != null) upgradeView.SetActive(false);
        if (resultView != null) { resultView.SetActive(false); Destroy(resultView); }
        overlay.SetActive(true);
        overlay.GetComponent<Image>().color = new Color(.04f,.025f,.015f,.62f);
        panelImage.gameObject.SetActive(false);
        resultView = Rect(overlay.transform,"Resultado da partida",new Vector2(.08f,.04f),new Vector2(.92f,.96f)).gameObject;
        var board = Rect(resultView.transform,"Painel",Vector2.zero,Vector2.one);
        var fit = board.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1.65f;
        string theme = result.won ? "vitoria" : "derrota";
        Picture(board,"Moldura","final_moldura_" + theme,Vector2.zero,Vector2.one);
        ResultStat(board,"icone_pontuacao","Pontuação",result.score.ToString(),.51f);
        ResultStat(board,"icone_rodadas_final","Rodadas",result.round.ToString(),.39f);
        ResultStat(board,"icone_derrotados","Inimigos derrotados",$"{result.enemiesDefeated}/{result.enemiesTotal}",.27f);
        var card = Rect(board,"Torre com mais dano",new Vector2(.65f,.25f),new Vector2(.85f,.63f));
        Picture(card,"Moldura da torre","final_torre_" + theme,Vector2.zero,Vector2.one);
        ResultLabel(card,"TORRE COM MAIS DANO",new Vector2(.10f,.81f),new Vector2(.90f,.94f),24);
        if (result.topDamageSprite != null)
        {
            var portrait = Rect(card,"Torre",new Vector2(.25f,.35f),new Vector2(.75f,.79f)).gameObject.AddComponent<Image>();
            portrait.sprite = result.topDamageSprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
        }
        else ResultLabel(card,"Nenhuma torre",new Vector2(.12f,.42f),new Vector2(.88f,.67f),23);
        ResultLabel(card,result.topDamage > 0 ? result.topDamageTower : "Sem dano registrado",
            new Vector2(.10f,.20f),new Vector2(.90f,.36f),23);
        ResultLabel(card,$"Dano: {result.topDamage:0}",new Vector2(.12f,.08f),new Vector2(.88f,.20f),28);
        UpgradeButton(board,"recomecar",new Vector2(.18f,.12f),new Vector2(.39f,.22f),()=>GameManager.Instance?.RestartCurrentLevel());
        UpgradeButton(board,"menu",new Vector2(.41f,.12f),new Vector2(.62f,.22f),ToMenu);
        UpgradeButton(board,"voltar",new Vector2(.64f,.12f),new Vector2(.82f,.22f),Back);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    Text ResultLabel(Transform parent,string text,Vector2 min,Vector2 max,int size)
    {
        var label = Label(parent,text,min,max,size);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = size;
        return label;
    }
    void ResultStat(Transform parent,string icon,string title,string value,float bottom)
    {
        Picture(parent,title,"final_" + icon,new Vector2(.13f,bottom),new Vector2(.19f,bottom+.085f),true);
        var label = ResultLabel(parent,title + ": " + value,new Vector2(.21f,bottom),new Vector2(.59f,bottom+.085f),32);
        label.alignment = TextAnchor.MiddleLeft;
    }
    void UpdateMoney(int value)
    {
        coins.text=value.ToString();
        foreach(var p in purchases) if(p.button!=null) p.button.interactable=value>=p.cost;
    }
    void UpdateLife(int value) => life.text=$"{value}/{GameManager.StartingLife}";
    void UpdateWave(int value,int total) => waves.text=$"{value}/{total}";
    void UpdateComposition(int value,int total,bool flying) { UpdateWave(value,total); threat.text=flying?"AÉREOS NESTA RODADA · prepare sua defesa":""; }
}
