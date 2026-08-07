# -*- coding: utf-8 -*-
"""Fix translation encoding issues properly by operating on raw bytes."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"

# Files with full double-UTF-8 encoding (C1)
FULL_MOJIBAKE = {"de.txt", "fr.txt", "hu.txt", "sv.txt"}
# Files with partial cp1252 in new keys (C2)
PARTIAL_CP1252 = {"pt.txt", "pt-br.txt"}
# Orphan keys to remove (A2)
ORPHAN_KEYS = {
    "COMMUTER_DESTINATION_PANEL_TITLE",
    "COMMUTER_DESTINATION_HEADER",
    "COMMUTER_DESTINATION_NONE",
    "COMMUTER_DESTINATION_LOADING",
    "COMMUTER_DESTINATION_BUTTON",
    "COMMUTER_DESTINATION_BUTTON_TOOLTIP",
    "COMMUTER_STOP_WITH_WAITING",
}

def fix_full_mojibake(text: str) -> str:
    """Reverse double UTF-8 encoding: UTF-8 bytes -> misread as latin-1 -> re-encoded as UTF-8.
    Fix: encode as latin-1, decode as UTF-8."""
    try:
        return text.encode("latin-1").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        # Fallback: try cp1252
        try:
            return text.encode("cp1252").decode("utf-8")
        except (UnicodeEncodeError, UnicodeDecodeError):
            return text

def fix_partial_cp1252(text: str) -> str:
    """Fix lines that have raw cp1252 bytes embedded in UTF-8 (mixed).
    Only touches non-ASCII chars that are valid cp1252 but not valid UTF-8 continuation."""
    # For each line, try whole-line recovery first
    try:
        return text.encode("cp1252").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        pass
    # Greedy chunk recovery for mixed lines
    out = []
    i = 0
    s = text
    while i < len(s):
        matched = False
        for n in (4, 3, 2):
            if i + n > len(s):
                continue
            chunk = s[i:i+n]
            for enc in ("cp1252", "latin-1"):
                try:
                    dec = chunk.encode(enc).decode("utf-8")
                except (UnicodeEncodeError, UnicodeDecodeError):
                    continue
                if any(ord(c) > 127 for c in dec) or len(dec) < n:
                    out.append(dec)
                    i += n
                    matched = True
                    break
            if matched:
                break
        if not matched:
            out.append(s[i])
            i += 1
    return "".join(out)

def process_file(path: Path, is_full_mojibake: bool, is_partial: bool) -> int:
    raw = path.read_bytes()
    # Decode as UTF-8 (current state)
    text = raw.decode("utf-8-sig")
    lines = text.splitlines()
    fixed_lines = []
    removed = 0
    for line in lines:
        # Skip orphan keys
        skip = False
        for k in ORPHAN_KEYS:
            if line.startswith(k + " "):
                skip = True
                removed += 1
                break
        if skip:
            continue
        # Fix encoding
        if is_full_mojibake:
            line = fix_full_mojibake(line)
        elif is_partial:
            line = fix_partial_cp1252(line)
        fixed_lines.append(line)
    # Write back
    out_text = "\n".join(fixed_lines) + "\n"
    path.write_text(out_text, encoding="utf-8")
    return removed

def fix_en_bullet(path: Path):
    text = path.read_text(encoding="utf-8-sig")
    fixed = text.replace("â€¢", "•")
    path.write_text(fixed, encoding="utf-8")

def main():
    total_removed = 0
    for f in ROOT.glob("*.txt"):
        if f.name == "pt-br.fixed.txt":
            continue
        is_full = f.name in FULL_MOJIBAKE
        is_partial = f.name in PARTIAL_CP1252
        removed = process_file(f, is_full, is_partial)
        total_removed += removed
        print(f"Processed {f.name}: removed {removed} orphan lines")
    fix_en_bullet(ROOT / "en.txt")
    print(f"Fixed bullet in en.txt")
    print(f"Total orphan lines removed: {total_removed}")

if __name__ == "__main__":
    main()