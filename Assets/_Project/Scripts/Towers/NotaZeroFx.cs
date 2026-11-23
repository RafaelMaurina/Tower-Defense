using UnityEngine;

/// Efeitos da nota 0 do Prof Rieder numa torre (vive no objeto "NotaZero", filho da torre):
///  - entrada: um "REPROVADO!" vermelho (mesma fonte de pixel dos outros textos) sai do professor e voa ate a torre; quando
///    chega, o "0" cai com quique, a torre treme 1 pixel, escurece e leva dois riscos em X, como correcao de prova;
///  - enquanto desligada: a torre fica escura e cinza e o "0" balanca 1 pixel;
///  - volta: o "0" e o X somem em fade, a torre recupera a cor e saem faiscas.
/// Tudo em pixel inteiro, sem escala nem rotacao. O visual e funcao do tempo (Aplicar), entao da para fotografar cada instante.
public class NotaZeroFx : MonoBehaviour
{
    public enum Fase { Fora, Entrando, Parado, Saindo }

    const float Pixel = 1f / 32f;
    const float Voo = .45f;                 // tempo do "REPROVADO!" ate a torre (sem professor, nao ha voo)
    const float Impacto = .45f;             // queda do "0", tremida e riscos, depois que o texto chega
    const float Saida = .45f, Tremida = .24f, PassoQueda = .05f;
    static readonly int[] Queda = { 12, 7, 3, 0, 2, 0, 1, 0 };          // pixels acima do lugar, a cada passo
    static readonly float[] Alfa = { 1f, 1f, .66f, .33f };               // fade em 4 passos
    static readonly Color Apagada = new Color(.5f, .5f, .56f);
    static readonly Vector2[] Faiscas = { new Vector2(-.55f, .25f), new Vector2(.45f, .5f), new Vector2(-.2f, .95f), new Vector2(.6f, 1f) };

    static Sprite xMeio, xCheio, faisca;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => xMeio = xCheio = faisca = null;

    Tower torre;
    SpriteRenderer torreSr, marcaSr, xSr, textoSr;
    SpriteRenderer[] faiscasSr;
    Vector3 marcaFinal, torreBase, origem;
    bool temOrigem;
    Fase fase = Fase.Fora;
    float t;

    public Fase Atual => fase;
    float TempoVoo => temOrigem ? Voo : 0f;

    public void Iniciar(Tower dono)
    {
        torre = dono;
        torreSr = dono.GetComponent<SpriteRenderer>();
        marcaSr = GetComponent<SpriteRenderer>();
        Monta();
    }

    void Monta()
    {
        Sprites();
        var x = new GameObject("RiscoX");
        x.transform.SetParent(transform, false);
        xSr = x.AddComponent<SpriteRenderer>();
        xSr.enabled = false;

        var txt = new GameObject("Reprovado");
        txt.transform.SetParent(transform, false);
        textoSr = txt.AddComponent<SpriteRenderer>();
        textoSr.sprite = AnimalSound.Palavra("REPROVADO!", new Color32(230, 30, 30, 255));
        textoSr.enabled = false;

        faiscasSr = new SpriteRenderer[Faiscas.Length];
        for (int i = 0; i < faiscasSr.Length; i++)
        {
            var f = new GameObject("Faisca");
            f.transform.SetParent(transform, false);
            faiscasSr[i] = f.AddComponent<SpriteRenderer>();
            faiscasSr[i].sprite = faisca;
            faiscasSr[i].enabled = false;
        }
    }

    /// Chamado quando a torre recebe a nota 0. de = de onde o professor solta o "REPROVADO!" (null = sem texto voando).
    public void Entrar(Vector3? de)
    {
        if (torreSr == null) torreSr = torre.GetComponent<SpriteRenderer>();
        marcaFinal = transform.position;
        torreBase = torre.transform.position;
        temOrigem = de.HasValue;
        if (de.HasValue) origem = de.Value;
        fase = Fase.Entrando;
        t = 0f;
        gameObject.SetActive(true);
        Aplicar(0f, fase);
    }

    public void Sair()
    {
        if (fase == Fase.Fora || fase == Fase.Saindo) return;
        fase = Fase.Saindo;
        t = 0f;
    }

    void Update()
    {
        if (torre == null) return;
        t += Time.deltaTime;
        if (fase == Fase.Entrando && t >= TempoVoo + Impacto) { fase = Fase.Parado; t = 0f; }
        else if (fase == Fase.Saindo && t >= Saida) { Desliga(); return; }
        Aplicar(t, fase);
    }

    void Desliga()
    {
        fase = Fase.Fora;
        Restaura();
        gameObject.SetActive(false);
    }

    void Restaura()
    {
        if (torreSr != null) torreSr.color = Color.white;
        if (torre != null && (torre.transform.position - torreBase).sqrMagnitude < 4f * Pixel * Pixel) torre.transform.position = torreBase;
        if (marcaSr != null) marcaSr.color = Color.white;
        if (xSr != null) xSr.enabled = false;
        if (textoSr != null) textoSr.enabled = false;
        if (faiscasSr != null) foreach (var f in faiscasSr) if (f != null) f.enabled = false;
    }

    void OnDisable()
    {
        if (fase != Fase.Fora) { fase = Fase.Fora; Restaura(); }
    }

