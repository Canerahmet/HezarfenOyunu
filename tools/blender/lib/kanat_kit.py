"""
**Kanat aygıtı** — oyunun imza nesnesi.

## Kaynak ne veriyor, ne vermiyor

Kaynak uçuşu anlatıyor, **kanadı anlatmıyor**. RESEARCH.md §"Hezarfen
değerlendirmesi" olayın tek tanığının Evliya olduğunu, mali kayıtlarda izi
bulunmadığını ve aerodinamik uzmanlarının gerekli süzülme oranını
(~55:1, modern delta kanat ~15:1) imkânsız bulduğunu kaydediyor. Ayrıca
Dankoff, Evliya'nın sistematik abartı üslubunu belgeliyor.

Yani **tarihî bir kanat planı yoktur** ve olmadığını söylemek bu varlığın
en dürüst özelliğidir. Kodekse bu not düşülür.

## Tasarım nereden geliyor

Plan Bölüm 10 kararı vermiş: *"ahşap çıta iskelet + kartal tüyü yüzey +
deri kayış — dönem malzemesi kuralına uyar."* Yani biçim uydurulmuyor,
**malzeme kuralından türetiliyor**: 1632'de bir zanaatkârın elinde ne
varsa o. Bez yerine tüy, metal yerine ahşap ve deri.

İskelet bir **yarasa/uçurtma** mantığıdır: merkezde omurga, ondan
yelpaze gibi açılan çıtalar, uçlarını bağlayan bir hatıl. Tüyler bu
kafesin üstüne, **uç tarafa doğru üst üste binerek** dizilir — kuş
kanadında olduğu gibi, çünkü bindirme yönü havayı tutan şeydir.

## Tek sert sayı: ALAN

`WindTuning.wingArea` **15 m²** ve uçuş bütçesi bu sayıyla ölçüldü.
Görünen kanat o alana sahip değilse model yalan söyler: oyuncu bir
şey görüp başka bir şeyin fiziğini yaşar. Bu yüzden `build_kanat`
üretilen yüzeyin alanını **ölçer** ve hedeften %6'dan fazla saparsa
üretimi durdurur.

Doğruluk: **T3** (tipolojik çıkarım) ve `status: draft`. Ölçü yok,
oran yok, çizim yok — yalnızca malzeme kuralı ve fizik kısıtı var.
"""

import math

import bmesh
import hz_blender as hz
import ottoman_kit as kit


#: Hedef kanat alanı (m²) — `WindTuning.wingArea` ile AYNI olmak zorunda.
TARGET_AREA = 15.0

#: Alan sapma toleransı. Bunun ötesinde üretim durur.
AREA_TOLERANCE = 0.06

#: Kök veteri (m) — omuz hizasındaki en geniş yer.
ROOT_CHORD = 2.55

#: Uç veteri (m). Sıfır değil: uçta da bir çıta var ve tüy oraya bağlanır.
TIP_CHORD = 0.62

#: Açıklık (m) — **elle yazılmaz, alandan TÜRER**.
#:
#: Tek sert sayı alandır (WindTuning.wingArea = 15 m²) ve uçuş bütçesi
#: onunla ölçüldü. İlk yazımda açıklığı 8,6 m diye elle koydum ve yamuk
#: alanı 13,63 m² çıktı — bekçi yakaladı. Sayıyı elle düzeltmek yerine
#: bağımlılığı ters çevirdim: veterler biçimi belirler, açıklık alanı
#: tutturur. Böylece veter değişse bile kanat 15 m² kalır.
#:
#: 9,46 m açıklık / 15 m² bir yamaç planörünün ölçüsüdür; 17. yy kanadı
#: için büyük ama fizik onu istiyor ve fizik önce gelir.
SPAN = TARGET_AREA / ((ROOT_CHORD + TIP_CHORD) * 0.5)

