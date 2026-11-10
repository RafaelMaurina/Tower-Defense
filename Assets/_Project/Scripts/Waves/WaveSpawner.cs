using System.Collections;
using UnityEngine;

/// Instancia ondas definidas por assets WaveData nos caminhos da cena ativa.
public class WaveSpawner : MonoBehaviour
{
    [SerializeField] WaveData[] waves;
    [SerializeField] WaypointPath[] paths;
    int liveEnemies;
    bool waveActive;
    bool startRequested;
    bool autoStart;
    public int CurrentWave { get; private set; }
    public int TotalWaves => waves == null ? 0 : waves.Length;
    public bool IsWaveActive => waveActive;
    public bool AutoStart => autoStart;
    public bool CanStartNextWave => !waveActive && CurrentWave < TotalWaves && GameManager.State == GameState.Playing;

    void Awake()
    {
        if (paths == null || paths.Length == 0) paths = GetComponentsInChildren<WaypointPath>(true);
    }

    void OnEnable() => Enemy.OnSpawnedByEnemy += HandleSpawnedByEnemy;
    void OnDisable() => Enemy.OnSpawnedByEnemy -= HandleSpawnedByEnemy;

    void Start() { if (waves != null && waves.Length > 0) StartCoroutine(Run()); }

    public void Configure(WaveData[] waveData) => waves = waveData;

    public void Begin() => StartNextWave();

    public void StartNextWave()
    {
        if (CanStartNextWave) startRequested = true;
    }

    public void SetAutoStart(bool enabled) => autoStart = enabled;

    IEnumerator Run()
    {
        // Em Play direto no nivel, GameManager.Start pode rodar depois deste Start.
        while (GameManager.State == GameState.Menu) yield return null;
        int totalEnemies = 0;
        foreach (WaveData wave in waves)
            if (wave != null && wave.entries != null)
                foreach (WaveEntry entry in wave.entries) totalEnemies += Mathf.Max(0, entry.count);
        GameManager.Instance?.RegisterPlannedEnemies(totalEnemies);

        for (int w = 0; w < waves.Length; w++)
        {
            while (!startRequested && !autoStart)
            {
                if (GameManager.State != GameState.Playing && GameManager.State != GameState.Paused) yield break;
                yield return null;
            }
            if (w > 0 && autoStart && !startRequested && waves[w - 1] != null)
            {
                float delay = waves[w - 1].pauseAfter;
                while (delay > 0f && autoStart && !startRequested)
                { delay -= Time.deltaTime; yield return null; }
            }
            while (!startRequested && !autoStart)
            {
                if (GameManager.State != GameState.Playing && GameManager.State != GameState.Paused) yield break;
                yield return null;
            }
            while (GameManager.State == GameState.Paused || Time.timeScale == 0f) yield return null;
            if (GameManager.State != GameState.Playing) yield break;
            startRequested = false;
            waveActive = true;
            CurrentWave = w + 1;
            WaveData wave = waves[w];
            bool hasFlyingEnemies = false;
            if (wave != null && wave.entries != null)
                foreach (WaveEntry entry in wave.entries)
                    if (entry.count > 0 && entry.enemy != null && entry.enemy.flying)
                    {
                        hasFlyingEnemies = true;
                        break;
                    }
            GameManager.Instance?.NotifyWave(CurrentWave, waves.Length, hasFlyingEnemies);
            if (wave == null || wave.entries == null) continue;

            foreach (WaveEntry entry in wave.entries)
            {
                if (entry.enemy == null || entry.enemyPrefab == null)
                {
                    Debug.LogError("WaveData precisa de EnemyData e prefab atribuidos.", wave);
                    continue;
                }

                for (int i = 0; i < Mathf.Max(0, entry.count); i++)
                {
                    while (GameManager.State == GameState.Paused || Time.timeScale == 0f) yield return null;
                    if (GameManager.State != GameState.Playing) yield break;
                    WaypointPath path = SelectPath(entry.pathIndex);
                    if (path == null)
                    {
                        Debug.LogError("WaveSpawner nao encontrou WaypointPath.", this);
                        yield break;
                    }

                    GameObject go = Pool.Get(entry.enemyPrefab, Vector3.zero, Quaternion.identity);
                    Enemy enemy = go.GetComponent<Enemy>();
                    if (enemy == null)
                    {
                        Debug.LogError("Prefab de inimigo precisa do componente Enemy.", go);
                        Destroy(go);
                    }
                    else
                    {
                        enemy.data = entry.enemy;
                        enemy.Init(path);
                        liveEnemies++;
                        enemy.Finished += OnEnemyFinished;
                    }

                    if (entry.spawnInterval > 0f) yield return new WaitForSeconds(entry.spawnInterval);
                }
            }

            while (liveEnemies > 0 && (GameManager.State == GameState.Playing || GameManager.State == GameState.Paused)) yield return null;
            while (GameManager.State == GameState.Paused || Time.timeScale == 0f) yield return null;
            if (GameManager.State != GameState.Playing) yield break;
            waveActive = false;
        }

        while (GameManager.State == GameState.Paused || Time.timeScale == 0f) yield return null;
        if (GameManager.State == GameState.Playing) GameManager.Instance?.CompleteLevel();
    }

    // Evento do proprio inimigo, e nao varredura por IsAlive: com pooling o mesmo objeto pode
    // renascer antes de uma varredura ver que ele morreu, e a onda nunca terminaria.
    void OnEnemyFinished(Enemy enemy) => liveEnemies = Mathf.Max(0, liveEnemies - 1);

    // Copia do AFN e invocados do boss tambem contam como inimigo vivo da onda (a onda so acaba
    // quando eles sairem) e como inimigo previsto, para o placar de derrotados continuar coerente.
    void HandleSpawnedByEnemy(Enemy parent, Enemy copy)
    {
        liveEnemies++;
        copy.Finished += OnEnemyFinished;
        GameManager.Instance?.RegisterPlannedEnemies(1);
    }

    WaypointPath SelectPath(int requested)
    {
        if (paths == null || paths.Length == 0) return null;
        int index = requested < 0 ? Random.Range(0, paths.Length) : Mathf.Clamp(requested, 0, paths.Length - 1);
        return paths[index];
    }
}
