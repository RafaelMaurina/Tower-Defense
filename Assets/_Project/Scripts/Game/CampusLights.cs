using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Luz dos postes do campus: cada filho deste objeto e um poste, e ao iniciar ganha uma Light2D pontual
/// amarela no lampiao, criada na hora (como as do NightLights, que ja funcionam assim). O brilho tremula
/// um pouco, como lampada velha. Independe da dificuldade: o campus e sempre noite.
public class CampusLights : MonoBehaviour
{
    static readonly Color Lampiao = new Color(1f, .8f, .4f);
    // vidro do lampiao: linha 10 de 64 do desenho, ~1,6 casa acima do pe do poste
    static readonly Vector3 Vidro = new Vector3(0f, 1.6f, 0f);
    const float Raio = 3f, Intensidade = 1f;

    Light2D[] lights;
    float[] baseIntensity;
    float[] seed;

    void Start()
    {
        int n = transform.childCount;
        var postes = new Transform[n];
        for (int i = 0; i < n; i++) postes[i] = transform.GetChild(i);

        lights = new Light2D[n];
        baseIntensity = new float[n];
        seed = new float[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Luz Poste");
            go.transform.SetParent(postes[i], false);
            go.transform.localPosition = Vidro;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = Lampiao;
            light.intensity = Intensidade;
            light.pointLightInnerRadius = Raio * .25f;
            light.pointLightOuterRadius = Raio;
            light.falloffIntensity = .7f;
            lights[i] = light;
            baseIntensity[i] = light.intensity;
            seed[i] = Random.value * 100f;
        }
    }

    void Update()
    {
        // tremulacao suave; Perlin com semente propria para cada luz nao piscar junto
        for (int i = 0; i < lights.Length; i++)
        {
            float n = Mathf.PerlinNoise(seed[i], Time.time * 3f);
            lights[i].intensity = baseIntensity[i] * (.85f + .3f * n);
        }
    }
}
