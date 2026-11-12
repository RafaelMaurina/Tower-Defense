using System.IO;
using System.Security.Cryptography;
using UnityEditor;

/// Os prefabs e as cenas dos niveis nao vao para o git (sao gerados e ocupam
/// milhares de linhas). Ao abrir o projeto, se algum estiver faltando, este
/// script roda o "Gerar niveis", que cria so os que nao existem.
/// Tambem refaz tudo quando o MontarNiveis.cs mudou desde a ultima geracao neste computador
/// (o hash fica em Library/, fora do git), para ninguem jogar com cena de versao antiga.
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
        string hash = HashDoGerador();
        bool faltaAlgum = MontarNiveis.FaltaAlgum();
        if (!faltaAlgum && hash == HashSalvo())
        {
            MontarNiveis.AtualizarCatalogos();
            MontarNiveis.AtualizarBuild();
            return;
        }

        if (!faltaAlgum)
        {
            UnityEngine.Debug.Log("O gerador de niveis mudou. Refazendo os niveis...");
            MontarNiveis.RefazerSemPerguntar();
            Salvar(hash);
            return;
        }

        UnityEngine.Debug.Log("Niveis nao encontrados neste computador. Gerando...");
        MontarNiveis.Gerar();
        Salvar(hash);
    }

    const string ARQUIVO_HASH = "Library/NiveisGerados.txt";

    static string HashDoGerador()
    {
        string[] achados = AssetDatabase.FindAssets("MontarNiveis t:MonoScript");
        foreach (string guid in achados)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (!caminho.EndsWith("/MontarNiveis.cs")) continue;
            using (var md5 = MD5.Create())
                return System.BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(caminho)));
        }
        return "";
    }

    static string HashSalvo() => File.Exists(ARQUIVO_HASH) ? File.ReadAllText(ARQUIVO_HASH) : "";

    static void Salvar(string hash) => File.WriteAllText(ARQUIVO_HASH, hash);
}
