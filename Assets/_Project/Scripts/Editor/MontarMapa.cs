using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

/// Monta um pedaco de mapa jogavel usando Tilemap de verdade, para servir de
/// referencia de como o cenario e construido. Tudo fica dentro de "DEMO Mapa";
/// apagar esse objeto desfaz.
public static class MontarMapa
{
    const string SPR   = "Assets/_Project/Art/Sprites/";
    const string TILES = "Assets/_Project/Art/Tiles";
    const string RAIZ  = "DEMO Mapa";

    // Grama 1 = primavera, 2 = verao, 3 = outono. Trocar aqui muda a estacao inteira.
    const int ESTACAO = 1;

    const int X0 = -7, X1 = 7, Y0 = -4, Y1 = 4;   // 15 x 9 casas
    const int ESTRADA_BAIXO = -1, ESTRADA_TOPO = 1;

    [MenuItem("Defesa da Colheita/Montar pedaco de mapa")]
    public static void Montar()
    {
        var antigo = GameObject.Find(RAIZ);
        if (antigo != null) Object.DestroyImmediate(antigo);

        if (!AssetDatabase.IsValidFolder(TILES))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "Tiles");

        string g = "Tiles/Grama/grama_" + ESTACAO;
        var grama      = Tile_(g);
        var bordaTopo  = Tile_(g + "_topo");
        var bordaBaixo = Tile_(g + "_base");
        var bordaEsq   = Tile_(g + "_esq");
        var bordaDir   = Tile_(g + "_dir");
        var cantoTE    = Tile_(g + "_canto_topo_esq");
        var cantoTD    = Tile_(g + "_canto_topo_dir");
        var cantoBE    = Tile_(g + "_canto_base_esq");
        var cantoBD    = Tile_(g + "_canto_base_dir");

        var estrada    = Tile_("Tiles/Estrada/estrada");
        var estradaDec = Tile_("Tiles/Estrada/estrada_grama_" + ESTACAO + "_dec");
        var bordaSul   = Tile_("Tiles/Estrada/estrada_grama_" + ESTACAO);
        // A franja de grama so existe embaixo. Para a borda norte da estrada,
        // espelho o mesmo tile no eixo Y em vez de pedir arte nova.
        var bordaNorte = Tile_("Tiles/Estrada/estrada_grama_" + ESTACAO, "_flipY",
                               Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f)));

        var raiz = new GameObject(RAIZ);
        var grid = new GameObject("Grid").AddComponent<Grid>();
        grid.transform.SetParent(raiz.transform);
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var chaoGo = new GameObject("Chao");
        chaoGo.transform.SetParent(grid.transform);
        var chao = chaoGo.AddComponent<Tilemap>();
        chaoGo.AddComponent<TilemapRenderer>().sortingOrder = 0;

        for (int x = X0; x <= X1; x++)
        {
            for (int y = Y0; y <= Y1; y++)
            {
                TileBase t;
                if (y == ESTRADA_TOPO)        t = bordaNorte;
                else if (y == ESTRADA_BAIXO)  t = bordaSul;
                else if (y == 0)              t = (x % 5 == 0) ? estradaDec : estrada;
                else if (x == X0 && y == Y1)  t = cantoTE;
                else if (x == X1 && y == Y1)  t = cantoTD;
                else if (x == X0 && y == Y0)  t = cantoBE;
                else if (x == X1 && y == Y0)  t = cantoBD;
                else if (y == Y1)             t = bordaTopo;
                else if (y == Y0)             t = bordaBaixo;
                else if (x == X0)             t = bordaEsq;
                else if (x == X1)             t = bordaDir;
                else                          t = grama;
                chao.SetTile(new Vector3Int(x, y, 0), t);
            }
        }

        var props = new GameObject("Props");
        props.transform.SetParent(raiz.transform);

        // Canteiros: pivo no centro, entao vao no centro da casa (x+0.5, y+0.5)
        foreach (var c in new[] { new Vector2Int(-4, 2), new Vector2Int(0, 2),
                                  new Vector2Int(4, 2), new Vector2Int(-2, -3),
                                  new Vector2Int(3, -3) })
            Prop(props, "Tiles/tile_canteiro", c.x + 0.5f, c.y + 0.5f);

        // Props com pivo no pe: apoiam na base da casa (x+0.5, y)
        Prop(props, "Props/prop_poco", -6.5f, -3f);
        Prop(props, "Props/prop_cogumelos", 5.5f, -3f);
        Prop(props, "Props/prop_cogumelos", -3.5f, 3f);

        // Cerca: telas de 32px = meia unidade, entao o passo e 0.5
        var cerca = new GameObject("Cerca");
        cerca.transform.SetParent(props.transform);
        string[] pecas = { "Cerca/cerca_lado_1", "Cerca/cerca_lado_2", "Cerca/cerca_lado_3" };
        int i = 0;
        for (float x = -6f; x <= 6f; x += 0.5f, i++)
        {
            bool portao = Mathf.Abs(x - 1.5f) < 0.01f || Mathf.Abs(x - 2f) < 0.01f;
            Prop(cerca, portao ? "Cerca/portao_1" : pecas[i % pecas.Length], x, 3.5f);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 270f / 64f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        Selection.activeGameObject = raiz;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("Mapa montado: " + (X1 - X0 + 1) + "x" + (Y1 - Y0 + 1)
                  + " casas, estacao " + ESTACAO + ". Apague \"" + RAIZ + "\" para desfazer.");
    }

    /// Cria (ou reaproveita) um Tile asset a partir de um sprite.
    static Tile Tile_(string caminhoSprite, string sufixo = "", Matrix4x4? matriz = null)
    {
        string nome = System.IO.Path.GetFileNameWithoutExtension(caminhoSprite) + sufixo;
        string destino = TILES + "/" + nome + ".asset";

        var existente = AssetDatabase.LoadAssetAtPath<Tile>(destino);
        if (existente != null) return existente;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminhoSprite + ".png");
        if (sprite == null) { Debug.LogWarning("Sprite nao encontrado: " + caminhoSprite); return null; }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        if (matriz.HasValue)
        {
            tile.transform = matriz.Value;
            tile.flags = TileFlags.LockTransform;   // sem isso a matriz e ignorada
        }
        AssetDatabase.CreateAsset(tile, destino);
        AssetDatabase.SaveAssets();
        return tile;
    }

    static void Prop(GameObject pai, string caminho, float x, float y)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminho + ".png");
        if (sprite == null) { Debug.LogWarning("Sprite nao encontrado: " + caminho); return; }

        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(caminho));
        go.transform.SetParent(pai.transform);
        go.transform.position = new Vector3(x, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        // quem esta mais embaixo na tela desenha na frente
        sr.sortingOrder = 10 + Mathf.RoundToInt(-y * 10f);
    }
}
