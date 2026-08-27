"""
Hezarfen: 1632 — Doğa ve mezarlık kiti: servi, çınar, mezar taşı.

## Neden bir "ağaç kiti" mimarî kadar önemli

RESEARCH.md §4 yeşil dokuyu şöyle tarif eder: *"mezarlıklar **servi
alanlarıyla** kent içi büyük yeşil kütleler."* Yani 1632 İstanbul'unun
siluetinde servi, minare kadar taşıyıcı bir öğedir. Cami avlusunda servi ve
çınar bulunmaması bir eksik değil, **yanlış**tır: avlu ağaçsız kurulunca taş
bir meydan gibi okunur, oysa gölgelenmek için oraya oturulur.

Mezarlık da mahalle dokusunun parçasıdır — cami avlusunun kenarında, sokağın
başında. **Şahide** (baş taşı) mezarın kimliğidir: erkek mezarında sarık/kavuk
biçimli bir başlık taşır, kadın mezarında başlıksız ve daha alçaktır. Bu fark
uzaktan bile okunur ve mezarlığı "dikili taşlar" olmaktan çıkarır.

## Bilinçli boşluk: dokusuz yaprak

Gövde ve yaprak **dokusuz PBR**dir. Poly Haven'da kabuk dokusu var ama
yaprak **alfa atlası** yok; yapraklı ağaç alfa kartı ister ve elimizdeki
kütüphane onu vermiyor. Kendi telifimiz olmayan görsel indirmek yasak
(CLAUDE.md). Bu yüzden taç **katı geometri** olarak, ölçülmüş renkle
üretiliyor: servinin siluet değeri kütlesindedir, yaprak detayında değil.
Kayıtlı boşluk — bkz. ADR 0019.

Eksen sözleşmesi kitin geri kalanıyla aynı; ağaç eksenel simetriktir, pivot
taban merkezdedir.
"""

import math

import bmesh
import mathutils

import hz_blender as hz
import ottoman_kit as kit

# Kabuk ve yaprak artık `ottoman_kit.PALETTES` içinde. Burada ayrı malzeme
# tanımlamak, aynı adın iki farklı tanımı olması riskini geri getirirdi
# (selftest'in kilitlediği hata); ayrıca `build_unity_maps.py` yalnızca
# TEXTURE_ROLES'u tarar, buradaki bir tanım Unity'ye hiç ulaşmazdı.
#
# Servi neredeyse siyaha çalan koyu yeşildir; çınar belirgin biçimde açık ve
# sarıya çalar. İkisini aynı yeşile boyamak, İstanbul siluetinin en tanınır
# karşıtlığını siler — bu yüzden iki ayrı doku ve iki ayrı malzeme.
FOLIAGE_ROLE = {"servi": "foliage_servi", "cinar": "foliage_cinar"}
BARK_ROLE = {"servi": "bark", "cinar": "bark_cinar"}


def build_materials(palette_name="default", textured=False):
    return kit.build_materials(palette_name, textured=textured)


# ------------------------------------------------------------------ yardımcı

def _lathe(name, profile, center_xy, segments, col, smooth=True, jitter=0.0,
           seed=0):
    """
    (yarıçap, z) profilini eksen etrafında döndürür — kapalı, tek mesh.

    Uçlarda yarıçap sıfırsa kutup köşesi konur (kapak yerine), böylece n-gon
    kapak ve onun getirdiği teğet uzayı uyarısı hiç doğmaz.

    `jitter` her halkayı deterministik biçimde hafifçe bozar: ağaç ekseninde
    kusursuz simetrik olursa döküm gibi durur, on tanesi yan yana dizilince
    tekrar hemen görünür.
    """
    cx, cy = center_xy
    bm = bmesh.new()
    rings, poles = [], {}

    for i, (r, z) in enumerate(profile):
        if r <= 1e-6:
            poles[i] = bm.verts.new((cx, cy, z))
            rings.append(None)
            continue
        ring = []
        for s in range(segments):
            a = 2.0 * math.pi * s / segments
            rr = r
            if jitter > 0.0:
                # Deterministik "gurultu": tohum + halka + segment.
                n = math.sin((seed * 7.13 + i * 3.71 + s * 2.39)) * 0.5 + \
                    math.sin((seed * 2.17 + i * 5.29 + s * 4.11)) * 0.5
                rr = r * (1.0 + jitter * n)
            ring.append(bm.verts.new((cx + rr * math.cos(a),
                                      cy + rr * math.sin(a), z)))
        rings.append(ring)
    bm.verts.ensure_lookup_table()

    faces = []
    for i in range(len(profile) - 1):
        a, b = rings[i], rings[i + 1]
        if a is None and b is None:
            continue
        if a is None:                       # alt kutup -> ilk halka
            p = poles[i]
            for s in range(segments):
                faces.append(bm.faces.new((p, b[s], b[(s + 1) % segments])))
        elif b is None:                     # son halka -> ust kutup
            p = poles[i + 1]
            for s in range(segments):
                faces.append(bm.faces.new((a[(s + 1) % segments], a[s], p)))
        else:
            for s in range(segments):
                t = (s + 1) % segments
                faces.append(bm.faces.new((a[s], a[t], b[t], b[s])))
    if smooth:
        for f in faces:
            f.smooth = True

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def _scaled(profile, r_scale, z_scale, z0=0.0):
    return [(r * r_scale, z0 + z * z_scale) for (r, z) in profile]


