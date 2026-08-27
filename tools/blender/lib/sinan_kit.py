"""
Hezarfen: 1632 — **selâtin ölçeğinde** cami kiti (Faz 3, S-kademe).

`mosque_kit.py` mahalle mescidini kurar: ahşap çatılı, tek minareli, mütevazı.
Bu modül öteki uçtur — merkezî kubbeli, yarım kubbeli, çift revaklı, çifte
minareli **anıt** cami. İkisini ayrı tutmanın nedeni tipolojik: mahalle
mescidinin kubbesi yoktur ve olmamalıdır (bkz. `mosque_kit` giriş notu);
burada ise kubbe yapının kendisidir.

İlk ve şimdilik tek yapı: **Üsküdar Mihrimah Sultan (İskele) Camii**, 1548,
Mimar Sinan. Hezarfen'in iniş noktasının silüetini bu yapı belirler.

## Doğruluk basamağı: kubbe D2, gerisi D3

Ölçülen tek şey kubbedir ve **ölçülen sayı türetilene tercih edilir**:

* dış çap **11,40 m**, iç çap **10,00 m**, kubbe yüksekliği **24,20 m**
  (Vardar 2021; Vikipedi iç/dış ayrımı) → **D2**
* çift minare, her biri **tek şerefeli** (Hadîkatü'l Cevâmi: "birer şerefeli
  minaresi") → sayım, **D2**
* **beş kubbeli** birinci revak, **altı mermer sütun** → sayım, **D2**
* çift revak — Sinan'ın **özgün** tipi; Üsküdar Mihrimah beş gözlü çift
  revaklı yedi caminin **ilki** (1543/44-1548) → **D2**
* set/dış avlu **~2 m**, merdivenle çıkılır (TDV) → **D2**
* **üç** yarım kubbe: iki yan + kıble; **giriş yönünde YOK** — planın
  "İstanbul'daki ilk ve tek üç yarım kubbeli örnek" olma nedeni budur → D2

Kütlenin geri kalanı ölçülmemiştir ve **uydurulmamıştır**: hepsi ölçülen
kubbeden türer (kemer açıklığı = dış çap, yarım kubbe yarıçapı = ana kubbe
yarıçapı, çünkü aynı kemerlerden doğarlar). Türetilenler **D3**'tür.

Minare yüksekliği için de ölçü yok. Yazılı bir kural kullanıldı ve kuralın
kendisi burada duruyor: **şerefe, ana kubbe kilidinin kotundadır**; üstüne
petek, külah ve âlem gelir. Sabit bir sayı yazmak, kubbe ölçüsü düzeltilince
sessizce yanlışa dönerdi.

## Bu dosyanın adı artık tam doğru değil

`sinan_kit` diye başladı çünkü ilk sakini Üsküdar Mihrimah'tı. İçinde
şimdi Dâvud Ağa'nın Yeni Cami'si, Sedefkâr Mehmed Ağa'nın Sultanahmet'i
ve **Atik Sinan'ın Fâtih Camii'si** (1470 — Mimar Sinan'dan seksen yıl
önce) var. Doğru ad "Osmanlı selâtin kiti" olurdu.

Yeniden adlandırmak her üreticiye dokunurdu; onun yerine kaymayı buraya
yazıyorum ki ad bir iddia sanılmasın.
"""

import math
import re

import bpy  # noqa: F401  (Blender ortamı)

import detay_kit as dk
import hz_blender as hz
import mosque_kit as mk
import ottoman_kit as kit
import street_kit as sk


# ----------------------------------------------------------------- ÖLÇÜLENLER

#: Merkezî kubbe **dış** çapı (m) — Vardar 2021, "11.40 metre çapında".
DOME_D_OUT = 11.40

#: Merkezî kubbe **iç** çapı (m). Dıştan 1,40 m küçük olması kabuk
#: kalınlığıdır; ikisini karıştırmak kütleyi %14 şişirir.
DOME_D_IN = 10.00

#: Kubbe yüksekliği (m) — harim döşemesinden kilit taşına, Vardar 2021.
DOME_CROWN_Z = 24.20

#: Set / dış avlu yüksekliği (m) — TDV, "yaklaşık 2 m. yükseklikteki geniş
#: bir dış avlu". Yapı düz zemine değil bu setin üstüne oturur.
PODIUM_H = 2.00

#: Birinci revak: beş kubbe, altı mermer sütun (sayım).
PORTICO_BAYS = 5

#: Osmanlı kubbesi basıktır: yükseklik / yarıçap. `mosque_kit` ile aynı oran
#: — iki kit farklı basıklık kullanırsa aynı sahnede iki ayrı üslup okunur.
DOME_RISE_RATIO = 0.78


class MihrimahParams(object):
    """
    Üsküdar Mihrimah Sultan (İskele) Camii, **1548** — 1632'de 84 yaşında.

    ## 1632'de ne VAR

    Cami (1548), medrese (16 hücre), sıbyan mektebi, imaret-tabhâne, han
    (Kurşunlu Han) ve suyolları. İmaret-tabhâne **1722**'de yandığı,
    kervansaray **1920'lerde** çöktüğü için ikisi de 1632'de ayaktadır.

    ## 1632'de ne YOK

    * **iki türbe, hamam, kasır, muvakkithane** — hepsi "sonraki dönemlerde"
      eklendi (Vardar 2021),
    * **güneş saati** (18. yy),
    * set duvarındaki **çeşme** (17. yy; 1632'nin önünde mi arkasında mı
      bilinmiyor — ihtiyatla konmadı),
    * ve meydanın bugünkü iki simgesi: **Yeni Valide Camii** (1708-11,
      kitabe 1122/1710) ile **III. Ahmed Meydan Çeşmesi** (1728). 1632'de
      Üsküdar meydanına tek başına Mihrimah hâkimdir.

    Bunlar modele girmez; listeyi burada tutmanın nedeni ileride birinin
    "eksik" sanıp eklemesini engellemektir.
    """

    def __init__(self, dome_d=DOME_D_OUT, crown_z=DOME_CROWN_Z,
                 podium_h=PODIUM_H, wall_t=1.20, bays=PORTICO_BAYS,
                 outer_revak=True, palette="default"):
        self.dome_d = dome_d
        self.crown_z = crown_z
        self.podium_h = podium_h
        self.wall_t = wall_t
        self.bays = bays
        self.outer_revak = outer_revak
        self.palette = palette

    # --------------------------------------------------------- türetilenler

    @property
    def r(self):
        """Ana kubbe yarıçapı — kemer açıklığının yarısı."""
        return self.dome_d * 0.5

    @property
    def dome_rise(self):
        return self.r * DOME_RISE_RATIO

    @property
    def spring_z(self):
        """Ana kubbenin doğduğu kot: kilit − kabarma."""
        return self.crown_z - self.dome_rise

    @property
    def arch_crown_z(self):
        """
        Kemer kilitlerinin kotu = pandantif eteği.

        Pandantif yüksekliği küresel üçgenden gelir: kare kemerin köşegen
        yarıçapı r·√2, kenar yarıçapı r; fark r·(√2−1).
        """
        return self.spring_z - self.r * (math.sqrt(2.0) - 1.0)

    @property
    def arch_z(self):
        """
        Kemer **eteği** = yarım kubbelerin doğduğu kot = dış saçak.

        İlk yazımda burası kemer KİLİDİ idi ve yarım kubbeler oraya
        oturtulmuştu. Sonuç ölçülebilir bir hataydı: duvar 5,70 m fazla
        yükseldi, kubbe kütlesi gövdenin üstünde bir şapka gibi kaldı ve
        yarım kubbeler ana kubbenin yalnızca 2,36 m altına düştü.

        Doğrusu geometriden çıkar: kemer yarım dairedir, açıklığı ana
        kubbenin çapıdır, dolayısıyla **kabarması yarıçap kadardır**. Yarım
        kubbe bu kemeri doldurur; tabanı kemerin eteğindedir.
        """
        return self.arch_crown_z - self.r

    @property
    def half_r(self):
        """Yarım kubbe yarıçapı = ana kubbeninki (aynı kemerlerden doğar)."""
        return self.r

    @property
    def hall_w(self):
        """Harim dış genişliği: iki yan yarım kubbe + duvar."""
        return 2.0 * (self.r + self.half_r) + 2.0 * self.wall_t

    @property
    def hall_d(self):
        """Harim dış derinliği: kıble yarım kubbesi VAR, giriş yönünde YOK."""
        return (self.r + self.half_r) + self.r + 2.0 * self.wall_t

    @property
    def sherefe_z(self):
        """Şerefe kotu = ana kubbe kilidi (kitin yazılı kuralı)."""
        return self.crown_z

    def validate(self):
        if abs(self.dome_d - DOME_D_OUT) > 0.01:
            raise ValueError(f"kubbe capi {self.dome_d} — olculen sayi "
                             f"{DOME_D_OUT} m (Vardar 2021); degistirmek "
                             "icin once kaynak degistir")
        if self.crown_z <= self.arch_z:
            raise ValueError("kilit, kemer kotunun altinda — oran bozuk")
        # Uc yarim kubbe SART: dordu de konursa plan siradanlasir, ikisi
        # konursa yapi baska bir cami olur.
        if self.bays != PORTICO_BAYS:
            raise ValueError(f"revak {self.bays} gozlu — sayilan deger "
                             f"{PORTICO_BAYS} (bes kubbe, alti sutun)")
        if self.podium_h < 1.0:
            raise ValueError("set en az 1 m: TDV 'yaklasik 2 m' der ve yapi "
                             "meydandan merdivenle cikilir")


# --------------------------------------------------------------- yardımcılar

def _column(name, x, y, z0, h, r, col, mats, capital=True,
            baslik="mukarnas"):
    """
    Mermer sütun — **kaide, gövde, başlık**.

    Önceki hâli bir silindir + bir kutuydu ve yorumu şöyleydi: *"başlık
    kaba: uzaktan gövde/başlık ayrımı yeter."* Yetmiyordu. Revağın ritmini
    okutan şey başlıkların sırasıdır ve Üsküdar Mihrimah'ın kaynağı iki
    başlık tipini **ayrı ayrı** anar (birinci revak **mukarnaslı**, ikinci
    revak **baklava dilimli**) — yani tip bir süs değil **bilgi**dir ve
    modelde karşılığı olmalıydı.
    """
    out = dk.sutun(name, x, y, z0, h, r, col,
                   capital=(baslik if capital else "baklava"), segments=12)
    for o in out:
        hz.assign(o, mats["marble"])
    return out


def _sherefe(name, x, y, z, r, col, mats):
    """
    Şerefe — **mukarnas konsol + tabla + delikli korkuluk**.

    Önceki hâli bir disk ve bir boru halkasıydı; minare siluetinde okunan
    tek ayrıntı budur ve o hâliyle minare "çubuk" gibi kalıyordu. Şerefe
    havada durmaz: altında onu taşıyan **mukarnas** vardır, ve korkuluğu
    korkuluk yapan şey **boşluklarıdır**.
    """
    out = dk.serefe(name, x, y, z, r, col)
    for o in out:
        hz.assign(o, mats["cutstone"] if "Konsol" in o.name
                  or "Tabla" in o.name else mats["marble"])
    return out


def _minaret(p, mats, col, name, x, y, base_z):
    """
    Minare — **tek şerefeli**, şerefesi ana kubbe kilidinde.

    Yükseklik ölçülmedi; kural kitin başında yazılı. Gövde çokgen: klasik
    Osmanlı minaresi silindir değil, çok yüzlüdür ve bu fark siluette
    okunur.
    """
    out = []
    r = 1.05
    # Kaide: kare kursu, sonra pabuc (gecis).
    kaide_h = 6.0
    k = hz.make_box(f"{name}_Kaide", (r * 3.4, r * 3.4, kaide_h),
                    (x, y, base_z + kaide_h * 0.5), col)
    hz.assign(k, mats["cutstone"])
    out.append(k)

    pabuc_h = 3.2
    pb = hz.make_tube(f"{name}_Pabuc", r * 1.70, r * 1.15, pabuc_h, (x, y),
                      base_z + kaide_h, segments=8, col=col)
    hz.assign(pb, mats["cutstone"])
    out.append(pb)

    z = base_z + kaide_h + pabuc_h
    shaft_h = p.sherefe_z - z
    if shaft_h < 4.0:
        raise ValueError(f"minare govdesi {shaft_h:.1f} m — serefe kotu "
                         "kaidenin altina dusmus, oran bozuk")
    for o in dk.minare_govde(f"{name}_Govde", x, y, z, shaft_h, r, r * 0.90,
                             col, segments=12):
        out.append(hz.assign(o, mats["cutstone"]))
    for o in dk.mukarnas(f"{name}_PabucMukarnas", x, y, r * 1.10, r * 1.55,
                         base_z + kaide_h + pabuc_h * 0.55, pabuc_h * 0.45,
                         col, tiers=3, segments=10):
        out.append(hz.assign(o, mats["cutstone"]))

    out += _sherefe(f"{name}_Serefe", x, y, p.sherefe_z, r * 0.90, col, mats)

    petek_h = 4.5
    pt = hz.make_tube(f"{name}_Petek", r * 0.86, r * 0.80, petek_h, (x, y),
                      p.sherefe_z + 1.35, segments=12, col=col)
    hz.assign(pt, mats["cutstone"])
    out.append(pt)

    # Kulah KURSUN ve KONIK: 1632 minaresinin tepesi budur.
    kulah_h = 5.5
    kl = hz.make_tube(f"{name}_Kulah", r * 0.95, 0.0, kulah_h, (x, y),
                      p.sherefe_z + 1.35 + petek_h, segments=12, col=col)
    hz.assign(kl, mats["lead"])
    out.append(kl)

    for o in dk.alem(f"{name}_Alem", x, y,
                     p.sherefe_z + 1.35 + petek_h + kulah_h, col, scale=0.85):
        out.append(hz.assign(o, mats["lead"]))
    return out




def _revak(p, mats, col, name, y_front, base_z, depth, height, bays,
           width, marble=True, timber_roof=False):
    """
    Revak: sütun dizisi + üstü.

    `timber_roof=True` ikinci revağın **ahşap** örtüsüdür. Kuban, 17. yy'a
    tarihlenen çeşmenin ikinci revak **çatısıyla** eşzamanlı olabileceğini
    düşünür — yani revağın kendisi Sinan'ın, örtüsü yenilenmiştir. Örtünün
    1632'deki biçimi bilinmiyor; ahşap sundurma olarak, **D3** kuruldu.
    """
    out = []
    y_back = y_front
    y_out = y_front - depth
    col_r = 0.42
    step = width / bays

    if timber_roof:
        for i in range(bays + 1):
            x = -width * 0.5 + i * step
            out += _column(f"{name}_Sutun{i}", x, y_out + col_r * 1.6,
                           base_z, height, col_r, col, mats, capital=True)
        # Sundurma EGIK olmali: duz bir tabla renderda masa gibi okundu.
        # `mosque_kit._lean_to` tam bu isi yapiyor — ikinci bir nusha yazmak
        # ayni orani iki yerde tutmak olurdu.
        roof = mk._lean_to(f"{name}_Sundurma", width + 1.2,
                           y_back + 0.3, y_out - 0.5,
                           base_z + height + 1.35, base_z + height + 0.30,
                           0.26, col)
        hz.assign(roof, mats["timber_bare"])
        out.append(roof)
        beam = hz.make_box(f"{name}_Hatil", (width + 1.2, 0.34, 0.44),
                           (0.0, y_out + col_r * 1.6, base_z + height + 0.45),
                           col)
        hz.assign(beam, mats["timber_bare"])
        out.append(beam)
        return out

    # Birinci revak: BES KUBBE (sayilan deger). Kemerler `detay_kit`ten;
    # onceki hali sutunlarin ustunde DUZ bir bant ve onun ustunde
    # kubbelerdi — son cemaat yeri boyle okunmuyor, kemer dizisi olmadan
    # revak bir sundurmaya benziyordu.
    out += dk.revak_sirasi(
        mats, col, name,
        -width * 0.5, y_out + col_r * 1.6,
        +width * 0.5, y_out + col_r * 1.6,
        bays, base_z, height, col_r,
        bay=depth - col_r * 1.6, bay_dir=(0.0, 1.0))
    return out


