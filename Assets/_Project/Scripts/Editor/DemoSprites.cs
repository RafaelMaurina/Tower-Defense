using UnityEngine;
using UnityEditor;

/// Monta uma demonstracao dos sprites na cena ATUAL, dentro de um objeto
/// unico chamado "DEMO Sprites" — basta apagar esse objeto para desfazer.
public static class DemoSprites
{
    const float PPU = 64f;
    const string PASTA = "Assets/_Project/Art/Sprites/";
    const string RAIZ = "DEMO Sprites";

    [MenuItem("Defesa da Colheita/Montar demo de sprites")]
    public static void Montar()
    {
        var antigo = GameObject.Find(RAIZ);
        if (antigo != null) Object.DestroyImmediate(antigo);

        var raiz = new GameObject(RAIZ);

        // Tiles de 64px: a 64 PPU cada um ocupa 1 unidade e encostam sem sobra
        Linha(raiz, "1 - Tiles (1 unidade cada, encostados)", 1f, new Vector3(-3.5f, 2.3f, 0f),
            "Tiles/tile_canteiro", "Tiles/tile_abobora", "Tiles/tile_cenoura", "Tiles/tile_trigo");

        // Pivo no pe: todos apoiam exatamente na mesma linha, independente da altura
        Linha(raiz, "2 - Props (pivo no pe, mesma linha de chao)", 1.4f, new Vector3(-3.5f, -0.4f, 0f),
            "Props/prop_poco", "Props/prop_cogumelos");

        // Cerca: telas de 32px = 0.5 unidade. Encostadas, aparece o vao entre as pecas
        Linha(raiz, "3 - Cerca encostada (mostra o vao)", 0.5f, new Vector3(-3.5f, -2.3f, 0f),
            "Cerca/cerca_lado_1", "Cerca/cerca_lado_2", "Cerca/cerca_lado_3",
            "Cerca/portao_1", "Cerca/portao_estrada");

        var cam = Camera.main;
        if (cam != null)
        {
            Debug.Log("Camera antes: orthographic=" + cam.orthographic + " size=" + cam.orthographicSize);
            cam.orthographic = true;
            cam.orthographicSize = 270f / PPU;   // 540px de altura de arte a 64 PPU
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        Selection.activeGameObject = raiz;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("Demo montada. Camera ortografica size " + (270f / PPU)
                  + " = 960x540 de arte a 64 PPU. Apague o objeto \"" + RAIZ + "\" para desfazer.");
    }

    static void Linha(GameObject raiz, string titulo, float passo, Vector3 inicio, params string[] nomes)
    {
        var grupo = new GameObject(titulo);
        grupo.transform.SetParent(raiz.transform);
        float x = inicio.x;
        foreach (var nome in nomes)
        {
            var caminho = PASTA + nome + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (sprite == null) { Debug.LogWarning("Sprite nao encontrado: " + caminho); continue; }

            var go = new GameObject(nome.Substring(nome.IndexOf('/') + 1));
            go.transform.SetParent(grupo.transform);
            go.transform.position = new Vector3(x, inicio.y, 0f);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            x += passo;
        }
    }
}