#: Kaç çıta (yarım kanat başına). Tek sayı: ortada omurga var.
#: İki yarının yataydan yukarı açısı (derece). **Süs değil.** Düz bir
#: levha yuvarlanmaya karşı kayıtsızdır: bir kanat düşünce o yarı daha
#: çok kaldırma üretmez, aygıt devrilmeye devam eder. Dihedral, alçalan
#: yarının havaya hücum açısını artırır ve kanat kendini toplar — 1632'de
#: bunun adı yoktu ama uçurtma yapan herkes biliyordu. Yarasa kanadı da,
#: uçurtma da düz değildir.
#:
#: Alanı BOZMAZ: `_mesh_area` zarı düzken ölçer ve fizik zaten
#: İZDÜŞÜM alanını ister. 7 derecede izdüşüm kaybı %0,7'dir.
DIHEDRAL_DEG = 7.0

RIBS = 7

#: Tüy sırası (yarım kanat başına).
FEATHER_ROWS = 3


class KanatParams(object):
    """
    Kanat aygıtı. `state` açılma durumu: `"open"` uçuş, `"folded"` sırtta
    taşınan, `"broken"` hasarlı (bir çıta kırık, tüyler dağınık).
    """

    def __init__(self, state="open", span=SPAN, root=ROOT_CHORD,
                 tip=TIP_CHORD, palette="default"):
        self.state = state
        self.span = span
        self.root = root
        self.tip = tip
        self.palette = palette

    def validate(self):
        if self.state not in ("open", "folded", "broken"):
            raise ValueError(f"state={self.state} — plan uc durum sayiyor: "
                             "acilma, cirpma, hasar")
        # Kok ucdan genis olmali: tersi bir kanat degil, bir kurek olurdu.
        if self.tip >= self.root:
            raise ValueError("uc veteri kok veterinden genis olamaz")
        return self

    @property
    def area_estimate(self):
        """Yamuk alanı — iki yarım kanat."""
        return self.span * (self.root + self.tip) * 0.5


def _membrane(name, span, root, tip, col, sweep=0.34, kirik_uc=0.0):
    """
    Kanat yüzeyi — yamuk bir zar, hafif **geriye süpürülmüş**.

    Süpürme (sweep) bir süs değil: ağırlık merkezi kanat merkezinin
    önünde kalmalı ki aygıt burun aşağı dengelensin. Düz bir yamuk
    kanatta pilot fazla önde durur ve aygıt sürekli burun yukarı gider.

    `kirik_uc` sağ yarının uç kısmını içeri çeker (0 = sağlam). Zar
    **asimetrik** olur ve bu önemlidir: kırık kanadın alanı gerçekten
    azalmalı. İlk yazımda çıtalar ve tüyler kırıkta düşüyordu ama zar
    tam kalıyordu; katalog kırık kanat için de 15,00 m² diyordu — yani
    kırık bir kanat sağlamıyla aynı fiziği taşırdı.
    """
    bm = bmesh.new()
    hs, hr, ht = span * 0.5, root * 0.5, tip * 0.5
    # Kok (x=0) ve uc (x=+-hs) kesitleri; y ileri-geri, z duz (zar).
    v = []
    for sx in (-1.0, 1.0):
        # Sag yari kirikta kisalir; sol yari her zaman tam.
        kis = (1.0 - kirik_uc) if sx > 0 else 1.0
        ux = sx * hs * kis
        # Kisalan yarinin ucu daha genis kalir (kirilma noktasindaki veter).
        # DIKKAT: burada `hr` adini KULLANMA — o zaten kok yariveteri ve
        # ust satirda taniml. Ilk yazimda walrus ile ezdim, kok veteri
        # 2,55'ten 1,93'e dustu ve alan 15,00 -> 12,07 oldu; alan bekcisi
        # yakaladi.
        kirik_pay = root * 0.5 - ht
        uht = (ht + kirik_pay * (1.0 - kis)) if sx > 0 else ht
        v.append([
            bm.verts.new((ux, -uht + sweep * hs * kis, 0.0)),   # uc on
            bm.verts.new((ux, +uht + sweep * hs * kis, 0.0)),   # uc arka
        ])
    kok = [bm.verts.new((0.0, -hr, 0.0)), bm.verts.new((0.0, +hr, 0.0))]
    bm.verts.ensure_lookup_table()
    for i, sx in enumerate((-1.0, 1.0)):
        a, b = v[i]
        if sx < 0:
            bm.faces.new((a, b, kok[1], kok[0]))
        else:
            bm.faces.new((kok[0], kok[1], b, a))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return hz.mesh_from_bmesh(name, bm, col)