def _revak_side(p, mats, col, name, base_z, depth, height, y0, y1, hall_w):
    """
    İkinci revağın **yan kanatları** (batı ve doğu).

    Kaynak ikinci revağın birinciyi üç yanından çevirdiğini söyler. Yalnız ön
    kanadı kurmak, yapıyı "revağı olan bir cami" yapardı; onu **çifte son
    cemaat yerli** kılan şey sarmasıdır.
    """
    out = []
    col_r = 0.42
    x_out = hall_w * 0.5 + depth - col_r * 1.6
    n = max(2, int(round(abs(y1 - y0) / 3.6)))
    for sgn in (-1, 1):
        for i in range(n + 1):
            y = y0 + (y1 - y0) * i / n
            out += _column(f"{name}{sgn}_Sutun{i}", sgn * x_out, y, base_z,
                           height, col_r, col, mats, capital=True)
        # `_lean_to` `width`i X'e yayar, egimi Y'de verir; yan kanat ise Y
        # boyunca uzanip X'te alcalir — yani doksan derece donmeli. AMA
        # `hz` kutuyu mesh'e yazar ve nesne donusumunu kimlik birakir:
        # parcayi YERINE koyup sonra dondurmek onu DUNYA ORIJINI etrafinda
        # dondurur. Once yalniz PROFILI orijinde kur, dondur, sonra tasi.
        a = sgn * (hall_w * 0.5 - 0.3)          # harime yakin uc: YUKSEK
        b = sgn * (x_out + 0.5)                 # dis uc: ALCAK
        mid = (a + b) * 0.5
        roof = mk._lean_to(f"{name}{sgn}_Sundurma", abs(y1 - y0) + 0.6,
                           a - mid, b - mid,
                           base_z + height + 1.35, base_z + height + 0.30,
                           0.26, col)
        # -90 derece: egimin YUKSEK ucu harim tarafinda kalsin (+90 aynalar).
        roof.rotation_euler = (0.0, 0.0, -math.pi * 0.5)
        roof.location = (mid, (y0 + y1) * 0.5, 0.0)
        hz.assign(roof, mats["timber_bare"])
        out.append(roof)
    return out


# ------------------------------------------------------------------- yapı

def build_mihrimah(p, col, asset_name, textured=False):
    """Üsküdar Mihrimah Sultan Camii (1548). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    hw, hd = p.hall_w, p.hall_d
    # Harim ic merkezi: kible yarim kubbesi VAR, giris yonunde YOK — bu
    # yuzden kutle merkezi kubbe merkezinden +Y'ye kaymaz; kubbe merkezini
    # orijine koyup govdeyi ona gore konumlandiriyoruz.
    y_front = -(p.r + p.wall_t)                 # giris cephesi (-Y)
    y_back = p.r + p.half_r + p.wall_t          # kible cephesi (+Y)
    y_mid = (y_front + y_back) * 0.5

    # --- 1) Set (dis avlu) ----------------------------------------------
    # Dis avlu TDV'de 'genis' diye gecer, olculmemistir (D3). Genislik
    # cift revagin disina bir dolasim seridi birakacak kadar: revak
    # govdeden 6 m tasar, iki yanda 3'er m dolasim.
    set_w, set_d = hw + 12.0, hd + 17.0
    podium = hz.make_box(f"Set_{asset_name}", (set_w, set_d, p.podium_h),
                         (0.0, y_mid - 2.0, p.podium_h * 0.5), col)
    hz.assign(podium, mats["cutstone"])
    parts.append(podium)

    # Merdiven: meydandan sete. TDV "merdivenle cikilan kapi" der.
    steps = 8
    for i in range(steps):
        h = p.podium_h * (i + 1) / steps
        st = hz.make_box(f"Basamak_{i}", (9.0, 0.36, h),
                         (0.0, y_mid - 2.0 - set_d * 0.5 - 0.18 - i * 0.36,
                          h * 0.5), col)
        hz.assign(st, mats["cutstone"])
        parts.append(st)

    z0 = p.podium_h

    # --- 2) Harim gövdesi ------------------------------------------------
    wall_h = p.arch_z
    body = hz.make_box(f"Harim_{asset_name}", (hw, hd, wall_h),
                       (0.0, y_mid, z0 + wall_h * 0.5), col)
    hz.assign(body, mats["cutstone"])
    parts.append(body)

    # Pencere sirasi: alt sira sivri degil dikdortgen kaba kutle — uzaktan
    # okunan sey delik ritmidir, kemer bicimi degil.
    for row, (zz, hh) in enumerate(((0.32, 0.16), (0.60, 0.13))):
        for sgn in (-1, 1):
            for i in range(6):
                x = -hw * 0.5 + hw * (i + 0.5) / 6.0
                w = hz.make_box(f"Pencere_{row}_{sgn}_{i}",
                                (1.30, 0.5, wall_h * hh),
                                (x, y_mid + sgn * (hd * 0.5 - 0.2),
                                 z0 + wall_h * zz), col)
                hz.assign(w, mats["shadow"])
                parts.append(w)

    # --- 3) UC yarim kubbe: iki yan + KIBLE, giriste YOK -----------------
    #
    # Planin "Istanbul'daki ilk ve TEK uc yarim kubbeli ornek" olma nedeni
    # tam olarak dorduncusunun OLMAMASIDIR.
    halfs = ((p.r, 0.0, 0.0),                        # dogu (+X)
             (-p.r, 0.0, math.pi),                   # bati (-X)
             (0.0, p.r, math.pi * 0.5))              # kible (+Y)
    for i, (cx, cy, facing) in enumerate(halfs):
        hd_ = hz.make_half_dome(f"YarimKubbe_{i}", p.half_r,
                                p.half_r * DOME_RISE_RATIO, (cx, cy),
                                z0 + p.arch_z, facing=facing,
                                segments=20, rings=6, col=col)
        hz.assign(hd_, mats["lead"])
        parts.append(hd_)

        # Eksedralar: her yarim kubbenin KIRIS UCLARINDA ikiser tane, yani
        # koselerde. Ilk yazimda duvar yuzune yapistirilmislardi ve renderda
        # siyil gibi okundular; dogru yer, yarim kubbenin capinin iki ucu.
        er = p.half_r * 0.46
        for sg in (-1, 1):
            ex = cx + math.cos(facing + sg * math.pi * 0.5) * (p.half_r - er)
            ey = cy + math.sin(facing + sg * math.pi * 0.5) * (p.half_r - er)
            e = hz.make_half_dome(f"Eksedra_{i}{sg}", er,
                                  er * DOME_RISE_RATIO, (ex, ey),
                                  z0 + p.arch_z,
                                  facing=facing + sg * math.pi * 0.30,
                                  segments=14, rings=5, col=col)
            hz.assign(e, mats["lead"])
            parts.append(e)

    # --- 4) Kasnak + ana kubbe -------------------------------------------
    # KEMER/TYMPANUM bolgesi KARE, kasnak degil. Ilk yazimda burasi
    # sacaktan kubbe etegine kadar uzanan tek bir silindirdi (8,06 m) ve
    # renderda kubbenin altinda bir KULE gibi okundu. Gercekte disaridan
    # gorunen sey once dort kemerin tasidigi kare kutle, sonra pandantifin
    # kisa kasnagidir.
    tymp_h = p.arch_crown_z - p.arch_z
    tymp = hz.make_box(f"Tympanum_{asset_name}",
                       (p.dome_d + 1.4, p.dome_d + 1.4, tymp_h),
                       (0.0, 0.0, z0 + p.arch_z + tymp_h * 0.5), col)
    hz.assign(tymp, mats["cutstone"])
    parts.append(tymp)

    # Kemer alinliklarindaki pencere sirasi: kare kutleyi duz birakmak onu
    # bir kaide gibi gosterirdi; gercekte her alinlikta pencere vardir.
    for sgn in (-1, 1):
        for axis in (0, 1):
            for i in range(3):
                u = (i - 1) * 2.6
                sz = (1.15, 0.5, 1.7) if axis == 0 else (0.5, 1.15, 1.7)
                ps = ((u, sgn * (p.dome_d + 1.4) * 0.5, z0 + p.arch_z + tymp_h * 0.62)
                      if axis == 0 else
                      (sgn * (p.dome_d + 1.4) * 0.5, u, z0 + p.arch_z + tymp_h * 0.62))
                w = hz.make_box(f"AlinlikPencere_{axis}{sgn}{i}", sz, ps, col)
                hz.assign(w, mats["shadow"])
                parts.append(w)

    drum_h = p.spring_z - p.arch_crown_z
    drum = hz.make_tube(f"Kasnak_{asset_name}", p.r * 1.06, p.r * 1.02,
                        drum_h, (0.0, 0.0), z0 + p.arch_crown_z, segments=16,
                        cap_top=False, col=col)
    hz.assign(drum, mats["cutstone"])
    parts.append(drum)

    dome = hz.make_dome(f"Kubbe_{asset_name}", p.r, p.dome_rise, (0.0, 0.0),
                        z0 + p.spring_z, segments=28, rings=8, col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    # KUBBE, BIRLESMEDEN ONCE OLCULUR.
    #
    # Galata turunda ogrenildi: birlestikten sonra olculen sey AYAK IZIDIR,
    # govde degil — orada sacak 0,95 m tasiyordu ve kendi denetimim haksiz
    # yere hata verdi. Burada olculen tek D2 sayidir; yanlissa her sey yanlis.
    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    measured_crown = dmx[2]
    if abs(measured_d - p.dome_d) > 0.06:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} m — olculen sayi "
                         f"{p.dome_d:.2f} m olmali")
    if abs(measured_crown - (z0 + p.crown_z)) > 0.02:
        raise ValueError(f"kubbe kilidi {measured_crown:.3f} m — set dahil "
                         f"{z0 + p.crown_z:.2f} m olmali")

    alem = hz.make_tube(f"KubbeAlem_{asset_name}", 0.13, 0.03, 1.6,
                        (0.0, 0.0), z0 + p.crown_z, segments=6, col=col)
    hz.assign(alem, mats["lead"])
    parts.append(alem)

    # --- 5) Tackapi + cift revak ------------------------------------------
    # Tackapi revaktan ONCE kurulur: duvar duzleminde durur, revak onunde.
    parts += dk.tackapi(mats, col, f"HarimTackapi_{asset_name}",
                        0.0, y_front, z0, 7.4, wall_h * 0.70, 1.7,
                        kapi_w=2.4, kapi_h=3.9, sutunce=False)

    rev1_h, rev1_d = 6.2, 5.2
    parts += _revak(p, mats, col, f"Revak1_{asset_name}", y_front, z0,
                    rev1_d, rev1_h, p.bays, hw)
    if p.outer_revak:
        # Ikinci revak birinciyi UC YANINDAN cevirir (bati-kuzey-dogu);
        # kible duvarinda revak olmaz. Onu yalniz one koymak, yapinin
        # "cifte son cemaat yeri" olma nitelijini yarim birakirdi.
        rev2_d, rev2_h = 4.2, 5.4
        parts += _revak(p, mats, col, f"Revak2_{asset_name}",
                        y_front - rev1_d, z0, rev2_d, rev2_h, p.bays + 2,
                        hw + 2.0 * rev2_d, timber_roof=True)
        parts += _revak_side(p, mats, col, f"Revak2Yan_{asset_name}", z0,
                             rev2_d, rev2_h, y_front - rev1_d,
                             y_front + rev1_d * 0.15, hw)

    # --- 6) Cifte minare, her biri TEK serefeli --------------------------
    for sgn in (-1, 1):
        parts += _minaret(p, mats, col, f"Minare_{'D' if sgn > 0 else 'B'}",
                          sgn * (hw * 0.5 + 1.6), y_front + 0.8, z0)

    # --- LOD1: siluet ----------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Set", (set_w, set_d, p.podium_h),
                                    (0.0, y_mid - 2.0, p.podium_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_box("L1_Harim", (hw, hd, wall_h),
                                    (0.0, y_mid, z0 + wall_h * 0.5), col),
                        mats["cutstone"]))
    for i, (cx, cy, facing) in enumerate(halfs):
        l1.append(hz.assign(
            hz.make_half_dome(f"L1_Yarim{i}", p.half_r,
                              p.half_r * DOME_RISE_RATIO, (cx, cy),
                              z0 + p.arch_z, facing=facing,
                              segments=10, rings=3, col=col), mats["lead"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", p.r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.spring_z, segments=14, rings=4,
                                     col=col), mats["lead"]))
    for sgn in (-1, 1):
        x = sgn * (hw * 0.5 + 1.6)
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{sgn}", 1.05, 0.80,
                         p.sherefe_z + 1.35 + 4.5, (x, y_front + 0.8), z0,
                         segments=8, col=col), mats["cutstone"]))
        l1.append(hz.assign(
            hz.make_tube(f"L1_Kulah{sgn}", 1.0, 0.0, 5.5, (x, y_front + 0.8),
                         z0 + p.sherefe_z + 1.35 + 4.5, segments=8, col=col),
            mats["lead"]))

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
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D3", dome_d=round(p.dome_d, 2),
                dome_crown_z=round(p.crown_z, 2),
                podium_h=round(p.podium_h, 2), minarets=2, sherefe_each=1,
                half_domes=3, portico_bays=p.bays, double_portico=True,
                minaret_top=round(p.sherefe_z + 1.35 + 4.5 + 5.5 + 1.2, 2),
                measured_dome_d=round(measured_d, 3),
                measured_crown_z=round(measured_crown, 3))
    return lod0, lod1, ucx, info


# ============================================ Yeni Cami harabesi (1632)

#: Harim **ölçülü** plan boyutları (m) — 35,50 × 40,90.
YENI_HARIM_W, YENI_HARIM_D = 35.50, 40.90

#: Ana kubbe çapı (m). Kaynaklarda iki sayı dolaşır: mimari tarif
#: **16,20 m**, yaygın anlatım **17,5 m** — büyük olasılıkla iç/dış farkı
#: (Üsküdar Mihrimah'ta aynı çelişki tam bu şekilde çözülmüştü). 1632
#: kabuğu için ikisi de kullanılmaz; kayda geçmesi için burada duruyor.
YENI_DOME_D_IN, YENI_DOME_D_OUT = 16.20, 17.50

#: Ana kubbeyi taşıyacak fil ayağı sayısı (sayım).
YENI_FIL_AYAGI = 4


class YeniCamiHarabeParams(object):
    """
    Yeni Cami, **1632** — bitmemiş, terk edilmiş bir kabuk.

    ## Yapı 1632'de bir CAMİ DEĞİLDİR

    İnşaat **1597**'de Safiye Sultan'ın emriyle başladı (mimar Dâvud Ağa,
    ardından Dalgıç Ahmed Çavuş). **1603**'te III. Mehmed ölünce Safiye
    Sultan Eski Saray'a gönderildi ve iş durdu; **1604**'te Safiye
    Sultan'ın ölümüyle tamamen bırakıldı. Yapı **57 yıl** öyle kaldı;
    **1660** yangınından sonra Turhan Sultan sürdürdü, **1663**'te
    tamamlandı.

    **1632'de kabuk 29 yıldır terk edilmiştir.**

    ## Nereye kadar yükselmişti

    İş durduğunda yapı **ilk pencere seviyesine** kadar çıkmıştı. Yani
    1632'de görülen şey duvarlar ve fil ayaklarıdır: **çatısız**, kubbesiz,
    minaresiz, kurşunsuz.

    ## Halkın adı: **Zulmiye**

    Aşırı masraf ek vergilere yol açtığı ve yapı harabeye döndüğü için
    İstanbullular ona "Zulmiye" derdi. Bu bir renk değil sahnenin anlamı:
    1632'de Eminönü'nde duran şey bir ibadet yapısı değil, bir **şikâyet
    konusudur**.

    ## Yıkıntı değil, DURMUŞ ŞANTİYE

    İkisi farklı görünür ve karıştırmak yapıyı yanlış anlatır. Yıkıntının
    üstü düzensiz kırılır; durmuş bir şantiyenin üstü **sıra sıra** biter —
    hangi taş sırasında bırakıldıysa orada. Model bu yüzden üst kenarı
    rastgele değil, **taş sırası adımlarıyla** değiştirir; ve avluda
    işlenmiş ama yerine konmamış **taş yığınları** durur.
    """

    def __init__(self, width=YENI_HARIM_W, depth=YENI_HARIM_D,
                 wall_t=1.60, wall_h=7.40, course=0.52,
                 piers=YENI_FIL_AYAGI, pier_side=4.20, palette="default"):
        self.width, self.depth = width, depth
        self.wall_t = wall_t
        self.wall_h = wall_h          # ilk pencere seviyesi (D3)
        self.course = course          # tas sirasi yuksekligi
        self.piers = piers
        self.pier_side = pier_side
        self.palette = palette

    def validate(self):
        if abs(self.width - YENI_HARIM_W) > 0.01 \
           or abs(self.depth - YENI_HARIM_D) > 0.01:
            raise ValueError(f"harim {self.width}x{self.depth} — olculen plan "
                             f"{YENI_HARIM_W}x{YENI_HARIM_D} m")
        if self.piers != YENI_FIL_AYAGI:
            raise ValueError(f"piers={self.piers} — ana kubbe DORT fil "
                             "ayagina oturur")
        # KABUK CATISIZ KALMALI. Duvar cok yukselirse yapi "bitmis" okunur.
        if self.wall_h > 12.0:
            raise ValueError(f"wall_h={self.wall_h} — 1603'te is ILK PENCERE "
                             "seviyesinde durdu; bu yukseklik bitmis bir "
                             "cami gibi okunur")
        return self


def build_yeni_cami_harabe(p, col, asset_name, textured=False):
    """Yeni Cami'nin 1632'deki bitmemiş kabuğu. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D, t = p.width, p.depth, p.wall_t

    # --- Subasman: is once buradan basladi, saglam durur ----------------
    parts.append(hz.assign(hz.make_box(f"Subasman_{asset_name}",
                                       (W + 1.4, D + 1.4, 1.10),
                                       (0.0, 0.0, 0.55), col),
                           mats["cutstone"]))
    z0 = 1.10

    # --- Cevre duvarlari: her parca KENDI kotunda biter ------------------
    #
    # Ust kenar rastgele DEGIL: is hangi tas sirasinda birakildiysa orada
    # biter. Yukseklik `course` katlariyla degisir — durmus bir santiye
    # boyle gorunur, yikilmis bir duvar boyle gorunmez.
    # RITIM ONEMLI. Ilk denemede yedi parca ve alternatif kotlar vardi
    # (0,1,0,2,1,3,1) ve render'da MAZGAL gibi okundu — sanki bir kale
    # bedeni. Durmus bir santiyede kotlar alternatiflenmez: uzun duzlukler
    # ve birkac basamak olur, ve bir cephe otekinden DAHA ILERI gitmistir.
    # Bes parca, artan ve tekdüze bir dizi, artı cephe basina taban kayması.
    segs = 5
    drop = (0, 0, 1, 1, 2)                # kac sira eksik — uzun duzlukler
    side_base = (0, 1, 3, 2)              # her cephe farkli kotta birakildi
    sides = (((1, 0), -1), ((1, 0), 1), ((0, 1), -1), ((0, 1), 1))
    for side, (ax, sign) in enumerate(sides):
        span = W if ax == (1, 0) else D
        off = (D * 0.5 - t * 0.5) if ax == (1, 0) else (W * 0.5 - t * 0.5)
        for i in range(segs):
            u = ((i + 0.5) / segs - 0.5) * span
            sw = span / segs
            idx = i if side % 2 == 0 else segs - 1 - i
            h = p.wall_h - (drop[idx] + side_base[side]) * p.course
            cx = (u, sign * off) if ax == (1, 0) else (sign * off, u)
            size = ((sw, t, h) if ax == (1, 0) else (t, sw, h))
            parts.append(hz.assign(
                hz.make_box(f"Duvar_{side}{i}", size,
                            (cx[0], cx[1], z0 + h * 0.5), col),
                mats["cutstone"]))

    # --- DORT FIL AYAGI --------------------------------------------------
    #
    # Kubbe yok ama ayaklar VAR: is durdugunda onlar da yukselmisti ve
    # kabugun icinde duran sey odur. Ayaklar duvardan biraz YUKSEK biter;
    # tasiyici once yukselir.
    span = YENI_DOME_D_IN * 0.5 + p.pier_side * 0.5
    ph = p.wall_h + 2.0 * p.course
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(hz.assign(
                hz.make_box(f"FilAyagi_{sx}{sy}",
                            (p.pier_side, p.pier_side, ph),
                            (sx * span, sy * span, z0 + ph * 0.5), col),
                mats["cutstone"]))

    # --- Mihrap duvarindaki cikinti (+Y = kible) ------------------------
    parts.append(hz.assign(hz.make_box(f"Mihrap_{asset_name}",
                                       (6.0, 2.2, p.wall_h - p.course),
                                       (0.0, D * 0.5 + 1.1,
                                        z0 + (p.wall_h - p.course) * 0.5),
                                       col), mats["cutstone"]))

    # --- Terk edilmisligin isareti: yigilmis kesme tas bloklar ----------
    #
    # Santiye BIRAKILMISTIR, sokulmemistir: islenmis ama yerine konmamis
    # taslar avluda oylece durur. "Yikinti" ile "durmus is" arasindaki
    # farki tek basina anlatan sey budur.
    piles = ((-W * 0.28, -D * 0.36, 3.2, 2.0, 1.15),
             (W * 0.31, -D * 0.30, 2.4, 2.4, 0.85),
             (-W * 0.05, D * 0.30, 4.0, 1.6, 0.60),
             (W * 0.18, D * 0.36, 2.0, 1.8, 1.40))
    for i, (bx, by, bw, bd, bh) in enumerate(piles):
        parts.append(hz.assign(hz.make_box(f"TasYigini_{i}", (bw, bd, bh),
                                           (bx, by, z0 + bh * 0.5), col),
                               mats["cutstone"]))

    l1.append(hz.assign(hz.make_box("L1_Kabuk", (W, D, p.wall_h + z0),
                                    (0.0, 0.0, (p.wall_h + z0) * 0.5), col),
                        mats["cutstone"]))

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
                kind="harabe", palette=p.palette, status="draft",
                accuracy="D3", harim_w=p.width, harim_d=p.depth,
                piers=p.piers, roofed=False, minarets=0,
                wall_h=round(p.wall_h, 2))
    return lod0, lod1, ucx, info


