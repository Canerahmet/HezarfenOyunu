"""
Theodosius **kara surları** — 5. yüzyıl; 1632'de ayakta. Faz 3, A-kademe.

## Bu kit neden `wall_kit`ten ayrı

`wall_kit` **Galata** surlarını kurar ve doğrulamaları Galata'ya özgü
olgulardır: *"burç Galata Kulesi'nden (16,45 m) ince olmalı — kaynak
kuleyi 'burçların hepsinden kalın' diye anar."* O cümle Theodosius
surları için hiçbir şey söylemez ve oradaki burçlar başka bir ailedir.

Ayasofya'da verilen kararın aynısı: doğrulama kuralları **olgu taşır**;
bir kiti başka bir yapıya uzatmak, o olguları da sessizce taşır.

## Ölçüler

| | | |
|---|---|---|
| İç sur | **4,5–6 m** kalın, **12 m** yüksek | belgeli |
| İç sur burcu | **96** adet, **25 m** yüksek | **sayılan** |
| Burç aralığı | 21–77 m, çoğu **40–60** | belgeli |
| Burç planı | **çoğunlukla kare**; bazıları sekizgen, altıgen, beşgen | belgeli |
| Dış sur | taban **2 m**, **8,5–9 m** yüksek | belgeli |
| Hendek | **20+ m** geniş, **10 m** derin | belgeli |
| **Toplam savunma derinliği** | **70 m** | belgeli |
| Uzunluk | **7,5 km** | belgeli |

**Toplam 70 m ölçülüdür, dağılımı değildir.** Hendek 20 + parateikhion 17
+ dış sur 2 + peribolos 20 + iç sur 5 + glasi 6 = 70. Ara ölçüler
tipolojiktir (**D3**); toplamları belgeli sayıya **oturmak zorundadır** ve
`validate` bunu denetler. Bu, "kaynak niteliksel olduğunda metrik geometri
uydurma" kuralının burada aldığı biçim: **uydurulan sayı yok, paylaşılan
bir toplam var.**

## Burç planı: hayatta kalan örnek örneklem değildir

Galata'da bu dersi bir kez almıştım (ADR 0034): ayakta kalan iki burcun
U planlı olması karenin olmadığı anlamına gelmiyordu. Burada kaynak
doğrudan söylüyor — *"çoğunlukla kare, bazıları sekizgen, altıgen ve
beşgen"* — yani **tek tip üretmek belgeye aykırı** olurdu. İki plan
üretiliyor: kare (çoğunluk) ve sekizgen.

## 1632'de

Surlar Osmanlı döneminde bakımlıydı ve **1632'de ayakta**. Yıkımlar
19.-20. yüzyıldır. Yedikule Hisarı'nı **Fatih 1457'de** Altın Kapı'nın
arkasına, **yedi kuleli** olarak yaptırdı — 1632'de 175 yaşında.

**Kaynaklar**: Vikipedi "Konstantinopolis Surları" (ölçüler); Koç
Üniversitesi İstanbul Surları projesi; Alan Başkanlığı. RESEARCH.md §5.15,
ADR 0049.
"""

import math

import hz_blender as hz
import ottoman_kit as kit
import detay_kit as dk
import street_kit as sk


# ---------------------------------------------------------------- ölçüler

#: İç sur: kalınlık (4,5-6 m aralığının ortası) ve yükseklik (m).
KS_IC_T, KS_IC_H = 5.0, 12.0

#: İç sur burçları: **doksan altı** adet, **25 m** yüksek. Sayı belgeli;
#: aralığı bu sayıdan ve hattın **ölçülen** uzunluğundan türer — elle
#: girilen bir "burçlar arası mesafe" yoktur.
#:
#: 25 m **toplam** yüksekliktir, gövde değil. Ayakta duran bir yapının
#: yayımlanan yüksekliği zeminden tepesine ölçülür; korkuluk ve mazgal
#: dişi o sayının içindedir. İlk kurulumda 25'i gövdeye verdim ve mesh
#: 28,0 m çıktı — kendi denetimim yakaladı (25/12 = 2,08 beklerken 2,33).
KS_TOWER_N, KS_TOWER_H = 96, 25.0

#: Burç plan ölçüsü (m) — **D3**. Kaynak sayı ve yükseklik verir, en
#: vermez; 25 m'lik bir kule için tipolojik oran.
KS_TOWER_W = 10.0

#: Dış sur: taban kalınlığı ve yükseklik (8,5-9 m aralığının ortası).
KS_DIS_T, KS_DIS_H = 2.0, 8.75

#: Dış sur burcu — iç surunkinden alçak ve küçük. **Kaynak buna yükseklik
#: vermiyor (D3).** İlk denemede 12 m yazdım ve doğrulama reddetti: gövde
#: 9,0 m, dış sur 8,75 m — burç duvarın 25 cm üstünde kalıyordu, yani
#: burç değil duvarın bir parçası oluyordu. 14 m, burcun korkuluğu
#: aşıp hendeği görebildiği en alçak değerdir.
KS_DIS_TOWER_W, KS_DIS_TOWER_H = 6.0, 14.0

