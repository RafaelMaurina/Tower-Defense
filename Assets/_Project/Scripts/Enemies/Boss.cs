using System.Collections.Generic;
using UnityEngine;

/// Prof Rieder, o boss da fase bonus. Vive junto do Enemy e segue as fases de EnemyData.phases,
/// uma por tipo da hierarquia de Chomsky:
///   Tipo 3 (regular):           anda e invoca AFD.
///   Tipo 2 (livre de contexto): ganha uma PILHA de escudos (o dano consome a camada de cima primeiro).
///   Tipo 0 (irrestrita):        salta pela "fita" (o caminho) e se cura.
/// A fase so avanca: curar-se na fase 3 nao volta para a anterior.
/// Em qualquer fase, de tempos em tempos da nota 0 em torres aleatorias (EnemyData.zero*): a torre
/// desliga, e a cada intervalo curto tem uma chance de voltar. Nao perde dinheiro nem nivel.
[RequireComponent(typeof(Enemy))]
public class Boss : MonoBehaviour
{
    Enemy enemy;
    int phase = -1;
    float summonTimer;
    int nextMinion;
    readonly List<float> shield = new List<float>();   // pilha: o fim da lista e o topo
    readonly List<Tower> zeroed = new List<Tower>();
    float zeroTimer, recoverTimer;

    public int PhaseIndex => phase;
    public int ShieldLayers => shield.Count;

    void Awake() => enemy = GetComponent<Enemy>();

    /// Chamado pelo Enemy.Init: o boss reaproveitado do Pool recomeca na fase 1.
    public void Begin()
    {
        phase = -1;
        shield.Clear();
        ReleaseAll();
        zeroTimer = recoverTimer = 0f;
        nextMinion = 0;
        EnterPhase(0);
    }

    // morreu, chegou na base ou voltou ao Pool: as torres zeradas voltam a funcionar
    void OnDisable() => ReleaseAll();

    void ReleaseAll()
    {
        for (int i = 0; i < zeroed.Count; i++)
            if (zeroed[i] != null) zeroed[i].SetZeroed(false);
        zeroed.Clear();
    }

    void UpdateZeros()
    {
        EnemyData d = enemy.data;
        if (d.zeroInterval <= 0f) return;

        recoverTimer += Time.deltaTime;
        if (recoverTimer >= d.zeroRecoverTick)
        {
            recoverTimer = 0f;
            for (int i = zeroed.Count - 1; i >= 0; i--)
            {
                if (zeroed[i] == null) { zeroed.RemoveAt(i); continue; }
                if (Random.value >= d.zeroRecoverChance) continue;
                zeroed[i].SetZeroed(false);
                zeroed.RemoveAt(i);
            }
        }

        zeroTimer += Time.deltaTime;
        if (zeroTimer < d.zeroInterval) return;
        zeroTimer = 0f;
        for (int n = 0; n < d.zeroCount; n++)
        {
            // sorteia entre as torres que ainda estao ligadas; sem nenhuma, a nota 0 nao tem alvo
            Tower pick = null;
            int seen = 0;
            for (int i = 0; i < Tower.All.Count; i++)
            {
                Tower t = Tower.All[i];
                if (t.Zeroed) continue;
                if (Random.Range(0, ++seen) == 0) pick = t;   // sorteio uniforme sem lista extra
            }
            if (pick == null) return;
            pick.SetZeroed(true, transform.position + new Vector3(0f, enemy.data.centerOffset.y, 0f));   // a caneta sai do professor
            zeroed.Add(pick);
        }
    }

    // capangas por trecho do caminho (EnemyData.minionAtPath): o professor solta o Chomsky no comeco e na metade
    void UpdateMinions()
    {
        EnemyData d = enemy.data;
        if (d.minionAtPath == null || d.minionEnemy == null || d.minionPrefab == null) return;
        float len = enemy.PathLength;
        float fraction = len > 0f ? enemy.Distance / len : 0f;
        while (nextMinion < d.minionAtPath.Length && fraction >= d.minionAtPath[nextMinion])
        {
            enemy.Spawn(d.minionPrefab, d.minionEnemy, Mathf.Max(0f, enemy.Distance - 0.5f));
            nextMinion++;
        }
    }

    void Update()
    {
        if (!enemy.IsAlive || enemy.data.phases == null || enemy.data.phases.Length == 0) return;
        BossPhase[] phases = enemy.data.phases;
        UpdateZeros();
        UpdateMinions();

        float fraction = enemy.Health / enemy.data.health;
        int target = phase;
        while (target + 1 < phases.Length && fraction <= phases[target + 1].startsBelowHealth) target++;
        if (target != phase) EnterPhase(target);

        BossPhase p = phases[phase];
        if (p.healPerSecond > 0f) enemy.Heal(p.healPerSecond * Time.deltaTime);

        if (p.summonInterval > 0f && p.summonEnemy != null && p.summonPrefab != null)
        {
            summonTimer += Time.deltaTime;
            if (summonTimer >= p.summonInterval)
            {
                summonTimer = 0f;
                enemy.Spawn(p.summonPrefab, p.summonEnemy, Mathf.Max(0f, enemy.Distance - 0.5f));
            }
        }
    }

    void EnterPhase(int index)
    {
        BossPhase[] phases = enemy.data.phases;
        if (phases == null || index < 0 || index >= phases.Length) return;

        phase = index;
        BossPhase p = phases[index];
        // Ao entrar numa fase nova (nao na primeira), o primeiro invocado nasce na hora, em vez de
        // esperar um intervalo inteiro: a fase pode acabar antes disso e nunca mostrar o seu inimigo.
        summonTimer = index > 0 ? p.summonInterval : 0f;
        enemy.SetMovement(p.speed, p.jumpInterval, p.jumpDistance, p.jumpDuration);

        shield.Clear();
        for (int i = 0; i < p.shieldLayers; i++) shield.Add(p.shieldPerLayer);

#if UNITY_EDITOR
        Debug.Log($"Boss fase {index + 1}/{phases.Length}: {p.name} (escudo: {p.shieldLayers} camadas)", this);
#endif
    }

    /// Escudo em pilha: o dano consome a camada do topo; o que sobra passa para a de baixo e depois
    /// para a vida. Devolve o dano que chegou a vida.
    public float AbsorbDamage(float amount)
    {
        while (amount > 0f && shield.Count > 0)
        {
            int top = shield.Count - 1;
            float taken = Mathf.Min(shield[top], amount);
            shield[top] -= taken;
            amount -= taken;
            if (shield[top] <= 0f) shield.RemoveAt(top);
        }
        return amount;
    }
}
