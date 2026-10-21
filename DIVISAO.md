# Divisao de trabalho: Defesa da Colheita

Apresentacao final: **24/11/2026**. Sprints em aula: 03/11, 10/11 e 17/11.
Cada um precisa ter commits proprios e saber explicar o proprio codigo na banca
(2,0 pontos da nota sao GitHub + dominio individual).

Pessoas: **Rafael** (arte e som), **Fabio** (combate) e **Joao** (jogo e interface).
Trocar os nomes se a divisao mudar.

## Rafael: arte e audio (1,0 + 1,0 + variedade de objetos)

**Sprites** (64x64, PPU 64, paleta em `paleta-do-trio.gpl`, regras em `ARTE.md`)
- [ ] Inimigos: corvo, guaxinim, javali, gafanhoto, raposa, morcego, pato
- [ ] Torres, 3 niveis cada: espantalho, uvas, canhao, colmeia, tufao (+ o pastor alemao)
- [ ] Projeteis: uva, abobora, abelha, rajada do tufao
- [ ] Celeiro (a base de cada nivel; o espaco na cerca ja esta reservado)
- [ ] Grama e estrada de inverno, se a quarta estacao entrar
- [ ] Texturas do chao (4 variacoes por tile) e canteiro sem fundo
- [ ] Redesenhar poco e cerca no dobro do tamanho (hoje sao ampliacao automatica)
- [ ] Icones de UI: moedas, vida, botoes de torre, poderes
- [ ] Fase bonus: asfalto, postes, placas AFD/AFN, boss em 3 fases
- [ ] Efeitos: explosao, acerto, morte, moeda, upgrade (VFX)

**Audio**
- [ ] SFX: tiro de cada torre, acerto, morte de cada inimigo, construir, upgrade, vender, vida perdida, vitoria, derrota
- [ ] Musica: menu, fase (manha, por do sol, noite), boss
- [ ] Mixagem simples: volume de musica e de efeitos separados

## Fabio: combate (2,5 pontos: movimentacao, torres, projeteis)

Trabalha em `Assets/_Project/Scripts/Enemies`, `Towers` e `Projectiles`.

- [ ] `Enemy`: anda pelo `WaypointPath` por **distancia** (ja pronto), vida, velocidade, recompensa
- [ ] Os 7 inimigos e seus comportamentos: gafanhoto salta, morcego voa, pato faz ziguezague, raposa, javali, corvo, guaxinim
- [ ] `Tower` base: alcance, cadencia, escolha de alvo (primeiro, mais forte, mais proximo)
- [ ] As 5 torres e os **3 niveis** de upgrade de cada
- [ ] Projeteis com **mira preditiva** (o pato existe para quebrar isso)
- [ ] Torres que acertam so terrestres vs. as que acertam aereos
- [ ] Tufao: invoca o pastor alemao corpo a corpo
- [ ] Ordem de desenho: inimigos e torres usam `100 + round((8 - y) * 10)` (ver `ARTE.md`)
- [ ] Pooling de projeteis e inimigos (item de performance da nota)

## Joao: jogo e interface (2,5 + 0,5 + 0,5)

Trabalha em `Scripts/Game`, `UI` e `Waves`.

- [ ] `GameManager`: estados (menu, jogando, pausa, vitoria, derrota), vida da base, dinheiro
- [ ] `WaveSpawner`: ondas por nivel, ligado ao `WaypointPath` (2 entradas no nivel 3)
- [ ] Construcao: clicar num canteiro, escolher a torre, preco, upgrade, vender
- [ ] Gerenciamento de alvos e colisoes (o gatilho da base ja existe em `Base`)
- [ ] HUD: moedas, vida, onda atual, botoes de torre
- [ ] Menu inicial, escolha de dificuldade, ranking (arquivo local) e tela de informacoes
- [ ] Dificuldade = horario do dia (manha, por do sol, noite), **so por iluminacao**, nunca tileset novo
- [ ] Poderes consumiveis (apicultor e fazendeiro com forcado)
- [ ] Troca entre os 3 niveis e carregamento de cena

## Compartilhado

- [ ] **Rafael**: integra sprites e audio no Unity (as importacoes ja seguem `ARTE.md`)
- [ ] **Fabio + Joao**: definem juntos as interfaces entre torre, inimigo e onda antes de codar
- [ ] **Todos**: README com link do executavel e do video (`README.md` ja tem os placeholders)
- [ ] **Todos**: executavel Windows (build) testado em outra maquina
- [ ] **Joao** grava o video de demonstracao; **Rafael** edita e coloca o som
- [ ] Repositorio compartilhado com o usuario `rafaelrieder`
- [ ] Ensaio da apresentacao: 10 min de demo + 5 min de perguntas; cada um explica a propria parte

## Marcos sugeridos

| Data | O que precisa estar pronto |
|---|---|
| 03/11 (sprint) | Jogo jogavel no nivel 1: 1 inimigo, 2 torres, ondas, vida, dinheiro, HUD basico |
| 10/11 (sprint) | 7 inimigos, 5 torres com 3 niveis, os 3 niveis, menu, ranking, SFX principais |
| 17/11 (sprint) | Tudo integrado, balanceado, VFX, musica, build testada |
| 24/11 | Apresentacao |

## Fase bonus "A Prova Final" (so se o resto estiver fechado)

Campus a noite, boss em 3 fases pela hierarquia de Chomsky, inimigos AFD/AFN (o AFN se
divide em 2 no primeiro dano). Tom de **homenagem**, nunca de chacota. Dividir assim:
arte e audio com o Rafael, boss e fases com o Fabio, cena, ligacao no menu e dialogo com o Joao.

## Regras de trabalho

- Cada um mexe nas **proprias pastas** e em **prefabs proprios**, para evitar conflito de merge
- Cenas e prefabs em YAML conflitam facil: avisar no grupo antes de editar uma cena compartilhada
- Fazer `git lfs install` antes de clonar (os PNGs e o audio ficam em LFS)
- Commits pequenos e frequentes, com mensagem que diga o que mudou
- Nada de assets de terceiros com copyright (Stardew, Spriters Resource e afins)
