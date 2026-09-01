"""
Hezarfen: 1632 — Rig kiti (Faz 5).

## Neden Rigify değil

Plan Bölüm 10 "Blender Rigify → Unity Humanoid retarget" diyor. Rigify'ın
verdiği şey **etkileşimli animasyon için IK/FK kontrol iskeletidir** ve
yüzden fazla kemik üretir. Bu projede animasyonu ben scriptle üretiyorum;
kontrol kolları hiç kullanılmayacak. Geriye kalan iş — deform kemiklerini
Unity Humanoid adlarına eşlemek — Rigify'ın adlandırmasıyla (`DEF-
upper_arm.L`) fazladan bir çeviri katmanı demek.

Onun yerine **Unity Humanoid'in tam istediği iskelet** doğrudan kuruluyor:
22 kemik, Unity adlarıyla, hiyerarşisi Humanoid'in beklediği gibi.

Karşılığında kaybedilen şey: Blender'da elle poz vermek zorlaşır (IK yok).
Kazanılan: eşleme katmanı yok, kemik sayısı beşte bir, ve her kemiğin yeri
**ölçülmüş** bir sayıdır. Gerekirse Rigify sonradan bu iskelete takılabilir.

Gerekçe ve Caner'e sorulan hâli: ADR 0066.

## Eklem yerleri ölçülür, tablodan alınmaz

Antropometrik oran tabloları (baş boyu, kol uzunluğu) bir gövde için
ortalamadır; elimdeki gövde için **doğru** değildir. Kit uzuvların
merkez çizgisini ağı dilimleyerek çıkarır ve eklemleri o çizgi üzerinde
bulur. A-pozundaki bir kolu dikey varsaymak dirseği 10 cm yanlış yere
koyardı.
"""

import math

import bpy
from mathutils import Vector

import hz_blender as hz

#: Unity Humanoid'in zorunlu kemikleri. Sıra hiyerarşi sırasıdır.
#: (ad, ebeveyn)
HUMANOID = [
    ("Hips", None),
    ("Spine", "Hips"),
    ("Chest", "Spine"),
    ("UpperChest", "Chest"),
    ("Neck", "UpperChest"),
    ("Head", "Neck"),
    ("LeftShoulder", "UpperChest"),
    ("LeftUpperArm", "LeftShoulder"),
    ("LeftLowerArm", "LeftUpperArm"),
    ("LeftHand", "LeftLowerArm"),
    ("RightShoulder", "UpperChest"),
    ("RightUpperArm", "RightShoulder"),
    ("RightLowerArm", "RightUpperArm"),
    ("RightHand", "RightLowerArm"),
    ("LeftUpperLeg", "Hips"),
    ("LeftLowerLeg", "LeftUpperLeg"),
    ("LeftFoot", "LeftLowerLeg"),
    ("LeftToes", "LeftFoot"),
    ("RightUpperLeg", "Hips"),
    ("RightLowerLeg", "RightUpperLeg"),
    ("RightFoot", "RightLowerLeg"),
    ("RightToes", "RightFoot"),
]

#: Uzuv oranları — merkez çizgisi ÜZERİNDE nereye düştükleri.
#: Kol: üst kol %45, önkol %37, el %18 (dirsek 0,45'te, bilek 0,82'de).
DIRSEK_T = 0.45
BILEK_T = 0.82


def _dilim_merkezi(vs, z, kal, filtre=None):
    """Bu kottaki noktaların merkezi (yoksa None)."""
    s = [v for v in vs if abs(v.z - z) < kal and (filtre is None or filtre(v))]
    if len(s) < 3:
        return None
    n = float(len(s))
    return Vector((sum(v.x for v in s) / n,
                   sum(v.y for v in s) / n,
                   sum(v.z for v in s) / n))


