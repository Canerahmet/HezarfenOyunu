"""
Hezarfen: 1632 — PBR malzeme ve dünya ölçekli UV (plan Faz 2, doku stratejisi).

İki iş yapar ve ikisi birbirine bağlıdır:

1. **UV: yüzeye hizalı, dünya ölçekli kutu izdüşümü.**
2. **Malzeme: Poly Haven CC0 haritalarından Principled BSDF.**

## Neden UV ölçeği gözle ayarlanmaz

Bir dokunun "ucuz" görünmesinin bir numaralı sebebi yanlış tekstür yoğunluğudur:
duvardaki taşlar 40 cm, çatıdaki kiremitler 8 cm olması gerekirken ikisi de aynı
UV katsayısıyla döşenirse göz bunu **hemen** yakalar, ama render'a bakan kişi
"bir tuhaf" der, sebebini söyleyemez.

Poly Haven her dokunun kaç metreyi kapladığını verir (`meta.json` →
`size_meters`). UV doğrudan **metreden** hesaplanır: `u = mesafe / doku_boyu`.
Böylece 2 m'lik bir taş dokusu duvarda gerçekten 2 m kaplar ve hiçbir yerde
katsayı elle ayarlanmaz.

## Neden dominant eksen değil, yüzeye hizalı izdüşüm

Basit kutu izdüşümü (dünya X/Y/Z'den birine bakmak) eğik yüzeylerde dokuyu
**kısaltır**: 30° eğimli bir çatıda tepeden bakan izdüşüm kiremitleri eğim
boyunca %13 sıkıştırır. Bunun yerine her yüzün KENDİ düzleminde ortonormal bir
taban kurulur; eğim ne olursa olsun texel yoğunluğu sabit kalır.

Bedeli, farklı yönlü yüzler arasında dikiş oluşmasıdır — mimari sert yüzeylerde
bu zaten normaldir ve kiremit/taş gibi düzensiz dokularda görünmez.
"""

import json
import math
import os

import bmesh
import bpy
from mathutils import Vector

import hz_blender as hz

TEXTURE_ROOT = os.path.join("art", "textures", "polyhaven")


def _abs(path):
    return path if os.path.isabs(path) else os.path.abspath(path)


def load_meta(asset_id, root=TEXTURE_ROOT):
    """Poly Haven `meta.json`unu okur. Yoksa None — çağıran graybox'a düşer."""
    p = _abs(os.path.join(root, asset_id, "meta.json"))
    if not os.path.exists(p):
        return None
    meta = json.load(open(p, encoding="utf-8"))
    meta["_dir"] = os.path.dirname(p)
    return meta


# ------------------------------------------------------------------- UV

def uv_project(obj, size_by_material, default_size=(2.0, 2.0)):
    """
    Yüzeye hizalı, dünya ölçekli UV üretir.

    `size_by_material`: malzeme indeksi -> (metre_u, metre_v). Her yüz KENDİ
    malzemesinin gerçek doku boyunu kullanır; yoksa `default_size`.

    Not: nesne dönüşümü uygulanmış varsayılır (bizim jeneratörlerde hep öyle;
    `hz.make_box` doğrudan dünya koordinatında kurar). Aksi hâlde ölçek
    nesne matrisiyle bozulurdu.
    """
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    metric = bm.faces.layers.int.get(hz.UV_METRIC)

    for f in bm.faces:
        su, sv = size_by_material.get(f.material_index, default_size)
        su = su if su > 1e-6 else 1.0
        sv = sv if sv > 1e-6 else 1.0

        # Eğri yüzey kendi UV'sini getirdi (metre cinsinden) — yeniden
        # yansıtmak onu bozardı; yalnızca dokunun ölçüsüne böl. Gerekçe:
        # `hz_blender.UV_METRIC`.
        if metric is not None and f[metric]:
            for loop in f.loops:
                u, v = loop[uv].uv
                loop[uv].uv = (u / su, v / sv)
            continue

        n = f.normal
        if n.length < 1e-9:
            continue
        n = n.normalized()

        # Yuzun kendi duzleminde ortonormal taban. Referans, normale paralel
        # olmayacak sekilde secilir; aksi halde cross carpim sifir olur ve UV coker.
        ref = Vector((0.0, 1.0, 0.0)) if abs(n.z) > 0.9 else Vector((0.0, 0.0, 1.0))
        t = ref.cross(n)
        if t.length < 1e-6:
            t = Vector((1.0, 0.0, 0.0))
        t.normalize()
        b = n.cross(t).normalized()

        for loop in f.loops:
            co = loop.vert.co
            loop[uv].uv = (co.dot(t) / su, co.dot(b) / sv)

    bm.to_mesh(me)
    bm.free()


# -------------------------------------------------------------- malzeme

def _image(path, non_color):
    """Görüntüyü yükler ve renk uzayını doğru kurar."""
    img = bpy.data.images.load(_abs(path), check_existing=True)
    # Normal/roughness/AO VERI'dir, renk degil. sRGB birakilirsa normaller
    # yanlis egim, roughness yanlis parlaklik uretir — render "plastik" gorunur
    # ve sebebi kolay kolay bulunmaz.
    img.colorspace_settings.name = "Non-Color" if non_color else "sRGB"
    return img


def _tex_node(nt, image, location):
    node = nt.nodes.new("ShaderNodeTexImage")
    node.image = image
    node.location = location
    node.interpolation = "Smart"
    return node


def _find_bsdf(nt):
    for n in nt.nodes:
        if n.type == "BSDF_PRINCIPLED":
            return n
    return nt.nodes.new("ShaderNodeBsdfPrincipled")


