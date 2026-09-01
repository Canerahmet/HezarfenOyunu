"""
Hezarfen: 1632 — Galata Kulesi (Faz 3'ün ilk landmark'ı).

Bu kit **tek bir yapı** içindir ve kitin geri kalanından farkı budur: kamusal
kit üyesi *çok ve tipolojiktir* (T2), landmark *tek ve belgelidir* (T1).
Bir mescidin ölçüsü uydurulabilir; Galata Kulesi'ninki uydurulamaz.

Bütün sayılar RESEARCH.md §5.1'den gelir ve orada kaynaklıdır. Aşağıda her
sabitin yanında **neden o sayı** yazar; bir gün biri onları değiştirmek
isterse neyi çürütmesi gerektiğini bilsin.

## 1632'nin kütlesi nereden çıktı — iki kot, bir zincir

1. **1509 depremi:** kule 13,20 m kotundan yukarı yeniden inşa edildi
   (Mimar Murad bin Hayreddin). Yani 1632'de görülen üst gövde Osmanlı işidir
   ve **13,20 m'deki tuğla kuşak tam o dikişin izidir.**
2. **1794 yangını:** ahşap külah ve üst katlar tamamen yandı; onarımda boy
   **1,9 m kısaltıldı**.
3. **1831 sonrası:** II. Mahmud **32,60 m'den yukarısını tamamen yıktırdı**.

(2) ve (3) birlikte 1632'nin kâgir gövdesini verir: yıkılan kot 32,60 m ve o
kot 1794'te zaten 1,9 m alçaltılmıştı → **1632'de kâgir gövde ≈ 34,5 m**.

## Külah VARDI — çağdaşı yazıyor

Evliya Çelebi kuleyi *"tepesinde kurşun kaplı bir külah"* ile tarif eder.
Yaygın İngilizce iddia (konik çatıyı 1832'de II. Mahmud ekledi) yanlıştır:
1794'te **yanan** bir ahşap külah kaydı vardır. Mahmud külahı eklemedi,
yenisini yaptı.

Evliya'nın **sayısı** kullanılmaz: 118 mimar arşını ≈ 89 m eder, ki bugünkü
62,6 m'nin bile üstündedir. Tanıklığı külahın **varlığı** için geçerlidir.

## Külahın BİÇİMİ D3'tür — bu yüzden iki varyant

Sağlam iki dönem tasvirini karşılaştırır: birinde külah *"dar ve yüksekçe"*
konidir ve **mazgallı bir siperle çevrilidir**; ötekinde çatı *"çok daha
basık ve geniş"*tir ve **saçakları mazgallardan dışarı taşar**. Hangisinin
1632'ye ait olduğu kaynaktan çıkmıyor. İkisi de üretilir; seçim Caner'in.

Eksen sözleşmesi kitin geri kalanıyla aynı: giriş cephesi −Y (Unity'de +Z).
"""

import math

import hz_blender as hz
import ottoman_kit as kit
import detay_kit as dk


# --------------------------------------------------------------- belgeli ölçüler

#: Dış çap (m) — bugün ölçülmüş; gövde 1632'de de aynı. TDV; Vikipedi.
OUTER_D = 16.45

#: Zeminde iç çap (m). Aynı kaynaklar.
INNER_D = 8.95

#: Duvar kalınlığı (m): 4. kata kadar, sonrası. Duvar İÇERİDEN incelir;
#: dış yüz düşeydir.
WALL_T_LOW = 3.75
WALL_T_HIGH = 3.00

#: Kat kotları (m) — TDV. Pencere sıraları bunlara oturur.
FLOOR_Z = (4.45, 8.97, 13.20, 17.17, 20.80)

#: Gövdedeki iki tuğla kuşağın kotu (m) — TDV. İlki 1509 onarımının dikişi.
BRICK_BAND_Z = (13.20, 17.17)

#: 1632'de kâgir gövdenin üst kotu (m). 32,60 (1831'de yıkılan kot)
#: + 1,90 (1794'te alçaltılan pay).
SHAFT_H_1632 = 34.50

#: Bugünkü toplam yükseklik (m). Model bunun ALTINDA kalmak zorunda —
#: 1632 silüeti bugünkünden alçaktır ve test bunu ölçer.
TODAY_TOTAL_H = 62.59