def uzuv_cizgisi(obj, z_ust, z_alt, filtre, adim=40, kesintide_dur=True):
    """Bir uzvun merkez çizgisi: yukarıdan aşağı dilimlerin merkezleri.

    `kesintide_dur` **uzuv bittiğinde durur** — ama boşluk boşluk değildir.

    ## İki ayrı boşluk, tek eşik

    İlk yazımda boş dilimleri atlıyordum. Kolun filtresi "gövde ekseninden
    uzak noktalar"dı ve o filtre parmak uçlarının 55 cm altında AYAKLARI da
    yakalıyordu. Çizgi omuzdan ayağa iniyor, %82'sine yürüyünce bilek
    **ayak bileği hizasında** çıkıyordu: kot %14,6, oysa olması gereken
    ~%40. Parmak ucu ile ayak arasındaki boşluk gerçek bir **yapısal**
    işarettir — atlamak yerine ona GÜVENMEK gerekir.

    Sonra bunu "ilk boş dilimde dur" diye yazdım ve düzeltmesi gereken
    hatanın ikizini ürettim. Yeni taban gövdenin baldırında köşe satırları
    seyrektir; ölçülen boşluklar 2,6 / 4,0 / **4,8** / 3,0 cm. Dilim
    kalınlığı 4,1 cm. Yani 4,8 cm'lik bir **örnekleme** boşluğu, uzvun
    bittiği sanıldı: bacak çizgisi 0,238 m'de kesildi, diz kotu 0,297'den
    0,346'ya kaydı ve rig denetimi haklı olarak reddetti.

    İki boşluk arasında ölçülmüş geniş bir açıklık var — 4,8 cm ile 55 cm.
    Eşik ortadadır: boyun %8'i (≈13,6 cm). Örnekleme boşluğunun üç katı,
    yapısal boşluğun dörtte biri.
    """
    mn, mx = hz.bounds(obj)
    boy = mx[2] - mn[2]
    kal = boy * 0.012
    #: Uzvun bittigine karar vermek icin gereken KESINTISIZ bos yukseklik.
    bosluk_tol = boy * 0.08
    mw = obj.matrix_world
    vs = [mw @ v.co for v in obj.data.vertices]
    nokta = []
    dilim_yuk = abs(z_alt - z_ust) / float(max(1, adim))
    bos = 0.0
    for i in range(adim + 1):
        z = z_ust + (z_alt - z_ust) * (i / float(adim))
        m = _dilim_merkezi(vs, z, kal, filtre)
        if m is None:
            bos += dilim_yuk
            if kesintide_dur and nokta and bos > bosluk_tol:
                break
            continue
        bos = 0.0
        nokta.append(m)
    return nokta


def cizgide_ilerle(nokta, t):
    """Merkez çizgisi üzerinde **yay uzunluğuna göre** `t` oranındaki nokta.

    Dizinle (`nokta[int(t*n)]`) almak yanlış olurdu: dilimler eşit kot
    aralıklı, ama kol yana açıldığı için eşit UZUNLUKTA değil. Dirseği
    dizinle bulmak onu birkaç santim yanlış yere koyardı.
    """
    if not nokta:
        return None
    if len(nokta) == 1:
        return nokta[0].copy()
    boy = [0.0]
    for a, b in zip(nokta, nokta[1:]):
        boy.append(boy[-1] + (b - a).length)
    hedef = boy[-1] * min(1.0, max(0.0, t))
    for i in range(1, len(boy)):
        if boy[i] >= hedef:
            araluk = boy[i] - boy[i - 1]
            u = 0.0 if araluk < 1e-9 else (hedef - boy[i - 1]) / araluk
            return nokta[i - 1].lerp(nokta[i], u)
    return nokta[-1].copy()


