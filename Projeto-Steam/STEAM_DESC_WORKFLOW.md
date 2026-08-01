# Steam description — incremental workflow

**Never rewrite the full body** on a patch release. Only change what actually
changed. Paste into the Workshop web editor per language (description is
**not** shipped in the `.vdf` — see `workshop-metadata-en.md`).

## The 30 Steam languages (copy/paste set)

Files: `workshop-description-<steam_name>.txt`

| Steam name | Language |
|---|---|
| english | English |
| german | German |
| french | French |
| italian | Italian |
| koreana | Korean |
| spanish | Spanish (Spain) |
| schinese | Simplified Chinese |
| tchinese | Traditional Chinese |
| russian | Russian |
| thai | Thai |
| japanese | Japanese |
| portuguese | Portuguese (Portugal) |
| polish | Polish |
| danish | Danish |
| dutch | Dutch |
| finnish | Finnish |
| norwegian | Norwegian |
| swedish | Swedish |
| hungarian | Hungarian |
| czech | Czech |
| romanian | Romanian |
| turkish | Turkish |
| brazilian | Portuguese (Brazil) |
| bulgarian | Bulgarian |
| greek | Greek |
| ukrainian | Ukrainian |
| latam | Spanish (LatAm) |
| vietnamese | Vietnamese |
| indonesian | Indonesian |
| malay | Malay |

Also keep `workshop-description-en.txt` as an alias of **english** (same content).

## File layout (fragments)

| Path | Role |
|---|---|
| `fragments/VERSION.txt` | One line, e.g. `4.8.0` — **only this** for bugfix releases |
| `fragments/GAME_VERSION.txt` | One line, e.g. `1.21.1-f9` |
| `fragments/HEADER.template.bbcode` | Banner + title + pitch + version line (`{VERSION}`, `{GAME_VERSION}`) |
| `workshop-description-*.txt` | Full text: header + stable body |

## Header (always first)

```
[img]https://i.imgur.com/8BfNPgj.png[/img]
[h1]Improved Public Transport 4[/h1]
[b]One mod instead of fifteen.[/b] ...
[b]Version X.Y.Z[/b] · Built for game version 1.21.1-f9
```

English branding in the header is intentional (all 30 files share it).

## How to update for a release

### A) Bugfix / polish only (e.g. **4.9.x** — no new features)

1. Edit `fragments/VERSION.txt` → `4.9.0` (or build number).
2. Run:

```powershell
cd C:\Users\Lucas\source\repos\cs1_ipt4\Projeto-Steam
python apply_steam_header.py
```

3. That **only** refreshes the header/version on all 30 languages. Body untouched.
4. Paste each file into Steam Workshop language fields (or only english + brazilian if short on time).
5. Change note in `write_vdf.py` CHANGENOTE = short bullet list of fixes (not a feature dump).

### B) New features / new section

1. Edit **english** body once (section under `[h2]New in X.Y[/h2]` or similar).
2. Port that **one section** to other languages (or leave EN until translated).
3. Bump `VERSION.txt` and run `apply_steam_header.py`.
4. Do **not** regenerate entire translated bodies from scratch.

### C) Header image / pitch only

1. Edit `fragments/HEADER.template.bbcode`.
2. Run `apply_steam_header.py`.

## Rules (do not break Steam save)

- No `[list]` / `[*]`
- No straight `"` in text (use typographic quotes)
- Keep under ~8000 chars per language
- Description is **manual paste** — not in VDF

## 4.9 plan (prepared now)

4.9 = **bugs + optimisations only**. Expected description delta:

- Version line `4.8.0` → `4.9.0` via script
- Body sections (Why / What / Absorbed / …) **unchanged**
- Optional one sentence under Stability if needed — not a full rewrite