class GalataParams(object):
    """
    Galata Kulesi, 1632.

    `crown`:
      * ``"sacakli"`` — külah basık ve geniş, **saçağı mazgalları örter**
        (Sağlam'ın ikinci tasviri). Varsayılan.
      * ``"mazgalli"`` — külah dar ve yüksek, **mazgallı siperin içinden**
        yükselir (birinci tasvir).
    """

    def __init__(self, crown="sacakli", shaft_h=SHAFT_H_1632,
                 parapet_h=1.70, merlon_n=24, cone_h=None, eave=0.95,
                 segments=32, door_w=2.2, door_h=3.4, palette="default"):
        self.crown = crown
        self.shaft_h = shaft_h
        self.parapet_h = parapet_h
        self.merlon_n = merlon_n
        self.eave = eave
        self.segments = segments
        self.door_w, self.door_h = door_w, door_h
        self.palette = palette
        # Külah boyu biçimden türer; elle verilirse o kullanılır.
        if cone_h is not None:
            self.cone_h = cone_h
        else:
            self.cone_h = 8.5 if crown == "sacakli" else 14.0

    @property
    def outer_r(self):
        return OUTER_D * 0.5

    #: Saçaklı varyantta külahın oturduğu ahşap kasnağın boyu (m).
    #: Mazgallar bunun altında GÖRÜNÜR kalır — v1'de saçak onları yutuyordu.
    DRUM_H = 1.30

    @property
    def total_h(self):
        if self.crown == "sacakli":
            return self.shaft_h + self.parapet_h + self.DRUM_H + self.cone_h
        return self.shaft_h + self.cone_h

    def validate(self):
        if self.crown not in ("sacakli", "mazgalli"):
            raise ValueError(f"crown={self.crown} — 'sacakli' ya da 'mazgalli'")

        # ÇAP BELGELİDİR. Kule silindiriktir ve çapı ölçülmüştür; onu
        # degistirmek baska bir kule modellemektir.
        if abs(OUTER_D - 16.45) > 1e-6:
            raise ValueError("OUTER_D belgeli sayidir (16,45 m) — degistirme")

        # Kagir govde 1794/1831 kotlarindan turer. Genis bir aralik biraktim
        # ki deneme yapilabilsin, ama 1831'de yikilan kotun (32,60) altina
        # inmek ya da bugunku govdeyi asmak belgeye aykiridir.
        if not (30.0 <= self.shaft_h <= 40.0):
            raise ValueError(f"shaft_h={self.shaft_h:.1f} — 1632 kagir govdesi "
                             "32,60 + 1,90 zincirinden ~34,5 m cikar")

        # KULAH VARDI. Evliya Celebi cagdas taniktir; kulahi sifirlamak
        # kaynagi cürütmek demektir.
        if self.cone_h < 4.0:
            raise ValueError(f"cone_h={self.cone_h:.1f} — Evliya Celebi "
                             "'tepesinde kursun kapli bir kulah' der")

        # 1632 SILUETI BUGUNKUNDEN ALCAKTIR. Bugunku yukseklik 1831 ve 1875
        # eklerini icerir; onlarin hicbiri 1632'de yoktu.
        if self.total_h >= TODAY_TOTAL_H:
            raise ValueError(f"toplam {self.total_h:.1f} m — bugunku kule "
                             f"{TODAY_TOTAL_H} m ve 1632 ONDAN ALCAK olmali "
                             "(1831 sofasi ve 1875 sekizgen katlari YOK)")

        # "Basik ve genis" ile "dar ve yuksek" AYIRT EDILEBILIR olmali;
        # yoksa iki varyant uretmenin anlami kalmaz.
        if self.crown == "sacakli" and self.cone_h > OUTER_D * 0.75:
            raise ValueError("sacakli varyantta kulah 'basik ve genis' olmali")
        if self.crown == "mazgalli" and self.cone_h < OUTER_D * 0.75:
            raise ValueError("mazgalli varyantta kulah 'dar ve yuksekce' olmali")


