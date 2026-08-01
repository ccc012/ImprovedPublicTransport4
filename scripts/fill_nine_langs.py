# -*- coding: utf-8 -*-
"""Write complete native UI translations for da/fi/no/sv/hu/ro/el/vi/ms.

CHANGELOG_* intentionally kept English. All other keys fully translated.
Source of truth: Translations/en.txt order and keys.
"""
from __future__ import print_function
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"

# ---------------------------------------------------------------------------
# Shared writer
# ---------------------------------------------------------------------------

def parse_en():
    order = []
    keys = {}
    for line in (ROOT / "en.txt").read_text(encoding="utf-8").splitlines():
        if not line.strip():
            order.append(None)
            continue
        i = line.find(" ")
        if i <= 0:
            order.append(None)
            continue
        k, v = line[:i], line[i + 1 :]
        keys[k] = v
        order.append(k)
    return keys, order


def write_lang(lang, translations, en_keys, order):
    lines = []
    for k in order:
        if k is None:
            lines.append("")
            continue
        if k.startswith("CHANGELOG_"):
            val = en_keys[k]
        else:
            val = translations.get(k)
            if val is None:
                # preserve existing non-English if present
                val = en_keys[k]
        lines.append(f"{k} {val}")
    path = ROOT / f"{lang}.txt"
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    same = sum(1 for k, v in en_keys.items() if not k.startswith("CHANGELOG_") and translations.get(k, v) == v)
    cl = sum(1 for k in en_keys if k.startswith("CHANGELOG_"))
    same_all = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else translations.get(k, v)) == v
    )
    print(f"{lang}: wrote {len(en_keys)} keys; non-CL same_as_en={same}; total same_as_en={same_all} (CL={cl})")


# ---------------------------------------------------------------------------
# Pattern helpers per language vocabulary
# ---------------------------------------------------------------------------

def expand_patterns(base, vocab):
    """Fill repetitive ticket/stop/delete keys from vocab terms."""
    t = dict(base)

    # Ticket prices
    for key, term in vocab["ticket"].items():
        t[key] = vocab["ticket_fmt"].format(term)

    # Max passengers waiting tooltips
    for key, term in vocab["stop_cap"].items():
        t[key] = vocab["stop_cap_fmt"].format(term)

    # Max passenger type labels (short)
    for key, term in vocab["stop_label"].items():
        t[key] = term

    # Delete tooltips
    for key, term in vocab["delete"].items():
        t[key] = vocab["delete_fmt"].format(term)

    # Delete labels
    for key, term in vocab["delete_label"].items():
        t[key] = term

    return t


