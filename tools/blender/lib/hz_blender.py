"""
Hezarfen: 1632 — Blender jeneratörleri için ortak katman.

Neden var: her jeneratör scriptinin sahne sıfırlama, birim ayarı, kutu üretme ve
malzeme kurma kodunu tekrar yazması, "her scriptte biraz farklı" bir boru hattı
doğurur. Ölçek/eksen hataları tam olarak böyle sızar. Bu modül o kararları TEK
yerde tutar; jeneratörler yalnızca biçim üretir.

Sözleşme (CLAUDE.md): 1 birim = 1 metre. Blender metrik, scale_length = 1.0.
"""

import argparse
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector


# ---------------------------------------------------------------- argümanlar

def argv_after_dashes():
    """`blender ... --python x.py -- --in a --out b` çağrısındaki kullanıcı argümanları."""
    if "--" in sys.argv:
        return sys.argv[sys.argv.index("--") + 1:]
    return []


def base_parser(description):
    """Tüm jeneratörlerin paylaştığı argümanlar."""
    p = argparse.ArgumentParser(description=description)
    p.add_argument("--out-blend", default=None, help="Kanonik .blend kayit yolu")
    p.add_argument("--out-fbx", default=None, help="FBX cikti yolu (export_fbx uzerinden)")
    return p


def log(msg):
    """Headless çıktıda kolay ayıklanan önekli günlük."""
    print(f"[HZ] {msg}", flush=True)


# ------------------------------------------------------------------- sahne

def reset_scene():
    """
    Boş, deterministik bir sahne. Fabrika ayarlarından başlarız ki makinedeki
    kullanıcı tercihleri (birim sistemi, renk yönetimi, açılış sahnesi) çıktıya
    sızmasın — headless üretimin tekrarlanabilir olmasının şartı budur.
    """
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ensure_units()
    log("scene reset (factory settings, empty)")


def ensure_units():
    """1 Blender birimi = 1 metre. Sözleşmenin Blender tarafındaki karşılığı."""
    u = bpy.context.scene.unit_settings
    u.system = "METRIC"
    u.scale_length = 1.0
    u.length_unit = "METERS"


def collection(name):
    """Adlandırılmış koleksiyon (yoksa oluşturur) — export kapsamı bununla seçilir."""
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    return col


def link(obj, col=None):
    target = col if col is not None else bpy.context.scene.collection
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    target.objects.link(obj)
    return obj


# ----------------------------------------------------------------- geometri

def mesh_from_bmesh(name, bm, col=None):
    """bmesh → nesne. Ops yerine bmesh: headless'ta bağlam (context) sorunu yok."""
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.validate()
    obj = bpy.data.objects.new(name, me)
    return link(obj, col)


def make_box(name, size, center=(0.0, 0.0, 0.0), col=None):
    """
    Eksen hizalı kutu. `size` tam boyuttur (metre), `center` merkez noktasıdır.

    Köşe koordinatları doğrudan mesh verisine yazılır, nesne dönüşümü kimliktir
    (identity). Böylece "uygulanmamış ölçek" diye bir durum hiç doğmaz — FBX'e
    sızan en yaygın ölçek hatası budur.
    """
    sx, sy, sz = (float(v) for v in size)
    cx, cy, cz = (float(v) for v in center)

    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector((sx, sy, sz)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector((cx, cy, cz)), verts=bm.verts)
    return mesh_from_bmesh(name, bm, col)


