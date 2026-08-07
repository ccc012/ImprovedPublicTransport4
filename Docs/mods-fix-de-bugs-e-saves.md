# Mods que corrigem bugs do jogo e de saves — Cities: Skylines (CS1)

Pesquisa exaustiva em 03/08/2026. Alvo: **Cities: Skylines original (CS1)**, patch atual **Race Day 1.21.1-f9** (appid 255710). *Não* inclui CS2 nem CS1 Remastered.

Método: 5 agentes em paralelo + verificação direta nas páginas do Steam Workshop (fetch + navegador). Contagens de inscritos aproximadas, capturadas das páginas em 03/08/2026. Links oficiais do Workshop/GitHub.

> **Aviso:** a faixa "This item has been removed from the community" que aparece em várias páginas é **artefato de renderização da Steam** (aparece até em mods ativos, como FPS Booster). Não desinscrever por causa dela.

---

## 1. Saves / carregamento (mods que consertam saves quebrados ou que não carregam)

**1. Loading Screen Mod Revisited 1.1.14** — Workshop ID: `2858591409` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2858591409) — Downloads: ~975.660 — Atualizado: 07/04/2026 (1.21.1-f8). Autor: algernon + Chamëleon TBN.
- O que conserta: saves que não abrem e crashes de carregamento. Skip de prefabs (prédios, props, veículos, **redes e árvores**), opções "Load used assets" e "Load enabled", **"Safe Mode"** e **"Try to recover from Simulation Errors"** (usados no protocolo oficial de save que não abre), melhor uso de RAM e logging, aponta assets com LOD inválido.
- ⚠️ Importante (do próprio autor): **"Doesn't touch the savefile"** — ajuda a *carregar*, não repara o arquivo. Desinscrever (não apenas desativar) qualquer outra versão do LSM. Requer Harmony 2.2.2-0.

**2. Save Our Saves [NO LONGER SUPPORTED]** — Workshop ID: `529129546` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=529129546) — Downloads: ~11.383 — Atualizado: 05/03/2017. Autor: BloodyPenguin.
- O que conserta: **saves quebrados na tela de carregamento** (broken save games on level loading). Não corrige problema de mod nem NetInfo faltante. Comentários de 2023 confirmam que ainda resgata saves que o LSM não conseguiu.
- Notas: **sem suporte** (2017), mas funcional. Após o load pode "congelar" um tempo — normal.

