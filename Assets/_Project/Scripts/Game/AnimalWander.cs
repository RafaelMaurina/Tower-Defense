using UnityEngine;

/// Bicho de pasto (vaca...) passeando: fica parado e, a cada 3, 5 ou 10 segundos (sorteado), ou anda ate
/// um ponto perto de casa ou pasta um pouco. Anda em pixel inteiro, trocando os quadros de walkFrames.
/// O quadro parado e o sprite que o SpriteRenderer ja tem.
[RequireComponent(typeof(SpriteRenderer))]
public class AnimalWander : MonoBehaviour
{
    public Sprite[] walkFrames;          // ciclo de andar (4 quadros na vaca)
    public Sprite grazeFrame;            // cabeca baixa, focinho no chao (opcional)
    public Sprite chewFrame;             // mesma pose com o focinho 1 pixel acima: mastigada (opcional)
    public bool facesLeft;               // para que lado o desenho olha
    public float radius = .9f;           // ate onde se afasta do ponto inicial, em casas
    public float speed = .35f;           // casas por segundo (vaca anda devagar)
    public float frameTime = .26f;       // tempo de cada quadro do andar

    static readonly float[] Waits = { 3f, 5f, 10f };

    enum State { Idle, Walk, Graze }
    State state;
    SpriteRenderer sr;
    Sprite idleFrame;
    Vector3 home, position, goal;
    float timer, clock, grazeTime;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        idleFrame = sr.sprite;
        home = position = transform.position;
        timer = Waits[Random.Range(0, Waits.Length)] * Random.Range(.3f, 1f);   // nao comecam juntos
    }

    void Update()
    {
        clock += Time.deltaTime;
        switch (state)
        {
            case State.Idle:
                timer -= Time.deltaTime;
                if (timer > 0f) break;
                bool canWalk = walkFrames != null && walkFrames.Length > 0;
                bool canGraze = grazeFrame != null;
                if (canWalk && (!canGraze || Random.value < .6f)) StartWalk();
                else if (canGraze) StartGraze();
                else timer = Waits[Random.Range(0, Waits.Length)];
                break;

            case State.Walk:
                position = Vector3.MoveTowards(position, goal, speed * Time.deltaTime);
                transform.position = PixelSprites.Snap(position);
                sr.sortingOrder = DrawOrder.ForY(transform.position.y);
                sr.sprite = walkFrames[(int)(clock / frameTime) % walkFrames.Length];
                if ((goal - position).sqrMagnitude < 1e-6f)
                {
                    if (grazeFrame != null && Random.value < .5f) StartGraze(); else Rest();   // chegou: as vezes pasta
                }
                break;

            case State.Graze:
                bool chew = chewFrame != null && (int)(clock / .35f) % 2 == 1;
                sr.sprite = chew ? chewFrame : grazeFrame;
                if (clock >= grazeTime) Rest();
                break;
        }
    }

    void StartWalk()
    {
        Vector2 r = Random.insideUnitCircle * radius;
        if (r.sqrMagnitude < .09f)                                       // caminhada minima: 0,3 casa
            r = (r.sqrMagnitude < 1e-6f ? Vector2.right : r.normalized) * .3f;
        goal = home + new Vector3(r.x, r.y * .5f, 0f);
        goal.x = Mathf.Clamp(goal.x, .6f, 29.4f);   // nunca passa da cerca nem da borda
        goal.y = Mathf.Clamp(goal.y, 1.15f, 15.3f);
        if (Mathf.Abs(goal.x - position.x) > .01f)
            sr.flipX = (goal.x < position.x) != facesLeft;
        state = State.Walk;
        clock = 0f;
    }

    void StartGraze()
    {
        grazeTime = Random.Range(1f, 2f);
        state = State.Graze;
        clock = 0f;
    }

    void Rest()
    {
        sr.sprite = idleFrame;
        state = State.Idle;
        timer = Waits[Random.Range(0, Waits.Length)];
    }
}
