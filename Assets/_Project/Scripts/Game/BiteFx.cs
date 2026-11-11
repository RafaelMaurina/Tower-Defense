using System.Collections;
using UnityEngine;

/// Resposta visual da mordida do Tufao: um brilhozinho branco de 2 quadros e um "NHAC" em
/// letras de pixel (fonte 3x5 feita a mao) que sobe uns pixels e some.
/// Liga sozinho: escuta Guardian.OnBite. Tudo em pixel inteiro, sem escala nem rotacao.
public class BiteFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;

    static BiteFx runner;
    static Sprite[] sparkle;
    static Sprite text;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        sparkle = null;
        text = null;
        Guardian.OnBite -= HandleBite;
        Guardian.OnBite += HandleBite;
    }

    static void HandleBite(Guardian guardian, Vector3 where)
    {
        if (runner == null)
        {
            runner = new GameObject("BiteFx").AddComponent<BiteFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.StartCoroutine(runner.Play(where));
    }

    IEnumerator Play(Vector3 where)
    {
        EnsureSprites();
        Vector3 basePos = PixelSprites.Snap(where);
        SpriteRenderer spark = NewRenderer("Brilho", DrawOrder.Projectile);
        SpriteRenderer word = NewRenderer("Nhac", DrawOrder.Projectile + 1);
        word.sprite = text;
        spark.transform.position = basePos;

        // 5 passos de 0,07 s = 0,35 s; o brilho dura os 2 primeiros, o texto sobe 4 pixels
        for (int step = 0; step < 5; step++)
        {
            spark.sprite = step < sparkle.Length ? sparkle[step] : null;
            word.transform.position = basePos + new Vector3(0f, (10 + step) * Pixel, 0f);
            yield return new WaitForSeconds(.07f);
        }
        Destroy(spark.gameObject);
        Destroy(word.gameObject);
    }

    SpriteRenderer NewRenderer(string objName, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }

    // Fonte 3x5: so as letras de NHAC
    static readonly string[][] Font =
    {
        new[] { "X.X", "XXX", "XXX", "X.X", "X.X" },   // N
        new[] { "X.X", "X.X", "XXX", "X.X", "X.X" },   // H
        new[] { ".X.", "X.X", "XXX", "X.X", "X.X" },   // A
        new[] { "XXX", "X..", "X..", "X..", "XXX" },   // C
    };

    static void EnsureSprites()
    {
        if (text != null && sparkle != null && sparkle[0] != null) return;
        var white = new Color32(255, 255, 255, 255);
        var dark = new Color32(0x20, 0x18, 0x21, 255);
        var none = new Color32(0, 0, 0, 0);

        // brilho: estrela de 5x5 e depois cruz de 3x3
        sparkle = new[]
        {
            Grid(new[] { "..w..", ".www.", "wwwww", ".www.", "..w.." }, white, "mordida_1"),
            Grid(new[] { ".w.", "www", ".w." }, white, "mordida_2"),
        };

        // texto: 4 letras de 3 pixels com 1 de espaco, mais 1 pixel de contorno em volta
        int w = 4 * 3 + 3 + 2, h = 5 + 2;
        var fill = new bool[w, h];                 // y = 0 embaixo
        for (int l = 0; l < 4; l++)
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 3; x++)
                    if (Font[l][y][x] == 'X') fill[1 + l * 4 + x, 1 + (4 - y)] = true;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (fill[x, y]) { px[y * w + x] = white; continue; }
                bool near = false;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && fill[nx, ny]) near = true;
                    }
                px[y * w + x] = near ? dark : none;
            }
        text = PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 32f, "nhac");
    }

    static Sprite Grid(string[] rows, Color32 color, string name)
    {
        int h = rows.Length, w = rows[0].Length;
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = rows[h - 1 - y][x] == 'w' ? color : new Color32(0, 0, 0, 0);
        return PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 32f, name);
    }
}