def _mesh_area(obj):
    """Mesh'in gerçek yüzey alanı (m²) — iddiayı ölçmek için."""
    me = obj.data
    toplam = 0.0
    for poly in me.polygons:
        toplam += poly.area
    return toplam


def _dihedral_uygula(obj, egim):
    """Uçları kaldır: `z += egim * |x|`.

    Birleştirilmiş ağa uygulanır, parçalara değil — böylece çıta, tüy ve
    zar aynı yüzeyde kalır. Parça parça uygulasaydım her birinin kendi
    yuvarlaması olurdu ve tüyler zardan ayrılırdı.
    """
    for v in obj.data.vertices:
        v.co.z += egim * abs(v.co.x)
    obj.data.update()
    return obj


def _cita_yerleri(span, root, tip, sweep, kirik):
    """Çıta konumları — LOD0 ve LOD1 **aynı** kaynaktan okusun diye.

    İkisini ayrı yazsaydım biri değişince öbürü sessizce eski kalırdı;
    bu projede o hata (aynı sayının iki yerde yazılması) daha önce
    tackapı ve revak yüksekliğinde üç kez çıktı.
    """
    hs = span * 0.5
    for sx in (-1, 1):
        for i in range(1, RIBS + 1):
            if kirik and sx > 0 and i >= RIBS - 1:
                continue               # kirik uc: son iki cita yok
            t = i / float(RIBS)
            veter = root + (tip - root) * t
            yield sx, i, sx * hs * t, sweep * hs * t, veter


