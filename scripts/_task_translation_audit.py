# -*- coding: utf-8 -*-
"""One-shot audit + fill for translation completeness (Task A/B)."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TRANS = ROOT / "Translations"
REPORT = ROOT / "scripts" / "_translation_gap_report.txt"

SKIP_PACKS = {"en.txt", "pt-br.fixed.txt"}


def is_valid_key(key: str) -> bool:
    if not key or len(key) < 2:
        return False
    return all(("A" <= c <= "Z") or ("0" <= c <= "9") or c == "_" for c in key)


def parse_lang_file(path: Path) -> dict[str, str]:
    """Match PlainTextLanguageDeserializer: skip blanks/#, KEY first whitespace, multi-line continue."""
    d: dict[str, str] = {}
    last_key: str | None = None
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    for line in text.splitlines():
        if line is None:
            continue
        if len(line) == 0 or len(line.strip()) == 0:
            continue
        trimmed_start = line.lstrip()
        if trimmed_start.startswith("#"):
            continue
        idx = line.find(" ")
        maybe_key = line[:idx] if idx > 0 else line.strip()
        if not is_valid_key(maybe_key):
            if last_key is not None and last_key in d:
                d[last_key] = d[last_key] + "\n" + line.rstrip()
            continue
        if idx <= 0:
            continue
        key = maybe_key
        value = line[idx + 1 :].replace("\\n", "\n")
        d[key] = value
        last_key = key
    return d


def parse_order(path: Path) -> list[str]:
    """Key order as first-seen valid keys in file (like en master)."""
    order: list[str] = []
    seen: set[str] = set()
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    for line in text.splitlines():
        if not line or not line.strip() or line.lstrip().startswith("#"):
            continue
        idx = line.find(" ")
        maybe = line[:idx] if idx > 0 else line.strip()
        if not is_valid_key(maybe) or idx <= 0:
            continue
        if maybe not in seen:
            seen.add(maybe)
            order.append(maybe)
    return order


def extract_code_keys() -> set[str]:
    keys: set[str] = set()
    pats = [
        re.compile(r'Localization\.Get\(\s*"([A-Z0-9_]+)"'),
        re.compile(r'ImprovedPublicTransport\.Localization\.Get\(\s*"([A-Z0-9_]+)"'),
        re.compile(r'\bL\(\s*"([A-Z0-9_]+)"'),
        re.compile(r'\.Translate\(\s*"([A-Z0-9_]+)"'),
    ]
    skip_parts = {"bin", "obj", ".git"}
    for p in ROOT.rglob("*.cs"):
        if any(part in skip_parts for part in p.parts):
            continue
        try:
            t = p.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        for pat in pats:
            for m in pat.finditer(t):
                keys.add(m.group(1))
    return keys


def find_junk(d: dict[str, str]) -> list[tuple[str, str]]:
    """Leaked multi-line junk like '# sync' inside values (not #{0} format tokens)."""
    junk: list[tuple[str, str]] = []
    for k, v in d.items():
        if "# sync" in v or "# Auto-filled" in v:
            junk.append((k, v[:160].replace("\n", "\\n")))
            continue
        # real newline then a comment-like line
        if re.search(r"\n\s*#\s*(sync|Auto-filled|TODO)", v, re.I):
            junk.append((k, v[:160].replace("\n", "\\n")))
    return junk


def rewrite_pack(
    path: Path,
    en: dict[str, str],
    en_order: list[str],
    existing: dict[str, str],
    fallback_from: dict[str, str] | None,
) -> tuple[int, int]:
    """
    Rebuild pack preserving existing translations; fill missing/empty from
    fallback_from then en. Returns (keys_added, empty_filled).
    """
    added = 0
    empty_filled = 0
    out_lines: list[str] = []
    # Preserve original file lines where key already present with non-empty value.
    # Simpler: rebuild in en order with best value.
    for key in en_order:
        val = existing.get(key, "")
        if not (val or "").strip():
            if fallback_from and (fallback_from.get(key) or "").strip():
                val = fallback_from[key]
            else:
                val = en[key]
            if key not in existing:
                added += 1
            else:
                empty_filled += 1
        # Write with \n escaped like other packs typically do for multi-line
        written = val.replace("\n", "\\n")
        out_lines.append(f"{key} {written}" if written else key)

    # Keep extra keys not in en (append) so we don't drop custom leftovers
    for key, val in existing.items():
        if key not in en:
            written = (val or "").replace("\n", "\\n")
            out_lines.append(f"{key} {written}" if written else key)

    path.write_text("\n".join(out_lines) + "\n", encoding="utf-8")
    return added, empty_filled