def eklemleri_olc(obj, kol_esik, z_kol_alt=None):
    """Gövdeden **ölçülen** eklem noktaları. `{ad: Vector}` döner.

    `z_kol_alt` verilirse kol yalnız o kotun **üstünde** aranır. Gerekçe
    `kiyafet_kit.kol_ayirici`'da: bu duruşta bacak (|x| 0,24) koldan
    (0,17) daha dışarıdadır, yani tek bir |x| eşiği bacağı kol sanır.
    Sanınca da bacak çizgisi baldırın dışını kaybeder ve diz kotu
    0,297'den 0,353'e kayar — diz yukarı çıkmaz, ölçüsü bozulur.
    """
    mn, mx = hz.bounds(obj)
    boy = mx[2] - mn[2]
    mw = obj.matrix_world
    vs = [mw @ v.co for v in obj.data.vertices]
    j = {}

    # --- GOVDE EKSENI: kollar ve bacaklar disarida --------------------
    def govde(v):
        return abs(v.x) < kol_esik

    for ad, oran in (("Hips", 0.530), ("Spine", 0.600), ("Chest", 0.685),
                     ("UpperChest", 0.762)):
        m = _dilim_merkezi(vs, boy * oran, boy * 0.015, govde)
        j[ad] = m if m is not None else Vector((0.0, 0.0, boy * oran))
        j[ad].x = 0.0                      # govde ekseni tam ortada

    # BOYUN ve BAS icin DAR filtre.
    #
    # `govde` filtresi "kollar disarida" demektir ve govde kaliniginda
    # dogru calisir. Ama boyun hizasinda ayni filtre TRAPEZ KASINI da
    # sayar: dilim boyun degil, omuz platosudur. Ilk yazimda boyun
    # y = -0,025 (onde), kafatasi y = +0,059 (arkada) cikti — aralarinda
    # 15 cm kotta 8,4 cm'lik bir kirik. Boyun dar bir SUTUNDUR; onu
    # ancak dar bir filtre gorur.
    def boyun_sutunu(v):
        return abs(v.x) < boy * 0.055

    bn = _dilim_merkezi(vs, boy * 0.838, boy * 0.015, boyun_sutunu)
    j["Neck"] = Vector((0.0, bn.y if bn else 0.0, boy * 0.838))

    # BAS eklemi (atlanto-oksipital): kafatasinin ekseni uzerindedir,
    # cene hizasindaki dilimin merkezinde DEGIL. Ilk yazimda 0,878'deki
    # dilimin merkezini aliyordum ve o kot cene/agiz hizasi: eklem 6,9 cm
    # ARKAYA dusuyordu, boyunla arasinda 9,4 cm'lik bir kirik olusuyordu.
    # Bas eklemi boynun TAM USTUNDE durur: atlanto-oksipital eklem
    # boyun sutununun devamidir ve notr pozda kafa dik oturur. Kafatasi
    # kesitinin merkezini almak yanlisti — o merkez yuzun one, ensenin
    # arkaya tasmasinin ortalamasidir, eklemin yeri degil.
    j["Head"] = Vector((0.0, j["Neck"].y, boy * 0.878))

    # --- KOLLAR ---------------------------------------------------------
    #
    # Omuz eklemi ÖLÇÜLEMEZ: koltuk altının üstünde kol ile gövde tek
    # parçadır, aralarında boşluk yoktur, yani dilimleyerek ayıramazsın.
    # O yüzden omuz eklemi omuz GENİŞLİĞİNDEN türetilir; ölçülebilen ilk
    # nokta koltuk altıdır ve çizgi oradan aşağı iner.
    #
    # İlk yazımda çizgiyi kalçada ölçülmüş TEK bir eşikle çıkarıyordum.
    # O eşik (0,256 m) omuz hizasında kolu hiç yakalamıyordu — kol orada
    # gövdeye 0,20 m'de yapışık — ve çizgi koltuk altından başlıyordu.
    # Sonuç: üst kol eklemi %74,6 kotta, 12 cm aşağıda.
    omuz_slc = [v.x for v in vs if abs(v.z - boy * 0.80) < boy * 0.012]
    yari_omuz = (max(omuz_slc) - min(omuz_slc)) * 0.5 if omuz_slc else boy * 0.13

    for yan in ("Left", "Right"):
        # Unity(x,y,z) = Blender(-x, z, -y): Blender +x, Unity'de -x'tir
        # ve Unity'de -x SAĞ'dır. Yani Unity SOL = Blender -x.
        bx = -1.0 if yan == "Left" else 1.0

        def kol_bolge(v, bx=bx):
            return (v.x * bx > 0 and abs(v.x) >= kol_esik
                    and (z_kol_alt is None or v.z >= z_kol_alt))

        kol = [v for v in vs if kol_bolge(v)]
        if not kol:
            continue

        # Glenohumeral eklem: omuz yarı genişliğinin ~%82'si, kot %81.
        omuz_ekl = Vector((bx * yari_omuz * 0.82, 0.0, boy * 0.810))
        j[f"{yan}Shoulder"] = Vector((bx * yari_omuz * 0.26, 0.0, boy * 0.822))
        j[f"{yan}UpperArm"] = omuz_ekl

        # Koltuk altından aşağı: boşlukta DURUR, yani parmak ucunda biter.
        cizgi = uzuv_cizgisi(obj, boy * 0.78, 0.0, kol_bolge)
        if not cizgi:
            continue
        tam = [omuz_ekl] + cizgi
        j[f"{yan}LowerArm"] = cizgide_ilerle(tam, DIRSEK_T)
        j[f"{yan}Hand"] = cizgide_ilerle(tam, BILEK_T)

    # --- BACAKLAR --------------------------------------------------------
    for yan, sx in (("Left", 1.0), ("Right", -1.0)):
        bx = -sx
        def bacak(v, bx=bx):
            # "Kol degilse bacaktir" — baldirin disi de bacaktir.
            return v.x * bx > 0 and not (
                abs(v.x) >= kol_esik
                and (z_kol_alt is None or v.z >= z_kol_alt))
        z_kalca = boy * 0.520
        cizgi = uzuv_cizgisi(obj, z_kalca, boy * 0.045, bacak)
        if not cizgi:
            continue
        j[f"{yan}UpperLeg"] = cizgi[0].copy()
        # Diz: uyluk %48, baldir %52 — diz bu yuzden tam ortada degil.
        j[f"{yan}LowerLeg"] = cizgide_ilerle(cizgi, 0.48)
        ayak = _dilim_merkezi(vs, boy * 0.042, boy * 0.02, bacak)
        j[f"{yan}Foot"] = ayak if ayak is not None else cizgi[-1].copy()
        # Parmak ucu: ayaktaki EN ON nokta (-y), zeminde.
        ayak_vs = [v for v in vs if v.z < boy * 0.05 and bacak(v)]
        if ayak_vs:
            on = min(ayak_vs, key=lambda v: v.y)
            j[f"{yan}Toes"] = Vector((j[f"{yan}Foot"].x,
                                      on.y + boy * 0.030, boy * 0.012))
        else:
            j[f"{yan}Toes"] = j[f"{yan}Foot"] + Vector((0.0, -boy * 0.06, 0.0))

    return j