def build_kanat(p, col, asset_name, textured=False):
    """Kanat aygıtı. `(lod0, lod1, ucx, info)`."""
    p.validate()
    mats, tex_sizes = kit.build_materials(p.palette, textured=textured)
    parts, l1 = [], []

    # Katlanmis durumda aciklik daralir; kirikta bir uc duser.
    span = p.span
    if p.state == "folded":
        span = p.span * 0.30
    kirik = p.state == "broken"

    hs = span * 0.5
    sweep = 0.34

    # --- ZAR (tuy yuzeyi) --------------------------------------------------
    zar = _membrane(f"Zar_{asset_name}", span, p.root, p.tip, col, sweep,
                    kirik_uc=0.34 if kirik else 0.0)
    hz.assign(zar, mats["feather"])
    parts.append(zar)
    # Alan zar DUZKEN olculur: fizik izdusum alanini ister, yuzey alanini
    # degil. Dihedral yuzeyi %0,7 buyutur ama tasiyan izdusum ayni kalir.
    alan = _mesh_area(zar)
    zar_mn, zar_mx = hz.bounds(zar)

    # --- OMURGA ------------------------------------------------------------
    # Pilotun sirtina gelen ana cita. Kanadin tasidigi her sey buna baglanir.
    parts.append(hz.assign(hz.make_box(
        f"Omurga_{asset_name}", (0.075, p.root * 1.06, 0.075),
        (0.0, 0.0, 0.02), col), mats["timber_bare"]))

    # --- CITALAR -----------------------------------------------------------
    # Yelpaze gibi acilir: kokte genis, uca dogru kisalir. Her citanin
    # boyu o noktadaki veterden turer — elle verilmez.
    for sx, i, x, y, veter in _cita_yerleri(span, p.root, p.tip, sweep, kirik):
            b = hz.make_box(f"Cita_{sx}_{i}", (0.048, veter, 0.048),
                            (0.0, 0.0, 0.0), col)
            # Cita omurgadan uca dogru hafifce geriye yatar.
            b.rotation_euler = (0.0, 0.0, math.atan2(sweep * span * 0.5, hs) * sx)
            b.location = (x, y, 0.03)
            parts.append(hz.assign(b, mats["timber_bare"]))

    # --- HUCUM KENARI ------------------------------------------------------
    # Uclari baglayan hatil. Kanadi bir arada tutan sey budur; onsuz
    # citalar bagimsiz cubuklardir.
    for sx in (-1, 1):
        if kirik and sx > 0:
            continue
        x0, y0 = 0.0, -p.root * 0.5
        x1 = sx * hs
        y1 = -p.tip * 0.5 + sweep * span * 0.5
        ln = math.hypot(x1 - x0, y1 - y0)
        b = hz.make_box(f"HucumKenari_{sx}", (ln, 0.062, 0.062),
                        (0.0, 0.0, 0.0), col)
        b.rotation_euler = (0.0, 0.0, math.atan2(y1 - y0, x1 - x0))
        b.location = ((x0 + x1) * 0.5, (y0 + y1) * 0.5, 0.05)
        parts.append(hz.assign(b, mats["timber_bare"]))

    # --- TUY SIRALARI ------------------------------------------------------
    # Tuyler UCA DOGRU ust uste biner — kus kanadinda oldugu gibi, cunku
    # bindirme yonu havayi tutan seydir. Ters bindirme kanadi sizdirir.
    for sx in (-1, 1):
        for row in range(FEATHER_ROWS):
            fr = (row + 0.6) / (FEATHER_ROWS + 0.2)
            n = 9 - row * 2
            for i in range(n):
                t = (i + 0.5) / n
                if kirik and sx > 0 and t > 0.62:
                    continue
                x = sx * hs * t
                veter = p.root + (p.tip - p.root) * t
                y = sweep * span * 0.5 * t + veter * (fr - 0.5) * 0.86
                boy = veter * 0.30 * (1.0 - 0.25 * row)
                f = hz.make_box(f"Tuy_{sx}_{row}_{i}",
                                (veter * 0.10, boy, 0.012),
                                (0.0, 0.0, 0.0), col)
                # Tuy uca dogru donuk: bindirmenin yonu bu.
                f.rotation_euler = (0.0, 0.0, sx * math.radians(14.0))
                f.location = (x, y, 0.055 + row * 0.006)
                parts.append(hz.assign(f, mats["feather"]))

    # --- DERI KAYISLAR -----------------------------------------------------
    # Aygit pilota BAGLANIR; kayis olmadan kanat bir levhadir. Omuz ve
    # bel olmak uzere iki cift.
    for sy, boy in ((-0.42, 0.52), (0.30, 0.44)):
        for sx in (-1, 1):
            parts.append(hz.assign(hz.make_box(
                f"Kayis_{sx}_{sy:.2f}", (0.055, 0.03, boy),
                (sx * 0.19, sy * p.root * 0.5, -boy * 0.5), col),
                mats["leather"]))
    # Tutamak: pilotun elini gectigi cubuk.
    parts.append(hz.assign(hz.make_box(
        f"Tutamak_{asset_name}", (0.62, 0.05, 0.05),
        (0.0, -p.root * 0.30, -0.34), col), mats["timber_bare"]))

    # --- LOD1 --------------------------------------------------------------
    # Ilk yazimda LOD1 yalnizca zardi: **4 ucgen**. 772'den 4'e dusmek bir
    # merdiven degil, bir yok olustur — ve kanadin okunan seyi zar degil,
    # yelpaze gibi acilan CITA silueti. Katalog sayisi gosterdi; render
    # gostermezdi, cunku render hep LOD0'i cizer.
    l1.append(hz.assign(_membrane(f"L1_{asset_name}", span, p.root, p.tip,
                                  col, sweep,
                                  kirik_uc=0.34 if kirik else 0.0),
                        mats["feather"]))
    l1.append(hz.assign(hz.make_box(
        f"L1Omurga_{asset_name}", (0.075, p.root * 1.06, 0.075),
        (0.0, 0.0, 0.02), col), mats["timber_bare"]))
    for sx, i, x, y, veter in _cita_yerleri(span, p.root, p.tip, sweep, kirik):
        if i % 2:
            continue                   # birer atlayarak: silueti tasiyan yeter
        b = hz.make_box(f"L1Cita_{sx}_{i}", (0.048, veter, 0.048),
                        (0.0, 0.0, 0.0), col)
        b.rotation_euler = (0.0, 0.0, math.atan2(sweep * hs, hs) * sx)
        b.location = (x, y, 0.03)
        l1.append(hz.assign(b, mats["timber_bare"]))

    lod0 = kit.join_parts(parts, f"SM_{asset_name}_LOD0", col)
    lod1 = kit.join_parts(l1, f"SM_{asset_name}_LOD1", col)

    # KATLI KANAT GERCEKTEN KATLANIR.
    #
    # "Katli" hal bugune kadar yalnizca ACIKLIGI %30'a indiriyordu;
    # veter (2,55 m) oldugu gibi kaliyordu. Sonuc 3,08 x 2,70 m'lik bir
    # elmasti — yani sirtta tasinan sey adamin kendisinden buyuk bir
    # tahta ucurtmaydi. Oyun turunda gorundu: ucuncu sahis kamerasinin
    # onunde duran, sehri tamamen kapatan bir levha. Bir oyuncu bunu
    # zaten bir kez yazmisti ("onumde 1,3 m cikinti yapan bir tezgah")
    # ve o tur cozum onu DONDURMEK olmustu; donen sey hala bir levhaydi.
    #
    # Yelpaze citali bir kanat katlanirken citalar omurgaya dogru
    # toplanir: aciklik da veter de kucululur, kalan sey uzun ve dar bir
    # DENKTIR. Olculer o denkten: 1,50 x 0,59 m — bir kurek boyu, bir
    # kucak eni. Fizik etkilenmez, cunku ucus alani (`wing_area`) ACIK
    # kanattan olculur ve katli hal hicbir zaman ucmaz.
    if p.state == "folded":
        for obj in (lod0, lod1):
            for v in obj.data.vertices:
                v.co.x *= 0.53      # aciklik: 2,84 -> 1,50 m
                v.co.y *= 0.22      # veter:   2,70 -> 0,59 m
            obj.data.update()

    # Dihedral BIRLESTIRMEDEN SONRA ve UCX'ten ONCE: carpisma kutusu
    # kalkan uclari kapsasin.
    egim = math.tan(math.radians(DIHEDRAL_DEG))
    for obj in (lod0, lod1):
        _dihedral_uygula(obj, egim)

    mn, mx = hz.bounds(lod0)
    ucx = hz.make_box(f"UCX_{asset_name}",
                      (mx[0] - mn[0], mx[1] - mn[1], 0.24),
                      ((mx[0] + mn[0]) * 0.5, (mx[1] + mn[1]) * 0.5, 0.0),
                      col)
    hz.assign(ucx, mats["timber_bare"])

    for obj in (lod0, lod1):
        kit.apply_uvs(obj, tex_sizes)

    info = dict(footprint_x=round(mx[0] - mn[0], 3),
                footprint_y=round(mx[1] - mn[1], 3),
                height=round(mx[2] - mn[2], 3),
                pivot_min_z=round(mn[2], 4),
                tris_lod0=kit.tri_count(lod0), tris_lod1=kit.tri_count(lod1),
                kind="kanat", state=p.state, palette=p.palette,
                status="draft", accuracy="D3",
                # OLCULEN aciklik, istenen degil. Kirik kanatta ikisi
                # ayrisir (bir uc dusmustur) ve katalog nominal sayiyi
                # bildirseydi kirik kanat saglam gorunurdu.
                span=round(zar_mx[0] - zar_mn[0], 2),
                span_nominal=round(span, 2),
                dihedral_deg=DIHEDRAL_DEG,
                root_chord=round(p.root, 2),
                tip_chord=round(p.tip, 2),
                wing_area=round(alan, 3),
                ribs=RIBS, feather_rows=FEATHER_ROWS)
    return lod0, lod1, ucx, info