#: Hendek (m).
KS_MOAT_W, KS_MOAT_D = 20.0, 10.0

#: **Toplam savunma derinliği (m) — belgeli.** Ara ölçülerin toplamı buna
#: eşit olmak zorundadır.
KS_TOTAL_DEPTH = 70.0

#: Kesitin dağılımı (m), hendekten içeri doğru. Toplamı KS_TOTAL_DEPTH.
KS_GLACIS = 6.0
KS_PARATEIKHION = 17.0
KS_PERIBOLOS = 20.0


def section_total():
    """Kesit toplamı — belgeli 70 m'ye eşit olmalı."""
    return (KS_GLACIS + KS_MOAT_W + KS_PARATEIKHION + KS_DIS_T
            + KS_PERIBOLOS + KS_IC_T)


class KaraSurBurcuParams(object):
    """
    Theodosius surlarının burcu.

    `plan="kare"` çoğunluktur; `plan="sekizgen"` kaynağın saydığı öteki
    tiptir. Tek tip üretmek belgeye aykırı olurdu.

    Yükseklik **25 m** ve bu sayı iç surdan (12 m) **iki kat** yüksek
    demektir: burç duvarın üstünde bir kule değil, duvarı **aşan** bir
    kütledir. Silueti belirleyen şey bu orandır.
    """

    def __init__(self, width=KS_TOWER_W, height=KS_TOWER_H, wall_h=KS_IC_H,
                 wall_t=KS_IC_T, plan="kare", parapet_h=1.6, merlon_n=4,
                 merlon_h=1.4, palette="default"):
        #: `height` **TOPLAM** yüksekliktir (zeminden mazgal tepesine);
        #: gövde ondan türer.
        self.width, self.height = width, height
        self.merlon_h = merlon_h
        self.wall_h, self.wall_t = wall_h, wall_t
        self.plan = plan
        self.parapet_h, self.merlon_n = parapet_h, merlon_n
        self.palette = palette

    @property
    def body_h(self):
        """Gövde = toplam − korkuluk − mazgal dişi."""
        return self.height - self.parapet_h - self.merlon_h

    @property
    def jut(self):
        """Duvar hattından dışa taşma (m)."""
        return (self.width - self.wall_t) * 0.5

    def validate(self):
        if self.plan not in ("kare", "sekizgen"):
            raise ValueError(f"plan={self.plan} — kaynak 'cogunlukla KARE, "
                             "bazilari SEKIZGEN' der")
        # Burc duvari ASMALI — ama NE KADAR asacagi kaynaktan gelmez.
        #
        # Ilk yazimda burada "duvarin 1,6 kati" yaziyordu ve gerekcesi
        # "kaynak burcu 25, ic suru 12 m verir" idi. Kural DIS SUR burcunda
        # patladi (12 / 8,75 = 1,37) ve hakli patladi ama YANLIS YERDE:
        # 25/12 orani **ic surun** olgusudur, kaynak dis sur burcuna hic
        # yukseklik vermiyor.
        #
        # Bu kiti `wall_kit`ten ayirma sebebimin aynisini kendi icimde
        # tekrarlamisim: bir olguyu, gecerli oldugu yapidan baska bir
        # yapiya sessizce tasimak. Genel kural artik yalnizca "burc
        # duvardan BELIRGIN yuksek olmali" der; 25/12 orani ait oldugu yerde,
        # ureticinin IC SUR denetiminde durur.
        if self.body_h < self.wall_h + 1.5:
            raise ValueError(
                f"burc govdesi {self.body_h:.1f} m, duvar "
                f"{self.wall_h:.1f} m — burc duvardan belirgin yukselmeli, "
                "yoksa duvarin bir parcasi olur")
        if self.jut < self.wall_t * 0.4:
            raise ValueError(f"tasma {self.jut:.1f} m — burc duvarin onunu "
                             "supurebilmeli")
        return self


def _merlons(parts, mats, col, name, cx, cy, w, d, base_z, n, h=1.4):
    """Mazgal dişleri — dört kenar boyunca."""
    step_x, step_y = w / n, d / n
    for i in range(n):
        ux = cx - w * 0.5 + step_x * (i + 0.5)
        uy = cy - d * 0.5 + step_y * (i + 0.5)
        for pos, size in (((ux, cy - d * 0.5 + 0.35), (step_x * 0.62, 0.7, h)),
                          ((ux, cy + d * 0.5 - 0.35), (step_x * 0.62, 0.7, h)),
                          ((cx - w * 0.5 + 0.35, uy), (0.7, step_y * 0.62, h)),
                          ((cx + w * 0.5 - 0.35, uy), (0.7, step_y * 0.62, h))):
            parts.append(hz.assign(
                hz.make_box(f"{name}_Mazgal_{i}_{pos[0]:.0f}_{pos[1]:.0f}",
                            size, (pos[0], pos[1], base_z + h * 0.5), col),
                mats["stone"]))


