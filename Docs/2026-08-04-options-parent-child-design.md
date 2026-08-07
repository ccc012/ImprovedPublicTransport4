# Cascatas pai/filha das opcoes

## Objetivo

Todas as opcoes realmente subordinadas a uma funcao pai devem bloquear clique e reduzir opacidade quando o pai estiver desligado. Valores persistidos permanecem preservados.

## Express Bus

- `ExpressBusUnbunchingMode=None` desabilita Self-balancing, Middle-stop balancing e Minibus.
- Middle-stop balancing exige tambem Self-balancing marcado.
- O runtime de self-balancing de onibus deve retornar sem agir quando o modo de onibus estiver desligado, mesmo se o modo de bonde mantiver a integracao carregada.
- Os tres controles usam `OptionsNestedTabs.SetEnabled`.

## Budget Control

- Quando Budget Control estiver ligado, Default vehicle count fica desabilitado e opaco.
- Quando desligado, o slider volta a ser editavel.
- O valor permanece salvo.

## Auto Line Color

- Minimum color difference e Maximum color attempts ficam ativos somente para estrategias que realmente os consomem.
- Disabled e CategorisedColor desabilitam e reduzem opacidade.
- Naming strategy permanece independente.

## Optimised Outside Connections

- Wait multiplier, Passenger scope, quatro toggles de dummy traffic e o botao Reset pertencem ao pai OOC.
- Unlimited Outside Connections e Intercity Bus Control permanecem independentes.

## Fora do escopo

- Stops & Stations: nao existe toggle pai no estado atual.
- Train Display, Ticket Price Path Cost e Hide Vehicle Editor: cascatas ja corretas.
- Alterar valores persistidos quando controles forem desabilitados.
- Reorganizar abas.

## Verificacao

1. Pai desligado: filhos subordinados com opacidade `0.45`, sem clique.
2. Pai ligado: filhos aplicaveis com opacidade `1`.
3. Express Bus Middle-stop exige modo ativo e Self-balancing ligado.
4. Valores sobrevivem a alternancia e reabertura das opcoes.
5. Opcoes independentes continuam clicaveis.
6. Self-balancing nao afeta onibus com modo Bus desligado.
