using UnityEditor;

/// Os prefabs e as cenas dos niveis nao vao para o git (sao gerados e ocupam
/// milhares de linhas). Ao abrir o projeto, se algum estiver faltando, este
/// script roda o "Gerar niveis", que cria so os que nao existem.
[InitializeOnLoad]
static class GerarNiveisAoAbrir
{
    static GerarNiveisAoAbrir() => EditorApplication.delayCall += Tentar;

    static void Tentar()
    {
        // espera o Unity terminar de importar e compilar antes de mexer em cena
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Tentar;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!MontarNiveis.FaltaAlgum()) return;

        UnityEngine.Debug.Log("Niveis nao encontrados neste computador. Gerando...");
        MontarNiveis.Gerar();
    }
}
