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


def kesit(govde, z, kalinlik=0.02, x_esik=None):
    """Bu kottaki gövde kesitinin (yarı-genişlik x, yarı-derinlik y) ölçüsü.

    `x_esik` verilirse o eşiğin dışındaki noktalar (yani **kollar**) sayılmaz.

    Bunu unutmak ölçülebilir bir hataya yol açtı: A-pozunda kollar kalça ve
    bel hizasından geçer, yani "bel kesiti" diye ölçtüğüm şey kolun x
    açıklığıydı. Kuşak 0,84 m çapında çıktı — hula hoop. Eteğin eteği de
    1,17 m. Bir kesit ölçüsü, kesitin neyi içerdiğini bilmiyorsa yalan
    söyler.
    """
    vs = [v.co for v in govde.data.vertices
          if abs(v.co.z - z) < kalinlik
          and (x_esik is None or abs(v.co.x) < x_esik)]
    if len(vs) < 4:
        return None
    return (max(abs(v.x) for v in vs), max(abs(v.y) for v in vs))


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
         yarik=False):
    """Belden aşağı **serbest** düşen etek — bacakları takip etmez.

    Entarinin eteği gövdeye yapışmaz, konidir. Kabuk yöntemiyle üretseydim
    etek iki bacağa ayrılırdı ve yürürken pantolon gibi davranırdı; oysa
    Rålamb plakalarında etek tek parçadır ve altından şalvar görünür.

    `yarik`: önde açıklık (binme ve yürüme için). Plaka 20'de entari önden
    açıktır ve altındaki koyu iç entari görünür.
    """
    bm = bmesh.new()
    halkalar = []
    for t in (0.0, 1.0):
        z = z_ust + (z_alt - z_ust) * t
        rx = r_ust[0] + (r_alt[0] - r_ust[0]) * t
        ry = r_ust[1] + (r_alt[1] - r_ust[1]) * t
        halka = []
        for i in range(segment):
            a = 2.0 * math.pi * i / segment
            # Yarik: on tarafta (-y) dar bir dilim atlanir.
            halka.append(bm.verts.new(
                (math.cos(a) * rx, math.sin(a) * ry, z)))
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


def band(ad, col, z, r, yukseklik, kalinlik, segment=20):
    """Kuşak / dizlik / bilezik — gövdeyi saran dar bir kuşak."""
    bm = bmesh.new()
    halkalar = []
    for zz in (z - yukseklik * 0.5, z + yukseklik * 0.5):
        halkalar.append([bm.verts.new(
            (math.cos(2 * math.pi * i / segment) * (r[0] + kalinlik),
             math.sin(2 * math.pi * i / segment) * (r[1] + kalinlik), zz))
            for i in range(segment)])
    a, b = halkalar
    for i in range(segment):
        j = (i + 1) % segment
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik * 1.6
    m.offset = 0.0
    return _uygula(obj)


def sarik(ad, col, z_taban, z_tepe, r, sarim=7, kalinlik=0.034):
    """Sarık: kavuk çekirdeğinin üstüne sarılan bez.

    Kotlar **açıkça** verilir. İlk yazımda merkez + yarıçaptan türetiyordum
    ve sarık kafanın üstüne değil yüzüne oturdu: tepesi 1,677 m, oysa başın
    tepesi 1,700 m. Sarık başlıktır; başın altında kalamaz.

    Tek bir küre değil **sarım sarım** kurulur, çünkü sarığı sarık yapan şey
    hacim değil o yatay çizgilerdir — plaka 35 ve 50'de sarımlar açıkça
    sayılabiliyor. Hacim Hezarfen için ORTA: plaka 20'nin sivil sarığı
    büyük, plaka 50'nin asker sarığı sıkı; o ne biri ne öbürü.
    """
    bm = bmesh.new()
    for k in range(sarim):
        t = (k + 0.5) / sarim
        # Sarim ortada genis, uclarda dar: sarik bir fici gibidir.
        # Profil iki ucta DARALIR: sarik bir fici, disk yigini degil.
        # Ilk turda ust sarim en genisiydi ve basin ustunde havada duran
        # bes ayri tabak gibi gorunuyordu.
        rr = r * (0.74 + 0.50 * math.sin(math.pi * t))
        z = z_taban + (z_tepe - z_taban) * t
        halkalar = []
        for dz in (-kalinlik * 0.5, kalinlik * 0.5):
            halkalar.append([bm.verts.new(
                (math.cos(2 * math.pi * i / 16) * rr,
                 math.sin(2 * math.pi * i / 16) * rr * 0.92,
                 z + dz)) for i in range(16)])
        a, b = halkalar
        for i in range(16):
            j = (i + 1) % 16
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col)
    m = obj.modifiers.new("kalinlik", "SOLIDIFY")
    m.thickness = kalinlik * 0.9
    m.offset = 0.0
    return _uygula(obj)


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
