using System.Collections.Generic;
using UnityEngine;

/// Balanco de vento para pixel art: em vez de girar ou esticar o sprite (o que quebra a grade
/// de pixel), monta 3 versoes dele com as linhas de cima deslocadas em pixels inteiros e troca
/// entre elas. O PNG original nao muda; as versoes sao criadas na memoria ao iniciar.
///
/// So balanca o que e planta: o fundo (transparente, ou igual ao sprite 'background', como a
/// terra do canteiro) fica parado, e os tons de laranja/vermelho (cenoura, abobora) tambem.
/// A textura precisa estar com Read/Write ligado no import.
[RequireComponent(typeof(SpriteRenderer))]
public class PixelSway : MonoBehaviour
{
    public Sprite background;            // opcional: o que estiver igual a este sprite nao se mexe
    public int amplitude = 1;            // pixels de desenho que o topo desloca para cada lado
    [Range(0f, .9f)] public float fixedFraction = .35f;   // parte de baixo de cada planta que fica parada
    public float frameTime = .45f;       // segundos por quadro

    static readonly int[] Sequence = { 1, 2, 1, 0 };     // indices em frames: [-1, 0, +1]
    static readonly Dictionary<(Sprite, Sprite, int, float), Sprite[]> cache =
        new Dictionary<(Sprite, Sprite, int, float), Sprite[]>();

    SpriteRenderer sr;
    Sprite[] frames;
    float phase;
    int shown = -1;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        frames = FramesFor(sr.sprite);
        // cada planta comeca num ponto diferente do ciclo, conforme a posicao no mapa
        Vector3 p = transform.position;
        phase = Mathf.Abs(p.x * 0.37f + p.y * 0.61f) % 1f * Sequence.Length * frameTime;
    }

    void Update()
    {
        if (frames == null) return;
        int i = Sequence[(int)((Time.time + phase) / frameTime) % Sequence.Length];
        if (i == shown) return;
        shown = i;
        sr.sprite = frames[i];
    }

    Sprite[] FramesFor(Sprite source)
    {
        if (source == null || !source.texture.isReadable ||
            (background != null && !background.texture.isReadable))
        {
            Debug.LogWarning("PixelSway: ligue Read/Write na textura de " + name, this);
            return null;
        }
        var key = (source, background, amplitude, fixedFraction);
        if (cache.TryGetValue(key, out Sprite[] ready) && ready[0] != null) return ready;

        Rect r = source.rect;
        int w = (int)r.width, h = (int)r.height;
        Color32[] src = Pixels(source, w, h);
        Color32[] bg = background != null && (int)background.rect.width == w && (int)background.rect.height == h
            ? Pixels(background, w, h) : null;

        // quem balanca: nao e fundo e nao e laranja/vermelho
        var movable = new bool[w * h];
        for (int i = 0; i < src.Length; i++)
            movable[i] = !IsBackground(src, bg, i) && !(src[i].r > 1.6f * src[i].g);

        // cada planta e um grupo de pixels ligados; guarda o pe e o topo de cada uma
        var plant = new int[w * h];
        for (int i = 0; i < plant.Length; i++) plant[i] = -1;
        var bottom = new List<int>();
        var top = new List<int>();
        var stack = new Stack<int>();
        for (int start = 0; start < plant.Length; start++)
        {
            if (!movable[start] || plant[start] >= 0) continue;
            int id = bottom.Count, lo = start / w, hi = lo;
            plant[start] = id;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int cur = stack.Pop(), cx = cur % w, cy = cur / w;
                if (cy < lo) lo = cy;
                if (cy > hi) hi = cy;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        int n = ny * w + nx;
                        if (!movable[n] || plant[n] >= 0) continue;
                        plant[n] = id;
                        stack.Push(n);
                    }
            }
            bottom.Add(lo);
            top.Add(hi);
        }

        var result = new Sprite[3];
        for (int f = 0; f < 3; f++)
        {
            int wind = f - 1;       // -1, 0, +1
            if (wind == 0) { result[f] = source; continue; }
            var dst = (Color32[])src.Clone();
            var shift = new int[w * h];
            for (int i = 0; i < src.Length; i++)
            {
                if (!movable[i]) continue;
                int lo = bottom[plant[i]], height = Mathf.Max(1, top[plant[i]] - lo);
                float k = ((i / w - lo) / (float)height - fixedFraction) / (1f - fixedFraction);   // 0 no pe, 1 no topo
                shift[i] = Mathf.RoundToInt(wind * amplitude * Mathf.Max(0f, k));
            }
            // apaga o que sai do lugar: aparece a planta parada que estava ao lado, ou o fundo
            for (int i = 0; i < src.Length; i++)
            {
                if (shift[i] == 0) continue;
                int x = i % w, from = x - wind;
                if (from >= 0 && from < w && !movable[i - wind] && !IsBackground(src, bg, i - wind)) dst[i] = src[i - wind];
                else dst[i] = bg != null ? bg[i] : new Color32(0, 0, 0, 0);
            }
            for (int i = 0; i < src.Length; i++)
            {
                if (shift[i] == 0) continue;
                int x = i % w + shift[i];
                if (x >= 0 && x < w) dst[i + shift[i]] = src[i];
            }
            for (int i = 0; i < src.Length; i++)
                if (movable[i] && shift[i] == 0) dst[i] = src[i];

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = source.name + "_vento" };
            tex.SetPixels32(dst);
            tex.Apply(false, true);
            result[f] = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(source.pivot.x / w, source.pivot.y / h), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            result[f].name = source.name + "_vento" + wind;
        }
        cache[key] = result;
        return result;
    }

    static bool IsBackground(Color32[] src, Color32[] bg, int i)
    {
        if (src[i].a == 0) return true;
        return bg != null && src[i].r == bg[i].r && src[i].g == bg[i].g && src[i].b == bg[i].b;
    }

    static Color32[] Pixels(Sprite s, int w, int h)
    {
        Color[] c = s.texture.GetPixels((int)s.rect.x, (int)s.rect.y, w, h);
        var result = new Color32[c.Length];
        for (int i = 0; i < c.Length; i++) result[i] = c[i];
        return result;
    }
}