def make_hip_roof(name, width, depth, height, center_xy=(0.0, 0.0), base_z=0.0,
                  ridge_axis="X", col=None):
    """
    Kırma (hipped) çatı: dört yöne eğimli, tepede bir mahya çizgisi.

    Osmanlı sivil mimarisinde alaturka kiremitli kırma çatı tipiktir; beşik çatı
    (gable) daha çok Balkan/Anadolu kırsalında görülür. Kutu ev testi için bile
    doğru siluet üretmek, Görev 11'deki `ottoman_kit`e sağlam bir tohum bırakır.
    """
    hw, hd = width * 0.5, depth * 0.5
    cx, cy = center_xy
    z0, z1 = base_z, base_z + height

    # Mahya uzunlugu: kisa kenarin yarisi kadar iceri cekilir -> 45 derece kalkan
    if ridge_axis.upper() == "X":
        inset = min(hd, hw * 0.9)
        r0 = Vector((cx - hw + inset, cy, z1))
        r1 = Vector((cx + hw - inset, cy, z1))
    else:
        inset = min(hw, hd * 0.9)
        r0 = Vector((cx, cy - hd + inset, z1))
        r1 = Vector((cx, cy + hd - inset, z1))

    bm = bmesh.new()
    c = [bm.verts.new((cx - hw, cy - hd, z0)),
         bm.verts.new((cx + hw, cy - hd, z0)),
         bm.verts.new((cx + hw, cy + hd, z0)),
         bm.verts.new((cx - hw, cy + hd, z0))]
    v0 = bm.verts.new(r0)
    v1 = bm.verts.new(r1)
    bm.verts.ensure_lookup_table()

    if ridge_axis.upper() == "X":
        bm.faces.new((c[0], c[1], v1, v0))   # on egim
        bm.faces.new((c[2], c[3], v0, v1))   # arka egim
        bm.faces.new((c[1], c[2], v1))       # sag kalkan
        bm.faces.new((c[3], c[0], v0))       # sol kalkan
    else:
        bm.faces.new((c[1], c[2], v1, v0))
        bm.faces.new((c[3], c[0], v0, v1))
        bm.faces.new((c[2], c[3], v1))
        bm.faces.new((c[0], c[1], v0))

    bm.faces.new((c[3], c[2], c[1], c[0]))   # taban (ic mekandan gorunmez)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return mesh_from_bmesh(name, bm, col)


def _shade_smooth(bm, faces):
    """
    Eğri yüzeyleri yumuşak gölgelendirir.

    Neden ayrı bir adım: kubbe ve minare gövdesi çokgen yaklaşımdır; düz
    gölgelendirmede her segment ayrı ayrı okunur ve kubbe **fazetli** çıkar —
    render'da anında yanlış görünür. Kapaklar ve bilinçli sekizgen kasnak
    düz kalır, yoksa köşeleri erir.
    """
    for f in faces:
        f.smooth = True


# Yüzey kendi UV'sini taşıyor mu?
#
# Kitteki UV'lerin neredeyse hepsi `materials.uv_project` ile, yüze hizalı
# dünya ölçekli izdüşümle üretilir; düz mimarî yüzeylerde doğru olan budur.
# EĞRİ yüzeyde ise yanlıştır ve yanlışlığı ölçülebilir: kubbenin her yüzü ayrı
# bir teğet düzlem olduğu için doku her yüz sınırında kopar. Taş ya da sıva
# gibi düzensiz dokularda görünmez, ama kurşun örtünün DİKİŞ IZGARASI varken
# kubbe "kırılmış fayans" gibi çıkar — bir kez tam bu oldu (ADR 0021).
#
# Çözüm: eğri yüzeyler UV'lerini KENDİLERİ kurar (kubbede meridyen dilimleri,
# konide eğim boyu) ve bu bayrakla işaretlenir. `uv_project` işaretli yüzü
# yeniden yansıtmaz, yalnızca dokunun metre ölçüsüne BÖLER. Yani buradaki UV
# birimi METREdir; dokuya çevirme tek yerde kalır.
UV_METRIC = "hz_uv_metric"


def metric_layers(bm):
    """
    UV ve işaret katmanlarını **yüzler kurulmadan önce** açar.

    Şart: bmesh'e sonradan özel veri katmanı eklemek mevcut eleman
    referanslarını GEÇERSİZ kılar (`ReferenceError: BMFace has been removed`).
    Katmanı en başta açmak, hatayı doğduğu yerde bitirir.
    """
    bm.loops.layers.uv.verify()
    return (bm.faces.layers.int.get(UV_METRIC)
            or bm.faces.layers.int.new(UV_METRIC))


def _mark_metric(bm, face_uvs):
    """`[(face, [(u, v), ...]), ...]` — metre biriminde UV yazar ve işaretler."""
    uv = bm.loops.layers.uv.verify()
    lock = bm.faces.layers.int.get(UV_METRIC)
    if lock is None:
        raise RuntimeError("metric_layers(bm) yuzlerden ONCE cagrilmali")
    for face, coords in face_uvs:
        face[lock] = 1
        for loop, c in zip(face.loops, coords):
            loop[uv].uv = c



# ------------------------------------------------------- ayrıntı kademesi

#: Eğri yüzeylerin bölüt sayısı bu çarpanla ölçeklenir. 1.0 tam ayrıntı.
#: `set_detail` dışında ELLE değiştirme.
DETAIL = 1.0

#: Bu uzunluğun altındaki öğeler orta kademede hiç üretilmez (m). 0 = hepsi.
DETAIL_MIN = 0.0


