# Workshop item metadata — English (canonical)

Item: https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930
App: 255710 (Cities: Skylines)

English is the canonical language for everything player-facing on Steam.
Translations, if added later, go in the Workshop's own translation fields —
the English text here stays the source.

---

## Title

```
Improved Public Transport 4
```

Do not append a version number to the title. The version belongs in the
description and in the changenote, so the title stays stable across updates.

---

## Short description (first paragraph of the description, shown in previews)

```
One mod instead of fifteen. Full control over every transit line — fleet size,
vehicle types, stops, fares, unbunching — with fifteen small public-transport
mods absorbed so they stop conflicting with each other.
```

Steam truncates the preview at roughly 240 characters, so the first two lines
have to carry the whole pitch on their own.

---

## Tags

Cities: Skylines only accepts tags from a fixed list. Applicable ones:

- `Mod` — required, this is the category
- `Transport` — the subject matter

Everything else on the list (Building, Park, Vehicle, Road, Prop, Map,
Scenario, Savegame, Style, Tree, Intersection, Canal, Quay, Pedestrian Path,
Track, Wall, Pipes and Cables, Terraforming, Hydro, Wind, Fossil Fuel, Nuclear,
Solar, Cemetery, Landfill, Fire, Police, Healthcare, Education, Transport,
Unique, Monument) does not apply, and adding tags that do not fit hurts
discoverability rather than helping it.

---

## Recommended discussion topics

Steam does not create these automatically. Pin them so bug reports arrive with
usable information instead of "doesn't work".

1. **Read first: reporting a problem** — asks for (a) what you were doing when
   it happened, (b) the `output_log.txt` path, (c) the full mod list. Explain
   that without the log there is nothing to diagnose.
2. **Known incompatibilities** — IPT2/IPT3, Transport Lines Manager, standalone
   AutoLineBudget, and any standalone copy of an absorbed mod. Keeping this in
   one pinned place saves repeating it in every thread.
3. **Translation corrections** — Czech, Dutch, Slovak, Thai and Turkish still
   fall back to English; the other 18 may have awkward phrasing. Point to
   `Translations/` on GitHub and explain that a key uses a literal `\n` for
   line breaks.
4. **Feature requests** — set the expectation up front: this is a personal fork
   maintained for one player's setup, so requests are read but not promised.

---

## Changenote conventions

Two audiences, two registers, same release:

- **Workshop changenote** — what changed from the player's point of view, and
  what they should do differently because of it. Symptom first, cause second,
  in plain language. No class names unless they help the player search a log.
- **`CHANGELOG.md`** in the repository — root cause, affected class or field,
  and why the failure was not obvious. Written for whoever maintains this next.

The Workshop changenote must be a single line inside the `.vdf`: Valve's
KeyValues parser rejects real newlines in a value ("key name too long"). Use
literal `\n` two-character sequences. [`write_vdf.py`](write_vdf.py) in this
directory generates the file correctly and verifies that no real newline made it
into the value.

---

## Upload

```
C:\steamcmd\steamcmd.exe +login ccc0220 +workshop_build_item C:\steamcmd\atualizar_ipt4.vdf +quit
```

The `.vdf` points `contentfolder` at the deployed mod directory, so **build
first** — the build auto-deploys, and the upload ships whatever is currently
in that folder. Uploading without rebuilding ships the previous binary with the
new changenote, which is worse than not updating at all.

`visibility` in the `.vdf`: `0` public, `1` friends-only, `2` private.
