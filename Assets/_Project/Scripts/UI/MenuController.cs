using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// Menu estatico montado com os sprites finais do pacote de arte.
public class MenuController : MonoBehaviour
{
    [Header("Fundo e letreiro")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Sprite logoSprite;
    [Header("Sprites dos botões")]
    [SerializeField] Sprite playNormalSprite;
    [SerializeField] Sprite playSelectedSprite;
    [Header("Indicadores de dificuldade")]
    [SerializeField] Sprite difficultyEasyBadge;
    [SerializeField] Sprite difficultyEasyBadgeSelected;
    [SerializeField] Sprite difficultyNormalBadge;
    [SerializeField] Sprite difficultyNormalBadgeSelected;
    [SerializeField] Sprite difficultyHardBadge;
    [SerializeField] Sprite difficultyHardBadgeSelected;
    [Header("Arte dos submenus")]
    [SerializeField] Sprite submenuPanelSprite;
    [SerializeField] Sprite backButtonSprite;
    [SerializeField] Sprite backButtonSelectedSprite;
    [SerializeField] Sprite repositoryButtonSprite;
    [SerializeField] Sprite rankingNormalSprite;
    [SerializeField] Sprite rankingSelectedSprite;
    [SerializeField] Sprite infoNormalSprite;
    [SerializeField] Sprite infoSelectedSprite;
    [SerializeField] Sprite exitNormalSprite;
    [SerializeField] Sprite exitSelectedSprite;
    [Header("Áudio da interface (sem música)")]
    [SerializeField] AudioClip hoverSound;
    [SerializeField] AudioClip clickSound;
    [SerializeField] string firstLevel = "Nivel_1_Primavera";

    Transform menuRoot;
    GameObject mainMenuContent;
    GameObject difficultyMenu;
    string selectedLevel;
    bool loading;
    GameDifficulty difficulty = GameDifficulty.Easy;

    void Start()
    {
        // Sons leves gerados em runtime até a equipe adicionar os efeitos finais.
        if (hoverSound == null) hoverSound = CreateUiTone("Menu Hover", 0.11f, 760f, 1120f, 0.22f);
        if (clickSound == null) clickSound = CreateUiTone("Menu Click", 0.10f, 260f, 520f, 0.30f);
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvasGo = new GameObject("Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var root = canvasGo.transform;
        menuRoot = root;
        mainMenuContent = new GameObject("Conteúdo do menu principal", typeof(RectTransform));
        mainMenuContent.transform.SetParent(root, false);
        var contentRect = mainMenuContent.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
        InfoPanel.Configure(submenuPanelSprite, backButtonSprite, backButtonSelectedSprite, repositoryButtonSprite, mainMenuContent);

        if (backgroundSprite != null) AddImage(root, "Fundo da fazenda", backgroundSprite, Vector2.zero, Vector2.one, Color.white, true);
        else AddImage(root, "Fundo de reserva", null, Vector2.zero, Vector2.one, new Color32(115, 183, 205, 255), false);

        // O novo logo já contém o nome do jogo; a sombra adicional foi removida.
        AddImage(mainMenuContent.transform, "Letreiro Defesa da Colheita", logoSprite, new Vector2(.28f, .72f), new Vector2(.72f, .99f), Color.white, true);

        CreateSpriteButton(mainMenuContent.transform, "Jogar", playNormalSprite, playSelectedSprite,
            new Vector2(.30f, .53f), new Vector2(.70f, .65f), StartGame);

        CreateSpriteButton(mainMenuContent.transform, "Ranking", rankingNormalSprite, rankingSelectedSprite,
            new Vector2(.30f, .36f), new Vector2(.70f, .48f), ShowRanking);
        CreateSpriteButton(mainMenuContent.transform, "Informações", infoNormalSprite, infoSelectedSprite,
            new Vector2(.30f, .19f), new Vector2(.70f, .31f), ShowInfo);
        CreateSpriteButton(mainMenuContent.transform, "Sair", exitNormalSprite, exitSelectedSprite,
            new Vector2(.30f, .02f), new Vector2(.70f, .14f), ExitGame);
        var bonus = CreateSpriteButton(mainMenuContent.transform, "A Prova Final",
            Resources.Load<Sprite>("hud/botao_normal"), Resources.Load<Sprite>("hud/botao_selecionado"),
            new Vector2(.73f,.20f), new Vector2(.96f,.30f), () => {
                if (!CampaignProgress.BonusUnlocked || loading) return;
                selectedLevel = CampaignProgress.BonusScene;
                OpenDifficultyMenu();
            });
        bonus.GetComponent<Button>().interactable = CampaignProgress.BonusUnlocked;
        bonus.color = CampaignProgress.BonusUnlocked ? Color.white : new Color(.6f,.6f,.6f);
        var bonusTitle = MakeText(bonus.transform, "A Prova Final", new Vector2(.08f,.1f), new Vector2(.92f,.9f),28,Color.white);
        bonusTitle.raycastTarget = false;
        MakeText(mainMenuContent.transform, CampaignProgress.BonusUnlocked ? "Bônus desbloqueado · campus à noite" : "Vença o Outono para desbloquear",
            new Vector2(.72f,.13f),new Vector2(.97f,.20f),20,Color.white).raycastTarget = false;
        var bonusRanking = MakeText(mainMenuContent.transform, "Ranking do bônus",new Vector2(.73f,.07f),new Vector2(.96f,.13f),24,Color.white);
        var bonusRankingButton = bonusRanking.gameObject.AddComponent<Button>();
        bonusRankingButton.targetGraphic = bonusRanking;
        bonusRankingButton.onClick.AddListener(() => ShowRankingFor(true));
        // O fundo é filho direto do Canvas e deve ficar atrás do conteúdo principal.
        mainMenuContent.transform.SetAsLastSibling();
    }

    static AudioClip CreateUiTone(string clipName, float duration, float startFrequency, float endFrequency, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = t / duration;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            float envelope = Mathf.Sin(Mathf.PI * progress) * Mathf.Exp(-progress * 1.3f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }
        var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void StartGame()
    {
        if (loading) return;
        selectedLevel = firstLevel;
        // Jogar sempre exige confirmar uma dificuldade antes de carregar a fase.
        OpenDifficultyMenu();
    }

    IEnumerator LoadGameAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.12f);
        SceneManager.LoadScene(selectedLevel);
    }

    void ExitGame() => StartCoroutine(ExitAfterClick());

    IEnumerator ExitAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.12f);
        QuitApplication();
    }

    static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    Image CreateSpriteButton(Transform root, string name, Sprite normal, Sprite selected,
        Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        var image = AddImage(root, "Botão " + name, normal, min, max, Color.white, true);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        var visualState = image.gameObject.AddComponent<MenuButtonVisualState>();
        visualState.Initialize(image, normal, selected != null ? selected : normal);
        var audio = image.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0;
        if (clickSound != null) button.onClick.AddListener(() => audio.PlayOneShot(clickSound));
        button.onClick.AddListener(action);
        var hover = image.gameObject.AddComponent<MenuButtonHoverSound>();
        hover.Initialize(audio, hoverSound);
        return image;
    }

    static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, Color color, bool preserveAspect)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite; image.color = color; image.preserveAspect = preserveAspect; image.raycastTarget = false;
        return image;
    }

    static Text MakeText(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color)
    {
        var go = new GameObject("Texto " + value, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>(); text.text = value; UiTypography.Apply(text,size);
        text.color = color; text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    GameObject CreateSubmenuPanel(string title, UnityEngine.Events.UnityAction backAction)
    {
        if (mainMenuContent != null) mainMenuContent.SetActive(false);
        var panelObject = new GameObject("Submenu " + title, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(menuRoot, false);
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.11f, .035f); panelRect.anchorMax = new Vector2(.89f, .965f);
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        var panelImage = panelObject.GetComponent<Image>();
        panelImage.sprite = submenuPanelSprite;
        panelImage.color = submenuPanelSprite != null ? Color.white : new Color32(111, 68, 41, 248);
        panelImage.preserveAspect = true;
        panelImage.raycastTarget = true;

        var heading = MakeText(panelObject.transform, title.ToUpperInvariant(), new Vector2(.30f, .79f), new Vector2(.70f, .875f), 48,
            new Color32(255, 248, 215, 255));
        InfoPanel.StyleSubmenuHeading(heading);

        if (backButtonSprite != null)
        {
            CreateSpriteButton(panelObject.transform, "Voltar", backButtonSprite, backButtonSelectedSprite,
                new Vector2(.67f, .055f), new Vector2(.88f, .15f), backAction);
        }
        else
        {
            var backText = MakeText(panelObject.transform, "VOLTAR", new Vector2(.67f, .055f), new Vector2(.88f, .15f), 24,
                new Color32(255, 237, 190, 255));
            backText.raycastTarget = true;
            var backButton = backText.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backText;
            backButton.transition = Selectable.Transition.None;
            backButton.onClick.AddListener(backAction);
        }
        return panelObject;
    }

    void OpenDifficultyMenu()
    {
        if (difficultyMenu != null) return;
        difficultyMenu = CreateSubmenuPanel("Dificuldade", CloseDifficultyMenu);
        CreateSpriteButton(difficultyMenu.transform, "Fácil", difficultyEasyBadge, difficultyEasyBadgeSelected,
            new Vector2(.35f, .65f), new Vector2(.65f, .77f), () => SelectDifficulty(GameDifficulty.Easy));
        CreateSpriteButton(difficultyMenu.transform, "Normal", difficultyNormalBadge, difficultyNormalBadgeSelected,
            new Vector2(.35f, .43f), new Vector2(.65f, .55f), () => SelectDifficulty(GameDifficulty.Normal));
        CreateSpriteButton(difficultyMenu.transform, "Difícil", difficultyHardBadge, difficultyHardBadgeSelected,
            new Vector2(.35f, .21f), new Vector2(.65f, .33f), () => SelectDifficulty(GameDifficulty.Hard));
    }

    void SelectDifficulty(GameDifficulty selectedDifficulty)
    {
        if (loading) return;
        if (selectedLevel == CampaignProgress.BonusScene && !CampaignProgress.BonusUnlocked) return;
        if (!Application.CanStreamedLevelBeLoaded(selectedLevel))
        {
            CloseDifficultyMenu();
            InfoPanel.Show("Fase indisponível. No editor, use Defesa da Colheita > Gerar niveis antes de jogar ou gerar a build.", title: "Fase indisponível");
            return;
        }
        loading = true;
        difficulty = selectedDifficulty;
        GameManager.SetDifficulty(difficulty);
        CloseDifficultyMenu();
        if (GameManager.Instance != null) GameManager.Instance.ResetGame();
        StartCoroutine(LoadGameAfterClick());
    }

    void CloseDifficultyMenu()
    {
        if (difficultyMenu == null) return;
        Destroy(difficultyMenu);
        difficultyMenu = null;
        if (mainMenuContent != null) mainMenuContent.SetActive(true);
    }

    void ShowInfo() => InfoPanel.Show(
        "Projeto desenvolvido como trabalho final da disciplina de Computação Gráfica.",
        "https://github.com/RafaelMaurina/Tower-Defense",
        new[] { "Rafael (@RafaelMaurina), arte e áudio", "Fabio (@fabioov), combate", "João (@Joao-Althaus), jogo e interface" },
        "Informações");

    void ShowRanking() => ShowRankingFor(false);

    void ShowRankingFor(bool bonus)
    {
        var entries = LocalRanking.GetEntries(bonus);
        string message = bonus ? "BÔNUS · A PROVA FINAL" : "RANKING DA CAMPANHA";
        for (int i = 0; i < entries.Count; i++) message += $"\n{i + 1}. {entries[i].score} pontos, {entries[i].difficulty}, {entries[i].date}";
        if (entries.Count == 0) message += "\nAinda não há partidas concluídas.";
        InfoPanel.Show(message, title: "Ranking");
    }

    public static void LoadMenu() => SceneManager.LoadScene("Menu");
}

public class MenuButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    AudioSource source;
    AudioClip clip;

    public void Initialize(AudioSource audioSource, AudioClip hoverClip)
    {
        source = audioSource;
        clip = hoverClip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }
}

