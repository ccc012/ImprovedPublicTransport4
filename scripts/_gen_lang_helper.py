# -*- coding: utf-8 -*-
"""Helper: write a lang file from a dict, en order, CHANGELOG stays English."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"


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
    for k in order:
        if k is None:
            lines.append("")
            continue
        if k.startswith("CHANGELOG_"):
            val = en_keys[k]
        else:
            val = tr.get(k)
            if val is None:
                raise KeyError(f"{lang} missing {k}")
        lines.append(f"{k} {val}")
    (ROOT / f"{lang}.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    non_cl_same = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and tr.get(k) == v
    )
    total_same = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else tr[k]) == v
    )
    cl = sum(1 for k in en_keys if k.startswith("CHANGELOG_"))
    print(f"{lang}: keys={len(en_keys)} non-CL same_as_en={non_cl_same} total_same_as_en={total_same} (changelog={cl})")
    return non_cl_same
