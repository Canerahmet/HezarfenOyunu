"""
**Ayrıntı dağarcığı** — Osmanlı mimarisinin tekrar eden öğeleri.

## Neden bu dosya var

Faz 3'ün landmark'ları **kütle** seviyesinde üretildi: Süleymaniye
7 294 üçgen, Ayasofya 4 448. Otuz altı landmark'ın **toplamı** 77 661 —
tek bir hero yapının olması gereken kadar. Siluet doğruydu, mimari
değildi: pencereler koyu dikdörtgen, korniş düz kutu, sütun silindir,
şerefe bir tabla, minare gövdesi düz koni.

Bu kit o boşluğu **paylaşılan bir dağarcıkla** kapatır. Beş selâtin
camisi aynı `_minaret_multi`, `_revak`, `_column`, `_sherefe`
yardımcılarını kullanıyor; onları buradaki öğelerle yeniden yazmak
beşini birden yükseltir.

## Ölçüt: "fotoğraftaki gibi" değil, "fotoğraftaki dil kadar"

İki kısıt var ve ikisi de gerçek:

1. **Fotoğraftaki her ayrıntı 1632 değildir.** Ayasofya'nın okra boyası
   Fossati'nin (1847-49), Fatih Camii'nin bütün dış cephesi 1767-71'in.
   Kaynağa bakıp gördüğünü kopyalamak, üç yüzyıl ileri gitmek olur.
2. **Telifli görsel kopyalanmaz** (SALT CC BY-NC-ND, Müller-Wiener
   telifli). Onlara bakılır, onlardan çizilmez.

Bu yüzden ayrıntı **biçimden** değil **gramerden** türer: mukarnas nasıl
kurulur, şerefe neye oturur, kemer hangi eğridir, silme kaç basamaktır.
Bunlar dönemin mimarisinin kendi kuralları ve her yapıya ölçüsüne göre
uygulanır — kopya değil, **inşa**.

## Üçgen bütçesi

RTX 4070 Laptop / 8 GB VRAM, uçuş oyunu: aynı anda 10-20 landmark
görünür, çoğu uzakta. LOD1 ve LOD2 uzağı taşıdığı sürece LOD0 rahatça
büyüyebilir. Hedef: selâtin camisi **40-70 bin**, kule/türbe/kapı
**5-15 bin**, sur burcu **1 500'ün altında** (192 örnek var).
"""

import math

import hz_blender as hz
import street_kit as sk


# ------------------------------------------------------------------- silme

def silme(name, w, d, z, col, steps=3, h=0.72, out=0.42, ters=False):
    """
    **Silme (korniş)** — düz bir kuşak değil, basamaklı bir profil.

    Osmanlı kâgirinde bir kuşak hiçbir zaman tek bir dikdörtgen değildir:
    aşağıdan yukarı doğru büyüyen (ya da `ters` ise küçülen) basamaklardan
    kurulur ve gölgeyi tutan şey o basamaklardır. Tek kutuyla yapılınca
    uzaktan **çizgi**, yakından **bant** gibi okunuyordu.

    `w`, `d` kuşağın oturduğu kütlenin ölçüsü; `out` toplam taşma.
    """
    parts = []
    for i in range(steps):
        t = (i + 1) / steps
        o = out * (1.0 - t if ters else t)
        hh = h / steps
        parts.append(hz.make_box(f"{name}_{i}", (w + 2.0 * o, d + 2.0 * o, hh),
                                 (0.0, 0.0, z + hh * (i + 0.5)), col))
    return parts


def silme_at(name, cx, cy, w, d, z, col, steps=3, h=0.72, out=0.42,
             ters=False):
    """`silme` ama merkezi verilebilen."""
    parts = silme(name, w, d, z, col, steps, h, out, ters)
    for p in parts:
        p.location = (p.location[0] + cx, p.location[1] + cy, p.location[2])
    return parts


# ---------------------------------------------------------------- mukarnas

def mukarnas(name, cx, cy, r_alt, r_ust, z, h, col, tiers=4, segments=12):
    """
    **Mukarnas** — sarkıtlı geçiş; şerefe altının, sütun başlığının ve
    taçkapı nişinin ortak dili.

    Gerçek mukarnas küçük tonoz hücrelerinin katmanlanmasıdır; burada her
    kat, bir öncekinden **taşan** ve **kaydırılmış** bir prizma dizisiyle
    yaklaşılır. Uzaktan okunması gereken şey hücrelerin tek tek biçimi
    değil, **dişli gölge dokusu**dur.

    `r_alt` altta (dar), `r_ust` üstte (geniş) yarıçap: mukarnas yukarı
    doğru **açılır**, çünkü işi bir çıkmayı taşımaktır.
    """
    parts = []
    for k in range(tiers):
        t = (k + 0.5) / tiers
        r = r_alt + (r_ust - r_alt) * t
        zz = z + h * k / tiers
        hh = h / tiers
        n = segments + k * 2                      # ust katta daha cok hucre
        for i in range(n):
            a = 2.0 * math.pi * (i + 0.5 * (k % 2)) / n
            cell = min(2.0 * math.pi * r / n * 0.62, hh * 1.8)
            parts.append(hz.make_box(
                f"{name}_{k}_{i}", (cell, cell, hh * 1.05),
                (cx + math.cos(a) * r, cy + math.sin(a) * r, zz + hh * 0.5),
                col))
    return parts