# ================================================ Süleymaniye Camii (1557)

#: Merkezî kubbe çapı (m). Kaynaklarda **26,5** ve **27,5** dolaşır —
#: Mihrimah ve Yeni Cami'dekiyle aynı iç/dış ikiliği (§5.4, §5.9). Yapının
#: kütlesi taşıyıcı açıklıktan kurulduğu için **26,5** kullanıldı.
SUL_DOME_D = 26.50

#: Kubbe kilit kotu (m) — harim döşemesinden.
SUL_CROWN_Z = 53.00

#: Yarım kubbe sayısı: **İKİ**, ana eksende (Ayasofya şeması). Üsküdar
#: Mihrimah'ta üçtü; sayı yapıyı tanımlar, süslemez.
SUL_HALF_DOMES = 2

#: Minare şerefe dizisi. **Dört minare, ON şerefe**: ikisi üçer, ikisi
#: ikişer. Yaygın yorum, Süleyman'ın **onuncu** padişah ve İstanbul'da
#: hüküm süren **dördüncü** padişah olmasına bağlar. Sayı belgelidir;
#: yorum değil sayı bağlayıcıdır.
SUL_SEREFE = (3, 3, 2, 2)

#: Süleymaniye'nin **ölçülen** eksen azimutu (ızgara kuzeyinden).
#:
#: Plandan iki bağımsız yolla okundu — dört minarenin köşegen açıortayı
#: (137,95°) ve minare ağırlık merkezinden kubbe merkezine giden hat
#: (140,05°); ikisi 2°'nin altında buluşuyor, alınan değer **139,0**.
#:
#: Şehrin 1632 kıblesi **133,7°**'dir (ADR 0046, on tarihî camiden
#: ölçüldü). Süleymaniye ondan **5,3°** sapar — küçük bir fark, ama
#: **ölçülmüş** bir fark ve "ölçülen türetileni yener". Bu yüzden yapı
#: kendi yönünü bildirir; şehir medyanına yuvarlanmaz.
SUL_AXIS_DEG = 139.0

#: Şehrin 1632 kıblesi (ızgara) — sapma bunun üzerinden yazılır.
QIBLA_1632_DEG = 133.7


class SuleymaniyeParams(object):
    """
    Süleymaniye Camii, **1550-1557**, Mimar Sinan — 1632'de 75 yaşında.

    ## Bu yapıda tanıdık siluet DOĞRUDUR

    Faz 3'ün alışkanlığı "bildiğin hâli 1632 değildir" oldu: Galata
    Kulesi, Adalet Kulesi, Kız Kulesi, Yeni Cami, Alay Köşkü — hepsinde
    tanınan görüntü sonraki yüzyılların eseriydi. **Süleymaniye öyle
    değil.** 1557'de tamamlandı ve 1632'ye kadar biçimini değiştiren bir
    olay yok; onu hırpalayan **1660 yangını** ve **1766 depremi** sonradır.

    Bunu yazmak, düzeltmeleri yazmak kadar önemli: kural "her şey
    farklıdır" değil, **"her şey sorulur"**.

    ## Sayılar

    * kubbe **26,5 m** çap, **53 m** kilit,
    * **iki** yarım kubbe (ana eksende — Ayasofya şeması),
    * **dört** minare, **on** şerefe (3+3+2+2),
    * harim yaklaşık 68×63 m.
    """

    def __init__(self, dome_d=SUL_DOME_D, crown_z=SUL_CROWN_Z,
                 half_domes=SUL_HALF_DOMES, sherefe=SUL_SEREFE,
                 hall_w=68.0, hall_d=63.0, wall_t=2.2,
                 bays=7, palette="default"):
        self.dome_d, self.crown_z = dome_d, crown_z
        self.half_domes = half_domes
        self.sherefe = tuple(sherefe)
        self.hall_w, self.hall_d = hall_w, hall_d
        self.wall_t = wall_t
        self.bays = bays
        self.palette = palette

    # Kubbe zinciri Mihrimah'takiyle AYNI geometridir (ADR 0036): kemer
    # yarim dairedir, kabarmasi yaricap kadardir; sacak = kemer etegi.
    @property
    def r(self):
        return self.dome_d * 0.5

    @property
    def dome_rise(self):
        return self.r * DOME_RISE_RATIO

    @property
    def spring_z(self):
        return self.crown_z - self.dome_rise

    @property
    def arch_crown_z(self):
        return self.spring_z - self.r * (math.sqrt(2.0) - 1.0)

    @property
    def arch_z(self):
        return self.arch_crown_z - self.r

    @property
    def sherefe_z(self):
        """Şerefe kotu = ana kubbe kilidi (kitin yazılı kuralı)."""
        return self.crown_z

    def validate(self):
        if abs(self.dome_d - SUL_DOME_D) > 0.01:
            raise ValueError(f"kubbe capi {self.dome_d} — olculen "
                             f"{SUL_DOME_D} m")
        if self.half_domes != SUL_HALF_DOMES:
            raise ValueError(f"half_domes={self.half_domes} — Suleymaniye "
                             "IKI yarim kubbelidir (ana eksende)")
        if len(self.sherefe) != 4 or sum(self.sherefe) != 10:
            raise ValueError(f"serefe {self.sherefe} — DORT minare, ON "
                             "serefe (3+3+2+2)")
        return self


def _minaret_multi(p, mats, col, name, x, y, base_z, sherefe, top_z):
    """
    Çok şerefeli minare.

    Şerefeler gövdeyi **eşit aralıklarla** böler: üç şerefeli bir minarede
    ilki en altta, sonuncusu peteğin hemen altındadır. Tek şerefeli
    `_minaret` bu işi yapmaz ve Mihrimah'ta öyle kalmalı — orada şerefe
    sayısı **sayılmış** bir değerdir (birer).
    """
    out = []
    r = 1.35
    kaide_h, pabuc_h = 8.0, 4.0
    out.append(hz.assign(hz.make_box(f"{name}_Kaide",
                                     (r * 3.2, r * 3.2, kaide_h),
                                     (x, y, base_z + kaide_h * 0.5), col),
                         mats["cutstone"]))
    out.append(hz.assign(hz.make_tube(f"{name}_Pabuc", r * 1.60, r * 1.12,
                                      pabuc_h, (x, y), base_z + kaide_h,
                                      segments=8, col=col), mats["cutstone"]))

    z = base_z + kaide_h + pabuc_h
    shaft_h = top_z - z
    if shaft_h < 8.0:
        raise ValueError(f"minare govdesi {shaft_h:.1f} m — oran bozuk")
    # GOVDE COK YUZLU: klasik Osmanli minaresi silindir degildir ve fark
    # siluette okunur. Kaide ile govde arasindaki pabuc da mukarnasla
    # gecer — dizi kaide/pabuc/govde/serefe/petek/kulah/alem'dir ve
    # her biri ayri okunmalidir.
    for o in dk.minare_govde(f"{name}_Govde", x, y, z, shaft_h, r, r * 0.86,
                             col, segments=16):
        out.append(hz.assign(o, mats["cutstone"]))
    for o in dk.mukarnas(f"{name}_PabucMukarnas", x, y, r * 1.10, r * 1.55,
                         base_z + kaide_h + pabuc_h * 0.55,
                         pabuc_h * 0.45, col, tiers=3, segments=10):
        out.append(hz.assign(o, mats["cutstone"]))

    # SEREFELER: govdeyi esit boler.
    for i in range(sherefe):
        sz = z + shaft_h * (i + 1) / (sherefe + 0.35)
        rr = r * (1.0 - 0.14 * (sz - z) / max(shaft_h, 1e-3))
        out += _sherefe(f"{name}_Serefe{i}", x, y, sz, rr, col, mats)

    petek_h, kulah_h = 5.2, 7.0
    out.append(hz.assign(hz.make_tube(f"{name}_Petek", r * 0.82, r * 0.76,
                                      petek_h, (x, y), top_z, segments=14,
                                      col=col), mats["cutstone"]))
    out.append(hz.assign(hz.make_tube(f"{name}_Kulah", r * 0.90, 0.0, kulah_h,
                                      (x, y), top_z + petek_h, segments=14,
                                      col=col), mats["lead"]))
    # ALEM: kure dizisi + hilal. Duz bir cubuk degil.
    for o in dk.alem(f"{name}_Alem", x, y, top_z + petek_h + kulah_h, col,
                     scale=1.0):
        out.append(hz.assign(o, mats["lead"]))
    return out


