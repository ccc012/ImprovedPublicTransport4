# -*- coding: utf-8 -*-
"""Generate no/sv/fi/hu from pack modules + write ro/el/vi/ms from embedded full packs.
Also verify da. Run: python gen_remaining_langs.py
"""
from __future__ import print_function
from pathlib import Path
import importlib.util
import sys

ROOT = Path(__file__).resolve().parents[1] / "Translations"
SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))


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
        if not k.startswith("CHANGELOG_") and tr.get(k, v) == v
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
    if missing:
        print("  missing sample:", missing[:10])
    return non_cl_same, missing


def load(name):
    path = SCRIPTS / f"{name}.py"
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def main():
    en_keys, order = parse(ROOT / "en.txt")
    non_cl = sum(1 for k in en_keys if not k.startswith("CHANGELOG_"))
    print(f"en keys={len(en_keys)} non-CL={non_cl}")

    # da already on disk
    da_keys, _ = parse(ROOT / "da.txt")
    da_same = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and da_keys.get(k) == v
    )
    print(f"da: non-CL same_as_en={da_same}")

    from lang_packs_all import NO
    from lang_packs_sv_fi import SV, FI
    from lang_packs_hu_ro import HU
    from lang_packs_ro import RO
    from lang_packs_el import EL
    from lang_packs_vi_ms import VI, MS

    packs = {
        "no": NO,
        "sv": SV,
        "fi": FI,
        "hu": HU,
        "ro": RO,
        "el": EL,
        "vi": VI,
        "ms": MS,
    }
    results = {}
    for lang, tr in packs.items():
        results[lang] = write_lang(lang, tr, en_keys, order)

    print("--- summary ---")
    print(f"da: non-CL same_as_en={da_same}")
    for lang, (same, miss) in results.items():
        print(f"{lang}: non-CL same_as_en={same} missing={len(miss)}")


if __name__ == "__main__":
    main()