def mukarnas_konsol(name, cx, cy, w, z, h, col, tiers=3):
    """Düz bir yüze oturan mukarnas konsol — taçkapı ve mihrap için."""
    parts = []
    for k in range(tiers):
        t = (k + 0.5) / tiers
        n = 3 + k * 2
        hh = h / tiers
        for i in range(n):
            u = -w * 0.5 + w * (i + 0.5) / n
            cell = w / n * 0.72
            parts.append(hz.make_box(
                f"{name}_{k}_{i}", (cell, cell * 0.5 * (1.0 - t * 0.4), hh),
                (cx + u, cy, z + h * k / tiers + hh * 0.5), col))
    return parts


# ------------------------------------------------------------------ sütun

def sutun(name, cx, cy, z0, h, r, col, capital="mukarnas", segments=12):
    """
    **Sütun** — kaide, gövde, başlık. Üçü de ayrı ve üçü de görünür.

    Revakta sütun bir silindir değildir: altında bir **kaide**, üstünde
    **baklava dilimli** ya da **mukarnaslı** bir başlık vardır ve revağın
    ritmini okutan şey başlıkların sırasıdır. Üsküdar Mihrimah'ın kaynağı
    iki başlık tipini **ayrı ayrı** anar (birinci revak mukarnaslı, ikinci
    revak baklava dilimli) — yani tip bir süs değil, **bilgi**.
    """
    parts = []
    base_h = r * 0.9
    cap_h = r * 1.5
    shaft_h = h - base_h - cap_h
    parts.append(hz.make_box(f"{name}_Kaide", (r * 2.5, r * 2.5, base_h),
                             (cx, cy, z0 + base_h * 0.5), col))
    parts.append(hz.make_tube(f"{name}_Govde", r, r * 0.93, shaft_h,
                              (cx, cy), z0 + base_h, segments=segments,
                              col=col))
    top = z0 + base_h + shaft_h
    if capital == "mukarnas":
        parts += mukarnas(f"{name}_Baslik", cx, cy, r * 0.95, r * 1.35,
                          top, cap_h * 0.72, col, tiers=3, segments=8)
        parts.append(hz.make_box(f"{name}_Abakus", (r * 2.9, r * 2.9,
                                                    cap_h * 0.28),
                                 (cx, cy, top + cap_h * 0.86), col))
    else:
        # BAKLAVA DILIMLI: kose kose kirilan, mukarnastan sade baslik.
        parts.append(hz.make_tube(f"{name}_Baslik", r * 1.02, r * 1.34,
                                  cap_h * 0.7, (cx, cy), top, segments=8,
                                  phase=math.pi / 8.0, col=col))
        parts.append(hz.make_box(f"{name}_Abakus", (r * 2.8, r * 2.8,
                                                    cap_h * 0.3),
                                 (cx, cy, top + cap_h * 0.85), col))
    return parts


# ----------------------------------------------------------------- şerefe

def serefe(name, cx, cy, z, r, col, korkuluk_n=16):
    """
    **Şerefe** — bir tabla değil, üç parça: mukarnas konsol, taşıyıcı
    tabla, **delikli** korkuluk.

    Minarenin siluetinde okunan tek ayrıntı budur ve düz bir disk olarak
    yapıldığında minare "çubuk" gibi kalıyordu. Mukarnas altta, korkuluk
    üstte; korkuluğun **boşlukları** onu korkuluk yapar.
    """
    parts = []
    parts += mukarnas(f"{name}_Konsol", cx, cy, r * 1.02, r * 1.75,
                      z - r * 0.9, r * 0.9, col, tiers=4, segments=12)
    parts.append(hz.make_tube(f"{name}_Tabla", r * 1.85, r * 1.85, 0.26,
                              (cx, cy), z, segments=16, col=col))
    # Korkuluk: dikmeler + ust ve alt kusak. Bosluklar SILUETTE okunur.
    parts.append(hz.make_tube(f"{name}_AltKusak", r * 1.80, r * 1.80, 0.14,
                              (cx, cy), z + 0.26, segments=16, col=col))
    for i in range(korkuluk_n):
        a = 2.0 * math.pi * i / korkuluk_n
        parts.append(hz.make_box(
            f"{name}_Dikme_{i}", (0.10, 0.10, 0.78),
            (cx + math.cos(a) * r * 1.78, cy + math.sin(a) * r * 1.78,
             z + 0.40 + 0.39), col))
    parts.append(hz.make_tube(f"{name}_UstKusak", r * 1.84, r * 1.84, 0.16,
                              (cx, cy), z + 1.18, segments=16, col=col))
    return parts


# --------------------------------------------------------- minare gövdesi

