# Relatório de Mudanças — v4.8.8 (git) → versão local a lançar

**Data:** 07/08/2026
**Comparação:** tag git `v4.8.8` / branch `master` vs working tree local (branch `4.8.9`, nada commitado ainda)
**Escopo:** 134 arquivos alterados, ~5.183 inserções, ~4.519 remoções (diff vs tag v4.8.8)

---

## 1. Resumo executivo

Esta release é essencialmente uma **grande onda de estabilização + novo sistema de ativação de features**:

1. **Novos "master switches"** (toggles mestre) em `ModSetting`: `EnableUnbunching`, `EnableBudgetFeatures`, `EnableAutoLineColor`, `EnableTrainDisplay`, `EnableVehicleEditor`, `EnableStopsAndStations` — todos default `false`, com efeitos em cascata nos sub-settings e nos Harmony patches.
2. **Thread-safety e endurecimento defensivo** no core (`Domain`, `UpdateManager`, `MovingAverage`, filas de veículos de `CachedTransportLineData`, bounds checks em buffers da engine).
3. **Reestruturação de features**: CommuterDestination foi **reimplementado do zero** (overlay clean-room, painel removido), SharedStopEnabler passou a usar o transpiler unificado da AdvancedStopSelection, IntercityBusControl ganhou restauração de prefabs + relink de conexões.
4. **13 novos hotkeys** configuráveis com persistência.
5. **Traduções**: 46 chaves novas / 73 removidas / 53 alteradas em `en.txt`; revisão geral de todos os idiomas; `pt-br.fixed.txt` eliminado (consolidado em `pt-br.txt`).

> ⚠️ **Nada disso está commitado.** A branch `4.8.9` existe localmente, mas todo o trabalho está no working tree não-commitado. O git só contém até o commit `c0d9f5e` (que está apenas 2 commits além da tag v4.8.8, ambos de documentação).

---

## 2. Novos recursos e mudanças de comportamento (visão do jogador)

| Recurso | O que mudou |
|---|---|
| Feature switches | 6 toggles mestre em Options: Unbunching/Express Bus, Budget Features, Auto Line Color, Vehicle Editor, Stops & Stations (Waiting Passenger Caps), Train Display. Desligar um desliga/desabilita todos os sub-settings e patches relacionados. |
| Hotkeys | 13 novos atalhos (abrir painel de linha, toggle unbunching, copiar/colar config, copiar para prédios/distritos, selecionar tipos de veículo, toggle Vehicle Editor, abrir Flight Tracker, veículo anterior/próximo), todos persistidos e reconfiguráveis. |
| Train Display | Removido o slider "update interval" (agora fixo por perfil: Light=1s, padrão=0.5s, Max=0.05s). Novo escopo `SelectedVehicle/FirstPerson/Both` (substitui "Only while following" e "First person only"). Integração com mod externo FPSCamera via reflexão. |
| Vehicle Editor | Nova posição `Hidden` no dropdown de posições. |
| Unbunching | Novo master switch que desliga todo o sistema de unbunching (veículos voltam ao comportamento vanilla). |
| CheckTransportLineVehicles | O patch deixa de bloquear tudo: agora permite o vanilla matar veículos de linhas SEM filtro de prefab, e libera veículos de linhas filtradas para troca de tipo funcionar. |
| Intercity Bus | Ao desativar, **restaura os prefabs originais** (corrige bug de prefabs convertidos permanecendo) e **relinka conexões externas** de estações convertidas. |
| Commuter Destination | Reimplementação clean-room como overlay no mapa (círculos no chão + labels de contagem). Painel flutuante, botões Prev/Next e patch de abertura removidos. Renderização apenas com o painel de parada aberto. |
| Express Bus | `Patch_BusStartPathFind` agora é **fail-open** (log + deixa o ônibus seguir em vez de congelá-lo). ServiceBalancer ignora tudo quando modo=NONE. Delegate.CreateDelegate no lugar de reflection lenta. |
| Ko-fi | Botão "Support me on Ko-fi" no topo da página Geral das Options. |
| Settings | Perfis Recommended/Realistic ligam os novos switches; perfil Vanilla desliga todos. Handlers live (sem reload) para a maioria dos switches. |

