#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script to add missing translation keys to 26 language files for IPT4 mod.
"""

import os
import re

# ============================================================
# TRANSLATIONS FOR ALL 26 LANGUAGES
# ============================================================

TRANSLATIONS = {
    # Arabic
    "ar": {
        "SETTINGS_SUPPORT_LABEL": "إذا أنقذ IPT4 حفظ لعبتك أو حسن أداءك، ففكر في دعم تطويره!",
        "SETTINGS_SUPPORT_BUTTON": "ادعمني على Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "تفريق / حافلة سريعة",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "يُمكّن تفريق المركبات، الحافلة/الترام السريع، فاصل وقت الظهور، وتحديد حجم الأسطول بقوة. جميع ميزات تباعد وميزانية الحافلات والترام.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "ميزات الميزانية",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "يُمكّن التحكم بميزانية الخط، الميزانية التلقائية للخط، مضاعف سعر التذكرة، والسعر كتكلفة مسار. جميع ميزات المال/التسعير/الأسطول.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "لون الخط التلقائي",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "يُعيّن الألوان والأسماء تلقائيًا لخطوط النقل الجديدة. يشمل استراتيجية اللون، استراتيجية التسمية، وإعدادات اللوحة.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "محرر المركبات",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "يُمكّن لوحة محرر المركبات لتغيير أنواع المركبات، الطلاء، والخصائص. يمكن وضعه في الأسفل أو اليمين.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "حدود الركاب المنتظرين",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "يحد عدد المواطنين الذين يمكنهم انتظار النقل العام في محطة واحدة. عطّل لإزالة جميع حدود الانتظار ودع اللعبة تدير سعة المحطة.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "إظهار الطبقة عند",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "اختر المركبة المحددة، كاميرا المنظور الأول، أو كلاهما.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "المركبة المحددة فقط",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "كاميرا المنظور الأول فقط",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "المركبة المحددة أو كاميرا المنظور الأول",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "تخطيط اللوحة",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "أي تخطيط تستخدمه الطبقة. كلاهما يعرض نفس المعلومات ويحترم نفس خانات الاختيار - فقط الشكل مختلف.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "معاينة للميزات المخطط لها. تظهر رمادية حتى تصبح جاهزة؛ وجهة الركاب تظل متاحة للاختبار.",
    },

    # Bulgarian
    "bg": {
        "SETTINGS_SUPPORT_LABEL": "Ако IPT4 е спасил играта ви или е подобрил перформанса, обмислете да подкрепите разработката му!",
        "SETTINGS_SUPPORT_BUTTON": "Подкрепете ме в Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Разделяне / Експресен автобус",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Включва разделяне на превозните средства, експресен автобус/трамвай, интервал на появяване и агресивно размериране на flota. Всички функции за разстояние и бюджет за автобуси и трамвае.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Бюджетни функции",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Включва контрол на бюджета на линията, автоматичен бюджет на линията, множител на цената на билета и цена като разход за път. Всички парични/ценови/флотови функции.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Автоматичен цвят на линията",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Автоматично задава цветове и имена на новите транспортни линии. Включва цветова стратегия, стратегия за именуване и настройки на панела.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Редактор на превозните средства",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Включва панела на редактора на превозните средства за промяна на типове, ливери и свойства. Може да се позиционира в долна или десна част.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Лимити на чакащите пътници",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Ограничава колко граждани могат да чакат обществения превоз на една спирка. Изключете за да премахнете всички лимити за чакане и да оставите играта да управлява капацитета на спирките.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Показване на оверлея при",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Изберете избрано превозно средство, камера от първи лице или и двете.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Само избрано превозно средство",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Само камера от първи лице",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Избрано превозно средство или камера от първи лице",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Подредба на панела",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Коя подредба използва оверлеят. И двете показват същата информация и уважават същите чекбоксове - само формата е различна.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Преглед на планираните функции. Показани в сиво до готовност; пътническа дестинация остава достъпна за тестване.",
    },

    # Bengali
    "bn": {
        "SETTINGS_SUPPORT_LABEL": "যদি IPT4 আপনার সেভগেম বাঁচায় বা পারফরম্যান্স উন্নত করে, তবে এর ডেভেলপমেন্টকে সমর্থন করার কথা ভাবুন!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi-এ আমাকে সমর্থন করুন",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "আনবঞ্চিং / এক্সপ্রেস বাস",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "গাড়ি আনবঞ্চিং, এক্সপ্রেস বাস/ট্রাম, স্পavn ইন্টারভাল এবং আক্রমণাত্মক ফ্লিট সাইজিং সক্ষম করে। বাস এবং ট্রামের সব স্পেসিং এবং বাজেট বৈশিষ্ট্য।",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "বাজেট বৈশিষ্ট্য",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "লাইন বাজেট কন্ট্রোল, অটো লাইন বাজেট, টিকিট প্রাইস মাল্টিপ্লায়ার এবং পথ খরচ হিসাবে দাম সক্ষম করে। সব টাকা/দাম/ফ্লিট বৈশিষ্ট্য।",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "স্বয়ংক্রিয় লাইন রঙ",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "নতুন পরিবহন লাইনের রঙ এবং নাম স্বয়ংক্রিয়ভাবে বরাদ্দ করে। রঙ কৌশল, নামকরণ কৌশল এবং প্যানেল সেটিংস অন্তর্ভুক্ত।",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "গাড়ি সম্পাদক",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "গাড়ির ধরন, লিভারি এবং বৈশিষ্ট্য পরিবর্তনের জন্য গাড়ি সম্পাদক প্যানেল সক্ষম করে। নিচে বা ডানে স্থাপন করা যেতে পারে।",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "অপেক্ষমাণকারী যাত্রী সীমা",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "একক স্টপে কতজন নাগরিক সর্বজনীন পরিবহনের জন্য অপেক্ষা করতে পারে সেটি সীমাবদ্ধ করে। সমস্ত অপেক্ষা সীমা সরিয়ে গেমটিকে স্টপ ক্ষমতা পরিচালনা করার জন্য অক্ষম করুন।",
        "SETTINGS_TRAINDISPLAY_SCOPE": "ওভারলে কখন দেখাবে",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "নির্বাচিত গাড়ি, ফার্স্ট-পারসন ক্যামেরা, অথবা দুটোই নির্বাচন করুন।",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "শুধু নির্বাচিত গাড়ি",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "শুধু ফার্স্ট-পারসন ক্যামেরা",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "নির্বাচিত গাড়ি বা ফার্স্ট-পারসন ক্যামেরা",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "প্যানেল লেআউট",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "ওভারলে কোন লেআউট ব্যবহার করে। দুটোই একই তথ্য দেখায় এবং একই চেকবক্স মেনে চলে - শুধু আকার ভিন্ন।",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "পরিকল্পিত বৈশিষ্ট্যগুলোর পূর্বদর্শন। প্রস্তুত না হওয়া পর্যন্ত ধূসর হয়ে থাকে; যাত্রী গন্তব্য টেস্টিং-এর জন্য উপলব্ধ থাকে।",
    },

    # Czech
    "cs": {
        "SETTINGS_SUPPORT_LABEL": "Pokud IPT4 zachránil vaši uloženou hru nebo zlepšil výkon, zvažte podporu jeho vývoje!",
        "SETTINGS_SUPPORT_BUTTON": "Podpořte mě na Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Rozbíhání / Expresní autobus",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Povolí rozbíhání vozidel, expresní autobus/tramvaj, interval spawnování a agresivní nastavování vozového parku. Všechny funkce rozestupů a rozpočtu pro autobusy a tramvaje.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Rozpočtové funkce",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Povolí kontrolu rozpočtu linky, automatický rozpočet linky, násobič ceny jízdenky a cenu jako náklady na cestu. Všechny peněžní/cenové/vozoparkové funkce.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatická barva linky",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Automaticky přiřazuje barvy a názvy novým dopravním linkám. Zahrnuje strategii barev, strategii pojmenování a nastavení panelu.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Editor vozidel",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Povolí panel editoru vozidel pro změnu typů vozidel, liverií a vlastností. Lze umístit dole nebo vpravo.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Limity čekajících cestujících",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Omezuje, kolik občanů může čekat na MHD na jedné zastávce. Zakázat pro odebrání všech limitů čekání a nechat hru spravovat kapacitu zastávek.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Zobrazit overlay při",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Vyberte vybrané vozidlo, first-person kameru nebo obojí.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Pouze vybrané vozidlo",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Pouze first-person kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Vybrané vozidlo nebo first-person kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Rozložení panelu",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Které rozložení overlay používá. Obě zobrazují stejné informace a respektují stejné zaškrtávací políčka - liší se pouze tvar.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Náhled plánovaných funkcí. Zobrazeno zašedlé dokud nejsou připravené; cesta pasažéra zůstává dostupná pro testování.",
    },

    # Danish
    "da": {
        "SETTINGS_SUPPORT_LABEL": "Hvis IPT4 har reddet din spilgemme eller forbedret din ydeevne, så overvej at støtte udviklingen!",
        "SETTINGS_SUPPORT_BUTTON": "Support mig på Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Udbunching / Ekspresbus",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Aktiverer køretøjs-udbunching, ekspresbus/tog, spawn-interval og aggressiv flådestørrelse. Alle bus/tog-afstands- og budgetfunktioner.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Budgetfunktioner",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Aktiverer linje-budgetkontrol, auto linje-budget, billetpris-multiplikator og pris som stikostnad. Alle penge/pris/flåde-funktioner.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatisk linjefarve",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Tildeler automatisk farver og navne til nye transportlinjer. Inkluderer farvestrategi, navngivningsstrategi og panelindstillinger.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Køretøjseditor",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Aktiverer køretøjseditor-panelet til at ændre køretøytyper, liverier og egenskaber. Kan placeres nederst eller til højre.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Ventende passagergrænser",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Begrænser, hvor mange borgere der kan vente på kollektivtrafik ved en enkelt stoppested. Deaktiver for at fjerne alle ventegrænser og lade spillet håndtere stopkapacitet.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Vis overlay når",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Vælg valgt køretøj, first-person kamera eller begge dele.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Kun valgt køretøj",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Kun first-person kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Valgt køretøj eller first-person kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Panel-layout",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Hvilket layout overlayet bruger. Begge viser samme information og respekterer samme afkrydsningsfelter - kun formen er forskellig.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Forhåndsvisning af planlagte funktioner. Vises gråed ud indtil de er klar; passagerdestination forbliver tilgængelig til test.",
    },

    # Greek
    "el": {
        "SETTINGS_SUPPORT_LABEL": "Αν το IPT4 έχει σώσει το savegame σας ή βελτιώσει την απόδοσή σας, σκεφτείτε να υποστηρίξετε την ανάπτυξή του!",
        "SETTINGS_SUPPORT_BUTTON": "Υποστηρίξτε με στο Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Αποσύμπληξη / Express λεωφορείο",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Ενεργοποιεί την αποσύμπληξη οχημάτων, express λεωφορείο/τραμ, διάστημα spawn και επιθετικό μέγεθος στόλου. Όλες οι λειτουργίες απόστασης/προϋπολογισμού για λεωφορεία και τραμ.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Χαρακτηριστικά προϋπολογισμού",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Ενεργοποιεί τον έλεγχο προϋπολογισμού γραμμής, αυτόματο προϋπολογισμό γραμμής, πολλαπλασιαστή τιμής εισιτηρίου και τιμή ως κόστος διαδρομής. Όλες οι λειτουργίες χρήματος/τιμολόγησης/στόλου.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Αυτόματο χρώμα γραμμής",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Αντιστοιχίζει αυτόματα χρώματα και ονόματα σε νέες γραμμές μεταφορών. Περιλαμβάνει στρατηγική χρωμάτων, στρατηγική ονοματοδοσίας και ρυθμίσεις πάνελ.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Επεξεργαστής οχημάτων",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Ενεργοποιεί το πάνελ επεξεργαστή οχημάτων για αλλαγή τύπων οχημάτων, liveries και ιδιοτήτων. Μπορεί να τοποθετηθεί στο κάτω ή δεξιό μέρος.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Όρια επιβατών που περιμένουν",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Περιορίζει πόσα άτομα μπορούν να περιμένουν τη δημόσια μεταφορά σε μια στάση. Απενεργοποιήστε για να καταργήσετε όλα τα όρια αναμονής και να αφήσετε το παιχνίδι να διαχειρίζεται τη χωρητικότητα των στασιών.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Εμφάνιση overlay όταν",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Επιλέξτε επιλεγμένο όχημα, κάμερα first-person ή και τα δύο.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Μόνο επιλεγμένο όχημα",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Μόνο κάμερα first-person",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Επιλεγμένο όχημα ή κάμερα first-person",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Διάταξη πάνελ",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Ποια διάταξη χρησιμοποιεί το overlay. Και τα δύο εμφανίζουν τις ίδιες πληροφορίες και σέβονται τα ίδια checkboxes - διαφέρει μόνο το σχήμα.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Προεπισκόπηση σχεδιαζόμενων λειτουργιών. Εμφανίζονται σβήστες μέχρι να είναι έτοιμες· η προοδός επιβατών παραμένει διαθέσιμη για δοκιμή.",
    },

    # Finnish
    "fi": {
        "SETTINGS_SUPPORT_LABEL": "Jos IPT4 on pelastanut tallennuksesi tai parantanut suorituskykyäsi, harkitse sen kehityksen tukemista!",
        "SETTINGS_SUPPORT_BUTTON": "Tue minua Ko-fi:lla",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Ryhmittely / Pikabussi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Ottaa käyttöön ajoneuvojen ryhmittelyn, pikabussin/ratikan, spawn-väliajan ja aggressiivisen kantojoukon koon. Kaikki bussi/ratikka-väliajan ja budjettitoiminnot.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Budjettitoiminnot",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Ottaa käyttöön linjan budjettien hallinnan, automaattisen linjabudjetin, lipun hinnan kerroin ja hinnan polun kustannuksena. Kaikki raha/hinta/kantojoukko-toiminnot.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automaattinen linjan väri",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Määrittää automaattisesti värit ja nimet uusille liikennelinjoille. Sisältää väristrategian, nimeämisstrategian ja paneelin asetukset.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Ajoneuvoeditori",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Ottaa käyttöön ajoneuvoeditori-paneelin ajoneuvotyyppien, liverien ja ominaisuuksien muuttamiseen. Voidaan sijoittaa alhaalle tai oikealle.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Odottavien matkustajien rajat",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Rajoittaa, kuinka monta kansalaista voi odottaa joukkoliikennettä yhdellä pysäkillä. Poista käyttööstä poistaaksesi kaikki odotuksen rajat ja antaaksesi pelin hallita pysäkin kapasiteettia.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Näytä overlay kun",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Valitse valittu ajoneuvo, first-person-kamera tai molemmat.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Vain valittu ajoneuvo",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Vain first-person-kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Valittu ajoneuvo tai first-person-kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Paneelin asettelu",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Mikä asettelu overlay käyttää. Molemmat näyttävät samat tiedot ja noudattavat samoja valintaruutuja - vain muoto eroaa.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Suunniteltujen toimintojen esikatselu. Näytetään harmaana kunnes valmiita; matkustajan kohde on edelleen testattavissa.",
    },

    # Hindi
    "hi": {
        "SETTINGS_SUPPORT_LABEL": "अगर IPT4 ने आपका सेवगेम बचाया है या आपका परफॉरमेंस सुधारा है, तो इसके डेवलपमेंट को सपोर्ट करने पर विचार करें!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi पर मुझे सपोर्ट करें",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "अनबंचिंग / एक्सप्रेस बस",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "वाहन अनबंचिंग, एक्सप्रेस बस/ट्राम, स्पॉन इंटरवल, और आक्रामक फ्लीट साइज़िंग को सक्षम करता है। बस/ट्राम के सभी स्पेसिंग और बजट फीचर।",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "बजट फीचर",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "लाइन बजट कंट्रोल, ऑटो लाइन बजट, टिकट प्राइस मल्टीप्लायर, और पथ लागत के रूप में कीमत को सक्षम करता है। सभी पैसे/कीमत/फ्लीट फीचर।",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "ऑटो लाइन कलर",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "नई परिवहन लाइनों को स्वचालित रूप से रंग और नाम असाइन करता है। कलर स्ट्रैटेजी, नेमिंग स्ट्रैटेजी, और पैनल सेटिंग्स शामिल हैं।",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "व्हीकल एडिटर",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "व्हीकल टाइप, लिवरी, और प्रॉपर्टी बदलने के लिए व्हीकल एडिटर पैनल को सक्षम करता है। इसे नीचे या दाएं रखा जा सकता है।",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "प्रतीक्षारत यात्री सीमाएं",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "सीमित करता है कि एक स्टॉप पर सार्वजनिक परिवहन के लिए कितने नागरिक प्रतीक्षा कर सकते हैं। सभी प्रतीक्षा सीमाओं को हटाने और गेम को स्टॉप क्षमता प्रबंधित करने देने के लिए अक्षम करें।",
        "SETTINGS_TRAINDISPLAY_SCOPE": "ओवरले कब दिखाएं",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "चुना हुआ वाहन, फर्स्ट-पर्सन कैमरा, या दोनों चुनें।",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "केवल चुना हुआ वाहन",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "केवल फर्स्ट-पर्सन कैमरा",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "चुना हुआ वाहन या फर्स्ट-पर्सन कैमरा",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "पैनल लेआउट",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "ओवरले किस लेआउट का उपयोग करता है। दोनों वही जानकारी दिखाते हैं और वही चेकबॉक्स मानते हैं - केवल आकार अलग है।",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "नियोजित फीचर्स का पूर्वावलोकन। तैयार होने तक ग्रे आउट दिखाया जाता है; पैसेंजर डेस्टिनेशन टेस्टिंग के लिए उपलब्ध रहता है।",
    },

    # Hungarian
    "hu": {
        "SETTINGS_SUPPORT_LABEL": "Ha az IPT4 megmentette a mentését vagy javította a teljesítményt, fontolja meg a fejlesztés támogatását!",
        "SETTINGS_SUPPORT_BUTTON": "Támogasson a Ko-fi-n",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Csoportbontás / Expresz busz",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Engedélyezi a járművek csoportbontását, az expresz busz/villamos, a spawn intervallumot és az aggresszív flotta méretezést. Az összes busz/villamos távolság- és költségvetési funkció.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Költségvetési funkciók",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Engedélyezi a vonat költségvetésének ellenőrzését, az automatikus vonat költségvetést, a jegyár szorzót és az utat költségként. Az összes pénz/ár/flotta funkció.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatikus vonatszín",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Automatikusan rendel színeket és neveket az új közlekedési vonatokhoz. Szístratégia, elnevezési stratégia és panel beállítások.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Járműszerkesztő",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Engedélyezi a járműszerkesztő panel használatát a járműtípusok, festékek és tulajdonságok módosításához. Alul vagy jobbra pozicionálható.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Várakozó utasok határai",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Korlátozza, hány polgár várhat tömegközlekedésre egy megállóban. Tiltsa le az összes várakozási határ eltávolításához, és hagyja a játéknak kezelni a megálló kapacitását.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Overlay megjelenítése ha",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Válassza ki a kiválasztott járművet, az első személyes kamerát, vagy mindkettőt.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Csak a kiválasztott jármű",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Csak az első személyes kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "A kiválasztott jármű vagy az első személyes kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Panel elrendezés",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Melyik elrendezést használja az overlay. Mindkettő ugyanazokat az információkat mutatja és ugyanezeket a bejelölőnégyzeteket tiszteleti - csak az alak különbözik.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Tervezett funkciók előnézete. Szürkén показано, amíg kész nem lesz; az utas célállomás tesztelésre elérhető marad.",
    },

    # Indonesian
    "id": {
        "SETTINGS_SUPPORT_LABEL": "Jika IPT4 telah menyelamatkan savegame Anda atau meningkatkan performa, pertimbangkan untuk mendukung pengembangannya!",
        "SETTINGS_SUPPORT_BUTTON": "Dukung saya di Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Unbunching / Bus Ekspres",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Mengaktifkan unbunching kendaraan, bus/tran ekspres, interval spawn, dan pengaturan ukuran armada agresif. Semua fitur jarak & anggaran bus/tran.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Fitur Anggaran",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Mengaktifkan kontrol anggaran jalur, anggaran jalur otomatis, pengali harga tiket, dan harga sebagai biaya jalur. Semua fitur uang/harga/armada.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Warna Jalur Otomatis",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Secara otomatis menetapkan warna dan nama ke jalur transportasi baru. Termasuk strategi warna, strategi penamaan, dan pengaturan panel.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Editor Kendaraan",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Mengaktifkan panel editor kendaraan untuk mengubah tipe kendaraan, livery, dan properti. Dapat diposisikan di bagian bawah atau kanan.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Batas Penumpang Menunggu",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Membatasi berapa warga yang bisa menunggu transportasi umum di satu halte. Nonaktifkan untuk menghapus semua batas menunggu dan biarkan game mengelola kapasitas halte.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Tampilkan overlay saat",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Pilih kendaraan terpilih, kamera first-person, atau keduanya.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Hanya kendaraan terpilih",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Hanya kamera first-person",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Kendaraan terpilih atau kamera first-person",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Tata letak panel",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Tata letak apa yang digunakan overlay. Keduanya menampilkan info yang sama dan menghormati checkbox yang sama - hanya bentuk yang berbeda.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Pratinjau fitur yang direncanakan. Ditampilkan abu-abu hingga siap; tujuan penumpang tetap tersedia untuk pengujian.",
    },

    # Japanese
    "ja": {
        "SETTINGS_SUPPORT_LABEL": "IPT4がセーブデータを救ったりパフォーマンスを改善した場合、開発支援をご検討ください！",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fiで支援する",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "アンバンチング / エクスプレスバス",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "車両のアンバンチング、エクスプレスバス/トラム、スポーン間隔、積極的なフリートサイズ調整を有効化。バス/トラムの全間隔・予算機能。",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "予算機能",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "ライン予算管理、自動ライン予算、チケット価格倍率、経路コストとしての運賃を有効化。全ての金銭/価格/フリート機能。",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "自動ライン色",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "新しい輸送路線に自動的に色と名前を割り当てます。色戦略、命名戦略、パレット設定を含みます。",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "車両エディタ",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "車両タイプ、リーバリー、プロパティ変更のための車両エディタパネルを有効化。画面下部または右側に配置可能。",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "待機乗客上限",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "1つの停留所で公共交通を待機できる市民数を制限。無効にすると全待機上限が撤廃され、ゲーム側が停留所容量を管理します。",
        "SETTINGS_TRAINDISPLAY_SCOPE": "オーバーレイ表示条件",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "選択車両、一人称視点カメラ、または両方を選択。",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "選択車両のみ",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "一人称視点カメラのみ",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "選択車両または一人称視点カメラ",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "パネルレイアウト",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "オーバーレイが使用するレイアウト。両方とも同じ情報を表示し同じチェックボックスを尊重 - 形状のみ異なる。",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "計画中機能のプレビュー。準備できるまでグレーアウト表示 — 乗客目的地はテスト用に利用可能。",
    },

    # Korean
    "ko": {
        "SETTINGS_SUPPORT_LABEL": "IPT4가 세이브게임을 살리거나 성능을 개선했다면, 개발 지원을 고려해보세요!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi에서 후원하기",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "언번칭 / 급행버스",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "차량 언번칭, 급행버스/트램, 스폰 간격, 공격적 함대 크기 조절 활성화. 모든 버스/트램 간격 및 예산 기능.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "예산 기능",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "노선 예산 통제, 자동 노선 예산, 티켓 가격 승수, 경로 비용으로서의 요금 활성화. 모든 자금/가격/함대 기능.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "자동 노선 색상",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "새로운 운송 노선에 자동으로 색상과 이름 할당. 색상 전략, 명명 전략, 패널 설정 포함.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "차량 에디터",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "차량 유형, 리버리, 속성 변경을 위한 차량 에디터 패널 활성화. 하단 또는 우측에 배치 가능.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "대기 승객 한도",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "단일 정류장에서 대중교통을 기다릴 수 있는 시민 수 제한. 비활성화 시 모든 대기 한도 제거 및 게임이 정류장 용량 관리.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "오버레이 표시 조건",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "선택된 차량, 1인칭 카메라, 또는 둘 다 선택.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "선택된 차량만",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "1인칭 카메라만",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "선택된 차량 또는 1인칭 카메라",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "패널 레이아웃",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "오버레이가 사용하는 레이아웃. 둘 다 같은 정보를 표시하고 같은 체크박스를 준수 - 모양만 다름.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "계획된 기능 미리보기. 준비될 때까지 회색 표시 — 승객 목적지는 테스트용으로 이용 가능.",
    },

    # Korean (kr - alternate)
    "kr": {
        "SETTINGS_SUPPORT_LABEL": "IPT4가 세이브게임을 살리거나 성능을 개선했다면, 개발 지원을 고려해보세요!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi에서 후원하기",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "언번칭 / 급행버스",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "차량 언번칭, 급행버스/트램, 스폰 간격, 공격적 함대 크기 조절 활성화. 모든 버스/트램 간격 및 예산 기능.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "예산 기능",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "노선 예산 통제, 자동 노선 예산, 티켓 가격 승수, 경로 비용으로서의 요금 활성화. 모든 자금/가격/함대 기능.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "자동 노선 색상",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "새로운 운송 노선에 자동으로 색상과 이름 할당. 색상 전략, 명명 전략, 패널 설정 포함.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "차량 에디터",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "차량 유형, 리버리, 속성 변경을 위한 차량 에디터 패널 활성화. 하단 또는 우측에 배치 가능.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "대기 승객 한도",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "단일 정류장에서 대중교통을 기다릴 수 있는 시민 수 제한. 비활성화 시 모든 대기 한도 제거 및 게임이 정류장 용량 관리.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "오버레이 표시 조건",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "선택된 차량, 1인칭 카메라, 또는 둘 다 선택.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "선택된 차량만",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "1인칭 카메라만",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "선택된 차량 또는 1인칭 카메라",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "패널 레이아웃",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "오버레이가 사용하는 레이아웃. 둘 다 같은 정보를 표시하고 같은 체크박스를 준수 - 모양만 다름.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "계획된 기능 미리보기. 준비될 때까지 회색 표시 — 승객 목적지는 테스트용으로 이용 가능.",
    },

    # Malay
    "ms": {
        "SETTINGS_SUPPORT_LABEL": "Jika IPT4 menyelamatkan savegame anda atau memperbaiki prestasi, pertimbangkan untuk menyokong pembangunannya!",
        "SETTINGS_SUPPORT_BUTTON": "Sokong saya di Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Unbunching / Bas Ekspres",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Mengaktifkan unbunching kenderaan, bas/tream ekspres, interval spawn, dan saizan armada agresif. Semua ciri jarak & bajet bas/tream.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Ciri Bajet",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Mengaktifkan kawalan bajet laluan, bajet laluan auto, penukar harga tiket, dan harga sebagai kos laluan. Semua ciri wang/harga/armada.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Warna Laluan Auto",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Secara automatik menetapkan warna dan nama ke laluan pengangkutan baharu. Termasuk strategi warna, strategi penamaan, dan tetapan panel.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Editor Kenderaan",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Mengaktifkan panel editor kenderaan untuk menukar jenis kenderaan, livery, dan ciri-ciri. Boleh diletakkan di bahagian bawah atau kanan.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Had Penumpang Menunggu",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Mengadkan berapa orang warga yang boleh menunggu pengangkutan awam di satu hentian. Nyahaktifkan untuk membuang semua had menunggu dan biarkan permainan menguruskan kapasiti hentian.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Tunjukkan overlay bila",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Pilih kenderaan terpilih, kamera first-person, atau kedua-duanya.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Kenderaan terpilih sahaja",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Kamera first-person sahaja",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Kenderaan terpilih atau kamera first-person",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Susun atur panel",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Susun atur mana yang overlay gunakan. Kedua-duanya memaparkan maklumat yang sama dan menghormati checkbox yang sama - hanya bentuk yang berbeza.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Pratonton ciri yang dirancang. Dipaparkan kelabu sehingga sedia; destinasi penumpang kekal tersedia untuk ujian.",
    },

    # Norwegian
    "no": {
        "SETTINGS_SUPPORT_LABEL": "Hvis IPT4 har reddet lagringsfilen din eller forbedret ytelsen, vurder å støtte utviklingen!",
        "SETTINGS_SUPPORT_BUTTON": "Støtt meg på Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Avbunting / Ekspressbuss",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Aktiverer kjøretøyavbunting, ekspressbuss/trikk, spawn-intervall og agresiv flåtestørrelse. Alle buss/trikk-avstands- og budsjettfunksjoner.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Budsjettfunksjoner",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Aktiverer linjebudsjettkontroll, auto linjebudsjett, billettpris-multiplikator og pris som rutekostnad. Alle penger/pris/flåte-funksjoner.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatisk linjefarge",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Tildeler automatisk farger og navn til nye transportlinjer. Inkluderer fargestategi, navnegivningsstrategi og panelerinnstillinger.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Kjøretøyeditor",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Aktiverer kjøretøyeditor-panelet for å endre kjøretøytyper, liverier og egenskaper. Kan plasseres nederst eller til høyre.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Ventende passasjergrenser",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Begrenser hvor mange borgere som kan vente på kollektivtransport ved en enkelt holdeplass. Deaktiver for å fjerne alle ventegrenser og la spillet håndtere holdeplaskapasitet.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Vis overlay når",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Velg valgt kjøretøy, first-person-kamera eller begge deler.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Bare valgt kjøretøy",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Bare first-person-kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Valgt kjøretøy eller first-person-kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Panel-oppsett",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Hvilket oppsett overlayet bruker. Begge viser samme informasjon og respekterer samme avhukingsbokser - bare formen er forskjellig.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Forhåndsvisning av planlagte funksjoner. Vises gråt ut til de er klare; passasjerdestinasjon forblir tilgjengelig for testing.",
    },

    # Romanian
    "ro": {
        "SETTINGS_SUPPORT_LABEL": "Dacă IPT4 v-a salvat savegame-ul sau a îmbunătățit performanța, luați în considerare să susțineți dezvoltarea sa!",
        "SETTINGS_SUPPORT_BUTTON": "Susțineți-mă pe Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Debunching / Autobuz Express",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Activează debunching-ul vehiculelor, autobuzul/tramvaiul express, intervalul de spawn și dimensionarea agresivă a flotei. Toate funcțiile de spațiere și buget pentru autobuze și tramvaie.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Funcții de buget",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Activează controlul bugetului liniei, bugetul automat al liniei, multiplicatorul prețului biletului și prețul ca cost de cale. Toate funcțiile de bani/preț/flotă.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Culoare automată linie",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Atribuie automat culori și nume noilor linii de transport. Include strategia de culoare, strategia de numire și setările panoului.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Editor vehicule",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Activează panoul editorului de vehicule pentru a schimba tipurile de vehicule, livreele și proprietățile. Poate fi poziționat în jos sau dreapta.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Limite pasageri așteptători",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Limitează câți cetățeni pot aștepta transportul public la o singură stație. Dezactivați pentru a elimina toate limitele de așteptare și a lăsa jocul să gestioneze capacitatea stațiilor.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Afișează overlay-ul când",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Selectați vehiculul selectat, camera first-person sau ambele.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Doar vehiculul selectat",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Doar camera first-person",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Vehiculul selectat sau camera first-person",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Aranjament panou",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Ce aranjament folosește overlay-ul. Ambele afișează aceleași informații și respectă aceleași casete bifate - doar forma difere.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Previzualizare a funcțiilor planificate. Afișat gri până sunt gata; destinația pasagerilor rămâne disponibilă pentru testare.",
    },

    # Russian
    "ru": {
        "SETTINGS_SUPPORT_LABEL": "Если IPT4 спас ваше сохранение или улучшил производительность, подумайте о поддержке разработки!",
        "SETTINGS_SUPPORT_BUTTON": "Поддержать на Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Разбивка скученности / Экспресс-автобус",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Включает разбивку скученности транспорта, экспресс-автобус/трамвай, интервал появления и агрессивное определение размера парка. Все функции интервалов и бюджета для автобусов и трамваев.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Бюджетные функции",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Включает контроль бюджета линии, автоматический бюджет линии, множитель цены билета и цену как стоимость пути. Все денежные/ценовые/парковые функции.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Автоматический цвет линии",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Автоматически назначает цвета и названия новым транспортным линиям. Включает стратегию цвета, стратегию именования и настройки панели.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Редактор транспорта",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Включает панель редактора транспорта для изменения типов, ливреев и свойств. Можно разместить внизу или справа.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Лимиты ожидающих пассажиров",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Ограничивает, сколько граждан может ждать общественный транспорт на одной остановке. Отключите, чтобы убрать все лимиты ожидания и позволить игре управлять вместимостью остановок.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Показывать оверлей когда",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Выберите выбранное транспортное средство, камеру от первого лица или оба варианта.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Только выбранное транспортное средство",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Только камера от первого лица",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Выбранное транспортное средство или камера от первого лица",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Макет панели",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Какой макет использует оверлей. Оба показывают одну информацию и уважают одни чекбоксы — отличается только форма.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Предварительный просмотр планируемых функций. Показано серым, пока не готово; пункт назначения пассажиров остаётся доступным для тестирования.",
    },

    # Slovak
    "sk": {
        "SETTINGS_SUPPORT_LABEL": "Ak IPT4 zachránil vašu uloženú hru alebo zlepšil výkon, zvážte podporu jeho vývoja!",
        "SETTINGS_SUPPORT_BUTTON": "Podporte ma na Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Rozbiehanie / Expresný autobus",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Povolí rozbiehanie vozidiel, expresný autobus/električku, interval spawnovania a agresívnu veľkosť vozového parku. Všechny funkcie rozestupov a rozpočtu pre autobusy a elektrické vozidlá.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Rozpočtové funkcie",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Povolí kontrolu rozpočtu linky, automatický rozpočet linky, násobič ceny lístka a cenu ako náklady na cestu. Všechny peňazné/ cenové/vozoparkové funkcie.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatická farba linky",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Automaticky priradí farby a názvy novým dopravným linkám. Zahŕna farebnú stratégiu, stratégiu menovania a nastavenia panela.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Editor vozidiel",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Povolí panel editora vozidiel na zmenu typov vozidiel, livérií a vlastností. Môže byť umiestnený dole alebo vpravo.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Limity čakajúcich cestujúcich",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Obmedzuje, koľko občanov môže čakať na MHD na jednej zastávke. Zakázať pre odstránenie všetkých limitov čakania a nechať hru spravovať kapacitu zastávok.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Zobraziť overlay pri",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Vyberte vybrané vozidlo, first-person kameru alebo obe.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Iba vybrané vozidlo",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Iba first-person kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Vybrané vozidlo alebo first-person kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Rozloženie panela",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Ktoré rozloženie overlay používa. Obidve zobrazujú rovnaké informácie a rešpektujú rovnaké zaškrtávacie políčka - líši sa len tvar.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Prehľad plánovaných funkcií. Zobrazené zašedlé kým nie sú pripravené; cieľová stanica cestujúceho zostáva dostupná na testovanie.",
    },

    # Swedish
    "sv": {
        "SETTINGS_SUPPORT_LABEL": "Om IPT4 har räddat din sparsfil eller förbättrat din prestanda, överväg att stödja utvecklingen!",
        "SETTINGS_SUPPORT_BUTTON": "Stödj mig på Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Avbunting / Expressbuss",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Aktiverar fordon-avbunting, expressbuss/spårvagn, spawn-intervall och aggressiv flottesizing. Alla buss/spårvagn-avstånds- och budgetfunktioner.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Budgetfunktioner",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Aktiverar linje-budgetkontroll, auto linje-budget, biljettpris-multiplikator och pris som vägkostnad. Alla pengar/pris/flotte-funktioner.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Automatisk linjefärg",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Tilldelar automatiskt färger och namn till nya transportlinjer. Inkluderar färgstrategi, namngivningsstrategi och panelinställningar.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Fordonredigerare",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Aktiverar fordonsredigerar-panelen för att ändra fordonstyper, liverier och egenskaper. Kan placeras längst ner eller till höger.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Väntande passagerargränser",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Begränsar hur många medborgare som kan vänta på kollektivtrafik vid en enda hållplats. Inaktivera för att ta bort alla väntande gränser och låta spelet hantera hållplatskapacitet.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Visa overlay när",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Välj valt fordon, first-person-kamera eller båda.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Endast valt fordon",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Endast first-person-kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Valt fordon eller first-person-kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Panellayout",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Vilken layout overlayen använder. Båda visar samma information och respektar samma kryssrutor - bara formen skiljer sig.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Förhandsvisning av planerade funktioner. Visas gråut tills de är klara; passagerardestination förblir tillgänglig för test.",
    },

    # Thai
    "th": {
        "SETTINGS_SUPPORT_LABEL": "หาก IPT4 ช่วยเซฟเกมของคุณหรือปรับปรุงประสิทธิภาพ โปรดพิจารณาสนับสนุนการพัฒนา!",
        "SETTINGS_SUPPORT_BUTTON": "สนับสนุนผมที่ Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "ยกเลิกการรวมกลุ่ม / รถบัสด่วน",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "เปิดใช้งานการยกเลิกการรวมกลุ่มยานพาหนะ รถบัส/รถรางด่วน ช่วงเวลาการ spawn และการปรับขนาดฝูงรถอย่างรุนแรง ฟีเจอร์ระยะห่างและงบประมาณรถบัส/รถรางทั้งหมด",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "ฟีเจอร์งบประมาณ",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "เปิดใช้งานการควบคุมงบประมาณเส้นทาง งบประมาณเส้นทางอัตโนมัติ ตัวคูณราคาตั๋ว และราคาเป็นต้นทุนเส้นทาง ฟีเจอร์เงิน/ราคา/ฝูงรถทั้งหมด",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "สีเส้นทางอัตโนมัติ",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "กำหนดสีและชื่อให้เส้นทางขนส่งใหม่อัตโนมัติ รวมถึงกลยุทธ์สี กลยุทธ์การตั้งชื่อ และการตั้งค่าแผง",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "ตัวแก้ไขยานพาหนะ",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "เปิดใช้งานแผงตัวแก้ไขยานพาหนะสำหรับเปลี่ยนประเภท ลิเวอรี่ และคุณสมบัติ สามารถวางได้ที่ด้านล่างหรือขวา",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "ขีดจำกัดผู้โดยสารรอ",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "จำกัดจำนวนพลเมืองที่รอขนส่งสาธารณะได้ที่ป้ายเดียว ปิดใช้งานเพื่อลบขีดจำกัดการรอทั้งหมดและให้เกมจัดการความจุป้าย",
        "SETTINGS_TRAINDISPLAY_SCOPE": "แสดงโอเวอร์เลย์เมื่อ",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "เลือกยานพาหนะที่เลือก กล้องมุมมองบุคคลที่หนึ่ง หรือทั้งสอง",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "เฉพาะยานพาหนะที่เลือก",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "เฉพาะกล้องมุมมองบุคคลที่หนึ่ง",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "ยานพาหนะที่เลือกหรือกล้องมุมมองบุคคลที่หนึ่ง",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "เลย์เอาต์แผง",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "เลย์เอาต์ที่โอเวอร์เลย์ใช้ ทั้งสองแสดงข้อมูลเดียวกันและเคารพชेकบ็อกซ์เดียวกัน - แตกต่างเพียงรูปร่าง",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "พรีวิวฟีเจอร์ที่วางแผนไว้ แสดงเป็นสีเทาจนกว่าจะพร้อม จุดหมายปลายทางผู้โดยสารยังคงพร้อมสำหรับทดสอบ",
    },

    # Turkish
    "tr": {
        "SETTINGS_SUPPORT_LABEL": "Eğer IPT4 oyununuzu kurtardı veya performansınızı iyileştirdiğinde, geliştirilmesini desteklemeyi düşünün!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi'da beni destekle",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Ayrıştırma / Ekspres Otobüs",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Araç ayrıştırma, ekspres otobüs/tramvay, spawn aralığı ve agresif filo boyutlandırmasını etkinleştirir. Tüm otobüs/tramvay aralık ve bütçe özellikleri.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Bütçe Özellikleri",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Hat bütçesi kontrolü, otomatik hat bütçesi, bilet fiyatı çarpanı ve yol maliyeti olarak fiyatı etkinleştirir. Tüm para/fiyat/filo özellikleri.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Otomatik Hat Rengi",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Yeni ulaşım hatlarına otomatik olarak renk ve isim atar. Renk stratejisi, isimlendirme stratejisi ve panel ayarları dahildir.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Araç Düzenleyici",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Araç türleri, livery'ler ve özellikleri değiştirmek için araç düzenleyici panelini etkinleştirir. Alt veya sağ kısımda konumlandırılabilir.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Bekleyen Yolcu Sınırları",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Tek bir durakta toplu taşıma bekleyen vatandaş sayısını sınırlar. Tüm bekleme sınırlarını kaldırmak ve oyunun durağı kapasitesini yönetmesine izin vermek için devre dışı bırakın.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Overlay ne zaman gösterilsin",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Seçili araç, first-person kamera veya her ikisini seçin.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Sadece seçili araç",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Sadece first-person kamera",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Seçili araç veya first-person kamera",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Panel düzeni",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Overlay'in hangi düzeni kullandığını belirler. Her ikisi de aynı bilgileri gösterir ve aynı onay kutularına saygı duyar - sadece şekil farklıdır.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Planlanan özelliklerin önizlemesi. Hazır olana kadar gri renkte gösterilir; yolcu hedefi test için kullanılabilir.",
    },

    # Ukrainian
    "uk": {
        "SETTINGS_SUPPORT_LABEL": "Якщо IPT4 врятував ваше збереження або покращив продуктивність, розгляньте підтримку його розробки!",
        "SETTINGS_SUPPORT_BUTTON": "Підтримати на Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Розбиття груп / Експрес-автобус",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Увімкнути розбиття груп транспортних засобів, експрес-автобус/трамвай, інтервал появи та агресивне визначення розміру автопарку. Усі функції інтервалів та бюджету для автобусів і трамваїв.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Бюджетні функції",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Увімкнути контроль бюджету лінії, автоматичний бюджет лінії, множник ціни квитка та ціну як вартість шляху. Усі грошові/цінкові/автопаркові функції.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Автоматичний колір лінії",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Автоматично призначає кольори та назви новим транспортним лініям. Включає стратегію кольору, стратегію іменування та налаштування панелі.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Редактор транспорту",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Увімкнути панель редактора транспорту для зміни типів, ліврей та властивостей. Можна розмістити внизу або справа.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Ліміти очікуючих пасажирів",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Обмежує, скільки громадян можуть чекати громадський транспорт на одній зупинці. Вимкніть, щоб прибрати всі ліміти очікування і дозволити грі керувати місткістю зупинок.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Показувати оверлей коли",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Оберіть вибраний транспортний засіб, камеру від першої особи або обидва.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Тільки вибраний транспортний засіб",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Тільки камера від першої особи",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Вибраний транспортний засіб або камера від першої особи",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Макет панелі",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Який макет використовує оверлей. Обидва показують одну інформацію і поважають одні чекбокси — відрізняється лише форма.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Попередній перегляд планованих функцій. Показано сірим, поки не готові; пункт призначення пасажира залишається доступним для тестування.",
    },

    # Urdu
    "ur": {
        "SETTINGS_SUPPORT_LABEL": "اگر IPT4 نے آپ کی سیو گیم بچائی ہے یا آپ کی کارکردگی کو بہتر بنایا ہے، تو اس کی ترقی کی حمایت کرنے کا خیال کریں!",
        "SETTINGS_SUPPORT_BUTTON": "Ko-fi پر مجھے سپورٹ کریں",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "آنبنچنگ / ایکسپریس بس",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "گاڑی آنبنچنگ، ایکسپریس بس/ٹرام، اسپان انٹروال، اور حملہ آور فلیٹ سائزنگ کو فعال کرتا ہے۔ بس/ٹرام کے تمام وقفے اور بجٹ فیچرز۔",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "بجٹ فیچرز",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "لائن بجٹ کنٹرول، آٹو لائن بجٹ، ٹکٹ پرائس ملٹیplier، اور راستے کی لاگت کے طور پر قیمت کو فعال کرتا ہے۔ تمام پیسہ/قیمت/فلیٹ فیچرز۔",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "آٹو لائن کلر",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "نئی ٹرانسپورٹ لائنز کو آپ سے کلر اور نام ترتیب دیتا ہے۔ کلر سٹریٹیجی، نیمنگ سٹریٹیجی، اور پنل سیٹنگز شامل ہیں۔",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "وہیکل ایڈیٹر",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "وہیکل ٹائپ، لवरी، اور پراپرٹیز بدلنے کے لیے وہیکل ایڈیٹر پنل کو فعال کرتا ہے۔ اسے نیچے یا دائیں رکھا جا سکتا ہے۔",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "انتظار کرنے والے مسافروں کی حد",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "محدود کرتا ہے کہ ایک اسٹاپ پر عوامی نقل و حمل کے لیے کتنے شہری انتظار کرسکتے ہیں۔ تمام انتظار کی حددیں ہٹانے اور گیم کو اسٹاپ کی صلاحیت کا انتظام کرنے دینے کے لیے غیر فعال کریں۔",
        "SETTINGS_TRAINDISPLAY_SCOPE": "اوورلے کب دکھائے",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "منتخب گاڑی،uerst-پرسون کیمرہ، یا دونوں منتخب کریں۔",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "صرف منتخب گاڑی",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "صرف اول-پرسون کیمرہ",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "منتخب گاڑی یا اول-پرسون کیمرہ",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "پینل لی آؤٹ",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "اوورلے کونسا لی آؤٹ استعمال کرتا ہے۔ دونوں وہی معلومات دکھاتے ہیں اور وہی چیک بکسز کا احترام کرتے ہیں - صرف شکل আলাদہ ہے۔",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "مخطوطہ فیچرز کا پیش نظارہ۔ تیار ہونے تک سیریہ میں دکھایا جاتا ہے؛ مسافر کی منزل ٹیسٹنگ کے لیے دستیاب رہتی ہے۔",
    },

    # Vietnamese
    "vi": {
        "SETTINGS_SUPPORT_LABEL": "Nếu IPT4 đã cứu savegame của bạn hoặc cải thiện hiệu suất, hãy cân nhắc hỗ trợ phát triển nó!",
        "SETTINGS_SUPPORT_BUTTON": "Ủng hộ tôi trên Ko-fi",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "Bóc tách / Xe buýt nhanh",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "Kích hoạt bóc tách xe, xe buýt/xe điện nhanh, khoảng thời gian spawn, và quy mô đội xe tích cực. Tất cả tính năng khoảng cách & ngân sách cho xe buýt và xe điện.",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "Tính năng ngân sách",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "Kích hoạt kiểm soát ngân sách tuyến, ngân sách tuyến tự động, nhân tử giá vé, và giá làm chi phí đường đi. Tất cả tính năng tiền/giá/đội xe.",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "Màu tuyến tự động",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "Tự động gán màu và tên cho các tuyến vận tải mới. Bao gồm chiến lược màu, chiến lược đặt tên, và cài đặt bảng điều khiển.",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "Trình chỉnh sửa xe",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "Kích hoạt bảng trình chỉnh sửa xe để thay đổi loại xe, livery, và thuộc tính. Có thể đặt ở dưới hoặc bên phải.",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "Giới hạn hành khách chờ",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Giới hạn số công dân có thể chờ giao thông công cộng tại một trạm. Tắt để xóa mọi giới hạn chờ và để game quản lý dung lượng trạm.",
        "SETTINGS_TRAINDISPLAY_SCOPE": "Hiển thị overlay khi",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "Chọn xe được chọn, camera first-person, hoặc cả hai.",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "Chỉ xe được chọn",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "Chỉ camera first-person",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "Xe được chọn hoặc camera first-person",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "Bố cục bảng",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "Bố cục nào mà overlay sử dụng. Cả hai hiển thị cùng thông tin và tuân thủ cùng checkbox - chỉ hình dạng khác nhau.",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "Xem trước các tính năng dự định. Hiển thị xám cho đến khi sẵn sàng; điểm đến hành khách vẫn có sẵn để kiểm tra.",
    },

    # Chinese Simplified (zh)
    "zh": {
        "SETTINGS_SUPPORT_LABEL": "如果 IPT4 挽救了你的存档或提升了性能，请考虑支持它的开发！",
        "SETTINGS_SUPPORT_BUTTON": "在 Ko-fi 上支持我",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "防挤堆 / 快速公交",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "启用车辆防挤堆、快速公交/有轨电车、生成间隔和激进车队规模。所有公交/有轨电车的间距和预算功能。",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "预算功能",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "启用线路预算控制、自动线路预算、票价倍数和作为路径成本的票价。所有金钱/定价/车队功能。",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "自动线路颜色",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "自动为新公交线路分配颜色和名称。包含颜色策略、命名策略和面板设置。",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "车辆编辑器",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "启用车辆编辑器面板以更改车辆类型、涂装和属性。可放置在底部或右侧。",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "等候乘客上限",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "限制单个站点可等候公共交通的市民数量。禁用以移除所有等候上限，让游戏管理站点容量。",
        "SETTINGS_TRAINDISPLAY_SCOPE": "显示覆盖层当",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "选择选中的车辆、第一人称相机或两者。",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "仅选中的车辆",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "仅第一人称相机",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "选中的车辆或第一人称相机",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "面板布局",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "覆盖层使用哪种布局。两者显示相同信息并遵守相同复选框 - 仅形状不同。",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "计划功能预览。就绪前显示为灰色；乘客目的地保持可用于测试。",
    },

    # Chinese Traditional (zh-tw)
    "zh-tw": {
        "SETTINGS_SUPPORT_LABEL": "如果 IPT4 拯救了你的存檔或提升了效能，請考慮支持它的開發！",
        "SETTINGS_SUPPORT_BUTTON": "在 Ko-fi 上支持我",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE": "防擠堆 / 快速公車",
        "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP": "啟用車輛防擠堆、快速公車/輕軌、生成間隔和激進車隊規模。所有公車/輕軌的間距和預算功能。",
        "SETTINGS_FEATURE_BUDGET_ENABLE": "預算功能",
        "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP": "啟用路線預算控制、自動路線預算、票價倍數和作為路徑成本的票價。所有金錢/定價/車隊功能。",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE": "自動路線顏色",
        "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP": "自動為新大眾運輸路線分配顏色和名稱。包含顏色策略、命名策略和面板設定。",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE": "車輛編輯器",
        "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP": "啟用車輛編輯器面板以更改車輛類型、塗裝和屬性。可放置在底部或右側。",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE": "等候乘客上限",
        "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP": "限制單一站點可等候大眾運輸的市民數量。停用以移除所有等候上限，讓遊戲管理站點容量。",
        "SETTINGS_TRAINDISPLAY_SCOPE": "顯示覆蓋層當",
        "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP": "選擇選中的車輛、第一人稱相機或兩者。",
        "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE": "僅選中的車輛",
        "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON": "僅第一人稱相機",
        "SETTINGS_TRAINDISPLAY_SCOPE_BOTH": "選中的車輛或第一人稱相機",
        "SETTINGS_TRAINDISPLAY_LAYOUT": "面板佈局",
        "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP": "覆蓋層使用哪種佈局。兩者顯示相同資訊並遵守相同核取方塊 - 僅形狀不同。",
        "SETTINGS_FUTURE_GROUP_DESCRIPTION": "計畫功能預覽。就緒前顯示為灰色；乘客目的地保持可用於測試。",
    },
}

# The list of languages to process
LANGUAGES = [
    "ar", "bg", "bn", "cs", "da", "el", "fi", "hi", "hu", "id",
    "ja", "ko", "kr", "ms", "no", "ro", "ru", "sk", "sv", "th",
    "tr", "uk", "ur", "vi", "zh", "zh-tw"
]

# Keys that should be added in order
NEW_KEYS = [
    # Support section (replacing KOFI)
    "SETTINGS_SUPPORT_LABEL",
    "SETTINGS_SUPPORT_BUTTON",
    # Feature toggles
    "SETTINGS_FEATURE_UNBUNCHING_ENABLE",
    "SETTINGS_FEATURE_UNBUNCHING_ENABLE_TOOLTIP",
    "SETTINGS_FEATURE_BUDGET_ENABLE",
    "SETTINGS_FEATURE_BUDGET_ENABLE_TOOLTIP",
    "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE",
    "SETTINGS_FEATURE_AUTOLINECOLOR_ENABLE_TOOLTIP",
    "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE",
    "SETTINGS_FEATURE_VEHICLEEDITOR_ENABLE_TOOLTIP",
    "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE",
    "SETTINGS_FEATURE_STOPSANDSTATIONS_ENABLE_TOOLTIP",
    # TrainDisplay scope/layout
    "SETTINGS_TRAINDISPLAY_SCOPE",
    "SETTINGS_TRAINDISPLAY_SCOPE_TOOLTIP",
    "SETTINGS_TRAINDISPLAY_SCOPE_SELECTEDVEHICLE",
    "SETTINGS_TRAINDISPLAY_SCOPE_FIRSTPERSON",
    "SETTINGS_TRAINDISPLAY_SCOPE_BOTH",
    "SETTINGS_TRAINDISPLAY_LAYOUT",
    "SETTINGS_TRAINDISPLAY_LAYOUT_TOOLTIP",
    # Updated Future group description
    "SETTINGS_FUTURE_GROUP_DESCRIPTION",
]

# Keys that should NOT be duplicated (already exist in most files)
EXISTING_KEYS_TO_SKIP = {
    "SETTINGS_KOFI_LINK", "SETTINGS_KOFI_GROUP", "SETTINGS_KOFI_DESCRIPTION", "SETTINGS_KOFI_BUTTON"
}

def read_translation_file(filepath):
    """Read a translation file and return list of (key, value) tuples and raw lines."""
    entries = []
    raw_lines = []
    with open(filepath, 'r', encoding='utf-8') as f:
        for line in f:
            raw_lines.append(line.rstrip('\n\r'))
            line_stripped = line.rstrip('\n\r')
            if line_stripped and not line_stripped.startswith('#'):
                parts = line_stripped.split(' ', 1)
                if len(parts) == 2:
                    entries.append((parts[0], parts[1]))
                else:
                    entries.append((parts[0], ""))
    return entries, raw_lines

def write_translation_file(filepath, raw_lines):
    """Write translation file from raw lines."""
    with open(filepath, 'w', encoding='utf-8') as f:
        for line in raw_lines:
            f.write(line + '\n')

def find_insertion_point(raw_lines):
    """Find the line index where CHANGELOG section starts."""
    for i, line in enumerate(raw_lines):
        if line.startswith('CHANGELOG_'):
            return i
    return len(raw_lines)

def key_exists_in_raw_lines(raw_lines, key):
    """Check if a key already exists in the raw lines."""
    for line in raw_lines:
        if line.startswith(key + ' '):
            return True
    return False

def process_language_file(lang_code, translations_dir):
    """Process a single language file."""
    filepath = os.path.join(translations_dir, f"{lang_code}.txt")
    
    if not os.path.exists(filepath):
        print(f"  File not found: {filepath}")
        return False, []
    
    entries, raw_lines = read_translation_file(filepath)
    existing_keys = set(key for key, _ in entries)
    
    # Find insertion point (before CHANGELOG)
    insert_idx = find_insertion_point(raw_lines)
    
    # Get translations for this language
    lang_translations = TRANSLATIONS.get(lang_code, {})
    
    # Build new lines to insert
    new_lines = []
    added_keys = []
    skipped_keys = []
    
    for key in NEW_KEYS:
        if key in existing_keys or key_exists_in_raw_lines(raw_lines, key):
            skipped_keys.append(key)
            continue
        
        if key in lang_translations:
            new_lines.append(f"{key} {lang_translations[key]}")
            added_keys.append(key)
        else:
            print(f"  WARNING: No translation for {key} in {lang_code}")
            skipped_keys.append(key)
    
    if new_lines:
        # Insert before CHANGELOG section
        # Add a blank line before new entries if the previous line isn't blank
        if insert_idx > 0 and raw_lines[insert_idx - 1].strip() != '':
            new_lines = [''] + new_lines
        
        # Insert the new lines
        for i, new_line in enumerate(new_lines):
            raw_lines.insert(insert_idx + i, new_line)
        
        write_translation_file(filepath, raw_lines)
        print(f"  [OK] {lang_code}: Added {len(added_keys)} keys, skipped {len(skipped_keys)} existing")
        return True, added_keys
    else:
        print(f"  - {lang_code}: No new keys to add (skipped {len(skipped_keys)} existing)")
        return False, []

def main():
    translations_dir = r"C:\Users\Lucas\source\repos\cs1_ipt4\Translations"
    
    print("=" * 60)
    print("IPT4 Translation Updater")
    print("=" * 60)
    
    total_added = 0
    total_files_modified = 0
    
    for lang in LANGUAGES:
        print(f"\nProcessing {lang}...")
        modified, added = process_language_file(lang, translations_dir)
        if modified:
            total_files_modified += 1
            total_added += len(added)
    
    print("\n" + "=" * 60)
    print(f"Summary: {total_files_modified} files modified, {total_added} total keys added")
    print("=" * 60)

if __name__ == "__main__":
    main()