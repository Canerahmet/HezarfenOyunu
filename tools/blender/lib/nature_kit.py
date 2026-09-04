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
import random

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


# Yaprak KUMESI: lobdan yayvan, ucu kut. Kume tek basina bir agac degil,
# bir yaprak kutlesidir; siluete cikinti versin diye tepesi duz degil sisman.
CINAR_KUME = [
    (0.00, -1.00), (0.62, -0.66), (0.92, -0.24), (1.00, 0.16),
    (0.86, 0.52), (0.54, 0.82), (0.00, 1.00),
]


def _konumla(obj, azimut, egim, taban):
    """Z boyunca kurulmus bir parcayi verilen noktadan azimut/egimle diker."""
    R = (mathutils.Matrix.Rotation(azimut, 4, "Z") @
         mathutils.Matrix.Rotation(egim, 4, "Y"))
    obj.data.transform(mathutils.Matrix.Translation(taban) @ R)
    return obj


def _dal_ucu(azimut, egim, uzunluk, taban):
    """`_konumla` ile dikilen bir dalin ust ucunun dunya konumu."""
    return (taban[0] + uzunluk * math.sin(egim) * math.cos(azimut),
            taban[1] + uzunluk * math.sin(egim) * math.sin(azimut),
            taban[2] + uzunluk * math.cos(egim))


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
            self.spread = self.height * (0.072 if self.kind == "servi" else 0.50)
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
        # ÇINAR ÇATALLANIR. İlk yazımda taç üç düzgün lobdu ve 356 üçgenlik
        # o kütle karede "yeşil balçık" olarak okunuyordu: silueti kırılmayan
        # bir ağaç, uzaktan ağaç değil tepedir. Servide bu kusur yok çünkü
        # servi gerçekten düzgün bir sütundur — yani hata "az üçgen" değildi,
        # **yanlış şekildi**.
        #
        # Yeni kurgu çınarın gerçek yapısını kurar: alçak ve kalın bir gövde,
        # ondan ayrılan dört ANA DAL, her birinin ikiye ayrılması ve dal
        # uçlarına oturan yaprak KÜMELERİ.
        #
        # Bir kuralı bilerek tersine çeviriyorum: eski yorum "yan loblar ana
        # lobun içine gömülmeli, taşarsa kesişim çentik okunur" diyordu. O,
        # DÜZGÜN bir taç için doğruydu — orada çentik kusurdu. Yaprak
        # kütlesinde çentik tam da aranılan şeydir: silueti kıran şey yaprağı
        # yaprak yapar. Aynı geometrik olay, bağlama göre kusur ya da çözüm.
        S = p.spread
        rng = random.Random(1632 + p.seed * 17)
        trunk_h = H * 0.28
        parts.append(_put(hz.make_tube(
            f"{asset_name}_Govde", p.trunk_r * 1.6, p.trunk_r * 0.95, trunk_h,
            (0.0, 0.0), 0.0, segments=8, cap_bottom=True, smooth=True, col=col),
            mats[BARK_ROLE[p.kind]]))

        catal = (0.0, 0.0, trunk_h * 0.94)
        kume_yerleri = []
        ana_sayi = 4
        for i in range(ana_sayi):
            az = 2.0 * math.pi * i / ana_sayi + rng.uniform(-0.22, 0.22)
            egim = math.radians(48.0 + rng.uniform(-6.0, 6.0))
            uz1 = S * 0.62 * rng.uniform(0.90, 1.10)
            parts.append(_put(_konumla(hz.make_tube(
                f"{asset_name}_Dal{i}", p.trunk_r * 0.62, p.trunk_r * 0.36, uz1,
                segments=5, cap_bottom=True, smooth=True, col=col),
                az, egim, catal), mats[BARK_ROLE[p.kind]]))
            dirsek = _dal_ucu(az, egim, uz1 * 0.97, catal)

            for j in (-1, 1):
                az2 = az + j * math.radians(30.0 + rng.uniform(-8.0, 8.0))
                egim2 = math.radians(34.0 + rng.uniform(-7.0, 7.0))
                uz2 = S * 0.50 * rng.uniform(0.85, 1.15)
                parts.append(_put(_konumla(hz.make_tube(
                    f"{asset_name}_Dal{i}_{j}", p.trunk_r * 0.38,
                    p.trunk_r * 0.20, uz2, segments=5, cap_bottom=True,
                    smooth=True, col=col),
                    az2, egim2, dirsek), mats[BARK_ROLE[p.kind]]))
                # Yaprak dalin UCUNDA degil, dal BOYUNCA durur. Tek kume
                # koyunca taç "sopa ucunda top" oluyordu — ilk denemenin
                # kusuru buydu: siluet kırılmıştı ama yanlış ölçekte,
                # yaprak arasında değil toplar arasında.
                for t, (oran, rk) in enumerate(((0.55, 0.070), (0.95, 0.090))):
                    kume_yerleri.append((_dal_ucu(az2, egim2, uz2 * oran, dirsek),
                                         H * rk, H * rk * 0.66))

        # Tepe kümeleri eksene yakın ve yükseklerdir: çınarın taç tepesi
        # ortadadır, dış halka ise genişliği verir. İkisini tek halkaya
        # yükletmek ya basık ya yumurta biçimli bir taç verir.
        for k in range(4):
            az = 2.0 * math.pi * k / 4.0 + 0.5
            r = H * 0.12
            kume_yerleri.append(((r * math.cos(az), r * math.sin(az), H * 0.80),
                                 H * 0.115, H * 0.115))

        # İç doldurma: çatalın hemen üstünde iki küme. Bunlar olmadan taç
        # gövdeden kopuk duruyordu — tepeden bakınca ortada delik, yandan
        # bakınca "sopa ucunda çali". Gerçek çınarda da yaprak çatala kadar iner.
        for k in range(2):
            az = math.pi * k + 0.9
            r = H * 0.07
            kume_yerleri.append(((r * math.cos(az), r * math.sin(az), H * 0.52),
                                 H * 0.105, H * 0.075))

        for i, (merkez, rk, hk) in enumerate(kume_yerleri):
            parts.append(_put(_lathe(
                f"{asset_name}_Kume{i}",
                _scaled(CINAR_KUME, rk, hk, z0=merkez[2]),
                (merkez[0], merkez[1]), 6, col,
                jitter=0.26, seed=p.seed * 5 + i * 3),
                mats[FOLIAGE_ROLE["cinar"]]))
        radius = S

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    # LOD1: ayni siluet, kaba. Agac uzaktan bir KUTLEDIR.
    l1 = []
    if p.kind == "servi":
        l1.append(_put(_lathe(f"{asset_name}_L1",
                              _scaled(SERVI_PROFILE, p.spread, H * 0.90,
                                      z0=H * 0.10), (0.0, 0.0), 5, col),
                       mats[FOLIAGE_ROLE["servi"]]))
    else:
        # LOD1 ayni KUTLEyi verir: 0,28H'den 0,96H'ye kadar yayvan bir tac.
        # Uzaktan siluetin kirilmasi zaten okunmaz; okunan sey nerede
        # baslayip nerede bittigidir.
        l1.append(_put(_lathe(f"{asset_name}_L1",
                              _scaled(CINAR_LOBE, p.spread, H * 0.34,
                                      z0=H * 0.62),
                              (0.0, 0.0), 6, col), mats[FOLIAGE_ROLE["cinar"]]))
        l1.append(_put(hz.make_box(f"{asset_name}_L1g",
                                   (p.trunk_r * 2.6, p.trunk_r * 2.6, H * 0.28),
                                   (0.0, 0.0, H * 0.14), col), mats[BARK_ROLE[p.kind]]))
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    # Carpisma: yalnizca GOVDE. Oyuncu tacin altindan gecebilmeli; agacin
    # tepesine carpmak, sacak altindan gecememekle ayni tur hayal kirikligi.
    # CARPISMA GOVDE BOYUNDA BITER — HAVADA DEVAM ETMEZ.
    #
    # Kutu her iki turde de H*0,5 idi. Servide bu dogru: servinin taci
    # neredeyse yere kadar iner, yani o yukseklikte gercekten bir kutle
    # var. Cinarda degil — govde 0,28H'de CATALLANIR ve ustunde yalniz
    # dallar ile yaprak vardir. 16 m'lik bir cinarda kutu 8 m'ye
    # cikiyordu, yani 4,5 m ile 8 m arasi **gorunmez bir duvardi**.
    # Yerde yuruyen biri bunu hic fark etmez (bas hizasinin cok
    # ustunde); ucan biri carpar. Bu bir ucus oyunu.
    carp_h = trunk_h if p.kind == "cinar" else H * 0.5
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (p.trunk_r * 2.6, p.trunk_r * 2.6, carp_h),
                      (0.0, 0.0, carp_h * 0.5), col)
    hz.assign(ucx, mats[BARK_ROLE[p.kind]])

    _mn, _mx = hz.bounds(lod0)
    _boya_olcekle((lod0, lod1, ucx), H, _mx[2] - _mn[2])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    _un, _ux = hz.bounds(ucx)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(radius * 2.0, 3),
                wall_depth=round(radius * 2.0, 3),
                # CARPISMA KUTUSU KATALOGA GIRER.
                #
                # Kutunun boyu degistirildi ve `catalog.json` diff'i HIC
                # kipirdamadi — yani depo kuralinin ("commit'lemeden once
                # katalog diff'ine bak") dayandigi kayit, gemiye giden bir
                # ozelligi hic tasimiyordu. Kaydedilmeyen sey olculemez;
                # olculemeyen sey de sessizce bozulur.
                ucx_h=round(_ux[2] - _un[2], 3),
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