---

## 3. Mudanças de código C# em detalhe

### 3.1 HarmonyPatches

| Arquivo | Mudança |
|---|---|
| `GetDepotLevelsPatch.cs` | Guard `lineID == 0 || lineID >= lines.m_size` → vanilla decide; evita IndexOutOfRange. |
| `DepotCapacityEnforcePatch.cs` | `_actionQueued` agora `int` com `Interlocked.CompareExchange/Exchange` (agendamento atômico de ação entre threads). Loops ushort→int. |
| `DepotStatsDisplayPatch.cs` | **Removido o cache de tooltip** — a string "LABEL: n/max" é revalidada a cada chamada (corrige tooltip obsoleto; troca micro-otimização por correção). |
| `StartTransferPatch.cs` | Bounds check `lineID >= lines.m_size` antes de acessar info. |
| `GetVehicleInfoPatch.cs` | Guard nula no `__instance.component.Hide()`. |
| `OnMouseDownPatch.cs` (StopButton) | Pattern-match `is not UIButton` + cast `ushort`. Caminho sem-Alt chama `CommuterDestinationOverlay.SelectStop`, com-Alt chama `Clear()`. Removida chamada à classe deletada `OpenStopDestinationPanelPatch`. |
| `OnMouseDownPatch.cs` (VehicleButton) | Mesma proteção type+pattern-match com fallback `return true`. |
| `RefreshVehicleButtonsPatch.cs` | `string.Format` da tooltip envolvido em try/catch (falha não aborta o loop). |
| `UpdateStopButtonsPatch.cs` | Tooltip vira `string.Empty` se `lineID >= 256`. **Fix:** `objectUserData` volta a ser o `ushort` do nó (vanilla espera isso; guardar InstanceID causava `InvalidCastException` no clique). |
| `CanLeaveStopPatch.cs` | **Novo master switch**: `!EnableUnbunching` → `__result = false; return true` (comportamento vanilla). |
| `SimulationStepPatch.cs` | Timer do postfix: `>= 3840U` → `== 3840U` (roda 1 frame por tick de 4096 em vez de 256). Se cache inicializado mas `GetTargetVehicleCount <= 0`, **recalcula com vanilla e grava no cache** (antes relia 0 e podia desovar a frota). |
| `CheckTransportLineVehiclesPatch.cs` | **Grande**: se nenhuma linha tem prefab filtrado → `return true` (vanilla). Se tem: veículos de linhas SEM filtro, com veículo selecionado diferente do atual e depot com nível compatível → `ReleaseLineVehicles()` (limpa para troca de tipo funcionar). |
| `CanLeavePatch.cs` (XYZVehicleAI) | Wrapper sai cedo se `!EnableUnbunching`. |
| `LoadPassengersPatch.cs` | Recebe `bool __runOriginal`; contabilização pós-passageiros só se o original rodou. |

### 3.2 Integration/* (features)

