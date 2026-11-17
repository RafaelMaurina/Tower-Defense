using UnityEngine;

/// Desbloqueio local independente do saldo, reinicio e ranking da partida.
public static class CampaignProgress
{
    public const string BonusScene = "Nivel_4_Campus";
    const string CompletedLevelsKey = "DefesaDaColheita.CompletedLevels.v1";
    const int AllCampaignLevels = 0b111;

#if UNITY_EDITOR
    public static void ResetCampaignProgressForTesting()
    {
        PlayerPrefs.DeleteKey(CompletedLevelsKey);
        PlayerPrefs.Save();
    }
#endif

    public static int CompletedCampaignLevelCount
    {
        get
        {
            int completed = PlayerPrefs.GetInt(CompletedLevelsKey, 0) & AllCampaignLevels;
            int count = 0;
            for (int bit = 1; bit <= 0b100; bit <<= 1)
                if ((completed & bit) != 0) count++;
            return count;
        }
    }

    public static bool BonusUnlocked =>
        (PlayerPrefs.GetInt(CompletedLevelsKey, 0) & AllCampaignLevels) == AllCampaignLevels;

    public static void Complete(string scene)
    {
        int levelBit = scene == "Nivel_2_Verao" ? 0b001
            : scene == "Nivel_1_Primavera" ? 0b010
            : scene == "Nivel_3_Outono" ? 0b100
            : 0;
        if (levelBit == 0) return;

        int completed = PlayerPrefs.GetInt(CompletedLevelsKey, 0) | levelBit;
        PlayerPrefs.SetInt(CompletedLevelsKey, completed);
        PlayerPrefs.Save();
    }
}
