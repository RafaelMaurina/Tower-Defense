using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Paused, Victory, Defeat }
public enum GameDifficulty { Easy, Normal, Hard }

/// Estado unico da partida. HUD e sistemas de jogo observam os eventos abaixo.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int Money => Instance != null ? Instance.money : 0;
    public static int BaseLife => Instance != null ? Instance.baseLife : 0;
    public static GameState State => Instance != null ? Instance.state : GameState.Menu;
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnMoneyChanged;
    public static event Action<int> OnBaseLifeChanged;
    public static event Action<int, int> OnWaveChanged;

    [SerializeField] int startingMoney = 150;
    [SerializeField] int startingLife = 20;
    [SerializeField] int money;
    [SerializeField] int baseLife;
    [SerializeField] GameState state = GameState.Menu;
    public static GameDifficulty Difficulty { get; private set; } = GameDifficulty.Easy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        money = startingMoney;
        baseLife = startingLife;
        Enemy.OnDied += HandleEnemyDied;
        Enemy.OnReachedBase += HandleEnemyReachedBase;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Enemy.OnDied -= HandleEnemyDied;
        Enemy.OnReachedBase -= HandleEnemyReachedBase;
        Instance = null;
        OnStateChanged = null; OnMoneyChanged = null; OnBaseLifeChanged = null; OnWaveChanged = null;
    }

    void Start()
    {
        OnMoneyChanged?.Invoke(money);
        OnBaseLifeChanged?.Invoke(baseLife);
        SetState(GameState.Playing);
    }

    public static void SetDifficulty(GameDifficulty value) => Difficulty = value;
    public void ResetGame() { money = startingMoney; baseLife = startingLife; Time.timeScale = 1f; SetState(GameState.Playing); OnMoneyChanged?.Invoke(money); OnBaseLifeChanged?.Invoke(baseLife); }
    public void CompleteLevel() {
        string current = SceneManager.GetActiveScene().name;
        string next = current == "Nivel_1_Primavera" ? "Nivel_2_Verao" : current == "Nivel_2_Verao" ? "Nivel_3_Outono" : null;
        if (next == null) CompleteGame(); else SceneManager.LoadScene(next);
    }
    public void SetState(GameState value)
    {
        if (state == value) return;
        state = value;
        Time.timeScale = value == GameState.Paused ? 0f : 1f;
        OnStateChanged?.Invoke(value);
    }

    public bool Spend(int amount)
    {
        if (amount < 0 || money < amount || state != GameState.Playing) return false;
        money -= amount; OnMoneyChanged?.Invoke(money); return true;
    }
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount; OnMoneyChanged?.Invoke(money);
    }
    public void DamageBase(int amount)
    {
        if (amount <= 0 || state != GameState.Playing) return;
        baseLife = Mathf.Max(0, baseLife - amount);
        OnBaseLifeChanged?.Invoke(baseLife);
        if (baseLife == 0) SetState(GameState.Defeat);
    }
    public void NotifyWave(int current, int total) => OnWaveChanged?.Invoke(current, total);
    public void CompleteGame() { if (state == GameState.Playing) { LocalRanking.Record(Difficulty, baseLife, money); SetState(GameState.Victory); } }
    public void TogglePause()
    {
        if (state == GameState.Playing) SetState(GameState.Paused);
        else if (state == GameState.Paused) SetState(GameState.Playing);
    }
    void HandleEnemyDied(Enemy enemy) { if (enemy != null && enemy.data != null) AddMoney(enemy.data.reward); }
    void HandleEnemyReachedBase(Enemy enemy) { if (enemy != null && enemy.data != null) DamageBase(enemy.data.baseDamage); }
}