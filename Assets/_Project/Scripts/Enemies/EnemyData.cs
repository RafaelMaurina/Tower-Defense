using UnityEngine;

/// Dados de um tipo de inimigo. Balanceamento mora aqui, nunca no codigo.
[CreateAssetMenu(menuName = "Defesa da Colheita/Enemy Data", fileName = "Enemy_")]
public class EnemyData : ScriptableObject
{
    public string displayName;
    public float health = 10f;
    public float speed = 1f;        // unidades de mundo por segundo, ao longo do caminho
    public int reward = 5;          // dinheiro ao morrer
    public int baseDamage = 1;      // vida que tira da base ao chegar
    public bool flying;             // so torres que atingem aereos podem mira-lo
    public Sprite sprite;
}
