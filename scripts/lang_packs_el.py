# -*- coding: utf-8 -*-
"""Greek complete UI pack."""
from __future__ import print_function

EL = {}
_EL = r"""
MOD_DESCRIPTION Βελτιωμένες δημόσιες συγκοινωνίες: έλεγχος γραμμών, στόλος, ενσωματώσεις και άλλα.
CURRENT_WEEK Τρέχουσα εβδομάδα
LAST_WEEK Προηγούμενη εβδομάδα
AVERAGE Μέσος όρος
AVERAGE_TOOLTIP Μέσος όρος των τελευταίων {0} εβδομάδων.
CITY_SERVICE_PANEL_TITLE_STATION_STOPS Στάσεις σταθμού
CITY_SERVICE_PANEL_TITLE_DEPOT_VEHICLES Οχήματα αμαξοστασίου
CITYSERVICE_ACCEPTINTERCITYBUSES Αποδοχή υπεραστικών λεωφορείων
CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP Επιτρέπει σε υπεραστικά λεωφορεία να χρησιμοποιούν αυτόν τον σταθμό. Απενεργοποιήστε για μόνο τοπικά.
EXPLANATION_BUDGET_CONTROL Έλεγχος προϋπολογισμού: Ο αριθμός οχημάτων ελέγχεται από τον προϋπολογισμό.
EXPLANATION_UNBUNCHING Αποφυγή συσσώρευσης: Το παιχνίδι προσπαθεί να δημιουργήσει κενό μεταξύ οχημάτων.
LINE_PANEL_STOPS Στάσεις: {0}
LINE_PANEL_SPAWNTIMER Επόμενο όχημα σε {0} δευτερόλεπτα.
LINE_PANEL_DEPOT_WARNING <color #FF0000>Το επιλεγμένο αμαξοστάσιο δεν έχει άλλα οχήματα.</color>
LINE_PANEL_BUDGET_CONTROL Έλεγχος προϋπολογισμού
LINE_PANEL_BUDGET_CONTROL_TOOLTIP Ενεργοποιεί ή απενεργοποιεί τον έλεγχο προϋπολογισμού για αυτή τη γραμμή.
LINE_PANEL_UNBUNCHING_TOOLTIP Ενεργοποιεί ή απενεργοποιεί την αποφυγή συσσώρευσης για αυτή τη γραμμή.\nΗ αποφυγή είναι ανενεργή αν η επιθετικότητα είναι 0.
LINE_PANEL_DEPOT Αμαξοστάσιο:
LINE_PANEL_NO_DEPOT_FOUND Δεν βρέθηκε αμαξοστάσιο.
LINE_PANEL_DEPOT_MARKER_TOOLTIP Μετάβαση στο επιλεγμένο αμαξοστάσιο.\nΚρατήστε Shift στο κλικ για ζουμ.
LINE_PANEL_SELECT_TYPES Επιλογή τύπων
LINE_PANEL_SELECT_TYPES_TOOLTIP Εναλλάσσει το πάνελ «Επιλογή τύπων».\nΑν το κουμπί είναι ανενεργό, επιλέξτε πρώτα αμαξοστάσιο.
LINE_PANEL_LINE_STOPS Στάσεις γραμμής
LINE_PANEL_LINE_VEHICLES Οχήματα σε αυτή τη γραμμή
LINE_PANEL_ENQUEUED Οχήματα στην ουρά
LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP {0} επιβάτες περιμένουν σε αυτή τη γραμμή.
LINE_PANEL_ADD_VEHICLE Προσθήκη οχήματος
LINE_PANEL_ADD_VEHICLE_TOOLTIP Προσθέτει νέο όχημα στη γραμμή.\nΑν το κουμπί είναι ανενεργό, το αμαξοστάσιο δεν έχει οχήματα.
LINE_PANEL_REMOVE_VEHICLE Αφαίρεση οχήματος
STOP_LIST_BOX_ROW_STOP Στάση #{0}
STOP_LIST_BOX_ROW_TOOLTIP {0}\nΕπιβάτες σε αναμονή: {1}\n\nΔεξί κλικ για μετάβαση στη στάση.\nΚρατήστε Shift στο κλικ για ζουμ.
STOP_PANEL_SUGGESTED_NAMES_TOOLTIP Λίστα προτεινόμενων ονομάτων στάσης.
STOP_PANEL_REUSE_NAME_TOOLTIP Ορίστε αυτό το όνομα σε όλες τις άλλες στάσεις σε αυτόν τον σταθμό/θέση.
STOP_PANEL_WAITING_PEOPLE Επιβάτες σε αναμονή: {0}
COMMUTER_DESTINATION_PANEL_TITLE Προορισμός επιβατών
COMMUTER_DESTINATION_HEADER Κορυφαίοι προορισμοί:
COMMUTER_DESTINATION_NONE Δεν περιμένει κανείς εδώ τώρα.
COMMUTER_DESTINATION_LOADING Υπολογισμός...
COMMUTER_DESTINATION_BUTTON Προορισμοί
COMMUTER_DESTINATION_BUTTON_TOOLTIP Δείχνει πού κατευθύνονται οι επιβάτες αυτής της στάσης.
STOP_PANEL_BORED_TIMER Χρόνος μέχρι ανυπομονησία: <color #{0}>{1}</color>
STOP_PANEL_BORED_TIMER_TOOLTIP Οι επιβάτες φεύγουν όταν η αντίστροφη μέτρηση φτάσει στο μηδέν.
STOP_PANEL_PASSENGERS_IN Επιβάτες είσοδος:
STOP_PANEL_PASSENGERS_IN_TOOLTIP Επιβάτες που επιβιβάζονται εδώ.
STOP_PANEL_PASSENGERS_OUT Επιβάτες έξοδος:
STOP_PANEL_PASSENGERS_OUT_TOOLTIP Επιβάτες που αποβιβάζονται εδώ.
STOP_PANEL_PASSENGERS_TOTAL Σύνολο:
STOP_PANEL_PASSENGERS_TOTAL_TOOLTIP Συνολικοί επιβάτες που εξυπηρετήθηκαν εδώ.
STOP_PANEL_UNBUNCHING_TOOLTIP Ενεργοποιεί ή απενεργοποιεί την αποφυγή συσσώρευσης σε αυτή τη στάση.\nΑνενεργή αν η επιθετικότητα είναι 0.
STOP_PANEL_UPDATE_CLOSE_STOPS Ενημέρωση κοντινών στάσεων
STOP_PANEL_UPDATE_CLOSE_STOPS_TOOLTIP Ορίστε την κατάσταση αποφυγής συσσώρευσης σε όλες τις άλλες στάσεις σε αυτόν τον σταθμό/θέση.
STOP_PANEL_PREVIOUS Προηγούμενη στάση
STOP_PANEL_PREVIOUS_TOOLTIP Μετάβαση στην προηγούμενη στάση.\nΚρατήστε Shift στο κλικ για ζουμ.
STOP_PANEL_DELETE_STOP Διαγραφή στάσης
STOP_PANEL_DELETE_STOP_TOOLTIP Το κουμπί ενεργοποιείται κρατώντας Alt.\nΧρησιμοποιήστε με δική σας ευθύνη!!!
STOP_PANEL_NEXT Επόμενη στάση
STOP_PANEL_NEXT_TOOLTIP Μετάβαση στην επόμενη στάση.\nΚρατήστε Shift στο κλικ για ζουμ.
STOP_BUTTON_TOOLTIP {0}\n\nΚλικ για μετάβαση στη στάση.\nΚρατήστε Shift στο κλικ για ζουμ.\nΚρατήστε Alt στο κλικ για να μην ανοίξει το πάνελ πληροφοριών στάσης.
SETTINGS_DELETE Διαγραφή
SETTINGS_RESET Επαναφορά
SETTINGS_TAB_GENERAL Γενικά
SETTINGS_ADVANCED_LINKS_GROUP Σύνδεσμοι
SETTINGS_GITHUB_REPO Πηγαίος κώδικας στο GitHub
SETTINGS_TAB_AUTOLINE Αυτόματη γραμμή
SETTINGS_TAB_STOPS Στάσεις και σταθμοί
SETTINGS_TAB_UNBUNCHING Αποφυγή συσσώρευσης
SETTINGS_TAB_DELETE Διαγραφή γραμμών
SETTINGS_TAB_FLEET Στόλος και προγραμματισμός
SETTINGS_TAB_BUDGET Προϋπολογισμός και τιμές
SETTINGS_TAB_LINECOLORS Χρώματα γραμμών
SETTINGS Ρυθμίσεις
SETTINGS_SPEED Εμφάνιση ταχύτητας σε: 
SETTINGS_SPEED_TOOLTIP Επιλέξτε τη μονάδα εμφάνισης ταχύτητας στη διεπαφή.
SETTINGS_GAMEPLAY_PROFILE Προφίλ παιχνιδιού
SETTINGS_GAMEPLAY_PROFILE_TOOLTIP Εφαρμόζει πακέτο ρυθμίσεων μονομιάς. Ασφαλές (προεπιλογή) αφήνει όλα off για μέγιστη συμβατότητα. Vanilla ταιριάζει στο βασικό παιχνίδι. Προτεινόμενο ενεργοποιεί μόνο τον πυρήνα IPT (έλεγχος στόλου προϋπολογισμού, αποφυγή συσσώρευσης, υπεραστικό, καρτέλες υποκτιρίων, unstucker, σύνθετη επιλογή στάσεων, υπερυψωμένες στάσεις). Ρεαλιστικό ενεργοποιεί τις περισσότερες ενσωματώσεις. Προσαρμοσμένο δεν κάνει cascade - διαχειρίζεστε κάθε διακόπτη μόνοι σας.
SETTINGS_GAMEPLAY_PROFILE_CUSTOM Προσαρμοσμένο
SETTINGS_GAMEPLAY_PROFILE_SAFE Ασφαλές (όλα off)
SETTINGS_GAMEPLAY_PROFILE_VANILLA Vanilla
SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED Προτεινόμενο (πυρήνας IPT)
SETTINGS_GAMEPLAY_PROFILE_REALISTIC Ρεαλιστικό
SETTINGS_SPEED_KPH χλμ/ώρα
SETTINGS_SPEED_MPH mph
SETTINGS_WALKING_SPEED Ταχύτητα περπατήματος/ποδηλάτου: 
SETTINGS_WALKING_SPEED_TOOLTIP Τυπικό: ταχύτητες βασικού παιχνιδιού.\nΡεαλιστικό: μειώνει το περπάτημα σε ρεαλιστικές τιμές ανά ηλικία.\nΡεαλιστικό μειώνει και το ποδήλατο με After Dark DLC.
SETTINGS_WALKING_SPEED_MODE_VANILLA Τυπικό
SETTINGS_WALKING_SPEED_MODE_REALISTIC Ρεαλιστικό
SETTINGS_BBSP Καλύτερη θέση στάσης λεωφορείου: 
SETTINGS_BBSP_TOOLTIP Ανενεργό: χωρίς προσαρμογή θέσης.\nΕνεργό: τα λεωφορεία σταματούν μπροστά στη στάση αντί στο κέντρο.
SETTINGS_BBSP_MODE_DISABLED Ανενεργό
SETTINGS_BBSP_MODE_ORIGINAL Ενεργό
SETTINGS_BBSP_MODE_UPDATED Χρήση πειραματικής λογικής
SETTINGS_BUDGET Προϋπολογισμός
SETTINGS_ENABLE_BUDGET_CONTROL Έλεγχος προϋπολογισμού γραμμής:
SETTINGS_BUDGET_CONTROL_DISABLED Ανενεργό
SETTINGS_BUDGET_CONTROL_ENABLED Ενεργό
SETTINGS_BUDGET_CONTROL_TOOLTIP Όταν ενεργό, ο αριθμός οχημάτων ελέγχεται από τον προϋπολογισμό· ενημερώνει όλες τις υπάρχουσες γραμμές και αδειάζει την ουρά.
SETTINGS_BUDGET_TICKET_PRICES Προσαρμογή τιμών εισιτηρίων:
SETTINGS_BUDGET_TICKET_PRICES_DISABLED Ανενεργό
SETTINGS_BUDGET_TICKET_PRICES_ENABLED Ενεργό
SETTINGS_BUDGET_TICKET_PRICES_TOOLTIP Όταν ενεργό, προσθέτει καρτέλα στο Οικονομία με ρυθμιστικά τιμών εισιτηρίων ανά τύπο μεταφοράς.
SETTINGS_AUTO_LINE_BUDGET Αυτόματο μέγεθος στόλου:
SETTINGS_AUTO_LINE_BUDGET_DISABLED Ανενεργό
SETTINGS_AUTO_LINE_BUDGET_ENABLED Ενεργό
SETTINGS_AUTO_LINE_BUDGET_TOOLTIP Όταν ενεργό, οι γραμμές σε λειτουργία Προϋπολογισμού προσαρμόζουν αυτόματα τον αριθμό οχημάτων στη πραγματική ζήτηση αντί για το vanilla ρυθμιστικό. Οι γραμμές σε Χειροκίνητο δεν αγγίζονται ποτέ.
SETTINGS_AUTO_LINE Αυτόματη γραμμή
SETTINGS_AUTOSHOW_LINE_INFO Αυτόματο άνοιγμα πάνελ πληροφοριών γραμμής
SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP Εμφανίζει αυτόματα το πάνελ πληροφοριών γραμμής μετά τη δημιουργία νέας γραμμής.
AUTOLINECOLOR_STRATEGY_DISABLED Ανενεργό
AUTOLINECOLOR_STRATEGY_RANDOM_HUE Τυχαία απόχρωση
AUTOLINECOLOR_STRATEGY_RANDOM_COLOR Τυχαίο χρώμα
AUTOLINECOLOR_STRATEGY_CATEGORISED Κατηγοριοποιημένο
AUTOLINECOLOR_STRATEGY_NAMED Ονομασμένα χρώματα
AUTOLINECOLOR_NAMING_DISABLED Ανενεργό
AUTOLINECOLOR_NAMING_DISTRICTS Συνοικίες
AUTOLINECOLOR_NAMING_LONDON Λονδίνο
AUTOLINECOLOR_NAMING_ROADS Δρόμοι
AUTOLINECOLOR_NAMING_COLORS Ονομασμένα χρώματα
AUTOLINECOLOR_COLOR_STRATEGY Στρατηγική χρώματος:
AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP Πώς ανατίθενται χρώματα σε νέες γραμμές:\n'Τυχαία απόχρωση' = ίδια κορεσμός/φωτεινότητα, διαφορετικές αποχρώσεις;\n'Τυχαίο χρώμα' = πλήρως τυχαίο RGB;\n'Κατηγοριοποιημένο' = χρώματα ανά τύπο οχήματος;\n'Ονομασμένα χρώματα' = προκαθορισμένη παλέτα.
AUTOLINECOLOR_NAMING_STRATEGY Στρατηγική ονομασίας:
AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP Πώς ανατίθενται ονόματα σε νέες γραμμές:\n'Κανένα' = χωρίς αυτόματη ονομασία;\n'Συνοικίες' = με βάση τις εξυπηρετούμενες συνοικίες;\n'Λονδίνο' = αριθμημένες διαδρομές (στυλ London Buses);\n'Δρόμοι' = με βάση ονόματα δρόμων;\n'Ονομασμένα χρώματα' = με βάση ονόματα χρωμάτων.
AUTOLINECOLOR_MIN_COLOR_DIFF Ελάχιστη διαφορά χρώματος (%):
AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP Ελάχιστο ποσοστό διαφοράς χρώματος στην επιλογή τυχαίων χρωμάτων.
AUTOLINECOLOR_MAX_COLOR_PICK Μέγιστες προσπάθειες:
AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP Μέγιστες προσπάθειες για επιλογή διακριτού χρώματος.
SETTINGS_UI Ρυθμίσεις UI
SETTINGS_VEHICLE_EDITOR_POSITION Θέση επεξεργαστή οχημάτων: 
SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP Επιλέξτε αν το πάνελ εμφανίζεται κάτω ή δεξιά.
SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM Κάτω
SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT Δεξιά
SETTINGS_VEHICLE_EDITOR_HIDE Απόκρυψη επεξεργαστή οχημάτων
SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP Απόκρυψη επεξεργαστή από τα πάνελ οχημάτων.
SETTINGS_STOPS Στάσεις δημόσιων συγκοινωνιών
SETTINGS_STOPSANDSTATIONS_DESCRIPTION Πόσοι επιβάτες κάθε τύπου μπορούν να περιμένουν σε μία στάση πριν θεωρηθεί γεμάτη. Υψηλότερες τιμές μειώνουν παράπονα συνωστισμού, με λιγότερο ρεαλιστικό μέγεθος ουράς.
SETTINGS_STOPSANDSTATIONS_RESET_TOOLTIP Επαναφορά όλων των ανώτατων ορίων επιβατών στις προεπιλογές.
SETTINGS_ENABLE_STOPS_AND_STATIONS Ενεργοποίηση στάσεων και σταθμών
SETTINGS_ENABLE_STOPS_AND_STATIONS_TOOLTIP Προσαρμόστε τον μέγιστο αριθμό πολιτών που περιμένουν σε στάσεις και σταθμούς. Ρύθμιση στην καρτέλα Στάσεις.
SETTINGS_STOPSANDSTATIONS_ENABLE Ενεργοποίηση στάσεων και σταθμών
SETTINGS_STOPSANDSTATIONS_ENABLE_TOOLTIP Ενεργοποίηση ή απενεργοποίηση ορίων επιβατών στις στάσεις.
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HEADER Μέγιστοι επιβάτες σε αναμονή σε στάση γραμμής:
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS Λεωφορείο γραμμής
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS_TOOLTIP Μέγιστοι επιβάτες σε στάση λεωφορείου γραμμής
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS Τρόλεϊ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS_TOOLTIP Μέγιστοι επιβάτες σε στάση τρόλεϊ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS Λεωφορείο εκκένωσης
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS_TOOLTIP Μέγιστοι επιβάτες σε στάση λεωφορείου εκκένωσης
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS Τουριστικό λεωφορείο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS_TOOLTIP Μέγιστοι επιβάτες σε στάση τουριστικού λεωφορείου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM Τραμ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM_TOOLTIP Μέγιστοι επιβάτες σε στάση τραμ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO Μετρό
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO_TOOLTIP Μέγιστοι επιβάτες σε σταθμό μετρό
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN Τρένο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN_TOOLTIP Μέγιστοι επιβάτες σε σιδηροδρομικό σταθμό
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL Μονόραγο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL_TOOLTIP Μέγιστοι επιβάτες σε στάση μονόραγου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP Πλοίο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP_TOOLTIP Μέγιστοι επιβάτες σε λιμάνι φορτίου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY Φέρι
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY_TOOLTIP Μέγιστοι επιβάτες σε στάση φέρι
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE Αεροπλάνο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE_TOOLTIP Μέγιστοι επιβάτες σε τερματικό αεροπλάνου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR Τελεφερίκ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR_TOOLTIP Μέγιστοι επιβάτες σε στάση τελεφερίκ
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON Αερόστατο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON_TOOLTIP Μέγιστοι επιβάτες σε στάση αερόστατου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER Ελικόπτερο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER_TOOLTIP Μέγιστοι επιβάτες σε στάση ελικοπτέρου
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP Αερόπλοιο
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP_TOOLTIP Μέγιστοι επιβάτες σε στάση αερόπλοιου
SETTINGS_UNBUNCHING Αποφυγή συσσώρευσης
SETTINGS_UNBUNCHING_AGGRESSION Επιθετικότητα αποφυγής:
SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP Πόσο δυνατά να δρα η αποφυγή συσσώρευσης;\nΤιμές: 0–52. Το 0 την απενεργοποιεί.\nΥψηλές τιμές επηρεάζουν έντονα την κυκλοφορία και μπορεί να εξαφανίσουν οχήματα.
SETTINGS_VEHICLE_COUNT Οχήματα σε νέες γραμμές:
SETTINGS_VEHICLE_COUNT_TOOLTIP Αριθμός οχημάτων που προστίθενται αυτόματα σε νέες γραμμές όταν ο έλεγχος προϋπολογισμού γραμμής είναι off.
SETTINGS_SPAWN_TIME_INTERVAL Διάστημα εμφάνισης:
SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP Χρόνος σε δευτερόλεπτα μεταξύ εμφανίσεων οχημάτων.
SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP Επαναφέρει όλα τα ρυθμιστικά στην προεπιλογή.
UNBUNCHING_ENABLED Αποφυγή συσσώρευσης οχημάτων
UNBUNCHING_DISABLED Η αποφυγή συσσώρευσης είναι ανενεργή.
UNBUNCHING_TARGET_GAP Στόχος κενού: {0}
SETTINGS_EBS_GROUP_BUS Υπηρεσίες λεωφορείου εξπρές
SETTINGS_EBS_GROUP_TRAM Υπηρεσίες τραμ εξπρές
SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE Λεωφορείο εξπρές: 
SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE 'Ανενεργό' = Τα λεωφορεία χρησιμοποιούν τις ρυθμίσεις αποφυγής παραπάνω.\n'Συνετό' = Σταματά λίγο, ελέγχει επιβάτες, μπορεί να φύγει άδειο.\n'Επιθετικό' = Παραλείπει στάσεις αν δεν περιμένει κανείς.
SETTINGS_EBS_ENABLE_SELFBAL Ενεργοποίηση αυτοεξισορρόπησης υπηρεσίας
SETTINGS_EBS_DESC_SELFBAL Επιτρέπει στο λεωφορείο εξπρές να αναδιανέμει οχήματα στη γραμμή υπέρ των πιο πολυσύχναστων τμημάτων.
SETTINGS_EBS_TOOLTIP_SELFBAL Αναλύει τμήματα γραμμής και μπορεί να μεταφέρει οχήματα σε πιο πολυσύχναστα τμήματα ή τέρματα.\nΟι αποφάσεις είναι πιθανοτικές και εξαρτώνται από αριθμούς επιβατών και πιθανότητες αναδιάταξης.
SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID Ενεργοποίηση αυτοεξισορρόπησης σε μεσαίες στάσεις
SETTINGS_EBS_DESC_SELFBAL_TARGETMID Επιτρέπει στην αυτοεξισορρόπηση να επιλέγει πολυσύχναστη μεσαία στάση αντί μόνο τέρματος.
SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID Επιτρέπει μεταφορά οχημάτων σε πολυσύχναστη μεσαία στάση (αντί τέρματος).\nΜόνο όταν η πιο πολυσύχναστη στάση έχει πάνω από 30 επιβάτες, μετά με ~50% πιθανότητα και υπό τις συνολικές πιθανότητες αναδιάταξης.
SETTINGS_EBS_ENABLE_MINIBUS Ενεργοποίηση λειτουργίας μίνι λεωφορείου
SETTINGS_EBS_DESC_MINIBUS Μικρότερα λεωφορεία φεύγουν νωρίτερα όταν λίγοι επιβάτες επιβιβάζονται/αποβιβάζονται.
SETTINGS_EBS_TOOLTIP_MINIBUS Λεωφορεία χωρητικότητας ≤20 μπορούν να φύγουν νωρίτερα όταν επιβιβάσεις + αποβιβάσεις ≤5.
SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE Τραμ εξπρές: 
SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING 'Ανενεργό' = Τα τραμ χρησιμοποιούν τις ρυθμίσεις αποφυγής παραπάνω.\n'Ελαφρύς σιδηρόδρομος' = Σταματά σε κάθε στάση, πάντα πλήρης χρονοδιακόπτης.\n'Αληθινό τραμ' = Σταματά μόνο όταν επιβιβάζονται/αποβιβάζονται επιβάτες.
SETTINGS_EBS_MODE_NONE Ανενεργό
SETTINGS_EBS_MODE_AGGRESSIVE Επιθετικό
SETTINGS_EBS_MODE_PRUDENTIAL Συνετό
SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL Λειτουργία ελαφρού σιδηροδρόμου
SETTINGS_EBS_TRAM_MODE_NONE Ανενεργό
SETTINGS_EBS_TRAM_MODE_TRAM Λειτουργία αληθινού τραμ
SETTINGS_PTU_GROUP Ξεκόλλημα δημόσιων συγκοινωνιών
SETTINGS_PTU_ENABLE Αφαίρεση κολλημένων επιβατών
SETTINGS_PTU_TOOLTIP Αφαιρεί αυτόματα επιβάτες που κολλάνε στην επιβίβαση, ώστε το όχημα να αναχωρεί κανονικά.
SETTINGS_LINE_DELETION_TOOL Εργαλείο διαγραφής γραμμών
SETTINGS_LINE_DELETION_TOOL_DESCRIPTION Επιλέξτε τύπους μεταφοράς παρακάτω και πατήστε Διαγραφή για αφαίρεση κάθε γραμμής αυτών των τύπων από την τρέχουσα πόλη. Η επιλογή είναι προσωρινή - ξεκινά πάντα μη επιλεγμένη και καθαρίζεται μετά τη διαγραφή· δεν αποθηκεύεται ως ρύθμιση.
SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP Διαγράφει όλες τις γραμμές των επιλεγμένων τύπων. Λειτουργεί μόνο με φορτωμένη πόλη.
SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE ΕΠΙΒΕΒΑΙΩΣΗ ΔΙΑΓΡΑΦΗΣ ΓΡΑΜΜΩΝ
SETTINGS_LINE_DELETION_TOOL_CONFIRM_MSG Πρόκειται να διαγράψετε όλες τις γραμμές.\nΘέλετε να συνεχίσετε;
SETTINGS_DELETE_BUS_TOOLTIP Διαγράφει όλες τις κανονικές γραμμές λεωφορείου.
SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL Τουριστικά λεωφορεία
SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP Διαγράφει όλες τις τουριστικές γραμμές λεωφορείου.
SETTINGS_DELETE_TRAM_TOOLTIP Διαγράφει όλες τις γραμμές τραμ.
SETTINGS_DELETE_TROLLEYBUS_TOOLTIP Διαγράφει όλες τις γραμμές τρόλεϊ.
SETTINGS_DELETE_TRAIN_TOOLTIP Διαγράφει όλες τις γραμμές τρένου.
SETTINGS_DELETE_METRO_TOOLTIP Διαγράφει όλες τις υπόγειες γραμμές μετρό.
SETTINGS_DELETE_MONORAIL_TOOLTIP Διαγράφει όλες τις γραμμές μονόραγου.
SETTINGS_DELETE_FERRY_LABEL Φέρι
SETTINGS_DELETE_SHIP_TOOLTIP Διαγράφει όλες τις γραμμές φέρι.
SETTINGS_DELETE_HELICOPTER_LABEL Ελικόπτερα
SETTINGS_DELETE_HELICOPTER_TOOLTIP Διαγράφει όλες τις γραμμές ελικοπτέρου.
SETTINGS_DELETE_BLIMP_LABEL Αερόπλοια
SETTINGS_DELETE_BLIMP_TOOLTIP Διαγράφει όλες τις γραμμές αερόπλοιου.
VEHICLE_EDITOR_TITLE Επεξεργαστής οχημάτων
VEHICLE_EDITOR_SUB_TITLE {0} οχήματα
VEHICLE_EDITOR_CAPACITY Χωρητικότητα επιβατών
VEHICLE_EDITOR_CAPACITY_TAXI Χωρητικότητα διαδρομής
VEHICLE_EDITOR_CAPACITY_TAXI_TOOLTIP Αριθμός επιβατών ανά βάρδια.
VEHICLE_EDITOR_MAINTENANCE Κόστος συντήρησης
VEHICLE_EDITOR_MAX_SPEED Μέγιστη ταχύτητα
VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS Μηχανή τρένου και στα δύο άκρα
VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS_TOOLTIP Ενεργοποιεί ή απενεργοποιεί μηχανές τρένου και στα δύο άκρα.
VEHICLE_EDITOR_APPLY Εφαρμογή
VEHICLE_EDITOR_DEFAULT Προεπιλογή
VEHICLE_LIST_BOX_ROW_TOOLTIP1 Δεξί κλικ για παρακολούθηση του οχήματος.\nΚρατήστε Shift στο κλικ για ζουμ.
VEHICLE_LIST_BOX_ROW_TOOLTIP2 Shift + κλικ για ουρά αυτού του οχήματος.
VEHICLE_PANEL_EDIT_TYPE Επεξεργασία τύπου οχήματος
VEHICLE_PANEL_EDIT_TYPE_TOOLTIP Επεξεργαστείτε αυτόν τον τύπο με τον επεξεργαστή οχημάτων.
VEHICLE_PANEL_STATUS_NEXT_STOP Επόμενη στάση:
VEHICLE_PANEL_STATUS_UNBUNCHING Αποφυγή συσσώρευσης σε εξέλιξη
VEHICLE_PANEL_LAST_STOP_EXCHANGE Ανταλλαγή επιβατών τελευταίας στάσης: <color #FF0000>-{0}</color> | <color #00FF00>+{1}</color>
VEHICLE_PANEL_PASSENGERS Επιβάτες:
VEHICLE_PANEL_EARNINGS Έσοδα:
VEHICLE_PANEL_EARNINGS_TOOLTIP Αποτέλεσμα πωλήσεων εισιτηρίων μείον κόστος συντήρησης οχήματος.
VEHICLE_PANEL_PREVIOUS Προηγούμενο όχημα
VEHICLE_PANEL_PREVIOUS_TOOLTIP Μετάβαση στο προηγούμενο όχημα.\nΚρατήστε Shift στο κλικ για ζουμ.
VEHICLE_PANEL_REMOVE_VEHICLE Αφαίρεση οχήματος
VEHICLE_PANEL_NEXT Επόμενο όχημα
VEHICLE_PANEL_NEXT_TOOLTIP Μετάβαση στο επόμενο όχημα.\nΚρατήστε Shift στο κλικ για ζουμ.
VEHICLE_SELECTION_CAPACITY Χωρητικότητα
VEHICLE_SELECTION_ADD_VEHICLE Προσθήκη αυτού του οχήματος στη λίστα επιτρεπόμενων
VEHICLE_SELECTION_ADD_ALL Προσθήκη όλων των κατάλληλων οχημάτων στη λίστα επιτρεπόμενων
VEHICLE_SELECTION_REMOVE_VEHICLE Αφαίρεση αυτού του οχήματος από τη λίστα επιτρεπόμενων
VEHICLE_SELECTION_REMOVE_ALL Αφαίρεση όλων των οχημάτων από τη λίστα επιτρεπόμενων
VEHICLE_SELECTION_AVAILABLE_VEHICLES Διαθέσιμα οχήματα
VEHICLE_SELECTION_SELECTED_VEHICLES Επιλεγμένα οχήματα
VEHICLE_SELECTION_ANY_VEHICLE Οποιοδήποτε όχημα
VEHICLE_BUTTON_TOOLTIP {0}\n\nΚλικ για μετάβαση σε αυτό το όχημα.\nΚρατήστε Shift στο κλικ για ζουμ.\nΚρατήστε Alt στο κλικ για να μην ανοίξει το πάνελ πληροφοριών οχήματος.
TRANSPORT_LINE_VEHICLECOUNT Αριθμός οχημάτων: {0}
FLIGHT_TRACKER_NAME Παρακολούθηση πτήσεων
FLIGHT_STATUS_NONE Κανένα
FLIGHT_STATUS_INCOMING Εισερχόμενο
FLIGHT_STATUS_LANDED Προσγειωμένο
FLIGHT_STATUS_AT_GATE Στην πύλη
FLIGHT_STATUS_DEPARTED Αναχωρημένο
TICKET_PRICE_TAXI_KILOMETER Τιμή ταξί ανά χιλιόμετρο: 
TICKET_PRICE_TAXI_MILE Τιμή ταξί ανά μίλι: 
TICKET_PRICE_BUS Τιμή εισιτηρίου λεωφορείου: 
TICKET_PRICE_INTERCITY_BUS Τιμή εισιτηρίου υπεραστικού: 
TICKET_PRICE_METRO Τιμή εισιτηρίου μετρό: 
TICKET_PRICE_TRAIN Τιμή εισιτηρίου τρένου: 
TICKET_PRICE_TRAM Τιμή εισιτηρίου τραμ: 
TICKET_PRICE_MONORAIL Τιμή εισιτηρίου μονόραγου: 
TICKET_PRICE_SHIP Τιμή εισιτηρίου πλοίου: 
TICKET_PRICE_FERRY Τιμή εισιτηρίου φέρι: 
TICKET_PRICE_PLANE Τιμή εισιτηρίου αεροπλάνου: 
TICKET_PRICE_CABLECAR Τιμή εισιτηρίου τελεφερίκ: 
TICKET_PRICE_SIGHTSEEING_BUS Τιμή εισιτηρίου τουριστικού: 
TICKET_PRICE_TROLLEYBUS Τιμή εισιτηρίου τρόλεϊ: 
TICKET_PRICE_BLIMP Τιμή εισιτηρίου αερόπλοιου: 
TICKET_PRICE_HELICOPTER Τιμή εισιτηρίου ελικοπτέρου: 
ECONOMY_TAB_TICKET_PRICES Τιμές εισιτηρίων
ECONOMY_TAB_TICKET_PRICES_TOOLTIP_PASSENGER_COUNT Συνολικός τρέχων αριθμός επιβατών για αυτόν τον τύπο μεταφοράς.
WHATSNEW_3_0_0_1 Ενημερώθηκε για Race Day· συμβατό με More Vehicles Renewed.
WHATSNEW_3_0_0_2 Τα εξής mods ενσωματώθηκαν στο IPT3:\n • Advanced Stop Selection Revisited\n • Auto Line Color Redux\n • Better Bus Stop Position\n • Better Train Boarding\n • Elevated Stops Enabler Revisited  \n • Express Bus Services\n • Flight Tracker\n • Intercity Bus Control\n • Mileage Taxi Services\n • Public Transport Unstucker\n • Realistic Walking Speed\n • Stops and Stations\n • Ticket Price Customizer
WHATSNEW_3_0_1 Το Public Transport Unstucker ενσωματώθηκε στο IPT3.\nΔείτε την έκδοση 3.0 για τα υπόλοιπα ενσωματωμένα mods. Καταργήστε την εγγραφή από τα πρωτότυπα για αποφυγή συγκρούσεων.
SETTINGS_TAB_TRAINDISPLAY Οθόνη τρένου
SETTINGS_TAB_INTEGRATIONS Ενσωματώσεις
SETTINGS_TRAINDISPLAY_GROUP Επικάλυψη οθόνης τρένου
SETTINGS_TRAINDISPLAY_GROUP_DESCRIPTION Ρυθμίστε την ενσωματωμένη επικάλυψη κατά την παρακολούθηση υποστηριζόμενων οχημάτων.
SETTINGS_TRAINDISPLAY_ENABLE Ενεργοποίηση οθόνης τρένου
SETTINGS_TRAINDISPLAY_ENABLE_TOOLTIP Ενεργοποίηση ή απενεργοποίηση της ενσωματωμένης επικάλυψης οθόνης τρένου.
SETTINGS_TRAINDISPLAY_MODE_DISABLED Ανενεργό
SETTINGS_TRAINDISPLAY_MODE_ENABLED Ενεργό
SETTINGS_TRAINDISPLAY_OVERLAY_POSITION Θέση επικάλυψης:
SETTINGS_TRAINDISPLAY_OVERLAY_POSITION_TOOLTIP Επιλέξτε πού εμφανίζεται η επικάλυψη στην οθόνη.
SETTINGS_TRAINDISPLAY_POS_TOPLEFT Πάνω αριστερά
SETTINGS_TRAINDISPLAY_POS_TOPRIGHT Πάνω δεξιά
SETTINGS_TRAINDISPLAY_POS_BOTTOMLEFT Κάτω αριστερά
SETTINGS_TRAINDISPLAY_POS_BOTTOMRIGHT Κάτω δεξιά
SETTINGS_TRAINDISPLAY_OVERLAY_SCALE Κλίμακα επικάλυψης:
SETTINGS_TRAINDISPLAY_OVERLAY_SCALE_TOOLTIP Κλιμάκωση μεγέθους επικάλυψης.
SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY Αδιαφάνεια επικάλυψης:
SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY_TOOLTIP Ρυθμίστε πόσο διαφανής είναι η επικάλυψη.
SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL Διάστημα ενημέρωσης:
SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL_TOOLTIP Πόσο συχνά ανανεώνεται η επικάλυψη κατά την παρακολούθηση.
SETTINGS_TRAINDISPLAY_SHOW_LINE Εμφάνιση ονόματος γραμμής
SETTINGS_TRAINDISPLAY_SHOW_LINE_TOOLTIP Συμπερίληψη ονόματος γραμμής στην επικάλυψη.
SETTINGS_TRAINDISPLAY_SHOW_DESTINATION Εμφάνιση προορισμού
SETTINGS_TRAINDISPLAY_SHOW_DESTINATION_TOOLTIP Συμπερίληψη προορισμού στην επικάλυψη.
SETTINGS_TRAINDISPLAY_SHOW_STATE Εμφάνιση κατάστασης
SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP Συμπερίληψη κατάστασης οχήματος στην επικάλυψη.
SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING Μόνο κατά την παρακολούθηση
SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING_TOOLTIP Απόκρυψη εκτός αν η κάμερα παρακολουθεί πραγματικά υποστηριζόμενο όχημα.
SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY Μόνο σε κάμερα πρώτου προσώπου
SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY_TOOLTIP Εμφάνιση μόνο με mod κάμερας πρώτου προσώπου (π.χ. First Person Camera - Continued). Αν off, εμφανίζεται σε οποιαδήποτε λειτουργία κάμερας.
SETTINGS_TRAINDISPLAY_THEME Θέμα χρώματος:
SETTINGS_TRAINDISPLAY_THEME_TOOLTIP Επιλέξτε χρώματα φόντου/κειμένου της επικάλυψης.
SETTINGS_TRAINDISPLAY_THEME_SIMPLE Απλό
SETTINGS_TRAINDISPLAY_THEME_DARK Σκούρο
SETTINGS_TRAINDISPLAY_THEME_LIGHT Φωτεινό
SETTINGS_TRAINDISPLAY_THEME_ORIGINAL Αρχικό
SETTINGS_TRAINDISPLAY_THEME_BLUE Μπλε
SETTINGS_TRAINDISPLAY_THEME_GREEN Πράσινο
SETTINGS_TRAINDISPLAY_THEME_AMBER Κεχριμπάρι
COPY_TIP Αντιγραφή ρυθμίσεων αυτής της γραμμής.
PASTE_TIP Επικόλληση αντιγραμμένων ρυθμίσεων γραμμής.
COPY_BUILDING_TIP Αντιγραφή αυτών των ρυθμίσεων σε όλες τις γραμμές που εξυπηρετούν αυτό το κτίριο.
COPY_DISTRICT_TIP Αντιγραφή αυτών των ρυθμίσεων σε όλες τις γραμμές αυτής της συνοικίας.
SETTINGS_INTEGRATIONS_GROUP Ενσωματωμένα πρόσθετα
SETTINGS_INTERCITY_BUS_ENABLE Ενεργοποίηση ελέγχου υπεραστικών λεωφορείων
SETTINGS_INTERCITY_BUS_ENABLE_TOOLTIP Ενεργοποιεί το ενσωματωμένο επίπεδο συμβατότητας υπεραστικών λεωφορείων και τα patches σταθμών.
SETTINGS_ADVANCEDSTOPSELECTION_ENABLE Ενεργοποίηση σύνθετης επιλογής στάσεων
SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP Επιτρέπει τοποθέτηση στάσεων σε εναλλακτικές αποβάθρες/τροχιές πολυτροχιακών σταθμών (κρατήστε το πλήκτρο εναλλακτικής λειτουργίας). Ισχύει στην επόμενη φόρτωση επιπέδου.
SETTINGS_BETTERBOARDING_ENABLE Ενεργοποίηση καλύτερης επιβίβασης
SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP Βελτιώνει τις αποφάσεις επιβίβασης ώστε οι επιβάτες να προτιμούν το όχημα που εξυπηρετεί τον προορισμό τους. Ισχύει στην επόμενη φόρτωση επιπέδου.
SETTINGS_MILEAGETAXI_ENABLE Ενεργοποίηση ταξί χιλιομέτρων
SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP Χρεώνει ταξί με βάση την απόσταση αντί σταθερής τιμής. Απαιτεί After Dark DLC. Ισχύει στην επόμενη φόρτωση επιπέδου.
SETTINGS_ELEVATEDSTOPS_ENABLE Ενεργοποίηση υπερυψωμένων στάσεων
SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP Επιτρέπει στάσεις δημόσιων συγκοινωνιών σε υπερυψωμένους δρόμους/γέφυρες και διατηρεί φωτισμό. Ισχύει στην επόμενη φόρτωση επιπέδου.
SETTINGS_INTERCITY_BUS_CAPACITY Χωρητικότητα τερματικού υπεραστικών
SETTINGS_INTERCITY_BUS_CAPACITY_TOOLTIP Πόσα οχήματα χωράει ταυτόχρονα ένα τερματικό υπεραστικών λεωφορείων.
SETTINGS_TRAM_DEPOT_CAPACITY Χωρητικότητα αμαξοστασίου τραμ
SETTINGS_TRAM_DEPOT_CAPACITY_TOOLTIP Το vanilla ορίζει κάθε αμαξοστάσιο τραμ σε πρακτικά απεριόριστο όριο 100.000 οχημάτων. Ρεαλιστικό και Ενδιάμεσο εφαρμόζουν μέτριο σταθερό όριο· Ανενεργό διατηρεί την υπάρχουσα συμπεριφορά.
SETTINGS_TAXI_DEPOT_CAPACITY Χωρητικότητα αμαξοστασίου ταξί
SETTINGS_TAXI_DEPOT_CAPACITY_TOOLTIP Το vanilla ορίζει κάθε αμαξοστάσιο ταξί σε πρακτικά απεριόριστο όριο 100.000 οχημάτων. Ρεαλιστικό και Ενδιάμεσο εφαρμόζουν μέτριο σταθερό όριο· Ανενεργό διατηρεί την υπάρχουσα συμπεριφορά.
SETTINGS_BUS_DEPOT_CAPACITY Χωρητικότητα αμαξοστασίου λεωφορείου
SETTINGS_BUS_DEPOT_CAPACITY_TOOLTIP Το vanilla ορίζει κάθε αμαξοστάσιο λεωφορείου (κανονικό, βιοκαύσιμο και γκαράζ τουριστικών) σε πρακτικά απεριόριστο όριο 100.000 οχημάτων. Ρεαλιστικό και Ενδιάμεσο εφαρμόζουν μέτριο σταθερό όριο· Ανενεργό διατηρεί την υπάρχουσα συμπεριφορά.
SETTINGS_TROLLEYBUS_DEPOT_CAPACITY Χωρητικότητα αμαξοστασίου τρόλεϊ
SETTINGS_TROLLEYBUS_DEPOT_CAPACITY_TOOLTIP Το vanilla ορίζει κάθε αμαξοστάσιο τρόλεϊ σε πρακτικά απεριόριστο όριο 100.000 οχημάτων. Ρεαλιστικό και Ενδιάμεσο εφαρμόζουν μέτριο σταθερό όριο· Ανενεργό διατηρεί την υπάρχουσα συμπεριφορά.
SETTINGS_FERRY_DEPOT_CAPACITY Χωρητικότητα αμαξοστασίου φέρι
SETTINGS_FERRY_DEPOT_CAPACITY_TOOLTIP Το vanilla ορίζει κάθε αμαξοστάσιο φέρι σε πρακτικά απεριόριστο όριο 100.000 οχημάτων. Ρεαλιστικό και Ενδιάμεσο εφαρμόζουν μέτριο σταθερό όριο· Ανενεργό διατηρεί την υπάρχουσα συμπεριφορά.
SETTINGS_DEPOT_CAPACITY_DISABLED Ανενεργό (χωρίς όριο)
SETTINGS_DEPOT_CAPACITY_INTERMEDIATE Ενδιάμεσο
SETTINGS_DEPOT_CAPACITY_REALISTIC Ρεαλιστικό
SETTINGS_FLIGHTTRACKER_ENABLE Ενεργοποίηση παρακολούθησης πτήσεων
SETTINGS_FLIGHTTRACKER_ENABLE_TOOLTIP Ενεργοποιεί τα ενσωματωμένα patches παρακολούθησης πτήσεων και την υποστήριξη UI.
SETTINGS_SUBBUILDINGSTABS_ENABLE Ενεργοποίηση καρτελών υποκτιρίων
SETTINGS_SUBBUILDINGSTABS_ENABLE_TOOLTIP Εμφανίζει λωρίδα καρτελών στο πάνελ πληροφοριών κτιρίου όταν έχει υποκτίρια (π.χ. αεροδρόμιο με μετρό), για εναλλαγή μεταξύ τους.
SETTINGS_TAXISTANDFIX_ENABLE Ενεργοποίηση διόρθωσης στάσης ταξί
SETTINGS_TAXISTANDFIX_ENABLE_TOOLTIP Στέλνει αδρανή ταξί στην πλησιέστερη στάση αντί να περιπλανώνται τυχαία. Απαιτεί After Dark DLC.
SETTINGS_SHAREDSTOPENABLER_ENABLE Ενεργοποίηση κοινών στάσεων
SETTINGS_SHAREDSTOPENABLER_ENABLE_TOOLTIP Επιτρέπει περισσότερους από έναν τύπους μεταφοράς (λεωφορείο, τραμ, τρόλεϊ) στην ίδια οδική ενότητα. Από προεπιλογή off - δείτε το GitHub του mod για τι παραλείπει αυτή η μειωμένη έκδοση.
SETTINGS_COMMUTERDESTINATION_ENABLE Ενεργοποίηση προορισμού επιβατών (ανασχεδιασμός σε αναμονή)
SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP Προσωρινά μη διαθέσιμο: το προηγούμενο πάνελ διπλασίαζε το UI πληροφοριών στάσης και είναι αναγκαστικά off μέχρι ανασχεδιασμό. Το πλαίσιο ελέγχου δεν το επανενεργοποιεί.
SETTINGS_OOC_ENABLE Βελτιστοποιημένες εξωτερικές συνδέσεις
SETTINGS_OOC_ENABLE_TOOLTIP Τρένα φορτίου, αεροπλάνα και πλοία περιμένουν περισσότερο για πληρέστερο φορτίο πριν αναχωρήσουν σε εξωτερικές συνδέσεις.
SETTINGS_OOC_WAIT_MULTIPLIER Πολλαπλασιαστής αναμονής
SETTINGS_OOC_WAIT_MULTIPLIER_TOOLTIP Πόσο περισσότερο από το vanilla να περιμένει για πληρέστερο φορτίο. Υψηλότερες τιμές σημαίνουν λιγότερα, πληρέστερα ταξίδια.
SETTINGS_OOC_PASSENGER_SCOPE Εύρος αναμονής επιβατών
SETTINGS_OOC_PASSENGER_SCOPE_TOOLTIP Πού ισχύει ο πολλαπλασιαστής αναμονής για πολίτες που περιμένουν δημόσιες συγκοινωνίες. Μόνο εξωτερικές συνδέσεις επηρεάζει μόνο όσους περιμένουν σε εξωτερική σύνδεση· σε όλη την πόλη επιβραδύνει και την κανονική εγχώρια αναμονή για κάθε πολίτη.
SETTINGS_OOC_PASSENGER_SCOPE_OUTSIDE Μόνο εξωτερικές συνδέσεις
SETTINGS_OOC_PASSENGER_SCOPE_CITYWIDE Σε όλη την πόλη
SETTINGS_OOC_PASSENGER_SCOPE_DISABLED Ανενεργό (vanilla)
SETTINGS_OOC_DISABLE_DUMMY Απενεργοποίηση διακοσμητικής διερχόμενης κυκλοφορίας
SETTINGS_OOC_DISABLE_DUMMY_ROAD Απενεργοποίηση διακοσμητικής οδικής κυκλοφορίας
SETTINGS_OOC_DISABLE_DUMMY_TRAIN Απενεργοποίηση διακοσμητικής κυκλοφορίας τρένων
SETTINGS_OOC_DISABLE_DUMMY_PLANE Απενεργοποίηση διακοσμητικής εναέριας κυκλοφορίας
SETTINGS_OOC_DISABLE_DUMMY_SHIP Απενεργοποίηση διακοσμητικής θαλάσσιας κυκλοφορίας
SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP Οι εξωτερικές συνδέσεις κανονικά δημιουργούν επιπλέον διακοσμητική κυκλοφορία που δεν εισέρχεται/εξέρχεται πραγματικά από την πόλη, μόνο για ατμόσφαιρα. Η απενεργοποίηση την αφαιρεί χωρίς να αγγίζει πραγματική ροή εισαγωγής/εξαγωγής/επιβατών.
SETTINGS_UOC_ENABLE Απεριόριστες εξωτερικές συνδέσεις
SETTINGS_UOC_ENABLE_TOOLTIP Αφαιρεί το vanilla όριο 4 συνδέσεων για δρόμους, τροχιές, θαλάσσιες και εναέριες διαδρομές, και συνδέει αναδρομικά γραμμές όταν χτίζεται νέα σύνδεση κοντά σε υπάρχοντα σταθμό. Ισχύει στην επόμενη φόρτωση επιπέδου.
SETTINGS_STTAI_ENABLE Αποφυγή σύγκρουσης σε μονή τροχιά
SETTINGS_STTAI_ENABLE_TOOLTIP Δεσμεύει τμήμα μονής τροχιάς για ένα τρένο τη φορά, κρατώντας το αντίθετο στην είσοδο μέχρι να αδειάσει. Πρωτότυπη λειτουργία IPT4, δεν υπάρχει στο vanilla ή σε απορροφημένο mod.
SETTINGS_STOPSTACKER_ENABLE Στοίβαξη θέσεων στάσης λεωφορείου
SETTINGS_STOPSTACKER_ENABLE_TOOLTIP Επιτρέπει σε δεύτερο/τρίτο λεωφορείο που πλησιάζει την ίδια στάση να χρησιμοποιεί δική του θέση πιο πίσω στη λωρίδα στάσης αντί να περιμένει σε μονή σειρά πίσω από το πρώτο, ώστε περισσότερα λεωφορεία να φορτώνουν/ξεφορτώνουν μαζί. Πρωτότυπη λειτουργία IPT4 (απλοποιημένη clean-room επανυλοποίηση), δεν υπάρχει στο vanilla ή σε απορροφημένο mod.
TRAINDISPLAY_LABEL_NAME Όνομα
TRAINDISPLAY_LABEL_STATUS Κατάσταση
TRAINDISPLAY_NO_LINE Χωρίς γραμμή
TRAINDISPLAY_NO_DESTINATION Χωρίς προορισμό
TRAINDISPLAY_HIDDEN Κρυφό
TRAINDISPLAY_VEHICLE Όχημα
TRAINDISPLAY_STATE_RETURNING Επιστρέφει
TRAINDISPLAY_STATE_STOPPED Στη στάση
TRAINDISPLAY_STATE_EN_ROUTE Σε διαδρομή
TRAINDISPLAY_STATE_ON_LINE Στη γραμμή
TRAINDISPLAY_STATE_IDLE Αδρανές
AUTOLINECOLOR_REFRESH_BUTTON Ανανέωση ονόματος/χρώματος
AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP Επαναανάθεση τρέχοντος ονόματος και χρώματος γραμμής σύμφωνα με τις ενεργές ρυθμίσεις AutoLineColor.
AUTOLINECOLOR_REFRESH_DISABLED_TOOLTIP Ενεργοποιήστε στρατηγική χρώματος ή ονομασίας στις Ρυθμίσεις πριν ανανεώσετε αυτή τη γραμμή.
TICKET_PRICE_LABEL_TOOLTIP Τρέχουσα τιμή: {0}\nΑρχική τιμή: {1}\nΕπιβάτες τώρα: {2}
"""

for line in _EL.strip().splitlines():
    if not line.strip():
        continue
    i = line.find(" ")
    if i > 0:
        EL[line[:i]] = line[i + 1 :]


if __name__ == "__main__":
    from lang_packs_all import emit

    emit("el", EL)
