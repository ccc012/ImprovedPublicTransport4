# Como enviar descrições rápido para o Steam Workshop — Cities: Skylines (CS1)

Pesquisa em 03/08/2026. Contexto: repo do IPT4 tem ~29 descrições localizadas em `Projeto-Steam\workshop-description-<lang>.txt`. Pergunta: qual o jeito mais rápido de colocar/atualizar essas descrições na página do Steam Workshop?

**Veredito em uma linha:** o jogo **NÃO lê** os arquivos `workshop-description-<lang>.txt` — eles são só a fonte de verdade local. O caminho mais rápido para os ~29 idiomas é um **script via API Steamworks (SteamUGC)**, que atualiza todas as descrições localizadas em uma execução com a conta Steam logada. Alternativa sem código: site/Steam Client (1 idioma por vez, manual) ou SteamCMD (só inglês).

---

## 1. Como o CS1 trata descrições (o que confirmamos)

- **O Content Manager do CS1 envia UMA única descrição** (a que está no diálogo ao publicar/atualizar; padrão = nome do item). Não há varredura de arquivos de descrição nem seletor de idioma — confirmado na [wiki oficial Content Manager](https://skylines.paradoxwikis.com/Content_Manager) e na [User path](https://skylines.paradoxwikis.com/User_path) (staging = `snapshot.png` + `Content\`).
- **Mods populares não usam arquivos:** TM:PE tem a descrição hardcoded (`TrafficManagerMod.cs`); o IPT2/IPT4 só tem tradução de UI em `Locale/`, sem arquivos de descrição.
- A convenção de **arquivos por idioma que o jogo lê** existe em outros jogos (Terraria/tModLoader `description_workshop_<culture>.txt`, EU5 `workshop_<lang>.txt`), **mas não no CS1**.
- Logo, os seus `workshop-description-<lang>.txt` são **sua convenção local** — para levá-los ao Workshop, precisa empurrar via API Steamworks.

---

## 2. Métodos comparados (caso: ~29 descrições localizadas)

| Método | Re-sobe conteúdo? | Descrição localizada (29) | Velocidade | Confiabilidade |
|---|---|---|---|---|
| **Site manual** (Owner Controls, aba por idioma) | Não | Sim, 1 por vez | Muito lenta (29×) | Alta, mas erro humano |
| **Steam Client** ("Edit item details") | Não | Sim, 1 por vez (mesmo formulário) | Lenta (29×) | Igual ao site |
| **In-game Update** (Content Manager) | **Sim** | **Não** (só a descrição default/en) | Rápida p/ conteúdo | Alta p/ conteúdo; não resolve idiomas |
| **SteamCMD** (+`workshop_build_item` VDF) | Sim (opcional) | **Não** (VDF sem campo de idioma) | Média | Média (sessão expira) |
| **Script SteamUGC/Steamworks API** | **Não** (só metadados) | **Sim, todas de uma vez** | **Muito rápida (1 execução)** | Alta (API oficial, sessão do Steam logado) |

**Conclusão:** script via Steamworks API para as descrições; in-game Update apenas quando o conteúdo/DLL mudar (mantém a descrição em inglês). Evitar o site para 29 idiomas.

---

## 3. Método recomendado: script Steamworks (SteamUGC)

### Requisitos
- Conta Steam **logada no client** e que seja **dona do item** do Workshop.
- A conta precisa **possuir o CS1 na biblioteca** (licença do appid 255710). A inicialização `SteamAPI_Init()` com o appid 255710 exige licença — **não** é preciso ser partner/desenvolvedor (Steamworks.NET issue #83; erro clássico "does not own a license for the provided App ID" é só de licença).
- Ter aceito o **Workshop Legal Agreement** (https://steamcommunity.com/workshop/workshoplegalagreement/).
- Arquivo `steam_appid.txt` com `255710` no diretório de trabalho do script.

### Mecanismo (um `SubmitItemUpdate` por idioma)
```
StartItemUpdate(255710, <publishedfileid>)
  → SetItemUpdateLanguage("<steam_lang>")
  → SetItemTitle(título)          # opcional
  → SetItemDescription(texto)
  → SubmitItemUpdate(handle, changeNote)
```
Documentação oficial: [ISteamUGC](https://partner.steamgames.com/doc/api/ISteamUGC) (SetItemDescription máx. 8000 bytes; título máx. 128 bytes). Um submit = um idioma; para 29 idiomas são 29 `SubmitItemUpdate` na mesma execução.

### Referência pronta (modelo completo)
- **EU5 community-mod-framework — `tools/upload.py`** (https://github.com/Europa-Universalis-5-Modding-Co-op/community-mod-framework/blob/main/tools/upload.py): binding Python `steamworks`, loop `StartItemUpdate → SetItemUpdateLanguage → SetItemTitle/SetItemDescription → SubmitItemUpdate`, regex `^workshop_(.+)\.txt$`, dicionário `LANGUAGE_TO_STEAM`, validação de 8000/128 bytes. **Adaptar para appid `255710`, seu `publishedfileid` e seus nomes de arquivo.**
- **tModLoader PR #5226** (https://github.com/tModLoader/tModLoader/pull/5226): implementação de referência do fluxo "arquivo por idioma → update por idioma".

### Mapeamento dos seus arquivos → códigos Steam
Seus nomes `workshop-description-<lang>.txt` já batem com os códigos Steam na maioria (`english`, `german`, `brazilian`, `french`, `spanish`, `latam`, `italian`, `dutch`, `portuguese`, `danish`, `norwegian`, `swedish`, `finnish`, `hungarian`, `romanian`, `turkish`, `greek`, `polish`, `russian`, `indonesian`, `malay`, `japanese`, `koreana`, `schinese`, `tchinese`, `vietnamese`). Lista oficial dos 29: https://partner.steamgames.com/doc/store/localization/languages. Encoding dos arquivos: **UTF-8 sem BOM**.

> ⚠️ **Caveat dos wrappers Python/Node:** `SteamworksPy` e `steamworks.js` **não** expõem `SetItemUpdateLanguage` (só o texto default/en). Para multi-idioma usar o SDK cru, o binding `steamworks` do upload.py do EU5 (neste repo já há o wrapper `SteamWorkshop.SetItemUpdateLanguage` em `interfaces/workshop.py`), ou as ferramentas abaixo.

### 3.1 Implementado e validado na prática (03/08 e 04/08/2026)

O uploader foi construído e executado com sucesso em 03/08/2026 (só descrições) e **04/08/2026 (título + descrição por idioma)**: **30/30 idiomas enviados, 0 falhas**. O envio grava **sempre título + descrição juntos** porque o Steam apaga o título localizado quando só a descrição é submetida — descoberto na conferência ao vivo em 03/08 (19 idiomas ficaram com título em branco). Conteúdo, preview e tags **não** são tocados.

**Localização:** `Projeto-Steam\uploader\`

| Arquivo | Papel |
|---|---|
| `upload_desc.py` | Script principal: lê `../workshop-description-<lang>.txt`, trata `en` como alias de `english`, e faz `StartItemUpdate → SetItemDescription → SetItemUpdateLanguage → SetItemTitle → SubmitItemUpdate` por idioma (título fixo `Improved Public Transport 4 (IPT4)`). Suporta `--dry-run`, `--lang <langs>`, `--yes`, e validação do limite de 8000 bytes. |
| `run_elevated.py` | Runner com log em `upload_run.log`; foi o usado no envio real (via UAC). Tem `try/except` por idioma para o log não morrer em silêncio. |
| `steamworks\` | Binding Python `ctypes` do Steamworks (MIT, extraído do community-mod-framework do EU5), 17 arquivos `.py`. |
| `SteamworksPy64.dll`, `steam_api64.dll`, `steam_appid.txt` | DLLs 64-bit + `steam_appid.txt` com o appid `255710`. |

**Como rodar:**
```
python upload_desc.py --dry-run                        # só planejar
python upload_desc.py --lang german,brazilian --yes    # idiomas específicos
python upload_desc.py --yes                            # todos
```

**⚠️ Elevação (UAC) obrigatória:** o shell/terminal comum roda com integridade **Low** no Windows enquanto o client Steam roda elevado (via `svchost`) → `SteamAPI_Init()` falha com "Steam is not running". Executar o runner via `Start-Process -Verb RunAs` (UAC) resolve; o log de execução fica em `upload_run.log` (que o `.gitignore` do repo ignora).

**Resultado da execução real (04/08/2026 ~05:55):** `INIT OK`, `logged_on: True`, persona `ccc02`, `steamid 76561198434832331`, `owns_app_255710: True`; 30 uploads (título + descrição), `ok=30 failed=0`. Conferido ao vivo: a página `https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930&l=<lang>` exibe o título `Improved Public Transport 4 (IPT4)` em **todos os 30 idiomas** e a descrição 4.8.8 em produção.

> 🐛 **Bug corrigido no caminho:** a primeira tentativa do envio título+descrição (04/08 ~05:48) morreu em silêncio com `AttributeError: 'SteamWorkshop' object has no attribute 'SetItemUpdateLanguage'` — o binding tinha a função C (`Workshop_SetItemUpdateLanguage`) mas faltava o wrapper Python na classe `SteamWorkshop`. Adicionado `SetItemUpdateLanguage` em `interfaces/workshop.py` + `try/except` por idioma no `run_elevated.py`; re-envio com `ok=30 failed=0`.

---

## 4. Ferramentas de terceiros (prontas)

| Ferramenta | Link | Linguagem | Observação |
|---|---|---|---|
| **steam-workshop-uploader (Darkborderman)** | https://github.com/DarkbordermanModding/steam-workshop-uploader | C++ | **Suporta descrições por idioma via YAML** (`localizations: {english: {...}, schinese: {...}}`) — mais próximo do caso de 29 idiomas |
| **Steam-Uploader** | https://github.com/SimKDT/Steam-Uploader | C++ (CLI) | `--appID --workshopID --description --preview --title --tags`; 1 idioma por execução |
| **Steam-Uploader-Menu** | https://github.com/xberkth/Steam-Uploader-Menu | Python | Wrapper de menu do Steam-Uploader |
| **steam-workshop-uploader (gibbo101)** | https://github.com/gibbo101/steam-workshop-uploader | C#/.NET | CLI; 1 descrição por update |
| **steamworks.js** | https://github.com/ceifa/steamworks.js | Node | `updateItem(id, {title, description, ...})`; sem por-idioma |
| **SteamworksPy** | https://github.com/philippj/SteamworksPy | Python (ctypes) | `StartItemUpdate/SetItemDescription/SubmitItemUpdate`; sem `SetItemUpdateLanguage` |
| **pdx-workshop-manager** | https://github.com/kaiser-chris/pdx-workshop-manager | Go | per-language; para jogos Paradox |
| **SWDD** (userscript) | https://github.com/criskkky/SWDD | JS | **Direção contrária**: baixa descrições publicadas (.bbcode/.md) — útil para backup/comparação |
| ❌ ValvePython/`steam` | https://github.com/ValvePython/steam | Python | **NÃO serve**: não faz upload/UGC |

---

## 5. Alternativa sem código: SteamCMD (só inglês)

Caminho oficial scriptável para **atualizar descrição + changenote** de um item já publicado. **Não suporta múltiplos idiomas** (VDF não tem campo de idioma) — serve para a descrição default/en e conteúdo.

1. Baixar SteamCMD (https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip).
2. Login + aceitar o Workshop Legal Agreement: `steamcmd.exe +login <usuario> <senha> +quit`.
3. `item.vdf` (persistente; o SteamCMD reescreve o `publishedfileid` nele):
```vdf
"workshopitem"
{
  "appid" "255710"
  "publishedfileid" "SEU_ID"
  "contentfolder" "C:\caminho\conteudo"
  "previewfile" "C:\caminho\preview.jpg"
  "visibility" "0"
  "title" "Improved Public Transport 4"
  "description" "Nova descrição..."
  "changenote" "Nota da atualização"
  "tags" { "0" "Mod" }
}
```
4. Upload: `steamcmd.exe +login <usuario> <senha> +workshop_build_item item.vdf +quit`.

Referência de CI pronta (CS1): **BuildingThemes2** — `.github/workflows/workshop-update-description.yml` (https://github.com/theLittleStone/BuildingThemes2) — gera VDF só-descrição com `Workshop/generate_description_vdf.py` e reusa a sessão via secrets `STEAM_CONFIG_VDF`/`STEAM_USERNAME`. ⚠️ O `config.vdf` do Steam **expira** — renovar com `steamcmd +login <usuario> <senha> +quit`. Steam Guard: usar `+set_steam_guard_code` ou TOTP/configVdf (a Action `m00nl1ght-dev/steam-workshop-deploy` resolve).

**Limitações CS1+SteamCMD:** Valve diz que SteamCMD no Workshop é "somente para testes"; não há confirmação oficial para mods CS1 (255710); já houve relato de "Error submitting workshop item: Fail" com conta compartilhada/família — usar a conta dona do jogo. Se falhar, cair para o Content Manager in-game.

---

## 6. Fluxo oficial in-game (para quando o CONTEÚDO mudar)

- Mod local fica em `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\<Nome>\` (é de lá que o jogo sobe). `Steam\steamapps\workshop\content\255710\<id>\` é a cópia local de itens subscritos.
- **Publicar novo:** Content Manager → Mods (mod ativado) → **Share** → título/descrição/tags/change note → Share. (Guia: https://city-skylines-modding.github.io/docs/guides/gui-008/)
- **Atualizar:** é **obrigatório estar inscrito no próprio item** para o botão virar **Update** ([wiki](https://skylines.paradoxwikis.com/Content_Manager)); alterar conteúdo em `Addons\Mods\<Nome>`; Content Manager → Mods → **Update** → mesmo diálogo. Re-sobe conteúdo + uma descrição.

---

## 7. Avisos importantes

1. **Idioma adicionado não pode ser removido** — só adicionar/sobrescrever (fonte: [SCS](https://forum.scssoft.com/viewtopic.php?t=204527), [Reddit SteamWorkshop](https://www.reddit.com/r/SteamWorkshop/comments/9ef9ve/)). **Decidir o conjunto final de idiomas antes de rodar o script.**
2. Limites: descrição **8000 bytes**, título **128 bytes** (UTF-8).
3. Cada `SubmitItemUpdate` conta como um update no histórico do item (29 idiomas = 29 entradas de update — normal).
4. Link direto do editor de descrições: `https://steamcommunity.com/sharedfiles/filedetails/edit/?id=<publishedfileid>`.
5. Web API **bloqueada** para modder comunitário: métodos de escrita (`IPublishedFileService`) exigem **publisher key** ligada ao grupo publisher do appid 255710 (só Paradox/CO emite). O caminho é sempre o SDK/SteamCMD, nunca a Web API.

---

## 8. Fontes

- https://skylines.paradoxwikis.com/Content_Manager · https://skylines.paradoxwikis.com/User_path · https://skylines.paradoxwikis.com/Modding
- https://city-skylines-modding.github.io/docs/guides/gui-008/ (fluxo de publicação in-game)
- https://partner.steamgames.com/doc/api/ISteamUGC (SetItemDescription, SetItemUpdateLanguage, StartItemUpdate, SubmitItemUpdate)
- https://partner.steamgames.com/doc/features/workshop/implementation (spec do VDF do SteamCMD)
- https://partner.steamgames.com/doc/webapi/ISteamRemoteStorage · https://partner.steamgames.com/doc/webapi/IPublishedFileService (por que Web API não serve)
- https://developer.valvesoftware.com/wiki/SteamCMD
- https://github.com/Europa-Universalis-5-Modding-Co-op/community-mod-framework/blob/main/tools/upload.py (script-modelo multi-idioma)
- https://github.com/tModLoader/tModLoader/pull/5226 (mecanismo por-idioma — Terraria)
- https://github.com/theLittleStone/BuildingThemes2 (CI SteamCMD só-descrição, CS1)
- https://github.com/SimKDT/Steam-Uploader · https://github.com/DarkbordermanModding/steam-workshop-uploader · https://github.com/philippj/SteamworksPy · https://github.com/ceifa/steamworks.js
- https://partner.steamgames.com/doc/store/localization/languages (29 idiomas e códigos)
- https://forum.scssoft.com/viewtopic.php?t=204527 · https://www.reddit.com/r/SteamWorkshop/comments/9ef9ve/ (idiomas não removíveis, UTF-8)
- https://github.com/m00nl1ght-dev/steam-workshop-deploy (Action de deploy com TOTP/configVdf)
