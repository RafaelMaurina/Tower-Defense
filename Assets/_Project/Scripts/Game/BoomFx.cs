using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Impacto do canhao de abobora: bola de fogo irregular de 5 quadros (clarao, bola amarela,
/// bola laranja com pontas, anel de fumaca, fiapos), estilhacos de abobora que caem no chao
/// e poeira rasteira. Dura ~0,6 s. O tamanho vem do splashRadius do projetil (1 unidade = 32 px).
/// Liga sozinho: escuta Projectile.OnHit e so reage a projetil de area (splashRadius > 0).
/// Tudo em pixel inteiro, sem escala nem rotacao. Sprites em cache e renderers em pool.
public class BoomFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float Tick = .045f;          // segundos por passo
    const int Ticks = 14;              // passos totais (~0,63 s)
    const int ShardCount = 10;
    const int DustCount = 8;
    const int MinDiameter = 36;        // pico da bola de fogo, em pixels

    // quadro da explosao em cada passo (12 passos); depois disso so os estilhacos ficam
    static readonly int[] FrameAt = { 0, 0, 1, 1, 2, 2, 3, 3, 3, 4, 4, 4 };
    static readonly float[] AlphaAt = { 1f, 1f, 1f, 1f, 1f, 1f, .95f, .85f, .7f, .8f, .55f, .3f };
    static readonly int[] LiftAt = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 };   // os fiapos sobem 3 px
    static readonly WaitForSeconds TickWait = new WaitForSeconds(Tick);

    sealed class Blast
    {
        public Transform root;
        public SpriteRenderer main;
        public SpriteRenderer[] shards = new SpriteRenderer[ShardCount];
        public SpriteRenderer[] dust = new SpriteRenderer[DustCount];
        public float[] sx = new float[ShardCount], sy = new float[ShardCount];
        public float[] vx = new float[ShardCount], vy = new float[ShardCount], ground = new float[ShardCount];
        public Vector2Int[] dustTarget = new Vector2Int[DustCount];
        public int[] dustKind = new int[DustCount];
    }

    static BoomFx runner;
    static readonly Dictionary<int, Sprite[]> boomByDiameter = new Dictionary<int, Sprite[]>();
    static Sprite[] dust;     // [0..2] 4x4, [3..5] 3x3 e [6..8] 2x2, cada um em 3 cores
    static Sprite[] shardSprites;
    readonly Stack<Blast> freeBlasts = new Stack<Blast>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        boomByDiameter.Clear();
        dust = null;
        shardSprites = null;
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
        // pico da bola = ~1,3x o raio de dano; o anel de fumaca chega perto da area real
        int diameter = Mathf.Clamp(Mathf.RoundToInt(projectile.splashRadius * 32f * 1.3f), MinDiameter, 64);
        runner.StartCoroutine(runner.Play(where, diameter));
    }

    IEnumerator Play(Vector3 where, int diameter)
    {
        EnsureSprites();
        Sprite[] frames = BoomFrames(diameter);
        Vector3 basePos = PixelSprites.Snap(where);
        Blast b = freeBlasts.Count > 0 ? freeBlasts.Pop() : NewBlast();
        b.root.gameObject.SetActive(true);
        b.main.transform.position = basePos;

        float spread = diameter * .5f;
        for (int i = 0; i < ShardCount; i++)
        {
            int side = i % 2 == 0 ? 1 : -1;
            b.sx[i] = side * Random.Range(0f, 3f);
            b.sy[i] = 0f;
            b.vx[i] = side * Random.Range(2.5f, 6.5f);
            b.vy[i] = Random.Range(3f, 5f);
            b.ground[i] = Random.Range(-7f, -1f);
            b.shards[i].sprite = shardSprites[Random.Range(0, shardSprites.Length)];
            b.shards[i].enabled = true;
        }
        for (int i = 0; i < DustCount; i++)
        {
            int side = i % 2 == 0 ? 1 : -1;   // corre rente ao chao, para os lados
            b.dustTarget[i] = new Vector2Int(side * Random.Range(12, Mathf.RoundToInt(spread) + 8), Random.Range(-8, -1));
            b.dustKind[i] = Random.Range(0, 3);
            b.dust[i].enabled = true;
        }

        for (int t = 0; t < Ticks; t++)
        {
            // bola de fogo / fumaca / fiapos
            if (t < FrameAt.Length)
            {
                b.main.enabled = true;
                b.main.sprite = frames[FrameAt[t]];
                b.main.color = new Color(1f, 1f, 1f, AlphaAt[t]);
                b.main.transform.position = basePos + new Vector3(0f, LiftAt[t] * Pixel, 0f);
            }
            else b.main.enabled = false;

            // poeira: sai rapido, desacelera, encolhe de 4x4 ate 2x2 e some
            float u = 1f - Mathf.Pow(1f - Mathf.Min(1f, (t + 1) / 12f), 2f);
            for (int i = 0; i < DustCount; i++)
            {
                if (t >= 12) { b.dust[i].enabled = false; continue; }
                b.dust[i].sprite = dust[(t < 4 ? 0 : t < 8 ? 3 : 6) + b.dustKind[i]];
                float rise = t >= 2 && t < 8 ? 1f : 0f;
                Vector3 off = new Vector3(Mathf.Round(b.dustTarget[i].x * u), Mathf.Round(b.dustTarget[i].y * u + rise), 0f) * Pixel;
                b.dust[i].transform.position = basePos + off;
            }

            // estilhacos: voam em arco com gravidade, pousam no chao e ficam ate o fim
            for (int i = 0; i < ShardCount; i++)
            {
                if (t > 0 && (b.vx[i] != 0f || b.vy[i] != 0f))   // parado = ja pousou
                {
                    b.sx[i] += b.vx[i];
                    b.sy[i] += b.vy[i];
                    b.vy[i] -= 1.4f;
                    if (b.vy[i] < 0f && b.sy[i] <= b.ground[i])
                    {
                        b.sy[i] = b.ground[i];
                        b.vx[i] = 0f;
                        b.vy[i] = 0f;
                    }
                }
                b.shards[i].transform.position = basePos + new Vector3(Mathf.Round(b.sx[i]), Mathf.Round(b.sy[i]), 0f) * Pixel;
            }
            yield return TickWait;
        }

        b.root.gameObject.SetActive(false);
        freeBlasts.Push(b);
    }

    Blast NewBlast()
    {
        var b = new Blast();
        var rootGo = new GameObject("Explosao");
        rootGo.transform.SetParent(transform);
        b.root = rootGo.transform;
        b.main = NewRenderer(b.root, "Boom", DrawOrder.Projectile + 1);
        for (int i = 0; i < DustCount; i++) b.dust[i] = NewRenderer(b.root, "Poeira", DrawOrder.Projectile);
        for (int i = 0; i < ShardCount; i++) b.shards[i] = NewRenderer(b.root, "Estilhaco", DrawOrder.Projectile + 2);
        return b;
    }

    static SpriteRenderer NewRenderer(Transform parent, string objName, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }

    static void EnsureSprites()
    {
        if (dust != null && dust[0] != null && shardSprites != null) return;
        boomByDiameter.Clear();

        var colors = new[] { PixelBlast.Earth, PixelBlast.EarthDark, PixelBlast.SmokeLight };
        dust = new Sprite[9];
        for (int i = 0; i < 3; i++)
        {
            dust[i] = Square(4, colors[i], "poeira_4");
            dust[3 + i] = Square(3, colors[i], "poeira_3");
            dust[6 + i] = Square(2, colors[i], "poeira_2");
        }

        // pedacos de abobora: o = laranja, d = laranja escuro, b = marrom, g = casca verde
        System.Func<char, Color32> pal = c =>
        {
            switch (c)
            {
                case 'o': return new Color32(0xf7, 0x96, 0x17, 255);
                case 'd': return new Color32(0xd5, 0x77, 0x26, 255);
                case 'b': return new Color32(0xb7, 0x67, 0x05, 255);
                default: return new Color32(0x00, 0x9d, 0x38, 255);
            }
        };
        shardSprites = new[]
        {
            PixelBlast.FromRows(new[] { "oo", "od" }, pal, "estilhaco_a"),
            PixelBlast.FromRows(new[] { "do", "bd" }, pal, "estilhaco_b"),
            PixelBlast.FromRows(new[] { "oo", "bb" }, pal, "estilhaco_c"),
            PixelBlast.FromRows(new[] { "ooo", "odb" }, pal, "estilhaco_d"),
            PixelBlast.FromRows(new[] { "ggo", "odd" }, pal, "estilhaco_casca3"),
            PixelBlast.FromRows(new[] { "go", "dd" }, pal, "estilhaco_casca2"),
        };
    }

    static Sprite[] BoomFrames(int diameter)
    {
        if (boomByDiameter.TryGetValue(diameter, out Sprite[] cached) && cached[0] != null) return cached;
        Sprite[] built = BuildBoom(diameter);
        boomByDiameter[diameter] = built;
        return built;
    }

    static float Dist(float dx, float dy) => Mathf.Sqrt(dx * dx + dy * dy);

    // Faixas de cor com serrilhado de xadrez perto de cada corte.
    static Color32 Band(float v, int x, int y, float a, float b, Color32 c0, Color32 c1, Color32 c2)
    {
        bool alt = ((x + y) & 1) == 0;
        if (v <= a) return alt && v > a - .06f ? c1 : c0;
        if (v <= b) return alt && v > b - .06f ? c2 : c1;
        return c2;
    }

    // 5 quadros na mesma tela, centro no meio. R = raio do pico da bola de fogo, em pixels.
    static Sprite[] BuildBoom(int diameter)
    {
        float R = diameter * .5f;
        float Rs = R * 1.3f;                       // o anel de fumaca abre mais que a bola
        int n = Mathf.CeilToInt(Rs) * 2 + 4;
        float[] lobesFlash = PixelBlast.Lobes(1, 10, .45f);
        float[] lobesBall = PixelBlast.Lobes(2, 12, .42f);
        float[] lobesBig = PixelBlast.Lobes(3, 14, .55f);
        float[] lobesRing = PixelBlast.Lobes(4, 14, .22f);
        float[] lobesWisp = PixelBlast.Lobes(5, 10, .3f);

        // 0: clarao branco pequeno com pontas
        var f0 = PixelBlast.Paint(n, (dx, dy, x, y) =>
        {
            float v = Dist(dx, dy) / (R * .32f * PixelBlast.Reach(lobesFlash, dx, dy));
            return v <= .6f ? PixelBlast.White : v <= 1f ? PixelBlast.Yellow : PixelBlast.Clear;
        });
        // 1: bola amarela com borda laranja e contorno escuro
        var f1 = PixelBlast.Paint(n, (dx, dy, x, y) =>
        {
            float v = Dist(dx, dy) / (R * .62f * PixelBlast.Reach(lobesBall, dx, dy));
            if (v > 1f) return PixelBlast.Clear;
            if (v <= .2f) return PixelBlast.White;
            return Band(v, x, y, .62f, 1f, PixelBlast.Yellow, PixelBlast.Orange, PixelBlast.Orange);
        });
        PixelBlast.AddOutline(f1, n, PixelBlast.Outline);
        // 2: bola grande laranja, miolo amarelo, pontas, contorno escuro
        var f2 = PixelBlast.Paint(n, (dx, dy, x, y) =>
        {
            float v = Dist(dx, dy) / (R * PixelBlast.Reach(lobesBig, dx, dy));
            if (v > 1f) return PixelBlast.Clear;
            if (v <= .18f) return PixelBlast.White;
            return Band(v, x, y, .5f, .84f, PixelBlast.Yellow, PixelBlast.Orange, PixelBlast.DarkOrange);
        });
        PixelBlast.AddOutline(f2, n, PixelBlast.Outline);
        // 3: anel de fumaca com buracos e algumas brasas no meio
        var f3 = PixelBlast.Paint(n, (dx, dy, x, y) =>
        {
            float v = Dist(dx, dy) / (Rs * PixelBlast.Reach(lobesRing, dx, dy));
            if (v > 1f) return PixelBlast.Clear;
            if (v >= .6f)
            {
                if (PixelBlast.Noise(9, x, y) < .14f) return PixelBlast.Clear;
                return v > .84f ? PixelBlast.SmokeLight : v > .7f ? PixelBlast.SmokeMid : PixelBlast.SmokeDark;
            }
            if (v < .36f && PixelBlast.Noise(11, x, y) < .4f)
                return PixelBlast.Noise(12, x, y) < .5f ? PixelBlast.Orange : PixelBlast.DarkOrange;
            return PixelBlast.Clear;
        });
        // 4: fiapos soltos (pedacinhos de 2x2 espalhados na area do anel)
        var f4 = PixelBlast.Paint(n, (dx, dy, x, y) =>
        {
            float v = Dist(dx, dy) / (Rs * PixelBlast.Reach(lobesWisp, dx, dy));
            if (v < .3f || v > .95f) return PixelBlast.Clear;
            if (PixelBlast.Noise(13, x / 2, y / 2) >= .2f) return PixelBlast.Clear;
            return PixelBlast.Noise(14, x, y) < .5f ? PixelBlast.SmokeLight : PixelBlast.SmokeMid;
        });

        var px = new[] { f0, f1, f2, f3, f4 };
        var sprites = new Sprite[5];
        for (int i = 0; i < 5; i++) sprites[i] = PixelBlast.Make(px[i], n, "boom_" + diameter + "_" + i);
        return sprites;
    }

    static Sprite Square(int n, Color32 color, string name)
    {
        var px = new Color32[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = color;
        return PixelSprites.Make(px, n, n, new Vector2(.5f, .5f), 32f, name);
    }
}
