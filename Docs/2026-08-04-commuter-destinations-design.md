# Commuter Destination Clean-Room — Design

## Objetivo

Substituir o painel Commuter Destination por marcadores no mapa, desenhados sem copiar código do mod original. Ao selecionar uma parada pelo painel IPT, mostrar para onde os passageiros que embarcam ali vão.

## Comportamento

- Ao clicar numa parada IPT, os marcadores aparecem sobre os **prédios de destino final** dos passageiros.
- O jogo já mostra a parada de desembarque; este recurso mostra o destino final a pé.
- Até 10 passageiros para o mesmo prédio: um único marcador sem número.
- Acima de 10: um círculo colorido com o total de passageiros.
- Leve: atualização somente no clique; marcador azul fixo.
- Normal: atualização a cada 5 s; círculo na cor da linha.
- Máximo: atualização a cada 1 s; círculo na cor da linha.
- Fechar o painel IPT, trocar de parada, desligar a função ou descarregar a cidade limpa os marcadores.

## Limites

- Leve: até 500 marcadores.
- Normal: até 1000 marcadores.
- Máximo: até 2000 marcadores, com clustering/priorização.
- Limite técnico conservador: 2000 visíveis. Acima disso, priorizar os maiores e agrupar.

## Renderização

Seguir o padrão do TM:PE (WorldToScreenPoint + desenho imediato) e do Commuter Destination (renderer do jogo para ícones de notificação), sem copiar código:

- `IRenderableManager` registrado via `SimulationManager.RegisterManager`.
- Dados agregados atualizados em intervalo baixo (1-5 Hz), nunca por frame.
- Render por frame apenas dos candidatos dentro do frustum, com culling por distância e viewport.
- Nenhum `UIComponent` por marcador; sem alocação/strings por frame.
- Reutilizar buffers; evitar LINQ, closures e `magnitude` em loops grandes.
- Cor: obter cor da linha selecionada; para perfil Leve usar azul fixo.

## Extração de dados

Reimplementar do zero com APIs vanilla, sem copiar grafo/código do upstream:

- `CitizenManager.m_citizenGrid` para achar passageiros próximos.
- Só cidadãos com `Flags.WaitingTransport`.
- Confirmar espera naquela parada via `CitizenAI.TransportArriveAtSource`.
- Caminhar os próximos stops da linha com limite de iterações.
- Inferir o prédio de destino final via path/citizen AI.
- Agregar por prédio; aplicar caps por perfil.

## Arquivos

- Criar: `Integration/CommuterDestination/CommuterDestinationOverlay.cs`.
- Substituir (clean-room): gerador de grafo e renderer existentes.
- Excluir: painel, botão de navegação e patch de abertura do painel.
- Manter: setting, checkbox, hooks IPT de settings/lifecycle/clique.
- Atualizar traduções e remover chaves de painel.

## Limpeza

- `SelectStop(ushort stopId)` publica snapshot novo; `Clear()` remove marcadores.
- Exceções no scan: manter último snapshot válido ou publicar vazio; log rate-limited.
- Exceções no render: capturar na borda; nunca propagar.
- Estado por cidade: reset no unload.

## Verificação

1. Off: nenhum marcador.
2. On + clique parada: marcadores corretos; sem painel secundário.
3. Trocar parada substitui marcadores.
4. Fechar painel/`Esc`/clique fora limpa tudo.
5. `Alt` não abre painel nem marcadores.
6. Desligar limpa imediatamente; religar funciona sem reload.
7. Cidades A/B sem reinício.
8. Perfis respeitam caps e atualização.
9. Parada removida, linha circular e dados inválidos não quebram.

## Fora do escopo

- Navegação anterior/próxima entre paradas.
- Detalhes por passageiro.
- Painel próprio.
- Ferramentas de clique nos marcadores.
