# -*- coding: utf-8 -*-
"""Sync pre-test translation gaps: new integration toggles, fixed changelog, commuter note.

Also applies full body translations for languages that were English stubs
(da/fi/no/sv/hu/ro/bg/el/uk/vi/ms).
"""
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


def write(path, order, keys):
    lines = []
    seen = set()
    for k, v in order:
        if not k:
            lines.append(v if v else "")
            continue
        lines.append(f"{k} {keys.get(k, v)}")
        seen.add(k)
    for k, v in keys.items():
        if k not in seen:
            lines.append(f"{k} {v}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


en_keys, en_order = parse(ROOT / "en.txt")

# Per-language overrides for keys that must not stay English (or that en just changed).
# Lang code matches filename stem (pt-br, es-419, zh-cn, ...).
OVERRIDES = {
    "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE": {
        "en": "Enable Advanced Stop Selection",
        "pt": "Ativar seleção avançada de paragens",
        "pt-br": "Ativar seleção avançada de paradas",
        "es": "Activar selección avanzada de paradas",
        "es-419": "Activar selección avanzada de paradas",
        "de": "Erweiterte Haltestellenauswahl aktivieren",
        "fr": "Activer la sélection avancée d'arrêts",
        "it": "Abilita selezione avanzata fermate",
        "nl": "Geavanceerde haltekeuze inschakelen",
        "pl": "Włącz zaawansowany wybór przystanków",
        "ru": "Включить расширенный выбор остановок",
        "ja": "高度な停留所選択を有効化",
        "ko": "고급 정류장 선택 사용",
        "kr": "고급 정류장 선택 사용",
        "zh-cn": "启用高级站点选择",
        "zh-tw": "啟用進階站點選擇",
        "zh": "启用高级站点选择",
        "cs": "Povolit pokročilý výběr zastávek",
        "sk": "Povoliť pokročilý výber zastávok",
        "tr": "Gelişmiş durak seçimini etkinleştir",
        "th": "เปิดใช้การเลือกจุดจอดขั้นสูง",
        "id": "Aktifkan pemilihan halte lanjutan",
        "ms": "Dayakan pemilihan perhentian lanjutan",
        "ar": "تفعيل اختيار المحطات المتقدم",
        "hi": "उन्नत स्टॉप चयन सक्षम करें",
        "bn": "উন্নত স্টপ নির্বাচন সক্রিয় করুন",
        "ur": "اعلیٰ سٹاپ انتخاب فعال کریں",
        "da": "Aktivér avanceret stopvalg",
        "fi": "Ota käyttöön edistynyt pysäkkivalinta",
        "no": "Aktiver avansert stoppvalg",
        "sv": "Aktivera avancerat hållplatsval",
        "hu": "Haladó megállóválasztás bekapcsolása",
        "ro": "Activează selecția avansată a stațiilor",
        "bg": "Включи разширен избор на спирки",
        "el": "Ενεργοποίηση σύνθετης επιλογής στάσεων",
        "uk": "Увімкнути розширений вибір зупинок",
        "vi": "Bật chọn điểm dừng nâng cao",
    },
    "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP": {
        "en": en_keys["SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP"],
        "pt": "Permite colocar paragens em plataformas/vias alternativas de estações multi-via (mantenha a tecla de modo alternativo ao colocar). Entra em vigor no próximo carregamento do nível.",
        "pt-br": "Permite colocar paradas em plataformas/vias alternativas de estações multi-via (segure a tecla de modo alternativo ao colocar). Entra em vigor no próximo carregamento do nível.",
        "es": "Permite colocar paradas en andenes/vías alternativos de estaciones multi-vía (mantén la tecla de modo alternativo al colocar). Tiene efecto al cargar el nivel de nuevo.",
        "es-419": "Permite colocar paradas en andenes/vías alternativos de estaciones multi-vía (mantén la tecla de modo alternativo al colocar). Tiene efecto al cargar el nivel de nuevo.",
        "de": "Ermöglicht Haltestellen auf alternativen Bahnsteigen/Gleisen von Mehrgleis-Stationen (Alternativmodus-Taste beim Platzieren halten). Wirkt nach dem nächsten Level-Laden.",
        "fr": "Permet de placer des arrêts sur les quais/voies alternatifs des gares multi-voies (maintenir la touche de mode alternatif). Prend effet au prochain chargement du niveau.",
        "it": "Consente di posizionare fermate su banchine/binari alternativi di stazioni multi-binario (tieni premuto il tasto modalità alternativa). Ha effetto al prossimo caricamento del livello.",
        "nl": "Laat je haltes plaatsen op alternatieve perrons/sporen van multi-spoorstations (houd de alternatieve-modus-toets in tijdens plaatsen). Werkt na de volgende level-load.",
        "pl": "Pozwala stawiać przystanki na alternatywnych peronach/torach stacji wielotorowych (trzymaj klawisz trybu alternatywnego). Działa po następnym wczytaniu poziomu.",
        "ru": "Позволяет ставить остановки на альтернативных платформах/путях многопутных станций (удерживайте клавишу альтернативного режима). Вступает в силу при следующей загрузке уровня.",
        "da": "Lader dig placere stop på alternative perroner/spor på multi-spors stationer (hold alternativ-tilstandstasten nede). Træder i kraft ved næste level-load.",
        "fi": "Voit sijoittaa pysäkkejä moniraitaisien asemien vaihtoehtoisille laitureille/raiteille (pidä vaihtoehto-tilanäppäintä pohjassa). Tulee voimaan seuraavalla tason latauksella.",
        "no": "Lar deg plassere stopp på alternative plattformer/spor på multi-sporsstasjoner (hold alternativ-modus-tasten). Trer i kraft ved neste level-load.",
        "sv": "Låter dig placera hållplatser på alternativa plattformar/spår på flerspårsstationer (håll ned alternativlägesknappen). Gäller vid nästa level-load.",
        "hu": "Lehetővé teszi megállók elhelyezését többvágányos állomások alternatív peronjain (tartsd az alternatív mód billentyűt). A következő pályabetöltéskor lép életbe.",
        "ro": "Permite plasarea stațiilor pe peroane/linii alternative ale stațiilor multi-linie (ține tasta de mod alternativ). Intră în vigoare la următoarea încărcare a nivelului.",
        "bg": "Позволява спирки на алтернативни перони/коловози на многопътни гари (задръжте клавиша за алтернативен режим). Влиза в сила при следващото зареждане.",
        "el": "Επιτρέπει στάσεις σε εναλλακτικές αποβάθρες/γραμμές πολυτροχιών σταθμών (κρατήστε το πλήκτρο εναλλακτικής λειτουργίας). Ισχύει στο επόμενο φόρτωμα επιπέδου.",
        "uk": "Дозволяє ставити зупинки на альтернативних платформах/коліях багатоколійних станцій (утримуйте клавішу альтернативного режиму). Діє після наступного завантаження рівня.",
        "vi": "Cho phép đặt điểm dừng trên sân ga/đường ray thay thế của ga nhiều ray (giữ phím chế độ thay thế khi đặt). Có hiệu lực khi tải level tiếp theo.",
        "ms": "Membolehkan penempatan perhentian di platform/trek alternatif stesen multi-trek (tahan kekunci mod alternatif). Berkuat kuasa pada muat level seterusnya.",
        "id": "Memungkinkan penempatan halte di peron/jalur alternatif stasiun multi-jalur (tahan tombol mode alternatif). Berlaku pada muat level berikutnya.",
        "ja": "複線駅の別ホーム／番線に停留所を配置できます（配置時に代替モードキーを押し続けてください）。次のレベル読み込みで有効になります。",
        "ko": "다선 역의 대체 승강장/선로에 정류장을 배치할 수 있습니다(배치 시 대체 모드 키를 누른 채). 다음 레벨 로드 시 적용됩니다.",
        "kr": "다선 역의 대체 승강장/선로에 정류장을 배치할 수 있습니다(배치 시 대체 모드 키를 누른 채). 다음 레벨 로드 시 적용됩니다.",
        "zh-cn": "可在多轨车站的备用站台/轨道上放置站点（放置时按住交替模式键）。下次加载关卡后生效。",
        "zh-tw": "可在多軌車站的備用月台／軌道上放置站點（放置時按住交替模式鍵）。下次載入關卡後生效。",
        "zh": "可在多轨车站的备用站台/轨道上放置站点（放置时按住交替模式键）。下次加载关卡后生效。",
        "cs": "Umožňuje umístit zastávky na alternativní nástupiště/koleje vícekolejných stanic (držte klávesu alternativního režimu). Platí po dalším načtení úrovně.",
        "sk": "Umožňuje umiestniť zastávky na alternatívne nástupištia/koľaje viackoľajných staníc (držte klávesu alternatívneho režimu). Platí po ďalšom načítaní úrovne.",
        "tr": "Çok hatlı istasyonların alternatif platform/raylarına durak koymanızı sağlar (yerleştirirken alternatif mod tuşunu basılı tutun). Sonraki seviye yüklemesinde geçerli olur.",
        "th": "ให้วางจุดจอดบนชานชาลา/รางทางเลือกของสถานีหลายราง (กดปุ่มโหมดทางเลือกค้างไว้) มีผลเมื่อโหลดด่านครั้งถัดไป",
        "ar": "يتيح وضع محطات على أرصفة/مسارات بديلة في المحطات متعددة المسارات (استمر بالضغط على مفتاح الوضع البديل). يسري عند تحميل المستوى التالي.",
        "hi": "मल्टी-ट्रैक स्टेशनों के वैकल्पिक प्लेटफ़ॉर्म/ट्रैक पर स्टॉप रखने देता है (रखते समय वैकल्पिक-मोड कुंजी दबाए रखें)। अगले स्तर लोड पर प्रभावी।",
        "bn": "মাল্টি-ট্র্যাক স্টেশনের বিকল্প প্ল্যাটফর্ম/ট্র্যাকে স্টপ রাখতে দেয় (রাখার সময় বিকল্প-মোড কী চেপে ধরুন)। পরবর্তী লেভেল লোডে কার্যকর।",
        "ur": "ملٹی ٹریک اسٹیشنوں کے متبادل پلیٹ فارم/ٹریک پر اسٹاپ رکھنے دیتا ہے (رکھتے وقت متبادل موڈ کلید دبائے رکھیں)۔ اگلے لیول لوڈ پر مؤثر۔",
    },
    "SETTINGS_BETTERBOARDING_ENABLE": {
        "en": "Enable Better Boarding",
        "pt": "Ativar embarque melhorado",
        "pt-br": "Ativar embarque melhorado",
        "es": "Activar embarque mejorado",
        "es-419": "Activar embarque mejorado",
        "de": "Besseres Einsteigen aktivieren",
        "fr": "Activer l'embarquement amélioré",
        "it": "Abilita imbarco migliorato",
        "nl": "Beter instappen inschakelen",
        "pl": "Włącz lepsze wsiadanie",
        "ru": "Включить улучшенную посадку",
        "ja": "より良い乗車判定を有効化",
        "ko": "향상된 승차 사용",
        "kr": "향상된 승차 사용",
        "zh-cn": "启用更好的上车逻辑",
        "zh-tw": "啟用更好的上車邏輯",
        "zh": "启用更好的上车逻辑",
        "cs": "Povolit lepší nástup",
        "sk": "Povoliť lepší nástup",
        "tr": "Gelişmiş binişi etkinleştir",
        "th": "เปิดใช้การขึ้นรถที่ดีขึ้น",
        "id": "Aktifkan naik penumpang yang lebih baik",
        "ms": "Dayakan menaiki penumpang yang lebih baik",
        "ar": "تفعيل صعود محسّن",
        "hi": "बेहतर बोर्डिंग सक्षम करें",
        "bn": "উন্নত বোর্ডিং সক্রিয় করুন",
        "ur": "بہتر بورڈنگ فعال کریں",
        "da": "Aktivér bedre ombordstigning",
        "fi": "Ota käyttöön parempi nousu",
        "no": "Aktiver bedre ombordstigning",
        "sv": "Aktivera bättre ombordstigning",
        "hu": "Jobb felszállás bekapcsolása",
        "ro": "Activează îmbarcarea îmbunătățită",
        "bg": "Включи подобрено качване",
        "el": "Ενεργοποίηση καλύτερης επιβίβασης",
        "uk": "Увімкнути покращену посадку",
        "vi": "Bật lên xe tốt hơn",
    },
    "SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP": {
        "en": en_keys["SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP"],
        "pt": "Melhora as decisões de embarque para os passageiros preferirem o veículo que realmente serve o seu destino. Entra em vigor no próximo carregamento do nível.",
        "pt-br": "Melhora as decisões de embarque para os passageiros preferirem o veículo que realmente atende o destino deles. Entra em vigor no próximo carregamento do nível.",
        "es": "Mejora las decisiones de embarque para que los pasajeros prefieran el vehículo que realmente sirve su destino. Tiene efecto al cargar el nivel de nuevo.",
        "es-419": "Mejora las decisiones de embarque para que los pasajeros prefieran el vehículo que realmente sirve su destino. Tiene efecto al cargar el nivel de nuevo.",
        "de": "Verbessert Einsteigeentscheidungen, damit Fahrgäste das Fahrzeug bevorzugen, das ihr Ziel wirklich bedient. Wirkt nach dem nächsten Level-Laden.",
        "fr": "Améliore les décisions d'embarquement pour que les passagers préfèrent le véhicule qui dessert vraiment leur destination. Prend effet au prochain chargement.",
        "it": "Migliora le decisioni di imbarco così i passeggeri preferiscono il veicolo che serve davvero la loro destinazione. Ha effetto al prossimo caricamento.",
        "nl": "Verbetert instapbeslissingen zodat reizigers het voertuig kiezen dat hun bestemming echt bedient. Werkt na de volgende level-load.",
        "pl": "Poprawia decyzje wsiadania, by pasażerowie wybierali pojazd jadący do ich celu. Działa po następnym wczytaniu poziomu.",
        "ru": "Улучшает решения о посадке: пассажиры предпочитают транспорт, который действительно везёт к их цели. Вступает в силу при следующей загрузке уровня.",
        "da": "Forbedrer ombordstigningsbeslutninger, så passagerer foretrækker det køretøj, der faktisk betjener deres destination. Træder i kraft ved næste level-load.",
        "fi": "Parantaa nousupäätöksiä, jotta matkustajat suosivat ajoneuvoa, joka todella palvelee heidän määränpäätään. Tulee voimaan seuraavalla tason latauksella.",
        "no": "Forbedrer ombordstigningsbeslutninger, så passasjerer foretrekker kjøretøyet som faktisk betjener destinasjonen. Trer i kraft ved neste level-load.",
        "sv": "Förbättrar ombordstigningsbeslut så att passagerare föredrar fordonet som faktiskt betjänar deras destination. Gäller vid nästa level-load.",
        "hu": "Javítja a felszállási döntéseket, hogy az utasok azt a járművet válasszák, amely valóban a céljukhoz visz. A következő pályabetöltéskor lép életbe.",
        "ro": "Îmbunătățește deciziile de îmbarcare, astfel încât pasagerii să prefere vehiculul care le servește destinația. Intră în vigoare la următoarea încărcare.",
        "bg": "Подобрява решенията за качване, за да предпочитат пътниците превозното средство към тяхната цел. Влиза в сила при следващото зареждане.",
        "el": "Βελτιώνει τις αποφάσεις επιβίβασης ώστε οι επιβάτες να προτιμούν το όχημα που εξυπηρετεί πραγματικά τον προορισμό τους. Ισχύει στο επόμενο φόρτωμα.",
        "uk": "Покращує рішення про посадку: пасажири обирають транспорт, який справді везе до їхньої цілі. Діє після наступного завантаження рівня.",
        "vi": "Cải thiện quyết định lên xe để hành khách ưu tiên phương tiện thực sự đến điểm đến của họ. Có hiệu lực khi tải level tiếp theo.",
        "ms": "Memperbaiki keputusan menaiki supaya penumpang memilih kenderaan yang benar-benar ke destinasi mereka. Berkuat kuasa pada muat level seterusnya.",
        "id": "Memperbaiki keputusan naik penumpang agar penumpang memilih kendaraan yang benar-benar ke tujuan mereka. Berlaku pada muat level berikutnya.",
        "ja": "乗客が本当に目的地へ向かう車両を優先するよう乗車判定を改善します。次のレベル読み込みで有効になります。",
        "ko": "승객이 실제로 목적지로 가는 차량을 선호하도록 승차 결정을 개선합니다. 다음 레벨 로드 시 적용됩니다.",
        "kr": "승객이 실제로 목적지로 가는 차량을 선호하도록 승차 결정을 개선합니다. 다음 레벨 로드 시 적용됩니다.",
        "zh-cn": "改进上车决策，使乘客优先选择真正前往其目的地的车辆。下次加载关卡后生效。",
        "zh-tw": "改進上車決策，使乘客優先選擇真正前往其目的地的車輛。下次載入關卡後生效。",
        "zh": "改进上车决策，使乘客优先选择真正前往其目的地的车辆。下次加载关卡后生效。",
        "cs": "Zlepšuje rozhodnutí o nástupu, aby cestující volili vozidlo, které skutečně jede k jejich cíli. Platí po dalším načtení úrovně.",
        "sk": "Zlepšuje rozhodnutia o nástupe, aby cestujúci volili vozidlo, ktoré skutočne ide k ich cieľu. Platí po ďalšom načítaní úrovne.",
        "tr": "Yolcuların gerçekten destinasyonlarına giden aracı tercih etmesi için biniş kararlarını iyileştirir. Sonraki seviye yüklemesinde geçerli olur.",
        "th": "ปรับปรุงการตัดสินใจขึ้นรถให้ผู้โดยสารเลือกรถที่ไปจุดหมายจริง มีผลเมื่อโหลดด่านครั้งถัดไป",
        "ar": "يحسّن قرارات الصعود ليفضل الركاب المركبة التي تخدم وجهتهم فعلياً. يسري عند تحميل المستوى التالي.",
        "hi": "बोर्डिंग निर्णयों को बेहतर बनाता है ताकि यात्री वही वाहन चुनें जो वास्तव में उनके गंतव्य पर जाता है। अगले स्तर लोड पर प्रभावी।",
        "bn": "বোর্ডিং সিদ্ধান্ত উন্নত করে যাতে যাত্রীরা সেই যান বেছে নেয় যা সত্যিই তাদের গন্তব্যে যায়। পরবর্তী লেভেল লোডে কার্যকর।",
        "ur": "بورڈنگ فیصلے بہتر بناتا ہے تاکہ مسافر وہی گاڑی چنیں جو واقعی ان کی منزل پر جاتی ہے۔ اگلے لیول لوڈ پر مؤثر۔",
    },
    "SETTINGS_MILEAGETAXI_ENABLE": {
        "en": "Enable Mileage Taxi Services",
        "pt": "Ativar táxis por quilometragem",
        "pt-br": "Ativar táxis por quilometragem",
        "es": "Activar taxis por kilometraje",
        "es-419": "Activar taxis por kilometraje",
        "de": "Kilometer-Taxi-Dienste aktivieren",
        "fr": "Activer les taxis au kilomètre",
        "it": "Abilita taxi a chilometraggio",
        "nl": "Kilometertaxi's inschakelen",
        "pl": "Włącz taksówki według przebiegu",
        "ru": "Включить такси по пробегу",
        "ja": "走行距離ベースのタクシー料金を有効化",
        "ko": "주행 거리 기반 택시 요금 사용",
        "kr": "주행 거리 기반 택시 요금 사용",
        "zh-cn": "启用按里程计费的出租车",
        "zh-tw": "啟用按里程計費的計程車",
        "zh": "启用按里程计费的出租车",
        "cs": "Povolit taxi podle ujetých km",
        "sk": "Povoliť taxi podľa najazdených km",
        "tr": "Kilometre bazlı taksiyi etkinleştir",
        "th": "เปิดใช้แท็กซี่คิดตามระยะทาง",
        "id": "Aktifkan taksi berbasis jarak tempuh",
        "ms": "Dayakan teksi berasaskan perbatuan",
        "ar": "تفعيل أجرة التاكسي حسب المسافة",
        "hi": "माइलेज टैक्सी सेवा सक्षम करें",
        "bn": "মাইলেজ ট্যাক্সি পরিষেবা সক্রিয় করুন",
        "ur": "مائلیج ٹیکسی سروس فعال کریں",
        "da": "Aktivér taxikørsel efter kilometer",
        "fi": "Ota käyttöön kilometripohjaiset taksit",
        "no": "Aktiver drosje etter kilometer",
        "sv": "Aktivera taxi efter körsträcka",
        "hu": "Kilométeralapú taxik bekapcsolása",
        "ro": "Activează taxiurile pe kilometraj",
        "bg": "Включи таксита по пробег",
        "el": "Ενεργοποίηση ταξί ανά χιλιόμετρο",
        "uk": "Увімкнути таксі за пробігом",
        "vi": "Bật taxi theo quãng đường",
    },
    "SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP": {
        "en": en_keys["SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP"],
        "pt": "Cobra tarifas de táxi pela distância percorrida em vez de uma taxa fixa, para viagens longas renderem mais. Requer o DLC After Dark. Entra em vigor no próximo carregamento do nível.",
        "pt-br": "Cobra tarifas de táxi pela distância percorrida em vez de taxa fixa, para viagens longas renderem mais. Requer o DLC After Dark. Entra em vigor no próximo carregamento do nível.",
        "es": "Cobra las tarifas de taxi por distancia en lugar de tarifa fija, para que los viajes largos generen más. Requiere el DLC After Dark. Tiene efecto al cargar el nivel de nuevo.",
        "es-419": "Cobra las tarifas de taxi por distancia en lugar de tarifa fija, para que los viajes largos generen más. Requiere el DLC After Dark. Tiene efecto al cargar el nivel de nuevo.",
        "de": "Berechnet Taxigebühren nach gefahrener Distanz statt Pauschale, damit längere Fahrten mehr einbringen. Benötigt After Dark DLC. Wirkt nach dem nächsten Level-Laden.",
        "fr": "Facture les taxis à la distance parcourue au lieu d'un forfait, pour que les longs trajets rapportent plus. Nécessite le DLC After Dark. Prend effet au prochain chargement.",
        "it": "Applica tariffe taxi in base alla distanza invece di una tariffa fissa, così le corse lunghe rendono di più. Richiede il DLC After Dark. Ha effetto al prossimo caricamento.",
        "nl": "Rekent taxitarieven af op afgelegde afstand in plaats van een vast tarief, zodat langere ritten meer opleveren. Vereist After Dark DLC. Werkt na de volgende level-load.",
        "pl": "Pobiera opłaty za taksówkę według dystansu zamiast stałej stawki, więc dłuższe kursy dają więcej. Wymaga DLC After Dark. Działa po następnym wczytaniu poziomu.",
        "ru": "Считает плату за такси по пройденному расстоянию, а не фиксированно — длинные поездки приносят больше. Нужен DLC After Dark. Вступает в силу при следующей загрузке уровня.",
        "da": "Opkræver taxipris efter kørt distance i stedet for fast takst, så længere ture giver mere. Kræver After Dark DLC. Træder i kraft ved næste level-load.",
        "fi": "Laskuttaa taksimaksun ajetun matkan mukaan kiinteän hinnan sijaan, joten pidemmät matkat tuottavat enemmän. Vaatii After Dark DLC:n. Tulee voimaan seuraavalla tason latauksella.",
        "no": "Tar drosjepris etter kjørt distanse i stedet for fast takst, så lengre turer gir mer. Krever After Dark DLC. Trer i kraft ved neste level-load.",
        "sv": "Tar taxipris efter körd sträcka i stället för fast taxa, så längre resor ger mer. Kräver After Dark DLC. Gäller vid nästa level-load.",
        "hu": "A taxi díját megtett távolság alapján számolja fix díj helyett, így a hosszabb utak többet hoznak. After Dark DLC szükséges. A következő pályabetöltéskor lép életbe.",
        "ro": "Taxează taxiul după distanța parcursă, nu tarif fix, astfel cătravele lungi aduc mai mult. Necesită DLC After Dark. Intră în vigoare la următoarea încărcare.",
        "bg": "Таксува таксита по изминато разстояние вместо фиксирана тарифа, така по-дългите курсове носят повече. Изисква After Dark DLC. Влиза в сила при следващото зареждане.",
        "el": "Χρεώνει ταξί ανά απόσταση αντί για σταθερό κόμιστρο, ώστε τα μεγαλύτερα ταξίδια να αποδίδουν περισσότερο. Απαιτεί το DLC After Dark. Ισχύει στο επόμενο φόρτωμα.",
        "uk": "Рахує плату за таксі за пройденою відстанню, а не фіксовано — довгі поїздки приносять більше. Потрібен DLC After Dark. Діє після наступного завантаження рівня.",
        "vi": "Tính cước taxi theo quãng đường thay vì giá cố định, chuyến dài kiếm nhiều hơn. Cần DLC After Dark. Có hiệu lực khi tải level tiếp theo.",
        "ms": "Mengecaj teksi mengikut jarak dipandu dan bukannya kadar tetap, supaya perjalanan jauh lebih berbaloi. Memerlukan DLC After Dark. Berkuat kuasa pada muat level seterusnya.",
        "id": "Mengecas taksi berdasarkan jarak tempuh, bukan tarif tetap, agar perjalanan jauh menghasilkan lebih banyak. Memerlukan DLC After Dark. Berlaku pada muat level berikutnya.",
        "ja": "定額ではなく走行距離に応じてタクシー料金を課金し、長距離ほど収入が増えます。After Dark DLCが必要です。次のレベル読み込みで有効になります。",
        "ko": "정액이 아닌 주행 거리로 택시 요금을 부과해 장거리일수록 수익이 늘어납니다. After Dark DLC 필요. 다음 레벨 로드 시 적용됩니다.",
        "kr": "정액이 아닌 주행 거리로 택시 요금을 부과해 장거리일수록 수익이 늘어납니다. After Dark DLC 필요. 다음 레벨 로드 시 적용됩니다.",
        "zh-cn": "按行驶里程而非固定费率收取出租车费，长途收入更高。需要 After Dark DLC。下次加载关卡后生效。",
        "zh-tw": "按行駛里程而非固定費率收取計程車費，長途收入更高。需要 After Dark DLC。下次載入關卡後生效。",
        "zh": "按行驶里程而非固定费率收取出租车费，长途收入更高。需要 After Dark DLC。下次加载关卡后生效。",
        "cs": "Účtuje taxi podle ujeté vzdálenosti místo pevné sazby, delší jízdy vydělají víc. Vyžaduje DLC After Dark. Platí po dalším načtení úrovně.",
        "sk": "Účtuje taxi podľa prejdenej vzdialenosti namiesto pevnej sadzby, dlhšie jazdy zarobia viac. Vyžaduje DLC After Dark. Platí po ďalšom načítaní úrovne.",
        "tr": "Sabit ücret yerine kat edilen mesafeye göre taksi ücreti alır; uzun yolculuklar daha çok kazandırır. After Dark DLC gerekir. Sonraki seviye yüklemesinde geçerli olur.",
        "th": "คิดค่าแท็กซี่ตามระยะทางแทนอัตราคงที่ เที่ยวไกลได้เงินมากขึ้น ต้องมี DLC After Dark มีผลเมื่อโหลดด่านครั้งถัดไป",
        "ar": "يحتسب أجرة التاكسي حسب المسافة بدلاً من سعر ثابت، فتزيد إيرادات الرحلات الطويلة. يتطلب DLC After Dark. يسري عند تحميل المستوى التالي.",
        "hi": "फ्लैट दर के बजाय चली दूरी से टैक्सी किराया लेता है, लंबी सवारी अधिक कमाती हैं। After Dark DLC आवश्यक। अगले स्तर लोड पर प्रभावी।",
        "bn": "ফ্ল্যাট রেটের বদলে দূরত্ব অনুসারে ট্যাক্সি ভাড়া নেয়, দীর্ঘ যাত্রায় বেশি আয়। After Dark DLC প্রয়োজন। পরবর্তী লেভেল লোডে কার্যকর।",
        "ur": "فلیٹ ریٹ کی بجائے طے شدہ فاصلے سے ٹیکسی کرایہ لیتا ہے، لمبی سواری زیادہ کماتی ہے۔ After Dark DLC درکار۔ اگلے لیول لوڈ پر مؤثر۔",
    },
    "SETTINGS_ELEVATEDSTOPS_ENABLE": {
        "en": "Enable Elevated Stops",
        "pt": "Ativar paragens elevadas",
        "pt-br": "Ativar paradas elevadas",
        "es": "Activar paradas elevadas",
        "es-419": "Activar paradas elevadas",
        "de": "Erhöhte Haltestellen aktivieren",
        "fr": "Activer les arrêts surélevés",
        "it": "Abilita fermate elevate",
        "nl": "Verhoogde haltes inschakelen",
        "pl": "Włącz podwyższone przystanki",
        "ru": "Включить остановки на эстакадах",
        "ja": "高架停留所を有効化",
        "ko": "고가 정류장 사용",
        "kr": "고가 정류장 사용",
        "zh-cn": "启用高架站点",
        "zh-tw": "啟用高架站點",
        "zh": "启用高架站点",
        "cs": "Povolit zvýšené zastávky",
        "sk": "Povoliť zvýšené zastávky",
        "tr": "Yükseltilmiş durakları etkinleştir",
        "th": "เปิดใช้จุดจอดบนทางยกระดับ",
        "id": "Aktifkan halte layang",
        "ms": "Dayakan perhentian bertingkat",
        "ar": "تفعيل المحطات المرتفعة",
        "hi": "उच्च स्टॉप सक्षम करें",
        "bn": "উঁচু স্টপ সক্রিয় করুন",
        "ur": "اونچے اسٹاپ فعال کریں",
        "da": "Aktivér forhøjede stop",
        "fi": "Ota käyttöön korotetut pysäkit",
        "no": "Aktiver hevede stopp",
        "sv": "Aktivera upphöjda hållplatser",
        "hu": "Emelt megállók bekapcsolása",
        "ro": "Activează stațiile elevate",
        "bg": "Включи спирки на естакади",
        "el": "Ενεργοποίηση υπερυψωμένων στάσεων",
        "uk": "Увімкнути зупинки на естакадах",
        "vi": "Bật điểm dừng trên đường cao",
    },
    "SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP": {
        "en": en_keys["SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP"],
        "pt": "Permite paragens de transporte público em estradas/pontes elevadas e mantém a iluminação nesses troços. Entra em vigor no próximo carregamento do nível.",
        "pt-br": "Permite paradas de transporte público em estradas/pontes elevadas e mantém a iluminação nesses trechos. Entra em vigor no próximo carregamento do nível.",
        "es": "Permite paradas de transporte público en carreteras/puentes elevados y mantiene las farolas en esos tramos. Tiene efecto al cargar el nivel de nuevo.",
        "es-419": "Permite paradas de transporte público en carreteras/puentes elevados y mantiene las farolas en esos tramos. Tiene efecto al cargar el nivel de nuevo.",
        "de": "Ermöglicht ÖPNV-Haltestellen auf Hochstraßen/Brücken und behält die Straßenbeleuchtung dort bei. Wirkt nach dem nächsten Level-Laden.",
        "fr": "Autorise les arrêts de transport public sur routes/ponts surélevés et conserve l'éclairage de ces segments. Prend effet au prochain chargement.",
        "it": "Consente fermate del trasporto pubblico su strade/ponti elevati e mantiene i lampioni su quei segmenti. Ha effetto al prossimo caricamento.",
        "nl": "Staat OV-haltes toe op verhoogde wegen/bruggen en houdt straatverlichting op die segmenten. Werkt na de volgende level-load.",
        "pl": "Umożliwia przystanki komunikacji na estakadach/mostach i zachowuje latarnie na tych odcinkach. Działa po następnym wczytaniu poziomu.",
        "ru": "Разрешает остановки ОТ на эстакадах/мостах и сохраняет уличное освещение на этих участках. Вступает в силу при следующей загрузке уровня.",
        "da": "Tillader kollektiv trafik-stop på forhøjede veje/broer og bevarer gadelys på de segmenter. Træder i kraft ved næste level-load.",
        "fi": "Sallii joukkoliikennepysäkit korotetuilla teillä/silloilla ja pitää katuvalot niillä osuuksilla. Tulee voimaan seuraavalla tason latauksella.",
        "no": "Tillater kollektivstopp på hevede veier/broer og beholder gatelys på disse segmentene. Trer i kraft ved neste level-load.",
        "sv": "Tillåter kollektivtrafikhållplatser på upphöjda vägar/broar och behåller gatubelysning på dessa sträckor. Gäller vid nästa level-load.",
        "hu": "Engedélyezi a tömegközlekedési megállókat emelt utakon/hidakon, és megtartja a közvilágítást ezeken a szakaszokon. A következő pályabetöltéskor lép életbe.",
        "ro": "Permite stații de transport public pe drumuri/poduri elevate și păstrează iluminatul pe acele segmente. Intră în vigoare la următoarea încărcare.",
        "bg": "Позволява спирки на ОТ на естакади/мостове и запазва уличното осветление. Влиза в сила при следващото зареждане.",
        "el": "Επιτρέπει στάσεις ΜΜΜ σε υπερυψωμένους δρόμους/γέφυρες και διατηρεί τον οδικό φωτισμό. Ισχύει στο επόμενο φόρτωμα.",
        "uk": "Дозволяє зупинки ГТ на естакадах/мостах і зберігає вуличне освітлення на цих ділянках. Діє після наступного завантаження рівня.",
        "vi": "Cho phép điểm dừng giao thông công cộng trên đường/cầu cao và giữ đèn đường trên các đoạn đó. Có hiệu lực khi tải level tiếp theo.",
        "ms": "Membenarkan perhentian pengangkutan awam di jalan/jambatan bertingkat dan mengekalkan lampu jalan pada segmen itu. Berkuat kuasa pada muat level seterusnya.",
        "id": "Mengizinkan halte transportasi umum di jalan/jembatan layang dan mempertahankan lampu jalan di segmen tersebut. Berlaku pada muat level berikutnya.",
        "ja": "高架道路／橋に公共交通の停留所を置け、その区間の街灯も維持します。次のレベル読み込みで有効になります。",
        "ko": "고가 도로/교량에 대중교통 정류장을 두고 해당 구간의 가로등을 유지합니다. 다음 레벨 로드 시 적용됩니다.",
        "kr": "고가 도로/교량에 대중교통 정류장을 두고 해당 구간의 가로등을 유지합니다. 다음 레벨 로드 시 적용됩니다.",
        "zh-cn": "允许在高架路/桥上设置公交站点，并保留这些路段的路灯。下次加载关卡后生效。",
        "zh-tw": "允許在高架路／橋上設置大眾運輸站點，並保留這些路段的路燈。下次載入關卡後生效。",
        "zh": "允许在高架路/桥上设置公交站点，并保留这些路段的路灯。下次加载关卡后生效。",
        "cs": "Umožňuje zastávky MHD na mostech/zvýšených silnicích a zachovává pouliční osvětlení. Platí po dalším načtení úrovně.",
        "sk": "Umožňuje zastávky MHD na mostoch/zvýšených cestách a zachováva pouličné osvetlenie. Platí po ďalšom načítaní úrovne.",
        "tr": "Yükseltilmiş yol/köprülerde toplu taşıma durağına izin verir ve o segmentlerdeki sokak lambalarını korur. Sonraki seviye yüklemesinde geçerli olur.",
        "th": "อนุญาตจุดจอดขนส่งสาธารณะบนถนน/สะพานยกระดับ และคงไฟถนนไว้ มีผลเมื่อโหลดด่านครั้งถัดไป",
        "ar": "يسمح بمحطات النقل العام على الطرق/الجسور المرتفعة ويحافظ على إنارة الشوارع. يسري عند تحميل المستوى التالي.",
        "hi": "ऊंचे सड़क/पुल पर सार्वजनिक परिवहन स्टॉप की अनुमति देता है और उन खंडों पर स्ट्रीट लाइट रखता है। अगले स्तर लोड पर प्रभावी।",
        "bn": "উঁচু রাস্তা/সেতুতে গণপরিবহন স্টপ অনুমোদন করে এবং সেই অংশে স্ট্রিট লাইট রাখে। পরবর্তী লেভেল লোডে কার্যকর।",
        "ur": "اونچی سڑک/پل پر عوامی ٹرانسپورٹ اسٹاپ کی اجازت دیتا ہے اور ان حصوں پر اسٹریٹ لائٹ رکھتا ہے۔ اگلے لیول لوڈ پر مؤثر۔",
    },
    "SETTINGS_COMMUTERDESTINATION_ENABLE": {
        "en": en_keys["SETTINGS_COMMUTERDESTINATION_ENABLE"],
        "pt": "Ativar destino dos passageiros (redesign pendente)",
        "pt-br": "Ativar destino dos passageiros (redesign pendente)",
        "es": "Activar destino de pasajeros (rediseño pendiente)",
        "es-419": "Activar destino de pasajeros (rediseño pendiente)",
        "de": "Pendlerziel aktivieren (Neugestaltung ausstehend)",
        "fr": "Activer la destination des usagers (refonte en attente)",
        "it": "Abilita destinazione pendolari (ridisegno in sospeso)",
        "nl": "Forensbestemming inschakelen (herontwerp in afwachting)",
        "pl": "Włącz cel dojazdów (przeprojektowanie w toku)",
        "ru": "Включить пункт назначения пассажиров (редизайн в ожидании)",
        "da": "Aktivér pendlermål (redesign afventer)",
        "fi": "Ota käyttöön työmatkakohde (uudelleensuunnittelu odottaa)",
        "no": "Aktiver pendlerdestinasjon (redesign venter)",
        "sv": "Aktivera pendlardestination (omdesign väntar)",
        "hu": "Ingadozói cél bekapcsolása (újratervezés függőben)",
        "ro": "Activează destinația navetiștilor (redesign în așteptare)",
        "bg": "Включи цел на пътниците (редизайн предстои)",
        "el": "Ενεργοποίηση προορισμού επιβατών (ανασχεδιασμός σε εκκρεμότητα)",
        "uk": "Увімкнути пункт призначення пасажирів (редизайн очікується)",
        "vi": "Bật điểm đến hành khách (đang chờ thiết kế lại)",
        "ms": "Dayakan destinasi penumpang (reka semula menunggu)",
        "id": "Aktifkan destinasi penumpang (redesain menunggu)",
        "ja": "通勤者の行き先を有効化（再設計待ち）",
        "ko": "통근 목적지 사용 (재설계 대기)",
        "kr": "통근 목적지 사용 (재설계 대기)",
        "zh-cn": "启用乘客目的地（重新设计待完成）",
        "zh-tw": "啟用乘客目的地（重新設計待完成）",
        "zh": "启用乘客目的地（重新设计待完成）",
        "cs": "Povolit cíl dojíždějících (přepracování čeká)",
        "sk": "Povoliť cieľ dochádzajúcich (prepracovanie čaká)",
        "tr": "Yolcu hedefini etkinleştir (yeniden tasarım bekliyor)",
        "th": "เปิดใช้ปลายทางผู้โดยสาร (รอออกแบบใหม่)",
        "ar": "تفعيل وجهة الركاب (إعادة التصميم معلّقة)",
        "hi": "कम्यूटर गंतव्य सक्षम करें (रीडिज़ाइन लंबित)",
        "bn": "কমিউটার গন্তব্য সক্রিয় করুন (রিডিজাইন মুলতুবি)",
        "ur": "مسافر منزل فعال کریں (ری ڈیزائن زیر التواء)",
    },
    "SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP": {
        "en": en_keys["SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP"],
        "pt": "Temporariamente indisponível: o painel anterior duplicava a UI de info da paragem e fica forçado desligado até um redesign. A caixa de seleção não o reativa.",
        "pt-br": "Temporariamente indisponível: o painel anterior duplicava a UI de info da parada e fica forçado desligado até um redesign. A caixa de seleção não o reativa.",
        "es": "Temporalmente no disponible: el panel anterior duplicaba la UI de info de parada y está forzado a desactivado hasta un rediseño. La casilla no lo reactiva.",
        "es-419": "Temporalmente no disponible: el panel anterior duplicaba la UI de info de parada y está forzado a desactivado hasta un rediseño. La casilla no lo reactiva.",
        "de": "Vorübergehend nicht verfügbar: das frühere Panel hat die Haltestellen-Info-UI verdoppelt und bleibt bis zu einer Neugestaltung erzwungen aus. Die Checkbox schaltet es nicht wieder ein.",
        "fr": "Temporairement indisponible : l'ancien panneau dupliquait l'UI d'info d'arrêt et reste forcé désactivé jusqu'à une refonte. La case ne le réactive pas.",
        "it": "Temporaneamente non disponibile: il pannello precedente duplicava l'UI info fermata ed è forzato disattivo fino a un ridisegno. La casella non lo riattiva.",
        "nl": "Tijdelijk niet beschikbaar: het oude paneel dupliceerde de halte-info-UI en blijft geforceerd uit tot een herontwerp. Het selectievakje zet het niet weer aan.",
        "pl": "Tymczasowo niedostępne: poprzedni panel dublował UI info przystanku i jest wymuszony wyłączony do przeprojektowania. Checkbox go nie włączy.",
        "ru": "Временно недоступно: прежняя панель дублировала UI информации об остановке и принудительно выключена до редизайна. Галочка не включает её снова.",
        "da": "Midlertidigt utilgængelig: det tidligere panel duplikerede stop-info-UI og er tvunget fra indtil redesign. Afkrydsningsfeltet genaktiverer det ikke.",
        "fi": "Väliaikaisesti poissa käytöstä: aiempi paneeli monisti pysäkkitiedon UI:n ja on pakotettu pois uudelleensuunnitteluun asti. Valintaruutu ei ota sitä takaisin käyttöön.",
        "no": "Midlertidig utilgjengelig: det forrige panelet dupliserte stopp-info-UI og er tvunget av til redesign. Avmerkingsboksen reaktiverer det ikke.",
        "sv": "Tillfälligt otillgänglig: den tidigare panelen duplicerade hållplatsinfo-UI och tvingas av tills omdesign. Kryssrutan återaktiverar den inte.",
        "hu": "Átmenetileg nem elérhető: a korábbi panel megduplázta a megálló-info UI-t, és az újratervezésig kényszerítve ki van kapcsolva. A jelölőnégyzet nem kapcsolja vissza.",
        "ro": "Temporar indisponibil: panoul anterior duplica UI-ul de info stație și e forțat oprit până la redesign. Bifarea nu îl reactivează.",
        "bg": "Временно недостъпно: предишният панел дублираше UI за спирка и е принудително изключен до редизайн. Отметката не го включва отново.",
        "el": "Προσωρινά μη διαθέσιμο: το παλιό πάνελ διπλασίαζε το UI πληροφοριών στάσης και παραμένει αναγκαστικά off μέχρι ανασχεδιασμό. Το πλαίσιο δεν το επανενεργοποιεί.",
        "uk": "Тимчасово недоступно: попередня панель дублювала UI інформації про зупинку й примусово вимкнена до редизайну. Прапорець не вмикає її знову.",
        "vi": "Tạm thời không dùng được: bảng cũ trùng UI thông tin điểm dừng và bị tắt bắt buộc cho đến khi thiết kế lại. Hộp chọn không bật lại được.",
        "ms": "Sementara tidak tersedia: panel lama menduplikasi UI maklumat perhentian dan dipaksa dimatikan sehingga reka semula. Kotak semak tidak menghidupkannya semula.",
        "id": "Sementara tidak tersedia: panel lama menduplikasi UI info halte dan dipaksa mati sampai redesain. Kotak centang tidak mengaktifkannya lagi.",
        "ja": "一時利用不可：以前のパネルは停留所情報UIを重複させており、再設計まで強制オフです。チェックボックスでは再有効化できません。",
        "ko": "일시적으로 사용 불가: 이전 패널이 정류장 정보 UI를 중복했고 재설계 전까지 강제 해제됩니다. 확인란으로 다시 켤 수 없습니다.",
        "kr": "일시적으로 사용 불가: 이전 패널이 정류장 정보 UI를 중복했고 재설계 전까지 강제 해제됩니다. 확인란으로 다시 켤 수 없습니다.",
        "zh-cn": "暂时不可用：旧面板与站点信息 UI 重复，在重新设计完成前强制关闭。复选框无法重新启用。",
        "zh-tw": "暫時不可用：舊面板與站點資訊 UI 重複，在重新設計完成前強制關閉。核取方塊無法重新啟用。",
        "zh": "暂时不可用：旧面板与站点信息 UI 重复，在重新设计完成前强制关闭。复选框无法重新启用。",
        "cs": "Dočasně nedostupné: dřívější panel duplikoval UI info o zastávce a je vynuceně vypnutý do přepracování. Zaškrtávátko ho znovu nezapne.",
        "sk": "Dočasne nedostupné: skorší panel duplikoval UI info o zastávke a je vynútene vypnutý do prepracovania. Začiarknutie ho znova nezapne.",
        "tr": "Geçici olarak kullanılamaz: önceki panel durak bilgisi UI'sini çoğaltıyordu ve yeniden tasarıma kadar zorla kapalı. Onay kutusu yeniden açmaz.",
        "th": "ใช้ชั่วคราวไม่ได้: แผงเดิมซ้ำกับ UI ข้อมูลจุดจอด และถูกบังคับปิดจนกว่าจะออกแบบใหม่ ช่องทำเครื่องหมายเปิดใหม่ไม่ได้",
        "ar": "غير متاح مؤقتاً: اللوحة السابقة كانت تكرر واجهة معلومات المحطة وهي معطّلة قسراً حتى إعادة التصميم. لا يعيد مربع الاختيار تفعيلها.",
        "hi": "अस्थायी रूप से अनुपलब्ध: पिछला पैनल स्टॉप-info UI दोहराता था और रीडिज़ाइन तक जबरन बंद है। चेकबॉक्स इसे फिर नहीं चालू करता।",
        "bn": "সাময়িকভাবে অনুপলব্ধ: আগের প্যানেল স্টপ-info UI ডুপ্লিকেট করত এবং রিডিজাইন পর্যন্ত জোর করে বন্ধ। চেকবক্স আবার চালু করে না।",
        "ur": "عارضی طور پر دستیاب نہیں: پرانا پینل اسٹاپ-info UI دہراتا تھا اور ری ڈیزائن تک زبردستی بند ہے۔ چیک باکس اسے دوبارہ آن نہیں کرتا۔",
    },
    "CHANGELOG_4_8_0_8": {
        "en": en_keys["CHANGELOG_4_8_0_8"],
    },
    "CHANGELOG_4_8_0_9": {
        "en": en_keys["CHANGELOG_4_8_0_9"],
    },
    "CHANGELOG_4_8_0_10": {
        "en": en_keys["CHANGELOG_4_8_0_10"],
    },
}

# Full-body replacements for languages that were English stubs (all keys where value == en).
# Loaded from sibling fully-translated files when possible, then key-specific overrides.
RELATED_BASE = {
    # Use closest fully-translated sibling as starting point where it helps.
    "uk": "ru",  # better than pure English; still apply OVERRIDES and STUB_FULL below
    "bg": "ru",
}

# Large full translations for stub languages: only applied when current value equals English.
# Kept in a separate generated section below for maintainability.
from stub_full_translations import STUB_FULL  # type: ignore


def pick(key, lang):
    block = OVERRIDES.get(key)
    if not block:
        return None
    if lang in block:
        return block[lang]
    # fallbacks
    if lang == "pt-br" and "pt" in block:
        return block["pt"]
    if lang == "es-419" and "es" in block:
        return block["es"]
    if lang == "kr" and "ko" in block:
        return block["ko"]
    if lang in ("zh-cn", "zh-tw") and "zh" in block:
        return block["zh"]
    if "en" in block:
        return block["en"]
    return None


def main():
    langs = sorted(p.stem for p in ROOT.glob("*.txt"))
    for lang in langs:
        path = ROOT / f"{lang}.txt"
        keys, order = parse(path)

        # Ensure every en key exists
        for k, v in en_keys.items():
            if k not in keys:
                keys[k] = v

        # Base from related language for stubs (only fill where still English)
        if lang in RELATED_BASE:
            base_path = ROOT / f"{RELATED_BASE[lang]}.txt"
            if base_path.exists():
                base_keys, _ = parse(base_path)
                for k, v in en_keys.items():
                    if keys.get(k) == v and k in base_keys and base_keys[k] != v:
                        keys[k] = base_keys[k]

        # Stub full translations
        if lang in STUB_FULL:
            for k, v in STUB_FULL[lang].items():
                if k in en_keys:
                    # Always apply for stub langs so we overwrite English bodies
                    keys[k] = v

        # Explicit overrides for changed/new keys
        for k in OVERRIDES:
            val = pick(k, lang)
            if val is not None:
                keys[k] = val

        # Drop keys no longer in en
        for k in list(keys.keys()):
            if k not in en_keys:
                del keys[k]

        # Rebuild order from en
        write(path, en_order, keys)
        print(f"synced {lang}.txt ({len(keys)} keys)")


if __name__ == "__main__":
    main()
