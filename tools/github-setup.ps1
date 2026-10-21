# Configura o GitHub do projeto: rotulos, milestones, issues, quadro kanban e protecao do main.
# Requer: gh (GitHub CLI) logado com os escopos repo e project:
#   gh auth login -s project
# Rodar uma vez, na raiz do projeto:  powershell -File tools/github-setup.ps1
$ErrorActionPreference = "Stop"
$repo = "RafaelMaurina/Tower-Defense"
$dono = "RafaelMaurina"

# ---- rotulos
$rotulos = @{
  "arte-audio" = "e99695"; "combate" = "d93f0b"; "jogo-ui" = "0e8a16"
  "bonus" = "5319e7"; "bug" = "b60205"; "infra" = "cccccc"
}
foreach ($n in $rotulos.Keys) { gh label create $n --color $rotulos[$n] --repo $repo --force | Out-Null }

# ---- milestones
$marcos = @(
  @("M1 - Nivel 1 jogavel", "2026-11-03"), @("M2 - Conteudo completo", "2026-11-10"),
  @("M3 - Integrado e testado", "2026-11-17"), @("Entrega", "2026-11-24"), @("Bonus - A Prova Final", "2026-11-24"))
foreach ($m in $marcos) {
  gh api "repos/$repo/milestones" -f title="$($m[0])" -f due_on="$($m[1])T23:59:59Z" 2>$null | Out-Null
}

# ---- issues: titulo | rotulo | marco | responsavel
$M1="M1 - Nivel 1 jogavel"; $M2="M2 - Conteudo completo"; $M3="M3 - Integrado e testado"; $E="Entrega"; $B="Bonus - A Prova Final"
$issues = @(
  # infra e contratos
  @("Fechar contratos entre combate e jogo (interfaces, ScriptableObjects vazios)", "infra", $M1),
  @("Proteger o main e ligar o CODEOWNERS", "infra", $M1),
  @("Compartilhar o repositorio com rafaelrieder", "infra", $M3),
  # Fabio: combate
  @("Enemy: andar pelo WaypointPath por distancia, vida e recompensa", "combate", $M1),
  @("EnemyRegistry: lista de inimigos vivos para as torres consultarem", "combate", $M1),
  @("Tower base: alcance, cadencia e escolha de alvo", "combate", $M1),
  @("Projectile com mira preditiva", "combate", $M1),
  @("Torre Espantalho (3 niveis)", "combate", $M1),
  @("Torre Metralhadora de Uvas (3 niveis)", "combate", $M2),
  @("Torre Canhao de Abobora (3 niveis)", "combate", $M2),
  @("Torre Colmeia (3 niveis)", "combate", $M2),
  @("Torre Tufao e pastor alemao (3 niveis)", "combate", $M2),
  @("Inimigos: corvo, guaxinim, javali", "combate", $M2),
  @("Inimigo gafanhoto (salta por distancia)", "combate", $M2),
  @("Inimigo raposa", "combate", $M2),
  @("Inimigo morcego (voa) e regra de torres que atingem aereos", "combate", $M2),
  @("Inimigo pato (ziguezague)", "combate", $M2),
  @("Pooling de projeteis e inimigos", "combate", $M3),
  @("Ordem de desenho de inimigos e torres pela formula do ARTE.md", "combate", $M1),
  # Joao: jogo e interface
  @("GameManager: estados, vida da base, dinheiro", "jogo-ui", $M1),
  @("WaveSpawner: ler WaveData e instanciar inimigos nos caminhos", "jogo-ui", $M1),
  @("Construir, melhorar e vender torre no canteiro", "jogo-ui", $M1),
  @("HUD: moedas, vida, onda atual, botoes de torre", "jogo-ui", $M1),
  @("Base: detectar inimigo que chegou e tirar vida", "jogo-ui", $M1),
  @("Menu inicial e escolha de dificuldade", "jogo-ui", $M2),
  @("Ranking local e tela de informacoes", "jogo-ui", $M2),
  @("Troca entre os 3 niveis e carregamento de cena", "jogo-ui", $M2),
  @("Dificuldade por horario do dia (manha, por do sol, noite) so com iluminacao", "jogo-ui", $M3),
  @("Poderes consumiveis: apicultor e fazendeiro com forcado", "jogo-ui", $M3),
  @("Tela de vitoria, derrota e pausa", "jogo-ui", $M2),
  @("Balanceamento das ondas dos 3 niveis", "jogo-ui", $M3),
  # Rafael: arte e audio
  @("Sprites: primeiro inimigo, primeira torre e projetil (para destravar o grupo)", "arte-audio", $M1),
  @("Sprites: corvo, guaxinim, javali", "arte-audio", $M2),
  @("Sprites: gafanhoto, raposa, morcego, pato", "arte-audio", $M2),
  @("Sprites: 5 torres com 3 niveis cada e pastor alemao", "arte-audio", $M2),
  @("Sprites: projeteis (uva, abobora, abelha, rajada)", "arte-audio", $M2),
  @("Sprite do celeiro (base de cada nivel)", "arte-audio", $M2),
  @("Icones de UI: moedas, vida, botoes de torre, poderes", "arte-audio", $M2),
  @("Canteiro sem fundo e texturas do chao (4 variacoes)", "arte-audio", $M3),
  @("Redesenhar poco e cerca no dobro do tamanho", "arte-audio", $M3),
  @("VFX: explosao, acerto, morte, moeda, upgrade", "arte-audio", $M3),
  @("SFX: tiros, acertos, mortes, construir, upgrade, vender, vida perdida", "arte-audio", $M2),
  @("SFX: vitoria e derrota; musica de menu e de fase", "arte-audio", $M3),
  @("Mixagem: volume de musica e efeitos separados", "arte-audio", $M3),
  # entrega
  @("Build Windows testada em outra maquina", "infra", $M3),
  @("README com link do executavel e do video", "infra", $E),
  @("Gravar e editar o video de demonstracao", "infra", $E),
  @("Ensaiar a apresentacao (10 min + 5 min de perguntas)", "infra", $E),
  # bonus
  @("Bonus: cena do campus a noite", "bonus", $B),
  @("Bonus: inimigos AFD e AFN (AFN se divide no primeiro dano)", "bonus", $B),
  @("Bonus: boss em 3 fases (Chomsky)", "bonus", $B)
)
$urls = @()
foreach ($i in $issues) {
  $u = gh issue create --repo $repo --title $i[0] --label $i[1] --milestone $i[2] --body "Ver PLANEJAMENTO.md (contratos e regras) e DIVISAO.md."
  $urls += $u
}

# ---- quadro kanban (aba Projetos)
$proj = gh project create --owner $dono --title "Tower Defense" --format json | ConvertFrom-Json
gh project link $proj.number --owner $dono --repo $repo | Out-Null
foreach ($u in $urls) { gh project item-add $proj.number --owner $dono --url $u | Out-Null }
Write-Host "Quadro: $($proj.url)"

# ---- protecao do main: so por PR, com aprovacao do dono do codigo (Rafael)
$regra = @{
  required_status_checks = $null
  enforce_admins = $false
  required_pull_request_reviews = @{ required_approving_review_count = 1; require_code_owner_reviews = $true; dismiss_stale_reviews = $true }
  restrictions = $null
  allow_force_pushes = $false
  allow_deletions = $false
} | ConvertTo-Json -Depth 5
$regra | gh api -X PUT "repos/$repo/branches/main/protection" --input - | Out-Null
Write-Host "main protegido."
