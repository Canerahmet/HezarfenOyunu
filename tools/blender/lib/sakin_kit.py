# -*- coding: utf-8 -*-
"""
Hezarfen: 1632 — şehir sakinlerinin gövde ve giysi çeşitliliği.

## Neden bu kit var

Şehirde 40.000 sakin yaşıyor ve altmışı her an ekranda; hepsi **aynı
adamdı**. Bir oyuncu raporundan: *"sokaktaki herkes aynı sakallı adamın
boyu değiştirilmiş, entarisi başka renge boyanmış kopyası; çocuklar
minik sakallı adamlar, kadın hiç yok."*

Tek gövdeyi tonlayıp ölçeklemek bir çeşitlilik değil bir tekrardır:
göz boyu ve rengi değil **siluet** ayırt eder. Yedi arketip, MPFB2'nin
kendi makro kaydırıcılarından (`gender`, `age`, `height`, `weight`)
türer — kit zaten parametrik, kullanılmıyordu.

## Giysi neye göre

`refs/ralamb/` (Rålamb kıyafet albümü, 1657-58, kamu malı) okunan
dilbilgisi; minyatür kopyalanmadı. Oradan gelen üç kural:

* Erkek: entari + kuşak + şalvar; başlık **rütbe** gösterir, o yüzden
  sokaktaki adam kavuk değil sarık ya da takke taşır.
* Kadın **dışarıda** ferace (uzun, önü kapalı üst giysi) ve yaşmak
  giyer. Bu bir tercih değil, sokağa çıkış kıyafetidir; oyunun bütün
  kadınları dışarıda görüldüğü için ferace doğru olan tek şeydir.
* Çocuk küçültülmüş yetişkin değildir: entarisi kısadır (koşar), başı
  takkelidir, sakalı yoktur.

Etiket **T2**: oranlar kaynaktan okundu, ölçüler makul rekonstrüksiyon.
"""

import math

import bmesh  # noqa: F401  (kit deyimi; alt işlevler kullanır)
from mathutils import Vector

import hz_blender as hz
import kiyafet_kit as kiy


#: (ad, makro, hedef_boy, giysi_tipi, neden)
#:
#: Boylar T2 ve gerekçeli: 17. yy Osmanlı erişkin erkek boyu bugünkünden
#: kısadır; kadın erkekten ~12 cm kısa; yedi yaşındaki çocuk ~1,20 m.
#: Yaşlıda `height` düşürülür — kambur değil, kısalan omurga.
ARKETIPLER = [
    ("Sakin_Erkek", dict(gender=1.0, age=0.55, muscle=0.55, weight=0.50),
     1.70, "erkek",
     "yetiskin erkek — entari, kusak, sarik, sakal"),
    ("Sakin_Erkek_Genc", dict(gender=1.0, age=0.28, muscle=0.48, weight=0.42),
     1.68, "genc",
     "genc erkek — kisa entari, takke, sakalsiz"),
    ("Sakin_Erkek_Yasli", dict(gender=1.0, age=0.95, muscle=0.35, weight=0.44),
     1.63, "yasli",
     "yasli erkek — uzun entari, buyuk sarik, ak sakal"),
    ("Sakin_Kadin", dict(gender=0.0, age=0.45, muscle=0.35, weight=0.48,
                         cupsize=0.35, firmness=0.5),
     1.58, "kadin",
     "yetiskin kadin — ferace ve yasmak (sokak kiyafeti)"),
    ("Sakin_Kadin_Yasli", dict(gender=0.0, age=0.92, muscle=0.28, weight=0.50,
                               cupsize=0.30, firmness=0.35),
     1.54, "kadin",
     "yasli kadin — ferace ve yasmak"),
    ("Sakin_Oglan", dict(gender=1.0, age=0.10, muscle=0.35, weight=0.40),
     1.24, "cocuk",
     "erkek cocuk — kisa entari, takke"),
    ("Sakin_Kiz", dict(gender=0.0, age=0.10, muscle=0.32, weight=0.40),
     1.21, "kiz",
     "kiz cocuk — kisa entari, basortu"),
]


def etek_orani(tip):
    """Giysinin eteğinin bittiği kot (boyun oranı olarak)."""
    return {
        "erkek": kiy.BILEK_ORAN,      # ayak bilegi — oturan adam
        "yasli": kiy.BILEK_ORAN,
        "kadin": kiy.BILEK_ORAN,      # ferace yere yakin iner
        "genc": kiy.BALDIR_ORAN,      # calisan adam: baldir
        "cocuk": kiy.BALDIR_ORAN,     # kosan cocuk
        "kiz": kiy.BALDIR_ORAN * 1.35,
    }.get(tip, kiy.BILEK_ORAN)


def sakalli(tip):
    """Sakal var mı — çocukta ve kadında yok, gençte seyrek."""
    return tip in ("erkek", "yasli")


