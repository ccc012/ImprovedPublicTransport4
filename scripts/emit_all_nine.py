# -*- coding: utf-8 -*-
"""Emit da/fi/no/sv/hu/ro/el/vi/ms full translation files.

Usage (from scripts/):
  python emit_all_nine.py

da.txt and no.txt are already complete on disk; this rewrites no/sv/fi/hu from packs,
ms from Indonesian with Malay vocabulary, and ro/el/vi if pack modules exist.
"""
from __future__ import print_function
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Translations"
sys.path.insert(0, str(Path(__file__).resolve().parent))


def parse(path):
    order, keys = [], {}
    for line in path.read_text(encoding="utf-8").splitlines():
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


def write_lang(lang, tr, en_keys, order):
    lines = []
    missing = []
    for k in order:
        if k is None:
            lines.append("")
            continue
        if k.startswith("CHANGELOG_"):
            val = en_keys[k]
        else:
            val = tr.get(k)
            if val is None:
                missing.append(k)
                val = en_keys[k]
        lines.append("%s %s" % (k, val))
    (ROOT / ("%s.txt" % lang)).write_text("\n".join(lines) + "\n", encoding="utf-8")
    non_cl = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and tr.get(k, v) == v
    )
    total = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else tr.get(k, en_keys[k])) == v
    )
    cl = sum(1 for k in en_keys if k.startswith("CHANGELOG_"))
    print(
        "%s: missing=%d non-CL same_as_en=%d total_same_as_en=%d (CL=%d)"
        % (lang, len(missing), non_cl, total, cl)
    )
    if missing:
        print("  missing:", missing[:8])
    return non_cl


