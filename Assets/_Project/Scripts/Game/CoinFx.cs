using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Recompensa da morte: uma moedinha de pixel (7x7, gira entre de frente e de lado) sobe do
/// inimigo uns 15 px desacelerando, com o "+N" ao lado em fonte 3x5 feita a mao. Dura ~0,7 s.
/// Recompensa 0 nao mostra nada. Liga sozinho: escuta Enemy.OnDied.
/// Tudo em pixel inteiro, sem escala nem rotacao. Sprites em cache e renderers em pool.
public class CoinFx : MonoBehaviour
{
    const float Pixel = 1f / 32f;
    const float Tick = .05f;
    const int Ticks = 14;              // 0,7 s
    const int CoinSize = 7;
    static readonly WaitForSeconds TickWait = new WaitForSeconds(Tick);

    static readonly string[][] CoinRows =
    {
        new[] { ".ooooo.", "owYyyyo", "oYyyyyo", "oyyyybo", "oyyyybo", "oybbbbo", ".ooooo." },   // de frente
        new[] { "..ooo..", "..oyo..", "..oYo..", "..oyo..", "..oyo..", "..obo..", "..ooo.." },   // de lado
    };

    // Fonte 3x5: digitos e o "+"
    static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
    {
        { '0', new[] { "XXX", "X.X", "X.X", "X.X", "XXX" } },
        { '1', new[] { ".X.", "XX.", ".X.", ".X.", "XXX" } },
        { '2', new[] { "XXX", "..X", "XXX", "X..", "XXX" } },
        { '3', new[] { "XXX", "..X", "XXX", "..X", "XXX" } },
        { '4', new[] { "X.X", "X.X", "XXX", "..X", "..X" } },
        { '5', new[] { "XXX", "X..", "XXX", "..X", "XXX" } },
        { '6', new[] { "XXX", "X..", "XXX", "X.X", "XXX" } },
        { '7', new[] { "XXX", "..X", "..X", ".X.", ".X." } },
        { '8', new[] { "XXX", "X.X", "XXX", "X.X", "XXX" } },
        { '9', new[] { "XXX", "X.X", "XXX", "..X", "XXX" } },
        { '+', new[] { "...", ".X.", "XXX", ".X.", "..." } },
    };

    static CoinFx runner;
    static Sprite[] coin;
    static readonly Dictionary<int, Sprite> labels = new Dictionary<int, Sprite>();
    readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        runner = null;
        coin = null;
        labels.Clear();
        Enemy.OnDied -= HandleDied;
        Enemy.OnDied += HandleDied;
    }

    static void HandleDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null || enemy.data.reward <= 0) return;
        if (runner == null)
        {
            runner = new GameObject("CoinFx").AddComponent<CoinFx>();
            DontDestroyOnLoad(runner.gameObject);
        }
        runner.StartCoroutine(runner.Play(PixelSprites.Snap(enemy.Center), enemy.data.reward));
    }

    IEnumerator Play(Vector3 center, int reward)
    {
        EnsureSprites();
        Sprite label = LabelFor(reward);
        int total = CoinSize + 1 + (int)label.rect.width;
        int x0 = -(total / 2);             // o conjunto moeda + texto fica centrado no inimigo
        SpriteRenderer coinSr = Rent("Moeda");
        SpriteRenderer text = Rent("Valor");
        text.sprite = label;

        for (int t = 0; t < Ticks; t++)
        {
            float u = t / (Ticks - 1f);
            int rise = 5 + Mathf.RoundToInt(10f * (1f - (1f - u) * (1f - u)));   // sobe desacelerando
            float alpha = t < 11 ? 1f : (t < 13 ? .66f : .33f);
            coinSr.sprite = coin[(t / 2) % 2];
            coinSr.color = new Color(1f, 1f, 1f, alpha);
            text.color = coinSr.color;
            coinSr.transform.position = center + new Vector3((x0 + 3) * Pixel, rise * Pixel, 0f);
            text.transform.position = center + new Vector3((x0 + CoinSize + 2) * Pixel, rise * Pixel, 0f);   // +1 pelo pivo (o contorno do texto fica a esquerda)
            yield return TickWait;
        }
        Give(coinSr);
        Give(text);
    }

    SpriteRenderer Rent(string objName)
    {
        SpriteRenderer sr = null;
        while (free.Count > 0 && sr == null) sr = free.Pop();
        if (sr == null)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform);
            sr = go.AddComponent<SpriteRenderer>();
        }
        sr.sortingOrder = DrawOrder.Projectile + 1;
        sr.color = Color.white;
        sr.enabled = true;
        return sr;
    }

    void Give(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.enabled = false;
        free.Push(sr);
    }

    static void EnsureSprites()
    {
        if (coin != null && coin[0] != null) return;
        coin = new[]
        {
            FxPixels.Rows(CoinRows[0], "moeda_frente", 3, 3, false),
            FxPixels.Rows(CoinRows[1], "moeda_lado", 3, 3, false),
        };
    }

    // "+N" em creme com contorno escuro; o pivo fica na borda esquerda, no meio da altura
    static Sprite LabelFor(int reward)
    {
        if (labels.TryGetValue(reward, out Sprite cached) && cached != null) return cached;
        string text = "+" + reward;
        var rows = new string[5];
        for (int y = 0; y < 5; y++)
        {
            var line = new System.Text.StringBuilder();
            for (int c = 0; c < text.Length; c++)
            {
                if (c > 0) line.Append('.');
                line.Append(Font[text[c]][y].Replace('X', 'w'));
            }
            rows[y] = line.ToString();
        }
        Sprite made = FxPixels.Rows(rows, "valor_" + reward, 0, 2, true);
        labels[reward] = made;
        return made;
    }
}
