using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RankingEntry
{
    public string difficulty;
    public int score;
    public string date;
}
[Serializable]
class RankingSaveData { public List<RankingEntry> entries = new List<RankingEntry>(); }

/// Ranking local simples, persistido entre execucoes com PlayerPrefs.
public static class LocalRanking
{
    const string Key = "DefesaDaColheita.Ranking.v1";
    const int Limit = 10;
    public static void Record(GameDifficulty difficulty, int remainingLife, int money)
    {
        var data = LoadData();
        data.entries.Add(new RankingEntry { difficulty = difficulty.ToString(), score = Mathf.Max(0, remainingLife) * 100 + Mathf.Max(0, money), date = DateTime.Now.ToString("yyyy-MM-dd") });
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