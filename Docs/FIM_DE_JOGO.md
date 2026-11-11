# Telas de fim de jogo

As telas de vitória e derrota usam o mapa da partida ao fundo, com escurecimento suave. As artes novas ficam em `Assets/_Project/Resources/hud/fianis_submenu` (nome solicitado), com exterior transparente, importação Sprite Single, Point, sem compressão nem mipmaps e PPU 32.

`GameHUD.Result` monta um painel com proporção fixa 1,65:1, estatísticas à esquerda, torre de maior dano à direita e os botões existentes Recomeçar, Menu e Voltar embaixo. Os textos e valores são dinâmicos; somente Vitória/Derrota estão desenhados na moldura. Voltar mantém o comportamento anterior de retornar ao menu, sem retomar uma partida encerrada.

`GameResult.topDamageSprite` é uma referência visual não serializada, capturada quando a torre assume a liderança de dano. A arte permanece disponível mesmo se ela for vendida. O ranking mantém seu formato de persistência atual. Uma partida sem dano registrado mostra uma mensagem no lugar do retrato.

## Artes e direção dos prompts

Geradas pela ferramenta integrada de imagens, com as quatro referências fornecidas pelo João. Uma imagem por sprite, exterior transparente, arte de fazenda desenhada em pixel art, contornos definidos, sem cenário externo, números ou botões incorporados.

- `moldura_vitoria`: painel horizontal, celeiro e título VITÓRIA!, trigo, folhas verdes, uvas e abóboras nas bordas; interior amplo de madeira escura.
- `moldura_derrota`: painel horizontal, título DERROTA!, corvos, folhas de outono e cipós roxos; interior vinho escuro, tom melancólico sem horror.
- `torre_vitoria` e `torre_derrota`: molduras verticais vazias, combinando com cada resultado, para retrato, nome e dano dinâmicos.
- `icone_pontuacao`: troféu dourado isolado.
- `icone_rodadas_final`: calendário creme e laranja isolado.
- `icone_derrotados`: caveira creme simples, isolada.

## Conferência

Concluir uma partida em vitória e outra em derrota. Conferir pontuação, rodada, contagem de inimigos, nome/sprite/dano da torre e estados dos três botões. Conferir também derrota sem torre causando dano e torre líder vendida antes do fim. Testar Full HD e janela menor; as proporções do painel são preservadas. Recomeçar reinicia o nível; Menu e Voltar retornam ao menu principal.