def malay_from_id(id_keys, en_keys):
    reps = [
        (r"Kendaraan", "Kenderaan"),
        (r"kendaraan", "kenderaan"),
        (r"Pemberhentian", "Perhentian"),
        (r"pemberhentian", "perhentian"),
        (r"Depo", "Depot"),
        (r"depo", "depot"),
        (r"Antrean", "Barisan"),
        (r"antrean", "barisan"),
        (r"Hapus", "Padam"),
        (r"hapus", "padam"),
        (r"Atur Ulang", "Set semula"),
        (r"Tautan", "Pautan"),
        (r"Kode sumber", "Kod sumber"),
        (r"Jalur", "Laluan"),
        (r"jalur", "laluan"),
        (r"Penjadwalan", "Penjadualan"),
        (r"Anggaran", "Bajet"),
        (r"anggaran", "bajet"),
        (r"Pengaturan", "Tetapan"),
        (r"pengaturan", "tetapan"),
        (r"Kecepatan", "Kelajuan"),
        (r"kecepatan", "kelajuan"),
        (r"Tampilkan", "Tunjuk"),
        (r"tampilkan", "tunjuk"),
        (r"Kustom", "Tersuai"),
        (r"Aman \(semua mati\)", "Selamat (semua dimatikan)"),
        (r"Direkomendasikan", "Disyorkan"),
        (r"Realistis", "Realistik"),
        (r"realistis", "realistik"),
        (r"Standar", "Standard"),
        (r"standar", "standard"),
        (r"Bersepeda", "Berbasikal"),
        (r"bersepeda", "berbasikal"),
        (r"Halte", "Perhentian"),
        (r"halte", "perhentian"),
        (r"\bbus\b", "bas"),
        (r"\bBus\b", "Bas"),
        (r"Nonaktif", "Dilumpuhkan"),
        (r"nonaktif", "dilumpuhkan"),
        (r"Aktifkan", "Dayakan"),
        (r"aktifkan", "dayakan"),
        (r"\bAktif\b", "Didayakan"),
        (r"\baktif\b", "didayakan"),
        (r"Otomatis", "Automatik"),
        (r"otomatis", "automatik"),
        (r"Pemisahan", "Elak berumpun"),
        (r"pemisahan", "elak berumpun"),
        (r"Agresivitas", "Keagresifan"),
        (r"agresivitas", "keagresifan"),
        (r"Layanan", "Perkhidmatan"),
        (r"layanan", "perkhidmatan"),
        (r"Hati-hati", "Berhemah"),
        (r"Distrik", "Daerah"),
        (r"distrik", "daerah"),
        (r"taksi", "teksi"),
        (r"Taksi", "Teksi"),
        (r"antarkota", "antara bandar"),
        (r"Antarkota", "Antara bandar"),
        (r"kota\b", "bandar"),
        (r"Kota\b", "Bandar"),
        (r"Opsi", "Pilihan"),
        (r"opsi", "pilihan"),
        (r"Rata-rata", "Purata"),
        (r"rata-rata", "purata"),
        (r"Izinkan", "Benarkan"),
        (r"izinkan", "benarkan"),
        (r"Kontrol", "Kawalan"),
        (r"kontrol", "kawalan"),
        (r"Tombol", "Butang"),
        (r"tombol", "butang"),
        (r"mengaktifkan atau menonaktifkan", "mendayakan atau melumpuhkan"),
        (r"Mengaktifkan atau menonaktifkan", "Mendayakan atau melumpuhkan"),
        (r"dinonaktifkan", "dilumpuhkan"),
        (r"Tidak ada", "Tiada"),
        (r"tidak ada", "tiada"),
        (r"Menghitung", "Mengira"),
        (r"Perbarui", "Kemas kini"),
        (r"Berikutnya", "Seterusnya"),
        (r"KONFIRMASI", "SAHKAN"),
        (r"Apakah Anda", "Adakah anda"),
        (r"Kapasitas", "Kapasiti"),
        (r"kapasitas", "kapasiti"),
        (r"perawatan", "penyelenggaraan"),
        (r"Perawatan", "Penyelenggaraan"),
        (r"Editor", "Penyunting"),
        (r"memperbesar tampilan", "menzum masuk"),
        (r"bawaan", "lalai"),
        (r"Bawaan", "Lalai"),
        (r"penggeser", "peluncur"),
        (r"Penggeser", "Peluncur"),
        (r"lalu lintas", "trafik"),
        (r"Lalu lintas", "Trafik"),
        (r"koneksi", "sambungan"),
        (r"Koneksi", "Sambungan"),
        (r"tabrakan", "perlanggaran"),
        (r"Tabrakan", "Perlanggaran"),
        (r"Hamparan", "Tindanan"),
        (r"hamparan", "tindanan"),
        (r"Transportasi", "Pengangkutan"),
        (r"transportasi", "pengangkutan"),
        (r"Tempel", "Tampal"),
        (r"Segarkan", "Muat semula"),
        (r"Tarif", "Tambang"),
        (r"tarif", "tambang"),
        (r"Minggu lalu", "Minggu lepas"),
        (r"Anda", "Anda"),
        (r"wisata", "pelancongan"),
        (r"Wisata", "Pelancongan"),
    ]
    out = {}
    for k, v in id_keys.items():
        if k.startswith("CHANGELOG_"):
            continue
        s = v
        for pat, rep in reps:
            s = re.sub(pat, rep, s)
        out[k] = s
    overrides = {
        "MOD_DESCRIPTION": "Pengangkutan awam diperbaiki: kawalan laluan, armada, integrasi dan lagi.",
        "SETTINGS_TAB_UNBUNCHING": "Elak berumpun",
        "SETTINGS_TAB_DELETE": "Padam laluan",
        "SETTINGS_TAB_FLEET": "Armada & penjadualan",
        "SETTINGS_TAB_BUDGET": "Bajet & harga",
        "SETTINGS_DELETE": "Padam",
        "SETTINGS_RESET": "Set semula",
        "SETTINGS": "Tetapan",
        "SETTINGS_SPEED_KPH": "km/j",
        "SETTINGS_GAMEPLAY_PROFILE_SAFE": "Selamat (semua dimatikan)",
        "SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED": "Disyorkan (teras IPT)",
        "SETTINGS_GAMEPLAY_PROFILE_REALISTIC": "Realistik",
        "SETTINGS_GAMEPLAY_PROFILE_CUSTOM": "Tersuai",
        "SETTINGS_BBSP_MODE_DISABLED": "Dilumpuhkan",
        "SETTINGS_BBSP_MODE_ORIGINAL": "Didayakan",
        "SETTINGS_BUDGET_CONTROL_DISABLED": "Dilumpuhkan",
        "SETTINGS_BUDGET_CONTROL_ENABLED": "Didayakan",
        "SETTINGS_DEPOT_CAPACITY_DISABLED": "Dilumpuhkan (tiada had)",
        "SETTINGS_DEPOT_CAPACITY_INTERMEDIATE": "Sederhana",
        "SETTINGS_DEPOT_CAPACITY_REALISTIC": "Realistik",
        "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE": "Dayakan pemilihan perhentian lanjutan",
        "TRAINDISPLAY_NO_LINE": "Tiada laluan",
        "TRAINDISPLAY_NO_DESTINATION": "Tiada destinasi",
        "TRAINDISPLAY_VEHICLE": "Kenderaan",
        "TRAINDISPLAY_STATE_RETURNING": "Pulang",
        "TRAINDISPLAY_STATE_STOPPED": "Di perhentian",
        "TRAINDISPLAY_STATE_ON_LINE": "Di laluan",
        "TRAINDISPLAY_STATE_IDLE": "Melahu",
        "SETTINGS_INTEGRATIONS_GROUP": "Add-on bersepadu",
        "SETTINGS_PTU_ENABLE": "Buang penumpang tersangkut",
        "UNBUNCHING_DISABLED": "Elak berumpun dilumpuhkan.",
        "SETTINGS_EBS_MODE_NONE": "Dilumpuhkan",
        "SETTINGS_EBS_TRAM_MODE_NONE": "Dilumpuhkan",
        "SETTINGS_TRAINDISPLAY_MODE_DISABLED": "Dilumpuhkan",
        "SETTINGS_TRAINDISPLAY_MODE_ENABLED": "Didayakan",
        "AUTOLINECOLOR_STRATEGY_DISABLED": "Dilumpuhkan",
        "AUTOLINECOLOR_NAMING_DISABLED": "Dilumpuhkan",
        "SETTINGS_AUTO_LINE_BUDGET_DISABLED": "Dilumpuhkan",
        "SETTINGS_AUTO_LINE_BUDGET_ENABLED": "Didayakan",
        "SETTINGS_BUDGET_TICKET_PRICES_DISABLED": "Dilumpuhkan",
        "SETTINGS_BUDGET_TICKET_PRICES_ENABLED": "Didayakan",
        "SETTINGS_OOC_PASSENGER_SCOPE_DISABLED": "Dilumpuhkan (vanilla)",
        "FLIGHT_STATUS_NONE": "Tiada",
        "SETTINGS_WALKING_SPEED_MODE_VANILLA": "Standard",
        "SETTINGS_WALKING_SPEED_MODE_REALISTIC": "Realistik",
    }
    out.update(overrides)
    for k, v in en_keys.items():
        if not k.startswith("CHANGELOG_") and k not in out:
            out[k] = v
    return out