def set_detail(scale, min_size=0.0):
    """
    **Ayrıntı kademesini** ayarlar — orta LOD'u aynı üreteçten kurmak için.

    ## Neden gerekti

    Ayrıntı geçişi LOD0'ı altı katına çıkardı ama LOD1'e dokunmadı ve arada
    kalan boşluk **ölçüldü**: Süleymaniye'nin LOD0'ı yalnızca **573 m**'ye
    kadar görüntüleniyor (LODGroup eşiği 0,25 ekran yüksekliği, FOV 40°);
    ötesinde 456 üçgenlik blok geliyor. Hezarfen'in uçuşu **3336 m**. Yani
    ayrıntının tamamı, oyunun merkez sahnesinde hiç görünmüyordu.

    Boşluk sonradan **filtreleyerek** kapatılamaz: ölçüldü, 4 m altındaki her
    parça atılsa bile üçgenlerin %33'ü kalıyor — çünkü yük küçük süslerde
    değil, **çok bölütlü kubbelerde ve kütlelerdedir**. Orta kademe bu yüzden
    aynı üreteçten, **daha az bölütle yeniden kurulur**.

    `scale` eğri yüzeylerin bölütlerini, `min_size` ise ayrıntı dağarcığının
    alt sınırını belirler: o boyun altındaki öğe (mukarnas hücresi, kubbe
    kaburgası, pencere kaydı) orta kademede zaten piksel altıdır.
    """
    global DETAIL, DETAIL_MIN
    DETAIL = float(scale)
    DETAIL_MIN = float(min_size)


def seg(n, alt=6):
    """
    Bölüt sayısını kademeye göre ölçekler; `alt` altına düşmez ama
    **istenen sayının üstüne de çıkmaz**.

    İlk yazımda `max(alt, n*DETAIL)` idi ve tam ayrıntıda hiçbir şeyi
    değiştirmemesi gerekirken zaten düşük bölütlü ilkelleri YÜKSELTİYORDU
    (6 bölütlü bir boru `alt=8` yüzünden 8'e çıkıyordu). Süleymaniye
    89 668'den 89 812'ye çıkınca yakalandı — kademe altyapısının tam
    ayrıntıda **görünmez** olması gerekir, ve bunu doğrulayan şey sayıydı.
    """
    return max(min(n, alt), int(round(n * DETAIL)))


def detay_var(boy):
    """`boy` metrelik bir ayrıntı bu kademede üretilir mi?"""
    return boy >= DETAIL_MIN

def make_tube(name, r_bottom, r_top, height, center_xy=(0.0, 0.0), base_z=0.0,
              segments=16, cap_top=True, cap_bottom=False, smooth=True, col=None,
              phase=0.0):
    """
    Kesik koni / silindir — minare gövdesi, külah, şerefe için.

    `r_top = 0` koni verir (külah), `r_top = r_bottom` silindir (gövde).
    Segment sayısı siluetin okunmasını belirler: minare gövdesinde 16 yeterli,
    daha azında yakından çokgenleşir.

    `phase` ilk köşeyi döndürür (radyan). Az segmentli bir gövdede bunun
    görünür bir sonucu var: varsayılan sıfırda köşeler 0°, 45°, … düşer, yani
    **−Y ekseninde bir köşe** vardır, düz yüz değil. Menzil taşının kitabesi
    tam oraya konuyordu ve modelde yalnızca **kenarı** görünüyordu — pano
    yüzeye teğet kaldığı için. `phase = π/segments` yüzleri yarım adım
    çevirir ve −Y'ye düz bir yüz getirir.

    Varsayılan 0 bilinçlidir: minare ve külah gibi mevcut çağıranların
    çıktısı bit bit aynı kalsın.
    """
    segments = seg(segments, 6)
    cx, cy = center_xy
    bm = bmesh.new()
    metric_layers(bm)
    bot, top = [], []
    for i in range(segments):
        a = 2.0 * math.pi * i / segments + phase
        c, s = math.cos(a), math.sin(a)
        bot.append(bm.verts.new((cx + r_bottom * c, cy + r_bottom * s, base_z)))
        if r_top > 1e-6:
            top.append(bm.verts.new((cx + r_top * c, cy + r_top * s, base_z + height)))
    apex = None
    if r_top <= 1e-6:
        apex = bm.verts.new((cx, cy, base_z + height))
    bm.verts.ensure_lookup_table()

    side = []
    for i in range(segments):
        j = (i + 1) % segments
        if apex is not None:
            side.append(bm.faces.new((bot[i], bot[j], apex)))
        else:
            side.append(bm.faces.new((bot[i], bot[j], top[j], top[i])))

    # Yan yuzeyin UV'si: cevre boyu (u) x egim boyu (v), METRE. Gerekce UV_METRIC.
    slant = math.sqrt(height * height + (r_bottom - r_top) ** 2)
    uu = [2.0 * math.pi * r_bottom * i / segments for i in range(segments + 1)]
    metric = []
    for i, face in enumerate(side):
        if apex is not None:
            metric.append((face, [(uu[i], 0.0), (uu[i + 1], 0.0),
                                  ((uu[i] + uu[i + 1]) * 0.5, slant)]))
        else:
            metric.append((face, [(uu[i], 0.0), (uu[i + 1], 0.0),
                                  (uu[i + 1], slant), (uu[i], slant)]))
    _mark_metric(bm, metric)

    if smooth:
        _shade_smooth(bm, side)          # kapaklar DUZ kalir
    # Kapaklar UCGEN YELPAZE, n-gon degil.
    #
    # Sekizgen bir kapak tek yuz olarak yazilirsa FBX ihracinda "4'ten fazla
    # koseli yuz, teget uzayi hesaplanamiyor" uyarisi cikar ve normal haritasi
    # o yuzde sessizce yanlis okunur. Merkez kose + yelpaze bunu kokten cozer.
    if cap_top and apex is None:
        c = bm.verts.new((cx, cy, base_z + height))
        for i in range(segments):
            bm.faces.new((top[(i + 1) % segments], top[i], c))
    if cap_bottom:
        c = bm.verts.new((cx, cy, base_z))
        for i in range(segments):
            bm.faces.new((bot[i], bot[(i + 1) % segments], c))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return mesh_from_bmesh(name, bm, col)


