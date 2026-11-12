using System;
using UnityEngine;

/// Formas de explosao em pixel art desenhadas em codigo (BoomFx e clarao do canhao).
/// Borda irregular com ruido fixo por semente: o mesmo desenho sai igual a cada vez.
/// Todas as telas sao de tamanho par, com o centro no cruzamento de 4 pixels (alinhado a grade).
public static class PixelBlast
{
    public static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    public static readonly Color32 White = new Color32(0xff, 0xff, 0xff, 255);
    public static readonly Color32 Yellow = new Color32(0xff, 0xe7, 0x31, 255);
    public static readonly Color32 Orange = new Color32(0xf7, 0x96, 0x17, 255);
    public static readonly Color32 DarkOrange = new Color32(0xd5, 0x77, 0x26, 255);
    public static readonly Color32 Outline = new Color32(0x20, 0x18, 0x21, 255);
    public static readonly Color32 SmokeLight = new Color32(0xd9, 0xd5, 0xd5, 255);
    public static readonly Color32 SmokeMid = new Color32(0xa2, 0xa2, 0xa2, 255);
    public static readonly Color32 SmokeDark = new Color32(0x68, 0x5b, 0x5b, 255);
    public static readonly Color32 Earth = new Color32(0xcf, 0x93, 0x3b, 255);
    public static readonly Color32 EarthDark = new Color32(0xca, 0x86, 0x22, 255);

    /// Ruido fixo de 0 a 1 para a semente e o par (a, b).
    public static float Noise(int seed, int a, int b)
    {
        unchecked
        {
            uint h = (uint)(seed * 374761393 + a * 668265263 + b * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }
    }

    /// Raio relativo (de 1 - amp ate 1) em 'bins' direcoes ao redor do centro.
    public static float[] Lobes(int seed, int bins, float amp)
    {
        var l = new float[bins];
        for (int i = 0; i < bins; i++) l[i] = 1f - amp * Noise(seed, i, 0);
        return l;
    }

    /// Raio relativo na direcao (dx, dy), interpolando entre as pontas.
    public static float Reach(float[] lobes, float dx, float dy)
    {
        int n = lobes.Length;
        float p = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI) * n;
        int i = Mathf.FloorToInt(p);
        float f = p - i;
        int i0 = ((i % n) + n) % n;
        return Mathf.Lerp(lobes[i0], lobes[(i0 + 1) % n], f);
    }

    /// Pinta uma tela n x n. shade recebe (dx, dy do centro, x, y do pixel; y de baixo).
    public static Color32[] Paint(int n, Func<float, float, int, int, Color32> shade)
    {
        var px = new Color32[n * n];
        float c = (n - 1) * .5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = shade(x - c, y - c, x, y);
        return px;
    }

    /// Contorno de 1 pixel nos vazios encostados em pixel pintado (4 vizinhos).
    public static void AddOutline(Color32[] px, int n, Color32 color)
    {
        var copy = (Color32[])px.Clone();
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (copy[y * n + x].a != 0) continue;
                bool near = (x > 0 && copy[y * n + x - 1].a != 0) || (x < n - 1 && copy[y * n + x + 1].a != 0)
                         || (y > 0 && copy[(y - 1) * n + x].a != 0) || (y < n - 1 && copy[(y + 1) * n + x].a != 0);
                if (near) px[y * n + x] = color;
            }
    }

    /// Desenho por texto: '.' vazio e uma letra por cor. Primeira linha do texto = topo.
    public static Sprite FromRows(string[] rows, Func<char, Color32> palette, string name)
    {
        int h = rows.Length, w = rows[0].Length;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = rows[h - 1 - y][x] == '.' ? Clear : palette(rows[h - 1 - y][x]);
        return PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 32f, name);
    }

    public static Sprite Make(Color32[] px, int n, string name) =>
        PixelSprites.Make(px, n, n, new Vector2(.5f, .5f), 32f, name);
}
