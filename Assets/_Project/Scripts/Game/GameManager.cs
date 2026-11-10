using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Paused, Victory, Defeat }
public enum GameDifficulty { Easy, Normal, Hard }

[Serializable]
public struct GameResult
{
    public bool won;
    public int score;
    public int round;
    public int enemiesDefeated;
    public int enemiesTotal;
    public int baseLife;
    public int money;
    public string topDamageTower;
    public float topDamage;
    public GameDifficulty difficulty;
}

/// Estado, economia e resumo da partida. HUD e sistemas observam os eventos abaixo.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int Money => Instance != null ? Instance.money : 0;
    public static int BaseLife => Instance != null ? Instance.baseLife : 0;
    public static int StartingLife => Instance != null ? Instance.startingLife : 0;
    public static int StateRound => Instance != null ? Instance.round : 0;
    public static GameState State => Instance != null ? Instance.state : GameState.Menu;
    public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Easy;
    public static float GameSpeed => Instance != null ? Instance.gameSpeed : 1f;
    public static bool IsBuildPaused => Instance != null && Instance.buildPaused;
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnMoneyChanged;
    public static event Action<int> OnBaseLifeChanged;
    public static event Action<int, int> OnWaveChanged;
    public static event Action<int, int, bool> OnWaveCompositionChanged;
    public static event Action<GameResult> OnGameEnded;

    [SerializeField] int startingMoney = 150;
    [SerializeField] int startingLife = 20;
    [SerializeField] int money;
    [SerializeField] int baseLife;
    [SerializeField] GameState state = GameState.Menu;

    int moneyReceived;
    int enemiesPlanned;
    int enemiesDefeated;
    int round;
    string activeLevelScene;
    string topDamageTower = "Nenhuma";
    float topDamage;
    readonly Dictionary<Tower, float> damageByTower = new Dictionary<Tower, float>();
    float gameSpeed = 1f;
    bool decisionPaused;
    bool buildPaused;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        money = Mathf.Max(0, startingMoney);
        startingMoney = money;
        baseLife = Mathf.Max(1, startingLife);
        startingLife = baseLife;
        moneyReceived = startingMoney;
        activeLevelScene = SceneManager.GetActiveScene().name;
        Enemy.OnDied += HandleEnemyDied;
        Enemy.OnReachedBase += HandleEnemyReachedBase;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Enemy.OnDied -= HandleEnemyDied;
        Enemy.OnReachedBase -= HandleEnemyReachedBase;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
        OnStateChanged = null;
        OnMoneyChanged = null;
        OnBaseLifeChanged = null;
        OnWaveChanged = null;
        OnWaveCompositionChanged = null;
        OnGameEnded = null;
    }

    void Start()
    {
        OnMoneyChanged?.Invoke(money);
        OnBaseLifeChanged?.Invoke(baseLife);
        SetState(GameState.Playing);
    }

    public static void SetDifficulty(GameDifficulty value) => Difficulty = value;

    public void ResetGame()
    {
        ResetRunData();
        decisionPaused = false;
        buildPaused = false;
        SetState(GameState.Playing);
        ApplyTimeScale();
        OnMoneyChanged?.Invoke(money);
        OnBaseLifeChanged?.Invoke(baseLife);
    }

    public void RestartCurrentLevel()
    {
        string scene = string.IsNullOrEmpty(activeLevelScene) ? SceneManager.GetActiveScene().name : activeLevelScene;
        ResetRunData();
        decisionPaused = false;
        buildPaused = false;
        SetState(GameState.Playing);
        ApplyTimeScale();
        SceneManager.LoadScene(scene);
    }

    void ResetRunData()
    {
        money = startingMoney;
        moneyReceived = startingMoney;
        baseLife = startingLife;
        enemiesPlanned = 0;
        enemiesDefeated = 0;
        round = 0;
        topDamageTower = "Nenhuma";
        topDamage = 0f;
        damageByTower.Clear();
    }

    public void CompleteLevel()
    {
        string current = SceneManager.GetActiveScene().name;
        string next = current == "Nivel_1_Primavera" ? "Nivel_2_Verao" : current == "Nivel_2_Verao" ? "Nivel_3_Outono" : null;
        if (next == null) CompleteGame();
        else SceneManager.LoadScene(next);
    }

    public void SetState(GameState value)
    {
        if (state == value) return;
        if (value == GameState.Menu || value == GameState.Victory || value == GameState.Defeat)
            buildPaused = false;
        state = value;
        ApplyTimeScale();
        OnStateChanged?.Invoke(value);
    }

    public void SetGameSpeed(float speed)
    {
        if (speed != 1f && speed != 1.5f && speed != 2f) return;
        gameSpeed = speed;
        ApplyTimeScale();
    }

    public void SetDecisionPause(bool paused)
    {
        decisionPaused = paused;
        ApplyTimeScale();
    }

    public void SetBuildPause(bool paused)
    {
        buildPaused = paused;
        ApplyTimeScale();
    }

    void ApplyTimeScale()
    {
        Time.timeScale = state == GameState.Paused || decisionPaused || buildPaused ? 0f
            : state == GameState.Playing ? gameSpeed : 1f;
    }

    public bool Spend(int amount)
    {
        if (amount < 0 || money < amount || state != GameState.Playing) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0 || state != GameState.Playing) return;
        money += amount;
        moneyReceived += amount;
        OnMoneyChanged?.Invoke(money);
    }

    public void DamageBase(int amount)
    {
        if (amount <= 0 || state != GameState.Playing) return;
        baseLife = Mathf.Max(0, baseLife - amount);
        OnBaseLifeChanged?.Invoke(baseLife);
        if (baseLife == 0) EndGame(false);
    }

    public void RegisterPlannedEnemies(int amount) => enemiesPlanned += Mathf.Max(0, amount);

    public void NotifyWave(int current, int total)
    {
        round = Mathf.Max(round, current);
        OnWaveChanged?.Invoke(current, total);
    }

    public void NotifyWave(int current, int total, bool hasFlyingEnemies)
    {
        NotifyWave(current, total);
        OnWaveCompositionChanged?.Invoke(current, total, hasFlyingEnemies);
    }

    public void RegisterTowerDamage(Tower tower, float amount)
    {
        if (tower == null || amount <= 0f) return;
        string towerName = tower.data != null && !string.IsNullOrEmpty(tower.data.displayName)
            ? tower.data.displayName : tower.name;
        damageByTower.TryGetValue(tower, out float accumulated);
        accumulated += amount;
        damageByTower[tower] = accumulated;
        if (accumulated > topDamage)
        {
            topDamage = accumulated;
            Vector3 position = tower.transform.position;
            topDamageTower = $"{towerName} ({position.x:0.0}, {position.y:0.0})";
        }
    }

    public void CompleteGame()
    {
        if (state == GameState.Playing) EndGame(true);
    }

    void EndGame(bool won)
    {
        int score = CalculateScore(enemiesDefeated, enemiesPlanned, baseLife, startingLife, money, moneyReceived);
        var result = new GameResult
        {
            won = won,
            score = score,
            round = round,
            enemiesDefeated = enemiesDefeated,
            enemiesTotal = enemiesPlanned,
            baseLife = baseLife,
            money = money,
            topDamageTower = topDamageTower,
            topDamage = topDamage,
            difficulty = Difficulty
        };
        LocalRanking.Record(result);
        SetState(won ? GameState.Victory : GameState.Defeat);
        OnGameEnded?.Invoke(result);
    }

    public static int CalculateScore(int defeated, int total, int remainingLife, int initialLife, int remainingMoney, int moneyReceived)
    {
        float defeatedRatio = total > 0 ? Mathf.Clamp01(defeated / (float)total) : 0f;
        float lifeRatio = initialLife > 0 ? Mathf.Clamp01(remainingLife / (float)initialLife) : 0f;
        float moneyRatio = moneyReceived > 0 ? Mathf.Clamp01(remainingMoney / (float)moneyReceived) : 0f;
        return Mathf.RoundToInt(1000f * (defeatedRatio + lifeRatio + moneyRatio) / 3f);
    }

    public void TogglePause()
    {
        if (state == GameState.Playing) SetState(GameState.Paused);
        else if (state == GameState.Paused) SetState(GameState.Playing);
    }

    void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null || state != GameState.Playing) return;
        enemiesDefeated++;
        AddMoney(enemy.data.reward);
    }

    void HandleEnemyReachedBase(Enemy enemy)
    {
        if (enemy != null && enemy.data != null) DamageBase(enemy.data.baseDamage);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Menu") activeLevelScene = scene.name;
    }
}
