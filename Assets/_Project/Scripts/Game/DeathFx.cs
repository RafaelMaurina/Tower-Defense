using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Morte do inimigo: a silhueta creme pisca 1 quadro, depois uma nuvem de "puf" de 3 quadros e
/// 3 a 5 faiscas que saem para os lados e somem. Dura ~0,35 s e fica na altura do corpo
/// (vale para voador tambem). O inimigo real ja voltou ao Pool, entao tudo e copia.
/// Liga sozinho: escuta Enemy.OnDied (so morte, nao chegada ao celeiro). Pixel inteiro, sem escala.
public class DeathFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float Tick = .05f;
    const int Ticks = 7;               // 1 de flash + 6 de puf (3 quadros de 2 passos)

    static readonly int[] PuffAt = { -1, 0, 0, 1, 1, 2, 2 };
    static readonly int[] PuffLift = { 0, 0, 0, 1, 1, 2, 2 };
    static readonly float[] PuffAlpha = { 1f, 1f, 1f, 1f, 1f, 1f, .5f };
    static readonly WaitForSeconds TickWait = new WaitForSeconds(Tick);

    static readonly string[][] PuffRows =
    {
        new[] { "...oooo...", ".ollllllo.", "ollllllllo", "ollllllmmo", "ollllmmmmo", "ollmmmmmmo", ".ommmmmmo.", "..oooooo.." },
        new[] { "....oooo........", "..oolllloo.oooo.", ".ollllllllollllo", "ollllllllllllllo", "ollllllllllllllo", "ollllllllllllmmo",
                "ollllllllllmmmmo", "ollllllllmmmmmmo", ".ollllmmmmmmmmo.", "..ommmmmmmmmmo..", "...ooommmmooo...", ".....oooooo....." },
        new[] { "....ll..........ll", "...llll........lll", "....ll....m.....l.", ".l.......mm.......", "lll...............",
                ".l..........m..ll.", "..m.....l..mm.lll.", ".mm....lll..m..ll.", "..m.....l.........", "...........l...m..",
                "m.l..mm...lll.mm..", ".lll.mm....l..m...", "..l....l..........", "......lll....l...." },
    };

    static DeathFx runner;
    static Sprite[] puffs;
    static Sprite[] sparks;     // 1x1 creme, 1x1 amarelo, 2x2 amarelo
    static readonly Dictionary<Sprite, Sprite> silhouettes = new Dictionary<Sprite, Sprite>();
    readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        puffs = null;
        sparks = null;
        silhouettes.Clear();
        Enemy.OnDied -= HandleDied;
        Enemy.OnDied += HandleDied;
    }

    static void HandleDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null) return;
        if (runner == null)
        {
            runner = new GameObject("DeathFx").AddComponent<DeathFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        var sr = enemy.GetComponent<SpriteRenderer>();
        Sprite flash = sr != null ? SilhouetteOf(sr.sprite) : null;
        int order = sr != null ? sr.sortingOrder : DrawOrder.Projectile - 1;
        bool flip = sr != null && sr.flipX;
        runner.StartCoroutine(runner.Play(enemy.transform.position, PixelSprites.Snap(enemy.Center), flash, flip, order));
    }

    static Sprite SilhouetteOf(Sprite source)
    {
        if (source == null) return null;
        if (!silhouettes.TryGetValue(source, out Sprite made))
        {
            made = FxPixels.Silhouette(source);
            silhouettes[source] = made;
        }
        return made;
    }

    IEnumerator Play(Vector3 feet, Vector3 center, Sprite flash, bool flip, int order)
    {
        EnsureSprites();
        SpriteRenderer flashSr = Rent("Piscada", order);
        flashSr.sprite = flash;
        flashSr.flipX = flip;
        flashSr.transform.position = feet;
        SpriteRenderer puff = Rent("Puf", order + 1);

        int n = Random.Range(3, 6);
        var spark = new SpriteRenderer[n];
        var side = new int[n];
        var reach = new float[n];
        var up = new float[n];
        var fall = new float[n];
        var y0 = new int[n];
        var kind = new int[n];
        var big = new bool[n];
        for (int i = 0; i < n; i++)
        {
            spark[i] = Rent("Faisca", order + 2);
            spark[i].enabled = false;
            side[i] = i % 2 == 0 ? 1 : -1;
            reach[i] = Random.Range(9f, 16f);
            up[i] = Random.Range(5f, 11f);
            fall[i] = Random.Range(3f, 9f);
            y0[i] = Random.Range(-2, 3);
            kind[i] = Random.Range(0, 2);
            big[i] = Random.value < .5f;
        }

        for (int t = 0; t < Ticks; t++)
        {
            flashSr.enabled = t == 0 && flash != null;
            if (PuffAt[t] >= 0)
            {
                puff.enabled = true;
                puff.sprite = puffs[PuffAt[t]];
                puff.color = new Color(1f, 1f, 1f, PuffAlpha[t]);
                puff.transform.position = center + new Vector3(0f, PuffLift[t] * Pixel, 0f);
            }
            else puff.enabled = false;

            if (t >= 1)
            {
                float u = t / 6f;
                for (int i = 0; i < n; i++)
                {
                    int x = side[i] * Mathf.RoundToInt(reach[i] * (1f - (1f - u) * (1f - u)));
                    int y = y0[i] + Mathf.RoundToInt(up[i] * u - fall[i] * u * u);
                    spark[i].enabled = true;
                    spark[i].sprite = big[i] && t <= 2 ? sparks[2] : sparks[kind[i]];
                    spark[i].color = new Color(1f, 1f, 1f, t == 6 ? .5f : 1f);
                    spark[i].transform.position = center + new Vector3(x * Pixel, y * Pixel, 0f);
                }
            }
            yield return TickWait;
        }

        Give(flashSr);
        Give(puff);
        for (int i = 0; i < n; i++) Give(spark[i]);
    }

    SpriteRenderer Rent(string objName, int order)
    {
        SpriteRenderer sr = null;
        while (free.Count > 0 && sr == null) sr = free.Pop();
        if (sr == null)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform);
            sr = go.AddComponent<SpriteRenderer>();
        }
        sr.sortingOrder = order;
        sr.flipX = false;
        sr.color = Color.white;
        sr.enabled = true;
        return sr;
    }

    void Give(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.enabled = false;
        free.Push(sr);
    }

    static void EnsureSprites()
    {
        if (puffs != null && puffs[0] != null) return;
        puffs = new Sprite[PuffRows.Length];
        for (int i = 0; i < puffs.Length; i++) puffs[i] = FxPixels.Rows(PuffRows[i], "puf_" + i, -1, 0, false);
        sparks = new[]
        {
            FxPixels.Rows(new[] { "w" }, "faisca_1", -1, 0, false),
            FxPixels.Rows(new[] { "Y" }, "faisca_2", -1, 0, false),
            FxPixels.Rows(new[] { "YY", "YY" }, "faisca_3", -1, 0, false),
        };
    }
}
