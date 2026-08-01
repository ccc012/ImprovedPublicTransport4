# -*- coding: utf-8 -*-
"""Remove # comments and orphan multi-line junk from Translations/*.txt; keep valid KEY value lines."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1] / "Translations"
key_re = re.compile(r"^[A-Z0-9_]{2,}(?:\s|$)")

for path in sorted(root.glob("*.txt")):
    if path.name.endswith(".fixed.txt"):
        continue
    raw = path.read_text(encoding="utf-8", errors="replace").splitlines()
    out = []
    seen = {}
    for line in raw:
        if not line.strip():
            continue
        if line.lstrip().startswith("#"):
            continue
        # Drop orphan bullet / English fragment lines that are not keys
        first = line.split(" ", 1)[0]
        if not key_re.match(first + " "):
            # allow multi-line only if we already have a key (we drop orphans after clean)
            continue
        # Keep last occurrence of each key (sync appends duplicates)
        seen[first] = line
    # Preserve approximate original order of first appearance
    order = []
    for line in raw:
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        first = line.split(" ", 1)[0]
        if first in seen and first not in order:
            order.append(first)
    cleaned = [seen[k] for k in order]
    path.write_text("\n".join(cleaned) + "\n", encoding="utf-8")
    print(f"{path.name}: {len(raw)} -> {len(cleaned)}")

# Ensure pt-br has full set from pt for Portuguese quality
en = {}
pt = {}
for line in (root / "en.txt").read_text(encoding="utf-8").splitlines():
    sp = line.find(" ")
    if sp > 0:
        en[line[:sp]] = line
for line in (root / "pt.txt").read_text(encoding="utf-8").splitlines():
    sp = line.find(" ")
    if sp > 0:
        pt[line[:sp]] = line
pbr_path = root / "pt-br.txt"
pbr = {}
for line in pbr_path.read_text(encoding="utf-8").splitlines():
    sp = line.find(" ")
    if sp > 0:
        pbr[line[:sp]] = line
# Prefer pt, then existing pbr, then en
final = []
for k in en:
    if k in pt:
        final.append(pt[k])
    elif k in pbr:
        final.append(pbr[k])
    else:
        final.append(en[k])
pbr_path.write_text("\n".join(final) + "\n", encoding="utf-8")
print(f"pt-br rebuilt: {len(final)} keys")
