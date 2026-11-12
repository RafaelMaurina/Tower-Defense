using System.Collections.Generic;
using UnityEngine;

/// Vento em pedacos de um sprite (chapeu, barra da roupa, folha): cada regiao e um retangulo
/// cujas linhas deslocam 1 pixel para o lado na metade solta e ficam paradas na metade presa.
/// Mesma ideia do PixelSway, mas so mexe onde foi marcado.
/// Tambem faz o tranco de um pedaco (o braco do espantalho ao atirar): Kick desloca um retangulo
/// em pixel inteiro por cima do quadro de vento atual, e ClearKick volta ao normal.
[RequireComponent(typeof(SpriteRenderer))]
public class PixelRegionSway : MonoBehaviour
{
    public struct Region
    {
        public RectInt rect;     // x, y de cima, como no Piskel
        public bool hangs;       // true: presa em cima (barra da roupa); false: presa embaixo (chapeu, folha)
        public Region(int x, int y, int w, int h, bool hangs) { rect = new RectInt(x, y, w, h); this.hangs = hangs; }
    }

    public float frameTime = .5f;

    static readonly int[] Sequence = { 1, 2, 1, 0 };     // indices em frames: [-1, 0, +1]
    static readonly Dictionary<Sprite, Sprite[]> cache = new Dictionary<Sprite, Sprite[]>();
    static readonly Dictionary<(Sprite, int, RectInt, Vector2Int), Sprite> kickCache =
        new Dictionary<(Sprite, int, RectInt, Vector2Int), Sprite>();

    SpriteRenderer sr;
    Sprite source;
    Color32[] srcPx;
    int srcW, srcH;
    Region[] regionList;
    Sprite[] frames;
    float phase;
    int shown = -1;
    bool kicked;
    RectInt kickRect;
    Vector2Int kickShift;
    int kickBody;

    public void Setup(Sprite sprite, Region[] regions)
    {
        sr = GetComponent<SpriteRenderer>();
        source = sprite;
        regionList = regions;
        shown = -1;
        kicked = false;
        frames = null;
        Vector3 p = transform.position;
        phase = Mathf.Abs(p.x * 0.37f + p.y * 0.61f) % 1f * Sequence.Length * frameTime;

        srcPx = PixelSprites.Read(source, out srcW, out srcH);
        if (srcPx == null) return;
        if (cache.TryGetValue(source, out Sprite[] ready) && ready[0] != null) { frames = ready; return; }

        frames = new Sprite[3];
        for (int f = 0; f < 3; f++)
            frames[f] = f == 1 ? source : PixelSprites.MakeLike(source, WindPixels(f - 1), srcW, srcH, "_vento" + (f - 1));
        cache[source] = frames;
    }

    Color32[] WindPixels(int wind)
    {
        var dst = (Color32[])srcPx.Clone();
        int w = srcW, h = srcH;
        foreach (Region region in regionList)
        {
            RectInt r = region.rect;
            for (int row = 0; row < r.height; row++)
            {
                float k = r.height <= 1 ? 1f : (region.hangs ? row : r.height - 1 - row) / (float)(r.height - 1);
                if (k < .5f) continue;                       // metade presa nao se mexe
                int line = (h - 1 - (r.y + row)) * w;
                for (int x = r.x; x < r.x + r.width; x++)
                {
                    int from = x - wind;
                    dst[line + x] = from >= r.x && from < r.x + r.width ? srcPx[line + from] : new Color32(0, 0, 0, 0);
                }
            }
        }
        return dst;
    }

    /// Desloca o retangulo (x, y de cima) por shift pixels enquanto o tranco durar. bodyDx: de que lado
    /// fica o corpo (+1 direita, -1 esquerda), para a manga acompanhar o braco.
    public void Kick(RectInt rect, Vector2Int shift, int bodyDx)
    {
        if (frames == null) return;
        kicked = true;
        kickRect = rect;
        kickShift = shift;
        kickBody = bodyDx;
        shown = -1;
    }

    public void ClearKick()
    {
        kicked = false;
        shown = -1;
    }

    Sprite KickFrame(int windIndex)
    {
        var key = (source, windIndex, kickRect, kickShift);
        if (kickCache.TryGetValue(key, out Sprite s) && s != null) return s;
        Color32[] basePx = windIndex == 1 ? srcPx : WindPixels(windIndex - 1);
        Color32[] px = PixelSprites.ShiftRegion(basePx, srcW, srcH, kickRect, kickShift, kickBody);
        s = PixelSprites.MakeLike(source, px, srcW, srcH, "_tranco");
        kickCache[key] = s;
        return s;
    }

    void Update()
    {
        if (frames == null) return;
        int i = Sequence[(int)((Time.time + phase) / frameTime) % Sequence.Length];
        if (i == shown) return;
        shown = i;
        sr.sprite = kicked ? KickFrame(i) : frames[i];
    }
}
