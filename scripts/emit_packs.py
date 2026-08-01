# -*- coding: utf-8 -*-
"""Emit Translations/<lang>.txt from in-memory packs. Execute via: python emit_packs.py"""
from __future__ import print_function
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"
sys.path.insert(0, str(Path(__file__).resolve().parent))


def parse_en():
    order, keys = [], {}
    for line in (ROOT / "en.txt").read_text(encoding="utf-8").splitlines():
        if not line.strip():
            order.append(None)
            continue
        i = line.find(" ")
        if i <= 0:
            order.append(None)
            continue
        k, v = line[:i], line[i + 1 :]
        keys[k] = v
        order.append(k)
    return keys, order


def write_lang(lang, tr, en_keys, order):
    lines = []
    missing = []
    for k in order:
        if k is None:
            lines.append("")
            continue
        if k.startswith("CHANGELOG_"):
            val = en_keys[k]
        else:
            val = tr.get(k)
            if val is None:
                missing.append(k)
                val = en_keys[k]
        lines.append("%s %s" % (k, val))
    (ROOT / ("%s.txt" % lang)).write_text("\n".join(lines) + "\n", encoding="utf-8")
    non_cl_same = sum(
        1
        for k, v in en_keys.items()
        if (not k.startswith("CHANGELOG_")) and tr.get(k, v) == v
    )
    total_same = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else tr.get(k, en_keys[k])) == v
    )
    print(
        "%s: missing=%d non-CL same_as_en=%d total_same_as_en=%d"
        % (lang, len(missing), non_cl_same, total_same)
    )
    if missing:
        print("  missing sample:", missing[:12])
    return non_cl_same


def main():
    en_keys, order = parse_en()
    from lang_packs_all import NO
    from lang_packs_sv_fi import SV, FI
    from lang_packs_hu_ro import HU

    for lang, tr in [("no", NO), ("sv", SV), ("fi", FI), ("hu", HU)]:
        write_lang(lang, tr, en_keys, order)

    # da already written; report
    da = {}
    for line in (ROOT / "da.txt").read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        i = line.find(" ")
        if i > 0:
            da[line[:i]] = line[i + 1 :]
    print(
        "da: non-CL same_as_en=%d"
        % sum(
            1
            for k, v in en_keys.items()
            if (not k.startswith("CHANGELOG_")) and da.get(k) == v
        )
    )


if __name__ == "__main__":
    main()
