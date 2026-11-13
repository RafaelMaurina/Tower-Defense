# Ondas e integração do bônus — #32 e #76

## Entrega

Branch `joao/ondas-bonus`, baseada em `joao/submenu-melhorar` (PR #95). Inclui main até 4573acd. A seleção completa de fases continua em trabalho futuro; a campanha mantém Primavera → Verão → Outono.

### #32 — ondas

Cada estação tem três assets WaveData, ordenados pelo novo campo `order` e, em empate, pelo nome. Os assets iniciais e seus GUIDs foram preservados; sufixos `_02` e `_03` são as rodadas seguintes. O GameBootstrap filtra por `levelScene` antes de ordenar. Não depende da ordem retornada por AssetDatabase.

| Fase | Onda 1 | Onda 2 | Onda 3 | Total |
|---|---|---|---|---|
| Primavera | 6 corvos, 2 guaxinins | 5 corvos, 4 guaxinins, 2 morcegos | 5 guaxinins, 3 morcegos, 1 javali | 28 |
| Verão | 5 guaxinins, 3 raposas | 5 corvos, 4 raposas, 3 morcegos | 5 raposas, 5 guaxinins, 2 javalis | 32 |
| Outono | 4 guaxinins, 4 gafanhotos, 3 patos | 4 raposas, 4 patos, 4 morcegos | 5 gafanhotos, 4 raposas, 4 patos, 2 javalis | 38 |

`Enemy_Rato` / `Inimigo_Rato` são os nomes atuais dos arquivos do **Pato** (displayName e zigue-zague). Foram reutilizados sem renomear assets do Fábio. Raposa começa no Verão; Gafanhoto e Pato no Outono, conforme comentários da #32. Outono usa explicitamente os caminhos A e B. As entradas de cada onda são sequenciais, como no spawner original.

Quantidades e intervalos são uma primeira proposta, não balanceamento final aprovado. Nenhum atributo de inimigo/torre foi alterado. Início manual, auto-start, pausa e velocidade existentes foram mantidos.

### #76 — fluxo e progresso

- Vencer o Outono salva `DefesaDaColheita.BonusUnlocked.v1` em PlayerPrefs e termina a campanha com a tela de vitória habitual. Derrota e conclusão de outras cenas não liberam o bônus.
- O menu oferece **A Prova Final**, bloqueado até esse desbloqueio. Depois, o jogador confirma a dificuldade na tela existente e inicia uma partida nova no bônus. A entrada checa novamente o desbloqueio e a disponibilidade da cena.
- Nome definitivo de integração: `Nivel_4_Campus`, igual no gerador, no progresso e em `Wave_Bonus_Prova_Final.levelScene`. O gerador registra essa cena no Build Settings e conecta catálogo de torres/ondas, Caminho_A, canteiros e base.
- O campus usa iluminação noturna em qualquer dificuldade. A geometria é funcional, mas reaproveita arte da fazenda: **não conclui o acabamento da #50**.
- Bônus opcional, fora da transição automática após Outono. Recomeçar reinicia o mesmo nível com saldo normal e preserva o desbloqueio. Voltar ao menu também o preserva.
- A onda bônus mantém 4 AFD, 4 AFN e 1 Chomsky, pelo caminho A. Invocados e cópias continuam contabilizados pelo WaveSpawner existente.
- Ranking do bônus separado em `DefesaDaColheita.Ranking.Bonus.v1`, dez entradas, acessível por “Ranking do bônus”. Campanha mantém a chave anterior e seus dados. Ambos usam a fórmula de pontuação existente.

## Para a equipe continuar

- **Fábio:** inimigos novos já entram nas ondas; ajustar `count`, `spawnInterval`, `pauseAfter` nos assets. Preservar `order`, `levelScene` e as referências de prefab/EnemyData. Boss e comportamentos de combate não foram alterados.
- **Rafael:** acabamento do campus (#50), sprites e sons podem se apoiar no nome `Nivel_4_Campus`; preservar Caminho_A, base, canteiros e GameBootstrap. O gerador mantém cenas já existentes, sem sobrescrever trabalho manual.
- **João:** seleção completa de fases e barra/diálogo do boss (#77) continuam separados desta entrega. A seleção futura pode consultar `CampaignProgress.BonusUnlocked`.

## Validação e teste antes do merge/build

Compilação de Assembly-CSharp e Assembly-CSharp-Editor sem erros (quatro avisos existentes de GroundFx, NightLights e PixelPerfectCamera). Checagem dos dez assets de ondas aprovada: GUIDs e fileIDs resolvem, EnemyData coincide com o prefab, contagens positivas, caminhos válidos, ordem sem repetição e presença dos inimigos previstos.

Teste em Play ainda pendente; a última tentativa de abrir Unity nesta máquina falhou por licença indisponível. Não fechar #32 como balanceamento final nem considerar #46 validada só pela compilação.

1. Abrir o Unity e aguardar a geração automática. Se necessário usar **Defesa da Colheita > Gerar niveis** (sem apagar/refazer). Confirmar Menu + quatro níveis no Build Settings. A atualização de catálogos também roda quando somente o campus estava faltando.
2. Jogar três ondas por estação, manual e auto-start, incluindo pausa/velocidade; conferir raposa no Verão e pato/gafanhoto nos dois caminhos do Outono.
3. Com progresso limpo, bônus bloqueado. Derrota não deve desbloquear. Vencer Outono deve mostrar vitória; ao voltar ao menu, bônus disponível. Fechar/reabrir o jogo deve manter esse estado.
4. Entrar no bônus, conferir AFD, divisão do AFN e boss com três fases; a vitória só ocorre quando todos terminarem. Boss na base mantém dano 999 (derrota).
5. Testar Recomeçar e retorno ao menu na vitória e derrota do bônus; conferir que os rankings ficam separados e o desbloqueio não é perdido.
6. Fazer os ajustes de economia/dificuldade com jogo real antes da build final Windows e do teste em outra máquina (#46).
