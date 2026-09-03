"""
Hezarfen: 1632 — Saç ve sakal kiti (Faz 5).

## Kart, kabuk değil

Kıyafet gövdeden **kabuk** çıkararak yapıldı, çünkü kumaş bedene oturur.
Saç oturmaz — saç **sarkar**, ve sarkan şeyin siluetinde boşluk vardır.
Katı bir kabuk saçı kask yapar.

Bu yüzden saç **kartlarla** kurulur: alfa dokulu ince şeritler (doku
`art/textures/generated/hair_card`, prosedürel, kendi telifimiz).

## Ne kadar saç

**Az.** Hezarfen sarıklıdır ve sarık saçın çoğunu örter. Görünen şey
şakak, ense ve sakaldır. Kafanın tamamını kartlarla kaplamak, hiç
görünmeyecek 40 kartın maliyetini ödemek olurdu — ve LOD'da o kartlar en
önce kaybolur, yani ödediğin şeyi hiç almazsın.

## Sakal: gözlenmiş bir seçim

Rålamb plaka 20 (sivil) ve 35 (saray hizmetlisi) **sakallı**; plaka 50
(baltalı muhafız) yalnızca bıyıklı. Sakal olgunluk ve mevki işaretidir,
bıyık genç/asker. Hezarfen ne asker ne genç: **sakallı**.

Bu bir üslup tercihi değil, plakaların söylediği şey — kıyafette
uyguladığımız kuralın aynısı.
"""

import math
import os

import bmesh
import bpy
from mathutils import Vector

import hz_blender as hz

#: Doku klasörü (prosedürel; `tools/textures/gen_hair_texture.py`).
DOKU_DIR = os.path.join("art", "textures", "generated", "hair_card")

#: Atlastaki şerit sayısı — kartlar U ekseninde bu şeritlerden birini alır.
SERIT = 4

#: Kart kalınlığı (m). Sıfır olamaz: sıfır kalınlıkta kart yandan
#: bakınca yok olur ve saç bir anda kaybolur.
KART_KAL = 0.0015


def sakal_material(ad="M_Beard", renk=(0.105, 0.072, 0.052)):
    """Sakal **kütlesi** için opak malzeme — kart malzemesi değil.

    Sakalı kart yığınından kabuğa çevirince (bkz. `gen_hezarfen.giydir`)
    kart malzemesini olduğu gibi kullandım ve sakal render'da tamamen
    kayboldu. Sebep basit ama görülmesi zor: kart malzemesinin alfası bir
    **tel deseni**dir; kartın büyük kısmı zaten şeffaf olsun diyedir. O
    maskeyi katı bir kabuğa uygulayınca kabuğun kendisi delik deşik olur.
    Kütlenin maskeye ihtiyacı yoktur; siluetini geometri veriyor.

    Renk saç kartlarıyla aynı aileden ama biraz açık: tam siyah bir
    sakal ışık almadığı için yüzde delik gibi okunuyordu.
    """
    mat = bpy.data.materials.get(ad) or bpy.data.materials.new(ad)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*renk, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.72
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.25
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if hasattr(mat, "blend_method"):
        mat.blend_method = "OPAQUE"
    return mat


