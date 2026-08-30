"""
Hezarfen: 1632 — Kıyafet kiti (Faz 5).

## Yöntem: giysi gövdeden TÜRER

Giysiyi elden modellemek yerine **gövdenin kendisinden** kabuk çıkarıyorum:
ilgili bölgenin yüzleri kopyalanır, normalleri boyunca dışarı itilir,
kalınlık verilir. Sebep tembellik değil — bu yolla giysinin oturması bir
göz kararı değil, **yapısal bir garanti**dir. Elden modellenen bir entari
karakter pozunu değiştirince gövdeye batar; kabuk batmaz, çünkü zaten o
gövdenin ofsetidir.

Ayrıca bu, projenin geri kalanıyla aynı ilke: sayı elle yazılmaz, ölçülen
şeyden türer.

## Kaynak

Biçim Rålamb kıyafet albümünden (1657–58, kamu malı) **okunan** dilbilgisine
dayanır — plakalar `refs/ralamb/`, okunan kurallar `docs/RESEARCH.md`.
Minyatür kopyalanmaz; okunan şey oranlardır:

- entarinin boyu işi söyler (oturan ayak bileğine, çalışan baldıra),
- kuşak doğal belde ve dar,
- şalvar entarinin altından **görünür**,
- kol ağzı astarı ters çevrilir,
- dizlik gerçek bir öğedir (plaka 50),
- başlık rütbe göstergesi: Hezarfen ne paşa ne asker → orta hacim.

**T2, taslak.** Albüm 1632'den 25 yıl sonra; ana hatlar değişmedi ama
ayrıntı değişebilir ve tam 1632'nin kaynağı yok.
"""

import math

import bmesh
import bpy
from mathutils import Vector

import hz_blender as hz

#: Katman kalınlıkları (m) — kumaş gerçekten incedir.
GOMLEK_KAL = 0.004
ENTARI_KAL = 0.006
KUSAK_KAL = 0.010

#: Gövde oranları (boya göre). Rålamb plakalarından okundu.
BEL_ORAN = 0.60          #: kuşağın oturduğu kot (doğal bel)
KALCA_ORAN = 0.52        #: eteğin başladığı kot
DIZ_ORAN = 0.285         #: dizlik bandı
#: Kısa entarinin eteği — **dizin hemen ÜSTÜ**, baldır değil.
#: İlk yazımda 0,22 (baldır ortası) yazmıştım ve etek dizi örtüyordu; o
#: yüzden dizlik üretiliyor ama hiç görünmüyordu. Plaka 50'de kaftan dizde
#: biter ve siyah dizlik bandı tam altında durur — bant görünmüyorsa
#: bandın anlamı da yoktur.
BALDIR_ORAN = 0.315
BILEK_ORAN = 0.055       #: uzun entarinin eteği (oturan adam)


def kopya_kabuk(govde, ad, col, tut, sisme, kalinlik):
    """`tut(v_dunya) -> bool` ile seçilen bölgenin dış kabuğu.

    Vertex normalleri boyunca `sisme` kadar dışarı itilir, sonra
    `kalinlik` kadar katılaştırılır. `sisme` giysinin ne kadar bol
    olduğudur; kumaş kalınlığı ayrı bir sayıdır ve karıştırılmamalıdır.
    """
    me = govde.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()

    at = [v for v in bm.verts if not tut(v.co)]
    if at:
        bmesh.ops.delete(bm, geom=at, context="VERTS")
    if not bm.faces:
        bm.free()
        return None

    bm.normal_update()
    for v in bm.verts:
        n = v.normal.copy()
        if n.length < 1e-6:
            continue
        s = sisme(v.co) if callable(sisme) else sisme
        v.co += n.normalized() * s

    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik
    m.offset = 1.0
    _uygula(obj)
    return obj


def _uygula(obj):
    """Modifier yığınını ağa yazar (kalıcı çıktı kuralı)."""
    dg = bpy.context.evaluated_depsgraph_get()
    yeni = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    eski = obj.data
    obj.modifiers.clear()
    obj.data = yeni
    obj.data.name = obj.name
    bpy.data.meshes.remove(eski)
    return obj


