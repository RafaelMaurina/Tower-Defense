using UnityEngine;

/// Como a torre escolhe entre varios inimigos no alcance.
public enum TargetMode { First, Strongest, Closest }

/// Torre base: procura inimigos no alcance (via EnemyRegistry), escolhe um alvo
/// e dispara o projetil na cadencia do nivel atual. As torres concretas sao
/// so um TowerData diferente.
public class Tower : MonoBehaviour
{
    public TowerData data;
    public TargetMode mode = TargetMode.First;

    float cooldown;

    /// Nivel atual, de 0 a data.levels.Length - 1.
    public int Level { get; private set; }
    public TowerLevel Stats => data.levels[Level];
    public bool CanUpgrade => Level < data.levels.Length - 1;

    /// O Joao chama ao construir no canteiro.
    public void Init(TowerData towerData)
    {
        data = towerData;
        Level = 0;
        cooldown = 0f;
        ApplySprite();
    }

    /// Sobe um nivel. O dinheiro e cobrado por quem chama (Stats do proximo nivel tem o custo).
    public bool Upgrade()
    {
        if (!CanUpgrade) return false;
        Level++;
        ApplySprite();
        return true;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindTarget();
        if (target == null) return;

        Fire(target);
        cooldown = 1f / Stats.fireRate;
    }

    Enemy FindTarget()
    {
        Enemy best = null;
        float bestScore = float.MinValue;
        var todos = EnemyRegistry.All;

        for (int i = 0; i < todos.Count; i++)
        {
            Enemy e = todos[i];
            if (!e.IsAlive) continue;                        // acabou de ser instanciado ou morreu
            if (e.Flying && !data.hitsFlying) continue;

            float dist = Vector3.Distance(transform.position, e.Transform.position);
            if (dist > Stats.range) continue;

            // Maior pontuacao vence.
            float score = mode switch
            {
                TargetMode.First => e.Distance,     // mais perto da base
                TargetMode.Strongest => e.Health,
                _ => -dist,                         // Closest
            };
            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best;
    }

    void Fire(Enemy target)
    {
        GameObject go = Instantiate(data.projectile, transform.position, Quaternion.identity);
        Projectile p = go.GetComponent<Projectile>();
        Vector3 aim = Projectile.AimPoint(transform.position, target, p.speed);
        p.Init(aim, Stats.damage, data.hitsFlying);
    }

    void ApplySprite()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && Stats.sprite != null) sr.sprite = Stats.sprite;
    }

    // Mostra o alcance na Scene view ao selecionar a torre.
    void OnDrawGizmosSelected()
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return;
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, Stats.range);
    }
}
