"""
Hezarfen: 1632 — Eksen/ölçek kalibrasyon varlığı.

Bu bir sanat varlığı değil, bir ÖLÇÜ ALETİDİR. Boru hattının "1 m küp gerçekten
1 m mi, Blender'ın +Y'si Unity'de nereye düşüyor" sorusunu tahminle değil ölçümle
cevaplar. Unity tarafındaki `AssetPipelineTests` bu FBX'i okur.

Tasarım: üç işaretçi ÜÇ FARKLI uzaklıkta (2 / 3 / 4 m). Unity'de bir işaretçiyi
gördüğümde uzaklığı bana hangi Blender ekseninden geldiğini, işareti ise yönün
korunup korunmadığını söyler. Eşit uzaklıklar kullanılsaydı eksen takası ile
eksen çevrimi birbirinden ayırt edilemezdi.

  Blender +X, 2 m  ->  SM_AxisCal_BX2
  Blender +Y, 3 m  ->  SM_AxisCal_BY3
  Blender +Z, 4 m  ->  SM_AxisCal_BZ4

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_axis_calibration.py -- \
      --out-blend art/blend/SM_AxisCalibration.blend \
      --out-fbx  unity/HezarfenGame/Assets/_Project/Art/Models/Calibration/SM_AxisCalibration.fbx
"""

import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"

# (nesne adi, Blender yonu, uzaklik m, renk)
MARKERS = [
    ("SM_AxisCal_BX2", (1.0, 0.0, 0.0), 2.0, (0.90, 0.15, 0.15)),
    ("SM_AxisCal_BY3", (0.0, 1.0, 0.0), 3.0, (0.15, 0.80, 0.20)),
    ("SM_AxisCal_BZ4", (0.0, 0.0, 1.0), 4.0, (0.20, 0.35, 0.95)),
]

MARKER_SIZE = 0.2
CUBE_EDGE = 1.0


def build():
    hz.reset_scene()
    col = hz.collection(COLLECTION)

    # Referans kup: tam olarak 1 x 1 x 1 m, merkezi orijinde.
    cube = hz.make_box("SM_AxisCal_UnitCube", (CUBE_EDGE,) * 3, (0.0, 0.0, 0.0), col)
    hz.assign(cube, hz.make_material("M_Cal_Unit", (0.85, 0.85, 0.85), roughness=0.6))

    for name, direction, dist, color in MARKERS:
        center = tuple(a * dist for a in direction)
        obj = hz.make_box(name, (MARKER_SIZE,) * 3, center, col)
        hz.assign(obj, hz.make_material(f"M_Cal_{name[-3:]}", color, roughness=0.6))
        hz.log(f"marker {name} at blender {center}")

    mn, mx = hz.bounds(cube)
    hz.log(f"unit cube bounds: min={tuple(round(v, 4) for v in mn)} "
           f"max={tuple(round(v, 4) for v in mx)}")
    return col


def main():
    parser = hz.base_parser(__doc__)
    args = parser.parse_args(hz.argv_after_dashes())

    build()

    if args.out_blend:
        hz.save_blend(args.out_blend)
    if args.out_fbx:
        export_fbx(args.out_fbx, collection_name=COLLECTION)

    hz.log("gen_axis_calibration OK")


if __name__ == "__main__":
    main()
