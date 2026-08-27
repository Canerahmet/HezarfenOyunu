"""
Hezarfen: 1632 — Osmanlı konut üreticisi (plan Faz 2).

`lib/ottoman_kit.py`teki parametrik evi tek varlık olarak üretir ve kanonik
.blend + FBX yazar.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_ottoman_house.py -- \
      --out-blend art/blend/SM_House_A.blend \
      --out-fbx  unity/HezarfenGame/Assets/_Import/SM_House_A.fbx \
      --floors 2 --cumba-type corbel --window-detail kafes

Varyant üretmek için `gen_house_variants.py` kullanılır; bu script tek ev içindir.
"""

import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
import ottoman_kit as kit          # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"


def add_args(p):
    p.add_argument("--asset", default="House_A", help="Varlik adi (SM_<ad>_LOD0)")
    p.add_argument("--floors", type=int, default=2, help="Kat sayisi (1-3)")
    p.add_argument("--width", type=float, default=7.0, help="Cephe genisligi (m)")
    p.add_argument("--depth", type=float, default=6.5, help="Derinlik (m)")
    p.add_argument("--floor-height", type=float, default=2.7, help="Kat yuksekligi (m)")
    p.add_argument("--plinth", type=float, default=0.6, help="Tas subasman (m)")

    p.add_argument("--cumba-type", default="flat", choices=kit.CUMBA_TYPES,
                   help="Cikma tipi")
    p.add_argument("--cumba", type=float, default=0.8, help="Cikma derinligi (m)")
    p.add_argument("--jetty-side", type=float, default=0.25, help="Yanlara cikma (m)")

    p.add_argument("--window-detail", default="recess", choices=kit.WINDOW_DETAILS,
                   help="Pencere kademesi")
    p.add_argument("--window-density", type=float, default=0.55,
                   help="Pencere yogunlugu (0-1); cephe genisligine gore sayi turetilir")
    p.add_argument("--kafes-bars", type=int, default=4, help="Kafes cita sayisi")

    p.add_argument("--detail", default="mass", choices=kit.DETAIL_LEVELS,
                   help="Yapim kademesi: mass=kalabalik doku, near=yaya seviyesi")
    p.add_argument("--facades", default="street", choices=kit.FACADE_MODES,
                   help="Hangi cephelerde aciklik olur")
    p.add_argument("--wall-thickness", type=float, default=0.30,
                   help="Kagir duvar kalinligi = sove derinligi (m)")
    p.add_argument("--rafter-spacing", type=float, default=0.75,
                   help="Sacak mertegi araligi (m); yalnizca --detail near")

    p.add_argument("--eave", type=float, default=0.7, help="Sacak derinligi (m)")
    p.add_argument("--roof-pitch", type=float, default=30.0, help="Cati egimi (derece)")
    p.add_argument("--palette", default="default", choices=tuple(kit.PALETTES),
                   help="Renk/tipoloji paleti")
    p.add_argument("--textured", action="store_true",
                   help="Poly Haven CC0 PBR dokularini kullan (yoksa graybox renk)")
    p.add_argument("--info-json", default=None,
                   help="Olculen degerleri bu dosyaya yaz (testler ve inceleme icin)")
    return p


def params_from_args(a):
    return kit.HouseParams(
        floors=a.floors, width=a.width, depth=a.depth,
        floor_height=a.floor_height, plinth=a.plinth,
        cumba_type=a.cumba_type, cumba=a.cumba, jetty_side=a.jetty_side,
        window_detail=a.window_detail, window_density=a.window_density,
        kafes_bars=a.kafes_bars,
        detail=a.detail, facades=a.facades,
        wall_thickness=a.wall_thickness, rafter_spacing=a.rafter_spacing,
        eave=a.eave, roof_pitch_deg=a.roof_pitch, palette=a.palette,
    ).apply_palette_rules()


def main():
    parser = add_args(hz.base_parser(__doc__))
    args = parser.parse_args(hz.argv_after_dashes())

    hz.reset_scene()
    col = hz.collection(COLLECTION)

    p = params_from_args(args)
    lod0, lod1, lod2, ucx, info = kit.build_house(p, col, args.asset,
                                                  textured=args.textured)

    hz.log(f"{args.asset}: {info['floors']} kat, ayak izi "
           f"{info['footprint_x']:.2f}x{info['footprint_y']:.2f} m, "
           f"yukseklik {info['height']:.2f} m (cati {info['roof_height']:.2f})")
    hz.log(f"cumba={info['cumba_type']} pencere={info['window_detail']} "
           f"palet={info['palette']}")
    hz.log(f"kademe={info['detail']} cepheler={info['facades']} "
           f"duvar={info['wall_thickness']:.2f} m")
    hz.log(f"ucgen LOD0={info['tris_lod0']} LOD1={info['tris_lod1']} "
           f"LOD2={info['tris_lod2']}")

    # Pivot denetimi: taban merkezde olmali. Kaymissa Unity'de her ev zemine
    # gomulur ya da havada durur ve bu ancak sahnede fark edilir.
    if abs(info["pivot_min_z"]) > 1e-3:
        raise SystemExit(f"[HZ] HATA pivot taban merkezde degil: "
                         f"min_z={info['pivot_min_z']} (0 olmali)")
    hz.log("pivot OK (taban merkez)")

    if args.info_json:
        os.makedirs(os.path.dirname(os.path.abspath(args.info_json)), exist_ok=True)
        with open(args.info_json, "w", encoding="utf-8") as fh:
            json.dump(info, fh, ensure_ascii=False, indent=1)
        hz.log(f"wrote {args.info_json}")

    if args.out_blend:
        hz.save_blend(args.out_blend)
    if args.out_fbx:
        export_fbx(args.out_fbx, collection_name=COLLECTION)

    hz.log("gen_ottoman_house OK")


if __name__ == "__main__":
    main()
