# -*- coding: utf-8 -*-
"""Apply complete translation packs for remaining stub languages.
Reads en.txt order; for each lang uses a complete dict; CHANGELOG_* from en.
"""
from __future__ import print_function
from pathlib import Path
import importlib.util
import sys

ROOT = Path(__file__).resolve().parents[1] / "Translations"
SCRIPTS = Path(__file__).resolve().parent


def parse(path):
    order, keys = [], {}
    for line in path.read_text(encoding="utf-8").splitlines():
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
        lines.append(f"{k} {val}")
    (ROOT / f"{lang}.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    non_cl_same = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and (tr.get(k) == v or k not in tr)
    )
    total_same = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else tr.get(k, en_keys[k])) == v
    )
    cl = sum(1 for k in en_keys if k.startswith("CHANGELOG_"))
    print(
        f"{lang}: missing={len(missing)} non-CL same_as_en={non_cl_same} "
        f"total_same_as_en={total_same} (CL={cl})"
    )
    if missing[:5]:
        print("  sample missing:", missing[:5])
    return non_cl_same


def load_module(name):
    path = SCRIPTS / f"{name}.py"
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def main():
    en_keys, order = parse(ROOT / "en.txt")
    # da already written fully; re-verify from file
    for lang in ["da"]:
        keys, _ = parse(ROOT / f"{lang}.txt")
        non_cl = sum(
            1
            for k, v in en_keys.items()
            if not k.startswith("CHANGELOG_") and keys.get(k) == v
        )
        total = sum(1 for k, v in en_keys.items() if keys.get(k) == v)
        print(f"{lang}: non-CL same_as_en={non_cl} total_same_as_en={total}")

    packs_mod = load_module("lang_packs_all")
    for lang in ["fi", "no", "sv", "hu", "ro", "el", "vi", "ms"]:
        tr = getattr(packs_mod, lang.upper())
        write_lang(lang, tr, en_keys, order)


if __name__ == "__main__":
    main()