def minare_govde(name, cx, cy, z0, h, r_alt, r_ust, col, yivli=False,
                 segments=16):
    """
    Minare gövdesi — **çok yüzlü**, isteğe bağlı **yivli**.

    Klasik Osmanlı minaresi silindir değildir; çok yüzlüdür ve fark
    siluette okunur. `yivli=True` gövdeye dikey oluklar ekler: Ayasofya'nın
    doğu minarelerinden biri kaynakta **"yivli"** diye ayrıca anılır
    (ADR 0045) ve o sıfat modelde bir şeye karşılık gelmeliydi.
    """
    parts = [hz.make_tube(f"{name}_Govde", r_alt, r_ust, h, (cx, cy), z0,
                          segments=segments, col=col)]
    if yivli:
        n = segments
        for i in range(n):
            a = 2.0 * math.pi * (i + 0.5) / n
            w = 2.0 * math.pi * r_alt / n * 0.42
            parts.append(hz.make_box(
                f"{name}_Yiv_{i}", (w, w, h * 0.97),
                (cx + math.cos(a) * r_alt * 0.99,
                 cy + math.sin(a) * r_alt * 0.99, z0 + h * 0.5), col))
    return parts


def alem(name, cx, cy, z, col, scale=1.0):
    """**Âlem** — küre dizisi + hilâl. Düz bir çubuk değil."""
    parts = []
    s = scale
    parts.append(hz.make_tube(f"{name}_Mil", 0.09 * s, 0.07 * s, 1.5 * s,
                              (cx, cy), z, segments=6, col=col))
    for k, (rr, zz) in enumerate(((0.20, 0.35), (0.15, 0.85), (0.11, 1.25))):
        parts.append(hz.make_dome(f"{name}_Kure_{k}", rr * s, rr * s * 1.9,
                                  (cx, cy), z + zz * s, segments=8, rings=3,
                                  col=col))
    parts.append(hz.make_tube(f"{name}_Hilal", 0.05 * s, 0.02 * s, 0.9 * s,
                              (cx, cy), z + 1.5 * s, segments=6, col=col))
    return parts




def donuk_kutu(name, size, center, rot, col):
    """
    Döndürülmüş kutu — **merkezini kendi etrafında** döndürerek.

    `hz.make_box` köşe koordinatlarını doğrudan **mesh verisine** yazar ve
    nesne dönüşümünü kimlik bırakır (ölçek hatalarını önlemek için, bilinçli
    bir karar). Bunun sinsi sonucu şudur: kutuyu yerine koyup **sonra**
    `rotation_euler` vermek, onu kendi merkezi etrafında değil **dünya
    orijini** etrafında döndürür. Kule dibindeki bir konsol, orijinden 80 m
    uzaktaysa, 22,5°'lik bir dönüşle otuz metre öteye savrulur.

    Bu hata iki kez yazıldı (`konsol_dizisi`, `mukarnas_kavsara`) ve ikisi de
    **renderda görüldü ama yanlış teşhis edildi**: türbe duvarındaki beyaz
    benekler "mukarnas hücreleri fazla küçük" sanılmıştı; oysa hücreler
    yapının üstüne savrulmuş taçkapı parçalarıydı. Yedikule'nin ayak izi
    7×13 m büyüyünce sayı yalan söyleyemedi ve gerçek sebep çıktı.

    Doğru sıra: **orijinde kur → döndür → yerine taşı.**
    """
    b = hz.make_box(name, size, (0.0, 0.0, 0.0), col)
    b.rotation_euler = rot
    b.location = center
    return b

# ------------------------------------------------------------------- kemer

def kemer(name, cx, cy, ux, uy, half_span, spring_z, band_w, depth, col,
          steps=12, sivri=True):
    """
    **Tek sivri kemer** — voussoir bandı olarak.

    `street_kit.arch_points` şehrin bütün kemerlerini üreten eğridir (iki
    merkezli, kabarması `a·√(1+2c)`); burada o eğri boyunca küçük
    prizmalar dizilir ve **kemerin kendisi** kütle olur. Revakta kemer
    bir boşluk değil, bir **taşıyıcıdır**; onu çizmeden revak "kubbeleri
    havada duran bir duvar" gibi okunuyordu.

    `(ux, uy)` kemerin açıklık yönü; `depth` kemer bandının derinliği.
    """
    # arch_points IKI DEGER doner: (noktalar, kabarma). Tek degere
    # acmaya calismak "too many values to unpack" verir.
    #
    # `sivri=False` YUVARLAK kemer verir. Osmanli yapisi sivri kemerlidir
    # ama Galata Kulesi Cenevizlidir (1348) ve onun kemerleri yarim
    # dairedir; sehrin tek bir kemer dilini varsaymak, Galata'yi Osmanli
    # gostermek olurdu.
    if sivri:
        pts, _rise = sk.arch_points(half_span, spring_z, steps=steps)
    else:
        pts = [(half_span * math.cos(math.pi * i / steps),
                spring_z + half_span * math.sin(math.pi * i / steps))
               for i in range(steps + 1)]
    parts = []
    for i in range(len(pts) - 1):
        (u0, v0), (u1, v1) = pts[i], pts[i + 1]
        du, dv = u1 - u0, v1 - v0
        ln = math.hypot(du, dv)
        if ln < 1e-4:
            continue
        um, vm = (u0 + u1) * 0.5, (v0 + v1) * 0.5
        b = hz.make_box(f"{name}_{i}", (ln * 1.08, depth, band_w),
                        (0.0, 0.0, 0.0), col)
        b.rotation_euler = (0.0, -math.atan2(dv, du),
                            math.atan2(uy, ux))
        b.location = (cx + ux * um, cy + uy * um, vm)
        parts.append(b)
    return parts


