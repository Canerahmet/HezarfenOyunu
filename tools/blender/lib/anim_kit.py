"""
Hezarfen: 1632 — Animasyon kiti (Faz 5).

## Animasyonun doğruluğu ölçülebilir bir şeydir

Bir yürüyüş döngüsünün "iyi göründüğü" bir görüştür. Ama **yanlış**
olduğu bir ölçümdür ve tek bir sayıya iner:

> Yere basan ayak, gövde ilerlerken **kaymamalı.**

Adım uzunluğu ile ilerleme hızı tutmuyorsa ayaklar paten kayar. Bu, oyun
animasyonundaki en görünür kusurdur ve gözle bakınca "biraz tuhaf" diye
geçiştirilir; ölçünce santimetre verir. Kit her döngü için kaymayı
hesaplar ve eşiği aşarsa **üretimi durdurur**.

Ölçüm şöyle: temas evresindeki her karede ayağın dünya konumu ile
gövdenin o karede ilerlemesi gereken mesafe karşılaştırılır. İkisi
birbirini götürmeliydi; götürmüyorsa fark kaymadır.

## Pozlar dünya değil KEMİK uzayında verilir

Blender'da bir kemiğin yerel Y'si kemik boyunca uzanır. Bacak aşağı
baktığı için yerel X kabaca dünya X'idir ve o eksende dönmek bacağı öne
arkaya sallar. "Kabaca" yeterli değil — kit dönüşten sonra ayağın
gerçekten **ön-arka** hareket ettiğini ölçer, yana değil.

## Klip başına bir FBX

Unity'nin `model@klip.fbx` sözleşmesi kullanılıyor. Blender tek FBX'e
birden çok action yazabilir ama Unity çoğu zaman yalnızca ilkini okur ve
bunu **sessizce** yapar — beş klip yazıp bir tanesini almak, bu projenin
en sevmediği hata türü.
"""

import math

import bpy
from mathutils import Euler, Vector

import hz_blender as hz

#: Kare hızı (fps). Unity'nin klipleri bu hızda okunur.
FPS = 30

#: Temas evresinde izin verilen toplam kayma (m). Bunun ötesinde
#: ayak gözle görülür şekilde paten kayar.
KAYMA_SINIRI = 0.05

#: Kule merdiveni — rıht (basamak yüksekliği) ve basamak derinliği (m).
#: **T2, taslak:** Galata kulesinin helezon merdiveninin ölçüsü
#: kaynakta yok. Bunlar dönemin kule merdivenleri için makul
#: değerlerdir ve tırmanma klibinin hızını BELİRLER — uydurulmuş
#: bir hız yazmaktansa uydurulmuş bir geometriden türetmek yeğdir,
#: çünkü geometri sorgulanabilir bir sayıdır.
MERDIVEN_RIHT = 0.19
MERDIVEN_BASAMAK = 0.26


def _pb(arm, ad):
    return arm.pose.bones.get(ad)


def poz_ver(arm, pozlar):
    """`{kemik: (x, y, z)}` derece cinsinden yerel dönüş uygular."""
    for ad, (rx, ry, rz) in pozlar.items():
        b = _pb(arm, ad)
        if b is None:
            continue
        b.rotation_mode = "XYZ"
        b.rotation_euler = Euler((math.radians(rx), math.radians(ry),
                                  math.radians(rz)), "XYZ")
    return arm


def sifirla(arm):
    """Bütün pozları dinlenme durumuna döndürür."""
    for b in arm.pose.bones:
        b.rotation_mode = "XYZ"
        b.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
        b.location = Vector((0.0, 0.0, 0.0))
    return arm


def anahtar(arm, kare, kemikler=None):
    """Verilen kemiklerin dönüşünü bu kareye anahtarlar."""
    for b in arm.pose.bones:
        if kemikler is not None and b.name not in kemikler:
            continue
        b.keyframe_insert("rotation_euler", frame=kare)
        if b.name == "Hips":
            b.keyframe_insert("location", frame=kare)


def klip_kur(arm, ad):
    """Yeni bir Action açar ve armature'a bağlar."""
    if arm.animation_data is None:
        arm.animation_data_create()
    act = bpy.data.actions.new(ad)
    act.use_fake_user = True
    arm.animation_data.action = act
    return act


