using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Gera os niveis completos, ainda sem inimigos: chao em Tilemap com as bordas
/// de grama escolhidas automaticamente, caminho dos inimigos (WaypointPath),
/// canteiros de construcao, decoracao e a base.
///
/// Cada nivel vira DUAS coisas:
///  - um prefab em Prefabs/Niveis, que e o que a cena de jogo vai instanciar;
///  - uma cena em Scenes, so para abrir, dar Play e testar o nivel sozinho.
/// Um prefab por nivel tambem evita conflito de merge: cada um mexe no seu.
public static class MontarNiveis
{
    const string SPR      = "Assets/_Project/Art/Sprites/";
    const string TILES    = "Assets/_Project/Art/Tiles";
    const string PREFABS  = "Assets/_Project/Prefabs/Niveis";
    const string CENAS    = "Assets/_Project/Scenes";
    const string TEMPLATE = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";

    // 30 x 17 casas de 64px: camera 1:1 em 1920x1080 (cada pixel da arte = 1 pixel da tela).
    const int W = 30, H = 17;
    const int REF_X = 1920, REF_Y = 1080;

    // Ordem de desenho: chao 0, coisas deitadas no chao 1, objetos em pe 100+.
    const int ORDEM_CHAO = 0, ORDEM_DECALQUE = 1;

    class Nivel
    {
        public int id; public string nome; public int estacao; public string cultura;
        public Vector2Int[][] caminhos;
        public Vector2Int[] canteiros, plantacao, cogumelos;
        public Vector2Int poco;
        public RectInt celeiro;   // area reservada para o celeiro, encostada no fim do caminho
    }

    static Vector2Int[] P(params int[] xy)
    {
        var r = new Vector2Int[xy.Length / 2];
        for (int i = 0; i < r.Length; i++) r[i] = new Vector2Int(xy[2 * i], xy[2 * i + 1]);
        return r;
    }

    // Casa (x, y) com y = 0 embaixo. O primeiro ponto de cada caminho fica em
    // x = -1, fora da tela: e de la que os inimigos entram.
    // O poco ocupa 3x3 casas; a posicao dada e a casa de baixo a esquerda.
    // As linhas y = 0 e y = 16 sao da cerca.
    static readonly Nivel[] NIVEIS =
    {
        // Primavera: zigue-zague com 6 curvas. 10 canteiros.
        new Nivel { id = 1, nome = "Primavera", estacao = 1, cultura = "tile_cenoura",
            caminhos  = new[] { P(-1,12, 8,12, 8,5, 16,5, 16,11, 23,11, 23,5, 25,5) },
            celeiro   = new RectInt(26, 4, 4, 4),
            canteiros = P(4,13, 7,10, 10,6, 12,4, 14,6, 17,9, 19,12, 20,10, 22,7, 24,4),
            plantacao = P(2,3, 3,3, 4,3, 2,4, 3,4, 4,4, 2,5, 3,5, 4,5),
            poco = new Vector2Int(11, 8),
            cogumelos = P(2,8, 18,2, 12,14, 28,13) },

        // Verao: serpentina longa. 12 canteiros.
        new Nivel { id = 2, nome = "Verao", estacao = 2, cultura = "tile_trigo",
            caminhos  = new[] { P(-1,3, 5,3, 5,13, 11,13, 11,4, 17,4, 17,13, 23,13, 23,8, 25,8) },
            celeiro   = new RectInt(26, 7, 4, 4),
            canteiros = P(3,4, 6,7, 6,11, 8,14, 10,10, 12,8, 14,5, 16,10, 18,7, 20,14, 22,10, 24,7),
            plantacao = P(19,1, 20,1, 21,1, 19,2, 20,2, 21,2, 19,3, 20,3, 21,3),
            poco = new Vector2Int(13, 9),
            cogumelos = P(2,9, 8,1, 27,14, 14,15) },

        // Outono: duas entradas que se juntam em (7,8). 12 canteiros.
        new Nivel { id = 3, nome = "Outono", estacao = 3, cultura = "tile_abobora",
            caminhos  = new[] { P(-1,13, 7,13, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9),
                                P(-1,3, 7,3, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9) },
            celeiro   = new RectInt(26, 8, 4, 4),
            canteiros = P(5,12, 5,4, 8,11, 6,7, 10,9, 12,7, 14,5, 16,6, 18,5, 20,3, 21,7, 24,10),
            plantacao = P(1,6, 2,6, 3,6, 1,7, 2,7, 3,7, 1,8, 2,8, 3,8),
            poco = new Vector2Int(10, 11),
            cogumelos = P(3,14, 18,12, 27,2, 12,2) },
    };

