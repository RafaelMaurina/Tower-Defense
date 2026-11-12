using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Efeitos de tiro que trocam o desenho da torre por um quadro deslocado em memoria (braco do
/// espantalho, cano da uva, toca da colmeia) e as particulas pequenas que saem junto: palha,
/// bagaco de uva e abelhinhas. Os PNG nunca mudam. Tudo em pixel inteiro, sem escala nem rotacao.
public partial class TowerFireFx
{
    static readonly RectInt BarrelRect = new RectInt(47, 26, 14, 22);   // so a ponta do cano, fora do toco (uva, os 3 niveis)
    static readonly Color32[] BarrelColors =                            // cinzas e pretos do cano; o resto do retangulo nao treme
    {
        new Color32(0x1a, 0x09, 0x09, 255), new Color32(0x68, 0x5b, 0x5b, 255),
        new Color32(0x28, 0x24, 0x24, 255), new Color32(0x15, 0x12, 0x16, 255),
    };
    static readonly Color32 HoleColor = new Color32(0x1a, 0x09, 0x09, 255);
    static readonly Color32 OutlineColor = new Color32(0x20, 0x18, 0x21, 255);

    // bagaco: trajetoria em pixels a partir do ponto de saida (x para a direita, y para baixo)
    static readonly Vector2Int[] SeedPath =
        { new Vector2Int(0, -2), new Vector2Int(1, -3), new Vector2Int(2, -3), new Vector2Int(3, -1),
          new Vector2Int(3, 2), new Vector2Int(4, 6), new Vector2Int(4, 10) };
    // palha: deslocamento no ponto do punho, atraso e queda (pixels) a cada passo
    static readonly Vector2Int[] StrawStart = { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
    static readonly int[] StrawDelay = { 0, 1, 2 };
    static readonly int[] StrawFall = { 0, 1, 2, 4, 5, 7 };

    static Sprite[] whiteBFrames;
    static Sprite[] strawSprites;     // 1x2: amarelo claro sobre dourado
    static Sprite[] seedSprites;      // 1x1 e 2x1, roxos da uva
    static Sprite beeDot;             // 1x1, contorno
    static int grapeShots;
    static readonly Dictionary<Sprite, Sprite> barrelPoses = new Dictionary<Sprite, Sprite>();
    static readonly Dictionary<Sprite, Sprite> holePoses = new Dictionary<Sprite, Sprite>();

    readonly Dictionary<Tower, Sprite> posedBase = new Dictionary<Tower, Sprite>();
    readonly Dictionary<Tower, int> poseToken = new Dictionary<Tower, int>();

    static void ResetPoseCaches()
    {
        whiteBFrames = null;
        strawSprites = null;
        seedSprites = null;
        beeDot = null;
        grapeShots = 0;
        barrelPoses.Clear();
        holePoses.Clear();
    }

    // ---------- espantalho

    // O braco do lado do alvo da um tranco de 2 px e depois 1 px na direcao do tiro e volta (2 quadros),
    // e 3 fiapos de palha caem do punho.
    void ScarecrowKick(Tower tower, Vector3 rest, Vector3 dir, Vector3 aim, int order)
    {
        int side = TowerShotFx.HandSide(tower, aim);
        StartCoroutine(ArmKick(tower, side, dir));
        StartCoroutine(Straw(tower, rest, side, order));
    }

    IEnumerator ArmKick(Tower tower, int side, Vector3 dir)
    {
        var sway = tower.GetComponent<PixelRegionSway>();
        var sr = tower.GetComponent<SpriteRenderer>();
        if (sway == null || sr == null || !TowerShotFx.TryGetSpot(sr.sprite, out TowerShotFx.Spot spot)) yield break;
        Vector2 hand = side < 0 ? spot.a : spot.b;
        var rect = new RectInt(Mathf.RoundToInt(hand.x) - 3, Mathf.RoundToInt(hand.y) - 4, 6, 7);   // a mao, 6x7 px
        int token = NextToken(tower);
        for (int k = 2; k >= 1; k--)
        {
            var shift = new Vector2Int(Mathf.RoundToInt(dir.x * k), Mathf.RoundToInt(-dir.y * k));
            if (k == 2 && shift == Vector2Int.zero) shift = new Vector2Int(side, 0);
            if (tower == null) yield break;
            sway.Kick(rect, shift, -side);
            yield return new WaitForSeconds(StepTime);
        }
        if (tower != null && poseToken.TryGetValue(tower, out int t) && t == token) { sway.ClearKick(); poseToken.Remove(tower); }
    }

    IEnumerator Straw(Tower tower, Vector3 rest, int side, int order)
    {
        EnsureParticleSprites();
        var sr = tower.GetComponent<SpriteRenderer>();
        Sprite sprite = sr != null ? sr.sprite : null;
        if (sprite == null) yield break;
        Vector3 wrist = rest + PixelSprites.Local(sprite, side < 0 ? 13f : 50f, 22f);
        const int n = 3, life = 6;
        var parts = new SpriteRenderer[n];
        for (int i = 0; i < n; i++) parts[i] = TakeFx("Palha", order);
        for (int t = 0; t < life + StrawDelay[n - 1]; t++)
        {
            for (int i = 0; i < n; i++)
            {
                int age = t - StrawDelay[i];
                if (age < 0 || age >= life) { parts[i].enabled = false; continue; }
                parts[i].enabled = true;
                parts[i].sprite = strawSprites[0];
                Vector3 off = new Vector3(StrawStart[i].x, -StrawStart[i].y - StrawFall[age], 0f);
                parts[i].transform.position = Snap(wrist + off * Pixel);
            }
            yield return new WaitForSeconds(StepTime);
        }
        for (int i = 0; i < n; i++) ReleaseFx(parts[i]);
    }

    // ---------- metralhadora de uvas

    // Cano 1 px para tras por um passo, e uma sementinha ou bagaco roxo que pula do lado do toco e cai.
    void GrapeBarrelAndSeed(Tower tower, Vector3 rest, int order)
    {
        var sr = tower.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        SwapPose(tower, barrelPoses, ShakeBarrel, StepTime);
        if (Random.value < .7f) StartCoroutine(Seed(tower, rest, order, sr.sprite));
    }

    IEnumerator Seed(Tower tower, Vector3 rest, int order, Sprite sprite)
    {
        EnsureParticleSprites();
        SpriteRenderer part = TakeFx("Bagaco", order);
        part.sprite = seedSprites[Random.value < .5f ? 0 : 1];
        part.enabled = true;
        Vector3 start = rest;
        if (TowerShotFx.TryGetSpot(sprite, out TowerShotFx.Spot spot))
            start += PixelSprites.Local(sprite, spot.b.x, spot.b.y);   // sob o cano, ao lado do toco
        int stretch = Random.value < .5f ? 1 : 2;   // alguns pulam mais para o lado
        for (int t = 0; t < SeedPath.Length; t++)
        {
            Vector2Int p = SeedPath[t];
            part.transform.position = Snap(start + new Vector3(p.x * stretch / 1f, -p.y, 0f) * Pixel);
            yield return new WaitForSeconds(StepTime * .9f);
        }
        ReleaseFx(part);
    }

    // ---------- colmeia

    // A toca abre (buraco 1 px maior) por um quadro e 3 abelhinhas de 1 px saem e se espalham em 2 quadros.
    void HiveSpit(Tower tower, Vector3 rest, Vector3 dir, int order)
    {
        var sr = tower.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        SwapPose(tower, holePoses, OpenHole, StepTime * 1.5f);
        StartCoroutine(Bees(rest + TowerShotFx.Muzzle(tower, TowerShotFx.Kind.Hive), dir, order));
    }

    IEnumerator Bees(Vector3 hole, Vector3 dir, int order)
    {
        EnsureParticleSprites();
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        float[] reach = { 3f, 7f };          // distancia da toca em cada quadro (px)
        float[] spread = { 1.3f, 2.8f };     // abertura lateral em cada quadro
        float[] side = { -1f, 0f, 1f };
        var bees = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            bees[i] = TakeFx("Abelhinha", order);
            bees[i].sprite = beeDot;
            bees[i].enabled = true;
        }
        for (int f = 0; f < 2; f++)
        {
            for (int i = 0; i < 3; i++)
                bees[i].transform.position = Snap(hole + (dir * reach[f] + perp * (side[i] * spread[f])) * Pixel);
            yield return new WaitForSeconds(StepTime);
        }
        for (int i = 0; i < 3; i++) ReleaseFx(bees[i]);
    }

    // Copia do sprite com so os pixels do cano (cinzas e pretos) 1 px para tras; toco e balde ficam parados.
    static Color32[] ShakeBarrel(Color32[] src)
    {
        int w = 64, h = 64;
        var dst = (Color32[])src.Clone();
        BarrelPixels(src, w, h, (x, y, c) => dst[(h - 1 - y) * w + x] = new Color32(0, 0, 0, 0));
        BarrelPixels(src, w, h, (x, y, c) => dst[(h - 1 - y) * w + x - 1] = c);
        return dst;
    }

    static void BarrelPixels(Color32[] src, int w, int h, System.Action<int, int, Color32> each)
    {
        for (int y = BarrelRect.y; y < BarrelRect.yMax; y++)
            for (int x = BarrelRect.x; x < BarrelRect.xMax; x++)
            {
                Color32 c = src[(h - 1 - y) * w + x];
                if (c.a == 0) continue;
                foreach (Color32 b in BarrelColors)
                    if (SameColor(c, b)) { each(x, y, c); break; }
            }
    }

    // Copia do sprite com o buraco escuro 1 px maior (so em cima do desenho, nunca no contorno).
    static Color32[] OpenHole(Color32[] src)
    {
        int w = 64, h = 64;
        var dst = (Color32[])src.Clone();
        for (int y = 24; y < 38; y++)
            for (int x = 24; x < 42; x++)
            {
                if (!SameColor(src[(h - 1 - y) * w + x], HoleColor)) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    Color32 c = src[(h - 1 - ny) * w + nx];
                    if (c.a != 0 && !SameColor(c, HoleColor) && !SameColor(c, OutlineColor)) dst[(h - 1 - ny) * w + nx] = HoleColor;
                }
            }
        return dst;
    }

    static bool SameColor(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    // ---------- troca de quadro da torre

    int NextToken(Tower tower)
    {
        int t = poseToken.TryGetValue(tower, out int old) ? old + 1 : 1;
        poseToken[tower] = t;
        return t;
    }

    // Mostra um quadro deslocado da torre por 'seconds' e volta ao desenho de antes, a nao ser que a
    // torre tenha subido de nivel nesse meio tempo ou outro tiro tenha renovado o quadro.
    void SwapPose(Tower tower, Dictionary<Sprite, Sprite> cache, System.Func<Color32[], Color32[]> make, float seconds)
    {
        var sr = tower.GetComponent<SpriteRenderer>();
        if (!posedBase.TryGetValue(tower, out Sprite baseSprite)) { baseSprite = sr.sprite; posedBase[tower] = baseSprite; }
        if (!cache.TryGetValue(baseSprite, out Sprite pose) || pose == null)
        {
            Color32[] src = PixelSprites.Read(baseSprite, out _, out _);
            if (src == null) return;
            pose = PixelSprites.MakeLike(baseSprite, make(src), 64, 64, "_pose");
            cache[baseSprite] = pose;
        }
        sr.sprite = pose;
        StartCoroutine(EndPose(tower, sr, pose, NextToken(tower), seconds));
    }

    IEnumerator EndPose(Tower tower, SpriteRenderer sr, Sprite pose, int token, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (tower == null || !poseToken.TryGetValue(tower, out int t) || t != token) yield break;
        if (sr != null && sr.sprite == pose && posedBase.TryGetValue(tower, out Sprite baseSprite)) sr.sprite = baseSprite;
        posedBase.Remove(tower);
        poseToken.Remove(tower);
    }

    // ---------- desenhos

    static Sprite[] NextWhiteFrames()
    {
        Sprite[] a = WhiteFrames();
        if (whiteBFrames == null || whiteBFrames[0] == null)
        {
            var branco = new Color32(0xff, 0xff, 0xff, 255);
            var cinza = new Color32(0xd9, 0xd5, 0xd5, 255);
            string[] grande = { "b...b", ".bcb.", ".ccc.", ".bcb.", "b...b" };   // em X, para alternar com a cruz
            string[] pequeno = { "b.b", ".c.", "b.b" };
            whiteBFrames = new[] { Draw(grande, branco, cinza), Draw(pequeno, branco, cinza) };
        }
        return (grapeShots++ & 1) == 0 ? a : whiteBFrames;
    }

    static void EnsureParticleSprites()
    {
        if (strawSprites != null && strawSprites[0] != null && seedSprites != null && seedSprites[0] != null && beeDot != null) return;
        var pivot = new Vector2(.5f, .5f);
        var yellow = new Color32(0xff, 0xe7, 0x31, 255);
        var gold = new Color32(0xf9, 0xc2, 0x2b, 255);
        var purple = new Color32(0xa1, 0x09, 0xc4, 255);
        var purpleDark = new Color32(0x71, 0x1c, 0x85, 255);
        strawSprites = new[] { PixelSprites.Make(new[] { gold, yellow }, 1, 2, pivot, 32f, "palha") };   // linha 0 = baixo
        seedSprites = new[]
        {
            PixelSprites.Make(new[] { purple }, 1, 1, pivot, 32f, "bagaco_1"),
            PixelSprites.Make(new[] { purpleDark, purple }, 2, 1, pivot, 32f, "bagaco_2"),
        };
        beeDot = PixelSprites.Make(new[] { OutlineColor }, 1, 1, pivot, 32f, "abelhinha");
    }
}