def make_dome(name, radius, height, center_xy=(0.0, 0.0), base_z=0.0,
              segments=24, rings=8, col=None):
    """
    Yarım elipsoid kubbe.

    `height` yarıçaptan bağımsızdır: Osmanlı kubbesi tam yarım küre değildir,
    hafif basıktır. Yükseklik ayrı verilmezse bu oran kaybolur ve kubbe
    "balon" gibi durur.
    """
    segments, rings = seg(segments, 8), seg(rings, 3)
    cx, cy = center_xy
    bm = bmesh.new()
    metric_layers(bm)
    ringverts = []
    for r in range(rings):
        t = (math.pi * 0.5) * r / rings
        rr, zz = radius * math.cos(t), height * math.sin(t)
        ring = []
        for i in range(segments):
            a = 2.0 * math.pi * i / segments
            ring.append(bm.verts.new((cx + rr * math.cos(a), cy + rr * math.sin(a),
                                      base_z + zz)))
        ringverts.append(ring)
    apex = bm.verts.new((cx, cy, base_z + height))
    bm.verts.ensure_lookup_table()

    # Meridyen yay uzunlugu: kursun DILIMLERI tabandan tepeye boyle gider.
    arc = [0.0]
    for r in range(rings):
        t0 = (math.pi * 0.5) * r / rings
        t1 = (math.pi * 0.5) * (r + 1) / rings
        p0 = (radius * math.cos(t0), height * math.sin(t0))
        p1 = (radius * math.cos(t1), height * math.sin(t1))
        arc.append(arc[-1] + math.hypot(p1[0] - p0[0], p1[1] - p0[1]))
    # Cevre YARICAPLA olculur, halka yaricapiyla degil: dilim tepeye dogru
    # DARALIR. Halka yaricapi kullanilsaydi dilimler paralel kenarli olur ve
    # doku tepede yatay olarak gerilirdi — gercek kursun dilimi de daralir.
    uu = [2.0 * math.pi * radius * i / segments for i in range(segments + 1)]

    faces, metric = [], []
    for r in range(rings - 1):
        a, b = ringverts[r], ringverts[r + 1]
        for i in range(segments):
            j = (i + 1) % segments
            f = bm.faces.new((a[i], a[j], b[j], b[i]))
            faces.append(f)
            metric.append((f, [(uu[i], arc[r]), (uu[i + 1], arc[r]),
                               (uu[i + 1], arc[r + 1]), (uu[i], arc[r + 1])]))
    last = ringverts[-1]
    for i in range(segments):
        f = bm.faces.new((last[i], last[(i + 1) % segments], apex))
        faces.append(f)
        metric.append((f, [(uu[i], arc[rings - 1]), (uu[i + 1], arc[rings - 1]),
                           ((uu[i] + uu[i + 1]) * 0.5, arc[rings])]))
    _mark_metric(bm, metric)
    _shade_smooth(bm, faces)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return mesh_from_bmesh(name, bm, col)


