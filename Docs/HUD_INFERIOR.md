# HUD inferior de torres e construção

A barra é criada por `GameHUD.CreateBottomTowerBar`, centralizada na parte inferior e limitada a aproximadamente 25% da altura da tela. A carta central fica no mesmo eixo do celeiro do HUD superior. Cada torre tem sua moldura com nome completo; os nomes longos usam duas linhas. Não há mensagem de orientação quando nenhuma seleção está ativa. A barra é ocultada nos submenus e ao encerrar a partida. O catálogo vem dos canteiros da cena, sem duplicatas, ordenado pelo custo inicial.

Cada carta mostra nome, sprite de nível 1 (com fallback para o prefab) e custo de `TowerData`. A moeda é `hud_superior/icone_moedas`, já utilizada no HUD superior. Textos e valores são dinâmicos. O preço fica vermelho quando falta saldo. A carta selecionada permanece destacada, com o realce e sons existentes.

Selecionar uma carta mantém o tipo de torre ativo e mostra um fantasma com alcance seguindo o cursor, encaixado no canteiro sob o mouse. Clicar em um canteiro vazio constrói e limpa a seleção, o destaque e a prévia. Também é possível arrastar a carta e soltá-la sobre um canteiro.

Clicar primeiro em um canteiro vazio o destaca com um círculo ciano. Passar pelas cartas mostra temporariamente o retrato e alcance daquela torre no canteiro; clicar confirma a construção. A prévia é verde quando válida e vermelha quando falta saldo, o local está ocupado ou o catálogo do canteiro não aceita a torre. Esc ou botão direito cancelam a seleção. A prévia não cria uma torre real e não causa dano. A cobrança e instanciação continuam centralizadas em `BuildPlot.Build`, que valida ocupação, catálogo, prefab e saldo. Clicar em uma torre já construída abre a melhoria e cancela a seleção de construção. Submenus ocultam a prévia; a pausa de construção do HUD continua permitindo colocar torres.

## Artes novas

PNG RGBA em `Assets/_Project/Resources/hud/hud_inferior`: `barra_torres.png` e `carta_torre.png`, acompanhados dos `.meta`. Sprite Single, Point, sem compressão/mipmaps, PPU 32. O carregamento recorta apenas margens externas, sem modificar os arquivos originais.

Geradas com a ferramenta integrada de imagens, usando a referência do João para composição e a moldura de ações do HUD superior para cor e acabamento. Prompts: barra horizontal de madeira mel com cantos reforçados, folhas discretas e centro marrom avermelhado sem divisórias; carta com placa creme vazia para nome, área central de madeira para retrato e placa creme vazia para custo. Sem textos, números, moedas ou torres incorporados nas imagens.

## Conferência no Play

Abrir uma fase e conferir as cinco torres, ordem de preço, retratos e valores. Testar carta → canteiro, canteiro → hover nas cartas → clique e arrastar → soltar. Confirmar uma única cobrança por construção, seleção limpa após construir, cancelamento, falta de saldo e canteiro ocupado. Confirmar que soltar sobre a UI não constrói. Conferir o centro do alcance na posição real da torre. Abrir pausa ou melhoria deve esconder a barra e a prévia; fechá-los deve restaurar a barra. Testar também com a pausa de construção ligada.