**3. Ccs Savegame Recoverer** — Workshop ID: `2891024966` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2891024966) — Downloads: ~2.092 — Postado: 20/11/2022. Autor: Klyte45. *(verificado no navegador)*
- O que conserta: quando **o processo de salvar falha**, o jogo deixa um intermediário `.ccs` em `...\Saves\Temp\`. O mod converte `.ccs` em `.crp` carregável no menu Load Game. Funciona em qualquer versão do jogo.

**4. Skyve v4.0 [Stable]** — Workshop ID: `2881031511` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2881031511) — Downloads: ~582.781 — Atualizado: 25/03/2026. Autor: Chamëleon TBN + equipe.
- O que conserta: **previne saves quebrados por mods**: detecta mods que vão quebrar o save (ou que quebraram), conflitantes, dependências faltantes, load order errada; botão de correção em um clique; sucessor do Compatibility Report + Loading Order Mod 1/2.
- Notas: não assinar junto com o Beta `2953447919`. GitHub `JadHajjar/Skyve-CS1`.

**5. Broken Nodes Detector** — Workshop ID: `1777173984` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1777173984) — Downloads: ~123.083 — Atualizado: 22/03/2023. Autor: Krzychu1245.
- O que conserta: "broken node bug" (veículos despawam), "ghost nodes"/erro "Array Index", rotas/paradas de transporte quebradas, segmentos curtos demais, prédios sem acesso a rua — problemas que **travam a simulação ao carregar o save**. Requer Harmony.

**6. Check Road Access for Growables** — Workshop ID: `2454302667` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2454302667) — Downloads: ~17.036 — Atualizado: 05/06/2023. Autor: egi.
- O que conserta: prédios sem acesso a rua (causado por Move It!/RICO/etc.) → serviços param; botão "recheck for all buildings" para cidades existentes.

---

## 2. Performance e gráficos (bugs de FPS/stutter/render)

**1. FPS Booster** — Workshop ID: `2105755179` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2105755179) — Downloads: ~1.085.720 — Atualizado: 10/03/2026 (1.21.1-f5). Autor: Krzychu1245.
- O que conserta: a cada frame o jogo atualiza **10.000+ componentes de UI** sem necessidade; o mod renomeia `Update()/LateUpdate()` da `UIComponent` em runtime → ganho real de FPS. Inclui todos os patches do Mini FPS Booster.
- Notas: **requer Patch Loader Mod** (auto-inscreve). Desativar sem desinscrever o Patch Loader **não** desativa os patches. Bug de fontes asiáticas (desativar Custom Font Manager). Limiter incluído (GPU de notebook).

**2. Patch Loader Mod** — Workshop ID: `2041457644` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2041457644) — Downloads: ~1.245.050 — Atualizado: 24/11/2022. Autor: Krzychu1245 + egi.
- O que conserta: **dependência central do FPS Booster** — aplica patches em runtime antes da inicialização do Mono. ⚠️ Última atualização nov/2022; há relatos de falha no patch 1.21 e de freeze no boot em Linux — validar na instalação.

**3. FPS Drop / Stutter Fix** — Workshop ID: `3772833571` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=3772833571) — Downloads: ~862 — Postado: 27/07/2026. *(verificado no navegador)*
- O que conserta: **spikes de FPS/stutter**, principalmente ao selecionar um prédio, causados por erro-spam do FPS Booster; hub de bugfixes para FPS Booster, Automatic Pedestrian Bridge e Transport Lines Manager. Use junto com o FPS Booster até os donos corrigirem. GitHub `Shadrous/fps-drop-stutter-fix`.

**4. Loading Screen Mod Revisited** — ID `2858591409` (item 1.1 da seção anterior) — também conserta **carregamento lento, RAM excessiva e crashes por memória**, além de apontar assets com LOD inválido que reduzem FPS.

**5. ACME 1.0.2** — Workshop ID: `2778750497` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2778750497) — Downloads: ~514.606 — Atualizado: 07/07/2026. Autor: algernon + Chamëleon TBN.
- O que conserta: **flicker da luz do mouse à noite** perto de prédios/solo e **sombras piscando** (raycast do jogo bloqueado por terreno/prédio); parâmetros de sombra customizáveis. Desde v0.6 **substitui o Shadow Distance Fix**.
- Notas: requer Harmony. Incompatível/desnecessário junto de Camera Positions Utility, Zoom It, Mouse Drag Camera, Zoom To Cursor (funcionalidades embutidas).

**6. Render It!** — Workshop ID: `1794015399` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1794015399) — Downloads: ~371.696 — Atualizado: 23/05/2023. Autor: Keallu.
- O que conserta: render — anti-aliasing (FXAA/TAA), filtragem anisotrópica, **mip map bias (texturas embaçadas)**, sombras, **fog estático e dinâmico** (edge fog), Ambient Occlusion, Bloom, Color Grading.
- Notas: requer Harmony. **NÃO usar junto** com Shadow Strength Adjuster, Sharp Textures, PostProcessFX, Daylight Classic, Softer Shadows, Relight, Fog Controller, Clouds & Fog Toggler.

**7. Hide It!** — Workshop ID: `1591417160` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1591417160) — Downloads: ~436.688 — Atualizado: 23/05/2023. Autor: Keallu.
- O que conserta: **esconde elementos que poluem/bugam a cena**: sprites de decoração, poluição roxa de chão, sujeira, **edge fog**, botões de UI, props e efeitos. Unifica No Purple Pollution, Remove Decoration Sprites e Remove Dirt. Requer Harmony. Fork mantido: **HideItBobby** (vitalii2011).

**8. Ultimate Level Of Detail (ULOD)** — Workshop ID: `1680642819` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1680642819) — Downloads: ~365.448 — Atualizado: 24/05/2021. Autor: boformer.
- O que conserta: **pop-in/flicker de LOD** — aumenta a distância de troca para LOD de alta qualidade em árvores, props, prédios e redes; inclui **dropdown de distância de sombra** (cobre o papel do antigo "Shadow Distance Tuner"). Baixo impacto de performance. GitHub `boformer/UltimateLevelOfDetail`.

**9. Clouds & Fog Toggler** — Workshop ID: `523824395` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=523824395) — Downloads: ~749.438 — Atualizado: 19/04/2016 (usuários confirmam que segue funcionando). Autor: BloodyPenguin.
- O que conserta: **nuvens e névoa de distância que bloqueiam a visão** ao afastar a câmera; desliga também smog industrial e edge fog. Criado porque a atualização After Dark quebrou o render de nuvens. Não usar junto com Render It! (fog sobreposto).

---

## 3. Gameplay / bugs do jogo base

**1. Traffic Manager: President Edition (TM:PE)** — Workshop ID: `1637663252` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1637663252) — Downloads: ~3.410.000 — Atualizado: 10/03/2026. GitHub `VictorPhilipp/Cities-Skylines-Traffic-Manager-President-Edition`.
- Bugs do vanilla corrigidos: **escolha aleatória de faixas**, vários bugs de **Parking AI** (turistas "despawnando", cims pulando no lugar esperando rota), **trens preferindo a via principal em vez de desviar** em shunts, seleção de faixa em viradas em U/becos, veículos travados.

**2. Transfer Manager CE** — Workshop ID: `2804719780` (estável) / `2810557345` (teste) — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2804719780) — Atualizado: 10/07/2026 (v3.1.38). GitHub `Sleepy334/TransferManagerCE`.
- Bug do vanilla corrigido: o **Transfer Manager escolhia alvos distantes** (caminhão de lixo cruzava a cidade enquanto os próximos acumulavam). Reescrito com algoritmos baseados em **rede de estradas/distância**, AI de serviço prioriza problemas próximos, multi-thread.
- Notas: sucessor do "More Effective Transfer Manager". Incompatível com District Dispatch, Taxi Stand Fix, Call Again, Transfer Controller.

**3. Better Train Boarding** — Workshop ID: `2773460744` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2773460744) — Downloads: ~112.859 — Atualizado: 07/06/2025.
- Bug corrigido: **ordem de embarque em trens** (passageiros embarcavam "quem chegou por último, primeiro") e outros problemas de embarque/desembarque.

**4. Public Transport Unstucker** — Workshop ID: `2774427140` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2774427140) — Downloads: ~14.700 — Atualizado: 08/04/2026.
- Bug corrigido: **veículos de transporte público travados** (stuck) — detecta e remove/reenvia veículos presos que no vanilla ficavam parados para sempre.

**5. More PathUnits** — Workshop ID: `2710657019` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2710657019) — Downloads: ~66.691 — Atualizado: 10/03/2026.
- Bug corrigido: **limite vanilla de unidades de pathfinding** — com muita gente/veículos, cims congelavam no lugar ou ficavam sem rota. Aumenta o pool (com contador de uso em tempo real).

**6. Optimised Outside Connections** — Workshop ID: `1721492498` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1721492498) — Downloads: ~585.445 — Atualizado: 23/05/2023.
- Bug corrigido: **importação/exportação quebrava quando a única saída congestionava** (caminhões fazendo viagens absurdas ao "exterior"); gerações de veículos de conexão externa atravessando o mapa.

**7. Improved Public Transport 4 (IPT4)** — Workshop ID: `3773802930` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930) — GitHub `ccc012/ImprovedPublicTransport4` — *é o próprio mod deste repo*.
- Bugs corrigidos (4.8.8): **Intercity Bus Control silenciosamente desabilitado para donos do Sunset Harbor** (check de DLC errado), aba desatualizada em sub-prédios após demolir/reconstruir, conflitos de estado por linha do IPT3. Fork do IPT3 que absorve mods de transporte.

**8. Lifecycle Rebalance Revisited 1.6.8** — Workshop ID: `2027161563` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2027161563) — Downloads: ~398.793 — Publicado: 19/03/2020. GitHub `algernon-A/Lifecycle-Rebalance-Revisited`. *(verificado no navegador: página ativa, apesar de um agente ter reportado remoção — falsa)*
- Bug corrigido: **death waves** — no vanilla os cims entram com a mesma idade e morrem juntos; o mod distribui idades (curva de mortalidade realista, morte precoce, imigrantes com idades variadas). Não resolve uma onda já em curso (só as futuras). Sucessor do "Citizen Lifecycle Rebalance".

---

## 4. UI/UX e ferramentas quebradas

**1. Yet Another Toolbar** — Workshop ID: `2448994345` — [link](https://steamcommunity.com/workshop/filedetails/?id=2448994345) — Downloads: ~493.875 — Atualizado: 16/12/2022. GitHub `sway2020/YetAnotherToolbar`.
- O que conserta: toolbar/painéis **mal dimensionados ou cortados por excesso de assets** (overflow do vanilla); linhas/colunas, escala de painel, compatibilidade com Find It 2, Ploppable RICO Revisited, UI Resolution e UnifiedUI.
- Notas: mantido. Requer Harmony. **Incompatível com Resize It!** (não usar juntos), Advanced Toolbar, More Advanced Toolbar. É a alternativa moderna ao More Advanced Toolbar.

**2. UI Resolution 1.3.4** — Workshop ID: `2487213155` — [link](https://steamcommunity.com/workshop/filedetails/?id=2487213155) — Downloads: ~133.540 — Atualizado: 11/03/2026. GitHub `MacSergey/UIResolution`.
- O que conserta: a UI vanilla é sempre 1920x1080 independente da resolução real — em monitores grandes fica gigante/desproporcional; escala a UI para a resolução e permite escala manual.
- Notas: mantido e atualizado (autor é funcionário da Colossal Order em conta pessoal). Requer Harmony. Aviso: mods com UI de posições fixas podem desalinhar. Substituto estável do ScaleUI (morto).

**3. Resize It!** — Workshop ID: `1577882296` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1577882296) — Downloads: ~146.710 — Atualizado: 23/05/2023. GitHub `keallu/CSL-ResizeIt`.
- O que conserta: **painéis roláveis pequenos/cortados** (listas de assets sem scroll útil); aplica a Extra Landscaping Tools, Surface Painter, Ploppable RICO Revisited e Find It! 2. Sem detours, não altera saves.
- Notas: substitui Advanced Toolbar, Extended Toolbar, Enhanced Build Panel e More Advanced Toolbar. Incompatível com YAT.

**4. Extended InfoPanel 2** — Workshop ID: `2498761388` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2498761388) — Downloads: ~23.373 — Atualizado: 25/05/2023.
- O que conserta: painel de informações com **texto pequeno/ilegível** (recurso "Friendly Tooltips"), painéis de Heating e Public Library, contagem de passageiros/carros estacionados, suporte a Fish/Mail em importação/exportação, ícones customizados.
- Notas: substitui 4 mods antigos (Extended InfoPanel `781767563`, More Simulation Speed Options `412292157`, HideUI `406326408`, Chirper Position Changer `405963579`).

**5. More Advanced Toolbar** — Workshop ID: `1597852915` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=1597852915) — Downloads: ~94.919 — Atualizado: 18/06/2019.
- O que conserta: reposicionar/redimensionar toolbar e painel, opacidade, esconder botão do advisor. ⚠️ **ABANDONADO e obfuscado**; Skyve marca como quebrado; prefira Yet Another Toolbar ou Resize It!.

**6. Improved Asset Icons** — Workshop ID: `508195208` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=508195208) — Downloads: ~26.150 — Atualizado: 19/09/2015.
- O que conserta: assets custom **sem ícone (caixa azul)** usam o preview do Workshop; ícones borrados fora de 1080p. ⚠️ Muito desatualizado. Variante funcional: **"Improved Asset Icons (alternative)"** `747836519`. Em caso de incompatibilidade, usar Find It! 2.

---

## 5. Frameworks / compatibilidade (previnem bugs causados por mods e saves)

**1. Harmony (Mod Dependency) 2.2.2-0** — Workshop ID: `2040656402` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402) — Downloads: ~4.145.357 — Atualizado: 12/12/2022.
- Previene conflitos de versão de Harmony entre mods; framework exigido por praticamente todos os mods (incluindo IPT4). Assinar só este; ignorar "[CS] Harmony 2.0 Mods" (`2384284008`).

**2. Skyve** — ID `2881031511` (item 1.4) — também **ordena mods deterministicamente** (Harmony primeiro) e mantém lista de quebrados/incompatíveis.

**3. AutoRepair / Mod Compatibility Checker** — GitHub `CitiesSkylinesMods/AutoRepair` — avisa na inicialização sobre mods conhecidamente incompatíveis/quebrados (mesma base embutida no Skyve).

**4. Patch Loader Mod** — ID `2041457644` (item 2.2).

**5. UnifiedUI (UUI) 2.2.1 - Continued [BETA]** — Workshop ID: `2966990700` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2966990700) — Downloads: ~277.769 — Atualizado: 17/07/2023.
- Previene **conflitos de hotkey e botões de mods sobrepostos**; centraliza a barra de ativação. Desinscrever o original (`2255219025`); exige Skyve para ordem de carregamento.

**6. Extended Error Reporting** — Workshop ID: `2055465280` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=2055465280) — Downloads: ~48.614 — Atualizado: 19/07/2020.
- Logs de erro detalhados para diagnosticar exceptions causadas por outros mods.

**7. ConflictSolver** — GitHub `dymanoid/conflictsolver` — sem ID no Workshop (instalar como mod local) — detecta conflitos entre mods (assemblies duplicados etc.).

---

## 6. Novos fixes de 2026 (pós-patch Race Day 1.21.1)

**1. Race Day Bug Fix v1.7** — Workshop ID: `3706034984` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=3706034984) — Downloads: ~1.866 — Atualizado: 22/07/2026 (publicado 13/04/2026). *(verificado no navegador)*
- Corrige bugs do patch Race Day: **NullReferenceExceptions** (EventManager.ScheduleEventRoute, RaceEventAI.GetDisorganizingEndFrame, DistrictPark.EndTogaParty), listas de cidadãos corrompidas, **espectadores de corrida que não spawnavam**.

**2. FPS Drop / Stutter Fix** — ID `3772833571` (item 2.3) — hub de correções pós-Race Day para FPS Booster, Automatic Pedestrian Bridge e Transport Lines Manager.

**3. Commuter Destination (Unofficial Bugfix)** — Workshop ID: `3704654752` — [link](https://steamcommunity.com/sharedfiles/filedetails/?id=3704654752) — Downloads: ~319 — Atualizado: 11/04/2026. GitHub `Vectorial1024/CSL-ShowCommuterDestination`.
- Corrige falha de patch do "Commuter Destination" com IPT2 (relevante para quem usa IPT4/IPT3).

---

## 7. Listas curadoras / fontes para checar antes de instalar

1. **Coleção "Recommended Mods (updated April 2026)"** — Workshop `3617102779` — 57 mods, núcleo de framework/bugfix (Harmony, Patch Loader, LSM, FPS Booster, UUI, Broken Nodes Detector) + TM:PE, Move It, Find It! 2, 81 Tiles 2, Network Anarchy, Node Controller Renewal, Intersection Marking Tool e forks de correção 2025/26 (IPT3, Network Skins Continued, Roundabout Builder UUI Fix, Procedural Objects popup fix, Hide TM:PE Crosswalks: Renewed).
2. **Coleção "2026 | Essential Mods | Updated"** — Workshop `3684196285` — 61 itens (mistura essenciais, visuais e autopromoção — filtrar).
3. **Paradox Wiki — Mod troubleshooting** — https://skylines.paradoxwikis.com/Mod_troubleshooting — wiki oficial; planilha de mods quebrados: https://pdxint.at/BrokenModCS
4. **Guia note.com "Cities: Skylines おすすめMOD図鑑 2026"** — https://note.com/msg_14/n/n805ccb2d4179 — atualizado 25/07/2026 (patch 1.21.1-f9).
5. **Vídeo "How to Fix Broken Mods in Cities: Skylines (2026)"** — https://www.youtube.com/watch?v=d6jxk-qvmi8 — 18/01/2026.
6. **Fórum Paradox — mods CS1** — https://forum.paradoxplaza.com/forum/forums/cities-skylines.859/ — inclui a thread "Race Day Patch 1.21.1-f4 – State of Mods".
7. ⚠️ Coleção antiga "[Cities Skylines] Broken & Incompatible Mods" (`1800020000`) está **morta** — usar a planilha `pdxint.at/BrokenModCS` + lista do Skyve/AutoRepair.

---

## 8. Excluídos / descontinuados / substitutos

| Mod (ID) | Motivo / substituto |
|---|---|
| Safenets (`1620588636`) | Superseded; a própria página recomenda a opção "Try to recover from Simulation Errors" do LSM Revisited |
| Phantom Lane Remover (`536250255`) | Quebrado após DLC Airports → usar Broken Nodes Detector |
| Mod Compatibility Checker (`2034713132`) | Obsoleto → Skyve |
| Compatibility Report (`2633433869`) | Aposentado → Skyve |
| Loading Order Mod (`2620852727`) | [END OF LIFE] → Skyve |
| Loading Order Mod V1.15.7 (`2448824112`) | Quebrado |
| More Effective Transfer Manager (`1680840913`) | Não funciona mais → Transfer Manager CE |
| Transport Lines Manager (`3007903394`) | Removido da Workshop; versão antiga `1312767991` (out/2022); hub de fixes: FPS Drop/Stutter Fix |
| Citizen Lifecycle Rebalance (`654707599`) | Deprecated → Lifecycle Rebalance Revisited |
| Advanced Vehicle Options (`1548831935`) | Contorna bugs, não corrige comportamento (duvidoso) |
| Realistic Walking Speed (`1412844620`) / Realistic Population 2 (`2025147082`) / Stops & Stations (`1776052533`) / Rebuild It! (`2863930641`) / Game Anarchy (`2781804786`) / No Deathcare (`803074771`) | Rebalance/QoL, não bugfix puro (duvidosos) |
| "Traffic Fix Cities Skyline" (`2522112022`) | Insere interseções pré-construídas; não corrige IA |
| ScaleUI (`2040218778`) | Quebrado → UI Resolution |
| Extended Toolbar (`451700838`) | Removido; scroll na toolbar já é nativo |
| Enhanced Build Panel | Abandonado → Resize It! |
| Shadow Distance Fix (`2126881996`) | Deprecado → ACME v0.6+ |
| "Performance Tuner" / "Shadow Distance Tuner" | **Não existem** como mods standalone; funções no FPS Booster/ULOD/ACME e no guia `465790009` |
| Sun Shafts (`933513277`) | Cosmético e pesado (-20/-30 FPS) |
| Mesh Info (`453956891`) | Ferramenta de diagnóstico, não conserta nada |
| Tree LOD Fix (`1349895184`) | Visual, não bugfix; coberto pelo ULOD |
| Fog Controller (`2483920668`) | Cosmético/quebrado; sobreposto por Render It! e Clouds & Fog Toggler |
| Mini FPS Booster (`1938493221`) | Redundante (FPS Booster já inclui tudo) |
| Daylight Classic (`530871278`) / Visibility Control | Cosmético / pesado ("fps killer") |
| Dynamic Resolution (`812713438`) | Render/AA, quebrado no Mac, só 1.9 |
| ModTools (`409520576`) | Utilitário de modder, não conserta bug para o jogador |
| Tooltip Fixer (`2600273722`) / Better UI Scaling (`2217509277`) | **Não são mods de CS1** (Darkest Dungeon / CK3) |
| "Useless Tools Remover" / "Broken Assets Detector" | **Não existem** no Workshop de CS1 |

---

## Resumo de dependências e conflitos

- **Harmony 2.2.2-0** (`2040656402`) — exigido por: LSM Revisited, ACME, Render It!, Hide It!, YAT, UI Resolution, Broken Nodes Detector, IPT4.
- **Patch Loader Mod** (`2041457644`) — obrigatório **apenas** para o FPS Booster.
- **Não instalar juntos:** YAT ↔ Resize It!; Render It! ↔ (Fog Controller, Clouds & Fog Toggler, Daylight Classic, Softer Shadows, Relight, PostProcessFX, Shadow Strength Adjuster, Sharp Textures); FPS Booster ↔ Mini FPS Booster; ACME ↔ (Camera Positions Utility, Zoom It, Mouse Drag Camera, Zoom To Cursor).
- **Skyve** deve ser o primeiro na ordem de carregamento (junto com Harmony).
