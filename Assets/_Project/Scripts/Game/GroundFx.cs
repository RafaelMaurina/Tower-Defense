using System.Collections.Generic;
using UnityEngine;

/// Marcas no chao: poeira atras do Tufao quando ele corre e pegadas dos inimigos na estrada, que
/// vao sumindo. Liga sozinho (um objeto que vive entre as cenas) e olha os bichos a cada quadro,
/// sem mexer no codigo dos inimigos nem do Tufao. Tudo em pixel inteiro.
public class GroundFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float DustEvery = .4f;          // casas andadas entre uma nuvem e outra
    const float PrintEvery = .5f;         // casas andadas entre uma pegada e outra
    const float PrintLife = 4f;           // segundos ate a pegada sumir
    const int MaxPrints = 140;

    static GroundFx instance;
    static Sprite[] puffs;                // 3 quadros: nasce, cresce, se desfaz
    static Sprite print;

    class Track
    {
        public Vector3 last;
        public float walked;
        public int side = 1;
        public int stamp;
    }

    class Print
    {
        public SpriteRenderer sr;
        public float born;
    }

    readonly Dictionary<Enemy, Track> enemies = new Dictionary<Enemy, Track>();
    readonly Dictionary<Guardian, Track> guardians = new Dictionary<Guardian, Track>();
    readonly Queue<Print> prints = new Queue<Print>();
    readonly List<Enemy> stale = new List<Enemy>();
    int frame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        instance = new GameObject("GroundFx").AddComponent<GroundFx>();
        DontDestroyOnLoad(instance.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        puffs = null;
        print = null;
    }

    /// Nuvenzinha de poeira em 'pos' (os pes do bicho); 'order' e a ordem de desenho.
    public static void Puff(Vector3 pos, int order)
    {
        Create();
        EnsureSprites();
        instance.StartCoroutine(instance.PlayPuff(PixelSprites.Snap(pos), order));
    }

    System.Collections.IEnumerator PlayPuff(Vector3 pos, int order)
    {
        var go = new GameObject("Poeira");
        go.transform.SetParent(transform);
        var r = go.AddComponent<SpriteRenderer>();
        r.sortingOrder = order;
        for (int i = 0; i < puffs.Length; i++)
        {
            r.sprite = puffs[i];
            go.transform.position = pos + new Vector3(0f, i * Pixel, 0f);
            yield return new WaitForSeconds(.1f);
        }
        Destroy(go);
    }

    void Update()
    {
        frame++;
        TrackGuardians();
        TrackEnemies();
        FadePrints();
        if (frame % 120 == 0) Cleanup();
    }

    void TrackGuardians()
    {
        foreach (Guardian g in Guardian.All)
        {
            if (g == null) continue;
            if (!guardians.TryGetValue(g, out Track t))
            {
                t = new Track { last = g.transform.position };
                guardians[g] = t;
            }
            Vector3 p = g.transform.position;
            t.walked += Vector3.Distance(p, t.last);
            t.last = p;
            t.stamp = frame;
            if (t.walked >= DustEvery)
            {
                t.walked = 0f;
                var sr = g.GetComponent<SpriteRenderer>();
                Puff(p, sr != null ? sr.sortingOrder - 1 : 100);
            }
        }
    }

    void TrackEnemies()
    {
        var all = EnemyRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            Enemy e = all[i];
            if (e == null || !e.IsAlive || e.Flying) continue;   // quem voa nao pisa no chao
            if (!enemies.TryGetValue(e, out Track t))
            {
                t = new Track { last = e.Transform.position };
                enemies[e] = t;
            }
            Vector3 p = e.Transform.position;
            Vector3 d = p - t.last;
            t.stamp = frame;
            if (d.sqrMagnitude < 1e-8f) continue;
            t.walked += d.magnitude;
            if (t.walked >= PrintEvery)
            {
                t.walked = 0f;
                t.side = -t.side;
                // cada pe de um lado do caminho: desloca para o lado, perpendicular ao rumo
                Vector3 dir = d.normalized;
                Vector3 side = new Vector3(-dir.y, dir.x, 0f) * (3f * Pixel) * t.side;
                DropPrint(p + side);
            }
            t.last = p;
        }
    }

    void DropPrint(Vector3 pos)
    {
        EnsureSprites();
        Print pr;
        if (prints.Count >= MaxPrints) pr = prints.Dequeue();   // reaproveita a mais antiga
        else
        {
            var go = new GameObject("Pegada");
            go.transform.SetParent(transform);
            pr = new Print { sr = go.AddComponent<SpriteRenderer>() };
            pr.sr.sprite = print;
            pr.sr.sortingOrder = 2;                            // acima do chao e dos canteiros, abaixo de tudo em pe
        }
        pr.sr.transform.position = PixelSprites.Snap(pos);
        pr.sr.color = Color.white;
        pr.sr.enabled = true;
        pr.born = Time.time;
        prints.Enqueue(pr);
    }

    void FadePrints()
    {
        foreach (Print pr in prints)
        {
            if (!pr.sr.enabled) continue;
            float age = (Time.time - pr.born) / PrintLife;
            if (age >= 1f) { pr.sr.enabled = false; continue; }
            // some em 4 passos, nao continuo, para combinar com o resto da arte
            float a = 1f - Mathf.Floor(age * 4f) / 4f;
            pr.sr.color = new Color(1f, 1f, 1f, a);
        }
    }

    void Cleanup()
    {
        stale.Clear();
        foreach (var kv in enemies) if (kv.Key == null || kv.Value.stamp < frame - 60) stale.Add(kv.Key);
        foreach (var e in stale) enemies.Remove(e);
        var gone = new List<Guardian>();
        foreach (var kv in guardians) if (kv.Key == null || kv.Value.stamp < frame - 60) gone.Add(kv.Key);
        foreach (var g in gone) guardians.Remove(g);
    }

    static void EnsureSprites()
    {
        if (puffs != null && puffs[0] != null && print != null) return;
        var c = new Color32(0xee, 0xd4, 0xa0, 210);             // poeira clara de terra
        var o = new Color32(0, 0, 0, 0);
        puffs = new[]
        {
            Grid(new[] { ".X.", "XXX" }, c, o, "poeira_1"),
            Grid(new[] { ".XXX.", "XXXXX", ".XXX." }, c, o, "poeira_2"),
            Grid(new[] { "X.X.X", ".X.X.", "X...X" }, c, o, "poeira_3"),
        };
        // pegada de 2x2 num marrom um pouco mais escuro que a estrada
        var d = new Color32(0xa8, 0x6a, 0x22, 255);
        print = Grid(new[] { "XX", "XX" }, d, o, "pegada");
    }

    static Sprite Grid(string[] rows, Color32 on, Color32 off, string name)
    {
        int h = rows.Length, w = rows[0].Length;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = rows[h - 1 - y][x] == 'X' ? on : off;
        return PixelSprites.Make(px, w, h, new Vector2(.5f, 0f), 32f, name);
    }
}
