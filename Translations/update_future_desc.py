#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script to update SETTINGS_FUTURE_GROUP_DESCRIPTION in all 26 language files.
"""

import os

# Updated translations for SETTINGS_FUTURE_GROUP_DESCRIPTION
FUTURE_DESC_TRANSLATIONS = {
    "ar": "معاينة للميزات المخطط لها. تظهر رمادية حتى تصبح جاهزة؛ وجهة الركاب تظل متاحة للاختبار.",
    "bg": "Преглед на планирани функции. Показани в сиво докато са готови; дестинацията на пътниците остава достъпна за тестване.",
    "bn": "পরিকল্পিত বৈশিষ্ট্যগুলোর পূর্বদর্শন। প্রস্তুত না হওয়া পর্যন্ত ধূসর হয়ে থাকে; যাত্রী গন্তব্য টেস্টিং-এর জন্য উপলব্ধ থাকে।",
    "cs": "Náhled plánovaných funkcí. Zobrazeno zašedlé dokud nejsou připravené; cesta pasažéra zůstává dostupná pro testování.",
    "da": "Forhåndsvisning af planlagte funktioner. Vises grået ud indtil de er klare; passagerdestination forbliver tilgængelig til test.",
    "el": "Προεπισκόπηση σχεδιαζόμενων λειτουργιών. Εμφανίζονται σβήστες μέχρι να είναι έτοιμες· η προοδός επιβατών παραμένει διαθέσιμη για δοκιμή.",
    "fi": "Suunniteltujen toimintojen esikatselu. Näytetään harmaana kunnes valmiita; matkustajan kohde on edelleen testattavissa.",
    "hi": "नियोजित फीचर्स का पूर्वावलोकन। तैयार होने तक ग्रे आउट दिखाया जाता है; पैसेंजर डेस्टिनेशन टेस्टिंग के लिए उपलब्ध रहता है।",
    "hu": "Tervezett funkciók előnézete. Szürkén показано, amíg kész nem lesz; az utas célállomás tesztelésre elérhető marad.",
    "id": "Pratinjau fitur yang direncanakan. Ditampilkan abu-abu hingga siap; tujuan penumpang tetap tersedia untuk pengujian.",
    "ja": "計画中機能のプレビュー。準備できるまでグレーアウト表示 — 乗客目的地はテスト用に利用可能。",
    "ko": "계획된 기능 미리보기. 준비될 때까지 회색 표시 — 승객 목적지는 테스트용으로 이용 가능.",
    "kr": "계획된 기능 미리보기. 준비될 때까지 회색 표시 — 승객 목적지는 테스트용으로 이용 가능.",
    "ms": "Pratonton ciri yang dirancang. Dipaparkan kelabu sehingga sedia; destinasi penumpang kekal tersedia untuk ujian.",
    "no": "Forhåndsvisning av planlagte funksjoner. Vises gråt ut til de er klare; passasjerdestinasjon forblir tilgjengelig for testing.",
    "ro": "Previzualizare a funcțiilor planificate. Afișat gri până sunt gata; destinația pasagerilor rămâne disponibilă pentru testare.",
    "ru": "Предварительный просмотр планируемых функций. Показано серым, пока не готово; пункт назначения пассажиров остаётся доступным для тестирования.",
    "sk": "Prehľad plánovaných funkcií. Zobrazené zašedlé kým nie sú pripravené; cieľová stanica cestujúceho zostáva dostupná na testovanie.",
    "sv": "Förhandsvisning av planerade funktioner. Visas gråut tills de är klara; passagerardestination förblir tillgänglig för test.",
    "th": "พรีวิวฟีเจอร์ที่วางแผนไว้ แสดงเป็นสีเทาจนกว่าจะพร้อม จุดหมายปลายทางผู้โดยสารยังคงพร้อมสำหรับทดสอบ",
    "tr": "Planlanan özelliklerin önizlemesi. Hazır olana kadar gri renkte gösterilir; yolcu hedefi test için kullanılabilir.",
    "uk": "Попередній перегляд планованих функцій. Показано сірим, поки не готові; пункт призначення пасажира залишається доступним для тестування.",
    "ur": "مخطوطہ فیچرز کا پیش نظارہ۔ تیار ہونے تک سیریہ میں دکھایا جاتا ہے؛ مسافر کی منزل ٹیسٹنگ کے لیے دستیاب رہتی ہے۔",
    "vi": "Xem trước các tính năng dự định. Hiển thị xám cho đến khi sẵn sàng; điểm đến hành khách vẫn có sẵn để kiểm tra.",
    "zh": "计划功能预览。就绪前显示为灰色；乘客目的地保持可用于测试。",
    "zh-tw": "計畫功能預覽。就緒前顯示為灰色；乘客目的地保持可用於測試。",
}

LANGUAGES = [
    "ar", "bg", "bn", "cs", "da", "el", "fi", "hi", "hu", "id",
    "ja", "ko", "kr", "ms", "no", "ro", "ru", "sk", "sv", "th",
    "tr", "uk", "ur", "vi", "zh", "zh-tw"
]

def read_translation_file(filepath):
    """Read a translation file and return raw lines."""
    with open(filepath, 'r', encoding='utf-8') as f:
        return [line.rstrip('\n\r') for line in f]

def write_translation_file(filepath, raw_lines):
    """Write translation file from raw lines."""
    with open(filepath, 'w', encoding='utf-8') as f:
        for line in raw_lines:
            f.write(line + '\n')

def update_future_description(lang_code, translations_dir):
    """Update SETTINGS_FUTURE_GROUP_DESCRIPTION in a language file."""
    filepath = os.path.join(translations_dir, f"{lang_code}.txt")
    
    if not os.path.exists(filepath):
        print(f"  File not found: {filepath}")
        return False
    
    raw_lines = read_translation_file(filepath)
    new_value = FUTURE_DESC_TRANSLATIONS.get(lang_code)
    
    if not new_value:
        print(f"  WARNING: No translation for {lang_code}")
        return False
    
    # Find and update the key
    updated = False
    for i, line in enumerate(raw_lines):
        if line.startswith('SETTINGS_FUTURE_GROUP_DESCRIPTION '):
            raw_lines[i] = f"SETTINGS_FUTURE_GROUP_DESCRIPTION {new_value}"
            updated = True
            break
    
    if updated:
        write_translation_file(filepath, raw_lines)
        print(f"  [OK] {lang_code}: Updated SETTINGS_FUTURE_GROUP_DESCRIPTION")
        return True
    else:
        print(f"  [SKIP] {lang_code}: Key not found")
        return False

def main():
    translations_dir = r"C:\Users\Lucas\source\repos\cs1_ipt4\Translations"
    
    print("=" * 60)
    print("Updating SETTINGS_FUTURE_GROUP_DESCRIPTION")
    print("=" * 60)
    
    total_updated = 0
    
    for lang in LANGUAGES:
        print(f"\nProcessing {lang}...")
        if update_future_description(lang, translations_dir):
            total_updated += 1
    
    print("\n" + "=" * 60)
    print(f"Summary: {total_updated} files updated")
    print("=" * 60)

if __name__ == "__main__":
    main()