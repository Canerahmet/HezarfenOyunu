"""
Hezarfen: 1632 — TEK yetkili FBX ihraç yolu (CLAUDE.md: "Elle export yasak").

Neden tek yol: FBX'in eksen ve birim ayarları elle verildiğinde her seferinde
biraz farklı olur; sonuç, Unity'de yan yatmış ya da 100 kat büyük modellerdir.
Bu dosya o ayarları sabitler ve tek yerde tutar. Ayar değişecekse burada değişir,
gerekçesi ADR'e yazılır ve `AssetPipelineTests` yeniden koşar.

İki kullanım:

  1) CLI — mevcut bir .blend'i ihraç et:
     blender --background --python tools/blender/export_fbx.py -- \
         --in art/blend/SM_BoxHouse.blend --out unity/.../SM_BoxHouse.fbx

  2) Modül — jeneratör scriptleri sahneyi kurduktan sonra doğrudan çağırır:
     from export_fbx import export_fbx
     export_fbx("cikti.fbx", collection_name="Export")
"""

import argparse
import os
import sys

import bpy

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from hz_blender import argv_after_dashes, ensure_units, log   # noqa: E402


# Unity'ye giden statik varlıklar icin kanonik ayarlar.
# axis_forward/-Z + axis_up/Y: Blender Z-up -> Unity Y-up donusumu.
# apply_unit_scale + global_scale 1.0: 1 Blender metresi = 1 Unity birimi.
# bake_space_transform: eksen donusumunu MESH VERISINE isler; boylece Unity'de
#   kok nesnenin rotasyonu (-89.98, 0, 0) olarak gelmez, temiz (0,0,0) olur.
#   Iskeletli/animasyonlu varliklarda bu secenek bozulmaya yol acar -> orada kapali.
_STATIC = dict(
    axis_forward="-Z",
    axis_up="Y",
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_NONE",
    bake_space_transform=True,
    object_types={"MESH", "EMPTY"},
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    use_triangles=False,
    use_tspace=True,
    use_custom_props=False,
    colors_type="SRGB",
    path_mode="STRIP",
    bake_anim=False,
)

_SKINNED = dict(
    _STATIC,
    bake_space_transform=False,          # deforme olan mesh'te uzay bakma YASAK
    object_types={"MESH", "EMPTY", "ARMATURE"},
    add_leaf_bones=False,
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    use_armature_deform_only=True,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,       # deterministik: otomatik sadelestirme yok
)


def _select(collection_name=None):
    """
    İhraç kapsamını seçer. Koleksiyon verilmişse yalnızca onun içindekiler,
    verilmemişse sahnedeki tüm mesh/empty nesneler.
    """
    for obj in bpy.context.scene.objects:
        obj.select_set(False)

    if collection_name:
        col = bpy.data.collections.get(collection_name)
        if col is None:
            raise SystemExit(f"[HZ] HATA: '{collection_name}' koleksiyonu yok.")
        targets = [o for o in col.all_objects]
    else:
        targets = [o for o in bpy.context.scene.objects
                   if o.type in {"MESH", "EMPTY", "ARMATURE"}]

    if not targets:
        raise SystemExit("[HZ] HATA: ihrac edilecek nesne yok.")

    for obj in targets:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = targets[0]
    return targets


def export_fbx(filepath, collection_name=None, skinned=False):
    """Kanonik ihraç. Dönüş: yazılan mutlak yol."""
    ensure_units()
    targets = _select(collection_name)

    filepath = os.path.abspath(filepath)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)

    opts = dict(_SKINNED if skinned else _STATIC)
    log(f"export ({'skinned' if skinned else 'static'}): {len(targets)} object(s) -> {filepath}")
    for obj in targets:
        log(f"  - {obj.name} [{obj.type}]")

    bpy.ops.export_scene.fbx(filepath=filepath, use_selection=True, **opts)

    if not os.path.exists(filepath):
        raise SystemExit(f"[HZ] HATA: FBX yazilamadi: {filepath}")
    log(f"wrote {os.path.getsize(filepath)} bytes")
    return filepath


def main():
    p = argparse.ArgumentParser(description="Hezarfen kanonik FBX ihracati")
    p.add_argument("--in", dest="src", required=True, help="Kaynak .blend")
    p.add_argument("--out", dest="dst", required=True, help="Hedef .fbx")
    p.add_argument("--collection", default=None, help="Yalnizca bu koleksiyonu ihrac et")
    p.add_argument("--skinned", action="store_true", help="Iskeletli/animasyonlu varlik")
    args = p.parse_args(argv_after_dashes())

    src = os.path.abspath(args.src)
    if not os.path.exists(src):
        raise SystemExit(f"[HZ] HATA: kaynak yok: {src}")

    bpy.ops.wm.open_mainfile(filepath=src)
    log(f"opened: {src}")
    export_fbx(args.dst, args.collection, args.skinned)


if __name__ == "__main__":
    main()
