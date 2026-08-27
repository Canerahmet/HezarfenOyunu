"""
Hezarfen: 1632 — **saray köşkü** kiti (Faz 3, S-kademe).

İlk ve şimdilik tek yapı: **İncili Köşk (Sinan Paşa Köşkü)**, 998-999/1590-91,
Koca Sinan Paşa'nın Mimar Dâvud Ağa'ya yaptırıp III. Murad'a sunduğu deniz
köşkü. 1632'de 41 yaşında ve ayakta; **1871-72**'de sahil demiryolu için
yıkıldı, bugün yalnızca temeli duruyor.

Oyun için önemi tek bir cümlede: **Evliya'ya göre IV. Murad, Hezarfen'in
uçuşunu buradan izledi** ve Lagari de önüne indi. Finalin kamerası bu yapıya
bakar.

## Ne ölçülü, ne değil

Ölçü **yok** — yapı 1872'de yıkıldı ve ölçülü çizimi bulunmuyor. Kütle bu
yüzden **D3**'tür. Ama kaynak sayılabilir şeyler söylüyor ve onlar geometriyi
bağlıyor:

* Bizans **deniz surunun önüne eklenen kesme taş kemerli alt yapı** üzerinde
  durur — yani yapı karada değil, **su kıyısındadır**;
* çıkmanın yan cephelerinde **Sarayburnu tarafında BİR, Ahırkapı tarafında
  İKİ kemer** vardır (asimetri belgeli);
* denize açılan **çift kemerin arasında çeşme** yer alır;
* esas mekânın **dört köşesinde birer baca** yükselir;
* denize doğru **ahşap konsollara oturan bir cumba** taşar.

## Çatı: çözülmemiş bir tartışma

TDV esas mekânı ortada yükselen kare bir kütle ve üstünde **kubbe** olarak
tarif eder; bir tasvir onu piramidal gösterir; **Sedat Hakkı Eldem** ise
gerçek örtünün **ahşap** olduğunu savunur. Karar verilmiş gibi davranmak
yerine iki varyant üretilir (`roof="kubbe"` ve `roof="ahsap"`) — Galata
Kulesi'nin külahında izlenen yolun aynısı (ADR 0033).
"""

import math

import bpy  # noqa: F401

import hz_blender as hz
import ottoman_kit as kit
import street_kit as sk

ROOF_TYPES = ("kubbe", "ahsap")

#: Sarayburnu tarafındaki kemer sayısı (belgeli).
ARCH_SARAYBURNU = 1

#: Ahırkapı tarafındaki kemer sayısı (belgeli).
ARCH_AHIRKAPI = 2

#: Esas mekânın köşe bacası sayısı (belgeli).
BACA = 4


class IncliKoskParams(object):
    """
    İncili Köşk, 1590-91 — 1632'de ayakta.

    ## 1632'de YOK

    * **1871-72 yıkımı** ve sahil demiryolu — yapıyı yok eden şey;
    * bugün ayakta duran **temel kalıntısı** dışındaki her modern ek.

    Oranlar ölçüm değil; hepsi `D3` ve `status="draft"`.
    """

    def __init__(self, hall_w=13.0, hall_d=9.5, wall_h=4.60,
                 podium_h=5.20, podium_sink=1.60, cumba=2.60,
                 dome_h=3.10, roof="kubbe", palette="default"):
        self.hall_w, self.hall_d = hall_w, hall_d
        self.wall_h = wall_h
        self.podium_h = podium_h          # su duzleminden platform kotuna
        self.podium_sink = podium_sink    # su altina inen pay
        self.cumba = cumba                # denize tasan cikma derinligi
        self.dome_h = dome_h
        self.roof = roof
        self.palette = palette

    @property
    def core(self):
        """Ortada yükselen kare kütlenin kenarı."""
        return min(self.hall_w, self.hall_d) * 0.62

    def validate(self):
        if self.roof not in ROOF_TYPES:
            raise ValueError(f"roof={self.roof} (secenekler: {ROOF_TYPES})")
        # Alt yapi SU USTUNDE bir platform olmali: kosk sur uzerinde durur ve
        # denize bakar. Alcak bir podyum onu sahil evine cevirirdi.
        if self.podium_h < 3.5:
            raise ValueError(f"podium_h={self.podium_h} — kosk kesme tas "
                             "kemerli bir ALT YAPI uzerinde durur")
        # Cumba TASMALI; tasmayan cumba cumba degildir.
        if self.cumba < 1.2:
            raise ValueError(f"cumba={self.cumba} — denize dogru TASAN bir "
                             "cikma olmali (ahsap konsollara oturur)")
        return self