    /// Mostra o efeito no instante "tempo" (segundos desde o comeco da fase f).
    public void Aplicar(float tempo, Fase f)
    {
        if (torreSr == null) torreSr = torre.GetComponent<SpriteRenderer>();
        Vector3 centro = torreSr != null ? torreSr.bounds.center : torre.transform.position;
        int ordem = torreSr != null ? torreSr.sortingOrder : 0;
        float voo = TempoVoo;
        float q = f == Fase.Entrando ? tempo - voo : 0f;      // segundos desde que o texto chegou na torre
        int passo = Mathf.FloorToInt(Mathf.Max(0f, q) / PassoQueda);

        // "0": so cai depois que o texto chega; balanca 1 pixel parado; some em fade na saida
        float desvio = 0f, alfa = 1f;
        if (f == Fase.Entrando) { desvio = Queda[Mathf.Min(passo, Queda.Length - 1)]; if (q < 0f) alfa = 0f; }
        else if (f == Fase.Parado) desvio = Mathf.FloorToInt(tempo / .5f) % 2 == 0 ? 0f : 1f;
        else if (f == Fase.Saindo) alfa = Alfa[Mathf.Min(3, Mathf.FloorToInt(tempo / Saida * 4f))];
        transform.position = marcaFinal + new Vector3(0f, desvio * Pixel, 0f);
        marcaSr.color = new Color(1f, 1f, 1f, alfa);

        // torre: cinza e escura enquanto zerada, com uma tremida de 1 pixel quando o texto bate
        float cor = f == Fase.Entrando ? Mathf.Clamp01(q / .3f) : (f == Fase.Parado ? 1f : 1f - Mathf.Clamp01(tempo / Saida));
        if (torreSr != null) torreSr.color = Color.Lerp(Color.white, Apagada, cor);
        Vector3 pos = torreBase;
        if (f == Fase.Entrando && q >= 0f && q < Tremida) pos.x += (passo % 2 == 0 ? 1 : -1) * Pixel;
        if ((torre.transform.position - torreBase).sqrMagnitude < 4f * Pixel * Pixel) torre.transform.position = pos;   // so mexe se ninguem mais mexeu nela

        // X de correcao: o primeiro risco .1 s depois da batida, o segundo .25 s depois
        bool xVisivel = f == Fase.Parado || f == Fase.Saindo || (f == Fase.Entrando && q >= .1f);
        xSr.enabled = xVisivel;
        if (xVisivel)
        {
            xSr.sprite = f == Fase.Entrando && q < .25f ? xMeio : xCheio;
            xSr.sortingOrder = ordem + 49;
            xSr.color = new Color(1f, 1f, 1f, f == Fase.Saindo ? alfa : .9f);
            xSr.transform.position = PixelSprites.Snap(new Vector3(centro.x, centro.y, 0f));
        }

        // "REPROVADO!": sai do professor e voa ate o meio da torre, acelerando
        bool textoVisivel = f == Fase.Entrando && temOrigem && q < .05f;
        textoSr.enabled = textoVisivel;
        if (textoVisivel)
        {
            float k = Mathf.Clamp01(tempo / voo);
            k = k * k * (3f - 2f * k);                           // sai devagar, passa rapido, chega suave
            textoSr.sortingOrder = ordem + 52;
            textoSr.transform.position = PixelSprites.Snap(Vector3.Lerp(origem, centro + new Vector3(0f, .2f, 0f), k) - new Vector3(0f, 0f, 0f));
        }

        // faiscas na volta
        for (int i = 0; i < faiscasSr.Length; i++)
        {
            bool vis = f == Fase.Saindo;
            faiscasSr[i].enabled = vis;
            if (!vis) continue;
            int sobe = Mathf.RoundToInt(6f * tempo / Saida);
            faiscasSr[i].sortingOrder = ordem + 51;
            faiscasSr[i].color = new Color(1f, 1f, 1f, alfa);
            faiscasSr[i].transform.position = PixelSprites.Snap(new Vector3(centro.x + Faiscas[i].x, centro.y + Faiscas[i].y - .5f + sobe * Pixel, 0f));
        }
    }

    static void Sprites()
    {
        if (xCheio != null) return;
        var vermelho = new Color32(230, 30, 30, 255);
        var escuro = new Color32(40, 8, 8, 255);
        xMeio = Risco(13, 13, false, vermelho, escuro, "risco_meio");
        xCheio = Risco(13, 13, true, vermelho, escuro, "risco_x");
        var f = new bool[3, 3];
        f[1, 0] = f[0, 1] = f[1, 1] = f[2, 1] = f[1, 2] = true;
        faisca = Preenche(f, 3, 3, new Color32(255, 240, 150, 255), escuro, "faisca");
    }

    // riscos de 2 pixels de espessura: o primeiro "\", o segundo "/"
    static Sprite Risco(int w, int h, bool completo, Color32 cor, Color32 contorno, string nome)
    {
        var f = new bool[w, h];
        int lado = w - 2;
        for (int i = 0; i < lado; i++)
        {
            int x = 1 + i, y = h - 2 - i;
            f[x, y] = true;
            if (x + 1 < w - 1) f[x + 1, y] = true;
            if (completo)
            {
                int x2 = w - 2 - i;
                f[x2, y] = true;
                if (x2 - 1 > 0) f[x2 - 1, y] = true;
            }
        }
        return Preenche(f, w, h, cor, contorno, nome);
    }

    static Sprite Preenche(bool[,] f, int w, int h, Color32 cor, Color32 contorno, string nome)
    {
        int W = w + 2, H = h + 2;
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool cheio = x > 0 && y > 0 && x <= w && y <= h && f[x - 1, y - 1];
                if (cheio) { px[y * W + x] = cor; continue; }
                bool perto = false;
                for (int dy = -1; dy <= 1 && !perto; dy++)
                    for (int dx = -1; dx <= 1 && !perto; dx++)
                    {
                        int nx = x + dx - 1, ny = y + dy - 1;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && f[nx, ny]) perto = true;
                    }
                px[y * W + x] = perto ? contorno : new Color32(0, 0, 0, 0);
            }
        return PixelSprites.Make(px, W, H, new Vector2(.5f, .5f), 1f / Pixel, nome);
    }
}
