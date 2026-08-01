# -*- coding: utf-8 -*-
"""Refresh the standard Workshop header on all 30 Steam language files.

Does NOT rewrite the body. Strips prior banner/header stacks, then inserts
HEADER.template.bbcode with VERSION + GAME_VERSION.

Usage:
  python apply_steam_header.py
  python apply_steam_header.py --version 4.9.0
"""
from __future__ import print_function
import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FRAG = ROOT / "fragments"

STEAM_LANGS = [
    "english",
    "german",
    "french",
    "italian",
    "koreana",
    "spanish",
    "schinese",
    "tchinese",
    "russian",
    "thai",
    "japanese",
    "portuguese",
    "polish",
    "danish",
    "dutch",
    "finnish",
    "norwegian",
    "swedish",
    "hungarian",
    "czech",
    "romanian",
    "turkish",
    "brazilian",
    "bulgarian",
    "greek",
    "ukrainian",
    "latam",
    "vietnamese",
    "indonesian",
    "malay",
]

# First "real" section heading (not the mod title)
BODY_START = re.compile(
    r"(\[h1\](?!Improved Public Transport 4)"
    r"[^\]]*\[/h1\])",
    re.IGNORECASE,
)

LEADING_BRAND = re.compile(
    r"\A\s*"
    r"(?:\[img\][^\[]*\[/img\]\s*)?"
    r"(?:\[h1\]Improved Public Transport 4\[/h1\]\s*)?"
    r"(?:\[b\]One mod instead of fifteen\.[^\n]*\n\s*)?"
    r"(?:\[b\](?:Version|Versão|Version)[^\n]*\n\s*)?"
    r"(?:\[hr\]\[/hr\]\s*)?",
    re.IGNORECASE,
)


def read_one_line(path, default):
    if path.exists():
        return path.read_text(encoding="utf-8").strip().splitlines()[0].strip()
    return default


def build_header(version, game_version):
    tpl = (FRAG / "HEADER.template.bbcode").read_text(encoding="utf-8")
    return (
        tpl.replace("{VERSION}", version)
        .replace("{GAME_VERSION}", game_version)
        .rstrip()
        + "\n\n"
    )


def extract_body(raw):
    t = raw.replace("\r\n", "\n").replace("\r", "\n")
    lines = []
    for line in t.split("\n"):
        if "Idioma Original Base" in line or "Conforme o pedido" in line:
            continue
        lines.append(line)
    t = "\n".join(lines)

    m = BODY_START.search(t)
    if m:
        return t[m.start() :].lstrip("\n")

    body = t
    for _ in range(8):
        nb = LEADING_BRAND.sub("", body, count=1).lstrip("\n")
        if nb == body:
            break
        body = nb
    return body.lstrip("\n")


def process_file(path, header):
    body = extract_body(path.read_text(encoding="utf-8"))
    new = header + body
    if not new.endswith("\n"):
        new += "\n"
    path.write_text(new, encoding="utf-8")
    return len(new)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", default=None)
    ap.add_argument("--game-version", default=None)
    args = ap.parse_args()

    version = args.version or read_one_line(FRAG / "VERSION.txt", "4.8.0")
    game_version = args.game_version or read_one_line(
        FRAG / "GAME_VERSION.txt", "1.21.1-f9"
    )
    if args.version:
        (FRAG / "VERSION.txt").write_text(version + "\n", encoding="utf-8")
    if args.game_version:
        (FRAG / "GAME_VERSION.txt").write_text(game_version + "\n", encoding="utf-8")

    header = build_header(version, game_version)
    ok = 0
    for stem in STEAM_LANGS:
        path = ROOT / ("workshop-description-%s.txt" % stem)
        if not path.exists():
            print("MISSING", path.name)
            continue
        n = process_file(path, header)
        print("OK  %-12s %5d chars" % (stem, n))
        ok += 1

    en_src = ROOT / "workshop-description-english.txt"
    en_alias = ROOT / "workshop-description-en.txt"
    if en_src.exists():
        en_alias.write_text(en_src.read_text(encoding="utf-8"), encoding="utf-8")
        print("OK  en (alias of english)")

    print(
        "\nDone: %d/%d | Version %s | Game %s"
        % (ok, len(STEAM_LANGS), version, game_version)
    )
    print("--- header ---")
    print(header.rstrip())


if __name__ == "__main__":
    main()
