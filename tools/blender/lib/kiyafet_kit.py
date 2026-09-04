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


def cizgi_yaricapi(obj, cizgi, filtre, oranlar=(0.08, 0.5, 0.92),
                   yuzde=0.86, pencere=0.15, en_cok=None):
    """Bir uzvun merkez çizgisi boyunca **ölçülen** yarıçapı.

    ## Neden ölçülüyor

    Kol kalınlıkları boyun oranı olarak yazılıydı (`boy * 0,052`) ve tek
    gövde varken bu bir sabitle aynı şeydi. İnceleme paketinde sonucu
    görüldü: omuzda 8,8 cm yarıçap, yani kolun kendisinin iki katı —
    entari değil **balon** kol. Kumaş payını doğru seçebilmek için
    altındaki kolun kaç santim olduğunu bilmek gerekir; boy onu
    bilmiyor, ağ biliyor.

    Yedi arketip gelince mesele büyüdü: çocuğun kolu adamınkinin yarısı
    kadar ve aynı oran ona iki kat kalın bir kol giydiriyordu.

    Yüzdelik (varsayılan %86) kullanılıyor, en büyük değer değil: tek bir
    aykırı köşe (parmak ucu, koltuk altı) ölçüyü tek başına şişirirdi.
    """
    if not cizgi or len(cizgi) < 2:
        return [None] * len(oranlar)
    mw = obj.matrix_world
    yay = [0.0]
    for a, b in zip(cizgi, cizgi[1:]):
        yay.append(yay[-1] + (b - a).length)
    toplam = yay[-1]
    if toplam < 1e-6:
        return [None] * len(oranlar)

    kova = [[] for _ in oranlar]
    for v in obj.data.vertices:
        c = mw @ v.co
        if not filtre(c):
            continue
        en_yakin = None
        son = len(cizgi) - 2
        for i in range(len(cizgi) - 1):
            a, b = cizgi[i], cizgi[i + 1]
            ab = b - a
            L2 = ab.length_squared
            if L2 < 1e-12:
                continue
            ham = (c - a).dot(ab) / L2
            u = min(1.0, max(0.0, ham))
            d = (c - (a + ab * u)).length
            # CIZGININ UCUNA SIKISMIS KOSELER SAYILMAZ.
            #
            # Cizgi bilekte kesiliyor ama filtre eli ve parmaklari hala
            # iceriyor: onlarin en yakin noktasi her zaman SON nokta
            # oluyor ve uzakligi bir el boyu. Ilk olcumde bilek yaricapi
            # 31,8 cm cikti — kolun degil, elin uzunlugu. Ayni sey
            # omuzun otesindeki govde icin bastan olur.
            #
            # Yalniz cizginin YANINDAKI koseler uzvun kalinligini
            # anlatir; ucundan tasanlar baska bir uzvu anlatir.
            if (i == 0 and ham <= 0.0) or (i == son and ham >= 1.0):
                continue
            if en_yakin is None or d < en_yakin[0]:
                en_yakin = (d, (yay[i] + ab.length * u) / toplam)
        if en_yakin is None:
            continue
        d, t = en_yakin
        # UZAK KOSE BU UZVUN KOSESI DEGILDIR.
        #
        # Filtre `|x| >= kol_esik ve z >= z_kol_alt` diyor ve bu, kalcanin
        # dis yuzunu de iceriye aliyor: bilek hizasi kalca hizasidir, yani
        # o koseler cizginin SONUNA en yakin duser. Ucta 37 cm yaricap
        # olctum — bir kolun degil, kolla kalca arasindaki bosluk. Bir
        # ust sinir (boyun %9'u) o kalabaligi disarida birakir; hicbir
        # insan kolunun yaricapi 15 cm degildir.
        if en_cok is not None and d > en_cok:
            continue
        for k, o in enumerate(oranlar):
            if abs(t - o) <= pencere:
                kova[k].append(d)

    cikti = []
    for k in range(len(oranlar)):
        vs = sorted(kova[k])
        cikti.append(vs[min(len(vs) - 1, int(len(vs) * yuzde))] if vs else None)
    return cikti


