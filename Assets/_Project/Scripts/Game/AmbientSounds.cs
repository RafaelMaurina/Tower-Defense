using UnityEngine;
using UnityEngine.SceneManagement;

/// Sons de ambiente nas fases, baixinho e de tempos em tempos: de dia o pisco; de noite (Campus e modo Dificil) a
/// coruja e o sapo (Resources/sons: ambiente_pisco, ambiente_coruja, ambiente_sapo).
public class AmbientSounds : MonoBehaviour
{
    const float VolumeAmbiente = .3f;

    string[] ids = new string[0];
    float timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        var go = new GameObject("Sons de ambiente");
        DontDestroyOnLoad(go);
        var ambiente = go.AddComponent<AmbientSounds>();
        SceneManager.sceneLoaded += (cena, modo) => ambiente.Trocar(cena.name);
        ambiente.Trocar(SceneManager.GetActiveScene().name);
    }

    void Trocar(string cena)
    {
        // a noite e o Campus e o modo Dificil (os mesmos niveis em que a camera escurece o fundo)
        bool noite = cena == CampaignProgress.BonusScene || GameManager.Difficulty == GameDifficulty.Hard;
        if (!cena.StartsWith("Nivel_")) ids = new string[0];
        else ids = noite ? new[] { "ambiente_coruja", "ambiente_sapo" } : new[] { "ambiente_pisco" };
        timer = Random.Range(6f, 14f);
    }

    void Update()
    {
        if (ids.Length == 0) return;
        timer -= Time.deltaTime;                  // com o jogo pausado o tempo nao corre
        if (timer > 0f) return;
        SoundBank.Play(ids[Random.Range(0, ids.Length)], VolumeAmbiente);
        timer = Random.Range(12f, 30f);
    }
}
