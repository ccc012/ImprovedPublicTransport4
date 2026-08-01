# -*- coding: utf-8 -*-
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"
KEY = "SETTINGS_GAMEPLAY_PROFILE_TOOLTIP"

UPDATES = {
    "pt": (
        "Aplica um lote de definições de uma vez. Seguro (predefinição) deixa tudo desligado "
        "para máxima compatibilidade com outros mods. Vanilla iguala o jogo base. Recomendado "
        "ativa só o núcleo IPT (controlo de frota por orçamento, unbunching, intercity, abas de "
        "sub-edifícios, unstucker, seleção avançada de paragens, paragens elevadas). Realista "
        "ativa a maioria das integrações absorvidas. Personalizado nunca cascateia - controla "
        "cada opção você mesmo."
    ),
    "pt-br": (
        "Aplica um lote de configurações de uma vez. Seguro (padrão) deixa tudo desligado "
        "para máxima compatibilidade com outros mods. Vanilla iguala o jogo base. Recomendado "
        "ativa só o núcleo IPT (controle de frota por orçamento, unbunching, intercity, abas de "
        "sub-edifícios, unstucker, seleção avançada de paradas, paradas elevadas). Realista "
        "ativa a maioria das integrações absorvidas. Personalizado nunca cascateia - você "
        "controla cada opção."
    ),
    "es": (
        "Aplica un lote de ajustes de una vez. Seguro (predeterminado) lo deja todo desactivado "
        "para máxima compatibilidad con otros mods. Vanilla coincide con el juego base. "
        "Recomendado activa solo el núcleo IPT (control de flota por presupuesto, unbunching, "
        "intercity, pestañas de subedificios, unstucker, selección avanzada de paradas, paradas "
        "elevadas). Realista activa la mayoría de integraciones. Personalizado nunca cascada: "
        "tú gestionas cada interruptor."
    ),
    "es-419": (
        "Aplica un lote de ajustes de una vez. Seguro (predeterminado) lo deja todo desactivado "
        "para máxima compatibilidad con otros mods. Vanilla coincide con el juego base. "
        "Recomendado activa solo el núcleo IPT (control de flota por presupuesto, unbunching, "
        "intercity, pestañas de subedificios, unstucker, selección avanzada de paradas, paradas "
        "elevadas). Realista activa la mayoría de integraciones. Personalizado nunca cascada: "
        "tú gestionas cada interruptor."
    ),
    "de": (
        "Wendet mehrere Einstellungen auf einmal an. Sicher (Standard) lässt alles aus für "
        "maximale Kompatibilität mit anderen Mods. Vanilla entspricht dem Basisspiel. Empfohlen "
        "aktiviert nur den IPT-Kern (Budget-Flottensteuerung, Unbunching, Intercity, "
        "Untergebäude-Tabs, Unstucker, erweiterte Haltestellenauswahl, erhöhte Haltestellen). "
        "Realistisch aktiviert die meisten Integrationen. Benutzerdefiniert kaskadiert nie - "
        "du steuerst jeden Schalter selbst."
    ),
    "fr": (
        "Applique un lot de réglages d'un coup. Sûr (défaut) laisse tout désactivé pour une "
        "compatibilité maximale. Vanilla correspond au jeu de base. Recommandé active uniquement "
        "le cœur IPT (flotte par budget, unbunching, intercity, onglets sous-bâtiments, "
        "unstucker, sélection avancée d'arrêts, arrêts surélevés). Réaliste active la plupart "
        "des intégrations. Personnalisé ne cascade jamais - vous gérez chaque option."
    ),
    "it": (
        "Applica un lotto di impostazioni in una volta. Sicuro (predefinito) lascia tutto spento "
        "per massima compatibilità. Vanilla corrisponde al gioco base. Consigliato attiva solo "
        "il nucleo IPT (flotta da budget, unbunching, intercity, schede sotto-edifici, unstucker, "
        "selezione avanzata fermate, fermate elevate). Realistico attiva la maggior parte delle "
        "integrazioni. Personalizzato non cascata mai - gestisci ogni interruttore tu."
    ),
    "nl": (
        "Past een reeks instellingen in één keer toe. Veilig (standaard) laat alles uit voor "
        "maximale compatibiliteit. Vanilla komt overeen met het basisspel. Aanbevolen schakelt "
        "alleen de IPT-kern in (budget vloot, unbunching, intercity, subgebouw-tabs, unstucker, "
        "geavanceerde haltekeuze, verhoogde haltes). Realistisch schakelt de meeste integraties "
        "in. Aangepast cascaderert nooit - jij beheert elke schakelaar."
    ),
    "pl": (
        "Stosuje pakiet ustawień naraz. Bezpieczny (domyślny) zostawia wszystko wyłączone dla "
        "maksymalnej kompatybilności. Vanilla odpowiada grze podstawowej. Zalecany włącza tylko "
        "rdzeń IPT (flota z budżetu, unbunching, intercity, karty podbudynków, unstucker, "
        "zaawansowany wybór przystanków, podwyższone przystanki). Realistyczny włącza większość "
        "integracji. Własny nigdy nie kaskaduje - sam zarządzasz każdym przełącznikiem."
    ),
    "ru": (
        "Применяет пакет настроек сразу. Безопасный (по умолчанию) оставляет всё выключенным "
        "для максимальной совместимости. Vanilla соответствует базовой игре. Рекомендуемый "
        "включает только ядро IPT (флот по бюджету, unbunching, intercity, вкладки подзданий, "
        "unstucker, расширенный выбор остановок, остановки на эстакадах). Реалистичный включает "
        "большинство интеграций. Пользовательский никогда не каскадирует — вы управляете каждым "
        "переключателем."
    ),
    "ja": (
        "設定をまとめて適用します。安全（既定）は他Modとの互換性のためすべてオフ。Vanillaは本編準拠。"
        "推奨はIPTコアのみ（予算フリート、間隔制御、都市間バス、サブ建物タブ、unstucker、高度な停留所選択、高架停留所）。"
        "リアルはほとんどの統合をオン。カスタムは一括変更せず、各項目を自分で管理します。"
    ),
    "ko": (
        "설정을 한 번에 적용합니다. 안전(기본)은 다른 모드와 최대 호환을 위해 모두 끔. Vanilla는 기본 게임. "
        "권장은 IPT 코어만(예산 함대, unbunching, 시외, 하위 건물 탭, unstucker, 고급 정류장 선택, 고가 정류장). "
        "실사는 대부분 통합 켜기. 사용자 지정은 연쇄 없음 - 각 스위치를 직접 관리."
    ),
    "kr": (
        "설정을 한 번에 적용합니다. 안전(기본)은 다른 모드와 최대 호환을 위해 모두 끔. Vanilla는 기본 게임. "
        "권장은 IPT 코어만(예산 함대, unbunching, 시외, 하위 건물 탭, unstucker, 고급 정류장 선택, 고가 정류장). "
        "실사는 대부분 통합 켜기. 사용자 지정은 연쇄 없음 - 각 스위치를 직접 관리."
    ),
    "zh-cn": (
        "一次应用一组设置。安全（默认）全部关闭，以最大兼容其他模组。Vanilla 对齐原版。"
        "推荐仅开启 IPT 核心（预算车队、疏解、城际、子建筑标签、unstucker、高级站点选择、高架站点）。"
        "真实开启大部分集成。自定义从不级联——你自行管理每个开关。"
    ),
    "zh-tw": (
        "一次套用一組設定。安全（預設）全部關閉，以最大相容其他模組。Vanilla 對齊原版。"
        "推薦僅開啟 IPT 核心（預算車隊、疏解、城際、子建築分頁、unstucker、進階站點選擇、高架站點）。"
        "真實開啟大部分整合。自訂從不級聯——你自行管理每個開關。"
    ),
    "zh": (
        "一次应用一组设置。安全（默认）全部关闭，以最大兼容其他模组。Vanilla 对齐原版。"
        "推荐仅开启 IPT 核心（预算车队、疏解、城际、子建筑标签、unstucker、高级站点选择、高架站点）。"
        "真实开启大部分集成。自定义从不级联——你自行管理每个开关。"
    ),
    "da": (
        "Anvender en pakke indstillinger på én gang. Sikker (standard) lader alt være slået fra "
        "for maksimal kompatibilitet med andre mods. Vanilla matcher basisspillet. Anbefalet "
        "aktiverer kun IPT-kernen (budget flådestyring, afstandsstyring, intercity, "
        "underbygning-faner, unstucker, avanceret stopvalg, forhøjede stop). Realistisk aktiverer "
        "de fleste absorberede integrationer. Brugerdefineret kaskaderer aldrig - du styrer hver "
        "kontakt selv."
    ),
    "fi": (
        "Soveltaa asetuserän kerralla. Turvallinen (oletus) jättää kaiken pois maksimaalisen "
        "yhteensopivuuden vuoksi. Vanilla vastaa peruspeliä. Suositeltu ottaa käyttöön vain "
        "IPT-ytimen (budjettikanta, unbunching, intercity, alirakennusvälilehdet, unstucker, "
        "edistynyt pysäkkivalinta, korotetut pysäkit). Realistinen ottaa useimmat integraatiot "
        "käyttöön. Mukautettu ei kaskadoi - hallitset jokaisen kytkimen itse."
    ),
    "no": (
        "Bruker en pakke innstillinger på én gang. Sikker (standard) lar alt være av for "
        "maksimal kompatibilitet. Vanilla matcher basisspillet. Anbefalt aktiverer bare "
        "IPT-kjernen (budsjettflåte, unbunching, intercity, underbygg-faner, unstucker, "
        "avansert stoppvalg, hevede stopp). Realistisk aktiverer de fleste integrasjoner. "
        "Egendefinert kaskaderer aldri - du styrer hver bryter selv."
    ),
    "sv": (
        "Tillämpar en uppsättning inställningar på en gång. Säker (standard) lämnar allt av "
        "för maximal kompatibilitet. Vanilla matchar basspelet. Rekommenderad aktiverar bara "
        "IPT-kärnan (budgetflotta, unbunching, intercity, underbyggnadsflikar, unstucker, "
        "avancerat hållplatsval, upphöjda hållplatser). Realistisk aktiverar de flesta "
        "integrationer. Anpassad kaskaderar aldrig - du styr varje reglage själv."
    ),
    "hu": (
        "Egyszerre alkalmaz egy beállítás-csomagot. Biztonságos (alapértelmezett) mindent "
        "kikapcsol a maximális kompatibilitásért. Vanilla a sima játékot követi. Ajánlott csak "
        "az IPT magot kapcsolja be (költségvetés-flotta, unbunching, intercity, alépület-fülek, "
        "unstucker, haladó megállóválasztás, emelt megállók). Realisztikus a legtöbb "
        "integrációt. Egyéni soha nem kaszkádol - minden kapcsolót te kezelsz."
    ),
    "ro": (
        "Aplică un lot de setări dintr-o dată. Sigur (implicit) lasă totul oprit pentru "
        "compatibilitate maximă. Vanilla potrivește jocul de bază. Recomandat activează doar "
        "nucleul IPT (flotă pe buget, unbunching, intercity, file sub-clădiri, unstucker, "
        "selecție avansată stații, stații elevate). Realist activează majoritatea integrărilor. "
        "Personalizat nu cascadă niciodată - tu gestionezi fiecare comutator."
    ),
    "bg": (
        "Прилага пакет настройки наведнъж. Безопасен (по подразбиране) оставя всичко изключено "
        "за максимална съвместимост. Vanilla следва основната игра. Препоръчан включва само "
        "ядрото IPT (флот по бюджет, unbunching, intercity, раздели подсгради, unstucker, "
        "разширен избор на спирки, спирки на естакади). Реалистичен включва повечето "
        "интеграции. По избор никога не каскадира - вие управлявате всеки превключвател."
    ),
    "el": (
        "Εφαρμόζει πακέτο ρυθμίσεων μονομιάς. Ασφαλές (προεπιλογή) αφήνει όλα off για μέγιστη "
        "συμβατότητα. Vanilla ταιριάζει στο βασικό παιχνίδι. Προτεινόμενο ενεργοποιεί μόνο τον "
        "πυρήνα IPT (στόλος από budget, unbunching, intercity, καρτέλες υποκτιρίων, unstucker, "
        "σύνθετη επιλογή στάσεων, υπερυψωμένες στάσεις). Ρεαλιστικό ενεργοποιεί τις περισσότερες "
        "ενσωματώσεις. Προσαρμοσμένο δεν κάνει cascade - εσείς διαχειρίζεστε κάθε διακόπτη."
    ),
    "uk": (
        "Застосовує набір параметрів одразу. Безпечний (типово) лишає все вимкненим для "
        "максимальної сумісності. Vanilla відповідає базовій грі. Рекомендований вмикає лише "
        "ядро IPT (флот за бюджетом, unbunching, intercity, вкладки підбудівель, unstucker, "
        "розширений вибір зупинок, зупинки на естакадах). Реалістичний вмикає більшість "
        "інтеграцій. Користувацький ніколи не каскадує — кожен перемикач ви налаштовуєте самі."
    ),
    "vi": (
        "Áp một gói cài đặt cùng lúc. An toàn (mặc định) tắt hết để tương thích tối đa. "
        "Vanilla giống game gốc. Khuyên dùng bật lõi IPT (đội xe theo ngân sách, unbunching, "
        "liên tỉnh, tab công trình phụ, unstucker, chọn điểm dừng nâng cao, điểm dừng trên "
        "đường cao). Thực tế bật hầu hết tích hợp. Tùy chỉnh không tự áp hàng loạt — bạn tự "
        "bật từng mục."
    ),
    "ms": (
        "Menggunakan sekumpulan tetapan sekali gus. Selamat (lalai) biarkan semua dimatikan "
        "untuk keserasian maksimum. Vanilla sepadan permainan asas. Disyorkan hanya menghidupkan "
        "teras IPT (armada bajet, unbunching, intercity, tab sub-bangunan, unstucker, pemilihan "
        "perhentian lanjutan, perhentian bertingkat). Realistik menghidupkan kebanyakan "
        "integrasi. Tersuai tidak pernah berjujukan - anda urus setiap suis sendiri."
    ),
    "id": (
        "Menerapkan sekelompok pengaturan sekaligus. Aman (bawaan) mematikan semuanya untuk "
        "kompatibilitas maksimum. Vanilla cocok dengan game dasar. Direkomendasikan hanya "
        "mengaktifkan inti IPT (armada anggaran, unbunching, intercity, tab sub-bangunan, "
        "unstucker, pemilihan halte lanjutan, halte layang). Realistis mengaktifkan sebagian "
        "besar integrasi. Kustom tidak pernah berantai - Anda kelola setiap saklar sendiri."
    ),
    "cs": (
        "Použije sadu nastavení najednou. Bezpečný (výchozí) nechá vše vypnuté pro maximální "
        "kompatibilitu. Vanilla odpovídá základní hře. Doporučený zapne jen jádro IPT (flotila "
        "z rozpočtu, unbunching, intercity, záložky podbudov, unstucker, pokročilý výběr "
        "zastávek, zvýšené zastávky). Realistický zapne většinu integrací. Vlastní nikdy "
        "nekaskáduje - každý přepínač řídíte sami."
    ),
    "sk": (
        "Použije sadu nastavení naraz. Bezpečný (predvolený) nechá všetko vypnuté pre maximálnu "
        "kompatibilitu. Vanilla zodpovedá základnej hre. Odporúčaný zapne len jadro IPT (flotila "
        "z rozpočtu, unbunching, intercity, záložky podbudov, unstucker, pokročilý výber "
        "zastávok, zvýšené zastávky). Realistický zapne väčšinu integrácií. Vlastný nikdy "
        "nekaskáduje - každý prepínač riadite sami."
    ),
    "tr": (
        "Bir ayar paketini bir kerede uygular. Güvenli (varsayılan) maksimum uyumluluk için her "
        "şeyi kapalı bırakır. Vanilla temel oyuna uyar. Önerilen yalnızca IPT çekirdeğini açar "
        "(bütçe filosu, unbunching, intercity, alt bina sekmeleri, unstucker, gelişmiş durak "
        "seçimi, yükseltilmiş duraklar). Gerçekçi çoğu entegrasyonu açar. Özel asla kademeli "
        "olmaz - her anahtarı siz yönetirsiniz."
    ),
}


def main():
    for lang, text in UPDATES.items():
        path = ROOT / f"{lang}.txt"
        lines = path.read_text(encoding="utf-8").splitlines()
        out = []
        found = False
        for line in lines:
            if line.startswith(KEY + " "):
                out.append(KEY + " " + text)
                found = True
            else:
                out.append(line)
        if not found:
            out.append(KEY + " " + text)
        path.write_text("\n".join(out) + "\n", encoding="utf-8")
        print("updated", lang)
    print("done")


if __name__ == "__main__":
    main()