def yumusat(obj, tekrar=4, carpan=1.0):
    """Kabuğun üstündeki **anatomiyi** siler; siluetini bırakır.

    `kopya_kabuk` giysiyi gövdenin ofseti olarak üretir ve bu, oturma
    garantisini verir (bkz. modül başlığı). Ama bir bedeli var: giysi
    gövdenin her ayrıntısını da kopyalar. İnceleme paketi v8'de görülen
    buydu — entarinin üstünde göğüs kasları, mestin üstünde **ayak
    parmakları**. Mest yumuşak deri bir çizmedir; parmağı yoktur.

    Kumaş ve deri, altındaki yüzeyi ortalar. Modelde bunun karşılığı
    komşu köşelerin ortalanmasıdır: siluet kalır, ayrıntı gider.
    """
    m = obj.modifiers.new("yumusat", "SMOOTH")
    m.iterations = int(tekrar)
    m.factor = float(carpan)
    return _uygula(obj)


def sadelestir(obj, oran):
    """Üçgen sayısını `oran` katına indirir (0..1). Ölçülerek kullanılır."""
    if oran >= 0.999:
        return obj
    m = obj.modifiers.new("sadelestir", "DECIMATE")
    m.decimate_type = "COLLAPSE"
    m.ratio = float(oran)
    return _uygula(obj)


def kol_siniri(govde, z):
    """Bu kotta gövde ile kolu ayıran |x| eşiğini **ölçer**.

    Kolları elle bir sayıyla ayırmak kırılgan olurdu (gövdeye göre
    değişir). Onun yerine: o kottaki noktaların |x| değerleri sıralanır ve
    **en büyük boşluk** aranır. İnsan gövdesiyle kolu arasında gerçek bir
    boşluk vardır — koltuk altı. Ölçüm o boşluğu bulur.
    """
    mn, mx = hz.bounds(govde)
    kal = (mx[2] - mn[2]) * 0.01
    xs = sorted(abs(v.co.x) for v in govde.data.vertices
                if abs(v.co.z - z) < kal)
    if len(xs) < 8:
        return None
    en_buyuk, esik = 0.0, None
    for a, b in zip(xs, xs[1:]):
        if b - a > en_buyuk:
            en_buyuk, esik = b - a, (a + b) * 0.5
    # Bosluk anlamli degilse (gomlek gibi kollar govdeye yapisik) None.
    return esik if en_buyuk > (mx[0] - mn[0]) * 0.03 else None


