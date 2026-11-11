using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Resposta visual do tiro, no estilo pixel art: a torre recua 2 pixels na direcao contraria
/// ao alvo e volta em dois passos, e um clarao de 2 quadros aparece do lado do alvo.
/// Tudo em passos de pixel inteiro (1/32 de unidade), sem escala nem rotacao.
/// Liga sozinho: escuta Tower.OnFired, nao precisa estar em cena nem em prefab.
public class TowerFireFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float StepTime = .05f;         // segundos de cada passo do recuo e de cada quadro do clarao
    const float FlashDistance = .55f;    // do centro da torre ate o clarao, em casas

    static TowerFireFx runner;
    static Sprite[] flashFrames;

    readonly Dictionary<Tower, Vector3> restPosition = new Dictionary<Tower, Vector3>();
    readonly Stack<SpriteRenderer> freeFlashes = new Stack<SpriteRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        flashFrames = null;
        Tower.OnFired -= HandleFired;
        Tower.OnFired += HandleFired;
    }

    static void HandleFired(Tower tower, Vector3 aim)
    {
        if (tower == null) return;
        if (runner == null)
        {
            runner = new GameObject("TowerFireFx").AddComponent<TowerFireFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.Play(tower, aim);
    }

    void Play(Tower tower, Vector3 aim)
    {
        // se a torre ainda esta no meio de um recuo, parte da posicao de descanso dela
        if (!restPosition.TryGetValue(tower, out Vector3 rest)) rest = tower.transform.position;
        Vector3 dir = aim - rest;
        dir.z = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();

        var sr = tower.GetComponent<SpriteRenderer>();
        Vector3 center = sr != null ? sr.bounds.center - (tower.transform.position - rest) : rest;
        StartCoroutine(Recoil(tower, rest, dir));
        StartCoroutine(Flash(Snap(center + dir * FlashDistance), sr != null ? sr.sortingOrder + 1 : 500));
    }

    IEnumerator Recoil(Tower tower, Vector3 rest, Vector3 dir)
    {
        restPosition[tower] = rest;
        for (int pixels = 2; pixels >= 1; pixels--)
        {
            if (tower == null) { restPosition.Remove(tower); yield break; }   // vendida no meio do recuo
            tower.transform.position = rest - Snap(dir * (pixels * Pixel));
            yield return new WaitForSeconds(StepTime);
        }
        if (tower != null) tower.transform.position = rest;
        restPosition.Remove(tower);
    }

    IEnumerator Flash(Vector3 position, int order)
    {
        SpriteRenderer flash = freeFlashes.Count > 0 ? freeFlashes.Pop() : NewFlash();
        flash.transform.position = position;
        flash.sortingOrder = order;
        flash.gameObject.SetActive(true);
        Sprite[] frames = FlashFrames();
        for (int i = 0; i < frames.Length; i++)
        {
            flash.sprite = frames[i];
            yield return new WaitForSeconds(StepTime);
        }
        flash.gameObject.SetActive(false);
        freeFlashes.Push(flash);
    }

    SpriteRenderer NewFlash()
    {
        var go = new GameObject("Clarao");
        go.transform.SetParent(transform);
        return go.AddComponent<SpriteRenderer>();
    }

    static Vector3 Snap(Vector3 v) =>
        new Vector3(Mathf.Round(v.x / Pixel) * Pixel, Mathf.Round(v.y / Pixel) * Pixel, 0f);

    // Clarao desenhado em codigo com cores da paleta do trio: uma estrela de 7x7 e depois um miolo de 3x3.
    static Sprite[] FlashFrames()
    {
        if (flashFrames != null && flashFrames[0] != null) return flashFrames;
        var claro = new Color32(0xff, 0xe7, 0x31, 255);
        var borda = new Color32(0xf7, 0x96, 0x17, 255);
        string[] grande =
        {
            "...b...",
            "..bcb..",
            ".bcccb.",
            "bcccccb",
            ".bcccb.",
            "..bcb..",
            "...b...",
        };
        string[] pequeno = { ".b.", "bcb", ".b." };
        flashFrames = new[] { Draw(grande, claro, borda), Draw(pequeno, claro, borda) };
        return flashFrames;
    }

    static Sprite Draw(string[] rows, Color32 light, Color32 edge)
    {
        int n = rows.Length;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "clarao" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                char c = rows[n - 1 - y][x];
                px[y * n + x] = c == 'c' ? light : c == 'b' ? edge : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 32f, 0, SpriteMeshType.FullRect);
    }
}
