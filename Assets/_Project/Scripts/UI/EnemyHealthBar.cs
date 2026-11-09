using UnityEngine;

/// Barra simples acima do inimigo. Os visuais sao filhos do objeto para acompanhar o Pool.
public class EnemyHealthBar : MonoBehaviour
{
    const string BarName = "Barra de vida";
    static Sprite pixel;

    Enemy enemy;
    SpriteRenderer enemySprite;
    SpriteRenderer background;
    SpriteRenderer fill;
    Transform barRoot;
    float width;

    static Sprite Pixel()
    {
        if (pixel != null) return pixel;
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0f, .5f), 1f);
        return pixel;
    }

    public void Initialize(Enemy owner)
    {
        enemy = owner;
        enemySprite = owner.GetComponent<SpriteRenderer>();
        barRoot = transform.Find(BarName);
        if (barRoot == null)
        {
            barRoot = new GameObject(BarName).transform;
            barRoot.SetParent(transform, false);
            background = NewPart("Fundo", barRoot);
            fill = NewPart("Vida", barRoot);
        }
        else
        {
            background = barRoot.Find("Fundo").GetComponent<SpriteRenderer>();
            fill = barRoot.Find("Vida").GetComponent<SpriteRenderer>();
        }

        float scaleX = Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.y));
        barRoot.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f);
        barRoot.localPosition = new Vector3(owner.data.centerOffset.x / scaleX,
            (owner.data.centerOffset.y + .28f) / scaleY, -.01f);
        width = owner.GetComponent<Boss>() != null ? 1.25f : .68f;
        background.transform.localPosition = new Vector3(-width * .5f - .025f, 0f, 0f);
        background.transform.localScale = new Vector3(width + .05f, .12f, 1f);
        fill.transform.localPosition = new Vector3(-width * .5f, 0f, -.01f);
        background.color = new Color32(47, 30, 20, 245);
        Refresh();
        UpdateOrder();
    }

    static SpriteRenderer NewPart(string name, Transform parent)
    {
        var part = new GameObject(name).AddComponent<SpriteRenderer>();
        part.transform.SetParent(parent, false);
        part.sprite = Pixel();
        return part;
    }

    public void Refresh()
    {
        if (enemy == null || fill == null || enemy.data == null) return;
        float fraction = Mathf.Clamp01(enemy.Health / Mathf.Max(1f, enemy.data.health));
        fill.transform.localScale = new Vector3(width * fraction, .075f, 1f);
        fill.color = fraction > .6f ? new Color32(94, 200, 84, 255) :
            fraction > .3f ? new Color32(243, 207, 70, 255) : new Color32(221, 75, 57, 255);
    }

    void LateUpdate() => UpdateOrder();

    void UpdateOrder()
    {
        if (enemySprite == null || background == null || fill == null) return;
        background.sortingLayerID = fill.sortingLayerID = enemySprite.sortingLayerID;
        background.sortingOrder = enemySprite.sortingOrder + 1;
        fill.sortingOrder = enemySprite.sortingOrder + 2;
    }
}
