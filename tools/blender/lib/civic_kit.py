"""
Hezarfen: 1632 — Hamam ve han (plan Faz 2b).

İkisi de siluetin taşıyıcısıdır ve ikisinin de imzası **çatısındadır**.

## Hamam — imza: fil gözü

Hamamı hamam yapan şey planı değil, **kurşun kubbe kümesi** ve o kubbeleri
delen **fil gözü** camlarıdır. Sıcaklıkta pencere olmaz (buhar kaçar, mahremiyet
bozulur); ışık yukarıdan, kubbeye açılmış küçük yuvarlak camlardan gelir. Fil
gözü olmadan hamam, kubbeli bir depodur.

Kütle sırası hep aynıdır ve **soğuktan sıcağa** dizilir:
**soğukluk/camekân** (en büyük kubbe, giriş) → **ılıklık** (ara) →
**sıcaklık** (göbek taşı + köşelerde **halvet** hücreleri) → **külhan**
(ocak, arkada, bacalı). Bacayı arkaya koymak zorunludur: külhan kirli işin
yeridir, giriş cephesine bakmaz.

## Han — imza: avlu ve sağır dış duvar

Han bir **avlu** yapısıdır ve dışarıya **kapalı**dır: zemin katta dışa pencere
yok denecek kadar azdır, çünkü han aynı zamanda kasadır. Bütün ışık ve hayat
avluya bakar; avlu cephesinde iki kat **revak** (kemerli galeri) döner. Tek
kapı vardır — **taçkapı** — ve geceleri kapanır.

Üst kattaki odaların her biri küçük bir kubbe ve bir **baca** taşır: han
silueti, damında sıralanmış o kubbe-baca ritmidir.

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z).
"""

import math

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


# ------------------------------------------------------------------- hamam

def _fil_gozu(parts, mats, col, name, cx, cy, base_z, radius, height, count,
              rings=2):
    """
    Kubbeyi delen **fil gözü** camları.

    Yarım elipsoid üstünde (yükseklik açısı `t`, azimut `a`) noktalar seçilir;
    her birine küçük bir cam kabarcık oturur. Kabarcık kubbenin İÇİNE değil
    dışına taşar — gerçekte cam, kubbe kabuğunun üstünde bir tümsektir ve
    yağmurda su tutmasın diye öyledir.
    """
    n = 0
    for r in range(rings):
        t = math.radians(24.0 + 34.0 * r / max(1, rings - 1)) if rings > 1 \
            else math.radians(38.0)
        rr, zz = radius * math.cos(t), height * math.sin(t)
        m = max(3, int(round(count * math.cos(t))))
        for i in range(m):
            a = 2.0 * math.pi * (i + 0.5 * r) / m
            px, py = cx + rr * math.cos(a), cy + rr * math.sin(a)
            eye = hz.make_dome(f"{name}_FilGozu", 0.20, 0.13, (px, py),
                               base_z + zz - 0.02, segments=6, rings=3, col=col)
            _put(parts, eye, mats["glass"])
            n += 1
    return n


def _domed_hall(parts, mats, col, name, cx, cy, w, d, wall_h, dome_h,
                eyes=0, drum=0.45, front_gap=0.0):
    """
    Kâgir kutu + kasnak + kurşun kubbe. Hamamın ve hanın ortak birimi.

    `front_gap`: kutunun ÖN yüzünden bu kadarı boş bırakılır — oraya delikli
    cephe paneli gelecektir. İlk denemede bırakılmadı ve kâgir kutu, kapının
    arkasını doldurup **kapıyı görünmez** yaptı: açıklık vardı ama arkasında
    duvar duruyordu. Render'da "kapı yok" diye okundu.
    """
    dd = d - front_gap
    _put(parts, hz.make_box(f"{name}_Duvar", (w, dd, wall_h),
                            (cx, cy + front_gap * 0.5, wall_h * 0.5), col),
         mats["stone"])
    r = min(w, d) * 0.5
    if drum > 0.01:
        _put(parts, hz.make_tube(f"{name}_Kasnak", r * 0.98, r * 0.98, drum,
                                 (cx, cy), wall_h, segments=12, smooth=False,
                                 col=col), mats["cutstone"])
    z = wall_h + drum
    _put(parts, hz.make_dome(f"{name}_Kubbe", r, dome_h, (cx, cy), z,
                             segments=16, rings=6, col=col), mats["lead"])
    if eyes:
        _fil_gozu(parts, mats, col, name, cx, cy, z, r, dome_h, eyes)
    return z + dome_h


