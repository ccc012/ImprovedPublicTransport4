# -*- coding: utf-8 -*-
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "Translations"
en_path = root / "en.txt"
replacements = {
    "SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING": (
        "SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING How trams leave stops (not the same as buses):\n"
        "• Disabled — use the spacing settings above.\n"
        "• Every stop (realistic) — always stops and waits the full time.\n"
        "• Express skip — may leave early when nobody boards/alights."
    ),
    "SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL": "SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL Every stop (realistic)",
    "SETTINGS_EBS_TRAM_MODE_TRAM": "SETTINGS_EBS_TRAM_MODE_TRAM Express skip",
    "SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE": "SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE Tram stops:",
}

lines = en_path.read_text(encoding="utf-8").splitlines()
out = []
for line in lines:
    key = line.split(" ", 1)[0] if line.strip() and not line.startswith("#") else None
    if key in replacements:
        out.append(replacements[key])
    else:
        out.append(line)
en_path.write_text("\n".join(out) + "\n", encoding="utf-8")
print("en fixed")

# Sync selected keys pt -> pt-br
pt = {}
for line in (root / "pt.txt").read_text(encoding="utf-8").splitlines():
    if not line.strip() or line.startswith("#"):
        continue
    sp = line.find(" ")
    if sp > 0:
        pt[line[:sp]] = line

keys = [
    "SETTINGS_GAMEPLAY_PROFILE_TOOLTIP",
    "SETTINGS_GAMEPLAY_PROFILE_DESC_BLOCK",
    "SETTINGS_GAMEPLAY_PROFILE_SAFE",
    "SETTINGS_GAMEPLAY_PROFILE_VANILLA",
    "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED",
    "SETTINGS_GAMEPLAY_PROFILE_REALISTIC",
    "SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING",
    "SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL",
    "SETTINGS_EBS_TRAM_MODE_TRAM",
    "SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE",
    "SETTINGS_TICKET_PATHFINDING_COST",
    "SETTINGS_TICKET_PATHFINDING_COST_TOOLTIP",
    "SETTINGS_TRAINDISPLAY_SHOW_STATE",
    "SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP",
]
pbr = root / "pt-br.txt"
lines = pbr.read_text(encoding="utf-8").splitlines()
have = set()
new = []
for line in lines:
    if line.strip() and not line.startswith("#"):
        sp = line.find(" ")
        if sp > 0 and line[:sp] in keys and line[:sp] in pt:
            new.append(pt[line[:sp]])
            have.add(line[:sp])
            continue
    new.append(line)
for k in keys:
    if k not in have and k in pt:
        new.append(pt[k])
pbr.write_text("\n".join(new) + "\n", encoding="utf-8")
print("pt-br synced")