def hair_material(ad="M_Hair", doku_dir=None, renk=(0.12, 0.08, 0.055)):
    """Alfa kesmeli saç malzemesi.

    Ortak malzeme sistemine (`ottoman_kit.build_materials`) eklenmedi ve
    bu bilerek: o sistem **opak** yüzeyler için kurulu ve alfa desteği
    eklemek 30'dan fazla var olan malzemenin harman kipini değiştirme
    riski taşıyor. Saç tek başına duruyor; tek başına duran şeyi ortak
    yola sokmak, ortak yolu saç için eğmek olurdu.

    Doku yoksa **düz renge düşer ve söyler** — yarısı dokulu bir saç,
    sebebi anlaşılmadan "bozuk" görünür.
    """
    d = os.path.abspath(doku_dir or DOKU_DIR)
    mat = bpy.data.materials.get(ad) or bpy.data.materials.new(ad)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*renk, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.46
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

    bc_yol = os.path.join(d, "T_hair_card_BC.png")
    a_yol = os.path.join(d, "T_hair_card_A.png")
    if not (os.path.exists(bc_yol) and os.path.exists(a_yol)):
        hz.log("UYARI saç dokusu yok — düz renge düşüldü. Önce: "
               "python tools/textures/gen_hair_texture.py")
        if hasattr(mat, "blend_method"):
            mat.blend_method = "OPAQUE"
        return mat

    uv = nt.nodes.new("ShaderNodeTexCoord")
    bc = nt.nodes.new("ShaderNodeTexImage")
    bc.image = bpy.data.images.load(bc_yol, check_existing=True)
    nt.links.new(uv.outputs["UV"], bc.inputs["Vector"])
    nt.links.new(bc.outputs["Color"], bsdf.inputs["Base Color"])

    al = nt.nodes.new("ShaderNodeTexImage")
    al.image = bpy.data.images.load(a_yol, check_existing=True)
    # Alfa RENK DEGIL: Non-Color okunmali, yoksa sRGB egrisi maskeyi
    # yumusatir ve tel kenarlari sisman gorunur.
    al.image.colorspace_settings.name = "Non-Color"
    nt.links.new(uv.outputs["UV"], al.inputs["Vector"])
    # TANI kancasi: `HZ_SAC_OPAK=1` ile alfa baglanmaz. Sac render'da
    # gorunmuyorsa sebep alfa mi yerlesim mi — tahminle degil bu
    # ANAHTARLA ayrilir. Kalici, cunku ayni soru her sac turunda cikar.
    if not os.environ.get("HZ_SAC_OPAK"):
        nt.links.new(al.outputs["Color"], bsdf.inputs["Alpha"])

    n_yol = os.path.join(d, "T_hair_card_N.png")
    if os.path.exists(n_yol):
        nrm = nt.nodes.new("ShaderNodeTexImage")
        nrm.image = bpy.data.images.load(n_yol, check_existing=True)
        nrm.image.colorspace_settings.name = "Non-Color"
        nmap = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(uv.outputs["UV"], nrm.inputs["Vector"])
        nt.links.new(nrm.outputs["Color"], nmap.inputs["Color"])
        nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])

    # Harman: kesme — sac icin karistirma (BLEND) siralama sorunlari
    # cikarir ve kartlar birbirini yer.
    #
    # Alan adlari Blender surumleri arasinda degisti (`shadow_method`
    # 4.2'de kalkti). Ucluyle yazmistim: `x = A if hasattr(...) else None`
    # — bekci dogru seyi denetliyor ama ATAMA yine de yapiliyor ve
    # olmayan alana None yazmak da hata. Denetim `if` olmali.
    # Blender 4.2'de EEVEE Next geldi ve alan adlari degisti:
    # `blend_method` -> `surface_render_method`. Ikisini de deniyoruz;
    # hangisi varsa o yazilir. Hicbiri yoksa Cycles zaten Principled'in
    # Alpha girisini okur ve inceleme render'i dogru cikar.
    for alan, deger in (("surface_render_method", "DITHERED"),
                        ("blend_method", "CLIP"),
                        ("alpha_threshold", 0.45),
                        ("shadow_method", "CLIP")):
        if hasattr(mat, alan):
            try:
                setattr(mat, alan, deger)
            except (TypeError, ValueError):
                pass
    return mat