def kol_ayirici(govde):
    """Kolu **hem gövdeden hem bacaktan** ayıran iki sayı: `(esik_x, z_alt)`.

    ## Neden tek bir |x| eşiği yetmiyor

    `kol_siniri` tek bir kotta koltuk altı boşluğunu arar. Bu, kolların o
    kottan geçtiği bir duruşta çalışır. Yeni taban gövdede çalışmadı:
    kalça kotunda (0,884 m) kol **yok** — eller 0,90 m'de bitiyor — ve
    ölçüm dürüstçe `None` döndü. Sonra elle yazılmış yedek (boyun %11'i =
    0,187 m) devreye girdi. Ölçülen |x| profili şunu söylüyor:

    | kot | |x| en büyük | ne |
    |---|---|---|
    | 0,0–0,5 | 0,217–0,240 | **bacak** |
    | 0,5–0,9 | 0,186–0,193 | gövde |
    | 0,9–1,2 | 0,442–0,538 | **kol** |

    Bacak, herhangi bir "kol eşiği"nden daha dışarıda. Yani |x| tek başına
    kolu ayıramaz — ayıramadığı için entari kolu baldırı sardı, şalvar
    baldırın dışını açıkta bıraktı ve "parmak ucu" diye ölçülen kot
    (0,074 m) aslında ayak bileğiydi. Üç kusur, tek yanlış cetvel.

    ## Eşik: "en büyük boşluk" değil, gövdenin kendi genişliği

    İlk yazımda eşiği "her kottaki en büyük boşluğun ortası"nın en küçüğü
    diye aradım ve **0,105 m** çıktı — koltuk altı değil, boyun hizasında
    rastgele bir aralık. En büyük boşluk her zaman koltuk altı değildir.

    Doğrusu ters yönden bakmak: orta çizgiden **dışarı yürü**, ilk gerçek
    boşluğa kadar gelen küme gövdedir. Gövde yüzeyindeki örnek aralığı
    ≤ 1,2 cm, koltuk altı boşluğu 12–26 cm — arada belirsizlik yok.
    Eşik, kotlar boyunca ölçülen en geniş gövde yarısının biraz dışıdır.

    ## z_alt: bacak nerede biter, kol nerede başlar

    Eşiği aşan köşelerin z değerlerinde bir **boş kuşak** vardır (burada
    0,5–0,9 m): altı bacak, üstü kol. `z_alt` o kuşağın ortasıdır.
    """
    mn, mx = hz.bounds(govde)
    boy = mx[2] - mn[2]
    vs = [v.co for v in govde.data.vertices]

    #: Gerçek boşluk sayılan en küçük aralık. Gövde örnek aralığı bunun
    #: çok altında, koltuk altı çok üstünde.
    BOSLUK = boy * 0.02

    # Yalniz KOLUN AYRI DURDUGU kotlar sayilir. Omuz basinda kol govdeye
    # anatomik olarak kaynar; orada "govde yarisi" diye olculen sey kolun
    # kendisidir ve butun kotlarin en genisini alinca esik 0,322 m'ye
    # firladi — entari govdeyi kaybederdi. Boslugun bulundugu kotta ise
    # ayrim gercektir.
    genisler = []
    adim = boy * 0.012
    z = mn[2]
    while z < mx[2]:
        xs = sorted(abs(v.x) for v in vs if z <= v.z < z + adim)
        z += adim
        if len(xs) < 8:
            continue
        govde_yari, ayrik = xs[0], False
        for a, b in zip(xs, xs[1:]):
            if b - a > BOSLUK:
                ayrik = True
                break
            govde_yari = b
        if ayrik:
            genisler.append(govde_yari)
    if len(genisler) < 3:
        return None, None
    esik = max(genisler) + boy * 0.004

    zs = sorted(v.z for v in vs if abs(v.x) >= esik)
    if len(zs) < 8:
        return esik, None
    eb, z_alt = 0.0, None
    for a, b in zip(zs, zs[1:]):
        if b - a > eb:
            eb, z_alt = b - a, (a + b) * 0.5
    # Bosluk anlamsizsa (bacak ile kol bitisik) ayirma yapilmaz.
    if eb < boy * 0.03:
        z_alt = None
    return esik, z_alt


def kesit(govde, z, kalinlik=0.02, x_esik=None, dislama=None):
    """Bu kottaki gövde kesitinin (yarı-genişlik x, yarı-derinlik y) ölçüsü.

    `dislama(co) -> bool` verilirse o köşeler sayılmaz — **kolları
    dışarıda bırakmanın doğru yolu budur**.

    ## İki kez yanlış ölçüldü, iki farklı yönde

    Önce hiç dışlama yoktu: A-pozunda kollar kalça ve bel hizasından
    geçer, yani "bel kesiti" diye ölçtüğüm şey kolun x açıklığıydı. Kuşak
    0,84 m çapında çıktı — hula hoop.

    Sonra `x_esik` eklendi ve ters yöne kaçtı. Yeni taban gövdede kol
    eşiği 0,173 m; kalça ise 0,20 m. Kalça kotunda **kol yok**, ama eşik
    kalçanın kendisini kırptı: etek gerçek kalçadan dar üretildi ve
    altındaki kırmızı şalvar eteğin içinden lekeler hâlinde dışarı
    vurdu — inceleme paketi v4'te görünen buydu.

    Bir eşik "kol nerede" sorusunu cevaplayamaz; cevabı iki sayı verir
    (`kol_ayirici`). O yüzden burada eşik değil **yüklem** alınır.
    `x_esik` geriye dönük uyumluluk için duruyor.
    """
    def at(co):
        if dislama is not None:
            return dislama(co)
        return x_esik is not None and abs(co.x) >= x_esik

    vs = [v.co for v in govde.data.vertices
          if abs(v.co.z - z) < kalinlik and not at(v.co)]
    if len(vs) < 4:
        return None
    return (max(abs(v.x) for v in vs), max(abs(v.y) for v in vs))