def ayak_dunya(arm, kemik="LeftToes"):
    """Kemiğin dünya uzayındaki baş konumu (poz uygulanmış)."""
    b = _pb(arm, kemik)
    if b is None:
        return None
    return (arm.matrix_world @ b.matrix).to_translation()


def dongu_olc(arm, act, hiz, kemikler=("LeftToes", "RightToes"),
              temas_esigi=0.02, dikey_hiz=0.0, temas_araligi=None):
    """Yürüyüş/koşu döngüsünün **ayak kaymasını** ölçer.

    `hiz` m/s. Döngü boyunca gövdenin ilerlemesi gereken mesafe
    `hiz * sure`'dir. Temas evresi, ayağın en alçak kaldığı karelerdir
    (zeminden `temas_esigi` kadar yukarıya kadar sayılır).

    Döner: `{kemik: {kayma, mesafe, oran}}` —
    `kayma` temas boyunca gereken ile gerçekleşen ilerlemenin farkı (m),
    `mesafe` ayağın gövdeye göre gerçekte gittiği yol (m),
    `oran` temas evresinin döngüye oranı.

    Son iki alan **çözüm** içindir: doğru döngü süresi tahminle
    aranmaz, bu iki sayıdan türetilir (bkz. `dongu_suresi_coz`).

    Not: ayak dünya konumu burada gövde ilerlemesini İÇERMEZ (kök
    hareketi Unity'de kontrolcüden gelir), yani temas evresinde ayak
    yerinde durmalıdır: ilerleme kadar geriye gitmelidir.
    """
    s0, s1 = act.frame_range
    n = int(round(s1 - s0)) + 1
    sure = (n - 1) / float(FPS)
    if sure <= 0:
        return {k: 0.0 for k in kemikler}

    sahne = bpy.context.scene
    izler = {k: [] for k in kemikler}
    for i in range(n):
        sahne.frame_set(int(round(s0)) + i)
        bpy.context.view_layer.update()
        for k in kemikler:
            p = ayak_dunya(arm, k)
            if p is not None:
                izler[k].append(p.copy())

    sonuc = {}
    for k, iz in izler.items():
        if len(iz) < 4:
            sonuc[k] = 0.0
            continue
        # Kapali dongude son kare ilkinin aynisidir; ikisini birden
        # saymak dongunun bir karesini iki kez saymak olurdu.
        if (iz[-1] - iz[0]).length < 1e-6:
            iz = iz[:-1]
        m = len(iz)
        if temas_araligi is not None:
            # BASIS EVRESI BILINIYORSA tahmin etme.
            #
            # "En alcak kareler temastir" sezgisi duz zeminde dogru, ama
            # TIRMANISTA degil: govde yukselirken ayak basis boyunca
            # zaten alcalir, yani en alcak nokta basisin SONUDUR ve
            # pencere salinimin basina kayar. Olcum 47 cm kayma bildirdi;
            # oysa yol tanim geregi kaymasizdi. Ureten taraf basis
            # evresini biliyorsa, olcen taraf onu aramamalidir.
            # Pencere KEMIGE gore verilebilir: iki ayak yarim faz
            # kaymalidir, yani ayni pencereyi ikisine birden vermek
            # sag ayagin SALINIMINI temas saymak olur. Ilk yazimda
            # oyle yaptim ve olcum 58,5 cm bildirdi — ki bu, salinim
            # boyunca ayagin one gitmesinin tam karsiligiydi.
            ara = (temas_araligi.get(k) if isinstance(temas_araligi, dict)
                   else temas_araligi)
            if ara is None:
                sonuc[k] = dict(kayma=0.0, mesafe=0.0, mesafe_z=0.0, oran=0.0)
                continue
            t0, t1 = ara
            temas = [(t0 <= ((i / float(m)) % 1.0) < t1)
                     if t0 < t1 else
                     ((i / float(m)) >= t0 or (i / float(m)) < t1)
                     for i in range(m)]
        else:
            z_min = min(p.z for p in iz)
            kalkis = max(p.z for p in iz) - z_min
            # Esik MUTLAK degil oransal: 14 cm kaldiran bir dongude 2 cm,
            # 4 cm kaldiran bir dongude 2 cm ayni sey degildir.
            esik = max(0.012, kalkis * 0.22)
            temas = [p.z <= z_min + esik for p in iz]

        # En uzun BITISIK temas dizisi — dongu DAIRESELDIR.
        #
        # Ilk yazimda `temas` listesinin ilk ve son indeksini aliyordum.
        # Ama temas evresi dongunun BASINDA ve SONUNDA olur (ayak orada
        # yerdedir), yani indeksler 0 ve n-1 cikiyor ve aradaki butun
        # dongu "temas" sayiliyordu. Olcum 149 cm kayma bildirdi; oysa
        # dongu iyiydi, ALET bozuktu. Bir olcum aleti, olctugu seyin
        # topolojisini bilmiyorsa yalan soyler.
        en_iyi = (0, 0)
        i = 0
        while i < m:
            if not temas[i]:
                i += 1
                continue
            uz = 0
            while uz < m and temas[(i + uz) % m]:
                uz += 1
            if uz > en_iyi[1]:
                en_iyi = (i, uz)
            i += max(1, uz)
        bas_i, uzunluk = en_iyi
        if uzunluk < 2 or uzunluk >= m:
            sonuc[k] = 0.0
            continue

        son_i = (bas_i + uzunluk - 1) % m
        dt = (uzunluk - 1) / float(FPS)
        gereken = hiz * dt
        # Karakter -Y'ye bakar. Govde ilerlerken temas halindeki ayak
        # dunyada yerinde kalir, yani GOVDEYE GORE geriye (+Y) gider.
        gercek = iz[son_i].y - iz[bas_i].y
        # Dikey bilesen: merdivende govde YUKSELIR, yani temas halindeki
        # ayak govdeye gore ASAGI iner. Duz zeminde `dikey_hiz` sifirdir
        # ve bu terim yok olur.
        #
        # Duz zemin olcutunu merdivene uygulamak yanlis seyi olcmekti:
        # tirmanan bir adamin ayagi basamakta durur ve govde onun
        # UZERINDEN gecer. Yatay kaymayi tek basina okuyunca 38,5 cm
        # gorunuyordu; oysa o mesafenin bir kismi tirmanistir.
        gereken_z = -dikey_hiz * dt
        gercek_z = iz[son_i].z - iz[bas_i].z
        hata = math.hypot(gercek - gereken, gercek_z - gereken_z)
        sonuc[k] = dict(kayma=hata,
                        mesafe=gercek,
                        mesafe_z=gercek_z,
                        oran=(uzunluk - 1) / float(m))
    return sonuc


