using UnityEngine;

/// Canhao de abobora de nivel 1 virando para o alvo: troca o sprite pelo da direcao do inimigo
/// (TowerLevel.facing do nivel atual, 8 setores). So troca o quadro inteiro, sem girar nada.
/// Ligado pelo TowerAmbientFx sempre que a torre recebe o sprite de um nivel (vale para os 3).
[RequireComponent(typeof(SpriteRenderer))]
public class CannonAim : MonoBehaviour
{
    const float Interval = .1f;          // de quanto em quanto tempo procura o alvo

    Tower tower;
    SpriteRenderer sr;
    Sprite[] facing;
    float timer;
    int sector = 4;                      // comeca olhando para o oeste, como o desenho original

    /// Para onde o cano aponta agora, na tela (para o recuo e o clarao).
    public Vector3 Facing { get; private set; } = Vector3.left;

    public bool Begin(Tower owner, Sprite[] sprites)
    {
        tower = owner;
        sr = GetComponent<SpriteRenderer>();
        facing = sprites;
        if (facing == null || facing.Length != 8) { enabled = false; return false; }
        enabled = true;
        Show(sector);
        return true;
    }

    void Update()
    {
        if (tower == null || facing == null) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Interval;
        Enemy target = tower.FindTarget();
        if (target == null) return;
        Vector3 d = target.Transform.position - transform.position;
        if (d.sqrMagnitude < 1e-6f) return;
        // angulo no sentido horario a partir do leste (y da tela cresce para cima)
        float ang = Mathf.Atan2(-d.y, d.x) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;
        int s = Mathf.RoundToInt(ang / 45f) % 8;
        if (s != sector) Show(s);
    }

    void Show(int s)
    {
        sector = s;
        if (facing[s] != null) sr.sprite = facing[s];
        float rad = -s * 45f * Mathf.Deg2Rad;
        Facing = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}