def cizgi_kes(cizgi, t):
    """Merkez çizgisini **yay uzunluğunun** `t` oranında keser."""
    if not cizgi or len(cizgi) < 2 or t >= 0.999:
        return list(cizgi)
    yay = [0.0]
    for a, b in zip(cizgi, cizgi[1:]):
        yay.append(yay[-1] + (b - a).length)
    hedef = yay[-1] * max(0.0, t)
    cikti = []
    for i in range(len(cizgi)):
        if yay[i] <= hedef:
            cikti.append(cizgi[i].copy())
            continue
        onceki = yay[i - 1]
        aralik = yay[i] - onceki
        u = 0.0 if aralik < 1e-9 else (hedef - onceki) / aralik
        cikti.append(cizgi[i - 1].lerp(cizgi[i], u))
        break
    return cikti if len(cikti) >= 2 else list(cizgi[:2])


def bilek_olc(obj, cizgi, filtre, en_cok, alt=0.45):
    """Kolun **en ince** yeri — `(yay_orani, yaricap)`. Ölçülür, yazılmaz.

    Kol çizgisi bilekte `z_parmak + boy * 0,105` ile kesiliyordu. O sabit
    bir el boyu varsayıyor ve yanılıyordu: ölçülen "bilek yarıçapı"
    13 cm çıktı, yani bileğin değil **elin** yarısı, ve entari kolu ele
    kadar inen 26 cm çapında bir çan oldu.

    Bilek bir sabit değil bir **biçim**dir: kol dirsekten aşağı incelir,
    bilekte en dardır, elde yeniden genişler. En dar yeri aramak hem
    çocukta hem yetişkinde doğru yeri bulur.
    """
    oranlar = [alt + (1.0 - alt) * i / 10.0 for i in range(11)]
    profil = cizgi_yaricapi(obj, cizgi, filtre, oranlar=oranlar,
                            yuzde=0.86, pencere=0.055, en_cok=en_cok)
    gecerli = [(r, o) for r, o in zip(profil, oranlar) if r is not None]
    if not gecerli:
        return 1.0, None
    # YARICAP DA BURADAN DONER, YENIDEN OLCULMEZ.
    #
    # Once yalniz oran donuyordu ve cagiran taraf yaricapi bir kez daha
    # olcuyordu — kesilmis cizginin ucunda, yani ELIN yaninda. Yetiskinde
    # 13,5 cm veriyordu (cocukta 3,2 cm, cunku onun eli cizgiye
    # yetismiyordu) ve iki gövde ayni kodla iki farkli seyi olcuyordu.
    # En ince yeri bulan tarama zaten oradaki yaricapi biliyor.
    r, o = min(gecerli)
    return o, r


