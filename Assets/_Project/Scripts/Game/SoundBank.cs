using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Toca sons soltos de Assets/_Project/Resources/sons/<id> (.wav, .ogg ou .mp3). Se o arquivo nao existe,
/// nao faz nada: da para ir pondo os sons aos poucos, sem mexer em codigo.
/// Varios arquivos para o mesmo id (galinha, galinha_2, galinha_3...) viram variacoes: toca um ao acaso.
///
/// ids de bichos: galinha, galo, vaca, ovelha, porco, gato (cliques); vaca_acorda (a vaca que dormia);
///                gato_coracao (5 cliques seguidos)
/// ids de combate: tiro_espantalho, tiro_canhaoabobora, tiro_metralhadorauvas, tiro_colmeia,
///                 tufao_chega, tufao_morde, inimigo_morre, inimigo_chega
/// ids de torre: constroi_<torre> (constroi_colmeia...), ao construir
/// ids de nascimento: nasce_<inimigo> (nasce_javali, nasce_morcego...), so quando o primeiro inimigo daquele
///                    tipo entra no mapa em cada fase (apresenta o monstro novo, nao toca a cada um)
public static class SoundBank
{
    static readonly Dictionary<string, List<AudioClip>> groups = new Dictionary<string, List<AudioClip>>();
    static bool loaded;
    // ultimo som tocado de cada id e quantas vezes seguidas: o mesmo arquivo nunca toca 3 vezes em fila
    static readonly Dictionary<string, (int index, int count)> lastPlayed = new Dictionary<string, (int, int)>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { groups.Clear(); lastPlayed.Clear(); loaded = false; }

    // le a pasta uma vez e agrupa as variacoes: "galinha_2" e "galinha_3" entram no grupo "galinha"
    static void Load()
    {
        loaded = true;
        groups.Clear();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("sons"))
        {
            string id = clip.name;
            int cut = id.LastIndexOf('_');
            if (cut > 0 && int.TryParse(id.Substring(cut + 1), out _)) id = id.Substring(0, cut);
            if (!groups.TryGetValue(id, out List<AudioClip> list)) groups[id] = list = new List<AudioClip>();
            list.Add(clip);
        }
    }

    /// Existe algum som para esse id?
    public static bool Has(string id)
    {
        if (!loaded) Load();
        return !string.IsNullOrEmpty(id) && groups.ContainsKey(id);
    }

    // sorteia uma variacao; depois de 2 iguais seguidas, sorteia outra (se o id so tem um arquivo, repete)
    static int PickIndex(string id, int count)
    {
        int index = Random.Range(0, count);
        if (lastPlayed.TryGetValue(id, out var last))
        {
            if (count > 1 && last.count >= 2)
                while (index == last.index) index = Random.Range(0, count);
            lastPlayed[id] = (index, index == last.index ? last.count + 1 : 1);
        }
        else lastPlayed[id] = (index, 1);
        return index;
    }

    // sons que tocam inteiros, sem o limite de 1 segundo com fadeout: o cocorico, a moedinha, a vitoria e a melhoria de torre
    static readonly HashSet<string> TocaInteiro = new HashSet<string> { "galo", "inimigo_morre", "vitoria", "torre_melhora" };

    public static void Play(string id, float volume = 1f)
    {
        if (!Has(id)) return;
        List<AudioClip> list = groups[id];
        AudioClip clip = list[PickIndex(id, list.Count)];
        if (clip == null) return;
        SomCurto.Tocar(clip, volume * GameSettings.Sfx, limitar: !TocaInteiro.Contains(id));
    }
}

/// Toca um som solto e o encerra com um fadeout: o som corre normal ate 1 segundo; dai o volume cai de forma suave em
/// 0,3 s e o som para. Sons mais curtos que 1 segundo tocam inteiros. Usa tempo real (nao para com o jogo pausado).
public class SomCurto : MonoBehaviour
{
    const float Duracao = 1f, Fade = .3f;

    AudioSource fonte;
    float volume, tempo;
    bool limitar = true;

    public static void Tocar(AudioClip clip, float volume, bool limitar = true)
    {
        var go = new GameObject("Som " + clip.name);
        var fonte = go.AddComponent<AudioSource>();
        fonte.clip = clip;
        fonte.volume = volume;
        fonte.spatialBlend = 0f;                 // 2D: nao muda com a posicao da camera
        fonte.playOnAwake = false;
        fonte.Play();
        var som = go.AddComponent<SomCurto>();
        som.fonte = fonte;
        som.volume = volume;
        som.limitar = limitar;
    }

    void Update()
    {
        tempo += Time.unscaledDeltaTime;
        if (fonte == null || !fonte.isPlaying || (limitar && tempo >= Duracao + Fade)) { Destroy(gameObject); return; }
        if (limitar && tempo > Duracao)
        {
            float t = (tempo - Duracao) / Fade;           // 0 -> 1
            fonte.volume = volume * (1f - t * t * (3f - 2f * t));   // queda suave (smoothstep), sem corte seco
        }
    }
}

/// Liga os eventos do jogo aos sons de combate (ver SoundBank).
public static class CombatSounds
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Subscribe()
    {
        Enemy.OnDied -= HandleDied;
        Enemy.OnDied += HandleDied;
        Enemy.OnReachedBase -= HandleReachedBase;
        Enemy.OnReachedBase += HandleReachedBase;
        Tower.OnFired -= HandleFired;
        Tower.OnFired += HandleFired;
        Guardian.OnBite -= HandleBite;
        Guardian.OnBite += HandleBite;
        WaveSpawner.OnEnemySpawned -= HandleSpawned;
        WaveSpawner.OnEnemySpawned += HandleSpawned;
        Tower.OnUpgraded -= HandleUpgraded;
        Tower.OnUpgraded += HandleUpgraded;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    static void HandleUpgraded(Tower t) => SoundBank.Play("torre_melhora");

    // o GameManager zera os seus eventos ao sair da fase: a cada fase carregada liga de novo
    static void HandleState(GameState state)
    {
        if (state == GameState.Victory) SoundBank.Play("vitoria");
    }

    // tipos de inimigo que ja se apresentaram nesta fase; recomeca a cada fase carregada
    static readonly HashSet<string> introduced = new HashSet<string>();

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        introduced.Clear();
        GameManager.OnStateChanged -= HandleState;
        GameManager.OnStateChanged += HandleState;
    }

    // o primeiro inimigo de cada tipo faz o som de nascimento; os seguintes do mesmo tipo ficam em silencio
    static void HandleSpawned(Enemy e)
    {
        if (e == null || e.data == null) return;
        string id = "nasce_" + e.data.name.Replace("Enemy_", "").ToLowerInvariant();
        if (!introduced.Add(id)) return;
        SoundBank.Play(id, .7f);
    }

    static void HandleDied(Enemy e) => SoundBank.Play("inimigo_morre", .5f);   // a moedinha: toca a cada inimigo, entao mais baixa
    static void HandleReachedBase(Enemy e) => SoundBank.Play("inimigo_chega");
    static void HandleBite(Guardian g, Vector3 where) => SoundBank.Play("tufao_morde");

    static void HandleFired(Tower tower, Vector3 aim)
    {
        if (tower == null || tower.data == null) return;
        string id = "tiro_" + tower.data.name.Replace("Tower_", "").ToLowerInvariant();
        SoundBank.Play(id, id == "tiro_colmeia" ? .45f : 1f);   // a colmeia atira rapido: mais baixo
    }
}