def revak_sirasi(mats, col, name, x0, y0, x1, y1, n, z0, col_h, col_r,
                 dome=True, capital="mukarnas", spandrel_h=1.6,
                 bay=0.0, bay_dir=(0.0, 0.0), ends=(True, True)):
    """
    **Revak sırası** — sütun, kemer, alınlık, kubbe.

    Faz 3'ün avlularında revak yoktu: bir duvar ve üstünde düz kubbeler
    vardı. Oysa avluyu avlu yapan şey **kemer ritmidir** ve o ritim dört
    parçadan kurulur:

    1. `n+1` **sütun** (kaide + gövde + başlık),
    2. aralarında `n` **sivri kemer**,
    3. kemerlerin üstünde **alınlık** duvarı,
    4. her gözün üstünde bir **kubbe**.

    `bay` revak ile avlu duvarı arasındaki **göz derinliğidir**; `bay_dir`
    o gözün hangi yöne açıldığı. Sıfır bırakılırsa kubbeler sütun hattının
    üstünde kalır — ilk denemede öyle oldu ve kubbeler ince bir bandın
    üstünde asılı durdu. Bir revak gözü sütun ile duvar ARASINI örter;
    kubbenin merkezi o açıklığın ortasındadır, sütun hattı değil.

    `ends` sıranın **iki ucundaki** sütunun basılıp basılmayacağıdır.
    Kapalı bir revak halkasında köşeler sütunla değil **L kesitli köşe
    ayağıyla** taşınır; uçları kapatıp köşelere `kose_ayagi` koymak,
    Sultanahmet'in "yirmi altı sütun / otuz kubbeli birim" sayısını
    birebir verir (kapalı halkada göz sayısı = mesnet sayısı = 30;
    dördü köşe ayağı → 26 sütun). Kaynak kendi içinde tutarlıydı.

    Sayı kaynaktan gelir (Süleymaniye'nin avlusu, Sultanahmet'in yirmi
    altı sütun / otuz birimi, Fâtih'in on sekiz sütun / yirmi iki kubbesi)
    ve bu fonksiyon o sayıyı **geometriye** çevirir.
    """
    parts = []
    dx, dy = x1 - x0, y1 - y0
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    pitch = L / n

    for i in range(n + 1):
        if (i == 0 and not ends[0]) or (i == n and not ends[1]):
            continue
        t = i / n
        for o in sutun(f"{name}_Sutun{i}", x0 + dx * t, y0 + dy * t, z0,
                       col_h, col_r, col, capital=capital):
            parts.append(hz.assign(o, mats["marble"]))

    spring = z0 + col_h
    half = pitch * 0.5 * 0.92
    rise = half * math.sqrt(1.0 + 2.0 * sk.ARCH_C)
    for i in range(n):
        t = (i + 0.5) / n
        for o in kemer(f"{name}_Kemer{i}", x0 + dx * t, y0 + dy * t,
                       ux, uy, half, spring, 0.5, col_r * 3.2, col):
            parts.append(hz.assign(o, mats["cutstone"]))

    # ALINLIK: kemerlerin ustunu kapatan duvar.
    top = spring + rise + spandrel_h
    parts.append(hz.assign(
        _bant(f"{name}_Alinlik", x0, y0, x1, y1, ux, uy, col_r * 3.2,
              spring + rise, spandrel_h, col), mats["cutstone"]))

    # GOZ: sutun hatti ile avlu duvari arasi. Ortu ve kubbe buraya oturur.
    bx, by = bay_dir[0] * bay, bay_dir[1] * bay
    if bay > 0.01:
        parts.append(hz.assign(
            _bant(f"{name}_Ortu", x0 + bx * 0.5, y0 + by * 0.5,
                  x1 + bx * 0.5, y1 + by * 0.5, ux, uy, bay + col_r * 3.2,
                  top - 0.35, 0.35, col), mats["lead"]))

    if dome:
        dr = min(pitch, max(bay, col_r * 3.2 * 1.6)) * 0.46
        for i in range(n):
            t = (i + 0.5) / n
            cx = x0 + dx * t + bx * 0.5
            cy = y0 + dy * t + by * 0.5
            parts.append(hz.assign(
                hz.make_dome(f"{name}_Kubbe{i}", dr, dr * 0.78, (cx, cy),
                             top, segments=14, rings=5, col=col),
                mats["lead"]))
            for o in kubbe_kaburga(f"{name}_Dikis{i}", cx, cy, dr, top,
                                   dr * 0.78, col, n=10, w=0.09, steps=4):
                parts.append(hz.assign(o, mats["lead"]))
    return parts