| Módulo | Mudança |
|---|---|
| **AdvancedStopSelection** | Refactor grande em transpiler unificado (usado também pelo SharedStopEnabler). Helpers novos: `FindTransportInfoLocals` (descobre índices de locais dinamicamente em vez de fixos 12/13), `GetStoredLocalIndex`, `LoadsLocal`, `GetLoadArgument`, `SameInstruction`, `IsBrfalse`. Intercepta loads de `TransportInfo.m_stopFlag` e injeta `FilterStopFlag` (retorna `None` quando SharedStopEnabler ativo). `GetAlternateMode` só com mod AdvancedStopSelection + hotkey. Patch/Unpatch por demanda. |
| **AutoLineBudget** | Ao remover linha: `SetTargetVehicleCount(lineID, CalculateTargetVehicleCount())` + `ClearEnqueuedVehicles`. |
| **AutoLineColor** | `ColorMonitor`: removida integração com TicketPricesTab (migrou para DayNightPriceWatcher). Loops ushort→int. `UsedColors`/`NamingStrategyBase`: loops int com guards. |
| **BetterBoarding** | `PassengerWaitingInfo`: guard nulo `citizenInfo?.m_citizenAI`. `TramAI`: `[HarmonyAfter]` ExpressBusServices. |
| **CommuterDestination** | **Pasta inteira deletada (10 arquivos)** — ver seção 6. Substituída por `CommuterDestinationOverlay.cs` (721 linhas, clean-room, baseada só em APIs vanilla): scan de `CitizenManager.m_citizenGrid` (raio 64m, flag `WaitingTransport`, confirma via `TransportArriveAtSource`), agrega por `m_targetBuilding`, render via `IRenderableManager` com culling por distância/backface, pool de 64 `UILabel`, clustering acima do cap do perfil (500/1000/2000), refresh 0/5/1s por perfil. API: `Activate()/Deactivate()/SelectStop()/Clear()/IsActive`. |
| **ExpressBusServices** | `DepartureChecker`: guards nulos `Info?.m_class`. `Patch_BusStartPathFind`: fail-open. `Patch_PublicTransportExtraSkip`: `Delegate.CreateDelegate` (call direto) no lugar de `MethodInfo.Invoke` + arrays object[]. `ServiceBalancerUtil`: modos NONE saem cedo e nunca deixam instrução pendurada. `TransportLineUtil`: guard `nextStop == 0 || nextStop >= nodes.Length`. |
| **IntercityBusControl** | `StationPatcher`: novo `OriginalPrefabStates` + `RecordOriginalState()` + `RestorePrefabs()` (reverte conversão ao desativar). Após patch, relink de `CreateConnectionLines` em edifícios reais com prefab convertido (corrige estação que desovava ônibus ao spawnar). `Patcher`: removido patch manual de `UpdateBindings` (o checking por atributo resolve). `LoadingExtension`: reset no bloco Game. |
| **MileageTaxiServices** | Guard `taxiInstance?.m_transportInfo == null` → return 0 (anti-NRE). |
| **SharedStopEnabler** | `PatchController`: ativa via transpiler unificado (`SetSharedStopEnablerActive`), desativa com `RestoreSegmentFlags()`. `SharedStopRegistry`: novos `SegmentFlagSnapshots`/`PropFlagSnapshots` (guarda flags antes/depois; restaura só o que o mod alterou). `LanesStillUsing` volta a `ushort[]`. |
| **SingleTrainTrackAI** | `PatchController`: `NetworkChangePatch.Apply()`/`Undo()` (invalida caches de classificação ao editar a rede). Novo `NetworkChangePatch.cs` (74 linhas): postfixes em `NetManager.CreateSegment` (2 overloads com argumentTypes explícitos para evitar AmbiguousMatchException), `ReleaseSegment`, `CreateNode`, `ReleaseNode` → limpa `SectionClassifier`/`SegmentClassifier`. |
| **StopsAndStations** | `PassengerCountLimiter`: skip quando `!InGame` ou `!EnableStopsAndStations` (master switch; caps ficam desativados). |
| **TicketPriceCustomizer** | `DayNightPriceWatcher` agora chama `TicketPricesTab.OnUpdate` por frame (try/catch + flag). `PriceCustomization`: limpa caches estáticos ao reaplicar. `TicketPricesTab`: `Destroy()` de materiais/atlas dos ícones customizados (corrige vazamento de texturas em reload). |
| **TrainDisplayUpdated** | `TrainDisplayIntegration`: removido `ResolveNextStopName`. `TrainDisplayWatcher`: removido slider de intervalo; usa `PerformanceProfile.TrainDisplayRefreshSeconds` fixo; novo escopo; resolve veículo via `FpsCameraIntegration.TryGetVehicle` ou seleção; `OnDestroy` limpa overlay. Novo `FpsCameraIntegration.cs` (86 linhas): reflexão no assembly "FPSCamera" (tipos `FPSCamController`, `VehicleCam`, prop `FollowID`) sem referência de compilação; `Clear()` descarta cache. |

### 3.3 Data/* (serialização e saves)