def build_kara_sur_burcu(p, col, asset_name, textured=False):
    """Kara surları burcu. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    w = p.width
    body = p.body_h

    if p.plan == "kare":
        parts.append(hz.assign(hz.make_box(f"Govde_{asset_name}",
                                           (w, w, body),
                                           (0.0, 0.0, body * 0.5), col),
                               mats["stone"]))
        top_w = w
    else:
        # SEKIZGEN: kaynak "bazilari sekizgen" der. Cokgen govde, kare
        # olanla ayni ayak izine oturur.
        parts.append(hz.assign(
            hz.make_tube(f"Govde_{asset_name}", w * 0.54, w * 0.51, body,
                         (0.0, 0.0), 0.0, segments=8,
                         phase=math.pi / 8.0, col=col), mats["stone"]))
        top_w = w * 0.98

    # ALMASIK ORGU: Bizans duvarinda tas siralari arasinda TUGLA kusaklari
    # vardir ve uzaktan okunan sey odur. Galata'da (ADR 0033) kusagin
    # anlaminin RENGINDE oldugu olculmustu; ayni malzeme rolu kullaniliyor.
    for i in range(4):
        z = body * (i + 1) / 5.0
        parts.append(hz.assign(
            hz.make_box(f"TuglaKusak_{asset_name}_{i}",
                        (top_w + 0.12, top_w + 0.12, 0.55), (0.0, 0.0, z),
                        col), mats["brick"]))

    # KONSOL SIRASI + SILME: korkuluk govdeden 0,35 m tasar ve o tasmayi
    # bir kademe tasir. 192 ornek basilacagi icin ucgen butcesi dar
    # (<1500); bu yuzden konsol DIZISI degil, iki kademeli bir silme —
    # ayni golge cizgisini otuz ucgene mal olmadan verir.
    for o in dk.silme(f"KorkulukSilme_{asset_name}", top_w + 0.7,
                      top_w + 0.7, body, col, steps=2, h=0.34, out=0.35,
                      ters=True):
        parts.append(hz.assign(o, mats["stone"]))

    # MAZGAL DELIKLERI: burc bir savunma yapisidir ve gozu ok mazgalidir.
    # Pencereler ust katta; bunlar alt katta ve DAR.
    for sgn in (-1, 1):
        for ax in (0, 1):
            pos = ((sgn * (top_w * 0.5 - 0.20), 0.0) if ax == 0
                   else (0.0, sgn * (top_w * 0.5 - 0.20)))
            size = (0.5, 0.28, 1.5) if ax == 0 else (0.28, 0.5, 1.5)
            parts.append(hz.assign(
                hz.make_box(f"Mazgal_{asset_name}_{ax}{sgn}", size,
                            (pos[0], pos[1], body * 0.34), col),
                mats["shadow"]))

    # Korkuluk + mazgal
    parts.append(hz.assign(hz.make_box(f"Korkuluk_{asset_name}",
                                       (top_w + 0.7, top_w + 0.7, p.parapet_h),
                                       (0.0, 0.0, body + p.parapet_h * 0.5),
                                       col), mats["stone"]))
    _merlons(parts, mats, col, asset_name, 0.0, 0.0, top_w + 0.7, top_w + 0.7,
             body + p.parapet_h, p.merlon_n, h=p.merlon_h)

    # Pencereler: burcun ust katinda, her yuzde birer.
    for sgn in (-1, 1):
        for ax in (0, 1):
            pos = ((sgn * (top_w * 0.5 - 0.25), 0.0) if ax == 0
                   else (0.0, sgn * (top_w * 0.5 - 0.25)))
            size = (0.6, 1.5, 2.4) if ax == 0 else (1.5, 0.6, 2.4)
            parts.append(hz.assign(
                hz.make_box(f"Pencere_{asset_name}_{ax}{sgn}", size,
                            (pos[0], pos[1], body * 0.72), col),
                mats["shadow"]))
            # Bizans burcunun kemeri YUVARLAKTIR (Osmanli sivrisi degil).
            ux, uy = (0.0, 1.0) if ax == 0 else (1.0, 0.0)
            for o in dk.kemer(f"PencKemer_{asset_name}_{ax}{sgn}",
                              pos[0], pos[1], ux, uy, 0.75,
                              body * 0.72 + 1.2, 0.22, 0.55, col,
                              steps=4, sivri=False):
                parts.append(hz.assign(o, mats["stone"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}", (w, w, p.height),
                                    (0.0, 0.0, p.height * 0.5), col),
                        mats["stone"]))
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
                kind="karasur_burc", plan=p.plan, palette=p.palette,
                status="draft", accuracy="D2",
                width=p.width, wall_h=p.wall_h, wall_t=p.wall_t,
                body_h=round(p.body_h, 2),
                tower_count=KS_TOWER_N, jut=round(p.jut, 2),
                section_total=round(section_total(), 1),
                moat_w=KS_MOAT_W, moat_d=KS_MOAT_D)
    return lod0, lod1, ucx, info


# ======================================================== Kara sur kapısı

#: Geçit açıklığı (m) — **D3**. Kaynak kara sur kapılarına ölçü vermiyor;
#: Galata'nın Harup Kapı rölövesi 2,70 m'dir ama o **2 m** kalınlığında
#: bir duvarın kapısıdır. Burada duvar **5 m** ve burçlar **25 m**; aynı
#: açıklık bu kütlede bir mazgal deliği gibi okunurdu.
#:
#: Galata kapısını buraya koymamak bilinçliydi (ADR 0049) ve ölçüsünü de
#: kopyalamıyorum: açıklık duvar kalınlığından türedi (5 m geçit derinliği
#: için 4,5 m açıklık, yani bir at arabasının geçebileceği en dar ölçü).
KSK_OPENING = 4.5
#: Kapi blogunun genisligi (m). Aciklik + iki burc sigmali; ilk deneme
#: 20 m'ydi ve dogrulama reddetti (4,5 + 2x9,0 = 22,5).
KSK_WIDTH = 24.0
KSK_TOWER_W, KSK_TOWER_H = 9.0, 22.0


class KaraSurKapisiParams(object):
    """
    Theodosius surlarının kara kapısı — Topkapı, Edirnekapı, Silivrikapı…

    Kapı **kendi burçlarıyla gelir**: gerçek kara sur kapıları iki burcun
    arasındadır ve kapıyı kapı yapan şey o iki kütledir. `LandWallBuilder`
    hat boyunca zaten 60,7 m'de bir burç koyuyor; kapı onlardan bağımsız
    kendi çiftini taşır, yoksa kapı yalnızca "duvarda bir delik" olur.
    """

    def __init__(self, width=KSK_WIDTH, opening=KSK_OPENING,
                 wall_h=KS_IC_H, wall_t=KS_IC_T,
                 tower_w=KSK_TOWER_W, tower_h=KSK_TOWER_H,
                 parapet_h=1.6, palette="default"):
        self.width, self.opening = width, opening
        self.wall_h, self.wall_t = wall_h, wall_t
        self.tower_w, self.tower_h = tower_w, tower_h
        self.parapet_h = parapet_h
        self.palette = palette

    @property
    def spring_z(self):
        """Kemer uzengisi — açıklığın yarıçapı kadar yukarıda."""
        return self.opening * 0.9

    def validate(self):
        if self.opening >= self.wall_t * 1.2:
            raise ValueError(
                f"aciklik {self.opening:.1f} m, duvar {self.wall_t:.1f} m — "
                "gecit duvar kalinligindan cok genis olursa kapi degil "
                "gedik olur")
        if self.width < self.opening + 2.0 * self.tower_w:
            raise ValueError(
                f"kapi blogu {self.width:.1f} m — aciklik ve iki burc "
                "sigmiyor")
        if self.tower_h <= self.wall_h + 3.0:
            raise ValueError("kapi burclari duvardan belirgin yuksek olmali")
        return self


def build_kara_sur_kapisi(p, col, asset_name, textured=False):
    """Kara sur kapısı. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    # GECIT GERCEK BIR KEMERDIR (Galata dersi, ADR 0034): kare bir delik
    # kapi gibi okunmaz. `arched_panel` sehrin tek kemer karakterini tasir.
    span = (-p.opening * 0.5, p.opening * 0.5)
    body = sk.arched_panel(f"Kapi_{asset_name}", p.width, p.wall_h, p.wall_t,
                           (0.0, 0.0, 0.0), (1.0, 0.0), (0.0, -1.0),
                           spans=[span], spring_z=p.spring_z, col=col)
    hz.assign(body, mats["stone"])
    parts.append(body)

    # Almasik orgu kusaklari — Bizans duvarinin okunan deseni.
    for i in range(3):
        parts.append(hz.assign(
            hz.make_box(f"TuglaKusak_{asset_name}_{i}",
                        (p.width + 0.14, p.wall_t + 0.14, 0.5),
                        (0.0, 0.0, p.wall_h * (i + 1) / 4.0), col),
            mats["brick"]))

    # Korkuluk + mazgal
    parts.append(hz.assign(hz.make_box(f"Korkuluk_{asset_name}",
                                       (p.width, p.wall_t + 0.7,
                                        p.parapet_h),
                                       (0.0, 0.0,
                                        p.wall_h + p.parapet_h * 0.5), col),
                           mats["stone"]))

    # IKI BURC — kapiyi kapi yapan sey.
    for sx in (-1, 1):
        x = sx * (p.width * 0.5 - p.tower_w * 0.5)
        body_h = p.tower_h - p.parapet_h - 1.4
        parts.append(hz.assign(
            hz.make_box(f"KapiBurc_{sx}", (p.tower_w, p.tower_w + 2.0, body_h),
                        (x, 0.0, body_h * 0.5), col), mats["stone"]))
        parts.append(hz.assign(
            hz.make_box(f"KapiBurcKorkuluk_{sx}",
                        (p.tower_w + 0.7, p.tower_w + 2.7, p.parapet_h),
                        (x, 0.0, body_h + p.parapet_h * 0.5), col),
            mats["stone"]))
        _merlons(parts, mats, col, f"KapiBurc_{sx}", x, 0.0,
                 p.tower_w + 0.7, p.tower_w + 2.7, body_h + p.parapet_h, 3)
        for i in range(2):
            parts.append(hz.assign(
                hz.make_box(f"KapiBurcPencere_{sx}{i}", (0.6, 1.4, 2.2),
                            (x + sx * (p.tower_w * 0.5 - 0.2),
                             (i - 0.5) * 3.0, body_h * 0.66), col),
                mats["shadow"]))

    l1.append(hz.assign(hz.make_box(f"L1_{asset_name}",
                                    (p.width, p.tower_w + 2.0, p.tower_h),
                                    (0.0, 0.0, p.tower_h * 0.5), col),
                        mats["stone"]))

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
                kind="karasur_kapi", palette=p.palette, status="draft",
                accuracy="D3", width=p.width, opening=p.opening,
                wall_h=p.wall_h, wall_t=p.wall_t, towers=2,
                height_tower=p.tower_h)
    return lod0, lod1, ucx, info


