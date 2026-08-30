"""
**Hayat donatısı** — mahalle avlusunun ve evler arası boşluğun eşyası.

## Neden bu kit var

Caner üç ayrı turda aynı şeyi söyledi:

> "acik dunya zemini gercekci degil ve cok fazla bos duruyor"
> "bosluk hala dolmamis gibi geldi"
> "sehir daha dolu gozuksun. gta ve rdr mantigi sehirde gorsel unsur
>  fazla olsun"

Şehir dışındaki boşluk kapatıldı (yollar, bostan, meyvelik — ADR 0074).
Şehir **içindeki** boşluk ölçüldü ve durum şuydu: bir mahallenin 200 m'lik
karesinde zeminin **%90,3'ü çıplak arazi**, **%81,7'sinin 4 m yakınında
hiçbir şey yok**, ve 240 m'lik kare içinde **sıfır** ağaç — çünkü kırsal
doku binalara 45 m'den fazla yaklaşmıyor.

Yani mahalle, sokağın iki yanına dizilmiş bir ev şeridi ve etrafında boş
bir tarla. Oysa Osmanlı konutu **avlulu ve hayatlıdır**: evin arkası bahçe,
avlusunda kuyu, su küpü, odunluk, asma çardağı vardır. Eksik olan şey
"dekor" değil, konutun kendi eklentileri.

## Doğruluk: D3, hepsi T3

Bu nesnelerin hiçbiri RESEARCH.md'de ölçüsüyle geçmiyor ve kaynakta
1632'ye ait bir odunluk ölçüsü yok. Bu yüzden **metrik geometri
uydurulmuyor**: her parça insan ölçeğinden türetiliyor ve neden o boyda
olduğu yazılıyor. Su küpü omuz yüksekliğinden alçaktır çünkü doldurulup
taşınır; çardak 2,2 m'dir çünkü altından geçilir; kuyu bileziği 0,80 m'dir
çünkü çocuk düşmesin diye bel hizasındadır ve üstüne oturulur.

Ölçü uydurmakla **oran kurmak** aynı şey değil: birincisi kaynağa
yalan söyler, ikincisi kaynağın sustuğu yerde insana bakar.
"""

import math

import hz_blender as hz
import ottoman_kit as kit


#: Çardağın altından geçilir — baş üstü açıklık.
CARDAK_YUKSEK = 2.20

#: Kuyu bileziği bel hizası: hem üstüne dayanılır hem çocuk düşmez.
KUYU_BILEZIK_Z = 0.80


class HayatParams(object):
    """Tek bir hayat donatısı. `tur` hangi nesne olduğunu söyler."""

    TURLER = ("odunluk", "kup", "sepet", "cardak", "kuyu", "cit",
              "sebze")

    def __init__(self, tur="odunluk", olcek=1.0, tohum=0, palette="default"):
        self.tur = tur
        self.olcek = olcek
        self.tohum = tohum
        self.palette = palette

    def validate(self):
        if self.tur not in self.TURLER:
            raise ValueError(f"bilinmeyen tur: {self.tur}")
        # Olcek serbest degil: bu nesneler INSAN olcegine bagli ve
        # yarisi/iki kati bambaska bir sey olur.
        if not 0.75 <= self.olcek <= 1.35:
            raise ValueError(f"olcek={self.olcek} — insan olceginden kopuyor")
        return self


def _rnd(tohum, k):
    """Tohumdan türeyen 0..1 — şehir deterministik kalsın."""
    h = (tohum * 2654435761 + k * 40503) & 0xFFFFFFFF
    h ^= h >> 13
    h = (h * 2246822519) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFF) / 65535.0


# --------------------------------------------------------------- odunluk