def iskelet_kur(ad, eklem, col=None, uc_uzunluk=0.06):
    """Ölçülen eklemlerden Unity Humanoid iskeletini kurar."""
    arm = bpy.data.armatures.new(ad)
    obj = bpy.data.objects.new(ad, arm)
    hz.link(obj, col)

    onceki = bpy.context.view_layer.objects.active
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")

    # Cocuklarin listesi: her kemigin ucu, tek cocugunun basidir.
    cocuk = {}
    for kemik, ebeveyn in HUMANOID:
        cocuk.setdefault(ebeveyn, []).append(kemik)

    for kemik, ebeveyn in HUMANOID:
        if kemik not in eklem:
            continue
        eb = arm.edit_bones.new(kemik)
        eb.head = eklem[kemik]
        cs = [c for c in cocuk.get(kemik, []) if c in eklem]
        if len(cs) == 1:
            eb.tail = eklem[cs[0]]
        elif cs:
            # Cok cocuklu kemik (Hips, UpperChest): uc, cocuklarin
            # ORTALAMASINA dogru uzar — yoksa yon keyfi olur.
            o = sum((eklem[c] for c in cs), Vector()) / len(cs)
            yon = (o - eb.head)
            eb.tail = eb.head + (yon.normalized() * uc_uzunluk
                                 if yon.length > 1e-6
                                 else Vector((0.0, 0.0, uc_uzunluk)))
        else:
            # Uc kemik (Head, Hand, Toes): kendi yonunde uzar.
            ebeveyn_var = ebeveyn is not None and ebeveyn in eklem
            yon = ((eb.head - eklem[ebeveyn]) if ebeveyn_var
                   else Vector((0.0, 0.0, 1.0)))
            eb.tail = eb.head + (yon.normalized() * uc_uzunluk
                                 if yon.length > 1e-6
                                 else Vector((0.0, 0.0, uc_uzunluk)))
        # Sifir uzunluklu kemik Blender'da SESSIZCE silinir.
        if (eb.tail - eb.head).length < 1e-4:
            eb.tail = eb.head + Vector((0.0, 0.0, uc_uzunluk))

    for kemik, ebeveyn in HUMANOID:
        # `ebeveyn is None` (Hips) korumasi sart: bpy koleksiyonu None ile
        # sorgulanamaz ve dogrudan TypeError atar.
        if ebeveyn is None:
            continue
        if kemik in arm.edit_bones and ebeveyn in arm.edit_bones:
            arm.edit_bones[kemik].parent = arm.edit_bones[ebeveyn]
            arm.edit_bones[kemik].use_connect = False

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = onceki
    return obj


