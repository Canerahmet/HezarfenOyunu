"""
Hezarfen: 1632 — Kayık ve pereme üreticisi (Faz 4 donatı geçişi).

Gerekçeler ve kaynak: `tools/blender/lib/kayik_kit.py` başlığı, RESEARCH.md
"Ulaşım": *"kayık ve pereme (deniz taksisi) ana ulaşım; iskeleler tarifeli
… Boğaz ve Haliç geçişleri kayıkla."*

Kullanım:
  blender --background --python tools/blender/gen_kayik.py
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
import kayik_kit as kk             # noqa: E402
import ottoman_kit as kit          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

TIER = "T1"
SOURCE = (
    "KAYIK ve PEREME, 1632. RESEARCH.md 'Ulasim': 'kayik ve pereme (deniz "
    "taksisi) ana ulasim; iskeleler tarifeli; at/katir karada; tekerlekli "
    "araba nadir. Bogaz ve Halic gecisleri kayikla.' "
    "**HALIC'TE KOPRU YOKTUR** — yani kayik bir sus degil, sehrin ulasim "
    "sisteminin kendisidir; bos bir Halic bos bir cadde kadar yanlistir. "
    "Kaynak IKI tip adlandiriyor ve kit tam o kadarini uretir; ucuncu bir "
    "tip (mavna, cektiri) eklemek kaynagin soylemedigini soylemek olurdu. "
    "OLCU YOK: peremenin boyu, kurek sayisi, bordasi kayitli degil -> "
    "kutle **D3, taslak**. Tek sayisal iddia boy/en oranidir (3,4-5,6): "
    "daha tombul bir tekne kurekle gitmez, daha ince olan devrilir — bu "
    "olcu uydurmak degil, kurekli teknenin isleme kisitidir."
)

#: (ad, tip, kürek çifti, oturak, neden)
VARIANTS = [
    ("Kayik",       "kayik",  1, 2, "kurekli kucuk tekne — kiyi trafigi"),
    ("Kayik_Bagli", "kayik",  0, 2, "iskeleye bagli, kureksiz"),
    ("Pereme",      "pereme", 2, 3, "deniz taksisi — Halic ve Bogaz gecisi"),
    ("Pereme_Bagli","pereme", 0, 3, "iskelede bekleyen pereme"),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "works"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "works",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)

    mevcut = []
    if os.path.exists(args.catalog):
        with open(args.catalog, encoding="utf-8") as fh:
            mevcut = json.load(fh).get("variants", [])
    yeni_adlar = {f"SM_{n}" for n, _, _, _, _ in VARIANTS}
    catalog = [v for v in mevcut if v.get("name") not in yeni_adlar]

    for name, kind, oars, thwarts, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        asset = f"SM_{name}"
        p = kk.KayikParams(kind=kind, oars=oars, thwarts=thwarts)
        lod0, lod1, ucx, info = kk.build_kayik(
            p, col, asset, textured=not args.no_textures)
        _ = (lod1, ucx)

        # --- PIVOT: SU HATTINDA, taban merkezinde DEGIL ------------------
        #
        # Boru hattinin genel kurali pivotun tabanda olmasidir ve dogrudur:
        # bir ev zemine oturur. Tekne oturmaz, YUZER. Pivot omurgada olsaydi
        # y=0'a konan bir kayik suyun USTUNDE dururdu; su hattinda olunca
        # dogru batar. Bu bilincli bir istisnadir ve kendi denetimi vardir:
        # gövde su hattinin ALTINA inmeli (su cekimi), USTUNE cikmali
        # (borda). Ikisinden biri sifirsa tekne tekne degildir.
        cekim = -info["pivot_min_z"]
        # DIKKAT: bu "borda" DEGIL. Yukseklikten su cekimini cikarinca
        # elde edilen sey suyun ustundeki EN YUKSEK NOKTAdir — bas kasarasi
        # ve kurekler dahil. Gercek orta borda gövde derinliginden turer.
        ust = info["height"] - cekim
        borda = info["draft_depth"] * (1.0 - kk.DRAFT_RATIO)
        if cekim < 0.15:
            raise SystemExit(f"[HZ] HATA {name}: su cekimi {cekim:.3f} m — "
                             "govde su hattinin altina inmiyor, pivot yanlis")
        if ust < 0.15:
            raise SystemExit(f"[HZ] HATA {name}: su ustu {ust:.3f} m — "
                             "govde su hattinin ustune cikmiyor")

        info.update(name=asset, prefab=f"PF_{name}", why=why,
                    tier=TIER, source=SOURCE,
                    waterline_pivot=True,
                    draft=round(cekim, 3),
                    freeboard=round(borda, 3),
                    above_water=round(ust, 3))
        catalog.append(info)

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                   collection_name=COLLECTION)
        hz.log(f"{name:14s} {info['footprint_x']:5.2f} m boy, "
               f"boy/en {info['length_beam']:.2f}, su cekimi {cekim:.2f} m, "
               f"orta borda {borda:.2f} m, {info['tris_lod0']:5d} ucgen  {why}")

    catalog.sort(key=lambda v: v["name"])
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(VARIANTS)} tekne; katalog: {args.catalog}")
    _ = kit


if __name__ == "__main__":
    main()