def gozleri_dagit(toplam, kenarlar):
    """
    Sayılan göz toplamını kenarlara **en eşit** biçimde dağıtır.

    Kaynaklar avlu revakının toplam göz sayısını verir (Sultanahmet 30,
    Fâtih 22, Beyazıt 24) ama **hangi kenarda kaç tane** olduğunu değil.
    İlk kurulumda o dağılımı elle tahmin ettim (10/10/5/5) ve
    Sultanahmet'in ön gözü **13 m** genişliğinde çıktı — bir revak gözü
    değil, bir salon. Oysa varsayım gerektirmeyen bir dağılım var: gözler
    birbirine eşit olsun, yani her kenar **uzunluğu oranında** pay alsın.
    Aynı toplam 7/7/8/8'e düşüyor ve gözler 7,9–8,2 m ile neredeyse eşit.

    En büyük artık (largest remainder) yöntemi kullanılır; toplam korunur.
    """
    L = float(sum(kenarlar))
    ham = [toplam * k / L for k in kenarlar]
    pay = [max(1, int(h)) for h in ham]
    while sum(pay) < toplam:
        i = max(range(len(pay)), key=lambda j: ham[j] - pay[j])
        pay[i] += 1
    while sum(pay) > toplam:
        i = max(range(len(pay)), key=lambda j: pay[j] - ham[j])
        if pay[i] > 1:
            pay[i] -= 1
        else:
            break
    return pay


def konsol_dizisi(name, cx, cy, r, z, col, n=24, out=0.45, h=0.85):
    """
    **Konsol dizisi** — siperin altındaki taşıyıcı taş sırası.

    Ceneviz askerî mimarisinde siper duvarın düzleminden dışarı taşar ve
    o taşmayı bir konsol sırası taşır (makuliye/kirpi saçak). Uzaktan
    bakıldığında gövde ile siper arasında **kesintili bir gölge sırası**
    olarak okunur; bu sıra olmadan siper gövdeden büyümüş gibi görünür,
    yani kule bir boru olur.
    """
    out_ = []
    for i in range(n):
        a = 2.0 * math.pi * i / n
        # Uzun ekseni TEGET olmali (konsol cevre boyunca dizilir), bu
        # yuzden donus `a + pi/2`. Ve `donuk_kutu` ile: yerine koyup
        # sonra dondurmek onu dunya orijini etrafinda savurur.
        out_.append(donuk_kutu(
            f"{name}_{i}", (2.0 * math.pi * r / n * 0.55, out * 2.0, h),
            (cx + math.cos(a) * (r + out * 0.35),
             cy + math.sin(a) * (r + out * 0.35), z + h * 0.5),
            (0.0, 0.0, a + math.pi * 0.5), col))
    return out_


def revak_ust(L, n, col_h, spandrel_h=1.6):
    """
    Bir revak sırasının **örtü kotu** — duvarı ona göre kurmak için.

    Avlu duvarının yüksekliğini elle yazmak, revağın oranı her
    değiştiğinde duvarı yalan kılıyor: ilk kurulumda örtü duvarı
    **1,4 m aşmıştı** ve kubbeler havada yüzüyordu. Kot türetilmiş bir
    değerdir; türetildiği yerden okunmalı.
    """
    half = (L / n) * 0.5 * 0.92
    return col_h + half * math.sqrt(1.0 + 2.0 * sk.ARCH_C) + spandrel_h


def kose_ayagi(mats, col, name, cx, cy, z0, h, r):
    """
    **Köşe ayağı** — revak halkasının dönüş noktası.

    Sütun değildir: iki kolu iki revak koluna paralel giden L kesitli bir
    ayaktır ve kaynakların sütun sayımına girmez. Köşede yuvarlak bir
    sütun kullanmak iki kemerin de yükünü tek bir noktada toplar; Osmanlı
    avlusu bunu ayakla çözer.
    """
    w = r * 2.6
    out = []
    for j, (ox, oy, sx_, sy_) in enumerate(((0, 0, w, w * 0.42),
                                            (0, 0, w * 0.42, w))):
        out.append(hz.assign(hz.make_box(f"{name}_Kol{j}", (sx_, sy_, h),
                                         (cx + ox, cy + oy, z0 + h * 0.5),
                                         col), mats["marble"]))
    for o in silme_at(f"{name}_Baslik", cx, cy, w * 1.25, w * 1.25, z0 + h,
                      col, steps=2, h=0.42, out=0.16):
        out.append(hz.assign(o, mats["marble"]))
    return out


def _bant(name, x0, y0, x1, y1, ux, uy, depth, z, h, col):
    """Bir hat boyunca uzanan, doğrultusuna dönmüş kuşak."""
    L = math.hypot(x1 - x0, y1 - y0)
    b = hz.make_box(name, (L, depth, h),
                    (0.0, 0.0, 0.0), col)
    b.rotation_euler = (0.0, 0.0, math.atan2(uy, ux))
    b.location = ((x0 + x1) * 0.5, (y0 + y1) * 0.5, z + h * 0.5)
    return b

# ------------------------------------------------------------- kubbe ayrıntısı

