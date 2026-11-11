using UnityEngine;

/// Galinha e galo ciscando: ficam parados e, a cada 3, 5 ou 10 segundos (sorteado), ou andam ate
/// um ponto perto de casa ou dao umas bicadas no chao. Anda em pixel inteiro.
[RequireComponent(typeof(SpriteRenderer))]
public class ChickenWander : MonoBehaviour
{
    public Sprite stepFrame;             // corpo 1 pixel acima, pernas esticadas
    public Sprite peckFrame;             // cabeca no chao
    public bool facesLeft;               // para que lado o desenho olha
    public float radius = .9f;           // ate onde se afasta do ponto inicial, em casas
    public float speed = .8f;            // casas por segundo
    public float frameTime = .16f;

    static readonly float[] Waits = { 3f, 5f, 10f };

    enum State { Idle, Walk, Peck }
    State state;
    SpriteRenderer sr;
    Sprite idleFrame;
    Vector3 home, position, goal;
    float timer, clock;
    int pecks;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        idleFrame = sr.sprite;
        home = position = transform.position;
        timer = Waits[Random.Range(0, Waits.Length)] * Random.Range(.3f, 1f);   // nao comecam juntas
    }

    void Update()
    {
        clock += Time.deltaTime;
        switch (state)
        {
            case State.Idle:
                timer -= Time.deltaTime;
                if (timer > 0f) break;
                if (Random.value < .5f && stepFrame != null) StartWalk(); else StartPeck();
                break;

            case State.Walk:
                position = Vector3.MoveTowards(position, goal, speed * Time.deltaTime);
                transform.position = PixelSprites.Snap(position);
                sr.sortingOrder = DrawOrder.ForY(transform.position.y);
                sr.sprite = (int)(clock / frameTime) % 2 == 0 ? stepFrame : idleFrame;
                if ((goal - position).sqrMagnitude < 1e-6f)
                {
                    if (Random.value < .6f) StartPeck(); else Rest();               // chegou: costuma bicar
                }
                break;

            case State.Peck:
                float beat = frameTime * 1.3f;
                bool down = (int)(clock / beat) % 2 == 0;
                sr.sprite = down && peckFrame != null ? peckFrame : idleFrame;
                if (clock >= pecks * 2 * beat) Rest();
                break;
        }
    }

    void StartWalk()
    {
        Vector2 r = Random.insideUnitCircle * radius;
        goal = home + new Vector3(r.x, r.y * .5f, 0f);
        sr.flipX = (goal.x < position.x) != facesLeft;
        state = State.Walk;
        clock = 0f;
    }

    void StartPeck()
    {
        pecks = Random.Range(2, 5);
        state = State.Peck;
        clock = 0f;
    }

    void Rest()
    {
        sr.sprite = idleFrame;
        state = State.Idle;
        timer = Waits[Random.Range(0, Waits.Length)];
    }
}
