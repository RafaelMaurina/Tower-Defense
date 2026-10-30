using UnityEngine;

/// Qualquer coisa que leva dano (inimigo, boss). Torres e projeteis
/// falam com esta interface, nunca com um tipo concreto de inimigo.
public interface IDamageable
{
    void TakeDamage(float amount);
    bool IsAlive { get; }
    Transform Transform { get; }
}
