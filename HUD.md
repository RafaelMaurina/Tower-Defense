# HUD da Colheita

Os PNGs ficam em `Assets/_Project/Resources/hud` para carregamento pelo GameHUD em runtime. Incluem molduras, bases de botao, pares normal/selecionado e icones isolados com transparencia. Importe como Sprite (Single), Point, sem mipmaps e sem compressao. Preserve os arquivos .meta.

Os botoes de melhoria, venda e das tres torres possuem desenhos e textos proprios gravados nos sprites, com pares normal/selecionado. Saldo, vida, rodada, precos e atributos continuam dinamicos. A moldura especifica de pausa inclui JOGO PAUSADO. O HUD superior usa a arte nova em `Assets/_Project/Resources/hud/v2`: moldura de recursos sem divisorias, placa de acao, base quadrada e icones de vida, moedas, rodadas, pausa, continuar, configuracoes e avancar automaticamente. Os numeros permanecem dinamicos e sem sombra.

## Comportamento

- Barra superior: moldura de recursos centralizada, com espaço para vida, moedas e rodada sem encostar nos ornamentos das bordas. Cada valor tem o icone a esquerda. `Iniciar onda N/total` fica a esquerda e inicia cada rodada manualmente. A faixa fica abaixo da cerca do mapa e preserva a proporcao dos sprites em 1920x1080.
- No canto superior direito, da esquerda para a direita: pausa/play congela ou retoma a simulacao sem bloquear construcao, melhoria ou venda; um unico botao de velocidade alterna 1x, 1,5x e 2x; engrenagem abre o submenu JOGO PAUSADO existente. Abaixo deles, `Auto: ligado/desligado` alterna o inicio automatico das proximas ondas. A pausa de construcao e independente da pausa dos submenus, de modo que fechar um painel nao cancela a pausa escolhida pelo jogador. Intervalos do spawner, movimento e ataques seguem o tempo escalado; a UI continua responsiva.

`Iniciar onda` e `Auto` usam a mesma placa de acao; Pausa, Velocidade e Configuracoes usam a base quadrada, com icones ou texto dinamicos. Ao pausar a simulacao, o icone muda para play. A mesma arte recebe um realce verde quando o cursor passa sobre ela ou quando Auto esta ligado; durante uma onda, Iniciar onda fica cinza. Os submenus de torres e de pausa continuam com a arte anterior.

As dez imagens `v2` foram geradas pela ferramenta integrada de imagem e copiadas para o projeto. Direcao dos prompts: pixel art feita a mao como em Aseprite/Piskel, madeira de carvalho em tons quentes, centro marrom escuro, contornos de pixels deliberados, pequenos detalhes de trigo e folhas, transparencia fora das pecas; sem texto, numeros, icones extras, pintura suave ou 3D. Moldura de recursos: uma placa horizontal continua. Placa de acao: botao horizontal sem texto. Base quadrada: centro vazio para icone dinamico. Icones: coracao vermelho, moeda dourada com trigo, espiga dourada, duas barras de pausa, triangulo verde de play, engrenagem dourada e setas verdes de avancar. `GameHUD.NewHudArt` ajusta apenas o retangulo de exibicao das imagens geradas para eliminar margens transparentes, preservando os arquivos originais.
- Pausa: Continuar, Recomecar nivel, Voltar ao menu e seta Voltar.
- Construcao: clicar num canteiro abre uma lista com rolagem das cinco torres, ordenadas pelo custo inicial lido de `TowerData`. Cada bloco mostra retrato, nome, alvo e preco. Clicar abre a ficha com dano, alcance, ataques por segundo, capacidade de atingir aereos e botao Construir. A ficha pode ser aberta sem saldo; somente Construir fica indisponivel. Voltar retorna a lista. A lista e a ficha pausam o tempo sem bloquear a compra; ao fechar, a velocidade anterior volta.
- Torre construida: titulo e nivel no alto da ficha; logo abaixo, icones de dano, alcance, tiros por segundo e capacidade de atingir aereos. Melhorar, Vender e prioridade de alvo ficam na mesma linha, com custo, reembolso e modo de mira visiveis. Melhorar permanece na linha, desativado, no nivel maximo. A seta Voltar fica na parte inferior. Aereos SIM significa que a torre tambem atinge terrestres; NAO significa apenas terrestres.
- Ao passar o mouse sobre uma torre, um circulo azul indica seu alcance. O painel da torre ou a ficha de construcao mostra o alcance em dourado; o circulo desaparece ao fechar. O painel de upgrade alterna entre alvo `Primeiro` (mais avancado no caminho) e `Mais vida`.
- Cada inimigo mostra uma barra de vida sobre o sprite. O preenchimento usa `Enemy.Health / EnemyData.health`: verde acima de 60%, amarelo entre 30% e 60%, vermelho em 30% ou menos. O boss usa uma barra mais larga. A barra acompanha cura, dano, divisao do AFN e reinicializacao pelo Pool.
- Botoes verdes ao passar o mouse ou selecionar pelo teclado, escurecidos durante clique e apagados quando o saldo nao basta. Nenhuma animacao.
- Voltar ao menu restaura Time.timeScale para 1.

## Conferencia no Unity

Abra Menu, entre em Play, escolha Jogar e uma dificuldade. Na aba Game selecione Full HD (1920x1080): Free Aspect muito pequeno dispara avisos da Pixel Perfect Camera e pode cortar o mapa. Clique Pausar e Continuar; clique num canteiro, role a lista ate Colmeia e Canhao, abra a ficha, volte e construa. `GerarNiveisAoAbrir` atualiza o catalogo e as ondas nos prefabs locais ja gerados sem refazer os mapas. Os tres assets `Wave_Teste_*` em `Data/Waves` oferecem uma rodada provisoria por nivel, com Corvo, Morcego, Guaxinim e Javali. Morcego permite conferir o aviso de aereos e as torres que o atingem. Custos, danos e composicao das ondas ainda exigem balanceamento.

## Integracao com o combate do Fabio

O HUD le nome, custo, sprite, dano, alcance, cadencia e `hitsFlying` diretamente de `TowerData`. O canteiro chama `BuildPlot.Build`, que cobra o custo e instancia `towerPrefab`; `Tower.Init(data)` inicia o combate. Novas torres entram automaticamente na lista quando o `TowerData` tem `towerPrefab` e ao menos um nivel. Os assets da Colmeia e do Tufao ja apontam para seus prefabs. O desbloqueio permanente do Tufao apos vencer no Dificil, previsto em `PLANEJAMENTO.md`, ainda precisa de implementacao; ele fica disponivel aqui para testes de integracao.

## Origem da arte

Arte original gerada com a ferramenta integrada de imagem. Direcao: pixel art de fazenda, madeira de carvalho dourada, centro marrom escuro, espigas e folhas verdes, contorno escuro, vista frontal, transparencia verdadeira e sem palavras. Foram geradas molduras de pausa e status, placa normal de madeira, placa selecionada verde e uma grade de 12 icones (moeda, coracao, espiga, pausa, morcego, espadas, alvo, ampulheta, melhoria, bolsa, seta voltar, continuar).

Os PNGs finais estao no repositorio e nao dependem dos scripts locais de extracao nem de acesso ao gerador. Os prompts das revisoes anteriores estao em `HUD-PROMPTS.md`; a arte superior atual esta documentada em `HUD-ART-V2.md`.
