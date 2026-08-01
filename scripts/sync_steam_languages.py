# -*- coding: utf-8 -*-
"""Ensure IPT4 Translations/*.txt cover every Steam Workshop language + sync new keys."""
from __future__ import print_function
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"


def parse(path):
    keys = {}
    order = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            order.append(("", ""))
            continue
        i = line.find(" ")
        if i <= 0:
            order.append(("", line))
            continue
        k, v = line[:i], line[i + 1 :]
        keys[k] = v
        order.append((k, v))
    return keys, order


en_keys, en_order = parse(ROOT / "en.txt")

# New keys (5) that exist in en but not older language packs
NEW = {
    "SETTINGS_GAMEPLAY_PROFILE_SAFE": {
        "en": "Safe (all off)",
        "de": "Sicher (alles aus)",
        "fr": "Sûr (tout désactivé)",
        "es": "Seguro (todo desactivado)",
        "es-419": "Seguro (todo desactivado)",
        "it": "Sicuro (tutto spento)",
        "pt": "Seguro (tudo desligado)",
        "pt-br": "Seguro (tudo desligado)",
        "nl": "Veilig (alles uit)",
        "pl": "Bezpieczny (wszystko wyłączone)",
        "ru": "Безопасный (всё выкл.)",
        "ja": "セーフ（すべてオフ）",
        "ko": "안전 (모두 끔)",
        "zh-cn": "安全（全部关闭）",
        "zh-tw": "安全（全部關閉）",
        "zh": "安全（全部关闭）",
        "cs": "Bezpečný (vše vypnuto)",
        "sk": "Bezpečný (všetko vypnuté)",
        "tr": "Güvenli (hepsi kapalı)",
        "th": "ปลอดภัย (ปิดทั้งหมด)",
        "id": "Aman (semua mati)",
        "ms": "Selamat (semua dimatikan)",
        "ar": "آمن (الكل متوقف)",
        "hi": "सुरक्षित (सब बंद)",
        "bn": "নিরাপদ (সব বন্ধ)",
        "ur": "محفوظ (سب بند)",
        "da": "Sikker (alt slået fra)",
        "fi": "Turvallinen (kaikki pois)",
        "no": "Sikker (alt av)",
        "sv": "Säker (allt av)",
        "hu": "Biztonságos (minden ki)",
        "ro": "Sigur (totul oprit)",
        "bg": "Безопасен (всичко изкл.)",
        "el": "Ασφαλές (όλα off)",
        "uk": "Безпечний (усе вимкнено)",
        "vi": "An toàn (tắt hết)",
    },
    "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED": {
        "en": "Recommended (IPT core)",
        "de": "Empfohlen (IPT-Kern)",
        "fr": "Recommandé (cœur IPT)",
        "es": "Recomendado (núcleo IPT)",
        "es-419": "Recomendado (núcleo IPT)",
        "it": "Consigliato (nucleo IPT)",
        "pt": "Recomendado (núcleo IPT)",
        "pt-br": "Recomendado (núcleo IPT)",
        "nl": "Aanbevolen (IPT-kern)",
        "pl": "Zalecany (rdzeń IPT)",
        "ru": "Рекомендуемый (ядро IPT)",
        "ja": "推奨（IPTコア）",
        "ko": "권장 (IPT 핵심)",
        "zh-cn": "推荐（IPT 核心）",
        "zh-tw": "推薦（IPT 核心）",
        "zh": "推荐（IPT 核心）",
        "cs": "Doporučené (jádro IPT)",
        "sk": "Odporúčané (jadro IPT)",
        "tr": "Önerilen (IPT çekirdeği)",
        "th": "แนะนำ (แกน IPT)",
        "id": "Direkomendasikan (inti IPT)",
        "ms": "Disyorkan (teras IPT)",
        "ar": "موصى به (نواة IPT)",
        "hi": "अनुशंसित (IPT कोर)",
        "bn": "সুপারিশকৃত (IPT কোর)",
        "ur": "تجویز کردہ (IPT کور)",
        "da": "Anbefalet (IPT-kerne)",
        "fi": "Suositeltu (IPT-ydin)",
        "no": "Anbefalt (IPT-kjerne)",
        "sv": "Rekommenderad (IPT-kärna)",
        "hu": "Ajánlott (IPT mag)",
        "ro": "Recomandat (nucleu IPT)",
        "bg": "Препоръчан (ядро IPT)",
        "el": "Προτεινόμενο (πυρήνας IPT)",
        "uk": "Рекомендований (ядро IPT)",
        "vi": "Khuyến nghị (lõi IPT)",
    },
    "SETTINGS_TRAINDISPLAY_THEME_BLUE": {
        "en": "Blue", "de": "Blau", "fr": "Bleu", "es": "Azul", "es-419": "Azul", "it": "Blu",
        "pt": "Azul", "pt-br": "Azul", "nl": "Blauw", "pl": "Niebieski", "ru": "Синий",
        "ja": "青", "ko": "파랑", "zh-cn": "蓝色", "zh-tw": "藍色", "zh": "蓝色",
        "cs": "Modrá", "sk": "Modrá", "tr": "Mavi", "th": "น้ำเงิน", "id": "Biru", "ms": "Biru",
        "ar": "أزرق", "hi": "नीला", "bn": "নীল", "ur": "نیلا", "da": "Blå", "fi": "Sininen",
        "no": "Blå", "sv": "Blå", "hu": "Kék", "ro": "Albastru", "bg": "Синьо", "el": "Μπλε",
        "uk": "Синій", "vi": "Xanh dương",
    },
    "SETTINGS_TRAINDISPLAY_THEME_GREEN": {
        "en": "Green", "de": "Grün", "fr": "Vert", "es": "Verde", "es-419": "Verde", "it": "Verde",
        "pt": "Verde", "pt-br": "Verde", "nl": "Groen", "pl": "Zielony", "ru": "Зелёный",
        "ja": "緑", "ko": "초록", "zh-cn": "绿色", "zh-tw": "綠色", "zh": "绿色",
        "cs": "Zelená", "sk": "Zelená", "tr": "Yeşil", "th": "เขียว", "id": "Hijau", "ms": "Hijau",
        "ar": "أخضر", "hi": "हरा", "bn": "সবুজ", "ur": "سبز", "da": "Grøn", "fi": "Vihreä",
        "no": "Grønn", "sv": "Grön", "hu": "Zöld", "ro": "Verde", "bg": "Зелено", "el": "Πράσινο",
        "uk": "Зелений", "vi": "Xanh lá",
    },
    "SETTINGS_TRAINDISPLAY_THEME_AMBER": {
        "en": "Amber", "de": "Bernstein", "fr": "Ambre", "es": "Ámbar", "es-419": "Ámbar", "it": "Ambra",
        "pt": "Âmbar", "pt-br": "Âmbar", "nl": "Amber", "pl": "Bursztynowy", "ru": "Янтарный",
        "ja": "アンバー", "ko": "호박색", "zh-cn": "琥珀色", "zh-tw": "琥珀色", "zh": "琥珀色",
        "cs": "Jantarová", "sk": "Jantárová", "tr": "Kehribar", "th": "อำพัน", "id": "Kuning tua", "ms": "Ambar",
        "ar": "كهرماني", "hi": "अंबर", "bn": "অ্যাম্বার", "ur": "عنبری", "da": "Rav", "fi": "Meripihka",
        "no": "Rav", "sv": "Bärnsten", "hu": "Borostyán", "ro": "Chihlimbar", "bg": "Кехлибар", "el": "Κεχριμπάρι",
        "uk": "Бурштиновий", "vi": "Hổ phách",
    },
}

