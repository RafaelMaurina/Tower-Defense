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
    public Vector2 centerOffset = new Vector2(0f, 0.35f);   // do pivo (pe) ate o centro do corpo, em unidades; e para la que a torre mira
    public Sprite[] frames;                 // quadros da animacao de andar, em ordem
    public float framesPerSecond = 6f;
}