def _arch_bay(parts, mats, col, name, w, h, center, u_ax, n_ax, t):
    """
    Kesme taş kemerli bir göz.

    Açıklık **panelin yüksekliğinden türetilir**, sabit bir orandan değil.
    İlk yazımda `w * 0.30` yazmıştım ve `arched_panel` haklı olarak
    reddetti: kitin kemeri sivridir ve kabarması `a·√(1+2c)` ≈ 1,22·a'dır,
    yarım dairenin (1,00·a) değil. Genişliği panele göre sabitlemek, kemer
    tepesini duvarın üstüne taşırıyordu.

    Burada tersi yapılır: önce kemerin sığacağı en büyük yarım açıklık
    hesaplanır, sonra istenen genişlikle sınırlanır.
    """
    spring = h * 0.36
    rise_ratio = math.sqrt(1.0 + 2.0 * sk.ARCH_C)
    a_max = (h - 0.35 - spring) / rise_ratio
    a = min(w * 0.30, max(0.35, a_max))
    parts.append(hz.assign(sk.arched_panel(
        name, w, h, t, center, u_ax, n_ax,
        spans=[(-a, a)], sill_z=0.0,
        spring_z=spring, col=col), mats["cutstone"]))


def build_incili_kosk(p, col, asset_name, textured=False):
    """İncili Köşk. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    W, D, PH = p.hall_w, p.hall_d, p.podium_h
    y_sea = -D * 0.5                      # denize bakan cephe (-Y)

    # --- 1) ALT YAPI: kesme tas, su altina iner ---------------------------
    #
    # Su duzlemi y=0. Alt yapi bir miktar ASAGI uzanir; su cizgisinde
    # kesilmis bir kutle "yuzuyor" gibi okunur (Kiz Kulesi turunda olculdu).
    base = hz.make_box(f"AltYapi_{asset_name}",
                       (W + 1.6, D + 1.6, PH + p.podium_sink),
                       (0.0, 0.0, (PH - p.podium_sink) * 0.5), col)
    hz.assign(base, mats["cutstone"])
    parts.append(base)

    # DENIZE ACILAN CIFT KEMER + ARASINDA CESME (belgeli).
    bay_w = (W + 1.6) * 0.30
    for s in (-1, 1):
        _arch_bay(parts, mats, col, f"DenizKemeri_{'D' if s > 0 else 'B'}",
                  bay_w, PH * 0.78,
                  (s * bay_w * 0.80, y_sea - 0.8, 0.0),
                  (1.0, 0.0), (0.0, -1.0), 0.75)
    cesme = hz.make_box(f"Cesme_{asset_name}", (1.70, 0.55, 2.10),
                        (0.0, y_sea - 1.05, 1.05), col)
    hz.assign(cesme, mats["marble"])
    parts.append(cesme)
    parts.append(hz.assign(hz.make_box(f"CesmeYalak_{asset_name}",
                                       (2.10, 0.75, 0.42),
                                       (0.0, y_sea - 1.35, 0.21), col),
                           mats["marble"]))

    # YAN KEMERLER: Sarayburnu tarafinda BIR, Ahirkapi tarafinda IKI.
    # Asimetri belgelidir ve bu yuzden sayilarla kurulur, gozle degil.
    for side, n, sign in (("Sarayburnu", ARCH_SARAYBURNU, 1),
                          ("Ahirkapi", ARCH_AHIRKAPI, -1)):
        for i in range(n):
            v = ((i + 0.5) / n - 0.5) * (D - 1.0)
            _arch_bay(parts, mats, col, f"YanKemer_{side}{i}",
                      (D - 1.0) / n * 0.82, PH * 0.72,
                      (sign * (W + 1.6) * 0.5, v, 0.0),
                      (0.0, 1.0), (float(sign), 0.0), 0.75)

    # KONSOL SIRASI: alt yapinin ustunde, tas.
    for i in range(9):
        u = (i / 8.0 - 0.5) * (W + 1.2)
        c = hz.make_box(f"Konsol_{i}", (0.34, 0.90, 0.34),
                        (u, y_sea - 0.75, PH - 0.30), col)
        hz.assign(c, mats["cutstone"])
        parts.append(c)

    silme = hz.make_box(f"Silme_{asset_name}", (W + 2.3, D + 2.3, 0.30),
                        (0.0, 0.0, PH + 0.15), col)
    hz.assign(silme, mats["cutstone"])
    parts.append(silme)

    z0 = PH + 0.30

    # --- 2) ESAS MEKAN: dikdortgen kagir --------------------------------
    t = 0.60
    faces = (("on", (1.0, 0.0), (0.0, -1.0), (0.0, -D * 0.5 + t * 0.5)),
             ("arka", (1.0, 0.0), (0.0, 1.0), (0.0, D * 0.5 - t * 0.5)),
             ("sag", (0.0, 1.0), (1.0, 0.0), (W * 0.5 - t * 0.5, 0.0)),
             ("sol", (0.0, 1.0), (-1.0, 0.0), (-W * 0.5 + t * 0.5, 0.0)))
    for nm, u_ax, n_ax, (cx, cy) in faces:
        span = W if nm in ("on", "arka") else D
        nwin = 4 if nm in ("on", "arka") else 3
        spans = []
        for i in range(nwin):
            u = (( i + 0.5) / nwin - 0.5) * (span - 1.4)
            spans.append((u - 0.45, u + 0.45))
        parts.append(hz.assign(sk.arched_panel(
            f"{nm}_{asset_name}", span, p.wall_h, t, (cx, cy, z0),
            u_ax, n_ax, spans=spans, sill_z=0.95, spring_z=2.60, col=col),
            mats["cutstone"]))
        for u0, u1 in spans:
            cu = (u0 + u1) * 0.5
            ctr = (cx + u_ax[0] * cu, cy + u_ax[1] * cu, z0 + 1.75)
            w = hz.make_box(f"PencereKaranlik_{nm}", (0.90, 0.90, 1.60),
                            ctr, col)
            hz.assign(w, mats["shadow"])
            parts.append(w)

    zc = z0 + p.wall_h

    # --- 3) CUMBA: denize tasar, AHSAP konsollara oturur ----------------
    #
    # Cumba bu yapinin ASIL yeridir: padisahlar kiyidaki toreni "kosk'un
    # PENCERELERINDEN" seyrederdi. Ilk yazimda duz bir siva kutusuydu ve
    # renderda bir TABELA gibi okundu — camekani olmayan bir cumba, cumba
    # degil bir cikintidir.
    cw = W * 0.60
    cy_ = y_sea - p.cumba * 0.5
    ch = p.wall_h * 0.82
    frame_t = 0.16

    # Ahsap iskelet: alt kusak, ust kusak, kose dikmeleri.
    for zz, hh in ((z0 + 0.28, 0.56), (z0 + ch - 0.30, 0.52)):
        parts.append(hz.assign(hz.make_box(f"CumbaKusak_{zz:.2f}",
                                           (cw, p.cumba, hh), (0.0, cy_, zz),
                                           col), mats["timber_bare"]))
    for sx in (-1, 1):
        parts.append(hz.assign(hz.make_box(f"CumbaDikme_{sx}",
                                           (0.22, p.cumba, ch),
                                           (sx * (cw * 0.5 - 0.11), cy_,
                                            z0 + ch * 0.5), col),
                               mats["timber_bare"]))
        # Yan yuzler: dar birer pencere.
        parts.append(hz.assign(hz.make_box(f"CumbaYanCam_{sx}",
                                           (0.10, p.cumba * 0.62, ch * 0.52),
                                           (sx * cw * 0.5, cy_,
                                            z0 + ch * 0.56), col),
                               mats["glass"]))

    # ON YUZ: dikmelerle bolunmus CAM seridi — cumbayi cumba yapan sey.
    n_cam = 5
    for i in range(n_cam):
        u = ((i + 0.5) / n_cam - 0.5) * (cw - 0.5)
        parts.append(hz.assign(hz.make_box(f"CumbaCam_{i}",
                                           ((cw - 0.5) / n_cam - 0.14, 0.10,
                                            ch * 0.52),
                                           (u, cy_ - p.cumba * 0.5,
                                            z0 + ch * 0.56), col),
                               mats["glass"]))
    for i in range(n_cam + 1):
        u = (i / n_cam - 0.5) * (cw - 0.5)
        parts.append(hz.assign(hz.make_box(f"CumbaSove_{i}",
                                           (0.13, 0.20, ch * 0.60),
                                           (u, cy_ - p.cumba * 0.5,
                                            z0 + ch * 0.55), col),
                               mats["timber_bare"]))
    # Doseme ve alttaki AHSAP konsollar.
    parts.append(hz.assign(hz.make_box(f"CumbaDoseme_{asset_name}",
                                       (cw + 0.3, p.cumba + 0.3, 0.26),
                                       (0.0, cy_, z0 + 0.13), col),
                           mats["timber_bare"]))
    for i in range(5):
        u = (i / 4.0 - 0.5) * (cw - 0.6)
        kk_ = hz.make_box(f"CumbaKonsol_{i}", (0.24, p.cumba * 0.9, 0.55),
                          (u, cy_, z0 - 0.28), col)
        hz.assign(kk_, mats["timber_bare"])
        parts.append(kk_)
    # SACAK: cumbanin ustu ayri ortulur.
    parts.append(hz.assign(hz.make_box(f"CumbaSacak_{asset_name}",
                                       (cw + 0.9, p.cumba + 0.9, 0.26),
                                       (0.0, cy_, z0 + ch + 0.13), col),
                           mats["cutstone"]))

    # --- 4) SACAK + ORTA KUTLE + ORTU ----------------------------------
    parts.append(hz.assign(hz.make_box(f"Sacak_{asset_name}",
                                       (W + 1.1, D + 1.1, 0.30),
                                       (0.0, 0.0, zc + 0.15), col),
                           mats["cutstone"]))
    core = p.core
    core_h = 1.70
    parts.append(hz.assign(hz.make_box(f"OrtaKutle_{asset_name}",
                                       (core, core, core_h),
                                       (0.0, 0.0, zc + 0.30 + core_h * 0.5),
                                       col), mats["cutstone"]))
    zr = zc + 0.30 + core_h
    if p.roof == "kubbe":
        parts.append(hz.assign(hz.make_dome(f"Kubbe_{asset_name}",
                                            core * 0.52, p.dome_h,
                                            (0.0, 0.0), zr, segments=20,
                                            rings=7, col=col), mats["lead"]))
        top = zr + p.dome_h
    else:
        # Eldem'in okumasi: ahsap, PIRAMIDAL ortu. Kursun kapli.
        parts.append(hz.assign(hz.make_tube(f"Ortu_{asset_name}",
                                            core * 0.76, 0.0, p.dome_h * 1.15,
                                            (0.0, 0.0), zr, segments=4,
                                            phase=math.pi * 0.25, col=col),
                               mats["lead"]))
        top = zr + p.dome_h * 1.15
    parts.append(hz.assign(hz.make_tube(f"Alem_{asset_name}", 0.10, 0.02, 1.1,
                                        (0.0, 0.0), top, segments=6, col=col),
                           mats["lead"]))

    # --- 5) DORT KOSE BACASI (belgeli sayi) -----------------------------
    #
    # "Esas mekanin dort kosesinde birer baca yukselir." Ilk yazimda bunlar
    # genis ve alcaki kutulardi; renderda baca degil KOSE PAYANDASI gibi
    # okundu. Baca INCE ve YUKSEK olur, ve catidan tasar.
    for sx in (-1, 1):
        for sy in (-1, 1):
            bx, by = sx * (W * 0.5 - 1.3), sy * (D * 0.5 - 1.3)
            b = hz.make_box(f"Baca_{sx}{sy}", (0.62, 0.62, 4.10),
                            (bx, by, zc + 0.30 + 2.05), col)
            hz.assign(b, mats["cutstone"])
            parts.append(b)
            # Kulah: bacanin ustunde tasan sapka. Onsuz baca "sutun" okunur.
            parts.append(hz.assign(hz.make_box(f"BacaKulah_{sx}{sy}",
                                               (0.95, 0.95, 0.30),
                                               (bx, by, zc + 0.30 + 4.25),
                                               col), mats["cutstone"]))
            parts.append(hz.assign(hz.make_box(f"BacaAgiz_{sx}{sy}",
                                               (0.34, 0.34, 0.22),
                                               (bx, by, zc + 0.30 + 4.10),
                                               col), mats["shadow"]))

    # --- LOD1 ------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Alt", (W + 1.6, D + 1.6,
                                               PH + p.podium_sink),
                                    (0.0, 0.0, (PH - p.podium_sink) * 0.5),
                                    col), mats["cutstone"]))
    l1.append(hz.assign(hz.make_box("L1_Mekan", (W, D, p.wall_h),
                                    (0.0, 0.0, z0 + p.wall_h * 0.5), col),
                        mats["cutstone"]))
    if p.roof == "kubbe":
        l1.append(hz.assign(hz.make_dome("L1_Kubbe", core * 0.52, p.dome_h,
                                         (0.0, 0.0), zr, segments=10, rings=4,
                                         col=col), mats["lead"]))
    else:
        l1.append(hz.assign(hz.make_tube("L1_Ortu", core * 0.76, 0.0,
                                         p.dome_h * 1.15, (0.0, 0.0), zr,
                                         segments=4, phase=math.pi * 0.25,
                                         col=col), mats["lead"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="kosk", roof=p.roof, palette=p.palette, status="draft",
                accuracy="D3", baca=BACA,
                arch_sarayburnu=ARCH_SARAYBURNU, arch_ahirkapi=ARCH_AHIRKAPI,
                cumba=round(p.cumba, 2), podium_h=round(p.podium_h, 2))
    return lod0, lod1, ucx, info


# ============================================== Alay Köşkü (1632: AHŞAP)

#: Alay Köşkü — **1632'de AHŞAP**. Bugünkü kâgir köşk **1810** ya da
#: **1819-20**, II. Mahmud'undur.
#:
#: Kaynak iki şeyi birden söylüyor ve ikincisi beklenmediktir:
#: 16. yüzyılda **aynı yerde ahşap bir köşk** vardı, ve II. Mahmud'un
#: yapısı **daha yüksek** bir köşkün ya da kulenin yerine geçti.
#:
#: Yani burada 1632 yapısı bugünkünden **ALÇAK DEĞİL, YÜKSEK**tir —
#: Galata Kulesi (ADR 0033) ve Adalet Kulesi'nin (ADR 0040) tersi. "Eski
#: olan alçaktır" diye bir kural yok; her yapı ayrı sorulur.
ALAY_W, ALAY_D = 11.0, 9.0
ALAY_WALL_H = 9.0          # Sur-i Sultani'nin bu kesimi
ALAY_BODY_H = 7.5
ALAY_JUT = 2.2             # sokaga tasma

#: Alay Köşkü'nün baktığı yön (derece) — **türetildi, D3**.
#:
#: Yön önce boş bırakıldı ve yerleştirici eğimden **90°** (doğu) verdi;
#: o yön köşkü **sarayın içine** çevirir. Alay Köşkü sur duvarının
#: üstündedir ve **dışa**, sokağa bakar — Yedikule'de kullanılan
#: akıl yürütmenin aynısı (ADR 0050).
#:
#: Sarayın merkezinden (28,9834 / 41,0115) köşke (28,98139 / 41,01175)
#: giden yön **279,4°**; yani batı. Ölçülü bir cephe açısı değil, ama
#: eğimden çok daha savunulabilir.
#:
#: **Sıfır yazılmaz**: `face_deg = 0` yerleştiricide "kuzeye bak"
#: anlamına gelir, "bildirilmedi" değil (bir kez öyle oldu). Kuzeye
#: bakan bir yapı **360** yazar.
ALAY_FACE_DEG = 279.4


class AlayKoskuParams(object):
    """
    Alay Köşkü — Sur-ı Sultanî üzerinde, **sokağa taşan** ahşap köşk.

    ## İncili Köşk'le aynı aile

    İkisi de bir **duvarın üstünde** durur, ikisi de **taşar**, ve ikisinde
    de padişah bir şeyi **seyreder**: İncili Köşk'ten Hezarfen'in uçuşu
    (ADR 0039), Alay Köşkü'nden devlet ricalinin alayları. Aynı yapı tipi,
    aynı işlev ailesi — bu yüzden aynı kitte.
    """

    def __init__(self, width=ALAY_W, depth=ALAY_D, wall_h=ALAY_WALL_H,
                 body_h=ALAY_BODY_H, jut=ALAY_JUT, palette="default"):
        self.width, self.depth = width, depth
        self.wall_h, self.body_h = wall_h, body_h
        self.jut = jut
        self.palette = palette

    @property
    def total_h(self):
        return self.wall_h + self.body_h

    def validate(self):
        # KOSK SURUN USTUNDEDIR: govdesi duvardan yuksekte baslar.
        if self.wall_h < 5.0:
            raise ValueError(f"sur {self.wall_h:.1f} m — Sur-i Sultani bu "
                             "kadar alcak degil")
        # VE SOKAGA TASAR: tasmayan bir kosk alay seyredemez.
        if self.jut < 1.2:
            raise ValueError(
                f"tasma {self.jut:.1f} m — Alay Kosku'nu Alay Kosku yapan "
                "sey sokaga TASMASIDIR; tasmayan bir kutle seyir yeri olmaz")
        return self


def build_alay_kosku(p, col, asset_name, textured=False):
    """Alay Köşkü, 1632 hâli (ahşap). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D = p.width, p.depth

    # SUR KESITI: kosk onun uzerinde durur.
    parts.append(hz.assign(hz.make_box(f"Sur_{asset_name}",
                                       (W + 6.0, D, p.wall_h),
                                       (0.0, 0.0, p.wall_h * 0.5), col),
                           mats["stone"]))
    for i in range(4):
        parts.append(hz.assign(
            hz.make_box(f"SurMazgal_{i}", (2.0, D + 0.4, 1.2),
                        (-(W + 6.0) * 0.5 + (W + 6.0) * (i + 0.5) / 4.0,
                         0.0, p.wall_h + 0.6), col), mats["stone"]))

    # AHSAP GOVDE — sokaga (-Y) tasar.
    cy = -p.jut * 0.5
    # Malzeme `timber_bare` — BOYASIZ. Govde asi kirmizisiyla kurulmustu ama
    # asi boyasi EV boyasidir (ADR 0035); Alay Kosku Sur-i Sultani'nin
    # ustundedir, yani bir SARAY yapisidir ve ADR 0055 bu yapinin ahsabi icin
    # zaten `timber_bare` diyor. Cevresindeki her parca (konsol, kayit, kiris)
    # dogru maldemedeydi; yalniz govde ile LOD1 gozden kacmisti.
    parts.append(hz.assign(hz.make_box(f"Govde_{asset_name}",
                                       (W, D + p.jut, p.body_h),
                                       (0.0, cy, p.wall_h + p.body_h * 0.5),
                                       col), mats["timber_bare"]))
    # KONSOLLAR (elibogrunde): tasmayi tasiyan sey.
    #
    # Kutu degil UCGEN prizma. Konsolun isi yuku duvara aktarmaktir ve o isi
    # okutan sey egik alt yuzudur; kutu koymak "kalin bir raf" gibi
    # goruunuyordu. Ev kitindeki cumba payandasinin ta kendisi
    # (`ottoman_kit._corbel_bracket`) — ayni ahsap isciligi surun ustunde de
    # gecerlidir ve ikinci bir nusha yazmak zamanla iki farkli payanda demek.
    for i in range(5):
        ux = -W * 0.5 + W * (i + 0.5) / 5.0
        parts.append(hz.assign(kit._corbel_bracket(
            f"Konsol_{i}", 0.26, p.jut + 0.7, 0.52,
            (ux, -D * 0.5 - p.jut * 0.5 + 0.2, p.wall_h - 0.34), col),
            mats["timber_bare"]))

    # PENCERE KUSAGI: kosk SEYIR yeridir, cephesi camdir.
    #
    # Cerceve ve dikme EKLENDI. Onceki hali cepheye kesilmis koyu
    # dikdortgenlerdi; oysa ahsap bir cephede acikligi okutan sey camin
    # kendisi degil, onu tutan KAYITTIR. Kafes DEGIL kayit: kafes mahremiyet
    # icindir, burasi ise padisahin alayi SEYRETTIGI yer — bakisi kesen bir
    # ogeyi buraya koymak yapinin islevini yanlis anlatirdi.
    pw, ph = W / 6.0 * 0.66, p.body_h * 0.46
    # Denizlik GOVDENIN ON YUZUNDEN turer, `-D/2 - jut` gibi bir ifadeden
    # DEGIL: govde `cy = -jut/2`'de merkezlidir, yani on yuzu
    # `cy - (D+jut)/2`. Elle yazilan ifade 0,25 m sapiyordu; derinlik 0,50
    # iken bu gorunmuyordu (kutu yuzeye yetisiyordu), 0,18'e indirilince
    # pencereler ahsabin ICINE gomulup tumuyle kayboldu. Kot bagli oldugu
    # seyden okunmali.
    py = cy - (D + p.jut) * 0.5
    pz = p.wall_h + p.body_h * 0.52
    for i in range(6):
        ux = -W * 0.5 + W * (i + 0.5) / 6.0
        # Sira onemli ve derinlik daha da onemli: bosluk SIG (0,18) olmali,
        # cerceve ve dikme onun ONUNDE durmali. Ilk yazimda bosluk 0,50
        # derinlikteydi ve cerceveyi de dikmeleri de icine aliyordu; renderda
        # cephe duzgun bir pencere sirasi degil, kirik kamalardan olusan bir
        # doku gibi okundu.
        parts.append(hz.assign(
            hz.make_box(f"Pencere_{i}", (pw, 0.18, ph), (ux, py, pz), col),
            mats["shadow"]))
        parts.append(hz.assign(
            hz.make_box(f"PencCerceve_{i}", (pw + 0.30, 0.14, ph + 0.30),
                        (ux, py - 0.16, pz), col), mats["timber_bare"]))
        for k in (-1, 1):           # ikiye bolen dusey kayit
            parts.append(hz.assign(hz.make_box(
                f"PencDikme_{i}_{k}", (0.075, 0.14, ph),
                (ux + k * pw * 0.25, py - 0.16, pz), col),
                mats["timber_bare"]))
    for sx in (-1, 1):
        for i in range(3):
            parts.append(hz.assign(
                hz.make_box(f"YanPencere_{sx}{i}", (0.18, D / 3.0 * 0.6,
                                                    p.body_h * 0.42),
                            (sx * W * 0.5,
                             cy - (D + p.jut) * 0.5 + (D + p.jut) * (i + 0.5) / 3.0,
                             p.wall_h + p.body_h * 0.52), col),
                mats["shadow"]))

    # KURSUN KAPLI KIRMA CATI + genis SACAK.
    roof = hz.make_hip_roof(f"Cati_{asset_name}", W + 2.4, D + p.jut + 2.4,
                            2.6, (0.0, cy), p.wall_h + p.body_h, col=col)
    hz.assign(roof, mats["lead"])
    parts.append(roof)

    # SACAK ALTI KIRISLEME: sacak her yandan 1,2 m tasiyor ve altinda hicbir
    # sey yoktu — kursun ortu havada duruyor gibi okunuyordu. Ahsap catida
    # tasmayi kirisler tasir ve uzaktan okunan sey onlarin BIRAKTIGI GOLGE
    # RITMIDIR, kirisin kendisi degil.
    ez = p.wall_h + p.body_h - 0.11
    for i in range(9):
        ux = -(W + 2.0) * 0.5 + (W + 2.0) * (i + 0.5) / 9.0
        for sy in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Kiris_{i}_{sy}", (0.11, 1.5, 0.22),
                (ux, cy + sy * ((D + p.jut) * 0.5 + 0.5), ez), col),
                mats["timber_bare"]))
    for j in range(5):
        uy = cy - (D + p.jut) * 0.5 + (D + p.jut) * (j + 0.5) / 5.0
        for sx in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"KirisYan_{j}_{sx}", (1.5, 0.11, 0.22),
                (sx * (W * 0.5 + 0.5), uy, ez), col), mats["timber_bare"]))

    l1.append(hz.assign(hz.make_box(f"L1_Sur_{asset_name}",
                                    (W + 6.0, D, p.wall_h),
                                    (0.0, 0.0, p.wall_h * 0.5), col),
                        mats["stone"]))
    l1.append(hz.assign(hz.make_box(f"L1_Govde_{asset_name}",
                                    (W, D + p.jut, p.body_h + 2.6),
                                    (0.0, cy,
                                     p.wall_h + (p.body_h + 2.6) * 0.5), col),
                        mats["timber_bare"]))

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
                kind="kosk", palette=p.palette, status="draft",
                accuracy="D3", material="ahsap", roof="ahsap",
                cumba=round(p.jut, 2), wall_h=p.wall_h,
                storeys=1, face_deg=ALAY_FACE_DEG)
    return lod0, lod1, ucx, info
