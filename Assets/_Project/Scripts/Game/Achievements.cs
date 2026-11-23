using System;
using System.Collections.Generic;
using UnityEngine;

public enum AchievementGroup { Campanha, Bichos, Torres, Inimigos }

public class AchievementInfo
{
    public string id, name, description;
    public AchievementGroup group;
}

[Serializable]
class AchievementSave
{
    public List<string> unlocked = new List<string>();   // conquistas ganhas
    public List<string> greeted = new List<string>();    // bichos em que ja clicou (7)
    public List<string> found = new List<string>();      // interacoes achadas (8)
    public List<string> hearts = new List<string>();     // bichos que ja soltaram coracao (9 e 10)
    public List<string> towers = new List<string>();     // tipos de torre ja construidos (13)
    public List<string> enemies = new List<string>();    // tipos de inimigo ja vistos (20)
}

/// 20 conquistas estilo Steam (issue #126). Guardadas em PlayerPrefs, como o CampaignProgress.
///
/// Tres tipos de regra:
///  - guardadas entre partidas (7, 8, 9, 10, 13, 20): listas em AchievementSave;
///  - contadas durante o nivel (5, 6, 11, 12, 15, 17): zeram em BeginLevel e valem so se o nivel for vencido
///    (5, 6, 11, 12) ou na hora em que o numero e batido (15, 17);
///  - na hora do evento (1 a 4, 14, 16, 18, 19).
/// Os numeros (150 moedas, 20 caes, metade e um terco do caminho...) ficam nas constantes abaixo.
public static class Achievements
{
    // ids (nao mudar depois de publicar: estao salvos no PlayerPrefs)
    const string FazendaFacil = "fazenda_facil", FazendaNormal = "fazenda_normal", FazendaDificil = "fazenda_dificil";
    const string ProvaFinal = "prova_final", SemPressa = "sem_pressa", CeleiroIntacto = "celeiro_intacto";
    const string BomDia = "bom_dia", Curioso = "curioso", Prrrr = "prrrr", CoracaoDeOuro = "coracao_de_ouro";
    const string FolgaDoGalo = "folga_do_galo", MaoDeVaca = "mao_de_vaca";
    const string MaoNaMassa = "mao_na_massa", FazendeiroDedicado = "fazendeiro_dedicado", CaoDeGuarda = "cao_de_guarda";
    const string ReiDoMel = "rei_do_mel", ChuvaDeMilho = "chuva_de_milho";
    const string AndaRato = "anda_rato", CacadorDeJavalis = "cacador_de_javalis", Bestiario = "bestiario";

    // numeros ajustaveis nos testes
    const int ExtraMoneyAllowed = 150;       // 12: moedas gastas alem das iniciais
    const int GuardianKillsNeeded = 20;      // 15: inimigos derrotados pelos caes do Tufao numa partida
    const int BeehivesNeeded = 3;            // 16: colmeias no nivel 3 ao mesmo tempo
    const int ScarecrowsNeeded = 4;          // 17: espantalhos diferentes que chegam ao nivel 3 numa partida
    const int MaxTowerLevel = 2;             // nivel 3 = indice 2
    const float RatHalfPath = .5f;           // 18
    const float BoarThirdPath = 1f / 3f;     // 19

    // o que cada lista guardada precisa ter para fechar a conquista
    static readonly string[] AnimalIds = { "galinha", "galo", "vaca", "porco", "ovelha", "gato" };
    static readonly string[] InteractionIds = { "vaca_acorda", "galinha_foge", "galo_foge", "gato_pula" };
    static readonly string[] TowerIds = { "Tower_CanhaoAbobora", "Tower_Colmeia", "Tower_Espantalho", "Tower_MetralhadoraUvas", "Tower_Tufao" };
    static readonly string[] EnemyIds =
    {
        "Enemy_Rato", "Enemy_Gafanhoto", "Enemy_Javali", "Enemy_Raposa", "Enemy_Guaxinim",
        "Enemy_Corvo", "Enemy_Morcego", "Enemy_AFD", "Enemy_AFN", "Enemy_Chomsky", "Enemy_Professor"   // Professor = o boss Rieder
    };
    const string RatId = "Enemy_Rato", BoarId = "Enemy_Javali";   // o rato anda em ziguezague
    const string BeehiveId = "Tower_Colmeia", ScarecrowId = "Tower_Espantalho";

