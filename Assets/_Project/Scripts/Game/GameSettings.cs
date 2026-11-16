using System.Collections.Generic;
using UnityEngine;

/// Configuracoes do jogador (volume de musica, volume de efeitos, mudo, resolucao e modo de tela),
/// guardadas em PlayerPrefs e aplicadas ao abrir o jogo. Padrao: tela cheia na resolucao do monitor,
/// musica em 60% e efeitos em 80%.
/// Efeitos: SoundBank e os sons da interface multiplicam o volume por GameSettings.Sfx.
/// Musica: MusicPlayer usa GameSettings.Music.
public static class GameSettings
{
    const string MusicKey = "cfg_musica", SfxKey = "cfg_efeitos", MuteKey = "cfg_mudo",
        WidthKey = "cfg_res_w", HeightKey = "cfg_res_h", FullKey = "cfg_tela_cheia";
    public const float DefaultMusic = .6f, DefaultSfx = .8f;
    const float WindowLimit = .85f;   // fracao maxima do monitor que a janela ocupa

    public static event System.Action OnChanged;

    static float music = DefaultMusic, sfx = DefaultSfx;
    static bool muted, fullScreen = true;
    static Vector2Int resolution;   // (0,0) = resolucao da area de trabalho

    /// 0..1
    /// Os volumes so mudam em memoria (o slider dispara a cada passo); chame Persist() ao soltar o slider.
    public static float MusicVolume { get => music; set { music = Mathf.Clamp01(value); OnChanged?.Invoke(); } }
    /// 0..1
    public static float SfxVolume { get => sfx; set { sfx = Mathf.Clamp01(value); OnChanged?.Invoke(); } }
    public static bool Muted { get => muted; set { muted = value; Save(); } }
    public static bool FullScreen => fullScreen;
    public static Vector2Int Resolution => resolution;

    /// Volume final da musica, ja com o mudo.
    public static float Music => muted ? 0f : music;
    /// Multiplicador final dos efeitos, ja com o mudo.
    public static float Sfx => muted ? 0f : sfx;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        music = PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
        sfx = PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
        muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        fullScreen = PlayerPrefs.GetInt(FullKey, 1) == 1;
        resolution = new Vector2Int(PlayerPrefs.GetInt(WidthKey, 0), PlayerPrefs.GetInt(HeightKey, 0));
        ApplyScreen();
    }

    /// Grava tudo em PlayerPrefs (volumes: chamar ao soltar o slider ou fechar o painel).
    public static void Persist()
    {
        PlayerPrefs.SetFloat(MusicKey, music);
        PlayerPrefs.SetFloat(SfxKey, sfx);
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.SetInt(FullKey, fullScreen ? 1 : 0);
        PlayerPrefs.SetInt(WidthKey, resolution.x);
        PlayerPrefs.SetInt(HeightKey, resolution.y);
        PlayerPrefs.Save();
    }

    static void Save()
    {
        Persist();
        OnChanged?.Invoke();
    }

    /// Alt+Enter troca o modo de tela por fora do GameSettings: le o modo real e guarda.
    public static void SyncFullScreen()
    {
        if (Screen.fullScreen == fullScreen) return;
        fullScreen = Screen.fullScreen;
        Save();
    }

    /// Resolucao da area de trabalho (a do monitor).
    static Vector2Int Desktop()
    {
        Resolution d = Screen.currentResolution;
        return new Vector2Int(d.width, d.height);
    }

    /// Resolucoes que o monitor aceita (sem repetir por taxa de atualizacao), da menor para a maior.
    public static List<Vector2Int> AvailableResolutions()
    {
        var list = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (r.width >= 800 && r.height >= 450 && !list.Contains(size)) list.Add(size);
        }
        // a salva pode nao estar na lista (monitor trocado): entra para o painel nao mostrar outra
        if (resolution.x > 0 && resolution.y > 0 && !list.Contains(resolution)) list.Add(resolution);
        if (list.Count == 0) list.Add(Desktop());
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    /// Resolucao em uso: a escolhida ou, sem escolha, a do monitor.
    public static Vector2Int CurrentResolution()
    {
        if (resolution.x > 0 && resolution.y > 0) return resolution;
        return Desktop();
    }

    public static void SetResolution(Vector2Int size)
    {
        resolution = size;
        ApplyScreen();
        Save();
    }

    public static void SetFullScreen(bool value)
    {
        fullScreen = value;
        ApplyScreen();
        Save();
    }

    static void ApplyScreen()
    {
        Vector2Int size = CurrentResolution();
        if (!fullScreen)
        {
            // janela nunca ocupa o monitor todo: senao a barra de titulo sai da tela
            Vector2Int desk = Desktop();
            float scale = Mathf.Min(1f, Mathf.Min(WindowLimit * desk.x / size.x, WindowLimit * desk.y / size.y));
            size = new Vector2Int(Mathf.RoundToInt(size.x * scale), Mathf.RoundToInt(size.y * scale));
        }
        Screen.SetResolution(size.x, size.y, fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }
}

/// Toca a musica de fundo em loop (Assets/_Project/Resources/musica/<arquivo>) no volume das configuracoes.
/// Sem arquivo na pasta nao faz nada: a musica entra quando a equipe adicionar o arquivo.
public class MusicPlayer : MonoBehaviour
{
    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("MusicPlayer");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicPlayer>();
    }

    void Start()
    {
        AudioClip[] clips = Resources.LoadAll<AudioClip>("musica");
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = GameSettings.Music;
        if (clips.Length > 0) { source.clip = clips[Random.Range(0, clips.Length)]; source.Play(); }
    }

    void Update()
    {
        if (source != null) source.volume = GameSettings.Music;
    }
}
