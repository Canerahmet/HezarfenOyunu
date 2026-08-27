"""
Hezarfen: 1632 — Üretim, ticaret ve su yapıları üreticisi (Faz 2b'nin kalanı).

İmaret · arasta · bozahane · değirmen · su terazisi · muvakkithane.
Gerekçeler ve kaynaklar: `tools/blender/lib/works_kit.py` başlığı, RESEARCH.md §4.7.

Kullanım:
  blender --background --python tools/blender/gen_works_kit.py
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
import ottoman_kit as kit          # noqa: E402
import works_kit as wk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

# Tarihsel kademe ve KAYNAK — Unity `HistoricalTag`'e buradan gider.
# Katalog tek kaynaktır; prefab her koşuşta yeniden yazılır, elle konan
# etiket kaybolur.
TIERS = {
    "imaret": ("T2",
               "Imaret bir ASEVI degil MUTFAK TESISIDIR: mutfak, yemekhane, "
               "ekmekhane (fodla), kiler, gorevli odalari, avlu. Mutfak "
               "mekanlari dikdortgen planda YAN YANA dizilir ve kubbeyle "
               "ortulur; degismeyen unsur FARKLI BOYDAKI OCAK BACALARIDIR. "
               "TDV 'Imaret'; METU JFA 2016/1. Olculer REKONSTRUKSIYON."),
    "arasta": ("T2",
               "Arasta 'bir eksen uzerinde dizilmis dukkan siralari'dir; "
               "dukkanlarin AYRI KAPISI YOKTUR, sabah aksam birlikte acilip "
               "kapanirlar — yani arasta tek bir YAPIDIR. Goz genisligi "
               "3,5 m: Selimiye Arastasi 256 m'de 73 kemer tasir (256/73). "
               "Vikipedi 'Arasta'; AA Selimiye Arastasi. Olculer T2."),
    "bozahane": ("T1",
                 "1632'DE ACIK. IV. Murad'in emriyle yapilan 1638 esnaf "
                 "sayiminda Istanbul'da 300 BOZAHANE ve ~1100 bozaci vardir; "
                 "ayrica ACI BOZA ureten ~40 esnaf. Aci boza sarhos edecek "
                 "kadar alkolludur ve bozahaneler IV. MURAD DONEMINDE "
                 "KAPATILMISTIR (1623-1640) — kahvehaneden sonra oyunun "
                 "IKINCI ZAMAN ISARETI. Bicim tipolojik."),
    "degirmen": ("T2",
                 "Su degirmeninde su, degirmenin yanina getirilip 5-6 M "
                 "UZUNLUGUNDA TAS BIR OLUKLA asagi akitilir ve carki "
                 "dondurur. 'at' varyantinda oluk ve cark yoktur; gucu "
                 "hayvan verir. Evliya (17. yy) Goksu'yu 'degirmenlerle "
                 "cevrili' anlatir. Kultur Portali 'Degirmencilik'."),
    "su_terazisi": ("T2",
                    "'Kule seklinde kagir yapi': su tepedeki HAZNEYE cikar, "
                    "oradan kunklerle bir sonraki teraziye gider; amac fazla "
                    "basincin kunkleri patlatmasini onlemektir. 1632'de "
                    "VARDIR — Kirkcesme tesisleri (Kanuni, Sinan, ~1563) "
                    "55 km'lik hat boyunca su terazileri tasir. "
                    "Vikipedi 'Su terazisi (yapi)'; TDV 'Kirkcesme Sulari'."),
    "muvakkithane": ("T2",
                     "1632'de VARDIR (Istanbul'un ilki Fatih Camii, 1470) ama "
                     "YAYGIN DEGILDIR: yayginlasmasi 18. yy sonu-19. yy "
                     "basidir. 1632'de muvakkithane MAHALLE MESCIDINE DEGIL, "
                     "SELATIN CAMISINE aittir — YERLESTIRME KURALI budur. "
                     "Bicim: 'bir iki odadan buyuk olmayan', cami avlusunda, "
                     "buyuk sebekeli pencereli. 17. yy muvakkitleri: Ahmed "
                     "Naksi Efendi (Suleymaniye), Muneccimek Mehmed (Fatih)."),
}

VARIANTS = [
    ("Imaret_A", "imaret", "kulliye imareti — dort kubbeli mutfak gozu + ekmekhane",
     dict()),
    ("Imaret_Kucuk", "imaret", "kucuk imaret — uc goz, ekmekhanesiz",
     dict(bays=3, bay=4.8, depth=6.2, wall_h=4.2, dome_h=1.55, ekmekhane=False,
          court_d=6.0)),
    ("Arasta_A", "arasta", "tonoz ortulu arasta — karsilikli sekiz goz",
     dict()),
    ("Arasta_Acik", "arasta", "ustu acik arasta — tek sira, tonozsuz",
     dict(bays=7, both_sides=False, vault=False, cell_d=3.8, wall_h=4.0)),
    ("Bozahane_A", "bozahane", "bozahane — sundurma altinda mayalanma kupleri",
     dict()),
    ("Degirmen_Su", "degirmen", "su degirmeni — tas oluk + dusey cark",
     dict()),
    ("Degirmen_At", "degirmen", "at degirmeni — donme diregi, carksiz",
     dict(kind="at", width=6.8, depth=6.8, wall_h=3.6)),
    ("SuTerazisi_A", "su_terazisi", "su terazisi — kagir kule + hazne",
     dict()),
    ("SuTerazisi_Kisa", "su_terazisi", "alcak su terazisi — yamacta",
     dict(height=6.0, base_side=2.0, top_side=1.35, hazne=0.9)),
    ("Muvakkithane_A", "muvakkithane", "muvakkithane — SELATIN camisi avlusunda",
     dict()),
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
    builders = {
        "imaret": (wk.ImaretParams, wk.build_imaret),
        "arasta": (wk.ArastaParams, wk.build_arasta),
        "bozahane": (wk.BozahaneParams, wk.build_bozahane),
        "degirmen": (wk.DegirmenParams, wk.build_degirmen),
        "su_terazisi": (wk.SuTeraziParams, wk.build_su_terazisi),
        "muvakkithane": (wk.MuvakkithaneParams, wk.build_muvakkithane),
    }
    catalog = []

    for name, kind, why, params in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        params_cls, build = builders[kind]
        lod0, lod1, ucx, info = build(params_cls(**params), col, name,
                                      textured=not args.no_textures)
        _ = (lod1, ucx)

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot taban merkezde degil "
                             f"(min_z={info['pivot_min_z']})")

        tier, source = TIERS[kind]
        info.update(name=name, prefab=f"PF_{name}", why=why, tier=tier,
                    source=source)
        catalog.append(info)

        hz.save_blend(os.path.join(args.blend_dir, f"SM_{name}.blend"))
        export_fbx(os.path.join(args.out_dir, f"SM_{name}.fbx"),
                   collection_name=COLLECTION)
        hz.log(f"{name:20s} {info['footprint_x']:6.2f}x{info['footprint_y']:6.2f}"
               f"x{info['height']:6.2f} m {info['tris_lod0']:6d} ucgen  {why}")

    catalog.sort(key=lambda v: v["name"])
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} uretim/su varligi; katalog: {args.catalog}")
    _ = kit


if __name__ == "__main__":
    main()
