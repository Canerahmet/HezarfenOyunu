"""
Hezarfen: 1632 — Türbe, sıbyan mektebi ve kahvehane üreticisi (plan Faz 2b).

Gerekçe `lib/mahalle_kit.py` başlığında ve ADR 0021'de.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_mahalle_kit.py --
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
import mahalle_kit as mk           # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

TIERS = {
    "turbe": ("T2",
              "RESEARCH.md 4.3(a): mahalle bir VAKIF etrafinda kurulur ve "
              "banisinin adiyla anilir (1546 ve 1600 Istanbul Vakiflari Tahrir "
              "Defteri); bani kendi mescidinin hazivresine gomulur. Sekizgen "
              "kagir govde + kursun kubbe + sebekeli pencere. OLCULER "
              "REKONSTRUKSIYON; belirli bir turbeye karsilik gelmez."),
    "mektep": ("T2",
               "RESEARCH.md 4.3(b): sibyan mektebi tek odalidir ve "
               "YUKSELTILIR — alti gelir getiren/hayrat birim (cesme, sarnic, "
               "dukkan), ustu ders odasi, disaridan tas merdiven. Tipoloji "
               "ifadesidir; 1632 Istanbul'undan belirli bir mektep icin "
               "birincil kaynak (vakfiye/sicil) DOGRULANMADI."),
    "kahvehane": ("T2",
                  "RESEARCH.md 4.3(c): ZAMAN ISARETI. 1632'de ACIK; 2 Eylul "
                  "1633 fermaniyla (BOA A.DVN nr. 25/47) yasaklanip "
                  "yiktirildi — yasak T1, mimari tip T2. Ahsap cephe + genis "
                  "sundurma + sokakta tas seki + ocak bacasi. Cephe olculeri "
                  "rekonstruksiyon."),
    "sebil": ("T2",
              "RESEARCH.md 4.3(d): sebil su DAGITIR — cesmeden kendin alirsin, "
              "sebilden sana verilir. Sebekeli pencereler ve her pencerede "
              "mermer tezgah bundandir. Kulliyenin sokaga bakan kosesinde "
              "durur; arkasi duvara yaslanir. Sekizgen govde + genis sacak + "
              "kursun kulah. Olculer rekonstruksiyon."),
    "firin": ("T2",
              "RESEARCH.md 4.3(e): mahalle firini. Yapiyi firin yapan sey "
              "cephesi degil ARKASIDIR — kagir kubbeli ocak ve kalin bacasi. "
              "Baca damdan en az 2 m yukselir (kivilcim ve is). Olculer "
              "rekonstruksiyon."),
}

VARIANTS = [
    ("Turbe_A", "turbe", "mahalle banisinin turbesi — sekizgen, hazire icinde",
     dict()),
    ("Turbe_B", "turbe", "kucuk turbe — altigen, dar hazire icin",
     dict(sides=6, apothem=2.55, wall_h=4.10, dome_h=2.20, win_w=0.72)),
    ("Mektep_A", "mektep", "sibyan mektebi — cesme uzerinde yukseltilmis",
     dict()),
    ("Kahvehane_A", "kahvehane", "carsi kahvehanesi — genis sacak, tas seki",
     dict()),
    ("Kahvehane_B", "kahvehane", "kucuk kahvehane — dar parsel, ocaksiz",
     dict(width=5.90, depth=5.20, open_w=3.20, eave=2.00, ocak=False,
          roof_h=1.30)),
    ("Sebil_A", "sebil", "kulliye kosesinde sebil — sekizgen, genis sacakli",
     dict()),
    ("Firin_A", "firin", "mahalle firini — arkada kubbeli ocak, kalin baca",
     dict()),
    ("Firin_B", "firin", "kucuk firin — dar parsel",
     dict(width=6.00, depth=7.00, open_w=1.90, ocak_w=3.70, ocak_d=2.60,
          baca_h=2.30, roof_h=1.30)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "mahalle"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "mahalle",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    builders = {"turbe": (mk.TurbeParams, mk.build_turbe),
                "mektep": (mk.MektepParams, mk.build_mektep),
                "kahvehane": (mk.KahvehaneParams, mk.build_kahvehane),
                "sebil": (mk.SebilParams, mk.build_sebil),
                "firin": (mk.FirinParams, mk.build_firin)}
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
        hz.log(f"{name:12s} {info['footprint_x']:6.2f}x{info['footprint_y']:6.2f}"
               f"x{info['height']:6.2f} m {info['tris_lod0']:6d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} mahalle yapisi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
