using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Importa o pacote novo como pixel art e vincula os sprites à cena Menu.
[InitializeOnLoad]
public static class MenuArtSetup
{
    const string ScenePath = "Assets/_Project/Scenes/Menu.unity";
    const string ArtFolder = "Assets/_Project/Art/Sprites/Menu/";
    const string PhaseArtFolder = "Assets/_Project/Resources/MenuPhase/";
    const string DifficultyArtFolder = "Assets/_Project/Resources/MenuDifficulty/";
    const string RankingArtFolder = "Assets/_Project/Resources/Ranking/";

    static MenuArtSetup() => EditorApplication.delayCall += ConfigureResourceImporters;

    [MenuItem("Defesa da Colheita/Aplicar arte do menu")]
    public static void Apply()
    {
        ConfigureImporters();
        ConfigureResourceImporters();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var controller = Object.FindAnyObjectByType<MenuController>();
        if (controller == null)
        {
            Debug.LogError("MenuController não encontrado na cena Menu.");
            return;
        }

        var serialized = new SerializedObject(controller);
        Assign(serialized, "backgroundSprite", "fundo_menu.png");
        Assign(serialized, "logoSprite", "logo_letreiro.png");
        Assign(serialized, "playNormalSprite", "menu_jogar_normal.png");
        Assign(serialized, "playSelectedSprite", "menu_jogar_selecionado.png");
        Assign(serialized, "difficultyEasyBadge", "dificuldade_facil 2.png");
        Assign(serialized, "difficultyEasyBadgeSelected", "dificuldade_facil_selecionado 2.png");
        Assign(serialized, "difficultyNormalBadge", "dificuldade_normal 2.png");
        Assign(serialized, "difficultyNormalBadgeSelected", "dificuldade_normal_selecionado 2.png");
        Assign(serialized, "difficultyHardBadge", "dificuldade_dificil 2.png");
        Assign(serialized, "difficultyHardBadgeSelected", "dificuldade_dificil_selecionado.png");
        Assign(serialized, "submenuPanelSprite", "submenu_molde.png");
        Assign(serialized, "backButtonSprite", "botao_voltar.png");
        Assign(serialized, "backButtonSelectedSprite", "botao_voltar_selecionado.png");
        Assign(serialized, "rankingNormalSprite", "menu_ranking_normal.png");
        Assign(serialized, "rankingSelectedSprite", "menu_ranking_selecionado.png");
        Assign(serialized, "infoNormalSprite", "menu_informacoes_normal.png");
        Assign(serialized, "infoSelectedSprite", "menu_informacoes_selecionado.png");
        Assign(serialized, "exitNormalSprite", "menu_sair_normal.png");
        Assign(serialized, "exitSelectedSprite", "menu_sair_selecionado.png");
        AssignIfPresent(serialized, "phaseMainFrameSprite", "selecao_moldura_principal.png");
        AssignIfPresent(serialized, "summerCardFrameSprite", "fase_verao_moldura.png");
        AssignIfPresent(serialized, "springCardFrameSprite", "fase_primavera_moldura.png");
        AssignIfPresent(serialized, "autumnCardFrameSprite", "fase_outono_moldura.png");
        AssignIfPresent(serialized, "bonusCardFrameSprite", "fase_bonus_moldura.png");
        AssignIfPresent(serialized, "phaseSelectionOutlineSprite", "fase_contorno_selecionado.png");
        AssignIfPresent(serialized, "summerPreviewSprite", "fase_verao_preview.png");
        AssignIfPresent(serialized, "springPreviewSprite", "fase_primavera_preview.png");
        AssignIfPresent(serialized, "autumnPreviewSprite", "fase_outono_preview.png");
        AssignIfPresent(serialized, "bonusPreviewSprite", "fase_bonus_preview.png");
        AssignIfPresent(serialized, "bonusLockedPreviewSprite", "fase_bonus_preview_bloqueada.png");
        AssignIfPresent(serialized, "bonusLockSprite", "fase_cadeado.png");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Sprites novos do menu importados e vinculados à cena Menu.");
    }

    static void ConfigureImporters()
    {
        ConfigureImportersIn(ArtFolder);
        ConfigureImportersIn(PhaseArtFolder);
    }

    static void ConfigureResourceImporters()
    {
        ConfigureImportersIn(PhaseArtFolder);
        ConfigureImportersIn(DifficultyArtFolder);
        ConfigureImportersIn(RankingArtFolder);
    }

    static void ConfigureImportersIn(string folder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.filterMode != FilterMode.Point
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.textureCompression != TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (changed) importer.SaveAndReimport();
        }
    }

    static void Assign(SerializedObject serialized, string field, string file)
    {
        var property = serialized.FindProperty(field);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + file);
        if (property == null || sprite == null)
        {
            Debug.LogWarning($"Não foi possível vincular {field}: {ArtFolder + file}");
            return;
        }
        property.objectReferenceValue = sprite;
    }

    static void AssignIfPresent(SerializedObject serialized, string field, string file)
    {
        var property = serialized.FindProperty(field);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PhaseArtFolder + file);
        if (property == null || sprite == null) return;
        property.objectReferenceValue = sprite;
    }

}
