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
    readonly System.Collections.Generic.List<Guardian> guardians = new System.Collections.Generic.List<Guardian>();

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
        UpdateOrder();
        EnsureGuardians();
    }

    // A torre nao se mexe: basta calcular a ordem de desenho uma vez, com a posicao final
    // (Start cobre o prefab arrastado para a cena; Init cobre a torre criada em codigo).
    void Start()
    {
        UpdateOrder();
        EnsureGuardians();
    }

    // Torre que invoca (Tufao): mantem Level + 1 guardioes vivos, um a mais por nivel.
    // Os guardioes leem dano, alcance e cadencia de Stats, entao o upgrade vale na hora.
    void EnsureGuardians()
    {
        if (data == null || data.guardian == null) return;
        while (guardians.Count < Level + 1)
        {
            Guardian g = Instantiate(data.guardian).GetComponent<Guardian>();
            g.Init(this, guardians.Count);
            guardians.Add(g);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < guardians.Count; i++)
            if (guardians[i] != null) Destroy(guardians[i].gameObject);
    }

    void UpdateOrder()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = DrawOrder.ForY(transform.position.y);
    }

    /// Sobe um nivel. O dinheiro e cobrado por quem chama (Stats do proximo nivel tem o custo).
    public bool Upgrade()
    {
        if (!CanUpgrade) return false;
        Level++;
        ApplySprite();
        EnsureGuardians();
        return true;
    }

    void Update()
    {
        if (data.guardian != null) return;   // quem ataca sao os guardioes
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindTarget();
        if (target == null) return;

        Fire(target);
        cooldown = 1f / Stats.fireRate;
    }

    /// Alvo dentro do alcance segundo o modo (respeita hitsFlying). Os guardioes tambem usam.
    public Enemy FindTarget()
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
