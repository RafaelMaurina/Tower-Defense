using UnityEngine;

/// Guardiao invocado por uma torre como o Tufao (o pastor alemao): corpo a corpo.
/// Corre ate o inimigo escolhido pela torre (dentro do alcance dela), morde em contato
/// e volta para perto da casinha quando nao ha alvo. Nao tem vida: os inimigos so andam.
/// Dano, alcance e cadencia (mordidas por segundo) vem de Tower.Stats, entao o upgrade vale na hora.
public class Guardian : MonoBehaviour
{
    public float speed = 4f;          // unidades de mundo por segundo
    public float biteRange = 0.5f;    // distancia dos pes do cao aos pes do inimigo para morder

    [Header("Animacao (troca de quadro, sem Animator)")]
    public Sprite[] idleFrames;       // parado: respira, abana o rabo, mexe a orelha
    public Sprite[] runFrames;        // ciclo de corrida
    public Sprite[] biteFrames;       // [0] abre a boca, [1] fecha: o dano sai junto com o [1]
    public float idleFrameTime = .22f;
    public float runFrameTime = .09f;
    public float biteFrameTime = .08f;

    /// Avisa que a mordida acertou: (cao, posicao do inimigo). Usado pelo BiteFx.
    public static event System.Action<Guardian, Vector3> OnBite;

    /// Todos os caes em campo (usado pelo GroundFx para a poeira da corrida).
    public static readonly System.Collections.Generic.List<Guardian> All = new System.Collections.Generic.List<Guardian>();

    Tower tower;
    Vector3 home;
    Vector3 chaseOffset;              // cada cao morde de um lado do inimigo
    Enemy target;
    float cooldown;
    SpriteRenderer sr;

    float animTime;
    bool running;
    float biteTime = -1f;             // < 0: nao esta mordendo
    bool biteLanded;
    Enemy biteTarget;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// 'index' separa os caes parados ao lado da casinha.
    public void Init(Tower owner, int index)
    {
        tower = owner;
        // um de cada lado da porta e o terceiro na frente, para um nao ficar em cima do outro
        Vector3[] spots = { new Vector3(-0.9f, -0.6f, 0f), new Vector3(0.85f, -0.6f, 0f), new Vector3(0f, -1.2f, 0f) };
        home = tower.transform.position + spots[index % spots.Length];
        chaseOffset = new Vector3((index - 1) * 0.35f, 0f, 0f);
        transform.position = home;
        animTime = index * 0.37f;     // caes lado a lado nao abanam o rabo juntos
        UpdateOrder();
    }

    void Update()
    {
        if (tower == null) { Destroy(gameObject); return; }

        // Mantem o alvo enquanto ele vive e esta no alcance; senao pede outro para a torre.
        if (target == null || !target.IsAlive ||
            Vector3.Distance(tower.transform.position, target.Transform.position) > tower.Stats.range)
            target = tower.FindTarget();

        Vector3 goal = target != null ? target.Transform.position + chaseOffset : home;
        Vector3 to = goal - transform.position;
        if (sr != null && Mathf.Abs(to.x) > 0.01f) sr.flipX = to.x < 0f;   // sprites olham para a direita
        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
        bool moved = (transform.position - before).sqrMagnitude > 1e-8f;
        UpdateOrder();

        cooldown -= Time.deltaTime;
        if (biteTime < 0f && target != null && cooldown <= 0f &&
            Vector3.Distance(transform.position, target.Transform.position) <= biteRange)
        {
            // comeca a mordida; o dano sai quando a boca fecha
            biteTime = 0f;
            biteLanded = false;
            biteTarget = target;
            cooldown = 1f / tower.Stats.fireRate;
            if (biteFrames == null || biteFrames.Length < 2) LandBite();
        }

        Animate(moved);
    }

    void LandBite()
    {
        biteLanded = true;
        if (biteTarget == null || !biteTarget.IsAlive) return;
        Vector3 where = biteTarget.Center;   // antes do dano: o inimigo pode morrer e sair do jogo
        float damage = biteTarget.ApplyDamage(tower.Stats.damage);
        if (damage > 0f && GameManager.Instance != null)
            GameManager.Instance.RegisterTowerDamage(tower, damage);
        if (damage > 0f) OnBite?.Invoke(this, where);
    }

    void Animate(bool moved)
    {
        if (sr == null) return;

        if (biteTime >= 0f)
        {
            biteTime += Time.deltaTime;
            bool closed = biteTime >= biteFrameTime;
            if (closed && !biteLanded) LandBite();
            if (biteTime >= biteFrameTime * 3f) biteTime = -1f;        // boca fechada dura 2 quadros
            else if (biteFrames != null && biteFrames.Length >= 2) { sr.sprite = biteFrames[closed ? 1 : 0]; return; }
            else return;
        }

        if (moved != running) { running = moved; animTime = 0f; }
        Sprite[] frames = running ? runFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;
        animTime += Time.deltaTime;
        float step = running ? runFrameTime : idleFrameTime;
        sr.sprite = frames[(int)(animTime / step) % frames.Length];
    }

    void UpdateOrder()
    {
        if (sr != null) sr.sortingOrder = DrawOrder.ForY(transform.position.y);
    }
}
