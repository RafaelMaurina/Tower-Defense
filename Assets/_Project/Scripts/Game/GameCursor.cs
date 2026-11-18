using UnityEngine;

/// Troca o ponteiro do mouse pelo do jogo (Resources/cursor.png, 48x48, o desenho original de 32x32 a 1,5x; ponta da seta em 13,7).
public static class GameCursor
{
    // ponta da seta, em pixels do desenho (y de cima para baixo)
    static readonly Vector2 Hotspot = new Vector2(13, 7);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        Texture2D cursor = Resources.Load<Texture2D>("cursor");
        if (cursor != null) Cursor.SetCursor(cursor, Hotspot, CursorMode.Auto);
    }
}
