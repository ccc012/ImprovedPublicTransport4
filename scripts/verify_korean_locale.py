#!/usr/bin/env python3
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TRANSLATIONS = ROOT / "Translations"
KEY = re.compile(r"^[A-Z][A-Z0-9_]*$")
MOJIBAKE = ("Ã", "Â", "â€", "ê°", "ëŒ", "ì„", "í•", "ðŸ")
CORRUPTED_KOREAN = ("포킬", "오버필로우", "세이뷔", "체쿠어", "대진되었습니다", "대중교여", "어림집어", "재산되는", "발생시힌", "바블곰", "64버트", "바로잖습니다", "서버스")
KNOWN_ENGLISH_RESIDUE = ("Show next stop", "Zoom the camera", "Open/close line panel", "Reduced log spam", "Re-enabled Commuter Destination", "Planned:")
ASCII_ONLY_ALLOWLIST = {
    "SETTINGS_COMPAT_BADGE_OK",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_MAPVIEW",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_STOPSTACKER",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_TMPE",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_FPSCAMERA",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_OWTRAINTRACKS",
    "SETTINGS_COMPAT_RECOMMENDED_MOD_COMMUTERDEST",
    "SETTINGS_SPEED_KPH",
    "SETTINGS_SPEED_MPH",
}
PLACEHOLDER = re.compile(r"\{\d+\}")
ALIASES = {"ko": "ko", "kr": "ko", "ko-kr": "ko"}


def load(stem):
    raw = (TRANSLATIONS / f"{stem}.txt").read_bytes()
    text = raw.decode("utf-8-sig")
    assert "\ufffd" not in text, f"{stem}: U+FFFD"
    found = [marker for marker in MOJIBAKE if marker in text]
    assert not found, f"{stem}: mojibake {found}"
    entries = {}
    keys = []
    for number, line in enumerate(text.splitlines(), 1):
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        key, separator, value = line.partition(" ")
        assert separator and KEY.fullmatch(key), f"{stem}:{number}: malformed line"
        assert value, f"{stem}:{number}: empty value for {key}"
        keys.append(key)
        entries[key] = value
    duplicates = [key for key, count in Counter(keys).items() if count > 1]
    assert not duplicates, f"{stem}: duplicate keys {duplicates}"
    return raw, entries


def main():
    _, english = load("en")
    _, korean = load("ko")
    assert set(korean) == set(english), f"ko key parity: missing={sorted(set(english) - set(korean))}, extra={sorted(set(korean) - set(english))}"
    residue = {key: value for key, value in korean.items() if any(marker in value for marker in KNOWN_ENGLISH_RESIDUE)}
    assert not residue, f"ko known English residue: {sorted(residue)}"
    corrupted = {key: value for key, value in korean.items() if any(marker in value for marker in CORRUPTED_KOREAN)}
    assert not corrupted, f"ko corrupted Korean: {sorted(corrupted)}"
    ascii_only = [key for key, value in korean.items() if value.isascii() and key not in ASCII_ONLY_ALLOWLIST]
    assert not ascii_only, f"ko untranslated ASCII-only values: {ascii_only}"
    placeholder_mismatches = [key for key in english if Counter(PLACEHOLDER.findall(english[key])) != Counter(PLACEHOLDER.findall(korean[key]))]
    assert not placeholder_mismatches, f"ko placeholder mismatch: {placeholder_mismatches}"
    loaded = {}
    for stem in sorted(("ko", "kr")):
        loaded.setdefault(ALIASES[stem], stem)
    for locale in ("ko", "kr", "ko-KR"):
        canonical = ALIASES[locale.lower()]
        assert loaded[canonical] == "ko", f"{locale} selected {loaded[canonical]}"
    print(f"PASS: UTF-8; no U+FFFD/mojibake/residual English/corrupted Korean; placeholders preserved; {len(english)} keys; no duplicates; ko/kr/ko-KR select ko")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, UnicodeDecodeError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        raise SystemExit(1)
