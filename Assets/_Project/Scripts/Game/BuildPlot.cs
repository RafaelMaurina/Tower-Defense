using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Clique em um canteiro abre a escolha de torre ou as acoes da torre colocada.
[RequireComponent(typeof(Collider2D))]
public class BuildPlot : MonoBehaviour
{
    [SerializeField] TowerData[] availableTowers;
    Tower placed;
    /// Canteiro so de terra, para ficar embaixo da torre construida. O gerador dos niveis preenche.
    public Sprite builtSprite;
    Sprite emptySprite;
    /// Carrinho e ferramentas por cima da terra do canteiro; somem quando a torre e construida.
    public GameObject ferramentas;

    public Tower Placed => placed;
    public TowerData[] AvailableTowers => availableTowers;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Update()
    {
        if (Mouse.current == null) return;
        Camera gameCamera = Camera.main;
        if (gameCamera == null) return;
        Vector2 worldPoint = gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool overPlot = GetComponent<Collider2D>().OverlapPoint(worldPoint);
        if (placed != null && overPlot && !overUi && !GameHUD.IsOverlayOpen && GameManager.State == GameState.Playing)
            GameHUD.ShowHoverRange(this);
        else GameHUD.HideHoverRange(this);
        if (!Mouse.current.leftButton.wasPressedThisFrame || !overPlot || overUi || GameHUD.IsOverlayOpen ||
            GameManager.State != GameState.Playing) return;

        if (placed != null) GameHUD.ShowTowerActions(this);
        else GameHUD.ShowTowerChoices(this, availableTowers);
    }

    public void SetAvailableTowers(TowerData[] catalog) => availableTowers = catalog;

    public bool Build(TowerData data)
    {
        if (placed != null || data == null || data.towerPrefab == null || data.levels == null || data.levels.Length == 0)
            return false;

        Tower prefabTower = data.towerPrefab.GetComponent<Tower>();
        if (prefabTower == null || GameManager.Instance == null) return false;
        int cost = Mathf.Max(0, data.levels[0].cost);
        if (!GameManager.Instance.Spend(cost)) return false;

        // o pe da torre apoia perto da base do canteiro, e nao no centro, para a terra ficar embaixo dela
        GameObject towerObject = Instantiate(data.towerPrefab, transform.position + new Vector3(0f, -0.35f, 0f), Quaternion.identity);
        placed = towerObject.GetComponent<Tower>();
        if (placed == null)
        {
            Destroy(towerObject);
            GameManager.Instance.AddMoney(cost);
            return false;
        }

        placed.Init(data);
        // com torre em cima, o carrinho de mao da lugar ao canteiro so de terra
        if (ferramentas != null) { ferramentas.SetActive(false); return true; }
        var sr = GetComponent<SpriteRenderer>();
        if (emptySprite == null) emptySprite = sr.sprite;
        if (builtSprite != null) sr.sprite = builtSprite;
        else sr.enabled = false;
        return true;
    }

    public int GetNextUpgradeCost()
    {
        if (placed == null || !placed.CanUpgrade) return 0;
        return Mathf.Max(0, placed.data.levels[placed.Level + 1].cost);
    }

    public int GetSellRefund()
    {
        if (placed == null) return 0;
        int invested = 0;
        for (int i = 0; i <= placed.Level; i++) invested += Mathf.Max(0, placed.data.levels[i].cost);
        return Mathf.RoundToInt(invested * 0.8f);
    }

    public bool Upgrade()
    {
        if (placed == null || !placed.CanUpgrade || GameManager.Instance == null) return false;
        int cost = GetNextUpgradeCost();
        if (!GameManager.Instance.Spend(cost)) return false;
        return placed.Upgrade();
    }

    public bool Sell()
    {
        if (placed == null || GameManager.Instance == null || GameManager.State != GameState.Playing) return false;
        GameManager.Instance.AddMoney(GetSellRefund());
        Destroy(placed.gameObject);
        placed = null;
        if (ferramentas != null) { ferramentas.SetActive(true); return true; }
        var sr = GetComponent<SpriteRenderer>();
        if (emptySprite != null) sr.sprite = emptySprite;
        sr.enabled = true;
        return true;
    }
}
