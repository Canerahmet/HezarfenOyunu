"""
Hezarfen: 1632 — Üsküdar Mihrimah Sultan (İskele) Camii üreticisi.

Hezarfen'in **iniş noktasının** silüetini bu yapı belirler. Ölçü ve
gerekçeler: `lib/sinan_kit.py`, RESEARCH.md §5.4.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_uskudar_mihrimah.py -- \
      --textured --out-fbx unity/HezarfenGame/Assets/_Import/SM_UskudarMihrimah.fbx
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import sinan_kit as sk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Uskudar Mihrimah Sultan (Iskele) Camii, 954/1548, Mimar Sinan. "
    "Kanuni Sultan Suleyman'in kizi Mihrimah Sultan yaptirdi; yapima "
    "Necipoglu'na gore 1543-44'te baslandi. **KUBBE OLCULU (D2)**: dis cap "
    "11,40 m, ic cap 10,00 m, kubbe yuksekligi 24,20 m. Plan **uc yarim "
    "kubbeli** (iki yan + kible) ve bu tipin Istanbul'daki ILK ve TEK "
    "ornegidir — giris yonunde yarim kubbe YOKTUR. Bes kubbeli birinci "
    "revak, alti mermer sutun (mukarnasli baslik); ikinci revak baklava "
    "dilimli baslikli ve ahsap ortulu. Cift revak Sinan'in OZGUN tipidir: "
    "bes gozlu cift revakli yedi caminin ilki. Cifte minare, her biri TEK "
    "serefeli (Hadikatu'l Cevami: 'birer serefeli minaresi'). Set/dis avlu "
    "~2 m, merdivenle cikilir (TDV). "
    "1632'DE VAR: cami, medrese (16 hucre), sibyan mektebi, imaret-tabhane "
    "(1722'de yandi), han/Kursunlu Han (1920'lerde coktu), suyollari. "
    "1632'DE YOK: iki turbe, hamam, kasir, muvakkithane (hepsi sonraki "
    "donemlerde eklendi); gunes saati (18. yy); set duvarindaki cesme "
    "(17. yy, 1632'ye gore belirsiz); meydanin bugunku iki simgesi **YENI "
    "VALIDE CAMII** (1708-11, kitabe 1122/1710) ve **III. AHMED MEYDAN "
    "CESMESI** (1728). Kutlenin kubbe disindaki gecmesi olculen kubbeden "
    "TUREDI ve D3'tur; minare yuksekligi icin yazili kural: serefe ana "
    "kubbe kilidi kotunda. "
    "Kaynak: Vardar, Kadriye Figen (2021), 'Uskudar Mihrimah Sultan Camii "
    "Tas Suslemelerinin Degerlendirilmesi', Sanat Tarihi Dergisi 30/2, "
    "1389-1419; TDV Islam Ansiklopedisi 'Mihrimah Sultan Kulliyesi "
    "(Uskudar)'; Kuban 2016; Necipoglu 2013. RESEARCH.md 5.4"
)


def add_args(p):
    p.add_argument("--asset", default="UskudarMihrimah")
    p.add_argument("--palette", default="default")
    p.add_argument("--no-outer-revak", action="store_true",
                   help="ikinci revagi birak (karsilastirma icin)")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = sk.MihrimahParams(palette=a.palette,
                          outer_revak=not a.no_outer_revak)
    lod0, lod1, ucx, info = sk.build_mihrimah(p, col, a.asset,
                                              textured=a.textured)

    hz.log(f"{a.asset}: kubbe {p.dome_d:.2f} m cap / {p.crown_z:.2f} m kilit "
           f"(OLCULU), harim {p.hall_w:.1f}x{p.hall_d:.1f} m (turetilen)")
    hz.log(f"kemer kotu {p.arch_z:.2f} m, kubbe etegi {p.spring_z:.2f} m, "
           f"serefe {p.sherefe_z:.2f} m, minare tepesi {info['minaret_top']:.2f} m")
    hz.log(f"ayak izi {info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"toplam {info['height']:.2f} m, LOD0={info['tris_lod0']}, "
           f"LOD1={info['tris_lod1']}")

    # Pivot ZEMINDE: yapi araziye oturacak, kule gibi suya degil.
    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f} — "
                         "set tabani z=0 olmali")
    hz.log("pivot OK: set tabani z=0")

    # Ana kubbe, yarim kubbelerin USTUNDE kalmali; degilse siluet duzlesir
    # ve yapi "uc yarim kubbeli" degil "cok kubbeli" okunur.
    half_crown = p.arch_z + p.half_r * sk.DOME_RISE_RATIO
    if p.crown_z - half_crown < 1.5:
        raise SystemExit(f"[HZ] HATA ana kubbe kilidi {p.crown_z:.2f} m, "
                         f"yarim kubbe kilidi {half_crown:.2f} m — fark "
                         "1,5 m'den az, siluet duzlesiyor")
    hz.log(f"siluet OK: ana kubbe yarim kubbelerden "
           f"{p.crown_z - half_crown:.2f} m yuksek")

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
    hz.log("gen_uskudar_mihrimah OK")


if __name__ == "__main__":
    main()
