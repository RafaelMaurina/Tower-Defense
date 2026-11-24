using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankingEntry
{
    public string difficulty;
    public string phaseScene;
    public int score;
    public string date;
    public int round;
    public string topDamageTower;
    public float topDamage;
    public bool won;
}
[Serializable]
class RankingSaveData { public List<RankingEntry> entries = new List<RankingEntry>(); }

/// Ranking local simples, persistido entre execucoes com PlayerPrefs.
public static class LocalRanking
{
    // Um ranking so, com todas as fases e o bonus. Chave nova: nao mistura com o antigo de campanha completa
    const string Key = "DefesaDaColheita.Ranking.Fases.v1";
    const int Limit = 10;
    public static void Record(GameResult result)
    {
        if (!result.won) return;
        var data = LoadData(Key);
        data.entries.Add(new RankingEntry
        {
            difficulty = result.difficulty.ToString(),
            phaseScene = result.phaseScene,
            score = result.score,
            date = DateTime.Now.ToString("yyyy-MM-dd"),
            round = result.round,
            topDamageTower = result.topDamageTower,
            topDamage = result.topDamage,
            won = result.won
        });
        data.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (data.entries.Count > Limit) data.entries.RemoveRange(Limit, data.entries.Count - Limit);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
    // o parametro fica so para a tela de ranking; a lista e uma so
    public static IReadOnlyList<RankingEntry> GetEntries(bool bonus = false) => LoadData(Key).entries;
    static RankingSaveData LoadData(string key)
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new RankingSaveData();
        try { var data = JsonUtility.FromJson<RankingSaveData>(json); return data ?? new RankingSaveData(); }
        catch (Exception) { return new RankingSaveData(); }
    }
}