def kubbe_kaburga(name, cx, cy, r, base_z, rise, col, n=24, w=0.16,
                  a0=0.0, a1=2.0 * math.pi, steps=7):
    """
    **Kurşun dilim dikişleri** — kubbenin üstündeki en görünür doku.

    Kurşun örtü levha levha döşenir ve dikişler meridyen boyunca kabarır.
    Uçuş oyununda kubbe **yukarıdan** görülür; dikişsiz bir kubbe plastik
    bir küre gibi okunur.
    """
    ## Dönüş bir kez ters çıktı ve render onu **kırık çizgiler** gibi
    ## gösterdi. Blender XYZ Euler'inde `(φ, 0, ψ)` yerel `+Y`'yi
    ## `(−sinψ·cosφ, cosψ·cosφ, sinφ)`'ye taşır; meridyen teğetine
    ## oturması için `ψ = a + π/2` ve `φ = atan2(Δz, Δyarıçap)` gerekir —
    ## Δyarıçap **pozitif** alınır (yukarı çıkarken yarıçap küçülür).
    ## İlk yazımda işaret tersti.
    parts = []
    for i in range(n):
        a = a0 + (a1 - a0) * i / n
        for k in range(steps):
            t0 = (math.pi * 0.5) * k / steps
            t1 = (math.pi * 0.5) * (k + 1) / steps
            rm = r * math.cos((t0 + t1) * 0.5)
            zm = base_z + rise * math.sin((t0 + t1) * 0.5)
            dr = r * math.cos(t0) - r * math.cos(t1)          # > 0
            dz = rise * (math.sin(t1) - math.sin(t0))         # > 0
            ln = math.hypot(dr, dz)
            b = hz.make_box(f"{name}_{i}_{k}", (w, ln * 1.06, w * 0.75),
                            (0.0, 0.0, 0.0), col)
            b.rotation_euler = (math.atan2(dz, dr), 0.0, a + math.pi * 0.5)
            b.location = (cx + math.cos(a) * rm * 1.004,
                          cy + math.sin(a) * rm * 1.004, zm)
            parts.append(b)
    return parts


# ------------------------------------------------------- pencere sıraları

def kemerli_pencere_sirasi(name, mats, col, wall_w, wall_h, wall_t,
                           origin, u_axis, n_axis, n, opening_w, sill_z,
                           spring_z, grille=True):
    """
    **Gerçek kemerli pencere sırası** — koyu dikdörtgen değil.

    Faz 3'ün camilerinde pencereler duvara yapıştırılmış koyu kutulardı:
    söve derinliği yok, kemer yok, şebeke yok. Oysa bir Osmanlı camisinin
    cephesini okutan şey **pencere ritmidir** ve o ritmi kuran şey
    kemerin eğrisi ile sövenin gölgesidir.

    `street_kit.arched_panel` bütün açıklıkların **aynı ölçüde** olmasını
    ister (T-kavşağı yok) — bir pencere sırası tam olarak odur: aynı
    açıklığın tekrarı.
    """
    spans = []
    pitch = wall_w / n
    for i in range(n):
        c = -wall_w * 0.5 + pitch * (i + 0.5)
        spans.append((c - opening_w * 0.5, c + opening_w * 0.5))
    panel = sk.arched_panel(name, wall_w, wall_h, wall_t, origin,
                            u_axis, n_axis, spans=spans, sill_z=sill_z,
                            spring_z=spring_z, col=col)
    out = [panel]
    if grille:
        for i in range(n):
            c = -wall_w * 0.5 + pitch * (i + 0.5)
            out += sk.iron_grille(f"{name}_Sebeke_{i}", opening_w * 0.86,
                                  (spring_z - sill_z) * 0.9, origin,
                                  u_axis, n_axis, c,
                                  sill_z + (spring_z - sill_z) * 0.5,
                                  wall_t, mats, col)
    return out


# ---------------------------------------------------------------- tackapi

#: Bir mukarnas hücresinin yaklaşık genişliği (m) — taş yontma ölçeği.
HUCRE_EN = 0.52


def mukarnas_kavsara(name, cx, cy, w, z, h, col, tiers=5, ters=False):
    """
    **Mukarnas kavsara** — taçkapı nişinin başındaki sarkıtlı yarım tonoz.

    `mukarnas` tam bir halka üretir (kaide, başlık, saçak altı için).
    Kavsara ise **yarım**dır: bir nişin içine, öne bakan yarım koni gibi
    oturur. Her sıra bir öncekinden küçük yarıçapla ve daha geride durur;
    hücreler sıradan sıraya kaydırılır — kaydırma olmadan sarkıtlar
    üst üste gelir ve mukarnas değil, merdiven okunur.

    Osmanlı camisini uzaktan tanıtan üç şeyden biri budur (diğer ikisi
    kubbe siluetı ve minare). Taçkapıyı iki kutuyla geçmek, camiyi
    kapısından tanınmaz kılıyordu.
    """
    out = []
    sy = -1.0 if not ters else 1.0
    for t in range(tiers):
        f = t / float(tiers)
        r = w * 0.5 * (1.0 - f * 0.62)
        zz = z + h * f
        hh = h / tiers
        # Hucre sayisi yapinin oranindan degil, HUCRENIN FIZIKSEL
        # BOYUTUNDAN turer: bir mukarnas hucresi taş yontma isidir ve
        # yaklasik yarim metredir — Babusselam'in 5,8 m'lik nisinde de,
        # turbenin 2,2 m'lik nisinde de. Sabit "7 hucre" yazinca genis
        # nislerde hucre 1,5 m'ye cikiyor ve kavsara sarkit degil DAMA
        # TAHTASI okunuyordu.
        n = max(3, min(18, int(round(math.pi * r / HUCRE_EN))))
        kay = 0.5 if (t % 2) else 0.0
        for i in range(n):
            a = math.pi * ((i + 0.5 + kay) / n - 0.5)
            bx = cx + math.sin(a) * r
            by = cy + sy * math.cos(a) * r * 0.62
            out.append(donuk_kutu(
                f"{name}_{t}_{i}", (r * 1.9 / n, hh * 0.78, hh * 0.92),
                (bx, by, zz + hh * 0.5), (0.0, 0.0, -a), col))
    return out