| Arquivo | Mudança |
|---|---|
| `CachedNodeData.cs` / `CachedVehicleData.cs` | Guards null + loop por `cachedData.Length` (não 32768 fixo). Formato de save inalterado. |
| `CachedTransportLineData.cs` | **Maior mudança de compatibilidade de saves.** Novos `QueueLocks` (256 locks). Init com guard de `firstStop`/bounds. `LoadData`: `IsKnownVersion` (v001–v004) em vez de `Length != 4`; **bounds no loop**; reads primitivos via `ReadInt32/Float/Bool/UInt16` (não mais BitConverter); depot com fallback 0; **FIX da leitura de `QueuedVehicles` em v002+** (lógica invertida: fila só popula quando `!BudgetControl` e prefab existe — releitura de saves antigos muda, porém fica mais fiel); `ReadCollectionCount` validado. `GetTargetVehicleCount` com clamp. Filas com `lock`. **Versão de save continua `"v004"`** — releitura de saves v002/v003 será DIFERENTE de antes. |
| `MovingAverage.cs` | `SampleLength` min 1; lock dentro do getter; ctor de array só usa os últimos N samples; `Add` drena com `while`. **Afeta reprodução dos dados de média persistidos em saves antigos.** |
| `PrefabData.cs` | Fix do Solaris Urbino 24: trailer exige `m_trailers.Length >= 2`, guard `Name.IndexOf('.')`; remoção do reset massivo de flags (só `~Flags.Inverted`); check `Created` por `(flags & 12)`. **Novo `VehicleDataDirectory`** (localApplicationData/IptVehicleData) com **fallback para o legado** relativo — arquivos antigos continuam lendo. |
| `SerializableDataExtension.cs` | Endurecimento: `EnsureAvailable` → `EndOfStreamException` em buffer truncado (não mais IndexOutOfRange genérico); `WriteString` null → ArgumentNullException; `ReadString` volta a StringBuilder + validação; `WriteFloatArray`/`ReadFloatArray` validados; `EnsureCollectionLength`. **Saves corrompidas agora falham com diagnóstico explícito.** |
| `VehicleData.cs` | `IncomeThisWeek` calculado em `long` com clamp (previne overflow). |
| `VehiclePrefabs.cs` | Guards null em `FindByName`/`FindByIndex`. |

### 3.4 UI/*

| Arquivo | Mudança |
|---|---|
| `CSLModsCommonOptionsPanel.cs` | Seção Ko-fi no topo (via novo `FillGeneralHeader` virtual). Checkboxes master switches em cada sub-página com `OptionsNestedTabs.SetEnabled` (filhos travados). MileageTaxi vira toggle live (sem reload). Vehicle Editor com posição `Hidden`. Train Display: dropdown de escopo, slider de intervalo removido. |
| `PanelExtenderLine.cs` | `_updateDepots` lido sob lock; unsubscribes antes de `Destroy`; `OnLineBudgetClick` zera target quando budget admin desligado; `OnUnbunchClick` dentro de `SimulationManager.AddAction`; `OnAddVehicleClick` reforçado (bounds, depot=0 → `SetBudgetControl(false); SetTargetVehicleCount(line, 1)`, `DepotUtil.CanAddVehicle` com flags, validação de classe de serviço, setTarget = current+1); `OnRemoveVehicleClick` com locks; `SetBudget` sem lock reentrante. |
| `PanelExtenderVehicle.cs` | **Refactor do controlador de progresso**: removidos FieldInfo/reflection de `_cachedCurrentProgress` etc.; observação de estado por frame (`TryObserveVehicle` captura vehicleId/lineId/stopped/routeProgress); barra **verde de embarque** quando parado na parada (via `CanLeaveStopPatch.BoardingTime`); quando móvel, volta à barra vanilla branca (exceto Ship/Plane); `ReapplyCachedFields` só se isStopped/route; guards NaN/Infinity com `Mathf.Clamp01`; guards em TransportManager/PathManager null; `Init` tolera panelObject ausente; failsafe ao reabrir painel. |
| `PreviewRenderer.cs` | `OnDestroy` libera RenderTexture/material/camera; `_fallbackMaterial`; bloco de iluminação em try/finally (sempre restaura sol/lua/exposição/sky tint, rotação/intensidade/cor da luz); RenderTexture liberado antes de recriar; `MaterialPropertyBlock` próprio; `previewMaterial?.shader == null` → não desenha. |
| `PublicTransportStopWorldInfoPanel.cs` | Guards `transportLine == 0` e `Info == null` → Hide; toggle unbunching com guard no cache. |
| `VehicleListBox.cs` | `SelectedItems` aceita null; remoção do early-out de igualdade. |
| `VehicleSelection.cs` / `VehicleSelectionRow.cs` | Guards de linha/capacidade. |
| `CopyPaste.cs` | Loops ushort→int. |

