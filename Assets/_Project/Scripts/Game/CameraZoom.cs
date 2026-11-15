using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// Zoom da camera da partida com a roda do mouse, em passos inteiros para a Pixel Perfect Camera
/// manter o pixel nitido. O enquadramento inicial (mapa inteiro) e o limite: nao da para afastar
/// alem dele nem arrastar a camera para fora do mapa. Com zoom, mover com botao do meio
/// arrastando ou WASD/setas. Funciona tambem com o jogo pausado (tempo nao escalado).
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    // 1x, 1,5x, 2x e 3x: com a base 960x540 e a tela em 1920x1080 todos dao escala inteira.
    static readonly float[] Steps = { 1f, 1.5f, 2f, 3f };
    const float ScrollCooldown = .12f, KeyPanSpeed = 14f;

    Camera cam;
    PixelPerfectCamera pixelPerfect;
    int baseRefX, baseRefY, step;
    float baseSize, nextScroll;
    Vector3 homePosition;
    Vector2 homeHalf;

    public static float Zoom { get; private set; } = 1f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        pixelPerfect = GetComponent<PixelPerfectCamera>();
        if (pixelPerfect != null) { baseRefX = pixelPerfect.refResolutionX; baseRefY = pixelPerfect.refResolutionY; }
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
        HandlePan();
    }

    void HandleScroll()
    {
        if (Mouse.current == null || Time.unscaledTime < nextScroll) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < .01f) return;
        int target = Mathf.Clamp(step + (scroll > 0f ? 1 : -1), 0, Steps.Length - 1);
        if (target == step) return;
        nextScroll = Time.unscaledTime + ScrollCooldown;

        // mantem o ponto do mapa sob o cursor no mesmo lugar da tela
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 ndc = new Vector2(screen.x / Screen.width * 2f - 1f, screen.y / Screen.height * 2f - 1f);
        Vector2 before = Half(Steps[step]);
        Vector2 focus = (Vector2)transform.position + Vector2.Scale(ndc, before);

        step = target;
        Zoom = Steps[step];
        Apply();
        Vector2 after = Half(Zoom);
        Place(focus - Vector2.Scale(ndc, after));
    }

    void HandlePan()
    {
        if (step == 0) { Place(homePosition); return; }
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

    void Apply()
    {
        if (pixelPerfect != null)
        {
            pixelPerfect.refResolutionX = Mathf.RoundToInt(baseRefX / Zoom);
            pixelPerfect.refResolutionY = Mathf.RoundToInt(baseRefY / Zoom);
        }
        else cam.orthographicSize = baseSize / Zoom;
    }

    void Place(Vector2 position)
    {
        Vector2 room = homeHalf - Half(Zoom);
        position.x = Mathf.Clamp(position.x, homePosition.x - room.x, homePosition.x + room.x);
        position.y = Mathf.Clamp(position.y, homePosition.y - room.y, homePosition.y + room.y);
        transform.position = new Vector3(position.x, position.y, homePosition.z);
    }
}