def deri_bagla(mesh_obj, arm_obj):
    """Ağı iskelete otomatik ağırlıkla bağlar."""
    bpy.ops.object.select_all(action="DESELECT")
    mesh_obj.select_set(True)
    arm_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    return mesh_obj


def agirliklari_tamamla(mesh_obj, arm_obj):
    """Ağırlıksız kalan köşeleri **en yakın kemiğe** bağlar.

    ## Neden gerekli

    `ARMATURE_AUTO` ısı yayılımıyla ağırlık dağıtır ve kapalı, ayrık
    kabuklarda (gömlek, entari, kuşak) çözüm bulamaz: ölçüldü, LOD0'da
    27.624 köşenin 2.964'ü (%10,7) hiçbir kemiğe bağlanmıyordu. Unity'de
    o köşeler kemik 0'a, yani köke düşer ve giysi adaları gövde oynarken
    kalçaya çakılı kalır.

    ## Neden en yakın kemik, neden daha akıllısı değil

    Giysi kabuğu gövdeden **kopyalanarak** üretiliyor
    (`kiyafet_kit.kopya_kabuk`), yani her köşe zaten tenin bir köşesinin
    birkaç santim dışında duruyor. O tenin takip ettiği kemik, giysinin
    de takip etmesi gereken kemiktir. Daha zarif bir çözüm (tenin
    ağırlıklarını en yakın komşudan aktarmak) daha doğru olurdu ama
    ölçülebilir farkı belirsiz; kaba çözüm sıfır ağırlıklı köşe
    bırakmıyor ve ölçü bunu söylüyor.

    Dönüş: doldurulan köşe sayısı.
    """
    kemikler = [(b.name, arm_obj.matrix_world @ ((b.head_local + b.tail_local) * 0.5))
                for b in arm_obj.data.bones]
    if not kemikler:
        return 0

    gruplar = {ad: (mesh_obj.vertex_groups.get(ad)
                    or mesh_obj.vertex_groups.new(name=ad))
               for ad, _ in kemikler}

    mw = mesh_obj.matrix_world
    dolduruldu = 0
    for v in mesh_obj.data.vertices:
        w = 0.0
        for g in v.groups:
            w += g.weight
        if w > 1e-6:
            continue
        p = mw @ v.co
        en_ad, en_d = kemikler[0][0], (p - kemikler[0][1]).length
        for ad, orta in kemikler[1:]:
            d = (p - orta).length
            if d < en_d:
                en_d, en_ad = d, ad
        gruplar[en_ad].add([v.index], 1.0, "REPLACE")
        dolduruldu += 1
    return dolduruldu


