using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AchievementsScreen : MonoBehaviour
{
    static readonly string[] GroupArt = { "campanha", "bichos", "torres", "inimigos" };
    static readonly Color32 Cream = new Color32(255, 240, 205, 255);
    static readonly Color32 Detail = new Color32(237, 217, 183, 255);
    static readonly Color32 LockedTitle = new Color32(220, 211, 194, 255);
    static readonly Color32 LockedDetail = new Color32(199, 187, 169, 255);
    static readonly Color32 LockedWood = new Color32(174, 157, 143, 255);
    const float BarWidth = 20f, BarSpacing = 8f;

    readonly List<Row> rows = new List<Row>();
    readonly TextMeshProUGUI[] groupLabels = new TextMeshProUGUI[GroupArt.Length];
    readonly int[] groupTotals = new int[GroupArt.Length];
    readonly int[] groupUnlocked = new int[GroupArt.Length];
    TextMeshProUGUI countText, percentageText;
    Image progressFill;
    Sprite lockSprite, checkSprite;
    int lastCount = -1;
    float nextRefresh;

    sealed class Row
    {
        public string id;
        public int group;
        public Image frame, shade, star, status;
        public TextMeshProUGUI title, description;
        public bool? earned;
    }

    public static Scrollbar Build(Transform parent)
    {
        var view = parent.gameObject.AddComponent<AchievementsScreen>();
        var scroll = CreateList(parent);
        view.AddProgress(scroll.content);
        view.AddRows(scroll.content);
        view.Refresh();
        return scroll.verticalScrollbar;
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        Refresh();
    }

    void Refresh()
    {
        int unlocked = Achievements.UnlockedCount;
        if (unlocked != lastCount)
        {
            lastCount = unlocked;
            float fraction = Achievements.Total > 0 ? (float)unlocked / Achievements.Total : 0f;
            countText.text = $"PROGRESSO GERAL — {unlocked} / {Achievements.Total}";
            percentageText.text = $"{Mathf.RoundToInt(fraction * 100f)}%";
            progressFill.fillAmount = fraction;
        }

        System.Array.Clear(groupUnlocked, 0, groupUnlocked.Length);
        foreach (var row in rows)
        {
            bool earned = Achievements.IsUnlocked(row.id);
            if (earned) groupUnlocked[row.group]++;
            if (row.earned == earned) continue;
            row.earned = earned;
            row.frame.color = earned ? Color.white : LockedWood;
            row.shade.enabled = !earned;
            row.star.color = earned ? Color.white : new Color32(151, 135, 110, 255);
            row.title.color = earned ? Cream : LockedTitle;
            row.description.color = earned ? Detail : LockedDetail;
            row.status.sprite = earned ? checkSprite : lockSprite;
        }

        for (int group = 0; group < groupLabels.Length; group++)
            if (groupLabels[group] != null)
                groupLabels[group].SetText("{0} / {1}", (float)groupUnlocked[group], (float)groupTotals[group]);
    }

    void AddProgress(Transform parent)
    {
        var panel = new GameObject("Progresso geral", typeof(RectTransform), typeof(LayoutElement));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<LayoutElement>().preferredHeight = 76f;
        countText = AddText(panel.transform, "Contador", "", new Vector2(.10f, .52f), new Vector2(.90f, 1f), 27, TextAlignmentOptions.Center);
        var track = AddImage(panel.transform, "Moldura do progresso", Art("progresso_fundo"), new Vector2(.10f, 0f), new Vector2(.90f, .52f), Image.Type.Sliced);
        progressFill = AddImage(track.transform, "Preenchimento", Art("progresso_verde"), Vector2.zero, Vector2.one, Image.Type.Filled);
        progressFill.rectTransform.offsetMin = new Vector2(5f, 5f);
        progressFill.rectTransform.offsetMax = new Vector2(-5f, -5f);
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        percentageText = AddText(track.transform, "Porcentagem", "", Vector2.zero, Vector2.one, 22, TextAlignmentOptions.Center);
    }

    static ScrollRect CreateList(Transform parent)
    {
        var scrollObject = new GameObject("Lista de conquistas", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        SetAnchors(scrollObject.GetComponent<RectTransform>(), new Vector2(.115f, .12f), new Vector2(.885f, .802f));

        var viewport = new GameObject("Área visível", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        SetAnchors(viewportRect, Vector2.zero, Vector2.one);
        viewportRect.offsetMax = new Vector2(-(BarWidth + BarSpacing), 0f);
        viewport.GetComponent<Image>().color = Color.clear;

        var content = new GameObject("Conteúdo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.pivot = new Vector2(.5f, 1f);
        SetAnchors(contentRect, new Vector2(0f, 1f), Vector2.one);
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(2, 2, 0, 2);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        scroll.verticalScrollbar = CreateScrollbar(scrollObject.transform);
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalNormalizedPosition = 1f;
        return scroll;
    }

    static Scrollbar CreateScrollbar(Transform parent)
    {
        var track = AddImage(parent, "Trilho da rolagem", Art("rolagem_trilho"), new Vector2(1f, 0f), Vector2.one, Image.Type.Sliced);
        track.rectTransform.offsetMin = new Vector2(-BarWidth, 0f);
        track.raycastTarget = true;

        var slideArea = new GameObject("Área da alça", typeof(RectTransform));
        slideArea.transform.SetParent(track.transform, false);
        var slideRect = slideArea.GetComponent<RectTransform>();
        SetAnchors(slideRect, Vector2.zero, Vector2.one);
        slideRect.offsetMin = new Vector2(2f, 2f);
        slideRect.offsetMax = new Vector2(-2f, -2f);

        var handle = AddImage(slideArea.transform, "Alça", Art("rolagem_alca"), Vector2.zero, Vector2.one, Image.Type.Sliced);
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var colors = scrollbar.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 240, 206, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color32(211, 174, 131, 255);
        scrollbar.colors = colors;
        return scrollbar;
    }

    void AddGroupHeader(Transform parent, int group)
    {
        var header = new GameObject("Seção " + GroupArt[group], typeof(RectTransform), typeof(LayoutElement));
        header.transform.SetParent(parent, false);
        header.GetComponent<LayoutElement>().preferredHeight = 92f;

        var sprite = Art("conquistas_secao_" + GroupArt[group]);
        var plate = AddImage(header.transform, "Placa da seção", sprite, Vector2.zero, Vector2.one);
        var fitter = plate.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

        var counter = AddText(plate.transform, "Conquistas da seção", "",
            new Vector2(.81f, .24f), new Vector2(.953f, .77f), 34, TextAlignmentOptions.Center);
        counter.fontSharedMaterial = counter.font.material;
        counter.outlineColor = new Color32(55, 24, 11, 255);
        counter.outlineWidth = .19f;
        groupLabels[group] = counter;
    }

    void AddRows(Transform parent)
    {
        var rowPrefab = Resources.Load<GameObject>("Conquistas/LinhaConquista");
        var star = AchievementToast.IconFor(string.Empty);
        lockSprite = Resources.Load<Sprite>("MenuPhase/fase_cadeado");
        checkSprite = Art("check");

        AchievementGroup? current = null;
        foreach (var info in Achievements.All)
        {
            int group = (int)info.group;
            groupTotals[group]++;
            if (current != info.group)
            {
                AddGroupHeader(parent, group);
                current = info.group;
            }

            var rowObject = Instantiate(rowPrefab, parent, false);
            rowObject.name = "Conquista " + info.id;
            var row = rowObject.transform;
            var shade = AddImage(row, "Sombra interna", null, Vector2.zero, Vector2.one);
            shade.rectTransform.offsetMin = new Vector2(5f, 5f);
            shade.rectTransform.offsetMax = new Vector2(-5f, -5f);
            shade.color = new Color32(31, 19, 14, 76);

            rows.Add(new Row
            {
                id = info.id,
                group = group,
                frame = rowObject.GetComponent<Image>(),
                shade = shade,
                star = AddIcon(row, "Estrela", star, .044f, 42f),
                status = AddIcon(row, "Estado", null, .95f, 42f),
                title = AddText(row, "Nome", info.name, new Vector2(.09f, .49f), new Vector2(.90f, .94f), 26, TextAlignmentOptions.MidlineLeft),
                description = AddText(row, "Descrição", info.description, new Vector2(.09f, .07f), new Vector2(.90f, .50f), 20, TextAlignmentOptions.MidlineLeft),
            });
        }
    }

    static Sprite Art(string name) => Resources.Load<Sprite>("Conquistas/" + name);

    static Image AddIcon(Transform parent, string name, Sprite sprite, float x, float size)
    {
        var anchor = new Vector2(x, .5f);
        var icon = AddImage(parent, name, sprite, anchor, anchor);
        icon.rectTransform.sizeDelta = new Vector2(size, size);
        icon.preserveAspect = true;
        return icon;
    }

    static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, Image.Type type = Image.Type.Simple)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        SetAnchors(image.rectTransform, min, max);
        image.sprite = sprite;
        image.type = type;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI AddText(Transform parent, string name, string value, Vector2 min, Vector2 max,
        float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        SetAnchors(label.rectTransform, min, max);
        label.font = TMP_Settings.defaultFontAsset;
        label.fontStyle = FontStyles.Bold;
        label.text = value;
        label.color = Cream;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * .55f;
        label.fontSizeMax = size;
        label.alignment = alignment;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
