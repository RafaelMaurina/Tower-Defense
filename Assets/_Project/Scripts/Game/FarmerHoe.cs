using UnityEngine;

/// Fazendeiro capinando: troca de quadro em loop (antecipa, enxada no alto, desce, bate, puxa a terra, volta), sem rotacao.
/// A cada 3 a 6 batidas ele descansa um instante com a enxada parada.
[RequireComponent(typeof(SpriteRenderer))]
public class FarmerHoe : MonoBehaviour
{
    // quadro 0 = parado; 1..6 = antecipa, topo, desce, bate, puxa, volta
    public Sprite[] frames;

    static readonly float[] Times = { .3f, .12f, .16f, .05f, .14f, .12f, .10f };

    SpriteRenderer sr;
    int step;
    int cyclesLeft;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cyclesLeft = Random.Range(3, 7);
        step = Random.Range(0, Times.Length);
        timer = Random.value * .3f;
    }

    void Update()
    {
        if (frames == null || frames.Length < Times.Length) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        step = (step + 1) % Times.Length;
        if (step == 0)
        {
            if (--cyclesLeft > 0) step = 1;            // segue batendo; so descansa quando acabam as batidas
            else
            {
                cyclesLeft = Random.Range(3, 7);
                timer = Random.Range(.8f, 1.4f);
                sr.sprite = frames[0];
                return;
            }
        }
        sr.sprite = frames[step];
        timer = Times[step];
    }
}
