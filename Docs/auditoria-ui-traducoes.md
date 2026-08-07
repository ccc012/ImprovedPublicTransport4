# Auditoria — UI e Localização · Improved Public Transport 4 (v4.8.8)

> Data: 2026-08-03 · Base: `c0d9f5e` (4.8.8, árvore git limpa)
> Foco: blockers de release relacionados a tradução (encoding de language packs, chaves órfãs, carregamento de locales) e estado da UI.

---

## 1. Resumo executivo

A infraestrutura de tradução e a UI estão, no geral, saudáveis: todos os 38 packs
têm as 625 chaves, o medidor de completude de todos os idiomas está ≥ 96%, o
changelog 4.8.0→4.8.8 está corretamente configurado e os hot paths de UI já têm
throttling. **Porém há 2 achados críticos de encoding que distorcem o texto de 6
idiomas inteiros (DE/FR/HU/SV/PT/PT-BR) e devem ser corrigidos antes do release**,
ambos com correção automatizada já existente ou trivial. Também há 1 arquivo
obsoleto que é carregado como idioma (`pt-br.fixed.txt`) e 1 lote de chaves órfãs.

- **C1 — Crítica:** `de/fr/hu/sv.txt` com dupla codificação UTF-8 (mojibake)
  introduzida no commit `cefe47b` (2026-08-01). Só 4 arquivos, correção via script.
- **C2 — Crítica:** `pt.txt`/`pt-br.txt` com corrupção parcial cp1252 nas chaves
  novas pós-4.8.5. O script de reparo já mira exatamente esses dois arquivos.
- **A1 — Alta:** `pt-br.fixed.txt` obsoleto (472 chaves, 153 faltando) é rastreado
  no git e surge como idioma no dropdown de opções. Deve ser excluído.
- **A2 — Média:** 7 chaves órfãs `COMMUTER_DESTINATION_*` traduzidas nos 38 packs
  (o código usa `COMMUTERDESTINATION_PANEL_*`, sem underscore).
- **A3 — Média:** `en.txt:368` com bullets `•` corrompidos em `â€¢` (linha de
  changelog em inglês exibe lixo).

Nada aqui impede a arquitetura; são correções de dados/tradução com impacto alto
na percepção do usuário. O plano de correção está na seção 6.

---

## 2. Escopo, base e método

- **Repositório:** `C:\Users\Lucas\source\repos\cs1_ipt4` (não é `cs1_ipt4\ImprovedPublicTransportMod`).
- **Base auditada:** commit `c0d9f5e` (v4.8.8), `git status` limpo.
- **Arquivos auditados:**
  - `Translations/*.txt` (38 arquivos, varredura byte-level de encoding);
  - `TranslationFramework/LocalizationManager.cs`, `Localization.cs`,
    `LanguageFormat/PlainTextLanguageDeserializer.cs`;
  - `IptModManager.cs` (changelog), `CSLModsCommonShared/ChangelogCollection.cs`,
    `CSLModsCommonShared/UI/Dialogs/ChangelogDialog.cs`;
  - `UI/CSLModsCommonOptionsPanel.cs`, `UI/StopListBoxRow.cs`, `UI/VehicleListBoxRow.cs`,
    `UI/VehicleEditor.cs`, `UI/PreviewRenderer/PreviewRenderer.cs`,
    `UI/PanelExtenders/*`;
  - `Integration/CommuterDestination/StopDestinationInfoPanel.cs`.
- **Método:** análise estática de referências de chaves + varredura byte-level
  (contagem de marcadores U+00C3/`Ã` — assinatura de dupla codificação — e
  classificação por idioma) + `git show`/`git log` para localizar o commit de
  introdução da corrupção + `Compare-Object` entre packs.

---

## 3. Arquitetura de localização

O mod usa **dois** sistemas de tradução:

1. **IPT4 `ImprovedPublicTransport.Localization`** (`Localization.cs`) → carrega
   `Translations/*.txt` via `PlainTextLanguageDeserializer` (aceita `.txt` simples).
   `Localization.cs:10–16` exige `LocalizationManager(typeof(Mod), ...)` — o tipo
   `Mod`, não `ImprovedPublicTransportMod` (um tipo errado aqui silenciosamente
   quebra a descoberta de idiomas). É o sistema que alimenta toda a UI e o changelog.
