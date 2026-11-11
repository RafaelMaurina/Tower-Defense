using System.Collections.Generic;
using UnityEngine;

/// Folhas caindo da arvore. As folhinhas soltas que ja estao desenhadas em volta do tronco saem
/// do sprite e viram objetos: nascem na copa, descem 1 pixel por vez balancando para os lados,
/// ficam um pouco no chao e somem. Serve para qualquer arvore com folhas soltas no desenho.
[RequireComponent(typeof(SpriteRenderer))]
public class FallingLeaves : MonoBehaviour
{
    public int count = 3;
    public float stepTime = .13f;          // segundos por pixel de queda
    // faixa da copa de onde as folhas saem e do chao onde param (x, y de cima)
    public RectInt canopy = new RectInt(14, 34, 36, 6);
    public Vector2Int ground = new Vector2Int(47, 58);

    static readonly Dictionary<Sprite, (Sprite tree, Sprite leaf)> cache = new Dictionary<Sprite, (Sprite, Sprite)>();

    // folha desenhada pendurada na borda da copa: encosta nela, entao a busca por folha solta nao acha.
    // O recorte (x, y de cima) tira so a folha; a linha de contorno da copa logo acima fica.
    static readonly Dictionary<string, RectInt> Hanging = new Dictionary<string, RectInt>
    {
        { "prop_arvore_maca", new RectInt(43, 43, 3, 3) },
        { "prop_arvore_laranja", new RectInt(18, 43, 3, 3) },
    };

    SpriteRenderer sr;
    Sprite source;
    SpriteRenderer[] leaves;
    float[] wait, clock, startX;
    int[] y, endY;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        source = sr.sprite;
        if (!cache.TryGetValue(source, out var made) || made.tree == null)
        {
            Color32[] px = PixelSprites.Read(source, out int w, out int h);
            if (px == null) { enabled = false; return; }
            List<RectInt> loose = LooseLeaves(px, w, h);
            if (loose.Count == 0) { enabled = false; return; }
            Sprite leaf = PixelSprites.Crop(source, px, w, h, loose[0], "folha");
            foreach (RectInt r in loose) PixelSprites.Erase(px, w, h, r);
            if (Hanging.TryGetValue(source.name, out RectInt hanging)) PixelSprites.Erase(px, w, h, hanging);
            made = (PixelSprites.MakeLike(source, px, w, h, "_sem_folhas"), leaf);
            cache[source] = made;
        }
        sr.sprite = made.tree;

        leaves = new SpriteRenderer[count];
        wait = new float[count]; clock = new float[count]; startX = new float[count];
        y = new int[count]; endY = new int[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Folha");
            go.transform.SetParent(transform, false);
            leaves[i] = go.AddComponent<SpriteRenderer>();
            leaves[i].sprite = made.leaf;
            leaves[i].sortingOrder = sr.sortingOrder + 1;
            Restart(i, Random.Range(0f, 4f));
        }
    }

    void Restart(int i, float delay)
    {
        wait[i] = delay;
        startX[i] = Random.Range(canopy.xMin, canopy.xMax);
        y[i] = Random.Range(canopy.yMin, canopy.yMax);
        endY[i] = Random.Range(ground.x, ground.y);
        clock[i] = 0f;
        leaves[i].gameObject.SetActive(false);
    }

    void Update()
    {
        if (leaves == null) return;
        for (int i = 0; i < leaves.Length; i++)
        {
            if (wait[i] > 0f)
            {
                wait[i] -= Time.deltaTime;
                if (wait[i] > 0f) continue;
                if (y[i] >= endY[i]) { Restart(i, Random.Range(1f, 5f)); continue; }   // ja ficou no chao
                leaves[i].gameObject.SetActive(true);
            }
            clock[i] += Time.deltaTime;
            if (clock[i] >= stepTime)
            {
                clock[i] -= stepTime;
                y[i]++;
                if (y[i] >= endY[i]) wait[i] = Random.Range(1.5f, 3f);                // pousa e espera
            }
            float x = startX[i] + Mathf.Round(Mathf.Sin(y[i] * .45f + i) * 2f);
            leaves[i].transform.localPosition = PixelSprites.Snap(PixelSprites.Local(source, x, y[i]));
        }
    }

    /// Grupos pequenos de pixels separados do resto, sem vermelho nem laranja (fruta caida nao conta).
    static List<RectInt> LooseLeaves(Color32[] px, int w, int h)
    {
        var found = new List<RectInt>();
        var seen = new bool[px.Length];
        var stack = new Stack<int>();
        for (int start = 0; start < px.Length; start++)
        {
            if (seen[start] || px[start].a == 0) continue;
            int size = 0, x0 = w, x1 = 0, y0 = h, y1 = 0;
            bool green = true;
            seen[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int cur = stack.Pop(), cx = cur % w, cy = cur / w;
                size++;
                if (px[cur].r > px[cur].g + 40) green = false;      // o contorno escuro passa, fruta nao
                if (cx < x0) x0 = cx;
                if (cx > x1) x1 = cx;
                if (cy < y0) y0 = cy;
                if (cy > y1) y1 = cy;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                        int n = ny * w + nx;
                        if (seen[n] || px[n].a == 0) continue;
                        seen[n] = true;
                        stack.Push(n);
                    }
            }
            if (size <= 20 && green) found.Add(new RectInt(x0, h - 1 - y1, x1 - x0 + 1, y1 - y0 + 1));
        }
        return found;
    }
}
