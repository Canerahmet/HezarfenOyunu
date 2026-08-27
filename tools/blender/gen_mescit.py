"""
Hezarfen: 1632 — Mahalle mescidi üreticisi (plan Faz 2b).

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_mescit.py -- \
      --asset Mescit_A --textured --roof timber \
      --out-blend art/blend/SM_Mescit_A.blend \
      --out-fbx  unity/HezarfenGame/Assets/_Import/SM_Mescit_A.fbx
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import mosque_kit as mk            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"


def add_args(p):
    p.add_argument("--asset", default="Mescit_A")
    p.add_argument("--hall", type=float, default=9.0, help="Kare harim kenari (m)")
    p.add_argument("--wall-h", type=float, default=5.4)
    p.add_argument("--plinth", type=float, default=0.7)
    p.add_argument("--roof", default="timber", choices=mk.ROOF_TYPES)
    p.add_argument("--roof-pitch", type=float, default=28.0)
    p.add_argument("--eave", type=float, default=0.9)
    p.add_argument("--no-portico", action="store_true")
    p.add_argument("--portico-depth", type=float, default=3.0)
    p.add_argument("--portico-bays", type=int, default=3)
    p.add_argument("--no-minaret", action="store_true")
    p.add_argument("--minaret-h", type=float, default=19.0)
    p.add_argument("--minaret-side", type=int, default=-1, choices=(-1, 1))
    p.add_argument("--wall-thickness", type=float, default=0.55)
    p.add_argument("--palette", default="default")
    p.add_argument("--textured", action="store_true")
    p.add_argument("--info-json", default=None)
    p.add_argument("--catalog", default=os.path.join("art", "blend", "mosque",
                                                     "catalog.json"))
    return p


def main():
    parser = add_args(hz.base_parser(__doc__))
    a = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = mk.MescitParams(
        hall=a.hall, wall_h=a.wall_h, plinth=a.plinth,
        roof=a.roof, roof_pitch_deg=a.roof_pitch, eave=a.eave,
        portico=not a.no_portico, portico_depth=a.portico_depth,
        portico_bays=a.portico_bays,
        minaret=not a.no_minaret, minaret_h=a.minaret_h,
        minaret_side=a.minaret_side,
        wall_thickness=a.wall_thickness, palette=a.palette)

    lod0, lod1, lod2, ucx, info = mk.build_mescit(p, col, a.asset,
                                                  textured=a.textured)

    hz.log(f"{a.asset}: harim {info['hall']:.1f} m, orto={info['roof']}, "
           f"minare {info['minaret_h']:.1f} m")
    hz.log(f"ayak izi {info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"yukseklik {info['height']:.2f} m")
    hz.log(f"ucgen LOD0={info['tris_lod0']} LOD1={info['tris_lod1']} "
           f"LOD2={info['tris_lod2']}")

    if abs(info["pivot_min_z"]) > 1e-3:
        raise SystemExit(f"[HZ] HATA pivot taban merkezde degil: "
                         f"min_z={info['pivot_min_z']}")
    hz.log("pivot OK (taban merkez)")

    # Tarihsel kademe -> Unity `HistoricalTag`. Katalog TEK kaynaktir; prefab
    # her boru hatti kosusunda yeniden yazilir, elle konan etiket kaybolur.
    # Kaynak notu ORTUYE gore secilir. Ikisi ayni yapi ailesi degil:
    # mahalle mescidi ahsap catili ve mahallenin cekirdegidir; orta olcek
    # cami tek kubbeli ve revakli son cemaat yerlidir, semt merkezine
    # aittir. Tek bir not yazmak, katalogda kubbeli camiyi "mahalle
    # mescidi" diye etiketlerdi.
    if a.roof == "dome":
        source = ("Orta olcek cami tipolojisi: TEK KUBBE + revakli son cemaat "
                  "yeri + tek serefeli minare; semt merkezlerinde. Mahalle "
                  "mescidinden farki ortu ve olcektir. PLAN.md 7.1, ADR 0017. "
                  "Olculer rekonstruksiyon; belirli bir camiye karsilik gelmez.")
    else:
        source = ("Mahalle mescidi tipolojisi: tek mekan harim + son cemaat "
                  "yeri + tek serefeli minare, ahsap catili. Mahallenin "
                  "cekirdegi. PLAN.md 7.1, ADR 0016/0017. Olculer "
                  "rekonstruksiyon; belirli bir mescide karsilik gelmez.")
    info.update(name=a.asset, prefab=f"PF_{a.asset}", tier="T2", source=source)

    if a.info_json:
        os.makedirs(os.path.dirname(os.path.abspath(a.info_json)), exist_ok=True)
        with open(a.info_json, "w", encoding="utf-8") as fh:
            json.dump(info, fh, ensure_ascii=False, indent=1)

    if a.catalog:
        # Katalog BIRLESTIRILIR, uzerine yazilmaz: bu script her koşuşta tek
        # varyant uretir (mescit, kubbeli cami...). Uzerine yazsaydi katalogda
        # yalnizca son varyant kalir, oncekiler Unity'de sessizce Graybox'a
        # duserdi — nitekim ilk yazimda tam bu oldu.
        os.makedirs(os.path.dirname(os.path.abspath(a.catalog)), exist_ok=True)
        variants = []
        if os.path.exists(a.catalog):
            with open(a.catalog, encoding="utf-8") as fh:
                variants = json.load(fh).get("variants", [])
        variants = [v for v in variants if v.get("name") != a.asset]
        variants.append(info)
        variants.sort(key=lambda v: v.get("name", ""))
        with open(a.catalog, "w", encoding="utf-8") as fh:
            json.dump({"variants": variants}, fh, ensure_ascii=False, indent=1)
        hz.log(f"katalog: {a.catalog} ({len(variants)} kayit)")

    if a.out_blend:
        hz.save_blend(a.out_blend)
    if a.out_fbx:
        export_fbx(a.out_fbx, collection_name=COLLECTION)

    hz.log("gen_mescit OK")


if __name__ == "__main__":
    main()
