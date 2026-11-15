using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Torre que sobe de nivel: um anel dourado no pe (3 quadros, 6 passos), setinhas e estrelas
/// com contorno subindo em volta e a torre "pulando" 2 px e voltando. Dura ~0,6 s.
/// Liga sozinho: escuta Tower.OnUpgraded (so ao melhorar, nao ao construir).
/// So mexe na posicao da torre (nunca no sprite). Se o TowerFireFx mover a torre no meio do pulo,
/// o pulo para e no fim a altura e conferida, para a torre nunca ficar fora do lugar.
/// Pixel inteiro, sem escala nem rotacao. Sprites em cache e renderers em pool.
public class UpgradeFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float Tick = .05f;
    const int Ticks = 12;
    const int PartCount = 8;
    const int PartLife = 7;

    static readonly int[] Hop = { 1, 2, 2, 1, 0 };                 // pixels acima do lugar, por passo
    static readonly int[] RingAt = { 0, 0, 1, 1, 2, 2, -1, -1, -1, -1, -1, -1 };
    static readonly float[] RingAlpha = { 1f, 1f, 1f, 1f, .66f, .33f };
    static readonly int[] PartX = { -20, 14, -8, 22, -26, 6, 28, -14 };   // para uma torre de 64 px de largura
    static readonly WaitForSeconds TickWait = new WaitForSeconds(Tick);

    static UpgradeFx runner;
    static Sprite arrow, star, dot;
    static readonly Dictionary<int, Sprite[]> rings = new Dictionary<int, Sprite[]>();
    readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        arrow = star = dot = null;
        rings.Clear();
        Tower.OnUpgraded -= HandleUpgraded;
        Tower.OnUpgraded += HandleUpgraded;
    }

    static void HandleUpgraded(Tower tower)
    {
        if (tower == null) return;
        if (tower.GetComponent<SpriteRenderer>() == null) return;
        if (runner == null)
        {
            runner = new GameObject("UpgradeFx").AddComponent<UpgradeFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.StartCoroutine(runner.Play(tower));
    }

    IEnumerator Play(Tower tower)
    {
        EnsureSprites();
        var sr = tower.GetComponent<SpriteRenderer>();
        int order = sr.sortingOrder;
        int width = sr.sprite != null ? Mathf.RoundToInt(sr.sprite.rect.width) : 64;
        Vector3 basePos = tower.transform.position;
        Vector3 foot = PixelSprites.Snap(basePos);          // o pivo da torre e o pe
        Sprite[] ringSprites = RingsFor(width);

        SpriteRenderer ring = Rent("Anel", order - 1);
        var part = new SpriteRenderer[PartCount];
        var px = new int[PartCount];
        var py = new int[PartCount];
        for (int i = 0; i < PartCount; i++)
        {
            part[i] = Rent("Brilho", order + 2);
            part[i].enabled = false;
            px[i] = Mathf.RoundToInt(PartX[i] * width / 64f);
            py[i] = Random.Range(6, 27);
        }

        Vector3 written = basePos;
        bool hopping = true;
        for (int t = 0; t < Ticks; t++)
        {
            if (tower == null) break;                       // vendida no meio do efeito

            if (RingAt[t] >= 0)
            {
                ring.enabled = true;
                ring.sprite = ringSprites[RingAt[t]];
                ring.color = new Color(1f, 1f, 1f, RingAlpha[t]);
                ring.transform.position = foot + new Vector3(0f, -Pixel, 0f);
            }
            else ring.enabled = false;

            // pulo: so escreve se ninguem mais mexeu na torre desde o ultimo passo
            if (hopping && t < Hop.Length)
            {
                if ((tower.transform.position - written).sqrMagnitude > 1e-8f) hopping = false;
                else
                {
                    written = basePos + new Vector3(0f, Hop[t] * Pixel, 0f);
                    tower.transform.position = written;
                }
            }

            for (int i = 0; i < PartCount; i++)
            {
                int k = t - i % 5;                          // cada brilho nasce num passo
                if (k < 0 || k >= PartLife) { part[i].enabled = false; continue; }
                int rise = Mathf.RoundToInt(3f * k - .25f * k * k);
                float alpha = k == PartLife - 2 ? .66f : (k == PartLife - 1 ? .33f : 1f);
                part[i].enabled = true;
                part[i].sprite = i % 2 == 0 ? arrow : (k % 2 == 0 ? star : dot);
                part[i].color = new Color(1f, 1f, 1f, alpha);
                part[i].transform.position = foot + new Vector3(px[i] * Pixel, (py[i] + rise) * Pixel, 0f);
            }
            yield return TickWait;
        }

        // se um recuo do TowerFireFx guardou a posicao do pulo como descanso, devolve ao lugar
        if (tower != null)
        {
            Vector3 p = tower.transform.position;
            float dy = (p.y - basePos.y) / Pixel;
            if (Mathf.Abs(p.x - basePos.x) < .001f && dy > .5f && dy < 2.5f) tower.transform.position = basePos;
        }
        Give(ring);
        for (int i = 0; i < PartCount; i++) Give(part[i]);
    }

    SpriteRenderer Rent(string objName, int order)
    {
        SpriteRenderer sr = null;
        while (free.Count > 0 && sr == null) sr = free.Pop();
        if (sr == null)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform);
            sr = go.AddComponent<SpriteRenderer>();
        }
        sr.sortingOrder = order;
        sr.color = Color.white;
        sr.enabled = true;
        return sr;
    }

    void Give(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.enabled = false;
        free.Push(sr);
    }

    static void EnsureSprites()
    {
        if (arrow != null) return;
        arrow = FxPixels.Rows(new[] { ".y.", "yYy", ".Y.", ".Y." }, "seta_upgrade", -1, 0, true);
        star = FxPixels.Rows(new[] { ".Y.", "YwY", ".Y." }, "estrela_upgrade", -1, 0, true);
        dot = FxPixels.Rows(new[] { "Y" }, "ponto_upgrade", -1, 0, true);
    }

    // 3 aneis de 2 px de espessura (Y por fora, y por dentro), do tamanho da torre; elipse achatada
    static Sprite[] RingsFor(int width)
    {
        if (rings.TryGetValue(width, out Sprite[] cached) && cached[0] != null) return cached;
        float[] share = { .22f, .32f, .40f };
        var made = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            int rx = Mathf.Max(4, (int)(width * share[i]));
            int ry = Mathf.Max(2, (int)(rx * .4f));
            made[i] = MakeRing(rx, ry, "anel_upgrade_" + i);
        }
        rings[width] = made;
        return made;
    }

    static Sprite MakeRing(int rx, int ry, string name)
    {
        int w = 2 * rx + 1, h = 2 * ry + 1;
        var px = new Color32[w * h];
        var outer = new Color32(0xff, 0xe7, 0x31, 255);
        var inner = new Color32(0xff, 0xc5, 0x00, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = x - rx, dy = y - ry;
                if (!InsideEllipse(dx, dy, rx, ry, 0) || InsideEllipse(dx, dy, rx, ry, 2)) continue;
                px[y * w + x] = InsideEllipse(dx, dy, rx, ry, 1) ? inner : outer;
            }
        return PixelSprites.Make(px, w, h, new Vector2(rx / (float)w, ry / (float)h), 32f, name);
    }

    // dentro da elipse encolhida em k pixels
    static bool InsideEllipse(int dx, int dy, int rx, int ry, int k)
    {
        float a = rx - k + .5f, b = ry - k + .5f;
        if (a <= 0f || b <= 0f) return false;
        return (dx * dx) / (a * a) + (dy * dy) / (b * b) <= 1f;
    }
}