2. **CSLModsCommon `CSLModsCommonShared.Manager.LocalizationManager`** → carrega
   CSV sob `AlgernonCommons/Translations/` (no repo só existe `en-EN.csv`). Usado
   pelos diálogos/notificações do framework (ex.: `NOTE_CLOSE`,
   `CONFLICT_DETECTED`). Somente inglês é um risco menor (textos de framework
   ficam em inglês para jogadores não anglófonos, mas não há blocos).

**Aliases de locale** (`PlainTextLanguageDeserializer.cs:15` `LocaleAliases`)
mapeiam IDs de idioma do jogo (ex.: `es-419`, `zh-cn`, `ko`, `kr`, `zh`) para os
stems de arquivo. É por isso que `kr.txt`/`zh.txt` colidem com `ko.txt`/`zh-cn.txt`
(v. achado M3).

**Medidor de completude** (`Util/TranslationCompleteness.cs`): usada no painel de
opções; trata qualquer valor byte-idêntico ao inglês como "não traduzido". Todos os
idiomas ficam ≥ 96%. Nota: esse medidor **não detecta mojibake** — os valores
corrompidos diferem do inglês e contam como traduzidos (por isso o C1 passou
despercebido).

---

## 4. Achados

### 4.1 Críticos (bloqueiam release)

#### C1 — `de.txt`, `fr.txt`, `hu.txt`, `sv.txt`: dupla codificação UTF-8 (mojibake)

**Onde:** `Translations/{de,fr,hu,sv}.txt` (todas as linhas acentuadas).

| Arquivo | Ocorrências do marcador `Ã` (U+00C3) | Outros marcadores |
|---|---|---|
| `hu.txt` | 1799 | 195 |
| `sv.txt` | 1608 | 6 |
| `fr.txt` | 1486 | 46 |
| `de.txt` | 455 | 6 |

**Evidência:** em HU/SV/FR/DE o caractere `Ã` não existe (nunca é legítimo);
centenas/milhares de ocorrências = sequências UTF-8 de 2 bytes (0xC3 0x80–0xBF)
decodificadas como cp1252/latin-1 e re-salvas. Exemplos: alemão `öffentlichen` →
`Ã¶ffentlichen`, francês `Gestion avancée` → `Gestion avancÃ©e`, húngaro
`Továbbfejlesztett` → `TovÃ¡bbfejlesztett`, sueco `Förbättrad` → `FÃ¶rbÃ¤ttrad`.