def yon_denetimi(arm, kemik, eksen_kemik, aci=25.0):
    """Bu kemiği döndürmek uzvu ÖN-ARKA mı oynatıyor, yana mı?

    Kemiklerin yerel eksen yönelimi (roll) otomatik hesaplanır ve
    "bacak aşağı bakıyorsa yerel X dünya X'idir" **kabaca** doğrudur.
    Kabaca yeterli değil: yanlış eksende dönen bir bacak yürümez, yana
    açılır. Bu yüzden dönüş uygulanır ve sonuç ÖLÇÜLÜR.

    Döner: `(ileri_geri_m, yanal_m)`.
    """
    sifirla(arm)
    bpy.context.view_layer.update()
    a = ayak_dunya(arm, eksen_kemik)
    poz_ver(arm, {kemik: (aci, 0.0, 0.0)})
    bpy.context.view_layer.update()
    b = ayak_dunya(arm, eksen_kemik)
    sifirla(arm)
    bpy.context.view_layer.update()
    if a is None or b is None:
        return (0.0, 0.0)
    return (abs(b.y - a.y), abs(b.x - a.x))


# ---------------------------------------------------------------- döngüler

def yurume_karesi(t, genlik=1.0):
    """Yürüyüş döngüsünün `t` (0..1) anındaki pozu.

    Klasik dört evre: temas, geçiş, alçalma, itiş. Sinüs tabanlı, çünkü
    bir yürüyüş döngüsü periyodiktir ve elle anahtarlanan sekiz kare
    aynı şeyin daha kaba hâli olurdu.

    Kollar bacakların TERSİ salınır — insan yürüyüşünde açısal momentum
    böyle dengelenir ve aynı yönde sallanan kollar "zombi" görünür.
    """
    a = 2.0 * math.pi * t
    g = genlik
    ust = 26.0 * g * math.sin(a)             # uyluk salinimi
    ust_r = 26.0 * g * math.sin(a + math.pi)
    # Diz, salinim geriye giderken bukulur (ayak yeri temizlesin).
    diz = -(18.0 + 26.0 * g) * max(0.0, math.sin(a - math.pi * 0.35))
    diz_r = -(18.0 + 26.0 * g) * max(0.0, math.sin(a + math.pi * 0.65))
    kol = -18.0 * g * math.sin(a)
    kol_r = -18.0 * g * math.sin(a + math.pi)
    return {
        "LeftUpperLeg": (ust, 0.0, 0.0),
        "RightUpperLeg": (ust_r, 0.0, 0.0),
        "LeftLowerLeg": (diz, 0.0, 0.0),
        "RightLowerLeg": (diz_r, 0.0, 0.0),
        "LeftFoot": (-ust * 0.35, 0.0, 0.0),
        "RightFoot": (-ust_r * 0.35, 0.0, 0.0),
        "LeftUpperArm": (kol, 0.0, 0.0),
        "RightUpperArm": (kol_r, 0.0, 0.0),
        "LeftLowerArm": (-abs(kol) * 0.5 - 8.0, 0.0, 0.0),
        "RightLowerArm": (-abs(kol_r) * 0.5 - 8.0, 0.0, 0.0),
        "Spine": (0.0, 2.0 * math.sin(2 * a), 0.0),
        "Chest": (-2.0 * g, 0.0, 0.0),
    }


