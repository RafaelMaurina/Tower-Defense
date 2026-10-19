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

    // 15 x 8 casas de 64px: cabe inteiro na referencia 960x540 da camera.
    const int W = 15, H = 8;

    // Ordem de desenho: chao 0, coisas deitadas no chao 1, objetos em pe 100+.
    const int ORDEM_CHAO = 0, ORDEM_DECALQUE = 1;

    class Nivel
    {
        public int id; public string nome; public int estacao; public string cultura;
        public Vector2Int[][] caminhos;
        public Vector2Int[] canteiros, plantacao, cogumelos;
        public Vector2Int poco;
    }

    static Vector2Int[] P(params int[] xy)
    {
        var r = new Vector2Int[xy.Length / 2];
        for (int i = 0; i < r.Length; i++) r[i] = new Vector2Int(xy[2 * i], xy[2 * i + 1]);
        return r;
    }

    // Casa (x, y) com y = 0 embaixo. O primeiro ponto de cada caminho fica em
    // x = -1, fora da tela: e de la que os inimigos entram.
    // O poco ocupa 2x2 casas; a posicao dada e a casa de baixo a esquerda.
    static readonly Nivel[] NIVEIS =
    {
        // Primavera: um caminho em Z, facil de ler. 6 canteiros.
        new Nivel { id = 1, nome = "Primavera", estacao = 1, cultura = "tile_cenoura",
            caminhos  = new[] { P(-1,5, 6,5, 6,2, 14,2) },
            canteiros = P(2,6, 4,4, 5,2, 7,4, 10,3, 11,1),
            plantacao = P(12,4, 13,4, 12,5, 13,5),
            poco = new Vector2Int(1, 1),
            cogumelos = P(9,6, 3,2, 8,1) },

        // Verao: serpentina com curvas fechadas. 8 canteiros.
        new Nivel { id = 2, nome = "Verao", estacao = 2, cultura = "tile_trigo",
            caminhos  = new[] { P(-1,2, 3,2, 3,5, 7,5, 7,2, 11,2, 11,5, 14,5) },
            canteiros = P(2,3, 4,4, 5,6, 6,3, 8,4, 9,1, 10,4, 12,4),
            plantacao = P(12,1, 13,1, 12,2, 13,2),
            poco = new Vector2Int(0, 4),
            cogumelos = P(5,1, 9,6) },

        // Outono: duas entradas que se juntam logo no comeco. 8 canteiros.
        new Nivel { id = 3, nome = "Outono", estacao = 3, cultura = "tile_abobora",
            caminhos  = new[] { P(-1,6, 3,6, 3,4, 10,4, 10,2, 14,2),
                                P(-1,1, 3,1, 3,4, 10,4, 10,2, 14,2) },
            canteiros = P(2,2, 2,5, 4,2, 5,5, 7,3, 8,5, 11,3, 12,1),
            plantacao = P(0,3, 1,3, 0,4, 1,4),
            poco = new Vector2Int(12, 5),
            cogumelos = P(6,1, 7,6) },
    };

    [MenuItem("Defesa da Colheita/Gerar niveis")]
    public static void Gerar() => GerarTodos(false);

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
            cam.orthographicSize = 270f / 64f;                    // 540px de altura / 64 PPU / 2
            cam.transform.position = new Vector3(W / 2f, H / 2f, -10f);
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 64;
            ppc.refResolutionX = 960;
            ppc.refResolutionY = 540;
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

        // props de pivo no pe apoiam na base da casa; o poco (2x2) centra entre as duas casas
        Sprite_(grupo, "Props/prop_poco_2x", new Vector3(nv.poco.x + 1f, nv.poco.y, 0f), -1);
        foreach (var c in nv.cogumelos)
            Sprite_(grupo, "Props/prop_cogumelos", new Vector3(c.x + 0.5f, c.y + 0.2f, 0f), -1);

        // cerca em cima e embaixo; pecas _2x de 64px = uma casa cada
        var cerca = new GameObject("Cerca").transform;
        cerca.SetParent(grupo);
        Vector3 baseMundo = Centro(nv.caminhos[0][nv.caminhos[0].Length - 1]);
        foreach (float y in new[] { 0.05f, H - 0.95f })
            for (int i = 0; i < W; i++)
            {
                var pos = new Vector3(0.5f + i, y, 0f);
                if (Vector2.Distance(pos, baseMundo) < 1.6f) continue;   // deixa espaco para o celeiro
                Sprite_(cerca, "Cerca/cerca_lado_" + (i % 3 + 1) + "_2x", pos, -1);
            }
    }

    static void CriarBase(Nivel nv, Transform pai)
    {
        // Arte do celeiro ainda nao existe. O gatilho ja fica no lugar certo:
        // e ele que vai detectar o inimigo chegando.
        var go = new GameObject("Base");
        go.transform.SetParent(pai);
        go.transform.position = Centro(nv.caminhos[0][nv.caminhos[0].Length - 1]);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;
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
