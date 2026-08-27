"""
Hezarfen: 1632 - Suleymaniye Camii.

Ayrinti ve gerekce: `lib/sinan_kit.py`, RESEARCH.md 5.10, ADR 0044.

Kullanim:
  blender --background --factory-startup --python tools/blender/gen_suleymaniye.py -- \
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
import ottoman_kit as kit    # noqa: E402
import sinan_kit as sk             # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

SOURCE = (
    "Suleymaniye Camii, 1550-1557, Mimar Sinan; bani Kanuni Sultan "
    "Suleyman. 1632'de 75 yasinda. "
    "**BU YAPIDA TANIDIK SILUET DOGRUDUR** ve bunu yazmak duzeltmeler "
    "kadar onemlidir: Galata Kulesi, Adalet Kulesi, Kiz Kulesi, Yeni Cami "
    "ve Alay Kosku'nde taninan goruntu sonraki yuzyillarin eseriydi; "
    "Suleymaniye 1557'de tamamlandi ve 1632'ye kadar bicimini degistiren "
    "bir olay yok. Onu hirpalayan 1660 YANGINI ve 1766 DEPREMI sonradir. "
    "Kural 'her sey farklidir' degil, HER SEY SORULUR. "
    "OLCULER (D2): kubbe **26,5 m** cap (kaynaklarda 27,5 de gecer — "
    "Mihrimah ve Yeni Cami'dekiyle ayni ic/dis ikiligi), kilit **53 m**; "
    "**IKI** yarim kubbe ANA EKSENDE (Ayasofya semasi; Uskudar "
    "Mihrimah'ta UC idi); **DORT** minare, **ON** serefe (3+3+2+2); harim "
    "yaklasik 68x63 m. "
    "1632'DE YOK: 1660 yangini ve 1766 depremi onarimlari. "
    "Kutlenin kubbe disindaki gecmesi olculen kubbeden TUREDI. "
    "RESEARCH.md 5.10, ADR 0044"
)


def add_args(p):
    p.add_argument("--asset", default="Suleymaniye")
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

    p = sk.SuleymaniyeParams(palette=a.palette)
    # Uc kademe: tam / orta / blok. Orta kademe ayni ureteçten, daha az
    # bolutle kurulur — gerekçe `ottoman_kit.build_with_mid_lod`.
    lod0, lod1, lod2, ucx, info = kit.build_with_mid_lod(
        sk.build_suleymaniye, p, col, a.asset, textured=a.textured)

    if info["sherefe_total"] != 10 or info["minarets"] != 4:
        raise SystemExit("[HZ] HATA: DORT minare ve ON serefe olmali.")
    if info["half_domes"] != 2:
        raise SystemExit("[HZ] HATA: IKI yarim kubbe (ana eksende).")

    hz.log(f"{a.asset}: kubbe {p.dome_d:.2f} m / kilit {p.crown_z:.2f} m "
           f"(OLCULU), mesh capi {info['measured_dome_d']:.2f}")
    hz.log(f"{info['minarets']} minare / {info['sherefe_total']} serefe "
           f"{info['sherefe_each']}, {info['half_domes']} yarim kubbe")
    hz.log(f"ayak izi {info['footprint_x']:.1f}x{info['footprint_y']:.1f} m, "
           f"yukseklik {info['height']:.2f} m, "
           f"LOD 0/1/2 = {info['tris_lod0']}/{info['tris_lod1']}/"
           f"{info['tris_lod2']}")

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
    hz.log("gen_suleymaniye OK")


if __name__ == "__main__":
    main()
