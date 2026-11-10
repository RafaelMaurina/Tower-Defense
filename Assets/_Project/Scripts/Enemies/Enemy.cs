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
    float zigzagTime;  // tempo desde o inicio, so para a fase do ziguezague
    float jumpTimer;   // tempo andando ate o proximo salto, ou tempo ja gasto no salto
    bool jumping;
    bool split;        // ja se dividiu, ou e a copia de quem se dividiu: so divide uma vez
    Boss boss;         // so no boss (Chomsky): fases, escudo, invocacao
    EnemyHealthBar healthBar;
    // Movimento deste momento: vem de data e o boss troca a cada fase (SetMovement).
    float speed, jumpInterval, jumpDistance, jumpDuration;

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
    /// Um inimigo criou outro em jogo (copia do AFN, invocado do boss): (pai, filho).
    /// O WaveSpawner escuta para contar o filho na onda.
    public static event Action<Enemy, Enemy> OnSpawnedByEnemy;
    /// Avisa quem gerou este inimigo (WaveSpawner) que ele saiu de jogo, por morte ou por chegar
    /// na base. Vale so para esta vida: e zerado ao devolver ao Pool, pois o objeto e reaproveitado.
    public event Action<Enemy> Finished;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        boss = GetComponent<Boss>();
    }

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
        split = false;
        zigzagTime = 0f;
        SetMovement(data.speed, data.jumpInterval, data.jumpDistance, data.jumpDuration);
        if (boss != null) boss.Begin();
        transform.position = path.PointAt(0f);
        UpdateOrder();
        Animate();
        if (healthBar == null) healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(this);
    }

    void Update()
    {
        if (!IsAlive) return;

        Distance += CurrentSpeed() * Time.deltaTime;
        zigzagTime += Time.deltaTime;
        transform.position = path.PointAt(Distance) + ZigzagOffset();
        UpdateOrder();
        animTime += Time.deltaTime;
        Animate();

        if (Distance >= path.Length) ReachBase();
    }

    // Velocidade deste quadro. Quem salta (gafanhoto) alterna andar e saltar; os demais so andam.
    float CurrentSpeed()
    {
        if (jumpDistance <= 0f || jumpInterval <= 0f) return speed;

        float duration = Mathf.Max(0.05f, jumpDuration);
        jumpTimer += Time.deltaTime;
        if (jumping)
        {
            if (jumpTimer >= duration) { jumping = false; jumpTimer = 0f; }
            return jumpDistance / duration;
        }
        if (jumpTimer >= jumpInterval) { jumping = true; jumpTimer = 0f; }
        return speed;
    }

    /// Troca o movimento (o boss muda a cada fase) e recomeca o ciclo de salto.
    public void SetMovement(float newSpeed, float newJumpInterval, float newJumpDistance, float newJumpDuration)
    {
        speed = newSpeed;
        jumpInterval = newJumpInterval;
        jumpDistance = newJumpDistance;
        jumpDuration = newJumpDuration;
        jumpTimer = 0f;
        jumping = false;
    }

    // Desvio lateral do pato: perpendicular ao trecho do caminho, indo e voltando em seno.
    // Distance nao muda (o pato nao anda mais nem menos), so a posicao desenhada sai do caminho.
    // Comeca em zero (seno de 0), entao nasce em cima do spawn.
    Vector3 ZigzagOffset()
    {
        if (data.zigzagAmplitude <= 0f || data.zigzagPeriod <= 0f) return Vector3.zero;

        Vector3 dir = path.PointAt(Distance + 0.1f) - path.PointAt(Distance);
        if (dir.sqrMagnitude < 1e-6f) return Vector3.zero;   // parado no fim do caminho
        Vector3 side = new Vector3(-dir.y, dir.x, 0f).normalized;
        return side * (data.zigzagAmplitude * Mathf.Sin(zigzagTime * 2f * Mathf.PI / data.zigzagPeriod));
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
    /// E o que o projetil usa para mirar a frente. Usa a velocidade media (com os saltos do
    /// gafanhoto) e de proposito NAO soma o ziguezague do pato (ZigzagOffset): a mira preditiva
    /// supoe o inimigo no centro do caminho, e por isso o pato escapa dela.
    public Vector3 PredictPosition(float seconds) =>
        path.PointAt(Distance + EnemyData.AverageSpeedOf(speed, jumpInterval, jumpDistance, jumpDuration) * seconds)
        + (Vector3)data.centerOffset;

    public void TakeDamage(float amount) => ApplyDamage(amount);

    /// Aplica dano e retorna apenas a vida realmente removida (sem contar excesso).
    public float ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return 0f;
        // O escudo do boss absorve antes da vida. O que ele absorveu conta como dano causado.
        float toHealth = boss != null ? boss.AbsorbDamage(amount) : amount;
        float actualDamage = Mathf.Min(health, toHealth);
        health -= actualDamage;
        healthBar?.Refresh();
        float dealt = (amount - toHealth) + actualDamage;
        if (health <= 0f) Die();
        else SplitOnFirstDamage();
        return dealt;
    }

    /// Recupera vida, sem passar da maxima. Usado pela fase 3 do boss.
    public void Heal(float amount)
    {
        if (IsAlive)
        {
            health = Mathf.Min(data.health, health + amount);
            healthBar?.Refresh();
        }
    }

    // AFN: no primeiro dano que nao mata, nasce uma copia no caminho, com a mesma vida que o
    // original tem agora. So divide uma vez (a copia ja nasce marcada), senao seria sem fim.
    void SplitOnFirstDamage()
    {
        if (split || !data.splitOnFirstDamage) return;
        split = true;

        Enemy copy = CreateChild(Pool.PrefabOf(gameObject), data, Mathf.Max(0f, Distance - data.splitOffset));
        copy.split = true;
        copy.health = health;
        copy.healthBar?.Refresh();
        OnSpawnedByEnemy?.Invoke(this, copy);
    }

    /// Cria um inimigo no mesmo caminho, a 'distance' do inicio (invocado do boss).
    public Enemy Spawn(GameObject prefab, EnemyData childData, float distance)
    {
        Enemy child = CreateChild(prefab, childData, distance);
        OnSpawnedByEnemy?.Invoke(this, child);
        return child;
    }

    // Prefab nulo = nao veio do Pool (teste): clona este proprio objeto.
    Enemy CreateChild(GameObject prefab, EnemyData childData, float distance)
    {
        GameObject go = prefab != null
            ? Pool.Get(prefab, transform.position, Quaternion.identity)
            : Instantiate(gameObject, transform.position, Quaternion.identity);
        Enemy child = go.GetComponent<Enemy>();
        child.data = childData;
        child.Init(path);
        child.Distance = distance;
        child.transform.position = path.PointAt(distance);
        child.UpdateOrder();
        return child;
    }

    void Die()
    {
        Finish();
        try { OnDied?.Invoke(this); }
        finally { Recycle(); }
    }

    void ReachBase()
    {
        Finish();
        try { OnReachedBase?.Invoke(this); }
        finally { Recycle(); }
    }

    // Sai da lista antes de avisar o jogo, para a torre nunca mirar num inimigo morto.
    void Finish()
    {
        IsAlive = false;
        EnemyRegistry.Unregister(this);
    }

    // Devolve ao Pool depois de avisar todo mundo (os ouvintes ainda leem data, posicao etc.).
    // Init zera o resto quando o objeto for reaproveitado.
    void Recycle()
    {
        Finished?.Invoke(this);
        Finished = null;
        Pool.Release(gameObject);
    }
}