class HamamParams(object):
    def __init__(self, **kw):
        self.camekan = kw.get("camekan", 11.0)     # sogukluk kare kenari
        self.iliklik_d = kw.get("iliklik_d", 5.0)
        self.sicaklik = kw.get("sicaklik", 10.0)
        self.wall_h = kw.get("wall_h", 5.2)
        self.halvet = kw.get("halvet", 3.6)        # kose hucresi kenari
        self.kulhan = kw.get("kulhan", True)
        self.baca_h = kw.get("baca_h", 9.5)
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.halvet * 2 > self.sicaklik + 1.0:
            errs.append(f"halvet={self.halvet} sicakligin ({self.sicaklik}) "
                        f"kosesine sigmaz")
        if self.wall_h < 4.0:
            errs.append(f"wall_h={self.wall_h} kagir hamam icin alcak")
        if errs:
            raise ValueError("HamamParams gecersiz: " + "; ".join(errs))
        return self


def build_hamam(p, col, asset_name, textured=False):
    """Çifte olmayan tek hamam. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []

    C, S, I = p.camekan, p.sicaklik, p.iliklik_d
    # Kutleler ONDEN ARKAYA: sogukluk (-Y, giris) -> iliklik -> sicaklik.
    y_cam = -(S * 0.5 + I + C * 0.5)
    y_ilik = -(S * 0.5 + I * 0.5)
    y_sic = 0.0

    top = _domed_hall(parts, mats, col, f"{asset_name}_Camekan",
                      0.0, y_cam, C, C, p.wall_h, C * 0.34, eyes=8,
                      front_gap=0.70)
    _domed_hall(parts, mats, col, f"{asset_name}_Iliklik",
                0.0, y_ilik, C * 0.72, I, p.wall_h * 0.86, I * 0.42,
                eyes=5, drum=0.30)
    top = max(top, _domed_hall(parts, mats, col, f"{asset_name}_Sicaklik",
                               0.0, y_sic, S, S, p.wall_h, S * 0.38, eyes=9))

    # Halvet: sicakligin dort kosesinde kucuk kubbeli hucreler.
    hv = p.halvet
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx = sx * (S * 0.5 + hv * 0.5 - 0.5)
            cy = y_sic + sy * (S * 0.5 - hv * 0.5)
            _domed_hall(parts, mats, col, f"{asset_name}_Halvet",
                        cx, cy, hv, hv, p.wall_h * 0.72, hv * 0.40,
                        eyes=4, drum=0.0)

    # Giris: sivri kemerli tac kapi, sogukluk cephesinde.
    y_front = y_cam - C * 0.5
    _put(parts, sk.arched_panel(
        f"{asset_name}_OnCephe", C, p.wall_h, 0.70,
        (0.0, y_front + 0.35, 0.0), (1.0, 0.0), (0.0, -1.0),
        spans=[(-0.8, 0.8)], sill_z=0.20, spring_z=2.45, col=col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik", (1.6, 0.06, 2.25),
                            (0.0, y_front + 0.68, 1.32), col), mats["shadow"])
    _put(parts, hz.make_box(f"{asset_name}_Esik", (2.6, 0.5, 0.20),
                            (0.0, y_front - 0.25, 0.10), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Silme", (C + 0.3, 0.3, 0.22),
                            (0.0, y_front + 0.05, p.wall_h + 0.11), col),
         mats["cutstone"])

    # Kulhan: ocak arkada, bacasi yuksek. Kirli is girise bakmaz.
    total_h = top
    if p.kulhan:
        y_k = S * 0.5 + 2.2
        _put(parts, hz.make_box(f"{asset_name}_Kulhan", (S * 0.55, 4.4, 3.6),
                                (0.0, y_k, 1.8), col), mats["stone"])
        bx = S * 0.20
        _put(parts, hz.make_tube(f"{asset_name}_Baca", 0.62, 0.50, p.baca_h,
                                 (bx, y_k + 1.0), 0.0, segments=8, smooth=False,
                                 col=col), mats["stone"])
        _put(parts, hz.make_tube(f"{asset_name}_BacaKulah", 0.78, 0.78, 0.30,
                                 (bx, y_k + 1.0), p.baca_h, segments=8,
                                 smooth=False, col=col), mats["cutstone"])
        total_h = max(total_h, p.baca_h + 0.30)

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)

    depth = C + I + S + (4.4 + 2.2 if p.kulhan else 0.0)
    width = S + 2.0 * (p.halvet - 0.5)
    cy = (-(S * 0.5 + I + C) + (S * 0.5 + (4.4 if p.kulhan else 0.0))) * 0.5
    l1 = [_solid(f"{asset_name}_L1", (width, depth, p.wall_h),
                 (0.0, cy, p.wall_h * 0.5), col, mats["stone"]),
          _solid(f"{asset_name}_L1d", (C * 0.9, C * 0.9, C * 0.30),
                 (0.0, y_cam, p.wall_h + C * 0.15), col, mats["lead"]),
          _solid(f"{asset_name}_L1s", (S * 0.9, S * 0.9, S * 0.34),
                 (0.0, y_sic, p.wall_h + S * 0.17), col, mats["lead"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    ucx = hz.make_box(f"UCX_{asset_name}", (width, depth, p.wall_h),
                      (0.0, cy, p.wall_h * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(width, 3), wall_depth=round(depth, 3),
                kind="hamam", palette=p.palette)
    return lod0, lod1, ucx, info


# --------------------------------------------------------------------- han

class HanParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 32.0)         # dis olcu
        self.depth = kw.get("depth", 26.0)
        self.wing = kw.get("wing", 7.0)            # kanat derinligi (oda + revak)
        self.floor_h = kw.get("floor_h", 4.2)
        self.floors = kw.get("floors", 2)
        self.wall_t = kw.get("wall_t", 0.85)
        self.arch_w = kw.get("arch_w", 2.4)        # revak acikligi (hedef)
        self.portal_w = kw.get("portal_w", 3.0)
        self.kuyu = kw.get("kuyu", True)           # avlu kuyusu
        self.domes = kw.get("domes", True)         # ust kat odalarina kubbe
        self.palette = kw.get("palette", "default")

    def validate(self):
        errs = []
        if self.wing * 2 > min(self.width, self.depth) - 8.0:
            errs.append(f"wing={self.wing} avluya yer birakmiyor")
        if self.portal_w > self.width * 0.25:
            errs.append(f"portal_w={self.portal_w} cepheye gore buyuk")
        # Tackapi sivri kemerlidir ve YUKSEK yer ister. Gerekli yuksekligi
        # kapinin kendisi dayatir: h >= (0,652 w + 0,45) / 0,38. Fikihtaki
        # "yuklu deve gecebilmeli" olcutunun han kapisindaki karsiligi budur —
        # alcak kapili han ise yaramaz.
        need = (0.652 * self.portal_w + 0.45) / 0.38
        H = self.floor_h * self.floors
        if H < need:
            errs.append(f"portal_w={self.portal_w} icin en az {need:.2f} m "
                        f"yukseklik gerekir, bina {H:.2f} m "
                        f"(floor_h veya floors artir, ya da kapiyi darat)")
        if errs:
            raise ValueError("HanParams gecersiz: " + "; ".join(errs))
        return self

    @property
    def court_w(self):
        return self.width - 2.0 * self.wing

    @property
    def court_d(self):
        return self.depth - 2.0 * self.wing


# Revak ölçüleri. Aralarındaki İLİŞKİ ölçüden önemlidir:
#
#   2·COL_R > PIER_W  ve  2·COL_R > REVAK_T
#
# Sütun ayaktan da panelden de **kalın** olmak zorunda. İlk denemede ayak
# 0,44 m, sütun çapı 0,40 m'ydi: silindir düz ayak yüzünün ARKASINDA kaldı ve
# cepheden hiç görünmedi — render'da revak yine "delikli duvar"dı. Sütunu
# gösteren şey konumu değil, ayağı örtmesidir.
REVAK_T = 0.38          # revak kemer duzleminin kalinligi
PIER_W = 0.30           # ayak: sutunun icinde kalan kagir omurga
COL_R = 0.22            # cap 0,44 > ayak 0,30 ve > panel 0,38
CORNER_W = 0.78         # avlu kosesindeki masif ayak


def _ring(parts, col, name, ow, od, iw, idp, z, h, mat):
    """
    Silme/dam **HALKASI** — dört kenar kutusu, ortası boş.

    İlk denemede han damı tam plakaydı ve **avlunun üstünü kapattı**: yapı
    avlulu olmaktan çıkıp ambara döndü. Üstten bakıldığında hata apaçıktı ama
    yandan hiç görünmüyordu. Avluyu örten her yüzey yapıyı başka bir şey
    yapar — medrese de aynı kuralla örtülür, o yüzden burada tek nüsha.
    """
    for sy in (-1, 1):
        _put(parts, hz.make_box(name, (ow, (od - idp) * 0.5, h),
                                (0.0, sy * (od + idp) * 0.25, z + h * 0.5),
                                col), mat)
    for sx in (-1, 1):
        _put(parts, hz.make_box(name, ((ow - iw) * 0.5, idp, h),
                                (sx * (ow + iw) * 0.25, 0.0, z + h * 0.5),
                                col), mat)


def _kuyu(parts, mats, col, name, cx=0.0, cy=0.0, base_z=0.10):
    """Avlu kuyusu: bilezik + karanlık ağız + iki direk ve kirişi."""
    _put(parts, hz.make_tube(f"{name}_KuyuBilezik", 0.58, 0.54, 0.82, (cx, cy),
                             base_z, segments=12, smooth=False, col=col),
         mats["cutstone"])
    _put(parts, hz.make_tube(f"{name}_KuyuAgzi", 0.40, 0.40, 0.03, (cx, cy),
                             base_z + 0.80, segments=12, smooth=False, col=col),
         mats["shadow"])
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{name}_KuyuDirek", (0.11, 0.11, 2.1),
                                (cx + sx * 0.72, cy, base_z + 1.05), col),
             mats["timber"])
    _put(parts, hz.make_box(f"{name}_KuyuKiris", (1.75, 0.12, 0.12),
                            (cx, cy, base_z + 2.16), col), mats["timber"])


def _column(parts, mats, col, name, x, y, base_z, shaft_h,
            r=COL_R, segments=8):
    """
    Devşirme sütun: kürsü + gövde + **başlık**.

    Başlık şart, süs değil: kemer taşa değil başlığa oturur, ve başlığın
    yaptığı o kısa genişleme gözün "burada yük aktarılıyor" diye okuduğu tek
    işarettir. Başlıksız bir silindir sütun değil borudur.
    """
    plinth_h, cap_h = 0.16, 0.26
    _put(parts, hz.make_box(f"{name}_Kursu", (r * 2.7, r * 2.7, plinth_h),
                            (x, y, base_z + plinth_h * 0.5), col), mats["cutstone"])
    _put(parts, hz.make_tube(f"{name}_Govde", r, r * 0.93, shaft_h, (x, y),
                             base_z + plinth_h, segments=segments, smooth=True,
                             col=col), mats["cutstone"])
    _put(parts, hz.make_tube(f"{name}_Baslik", r * 0.93, r * 1.42, cap_h, (x, y),
                             base_z + plinth_h + shaft_h, segments=segments,
                             smooth=False, col=col), mats["cutstone"])
    return plinth_h + shaft_h + cap_h


def _arcade(parts, mats, col, name, length, height, origin,
            u_axis, n_axis, arch_w, sutun=True):
    """
    Revak: eşit açıklıklı sivri kemer dizisi + sütunları.

    ## Neden ayak İNCE

    İlk yazımda ayak 1,1 m'ydi ve kemerler doğrudan o kâgir bloklara oturuyordu:
    yapı revak değil **delikli duvar** okunuyordu. Revak bir duvar değil, açık
    bir galeridir; onu galeri yapan şey ayakların ince olmasıdır. Ayak burada
    yalnızca sütunun içindeki kâgir omurgadır; görülen şey sütundur. Avlu
    köşeleri buna dahil değil — orası masif kalır (`build_han`).

    ## Neden açıklık uzunluktan TÜRETİLİR

    `arched_panel` bütün açıklıkların aynı ölçüde olmasını ister (T-kavşağı
    yok). Ölçüyü sabitleyip sayıyı yuvarlamak kenarlarda artık boşluk bırakırdı;
    bunun yerine sayı yuvarlanır ve açıklık uzunluğa **tam bölünür**.
    """
    bays = max(2, int(round((length - PIER_W) / (arch_w + PIER_W))))
    span_w = (length - (bays + 1) * PIER_W) / bays
    if span_w < 1.0:
        return 0

    # Kemer tepesi panelin icinde kalmali; basma kotu buna gore SIKISTIRILIR.
    _, rise = sk.arch_points(span_w * 0.5, 1.0)
    spring = min(height * 0.55, height - rise - 0.28)
    if spring < 1.6:
        return 0

    spans, u = [], -length * 0.5 + PIER_W
    piers = [-length * 0.5 + PIER_W * 0.5]
    for _ in range(bays):
        spans.append((u, u + span_w))
        u += span_w + PIER_W
        piers.append(u - PIER_W * 0.5)

    _put(parts, sk.arched_panel(name, length, height, REVAK_T, origin,
                                u_axis, n_axis, spans=spans, sill_z=0.0,
                                spring_z=spring, col=col), mats["stone"])

    if sutun:
        U = hz.Vector((u_axis[0], u_axis[1], 0.0)).normalized()
        O = hz.Vector(origin)
        # Kose ayaklari KAGIR kalir: iki revak orada dik kesisir, sutun konsa
        # iki kez ust uste binerdi ve gercekte de kose ayagi masiftir.
        for pu in piers[1:-1]:
            p = O + U * pu
            _column(parts, mats, col, f"{name}_Sutun", p.x, p.y, p.z,
                    spring - 0.42)
    return bays


def build_han(p, col, asset_name, textured=False):
    """Avlulu han. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, t = p.width, p.depth, p.wall_t
    H = p.floor_h * p.floors
    cw, cd = p.court_w, p.court_d

    # --- DIS DUVAR: sagir. Han ayni zamanda kasadir.
    #
    # Ust katta kucuk, yuksek pencereler var; zemin katta yok. Bunu "cephe
    # boslugu" diye doldurmak yapinin ne oldugunu siler.
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_DisYan", (t, D, H),
                                (sx * (W * 0.5 - t * 0.5), 0.0, H * 0.5), col),
             mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_DisArka", (W - 2 * t, t, H),
                            (0.0, D * 0.5 - t * 0.5, H * 0.5), col), mats["stone"])

    # --- ON CEPHE + TACKAPI
    y_front = -D * 0.5 + t * 0.5

    # Taçkapı ölçüleri BİNADAN TÜRETİLİR, sabit değil.
    #
    # İlk yazımda basma kotu 3,6 m yazılıydı; tek katlı `Han_B` (H = 4,20 m)
    # üretilirken kemer tepesi 5,29 m çıktı ve panel reddetti. Sabit ölçü,
    # parametre değişince sessizce bozulan ölçüdür — burada gürültülü bozuldu
    # (`arched_panel` hata fırlattı), ki iyi oldu.
    sill = 0.22
    spring = min(H * 0.62, 3.6)
    _, prise = sk.arch_points(p.portal_w * 0.5, spring)
    ptop = spring + prise
    _put(parts, sk.arched_panel(
        f"{asset_name}_OnCephe", W - 2 * t, H, t,
        (0.0, y_front, 0.0), (1.0, 0.0), (0.0, -1.0),
        spans=[(-p.portal_w * 0.5, p.portal_w * 0.5)],
        sill_z=sill, spring_z=spring, col=col), mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik",
                            (p.portal_w, 0.06, spring - sill),
                            (0.0, y_front + t * 0.5 - 0.05,
                             sill + (spring - sill) * 0.5), col),
         mats["shadow"])
    # Tackapi cercevesi: kesme tas kusak + kitabe.
    frame_h = min(ptop + 0.55, H - 0.10)
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_PortalSove",
                                (0.45, 0.20, frame_h),
                                (sx * (p.portal_w * 0.5 + 0.22),
                                 y_front - t * 0.5 - 0.10, frame_h * 0.5), col),
             mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_PortalAlinlik",
                            (p.portal_w + 1.5, 0.24, 0.40),
                            (0.0, y_front - t * 0.5 - 0.12, frame_h + 0.20), col),
         mats["cutstone"])
    if H - frame_h > 1.1:
        _put(parts, hz.make_box(f"{asset_name}_Kitabe",
                                (p.portal_w * 0.8, 0.10, 0.60),
                                (0.0, y_front - t * 0.5 - 0.20, frame_h + 0.75),
                                col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Esik", (p.portal_w + 2.0, 0.6, 0.22),
                            (0.0, -D * 0.5 - 0.30, 0.11), col), mats["cutstone"])

    # --- AVLU CEPHESI: iki kat REVAK
    #
    # Hanin butun isigi ve hayati avluya bakar. Revak olmadan avlu bir bosluk,
    # yapi da dikdortgen bir ambar olur.
    bays = 0
    for f in range(p.floors):
        z = f * p.floor_h
        for sx in (-1, 1):
            bays += _arcade(parts, mats, col,
                            f"{asset_name}_RevakYan{f}", cd, p.floor_h,
                            (sx * cw * 0.5, 0.0, z), (0.0, 1.0),
                            (-float(sx), 0.0), p.arch_w)
        for sy in (-1, 1):
            bays += _arcade(parts, mats, col,
                            f"{asset_name}_RevakUc{f}", cw - 2 * REVAK_T,
                            p.floor_h, (0.0, sy * cd * 0.5, z), (1.0, 0.0),
                            (0.0, -float(sy)), p.arch_w)

    # Avlu koselerinde MASIF ayak. Revak ayaklari sutunun icinde kalacak kadar
    # inceldi; kose oyle birakilamaz, cunku iki revak orada dik kesisir ve yuk
    # koseye biner. Kose ayagi bir kez, ikisinin ortak noktasina konur —
    # her revak kendi ucuna koysaydi ayni yerde iki kutu ust uste binerdi.
    for sx in (-1, 1):
        for sy in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_AvluKose",
                                    (CORNER_W, CORNER_W, H),
                                    (sx * cw * 0.5, sy * cd * 0.5, H * 0.5), col),
                 mats["stone"])

    # --- AVLU ZEMINI ve KUYU
    #
    # Avlu, hanin odasidir: yuk orada indirilir, hayvan orada baglanir, su
    # oradan cekilir. Ilk uretimde zemin HIC yoktu — ustten bakinca yapinin
    # ortasinda arazi gorunuyordu ve han bir "cerceve" gibi okunuyordu.
    _put(parts, hz.make_box(f"{asset_name}_AvluZemin",
                            (cw + 2 * REVAK_T, cd + 2 * REVAK_T, 0.10),
                            (0.0, 0.0, 0.05), col), mats["paving"])
    if p.kuyu:
        _kuyu(parts, mats, col, asset_name)

    if p.floors > 1:
        _ring(parts, col, f"{asset_name}_KatSilme", W + 0.24, D + 0.24, cw, cd,
              p.floor_h - 0.12, 0.24, mats["cutstone"])
    _ring(parts, col, f"{asset_name}_Dam", W, D, cw, cd, H, 0.30, mats["cutstone"])
    _ring(parts, col, f"{asset_name}_UstSilme", W + 0.34, D + 0.34, W - 0.2,
          D - 0.2, H + 0.02, 0.28, mats["cutstone"])

    # --- DAM: her odaya bir kubbe + bir BACA. Hanin silueti bu ritimdir.
    top = H + 0.30
    if p.domes:
        step = p.wing * 0.86
        cells = []
        nx = max(2, int(round((W - 2 * t) / step)))
        nz = max(2, int(round((D - 2 * p.wing) / step)))
        for i in range(nx):
            x = -W * 0.5 + (i + 0.5) * (W / nx)
            cells.append((x, -D * 0.5 + p.wing * 0.5))
            cells.append((x, D * 0.5 - p.wing * 0.5))
        # Yan kanat kubbeleri yalnizca AVLU BOYUNCA; on/arka sirayla cakisan
        # kose hucreleri iki kez kubbe almasin.
        for j in range(nz):
            y = -cd * 0.5 + (j + 0.5) * (cd / nz)
            cells.append((-W * 0.5 + p.wing * 0.5, y))
            cells.append((W * 0.5 - p.wing * 0.5, y))
        r = p.wing * 0.30
        for k, (x, y) in enumerate(cells):
            _put(parts, hz.make_dome(f"{asset_name}_OdaKubbe", r, r * 0.62,
                                     (x, y), H + 0.30, segments=10, rings=4,
                                     col=col), mats["lead"])
            if k % 2 == 0:
                _put(parts, hz.make_box(f"{asset_name}_Baca", (0.55, 0.55, 1.5),
                                        (x + r * 1.25, y, H + 1.05), col),
                     mats["stone"])
        top = H + 0.30 + r * 0.62

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (W, D, H), (0.0, 0.0, H * 0.5), col,
                 mats["stone"]),
          _solid(f"{asset_name}_L1c", (cw, cd, 0.2), (0.0, 0.0, H - 0.1), col,
                 mats["shadow"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W, D, H), (0.0, 0.0, H * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(D, 3),
                kind="han", palette=p.palette, bays=bays)
    return lod0, lod1, ucx, info


# ----------------------------------------------------------------- medrese

class MedreseParams(object):
    def __init__(self, **kw):
        self.width = kw.get("width", 28.0)
        self.depth = kw.get("depth", 23.0)
        self.wing = kw.get("wing", 5.4)            # hucre derinligi + revak
        self.floor_h = kw.get("floor_h", 3.90)
        self.wall_t = kw.get("wall_t", 0.75)
        self.arch_w = kw.get("arch_w", 2.20)
        self.portal_w = kw.get("portal_w", 2.60)
        self.dershane = kw.get("dershane", True)   # arkada buyuk kubbeli sinif
        self.dershane_w = kw.get("dershane_w", 8.0)
        self.kuyu = kw.get("kuyu", True)
        self.palette = kw.get("palette", "default")

    @property
    def portal_block(self):
        """Taçkapının `(genişlik, yükseklik, basma kotu)` — cepheden TÜRETİLİR."""
        spring = max(2.60, self.floor_h * 0.62)
        _, rise = sk.arch_points(self.portal_w * 0.5, spring)
        return (self.portal_w + 2.2, spring + rise + 0.55, spring)

    def validate(self):
        errs = []
        if self.wing * 2 > min(self.width, self.depth) - 8.0:
            errs.append(f"wing={self.wing} avluya yer birakmiyor")

        # TACKAPI TEK KAT DEGILDIR — cepheden YUKSELIR.
        #
        # Ilk yazimda han kuralini oldugu gibi kopyaladim: "kapinin gerektirdigi
        # yukseklik binada olmali". Medrese tek katli oldugu icin dogrulama
        # reddetti — ve haklıydı, ama cozum kapiyi daraltmak degildi. Osmanli
        # medresesinin ve hanin kapisi zaten ayri, ONE TASAN ve DAMI ASAN bir
        # kutledir; adi "tac" kapi olmasinin sebebi budur. Kisit yapiya degil,
        # kapinin kendi blokuna uygulanir.
        pbw, ph, _ = self.portal_block
        if ph < self.floor_h + 0.60:
            errs.append(f"tackapi blogu {ph:.2f} m, cephe {self.floor_h:.2f} m — "
                        f"taci okunmuyor (kapiyi genislet ya da kati alcalt)")
        if pbw > self.width - 2 * self.wall_t - 4.0:
            errs.append(f"tackapi blogu {pbw:.2f} m, cepheye ({self.width:.1f} m) "
                        f"yanlarinda duvar birakmiyor")
        if self.dershane and self.dershane_w > self.width - 2 * self.wing - 2.0:
            errs.append(f"dershane_w={self.dershane_w} avlu genisligini asiyor")
        if errs:
            raise ValueError("MedreseParams gecersiz: " + "; ".join(errs))
        return self

    @property
    def court_w(self):
        return self.width - 2.0 * self.wing

    @property
    def court_d(self):
        return self.depth - 2.0 * self.wing


def build_medrese(p, col, asset_name, textured=False):
    """
    Revaklı avlulu medrese. Dönüş: `(lod0, lod1, ucx, info)`.

    ## Han ile aynı gramer, farklı cümle

    İkisi de avlu + revak + kubbeli dam. Ayıran üç şey var ve üçü de siluetten
    okunur:

      * Medrese **TEK KATLIDIR**. Han üst kata oda dizer (mal ve tüccar
        yukarıda uyur); medresenin hücreleri zeminde, avluya bakar.
      * Hücre kubbeleri **her kanatta** vardır ve **eşittir** — dam bir ritim,
        yapı bir sayıdır: kaç talebe barınıyor.
      * O eşit ritmi tek bir yerde **DERSHANE** kırar: avlunun karşı ucunda,
        ötekilerden büyük tek kubbe. Dershanesiz bir medrese, kubbeli bir
        koğuştur.

    Her hücrede bir **ocak** vardır, yani her kubbenin bir bacası olur; bu da
    hana benzemez (handa bacalar seyrektir, odaların yarısı ambar).
    """
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D, t = p.width, p.depth, p.wall_t
    H = p.floor_h
    cw, cd = p.court_w, p.court_d

    # --- DIS DUVAR: yuksek ve az pencereli. Medrese ic dunyaya bakar.
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_DisYan", (t, D, H),
                                (sx * (W * 0.5 - t * 0.5), 0.0, H * 0.5), col),
             mats["stone"])
    _put(parts, hz.make_box(f"{asset_name}_DisArka", (W - 2 * t, t, H),
                            (0.0, D * 0.5 - t * 0.5, H * 0.5), col), mats["stone"])

    # --- ON CEPHE + TACKAPI (blok ONE TASAR ve DAMI ASAR)
    y_front = -D * 0.5 + t * 0.5
    sill = 0.20
    pbw, ph, spring = p.portal_block
    _, prise = sk.arch_points(p.portal_w * 0.5, spring)
    ptop = spring + prise

    # Tackapinin iki yanindaki cephe: sagir duvar, kat yuksekliginde.
    side_w = (W - 2 * t - pbw) * 0.5
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_OnCephe", (side_w, t, H),
                                (sx * (pbw + side_w) * 0.5, y_front, H * 0.5),
                                col), mats["stone"])

    p_t = t + 0.50                     # blok cepheden 0,50 m one tasar
    y_block = y_front - 0.25
    y_face = y_block - p_t * 0.5
    _put(parts, sk.arched_panel(
        f"{asset_name}_TacKapi", pbw, ph, p_t,
        (0.0, y_block, 0.0), (1.0, 0.0), (0.0, -1.0),
        spans=[(-p.portal_w * 0.5, p.portal_w * 0.5)],
        sill_z=sill, spring_z=spring, col=col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_KapiKaranlik",
                            (p.portal_w, 0.06, spring - sill),
                            (0.0, y_block + p_t * 0.5 - 0.05,
                             sill + (spring - sill) * 0.5), col), mats["shadow"])
    for sx in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_PortalSove", (0.34, 0.18, ptop),
                                (sx * (p.portal_w * 0.5 + 0.17), y_face - 0.09,
                                 ptop * 0.5), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Kitabe", (p.portal_w * 0.9, 0.10, 0.50),
                            (0.0, y_face - 0.14, ptop + 0.42), col),
         mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_PortalSilme", (pbw + 0.30, 0.26, 0.26),
                            (0.0, y_face - 0.06, ph + 0.13), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Esik", (p.portal_w + 1.6, 0.55, 0.20),
                            (0.0, y_face - 0.28, 0.10), col), mats["cutstone"])

    # --- AVLU REVAKI: tek kat, dort yan.
    bays = 0
    for sx in (-1, 1):
        bays += _arcade(parts, mats, col, f"{asset_name}_RevakYan", cd, H,
                        (sx * cw * 0.5, 0.0, 0.0), (0.0, 1.0),
                        (-float(sx), 0.0), p.arch_w)
    for sy in (-1, 1):
        bays += _arcade(parts, mats, col, f"{asset_name}_RevakUc",
                        cw - 2 * REVAK_T, H, (0.0, sy * cd * 0.5, 0.0),
                        (1.0, 0.0), (0.0, -float(sy)), p.arch_w)
    for sx in (-1, 1):
        for sy in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_AvluKose",
                                    (CORNER_W, CORNER_W, H),
                                    (sx * cw * 0.5, sy * cd * 0.5, H * 0.5), col),
                 mats["stone"])

    _put(parts, hz.make_box(f"{asset_name}_AvluZemin",
                            (cw + 2 * REVAK_T, cd + 2 * REVAK_T, 0.10),
                            (0.0, 0.0, 0.05), col), mats["paving"])
    if p.kuyu:
        _kuyu(parts, mats, col, asset_name)

    _ring(parts, col, f"{asset_name}_Dam", W, D, cw, cd, H, 0.28,
          mats["cutstone"])
    _ring(parts, col, f"{asset_name}_UstSilme", W + 0.32, D + 0.32, W - 0.2,
          D - 0.2, H + 0.02, 0.26, mats["cutstone"])

    # --- HUCRE KUBBELERI: esit, her kanatta, HER BIRINDE BACA.
    #
    # Bacanin sayisi burada anlamlidir: her hucrede bir ocak vardir. Handa
    # bacalar seyrektir (odalarin yarisi ambar), medresede degildir.
    z_dam = H + 0.28
    step = p.wing * 0.90
    cells = []
    nx = max(2, int(round((W - 2 * t) / step)))
    nz = max(2, int(round(cd / step)))
    # Her hucre kendi DIS duvarinin yonunu tasir: baca oradan cikar.
    #
    # Ilk yazimda baca hep +X'e kaydiriliyordu; sag sirada duvarin DISINA
    # tasti ve dam bir cit gibi okundu. Ocak dis duvara yaslanir, bacasi da o
    # duvarin icinden yukselir — yon hucrenin kendisinden gelmeli.
    for i in range(nx):
        x = -W * 0.5 + (i + 0.5) * (W / nx)
        cells.append((x, -D * 0.5 + p.wing * 0.5, 0.0, -1.0))
        cells.append((x, D * 0.5 - p.wing * 0.5, 0.0, 1.0))
    for j in range(nz):
        y = -cd * 0.5 + (j + 0.5) * (cd / nz)
        cells.append((-W * 0.5 + p.wing * 0.5, y, -1.0, 0.0))
        cells.append((W * 0.5 - p.wing * 0.5, y, 1.0, 0.0))
    r = p.wing * 0.32
    off = p.wing * 0.5 - 0.55
    for x, y, ox, oy in cells:
        _put(parts, hz.make_dome(f"{asset_name}_HucreKubbe", r, r * 0.60,
                                 (x, y), z_dam, segments=10, rings=4, col=col),
             mats["lead"])
        bx, by = x + ox * off, y + oy * off
        _put(parts, hz.make_box(f"{asset_name}_Baca", (0.52, 0.52, 1.30),
                                (bx, by, z_dam + 0.65), col), mats["stone"])
        _put(parts, hz.make_box(f"{asset_name}_BacaKulah", (0.72, 0.72, 0.16),
                                (bx, by, z_dam + 1.38), col), mats["cutstone"])
    top = z_dam + max(r * 0.60, 1.46)

    # --- DERSHANE: esit ritmi kiran tek kubbe, avlunun KARSI ucunda.
    if p.dershane:
        dw = p.dershane_w
        dy = D * 0.5 + dw * 0.5 - p.wing * 0.6
        top = max(top, _domed_hall(parts, mats, col, f"{asset_name}_Dershane",
                                   0.0, dy, dw, dw, H + 0.55, dw * 0.36,
                                   drum=0.50))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    depth_total = D + (p.dershane_w - p.wing * 0.6 if p.dershane else 0.0)
    cy = (depth_total - D) * 0.5
    l1 = [_solid(f"{asset_name}_L1", (W, D, H), (0.0, 0.0, H * 0.5), col,
                 mats["stone"]),
          _solid(f"{asset_name}_L1c", (cw, cd, 0.2), (0.0, 0.0, H - 0.1), col,
                 mats["shadow"])]
    if p.dershane:
        l1.append(_solid(f"{asset_name}_L1d",
                         (p.dershane_w * 0.9, p.dershane_w * 0.9,
                          p.dershane_w * 0.36),
                         (0.0, D * 0.5 + p.dershane_w * 0.5 - p.wing * 0.6,
                          H + 0.55 + p.dershane_w * 0.18), col, mats["lead"]))
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (W, depth_total, H),
                      (0.0, cy, H * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                wall_width=round(W, 3), wall_depth=round(depth_total, 3),
                kind="medrese", palette=p.palette, bays=bays,
                hucre=len(cells))
    return lod0, lod1, ucx, info
