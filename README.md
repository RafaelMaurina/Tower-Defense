# Defesa da Colheita

Tower defense 2D em pixel art. Trabalho prático de CCC300 (Computação Gráfica e
Realidade Virtual), Prof. Dr. Rafael Rieder, UPF.

## Como clonar

Este repositório usa **Git LFS** para os arquivos binários: sprites hoje, música e
efeitos sonoros depois. Instale o LFS **antes do primeiro clone**.

```bash
git lfs install
git clone https://github.com/RafaelMaurina/Tower-Defense.git
```

Sem isso, os arquivos chegam como ponteiros de texto de 131 bytes em vez das
imagens, e o projeto abre sem arte nenhuma. Se você já clonou sem o LFS, dá para
consertar sem clonar de novo:

```bash
git lfs install
git lfs pull
```

## Abrir no Unity

Versão **6000.6.2f1**. Abrir em versão diferente faz o Unity reimportar os assets e
sujar o diff para o resto do time. Confira no Unity Hub antes de abrir.

Os níveis não vêm no clone: na primeira vez que o projeto abre, o Unity gera os
três sozinho (o menu **Defesa da Colheita → Gerar niveis** faz o mesmo à mão).

## Arte

Densidade de pixel, configuração de importação e paleta estão em [ARTE.md](ARTE.md).

As configurações de importação ficam gravadas nos arquivos `.meta` ao lado de cada
PNG, então vêm junto no clone. Não é preciso configurar sprite na mão.

Para ver os sprites em cena: menu **Defesa da Colheita → Montar demo de sprites**.
Ele cria um objeto `DEMO Sprites`; apagar o objeto desfaz.

## Entrega

- Executável: _a definir_
- Vídeo demonstrativo: _a definir_

## Equipe

- Rafael ([@RafaelMaurina](https://github.com/RafaelMaurina)): arte e audio
- Fabio ([@fabioov](https://github.com/fabioov)): combate
- Joao ([@Joao-Althaus](https://github.com/Joao-Althaus)): jogo e interface