def build_suleymaniye(p, col, asset_name, textured=False):
    """Süleymaniye Camii (1557). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D, t = p.hall_w, p.hall_d, p.wall_t

    # --- Set ------------------------------------------------------------
    podium_h = 2.6
    parts.append(hz.assign(hz.make_box(f"Set_{asset_name}",
                                       (W + 14.0, D + 18.0, podium_h),
                                       (0.0, -3.0, podium_h * 0.5), col),
                           mats["cutstone"]))
    z0 = podium_h

    # --- Harim: DORT CEPHE, GERCEK KEMERLI PENCERELERLE -------------------
    #
    # Onceki hali tek bir kutu ve ona yapistirilmis koyu dikdortgenlerdi.
    # Suleymaniye'nin cephesi UC katli pencere sirasi tasir ve yapiyi
    # okutan sey o ritimdir.
    wall_h = p.arch_z
    kat = wall_h / 3.0
    rows = ((9, 1.9, kat), (9, 1.7, kat), (7, 1.5, kat))
    kabuk, built_h = dk.kabuk(mats, col, f"Harim_{asset_name}", W, D,
                                   p.wall_t, z0, rows)
    parts += kabuk
    # Sacak silmesi: kutlenin ustunu bitiren basamakli profil.
    for o in dk.silme(f"Sacak_{asset_name}", W, D, z0 + built_h, col,
                      steps=3, h=0.9, out=0.55):
        parts.append(hz.assign(o, mats["cutstone"]))
    # HARIM TACKAPISI: avludan harime acilan anitsal giris. Revagin
    # arkasinda kalir ama revagin kemer araligindan gorunur — Osmanli
    # avlusunun bakisi zaten oraya kurulur.
    parts += dk.tackapi(mats, col, f"HarimTackapi_{asset_name}",
                        0.0, -D * 0.5, z0, 10.4, wall_h * 0.74, 2.0,
                        kapi_w=2.8, kapi_h=4.4)


    # --- IKI yarim kubbe: ANA EKSENDE (kible ve giris) -------------------
    #
    # Uc degil IKI. Uskudar Mihrimah'ta uctu ve orada yan yarim kubbeler
    # plani tanimliyordu; burada eksen boyunca iki tanedir (Ayasofya
    # semasi) ve yanlar kemerli duvarlarla kapanir.
    halfs = ((0.0, p.r, math.pi * 0.5), (0.0, -p.r, -math.pi * 0.5))
    for i, (cx, cy, facing) in enumerate(halfs):
        parts.append(hz.assign(
            hz.make_half_dome(f"YarimKubbe_{i}", p.r, p.r * DOME_RISE_RATIO,
                              (cx, cy), z0 + p.arch_z, facing=facing,
                              segments=24, rings=7, col=col), mats["lead"]))
        # Yarim kubbenin de dikisleri var: yayin YARISI kadar.
        for o in dk.kubbe_kaburga(f"YarimDikis_{i}", cx, cy, p.r,
                                  z0 + p.arch_z, p.r * DOME_RISE_RATIO, col,
                                  n=16, a0=facing - math.pi * 0.5,
                                  a1=facing + math.pi * 0.5):
            parts.append(hz.assign(o, mats["lead"]))
        er = p.r * 0.44
        for sg in (-1, 1):
            ex = cx + math.cos(facing + sg * math.pi * 0.5) * (p.r - er)
            ey = cy + math.sin(facing + sg * math.pi * 0.5) * (p.r - er)
            parts.append(hz.assign(
                hz.make_half_dome(f"Eksedra_{i}{sg}", er,
                                  er * DOME_RISE_RATIO, (ex, ey),
                                  z0 + p.arch_z,
                                  facing=facing + sg * math.pi * 0.30,
                                  segments=16, rings=5, col=col),
                mats["lead"]))

    # --- Tympanum + kasnak + ana kubbe ----------------------------------
    tymp_h = p.arch_crown_z - p.arch_z
    parts.append(hz.assign(hz.make_box(f"Tympanum_{asset_name}",
                                       (p.dome_d + 3.0, p.dome_d + 3.0,
                                        tymp_h),
                                       (0.0, 0.0, z0 + p.arch_z + tymp_h * 0.5),
                                       col), mats["cutstone"]))
    drum_h = p.spring_z - p.arch_crown_z
    parts.append(hz.assign(hz.make_tube(f"Kasnak_{asset_name}", p.r * 1.05,
                                        p.r * 1.02, drum_h, (0.0, 0.0),
                                        z0 + p.arch_crown_z, segments=24,
                                        cap_top=False, col=col),
                           mats["cutstone"]))
    dome = hz.make_dome(f"Kubbe_{asset_name}", p.r, p.dome_rise, (0.0, 0.0),
                        z0 + p.spring_z, segments=32, rings=9, col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    # KUBBE, BIRLESMEDEN ONCE OLCULUR (Galata dersi, ADR 0033).
    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    if abs(measured_d - p.dome_d) > 0.08:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} m — olculen "
                         f"{p.dome_d:.2f} m olmali")

    # KURSUN DILIM DIKISLERI: kubbenin ustundeki en gorunur doku ve ucus
    # oyununda kubbe YUKARIDAN gorulur. Dikissiz bir kubbe plastik bir
    # kure gibi okunuyordu.
    for o in dk.kubbe_kaburga(f"KubbeDikis_{asset_name}", 0.0, 0.0, p.r,
                              z0 + p.spring_z, p.dome_rise, col, n=32):
        parts.append(hz.assign(o, mats["lead"]))

    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z0 + p.crown_z, col,
                     scale=1.6):
        parts.append(hz.assign(o, mats["lead"]))

    # --- Revakli son cemaat yeri -----------------------------------------
    parts += _revak(p, mats, col, f"Revak_{asset_name}", -D * 0.5, z0,
                    6.5, 8.0, p.bays, W)

    # --- REVAKLI AVLU ------------------------------------------------------
    #
    # Avlu bir sus degil YAPININ PARCASI: kisa iki minare onun DIS
    # koselerinde durur. Ilk kurulumda avlu yoktu ve o iki minare renderda
    # BOSLUKTA duruyordu — yapinin yarisini atlayinca oteki yarisi da
    # yanlis okunuyor.
    avlu_d = 34.0
    avlu_y0 = -D * 0.5
    avlu_y1 = avlu_y0 - avlu_d
    rev_h = 5.6
    avlu_h = dk.revak_ust(avlu_d, 6, rev_h) + 0.6
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"AvluDuvar_{sx}", (1.6, avlu_d, avlu_h),
                        (sx * (W * 0.5 - 0.8), (avlu_y0 + avlu_y1) * 0.5,
                         z0 + avlu_h * 0.5), col), mats["cutstone"]))
    parts.append(hz.assign(
        hz.make_box(f"AvluDuvar_On", (W, 1.6, avlu_h),
                    (0.0, avlu_y1 + 0.8, z0 + avlu_h * 0.5), col),
        mats["cutstone"]))
    # AVLU REVAKI: sutun + kemer + alinlik + kubbe.
    #
    # Onceki hali "duvar ustunde duz kubbeler"di ve yorumunda "uzaktan
    # okunan sey kubbelerin ritmidir, sutunlar degil" yaziyordu. Yanlisti:
    # avluyu avlu yapan sey KEMER RITMIDIR ve kubbeler ona oturur.
    for sx in (-1, 1):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_{sx}",
            sx * (W * 0.5 - 4.6), avlu_y1 + 4.6,
            sx * (W * 0.5 - 4.6), avlu_y0 - 4.6,
            6, z0, rev_h, 0.44,
            bay=4.6, bay_dir=(sx, 0.0))
    parts += dk.revak_sirasi(
        mats, col, "AvluRevakOn",
        -W * 0.5 + 4.6, avlu_y1 + 4.6, W * 0.5 - 4.6, avlu_y1 + 4.6,
        9, z0, rev_h, 0.44,
        bay=4.6, bay_dir=(0.0, -1.0))
    # Sadirvan: avlunun ortasinda.
    parts.append(hz.assign(
        hz.make_tube(f"Sadirvan_{asset_name}", 3.2, 3.0, 1.5,
                     (0.0, (avlu_y0 + avlu_y1) * 0.5), z0, segments=12,
                     col=col), mats["marble"]))

    # --- DORT MINARE, ON SEREFE ------------------------------------------
    #
    # Uzun ikisi (uc serefeli) HARIM kosesinde, kisa ikisi (iki serefeli)
    # avlu kosesindedir — Suleymaniye'nin bilinen dizilisi budur.
    corners = ((1, avlu_y0 - 1.0, 3), (-1, avlu_y0 - 1.0, 3),
               (1, avlu_y1 + 1.0, 2), (-1, avlu_y1 + 1.0, 2))
    for i, (sx, cy, ser) in enumerate(corners):
        top = p.sherefe_z if ser == 3 else p.sherefe_z * 0.78
        parts += _minaret_multi(p, mats, col, f"Minare{i}",
                                sx * (W * 0.5 + 2.0), cy, z0, ser, top)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Harim", (W, D, wall_h),
                                    (0.0, 0.0, z0 + wall_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", p.r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.spring_z, segments=16, rings=5,
                                     col=col), mats["lead"]))
    for i, (cx, cy, facing) in enumerate(halfs):
        l1.append(hz.assign(
            hz.make_half_dome(f"L1_Yarim{i}", p.r, p.r * DOME_RISE_RATIO,
                              (cx, cy), z0 + p.arch_z, facing=facing,
                              segments=12, rings=4, col=col), mats["lead"]))
    for i, (sx, cy, ser) in enumerate(corners):
        top = p.sherefe_z if ser == 3 else p.sherefe_z * 0.78
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{i}", 1.35, 1.0, top + 5.2,
                         (sx * (W * 0.5 + 2.0), cy), z0, segments=8, col=col),
            mats["cutstone"]))

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
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D2", dome_d=round(p.dome_d, 2),
                dome_crown_z=round(p.crown_z, 2),
                measured_dome_d=round(measured_d, 3),
                half_domes=p.half_domes, minarets=len(p.sherefe),
                sherefe_total=sum(p.sherefe),
                sherefe_each=list(p.sherefe), portico_bays=p.bays,
                double_portico=False, podium_h=podium_h,
                minaret_top=round(p.sherefe_z + 5.2 + 7.0 + 1.5, 2),
                # OLCULEN eksen sehrin kible medyanina yuvarlanmaz.
                face_deg=round((SUL_AXIS_DEG + 180.0) % 360.0, 1),
                qibla_offset_deg=round(abs(SUL_AXIS_DEG - QIBLA_1632_DEG), 1))
    return lod0, lod1, ucx, info


# ============================================ Sultan Ahmed Camii (1609-1616)

#: Ana kubbe çapı (m) — **açıklık**.
#:
#: Bu sayı plandan **çıkarıldı**, kaynaktan kopyalanmadı: ayak duvarlarının
#: eksenleri 30,75 m aralıklı, duvar kalınlığı 3,65 m → iç yüzler arası
#: **23,45 m**. Yayımlanan **23,5 m** tam olarak budur.
#:
#: Aynı kubbenin **üç** ayrı sayısı var ve üçü de doğru:
#:
#: * **22,40 m** — TDV, "içten" (kubbe kabuğunun eteği),
#: * **23,50 m** — açıklık (ayaklar arası),
#: * **27,7 m** — plandan okunan kurşun izi, yani **kasnak + saçak**.
#:
#: Ayasofya'da (§5.11) yalnızca iki sayı vardı; orada kasnak yok. Osmanlı
#: kubbesi kasnağa oturur ve üçüncü bir sayı doğurur. Bu bir muhasebe
#: sıkıntısı değil, mimari bir fark.
SA_DOME_D = 23.50
SA_DOME_D_IN = 22.40

#: Kubbe kilidi, harim döşemesinden (m).
SA_CROWN_Z = 43.00

#: **DÖRT** yarım kubbe — dört yönde birer tane. Ayasofya ve
#: Süleymaniye'de **iki** (ana eksende), Üsküdar Mihrimah'ta **üç**.
#: Sayı planı tanımlar; Sultanahmet'inki dört yapraklı şemadır.
SA_HALF_DOMES = 4

#: Yarım kubbe çapı (m) — plandan.
SA_HALF_D = 21.60

#: Her yarım kubbe **üç** eksedra ile genişler (TDV) → toplam **on iki**.
#:
#: Ayasofya'da eksedralar mesh'e girmedi çünkü iç mekân öğesiydiler
#: (ADR 0045). **Burada girerler**: Sultanahmet'in eksedraları yarım
#: kubbelerin eteğinden dışa taşar ve siluetin basamaklı kaskadını onlar
#: yapar. Aynı sözcük, iki yapıda iki ayrı şey.
SA_EXEDRAE_PER_HALF = 3

#: Ana kubbeyi taşıyan **dört fil ayağı**, çapı **5 m** (TDV).
SA_PIERS, SA_PIER_D = 4, 5.00

#: **ALTI** minare ve **ON ALTI** şerefe: harim köşesindeki dördü üçer,
#: avlu köşesindeki ikisi ikişer. O güne kadar denenmemiş bir düzendir.
SA_SEREFE = (3, 3, 3, 3, 2, 2)

#: Minare konumları (X, Y, şerefe, boy) — **plandan ölçüldü**.
#: +Y kıble yönü, +X kıbleye bakarken sağ yan.
SA_MINARETS = (
    (-30.5, +28.8, 3, 64.0),   # harim, kible-sol
    (+30.5, +28.8, 3, 64.0),   # harim, kible-sag
    (-30.5, -25.2, 3, 64.0),   # harim, giris-sol
    (+30.5, -25.2, 3, 64.0),   # harim, giris-sag
    (-31.5, -82.0, 2, 54.0),   # avlu dis kosesi
    (+31.5, -82.0, 2, 54.0),
)

#: Harim ve avlu kütleleri (m) — plandan.
#: Yayımlanan "64 × 72 m" yapının **neresini** anlattığını söylemiyor;
#: Ayasofya'daki tuzağın aynısı (§5.11), üçüncü kez. Ölçülen kullanıldı.
SA_HARIM_W, SA_HARIM_D = 61.0, 55.0
SA_COURT_W, SA_COURT_D = 65.6, 55.3

#: Avlu revakı: **yirmi altı sütun**, **otuz** kubbeli birim (TDV).
SA_COURT_COLS, SA_COURT_BAYS = 26, 30


class SultanahmetParams(object):
    """
    Sultan Ahmed Camii, **1609-1616**, Sedefkâr Mehmed Ağa.

    ## 1632'de on altı yaşında

    Süleymaniye'de (ADR 0044) "1557'den beri değişmedi" demiştim.
    Sultanahmet daha da keskin: **1616'da açıldı**, yani IV. Murad'ın
    İstanbul'unda bu yapı **yeni**. Şehrin en tanınan silueti, oyunun
    geçtiği yıl daha bir kuşak eskimemiştir.

    1632'de tamam olan: cami (1616), arasta ve hamam (1617), **Sultan
    Ahmed türbesi (1619**, II. Osman tamamlattı), medrese-darüşşifa-imaret
    (1620). Yani külliye bütünüyle ayakta.

    1632'de **yok**: III. Selim'in su haznesi (1802 sonrası). Bu yapıda
    "sonradan eklendi" listesi kısadır — Faz 3'ün ikinci "değişmemiş"
    yapısı.

    ## Kubbe zinciri

    Osmanlı zinciri (ADR 0036) burada **geçerlidir** — Ayasofya'nın
    aksine (0,909 Bizans oranı, ayrı kit). Kilit 43,00 m'den türeyen
    saçak kotu **17,22 m**; plandan bağımsız okunan kemer katı **30 m**
    ile türetilen kemer kilidi **28,97 m** arasında 1,03 m fark var.
    Zincir üçüncü kez bağımsız olarak doğrulandı.
    """

    def __init__(self, dome_d=SA_DOME_D, crown_z=SA_CROWN_Z,
                 half_domes=SA_HALF_DOMES, half_d=SA_HALF_D,
                 minarets=SA_MINARETS, hall_w=SA_HARIM_W, hall_d=SA_HARIM_D,
                 court_w=SA_COURT_W, court_d=SA_COURT_D,
                 court_bays=SA_COURT_BAYS, court_cols=SA_COURT_COLS,
                 palette="default"):
        self.dome_d, self.crown_z = dome_d, crown_z
        self.half_domes, self.half_d = half_domes, half_d
        self.minarets = tuple(minarets)
        self.hall_w, self.hall_d = hall_w, hall_d
        self.court_w, self.court_d = court_w, court_d
        self.court_bays, self.court_cols = court_bays, court_cols
        self.palette = palette

    @property
    def r(self):
        return self.dome_d * 0.5

    @property
    def half_r(self):
        return self.half_d * 0.5

    @property
    def dome_rise(self):
        return self.r * DOME_RISE_RATIO

    @property
    def spring_z(self):
        return self.crown_z - self.dome_rise

    @property
    def arch_crown_z(self):
        return self.spring_z - self.r * (math.sqrt(2.0) - 1.0)

    @property
    def arch_z(self):
        """Saçak = kemer eteği = yarım kubbelerin doğduğu kot."""
        return self.arch_crown_z - self.r

    @property
    def sherefe_total(self):
        return sum(m[2] for m in self.minarets)

    def validate(self):
        if abs(self.dome_d - SA_DOME_D) > 0.01:
            raise ValueError(f"kubbe capi {self.dome_d} — aciklik {SA_DOME_D} m")
        if self.half_domes != 4:
            raise ValueError(f"half_domes={self.half_domes} — Sultanahmet "
                             "DORT yarim kubbelidir (dort yonde birer)")
        if len(self.minarets) != 6:
            raise ValueError("ALTI minare")
        if self.sherefe_total != 16:
            raise ValueError(f"serefe {self.sherefe_total} — ON ALTI olmali "
                             "(4x3 harim + 2x2 avlu)")
        # Kapali revak halkasinda goz sayisi = mesnet sayisi. Dort kose
        # ayakla tasinir ve kaynagin sutun sayimina girmez; geriye kalan
        # farkin DORT olmasi bu iki sayinin ayni geometriyi tarif ettiginin
        # kanitidir. Baska bir yapiya bu kiti tasirsan bu bagi da tasirsin.
        if self.court_bays - self.court_cols != 4:
            raise ValueError(
                f"avlu {self.court_bays} goz / {self.court_cols} sutun — "
                "kapali halkada fark tam DORT (kose ayaklari) olmali")
        boy = sorted(set(m[3] for m in self.minarets))
        if len(boy) != 2:
            raise ValueError(f"minare boylari {boy} — harim minareleri UZUN, "
                             "avlu minareleri KISA; ikisi ayni olamaz")
        return self


def build_sultanahmet(p, col, asset_name, textured=False):
    """Sultan Ahmed Camii (1616). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D = p.hall_w, p.hall_d

    # --- Set ------------------------------------------------------------
    podium_h = 2.0
    y_south = -(p.court_d + D * 0.5 + 4.0)
    parts.append(hz.assign(hz.make_box(f"Set_{asset_name}",
                                       (max(W, p.court_w) + 8.0,
                                        D + p.court_d + 10.0, podium_h),
                                       (0.0, (D * 0.5 + 4.0 + y_south) * 0.5,
                                        podium_h * 0.5), col),
                           mats["cutstone"]))
    z0 = podium_h

    # --- Harim: DORT CEPHE, GERCEK KEMERLI PENCERELERLE -------------------
    wall_h = p.arch_z
    kat = wall_h / 3.0
    kabuk, built_h = dk.kabuk(mats, col, f"Harim_{asset_name}", W, D,
                              2.0, z0, ((10, 1.8, kat), (10, 1.6, kat),
                                        (8, 1.4, kat)))
    parts += kabuk
    for o in dk.silme(f"Sacak_{asset_name}", W, D, z0 + built_h, col,
                      steps=3, h=0.9, out=0.55):
        parts.append(hz.assign(o, mats["cutstone"]))
    # HARIM TACKAPISI (Sultanahmet). Not: Suleymaniye ile AYNI silme
    # degerlerini paylasiyor; metne gore ekleme yapinca ilk eslesme
    # Suleymaniye'ye gitti ve bu cami sessizce atlandi. Cevreyle ayirt et.
    parts += dk.tackapi(mats, col, f"HarimTackapi_{asset_name}",
                        0.0, -D * 0.5, z0, 11.0, wall_h * 0.72, 2.2,
                        kapi_w=3.0, kapi_h=4.6)

    parts.append(hz.assign(hz.make_box(f"HarimOrtu_{asset_name}",
                                       (W + 1.2, D + 1.2, 0.6),
                                       (0.0, 0.0, z0 + wall_h + 0.3), col),
                           mats["lead"]))

    # --- Kemer kati (fil ayaklari + tympanumlar) -------------------------
    #
    # Ayak duvarlarinin ekseni plandan 30,9 m; kubbe acikligi bu eksenler
    # arasindan duvar kalinligi cikarilarak turedi (23,45 = 23,5).
    pier_ax = 30.9
    tymp_h = p.arch_crown_z - p.arch_z
    parts.append(hz.assign(hz.make_box(f"KemerKati_{asset_name}",
                                       (pier_ax, pier_ax, tymp_h),
                                       (0.0, 0.0, z0 + p.arch_z + tymp_h * 0.5),
                                       col), mats["cutstone"]))
    for sgn in (-1, 1):
        for i in range(5):
            u = -pier_ax * 0.5 + pier_ax * (i + 0.5) / 5.0
            for ax in (0, 1):
                pos = ((sgn * (pier_ax * 0.5 - 0.3), u) if ax == 0
                       else (u, sgn * (pier_ax * 0.5 - 0.3)))
                size = ((0.7, 1.8, tymp_h * 0.34) if ax == 0
                        else (1.8, 0.7, tymp_h * 0.34))
                parts.append(hz.assign(
                    hz.make_box(f"Tympanum_{ax}{sgn}_{i}", size,
                                (pos[0], pos[1],
                                 z0 + p.arch_z + tymp_h * 0.44), col),
                    mats["shadow"]))

    # --- DORT YARIM KUBBE + ON IKI EKSEDRA -------------------------------
    #
    # Eksedralar BURADA mesh'e girer. Ayasofya'da girmemislerdi cunku orada
    # ic mekan ogesidirler (ADR 0045); Sultanahmet'te yarim kubbelerin
    # eteginden disa tasarlar ve siluetin basamakli kaskadini onlar yapar.
    hr = p.half_r
    halfs = ((0.0, +14.0, math.pi * 0.5), (0.0, -14.2, -math.pi * 0.5),
             (+16.4, 0.0, 0.0), (-16.4, 0.0, math.pi))
    exedra_n = 0
    for i, (cx, cy, facing) in enumerate(halfs):
        # YARIM KUBBE YARIM KUREDIR, BASIK DEGIL — ve bu bir uslup tercihi
        # degil, GEOMETRIK ZORUNLULUK.
        #
        # Ilk kurulumda ana kubbenin 0,78 basikligi yarim kubbelere de
        # uygulanmisti; kilitleri 25,64 m'de kaliyordu, kemer katinin tepesi
        # ise 28,97 m — yani dortu de bloğun ARKASINA gomulmustu ve renderda
        # kubbenin dibinde kabarcik gibi okunuyorlardi.
        #
        # Plan ikisinin AYNI kotta oldugunu soyluyor (yarim kubbe izi h=39,
        # kemer duvarlarinin tepesi de h=39). Sebebi de acik: her yarim kubbe
        # dort buyuk kemerden BIRININ uzerine oturur, yani kilidi o kemerin
        # kilididir. Yaricapi 10,8 m'lik bir yarim kure 17,22'den doğup
        # 28,02'ye cikar — kemer kilidi 28,97. Bulusuyorlar.
        parts.append(hz.assign(
            hz.make_half_dome(f"YarimKubbe_{i}", hr, hr,
                              (cx, cy), z0 + p.arch_z, facing=facing,
                              segments=20, rings=6, col=col), mats["lead"]))
        for o in dk.kubbe_kaburga(f"YarimDikis_{i}", cx, cy, hr,
                                  z0 + p.arch_z, hr, col, n=14,
                                  a0=facing - math.pi * 0.5,
                                  a1=facing + math.pi * 0.5):
            parts.append(hz.assign(o, mats["lead"]))
        er = hr * 0.42
        for k in range(SA_EXEDRAE_PER_HALF):
            ang = facing + (k - 1) * math.radians(58.0)
            ex = cx + math.cos(ang) * hr * 0.82
            ey = cy + math.sin(ang) * hr * 0.82
            # Eksedra da yarim kure ve SACAK kotundan doğar: harim catisi
            # 17,22'de, eksedra kilidi 21,8'de — yani catinin USTUNE cikar.
            # Onceki kurulumda 3,2 m alcaktan basliyordu ve kilidi catiyla
            # ayni kota dusuyordu; on iki eksedra siluete hic katilmiyordu.
            parts.append(hz.assign(
                hz.make_half_dome(f"Eksedra_{i}{k}", er, er,
                                  (ex, ey), z0 + p.arch_z, facing=ang,
                                  segments=12, rings=4, col=col),
                mats["lead"]))
            exedra_n += 1

    # --- Kose agirlik kuleleri (kubbe karesinin dort kosesi) --------------
    #
    # Plandan: 4,0 x 4,0 m, eksenden +-15,5 m. Sultanahmet'in siluetinde
    # kubbenin cevresini saran dort ince kule bunlardir; atlaninca kubbe
    # cıplak bir yarim kure gibi okunur.
    turret_top = p.spring_z - 2.0
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(hz.assign(
                hz.make_box(f"AgirlikKule_{sx}{sy}", (4.0, 4.0,
                                                      turret_top - p.arch_z),
                            (sx * 15.5, sy * 15.4,
                             z0 + (p.arch_z + turret_top) * 0.5), col),
                mats["cutstone"]))
            parts.append(hz.assign(
                hz.make_tube(f"AgirlikKulah_{sx}{sy}", 2.6, 0.0, 3.4,
                             (sx * 15.5, sy * 15.4), z0 + turret_top,
                             segments=8, col=col), mats["lead"]))

    # --- Kasnak + ana kubbe ----------------------------------------------
    drum_h = p.spring_z - p.arch_crown_z
    parts.append(hz.assign(hz.make_tube(f"Kasnak_{asset_name}", p.r * 1.18,
                                        p.r * 1.14, drum_h, (0.0, 0.0),
                                        z0 + p.arch_crown_z, segments=24,
                                        cap_top=False, col=col),
                           mats["cutstone"]))
    dome = hz.make_dome(f"Kubbe_{asset_name}", p.r, p.dome_rise, (0.0, 0.0),
                        z0 + p.spring_z, segments=28, rings=8, col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    # KUBBE, BIRLESMEDEN ONCE OLCULUR (Galata dersi, ADR 0033).
    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    if abs(measured_d - p.dome_d) > 0.08:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} m — {p.dome_d:.2f}")

    for o in dk.kubbe_kaburga(f"KubbeDikis_{asset_name}", 0.0, 0.0, p.r,
                              z0 + p.spring_z, p.dome_rise, col, n=28):
        parts.append(hz.assign(o, mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z0 + p.crown_z, col,
                     scale=1.5):
        parts.append(hz.assign(o, mats["lead"]))

    # --- REVAKLI AVLU ----------------------------------------------------
    #
    # Kisa iki minare avlunun DIS koselerindedir (Suleymaniye dersi,
    # ADR 0044): avlu olmadan o ikisi boslukta durur.
    ay0 = -D * 0.5
    ay1 = ay0 - p.court_d
    _rev_h = 5.7
    avlu_h = dk.revak_ust(SA_COURT_D, 7, _rev_h) + 0.6
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"AvluDuvar_{sx}", (1.8, p.court_d, avlu_h),
                        (sx * (p.court_w * 0.5 - 0.9), (ay0 + ay1) * 0.5,
                         z0 + avlu_h * 0.5), col), mats["cutstone"]))
    parts.append(hz.assign(
        hz.make_box(f"AvluDuvar_On", (p.court_w, 1.8, avlu_h),
                    (0.0, ay1 + 0.9, z0 + avlu_h * 0.5), col),
        mats["cutstone"]))
    # KAPALI REVAK HALKASI: otuz goz, yirmi alti sutun, dort kose ayagi.
    #
    # Onceki hali cevreyi dolasan duz kubbelerdi ve yorumunda "uzaktan
    # okunan sey kubbelerin ritmidir, sutunlar degil" yaziyordu. Sutunlar
    # bulunamayinca sayilari da denetlenemiyordu. Kapali halkada goz
    # sayisi mesnet sayisina esittir (30); dordu kose ayagi olunca geriye
    # TDV'nin verdigi 26 sutun kalir. Iki sayi ayni geometriyi tarif
    # ediyormus.
    yan_n, on_n = dk.gozleri_dagit(p.court_bays,
                                   (p.court_d, p.court_d,
                                    p.court_w, p.court_w))[:3:2]
    side_n = yan_n
    ofs = 5.4
    rev_h = _rev_h
    for sx in (-1, 1):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_{sx}",
            sx * (p.court_w * 0.5 - ofs), ay1 + ofs,
            sx * (p.court_w * 0.5 - ofs), ay0 - ofs,
            side_n, z0, rev_h, 0.42,
            bay=ofs, bay_dir=(sx, 0.0), ends=(False, False))
    # Goz DUVARA acilir: ay1'deki sirada duvar -Y'de, ay0'daki
    # sirada harim +Y'de. Ilk yazimda isaretler tersti ve on
    # revagin gozu duvara degil avluya bakiyordu.
    for sy, yy in ((-1.0, ay1 + ofs), (1.0, ay0 - ofs)):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_Y{int(sy)}",
            -p.court_w * 0.5 + ofs, yy, p.court_w * 0.5 - ofs, yy,
            on_n, z0, rev_h, 0.42,
            bay=ofs, bay_dir=(0.0, sy), ends=(False, False))
    for sx in (-1, 1):
        for yy in (ay1 + ofs, ay0 - ofs):
            parts += dk.kose_ayagi(
                mats, col, f"AvluKose_{sx}_{int(yy)}",
                sx * (p.court_w * 0.5 - ofs), yy, z0, rev_h, 0.42)
    # SAYIM DENETIMI: "yirmi alti sutun, otuz kubbeli birim" meshte mi?
    # Katalogda yazmak yetmez; sayi geometride durmuyorsa yoktur.
    sut = set(re.findall(r"(AvluRevak_[^_]+_Sutun\d+)_", "|".join(
        o.name for o in parts)))
    kub = set(re.findall(r"(AvluRevak_[^_]+_Kubbe\d+)", "|".join(
        o.name for o in parts)))
    if len(sut) != p.court_cols:
        raise ValueError(f"avlu sutun sayisi {len(sut)} != {p.court_cols}")
    if len(kub) != p.court_bays:
        raise ValueError(f"avlu goz sayisi {len(kub)} != {p.court_bays}")

    # Sadirvan: ALTIGEN planli, kubbeli (TDV). Plandan Y = -49.
    sad_y = -48.9
    parts.append(hz.assign(
        hz.make_tube(f"Sadirvan_{asset_name}", 3.4, 3.2, 2.6, (0.0, sad_y),
                     z0, segments=6, col=col), mats["marble"]))
    parts.append(hz.assign(
        hz.make_tube(f"SadirvanKubbe_{asset_name}", 3.6, 0.4, 2.0,
                     (0.0, sad_y), z0 + 2.6, segments=6, col=col),
        mats["lead"]))

    # --- ALTI MINARE, ON ALTI SEREFE -------------------------------------
    for i, (mx_, my_, ser, boy) in enumerate(p.minarets):
        parts += _minaret_multi(p, mats, col, f"Minare{i}", mx_, my_, z0,
                                ser, boy - 13.0)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Harim", (W, D, wall_h),
                                    (0.0, 0.0, z0 + wall_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_box("L1_KemerKati", (pier_ax, pier_ax, tymp_h),
                                    (0.0, 0.0,
                                     z0 + p.arch_z + tymp_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", p.r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.spring_z, segments=14, rings=5,
                                     col=col), mats["lead"]))
    for i, (cx, cy, facing) in enumerate(halfs):
        l1.append(hz.assign(
            hz.make_half_dome(f"L1_Yarim{i}", hr, hr,
                              (cx, cy), z0 + p.arch_z, facing=facing,
                              segments=10, rings=3, col=col), mats["lead"]))
    for i, (mx_, my_, ser, boy) in enumerate(p.minarets):
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{i}", 1.35, 1.0, boy, (mx_, my_), z0,
                         segments=8, col=col), mats["cutstone"]))

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
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D2", dome_d=round(p.dome_d, 2),
                dome_d_in=SA_DOME_D_IN,
                dome_crown_z=round(p.crown_z, 2),
                measured_dome_d=round(measured_d, 3),
                half_domes=p.half_domes, half_dome_d=p.half_d,
                exedrae=exedra_n, piers=SA_PIERS, pier_d=SA_PIER_D,
                minarets=len(p.minarets), sherefe_total=p.sherefe_total,
                sherefe_each=[m[2] for m in p.minarets],
                minaret_h_tall=max(m[3] for m in p.minarets),
                minaret_h_short=min(m[3] for m in p.minarets),
                portico_bays=p.court_bays,
                court_columns=p.court_cols, double_portico=False,
                podium_h=podium_h, harim_w=W, harim_d=D,
                minaret_top=round(mx[2] - mn[2], 2))
    return lod0, lod1, ucx, info


