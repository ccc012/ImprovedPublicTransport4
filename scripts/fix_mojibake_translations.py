# -*- coding: utf-8 -*-
"""Repair double-encoded UTF-8 (mojibake) in Translations/*.txt packs.

Handles both pure latin-1 and Windows-1252 reinterpretations (e.g. Ã”nibus -> Ônibus),
including mixed lines where only some codepoints are still mojibake.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"


def fix_line(line: str) -> str:
    # Whole-line recoveries first.
    for enc in ("latin-1", "cp1252"):
        try:
            return line.encode(enc).decode("utf-8")
        except (UnicodeEncodeError, UnicodeDecodeError):
            pass

    # Walk: greedily decode 2–4 char cp1252/latin-1 chunks that form valid UTF-8.
    out = []
    j = 0
    s = line
    while j < len(s):
        matched = False
        for n in (4, 3, 2):
            if j + n > len(s):
                continue
            chunk = s[j : j + n]
            for enc in ("cp1252", "latin-1"):
                try:
                    dec = chunk.encode(enc).decode("utf-8")
                except (UnicodeEncodeError, UnicodeDecodeError):
                    continue
                # Accept only multi-byte style recoveries (non-ascii or shortened).
                if any(ord(c) > 127 for c in dec) or len(dec) < n:
                    out.append(dec)
                    j += n
                    matched = True
                    break
            if matched:
                break
        if not matched:
            out.append(s[j])
            j += 1
    return "".join(out)


def fix_file(path: Path) -> None:
    text = path.read_text(encoding="utf-8-sig")
    fixed = [fix_line(line) for line in text.splitlines()]
    path.write_text("\n".join(fixed) + "\n", encoding="utf-8")
    # Show key samples
    for line in fixed:
        if (
            line.startswith("CITYSERVICE_ACCEPTINTERCITYBUSES ")
            or line.startswith("CITY_SERVICE_PANEL_TITLE_STATION_STOPS ")
            or line.startswith("DEPOT_STATS_")
            or "Ônibus" in line
            or "REMOÇÃO" in line
            or "Âmbar" in line
        ):
            if line.startswith("CITYSERVICE_ACCEPT") or line.startswith("DEPOT_STATS_") or any(
                k in line
                for k in (
                    "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS ",
                    "SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE ",
                    "SETTINGS_TRAINDISPLAY_THEME_AMBER ",
                    "SETTINGS_EBS_TOOLTIP_MINIBUS ",
                )
            ):
                print(f"  {path.name}: {line[:120]}")


def main() -> None:
    targets = ["pt-br.txt", "pt.txt"]
    for name in targets:
        path = ROOT / name
        if not path.exists():
            print(f"missing {path}")
            continue
        print(f"fixing {path}")
        fix_file(path)


if __name__ == "__main__":
    main()
