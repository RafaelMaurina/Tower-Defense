# Arte: configuracao de importacao

Fica na raiz do projeto, fora de `Assets/`, para o Unity nao importar este arquivo.

## Densidade

**Pixels Per Unit = 64 em todo sprite.** 1 unidade de mundo = 1 tile de 64px.
Nunca use o Scale do Transform para corrigir tamanho: isso mistura pixels de
tamanhos diferentes na tela e e o que mais denuncia pixel art amadora. Se um
objeto ficou do tamanho errado, redesenhe no tamanho certo.

Tamanho da tela de desenho segue o tamanho real do objeto:
tile 64x64, cerca 32x32, e assim por diante. Telas diferentes nao sao problema
desde que a espessura do traco seja a mesma, e isso ja foi conferido neste conjunto.

## Configuracao gravada nos .meta

| Campo | Valor | Por que |
|---|---|---|
| Texture Type | Sprite (2D and UI) | |
| Pixels Per Unit | 64 | 1 tile = 1 unidade |
| Filter Mode | Point | o padrao Bilinear borra a arte |
| Compression | None | o padrao altera as cores da paleta |
| Generate Mip Maps | off | borra quando a camera afasta |
| Mesh Type | Full Rect | evita corte em tile |
| Wrap Mode | Clamp | |
| Pivot dos tiles | Center | |
| Pivot de props e cerca | Bottom Center | o objeto apoia no chao e ordena pelo pe |

Essas opcoes estao nos arquivos `.meta` ao lado de cada PNG, entao vao junto no
git e valem para o trio inteiro. Sprite novo entra pelas mesmas regras: o jeito
mais seguro e copiar um `.meta` existente ou registrar um Preset no
Project Settings > Preset Manager para a pasta `Assets/_Project/Art/`.

## Camera

Ortografica, `orthographicSize = 540 / 64 = 8.4375`, que da 1920x1080 de arte:
cada pixel da arte vira 1 pixel da tela e cabem 30 x 17 casas. Para travar a
nitidez, a Main Camera tem o componente **Pixel Perfect Camera** com Assets PPU 64
e Reference Resolution 1920x1080 (os niveis ja saem assim do gerador).

As demos (`DemoSprites`, `MontarMapa`) usam a mesma camera.

## Estacoes

Cada grama e uma estacao, e a cor e o que identifica:

| Arquivo | Cores base | Estacao |
|---|---|---|
| `grama_1` | `#5ac224` `#348808` `#99eb6f` | Primavera |
| `grama_2` | `#06702c` `#00a03a` `#56e289` | Verao |
| `grama_3` | `#92b121` `#5b7302` `#d4f269` | Outono |
| - | - | Inverno (a fazer) |

Cada estacao tem o conjunto completo: base, quatro bordas, quatro cantos, e as
versoes `_alpha`: grama que so termina, sem a borda de terra desenhada, para
sobrepor a outra camada.

**A estrada acompanha a estacao.** `estrada_grama_1` combina com `grama_1`, e
assim por diante: a franja de grama no pe do tile usa as cores daquela estacao.
Misturar `estrada_grama_1` com `grama_2` deixa a franja fora de tom.

`estrada` (sem sufixo) e o miolo da pista, sem franja, e serve para qualquer estacao.

A franja so existe na **base** dos tiles de estrada. Para a borda de cima da
pista, o Tile e espelhado no eixo Y por codigo (ver `MontarMapa.cs`), em vez de
exigir arte nova.

## Demos

- `Defesa da Colheita > Montar demo de sprites`: tiles, props e cerca isolados,
  dentro de um objeto `DEMO Sprites`
- `Defesa da Colheita > Montar pedaco de mapa`: mapa 30x17 com Tilemap de
  verdade, estrada atravessando, cerca e canteiros, dentro de `DEMO Mapa`

Para trocar a estacao do mapa, mude a constante `ESTACAO` no topo de
`MontarMapa.cs` para 1, 2 ou 3.

Apagar o objeto raiz desfaz qualquer uma das duas demos.

## Inimigos

Desenhados numa tela de **21x21** (um terco do tile) e ampliados 3x com Scale3x,
o mesmo grao do poco e da cerca. O original fica em `ArtSource/Inimigos` como
`.piskel`, com a animacao inteira, e o ampliado, com tela de 64x64 e pivo no pe,
em `Art/Sprites/Inimigos`.

No Piskel o desenho fica **sem contorno**. O contorno preto (`#201821`) de 1px e
aplicado depois de ampliar, para ficar fino na tela. Quatro quadros por bicho:
`inimigo_<nome>_1.png` ate `inimigo_<nome>_4.png`.

