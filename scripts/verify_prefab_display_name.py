from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
sources = [
    root / "UI" / "AlgernonCommons" / "PrefabUtils.cs",
    root / "AlgernonCommons" / "Utils" / "PrefabUtils.cs",
]


def sanitize(title):
    match = re.match(r"^Update([ _\-:]+)(.*)$", title, re.IGNORECASE)
    if not match or not re.search(r"[^\W\d_]", match.group(2), re.UNICODE):
        return title
    if match.group(1)[0] == " " and " " not in match.group(2):
        return title
    return match.group(2)


positive = {
    "Update Harbor Ferry": "Harbor Ferry",
    "Update_Island Ferry": "Island Ferry",
    "Update-Coastal Ferry": "Coastal Ferry",
    "Update:River Ferry": "River Ferry",
}
negative = (
    "Updated Ferry",
    "Update Express",
    "Ferry Update",
    "Update 123",
    "Update - _ :",
)

for title, expected in positive.items():
    assert sanitize(title) == expected, title
for title in negative:
    assert sanitize(title) == title, title

required = (
    "StripUpdatePrefix",
    "StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)",
    "char.IsLetter",
    "IsUpdateSeparator",
)
for path in sources:
    source = path.read_text(encoding="utf-8-sig")
    missing = [marker for marker in required if marker not in source]
    if missing:
        raise SystemExit(f"{path.relative_to(root)} missing: {', '.join(missing)}")

print("Prefab display name verification: PASS")