def kesit_merkezli(govde, z, kalinlik=0.02, dislama=None):
    """Kesitin **kendi merkezine göre** ölçüsü: `(rx, ry, cy)`.

    ## Gövdenin y ekseni ortalanmış değil

    `kesit` yarı-ölçüyü `max(|y|)` diye verir; bu ancak gövde y=0'da
    ortalıysa doğrudur ve **değil**. Ölçüldü: kalça kotunda kaba et
    y = +0,228, karın y ≈ 0 — yani yerel y ekseni gövdenin ortasından
    değil ÖNÜNDEN geçiyor. `max(|y|)` bu durumda gövdenin derinliğini
    değil, eksenin ne kadar kenarda olduğunu ölçer.

    Bedeli ölçüldü: etek konisi (0,0)'a göre kuruluyordu, o yüzden kaba
    eti içine alabilmek için yarıçapı 0,71 m'ye çıkması gerekiyordu —
    1,4 m çapında bir etek. Aynı kayma kuşağı da gövdenin bir yanına
    itiyordu.

    Doğrusu merkezi de ölçmektir: `cy` kesitin y ortasıdır, `ry` o
    merkeze göre yarı derinliktir.
    """
    vs = [v.co for v in govde.data.vertices
          if abs(v.co.z - z) < kalinlik
          and (dislama is None or not dislama(v.co))]
    if len(vs) < 4:
        return None
    y0, y1 = min(v.y for v in vs), max(v.y for v in vs)
    return (max(abs(v.x) for v in vs), (y1 - y0) * 0.5, (y0 + y1) * 0.5)


def bacak_kesit(govde, z, sx, kalinlik=0.02):
    """Tek bacağın bu kottaki merkezi ve yarıçapı: `(cx, rx, ry)`.

    Dizliği gövdenin tümünden ölçemezdim: iki bacak var ve ortak kesit
    ikisinin arasındaki boşluğu da sayardı.
    """
    vs = [v.co for v in govde.data.vertices
          if abs(v.co.z - z) < kalinlik and (v.co.x * sx) > 0.0]
    if len(vs) < 4:
        return None
    x0, x1 = min(v.x for v in vs), max(v.x for v in vs)
    y0, y1 = min(v.y for v in vs), max(v.y for v in vs)
    return ((x0 + x1) * 0.5, (x1 - x0) * 0.5, (y1 - y0) * 0.5)


