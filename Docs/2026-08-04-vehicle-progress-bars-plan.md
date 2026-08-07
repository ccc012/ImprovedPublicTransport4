# Vehicle Progress Bars Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fazer a barra verde subir uma vez de 0 a 100 durante embarque, permanecer em 100 durante espera e devolver imediatamente a barra ao movimento azul na saida.

**Architecture:** Separar observacao barata por frame da atualizacao pesada de 0,2 s. Definir posse integral da barra por modo, invalidando snapshots quando veiculo, linha ou estado parado mudar. Manter vanilla como unico dono durante movimento comum.

**Tech Stack:** C#, Unity UI, Cities: Skylines API, .NET 3.5.

## Global Constraints

- Modificar somente `UI/PanelExtenders/PanelExtenderVehicle.cs` e documentos do item 2.
- Nao alterar tempos ou logica de embarque/unbunching.
- Movimento comum deve ficar totalmente vanilla.
- Navio e aviao mantem progresso especial IPT.
- Preservar throttle de 0,2 s para estatisticas e buscas pesadas.

---

### Task 1: Estado e posse da barra

**Files:**
- Modify: `UI/PanelExtenders/PanelExtenderVehicle.cs`

**Interfaces:**
- Produces: `ClearProgressOwnership()`, chave do snapshot e atualizacao imediata de transicoes.

- [ ] Adicionar campos para veiculo, linha e `Stopped` observados no snapshot.
- [ ] Criar `ClearProgressOwnership()` para zerar valor, cor e texto cacheados juntos.
- [ ] Criar observacao barata por frame que resolve selecao sem `FindObjectsOfType` quando possivel e detecta transicao.
- [ ] Em transicao, limpar caches e forcar `UpdateBindings()` uma vez.
- [ ] Ao painel ficar invisivel, invalidar snapshot antes de retornar.

### Task 2: Barra verde unica

**Files:**
- Modify: `UI/PanelExtenders/PanelExtenderVehicle.cs:303-335`

- [ ] Substituir formula de duas fases por `Clamp01(waitCounter / boardingTime)`.
- [ ] Manter verde em 100 enquanto `Stopped` continuar ativo.
- [ ] Manter texto de unbunching quando o cache indicar espera ativa.

### Task 3: Movimento azul continuo

**Files:**
- Modify: `UI/PanelExtenders/PanelExtenderVehicle.cs:338-372,656-684`

- [ ] Para movimento comum, limpar valor, cor e texto IPT; vanilla controla os tres.
- [ ] Para navio/aviao, atualizar `UpdateProgress()` por frame sem executar o restante de `UpdateBindings()`.
- [ ] Se reflection ou progresso for invalido, limpar posse IPT antes de retornar.
- [ ] Nunca cachear somente cor.

### Task 4: Retornos seguros

**Files:**
- Modify: `UI/PanelExtenders/PanelExtenderVehicle.cs:195-407,656-684`

- [ ] Verificar painel, campos refletidos, veiculo, linha, `Info` e `PrefabData`.
- [ ] Limpar caches antes de todos os retornos invalidados relevantes.
- [ ] Garantir que troca de veiculo nao aplique snapshot anterior.

### Task 5: Verificacao

- [ ] Build: `dotnet build ImprovedPublicTransport4.csproj -c Release -p:AutoDeploy=false`.
- [ ] `git diff --check -- UI/PanelExtenders/PanelExtenderVehicle.cs`.
- [ ] Revisao independente contra design.
- [ ] Teste manual: onibus, trem, metro, bonde, navio e aviao; chegada, espera, saida, troca de veiculo e reabertura do painel.