# ================================== Fâtih Camii, **1766 öncesi özgün şema**

#: Ana kubbe çapı (m). **Yüz yıl boyunca İstanbul'un en büyük kubbesi**
#: kaldı — 1470'ten Süleymaniye'ye (1557, 26,5 m) kadar. Rekor iddiası
#: sayıyı da doğruluyor: 87 yıl, "bir yüzyıl" tarifine oturuyor.
#:
#: 1767-71 yeniden inşası bu çapı korudu, gerisini korumadı.
FT_DOME_D = 26.00

#: Kubbe kilidi (m) — **TÜRETİLDİ, D3**.
#:
#: Özgün yapının kilit kotu hiçbir kaynakta yok; yapı 1766'da zemine kadar
#: yıktırıldı. Sayı şöyle bağlandı: yan neflerin **üçer** küçük kubbesi
#: (~8,7 m açıklık) saçağın **altında** kalmak zorunda, bu da saçağı
#: ~22 m'ye koyuyor; Osmanlı zinciri (ADR 0036) oradan **50,5 m** veriyor.
#:
#: Yani sayı uydurulmadı, **sayılan bir değerden** (üçer kubbe) türedi —
#: ama yine de D3'tür ve öyle işaretlidir.
FT_CROWN_Z = 50.50

#: **BİR** yarım kubbe, **mihrap** yönünde.
#:
#: TDV: *"İlk Fâtih Camii'nin ortada bir büyük kubbesiyle mihrap tarafında
#: BİR yarım kubbesi ve yanlarda daha alçak ÜÇER küçük kubbeli bölümleri
#: bulunduğu eski resimlerinden anlaşılmaktadır."*
#:
#: Bugün görülen **dört** yarım kubbeli barok şema **1767-71**'dir. Yarım
#: kubbe sayısı bu projede planı tanımlayan değerdir (Üsküdar Mihrimah üç,
#: Süleymaniye ve Ayasofya iki, Sultanahmet dört) — ve Fâtih **bir**.
FT_HALF_DOMES = 1

