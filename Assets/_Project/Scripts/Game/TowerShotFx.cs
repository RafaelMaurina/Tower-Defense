using System.Collections.Generic;
using UnityEngine;

/// Ajustes visuais do projetil por tipo de torre: de onde sai, tamanho e arco.
/// Tudo em codigo, o PNG dos projeteis nao muda. O tipo vem do comeco do nome do sprite
/// (a colmeia troca para torre_colmeia_sem_abelhas, mas o pivo e o tamanho sao os mesmos).
public static class TowerShotFx
{
    public enum Kind { None, Grapes, Hive, Pumpkin, Scarecrow }

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
        if (n.StartsWith("torre_espantalho")) return Kind.Scarecrow;
        return Kind.None;
    }

    /// Pontos de cada sprite (x, y de cima, como no Piskel). O desenho muda entre os niveis, entao a tabela
    /// e por nome do sprite, nao por nivel. 'a' e a boca (espantalho: mao esquerda) e 'b' a mao direita (uvas: b e o ponto de saida do bagaco).
    public struct Spot
    {
        public Vector2 a, b;
        public Spot(float ax, float ay, float bx = 0f, float by = 0f) { a = new Vector2(ax, ay); b = new Vector2(bx, by); }
    }

    static readonly string[] Variants = { "_vento", "_tranco", "_pose" };

    static readonly Dictionary<string, Spot> Spots = new Dictionary<string, Spot>
    {
        { "torre_uvas", new Spot(52, 35, 48, 43) },               // 2 canos: entre as duas bocas; b = onde o bagaco sai, sob o cano
        { "torre_uvas_2", new Spot(58, 36, 48, 44) },             // 4 canos: ponta do feixe
        { "torre_uvas_3", new Spot(58, 36, 48, 44) },
        { "torre_colmeia", new Spot(32, 32) },                    // centro do buraco da toca
        { "torre_colmeia_2", new Spot(32, 32) },
        { "torre_colmeia_3", new Spot(32, 32) },
        { "torre_canhao_abobora", new Spot(8, 25) },              // boca do canhao (esquerda); so visual
        { "torre_canhao_abobora_2", new Spot(8, 25) },
        { "torre_canhao_abobora_3", new Spot(8, 25) },
        { "torre_espantalho", new Spot(12, 19, 51, 19) },         // palma da mao esquerda e da direita
        { "torre_espantalho_2", new Spot(12, 19, 51, 19) },
        { "torre_espantalho_3", new Spot(12, 19, 51, 19) },
    };

    public static bool TryGetSpot(Sprite s, out Spot spot)
    {
        spot = default;
        if (s == null) return false;
        string n = s.name;
        foreach (string mark in Variants)   // quadros gerados em memoria (vento, tranco, pose) valem como o original
        {
            int i = n.IndexOf(mark);
            if (i > 0) n = n.Substring(0, i);
        }
        return Spots.TryGetValue(n, out spot);
    }

    /// Espantalho: lado da mao que atira, -1 esquerda ou +1 direita, conforme o alvo.
    public static int HandSide(Tower tower, Vector3 aim) => aim.x < tower.transform.position.x ? -1 : 1;

    /// Posicao local (relativa a torre) de onde o tiro sai. A uva nao vira: a boca fica sempre a direita.
    public static Vector3 Muzzle(Tower tower, Kind kind)
    {
        Sprite s = tower.GetComponent<SpriteRenderer>().sprite;
        if (!TryGetSpot(s, out Spot spot)) return Vector3.zero;
        return PixelSprites.Snap(PixelSprites.Local(s, spot.a.x, spot.a.y));
    }

    /// Mao do espantalho que atira (lado -1 ou +1), em posicao local.
    public static Vector3 Hand(Tower tower, int side)
    {
        Sprite s = tower.GetComponent<SpriteRenderer>().sprite;
        if (!TryGetSpot(s, out Spot spot)) return Vector3.zero;
        Vector2 v = side < 0 ? spot.a : spot.b;
        return PixelSprites.Snap(PixelSprites.Local(s, v.x, v.y));
    }

    /// Ponto de onde o projetil nasce (target: onde o alvo esta, so o espantalho usa). A abobora nasce no centro (posicao logica); so o desenho sai da boca.
    public static Vector3 Origin(Tower tower, Vector3 target)
    {
        Kind k = KindOf(tower);
        if (k == Kind.Grapes || k == Kind.Hive) return tower.transform.position + Muzzle(tower, k);
        if (k == Kind.Scarecrow) return tower.transform.position + Hand(tower, HandSide(tower, target));   // o milho sai da mao do lado do alvo
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
                arc.Begin(origin, aim, Muzzle(tower, Kind.Pumpkin));
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

/// Arco so visual da abobora: a posicao logica do projetil nao muda. O desenho sai da boca do
/// canhao, sobe rapido numa parabola alta (paira no topo e cai) em pixel inteiro, e uma sombra
/// no chao encolhe quando a abobora esta alta e cresce na queda.
public class ShotArc : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float MinHeight = 0f;       // casas (0 = voo reto, sem arco)
    const float MaxHeight = 0f;
    const float HeightPerDist = 0f;
    const float LeaveMuzzle = 1f;     // o desenho sai da boca e chega ao alvo em linha reta, durante o voo todo

    static Sprite[] shadowSprites;   // 7x3 (no chao), 5x2 (meio), 3x1 (no alto)

    SpriteRenderer own, body, shadow;
    Vector3 start, aim, muzzleOffset;
    float height;
    int shadowIndex = -1;

    public void Begin(Vector3 from, Vector3 to, Vector3 muzzle = default)
    {
        start = from;
        aim = to;
        muzzleOffset = muzzle;
        float dist = Vector3.Distance(from, to);
        height = Mathf.Round(Mathf.Clamp(dist * HeightPerDist, MinHeight, MaxHeight) / Pixel) * Pixel;
        shadowIndex = -1;

        if (own == null)
        {
            own = GetComponent<SpriteRenderer>();
            body = NewChild("Arco", own.sprite, DrawOrder.Projectile + 1);
            if (shadowSprites == null || shadowSprites[0] == null) shadowSprites = BuildShadows();
            shadow = NewChild("Sombra", shadowSprites[0], DrawOrder.Projectile - 1);
        }
        own.enabled = false;   // quem desenha e o filho, que sobe no arco
        body.gameObject.SetActive(true);
        shadow.gameObject.SetActive(false);   // sem arco, sem sombra no chao
        LateUpdate();
    }

    static Sprite[] BuildShadows()
    {
        var c = new Color32(0x20, 0x18, 0x21, 120);
        var o = new Color32(0, 0, 0, 0);
        return new[]
        {
            PixelSprites.Make(new[] { o, c, c, c, c, c, o, c, c, c, c, c, c, c, o, c, c, c, c, c, o }, 7, 3, new Vector2(.5f, .5f), 32f, "sombra_abobora_7"),
            PixelSprites.Make(new[] { o, c, c, c, o, c, c, c, c, c }, 5, 2, new Vector2(.5f, .5f), 32f, "sombra_abobora_5"),
            PixelSprites.Make(new[] { c, c, c }, 3, 1, new Vector2(.5f, .5f), 32f, "sombra_abobora_3"),
        };
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

        // 1 - p^4: sobe rapido, fica um tempo no alto e cai
        float p = 2f * t - 1f;
        float profile = 1f - p * p * p * p;
        float lift = Mathf.Round(height * profile / Pixel) * Pixel;

        // o desenho nasce na boca (offset) e se junta a trajetoria logica nos primeiros 30% do voo
        Vector3 off = muzzleOffset * (1f - Mathf.Clamp01(t / LeaveMuzzle));
        Vector3 ground = PixelSprites.Snap(transform.position);
        body.transform.position = PixelSprites.Snap(ground + off) + new Vector3(0f, lift, 0f);
        shadow.transform.position = PixelSprites.Snap(ground + new Vector3(off.x, 0f, 0f)) + new Vector3(0f, -2f * Pixel, 0f);

        int idx = profile < .3f ? 0 : profile < .7f ? 1 : 2;
        if (idx != shadowIndex)
        {
            shadowIndex = idx;
            shadow.sprite = shadowSprites[idx];
        }
    }
}