def _odunluk(parts, l1, mats, col, s, tohum):
    """
    **Odun yığını.** Kışlık odun avluda istiflenir; yığın duvara dayanır.

    Ölçü insandan: yığın 0,95 m yüksek (belden yukarısı devrilir),
    1,70 m uzun (bir kucak odunun iki katı), 0,60 m derin (kollarla
    kavranan boy).
    """
    en, derin, yuk = 1.70 * s, 0.60 * s, 0.95 * s
    r = 0.065 * s                       # tek kütüğün yarıçapı
    apotem = r * math.cos(math.pi / 6.0)   # altıgen prizmanın düz yüzü
    sira = max(2, int(yuk / (r * 2.0)))
    sut = max(3, int(en / (r * 2.0)))

    for i in range(sira):
        # Her sıra yarım kütük kayar: istif böyle durur.
        kayma = (r if i % 2 else 0.0)
        for j in range(sut):
            x = -en * 0.5 + r + j * r * 2.0 + kayma
            if x > en * 0.5 - r * 0.5:
                continue
            # Üst sıra eksik olur — istif hiçbir zaman tam bitmez.
            if i == sira - 1 and _rnd(tohum, i * 31 + j) < 0.45:
                continue
            # ONCE ORIJINDE KUR, SONRA DONDUR, EN SON YERINE TASI.
            #
            # Parcayi yerinde kurup dondurmek onu DUNYA ORIJINI etrafinda
            # dondurur; kit bunu bir degismezle yakaliyor ve hakli:
            # ilk yazimda kutuk 0,43 m kaydi.
            o = hz.make_tube(f"Odun_{i}_{j}", r, r * 0.94, derin,
                             center_xy=(0.0, 0.0), base_z=-derin * 0.5,
                             segments=6, cap_top=True, cap_bottom=True, col=col)
            # Kütükler YATAY durur: dikey istif okunmaz.
            o.rotation_euler = (math.radians(90.0), 0.0,
                                math.radians((_rnd(tohum, i * 7 + j) - 0.5) * 6.0))
            # ALTI DUZ YUZDUR, KOSE DEGIL. `make_tube` altigen bir prizma
            # uretir: eksenin yere uzakligi yaricap degil APOTEMdir
            # (r * cos 30). Yaricapla yerlestirince yigin 0,87 cm havada
            # kaldi ve kitin pivot degismezi bunu yakaladi. Kutuk zaten
            # yuzune yatar, kosesine degil.
            o.location = (x, derin * 0.5, apotem + i * r * 1.85)
            parts.append(hz.assign(o, mats["timber_bare"]))

    l1.append(hz.assign(hz.make_box(
        "L1_Odunluk", (en, derin, yuk), (0.0, derin * 0.5, yuk * 0.5), col),
        mats["timber_bare"]))
    return en, derin, yuk


# ------------------------------------------------------------------- küp

def _kup(parts, l1, mats, col, s, tohum):
    """
    **Su küpü.** Avlunun su kabı; doldurulup taşınır, o yüzden bel
    hizasından alçaktır (0,72 m).
    """
    h = 0.72 * s
    # Gövde: karın ortada, ağız dar. Üç parçalı yaklaşım yeterli —
    # yakından bakılan bir nesne değil.
    parts.append(hz.assign(hz.make_tube(
        "Kup_Alt", 0.14 * s, 0.26 * s, h * 0.42, base_z=0.0,
        segments=12, cap_bottom=True, col=col), mats["roof"]))
    parts.append(hz.assign(hz.make_tube(
        "Kup_Karin", 0.26 * s, 0.19 * s, h * 0.40, base_z=h * 0.42,
        segments=12, col=col), mats["roof"]))
    parts.append(hz.assign(hz.make_tube(
        "Kup_Boyun", 0.19 * s, 0.12 * s, h * 0.18, base_z=h * 0.82,
        segments=12, cap_top=True, col=col), mats["roof"]))
    l1.append(hz.assign(hz.make_box(
        "L1_Kup", (0.44 * s, 0.44 * s, h), (0.0, 0.0, h * 0.5), col),
        mats["roof"]))
    return 0.52 * s, 0.52 * s, h


# ----------------------------------------------------------------- sepet

def _sepet(parts, l1, mats, col, s, tohum):
    """**Sepet.** Hasır sepet — yere konur, içine bir şey konur."""
    h = 0.34 * s
    parts.append(hz.assign(hz.make_tube(
        "Sepet", 0.19 * s, 0.25 * s, h, base_z=0.0,
        segments=12, cap_bottom=True, cap_top=False, col=col),
        mats["timber_bare"]))
    l1.append(hz.assign(hz.make_box(
        "L1_Sepet", (0.5 * s, 0.5 * s, h), (0.0, 0.0, h * 0.5), col),
        mats["timber_bare"]))
    return 0.5 * s, 0.5 * s, h


# ---------------------------------------------------------------- çardak