#: Yan neflerde **üçer** küçük kubbe (toplam altı) — sayılan.
FT_SIDE_DOMES_PER_SIDE = 3

#: Kubbeyi taşıyan ayak: **İKİ**.
#:
#: Vikipedi: *"cami alanını genişletmek için duvarlar ve İKİ AYAK üzerine
#: bir kubbe oturtulmuş ve bunun da önüne bir yarım kubbe ilave
#: edilmiştir."* Bugünkü yapıda **dört** fil ayağı var. Sayı, şemanın
#: kendisidir: kubbe iki ayak ve iki duvara oturunca plan **uzunlamasına**
#: olur, merkezî olmaz.
FT_PIERS = 2

#: **İki** minare, her biri **BİRER** şerefeli — ve konumları belgeli:
#: cümle kapısı duvarının köşelerine bitişik. Bugünkü minareler **ikişer**
#: şerefelidir; kaide, pabuç ve gövdelerin başlangıcı ilk yapıdan kalmadır.
FT_MINARETS, FT_SEREFE_EACH = 2, 1

#: Harim: ölçülü değil, **sayılan değerden türedi**. Kubbe karesi 26 m;
#: yanlarda üçer kubbe → her nef ~8,7 m; genişlik 26 + 2×8,7 = **43,4 m**.
#: Derinlik = kubbe karesi + yarım kubbe yarıçapı ≈ **39,0 m**. Kaynak
#: "kareye yakın plânlı" der ve 43,4 × 39,0 kareye yakındır — türetme
#: kendi kendini denetliyor.
FT_HARIM_W, FT_HARIM_D = 43.4, 39.0

#: Avlu — **ilk yapıdan kalmadır** ve sayıları o yüzden 1632'yi bağlar:
#: *"Etrafı ON SEKİZ sütun üzerinde YİRMİ İKİ kubbeli revakla çevrilidir"*;
#: üç kapı (ikisi yanlarda); ortasında **şadırvan**. Şadırvan, avlunun üç
#: duvarı, taçkapı ve mihrap 1766'yı atlatan parçalardır.
FT_COURT_COLS, FT_COURT_DOMES, FT_COURT_GATES = 18, 22, 3
FT_COURT_W, FT_COURT_D = 43.4, 38.0


class FatihParams(object):
    """
    Fâtih Camii, **1463-1470**, mimar Atik Sinan — 1632'de 162 yaşında.

    ## Bugün gördüğün yapı 1632'de YOK

    Faz 3'ün en büyük farkı burada. **1766 depremi** camiyi yıktı ve
    *"caminin geri kalan kısmı zemine kadar yıktırıldı"*; bugünkü barok
    yapı **1767-71**, mimar Mehmed Tahir Ağa'dır.

    1632'de ayakta olan şema başkadır:

    * ortada **bir** büyük kubbe (26 m), **iki ayak** ve duvarlar üzerinde,
    * mihrap yönünde **bir** yarım kubbe — bugünkü **dört** değil,
    * yanlarda daha alçak **üçer** küçük kubbeli bölümler,
    * **iki** minare, her biri **birer** şerefeli — bugün ikişer.

    Yani plan bugünkü gibi merkezî değil **uzunlamasına**dır ve dışarıdan
    Edirne Üç Şerefeli'ye benzeyen erken klasik bir kütledir.

    ## 1632'de ayakta olan gerçek parçalar

    İlk yapıdan bugüne kalanlar — yani 1632'de kesinlikle var olanlar —
    şunlardır: **şadırvan avlusunun üç duvarı**, **avlunun ortasındaki
    şadırvan**, **taçkapı**, **mihrap**, ve **minarelerin şerefe altına
    kadar kaide, pabuç ve gövdeleri**.

    Bu liste modelin omurgasıdır: avlunun sayıları (18 sütun, 22 kubbe,
    3 kapı) 15. yüzyıla aittir ve doğrudan 1632'yi bağlar.

    ## Doğruluk

    Kubbe çapı **D2**; şema ve sayılar **D3** (TDV bunları "eski
    resimlerinden anlaşılmaktadır" diye verir — çizim değil, tasvir);
    kilit kotu **D3** ve türetilmiştir.
    """

    def __init__(self, dome_d=FT_DOME_D, crown_z=FT_CROWN_Z,
                 half_domes=FT_HALF_DOMES,
                 side_domes=FT_SIDE_DOMES_PER_SIDE, piers=FT_PIERS,
                 minarets=FT_MINARETS, sherefe_each=FT_SEREFE_EACH,
                 hall_w=FT_HARIM_W, hall_d=FT_HARIM_D,
                 court_w=FT_COURT_W, court_d=FT_COURT_D,
                 court_domes=FT_COURT_DOMES, palette="default"):
        self.dome_d, self.crown_z = dome_d, crown_z
        self.half_domes, self.side_domes = half_domes, side_domes
        self.piers = piers
        self.minarets, self.sherefe_each = minarets, sherefe_each
        self.hall_w, self.hall_d = hall_w, hall_d
        self.court_w, self.court_d = court_w, court_d
        self.court_domes = court_domes
        self.palette = palette

    @property
    def r(self):
        return self.dome_d * 0.5

    @property
    def dome_rise(self):
        return self.r * DOME_RISE_RATIO

    @property
    def spring_z(self):
        return self.crown_z - self.dome_rise

    @property
    def arch_crown_z(self):
        return self.spring_z - self.r * (math.sqrt(2.0) - 1.0)

    @property
    def arch_z(self):
        return self.arch_crown_z - self.r

    @property
    def aisle_w(self):
        """Yan nef genişliği = küçük kubbe açıklığı."""
        return (self.hall_w - self.dome_d) * 0.5

    def validate(self):
        if abs(self.dome_d - FT_DOME_D) > 0.01:
            raise ValueError(f"kubbe capi {self.dome_d} — olculen {FT_DOME_D}")
        if self.half_domes != 1:
            raise ValueError(
                f"half_domes={self.half_domes} — 1632'de Fatih Camii'nin "
                "MIHRAP yonunde BIR yarim kubbesi vardir. Bugunku DORT "
                "yarim kubbeli barok sema 1767-71'dir (ADR 0048).")
        if self.piers != 2:
            raise ValueError(
                f"piers={self.piers} — ozgun kubbe IKI ayak ve duvarlar "
                "uzerindeydi; bugunku dort fil ayagi 1767-71'dir.")
        if self.side_domes != 3:
            raise ValueError(f"side_domes={self.side_domes} — yanlarda "
                             "UCER kucuk kubbe (sayilan).")
        if self.sherefe_each != 1:
            raise ValueError(
                f"serefe {self.sherefe_each} — ozgun minareler BIRER "
                "serefeliydi; bugunku ikiser serefe sonradandir.")
        # Sacak, yan neflerin kucuk kubbelerini GECMEK zorunda: kilit oradan
        # turedi ve ters yonde de tutmali.
        # Yan nef catisi 14 m, kucuk kubbe yaricapi ~0,46*nef; kilit
        # oradan cikar ve SACAGIN altinda kalmali.
        small_crown = 14.5 + self.aisle_w * 0.46 * DOME_RISE_RATIO
        if self.arch_z < small_crown:
            raise ValueError(
                f"sacak {self.arch_z:.1f} m, yan kubbelerin kilidi "
                f"~{small_crown:.1f} m — sacak onlarin ALTINDA kalamaz.")
        return self