def agirliksiz_kose(mesh_obj):
    """Hiçbir kemiğe bağlanmamış köşe sayısı — **kusurun ölçüsü**.

    ## Neden bu sayı önemli

    Oyun içi karelerde gömlek gövdenin önünde ayrı bir levha, entari
    eteği bacağın yanında bağımsız bir tabaka olarak duruyordu; Blender'ın
    bind-poz kontak sayfası ise tertemizdi. Yani kusur modelde değil
    **deri bağlamada**.

    Şüpheli belli: `ARMATURE_AUTO` ısı yayılımıyla ağırlık dağıtır ve
    birleştirilmiş ağdaki **kapalı, ayrık kabuklarda** (gömlek, entari,
    kuşak — hepsi `kopya_kabuk` + kalınlık) çözüm bulamaz. Ağırlıksız
    kalan köşe Unity'de kemik 0'a, yani köke düşer: gövde animasyon
    oynarken o adalar kalçaya çakılı kalır.

    Ama bu bir **hipotez**; sayı olmadan düzeltmeye kalkmak, bu projede
    beş kez yanlış şeyi düzeltmekle sonuçlandı. Önce ölçülür.
    """
    toplam = len(mesh_obj.data.vertices)
    agirliksiz = 0
    for v in mesh_obj.data.vertices:
        w = 0.0
        for g in v.groups:
            w += g.weight
        if w <= 1e-6:
            agirliksiz += 1
    return agirliksiz, toplam


def kemik_raporu(arm_obj, boy):
    """Kemiklerin **ölçülebilir** kaydı — inceleme ve test için.

    Sözlük değil **liste** döner. Sebep tarz değil: Unity'nin
    `JsonUtility`'si keyfi anahtarlı sözlük okuyamaz, ve bu kayıt orada
    teste bağlanıyor. Okunamayan bir kayıt, kayıt değildir.
    """
    return [dict(ad=b.name,
                 bas=[round(c, 4) for c in b.head_local],
                 uzunluk=round(b.length, 4),
                 kot_orani=round(b.head_local.z / boy, 4))
            for b in arm_obj.data.bones]


def uzuv_denetimi(eklem, boy):
    """Eklemler bir insana ait mi — ölçüm hatasını yakalar.

    Her satır bir ORAN aralığıdır ve aralıklar genel yetişkin
    anatomisinden gelir. Amaç ince ayar değil, **yanlış yeri** yakalamak:
    dirsek bileğin altına düşmüşse ya da diz kalçanın üstündeyse ölçüm
    çizgiyi kaybetmiştir.
    """
    hata = []

    def kot(ad):
        return eklem[ad].z / boy if ad in eklem else None

    for yan in ("Left", "Right"):
        omuz, dirsek = kot(f"{yan}UpperArm"), kot(f"{yan}LowerArm")
        bilek = kot(f"{yan}Hand")
        if None not in (omuz, dirsek, bilek):
            if not (omuz > dirsek > bilek):
                hata.append(f"{yan} kol: omuz {omuz:.3f} > dirsek "
                            f"{dirsek:.3f} > bilek {bilek:.3f} olmali")
        kalca, diz, ayak = (kot(f"{yan}UpperLeg"), kot(f"{yan}LowerLeg"),
                            kot(f"{yan}Foot"))
        if None not in (kalca, diz, ayak):
            if not (kalca > diz > ayak):
                hata.append(f"{yan} bacak: kalca {kalca:.3f} > diz "
                            f"{diz:.3f} > ayak {ayak:.3f} olmali")
            if not 0.24 <= diz <= 0.32:
                hata.append(f"{yan} diz kotu {diz:.3f} — 0,24-0,32 olmali")

    # Simetri: sol ve sag ayni kotta olmali.
    for a, b in (("LeftUpperArm", "RightUpperArm"),
                 ("LeftLowerLeg", "RightLowerLeg")):
        if a in eklem and b in eklem:
            d = abs(eklem[a].z - eklem[b].z) / boy
            if d > 0.01:
                hata.append(f"{a}/{b} kot farki %{d*100:.1f} — govde simetrik")
    return hata

