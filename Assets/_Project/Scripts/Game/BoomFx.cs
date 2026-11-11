using System.Collections;
using UnityEngine;

/// Impacto do canhao de abobora: explosao de 3 quadros (laranja, amarelo e miolo branco)
/// e poeira de bolinhas de terra que se espalham e somem em ~0,5 s.
/// Liga sozinho: escuta Projectile.OnHit e so reage a projetil de area (splashRadius > 0).
/// Tudo em pixel inteiro, sem escala nem rotacao.
public class BoomFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const int DustCount = 8;

    static BoomFx runner;
    static Sprite[] boom;
    static Sprite[] dust;   // [0..2] 4x4, [3..5] 3x3 e [6..8] 2x2, cada um em 3 cores

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        boom = null;
        dust = null;
        Projectile.OnHit -= HandleHit;
        Projectile.OnHit += HandleHit;
    }

    static void HandleHit(Projectile projectile, Vector3 where)
    {
        if (projectile == null || projectile.splashRadius <= 0f) return;
        if (runner == null)
        {
            runner = new GameObject("BoomFx").AddComponent<BoomFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.StartCoroutine(runner.Play(where));
    }

    IEnumerator Play(Vector3 where)
    {
        EnsureSprites();
        Vector3 basePos = PixelSprites.Snap(where);

        SpriteRenderer flash = NewRenderer("Boom", DrawOrder.Projectile + 1);
        flash.transform.position = basePos;

        var parts = new SpriteRenderer[DustCount];
        var target = new Vector2Int[DustCount];
        var kind = new int[DustCount];
        for (int i = 0; i < DustCount; i++)
        {
            parts[i] = NewRenderer("Poeira", DrawOrder.Projectile);
            int side = i % 2 == 0 ? 1 : -1;   // 6 a 12 pixels para os lados e 2 a 9 para cima
            target[i] = new Vector2Int(side * Random.Range(6, 13), Random.Range(2, 10));
            kind[i] = Random.Range(0, 3);
        }

        // 5 passos de 0,1 s: a poeira sai rapido e desacelera, encolhe de 4x4 ate 2x2 e some
        for (int step = 0; step < 5; step++)
        {
            flash.sprite = step < boom.Length ? boom[step] : null;
            float u = 1f - Mathf.Pow(1f - (step + 1) / 5f, 2f);
            for (int i = 0; i < DustCount; i++)
            {
                parts[i].sprite = dust[(step < 2 ? 0 : step < 4 ? 3 : 6) + kind[i]];
                Vector3 off = new Vector3(Mathf.Round(target[i].x * u), Mathf.Round(target[i].y * u), 0f) * Pixel;
                parts[i].transform.position = basePos + off;
            }
            yield return new WaitForSeconds(.1f);
        }
        Destroy(flash.gameObject);
        for (int i = 0; i < DustCount; i++) Destroy(parts[i].gameObject);
    }

    SpriteRenderer NewRenderer(string objName, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }

    static void EnsureSprites()
    {
        if (boom != null && boom[0] != null && dust != null) return;
        boom = new[] { Disc(5, false), Disc(8, false), Disc(11, true) };

        var colors = new[]
        {
            new Color32(0xcf, 0x93, 0x3b, 255),
            new Color32(0xca, 0x86, 0x22, 255),
            new Color32(0xd9, 0xd5, 0xd5, 255),
        };
        dust = new Sprite[9];
        for (int i = 0; i < 3; i++)
        {
            dust[i] = Square(4, colors[i], "poeira_4");
            dust[3 + i] = Square(3, colors[i], "poeira_3");
            dust[6 + i] = Square(2, colors[i], "poeira_2");
        }
    }

    // Explosao: borda laranja (f79617), meio amarelo (ffe731) e miolo branco. O ultimo quadro e so o anel.
    static Sprite Disc(int r, bool ringOnly)
    {
        int n = r * 2 + 1;
        var orange = new Color32(0xf7, 0x96, 0x17, 255);
        var yellow = new Color32(0xff, 0xe7, 0x31, 255);
        var white = new Color32(255, 255, 255, 255);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                Color32 c = new Color32(0, 0, 0, 0);
                if (d <= r + .3f)
                {
                    if (ringOnly) { if (d >= r * .6f) c = d > r - .8f ? orange : yellow; }
                    else c = d > r - 1.2f ? orange : d > r * .4f ? yellow : white;
                }
                px[y * n + x] = c;
            }
        return PixelSprites.Make(px, n, n, new Vector2(.5f, .5f), 32f, "boom_" + r);
    }

    static Sprite Square(int n, Color32 color, string name)
    {
        var px = new Color32[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = color;
        return PixelSprites.Make(px, n, n, new Vector2(.5f, .5f), 32f, name);
    }
}