def make_half_dome(name, radius, height, center_xy=(0.0, 0.0), base_z=0.0,
                   facing=0.0, segments=24, rings=8, col=None):
    """
    Yarım kubbe (yarım kubbe / semi-dome) — kubbenin **yarısı**.

    `facing` yarım kubbenin BAKTIĞI yön (radyan, +X'ten saat tersine): kütle
    o yöne doğru açılır, arkasında kirişi kapatan düz bir yüz kalır. Üsküdar
    Mihrimah'ta üç tane var (iki yan + kıble) ve giriş yönünde **yoktur** —
    yön parametresi bu yüzden şart: dördünü de koymak, planın "İstanbul'daki
    ilk ve tek üç yarım kubbeli örnek" olma özelliğini silerdi.

    Kubbe ile aynı basıklık oranını taşır (`height` yarıçaptan bağımsız):
    yarım kubbe tam yarım küre yapılırsa ana kubbeyle uyumsuz durur.
    """
    segments, rings = seg(segments, 8), seg(rings, 3)
    cx, cy = center_xy
    bm = bmesh.new()
    metric_layers(bm)

    # Yay, `facing` yonunu ORTALAR: [facing - pi/2, facing + pi/2].
    a0 = facing - math.pi * 0.5

    ringverts = []
    for r in range(rings):
        t = (math.pi * 0.5) * r / rings
        rr, zz = radius * math.cos(t), height * math.sin(t)
        ring = []
        for i in range(segments + 1):          # +1: yay KAPALI degil
            a = a0 + math.pi * i / segments
            ring.append(bm.verts.new((cx + rr * math.cos(a), cy + rr * math.sin(a),
                                      base_z + zz)))
        ringverts.append(ring)
    apex = bm.verts.new((cx, cy, base_z + height))
    bm.verts.ensure_lookup_table()

    arc = [0.0]
    for r in range(rings):
        t0 = (math.pi * 0.5) * r / rings
        t1 = (math.pi * 0.5) * (r + 1) / rings
        p0 = (radius * math.cos(t0), height * math.sin(t0))
        p1 = (radius * math.cos(t1), height * math.sin(t1))
        arc.append(arc[-1] + math.hypot(p1[0] - p0[0], p1[1] - p0[1]))
    uu = [math.pi * radius * i / segments for i in range(segments + 1)]

    faces, metric = [], []
    for r in range(rings - 1):
        a, b = ringverts[r], ringverts[r + 1]
        for i in range(segments):
            f = bm.faces.new((a[i], a[i + 1], b[i + 1], b[i]))
            faces.append(f)
            metric.append((f, [(uu[i], arc[r]), (uu[i + 1], arc[r]),
                               (uu[i + 1], arc[r + 1]), (uu[i], arc[r + 1])]))
    last = ringverts[-1]
    for i in range(segments):
        f = bm.faces.new((last[i], last[i + 1], apex))
        faces.append(f)
        metric.append((f, [(uu[i], arc[rings - 1]), (uu[i + 1], arc[rings - 1]),
                           ((uu[i] + uu[i + 1]) * 0.5, arc[rings])]))

    # KIRIS YUZU: yarim kubbenin arkasi acik kalirsa kutle icten gorunur ve
    # su gecirmez olmaz. Duz yuz, her halkanin iki ucunu tepeye baglar.
    for r in range(rings - 1):
        a, b = ringverts[r], ringverts[r + 1]
        f = bm.faces.new((a[0], b[0], b[-1], a[-1]))
        faces.append(f)
        metric.append((f, [(0.0, arc[r]), (0.0, arc[r + 1]),
                           (2.0 * radius, arc[r + 1]), (2.0 * radius, arc[r])]))
    f = bm.faces.new((last[0], apex, last[-1]))
    faces.append(f)
    metric.append((f, [(0.0, arc[rings - 1]), (radius, arc[rings]),
                       (2.0 * radius, arc[rings - 1])]))

    # TABAN KAPAGI. Ilk yazimda yoktu ve oz-test 17 acik kenar sayarak
    # bildirdi (16 yay + 1 kiris): yarim kubbenin alti aciktir ve tam
    # kubbeden farkli olarak duvarin icine gomulmez, disaridan gorunur.
    # Kapak yuzu ASAGI bakar; bu yuzden duz yuzler listesinden ayri tutulur
    # ve yumusak golgeleme uygulanmaz.
    # Yelpaze MERKEZDEN degil, kirisin bir UCUNDAN acilir. Merkeze bir gobek
    # koymak uc acik kenar birakiyordu (iki yaricap + kirisin alt kenari);
    # ucundan acilinca kapagin siniri tam olarak yay + kiris olur ve kiris
    # kenari zaten duz yuzun altindadir.
    base = ringverts[0]
    for i in range(1, segments):
        f = bm.faces.new((base[0], base[i], base[i + 1]))
        metric.append((f, [(uu[0], 0.0), (uu[i], -radius),
                           (uu[i + 1], -radius)]))

    _mark_metric(bm, metric)
    _shade_smooth(bm, faces)

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return mesh_from_bmesh(name, bm, col)