def main() -> None:
    en_path = TRANS / "en.txt"
    en = parse_lang_file(en_path)
    en_order = parse_order(en_path)
    assert len(en) == len(en_order), (len(en), len(en_order))

    packs = sorted(
        p
        for p in TRANS.glob("*.txt")
        if p.name not in SKIP_PACKS and not p.name.endswith(".fixed.txt")
    )

    # Portuguese preferred fallbacks
    pt_br = parse_lang_file(TRANS / "pt-br.txt") if (TRANS / "pt-br.txt").exists() else {}
    pt = parse_lang_file(TRANS / "pt.txt") if (TRANS / "pt.txt").exists() else {}

    lines: list[str] = []
    lines.append("IPT4 Translation Completeness Report")
    lines.append("=" * 60)
    lines.append(f"Master: en.txt ({len(en)} keys)")
    lines.append(f"Packs audited: {len(packs)}")
    lines.append(
        f"CHANGELOG_4_8_5_* in en: {sum(1 for k in en if k.startswith('CHANGELOG_4_8_5_'))}"
    )
    lines.append(f"SETTINGS_* in en: {sum(1 for k in en if k.startswith('SETTINGS_'))}")
    lines.append("")
    lines.append("--- BEFORE ---")

    before_stats: dict[str, dict] = {}
    total_missing_before = 0
    total_empty_before = 0
    packs_with_gaps: list[str] = []

    for p in packs:
        d = parse_lang_file(p)
        missing = [k for k in en_order if k not in d]
        empty = [k for k in en_order if k in d and not (d[k] or "").strip()]
        junk = find_junk(d)
        before_stats[p.name] = {
            "total": len(d),
            "missing": missing,
            "empty": empty,
            "junk": junk,
        }
        total_missing_before += len(missing)
        total_empty_before += len(empty)
        if missing or empty:
            packs_with_gaps.append(p.name)
        lines.append(
            f"{p.name}: total={len(d)} missing={len(missing)} empty={len(empty)} junk={len(junk)}"
        )
        if missing:
            for k in missing[:15]:
                lines.append(f"  - miss {k}")
            if len(missing) > 15:
                lines.append(f"  +{len(missing)-15} more missing")
        if empty:
            for k in empty[:15]:
                lines.append(f"  - empty {k}")
        if junk:
            for k, sample in junk[:5]:
                lines.append(f"  - junk {k}: {sample}")

    lines.append("")
    lines.append(f"Packs with gaps before: {packs_with_gaps or '(none)'}")
    lines.append(f"TOTAL missing before: {total_missing_before}")
    lines.append(f"TOTAL empty before: {total_empty_before}")
    lines.append("")

    # Task B: code keys missing from en
    code_keys = extract_code_keys()
    code_missing = sorted(code_keys - set(en))
    lines.append("--- CODE vs en.txt (Task B) ---")
    lines.append(f"Localization/L/Translate keys in code: {len(code_keys)}")
    lines.append(f"Missing from en.txt: {len(code_missing)}")
    for k in code_missing:
        lines.append(f"  - {k}")
    lines.append("")

    # Fill en with missing code keys (English placeholder = key itself is bad; use readable key)
    # Without known English text, use key as last resort only if we can find nothing.
    # Better: skip inventing English for unknown keys unless we find string literals nearby.
    # Task says: ADD them to en.txt and all packs (English text).
    en_added = 0
    if code_missing:
        # Append with key-as-label is ugly; prefer Title Case from key
        def humanize(k: str) -> str:
            return k.replace("_", " ").title()

        with en_path.open("a", encoding="utf-8") as f:
            f.write("\n# Added from code Localization.Get (Task B)\n")
            for k in code_missing:
                en[k] = humanize(k)
                en_order.append(k)
                f.write(f"{k} {en[k]}\n")
                en_added += 1
        lines.append(f"Appended {en_added} keys to en.txt")
        # re-parse to be safe
        en = parse_lang_file(en_path)
        en_order = parse_order(en_path)

    # Fill packs
    total_added = 0
    total_empty_filled = 0
    files_updated = 0
    for p in packs:
        d = parse_lang_file(p)
        missing = [k for k in en_order if k not in d]
        empty = [k for k in en_order if k in d and not (d[k] or "").strip()]
        junk = find_junk(d)

        # Clean junk by re-writing clean values for junked keys from en if contaminated
        cleaned = False
        for k, _ in junk:
            # strip leaked comment lines from value
            v = d.get(k, "")
            cleaned_lines = []
            for part in v.split("\n"):
                ts = part.lstrip()
                if ts.startswith("# sync") or ts.startswith("# Auto-filled"):
                    cleaned = True
                    continue
                cleaned_lines.append(part)
            d[k] = "\n".join(cleaned_lines).strip()
            if not d[k].strip():
                d[k] = en.get(k, "")
                cleaned = True

        need_write = bool(missing or empty or cleaned)
        if not need_write:
            continue

        stem = p.stem.lower()
        fallback = None
        if stem in ("pt", "pt-br"):
            # prefer the other Portuguese pack for missing, else en
            if stem == "pt":
                fallback = pt_br
            else:
                fallback = pt
        elif stem.startswith("pt"):
            fallback = pt_br or pt

        # Only rewrite if gaps; for junk-only, fix in place without full reorder if possible
        if missing or empty or cleaned:
            # Prefer minimal change: append missing only, fill empty in-place
            if cleaned or empty:
                # full rewrite to fix values safely
                added, ef = rewrite_pack(p, en, en_order, d, fallback)
                total_added += added
                total_empty_filled += ef
                files_updated += 1
            else:
                # append only missing keys
                raw = p.read_text(encoding="utf-8-sig", errors="replace")
                if raw and not raw.endswith("\n"):
                    raw += "\n"
                block = []
                for k in missing:
                    val = en[k]
                    if fallback and (fallback.get(k) or "").strip():
                        val = fallback[k]
                    written = val.replace("\n", "\\n")
                    block.append(f"{k} {written}" if written else k)
                p.write_text(raw + "\n".join(block) + "\n", encoding="utf-8")
                total_added += len(missing)
                files_updated += 1

    # AFTER
    lines.append("--- AFTER ---")
    total_missing_after = 0
    total_empty_after = 0
    remaining_junk = 0
    for p in packs:
        d = parse_lang_file(p)
        missing = [k for k in en_order if k not in d]
        empty = [k for k in en_order if k in d and not (d[k] or "").strip()]
        junk = find_junk(d)
        total_missing_after += len(missing)
        total_empty_after += len(empty)
        remaining_junk += len(junk)
        # ensure required keys
        chg_miss = [
            k
            for k in en_order
            if k.startswith("CHANGELOG_4_8_5_")
            and (k not in d or not (d[k] or "").strip())
        ]
        set_miss = [
            k
            for k in en_order
            if k.startswith("SETTINGS_") and (k not in d or not (d[k] or "").strip())
        ]
        flag = ""
        if missing or empty or junk or chg_miss or set_miss:
            flag = f" ISSUES miss={len(missing)} empty={len(empty)} junk={len(junk)} chg={len(chg_miss)} settings={len(set_miss)}"
        lines.append(f"{p.name}: total={len(d)}{flag}")

    lines.append("")
    lines.append("Summary:")
    lines.append(f"  en key count: {len(en)}")
    lines.append(f"  packs with gaps before: {packs_with_gaps or '(none)'}")
    lines.append(f"  files updated: {files_updated}")
    lines.append(f"  keys added (missing filled): {total_added}")
    lines.append(f"  empty values filled: {total_empty_filled}")
    lines.append(f"  code keys added to en: {en_added}")
    lines.append(f"  remaining missing: {total_missing_after}")
    lines.append(f"  remaining empty: {total_empty_after}")
    lines.append(f"  remaining junk: {remaining_junk}")

    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    print(f"\nWrote report: {REPORT}")


if __name__ == "__main__":
    main()
