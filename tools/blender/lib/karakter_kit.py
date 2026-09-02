"""
Hezarfen: 1632 — Karakter kiti (Faz 5).

## Bu kit neyi yapar, neyi yapmaz

Bu projenin bütün varlıkları scriptle **sıfırdan** üretildi. Karakter tek
istisna ve istisna plan tarafından konuldu (Bölüm 10): taban geometri
Blender Studio'nun **Human Base Meshes** paketinden (CC0) gelir. Sebep
anatomi değil **deformasyon**: iyi bir insan gövdesinin dirsek, diz, omuz
ve göz çevresindeki kenar halkaları animasyon için özel olarak
yerleştirilmiştir ve bunu prosedürel üretmek, üretilenin iyi olduğunu
ölçmek çok daha zordur. Ev üretebilirim; omuz halkası başka iş.

Kit o tabanı **getirir, ölçer, normalleştirir** — ve bundan sonrası yine
scripttir: oran uyarlaması, kıyafet, saç, rig.

## Ölçülen şeyler (varsayılmayanlar)

İki şey burada varsayılmıyor çünkü ikisi de sessizce yanlış olabilir:

1. **Yön.** Taban ağın hangi eksene baktığı paket sürümüne göre değişir.
   Kit ayak parmaklarını bularak yönü ÖLÇER (ayaklar öne bakar) ve
   gerekirse döndürür. Yanlış yön, karakteri geri geri yürüten türden bir
   hatadır ve bir animasyon turu boyunca fark edilmeyebilir.
2. **Boy.** Hedef 1,70 m ve bu keyfî değil: bu projenin bütün inceleme
   paketleri 1,70 m'lik bir ölçek figürüne göre yargılandı. Karakter
   başka boyda olursa şehir yanlış ölçekte kurulmuş olur — ya da daha
   kötüsü, şehir doğru ama karakter yanlış olur ve kimse fark etmez.

## Eksen sözleşmesi

Unity(x,y,z) = Blender(-x, z, -y); **ön yüz +Z (Unity)** = **-Y
(Blender)**. Ev kitiyle aynı; ayrı bir kural yok.

Pivot: **iki ayağın arasında, zeminde** (z=0). Karakter bir zemine basar,
kutuya oturmaz.
"""

import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

import hz_blender as hz

#: Hedef boy (m). Bu projenin ölçek figürüyle AYNI sayı olmalı.
#: `tools/blender/render_preview.py` her karede 1,70 m'lik figür çizer ve
#: 36 landmark + 142 mahalle o figüre bakılarak onaylandı.
HEDEF_BOY = 1.70

#: Boy toleransı (m) — bunun ötesinde uyarlamayı hata sayarız.
BOY_TOLERANS = 0.005

#: Paket içinde seçilen taban. Gerekçesi `art/base/blender-studio/meta.json`.
TABAN_OBJE = "GEO-body_male_realistic"

#: İndirilen paketin varsayılan yeri (depoya girmez).
TABAN_BLEND = os.path.join(
    "art", "base", "blender-studio",
    "human-base-meshes-bundle-v1.4.1", "human_base_meshes_bundle.blend")


class TabanYok(RuntimeError):
    """Taban paketi yok — indirilmesi gerektiğini AÇIKÇA söyler."""