### 3.5 CSLModsCommonShared/* (framework embutido)

| Arquivo | Mudança |
|---|---|
| `Domain.cs` | **Thread-safe**: `AllDomainsLock`, `_managerLock`, `TryGetManager`, snapshots de eventos, remoção do lookup público direto, `GetOrCreateManager` limpa entrada em falha, `Dispose` remove do AllDomains. |
| `UpdateManager.cs` | Remove dependência de `ManagerLookup`; usa `Domain.TryGetManager` + `_lookupLock`; itera snapshots (evita "collection modified"). |
| `LocalizationManager.cs` | `_sessionScanComplete` — plugins escaneados 1x por sessão. |
| `OptionsPanelBase.cs` | Novo `protected virtual FillGeneralHeader(ScrollContainer)`. |
| `TextDocument.cs` | **Fix de bug**: `Redo()` lia `_undoStack.Last()` em vez de `_redoStack.Last()` (undo corrompia o redo). |
| `CliExecutor.cs` | Handlers de `Exited` em try/finally com Dispose de wait handles/processo (evita race/hang). |
| `JsonHelper.cs` | `TypeNameHandling` `Auto` → **`None`** (hardening de segurança; só afeta se algo serializava tipos derivados — não deve afetar nada, pois `NullValueHandling.Ignore` continua). |
| `ListExtensions.cs` | `Last`/`PopLast` lançam exceções adequadas. |

### 3.6 Settings / Query / Util / raiz

