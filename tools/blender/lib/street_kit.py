"""
Hezarfen: 1632 — Sokak donatısı: çeşme ve dükkân (plan Faz 2b).

Sokağı sokak yapan şey ev sırası değil, **durulan yerler**dir: su alınan çeşme,
alışveriş yapılan dükkân. ADR 0016'nın mahallesi şu an bunlardan yoksun ve
yaya seviyesinde ilk göze çarpan eksik bu.

## Çeşme

Klasik duvar çeşmesinin imzası **sivri kemerli niş**tir. Niş gerçek bir
boşluktur — cepheye çizilmiş bir dikdörtgen değil. Boolean kullanmadan
üretilir: kemer eğrisi boyunca şerit örülür, altında iki ayak, üstünde alınlık.

Parçalar: **teknelik** (yalak), **ayna taşı** (lülenin bulunduğu yüz),
**kitabe** (kemer üstü yazı levhası), **silme** (üst korniş).

## Dükkân

Arasta biriminin karakteristiği **kepenk**tir: alt kanat aşağı katlanınca
tezgâh, üst kanat yukarı kalkınca sundurma olur. Kapalı bir kutu çizmek
dükkânı depo yapar; dükkânı dükkân yapan şey bu iki kanattır.

Eksen sözleşmesi kitin geri kalanıyla aynı: ön cephe −Y (Unity'de +Z).
"""

import math

import bmesh

import hz_blender as hz
import ottoman_kit as kit

# Sivri kemer "iki merkezli"dir. `ARCH_C` merkezlerin eksenden kaçıklığını
# yarı-açıklığın katı olarak verir: 0 yuvarlak kemer, büyüdükçe sivrileşir.
# 0,35 Osmanlı sivri kemerinin okunaklı orta noktası (T2).
ARCH_C = 0.35


def arch_points(half_span, spring_z, steps=14, c=ARCH_C):
    """
    Sivri kemer eğrisi: `(-half_span, spring_z)`'den tepeye, oradan sağa.

    İki merkezli kemer: sol yay sağdaki merkezden, sağ yay soldakinden çizilir.
    Tepe yüksekliği **türetilir** (`a·√(1+2c)`), elle verilmez — açıklık
    değiştiğinde kemerin karakteri korunsun diye.
    """
    a = half_span
    R = a * (1.0 + c)
    cx = a * c
    rise = a * math.sqrt(1.0 + 2.0 * c)

    a0 = math.atan2(0.0, -a - cx)                 # (-a, spring) noktasinin acisi
    a1 = math.atan2(rise, -cx)                    # tepe noktasinin acisi
    pts = []
    for i in range(steps + 1):
        t = i / steps
        ang = a0 + (a1 - a0) * t
        pts.append((cx + R * math.cos(ang), spring_z + R * math.sin(ang)))
    # Sag yari: ayna.
    for i in range(steps - 1, -1, -1):
        x, z = pts[i]
        pts.append((-x, z))
    return pts, rise


class CesmeParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 2.9)
        self.height = kw.get("height", 3.7)
        self.depth = kw.get("depth", 1.05)
        self.niche_w = kw.get("niche_w", 1.35)
        self.spring_z = kw.get("spring_z", 1.75)     # kemer basma kotu
        self.niche_depth = kw.get("niche_depth", 0.38)
        self.basin = kw.get("basin", True)
        self.kitabe = kw.get("kitabe", True)
        # Duvar kanatlari: cesme SERBEST DURMAZ.
        #
        # "Duvar cesmesi" adi tesadufi degil — cesme bir bahce, avlu ya da yapi
        # duvarina gomulur; tek basina duran tas kutu, ceyrek yuzyil sonrasinin
        # meydan cesmesidir. Kanatsiz uretilen ilk surumde tam bu oldu ve
        # sokakta anit gibi durdu.
        self.wings = kw.get("wings", 1.8)          # 0 -> kanatsiz
        self.wing_h = kw.get("wing_h", 2.35)
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.niche_w > self.width - 0.7:
            errs.append(f"niche_w={self.niche_w} govdeye ({self.width}) sigmaz")
        _, rise = arch_points(self.niche_w * 0.5, self.spring_z)
        top = self.spring_z + rise
        if top > self.height - 0.5:
            errs.append(f"kemer tepesi {top:.2f} m, govde {self.height} m — "
                        f"alinlik icin yer yok")
        if self.niche_depth > self.depth - 0.25:
            errs.append(f"niche_depth={self.niche_depth} govde derinligini yiyor")
        if errs:
            raise ValueError("CesmeParams gecersiz: " + "; ".join(errs))
        return self