# Normalize profiller (yaricap 0-1, yukseklik 0-1).
#
# Servi SUTUNSUdur: en genis yeri govdenin alt ucte biri, oradan tepeye kadar
# yavas incelir. En genis capin boya orani ~1/7 — bundan sismani selviyi kavak
# yapar.
SERVI_PROFILE = [
    (0.00, 0.00), (0.42, 0.03), (0.78, 0.12), (1.00, 0.28), (0.96, 0.45),
    (0.84, 0.60), (0.66, 0.74), (0.44, 0.86), (0.22, 0.94), (0.00, 1.00),
]

# Cinar YAYVANdir: govde kisa, tac genis ve yanlara acilir.
CINAR_LOBE = [
    (0.00, -1.00), (0.52, -0.72), (0.82, -0.36), (1.00, 0.00),
    (0.94, 0.34), (0.70, 0.66), (0.38, 0.88), (0.00, 1.00),
]


class AgacParams(object):
    def __init__(self, **kw):
        self.kind = kw.get("kind", "servi")       # servi | cinar
        self.height = kw.get("height", 11.0)
        self.trunk_r = kw.get("trunk_r", None)    # None -> boydan turetilir
        self.spread = kw.get("spread", None)      # None -> tipe gore turetilir
        self.seed = kw.get("seed", 0)
        self.segments = kw.get("segments", 9)
        self.palette = kw.get("palette", "default")

    def resolve(self):
        if self.kind not in ("servi", "cinar"):
            raise ValueError(f"kind={self.kind} (servi | cinar)")
        if self.trunk_r is None:
            self.trunk_r = self.height * (0.012 if self.kind == "servi" else 0.028)
        if self.spread is None:
            # `spread` YARIÇAPtır — ilk yazımda çapla karıştırdım ve 13 m'lik
            # servi 3,7 m genişlikte çıktı (boy/en 3,5): o bir servi değil,
            # kavaktır. Sütunsu servide boy/en oranı 6-10 arasıdır.
            # Çınarda tersi: taç boya yakın ya da ondan geniştir.
            self.spread = self.height * (0.072 if self.kind == "servi" else 0.45)
        return self


