using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

/// Menu estatico montado com os sprites finais do pacote de arte.
public class MenuController : MonoBehaviour
{
    static TMP_FontAsset rankingFontAsset;
    [Header("Fundo e letreiro")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] Sprite logoSprite;
    [Header("Sprites dos botões")]
    [SerializeField] Sprite playNormalSprite;
    [SerializeField] Sprite playSelectedSprite;
    [Header("Seleção de fases")]
    [SerializeField] Sprite phaseMainFrameSprite;
    [SerializeField] Sprite summerCardFrameSprite;
    [SerializeField] Sprite springCardFrameSprite;
    [SerializeField] Sprite autumnCardFrameSprite;
    [SerializeField] Sprite bonusCardFrameSprite;
    [SerializeField] Sprite phaseSelectionOutlineSprite;
    [SerializeField] Sprite summerPreviewSprite;
    [SerializeField] Sprite springPreviewSprite;
    [SerializeField] Sprite autumnPreviewSprite;
    [SerializeField] Sprite bonusPreviewSprite;
    [SerializeField] Sprite bonusLockedPreviewSprite;
    [SerializeField] Sprite bonusLockSprite;
    bool springPreviewIsProvisional;
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
    [Header("Arte do ranking")]
    [SerializeField] Sprite rankingFrameSprite;
    [SerializeField] Sprite rankingHeaderSprite;
    [SerializeField] Sprite rankingRowSprite;
    [SerializeField] Sprite rankingFirstSprite;
    [SerializeField] Sprite rankingSecondSprite;
    [SerializeField] Sprite rankingThirdSprite;
    [SerializeField] Sprite rankingGoldMedalSprite;
    [SerializeField] Sprite rankingSilverMedalSprite;
    [SerializeField] Sprite rankingBronzeMedalSprite;
    [Header("Áudio da interface (sem música)")]
    [SerializeField] AudioClip hoverSound;
    [SerializeField] AudioClip clickSound;

    Transform menuRoot;
    GameObject mainMenuContent;
    GameObject difficultyMenu;
    GameObject phaseSelectionMenu;
    readonly List<PhaseCard> phaseCards = new List<PhaseCard>();
    PhaseCard selectedPhaseCard;
    string selectedLevel;
    bool loading;
    GameDifficulty difficulty = GameDifficulty.Easy;

    class PhaseCard
    {
        public string scene;
        public Button button;
        public Image outline;
    }

    void Start()
    {
        LoadPhaseArtFromResources();
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
        // O fundo é filho direto do Canvas e deve ficar atrás do conteúdo principal.
        mainMenuContent.transform.SetAsLastSibling();
    }

