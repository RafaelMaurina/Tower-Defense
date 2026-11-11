using UnityEngine;

/// Ajuda para animacao em pixel art feita em codigo: le os pixels de um sprite e monta outro
/// com o mesmo pivo e PPU. O PNG nunca muda; tudo fica na memoria.
/// As coordenadas dos efeitos usam o mesmo sistema do Piskel: x da esquerda, y de cima.
public static class PixelSprites
{
    public const float Pixel = 1f / 32f;

    /// Pixels do sprite, linha 0 embaixo (como o Unity guarda). Null se a textura nao tem Read/Write.
    public static Color32[] Read(Sprite s, out int w, out int h)
    {
        w = h = 0;
        if (s == null || !s.texture.isReadable)
        {
            if (s != null) Debug.LogWarning("Ligue Read/Write na textura de " + s.name);
            return null;
        }
        w = (int)s.rect.width;
        h = (int)s.rect.height;
        Color[] c = s.texture.GetPixels((int)s.rect.x, (int)s.rect.y, w, h);
        var px = new Color32[c.Length];
        for (int i = 0; i < c.Length; i++) px[i] = c[i];
        return px;
    }

    public static Sprite Make(Color32[] px, int w, int h, Vector2 pivot01, float ppu, string name)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot01, ppu, 0, SpriteMeshType.FullRect);
        s.name = name;
        return s;
    }

    /// Copia com o mesmo pivo e PPU do original.
    public static Sprite MakeLike(Sprite source, Color32[] px, int w, int h, string suffix) =>
        Make(px, w, h, new Vector2(source.pivot.x / w, source.pivot.y / h), source.pixelsPerUnit, source.name + suffix);

    /// Recorte (x, y de cima) como sprite proprio, com pivo no centro.
    public static Sprite Crop(Sprite source, Color32[] px, int w, int h, RectInt r, string name)
    {
        var part = new Color32[r.width * r.height];
        for (int y = 0; y < r.height; y++)          // y = linha do recorte, contando de cima
            for (int x = 0; x < r.width; x++)
                part[(r.height - 1 - y) * r.width + x] = px[(h - 1 - (r.y + y)) * w + r.x + x];
        return Make(part, r.width, r.height, new Vector2(.5f, .5f), source.pixelsPerUnit, name);
    }

    public static void Erase(Color32[] px, int w, int h, RectInt r)
    {
        for (int y = r.y; y < r.y + r.height; y++)
            for (int x = r.x; x < r.x + r.width; x++)
                px[(h - 1 - y) * w + x] = new Color32(0, 0, 0, 0);
    }

    /// Posicao local de um pixel do desenho (x, y de cima) em relacao ao pivo do sprite.
    public static Vector3 Local(Sprite s, float x, float y) =>
        new Vector3((x - s.pivot.x) / s.pixelsPerUnit, (s.rect.height - y - s.pivot.y) / s.pixelsPerUnit, 0f);

    public static Vector3 Snap(Vector3 v) =>
        new Vector3(Mathf.Round(v.x / Pixel) * Pixel, Mathf.Round(v.y / Pixel) * Pixel, 0f);
}