STEM_LANG = {
    "ar": "ar", "bn": "bn", "cs": "cs", "de": "de", "es": "es", "fr": "fr", "hi": "hi", "id": "id",
    "it": "it", "ja": "ja", "ko": "ko", "kr": "ko", "nl": "nl", "pl": "pl", "pt": "pt", "ru": "ru",
    "sk": "sk", "th": "th", "tr": "tr", "ur": "ur", "zh": "zh", "zh-cn": "zh-cn", "zh-tw": "zh-tw",
    "da": "da", "fi": "fi", "no": "no", "sv": "sv", "hu": "hu", "ro": "ro", "bg": "bg", "el": "el",
    "uk": "uk", "vi": "vi", "ms": "ms", "pt-br": "pt-br", "es-419": "es-419",
}


def t(key, lang):
    d = NEW[key]
    return d.get(lang) or d["en"]


def main():
    # 1) Append missing NEW keys to existing packs
    for f in sorted(ROOT.glob("*.txt")):
        stem = f.stem.lower()
        if stem == "en":
            continue
        keys, _ = parse(f)
        lang = STEM_LANG.get(stem, "en")
        adds = []
        for k in NEW:
            if k not in keys:
                adds.append("%s %s" % (k, t(k, lang)))
        if adds:
            text = f.read_text(encoding="utf-8")
            if not text.endswith("\n"):
                text += "\n"
            f.write_text(text + "\n".join(adds) + "\n", encoding="utf-8")
            print("updated", f.name, "+", len(adds))

    # 2) Full files for Steam languages not yet present
    bases = {}
    if (ROOT / "pt.txt").exists():
        bases["pt-br"] = parse(ROOT / "pt.txt")[0]
    if (ROOT / "es.txt").exists():
        bases["es-419"] = parse(ROOT / "es.txt")[0]

    missing = [
        ("da", "da"), ("fi", "fi"), ("no", "no"), ("sv", "sv"), ("hu", "hu"),
        ("ro", "ro"), ("bg", "bg"), ("el", "el"), ("uk", "uk"), ("vi", "vi"),
        ("ms", "ms"), ("pt-br", "pt-br"), ("es-419", "es-419"),
    ]

    for stem, lang in missing:
        path = ROOT / ("%s.txt" % stem)
        base = bases.get(stem)
        lines = []
        for k, v in en_order:
            if k == "":
                lines.append("")
                continue
            if base and k in base:
                val = base[k]
            else:
                val = en_keys[k]
            if k in NEW:
                val = t(k, lang)
            lines.append("%s %s" % (k, val))
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        print("wrote", path.name)

    # 3) Verify
    print("--- verify ---")
    for f in sorted(ROOT.glob("*.txt")):
        k = parse(f)[0]
        miss = sorted(set(en_keys) - set(k))
        print("%s: %d keys, missing %d" % (f.name, len(k), len(miss)))
        if miss:
            print(" ", miss[:8])


if __name__ == "__main__":
    main()