def _ring_openings(parts, col, mat, r, z, height, width, count, depth=0.35,
                   phase=0.0, stone=None, arch=True):
    """
    Gövde çevresine eşit aralıklı, içeri gömülü açıklık sırası.

    `arch=True` her açıklığın üstüne **yarım daire kemer** basar. Kule
    Ceneviz yapısıdır (1348) ve kemerleri sivri değil yuvarlaktır; düz
    lentolu dikdörtgen delikler ise ne Ceneviz ne Osmanlı — yalnızca
    modellenmemiş demekti.
    """
    for i in range(count):
        a = 2.0 * math.pi * i / count + phase
        cx, cy = math.cos(a), math.sin(a)
        # Kutu radyal yone dik durmali; Blender'da dondurup birlestirecegiz.
        obj = hz.make_box(f"Acik_{i:02d}", (width, depth, height),
                          (0.0, 0.0, 0.0), col)
        obj.rotation_euler = (0.0, 0.0, a + math.pi * 0.5)
        obj.location = ((r - depth * 0.45) * cx, (r - depth * 0.45) * cy,
                        z + height * 0.5)
        hz.assign(obj, mat)
        parts.append(obj)
        if arch and stone is not None:
            ux, uy = -math.sin(a), math.cos(a)   # aciklik dogrultusu
            for o in dk.kemer(f"AcikKemer_{int(z * 10)}_{i:02d}",
                              (r - 0.05) * cx, (r - 0.05) * cy, ux, uy,
                              width * 0.5, z + height, 0.24, 0.30, col,
                              steps=5, sivri=False):
                hz.assign(o, stone)
                parts.append(o)


