using System.Collections.Generic;
using UnityEngine;

/// Chomsky, o boss da fase bonus. Vive junto do Enemy e segue as fases de EnemyData.phases,
/// uma por tipo da hierarquia de Chomsky:
///   Tipo 3 (regular):           anda e invoca AFD.
///   Tipo 2 (livre de contexto): ganha uma PILHA de escudos (o dano consome a camada de cima primeiro).
///   Tipo 0 (irrestrita):        salta pela "fita" (o caminho) e se cura.
/// A fase so avanca: curar-se na fase 3 nao volta para a anterior.
[RequireComponent(typeof(Enemy))]
public class Boss : MonoBehaviour
{
    Enemy enemy;
    int phase = -1;
    float summonTimer;
    readonly List<float> shield = new List<float>();   // pilha: o fim da lista e o topo

    public int PhaseIndex => phase;
    public int ShieldLayers => shield.Count;

    void Awake() => enemy = GetComponent<Enemy>();

    /// Chamado pelo Enemy.Init: o boss reaproveitado do Pool recomeca na fase 1.
    public void Begin()
    {
        phase = -1;
        shield.Clear();
        EnterPhase(0);
    }

    void Update()
    {
        if (!enemy.IsAlive || enemy.data.phases == null || enemy.data.phases.Length == 0) return;
        BossPhase[] phases = enemy.data.phases;

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