# ============================================== Yedikule Hisarı (1457-58)

#: **YEDİ** kule — hisarın adı budur ve sayı yapının kendisidir.
#:
#: Üçü Fatih'in **dairesel** kuleleri, dördü Bizans'tan (Altın Kapı'nın iki
#: **mermer** kulesi + Theodosius surunun iki burcu). Kaynak: *"Altın Kapı
#: ve Roma surları tarafından oluşturulan batı bölümü hariç, Fatih Sultan
#: Mehmed döneminde yapılan, dairesel planlı ÜÇ büyük kule ve onları
#: birleştiren ÜÇ uzun beden duvarı"*.
YK_TOWERS, YK_ROUND_TOWERS = 7, 3

#: Açık alan **15 000 m²** (belgeli). Beşgen plandan yarıçap türer:
#: düzgün beşgen alanı 2,378·R² → **R ≈ 79,4 m**, kenar ≈ 93 m.
YK_AREA = 15000.0

#: **ALTIN KAPI: üç kemer.** Ortadaki büyük kemer yalnızca imparatorlara,
#: iki yanındaki küçükler halka. Klasik bir zafer takıdır.
YK_GATE_ARCHES = 3

#: Altın Kapı'nın **mermer kulelerinin** kapı ortasından uzaklığı (m).
#: Üç kemerli kompozisyon ±17 m'ye kadar uzanır; kuleler onu kucaklar.
YK_MARBLE_OFFSET = 22.0

