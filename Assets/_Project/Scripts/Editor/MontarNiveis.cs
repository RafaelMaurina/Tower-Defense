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

    // 30 x 17 casas de 64px: camera 1:1 em 1920x1080.
    const int W = 30, H = 17;
    const int REF_X = 1920, REF_Y = 1080;

    // Ordem de desenho: chao 0, coisas deitadas no chao 1, objetos em pe 100+.
    const int ORDEM_CHAO = 0, ORDEM_DECALQUE = 1;

    class Nivel
    {
        public int id; public string nome; public int estacao; public string cultura;
        public Vector2Int[][] caminhos;
        public Vector2Int[] canteiros, plantacao, cogumelos, milho, macieiras, laranjeiras;
        public Vector2Int galinheiro, galinha, galo;
        public Vector2 pasto;     // centro do cantinho das vacas (feno, 2 vacas, cocho, porco e ovelha), ~4 x 2,5 casas; longe de canteiro e estrada
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
    // O poco ocupa 2x2 casas; a posicao dada e a casa de baixo a esquerda.
    // As linhas y = 0 e y = 16 sao da cerca.
    // A HUD cobre o topo: nada de canteiro nem enfeite em y >= 14 (nem em y >= 13 com x >= 23).
    // A plantacao sao 2 blocos de 2x2 casas lado a lado (4x2 canteiros de 1 casa); a posicao e a casa de baixo a esquerda de cada bloco.
    // Deixe 2 casas de grama entre ela e a estrada: a grama em volta ganha borda de terra.
    static readonly Nivel[] NIVEIS =
    {
        // Primavera: zigue-zague com 6 curvas. 10 canteiros.
        new Nivel { id = 1, nome = "Primavera", estacao = 1, cultura = "tile_cenoura",
            caminhos  = new[] { P(-1,12, 8,12, 8,5, 16,5, 16,11, 23,11, 23,5, 25,5) },
            celeiro   = new RectInt(26, 4, 4, 4),
            canteiros = P(4,13, 7,10, 10,6, 12,4, 14,6, 17,9, 19,12, 20,10, 22,7, 24,2),
            plantacao = P(2,2, 4,2),
            poco = new Vector2Int(11, 8),
            cogumelos = P(7,3, 18,2, 13,11, 28,10),
            milho     = P(26,1, 27,1, 28,1, 29,1),
            macieiras = P(0,8), laranjeiras = P(19,6),
            galinheiro = new Vector2Int(13,1), galinha = new Vector2Int(11,1), galo = new Vector2Int(16,2),
            pasto = new Vector2(4.9f, 7.6f) },

        // Verao: serpentina longa. 12 canteiros.
        new Nivel { id = 2, nome = "Verao", estacao = 2, cultura = "tile_trigo",
            caminhos  = new[] { P(-1,3, 5,3, 5,13, 11,13, 11,4, 17,4, 17,13, 23,13, 23,8, 25,8) },
            celeiro   = new RectInt(26, 7, 4, 4),
            canteiros = P(3,4, 6,7, 6,11, 8,11, 10,10, 12,8, 14,5, 16,10, 18,7, 20,11, 22,10, 24,5),
            plantacao = P(20,1, 22,1),
            poco = new Vector2Int(13, 9),
            cogumelos = P(9,6, 8,1, 27,12, 14,12),
            milho     = P(25,1, 26,1, 27,1, 28,1),
            macieiras = P(1,10), laranjeiras = P(27,3),
            galinheiro = new Vector2Int(13,1), galinha = new Vector2Int(12,2), galo = new Vector2Int(15,1),
            pasto = new Vector2(2.1f, 7.2f) },

        // Outono: duas entradas que se juntam em (7,8). 12 canteiros.
        new Nivel { id = 3, nome = "Outono", estacao = 3, cultura = "tile_abobora",
            caminhos  = new[] { P(-1,13, 7,13, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9),
                                P(-1,3, 7,3, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9) },
            celeiro   = new RectInt(26, 8, 4, 4),
            canteiros = P(5,12, 5,4, 8,11, 6,7, 10,9, 12,7, 14,5, 16,6, 18,5, 20,3, 21,7, 23,11),
            plantacao = P(1,6, 3,6),
            poco = new Vector2Int(10, 11),
            cogumelos = P(9,13, 13,12, 27,2, 12,2),
            milho     = P(22,1, 23,1, 24,1, 25,1),
            macieiras = P(1,9), laranjeiras = P(26,4),
            galinheiro = new Vector2Int(9,1), galinha = new Vector2Int(8,2), galo = new Vector2Int(13,1),
            pasto = new Vector2(18.5f, 10.5f) },
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
        AtualizarBuild();
        Debug.Log(feitos + " nivel(is) gerado(s). Cenas em " + CENAS + ", prefabs em " + PREFABS + ".");
    }

    /// As cenas dos niveis sao geradas em cada computador, entao o GUID muda de
    /// um PC para outro. Aqui o Build Settings e refeito pelo caminho: Menu
    /// primeiro e depois os tres niveis.
    public static void AtualizarBuild()
    {
        var lista = new List<EditorBuildSettingsScene>();
        string menu = CENAS + "/Menu.unity";
        if (AssetDatabase.LoadAssetAtPath<Object>(menu) != null) lista.Add(new EditorBuildSettingsScene(menu, true));
        foreach (var nv in NIVEIS)
        {
            string cena = CENAS + "/Nivel_" + nv.id + "_" + nv.nome + ".unity";
            if (AssetDatabase.LoadAssetAtPath<Object>(cena) != null) lista.Add(new EditorBuildSettingsScene(cena, true));
        }

        var atual = EditorBuildSettings.scenes;
        bool igual = atual.Length == lista.Count;
        for (int i = 0; igual && i < atual.Length; i++)
            igual = atual[i].path == lista[i].path && atual[i].guid == lista[i].guid && atual[i].enabled;
        if (!igual) EditorBuildSettings.scenes = lista.ToArray();
    }

    /// Atualiza dados dos prefabs locais gerados sem apagar a geometria dos niveis.
    public static void AtualizarCatalogos()
    {
        var towers = CarregarAssets<TowerData>("Assets/_Project/Data/Towers");
        var waves = CarregarAssets<WaveData>("Assets/_Project/Data/Waves");
        foreach (var nv in NIVEIS)
        {
            string path = PREFABS + "/Nivel_" + nv.id + "_" + nv.nome + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var bootstrap = root.GetComponentInChildren<GameBootstrap>(true);
                if (bootstrap == null || !bootstrap.NeedsCatalogUpdate(towers, waves)) continue;
                bootstrap.Configure(towers, waves);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Catalogo de torres e ondas atualizado em " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
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
        var bootstrap = raiz.AddComponent<GameBootstrap>();
        bootstrap.Configure(
            CarregarAssets<TowerData>("Assets/_Project/Data/Towers"),
            CarregarAssets<WaveData>("Assets/_Project/Data/Waves"));

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
            // Grao do jogo: 1 pixel de desenho = 2 pixels de tela (PPU 32, base 960x540).
            // O chao ainda e PPU 64 (pixel mais fino) ate ser redesenhado em 32x32.
            ppc.assetsPPU = 32;
            ppc.refResolutionX = REF_X / 2;
            ppc.refResolutionY = REF_Y / 2;
            ppc.upscaleRT = false;
        }

        // Para dar Play direto no nivel sem passar pelo menu. Vindo do menu, o
        // GameManager de la ja existe e este se apaga sozinho no Awake.
        new GameObject("GameManager").AddComponent<GameManager>();

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

    /// Casas ocupadas pela plantacao (blocos de 2x2). So entram na conta da borda de grama:
    /// o inimigo nao anda ali, entao nao viram estrada.
    static HashSet<Vector2Int> CelulasDaPlantacao(Nivel nv)
    {
        var r = new HashSet<Vector2Int>();
        foreach (var c in nv.plantacao)
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                    r.Add(new Vector2Int(c.x + dx, c.y + dy));
        return r;
    }

    /// Qual borda de grama usar: a terra fica do lado que encosta na estrada (ou na plantacao).
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

    // Sorteio fixo pela posicao (o nivel sai igual toda vez que e gerado).
    static string Variacao(string grama, int x, int y)
    {
        int k = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
        string sufixo = (k % 3) == 1 ? "_v2" : (k % 3) == 2 ? "_v3" : "";
        return sufixo != "" && System.IO.File.Exists(SPR + grama + sufixo + ".png") ? sufixo : "";
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
        // a grama vizinha da plantacao ganha a mesma borda de terra da estrada, virada para ela
        var terra = new HashSet<Vector2Int>(estrada);
        terra.UnionWith(CelulasDaPlantacao(nv));
        string g = "Tiles/Grama/grama_" + nv.estacao;
        // Uma casa de sobra em volta: a camera mostra 8,4 casas de altura e o
        // mapa tem 8, entao sem isso aparece a cor de fundo em cima e embaixo.
        // A casa x = -1 do caminho tambem vira estrada, e o inimigo ja entra nela.
        for (int x = -1; x < W + 1; x++)
            for (int y = -1; y < H + 1; y++)
            {
                var c = new Vector3Int(x, y, 0);
                string borda = Borda(x, y, terra);
                // grama lisa sorteia entre as variacoes (grama_N, _v2, _v3) para nao formar grade
                if (borda == "") borda = Variacao(g, x, y);
                mapa.SetTile(c, estrada.Contains(new Vector2Int(x, y)) ? pista : ObterTile(g + borda));
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
            // terra de 1 casa (PPU 64) com o carrinho e as ferramentas por cima em 3/4 do tamanho (PPU 42,67),
            // que passam da casa; o BuildPlot esconde as ferramentas quando a torre e construida
            var go = Sprite_(grupo, "Tiles/canteiro_terra", Centro(nv.canteiros[i]), ORDEM_DECALQUE);
            go.name = "Canteiro_" + (i + 1).ToString("00");
            var ferramentas = Sprite_(go.transform, "Tiles/canteiro_ferramentas", Centro(nv.canteiros[i]) + new Vector3(0f, .25f, 0f), -1);
            // colisor para o clique de construir, que vem depois
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<BuildPlot>().ferramentas = ferramentas;
        }
    }

    static void CriarDecoracao(Nivel nv, Transform pai)
    {
        var grupo = new GameObject("Decoracao").transform;
        grupo.SetParent(pai);

        // plantacao em canteiros de 1 casa: cada entrada de 'plantacao' e a casa de baixo a esquerda de um
        // bloco de 2x2 canteiros; 2 blocos lado a lado = 8 canteiros. As folhas balancam de leve (PixelSway).
        // Cenoura: desenho de 32x32 que ja inclui o canteiro (a terra fica parada).
        // Trigo e abobora: canteiro de terra e, por cima, a planta no tamanho original (PPU 32), que passa um
        // pouco da casa; as de cima desenham por tras das de baixo.
        var terra = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Tiles/canteiro_horta.png");
        foreach (var c in nv.plantacao)
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                {
                    var cel = new Vector2Int(c.x + dx, c.y + dy);
                    if (nv.cultura == "tile_cenoura")
                        Sprite_(grupo, "Tiles/tile_cenoura", Centro(cel), ORDEM_DECALQUE).AddComponent<PixelSway>().background = terra;
                    else
                    {
                        Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
                        Sprite_(grupo, "Tiles/" + nv.cultura.Replace("tile_", "planta_"), Centro(cel), 2 + (H - 1 - cel.y))
                            .AddComponent<PixelSway>();
                    }
                }

        // props de pivo no pe apoiam na base da casa; o poco (2x2, PPU 32) centra entre as duas casas
        Sprite_(grupo, "Props/prop_poco", new Vector3(nv.poco.x + 1f, nv.poco.y, 0f), -1);
        foreach (var c in nv.cogumelos)
            Sprite_(grupo, "Props/prop_cogumelos", new Vector3(c.x + 0.5f, c.y + 0.2f, 0f), -1);
        // carreira de pes de milho num canto livre, so de enfeite
        foreach (var c in nv.milho)
        {
            var vento = Sprite_(grupo, "Props/prop_milho", new Vector3(c.x + 0.5f, c.y, 0f), -1).AddComponent<PixelSway>();
            vento.amplitude = 2;
            vento.fixedFraction = .25f;
            vento.frameTime = .28f;
        }

        // galinheiro (PPU 21,33) ocupa uns 2,3 x 2,6 casas, pe no meio de c.x + 1; galinha e galo, uma casa.
        // arvores: desenho de 64 px em PPU 16, uns 2,8 casas de copa por 3,6 de altura, pe no meio de c.x + 1
        foreach (var c in nv.macieiras)
            Sprite_(grupo, "Props/prop_arvore_maca", new Vector3(c.x + 1.4f, c.y, 0f), -1).AddComponent<FallingLeaves>();
        foreach (var c in nv.laranjeiras)
            Sprite_(grupo, "Props/prop_arvore_laranja", new Vector3(c.x + 1f, c.y, 0f), -1).AddComponent<FallingLeaves>();
        Sprite_(grupo, "Props/prop_galinheiro", new Vector3(nv.galinheiro.x + 1f, nv.galinheiro.y, 0f), -1);
        // galinha e galo ciscam em volta de onde nascem; a galinha olha para a esquerda no desenho
        Som_(Ave_(grupo, "Props/prop_galinha", new Vector3(nv.galinha.x + 0.5f, nv.galinha.y + 0.1f, 0f), true).gameObject, "galinha", "COCO");
        Som_(Ave_(grupo, "Props/prop_galo", new Vector3(nv.galo.x + 0.5f, nv.galo.y + 0.1f, 0f), false).gameObject, "galo", "COCORICO");

        // cantinho das vacas (bichos em PPU 21,33, o mesmo grao do gato e da galinha): 2 fardos de feno a esquerda,
        // a marrom passeando e pastando, a creme dormindo, o cocho a direita, porco e ovelha atras.
        // Ocupa umas 4 casas de largura por 2,5 de altura em volta de nv.pasto.
        Vector3 pasto = nv.pasto;
        foreach (var f in new[] { new Vector2(-1.4f, .4f), new Vector2(-1.1f, -.15f) })
            Sprite_(grupo, "Props/prop_feno", pasto + (Vector3)f, -1);
        var vaca = Sprite_(grupo, "Props/prop_vaca_marrom_em_pe", pasto + new Vector3(-.25f, .25f, 0f), -1).AddComponent<AnimalWander>();
        vaca.walkFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
            vaca.walkFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/prop_vaca_marrom_anda_" + (i + 1) + ".png");
        vaca.grazeFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/prop_vaca_marrom_pasta.png");
        vaca.chewFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/prop_vaca_marrom_pasta_2.png");
        vaca.facesLeft = true;
        vaca.radius = .3f;
        vaca.speed = .35f;
        Som_(vaca.gameObject, "vaca", "MUU");
        var dorme = Sprite_(grupo, "Props/prop_vaca_creme_deitada", pasto + new Vector3(.75f, -.45f, 0f), -1);
        Som_(dorme, "vaca", "MUU");
        var zz = Sprite_(dorme.transform, "Props/fx_zz", dorme.transform.position + new Vector3(-.15f, .7f, 0f), -1);
        zz.GetComponent<SpriteRenderer>().sortingOrder = dorme.GetComponent<SpriteRenderer>().sortingOrder + 1;
        zz.AddComponent<SleepZ>();
        Sprite_(grupo, "Props/prop_cocho_agua", pasto + new Vector3(1.5f, .1f, 0f), -1);
        // porco e ovelha pastam por perto (andam pouco, para nao pisar na estrada)
        var porco = Ave_(grupo, "Props/prop_porco", pasto + new Vector3(-.7f, 1.15f, 0f), true);
        porco.radius = .22f;
        Som_(porco.gameObject, "porco", "OINK");
        var ovelha = Ave_(grupo, "Props/prop_ovelha", pasto + new Vector3(.55f, 1.05f, 0f), true);
        ovelha.radius = .22f;
        Som_(ovelha.gameObject, "ovelha", "MEE");

        // gato brincando com o novelo na frente da porta do celeiro; o novelo nasce a esquerda dele
        Vector3 gatoPos = new Vector3(nv.celeiro.x + 1.3f, nv.celeiro.y - .5f, 0f);
        var gato = Sprite_(grupo, "Props/prop_gato_bicando", gatoPos, -1).AddComponent<CatYarnPlay>();
        var novelo = Sprite_(grupo, "Props/prop_novelo_1", gatoPos + new Vector3(-14f / 21.333f, 0f, 0f), -1);   // 14 pixels do gato
        Som_(gato.gameObject, "gato", "MEOW");
        gato.sit = Bicho_("prop_gato_bicando");
        gato.crouch = Bicho_("prop_gato_agacha");
        gato.crouch2 = Bicho_("prop_gato_agacha_2");
        gato.pounce = Bicho_("prop_gato_pula");
        gato.pat1 = Bicho_("prop_gato_tapa_1");
        gato.pat2 = Bicho_("prop_gato_tapa_2");
        gato.lie = Bicho_("prop_gato_brinca");
        gato.walkFrames = new[] { Bicho_("prop_gato"), Bicho_("prop_gato_passo") };
        gato.yarn = novelo.GetComponent<SpriteRenderer>();
        gato.yarnFrames = new[] { Bicho_("prop_novelo_1"), Bicho_("prop_novelo_2") };
        gato.facesLeft = true;
        gato.radius = .8f;

        // cerquinha so no lado de tras (y maior) da plantacao: pecas _3x de 1,5 casa, centradas na largura dela
        int px0 = int.MaxValue, px1 = int.MinValue, py1 = int.MinValue;
        foreach (var c in nv.plantacao)
        {
            px0 = Mathf.Min(px0, c.x);
            px1 = Mathf.Max(px1, c.x + 2);
            py1 = Mathf.Max(py1, c.y + 2);
        }
        int pecas = Mathf.CeilToInt((px1 - px0) / 1.5f);
        float inicio = (px0 + px1) / 2f - pecas * 0.75f;
        var cercaPlantacao = new GameObject("CercaPlantacao").transform;
        cercaPlantacao.SetParent(grupo);
        for (int i = 0; i < pecas; i++)
            Sprite_(cercaPlantacao, "Cerca/cerca_lado_" + (i % 3 + 1) + "_3x", new Vector3(inicio + 0.75f + i * 1.5f, py1 + 0.1f, 0f), -1);

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

        // Desenhado em 64x64 com PPU 10,67 (6x): uns 5,4 casas de largura, encostado na borda direita e no fim do
        // caminho para os inimigos "entrarem" no celeiro.
        // O nome "Celeiro" e usado pelas luzes da noite (NightLights).
        var celeiro = Sprite_(pai, "Props/prop_celeiro",
            new Vector3(nv.celeiro.x + 1.28f, nv.celeiro.y, 0f), -1);
        celeiro.name = "Celeiro";
        celeiro.AddComponent<BarnHitFx>();   // treme e solta lascas quando a base perde vida
    }

    // --------------------------------------------------------- utilitarios

    static T[] CarregarAssets<T>(string pasta) where T : UnityEngine.Object
    {
        if (!AssetDatabase.IsValidFolder(pasta)) return new T[0];
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { pasta });
        var assets = new List<T>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) assets.Add(asset);
        }
        return assets.ToArray();
    }
    static Vector3 Centro(Vector2Int c) => new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);

    /// ordem &lt; 0 = objeto em pe, ordenado pela altura na tela.
    static Sprite Bicho_(string nome) => AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/" + nome + ".png");

    /// Clicar no bicho mostra a palavra e toca Resources/sons/<id>, se existir (ver AnimalSound).
    static void Som_(GameObject bicho, string id, string palavra)
    {
        var som = bicho.AddComponent<AnimalSound>();
        som.id = id;
        som.word = palavra;
    }

    static ChickenWander Ave_(Transform pai, string caminho, Vector3 pos, bool olhaParaEsquerda)
    {
        var ave = Sprite_(pai, caminho, pos, -1).AddComponent<ChickenWander>();
        ave.stepFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminho + "_passo.png");
        ave.peckFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminho + "_bicando.png");
        ave.facesLeft = olhaParaEsquerda;
        return ave;
    }

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
