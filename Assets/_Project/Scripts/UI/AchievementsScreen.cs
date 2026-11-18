using UnityEngine;
using UnityEngine.UI;

/// Lista das 20 conquistas para o submenu "Conquistas" do menu principal: agrupadas, com icone,
/// nome, descricao e, nas que guardam progresso entre partidas, quanto falta ("3/6").
/// As ganhas aparecem em cores; as que faltam, apagadas.
public static class AchievementsScreen
{
    static readonly string[] GroupTitles = { "CAMPANHA", "BICHOS E INTERAÇÕES", "TORRES", "INIMIGOS" };

    static readonly Color32 RowEarned = new Color32(111, 68, 41, 240);
    static readonly Color32 RowLocked = new Color32(58, 44, 36, 200);
    static readonly Color32 Cream = new Color32(255, 248, 215, 255);
    static readonly Color32 Gold = new Color32(255, 222, 126, 255);
    static readonly Color32 Dim = new Color32(150, 138, 124, 255);

    const float BarWidth = 12f, BarSpacing = 6f;

    /// Monta a lista dentro de 'parent' (a area do desenho da moldura do submenu), na parte escura de dentro.
    /// Devolve a barra de rolagem, para o menu ligar a navegacao do teclado e do controle.
    public static Scrollbar Build(Transform parent)
    {
        var count = MakeText(parent, "Contagem", $"{Achievements.UnlockedCount} / {Achievements.Total}",
            new Vector2(.30f, .735f), new Vector2(.70f, .79f), 30, Gold, TextAnchor.MiddleCenter);
        count.raycastTarget = false;

        var scrollObject = new GameObject("Lista de conquistas", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(parent, false);
        var scrollRect = scrollObject.GetComponent<RectTransform>();
        // parte escura da moldura (submenu_molde, 1024x768): x de .115 a .881, y de .219 a .781;
        // com folga para as folhas dos cantos
        scrollRect.anchorMin = new Vector2(.15f, .235f); scrollRect.anchorMax = new Vector2(.85f, .72f);
        scrollRect.offsetMin = Vector2.zero; scrollRect.offsetMax = Vector2.zero;

        // area visivel: corta o que passa e recebe a roda do mouse
        var viewport = new GameObject("Area visivel", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = new Vector2(-(BarWidth + BarSpacing), 0f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportImage.raycastTarget = true;

        var content = new GameObject("Conteudo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f); contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(.5f, 1f);
        contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        scroll.verticalScrollbar = MakeScrollbar(scrollObject.transform);
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        AchievementGroup? current = null;
        foreach (AchievementInfo info in Achievements.All)
        {
            if (current != info.group)
            {
                current = info.group;
                AddHeader(content.transform, GroupTitles[(int)info.group]);
            }
            AddRow(content.transform, info);
        }
        return scroll.verticalScrollbar;
    }

    // barra simples, sem arte: trilho escuro e alca creme; com foco a alca fica dourada e as setas rolam
    static Scrollbar MakeScrollbar(Transform parent)
    {
        var bar = new GameObject("Barra de rolagem", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        bar.transform.SetParent(parent, false);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(1f, 0f); barRect.anchorMax = Vector2.one;
        barRect.pivot = new Vector2(1f, .5f);
        barRect.offsetMin = new Vector2(-BarWidth, 0f); barRect.offsetMax = Vector2.zero;
        bar.GetComponent<Image>().color = new Color32(30, 20, 14, 140);

        var area = new GameObject("Area da alca", typeof(RectTransform));
        area.transform.SetParent(bar.transform, false);
        var areaRect = area.GetComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one;
        areaRect.offsetMin = Vector2.zero; areaRect.offsetMax = Vector2.zero;

        var handle = new GameObject("Alca", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(area.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.offsetMin = Vector2.zero; handleRect.offsetMax = Vector2.zero;
        var handleImage = handle.GetComponent<Image>();
        handleImage.color = Color.white;

        var scrollbar = bar.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var colors = scrollbar.colors;
        colors.normalColor = new Color32(255, 248, 215, 150);
        colors.highlightedColor = new Color32(255, 248, 215, 230);
        colors.selectedColor = Gold;
        colors.pressedColor = Gold;
        scrollbar.colors = colors;
        return scrollbar;
    }

    static void AddHeader(Transform parent, string title)
    {
        var text = MakeText(parent, "Grupo " + title, title, Vector2.zero, Vector2.one, 26, Gold, TextAnchor.MiddleLeft);
        text.raycastTarget = false;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
    }

    static void AddRow(Transform parent, AchievementInfo info)
    {
        bool earned = Achievements.IsUnlocked(info.id);
        var row = new GameObject("Conquista " + info.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = 78f;
        var back = row.GetComponent<Image>();
        back.color = earned ? RowEarned : RowLocked;
        back.raycastTarget = false;

        var icon = new GameObject("Icone", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(row.transform, false);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(.015f, .1f); iconRect.anchorMax = new Vector2(.085f, .9f);
        iconRect.offsetMin = Vector2.zero; iconRect.offsetMax = Vector2.zero;
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = AchievementToast.IconFor(info.id);
        iconImage.preserveAspect = true;
        iconImage.color = earned ? Color.white : new Color(.35f, .35f, .35f, .75f);   // as que faltam ficam em cinza
        iconImage.raycastTarget = false;

        MakeText(row.transform, "Nome", info.name, new Vector2(.10f, .48f), new Vector2(.80f, .95f), 28,
            earned ? Cream : Dim, TextAnchor.MiddleLeft).raycastTarget = false;
        MakeText(row.transform, "Descricao", info.description, new Vector2(.10f, .05f), new Vector2(.80f, .52f), 20,
            earned ? new Color32(238, 222, 190, 255) : Dim, TextAnchor.MiddleLeft).raycastTarget = false;

        string status = "";
        if (earned) status = "GANHA";
        else
        {
            Achievements.Progress(info.id, out int have, out int total);
            if (total > 0) status = $"{have}/{total}";
        }
        if (status.Length > 0)
            MakeText(row.transform, "Estado", status, new Vector2(.80f, .1f), new Vector2(.985f, .9f), 24,
                earned ? Gold : Dim, TextAnchor.MiddleRight).raycastTarget = false;
    }

    static Text MakeText(Transform parent, string name, string value, Vector2 min, Vector2 max, int size,
        Color color, TextAnchor anchor)
    {
        var go = new GameObject("Texto " + name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.text = value;
        UiTypography.Apply(text, size);
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(10, size / 2);
        text.resizeTextMaxSize = size;
        return text;
    }
}
