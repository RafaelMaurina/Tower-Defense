using System.Collections.Generic;
using UnityEngine;

/// Toca sons soltos de Assets/_Project/Resources/sons/<id> (.wav, .ogg ou .mp3). Se o arquivo nao existe,
/// nao faz nada: da para ir pondo os sons aos poucos, sem mexer em codigo.
///
/// ids de bichos: galinha, galo, vaca, ovelha, porco, gato
/// ids de combate: tiro_espantalho, tiro_canhaoabobora, tiro_metralhadorauvas, tiro_colmeia,
///                 tufao_chega, tufao_morde, inimigo_morre, inimigo_chega
public static class SoundBank
{
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => cache.Clear();

    public static void Play(string id, float volume = 1f)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!cache.TryGetValue(id, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>("sons/" + id);
            cache[id] = clip;                       // guarda tambem o "nao tem", para nao procurar a cada tiro
        }
        if (clip == null) return;
        Camera cam = Camera.main;
        AudioSource.PlayClipAtPoint(clip, cam != null ? cam.transform.position : Vector3.zero, volume);
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
    }

    static void HandleDied(Enemy e) => SoundBank.Play("inimigo_morre");
    static void HandleReachedBase(Enemy e) => SoundBank.Play("inimigo_chega");
    static void HandleBite(Guardian g, Vector3 where) => SoundBank.Play("tufao_morde");

    static void HandleFired(Tower tower, Vector3 aim)
    {
        if (tower == null || tower.data == null) return;
        SoundBank.Play("tiro_" + tower.data.name.Replace("Tower_", "").ToLowerInvariant());
    }
}
