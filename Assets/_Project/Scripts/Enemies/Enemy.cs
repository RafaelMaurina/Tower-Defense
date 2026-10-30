using System;
using UnityEngine;

/// Inimigo: anda por DISTANCIA no WaypointPath (ver WaypointPath.cs).
/// Contrato esperado pelo resto do jogo; a logica de movimento e vida entra na issue do Enemy.
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyData data;

    /// Distancia percorrida no caminho. Empurrao diminui, salto soma.
    public float Distance { get; set; }

    public bool Flying => data.flying;
    public bool IsAlive { get; private set; } = true;
    public Transform Transform => transform;

    /// GameManager escuta para somar dinheiro.
    public static event Action<Enemy> OnDied;
    /// GameManager escuta para tirar vida da base.
    public static event Action<Enemy> OnReachedBase;

    void OnEnable() => EnemyRegistry.Register(this);
    void OnDisable() => EnemyRegistry.Unregister(this);

    public void TakeDamage(float amount)
    {
        // Implementado na issue do Enemy.
    }
}