# ===========================================================================
# DANISH
# ===========================================================================
DA_BASE = {
    "MOD_DESCRIPTION": "Forbedret offentlig transport: linjekontrol, flåde, integrationer og mere.",
    "CURRENT_WEEK": "Denne uge",
    "LAST_WEEK": "Sidste uge",
    "AVERAGE": "Gennemsnit",
    "AVERAGE_TOOLTIP": "Gennemsnit af de seneste {0} uger.",
    "CITY_SERVICE_PANEL_TITLE_STATION_STOPS": "Stationsstop",
    "CITY_SERVICE_PANEL_TITLE_DEPOT_VEHICLES": "Depotkøretøjer",
    "CITYSERVICE_ACCEPTINTERCITYBUSES": "Tillad intercitybusser",
    "CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP": "Tillad intercitybusser at bruge denne station. Deaktiver for kun lokale busser.",
    "EXPLANATION_BUDGET_CONTROL": "Budgetstyring: Antallet af køretøjer styres af budgettet.",
    "EXPLANATION_UNBUNCHING": "Afstandsstyring: Spillet forsøger at skabe afstand mellem køretøjer.",
    "LINE_PANEL_STOPS": "Stop: {0}",
    "LINE_PANEL_SPAWNTIMER": "Næste køretøj om {0} sekunder.",
    "LINE_PANEL_DEPOT_WARNING": "<color #FF0000>Det valgte depot har ingen køretøjer tilbage.</color>",
    "LINE_PANEL_BUDGET_CONTROL": "Budgetstyring",
    "LINE_PANEL_BUDGET_CONTROL_TOOLTIP": "Aktiverer eller deaktiverer budgetstyring for denne linje.",
    "LINE_PANEL_UNBUNCHING_TOOLTIP": "Aktiverer eller deaktiverer afstandsstyring for denne linje.\nAfstandsstyring er deaktiveret, hvis aggressiviteten er sat til 0.",
    "LINE_PANEL_DEPOT": "Depot:",
    "LINE_PANEL_NO_DEPOT_FOUND": "Intet depot fundet.",
    "LINE_PANEL_DEPOT_MARKER_TOOLTIP": "Spring til det valgte depot.\nHold Shift nede under klik for også at zoome ind.",
    "LINE_PANEL_SELECT_TYPES": "Vælg typer",
    "LINE_PANEL_SELECT_TYPES_TOOLTIP": "Skifter panelet 'Vælg typer'.\nHvis knappen er deaktiveret, skal du først vælge et depot.",
    "LINE_PANEL_LINE_STOPS": "Linjestop",
    "LINE_PANEL_LINE_VEHICLES": "Køretøjer på denne linje",
    "LINE_PANEL_ENQUEUED": "Køretøjer i kø",
    "LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP": "{0} passagerer venter på denne linje.",
    "LINE_PANEL_ADD_VEHICLE": "Tilføj køretøj",
    "LINE_PANEL_ADD_VEHICLE_TOOLTIP": "Tilføjer et nyt køretøj til linjen.\nHvis knappen er deaktiveret, har det valgte depot ingen køretøjer tilbage.",
    "LINE_PANEL_REMOVE_VEHICLE": "Fjern køretøj",
    "STOP_LIST_BOX_ROW_STOP": "Stop #{0}",
    "STOP_LIST_BOX_ROW_TOOLTIP": "{0}\nVentende passagerer: {1}\n\nHøjreklik for at springe til dette stop.\nHold Shift nede under klik for også at zoome ind.",
    "STOP_PANEL_SUGGESTED_NAMES_TOOLTIP": "Liste over foreslåede stopnavne.",
    "STOP_PANEL_REUSE_NAME_TOOLTIP": "Anvend dette navn på alle andre stop på denne station/position.",
    "STOP_PANEL_WAITING_PEOPLE": "Ventende passagerer: {0}",
    "COMMUTER_DESTINATION_PANEL_TITLE": "Passagerdestination",
    "COMMUTER_DESTINATION_HEADER": "Mest populære destinationer:",
    "COMMUTER_DESTINATION_NONE": "Ingen passagerer venter her lige nu.",
    "COMMUTER_DESTINATION_LOADING": "Beregner...",
    "COMMUTER_DESTINATION_BUTTON": "Destinationer",
    "COMMUTER_DESTINATION_BUTTON_TOOLTIP": "Vis hvor passagererne ved dette stop er på vej hen.",
    "STOP_PANEL_BORED_TIMER": "Tid til utålmodighed: <color #{0}>{1}</color>",
    "STOP_PANEL_BORED_TIMER_TOOLTIP": "Passagerer forlader stoppet, når nedtællingen når nul.",
    "STOP_PANEL_PASSENGERS_IN": "Passagerer ind:",
    "STOP_PANEL_PASSENGERS_IN_TOOLTIP": "Passagerer der stiger ind her.",
    "STOP_PANEL_PASSENGERS_OUT": "Passagerer ud:",
    "STOP_PANEL_PASSENGERS_OUT_TOOLTIP": "Passagerer der stiger af her.",
    "STOP_PANEL_PASSENGERS_TOTAL": "I alt:",
    "STOP_PANEL_PASSENGERS_TOTAL_TOOLTIP": "Samlet antal passagerer betjent her.",
    "STOP_PANEL_UNBUNCHING_TOOLTIP": "Aktiverer eller deaktiverer afstandsstyring ved dette stop.\nAfstandsstyring er deaktiveret, hvis aggressiviteten er sat til 0.",
    "STOP_PANEL_UPDATE_CLOSE_STOPS": "Opdater nærliggende stop",
    "STOP_PANEL_UPDATE_CLOSE_STOPS_TOOLTIP": "Sæt afstandsstyringsstatus for alle andre stop på denne station/position.",
    "STOP_PANEL_PREVIOUS": "Forrige stop",
    "STOP_PANEL_PREVIOUS_TOOLTIP": "Spring til forrige stop.\nHold Shift nede under klik for også at zoome ind.",
    "STOP_PANEL_DELETE_STOP": "Slet stop",
    "STOP_PANEL_DELETE_STOP_TOOLTIP": "Denne knap er aktiv, mens du holder Alt nede.\nBrug på eget ansvar!!!",
    "STOP_PANEL_NEXT": "Næste stop",
    "STOP_PANEL_NEXT_TOOLTIP": "Spring til næste stop.\nHold Shift nede under klik for også at zoome ind.",
    "STOP_BUTTON_TOOLTIP": "{0}\n\nKlik for at springe til dette stop.\nHold Shift nede under klik for også at zoome ind.\nHold Alt nede under klik for at undgå at åbne stopinfo-panelet.",
    "SETTINGS_DELETE": "Slet",
    "SETTINGS_RESET": "Nulstil",
    "SETTINGS_TAB_GENERAL": "Generelt",
    "SETTINGS_ADVANCED_LINKS_GROUP": "Links",
    "SETTINGS_GITHUB_REPO": "Kildekode på GitHub",
    "SETTINGS_TAB_AUTOLINE": "Auto-linje",
    "SETTINGS_TAB_STOPS": "Stop og stationer",
    "SETTINGS_TAB_UNBUNCHING": "Afstandsstyring",
    "SETTINGS_TAB_DELETE": "Slet linjer",
    "SETTINGS_TAB_FLEET": "Flåde og planlægning",
    "SETTINGS_TAB_BUDGET": "Budget og priser",
    "SETTINGS_TAB_LINECOLORS": "Linjefarver",
    "SETTINGS": "Indstillinger",
    "SETTINGS_SPEED": "Vis hastighed i: ",
    "SETTINGS_SPEED_TOOLTIP": "Vælg enheden til hastighedsvisning i grænsefladen.",
    "SETTINGS_GAMEPLAY_PROFILE": "Gameplay-profil",
    "SETTINGS_GAMEPLAY_PROFILE_TOOLTIP": "Anvender en pakke indstillinger på én gang. Sikker (standard) lader alt være slået fra for maksimal kompatibilitet med andre mods. Vanilla matcher basisspillet. Anbefalet aktiverer kun IPT-kernen (budget flådestyring, afstandsstyring, intercity-kontakt, underbygning-faner, unstucker, avanceret stopvalg, forhøjede stop). Realistisk aktiverer de fleste absorberede integrationer. Brugerdefineret kaskaderer aldrig - du styrer hver kontakt selv.",
    "SETTINGS_GAMEPLAY_PROFILE_CUSTOM": "Brugerdefineret",
    "SETTINGS_GAMEPLAY_PROFILE_SAFE": "Sikker (alt fra)",
    "SETTINGS_GAMEPLAY_PROFILE_VANILLA": "Vanilla",
    "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED": "Anbefalet (IPT-kerne)",
    "SETTINGS_GAMEPLAY_PROFILE_REALISTIC": "Realistisk",
    "SETTINGS_SPEED_KPH": "km/t",
    "SETTINGS_SPEED_MPH": "mph",
    "SETTINGS_WALKING_SPEED": "Gang-/cykelhastighed: ",
    "SETTINGS_WALKING_SPEED_TOOLTIP": "Standard: basisspillets hastigheder.\nRealistisk: reducerer ganghastighed til realistiske, aldersbaserede værdier.\nRealistisk reducerer også cykelhastighed med After Dark DLC.",
    "SETTINGS_WALKING_SPEED_MODE_VANILLA": "Standard",
    "SETTINGS_WALKING_SPEED_MODE_REALISTIC": "Realistisk",
    "SETTINGS_BBSP": "Bedre busstop-position: ",
    "SETTINGS_BBSP_TOOLTIP": "Deaktiveret: ingen positionsjustering.\nAktiveret: busser stopper forrest ved busstoppet i stedet for midt.",
    "SETTINGS_BBSP_MODE_DISABLED": "Deaktiveret",
    "SETTINGS_BBSP_MODE_ORIGINAL": "Aktiveret",
    "SETTINGS_BBSP_MODE_UPDATED": "Brug eksperimentel logik",
    "SETTINGS_BUDGET": "Budget",
    "SETTINGS_ENABLE_BUDGET_CONTROL": "Linjebudgetstyring:",
    "SETTINGS_BUDGET_CONTROL_DISABLED": "Deaktiveret",
    "SETTINGS_BUDGET_CONTROL_ENABLED": "Aktiveret",
    "SETTINGS_BUDGET_CONTROL_TOOLTIP": "Når aktiveret styres antallet af køretøjer på transportlinjer af budgettet; opdaterer alle eksisterende linjer og rydder køretøjer i kø.",
    "SETTINGS_BUDGET_TICKET_PRICES": "Billetpris-tilpasning:",
    "SETTINGS_BUDGET_TICKET_PRICES_DISABLED": "Deaktiveret",
    "SETTINGS_BUDGET_TICKET_PRICES_ENABLED": "Aktiveret",
    "SETTINGS_BUDGET_TICKET_PRICES_TOOLTIP": "Når aktiveret tilføjes en ny fane til Økonomi-panelet med skydere til billetpriser for hver transporttype.",
    "SETTINGS_AUTO_LINE_BUDGET": "Automatisk flådestørrelse:",
    "SETTINGS_AUTO_LINE_BUDGET_DISABLED": "Deaktiveret",
    "SETTINGS_AUTO_LINE_BUDGET_ENABLED": "Aktiveret",
    "SETTINGS_AUTO_LINE_BUDGET_TOOLTIP": "Når aktiveret tilpasser linjer i budgettilstand automatisk køretøjsantallet efter reel passagerefterspørgsel i stedet for vanilla-budgetskyderen. Linjer sat til Manuel røres aldrig.",
    "SETTINGS_AUTO_LINE": "Auto-linje",
    "SETTINGS_AUTOSHOW_LINE_INFO": "Åbn automatisk linjeinfo-panel",
    "SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP": "Vis automatisk linjeinfo-panelet efter en ny linje er oprettet.",
    "AUTOLINECOLOR_STRATEGY_DISABLED": "Deaktiveret",
    "AUTOLINECOLOR_STRATEGY_RANDOM_HUE": "Tilfældig nuance",
    "AUTOLINECOLOR_STRATEGY_RANDOM_COLOR": "Tilfældig farve",
    "AUTOLINECOLOR_STRATEGY_CATEGORISED": "Kategoriseret",
    "AUTOLINECOLOR_STRATEGY_NAMED": "Navngivne farver",
    "AUTOLINECOLOR_NAMING_DISABLED": "Deaktiveret",
    "AUTOLINECOLOR_NAMING_DISTRICTS": "Distrikter",
    "AUTOLINECOLOR_NAMING_LONDON": "London",
    "AUTOLINECOLOR_NAMING_ROADS": "Veje",
    "AUTOLINECOLOR_NAMING_COLORS": "Navngivne farver",
    "AUTOLINECOLOR_COLOR_STRATEGY": "Farvestrategi:",
    "AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP": "Hvordan farver tildeles nye linjer:\n'Tilfældig nuance' = samme mætning/lysstyrke, forskellige nuancer;\n'Tilfældig farve' = fuldt tilfældig RGB;\n'Kategoriseret' = farver efter køretøjstype;\n'Navngivne farver' = foruddefineret farvepalet.",
    "AUTOLINECOLOR_NAMING_STRATEGY": "Navngivningsstrategi:",
    "AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP": "Hvordan navne tildeles nye linjer:\n'Ingen' = ingen auto-navngivning;\n'Distrikter' = baseret på betjente distrikter;\n'London' = nummererede ruter (London Buses-stil);\n'Veje' = baseret på gadenavne;\n'Navngivne farver' = baseret på farvenavne.",
    "AUTOLINECOLOR_MIN_COLOR_DIFF": "Minimum farveforskel (%):",
    "AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP": "Minimum farveforskel i procent ved valg af tilfældige farver.",
    "AUTOLINECOLOR_MAX_COLOR_PICK": "Maksimale forsøg:",
    "AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP": "Maksimalt antal forsøg på at vælge en skelnelig farve.",
    "SETTINGS_UI": "UI-indstillinger",
    "SETTINGS_VEHICLE_EDITOR_POSITION": "Køretøjseditor-position: ",
    "SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP": "Vælg om køretøjseditoren vises nederst eller i højre side af skærmen.",
    "SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM": "Nederst",
    "SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT": "Højre",
    "SETTINGS_VEHICLE_EDITOR_HIDE": "Skjul køretøjseditor",
    "SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP": "Skjul køretøjseditoren fra køretøjspaneler.",
    "SETTINGS_STOPS": "Offentlige transportstop",
    "SETTINGS_STOPSANDSTATIONS_DESCRIPTION": "Hvor mange passagerer af hver transporttype der kan vente ved et enkelt stop, før det anses for fuldt. Højere værdier reducerer overfyldningsklager ved travle stop på bekostning af en mindre realistisk køstørrelse.",
    "SETTINGS_STOPSANDSTATIONS_RESET_TOOLTIP": "Nulstil alle passagerlofter ovenfor til standardværdier.",
    "SETTINGS_ENABLE_STOPS_AND_STATIONS": "Aktiver stop og stationer",
    "SETTINGS_ENABLE_STOPS_AND_STATIONS_TOOLTIP": "Juster det maksimale antal borgere der kan vente på offentlig transport ved stop og stationer. Konfigurer under fanen Stop.",
    "SETTINGS_STOPSANDSTATIONS_ENABLE": "Aktiver stop og stationer",
    "SETTINGS_STOPSANDSTATIONS_ENABLE_TOOLTIP": "Aktiver eller deaktiver håndhævelse af passagergrænser ved stop.",
    "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HEADER": "Maksimale ventende passagerer ved et linjestop:",
    "SETTINGS_UNBUNCHING": "Afstandsstyring",
    "SETTINGS_UNBUNCHING_AGGRESSION": "Afstandsstyrings-aggressivitet:",
    "SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP": "Hvor stærkt skal afstandsstyring virke?\nMulige værdier: 0–52. Værdi 0 deaktiverer afstandsstyring.\nHøjere værdier påvirker trafikken kraftigt og kan få køretøjer til at forsvinde.",
    "SETTINGS_VEHICLE_COUNT": "Køretøjer på nye linjer:",
    "SETTINGS_VEHICLE_COUNT_TOOLTIP": "Antal køretøjer der automatisk tilføjes til nye linjer, når linjebudgetstyring er slået fra.",
    "SETTINGS_SPAWN_TIME_INTERVAL": "Spawn-tidsinterval:",
    "SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP": "Tid i sekunder mellem køretøjsspawns.",
    "SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP": "Nulstiller alle skydere til standard.",
    "UNBUNCHING_ENABLED": "Afstandsstyring for køretøjer",
    "UNBUNCHING_DISABLED": "Afstandsstyring er deaktiveret.",
    "UNBUNCHING_TARGET_GAP": "Målafstand: {0}",
    "SETTINGS_EBS_GROUP_BUS": "Ekspresbus-tjenester",
    "SETTINGS_EBS_GROUP_TRAM": "Ekspressporvogn-tjenester",
    "SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE": "Ekspresbus: ",
    "SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE": "'Deaktiveret' = Busser bruger afstandsstyring ovenfor.\n'Forsigtig' = Stopper kort, tjekker passagerer, kan køre tomt.\n'Aggressiv' = Springer stop over hvis ingen passagerer venter.",
    "SETTINGS_EBS_ENABLE_SELFBAL": "Aktiver selvbalancering af service",
    "SETTINGS_EBS_DESC_SELFBAL": "Lader ekspresbus-tjenesten omfordele køretøjer langs linjen for at prioritere travle segmenter og reducere ventetid.",
    "SETTINGS_EBS_TOOLTIP_SELFBAL": "Analyserer linjesegmenter og kan omplacere køretøjer til travlere segmenter eller endestationer for jævnere service og kortere ventetid.\nBeslutninger er probabilistiske og afhænger af passagertal og omplaceringsodds.",
    "SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID": "Aktiver selvbalancering til midtlinje-stop",
    "SETTINGS_EBS_DESC_SELFBAL_TARGETMID": "Tillader selvbalancering at vælge et travlt stop midt på linjen i stedet for kun en endestation ved omplacering.",
    "SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID": "Tillad selvbalancering at omplacere køretøjer til et travlt midtlinje-stop (i stedet for endestation).\nOvervejes kun når det travleste stop har over 30 ventende passagerer, derefter valgt med ~50 % sandsynlighed og underlagt samlede omplaceringsodds.",
    "SETTINGS_EBS_ENABLE_MINIBUS": "Aktiver minibus-tilstand",
    "SETTINGS_EBS_DESC_MINIBUS": "Får mindre busser til at køre tidligere, når kun få passagerer stiger på eller af.",
    "SETTINGS_EBS_TOOLTIP_MINIBUS": "Busser med kapacitet ≤20 kan køre tidligere fra et stop, når påstigende + afstigende passagerer ≤5.",
    "SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE": "Ekspressporvogn: ",
    "SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING": "'Deaktiveret' = Sporvogne bruger afstandsstyring ovenfor.\n'Letbane' = Stopper ved hvert stop, venter altid fuld tid (streng disciplin).\n'Ægte sporvogn' = Stopper kun når passagerer stiger på/af.",
    "SETTINGS_EBS_MODE_NONE": "Deaktiveret",
    "SETTINGS_EBS_MODE_AGGRESSIVE": "Aggressiv",
    "SETTINGS_EBS_MODE_PRUDENTIAL": "Forsigtig",
    "SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL": "Letbane-tilstand",
    "SETTINGS_EBS_TRAM_MODE_NONE": "Deaktiveret",
    "SETTINGS_EBS_TRAM_MODE_TRAM": "Ægte sporvogn-tilstand",
    "SETTINGS_PTU_GROUP": "Frigør fastsiddende passagerer",
    "SETTINGS_PTU_ENABLE": "Fjern fastsiddende passagerer",
    "SETTINGS_PTU_TOOLTIP": "Fjerner automatisk passagerer der sidder fast under indstigning, så køretøjet kan køre normalt og undgå frosne afgange.",
    "SETTINGS_LINE_DELETION_TOOL": "Værktøj til sletning af linjer",
    "SETTINGS_LINE_DELETION_TOOL_DESCRIPTION": "Markér transporttyperne nedenfor, og tryk Slet for at fjerne alle linjer af de typer fra den aktuelle by. Valget er midlertidigt - det starter altid umarkeret og ryddes efter sletning; det gemmes ikke som indstilling.",
    "SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP": "Sletter alle linjer af de valgte typer. Virker kun mens byen er indlæst.",
    "SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE": "BEKRÆFT SLETNING AF LINJER",
    "SETTINGS_LINE_DELETION_TOOL_CONFIRM_MSG": "Du er ved at slette alle linjer.\nVil du fortsætte?",
    "VEHICLE_EDITOR_TITLE": "Køretøjseditor",
    "VEHICLE_EDITOR_SUB_TITLE": "{0} køretøjer",
    "VEHICLE_EDITOR_CAPACITY": "Passagkapacitet",
    "VEHICLE_EDITOR_CAPACITY_TAXI": "Rejsekapacitet",
    "VEHICLE_EDITOR_CAPACITY_TAXI_TOOLTIP": "Antal passagerer pr. arbejdsskift.",
    "VEHICLE_EDITOR_MAINTENANCE": "Vedligeholdelsesomkostning",
    "VEHICLE_EDITOR_MAX_SPEED": "Maks. hastighed",
    "VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS": "Togmotor i begge ender",
    "VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS_TOOLTIP": "Aktiverer eller deaktiverer togmotorer i begge ender af toget.",
    "VEHICLE_EDITOR_APPLY": "Anvend",
    "VEHICLE_EDITOR_DEFAULT": "Standard",
    "VEHICLE_LIST_BOX_ROW_TOOLTIP1": "Højreklik for at følge dette køretøj.\nHold Shift nede under klik for også at zoome ind.",
    "VEHICLE_LIST_BOX_ROW_TOOLTIP2": "Shift + klik for at sætte dette køretøj i kø.",
    "VEHICLE_PANEL_EDIT_TYPE": "Rediger køretøjstype",
    "VEHICLE_PANEL_EDIT_TYPE_TOOLTIP": "Rediger denne køretøjstype med køretøjseditoren.",
    "VEHICLE_PANEL_STATUS_NEXT_STOP": "Næste stop:",
    "VEHICLE_PANEL_STATUS_UNBUNCHING": "Afstandsstyring i gang",
    "VEHICLE_PANEL_LAST_STOP_EXCHANGE": "Passagerudveksling ved sidste stop: <color #FF0000>-{0}</color> | <color #00FF00>+{1}</color>",
    "VEHICLE_PANEL_PASSENGERS": "Passagerer:",
    "VEHICLE_PANEL_EARNINGS": "Indtjening:",
    "VEHICLE_PANEL_EARNINGS_TOOLTIP": "Resultat af billetsalg minus vedligeholdelsesomkostning for køretøjet.",
    "VEHICLE_PANEL_PREVIOUS": "Forrige køretøj",
    "VEHICLE_PANEL_PREVIOUS_TOOLTIP": "Spring til forrige køretøj.\nHold Shift nede under klik for også at zoome ind.",
    "VEHICLE_PANEL_REMOVE_VEHICLE": "Fjern køretøj",
    "VEHICLE_PANEL_NEXT": "Næste køretøj",
    "VEHICLE_PANEL_NEXT_TOOLTIP": "Spring til næste køretøj.\nHold Shift nede under klik for også at zoome ind.",
    "VEHICLE_SELECTION_CAPACITY": "Kapacitet",
    "VEHICLE_SELECTION_ADD_VEHICLE": "Tilføj dette køretøj til listen over tilladte køretøjer",
    "VEHICLE_SELECTION_ADD_ALL": "Tilføj alle egnede køretøjer til listen over tilladte køretøjer",
    "VEHICLE_SELECTION_REMOVE_VEHICLE": "Fjern dette køretøj fra listen over tilladte køretøjer",
    "VEHICLE_SELECTION_REMOVE_ALL": "Fjern alle køretøjer fra listen over tilladte køretøjer",
    "VEHICLE_SELECTION_AVAILABLE_VEHICLES": "Tilgængelige køretøjer",
    "VEHICLE_SELECTION_SELECTED_VEHICLES": "Valgte køretøjer",
    "VEHICLE_SELECTION_ANY_VEHICLE": "Ethvert køretøj",
    "VEHICLE_BUTTON_TOOLTIP": "{0}\n\nKlik for at springe til dette køretøj.\nHold Shift nede under klik for også at zoome ind.\nHold Alt nede under klik for at undgå at åbne køretøjsinfo-panelet.",
    "TRANSPORT_LINE_VEHICLECOUNT": "Antal køretøjer: {0}",
    "FLIGHT_TRACKER_NAME": "Flytracker",
    "FLIGHT_STATUS_NONE": "Ingen",
    "FLIGHT_STATUS_INCOMING": "Indgående",
    "FLIGHT_STATUS_LANDED": "Landet",
    "FLIGHT_STATUS_AT_GATE": "Ved gate",
    "FLIGHT_STATUS_DEPARTED": "Afgået",
    "ECONOMY_TAB_TICKET_PRICES": "Billetpriser",
    "ECONOMY_TAB_TICKET_PRICES_TOOLTIP_PASSENGER_COUNT": "Samlet aktuelt passagertal for denne transporttype.",
    "WHATSNEW_3_0_0_1": "Opdateret til Race Day; nu kompatibel med More Vehicles Renewed.",
    "WHATSNEW_3_0_0_2": "Følgende mods er integreret i IPT3:\n • Advanced Stop Selection Revisited\n • Auto Line Color Redux\n • Better Bus Stop Position\n • Better Train Boarding\n • Elevated Stops Enabler Revisited  \n • Express Bus Services\n • Flight Tracker\n • Intercity Bus Control\n • Mileage Taxi Services\n • Public Transport Unstucker\n • Realistic Walking Speed\n • Stops and Stations\n • Ticket Price Customizer",
    "WHATSNEW_3_0_1": "Public Transport Unstucker er nu integreret i IPT3.\nSe version 3.0 for alle andre integrerede mods. Afmeld originalerne for at undgå konflikter.",
    "SETTINGS_TAB_TRAINDISPLAY": "Togvisning",
    "SETTINGS_TAB_INTEGRATIONS": "Integrationer",
    "SETTINGS_TRAINDISPLAY_GROUP": "Togvisning-overlay",
    "SETTINGS_TRAINDISPLAY_GROUP_DESCRIPTION": "Konfigurer det integrerede overlay, der vises mens du følger understøttede transportkøretøjer.",
    "SETTINGS_TRAINDISPLAY_ENABLE": "Aktiver togvise",
    "SETTINGS_TRAINDISPLAY_ENABLE_TOOLTIP": "Tænd eller sluk det integrerede togvise-overlay.",
    "SETTINGS_TRAINDISPLAY_MODE_DISABLED": "Deaktiveret",
    "SETTINGS_TRAINDISPLAY_MODE_ENABLED": "Aktiveret",
    "SETTINGS_TRAINDISPLAY_OVERLAY_POSITION": "Overlay-position:",
    "SETTINGS_TRAINDISPLAY_OVERLAY_POSITION_TOOLTIP": "Vælg hvor overlayet vises på skærmen.",
    "SETTINGS_TRAINDISPLAY_POS_TOPLEFT": "Øverst til venstre",
    "SETTINGS_TRAINDISPLAY_POS_TOPRIGHT": "Øverst til højre",
    "SETTINGS_TRAINDISPLAY_POS_BOTTOMLEFT": "Nederst til venstre",
    "SETTINGS_TRAINDISPLAY_POS_BOTTOMRIGHT": "Nederst til højre",
    "SETTINGS_TRAINDISPLAY_OVERLAY_SCALE": "Overlay-skala:",
    "SETTINGS_TRAINDISPLAY_OVERLAY_SCALE_TOOLTIP": "Skalér overlayets størrelse.",
    "SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY": "Overlay-gennemsigtighed:",
    "SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY_TOOLTIP": "Juster hvor gennemsigtigt overlayet er.",
    "SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL": "Opdateringsinterval:",
    "SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL_TOOLTIP": "Hvor ofte overlayet opdateres mens du følger et køretøj.",
    "SETTINGS_TRAINDISPLAY_SHOW_LINE": "Vis linjenavn",
    "SETTINGS_TRAINDISPLAY_SHOW_LINE_TOOLTIP": "Inkluder linjenavnet i overlayet.",
    "SETTINGS_TRAINDISPLAY_SHOW_DESTINATION": "Vis destination",
    "SETTINGS_TRAINDISPLAY_SHOW_DESTINATION_TOOLTIP": "Inkluder destinationen i overlayet.",
    "SETTINGS_TRAINDISPLAY_SHOW_STATE": "Vis tilstand",
    "SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP": "Inkluder køretøjets tilstand i overlayet.",
    "SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING": "Kun under følgning",
    "SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING_TOOLTIP": "Skjul overlayet medmindre kameraet faktisk følger et understøttet køretøj.",
    "SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY": "Kun i førstepersons-kamera",
    "SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY_TOOLTIP": "Vis kun overlayet ved brug af en førstepersons-kameramod (f.eks. First Person Camera - Continued). Hvis slået fra, vises det når du følger et understøttet køretøj i enhver kameratilstand.",
    "SETTINGS_TRAINDISPLAY_THEME": "Farvetema:",
    "SETTINGS_TRAINDISPLAY_THEME_TOOLTIP": "Vælg overlayets baggrunds-/tekstfarver.",
    "SETTINGS_TRAINDISPLAY_THEME_SIMPLE": "Simpel",
    "SETTINGS_TRAINDISPLAY_THEME_DARK": "Mørk",
    "SETTINGS_TRAINDISPLAY_THEME_LIGHT": "Lys",
    "SETTINGS_TRAINDISPLAY_THEME_ORIGINAL": "Original",
    "SETTINGS_TRAINDISPLAY_THEME_BLUE": "Blå",
    "SETTINGS_TRAINDISPLAY_THEME_GREEN": "Grøn",
    "SETTINGS_TRAINDISPLAY_THEME_AMBER": "Rav",
    "COPY_TIP": "Kopiér denne linjes indstillinger.",
    "PASTE_TIP": "Indsæt de kopierede linjeindstillinger.",
    "COPY_BUILDING_TIP": "Kopiér disse indstillinger til alle linjer der betjener denne bygning.",
    "COPY_DISTRICT_TIP": "Kopiér disse indstillinger til alle linjer i dette distrikt.",
    "SETTINGS_INTEGRATIONS_GROUP": "Integrerede tilføjelser",
    "SETTINGS_INTERCITY_BUS_ENABLE": "Aktiver intercitybus-styring",
    "SETTINGS_INTERCITY_BUS_ENABLE_TOOLTIP": "Aktiverer det integrerede intercitybus-kompatibilitetslag og stationspatching.",
    "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE": "Aktivér avanceret stopvalg",
    "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP": "Lader dig placere stop på alternative perroner/spor på flersporede stationer (hold alternativ-tilstandstasten under placering). Træder i kraft ved næste niveauindlæsning.",
    "SETTINGS_BETTERBOARDING_ENABLE": "Aktiver bedre indstigning",
    "SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP": "Forbedrer indstigningsvalg, så passagerer foretrækker køretøjet der faktisk betjener deres destination. Træder i kraft ved næste niveauindlæsning.",
    "SETTINGS_MILEAGETAXI_ENABLE": "Aktiver kilometertaxi",
    "SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP": "Opkræver taxifart efter kørt distance i stedet for fast pris, så længere ture giver mere. Kræver After Dark DLC. Træder i kraft ved næste niveauindlæsning.",
    "SETTINGS_ELEVATEDSTOPS_ENABLE": "Aktiver forhøjede stop",
    "SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP": "Tillader offentlige transportstop på forhøjede veje/broer og bevarer gadebelysning på de segmenter. Træder i kraft ved næste niveauindlæsning.",
    "SETTINGS_INTERCITY_BUS_CAPACITY": "Kapacitet for intercityterminal",
    "SETTINGS_INTERCITY_BUS_CAPACITY_TOOLTIP": "Hvor mange køretøjer en intercitybusterminal kan rumme ad gangen.",
    "SETTINGS_TRAM_DEPOT_CAPACITY": "Sporvognsdepot-kapacitet",
    "SETTINGS_TRAM_DEPOT_CAPACITY_TOOLTIP": "Vanilla giver hvert sporvognsdepot en praktisk ubegrænset grænse på 100.000 køretøjer. Realistisk og Mellem anvender i stedet et moderat fast loft; Deaktiveret bevarer eksisterende adfærd.",
    "SETTINGS_TAXI_DEPOT_CAPACITY": "Taxidepot-kapacitet",
    "SETTINGS_TAXI_DEPOT_CAPACITY_TOOLTIP": "Vanilla giver hvert taxidepot en praktisk ubegrænset grænse på 100.000 køretøjer. Realistisk og Mellem anvender i stedet et moderat fast loft; Deaktiveret bevarer eksisterende adfærd.",
    "SETTINGS_BUS_DEPOT_CAPACITY": "Busdepot-kapacitet",
    "SETTINGS_BUS_DEPOT_CAPACITY_TOOLTIP": "Vanilla giver hvert busdepot (almindelige, biobrændstof og turistbusgarager) en praktisk ubegrænset grænse på 100.000 køretøjer. Realistisk og Mellem anvender i stedet et moderat fast loft; Deaktiveret bevarer eksisterende adfærd.",
    "SETTINGS_TROLLEYBUS_DEPOT_CAPACITY": "Trolleybusdepot-kapacitet",
    "SETTINGS_TROLLEYBUS_DEPOT_CAPACITY_TOOLTIP": "Vanilla giver hvert trolleybusdepot en praktisk ubegrænset grænse på 100.000 køretøjer. Realistisk og Mellem anvender i stedet et moderat fast loft; Deaktiveret bevarer eksisterende adfærd.",
    "SETTINGS_FERRY_DEPOT_CAPACITY": "Færgedepot-kapacitet",
    "SETTINGS_FERRY_DEPOT_CAPACITY_TOOLTIP": "Vanilla giver hvert færgedepot en praktisk ubegrænset grænse på 100.000 køretøjer. Realistisk og Mellem anvender i stedet et moderat fast loft; Deaktiveret bevarer eksisterende adfærd.",
    "SETTINGS_DEPOT_CAPACITY_DISABLED": "Deaktiveret (ubegrænset)",
    "SETTINGS_DEPOT_CAPACITY_INTERMEDIATE": "Mellem",
    "SETTINGS_DEPOT_CAPACITY_REALISTIC": "Realistisk",
    "SETTINGS_FLIGHTTRACKER_ENABLE": "Aktiver flytracker",
    "SETTINGS_FLIGHTTRACKER_ENABLE_TOOLTIP": "Aktiverer de integrerede flytracker-patches og UI-understøttelse.",
    "SETTINGS_SUBBUILDINGSTABS_ENABLE": "Aktiver underbygning-faner",
    "SETTINGS_SUBBUILDINGSTABS_ENABLE_TOOLTIP": "Viser en fanestribe på en bygnings infopanel, når den har underbygninger (f.eks. en lufthavn med indbygget metrostation), så du kan skifte mellem dem.",
    "SETTINGS_TAXISTANDFIX_ENABLE": "Aktiver taxaholdeplads-rettelse",
    "SETTINGS_TAXISTANDFIX_ENABLE_TOOLTIP": "Sender ledige taxaer til nærmeste holdeplads i stedet for at lade dem køre tilfældigt. Kræver After Dark DLC.",
    "SETTINGS_SHAREDSTOPENABLER_ENABLE": "Aktiver delte stop",
    "SETTINGS_SHAREDSTOPENABLER_ENABLE_TOOLTIP": "Tillader mere end én transporttype (bus, sporvogn, trolleybus) at stoppe på samme vejsegment. Fra som standard - se moddens GitHub for hvad denne reducerede version udelader.",
    "SETTINGS_COMMUTERDESTINATION_ENABLE": "Aktiver passagerdestination (redesign afventer)",
    "SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP": "Midlertidigt utilgængelig: det tidligere panel duplikerede stopinfo-UI og er tvunget fra indtil et redesign. Afkrydsningsfeltet kan ikke genaktivere det.",
    "SETTINGS_OOC_ENABLE": "Optimerede eksterne forbindelser",
    "SETTINGS_OOC_ENABLE_TOOLTIP": "Godstog, fly og skibe venter længere på en fyldigere last, før de afgår ved eksterne forbindelser.",
    "SETTINGS_OOC_WAIT_MULTIPLIER": "Ventemultiplikator",
    "SETTINGS_OOC_WAIT_MULTIPLIER_TOOLTIP": "Hvor meget længere end vanilla der ventes på en fyldigere last. Højere værdier betyder færre, men fyldigere ture.",
    "SETTINGS_OOC_PASSENGER_SCOPE": "Omfang for passagerventetid",
    "SETTINGS_OOC_PASSENGER_SCOPE_TOOLTIP": "Hvor ventemultiplikatoren ovenfor gælder for borgere der venter på offentlig transport. Kun eksterne forbindelser påvirker kun borgere der venter ved en ekstern forbindelse; byomfattende forsinker også almindelig indenlandsk venten for alle borgere (matcher kildemodens faktiske adfærd).",
    "SETTINGS_OOC_PASSENGER_SCOPE_OUTSIDE": "Kun eksterne forbindelser",
    "SETTINGS_OOC_PASSENGER_SCOPE_CITYWIDE": "Byomfattende",
    "SETTINGS_OOC_PASSENGER_SCOPE_DISABLED": "Deaktiveret (vanilla)",
    "SETTINGS_OOC_DISABLE_DUMMY": "Deaktiver dekorativ gennemkørende trafik",
    "SETTINGS_OOC_DISABLE_DUMMY_ROAD": "Deaktiver dekorativ vejtrafik",
    "SETTINGS_OOC_DISABLE_DUMMY_TRAIN": "Deaktiver dekorativ togtrafik",
    "SETTINGS_OOC_DISABLE_DUMMY_PLANE": "Deaktiver dekorativ flytrafik",
    "SETTINGS_OOC_DISABLE_DUMMY_SHIP": "Deaktiver dekorativ skibstrafik",
    "SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP": "Eksterne forbindelser spawner normalt ekstra dekorativ trafik der aldrig reelt kommer ind i eller forlader byen, kun for visuel atmosfære. Deaktivering fjerner den trafik uden at påvirke reel import/eksport/passagerflow.",
    "SETTINGS_UOC_ENABLE": "Ubegrænsede eksterne forbindelser",
    "SETTINGS_UOC_ENABLE_TOOLTIP": "Fjerner vanillas grænse på 4 forbindelser for veje, skinner, skibs- og flyruter, og tilslutter retrospektivt transportlinjer når en ny forbindelse bygges nær en eksisterende station. Træder i kraft ved næste niveauindlæsning.",
    "SETTINGS_STTAI_ENABLE": "Kollisionsundgåelse på enkeltsporet jernbane",
    "SETTINGS_STTAI_ENABLE_TOOLTIP": "Reserverer et enkeltsporet segment til ét tog ad gangen og holder et modgående tog ved indgangen, indtil segmentet er frit. Original IPT4-funktion, findes ikke i vanilla eller nogen absorberet mod.",
    "SETTINGS_STOPSTACKER_ENABLE": "Stabling af busstop-pladser",
    "SETTINGS_STOPSTACKER_ENABLE_TOOLTIP": "Lader en anden/tredje bus der nærmer sig samme stop bruge sin egen plads længere tilbage langs stopsporet i stedet for at stå i enkeltfil bag den forreste bus, så flere busser kan laste/losse samtidigt. Original IPT4-funktion (forenklet clean-room-genimplementering), findes ikke i vanilla eller nogen absorberet mod.",
    "TRAINDISPLAY_LABEL_NAME": "Navn",
    "TRAINDISPLAY_LABEL_STATUS": "Status",
    "TRAINDISPLAY_NO_LINE": "Ingen linje",
    "TRAINDISPLAY_NO_DESTINATION": "Ingen destination",
    "TRAINDISPLAY_HIDDEN": "Skjult",
    "TRAINDISPLAY_VEHICLE": "Køretøj",
    "TRAINDISPLAY_STATE_RETURNING": "Returnerer",
    "TRAINDISPLAY_STATE_STOPPED": "Ved stop",
    "TRAINDISPLAY_STATE_EN_ROUTE": "Undervejs",
    "TRAINDISPLAY_STATE_ON_LINE": "På linjen",
    "TRAINDISPLAY_STATE_IDLE": "Inaktiv",
    "AUTOLINECOLOR_REFRESH_BUTTON": "Opdater navn/farve",
    "AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP": "Tildel igen det aktuelle linjenavn og farve efter de aktive AutoLineColor-indstillinger.",
    "AUTOLINECOLOR_REFRESH_DISABLED_TOOLTIP": "Aktiver en farve- eller navngivningsstrategi under Indstillinger, før du opdaterer denne linje.",
    "TICKET_PRICE_LABEL_TOOLTIP": "Aktuel pris: {0}\nOriginal pris: {1}\nPassagerer undervejs nu: {2}",
}