def etek(ad, col, z_ust, z_alt, r_ust, r_alt, kalinlik, segment=32,
         yarik=False, cy=0.0, cy_alt=None):
    """Belden aşağı **serbest** düşen etek — bacakları takip etmez.

    Entarinin eteği gövdeye yapışmaz, konidir. Kabuk yöntemiyle üretseydim
    etek iki bacağa ayrılırdı ve yürürken pantolon gibi davranırdı; oysa
    Rålamb plakalarında etek tek parçadır ve altından şalvar görünür.

    `yarik`: önde açıklık (binme ve yürüme için). Plaka 20'de entari önden
    açıktır ve altındaki koyu iç entari görünür.
    """
    bm = bmesh.new()
    halkalar = []
    cy_a = cy if cy_alt is None else cy_alt
    for t in (0.0, 1.0):
        z = z_ust + (z_alt - z_ust) * t
        rx = r_ust[0] + (r_alt[0] - r_ust[0]) * t
        ry = r_ust[1] + (r_alt[1] - r_ust[1]) * t
        cy_t = cy + (cy_a - cy) * t
        halka = []
        for i in range(segment):
            a = 2.0 * math.pi * i / segment
            # Yarik: on tarafta (-y) dar bir dilim atlanir.
            halka.append(bm.verts.new(
                (math.cos(a) * rx, cy_t + math.sin(a) * ry, z)))
        halkalar.append(halka)

    ust, alt = halkalar
    n = segment
    on = int(round(n * 0.75))          # -y yonundeki dilim
    for i in range(n):
        j = (i + 1) % n
        # Yarik TEK dilim: 32 segmentte ~11 derece. Ilk yazimda iki
        # dilimdi ve etek iki ayri panel gibi acildi.
        if yarik and i == on:
            continue
        bm.faces.new((ust[i], ust[j], alt[j], alt[i]))
    bm.normal_update()

    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik
    m.offset = 0.0
    return _uygula(obj)


def alt_zarf(govde, z_alt, z_ust, sisme, dislama=None, adim=0.02):
    """Eteğin altında kalan her şeyin kot-kot **kutusu**: `[(z, rx, y0, y1)]`.

    `rx` x yarı-genişliğidir (x ekseni gövdede gerçekten ortalıdır),
    `y0..y1` ise y aralığının kendisidir — yarı-ölçü DEĞİL. Gerekçe
    `kesit_merkezli`'de: gövdenin yerel y ekseni ortalanmış değil, o
    yüzden y'de "yarıçap" ancak merkeziyle birlikte anlamlıdır.

    `sisme(co) -> m` alt katmanın (şalvar) o kottaki şişmesidir.
    """
    vs = [v.co for v in govde.data.vertices]
    cikti = []
    z = z_alt
    while z <= z_ust:
        s = [c for c in vs if abs(c.z - z) < adim
             and (dislama is None or not dislama(c))]
        if len(s) >= 4:
            ek = sisme(_Kot(z)) if callable(sisme) else float(sisme)
            cikti.append((z,
                          max(abs(c.x) for c in s) + ek,
                          min(c.y for c in s) - ek,
                          max(c.y for c in s) + ek))
        z += adim
    return cikti


class _Kot:
    """`sisme` geri çağrısı bir köşe bekler; burada yalnız `z` gerekli."""

    __slots__ = ("x", "y", "z")

    def __init__(self, z):
        self.x = self.y = 0.0
        self.z = z


