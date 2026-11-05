using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankingEntry
{
    public string difficulty;
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
    const string Key = "DefesaDaColheita.Ranking.v1";
    const int Limit = 10;
    public static void Record(GameResult result)
    {
        var data = LoadData();
        data.entries.Add(new RankingEntry
        {
            difficulty = result.difficulty.ToString(),
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
    public static IReadOnlyList<RankingEntry> GetEntries() => LoadData().entries;
    static RankingSaveData LoadData()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new RankingSaveData();
        try { var data = JsonUtility.FromJson<RankingSaveData>(json); return data ?? new RankingSaveData(); }
        catch (Exception) { return new RankingSaveData(); }
    }
}