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

    public static event System.Action OnChanged;

    static float music = DefaultMusic, sfx = DefaultSfx;
    static bool muted, fullScreen = true;
    static Vector2Int resolution;   // (0,0) = resolucao do monitor

    /// 0..1
    public static float MusicVolume { get => music; set { music = Mathf.Clamp01(value); Save(); } }
    /// 0..1
    public static float SfxVolume { get => sfx; set { sfx = Mathf.Clamp01(value); Save(); } }
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

    static void Save()
    {
        PlayerPrefs.SetFloat(MusicKey, music);
        PlayerPrefs.SetFloat(SfxKey, sfx);
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.SetInt(FullKey, fullScreen ? 1 : 0);
        PlayerPrefs.SetInt(WidthKey, resolution.x);
        PlayerPrefs.SetInt(HeightKey, resolution.y);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
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
        if (list.Count == 0) list.Add(new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height));
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    /// Resolucao em uso: a escolhida ou, sem escolha, a do monitor.
    public static Vector2Int CurrentResolution()
    {
        if (resolution.x > 0 && resolution.y > 0) return resolution;
        List<Vector2Int> all = AvailableResolutions();
        return all[all.Count - 1];
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