def _cardak(parts, l1, mats, col, s, tohum):
    """
    **Asma çardağı.** Dört direk, üstünde kafes; asma onun üstüne sarılır.
    Altından geçilir, o yüzden 2,20 m.
    """
    en, derin, yuk = 3.00 * s, 2.40 * s, CARDAK_YUKSEK * s
    d = 0.10 * s
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Direk_{sx}_{sy}", (d, d, yuk),
                (sx * (en * 0.5 - d), sy * (derin * 0.5 - d), yuk * 0.5), col),
                mats["timber_bare"]))
    # Kirişler: iki yönde, üstte.
    for sy in (-1, 1):
        parts.append(hz.assign(hz.make_box(
            f"Kiris_{sy}", (en, d, d * 0.9),
            (0.0, sy * (derin * 0.5 - d), yuk + d * 0.45), col),
            mats["timber_bare"]))
    kafes = 5
    for k in range(kafes):
        t = -1.0 + 2.0 * (k + 0.5) / kafes
        parts.append(hz.assign(hz.make_box(
            f"Kafes_{k}", (d * 0.7, derin, d * 0.6),
            (t * en * 0.42, 0.0, yuk + d * 1.2), col),
            mats["timber_bare"]))
    # ASMA: cardak bos bir iskele degil, uzerine asma sarilir.
    #
    # Iskeleti tek basina koymak "insaat" gibi okunuyordu; cardagin
    # varlik sebebi golgedir ve golgeyi yaprak yapar. Yaprak orgusu
    # kafesin USTUNE, hafif tasarak konur — asma kenardan sarkar.
    parts.append(hz.assign(hz.make_box(
        "Asma", (en * 1.06, derin * 1.06, d * 0.55),
        (0.0, 0.0, yuk + d * 1.55), col), mats["foliage_cinar"]))
    for k in range(4):
        # Kenardan sarkan salkimlar: duz bir levha "tente" gibi okunur.
        float_t = -1.0 + 2.0 * (k + 0.5) / 4.0
        parts.append(hz.assign(hz.make_box(
            f"AsmaSarkma_{k}", (d * 1.6, d * 1.6, d * 2.4),
            (float_t * en * 0.40, derin * 0.52, yuk + d * 0.6), col),
            mats["foliage_cinar"]))

    l1.append(hz.assign(hz.make_box(
        "L1_Cardak", (en, derin, yuk), (0.0, 0.0, yuk * 0.5), col),
        mats["timber_bare"]))
    return en * 1.06, derin * 1.06, yuk + d * 1.9


# ------------------------------------------------------------------ kuyu

def _kuyu(parts, l1, mats, col, s, tohum):
    """
    **Kuyu.** Taş bilezik + iki direk + makara kirişi + kova.

    Bilezik bel hizasındadır (0,80 m): hem üstüne dayanılır hem çocuk
    düşmez. Bu bir ölçü iddiası değil, insan ölçeğinden bir çıkarım.
    """
    r = 0.45 * s
    z = KUYU_BILEZIK_Z * s
    parts.append(hz.assign(hz.make_tube(
        "Bilezik_Dis", r, r, z, base_z=0.0, segments=14,
        cap_top=False, col=col), mats["stone"]))
    parts.append(hz.assign(hz.make_tube(
        "Bilezik_Ic", r * 0.72, r * 0.72, z, base_z=0.02 * s, segments=14,
        cap_top=False, cap_bottom=True, col=col), mats["stone"]))
    # Üst halka: bileziğin ağzını kapatan taş yüzey.
    parts.append(hz.assign(hz.make_tube(
        "Bilezik_Ust", r, r * 0.99, 0.06 * s, base_z=z - 0.06 * s,
        segments=14, cap_top=True, col=col), mats["cutstone"]))

    d = 0.09 * s
    kol = 1.85 * s
    for sx in (-1, 1):
        parts.append(hz.assign(hz.make_box(
            f"KuyuDirek_{sx}", (d, d, kol),
            (sx * r * 0.85, 0.0, kol * 0.5), col), mats["timber_bare"]))
    parts.append(hz.assign(hz.make_box(
        "KuyuKiris", (r * 2.1, d, d), (0.0, 0.0, kol + d * 0.5), col),
        mats["timber_bare"]))
    # Kova ipin ucunda asılı — kuyu "çalışıyor" görünsün.
    parts.append(hz.assign(hz.make_box(
        "Ip", (0.02 * s, 0.02 * s, kol * 0.42),
        (0.0, 0.0, kol - kol * 0.21), col), mats["trim"]))
    parts.append(hz.assign(hz.make_tube(
        "Kova", 0.11 * s, 0.13 * s, 0.20 * s,
        base_z=kol - kol * 0.42 - 0.20 * s, segments=10,
        cap_bottom=True, col=col), mats["timber_bare"]))

    l1.append(hz.assign(hz.make_box(
        "L1_Kuyu", (r * 2.2, r * 2.2, kol + d),
        (0.0, 0.0, (kol + d) * 0.5), col), mats["stone"]))
    return r * 2.2, r * 2.2, kol + d


