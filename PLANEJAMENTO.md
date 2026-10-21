# Planejamento geral: Defesa da Colheita

> **Para quem for passar este arquivo ao seu Claude:** ele e autocontido. Leia tudo antes
> de propor ou escrever codigo. Siga os **contratos** da secao 4 e as **regras** da secao 7.
> Nao invente outra arquitetura sem avisar o grupo. Trabalho avaliado: o codigo precisa
> ser simples o bastante para o autor explicar na banca.

## 1. Contexto

- Disciplina CCC300 (Computacao Grafica e Realidade Virtual), Prof. Dr. Rafael Rieder, UPF.
- Tower Defense em **Unity 6000.6.2f1, 2D URP**, em trio: **Rafael** (arte e audio), **Fabio** (combate), **Joao** (jogo e interface).
- Apresentacao: **24/11/2026** (10 min de demo + 5 min de perguntas). Sprints em aula: 03/11, 10/11, 17/11.
- Repositorio privado: `RafaelMaurina/Tower-Defense`. Deve ser compartilhado com o usuario `rafaelrieder`.
- Nota: movimentacao/torres/projeteis 2,5 · interatividade 2,5 · GitHub + dominio individual 2,0 · modelagem/variedade 1,0 · VFX+SFX 1,0 · menu (dificuldade, ranking, info) 0,5 · video 0,5.
- **Os tres precisam ter commits** no historico. README com link do executavel e do video.

## 2. O jogo

Tema: defesa de uma fazenda, pixel art cozy visto de cima. Arte propria do trio (nada de
assets de terceiros com copyright).

- **3 niveis** (Primavera, Verao, Outono), 15 x 8 casas de 64 px, ja gerados em `Assets/_Project/Scenes` e `Prefabs/Niveis`.
- **5 torres**, cada uma com **3 niveis de upgrade**: Espantalho, Metralhadora de Uvas, Canhao de Abobora, Colmeia, Tufao (casinha que invoca um pastor alemao corpo a corpo; destrava ao vencer no Dificil).
- **7 inimigos**: corvo, guaxinim, javali, gafanhoto (salta), raposa, morcego (voa), pato (ziguezague, quebra a mira preditiva).
- Apenas 2 torres atingem inimigos aereos.
- **2 poderes consumiveis** (apicultor, fazendeiro com forcado), destravados no nivel 3 das torres.
- **Dificuldade = horario do dia**: Facil manha, Normal por do sol, Dificil noite. **So iluminacao, nunca tileset novo.**
- Menu com dificuldade, ranking local e tela de informacoes.
- **Fase bonus (opcional)** "A Prova Final": campus a noite, boss em 3 fases (hierarquia de Chomsky), inimigos AFD/AFN (o AFN se divide em 2 no primeiro dano). Tom de homenagem, nunca de chacota. So entra se o resto estiver fechado.
- Realidade virtual esta **fora do escopo**.

## 3. O que ja existe no repositorio

- `WaypointPath` (`Scripts/Enemies`): caminho por **distancia** percorrida (`Length`, `PointAt(distancia)`). Base para empurrao, salto e copia do AFN.
- `MontarNiveis` (menu *Defesa da Colheita > Gerar niveis*): gera tilemap, caminhos, canteiros, decoracao e a base.
- Cada nivel tem `Caminhos/Caminho_A` (e `_B` no nivel 3), `Canteiros/Canteiro_NN` (sprite + trigger) e `Base` (trigger).
- Sprites em `Assets/_Project/Art/Sprites`, importados com PPU 64, filtro Point, sem compressao. Regras em `ARTE.md`.
- Arte: leia `ARTE.md` (ordem de desenho, estacoes, pendencias). Divisao de tarefas: `DIVISAO.md`.

## 4. Contratos entre as partes (Fabio e Joao devem seguir)

Combinar estes nomes antes de codar evita retrabalho. Mudar um contrato = avisar o grupo.

```csharp
// Qualquer coisa que leva dano (inimigo, boss).
public interface IDamageable {
    void TakeDamage(float amount);
    bool IsAlive { get; }
    Transform Transform { get; }
}

// Inimigo: anda por distancia no WaypointPath.
public class Enemy : MonoBehaviour, IDamageable {
    public EnemyData data;                 // ScriptableObject: vida, velocidade, recompensa, voa?
    public float Distance { get; set; }    // distancia percorrida no caminho
    public bool Flying => data.flying;
    public static event System.Action<Enemy> OnDied;      // GameManager soma dinheiro
    public static event System.Action<Enemy> OnReachedBase; // GameManager tira vida
}

// Dados em ScriptableObject, nunca numeros soltos no codigo.
// EnemyData: vida, velocidade, recompensa, dano na base, voa.
// TowerData: nome, 3 niveis (custo, dano, alcance, cadencia, sprite), atinge aereos?, projetil.
// WaveData:  lista de (EnemyData, quantidade, intervalo, caminho A/B), pausa entre ondas.

// Torre pergunta ao jogo quem esta no alcance; nao procura por Find.
public static class EnemyRegistry {
    public static IReadOnlyList<Enemy> All { get; }   // Fabio mantem
}

// Joao (jogo): estado, dinheiro, vida da base.
public static class GameManager {
    public static int Money, BaseLife;
    public static event System.Action<GameState> OnStateChanged;
}
```

