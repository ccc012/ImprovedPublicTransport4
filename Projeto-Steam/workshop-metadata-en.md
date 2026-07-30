# Workshop item metadata — English (canonical)

Item: https://steamcommunity.com/sharedfiles/filedetails/?id=3773802930
App: 255710 (Cities: Skylines)

English is the canonical language for everything player-facing on Steam.
Translations, if added later, go in the Workshop's own translation fields —
the English text here stays the source.

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

**The item already carries `Mod`, and that is the only correct tag. Do not add
more, and do not set the `tags` key in the `.vdf` at all** - setting it replaces
the existing tag list rather than adding to it, so a typo or an invalid name can
silently strip `Mod` and drop the item out of every mod listing.

Cities: Skylines splits its tag list into CATEGORIES (Map, Mod, SaveGame,
District Style, Map Theme, Scenario) and ASSETS. `Transport` and its variants
(`Transport Bus`, `Transport Metro`, `Transport Train`, ...) are **ASSET** tags:
they mean "this item *is* a transport building or vehicle", not "this item is
about transport". Tagging a code mod with them puts it in listings where people
are browsing for props and buildings - actively worse for discovery than leaving
it alone.

## Description

`workshop-description-en.txt` is the source of truth, but **publishing it is
manual**: paste it into the Workshop editor in the browser.

**Do not put a `description` key in the `.vdf`.** Tested against item 3773802930
on 2026-07-29: with the key, the upload transfers the content and then fails at
`Committing update...ERROR! Failed to update workshop item (Invalid Parameter)`.
Remove the key and the identical `.vdf` commits `Success`. It is not a length
problem - it failed at 10,723 characters and failed the same way at 7,968; a
bisection isolated the key itself.

Keep the description under **8,000 characters** anyway; that is the Workshop
editor's own limit.

**No straight double quotes in the file.** The `.vdf` parser does not process
escape sequences, so an unescaped `"` terminates the value and everything after it
is read as a key name - that is the real source of the
`KeyValues Error: key name too long (1235 chars)` from the first upload attempt,
not the line breaks, which is what it looked like at the time. Use typographic
quotes. `write_vdf.py` asserts this even though the description no longer goes
into the `.vdf`, so a future change cannot reintroduce it quietly.

## Title

The item is titled:

```
Improved Public Transport 4 (IPT4)
```

**Omit the `title` key from the `.vdf`** unless the title is deliberately being
changed - the Workshop keeps the existing title when the key is absent, and
passing it is an easy way to overwrite the title by accident during an otherwise
routine upload.

Never put a version number in the title. It belongs in the description and the
changenote, so the title stays stable across updates and people can find the item
by the same name every time.

If it is ever renamed, `Improved Public Transport 4` (spaced, no suffix) is closer
to how people search - but that is a deliberate decision to make once, not
something to change casually.

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

- **Workshop changenote** - what changed from the player's point of view, and what
  they should do differently because of it. Symptom first, cause second, in plain
  language. No class names unless they help someone search a log.
- **`CHANGELOG.md`** in the repository - root cause, affected class or field, and
  why the failure was not obvious. Written for whoever maintains this next.

### Line breaks: use real ones

Put **real newlines** inside the quoted value. Verified on the page after the
4.3.8 upload: 27 real line breaks, no literal `\n` anywhere.

The 4.3.5 note was written as a single line with literal `\n` separators on the
assumption that real newlines break the parser. They do not - and because the
parser does not process escapes, that note is still sitting on the page with
`\n` printed as visible text in the middle of its sentences. It reads as broken,
because it is. Left as-is rather than rewritten, since a change note is a record
of what was said at the time.

## Preview image

**Never point `previewfile` at a path inside the deployed mod folder.** That folder
is generated by the build (`DeployToModDirectory`), so anything placed there by
hand disappears on the next build - which is exactly how the cover image was lost
before the 4.3.8 upload, producing
`Failed to update workshop item (File Not Found)` and no update at all.

If the file is missing, `write_vdf.py` **omits the key**, and the Workshop keeps
the image already on the page. To replace the cover, put a `.jpg` (under 2 MB) at
`Projeto-Steam/ipt4-workshop-cover.jpg`.

Keeping it out of the mod folder is also right for players: everything in
`contentfolder` ships in their download, and a 2 MB cover image does nothing in
the game.

## Upload

```
C:\steamcmd\steamcmd.exe +login ccc0220 +workshop_build_item C:\steamcmd\atualizar_ipt4.vdf +quit
```

The `.vdf` points `contentfolder` at the deployed mod directory, so **build
first** — the build auto-deploys, and the upload ships whatever is currently
in that folder. Uploading without rebuilding ships the previous binary with the
new changenote, which is worse than not updating at all.

`visibility` in the `.vdf`: `0` public, `1` friends-only, `2` private,
`3` unlisted. Note that `2` is **private**, not "hidden but reachable" - if you
want a link-only build for testing, `3` is the one you want; `2` makes the item
invisible even to someone holding the URL.