def taban_getir(blend_path=None, obje=TABAN_OBJE, col=None):
    """CC0 taban gövdesini sahneye getirir ve bağımsız bir kopya yapar.

    `append` (link değil): dosya yeniden indirilebilir bir üçüncü taraf
    kaynağıdır ve depoya girmez, yani bir bağlantı ilk klonda kırılırdı.
    """
    # MUTLAK yol sart: `bpy.data.libraries.load` goreli yolu Blender'in
    # kendi kokune gore cozer, Python'un CWD'sine gore degil. Ilk koşuda
    # `os.path.exists` gectigi halde load `C:rt\...` aradi.
    path = os.path.abspath(blend_path or TABAN_BLEND)
    if not os.path.exists(path):
        raise TabanYok(
            f"Taban paketi yok: {path}\n"
            "Indir: https://download.blender.org/demo/asset-bundles/"
            "human-base-meshes/human-base-meshes-bundle-v1.4.1.zip\n"
            "Kayit: art/base/blender-studio/meta.json (CC0, "
            "refs/LICENSES.md'de kayitli). Depoya GIRMEZ.")

    before = set(bpy.data.objects)
    with bpy.data.libraries.load(path, link=False) as (src, dst):
        if obje not in src.objects:
            raise TabanYok(f"'{obje}' pakette yok. Paket icerigi: "
                           f"{sorted(o for o in src.objects if 'body' in o)}")
        dst.objects = [obje]
    yeni = [o for o in set(bpy.data.objects) - before if o is not None]
    if not yeni:
        raise TabanYok(f"'{obje}' getirilemedi.")
    o = yeni[0]
    hz.link(o, col)

    # Paketteki maskeleme modifier'lari kirik surucu (driver) tasiyor ve
    # her acilista uyari basiyor. Bize govdenin TAMAMI lazim, maskeli
    # hali degil: hepsi silinir.
    o.modifiers.clear()
    o.animation_data_clear()

    # Pakette nesneler bir sirada YAN YANA dizilidir; getirilen govdenin
    # kendi konumu vardir (bu paket icin x ~ -2,26). Boru hattinin geri
    # kalani KIMLIK donusumu bekler ve butun olcum kodum yerel koordinat
    # yaziyor — ikisi karisinca govde giysilerden 2 metre uzakta duruyordu.
    # Donusum aga yazilir ve transform sifirlanir.
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix.Identity(4)
    o.data.update()
    return o


def yonu_olc(obj):
    """Ağın hangi yatay yöne baktığını **ölçer**.

    ## İlk yazımın hatası (ve neden sayı yakaladı)

    Önce ayak parmaklarını arıyordum: "iki ayağın ortak merkezinden en
    uzak nokta parmak ucudur." Değil. İki ayak yanyana durur, aralarındaki
    YANAL açıklık parmak uzunluğundan büyüktür, yani en uzak nokta ayağın
    DIŞ yanıdır. Ölçüm 82,2 derece döndürme istedi ve gövdeyi yan çevirdi.

    Yakalayan şey render değil, bir sayıydı: omuz genişliği **0,247 m**
    çıktı. Bir yetişkin omzu 0,38-0,48 m'dir. Yani ölçü aleti kendi
    hatasını raporladı.

    ## Şimdiki ölçüm

    İnsan **geniş ve ince**dir: omuzdan omuza mesafe, göğüsten sırta
    mesafenin iki-üç katıdır. O yüzden:

    1. **Yanal eksen** = yatay eksenlerden AÇIKLIĞI BÜYÜK olan.
    2. **Bakış ekseni** = diğeri (dar olan).
    3. **İşaret** = ayaklarda hangi tarafa daha çok taşma varsa orası ön;
       parmaklar öne uzar, topuk arkaya kısa çıkar.

    `(yon_vektoru, guven)` döner. Güven = iki eksenin açıklık oranı;
    1'e yakınsa gövde kare demektir ve ölçüm güvenilmez.
    """
    mw = obj.matrix_world
    vs = [mw @ v.co for v in obj.data.vertices]
    xs = [v.x for v in vs]
    ys = [v.y for v in vs]
    zs = [v.z for v in vs]
    ax = max(xs) - min(xs)
    ay = max(ys) - min(ys)
    if min(ax, ay) < 1e-6:
        return Vector((0.0, -1.0, 0.0)), 0.0

    # 1-2) Dar eksen bakis eksenidir.
    dar_x = ax < ay
    guven = 1.0 - min(ax, ay) / max(ax, ay)

    # 3) ISARET: BURUN.
    #
    # Onceki isaret ayak tasmasiydi ve YANLIS OLCTU. Referans olarak
    # butun kosenlerin agirlik merkezini (`govde_o`) aliyordu; o nokta
    # ayak bilegi degil, govde ve kafa koselerinin yogunluk merkezidir.
    # Parmak-topuk farki o referansa gore isaret vermedi ve olcum tabani
    # 180 derece ters cevirdi. Sonuc zincirin sonuna kadar gitti:
    # catalog.json'a `yon_duzeltme_derece: -180.0` yazildi, inceleme
    # render'inda "on cephe" karesi SIRTI gosterdi, Unity'de omuz ustu
    # kamerasi karakterin yuzunu gordu, ve rig'in ayak parmagi kemigi
    # TOPUGA kondu (rig_kit onun de -y oldugunu varsayiyor).
    #
    # Yeni isaret Unity tarafinda olculerek dogrulandi: govde onden
    # arkadan neredeyse simetrik (ayak +0,275 / -0,271; bas +0,111 /
    # -0,107) ama YUZ HIZASINDA, MERKEZ SERIDINDE burun 2 cm'lik keskin
    # bir asimetri veriyor. Kaba ölçü yön söylemiyor, burun söylüyor.
    z0, z1 = min(zs), max(zs)
    boy = z1 - z0
    if boy < 1e-6:
        return Vector((0.0, -1.0, 0.0)), 0.0

    yanal = ys if dar_x else xs          # genis eksen
    derin = xs if dar_x else ys          # dar eksen = bakis ekseni
    yanal_o = sum(yanal) / len(yanal)
    serit = boy * 0.035                  # ~6 cm, yuzun orta seridi

    ileri_uc, geri_uc = None, None
    for i, v in enumerate(vs):
        t = (v.z - z0) / boy
        if not (0.86 <= t <= 0.93):      # yuz bandi
            continue
        if abs(yanal[i] - yanal_o) > serit:
            continue
        dv = derin[i]
        ileri_uc = dv if ileri_uc is None else max(ileri_uc, dv)
        geri_uc = dv if geri_uc is None else min(geri_uc, dv)

    if ileri_uc is None or geri_uc is None:
        # Yuz bandi bos: sessizce tahmin etme, guveni sifirla.
        return Vector((0.0, -1.0, 0.0)), 0.0

    derin_o = (ileri_uc + geri_uc) * 0.5
    burun_arti = ileri_uc - derin_o
    burun_eksi = derin_o - geri_uc
    isaret = 1.0 if burun_arti >= burun_eksi else -1.0

    # Burun sinyali cok zayifsa (simetrik kafa) guveni dusur ki
    # `one_cevir` dondurmeyi reddetsin.
    fark = abs(burun_arti - burun_eksi)
    if fark < boy * 0.004:               # < ~7 mm
        guven = min(guven, 0.4)

    d = (Vector((isaret, 0.0, 0.0)) if dar_x
         else Vector((0.0, isaret, 0.0)))
    return d, guven


