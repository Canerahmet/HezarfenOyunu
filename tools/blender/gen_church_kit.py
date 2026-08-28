"""
Hezarfen: 1632 — Kilise ve sinagog üreticisi (plan Faz 2b).

Galata'nın Latin bazilikası, suriçi/Fener'in mütevazı kilisesi ve Balat'ın
sinagogu. Gerekçeler `lib/church_kit.py` başlığında ve ADR 0018'de.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_church_kit.py --
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
import church_kit as ck            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

# Tarihsel kademe ve kaynak notu — Unity prefabındaki `HistoricalTag`e buradan
# geçer. Boru hattı prefabı her koşuşta yeniden yazar; etiket elle konursa
# ilk yeniden üretimde sessizce kaybolur. Bu yüzden kaynağı katalogdur.
TIERS = {
    "kilise_latin": ("T2",
                     "Galata Ceneviz kilisesi tipolojisi: uc nefli bazilika, "
                     "sivri kemer pencere, ahsap cati, kare planli can kulesi "
                     "(San Domenico / Arap Camii, 1323-37). Galata 1453'te "
                     "antlasmayla teslim oldugu icin Latin kiliseleri bicimini "
                     "korudu. RESEARCH.md 3-Galata, 4.2. Olculer rekonstruksiyon."),
    "kilise_orthodox": ("T2",
                        "Surici/Fener-Balat Rum-Ermeni kilisesi tipolojisi: uc nef "
                        "tek ahsap besik cati altinda, can kulesi YOK, sokaktan "
                        "alcak. Zimmi kisiti. RESEARCH.md 4.2. Olculer "
                        "rekonstruksiyon; belirli bir yapiya karsilik gelmez."),
    # ARAP CAMII — TIPOLOJI DEGIL, ADI OLAN BIR YAPI.
    #
    # Digerleri "Galata kilisesi nasil olurdu" sorusunun cevabidir (T2,
    # rekonstruksiyon). Bu degil: San Domenico ayakta duruyor, olculeri
    # kaynakta yazili ve yapinin 1632'deki hali BILINIYOR — 1475'ten beri
    # camidir. O yuzden T1.
    "arap_camii": ("T1",
                   "San Domenico (~1323-37), 1475'ten beri Arap Camii. "
                   "Uc nefli bazilika, 40 x 15 m, moloz tas ve tugla "
                   "almasik orgu, sivri kemerli pencereler, ahsap cati; "
                   "orta nef yan neflerden yuksek; KARE PLANLI CAN KULESI "
                   "(RESEARCH.md 4.2(a): 'sonradan minareye cevrilen kule "
                   "budur'). Kaynak: Koc U. Istanbul Surlari, Arap Camii / "
                   "San Domenico; Mitler, The Genoese in Galata 1453-1682."),
    "sinagog": ("T2",
                "Balat/Haskoy sinagogu tipolojisi: dikdortgen kagir+ahsap salon, "
                "kendine ozgu dis mimarisi yok, yuksek duvarli avlu icinde; "
                "kadinlar mahfili ust pencere sirasi olarak okunur. "
                "RESEARCH.md 4.2. Olculer rekonstruksiyon."),
}

# (ad, tur, gerekce, parametreler)
VARIANTS = [
    ("Kilise_Latin_A", "kilise",
     "Galata Ceneviz kilisesi — uc nefli bazilika, can kuleli",
     dict(kind="latin")),
    ("Kilise_Latin_B", "kilise",
     "kucuk Latin kilisesi — kulesiz, dar parselde",
     dict(kind="latin", nave_w=6.0, aisle_w=2.9, length=17.0, bays=4,
          tower=False, aisle_h=5.4, nave_h=8.8, apse_r=2.8)),
    ("Kilise_Rum_A", "kilise",
     "surici/Fener kilisesi — tek besik cati, kulesiz, alcak",
     dict(kind="orthodox", nave_w=6.4, aisle_w=3.1, length=20.0, bays=4,
          aisle_h=5.6, nave_h=5.6, sink=1.6, apse_r=2.9,
          window_sill=3.10, window_spring=4.15, window_w=0.72,
          portal_w=1.55, portal_spring=2.25)),
    ("Kilise_Rum_B", "kilise",
     "mahalle kilisesi — kucuk, apsissiz olcek",
     dict(kind="orthodox", nave_w=5.2, aisle_w=2.5, length=13.0, bays=3,
          aisle_h=5.0, nave_h=5.0, sink=1.2, apse_r=2.4,
          window_sill=2.80, window_spring=3.75, window_w=0.68,
          portal_w=1.45, portal_spring=2.15)),
    # Olculer kaynaktan: 40 x 15 m DIS olcu.
    #   outer_l = length + 2*wall_t = 38.5 + 1.5 = 40.0
    #   outer_w = nave_w + 2*aisle_w + 2*wall_t = 6.5 + 7.0 + 1.5 = 15.0
    # Uydurulan tek sey yukseklikler ve pencere ritmi; kutle olculu.
    ("ArapCamii", "kilise",
     "San Domenico / Arap Camii — cami olmus Ceneviz bazilikasi, "
     "kare kule minare",
     dict(kind="latin", nave_w=6.5, aisle_w=3.5, length=38.5, bays=8,
          aisle_h=7.0, nave_h=11.5, apse_r=4.2,
          tower=True, tower_h=22.0, tower_w=4.20,
          # Hac inik, serefe kurulu: kule artik minare.
          cross=False, serefe=True),
     "arap_camii"),

    ("Sinagog_A", "sinagog",
     "Balat sinagogu — avluya bakan, kadinlar mahfilli",
     dict()),
    ("Sinagog_B", "sinagog",
     "kucuk cemaat sinagogu — mahfilsiz, tek sira pencere",
     dict(width=9.0, length=11.5, stone_h=2.9, height=5.2, bays=2,
          gallery=False)),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "church"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "church",
                                                      "catalog.json"))
    ap.add_argument("--no-textures", action="store_true")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    for varyant in VARIANTS:
        # Bes elemanli varyant kendi TIERS anahtarini soyler: adi olan bir
        # yapi (Arap Camii) tipolojinin kaydini tasiyamaz.
        name, kind, why, params = varyant[:4]
        tier_key = varyant[4] if len(varyant) > 4 else None
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        tex = not args.no_textures

        if kind == "kilise":
            lod0, lod1, ucx, info = ck.build_kilise(
                ck.KiliseParams(**params), col, name, textured=tex)
        elif kind == "sinagog":
            lod0, lod1, ucx, info = ck.build_sinagog(
                ck.SinagogParams(**params), col, name, textured=tex)
        else:
            raise SystemExit(f"[HZ] bilinmeyen tur: {kind}")

        if abs(info["pivot_min_z"]) > 1e-3:
            raise SystemExit(f"[HZ] HATA {name}: pivot taban merkezde degil "
                             f"({info['pivot_min_z']})")

        hz.save_blend(os.path.join(args.blend_dir, f"SM_{name}.blend"))
        export_fbx(os.path.join(args.out_dir, f"SM_{name}.fbx"),
                   collection_name=COLLECTION)

        tier, source = TIERS[tier_key or info["kind"]]
        info.update(name=name, why=why, prefab=f"PF_{name}",
                    tier=tier, source=source)
        catalog.append(info)
        hz.log(f"{name:16s} {info['footprint_x']:5.2f}x{info['footprint_y']:5.2f}"
               f"x{info['height']:5.2f} m {info['tris_lod0']:6d} ucgen  {why}")

    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} ibadet yapisi; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
