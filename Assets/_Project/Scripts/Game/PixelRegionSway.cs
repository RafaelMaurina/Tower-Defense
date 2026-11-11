using System.Collections.Generic;
using UnityEngine;

/// Vento em pedacos de um sprite (chapeu, barra da roupa, folha): cada regiao e um retangulo
/// cujas linhas deslocam 1 pixel para o lado na metade solta e ficam paradas na metade presa.
/// Mesma ideia do PixelSway, mas so mexe onde foi marcado.
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

    SpriteRenderer sr;
    Sprite[] frames;
    float phase;
    int shown = -1;

    public void Setup(Sprite source, Region[] regions)
    {
        sr = GetComponent<SpriteRenderer>();
        shown = -1;
        frames = null;
        Vector3 p = transform.position;
        phase = Mathf.Abs(p.x * 0.37f + p.y * 0.61f) % 1f * Sequence.Length * frameTime;
        if (cache.TryGetValue(source, out Sprite[] ready) && ready[0] != null) { frames = ready; return; }

        Color32[] src = PixelSprites.Read(source, out int w, out int h);
        if (src == null) return;
        frames = new Sprite[3];
        for (int f = 0; f < 3; f++)
        {
            int wind = f - 1;
            if (wind == 0) { frames[f] = source; continue; }
            var dst = (Color32[])src.Clone();
            foreach (Region region in regions)
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
                        dst[line + x] = from >= r.x && from < r.x + r.width ? src[line + from] : new Color32(0, 0, 0, 0);
                    }
                }
            }
            frames[f] = PixelSprites.MakeLike(source, dst, w, h, "_vento" + wind);
        }
        cache[source] = frames;
    }

    void Update()
    {
        if (frames == null) return;
        int i = Sequence[(int)((Time.time + phase) / frameTime) % Sequence.Length];
        if (i == shown) return;
        shown = i;
        sr.sprite = frames[i];
    }
}