    public static readonly AchievementInfo[] All =
    {
        New(FazendaFacil, "Fazenda salva (Fácil)", "Vença todos os níveis no Fácil.", AchievementGroup.Campanha),
        New(FazendaNormal, "Fazenda salva (Normal)", "Vença todos os níveis no Normal.", AchievementGroup.Campanha),
        New(FazendaDificil, "Fazenda salva (Difícil)", "Vença todos os níveis no Difícil.", AchievementGroup.Campanha),
        New(ProvaFinal, "Prova Final concluída", "Vença a fase bônus.", AchievementGroup.Campanha),
        New(SemPressa, "Sem pressa", "Vença um nível sem chamar a próxima onda antes da hora.", AchievementGroup.Campanha),
        New(CeleiroIntacto, "Celeiro intacto", "Vença um nível sem nenhum inimigo tocar no celeiro.", AchievementGroup.Campanha),

        New(BomDia, "Bom dia, fazenda", "Cumprimente todos os animais.", AchievementGroup.Bichos),
        New(Curioso, "Curioso", "Ache todas as interações dos bichos.", AchievementGroup.Bichos),
        New(Prrrr, "Prrrr", "Faça o gato ronronar.", AchievementGroup.Bichos),
        New(CoracaoDeOuro, "Coração de ouro", "Ganhe o coração de todos os animais.", AchievementGroup.Bichos),
        New(FolgaDoGalo, "Dia de folga do galo", "Vença um nível sem clicar em nenhum animal.", AchievementGroup.Bichos),
        New(MaoDeVaca, "Mão de vaca", "Vença um nível sem gastar mais de 150 moedas além das iniciais.", AchievementGroup.Bichos),

        New(MaoNaMassa, "Mão na massa", "Construa cada um dos 5 tipos de torre.", AchievementGroup.Torres),
        New(FazendeiroDedicado, "Fazendeiro dedicado", "Melhore uma torre até o nível 3.", AchievementGroup.Torres),
        New(CaoDeGuarda, "Cão de guarda", "Os cães do Tufão derrotam 20 inimigos numa partida.", AchievementGroup.Torres),
        New(ReiDoMel, "Rei do mel", "Tenha 3 colmeias no nível 3 ao mesmo tempo.", AchievementGroup.Torres),
        New(ChuvaDeMilho, "Chuva de milho", "Tenha 4 espantalhos no nível 3 numa partida.", AchievementGroup.Torres),

        New(AndaRato, "Anda, rato", "Derrote um rato antes de ele andar metade do caminho.", AchievementGroup.Inimigos),
        New(CacadorDeJavalis, "Caçador de javalis", "Derrote um javali antes de ele chegar a um terço do caminho.", AchievementGroup.Inimigos),
        New(Bestiario, "Bestiário completo", "Veja todos os tipos de inimigo.", AchievementGroup.Inimigos),
    };

    const string SaveKey = "DefesaDaColheita.Achievements.v1";
    static AchievementSave save;

    // contadores do nivel atual
    static int levelSpent, guardianKills;
    static readonly HashSet<Tower> scarecrowsMaxed = new HashSet<Tower>();   // cada torre conta uma vez
    static bool earlyWave, barnHit, animalClicked;

    static AchievementInfo New(string id, string name, string description, AchievementGroup group) =>
        new AchievementInfo { id = id, name = name, description = description, group = group };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        save = null;
        BeginLevel();
        // -= antes de += : sem recarregar o dominio no editor, a inscricao anterior continua viva
        Enemy.OnDied -= HandleEnemyDied;
        Enemy.OnReachedBase -= HandleEnemyReachedBase;
        Enemy.OnSpawnedByEnemy -= HandleSpawnedByEnemy;
        WaveSpawner.OnEnemySpawned -= HandleEnemySpawned;
        Tower.OnUpgraded -= HandleTowerUpgraded;
        Enemy.OnDied += HandleEnemyDied;
        Enemy.OnReachedBase += HandleEnemyReachedBase;
        Enemy.OnSpawnedByEnemy += HandleSpawnedByEnemy;
        WaveSpawner.OnEnemySpawned += HandleEnemySpawned;
        Tower.OnUpgraded += HandleTowerUpgraded;
    }

    // ------------------------------------------------------------------ consulta (menu)

    public static int Total => All.Length;

    public static int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (AchievementInfo info in All) if (IsUnlocked(info.id)) count++;
            return count;
        }
    }

    public static bool IsUnlocked(string id) => Data.unlocked.Contains(id);

    /// Progresso das conquistas que guardam lista entre partidas ("3/6"). total = 0: sem progresso a mostrar.
    public static void Progress(string id, out int current, out int total)
    {
        switch (id)
        {
            case BomDia: current = Data.greeted.Count; total = AnimalIds.Length; break;
            case Curioso: current = Data.found.Count; total = InteractionIds.Length; break;
            case CoracaoDeOuro: current = Data.hearts.Count; total = AnimalIds.Length; break;
            case MaoNaMassa: current = Data.towers.Count; total = TowerIds.Length; break;
            case Bestiario: current = Data.enemies.Count; total = EnemyIds.Length; break;
            default: current = 0; total = 0; break;
        }
    }

#if UNITY_EDITOR
    public static void ResetForTesting()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        save = null;
    }
