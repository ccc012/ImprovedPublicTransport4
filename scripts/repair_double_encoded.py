"""Repair double-encoded UTF-8 packs, per line, with a strict all-or-nothing rule.

Why the previous attempt failed
-------------------------------
It decoded whole lines with `text.encode('latin-1').decode('utf-8')` inside a
try/except that returned the ORIGINAL text on failure. A line is only fully
recoverable that way if every one of its non-ASCII characters is part of a
re-encoded pair; a single stray character made the whole line silently fall
through unchanged, which is why de/fr/hu/sv still carried thousands of C3 83
markers after the "fix".

What this does instead
----------------------
Walks each line character by character and collapses only the pairs that are
provably re-encoded (a U+00C0-U+00FF lead followed by a U+0080-U+00BF
continuation, both of which round-trip through latin-1 into valid UTF-8).
Characters that are not part of such a pair are left exactly as they are, so a
partially-corrupted line is repaired instead of skipped.
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"
TARGETS = ["de.txt", "fr.txt", "hu.txt", "sv.txt", "pt.txt", "pt-br.txt"]


def repair(text: str) -> str:
    out = []
    i = 0
    n = len(text)
    while i < n:
        ch = text[i]
        # Lead byte of a re-encoded 2-byte sequence lives in U+00C2..U+00DF,
        # 3-byte in U+00E0..U+00EF. Anything below U+0080 is plain ASCII.
        if 0xC2 <= ord(ch) <= 0xEF:
            for length in (4, 3, 2):
                if i + length > n:
                    continue
                chunk = text[i:i + length]
                if any(ord(c) > 0xFF for c in chunk):
                    continue
                try:
                    decoded = chunk.encode("latin-1").decode("utf-8")
                except (UnicodeEncodeError, UnicodeDecodeError):
                    continue
                # Only accept a real collapse: fewer characters out than in.
                if len(decoded) < length:
                    out.append(decoded)
                    i += length
                    break
            else:
                out.append(ch)
                i += 1
            continue
        out.append(ch)
        i += 1
    return "".join(out)


def markers(data: bytes) -> int:
    return data.count(b"\xc3\x83")


def main() -> None:
    for name in TARGETS:
        path = ROOT / name
        if not path.exists():
            print(f"missing {name}")
            continue

        before = markers(path.read_bytes())
        lines = path.read_text(encoding="utf-8-sig").splitlines()

        # Repeat until stable: a triple-encoded line needs more than one pass.
        for _ in range(5):
            repaired = [repair(l) for l in lines]
            if repaired == lines:
                break
            lines = repaired

        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        after = markers(path.read_bytes())
        print(f"{name:12} C383 {before:5} -> {after:5}")


if __name__ == "__main__":
    main()