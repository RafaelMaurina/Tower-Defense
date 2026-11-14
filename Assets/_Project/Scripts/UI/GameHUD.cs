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
    RectTransform bottomBarArea;
    readonly List<HudButtonFeedback> bottomCardFeedback = new List<HudButtonFeedback>();
    readonly List<Text> bottomPrices = new List<Text>();
    readonly List<int> bottomCosts = new List<int>();
    readonly List<TowerData> bottomTowers = new List<TowerData>();
    BuildPlot[] buildPlots;
    BuildPlot pendingBuildPlot;
    TowerData buildSelection, hoveredCard;
    SpriteRenderer buildGhost, buildRadius, plotMarker;
    Text buildHint;
    bool draggingCard;
    readonly List<RaycastResult> buildRaycastHits = new List<RaycastResult>(16);
    PointerEventData buildPointer;
    EventSystem buildEventSystem;
    AudioSource uiAudio;
    AudioClip hoverSound, clickSound;


    Sprite Art(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite))
        {
            if (name.StartsWith("v2/")) sprite = NewHudArt(name.Substring(3));
            else if (name == "inferior_barra_torres" || name == "inferior_carta_torre")
            {
                var source = Resources.Load<Sprite>("hud/" + ArtPath(name));
                if (source != null)
                {
                    var crop = name == "inferior_barra_torres" ? new Rect(16,74,2140,598) : new Rect(188,114,754,1165);
                    var originalSize = name == "inferior_barra_torres" ? new Vector2(2172,724) : new Vector2(1158,1358);
                    crop = new Rect(crop.x * source.texture.width / originalSize.x, crop.y * source.texture.height / originalSize.y,
                        crop.width * source.texture.width / originalSize.x, crop.height * source.texture.height / originalSize.y);
                    sprite = Sprite.Create(source.texture,crop,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
                    generatedSprites.Add(sprite);
                }
            }
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
        if (name.StartsWith("inferior_")) return "hud_inferior/" + name.Substring(9);
        if (name.StartsWith("final_")) return "finais_submenu/" + name.Substring(6);
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
        CreateBottomTowerBar();
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
        if (buildGhost != null) Destroy(buildGhost.gameObject);
        if (buildRadius != null) Destroy(buildRadius.gameObject);
        if (plotMarker != null) Destroy(plotMarker.gameObject);
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
        if (bottomBarArea != null)
        {
            bool visible = bottomCardFeedback.Count > 0 && !IsOverlayOpen && GameManager.State == GameState.Playing;
            if (bottomBarArea.gameObject.activeSelf != visible) bottomBarArea.gameObject.SetActive(visible);
        }
        if (Keyboard.current == null) return;
        if (overlay.activeSelf && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
        {
            foreach (Button option in overlay.GetComponentsInChildren<Button>())
                if (option.interactable) { EventSystem.current.SetSelectedGameObject(option.gameObject); break; }
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (!IsOverlayOpen && (buildSelection != null || pendingBuildPlot != null)) { CancelBuildSelection(); return; }
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
        t.text = text; UiTypography.Apply(t,size);
        t.color = new Color32(255, 239, 196, 255);
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

        // Centro em x=0.5, no mesmo eixo do celeiro da moldura superior.
        bottomBarArea = Rect(root.transform,"HUD inferior de torres",new Vector2(.28f,.006f),new Vector2(.72f,.165f));
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
    void CreateBottomTowerBar()
    {
        var towers = new List<TowerData>();
        buildPlots = FindObjectsByType<BuildPlot>();
        foreach (var plot in buildPlots)
            foreach (var data in plot.AvailableTowers ?? new TowerData[0])
                if (data != null && data.towerPrefab != null && data.levels != null &&
                    data.levels.Length > 0 && !towers.Contains(data)) towers.Add(data);
        towers.Sort((a,b) => {
            int cost = a.levels[0].cost.CompareTo(b.levels[0].cost);
            return cost != 0 ? cost : string.CompareOrdinal(a.displayName,b.displayName);
        });
        if (towers.Count == 0) { bottomBarArea.gameObject.SetActive(false); return; }
        var board = Rect(bottomBarArea,"Barra de cartas",Vector2.zero,Vector2.one);
        var fit = board.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 5f;
        var background = Picture(board,"Moldura","inferior_barra_torres",Vector2.zero,Vector2.one);
        background.raycastTarget = false;
        buildHint = Label(bottomBarArea,"",new Vector2(-.10f,1.01f),new Vector2(1.10f,1.15f),20);
        buildHint.resizeTextForBestFit = true;
        buildHint.resizeTextMinSize = 14;
        float step = .90f / towers.Count;
        for (int i=0;i<towers.Count;i++)
        {
            var data = towers[i];
            var slot = Rect(board,data.displayName,new Vector2(.05f+i*step+.004f,.13f),
                new Vector2(.05f+(i+1)*step-.004f,.85f));
            var card = Rect(slot,"Carta proporcional",Vector2.zero,Vector2.one);
            var cardFit = card.gameObject.AddComponent<AspectRatioFitter>();
            cardFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            cardFit.aspectRatio = 754f/1165f;
            var plate = Picture(card,"Carta","inferior_carta_torre",Vector2.zero,Vector2.one,true);
            plate.raycastTarget = true;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            var feedback = card.gameObject.AddComponent<HudButtonFeedback>();
            feedback.Configure(plate,Art("inferior_carta_torre"),Art("inferior_carta_torre"));
            feedback.ConfigureSound(uiAudio,hoverSound);
            bottomCardFeedback.Add(feedback);
            bottomTowers.Add(data);
            button.onClick.AddListener(PlayClick);
            button.onClick.AddListener(()=> {
                SelectBuildTower(data);
            });
            var events = card.gameObject.AddComponent<EventTrigger>();
            CardEvent(events,EventTriggerType.PointerEnter,()=>hoveredCard=data);
            CardEvent(events,EventTriggerType.PointerExit,()=> { if (hoveredCard == data) hoveredCard=null; });
            CardEvent(events,EventTriggerType.BeginDrag,()=> { draggingCard=true; SelectBuildTower(data,false); });
            CardEvent(events,EventTriggerType.Drag,()=> {});
            CardEvent(events,EventTriggerType.EndDrag,()=> {
                draggingCard=false;
                var plot=PlotUnderPointer();
                if (plot != null) TryBuildSelected(plot);
            });
            string cardName = TowerDisplayName(data);
            if (cardName == "Metralhadora de Uvas") cardName = "Metralhadora\nde Uvas";
            else if (cardName == "Canhao de Abobora" || cardName == "Canhão de Abóbora") cardName = "Canhão de\nAbóbora";
            else if (cardName == "Tufao") cardName = "Tufão";
            var title = Label(card,cardName,new Vector2(.095f,.79f),new Vector2(.905f,.955f),22,false);
            title.color = new Color32(55,30,14,255);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 22;
            var portrait = Rect(card,"Torre nível 1",new Vector2(.18f,.25f),new Vector2(.82f,.76f)).gameObject.AddComponent<Image>();
            portrait.sprite = data.levels[0].sprite != null ? data.levels[0].sprite :
                data.towerPrefab.GetComponent<SpriteRenderer>()?.sprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            Picture(card,"Moeda","icone_moedas",new Vector2(.19f,.045f),new Vector2(.38f,.195f),true);
            int cost = Mathf.Max(0,data.levels[0].cost);
            var price = Label(card,cost.ToString(),new Vector2(.40f,.04f),new Vector2(.83f,.20f),25,false);
            price.resizeTextForBestFit = true;
            price.resizeTextMinSize = 14;
            price.resizeTextMaxSize = 25;
            bottomCosts.Add(cost);
            bottomPrices.Add(price);
        }
    }
    static string TowerDisplayName(TowerData data)
    {
        return data.displayName.Replace("Canhao", "Canhão").Replace("Abobora", "Abóbora").Replace("Tufao", "Tufão");
    }
    static void CardEvent(EventTrigger trigger,EventTriggerType type,UnityEngine.Events.UnityAction callback)
    {
        var entry = new EventTrigger.Entry { eventID=type };
        entry.callback.AddListener(_=>callback());
        trigger.triggers.Add(entry);
    }
    void SelectBuildTower(TowerData data,bool buildPending=true)
    {
        if (IsOverlayOpen || GameManager.State != GameState.Playing) return;
        buildSelection=data;
        HideSelectedRange();
        for (int i=0;i<bottomCardFeedback.Count;i++) bottomCardFeedback[i].SetToggleState(bottomTowers[i]==data);
        buildHint.text="Selecionado: "+TowerDisplayName(data)+" · Esc ou botão direito para cancelar";
        if (buildPending && pendingBuildPlot != null) TryBuildSelected(pendingBuildPlot);
    }
    public static void SelectBuildPlot(BuildPlot plot)
    {
        if (current == null || plot == null || plot.Placed != null || IsOverlayOpen || GameManager.State != GameState.Playing) return;
        current.pendingBuildPlot=plot;
        current.HideSelectedRange();
        if (current.buildSelection != null) current.TryBuildSelected(plot);
        else if (current.buildHint != null) current.buildHint.text="Canteiro selecionado · passe pelas cartas e clique para construir";
    }
    void TryBuildSelected(BuildPlot plot)
    {
        if (IsOverlayOpen || GameManager.State != GameState.Playing || buildSelection == null || plot == null) return;
        if (plot.Placed != null) { buildHint.text="Este canteiro já está ocupado"; return; }
        if (!plot.Accepts(buildSelection)) { buildHint.text="Torre indisponível neste canteiro"; return; }
        if (!plot.Build(buildSelection)) { buildHint.text="Moedas insuficientes para construir "+TowerDisplayName(buildSelection); return; }
        CancelBuildSelection();
    }
    BuildPlot PlotUnderPointer()
    {
        if (Mouse.current == null || Camera.main == null) return null;
        // Raycast da UI explicito: tambem cobre o instante de soltar um arrasto.
        if (EventSystem.current != null)
        {
            if (buildPointer == null || buildEventSystem != EventSystem.current)
                { buildEventSystem = EventSystem.current; buildPointer = new PointerEventData(buildEventSystem); }
            buildPointer.Reset();
            buildPointer.position=Mouse.current.position.ReadValue();
            buildRaycastHits.Clear();
            EventSystem.current.RaycastAll(buildPointer,buildRaycastHits);
            if (buildRaycastHits.Count>0) return null;
        }
        Vector2 point=Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if (buildPlots == null) return null;
        foreach (var plot in buildPlots)
            if (plot != null && plot.GetComponent<Collider2D>().OverlapPoint(point)) return plot;
        return null;
    }
    SpriteRenderer BuildVisual(string name,Sprite sprite)
    {
        var renderer=new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.sprite=sprite;
        return renderer;
    }
    void CancelBuildSelection()
    {
        buildSelection=null; hoveredCard=null; pendingBuildPlot=null; draggingCard=false;
        foreach (var feedback in bottomCardFeedback) feedback.SetToggleState(false);
        if (buildHint != null) buildHint.text="";
        HideBuildPreview();
    }
    void HideBuildPreview()
    {
        if (buildGhost != null) buildGhost.enabled=false;
        if (buildRadius != null) buildRadius.enabled=false;
        if (plotMarker != null) plotMarker.enabled=false;
    }
    void LateUpdate()
    {
        HideBuildPreview();
        if (IsOverlayOpen || GameManager.State != GameState.Playing) { hoveredCard=null; return; }
        if (Mouse.current == null || Camera.main == null) return;
        if (Mouse.current.rightButton.wasPressedThisFrame) { CancelBuildSelection(); return; }
        if (pendingBuildPlot != null && pendingBuildPlot.Placed != null) pendingBuildPlot=null;
        if (pendingBuildPlot != null)
        {
            if (plotMarker == null) plotMarker=BuildVisual("Canteiro selecionado",RangeSprite());
            plotMarker.transform.position=pendingBuildPlot.transform.position;
            plotMarker.transform.localScale=Vector3.one*.7f;
            plotMarker.color=new Color(.3f,1f,1f,.9f);
            plotMarker.sortingOrder=DrawOrder.ForY(pendingBuildPlot.transform.position.y)+5;
            plotMarker.enabled=true;
        }
        TowerData data=hoveredCard != null && pendingBuildPlot != null && !draggingCard ? hoveredCard : buildSelection;
        if (data == null) return;
        var plot=draggingCard ? PlotUnderPointer() : pendingBuildPlot != null ? pendingBuildPlot : PlotUnderPointer();
        bool overUi=EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (plot == null && overUi && !draggingCard) return;
        Vector3 position=plot != null ? plot.transform.position+new Vector3(0,-.35f,0) : Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        position.z=0;
        bool valid=plot != null && plot.Placed == null && plot.Accepts(data) && GameManager.Money>=Mathf.Max(0,data.levels[0].cost);
        var prefabSprite=data.towerPrefab.GetComponent<SpriteRenderer>();
        if (buildGhost == null) buildGhost=BuildVisual("Prévia da torre",null);
        buildGhost.sprite=data.levels[0].sprite != null ? data.levels[0].sprite : prefabSprite?.sprite;
        buildGhost.transform.position=position;
        buildGhost.transform.localScale=data.towerPrefab.transform.localScale;
        buildGhost.sortingLayerID=prefabSprite != null ? prefabSprite.sortingLayerID : 0;
        buildGhost.sortingOrder=DrawOrder.ForY(position.y)+5;
        buildGhost.color=valid ? new Color(.75f,1f,.75f,.7f) : new Color(1f,.4f,.4f,.65f);
        buildGhost.enabled=true;
        if (buildRadius == null) buildRadius=BuildVisual("Alcance da prévia",RangeSprite());
        buildRadius.transform.position=position;
        buildRadius.transform.localScale=Vector3.one*data.levels[0].range;
        buildRadius.sortingLayerID=buildGhost.sortingLayerID;
        buildRadius.sortingOrder=buildGhost.sortingOrder-1;
        buildRadius.color=valid ? new Color(.6f,1f,.55f,.75f) : new Color(1f,.35f,.25f,.65f);
        buildRadius.enabled=true;
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
        towerPanel = false;
        purchases.Clear();
        selectedRange = false;
        hoverPlot = null;
        if (rangeRenderer != null) rangeRenderer.enabled = false;
        EndDecisionPause();
    }
    void Back()
    {
        if (towerPanel) Close();

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
    public static void ShowTowerActions(BuildPlot plot)
    {
        if (current == null || plot == null || plot.Placed == null) return;
        current.CancelBuildSelection();
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
        if (current == null || current.selectedRange || current.buildSelection != null || current.pendingBuildPlot != null || plot == null || plot.Placed == null || IsOverlayOpen) return;
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
        UpgradeButton(board,"recomecar",new Vector2(.25f,.12f),new Vector2(.48f,.22f),()=>GameManager.Instance?.RestartCurrentLevel());
        UpgradeButton(board,"menu",new Vector2(.52f,.12f),new Vector2(.75f,.22f),ToMenu);
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
        for (int i=0;i<bottomPrices.Count;i++)
            bottomPrices[i].color = value >= bottomCosts[i] ? new Color32(55,30,14,255) : new Color32(155,35,25,255);
        foreach(var p in purchases) if(p.button!=null) p.button.interactable=value>=p.cost;
    }
    void UpdateLife(int value) => life.text=$"{value}/{GameManager.StartingLife}";
    void UpdateWave(int value,int total) => waves.text=$"{value}/{total}";
    void UpdateComposition(int value,int total,bool flying) { UpdateWave(value,total); threat.text=flying?"AÉREOS NESTA RODADA · prepare sua defesa":""; }
}