def on_yonu(o):
    """
    Gövdenin baktığı Y yönü: -1 (-Y) ya da +1 (+Y). Ölçü **ayaktan**.

    ## Neden kafadan değil

    Önce kafa bandındaki en uzak noktaya baktım — ve o nokta burun
    değil **ense kubbesi** çıktı. Kafa y=0'da merkezli değildir, o
    yüzden `abs(en_uzak)` karşılaştırması yüzü değil kafanın hangi
    tarafının orijinden uzak olduğunu ölçer. Ölçüm "+Y" dedi; aynı
    gövdenin -Y'den alınan render'ı **yüzü** gösterdi. Sayı yanlış,
    resim doğruydu.

    ## Ayak neden şüpheye yer bırakmıyor

    Ayak bileğinden parmak ucuna olan mesafe, topuğa olanın yaklaşık
    iki katıdır — ve bu her insanda böyledir. Tek şart doğru referans:
    gövde merkezi değil, **baldırın kendisi**. `karakter_kit`'in eski
    hatası tam buydu; oradaki not "referans olarak bütün köşelerin
    ağırlık merkezini alıyordu" diyor.

    Referans: z = 0,06-0,11 bandındaki (alt baldır) köşelerin y
    ortalaması. Taban: z < 0,02 (yere basan taban).
    """
    vs = [v.co for v in o.data.vertices]
    zs = [v.z for v in vs]
    zmin, zmax = min(zs), max(zs)
    boy = zmax - zmin
    baldir = [v.y for v in vs
              if zmin + boy * 0.035 <= v.z <= zmin + boy * 0.065]
    taban = [v.y for v in vs if v.z <= zmin + boy * 0.012]
    if not baldir or not taban:
        return 0
    ref = sum(baldir) / len(baldir)
    ileri = max(taban) - ref          # +Y yonunde tasma
    geri = ref - min(taban)           # -Y yonunde tasma
    if abs(ileri - geri) < boy * 0.005:
        return 0                      # ayirt edilemiyor
    return 1 if ileri > geri else -1