def tackapi(mats, col, name, cx, cy, z0, w, h, jut, kapi_w=2.6, kapi_h=4.2,
            kitabe=True, sutunce=True, mihrabiye=True):
    """
    **Taçkapı** — cepheden taşan ve YÜKSELEN anıtsal giriş.

    Sekiz parçadan kurulur ve hiçbiri süs değildir:

    1. iki **yan ayak** ile üstteki **alınlık** — aralarında gerçek boşluk
       (gölge kutusuyla sahte delik açmıyoruz; niş her açıdan okunmalı),
    2. nişin ağzını çeviren **sivri kemer**,
    3. niş başındaki **mukarnas kavsara**,
    4. kapı **söveleri** ve düz **lento**,
    5. lentonun üstünde **kitabe** panosu,
    6. ön köşelerde **sütunçeler**,
    7. bloğu taçlandıran **silme**,
    8. yanlarda birer **mihrabiye** nişi.

    `cy` duvar yüzüdür; blok oradan `jut` kadar **−Y**'ye taşar (ön yön).
    """
    out = []
    nw = w * 0.52                      # nis agzi
    nis_z = h * 0.74                   # nis kemerinin basma kotu
    yf = cy - jut                      # blogun on yuzu
    ym = (cy + yf) * 0.5

    for sx in (-1, 1):                 # 1) yan ayaklar
        pw = (w - nw) * 0.5
        out.append(hz.assign(hz.make_box(
            f"{name}_Ayak{sx}", (pw, jut, h),
            (cx + sx * (nw + pw) * 0.5, ym, z0 + h * 0.5), col),
            mats["marble"]))
    ust_h = h - nis_z
    out.append(hz.assign(hz.make_box(
        f"{name}_Alinlik", (w, jut, ust_h * 0.42),
        (cx, ym, z0 + h - ust_h * 0.21), col), mats["marble"]))

    # 2) nis agzini ceviren sivri kemer
    for o in kemer(f"{name}_NisKemer", cx, yf + 0.12, 1.0, 0.0,
                   nw * 0.5, z0 + nis_z, 0.42, 0.24, col):
        out.append(hz.assign(o, mats["marble"]))

    # 3) kavsara — nisin ARKA yarisinda, kemerin gerisinde
    out += [hz.assign(o, mats["marble"]) for o in mukarnas_kavsara(
        f"{name}_Kavsara", cx, cy - jut * 0.30, nw * 0.92, z0 + nis_z,
        ust_h * 0.72, col)]

    # 4) kapi soveleri + lento (nisin dibinde, DUVAR duzleminde)
    for sx in (-1, 1):
        jw = (nw - kapi_w) * 0.5
        out.append(hz.assign(hz.make_box(
            f"{name}_Sove{sx}", (jw, jut * 0.55, kapi_h),
            (cx + sx * (kapi_w + jw) * 0.5, cy - jut * 0.28,
             z0 + kapi_h * 0.5), col),
            mats["marble"]))
    out.append(hz.assign(hz.make_box(
        f"{name}_Lento", (nw, jut * 0.55, 0.55),
        (cx, cy - jut * 0.28, z0 + kapi_h + 0.28), col), mats["marble"]))
    out.append(hz.assign(hz.make_box(
        f"{name}_KapiBosluk", (kapi_w, 0.5, kapi_h),
        (cx, cy - 0.05, z0 + kapi_h * 0.5), col), mats["shadow"]))

    # 5) kitabe
    if kitabe:
        out.append(hz.assign(hz.make_box(
            f"{name}_Kitabe", (kapi_w * 1.35, jut * 0.62, 0.95),
            (cx, cy - jut * 0.31, z0 + kapi_h + 1.25), col),
            mats["marble"]))

    # 6) on kose sutunceleri
    if sutunce:
        for sx in (-1, 1):
            out += [hz.assign(o, mats["marble"]) for o in sutun(
                f"{name}_Sutunce{sx}", cx + sx * (w * 0.5 - 0.30),
                yf + 0.22, z0, h * 0.80, 0.22, col,
                capital="mukarnas", segments=8)]

    # 7) silme: blogun tepesi
    for o in silme_at(f"{name}_Silme", cx, ym, w * 1.06, jut * 1.16,
                      z0 + h, col, steps=3, h=0.55, out=0.30):
        out.append(hz.assign(o, mats["marble"]))

    # 8) mihrabiye: nisin iki yaninda kucuk kavsarali nis. Kucuk kapilarda
    # kapatilabilir.
    #
    # TESHIS DUZELTMESI: bir ara bu bayragi "mukarnas hucreleri kucuk
    # olcekte gurultu okuyor" diye kapatmistim. Yanlisti. Renderdaki beyaz
    # benekler `mukarnas_kavsara`nin dunya orijini etrafinda savrulmasindan
    # geliyordu (bkz. `donuk_kutu`); dönüş duzeltilince benekler mihrabiye
    # ACIKKEN de kayboldu. Bir kusuru gordugunde neyi degistirdigini degil,
    # neyin ölçüldügünü sor.
    if not mihrabiye:
        return out
    for sx in (-1, 1):
        mx = cx + sx * (nw * 0.5 + (w - nw) * 0.25)
        out.append(hz.assign(hz.make_box(
            f"{name}_Mihrabiye{sx}", (1.5, 0.45, 3.0),
            (mx, yf + 0.22, z0 + 2.1), col), mats["shadow"]))
        out += [hz.assign(o, mats["marble"]) for o in mukarnas_kavsara(
            f"{name}_MihKavsara{sx}", mx, yf + 0.30, 1.4, z0 + 3.6, 1.1,
            col,
            tiers=3)]
    return out

