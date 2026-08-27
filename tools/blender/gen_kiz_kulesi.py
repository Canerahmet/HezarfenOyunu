"""
Hezarfen: 1632 — Kız Kulesi üreticisi (plan Faz 3, S-kademe).

**1632'de bu kule AHŞAPTIR.** Herkesin bildiği kâgir kule, camlı köşk ve
kurşun kubbe 1725'tir. Gerekçe ve kaynak: `lib/tower_kit.py`, RESEARCH.md §5.3.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_kiz_kulesi.py -- \
      --textured --out-fbx unity/HezarfenGame/Assets/_Import/SM_KizKulesi.fbx
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import tower_kit as tk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Kiz Kulesi, 1632. **D3** (tipolojik; 1632 kulesi YANMISTIR ve olculu "
    "cizimi yoktur). Salacak kiyisindan ~100 m acikta, kayaliklar uzerinde. "
    "1632'DE AHSAP: 1509 depreminde yikilan kulenin yerine yapilan da 'YINE "
    "AHSAP'tir; kule 1720'de (1130 Receb) bir kivilcimla yanmis, Damat "
    "Ibrahim Pasa yerine KAGIR bir fener kulesi yaptirmistir. "
    "ISLEVI FENER DEGIL KARAKOL: Fatih 1453'ten sonra nobetci birligi "
    "yerlestirmis ve yapiyi saglamlastirmistir; her aksam yatsidan sonra ve "
    "seher vakti MEHTER nobet calar, bayram ve culuslarda TOP atilir. "
    "1632'de YOK: kagir govde, camli kosk ve kursun KUBBE (1725); zeytinyagi "
    "FENERI (Damat Ibrahim Pasa, sadaret 1718-1730); II. Mahmud (1832) ve "
    "1945 sonrasi ekler; Manuel Komnenos'un Sarayburnu'na gerdigi ZINCIR "
    "(12. yy). "
    "ADA COPERNICUS GLO-30'DA YOK — olculdu, cevresi bastan basa -12 m; "
    "kayalik bu yuzden arazinin degil VARLIGIN parcasidir. "
    "Kaynak: Goksoy Ozkan, Vildan (2012), 'Istanbul Siluetinin Vazgecilmezi "
    "Kiz Kulesi', Istanbul Journal of Social Sciences, 2012/1. "
    "RESEARCH.md 5.3"
)


def add_args(p):
    p.add_argument("--asset", default="KizKulesi")
    p.add_argument("--storeys", type=int, default=2)
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = tk.KizKuleParams(storeys=a.storeys, palette=a.palette)
    lod0, lod1, ucx, info = tk.build_kiz_kulesi(p, col, a.asset,
                                                textured=a.textured)

    hz.log(f"{a.asset}: kayalik {p.rock_w:.1f}x{p.rock_d:.1f} m, "
           f"AHSAP govde {p.body:.1f} m x {a.storeys} kat, "
           f"su ustunde {info['above_water']:.1f} m")
    hz.log(f"ayak izi {info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"toplam {info['height']:.2f} m, LOD0={info['tris_lod0']}")

    # Pivot burada TABANDA DEGIL: kayalik su altina uzaniyor. Onemli olan
    # pivotun SU DUZLEMINDE (y=0) olmasi — kule denize oturtulacak ve
    # yerlestirici arazi kotu kullanamaz (ada DEM'de yok).
    if abs(info["pivot_min_z"] + 2.5) > 0.01:
        raise SystemExit(f"[HZ] HATA kayalik tabani {info['pivot_min_z']:.2f} — "
                         "-2,50 m olmali (su cizgisinin altina uzanmali)")
    hz.log("pivot OK: kayalik -2,50 m'den baslar, su duzlemi y=0")

    info.update(name=a.asset, prefab=f"PF_{a.asset}", tier="T1", source=SOURCE)

    if a.catalog:
        os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
        cat = {"variants": []}
        if os.path.exists(a.catalog):
            with open(a.catalog, encoding="utf-8") as fh:
                cat = json.load(fh)
        rest = [v for v in cat.get("variants", []) if v.get("name") != a.asset]
        rest.append(info)
        rest.sort(key=lambda v: v["name"])
        with open(a.catalog, "w", encoding="utf-8") as fh:
            json.dump({"variants": rest}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {a.catalog} ({len(rest)} kayit)")

    if a.out_blend:
        hz.save_blend(a.out_blend)
    if a.out_fbx:
        export_fbx(a.out_fbx, collection_name=COLLECTION)
    hz.log("gen_kiz_kulesi OK")


if __name__ == "__main__":
    main()