def make_pbr_material(name, meta, tint=None, tint_factor=0.0, roughness_boost=0.0,
                      value_gamma=1.0, tint_blend="COLOR", metallic=0.0):
    """
    Poly Haven haritalarından Principled BSDF malzemesi kurar.

    `tint` + `tint_factor`: aşı boyası gibi **boyalı** yüzeyler için. Karışım
    `COLOR` kipindedir: renk tonu boyadan, parlaklık deseni dokudan gelir —
    çarpma (MULTIPLY) kullanılsaydı ahşabın damarı kararıp kaybolurdu.

    `value_gamma`: taban rengin **değerini** düzeltir (<1 aydınlatır). Gamma
    tint'ten ÖNCE uygulanır; sonra uygulansaydı boyanın tonu da bozulurdu.

    `tint_blend` — hangi karışım, neden:

      "COLOR"  Ton boyadan, **parlaklık dokudan**. Ahşabın kendi rengini
               değiştirmek (ör. ceviri koyulaştırmak) için doğru kip.
      "MIX"    Albedo büyük ölçüde **boyanın kendisi**; doku yıpranma ve
               damar katkısı verir. **Boyalı yüzeyin fiziksel modeli budur.**

    Bu ayrım ölçümle çıktı: aşı boyalı ahşak `COLOR` ile 40/255 parlaklık
    verdi, hedef ~100'dü. Gamma ile 63'e çıktı ama hedefe zorlamak için gereken
    gamma (~0,19) damarı eziyordu. Sebep model hatasıydı: boya, altındaki
    tahtanın koyuluğunu **taşımaz**, örter.

    `metallic`: sayı ise sabit değer; **"arm"** ise ARM haritasının B kanalından
    piksel piksel okunur. İkinci yol kurşun için şart — oksitle örtülü yerler
    dielektrik, yıkanmış sırtlar metaldir ve fark yüzeyin üstünde değişir. Sabit
    bir sayı vermek inceleme render'ı ile oyun içi görüntüyü ayrıştırırdı; bu
    ayrışma bir kez yaşandı (çatı boyası, ADR 0019) ve render'a bakarak
    görülmüyordu.
    """
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = _find_bsdf(nt)
    maps = meta.get("maps", {})
    d = meta["_dir"]

    color_out = None

    if "BC" in maps:
        bc = _tex_node(nt, _image(os.path.join(d, maps["BC"]), False), (-900, 300))
        color_out = bc.outputs["Color"]

        if abs(value_gamma - 1.0) > 1e-6:
            g = nt.nodes.new("ShaderNodeGamma")
            g.location = (-760, 300)
            g.inputs["Gamma"].default_value = value_gamma
            nt.links.new(g.inputs["Color"], color_out)
            color_out = g.outputs["Color"]

        if tint is not None and tint_factor > 0.0:
            mix = nt.nodes.new("ShaderNodeMix")
            mix.data_type = "RGBA"
            mix.blend_type = tint_blend
            mix.location = (-620, 320)
            mix.inputs["Factor"].default_value = tint_factor
            nt.links.new(mix.inputs[6], color_out)          # A
            mix.inputs[7].default_value = (*tint, 1.0)      # B
            color_out = mix.outputs[2]

        # AO'yu taban renge carpmak fiziksel olarak tam dogru degildir ama
        # Blender onizlemesinde girintileri okunur kilar. Unity tarafinda AO
        # kendi kanalinda tasinir; bu yalnizca inceleme render'i icindir.
        if "AO" in maps:
            ao = _tex_node(nt, _image(os.path.join(d, maps["AO"]), True), (-900, 20))
            mul = nt.nodes.new("ShaderNodeMix")
            mul.data_type = "RGBA"
            mul.blend_type = "MULTIPLY"
            mul.location = (-400, 300)
            mul.inputs["Factor"].default_value = 0.85
            nt.links.new(mul.inputs[6], color_out)
            nt.links.new(mul.inputs[7], ao.outputs["Color"])
            color_out = mul.outputs[2]

        nt.links.new(bsdf.inputs["Base Color"], color_out)

    if "R" in maps:
        r = _tex_node(nt, _image(os.path.join(d, maps["R"]), True), (-900, -260))
        if roughness_boost:
            add = nt.nodes.new("ShaderNodeMath")
            add.operation = "ADD"
            add.location = (-600, -260)
            add.inputs[1].default_value = roughness_boost
            add.use_clamp = True
            nt.links.new(add.inputs[0], r.outputs["Color"])
            nt.links.new(bsdf.inputs["Roughness"], add.outputs["Value"])
        else:
            nt.links.new(bsdf.inputs["Roughness"], r.outputs["Color"])

    if "N" in maps:
        n = _tex_node(nt, _image(os.path.join(d, maps["N"]), True), (-900, -540))
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nm.location = (-560, -540)
        nt.links.new(nm.inputs["Color"], n.outputs["Color"])
        nt.links.new(bsdf.inputs["Normal"], nm.outputs["Normal"])

    if metallic == "arm":
        if "ARM" not in maps:
            raise ValueError(f"{name}: metallic='arm' istendi ama ARM haritasi yok")
        arm = _tex_node(nt, _image(os.path.join(d, maps["ARM"]), True), (-900, -820))
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        sep.location = (-560, -820)
        nt.links.new(sep.inputs["Color"], arm.outputs["Color"])
        nt.links.new(bsdf.inputs["Metallic"], sep.outputs["Blue"])
    elif metallic:
        bsdf.inputs["Metallic"].default_value = float(metallic)

    return mat


def material_size(meta, fallback=(2.0, 2.0)):
    """Dokunun gerçek dünya ölçüsü (metre) — UV bunu okur."""
    if not meta:
        return fallback
    sm = meta.get("size_meters")
    if not sm or len(sm) < 2:
        return fallback
    return (float(sm[0]), float(sm[1]))
