using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Clique em um canteiro abre a escolha de torre ou as acoes da torre colocada.
[RequireComponent(typeof(Collider2D))]
public class BuildPlot : MonoBehaviour
{
    [SerializeField] TowerData[] availableTowers;
    Tower placed;

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

        GameObject towerObject = Instantiate(data.towerPrefab, transform.position, Quaternion.identity);
        placed = towerObject.GetComponent<Tower>();
        if (placed == null)
        {
            Destroy(towerObject);
            GameManager.Instance.AddMoney(cost);
            return false;
        }

        placed.Init(data);
        // o carrinho de mao do canteiro some enquanto ha torre em cima
        GetComponent<SpriteRenderer>().enabled = false;
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
        GetComponent<SpriteRenderer>().enabled = true;
        return true;
    }
}