| Arquivo | Mudança |
|---|---|
| `IptHotkeys.cs` | **+13 hotkeys** (~+250 linhas): OpenLinePanel, ToggleLineUnbunching, CopyLineConfig, PasteLineConfig, CopyToServedBuildings, CopyToDistricts, SelectVehicleTypes, ToggleVehicleEditor, OpenFlightTracker, PrevVehicle, NextVehicle. Persistência: bindings salvos em `ModSetting.Hotkey*` (formato `"key(int)|mods(int)"`), `""`=default, `"0|0"`=unbound. Handlers implementados (abrir painel na linha selecionada, toggle no cache, CopyPaste, PrefabPanelManager, WorldInfoPanel.ChangeInstanceID etc.). |
| `SettingsActions.cs` | Perfis atualizados (Recommended/Realistic ligam switches; Vanilla desliga todos). Handlers live: `OnMileageTaxiChanged`, `OnUnbunchingChanged` (reseta filhos + ServiceBalancer + EBS), `OnBudgetFeaturesChanged`, `OnAutoLineColorChanged` (reload), `OnTrainDisplayChanged` (sincroniza mode), `OnStopsAndStationsChanged`, `OnVehicleEditorChanged` (Hide). Intercity off → `RestorePrefabs()` + reset. CommuterDestination off → `Overlay.Deactivate/Clear`. `DeleteLines` com AddAction única. |
| `ModSetting.cs` | Novo enum `TrainDisplayScopes`; `VehicleEditorPositions.Hidden = 2`; 6 booleanos mestre (default false); 14 strings `Hotkey*` (default `""`). Settings JSON antigos continuam válidos (propriedades novas ausentes → default). |
| `Query/WorldInfoCurrentLineIDQuery.cs` | Reescrito: `IsValidLine` (linha != 0, bounds, flag Created, Info != null), `TryGetVehicleLine`, try/catch global → retorna 0 (corrige IndexOutOfRange quando painel de veículo nunca criado). |
| `Query/AvailableVehiclesQuery.cs` | Guards null. |
| `Util/PerformanceProfile.cs` | `TrainDisplayPollMultiplier` → `TrainDisplayRefreshSeconds` fixo (Light=1f, padrão=0.5f, Max=0.05f). |
| `Util/SessionWatchdog.cs` | Heartbeat de 15s só com `Diagnostics.VerboseRuntimeLogs`; dump inclui os novos switches. |
| `Util/TranslationCompleteness.cs` | Cache por locale (`_localeCache`) — não re-lê .txt a cada abertura. |
| `Util/TransportLineUtil.cs` | Guard `m_sourceBuilding != 0`. |
| `Util/Utils.cs` | `ToSingle/ToInt32/ToByte` com `NumberStyles.InvariantCulture` (parse determinístico entre locais). |
| `BuildingExtension.cs` | `Deinit` limpa `_depotMap` e eventos (fix de refs fantasma em reload); `OnBuildingReleased` usa triplet da chave em vez de `DepotUtil.GetStats`; `ObserveForInfo` com guard de bounds. |
| `ImprovedPublicTransportMod.cs` | `InGame` volatile; CommuterDestination via Overlay (Activate no OnLevelLoaded, Clear+Deactivate no unload); TicketPrice unload → `PriceCustomization.ResetToVanilla()`; Intercity unload → `RestorePrefabs()`. |
| `IptModManager.cs` | **Lista de mods banidos reduzida de dezenas para 14** (SharedStopEnabler, SingleTrainTrackAI, RealisticWalkingSpeed, UnlimitedOutsideConnections, ExpressBusServices, AdvancedStopSelection, BetterBusStopPosition, BetterTrainBoarding, ElevatedStopsEnabler, MileageTaxiServices, OptimisedOutsideConnections, PublicTransportUnstuck, TransitVehicleSpawnDelay, CommuterDestination.CS1) + regra de assembly "CommuterDestination alt". Menos bans automáticos (evita falsos positivos). |
| `TranslationFramework/LocalizationManager.cs` | Novo `Deinit()` (desinscreve eventos de locale — fix de reload). |
| `Util/CompatibilityGuard.cs` (NOVO) | Detecção de `TaxiAI.SimulationStep` patchado por TMCE/TME → desliga só o Taxi Stand Fix. ⚠️ **Código morto: nenhum call site chama `RunLevelChecks()`/`Reset()`.** |

---

## 4. Traduções

### 4.1 en.txt (arquivo mestre)

- **46 chaves novas**: `SETTINGS_FEATURE_*_ENABLE(_TOOLTIP)` ×6 (Unbunching, Budget, AutoLineColor, VehicleEditor, StopsAndStations, TrainDisplay), `SETTINGS_FUTURE_LINEMAP/STOPPLACER/LINEHISTORY` (+_TIP), `SETTINGS_TRAINDISPLAY_SCOPE*` (5), 11 pares de hotkeys `OPEN_LINE_PANEL`...`NEXT_VEHICLE` (+_TOOLTIP), `SETTINGS_KOFI_GROUP/DESCRIPTION/BUTTON/LINK`.
- **73 chaves removidas**: abas antigas (`SETTINGS_TAB_*`, `SETTINGS_AUTO_LINE`, `SETTINGS_INTEGRATIONS_GROUP`, `SETTINGS_PTU_GROUP`), switches antigos (`SETTINGS_ENABLE_STOPS_AND_STATIONS*`, `COMMUTER_DESTINATION_*` overlay antigo, `COMMUTERDESTINATION_PANEL_*` ×7, `SETTINGS_COMMUTERDESTINATION_MAP_*`), `MOD_DESCRIPTION`, `TRAINDISPLAY_LABEL_NAME/NO_LINE/HIDDEN`, `LINE_PANEL_LINE_STOPS`, tooltips de capacidade de parada (todas as modalidades), `SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING*`/`FIRST_PERSON_ONLY*`, `WHATSNEW_3_0_0_1/2`, `WHATSNEW_3_0_1`, `SETTINGS_BBSP_MODE_UPDATED`, `SETTINGS_INTERCITY_BUS_CAPACITY(_TOOLTIP)`, `SETTINGS_OOC_DISABLE_DUMMY`, `SETTINGS_EBS_DESC_SELFBAL(_TARGETMID/_MINIBUS)`.
- **53 chaves alteradas**: "Categorised"→"Categorized", "colour"→"color", "kerb"→"curb", "behaviour"→"behavior", **EBS: "Prudential/Aggressive"→"Prudent/Realistic"** (e no tram: "Express skip"→"Prudent", "Every stop (realistic)"→"Realistic"), temas Train Display (Simple/Dark/Light/Original/Blue/Green/Amber/Black Semi), `VEHICLE_PANEL_EARNINGS_TOOLTIP` "ticket sells"→"ticket sales", `STOP_PANEL_BORED_TIMER` "till"→"until", `CHANGELOG_4_3_8_1` gramática, Shift/Alt capitalizados, consistência de pontuação.

