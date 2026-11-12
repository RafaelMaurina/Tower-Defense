using UnityEngine;

/// "zz" de quem dorme: sobe 1 pixel por vez em 3 passos, some um instante e recomeca.
public class SleepZ : MonoBehaviour
{
    public float stepTime = .5f;

    SpriteRenderer sr;
    Vector3 start;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        start = transform.localPosition;
    }

    void Update()
    {
        int step = (int)(Time.time / stepTime) % 4;
        sr.enabled = step < 3;
        transform.localPosition = start + new Vector3(0f, step * PixelSprites.Pixel, 0f);
    }
}
