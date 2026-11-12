using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Clicar num bicho do cenario faz uma palavra de pixel subir da cabeca dele ("MUU", "OINK"...), no mesmo
/// estilo do "NHAC" do Tufao (fonte feita a mao, branca com contorno escuro, sobe uns pixels e some).
///
/// Som: se existir o arquivo Assets/_Project/Resources/sons/<id>.wav (ou .ogg/.mp3), ele toca no clique
/// (ver SoundBank). ids: galinha, galo, vaca, ovelha, porco, gato. Sem arquivo, so aparece o texto.
///
/// Extras: a vaca que dorme acorda por uns segundos, galinha e galo saem correndo, o gato pula no novelo,
/// 5 cliques seguidos no mesmo bicho soltam um coracao, e o galo canta sozinho no comeco de cada onda.
public class AnimalSound : MonoBehaviour
{
    const float Pixel = 1f / 32f;

    public string id;              // nome do arquivo de som
    public string word = "MUU";    // so letras da fonte: C O M U I N K E W R F Z
    public float cooldown = .6f;   // evita encher a tela clicando sem parar
    public Sprite awakeSprite;     // se tiver, o bicho "acorda" com este desenho por awakeTime segundos
    public float awakeTime = 2f;
    public string sleepWord = "ZZZZ";   // dormindo, os primeiros cliques mostram isto
    public int clicksToWake = 3;         // o ultimo clique acorda o bicho e mostra 'word'
    public bool flee;              // sai correndo do clique (galinha e galo)
    public bool crowsOnWave;       // canta sozinho no comeco de cada onda (galo)

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static AnimalSound runner;
    static Sprite heart;

