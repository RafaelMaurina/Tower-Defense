# HUD inferior de torres e construção

A barra é criada por `GameHUD.CreateBottomTowerBar`, centralizada na parte inferior e limitada à faixa entre 0,6% e 16,5% da altura da tela, com largura máxima de 44%. A carta central fica no mesmo eixo do celeiro do HUD superior. Cada torre tem sua moldura com nome completo; os nomes longos usam duas linhas. Não há mensagem de orientação quando nenhuma seleção está ativa. A barra é ocultada nos submenus e ao encerrar a partida. O catálogo vem dos canteiros da cena, sem duplicatas, ordenado pelo custo inicial.

Cada carta mostra nome, sprite de nível 1 (com fallback para o prefab) e custo de `TowerData`. A moeda é `hud_superior/icone_moedas`, já utilizada no HUD superior. Textos e valores são dinâmicos. O preço fica vermelho quando falta saldo. A carta selecionada permanece destacada, com o realce e sons existentes.

Selecionar uma carta mantém o tipo de torre ativo e mostra um fantasma com alcance seguindo o cursor, encaixado no canteiro sob o mouse. Clicar em um canteiro vazio constrói e limpa a seleção, o destaque e a prévia. Também é possível arrastar a carta e soltá-la sobre um canteiro.

Clicar primeiro em um canteiro vazio o destaca com um círculo ciano. Passar pelas cartas mostra temporariamente o retrato e alcance daquela torre no canteiro; clicar confirma a construção. A prévia é verde quando válida e vermelha quando falta saldo, o local está ocupado ou o catálogo do canteiro não aceita a torre. Esc ou botão direito cancelam a seleção. A prévia não cria uma torre real e não causa dano. A cobrança e instanciação continuam centralizadas em `BuildPlot.Build`, que valida ocupação, catálogo, prefab e saldo. Clicar em uma torre já construída abre a melhoria e cancela a seleção de construção. Submenus ocultam a prévia; a pausa de construção do HUD continua permitindo colocar torres.

## Artes novas

PNG RGBA em `Assets/_Project/Resources/hud/hud_inferior`: `barra_torres.png` e `carta_torre.png`, acompanhados dos `.meta`. Sprite Single, Point, sem compressão/mipmaps, PPU 32. O importador limita a barra a 1024 px e a carta a 512 px. O recorte acompanha a resolução importada; os arquivos originais permanecem preservados. A carta mantém a proporção 754:1165 com AspectRatioFitter e preserveAspect. Apenas as cartas interceptam o mouse; o fundo decorativo não bloqueia cliques.

Geradas com a ferramenta integrada de imagens, usando a referência do João para composição e a moldura de ações do HUD superior para cor e acabamento. Prompts: barra horizontal de madeira mel com cantos reforçados, folhas discretas e centro marrom avermelhado sem divisórias; carta com placa creme vazia para nome, área central de madeira para retrato e placa creme vazia para custo. Sem textos, números, moedas ou torres incorporados nas imagens.

## Conferência no Play

Abrir uma fase e conferir as cinco torres, ordem de preço, retratos e valores. Testar carta → canteiro, canteiro → hover nas cartas → clique e arrastar → soltar. Confirmar uma única cobrança por construção, seleção limpa após construir, cancelamento, falta de saldo e canteiro ocupado. Confirmar que soltar sobre a UI não constrói. Conferir o centro do alcance na posição real da torre. Abrir pausa ou melhoria deve esconder a barra e a prévia; fechá-los deve restaurar a barra. Testar também com a pausa de construção ligada.

## Revisão do PR #95

- Removido o saldo de teste de 10.000 moedas no editor. Saldo inicial e dinheiro recebido seguem a configuração normal do GameManager.
- Removido o catálogo modal antigo, substituído pelas cartas. O raycast do cursor reutiliza a lista de resultados e PointerEventData.
- A dica usa "Selecionado: nome" e corrige acentos dos nomes exibidos.
- Em 16:9, com câmera em (15; 8,5) e orthographicSize 8,4375, o topo máximo da barra fica em y=2,846875. As estradas inferiores começam em y=3. O canteiro do Outono (20,3) e o da Primavera (12,4) ficam acima. O canteiro da Primavera (24,2) fica fora da largura da barra (x=8,4 a 21,6).
- Compilação C# validada sem erros. Teste visual/interativo no Play ainda pendente: o Unity local encerrou por licença indisponível. Revalidar Primavera, Verão e Outono em Full HD antes do merge.
- Caches, recuperação e ferramentas de arte locais foram retirados do histórico publicado do PR. O script compartilhado tools/github-setup.ps1, já existente na main, foi mantido.

## Artes das cartas — 14/11/2026

Seis PNGs em Resources/hud/hud_inferior: hud_inferior_fundo e hud_torre_espantalho, hud_torre_metralhadora_uvas, hud_torre_tufao, hud_torre_colmeia, hud_torre_canhao_abobora. Cinco cartas RGBA com canvas idêntico de 1122×1402; fundo 2048×682 copiado da referência sem redesenho. Mantidas as dimensões originais para preservar a referência (não são múltiplos de 16). Importação Single/Point, PPU 32, sem mipmaps/compressão, limites 1024 para cartas e 2048 para fundo.

Texto branco e acentuado incorporado no cabeçalho; Unity não sobrepõe outro nome. Centro vazio recebe sprite de nível 1; rodapé vazio recebe o ícone existente hud_superior/icone_moedas e custo atual do TowerData. Seleção, arrasto, prévia e sons continuam funcionando pelo código existente. O fundo e as cartas preservam a proporção no layout; a faixa inferior máxima não foi ampliada. Conferência visual em Play ainda pendente.

### Prompts usados (ferramenta integrada de geração de imagens)

Carta mestre, referência individual anexada: edição precisa, preservar silhueta de madeira laranja, trigo, folhas, placas creme, painel central marrom e proporções. Substituir nome por “Espantalho” em letras brancas pixel art com contorno marrom escuro. Remover moeda do rodapé para receber a moeda existente e preço dinâmico pela Unity. Sem torre ou preço incorporados. Exterior transparente, sem sombra externa.

Variantes, usando a carta mestre como referência: alterar somente o cabeçalho para “Metralhadora\nde Uvas”, “Tufão”, “Colmeia” ou “Canhão de\nAbóbora”, em branco com contorno escuro; manter dimensões, posições, ornamentos e rodapé vazio. Não redesenhar a moldura.

O fundo foi reutilizado diretamente do anexo. As cartas foram conferidas visualmente e têm as mesmas dimensões; a edição generativa pode produzir pequenas diferenças de pixels na textura entre variantes.


## HUD atual

A interface usa hud_conjunto_contraste.png com nomes brancos e preços dinâmicos. Largura, altura e deslocamento inferior são configuráveis no GameHUD; as cartas individuais e o fundo separado foram retirados do Resources em 15/11, porque o jogo usa só o conjunto e tudo em Resources entra na build (as prévias continuam em Docs/Arte). O indicador visual de canteiro selecionado foi retirado do GameHUD para não duplicar a implementação da equipe. As prévias de torre e alcance foram preservadas. Revalidar cobertura do mapa em Play após os ajustes de tamanho.