### 4.2 Demais idiomas (37 arquivos)

- **Padrão por idioma**: +19 chaves (10 `SETTINGS_FEATURE_*` + 5 `SETTINGS_TRAINDISPLAY_SCOPE*` + LAYOUT/_TOOLTIP) + 2 extras.
- ⚠️ **Inconsistência de chaves**: **26 idiomas** adicionaram `SETTINGS_SUPPORT_LABEL`/`SETTINGS_SUPPORT_BUTTON` — chaves que **NÃO existem no en.txt nem são usadas pelo código** (órfãs). Apenas 9 idiomas usam o conjunto correto `SETTINGS_KOFI_*` (de, es, fr, it, nl, pl, zh-cn, zh-tw, pt-br). **Correção sugerida**: alinhar os 26 idiomas para `SETTINGS_KOFI_*`.
- **Traduções pendentes** (valores ainda idênticos ao inglês): ur (9 chaves), fr (8), sv (7), de (4), es/it/hu/pt/pt-br/ro (1–3).
- **pt-br.fixed.txt DELETADO** (398 linhas) — trabalho consolidado em `pt-br.txt` (+58 novas, −17 removidas, 9 alteradas, incluindo 22 chaves de hotkey).
- **pt.txt**: +50/−17/190 alteradas (changelog 4_8_8_3.._6 existem apenas lá).
- **sv.txt**: +20/−2/~370 alteradas (inclui rename `SETTINGS_AUTONAMESTOPS_ENABLE`→`_AUTO`).
- Changelog: sem novas chaves `CHANGELOG_*`; apenas re-traduções em de/fr/sv/pt (200+265 linhas).

---

## 5. Infraestrutura e arquivos não-C#

| Arquivo | Mudança |
|---|---|
| `ImprovedPublicTransport4.csproj` | Exclui `Translations/*.fixed.txt` e `*.bak` do copy-to-output. Novos .cs entram via globs automáticos. |
| `packages.config` | CitiesHarmony.API 2.1.0→2.2.0; CitiesHarmony.Harmony 2.2.0→2.2.2. |
| `README.md` | (commitado) bump para 4.8.8 + link GitHub Releases + Ko-fi. |
| `Projeto-Steam/workshop-discussion-incompatible-mods-en.txt` | (commitado) nova seção sobre interação TMCE × IPT4 (CreateIncomingVehicle/CreateOutgoingVehicle). |
| `Integration/CommuterDestination/LICENSE.txt` | Deletado (atribuição MIT do mod upstream — substituída por reimplementação clean-room). |

---

## 6. Arquivos deletados / novos

**Deletados** (working tree, ainda presentes no git):
- `Integration/CommuterDestination/`: CitizenDestination.cs, DestinationGraph.cs, DestinationGraphGenerator.cs, DestinationGraphJourney.cs, DestinationGraphStop.cs, DestinationOverlayManager.cs, PatchController.cs, Patch_PublicTransportStopWorldInfoPanel.cs, StopDestinationInfoPanel.cs, StopPanelNavigationButton.cs, LICENSE.txt (~1.100 linhas de código).
- `Translations/pt-br.fixed.txt` (398 linhas).

