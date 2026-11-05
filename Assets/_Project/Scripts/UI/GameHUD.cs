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
        public Action(string text, string art, string icon, UnityEngine.Events.UnityAction callback, int cost = -1, string detail = null)
        { this.text = text; this.art = art; this.icon = icon; this.callback = callback; this.cost = cost; this.detail = detail; }
    }
    sealed class Purchase { public Button button; public int cost; }
    static GameHUD current;
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    readonly List<Purchase> purchases = new List<Purchase>();
    Text coins, life, waves, threat, heading, description;
    RectTransform rows, stats;
    BuildPlot selectedPlot;
    TowerData[] catalog;
    GameObject catalogView;
    GameObject overlay;
    Button pause;
    Image panelImage;
    bool towerPanel;
    AudioSource uiAudio;
    AudioClip hoverSound, clickSound;


    Sprite Art(string name)
    {
        if (!sprites.TryGetValue(name, out Sprite sprite))
        { sprite = Resources.Load<Sprite>("hud/" + name); sprites[name] = sprite; }
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
    void Start() { UpdateMoney(GameManager.Money); UpdateLife(GameManager.BaseLife); UpdateWave(GameManager.StateRound, 0); }
    void OnDisable()
    {
        GameManager.OnMoneyChanged -= UpdateMoney;
        GameManager.OnBaseLifeChanged -= UpdateLife;
        GameManager.OnWaveChanged -= UpdateWave;
        GameManager.OnWaveCompositionChanged -= UpdateComposition;
        GameManager.OnStateChanged -= StateChanged;
        GameManager.OnGameEnded -= Result;
        if (current == this) current = null;
    }
    void Update()
    {
        if (Keyboard.current == null) return;
        if (overlay.activeSelf && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame))
        {
            foreach (Button option in rows.GetComponentsInChildren<Button>())
                if (option.interactable) { EventSystem.current.SetSelectedGameObject(option.gameObject); break; }
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (towerPanel && overlay.activeSelf) Back();
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
    static Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size)
    {
        var t = Rect(parent, text, min, max).gameObject.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = new Color32(255, 239, 196, 255);
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        var shadow = t.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color32(49, 25, 12, 240);
        shadow.effectDistance = new Vector2(1, -2); return t;
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
            if (!string.IsNullOrEmpty(action.icon)) Picture(image.transform, action.icon, "icone_" + action.icon, new Vector2(.06f,.23f), new Vector2(.20f,.77f), true);
            var caption = Label(image.transform, action.text, new Vector2(.22f,.15f), new Vector2(.92f,.85f), 32);
            caption.resizeTextForBestFit = true; caption.resizeTextMinSize = 22; caption.resizeTextMaxSize = 32;
        }
        if (!string.IsNullOrEmpty(action.detail))
            Label(holder, action.detail, Vector2.zero, new Vector2(1,.27f), 26);
        b.interactable = action.cost < 0 || GameManager.Money >= action.cost;
        b.onClick.AddListener(PlayClick);
        b.onClick.AddListener(action.callback);
        if (action.cost >= 0) purchases.Add(new Purchase { button = b, cost = action.cost });
        return b;
    }
    void CreateCanvas()
    {
        var root = new GameObject("HUD da Colheita", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
        var bar = Picture(root.transform, "Recursos da fazenda", "moldura_status", new Vector2(.18f,.88f), new Vector2(.82f,.995f));
        coins = Counter(bar.transform, "MOEDAS", "moedas", .03f, .24f);
        life = Counter(bar.transform, "VIDA", "vida", .27f, .48f);
        waves = Counter(bar.transform, "RODADA", "rodadas", .51f, .72f);
        var pauseArea = Rect(bar.transform,"Pausar",new Vector2(.85f,.22f),new Vector2(.94f,.78f));
        var hit = pauseArea.gameObject.AddComponent<Image>(); hit.color = Color.clear;
        pause = pauseArea.gameObject.AddComponent<Button>(); pause.targetGraphic = hit; pause.transition = Selectable.Transition.None;
        var leftBar = Rect(pauseArea,"Barra esquerda",new Vector2(.28f,.12f),new Vector2(.43f,.88f)).gameObject.AddComponent<Image>();
        var rightBar = Rect(pauseArea,"Barra direita",new Vector2(.57f,.12f),new Vector2(.72f,.88f)).gameObject.AddComponent<Image>();
        leftBar.raycastTarget = rightBar.raycastTarget = false;
        var pauseFeedback = pauseArea.gameObject.AddComponent<HudButtonFeedback>();
        pauseFeedback.ConfigureBars(leftBar,rightBar);
        pauseFeedback.ConfigureSound(uiAudio, hoverSound);
        pause.onClick.AddListener(PlayClick);
        pause.onClick.AddListener(()=>GameManager.Instance?.TogglePause());
        threat = Label(root.transform, "", new Vector2(.38f,.83f), new Vector2(.62f,.88f), 23);
        threat.color = new Color32(255,222,126,255);

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
    Text Counter(Transform parent, string title, string icon, float left, float right)
    {
        Picture(parent, icon, "icone_"+icon, new Vector2(left,.24f), new Vector2(left+.055f,.76f), true);
        Label(parent,title,new Vector2(left+.06f,.57f),new Vector2(right,.81f),22);
        return Label(parent,"0",new Vector2(left+.06f,.20f),new Vector2(right,.59f),36);
    }
    void Clear(Transform parent)
    {
        foreach (Transform child in parent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
    }
    void Show(string title, string body, bool isTower, params Action[] actions)
    {
        rows.gameObject.SetActive(true);
        stats.gameObject.SetActive(true);
        if (catalogView != null) catalogView.SetActive(false);
        Clear(rows); Clear(stats); purchases.Clear(); towerPanel = isTower;
        heading.text = title; description.text = body; overlay.SetActive(true);
        description.rectTransform.anchorMin = new Vector2(.12f,.64f);
        bool paused = GameManager.State == GameState.Paused && !isTower;
        panelImage.sprite = Art(paused ? "moldura_pausa_titulo" : "moldura_pausa");
        heading.gameObject.SetActive(!paused);
        rows.anchorMin = new Vector2(isTower ? .16f : .24f, .19f);
        rows.anchorMax = new Vector2(isTower ? .84f : .76f, isTower ? .53f : .60f);
        int n = Mathf.Max(1,actions.Length);
        for (int i=0;i<actions.Length;i++)
            MakeButton(rows,actions[i],new Vector2(0,1f-(i+1f)/n+.015f),new Vector2(1,1f-i/(float)n-.015f));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    void Close()
    {
        overlay.SetActive(false);
        if (catalogView != null) catalogView.SetActive(false);
        towerPanel = false;
        selectedPlot = null;
        purchases.Clear();
    }
    void Back()
    {
        if (GameManager.State == GameState.Paused) GameManager.Instance?.TogglePause();
        else if (GameManager.State == GameState.Victory || GameManager.State == GameState.Defeat) ToMenu();
        else if (catalogView != null && catalogView.activeSelf) Close();
        else if (selectedPlot != null && selectedPlot.Placed == null) ShowTowerChoices(selectedPlot, catalog);
        else Close();
    }
    void ToMenu()
    {
        Time.timeScale = 1;
        GameManager.Instance?.SetState(GameState.Menu);
        MenuController.LoadMenu();
    }
    public static void ShowTowerChoices(BuildPlot plot, TowerData[] choices)
    {
        if (current == null || plot == null) return;
        current.selectedPlot = plot;
        current.catalog = choices;
        current.ShowCatalog();
    }
    void ShowCatalog()
    {
        if (catalogView != null) { catalogView.SetActive(false); Destroy(catalogView); catalogView = null; }
        Show("ESCOLHA UMA TORRE", "Ordenadas pelo custo inicial. Selecione uma placa para ver os atributos.", true);
        description.rectTransform.anchorMin = new Vector2(.13f,.70f);
        rows.gameObject.SetActive(false);
        stats.gameObject.SetActive(false);
        catalogView = Rect(panelImage.transform,"Lista de torres",new Vector2(.12f,.16f),new Vector2(.88f,.70f)).gameObject;
        var viewport = Rect(catalogView.transform,"Area visivel",Vector2.zero,Vector2.one);
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.white;   // Mask usa o alfa do grafico; com .001 o Unity arredonda para 0 e esconde a lista (showMaskGraphic=false ja o deixa invisivel)
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
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
        Stat(0,"dano",$"Dano\n{level.damage:0.#}");
        Stat(1,"alcance",$"Alcance\n{level.range:0.#}");
        Stat(2,"cadencia",$"Tiros/s\n{level.fireRate:0.#}");
        Stat(3,"aereos",data.hitsFlying?"Aéreos\nSIM":"Aéreos\nNÃO");
    }
    public static void ShowTowerActions(BuildPlot plot)
    {
        if (current == null || plot == null || plot.Placed == null) return;

        current.selectedPlot = null;
        Tower tower = plot.Placed;
        var actions = new List<Action>();
        if (tower.CanUpgrade)
        {
            int cost=plot.GetNextUpgradeCost();
            actions.Add(new Action("Melhorar","melhorar","melhorar",()=>
            { if (plot.Upgrade()) ShowTowerActions(plot); },cost,$"Custo: {cost} moedas"));
        }
        actions.Add(new Action("Vender","vender","vender",()=> { if(plot.Sell()) current.Close(); },-1,$"Receber: {plot.GetSellRefund()} moedas"));
        current.Show(tower.data.displayName.ToUpperInvariant(),$"Nível {tower.Level+1} · Alvo: mais avançado no caminho\n"+(tower.data.hitsFlying?"Atinge inimigos aéreos e terrestres.":"Atinge apenas inimigos terrestres."),true,actions.ToArray());
        current.Stat(0,"dano",$"Dano\n{tower.Stats.damage:0.#}");
        current.Stat(1,"alcance",$"Alcance\n{tower.Stats.range:0.#}");
        current.Stat(2,"cadencia",$"Tiros/s\n{tower.Stats.fireRate:0.#}");
        current.Stat(3,"aereos",tower.data.hitsFlying?"Aéreos\nSIM":"Aéreos\nNÃO");
    }
    void Stat(int column, string icon, string text)
    {
        float x=column*.25f;
        Picture(stats,icon,"icone_"+icon,new Vector2(x,.2f),new Vector2(x+.07f,.8f),true);
        Label(stats,text,new Vector2(x+.075f,0),new Vector2(x+.245f,1),18);
    }
    void StateChanged(GameState state)
    {
        pause.interactable = state == GameState.Playing || state == GameState.Paused;
        if (state == GameState.Paused)
            Show("JOGO PAUSADO","A colheita pode esperar um pouquinho.\nRetome a defesa quando estiver pronto.",false,
                new Action("Continuar","continuar","continuar",()=>GameManager.Instance?.TogglePause()),
                new Action("Recomeçar nível","recomecar","voltar",()=>GameManager.Instance?.RestartCurrentLevel()),
                new Action("Voltar ao menu","menu","voltar",ToMenu));
        else if (state == GameState.Playing) Close();
    }
    void Result(GameResult result)
    {
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