**Origem:** `git bisect`-like — `fr.txt` em `cefe47b~1` tem 0 ocorrências; em
`cefe47b` (2026-08-01, "Close most of the post-4.8.5 translation gap across 32
languages") vai a 1073; HEAD mantém 1073. Ou seja: a corrupção entrou no commit de
preenchimento da lacuna de traduções.

**Impacto:** jogadores DE/FR/HU/SV veem praticamente todo texto acentuado
corrompido na interface e no changelog; o medidor de completude não acusa (valores
≠ inglês = "traduzido").

**Correção:** estender `scripts/fix_mojibake_translations.py` (hoje mira apenas
`pt-br.txt`/`pt.txt`, linha 76) para aceitar os 4 arquivos, ou restaurar esses 4
packs de `cefe47b~1`. Backup antes; commit separado.

#### C2 — `pt.txt`/`pt-br.txt`: corrupção parcial cp1252 nas chaves pós-4.8.5

**Onde:** `Translations/pt.txt` e `Translations/pt-br.txt` (ambos com 625 chaves).

**Evidência:** cada um tem 1 `Ã` + 2 marcadores; o `Compare-Object` ordenado
mostra dezenas de caracteres latinos crus de byte único (cp1252) embutidos em
arquivo UTF-8, concentrados nas chaves novas do gap-fill (ex.:
`SETTINGS_HIDDENBEHAVIOUR_GROUP_DESC`, `SETTINGS_AUTONAMESTOPS_ENABLE_TOOLTIP`,
`SETTINGS_FUTURE_BUSWAYPOINT_TIP`, `SETTINGS_RESCUEFULLWIDTHDIGITS_ENABLE_TOOLTIP` —
"opção" → `op��ǜo`, "prédio" → `pr��dio`).

**Impacto:** o texto português exibe caracteres corrompidos em várias chaves de
configuração.

**Correção:** rodar `scripts/fix_mojibake_translations.py` (já alvo desses 2
arquivos) — a função `fix_line` cobre tanto decodificação de linha inteira quanto
recuperação greedy por pedaços. Backup antes.

> Verificação de contraste: `ro.txt` (11× `Î`, legítimo em romeno), `tr.txt` (11×
> `Â`, legítimo em turco), `da.txt` (4× `Å`, legítimo em dinamarquês) e
> `cs/sk/no` estão **corretos** — seus marcadores são caracteres reais do idioma,
> não mojibake.

### 4.2 Altos

#### A1 — `pt-br.fixed.txt` obsoleto é carregado como idioma

**Onde:** `Translations/pt-br.fixed.txt` (rastreado no git; ~57 KB; 472 chaves,
153 faltando — inclusive `SETTINGS_HOTKEY_LINE_COLOR_TOOLTIP` e
`SETTINGS_TRAINDISPLAY_TYPES_GROUP_DESCRIPTION`).

**Impacto:** o nome do arquivo deriva o locale `pt-br.fixed`, que **aparece como
opção no dropdown de idiomas** e contém texto desatualizado com mojibake.

**Correção:** excluir do repositório e do deploy.

### 4.3 Médios

#### M1 — `en.txt:368` (`WHATSNEW_3_0_0_2`): bullets corrompidos

13 ocorrências de `\n â€¢` (deve ser `\n •`). É a linha de changelog em inglês
exibindo lixo. Foi introduzida no commit recente de ajuste do `en.txt`.

#### M2 — Orfanato de chaves `COMMUTER_DESTINATION_*`

`en.txt:36–42` (e, por espelhamento, os 37 packs): `COMMUTER_DESTINATION_PANEL_TITLE`,
`_HEADER`, `_NONE`, `_LOADING`, `_BUTTON`, `_BUTTON_TOOLTIP`,
`COMMUTER_STOP_WITH_WAITING`. O código usa `COMMUTERDESTINATION_PANEL_*` (sem
underscore; `en.txt:612–617`, em
`Integration/CommuterDestination/StopDestinationInfoPanel.cs`). Referência antiga
ainda em `Docs/referencia-commuterdestination/...` aponta para a versão antiga.

**Correção:** remover as 7 chaves dos 38 packs ou religar o painel a elas.

#### M3 — `kr.txt` e `zh.txt` duplicatas de `ko.txt`/`zh-cn.txt`

Os aliases `kr→ko` e `zh→zh-cn` fazem os arquivos serem carregados e descartados
pelo dedup a cada start (com aviso "Skipping duplicate localisation file").
Peso morto + ruído de log. Remover ou manter documentado.

#### M4 — `autoGenerate` do changelog é inoperante (latente)

`CSLModsCommonShared/ChangelogCollection.cs:26–28` monta
`PrefixKey = "Changelog_v{Major}_{Minor}_{Build}"` e `:54–84` filtra chaves com
`Contains(PrefixKey)` / `Contains($"{PrefixKey}_{flag}")`. As chaves reais do IPT
são `CHANGELOG_4_8_8_1` (maiúsculas, sem "v", sufixo numérico) → `Contains` nunca
casa (case-sensitive). **Não quebra hoje** porque todos os usos em
`IptModManager.cs:198+` passam `autoGenerate: false` e
`ChangelogDialog.cs:105` (`GenerateFromLocalization`) vira no-op. Se alguém ligar
`autoGenerate`, o changelog sai vazio.

**Correção:** `PrefixKey = $"CHANGELOG_{Major}_{Minor}_{Build}"` e casar o flag
pelo sufixo numérico.

#### M5 — Chaves `SETTINGS_TAB_*` (sem sufixo) mortas

`en.txt:63–73, 90, 144, 370–371`: `SETTINGS_TAB_GENERAL`, `_AUTOLINE`, `_STOPS`,
`_UNBUNCHING`, `_DELETE`, `_FLEET`, `_BUDGET`, `_PERFORMANCE`, `_COMPATIBILITY`,
`_TRAINDISPLAY`, `_INTEGRATIONS`. O código só usa `_FLEET_SHORT`/`_STOPS_SHORT`/
`_OVERLAY_SHORT`/`_SYSTEM_SHORT` (`UI/CSLModsCommonOptionsPanel.cs:85–105`) e
`_LINECOLORS` (`:526`). Legado da reorganização de abas da 4.7.0.8. Limpar ou
reutilizar.

#### M6 — Sem fallback automático para `en.txt` quando a chave faltar

`TranslationFramework/LocalizationManager.cs:283–311`: chave ausente retorna a
**própria chave** (`SETTINGS_XXX` crua); o fallback para inglês existe apenas como
último recurso em `Localization.cs:132` (`TryGetTranslationFromLocaleFile`). Hoje
todos os packs estão completos (0 faltantes), então o risco é baixo — mas qualquer
pack futuro desatualizado exibirá chaves cruas em vez de inglês.

**Correção:** fallback automático para `en.txt` antes de retornar a chave.

### 4.4 Baixos / Info

#### B1 — 7 checkboxes "futuras" sem efeito
`UI/CSLModsCommonOptionsPanel.cs:255–263` + `:314` (`AddFutureSpoiler` é no-op):
`SETTINGS_FUTURE_*` traduzidas em 38 idiomas, mas desabilitadas e sem comportamento.
Poluem a UI com controles mortos. Remover ou mover para docs.

#### B2 — Offsets/layout hardcoded com TODO
- `UI/PanelExtenders/PanelExtenderCityService.cs:20` — `VerticalOffset = 40f`,
  TODO "needed due to the UI issue, revert if CO fixes the panel".
- `UI/PanelExtenders/PanelExtenderLine.cs:933,960` — `uiPanel.height = 200f`,
  TODO "do we really need to set?".

#### B3 — Cor da linha não aplicada no preview
`UI/PreviewRenderer/PreviewRenderer.cs:213` — TODO "set line color, also clean up
how it's set up": o `MaterialPropertyBlock` só recebe `ID_VehicleColor` quando
`_colorSet`; a cor da linha não é aplicada ao preview do veículo.

#### B4 — BOM inconsistente
7 arquivos com BOM UTF-8 (`bg, fr, hu, ru, sk, sv, uk`) vs 31 sem. Não quebra
(`File.ReadAllLines` detecta BOM), mas os scripts gravam sem BOM e removeriam o BOM
silenciosamente ao rodar. Normalizar (recomendo: todos sem BOM).

#### B5 — Chaves legadas `WHATSNEW_3_0_0_*` do IPT3
Não usadas pelo changelog CSLModsCommon; apenas a `WHATSNEW_3_0_0_2` ainda consta
(e está corrompida, v. M1).

#### B6 — Hot paths de UI já otimizados (bom estado, sem ação)
- `StopListBoxRow.Update()` 0,5 s nome + 1,5 s varredura de passageiros;
- `VehicleListBoxRow.Update()` 0,5 s; `PanelExtenderLine` 0,25 s;
- `VehicleEditor` com cache de `Find<>`; throttles 0,2–0,5 s no resto;
- `PublicTransportStopWorldInfoPanel`/`VehicleListBoxRow` usam throttles,
  caches e query pools — sem alocação relevante por frame.

#### B7 — `pt.txt` vs `pt-br.txt` são traduções regionais **genuínas** (correção de nota anterior)
`Compare-Object` ordenado e case-sensitive: **70/625 linhas diferem** com
vocabulário real de PT-PT vs PT-BR (ex.: "paragens"/"paradas",
"correram"/"rodaram", "dá-lhe"/"dê a ela", "guardados"/"salvos", "introdução"/
"inserção"). **Não** são duplicatas — não remover `pt.txt`. Ambos, porém, têm a
corrupção do C2.

---

## 5. Estado por idioma (resumo da varredura)

| Estado | Idiomas |
|---|---|
| Corruptos (dupla codificação) — **corrigir** | `de`, `fr`, `hu`, `sv` |
| Corruptos parciais (cp1252) — **corrigir** | `pt`, `pt-br` |
| Obsoleto — **remover** | `pt-br.fixed` |
| Legítimos (verificados byte-level) | `ro`, `tr`, `da`, `cs`, `sk`, `no` e demais |
| Cobertura | 38 packs com 625/625 chaves (0 faltantes); `pt-br.fixed` 472/625 (153 faltando) |
| Completude (medidor) | todos os idiomas ≥ 96% |

---

## 6. Plano de correção (ordem recomendada)

1. **Encoding (C1+C2):** generalizar `scripts/fix_mojibake_translations.py` para
   `de/fr/hu/sv/pt/pt-br` (backup antes; conferir resultado com a varredura de
   marcadores `Ã`→0 e um `git diff` dirigido); corrigir também `en.txt:368`.
   → commit 1.
2. **A1:** excluir `Translations/pt-br.fixed.txt`; decidir `kr.txt`/`zh.txt` (M3).
   → commit 2.
3. **A2:** limpar as 7 chaves `COMMUTER_DESTINATION_*` dos 38 packs (ou religar o
   painel). → commit 3.
4. **M6:** fallback automático para `en.txt` quando a chave faltar.
5. **M4:** corrigir `PrefixKey` do `ChangelogCollection` para casar
   `CHANGELOG_4_8_8_1`.
6. **M5/B1/B4/B2/B3:** limpeza de chaves mortas, checkboxes futuras, BOM, TODOs —
   conforme prioridade de release.

---

## 7. Trabalho já verificado (não requer ação)

- 4 chaves novas da 4.8.8 presentes em todos os packs:
  `SETTINGS_HOTKEY_LINE_COLOR_TOOLTIP` (`en.txt:460`),
  `SETTINGS_INTERCITY_BUS_ENABLE` (`en.txt:416`),
  `SETTINGS_FLIGHTTRACKER_ENABLE` (`en.txt:441`),
  `SETTINGS_TRAINDISPLAY_TYPES_GROUP_DESCRIPTION` (`en.txt:588`).
- Changelog 4.8.0→4.8.8 configurado em `IptModManager.cs:198–319` com
  `autoGenerate:false`, última entrada 4.8.8 datada 2026-08-03; o dialog expande a
  versão mais nova primeiro (`ChangelogDialog.Init(maximizeFirst:true)`);
  `WhatsNewLastSeenVersion` em `ModSetting.cs:235`; notificação em
  `ImprovedPublicTransportMod.cs:593`.
- O fluxo do changelog usa `L("CHANGELOG_...")` (localiza na hora de construir o
  dialog) e renderiza o texto direto (`LocalizedDescription=false` → branch sem
  `LocalizeFormat` em `ChangelogDialog.cs:214`) — **não** há dupla localização no
  caminho atual. A única ligação com encoding é o texto dos packs (C1/C2).
- `AlgernonCommons/Translations/en-EN.csv` existe e alimenta os diálogos do
  framework; só inglês.
- Sem stub "not implemented" para `AUTOLINECOLOR_*` (chave é montada dinamicamente).

---

## 8. Arquivos de referência

- `Translations/en.txt` — pack canônico 625 chaves; `:368` mojibake; `:36–42`
  órfãs `COMMUTER_DESTINATION_*`; `:612–617` `COMMUTERDESTINATION_PANEL_*`.
- `Translations/{de,fr,hu,sv,pt,pt-br}.txt` — corrupção de encoding (C1/C2).
- `Translations/pt-br.fixed.txt` — obsoleto (A1).
- `scripts/fix_mojibake_translations.py` — reparo cp1252/latin-1→UTF-8 (alvo fixo
  hoje: `pt-br.txt`, `pt.txt`; linha 76).
- `scripts/_audit_fill_translations.py` — parse/auditoria de `Translations/*.txt`.
- `TranslationFramework/LocalizationManager.cs:283–311` — chave ausente retorna a
  própria chave (M6); `:31` dedup de log.
- `Localization.cs` — entrada; `typeof(Mod)` obrigatório; `:132` fallback último
  recurso.
- `LanguageFormat/PlainTextLanguageDeserializer.cs:15` — `LocaleAliases`
  (`es-419`, `zh-cn`, `ko`, `kr`, `zh`).
- `Util/TranslationCompleteness.cs` — medidor "idêntico ao en.txt" = não traduzido.
- `CSLModsCommonShared/ChangelogCollection.cs:26–28,54–84` — bug latente do
  `autoGenerate` (M4).
- `CSLModsCommonShared/UI/Dialogs/ChangelogDialog.cs:105,214` — geração + render.
- `IptModManager.cs:198–319` — coleções de changelog (todas `autoGenerate:false`).
- `UI/CSLModsCommonOptionsPanel.cs:85–105,255–263,314,526` — abas e spoilers.
- `UI/PanelExtenders/PanelExtenderCityService.cs:20`,
  `UI/PanelExtenders/PanelExtenderLine.cs:933,960`,
  `UI/PreviewRenderer/PreviewRenderer.cs:213` — TODOs hardcoded (B2/B3).
- `CSLModsCommonShared/Manager/LocalizationManager.cs` — loader CSV do framework
  (`Localize` 174, `LocalizeFormat` 199).
