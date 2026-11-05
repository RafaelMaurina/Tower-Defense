using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Luzes pontuais da noite (dificuldade Dificil): janela do celeiro e um lampiao em cada
/// canteiro. Sao Light2D criadas na hora, sem arte nova. O brilho tremula um pouco, como chama.
public class NightLights : MonoBehaviour
{
    static readonly Color Lampiao = new Color(1f, .72f, .38f);

    Light2D[] lights;
    float[] baseIntensity;
    float[] seed;

    void Start()
    {
        var list = new System.Collections.Generic.List<Light2D>();

        GameObject celeiro = GameObject.Find("Celeiro");
        if (celeiro != null)
            list.Add(Create("Luz Celeiro", celeiro.transform.position + new Vector3(0f, 1.2f, 0f), 3.5f, 1.1f));

        foreach (BuildPlot plot in FindObjectsByType<BuildPlot>(FindObjectsSortMode.None))
            list.Add(Create("Luz Canteiro", plot.transform.position + new Vector3(0f, .4f, 0f), 1.6f, .8f));

        lights = list.ToArray();
        baseIntensity = new float[lights.Length];
        seed = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            baseIntensity[i] = lights[i].intensity;
            seed[i] = Random.value * 100f;
        }
    }

    Light2D Create(string name, Vector3 position, float radius, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.position = position;
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = Lampiao;
        light.intensity = intensity;
        light.pointLightInnerRadius = radius * .25f;
        light.pointLightOuterRadius = radius;
        light.falloffIntensity = .7f;
        return light;
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
