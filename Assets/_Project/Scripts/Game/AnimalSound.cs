using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Clicar num bicho do cenario faz uma palavra de pixel subir da cabeca dele ("MUU", "OINK"...), no mesmo
/// estilo do "NHAC" do Tufao (fonte feita a mao, branca com contorno escuro, sobe uns pixels e some).
///
/// Som: se existir o arquivo Assets/_Project/Resources/sons/<id>.wav (ou .ogg/.mp3), ele toca no clique.
/// ids: galinha, galo, vaca, ovelha, porco, gato. Sem arquivo, so aparece o texto.
public class AnimalSound : MonoBehaviour
{
    const float Pixel = 1f / 32f;

    public string id;              // nome do arquivo de som e da palavra
    public string word = "MUU";    // so letras da fonte: C O M U I N K E W R
    public float cooldown = .6f;   // evita encher a tela clicando sem parar

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static AnimalSound runner;

    SpriteRenderer sr;
    AudioClip clip;
    float nextTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cache.Clear();
        runner = null;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!string.IsNullOrEmpty(id)) clip = Resources.Load<AudioClip>("sons/" + id);
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
        if (clip != null) AudioSource.PlayClipAtPoint(clip, cam.transform.position);
        if (runner == null)
        {
            runner = new GameObject("AnimalSoundFx").AddComponent<AnimalSound>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.StartCoroutine(runner.Show(word, new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0f)));
    }

    IEnumerator Show(string text, Vector3 above)
    {
        Sprite sprite = Word(text);
        var go = new GameObject("Palavra_" + text);
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

        var white = new Color32(255, 255, 255, 255);
        var dark = new Color32(0x20, 0x18, 0x21, 255);
        var none = new Color32(0, 0, 0, 0);
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
        Sprite s = PixelSprites.Make(px, w, h, new Vector2(.5f, 0f), 32f, "palavra_" + text);
        cache[text] = s;
        return s;
    }
}