# 64 dilim: dokuz kivrim 32 dilimle cizilemez (kivrim basina 3,5
# ornek — dalga degil cokgen cikar). 64 ile kivrim basina 7 ornek
# duser ve yumusatma isini yapabilir. Bedeli 64 dortgen.
def etek(ad, col, z_ust, z_alt, r_ust, r_alt, kalinlik, segment=64,
         yarik=False, cy=0.0, cy_alt=None,
         kirisik=9, kirisik_pay=0.035, halka_sayisi=5, egri=1.0,
         ic_kapak=None):
    """Belden aşağı **serbest** düşen etek — bacakları takip etmez.

    Entarinin eteği gövdeye yapışmaz, konidir. Kabuk yöntemiyle üretseydim
    etek iki bacağa ayrılırdı ve yürürken pantolon gibi davranırdı; oysa
    Rålamb plakalarında etek tek parçadır ve altından şalvar görünür.

    `yarik`: önde açıklık (binme ve yürüme için). Plaka 20'de entari önden
    açıktır ve altındaki koyu iç entari görünür.
    """
    # ETEK COK HALKALI — AMA PROFIL DUZ KALIYOR (egri = 1,0).
    #
    # Ferace incelemesinde belde keskin bir dikis okunuyordu. Once
    # "basamak var" sanildi; olculdu ve basamak **8 mm / 3 mm** cikti
    # (`ferace dikisi` satiri), yani goze gorunen sey yaricap farki
    # degil, TEGET kirilmasi: kabuk asagi-iceri inerken koni birden
    # asagi-disari aciliyor.
    #
    # Denendi ve OLCUM REDDETTI: yaricap `t**1,6` ile ilerletilince
    # etek ust ucta dikeye yaklasti, dikis yumusadi — ama ayni turda
    # etegin yan tarafinda **kirmizi salvar** goründü. Sebep yapisal:
    # `r_ust` kabugun yaricapina, `r_alt` da altta kalanlarin zarfina
    # (`alt_zarf`) sabit. Bu iki ucu birlestiren egrilerin ICE bukuk
    # olani zarfin icine giriyor. Yani "ust ucu dikey baslasin" ile
    # "altindakini ortsun" ayni anda saglanamaz — duz koni bu iki
    # sartin TEK cozumudur.
    #
    # Halka sayisi yine de 2'den 5'e cikti: geometri ayni (egri 1,0
    # dogrusaldir) ama etek animasyonda kalcadan dize kadar tek bir
    # dortgenle deforme olmuyor. Dikis meselesi acik kaldi ve
    # `docs/feedback/sakin_kadin.md`de Caner'e soruldu.
    bm = bmesh.new()
    halkalar = []
    cy_a = cy if cy_alt is None else cy_alt
    hs = max(2, int(halka_sayisi))
    for k in range(hs):
        t = k / (hs - 1.0)
        f = t ** egri
        z = z_ust + (z_alt - z_ust) * t
        rx = r_ust[0] + (r_alt[0] - r_ust[0]) * f
        ry = r_ust[1] + (r_alt[1] - r_ust[1]) * f
        cy_t = cy + (cy_a - cy) * t
        halka = []
        for i in range(segment):
            a = 2.0 * math.pi * i / segment
            # ETEK KIRISIR — DUZ KONI KUMAS DEGILDIR.
            #
            # Etek bugune kadar iki halkali duz bir koniydi ve dokuma
            # gelince bile plastik okumaya devam etti: dokuma yuzeyi
            # anlatir, SILUETI anlatmaz. Kumasin siluetini yapan sey
            # dikey kivrimlardir — kumas belde toplanir, asagi dogru
            # acilir ve o toplanma bir dizi oluk birakir.
            #
            # Genlik ETEK BOYUNCA BUYUR (`t`): belde kusagin altinda
            # neredeyse yok, etek ucunda en cok. Kumas boyle davranir;
            # sabit genlikli bir dalga oluklu sac gibi okurdu.
            #
            # Dokuz kivrim, cunku kivrim sayisi kumasin GENISLIGINDEN
            # cikar: etek cevresi ~1,9 m, bir kivrim ~20 cm — el
            # tezgahinda dokunan bezin eni kadar.
            dalga = 1.0 + kirisik_pay * t * math.sin(kirisik * a)
            halka.append(bm.verts.new(
                (math.cos(a) * rx * dalga,
                 cy_t + math.sin(a) * ry * dalga, z)))
        halkalar.append(halka)

    # ETEGIN USTU ACIK KALMASIN.
    #
    # Etegin ust yaricapi kabuktan 8 mm DISARIDA (olculdu) ve arada
    # kalan halka acikti: inceleme karesinde belde koyu bir serit
    # goruluyordu ve o serit golge degil, etegin ICIYDI — figurun
    # icine bakiliyordu. "Dikis" diye okudugum seyin buyuk kismi bu
    # delikti.
    #
    # `ic_kapak` verilirse ust halkadan ICERIYE, kabugun yaricapina
    # dogru yatay bir raf orulur ve delik kapanir. Kumas gercekte de
    # oyle davranir: etek belde ice kivrilip govdeye dikilir.
    if ic_kapak is not None:
        ic = []
        for i in range(segment):
            a_ = 2.0 * math.pi * i / segment
            ic.append(bm.verts.new(
                (math.cos(a_) * ic_kapak[0],
                 cy + math.sin(a_) * ic_kapak[1], z_ust)))
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((ic[i], ic[j], halkalar[0][j], halkalar[0][i]))

    ust, alt = halkalar[0], halkalar[-1]
    n = segment
    on = int(round(n * 0.75))          # -y yonundeki dilim
    for h in range(len(halkalar) - 1):
        a_h, b_h = halkalar[h], halkalar[h + 1]
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a_h[i], a_h[j], b_h[j], b_h[i]))

    # YARIK BIR DELIK DEGIL, BIR BINDIRMEDIR.
    #
    # Once yarik "o dilimin yuzunu atla" diye yaziliyordu ve inceleme
    # paketinde ne oldugu gorundu: entarinin onunde bacak boyu bir
    # DELIK var, icinden kirmizi salvarin ic yuzu gorunuyor. Gercek
    # entari onden acilir ama iki kenar BINER; arasindan govde
    # gorunmez. Delik acmak "acik entari" degil "yirtik entari"ydi.
    #
    # Bindirme: on dilimin uzerine, disariya kaydirilmis dar bir kanat.
    # Siluette dikey bir kumas kenari verir, delik acmaz.
    if yarik:
        pay = kalinlik * 2.6 + 0.004
        kanat = []
        for hi, halka_h in enumerate(halkalar):
            zt = hi / (len(halkalar) - 1.0)
            sira = []
            for d in (-1, 0, 1, 2):
                k = halka_h[(on + d) % n]
                v = k.co.copy()
                cy_t = cy + (cy_a - cy) * zt
                yon = Vector((v.x, v.y - cy_t, 0.0))
                if yon.length > 1e-6:
                    yon.normalize()
                    v.x += yon.x * pay
                    v.y += yon.y * pay
                sira.append(bm.verts.new(v))
            kanat.append(sira)
        for h in range(len(kanat) - 1):
            for i in range(len(kanat[h]) - 1):
                bm.faces.new((kanat[h][i], kanat[h][i + 1],
                              kanat[h + 1][i + 1], kanat[h + 1][i]))
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
          segment=20, cy=0.0):
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
    # DILIM SAYISI SARIM BASINA OLCULUR, TOPLAMA GORE DEGIL.
    #
    # Once `sarim * 4` yaziyordu: yedi sarim icin 28 dilim, yani her
    # sinus donusune DORT ornek. Dort ornekle bir sinus cizilmez, bir
    # zikzak cizilir — inceleme paketinde sarik dokuz ayri disk gibi
    # okundu ve aralarindaki koyu cizgiler bosluk sanildi. Kusur
    # geometride degil ORNEKLEMEDEYDI; on iki ornekle ayni dalga
    # kumas gibi akiyor.
    dilim = max(24, sarim * 12)
    yuk = z_tepe - z_taban
    halkalar = []
    for k in range(dilim + 1):
        t = k / float(dilim)
        # Ana profil: TABANDA BASTAN GENIS.
        #
        # Once taban orani 0,787 idi — yani sarigin en alt halkasi basin
        # kendisinden %21 DARDI. Sonuc renderda gorundu: sarik kafanin
        # tepesine tunemis bir yay gibi duruyor, altindan cipla kafa
        # derisi cikiyordu. Sarik basa GECIRILIR; alni ortmesi
        # gerekiyorsa taban yaricapi bastan buyuk olmali.
        taban = (1.02 + 0.22 * math.sin(math.pi * (t ** 0.85)))             * (1.0 - 0.72 * (t ** 2.4))
        # Sarim dalgasi: gorunen yatay cizgiler.
        dalga = 1.0 + 0.048 * math.sin(2.0 * math.pi * sarim * t)
        rr = max(r * 0.06, r * taban * dalga)
        z = z_taban + yuk * t
        # `cy` — BASIN KENDI MERKEZI, y=0 DEGIL.
        #
        # Etekte ve kusakta bir kez olculup duzeltilen kusurun ayni sinin
        # basta duruyordu: govdenin yerel y ekseni ortasindan degil
        # ONUNDEN geciyor, o yuzden y=0'a kurulan sarik kafanin on
        # yarisina kayiyor. Renderda sarik gozleri ortuyor ve ensede
        # ciplak kafa derisi kaliyordu; "sarik kucuk" sanilmisti, oysa
        # sarik YANLIS YERDEYDI.
        halkalar.append([bm.verts.new(
            (math.cos(2 * math.pi * i / segment) * rr,
             cy + math.sin(2 * math.pi * i / segment) * rr * 0.92, z))
            for i in range(segment)])
    for a, b in zip(halkalar, halkalar[1:]):
        for i in range(segment):
            j = (i + 1) % segment
            bm.faces.new((a[i], a[j], b[j], b[i]))
    # Tepe kapagi: acik kalirsa sarigin ici gorunur.
    tepe = bm.verts.new((0.0, cy, z_tepe + yuk * 0.06))
    ust = halkalar[-1]
    for i in range(segment):
        bm.faces.new((ust[i], ust[(i + 1) % segment], tepe))
    # Taban kapagi: kavugun icinde kalir ama ag KAPALI olmali —
    # acik kenar hem normalleri hem katilastirmayi bozar.
    alt = halkalar[0]
    dip = bm.verts.new((0.0, cy, z_taban))
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