    void LoadPhaseArtFromResources()
    {
        if (phaseMainFrameSprite == null) phaseMainFrameSprite = Resources.Load<Sprite>("MenuPhase/selecao_moldura_principal");
        if (summerCardFrameSprite == null) summerCardFrameSprite = Resources.Load<Sprite>("MenuPhase/fase_verao_moldura");
        if (springCardFrameSprite == null) springCardFrameSprite = Resources.Load<Sprite>("MenuPhase/fase_primavera_moldura");
        if (autumnCardFrameSprite == null) autumnCardFrameSprite = Resources.Load<Sprite>("MenuPhase/fase_outono_moldura");
        if (bonusCardFrameSprite == null) bonusCardFrameSprite = Resources.Load<Sprite>("MenuPhase/fase_bonus_moldura");
        if (phaseSelectionOutlineSprite == null) phaseSelectionOutlineSprite = Resources.Load<Sprite>("MenuPhase/fase_contorno_selecionado");
        if (summerPreviewSprite == null) summerPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_verao_preview");
        if (springPreviewSprite == null)
        {
            springPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_primavera_preview");
            if (springPreviewSprite == null)
            {
                springPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_primavera_preview_provisoria");
                springPreviewIsProvisional = springPreviewSprite != null;
            }
        }
        if (autumnPreviewSprite == null) autumnPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_outono_preview");
        if (bonusPreviewSprite == null) bonusPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_bonus_preview");
        if (bonusLockedPreviewSprite == null) bonusLockedPreviewSprite = Resources.Load<Sprite>("MenuPhase/fase_bonus_preview_bloqueada");
        if (bonusLockSprite == null) bonusLockSprite = Resources.Load<Sprite>("MenuPhase/fase_cadeado");
        if (rankingFrameSprite == null) rankingFrameSprite = Resources.Load<Sprite>("Ranking/ranking_moldura_principal");
        if (rankingHeaderSprite == null) rankingHeaderSprite = Resources.Load<Sprite>("Ranking/ranking_cabecalho");
        if (rankingRowSprite == null) rankingRowSprite = Resources.Load<Sprite>("Ranking/ranking_linha_padrao");
        if (rankingFirstSprite == null) rankingFirstSprite = Resources.Load<Sprite>("Ranking/ranking_linha_ouro");
        if (rankingSecondSprite == null) rankingSecondSprite = Resources.Load<Sprite>("Ranking/ranking_linha_prata");
        if (rankingThirdSprite == null) rankingThirdSprite = Resources.Load<Sprite>("Ranking/ranking_linha_bronze");
        if (rankingGoldMedalSprite == null) rankingGoldMedalSprite = Resources.Load<Sprite>("Ranking/ranking_medalha_ouro");
        if (rankingSilverMedalSprite == null) rankingSilverMedalSprite = Resources.Load<Sprite>("Ranking/ranking_medalha_prata");
        if (rankingBronzeMedalSprite == null) rankingBronzeMedalSprite = Resources.Load<Sprite>("Ranking/ranking_medalha_bronze");
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
        OpenPhaseSelection();
    }

    void OpenPhaseSelection()
    {
        if (phaseSelectionMenu != null) return;
        if (mainMenuContent != null) mainMenuContent.SetActive(false);

        phaseSelectionMenu = new GameObject("Seleção de fases", typeof(RectTransform));
        phaseSelectionMenu.transform.SetParent(menuRoot, false);
        var rootRect = phaseSelectionMenu.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

        var panelImage = AddImage(phaseSelectionMenu.transform, "Moldura principal da seleção",
            phaseMainFrameSprite != null ? phaseMainFrameSprite : submenuPanelSprite,
            new Vector2(.10f, .10f), new Vector2(.90f, .98f), Color.white, false);
        panelImage.raycastTarget = false;

        if (phaseMainFrameSprite == null)
        {
            var heading = MakeText(phaseSelectionMenu.transform, "SELECIONE A FASE",
                new Vector2(.30f, .83f), new Vector2(.70f, .93f), 48, new Color32(255, 248, 215, 255));
            InfoPanel.StyleSubmenuHeading(heading);
        }

        phaseCards.Clear();
        AddPhaseCard("VERÃO", "Nivel_2_Verao", summerPreviewSprite, summerCardFrameSprite, false, false,
            new Vector2(.21f, .52f), new Vector2(.49f, .80f));
        AddPhaseCard("PRIMAVERA", "Nivel_1_Primavera", springPreviewSprite, springCardFrameSprite, false,
            springPreviewIsProvisional,
            new Vector2(.51f, .52f), new Vector2(.79f, .80f));
        AddPhaseCard("OUTONO", "Nivel_3_Outono", autumnPreviewSprite, autumnCardFrameSprite, false, false,
            new Vector2(.21f, .22f), new Vector2(.49f, .50f));
        bool bonusLocked = !CampaignProgress.BonusUnlocked;
        Sprite bonusCardPreview = bonusLocked ? bonusLockedPreviewSprite : bonusPreviewSprite;
        if (bonusCardPreview == null) bonusCardPreview = bonusPreviewSprite;
        AddPhaseCard("BÔNUS", CampaignProgress.BonusScene, bonusCardPreview, bonusCardFrameSprite,
            bonusLocked, true, new Vector2(.51f, .22f), new Vector2(.79f, .50f));

        CreateSpriteButton(phaseSelectionMenu.transform, "Voltar", backButtonSprite, backButtonSelectedSprite,
            new Vector2(.39f, .005f), new Vector2(.61f, .09f), ClosePhaseSelection);
        phaseSelectionMenu.transform.SetAsLastSibling();

        if (EventSystem.current != null && phaseCards.Count > 0)
            EventSystem.current.SetSelectedGameObject(phaseCards[0].button.gameObject);
    }

