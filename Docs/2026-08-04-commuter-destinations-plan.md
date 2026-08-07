# Commuter Destinations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir o painel Commuter Destination por marcadores clean-room sobre prédios de destino, controlados por perfil de performance.

**Architecture:** Um overlay registrado como `IRenderableManager`; dados agregados em snapshot atualizado por intervalo; render imediato com culling. Sem `UIComponent` por marcador.

**Tech Stack:** C#, Unity, Cities: Skylines API, .NET 3.5.

## Global Constraints

- Não copiar código/estrutura do mod original.
- Nenhum painel secundário.
- Sem `UIComponent` por marcador.
- Manter setting, checkbox e hooks IPT existentes.
- Caps por perfil e atualização intervalada.

---

### Task 1: Snapshot de destinos

**Files:**
- Create: `Integration/CommuterDestination/CommuterDestinationOverlay.cs`

- [ ] Definir `DestinationMarkerData` com posição e contagem.
- [ ] Implementar extração clean-room com grid de cidadãos e stops, com limites.
- [ ] Agregar por prédio; até 10 vira marcador sem número; acima de 10 vira círculo numerado.
- [ ] Respeitar `PerformanceProfile.CommuterMaxCitizens` e `CommuterMaxDestinations`.
- [ ] Publicar snapshot somente após cálculo completo.
- [ ] `SelectStop(ushort)` e `Clear()`.

### Task 2: Renderização

- [ ] Registrar via `SimulationManager.RegisterManager` como `IRenderableManager`.
- [ ] Projetar com `WorldToScreenPoint`; rejeitar atrás da câmera e fora da viewport.
- [ ] Culling por distância quadrática e limites por perfil.
- [ ] Desenhar marcadores sem `UIComponent`, sem alocação por frame.
- [ ] Cor: perfil Leve azul fixo; demais cor da linha.
- [ ] Render intervalado apenas quando visível e painel aberto.

### Task 3: Lifecycle e integração

- [ ] Chamar `SelectStop` no clique IPT da parada.
- [ ] `Clear()` ao fechar/trocar/desligar/unload.
- [ ] Reset por cidade.
- [ ] Remover painel, navegação e patch antigos.
- [ ] Atualizar traduções e remover chaves órfãs.

### Task 4: Verificação

- [ ] `dotnet build ImprovedPublicTransport4.csproj -c Release -p:AutoDeploy=false`.
- [ ] `git diff --check`.
- [ ] Revisão clean-room e de lifecycle.
- [ ] Testes manuais do desenho.
