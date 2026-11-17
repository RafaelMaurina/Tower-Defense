# Seleção de fases e fase bônus

## Fluxo de fases

A tela de seleção apresenta as fases na ordem da campanha: Primavera, Verão e Outono. Cada card pode iniciar sua fase diretamente. Uma campanha completa começa em Primavera e avança automaticamente por Verão e Outono.

O ranking da campanha registra somente uma vitória ao concluir os três níveis em sequência, começando em Primavera. Partidas iniciadas diretamente em Verão ou Outono continuam jogáveis, mas seus resultados não entram no ranking da campanha. O ranking do bônus permanece separado e acessível pelo menu principal.

## Desbloqueio do bônus

A fase `Nivel_4_Campus` é desbloqueada depois que o jogador vence Primavera, Verão e Outono pelo menos uma vez cada, em qualquer dificuldade. As conclusões ficam salvas em `PlayerPrefs`, independentemente da ordem ou dificuldade, e continuam disponíveis ao reabrir o jogo. Vencer apenas Outono não libera o bônus.

A seleção mostra o progresso das três conclusões enquanto o bônus está bloqueado. A fase bônus não participa da progressão automática da campanha.

## Integração

O nome de cena usado na seleção e no progresso é `Nivel_4_Campus`, igual ao gerador e ao `Wave_Bonus_Prova_Final.levelScene`. O campus usa iluminação noturna em qualquer dificuldade. A geometria é funcional e reaproveita arte da fazenda; o acabamento visual do campus continua separado.

As ondas e seus dados não foram alterados por esta atualização da interface e do fluxo de seleção.

## Validação manual

1. Com progresso limpo, confirmar que o bônus está bloqueado e mostra `0/3`.
2. Vencer cada uma das três fases em qualquer ordem e dificuldade. Conferir o contador após cada vitória e o desbloqueio após a terceira.
3. Reiniciar o jogo e confirmar que o bônus continua desbloqueado.
4. Iniciar uma fase diretamente em Verão ou Outono e confirmar que o resultado não entra no ranking da campanha.
5. Iniciar em Primavera, concluir as três fases e confirmar que o resultado aparece no ranking da campanha.
6. Conferir que os resultados do bônus aparecem no ranking próprio.
