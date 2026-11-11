using System.Collections.Generic;
using UnityEngine;

/// Abelhas voando em volta da colmeia. As duas abelhas soltas do desenho saem do sprite e viram
/// objetos que giram numa elipse: passam pela frente na metade de baixo e por tras na de cima.
/// Movimento em pixel inteiro.
[RequireComponent(typeof(SpriteRenderer))]
public class HiveBees : MonoBehaviour
{
    // abelhas soltas no torre_colmeia.png (x, y de cima)
    static readonly RectInt[] Drawn = { new RectInt(52, 9, 6, 3), new RectInt(5, 15, 6, 3) };
    const int Count = 4;
    const float RadiusX = 27f, RadiusY = 9f;     // em pixels de desenho
    static readonly Vector2 Center = new Vector2(31.5f, 22f);

    static readonly Dictionary<Sprite, (Sprite hive, Sprite bee)> cache = new Dictionary<Sprite, (Sprite, Sprite)>();

    SpriteRenderer sr;
    Sprite source;
    readonly List<SpriteRenderer> bees = new List<SpriteRenderer>();
    float[] speed, phase, height;

    public void Setup(Sprite sprite)
    {
        sr = GetComponent<SpriteRenderer>();
        if (!cache.TryGetValue(sprite, out var made) || made.hive == null)
        {
            Color32[] px = PixelSprites.Read(sprite, out int w, out int h);
            if (px == null) return;
            Sprite bee = PixelSprites.Crop(sprite, px, w, h, Drawn[0], "abelha");
            foreach (RectInt r in Drawn) PixelSprites.Erase(px, w, h, r);
            made = (PixelSprites.MakeLike(sprite, px, w, h, "_sem_abelhas"), bee);
            cache[sprite] = made;
        }
        source = sprite;
        sr.sprite = made.hive;

        if (bees.Count == 0)
        {
            speed = new float[Count]; phase = new float[Count]; height = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("Abelha");
                go.transform.SetParent(transform, false);
                bees.Add(go.AddComponent<SpriteRenderer>());
                speed[i] = Random.Range(1.6f, 2.4f) * (i % 2 == 0 ? 1f : -1f);
                phase[i] = i * Mathf.PI * 2f / Count + Random.Range(-.4f, .4f);
                height[i] = Random.Range(-6f, 6f);
            }
        }
        foreach (SpriteRenderer b in bees) b.sprite = made.bee;
    }

    void Update()
    {
        if (source == null) return;
        for (int i = 0; i < bees.Count; i++)
        {
            float a = Time.time * speed[i] + phase[i];
            float x = Center.x + Mathf.Cos(a) * RadiusX;
            float y = Center.y + height[i] - Mathf.Sin(a) * RadiusY + Mathf.Sin(Time.time * 9f + i) * 1.2f;
            bees[i].transform.localPosition = PixelSprites.Snap(PixelSprites.Local(source, x, y));
            // sin < 0 e a metade de baixo da elipse (y cresce para baixo): passa pela frente
            bees[i].sortingOrder = sr.sortingOrder + (Mathf.Sin(a) < 0f ? 1 : -1);
        }
    }
}