def giysi_kolu(ad, col, cizgi, r_omuz, r_dirsek, r_bilek, kalinlik,
               segment=16, ic_olcek=0.70, ic_bolge=0.13):
    """
    Entari kolu — **gövdenin kopyası değil, kendi hacmi olan bir giysi**.

    ## Neden gerekti

    Kol bugüne kadar :func:`kopya_kabuk` ile üretiliyordu: gövde kabuğu
    kopyalanıp birkaç milimetre şişiriliyordu. Bir oyuncunun cümlesi
    kusuru tam söylüyor — kıyafet *"giysi değil, biraz büyük bir vücut"*
    gibi duruyor. Kola yapışmış kumaş, kumaş gibi görünmez.

    Osmanlı entarisinin kolu **bileğe doğru genişler** ve sarkar;
    Rålamb albümündeki (1657-58, kamu malı, ``refs/ralamb/``)
    figürlerin hepsinde böyle. Yani kolun silueti bir ofset değil bir
    **profil**: omuzda dar, dirsekte orta, bilekte geniş.

    ## Nasıl

    Kolun merkez çizgisi zaten ölçülüyor (``rig_kit.uzuv_cizgisi`` — rig
    eklemleri de oradan çıkıyor). Bu işlev o çizgi boyunca halkalar
    dizer ve halkaları çizginin **kendi yönüne dik** tutar; yoksa kol
    dirsekte ezilir.
    """
    if not cizgi or len(cizgi) < 2:
        return None

    n = len(cizgi)

    def yaricap(t):
        # Omuz -> dirsek -> bilek: parcali dogrusal.
        if t <= 0.5:
            r = r_omuz + (r_dirsek - r_omuz) * (t / 0.5)
        else:
            r = r_dirsek + (r_bilek - r_dirsek) * ((t - 0.5) / 0.5)
        # IC UC DARALIR — YOKSA OMUZDA TOP OLUR.
        #
        # Cizginin ilk noktasi govdenin ICINE uzatiliyor (dikisi
        # gizlemek icin) ve o halka kolun yonune DIK duruyor; omuzda
        # kol asagi-disari baktigi icin, tam yaricapli bir halka
        # omuz cizgisinin USTUNE tasiyor. Inceleme paketinde her iki
        # omuzda birer kure gorundu — kumas degil, egik bir disk.
        # Kolun yalniz omzu daraltmak yetmedi (2,0 -> 1,7 -> 1,0 cm),
        # cunku sorun yaricapin buyuklugu degil EGIKLIGIYDI: govdenin
        # icinde kalan uc daralinca top kayboluyor, dikis hala gizli.
        if t < ic_bolge:
            r *= ic_olcek + (1.0 - ic_olcek) * (t / ic_bolge)
        return r

    bm = bmesh.new()
    halkalar = []
    for i, p in enumerate(cizgi):
        t = i / float(n - 1)
        if i == 0:
            yon = cizgi[1] - cizgi[0]
        elif i == n - 1:
            yon = cizgi[-1] - cizgi[-2]
        else:
            yon = cizgi[i + 1] - cizgi[i - 1]
        if yon.length < 1e-6:
            yon = Vector((0.0, 0.0, -1.0))
        yon = yon.normalized()

        gecici = Vector((0.0, 0.0, 1.0))
        if abs(yon.dot(gecici)) > 0.95:
            gecici = Vector((1.0, 0.0, 0.0))
        u_ax = yon.cross(gecici).normalized()
        v_ax = yon.cross(u_ax).normalized()

        r = yaricap(t)
        halka = []
        for k in range(segment):
            a = math.tau * k / segment
            halka.append(bm.verts.new(
                p + u_ax * (math.cos(a) * r) + v_ax * (math.sin(a) * r)))
        halkalar.append(halka)

    bm.verts.ensure_lookup_table()
    for i in range(n - 1):
        ust, alt = halkalar[i], halkalar[i + 1]
        for k in range(segment):
            k2 = (k + 1) % segment
            bm.faces.new((ust[k], ust[k2], alt[k2], alt[k]))

    obj = hz.mesh_from_bmesh(ad, bm, col=col)
    if obj is None:
        return None

    # KALINLIK: tek yuzlu bir kol iceriden gorunmez ve birinci sahiste
    # kanadin altindan tam da oraya bakiliyor.
    m = obj.modifiers.new("Kalinlik", "SOLIDIFY")
    m.thickness = kalinlik
    m.offset = 1.0
    _uygula(obj)

    # YUMUSAK GOLGELEME: 16 segmentli bir tup duz golgelenirse
    # renderda faseli bir fici gibi okunur — ilk denemede tam oyle
    # cikti.
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj
