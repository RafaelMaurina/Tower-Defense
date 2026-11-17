using UnityEngine;

/// Ajuda dos efeitos de morte, moeda e upgrade: monta sprites a partir de texto, com contorno
/// opcional, e le os pixels de um sprite mesmo sem Read/Write (copia pela GPU).
/// Letras: o 201821, l d9d5d5, m a2a2a2, w f7f3e6, Y ffe731, y ffc500, b b76705. '.' = vazio.
public static class FxPixels
{
    public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    public static readonly Color32 Outline = new Color32(0x20, 0x18, 0x21, 255);
    public static readonly Color32 Cream = new Color32(0xf7, 0xf3, 0xe6, 255);

    static Color32 Paint(char c)
    {
        switch (c)
        {
            case 'o': return Outline;
            case 'l': return new Color32(0xd9, 0xd5, 0xd5, 255);
            case 'm': return new Color32(0xa2, 0xa2, 0xa2, 255);
            case 'w': return Cream;
            case 'Y': return new Color32(0xff, 0xe7, 0x31, 255);
            case 'y': return new Color32(0xff, 0xc5, 0x00, 255);
            case 'b': return new Color32(0xb7, 0x67, 0x05, 255);
            default: return Clear;
        }
    }

    /// Sprite do texto (primeira linha = topo). O pivo e um pixel inteiro (px, py contados de baixo
    /// e da esquerda, sem o contorno); px < 0 usa o centro. Com contorno o desenho ganha 1 pixel em volta.
    public static Sprite Rows(string[] rows, string name, int px, int py, bool outline)
    {
        int h = rows.Length, w = rows[0].Length;
        if (px < 0) { px = w / 2; py = h / 2; }
        int m = outline ? 1 : 0, ow = w + 2 * m, oh = h + 2 * m;
        var fill = new Color32[ow * oh];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                fill[(h - 1 - j + m) * ow + i + m] = Paint(rows[j][i]);
        var all = (Color32[])fill.Clone();
        if (outline)
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    if (fill[y * ow + x].a != 0) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < ow && ny < oh && fill[ny * ow + nx].a != 0) { near = true; break; }
                        }
                    if (near) all[y * ow + x] = Outline;
                }
        return PixelSprites.Make(all, ow, oh, new Vector2((px + m) / (float)ow, (py + m) / (float)oh), 32f, name);
    }

    /// Pixels do sprite (linha 0 embaixo). Se a textura nao e legivel, copia pela GPU.
    public static Color32[] ReadAny(Sprite s, out int w, out int h)
    {
        w = h = 0;
        if (s == null) return null;
        Texture2D tex = s.texture;
        Rect r = s.textureRect;
        w = (int)r.width;
        h = (int)r.height;
        if (tex.isReadable)
        {
            Color[] c = tex.GetPixels((int)r.x, (int)r.y, w, h);
            var px = new Color32[c.Length];
            for (int i = 0; i < c.Length; i++) px[i] = c[i];
            return px;
        }
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0);
        Color32[] result = copy.GetPixels32();
        Object.Destroy(copy);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    /// Copia do sprite toda em creme (silhueta), com o mesmo pivo e PPU. Null se nao deu para ler.
    public static Sprite Silhouette(Sprite source)
    {
        Color32[] px = ReadAny(source, out int w, out int h);
        if (px == null) return null;
        for (int i = 0; i < px.Length; i++) px[i] = px[i].a >= 128 ? Cream : Clear;
        return PixelSprites.MakeLike(source, px, w, h, "_flash");
    }
}
