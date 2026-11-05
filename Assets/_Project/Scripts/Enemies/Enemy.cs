using System;
using UnityEngine;

/// Inimigo: anda por DISTANCIA no WaypointPath (ver WaypointPath.cs).
/// O WaveSpawner instancia o prefab e chama Init(caminho).
public class Enemy : MonoBehaviour, IDamageable
{
    public EnemyData data;

    WaypointPath path;
    SpriteRenderer sr;
    float health;
    float animTime;

    /// Distancia percorrida no caminho. Empurrao diminui, salto soma.
    public float Distance { get; set; }

    public float Health => health;
    public bool IsFlying => data != null && data.flying;
    public bool Flying => IsFlying;
    public bool IsAlive { get; private set; }
    public Transform Transform => transform;

    /// GameManager escuta para somar dinheiro.
    public static event Action<Enemy> OnDied;
    /// GameManager escuta para tirar vida da base.
    public static event Action<Enemy> OnReachedBase;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnEnable() => EnemyRegistry.Register(this);
    void OnDisable() => EnemyRegistry.Unregister(this);

    /// Coloca o inimigo no inicio do caminho com a vida cheia.
    public void Init(WaypointPath caminho)
    {
        path = caminho;
        Distance = 0f;
        health = data.health;
        IsAlive = true;
        animTime = 0f;
        transform.position = path.PointAt(0f);
        UpdateOrder();
        Animate();
    }

    void Update()
    {
        if (!IsAlive) return;

        Distance += data.speed * Time.deltaTime;
        transform.position = path.PointAt(Distance);
        UpdateOrder();
        animTime += Time.deltaTime;
        Animate();

        if (Distance >= path.Length) ReachBase();
    }

    // O inimigo anda, entao a ordem de desenho acompanha a altura a cada quadro (ver DrawOrder).
    void UpdateOrder()
    {
        if (sr != null) sr.sortingOrder = DrawOrder.ForY(transform.position.y);
    }

    // Troca o quadro pelo tempo. Sem quadros (ex.: placeholder), mantem o sprite do prefab.
    // Os sprites olham para a direita e os caminhos so vao para a direita ou na vertical;
    // se um caminho passar a ir para a esquerda, espelhar aqui (sr.flipX).
    void Animate()
    {
        if (sr == null || data.frames == null || data.frames.Length == 0) return;
        int i = (int)(animTime * data.framesPerSecond) % data.frames.Length;
        sr.sprite = data.frames[i];
    }

    /// Centro do corpo. O pivo do sprite fica no pe; projetil mira e acerta aqui.
    public Vector3 Center => transform.position + (Vector3)data.centerOffset;

    /// Onde o CENTRO do inimigo estara depois de 'seconds', se continuar reto pelo caminho.
    /// E o que o projetil usa para mirar a frente. Nao considera desvios
    /// (ziguezague do pato), e por isso o pato escapa da mira.
    public Vector3 PredictPosition(float seconds) =>
        path.PointAt(Distance + data.speed * seconds) + (Vector3)data.centerOffset;

    public void TakeDamage(float amount) => ApplyDamage(amount);

    /// Aplica dano e retorna apenas a vida realmente removida (sem contar excesso).
    public float ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return 0f;
        float actualDamage = Mathf.Min(health, amount);
        health -= actualDamage;
        if (health <= 0f) Die();
        return actualDamage;
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