# ------------------------------------------ cephe ve kabuk (ayrıntılı)

def cephe(mats, col, name, w, h, t, origin, u_axis, n_axis, rows,
           grille=True):
    """
    Bir cephe — **gerçek kemerli pencere sıralarıyla**.

    Faz 3'ün camilerinde gövde tek bir kutuydu ve pencereler ona
    yapıştırılmış koyu dikdörtgenlerdi: söve derinliği yok, kemer yok,
    şebeke yok. Bir Osmanlı camisinin cephesini okutan şey **pencere
    ritmidir**; onu kuran şey kemerin eğrisi ile sövenin gölgesidir.

    `rows` = [(pencere sayısı, açıklık genişliği, kat yüksekliği), …];
    katlar alttan üste dizilir ve aralarına **silme** girer.

    `street_kit.arched_panel` bütün açıklıkların **aynı** ölçüde olmasını
    ister (T-kavşağı yok) — bir pencere sırası tam olarak odur: aynı
    açıklığın tekrarı. Farklı ölçü isteyen kat **ayrı panel** olur ve
    zaten öyle kurulur.
    """
    out = []
    ox, oy, oz = origin
    z = 0.0
    for k, (n, opening, floor_h) in enumerate(rows):
        sill = floor_h * 0.30
        spring = floor_h * 0.62
        spans = []
        pitch = w / n
        for i in range(n):
            c = -w * 0.5 + pitch * (i + 0.5)
            spans.append((c - opening * 0.5, c + opening * 0.5))
        panel = sk.arched_panel(f"{name}_Kat{k}", w, floor_h, t,
                               (ox, oy, oz + z), u_axis, n_axis,
                               spans=spans, sill_z=sill, spring_z=spring,
                               col=col)
        out.append(hz.assign(panel, mats["cutstone"]))
        if grille:
            for i in range(n):
                c = -w * 0.5 + pitch * (i + 0.5)
                for g in sk.iron_grille(f"{name}_Sebeke{k}_{i}",
                                        opening * 0.86,
                                        (spring - sill) * 0.85,
                                        (ox, oy, oz + z), u_axis, n_axis,
                                        c, sill + (spring - sill) * 0.5,
                                        t, mats, col):
                    out.append(g)
        z += floor_h
        # KAT ARASI SILME: cephenin yatay okumasi bundan cikar.
        if k < len(rows) - 1:
            band = hz.make_box(f"{name}_Silme{k}",
                               (w if abs(u_axis[0]) > 0.5 else t + 0.5,
                                t + 0.5 if abs(u_axis[0]) > 0.5 else w,
                                0.34),
                               (ox, oy, oz + z + 0.17), col)
            out.append(hz.assign(band, mats["cutstone"]))
    return out, z


def kabuk(mats, col, name, W, D, t, z0, rows, cy=0.0, grille=True):
    """
    Harimin dört cephesi — **kutu değil, kabuk**.

    Dört panel köşelerde üst üste biner ve kutu kapanır; her panelin
    kendi pencere sırası vardır. Toplam yükseklik `rows`'tan **türer**,
    elle verilmez: cephe kaç katsa duvar o kadar yüksektir.
    """
    out = []
    sides = (
        (f"{name}_On",   W, (0.0, cy - D * 0.5, z0), (1.0, 0.0), (0.0, -1.0)),
        (f"{name}_Arka", W, (0.0, cy + D * 0.5, z0), (-1.0, 0.0), (0.0, 1.0)),
        (f"{name}_Sol",  D, (-W * 0.5, cy, z0), (0.0, -1.0), (-1.0, 0.0)),
        (f"{name}_Sag",  D, (W * 0.5, cy, z0), (0.0, 1.0), (1.0, 0.0)),
    )
    total = 0.0
    for nm, ww, org, u, n in sides:
        parts, total = cephe(mats, col, nm, ww, 0.0, t, org, u, n, rows,
                              grille)
        out += parts
    # KOSE PAYELERI: dort panelin bulustugu yeri kapatir ve kutleye
    # kose cizgisi verir.
    for sx in (-1, 1):
        for sy in (-1, 1):
            out.append(hz.assign(
                hz.make_box(f"{name}_Kose_{sx}{sy}", (t * 1.6, t * 1.6, total),
                            (sx * W * 0.5, cy + sy * D * 0.5,
                             z0 + total * 0.5), col), mats["cutstone"]))
    return out, total