DA_VOCAB = {
    "ticket_fmt": "{} billetpris: ",
    "ticket": {
        "TICKET_PRICE_TAXI_KILOMETER": "Taxipris pr. kilometer:",
        "TICKET_PRICE_TAXI_MILE": "Taxipris pr. mile:",
        "TICKET_PRICE_BUS": "Bus",
        "TICKET_PRICE_INTERCITY_BUS": "Intercitybus",
        "TICKET_PRICE_METRO": "Metro",
        "TICKET_PRICE_TRAIN": "Tog",
        "TICKET_PRICE_TRAM": "Sporvogn",
        "TICKET_PRICE_MONORAIL": "Monorail",
        "TICKET_PRICE_SHIP": "Skib",
        "TICKET_PRICE_FERRY": "Færge",
        "TICKET_PRICE_PLANE": "Fly",
        "TICKET_PRICE_CABLECAR": "Svævebane",
        "TICKET_PRICE_SIGHTSEEING_BUS": "Turistbus",
        "TICKET_PRICE_TROLLEYBUS": "Trolleybus",
        "TICKET_PRICE_BLIMP": "Luftskib",
        "TICKET_PRICE_HELICOPTER": "Helikopter",
    },
    "stop_cap_fmt": "Maksimale ventende passagerer ved {}",
    "stop_cap": {
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS_TOOLTIP": "et linjebusstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS_TOOLTIP": "et trolleybusstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS_TOOLTIP": "et evakueringsbusstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS_TOOLTIP": "et turistbusstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM_TOOLTIP": "et sporvognsstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO_TOOLTIP": "en metrostation",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN_TOOLTIP": "en togstation",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL_TOOLTIP": "et monorailstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP_TOOLTIP": "en godsskibshavn",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY_TOOLTIP": "et færgestop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE_TOOLTIP": "en flyterminal",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR_TOOLTIP": "et svævebanestop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON_TOOLTIP": "et varmluftsballonstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER_TOOLTIP": "et helikopterstop",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP_TOOLTIP": "et luftskibsstop",
    },
    "stop_label": {
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS": "Linjebus",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS": "Trolleybus",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS": "Evakueringsbus",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS": "Turistbus",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM": "Sporvogn",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO": "Metro",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN": "Tog",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL": "Monorail",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP": "Skib",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY": "Færge",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE": "Fly",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR": "Svævebane",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON": "Varmluftsballon",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER": "Helikopter",
        "SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP": "Luftskib",
    },
    "delete_fmt": "Sletter alle {}.",
    "delete": {
        "SETTINGS_DELETE_BUS_TOOLTIP": "almindelige buslinjer",
        "SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP": "turistbuslinjer",
        "SETTINGS_DELETE_TRAM_TOOLTIP": "sporvognslinjer",
        "SETTINGS_DELETE_TROLLEYBUS_TOOLTIP": "trolleybuslinjer",
        "SETTINGS_DELETE_TRAIN_TOOLTIP": "toglinjer",
        "SETTINGS_DELETE_METRO_TOOLTIP": "underjordiske metrolinjer",
        "SETTINGS_DELETE_MONORAIL_TOOLTIP": "monoraillinjer",
        "SETTINGS_DELETE_SHIP_TOOLTIP": "færgelinjer",
        "SETTINGS_DELETE_HELICOPTER_TOOLTIP": "helikopterlinjer",
        "SETTINGS_DELETE_BLIMP_TOOLTIP": "luftskibslinjer",
    },
    "delete_label": {
        "SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL": "Turistbusser",
        "SETTINGS_DELETE_FERRY_LABEL": "Færger",
        "SETTINGS_DELETE_HELICOPTER_LABEL": "Helikoptere",
        "SETTINGS_DELETE_BLIMP_LABEL": "Luftskibe",
    },
}

