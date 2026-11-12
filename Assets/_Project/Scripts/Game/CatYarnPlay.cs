using UnityEngine;

/// Gato brincando com um novelo de la. Fica no objeto do gato; o novelo e outro SpriteRenderer.
/// Cena: o gato senta, da uns tapas, o novelo rola alguns pixels, o gato agacha, pula, senta
/// (as vezes passa por cima e cai do outro lado, as vezes rola de costas com o novelo) e repete.
/// Tudo em pixel inteiro, sem Animator e sem fisica. Os desenhos olham para a esquerda (facesLeft).
[RequireComponent(typeof(SpriteRenderer))]
public class CatYarnPlay : MonoBehaviour
{
    public Sprite sit;                   // sentado
    public Sprite crouch;                // agachado para o bote
    public Sprite crouch2;               // agachado com o traseiro 1 pixel acima (opcional)
    public Sprite pounce;                // esticado no ar
    public Sprite pat1;                  // pata recolhida
    public Sprite pat2;                  // pata esticada para a frente
    public Sprite lie;                   // de costas com o novelo entre as patas (opcional)
    public Sprite[] walkFrames;          // andar: parado e passo (2 quadros)
    public SpriteRenderer yarn;          // o novelo
    public Sprite[] yarnFrames;          // 2 quadros de rolar
    public bool facesLeft;               // para que lado os desenhos olham
    public float radius = .8f;           // ate onde o novelo vai a partir do ponto inicial do gato, em casas
    public float frameTime = .14f;       // tempo de cada quadro do andar

    const int Gap = 14;                  // pixels entre o centro do gato sentado e o do novelo
    const int MinRoll = 6, MaxRoll = 14; // quanto o novelo rola por tapa, em pixels
    const int LieShift = 2;              // o novelo desenhado em "lie" fica 2 px a frente do pivo
    const float WalkStep = .07f;         // segundos por pixel andado
    static readonly float[] PatTimes = { .28f, .16f, .28f, .16f };

    enum State { Sit, Pat, Watch, Crouch, Pounce, Walk, Play }
    State state;
    SpriteRenderer sr;
    Vector3 home, yarnHome;              // chao do gato e do novelo (em pixel inteiro)
    int catX, hopY, yarnX, radiusPx;     // x relativo a home, em pixels
    float timer, clock, px;

    // novelo rolando
    bool rolling, yarnFlip;
    int rollDir, rollLeft, rollTotal;
    float rollTimer;
    Sprite yarnSprite;

    // bote
    int hopFrom, hopTo, hopHeight, nextDir;
    float hopTime;
    bool playAfterHop;

    // passeio
    int walkGoal, patPhase;
    float walkTimer;
    bool awayPending, justWalked;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        px = 1f / sr.sprite.pixelsPerUnit;   // 1 pixel do desenho do gato (ele pode usar PPU diferente de 32)
        home = PixelSprites.Snap(transform.position);
        radiusPx = Mathf.Max(1, Mathf.RoundToInt(radius / px));
        if (yarn != null)
        {
            yarnHome = PixelSprites.Snap(yarn.transform.position);
            yarnX = Mathf.RoundToInt((yarnHome.x - home.x) / px);
            if (yarnFrames != null && yarnFrames.Length > 0) yarnSprite = yarnFrames[0];
        }
        StartSit(Random.Range(.3f, 2f));
        Apply();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        clock += dt;
        timer -= dt;
        UpdateYarn(dt);

