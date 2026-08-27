"""
Hezarfen: 1632 — Yeni Cami'nin **bitmemiş kabuğu**.

1632'de Eminönü'nde duran şey bir cami değil, **29 yıldır terk edilmiş bir
şantiyedir** ve halk ona **"Zulmiye"** der. Ayrıntı ve gerekçe:
`lib/sinan_kit.py`, RESEARCH.md §5.9, ADR 0042.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_yeni_cami_harabe.py -- \
      --textured --out-dir unity/HezarfenGame/Assets/_Import
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
    "Yeni Cami (Eminonu), **1632'DE BITMEMIS BIR KABUK**. Insaat 1597'de "
    "Safiye Sultan'in emriyle basladi; mimar DAVUD AGA, ardindan DALGIC "
    "AHMED CAVUS. **1603**'te III. Mehmed olunce Safiye Sultan Eski "
    "Saray'a gonderildi ve is DURDU; 1604'te Safiye Sultan'in olumuyle "
    "tamamen birakildi. Yapi **57 YIL** oyle kaldi; 1660 Eminonu "
    "yangınından sonra TURHAN SULTAN surdurdu ve **1663**'te tamamlandi "
    "(kulliye 1665). "
    "**1632'DE KABUK 29 YASINDA VE TERK EDILMIS.** "
    "NEREYE KADAR: is durdugunda yapi **ILK PENCERE SEVIYESINE** kadar "
    "yukselmisti. Yani 1632'de gorulen sey duvarlar ve fil ayaklaridir — "
    "CATISIZ, kubbesiz, minaresiz, kursunsuz. "
    "HALKIN ADI **ZULMIYE**: asiri masraf ek vergilere yol actigi ve yapi "
    "harabeye dondugu icin Istanbullular boyle derdi. "
    "OLCULU olan: harim plani **35,50 x 40,90 m**. Kubbe capi kaynaklarda "
    "16,20 m (mimari tarif) ve 17,5 m (yaygin anlatim) olarak dolasir — "
    "muhtemelen ic/dis farki; 1632 kabugunda ikisi de gorunmez. Ana kubbe "
    "DORT fil ayagina oturacakti. "
    "1632'DE YOK: kubbe ve dort yarim kubbe, minareler, kursun ortu, "
    "revakli avlu, Hunkar Kasri, Misir Carsisi (hepsi 1660-65). "
    "Duvar YUKSEKLIGI olculmedi; 'ilk pencere seviyesi' tarifinden "
    "turetildi ve **D3**'tur. RESEARCH.md 5.9, ADR 0042"
)


def add_args(p):
    p.add_argument("--asset", default="YeniCamiHarabe")
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--out-dir", default=None)
    p.add_argument("--blend-dir", default=os.path.join("art", "blend", "landmark"))
    p.add_argument("--catalog", default=os.path.join("art", "blend", "landmark",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = sk.YeniCamiHarabeParams(palette=a.palette)
    lod0, lod1, ucx, info = sk.build_yeni_cami_harabe(p, col, a.asset,
                                                      textured=a.textured)

    # KABUK OLDUGU KUTLEDEN SORULUR, bayraktan degil.
    #
    # Hudayi turbesinde `acik` bayragi okunmadigi halde "acik" diye
    # kataloglanmisti (ADR 0037). Burada da ayni tuzak var: "roofed=False"
    # yazmak, catisiz oldugunu KANITLAMAZ. Olcu, kutlenin yuksekliginin
    # duvar yuksekligini asmamasidir — kubbe olsaydi asardi.
    ceiling = info["wall_h"] + 1.10 + 2.0 * p.course + 0.05
    if info["height"] > ceiling:
        raise SystemExit(f"[HZ] HATA kabuk {info['height']:.2f} m — en fazla "
                         f"{ceiling:.2f} m olmali. Bir sey CATI gibi "
                         "yukseliyor; 1632'de bu yapinin ortusu YOKTUR.")
    if info["minarets"] != 0 or info["roofed"]:
        raise SystemExit("[HZ] HATA: 1632 kabugunda minare ve ortu YOKTUR.")

    hz.log(f"{a.asset}: harim {info['harim_w']:.2f}x{info['harim_d']:.2f} m "
           f"(OLCULU), duvar {info['wall_h']:.2f} m, {info['piers']} fil ayagi")
    hz.log(f"ayak izi {info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"toplam yukseklik {info['height']:.2f} m (tavan {ceiling:.2f}), "
           f"LOD0={info['tris_lod0']}")
    hz.log("kabuk OK: catisiz, kubbesiz, minaresiz")

    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f} — "
                         "subasman tabani z=0 olmali")

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

    if a.blend_dir:
        os.makedirs(a.blend_dir, exist_ok=True)
        hz.save_blend(os.path.join(a.blend_dir, f"{a.asset}.blend"))
    if a.out_dir:
        os.makedirs(a.out_dir, exist_ok=True)
        export_fbx(os.path.join(a.out_dir, f"SM_{a.asset}.fbx"),
                   collection_name=COLLECTION)
    hz.log("gen_yeni_cami_harabe OK")


if __name__ == "__main__":
    main()
