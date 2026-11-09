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
    AudioSource uiAudio;
    AudioClip hoverSound, clickSound;


    Sprite Art(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite))
        {
            if (name.StartsWith("v2/")) sprite = NewHudArt(name.Substring(3));
            else sprite = Resources.Load<Sprite>("hud/" + name);
            sprites[name] = sprite;
        }
        return sprite;
    }
    Sprite NewHudArt(string name)
    {
        var source = Resources.Load<Sprite>("hud/v2/" + name);
        if (source == null) return null;
        Rect crop;
        switch (name)
        {
            case "status_frame": crop = new Rect(0, 124, 2172, 450); break;
            case "action_button": crop = new Rect(0, 114, 2172, 490); break;
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
            foreach (Button option in rows.GetComponentsInChildren<Button>())
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
                new Vector2(.07f,.22f), new Vector2(.20f,.78f), true);
        var caption = Label(holder, name,
            new Vector2(string.IsNullOrEmpty(icon) ? .10f : .22f,.12f), new Vector2(.90f,.88f), 32);
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 22;
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
        Picture(root.transform, "Moldura dos recursos", "v2/status_frame",
            new Vector2(.26f,.815f), new Vector2(.74f,.965f), true);
        life = Counter(root.transform, "vida", .335f, .435f);
        coins = Counter(root.transform, "moedas", .45f, .55f);
        waves = Counter(root.transform, "rodadas", .565f, .665f);
        buildPauseButton = TopControl(root.transform, "Pausar construção", null, "v2/icon_pause", "v2/square_button",
            new Vector2(.79f,.865f), new Vector2(.835f,.945f), () =>
                GameManager.Instance?.SetBuildPause(!GameManager.IsBuildPaused));
        buildPauseIcon = buildPauseButton.transform.Find("Ícone").GetComponent<Image>();
        speedButton = TopControl(root.transform, "Velocidade", "1x", null, "v2/square_button",
            new Vector2(.845f,.865f), new Vector2(.92f,.945f), CycleSpeed);
        speedText = speedButton.GetComponentInChildren<Text>();
        settingsButton = TopControl(root.transform, "Configurações", "⚙", "v2/icon_settings", "v2/square_button",
            new Vector2(.935f,.865f), new Vector2(.98f,.945f), () => GameManager.Instance?.TogglePause());
        threat = Label(root.transform, "", new Vector2(.28f,.745f), new Vector2(.72f,.79f), 23);
        threat.color = new Color32(255,222,126,255);

        startWaveButton = MakeTopWaveButton(root.transform, "Iniciar onda", "v2/action_button", null,
            new Vector2(.015f,.865f), new Vector2(.22f,.945f), () => waveSpawner?.StartNextWave());
        startWaveButton.GetComponent<HudButtonFeedback>().SetSprites(
            Art("v2/action_button"), Art("v2/action_button"), Art("v2/action_button"));
        autoWaveButton = MakeTopWaveButton(root.transform, "Auto: desligado", "v2/action_button", "v2/icon_auto",
            new Vector2(.785f,.765f), new Vector2(.985f,.845f), () => {
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
        buildPauseIcon.sprite = Art(GameManager.IsBuildPaused ? "v2/icon_play" : "v2/icon_pause");
        buildPauseButton.GetComponent<HudButtonFeedback>().SetToggleState(GameManager.IsBuildPaused);
        if (waveSpawner != null)
        {
            startWaveText.text = waveSpawner.IsWaveActive ? "Onda em curso" :
                waveSpawner.CurrentWave >= waveSpawner.TotalWaves ? "Última onda" :
                $"Iniciar onda {waveSpawner.CurrentWave + 1}/{waveSpawner.TotalWaves}";
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
                new Vector2(.28f,.16f), new Vector2(.72f,.84f), true);
            if (iconImage.sprite == null)
            {
                iconImage.gameObject.SetActive(false);
                var fallback = Label(holder, caption, new Vector2(.05f,.08f), new Vector2(.95f,.92f), 32);
                fallback.resizeTextForBestFit = true;
            }
        }
        else if (!string.IsNullOrEmpty(caption))
        {
            var label = Label(holder, caption, new Vector2(.05f,.08f), new Vector2(.95f,.92f), 32);
            label.resizeTextForBestFit = true;
        }
        button.onClick.AddListener(PlayClick);
        button.onClick.AddListener(callback);
        return button;
    }
    Text Counter(Transform parent, string icon, float left, float right)
    {
        var badge = Rect(parent, "Indicador " + icon, new Vector2(left,.875f), new Vector2(right,.935f));
        string art = icon == "vida" ? "v2/icon_life" : icon == "moedas" ? "v2/icon_coins" : "v2/icon_rounds";
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
        Tower tower = plot.Placed;
        var actions = new List<Action>();
        if (tower.CanUpgrade)
        {
            int cost=plot.GetNextUpgradeCost();
            actions.Add(new Action("Melhorar","melhorar","melhorar",()=>
            { if (plot.Upgrade()) ShowTowerActions(plot); },cost,$"Custo: {cost} moedas"));
        }
        else actions.Add(new Action("Melhorar","melhorar","melhorar",()=> { },-1,"Nível máximo",false));
        actions.Add(new Action("Vender","vender","vender",()=> { if(plot.Sell()) current.Close(); },-1,$"Receber: {plot.GetSellRefund()} moedas"));
        actions.Add(new Action(tower.mode == TargetMode.Strongest ? "Alvo: Mais vida" : "Alvo: Primeiro",
            "construir", "alcance", () => {
                tower.mode = tower.mode == TargetMode.Strongest ? TargetMode.First : TargetMode.Strongest;
                ShowTowerActions(plot);
            },-1,"Alternar prioridade"));
        string targetMode = tower.mode == TargetMode.Strongest ? "mais vida" : "mais avançado no caminho";
        current.ShowWithLayout(tower.data.displayName.ToUpperInvariant(),$"Nível {tower.Level+1} · Alvo: {targetMode}\n"+(tower.data.hitsFlying?"Atinge inimigos aéreos e terrestres.":"Atinge apenas inimigos terrestres."),true,true,actions.ToArray());
        current.ShowSelectedRange(plot, tower.Stats.range);
        current.Stat(0,"dano",$"Dano\n{tower.Stats.damage:0.#}");
        current.Stat(1,"alcance",$"Alcance\n{tower.Stats.range:0.#}");
        current.Stat(2,"cadencia",$"Tiros/s\n{tower.Stats.fireRate:0.#}");
        current.Stat(3,"aereos",tower.data.hitsFlying?"Aéreos\nSIM":"Aéreos\nNÃO");
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
        Show(result.won?"COLHEITA PROTEGIDA!":"A COLHEITA PRECISA DE VOCÊ",$"Pontuação: {result.score} · Rodada: {result.round}\nInimigos derrotados: {result.enemiesDefeated}/{result.enemiesTotal}\nMaior dano: {result.topDamageTower} ({result.topDamage:0})",false,
            new Action("Jogar novamente","recomecar","continuar",()=>GameManager.Instance?.RestartCurrentLevel()),
            new Action("Voltar ao menu","menu","voltar",ToMenu));
    }
    void UpdateMoney(int value)
    {
        coins.text=value.ToString();
        foreach(var p in purchases) if(p.button!=null) p.button.interactable=value>=p.cost;
    }
    void UpdateLife(int value) => life.text=$"{value}/{GameManager.StartingLife}";
    void UpdateWave(int value,int total) => waves.text=total>0?$"{value}/{total}":value.ToString();
    void UpdateComposition(int value,int total,bool flying) { UpdateWave(value,total); threat.text=flying?"AÉREOS NESTA RODADA · prepare sua defesa":""; }
}