        switch (state)
        {
            case State.Sit:
                if (timer <= 0f) EndSit();
                break;

            case State.Pat:
                if (timer > 0f) break;
                patPhase++;
                if (patPhase >= PatTimes.Length) { Show(sit); StartWatch(); break; }
                Show(patPhase % 2 == 0 ? pat1 : pat2);
                timer = PatTimes[patPhase];
                if (patPhase == PatTimes.Length - 1) BeginRoll();       // ultimo tapa empurra o novelo
                break;

            case State.Watch:                                            // espera o novelo parar
                if (rolling) timer = .45f;
                else if (timer <= 0f) StartCrouch();
                break;

            case State.Crouch:
                bool wiggle = crouch2 != null && (int)(clock / .1f) % 2 == 1;
                Show(wiggle ? crouch2 : crouch);
                if (timer <= 0f) StartPounce();
                break;

            case State.Pounce:
                hopTime = Mathf.Max(hopTime, .01f);
                float t = Mathf.Clamp01(clock / hopTime);
                catX = Mathf.RoundToInt(Mathf.Lerp(hopFrom, hopTo, t));
                hopY = Mathf.RoundToInt(4f * hopHeight * t * (1f - t));
                if (t >= 1f) Land();
                break;

            case State.Walk:
                Show(walkFrames[(int)(clock / frameTime) % walkFrames.Length]);
                walkTimer -= dt;
                if (walkTimer > 0f) break;
                walkTimer = WalkStep;
                catX += walkGoal > catX ? 1 : -1;
                if (catX == walkGoal) StartSit(awayPending ? Random.Range(1f, 2f) : Random.Range(.4f, .8f));
                break;

            case State.Play:                                             // de costas, balancando de leve
                hopY = (int)(clock / .25f) % 2;
                if (timer <= 0f) EndPlay();
                break;
        }
        Apply();
    }

    // ---------------------------------------------------------------- estados

    void StartSit(float time)
    {
        state = State.Sit;
        Show(sit);
        FaceYarn();
        hopY = 0;
        timer = time;
    }

    void EndSit()
    {
        if (awayPending)                                                 // passeou: volta para perto do novelo
        {
            awayPending = false;
            justWalked = true;
            StartWalk(SideX());
            return;
        }
        if (!justWalked && walkFrames != null && walkFrames.Length > 0 && Random.value < .3f)
        {
            awayPending = true;
            int away = catX >= yarnX ? 1 : -1;
            StartWalk(catX + away * Random.Range(6, 13));
            return;
        }
        justWalked = false;
        state = State.Pat;
        patPhase = 0;
        Show(pat1);
        timer = PatTimes[0];
    }

    void StartWalk(int goal)
    {
        walkGoal = goal;
        walkTimer = 0f;
        state = State.Walk;
        clock = 0f;
        if (goal != catX) Face(goal > catX ? 1 : -1);
    }

    void StartWatch()
    {
        state = State.Watch;
        timer = .45f;
    }

    void StartCrouch()
    {
        state = State.Crouch;
        clock = 0f;
        timer = .4f;
        Show(crouch);
    }

    void StartPounce()
    {
        nextDir = PickRollDir();
        bool onTop = lie != null && Random.value < .25f;                 // cai em cima do novelo e rola de costas
        int landX = onTop ? yarnX : yarnX - nextDir * Gap;               // senta no lado de onde vai empurrar
        int dist = Mathf.Abs(landX - catX);
        StartHop(landX, Mathf.Clamp(dist / 3, 4, 9), .3f + dist * .008f, onTop);
    }

    void StartHop(int toX, int height, float time, bool thenPlay)
    {
        state = State.Pounce;
        hopFrom = catX;
        hopTo = toX;
        hopHeight = height;
        hopTime = time;
        playAfterHop = thenPlay;
        clock = 0f;
        Show(pounce);
        if (toX != catX) Face(toX > catX ? 1 : -1);                      // olha para onde pula
    }

    void Land()
    {
        catX = hopTo;
        hopY = 0;
        if (playAfterHop) StartPlay(); else StartSit(Random.Range(1f, 3f));
    }

    void StartPlay()
    {
        state = State.Play;
        clock = 0f;
        timer = Random.Range(1.5f, 2.5f);
        Show(lie);
        catX = yarnX - (sr.flipX ? -LieShift : LieShift);                // o novelo desenhado cai sobre o de verdade
        if (yarn != null) yarn.enabled = false;
    }

    void EndPlay()
    {
        if (yarn != null) yarn.enabled = true;
        hopY = 0;
        StartHop(yarnX - nextDir * Gap, 3, .35f, false);                 // pula para o lado
    }

    // ---------------------------------------------------------------- novelo

    /// Lado para onde o proximo tapa empurra o novelo: so onde cabem pelo menos MinRoll pixels dentro do raio.
    int PickRollDir()
    {
        bool okLeft = yarnX + radiusPx >= MinRoll;
        bool okRight = radiusPx - yarnX >= MinRoll;
        if (okLeft && okRight) return Random.value < .5f ? -1 : 1;
        if (okLeft) return -1;
        if (okRight) return 1;
        return yarnX + radiusPx > radiusPx - yarnX ? -1 : 1;
    }

    void BeginRoll()
    {
        rollDir = yarnX >= catX ? 1 : -1;                                // o gato empurra para longe de si
        int room = rollDir > 0 ? radiusPx - yarnX : radiusPx + yarnX;    // o novelo nunca sai do raio
        rollTotal = rollLeft = Mathf.Min(Random.Range(MinRoll, MaxRoll + 1), Mathf.Max(room, 0));
        rolling = rollLeft > 0;
        rollTimer = 0f;
        yarnFlip = rollDir > 0;                                          // o fio solto fica atras
    }

    void UpdateYarn(float dt)
    {
        if (!rolling) return;
        rollTimer -= dt;
        if (rollTimer > 0f) return;
        yarnX += rollDir;
        rollLeft--;
        int done = rollTotal - rollLeft;
        if (yarnFrames != null && yarnFrames.Length > 0) yarnSprite = yarnFrames[(done / 2) % yarnFrames.Length];
        float f = done / (float)rollTotal;
        rollTimer = .04f + .18f * f * f;                                 // desacelera
        if (rollLeft <= 0) rolling = false;
    }

    // ---------------------------------------------------------------- apoio

    int SideX() => yarnX + (catX >= yarnX ? 1 : -1) * Gap;

    void FaceYarn() { Face(yarnX >= catX ? 1 : -1); }

    void Face(int dir) { sr.flipX = (dir > 0) == facesLeft; }

    void Show(Sprite s) { if (s != null) sr.sprite = s; }

    void Apply()
    {
        transform.position = home + new Vector3(catX * px, hopY * px, 0f);
        sr.sortingOrder = DrawOrder.ForY(home.y) + 1;                    // por cima do novelo
        if (yarn == null) return;
        yarn.transform.position = new Vector3(home.x + yarnX * px, yarnHome.y, 0f);
        yarn.sortingOrder = DrawOrder.ForY(yarnHome.y);
        yarn.flipX = yarnFlip;
        if (yarnSprite != null) yarn.sprite = yarnSprite;
    }
}