def make_wall_panel(name, width, height, thickness, origin, u_axis, n_axis,
                    openings=(), col=None):
    """
    GERÇEK açıklıkları olan duvar paneli — pencere/kapı boşluğu delinmiştir.

    Neden boolean değil: 8 000 ev ölçeğinde boolean hem yavaş hem kırılgandır
    (dejenere yüz, tutarsız normal). Burada duvar doğrudan **delikli örülür**:
    açıklık kenarlarından düşey kesitler alınır, her kesit boşluğun altında ve
    üstünde kapanır. Sonuç dörtgen, kapalı ve deterministiktir.

    Bunun panelden (cepheye yapıştırılmış koyu dikdörtgen) farkı sokak
    seviyesinde ortaya çıkar: **söve derinliği** görünür. Duvar kalınlığı kadar
    içeri giren yan/üst/alt yüzler ışığı yakalar; yaya gözü açıklığın gerçekten
    delik olduğunu oradan anlar.

    Yerel çerçeve: `u` duvar boyunca, `v` yukarı (z), `n` dışa doğru kalınlık.
    `origin` panelin TABAN ORTA noktasıdır (u=0, v=0, kalınlığın ortası).
    `openings` = [(u0, u1, v0, v1), ...]; panel kenarına DEĞMEMELİDİRLER.

    T-kavşağı yok: bütün yüzler, açıklıkların v seviyelerinden geçen ortak
    banda bölünür. Kapalı ve manifold kalması, normallerin tek çağrıda doğru
    yöne çevrilebilmesi içindir — el ile sarım yönü yazmak sessiz hata kaynağı.
    """
    U = Vector((u_axis[0], u_axis[1], 0.0)).normalized()
    N = Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    Z = Vector((0.0, 0.0, 1.0))
    O = Vector(origin)
    hw, ht = width * 0.5, thickness * 0.5
    eps = 1e-6

    # Açıklıklar panel kenarına DEĞMEZ — dördü de tam çevrelenmiş olmalı.
    #
    # Kapı için bu bir kısıt değil, doğru mimari: Osmanlı konutunda kapı taş bir
    # **eşiğin** üstüne oturur, zemine sıfırlanmaz. Kenara değen bir açıklık
    # paneli açık kenarlı (manifold olmayan) bırakır ve normal hesabı sessizce
    # bütün duvarı ters çevirebilir — sebebi ancak render'da anlaşılan bir hata.
    ops = sorted(openings, key=lambda o: o[0])
    for o in ops:
        if (o[0] <= -hw + eps or o[1] >= hw - eps
                or o[2] <= eps or o[3] >= height - eps):
            raise ValueError(f"make_wall_panel({name}): aciklik panel kenarina "
                             f"degiyor ya da disina tasiyor: {o} "
                             f"(panel {width:.2f}x{height:.2f})")

    # Ortak v seviyeleri: her yuz bu seviyelerde bolunur -> T-kavsagi kalmaz.
    levels = sorted({0.0, float(height)} | {float(o[2]) for o in ops} | {float(o[3]) for o in ops})

    def bands(v0, v1):
        inner = [v for v in levels if v0 + eps < v < v1 - eps]
        edges = [v0] + inner + [v1]
        return [(edges[i], edges[i + 1]) for i in range(len(edges) - 1)
                if edges[i + 1] - edges[i] > eps]

    bm = bmesh.new()
    cache = {}

    def vert(u, v, n):
        key = (round(u, 5), round(v, 5), round(n, 5))
        got = cache.get(key)
        if got is None:
            got = bm.verts.new(O + U * u + Z * v + N * n)
            cache[key] = got
        return got

    def quad(a, b, c, d):
        bm.faces.new((vert(*a), vert(*b), vert(*c), vert(*d)))

    # u ekseninde kesitler: aciklik kenarlari + panel uclari.
    #
    # Açıklıklar **sütunlara** gruplanır: aynı u aralığında birden çok açıklık
    # olabilir (iki katlı bir cephede üst üste hizalanmış pencereler — kâgir
    # yapıda kural budur, şaşırtmaca değil). Sütun başına tek delik varsayan
    # kod aynı yüzeyi iki kez yazar; Blender bunu reddeder, etmeseydi de
    # üst üste iki yüz olarak FBX'e sızardı.
    cols = {}
    for o in ops:
        cols.setdefault((round(float(o[0]), 5), round(float(o[1]), 5)),
                        []).append((float(o[2]), float(o[3])))
    keys = sorted(cols)
    for i in range(len(keys) - 1):
        if keys[i][1] > keys[i + 1][0] + eps:
            raise ValueError(f"make_wall_panel({name}): aciklik sutunlari "
                             f"kismen ust uste biniyor: {keys[i]} / {keys[i+1]}")
    for k in keys:
        vs = sorted(cols[k])
        for j in range(len(vs) - 1):
            if vs[j][1] > vs[j + 1][0] + eps:
                raise ValueError(f"make_wall_panel({name}): {k} sutununda iki "
                                 f"aciklik dikeyde cakisiyor: {vs[j]} / {vs[j+1]}")

    cuts = [-hw]
    for u0, u1 in keys:
        cuts += [u0, u1]
    cuts.append(hw)

    for i in range(len(cuts) - 1):
        a, b = cuts[i], cuts[i + 1]
        if b - a < eps:
            continue
        holes = cols.get((round(a, 5), round(b, 5)))
        if holes is None:
            rects = [(0.0, float(height))]
        else:
            rects = []
            prev = 0.0
            for v0, v1 in sorted(holes):
                if v0 - prev > eps:
                    rects.append((prev, v0))
                prev = v1
            if float(height) - prev > eps:
                rects.append((prev, float(height)))
        for r0, r1 in rects:
            for v0, v1 in bands(r0, r1):
                quad((a, v0, ht), (b, v0, ht), (b, v1, ht), (a, v1, ht))     # dis yuz
                quad((a, v0, -ht), (b, v0, -ht), (b, v1, -ht), (a, v1, -ht))  # ic yuz

    # Sove yuzleri: acikligin ICINE bakan dort yuzey. Yakin planin asil kazanci.
    for o in ops:
        u0, u1, v0, v1 = (float(x) for x in o)
        for u in (u0, u1):
            for w0, w1 in bands(v0, v1):
                quad((u, w0, -ht), (u, w0, ht), (u, w1, ht), (u, w1, -ht))
        for v in (v0, v1):
            quad((u0, v, -ht), (u1, v, -ht), (u1, v, ht), (u0, v, ht))

    # Panel uclari ve alt/ust kenari — ayni kesitlerde bolunur.
    for v0, v1 in bands(0.0, float(height)):
        for u in (-hw, hw):
            quad((u, v0, -ht), (u, v0, ht), (u, v1, ht), (u, v1, -ht))
    for i in range(len(cuts) - 1):
        a, b = cuts[i], cuts[i + 1]
        if b - a < eps:
            continue
        for v in (0.0, float(height)):
            quad((a, v, -ht), (b, v, -ht), (b, v, ht), (a, v, ht))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return ensure_outward(mesh_from_bmesh(name, bm, col))


