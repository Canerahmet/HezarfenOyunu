"""
Hezarfen: 1632 — Kanat aygıtı üreticisi (Faz 5).

Gerekçe ve **tarihî plan olmadığı** notu: `tools/blender/lib/kanat_kit.py`
başlığı. Kısaca: kaynak uçuşu anlatır, kanadı anlatmaz; tasarım plan
Bölüm 10'un malzeme kuralından türer (ahşap çıta + kartal tüyü + deri kayış).

Kullanım:
  blender --background --python tools/blender/gen_kanat.py
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz             # noqa: E402
import kanat_kit as kk              # noqa: E402
import ottoman_kit as kit           # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

TIER = "T3"
SOURCE = (
    "KANAT AYGITI, 1632. **TARIHI PLAN YOKTUR** ve bunu soylemek bu "
    "varligin en durust ozelligidir. RESEARCH.md: olayin tek tanigi "
    "Evliya Celebi'dir, kese altin ihsaninin mali kayitlarda izi yoktur, "
    "aerodinamik uzmanlari gereken ~55:1 suzulme oranini imkansiz bulur "
    "(modern delta kanat ~15:1), ve Dankoff Evliya'nin sistematik abarti "
    "uslubunu belgeler. Ucus TARIHI bile celiskilidir (1632 / 1638). "
    "Tasarim uydurulmadi, MALZEME KURALINDAN turetildi (plan Bolum 10): "
    "ahsap cita iskelet + kartal tuyu yuzey + deri kayis — 1632'de bir "
    "zanaatkarin elinde ne varsa o. Bicim yarasa/ucurtma mantigi: merkezde "
    "omurga, yelpaze gibi acilan citalar, uclari baglayan hucum kenari. "
    "Tuyler UCA DOGRU ust uste biner, cunku bindirme yonu havayi tutan "
    "seydir. TEK SERT SAYI ALANDIR: `WindTuning.wingArea` 15 m2 ve ucus "
    "butcesi o sayiyla olculdu; gorunen kanat o alana sahip degilse oyuncu "
    "bir sey gorup baska bir seyin fizigini yasar. Uretici alani OLCER."
)

#: (ad, durum, neden)
VARIANTS = [
    ("Kanat_Acik",    "open",   "ucus durumu — aygitin tam acilmis hali"),
    ("Kanat_Katli",   "folded", "sirtta tasinan; kule merdiveninde bu"),
    ("Kanat_Kirik",   "broken", "hasar: bir uc citasi kirik, tuyler dagilmis"),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "kanat"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "kanat",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    for name, state, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        asset = f"SM_{name}"
        p = kk.KanatParams(state=state)
        lod0, lod1, ucx, info = kk.build_kanat(
            p, col, asset, textured=not args.no_textures)
        _ = (lod1, ucx)

        # --- ALAN DENETIMI: fizik ile gorunen ayni sey olmali -----------
        #
        # `WindTuning.wingArea` 15 m2 ve ucus butcesi (FlightBudget,
        # ThermalFlightSim) o sayiyla olculdu. Gorunen kanat baska bir
        # alana sahipse model yalan soyler. Yalnizca ACIK durum denetlenir:
        # katli ve kirik kanat zaten daha az alan tasir, ve tasimasi
        # gerekir.
        if state == "open":
            sapma = abs(info["wing_area"] - kk.TARGET_AREA) / kk.TARGET_AREA
            if sapma > kk.AREA_TOLERANCE:
                raise SystemExit(
                    f"[HZ] HATA {name}: kanat alani {info['wing_area']:.2f} m2, "
                    f"hedef {kk.TARGET_AREA:.1f} m2 (sapma %{sapma*100:.1f}). "
                    "WindTuning.wingArea ile ayni olmali — yoksa oyuncu bir "
                    "sey gorup baska bir seyin fizigini yasar.")

        # Katalog anahtari CIPLAK ad — "SM_" onekiyle DEGIL. Ic aktarici
        # dosya adindan oneki soyup arar; onekli yazdigim ilk turda hat
        # "3 model yerlestirildi" dedi ama uc kanadin da HistoricalTag'i
        # Graybox kaldi. Basarili gorunen bir adim eksik is yapabilir.
        info.update(name=name, prefab=f"PF_{name}", why=why,
                    tier=TIER, source=SOURCE,
                    target_area=kk.TARGET_AREA)
        catalog.append(info)

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                   collection_name=COLLECTION)
        hz.log(f"{name:14s} aciklik {info['span']:.2f} m, alan "
               f"{info['wing_area']:6.2f} m2, {info['tris_lod0']:5d} ucgen  {why}")

    catalog.sort(key=lambda v: v["name"])
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(VARIANTS)} kanat durumu; katalog: {args.catalog}")
    _ = kit


if __name__ == "__main__":
    main()