def build_agac(p, col, asset_name, textured=False):
    """Servi ya da çınar. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.resolve()
    mats, tex_sizes = build_materials(p.palette, textured=textured)
    parts = []
    H = p.height

    if p.kind == "servi":
        # Govde yalnizca ALTTA gorunur; tacin icinde kalan kismi cizilmez.
        trunk_h = H * 0.14
        parts.append(_put(hz.make_tube(
            f"{asset_name}_Govde", p.trunk_r * 1.35, p.trunk_r, trunk_h,
            (0.0, 0.0), 0.0, segments=7, cap_bottom=True, smooth=True, col=col),
            mats[BARK_ROLE[p.kind]]))
        parts.append(_put(_lathe(f"{asset_name}_Tac",
                                 _scaled(SERVI_PROFILE, p.spread, H * 0.90,
                                         z0=H * 0.10),
                                 (0.0, 0.0), p.segments, col,
                                 jitter=0.15, seed=p.seed),
                          mats[FOLIAGE_ROLE["servi"]]))
        radius = p.spread
    else:
        trunk_h = H * 0.42
        parts.append(_put(hz.make_tube(
            f"{asset_name}_Govde", p.trunk_r * 1.5, p.trunk_r * 0.85, trunk_h,
            (0.0, 0.0), 0.0, segments=8, cap_bottom=True, smooth=True, col=col),
            mats[BARK_ROLE[p.kind]]))
        # Tac: uc bindirmeli lob. Tek kure "yesil top" verir.
        # Yan loblar ANA LOBUN İÇİNE gömülür. Dışarı taşarlarsa iki kapalı
        # kabuk siluetin kenarında kesişir ve o kesişim çentik olarak okunur —
        # boolean birleşim olmadan tek çare, taşmayı azaltmak.
        lobes = ((0.00, 0.00, 1.00, 0.62), (0.36, 0.11, 0.58, 0.46),
                 (-0.27, -0.33, 0.54, 0.42))
        for i, (ox, oy, rs, hs) in enumerate(lobes):
            rr = p.spread * rs
            parts.append(_put(_lathe(
                f"{asset_name}_Tac{i}",
                _scaled(CINAR_LOBE, rr, H * 0.30 * hs / 0.62,
                        z0=trunk_h + H * 0.30 * hs / 0.62),
                (ox * p.spread, oy * p.spread), p.segments, col,
                jitter=0.07, seed=p.seed * 3 + i), mats[FOLIAGE_ROLE["cinar"]]))
        radius = p.spread

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    # LOD1: ayni siluet, kaba. Agac uzaktan bir KUTLEDIR.
    l1 = []
    if p.kind == "servi":
        l1.append(_put(_lathe(f"{asset_name}_L1",
                              _scaled(SERVI_PROFILE, p.spread, H * 0.90,
                                      z0=H * 0.10), (0.0, 0.0), 5, col),
                       mats[FOLIAGE_ROLE["servi"]]))
    else:
        l1.append(_put(_lathe(f"{asset_name}_L1",
                              _scaled(CINAR_LOBE, p.spread, H * 0.30,
                                      z0=H * 0.42 + H * 0.30),
                              (0.0, 0.0), 6, col), mats[FOLIAGE_ROLE["cinar"]]))
        l1.append(_put(hz.make_box(f"{asset_name}_L1g",
                                   (p.trunk_r * 2.4, p.trunk_r * 2.4, H * 0.42),
                                   (0.0, 0.0, H * 0.21), col), mats[BARK_ROLE[p.kind]]))
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    # Carpisma: yalnizca GOVDE. Oyuncu tacin altindan gecebilmeli; agacin
    # tepesine carpmak, sacak altindan gecememekle ayni tur hayal kirikligi.
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (p.trunk_r * 2.6, p.trunk_r * 2.6, H * 0.5),
                      (0.0, 0.0, H * 0.25), col)
    hz.assign(ucx, mats[BARK_ROLE[p.kind]])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(radius * 2.0, 3),
                wall_depth=round(radius * 2.0, 3),
                kind=f"agac_{p.kind}", palette=p.palette)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------------ mezar taşı

class MezarParams(object):
    def __init__(self, **kw):
        self.gender = kw.get("gender", "erkek")   # erkek | kadin
        self.height = kw.get("height", 1.15)      # sahide boyu (yer ustu)
        self.width = kw.get("width", 0.30)
        self.thickness = kw.get("thickness", 0.11)
        self.tilt_deg = kw.get("tilt_deg", 5.0)
        self.footstone = kw.get("footstone", True)
        self.palette = kw.get("palette", "default")


def build_mezar(p, col, asset_name, textured=False):
    """
    Şahide (baş taşı) + ayak taşı.

    Erkek mezarında şahide **başlık** taşır (sarık/kavuk); kadın mezarında
    başlıksızdır ve daha alçaktır. Uzaktan bile okunan bu fark olmadan mezarlık
    "dikili taşlar" olur.

    Taş **eğiktir**: eski mezarlıkta şahideler zeminin oturmasıyla yatar ve
    mezarlığa o karakterini veren şey bu düzensizliktir — dimdik bir sıra,
    modern bir şehitlik gibi durur.
    """
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    H = p.height if p.gender == "erkek" else p.height * 0.82

    parts.append(_put(hz.make_box(f"{asset_name}_Sahide",
                                  (p.width, p.thickness, H),
                                  (0.0, 0.0, H * 0.5), col), mats["cutstone"]))

    # KİTABE: şahidenin ön yüzündeki oyulmuş yazı panosu.
    #
    # Şahideyi mezar taşı yapan şey oymadır — düz bir levha sınır taşı gibi
    # okunur. Yazının kendisi doku işidir (henüz yok); burada verilen, ışığın
    # tutunacağı **girinti**dir: panonun kenarı gölge çizgisi bırakır ve taş
    # yakın planda boş kalmaz.
    kw_, kh = p.width * 0.72, H * 0.58
    parts.append(_put(hz.make_box(
        f"{asset_name}_Kitabe", (kw_, 0.022, kh),
        (0.0, -p.thickness * 0.5 - 0.011, H * 0.52), col), mats["shadow"]))
    for dx, dz, sw, sh in ((0.0, kh * 0.5 + 0.02, kw_ + 0.04, 0.035),
                           (0.0, -kh * 0.5 - 0.02, kw_ + 0.04, 0.035),
                           (kw_ * 0.5 + 0.02, 0.0, 0.035, kh + 0.10),
                           (-kw_ * 0.5 - 0.02, 0.0, 0.035, kh + 0.10)):
        parts.append(_put(hz.make_box(
            f"{asset_name}_KitabeSove", (sw, 0.030, sh),
            (dx, -p.thickness * 0.5 - 0.015, H * 0.52 + dz), col),
            mats["cutstone"]))

    if p.gender == "erkek":
        # Kavuk: silindirik govde + sarik halkasi.
        parts.append(_put(hz.make_tube(
            f"{asset_name}_Kavuk", p.width * 0.44, p.width * 0.40, 0.22,
            (0.0, 0.0), H, segments=8, smooth=True, col=col), mats["cutstone"]))
        parts.append(_put(hz.make_tube(
            f"{asset_name}_Sarik", p.width * 0.52, p.width * 0.52, 0.09,
            (0.0, 0.0), H + 0.05, segments=8, smooth=True, col=col),
            mats["cutstone"]))
        total_h = H + 0.22
    else:
        # Kadin sahidesi: ust ucu yuvarlatilir (basit bir tepe blogu).
        parts.append(_put(hz.make_box(
            f"{asset_name}_Tepe", (p.width * 0.78, p.thickness, 0.10),
            (0.0, 0.0, H + 0.05), col), mats["cutstone"]))
        total_h = H + 0.10

    if p.footstone:
        parts.append(_put(hz.make_box(
            f"{asset_name}_AyakTasi", (p.width * 0.72, p.thickness * 0.85, H * 0.42),
            (0.0, -1.65, H * 0.21), col), mats["cutstone"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    # Taş EĞİLİR — ötelenmez.
    #
    # İlk yazımda eğiklik, parçaları yükseklikle orantılı olarak Y'de kaydırarak
    # taklit ediliyordu: bu taşı eğmez, olduğu gibi öteler. Gerçek dönüşü mesh'e
    # uygulayıp tabanı yeniden yere oturtmak gerekiyor.
    _tilt(lod0, math.radians(p.tilt_deg))
    _drop_to_ground(lod0)
    l1 = [_put(hz.make_box(f"{asset_name}_L1", (p.width, p.thickness, total_h),
                           (0.0, 0.0, total_h * 0.5), col), mats["cutstone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (p.width, 2.0, total_h),
                      (0.0, -0.8, total_h * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(p.width, 3), wall_depth=round(2.0, 3),
                kind=f"mezar_{p.gender}", palette=p.palette)
    return lod0, lod1, ucx, info


def _tilt(obj, angle_rad, pivot=(0.0, 0.0, 0.0)):
    """Mesh'i X ekseni etrafında, verilen noktadan döndürür."""
    R = mathutils.Matrix.Rotation(angle_rad, 4, "X")
    T1 = mathutils.Matrix.Translation((-pivot[0], -pivot[1], -pivot[2]))
    T2 = mathutils.Matrix.Translation(pivot)
    obj.data.transform(T2 @ R @ T1)


def _drop_to_ground(obj):
    """Döndürme sonrası tabanı z=0'a getirir (pivot sözleşmesi)."""
    mn, _ = hz.bounds(obj)
    if abs(mn[2]) > 1e-6:
        obj.data.transform(mathutils.Matrix.Translation((0.0, 0.0, -mn[2])))


def _put(obj, mat):
    hz.assign(obj, mat)
    return obj