## Niveis

`Defesa da Colheita > Gerar niveis` cria os tres niveis prontos (sem inimigos),
com 30 x 17 casas cada:

| Nivel | Estacao | Caminho | Canteiros | Plantacao |
|---|---|---|---|---|
| 1 | Primavera | zigue-zague, 45 casas | 10 | cenoura 3x3 |
| 2 | Verao | serpentina, 59 casas | 12 | trigo 3x3 |
| 3 | Outono | 2 entradas que se juntam em (7,8), 52 casas | 12 | abobora 3x3 |

Cada nivel tem uma area de 4 x 4 casas reservada para o celeiro, encostada no
fim do caminho, e um objeto `Celeiro` vazio marcando onde o sprite entra.

Cada nivel vira um **prefab** em `Assets/_Project/Prefabs/Niveis` (o que a cena
de jogo vai instanciar) e uma **cena** em `Assets/_Project/Scenes` (para abrir,
dar Play e testar sozinho). Se ja existirem, o menu nao mexe neles; para gerar
de novo use `Gerar niveis (apagar e refazer)`, que pede confirmacao.

Esses prefabs e cenas **nao vao para o git**: sao milhares de linhas geradas. Ao
abrir o projeto, `GerarNiveisAoAbrir` cria os que estiverem faltando. Por isso:
mudanca de nivel e feita nos dados do `MontarNiveis.cs`, nunca editando o prefab
ou a cena a mao, e nenhum outro asset deve apontar direto para eles. Para usar
um nivel no jogo, carregue pelo nome (`Nivel_1_Primavera`).

Os dados dos niveis ficam no topo de `MontarNiveis.cs`, em coordenadas de casa
(x, y com y = 0 embaixo). O caminho e uma lista de pontos de virada; o primeiro
fica em x = -1, fora da tela, que e de onde os inimigos entram.

A estrada nos niveis e o tile `estrada` liso, e as bordas sao desenhadas pela
**grama**: cada casa de grama que encosta na estrada recebe a borda com terra
virada para ela, inclusive nos cantos. Isso funciona em curva, o que os tiles
`estrada_grama_N` nao fazem.

## Ordem de desenho

| Camada | sortingOrder |
|---|---|
| Chao (Tilemap) | 0 |
| Coisas deitadas no chao (canteiro, plantacao) | 1 |
| Coisas em pe (poco, cerca, cogumelo; inimigos e torres depois) | `100 + round((17 - y) * 10)` |

A formula faz quem esta mais embaixo na tela desenhar por cima. **Inimigos e
torres precisam usar a mesma formula**, senao atravessam a cerca ou somem atras
do poco.

## Pendencias conhecidas

- **Inverno**: falta o conjunto de grama e a estrada correspondente.
- **Celeiro**: a base de cada nivel ja tem o gatilho no lugar, mas sem arte.
- **Canteiro**: o fundo do `tile_canteiro` e opaco, nas cores da estrada
  (`#cf933b` `#ca8622`). Na grama ele parece um pedaco de estrada, e quando
  encosta na pista se funde com ela. Apagar esse fundo no Piskel (deixar
  transparente em volta do canteiro) resolve; a comparacao ja foi feita e
  fica bem melhor.
- **Poco e cerca**: os niveis usam `prop_poco_3x.png` e `cerca_lado_N_3x.png`,
  que sao os originais ampliados 3x por algoritmo (Scale3x). O poco ocupa 3x3
  casas e cada peca de cerca, 1,5 casa. E provisorio: o contorno ficou com 3px,
  mais grosso que o resto da arte. Redesenhar no Piskel numa tela do triplo do
  tamanho (poco 192x192, cerca 96x96), usando esses arquivos como base. Os
  originais nao foram alterados; portoes e cerca vertical continuam 1x.
- **Mapa vazio**: com 30 x 17 casas sobra muito gramado. Faltam arvores,
  arbustos, pedras e flores para preencher.
- **Canto interno**: onde a estrada faz curva, a casa de grama na diagonal do
  lado de FORA da curva nao encosta em estrada nenhuma e fica sem borda, e
  aparece um degrauzinho no canto. Conferido no Unity: sao 2 a 4 pixels, so
  aparece com zoom. Prioridade baixa; um tile de canto interno por estacao resolveria.
- **Cerca vertical**: os postes agora emendam, mas o espacamento entre as
  travessas fica irregular quando varias pecas sao empilhadas, porque o ritmo
  do desenho (~19px) nao divide 64. So incomoda em corridas longas.
- **Paleta**: usar `paleta-do-trio.gpl`, extraida da arte real. Importar no
  Piskel antes de desenhar qualquer sprite novo.