def kart(ad, kok, yon, yukari, boy, en, col, serit=0, egim=0.35, bolum=3):
    """Tek saç kartı: kökten uca eğilerek sarkan bir şerit.

    `kok` kartın başladığı nokta, `yon` sarkma yönü, `yukari` kartın
    yüzey normali. Kart düz bir dikdörtgen değil **eğilerek** iner:
    saç yerçekimine uyar ve düz bir kart kağıt gibi durur.
    """
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    yon = Vector(yon).normalized()
    yukari = Vector(yukari).normalized()
    yan = yon.cross(yukari).normalized()
    if yan.length < 1e-6:
        yan = Vector((1.0, 0.0, 0.0))

    u0 = serit / float(SERIT)
    u1 = (serit + 1) / float(SERIT)

    halkalar = []
    for i in range(bolum + 1):
        t = i / float(bolum)
        # Eğim: uca doğru aşağı bükülür (yerçekimi).
        p = (Vector(kok) + yon * (boy * t)
             + Vector((0.0, 0.0, -1.0)) * (boy * egim * t * t))
        # Uca doğru daralır: tutamın ucu kökünden dardır.
        w = en * (1.0 - 0.35 * t) * 0.5
        halkalar.append((bm.verts.new(p - yan * w),
                         bm.verts.new(p + yan * w)))

    for i in range(bolum):
        a0, a1 = halkalar[i]
        b0, b1 = halkalar[i + 1]
        f = bm.faces.new((a0, a1, b1, b0))
        v0 = i / float(bolum)
        v1 = (i + 1) / float(bolum)
        for loop, (uu, vv) in zip(f.loops, ((u0, v0), (u1, v0),
                                            (u1, v1), (u0, v1))):
            loop[uv].uv = (uu, vv)

    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kal", "SOLIDIFY")
    m.thickness = KART_KAL
    m.offset = 0.0
    dg = bpy.context.evaluated_depsgraph_get()
    yeni = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    eski = obj.data
    obj.modifiers.clear()
    obj.data = yeni
    obj.data.name = ad
    bpy.data.meshes.remove(eski)
    return obj


def cene_hatti(govde, boy, adim=9):
    """Çene hattı: çeneden kulaklara giden yay. `[(nokta, disari)]`.

    Sakalı sabit bir yaya oturtmak yerine **kafanın kendi kesitinden**
    okuyoruz — kafa değişirse (MakeHuman, ADR 0068) sakal kendini
    yeniden kurar.
    """
    mw = govde.matrix_world
    vs = [mw @ v.co for v in govde.data.vertices]
    # ARALIK tara, tek dilim degil.
    #
    # Tek dilimle (`z = boy*0.878`) aldigim "cene" noktalari kafanin
    # gercek on yuzunden 1,5 cm GERIDEYDI ve 51 sakal karti kafanin
    # ICINDE kaldi: render'da hicbiri gorunmedi. Cene bir kot degil bir
    # BOLGEDIR; en one cikan nokta o bolgenin icinde bir yerdedir ve
    # nerede oldugunu varsaymak yerine aramak gerekir.
    z0, z1 = boy * 0.862, boy * 0.892
    dilim = [v for v in vs if z0 <= v.z <= z1]
    if len(dilim) < 8:
        return []

    cy = sum(v.y for v in dilim) / len(dilim)
    nokta = []
    for i in range(adim):
        # Yay: ön orta (-y) → yan (±x). Simetrik olduğu için sağ yarım
        # üretilir, çağıran aynalar.
        a = math.pi * 0.5 * (i / float(adim - 1))
        yon = Vector((math.sin(a), -math.cos(a), 0.0))
        # O yöndeki en uzak nokta = kafanın kenarı.
        en_uzak, en_d = None, -1.0
        for v in dilim:
            d = (v.x) * yon.x + (v.y - cy) * yon.y
            if d > en_d:
                en_d, en_uzak = d, v
        if en_uzak is not None:
            nokta.append((en_uzak.copy(), yon))
    return nokta


#: MPFB2'nin çekirdek doku dizini — `tools/textures/gen_deri_texture.py`
#: ile aynı arama listesi. Ten dokusu zaten bu maskelerden besteleniyor
#: (`refs/LICENSES.md`, MPFB 2.0.17 satırı: çekirdek varlıklar CC0).
MPFB_DOKU = [
    os.path.expandvars(
        r"%APPDATA%\Blender Foundation\Blender\5.2\extensions"
        r"\user_default\mpfb\data\textures"),
    os.path.expanduser(
        "~/.config/blender/5.2/extensions/user_default/mpfb/data/textures"),
]


