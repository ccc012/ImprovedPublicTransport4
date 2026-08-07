# Options Parent/Child Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Normalizar todas as cascatas pai/filha reais do menu atual e fechar o vazamento runtime do Express Bus.

**Architecture:** Centralizar estado visual em helpers pequenos usando `OptionsNestedTabs.SetEnabled`. Manter valores persistidos dormentes. Adicionar um guard runtime no ponto de decisao do self-balancing de onibus.

**Tech Stack:** C#, Unity/Colossal UI, CSLModsCommon UI, .NET 3.5.

## Global Constraints

- Nao alterar opcoes independentes.
- Nao apagar valores ao desabilitar filhos.
- Nao modificar Stops & Stations.
- Preservar mudancas locais preexistentes, especialmente `Patch_PublicTransportExtraSkip.cs`.

---

### Task 1: Express Bus

**Files:**
- Modify: `UI/CSLModsCommonOptionsPanel.cs:71-77,428-455`
- Modify: `Integration/ExpressBusServices/ServiceBalancerUtil.cs:57-65`

- [ ] Trocar `isEnabled` direto por `OptionsNestedTabs.SetEnabled`.
- [ ] Calcular Middle-stop como `busMode != None && selfBalancing`.
- [ ] Atualizar Middle-stop no callback de Self-balancing.
- [ ] Adicionar guard runtime para modo Bus `NONE` antes do self-balancing.

### Task 2: Budget e Auto Line Color

**Files:**
- Modify: `UI/CSLModsCommonOptionsPanel.cs:63-69`
- Modify: `Settings/SettingsActions.cs:219-226`

- [ ] Aplicar `OptionsNestedTabs.SetEnabled` aos dois sliders de cor.
- [ ] Aplicar `OptionsNestedTabs.SetEnabled` ao slider Default vehicle count.

### Task 3: Reset OOC

**Files:**
- Modify: `UI/CSLModsCommonOptionsPanel.cs:670-729`

- [ ] Capturar o controle retornado pelo botao Reset.
- [ ] Adicionar o controle a `_oocChildControls`.
- [ ] Manter UOC e Intercity Bus fora da lista.

### Task 4: Verificacao

- [ ] Build: `dotnet build ImprovedPublicTransport4.csproj -c Release -p:AutoDeploy=false`.
- [ ] Revisar diff somente nos arquivos planejados.
- [ ] Confirmar que mudancas locais preexistentes nao foram sobrescritas.
- [ ] Executar checklist manual descrito no design.
