"""
Hezarfen: 1632 — Parametrik kutu ev (boru hattı doğrulama varlığı, plan Görev 7).

Amaç bir "güzel ev" değil, üretim bandının uçtan uca çalıştığını kanıtlayan gerçek
bir varlık: jeneratör → kanonik .blend → `export_fbx.py` → Unity import → ölçek
testi → prefab. Yine de siluet doğru tutuldu (taş subasman, cumbalı üst kat, derin
saçak, kırma çatı) — Görev 11'deki `ottoman_kit` bunun üstüne kurulacak.

Eksenler: genişlik +X, derinlik +Y, yükseklik +Z. Sokak cephesi -Y yönündedir;
cumba oraya taşar. Modelin orijini taban merkezindedir (0,0,0), böylece Unity'de
zemine oturtmak için ofset gerekmez.

Kullanım:
  blender --background --factory-startup --python tools/blender/gen_box_house.py -- \
      --out-blend art/blend/SM_BoxHouse.blend \
      --out-fbx  unity/HezarfenGame/Assets/_Import/SM_BoxHouse.fbx
"""

import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import bpy                         # noqa: E402
import hz_blender as hz            # noqa: E402
from export_fbx import export_fbx  # noqa: E402

COLLECTION = "Export"
ASSET = "BoxHouse"

# Palet (RESEARCH.md: kireç badana + asi kirmizisi ahsap + alaturka kiremit).
# Graybox seviyesinde doku yok; renk yalnizca kutlelerin okunmasi icin.
PALETTE = {
    "stone":   ((0.42, 0.40, 0.37), 0.90),
    "plaster": ((0.86, 0.84, 0.78), 0.85),
    "timber":  ((0.55, 0.24, 0.18), 0.80),
    "roof":    ((0.52, 0.27, 0.19), 0.75),
}


def add_args(p):
    p.add_argument("--floors", type=int, default=2, help="Kat sayisi (1-3)")
    p.add_argument("--width", type=float, default=7.0, help="Cephe genisligi (m, +X)")
    p.add_argument("--depth", type=float, default=6.5, help="Derinlik (m, +Y)")
    p.add_argument("--floor-height", type=float, default=2.7, help="Kat yuksekligi (m)")
    p.add_argument("--plinth", type=float, default=0.6, help="Tas subasman yuksekligi (m)")
    p.add_argument("--cumba", type=float, default=0.8, help="Cumba/cikma derinligi (m, -Y)")
    p.add_argument("--jetty-side", type=float, default=0.25, help="Yanlara cikma (m, +/-X)")
    p.add_argument("--eave", type=float, default=0.7, help="Sacak derinligi (m)")
    p.add_argument("--roof-height", type=float, default=2.2, help="Cati yuksekligi (m)")
    return p