def ensure_outward(obj):
    """
    Kapalı bir kabuğun normallerinin DIŞA baktığını **doğrular** — varsaymaz.

    `recalc_face_normals` normalleri tutarlı yapar ama kapalı bir kabuğu
    bütünüyle içe çevirmesi mümkündür. Blender bunu göstermez (arka yüzler de
    çizilir), Unity ise arka yüzü eler: duvar orada **görünmez** olur ve hata
    ancak oyun içinde, sebebi anlaşılmadan fark edilir.

    Ölçü işaretli hacimdir (diverjans teoremi): dışa dönük kapalı bir yüzeyde
    pozitiftir. Hacim **ağırlık merkezine göre** toplanır — kabuk orijinden
    uzaktaysa (ör. cephe düzlemi y = −6 m'de) ham koordinatla toplam iki büyük
    sayının farkı olur ve işaret float gürültüsüne kalır.
    """
    me = obj.data
    n = len(me.vertices)
    if n == 0:
        return obj
    cx = sum(v.co.x for v in me.vertices) / n
    cy = sum(v.co.y for v in me.vertices) / n
    cz = sum(v.co.z for v in me.vertices) / n
    c0 = Vector((cx, cy, cz))

    me.calc_loop_triangles()
    vol = 0.0
    for tri in me.loop_triangles:
        a, b, c = (me.vertices[i].co - c0 for i in tri.vertices)
        vol += a.dot(b.cross(c))
    if vol < 0.0:
        me.flip_normals()
        log(f"UYARI {obj.name}: kabuk ters cikti, normaller cevrildi "
            f"(isaretli hacim {vol / 6.0:.3f})")
    return obj