#endif

    // ------------------------------------------------------------------ chamado pelo jogo

    /// Zera os contadores do nivel. O GameManager chama ao abrir um nivel e ao reiniciar.
    public static void BeginLevel()
    {
        levelSpent = 0;
        guardianKills = 0;
        scarecrowsMaxed.Clear();
        earlyWave = false;
        barnHit = false;
        animalClicked = false;
    }

    public static void NoteSpent(int amount) => levelSpent += Mathf.Max(0, amount);

    /// 5: chamou a proxima onda antes de acabar a espera entre ondas (pauseAfter).
    public static void NoteEarlyWave() => earlyWave = true;

    /// Nivel vencido (GameManager.CompleteLevel, vale para os 3 da campanha e para a fase bonus).
    public static void LevelWon(string scene, int startingMoney)
    {
        if (scene == CampaignProgress.BonusScene) Unlock(ProvaFinal);
        if (!earlyWave) Unlock(SemPressa);
        if (!barnHit) Unlock(CeleiroIntacto);
        if (!animalClicked) Unlock(FolgaDoGalo);
        if (levelSpent <= startingMoney + ExtraMoneyAllowed) Unlock(MaoDeVaca);
    }

    /// Fim da partida (GameManager.EndGame): so a campanha inteira vale para 1, 2 e 3.
    public static void GameWon(GameResult result)
    {
        if (!result.won || !result.fullCampaign) return;
        Unlock(result.difficulty == GameDifficulty.Easy ? FazendaFacil
            : result.difficulty == GameDifficulty.Normal ? FazendaNormal : FazendaDificil);
    }

    /// Clique num bicho (AnimalSound): 7 e 11.
    public static void NoteAnimalClick(string animal)
    {
        animalClicked = true;
        if (AddTo(Data.greeted, animal)) CompleteIfAll(Data.greeted, AnimalIds, BomDia);
    }

    /// Reacao especial de um bicho (AnimalSound): 8.
    public static void NoteInteraction(string interaction)
    {
        if (AddTo(Data.found, interaction)) CompleteIfAll(Data.found, InteractionIds, Curioso);
    }

    /// Bicho soltou o coracao, 5 cliques seguidos (AnimalSound): 9 e 10.
    public static void NoteHeart(string animal)
    {
        if (animal == "gato") Unlock(Prrrr);
        if (AddTo(Data.hearts, animal)) CompleteIfAll(Data.hearts, AnimalIds, CoracaoDeOuro);
    }

    /// Torre construida (BuildPlot): 13.
    public static void NoteTowerBuilt(TowerData data)
    {
        if (data != null && AddTo(Data.towers, data.name)) CompleteIfAll(Data.towers, TowerIds, MaoNaMassa);
    }

    /// Um cao do Tufao deu a mordida que matou (Guardian): 15.
    public static void NoteGuardianKill()
    {
        if (++guardianKills >= GuardianKillsNeeded) Unlock(CaoDeGuarda);
    }

    // ------------------------------------------------------------------ eventos do jogo

    static void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null || enemy.PathLength <= 0f) return;
        float progress = enemy.Distance / enemy.PathLength;
        if (enemy.data.name == RatId && progress < RatHalfPath) Unlock(AndaRato);
        else if (enemy.data.name == BoarId && progress < BoarThirdPath) Unlock(CacadorDeJavalis);
    }

    static void HandleEnemyReachedBase(Enemy enemy) => barnHit = true;

    static void HandleEnemySpawned(Enemy enemy) => NoteEnemySeen(enemy);
    static void HandleSpawnedByEnemy(Enemy parent, Enemy child) => NoteEnemySeen(child);

    static void NoteEnemySeen(Enemy enemy)
    {
        if (enemy != null && enemy.data != null && AddTo(Data.enemies, enemy.data.name))
            CompleteIfAll(Data.enemies, EnemyIds, Bestiario);
    }

    static void HandleTowerUpgraded(Tower tower)
    {
        if (tower == null || tower.data == null || tower.Level < MaxTowerLevel) return;
        Unlock(FazendeiroDedicado);

        if (tower.data.name == BeehiveId)
        {
            int maxed = 0;
            foreach (Tower other in Tower.All)
                if (other != null && other.data == tower.data && other.Level >= MaxTowerLevel) maxed++;
            if (maxed >= BeehivesNeeded) Unlock(ReiDoMel);
        }
        else if (tower.data.name == ScarecrowId && scarecrowsMaxed.Add(tower) && scarecrowsMaxed.Count >= ScarecrowsNeeded)
            Unlock(ChuvaDeMilho);
    }

    // ------------------------------------------------------------------ guardar

    static AchievementSave Data
    {
        get
        {
            if (save != null) return save;
            try { save = JsonUtility.FromJson<AchievementSave>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (Exception) { save = null; }
            return save ??= new AchievementSave();
        }
    }

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
    }

    /// Acrescenta se ainda nao estava (e grava). Devolve true se era novo.
    static bool AddTo(List<string> list, string value)
    {
        if (string.IsNullOrEmpty(value) || list.Contains(value)) return false;
        list.Add(value);
        Save();
        return true;
    }

    static void CompleteIfAll(List<string> have, string[] needed, string id)
    {
        foreach (string item in needed)
            if (!have.Contains(item)) return;
        Unlock(id);
    }

    static void Unlock(string id)
    {
        if (!AddTo(Data.unlocked, id)) return;
        foreach (AchievementInfo info in All)
            if (info.id == id) { SoundBank.Play("conquista"); AchievementToast.Show(info); return; }
    }
}
