"""
Hezarfen: 1632 — Hayat (avlu) donatısı üreticisi.

Gerekçe: `tools/blender/lib/hayat_kit.py` başlığı. Kısaca — şehir dışının
boşluğu kapatıldı ama şehir **içi** ölçüldüğünde bir mahallenin 200 m'lik
karesinin %90,3'ü çıplak arazi, %81,7'sinin 4 m yakınında hiçbir şey yok.
Eksik olan dekor değil, konutun kendi eklentileri.

Kullanım:
  blender --background --python tools/blender/gen_hayat.py -- [--export]
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hayat_kit as hk              # noqa: E402
import hz_blender as hz             # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

TIER = "T3"
SOURCE = (
    "MAHALLE HAYATI DONATISI, 1632. Kaynakta olcu YOK — RESEARCH.md "
    "bu nesnelerin hicbirini olcusuyle vermiyor, o yuzden T3 "
    "(tipolojik cikarim) ve status draft. Metrik geometri UYDURULMADI: "
    "her olcu insan olceginden turetildi ve gerekcesi kit icinde yazili "
    "(su kupu bel hizasindan alcak cunku doldurulup tasinir; cardak "
    "2,20 m cunku altindan gecilir; kuyu bilezigi 0,80 m cunku hem "
    "uzerine dayanilir hem cocuk dusmez). Osmanli konutunun avlulu ve "
    "hayatli oldugu RESEARCH.md 4.1'de kayitlidir; buradaki iddia "
    "avlunun BOS OLMADIGIdir, belirli bir avlunun envanteri degil."
)

#: (ad, tur, olcek, tohum, neden)
VARIANTS = [
    ("Odunluk_A", "odunluk", 1.00, 11, "kislik odun istifi — duvara dayanir"),
    ("Odunluk_B", "odunluk", 0.85, 23, "kucuk istif, yarisi harcanmis"),
    ("SuKupu_A",  "kup",     1.00, 31, "avlunun su kabi"),
    ("SuKupu_B",  "kup",     0.82, 37, "kucuk kup — ikincisi her avluda olur"),
    ("Sepet_A",   "sepet",   1.00, 41, "hasir sepet"),
    ("Cardak_A",  "cardak",  1.00, 53, "asma cardagi — altindan gecilir"),
    ("Kuyu_A",    "kuyu",    1.00, 61, "avlu kuyusu, makarali"),
    ("Cit_A",     "cit",     1.00, 71, "bahce siniri — avlu duvari tas, bahce citi calidir"),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "mahalle"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "mahalle",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    ap.add_argument("--export", action="store_true",
                    help="FBX yaz (yoksa yalniz .blend ve katalog)")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)

    mevcut = []
    if os.path.exists(args.catalog):
        with open(args.catalog, encoding="utf-8") as fh:
            mevcut = json.load(fh).get("variants", [])
    yeni = {f"SM_{n}" for n, _, _, _, _ in VARIANTS}
    catalog = [v for v in mevcut if v.get("name") not in yeni]

    for name, tur, olcek, tohum, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        asset = f"SM_{name}"
        p = hk.HayatParams(tur=tur, olcek=olcek, tohum=tohum)
        lod0, lod1, ucx, info = hk.build_hayat(
            p, col, asset, textured=not args.no_textures)
        _ = (lod1, ucx)

        # PIVOT TABANDA: yerlestirici nesneyi zemine oturtur ve bunu
        # pivotun tabanda oldugunu VARSAYARAK yapar. Varsayimi burada
        # sinamak, sahnede havada duran bir kupu aramaktan ucuz.
        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot tabanda degil "
                             f"({info['pivot_min_z']})")
        # INSAN OLCEGI: 1,70 m'lik figurun yanina konacak; 2,6 m'yi asan
        # bir sey avlu donatisi degil, kucuk bir yapidir.
        if info["height"] > 2.6:
            raise SystemExit(f"[HZ] HATA {name}: {info['height']} m — "
                             "avlu donatisi degil, yapi olur")

        info.update(name=asset, prefab=f"PF_{name}", why=why,
                    tier=TIER, source=SOURCE)
        catalog.append(info)

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        if args.export:
            export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                       collection_name=COLLECTION)
        hz.log(f"{name:12s} {info['footprint_x']:.2f} x "
               f"{info['footprint_y']:.2f} x {info['height']:.2f} m, "
               f"{info['tris_lod0']:4d} ucgen  {why}")

    catalog.sort(key=lambda v: v["name"])
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(VARIANTS)} hayat donatisi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