def etek_acikligi(r_ust, cy_ust, z_ust, z_alt, zarf, pay, acilma):
    """Koninin iki ucu: `(r_ust, r_alt, cy_ust, cy_alt)` — zarfı her kotta
    içine alan en küçük etek.

    ## Neden hesaplanıyor, neden bir çarpan değil

    Etek gövdeden türemez — serbest düşen bir konidir. Bu iyi bir seçim
    (kumaş bacağı takip etmez) ama bedeli var: **koninin gövdeyi içerdiği
    garanti değildir.** İnceleme paketi v4/v5'te bunun ne demek olduğu
    görüldü: bacaklar açık durduğu için (|x| 0,23 m) şalvar eteğin dışına
    taştı ve teal eteğin üstünde 478 yüzlük kırmızı mercek lekeleri
    belirdi. Elle bir "açılma çarpanı" büyütmek bunu tesadüfen kapatır,
    bir sonraki gövdede yine açar.

    ## Üst uç da ölçülür — yoksa çadır çıkar

    İlk çözümde yalnız **alt** ucu hesaplıyordum ve sonuç 0,59 m
    yarıçaptı: 1,2 m çapında bir etek. Sebebi yapısaldı — eteğin tepesi
    beldedir, gövdenin en geniş yeri ise belin hemen ALTINDA (kalça).
    Doğrusal bir koni, tepesinden 5 cm aşağıdaki bir taşmayı ancak
    tabanını devasa açarak karşılayabilir; küçük `t`'de payda küçüktür,
    gereken taban patlar.

    Doğrusu: kalça da eteğin tepesine dahildir. Üst uç, tepenin ilk
    beşte birindeki (`t <= UST_PAY`) örnekleri karşılayacak kadar geniş
    alınır; kalan örnekler tabanı belirler. Kuşak zaten belde bağlandığı
    için gözde bel yine incedir — kumaşın kalçayı örtmesi doğru olandır.

    `acilma` tarihsel açılma çarpanıdır ve **taban** olarak korunur;
    hesap yalnız yükseltebilir.
    """
    #: Koninin "tepe" sayilan boyu. Kalca belin ~%8'i altindadir; bes
    #: kat pay birakiyoruz ki olcum bir kotun tam ustune dusmesin.
    UST_PAY = 0.20

    if not zarf:
        return (r_ust, (r_ust[0] * acilma, r_ust[1] * acilma),
                cy_ust, cy_ust)

    z_en_alt = min(z for z, _, _, _ in zarf)
    alt_ornek = [o for o in zarf if abs(o[0] - z_en_alt) < 1e-6][0]
    cy_alt = (alt_ornek[2] + alt_ornek[3]) * 0.5

    ornek = []
    for z, rx, y0, y1 in zarf:
        if not (min(z_alt, z_ust) <= z <= max(z_alt, z_ust)):
            continue
        t = (z_ust - z) / max(1e-6, z_ust - z_alt)
        ornek.append((t, rx + pay, y0 - pay, y1 + pay))

    # --- 1. TEPE: ilk UST_PAY'daki ornekler tepeyi genisletir ----------
    ux, uy = r_ust[0], r_ust[1]
    for t, rx, y0, y1 in ornek:
        if t > UST_PAY:
            continue
        cy = cy_ust + (cy_alt - cy_ust) * t
        ux = max(ux, rx)
        uy = max(uy, max(y1 - cy, cy - y0))

    # --- 2. TABAN: kalan ornekler ------------------------------------
    ax, ay = ux * acilma, uy * acilma
    for t, rx, y0, y1 in ornek:
        if t <= UST_PAY:
            continue
        cy = cy_ust + (cy_alt - cy_ust) * t
        gerek_y = max(y1 - cy, cy - y0)
        ax = max(ax, ux + (rx - ux) / t)
        ay = max(ay, uy + (gerek_y - uy) / t)
    return ((ux, uy), (ax, ay), cy_ust, cy_alt)


def band(ad, col, z, r, yukseklik, kalinlik, segment=20, fici=0.35, cy=0.0):
    """Kuşak / dizlik / bilezik — gövdeyi saran dar bir kuşak.

    `fici`: kuşağın ortasının kenarlarına göre ne kadar şiştiği (kalınlık
    katı olarak). **0 düz silindir demektir ve düz silindir yanlıştır.**

    İlk yazım düz bir silindirdi. Sonuç inceleme paketinde görüldü: kuşak
    giysiden birkaç santim açıkta duran, üstten bakınca **içine
    bakılabilen** bir kova gibiydi — kumaş değil, fıçı çemberi. Kuşak
    sarılarak bağlanır; ortası şişer, kenarları giysiye yatar. Üç halka
    (alt–orta–üst) bunu verir ve aynı zamanda halka boşluğunu kapatır:
    kenarlar giysiye değdiği için bakılacak bir aralık kalmaz.
    """
    bm = bmesh.new()
    kotlar = ((z - yukseklik * 0.5, 0.0),
              (z, 1.0),
              (z + yukseklik * 0.5, 0.0))
    halkalar = []
    for zz, t in kotlar:
        pay = kalinlik + fici * kalinlik * t * 3.0
        halkalar.append([bm.verts.new(
            (math.cos(2 * math.pi * i / segment) * (r[0] + pay),
             cy + math.sin(2 * math.pi * i / segment) * (r[1] + pay), zz))
            for i in range(segment)])
    for a, b in zip(halkalar, halkalar[1:]):
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik * 1.6
    m.offset = 0.0
    return _uygula(obj)