def yasmak(ad, col, bas_r, kotlar, cy=0.0, yuz_acik=False,
           kalinlik=0.005, segment=24):
    """
    Kadının ve kızın **baş örtüsü** — başa oturur, koni değildir.

    ## Ölçülen kusur

    İlk yazımında örtü, baş yarıçapından omuza doğru düz açılan bir
    yüzeydi ve tepesi açıktı. İnceleme paketinde ne olduğu görüldü:
    başın iki katı genişlikte, kimseye değmeyen, tepesinden içi görünen
    beyaz bir **abajur**. Kızda daha da kötüydü — çocuğun başı küçük
    olduğu için aynı çarpan orantısız büyük bir huni yapıyordu.

    Örtü bir hacim değil, altındaki başın biçimini alan bir yüzeydir.
    Bu yüzden yarıçap profili boydan değil **başın kendi kotlarından**
    türer: tepede kapanır, alında başı sarar, çenede biraz genişler,
    omuzda yayılır.

    ## Yüz

    Yaşmak yüzü tamamen kapatan bir çuval değildir: saçı, alnı ve ağzı
    örter, **gözler açık kalır**. İlk denemede yüz baştan aşağı kapalıydı
    ve figür yüzsüz duruyordu — kaynağın söylediği şey de bu değildi.

    Çocuk örtülmez (`yuz_acik`): kızın başında saçını örten bir örtü
    vardır, yüzü açıktır.

    `kotlar`: `(z_omuz, z_cene, z_goz, z_alin, z_tepe)` — hepsi mutlak.
    """
    z_omuz, z_cene, z_goz, z_alin, z_tepe = kotlar
    # (kot, bas_r carpani) — tepeden omuza. Carpanlar basin kendi
    # yaricapina goredir, boya gore degil: cocukta da yetiskinde de
    # ortu basa AYNI sikilikta oturur.
    profil = [(z_tepe, 0.34), (z_alin, 1.06), (z_goz, 1.09),
              (z_cene, 1.16), (z_omuz, 1.52)]

    bm = bmesh.new()
    halkalar = []
    kotlar_z = []
    ara = 3                     # iki denetim noktasi arasindaki bolme
    for i in range(len(profil) - 1):
        z0, k0 = profil[i]
        z1, k1 = profil[i + 1]
        for j in range(ara if i < len(profil) - 2 else ara + 1):
            u = j / float(ara)
            z = z0 + (z1 - z0) * u
            k = k0 + (k1 - k0) * u
            r = max(bas_r * 0.02, bas_r * k)
            halka = []
            for m in range(segment):
                a = math.tau * m / segment
                halka.append(bm.verts.new(
                    Vector((math.cos(a) * r,
                            cy + math.sin(a) * r * 0.90, z))))
            halkalar.append(halka)
            kotlar_z.append(z)

    bm.verts.ensure_lookup_table()

    # ON YON -y'dir (evin onu +Z kuralinin govdedeki karsiligi: yuz -y).
    # Yuz acikligi o yonun cevresindeki dilimlerde ACILIR.
    def on_mu(m, yariacik):
        a = math.tau * (m + 0.5) / segment
        # -y yonu: sin(a) = -1, yani a = 3pi/2.
        fark = abs(((a - 1.5 * math.pi + math.pi) % math.tau) - math.pi)
        return fark < yariacik

    for i in range(len(halkalar) - 1):
        ust, alt = halkalar[i], halkalar[i + 1]
        z_orta = (kotlar_z[i] + kotlar_z[i + 1]) * 0.5
        for m in range(segment):
            m2 = (m + 1) % segment
            if yuz_acik:
                # Cocuk: alindan asagi butun on yuz acik.
                if z_orta < z_alin and on_mu(m, math.radians(62)):
                    continue
            else:
                # Yetiskin: yalniz GOZ bandi acik.
                if (z_goz <= z_orta <= z_alin
                        and on_mu(m, math.radians(46))):
                    continue
            bm.faces.new((ust[m], ust[m2], alt[m2], alt[m]))

    # TEPE KAPAGI — acik kalirsa ortunun ici tepeden gorunur (ilk
    # yazimda gorunuyordu).
    tepe = bm.verts.new(Vector((0.0, cy, z_tepe + bas_r * 0.06)))
    ust = halkalar[0]
    for m in range(segment):
        bm.faces.new((ust[m], ust[(m + 1) % segment], tepe))

    bm.normal_update()
    obj = hz.mesh_from_bmesh(ad, bm, col=col)
    if obj is None:
        return None
    m = obj.modifiers.new("Kalinlik", "SOLIDIFY")
    m.thickness = kalinlik
    m.offset = 0.0
    kiy._uygula(obj)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def takke(ad, col, z_taban, r, yukseklik, kalinlik=0.006, segment=18,
          cy=0.0):
    """
    **Takke** — çocuğun ve gencin başlığı.

    Sarık rütbe gösterir (`SOURCE_KIYAFET`); bir çocuğun sarığı olmaz.
    Takke başa oturan yarım kubbedir: kavuktan alçak, sarıktan sade.
    """
    bm = bmesh.new()
    kat = 5
    halkalar = []
    for i in range(kat):
        t = i / float(kat - 1)
        z = z_taban + yukseklik * t
        rr = r * math.cos(t * math.pi * 0.5) ** 0.55
        if rr < 1e-4:
            rr = 1e-4
        halka = []
        for k in range(segment):
            a = math.tau * k / segment
            halka.append(bm.verts.new(
                Vector((math.cos(a) * rr, cy + math.sin(a) * rr, z))))
        halkalar.append(halka)

    bm.verts.ensure_lookup_table()
    for i in range(kat - 1):
        ust, alt = halkalar[i], halkalar[i + 1]
        for k in range(segment):
            k2 = (k + 1) % segment
            bm.faces.new((ust[k], ust[k2], alt[k2], alt[k]))
    bm.faces.new(tuple(halkalar[-1]))

    obj = hz.mesh_from_bmesh(ad, bm, col=col)
    if obj is None:
        return None
    m = obj.modifiers.new("Kalinlik", "SOLIDIFY")
    m.thickness = kalinlik
    m.offset = 0.0
    kiy._uygula(obj)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def yas_bandi(makro):
    """MPFB2 makrosundan **yaş bandı** — Unity'nin seçim anahtarı.

    `tip` tek başına yetmiyor: `Sakin_Kadin` ve `Sakin_Kadin_Yasli`'nin
    ikisi de "kadin" tipinde ve bir NPC'ye hangisinin verileceğini
    ayırt edemiyor. Ayıran şey ikisinin de zaten taşıdığı sayıdır —
    makronun `age` değeri. İkinci bir tabloya yazmak, bu depoda üç kez
    bedeli ödenmiş "bir sayının iki sahibi" olurdu.
    """
    y = float((makro or {}).get("age", 0.5))
    if y < 0.16:
        return "cocuk"
    if y < 0.35:
        return "genc"
    if y < 0.80:
        return "yetiskin"
    return "yasli"