def join(objects, name, col=None):
    """
    Birden çok nesneyi tek mesh'te birleştirir (LOD ve collider üretimi için).
    `bpy.ops.object.join` bağlam ister; bmesh ile bağlamsız ve deterministik.

    Malzeme indeksleri YENİDEN EŞLENİR. Her nesnenin kendi slot listesi vardır ve
    hepsi 0'dan başlar; birleştirmede eşleme yapılmazsa bütün yüzeyler ilk
    malzemeye düşer ve model tek renk çıkar. (Bu bir kez yaşandı: kutu evin
    paleti ilk inceleme paketinde kaybolmuştu — hata render'a bakınca görüldü.)

    ## Nesne DÖNÜŞÜMÜ pişirilir

    `bm.from_mesh(obj.data)` ham mesh'i okur; nesnenin `location` ve
    `rotation_euler`ini **görmez**. Yani birleştirmeden önce döndürülmüş ya
    da taşınmış bir parça, birleştikten sonra sessizce başlangıç noktasına
    döner. Arasta tonozunun on parçası tam olarak bunu yaptı: hepsi üst üste
    yığıldı ve modelin tabanı eksiye kaydı.

    `matrix_basis` (ebeveynsiz nesnede `matrix_world`a eşittir ama
    **anında** günceldir; `matrix_world` depsgraph çevrimi ister) birim
    değilse mesh'in bir kopyası dönüştürülüp okunur. Birim olduğunda hiçbir
    şey değişmez — mevcut ve onaylanmış bütün varlıklar bit bit aynı kalır.
    """
    bm = bmesh.new()
    merged = []
    scratch = []
    # Katmani ONCEDEN ac: parcalarin bir kismi metrik UV tasiyor (kubbe, koni),
    # bir kismi tasimiyor. Katman ilk kez ortadaki bir parcayla dogsaydi ondan
    # oncekiler katmansiz kalirdi.
    metric_layers(bm)

    for obj in objects:
        start = len(bm.faces)
        if obj.matrix_basis.is_identity:
            bm.from_mesh(obj.data)
        else:
            tmp = obj.data.copy()
            tmp.transform(obj.matrix_basis)
            scratch.append(tmp)
            bm.from_mesh(tmp)
        bm.faces.ensure_lookup_table()

        remap = {}
        for slot, mat in enumerate(obj.data.materials):
            if mat is None:
                continue
            if mat not in merged:
                merged.append(mat)
            remap[slot] = merged.index(mat)

        for face in bm.faces[start:]:
            face.material_index = remap.get(face.material_index, 0)

    out = mesh_from_bmesh(name, bm, col)
    for mat in merged:
        out.data.materials.append(mat)
    for tmp in scratch:
        bpy.data.meshes.remove(tmp)
    return out


def bounds(obj):
    """(min, max) dünya uzayında — ölçek doğrulaması için."""
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    mn = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    mx = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    return mn, mx


# ---------------------------------------------------------------- malzeme

def make_material(name, color, roughness=0.85, metallic=0.0):
    """
    Principled BSDF, yalnızca sürüm-kararlı girdiler.

    Blender 4.x'te "Specular" → "Specular IOR Level", "Emission" → "Emission Color"
    olarak yeniden adlandırıldı. Base Color / Roughness / Metallic sabit kaldı;
    sadece bunlara dokunmak scripti sürüm değişimlerine karşı dayanıklı tutar.
    """
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        r, g, b = color[:3]
        a = color[3] if len(color) > 3 else 1.0
        bsdf.inputs["Base Color"].default_value = (r, g, b, a)
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
    mat.diffuse_color = (color[0], color[1], color[2], 1.0)   # viewport/solid
    return mat


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


# ------------------------------------------------------------------- kayıt

def save_blend(path):
    """Kanonik .blend — 'sadece sohbette var olan varlık yasak' kuralının dosyası."""
    path = os.path.abspath(path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=path)
    log(f"saved blend: {path}")
    return path