def _boya_olcekle(objs, hedef, olculen):
    """
    Taç tepesi gövde boyunun tamına çıkmaz: çınarda en üst yaprak kümesi
    ~0,92H'de biter. İstenen boy ise AĞACIN boyudur.

    İlk çözüm tepe kümelerini yukarı itmekti ve taçı ikiye böldü — aradaki
    boşluk karede "ayrı duran şapka" olarak okundu. Şekli bozmadan boyu
    tutturmanın tek yolu **düzgün ölçek**: bütün LOD'lar ve çarpışma aynı
    katsayıyla büyür, oranlar aynen kalır, pivot z=0'da olduğu için taban
    yerinde durur.
    """
    if olculen <= 1e-6 or abs(hedef - olculen) < 1e-4:
        return 1.0
    k = hedef / olculen
    M = mathutils.Matrix.Diagonal((k, k, k, 1.0))
    for o in objs:
        o.data.transform(M)
    return k


def _drop_to_ground(obj):
    """Döndürme sonrası tabanı z=0'a getirir (pivot sözleşmesi)."""
    mn, _ = hz.bounds(obj)
    if abs(mn[2]) > 1e-6:
        obj.data.transform(mathutils.Matrix.Translation((0.0, 0.0, -mn[2])))


def _put(obj, mat):
    hz.assign(obj, mat)
    return obj