def sarik(ad, col, z_taban, z_tepe, r, sarim=7, kalinlik=0.034,
          segment=20):
    """Sarık: kavuk çekirdeğinin üstüne sarılan bez.

    Kotlar **açıkça** verilir. İlk yazımda merkez + yarıçaptan türetiyordum
    ve sarık kafanın üstüne değil yüzüne oturdu: tepesi 1,677 m, oysa başın
    tepesi 1,700 m. Sarık başlıktır; başın altında kalamaz.

    Sarığı sarık yapan şey hacim değil o yatay çizgilerdir — plaka 35 ve
    50'de sarımlar açıkça sayılabiliyor. Hacim Hezarfen için ORTA: plaka
    20'nin sivil sarığı büyük, plaka 50'nin asker sarığı sıkı; o ne biri
    ne öbürü.

    ## Ayrı bantlar değil, TEK yüzey

    Sarımları yedi ayrı halka bandı olarak kurmuştum. İnceleme paketinde
    ne olduğu görüldü: bantların arası açıktı ve üstte kapak yoktu — üstten
    bakınca sarığın **içi** görünüyordu. Başın üstünde bir yay gibi duran
    beş beyaz tabak.

    Doğrusu tek bir dönel yüzeydir: yarıçap profili yukarı doğru daralır ve
    tepede kapanır, sarımlar o profile binen bir **dalgadır**. Böylece
    çizgiler kalır, boşluk kalmaz. Sarma çizgisi geometriden değil
    dalgadan gelir; bez zaten sürekli bir şeydir.
    """
    bm = bmesh.new()
    dilim = max(8, sarim * 4)
    yuk = z_tepe - z_taban
    halkalar = []
    for k in range(dilim + 1):
        t = k / float(dilim)
        # Ana profil: ortada genis, iki ucta dar — sarik bir ficidir.
        taban = 0.74 + 0.50 * math.sin(math.pi * min(1.0, t * 0.94 + 0.03))
        # Sarim dalgasi: gorunen yatay cizgiler.
        dalga = 1.0 + 0.055 * math.sin(2.0 * math.pi * sarim * t)
        rr = r * taban * dalga
        z = z_taban + yuk * t
        halkalar.append([bm.verts.new(
            (math.cos(2 * math.pi * i / segment) * rr,
             math.sin(2 * math.pi * i / segment) * rr * 0.92, z))
            for i in range(segment)])
    for a, b in zip(halkalar, halkalar[1:]):
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((a[i], a[j], b[j], b[i]))
    # Tepe kapagi: acik kalirsa sarigin ici gorunur.
    tepe = bm.verts.new((0.0, 0.0, z_tepe + yuk * 0.06))
    ust = halkalar[-1]
    for i in range(segment):
        bm.faces.new((ust[i], ust[(i + 1) % segment], tepe))
    # Taban kapagi: kavugun icinde kalir ama ag KAPALI olmali —
    # acik kenar hem normalleri hem katilastirmayi bozar.
    alt = halkalar[0]
    dip = bm.verts.new((0.0, 0.0, z_taban))
    for i in range(segment):
        bm.faces.new((alt[(i + 1) % segment], alt[i], dip))
    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik * 0.5
    m.offset = -1.0
    return _uygula(obj)


