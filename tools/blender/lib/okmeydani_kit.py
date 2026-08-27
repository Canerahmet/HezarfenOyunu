"""
Hezarfen: 1632 — Okmeydanı: namazgâh, Okçular Tekkesi, menzil taşı.

Bu üç yapı kitin geri kalanından farklı bir sebeple var: **oyunun hikâyesi
buradan başlıyor.** RESEARCH.md'ye göre Okmeydanı, Hezarfen'in talim yaptığı
yerdir; II. Mehmed'in bağımsız vakıf olarak tesis ettiği, II. Bayezid'in
genişlettiği bir atış sahasıdır.

## Okmeydanı'nı Okmeydanı yapan şey BOŞLUKTUR

II. Bayezid'in vakfiyesi meydana *"bir karış tecavüz edilmemesi, yapı, mezar,
su yolu, **bağ ve bahçe** yapılmaması"*nı kesin olarak yasaklar. Yani burası
tesadüfen boş bir tarla değil, **bilinçle boş tutulmuş** bir alandır — ve o
boşluk zaten ADR 0026'da ağaçsız bir poligon olarak korunuyor.

Buraya konacak şey bu yüzden azdır ve her biri sayılıdır: bir namazgâh, bir
tekke, ve **taşlar**.

## Namazgâh — minaresi olmayan, duvarı olmayan mescit

Namazgâh açık hava namaz yeridir. Yapıyı yapı yapan üç şey var ve üçü de
zorunlu: zeminden **seki** ile ayrılmış bir platform, kıble yönünde bir
**mihrap taşı**, ve —bu namazgâh için— bir **minber**.

Minber ayrıntısı 1632 için önemli: Okmeydanı namazgâhı **minberlidir** ve
minberi **Gürcü Mehmed Paşa 1624–25'te** eklemiştir (RESEARCH.md). Yani
oyunun geçtiği yılda minber **yedi yıllıktır** — yeni sayılır.

Ölçü çapası: Gelibolu Azebler Namazgâhı dikdörtgen planlı ve yaklaşık
**12 × 8 m**dir. Okmeydanı'nınki için ölçü yok; bu oran alındı.

## Okçular Tekkesi — 1632'de MİNARESİZ

Tekke mescidinin minaresi ancak **1770–71**'de eklenmiştir; 1632'de yoktur
(RESEARCH.md). Bu, kitin öteki mescidiyle arasındaki en görünür farktır ve
`mosque_kit`ten ayrı bir yapı olmasının sebebidir: minareyi silmek yetmez,
tekke bir mescit değil bir **külliyeciktir** — tevhidhane/mescit + derviş
hücreleri + meydan şeyhinin odası.

## Menzil taşı — oyunun içinde duran ölçü aleti

Menzil taşları tek parça **mermer sütunlardır**; üstlerinde okçunun adı,
mesleği, atışın yapıldığı günün **havası** (rüzgârı), **mesafesi** ve tarihi
yazılıdır. Rekor kırıldığında okun düştüğü nokta çakılla işaretlenir ve altı
ay içinde yeni taş dikilir.

İkişer dikilirler: atışın yapıldığı yerde **ayak taşı**, okun en ileriye
düştüğü yerde **baş taşı** (ana taşı). Yani bir çift taş, aralarındaki
mesafeyi *ölçer*. Meydanın rekoru **1281,5 gez**dir (Arkurı Menzili,
gündoğusu havası; TDV bunu 845,66 m verir) — oyunun uçuş mesafeleriyle aynı
mertebede, ve oyuncunun yerde yürüyerek okuyabileceği bir ölçek.

**Mermer, kesme taş değil.** Malzeme `marble` rolüdür ve dokusu
`tools/textures/gen_marble_texture.py` ile üretilir. Kesme taş bir DUVAR
malzemesidir; bir sütuna sarıldığında derzleri "tek parça" iddiasını
yalanlar — ölçüldü, sütunda 0,95 m periyotlu taş sırası çıkıyordu.

Eksen sözleşmesi kitin geri kalanıyla aynı: **mihrap +Y'de** (mosque_kit ile
aynı), giriş −Y. Kıbleye döndürme yerleştirmede yapılır.
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


# ----------------------------------------------------------------- namazgâh

class NamazgahParams(object):
    """
    Minberli namazgâh — açık hava namaz yeri.

    `width` × `depth`: seki ölçüsü. Varsayılan 12,0 × 8,0 m, Gelibolu Azebler
    Namazgâhı'nın belgelenmiş ölçüsünden alındı; Okmeydanı'nınki için ölçü yok.
    """

    def __init__(self, width=12.0, depth=8.0, steps=2, step_h=0.18,
                 step_run=0.40, wall_h=0.62, wall_t=0.42,
                 mihrap_w=1.30, mihrap_h=2.35, mihrap_d=0.55,
                 minber=True, minber_steps=5, minber_w=0.95,
                 palette="default"):
        self.width, self.depth = width, depth
        self.steps, self.step_h, self.step_run = steps, step_h, step_run
        self.wall_h, self.wall_t = wall_h, wall_t
        self.mihrap_w, self.mihrap_h, self.mihrap_d = mihrap_w, mihrap_h, mihrap_d
        self.minber, self.minber_steps = minber, minber_steps
        self.minber_w = minber_w
        self.palette = palette

    @property
    def platform_z(self):
        return self.steps * self.step_h

    def validate(self):
        # Namazgahi namazgah yapan sey SEKIDIR: zeminden ayrilmamis bir
        # dikdortgen, namaz yeri degil yalnizca yerdir.
        if self.platform_z < 0.12:
            raise ValueError(f"seki {self.platform_z:.2f} m — zeminden "
                             "ayrilmiyor; namazgah bir PLATFORMDUR")
        # Cevre duvari BEL HIZASINI ASMAMALI: acik hava olmaktan cikar.
        if self.wall_h > 1.10:
            raise ValueError(f"cevre duvari {self.wall_h:.2f} m — bu kadar "
                             "yuksek duvar aciklik duygusunu oldurur")
        # Mihrap tasi INSAN BOYUNDA olmali; alcak kalirsa sinir tasi gibi
        # okunur, yuksek olursa duvar olur.
        if not 1.80 <= self.mihrap_h <= 3.20:
            raise ValueError(f"mihrap tasi {self.mihrap_h:.2f} m — 1,80-3,20 disi")
        if self.minber and self.minber_steps < 3:
            raise ValueError("minber en az uc basamak olmali")


def build_namazgah(p, col, asset_name, textured=False):
    """Minberli namazgâh. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    W, D = p.width, p.depth
    pz = p.platform_z

    # --- SEKI: her basamak bir oncekinden `step_run` kadar genis.
    for i in range(p.steps):
        k = (p.steps - i) * p.step_run * 2.0
        _put(parts, hz.make_box(f"{asset_name}_Basamak",
                                (W + k, D + k, p.step_h),
                                (0.0, 0.0, (i + 0.5) * p.step_h), col),
             mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Doseme", (W, D, 0.10),
                            (0.0, 0.0, pz + 0.05), col), mats["paving"])

    # --- CEVRE DUVARI: yalniz UC yanda. Kible yani mihraba birakilir,
    #     giris yani (-Y) acik kalir — namazgaha kapidan degil,
    #     basamaktan girilir.
    t = p.wall_t
    for name, size, center in (
        ("Sol", (t, D, p.wall_h), (-W * 0.5 + t * 0.5, 0.0, pz + p.wall_h * 0.5)),
        ("Sag", (t, D, p.wall_h), (W * 0.5 - t * 0.5, 0.0, pz + p.wall_h * 0.5)),
        ("Kible", (W, t, p.wall_h), (0.0, D * 0.5 - t * 0.5,
                                     pz + p.wall_h * 0.5)),
    ):
        _put(parts, hz.make_box(f"{asset_name}_Duvar{name}", size, center, col),
             mats["cutstone"])

    # --- MIHRAP TASI: kible duvarinin ORTASINDA, duvari asan tek dusey.
    #
    # Namazgahta mihrap bir nis degil bir TASTIR: arkasinda mekan yoktur.
    # Yuzunde sivri kemerli oyuk vardir, o kadar.
    my = D * 0.5 - t * 0.5
    _put(parts, hz.make_box(f"{asset_name}_MihrapTasi",
                            (p.mihrap_w + 0.30, p.mihrap_d, p.mihrap_h),
                            (0.0, my, pz + p.mihrap_h * 0.5), col),
         mats["cutstone"])

    spring = p.mihrap_h * 0.58
    _, rise = sk.arch_points(p.mihrap_w * 0.5, spring)
    nis_h = spring + rise
    _put(parts, hz.make_box(f"{asset_name}_MihrapNis",
                            (p.mihrap_w, 0.10, nis_h),
                            (0.0, my - p.mihrap_d * 0.5 + 0.05,
                             pz + nis_h * 0.5), col), mats["shadow"])
    _put(parts, hz.make_box(f"{asset_name}_MihrapKitabe",
                            (p.mihrap_w + 0.18, 0.08, 0.34),
                            (0.0, my - p.mihrap_d * 0.5 - 0.04,
                             pz + nis_h + 0.28), col), mats["cutstone"])

    # --- MINBER: 1624-25'te GURCU MEHMED PASA ekledi; 1632'de YEDI YILLIK.
    #
    # Namazgah minberi cami minberi degildir: tas, kucuk, ve mihrabin SAGINDA
    # durur (cemaate gore). Basamak sayisi tek: hatip en usttekine cikmaz.
    if p.minber:
        mx = p.mihrap_w * 0.5 + 1.15
        sh, run = 0.19, 0.33
        for i in range(p.minber_steps):
            _put(parts, hz.make_box(f"{asset_name}_MinberBasamak",
                                    (p.minber_w, run, sh),
                                    (mx, my - t * 0.5 - 0.35 - (i + 0.5) * run,
                                     pz + (i + 0.5) * sh), col),
                 mats["cutstone"])
        top = p.minber_steps * sh
        _put(parts, hz.make_box(f"{asset_name}_MinberKursu",
                                (p.minber_w + 0.16, 0.62, 0.14),
                                (mx, my - t * 0.5 - 0.20, pz + top + 0.07), col),
             mats["cutstone"])
        for s in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_MinberKorkuluk",
                                    (0.10, p.minber_steps * run + 0.62, 0.44),
                                    (mx + s * (p.minber_w * 0.5 + 0.05),
                                     my - t * 0.5 - 0.20
                                     - (p.minber_steps * run + 0.62) * 0.5 + 0.31,
                                     pz + top * 0.5 + 0.30), col),
                 mats["cutstone"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (W + 0.8, D + 0.8, pz + 0.10),
                 (0.0, 0.0, (pz + 0.10) * 0.5), col, mats["cutstone"]),
          _solid(f"{asset_name}_L1m", (p.mihrap_w + 0.3, p.mihrap_d, p.mihrap_h),
                 (0.0, my, pz + p.mihrap_h * 0.5), col, mats["cutstone"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    ucx = hz.make_box(f"UCX_{asset_name}", (W + 0.8, D + 0.8, pz + 0.10),
                      (0.0, 0.0, (pz + 0.10) * 0.5), col)
    hz.assign(ucx, mats["cutstone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx_b = hz.bounds(lod0)
    info = dict(footprint_x=round(mx_b[0] - mn[0], 3),
                footprint_y=round(mx_b[1] - mn[1], 3),
                height=round(mx_b[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                platform_z=round(pz, 3), minber=bool(p.minber),
                kind="namazgah", palette=p.palette)
    return lod0, lod1, ucx, info


# --------------------------------------------------------------------- tekke

class TekkeParams(object):
    """
    Okçular (Kemankeş) Tekkesi — **1632'de MİNARESİZ**.

    Mescit + derviş hücreleri + meydan şeyhinin odası. Minare 1770–71'de
    eklenmiştir; bu yüzden `minare` diye bir parametre bile YOKTUR — olmayan
    bir şeyi kapatılabilir kılmak, bir gün yanlışlıkla açılmasına davetiyedir.
    """

    def __init__(self, hall_w=9.20, hall_d=9.20, wall_h=4.60, dome_h=2.60,
                 cells=6, cell_w=3.10, cell_d=4.20, cell_h=3.20,
                 court_w=11.60, plinth=0.34, wall_t=0.62,
                 revak_d=2.10, palette="default"):
        self.hall_w, self.hall_d = hall_w, hall_d
        self.wall_h, self.dome_h = wall_h, dome_h
        self.cells, self.cell_w, self.cell_d = cells, cell_w, cell_d
        self.cell_h = cell_h
        self.court_w, self.plinth, self.wall_t = court_w, plinth, wall_t
        self.revak_d = revak_d
        self.palette = palette

    @property
    def court_d(self):
        """Avlu derinliği hücre sayısından ÇIKAR, ayrıca yazılmaz."""
        return (self.cells // 2) * self.cell_w

    def validate(self):
        # Tekke bir MESCIT DEGIL kulliyeciktir: hucreler olmadan yalnizca
        # kucuk bir mescit olur ve Okmeydani'ndaki farki kaybolur.
        if self.cells < 4:
            raise ValueError(f"{self.cells} hucre — tekke en az dort dervis "
                             "hucresi ister, yoksa mescitten farki kalmaz")
        if self.cells % 2:
            raise ValueError("hucre sayisi CIFT olmali: avlunun iki yanina "
                             "esit dagilir")
        # Hucre kubbeli mescitten ALCAK olmali; esitlenirse kutle tek bir
        # blok olarak okunur ve tekke bir hana benzer.
        if self.cell_h > self.wall_h - 0.80:
            raise ValueError(f"hucre {self.cell_h:.2f} m, mescit duvari "
                             f"{self.wall_h:.2f} m — hucreler mescidi bastiriyor")
        if self.dome_h < self.hall_w * 0.22:
            raise ValueError("kubbe cok basik — kagir bir kutu gibi okunur")
        # Avlu mescitten DAR olamaz: mescit avlunun gerisinde durur ve iki
        # yandan tasarsa kutle "yan yana uc bina" olarak okunur.
        if self.court_w < self.hall_w + 1.0:
            raise ValueError(f"avlu {self.court_w:.2f} m, mescit "
                             f"{self.hall_w:.2f} m — avlu mescitten dar")


def _cell_row(parts, mats, col, name, n, cw, cd, ch, x_out, y0, plinth, inward):
    """
    Avlunun bir yanındaki hücre sırası + her hücrede baca.

    Hücreler avluya **bakar**: kapı içeri, baca dışarı. `inward` avlunun
    hangi tarafta olduğunu söyler (+1 ya da −1); iki kanat aynı kodu
    kullanır ve elle aynalanmaz — aynalama, bir gün bir kanadın kapısını
    ters çevirmenin en kısa yoludur.
    """
    for i in range(n):
        cy = y0 + (i + 0.5) * cw
        # `x_out` DIS yuzdur; merkez oradan AVLUYA dogru yarim derinlik iceri.
        # Ilk yazimda isaret tersti ve butun sira bir hucre boyu disari
        # kaydi: avluyla hucreler arasinda bos bir serit kaldi ve revak
        # onlerinde ayri duran bir pergola gibi okundu.
        cx = x_out + inward * cd * 0.5
        _put(parts, hz.make_box(f"{name}_Hucre", (cd, cw, ch),
                                (cx, cy, plinth + ch * 0.5), col),
             mats["plaster"])
        # Kapi AVLUYA bakar.
        _put(parts, hz.make_box(f"{name}_HucreKapi", (0.12, 0.86, 1.95),
                                (cx + inward * (cd * 0.5 - 0.06), cy,
                                 plinth + 0.975), col), mats["shadow"])
        # Ocak DIS duvardadir; baca oradan cikar.
        bx = x_out - inward * 0.40
        _put(parts, hz.make_box(f"{name}_Baca", (0.52, 0.52, ch + 1.05),
                                (bx, cy, (ch + 1.05) * 0.5), col), mats["stone"])
        _put(parts, hz.make_box(f"{name}_BacaKulah", (0.68, 0.68, 0.16),
                                (bx, cy, ch + 1.13), col), mats["cutstone"])


def build_tekke(p, col, asset_name, textured=False):
    """Okçular Tekkesi — minaresiz, avlulu. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []
    HW, HD, t = p.hall_w, p.hall_d, p.wall_t
    z0, H = p.plinth, p.wall_h
    CW, CD = p.court_w, p.court_d

    # KURULUS: avlu merkezde, mescit onun GERISINDE (+Y, kible yani).
    # Hucreler avlunun iki yaninda, kapilari avluya bakar. Ilk yazimda
    # hucreler mescidin YANINA diziliyordu ve render'da "bir mescit ve iki
    # baraka" gibi okunuyordu — tekkeyi tekke yapan sey avludur.
    hall_y = CD * 0.5 + HD * 0.5

    _put(parts, hz.make_box(f"{asset_name}_Subasman", (HW + 0.3, HD + 0.3, z0),
                            (0.0, hall_y, z0 * 0.5), col), mats["stone"])
    for name, size, center in (
        ("Sol", (t, HD, H), (-HW * 0.5 + t * 0.5, hall_y, z0 + H * 0.5)),
        ("Sag", (t, HD, H), (HW * 0.5 - t * 0.5, hall_y, z0 + H * 0.5)),
        ("Kible", (HW, t, H), (0.0, hall_y + HD * 0.5 - t * 0.5, z0 + H * 0.5)),
        ("On", (HW, t, H), (0.0, hall_y - HD * 0.5 + t * 0.5, z0 + H * 0.5)),
    ):
        _put(parts, hz.make_box(f"{asset_name}_Mescit{name}", size, center, col),
             mats["plaster"])

    # Mihrap: kible duvarindan (+Y) disari tasan yarim kutle — mosque_kit
    # ile ayni gramer. MINARE YOK: 1770-71'e kadar yoktu.
    _put(parts, hz.make_box(f"{asset_name}_Mihrap", (2.05, 0.75, H * 0.80),
                            (0.0, hall_y + HD * 0.5 + 0.30, z0 + H * 0.40), col),
         mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_Kapi", (1.20, 0.14, 2.25),
                            (0.0, hall_y - HD * 0.5 + 0.07, z0 + 1.125), col),
         mats["shadow"])
    for s in (-1, 1):
        for k in (-1, 1):
            _put(parts, hz.make_box(f"{asset_name}_Pencere", (0.10, 0.90, 1.30),
                                    (s * (HW * 0.5 - 0.05), hall_y + k * HD * 0.22,
                                     z0 + H * 0.52), col), mats["shadow"])

    dome = hz.make_dome(f"{asset_name}_Kubbe", HW * 0.5 + 0.16, p.dome_h,
                        (0.0, hall_y), z0 + H, col=col, segments=24, rings=8)
    _put(parts, dome, mats["lead"])

    # --- DERVIS HUCRELERI: avlunun iki yani
    cw, cd, ch = p.cell_w, p.cell_d, p.cell_h
    n_side = p.cells // 2
    for s in (-1, 1):
        x_out = s * (CW * 0.5 + cd)
        _cell_row(parts, mats, col, asset_name, n_side, cw, cd, ch,
                  x_out, -CD * 0.5, z0, inward=-s)
        _put(parts, hz.make_box(f"{asset_name}_HucreDam",
                                (cd + 0.36, n_side * cw + 0.36, 0.22),
                                (s * (CW * 0.5 + cd * 0.5), 0.0, z0 + ch + 0.11),
                                col), mats["cutstone"])

    # --- REVAK: avlunun uc yaninda (hucre onleri + mescit onu).
    #     Tekkede revak sus degil dolasim: hucreye yagmurda da girilir.
    # Revak yuksekligi HUCRE DAMINDAN cikar, ayrica yazilmaz: iki ayri sayi
    # bir gun ayrisir ve revak damin altinda ayri duran bir pergolaya doner —
    # ilk uretimde tam bu olmustu.
    col_h = ch - 0.18
    rz = z0 + col_h
    for s in (-1, 1):
        for i in range(n_side + 1):
            y = -CD * 0.5 + i * (CD / n_side)
            _put(parts, hz.make_tube(f"{asset_name}_RevakSutun", 0.19, 0.17,
                                     col_h, (s * (CW * 0.5 - 0.25), y), z0,
                                     col=col, segments=8), mats["cutstone"])
        # Saçak hücre duvarına DAYANIR: kolon ekseninden hücre yüzüne kadar.
        span = cd * 0.0 + (CW * 0.5 + 0.0) - (CW * 0.5 - 0.25) + 0.45
        _put(parts, hz.make_box(f"{asset_name}_RevakSaci",
                                (span + 0.30, CD + 0.30, 0.22),
                                (s * (CW * 0.5 - 0.25 + (span + 0.30) * 0.5 - 0.15),
                                 0.0, rz + 0.11), col), mats["cutstone"])

    # --- AVLU ZEMINI ve giris esigi (-Y)
    _put(parts, hz.make_box(f"{asset_name}_AvluZemin",
                            (CW + 2 * cd, CD, 0.10),
                            (0.0, 0.0, 0.05), col), mats["paving"])
    # Avlu kapisi: IKI PAYE + LENTO, arasi bosluk. Ilk yazimda tek dolu blok
    # yaziliydi ve girisin ortasinda duran bir duvar parcasi gibi okunuyordu —
    # kapi, iceri girilebildigi icin kapidir.
    gy = -CD * 0.5 - 0.25
    for s in (-1, 1):
        _put(parts, hz.make_box(f"{asset_name}_AvluPaye", (0.70, 0.50, 2.60),
                                (s * 1.35, gy, 1.30), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_AvluLento", (3.40, 0.56, 0.42),
                            (0.0, gy, 2.81), col), mats["cutstone"])
    _put(parts, hz.make_box(f"{asset_name}_AvluGolge", (2.00, 0.10, 2.60),
                            (0.0, gy + 0.20, 1.30), col), mats["shadow"])

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    l1 = [_solid(f"{asset_name}_L1", (HW, HD, z0 + H),
                 (0.0, hall_y, (z0 + H) * 0.5), col, mats["plaster"]),
          _solid(f"{asset_name}_L1s", (CW + 2 * cd, CD, z0 + ch),
                 (0.0, 0.0, (z0 + ch) * 0.5), col, mats["plaster"])]
    l1.append(_put([], hz.make_dome(f"{asset_name}_L1k", HW * 0.5, p.dome_h,
                                    (0.0, hall_y), z0 + H, col=col,
                                    segments=10, rings=4), mats["lead"]))
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    total_d = CD + HD
    ucx = hz.make_box(f"UCX_{asset_name}", (CW + 2 * cd, total_d, z0 + H),
                      (0.0, CD * 0.5 + HD * 0.5 - total_d * 0.5 + CD * 0.0,
                       (z0 + H) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                cells=p.cells, minaret=False, court_w=round(CW, 2),
                court_d=round(CD, 2),
                kind="tekke", palette=p.palette)
    return lod0, lod1, ucx, info


# --------------------------------------------------------------- menzil taşı

class MenzilTasiParams(object):
    """
    Menzil taşı — tek parça mermer sütun.

    `role`: `"bas"` (okun en ileriye düştüğü yer — yüksek, kitabeli, alemli)
    ya da `"ayak"` (atışın yapıldığı yer — alçak, sade). İkisi bir
    **çifttir** ve aralarındaki mesafe atışın kendisidir.

    **Adlandırma kaynaktan gelir:** "Bir menzilde okun en ileriye düştüğü
    yere dikilen taşa o menzilin *baş taşı*, menzilde atışın yapıldığı ilk
    noktaya ise o menzilin *ayak taşı* denilmektedir" (Boran & Kılıç 2025).
    İlk yazımda buna "nişan taşı" demiştik; yanlıştı — kaynaklarda "nişan"
    atışın kendisidir ("nişan etmek", "nişan alanı") ve taş dikilene kadar
    okun düştüğü yeri kaybetmemek için bırakılan çakıl yığınıdır.
    """

    def __init__(self, role="bas", height=2.60, side=0.34, base_side=0.72,
                 base_h=0.34, sides=8, kitabe=True, palette="default"):
        self.role = role
        self.height, self.side = height, side
        self.base_side, self.base_h = base_side, base_h
        self.sides, self.kitabe = sides, kitabe
        self.palette = palette

    def validate(self):
        if self.role not in ("bas", "ayak"):
            raise ValueError(f"bilinmeyen rol: {self.role}")
        # Tas OKUNABILIR olmali: uzerinde okcunun adi, meslegi, yonu, MESAFESI
        # ve tarihi yazar. Insan boyunun altina inerse sinir tasi olur.
        if not 1.40 <= self.height <= 4.00:
            raise ValueError(f"tas {self.height:.2f} m — 1,40-4,00 disi")
        # Bas tasi ayak tasindan YUKSEKTIR; ayni boyda olurlarsa cift
        # okunmaz ve mesafe anlamini kaybeder.
        if self.role == "ayak" and self.height > 2.10:
            raise ValueError("ayak tasi bas tasindan alcak olmali (<= 2,10 m)")
        if self.side > self.base_side * 0.75:
            raise ValueError("govde kaideye gore kalin — sutun degil paye olur")


def build_menzil_tasi(p, col, asset_name, textured=False):
    """Mermer menzil taşı. Dönüş: `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts = []

    _put(parts, hz.make_box(f"{asset_name}_Kaide",
                            (p.base_side, p.base_side, p.base_h),
                            (0.0, 0.0, p.base_h * 0.5), col), mats["marble"])
    _put(parts, hz.make_box(f"{asset_name}_Pabuc",
                            (p.base_side * 0.78, p.base_side * 0.78, 0.12),
                            (0.0, 0.0, p.base_h + 0.06), col), mats["marble"])

    shaft_z = p.base_h + 0.12
    shaft_h = p.height - shaft_z
    # `phase`: govdenin duz bir YUZU kitabe tarafina (-Y) baksin. Varsayilan
    # sifirda sekizgenin -Y'sinde bir KOSE durur; kitabe panosu oraya konunca
    # yuzeye teges kaliyor ve modelde yalnizca kenari gorunuyordu — Blender
    # inceleme paketi bunu gosterdi, sahne karesi gostermedi (iki taraftan da
    # "kitabe yok" gibi okunuyordu).
    _put(parts, hz.make_tube(f"{asset_name}_Govde", p.side * 0.5,
                             p.side * 0.44, shaft_h, (0.0, 0.0), shaft_z,
                             col=col, segments=p.sides,
                             phase=math.pi / p.sides), mats["marble"])

    if p.kitabe:
        # Kitabe, OTURDUGU YUZDEN turetilir — bagimsiz bir olcu degil.
        #
        # Iki kez yanlis yazildi ve ikisi de "pano sutuna dayanmis levha gibi
        # duruyor" diye okundu:
        #
        #   1. Kalinlik: pano govdeden 1 cm'den fazla tasiyordu. Duzeltildi.
        #   2. GENISLIK: pano 0,248 m'ydi, oturdugu duz yuz ise 0,142 m.
        #      Yani levhanin kenarlari govdenin siluetinden TASIYORDU ve
        #      modelde ince bir dil gibi gorunuyordu. Sahne karesinden
        #      anlasilmadi (iki taraftan da "kitabe yok" okunuyordu);
        #      Blender inceleme paketi gosterdi.
        #
        # Artik hem genislik hem derinlik, kitabe yuksekligindeki gercek
        # yaricaptan hesaplaniyor: cokgenin duz yuzu `r cos(pi/n)`de durur ve
        # `2 r sin(pi/n)` genisligindedir.
        kh = min(0.95, shaft_h * 0.42)
        t = 0.58                                    # gövdedeki yükseklik oranı
        r_kit = p.side * 0.5 + (p.side * 0.44 - p.side * 0.5) * t
        half = math.pi / p.sides
        face = r_kit * math.cos(half)
        kw = 2.0 * r_kit * math.sin(half) * 0.82    # yüze paylı sığsın
        _put(parts, hz.make_box(f"{asset_name}_Kitabe",
                                (kw, 0.03, kh),
                                (0.0, -(face - 0.011),
                                 shaft_z + shaft_h * t), col), mats["kitabe"])

    top = p.height
    if p.role == "bas":
        # Bas tasinin BASLIGI vardir: bilezik + kucuk kulah. Ayak tasi
        # sadedir — cift, yalnizca boyla degil bicimle de ayrilir.
        _put(parts, hz.make_box(f"{asset_name}_Bilezik",
                                (p.side * 1.32, p.side * 1.32, 0.13),
                                (0.0, 0.0, top + 0.065), col), mats["marble"])
        _put(parts, hz.make_tube(f"{asset_name}_Kulah", p.side * 0.62, 0.02,
                                 0.34, (0.0, 0.0), top + 0.13, col=col,
                                 segments=p.sides), mats["marble"])
        top += 0.47
    else:
        _put(parts, hz.make_box(f"{asset_name}_Tepe",
                                (p.side * 1.10, p.side * 1.10, 0.10),
                                (0.0, 0.0, top + 0.05), col), mats["marble"])
        top += 0.10

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    # LOD1 de MERMER: uzak siluet malzeme degistirirse tas, LOD sinirinda
    # renk atlar — hareket halindeki bir oyuncunun goreceginden en kotusu.
    l1 = [_solid(f"{asset_name}_L1", (p.side, p.side, top),
                 (0.0, 0.0, top * 0.5), col, mats["marble"])]
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    ucx = hz.make_box(f"UCX_{asset_name}", (p.base_side, p.base_side, top),
                      (0.0, 0.0, top * 0.5), col)
    hz.assign(ucx, mats["marble"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                role=p.role, kind="menzil_tasi", palette=p.palette)
    return lod0, lod1, ucx, info
