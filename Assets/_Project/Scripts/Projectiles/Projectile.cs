using System.Collections.Generic;
using UnityEngine;

/// Projetil com mira preditiva: a torre calcula onde o inimigo vai estar e o
/// projetil voa em linha reta ate la. Nao persegue o alvo; por isso um inimigo
/// que muda de rumo (o pato) escapa.
///
/// Com splashRadius > 0 vira projetil de area (canhao): explode ao tocar um
/// inimigo ou ao chegar no ponto de mira, e machuca todos no raio.
public class Projectile : MonoBehaviour
{
    public float speed = 8f;          // unidades de mundo por segundo
    public float hitRadius = 0.3f;    // distancia para contar como acerto
    public float splashRadius = 0f;   // 0 = dano so no inimigo acertado

    // Reaproveitado entre explosoes para nao gerar lixo a cada tiro.
    static readonly List<Enemy> atingidos = new List<Enemy>();

    Vector3 direction;
    Vector3 aimPoint;
    float damage;
    bool hitsFlying;
    float aimDistance;   // distancia do disparo ate o ponto de mira
    float traveled;
    float remaining;     // distancia que ainda pode voar antes de sumir

    /// Ponto de mira calculado pela torre. Passa um pouco do ponto, para quem
    /// chegar la na hora ainda ser acertado e quem nao chegar ser errado.
    public void Init(Vector3 aimPoint, float damage, bool hitsFlying)
    {
        this.aimPoint = aimPoint;
        this.damage = damage;
        this.hitsFlying = hitsFlying;
        direction = (aimPoint - transform.position).normalized;
        aimDistance = Vector3.Distance(transform.position, aimPoint);
        remaining = aimDistance + 2f;
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
        traveled += step;
        remaining -= step;

        Enemy hit = FindHit();
        if (hit != null)
        {
            Impact(hit, transform.position);
        }
        else if (splashRadius > 0f && traveled >= aimDistance)
        {
            Impact(null, aimPoint);   // canhao explode onde mirou, mesmo sem tocar ninguem
        }
        else if (remaining <= 0f)
        {
            Destroy(gameObject);      // errou
        }
    }

    // O dano e aplicado depois de percorrer o registro, que muda quando um inimigo morre.
    void Impact(Enemy hit, Vector3 center)
    {
        if (splashRadius <= 0f)
        {
            hit.TakeDamage(damage);
        }
        else
        {
            atingidos.Clear();
            var todos = EnemyRegistry.All;
            for (int i = 0; i < todos.Count; i++)
            {
                Enemy e = todos[i];
                if (!e.IsAlive) continue;
                if (e.Flying && !hitsFlying) continue;
                if (Vector3.Distance(center, e.Transform.position) <= splashRadius) atingidos.Add(e);
            }
            for (int i = 0; i < atingidos.Count; i++) atingidos[i].TakeDamage(damage);
        }
        Destroy(gameObject);
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