def suzulme_pozu(pitch=0.0, roll=0.0):
    """Süzülüş: kollar kanadın tutamağında, gövde yatay.

    Hezarfen kanadı **taşımıyor**, ona asılı. Kollar öne uzanır, gövde
    öne yatar, bacaklar arkada toplanır. Pitch/roll blend ağacının uç
    pozları bu fonksiyondan türetilir.
    """
    return {
        "Hips": (72.0 + pitch, 0.0, 0.0),
        "Spine": (-6.0, 0.0, roll * 0.25),
        "Chest": (-6.0, 0.0, roll * 0.25),
        "Neck": (-16.0, 0.0, 0.0),
        "Head": (-14.0, 0.0, 0.0),
        "LeftUpperArm": (-78.0, 0.0, -roll * 0.4),
        "RightUpperArm": (-78.0, 0.0, roll * 0.4),
        "LeftLowerArm": (-24.0, 0.0, 0.0),
        "RightLowerArm": (-24.0, 0.0, 0.0),
        "LeftUpperLeg": (-14.0, 0.0, 0.0),
        "RightUpperLeg": (-14.0, 0.0, 0.0),
        "LeftLowerLeg": (-26.0, 0.0, 0.0),
        "RightLowerLeg": (-26.0, 0.0, 0.0),
        "LeftFoot": (18.0, 0.0, 0.0),
        "RightFoot": (18.0, 0.0, 0.0),
    }


def dongu_coz(arm, poz_fn, hiz, tempo, ref_genlik=1.0, tur=8,
              kemik="LeftToes", dikey_hiz=0.0):
    """Bu adım için **hem genliği hem süreyi** çözer. `(kare, genlik, kayma)`.

    ## İki denklem

    Adım uzunluğu ile tempo bağımsız seçilemez: çarpımları HIZDIR ve hız
    zaten verilmiştir (`WalkController.walkSpeed`). Dolayısıyla:

    - Kayma sıfır:  `D(g) = hiz * f * T`
    - Tempo tutar:  `T = 120 / tempo`  (döngü = iki adım)

    Süre doğrudan tempodan gelir. Geriye tek bilinmeyen kalır: genlik.

    ## Neden yineleme, neden formül değil

    İlk yazımda `D`'nin genlikle doğrusal ölçeklendiğini varsayıp tek
    adımda çözdüm. Tempo düzeldi (110 adım/dk) ama kayma **24,6 cm**
    çıktı. Sebep: temas oranı `f` de genliğe bağlıdır — genlik küçülünce
    ayak daha az kalkar, eşiğin altında kalan kare sayısı artar, `f`
    büyür. Yani sağ taraf da hareket ediyor.

    İki değişkenin birbirine bağlı olduğu yerde doğrusal varsaymak
    ucuzdur ama yanlıştır. Onun yerine: kur, **ölç**, düzelt, tekrarla.
    Ölçüm zaten var; kullanmamak için sebep yok.
    """
    T = 120.0 / float(tempo)
    kare = max(6, int(round(T * FPS)))
    genlik = ref_genlik
    kayma = None

    for _ in range(tur):
        act = klip_kur(arm, "_coz_gecici")
        for i in range(kare + 1):
            sifirla(arm)
            poz_ver(arm, poz_fn((i % kare) / float(kare), genlik))
            anahtar(arm, i + 1)
        arm.animation_data.action = act
        d = dongu_olc(arm, act, hiz,
                      dikey_hiz=dikey_hiz).get(kemik, {})
        bpy.data.actions.remove(act)

        D, f, kayma = (abs(d.get("mesafe", 0.0)), d.get("oran", 0.0),
                       d.get("kayma", 0.0))
        if kayma <= KAYMA_SINIRI * 0.6:
            break
        if D < 1e-4 or f < 1e-4:
            break
        hedef = hiz * f * T
        # Yumusatilmis duzeltme: oran birden uygulanirsa `f`nin tepkisi
        # yuzunden salinabilir.
        oran = hedef / D
        genlik *= 1.0 + (oran - 1.0) * 0.7
        genlik = min(3.0, max(0.15, genlik))

    return kare, genlik, kayma