def main():
    en_keys, order = parse(ROOT / "en.txt")
    results = {}

    # Report da
    da, _ = parse(ROOT / "da.txt")
    results["da"] = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and da.get(k) == v
    )
    print("da: non-CL same_as_en=%d (hand-written)" % results["da"])

    from lang_packs_all import NO
    from lang_packs_sv_fi import SV, FI
    from lang_packs_hu_ro import HU

    for lang, tr in [("no", NO), ("sv", SV), ("fi", FI), ("hu", HU)]:
        results[lang] = write_lang(lang, tr, en_keys, order)

    id_keys, _ = parse(ROOT / "id.txt")
    results["ms"] = write_lang("ms", malay_from_id(id_keys, en_keys), en_keys, order)

    for mod_name, attr, lang in [
        ("lang_packs_ro", "RO", "ro"),
        ("lang_packs_vi", "VI", "vi"),
        ("lang_packs_el", "EL", "el"),
    ]:
        try:
            mod = __import__(mod_name)
            tr = getattr(mod, attr)
            results[lang] = write_lang(lang, tr, en_keys, order)
        except Exception as e:
            print("%s not ready: %s" % (lang, e))
            results[lang] = None

    print("\n=== SUMMARY (non-CL same_as_en; expect ~0; CHANGELOG stays EN) ===")
    for lang in ["da", "fi", "no", "sv", "hu", "ro", "el", "vi", "ms"]:
        print("  %s: %s" % (lang, results.get(lang)))


if __name__ == "__main__":
    main()
