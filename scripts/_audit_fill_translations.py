# -*- coding: utf-8 -*-
"""Audit translation gaps vs en.txt and fill missing keys with English fallback."""
from __future__ import annotations

import re
from pathlib import Path

TRANS = Path(r"C:\Users\Lucas\source\repos\cs1_ipt4\Translations")
REPORT = Path(r"C:\Users\Lucas\source\repos\cs1_ipt4\scripts\_translation_gap_report.txt")

SKIP_NAMES = {"en.txt"}
SKIP_SUFFIXES = (".fixed.txt", ".bak", ".backup", ".orig")

# Optional PT translations for high-priority keys if missing
PT_OVERRIDES = {
    # Will only apply if key is missing; values filled after we know missing set
}

PRIORITY_ORDER = [
    "pt.txt",
    "pt-br.txt",
    "es.txt",
    "es-419.txt",
    "de.txt",
    "fr.txt",
    "it.txt",
    "ru.txt",
    "zh.txt",
    "zh-tw.txt",
    "zh-cn.txt",
]


def parse_lang_file(path: Path) -> dict[str, str]:
    """KEY is first token; rest is value (may be empty)."""
    d: dict[str, str] = {}
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    for line in text.splitlines():
        s = line.strip()
        if not s or s.startswith("#"):
            continue
        parts = s.split(None, 1)
        if not parts:
            continue
        key = parts[0]
        val = parts[1] if len(parts) > 1 else ""
        d[key] = val
    return d


def is_lang_file(name: str) -> bool:
    if not name.endswith(".txt"):
        return False
    if name in SKIP_NAMES:
        return False
    if name.endswith(".fixed.txt"):
        return False
    lower = name.lower()
    for suf in SKIP_SUFFIXES:
        if lower.endswith(suf):
            return False
    return True


def main() -> None:
    en_path = TRANS / "en.txt"
    en = parse_lang_file(en_path)
    en_keys = list(en.keys())  # preserve order from en.txt
    en_key_set = set(en_keys)

    lang_files = sorted(p for p in TRANS.iterdir() if p.is_file() and is_lang_file(p.name))

    # Priority first, then rest alphabetically without duplicates
    ordered: list[Path] = []
    seen: set[str] = set()
    for name in PRIORITY_ORDER:
        p = TRANS / name
        if p in lang_files and name not in seen:
            ordered.append(p)
            seen.add(name)
    for p in lang_files:
        if p.name not in seen:
            ordered.append(p)
            seen.add(p.name)

    before: dict[str, list[str]] = {}
    identical_en_samples: dict[str, list[str]] = {}

    for p in ordered:
        lang = parse_lang_file(p)
        missing = [k for k in en_keys if k not in lang]
        before[p.name] = missing
        # light identical-to-en check (not for en-gb style; we have no en-gb)
        if not p.name.lower().startswith("en"):
            same = [
                k
                for k, v in lang.items()
                if k in en and v == en[k] and v.strip()
            ]
            # only sample a few high-signal ones if many
            identical_en_samples[p.name] = same

    lines: list[str] = []
    lines.append("IPT4 Translation Gap Report")
    lines.append("=" * 60)
    lines.append(f"Master: en.txt ({len(en_keys)} keys)")
    lines.append(f"Language files audited: {len(ordered)}")
    lines.append("")
    lines.append("--- BEFORE FILL ---")
    lines.append("")

    def list_keys(keys: list[str], cap: int = 80) -> list[str]:
        out = []
        if not keys:
            out.append("  (none)")
            return out
        show = keys[:cap]
        for k in show:
            out.append(f"  - {k}")
        if len(keys) > cap:
            out.append(f"  +{len(keys) - cap} more")
        return out

    total_missing_before = 0
    for p in ordered:
        miss = before[p.name]
        total_missing_before += len(miss)
        lines.append(f"{p.name}: {len(miss)} missing")
        lines.extend(list_keys(miss))
        lines.append("")

    lines.append(f"TOTAL missing key-slots before: {total_missing_before}")
    lines.append("")
    lines.append("--- Optional: keys with value EXACTLY equal to English (sample counts) ---")
    for p in ordered:
        same = identical_en_samples.get(p.name, [])
        if same:
            lines.append(f"{p.name}: {len(same)} keys identical to en (not auto-changed)")
    lines.append("")

    # Fill missing
    after: dict[str, list[str]] = {}
    filled_counts: dict[str, int] = {}

    for p in ordered:
        miss = before[p.name]
        if not miss:
            after[p.name] = []
            filled_counts[p.name] = 0
            continue

        raw = p.read_text(encoding="utf-8-sig", errors="replace")
        # Normalize: ensure trailing newline before append block
        if raw and not raw.endswith("\n"):
            raw += "\n"
        if not raw.endswith("\n\n"):
            # add blank line separator if last line isn't already blank
            if not raw.endswith("\n\n"):
                raw += "\n"

        block_lines = [
            "# Auto-filled missing keys from en.txt (temporary English fallback)",
        ]
        for k in miss:
            # pt.txt optional decent PT for priority prefixes
            val = en[k]
            if p.name == "pt.txt":
                pt_val = maybe_pt(k, val)
                if pt_val is not None:
                    val = pt_val
            # Format: KEY value (space-separated like en)
            if val:
                block_lines.append(f"{k} {val}")
            else:
                block_lines.append(k)

        append_text = "\n".join(block_lines) + "\n"
        p.write_text(raw + append_text, encoding="utf-8")
        filled_counts[p.name] = len(miss)

        # re-parse
        lang2 = parse_lang_file(p)
        after[p.name] = [k for k in en_keys if k not in lang2]

    lines.append("--- AFTER FILL ---")
    lines.append("")
    total_missing_after = 0
    for p in ordered:
        miss = after[p.name]
        total_missing_after += len(miss)
        nfill = filled_counts.get(p.name, 0)
        lines.append(f"{p.name}: filled {nfill}, remaining missing {len(miss)}")
        if miss:
            lines.extend(list_keys(miss))
    lines.append("")
    lines.append(f"TOTAL missing key-slots after: {total_missing_after}")
    lines.append("")
    lines.append("Summary:")
    lines.append(f"  en keys: {len(en_keys)}")
    lines.append(f"  files updated: {sum(1 for n, c in filled_counts.items() if c > 0)}")
    lines.append(f"  total keys appended: {sum(filled_counts.values())}")
    lines.append(f"  remaining gaps: {total_missing_after}")

    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    print(f"\nWrote report: {REPORT}")