def bolge_kotu(govde, maske, esik=0.5):
    """MPFB2 bölge maskesinin **ölçülen** kot aralığı: `(z_alt, z_ust)`.

    ## Neden ölçülüyor

    Sakalın üst sınırı `boy * 0.897` diye yazılıydı ve inceleme karesi
    bunun ne demek olduğunu gösterdi: yaşlının ak sakalı **ağzın
    üstünden** geçiyor, yüzün alt yarısını kulaktan kulağa kaplayan bir
    **bant** gibi okunuyordu. Sakal değil, sargı.

    0,897 bir ölçü değil bir tahmindi — ve bu depoda tekrar eden dersin
    bir örneği daha: *bir sabit, bir ölçümün yerinde duruyordu.*

    Ağız nerede olduğunu MPFB2'nin kendi bölge maskesi biliyor:
    `mpfb_lips.jpg`, gövdenin UV atlasında dudak adalarını işaretler ve
    ten dokusu zaten ondan besteleniyor. Yani kaynak yeni değil, yalnız
    ikinci kez okunuyor.

    ## Neden köşe başına UV

    UV döngü (loop) başınadır, köşe başına değil. Bir köşenin dudakta
    olup olmadığını sormak için köşeye ait **herhangi** bir döngünün
    maskeyi geçmesi yeterli: dikişin iki yakasından biri dudakta
    olabilir.
    """
    dizin = next((y for y in MPFB_DOKU if os.path.isdir(y)), None)
    if dizin is None:
        return None
    yol = os.path.join(dizin, maske)
    if not os.path.isfile(yol):
        return None
    if not govde.data.uv_layers:
        return None

    im = None
    try:
        im = bpy.data.images.load(yol, check_existing=True)
        en, boy_px = im.size
        if en == 0 or boy_px == 0:
            return None
        piksel = list(im.pixels)
        kanal = im.channels

        uv = govde.data.uv_layers[0].data
        mw = govde.matrix_world
        zs = []
        dudakta = set()
        for dongu in govde.data.loops:
            u, v = uv[dongu.index].uv
            x = min(en - 1, max(0, int(u % 1.0 * en)))
            y = min(boy_px - 1, max(0, int(v % 1.0 * boy_px)))
            if piksel[(y * en + x) * kanal] > esik:
                dudakta.add(dongu.vertex_index)
        for i in dudakta:
            zs.append((mw @ govde.data.vertices[i].co).z)
    finally:
        if im is not None:
            bpy.data.images.remove(im)

    if len(zs) < 8:
        return None
    return (min(zs), max(zs))


def dudak_kotu(govde, esik=0.5):
    """Dudakların kot aralığı — sakalın üst sınırı."""
    return bolge_kotu(govde, "mpfb_lips.jpg", esik)


def cene_kotu(govde, esik=0.5):
    """Çenenin (yüz maskesinin en alt noktası) kotu, ya da None.

    Sakalın **alt** sınırı buradan türer. Eskiden `boy * 0.806`
    yazılıydı ve ölçülünce ne olduğu görüldü: çene 0,869'da, yani sabit
    sayı çenenin **10 cm altındaydı**. Sakal bir kabuk olduğu için o
    10 cm boyunca boynu takip ediyordu — inceleme karesinde yaşlının
    ak sakalı bir sakal değil bir **boyunluk** gibi duruyordu.

    Sakal çeneden aşağı sarkar ama boyun silindirini sarmaz; kabuk
    yöntemi sarkmayı zaten veremez, o yüzden alt sınır çenenin biraz
    altında durur.
    """
    yuz = bolge_kotu(govde, "mpfb_face.jpg", esik)
    return None if yuz is None else yuz[0]