    [MenuItem("Defesa da Colheita/Gerar niveis")]
    public static void Gerar() => GerarTodos(false);

    /// Algum prefab ou cena de nivel ainda nao existe neste computador?
    public static bool FaltaAlgum()
    {
        foreach (var nv in NIVEIS)
        {
            string nome = "Nivel_" + nv.id + "_" + nv.nome;
            if (AssetDatabase.LoadAssetAtPath<Object>(PREFABS + "/" + nome + ".prefab") == null) return true;
            if (AssetDatabase.LoadAssetAtPath<Object>(CENAS + "/" + nome + ".unity") == null) return true;
        }
        return false;
    }

    [MenuItem("Defesa da Colheita/Gerar niveis (apagar e refazer)")]
    public static void Refazer()
    {
        if (!EditorUtility.DisplayDialog("Refazer niveis",
                "Apaga os prefabs e as cenas dos niveis e gera de novo. Edicoes feitas a mao neles serao perdidas.",
                "Apagar e refazer", "Cancelar"))
            return;
        GerarTodos(true);
    }

    static void GerarTodos(bool sobrescrever)
    {
        GarantirPasta("Assets/_Project", "Prefabs");
        GarantirPasta("Assets/_Project/Prefabs", "Niveis");
        GarantirPasta("Assets/_Project", "Scenes");
        GarantirPasta("Assets/_Project/Art", "Tiles");

        var anterior = SceneManager.GetActiveScene();
        int feitos = 0;
        foreach (var nv in NIVEIS)
        {
            string nome = "Nivel_" + nv.id + "_" + nv.nome;
            string prefab = PREFABS + "/" + nome + ".prefab";
            string cena = CENAS + "/" + nome + ".unity";

            bool existe = AssetDatabase.LoadAssetAtPath<Object>(prefab) != null
                       || AssetDatabase.LoadAssetAtPath<Object>(cena) != null;
            if (existe && !sobrescrever)
            {
                Debug.Log(nome + " ja existe, mantido. Use \"apagar e refazer\" para gerar de novo.");
                continue;
            }
            if (SceneManager.GetSceneByPath(cena).isLoaded)
            {
                Debug.LogWarning(nome + " esta aberta no editor. Feche a cena e rode de novo.");
                continue;
            }
            AssetDatabase.DeleteAsset(prefab);
            AssetDatabase.DeleteAsset(cena);

            Montar(nv, nome, prefab, cena);
            feitos++;
        }
        if (anterior.IsValid()) SceneManager.SetActiveScene(anterior);
        AssetDatabase.SaveAssets();
        Debug.Log(feitos + " nivel(is) gerado(s). Cenas em " + CENAS + ", prefabs em " + PREFABS + ".");
    }

    static void Montar(Nivel nv, string nome, string prefabPath, string cenaPath)
    {
        // A cena do template ja traz camera e Global Light 2D. Sem luz 2D, os
        // sprites com material Lit do URP aparecem pretos.
        Scene cena;
        if (AssetDatabase.CopyAsset(TEMPLATE, cenaPath))
            cena = EditorSceneManager.OpenScene(cenaPath, OpenSceneMode.Additive);
        else
        {
            cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(cena);
            new GameObject("Main Camera", typeof(Camera)).tag = "MainCamera";
            new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        }
        SceneManager.SetActiveScene(cena);

        var estrada = CelulasDaEstrada(nv);
        var raiz = new GameObject(nome);

        PintarChao(nv, estrada, raiz.transform);
        CriarCaminhos(nv, raiz.transform);
        CriarCanteiros(nv, raiz.transform);
        CriarDecoracao(nv, raiz.transform);
        CriarBase(nv, raiz.transform);

        PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, prefabPath, InteractionMode.AutomatedAction);

