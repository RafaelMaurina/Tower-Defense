using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// Liga catalogo de torres, ondas, canteiros e HUD na cena gerada do nivel.
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] WaveData[] waves;
    [SerializeField] TowerData[] towerCatalog;

    public void Configure(TowerData[] towers, WaveData[] levelWaves)
    {
        towerCatalog = towers;
        waves = levelWaves;
    }

    public bool NeedsCatalogUpdate(TowerData[] towers, WaveData[] levelWaves)
    {
        return !Same(towerCatalog, towers) || !Same(waves, levelWaves);
    }

    static bool Same<T>(T[] current, T[] updated) where T : Object
    {
        if (current == null || updated == null) return current == updated;
        if (current.Length != updated.Length) return false;
        for (int i = 0; i < current.Length; i++)
            if (current[i] != updated[i]) return false;
        return true;
    }

    void Awake()
    {
        if (FindAnyObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        WaveSpawner spawner = GetComponent<WaveSpawner>();
        if (spawner == null) spawner = gameObject.AddComponent<WaveSpawner>();
        string currentScene = SceneManager.GetActiveScene().name;
        WaveData[] levelWaves = System.Array.FindAll(waves ?? new WaveData[0], wave =>
            wave != null && wave.levelScene == currentScene);
        System.Array.Sort(levelWaves, (a, b) => {
            int order = a.order.CompareTo(b.order);
            return order != 0 ? order : string.CompareOrdinal(a.name, b.name);
        });
        spawner.Configure(levelWaves);

        foreach (BuildPlot plot in GetComponentsInChildren<BuildPlot>(true))
            plot.SetAvailableTowers(towerCatalog);

        if (FindAnyObjectByType<GameHUD>() == null)
            new GameObject("GameHUD").AddComponent<GameHUD>();

        ApplyDifficulty();
    }

    bool CurrentIsBonus() => SceneManager.GetActiveScene().name == CampaignProgress.BonusScene;

    void ApplyDifficulty()
    {
        Light2D light = FindAnyObjectByType<Light2D>();
        if (light == null) return;
        // O campus acontece sempre a noite, independentemente da dificuldade escolhida.
        var lighting = CurrentIsBonus() ? GameDifficulty.Hard : GameManager.Difficulty;
        switch (lighting)
        {
            case GameDifficulty.Easy:
                light.color = new Color(1f, .95f, .78f);
                light.intensity = 1.15f;
                break;
            case GameDifficulty.Normal:
                light.color = new Color(1f, .62f, .42f);
                light.intensity = .9f;
                break;
            case GameDifficulty.Hard:
                light.color = new Color(.42f, .52f, .86f);
                light.intensity = .62f;
                gameObject.AddComponent<NightLights>();
                break;
        }
    }
}