def etek_kemikleri(arm_obj, mesh_obj, z_bel, z_etek, zincir=4, eklem=2,
                   yaricap=None):
    """
    Eteğe **salınım kemikleri** ekler ve etek köşelerini onlara bağlar.

    ## Neden gerekti

    Caner *"yürürken dalgalansın"* dedi ve doğru görünen cevap Unity'nin
    kumaş çözücüsüydü. Ölçü onu eliyor: şehirde aynı anda 60 görünür
    gövde var, her biri 16 bin üçgen. Altmış kumaş çözücüsü 16,7 ms'lik
    kare bütçesinin tamamını yerdi.

    Kemik ise ucuz: dört zincir × iki eklem = gövde başına 8 transform,
    altmış gövdede 480. Ölçüm gürültüsü kadar.

    ## Neden Humanoid rig'i bozmuyor

    Kemikler ``Hips``'e bağlanır ve adları ``Etek_`` ile başlar. Unity
    Humanoid eşlemesi yalnızca tanıdığı kemikleri kullanır; geri kalanı
    hiyerarşide **durur** ve Mixamo klipleri onlara dokunmaz. Yani klip
    yürüyüşü oynatır, kemikleri betik sürer, ikisi çakışmaz.

    Ağırlık **kota göre** karışır: belde tamamen gövdeye bağlı, etek
    ucunda tamamen salınım kemiğine. Aksi hâlde etek belden kopar.
    """
    import math
    from mathutils import Vector

    if arm_obj is None or mesh_obj is None:
        return 0

    mn, mx = hz.bounds(mesh_obj)
    if yaricap is None:
        yaricap = max(mx[0] - mn[0], mx[1] - mn[1]) * 0.32

    onceki = bpy.context.view_layer.objects.active
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")

    arm = arm_obj.data
    if "Hips" not in arm.edit_bones:
        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.context.view_layer.objects.active = onceki
        return 0

    kalca = arm.edit_bones["Hips"]
    adlar = []
    for i in range(zincir):
        a = math.tau * i / zincir
        yon = Vector((math.cos(a), math.sin(a), 0.0))
        ebeveyn = kalca
        for j in range(eklem):
            t0 = j / float(eklem)
            t1 = (j + 1) / float(eklem)
            z0 = z_bel + (z_etek - z_bel) * t0
            z1 = z_bel + (z_etek - z_bel) * t1
            ad = f"Etek_{i}_{j}"
            eb = arm.edit_bones.new(ad)
            eb.head = Vector((yon.x * yaricap, yon.y * yaricap, z0))
            eb.tail = Vector((yon.x * yaricap, yon.y * yaricap, z1))
            eb.parent = ebeveyn
            eb.use_connect = False
            ebeveyn = eb
            adlar.append((ad, yon, z0, z1))

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = onceki

    # --- AGIRLIKLAR: kota gore karisim ------------------------------
    for ad, _, _, _ in adlar:
        if ad not in mesh_obj.vertex_groups:
            mesh_obj.vertex_groups.new(name=ad)

    mw = mesh_obj.matrix_world
    bagli = 0
    for v in mesh_obj.data.vertices:
        p = mw @ v.co
        if p.z > z_bel or p.z < z_etek - 0.02:
            continue

        # Belde 0, etek ucunda 1: salinimin payi asagi indikce artar.
        t = (z_bel - p.z) / max(1e-6, z_bel - z_etek)
        pay = min(1.0, max(0.0, t)) ** 1.6
        if pay < 0.02:
            continue

        # En yakin zincir: aci farkina gore.
        aci = math.atan2(p.y, p.x)
        en_iyi, en_fark = None, 1e9
        for ad, yon, z0, z1 in adlar:
            d = abs((math.atan2(yon.y, yon.x) - aci + math.pi)
                    % math.tau - math.pi)
            if d < en_fark:
                en_fark, en_iyi = d, (ad, z0, z1)
        if en_iyi is None:
            continue

        ad, z0, z1 = en_iyi
        mesh_obj.vertex_groups[ad].add([v.index], pay, "REPLACE")
        bagli += 1

    return bagli
