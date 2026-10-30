using System;
using UnityEngine;

/// Um nivel de upgrade da torre.
[Serializable]
public struct TowerLevel
{
    public int cost;          // preco de construir (nivel 1) ou de subir para este nivel
    public float damage;
    public float range;       // em unidades de mundo
    public float fireRate;    // tiros por segundo
    public Sprite sprite;
}

/// Dados de uma torre, com 3 niveis. Joao le o custo daqui ao construir no canteiro.
[CreateAssetMenu(menuName = "Defesa da Colheita/Tower Data", fileName = "Tower_")]
public class TowerData : ScriptableObject
{
    public string displayName;
    public TowerLevel[] levels = new TowerLevel[3];
    public bool hitsFlying;         // so 2 das 5 torres atingem aereos
    public GameObject projectile;   // prefab do projetil
}
