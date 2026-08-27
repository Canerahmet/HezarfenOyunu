"""
Hezarfen: 1632 — Hamam ve han üreticisi (plan Faz 2b).

Gerekçe `lib/civic_kit.py` başlığında ve ADR 0020'de.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_civic_kit.py --
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
import civic_kit as ck             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

TIERS = {
    "hamam": ("T2",
              "Osmanli hamam tipolojisi: sogukluk/camekan -> iliklik -> "
              "sicaklik + kose halvetleri, arkada kulhan ve bacasi. Kursun "
              "kubbelerde FIL GOZU camlari (sicaklikta pencere olmaz, isik "
              "yukaridan gelir). PLAN.md 7.1. Olculer rekonstruksiyon; belirli "
              "bir hamama karsilik gelmez."),
    "han": ("T2",
            "Avlulu han tipolojisi: sagir dis duvar, tek tackapi, avluya bakan "
            "iki kat revak, ust kat odalarinda kubbe+baca. RESEARCH.md 3-Ticaret "
            "yapilari (Buyuk Valide Han tartismali, Buyuk Yeni Han YOK). "
            "Olculer rekonstruksiyon."),
    "medrese": ("T2",
                "RESEARCH.md 4.3(f): han ile ayni gramer, farkli cumle. TEK "
                "KATLI, avluya bakan hucre sirasi, her hucrede ocak "
                "(dolayisiyla her kubbede baca), ve esit ritmi kiran tek buyuk "
                "kubbe = DERSHANE. Tackapi tek katli yapida bile DAMI ASAR. "
                "Olculer rekonstruksiyon; belirli bir medreseye karsilik "
                "gelmez."),
}

VARIANTS = [
    ("Hamam_A", "hamam", "mahalle hamami — cifte degil, tek",
     dict()),
    ("Hamam_B", "hamam", "kucuk hamam — dar parselde",
     dict(camekan=9.0, sicaklik=8.0, iliklik_d=4.0, halvet=3.0, baca_h=8.0)),
    ("Han_A", "han", "avlulu han — ticaret cekirdegi",
     dict()),
    ("Han_B", "han", "kucuk han — tek katli ama YUKSEK; kubbesiz dam",
     dict(width=24.0, depth=20.0, wing=6.0, floors=1, floor_h=6.4,
          domes=False, portal_w=2.8)),
    ("Medrese_A", "medrese", "revakli avlulu medrese — dershaneli",
     dict()),
    ("Medrese_B", "medrese", "kucuk medrese — dershanesiz, dar avlulu",
     dict(width=22.0, depth=18.0, wing=4.8, dershane=False, portal_w=2.3,
          arch_w=2.0)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "civic"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "civic",
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

        builders = {"hamam": (ck.HamamParams, ck.build_hamam),
                    "han": (ck.HanParams, ck.build_han),
                    "medrese": (ck.MedreseParams, ck.build_medrese)}
        if kind not in builders:
            raise SystemExit(f"[HZ] bilinmeyen tur: {kind}")
        params_cls, build = builders[kind]
        lod0, lod1, ucx, info = build(params_cls(**params), col, name,
                                      textured=tex)

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
        hz.log(f"{name:10s} {info['footprint_x']:6.2f}x{info['footprint_y']:6.2f}"
               f"x{info['height']:6.2f} m {info['tris_lod0']:6d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} kamusal yapi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
