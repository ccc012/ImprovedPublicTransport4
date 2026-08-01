# -*- coding: utf-8 -*-
"""Revert false-positive Task B keys; re-audit real completeness."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TRANS = ROOT / "Translations"
REPORT = ROOT / "scripts" / "_translation_gap_report.txt"

# Keys wrongly added by naive Localization.Get scanner (prefixes / game / shared UI).
FALSE_POSITIVE_KEYS = {
    "AUTOLINECOLOR_NAMING_",
    "SETTINGS_EBS_MODE_",
    "SETTINGS_EBS_TRAM_MODE_",
    "SETTINGS_TRAINDISPLAY_POS_",
    "SETTINGS_TRAINDISPLAY_THEME_",
    "INFO_PUBLICTRANSPORT_BUS",
    "INFO_PUBLICTRANSPORT_METRO",
    "INFO_PUBLICTRANSPORT_MONORAIL",
    "INFO_PUBLICTRANSPORT_TRAIN",
    "INFO_PUBLICTRANSPORT_TRAM",
    "INFO_PUBLICTRANSPORT_TROLLEYBUS",
    "CONFLICTING_MODS",
    "CONFLICT_DETECTED",
    "HARMONY_ERROR",
    "HARMONY_MOD_CONFLICT",
    "HARMONY_MOD_ERROR",
    "HARMONY_NOT_INSTALLED",
    "HARMONY_PROBLEM_CAUSES",
    "NO",
    "YES",
    "NOTE_CLOSE",
    "NOTE_DONTSHOWAGAIN",
    "PRESS_ANY_KEY",
    "UNABLE_TO_OPERATE",
}

MARKER = "# Added from code Localization.Get (Task B)"


def is_valid_key(key: str) -> bool:
    if not key or len(key) < 2:
        return False
    return all(("A" <= c <= "Z") or ("0" <= c <= "9") or c == "_" for c in key)


def parse_lang_file(path: Path) -> dict[str, str]:
    d: dict[str, str] = {}
    last_key: str | None = None
    for line in path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
        if not line or not line.strip():
            continue
        if line.lstrip().startswith("#"):
            continue
        idx = line.find(" ")
        maybe = line[:idx] if idx > 0 else line.strip()
        if not is_valid_key(maybe):
            if last_key is not None and last_key in d:
                d[last_key] = d[last_key] + "\n" + line.rstrip()
            continue
        if idx <= 0:
            continue
        d[maybe] = line[idx + 1 :].replace("\\n", "\n")
        last_key = maybe
    return d


def strip_false_positives_from_file(path: Path) -> int:
    """Remove false-positive keys and the Task B marker block. Returns removals."""
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    lines = text.splitlines()
    out: list[str] = []
    removed = 0
    for line in lines:
        if line.strip() == MARKER:
            removed += 1
            continue
        if not line.strip() or line.lstrip().startswith("#"):
            # keep non-marker comments; drop trailing blank runs later
            out.append(line)
            continue
        idx = line.find(" ")
        maybe = line[:idx] if idx > 0 else line.strip()
        if is_valid_key(maybe) and maybe in FALSE_POSITIVE_KEYS:
            removed += 1
            continue
        out.append(line)
    # trim trailing empty lines to single newline
    while out and not out[-1].strip():
        out.pop()
    path.write_text("\n".join(out) + "\n", encoding="utf-8")
    return removed


def extract_code_keys_strict() -> tuple[set[str], set[str]]:
    """
    Returns (complete_literal_keys, prefix_keys_used_with_concat).
    Only matches Localization.Get("KEY") and L("KEY") as complete keys.
    Prefix form: Get("PREFIX" + ...)
    """
    complete: set[str] = set()
    prefixes: set[str] = set()
    lit = re.compile(r'(?:Localization\.Get|\bL)\(\s*"([A-Z0-9_]+)"\s*\)')
    pref = re.compile(r'(?:Localization\.Get|\bL)\(\s*"([A-Z0-9_]+)"\s*\+')
    skip = {"bin", "obj", ".git"}
    for p in ROOT.rglob("*.cs"):
        if any(part in skip for part in p.parts):
            continue
        try:
            t = p.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        for m in pref.finditer(t):
            prefixes.add(m.group(1))
        for m in lit.finditer(t):
            complete.add(m.group(1))
    # remove pure prefixes that only appear as concat
    complete -= prefixes
    return complete, prefixes


def find_junk(d: dict[str, str]) -> list[tuple[str, str]]:
    junk = []
    for k, v in d.items():
        if "# sync" in v or "# Auto-filled" in v:
            junk.append((k, v[:120].replace("\n", "\\n")))
        elif re.search(r"\n\s*#\s*(sync|Auto-filled)", v, re.I):
            junk.append((k, v[:120].replace("\n", "\\n")))
    return junk


def main() -> None:
    packs = sorted(
        p
        for p in TRANS.glob("*.txt")
        if not p.name.endswith(".fixed.txt")
    )

    total_removed = 0
    for p in packs:
        n = strip_false_positives_from_file(p)
        total_removed += n
        print(f"cleaned {p.name}: removed {n} lines")

    en = parse_lang_file(TRANS / "en.txt")
    en_keys = list(en.keys())
    en_set = set(en_keys)

    complete, prefixes = extract_code_keys_strict()
    code_missing = sorted(complete - en_set)

    lines: list[str] = []
    lines.append("IPT4 Translation Completeness Report (final)")
    lines.append("=" * 60)
    lines.append(f"Master: en.txt ({len(en)} keys)")
    lines.append(f"Removed false-positive Task B lines (all packs): {total_removed}")
    lines.append(f"CHANGELOG_4_8_5_* : {sum(1 for k in en if k.startswith('CHANGELOG_4_8_5_'))}")
    lines.append(f"SETTINGS_* : {sum(1 for k in en if k.startswith('SETTINGS_'))}")
    lines.append("")
    lines.append(f"Code concat prefixes (not keys): {sorted(prefixes)}")
    lines.append(f"Complete Localization/L keys in code: {len(complete)}")
    lines.append(f"Still missing from en: {code_missing or '(none)'}")
    lines.append("")

    # Optional Task B: only add real complete keys missing from en
    en_added = 0
    if code_missing:
        # These should be rare; use humanized English as last resort
        with (TRANS / "en.txt").open("a", encoding="utf-8") as f:
            f.write("\n# Added from complete Localization.Get literals (Task B)\n")
            for k in code_missing:
                val = k.replace("_", " ").title()
                en[k] = val
                f.write(f"{k} {val}\n")
                en_added += 1
        en = parse_lang_file(TRANS / "en.txt")
        en_keys = list(en.keys())
        en_set = set(en_keys)

    # Sync packs
    lang_packs = [p for p in packs if p.name != "en.txt"]
    total_added = 0
    packs_with_gaps_before = []
    before_detail = []

    for p in lang_packs:
        d = parse_lang_file(p)
        missing = [k for k in en_keys if k not in d]
        empty = [k for k in en_keys if k in d and not (d[k] or "").strip()]
        junk = find_junk(d)
        if missing or empty:
            packs_with_gaps_before.append(p.name)
        before_detail.append(
            f"{p.name}: total={len(d)} missing={len(missing)} empty={len(empty)} junk={len(junk)}"
        )
        if missing:
            raw = p.read_text(encoding="utf-8-sig", errors="replace")
            if raw and not raw.endswith("\n"):
                raw += "\n"
            block = []
            for k in missing:
                val = en[k].replace("\n", "\\n")
                # Portuguese prefer sibling if available
                if p.stem.lower() in ("pt", "pt-br"):
                    other = TRANS / ("pt-br.txt" if p.stem.lower() == "pt" else "pt.txt")
                    if other.exists():
                        od = parse_lang_file(other)
                        if (od.get(k) or "").strip():
                            val = od[k].replace("\n", "\\n")
                block.append(f"{k} {val}" if val else k)
            p.write_text(raw + "\n".join(block) + "\n", encoding="utf-8")
            total_added += len(missing)
        if empty:
            # rewrite empty values from en (rare)
            raw_lines = p.read_text(encoding="utf-8-sig", errors="replace").splitlines()
            new_lines = []
            empty_set = set(empty)
            for line in raw_lines:
                if not line.strip() or line.lstrip().startswith("#"):
                    new_lines.append(line)
                    continue
                idx = line.find(" ")
                maybe = line[:idx] if idx > 0 else line.strip()
                if is_valid_key(maybe) and maybe in empty_set:
                    val = en[maybe].replace("\n", "\\n")
                    new_lines.append(f"{maybe} {val}" if val else maybe)
                else:
                    new_lines.append(line)
            p.write_text("\n".join(new_lines) + "\n", encoding="utf-8")

    lines.append("--- Pack status (after cleanup) ---")
    lines.extend(before_detail)
    lines.append("")

    # Final verify
    remaining_missing = 0
    remaining_empty = 0
    remaining_junk = 0
    chg_ok = True
    settings_ok = True
    after_lines = []
    for p in lang_packs:
        d = parse_lang_file(p)
        missing = [k for k in en_keys if k not in d]
        empty = [k for k in en_keys if k in d and not (d[k] or "").strip()]
        junk = find_junk(d)
        remaining_missing += len(missing)
        remaining_empty += len(empty)
        remaining_junk += len(junk)
        for k in en_keys:
            if k.startswith("CHANGELOG_4_8_5_") and (k not in d or not d[k].strip()):
                chg_ok = False
            if k.startswith("SETTINGS_") and (k not in d or not d[k].strip()):
                settings_ok = False
        after_lines.append(
            f"{p.name}: total={len(d)} miss={len(missing)} empty={len(empty)} junk={len(junk)}"
        )

    lines.append("--- Final ---")
    lines.extend(after_lines)
    lines.append("")
    lines.append("Summary:")
    lines.append(f"  en key count: {len(en)}")
    lines.append(f"  packs with gaps before fill: {packs_with_gaps_before or '(none)'}")
    lines.append(f"  keys added to packs this run: {total_added}")
    lines.append(f"  real code keys added to en: {en_added}")
    lines.append(f"  remaining missing: {remaining_missing}")
    lines.append(f"  remaining empty: {remaining_empty}")
    lines.append(f"  remaining junk (# sync leaks): {remaining_junk}")
    lines.append(f"  CHANGELOG_4_8_5_* complete in all packs: {chg_ok}")
    lines.append(f"  SETTINGS_* complete in all packs: {settings_ok}")

    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    print(f"\nWrote {REPORT}")


if __name__ == "__main__":
    main()