def maybe_pt(key: str, en_val: str) -> str | None:
    """Return Portuguese for high-priority missing keys if we have a mapping, else None."""
    # Static map for common patterns — only used when key was missing
    # Leave None to use English; populate a few if needed after first run.
    return PT_MAP.get(key)


# Decent PT for likely missing high-priority keys (safe fallbacks if already present unused)
PT_MAP: dict[str, str] = {
    "SETTINGS_EMPTY_BEFORE_DEPOT": "Esvaziar passageiros antes de voltar ao depósito",
    "SETTINGS_EMPTY_BEFORE_DEPOT_TOOLTIP": "Quando ativo, veículos de transporte com passageiros não saem da linha para o depósito até ficarem vazios. Depois voltam. Desligado por padrão; o perfil Realista liga.",
    "SETTINGS_TRAINDISPLAY_TYPES_GROUP": "Tipos de veículo na sobreposição",
    "SETTINGS_TRAINDISPLAY_TYPES_GROUP_DESCRIPTION": "Quais veículos de transporte público podem mostrar o painel do Train Display quando selecionados.",
    "SETTINGS_TRAINDISPLAY_TYPE_TOOLTIP": "Mostrar a sobreposição quando este tipo de veículo estiver selecionado.",
    "SETTINGS_TRAINDISPLAY_TYPE_BUS": "Ônibus",
    "SETTINGS_TRAINDISPLAY_TYPE_TROLLEY": "Trólebus",
    "SETTINGS_TRAINDISPLAY_TYPE_TRAM": "Bonde",
    "SETTINGS_TRAINDISPLAY_TYPE_METRO": "Metrô",
    "SETTINGS_TRAINDISPLAY_TYPE_TRAIN": "Trem",
    "SETTINGS_TRAINDISPLAY_TYPE_MONORAIL": "Monotrilho",
    "SETTINGS_TRAINDISPLAY_TYPE_SHIP": "Navio / balsa",
    "SETTINGS_TRAINDISPLAY_TYPE_PLANE": "Avião / dirigível / heli",
    "SETTINGS_TRAINDISPLAY_TYPE_TAXI": "Táxi",
    "SETTINGS_TRAINDISPLAY_TYPE_CABLECAR": "Teleférico",
    "SETTINGS_TRAINDISPLAY_TYPE_TOURS": "Ônibus turístico",
    "SETTINGS_TRAINDISPLAY_THEME_BLACKSEMI": "Preto (semi-transparente)",
    "SETTINGS_TRAINDISPLAY_SHOW_SPEED": "Mostrar velocidade (faixa extra)",
    "SETTINGS_TRAINDISPLAY_SHOW_PASSENGERS": "Mostrar passageiros (faixa extra)",
    "SETTINGS_TRAINDISPLAY_SHOW_ELAPSED": "Mostrar tempo (faixa extra)",
    "SETTINGS_TRAINDISPLAY_SHOW_EXTRAS_TOOLTIP": "Adiciona uma faixa na cor da linha acima do painel principal com estas extras. Desligado por padrão.",
    "TRAINDISPLAY_LABEL_SPEED": "Velocidade",
    "TRAINDISPLAY_LABEL_TIME": "Tempo",
}


if __name__ == "__main__":
    main()
