from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
FORBIDDEN = re.compile(
    r"WorldInfoCurrentLineIDQuery\s*\.\s*Query|"
    r"(?:UnityEngine\s*\.\s*)?(?:Object|Resources)\s*\.\s*FindObjectsOfType(?:All)?|"
    r"WorldInfoPanel\s*\.|UIView\s*\.|ToolsModifierControl\s*\."
)


def action_bodies(source):
    start_pattern = re.compile(r"\bAddAction\s*\(\s*\(\s*\)\s*=>")
    for match in start_pattern.finditer(source):
        cursor = match.end()
        while cursor < len(source) and source[cursor].isspace():
            cursor += 1
        if cursor >= len(source):
            continue
        if source[cursor] != "{":
            end = source.find(");", cursor)
            yield match.start(), source[cursor:end if end >= 0 else len(source)]
            continue
        depth = 0
        for end in range(cursor, len(source)):
            if source[end] == "{":
                depth += 1
            elif source[end] == "}":
                depth -= 1
                if depth == 0:
                    yield match.start(), source[cursor:end + 1]
                    break


violations = []
for path in ROOT.rglob("*.cs"):
    if any(part in {"bin", "obj"} for part in path.parts):
        continue
    source = path.read_text(encoding="utf-8-sig")
    for offset, body in action_bodies(source):
        forbidden = FORBIDDEN.search(body)
        if forbidden:
            line = source.count("\n", 0, offset) + 1
            violations.append(f"{path.relative_to(ROOT)}:{line}: {forbidden.group(0)}")

if violations:
    print("simulation action UI access: FAIL")
    print("\n".join(violations))
    sys.exit(1)

print("simulation action UI access: PASS")
