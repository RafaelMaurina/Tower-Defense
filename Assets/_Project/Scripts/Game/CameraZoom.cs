using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

/// Zoom da camera da partida com a roda do mouse, continuo e suavizado. O enquadramento inicial
/// (mapa inteiro) e o limite: nao da para afastar alem dele nem arrastar a camera para fora do mapa.
/// Com zoom, mover com botao do meio arrastando ou WASD/setas. Funciona tambem com o jogo pausado
/// (tempo nao escalado). A Pixel Perfect Camera trava o zoom em passos inteiros, entao ela fica
/// desligada enquanto ampliado e volta a ligar ao retornar a 1x.
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    const float MaxZoom = 3f, WheelStrength = .16f, Smoothing = 10f, KeyPanSpeed = 14f;

    Camera cam;
    PixelPerfectCamera pixelPerfect;
    float baseSize, targetZoom = 1f, currentZoom = 1f;
    Vector2 focusNdc;
    Vector3 homePosition;
    Vector2 homeHalf;

    public static float Zoom { get; private set; } = 1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCamera>();
        baseSize = cam.orthographicSize;
        homePosition = transform.position;
        Zoom = 1f;
    }

    void OnDestroy() => Zoom = 1f;

    void Update()
    {
        if (cam == null || !cam.orthographic) return;
        if (GameHUD.IsOverlayOpen || GameManager.State == GameState.Victory || GameManager.State == GameState.Defeat) return;
        if (homeHalf == Vector2.zero) homeHalf = new Vector2(baseSize * cam.aspect, baseSize);
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
        if (pixelPerfect != null) pixelPerfect.enabled = !magnified;
        if (magnified || pixelPerfect == null) cam.orthographicSize = baseSize / currentZoom;
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
        Vector2 room = homeHalf - Half(Zoom);
        position.x = Mathf.Clamp(position.x, homePosition.x - room.x, homePosition.x + room.x);
        position.y = Mathf.Clamp(position.y, homePosition.y - room.y, homePosition.y + room.y);
        transform.position = new Vector3(position.x, position.y, homePosition.z);
    }
}