# ------------------------------------------------------------------- bacak IK

def bacak_boylari(arm, yan="Left"):
    """Uyluk ve baldır uzunlukları (m) — iskeletten okunur."""
    u = arm.data.bones.get(f"{yan}UpperLeg")
    b = arm.data.bones.get(f"{yan}LowerLeg")
    if u is None or b is None:
        return (0.39, 0.44)
    return (u.length, b.length)


def bacak_isaret(arm, yan="Left"):
    """Uyluk ve diz açılarının **işaretini ölçer**.

    `+10°` uygulanınca ayak ileri mi gidiyor geri mi, diz bükülünce
    topuk yukarı mı kalkıyor — bunları varsaymak yerine ölçüyoruz.
    Kemiklerin roll'u otomatik hesaplandığı için işaret iskeletten
    iskelete değişebilir ve yanlış işaretli bir IK bacağı ters büker.
    """
    sifirla(arm)
    bpy.context.view_layer.update()
    a = ayak_dunya(arm, f"{yan}Toes")
    poz_ver(arm, {f"{yan}UpperLeg": (10.0, 0.0, 0.0)})
    bpy.context.view_layer.update()
    b = ayak_dunya(arm, f"{yan}Toes")
    uyluk = -1.0 if (b.y - a.y) > 0 else 1.0     # + aci ILERI (-y) olsun

    sifirla(arm)
    bpy.context.view_layer.update()
    a = ayak_dunya(arm, f"{yan}Toes")
    poz_ver(arm, {f"{yan}LowerLeg": (10.0, 0.0, 0.0)})
    bpy.context.view_layer.update()
    b = ayak_dunya(arm, f"{yan}Toes")
    diz = 1.0 if (b.z - a.z) > 0 else -1.0       # + aci ayagi KALDIRSIN
    sifirla(arm)
    bpy.context.view_layer.update()
    return uyluk, diz


