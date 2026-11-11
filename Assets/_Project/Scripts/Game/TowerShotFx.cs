using UnityEngine;

/// Ajustes visuais do projetil por tipo de torre: de onde sai, tamanho e arco.
/// Tudo em codigo, o PNG dos projeteis nao muda. O tipo vem do comeco do nome do sprite
/// (a colmeia troca para torre_colmeia_sem_abelhas, mas o pivo e o tamanho sao os mesmos).
public static class TowerShotFx
{
    public enum Kind { None, Grapes, Hive, Pumpkin }

    static Sprite grapeSprite, beeSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        grapeSprite = null;
        beeSprite = null;
    }

    public static Kind KindOf(Tower tower)
    {
        var sr = tower != null ? tower.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.sprite == null) return Kind.None;
        string n = sr.sprite.name;
        if (n.StartsWith("torre_uvas")) return Kind.Grapes;
        if (n.StartsWith("torre_colmeia")) return Kind.Hive;
        if (n.StartsWith("torre_canhao_abobora")) return Kind.Pumpkin;
        return Kind.None;
    }

    /// Posicao local (relativa a torre) de onde o tiro sai. A uva nao vira: a boca fica sempre a direita.
    public static Vector3 Muzzle(Tower tower, Kind kind)
    {
        Sprite s = tower.GetComponent<SpriteRenderer>().sprite;
        if (kind == Kind.Grapes) return PixelSprites.Snap(PixelSprites.Local(s, 58f, 35f));
        if (kind == Kind.Hive) return PixelSprites.Snap(PixelSprites.Local(s, 32f, 26f));   // toca (buraco escuro)
        return Vector3.zero;
    }

    /// Ponto de onde o projetil nasce.
    public static Vector3 Origin(Tower tower)
    {
        Kind k = KindOf(tower);
        if (k == Kind.Grapes || k == Kind.Hive) return tower.transform.position + Muzzle(tower, k);
        return tower.transform.position;
    }

    /// Chamado logo depois de p.Init: troca o sprite (uva menor, abelha maior) ou liga o arco da abobora.
    public static void Prepare(Tower tower, Projectile p, Vector3 origin, Vector3 aim)
    {
        var sr = p.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        switch (KindOf(tower))
        {
            case Kind.Grapes:
                if (grapeSprite == null) grapeSprite = Draw(GrapeRows, 6, 8, true, "projetil_uva_pequena");
                sr.sprite = grapeSprite;
                break;
            case Kind.Hive:
                if (beeSprite == null) beeSprite = Draw(BeeRows, 12, 8, false, "projetil_abelha_grande");
                sr.sprite = beeSprite;
                sr.flipX = aim.x < origin.x;
                break;
            case Kind.Pumpkin:
                ShotArc arc = p.GetComponent<ShotArc>();
                if (arc == null) arc = p.gameObject.AddComponent<ShotArc>();
                arc.Begin(origin, aim);
                break;
        }
    }

    // uva: era 8x10 pixels, fica 6x8
    static readonly string[] GrapeRows =
    {
        ".bbbb.",
        "abccbb",
        "abcbbb",
        "abbbbb",
        "abbbbb",
        "abbbbb",
        ".abbbb",
        "..bbb.",
    };

    // abelha: era 10x7 pixels, fica 12x8
    static readonly string[] BeeRows =
    {
        "..aaa.aaa...",
        ".abcbaabcba.",
        "..aaaaaaaaa.",
        ".addaeeaddaa",
        "aaeeaaeeaeea",
        ".aaeeaaeeaa.",
        "..affaffaaa.",
        "...aaaaaaa..",
    };

    static Color32 PaletteColor(char c, bool grape)
    {
        if (grape)   // a = roxo escuro, b = roxo, c = brilho
        {
            if (c == 'a') return new Color32(0x71, 0x1c, 0x85, 255);
            if (c == 'b') return new Color32(0xa1, 0x09, 0xc4, 255);
            if (c == 'c') return new Color32(255, 255, 255, 255);
            return new Color32(0, 0, 0, 0);
        }
        switch (c)   // abelha: a = contorno, b/c = asa, d/e/f = corpo
        {
            case 'a': return new Color32(0x20, 0x18, 0x21, 255);
            case 'b': return new Color32(0x00, 0x8d, 0xbf, 255);
            case 'c': return new Color32(0x44, 0xce, 0xff, 255);
            case 'd': return new Color32(0xff, 0xdf, 0x7a, 255);
            case 'e': return new Color32(0xff, 0xc1, 0x00, 255);
            case 'f': return new Color32(0xf3, 0xad, 0x00, 255);
            default: return new Color32(0, 0, 0, 0);
        }
    }

    static Sprite Draw(string[] rows, int w, int h, bool grape, string name)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = PaletteColor(rows[h - 1 - y][x], grape);
        return PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 32f, name);
    }
}

/// Arco so visual da abobora: a posicao logica do projetil nao muda. O desenho sobe numa
/// parabola em pixel inteiro e uma sombrinha fica no chao.
public class ShotArc : MonoBehaviour
{
    const float Pixel = 1f / 32f;

    static Sprite shadowSprite;

    SpriteRenderer own, body, shadow;
    Vector3 start, aim;
    float height;

    public void Begin(Vector3 from, Vector3 to)
    {
        start = from;
        aim = to;
        float dist = Vector3.Distance(from, to);
        height = Mathf.Round(Mathf.Clamp(dist * .5f, .5f, 3f) / Pixel) * Pixel;

        if (own == null)
        {
            own = GetComponent<SpriteRenderer>();
            body = NewChild("Arco", own.sprite, DrawOrder.Projectile + 1);
            if (shadowSprite == null)
            {
                var c = new Color32(0x20, 0x18, 0x21, 120);
                var o = new Color32(0, 0, 0, 0);
                shadowSprite = PixelSprites.Make(new[] { o, c, c, c, o, c, c, c, c, c }, 5, 2, new Vector2(.5f, .5f), 32f, "sombra_abobora");
            }
            shadow = NewChild("Sombra", shadowSprite, DrawOrder.Projectile - 1);
        }
        own.enabled = false;   // quem desenha e o filho, que sobe no arco
        body.gameObject.SetActive(true);
        shadow.gameObject.SetActive(true);
        LateUpdate();
    }

    SpriteRenderer NewChild(string childName, Sprite sprite, int order)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    void LateUpdate()
    {
        if (body == null) return;
        float total = Vector3.Distance(start, aim);
        float t = total < .01f ? 1f : Mathf.Clamp01(Vector3.Distance(start, transform.position) / total);
        float lift = Mathf.Round(4f * height * t * (1f - t) / Pixel) * Pixel;
        Vector3 ground = PixelSprites.Snap(transform.position);
        body.transform.position = ground + new Vector3(0f, lift, 0f);
        shadow.transform.position = ground + new Vector3(0f, -2f * Pixel, 0f);
    }
}