public class MenuButtonVisualState : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Image image;
    Sprite normalSprite;
    Sprite highlightedSprite;
    bool pointerInside;
    bool pointerDown;

    public void Initialize(Image targetImage, Sprite normal, Sprite highlighted)
    {
        image = targetImage;
        normalSprite = normal;
        highlightedSprite = highlighted;
    }

    void OnEnable()
    {
        pointerInside = false;
        pointerDown = false;
        RefreshSprite();
    }

    void OnDisable()
    {
        pointerInside = false;
        pointerDown = false;
        RefreshSprite();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        RefreshSprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshSprite();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        RefreshSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
        pointerInside = RectTransformUtility.RectangleContainsScreenPoint(
            image.rectTransform, eventData.position, eventData.enterEventCamera);
        RefreshSprite();
    }

    void RefreshSprite()
    {
        if (image != null) image.sprite = pointerInside || pointerDown ? highlightedSprite : normalSprite;
    }
}

public class InfoPanel : MonoBehaviour
{
    static Sprite panelSprite;
    static Sprite backSprite;
    static Sprite backSelectedSprite;
    static Sprite repositorySprite;
    static GameObject mainMenuContent;

    public static void Configure(Sprite submenuPanel, Sprite backButton, Sprite backButtonSelected, Sprite repositoryButton, GameObject content)
    {
        panelSprite = submenuPanel;
        backSprite = backButton;
        backSelectedSprite = backButtonSelected;
        repositorySprite = repositoryButton;
        mainMenuContent = content;
    }

