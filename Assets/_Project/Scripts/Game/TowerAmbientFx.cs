using UnityEngine;

/// Liga a animacao de ambiente de cada torre pelo nome do sprite, sem mexer em prefab:
/// abelhas na colmeia, vento no chapeu e na barra da roupa do espantalho, vento na folha do
/// canhao de abobora e fumacinha no cano da metralhadora de uvas.
/// Escuta Tower.OnSpriteChanged, entao vale tambem depois de melhorar a torre.
public static class TowerAmbientFx
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Subscribe()
    {
        Tower.OnSpriteChanged -= Apply;
        Tower.OnSpriteChanged += Apply;
    }

    static void Apply(Tower tower, Sprite sprite)
    {
        if (tower == null || sprite == null) return;
        switch (sprite.name)
        {
            case "torre_colmeia":
            case "torre_colmeia_2":
            case "torre_colmeia_3":
                Get<HiveBees>(tower).Setup(sprite, 3 + 2 * tower.Level);   // 3, 5 e 7 abelhas
                break;
            case "torre_espantalho":
            case "torre_espantalho_2":
            case "torre_espantalho_3":
                Get<PixelRegionSway>(tower).Setup(sprite, new[]
                {
                    new PixelRegionSway.Region(21, 3, 23, 10, false),    // chapeu
                    new PixelRegionSway.Region(23, 33, 18, 6, true),     // barra da roupa
                });
                break;
            case "torre_canhao_abobora":
            case "torre_canhao_abobora_2":
            case "torre_canhao_abobora_3":
                Get<PixelRegionSway>(tower).Setup(sprite, new[]
                {
                    new PixelRegionSway.Region(33, 1, 13, 9, false),     // cabinho e folha
                });
                break;
            case "torre_uvas":
                Get<MuzzleSmoke>(tower).muzzle = PixelSprites.Local(sprite, 58f, 35f);
                break;
        }
    }

    static T Get<T>(Tower tower) where T : Component
    {
        T c = tower.GetComponent<T>();
        return c != null ? c : tower.gameObject.AddComponent<T>();
    }
}
