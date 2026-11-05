# HUD da Colheita

Os PNGs ficam em `Assets/_Project/Resources/hud` para carregamento pelo GameHUD em runtime. Incluem duas molduras, bases de botao, oito pares normal/selecionado e doze icones isolados com transparencia. Importe como Sprite (Single), Point, sem mipmaps e sem compressao. Preserve os arquivos .meta.

Os botoes de pausa, melhoria, venda e das tres torres possuem desenhos e textos proprios gravados nos sprites, com pares normal/selecionado. Saldo, vida, rodada, precos e atributos continuam dinamicos. A moldura especifica de pausa inclui JOGO PAUSADO. A barra de recursos nao tem divisorias; a pausa usa duas barras nativas de UI com destaque dourado.

## Comportamento

- Barra superior: moedas, vida, rodada e pausa.
- Pausa: Continuar, Recomecar nivel, Voltar ao menu e seta Voltar.
- Construcao: clicar num canteiro abre uma lista com rolagem das cinco torres, ordenadas pelo custo inicial lido de `TowerData`. Cada bloco mostra retrato, nome, alvo e preco. Clicar abre a ficha com dano, alcance, ataques por segundo, capacidade de atingir aereos e botao Construir. A ficha pode ser aberta sem saldo; somente Construir fica indisponivel. Voltar retorna a lista.
- Torre construida: dano, alcance, tiros por segundo e capacidade de atingir aereos. Aereos SIM significa que a torre tambem atinge terrestres; NAO significa apenas terrestres.
- Botoes verdes ao passar o mouse ou selecionar pelo teclado, escurecidos durante clique e apagados quando o saldo nao basta. Nenhuma animacao.
- Voltar ao menu restaura Time.timeScale para 1.

## Conferencia no Unity

Abra Menu, entre em Play, escolha Jogar e uma dificuldade. Na aba Game selecione Full HD (1920x1080): Free Aspect muito pequeno dispara avisos da Pixel Perfect Camera e pode cortar o mapa. Clique Pausar e Continuar; clique num canteiro, role a lista ate Colmeia e Canhao, abra a ficha, volte e construa. `GerarNiveisAoAbrir` atualiza o catalogo e as ondas nos prefabs locais ja gerados sem refazer os mapas. Os tres assets `Wave_Teste_*` em `Data/Waves` oferecem uma rodada provisoria por nivel, com Corvo, Morcego, Guaxinim e Javali. Morcego permite conferir o aviso de aereos e as torres que o atingem. Custos, danos e composicao das ondas ainda exigem balanceamento.

## Integracao com o combate do Fabio

O HUD le nome, custo, sprite, dano, alcance, cadencia e `hitsFlying` diretamente de `TowerData`. O canteiro chama `BuildPlot.Build`, que cobra o custo e instancia `towerPrefab`; `Tower.Init(data)` inicia o combate. Novas torres entram automaticamente na lista quando o `TowerData` tem `towerPrefab` e ao menos um nivel. Os assets da Colmeia e do Tufao ja apontam para seus prefabs. O desbloqueio permanente do Tufao apos vencer no Dificil, previsto em `PLANEJAMENTO.md`, ainda precisa de implementacao; ele fica disponivel aqui para testes de integracao.

## Origem da arte

Arte original gerada com a ferramenta integrada de imagem. Direcao: pixel art de fazenda, madeira de carvalho dourada, centro marrom escuro, espigas e folhas verdes, contorno escuro, vista frontal, transparencia verdadeira e sem palavras. Foram geradas molduras de pausa e status, placa normal de madeira, placa selecionada verde e uma grade de 12 icones (moeda, coracao, espiga, pausa, morcego, espadas, alvo, ampulheta, melhoria, bolsa, seta voltar, continuar).

`tools/ExtractHudPairs.ps1` separa a revisao atual conforme `tools/hud-sources-v2.json`, mantendo cada par com dimensoes iguais. O script ExtractHudSprites e o manifesto hud-sources.json sao historicos da primeira revisao; nao os execute para substituir a arte atual. Prompts completos em HUD-PROMPTS.md. Os PNGs finais estao no repositorio e nao dependem de executar o script nem de acesso ao gerador. O caminho fonte do script e local a esta geracao.
