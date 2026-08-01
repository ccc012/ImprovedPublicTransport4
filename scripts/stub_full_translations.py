# -*- coding: utf-8 -*-
"""Full body translations for languages that previously shipped as English stubs.

Only keys present here overwrite English; changelog entries intentionally stay English
(technical notes for players who already read English Workshop notes).
"""
from __future__ import print_function

# Built at import time by build_stub_full() so the file stays maintainable.
STUB_FULL = {}


def _merge(lang_dicts):
    out = {}
    for lang, d in lang_dicts.items():
        out[lang] = dict(d)
    return out


# Common UI / settings strings translated for each stub language.
# Format: key -> {lang: text}
_COMMON = {
    "MOD_DESCRIPTION": {
        "da": "Forbedret offentlig transport: linjekontrol, flåde, integrationer og mere.",
        "fi": "Parannettu joukkoliikenne: linjanhallinta, kalusto, integraatiot ja muuta.",
        "no": "Forbedret kollektivtrafikk: linjekontroll, flåte, integrasjoner og mer.",
        "sv": "Förbättrad kollektivtrafik: linjekontroll, flotta, integrationer och mer.",
        "hu": "Továbbfejlesztett tömegközlekedés: vonalvezérlés, flotta, integrációk és egyebek.",
        "ro": "Transport public îmbunătățit: control linii, flotă, integrări și altele.",
        "bg": "Подобрен градски транспорт: контрол на линии, автопарк, интеграции и още.",
        "el": "Βελτιωμένες δημόσιες συγκοινωνίες: έλεγχος γραμμών, στόλος, ενσωματώσεις και άλλα.",
        "uk": "Покращений громадський транспорт: керування лініями, парк, інтеграції та інше.",
        "vi": "Giao thông công cộng cải tiến: điều khiển tuyến, đội xe, tích hợp và hơn thế.",
        "ms": "Pengangkutan awam diperbaiki: kawalan laluan, armada, integrasi dan lagi.",
    },
    "SETTINGS_TAB_GENERAL": {
        "da": "Generelt", "fi": "Yleiset", "no": "Generelt", "sv": "Allmänt",
        "hu": "Általános", "ro": "General", "bg": "Общи", "el": "Γενικά",
        "uk": "Загальні", "vi": "Chung", "ms": "Umum",
    },
    "SETTINGS_TAB_AUTOLINE": {
        "da": "Auto-linje", "fi": "Automaattilinja", "no": "Auto-linje", "sv": "Autolinje",
        "hu": "Auto vonal", "ro": "Linie auto", "bg": "Авто линия", "el": "Αυτόματη γραμμή",
        "uk": "Автолінія", "vi": "Tuyến tự động", "ms": "Laluan auto",
    },
    "SETTINGS_TAB_STOPS": {
        "da": "Stop og stationer", "fi": "Pysäkit ja asemat", "no": "Stopp og stasjoner", "sv": "Hållplatser och stationer",
        "hu": "Megállók és állomások", "ro": "Stații și gări", "bg": "Спирки и гари", "el": "Στάσεις και σταθμοί",
        "uk": "Зупинки та станції", "vi": "Điểm dừng và ga", "ms": "Perhentian dan stesen",
    },
    "SETTINGS_TAB_UNBUNCHING": {
        "da": "Afstandsstyring", "fi": "Ruuhkautumisen estäminen", "no": "Avstandsstyring", "sv": "Avståndsstyrning",
        "hu": "Csoportosulásgátlás", "ro": "Anti-grupare", "bg": "Разстояние между превозни", "el": "Αποφυγή συσσώρευσης",
        "uk": "Розведення інтервалу", "vi": "Chống dồn xe", "ms": "Elak berumpun",
    },
    "SETTINGS_TAB_DELETE": {
        "da": "Slet linjer", "fi": "Poista linjoja", "no": "Slett linjer", "sv": "Radera linjer",
        "hu": "Vonalak törlése", "ro": "Șterge linii", "bg": "Изтрий линии", "el": "Διαγραφή γραμμών",
        "uk": "Видалити лінії", "vi": "Xóa tuyến", "ms": "Padam laluan",
    },
    "SETTINGS_TAB_FLEET": {
        "da": "Flåde og planlægning", "fi": "Kalusto ja aikataulut", "no": "Flåte og planlegging", "sv": "Flotta och schemaläggning",
        "hu": "Flotta és ütemezés", "ro": "Flotă și programare", "bg": "Автопарк и график", "el": "Στόλος και προγραμματισμός",
        "uk": "Парк і розклад", "vi": "Đội xe & lịch", "ms": "Armada & penjadualan",
    },
    "SETTINGS_TAB_BUDGET": {
        "da": "Budget og priser", "fi": "Budjetti ja hinnat", "no": "Budsjett og priser", "sv": "Budget och priser",
        "hu": "Költségvetés és árak", "ro": "Buget și prețuri", "bg": "Бюджет и цени", "el": "Προϋπολογισμός και τιμές",
        "uk": "Бюджет і ціни", "vi": "Ngân sách & giá", "ms": "Bajet & harga",
    },
    "SETTINGS_TAB_LINECOLORS": {
        "da": "Linjefarver", "fi": "Linjavärit", "no": "Linjefarger", "sv": "Linjefärger",
        "hu": "Vonalszínek", "ro": "Culori linii", "bg": "Цветове на линии", "el": "Χρώματα γραμμών",
        "uk": "Кольори ліній", "vi": "Màu tuyến", "ms": "Warna laluan",
    },
    "SETTINGS_DELETE": {
        "da": "Slet", "fi": "Poista", "no": "Slett", "sv": "Radera",
        "hu": "Törlés", "ro": "Șterge", "bg": "Изтрий", "el": "Διαγραφή",
        "uk": "Видалити", "vi": "Xóa", "ms": "Padam",
    },
    "SETTINGS_RESET": {
        "da": "Nulstil", "fi": "Nollaa", "no": "Tilbakestill", "sv": "Återställ",
        "hu": "Visszaállítás", "ro": "Resetează", "bg": "Нулирай", "el": "Επαναφορά",
        "uk": "Скинути", "vi": "Đặt lại", "ms": "Set semula",
    },
    "SETTINGS_GAMEPLAY_PROFILE": {
        "da": "Gameplay-profil", "fi": "Pelaamisprofiili", "no": "Spillprofil", "sv": "Spelläge-profil",
        "hu": "Játékmenet-profil", "ro": "Profil de joc", "bg": "Игров профил", "el": "Προφίλ παιχνιδιού",
        "uk": "Ігровий профіль", "vi": "Hồ sơ gameplay", "ms": "Profil permainan",
    },
    "SETTINGS_GAMEPLAY_PROFILE_TOOLTIP": {
        "da": "Anvender en pakke indstillinger på én gang. Sikker (standard) lader alt være slået fra for maksimal kompatibilitet med andre mods. Vanilla matcher basisspillet. Anbefalet aktiverer kun IPT-kernen (budget flådestyring, afstandsstyring, intercity-kontakt, underbygning-faner, unstucker). Realistisk aktiverer de fleste absorberede integrationer. Brugerdefineret kaskaderer aldrig - du styrer hver kontakt selv.",
        "fi": "Soveltaa asetuserän kerralla. Turvallinen (oletus) jättää kaiken pois maksimaalisen yhteensopivuuden vuoksi. Vanilla vastaa peruspeliä. Suositeltu ottaa käyttöön vain IPT-ytimen. Realistinen ottaa useimmat integraatiot käyttöön. Mukautettu ei kaskadoi - hallitset jokaisen kytkimen itse.",
        "no": "Bruker en pakke innstillinger på én gang. Sikker (standard) lar alt være av for maksimal kompatibilitet med andre mods. Vanilla matcher basisspillet. Anbefalt aktiverer bare IPT-kjernen. Realistisk aktiverer de fleste integrasjoner. Egendefinert kaskaderer aldri - du styrer hver bryter selv.",
        "sv": "Tillämpar en uppsättning inställningar på en gång. Säker (standard) lämnar allt av för maximal kompatibilitet. Vanilla matchar basspelet. Rekommenderad aktiverar bara IPT-kärnan. Realistisk aktiverar de flesta integrationer. Anpassad kaskaderar aldrig - du styr varje reglage själv.",
        "hu": "Egyszerre alkalmaz egy beállítás-csomagot. Biztonságos (alapértelmezett) mindent kikapcsol a maximális kompatibilitásért. Vanilla a sima játékot követi. Ajánlott csak az IPT magot kapcsolja be. Realisztikus a legtöbb integrációt. Egyéni soha nem kaszkádol - minden kapcsolót te kezelsz.",
        "ro": "Aplică un lot de setări dintr-o dată. Sigur (implicit) lasă totul oprit pentru compatibilitate maximă. Vanilla potrivește jocul de bază. Recomandat activează doar nucleul IPT. Realist activează majoritatea integrărilor. Personalizat nu cascadă niciodată - tu gestionezi fiecare comutator.",
        "bg": "Прилага пакет настройки наведнъж. Безопасен (по подразбиране) оставя всичко изключено за максимална съвместимост. Vanilla следва основната игра. Препоръчан включва само ядрото IPT. Реалистичен включва повечето интеграции. По избор никога не каскадира - вие управлявате всеки превключвател.",
        "el": "Εφαρμόζει πακέτο ρυθμίσεων μονομιάς. Ασφαλές (προεπιλογή) αφήνει όλα off για μέγιστη συμβατότητα. Vanilla ταιριάζει στο βασικό παιχνίδι. Προτεινόμενο ενεργοποιεί μόνο τον πυρήνα IPT. Ρεαλιστικό ενεργοποιεί τις περισσότερες ενσωματώσεις. Προσαρμοσμένο δεν κάνει cascade - εσείς διαχειρίζεστε κάθε διακόπτη.",
        "uk": "Застосовує набір параметрів одразу. Безпечний (типово) лишає все вимкненим для максимальної сумісності. Vanilla відповідає базовій грі. Рекомендований вмикає лише ядро IPT. Реалістичний вмикає більшість інтеграцій. Користувацький ніколи не каскадує — кожен перемикач ви налаштовуєте самі.",
        "vi": "Áp một gói cài đặt cùng lúc. An toàn (mặc định) tắt hết để tương thích tối đa với mod khác. Vanilla giống game gốc. Khuyên dùng bật lõi IPT. Thực tế bật hầu hết tích hợp. Tùy chỉnh không tự áp hàng loạt — bạn tự bật từng mục.",
        "ms": "Menggunakan sekumpulan tetapan sekali gus. Selamat (lalai) biarkan semua dimatikan untuk keserasian maksimum. Vanilla sepadan permainan asas. Disyorkan hanya menghidupkan teras IPT. Realistik menghidupkan kebanyakan integrasi. Tersuai tidak pernah berjujukan - anda urus setiap suis sendiri.",
    },
    "SETTINGS_GAMEPLAY_PROFILE_CUSTOM": {
        "da": "Brugerdefineret", "fi": "Mukautettu", "no": "Egendefinert", "sv": "Anpassad",
        "hu": "Egyéni", "ro": "Personalizat", "bg": "По избор", "el": "Προσαρμοσμένο",
        "uk": "Користувацький", "vi": "Tùy chỉnh", "ms": "Tersuai",
    },
    "SETTINGS_GAMEPLAY_PROFILE_SAFE": {
        "da": "Sikker (alt fra)", "fi": "Turvallinen (kaikki pois)", "no": "Sikker (alt av)", "sv": "Säker (allt av)",
        "hu": "Biztonságos (minden ki)", "ro": "Sigur (totul oprit)", "bg": "Безопасен (всичко изкл.)", "el": "Ασφαλές (όλα off)",
        "uk": "Безпечний (усе вимкнено)", "vi": "An toàn (tắt hết)", "ms": "Selamat (semua dimatikan)",
    },
    "SETTINGS_GAMEPLAY_PROFILE_VANILLA": {
        "da": "Vanilla", "fi": "Vanilla", "no": "Vanilla", "sv": "Vanilla",
        "hu": "Vanilla", "ro": "Vanilla", "bg": "Vanilla", "el": "Vanilla",
        "uk": "Vanilla", "vi": "Vanilla", "ms": "Vanilla",
    },
    "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED": {
        "da": "Anbefalet (IPT-kerne)", "fi": "Suositeltu (IPT-ydin)", "no": "Anbefalt (IPT-kjerne)", "sv": "Rekommenderad (IPT-kärna)",
        "hu": "Ajánlott (IPT mag)", "ro": "Recomandat (nucleu IPT)", "bg": "Препоръчан (ядро IPT)", "el": "Προτεινόμενο (πυρήνας IPT)",
        "uk": "Рекомендований (ядро IPT)", "vi": "Khuyên dùng (lõi IPT)", "ms": "Disyorkan (teras IPT)",
    },
    "SETTINGS_GAMEPLAY_PROFILE_REALISTIC": {
        "da": "Realistisk", "fi": "Realistinen", "no": "Realistisk", "sv": "Realistisk",
        "hu": "Realisztikus", "ro": "Realist", "bg": "Реалистичен", "el": "Ρεαλιστικό",
        "uk": "Реалістичний", "vi": "Thực tế", "ms": "Realistik",
    },
    "SETTINGS_INTEGRATIONS_GROUP": {
        "da": "Integrerede tilføjelser", "fi": "Integroidut lisäosat", "no": "Integrerte tillegg", "sv": "Integrerade tillägg",
        "hu": "Beépített kiegészítők", "ro": "Suplimente integrate", "bg": "Интегрирани добавки", "el": "Ενσωματωμένα πρόσθετα",
        "uk": "Вбудовані доповнення", "vi": "Tiện ích tích hợp", "ms": "Add-on bersepadu",
    },
    "SETTINGS_PTU_GROUP": {
        "da": "Sidder fast", "fi": "Jumissa olevat", "no": "Sitter fast", "sv": "Fastnade",
        "hu": "Beragadt", "ro": "Blocate", "bg": "Заседнали", "el": "Κολλημένα",
        "uk": "Застряглі", "vi": "Bị kẹt", "ms": "Tersangkut",
    },
    "SETTINGS_PTU_ENABLE": {
        "da": "Fjern fastsiddende passagerer", "fi": "Poista jumissa olevat matkustajat", "no": "Fjern fastsittende passasjerer", "sv": "Ta bort fastnade passagerare",
        "hu": "Beragadt utasok eltávolítása", "ro": "Elimină pasagerii blocați", "bg": "Премахни заседнали пътници", "el": "Αφαίρεση κολλημένων επιβατών",
        "uk": "Прибрати застряглих пасажирів", "vi": "Xóa hành khách bị kẹt", "ms": "Buang penumpang tersangkut",
    },
    "SETTINGS_SPEED": {
        "da": "Vis hastighed i:", "fi": "Näytä nopeus:", "no": "Vis hastighet i:", "sv": "Visa hastighet i:",
        "hu": "Sebesség megjelenítése:", "ro": "Afișează viteza în:", "bg": "Показвай скорост в:", "el": "Εμφάνιση ταχύτητας σε:",
        "uk": "Показувати швидкість у:", "vi": "Hiện tốc độ bằng:", "ms": "Tunjuk kelajuan dalam:",
    },
    "SETTINGS_SPEED_KPH": {
        "da": "km/t", "fi": "km/h", "no": "km/t", "sv": "km/h",
        "hu": "km/h", "ro": "km/h", "bg": "км/ч", "el": "χλμ/ώρα",
        "uk": "км/год", "vi": "km/h", "ms": "km/j",
    },
    "SETTINGS_SPEED_MPH": {
        "da": "mph", "fi": "mph", "no": "mph", "sv": "mph",
        "hu": "mph", "ro": "mph", "bg": "mph", "el": "mph",
        "uk": "mph", "vi": "mph", "ms": "mph",
    },
    "SETTINGS_BBSP_MODE_DISABLED": {
        "da": "Deaktiveret", "fi": "Pois käytöstä", "no": "Deaktivert", "sv": "Inaktiverad",
        "hu": "Kikapcsolva", "ro": "Dezactivat", "bg": "Изключено", "el": "Ανενεργό",
        "uk": "Вимкнено", "vi": "Tắt", "ms": "Dilumpuhkan",
    },
    "SETTINGS_BBSP_MODE_ORIGINAL": {
        "da": "Aktiveret", "fi": "Käytössä", "no": "Aktivert", "sv": "Aktiverad",
        "hu": "Bekapcsolva", "ro": "Activat", "bg": "Включено", "el": "Ενεργό",
        "uk": "Увімкнено", "vi": "Bật", "ms": "Didayakan",
    },
    "SETTINGS_BUDGET_CONTROL_DISABLED": {
        "da": "Deaktiveret", "fi": "Pois käytöstä", "no": "Deaktivert", "sv": "Inaktiverad",
        "hu": "Kikapcsolva", "ro": "Dezactivat", "bg": "Изключено", "el": "Ανενεργό",
        "uk": "Вимкнено", "vi": "Tắt", "ms": "Dilumpuhkan",
    },
    "SETTINGS_BUDGET_CONTROL_ENABLED": {
        "da": "Aktiveret", "fi": "Käytössä", "no": "Aktivert", "sv": "Aktiverad",
        "hu": "Bekapcsolva", "ro": "Activat", "bg": "Включено", "el": "Ενεργό",
        "uk": "Увімкнено", "vi": "Bật", "ms": "Didayakan",
    },
    "SETTINGS_AUTO_LINE_BUDGET_DISABLED": {
        "da": "Deaktiveret", "fi": "Pois käytöstä", "no": "Deaktivert", "sv": "Inaktiverad",
        "hu": "Kikapcsolva", "ro": "Dezactivat", "bg": "Изключено", "el": "Ανενεργό",
        "uk": "Вимкнено", "vi": "Tắt", "ms": "Dilumpuhkan",
    },
    "SETTINGS_AUTO_LINE_BUDGET_ENABLED": {
        "da": "Aktiveret", "fi": "Käytössä", "no": "Aktivert", "sv": "Aktiverad",
        "hu": "Bekapcsolva", "ro": "Activat", "bg": "Включено", "el": "Ενεργό",
        "uk": "Увімкнено", "vi": "Bật", "ms": "Didayakan",
    },
    "SETTINGS_BUDGET_TICKET_PRICES_DISABLED": {
        "da": "Deaktiveret", "fi": "Pois käytöstä", "no": "Deaktivert", "sv": "Inaktiverad",
        "hu": "Kikapcsolva", "ro": "Dezactivat", "bg": "Изключено", "el": "Ανενεργό",
        "uk": "Вимкнено", "vi": "Tắt", "ms": "Dilumpuhkan",
    },
    "SETTINGS_BUDGET_TICKET_PRICES_ENABLED": {
        "da": "Aktiveret", "fi": "Käytössä", "no": "Aktivert", "sv": "Aktiverad",
        "hu": "Bekapcsolva", "ro": "Activat", "bg": "Включено", "el": "Ενεργό",
        "uk": "Увімкнено", "vi": "Bật", "ms": "Didayakan",
    },
    "SETTINGS_DEPOT_CAPACITY_DISABLED": {
        "da": "Deaktiveret (ubegrænset)", "fi": "Pois käytöstä (rajoittamaton)", "no": "Deaktivert (ubegrenset)", "sv": "Inaktiverad (obegränsad)",
        "hu": "Kikapcsolva (korlátlan)", "ro": "Dezactivat (fără plafon)", "bg": "Изключено (без лимит)", "el": "Ανενεργό (χωρίς όριο)",
        "uk": "Вимкнено (без ліміту)", "vi": "Tắt (không giới hạn)", "ms": "Dilumpuhkan (tiada had)",
    },
    "SETTINGS_DEPOT_CAPACITY_INTERMEDIATE": {
        "da": "Mellem", "fi": "Keskitaso", "no": "Middels", "sv": "Mellan",
        "hu": "Közepes", "ro": "Intermediar", "bg": "Междинен", "el": "Ενδιάμεσο",
        "uk": "Проміжний", "vi": "Trung bình", "ms": "Sederhana",
    },
    "SETTINGS_DEPOT_CAPACITY_REALISTIC": {
        "da": "Realistisk", "fi": "Realistinen", "no": "Realistisk", "sv": "Realistisk",
        "hu": "Realisztikus", "ro": "Realist", "bg": "Реалистичен", "el": "Ρεαλιστικό",
        "uk": "Реалістичний", "vi": "Thực tế", "ms": "Realistik",
    },
    "SETTINGS_TRAINDISPLAY_ENABLE": {
        "da": "Aktivér togvise", "fi": "Ota junanäyttö käyttöön", "no": "Aktiver togvisning", "sv": "Aktivera tågvisning",
        "hu": "Vonatkijelző bekapcsolása", "ro": "Activează afișajul trenului", "bg": "Включи дисплей за влак", "el": "Ενεργοποίηση οθόνης τρένου",
        "uk": "Увімкнути дисплей поїзда", "vi": "Bật hiển thị tàu", "ms": "Dayakan paparan kereta api",
    },
    "SETTINGS_TRAINDISPLAY_MODE_ENABLED": {
        "da": "Aktiveret", "fi": "Käytössä", "no": "Aktivert", "sv": "Aktiverad",
        "hu": "Bekapcsolva", "ro": "Activat", "bg": "Включено", "el": "Ενεργό",
        "uk": "Увімкнено", "vi": "Bật", "ms": "Didayakan",
    },
    "SETTINGS_TRAINDISPLAY_THEME_BLUE": {
        "da": "Blå", "fi": "Sininen", "no": "Blå", "sv": "Blå",
        "hu": "Kék", "ro": "Albastru", "bg": "Синьо", "el": "Μπλε",
        "uk": "Синій", "vi": "Xanh dương", "ms": "Biru",
    },
    "SETTINGS_TRAINDISPLAY_THEME_GREEN": {
        "da": "Grøn", "fi": "Vihreä", "no": "Grønn", "sv": "Grön",
        "hu": "Zöld", "ro": "Verde", "bg": "Зелено", "el": "Πράσινο",
        "uk": "Зелений", "vi": "Xanh lá", "ms": "Hijau",
    },
    "SETTINGS_TRAINDISPLAY_THEME_AMBER": {
        "da": "Rav", "fi": "Meripihka", "no": "Rav", "sv": "Bärnsten",
        "hu": "Borostyán", "ro": "Chihlimbar", "bg": "Кехлибар", "el": "Κεχριμπάρι",
        "uk": "Бурштиновий", "vi": "Hổ phách", "ms": "Amber",
    },
    "SETTINGS_WALKING_SPEED_MODE_VANILLA": {
        "da": "Standard", "fi": "Vakio", "no": "Standard", "sv": "Standard",
        "hu": "Alap", "ro": "Standard", "bg": "Стандарт", "el": "Τυπικό",
        "uk": "Стандарт", "vi": "Tiêu chuẩn", "ms": "Standard",
    },
    "SETTINGS_WALKING_SPEED_MODE_REALISTIC": {
        "da": "Realistisk", "fi": "Realistinen", "no": "Realistisk", "sv": "Realistisk",
        "hu": "Realisztikus", "ro": "Realist", "bg": "Реалистичен", "el": "Ρεαλιστικό",
        "uk": "Реалістичний", "vi": "Thực tế", "ms": "Realistik",
    },
    "SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM": {
        "da": "Nederst", "fi": "Alhaalla", "no": "Nederst", "sv": "Nederst",
        "hu": "Alul", "ro": "Jos", "bg": "Долу", "el": "Κάτω",
        "uk": "Внизу", "vi": "Dưới", "ms": "Bawah",
    },
    "SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT": {
        "da": "Højre", "fi": "Oikealla", "no": "Høyre", "sv": "Höger",
        "hu": "Jobb", "ro": "Dreapta", "bg": "Дясно", "el": "Δεξιά",
        "uk": "Праворуч", "vi": "Phải", "ms": "Kanan",
    },
    "TRAINDISPLAY_LABEL_NAME": {
        "da": "Navn", "fi": "Nimi", "no": "Navn", "sv": "Namn",
        "hu": "Név", "ro": "Nume", "bg": "Име", "el": "Όνομα",
        "uk": "Назва", "vi": "Tên", "ms": "Nama",
    },
    "TRAINDISPLAY_LABEL_STATUS": {
        "da": "Status", "fi": "Tila", "no": "Status", "sv": "Status",
        "hu": "Állapot", "ro": "Stare", "bg": "Състояние", "el": "Κατάσταση",
        "uk": "Стан", "vi": "Trạng thái", "ms": "Status",
    },
    "TRAINDISPLAY_NO_LINE": {
        "da": "Ingen linje", "fi": "Ei linjaa", "no": "Ingen linje", "sv": "Ingen linje",
        "hu": "Nincs vonal", "ro": "Nicio linie", "bg": "Няма линия", "el": "Χωρίς γραμμή",
        "uk": "Немає лінії", "vi": "Không có tuyến", "ms": "Tiada laluan",
    },
    "TRAINDISPLAY_NO_DESTINATION": {
        "da": "Ingen destination", "fi": "Ei määränpäätä", "no": "Ingen destinasjon", "sv": "Ingen destination",
        "hu": "Nincs cél", "ro": "Nicio destinație", "bg": "Няма цел", "el": "Χωρίς προορισμό",
        "uk": "Немає пункту призначення", "vi": "Không điểm đến", "ms": "Tiada destinasi",
    },
    "TRAINDISPLAY_HIDDEN": {
        "da": "Skjult", "fi": "Piilotettu", "no": "Skjult", "sv": "Dold",
        "hu": "Rejtett", "ro": "Ascuns", "bg": "Скрито", "el": "Κρυφό",
        "uk": "Приховано", "vi": "Ẩn", "ms": "Tersembunyi",
    },
    "TRAINDISPLAY_VEHICLE": {
        "da": "Køretøj", "fi": "Ajoneuvo", "no": "Kjøretøy", "sv": "Fordon",
        "hu": "Jármű", "ro": "Vehicul", "bg": "Превозно средство", "el": "Όχημα",
        "uk": "Транспорт", "vi": "Phương tiện", "ms": "Kenderaan",
    },
    "TRAINDISPLAY_STATE_RETURNING": {
        "da": "Returnerer", "fi": "Palaa", "no": "Returnerer", "sv": "Återvänder",
        "hu": "Visszatér", "ro": "Revine", "bg": "Връща се", "el": "Επιστρέφει",
        "uk": "Повертається", "vi": "Đang về", "ms": "Pulang",
    },
    "TRAINDISPLAY_STATE_STOPPED": {
        "da": "Ved stop", "fi": "Pysäkillä", "no": "Ved stopp", "sv": "Vid hållplats",
        "hu": "Megállónál", "ro": "La stație", "bg": "На спирка", "el": "Στη στάση",
        "uk": "На зупинці", "vi": "Tại điểm dừng", "ms": "Di perhentian",
    },
    "TRAINDISPLAY_STATE_EN_ROUTE": {
        "da": "Undervejs", "fi": "Matkalla", "no": "Underveis", "sv": "På väg",
        "hu": "Úton", "ro": "În traseu", "bg": "По маршрута", "el": "Σε διαδρομή",
        "uk": "У дорозі", "vi": "Đang đi", "ms": "Dalam perjalanan",
    },
    "TRAINDISPLAY_STATE_ON_LINE": {
        "da": "På linjen", "fi": "Linjalla", "no": "På linjen", "sv": "På linjen",
        "hu": "Vonalon", "ro": "Pe linie", "bg": "На линията", "el": "Στη γραμμή",
        "uk": "На лінії", "vi": "Trên tuyến", "ms": "Di laluan",
    },
    "TRAINDISPLAY_STATE_IDLE": {
        "da": "Inaktiv", "fi": "Jouten", "no": "Inaktiv", "sv": "Inaktiv",
        "hu": "Üresjárat", "ro": "Inactiv", "bg": "Неактивен", "el": "Αδρανές",
        "uk": "Простій", "vi": "Nhàn rỗi", "ms": "Melahu",
    },
}


def build_stub_full():
    langs = ["da", "fi", "no", "sv", "hu", "ro", "bg", "el", "uk", "vi", "ms"]
    out = {lang: {} for lang in langs}
    for key, per_lang in _COMMON.items():
        for lang, text in per_lang.items():
            if lang in out:
                out[lang][key] = text
    return out


STUB_FULL = build_stub_full()
