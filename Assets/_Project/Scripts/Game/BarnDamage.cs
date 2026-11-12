using UnityEngine;

/// Estragos no celeiro conforme a vida da base cai: telhado torto, tabua solta e janela quebrada.
/// Cada estagio e um desenho transparente do mesmo tamanho do celeiro, posto por cima (nao mexe
/// no desenho original). Fica no objeto "Celeiro" (o gerador de niveis coloca).
/// Olha a vida a cada quadro, porque o GameManager zera os proprios eventos quando e recriado.
[RequireComponent(typeof(SpriteRenderer))]
public class BarnDamage : MonoBehaviour
{
    public Sprite[] stages;                                  // estagio 1, 2 e 3 (cada um soma aos anteriores)

    // fracao da vida a partir da qual cada estagio aparece
    static readonly float[] Thresholds = { .7f, .45f, .2f };

    SpriteRenderer sr;
    SpriteRenderer[] overlays;
    int maxLife;
    int shown = -1;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        overlays = new SpriteRenderer[stages.Length];
        for (int i = 0; i < stages.Length; i++)
        {
            var go = new GameObject("Dano_" + (i + 1));
            go.transform.SetParent(transform, false);        // acompanha o tremor do celeiro
            overlays[i] = go.AddComponent<SpriteRenderer>();
            overlays[i].sprite = stages[i];
            overlays[i].sortingOrder = sr.sortingOrder + 1;
            overlays[i].enabled = false;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || overlays == null) return;
        int life = GameManager.BaseLife;
        if (life > maxLife) maxLife = life;                  // o maior valor visto e a vida do comeco
        if (maxLife <= 0) return;

        float fraction = (float)life / maxLife;
        int stage = 0;
        for (int i = 0; i < Thresholds.Length && i < overlays.Length; i++)
            if (fraction <= Thresholds[i]) stage = i + 1;
        if (stage == shown) return;

        shown = stage;
        for (int i = 0; i < overlays.Length; i++) overlays[i].enabled = i < stage;
    }
}
