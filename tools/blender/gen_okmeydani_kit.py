"""
Hezarfen: 1632 — Okmeydanı varlıkları (plan Faz 2b): namazgâh, tekke, menzil taşı.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_okmeydani_kit.py
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
import okmeydani_kit as ok          # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

TIERS = {
    "namazgah": ("T2",
                 "RESEARCH.md 'Okmeydani': II. Bayezid vakfi talim alaninda "
                 "MINBERLI namazgah mevcut; minberi Gurcu Mehmed Pasa 1624-25'te "
                 "ekledi — yani 1632'de YEDI YILLIKTIR (T1 olay). Namazgahin "
                 "bicimi (seki + mihrap tasi + minber) TDV 'Namazgah'tan; "
                 "12x8 m orani Gelibolu Azebler Namazgahi'nin belgelenmis "
                 "olcusunden alindi. Okmeydani'ninki icin OLCU YOK — "
                 "olculer REKONSTRUKSIYON."),
    "tekke": ("T2",
              "RESEARCH.md 'Okmeydani': Okcular (Kemankes) Tekkesi mevcut; "
              "tekke mescidinin minaresi ancak 1770-71'de eklenmistir, yani "
              "1632'de MINARESIZDIR (T1 yokluk). Tevhidhane/mescit + dervis "
              "hucreleri semasi tipolojiktir; olculer REKONSTRUKSIYON."),
    "menzil_tasi": ("T2",
                    "RESEARCH.md 'Okmeydani': meydanda 132 ok abidesi tespit "
                    "edilmis, 55'i ayakta (TDV). Taslar tek parca MERMER "
                    "SUTUNDUR ve uzerlerinde okcunun adi, meslegi, atisin "
                    "yapildigi gunun HAVASI (ruzgari), MESAFESI ve tarihi "
                    "yazar; ikiser dikilirler (ayak tasi + BAS tasi). Bicim "
                    "tipolojik, olculer REKONSTRUKSIYON."),
}

VARIANTS = [
    ("Namazgah_Okmeydani", "namazgah",
     "Okmeydani namazgahi — minberli, 1624-25 minberi", dict()),
    ("Namazgah_Kucuk", "namazgah",
     "yol ustu namazgahi — minbersiz, yalnizca mihrap tasi",
     dict(width=7.40, depth=5.60, minber=False, steps=1, step_h=0.22,
          mihrap_h=2.05, wall_h=0.50)),
    ("Tekke_Okcular", "tekke",
     "Okcular Tekkesi — MINARESIZ mescit + avlulu dervis hucreleri", dict()),
    ("Tekke_Kucuk", "tekke",
     "kucuk zaviye — dort hucre, dar avlu",
     dict(hall_w=7.60, hall_d=7.60, wall_h=4.10, dome_h=2.10, cells=4,
          court_w=9.20, cell_w=2.90, cell_d=3.80)),
    ("MenzilTasi_Bas", "menzil_tasi",
     "bas tasi — okun en ileriye dustugu yer; yuksek, kitabeli, kulahli",
     dict()),
    ("MenzilTasi_Ayak", "menzil_tasi",
     "ayak tasi — atisin yapildigi yer; alcak ve sade",
     dict(role="ayak", height=1.85, side=0.30, base_side=0.62, kitabe=False)),
    ("MenzilTasi_Buyuk", "menzil_tasi",
     "buyuk bas tasi — rekor tasi olcusunde",
     dict(height=3.30, side=0.40, base_side=0.88, base_h=0.42)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "okmeydani"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "okmeydani",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    builders = {"namazgah": (ok.NamazgahParams, ok.build_namazgah),
                "tekke": (ok.TekkeParams, ok.build_tekke),
                "menzil_tasi": (ok.MenzilTasiParams, ok.build_menzil_tasi)}
    catalog = []

    for name, kind, why, params in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        params_cls, build = builders[kind]
        lod0, lod1, ucx, info = build(params_cls(**params), col, name,
                                      textured=not args.no_textures)

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
        hz.log(f"{name:20s} {info['footprint_x']:6.2f}x{info['footprint_y']:6.2f}"
               f"x{info['height']:6.2f} m {info['tris_lod0']:6d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} Okmeydani varligi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