    public static void Show(string message, string repositoryUrl = null, string[] creditLines = null, string title = "Informações")
    {
        var canvas = Object.FindAnyObjectByType<Canvas>(); if (canvas == null) return;
        if (mainMenuContent != null) mainMenuContent.SetActive(false);
        var go = new GameObject("Submenu " + title, typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.11f, .035f); rect.anchorMax = new Vector2(.89f, .965f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var panel = go.GetComponent<Image>(); panel.sprite = panelSprite; panel.preserveAspect = true; panel.raycastTarget = true;
        panel.color = panelSprite != null ? Color.white : new Color32(111, 68, 41, 248);

        var heading = MakeText(go.transform, title.ToUpperInvariant(), new Vector2(.30f, .79f), new Vector2(.70f, .875f), 48,
            new Color32(255, 248, 215, 255));
        StyleSubmenuHeading(heading);

        if (repositoryUrl != null)
        {
            MakeText(go.transform, message, new Vector2(.19f, .66f), new Vector2(.81f, .76f), 40,
                new Color32(255, 237, 190, 255)).raycastTarget = false;
            if (creditLines != null)
            {
                float[] rowBottoms = { .545f, .435f, .325f };
                for (int i = 0; i < Mathf.Min(creditLines.Length, rowBottoms.Length); i++)
                {
                    float bottom = rowBottoms[i];
                    var credit = MakeText(go.transform, creditLines[i], new Vector2(.22f, bottom), new Vector2(.78f, bottom + .08f), 38,
                        new Color32(255, 237, 190, 255));
                    credit.fontStyle = FontStyle.Bold;
                    credit.raycastTarget = false;
                }
            }
            if (repositorySprite != null)
                AddImageActionButton(go.transform, repositorySprite, new Vector2(.20f, .20f), new Vector2(.57f, .30f),
                    () => Application.OpenURL(repositoryUrl));
            else
                AddActionButton(go.transform, "GitHub do projeto", new Vector2(.20f, .20f), new Vector2(.57f, .30f),
                    new Color32(67, 105, 56, 245), () => Application.OpenURL(repositoryUrl));
        }
        else
        {
            var text = MakeText(go.transform, message, new Vector2(.18f, .20f), new Vector2(.82f, .76f), 40,
                new Color32(255, 237, 190, 255));
            text.raycastTarget = false;
        }

        AddBackButton(go.transform, () => { Object.Destroy(go); if (mainMenuContent != null) mainMenuContent.SetActive(true); });
    }

    static void AddBackButton(Transform parent, UnityEngine.Events.UnityAction action)
    {
        if (backSprite != null)
        {
            var buttonObject = new GameObject("Voltar", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.59f, .20f); rect.anchorMax = new Vector2(.85f, .30f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var image = buttonObject.GetComponent<Image>(); image.sprite = backSprite; image.preserveAspect = true; image.raycastTarget = true;
            var button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.transition = Selectable.Transition.None;
            var visualState = buttonObject.AddComponent<MenuButtonVisualState>();
            visualState.Initialize(image, backSprite, backSelectedSprite != null ? backSelectedSprite : backSprite);
            button.onClick.AddListener(action);
            return;
        }
        var text = MakeText(parent, "VOLTAR", new Vector2(.59f, .20f), new Vector2(.85f, .30f), 30,
            new Color32(255, 237, 190, 255)); text.raycastTarget = true;
        var fallbackButton = text.gameObject.AddComponent<Button>(); fallbackButton.targetGraphic = text;
        fallbackButton.transition = Selectable.Transition.None;
        fallbackButton.onClick.AddListener(action);
    }

    public static void StyleSubmenuHeading(Text heading)
    {
        UiTypography.Apply(heading,heading.fontSize);
        heading.raycastTarget = false;
        var outline = heading.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(68, 35, 19, 255);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    static Text MakeText(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color)
    {
        var go = new GameObject("Texto " + value, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>(); text.text = value; UiTypography.Apply(text,size);
        text.color = color; text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    static void AddActionButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = buttonObject.GetComponent<Image>(); image.color = color;
        var button = buttonObject.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var text = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(buttonObject.transform, false);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(4, 2); text.rectTransform.offsetMax = new Vector2(-4, -2);
        text.text = label; UiTypography.Apply(text,40);
        text.fontStyle = FontStyle.Bold; text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(255, 237, 190, 255); text.raycastTarget = false;
    }

    static void AddImageActionButton(Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax,
        UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject("GitHub do projeto", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var image = buttonObject.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = true;
        var button = buttonObject.GetComponent<Button>(); button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors; colors.highlightedColor = new Color32(255, 242, 193, 255);
        colors.pressedColor = new Color32(220, 204, 166, 255); button.colors = colors;
        button.onClick.AddListener(action);
    }
}







/// Padrao compartilhado pelos textos editaveis do menu, HUD e submenus.
public static class UiTypography
{
    static Font sharedFont;

    public static void Apply(Text text, int size)
    {
        if (sharedFont == null) sharedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = sharedFont;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = size;
        text.lineSpacing = 1f;
        text.supportRichText = true;
    }
}