def build_galata(p, col, asset_name, textured=False):
    """Galata Kulesi (1632). `(lod0, lod1, ucx, info)` döner."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)

    r = p.outer_r
    parts, l1 = [], []

    # --- 1) Kâgir gövde -------------------------------------------------
    #
    # Silindir; duvar iceriden incelir, dis yuz DUSEYDIR. Ic bosluk
    # modellenmiyor: kule 1632'de tersane ambariydi ve ic mekan Faz 3'un
    # konusu degil (butun kitte oldugu gibi).
    shaft = hz.make_tube(f"Govde_{asset_name}", r, r, p.shaft_h,
                         base_z=0.0, segments=p.segments,
                         cap_top=False, cap_bottom=False, col=col)
    hz.assign(shaft, mats["stone"])
    parts.append(shaft)
    # Gövdenin çapı BIRLESTIRMEDEN ONCE olculur. Birlestirilmis modelin ayak
    # izi sacagi da kapsar (saçaklı varyantta 18,35 m) ve belgeli sayiyi
    # (16,45 m) dogrulamak icin yanlis olcudur. Kendi denetimim bu yuzden
    # once haksiz yere hata verdi.
    _mn, _mx = hz.bounds(shaft)
    shaft_d = max(_mx[0] - _mn[0], _mx[1] - _mn[1])

    l1.append(hz.assign(
        hz.make_tube("L1_Govde", r, r, p.shaft_h, base_z=0.0, segments=12,
                     cap_top=False, cap_bottom=False, col=col),
        mats["stone"]))

    # --- 2) İki tuğla kuşak --------------------------------------------
    #
    # 13,20 ve 17,17 m — TDV. Ilki 1509 onariminin dikisi: o kottan yukarisi
    # Osmanli isi. Kusak hafifce disari tasar; duz bir renk seridi uzaktan
    # kaybolur, tasan bir silme GOLGE cizgisi birakir ve siluete girer.
    for z in BRICK_BAND_Z:
        band = hz.make_tube(f"Kusak_{int(z * 100)}", r + 0.12, r + 0.12, 0.42,
                            base_z=z - 0.21, segments=p.segments,
                            cap_top=False, cap_bottom=False, col=col)
        hz.assign(band, mats["brick"])
        parts.append(band)

    # --- 3) Pencere sıraları -------------------------------------------
    #
    # Kule bir savunma yapisi: acikliklar DAR. Ucuncu katta (17,17 m) belirgin
    # sekilde genis acikliklar oldugu kayitli (The Byzantine Legacy).
    #
    # ÜÇ KUSUR DÜZELTİLDİ (v1 render'i):
    #  * Sıralar tuğla kuşaklara BİNİYORDU (kuşak kotlari 13,20 ve 17,17 aynı
    #    zamanda kat baslangicidir). Artık kuşağa 0,9 m'den yakın sıra atlanır.
    #  * Her sıra farklı faz alıyordu (`0.12 * i`) ve cephe sarhos okunuyordu.
    #    Faz artık yarım adım DÖNÜŞÜMLÜ: dikey diziler kayar ama düzenlidir.
    #  * Belgeli kat kotları 20,80'de bitiyor, gövde ise 34,5 m — yani 12 m'lik
    #    bir bölüm bomboştu. Üstü belgeli aralıklarin ORTALAMASIYLA sürüyor
    #    (4,52 / 4,23 / 3,97 / 3,63 → daralan ritim, ~3,5 m) ve bu ÇIKARIMDIR.
    rows = list(FLOOR_Z)
    z = FLOOR_Z[-1]
    while z + 3.5 < p.shaft_h - 2.5:
        z += 3.5
        rows.append(z)

    for i, z in enumerate(rows):
        top = z + 1.4
        if top > p.shaft_h - 2.0:
            break
        if any(abs(z + 0.7 - b) < 0.9 for b in BRICK_BAND_Z):
            continue                          # kusagin uzerine pencere acilmaz
        wide = abs(z - 17.17) < 0.01          # ucuncu kat
        _ring_openings(parts, col, mats["shadow"], r,
                       z + 0.85, 1.7 if wide else 1.35, 0.95 if wide else 0.55,
                       6, phase=(math.pi / 6.0) * (i % 2),
                       stone=mats["stone"])

    # --- 4) Kapı: −Y cephesinde ----------------------------------------
    #
    # Kapi ustundeki kitabe 1832 onarimini anar — 1632'de YOK.
    door = hz.make_box(f"Kapi_{asset_name}", (p.door_w, 0.5, p.door_h),
                       (0.0, -(r - 0.22), p.door_h * 0.5), col)
    hz.assign(door, mats["shadow"])
    parts.append(door)

    # --- 4b) Kapi cercevesi + kemeri -----------------------------------
    for o in dk.kemer(f"KapiKemer_{asset_name}", 0.0, -(r - 0.05),
                      1.0, 0.0, p.door_w * 0.5, p.door_h, 0.30, 0.42, col,
                      steps=6, sivri=False):
        parts.append(hz.assign(o, mats["stone"]))

    # --- 5) Konsol sirasi: siperin ALTINDA -----------------------------
    # Ceneviz askeri mimarisinde siper duvar duzleminden tasar ve o tasmayi
    # bir konsol sirasi tasir. Siraya kadar kule duz bir boruydu.
    for o in dk.konsol_dizisi(f"Konsol_{asset_name}", 0.0, 0.0, r,
                              p.shaft_h - 1.05, col, n=p.segments,
                              out=0.70, h=1.00):
        parts.append(hz.assign(o, mats["stone"]))

    # --- 6) Taç: iki varyant -------------------------------------------
    if p.crown == "mazgalli":
        # BİRİNCİ TASVİR: dar ve yuksek koni, MAZGALLI SIPERIN ICINDEN yukselir.
        _merlons(parts, col, mats["stone"], r, p.shaft_h, p.parapet_h,
                 p.merlon_n)
        cone_r = r - 0.75                     # siperin ic yuzu
        cone_z = p.shaft_h
    else:
        # İKİNCİ TASVİR: basik ve genis; SACAK mazgallardan disari tasar.
        #
        # v1'de külah doğrudan siperin üstüne oturuyordu ve 0,95 m'lik saçak
        # mazgalları **tümüyle** yutuyordu: 24 parça geometri hiç
        # görünmüyordu. Oysa kaynak *"saçakları mazgallardan dışarı taşar"*
        # diyor — yani mazgal GÖRÜNÜR, saçak onun ötesine geçer.
        #
        # Çözüm külahın kendi ahşap **kasnağı**: çatı siperin üstündeki bir
        # duvara oturur, saçak oradan taşar. Gerçekte de bir kâgir siperin
        # içine kurulan ahşap külah böyle durur.
        _merlons(parts, col, mats["stone"], r, p.shaft_h, p.parapet_h,
                 p.merlon_n)
        drum = hz.make_tube(f"Kasnak_{asset_name}", r - 0.35, r - 0.35, p.DRUM_H,
                            base_z=p.shaft_h + p.parapet_h,
                            segments=p.segments, cap_top=False,
                            cap_bottom=False, col=col)
        hz.assign(drum, mats["timber"])
        parts.append(drum)
        cone_r = r + p.eave
        cone_z = p.shaft_h + p.parapet_h + p.DRUM_H

    cone = hz.make_tube(f"Kulah_{asset_name}", cone_r, 0.0, p.cone_h,
                        base_z=cone_z, segments=p.segments, col=col)
    hz.assign(cone, mats["lead"])
    parts.append(cone)
    l1.append(hz.assign(
        hz.make_tube("L1_Kulah", cone_r, 0.0, p.cone_h, base_z=cone_z,
                     segments=12, col=col),
        mats["lead"]))

    # Saçak altı: kulahin altinda kalan bosluga koyu bir halka — sacak
    # golgesi. Basik varyantta sacak 0,95 m tasiyor ve altini gostermeden
    # kulah havada duruyor gibi okunuyordu.
    if p.crown == "sacakli":
        soffit = hz.make_tube(f"SacakAlti_{asset_name}", cone_r, r, 0.30,
                              base_z=cone_z - 0.30, segments=p.segments,
                              cap_top=False, cap_bottom=False, col=col)
        hz.assign(soffit, mats["timber"])
        parts.append(soffit)

    # --- 7) Toparlama ---------------------------------------------------
    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)

    # Cizici SILINDIRIK olmali: kutu collider, kulenin dibinde 3,4 m'lik
    # gorunmez bir kose birakirdi (16,45 m capli bir daire ile onu saran
    # karenin farki). Oyuncu kuleye carpar ama havada durur.
    # CIZICI KULENIN BICIMINI IZLER — TEK DOLU SILINDIR DEGIL.
    #
    # Once butun yuksekligi kaplayan tek bir kapali tup uretiliyordu ve
    # `UCX_` oneki onu **disbukey** yapiyordu. Sonucu bir oyuncu buldu:
    # kapidan gecince kagir govdenin ustundeki mazgalli sahanliga
    # cikmasi gerekirken **tasin icinde** kaliyordu — olculdu,
    # `ClosestPoint` noktayi carpistiricinin icinde buldu
    # (MeshCollider, sinir 52,2–98,2 m).
    #
    # Kulenin gercek bicimi kademeli: kagir govde `shaft_h` (34,50) +
    # korkuluk (1,70) yuksekliginde biter ve USTU DUZDUR — Hezarfen'in
    # kalktigi yer orasi. Uzerinde daha dar bir kasnak ve ahsap kulah
    # durur.
    #
    # Iki tup birlestirilir ve `UCXB_` adini alir: depo bu sozlesmeyi
    # evlerde zaten kurdu (`ImportLanding`: UCX_ dolu -> convex,
    # UCXB_ ici bos -> convex DEGIL). Disbukey yapmak kademeyi geri
    # yutar ve sahanlik yine kaybolurdu.
    _ust = p.shaft_h + p.parapet_h
    _govde = hz.make_tube("UCXB_govde", r, r, _ust, base_z=mn[2],
                          segments=12, cap_top=True, cap_bottom=True, col=col)
    _tepe = hz.make_tube("UCXB_tepe", r - 0.35, r - 0.35,
                         (mx[2] - mn[2]) - _ust, base_z=mn[2] + _ust,
                         segments=12, cap_top=True, cap_bottom=False, col=col)
    ucx = hz.join([_govde, _tepe], f"UCXB_{asset_name}", col=col)
    hz.assign(ucx, mats["stone"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="kule", crown=p.crown, palette=p.palette,
                outer_d=OUTER_D, shaft_d=round(shaft_d, 3),
                shaft_h=round(p.shaft_h, 2),
                cone_h=round(p.cone_h, 2),
                today_total_h=TODAY_TOTAL_H)
    return lod0, lod1, ucx, info


def _merlons(parts, col, mat, r, base_z, height, count):
    """
    Mazgallı siper — dişler ve aralarındaki boşluklar.

    Siper GÖVDENİN ÜSTÜNDE, dış yüzle hizalı. Dişin genişliği aralığa
    yakın tutuluyor: çok seyrek dizilirse korkuluk, çok sık dizilirse
    tarak gibi okunur.
    """
    t = 0.70                                   # siper kalinligi
    for i in range(count):
        a = 2.0 * math.pi * i / count
        step = 2.0 * math.pi / count
        w = 2.0 * (r - t * 0.5) * math.sin(step * 0.30)   # dis genisligi
        obj = hz.make_box(f"Mazgal_{i:02d}", (w, t, height),
                          (0.0, 0.0, 0.0), col)
        obj.rotation_euler = (0.0, 0.0, a + math.pi * 0.5)
        obj.location = ((r - t * 0.5) * math.cos(a),
                        (r - t * 0.5) * math.sin(a),
                        base_z + height * 0.5)
        hz.assign(obj, mat)
        parts.append(obj)


# ===================================================================== Kız Kulesi

#: Kule Salacak kıyısından **100 m** açıkta, kayalıklar üzerinde
#: (Göksoy Özkan 2012). Ada Copernicus GLO-30'da **yok** — ölçüldü, çevresi
#: baştan başa −12 m. Kayalık bu yüzden arazinin değil VARLIĞIN parçası.
KIZ_OFFSHORE_M = 100.0


class KizKuleParams(object):
    """
    Kız Kulesi, **1632** — ve 1632'de bu kule **AHŞAPTIR**.

    ## Bugünkü kule 1632'de YOKTUR

    Herkesin bildiği kâgir kule, camlı köşk ve kurşun kubbe **1725**'tir:
    kule 1720'de (1130 Receb) sıçrayan bir kıvılcımla yanmış, Damat İbrahim
    Paşa yerine **kâgir bir fener kulesi** yaptırmıştır. Ondan öncesi
    ahşaptır — 1509 depreminde yıkılan kulenin yerine yapılan da *"yine
    ahşap"*tır (Göksoy Özkan 2012).

    Yani 1632'de Kız Kulesi: **kayalık üstünde, kâgir subasmanlı, ahşap
    gövdeli, kurşun örtülü** bir kule.

    ## 1632'de ne İŞE YARAR

    Fener **değil**. Zeytinyağı fenerini Damat İbrahim Paşa (sadaret
    1718-1730) koydurmuştur; 1632'de yoktur. Kule bir **karakoldur**: Fatih
    1453'ten sonra buraya nöbetçi birliği yerleştirmiş ve yapıyı
    sağlamlaştırmıştır; her akşam yatsıdan sonra ve seher vakti **mehter
    nöbet** çalar, bayramlarda ve cülûslarda **top atılır**.

    Bu yüzden model bir deniz feneri gibi değil, **nöbet kulesi** gibi
    kurulur: tepesinde çalgının durduğu korkuluklu bir **nöbet sahanlığı**
    var, fener yok.

    ## 1632'de OLMAYANLAR

    * kâgir kule gövdesi, **camlı köşk**, **kurşun kubbe** (1725),
    * **zeytinyağı feneri** (1718 sonrası),
    * II. Mahmud (1832) ve 1945 sonrası ekleri,
    * Manuel Komnenos'un Sarayburnu'na gerdiği **zincir** (12. yy).

    Ölçü yok: 1632 kulesi yanmıştır ve ölçülü çizimi bulunmamaktadır. Kütle
    **D3**'tür (tipolojik) ve `status="draft"` taşır.
    """

    def __init__(self, rock_w=26.0, rock_d=20.0, rock_h=3.2, plinth_h=2.4,
                 body=9.0, storeys=2, storey_h=3.6, gallery_h=2.2,
                 roof_h=5.0, eave=1.4, palette="default"):
        self.rock_w, self.rock_d, self.rock_h = rock_w, rock_d, rock_h
        self.plinth_h = plinth_h
        self.body, self.storeys, self.storey_h = body, storeys, storey_h
        self.gallery_h = gallery_h
        self.roof_h, self.eave = roof_h, eave
        self.palette = palette

    @property
    def total_h(self):
        return (self.rock_h + self.plinth_h + self.storeys * self.storey_h
                + self.gallery_h + self.roof_h)

    def validate(self):
        # Kule KAYALIK uzerinde durur; kaya govdeden genis olmali, yoksa
        # yapi denizden cikan bir kutu gibi okunur.
        if self.rock_w <= self.body + 4.0:
            raise ValueError(f"kayalik {self.rock_w} m — govdeden ({self.body}) "
                             "en az 4 m genis olmali")
        # Kaya su yuzunun USTUNDE kalmali: su duzlemi y=0.
        if self.rock_h < 1.5:
            raise ValueError("kayalik su yuzunden en az 1,5 m yukselmeli")
        # AHSAP kule bir kule olmali, kulube degil.
        if self.storeys < 2:
            raise ValueError("1632 kulesi cok katlidir (nobet + barinak)")
        # Ve BUGUNKUNDEN farkli kalmali: bugunku kule ~23 m. Ahsap kule
        # ondan yuksek olamaz; olursa modellenen sey 1725 kulesidir.
        if self.total_h > 23.0:
            raise ValueError(f"toplam {self.total_h:.1f} m — 1632 AHSAP "
                             "kulesi bugunku kagir kuleden (~23 m) yuksek "
                             "olamaz; 1725 kulesini modelliyorsundur")


def build_kiz_kulesi(p, col, asset_name, textured=False):
    """Kız Kulesi (1632, ahşap). `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    # --- 1) Kayalik ------------------------------------------------------
    #
    # Ada DEM'de YOK (olculdu: cevresi bastan basa -12 m), o yuzden kayalik
    # varligin parcasi. Su duzlemi y=0; kaya bir miktar ASAGI da uzanir ki
    # su cizgisinde kesilmis gibi durmasin.
    # TEK koni bir kum yigini gibi okunuyordu (v2 renderi). Kayalik bir
    # yigin degil bir OBEK: farkli yukseklikte, farkli faz ve yaricapta uc
    # kutle ust uste binince siluet kirilir ve tas gibi durur.
    rock = None
    for i, (rx, rt, hh, ph, ox, oy) in enumerate((
            (0.50, 0.39, p.rock_h + 2.5, 0.0, 0.0, 0.0),
            (0.30, 0.24, p.rock_h + 2.2, 0.7, -0.26, 0.17),
            (0.25, 0.20, p.rock_h + 1.6, 1.9, 0.28, -0.21))):
        b = hz.make_tube(f"Kaya_{asset_name}_{i}", p.rock_w * rx, p.rock_w * rt,
                         hh, base_z=-2.5, segments=7, phase=ph,
                         cap_top=True, cap_bottom=False, col=col)
        b.location.x += p.rock_w * ox
        b.location.y += p.rock_w * oy
        hz.assign(b, mats["stone"])
        parts.append(b)
        if rock is None:
            rock = b

    z = p.rock_h

    # --- 2) Kagir subasman ----------------------------------------------
    #
    # Fatih 1453'ten sonra "yapiyi saglamlastirmistir" — ahsap govde ciplak
    # kayaya degil kagir bir taban uzerine oturur.
    plinth = hz.make_box(f"Subasman_{asset_name}",
                         (p.body + 2.2, p.body + 2.2, p.plinth_h),
                         (0.0, 0.0, z + p.plinth_h * 0.5), col)
    hz.assign(plinth, mats["stone"])
    parts.append(plinth)
    z += p.plinth_h

    # --- 3) AHSAP govde --------------------------------------------------
    #
    # BOYASIZ yapisal ahsap. Ilk yazimda burada `trim` vardi ve yorum
    # "boyasiz" diyordu — yorum yanlisti: `trim`, ASI_DARK ile %70 MIX
    # tintlenmis, yani BOYALI. Renderda kirmizi okundu, olcum dogruladi.
    # `timber_bare` bu is icin acildi; ayirt edici nitelik aciklik degil
    # DOYGUNLUK: boyasiz kereste kroma 5,4, asi ailesi 11-28 (CIELAB).
    body_h = p.storeys * p.storey_h
    body = hz.make_box(f"Govde_{asset_name}", (p.body, p.body, body_h),
                       (0.0, 0.0, z + body_h * 0.5), col)
    hz.assign(body, mats["timber_bare"])
    parts.append(body)

    for k in range(p.storeys):
        zz = z + k * p.storey_h
        belt = hz.make_box(f"Kusak_{k}", (p.body + 0.24, p.body + 0.24, 0.28),
                           (0.0, 0.0, zz + p.storey_h - 0.14), col)
        hz.assign(belt, mats["timber_bare"])
        parts.append(belt)
        for s in (-1, 1):
            for axis in (0, 1):
                size = (0.55, 0.35, 1.3) if axis == 0 else (0.35, 0.55, 1.3)
                pos = ((s * (p.body * 0.5 - 0.12), 0.0, zz + p.storey_h * 0.55)
                       if axis == 0 else
                       (0.0, s * (p.body * 0.5 - 0.12), zz + p.storey_h * 0.55))
                w = hz.make_box(f"Pencere_{k}{axis}{s}", size, pos, col)
                hz.assign(w, mats["shadow"])
                parts.append(w)
    z += body_h

    # --- 4) NOBET SAHANLIGI ---------------------------------------------
    #
    # Kuleyi 1632'de kule yapan sey burasi: her aksam yatsidan sonra ve
    # seher vakti MEHTER calar. Fener DEGIL — o 1718 sonrasi.
    # PAYANDA once: sahanligi uzaktan okunur kilan sey korkuluk degil,
    # altindaki egik destek sirasidir. v1 renderinda sahanlik govdeden
    # yalnizca %10 tasiyordu (0,90 m / 9,00 m) ve bir cikinti gibi degil
    # bir KENAR gibi okundu; cikma 1,40 m'ye alindi ve payanda eklendi.
    rail_w = p.body + p.eave * 2.0
    n_cor = 5
    for axis in (0, 1):
        for sgn in (-1, 1):
            for i in range(n_cor):
                t = (i + 0.5) / n_cor - 0.5
                u = t * (p.body - 0.4)
                size = ((0.16, p.eave, 0.55) if axis == 0
                        else (p.eave, 0.16, 0.55))
                pos = ((u, sgn * (p.body * 0.5 + p.eave * 0.5), z - 0.25)
                       if axis == 0 else
                       (sgn * (p.body * 0.5 + p.eave * 0.5), u, z - 0.25))
                cb = hz.make_box(f"Payanda_{axis}{sgn}{i}", size, pos, col)
                hz.assign(cb, mats["timber_bare"])
                parts.append(cb)

    deck = hz.make_box(f"Sahanlik_{asset_name}",
                       (rail_w, rail_w, 0.30), (0.0, 0.0, z + 0.15), col)
    hz.assign(deck, mats["timber_bare"])
    parts.append(deck)

    # Korkuluk: dolu levha DEGIL — direk + kusak. Mehter burada calar;
    # dolu bir levha calgiciyi gizler ve sahanlik bir kutu gibi okunur.
    rail_h = p.gallery_h * 0.55
    for axis in (0, 1):
        for sgn in (-1, 1):
            top_size = ((rail_w, 0.14, 0.14) if axis == 0
                        else (0.14, rail_w, 0.14))
            top_pos = ((0.0, sgn * (rail_w * 0.5 - 0.07), z + 0.30 + rail_h)
                       if axis == 0 else
                       (sgn * (rail_w * 0.5 - 0.07), 0.0, z + 0.30 + rail_h))
            tr = hz.make_box(f"KorkulukUst_{axis}{sgn}", top_size, top_pos, col)
            hz.assign(tr, mats["timber_bare"])
            parts.append(tr)
            for i in range(6):
                u = ((i + 0.5) / 6 - 0.5) * (rail_w - 0.3)
                ps = (0.12, 0.12, rail_h)
                pp = ((u, sgn * (rail_w * 0.5 - 0.07), z + 0.30 + rail_h * 0.5)
                      if axis == 0 else
                      (sgn * (rail_w * 0.5 - 0.07), u, z + 0.30 + rail_h * 0.5))
                pst = hz.make_box(f"Direk_{axis}{sgn}{i}", ps, pp, col)
                hz.assign(pst, mats["timber_bare"])
                parts.append(pst)
    z += 0.30 + p.gallery_h

    # --- 5) Kursun ortu --------------------------------------------------
    #
    # Kaynak catida KURSUN ortuden soz eder. PIRAMIT: kubbe DEGIL — kubbe
    # 1725'in isaretidir ve 1632'de yoktur.
    roof = hz.make_tube(f"Ortu_{asset_name}", (p.body * 0.5 + p.eave) * 1.30,
                        0.0, p.roof_h, base_z=z, segments=4,
                        phase=math.pi * 0.25, col=col)
    hz.assign(roof, mats["lead"])
    parts.append(roof)

    l1.append(hz.assign(hz.make_box("L1_Kaya",
                                    (p.rock_w, p.rock_d, p.rock_h + 2.5),
                                    (0.0, 0.0, (p.rock_h - 2.5) * 0.5), col),
                        mats["stone"]))
    l1.append(hz.assign(hz.make_box("L1_Govde",
                                    (p.body, p.body, body_h + p.plinth_h),
                                    (0.0, 0.0,
                                     p.rock_h + (body_h + p.plinth_h) * 0.5),
                                    col), mats["timber_bare"]))
    l1.append(hz.assign(hz.make_tube("L1_Ortu", (p.body * 0.5 + p.eave) * 1.30,
                                     0.0, p.roof_h, base_z=z, segments=4,
                                     phase=math.pi * 0.25, col=col),
                        mats["lead"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)
    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], mx[2] - mn[2]),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5,
                       (mx[2] + mn[2]) * 0.5), col)
    hz.assign(ucx, mats["stone"])
    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="kizkulesi", palette=p.palette, status="draft",
                material="ahsap", storeys=p.storeys,
                above_water=round(p.total_h, 2), accuracy="D3")
    return lod0, lod1, ucx, info
