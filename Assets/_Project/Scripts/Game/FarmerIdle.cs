using UnityEngine;

/// Fazendeiro parado: respira (sobe e desce 1 pixel) e, de vez em quando, olha para o outro lado.
[RequireComponent(typeof(SpriteRenderer))]
public class FarmerIdle : MonoBehaviour
{
    const float Pixel = 1f / 32f;

    SpriteRenderer sr;
    Vector3 home;
    float bobTimer, turnTimer;
    bool up;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        home = transform.position;
        bobTimer = Random.Range(.2f, .6f);
        turnTimer = Random.Range(2f, 5f);
    }

    void Update()
    {
        bobTimer -= Time.deltaTime;
        if (bobTimer <= 0f)
        {
            up = !up;
            transform.position = home + (up ? Vector3.up * Pixel : Vector3.zero);
            bobTimer = .55f;
        }
        turnTimer -= Time.deltaTime;
        if (turnTimer <= 0f)
        {
            sr.flipX = !sr.flipX;
            turnTimer = Random.Range(2.5f, 6f);
        }
    }
}