    SpriteRenderer sr;
    float nextTime, lastClick;
    int streak, sleepClicks;
    bool awake;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cache.Clear();
        runner = null;
        heart = null;
    }

    void Start() => sr = GetComponent<SpriteRenderer>();

    // inscreve por bicho, porque o GameManager zera os eventos de onda ao fim de cada partida
    void OnEnable()
    {
        if (crowsOnWave) GameManager.OnWaveChanged += HandleWave;
    }

    void OnDisable()
    {
        if (crowsOnWave) GameManager.OnWaveChanged -= HandleWave;
    }

    void HandleWave(int current, int total)
    {
        if (sr == null) return;
        SoundBank.Play(id);
        Say(word, new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0f));
    }

    /// Mostra uma palavra de pixel subindo de 'above' (usada tambem pelo Tufao ao chegar).
    public static void Say(string text, Vector3 above)
    {
        EnsureRunner();
        runner.StartCoroutine(runner.Show(Word(text), above, text));
    }

    static void EnsureRunner()
    {
        if (runner != null) return;
        runner = new GameObject("AnimalSoundFx").AddComponent<AnimalSound>();
        DontDestroyOnLoad(runner.gameObject);
    }

    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (sr == null || Time.unscaledTime < nextTime) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (GameHUD.IsOverlayOpen) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 p = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        p.z = sr.bounds.center.z;
        if (!sr.bounds.Contains(p)) return;

        nextTime = Time.unscaledTime + cooldown;
        // dormindo: os primeiros cliques so mostram o ZZZZ; o ultimo acorda o bicho, que faz o seu barulho
        string say = word;
        bool makesSound = true, wakeNow = false;
        if (awakeSprite != null && !awake)
        {
            sleepClicks++;
            if (sleepClicks < clicksToWake) { say = sleepWord; makesSound = false; }
            else { sleepClicks = 0; wakeNow = true; }
        }
        if (makesSound) SoundBank.Play(id);
        Vector3 above = new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0f);
        Say(say, above);

        // 5 cliques seguidos (cada um em ate 1,2 s do anterior): coracao
        streak = Time.unscaledTime - lastClick < 1.2f ? streak + 1 : 1;
        lastClick = Time.unscaledTime;
        if (streak >= 5)
        {
            streak = 0;
            EnsureRunner();
            runner.StartCoroutine(runner.Show(Heart(), above + new Vector3(0f, 9f * Pixel, 0f), "coracao"));
        }

        var bird = GetComponent<ChickenWander>();
        if (flee && bird != null) bird.Flee(p);
        var cat = GetComponent<CatYarnPlay>();
        if (cat != null) cat.Startle();
        if (wakeNow) StartCoroutine(Wake());
    }

    // a vaca que dormia levanta (some o "Z") e volta a deitar
    IEnumerator Wake()
    {
        awake = true;
        Sprite sleeping = sr.sprite;
        SleepZ z = GetComponentInChildren<SleepZ>(true);
        if (z != null) z.gameObject.SetActive(false);
        sr.sprite = awakeSprite;
        yield return new WaitForSeconds(awakeTime);
        sr.sprite = sleeping;
        if (z != null) z.gameObject.SetActive(true);
        awake = false;
    }

    IEnumerator Show(Sprite sprite, Vector3 above, string label)
    {
        var go = new GameObject("Palavra_" + label);
        go.transform.SetParent(transform);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sortingOrder = DrawOrder.Projectile + 1;
        Vector3 basePos = PixelSprites.Snap(above);
        // 8 passos de 0,1 s: sobe 6 pixels e fica um pouco no alto
        for (int step = 0; step < 8; step++)
        {
            go.transform.position = basePos + new Vector3(0f, (4 + Mathf.Min(step, 6)) * Pixel, 0f);
            yield return new WaitForSeconds(.1f);
        }
        Destroy(go);
    }

    // Fonte 5 pixels de altura; a largura de cada letra e o tamanho da linha.
    static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        { 'C', new[] { "XXX", "X..", "X..", "X..", "XXX" } },
        { 'O', new[] { "XXX", "X.X", "X.X", "X.X", "XXX" } },
        { 'M', new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X" } },
        { 'W', new[] { "X...X", "X...X", "X.X.X", "XX.XX", "X...X" } },
        { 'U', new[] { "X.X", "X.X", "X.X", "X.X", "XXX" } },
        { 'I', new[] { "XXX", ".X.", ".X.", ".X.", "XXX" } },
        { 'N', new[] { "X..X", "XX.X", "X.XX", "X..X", "X..X" } },
        { 'K', new[] { "X.X", "X.X", "XX.", "X.X", "X.X" } },
        { 'E', new[] { "XXX", "X..", "XXX", "X..", "XXX" } },
        { 'R', new[] { "XX.", "X.X", "XX.", "X.X", "X.X" } },
        { 'F', new[] { "XXX", "X..", "XX.", "X..", "X.." } },
        { 'Z', new[] { "XXX", "..X", ".X.", "X..", "XXX" } },
    };

    static Sprite Word(string text)
    {
        if (cache.TryGetValue(text, out Sprite cached) && cached != null) return cached;

        int width = 0;
        foreach (char c in text) width += Font[c][0].Length + 1;
        width -= 1;
        int w = width + 2, h = 5 + 2;                 // 1 pixel de contorno em volta
        var fill = new bool[w, h];                    // y = 0 embaixo
        int x0 = 1;
        foreach (char c in text)
        {
            string[] g = Font[c];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < g[0].Length; x++)
                    if (g[y][x] == 'X') fill[x0 + x, 1 + (4 - y)] = true;
            x0 += g[0].Length + 1;
        }
        Sprite s = Outlined(fill, w, h, new Color32(255, 255, 255, 255), "palavra_" + text);
        cache[text] = s;
        return s;
    }

    // coracao 7x6 vermelho com o mesmo contorno escuro das palavras
    static Sprite Heart()
    {
        if (heart != null) return heart;
        string[] rows = { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
        int w = 7 + 2, h = 6 + 2;
        var fill = new bool[w, h];
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 7; x++)
                if (rows[y][x] == 'X') fill[1 + x, 1 + (5 - y)] = true;
        heart = Outlined(fill, w, h, new Color32(0xE0, 0x30, 0x48, 255), "coracao");
        return heart;
    }

    static Sprite Outlined(bool[,] fill, int w, int h, Color32 color, string name)
    {
        var dark = new Color32(0x20, 0x18, 0x21, 255);
        var none = new Color32(0, 0, 0, 0);
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (fill[x, y]) { px[y * w + x] = color; continue; }
                bool near = false;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && fill[nx, ny]) near = true;
                    }
                px[y * w + x] = near ? dark : none;
            }
        return PixelSprites.Make(px, w, h, new Vector2(.5f, 0f), 32f, name);
    }
}