def build_fatih(p, col, asset_name, textured=False):
    """Fâtih Camii, **1632 hâli** (1766 öncesi). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D = p.hall_w, p.hall_d

    podium_h = 1.6
    ay0 = -D * 0.5
    ay1 = ay0 - p.court_d
    parts.append(hz.assign(hz.make_box(f"Set_{asset_name}",
                                       (max(W, p.court_w) + 7.0,
                                        D + p.court_d + 8.0, podium_h),
                                       (0.0, (D * 0.5 + 4.0 + ay1 - 4.0) * 0.5,
                                        podium_h * 0.5), col),
                           mats["cutstone"]))
    z0 = podium_h

    # --- Harim: YAN BOLUMLER **DAHA ALCAK** ------------------------------
    #
    # Kaynagin sozu bu: "yanlarda **daha alcak** ucer kucuk kubbeli
    # bolumleri". Ilk kurulumda yanlar da orta kutle kadar yuksekti ve
    # ucer kubbe catida 1,3 m'lik kabarciklara donuyordu — sayi meshte
    # vardi ama SILUETTE yoktu.
    #
    # Iki kademe: yan nefler 14 m, orta kutle sacaga (21,98) kadar. Ozgun
    # semayi bugunkunden ayiran sey tam olarak bu basamaktir; barok yapi
    # yanlari yarim kubbelerle cozup kutleyi tek parca yapar.
    wall_h = p.arch_z
    aw = p.aisle_w
    aisle_h = 14.0
    parts.append(hz.assign(hz.make_box(f"YanNefler_{asset_name}",
                                       (W, D, aisle_h),
                                       (0.0, 0.0, z0 + aisle_h * 0.5), col),
                           mats["cutstone"]))
    kabuk_f, _ = dk.kabuk(mats, col, f"OrtaKutle_{asset_name}",
                          p.dome_d + 2.2, D, 1.6, z0 + aisle_h,
                          ((5, 1.6, (wall_h - aisle_h) * 0.5),
                           (5, 1.4, (wall_h - aisle_h) * 0.5)))
    parts += kabuk_f
    parts.append(hz.assign(hz.make_box(f"OrtaAlt_{asset_name}",
                                       (p.dome_d + 2.2, D, aisle_h),
                                       (0.0, 0.0, z0 + aisle_h * 0.5), col),
                           mats["cutstone"]))
    for sgn in (-1, 1):
        for i in range(6):
            u = -D * 0.5 + D * (i + 0.5) / 6.0
            parts.append(hz.assign(
                hz.make_box(f"YanPencere_{sgn}_{i}", (0.7, 2.0, aisle_h * 0.22),
                            (sgn * (W * 0.5 - 0.3), u, z0 + aisle_h * 0.40),
                            col), mats["shadow"]))
        for i in range(5):
            u = -D * 0.5 + D * (i + 0.5) / 5.0
            parts.append(hz.assign(
                hz.make_box(f"OrtaPencere_{sgn}_{i}",
                            (0.7, 1.8, (wall_h - aisle_h) * 0.34),
                            (sgn * ((p.dome_d + 2.2) * 0.5 - 0.3), u,
                             z0 + aisle_h + (wall_h - aisle_h) * 0.48), col),
                mats["shadow"]))
    parts.append(hz.assign(hz.make_box(f"NefOrtu_{asset_name}",
                                       (W + 1.0, D + 1.0, 0.5),
                                       (0.0, 0.0, z0 + aisle_h + 0.25), col),
                           mats["lead"]))

    # --- YAN NEFLER: UCER KUCUK KUBBE ------------------------------------
    #
    # Sayilan deger. Bugunku yapida yoklar — barok sema yan nefleri
    # yarim kubbelerle cozer. Ucer kubbe, plani UZUNLAMASINA yapan seydir.
    small_r = aw * 0.46
    for sx in (-1, 1):
        for i in range(p.side_domes):
            cy = -D * 0.5 + D * (i + 0.5) / p.side_domes
            parts.append(hz.assign(
                hz.make_dome(f"YanKubbe_{sx}{i}", small_r,
                             small_r * DOME_RISE_RATIO,
                             (sx * (p.r + aw * 0.5), cy), z0 + aisle_h + 0.5,
                             segments=14, rings=5, col=col), mats["lead"]))

    # --- Kemer kati ------------------------------------------------------
    tymp = p.arch_crown_z - p.arch_z
    parts.append(hz.assign(hz.make_box(f"KemerKati_{asset_name}",
                                       (p.dome_d + 2.2, p.dome_d + 2.2, tymp),
                                       (0.0, 0.0, z0 + p.arch_z + tymp * 0.5),
                                       col), mats["cutstone"]))
    for sgn in (-1, 1):
        for i in range(5):
            u = -(p.dome_d + 2.2) * 0.5 + (p.dome_d + 2.2) * (i + 0.5) / 5.0
            parts.append(hz.assign(
                hz.make_box(f"Tympanum_{sgn}_{i}", (0.7, 1.7, tymp * 0.32),
                            (sgn * ((p.dome_d + 2.2) * 0.5 - 0.3), u,
                             z0 + p.arch_z + tymp * 0.44), col),
                mats["shadow"]))

    # --- BIR YARIM KUBBE, MIHRAP YONUNDE (+Y) ----------------------------
    #
    # Sultanahmet dersi (ADR 0047): yarim kubbe kendi buyuk kemerinin
    # uzerine oturur, yani YARIM KUREDIR ve kilidi kemerin kilididir.
    parts.append(hz.assign(
        hz.make_half_dome(f"YarimKubbe_{asset_name}", p.r, p.r,
                          (0.0, p.r * 0.92), z0 + p.arch_z,
                          facing=math.pi * 0.5, segments=22, rings=6,
                          col=col), mats["lead"]))

    # --- Kasnak + ana kubbe ----------------------------------------------
    drum_h = p.spring_z - p.arch_crown_z
    parts.append(hz.assign(hz.make_tube(f"Kasnak_{asset_name}", p.r * 1.12,
                                        p.r * 1.08, drum_h, (0.0, 0.0),
                                        z0 + p.arch_crown_z, segments=22,
                                        cap_top=False, col=col),
                           mats["cutstone"]))
    dome = hz.make_dome(f"Kubbe_{asset_name}", p.r, p.dome_rise, (0.0, 0.0),
                        z0 + p.spring_z, segments=28, rings=8, col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    if abs(measured_d - p.dome_d) > 0.08:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} — {p.dome_d:.2f}")

    for o in dk.kubbe_kaburga(f"KubbeDikis_{asset_name}", 0.0, 0.0, p.r,
                              z0 + p.spring_z, p.dome_rise, col, n=26):
        parts.append(hz.assign(o, mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z0 + p.crown_z, col,
                     scale=1.5):
        parts.append(hz.assign(o, mats["lead"]))

    # --- TACKAPI: 1766'yi atlatan parcalardan biri -----------------------
    # Onceki hali iki kutuydu: bir levha ve uzerinde koyu bir dikdortgen.
    # Osmanli camisini uzaktan tanitan uc seyden biri tackapidir; onu iki
    # kutuyla gecmek camiyi kapisindan taninmaz kiliyordu.
    parts += dk.tackapi(mats, col, f"Tackapi_{asset_name}",
                        0.0, ay0, z0, 9.6, wall_h * 0.86, 2.2,
                        kapi_w=3.0, kapi_h=4.6)

    # --- REVAKLI AVLU: ON SEKIZ SUTUN, YIRMI IKI KUBBE -------------------
    #
    # Avlunun UC DUVARI, SADIRVANI ve TACKAPI ilk yapidan kalmadir — yani
    # bu sayilar dogrudan 1632'yi baglar; harim gibi turetilmis degil.
    _rev_h = 5.2
    avlu_h = dk.revak_ust(p.court_d, 6, _rev_h) + 0.6
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"AvluDuvar_{sx}", (1.6, p.court_d, avlu_h),
                        (sx * (p.court_w * 0.5 - 0.8), (ay0 + ay1) * 0.5,
                         z0 + avlu_h * 0.5), col), mats["cutstone"]))
    parts.append(hz.assign(
        hz.make_box("AvluDuvar_On", (p.court_w, 1.6, avlu_h),
                    (0.0, ay1 + 0.8, z0 + avlu_h * 0.5), col),
        mats["cutstone"]))
    # UC KAPI: ikisi yanlarda, biri karsida.
    for gx, gy, gw, gd in ((0.0, ay1 + 0.8, 3.0, 0.9),
                           (-(p.court_w * 0.5 - 0.8), (ay0 + ay1) * 0.5, 0.9, 3.0),
                           (+(p.court_w * 0.5 - 0.8), (ay0 + ay1) * 0.5, 0.9, 3.0)):
        parts.append(hz.assign(
            hz.make_box(f"AvluKapi_{gx:.0f}_{gy:.0f}", (gw, gd, 4.6),
                        (gx, gy, z0 + 2.3), col), mats["shadow"]))
    # KAPALI REVAK HALKASI: yirmi iki goz, on sekiz sutun, dort kose ayagi.
    # Fatih'in avlusu 1471'den AYAKTA; iki sayi da olculmustur ve farklari
    # yine tam DORT. Sultanahmet'te bulunan bag burada bagimsiz olarak
    # dogrulaniyor: kapali halkada goz = mesnet, dordu kose ayagi.
    f_yan, f_on = dk.gozleri_dagit(p.court_domes,
                                   (p.court_d, p.court_d,
                                    p.court_w, p.court_w))[:3:2]
    ndome, ofs, rev_h = 0, 4.6, _rev_h
    for sx in (-1, 1):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_{sx}",
            sx * (p.court_w * 0.5 - ofs), ay1 + ofs,
            sx * (p.court_w * 0.5 - ofs), ay0 - ofs,
            f_yan, z0, rev_h, 0.40,
            bay=ofs, bay_dir=(sx, 0.0), ends=(False, False))
        ndome += f_yan
    # Goz DUVARA acilir: ay1'deki sirada duvar -Y'de, ay0'daki
    # sirada harim +Y'de. Ilk yazimda isaretler tersti ve on
    # revagin gozu duvara degil avluya bakiyordu.
    for sy, yy in ((-1.0, ay1 + ofs), (1.0, ay0 - ofs)):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_Y{int(sy)}",
            -p.court_w * 0.5 + ofs, yy, p.court_w * 0.5 - ofs, yy,
            f_on, z0, rev_h, 0.40,
            bay=ofs, bay_dir=(0.0, sy), ends=(False, False))
        ndome += f_on
    for sx in (-1, 1):
        for yy in (ay1 + ofs, ay0 - ofs):
            parts += dk.kose_ayagi(mats, col, f"AvluKose_{sx}_{int(yy)}",
                                   sx * (p.court_w * 0.5 - ofs), yy,
                                   z0, rev_h, 0.40)
    if ndome != p.court_domes:
        raise ValueError(f"avlu kubbesi {ndome} — kaynak {p.court_domes} der")
    nsut = len(set(re.findall(r"(AvluRevak_[^_]+_Sutun\d+)_",
                              "|".join(o.name for o in parts))))
    if nsut != FT_COURT_COLS:
        raise ValueError(f"avlu sutunu {nsut} — olculmus deger "
                         f"{FT_COURT_COLS} (1471'den ayakta)")
    # SADIRVAN: ilk yapidan kalmadir.
    parts.append(hz.assign(
        hz.make_tube(f"Sadirvan_{asset_name}", 3.0, 2.8, 2.4,
                     (0.0, (ay0 + ay1) * 0.5), z0, segments=8, col=col),
        mats["marble"]))
    parts.append(hz.assign(
        hz.make_tube(f"SadirvanKulah_{asset_name}", 3.3, 0.3, 2.2,
                     (0.0, (ay0 + ay1) * 0.5), z0 + 2.4, segments=8, col=col),
        mats["lead"]))

    # --- IKI MINARE, BIRER SEREFE ----------------------------------------
    #
    # Konum BELGELI: cumle kapisi duvarinin koselerine bitisik. Kaide,
    # pabuc ve govdenin baslangici ilk yapidan kalmadir.
    for i, sx in enumerate((-1, 1)):
        parts += _minaret_multi(p, mats, col, f"Minare{i}",
                                sx * (W * 0.5 - 1.6), ay0 - 1.2, z0,
                                p.sherefe_each, p.crown_z * 0.86)

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_YanNefler", (W, D, aisle_h),
                                    (0.0, 0.0, z0 + aisle_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_box("L1_OrtaKutle", (p.dome_d + 2.2, D, wall_h),
                                    (0.0, 0.0, z0 + wall_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", p.r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.spring_z, segments=14, rings=5,
                                     col=col), mats["lead"]))
    l1.append(hz.assign(
        hz.make_half_dome("L1_Yarim", p.r, p.r, (0.0, p.r * 0.92),
                          z0 + p.arch_z, facing=math.pi * 0.5,
                          segments=10, rings=3, col=col), mats["lead"]))
    for i, sx in enumerate((-1, 1)):
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{i}", 1.35, 1.0, p.crown_z * 0.86 + 13.7,
                         (sx * (W * 0.5 - 1.6), ay0 - 1.2), z0, segments=8,
                         col=col), mats["cutstone"]))

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
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D3", dome_d=round(p.dome_d, 2),
                dome_crown_z=round(p.crown_z, 2),
                measured_dome_d=round(measured_d, 3),
                half_domes=p.half_domes, side_domes=p.side_domes,
                side_domes_total=p.side_domes * 2, piers=p.piers,
                minarets=p.minarets, sherefe_each=p.sherefe_each,
                sherefe_total=p.minarets * p.sherefe_each,
                portico_bays=p.court_domes, court_columns=FT_COURT_COLS,
                court_gates=FT_COURT_GATES, double_portico=False,
                podium_h=podium_h, harim_w=W, harim_d=D,
                aisle_h=aisle_h, wall_h=round(wall_h, 2),
                minaret_top=round(mx[2] - mn[2], 2))
    return lod0, lod1, ucx, info


# ================================================ Beyazıt Camii (1501-1506)

#: Ana kubbe çapı (m) — **ölçülü**.
BZ_DOME_D = 16.78

#: Harim iç ölçüsü (m) — **ölçülü**: 37,06 × 36,80. Kaynak "kare biçimli"
#: der ve ölçü onu doğruluyor (fark 26 cm).
BZ_HARIM_W, BZ_HARIM_D = 37.06, 36.80

#: Kubbe kilidi (m) — **TÜRETİLDİ, D3**. Yayımlanan bir kot yok.
#:
#: İki kısıtla bağlandı: (1) saçak iki katlı yan neflerin çatısını
#: geçmeli, (2) kilit/çap oranı öteki dört selâtin camisinde **ölçülen**
#: banda düşmeli — Ayasofya 1,68, Sultanahmet 1,83, Süleymaniye 2,00,
#: Üsküdar Mihrimah 2,12. Beyazıt'ta 35,00 / 16,78 = **2,09**.
BZ_CROWN_Z = 35.00

#: **İKİ** yarım kubbe, kıble ekseninde; **DÖRT** pâye.
BZ_HALF_DOMES, BZ_PIERS = 2, 4

#: Sayılan pencereler: ana kubbede **yirmi**, her yarım kubbede **yedişer**.
BZ_DOME_WINDOWS, BZ_HALF_WINDOWS = 20, 7

#: **Minareler camiye değil TABHÂNE kanatlarına bitişiktir** ve aralarında
#: **79 m** vardır. Bu ölçü yapının en tanınan sayısal özelliğidir ve
#: kütlenin toplam genişliğini **bağlar** — kanat uzunluğu ondan türer,
#: elle girilmez.
BZ_MINARET_SPAN = 79.0
BZ_MINARETS, BZ_SEREFE_EACH = 2, 1

#: Tabhâne: iki kanat, her birinde **dörder kubbeli hücre** (TDV).
#: Yaygın anlatım "beşer kubbe" der; ikisi aynı şeyi saymıyor olabilir
#: (hücre ≠ kubbe). TDV alındı ve çelişki kayda geçti.
BZ_TABHANE_CELLS = 4

#: Avlu: kare, **yirmi dört** kubbeli revak, mermer döşeli.
BZ_COURT_DOMES = 24


class BeyazitParams(object):
    """
    Beyazıt II Camii, **1501-1506** — 1632'de 126 yaşında.

    ## 1632'de bir şantiye var ve şantiyenin sahibi oyunun padişahı

    TDV: şadırvanın üstündeki **sekiz sütuna oturan kubbeyi IV. Murad**
    ekletmiştir, **1623-1640** arasında. Oyunun geçtiği yıl o aralığın
    **tam ortasıdır**.

    Model kubbeyi **koymaz**. Gerekçe tarihsel: Murad IV 1623'te on bir
    yaşında tahta çıktı ve gerçek iktidarı **1632'de** ele aldı; büyük
    hayrat işleri o tarihten sonra beklenir. Yani 1632'de şadırvanın
    kubbesi büyük olasılıkla **henüz yok** — ama bu bir olasılıktır,
    kesinlik değil, ve öyle işaretlidir.

    ## Ölçülen ve türetilen

    * kubbe **16,78 m**, harim **37,06 × 36,80 m** → ölçülü,
    * minareler arası **79 m** → ölçülü, ve kütlenin genişliğini bağlar,
    * kilit kotu → **türetildi (D3)**.

    ## 1509 ve 1573

    1509 depreminde kubbe *"dağılıp pâre pâre"* oldu; medrese yıkıldı.
    Sinan **1573-74**'te *"bir kemer-i cedîdle"* yapıyı takviye etti.
    Yani 1632'de ayakta olan şey iki yapısal müdahaleden geçmiştir —
    ama biçimi değişmemiştir.
    """

    def __init__(self, dome_d=BZ_DOME_D, crown_z=BZ_CROWN_Z,
                 hall_w=BZ_HARIM_W, hall_d=BZ_HARIM_D,
                 half_domes=BZ_HALF_DOMES, piers=BZ_PIERS,
                 minaret_span=BZ_MINARET_SPAN, minarets=BZ_MINARETS,
                 sherefe_each=BZ_SEREFE_EACH, tabhane_cells=BZ_TABHANE_CELLS,
                 court_domes=BZ_COURT_DOMES, sadirvan_dome=False,
                 palette="default"):
        self.dome_d, self.crown_z = dome_d, crown_z
        self.hall_w, self.hall_d = hall_w, hall_d
        self.half_domes, self.piers = half_domes, piers
        self.minaret_span = minaret_span
        self.minarets, self.sherefe_each = minarets, sherefe_each
        self.tabhane_cells = tabhane_cells
        self.court_domes = court_domes
        #: IV. Murad'ın kubbesi — 1632'de **yok** kabul edildi.
        self.sadirvan_dome = sadirvan_dome
        self.palette = palette

    @property
    def r(self):
        return self.dome_d * 0.5

    @property
    def dome_rise(self):
        return self.r * DOME_RISE_RATIO

    @property
    def spring_z(self):
        return self.crown_z - self.dome_rise

    @property
    def arch_crown_z(self):
        return self.spring_z - self.r * (math.sqrt(2.0) - 1.0)

    @property
    def arch_z(self):
        return self.arch_crown_z - self.r

    @property
    def wall_t(self):
        return 1.6

    @property
    def outer_w(self):
        """Harimin dış genişliği."""
        return self.hall_w + 2.0 * self.wall_t

    @property
    def wing_len(self):
        """Tabhâne kanadı — **79 m'lik ölçüden türer**, elle girilmez."""
        return (self.minaret_span - self.outer_w) * 0.5

    def validate(self):
        if abs(self.dome_d - BZ_DOME_D) > 0.01:
            raise ValueError(f"kubbe capi {self.dome_d} — olculen {BZ_DOME_D}")
        if self.half_domes != 2:
            raise ValueError("IKI yarim kubbe, kible ekseninde")
        if self.piers != 4:
            raise ValueError("DORT paye")
        if abs(self.hall_w - self.hall_d) > 1.0:
            raise ValueError(
                f"harim {self.hall_w} x {self.hall_d} — kaynak 'kare "
                "bicimli' der ve olcu 26 cm fark verir")
        # Kilit/cap orani, olculu dort selatin camisinin BANDINDA olmali.
        oran = self.crown_z / self.dome_d
        if not (1.80 <= oran <= 2.15):
            raise ValueError(
                f"kilit/cap orani {oran:.2f} — olculu dort camide 1,68-2,12 "
                "arasi; disina cikan bir kot TURETILEMEZ, olculmesi gerekir")
        if self.wing_len < 8.0:
            raise ValueError(
                f"tabhane kanadi {self.wing_len:.1f} m — 79 m'lik minare "
                "acikligina harim sigmiyor")
        if self.sherefe_each != 1:
            raise ValueError("her minare BIRER serefeli")
        return self