def arched_panel(name, width, height, thickness, origin, u_axis, n_axis,
                 spans=(), sill_z=0.0, spring_z=2.0, col=None,
                 steps=14, c=ARCH_C):
    """
    Sivri kemerli **gerçek açıklıkları** olan duvar paneli.

    `hz.make_wall_panel` ile aynı eksen sözleşmesi (`origin` panel tabanının
    ortası, `u_axis` genişlik yönü, `n_axis` dışa bakan yön, kalınlık düzleme
    göre ortalanır). Tek fark: açıklığın üstü düz lento değil **iki merkezli
    sivri kemer**tir. Çeşme nişi, avlu kapısı, kilise penceresi ve çan kulesi
    açıklığı hep buradan çıkar — mahallede tek bir kemer karakteri olsun diye.

    ## Neden bütün açıklıklar aynı ölçüde olmak zorunda

    Panelin her yüzü aynı z seviyelerinde bölünür; böylece komşu yüzeyler
    arasında **T-kavşağı** kalmaz (bir kenarın ortasından geçen serbest köşe:
    ışıkta çatlak, LOD'da yırtık). Açıklıklar farklı genişlikte olsaydı kemer
    tepeleri de farklı olurdu ve o seviyeler yalnızca bazı sütunlarda bulunurdu
    — kaçınılmaz T-kavşağı. Kısıt mimarî olarak da doğrudur: revak bir
    **ritim**dir, aynı açıklığın tekrarıdır. Farklı ölçü isteyen yer ayrı panel
    ister.
    """
    U = hz.Vector((u_axis[0], u_axis[1], 0.0)).normalized()
    N = hz.Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    Z = hz.Vector((0.0, 0.0, 1.0))
    O = hz.Vector(origin)
    hw, ht = width * 0.5, thickness * 0.5
    eps = 1e-6

    ops = sorted([(float(a), float(b)) for a, b in spans], key=lambda s: s[0])
    if not ops:
        raise ValueError(f"arched_panel({name}): en az bir aciklik gerekir")

    w0 = ops[0][1] - ops[0][0]
    for u0, u1 in ops:
        if abs((u1 - u0) - w0) > 1e-4:
            raise ValueError(f"arched_panel({name}): butun aciklıklar ayni "
                             f"genislikte olmali ({w0:.3f} m); {u1 - u0:.3f} geldi")
    for i in range(len(ops) - 1):
        if ops[i + 1][0] - ops[i][1] < 0.05:
            raise ValueError(f"arched_panel({name}): {i}. ve {i+1}. aciklik "
                             f"arasinda ayak kalmiyor")

    half = w0 * 0.5
    base_pts, rise = arch_points(half, spring_z, steps=steps, c=c)
    top = spring_z + rise
    if sill_z < -eps or sill_z >= spring_z - eps:
        raise ValueError(f"arched_panel({name}): sill_z={sill_z} kemer basmasi "
                         f"{spring_z} ile taban arasinda olmali")
    if top >= height - eps:
        raise ValueError(f"arched_panel({name}): kemer tepesi {top:.2f} m, "
                         f"panel {height:.2f} m — ustunde duvar kalmiyor")
    if ops[0][0] <= -hw + eps or ops[-1][1] >= hw - eps:
        raise ValueError(f"arched_panel({name}): aciklik panel kenarina degiyor")

    has_sill = sill_z > eps
    levels = sorted({0.0, float(height), spring_z, top}
                    | ({sill_z} if has_sill else set()))

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

    def quad(a, b, cc, d):
        bm.faces.new((vert(*a), vert(*b), vert(*cc), vert(*d)))

    def tri(a, b, cc):
        bm.faces.new((vert(*a), vert(*b), vert(*cc)))

    def face_pair(a0, a1, v0, v1):
        """Panelin iki yüzünde aynı dörtgen."""
        for n in (ht, -ht):
            quad((a0, v0, n), (a1, v0, n), (a1, v1, n), (a0, v1, n))

    # Dolu ayaklar: acikliklarin arasi ve panel uclari.
    piers = []
    prev = -hw
    for u0, u1 in ops:
        piers.append((prev, u0))
        prev = u1
    piers.append((prev, hw))
    for a0, a1 in piers:
        if a1 - a0 < eps:
            continue
        for v0, v1 in bands(0.0, float(height)):
            face_pair(a0, a1, v0, v1)

    for u0, u1 in ops:
        cu = (u0 + u1) * 0.5
        pts = [(cu + x, z) for (x, z) in base_pts]
        if has_sill:
            for v0, v1 in bands(0.0, sill_z):
                face_pair(u0, u1, v0, v1)
        for v0, v1 in bands(top, float(height)):
            face_pair(u0, u1, v0, v1)
        # Kemerin ustu: egri boyunca serit, TEPE KOTUNDA duz kesilir.
        #
        # Tepe noktasi zaten `top` seviyesindedir; oradaki dortgen kendi
        # ustune kapanir. Dejenere yuz uretmek yerine ucgen yazilir — Blender
        # ayni kosesi iki kez kullanan yuzu kabul etmez, etseydi de FBX'te
        # sifir alanli yuz olarak sizardi.
        for i in range(len(pts) - 1):
            x0, z0 = pts[i]
            x1, z1 = pts[i + 1]
            hi0, hi1 = abs(z0 - top) < eps, abs(z1 - top) < eps
            for n in (ht, -ht):
                if hi0 and hi1:
                    continue
                elif hi1:
                    tri((x0, z0, n), (x1, z1, n), (x0, top, n))
                elif hi0:
                    tri((x0, z0, n), (x1, z1, n), (x1, top, n))
                else:
                    quad((x0, z0, n), (x1, z1, n), (x1, top, n), (x0, top, n))

        # Söve: açıklığın İÇİNE bakan yüzeyler.
        if has_sill:
            quad((u0, sill_z, ht), (u1, sill_z, ht),
                 (u1, sill_z, -ht), (u0, sill_z, -ht))
        for u in (u0, u1):
            quad((u, sill_z, ht), (u, sill_z, -ht),
                 (u, spring_z, -ht), (u, spring_z, ht))
        for i in range(len(pts) - 1):
            x0, z0 = pts[i]
            x1, z1 = pts[i + 1]
            quad((x0, z0, ht), (x0, z0, -ht), (x1, z1, -ht), (x1, z1, ht))

    # Panel uclari, ust ve alt kenar — ayni seviyelerde bolunur.
    for u in (-hw, hw):
        for v0, v1 in bands(0.0, float(height)):
            quad((u, v0, ht), (u, v0, -ht), (u, v1, -ht), (u, v1, ht))
    cuts = [-hw]
    for u0, u1 in ops:
        cuts += [u0, u1]
    cuts.append(hw)
    solid_at_base = set(piers) if not has_sill else set(piers) | set(ops)
    for i in range(len(cuts) - 1):
        a0, a1 = cuts[i], cuts[i + 1]
        if a1 - a0 < eps:
            continue
        quad((a0, float(height), ht), (a1, float(height), ht),
             (a1, float(height), -ht), (a0, float(height), -ht))
        if (a0, a1) in solid_at_base:
            quad((a0, 0.0, ht), (a1, 0.0, ht), (a1, 0.0, -ht), (a0, 0.0, -ht))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def iron_grille(name, ow, oh, origin, u_axis, n_axis, cu, cv, thick, mats, col):
    """
    Pencere şebekesi — dövme demir ızgara.

    Açıklığın **dış yüzüne yakın** durur (kâgir duvarda şebeke dış yüzeye yakın
    kurulur), böylece sövenin derinliği arkasında kalır ve okunur. Mescitte,
    kilisede ve sinagogda aynı şebeke kullanılır: demir işçiliği mahalleye
    aittir, cemaate değil.
    """
    # ORTA KADEME: şebeke bir GÖLGE DOKUSUDUR. 2 cm'lik demir çubuklar
    # birkaç yüz metreden piksel altına düşer ve açıklığın koyu
    # dikdörtgeni zaten okunur. Ölçüldü: Süleymaniye'nin orta
    # kademesinin %13'ü (7 200 üçgen) şebekeydi.
    if not hz.detay_var(0.35):
        return []
    ox, oy, oz = origin
    n = thick * 0.5 - 0.09
    bar = 0.032
    out = []

    def place(du, dv, sw, sh):
        px = ox + u_axis[0] * (cu + du) + n_axis[0] * n
        py = oy + u_axis[1] * (cu + du) + n_axis[1] * n
        sz = (sw, bar, sh) if abs(u_axis[0]) > 0.5 else (bar, sw, sh)
        obj = hz.make_box(name, sz, (px, py, oz + cv + dv), col)
        hz.assign(obj, mats["trim"])
        out.append(obj)

    for i in range(4):
        place((i + 0.5) / 4.0 * ow - ow * 0.5, 0.0, bar, oh * 0.94)
    for j in range(2):
        place(0.0, (j + 0.5) / 2.0 * oh - oh * 0.5, ow * 0.94, bar)
    return out


def _arched_plate(name, width, height, thickness, niche_w, spring_z,
                  y_front, col):
    """Tek nişli ön levha — `arched_panel`ın çeşme/kapı kısayolu."""
    return arched_panel(name, width, height, thickness,
                        (0.0, y_front + thickness * 0.5, 0.0), (1.0, 0.0),
                        (0.0, -1.0), spans=[(-niche_w * 0.5, niche_w * 0.5)],
                        sill_z=0.0, spring_z=spring_z, col=col)


def build_cesme(p, col, asset_name, textured=False):
    """Duvar çeşmesi. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []

    D = p.depth
    y_front = -D * 0.5
    nd = p.niche_depth

    plate = _arched_plate(f"{asset_name}_Plate", p.width, p.height, nd,
                          p.niche_w, p.spring_z, y_front, col)
    hz.assign(plate, mats["stone"])
    parts.append(plate)

    # Govde: levhanin arkasi. 1,5 cm bosluk birakilir — es duzlemde iki
    # yuzey cizilirse z-cakismasi (z-fighting) titrer.
    core_d = D - nd - 0.015
    core = hz.make_box(f"{asset_name}_Core", (p.width, core_d, p.height),
                       (0.0, y_front + nd + 0.015 + core_d * 0.5, p.height * 0.5), col)
    hz.assign(core, mats["stone"])
    parts.append(core)

    # Ayna tasi: nisin dibinde, lulenin bulundugu yuz.
    ayna = hz.make_box(f"{asset_name}_AynaTasi",
                       (p.niche_w - 0.16, 0.07, p.spring_z * 0.72),
                       (0.0, y_front + nd - 0.035, p.spring_z * 0.40), col)
    hz.assign(ayna, mats["cutstone"])
    parts.append(ayna)
    lule = hz.make_box(f"{asset_name}_Lule", (0.09, 0.16, 0.09),
                       (0.0, y_front + nd - 0.11, p.spring_z * 0.44), col)
    hz.assign(lule, mats["cutstone"])
    parts.append(lule)

    # Teknelik: sudan tasan yalak, cepheden disari cikar.
    if p.basin:
        bw = p.niche_w + 0.5
        tek = hz.make_box(f"{asset_name}_Teknelik", (bw, 0.52, 0.40),
                          (0.0, y_front - 0.14, 0.30), col)
        hz.assign(tek, mats["cutstone"])
        parts.append(tek)

    # Kitabe: kemer ustu yazi levhasi.
    if p.kitabe:
        _, rise = arch_points(p.niche_w * 0.5, p.spring_z)
        kz = p.spring_z + rise + (p.height - p.spring_z - rise) * 0.45
        kit_obj = hz.make_box(f"{asset_name}_Kitabe",
                              (p.niche_w + 0.25, 0.07, 0.42),
                              (0.0, y_front - 0.035, kz), col)
        hz.assign(kit_obj, mats["cutstone"])
        parts.append(kit_obj)

    # Silme (korniş): govdeyi bitiren tasma.
    silme = hz.make_box(f"{asset_name}_Silme",
                        (p.width + 0.26, D + 0.26, 0.20),
                        (0.0, 0.0, p.height + 0.10), col)
    hz.assign(silme, mats["cutstone"])
    parts.append(silme)

    # Duvar kanatlari + harpusta: cesmeyi duvara BAGLAR.
    if p.wings > 0.01:
        wt = D * 0.55
        for sgn in (-1, 1):
            wc = sgn * (p.width * 0.5 + p.wings * 0.5)
            wall = hz.make_box(f"{asset_name}_Kanat",
                               (p.wings, wt, p.wing_h),
                               (wc, y_front + nd + wt * 0.5 - 0.02, p.wing_h * 0.5),
                               col)
            hz.assign(wall, mats["stone"])
            parts.append(wall)
            hrp = hz.make_box(f"{asset_name}_KanatHarpusta",
                              (p.wings + 0.06, wt + 0.14, 0.13),
                              (wc, y_front + nd + wt * 0.5 - 0.02, p.wing_h + 0.065),
                              col)
            hz.assign(hrp, mats["cutstone"])
            parts.append(hrp)

    total_h = p.height + 0.20
    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    l1 = [_solid(f"{asset_name}_L1", (p.width, D, total_h),
                 (0.0, 0.0, total_h * 0.5), col, mats["stone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    ucx = hz.make_box(f"UCX_{asset_name}", (p.width, D, total_h),
                      (0.0, 0.0, total_h * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(total_h, 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(p.width + 2.0 * p.wings, 3),
                wall_depth=round(p.depth, 3),
                kind="cesme", palette=p.palette)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------------- dükkân

class DukkanParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 3.4)
        self.depth = kw.get("depth", 4.0)
        self.height = kw.get("height", 3.3)
        self.open_w = kw.get("open_w", 2.2)          # kepenk acikligi
        self.counter_z = kw.get("counter_z", 0.95)   # tezgah kotu
        self.awning = kw.get("awning", True)         # ust kanat kalkik mi
        self.upper_floor = kw.get("upper_floor", False)
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.open_w > self.width - 0.5:
            errs.append(f"open_w={self.open_w} cepheye ({self.width}) sigmaz")
        if self.counter_z > self.height * 0.5:
            errs.append(f"counter_z={self.counter_z} cok yuksek")
        if errs:
            raise ValueError("DukkanParams gecersiz: " + "; ".join(errs))
        return self


def build_dukkan(p, col, asset_name, textured=False):
    """Kepenkli dükkân (arasta birimi). Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []

    W, D, H = p.width, p.depth, p.height
    y_front = -D * 0.5
    t = 0.28
    ow = p.open_w
    lintel_z = H - 0.75

    # Yan ve arka duvarlar + tavan; on cephe ayri kurulur (aciklik var).
    for name, size, center in (
        ("Left", (t, D, H), (-W * 0.5 + t * 0.5, 0.0, H * 0.5)),
        ("Right", (t, D, H), (W * 0.5 - t * 0.5, 0.0, H * 0.5)),
        ("Back", (W, t, H), (0.0, D * 0.5 - t * 0.5, H * 0.5)),
    ):
        obj = hz.make_box(f"{asset_name}_{name}", size, center, col)
        hz.assign(obj, mats["plaster"])
        parts.append(obj)

    # On cephe: iki ayak + lento. Aciklik gercek bosluktur.
    for s in (-1, 1):
        pier_w = (W - ow) * 0.5
        pier = hz.make_box(f"{asset_name}_Pier", (pier_w, t, lintel_z),
                           (s * (ow + pier_w) * 0.5, y_front + t * 0.5, lintel_z * 0.5), col)
        hz.assign(pier, mats["plaster"])
        parts.append(pier)
    lentos = hz.make_box(f"{asset_name}_Lento", (W, t, H - lintel_z),
                         (0.0, y_front + t * 0.5, (H + lintel_z) * 0.5), col)
    hz.assign(lentos, mats["timber"])
    parts.append(lentos)

    # Ic karanlik: dukkan icinin okunmasini saglar.
    dark = hz.make_box(f"{asset_name}_Dark", (ow, 0.06, lintel_z - 0.05),
                       (0.0, y_front + t + 0.03, (lintel_z - 0.05) * 0.5), col)
    hz.assign(dark, mats["shadow"])
    parts.append(dark)

    # KEPENK ALT KANAT — asagi katlanip TEZGAH olur.
    #
    # Dukkani dukkan yapan sey budur. Kapali bir kutu cizmek onu depo yapar.
    tez_d = 0.75
    tez = hz.make_box(f"{asset_name}_Tezgah", (ow, tez_d, 0.07),
                      (0.0, y_front - tez_d * 0.5 + 0.05, p.counter_z), col)
    hz.assign(tez, mats["timber"])
    parts.append(tez)
    for s in (-1, 1):
        leg = hz.make_box(f"{asset_name}_TezgahAyak", (0.09, 0.09, p.counter_z),
                          (s * (ow * 0.5 - 0.12), y_front - tez_d + 0.14,
                           p.counter_z * 0.5), col)
        hz.assign(leg, mats["timber"])
        parts.append(leg)

    # KEPENK UST KANAT — yukari kalkip SUNDURMA olur, iki destekle tutulur.
    if p.awning:
        aw_d = 1.15
        aw = _slab(f"{asset_name}_Sundurma", ow + 0.2,
                   y_front + 0.02, y_front - aw_d,
                   lintel_z + 0.02, lintel_z + 0.62, 0.07, col)
        hz.assign(aw, mats["timber"])
        parts.append(aw)
        for s in (-1, 1):
            prop = hz.make_box(f"{asset_name}_Destek", (0.07, 0.07, 1.05),
                               (s * (ow * 0.5 - 0.1), y_front - aw_d + 0.12,
                                lintel_z - 0.2), col)
            hz.assign(prop, mats["timber"])
            parts.append(prop)

    # Ust kat (bazi dukkanlarda) ya da duz cati + sacak.
    if p.upper_floor:
        up = hz.make_box(f"{asset_name}_Ust", (W + 0.5, D, 2.5),
                         (0.0, -0.25, H + 1.25), col)
        hz.assign(up, mats["timber"])
        parts.append(up)
        top_z = H + 2.5
    else:
        top_z = H
    roof = hz.make_box(f"{asset_name}_Cati", (W + 0.7, D + 0.7, 0.24),
                       (0.0, 0.0, top_z + 0.12), col)
    hz.assign(roof, mats["roof"])
    parts.append(roof)
    total_h = top_z + 0.24

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (W, D, total_h), (0.0, 0.0, total_h * 0.5),
                 col, mats["plaster"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    ucx = hz.make_box(f"UCX_{asset_name}", (W, D, total_h),
                      (0.0, 0.0, total_h * 0.5), col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(total_h, 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(D, 3),
                kind="dukkan", palette=p.palette)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------- avlu ve şadırvan

class AvluParams(object):
    """Avlu duvarı ve kapısı."""

    def __init__(self, **kw):
        self.length = kw.get("length", 4.0)      # duvar parcasi uzunlugu
        self.height = kw.get("height", 1.85)
        self.thickness = kw.get("thickness", 0.45)
        self.gate = kw.get("gate", False)        # kemerli kapi mi
        self.gate_w = kw.get("gate_w", 1.6)
        self.gate_h = kw.get("gate_h", 3.4)
        self.spring_z = kw.get("spring_z", 2.0)
        self.palette = kw.get("palette", "default")


def build_avlu(p, col, asset_name, textured=False):
    """
    Avlu duvarı parçası — düz ya da **kemerli kapı**.

    Kapı, çeşmenin kemer kodunu yeniden kullanır: aynı iki merkezli sivri
    kemer. Aynı mahallede iki farklı kemer karakteri olması, gözün fark ettiği
    ama sebebini söyleyemediği türden bir tutarsızlık olurdu.

    Duvarın üstünde **harpuşta** (koruyucu taş) vardır: yağmuru duvardan
    uzaklaştırır ve duvarı "bitirir"; harpuştasız duvar kesilmiş gibi durur.
    """
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    L, T = p.length, p.thickness

    if p.gate:
        H = p.gate_h
        plate = _arched_plate(f"{asset_name}_Kapi", L, H, T, p.gate_w,
                              p.spring_z, -T * 0.5, col)
        hz.assign(plate, mats["stone"])
        parts.append(plate)
        top = H
    else:
        H = p.height
        wall = hz.make_box(f"{asset_name}_Duvar", (L, T, H), (0.0, 0.0, H * 0.5), col)
        hz.assign(wall, mats["stone"])
        parts.append(wall)
        top = H

    harpusta = hz.make_box(f"{asset_name}_Harpusta", (L, T + 0.16, 0.14),
                           (0.0, 0.0, top + 0.07), col)
    hz.assign(harpusta, mats["cutstone"])
    parts.append(harpusta)
    total_h = top + 0.14

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (L, T, total_h), (0.0, 0.0, total_h * 0.5),
                 col, mats["stone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (L, T, total_h),
                      (0.0, 0.0, total_h * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(total_h, 3), pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(L, 3), wall_depth=round(T, 3),
                kind="avlu_kapi" if p.gate else "avlu_duvar", palette=p.palette)
    return lod0, lod1, ucx, info


class SadirvanParams(object):
    def __init__(self, **kw):
        self.radius = kw.get("radius", 1.75)
        self.basin_h = kw.get("basin_h", 0.95)
        self.post_h = kw.get("post_h", 2.7)
        self.posts = kw.get("posts", 8)
        self.palette = kw.get("palette", "default")


def build_sadirvan(p, col, asset_name, textured=False):
    """
    Şadırvan — avlunun ortasındaki abdest çeşmesi.

    Sekizgen tekne, çevresinde ahşap direkler, üstünde konik örtü. Sekizgen
    **bilerek** düz gölgelendirilir: şadırvanın karakteri o kırıklı yüzeydir,
    yuvarlatılırsa varil gibi durur.
    """
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    R = p.radius

    tekne = hz.make_tube(f"{asset_name}_Tekne", R, R * 0.97, p.basin_h,
                         (0.0, 0.0), 0.0, segments=8, smooth=False, col=col)
    hz.assign(tekne, mats["cutstone"])
    parts.append(tekne)
    ic = hz.make_tube(f"{asset_name}_Su", R * 0.86, R * 0.86, 0.06,
                      (0.0, 0.0), p.basin_h - 0.14, segments=8, smooth=False, col=col)
    hz.assign(ic, mats["shadow"])
    parts.append(ic)
    govde = hz.make_tube(f"{asset_name}_Govde", R * 0.30, R * 0.24, p.basin_h + 0.7,
                         (0.0, 0.0), 0.0, segments=8, smooth=False, col=col)
    hz.assign(govde, mats["cutstone"])
    parts.append(govde)

    ring = R + 0.55
    for i in range(p.posts):
        a = 2.0 * math.pi * i / p.posts
        px, py = ring * math.cos(a), ring * math.sin(a)
        post = hz.make_box(f"{asset_name}_Direk{i}", (0.16, 0.16, p.post_h),
                           (px, py, p.post_h * 0.5), col)
        hz.assign(post, mats["timber"])
        parts.append(post)

    tabla = hz.make_tube(f"{asset_name}_Hatil", ring + 0.28, ring + 0.28, 0.18,
                         (0.0, 0.0), p.post_h, segments=p.posts, smooth=False, col=col)
    hz.assign(tabla, mats["trim"])
    parts.append(tabla)
    ortu = hz.make_tube(f"{asset_name}_Ortu", ring + 0.42, 0.0, 1.25,
                        (0.0, 0.0), p.post_h + 0.18, segments=p.posts,
                        smooth=False, col=col)
    hz.assign(ortu, mats["roof"])
    parts.append(ortu)
    total_h = p.post_h + 0.18 + 1.25

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (ring * 2, ring * 2, total_h),
                 (0.0, 0.0, total_h * 0.5), col, mats["cutstone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (R * 2, R * 2, p.basin_h),
                      (0.0, 0.0, p.basin_h * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(total_h, 3), pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round((ring + 0.42) * 2, 3),
                wall_depth=round((ring + 0.42) * 2, 3),
                kind="sadirvan", palette=p.palette)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------------ yardımcı

def _slab(name, width, y_high, y_low, z_high, z_low, thick, col):
    """Eğimli ince levha (sundurma / kepenk kanadı)."""
    hw = width * 0.5
    bm = bmesh.new()
    top = [(-hw, y_high, z_high), (hw, y_high, z_high),
           (hw, y_low, z_low), (-hw, y_low, z_low)]
    vt = [bm.verts.new(v) for v in top]
    vb = [bm.verts.new((x, y, z - thick)) for (x, y, z) in top]
    bm.verts.ensure_lookup_table()
    bm.faces.new(vt)
    bm.faces.new(list(reversed(vb)))
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((vt[i], vt[j], vb[j], vb[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.mesh_from_bmesh(name, bm, col)


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj
