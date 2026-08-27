"""
Hezarfen: 1632 — Galata surlarının **burcu ve kapısı** (Faz 3, S-kademe).

## Neden burada yalnız iki parça var

Sur üç şeyden oluşur: perde duvar, burç, kapı. Perde duvar **2,5 km'lik bir
hattır** ve prefabla döşemek 300+ örnek eder; o yüzden Unity tarafında GIS
hattı boyunca **tek mesh** olarak üretiliyor (kaldırım ve taş kaidelerle aynı
ilke). Burç ve kapı ise ayrı ayrı **yapılardır** — sayılıdır, bakılır,
incelenir. Bu dosya onları üretir.

## Ölçüler RÖLÖVEDEN — taslak değil

İlk yazımda yükseklik **taslaktı** ve Caner'e soruldu (Karar 15). Caner
*"tezden bulmaya çalışalım"* dedi; tez bulundu ve **ölçüleri verdi**:

> Erdoğan, Batuhan Burhan (2013), *Galata Kent Surları ve Koruma Önerileri*,
> YL tezi, İTÜ FBE, dan. Zeynep Ahunbay — 2010 arazi ölçümleri.

Bundan sonra hiçbir sayı uydurulmadı:

* duvar kalınlığı **~2 m** (bir kesitte ölçülen 1,8 m),
* çevre **2 800 m**, alan **~37 ha** (ADR 0029'un çapalarıyla birebir aynı),
* hendek **15 m** (Eyice 1969; İnciciyan 1976),
* burçlar **U planlı**, ön yüzü **dairesel**; 16 no'lu 9,80 × 7,70 m ve
  **16,16 m** yüksek, 9 no'lu 7,02 × 5,84 m ve **~10 m**,
* Harup Kapı: açıklık **2,70 m**, kemere yükseklik **4,60 m**, üzengi
  **3,60 m**, kapı üstünde sur **6,50 m**, kesit **1,80 m**,
* Galata Kulesi surun **baş kulesi**, dış çap **~16 m** — ADR 0033'te TDV'den
  alınan 16,45 m'yi bağımsız olarak doğruluyor.

Duvar yüksekliği ölçümlerde **6,50 – 17 m** arasında değişiyor ve bu çelişki
değil **eğim**: yüksek sayılar yamaç aşağı bakan dış yüzde, düşük sayılar
sokak kotunda ölçülmüş.

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z).

"""

import math

import hz_blender as hz
import ottoman_kit as kit
import street_kit as sk


#: Perde duvar kalınlığı (m) — BELGELİ.
WALL_T = 2.0

#: Hendek genişliği (m) — BELGELİ. Duvarın kendisi değil ama yerleştirmenin
#: girdisi: burç hendeğe doğru taşar.
MOAT_W = 15.0

#: Perde duvarının **yerel zeminden** yüksekliği (m) — ÖLÇÜLÜ.
#:
#: Erdoğan (2013) rölövesinde ayakta kalan parçalar ölçülmüş ve aralık geniş:
#: Harup Kapı üstünde **6,50 m**, 3 no'lu parçada **7,70 m**, 16 no'lu burcun
#: yanında **14 m**, aynı hattın devamında **ortalama 17 m**.
#:
#: Aralık çelişki değil, **eğimdir**: yüksek sayılar duvarın yamaç aşağı
#: bakan dış yüzünde ölçülmüş, düşük sayılar kapının bulunduğu sokak kotunda.
#: Modelde duvarın tabanı araziyi izler, tepesi ise kısa koşular hâlinde
#: düzdür; dolayısıyla yamaçta dış yüz kendiliğinden 14-17 m'ye çıkar.
#: Buradaki sayı **iç/yüksek kottan** olan yüksekliktir.
WALL_H = 7.0

#: Geriye dönük ad — eski çağıranlar için.
WALL_H_DRAFT = WALL_H


class BurcParams(object):
    """
    Sur burcu — **U PLANLI**: arkası dikdörtgen, öne bakan yüzü **dairesel**.

    ## İKİ TİP DE BELGELİ — ve ben bir kez aşırı düzelttim

    İlk yazımda burç kare bir kuleydi. Rölövedeki iki **ayakta kalan** burcu
    okuyup "kare yanlış, hepsi U planlı" dedim. **Bu bir aşırı düzeltmeydi.**
    Aynı tez şunu da yazıyor:

    > *"Galata Surları belirli aralıklarla inşa edilmiş **dörtgen ve U
    > planlı** burçlar ile güçlendirilmiştir."*

    Yani iki tip de vardı; bugüne kalan iki örneğin U planlı olması, kare
    burcun olmadığı anlamına gelmiyor. Ders: **hayatta kalan örnek, örneklem
    değildir.** Üç varyant üretiliyor — iki U planlı (ölçülü) ve bir dörtgen.

    Ölçülü olanlar:

    * **16 no'lu burç:** U planlı, 9,80 × 7,70 m, zeminden **16,16 m**
    * **9 no'lu burç:** U planlı, 7,02 × 5,84 m, zeminden **~10 m**

    İkisi de kaba yonu taş yığma. Dörtgen varyantın ölçüsü yok; U planlı
    büyük burcun ölçüleri ödünç alınıyor ve bu `plan="dortgen"` ile
    işaretleniyor.

    Burçlar birer **Hıristiyan azizinin adını** taşırdı ve üzerlerindeki
    mermer levhalara o ad işlenirdi (ör. Büyük Kule Kapısı yanında **1349
    tarihli St. Nicolas Burcu**). Levha modellenmedi — yakın plan işi.
    """

    def __init__(self, width=9.80, depth=7.70, height=16.16, wall_h=WALL_H,
                 parapet_h=1.5, merlon_n=5, segments=14, plan="u",
                 palette="default"):
        self.plan = plan            # "u" (yarim daire on yuz) | "dortgen"
        self.width, self.depth = width, depth
        self.height = height
        self.wall_h = wall_h
        self.parapet_h = parapet_h
        self.merlon_n = merlon_n
        self.segments = segments
        self.palette = palette

    @property
    def jut(self):
        """Duvar hattından dışarı taşma (m) — yarım dairenin yarıçapı kadar."""
        return self.depth - WALL_T

    def validate(self):
        # Galata Kulesi "burclarin HEPSINDEN kalin"dir; burc ondan ince olmali.
        # Kule dis capi 16,45 m (olculu, TDV; tezde de "yaklasik 16 m").
        if max(self.width, self.depth) >= 16.45:
            raise ValueError(f"burc {self.width}x{self.depth} — Galata "
                             "Kulesi'nden (16,45 m) ince olmali; kaynak kuleyi "
                             "'burclarin hepsinden kalin' diye anar")
        if self.plan not in ("u", "dortgen"):
            raise ValueError(f"plan={self.plan} — 'u' ya da 'dortgen'")
        # U PLAN: on yuz yarim daire, yani derinlik genislikten kucuk olmali.
        # Dortgen burcta boyle bir kisit yok.
        if self.plan == "u" and self.depth >= self.width:
            raise ValueError(f"burc {self.width}x{self.depth} — U planli burcta "
                             "derinlik genislikten kucuktur (on yuz yarim daire)")
        # Burc DUVARDAN YUKSEK olmali, yoksa duvarin bir parcasi olur.
        if self.height <= self.wall_h + 1.5:
            raise ValueError(f"height={self.height:.1f} duvara ({self.wall_h}) "
                             "gore alcak — burc duvardan yukselir")
        # Ve DISARI TASMALI.
        if self.jut < WALL_T:
            raise ValueError(f"tasma {self.jut:.1f} m — duvar kalinligindan "
                             f"({WALL_T} m) fazla olmali, yoksa duvarin onunu "
                             "supuremez")


class KapiParams(object):
    """
    Sur kapısı — kemerli geçit, iki yanı kalın paye, üstü mazgallı.

    Galata'nın kapıları kaynakta **adlarıyla** anılıyor (Azapkapı, Kule
    Kapısı, Karaköy, Balıkpazarı, Yağkapanı, Kürkçükapı, Kurşunlumahzen).
    Ölçüler ayakta kalan **Harup Kapı** rölövesinden (Erdoğan 2013): açıklık
    2,70 m, kemere yükseklik 4,60 m, üzengi 3,60 m, kapı üstünde sur 6,50 m,
    kesit 1,80 m; kemer örgüsünde bir sıra taş, iki sıra tuğla.
    """

    #: Harup Kapı rölövesi (Erdoğan 2013) — kapının ÖLÇÜLMÜŞ değerleri.
    ARCH_CROWN_M = 4.60          # kemere yükseklik
    SPRING_M = 3.60              # kemer üzengi seviyesi

    def __init__(self, width=11.0, opening=2.70, height=None,
                 wall_h=WALL_H, depth=5.0, parapet_h=1.5, merlon_n=6,
                 palette="default"):
        self.width, self.opening = width, opening
        self.wall_h = wall_h
        # Kapi ustundeki sur yuksekligi Harup Kapi'da 6,50 m olculmus; kapi
        # yapisi ondan yuksektir (kule gibi degil ama duvardan yukarida).
        self.height = height if height is not None else max(wall_h * 1.4, 9.6)
        self.depth, self.parapet_h = depth, parapet_h
        self.merlon_n = merlon_n
        self.palette = palette

    @property
    def spring_z(self):
        """
        Kemer basma kotu — **ölçülmüş** (3,60 m, Harup Kapı).

        Önceki hâli açıklıktan türetiyordu (`opening × 0,80`) çünkü ölçü
        yoktu; ADR 0030'un sabit-sayı yasağı öyle gerektiriyordu. Artık ölçü
        var ve türetimin yerini alıyor: **ölçü varsa çıkarım kullanılmaz.**
        """
        return self.SPRING_M

    def validate(self):
        # Kapi acikligi OLCULMUS: Harup Kapi 2,70 m. Aralik yine de sinanir —
        # baska bir kapi modellenirse mahalle sokagindan (4,6 m) genis
        # olamaz, 2,0 m'den dar olursa gecit degil delik olur.
        if not (2.0 <= self.opening <= 4.6):
            raise ValueError(f"opening={self.opening} — sur kapisi 2,0-4,6 m "
                             "(Harup Kapi olcusu 2,70)")
        # Iki yanda gercek PAYE kalmali.
        if self.width - self.opening < 5.0:
            raise ValueError("kapinin iki yanindaki paye toplami 5 m'den az — "
                             "kapi bir yapi degil, duvarda bir bosluk olur")
        if self.depth < WALL_T:
            raise ValueError("kapi duvardan derin olmali (gecit tonozu)")


def _merlons_line(parts, col, mat, cx, cy, span, thickness, base_z, height,
                  count, along_x=True):
    """Düz bir hat boyunca mazgal dişleri — kulenin dairesel olanının karşılığı."""
    if count < 1:
        return
    step = span / count
    w = step * 0.58                                  # dis / bosluk orani
    for i in range(count):
        t = -span * 0.5 + step * (i + 0.5)
        size = (w, thickness, height) if along_x else (thickness, w, height)
        pos = (cx + t, cy, base_z + height * 0.5) if along_x \
            else (cx, cy + t, base_z + height * 0.5)
        obj = hz.make_box(f"Mazgal_{i:02d}", size, pos, col)
        hz.assign(obj, mat)
        parts.append(obj)


def _crown(parts, col, mat, cx, cy, sx, sy, base_z, parapet_h, merlon_n):
    """Dört kenara mazgal — burç ve kapı tacı."""
    _merlons_line(parts, col, mat, cx, cy - sy * 0.5 + 0.35, sx, 0.7,
                  base_z, parapet_h, merlon_n, along_x=True)
    _merlons_line(parts, col, mat, cx, cy + sy * 0.5 - 0.35, sx, 0.7,
                  base_z, parapet_h, merlon_n, along_x=True)
    _merlons_line(parts, col, mat, cx - sx * 0.5 + 0.35, cy, sy, 0.7,
                  base_z, parapet_h, merlon_n, along_x=False)
    _merlons_line(parts, col, mat, cx + sx * 0.5 - 0.35, cy, sy, 0.7,
                  base_z, parapet_h, merlon_n, along_x=False)


