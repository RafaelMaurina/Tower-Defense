using System.Collections;
using UnityEngine;

/// Celeiro tomando dano: treme alguns pixels para os lados, pisca avermelhado e solta lascas de
/// madeira que caem. Fica no objeto "Celeiro" (o gerador de niveis coloca).
/// Olha a vida da base a cada quadro em vez de escutar o evento do GameManager, porque o
/// GameManager zera os proprios eventos quando e recriado.
[RequireComponent(typeof(SpriteRenderer))]
public class BarnHitFx : MonoBehaviour
{
    const float Step = .05f;                              // segundos por passo do tremor
    static readonly int[] Shake = { 3, -3, 2, -2, 1, 0 }; // pixels para o lado em cada passo
    const int ChipCount = 7;

    static Sprite[] chips;

    SpriteRenderer sr;
    Vector3 rest;
    int lastLife = -1;
    Coroutine playing;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rest = transform.position;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        int life = GameManager.BaseLife;
        if (lastLife >= 0 && life < lastLife)
        {
            if (playing == null) playing = StartCoroutine(Hit());   // dano em cima de dano: um tremor so
        }
        lastLife = life;
    }

    IEnumerator Hit()
    {
        EnsureChips();
        // as lascas saem da parede da esquerda, perto do chao: e por onde os inimigos chegam
        Vector3 door = rest + new Vector3(-1.8f, .5f, 0f);
        var parts = new SpriteRenderer[ChipCount];
        var speed = new Vector2[ChipCount];
        var offset = new Vector2[ChipCount];
        for (int i = 0; i < ChipCount; i++)
        {
            var go = new GameObject("Lasca");
            go.transform.SetParent(transform.parent, false);
            parts[i] = go.AddComponent<SpriteRenderer>();
            parts[i].sprite = chips[Random.Range(0, chips.Length)];
            parts[i].sortingOrder = DrawOrder.Projectile;
            speed[i] = new Vector2(Random.Range(-5f, 3f), Random.Range(3f, 7f));   // pixels por passo
        }

        for (int s = 0; s < 10; s++)
        {
            int dx = s < Shake.Length ? Shake[s] : 0;
            transform.position = rest + new Vector3(dx * PixelSprites.Pixel, 0f, 0f);
            sr.color = s < 2 || s == 3 ? new Color(1f, .55f, .55f) : Color.white;
            for (int i = 0; i < ChipCount; i++)
            {
                offset[i] += speed[i];
                speed[i].y -= 2f;                           // gravidade: sobem e caem
                parts[i].transform.position =
                    PixelSprites.Snap(door + new Vector3(offset[i].x, offset[i].y, 0f) * PixelSprites.Pixel);
                if (s >= 8) parts[i].enabled = false;
            }
            yield return new WaitForSeconds(Step);
        }
        transform.position = rest;
        sr.color = Color.white;
        for (int i = 0; i < ChipCount; i++) Destroy(parts[i].gameObject);
        playing = null;
    }

    void OnDisable()
    {
        // interrompido no meio: volta ao lugar e a cor normal
        if (sr == null) return;
        transform.position = rest;
        sr.color = Color.white;
    }

    // lascas de 2x2 e 3x2 nas cores do celeiro: vermelho da parede e madeira do telhado
    static void EnsureChips()
    {
        if (chips != null && chips[0] != null) return;
        var colors = new[]
        {
            new Color32(0xde, 0x00, 0x40, 255), new Color32(0x95, 0x04, 0x2e, 255),
            new Color32(0xb9, 0x71, 0x0f, 255), new Color32(0x75, 0x2d, 0x02, 255),
        };
        chips = new Sprite[colors.Length * 2];
        for (int i = 0; i < colors.Length; i++)
        {
            chips[i * 2] = Block(2, 2, colors[i]);
            chips[i * 2 + 1] = Block(3, 2, colors[i]);
        }
    }

    static Sprite Block(int w, int h, Color32 color)
    {
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = color;
        return PixelSprites.Make(px, w, h, new Vector2(.5f, .5f), 32f, "lasca");
    }
}
