using UnityEditor;
using UnityEngine;

/// Define o galo como icone do executavel (icone padrao do jogador). Roda ao abrir o projeto e so
/// mexe nas configuracoes se o icone ainda nao estiver aplicado. Tambem esta no menu.
[InitializeOnLoad]
static class IconeDoJogo
{
    const string Caminho = "Assets/_Project/Art/Icon/icone_jogo.png";

    static IconeDoJogo() => EditorApplication.delayCall += Aplicar;

    [MenuItem("Defesa da Colheita/Aplicar icone do jogo")]
    static void Aplicar()
    {
        var icone = AssetDatabase.LoadAssetAtPath<Texture2D>(Caminho);
        if (icone == null) return;
        var atuais = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
        if (atuais != null && atuais.Length > 0 && atuais[0] == icone) return;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icone });
    }
}