def one_cevir(obj, hedef=Vector((0.0, -1.0, 0.0)), birlikte=()):
    """Ölçülen yönü Blender **-Y**'ye (Unity +Z) döndürür.

    Dönüş nesne dönüşümüne değil **ağa** uygulanır: pipeline'in geri
    kalanı kimlik dönüşümü bekler (bkz. `donuk_kutu`'nun acı dersi —
    nesne dönüşümü bırakmak dünyanın orijini etrafında dönmeye yol açar).
    """
    d, guven = yonu_olc(obj)
    if guven < 0.5:
        # Istatistiksel olcum karar veremiyorsa AYAGA sorulur. Ayak
        # bileginden parmak ucuna olan mesafe topuga olanin ~iki
        # katidir ve bu her insanda boyledir; `on_yonu` bunu olcer.
        #
        # Once burada yalnizca "olcum zayif, dondurulmedi" yazip
        # geciyordum. Bunun ne demek oldugu olculdu: MPFB govdesinde
        # guven 0,40 cikti, yani hem donus HEM DE "burun +Y'de
        # kalmasin" degismezi atlandi ve karakter sirti donuk
        # uretilebilirdi. Bir olcum karar veremiyorsa yapilacak sey
        # susmak degil, BASKA BIR SEYI OLCMEKTIR.
        yon = on_yonu(obj)
        if yon == 0:
            hz.log(f"UYARI {obj.name}: yon ne siluetten ne ayaktan "
                   f"olculebildi (guven {guven:.2f}); dondurulmedi.")
            return 0.0
        if yon > 0:
            # BIRLIKTE DONEN NESNELER.
            #
            # Goz kuresi ayri bir mesh ve govdeyle AYNI donusumleri
            # almak zorunda. Ayri hesaplansaydi bir gun biri degisir,
            # oteki eskirdi — bu depoda uc kez odenen kusur.
            _m = Matrix.Rotation(math.pi, 4, "Z")
            obj.data.transform(_m)
            for _e in birlikte:
                if _e is not None:
                    _e.data.transform(_m)
                    _e.data.update()
            obj.data.update()
            hz.log(f"{obj.name}: yon ayaktan olculdu, 180 derece donduruldu.")
            return math.pi
        return 0.0
    aci = math.atan2(hedef.y, hedef.x) - math.atan2(d.y, d.x)
    # En kucuk esdeger donusu sec.
    while aci > math.pi:
        aci -= 2 * math.pi
    while aci < -math.pi:
        aci += 2 * math.pi
    if abs(aci) >= math.radians(1.0):
        c, s = math.cos(aci), math.sin(aci)
        for v in obj.data.vertices:
            x, y = v.co.x, v.co.y
            v.co.x, v.co.y = x * c - y * s, x * s + y * c
        obj.data.update()

    # INVARYANT: donusten SONRA omuz ekseni (x) derinlik ekseninden (y)
    # genis olmali. Insan genis ve incedir. Ilk yazimda yon olcumu
    # govdeyi 82 derece yan cevirdi ve bu denetim olsaydi o anda patlardi;
    # onun yerine iki adim sonra omuz genisligi 0,247 m olarak cikti.
    # INVARYANT 2: DONUSTEN SONRA BURUN -Y'DE OLMALI.
    #
    # Asagidaki genislik/derinlik denetimi 180 dereceye KORDUR: omuz
    # ekseni 180 donusten sonra hala X, derinlik hala Y'dir, kontrol
    # gecer. Nitekim gecti — tabani 180 ters cevirdik ve uretim hicbir
    # hata vermeden tamamlandi. Hata ancak oyunda, kamera arkaya
    # gectiginde goruldu.
    d2, guven2 = yonu_olc(obj)
    if guven2 >= 0.5 and d2.y > 0.0:
        raise ValueError(
            f"{obj.name}: donusten SONRA burun +Y'de. Hedef -Y "
            f"(Unity +Z). Yon olcumu ters calisiyor — 180 derece "
            f"donmus bir karakter uretilmek uzereydi.")

    mn, mx = hz.bounds(obj)
    if (mx[0] - mn[0]) <= (mx[1] - mn[1]):
        raise ValueError(
            f"{obj.name}: donusten sonra genislik {mx[0]-mn[0]:.3f} m, "
            f"derinlik {mx[1]-mn[1]:.3f} m — govde yan duruyor.")
    return aci