def build(a):
    hz.reset_scene()
    col = hz.collection(COLLECTION)

    floors = max(1, min(3, a.floors))
    mats = {k: hz.make_material(name, PALETTE[k][0], roughness=PALETTE[k][1])
            for k, name in (("stone", "M_Stone_Rubble"),
                            ("plaster", "M_Plaster_Lime"),
                            ("timber", "M_Timber_AsiRed"),
                            ("roof", "M_Roof_Alaturka"))}

    parts = []

    # 1) Tas subasman — tam ayak izi, zeminden itibaren.
    z = 0.0
    plinth = hz.make_box(f"{ASSET}_Plinth", (a.width, a.depth, a.plinth),
                         (0.0, 0.0, a.plinth * 0.5), col)
    hz.assign(plinth, mats["stone"])
    parts.append(plinth)
    z += a.plinth

    # 2) Alt katlar — tam ayak izi, kireç badanalı kagir.
    for i in range(floors - 1):
        obj = hz.make_box(f"{ASSET}_Floor{i}", (a.width, a.depth, a.floor_height),
                          (0.0, 0.0, z + a.floor_height * 0.5), col)
        hz.assign(obj, mats["plaster"])
        parts.append(obj)
        z += a.floor_height

    # 3) Ust kat — cumbali. Sokak cephesine (-Y) tasar, yanlara az miktarda cikar.
    #    Cumba bu tipolojinin imzasidir: alt kat ayak izini buyutmeden ust katta
    #    yasam alani kazandirir ve sokagi daraltip golgeler.
    top_w = a.width + 2.0 * a.jetty_side
    top_d = a.depth + a.cumba
    top_cy = -a.cumba * 0.5                       # -Y'ye tasma, merkez kayar
    top = hz.make_box(f"{ASSET}_Top", (top_w, top_d, a.floor_height),
                      (0.0, top_cy, z + a.floor_height * 0.5), col)
    hz.assign(top, mats["timber"])
    parts.append(top)
    z += a.floor_height

    # 4) Kirma cati — sacak ust kat ayak izini her yonde asar.
    roof_w = top_w + 2.0 * a.eave
    roof_d = top_d + 2.0 * a.eave
    roof = hz.make_hip_roof(f"{ASSET}_Roof", roof_w, roof_d, a.roof_height,
                            center_xy=(0.0, top_cy), base_z=z,
                            ridge_axis="X" if roof_w >= roof_d else "Y", col=col)
    hz.assign(roof, mats["roof"])
    parts.append(roof)
    total_h = z + a.roof_height

    # 5) LOD0 = tum parcalar tek mesh. Unity, kardes nesneleri _LOD0/_LOD1
    #    sonekiyle taniyip LODGroup kurar; bu yuzden isimlendirme sozlesmeye bagli.
    #    hz.join malzeme indekslerini yeniden esler; parcalarin renkleri korunur.
    lod0 = hz.join(parts, f"SM_{ASSET}_LOD0", col)
    _purge(parts)

    # 6) LOD1 = tek kutle + cati. Uzaktan cumba ve subasman okunmaz;
    #    siluet ayni kaldigi surece pop-in gorunmez.
    mass = hz.make_box(f"{ASSET}_L1_Mass", (a.width, a.depth, total_h - a.roof_height),
                       (0.0, 0.0, (total_h - a.roof_height) * 0.5), col)
    hz.assign(mass, mats["plaster"])
    l1roof = hz.make_hip_roof(f"{ASSET}_L1_Roof", roof_w, roof_d, a.roof_height,
                              center_xy=(0.0, top_cy), base_z=total_h - a.roof_height,
                              ridge_axis="X" if roof_w >= roof_d else "Y", col=col)
    hz.assign(l1roof, mats["roof"])
    lod1 = hz.join([mass, l1roof], f"SM_{ASSET}_LOD1", col)
    _purge([mass, l1roof])

    # 7) Carpisma kutlesi — cati egimi olmadan, tek kutu. Ucus oyununda
    #    carpismanin ADIL olmasi icin collider siluetten DAR olmali: oyuncu
    #    "degmedim ama carpistim" hissini affetmez.
    ucx = hz.make_box(f"UCX_{ASSET}", (a.width, a.depth, total_h * 0.98),
                      (0.0, 0.0, total_h * 0.49), col)
    hz.assign(ucx, mats["stone"])

    mn, mx = hz.bounds(lod0)
    hz.log(f"{ASSET}: floors={floors} footprint={a.width:.2f}x{a.depth:.2f} m "
           f"height={total_h:.2f} m")
    hz.log(f"LOD0 bounds min={tuple(round(v, 3) for v in mn)} "
           f"max={tuple(round(v, 3) for v in mx)}")
    hz.log(f"LOD0 tris~{len(lod0.data.polygons)} faces, LOD1 {len(lod1.data.polygons)} faces")
    return col


def _purge(objects):
    """Ara parçaları sahneden ve veriden temizler — FBX'e sızmasınlar."""
    for obj in objects:
        me = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if me.users == 0:
            bpy.data.meshes.remove(me)


def main():
    parser = add_args(hz.base_parser(__doc__))
    args = parser.parse_args(hz.argv_after_dashes())

    build(args)

    if args.out_blend:
        hz.save_blend(args.out_blend)
    if args.out_fbx:
        export_fbx(args.out_fbx, collection_name=COLLECTION)

    hz.log("gen_box_house OK")


if __name__ == "__main__":
    main()
