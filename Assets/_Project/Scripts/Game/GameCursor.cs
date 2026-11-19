using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Ponteiro do mouse do jogo (Resources/cursor.png, 32x32, ponta da seta em 9,5), desenhado por cima de tudo.
/// O cursor do sistema do Windows fica limitado a uns 32 px e nao cresce com Cursor.SetCursor, entao o ponteiro
/// e uma imagem que segue o mouse, em escala inteira (3x em 1080p) para os pixels ficarem nitidos.
public class GameCursor : MonoBehaviour
{
    const int Tamanho = 32;                       // lado do desenho original
    static readonly Vector2 Ponta = new Vector2(9, 5);   // ponta da seta, em pixels do desenho (y de cima para baixo)
    const float EscalaEm1080 = 3f;

    RectTransform imagem;
    int escala;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Criar()
    {
        if (FindFirstObjectByType<GameCursor>() != null) return;
        Texture2D textura = Resources.Load<Texture2D>("cursor");
        if (textura == null) return;
        textura.filterMode = FilterMode.Point;

        var raiz = new GameObject("Cursor do jogo", typeof(RectTransform));
        DontDestroyOnLoad(raiz);
        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;     // acima de qualquer menu ou HUD

        var filho = new GameObject("Seta", typeof(RectTransform), typeof(Image));
        filho.transform.SetParent(raiz.transform, false);
        var sprite = Sprite.Create(textura, new Rect(0, 0, Tamanho, Tamanho),
            new Vector2(Ponta.x / Tamanho, 1f - Ponta.y / Tamanho), Tamanho);
        var img = filho.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;                // nao rouba clique de ninguem

        var cursor = raiz.AddComponent<GameCursor>();
        cursor.imagem = filho.GetComponent<RectTransform>();
        cursor.imagem.anchorMin = cursor.imagem.anchorMax = Vector2.zero;
        cursor.imagem.pivot = new Vector2(Ponta.x / Tamanho, 1f - Ponta.y / Tamanho);
    }

    void Update()
    {
        if (imagem == null) return;
        Cursor.visible = false;
        if (Mouse.current == null) { imagem.gameObject.SetActive(false); return; }

        int nova = Mathf.Max(1, Mathf.RoundToInt(EscalaEm1080 * Screen.height / 1080f));
        if (nova != escala)
        {
            escala = nova;
            imagem.sizeDelta = new Vector2(Tamanho, Tamanho) * escala;
        }
        imagem.position = Mouse.current.position.ReadValue();
    }

    void OnApplicationFocus(bool focus) => Cursor.visible = !focus;

    void OnDestroy() => Cursor.visible = true;
}
