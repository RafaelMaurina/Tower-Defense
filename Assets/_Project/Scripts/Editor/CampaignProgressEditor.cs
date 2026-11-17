using UnityEditor;
using UnityEngine;

public static class CampaignProgressEditor
{
    [MenuItem("Defesa da Colheita/Testes/Resetar progresso de fases")]
    static void ResetCampaignProgress()
    {
        if (!EditorUtility.DisplayDialog(
                "Resetar progresso de fases",
                "Isso vai bloquear novamente a fase Bônus. O ranking não será apagado.",
                "Resetar progresso",
                "Cancelar"))
            return;

        CampaignProgress.ResetCampaignProgressForTesting();
        Debug.Log("Progresso de fases resetado. Volte ao menu ou reabra a seleção de fases para conferir a Bônus bloqueada.");
    }
}
