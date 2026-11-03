using System;
using UnityEngine;

/// Inimigo: anda por DISTANCIA no WaypointPath (ver WaypointPath.cs).
/// O WaveSpawner instancia o prefab e chama Init(caminho).
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyData data;

    WaypointPath path;
    float health;

    /// Distancia percorrida no caminho. Empurrao diminui, salto soma.
    public float Distance { get; set; }

    public float Health => health;
    public bool Flying => data.flying;
    public bool IsAlive { get; private set; }
    public Transform Transform => transform;

    /// GameManager escuta para somar dinheiro.
    public static event Action<Enemy> OnDied;
    /// GameManager escuta para tirar vida da base.
    public static event Action<Enemy> OnReachedBase;

    void OnEnable() => EnemyRegistry.Register(this);
    void OnDisable() => EnemyRegistry.Unregister(this);

    /// Coloca o inimigo no inicio do caminho com a vida cheia.
    public void Init(WaypointPath caminho)
    {
        path = caminho;
        Distance = 0f;
        health = data.health;
        IsAlive = true;
        transform.position = path.PointAt(0f);
    }

    void Update()
    {
        if (!IsAlive) return;

        Distance += data.speed * Time.deltaTime;
        transform.position = path.PointAt(Distance);

        if (Distance >= path.Length) ReachBase();
    }

    /// Onde o inimigo estara depois de 'seconds', se continuar reto pelo caminho.
    /// E o que o projetil usa para mirar a frente. Nao considera desvios
    /// (ziguezague do pato), e por isso o pato escapa da mira.
    public Vector3 PredictPosition(float seconds) => path.PointAt(Distance + data.speed * seconds);

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        health -= amount;
        if (health <= 0f) Die();
    }

    void Die()
    {
        Finish();
        OnDied?.Invoke(this);
    }

    void ReachBase()
    {
        Finish();
        OnReachedBase?.Invoke(this);
    }

    // Sai da lista antes de avisar o jogo, para a torre nunca mirar num inimigo morto.
    // Destroy fica isolado aqui para trocar por pooling depois (issue #19).
    void Finish()
    {
        IsAlive = false;
        EnemyRegistry.Unregister(this);
        Destroy(gameObject);
    }
}
