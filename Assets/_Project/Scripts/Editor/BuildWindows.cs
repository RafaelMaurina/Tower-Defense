using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Build local de teste: usa o projeto atual, sem caminhos especificos de computador.
public static class BuildWindows
{

    static readonly string[] Scenes = {
        "Assets/_Project/Scenes/Menu.unity",
        "Assets/_Project/Scenes/Nivel_1_Primavera.unity",
        "Assets/_Project/Scenes/Nivel_2_Verao.unity",
        "Assets/_Project/Scenes/Nivel_3_Outono.unity",
        "Assets/_Project/Scenes/Nivel_4_Campus.unity"
    };

    [MenuItem("Defesa da Colheita/Build Windows/Gerar build")]
    public static void Generate() => Build(false);

    [MenuItem("Defesa da Colheita/Build Windows/Gerar e jogar")]
    public static void GenerateAndRun() => Build(true);

    [MenuItem("Defesa da Colheita/Build Windows/Gerar build", true)]
    [MenuItem("Defesa da Colheita/Build Windows/Gerar e jogar", true)]
    static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode
        && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;

    // Tambem pode ser chamado com -batchmode -quit -executeMethod BuildWindows.Generate.
    static void Build(bool run)
    {
        if (!CanBuild()) throw new BuildFailedException("Pare o Play e aguarde a compilacao antes de gerar a build.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new BuildFailedException("Instale o modulo Windows Build Support desta versao do Unity pelo Unity Hub.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Gera somente niveis ausentes, preservando os niveis locais existentes.
        if (MontarNiveis.FaltaAlgum()) MontarNiveis.Gerar();
        MontarNiveis.AtualizarCatalogos();
        MontarNiveis.AtualizarBuild();
        AssetDatabase.SaveAssets();
        foreach (string scene in Scenes)
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new BuildFailedException("Cena ausente: " + scene + ". Feche a cena e use Defesa da Colheita > Gerar niveis.");
        if (EditorBuildSettings.scenes.Count(s => s.enabled && Scenes.Contains(s.path)) != Scenes.Length)
            throw new BuildFailedException("Os cinco cenarios precisam estar habilitados no Build Settings.");

        string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DefesaDaColheita-Build", "DefesaDaColheita.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var oldMode = PlayerSettings.fullScreenMode;
        int oldWidth = PlayerSettings.defaultScreenWidth;
        int oldHeight = PlayerSettings.defaultScreenHeight;
        try
        {
            // Tela cheia sem bordas; a resolucao real acompanha o monitor.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None)
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Build Windows falhou: " + report.summary.result +
                    ". Erros: " + report.summary.totalErrors + ". Confira o Console.");
            Debug.Log("Build Windows concluida: " + output + " (" + report.summary.totalSize + " bytes).");
            if (!Application.isBatchMode && !run) EditorUtility.RevealInFinder(output);
        }
        finally
        {
            PlayerSettings.fullScreenMode = oldMode;
            PlayerSettings.defaultScreenWidth = oldWidth;
            PlayerSettings.defaultScreenHeight = oldHeight;
        }
    }
}