def normalize(obj, hedef_boy=HEDEF_BOY, birlikte=()):
    """Boyu hedefe ölçekler, ayakları z=0'a, gövdeyi x=0'a oturtur.

    Pivot **iki ayağın arasında, zeminde**. Kutunun merkezi değil: bir
    karakter zemine basar ve bütün animasyon ayak temasından okunur.
    Merkeze oturtsaydım her klipte yarım metre kayma olurdu.
    """
    mn, mx = hz.bounds(obj)
    boy = mx[2] - mn[2]
    if boy <= 1e-6:
        raise ValueError(f"{obj.name}: boy sifir.")
    k = hedef_boy / boy

    # Yatay merkez: govde ekseni (ayaklarin ortasi degil — ayaklar
    # ayrik durabilir ve o zaman merkez iki ayagin ortasidir; ikisi de
    # ayni yere duser cunku govde simetriktir).
    cx = (mn[0] + mx[0]) * 0.5
    cy = (mn[1] + mx[1]) * 0.5
    for v in obj.data.vertices:
        v.co.x = (v.co.x - cx) * k
        v.co.y = (v.co.y - cy) * k
        v.co.z = (v.co.z - mn[2]) * k
    obj.data.update()

    mn2, mx2 = hz.bounds(obj)
    yeni = mx2[2] - mn2[2]
    if abs(yeni - hedef_boy) > BOY_TOLERANS:
        raise ValueError(f"{obj.name}: boy {yeni:.4f} m, hedef {hedef_boy} m.")
    return k


