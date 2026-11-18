using UnityEngine;

/// O "0" vermelho que aparece em cima da torre quando o Prof Rieder da nota 0 nela.
/// Desenhado em codigo (5x7 pixels, contorno escuro), no mesmo grao de pixel do resto do jogo (32 por unidade).
public static class ZeroMark
{
    const float Pixel = 1f / 32f;
    static Sprite sprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => sprite = null;

    public static GameObject Create(Transform tower)
    {
        var go = new GameObject("NotaZero");
        go.transform.SetParent(tower, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Build();

        var towerSr = tower.GetComponent<SpriteRenderer>();
        float top = towerSr != null ? towerSr.bounds.max.y : tower.position.y + 1f;
        go.transform.position = new Vector3(tower.position.x, top + .45f, 0f);
        go.transform.localScale = Vector3.one * 2f;   // cada pixel do 0 fica com 2 pixels do jogo
        if (towerSr != null)
        {
            sr.sortingLayerID = towerSr.sortingLayerID;
            sr.sortingOrder = towerSr.sortingOrder + 50;   // sempre acima da torre e dos vizinhos
        }
        return go;
    }

    static Sprite Build()
    {
        if (sprite != null) return sprite;
        string[] rows =
        {
            ".###.",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            "##.##",
            ".###.",
        };
        const int w = 7, h = 9;   // 5x7 mais 1 pixel de contorno em volta
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color32(0, 0, 0, 0);
        var outline = new Color32(40, 8, 8, 255);
        var red = new Color32(230, 30, 30, 255);
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = clear;
        for (int r = 0; r < 7; r++)
            for (int c = 0; c < 5; c++)
                if (rows[r][c] == '#')
                    px[(h - 2 - r) * w + (c + 1)] = red;
        // contorno: todo pixel vazio encostado (8 vizinhos) num vermelho
        var final = (Color32[])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a != 0) continue;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && px[ny * w + nx].a != 0) final[y * w + x] = outline;
                    }
            }
        tex.SetPixels32(final);
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, 0f), 1f / Pixel);
        return sprite;
    }
}