# ------------------------------------------------------------------- çit

def _cit(parts, l1, mats, col, s, tohum):
    """
    **Çit.** Bahçe sınırı — avlu duvarı taştır, bahçe çiti çalıdır.
    Alçak (1,05 m): sınır çizer, görüşü kesmez.
    """
    en, yuk = 2.40 * s, 1.05 * s
    d = 0.07 * s
    kazik = 7
    for k in range(kazik):
        t = -1.0 + 2.0 * k / (kazik - 1)
        h = yuk * (0.88 + 0.24 * _rnd(tohum, k))
        parts.append(hz.assign(hz.make_box(
            f"Kazik_{k}", (d, d, h), (t * en * 0.5, 0.0, h * 0.5), col),
            mats["timber_bare"]))
    for k in range(3):
        z = yuk * (0.28 + 0.30 * k)
        parts.append(hz.assign(hz.make_box(
            f"Yatay_{k}", (en, d * 0.7, d * 0.6), (0.0, 0.0, z), col),
            mats["timber_bare"]))
    l1.append(hz.assign(hz.make_box(
        "L1_Cit", (en, d * 2, yuk), (0.0, 0.0, yuk * 0.5), col),
        mats["timber_bare"]))
    return en, d * 3, yuk


# ----------------------------------------------------------------- sebze

def _sebze(parts, l1, mats, col, s, tohum):
    """
    **Sebze tahtası.** Bahçenin işlenmiş kısmı: sürülmüş toprak sırtları
    ve üstünde yeşil.

    Ölçü insandan: sırt arası 0,55 m (iki sıra arasından geçilir), tahta
    2,4 x 1,6 m (bir kişinin eğilmeden ulaşabileceği en), sırt 0,22 m
    yüksek (çapayla atılan toprak).
    """
    en, derin = 2.40 * s, 1.60 * s
    sirt_h = 0.22 * s
    sira = 3
    # EKILI TAHTA YESILDIR, KAHVERENGI DEGIL.
    #
    # Ilk yazimda sirtlar `bark` malzemesiyleydi — mantik "toprak
    # kahverengidir" idi ama `bark` AGAC KABUGU dokusudur ve dokulu
    # boru hattinda turuncu okuyor: bahcedeki 1.945 tahta, yerden
    # bakinca turuncu sandiklara donmustu.
    #
    # Dogrusu daha basit: ekili bir sebze tahtasinin GORUNEN yuzeyi
    # bitkidir. Toprak yalnizca kenarda, ince bir seritte gorunur.
    for i in range(sira):
        y = -derin * 0.5 + derin * (i + 0.5) / sira
        # Toprak: alcak ve ince — yalnizca sirtin kenari gorunur.
        parts.append(hz.assign(hz.make_box(
            f"Sirt_{i}", (en, derin / sira * 0.72, sirt_h * 0.45),
            (0.0, y, sirt_h * 0.225), col), mats["trim"]))
        # Bitki: sirtin ustunu ortuyor.
        for k in range(4):
            x = -en * 0.5 + en * (k + 0.5) / 4.0
            h = (0.22 + 0.12 * _rnd(tohum, i * 13 + k)) * s
            parts.append(hz.assign(hz.make_box(
                f"Yesil_{i}_{k}", (en / 4.0 * 0.82, derin / sira * 0.66, h),
                (x, y, sirt_h * 0.45 + h * 0.5), col),
                mats["foliage_cinar"]))

    l1.append(hz.assign(hz.make_box(
        "L1_Sebze", (en, derin, sirt_h * 2.0),
        (0.0, 0.0, sirt_h), col), mats["bark"]))
    return en, derin, sirt_h + 0.26 * s


_YAPICILAR = {
    "odunluk": _odunluk, "kup": _kup, "sepet": _sepet,
    "cardak": _cardak, "kuyu": _kuyu, "cit": _cit,
    "sebze": _sebze,
}


def build_hayat(p, col, asset_name, textured=False):
    """Bir hayat donatısı. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    en, derin, yuk = _YAPICILAR[p.tur](
        parts, l1, mats, col, p.olcek, p.tohum)

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)

    # Çarpıştırıcı KUTU: bu nesnelerin hiçbiri içine girilecek şey değil.
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["trim"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="hayat", tur=p.tur, palette=p.palette,
                status="draft", accuracy="D3",
                courtyard_only=True)
    return lod0, lod1, ucx, info
