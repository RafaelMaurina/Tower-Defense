using UnityEngine;

/// Fumacinha leve e constante saindo do cano: bolinhas de poucos pixels que sobem 1 pixel por vez,
/// encolhem e somem. Desenhada em codigo.
public class MuzzleSmoke : MonoBehaviour
{
    public Vector3 muzzle;               // posicao local da boca do cano
    public float interval = .45f;        // segundos entre uma bolinha e outra
    public float stepTime = .14f;        // segundos por pixel de subida
    const int Life = 9;                  // passos ate sumir

    static Sprite[] puffs;
    SpriteRenderer[] pool;
    int[] step, drift;
    float[] clock;
    float spawn;
    SpriteRenderer owner;

    void Start()
    {
        owner = GetComponent<SpriteRenderer>();
        if (puffs == null || puffs[0] == null)
        {
            var c = new Color32(0xd9, 0xd5, 0xd5, 255);
            var o = new Color32(0, 0, 0, 0);
            puffs = new[]
            {
                PixelSprites.Make(new[] { o, c, o, c, c, c, o, c, o }, 3, 3, new Vector2(.5f, .5f), 32f, "fumaca_3"),
                PixelSprites.Make(new[] { c, c, c, c }, 2, 2, new Vector2(.5f, .5f), 32f, "fumaca_2"),
                PixelSprites.Make(new[] { c }, 1, 1, new Vector2(.5f, .5f), 32f, "fumaca_1"),
            };
        }
        pool = new SpriteRenderer[5];
        step = new int[pool.Length]; clock = new float[pool.Length]; drift = new int[pool.Length];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("Fumaca");
            go.transform.SetParent(transform, false);
            pool[i] = go.AddComponent<SpriteRenderer>();
            go.SetActive(false);
            step[i] = Life;
        }
    }

    void Update()
    {
        spawn -= Time.deltaTime;
        for (int i = 0; i < pool.Length; i++)
        {
            if (step[i] >= Life)
            {
                if (spawn > 0f) continue;
                spawn = interval * Random.Range(.8f, 1.3f);
                step[i] = 0; clock[i] = 0f; drift[i] = 0;
                pool[i].gameObject.SetActive(true);
            }
            else
            {
                clock[i] += Time.deltaTime;
                if (clock[i] < stepTime) continue;
                clock[i] -= stepTime;
                step[i]++;
                if (step[i] >= Life) { pool[i].gameObject.SetActive(false); continue; }
                if (step[i] % 3 == 0) drift[i] += Random.value < .7f ? 1 : -1;   // o vento leva para a direita
            }
            pool[i].sprite = puffs[step[i] * puffs.Length / Life];
            pool[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(.75f, .3f, step[i] / (float)Life));
            pool[i].sortingOrder = owner != null ? owner.sortingOrder + 1 : DrawOrder.Projectile;
            pool[i].transform.localPosition =
                PixelSprites.Snap(muzzle + new Vector3(drift[i], step[i], 0f) * PixelSprites.Pixel);
        }
    }
}