**Novos (untracked)**:
- `Integration/CommuterDestination/CommuterDestinationOverlay.cs` (721 linhas) — clean-room, referenciado em `ImprovedPublicTransportMod.cs:495,500,926-927`, `SettingsActions.cs:725-729`, `OnMouseDownPatch.cs:57,61`.
- `Integration/SingleTrainTrackAI/NetworkChangePatch.cs` (74 linhas) — referenciado em `PatchController.cs:25,33`.
- `Integration/TrainDisplayUpdated/FpsCameraIntegration.cs` (86 linhas) — referenciado em `TrainDisplayWatcher.cs:36,80`.
- `Util/CompatibilityGuard.cs` (113 linhas) — ⚠️ **sem call sites (código morto)**.

**Ferramentas/docs (untracked, não vão no release)**: `Docs/` (design/plan de commuter-destinations, options-parent-child, vehicle-progress-bars, auditoria-ui-traducoes, mods-fix-de-bugs-e-saves), `BUGHUNT_2026-08-05.md`, `DEVELOPMENT_LEARNINGS.md`, `scripts/` (pipeline de tradução/QA: scan_mojibake, repair_double_encoded, check_encoding, verify_deploy, verify-critical-fixes, Deploy-Local.ps1, Reset-IptSettings.ps1, add_stop_keys.ps1), `fix_encoding.ps1`, `Translations/add_translations.py`, `Translations/update_future_desc.py`, `new_keys_ar.txt`, `Progetto-Steam/` (descrição italiana), `Proyecto-Steam/` (descrições es/latam), `Projeto-Steam/` (kit de release: 30 descrições, apply_steam_header.py, write_vdf.py, _verify.py, _fix_quotes.py, uploader/ órfão com apenas .pyc).

---

## 7. Pontos de atenção para a release

1. **Nada commitado**: todo o trabalho está no working tree. Fazer commit/PR antes de lançar; decidir o que entra (docs/scripts podem ficar de fora).
2. **CompatibilityGuard.cs é código morto** — ou conecta `RunLevelChecks()` (ex: no OnLevelLoaded) ou remove da release.
3. **`SETTINGS_SUPPORT_*` órfãs em 26 idiomas** — alinhar para `SETTINGS_KOFI_*` ou o botão Ko-fi vai mostrar chave crua nesses idiomas... na verdade o código usa `SETTINGS_KOFI_*` (presente no en), então os 26 idiomas cairão no fallback inglês (sem chave crua, mas sem tradução nativa do Ko-fi).
4. **Compatibilidade de saves**: leitura de saves v002/v003 mudou (fila de veículos mais fiel); `MovingAverage` refeito; versão de save **continua "v004"** (releitura diferente de antes — vale teste com um save antigo antes de lançar).
5. **Changelog do jogo não cobre a nova versão**: nenhuma chave `CHANGELOG_4_8_8_*`/`4_8_9_*` foi adicionada; a versão nova ficaria sem entry no painel "What's New" — decidir se adiciona.
6. **Steam**: `Projeto-Steam/write_vdf.py` ainda diz "Version 4.8.6" no changenote; as descrições do Workshop em `Projeto-Steam/` estão na versão 4.8.8 (commits `translations-4.8.8`); atualizar para a nova versão.
7. **Versão do assembly continua 4.8.8.0** (`Properties/AssemblyInfo.cs` não foi bumpado) — decidir bump para 4.8.9 antes do build final.
8. **Mudanças de UI que exigem teste em jogo**: barra verde de embarque (PanelExtenderVehicle), overlay de destinos, master switches (habilita/desabilita de sub-settings), hotkeys, Ko-fi no topo da página Geral.
9. **Risco moderado**: `JsonHelper.TypeNameHandling.None` — se algum save/setting JSON depender de polimorfismo, quebra; testar carregar settings antigos.
10. **IntercityBusControl restauração de prefabs**: mudança estrutural — testar desativar a integração com uma estação convertida em mapa salvo (agora restaura corretamente, mas o fluxo mudou).