def build_beyazit(p, col, asset_name, textured=False):
    """Beyazıt II Camii (1506), 1632 hâli. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []
    W, D = p.outer_w, p.hall_d + 2.0 * p.wall_t

    podium_h = 1.8
    court_d = 34.0
    ay0 = -D * 0.5
    ay1 = ay0 - court_d
    parts.append(hz.assign(hz.make_box(f"Set_{asset_name}",
                                       (p.minaret_span + 6.0,
                                        D + court_d + 8.0, podium_h),
                                       (0.0, (D * 0.5 + 4.0 + ay1 - 4.0) * 0.5,
                                        podium_h * 0.5), col),
                           mats["cutstone"]))
    z0 = podium_h

    # --- Harim ------------------------------------------------------------
    wall_h = p.arch_z
    kat_b = wall_h / 2.0
    kabuk_b, built_b = dk.kabuk(mats, col, f"Harim_{asset_name}", W, D,
                                p.wall_t, z0,
                                ((7, 1.9, kat_b), (7, 1.6, kat_b)))
    parts += kabuk_b
    for o in dk.silme(f"Sacak_{asset_name}", W, D, z0 + built_b, col,
                      steps=3, h=0.8, out=0.5):
        parts.append(hz.assign(o, mats["cutstone"]))
    # HARIM TACKAPISI: avludan harime acilan anitsal giris. Revagin
    # arkasinda kalir ama revagin kemer araligindan gorunur — Osmanli
    # avlusunun bakisi zaten oraya kurulur.
    parts += dk.tackapi(mats, col, f"HarimTackapi_{asset_name}",
                        0.0, -D * 0.5, z0, 8.6, wall_h * 0.74, 2.0,
                        kapi_w=2.8, kapi_h=4.4)

    parts.append(hz.assign(hz.make_box(f"HarimOrtu_{asset_name}",
                                       (W + 1.0, D + 1.0, 0.5),
                                       (0.0, 0.0, z0 + wall_h + 0.25), col),
                           mats["lead"]))

    # --- Kemer kati (DORT paye) -------------------------------------------
    tymp = p.arch_crown_z - p.arch_z
    pier_ax = p.dome_d + 2.6
    parts.append(hz.assign(hz.make_box(f"KemerKati_{asset_name}",
                                       (pier_ax, pier_ax, tymp),
                                       (0.0, 0.0, z0 + p.arch_z + tymp * 0.5),
                                       col), mats["cutstone"]))

    # --- IKI YARIM KUBBE, KIBLE EKSENINDE ---------------------------------
    #
    # Yarim kubbe YARIM KUREDIR (Sultanahmet dersi, ADR 0047): kendi buyuk
    # kemerinin uzerine oturur, kilidi o kemerin kilididir.
    halfs = ((0.0, +p.r * 0.95, math.pi * 0.5),
             (0.0, -p.r * 0.95, -math.pi * 0.5))
    for i, (cx, cy, facing) in enumerate(halfs):
        parts.append(hz.assign(
            hz.make_half_dome(f"YarimKubbe_{i}", p.r, p.r, (cx, cy),
                              z0 + p.arch_z, facing=facing, segments=20,
                              rings=6, col=col), mats["lead"]))
        # YEDISER pencere — sayilan deger.
        for k in range(BZ_HALF_WINDOWS):
            a = facing - math.pi * 0.5 + math.pi * (k + 0.5) / BZ_HALF_WINDOWS
            parts.append(hz.assign(
                hz.make_box(f"YarimPencere_{i}{k}", (0.7, 0.7, 1.8),
                            (cx + math.cos(a) * p.r * 0.97,
                             cy + math.sin(a) * p.r * 0.97,
                             z0 + p.arch_z + 1.6), col), mats["shadow"]))

    # --- Kasnak + ana kubbe, YIRMI pencere --------------------------------
    drum_h = p.spring_z - p.arch_crown_z
    parts.append(hz.assign(hz.make_tube(f"Kasnak_{asset_name}", p.r * 1.16,
                                        p.r * 1.12, drum_h, (0.0, 0.0),
                                        z0 + p.arch_crown_z, segments=20,
                                        cap_top=False, col=col),
                           mats["cutstone"]))
    dome = hz.make_dome(f"Kubbe_{asset_name}", p.r, p.dome_rise, (0.0, 0.0),
                        z0 + p.spring_z, segments=BZ_DOME_WINDOWS, rings=8,
                        col=col)
    hz.assign(dome, mats["lead"])
    parts.append(dome)

    dmn, dmx = hz.bounds(dome)
    measured_d = max(dmx[0] - dmn[0], dmx[1] - dmn[1])
    if abs(measured_d - p.dome_d) > 0.08:
        raise ValueError(f"kubbe mesh capi {measured_d:.3f} — {p.dome_d:.2f}")

    # YIRMI pencere: kubbe 20 dilimli uretildi, pencereler dilim aralarina
    # duser (Ayasofya'daki kirk kaburga ile ayni ilke, ADR 0045).
    for k in range(BZ_DOME_WINDOWS):
        a = 2.0 * math.pi * (k + 0.5) / BZ_DOME_WINDOWS
        parts.append(hz.assign(
            hz.make_box(f"KubbePencere_{k}", (0.8, 0.8, 2.2),
                        (math.cos(a) * p.r * 0.98, math.sin(a) * p.r * 0.98,
                         z0 + p.spring_z + 1.1), col), mats["shadow"]))

    for o in dk.kubbe_kaburga(f"KubbeDikis_{asset_name}", 0.0, 0.0, p.r,
                              z0 + p.spring_z, p.dome_rise, col, n=20):
        parts.append(hz.assign(o, mats["lead"]))
    for o in dk.alem(f"Alem_{asset_name}", 0.0, 0.0, z0 + p.crown_z, col,
                     scale=1.3):
        parts.append(hz.assign(o, mats["lead"]))

    # --- TABHANE KANATLARI: DORDER KUBBELI HUCRE --------------------------
    #
    # Kanat uzunlugu ELLE GIRILMEZ: minareler arasi 79 m OLCULU ve harimin
    # dis genisligi ondan cikarilip ikiye bolunur.
    wing = p.wing_len
    wing_h = 9.5
    cell = wing / p.tabhane_cells
    for sx in (-1, 1):
        cxw = sx * (W * 0.5 + wing * 0.5)
        parts.append(hz.assign(
            hz.make_box(f"Tabhane_{sx}", (wing, D * 0.62, wing_h),
                        (cxw, 0.0, z0 + wing_h * 0.5), col), mats["cutstone"]))
        for k in range(p.tabhane_cells):
            ux = sx * (W * 0.5 + cell * (k + 0.5))
            parts.append(hz.assign(
                hz.make_dome(f"TabhaneKubbe_{sx}{k}", cell * 0.42,
                             cell * 0.33, (ux, 0.0), z0 + wing_h,
                             segments=12, rings=4, col=col), mats["lead"]))

    # --- IKI MINARE, TABHANE KOSESINDE, ARALARI 79 m ----------------------
    for i, sx in enumerate((-1, 1)):
        parts += _minaret_multi(p, mats, col, f"Minare{i}",
                                sx * p.minaret_span * 0.5, -D * 0.30, z0,
                                p.sherefe_each, p.crown_z * 0.92)

    # --- AVLU: YIRMI DORT KUBBELI REVAK -----------------------------------
    _rev_h = 5.3
    avlu_h = dk.revak_ust(court_d, 6, _rev_h) + 0.6
    court_w = W
    for sx in (-1, 1):
        parts.append(hz.assign(
            hz.make_box(f"AvluDuvar_{sx}", (1.5, court_d, avlu_h),
                        (sx * (court_w * 0.5 - 0.75), (ay0 + ay1) * 0.5,
                         z0 + avlu_h * 0.5), col), mats["cutstone"]))
    parts.append(hz.assign(
        hz.make_box("AvluDuvar_On", (court_w, 1.5, avlu_h),
                    (0.0, ay1 + 0.75, z0 + avlu_h * 0.5), col),
        mats["cutstone"]))
    # KAPALI REVAK HALKASI. Beyazit icin kubbe sayisi (24) kaynaktan;
    # sutun sayisi kaynakta YOK. Halkanin yasasi 20 sutun ongoruyor ama
    # bu TURETILMIS bir degerdir — olculmus gibi davranmiyorum, bu yuzden
    # Fatih ve Sultanahmet'teki gibi bir sutun denetimi yazmadim.
    b_yan, b_on = dk.gozleri_dagit(p.court_domes,
                                   (court_d, court_d,
                                    court_w, court_w))[:3:2]
    ndome, ofs, rev_h = 0, 4.6, _rev_h
    for sx in (-1, 1):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_{sx}",
            sx * (court_w * 0.5 - ofs), ay1 + ofs,
            sx * (court_w * 0.5 - ofs), ay0 - ofs,
            b_yan, z0, rev_h, 0.40,
            bay=ofs, bay_dir=(sx, 0.0), ends=(False, False))
        ndome += b_yan
    # Goz DUVARA acilir: ay1'deki sirada duvar -Y'de, ay0'daki
    # sirada harim +Y'de. Ilk yazimda isaretler tersti ve on
    # revagin gozu duvara degil avluya bakiyordu.
    for sy, yy in ((-1.0, ay1 + ofs), (1.0, ay0 - ofs)):
        parts += dk.revak_sirasi(
            mats, col, f"AvluRevak_Y{int(sy)}",
            -court_w * 0.5 + ofs, yy, court_w * 0.5 - ofs, yy,
            b_on, z0, rev_h, 0.40,
            bay=ofs, bay_dir=(0.0, sy), ends=(False, False))
        ndome += b_on
    for sx in (-1, 1):
        for yy in (ay1 + ofs, ay0 - ofs):
            parts += dk.kose_ayagi(mats, col, f"AvluKose_{sx}_{int(yy)}",
                                   sx * (court_w * 0.5 - ofs), yy,
                                   z0, rev_h, 0.40)
    if ndome != p.court_domes:
        raise ValueError(f"avlu kubbesi {ndome} — kaynak {p.court_domes} der")

    # SADIRVAN — 1632'de KUBBESIZ.
    #
    # TDV: sekiz sutuna oturan kubbeyi IV. MURAD ekletmistir, 1623-1640
    # arasi. Oyunun yili o araligin TAM ORTASI. Kubbe konmadi cunku Murad IV
    # gercek iktidari 1632'de aldi ve buyuk hayrat isleri ondan sonra
    # beklenir — ama bu bir OLASILIKTIR, kesinlik degil.
    sad_y = (ay0 + ay1) * 0.5
    parts.append(hz.assign(
        hz.make_tube(f"Sadirvan_{asset_name}", 3.0, 2.8, 2.2, (0.0, sad_y),
                     z0, segments=8, col=col), mats["marble"]))
    if p.sadirvan_dome:
        for k in range(8):
            a = 2.0 * math.pi * k / 8.0
            parts.append(hz.assign(
                hz.make_tube(f"SadirvanSutun_{k}", 0.28, 0.26, 4.2,
                             (math.cos(a) * 4.2, sad_y + math.sin(a) * 4.2),
                             z0, segments=8, col=col), mats["marble"]))
        parts.append(hz.assign(
            hz.make_dome(f"SadirvanKubbe_{asset_name}", 4.6, 2.6,
                         (0.0, sad_y), z0 + 4.2, segments=14, rings=5,
                         col=col), mats["lead"]))

    # --- LOD1 -------------------------------------------------------------
    l1.append(hz.assign(hz.make_box("L1_Harim", (W, D, wall_h),
                                    (0.0, 0.0, z0 + wall_h * 0.5), col),
                        mats["cutstone"]))
    l1.append(hz.assign(hz.make_dome("L1_Kubbe", p.r, p.dome_rise, (0.0, 0.0),
                                     z0 + p.spring_z, segments=12, rings=4,
                                     col=col), mats["lead"]))
    for i, (cx, cy, facing) in enumerate(halfs):
        l1.append(hz.assign(
            hz.make_half_dome(f"L1_Yarim{i}", p.r, p.r, (cx, cy),
                              z0 + p.arch_z, facing=facing, segments=10,
                              rings=3, col=col), mats["lead"]))
    for sx in (-1, 1):
        l1.append(hz.assign(
            hz.make_box(f"L1_Tabhane{sx}", (wing, D * 0.62, wing_h),
                        (sx * (W * 0.5 + wing * 0.5), 0.0, z0 + wing_h * 0.5),
                        col), mats["cutstone"]))
        l1.append(hz.assign(
            hz.make_tube(f"L1_Minare{sx}", 1.35, 1.0, p.crown_z * 0.92 + 13.7,
                         (sx * p.minaret_span * 0.5, -D * 0.30), z0,
                         segments=8, col=col), mats["cutstone"]))

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

    measured_span = 0.0
    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="selatin", palette=p.palette, status="draft",
                accuracy="D2", dome_d=round(p.dome_d, 2),
                dome_crown_z=round(p.crown_z, 2),
                measured_dome_d=round(measured_d, 3),
                half_domes=p.half_domes, piers=p.piers,
                dome_windows=BZ_DOME_WINDOWS,
                half_dome_windows=BZ_HALF_WINDOWS,
                minarets=p.minarets, sherefe_each=p.sherefe_each,
                sherefe_total=p.minarets * p.sherefe_each,
                minaret_span=round(p.minaret_span, 2),
                tabhane_cells=p.tabhane_cells,
                tabhane_cells_total=p.tabhane_cells * 2,
                wing_len=round(p.wing_len, 2),
                portico_bays=p.court_domes, double_portico=False,
                sadirvan=True, sadirvan_dome=p.sadirvan_dome,
                harim_w=p.hall_w, harim_d=p.hall_d, podium_h=podium_h,
                minaret_top=round(mx[2] - mn[2], 2))
    return lod0, lod1, ucx, info
