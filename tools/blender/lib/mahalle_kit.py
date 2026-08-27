"""
Hezarfen: 1632 — Mahallenin üç küçük yapısı (plan Faz 2b).

Üçü de küçüktür ve üçü de mahallenin **neden orada olduğunu** anlatır.

## Türbe — mahalleye adını veren yapı

Osmanlı mahallesi çoğu zaman bir vakfın etrafında kurulur ve vakfı kuran kişi
kendi yaptırdığı mescidin hazîresine gömülür. Mahallenin adı da genellikle
odur. Türbe bu yüzden mezar taşı değil **yapı**dır: sekizgen kâgir gövde,
kurşun kubbe, her yüzde şebekeli pencere.

Sekizgen tesadüf değil: kare plandan kubbeye geçmenin en kısa yolu köşeleri
pahlamaktır ve sekizgen o geçişin kendisidir. Yapı zaten kubbenin kaidesi
olduğu için ara bir kasnak gerekmez.

## Sıbyan mektebi — havada duran tek oda

Mahalle mektebi tek odadır ve neredeyse her zaman **yükseltilir**: altı ya
dükkân, ya sarnıç, ya çeşmedir; üstü ders odası. Sebebi ekonomiktir (vakıf
geliri alttan gelir) ama sonucu mimarîdir — mektep sokakta hep bir kat yukarıda
durur ve dışarıdan taş bir merdivenle çıkılır. Merdiveni silmek yapıyı küçük
bir mescide çevirir.

## Kahvehane — 1632'nin zaman işareti

Kahvehane bu şehirde 1550'lerde açıldı ve 1633 Eylül'ünde IV. Murad tarafından
yasaklanıp yıktırıldı (PLAN.md §7.1; RESEARCH.md §5). Oyun **1632**'de geçtiği
için kahvehane AÇIKtır — ve tam bu yüzden oyunun tek gerçek zaman işaretidir:
bir yıl sonra aynı yerde durmayacak tek yapı odur.

Mimarî olarak anıt değildir: dükkân ölçeğinde, ahşap cepheli, sokağa **geniş
saçak** ve önünde taş **seki** ile açılan bir oda. Kahveyi kahvehane yapan şey
içerisi değil, o sekidir — oturulan yer sokaktır. Arkada ocak ve bacası.

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z).
"""

import math

import bmesh

import hz_blender as hz
import ottoman_kit as kit
import street_kit as sk


def _put(parts, obj, mat):
    hz.assign(obj, mat)
    parts.append(obj)
    return obj


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj


def oriented_box(name, su, sn, sz, center, u_axis, n_axis, col=None):
    """
    Eksene hizalı OLMAYAN kutu — `(u, n, z)` çerçevesinde kurulur.

    `hz.make_box` yalnızca dünya eksenlerine hizalı kutu verir; sekizgen
    türbenin yüzleri 45°'lik açılarda durduğu için oradaki söve, şebeke ve
    karanlık levha eksene hizalı kutuyla yapılamaz — yamuk durur ve duvarı
    deler. Kitin geri kalanındaki `_put_shadow`/`iron_grille` yardımcıları
    `abs(u_axis[0]) > 0.5` diye BAKARAK eksen seçiyor; bu, çapraz yüzlerde
    sessizce yanlış cevap veren bir varsayımdır. Burada varsayım yok.
    """
    U = hz.Vector((u_axis[0], u_axis[1], 0.0)).normalized()
    N = hz.Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    Z = hz.Vector((0.0, 0.0, 1.0))
    C = hz.Vector(center)
    bm = bmesh.new()
    v = {}
    for a in (-1, 1):
        for b in (-1, 1):
            for c in (-1, 1):
                v[(a, b, c)] = bm.verts.new(
                    C + U * (a * su * 0.5) + N * (b * sn * 0.5) + Z * (c * sz * 0.5))
    for f in (((-1, -1, -1), (1, -1, -1), (1, 1, -1), (-1, 1, -1)),
              ((-1, -1, 1), (-1, 1, 1), (1, 1, 1), (1, -1, 1)),
              ((-1, -1, -1), (-1, -1, 1), (1, -1, 1), (1, -1, -1)),
              ((-1, 1, -1), (1, 1, -1), (1, 1, 1), (-1, 1, 1)),
              ((-1, -1, -1), (-1, 1, -1), (-1, 1, 1), (-1, -1, 1)),
              ((1, -1, -1), (1, -1, 1), (1, 1, 1), (1, 1, -1))):
        bm.faces.new([v[k] for k in f])
    bm.normal_update()
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def _face_shadow(parts, mats, col, name, ow, oh, center, u_axis, n_axis, thick):
    """Açıklığın arkasındaki karanlık levha — boşluk boşluk gibi okunsun diye."""
    N = hz.Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    c = hz.Vector(center) - N * (thick * 0.5 - 0.04)
    _put(parts, oriented_box(f"{name}_Karanlik", ow, 0.05, oh, c,
                             u_axis, n_axis, col), mats["shadow"])


def _face_grille(parts, mats, col, name, ow, oh, center, u_axis, n_axis, thick,
                 bars=3, rails=1):
    """Dövme demir şebeke — açıklığın dış yüzüne yakın."""
    N = hz.Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    U = hz.Vector((u_axis[0], u_axis[1], 0.0)).normalized()
    base = hz.Vector(center) + N * (thick * 0.5 - 0.07)
    b = 0.030
    for i in range(bars):
        du = (i + 0.5) / bars * ow - ow * 0.5
        _put(parts, oriented_box(f"{name}_Sebeke", b, b, oh * 0.94,
                                 base + U * du, u_axis, n_axis, col),
             mats["trim"])
    for j in range(rails):
        dv = (j + 0.5) / rails * oh - oh * 0.5
        _put(parts, oriented_box(f"{name}_Sebeke", ow * 0.94, b, b,
                                 base + hz.Vector((0.0, 0.0, dv)),
                                 u_axis, n_axis, col), mats["trim"])


def _bay_spans(length, bays, opening_w):
    """Uzunluk boyunca `bays` adet eşit açıklığın (u0, u1) listesi."""
    pitch = length / bays
    half = opening_w * 0.5
    return [(-length * 0.5 + (i + 0.5) * pitch - half,
             -length * 0.5 + (i + 0.5) * pitch + half) for i in range(bays)]