def bacak_ik(arm, yan, hedef, taban_poz=None, tahmin=(0.0, 0.0),
             tur=14, tol=0.004):
    """Ayak bileğini `hedef` **dünya** konumuna götüren `(uyluk, diz)`.

    ## Neden analitik değil, sayısal

    Kosinüs teoremiyle iki kemikli IK yazmak beş satırdır ve ben yazdım.
    Çalışmadı — çünkü formül "uyluk sıfır dönüşte tam aşağı bakar"
    varsayar. Bu iskelette öyle değil: kemiklerin roll'u otomatik
    hesaplanır, `Hips` ayrıca 12° eğiktir ve dinlenme duruşunda bacak
    zaten neredeyse tam açıktır (kalça-ayak 0,820 m, bacak 0,827 m).
    Formül ayak bileğini dünyada 60 cm öteye koyuyordu.

    Kemik çerçevesini **tahmin etmek** yerine Blender'ın kendi ileri
    kinematiğine karşı çözüyoruz: iki bilinmeyen, sonlu farkla Jacobian,
    birkaç Newton adımı. Yavaş ama **doğru**, ve doğruluğu her karede
    ölçülüyor.

    `taban_poz` çözüm sırasında sabit tutulacak gövde pozudur (`Hips`
    eğimi gibi) — onsuz çözüm başka bir gövdeye ait olurdu.
    """
    ust, alt = f"{yan}UpperLeg", f"{yan}LowerLeg"
    ayak = f"{yan}Foot"

    def yerlestir(a, b):
        sifirla(arm)
        if taban_poz:
            poz_ver(arm, taban_poz)
        poz_ver(arm, {ust: (a, 0.0, 0.0), alt: (b, 0.0, 0.0)})
        bpy.context.view_layer.update()
        return ayak_dunya(arm, ayak)

    a, b = tahmin
    for _ in range(tur):
        p0 = yerlestir(a, b)
        e = Vector((hedef[0] - p0.y, hedef[1] - p0.z))
        if e.length < tol:
            break
        h = 2.0
        pa = yerlestir(a + h, b)
        pb = yerlestir(a, b + h)
        # Jacobian: [d(y,z)/da, d(y,z)/db]
        j00, j10 = (pa.y - p0.y) / h, (pa.z - p0.z) / h
        j01, j11 = (pb.y - p0.y) / h, (pb.z - p0.z) / h
        det = j00 * j11 - j01 * j10
        if abs(det) < 1e-9:
            break
        da = (e.x * j11 - e.y * j01) / det
        db = (j00 * e.y - j10 * e.x) / det
        # Adim sinirlamasi: buyuk sicramalar tekil duruslara sapiyor.
        a += max(-25.0, min(25.0, da))
        b += max(-25.0, min(25.0, db))
        a = max(-120.0, min(120.0, a))
        b = max(-150.0, min(150.0, b))

    return a, b


def ayak_duzle(arm, yan, hedef_toe, taban_poz, uyluk, diz, tahmin=0.0,
               tur=10, tol=0.004):
    """Ayak bileği açısını çözer: parmak ucu `hedef_toe`'ya gitsin.

    ## Neden bu da çözülmeli

    İki kemikli IK ayak **bileğini** yerleştirir. Ama yere basan şey
    bilek değil **taban**dır ve parmak ucu bileğe göre sabit bir kolun
    ucundadır — o kol baldırla birlikte döner. Baldır tırmanış boyunca
    çok döndüğü için bilek doğru yerde dururken parmak ucu **61 cm**
    savruluyordu.

    Yani "ayak düz kalsın" bir üslup tercihi değil, kaymanın kendisi.
    Basan bir ayağın tabanı basamakta durur; uyluk ve diz dönerken ayak
    onları telafi eder.
    """
    ayak = f"{yan}Foot"
    toe = f"{yan}Toes"
    ust, alt = f"{yan}UpperLeg", f"{yan}LowerLeg"

    def yerlestir(c):
        sifirla(arm)
        if taban_poz:
            poz_ver(arm, taban_poz)
        poz_ver(arm, {ust: (uyluk, 0.0, 0.0), alt: (diz, 0.0, 0.0),
                      ayak: (c, 0.0, 0.0)})
        bpy.context.view_layer.update()
        return ayak_dunya(arm, toe)

    c = tahmin
    for _ in range(tur):
        p0 = yerlestir(c)
        e = Vector((hedef_toe[0] - p0.y, hedef_toe[1] - p0.z))
        if e.length < tol:
            break
        h = 3.0
        p1 = yerlestir(c + h)
        d = Vector(((p1.y - p0.y) / h, (p1.z - p0.z) / h))
        if d.length_squared < 1e-12:
            break
        # Tek serbestlik: hatanin turev yonundeki izdusumu kadar ilerle.
        adim = e.dot(d) / d.length_squared
        c += max(-30.0, min(30.0, adim))
        c = max(-90.0, min(90.0, c))
    return c


def parmak_ofseti(arm, yan):
    """Dinlenme duruşunda parmak ucunun ayak bileğine göre yeri `(dy, dz)`."""
    sifirla(arm)
    bpy.context.view_layer.update()
    a = ayak_dunya(arm, f"{yan}Foot")
    t = ayak_dunya(arm, f"{yan}Toes")
    return (t.y - a.y, t.z - a.z)
