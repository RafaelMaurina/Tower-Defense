using System.Collections.Generic;
using UnityEngine;

/// Abelhas voando em volta da colmeia. As abelhas do desenho saem do sprite e viram
/// objetos que giram numa elipse: passam pela frente na metade de baixo e por tras na de cima.
/// Movimento em pixel inteiro.
[RequireComponent(typeof(SpriteRenderer))]
public class HiveBees : MonoBehaviour
{
    const float RadiusX = 27f, RadiusY = 9f;     // em pixels de desenho
    static readonly Vector2 Center = new Vector2(31.5f, 22f);

    static Sprite bee;

    SpriteRenderer sr;
    Sprite source;
    readonly List<SpriteRenderer> bees = new List<SpriteRenderer>();
    float[] speed, phase, height;

    /// 'count' abelhas em volta da colmeia: mais abelhas a cada nivel da torre.
    public void Setup(Sprite sprite, int count)
    {
        sr = GetComponent<SpriteRenderer>();
        source = sprite;
        if (bee == null) bee = MakeBee();

        // refaz as abelhas quando o numero muda (melhoria da torre)
        foreach (SpriteRenderer b in bees) if (b != null) Destroy(b.gameObject);
        bees.Clear();
        speed = new float[count]; phase = new float[count]; height = new float[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Abelha");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = bee;
            bees.Add(r);
            speed[i] = Random.Range(1.6f, 2.4f) * (i % 2 == 0 ? 1f : -1f);
            phase[i] = i * Mathf.PI * 2f / count + Random.Range(-.4f, .4f);
            height[i] = Random.Range(-6f, 6f);
        }
    }

    // abelha de 6x3 pixels: asas claras em cima, corpo listrado embaixo
    static Sprite MakeBee()
    {
        Color32 o = new Color32(0x28, 0x18, 0x01, 255), w = new Color32(0xe2, 0xe2, 0xe2, 255), y = new Color32(0xf9, 0xc2, 0x2b, 255);
        Color32[] rows = { o, o, w, w, o, o,   o, y, o, y, o, o,   o, o, o, o, o, o };
        var px = new Color32[18];
        for (int row = 0; row < 3; row++)
            for (int x = 0; x < 6; x++)
                px[(2 - row) * 6 + x] = rows[row * 6 + x];
        return PixelSprites.Make(px, 6, 3, new Vector2(.5f, .5f), 32f, "abelha");
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