Responsabilidades nas fronteiras:
- **Fabio** cria `Enemy`, `Tower`, `Projectile`, `EnemyRegistry` e os `ScriptableObject` de inimigo e torre.
- **Joao** cria `GameManager`, `WaveSpawner` (le `WaveData`, instancia `Enemy`), construcao nos canteiros, HUD, menu, ranking.
- **Joao** liga o clique do canteiro a `Tower` instanciada por `TowerData` (preco vem do `TowerData`).
- Ordem de desenho de inimigos e torres: `sortingOrder = 100 + round((8 - y) * 10)` (ver `ARTE.md`).

Estrutura de pastas (cada um mexe na sua):
```
Assets/_Project/
  Art/            Rafael
  Audio/          Rafael
  Prefabs/Niveis/ gerado (nao editar a mao)
  Prefabs/Enemies, Towers, Projectiles/   Fabio
  Scripts/Enemies, Towers, Projectiles/   Fabio
  Scripts/Game, UI, Waves/                Joao
  Data/Enemies, Towers, Waves/            Fabio (inimigos, torres) e Joao (ondas)
  Scenes/         cenas dos niveis geradas; Menu e Jogo: Joao
```

## 5. Marcos e entregas

| Marco | Data | Pronto quando |
|---|---|---|
| **M1** | 03/11 | Nivel 1 jogavel de ponta a ponta: 1 inimigo, 2 torres, ondas, vida, dinheiro, HUD basico |
| **M2** | 10/11 | 7 inimigos, 5 torres com 3 niveis, os 3 niveis, menu, ranking, SFX principais |
| **M3** | 17/11 | Tudo integrado e balanceado, VFX, musica, build testada em outra maquina |
| **Entrega** | 24/11 | Apresentacao. README com executavel e video |
| Bonus | so apos M2 | Fase "A Prova Final" |

O quadro (aba Projetos do GitHub) tem uma coluna por estado e um milestone por marco.
Cada tarefa e uma **issue** com dono, rotulo de area (`arte-audio`, `combate`, `jogo-ui`) e milestone.

## 6. Ordem sugerida para destravar o grupo

1. **Fabio e Joao** fecham os contratos da secao 4 numa conversa de 20 minutos e abrem um PR so com as interfaces e os `ScriptableObject` vazios.
2. **Fabio**: `Enemy` andando no `WaypointPath` e `EnemyRegistry`. **Joao**: `GameManager` e `WaveSpawner` com um inimigo de teste.
3. **Fabio**: `Tower` base com 1 projetil. **Joao**: construir no canteiro, dinheiro, HUD.
4. **Rafael**: sprites do primeiro inimigo, da primeira torre e do projetil entram cedo, para o jogo nao ficar de quadrado cinza.
5. Depois, em paralelo: variedade (inimigos e torres), menu e ranking, audio.

## 7. Regras de trabalho

- **Branch por tarefa**, nome `pessoa/assunto` (ex.: `fabio/torre-base`). Nunca commitar direto no `main`.
- O `main` e protegido: so entra por **Pull Request**, e o **Rafael** aprova e faz o merge (arquivo `.github/CODEOWNERS`).
- PR pequeno, ligado a issue (`Closes #N`), com a mensagem dizendo o que mudou e como testar.
- **Cena e prefab compartilhados**: avisar no grupo antes de editar. Montar objetos como prefab proprio e so arrastar para a cena.
- **Sempre commitar os `.meta`.** Nao renomear nem mover arquivos fora do Unity.
- `git lfs install` uma vez, antes de clonar (PNG e audio em LFS).
- Importar sprites pelas regras de `ARTE.md`: PPU 64, filtro Point, sem compressao.
- Nunca usar Scale do Transform para ajustar tamanho de sprite.
- Dados de balanceamento em `ScriptableObject`, nao no codigo.
- Sem assets de terceiros com copyright.
- Commits sem a linha `Co-Authored-By` (trabalho avaliado).
- Cada um precisa conseguir **explicar o proprio codigo** na banca: prefira simples e comentado onde nao e obvio.

## 8. Fora do escopo

Realidade virtual, multiplayer, salvamento em nuvem, 4a estacao (so se sobrar tempo e a arte estiver pronta).