#: Mermer kulelerin duvar düzleminden dışa taşması (m). Bir kuleyi kule
#: yapan şey yükselmesi kadar **taşmasıdır**.
YK_MARBLE_JUT = 3.0

#: Kule yüksekliği (m) — **D3**, kaynak vermiyor. Theodosius burçlarının
#: 25 m'sinden türedi: hisar kuleleri onlardan alçak değildir.
YK_TOWER_H, YK_TOWER_D = 26.0, 15.0

#: Beden duvarı (m) — **D3**.
YK_WALL_H, YK_WALL_T = 15.0, 4.0

#: Altın Kapı'nın baktığı yön (derece, ızgara kuzeyinden) — **ölçüldü**.
#:
#: Hisar surun içindedir ve kapısı **dışa**, yani şehirden uzağa bakar.
#: Sayı elle yazılmadı: sur hattının Yedikule'deki dış normali hesaplandı
#: (şehrin içi deniz surlarının ağırlık merkezinden türer, ADR 0049).
YK_FACE_DEG = 261.2


def yedikule_radius():
    """Beşgenin çevrel yarıçapı — **ölçülen alandan** türer."""
    return math.sqrt(YK_AREA / (2.5 * math.sin(math.radians(72.0))))


class YedikuleParams(object):
    """
    Yedikule Hisarı — **Fatih, 1457-58**; 1632'de 175 yaşında.

    ## 1632 için bu yapı bir HABERDİR

    Kulelerden birinin adı **Genç Osman Kulesi**'dir ve sebebi taze:
    **II. Osman 1622'de burada öldürüldü**. Oyunun geçtiği yıl olaydan
    **on yıl** sonrasıdır ve tahttaki IV. Murad onun kardeşidir. Yedikule
    1632'de bir harabe değil, herkesin bildiği bir yerdir.

    Kulelerden biri **Hazine Kulesi**'dir: hisar devletin hazinesini
    tutar. Bir başkası **Zindan Kulesi**.

    **III. Ahmed Kulesi** adı 1632'de **YOKTUR** — III. Ahmed 1703-1730
    arasında hüküm sürer. Kule vardır, adı sonradandır. Katalog kule
    adlarını değil **sayısını** taşır; ad bir yorumdur, yedi bir olgudur.

    ## Plan

    Beşgen: batı yanı **Altın Kapı ve Theodosius suru**, öteki üç yan
    Fatih'in **dairesel** kuleleri ve onları birleştiren beden duvarları.
    Açık alan **15 000 m²**; beşgenin yarıçapı bu ölçüden türer (79,4 m).
    """

    def __init__(self, towers=YK_TOWERS, round_towers=YK_ROUND_TOWERS,
                 area=YK_AREA, gate_arches=YK_GATE_ARCHES,
                 tower_h=YK_TOWER_H, tower_d=YK_TOWER_D,
                 wall_h=YK_WALL_H, wall_t=YK_WALL_T, palette="default"):
        self.towers, self.round_towers = towers, round_towers
        self.area, self.gate_arches = area, gate_arches
        self.tower_h, self.tower_d = tower_h, tower_d
        self.wall_h, self.wall_t = wall_h, wall_t
        self.palette = palette

    @property
    def radius(self):
        return math.sqrt(self.area / (2.5 * math.sin(math.radians(72.0))))

    def validate(self):
        if self.towers != 7:
            raise ValueError(f"{self.towers} kule — hisarin ADI Yedikule'dir")
        if self.round_towers != 3:
            raise ValueError(
                f"{self.round_towers} dairesel kule — kaynak Fatih'in UC "
                "dairesel kulesini ve onlari birlestiren UC beden duvarini "
                "sayar; kalan dordu Bizans'tandir")
        if self.gate_arches != 3:
            raise ValueError("Altin Kapi UC kemerlidir (ortadaki imparatora)")
        if self.tower_h <= self.wall_h + 5.0:
            raise ValueError("kuleler beden duvarindan belirgin yuksek olmali")
        return self


