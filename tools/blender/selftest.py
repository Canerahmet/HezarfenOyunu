"""
Hezarfen: 1632 — Blender varlık hattı öz-testi.

Unity tarafının Editor testleri var; Blender tarafının yoktu. Yakın plan yapımı
(delikli duvar panelleri) sessiz bozulabilecek **geometrik değişmezler** getirdi:
bir kabuk kapanmazsa ya da normalleri terse dönerse Blender bunu göstermez —
hata Unity'de, arka yüz elemesi yüzünden "duvar yok" diye ortaya çıkar.

Burada sınanan şeyler, hepsi ya ölçüm ya da beklenen istisna:

  1. Duvar paneli **su geçirmez** ve normalleri dışa dönük.
     Ölçü: işaretli hacim (diverjans teoremi) analitik hacme eşit olmalı.
     Kapanmayan bir kabukte bu eşitlik tutmaz — yani tek sayı iki şeyi birden
     doğrular.
  2. Panel kenarına değen açıklık **hata fırlatır** (sessizce açık kenar bırakmaz).
  3. Kapı ve pencereler cephede **hiç çakışmaz** (bir kez çakışmışlardı).
  4. Pivot her parametre birleşiminde taban merkezde.
  5. `mass` ve `near` aynı parametrelerden **aynı kütleyi** üretir; LOD/kademe
     geçişinde ev yerinden oynamaz.

Koşum:
  blender --background --factory-startup --python tools/blender/selftest.py
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

import hz_blender as hz          # noqa: E402
import ottoman_kit as kit        # noqa: E402

FAILED = []


def check(name, fn):
    try:
        fn()
        hz.log(f"  OK   {name}")
    except Exception as exc:                        # noqa: BLE001
        FAILED.append((name, exc))
        hz.log(f"  FAIL {name}: {exc}")


def open_edges(obj):
    """
    Tam olarak 2 yüze bağlı OLMAYAN kenar sayısı.

    Su geçirmezliğin doğru ölçüsü budur — tam sayıdır, kayan nokta toleransı
    gerektirmez. İlk yazımda kapalılık yalnızca hacim karşılaştırmasıyla
    sınanıyordu ve test 1,1e-4'lük bir farkla kaldı; fark sızıntı değil,
    float32 köşe koordinatlarının birikme hatasıydı. Toleransı gevşetmek
    testin dişini sökerdi; doğru cevap ölçüyü değiştirmekti.
    """
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    n = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    bm.free()
    return n


def signed_volume(obj):
    """
    İşaretli hacim — normallerin DIŞA dönük olduğunun ölçüsü.

    Toplam, ağırlık merkezine göre alınır: köşeler float32'dir ve orijinden
    uzaktaki bir panelde ham koordinatlarla toplamak anlamlı basamak yer.
    """
    me = obj.data
    me.calc_loop_triangles()
    vs = [v.co.copy() for v in me.vertices]
    c0 = sum(vs, Vector()) / max(len(vs), 1)
    v = 0.0
    for tri in me.loop_triangles:
        a, b, c = (vs[i] - c0 for i in tri.vertices)
        v += a.dot(b.cross(c))
    return v / 6.0


# ------------------------------------------------------------------ 1 ve 2

def t_panel_watertight():
    col = hz.collection("T")
    cases = [
        ("aciksiz", 4.0, 3.0, 0.30, []),
        ("tek", 4.0, 3.0, 0.30, [(-0.5, 0.5, 1.0, 2.0)]),
        # Farkli v seviyeleri: bant bolunmesinin T-kavsagi birakmadigi bu vakada
        # anlasilir. Kapali kalmazsa hacim tutmaz.
        ("uc_farkli_seviye", 7.0, 2.7, 0.18,
         [(-2.6, -1.9, 0.9, 2.1), (-0.5, 0.5, 0.14, 2.1), (1.9, 2.6, 1.2, 2.4)]),
    ]
    for tag, w, h, t, ops in cases:
        obj = hz.make_wall_panel(f"P_{tag}", w, h, t, (11.0, -6.0, 2.0),
                                 (1.0, 0.0), (0.0, -1.0), openings=ops, col=col)
        bad = open_edges(obj)
        if bad:
            raise AssertionError(f"{tag}: {bad} kenar 2 yuze bagli degil "
                                 f"(kabuk kapali degil / T-kavsagi var)")
        want = w * h * t - sum((b - a) * (d - c) * t for a, b, c, d in ops)
        got = signed_volume(obj)
        if got <= 0.0:
            raise AssertionError(f"{tag}: normaller ice donuk (hacim {got:.5f})")
        if abs(got - want) > 1e-3 * want:
            raise AssertionError(f"{tag}: hacim {got:.5f} != {want:.5f}")


def t_panel_edge_opening_raises():
    col = hz.collection("T")
    for ops in ([(-2.0, -1.0, 1.0, 2.0)],          # sol kenara degiyor
                [(-0.5, 0.5, 0.0, 2.0)],           # tabana degiyor
                [(-0.5, 0.5, 1.0, 3.0)]):          # tepeye degiyor
        try:
            hz.make_wall_panel("Bad", 4.0, 3.0, 0.3, (0.0, 0.0, 0.0),
                               (1.0, 0.0), (0.0, -1.0), openings=ops, col=col)
        except ValueError:
            continue
        raise AssertionError(f"kenara degen aciklik hata firlatmadi: {ops}")


# ---------------------------------------------------------------------- 3

def t_openings_never_overlap():
    for width in (3.2, 5.0, 7.0, 9.5, 12.0):
        for density in (0.3, 0.55, 0.8):
            p = kit.HouseParams(width=width, window_density=density).validate()
            lay = kit._opening_layout(p, width * 0.86, ground=True)
            spans = sorted((o["cu"] - o["w"] * 0.5, o["cu"] + o["w"] * 0.5)
                           for o in lay)
            for (a0, a1), (b0, b1) in zip(spans, spans[1:]):
                if b0 < a1 - 1e-9:
                    raise AssertionError(
                        f"w={width} d={density}: aciklik cakismasi "
                        f"[{a0:.2f},{a1:.2f}] / [{b0:.2f},{b1:.2f}]")
            # Panel kenarina da degmemeli, yoksa uretim patlar.
            if spans and (spans[0][0] <= -width * 0.5 or spans[-1][1] >= width * 0.5):
                raise AssertionError(f"w={width} d={density}: aciklik duvar disina tasti")


# ------------------------------------------------------------------ 4 ve 5

def t_material_names_unique():
    """
    Bir malzeme ADI her zaman aynı dokuyu ve aynı boya parametrelerini göstermeli.

    Blender çakışan adı sessizce `.001` ekleyerek geçiştirir; hata ancak Unity'ye
    malzeme yazarken ortaya çıkar — ve o noktada hangi `.001`in hangi ev olduğu
    belli değildir. İlk yazımda üç çakışma vardı.
    """
    seen = {}
    for pal_name, pal in kit.PALETTES.items():
        names = [entry[2] for entry in pal.values()]
        dupes = {n for n in names if names.count(n) > 1}
        if dupes:
            raise AssertionError(f"{pal_name} paletinde ayni ad birden fazla "
                                 f"rolde: {sorted(dupes)}")
        roles = kit.TEXTURE_ROLES.get(pal_name, {})
        for key, entry in pal.items():
            name = entry[2]
            r = roles.get(key, {})
            sig = (r.get("asset"), r.get("tint"), r.get("tint_factor", 0.0),
                   r.get("value_gamma", 1.0), r.get("tint_blend", "COLOR"))
            if name in seen and seen[name][0] != sig:
                raise AssertionError(
                    f"'{name}' iki farkli tanim gosteriyor: "
                    f"{seen[name][1]} -> {seen[name][0]} ve {pal_name}.{key} -> {sig}")
            seen.setdefault(name, (sig, f"{pal_name}.{key}"))


TEXTURE_DIR = os.path.join("unity", "HezarfenGame", "Assets", "_Project",
                           "Art", "Textures", "Ottoman")


def _albedo_stats(basename):
    """Pişirilmiş albedo dokusunun ortalama R/G ve doygunluğu (ışıksız)."""
    import glob
    import numpy as np
    from measure_render import load_srgb            # noqa: E402

    hits = glob.glob(os.path.join(TEXTURE_DIR, basename + ".*"))
    hits = [h for h in hits if not h.endswith(".meta")]
    if not hits:
        raise AssertionError(f"doku yok: {basename} ({TEXTURE_DIR})")
    a = load_srgb(hits[0]).astype(np.float64)
    h, w, _ = a.shape
    patch = a[h // 5:h * 4 // 5, w // 5:w * 4 // 5, :]
    r, g, b = (patch[..., i].mean() for i in range(3))
    mx, mn = max(r, g, b), min(r, g, b)
    return r / max(g, 1e-6), (mx - mn) / max(mx, 1e-6), (r + g + b) / 3.0


def t_roof_textures_same_colour_family():
    """
    İki paletin çatı dokusu **aynı renk ailesinde** olmalı.

    "Kesme taş her iki palette AYNI: taşın kendisi mahalleye göre değişmez"
    ilkesi kiremit için de geçerlidir — aynı ocaktan çıkar. Boya kısıtı
    zimmînin duvarına konur, çatısına değil.

    Bu test bir kez ihlal edilmiş olduğu için var: iki palet farklı Poly Haven
    varlığı kullanıyordu ve gayrimüslim olanı hakiye çalıyordu (R/G 1,24).
    Boyayla düzeltilirken bu sefer AŞIRI kırmızıya kaçtı (2,78) — ve ikisi de
    ancak sahnede fark edildi. Ölçü artık dokunun kendisidir; aydınlatmalı bir
    render bu farkı gizleyebiliyor.
    """
    ref = _albedo_stats("T_ClayRoofTiles02_BC")
    other = _albedo_stats("T_Roof_Ceramic_BC")
    if abs(other[0] - ref[0]) > 0.35:
        raise AssertionError(f"cati R/G ayrisiyor: varsayilan {ref[0]:.2f}, "
                             f"gayrimuslim {other[0]:.2f} (sinir 0,35)")
    if abs(other[1] - ref[1]) > 0.15:
        raise AssertionError(f"cati doygunlugu ayrisiyor: {ref[1]:.2f} / "
                             f"{other[1]:.2f} (sinir 0,15)")
    if abs(other[2] - ref[2]) > 25.0:
        raise AssertionError(f"cati parlakligi ayrisiyor: {ref[2]:.1f} / "
                             f"{other[2]:.1f} (sinir 25)")



def _chroma(rgb):
    """CIELAB kromasi (C*ab) — DOYGUNLUK olcusu, aciklik degil."""
    R, G, B = rgb
    X = R * 0.4124 + G * 0.3576 + B * 0.1805
    Y = R * 0.2126 + G * 0.7152 + B * 0.0722
    Z = R * 0.0193 + G * 0.1192 + B * 0.9505

    def f(t):
        return t ** (1.0 / 3.0) if t > 0.008856 else 7.787 * t + 16.0 / 116.0

    fx, fy, fz = f(X / 0.95047), f(Y / 1.0), f(Z / 1.08883)
    a, b = 500.0 * (fx - fy), 200.0 * (fy - fz)
    return (a * a + b * b) ** 0.5


def t_bare_timber_is_not_painted():
    """
    **Boyasız ahşap gerçekten boyasız kalmalı.**

    Aşı kırmızısı bir EV boyasıdır (ADR 0030 §5c); nöbet kulesi, iskele,
    değirmen gibi yapısal ahşap boyanmaz. Kız Kulesi ilk yazımda `trim`
    kullanıyordu ve kod yorumu "boyasız" diyordu — yorum yanlıştı: `trim`,
    ASI_DARK ile %70 karıştırılmış, yani BOYALI. Render'da kırmızı okundu.

    Ayırt edici nitelik **açıklık değil DOYGUNLUK**tur: boyasız kereste ile
    koyu aşı, açıklıkta birbirine yakın durabilir (ΔE yalnızca 8,4) ama
    kromada ayrışır. Bu yüzden ölçü krom.
    """
    bare_limit, painted_floor = 8.0, 11.0
    for name, pal in kit.PALETTES.items():
        role = pal.get("timber_bare")
        if role is None:
            raise AssertionError(f"'{name}' paletinde timber_bare yok")
        c = _chroma(role[0])
        if c >= bare_limit:
            raise AssertionError(f"{name}/timber_bare kroma {c:.1f} — "
                                 f"boyasiz ahsap {bare_limit} altinda olmali")
        for other in ("timber", "trim"):
            co = _chroma(pal[other][0])
            if co <= painted_floor:
                raise AssertionError(
                    f"{name}/{other} kroma {co:.1f} — asi ailesi "
                    f"{painted_floor} ustunde olmali; degilse boyali ile "
                    "boyasiz ayrimi olcumle yapilamaz")




def t_half_dome_watertight_and_outward():
    """
    Yarım kubbe **su geçirmez** ve yüzleri **dışa** dönük olmalı.

    Bu test yeni bir geometri yazıldığı için değil, aynı hatayı **iki kez**
    yaptığım için var: kaldırım şeridinde ve sur perdesinde sarım tersti ve
    ikisinde de kendi yorumum "sarım doğru" diyordu. Ters sarımlı bir yüzey
    üstten ışıksız okunur, ışın izleme onu görmez ve altındaki şey zemin
    sanılır — hiçbiri render'a bakarak fark edilmez.

    Ölçü sayımdır: (1) her kenar tam iki yüze ait mi (kapalı kabuk),
    (2) kaç yüz normali dışarı bakıyor.
    """
    hz.reset_scene()
    col = hz.collection("T")
    obj = hz.make_half_dome("T_Yarim", 5.0, 3.9, (0.0, 0.0), 0.0,
                            facing=math.pi * 0.5, segments=16, rings=6,
                            col=col)

    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()

    open_edges = [e for e in bm.edges if len(e.link_faces) != 2]
    if open_edges:
        raise AssertionError(f"yarim kubbede {len(open_edges)} acik kenar — "
                             "kabuk kapali degil, kutle icten gorunur")

    # Merkez: kabugun icindeki bir nokta. Yarim kubbe `facing` yonune acilir,
    # dolayisiyla agirlik merkezi kiris duzleminin O yaninda.
    cx = sum(v.co.x for v in bm.verts) / len(bm.verts)
    cy = sum(v.co.y for v in bm.verts) / len(bm.verts)
    cz = sum(v.co.z for v in bm.verts) / len(bm.verts)
    inward = 0
    for f in bm.faces:
        c = f.calc_center_median()
        if f.normal.dot(c - Vector((cx, cy, cz))) < 0.0:
            inward += 1
    bm.free()
    if inward:
        raise AssertionError(f"{inward} yuz ICERI bakiyor — ters sarim")



def _build(**kw):
    hz.reset_scene()
    col = hz.collection("Export")
    p = kit.HouseParams(**kw).apply_palette_rules()
    return kit.build_house(p, col, "T", textured=False)[4]


def t_dome_uv_continuous():
    """
    Kubbenin UV'si yüz sınırlarında **kopmuyor**.

    Neden ölçülüyor: kurşun örtünün dikiş ızgarası düz çizgilerden oluşur ve
    yüze hizalı izdüşüm eğri yüzeyde her yüzü ayrı bir düzleme yansıtır. Sonuç
    "kırılmış fayans"tır ve render'a bakmadan görülmez; bir kez öyle çıktı
    (ADR 0021). Ölçü: iki yüzün PAYLAŞTIĞI köşeye verdikleri UV aynı mı.

    Beklenen tek kopukluk **azimut dikişi**dir (u = 0 ile u = 2πr'nin
    buluştuğu sütun) — küre UV'sinin kaçınılmaz maliyeti, gerçek kurşun
    örtüde de kapanış dilimi oradadır. Yani eşik `rings` kadar kenardır;
    düzlemsel izdüşümde bu sayı yüzlerce olurdu.

    **Tepe noktası ölçüye girmez.** Kubbenin apeksi tek köşedir ama oraya
    gelen her dilim farklı bir `u` taşır; UV köşede değil ilmekte durduğu için
    bu bir kopukluk değil, kutupun tanımıdır. İlk yazımda dahil edilmişti ve
    test 21 kopukluk saydı — 16'sı tepedeydi. Ölçünün kendisi yanlıştı.
    """
    import materials as mtl

    hz.reset_scene()
    col = hz.collection("T")
    segments, rings = 16, 6
    dome = hz.make_dome("T_Kubbe", 3.0, 2.0, (0.0, 0.0), 0.0,
                        segments=segments, rings=rings, col=col)
    hz.assign(dome, hz.make_material("M_T", (0.5, 0.5, 0.5)))
    mtl.uv_project(dome, {0: (2.0, 2.0)})

    bm = bmesh.new()
    bm.from_mesh(dome.data)
    uv = bm.loops.layers.uv.verify()
    bm.verts.ensure_lookup_table()
    pole = max(bm.verts, key=lambda v: v.co.z).index
    broken = 0
    for e in bm.edges:
        if len(e.link_faces) != 2:
            continue
        got = []
        for f in e.link_faces:
            got.append({lp.vert.index: tuple(lp[uv].uv) for lp in f.loops})
        for v in (e.verts[0].index, e.verts[1].index):
            if v == pole:
                continue
            a, b = got[0].get(v), got[1].get(v)
            if a is None or b is None:
                continue
            if abs(a[0] - b[0]) > 1e-4 or abs(a[1] - b[1]) > 1e-4:
                broken += 1
                break
    bm.free()

    if broken > rings:
        raise AssertionError(f"kubbe UV'si {broken} kenarda kopuyor; en fazla "
                             f"{rings} (azimut dikisi) olmali")

    # Testin AYIRT ETME gucu: metrik isaret olmasaydi ayni olcu buyuk cikmali.
    # Yoksa "sessizce gecti" ile "dogru" ayirt edilemez.
    bm = bmesh.new()
    bm.from_mesh(dome.data)
    lock = bm.faces.layers.int.get(hz.UV_METRIC)
    marked = sum(1 for f in bm.faces if lock is not None and f[lock])
    total = len(bm.faces)
    bm.free()
    if marked != total:
        raise AssertionError(f"kubbenin {total - marked} yuzu metrik UV "
                             f"isareti tasimiyor")


def t_pivot_at_base_centre():
    for kw in (dict(floors=1, cumba_type="none", detail="near"),
               dict(floors=2, cumba_type="corbel", detail="near", facades="all"),
               dict(floors=3, cumba_type="corner", detail="near", window_detail="none"),
               dict(floors=2, palette="nonmuslim", detail="near"),
               dict(floors=2, detail="mass")):
        info = _build(**kw)
        if abs(info["pivot_min_z"]) > 1e-3:
            raise AssertionError(f"{kw}: pivot min_z={info['pivot_min_z']}")


def t_mass_and_near_same_mass():
    """Kademe DETAYI değiştirir, KÜTLEYİ değil."""
    for kw in (dict(floors=2, cumba_type="corbel"),
               dict(floors=3, cumba_type="corner", width=9.0),
               dict(floors=1, cumba_type="none")):
        a = _build(detail="mass", **kw)
        b = _build(detail="near", **kw)
        for key in ("footprint_x", "footprint_y", "height", "roof_height", "wall_top"):
            if abs(a[key] - b[key]) > 1e-3:
                raise AssertionError(f"{kw}: {key} mass={a[key]} near={b[key]}")
        if b["tris_lod1"] != a["tris_lod1"] or b["tris_lod2"] != a["tris_lod2"]:
            raise AssertionError(f"{kw}: yakin plan uzak LOD'lari degistirdi "
                                 f"({a['tris_lod1']}/{a['tris_lod2']} -> "
                                 f"{b['tris_lod1']}/{b['tris_lod2']})")


def t_dome_facets_carry_the_counted_rib_number():
    """
    Sayılan bir değer **mesh'te** yaşamalı, katalogda değil.

    Ayasofya'nın kubbe eteğinde **kırk** kaburga ve aralarında **kırk**
    pencere vardır. Kubbe `segments = 40` ile üretilir ki dilimler
    kaburgalara denk gelsin. Katalogda "40" yazıp meshi 32 dilim üretmek
    tam olarak Faz 3 boyunca kovaladığım hatadır: **katalogda yaşayıp
    meshte yaşamayan sayı**.

    Ölçü: taban halkasındaki köşelerin açıları sayılır.
    """
    for n in (40, 24):
        hz.reset_scene()
        col = hz.collection("T")
        dome = hz.make_dome("D", 16.5, 15.0, (0.0, 0.0), 0.0,
                            segments=n, rings=9, col=col)
        base = [v.co for v in dome.data.vertices if abs(v.co.z) < 1e-6]
        angles = sorted(set(round(math.degrees(math.atan2(c.y, c.x)) % 360.0, 3)
                            for c in base))
        if len(angles) != n:
            raise AssertionError(f"segments={n} istendi, taban halkasinda "
                                 f"{len(angles)} kose var")
        step = 360.0 / n
        for i, a in enumerate(angles):
            if abs(a - i * step) > 1e-2:
                raise AssertionError(f"segments={n}: {i}. kose {a:.3f} derece, "
                                     f"{i * step:.3f} olmali")


def main():
    hz.reset_scene()
    hz.log("Blender oz-testi:")
    check("duvar paneli su gecirmez + disa donuk", t_panel_watertight)
    check("kenara degen aciklik reddediliyor", t_panel_edge_opening_raises)
    check("kapi/pencere hic cakismiyor", t_openings_never_overlap)
    check("malzeme adlari benzersiz ve tutarli", t_material_names_unique)
    check("iki paletin catisi ayni renk ailesinde", t_roof_textures_same_colour_family)
    check("boyasiz ahsap gercekten boyasiz", t_bare_timber_is_not_painted)
    check("yarim kubbe su gecirmez + disa donuk", t_half_dome_watertight_and_outward)
    check("kubbe UV'si yuz sinirinda kopmuyor", t_dome_uv_continuous)
    check("kubbe dilimleri sayilan kaburga sayisini tasiyor",
          t_dome_facets_carry_the_counted_rib_number)
    check("pivot taban merkezde", t_pivot_at_base_centre)
    check("mass ve near ayni kutle", t_mass_and_near_same_mass)

    if FAILED:
        hz.log(f"SONUC: {len(FAILED)} test BASARISIZ")
        raise SystemExit(1)
    hz.log("SONUC: hepsi gecti")


if __name__ == "__main__":
    main()
