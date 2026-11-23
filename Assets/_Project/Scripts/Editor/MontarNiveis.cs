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
        public Vector2 casa;      // casa da fazenda, no alto do cenario (pe no meio da base); fora do campus
        public RectInt celeiro;   // area reservada para o celeiro, encostada no fim do caminho
        public Vector4[] cercas;   // trechos de cerca soltos no cenario: (x0, y0, x1, y1), reto na horizontal ou na vertical
        public Vector2[] ovelhas;  // ovelhas extras pastando (centro)
        public Vector2[] galinhas; // galinhas extras ciscando
        public Vector2[] ovelhasDireita; // ovelhas extras virando para a direita
        public Vector2 fazendeiro; // onde o fazendeiro capina (pe); zero = ao lado da plantacao
        public Vector2Int[] cenourasNaTerra; // canteiros (de 1 casa) do bloco so de terra que levam cenoura; o resto fica de terra limpa
        public Vector4[] cercasFinas;      // cercas descendo (peca de frente inteira de 3 casas, presa pelo topo): (x, -, -, y do topo)
        public int linhaOculta = -1;       // fileira (y) de canteiros da plantacao que nao entra: a do alto, sem cenoura
        public Vector2[] baldes;           // baldes soltos (pe)
        public Vector2[] cochos;           // cochos de agua soltos (pe)
        public Vector2[] macieirasSoltas;  // macieiras em posicao exata (pe), para o pomar nao ficar na regua
        public Vector2[] laranjeirasSoltas; // laranjeiras em posicao exata (pe)
        public Vector2[] postesCurtos;     // meia peca de frente (poste liso de 1,5 casa): (x, y do pe)
        public Vector2[] fazendeiroParado; // fazendeiro sem enxada, parado (pe)
        public Vector4[] cercasAtras;      // cercas horizontais desenhadas atras das plantacoes (o trigo passa na frente)
        public Vector4 areaGalinhas;       // (x min, y min, x max, y max) onde galinha e galo ciscam; zero = livre
        public Vector2Int[] plantioExtra;  // canteiros (de 1 casa) a mais da cultura da fase, fora dos blocos 2x2
        public Vector2[] fenos, porcos;    // fardos de feno e porcos soltos (pe)
        public Vector2[] porcosDireita;    // porcos soltos que comecam virados para a direita
        public Vector2Int[] plantioVazio;  // canteiros (de 1 casa) so de terra arada, sem planta
        public Vector4[] cercasPortao;     // cercas horizontais (x0, y, x1, y) com um portao no meio
        public bool fazendeiroEspelhado;   // o fazendeiro capina para a esquerda (desenho espelhado)
        public Vector4[] cercasDoisPortoes; // cercas horizontais (x0, y, x1, y) com dois portoes juntos no meio
        public Vector2[] vacasPretas;      // vacas pretas passeando e pastando (pe)
        public Vector2[] vacasPretasDormindo; // vacas pretas deitadas dormindo (pe)
        public Vector2[] vacasClaras;      // vacas claras (creme) de pe, acordadas, passeando e pastando (pe)
        public Vector4 areaVacas;          // (x min, y min, x max, y max) onde as vacas andam (dentro do cercado); zero = livre
        public Vector4[] bloqueiosVacas;   // retangulos (x, y, largura, altura) que as vacas nao atravessam (tronco de arvore)
        public bool semOvelhaNoPasto;      // o cantinho das vacas nao leva a ovelha
        public bool semPorcoNoPasto;       // o cantinho das vacas nao leva o porco
        public bool semFazendeiro;         // a fase nao tem o fazendeiro animado capinando
        public bool semCercado;            // a plantacao nao ganha o cercado automatico (a fase desenha as cercas em "cercas")
        public float portaoAberto = -1f; // x onde o lado de baixo do cercado fica sem peca (portao aberto); -1 = nenhum
        public Vector2Int[] plantacaoTerra, plantacaoAbobora; // blocos 2x2: so terra (onde o fazendeiro capina) e de abobora
        public Vector2[] vacas;    // vacas marrons extras passeando e pastando; a ultima da lista e a creme dormindo
        public Vector2Int milharal; // de x a y (casas): milharal em 2 fileiras desencontradas na frente da cerca de cima; 0,0 = sem
        public bool campus;       // fase bonus: asfalto com meio-fio, postes com luz, sem plantacao, galinheiro nem milho
        public Vector2Int[] postes;   // so no campus: casa de grama de cada poste; o pe fica a 1/4 de casa da base dela
        public Vector3[] onibus;      // so no campus: onibus estacionados (x, y do pe, 1 = virado para a direita), colados no asfalto
        public Vector4[] carros;      // so no campus: carros estacionados (x, y do pe, cor 0 a 5, 1 = virado para a direita), colados no asfalto
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
    // Arvores de fruta tem uns 3,6 casas de altura a partir do pe (y): pe em y <= 10 (y <= 9 com x >= 23).
    // A plantacao sao 2 blocos de 2x2 casas lado a lado (4x2 canteiros de 1 casa); a posicao e a casa de baixo a esquerda de cada bloco.
    // Deixe 2 casas de grama entre ela e a estrada: a grama em volta ganha borda de terra.
    static readonly Nivel[] NIVEIS =
    {
        // Primavera: zigue-zague com 6 curvas. 9 canteiros.
        new Nivel { id = 1, nome = "Primavera", estacao = 1, cultura = "tile_cenoura",
            caminhos  = new[] { P(-1,12, 8,12, 8,5, 16,5, 16,11, 23,11, 23,5, 25,5) },
            celeiro   = new RectInt(26, 4, 4, 4),
            canteiros = P(4,13, 7,10, 10,6, 12,4, 14,6, 17,9, 19,12, 20,10, 22,7),
            plantacao = P(1,4, 3,4, 5,4, 5,2), plantacaoTerra = P(3,2), cenourasNaTerra = P(3,3, 4,3, 4,2), fazendeiro = new Vector2(2.4f, 2.6f), portaoAberto = 2.4f, linhaOculta = 5,
            poco = new Vector2Int(11, 8), casa = new Vector2(26f, 12.4f),
            cogumelos = P(9,3, 18,2, 13,11, 28,10),
            milho     = P(), milharal = new Vector2Int(10, 21),
            macieiras = P(0,8, 7,13), laranjeiras = P(18,6, 20,1),
            galinheiro = new Vector2Int(13,1), galinha = new Vector2Int(11,1), galo = new Vector2Int(16,2),
            pasto = new Vector2(4.9f, 7.6f),
            cercas = new[] { new Vector4(.5f, 11.5f, 6.5f, 11.5f), new Vector4(9.3f, 8f, 9.3f, 12.5f), new Vector4(15.6f, 12.15f, 18.6f, 12.15f),
                             new Vector4(25.13f, 7.5f, 25.13f, 11.7f), new Vector4(24.9f, 11.4f, 30f, 11.4f) },
            plantacaoAbobora = P(26,9, 28,9, 26,7, 28,7),
            cercasFinas = new[] { new Vector4(15.83f, 0f, 0f, 12.5f) },
            galinhas = new[] { new Vector2(12.5f, 1.8f) },
            ovelhas = new[] { new Vector2(23.4f, 2f), new Vector2(25.2f, 2.1f) },
            ovelhasDireita = new[] { new Vector2(24.3f, 1.2f) },
            cochos = new[] { new Vector2(22.5f, 1f) } },

        // Verao: serpentina longa. 11 canteiros.
        new Nivel { id = 2, nome = "Verao", estacao = 3, cultura = "tile_trigo",
            caminhos  = new[] { P(-1,3, 5,3, 5,13, 11,13, 11,4, 17,4, 17,13, 23,13, 23,8, 25,8) },
            celeiro   = new RectInt(26, 7, 4, 4),
            canteiros = P(3,4, 6,7, 6,11, 8,11, 10,10, 12,8, 14,5, 16,10, 18,7, 20,11, 22,10),
            // trigo: um campo embaixo a esquerda e outro na frente do celeiro (4 + 10 blocos de 2x2)
            plantacao = P(0,1, 2,1, 4,1, 6,1, 20,1, 22,1, 24,1, 26,1, 28,1, 20,3, 22,3, 24,3, 26,3, 28,3),
            semCercado = true, semFazendeiro = true,
            poco = new Vector2Int(13, 10), casa = new Vector2(1.4f, 13.4f),
            cogumelos = P(9,6, 8,1, 24,14, 14,12),
            milho     = P(25,12, 26,12, 27,12, 28,12, 29,12, 25,13, 26,13, 27,13, 28,13, 29,13, 25,14, 26,14, 27,14, 28,14, 29,14, 25,15, 26,15, 27,15, 28,15, 29,15),
            macieiras = P(1,10), laranjeiras = P(20,6),
            // pomarzinho no alto: alguns pixels (1/32 de casa) para cima e para baixo entre elas
            macieirasSoltas = new[] { new Vector2(6.4f, 14.19f), new Vector2(8.4f, 13.84f), new Vector2(10.4f, 14.22f), new Vector2(12.8f, 13.76f), new Vector2(14.6f, 14.24f) },
            fazendeiroParado = new[] { new Vector2(4.1f, 12.9f) },
            galinhas = new[] { new Vector2(8.6f, 3.6f) },
            galinheiro = new Vector2Int(8,5), galinha = new Vector2Int(7,4), galo = new Vector2Int(9,4), areaGalinhas = new Vector4(6.9f, 3.4f, 10.4f, 4.9f),
            pasto = new Vector2(2.1f, 7.2f),
            cercas = new[] {
                new Vector4(4.6f, 6f, 4.6f, 10.3f), new Vector4(6.56f, 3.2f, 6.56f, 6.2f), new Vector4(0f, 4.6f, 2.6f, 4.6f),
                new Vector4(20.3f, 14.1f, 23.8f, 14.1f),
                new Vector4(10.86f, 3.2f, 19.86f, 3.2f) },
            cercasAtras = new[] { new Vector4(19.4f, 4.6f, 30f, 4.6f) },
            postesCurtos = new[] { new Vector2(19.63f, 3.6f) } },

        // Outono: duas entradas que se juntam em (7,8). 12 canteiros.
        new Nivel { id = 3, nome = "Outono", estacao = 2, cultura = "tile_abobora",
            caminhos  = new[] { P(-1,13, 7,13, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9),
                                P(-1,3, 7,3, 7,8, 15,8, 15,4, 22,4, 22,9, 25,9) },
            celeiro   = new RectInt(26, 8, 4, 4),
            canteiros = P(5,12, 5,2, 8,11, 6,7, 10,9, 12,7, 14,5, 16,6, 18,5, 20,3, 21,7, 23,11),
            // abobora maior: 2 blocos e mais 6 canteiros (coluna da esquerda e fileira de baixo), num cercado com portao
            plantacao = P(1,6, 3,6), plantioExtra = P(0,6, 0,7, 0,5, 1,5, 2,5, 3,5), plantioVazio = P(4,5),
            semCercado = true, fazendeiro = new Vector2(5.05f, 5.2f), fazendeiroEspelhado = true, semPorcoNoPasto = true, semOvelhaNoPasto = true,
            poco = new Vector2Int(18, 7), casa = new Vector2(26f, 12.4f),
            cogumelos = P(8,14, 13,12, 7,1, 9,2),
            milho     = P(22,1, 23,1, 24,1, 25,1, 26,1, 27,1, 28,1, 29,1, 25,2, 26,2, 27,2, 28,2, 29,2, 28,3, 29,3, 28,4, 29,4),
            macieiras = P(1,9, 16,13), laranjeiras = P(24,4, 13,1),
            galinheiro = new Vector2Int(10,4), galinha = new Vector2Int(9,5), galo = new Vector2Int(12,6),
            galinhas = new[] { new Vector2(12.5f, 5f), new Vector2(11.5f, 4.2f), new Vector2(9.5f, 6.5f) },
            areaGalinhas = new Vector4(8.3f, 3.2f, 14.7f, 7.6f),
            pasto = new Vector2(15.5f, 12f),
            // cercado de grade para as vacas e os animais, no alto; as vacas e o feno ficam dentro
            vacas = new[] { new Vector2(12f, 14.3f), new Vector2(13.6f, 14.6f), new Vector2(11.8f, 11.6f) },
            porcos = new[] { new Vector2(1.9f, 1.4f), new Vector2(3.9f, 1.5f) },
            porcosDireita = new[] { new Vector2(2.9f, 1.6f) },
            cercas = new[] {
                new Vector4(0.1f, 12f, 4.6f, 12f),
                new Vector4(-0.7f, 8.2f, 5.3f, 8.2f), new Vector4(-0.5f, 4.1f, 4f, 4.1f), new Vector4(5.3f, 5.6f, 5.3f, 8.6f),
                new Vector4(10.63f, 10.6f, 10.63f, 16.2f), new Vector4(19.17f, 10.6f, 19.17f, 16.2f),
                new Vector4(16.4f, 3f, 19.4f, 3f),
                new Vector4(0.1f, 2.05f, 3.6f, 2.05f) },
            cercasDoisPortoes = new[] { new Vector4(10.4f, 10.2f, 19.4f, 10.2f) },
            vacasPretas = new[] { new Vector2(15.3f, 14.2f), new Vector2(14.6f, 13.5f) },
            areaVacas = new Vector4(11.1f, 11.1f, 18.8f, 15.3f),
            bloqueiosVacas = new[] { new Vector4(16.9f, 12.7f, 1.0f, 1.3f) },
            baldes = new[] { new Vector2(20.05f, 7.2f) },
            vacasPretasDormindo = new[] { new Vector2(18.2f, 11.4f) },
            vacasClaras = new[] { new Vector2(12.8f, 12.8f) },
            cercasPortao = new Vector4[0] },
        // Campus (bonus, #50): noite no campus. Asfalto com meio-fio, postes com luz, sem plantacao, galinheiro nem milho.
        // Canteiros a 2 casas da estrada (1 casa de grama no meio); o caminho dos inimigos e o do Joao, sem mudanca.
        new Nivel { id = 4, nome = "Campus", estacao = 3, cultura = "tile_abobora", campus = true,
            // estrada puxada para a esquerda para a faculdade (um pouco maior que o celeiro) ter espaco no fim da pista
            // cruzamento em (15,9): metade dos inimigos segue pelo alto (A) e metade desce e entra pela porta da faculdade (B)
            caminhos = new[] { P(-1,11, 4,11, 4,5, 11,5, 11,13, 15,13, 15,9, 19,9, 19,13, 21,13, 21,6, 23,6),
                               P(-1,11, 4,11, 4,5, 11,5, 11,13, 15,13, 15,9, 15,3, 26,3, 26,5) },
            celeiro = new RectInt(24,5,4,4),
            canteiros = P(3,9, 5,10, 6,6, 8,4, 10,8, 12,7, 14,11, 17,8, 22,10, 20,8, 14,5, 19,2, 23,2),
            postes = P(2,10, 5,7, 6,4, 10,4, 13,10, 16,11, 20,11, 14,8, 18,2),
            plantacao = P(), poco = new Vector2Int(17,5),
            cogumelos = P(9,2, 27,2), milho = P(),
            macieiras = P(0,5, 7,8), laranjeiras = P(2,1, 12,1),
            // florestinha em cima da faculdade em duas fileiras (pes em y 9 a 9,4 e em y 10,9 a 12,3), espacadas ao acaso e com as copas se
            // tocando; longe do canteiro (22,10), para nao cobrir a torre
            macieirasSoltas = new[] { new Vector2(24.6f, 9.05f), new Vector2(27.3f, 9.0f), new Vector2(29.5f, 9.15f),
                                      new Vector2(25.4f, 11.1f), new Vector2(28.8f, 10.9f), new Vector2(29.7f, 12.0f),
                                      new Vector2(23.9f, 12.2f), new Vector2(27.4f, 10.6f), new Vector2(25.8f, 13.0f), new Vector2(29.4f, 13.1f),
                                      new Vector2(6.8f, 12.4f) },   // a ultima, sozinha, no vazio ao lado do carro vermelho
            laranjeirasSoltas = new[] { new Vector2(26.0f, 9.4f), new Vector2(28.6f, 9.35f), new Vector2(27.1f, 11.7f), new Vector2(26.3f, 12.3f),
                                        new Vector2(28.2f, 12.8f), new Vector2(27.7f, 13.3f) },
            // estacionados na grama colada ao asfalto (so nos trechos retos de lado, o desenho e de perfil); o onibus fica onde
            // estava e o outro esta "indo" para a faculdade, virado para ela no fim da pista
            onibus = new[] { new Vector3(1.7f, 12.1f, 0), new Vector3(20.4f, 4.1f, 1) },
            carros = new[] { new Vector4(4.0f, 12.07f, 0, 1), new Vector4(0.9f, 10.1f, 4, 0), new Vector4(5.4f, 4.1f, 5, 1),
                             new Vector4(8.0f, 6.07f, 2, 0), new Vector4(9.8f, 6.07f, 3, 1), new Vector4(17f, 10.07f, 1, 1),
                             new Vector4(12.9f, 12.1f, 5, 0), new Vector4(16.8f, 4.07f, 0, 0), new Vector4(22.8f, 4.07f, 3, 1),
                             new Vector4(17f, 2.1f, 5, 1), new Vector4(21.2f, 2.1f, 2, 0) },
            pasto = new Vector2(3.2f, 2.3f) },
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
        foreach (var c in TodosOsBlocos(nv))
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                    if (c.y + dy != nv.linhaOculta)
                        r.Add(new Vector2Int(c.x + dx, c.y + dy));
        if (nv.plantioExtra != null) r.UnionWith(nv.plantioExtra);
        if (nv.plantioVazio != null) r.UnionWith(nv.plantioVazio);
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
        if (nv.campus) { PintarChaoCampus(nv, estrada, mapa, grid.transform); return; }

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

    /// Chao do campus: grama lisa sorteada, sem a borda de terra da fazenda, e asfalto na estrada.
    /// O asfalto pode ser liso, com rachadura ou com tampa de bueiro (sorteio fixo pela posicao, ~1 em 8 casas cada)
    /// e, nos trechos retos, leva a faixa amarela no meio (girada 90 graus na pista vertical).
    /// Onde a grama encosta no asfalto entra o meio-fio daquele lado: 4 Tilemaps de sobreposicao (cima, baixo,
    /// esquerda, direita), pois uma casa pode ter grama de varios lados. Cada um usa a mesma faixa de 6 linhas
    /// (campus_meio_fio_borda, so o meio-fio e o resto transparente), girada pela matriz de cada casa.
    static void PintarChaoCampus(Nivel nv, HashSet<Vector2Int> estrada, Tilemap mapa, Transform grid)
    {
        string g = "Tiles/Grama/grama_" + nv.estacao;
        var asfalto = ObterTile("Campus/campus_asfalto");
        var rachadura = ObterTile("Campus/campus_asfalto_v2");
        var bueiro = ObterTile("Campus/campus_asfalto_v3");
        var faixa = ObterTile("Campus/campus_asfalto_faixa");
        var meioFio = ObterTile("Campus/campus_meio_fio_borda");
        var giro90 = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one);

        // Uma casa de sobra em volta, como na fazenda; a casa x = -1 do caminho tambem e asfalto.
        for (int x = -1; x < W + 1; x++)
            for (int y = -1; y < H + 1; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (!estrada.Contains(new Vector2Int(x, y)))
                {
                    mapa.SetTile(pos, ObterTile(g + Variacao(g, x, y)));   // grama lisa, sem borda
                    continue;
                }

                int h = ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
                int sorteio = (h >> 5) % 8;
                Tile tile = sorteio == 1 ? rachadura : sorteio == 2 ? bueiro : asfalto;
                bool vertical = false;
                if (tile == asfalto)   // a faixa so entra em trecho reto e onde nao caiu rachadura ou bueiro
                {
                    bool esq = estrada.Contains(new Vector2Int(x - 1, y)), dir = estrada.Contains(new Vector2Int(x + 1, y));
                    bool cima = estrada.Contains(new Vector2Int(x, y + 1)), baixo = estrada.Contains(new Vector2Int(x, y - 1));
                    if (esq && dir && !cima && !baixo) tile = faixa;
                    else if (cima && baixo && !esq && !dir) { tile = faixa; vertical = true; }
                }
                mapa.SetTile(pos, tile);
                if (vertical) mapa.SetTransformMatrix(pos, giro90);
            }

        Vector2Int[] lado = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        string[] nome = { "MeioFio_Cima", "MeioFio_Baixo", "MeioFio_Esquerda", "MeioFio_Direita" };
        float[] angulo = { 0f, 180f, 90f, 270f };   // o PNG tem o meio-fio em cima; gira no sentido anti-horario
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject(nome[i]);
            go.transform.SetParent(grid);
            var camada = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = ORDEM_DECALQUE;
            var giro = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angulo[i]), Vector3.one);
            foreach (var cel in estrada)
            {
                var viz = cel + lado[i];
                if (estrada.Contains(viz)) continue;       // so onde o asfalto encosta na grama
                if (nv.celeiro.Contains(viz)) continue;    // o fim da pista entra no celeiro, sem meio-fio
                if (viz.x < -1 || viz.x > W || viz.y < -1 || viz.y > H) continue;
                var pos = new Vector3Int(cel.x, cel.y, 0);
                camada.SetTile(pos, meioFio);
                if (angulo[i] != 0f) camada.SetTransformMatrix(pos, giro);
            }
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
                    if (cel.y == nv.linhaOculta) continue;
                    if (nv.cultura == "tile_cenoura")
                        Sprite_(grupo, "Tiles/tile_cenoura", Centro(cel), ORDEM_DECALQUE).AddComponent<PixelSway>().background = terra;
                    else
                    {
                        Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
                        Sprite_(grupo, "Tiles/" + nv.cultura.Replace("tile_", "planta_"), Centro(cel), 2 + (H - 1 - cel.y))
                            .AddComponent<PixelSway>();
                    }
                }

        // bloco so de terra (onde o fazendeiro capina) e plantacao de abobora (canteiro de terra e a planta por cima)
        foreach (var c in nv.plantacaoTerra ?? new Vector2Int[0])
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                {
                    var cel = new Vector2Int(c.x + dx, c.y + dy);
                    if (System.Array.IndexOf(nv.cenourasNaTerra ?? new Vector2Int[0], cel) >= 0) continue;   // a cenoura ja traz a terra
                    Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
                }
        foreach (var cel in nv.plantioExtra ?? new Vector2Int[0])
        {
            Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
            Sprite_(grupo, "Tiles/" + nv.cultura.Replace("tile_", "planta_"), Centro(cel), 2 + (H - 1 - cel.y)).AddComponent<PixelSway>();
        }
        foreach (var cel in nv.plantioVazio ?? new Vector2Int[0])
            Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
        foreach (var c in nv.cenourasNaTerra ?? new Vector2Int[0])
            Sprite_(grupo, "Tiles/tile_cenoura", Centro(c), ORDEM_DECALQUE).AddComponent<PixelSway>().background = terra;
        foreach (var c in nv.plantacaoAbobora ?? new Vector2Int[0])
            for (int dx = 0; dx < 2; dx++)
                for (int dy = 0; dy < 2; dy++)
                {
                    var cel = new Vector2Int(c.x + dx, c.y + dy);
                    Sprite_(grupo, "Tiles/canteiro_horta", Centro(cel), ORDEM_DECALQUE);
                    Sprite_(grupo, "Tiles/planta_abobora", Centro(cel), 2 + (H - 1 - cel.y)).AddComponent<PixelSway>();
                }

        estacaoAtual = nv.estacao;

        // fazendeiro de chapeu capinando dentro do cercado da plantacao: na grama, ao lado do bloco so de terra, se a fase
        // tem um (a enxada bate na terra); senao, ao lado dos blocos, abrindo um canteiro novo. Pe em pixel inteiro (1/32).
        // (o campus nao tem plantacao, entao nao tem fazendeiro)
        if (nv.plantacao.Length > 0 && !nv.semFazendeiro)
        {
            Vector3 posFazendeiro;
            if (nv.fazendeiro != Vector2.zero)
                posFazendeiro = new Vector3(Mathf.Round(nv.fazendeiro.x * 32f) / 32f, nv.fazendeiro.y, 0f);
            else
            {
                PlantacaoLimites(nv, out _, out int fx1, out int fy0, out _);
                posFazendeiro = new Vector3(Mathf.Round((fx1 + .7f) * 32f) / 32f, fy0 + .9f, 0f);
            }
            var fazendeiro = Sprite_(grupo, Est_(nv, "Props/prop_fazendeiro_1"), posFazendeiro, -1).AddComponent<FarmerHoe>();
            Som_(fazendeiro.gameObject, "fazendeiro", "HMM").words = FalasFazendeiro;   // clicar nele: uma fala de aldeao
            if (nv.fazendeiroEspelhado) fazendeiro.GetComponent<SpriteRenderer>().flipX = true;
            fazendeiro.frames = new Sprite[7];
            for (int i = 0; i < 7; i++)
                fazendeiro.frames[i] = Bicho_("prop_fazendeiro_" + (i + 1));
        }
        // props de pivo no pe apoiam na base da casa; o poco (2x2, PPU 32) centra entre as duas casas
        Sprite_(grupo, Est_(nv, "Props/prop_poco"), new Vector3(nv.poco.x + 1f, nv.poco.y, 0f), -1);
        // casa da fazenda no alto do cenario, maior que o celeiro (PPU 8: uns 7,3 x 5,4 casas); encostada no canto, o que
        // passa da tela e o topo cobrem a cerca de cima
        if (!nv.campus && nv.casa != Vector2.zero)
            Sprite_(grupo, Est_(nv, "Props/prop_casa"), new Vector3(nv.casa.x, nv.casa.y, 0f), -1);
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

        // milharal na frente da cerca de cima: fileira de tras numa casa inteira, a da frente meia casa adiantada e
        // meia casa mais baixa, para nao formar grade; passa por baixo da HUD, e so enfeite
        if (nv.milharal != Vector2Int.zero)
            for (int fileira = 0; fileira < 2; fileira++)
                for (int x = nv.milharal.x; x <= nv.milharal.y - fileira; x++)
                {
                    var pe = Sprite_(grupo, "Props/prop_milho", new Vector3(x + .5f + fileira * .5f, 15.1f - fileira * .5f, 0f), -1).AddComponent<PixelSway>();
                    pe.amplitude = 2;
                    pe.fixedFraction = .25f;
                    pe.frameTime = .28f;
                }

        // galinheiro (PPU 21,33) ocupa uns 2,3 x 2,6 casas, pe no meio de c.x + 1; galinha e galo, uma casa.
        // arvores: desenho de 64 px em PPU 16, uns 2,8 casas de copa por 3,6 de altura, pe no meio de c.x + 1
        foreach (var c in nv.macieiras)
            Sprite_(grupo, Est_(nv, "Props/prop_arvore_maca"), new Vector3(c.x + 1.4f, c.y, 0f), -1).AddComponent<FallingLeaves>();
        foreach (var c in nv.laranjeiras)
            Sprite_(grupo, Est_(nv, "Props/prop_arvore_laranja"), new Vector3(c.x + 1f, c.y, 0f), -1).AddComponent<FallingLeaves>();
        if (!nv.campus)   // o campus nao tem galinheiro nem galinhas
        {
            Sprite_(grupo, Est_(nv, "Props/prop_galinheiro"), new Vector3(nv.galinheiro.x + 1f, nv.galinheiro.y, 0f), -1);
            // galinha e galo ciscam em volta de onde nascem; a galinha olha para a esquerda no desenho
            Som_(Area_(nv, Ave_(grupo, Est_(nv, "Props/prop_galinha"), new Vector3(nv.galinha.x + 0.5f, nv.galinha.y + 0.1f, 0f), true)).gameObject, "galinha", "COCO").flee = true;
            var cantoDoGalo = Som_(Area_(nv, Ave_(grupo, Est_(nv, "Props/prop_galo"), new Vector3(nv.galo.x + 0.5f, nv.galo.y + 0.1f, 0f), false)).gameObject, "galo", "COCORICO");
            cantoDoGalo.flee = true;
            cantoDoGalo.crowsOnWave = true;   // canta no comeco de cada onda
        }

        // cantinho das vacas (bichos em PPU 21,33, o mesmo grao do gato e da galinha): 2 fardos de feno a esquerda,
        // a marrom passeando e pastando, a creme dormindo, o cocho a direita, porco e ovelha atras.
        // Ocupa umas 4 casas de largura por 2,5 de altura em volta de nv.pasto.
        Vector3 pasto = nv.pasto;
        if (!nv.campus)   // o campus nao tem bichos de fazenda (so o gato)
        {
        foreach (var f in new[] { new Vector2(-1.4f, .4f), new Vector2(-1.1f, -.15f) })
            Sprite_(grupo, "Props/prop_feno", pasto + (Vector3)f, -1);
        VacaMarrom_(grupo, pasto + new Vector3(-.25f, .25f, 0f), nv);
        VacaDormindo_(grupo, pasto + new Vector3(.75f, -.45f, 0f));
        Sprite_(grupo, "Props/prop_cocho_agua", pasto + new Vector3(1.5f, .1f, 0f), -1);
        // porco e ovelha pastam por perto (andam pouco, para nao pisar na estrada)
        if (!nv.semPorcoNoPasto)
        {
            var porco = Ave_(grupo, "Props/prop_porco", pasto + new Vector3(-.7f, 1.15f, 0f), true);
            porco.radius = .22f;
            Som_(porco.gameObject, "porco", "OINK");
        }
        if (!nv.semOvelhaNoPasto)
        {
            var ovelha = Ave_(grupo, "Props/prop_ovelha", pasto + new Vector3(.55f, 1.05f, 0f), true);
            ovelha.radius = .22f;
            Som_(ovelha.gameObject, "ovelha", "MEE");
        }
        }

        // gato brincando com o novelo na frente da porta do celeiro; o novelo nasce a esquerda dele
        // (no campus a porta tem uma pista chegando por baixo: o gato brinca ao lado dela)
        Vector3 gatoPos = new Vector3(nv.celeiro.x + (nv.campus ? 4.3f : 1.3f), nv.celeiro.y - .5f, 0f);
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

        // cercado em volta da plantacao, com folga e mais uma casa e meia a direita (onde o fazendeiro abre um canteiro
        // novo). Lados de cima e de baixo com pecas _3x e 2 portoes no meio do de cima; as laterais sao pecas de frente.
        // Se o lado de baixo cairia colado na cerca do fim do mapa, ele nao e desenhado: a do mapa faz esse papel.
        if (nv.plantacao.Length > 0 && !nv.semCercado)
        {
            PlantacaoLimites(nv, out int px0, out int px1, out int py0, out int py1);
            // largura em pecas inteiras de 1,5 casa, com meia casa de folga de cada lado (e mais uma casa a direita se
            // nao ha bloco so de terra: la o fazendeiro abre um canteiro novo)
            bool temTerra = nv.plantacaoTerra != null && nv.plantacaoTerra.Length > 0;
            float largura = px1 - px0 + 1f + (temTerra ? 0f : 1f);
            int pecas = Mathf.CeilToInt(largura / 1.5f);
            float centro = (px0 + px1) / 2f + (temTerra ? 0f : .5f);
            float esq = centro - pecas * .75f;
            float dir = esq + pecas * 1.5f;
            float baixo = Mathf.Max(py0 - .9f, .9f), cima = py1 + .1f;
            var cercado = new GameObject("CercadoPlantacao").transform;
            cercado.SetParent(grupo);
            CercaHorizontal(cercado, nv, esq, dir, cima, 2);
            if (py0 - .9f >= .9f) CercaHorizontal(cercado, nv, esq, dir, baixo, 0, nv.portaoAberto);
            if (nv.portaoAberto >= 0f && py0 - .9f >= .9f)
            {
                // portao aberto para dentro: a cerca de lado (meia peca de frente, 1,5 casa) faz o papel da folha, no
                // poste da esquerda do vao
                float ini = esq + Mathf.Floor((nv.portaoAberto - esq) / 1.5f) * 1.5f;
                Sprite_(cercado, "Cerca/cerca_frente_" + nv.estacao + "_meia", new Vector3(ini + .15f, baixo, 0f), -1);
            }
            CercaVertical(cercado, nv, esq + .23f, baixo, cima + .3f);
            CercaVertical(cercado, nv, dir - .23f, baixo, cima + .3f);
        }
        if (nv.cercas != null)
        {
            var soltas = new GameObject("CercasSoltas").transform;
            soltas.SetParent(grupo);
            foreach (var c in nv.cercas)
            {
                if (Mathf.Approximately(c.y, c.w)) CercaHorizontal(soltas, nv, c.x, c.z, c.y, 0);
                else CercaVertical(soltas, nv, c.x, c.y, c.w);
            }
        }
        if (nv.vacas != null)
            for (int i = 0; i < nv.vacas.Length; i++)
            {
                var v = new Vector3(nv.vacas[i].x, nv.vacas[i].y, 0f);
                if (i == nv.vacas.Length - 1) VacaDormindo_(grupo, v);
                else VacaMarrom_(grupo, v, nv);
            }
        if (nv.cercasFinas != null)
        {
            var finas = new GameObject("CercasFinas").transform;
            finas.SetParent(grupo);
            foreach (var c in nv.cercasFinas)
                CercaDescendo(finas, nv, c.x, c.w);
        }
        if (nv.laranjeirasSoltas != null)
            foreach (var m in nv.laranjeirasSoltas)
                Sprite_(grupo, Est_(nv, "Props/prop_arvore_laranja"), new Vector3(m.x, m.y, 0f), -1).AddComponent<FallingLeaves>();
        if (nv.macieirasSoltas != null)
            foreach (var m in nv.macieirasSoltas)
                Sprite_(grupo, Est_(nv, "Props/prop_arvore_maca"), new Vector3(m.x, m.y, 0f), -1).AddComponent<FallingLeaves>();
        if (nv.postesCurtos != null)
            foreach (var p in nv.postesCurtos)
                Sprite_(grupo, "Cerca/cerca_frente_" + nv.estacao + "_meia", new Vector3(p.x, p.y, 0f), -1);
        if (nv.fazendeiroParado != null)
            foreach (var f in nv.fazendeiroParado)
            {
                var parado = Sprite_(grupo, "Props/prop_fazendeiro_parado", new Vector3(f.x, f.y, 0f), -1);
                parado.AddComponent<FarmerIdle>();
                Som_(parado, "fazendeiro", "HMM").words = FalasFazendeiro;
            }
        if (nv.cercasAtras != null)
        {
            var atras = new GameObject("CercasAtrasDoTrigo").transform;
            atras.SetParent(grupo);
            foreach (var c in nv.cercasAtras)
                CercaHorizontal(atras, nv, c.x, c.z, c.y, 0, -1f, 2);
        }
        if (nv.cercasDoisPortoes != null)
        {
            var doisPortoes = new GameObject("CercasComDoisPortoes").transform;
            doisPortoes.SetParent(grupo);
            foreach (var c in nv.cercasDoisPortoes)
                CercaHorizontal(doisPortoes, nv, c.x, c.z, c.y, 2);
        }
        if (nv.vacasPretas != null)
            foreach (var v in nv.vacasPretas)
                VacaAnda_(grupo, new Vector3(v.x, v.y, 0f), "preta", nv);
        if (nv.vacasPretasDormindo != null)
            foreach (var v in nv.vacasPretasDormindo)
                VacaDormindo_(grupo, new Vector3(v.x, v.y, 0f), "preta");
        if (nv.vacasClaras != null)
            foreach (var v in nv.vacasClaras)
                VacaAnda_(grupo, new Vector3(v.x, v.y, 0f), "creme", nv);
        if (nv.cercasPortao != null)
        {
            var comPortao = new GameObject("CercasComPortao").transform;
            comPortao.SetParent(grupo);
            foreach (var c in nv.cercasPortao)
                CercaHorizontal(comPortao, nv, c.x, c.z, c.y, 1);
        }
        if (nv.fenos != null)
            foreach (var f in nv.fenos)
                Sprite_(grupo, "Props/prop_feno", new Vector3(f.x, f.y, 0f), -1);
        if (nv.porcos != null)
            foreach (var p in nv.porcos)
            {
                var porquinho = Ave_(grupo, "Props/prop_porco", new Vector3(p.x, p.y, 0f), true);
                porquinho.radius = .22f;
                Som_(porquinho.gameObject, "porco", "OINK");
            }
        if (nv.porcosDireita != null)
            foreach (var p in nv.porcosDireita)
            {
                // o desenho olha para a esquerda: espelhado comeca olhando para a direita
                var porquinho = Ave_(grupo, "Props/prop_porco", new Vector3(p.x, p.y, 0f), true);
                porquinho.GetComponent<SpriteRenderer>().flipX = true;
                porquinho.radius = .22f;
                Som_(porquinho.gameObject, "porco", "OINK");
            }
        if (nv.baldes != null)
            foreach (var b in nv.baldes)
                Sprite_(grupo, "Props/prop_balde", new Vector3(b.x, b.y, 0f), -1);
        if (nv.cochos != null)
            foreach (var c in nv.cochos)
                Sprite_(grupo, "Props/prop_cocho_agua", new Vector3(c.x, c.y, 0f), -1);
        if (nv.galinhas != null)
            foreach (var g in nv.galinhas)
                Som_(Area_(nv, Ave_(grupo, Est_(nv, "Props/prop_galinha"), new Vector3(g.x, g.y, 0f), true)).gameObject, "galinha", "COCO").flee = true;
        if (nv.ovelhasDireita != null)
            foreach (var o in nv.ovelhasDireita)
            {
                // o desenho olha para a esquerda: espelhado ele comeca olhando para a direita (depois vira para onde anda)
                var extra = Ave_(grupo, "Props/prop_ovelha", new Vector3(o.x, o.y, 0f), true);
                extra.GetComponent<SpriteRenderer>().flipX = true;
                extra.radius = .22f;
                Som_(extra.gameObject, "ovelha", "MEE");
            }
        if (nv.ovelhas != null)
            foreach (var o in nv.ovelhas)
            {
                var extra = Ave_(grupo, "Props/prop_ovelha", new Vector3(o.x, o.y, 0f), true);
                extra.radius = .22f;
                Som_(extra.gameObject, "ovelha", "MEE");
            }

        if (nv.campus) CriarPostes(nv, grupo);
        if (nv.campus && nv.onibus != null)
            foreach (var o in nv.onibus)
            {
                var onibus = Sprite_(grupo, "Campus/campus_onibus", new Vector3(o.x, o.y, 0f), -1);
                if (o.z >= 1f) onibus.GetComponent<SpriteRenderer>().flipX = true;   // o desenho olha para a esquerda
            }
        if (nv.campus && nv.carros != null)
            foreach (var c in nv.carros)
            {
                var carro = Sprite_(grupo, "Campus/campus_carro_" + CoresCarro[(int)c.z], new Vector3(c.x, c.y, 0f), -1);
                if (c.w < 1f) carro.GetComponent<SpriteRenderer>().flipX = true;   // o desenho olha para a direita
            }

        // cerca em cima e embaixo; pecas _3x de 96px = uma casa e meia cada
        var cerca = new GameObject("Cerca").transform;
        cerca.SetParent(grupo);
        foreach (float y in new[] { 0.05f, H - 0.95f })
            for (int i = 0; i < W * 2 / 3; i++)
                Sprite_(cerca, "Cerca/cerca_lado_" + (i % 3 + 1) + "_3x", new Vector3(0.75f + i * 1.5f, y, 0f), -1);
    }

    /// Postes do campus: pe a 1/4 de casa da base da casa dada, ordem de desenho pela altura do pe (formula de
    /// Sprite_, como os outros objetos em pe). O componente CampusLights cria a Light2D de cada um ao iniciar.
    static void CriarPostes(Nivel nv, Transform pai)
    {
        var grupo = new GameObject("Postes").transform;
        grupo.SetParent(pai);
        grupo.gameObject.AddComponent<CampusLights>();
        for (int i = 0; i < nv.postes.Length; i++)
        {
            var c = nv.postes[i];
            var poste = Sprite_(grupo, "Campus/campus_poste", new Vector3(c.x + 0.5f, c.y + 0.25f, 0f), -1);
            poste.name = "Poste_" + (i + 1).ToString("00");
        }
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
        // no campus o fim da pista e a faculdade (mesmo tamanho e pe do celeiro); os estragos de celeiro nao valem para ela
        // a faculdade (PPU 8, 7,25 casas de largura) encosta na borda direita da tela, com a porta no centro da pista que chega
        // por baixo (x = 26,5): o corpo fica a 1 pixel do desenho da borda e so a sombra passa dela
        float xCeleiro = nv.campus ? 26.625f : nv.celeiro.x + 1.28f;
        var celeiro = Sprite_(pai, nv.campus ? "Campus/campus_faculdade" : Est_(nv, "Props/prop_celeiro"),
            new Vector3(xCeleiro, nv.celeiro.y, 0f), -1);
        celeiro.name = "Celeiro";
        celeiro.AddComponent<BarnHitFx>();   // treme e solta lascas quando a base perde vida
        // telhado torto, tabua solta e janela quebrada conforme a vida cai (desenhos por cima, nao mexem no celeiro)
        if (!nv.campus) celeiro.AddComponent<BarnDamage>().stages = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/celeiro_dano_1.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/celeiro_dano_2.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>(SPR + "Props/celeiro_dano_3.png"),
        };
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
    static Sprite Bicho_(string nome) => AssetDatabase.LoadAssetAtPath<Sprite>(SPR + Pasta_("Props/" + nome) + ".png");

    // Estacao da fase em construcao. Bichos e coisas do pasto com sombra de grama de verao e de outono ficam em
    // Props/Estacao2 e Props/Estacao3 (mesmo nome do arquivo); sem o arquivo da estacao, vale o da primavera.
    static int estacaoAtual = 1;

    static string Pasta_(string caminho)
    {
        if (estacaoAtual > 1)
        {
            int barra = caminho.LastIndexOf('/');
            string alternativo = caminho.Substring(0, barra) + "/Estacao" + estacaoAtual + caminho.Substring(barra);
            if (AssetDatabase.LoadAssetAtPath<Sprite>(SPR + alternativo + ".png") != null) return alternativo;
        }
        return caminho;
    }

    /// Clicar no bicho mostra a palavra e toca Resources/sons/<id>, se existir (ver AnimalSound).
    static readonly string[] CoresCarro = { "vermelho", "azul", "branco", "verde", "preto", "preto_2" };

    static readonly string[] FalasFazendeiro = { "HMM", "HM", "UHUM" };

    static AnimalSound Som_(GameObject bicho, string id, string palavra)
    {
        var som = bicho.AddComponent<AnimalSound>();
        som.id = id;
        som.word = palavra;
        return som;
    }

    // Prende a galinha na area da fase (para nao atravessar trigo, cerca e galinheiro), se ela tem uma.
    static ChickenWander Area_(Nivel nv, ChickenWander ave)
    {
        if (nv.areaGalinhas != Vector4.zero)
        {
            ave.minPos = new Vector2(nv.areaGalinhas.x, nv.areaGalinhas.y);
            ave.maxPos = new Vector2(nv.areaGalinhas.z, nv.areaGalinhas.w);
        }
        return ave;
    }

    // Lado em que o bicho comeca virado: alterna pela posicao, para nao ficar todo mundo olhando para o mesmo lado
    // (depois eles viram para onde andam).
    static bool LadoInicial(Vector3 pos) => Mathf.FloorToInt(pos.x * 3f + pos.y * 5f) % 2 == 0;

    static ChickenWander Ave_(Transform pai, string caminho, Vector3 pos, bool olhaParaEsquerda)
    {
        var ave = Sprite_(pai, caminho, pos, -1).AddComponent<ChickenWander>();
        ave.GetComponent<SpriteRenderer>().flipX = LadoInicial(pos);
        ave.stepFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + Pasta_(caminho + "_passo") + ".png");
        ave.peckFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + Pasta_(caminho + "_bicando") + ".png");
        ave.facesLeft = olhaParaEsquerda;
        return ave;
    }

    // Vaca marrom que passeia e pasta por perto (bichos em PPU 21,33, o mesmo grao do gato e da galinha).
    static void VacaMarrom_(Transform grupo, Vector3 pos, Nivel nv = null) => VacaAnda_(grupo, pos, "marrom", nv);

    // Vaca que passeia e pasta por perto; cor = marrom ou preta (desenhos prop_vaca_<cor>_*).
    static void VacaAnda_(Transform grupo, Vector3 pos, string cor, Nivel nv = null)
    {
        var vaca = Sprite_(grupo, "Props/prop_vaca_" + cor + "_em_pe", pos, -1).AddComponent<AnimalWander>();
        vaca.walkFrames = new Sprite[4];
        for (int i = 0; i < 4; i++)
            vaca.walkFrames[i] = Bicho_("prop_vaca_" + cor + "_anda_" + (i + 1));
        vaca.grazeFrame = Bicho_("prop_vaca_" + cor + "_pasta");
        vaca.chewFrame = Bicho_("prop_vaca_" + cor + "_pasta_2");
        vaca.GetComponent<SpriteRenderer>().flipX = LadoInicial(pos);
        if (nv != null && nv.areaVacas != Vector4.zero)
        {
            vaca.minPos = new Vector2(nv.areaVacas.x, nv.areaVacas.y);
            vaca.maxPos = new Vector2(nv.areaVacas.z, nv.areaVacas.w);
            var rects = new System.Collections.Generic.List<Rect>();
            foreach (var b in nv.bloqueiosVacas ?? new Vector4[0]) rects.Add(new Rect(b.x, b.y, b.z, b.w));
            vaca.blocks = rects.ToArray();
        }
        vaca.facesLeft = true;
        vaca.radius = .3f;
        vaca.speed = .35f;
        Som_(vaca.gameObject, "vaca", "MUU");
    }

    // Vaca creme dormindo: 2 cliques ZZZZ, no 3o levanta uns segundos e faz MUU.
    static void VacaDormindo_(Transform grupo, Vector3 pos, string cor = "creme")
    {
        var dorme = Sprite_(grupo, "Props/prop_vaca_" + cor + "_deitada", pos, -1);
        Som_(dorme, "vaca", "MUU").awakeSprite = Bicho_("prop_vaca_" + cor + "_em_pe");
        var zz = Sprite_(dorme.transform, "Props/fx_zz", dorme.transform.position + new Vector3(-.15f, .7f, 0f), -1);
        zz.GetComponent<SpriteRenderer>().sortingOrder = dorme.GetComponent<SpriteRenderer>().sortingOrder + 1;
        zz.AddComponent<SleepZ>();
    }

    // Cerca horizontal a partir de x0 na altura y: pecas _3x de 1,5 casa em fila (a sombra e a da estacao da fase);
    // a ultima pode passar de x1. portoes = quantos portoes entram, nas pecas do meio; abrirEm = x onde uma peca
    // fica de fora (portao aberto); ordem >= 0 fixa a ordem de desenho (baixa = atras de plantas e bichos).
    static void CercaHorizontal(Transform pai, Nivel nv, float x0, float x1, float y, int portoes, float abrirEm = -1f, int ordem = -1)
    {
        int pecas = Mathf.Max(1, Mathf.CeilToInt((x1 - x0 - .01f) / 1.5f));
        int primeiro = (pecas - portoes) / 2;
        for (int i = 0; i < pecas; i++)
        {
            float ini = x0 + i * 1.5f;
            if (abrirEm >= ini && abrirEm < ini + 1.5f) continue;   // portao aberto: fica o vao
            bool portao = portoes > 0 && i >= primeiro && i < primeiro + portoes;
            string peca = portao ? "Cerca/portao_" + nv.estacao + "_3x" : "Cerca/cerca_lado_" + nv.estacao + "_3x";
            Sprite_(pai, peca, new Vector3(x0 + .75f + i * 1.5f, y, 0f), ordem);
        }
    }

    // Cercas verticais sao altas e finas: ordenadas pelo pe elas cobriam galinhas e bichos que passam ao lado (a sombra
    // do desenho sobrepunha o bicho). Com ordem fixa baixa ficam atras de bichos, arvores e construcoes.
    const int OrdemCercaVertical = 3;

    // Cerca descendo: duas pecas duplas de frente (3 casas de altura, pivo na base) sobrepostas, a de cima com o topo
    // em y1; a sobreposicao tampa o vao do meio do desenho, como nas laterais do cercado de abobora.
    static void CercaDescendo(Transform pai, Nivel nv, float x, float y1)
    {
        string peca = "Cerca/cerca_frente_dupla_" + nv.estacao + "_3x";
        Sprite_(pai, peca, new Vector3(x, y1 - 3f - 1.72f, 0f), OrdemCercaVertical);
        Sprite_(pai, peca, new Vector3(x, y1 - 3f, 0f), OrdemCercaVertical);
    }

    // Cerca vertical em x de y0 a y1: pecas duplas de frente (2,48 casas de altura, pivo na base do desenho).
    static void CercaVertical(Transform pai, Nivel nv, float x, float y0, float y1)
    {
        const float Altura = 2.48f;
        float comprimento = y1 - y0;
        int pecas = Mathf.Max(1, Mathf.CeilToInt(comprimento / Altura));
        float passo = pecas > 1 ? (comprimento - Altura) / (pecas - 1) : 0f;
        for (int j = 0; j < pecas; j++)
            Sprite_(pai, "Cerca/cerca_frente_dupla_" + nv.estacao + "_3x", new Vector3(x, y0 + j * passo, 0f), OrdemCercaVertical);
    }

    // Todos os blocos 2x2 da fase: cenoura (ou a cultura da fase), so de terra e abobora.
    static IEnumerable<Vector2Int> TodosOsBlocos(Nivel nv)
    {
        foreach (var c in nv.plantacao) yield return c;
        if (nv.plantacaoTerra != null) foreach (var c in nv.plantacaoTerra) yield return c;
        if (nv.plantacaoAbobora != null) foreach (var c in nv.plantacaoAbobora) yield return c;
    }

    // Limites da plantacao principal (com o bloco so de terra) em casas: x de px0 a px1, y de py0 a py1 (borda de cima).
    static void PlantacaoLimites(Nivel nv, out int px0, out int px1, out int py0, out int py1)
    {
        px0 = int.MaxValue; px1 = int.MinValue; py0 = int.MaxValue; py1 = int.MinValue;
        foreach (var c in nv.plantacao)
            Incluir(c, ref px0, ref px1, ref py0, ref py1);
        if (nv.plantacaoTerra != null)
            foreach (var c in nv.plantacaoTerra)
                Incluir(c, ref px0, ref px1, ref py0, ref py1);
        if (nv.linhaOculta == py1 - 1) py1--;   // sem a fileira de cima, o cercado desce uma casa
    }

    static void Incluir(Vector2Int c, ref int px0, ref int px1, ref int py0, ref int py1)
    {
        px0 = Mathf.Min(px0, c.x);
        px1 = Mathf.Max(px1, c.x + 2);
        py0 = Mathf.Min(py0, c.y);
        py1 = Mathf.Max(py1, c.y + 2);
    }

    // Desenho da estacao do nivel: a sombra de cada um combina com a grama da fase (estacao 2 e 3 usam
    // o arquivo com sufixo _2 ou _3; a estacao 1 usa o nome puro). Sem o arquivo da estacao, cai no puro.
    static string Est_(Nivel nv, string caminho)
    {
        if (nv.estacao > 1 && AssetDatabase.LoadAssetAtPath<Sprite>(SPR + caminho + "_" + nv.estacao + ".png") != null)
            return caminho + "_" + nv.estacao;
        return caminho;
    }

    static GameObject Sprite_(Transform pai, string caminho, Vector3 pos, int ordem)
    {
        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(caminho));
        go.transform.SetParent(pai);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR + Pasta_(caminho) + ".png");
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
