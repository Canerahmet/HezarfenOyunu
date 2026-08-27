"""
Hezarfen: 1632 — Doğa ve mezarlık üreticisi: servi, çınar, mezar taşı.

Gerekçe `lib/nature_kit.py` başlığında ve ADR 0019'da.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_nature_kit.py --
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import nature_kit as nk            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

TIERS = {
    "agac_servi": ("T2",
                   "Servi (Cupressus sempervirens): mezarlik ve cami avlusunun "
                   "imza agaci; RESEARCH.md 4 'mezarliklar servi alanlariyla "
                   "kent ici buyuk yesil kutleler'. Bicim tipolojiktir, belirli "
                   "bir agaca karsilik gelmez."),
    "agac_cinar": ("T2",
                   "Cinar (Platanus orientalis): cami avlusu ve meydan agaci, "
                   "yayvan tacli. RESEARCH.md 4 (yesil doku, mesireler)."),
    "mezar_erkek": ("T2",
                    "Erkek sahidesi: kavuk/sarik biciminde basliklidir. "
                    "PLAN.md Faz 2 kit listesi (mezar tasi + servi)."),
    "mezar_kadin": ("T2",
                    "Kadin sahidesi: basliksiz ve daha alcak. "
                    "PLAN.md Faz 2 kit listesi."),
}

# (ad, tur, gerekce, parametreler)
VARIANTS = [
    ("Servi_A", "agac", "buyuk servi — mezarlik ve avlu", dict(height=13.0, seed=1)),
    ("Servi_B", "agac", "orta servi", dict(height=10.0, seed=2)),
    ("Servi_C", "agac", "genc servi — sokak arasi", dict(height=7.0, seed=3)),
    ("Cinar_A", "agac", "buyuk cinar — avlu ortasi, golge agaci",
     dict(kind="cinar", height=16.0, seed=4)),
    ("Cinar_B", "agac", "orta cinar", dict(kind="cinar", height=12.0, seed=5)),
    ("Mezar_Erkek", "mezar", "erkek sahidesi — kavuklu", dict(gender="erkek")),
    ("Mezar_ErkekB", "mezar", "kisa erkek sahidesi",
     dict(gender="erkek", height=0.92, tilt_deg=9.0, width=0.26)),
    ("Mezar_Kadin", "mezar", "kadin sahidesi — basliksiz",
     dict(gender="kadin", tilt_deg=3.0)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "nature"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "nature",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    for name, kind, why, params in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        tex = not args.no_textures

        if kind == "agac":
            lod0, lod1, ucx, info = nk.build_agac(
                nk.AgacParams(**params), col, name, textured=tex)
        elif kind == "mezar":
            lod0, lod1, ucx, info = nk.build_mezar(
                nk.MezarParams(**params), col, name, textured=tex)
        else:
            raise SystemExit(f"[HZ] bilinmeyen tur: {kind}")

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot taban merkezde degil "
                             f"({info['pivot_min_z']})")

        hz.save_blend(os.path.join(args.blend_dir, f"SM_{name}.blend"))
        export_fbx(os.path.join(args.out_dir, f"SM_{name}.fbx"),
                   collection_name=COLLECTION)

        tier, source = TIERS[info["kind"]]
        info.update(name=name, why=why, prefab=f"PF_{name}",
                    tier=tier, source=source)
        catalog.append(info)
        hz.log(f"{name:14s} {info['footprint_x']:5.2f}x{info['footprint_y']:5.2f}"
               f"x{info['height']:6.2f} m {info['tris_lod0']:5d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} doga/mezar varligi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