# Fix ticket specials (taxi already full string)
def fix_da_ticket(t):
    t["TICKET_PRICE_TAXI_KILOMETER"] = "Taxipris pr. kilometer: "
    t["TICKET_PRICE_TAXI_MILE"] = "Taxipris pr. mile: "
    for k in list(t.keys()):
        if k.startswith("TICKET_PRICE_") and k not in ("TICKET_PRICE_TAXI_KILOMETER", "TICKET_PRICE_TAXI_MILE", "TICKET_PRICE_LABEL_TOOLTIP"):
            if not t[k].endswith(": ") and not t[k].endswith(":"):
                pass
            # rebuild properly
    mapping = {
        "TICKET_PRICE_BUS": "Busbilletpris: ",
        "TICKET_PRICE_INTERCITY_BUS": "Intercitybus-billetpris: ",
        "TICKET_PRICE_METRO": "Metrobilletpris: ",
        "TICKET_PRICE_TRAIN": "Togbilletpris: ",
        "TICKET_PRICE_TRAM": "Sporvognsbilletpris: ",
        "TICKET_PRICE_MONORAIL": "Monorail-billetpris: ",
        "TICKET_PRICE_SHIP": "Skibsbilletpris: ",
        "TICKET_PRICE_FERRY": "Færgebilletpris: ",
        "TICKET_PRICE_PLANE": "Flybilletpris: ",
        "TICKET_PRICE_CABLECAR": "Svævebanebilletpris: ",
        "TICKET_PRICE_SIGHTSEEING_BUS": "Turistbusbilletpris: ",
        "TICKET_PRICE_TROLLEYBUS": "Trolleybusbilletpris: ",
        "TICKET_PRICE_BLIMP": "Luftskibsbilletpris: ",
        "TICKET_PRICE_HELICOPTER": "Helikopterbilletpris: ",
    }
    t.update(mapping)
    return t


# Due to file size, remaining languages are loaded from companion modules if present,
# otherwise built inline below via import of generated data files.
# We embed all remaining languages in this file via LANG_DATA dict populated at bottom.

def build_da():
    t = expand_patterns(DA_BASE, DA_VOCAB)
    return fix_da_ticket(t)


# Import full packs generated alongside this script
from fill_nine_langs_data import LANG_PACKS  # noqa: E402


def main():
    en_keys, order = parse_en()
    packs = dict(LANG_PACKS)
    packs["da"] = build_da()

    for lang in ["da", "fi", "no", "sv", "hu", "ro", "el", "vi", "ms"]:
        tr = packs[lang]
        # Ensure every non-changelog key has a value
        missing = [k for k in en_keys if not k.startswith("CHANGELOG_") and k not in tr]
        if missing:
            print(f"WARNING {lang}: {len(missing)} missing keys e.g. {missing[:8]}")
        write_lang(lang, tr, en_keys, order)


if __name__ == "__main__":
    main()
