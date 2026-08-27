"""
Hezarfen: 1632 — Avlu çamaşır ipi üreticisi (Faz 4 donatı geçişi).

Gerekçe ve **neden sokak değil avlu** olduğu: `tools/blender/lib/camasir_kit.py`
başlığı. Kısaca: sokağa gerilmiş çamaşır Napoli imgesidir; Osmanlı konutu
avlulu ve hayatlıdır, çamaşır duvarın içinde kalır.

Kullanım:
  blender --background --python tools/blender/gen_camasir.py
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import camasir_kit as ck            # noqa: E402
import hz_blender as hz             # noqa: E402
import ottoman_kit as kit           # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

TIER = "T3"
SOURCE = (
    "AVLU CAMASIR IPI, 1632. **Kaynakta kayit YOK** — plan donati "
    "gecisinde sayiyor, RESEARCH.md'de gecmiyor. Bu yuzden T3 "
    "(tipolojik cikarim) ve status draft. "
    "Konum kararı gerekcelidir: sokagin bir yanindan obur yanina gerilmis "
    "camasir NAPOLI/CENOVA imgesidir. Osmanli konutu AVLULU ve HAYATLIdir, "
    "avlu duvarla cevrilidir ve mahremiyet o duvarin isidir; camasiri "
    "sokaga asmak evin icini sokaga asmak olurdu. Kit bu yuzden SOKAK USTU "
    "IP URETMEZ — yalnizca avlu ici, iki direk arasi kisa ip. "
    "Olcu yok; tek sayisal kisit oranlardir: ip 1,78 m (asanin erisebilecegi "
    "kot), aciklik 2,5-4,5 m (avlu olcegi), sarkma aciklığın %11'i "
    "(gerilmis ama gergin olmayan ip). Ip DUZ degildir: kendi agirligiyla "
    "sarkar ve duz cizilirse cubuk gibi okunur."
)

#: (ad, açıklık, parça, direkli mi, neden)
VARIANTS = [
    ("Camasir_A", 3.4, 4, True,  "orta avlu ipi — dort parca"),
    ("Camasir_B", 2.8, 3, True,  "kucuk avlu, uc parca"),
    ("Camasir_C", 4.2, 5, True,  "genis avlu, bes parca"),
    ("Camasir_Bos", 3.0, 0, True, "bos ip — her avlu dolu olmaz"),
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

    mevcut = []
    if os.path.exists(args.catalog):
        with open(args.catalog, encoding="utf-8") as fh:
            mevcut = json.load(fh).get("variants", [])
    yeni = {f"SM_{n}" for n, _, _, _, _ in VARIANTS}
    catalog = [v for v in mevcut if v.get("name") not in yeni]

    for name, span, cloth, posts, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        asset = f"SM_{name}"
        p = ck.CamasirParams(span=span, cloth=cloth, posts=posts)
        lod0, lod1, ucx, info = ck.build_camasir(
            p, col, asset, textured=not args.no_textures)
        _ = (lod1, ucx)

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot tabanda degil "
                             f"({info['pivot_min_z']})")
        # Ip SARKMALI: duz bir ip cubuk gibi okunur ve bu kitin tek
        # bicimsel iddiasidir, o yuzden denetlenir.
        if info["sag"] < span * 0.06:
            raise SystemExit(f"[HZ] HATA {name}: sarkma {info['sag']} m — "
                             "ip neredeyse duz, cubuk gibi okunur")

        info.update(name=asset, prefab=f"PF_{name}", why=why,
                    tier=TIER, source=SOURCE)
        catalog.append(info)

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                   collection_name=COLLECTION)
        hz.log(f"{name:14s} aciklik {span:.1f} m, sarkma {info['sag']:.2f} m, "
               f"{cloth} parca, {info['tris_lod0']:4d} ucgen  {why}")

    catalog.sort(key=lambda v: v["name"])
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(VARIANTS)} camasir ipi; katalog: {args.catalog}")
    _ = kit


if __name__ == "__main__":
    main()