def olcu_al(obj):
    """Antropometrik ölçüler — uyarlamanın **kanıtı**.

    Render "doğru görünüyor" der; bu sayılar doğru OLUP OLMADIĞINI söyler.

    ## Ölçünün adı ölçtüğü şey olmalı

    İlk yazımda iki alan yanlış adlandırılmıştı ve yanlış ad, yanlış
    sayıdan daha sinsidir — çünkü sayıya bakan kişi adın doğru olduğunu
    varsayar:

    - `kulac` diye yazdığım şey gövdenin **en geniş yatay açıklığıydı**.
      Taban ağ A-pozundadır, kollar yanda durur; kulaç (kol açıklığı)
      ancak T-pozunda ölçülür. 0,886 m'yi "kulaç" sanan biri, kulaç ≈ boy
      kuralına bakıp gövdeyi bozuk sanırdı.
    - `omuz_genisligi` diye yazdığım şey boyun %78'indeki açıklıktı; orası
      omuz değil, üst kol hizasıdır ve 0,508 m veriyordu.

    Omuz artık **bulunuyor**: baştan aşağı tarandığında yatay açıklığın en
    daraldığı yer BOYUNDUR; omuz onun hemen altındadır. Bu bir tahmin
    değil, ağın kendi biçiminden okunan bir yer.
    """
    mn, mx = hz.bounds(obj)
    boy = mx[2] - mn[2]
    mw = obj.matrix_world
    vs = [mw @ v.co for v in obj.data.vertices]

    def acik(z, kalinlik):
        dilim = [v.x for v in vs if abs(v.z - z) < kalinlik]
        return (max(dilim) - min(dilim)) if len(dilim) > 1 else 0.0

    # BOYUN: bas-govde birlesimindeki en dar kot.
    #
    # Arama araligi %78-%88 ile SINIRLI ve bu sinir bir suslemeden ibaret
    # degil. Ilk yazimda "ust %30'un en dar dilimi" diye aradim ve olcum
    # KAFATASININ TEPESINI buldu: en tepede yatay aciklik sifira gider,
    # yani her zaman boyundan dardir. Boyun 0,088 m, omuz 0,151 m cikti —
    # ikisi de bir bebege bile dar. Bir "en kucugu bul" araması, aradigi
    # seyin nerede olabilecegini bilmiyorsa hep kenari bulur.
    kal = boy * 0.008
    adaylar = [(acik(mn[2] + boy * (0.78 + 0.005 * i), kal),
                mn[2] + boy * (0.78 + 0.005 * i)) for i in range(21)]
    adaylar = [(a, z) for a, z in adaylar if a > boy * 0.03]
    boyun_a, boyun_z = min(adaylar) if adaylar else (0.0, mn[2] + boy * 0.82)

    # OMUZ: boynun altindaki 12 cm icinde EN GENIS dilim. Tek bir kot
    # secmek kirilgan olurdu — omuz egimi govdeden govdeye degisir; en
    # genis dilimi aramak o degiskenlige bagisiktir.
    omuz = max(acik(boyun_z - 0.12 * i / 8.0, kal) for i in range(9))

    # BAS BOYU: boynun en dar kotundan tepeye.
    #
    # Burada once "en ust %13" yaziyordu ve bu bir olcum DEGILDI: bas
    # boyu tanim geregi boyun 0,13'u oluyor, dolayisiyla `bas_orani` her
    # govdede 1/7,69 cikiyordu. Sabiti bolup sonucu "olculen oran" diye
    # yazmak — ve `denetle` ile "1/7 ile 1/8 arasinda olmali" diye
    # denetlemek — kendi kendini onaylayan bir denetimdi: bir cocugun
    # buyuk kafasi da, bir devin kucuk kafasi da ayni 1/7,69'u verirdi.
    # Yedi arketip uretmeye baslayinca ortaya cikti; tek govde varken
    # yanlis oldugu hic gorunmemisti.
    #
    # Boynun en dar kotu zaten YUKARIDA bulunuyor (agin kendi biciminden).
    # Cene ondan birkac milimetre yukaridadir, yani bu olcu bas + kisa
    # bir boyun payidir; sabit degildir ve govde degisince degisir.
    bas_boy = mx[2] - boyun_z

    return dict(boy=round(boy, 4),
                bas_boy=round(bas_boy, 4),
                bas_orani=round(boy / bas_boy, 2) if bas_boy > 1e-6 else 0.0,
                en_genis=round(mx[0] - mn[0], 4),
                derinlik=round(mx[1] - mn[1], 4),
                boyun_genisligi=round(boyun_a, 4),
                boyun_kotu=round(boyun_z, 4),
                omuz_genisligi=round(omuz, 4),
                vertex=len(obj.data.vertices),
                ucgen=hz_tri(obj))


def hz_tri(obj):
    """Üçgen sayısı (n-gon'lar üçgenlenmiş gibi sayılır)."""
    return sum(max(0, len(p.vertices) - 2) for p in obj.data.polygons)


def desimasyon(obj, hedef_oran, ad):
    """LOD için basitleştirilmiş kopya.

    Blender'ın Decimate modifier'ı kullanılır: elle yazılmış bir
    basitleştirici burada yanlış olurdu — kenar çökertme sırası
    deformasyonu bozmadan yapılmalı ve bu çözülmüş bir problem.
    """
    kopya = obj.copy()
    kopya.data = obj.data.copy()
    kopya.name = ad
    kopya.data.name = ad
    hz.link(kopya, None)
    m = kopya.modifiers.new("dec", "DECIMATE")
    m.ratio = hedef_oran
    dg = bpy.context.evaluated_depsgraph_get()
    eva = kopya.evaluated_get(dg)
    yeni = bpy.data.meshes.new_from_object(eva)
    kopya.modifiers.clear()
    eski = kopya.data
    kopya.data = yeni
    kopya.data.name = ad
    bpy.data.meshes.remove(eski)
    return kopya


def uv_var_mi(obj):
    """Taban ağın UV'si var mı — doku için şart."""
    return bool(obj.data.uv_layers)


def temiz_ag(obj):
    """Çift vertex ve gevşek geometriyi temizler; normalleri düzeltir."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return obj