        foreach (var go in cena.GetRootGameObjects())
        {
            var cam = go.GetComponent<Camera>();
            if (cam == null) continue;
            cam.orthographic = true;
            cam.orthographicSize = REF_Y / 2f / 64f;              // 1080px de altura / 64 PPU / 2
            cam.transform.position = new Vector3(W / 2f, H / 2f, -10f);
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 64;
            ppc.refResolutionX = REF_X;
            ppc.refResolutionY = REF_Y;
        }

        EditorSceneManager.SaveScene(cena, cenaPath);
        EditorSceneManager.CloseScene(cena, true);
    }

    // ---------------------------------------------------------------- chao

    static HashSet<Vector2Int> CelulasDaEstrada(Nivel nv)
    {
        var r = new HashSet<Vector2Int>();
        foreach (var cam in nv.caminhos)
            for (int i = 1; i < cam.Length; i++)
            {
                Vector2Int a = cam[i - 1], b = cam[i];
                for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
                    for (int y = Mathf.Min(a.y, b.y); y <= Mathf.Max(a.y, b.y); y++)
                        r.Add(new Vector2Int(x, y));
            }
        return r;
    }

    /// Qual borda de grama usar: a terra fica do lado que encosta na estrada.
    static string Borda(int x, int y, HashSet<Vector2Int> r)
    {
        bool cima  = r.Contains(new Vector2Int(x, y + 1));
        bool baixo = r.Contains(new Vector2Int(x, y - 1));
        bool esq   = r.Contains(new Vector2Int(x - 1, y));
        bool dir   = r.Contains(new Vector2Int(x + 1, y));
        if (cima && esq)  return "_canto_topo_esq";
        if (cima && dir)  return "_canto_topo_dir";
        if (baixo && esq) return "_canto_base_esq";
        if (baixo && dir) return "_canto_base_dir";
        if (cima)  return "_topo";
        if (baixo) return "_base";
        if (esq)   return "_esq";
        if (dir)   return "_dir";
        return "";
    }

    static void PintarChao(Nivel nv, HashSet<Vector2Int> estrada, Transform pai)
    {
        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.transform.SetParent(pai);
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var go = new GameObject("Chao");
        go.transform.SetParent(grid.transform);
        var mapa = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>().sortingOrder = ORDEM_CHAO;

        var pista = ObterTile("Tiles/Estrada/estrada");
        string g = "Tiles/Grama/grama_" + nv.estacao;
        // Uma casa de sobra em volta: a camera mostra 8,4 casas de altura e o
        // mapa tem 8, entao sem isso aparece a cor de fundo em cima e embaixo.
        // A casa x = -1 do caminho tambem vira estrada, e o inimigo ja entra nela.
        for (int x = -1; x < W + 1; x++)
            for (int y = -1; y < H + 1; y++)
            {
                var c = new Vector3Int(x, y, 0);
                mapa.SetTile(c, estrada.Contains(new Vector2Int(x, y)) ? pista : ObterTile(g + Borda(x, y, estrada)));
            }
    }

    // ------------------------------------------------------------ caminhos

    static void CriarCaminhos(Nivel nv, Transform pai)
    {
        var grupo = new GameObject("Caminhos").transform;
        grupo.SetParent(pai);
        for (int k = 0; k < nv.caminhos.Length; k++)
        {
            var go = new GameObject("Caminho_" + (char)('A' + k));
            go.transform.SetParent(grupo);
            go.AddComponent<WaypointPath>();
            var cam = nv.caminhos[k];
            for (int i = 0; i < cam.Length; i++)
            {
                var p = new GameObject("P" + i.ToString("00")).transform;
                p.SetParent(go.transform);
                p.position = Centro(cam[i]);
            }
        }
    }

    // ------------------------------------------------ canteiros e decoracao

    static void CriarCanteiros(Nivel nv, Transform pai)
    {
        var grupo = new GameObject("Canteiros").transform;
        grupo.SetParent(pai);
        for (int i = 0; i < nv.canteiros.Length; i++)
        {
            var go = Sprite_(grupo, "Tiles/tile_canteiro", Centro(nv.canteiros[i]), ORDEM_DECALQUE);
            go.name = "Canteiro_" + (i + 1).ToString("00");
            // colisor para o clique de construir, que vem depois
            go.AddComponent<BoxCollider2D>().isTrigger = true;
        }
    }

    static void CriarDecoracao(Nivel nv, Transform pai)
    {
        var grupo = new GameObject("Decoracao").transform;
        grupo.SetParent(pai);

        foreach (var c in nv.plantacao)
            Sprite_(grupo, "Tiles/" + nv.cultura, Centro(c), ORDEM_DECALQUE);

        // props de pivo no pe apoiam na base da casa; o poco (3x3) centra na casa do meio
        Sprite_(grupo, "Props/prop_poco_3x", new Vector3(nv.poco.x + 1.5f, nv.poco.y, 0f), -1);
        foreach (var c in nv.cogumelos)
            Sprite_(grupo, "Props/prop_cogumelos", new Vector3(c.x + 0.5f, c.y + 0.2f, 0f), -1);

        // cerca em cima e embaixo; pecas _3x de 96px = uma casa e meia cada
        var cerca = new GameObject("Cerca").transform;
        cerca.SetParent(grupo);
        foreach (float y in new[] { 0.05f, H - 0.95f })
            for (int i = 0; i < W * 2 / 3; i++)
                Sprite_(cerca, "Cerca/cerca_lado_" + (i % 3 + 1) + "_3x", new Vector3(0.75f + i * 1.5f, y, 0f), -1);
    }

    static void CriarBase(Nivel nv, Transform pai)
    {
        // O gatilho fica no fim do caminho: e ele que detecta o inimigo chegando.
        var go = new GameObject("Base");
        go.transform.SetParent(pai);
        go.transform.position = Centro(nv.caminhos[0][nv.caminhos[0].Length - 1]);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        // Arte do celeiro ainda nao existe: este objeto marca a area reservada.
        // Pivo no pe e no meio, como os outros props; e so colocar o SpriteRenderer.
        var celeiro = new GameObject("Celeiro");
        celeiro.transform.SetParent(pai);
        celeiro.transform.position = new Vector3(nv.celeiro.x + nv.celeiro.width / 2f, nv.celeiro.y, 0f);
    }

    // --------------------------------------------------------- utilitarios

    static Vector3 Centro(Vector2Int c) => new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);

    /// ordem &lt; 0 = objeto em pe, ordenado pela altura na tela.
    static GameObject Sprite_(Transform pai, string caminho, Vector3 pos, int ordem)
    {
        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(caminho));
        go.transform.SetParent(pai);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminho + ".png");
        if (sr.sprite == null) Debug.LogWarning("Sprite nao encontrado: " + caminho);
        // mais embaixo na tela = mais perto da camera = desenha por cima
        sr.sortingOrder = ordem >= 0 ? ordem : 100 + Mathf.RoundToInt((H - pos.y) * 10f);
        return go;
    }

    static readonly Dictionary<string, Tile> cache = new Dictionary<string, Tile>();

    static Tile ObterTile(string caminhoSprite)
    {
        if (cache.TryGetValue(caminhoSprite, out var t) && t != null) return t;

        string destino = TILES + "/" + System.IO.Path.GetFileNameWithoutExtension(caminhoSprite) + ".asset";
        t = AssetDatabase.LoadAssetAtPath<Tile>(destino);
        if (t == null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminhoSprite + ".png");
            if (sprite == null) { Debug.LogWarning("Sprite nao encontrado: " + caminhoSprite); return null; }
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            AssetDatabase.CreateAsset(t, destino);
        }
        cache[caminhoSprite] = t;
        return t;
    }

    static void GarantirPasta(string pai, string nome)
    {
        if (!AssetDatabase.IsValidFolder(pai + "/" + nome)) AssetDatabase.CreateFolder(pai, nome);
    }
}
