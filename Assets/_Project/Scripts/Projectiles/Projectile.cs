using UnityEngine;

/// Projetil com mira preditiva: a torre calcula onde o inimigo vai estar e o
/// projetil voa em linha reta ate la. Nao persegue o alvo; por isso um inimigo
/// que muda de rumo (o pato) escapa.
public class Projectile : MonoBehaviour
{
    public float speed = 8f;        // unidades de mundo por segundo
    public float hitRadius = 0.3f;  // distancia para contar como acerto

    Vector3 direction;
    float damage;
    bool hitsFlying;
    float remaining;   // distancia que ainda pode voar antes de sumir

    /// Ponto de mira calculado pela torre. Passa um pouco do ponto, para quem
    /// chegar la na hora ainda ser acertado e quem nao chegar ser errado.
    public void Init(Vector3 aimPoint, float damage, bool hitsFlying)
    {
        this.damage = damage;
        this.hitsFlying = hitsFlying;
        direction = (aimPoint - transform.position).normalized;
        remaining = Vector3.Distance(transform.position, aimPoint) + 2f;
    }

    /// Calcula o ponto onde um projetil com 'projectileSpeed' encontra o alvo.
    /// Cada volta refina o tempo de voo: voo ate a posicao prevista -> novo tempo.
    /// Converge rapido porque o projetil e mais veloz que o inimigo.
    public static Vector3 AimPoint(Vector3 origin, Enemy target, float projectileSpeed)
    {
        Vector3 aim = target.Transform.position;
        for (int i = 0; i < 5; i++)
        {
            float t = Vector3.Distance(origin, aim) / projectileSpeed;
            aim = target.PredictPosition(t);
        }
        return aim;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        remaining -= step;

        Enemy hit = FindHit();
        if (hit != null)
        {
            hit.TakeDamage(damage);   // fora do loop do registro, que muda quando o inimigo morre
            Destroy(gameObject);
        }
        else if (remaining <= 0f)
        {
            Destroy(gameObject);      // errou
        }
    }

    Enemy FindHit()
    {
        var todos = EnemyRegistry.All;
        for (int i = 0; i < todos.Count; i++)
        {
            Enemy e = todos[i];
            if (!e.IsAlive) continue;
            if (e.Flying && !hitsFlying) continue;
            if (Vector3.Distance(transform.position, e.Transform.position) <= hitRadius) return e;
        }
        return null;
    }
}
