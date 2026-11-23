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
    const string Key = "DefesaDaColheita.Ranking.CampanhaCompleta.v1";
    const string BonusKey = "DefesaDaColheita.Ranking.Bonus.v1";
    public static void Record(GameResult result)
    {
        if (!result.won) return;
        string key = result.bonus ? BonusKey : Key;
        var data = LoadData(key);
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
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
    public static IReadOnlyList<RankingEntry> GetEntries(bool bonus = false)
    {
        var bonusEntries = LoadData(BonusKey).entries;
        if (bonus) return bonusEntries;

        var entries = LoadData(Key).entries;
        entries.AddRange(bonusEntries.FindAll(entry => entry.won));
        entries.Sort((a, b) => b.score.CompareTo(a.score));
        return entries;
    }
    static RankingSaveData LoadData(string key)
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new RankingSaveData();
        try { var data = JsonUtility.FromJson<RankingSaveData>(json); return data ?? new RankingSaveData(); }
        catch (Exception) { return new RankingSaveData(); }
    }
}
