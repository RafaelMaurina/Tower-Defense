using System;
using UnityEngine;

/// Uma fase do boss (ver Boss.cs). A fase vale enquanto a vida estiver abaixo de startsBelowHealth
/// (fracao da vida maxima) e acima da proxima fase. A primeira fase deve usar 1.
[Serializable]
public class BossPhase
{
    public string name;
    [Range(0f, 1f)] public float startsBelowHealth = 1f;
    public float speed = 1f;
    [Header("Salto. jumpDistance = 0 desliga")]
    public float jumpInterval;
    public float jumpDistance;
    public float jumpDuration = 0.3f;
    [Header("Invocacao. summonInterval = 0 desliga")]
    public float summonInterval;        // segundos entre uma invocacao e outra
    public EnemyData summonEnemy;
    public GameObject summonPrefab;
    [Header("Escudo em pilha (Tipo 2). shieldLayers = 0 desliga")]
    public int shieldLayers;            // camadas empilhadas ao entrar na fase
    public float shieldPerLayer;        // dano que cada camada absorve; o dano consome a de cima primeiro
    [Header("Cura")]
    public float healPerSecond;
}

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
    [Header("Salto (gafanhoto). jumpDistance = 0 desliga")]
    public float jumpInterval;      // segundos andando entre um salto e outro
    public float jumpDistance;      // quanto o salto avanca no caminho, em unidades
    public float jumpDuration = 0.3f;   // segundos que o salto leva (avanca jumpDistance nesse tempo)
    [Header("Ziguezague (pato). zigzagAmplitude = 0 desliga")]
    public float zigzagAmplitude;   // desvio lateral maximo para cada lado do caminho, em unidades
    public float zigzagPeriod = 1.6f;   // segundos para ir e voltar (um ciclo completo)
    [Header("Divisao (AFN). splitOnFirstDamage = false desliga")]
    public bool splitOnFirstDamage;     // no primeiro dano que nao mata, nasce uma copia com a mesma vida
    public float splitOffset;           // quanto a copia nasce atras do original, em unidades (0 = no mesmo ponto)
    [Header("Boss (Chomsky). Sem fases = inimigo comum")]
    public BossPhase[] phases;
    public Sprite[] frames;                 // quadros da animacao de andar, em ordem
    public float framesPerSecond = 6f;

    /// Velocidade media no caminho, contando os saltos. E a que a torre usa para mirar:
    /// o tiro acerta quem esta no ritmo medio e erra quando o salto cai fora dele.
    public float AverageSpeed => AverageSpeedOf(speed, jumpInterval, jumpDistance, jumpDuration);

    /// Mesma conta para quem muda de velocidade em jogo (as fases do boss).
    public static float AverageSpeedOf(float speed, float jumpInterval, float jumpDistance, float jumpDuration)
    {
        if (jumpDistance <= 0f || jumpInterval <= 0f) return speed;
        float d = Mathf.Max(0.05f, jumpDuration);
        return (speed * jumpInterval + jumpDistance) / (jumpInterval + d);
    }
}
