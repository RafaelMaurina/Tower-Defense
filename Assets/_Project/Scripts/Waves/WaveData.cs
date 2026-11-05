using System;
using UnityEngine;

[Serializable]
public struct WaveEntry
{
    public EnemyData enemy;
    public GameObject enemyPrefab;
    [Min(1)] public int count;
    [Min(0f)] public float spawnInterval;
    [Tooltip("-1 escolhe um caminho aleatorio; 0=A, 1=B.")]
    public int pathIndex;
}

[CreateAssetMenu(menuName = "Defesa da Colheita/Wave Data", fileName = "Wave_")]
public class WaveData : ScriptableObject
{
    [Tooltip("Nome da cena do nivel que usa estas ondas, por exemplo Nivel_1_Primavera.")]
    public string levelScene;
    public WaveEntry[] entries;
    [Min(0f)] public float pauseAfter = 5f;
}