    void AddPhaseCard(string label, string scene, Sprite preview, Sprite frame, bool locked, bool provisional,
        Vector2 min, Vector2 max)
    {
        var cardObject = new GameObject("Fase " + label, typeof(RectTransform), typeof(Image), typeof(Button));
        cardObject.transform.SetParent(phaseSelectionMenu.transform, false);
        var rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var background = cardObject.GetComponent<Image>();
        background.color = Color.clear;
        background.sprite = null;
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.raycastTarget = true;

        var button = cardObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        cardObject.AddComponent<PhaseCardHoverScale>();

        var previewImage = AddImage(cardObject.transform,
            preview != null ? "Imagem " + label : "Área de prévia " + label,
            preview, new Vector2(.08f, .20f), new Vector2(.92f, .86f),
            preview != null ? Color.white : PhaseColor(label), true);
        previewImage.raycastTarget = false;

        var card = new PhaseCard { scene = scene, button = button };
        if (locked)
        {
            var tint = AddImage(cardObject.transform, "Escurecimento de fase bloqueada", null,
                new Vector2(.08f, .20f), new Vector2(.92f, .86f), new Color(0.23f, 0.24f, 0.24f, .78f), false);
            tint.raycastTarget = false;
            if (bonusLockSprite != null)
                AddImage(cardObject.transform, "Cadeado da fase bônus", bonusLockSprite,
                    new Vector2(.42f, .37f), new Vector2(.58f, .67f), Color.white, true);
            else
                CreateLockIcon(cardObject.transform);
            button.interactable = false;
        }
        else
        {
            button.onClick.AddListener(() => SelectPhase(card));
            var audio = cardObject.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0;
            if (clickSound != null) button.onClick.AddListener(() => audio.PlayOneShot(clickSound));
            cardObject.AddComponent<MenuButtonHoverSound>().Initialize(audio, hoverSound);
        }

        if (provisional)
        {
            var provisionalText = MakeText(cardObject.transform, "PRÉVIA PROVISÓRIA",
                new Vector2(.18f, .72f), new Vector2(.82f, .80f), 17, new Color32(255, 244, 220, 255));
            provisionalText.raycastTarget = false;
            var provisionalOutline = provisionalText.gameObject.AddComponent<Outline>();
            provisionalOutline.effectColor = new Color32(43, 31, 22, 255);
            provisionalOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        var frameImage = AddImage(cardObject.transform, "Moldura " + label, frame,
            Vector2.zero, Vector2.one, Color.white, false);
        frameImage.raycastTarget = false;

        var outline = AddImage(cardObject.transform, "Contorno verde de seleção",
            phaseSelectionOutlineSprite, Vector2.zero, Vector2.one, Color.white, false);
        outline.raycastTarget = false;
        if (phaseSelectionOutlineSprite == null) outline.color = Color.clear;
        outline.gameObject.SetActive(false);
        card.outline = outline;
        phaseCards.Add(card);
    }

    static Color PhaseColor(string label)
    {
        if (label == "VERÃO") return new Color32(126, 168, 69, 255);
        if (label == "PRIMAVERA") return new Color32(63, 155, 100, 255);
        if (label == "OUTONO") return new Color32(174, 137, 55, 255);
        return new Color32(87, 83, 73, 255);
    }

    void SelectPhase(PhaseCard card)
    {
        if (card == null || card.scene == CampaignProgress.BonusScene && !CampaignProgress.BonusUnlocked) return;
        if (selectedPhaseCard == card)
        {
            ConfirmSelectedPhase();
            return;
        }
        selectedPhaseCard = card;
        for (int i = 0; i < phaseCards.Count; i++)
            phaseCards[i].outline.gameObject.SetActive(phaseCards[i] == card);
    }

    void ConfirmSelectedPhase()
    {
        if (selectedPhaseCard == null || loading) return;
        selectedLevel = selectedPhaseCard.scene;
        if (selectedLevel == CampaignProgress.BonusScene && !CampaignProgress.BonusUnlocked) return;
        if (phaseSelectionMenu != null) phaseSelectionMenu.SetActive(false);
        OpenDifficultyMenu();
    }

    void ClosePhaseSelection()
    {
        if (phaseSelectionMenu != null) Destroy(phaseSelectionMenu);
        phaseSelectionMenu = null;
        selectedPhaseCard = null;
        phaseCards.Clear();
        if (mainMenuContent != null) mainMenuContent.SetActive(true);
    }

    static void CreateLockIcon(Transform parent)
    {
        var shackle = new GameObject("Arco do cadeado", typeof(RectTransform), typeof(Image));
        shackle.transform.SetParent(parent, false);
        var archRect = shackle.GetComponent<RectTransform>();
        archRect.anchorMin = new Vector2(.425f, .52f); archRect.anchorMax = new Vector2(.575f, .72f);
        archRect.offsetMin = Vector2.zero; archRect.offsetMax = Vector2.zero;
        shackle.GetComponent<Image>().color = new Color32(255, 190, 54, 255);
        var opening = new GameObject("Abertura do cadeado", typeof(RectTransform), typeof(Image));
        opening.transform.SetParent(parent, false);
        var openingRect = opening.GetComponent<RectTransform>();
        openingRect.anchorMin = new Vector2(.46f, .53f); openingRect.anchorMax = new Vector2(.54f, .64f);
        openingRect.offsetMin = Vector2.zero; openingRect.offsetMax = Vector2.zero;
        opening.GetComponent<Image>().color = new Color32(62, 64, 62, 255);
        opening.GetComponent<Image>().raycastTarget = false;
        var body = new GameObject("Corpo do cadeado", typeof(RectTransform), typeof(Image));
        body.transform.SetParent(parent, false);
        var bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(.385f, .32f); bodyRect.anchorMax = new Vector2(.615f, .57f);
        bodyRect.offsetMin = Vector2.zero; bodyRect.offsetMax = Vector2.zero;
        body.GetComponent<Image>().color = new Color32(242, 158, 34, 255);
        var keyhole = MakeText(body.transform, "●", new Vector2(.35f, .22f), new Vector2(.65f, .75f), 24,
            new Color32(73, 43, 25, 255));
        keyhole.raycastTarget = false;
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
        if (phaseSelectionMenu != null) phaseSelectionMenu.SetActive(true);
        else if (mainMenuContent != null) mainMenuContent.SetActive(true);
    }

    void ShowInfo() => InfoPanel.Show(
        "Projeto desenvolvido como trabalho final da disciplina de Computação Gráfica.",
        "https://github.com/RafaelMaurina/Tower-Defense",
        new[] { "Rafael (@RafaelMaurina), arte e áudio", "Fabio (@fabioov), combate", "João (@Joao-Althaus), jogo e interface" },
        "Informações");

    void ShowRanking() => ShowRankingFor(false);

    void ShowRankingFor(bool bonus)
    {
        OpenRankingScreen(bonus);
    }

    void OpenRankingScreen(bool bonus)
    {
        if (mainMenuContent != null) mainMenuContent.SetActive(false);
        var screen = new GameObject("Tela Ranking", typeof(RectTransform));
        screen.transform.SetParent(menuRoot, false);
        SetFullRect(screen.GetComponent<RectTransform>());

        AddImage(screen.transform, "Moldura do ranking", rankingFrameSprite,
            new Vector2(.12f, .19f), new Vector2(.88f, .97f), Color.white, true);

        AddImage(screen.transform, "Cabeçalho da tabela", rankingHeaderSprite,
            new Vector2(.275f, .715f), new Vector2(.725f, .799f), Color.white, true);
        string[] headers = { "POS.", "PONTUAÇÃO", "DIFICULDADE", "DATA" };
        // Align data columns with the separators and label centers baked into the header sprite.
        float[] left = { .280f, .378f, .486f, .612f };
        float[] right = { .368f, .477f, .603f, .720f };

        var entries = LocalRanking.GetEntries(bonus);
        const int maxRows = 10;
        const float rowHeight = .040f;
        float startY = .695f;
        for (int i = 0; i < Mathf.Min(maxRows, entries.Count); i++)
        {
            float yMax = startY - i * rowHeight;
            float yMin = yMax - rowHeight;
            Sprite row = i == 0 ? rankingFirstSprite : i == 1 ? rankingSecondSprite
                : i == 2 ? rankingThirdSprite : rankingRowSprite;
            AddImage(screen.transform, "Fundo posição " + (i + 1), row,
                new Vector2(.275f, yMin), new Vector2(.725f, yMax),
                i >= 3 && i % 2 == 1 ? new Color32(216, 202, 174, 255) : Color.white, false);
            RankingEntry entry = entries[i];
            string position = i < 3 ? (i + 1).ToString() : (i + 1) + ".";
            string[] values = { position, entry.score.ToString(), entry.difficulty, FormatRankingDate(entry.date) };
            for (int col = 0; col < values.Length; col++)
            {
                if (col == 0 && i < 3)
                {
                    Sprite medal = i == 0 ? rankingGoldMedalSprite : i == 1 ? rankingSilverMedalSprite : rankingBronzeMedalSprite;
                    AddImage(screen.transform, "Medalha posição " + (i + 1), medal,
                        new Vector2(.300f, yMin - .004f), new Vector2(.347f, yMax + .004f), Color.white, true);
                    continue;
                }
                CreateTmpText(screen.transform, "Dado " + (i + 1) + " " + headers[col], values[col],
                    new Vector2(left[col], yMin + .002f), new Vector2(right[col], yMax - .002f), 25);
            }
        }
        if (entries.Count == 0)
            CreateTmpText(screen.transform, "Ranking vazio", "AINDA NÃO HÁ PARTIDAS CONCLUÍDAS",
                new Vector2(.25f, .43f), new Vector2(.75f, .53f), 30);

        CreateSpriteButton(screen.transform, "Voltar", backButtonSprite, backButtonSelectedSprite,
            new Vector2(.40f, .08f), new Vector2(.60f, .18f), () =>
            {
                Destroy(screen);
                if (mainMenuContent != null) mainMenuContent.SetActive(true);
            });
    }

    static string FormatRankingDate(string value)
    {
        string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy" };
        return System.DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : value;
    }

    static void SetFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateTmpText(Transform parent, string objectName, string value,
        Vector2 min, Vector2 max, float fontSize)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<TextMeshProUGUI>();
        if (rankingFontAsset == null)
            rankingFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (rankingFontAsset != null)
        {
            text.font = rankingFontAsset;
            text.fontSharedMaterial = rankingFontAsset.material;
        }
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMin = fontSize * .75f;
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(255, 246, 213, 255);
        if (rankingFontAsset != null)
        {
            text.outlineWidth = .18f;
            text.outlineColor = new Color32(54, 27, 13, 255);
        }
        text.raycastTarget = false;
        return text;
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

/// Aumenta suavemente o card enquanto o ponteiro estiver sobre ele.
public class PhaseCardHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const float HoverScale = 1.06f;
    const float SmoothTime = 0.11f;

    Vector3 normalScale;
    Vector3 targetScale;
    Vector3 scaleVelocity;

    void Awake()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = normalScale * HoverScale;

    public void OnPointerExit(PointerEventData eventData) => targetScale = normalScale;

    void Update()
    {
        transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale,
            ref scaleVelocity, SmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    void OnDisable()
    {
        if (transform == null) return;
        transform.localScale = normalScale;
        targetScale = normalScale;
        scaleVelocity = Vector3.zero;
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