def build_yedikule(p, col, asset_name, textured=False):
    """Yedikule Hisarı, 1632 hâli. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    R = p.radius

    # BESGEN: bir KENARI -Y'ye baksin (Altin Kapi orada, disa bakar).
    # Kose acilari: kenar ortasi -90 derecede olacak sekilde dondur.
    verts = []
    for i in range(5):
        a = math.radians(-90.0 + 36.0 + 72.0 * i)
        verts.append((R * math.cos(a), R * math.sin(a)))

    # --- Beden duvarlari --------------------------------------------------
    for i in range(5):
        x0, y0 = verts[i]
        x1, y1 = verts[(i + 1) % 5]
        mx_, my_ = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        dx, dy = x1 - x0, y1 - y0
        L = math.hypot(dx, dy)
        ang = math.atan2(dy, dx)
        # ALTIN KAPI kenari (i == 4, -Y'ye bakan) ayri kurulur.
        if i == 4:
            continue
        wall = hz.make_box(f"BedenDuvar_{i}", (L, p.wall_t, p.wall_h),
                           (0.0, 0.0, p.wall_h * 0.5), col)
        wall.rotation_euler = (0.0, 0.0, ang)
        wall.location = (mx_, my_, 0.0)
        hz.assign(wall, mats["stone"])
        parts.append(wall)
        # Mazgal: duvarin ustunde ritim.
        n = max(3, int(L / 4.0))
        for k in range(n):
            t = (k + 0.5) / n
            px, py = x0 + dx * t, y0 + dy * t
            m = hz.make_box(f"BedenMazgal_{i}_{k}", (2.0, p.wall_t + 0.5, 1.5),
                            (0.0, 0.0, 0.0), col)
            m.rotation_euler = (0.0, 0.0, ang)
            m.location = (px, py, p.wall_h + 0.75)
            hz.assign(m, mats["stone"])
            parts.append(m)

    # --- YEDI KULE --------------------------------------------------------
    #
    # Uc DAIRESEL (Fatih) + dort Bizans kulesi. Sayi hisarin ADIDIR.
    round_at = (0, 1, 2)
    tower_n = 0
    for i, (tx, ty) in enumerate(verts):
        if i in round_at:
            parts.append(hz.assign(
                hz.make_tube(f"Kule_{i}", p.tower_d * 0.5, p.tower_d * 0.47,
                             p.tower_h - 2.0, (tx, ty), 0.0, segments=16,
                             col=col), mats["stone"]))
            # Siperi tasiyan KONSOL SIRASI. Bizans/Osmanli hisar kulesinde
            # siper govdeden tasar ve o tasmanin bir tasiyicisi vardir;
            # onsuz kule silindirden buyumus bir bilezik gibi okunuyordu.
            for o in dk.konsol_dizisi(f"KuleKonsol_{i}", tx, ty,
                                      p.tower_d * 0.5, p.tower_h - 2.9,
                                      col, n=16, out=0.42, h=0.90):
                parts.append(hz.assign(o, mats["stone"]))
            parts.append(hz.assign(
                hz.make_tube(f"KuleKorkuluk_{i}", p.tower_d * 0.55,
                             p.tower_d * 0.55, 2.0, (tx, ty),
                             p.tower_h - 2.0, segments=16, cap_top=False,
                             col=col), mats["stone"]))
            # Yuvarlak kemerli aciklik sirasi: kule Fatih isi ama Bizans
            # geleneginde, kemeri YUVARLAK.
            for k in range(4):
                a = 2.0 * math.pi * k / 4.0 + math.pi / 4.0
                ox, oy = math.cos(a), math.sin(a)
                px, py = tx + ox * p.tower_d * 0.48, ty + oy * p.tower_d * 0.48
                parts.append(hz.assign(hz.make_box(
                    f"KuleAcik_{i}_{k}", (1.0, 1.0, 2.4),
                    (px, py, p.tower_h * 0.62), col), mats["shadow"]))
                for o in dk.kemer(f"KuleAcikKemer_{i}_{k}", px, py,
                                  -oy, ox, 0.5, p.tower_h * 0.62 + 1.2,
                                  0.22, 0.6, col, steps=4, sivri=False):
                    parts.append(hz.assign(o, mats["stone"]))
            for k in range(8):
                a = 2.0 * math.pi * (k + 0.5) / 8.0
                parts.append(hz.assign(
                    hz.make_box(f"KuleMazgal_{i}_{k}", (1.6, 1.0, 1.4),
                                (tx + math.cos(a) * p.tower_d * 0.5,
                                 ty + math.sin(a) * p.tower_d * 0.5,
                                 p.tower_h + 0.7), col), mats["stone"]))
            tower_n += 1
        else:
            # BIZANS kulesi: kare planli, Theodosius burcuyla ayni aile.
            parts.append(hz.assign(
                hz.make_box(f"Kule_{i}", (p.tower_d * 0.8, p.tower_d * 0.8,
                                          p.tower_h - 2.4),
                            (tx, ty, (p.tower_h - 2.4) * 0.5), col),
                mats["stone"]))
            for o in dk.silme_at(f"KuleSilme_{i}", tx, ty,
                                 p.tower_d * 0.88, p.tower_d * 0.88,
                                 p.tower_h - 2.4, col, steps=2, h=0.36,
                                 out=0.34, ters=True):
                parts.append(hz.assign(o, mats["stone"]))
            parts.append(hz.assign(
                hz.make_box(f"KuleKorkuluk_{i}",
                            (p.tower_d * 0.88, p.tower_d * 0.88, 2.4),
                            (tx, ty, p.tower_h - 1.2), col), mats["stone"]))
            for k in range(4):
                sx_, sy_ = ((1, 0), (-1, 0), (0, 1), (0, -1))[k]
                px = tx + sx_ * p.tower_d * 0.40
                py = ty + sy_ * p.tower_d * 0.40
                parts.append(hz.assign(hz.make_box(
                    f"KuleAcik_{i}_{k}",
                    (0.6 if sx_ else 1.1, 1.1 if sx_ else 0.6, 2.2),
                    (px, py, p.tower_h * 0.60), col), mats["shadow"]))
            tower_n += 1
        for k in range(3):
            parts.append(hz.assign(
                hz.make_box(f"KuleTugla_{i}_{k}",
                            (p.tower_d * 0.86, p.tower_d * 0.86, 0.45),
                            (tx, ty, p.tower_h * (k + 1) / 4.0), col),
                mats["brick"]))

    # Bizans kuleleri dorttur ama besgenin yalnizca iki kosesi kaldi;
    # kalan IKISI Altin Kapi'nin iki yanindaki MERMER kulelerdir.
    gx0, gy0 = verts[4]
    gx1, gy1 = verts[0]
    gmx, gmy = (gx0 + gx1) * 0.5, (gy0 + gy1) * 0.5
    gdx, gdy = gx1 - gx0, gy1 - gy0
    gL = math.hypot(gdx, gdy)
    gang = math.atan2(gdy, gdx)
    ux, uy = gdx / gL, gdy / gL
    # MERMER KULELER KAPIYI KUCAKLAR, kosede durmaz.
    #
    # Ilk kurulumda kenarin UCLARINA konmuslardi (gL/2 - 6,3) ve kose
    # kulelerinden 6,3 m otedeydiler — renderda ikisi tek bir yigin gibi
    # okunuyordu. Altin Kapi'nin mermer kuleleri UC KEMERLI kompozisyonun
    # iki yanindadir; onlari kosede tutmak, kapinin kendisini kulesiz
    # birakir.
    # Kuleler duvardan DISA TASAR ve TAM BOYDADIR.
    #
    # Ilk kurulumda duvarla ayni duzlemde ve ondan yalnizca 3 m yuksektiler;
    # ayni malzemeyle birlikte renderda kule degil "duvarin kalin yeri" gibi
    # okunuyorlardi. Bir kuleyi kule yapan sey yukselmesi kadar TASMASIDIR
    # — Galata burcunda da olculen sey buydu (ADR 0034).
    nx, ny = -uy, ux
    if nx * gmx + ny * gmy < 0.0:
        nx, ny = -nx, -ny
    for sgn in (-1, 1):
        mx2 = gmx + ux * sgn * YK_MARBLE_OFFSET + nx * YK_MARBLE_JUT
        my2 = gmy + uy * sgn * YK_MARBLE_OFFSET + ny * YK_MARBLE_JUT
        tw = p.tower_d * 0.78
        kule = hz.make_box(f"MermerKule_{sgn}", (tw, tw, p.tower_h - 2.2),
                           (0.0, 0.0, (p.tower_h - 2.2) * 0.5), col)
        kule.rotation_euler = (0.0, 0.0, gang)
        kule.location = (mx2, my2, 0.0)
        hz.assign(kule, mats["marble"])
        parts.append(kule)
        kor = hz.make_box(f"MermerKuleKorkuluk_{sgn}",
                          (tw + 0.8, tw + 0.8, 2.2),
                          (0.0, 0.0, p.tower_h - 1.1), col)
        kor.rotation_euler = (0.0, 0.0, gang)
        kor.location = (mx2, my2, 0.0)
        hz.assign(kor, mats["marble"])
        parts.append(kor)
        tower_n += 1

    # --- ALTIN KAPI: UC KEMER --------------------------------------------
    #
    # `arched_panel` butun aciklıklarin AYNI olcude olmasini ister (T-kavsagi
    # yok). Ortadaki kemer buyuk, yandakiler kucuk — yani UC AYRI panel.
    # Aracin kendi notu bunu soyluyor: "Farkli olcu isteyen yer ayri panel
    # ister."
    gate_h = p.wall_h + 4.0
    big, small = 6.0, 3.2
    # Kenar boyunca yerlesim (m, kapi ortasindan): panellerin ve dolgularin
    # araliklari CAKISMAZ. Ilk kurulumda yan paneller +-16'da, dolgular ise
    # 14,7-33,3 arasindaydi ve dolgu kemerlerin USTUNU ortuyordu — uc kemerli
    # kapi tek kemerli gorunuyordu.
    panels = ((0.0, 12.0, big), (-12.5, 9.0, small), (+12.5, 9.0, small))
    arches = 0
    for j, (off, pw, op) in enumerate(panels):
        px = gmx + ux * off
        py = gmy + uy * off
        pan = sk.arched_panel(f"AltinKapi_{j}", pw, gate_h, p.wall_t,
                              (0.0, 0.0, 0.0), (1.0, 0.0), (0.0, -1.0),
                              spans=[(-op * 0.5, op * 0.5)],
                              spring_z=op * 0.95, col=col)
        pan.rotation_euler = (0.0, 0.0, gang)
        pan.location = (px, py, 0.0)
        hz.assign(pan, mats["marble"])
        parts.append(pan)
        arches += 1
    # Panellerin arasindaki dolgu.
    # DOLGULAR BOSLUKLARI KAPATIR, PANELLERIN USTUNE BINMEZ.
    #
    # Panel araliklari: orta -6..+6, yan +-8..+-17. Yani kapatilacak iki
    # bosluk 6..8 ve 17..kose. Ilk kurulumda dolgular +-9 ve +-24'teydi:
    # 6..8 acikta kaliyor, 14,7..20,5 ise yan kemerin USTUNE biniyordu ve
    # uc kemerli kapi tek kemerli okunuyordu.
    outer_w = gL * 0.5 - 17.0
    outer_c = 17.0 + outer_w * 0.5
    for off, pw in ((-7.0, 2.0), (+7.0, 2.0),
                    (-outer_c, outer_w), (+outer_c, outer_w)):
        if pw <= 0.2:
            continue
        fx = gmx + ux * off
        fy = gmy + uy * off
        f = hz.make_box(f"KapiDolgu_{off:.0f}", (pw, p.wall_t, gate_h),
                        (0.0, 0.0, gate_h * 0.5), col)
        f.rotation_euler = (0.0, 0.0, gang)
        f.location = (fx, fy, 0.0)
        hz.assign(f, mats["marble"])
        parts.append(f)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_tube("L1_Hisar", R, R, p.wall_h, (0.0, 0.0),
                                     0.0, segments=5, col=col),
                        mats["stone"]))
    for i, (tx, ty) in enumerate(verts):
        l1.append(hz.assign(
            hz.make_tube(f"L1_Kule{i}", p.tower_d * 0.45, p.tower_d * 0.45,
                         p.tower_h, (tx, ty), 0.0, segments=8, col=col),
            mats["stone"]))

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
                kind="hisar", palette=p.palette, status="draft",
                accuracy="D3", towers=tower_n,
                face_deg=YK_FACE_DEG,
                round_towers=p.round_towers, gate_arches=arches,
                area_m2=p.area, radius=round(R, 2),
                wall_h=p.wall_h, height_tower=p.tower_h)
    return lod0, lod1, ucx, info
