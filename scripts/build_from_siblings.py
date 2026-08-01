# -*- coding: utf-8 -*-
"""Build remaining language packs by writing complete .txt files using
sibling full translations + native overlays for keys that must differ.

Strategy for ms: start from id.txt (close to Malay), apply MS-specific overrides.
For others: load complete dict modules.
"""
from __future__ import print_function
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1] / "Translations"
SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))


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
        lines.append(f"{k} {val}")
    (ROOT / f"{lang}.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    non_cl_same = sum(
        1
        for k, v in en_keys.items()
        if not k.startswith("CHANGELOG_") and tr.get(k, v) == v
    )
    total_same = sum(
        1
        for k, v in en_keys.items()
        if (en_keys[k] if k.startswith("CHANGELOG_") else tr.get(k, en_keys[k])) == v
    )
    cl = sum(1 for k in en_keys if k.startswith("CHANGELOG_"))
    print(
        f"{lang}: missing={len(missing)} non-CL same_as_en={non_cl_same} "
        f"total_same_as_en={total_same} (CL={cl})"
    )
    if missing[:8]:
        print("  missing:", missing[:8])
    return non_cl_same


def malay_from_indonesian(id_keys, en_keys):
    """Convert Indonesian pack to Malay with systematic replacements + overrides."""
    # Word-level replacements (order matters for multiword)
    reps = [
        (r"\bAktifkan\b", "Dayakan"),
        (r"\bNonaktifkan\b", "Lumpuhkan"),
        (r"\bNonaktif\b", "Dilumpuhkan"),
        (r"\bAktif\b", "Didayakan"),
        (r"\bPengaturan\b", "Tetapan"),
        (r"\bpengaturan\b", "tetapan"),
        (r"\bPengaturan\b", "Tetapan"),
        (r"\bKendaraan\b", "Kenderaan"),
        (r"\bkendaraan\b", "kenderaan"),
        (r"\bPemberhentian\b", "Perhentian"),
        (r"\bpemberhentian\b", "perhentian"),
        (r"\bDepo\b", "Depot"),
        (r"\bdepo\b", "depot"),
        (r"\bAntrean\b", "Barisan"),
        (r"\bantrean\b", "barisan"),
        (r"\bmengantrekan\b", "memasukkan ke barisan"),
        (r"\bmengantre\b", "berbaris"),
        (r"\bHapus\b", "Padam"),
        (r"\bhapus\b", "padam"),
        (r"\bAtur Ulang\b", "Set semula"),
        (r"\bUmum\b", "Umum"),
        (r"\bTautan\b", "Pautan"),
        (r"\bKode sumber\b", "Kod sumber"),
        (r"\bJalur\b", "Laluan"),
        (r"\bjalur\b", "laluan"),
        (r"\bArmada\b", "Armada"),
        (r"\bPenjadwalan\b", "Penjadualan"),
        (r"\bAnggaran\b", "Bajet"),
        (r"\banggaran\b", "bajet"),
        (r"\bHarga\b", "Harga"),
        (r"\bWarna\b", "Warna"),
        (r"\bKecepatan\b", "Kelajuan"),
        (r"\bkecepatan\b", "kelajuan"),
        (r"\bTampilkan\b", "Tunjuk"),
        (r"\btampilkan\b", "tunjuk"),
        (r"\bProfil gameplay\b", "Profil permainan"),
        (r"\bKustom\b", "Tersuai"),
        (r"\bAman \(semua mati\)\b", "Selamat (semua dimatikan)"),
        (r"\bDirekomendasikan\b", "Disyorkan"),
        (r"\bRealistis\b", "Realistik"),
        (r"\brealistis\b", "realistik"),
        (r"\bStandar\b", "Standard"),
        (r"\bstandar\b", "standard"),
        (r"\bJalan/Bersepeda\b", "Berjalan/Berbasikal"),
        (r"\bBersepeda\b", "Berbasikal"),
        (r"\bbersepeda\b", "berbasikal"),
        (r"\bHalte Bus\b", "Perhentian Bas"),
        (r"\bHalte\b", "Perhentian"),
        (r"\bhalte\b", "perhentian"),
        (r"\bbus\b", "bas"),
        (r"\bBus\b", "Bas"),
        (r"\bPenyesuai\b", "Penyesuai"),
        (r"\bpenggeser\b", "peluncur"),
        (r"\bPenggeser\b", "Peluncur"),
        (r"\bbawaan\b", "lalai"),
        (r"\bBawaan\b", "Lalai"),
        (r"\bOtomatis\b", "Automatik"),
        (r"\botomatis\b", "automatik"),
        (r"\bSecara Otomatis\b", "Secara Automatik"),
        (r"\bRona\b", "Rona"),
        (r"\bBerdasarkan Kategori\b", "Dikategorikan"),
        (r"\bBernama\b", "Bernama"),
        (r"\bDistrik\b", "Daerah"),
        (r"\bdistrik\b", "daerah"),
        (r"\bJalan\b", "Jalan"),
        (r"\bPercobaan\b", "Cubaan"),
        (r"\bpercobaan\b", "cubaan"),
        (r"\bBawah\b", "Bawah"),
        (r"\bKanan\b", "Kanan"),
        (r"\bSembunyikan\b", "Sembunyikan"),
        (r"\bTransportasi Umum\b", "Pengangkutan Awam"),
        (r"\btransportasi umum\b", "pengangkutan awam"),
        (r"\btransportasi\b", "pengangkutan"),
        (r"\bTransportasi\b", "Pengangkutan"),
        (r"\bwarga\b", "warga"),
        (r"\bPemisahan\b", "Elak berumpun"),
        (r"\bpemisahan\b", "elak berumpun"),
        (r"\bAgresivitas\b", "Keagresifan"),
        (r"\bagresivitas\b", "keagresifan"),
        (r"\bkemunculan\b", "penjelmaan"),
        (r"\bKemunculan\b", "Penjelmaan"),
        (r"\bLayanan\b", "Perkhidmatan"),
        (r"\blayanan\b", "perkhidmatan"),
        (r"\bEkspres\b", "Ekspres"),
        (r"\bPenyeimbangan\b", "Penyeimbangan"),
        (r"\bmendistribusikan ulang\b", "mengagihkan semula"),
        (r"\bmenempatkan ulang\b", "menempatkan semula"),
        (r"\bpenempatan ulang\b", "penempatan semula"),
        (r"\bHati-hati\b", "Berhemah"),
        (r"\bAgresif\b", "Agresif"),
        (r"\bKereta Ringan\b", "Rel Ringan"),
        (r"\bTrem Sejati\b", "Trem Sejati"),
        (r"\bTrem\b", "Trem"),
        (r"\btrem\b", "trem"),
        (r"\bPelepas Kemacetan\b", "Penyahsangkut"),
        (r"\bmacet\b", "tersangkut"),
        (r"\bMacet\b", "Tersangkut"),
        (r"\bAlat Penghapus\b", "Alat Pemadam"),
        (r"\bCentang\b", "Tandakan"),
        (r"\btekan\b", "tekan"),
        (r"\bKONFIRMASI\b", "SAHKAN"),
        (r"\bApakah Anda\b", "Adakah anda"),
        (r"\bAnda\b", "Anda"),
        (r"\breguler\b", "biasa"),
        (r"\bWisata\b", "Pelancongan"),
        (r"\bwisata\b", "pelancongan"),
        (r"\bFeri\b", "Feri"),
        (r"\bZeppelin\b", "Kapal udara"),
        (r"\bEditor\b", "Penyunting"),
        (r"\bKapasitas\b", "Kapasiti"),
        (r"\bkapasitas\b", "kapasiti"),
        (r"\bperawatan\b", "penyelenggaraan"),
        (r"\bPerawatan\b", "Penyelenggaraan"),
        (r"\bTerapkan\b", "Guna"),
        (r"\bmengikuti\b", "mengikuti"),
        (r"\bmemperbesar tampilan\b", "menzum masuk"),
        (r"\bEdit\b", "Sunting"),
        (r"\bPendapatan\b", "Pendapatan"),
        (r"\bPenumpang\b", "Penumpang"),
        (r"\bdiizinkan\b", "dibenarkan"),
        (r"\bmemenuhi syarat\b", "layak"),
        (r"\btersedia\b", "tersedia"),
        (r"\bterpilih\b", "dipilih"),
        (r"\bapa pun\b", "mana-mana"),
        (r"\bPelacak Penerbangan\b", "Penjejak Penerbangan"),
        (r"\bDatang\b", "Masuk"),
        (r"\bMendarat\b", "Mendarat"),
        (r"\bBerangkat\b", "Berlepas"),
        (r"\btaksi\b", "teksi"),
        (r"\bTaksi\b", "Teksi"),
        (r"\bkilometer\b", "kilometer"),
        (r"\bTiket\b", "Tiket"),
        (r"\btiket\b", "tiket"),
        (r"\bantarkota\b", "antara bandar"),
        (r"\bAntarkota\b", "Antara bandar"),
        (r"\bKereta\b", "Kereta api"),
        (r"\bkereta\b", "kereta api"),
        (r"\bMonorel\b", "Monorel"),
        (r"\bKapal\b", "Kapal"),
        (r"\bPesawat\b", "Kapal terbang"),
        (r"\bpesawat\b", "kapal terbang"),
        (r"\bKereta Gantung\b", "Kereta gantung"),
        (r"\bBus Listrik\b", "Bas troli"),
        (r"\bbus listrik\b", "bas troli"),
        (r"\bHelikopter\b", "Helikopter"),
        (r"\bDiperbarui\b", "Dikemas kini"),
        (r"\bTampilan Kereta\b", "Paparan Kereta Api"),
        (r"\bHamparan\b", "Tindanan"),
        (r"\bhamparan\b", "tindanan"),
        (r"\bKonfigurasikan\b", "Konfigurasikan"),
        (r"\bOpasitas\b", "Kelegapan"),
        (r"\bInterval pembaruan\b", "Selang kemas kini"),
        (r"\bpembaruan\b", "kemas kini"),
        (r"\bdisegarkan\b", "dimuat semula"),
        (r"\bSederhana\b", "Ringkas"),
        (r"\bGelap\b", "Gelap"),
        (r"\bTerang\b", "Cerah"),
        (r"\bAsli\b", "Asal"),
        (r"\bBiru\b", "Biru"),
        (r"\bHijau\b", "Hijau"),
        (r"\bKuning tua\b", "Amber"),
        (r"\bSalin\b", "Salin"),
        (r"\bTempel\b", "Tampal"),
        (r"\bbangunan\b", "bangunan"),
        (r"\bAdd-on terintegrasi\b", "Add-on bersepadu"),
        (r"\bterintegrasi\b", "bersepadu"),
        (r"\bpenambalan\b", "penampalan"),
        (r"\blanjutan\b", "lanjutan"),
        (r"\bperon\b", "platform"),
        (r"\bmuat level\b", "muat tahap"),
        (r"\blevel\b", "tahap"),
        (r"\bMengecas\b", "Mengecaj"),
        (r"\bjarak tempuh\b", "jarak perjalanan"),
        (r"\blayang\b", "tinggi"),
        (r"\bMenengah\b", "Sederhana"),
        (r"\bmoderat\b", "sederhana"),
        (r"\btak terbatas\b", "tiada had"),
        (r"\btanpa batas\b", "tiada had"),
        (r"\bTab Sub-Bangunan\b", "Tab Sub-Bangunan"),
        (r"\bPangkalan Taksi\b", "Perhentian Teksi"),
        (r"\bpangkalan taksi\b", "perhentian teksi"),
        (r"\bmenganggur\b", "melahu"),
        (r"\bberkeliaran\b", "berkeliaran"),
        (r"\bacak\b", "rawak"),
        (r"\bPemberdaya\b", "Pemboleh"),
        (r"\bdefault\b", "lalai"),
        (r"\bdestinasi penumpang\b", "destinasi penumpang"),
        (r"\bredesain\b", "reka bentuk semula"),
        (r"\bKoneksi luar\b", "Sambungan luar"),
        (r"\bkoneksi luar\b", "sambungan luar"),
        (r"\bkoneksi\b", "sambungan"),
        (r"\bKoneksi\b", "Sambungan"),
        (r"\bdioptimalkan\b", "dioptimumkan"),
        (r"\bPengali\b", "Pengganda"),
        (r"\bpengali\b", "pengganda"),
        (r"\bCakupan\b", "Skop"),
        (r"\bcakupan\b", "skop"),
        (r"\bSeluruh kota\b", "Seluruh bandar"),
        (r"\bkota\b", "bandar"),
        (r"\bKota\b", "Bandar"),
        (r"\blalu lintas\b", "trafik"),
        (r"\bLalu lintas\b", "Trafik"),
        (r"\bhias\b", "hiasan"),
        (r"\bmelintas\b", "melalui"),
        (r"\btanpa batas\b", "tanpa had"),
        (r"\bPenghindaran tabrakan\b", "Pengelakan perlanggaran"),
        (r"\btabrakan\b", "perlanggaran"),
        (r"\bjalur tunggal\b", "trek tunggal"),
        (r"\bPenumpukan\b", "Penindanan"),
        (r"\bmengantre satu per satu\b", "berbaris satu fail"),
        (r"\bimplementasi ulang\b", "pelaksanaan semula"),
        (r"\bTidak ada\b", "Tiada"),
        (r"\btidak ada\b", "tiada"),
        (r"\bTersembunyi\b", "Tersembunyi"),
        (r"\bKembali\b", "Pulang"),
        (r"\bDalam perjalanan\b", "Dalam perjalanan"),
        (r"\bBeroperasi\b", "Di laluan"),
        (r"\bMenganggur\b", "Melahu"),
        (r"\bSegarkan\b", "Muat semula"),
        (r"\bmenyegarkan\b", "memuat semula"),
        (r"\bOpsi\b", "Pilihan"),
        (r"\bopsi\b", "pilihan"),
        (r"\bTarif\b", "Tambang"),
        (r"\btarif\b", "tambang"),
        (r"\bsaat ini\b", "semasa"),
        (r"\bSaat ini\b", "Semasa"),
        (r"\bMinggu ini\b", "Minggu ini"),
        (r"\bMinggu lalu\b", "Minggu lepas"),
        (r"\bRata-rata\b", "Purata"),
        (r"\brata-rata\b", "purata"),
        (r"\bIzinkan\b", "Benarkan"),
        (r"\bizinkan\b", "benarkan"),
        (r"\bKontrol\b", "Kawalan"),
        (r"\bkontrol\b", "kawalan"),
        (r"\bMengaktifkan atau menonaktifkan\b", "Mendayakan atau melumpuhkan"),
        (r"\bmengaktifkan atau menonaktifkan\b", "mendayakan atau melumpuhkan"),
        (r"\bdinonaktifkan\b", "dilumpuhkan"),
        (r"\bnonaktif\b", "dilumpuhkan"),
        (r"\bLompat\b", "Lompat"),
        (r"\blompat\b", "lompat"),
        (r"\bMenahan tombol\b", "Menahan kekunci"),
        (r"\btombol\b", "butang"),
        (r"\bTombol\b", "Butang"),
        (r"\bPilih Jenis\b", "Pilih Jenis"),
        (r"\bTambah\b", "Tambah"),
        (r"\bSebelumnya\b", "Sebelumnya"),
        (r"\bBerikutnya\b", "Seterusnya"),
        (r"\bMenghitung\b", "Mengira"),
        (r"\bmenghitung\b", "mengira"),
        (r"\bTujuan\b", "Destinasi"),
        (r"\btujuan\b", "destinasi"),
        (r"\bKomuter\b", "Komuter"),
        (r"\bbosan\b", "bosan"),
        (r"\bhitungan mundur\b", "kiraan undur"),
        (r"\bmasuk\b", "masuk"),
        (r"\bkeluar\b", "keluar"),
        (r"\bTotal\b", "Jumlah"),
        (r"\btotal\b", "jumlah"),
        (r"\bPerbarui\b", "Kemas kini"),
        (r"\bterdekat\b", "terdekat"),
        (r"\brisiko Anda sendiri\b", "risiko anda sendiri"),
        (r"\bmencegah\b", "menghalang"),
        (r"\bpembukaan\b", "pembukaan"),
        (r"\bpanel info\b", "panel maklumat"),
        (r"\bIntegrasi\b", "Integrasi"),
        (r"\bintegrasi\b", "integrasi"),
    ]
    out = {}
    for k, v in id_keys.items():
        if k.startswith("CHANGELOG_"):
            continue
        s = v
        for pat, rep in reps:
            s = re.sub(pat, rep, s)
        out[k] = s

    # Explicit high-quality Malay overrides for critical UI
    overrides = {
        "MOD_DESCRIPTION": "Pengangkutan awam diperbaiki: kawalan laluan, armada, integrasi dan lagi.",
        "CURRENT_WEEK": "Minggu ini",
        "LAST_WEEK": "Minggu lepas",
        "AVERAGE": "Purata",
        "AVERAGE_TOOLTIP": "Purata {0} minggu terakhir.",
        "SETTINGS_TAB_GENERAL": "Umum",
        "SETTINGS_TAB_UNBUNCHING": "Elak berumpun",
        "SETTINGS_TAB_DELETE": "Padam laluan",
        "SETTINGS_TAB_FLEET": "Armada & penjadualan",
        "SETTINGS_TAB_BUDGET": "Bajet & harga",
        "SETTINGS_TAB_LINECOLORS": "Warna laluan",
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
        "TRAINDISPLAY_LABEL_NAME": "Nama",
        "TRAINDISPLAY_LABEL_STATUS": "Status",
        "TRAINDISPLAY_NO_LINE": "Tiada laluan",
        "TRAINDISPLAY_NO_DESTINATION": "Tiada destinasi",
        "TRAINDISPLAY_HIDDEN": "Tersembunyi",
        "TRAINDISPLAY_VEHICLE": "Kenderaan",
        "TRAINDISPLAY_STATE_RETURNING": "Pulang",
        "TRAINDISPLAY_STATE_STOPPED": "Di perhentian",
        "TRAINDISPLAY_STATE_EN_ROUTE": "Dalam perjalanan",
        "TRAINDISPLAY_STATE_ON_LINE": "Di laluan",
        "TRAINDISPLAY_STATE_IDLE": "Melahu",
        "SETTINGS_TRAINDISPLAY_THEME_BLUE": "Biru",
        "SETTINGS_TRAINDISPLAY_THEME_GREEN": "Hijau",
        "SETTINGS_TRAINDISPLAY_THEME_AMBER": "Amber",
        "SETTINGS_ADVANCEDSTOPSELECTION_ENABLE": "Dayakan pemilihan perhentian lanjutan",
        "SETTINGS_PTU_ENABLE": "Buang penumpang tersangkut",
        "SETTINGS_INTEGRATIONS_GROUP": "Add-on bersepadu",
        "SETTINGS_WALKING_SPEED_MODE_VANILLA": "Standard",
        "SETTINGS_WALKING_SPEED_MODE_REALISTIC": "Realistik",
        "SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM": "Bawah",
        "SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT": "Kanan",
        "UNBUNCHING_DISABLED": "Elak berumpun dilumpuhkan.",
        "SETTINGS_EBS_MODE_NONE": "Dilumpuhkan",
        "SETTINGS_EBS_TRAM_MODE_NONE": "Dilumpuhkan",
        "FLIGHT_STATUS_NONE": "Tiada",
        "SETTINGS_TRAINDISPLAY_MODE_DISABLED": "Dilumpuhkan",
        "SETTINGS_TRAINDISPLAY_MODE_ENABLED": "Didayakan",
        "AUTOLINECOLOR_STRATEGY_DISABLED": "Dilumpuhkan",
        "AUTOLINECOLOR_NAMING_DISABLED": "Dilumpuhkan",
        "SETTINGS_AUTO_LINE_BUDGET_DISABLED": "Dilumpuhkan",
        "SETTINGS_AUTO_LINE_BUDGET_ENABLED": "Didayakan",
        "SETTINGS_BUDGET_TICKET_PRICES_DISABLED": "Dilumpuhkan",
        "SETTINGS_BUDGET_TICKET_PRICES_ENABLED": "Didayakan",
        "SETTINGS_OOC_PASSENGER_SCOPE_DISABLED": "Dilumpuhkan (vanilla)",
    }
    out.update(overrides)
    # Fill any missing non-changelog from en (should not happen if id is complete)
    for k, v in en_keys.items():
        if k.startswith("CHANGELOG_"):
            continue
        if k not in out:
            out[k] = v
    return out


