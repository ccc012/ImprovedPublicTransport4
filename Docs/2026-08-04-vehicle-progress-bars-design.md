# Barras de progresso do painel de veiculo

## Comportamento

- Quando o veiculo chega e recebe `Vehicle.Flags.Stopped`, a barra fica verde e inicia em 0%.
- Durante o embarque, a barra verde sobe uma unica vez ate 100%.
- Se o veiculo permanecer parado por unbunching, a barra permanece em 100%; nao reinicia.
- Quando o veiculo sai da parada, a barra volta imediatamente ao modo de movimento azul.
- Em movimento comum, vanilla controla valor, cor e percentual juntos, atualizando continuamente.
- Navios e avioes mantem o calculo especial IPT, mas seu progresso leve e atualizado por frame; o restante do painel continua limitado a 5 Hz.

## Posse da barra

O IPT nunca deve controlar apenas parte da barra enquanto vanilla controla o restante.

- `Vanilla`: nenhum cache IPT de valor, cor ou percentual.
- `Boarding`: IPT controla valor, verde e percentual.
- `RouteProgress`: IPT controla valor, cor normal e percentual para navio/aviao.

## Transicoes

Uma observacao barata por frame usa o veiculo selecionado, primeiro veiculo, linha e flag `Stopped`.

- Mudanca de veiculo, linha ou `Stopped` invalida caches imediatamente.
- A transicao forca uma atualizacao completa uma vez; a atualizacao pesada normal permanece em 0,2 s.
- Fechar/reabrir painel ou encontrar dados invalidos limpa a posse IPT.

## Formula verde

`progress = Clamp01(waitCounter / boardingTime)`.

- Embarque comum: 12 ticks.
- Aviao: 200 ticks.
- Depois do tempo de embarque: 100% enquanto continuar parado.
- `IntervalAggressionFactor` nao entra na formula visual.

## Robustez da auditoria

- Verificar objetos/campos de reflection antes de usar.
- Limpar caches antes de retornos causados por linha, veiculo, info, prefab ou progresso invalido.
- Nao alterar patches de simulacao nem tempos reais de embarque/unbunching.

## Verificacao

1. Onibus chegando: azul muda para verde sem atraso perceptivel.
2. Verde sobe 0-100 uma vez e permanece 100 durante espera.
3. Saida: verde some; azul volta sem frame congelado do estado anterior.
4. Troca de veiculo nao reutiliza valor anterior.
5. Movimento de onibus/trem/metro/bonde fica totalmente vanilla.
6. Navio/aviao atualizam rota continuamente.
7. Sem linha/dados invalidos: nenhum cache IPT reaplicado.

## Fora do escopo

- Alterar UI, cores ou textos alem da barra existente.
- Alterar a logica de unbunching.
- Corrigir itens da auditoria sem relacao com `PanelExtenderVehicle`.
