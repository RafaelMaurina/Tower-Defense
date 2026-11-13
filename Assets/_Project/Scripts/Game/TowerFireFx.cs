using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Resposta visual do tiro, no estilo pixel art: a torre recua 2 pixels na direcao contraria
/// ao alvo e volta em dois passos, e um clarao de 2 quadros aparece do lado do alvo.
/// O canhao de abobora tem disparo proprio (CannonBlast): recuo de 4 px com peso, clarao grande na
/// boca, baforada de fumaca e poeira no pe.
/// Tudo em passos de pixel inteiro (1/32 de unidade), sem escala nem rotacao.
/// Liga sozinho: escuta Tower.OnFired, nao precisa estar em cena nem em prefab.
public partial class TowerFireFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float StepTime = .05f;         // segundos de cada passo do recuo e de cada quadro do clarao
    const float FlashDistance = .55f;    // do centro da torre ate o clarao, em casas

    // canhao de abobora
    const float CannonTick = .045f;
    const int CannonTicks = 14;          // ~0,63 s
    static readonly int[] NormalRecoil = { 2, 1 };
    static readonly int[] CannonRecoil = { 4, 3, 2, 1, 0 };
    static readonly int[] CannonFlashAt = { 0, 0, 1, 2 };   // quadro do clarao em cada passo
    static readonly Vector2Int[] CannonFlashOffset = { new Vector2Int(0, 0), new Vector2Int(-3, 1), new Vector2Int(-5, 2) };
    static readonly Vector3 CannonKick = new Vector3(-1f, .25f, 0f).normalized;   // para onde o tiro sai (a boca e a esquerda)
    Vector3 cannonOut = CannonKick;      // direcao do cano no tiro atual (muda com o CannonAim)
    static readonly WaitForSeconds CannonWait = new WaitForSeconds(CannonTick);

    static TowerFireFx runner;
    static Sprite[] flashFrames;
    static Sprite[] whiteFrames;
    static Sprite[] cannonFlash;   // 3 quadros: ~17 px, ~11 px e ~7 px
    static Sprite[] smokePuffs;    // [0..2] claro 4x4, 3x3, 2x2 e [3..5] cinza medio
    static Sprite[] earthPuffs;    // [0..1] 3x3 e [2..3] 2x2, em 2 cores de terra

    readonly Dictionary<Tower, Vector3> restPosition = new Dictionary<Tower, Vector3>();
    readonly Stack<SpriteRenderer> freeFlashes = new Stack<SpriteRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        flashFrames = null;
        whiteFrames = null;
        cannonFlash = null;
        smokePuffs = null;
        earthPuffs = null;
        ResetPoseCaches();
        Tower.OnFired -= HandleFired;
        Tower.OnFired += HandleFired;
    }

    static void HandleFired(Tower tower, Vector3 aim)
    {
        if (tower == null) return;
        if (runner == null)
        {
            runner = new GameObject("TowerFireFx").AddComponent<TowerFireFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.Play(tower, aim);
    }

    void Play(Tower tower, Vector3 aim)
    {
        // se a torre ainda esta no meio de um recuo, parte da posicao de descanso dela
        if (!restPosition.TryGetValue(tower, out Vector3 rest)) rest = tower.transform.position;
        var sr = tower.GetComponent<SpriteRenderer>();
        int order = sr != null ? sr.sortingOrder + 1 : 500;
        TowerShotFx.Kind kind = TowerShotFx.KindOf(tower);

        if (kind == TowerShotFx.Kind.Pumpkin)   // canhao: a boca fica sempre a esquerda, nao depende do alvo
        {
            // canhao que vira (nivel 1): recuo e clarao seguem a direcao do cano
            var aimer = tower.GetComponent<CannonAim>();
            cannonOut = aimer != null && aimer.enabled ? aimer.Facing : CannonKick;
            StartCoroutine(Recoil(tower, rest, cannonOut, CannonRecoil, true));
            StartCoroutine(CannonBlast(rest + TowerShotFx.Muzzle(tower, kind), rest, order, sr != null ? sr.sprite : null));
            return;
        }

        Vector3 dir = aim - rest;
        dir.z = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();

        Vector3 center = sr != null ? sr.bounds.center - (tower.transform.position - rest) : rest;
        StartCoroutine(Recoil(tower, rest, dir, NormalRecoil, false));
        if (kind == TowerShotFx.Kind.Hive)           // colmeia: sem clarao, a toca cospe a abelha
        {
            HiveSpit(tower, rest, dir, order);
            return;
        }
        if (kind == TowerShotFx.Kind.Grapes)         // metralhadora: clarao branco na boca, cano que treme, bagaco
        {
            StartCoroutine(Flash(Snap(rest + TowerShotFx.Muzzle(tower, kind)), order, NextWhiteFrames()));
            GrapeBarrelAndSeed(tower, rest, order);
            return;
        }
        if (kind == TowerShotFx.Kind.Scarecrow)      // espantalho: tranco do braco e palha, sem clarao
        {
            ScarecrowKick(tower, rest, dir, aim, order);
            return;
        }
        StartCoroutine(Flash(Snap(center + dir * FlashDistance), order, FlashFrames()));
    }

    // 'toward' aponta para onde o tiro sai; a torre vai para o lado contrario, um passo por item de
    // 'steps' (pixels). settle: no primeiro passo a torre tambem desce 1 px (peso do canhao).
    IEnumerator Recoil(Tower tower, Vector3 rest, Vector3 toward, int[] steps, bool settle)
    {
        restPosition[tower] = rest;
        for (int i = 0; i < steps.Length; i++)
        {
            if (tower == null) { restPosition.Remove(tower); yield break; }   // vendida no meio do recuo
            Vector3 pos = rest - Snap(toward * (steps[i] * Pixel));
            if (settle && i == 0) pos.y -= Pixel;
            tower.transform.position = pos;
            yield return new WaitForSeconds(settle && i == 0 ? .04f : StepTime);
        }
        if (tower != null) tower.transform.position = rest;
        restPosition.Remove(tower);
    }

    // Disparo do canhao num so laco de passos de 0,045 s: clarao na boca (4 passos), 5 bolinhas de
    // fumaca que saem da boca para a esquerda e sobem (~0,5 s) e 3 bolinhas de poeira no pe da torre.
    IEnumerator CannonBlast(Vector3 mouth, Vector3 rest, int order, Sprite towerSprite)
    {
        EnsureCannonSprites();
        const int smokeN = 5, dustN = 3, smokeLife = 11;

        SpriteRenderer flash = TakeFx("Clarao", order);
        var smoke = new SpriteRenderer[smokeN];
        var sx = new float[smokeN];
        var sy = new float[smokeN];
        var vx = new float[smokeN];
        var vy = new float[smokeN];
        var delay = new int[smokeN];
        var shade = new int[smokeN];
        for (int i = 0; i < smokeN; i++)
        {
            smoke[i] = TakeFx("Fumaca", order + 2);
            // a fumaca sai na direcao do cano (para cima/baixo, abre para os dois lados)
            float side = cannonOut.x < -.3f ? -1f : cannonOut.x > .3f ? 1f : (i % 2 == 0 ? -1f : 1f);
            sx[i] = side;
            sy[i] = cannonOut.y < -.5f ? -1f : 0f;
            vx[i] = side * Random.Range(2f, 4f) * (Mathf.Abs(cannonOut.x) > .3f ? 1f : .5f);
            vy[i] = Random.Range(.5f, 2.5f) + (cannonOut.y > .5f ? 1.5f : 0f);
            delay[i] = i / 2;
            shade[i] = i % 3 == 2 ? 3 : 0;
        }

        // poeira no pe da torre (pixel 32,50 do desenho): sai de baixo e corre para os lados
        Vector3 foot = towerSprite != null ? rest + PixelSprites.Local(towerSprite, 32f, 50f) : rest;
        var dust = new SpriteRenderer[dustN];
        var dstart = new float[dustN];
        var dtarget = new float[dustN];
        var dkind = new int[dustN];
        for (int i = 0; i < dustN; i++)
        {
            dust[i] = TakeFx("Poeira", order + 1);
            int side = i == 0 ? -1 : i == 1 ? 1 : (Random.value < .5f ? -1 : 1);
            dstart[i] = side * (i < 2 ? 14f : 4f);
            dtarget[i] = side * Random.Range(7, 12);
            dkind[i] = Random.Range(0, 2);
        }

        for (int t = 0; t < CannonTicks; t++)
        {
            // clarao da boca
            if (t < CannonFlashAt.Length)
            {
                int f = CannonFlashAt[t];
                flash.sprite = cannonFlash[f];
                // o clarao se afasta da boca na direcao do cano (para o oeste fica igual ao desenho original)
                Vector2Int o = CannonFlashOffset[f];
                Vector3 along = cannonOut * Mathf.Abs(o.x) + Vector3.up * o.y;
                flash.transform.position = Snap(mouth + along * Pixel);
                flash.enabled = true;
            }
            else flash.enabled = false;

            // fumaca: desacelera, encolhe 4x4 -> 3x3 -> 2x2 e esmaece
            for (int i = 0; i < smokeN; i++)
            {
                int age = t - delay[i];
                if (age < 0 || age >= smokeLife) { smoke[i].enabled = false; continue; }
                if (age > 0)
                {
                    sx[i] += vx[i];
                    sy[i] += vy[i];
                    vx[i] *= .8f;
                    vy[i] = vy[i] * .9f + .12f;
                }
                smoke[i].enabled = true;
                smoke[i].sprite = smokePuffs[(age < 4 ? 0 : age < 8 ? 1 : 2) + shade[i]];
                float a = Mathf.Round(Mathf.Lerp(1f, .35f, age / (float)smokeLife) * 5f) / 5f;
                smoke[i].color = new Color(1f, 1f, 1f, a);
                smoke[i].transform.position = Snap(mouth + new Vector3(sx[i], sy[i], 0f) * Pixel);
            }

            // poeira: corre para o lado e sobe 1 a 2 px, 3x3 e depois 2x2
            for (int i = 0; i < dustN; i++)
            {
                if (t >= 8) { dust[i].enabled = false; continue; }
                float u = 1f - Mathf.Pow(1f - (t + 1) / 8f, 2f);
                dust[i].enabled = true;
                dust[i].sprite = earthPuffs[(t < 4 ? 0 : 2) + dkind[i]];
                float rise = t < 2 ? 0f : t < 5 ? 1f : 2f;
                dust[i].transform.position = Snap(foot + new Vector3(dstart[i] + dtarget[i] * u, rise, 0f) * Pixel);
            }
            yield return CannonWait;
        }

        ReleaseFx(flash);
        for (int i = 0; i < smokeN; i++) ReleaseFx(smoke[i]);
        for (int i = 0; i < dustN; i++) ReleaseFx(dust[i]);
    }

    SpriteRenderer TakeFx(string fxName, int order)
    {
        SpriteRenderer sr = freeFlashes.Count > 0 ? freeFlashes.Pop() : NewFlash();
        sr.name = fxName;
        sr.sortingOrder = order;
        sr.color = Color.white;
        sr.enabled = false;
        sr.gameObject.SetActive(true);
        return sr;
    }

    void ReleaseFx(SpriteRenderer sr)
    {
        sr.enabled = true;
        sr.gameObject.SetActive(false);
        freeFlashes.Push(sr);
    }

    IEnumerator Flash(Vector3 position, int order, Sprite[] frames)
    {
        SpriteRenderer flash = freeFlashes.Count > 0 ? freeFlashes.Pop() : NewFlash();
        flash.name = "Clarao";
        flash.transform.position = position;
        flash.sortingOrder = order;
        flash.color = Color.white;
        flash.enabled = true;
        flash.gameObject.SetActive(true);
        for (int i = 0; i < frames.Length; i++)
        {
            flash.sprite = frames[i];
            yield return new WaitForSeconds(StepTime);
        }
        flash.gameObject.SetActive(false);
        freeFlashes.Push(flash);
    }

    SpriteRenderer NewFlash()
    {
        var go = new GameObject("Clarao");
        go.transform.SetParent(transform);
        return go.AddComponent<SpriteRenderer>();
    }

    static Vector3 Snap(Vector3 v) =>
        new Vector3(Mathf.Round(v.x / Pixel) * Pixel, Mathf.Round(v.y / Pixel) * Pixel, 0f);

    // Clarao desenhado em codigo com cores da paleta do trio: uma estrela de 7x7 e depois um miolo de 3x3.
    static Sprite[] FlashFrames()
    {
        if (flashFrames != null && flashFrames[0] != null) return flashFrames;
        var claro = new Color32(0xff, 0xe7, 0x31, 255);
        var borda = new Color32(0xf7, 0x96, 0x17, 255);
        string[] grande =
        {
            "...b...",
            "..bcb..",
            ".bcccb.",
            "bcccccb",
            ".bcccb.",
            "..bcb..",
            "...b...",
        };
        string[] pequeno = { ".b.", "bcb", ".b." };
        flashFrames = new[] { Draw(grande, claro, borda), Draw(pequeno, claro, borda) };
        return flashFrames;
    }

    // Clarao branco da boca da metralhadora: estrela de 5x5 e depois cruz de 3x3.
    static Sprite[] WhiteFrames()
    {
        if (whiteFrames != null && whiteFrames[0] != null) return whiteFrames;
        var branco = new Color32(0xff, 0xff, 0xff, 255);
        var cinza = new Color32(0xd9, 0xd5, 0xd5, 255);
        string[] grande = { "..b..", ".bcb.", "bcccb", ".bcb.", "..b.." };
        string[] pequeno = { ".b.", "bcb", ".b." };
        whiteFrames = new[] { Draw(grande, branco, cinza), Draw(pequeno, branco, cinza) };
        return whiteFrames;
    }

    static float Dist(float dx, float dy) => Mathf.Sqrt(dx * dx + dy * dy);

    static void EnsureCannonSprites()
    {
        if (cannonFlash != null && cannonFlash[0] != null && smokePuffs != null && earthPuffs != null) return;

        // clarao da boca: estrela irregular, branco no miolo, amarelo e borda laranja
        float[] lobes = PixelBlast.Lobes(21, 10, .45f);
        float[] radius = { 8.5f, 5.5f, 3.5f };
        int[] size = { 18, 12, 8 };
        cannonFlash = new Sprite[3];
        for (int k = 0; k < 3; k++)
        {
            float r = radius[k];
            int frame = k;
            Color32[] px = PixelBlast.Paint(size[k], (dx, dy, x, y) =>
            {
                float v = Dist(dx, dy) / (r * PixelBlast.Reach(lobes, dx, dy));
                if (v > 1f) return PixelBlast.Clear;
                if (frame == 2) return v <= .55f ? PixelBlast.White : PixelBlast.Yellow;
                return v <= (frame == 0 ? .45f : .4f) ? PixelBlast.White : v <= .75f ? PixelBlast.Yellow : PixelBlast.Orange;
            });
            cannonFlash[k] = PixelBlast.Make(px, size[k], "clarao_canhao_" + k);
        }

        System.Func<char, Color32> light = c => PixelBlast.SmokeLight;
        System.Func<char, Color32> mid = c => PixelBlast.SmokeMid;
        smokePuffs = new[]
        {
            PixelBlast.FromRows(new[] { ".cc.", "cccc", "cccc", ".cc." }, light, "baforada_4"),
            PixelBlast.FromRows(new[] { ".c.", "ccc", ".c." }, light, "baforada_3"),
            PixelBlast.FromRows(new[] { "cc", "cc" }, light, "baforada_2"),
            PixelBlast.FromRows(new[] { ".cc.", "cccc", "cccc", ".cc." }, mid, "baforada_4m"),
            PixelBlast.FromRows(new[] { ".c.", "ccc", ".c." }, mid, "baforada_3m"),
            PixelBlast.FromRows(new[] { "cc", "cc" }, mid, "baforada_2m"),
        };

        System.Func<char, Color32> earth = c => PixelBlast.Earth;
        System.Func<char, Color32> earthDark = c => PixelBlast.EarthDark;
        earthPuffs = new[]
        {
            PixelBlast.FromRows(new[] { ".c.", "ccc", "ccc" }, earth, "poeira_pe_3"),
            PixelBlast.FromRows(new[] { ".c.", "ccc", "ccc" }, earthDark, "poeira_pe_3b"),
            PixelBlast.FromRows(new[] { "cc", "cc" }, earth, "poeira_pe_2"),
            PixelBlast.FromRows(new[] { "cc", "cc" }, earthDark, "poeira_pe_2b"),
        };
    }

    static Sprite Draw(string[] rows, Color32 light, Color32 edge)
    {
        int n = rows.Length;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "clarao" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                char c = rows[n - 1 - y][x];
                px[y * n + x] = c == 'c' ? light : c == 'b' ? edge : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 32f, 0, SpriteMeshType.FullRect);
    }
}