def mest(ad, col, govde, sx, boy, z_bilek_orani=0.078, segment=20,
         pay=0.006):
    """Mest — ölçülen ayaktan **kalıplanan** yumuşak deri çizme.

    ## Neden kabuk değil

    Öteki giysiler gövdeden kopyalanır (`kopya_kabuk`) ve bu doğru
    yöntemdir: kumaş altındaki biçime oturur. Mest için yanlış çıktı.
    MakeHuman ayağı parmak parmak modellidir — 4 180 üçgen — ve kabuk
    onu olduğu gibi taşıdı: sarı derinin üstünde beş ayrı parmak.
    İnceleme paketi v8 ve v9'da görülen buydu. Yumuşatma da çözmedi
    (14 yineleme denendi): parmaklar ayrı lobtur, ortalama onları
    inceltir ama silmez.

    Sebep yöntemsel: **mest ayağın ofseti değildir.** Ayağa geçirilen,
    kendi biçimi olan bir kılıftır; parmak ayrımı taşımaz. O yüzden
    ölçülen iki kesitten — taban izi ve bilek — kalıplanıyor.

    Kazanç ölçüldü: 8 312 üçgen yerine birkaç yüz.

    ## Ölçüler

    - **Taban izi**: z ≤ boyun %1,2'sindeki köşelerin x ve y aralığı.
    - **Bilek**: `z_bilek_orani` kotundaki aynı ayağın kesiti.
    - Ara halkalarda uzunluk (y) hızla, genişlik (x) yavaş daralır:
      ayağın üstü (tarak) burundan çok daha kısadır.
    """
    vs = [v.co for v in govde.data.vertices if v.co.x * sx > 0.0]
    mn = min(v.co.z for v in govde.data.vertices)
    z_b = mn + boy * z_bilek_orani

    def kesit_kutu(z, kal):
        g = [v for v in vs if abs(v.z - z) < kal]
        if len(g) < 5:
            return None
        return (min(v.x for v in g), max(v.x for v in g),
                min(v.y for v in g), max(v.y for v in g))

    #: Halkalar KOTLARA gore olculur, iki uc arasinda ara deger degil.
    #: Ilk yazimda taban izinden bilege dogrusal (usluyle bukulmus) bir
    #: gecis yapiyordum ve mest parmaklari icine almiyordu: cizme
    #: 2,8 cm yukseklikte ayagin %44'u kadar kisalmisti, oysa parmaklar
    #: 2,5 cm yuksekligine kadar ILERI uzaniyor. Ayaga gecirilen bir
    #: kilif, ayagin O KOTTAKI kesitinden dar olamaz.
    ORAN = (0.0, 0.12, 0.26, 0.44, 0.64, 0.82, 1.0)
    kal = boy * 0.013
    halka_kutu = []
    for t in ORAN:
        z = mn + (z_b - mn) * t
        k = kesit_kutu(z, kal) or kesit_kutu(z, kal * 2.0)
        if k is None:
            return None
        halka_kutu.append((z, k))

    # Kilif ASAGI DOGRU DARALAMAZ: taban izi en genis olandir ve ust
    # halkalar en azindan altindakinin bilekten uzak ucunu tasimalidir.
    bm = bmesh.new()
    halkalar = []
    for idx, (z, (x0, x1, y0, y1)) in enumerate(halka_kutu):
        cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
        rx, ry = (x1 - x0) * 0.5 + pay, (y1 - y0) * 0.5 + pay
        halkalar.append([bm.verts.new(
            (cx + math.cos(2 * math.pi * i / segment) * rx,
             cy + math.sin(2 * math.pi * i / segment) * ry, z))
            for i in range(segment)])
    for a, b in zip(halkalar, halkalar[1:]):
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((a[i], a[j], b[j], b[i]))
    for halka, ters in ((halkalar[0], True), (halkalar[-1], False)):
        orta = bm.verts.new((sum(v.co.x for v in halka) / segment,
                             sum(v.co.y for v in halka) / segment,
                             halka[0].co.z))
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((halka[j], halka[i], orta) if ters
                         else (halka[i], halka[j], orta))
    bm.normal_update()
    return hz.mesh_from_bmesh(ad, bm, col)


def zemine_otur(obj, z=0.0):
    """Tabanı z düzlemine bastırır.

    Mest'in tabanı normali boyunca şişince zeminin ALTINA iniyordu
    (-0,011 m). Ayakkabı tabanı zaten düzdür ve zemine basar.
    """
    for v in obj.data.vertices:
        if v.co.z < z:
            v.co.z = z
    obj.data.update()
    return obj
