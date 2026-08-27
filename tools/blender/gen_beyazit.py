"""
Hezarfen: 1632 - Beyazit II Camii.

Ayrinti ve gerekce: `lib/sinan_kit.py`, RESEARCH.md 5.17, ADR 0051.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_beyazit.py -- \
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
    "Beyazit II Camii, **1501-1506** (Temmuz 1501'de baslandi, 911/1505'te "
    "tamamlandi); bani II. Bayezid. 1632'de **126 yasinda**. "
    "DOGRULUK BASAMAGI **D2**: kubbe capi, harim olcusu ve minareler arasi "
    "aciklik OLCULUDUR; kilit kotu turetilmistir ve asagida D3 diye "
    "isaretlidir. "
    "**1632'DE BURADA BIR SANTIYE OLABILIR VE SAHIBI OYUNUN PADISAHIDIR**: "
    "TDV, sadirvanin uzerindeki SEKIZ SUTUNA oturan kubbeyi **IV. MURAD**'in "
    "eklettigini yazar, **1623-1640** arasi. Oyunun gectigi yil o araligin "
    "TAM ORTASI. Model kubbeyi KOYMAZ; gerekce tarihsel: Murad IV 1623'te "
    "on bir yasinda tahta cikti ve gercek iktidari 1632'de aldi, buyuk "
    "hayrat isleri ondan sonra beklenir. Ama bu bir OLASILIKTIR, kesinlik "
    "degil — katalog `sadirvan_dome=false` diye kaydeder ki karar gorunur "
    "kalsin. "
    "OLCULER: ana kubbe **16,78 m** cap; harim ic olcusu **37,06 x 36,80 m** "
    "(kaynak 'kare bicimli' der, olcu 26 cm fark verir — tarif olcuyle "
    "dogrulandi). "
    "SAYILAN: **IKI** yarim kubbe kible ekseninde; **DORT** paye; ana "
    "kubbede **YIRMI** pencere, her yarim kubbede **YEDISER**; **IKI** "
    "minare, **BIRER** serefeli; tabhanede **DORDER** kubbeli hucre; avluda "
    "**YIRMI DORT** kubbeli revak. "
    "**MINARELER CAMIYE DEGIL TABHANE KANATLARINA BITISIKTIR** ve aralarinda "
    "**79 m** vardir. Bu olcu yapinin en taninan sayisal ozelligidir ve "
    "kutlenin genisligini BAGLAR: kanat uzunlugu ondan turer, elle "
    "girilmez. Tabhaneli plan 'kanatli yapilarin son ornegi' sayilir. "
    "KILIT KOTU **D3, TURETILDI**: yayimlanan bir kot yok. Iki kisitla "
    "baglandi — sacak iki katli yan neflerin catisini gecmeli, ve "
    "kilit/cap orani olculu dort selatin camisinin bandina dusmeli "
    "(Ayasofya 1,68, Sultanahmet 1,83, Suleymaniye 2,00, Uskudar Mihrimah "
    "2,12). Beyazit'ta 35,00 / 16,78 = **2,09**; dogrulama bandin disina "
    "cikan bir kotu REDDEDER. "
    "TABHANE HUCRE SAYISINDA CELISKI VAR: TDV 'kubbeli DORDER hucre' der, "
    "yaygin anlatim 'BESER kubbe'. Ikisi ayni seyi saymiyor olabilir "
    "(hucre != kubbe). TDV alindi, celiski kayda gecti. "
    "1509 DEPREMINDE kubbe 'dagilip pare pare' oldu ve medrese yikildi; "
    "SINAN 1573-74'te 'bir kemer-i cedidle' yapiyi takviye etti. Yani "
    "1632'de ayakta olan sey iki yapisal mudahaleden gecmistir ama bicimi "
    "degismemistir. "
    "1632'DE VAR: medrese (1507), sibyan mektebi (1507), imaret, "
    "kervansaray (1507-08), II. Bayezid turbesi (Yavuz Selim yaptirdi, "
    "kible tarafinda). "
    "1632'DE YOK: Veliyyuddin Efendi kutuphanesi (1181/1767-68); ve "
    "buyuk olasilikla sadirvanin kubbesi. "
    "Kaynaklar: TDV Islam Ansiklopedisi 'Beyazit II Camii ve Kulliyesi'; "
    "Teknevia; istanbul.net.tr. RESEARCH.md 5.17, ADR 0051"
)


def add_args(p):
    p.add_argument("--asset", default="Beyazit")
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--sadirvan-dome", action="store_true",
                   help="IV. Murad'in kubbesini KOY (1632 icin varsayilan: hayir)")
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

    p = sk.BeyazitParams(sadirvan_dome=a.sadirvan_dome, palette=a.palette)
    lod0, lod1, ucx, info = sk.build_beyazit(p, col, a.asset,
                                             textured=a.textured)

    if info["half_domes"] != 2 or info["piers"] != 4:
        raise SystemExit("[HZ] HATA: IKI yarim kubbe, DORT paye.")
    if info["dome_windows"] != 20 or info["half_dome_windows"] != 7:
        raise SystemExit("[HZ] HATA: kubbede YIRMI, yarim kubbede YEDISER "
                         "pencere.")
    if info["minarets"] != 2 or info["sherefe_total"] != 2:
        raise SystemExit("[HZ] HATA: IKI minare, BIRER serefe.")
    if abs(info["minaret_span"] - 79.0) > 0.01:
        raise SystemExit("[HZ] HATA: minareler arasi OLCULU 79 m.")
    if info["tabhane_cells"] != 4:
        raise SystemExit("[HZ] HATA: tabhanede DORDER hucre (TDV).")
    if info["portico_bays"] != 24:
        raise SystemExit("[HZ] HATA: avluda YIRMI DORT kubbe.")
    if info["sadirvan_dome"]:
        hz.log("UYARI: sadirvan kubbesi KONDU — IV. Murad'in ekidir "
               "(1623-1640) ve 1632'de buyuk olasilikla YOKTUR.")

    hz.log(f"{a.asset}: kubbe {p.dome_d:.2f} m (OLCULU), kilit "
           f"{p.crown_z:.2f} m (TURETILEN, oran "
           f"{p.crown_z / p.dome_d:.2f}); mesh capi "
           f"{info['measured_dome_d']:.2f}")
    hz.log(f"harim {p.hall_w:.2f} x {p.hall_d:.2f} m (OLCULU, 'kare bicimli')")
    hz.log(f"minareler arasi {info['minaret_span']:.1f} m (OLCULU) -> tabhane "
           f"kanadi {info['wing_len']:.2f} m (TUREDI)")
    hz.log(f"{info['dome_windows']} + 2x{info['half_dome_windows']} pencere, "
           f"{info['tabhane_cells_total']} tabhane hucresi, "
           f"{info['portico_bays']} avlu kubbesi")
    hz.log(f"sadirvan kubbesi: {'VAR' if info['sadirvan_dome'] else 'YOK'} "
           "(IV. Murad, 1623-1640)")
    hz.log(f"ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, LOD0={info['tris_lod0']}")

    if abs(info["pivot_min_z"]) > 0.01:
        raise SystemExit(f"[HZ] HATA pivot {info['pivot_min_z']:.3f}")

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
    hz.log("gen_beyazit OK")


if __name__ == "__main__":
    main()
