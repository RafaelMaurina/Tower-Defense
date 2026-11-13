using UnityEngine;

/// Desbloqueio local independente do saldo, reinicio e ranking da partida.
public static class CampaignProgress
{
    public const string BonusScene = "Nivel_4_Campus";
    const string Key = "DefesaDaColheita.BonusUnlocked.v1";
    public static bool BonusUnlocked => PlayerPrefs.GetInt(Key, 0) == 1;
    public static void Complete(string scene)
    {
        if (scene != "Nivel_3_Outono" || BonusUnlocked) return;
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
    }
}