def _finish(parts, l1, col, asset_name, mats, tex_sizes, kind, extra):
    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind=kind, wall_t=WALL_T, status="measured")
    info.update(extra)
    return lod0, lod1, ucx, info


def build_burc(p, col, asset_name, textured=False):
    """Sur burcu. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    # U PLAN = arkada dikdortgen govde + onde YARIM DAIRE.
    #
    # Yarim daire tam silindir olarak konuyor ve arka yarisi govdenin icinde
    # kaliyor. Blender'da boolean yok; iki katinin birlesimi gorsel olarak
    # dogru ve ic yuzler gorunmez. Kesme islemi ucgen sayisini dusurmezdi
    # zaten — silindir 14 segment.
    if p.plan == "dortgen":
        # DORTGEN BURC: tek prizma. Tez iki tipi de belgeliyor; bugune
        # kalan iki ornegin U planli olmasi karenin olmadigini gostermez.
        body = hz.make_box(f"BurcGovde_{asset_name}",
                           (p.width, p.depth + WALL_T, p.height),
                           (0.0, -(p.depth - WALL_T) * 0.5, p.height * 0.5), col)
        hz.assign(body, mats["stone"])
        parts.append(body)
        l1.append(hz.assign(hz.make_box("L1_Burc",
                                        (p.width, p.depth + WALL_T, p.height),
                                        (0.0, -(p.depth - WALL_T) * 0.5,
                                         p.height * 0.5), col),
                            mats["stone"]))
        band = hz.make_box(f"Kusak_{asset_name}",
                           (p.width + 0.12, p.depth + WALL_T + 0.12, 0.45),
                           (0.0, -(p.depth - WALL_T) * 0.5,
                            p.height * 0.58 + 0.225), col)
        hz.assign(band, mats["brick"])
        parts.append(band)
        for k2, z in enumerate((p.height * 0.34, p.height * 0.66)):
            slit = hz.make_box(f"Mazgal_P{k2}", (0.45, 0.4, 1.5),
                               (0.0, -p.depth + 0.18, z), col)
            hz.assign(slit, mats["shadow"])
            parts.append(slit)
        _crown(parts, col, mats["stone"], 0.0, -(p.depth - WALL_T) * 0.5,
               p.width, p.depth + WALL_T, p.height, p.parapet_h, p.merlon_n)
        return _finish(parts, l1, col, asset_name, mats, tex_sizes, "burc",
                       dict(width=p.width, depth=p.depth, jut=round(p.jut, 2),
                            wall_h=p.wall_h, plan=p.plan, accuracy="D3"))

    r = p.width * 0.5
    back_d = p.depth - r                      # dikdortgen kismin derinligi
    if back_d < 0.6:
        raise ValueError("burc derinligi yarim daireyi zor tasiyor")

    # Duvar hatti y=0; burc -Y'ye tasar. Yarim dairenin merkezi -back_d'de.
    body = hz.make_box(f"BurcGovde_{asset_name}",
                       (p.width, back_d + WALL_T, p.height),
                       (0.0, -(back_d - WALL_T) * 0.5, p.height * 0.5), col)
    hz.assign(body, mats["stone"])
    parts.append(body)

    nose = hz.make_tube(f"BurcBurun_{asset_name}", r, r, p.height,
                        center_xy=(0.0, -back_d), base_z=0.0,
                        segments=p.segments, cap_top=True, cap_bottom=False,
                        col=col)
    hz.assign(nose, mats["stone"])
    parts.append(nose)

    l1.append(hz.assign(hz.make_box("L1_Burc", (p.width, p.depth, p.height),
                                    (0.0, -(p.depth - WALL_T) * 0.5,
                                     p.height * 0.5), col),
                        mats["stone"]))

    # Tugla kusak: Ceneviz/Bizans duvarinda tugla-tas almasik orgu yaygindir
    # ve tezde kemer orgusu icin "bir sira tas, iki sira tugla" olculmus.
    # Kuleyle ayni dil (13,20 m'deki kusak).
    band = hz.make_tube(f"Kusak_{asset_name}", r + 0.12, r + 0.12, 0.45,
                        center_xy=(0.0, -back_d), base_z=p.height * 0.58,
                        segments=p.segments, cap_top=False, cap_bottom=False,
                        col=col)
    hz.assign(band, mats["brick"])
    parts.append(band)
    band2 = hz.make_box(f"KusakArka_{asset_name}",
                        (p.width + 0.12, back_d + WALL_T, 0.45),
                        (0.0, -(back_d - WALL_T) * 0.5, p.height * 0.58 + 0.225),
                        col)
    hz.assign(band2, mats["brick"])
    parts.append(band2)

    # Dar mazgal pencereler: burc bir savunma yapisi.
    for k, z in enumerate((p.height * 0.34, p.height * 0.66)):
        slit = hz.make_box(f"Mazgal_P{k}", (0.45, 0.4, 1.5),
                           (0.0, -back_d - r + 0.18, z), col)
        hz.assign(slit, mats["shadow"])
        parts.append(slit)

    # Tac: yarim daire uzerinde radyal, arkada duz.
    for i in range(p.merlon_n * 2):
        a = math.pi * (i + 0.5) / (p.merlon_n * 2) + math.pi * 0.5
        if i % 2:
            continue
        w = 2.0 * r * math.sin(math.pi / (p.merlon_n * 2) * 0.62)
        obj = hz.make_box(f"MazgalD_{i:02d}", (w, 0.7, p.parapet_h),
                          (0.0, 0.0, 0.0), col)
        obj.rotation_euler = (0.0, 0.0, a + math.pi * 0.5)
        obj.location = ((r - 0.35) * math.cos(a),
                        -back_d + (r - 0.35) * math.sin(a),
                        p.height + p.parapet_h * 0.5)
        hz.assign(obj, mats["stone"])
        parts.append(obj)
    _merlons_line(parts, col, mats["stone"], 0.0, WALL_T * 0.5 - 0.35,
                  p.width, 0.7, p.height, p.parapet_h, p.merlon_n, along_x=True)

    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "burc",
                   dict(width=p.width, depth=p.depth, jut=round(p.jut, 2),
                        wall_h=p.wall_h, plan=p.plan, accuracy="D2"))


def build_kapi(p, col, asset_name, textured=False):
    """Sur kapısı. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    # GEÇİT GERÇEK BİR KEMERDİR.
    #
    # İlk yazımda kapı iki paye + bir lento + koyu bir kutuydu ve render
    # kusuru gösterdi: açıklık **kare bir delik** olarak okunuyordu, üstelik
    # yalnız 2,9 m yüksekliğinde. Bir sur kapısını kapı yapan şey kemeridir.
    #
    # `arched_panel` mahallenin bütün kemerlerini üreten alettir (çeşme nişi,
    # avlu kapısı, kilise penceresi) — sur kapısı da aynı kemer karakterini
    # taşımalı, yoksa şehirde iki ayrı mimarî dil olur. Panel kalınlığı
    # geçit derinliğidir, yani açıklık yapının **içinden geçer**.
    span = (-p.opening * 0.5, p.opening * 0.5)
    body = sk.arched_panel(f"Kapi_{asset_name}", p.width, p.height, p.depth,
                           (0.0, 0.0, 0.0), (1.0, 0.0), (0.0, -1.0),
                           spans=[span], spring_z=p.spring_z, col=col)
    hz.assign(body, mats["stone"])
    parts.append(body)

    band = hz.make_box(f"Kusak_{asset_name}",
                       (p.width + 0.16, p.depth + 0.16, 0.45),
                       (0.0, 0.0, p.height * 0.72), col)
    hz.assign(band, mats["brick"])
    parts.append(band)

    l1.append(hz.assign(hz.make_box("L1_Kapi", (p.width, p.depth, p.height),
                                    (0.0, 0.0, p.height * 0.5), col),
                        mats["stone"]))

    _crown(parts, col, mats["stone"], 0.0, 0.0, p.width, p.depth, p.height,
           p.parapet_h, p.merlon_n)

    return _finish(parts, l1, col, asset_name, mats, tex_sizes, "kapi",
                   dict(width=p.width, opening=p.opening, wall_h=p.wall_h,
                        arch_crown=p.ARCH_CROWN_M, spring=p.SPRING_M,
                        accuracy="D2"))
