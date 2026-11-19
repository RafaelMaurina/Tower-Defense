using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// Zoom da camera da partida com a roda do mouse, continuo e suavizado. O enquadramento inicial
/// (mapa inteiro) e o limite: nao da para afastar alem dele nem arrastar a camera para fora do mapa.
/// Com zoom, mover com botao do meio arrastando ou WASD/setas. Funciona tambem com o jogo pausado
/// (tempo nao escalado). A Pixel Perfect Camera trava o zoom em passos inteiros, entao ela fica
/// desligada enquanto ampliado e volta a ligar ao retornar a 1x.
///
/// Enquadramento por resolucao: so em janelas que sao multiplo inteiro exato de 960x540 (1920x1080,
/// 2880x1620, 3840x2160...) a Pixel Perfect Camera fica ligada. Em qualquer outra (1366x768, 1600x900,
/// 2560x1440...) ela desliga e o tamanho da camera e calculado para o mapa inteiro caber na janela,
/// sem faixa vazia e sem zoom que sobra ou falta. Recalcula ao redimensionar a janela.
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    const float MaxZoom = 3f, WheelStrength = .16f, Smoothing = 10f, KeyPanSpeed = 14f;

    Camera cam;
    PixelPerfectCamera pixelPerfect;
    // grao do jogo: 960x540 de referencia, PPU 32 (mesmo que MontarNiveis grava na Pixel Perfect Camera)
    const float RefWidth = 960f, RefHeight = 540f, RefPpu = 32f;
    // O mapa tem 30x17 casas em todas as fases (fixo de proposito: a posicao da camera nao e fonte confiavel).
    // A altura (17) entra pelo piso de baseSize, ver RefreshFraming.
    const float MapWidth = 30f;
    const float MapHeight = 17f;
    // Onde sobrar tela: verde de grama de dia; de noite (Campus e dificuldade Dificil) a luz nao alcanca
    // o fundo, entao ele e quase preto para nao brilhar fora do mapa.
    static readonly Color DayBorder = new Color32(52, 92, 40, 255), NightBorder = new Color32(8, 10, 18, 255);

    float baseSize, targetZoom = 1f, currentZoom = 1f;
    Vector2Int lastScreen;
    Vector2 focusNdc;
    Vector3 homePosition;
    Vector2 homeHalf;

    public static float Zoom { get; private set; } = 1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCamera>();
        homePosition = transform.position;
        cam.clearFlags = CameraClearFlags.SolidColor;
        bool night = SceneManager.GetActiveScene().name == CampaignProgress.BonusScene || GameManager.Difficulty == GameDifficulty.Hard;
        cam.backgroundColor = night ? NightBorder : DayBorder;
        RefreshFraming();
        Zoom = 1f;
    }

    /// Escolhe entre pixel perfect (janela multiplo exato do 960x540) e ajuste continuo ao mapa inteiro.
    void RefreshFraming()
    {
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        float aspect = (float)Screen.width / Screen.height;
        float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
        bool exact = Mathf.Abs(scale - Mathf.Round(scale)) < .001f && Mathf.Abs(aspect - RefWidth / RefHeight) < .01f;

        if (exact)
            baseSize = RefHeight / (RefPpu * 2f);   // 540 / 64 = 8,4375 unidades
        else
        {
            // mapa inteiro: cabe pela altura ou pela largura, o que apertar mais. O piso e a altura do modo
            // exato (8,4375): em 16:9 fica igual a ele e enche a tela (8,5 deixaria uma faixa fina dos lados).
            baseSize = Mathf.Max(RefHeight / (RefPpu * 2f), MapWidth / 2f / aspect);
        }
        if (pixelPerfect != null) pixelPerfect.enabled = exact && currentZoom <= 1f;
        exactFraming = exact;
        cam.orthographicSize = baseSize / currentZoom;
        homeHalf = new Vector2(baseSize * cam.aspect, baseSize);
        Place((Vector2)transform.position);
    }
    bool exactFraming;

    void OnDestroy() => Zoom = 1f;

    void Update()
    {
        if (cam == null || !cam.orthographic) return;
        // antes do return: trocar a resolucao no painel de configuracoes (pausa) reenquadra na hora
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height) RefreshFraming();
        if (GameHUD.IsOverlayOpen || GameManager.State == GameState.Victory || GameManager.State == GameState.Defeat) return;
        HandleScroll();
        AnimateZoom();
        HandlePan();
    }

    void HandleScroll()
    {
        if (Mouse.current == null) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < .01f) return;
        // roda comum entrega +-120 (ou +-1); trackpad entrega valores pequenos e continuos
        float notches = Mathf.Abs(scroll) >= 10f ? scroll / 120f : scroll;
        targetZoom = Mathf.Clamp(targetZoom * Mathf.Exp(Mathf.Clamp(notches, -3f, 3f) * WheelStrength), 1f, MaxZoom);
        Vector2 screen = Mouse.current.position.ReadValue();
        focusNdc = new Vector2(Mathf.Clamp(screen.x / Screen.width * 2f - 1f, -1f, 1f),
            Mathf.Clamp(screen.y / Screen.height * 2f - 1f, -1f, 1f));
    }

    void AnimateZoom()
    {
        if (Mathf.Approximately(currentZoom, targetZoom)) return;
        float previous = currentZoom;
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, 1f - Mathf.Exp(-Smoothing * Time.unscaledDeltaTime));
        if (Mathf.Abs(currentZoom - targetZoom) < .002f) currentZoom = targetZoom;
        Zoom = currentZoom;

        // mantem o ponto do mapa sob o cursor no mesmo lugar da tela
        Vector2 focus = (Vector2)transform.position + Vector2.Scale(focusNdc, Half(previous));
        bool magnified = currentZoom > 1f;
        if (pixelPerfect != null) pixelPerfect.enabled = exactFraming && !magnified;
        if (magnified || !exactFraming || pixelPerfect == null) cam.orthographicSize = baseSize / currentZoom;
        Place(focus - Vector2.Scale(focusNdc, Half(currentZoom)));
    }

    void HandlePan()
    {
        if (currentZoom <= 1f) { Place(homePosition); return; }
        Vector2 move = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
        }
        Vector2 position = (Vector2)transform.position + move * (KeyPanSpeed / Zoom * Time.unscaledDeltaTime);
        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            float worldPerPixel = 2f * Half(Zoom).y / Screen.height;
            position -= Mouse.current.delta.ReadValue() * worldPerPixel;
        }
        Place(position);
    }

    Vector2 Half(float zoom) => new Vector2(baseSize * cam.aspect, baseSize) / zoom;

    void Place(Vector2 position)
    {
        // o limite e o mapa (30x17), nao o enquadramento de 1x: onde sobra faixa (ultrawide, 16:10), com zoom
        // nao da para arrastar a camera para dentro dela. O Max(0) evita min maior que max no Clamp.
        Vector2 room = Vector2.Max(Vector2.zero, Vector2.Min(homeHalf, new Vector2(MapWidth / 2f, MapHeight / 2f)) - Half(Zoom));
        position.x = Mathf.Clamp(position.x, homePosition.x - room.x, homePosition.x + room.x);
        position.y = Mathf.Clamp(position.y, homePosition.y - room.y, homePosition.y + room.y);
        transform.position = new Vector3(position.x, position.y, homePosition.z);
    }
}
