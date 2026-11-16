using UnityEngine;

/// Desbloqueio local independente do saldo, reinicio e ranking da partida.
public static class CampaignProgress
{
    public const string BonusScene = "Nivel_4_Campus";
    const string Key = "DefesaDaColheita.BonusUnlocked.v1";
    const string CompletedLevelsKey = "DefesaDaColheita.CompletedLevels.v1";
    const int AllCampaignLevels = 0b111;

#if UNITY_EDITOR
    public static void ResetCampaignProgressForTesting()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(CompletedLevelsKey);
        PlayerPrefs.Save();
    }
#endif

    public static bool BonusUnlocked => PlayerPrefs.GetInt(Key, 0) == 1
        || (PlayerPrefs.GetInt(CompletedLevelsKey, 0) & AllCampaignLevels) == AllCampaignLevels;

    public static void Complete(string scene)
    {
        int levelBit = scene == "Nivel_2_Verao" ? 0b001
            : scene == "Nivel_1_Primavera" ? 0b010
            : scene == "Nivel_3_Outono" ? 0b100
            : 0;
        if (levelBit == 0) return;

        int completed = PlayerPrefs.GetInt(CompletedLevelsKey, 0) | levelBit;
        PlayerPrefs.SetInt(CompletedLevelsKey, completed);
        if ((completed & AllCampaignLevels) == AllCampaignLevels)
            PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
    }
}