def main():
    en_keys, order = parse(ROOT / "en.txt")
    print(f"en non-CL={sum(1 for k in en_keys if not k.startswith('CHANGELOG_'))}")

    # da verify
    da, _ = parse(ROOT / "da.txt")
    print(
        "da non-CL same",
        sum(
            1
            for k, v in en_keys.items()
            if not k.startswith("CHANGELOG_") and da.get(k) == v
        ),
    )

    from lang_packs_all import NO
    from lang_packs_sv_fi import SV, FI
    from lang_packs_hu_ro import HU

    for lang, tr in [("no", NO), ("sv", SV), ("fi", FI), ("hu", HU)]:
        write_lang(lang, tr, en_keys, order)

    # Malay from Indonesian
    id_keys, _ = parse(ROOT / "id.txt")
    ms = malay_from_indonesian(id_keys, en_keys)
    write_lang("ms", ms, en_keys, order)

    # RO, EL, VI need dedicated modules
    try:
        from lang_packs_ro import RO

        write_lang("ro", RO, en_keys, order)
    except Exception as e:
        print("RO not ready:", e)
    try:
        from lang_packs_el import EL

        write_lang("el", EL, en_keys, order)
    except Exception as e:
        print("EL not ready:", e)
    try:
        from lang_packs_vi_ms import VI

        write_lang("vi", VI, en_keys, order)
    except Exception as e:
        print("VI not ready:", e)


if __name__ == "__main__":
    main()
