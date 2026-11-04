using UnityEngine;

/// Guardiao invocado por uma torre como o Tufao (o pastor alemao): corpo a corpo.
/// Corre ate o inimigo escolhido pela torre (dentro do alcance dela), morde em contato
/// e volta para perto da casinha quando nao ha alvo. Nao tem vida: os inimigos so andam.
/// Dano, alcance e cadencia (mordidas por segundo) vem de Tower.Stats, entao o upgrade vale na hora.
public class Guardian : MonoBehaviour
{
    public float speed = 4f;          // unidades de mundo por segundo
    public float biteRange = 0.5f;    // distancia dos pes do cao aos pes do inimigo para morder

    Tower tower;
    Vector3 home;
    Enemy target;
    float cooldown;
    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    /// 'index' separa os caes parados ao lado da casinha.
    public void Init(Tower owner, int index)
    {
        tower = owner;
        home = tower.transform.position + new Vector3((index - 1) * 0.5f, -0.6f, 0f);
        transform.position = home;
        UpdateOrder();
    }

    void Update()
    {
        if (tower == null) { Destroy(gameObject); return; }

        // Mantem o alvo enquanto ele vive e esta no alcance; senao pede outro para a torre.
        if (target == null || !target.IsAlive ||
            Vector3.Distance(tower.transform.position, target.Transform.position) > tower.Stats.range)
            target = tower.FindTarget();

        Vector3 goal = target != null ? target.Transform.position : home;
        Vector3 to = goal - transform.position;
        if (sr != null && Mathf.Abs(to.x) > 0.01f) sr.flipX = to.x < 0f;   // sprites olham para a direita
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
        UpdateOrder();

        cooldown -= Time.deltaTime;
        if (target != null && cooldown <= 0f &&
            Vector3.Distance(transform.position, target.Transform.position) <= biteRange)
        {
            target.TakeDamage(tower.Stats.damage);
            cooldown = 1f / tower.Stats.fireRate;
        }
    }

    void UpdateOrder()
    {
        if (sr != null) sr.sortingOrder = DrawOrder.ForY(transform.position.y);
    }
}
