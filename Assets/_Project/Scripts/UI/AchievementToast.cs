using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Aviso na tela quando uma conquista e ganha (estilo Steam): uma plaquinha desce do alto, fica uns
/// segundos e sobe. Cria o proprio Canvas e usa tempo real, entao aparece tambem com o jogo pausado
/// ou na tela de vitoria. Varias conquistas de uma vez entram numa fila.
public class AchievementToast : MonoBehaviour
{
    // posicao e tempos: ajustar aqui
    static readonly Vector2 AnchorMin = new Vector2(.34f, .80f), AnchorMax = new Vector2(.66f, .895f);
    const float SlideTime = .35f, HoldTime = 3.5f;

    static AchievementToast instance;
    static Sprite fallbackIcon;
    readonly Queue<AchievementInfo> queue = new Queue<AchievementInfo>();
    bool running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        fallbackIcon = null;
    }

    public static void Show(AchievementInfo info)
    {
        if (info == null || !Application.isPlaying) return;   // fora do Play (testes de editor) nao ha tela para avisar
        if (instance == null)
        {
            instance = new GameObject("AchievementToast").AddComponent<AchievementToast>();
            DontDestroyOnLoad(instance.gameObject);
        }
        instance.queue.Enqueue(info);
        if (!instance.running) instance.StartCoroutine(instance.Run());
    }

    /// Icone da conquista: Resources/Conquistas/icone_<id>, depois Resources/Conquistas/icone_padrao,
    /// depois uma estrela de pixel gerada aqui. A arte final do Rafael entra so soltando os arquivos na pasta.
    public static Sprite IconFor(string id)
    {
        Sprite sprite = Resources.Load<Sprite>("Conquistas/icone_" + id);
        if (sprite == null) sprite = Resources.Load<Sprite>("Conquistas/icone_padrao");
        return sprite != null ? sprite : fallbackIcon ??= BuildStar();
    }

    IEnumerator Run()
    {
        running = true;
        var canvasObject = new GameObject("Canvas das conquistas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;   // acima do HUD (50)
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        while (queue.Count > 0)
        {
            RectTransform panel = BuildPanel(canvasObject.transform, queue.Dequeue());
            Vector2 shown = panel.anchoredPosition, hidden = shown + new Vector2(0f, 260f);   // sai pelo alto
            yield return Slide(panel, hidden, shown);
            yield return new WaitForSecondsRealtime(HoldTime);
            yield return Slide(panel, shown, hidden);
            Destroy(panel.gameObject);
        }

        Destroy(canvasObject);
        running = false;
    }

    static IEnumerator Slide(RectTransform panel, Vector2 from, Vector2 to)
    {
        for (float t = 0f; t < SlideTime; t += Time.unscaledDeltaTime)
        {
            float k = t / SlideTime;
            panel.anchoredPosition = Vector2.Lerp(from, to, k * (2f - k));   // desacelera no fim
            yield return null;
        }
        panel.anchoredPosition = to;
    }

    static RectTransform BuildPanel(Transform parent, AchievementInfo info)
    {
        var go = new GameObject("Conquista " + info.id, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = AnchorMin; rect.anchorMax = AnchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var back = go.GetComponent<Image>();
        back.color = new Color32(111, 68, 41, 245);   // madeira, como os botoes do menu
        back.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color32(43, 31, 22, 255);
        outline.effectDistance = new Vector2(3f, -3f);

        var icon = new GameObject("Icone", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(go.transform, false);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(.03f, .12f); iconRect.anchorMax = new Vector2(.19f, .88f);
        iconRect.offsetMin = Vector2.zero; iconRect.offsetMax = Vector2.zero;
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = IconFor(info.id);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        AddText(go.transform, "CONQUISTA DESBLOQUEADA", new Vector2(.22f, .68f), new Vector2(.97f, .94f), 17,
            new Color32(255, 222, 126, 255));
        AddText(go.transform, info.name, new Vector2(.22f, .36f), new Vector2(.97f, .70f), 26,
            new Color32(255, 248, 215, 255));
        AddText(go.transform, info.description, new Vector2(.22f, .04f), new Vector2(.97f, .38f), 16,
            new Color32(238, 222, 190, 255));
        return rect;
    }

    static void AddText(Transform parent, string value, Vector2 min, Vector2 max, int size, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.text = value;
        UiTypography.Apply(text, size);
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(10, size / 2);
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
    }

    // estrela 11x11 amarela com o mesmo contorno escuro dos desenhos do jogo
    static Sprite BuildStar()
    {
        string[] rows =
        {
            ".....X.....",
            ".....X.....",
            "....XXX....",
            "....XXX....",
            "XXXXXXXXXXX",
            ".XXXXXXXXX.",
            "..XXXXXXX..",
            "...XXXXX...",
            "...XXXXX...",
            "..XXX.XXX..",
            "..XX...XX..",
        };
        int w = 11 + 2, h = rows.Length + 2;
        var fill = new bool[w, h];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < 11; x++)
                if (rows[y][x] == 'X') fill[1 + x, 1 + (rows.Length - 1 - y)] = true;

        var gold = new Color32(0xF5, 0xC8, 0x42, 255);
        var dark = new Color32(0x20, 0x18, 0x21, 255);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++)
                    for (int dx = -1; dx <= 1 && !near; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        near = nx >= 0 && ny >= 0 && nx < w && ny < h && fill[nx, ny];
                    }
                px[y * w + x] = fill[x, y] ? gold : near ? dark : new Color32(0, 0, 0, 0);
            }
        return PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 16f, "icone_conquista_padrao");
    }
}
