using System.Collections.Generic;
using UnityEngine;

/// A horta paga o preco de cada inimigo que chega ao celeiro: um canteiro perde a planta (sorteado) e
/// solta uma nuvem de poeira. Cenoura: o canteiro de 32x32 troca para a terra vazia. Trigo e abobora:
/// a planta que esta por cima some. O gerador de niveis cadastra os canteiros.
public class HortaMurcha : MonoBehaviour
{
    public class Canteiro
    {
        public SpriteRenderer bed;       // cenoura: o proprio canteiro, que troca de desenho
        public GameObject plant;         // trigo e abobora: a planta por cima
        public Sprite empty;             // terra vazia (cenoura)
        public bool eaten;
    }

    readonly List<Canteiro> plots = new List<Canteiro>();

    public void Add(Canteiro plot) => plots.Add(plot);

    // inscreve por objeto: cada fase nova cria uma horta nova
    void OnEnable() => Enemy.OnReachedBase += HandleReached;
    void OnDisable() => Enemy.OnReachedBase -= HandleReached;

    void HandleReached(Enemy enemy)
    {
        var alive = new List<Canteiro>();
        foreach (var p in plots) if (!p.eaten) alive.Add(p);
        if (alive.Count == 0) return;

        Canteiro chosen = alive[Random.Range(0, alive.Count)];
        chosen.eaten = true;
        Vector3 where;
        if (chosen.plant != null)
        {
            where = chosen.plant.transform.position;
            chosen.plant.SetActive(false);
        }
        else
        {
            where = chosen.bed.transform.position;
            var sway = chosen.bed.GetComponent<PixelSway>();
            if (sway != null) sway.enabled = false;
            chosen.bed.sprite = chosen.empty;
        }
        GroundFx.Puff(where + new Vector3(0f, -.2f, 0f), DrawOrder.Projectile);
    }
}