def cinsiyet(makro):
    """Makrodan cinsiyet — `gender` 0 kadın, 1 erkek."""
    return "kadin" if float((makro or {}).get("gender", 1.0)) < 0.5         else "erkek"


def goz_boya(obj, mats, iris_yari=29.0, bebek_yari=9.5):
    """
    Göz küresine **üç malzeme** verir: ak, iris, bebek.

    ## Neden UV değil yön

    Göz MakeHuman'ın helper geometrisinden geliyor ve o küre bir iris
    için UV taşımıyor. Kendi UV'sini uydurmak, irisin nereye düşeceğini
    tahmin etmek olurdu. Oysa bir göz küresinde iris **her zaman aynı
    yerdedir**: bakış yönünde. Yani soru "UV nerede" değil, "bu yüz
    hangi yöne bakıyor".

    Açılar anatomiden: göz küresi ~24 mm, iris ~11,7 mm → yarı açı
    asin(5,85/12) ≈ 29°; bebek ~4 mm → asin(2/12) ≈ 9,5°. Uydurulmuş
    değil, ölçülebilir sayılar — ve bebek büyüklüğü ışıkla değişir,
    burada gündüz değeri.

    Bakış yönü **−Y**: hattın sözleşmesi burnu −Y'ye çevirir
    (`karakter_kit.one_cevir`), göz de onunla beraber döner.
    """
    if obj is None:
        return obj
    me = obj.data
    me.materials.clear()
    for anahtar in ("goz_ak", "goz_iris", "goz_bebek"):
        me.materials.append(mats[anahtar])

    # HER GOZ KENDI MERKEZINE GORE OLCULUR.
    #
    # Iki goz tek mesh'te ve ortak bir merkez kullanmak irisleri ice
    # dogru kaydirirdi (sasi bakan bir karakter). Koseler x isaretine
    # gore ikiye ayrilir ve her yarim kendi merkezini bulur.
    from mathutils import Vector
    merkez = {}
    for isaret in (-1, 1):
        ps = [v.co for v in me.vertices if (v.co.x * isaret) > 0.0]
        if not ps:
            continue
        merkez[isaret] = Vector((
            sum(p.x for p in ps) / len(ps),
            sum(p.y for p in ps) / len(ps),
            sum(p.z for p in ps) / len(ps)))

    ileri = Vector((0.0, -1.0, 0.0))
    iris_cos = math.cos(math.radians(iris_yari))
    bebek_cos = math.cos(math.radians(bebek_yari))
    sayim = [0, 0, 0]
    for poly in me.polygons:
        c = Vector((0.0, 0.0, 0.0))
        for i in poly.vertices:
            c += me.vertices[i].co
        c /= len(poly.vertices)
        isaret = 1 if c.x > 0.0 else -1
        m = merkez.get(isaret)
        if m is None:
            continue
        d = c - m
        if d.length < 1e-9:
            continue
        nokta = d.normalized().dot(ileri)
        if nokta >= bebek_cos:
            poly.material_index = 2
        elif nokta >= iris_cos:
            poly.material_index = 1
        else:
            poly.material_index = 0
        poly.use_smooth = True
        sayim[poly.material_index] += 1
    hz.log(f"goz: {sayim[0]} ak / {sayim[1]} iris / {sayim[2]} bebek yuz")
    return obj