def _shed(name, u0, u1, n_a, z_a, n_b, z_b, thick, origin,
          u_axis, n_axis, col):
    """
    Tek eğimli levha (çatı/saçak) — `(u, n, z)` çerçevesinde.

    İki kenar **çift olarak** verilir: `n_a` uzaklığında kot `z_a`, `n_b`
    uzaklığında kot `z_b`. `n` her zaman `n_axis` YÖNÜNDE ölçülür, yani
    dışarı doğru pozitiftir.

    Bu imza ilk yazımdaki `(n_near, n_far, z_near, z_far)`nin yerine geçti:
    orada uzaklık ile kot ayrı listelerdeydi ve çağıran yeri karıştırdı —
    `n_axis` zaten yönü taşırken bir de uzaklığa işaret çarpıldı, iki çatı
    levhası aynı tarafa düştü ve biri yok oldu. Çifti bitişik yazmak o hatayı
    imzada imkânsız kılar.
    """
    U = hz.Vector((u_axis[0], u_axis[1], 0.0)).normalized()
    N = hz.Vector((n_axis[0], n_axis[1], 0.0)).normalized()
    O = hz.Vector(origin)

    def p(u, n, z):
        return O + U * u + N * n + hz.Vector((0.0, 0.0, z))

    top = [p(u0, n_b, z_b), p(u1, n_b, z_b),
           p(u1, n_a, z_a), p(u0, n_a, z_a)]
    bm = bmesh.new()
    vt = [bm.verts.new(v) for v in top]
    vb = [bm.verts.new(v - hz.Vector((0.0, 0.0, thick))) for v in top]
    bm.verts.ensure_lookup_table()
    bm.faces.new(vt)
    bm.faces.new(list(reversed(vb)))
    for i in range(4):
        j = (i + 1) % 4
        bm.faces.new((vt[i], vt[j], vb[j], vb[i]))
    bm.normal_update()
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def _gable_end(name, span, rise, x_center, thickness, base_z, col):
    """Alınlık duvarı — çatı eğimini izleyen üçgen kâgir uç (Y ekseninde)."""
    hs, ht = span * 0.5, thickness * 0.5
    bm = bmesh.new()
    faces = []
    for n in (-ht, ht):
        tri = [bm.verts.new((x_center + n, -hs, base_z)),
               bm.verts.new((x_center + n, hs, base_z)),
               bm.verts.new((x_center + n, 0.0, base_z + rise))]
        faces.append((tri, bm.faces.new(tri)))
    (a, _), (b, _) = faces
    for i in range(3):
        j = (i + 1) % 3
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.normal_update()
    return hz.ensure_outward(hz.mesh_from_bmesh(name, bm, col))


def _alem(parts, mats, col, name, x, y, z):
    """Alem — kubbenin tepesindeki madenî tepelik."""
    _put(parts, hz.make_tube(f"{name}_AlemMil", 0.055, 0.045, 0.95, (x, y), z,
                             segments=6, smooth=False, col=col), mats["lead"])
    _put(parts, hz.make_dome(f"{name}_AlemTop", 0.15, 0.20, (x, y), z + 0.80,
                             segments=8, rings=3, col=col), mats["lead"])


# -------------------------------------------------------------------- türbe

class TurbeParams(object):
    def __init__(self, **kw):
        self.sides = kw.get("sides", 8)
        self.apothem = kw.get("apothem", 3.10)     # yuz ortasina uzaklik
        self.wall_t = kw.get("wall_t", 0.55)
        self.wall_h = kw.get("wall_h", 4.60)
        self.dome_h = kw.get("dome_h", 2.60)
        self.door_w = kw.get("door_w", 1.15)
        self.door_spring = kw.get("door_spring", 2.15)
        self.win_w = kw.get("win_w", 0.78)
        self.win_sill = kw.get("win_sill", 1.05)
        self.win_spring = kw.get("win_spring", 2.05)
        self.acik = kw.get("acik", False)          # baldaken (acik) turbe mi
        self.palette = kw.get("palette", "default")

    @property
    def face_w(self):
        """Dış yüzeyde bir kenarın uzunluğu — köşeler KAPANSIN diye dıştan."""
        return 2.0 * (self.apothem + self.wall_t * 0.5) \
            * math.tan(math.pi / self.sides)

    def validate(self):
        errs = []
        if self.acik:
            # ACIK (baldaken) turbe DORT AYAKLI da olabilir; kapali turbenin
            # alti/sekizgen kurali onun duvarlarina aittir, sutunlarina degil.
            if self.sides not in (4, 6, 8):
                errs.append(f"sides={self.sides} — acik turbe 4, 6 ya da 8 "
                            "ayakli olur")
        elif self.sides not in (6, 8):
            errs.append(f"sides={self.sides} — turbe alti ya da sekizgendir")
        if self.acik:
            # Acik turbede duvar yok: kapi ve pencere denetimleri KONU DISI.
            # Ilk yazimda burasi kosulsuzdu ve dar bir baldaken "kapi sigmaz"
            # diye reddedilirdi — olmayan bir kapi icin.
            if errs:
                raise ValueError("TurbeParams gecersiz: " + "; ".join(errs))
            return self
        w = self.face_w
        if self.door_w > w - 0.9:
            errs.append(f"door_w={self.door_w}, {w:.2f} m'lik yuze sigmaz")
        _, rise = sk.arch_points(self.door_w * 0.5, self.door_spring)
        if self.door_spring + rise > self.wall_h - 0.4:
            errs.append(f"kapi kemeri tepesi {self.door_spring + rise:.2f} m, "
                        f"duvar {self.wall_h} m")
        if errs:
            raise ValueError("TurbeParams gecersiz: " + "; ".join(errs))
        return self

    def face_frame(self, k):
        """`k`. yüzün (merkez, u_axis, n_axis). Yüz 0 ÖNdür (−Y)."""
        th = -math.pi * 0.5 + 2.0 * math.pi * k / self.sides
        c = (self.apothem * math.cos(th), self.apothem * math.sin(th))
        return c, (-math.sin(th), math.cos(th)), (math.cos(th), math.sin(th))


def build_turbe(p, col, asset_name, textured=False):
    """Sekizgen kâgir türbe, kurşun kubbeli. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    if p.acik:
        return _build_acik_turbe(p, col, asset_name, mats, tex_sizes)
    parts = []
    a, t, H, w = p.apothem, p.wall_t, p.wall_h, p.face_w
    R = a / math.cos(math.pi / p.sides)            # kose yaricapi

    # Subasman: sekizgeni izleyen alcak kursu.
    _put(parts, hz.make_tube(f"{asset_name}_Subasman", (R + 0.30), (R + 0.26),
                             0.34, (0.0, 0.0), 0.0, segments=p.sides,
                             smooth=False, col=col), mats["cutstone"])

    # --- GOVDE: her yuz ayri DELIKLI panel.
    #
    # Paneller dis kenar uzunluguyla kurulur, yani komsu paneller kosede
    # BIRBIRINE GIRER. Ic kenar uzunluguyla kurulsalardi kose disinda V
    # bicimli bir yarik kalirdi ve isik oradan sizardi.
    for k in range(p.sides):
        (cx, cy), u_ax, n_ax = p.face_frame(k)
        if k == 0:
            _put(parts, sk.arched_panel(
                f"{asset_name}_Yuz{k}", w, H, t, (cx, cy, 0.34), u_ax, n_ax,
                spans=[(-p.door_w * 0.5, p.door_w * 0.5)], sill_z=0.0,
                spring_z=p.door_spring, col=col), mats["stone"])
            _, rise = sk.arch_points(p.door_w * 0.5, p.door_spring)
            oh = p.door_spring + rise * 0.6
            _face_shadow(parts, mats, col, f"{asset_name}_Kapi", p.door_w, oh,
                         (cx, cy, 0.34 + oh * 0.5), u_ax, n_ax, t)
            continue

        _put(parts, sk.arched_panel(
            f"{asset_name}_Yuz{k}", w, H, t, (cx, cy, 0.34), u_ax, n_ax,
            spans=[(-p.win_w * 0.5, p.win_w * 0.5)], sill_z=p.win_sill,
            spring_z=p.win_spring, col=col), mats["stone"])
        _, rise = sk.arch_points(p.win_w * 0.5, p.win_spring)
        oh = (p.win_spring - p.win_sill) + rise * 0.6
        ctr = (cx, cy, 0.34 + p.win_sill + oh * 0.5)
        _face_shadow(parts, mats, col, f"{asset_name}_Pencere{k}",
                     p.win_w, oh, ctr, u_ax, n_ax, t)
        _face_grille(parts, mats, col, f"{asset_name}_Pencere{k}",
                     p.win_w, oh, ctr, u_ax, n_ax, t)

    # --- SACAK KUSAGI: kubbe kaidesi. Kose kacaklarini da ortur.
    z_cor = 0.34 + H
    for k in range(p.sides):
        (cx, cy), u_ax, n_ax = p.face_frame(k)
        cw = 2.0 * (a + t * 0.5 + 0.16) * math.tan(math.pi / p.sides)
        _put(parts, oriented_box(f"{asset_name}_Silme", cw, t + 0.32, 0.28,
                                 (cx, cy, z_cor + 0.14), u_ax, n_ax, col),
             mats["cutstone"])

    # --- KUBBE: kursun. Turbenin siluetini tasiyan sey.
    dz = z_cor + 0.28
    dr = a + t * 0.5 + 0.06
    _put(parts, hz.make_dome(f"{asset_name}_Kubbe", dr, p.dome_h, (0.0, 0.0),
                             dz, segments=20, rings=7, col=col), mats["lead"])
    _alem(parts, mats, col, asset_name, 0.0, 0.0, dz + p.dome_h - 0.05)

    # --- KAPI DONANIMI (yuz 0 eksene hizalidir, duz kutu yeter)
    y0 = -(a + t * 0.5)
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_KapiSove", (0.26, 0.16, 2.95),
                                (s * (p.door_w * 0.5 + 0.13), y0 - 0.08, 1.81),
                                col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Kitabe", (p.door_w + 0.5, 0.12, 0.46),
                            (0.0, y0 - 0.10, 3.42), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Esik", (p.door_w + 1.1, 0.75, 0.34),
                            (0.0, y0 - 0.36, 0.17), col), mats["cutstone"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (2 * R, 2 * R, H + 0.34),
                 (0.0, 0.0, (H + 0.34) * 0.5), col, mats["stone"]),
          _solid(f"{asset_name}_L1d", (2 * dr * 0.92, 2 * dr * 0.92, p.dome_h),
                 (0.0, 0.0, dz + p.dome_h * 0.5), col, mats["lead"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (2 * R, 2 * R, H + 0.34),
                      (0.0, 0.0, (H + 0.34) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(2 * R, 3), wall_depth=round(2 * R, 3),
                kind="turbe", sides=p.sides, palette=p.palette)
    return lod0, lod1, ucx, info



def _build_acik_turbe(p, col, asset_name, mats, tex_sizes):
    """
    **Açık (baldaken) türbe**: duvar yok, sütun var.

    ## Neden ayrı bir dal

    `TurbeParams.acik` bayrağı **vardı ama hiçbir şey yapmıyordu** — kapalı
    türbe kurulup "açık" diye kataloglanıyordu. Üreticinin kendi denetimi
    (`if not p.acik: raise`) bunu geçirdi, çünkü bayrağın değerine bakıyordu,
    yapının kendisine değil. Bayrak okunuyor sanmak, en sessiz hata türü.

    ## Biçim nereden geliyor

    Sanduka üstünde, **sütunlara oturan** kurşun kubbe; yanları açık, altta
    alçak bir korkuluk. Sütun sayısı `sides`tir ve Hüdâyî türbesi için
    **dört**: bugünkü (1855-56) yapının kubbesi "dört mermer sütun üzerine
    oturur" ve o baldaken çekirdek, kapatılmadan önceki hâlin izidir. Geri
    kalan oranlar **D3**'tür — 1632 hâlinin ölçülü çizimi yok.
    """
    parts = []
    a = p.apothem                       # sutun merkezlerine yariçap
    R = a * math.sqrt(2.0) if p.sides == 4 else a / math.cos(math.pi / p.sides)
    col_r = 0.19
    H = p.wall_h                        # sutun yuksekligi

    # Set: turbe zeminden bir basamak yukseridir.
    _put(parts, hz.make_tube(f"{asset_name}_Set", R + 0.85, R + 0.80, 0.36,
                             (0.0, 0.0), 0.0, segments=max(8, p.sides * 2),
                             smooth=False, col=col), mats["cutstone"])
    z0 = 0.36

    # SUTUNLAR — mermer. Kaynak "dort MERMER sutun" der.
    pts = []
    for k in range(p.sides):
        th = math.pi / 4.0 + 2.0 * math.pi * k / p.sides
        cx, cy = R * math.cos(th), R * math.sin(th)
        pts.append((cx, cy))
        _put(parts, hz.make_tube(f"{asset_name}_Sutun{k}", col_r, col_r * 0.93,
                                 H, (cx, cy), z0, segments=12, col=col),
             mats["marble"])
        _put(parts, hz.make_box(f"{asset_name}_Baslik{k}",
                                (col_r * 2.7, col_r * 2.7, col_r * 1.1),
                                (cx, cy, z0 + H + col_r * 0.55), col),
             mats["marble"])
        _put(parts, hz.make_box(f"{asset_name}_Kaide{k}",
                                (col_r * 2.9, col_r * 2.9, 0.26),
                                (cx, cy, z0 + 0.13), col), mats["cutstone"])

    # HATIL: sutunlari baglayan kusak. Kubbe buna oturur.
    z_top = z0 + H + col_r * 1.1
    for k in range(p.sides):
        x0, y0 = pts[k]
        x1, y1 = pts[(k + 1) % p.sides]
        mx, my = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        span = math.hypot(x1 - x0, y1 - y0)
        ang = math.atan2(y1 - y0, x1 - x0)
        # Cerceve SAG ELLI olmali. Ilk yazimda n_ax = (-sin, cos) idi ve
        # `ensure_outward` sekiz kabugu birden cevirdi (isaretli hacim
        # negatif). Ag yakaladi diye neden birakilmaz: bir dahaki kutuda
        # ag olmayabilir.
        u_ax = (math.cos(ang), math.sin(ang))
        n_ax = (math.sin(ang), -math.cos(ang))
        _put(parts, oriented_box(f"{asset_name}_Hatil{k}", span, 0.30, 0.42,
                                 (mx, my, z_top + 0.21), u_ax, n_ax, col),
             mats["cutstone"])
        # KORKULUK: acik turbenin yanlari bos degil, alcak sebekelidir.
        _put(parts, oriented_box(f"{asset_name}_Korkuluk{k}", span, 0.16, 0.95,
                                 (mx, my, z0 + 0.475), u_ax, n_ax, col),
             mats["marble"])

    # KUBBE: kursun, hatilin ustunde.
    dz = z_top + 0.42
    dr = R * 1.06
    _put(parts, hz.make_dome(f"{asset_name}_Kubbe", dr, p.dome_h, (0.0, 0.0),
                             dz, segments=20, rings=7, col=col), mats["lead"])
    _alem(parts, mats, col, asset_name, 0.0, 0.0, dz + p.dome_h - 0.05)

    # SANDUKA + bas tasi: turbeyi turbe yapan sey burasi.
    _put(parts, hz.make_box(f"{asset_name}_Sanduka", (0.95, 2.15, 0.62),
                            (0.0, 0.0, z0 + 0.31), col), mats["marble"])
    _put(parts, hz.make_box(f"{asset_name}_Sanduka2", (0.66, 1.85, 0.34),
                            (0.0, 0.0, z0 + 0.62 + 0.17), col), mats["marble"])
    _put(parts, hz.make_tube(f"{asset_name}_BasTasi", 0.11, 0.10, 1.15,
                             (0.0, 1.18), z0 + 0.62, segments=10, col=col),
         mats["marble"])
    # Sarik: Celveti seyhinin bas tasi sarikli olur.
    _put(parts, hz.make_dome(f"{asset_name}_Sarik", 0.17, 0.30, (0.0, 1.18),
                             z0 + 0.62 + 1.15, segments=12, rings=4, col=col),
         mats["marble"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (2 * R, 2 * R, H + z0),
                 (0.0, 0.0, (H + z0) * 0.5), col, mats["marble"]),
          _solid(f"{asset_name}_L1d", (2 * dr * 0.92, 2 * dr * 0.92, p.dome_h),
                 (0.0, 0.0, dz + p.dome_h * 0.5), col, mats["lead"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (2 * (R + 0.85), 2 * (R + 0.85),
                                            dz + p.dome_h),
                      (0.0, 0.0, (dz + p.dome_h) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(2 * R, 3), wall_depth=round(2 * R, 3),
                kind="turbe", sides=p.sides, acik=True, walls=False,
                columns=p.sides, palette=p.palette)
    return lod0, lod1, ucx, info


# ------------------------------------------------------------ sıbyan mektebi

class MektepParams(object):
    def __init__(self, **kw):
        self.room = kw.get("room", 5.60)           # ic kenar (kare)
        self.wall_t = kw.get("wall_t", 0.50)
        self.wall_h = kw.get("wall_h", 3.50)
        self.base_h = kw.get("base_h", 1.70)       # alt yapi yuksekligi
        self.dome_h = kw.get("dome_h", 1.95)
        self.door_w = kw.get("door_w", 1.05)
        self.win_w = kw.get("win_w", 0.82)
        self.win_sill = kw.get("win_sill", 0.95)
        self.win_spring = kw.get("win_spring", 1.95)
        self.steps = kw.get("steps", 7)
        # YAZLIK bolum: kubbeli ACIK EYVAN. Mahalle mektebinde yoktur
        # (tek oda yeter); vakif mektebinde kislik oda + yazlik eyvan
        # ikilisi tipiktir ve kaynak Uskudar Mihrimah mektebi icin
        # tam bunu soyler: "kubbeli bir dershane ve kubbeli acik
        # eyvan; kislik ve yazlik bolumleri vardir".
        self.eyvan = kw.get("eyvan", False)
        self.eyvan_d = kw.get("eyvan_d", 3.60)
        self.palette = kw.get("palette", "default")

    @property
    def outer(self):
        return self.room + 2.0 * self.wall_t

    def validate(self):
        errs = []
        if self.base_h < 1.2:
            errs.append(f"base_h={self.base_h} — mektep YUKSELTILIR; "
                        f"alt yapi bir kat olmali")
        if self.win_w * 2 > self.outer - 2.2:
            errs.append(f"win_w={self.win_w} iki pencere olarak sigmaz")
        _, rise = sk.arch_points(self.win_w * 0.5, self.win_spring)
        if self.win_spring + rise > self.wall_h - 0.3:
            errs.append(f"pencere kemeri {self.win_spring + rise:.2f} m, "
                        f"duvar {self.wall_h} m")
        if self.steps < 5:
            errs.append(f"steps={self.steps} — {self.base_h} m'ye cikmaz")
        if errs:
            raise ValueError("MektepParams gecersiz: " + "; ".join(errs))
        return self


def build_mektep(p, col, asset_name, textured=False):
    """Yükseltilmiş tek odalı sıbyan mektebi. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, t, H, B = p.outer, p.wall_t, p.wall_h, p.base_h

    # --- ALT YAPI: kagir kursu. Ustundeki oda bunun uzerinde durur.
    _put(parts, hz.make_box(f"{asset_name}_AltYapi", (W + 0.55, W + 0.55, B),
                            (0.0, 0.0, B * 0.5), col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_AltSilme", (W + 0.85, W + 0.85, 0.22),
                            (0.0, 0.0, B + 0.11), col), mats["cutstone"])
    # Alt yapinin on yuzunde CESME nisi: mektebin altinda gelir getiren ya da
    # hayrat olan bir birim olurdu; bosluk birakmak yapiyi "kaide uzerinde
    # kutu" yapardi.
    #
    # Nis ORTADA DEGIL: orta, kapinin onundeki sahanligin arkasina dusuyor ve
    # ilk denemede tamamen gorunmez kaldi. Merdiven cepheyi ortadan isgal
    # ettigi icin cesme yana kayar — donemin yapilarinda da boyledir.
    yb = -(W + 0.55) * 0.5
    nx = (W + 0.55) * 0.5 - 1.15
    _put(parts, hz.make_box(f"{asset_name}_NisKaranlik", (1.05, 0.10, 1.15),
                            (nx, yb + 0.06, 0.62), col), mats["shadow"])
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_NisSove", (0.22, 0.14, 1.35),
                                (nx + s * 0.63, yb - 0.05, 0.68), col),
             mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_NisKemer", (1.55, 0.14, 0.30),
                            (nx, yb - 0.05, 1.50), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_CesmeYalak", (1.30, 0.62, 0.42),
                            (nx, yb - 0.31, 0.21), col), mats["cutstone"])

    z0 = B + 0.22
    half = W * 0.5

    # --- ODA: on cephede kapi, iki yanda ikiser pencere, arka sagir.
    _put(parts, sk.arched_panel(
        f"{asset_name}_OnCephe", W, H, t, (0.0, -half + t * 0.5, z0),
        (1.0, 0.0), (0.0, -1.0),
        spans=[(-p.door_w * 0.5, p.door_w * 0.5)], sill_z=0.0,
        spring_z=2.05, col=col), mats["plaster"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik",
                            (p.door_w, 0.06, 2.05),
                            (0.0, -half + t - 0.02, z0 + 1.02), col),
         mats["shadow"])

    spans = _bay_spans(W - 1.9, 2, p.win_w)
    for sx in (-1, 1):
        _put(parts, sk.arched_panel(
            f"{asset_name}_YanCephe", W, H, t,
            (sx * (half - t * 0.5), 0.0, z0), (0.0, 1.0), (float(sx), 0.0),
            spans=spans, sill_z=p.win_sill, spring_z=p.win_spring, col=col),
            mats["plaster"])
        _, rise = sk.arch_points(p.win_w * 0.5, p.win_spring)
        oh = (p.win_spring - p.win_sill) + rise * 0.6
        for u0, u1 in spans:
            cu = (u0 + u1) * 0.5
            ctr = (sx * (half - t * 0.5), cu, z0 + p.win_sill + oh * 0.5)
            _face_shadow(parts, mats, col, f"{asset_name}_Pencere",
                         p.win_w, oh, ctr, (0.0, 1.0), (float(sx), 0.0), t)
            _face_grille(parts, mats, col, f"{asset_name}_Pencere",
                         p.win_w, oh, ctr, (0.0, 1.0), (float(sx), 0.0), t)
    _put(parts, hz.make_box(f"{asset_name}_ArkaCephe", (W, t, H),
                            (0.0, half - t * 0.5, z0 + H * 0.5), col),
         mats["plaster"])

    # --- SACAK + KUBBE. Kubbe kareden sekizgene gecerek oturur.
    zc = z0 + H
    _put(parts, hz.make_box(f"{asset_name}_Sacak", (W + 0.80, W + 0.80, 0.26),
                            (0.0, 0.0, zc + 0.13), col), mats["cutstone"])
    _put(parts, hz.make_tube(f"{asset_name}_Kasnak", W * 0.50, W * 0.48, 0.42,
                             (0.0, 0.0), zc + 0.26, segments=8, smooth=False,
                             col=col), mats["cutstone"])
    dz = zc + 0.68
    _put(parts, hz.make_dome(f"{asset_name}_Kubbe", W * 0.485, p.dome_h,
                             (0.0, 0.0), dz, segments=18, rings=6, col=col),
         mats["lead"])
    _alem(parts, mats, col, asset_name, 0.0, 0.0, dz + p.dome_h - 0.05)

    # --- YAZLIK EYVAN: uc yani kapali, ONU ACIK, kubbeli.
    #
    # Eyvan bir oda degil bir NIS'tir: onu tamamen aciktir. Kapatmak onu
    # ikinci bir odaya cevirir ve "kislik/yazlik" ayrimini siler.
    if p.eyvan:
        ed = p.eyvan_d
        ey_y = half + ed * 0.5                 # arka cepheye bitisik
        eh = H * 0.92
        for sx in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_EyvanPaye",
                                    (t, ed, eh),
                                    (sx * (half - t * 0.5), ey_y, z0 + eh * 0.5),
                                    col), mats["plaster"])
        # ARKA duvar YOK: mektebin kendi arka cephesi eyvanin arkasidir.
        _put(parts, hz.make_box(f"{asset_name}_EyvanSacak",
                                (W + 0.60, ed + 0.60, 0.24),
                                (0.0, ey_y, z0 + eh + 0.12), col),
             mats["cutstone"])
        _put(parts, hz.make_box(f"{asset_name}_EyvanKemer",
                                (W, 0.32, 0.55),
                                (0.0, ey_y + ed * 0.5 - 0.16,
                                 z0 + eh - 0.28), col), mats["plaster"])
        _put(parts, hz.make_box(f"{asset_name}_EyvanZemin",
                                (W, ed, 0.30),
                                (0.0, ey_y, z0 - 0.15), col), mats["cutstone"])
        edz = z0 + eh + 0.24
        _put(parts, hz.make_dome(f"{asset_name}_EyvanKubbe",
                                 min(W, ed) * 0.50, p.dome_h * 0.62,
                                 (0.0, ey_y), edz, segments=16, rings=5,
                                 col=col), mats["lead"])

    # --- DIS MERDIVEN: mektebi mektep yapan ikinci sey.
    #
    # Cepheye PARALEL cikar ve kapinin onunde sahanliga varir; cepheye dik
    # cikan bir merdiven kapiyi kapatirdi.
    rise = (z0 - 0.0) / p.steps
    tread = 0.32
    run = p.steps * tread
    x_end = -p.door_w * 0.5 - 0.35
    y_st = -(W + 0.55) * 0.5 - 0.70
    for i in range(p.steps):
        z = rise * (i + 1)
        _put(parts, hz.make_box(f"{asset_name}_Basamak",
                                (tread, 1.30, z),
                                (x_end - run + (i + 0.5) * tread, y_st, z * 0.5),
                                col), mats["cutstone"])
    land_hw = (p.door_w + 1.5) * 0.5
    _put(parts, hz.make_box(f"{asset_name}_Sahanlik", (land_hw * 2, 1.30, z0),
                            (0.0, y_st, z0 * 0.5), col), mats["cutstone"])

    # Korkuluk EGIMI IZLER, düz değildir.
    #
    # İlk denemede tek parça, sabit kotlu bir duvardı: üst kotu 2,02 m'ye
    # çıkıyor, alttaki basamakların hepsi arkasında kalıyordu. Cepheden
    # bakınca merdiven diye bir şey görünmüyordu — mektebi mektep yapan ikinci
    # işaret, birincisinin arkasına saklanmıştı. Basamak başına bir parça,
    # sorunu ölçüyle değil biçimle çözer.
    for i in range(p.steps):
        z = rise * (i + 1)
        _put(parts, hz.make_box(f"{asset_name}_Korkuluk", (tread, 0.18, z + 0.52),
                                (x_end - run + (i + 0.5) * tread, y_st - 0.56,
                                 (z + 0.52) * 0.5), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Korkuluk",
                            (land_hw - x_end + 0.35, 0.18, z0 + 0.52),
                            ((x_end - 0.35 + land_hw) * 0.5, y_st - 0.56,
                             (z0 + 0.52) * 0.5), col), mats["cutstone"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    total = dz + p.dome_h
    l1 = [_solid(f"{asset_name}_L1", (W + 0.55, W + 0.55, z0),
                 (0.0, 0.0, z0 * 0.5), col, mats["stone"]),
          _solid(f"{asset_name}_L1r", (W, W, H), (0.0, 0.0, z0 + H * 0.5),
                 col, mats["plaster"]),
          _solid(f"{asset_name}_L1d", (W * 0.9, W * 0.9, p.dome_h),
                 (0.0, 0.0, dz + p.dome_h * 0.5), col, mats["lead"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W + 0.55, W + 0.55, z0 + H),
                      (0.0, 0.0, (z0 + H) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W + 0.55, 3), wall_depth=round(W + 0.55, 3),
                kind="mektep", floor_z=round(z0, 3), palette=p.palette,
                eyvan=bool(p.eyvan))
    return lod0, lod1, ucx, info


# ---------------------------------------------------------------- kahvehane

class KahvehaneParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 7.40)
        self.depth = kw.get("depth", 6.20)
        self.wall_t = kw.get("wall_t", 0.34)
        self.plinth = kw.get("plinth", 0.50)
        self.wall_h = kw.get("wall_h", 3.25)
        self.open_w = kw.get("open_w", 4.20)       # sokaga acilan genislik
        self.lintel_z = kw.get("lintel_z", 2.35)
        self.seki_d = kw.get("seki_d", 1.55)       # on seki derinligi
        self.seki_h = kw.get("seki_h", 0.45)
        self.eave = kw.get("eave", 2.30)           # sokaga tasan sacak
        self.roof_h = kw.get("roof_h", 1.55)
        self.ocak = kw.get("ocak", True)
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.open_w > self.width - 1.2:
            errs.append(f"open_w={self.open_w}, {self.width} m cepheye sigmaz")
        if self.lintel_z > self.wall_h - 0.6:
            errs.append(f"lintel_z={self.lintel_z} — ustunde kafes yeri kalmiyor")
        # Sekinin oturulabilir olmasi ölçülebilir bir sarttir: 0,38-0,50 m
        # disi sira degil ya basamak ya masadir.
        if not 0.36 <= self.seki_h <= 0.52:
            errs.append(f"seki_h={self.seki_h} — oturulacak yukseklik degil")
        if self.eave < self.seki_d + 0.4:
            errs.append(f"eave={self.eave} sekiyi ({self.seki_d} m) ortmuyor — "
                        f"yagmurda oturulamaz")
        if errs:
            raise ValueError("KahvehaneParams gecersiz: " + "; ".join(errs))
        return self


def build_kahvehane(p, col, asset_name, textured=False):
    """Ahşap cepheli kahvehane, geniş saçaklı. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, t = p.width, p.depth, p.wall_t
    z0, H = p.plinth, p.wall_h
    y_front = -D * 0.5

    _put(parts, hz.make_box(f"{asset_name}_Subasman", (W + 0.24, D + 0.24, z0),
                            (0.0, 0.0, z0 * 0.5), col), mats["stone"])
    for name, size, center in (
        ("Sol", (t, D, H), (-W * 0.5 + t * 0.5, 0.0, z0 + H * 0.5)),
        ("Sag", (t, D, H), (W * 0.5 - t * 0.5, 0.0, z0 + H * 0.5)),
        ("Arka", (W - 2 * t, t, H), (0.0, D * 0.5 - t * 0.5, z0 + H * 0.5)),
    ):
        _put(parts, hz.make_box(f"{asset_name}_{name}", size, center, col),
             mats["plaster"])

    # --- ON CEPHE: AHSAP dikme-kirisi. Kagir degil.
    #
    # Kahvehane duvarla degil DIREKle acilir; ictekiler sokagi, sokaktakiler
    # icerisini gorur. Kagir bir cephe koymak onu dukkana cevirirdi.
    ow = p.open_w
    pier = (W - ow) * 0.5
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_OnAyak", (pier, t, p.lintel_z),
                                (s * (ow + pier) * 0.5, y_front + t * 0.5,
                                 z0 + p.lintel_z * 0.5), col), mats["plaster"])
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_Direk", (0.16, 0.16, p.lintel_z),
                                (s * ow * 0.5, y_front + t * 0.5,
                                 z0 + p.lintel_z * 0.5), col), mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_Direk", (0.16, 0.16, p.lintel_z),
                            (0.0, y_front + t * 0.5, z0 + p.lintel_z * 0.5), col),
         mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_Lento", (W, t + 0.10, 0.22),
                            (0.0, y_front + t * 0.5, z0 + p.lintel_z + 0.11), col),
         mats["timber"])
    _put(parts, hz.make_box(f"{asset_name}_IcKaranlik", (ow - 0.2, 0.06,
                                                         p.lintel_z - 0.05),
                            (0.0, y_front + t + 0.04,
                             z0 + (p.lintel_z - 0.05) * 0.5), col), mats["shadow"])

    # Lentonun ustu: KAFES bandi. Isik girer, gorunmez olunmaz.
    kz = z0 + p.lintel_z + 0.22
    kh = z0 + H - kz
    if kh > 0.30:
        _put(parts, hz.make_box(f"{asset_name}_KafesArka", (W - 2 * t, 0.05, kh),
                                (0.0, y_front + t * 0.6, kz + kh * 0.5), col),
             mats["shadow"])
        n = max(6, int(round((W - 2 * t) / 0.32)))
        for i in range(n):
            x = -(W - 2 * t) * 0.5 + (i + 0.5) * (W - 2 * t) / n
            _put(parts, hz.make_box(f"{asset_name}_Kafes", (0.055, 0.06, kh),
                                    (x, y_front + t * 0.5, kz + kh * 0.5), col),
                 mats["trim"])

    # --- SEKI: kahvehanenin gercek odasi SOKAKTIR.
    _put(parts, hz.make_box(f"{asset_name}_Seki", (W + 0.24, p.seki_d, p.seki_h),
                            (0.0, y_front - p.seki_d * 0.5, p.seki_h * 0.5), col),
         mats["cutstone"])

    # --- CATI: alaturka besik, mahyasi X boyunca; egimler sokaga ve arkaya.
    zr = z0 + H
    ridge_z = zr + p.roof_h
    for sy in (-1, 1):
        _put(parts, _shed(f"{asset_name}_Cati", -(W * 0.5 + 0.55), W * 0.5 + 0.55,
                          0.0, ridge_z, D * 0.5 + 0.55, zr - 0.10, 0.14,
                          (0.0, 0.0, 0.0), (1.0, 0.0), (0.0, float(sy)), col),
             mats["roof"])
    # Alinlik DUVARdir, kiremit degil (ADR 0018): ucgen kagir uc.
    for sx in (-1, 1):
        _put(parts, _gable_end(f"{asset_name}_Alinlik", D, p.roof_h,
                               sx * (W * 0.5 - t * 0.5), t, zr, col),
             mats["plaster"])
    top = ridge_z

    # --- SUNDURMA: sekiyi ORTMEK zorunda (validate bunu olcer).
    #
    # Cati sacagi degil, ONUN ALTINDA ayri bir ahsap ortu — kahvehanenin
    # sokaga tasan kanadi budur ve iki direkle tutulur.
    ez = z0 + p.lintel_z + 0.75
    z_out = ez - 0.55
    _put(parts, _shed(f"{asset_name}_Sundurma", -(W * 0.5 + 0.45), W * 0.5 + 0.45,
                      -0.10, ez, p.eave, z_out, 0.09,
                      (0.0, y_front, 0.0), (1.0, 0.0), (0.0, -1.0), col),
         mats["timber"])
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_SundurmaDirek",
                                (0.13, 0.13, z_out),
                                (s * (W * 0.5 - 0.30), y_front - p.eave + 0.20,
                                 z_out * 0.5), col), mats["timber"])

    # --- OCAK ve BACA: kahve burada kavrulur, kaynatilir.
    if p.ocak:
        bx, by = W * 0.5 - 0.85, D * 0.5 - 0.85
        _put(parts, hz.make_box(f"{asset_name}_Baca", (0.90, 0.90, top + 1.30),
                                (bx, by, (top + 1.30) * 0.5), col), mats["stone"])
        _put(parts, hz.make_box(f"{asset_name}_BacaKulah", (1.15, 1.15, 0.22),
                                (bx, by, top + 1.41), col), mats["cutstone"])
        top += 1.52

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (W, D, zr), (0.0, 0.0, zr * 0.5), col,
                 mats["plaster"]),
          _solid(f"{asset_name}_L1r", (W + 1.1, D + 1.1, p.roof_h),
                 (0.0, 0.0, zr + p.roof_h * 0.5), col, mats["roof"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W, D + p.seki_d,
                                            zr + p.roof_h * 0.5),
                      (0.0, -p.seki_d * 0.5, (zr + p.roof_h * 0.5) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(D + p.seki_d, 3),
                kind="kahvehane", palette=p.palette)
    return lod0, lod1, ucx, info


# --------------------------------------------------------------------- sebil

class SebilParams(object):
    """
    Sebil — sudağıtım yapısı. Çeşme değildir.

    Fark işlevdedir ve biçime geçer: çeşmeden **kendin alırsın**, sebilden
    sana **verilir**. O yüzden sebilin şebekeli pencereleri ve her pencerenin
    önünde bir **mermer tezgâh** vardır — bardak oradan uzatılır. İçeride
    duran bir görevli olduğu için yapı bir odadır, bir niş değil.

    Kalabalık yerde durur: külliyenin sokağa bakan köşesi. Bu yüzden çokgen
    gövdenin **yalnız sokağa bakan yüzleri açıktır**; arkası duvara yaslanır.
    """

    def __init__(self, **kw):
        self.sides = kw.get("sides", 8)
        self.apothem = kw.get("apothem", 1.85)
        self.wall_t = kw.get("wall_t", 0.40)
        self.wall_h = kw.get("wall_h", 3.30)
        self.open_w = kw.get("open_w", 0.95)
        self.sill_z = kw.get("sill_z", 1.05)       # tezgah kotu
        self.spring_z = kw.get("spring_z", 2.15)
        self.eave = kw.get("eave", 0.95)           # sacak cikmasi
        self.cap_h = kw.get("cap_h", 1.10)
        self.palette = kw.get("palette", "default")

    @property
    def face_w(self):
        return 2.0 * (self.apothem + self.wall_t * 0.5) \
            * math.tan(math.pi / self.sides)

    def validate(self):
        errs = []
        if self.sides != 8:
            errs.append(f"sides={self.sides} — sebil sekizgen kuruluyor")
        if self.open_w > self.face_w - 0.5:
            errs.append(f"open_w={self.open_w}, {self.face_w:.2f} m yuze sigmaz")
        _, rise = sk.arch_points(self.open_w * 0.5, self.spring_z)
        if self.spring_z + rise > self.wall_h - 0.3:
            errs.append(f"kemer tepesi {self.spring_z + rise:.2f} m, "
                        f"duvar {self.wall_h} m")
        # Sacak SEBILI SEBIL YAPAR: tezgahi ve bekleyeni orter. Kisa sacak
        # yapiyi sekizgen bir kuleye cevirir.
        if self.eave < 0.6:
            errs.append(f"eave={self.eave} — tezgahi ortmez")
        if not 0.85 <= self.sill_z <= 1.20:
            errs.append(f"sill_z={self.sill_z} — bardak uzatilacak yukseklik degil")
        if errs:
            raise ValueError("SebilParams gecersiz: " + "; ".join(errs))
        return self

    def face_frame(self, k):
        th = -math.pi * 0.5 + 2.0 * math.pi * k / self.sides
        c = (self.apothem * math.cos(th), self.apothem * math.sin(th))
        return c, (-math.sin(th), math.cos(th)), (math.cos(th), math.sin(th))


# Sokaga bakan yay acik, arka uc yuz kapali: sebil bir duvara yaslanir.
SEBIL_OPEN = (7, 0, 1, 2, 6)


def build_sebil(p, col, asset_name, textured=False):
    """Şebekeli sebil, geniş saçaklı. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    a, t, H, w = p.apothem, p.wall_t, p.wall_h, p.face_w
    R = a / math.cos(math.pi / p.sides)

    _put(parts, hz.make_tube(f"{asset_name}_Kursu", R + 0.34, R + 0.30, 0.30,
                             (0.0, 0.0), 0.0, segments=p.sides, smooth=False,
                             col=col), mats["cutstone"])
    z0 = 0.30

    for k in range(p.sides):
        (cx, cy), u_ax, n_ax = p.face_frame(k)
        if k not in SEBIL_OPEN:
            _put(parts, oriented_box(f"{asset_name}_Yuz{k}", w, t, H,
                                     (cx, cy, z0 + H * 0.5), u_ax, n_ax, col),
                 mats["stone"])
            continue

        _put(parts, sk.arched_panel(
            f"{asset_name}_Yuz{k}", w, H, t, (cx, cy, z0), u_ax, n_ax,
            spans=[(-p.open_w * 0.5, p.open_w * 0.5)], sill_z=p.sill_z,
            spring_z=p.spring_z, col=col), mats["cutstone"])
        _, rise = sk.arch_points(p.open_w * 0.5, p.spring_z)
        oh = (p.spring_z - p.sill_z) + rise * 0.6
        ctr = (cx, cy, z0 + p.sill_z + oh * 0.5)
        _face_shadow(parts, mats, col, f"{asset_name}_Pencere{k}",
                     p.open_w, oh, ctr, u_ax, n_ax, t)
        _face_grille(parts, mats, col, f"{asset_name}_Pencere{k}",
                     p.open_w, oh, ctr, u_ax, n_ax, t, bars=4, rails=2)

        # MERMER TEZGAH: sebili cesmeden ayiran sey. Disariya tasar.
        N = hz.Vector((n_ax[0], n_ax[1], 0.0)).normalized()
        base = hz.Vector((cx, cy, z0 + p.sill_z - 0.05)) + N * (t * 0.5 + 0.16)
        _put(parts, oriented_box(f"{asset_name}_Tezgah", p.open_w + 0.55, 0.42,
                                 0.11, base, u_ax, n_ax, col), mats["cutstone"])

    # --- SACAK: genis ve derin. Sebilin siluetini tasiyan sey budur.
    #
    # Ustu KURSUN, alti ahsap: sacak yagmur alan bir yuzeydir ve kubbeyle ayni
    # malzemeden orulur. Ilk denemede ustu de ahsap (`trim`) idi ve yapinin
    # tepesine kirmizi bir tabak konmus gibi duruyordu — kulah ile sacak iki
    # ayri yapiya ait gibi okunuyordu.
    zc = z0 + H
    _put(parts, hz.make_tube(f"{asset_name}_Sacak", R + p.eave, R + p.eave,
                             0.20, (0.0, 0.0), zc + 0.04, segments=p.sides * 2,
                             smooth=False, col=col), mats["lead"])
    _put(parts, hz.make_tube(f"{asset_name}_SacakAlt", R + 0.18, R + p.eave,
                             0.26, (0.0, 0.0), zc - 0.22, segments=p.sides * 2,
                             smooth=False, col=col), mats["timber"])
    # Ahsap KONSOLLAR: 0,95 m'lik cikmayi tasiyan sey. Onlarsiz sacak havada
    # duran bir disk gibi okunur.
    for k in range(p.sides):
        (cx, cy), u_ax, n_ax = p.face_frame(k)
        N = hz.Vector((n_ax[0], n_ax[1], 0.0)).normalized()
        c = hz.Vector((cx, cy, zc - 0.44)) + N * (p.eave * 0.42 + 0.10)
        _put(parts, oriented_box(f"{asset_name}_Konsol", 0.13,
                                 p.eave * 0.84 + 0.20, 0.30, c, u_ax, n_ax,
                                 col), mats["timber"])
    # Kursun kulah: kubbe degil BASIK kulah — sebil kucuktur.
    _put(parts, hz.make_dome(f"{asset_name}_Kulah", R + 0.10, p.cap_h,
                             (0.0, 0.0), zc + 0.24, segments=16, rings=5,
                             col=col), mats["lead"])
    _alem(parts, mats, col, asset_name, 0.0, 0.0, zc + 0.24 + p.cap_h - 0.05)

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    total = zc + 0.24 + p.cap_h
    l1 = [_solid(f"{asset_name}_L1", (2 * R, 2 * R, zc), (0.0, 0.0, zc * 0.5),
                 col, mats["cutstone"]),
          _solid(f"{asset_name}_L1c", (2 * (R + p.eave), 2 * (R + p.eave), 0.24),
                 (0.0, 0.0, zc + 0.12), col, mats["timber"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (2 * R, 2 * R, zc),
                      (0.0, 0.0, zc * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(2 * (R + p.eave), 3),
                wall_depth=round(2 * (R + p.eave), 3),
                kind="sebil", palette=p.palette)
    return lod0, lod1, ucx, info


# --------------------------------------------------------------------- fırın

class FirinParams(object):
    """
    Mahalle fırını. Ekmek burada pişer ve mahalle bunsuz olmaz.

    Yapıyı fırın yapan şey cephesi değil **arkasıdır**: kubbeli taş ocak ve
    onun kalın bacası. Cepheden bakınca dükkândır; siluetten bakınca fırındır.
    """

    def __init__(self, **kw):
        self.width = kw.get("width", 7.00)
        self.depth = kw.get("depth", 8.20)
        self.wall_t = kw.get("wall_t", 0.42)
        self.plinth = kw.get("plinth", 0.40)
        self.wall_h = kw.get("wall_h", 3.70)
        self.open_w = kw.get("open_w", 2.20)
        self.counter_z = kw.get("counter_z", 0.98)
        self.roof_h = kw.get("roof_h", 1.45)
        self.ocak_w = kw.get("ocak_w", 4.40)       # arkadaki kagir ocak kutlesi
        self.ocak_d = kw.get("ocak_d", 3.00)
        self.baca_h = kw.get("baca_h", 2.60)       # damdan yukarisi
        self.palette = kw.get("palette", "default")

    @property
    def arch_spring(self):
        """
        Kemer basma kotu — GENİŞLİKTEN ve duvardan türetilir, sabit değil.

        Sivri kemerin yükselişi açıklığın yarısıyla orantılıdır
        (`rise = 0,652 · w`). İlk yazımda basma kotu 2,20 m yazılıydı ve 2,20 m
        açıklıkta kemer tepesi 3,63 m çıkıp 3,40 m'lik duvarı aştı — panel
        reddetti. Fırın kapısı bir geçit değil **tezgâh açıklığı**dır; alçak
        basar, geniş açılır.
        """
        rise = 0.65192 * self.open_w
        return min(self.wall_h * 0.62, self.wall_h - 0.34 - rise)

    def validate(self):
        errs = []
        if self.open_w > self.width - 1.4:
            errs.append(f"open_w={self.open_w}, {self.width} m cepheye sigmaz")
        if self.arch_spring < 1.55:
            errs.append(f"open_w={self.open_w} / wall_h={self.wall_h}: kemer "
                        f"{self.arch_spring:.2f} m'den basiyor — tezgahin "
                        f"ustunde kemer icin yer kalmiyor (duvari yukselt ya "
                        f"da acikligi darat)")
        if self.ocak_w > self.width - 0.8:
            errs.append(f"ocak_w={self.ocak_w} govdeden genis")
        # Baca UZUN olmak zorunda: firin bacasi mahallenin en cok tuten
        # bacasidir ve kivilcim komsu ahsap catiya dusmemeli. Kisa baca hem
        # tarihsel hem gorsel olarak yanlis.
        if self.baca_h < 2.0:
            errs.append(f"baca_h={self.baca_h} — firin bacasi damdan en az "
                        f"2 m yukselir (kivilcim ve is)")
        if errs:
            raise ValueError("FirinParams gecersiz: " + "; ".join(errs))
        return self


def build_firin(p, col, asset_name, textured=False):
    """Kubbeli ocaklı mahalle fırını. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, t = p.width, p.depth, p.wall_t
    z0, H = p.plinth, p.wall_h
    y_front = -D * 0.5

    _put(parts, hz.make_box(f"{asset_name}_Subasman", (W + 0.24, D + 0.24, z0),
                            (0.0, 0.0, z0 * 0.5), col), mats["stone"])
    for name, size, center in (
        ("Sol", (t, D, H), (-W * 0.5 + t * 0.5, 0.0, z0 + H * 0.5)),
        ("Sag", (t, D, H), (W * 0.5 - t * 0.5, 0.0, z0 + H * 0.5)),
        ("Arka", (W - 2 * t, t, H), (0.0, D * 0.5 - t * 0.5, z0 + H * 0.5)),
    ):
        _put(parts, hz.make_box(f"{asset_name}_{name}", size, center, col),
             mats["stone"])

    # --- ON CEPHE: tek genis kemerli aciklik; ekmek buradan verilir.
    spring = p.arch_spring
    _put(parts, sk.arched_panel(
        f"{asset_name}_OnCephe", W, H, t, (0.0, y_front + t * 0.5, z0),
        (1.0, 0.0), (0.0, -1.0),
        spans=[(-p.open_w * 0.5, p.open_w * 0.5)], sill_z=0.0,
        spring_z=spring, col=col), mats["plaster"])
    # Karanlik levha KEMERIN TEPESINE kadar cikar, basma kotuna kadar degil.
    #
    # Ilk yazimda `spring`de bitiyordu ve kemer basliginin arkasi acikta
    # kaliyordu: sokaktan bakinca acikligin ustunden CATININ ALTI goruluyordu.
    # Handa ve medresede ayni acikligin arkasi avludur, yani gormek dogrudur —
    # firinin arkasi ise kapali bir odadir. Kusur ancak isik gelince gorundu.
    _, arch_rise = sk.arch_points(p.open_w * 0.5, spring)
    dark_h = spring + arch_rise - 0.05
    _put(parts, hz.make_box(f"{asset_name}_IcKaranlik",
                            (p.open_w, 0.06, dark_h),
                            (0.0, y_front + t + 0.03,
                             z0 + dark_h * 0.5), col), mats["shadow"])
    # TEZGAH: ekmek tartilir ve uzatilir.
    _put(parts, hz.make_box(f"{asset_name}_Tezgah", (p.open_w + 0.4, 0.70, 0.10),
                            (0.0, y_front - 0.30, z0 + p.counter_z), col),
         mats["cutstone"])
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_TezgahAyak", (0.14, 0.14,
                                                             z0 + p.counter_z),
                                (s * (p.open_w * 0.5 - 0.15), y_front - 0.55,
                                 (z0 + p.counter_z) * 0.5), col),
             mats["cutstone"])
    # Sundurma: unu ve ekmegi yagmurdan korur.
    ez = z0 + H - 0.25
    _put(parts, _shed(f"{asset_name}_Sundurma", -(W * 0.5 + 0.35), W * 0.5 + 0.35,
                      -0.10, ez, 1.55, ez - 0.42, 0.08,
                      (0.0, y_front, 0.0), (1.0, 0.0), (0.0, -1.0), col),
         mats["timber"])

    # --- CATI: besik, mahya X boyunca.
    zr = z0 + H
    ridge_z = zr + p.roof_h
    for sy in (-1, 1):
        _put(parts, _shed(f"{asset_name}_Cati", -(W * 0.5 + 0.5), W * 0.5 + 0.5,
                          0.0, ridge_z, D * 0.5 + 0.5, zr - 0.10, 0.13,
                          (0.0, 0.0, 0.0), (1.0, 0.0), (0.0, float(sy)), col),
             mats["roof"])
    for sx in (-1, 1):
        _put(parts, _gable_end(f"{asset_name}_Alinlik", D, p.roof_h,
                               sx * (W * 0.5 - t * 0.5), t, zr, col),
             mats["stone"])

    # --- OCAK: yapinin ARKASINDA kagir kutle + KUBBE + kalin baca.
    #
    # Ocak govdenin icinde kalsaydi disaridan hicbir isareti olmazdi ve yapi
    # bir dukkandan ayirt edilemezdi. Gercekte de firin ocagi arkaya, komsudan
    # uzaga, tas bir kutle olarak yapilir.
    oy = D * 0.5 + p.ocak_d * 0.5 - 0.35
    _put(parts, hz.make_box(f"{asset_name}_Ocak", (p.ocak_w, p.ocak_d, H * 0.72),
                            (0.0, oy, z0 + H * 0.36), col), mats["stone"])
    _put(parts, hz.make_dome(f"{asset_name}_OcakKubbe", p.ocak_w * 0.46,
                             p.ocak_w * 0.26, (0.0, oy), z0 + H * 0.72,
                             segments=12, rings=4, col=col), mats["stone"])
    bx = p.ocak_w * 0.28
    b_top = ridge_z + p.baca_h
    _put(parts, hz.make_box(f"{asset_name}_Baca", (0.95, 0.95, b_top),
                            (bx, oy - 0.20, b_top * 0.5), col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_BacaKulah", (1.22, 1.22, 0.24),
                            (bx, oy - 0.20, b_top + 0.12), col), mats["cutstone"])
    top = b_top + 0.24

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    dt = D + p.ocak_d - 0.35
    l1 = [_solid(f"{asset_name}_L1", (W, D, zr), (0.0, 0.0, zr * 0.5), col,
                 mats["stone"]),
          _solid(f"{asset_name}_L1r", (W + 1.0, D + 1.0, p.roof_h),
                 (0.0, 0.0, zr + p.roof_h * 0.5), col, mats["roof"]),
          _solid(f"{asset_name}_L1b", (1.0, 1.0, b_top), (bx, oy - 0.20,
                                                          b_top * 0.5), col,
                 mats["stone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W, dt, zr),
                      (0.0, (dt - D) * 0.5, zr * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(dt, 3),
                kind="firin", palette=p.palette)
    return lod0, lod1, ucx, info
