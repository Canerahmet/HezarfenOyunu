"""
Hezarfen: 1632 — Karakter (Faz 5).

Taban gövde CC0 paketten gelir (kayıt: `art/base/blender-studio/meta.json`);
kıyafet **gövdeden türetilen kabuklarla** kurulur ve biçim Rålamb
albümünden okunan dilbilgisine dayanır (`docs/RESEARCH.md`).

Üretilen üç durum:

- `Hezarfen_Govde`  — çıplak taban. Ara ürün; rig ve NPC varyantları için.
- `Hezarfen_Sivil`  — ayak bileğine inen uzun entari. Yerdeki Hezarfen.
- `Hezarfen_Ucus`   — baldıra kadar kısa entari + dizlik. Kuleye çıkan ve
                      uçan Hezarfen. Kısalık bir tasarım tercihi değil:
                      plakalarda **çalışan adamın entarisi kısadır**.

Kullanım:
  blender --background --python tools/blender/gen_hezarfen.py -- [--export]
"""

import argparse
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
for _p in (_HERE, os.path.join(_HERE, "lib")):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from mathutils import Vector        # noqa: E402

import hz_blender as hz             # noqa: E402
import karakter_kit as kar          # noqa: E402
import kiyafet_kit as kiy           # noqa: E402
import ottoman_kit as kit           # noqa: E402
import rig_kit as rk                # noqa: E402
import sac_kit as sk                # noqa: E402
from export_fbx import export_fbx   # noqa: E402

COLLECTION = "Export"

SOURCE_GOVDE = (
    "HEZARFEN AHMED CELEBI'nin GOVDESI. Portresi YOKTUR — ne minyaturu, "
    "ne tarifi. RESEARCH.md: sahsi hakkinda bilinen tek sey Evliya'nin "
    "birkac cumlesidir. Bu yuzden govde bir BENZERLIK iddiasi tasimaz; "
    "genel yetiskin erkek anatomisidir. Taban geometri MPFB2 "
    "(MakeHuman Plugin For Blender) 2.0.17 CEKIRDEK varlıklariyla "
    "parametrik uretilir; eklenti GPL-3.0 ama URETILEN MODEL CC0 "
    "(kayit: refs/LICENSES.md, MakeHuman SSS bagi orada). Ucuncu "
    "taraf asset pack'i KULLANILMAZ. Makro degerleri "
    "HEZARFEN_MAKRO'da ve gerekcelidir. Boy 1,70 m ve bu sayi keyfi "
    "degil: bu projenin butun inceleme paketleri 1,70 m'lik olcek "
    "figurune gore yargilandi."
)
SOURCE_KIYAFET = (
    "KIYAFET: Ralamb kiyafet albumunden (1657-58, kamu mali, refs/ralamb/) "
    "OKUNAN dilbilgisi. Minyatur kopyalanmadi; okunan sey oranlardir — "
    "entarinin boyu isi soyler (oturan ayak bilegine, calisan baldira), "
    "kusak dogal belde ve dar, salvar entarinin altindan GORUNUR, kol agzi "
    "astari ters cevrilir, dizlik gercek bir ogedir (plaka 50), baslik "
    "rutbe gostergesidir ve Hezarfen ne pasa ne asker. UYARI: album "
    "1657-58, oyun 1632 — YIRMI BES YIL fark. Ana hatlar (salvar-gomlek-"
    "entari-kusak-sarik) bu aralikta degismedi ama ayrinti degisebilir. "
    "Peter Mundy (1618) obur yandan yaklasiyor; 1632'nin TAM ORTASINDA "
    "BIR KAYNAK YOK ve bu bosluk kapatilamaz, ancak soylenebilir."
)

#: Taban gövdenin makro ayarları (MPFB2 / MakeHuman).
#:
#: **Varsayılan bırakmak bir seçimdi ve yanlış seçimdi:** MPFB
#: `gender: 0.5` ile gelir, yani ne erkek ne kadın — üretilen gövdede
#: göğüs vardı ve entarinin altından okunuyordu. İlk inceleme
#: paketinde (Hezarfen_Sivil_v3) görünen buydu. Bir varsayılan, hiç
#: karar vermemek değildir; başkasının verdiği karardır.
#:
#: | anahtar | değer | neden |
#: |---|---|---|
#: | gender | 1,0 | Hezarfen Ahmed Çelebi bir erkektir (RESEARCH §2) |
#: | age | 0,58 | MakeHuman'da 0,5 = 25 yaş, 1,0 = 90; bu ≈ 35 yaş |
#: | muscle | 0,58 | kanat yapıp kuleye çıkan adam; atlet değil |
#: | weight | 0,48 | dönem tasvirlerinde zanaatkâr ince yapılıdır |
#: | proportions | 0,5 | "ideal" oran istemiyoruz, ortalama insan |
#: | height | 0,5 | boy zaten 1,70 m'ye ÖLÇEKLENİYOR; buradaki değer
#:                   yalnız uzuv oranlarını etkiler, ortalama kalsın |
#: | race | ağırlıklı caucasian | MakeHuman'ın bu ekseni Akdeniz ve
#:                   Yakın Doğu'yu "caucasian" altında toplar |
#:
#: **T3 — bu bir portre değildir.** Hezarfen'in çağdaş bir tasviri
#: yoktur (RESEARCH §2); buradaki yüz onun yüzü DEĞİL, 17. yy
#: İstanbul'unda yetişkin bir erkeğin makul bir gövdesidir. Kaynak
#: olmadığı için iddia da yok.
HEZARFEN_MAKRO = {
    "gender": 1.0,
    "age": 0.58,
    "muscle": 0.58,
    "weight": 0.48,
    "proportions": 0.5,
    "height": 0.5,
    "cupsize": 0.0,
    "firmness": 0.5,
    "race": {"caucasian": 0.80, "asian": 0.15, "african": 0.05},
}

#: (ad, etek_kotu_orani, dizlik_var, neden)
VARIANTS = [
    ("Hezarfen_Sivil", kiy.BILEK_ORAN, False,
     "yerdeki Hezarfen — uzun entari, oturan adamin boyu"),
    ("Hezarfen_Ucus", kiy.BALDIR_ORAN, True,
     "kuleye cikan ve ucan Hezarfen — kisa entari + dizlik"),
]


def giydir(govde, col, mats, etek_orani, dizlik_var):
    """Gövdeye kıyafeti giydirir; parça listesi döner."""
    parts = []
    mn, mx = hz.bounds(govde)
    boy = mx[2] - mn[2]

    z_bel = boy * kiy.BEL_ORAN
    z_kalca = boy * kiy.KALCA_ORAN
    z_diz = boy * kiy.DIZ_ORAN
    z_etek = boy * etek_orani
    z_boyun = boy * 0.855

    # --- Kol/govde/BACAK ayrimi OLCULUR, elle yazilmaz --------------------
    # Iki sayi gerekiyor, bir degil: bu duruslarda bacak koldan daha
    # disaridadir (|x| 0,24 > 0,20), yani tek bir |x| esigi kolu bacaktan
    # ayiramaz. Gerekce ve olcum: kiy.kol_ayirici.
    kol_esik, z_kol_alt = kiy.kol_ayirici(govde)
    if kol_esik is None:
        kol_esik = kiy.kol_siniri(govde, z_kalca) or (boy * 0.11)
    if z_kol_alt is None:
        z_kol_alt = z_kalca

    def kol(c):
        """Bu kose kola mi ait? Disarida VE bacak kotunun ustunde."""
        return abs(c.x) >= kol_esik and c.z >= z_kol_alt

    # Kol bolgesinin en alt noktasi parmak ucudur; bilek ondan bir el boyu
    # (boyun ~%10,5'i) yukaridadir. Kol agzi bilekte biter — plakalarda
    # parmaklar gorunur, entari eli yutmaz.
    kol_vs = [v.co.z for v in govde.data.vertices if kol(v.co)]
    z_parmak = min(kol_vs) if kol_vs else boy * 0.36
    z_bilek = z_parmak + boy * 0.105

    # --- GOMLEK (ic keten) ------------------------------------------------
    gomlek = kiy.kopya_kabuk(
        govde, "Gomlek", col,
        # Belin altinda kalan gomlek eteğin ICINDE kalir; gorunmeyen
        # geometri uretmiyoruz.
        tut=lambda c: (z_bel - boy * 0.035) <= c.z <= z_boyun and not kol(c),
        sisme=0.008, kalinlik=kiy.GOMLEK_KAL)
    if gomlek:
        parts.append(hz.assign(kiy.yumusat(gomlek, 3), mats["gomlek"]))

    # --- SALVAR ------------------------------------------------------------
    # Sisme kota GORE degisir: bilekte dar, uylukte bol, belde toplanir.
    # Salvarin bicimi budur; sabit bir ofset dar pantolon verirdi ve
    # salvar dar pantolonun tam tersidir.
    z0 = boy * 0.045

    # KATMAN KURALI: ust katman alt katmandan HER KOTTA daha cok sismeli.
    # Ilk turda salvar belde 0,030, entari 0,021 sisiyordu — alt katman
    # ustu deldi ve karinda kirmizi bir leke cikti. Kumas kalinligi degil
    # SISME sirasi belirler kimin ustte oldugunu.
    SALVAR_UYLUK = 0.042
    SALVAR_BEL = 0.022
    ENTARI_SIS = 0.034          # > SALVAR_BEL
    ETEK_PAY = 0.058            # > SALVAR_UYLUK

    def salvar_sis(c):
        t = min(1.0, max(0.0, (c.z - z0) / max(1e-6, z_bel - z0)))
        if t < 0.55:
            return 0.010 + (SALVAR_UYLUK - 0.010) * (t / 0.55)
        return SALVAR_UYLUK + (SALVAR_BEL - SALVAR_UYLUK) * ((t - 0.55) / 0.45)

    salvar = kiy.kopya_kabuk(
        govde, "Salvar", col,
        # "abs(c.x) < kol_esik" yazmak baldirin DISINI (|x| 0,24 > esik
        # 0,20) salvarin disinda birakiyordu — pacada bir serit acikta
        # kaliyordu. Dogru olcut kolun ta kendisi: kol degilse bacaktir.
        tut=lambda c: (z0 <= c.z <= z_bel) and not kol(c),
        sisme=salvar_sis, kalinlik=kiy.GOMLEK_KAL)
    if salvar:
        parts.append(hz.assign(kiy.yumusat(salvar, 4), mats["salvar"]))

    # --- ENTARI: govde + kollar --------------------------------------------
    #
    # Etek ARTIK BELDEN basliyor (asagida), o yuzden ust kabuk kalcaya
    # kadar inmiyor: belin biraz altinda biter ve gerisi konidir. Once
    # kalcaya kadar iniyordu ve o parca eteğin ICINDE kaliyordu — hic
    # gorunmeyen ~7 bin ucgen. Bir katman gorunmuyorsa katman degildir.
    z_ust_alt = z_bel - boy * 0.035
    entari_ust = kiy.kopya_kabuk(
        govde, "Entari_Ust", col,
        tut=lambda c: (z_ust_alt <= c.z <= z_boyun and not kol(c))
        or (kol(c) and c.z >= z_bilek),
        sisme=ENTARI_SIS, kalinlik=kiy.ENTARI_KAL)
    if entari_ust:
        parts.append(hz.assign(kiy.yumusat(entari_ust, 5), mats["entari"]))

    # --- ENTARI ETEGI -------------------------------------------------------
    #
    # ## Etek BELDEN baslar, kalcadan degil
    #
    # Once kalcadan (z_kalca + 2 cm) basliyordu ve ust halkasi entarinin
    # yuzeyinden 2,4 cm disaridaydi: aradaki halka acikligindan icerisi —
    # kirmizi salvar — gorunuyordu. Inceleme paketi v5'te karakter belden
    # asagi bir kovanin icinde duruyor gibiydi. Etek belde, kusagin
    # altinda baslarsa dikis kusakla ortulur ve bakilacak aralik kalmaz.
    #
    # ## Alt yaricap ELLE degil, alt zarftan hesaplanir
    #
    # Gerekce kiy.etek_acikligi'nda: koni gövdeden bagimsizdir, o yuzden
    # gövdeyi icerdigi garanti degildir.
    bel_k = (kiy.kesit_merkezli(govde, z_bel, dislama=kol)
             or (boy * 0.10, boy * 0.068, 0.0))
    bel_cy = bel_k[2]
    etek_ust_z = z_bel
    r_ust = (bel_k[0] + ENTARI_SIS + kiy.ENTARI_KAL + 0.002,
             bel_k[1] + ENTARI_SIS + kiy.ENTARI_KAL + 0.002)

    # Kisa entari daha cok acilir: hareket eden adamin adimina yer birakir.
    acilma = 1.34 if etek_orani < 0.15 else 1.52
    # Etek SALVARI ortmek zorundadir, ayagi degil: mest ve ayak eteğin
    # altindan gorunur. O yuzden zarf salvarin kot araliginda olculur.
    zarf = kiy.alt_zarf(govde, max(z_etek, z0), etek_ust_z, salvar_sis,
                        dislama=kol)
    r_ust, r_alt, bel_cy, etek_cy_alt = kiy.etek_acikligi(
        r_ust, bel_cy, etek_ust_z, z_etek, zarf,
        kiy.GOMLEK_KAL + 0.012, acilma)
    hz.log(f"etek: ust {r_ust[0]:.3f}/{r_ust[1]:.3f} @cy {bel_cy:+.3f} -> "
           f"alt {r_alt[0]:.3f}/{r_alt[1]:.3f} @cy {etek_cy_alt:+.3f}")

    parts.append(hz.assign(kiy.etek(
        "Entari_Etek", col, etek_ust_z, z_etek,
        r_ust, r_alt, kiy.ENTARI_KAL, yarik=True, cy=bel_cy,
        cy_alt=etek_cy_alt), mats["entari"]))

    # --- KUSAK ---------------------------------------------------------------
    bel = bel_k
    # Kusak entarinin USTUNDE baglanir; ic katman degildir. Ilk turda
    # yaricapi entarininkinden kucuktu (0,024 < 0,034) ve kusak entarinin
    # ICINDE kaldi — yalnizca belin ve sirtin CUKUR yerlerinde bir damla
    # gibi disari sizdi. Bir kusak gorunmuyorsa kusak degildir.
    # Kusak entarinin YUZEYINE oturur; disari cikmasini fici bicimi
    # (kiy.band) saglar. Once buraya 12 mm daha ekliyordum ve kusak
    # giysiden ayri, ortasi bos bir cember gibi duruyordu.
    kusak_pay = ENTARI_SIS + kiy.ENTARI_KAL + 0.002
    parts.append(hz.assign(kiy.band(
        "Kusak", col, z_bel, (bel[0] + kusak_pay, bel[1] + kusak_pay),
        boy * 0.055, kiy.KUSAK_KAL, cy=bel_cy), mats["kusak"]))

    # --- DIZLIK (yalniz ucus varyanti) ---------------------------------------
    if dizlik_var:
        # Dizlik SALVARIN USTUNE baglanir — salvari dizde toplayan sey odur
        # (plaka 50). Yaricap bacaktan OLCULUR ve salvarin o kottaki
        # sismesi eklenir; elle bir sayi yazsaydim dizlik salvarin icinde
        # kalir ve hic gorunmezdi.
        diz_sis = salvar_sis(type("P", (), {"z": z_diz})())
        for sx in (-1, 1):
            bk = kiy.bacak_kesit(govde, z_diz, sx)
            if bk is None:
                continue
            cx, rx, ry = bk
            pay = diz_sis + kiy.GOMLEK_KAL + 0.010
            b = kiy.band(f"Dizlik_{sx}", col, z_diz,
                         (rx + pay, ry + pay), boy * 0.030, 0.007)
            for v in b.data.vertices:
                v.co.x += cx
            b.data.update()
            parts.append(hz.assign(b, mats["leather"]))

    # --- SARIK ----------------------------------------------------------------
    # Sarik ALINDAN baslar ve basin tepesini ASAR — baslik odur. Yarıcap
    # kafanin kendi genisliginden turer, elle yazilmaz.
    # Taban KASIN USTUNDE: ilk turda 0,905'ten basliyordu ve sarik gozleri
    # ortuyordu. Bir baslik yuzu kapatmaz.
    bas = kiy.kesit(govde, boy * 0.955) or (boy * 0.048, boy * 0.058)
    bas_r = max(bas[0], boy * 0.048)
    # KAVUK: sarigin altindaki cekirdek. Plaka 35 ve 50'de sarigin
    # tepesinden kirmizi bir tepe gorunur — sarik bir kupa degil, bir
    # cekirdegin uzerine sarilmis bezdir. Cekirdek olmadan sarigin
    # tepesindeki delikten bosluk gorunuyordu.
    parts.append(hz.assign(kiy.band(
        "Kavuk", col, boy * 1.005, (bas_r * 0.80, bas_r * 0.74),
        boy * 0.075, 0.006), mats["kavuk"]))
    parts.append(hz.assign(kiy.sarik(
        "Sarik", col, boy * 0.948, boy * 1.052, bas_r), mats["sarik"]))

    # --- SAKAL ve SAC ----------------------------------------------------------
    #
    # AZ kart, cunku Hezarfen sarikli: sarik sacin cogunu orter ve
    # gorunen sey sakal, sakak, ense. Kafanin tamamini kaplamak hic
    # gorunmeyecek 40 kartin bedelini odemek olurdu.
    sac_mat = sk.hair_material()
    hat = sk.cene_hatti(govde, boy)

    # --- SAKAL: KART DEGIL KABUK ------------------------------------------
    #
    # Sakali uc kat alfa kartiyla kuruyordum. Yakin cekimde ne oldugu
    # goruldu (renders/denetim/kafa_yakin.png): kartlar cene hattina
    # diziliyor ama duz dikdortgen olduklari icin kulaktan kulaga giden
    # bir ONLUK olusturuyorlardi — cenenin bicimini hic izlemiyorlardi.
    # Ustelik kartlarin arasi bosluk oldugu icin toplu halde isik almiyor
    # ve siyah bir delik gibi okunuyorlardi.
    #
    # Giysilerde calisan yontem burada da calisir ve ayni sebeple:
    # sakal da altindaki bicime OTURAN bir kutledir. Cene bolgesinin
    # yuzleri kopyalanip disari itilince sakal kafanin bicimini kendi
    # kendine alir — kart dizmek gerekmez, ve kafa degisirse sakal
    # kendini yeniden kurar.
    #
    # Bolge cene hattindan OLCULUR: bir kose, cene yayindaki en yakin
    # noktaya `sakal_menzil`den yakinsa ve agiz kotunun altindaysa
    # sakaldir. Boylece ust dudak (biyik ayri parcadir) ve boyun disarida
    # kalir.
    if hat:
        hat_p = [p for p, _ in hat]
        # Yay yalniz sag yarimdir; sol taraf aynalanarak eklenir.
        hat_p = hat_p + [Vector((-p.x, p.y, p.z)) for p in hat_p]
        z_agiz = boy * 0.897
        z_dip = boy * 0.806
        sakal_menzil = boy * 0.052

        def sakal_bolge(c):
            if c.z > z_agiz or c.z < z_dip:
                return False
            return min((c - p).length for p in hat_p) < sakal_menzil

        sakal = kiy.kopya_kabuk(
            govde, "Sakal", col, tut=sakal_bolge,
            sisme=lambda c: 0.006 + 0.016 * min(
                1.0, max(0.0, (z_agiz - c.z) / (z_agiz - z_dip))),
            kalinlik=0.004)
        if sakal:
            parts.append(hz.assign(kiy.yumusat(sakal, 2),
                                   sk.sakal_material()))

    # Cene ucundan sarkan tutam: kabuk yuzeye oturur, sakalin UCU
    # yuzeyden ayrilir. Birkac kart bu silueti verir.
    for i, (p, yon) in enumerate(hat[:4]):
        for sx in (-1, 1):
            if sx > 0 and abs(p.x) < boy * 0.004:
                continue
            k = sk.kart(f"SakalUc_{sx}_{i}",
                        (p.x * sx + yon.x * sx * 0.020,
                         p.y + yon.y * 0.020, p.z - boy * 0.030),
                        (yon.x * sx * 0.30, yon.y * 0.30, -1.0),
                        (yon.x * sx, yon.y, 0.30),
                        boy * 0.030, boy * 0.028, col,
                        serit=i % 4, egim=0.22)
            parts.append(hz.assign(k, sac_mat))

    # BIYIK: ust dudak. Plaka 20 ve 35'te sakalla birlikte var.
    for sx in (-1, 1):
        b = sk.kart(f"Biyik_{sx}", (sx * boy * 0.010, -boy * 0.052,
                                    boy * 0.905),
                    (sx * 0.85, -0.30, -0.42), (0.0, -1.0, 0.25),
                    boy * 0.026, boy * 0.020, col, serit=2, egim=0.10)
        parts.append(hz.assign(b, sac_mat))

    # Sakak ve ense: sarigin altindan cikan tutamlar.
    for sx in (-1, 1):
        for i, (dy, dz, ser) in enumerate((
                (0.30, 0.905, 0), (0.10, 0.895, 3), (-0.34, 0.900, 1))):
            kesit = kiy.kesit(govde, boy * dz) or (boy * 0.05, boy * 0.06)
            k = sk.kart(f"Sac_{sx}_{i}",
                        (sx * kesit[0] * 0.94, dy * kesit[1], boy * dz),
                        (sx * 0.35, dy * 0.3, -1.0), (sx, dy * 0.4, 0.25),
                        boy * 0.034, boy * 0.030, col, serit=ser, egim=0.30)
            parts.append(hz.assign(k, sac_mat))

    # --- MEST ------------------------------------------------------------------
    # Mest kabuk DEGIL kaliptir; gerekcesi kiy.mest'te (kabuk yontemi
    # MakeHuman'in bes ayri parmagini deriye tasiyordu).
    for sx in (-1.0, 1.0):
        m = kiy.mest(f"Mest_{int(sx)}", col, govde, sx, boy)
        if m:
            kiy.zemine_otur(m)
            parts.append(hz.assign(m, mats["mest"]))

    return parts, kol_esik, z_kol_alt


def taban_kur(args, mats):
    """
    Çıplak taban gövdeyi getirir, ölçer, normalleştirir.

    ## İki kaynak, tek sözleşme

    `mpfb` (varsayılan) — MPFB2 ile **parametrik** üretilir; yaş, boy,
    kilo, kas kaydırıcıları var ve Faz 6'nın NPC çeşitliliği oradan
    gelecek. `paket` — Blender Studio'nun CC0 tek gövdesi; geri çekilme
    yolu olarak duruyor.

    İkisi de aynı şeyi döndürür (kimlik dönüşümü, ayaklar z=0, boy
    1,70 m), o yüzden aşağıdaki üç satır — temizle, öne çevir,
    normalleştir — ikisinde de aynı çalışır. ADR 0068 bunu iki gün önce
    böyle öngörmüştü: kıyafet gövdeden kopyalanıyor, rig gövdeden
    ölçülüyor; taban değişimi bu hattın KIRILMASI değil, tasarlandığı
    durum.
    """
    if getattr(args, "taban_kaynak", "mpfb") == "mpfb":
        import mpfb_kit as mp                       # noqa: PLC0415
        govde = mp.taban_getir_mpfb(col=hz.collection(COLLECTION),
                                    makro=HEZARFEN_MAKRO)
        hz.log(f"taban: MPFB2 parametrik — {mp.olc(govde)}")
    else:
        govde = kar.taban_getir(args.taban, col=hz.collection(COLLECTION))
        hz.log("taban: Blender Studio CC0 paketi")
    kar.temiz_ag(govde)
    aci = kar.one_cevir(govde)
    k = kar.normalize(govde)
    hz.assign(govde, mats["skin"])
    return govde, aci, k


def denetle(olcu):
    """Oranlar insan mı — render 'doğru görünüyor' der, bunlar doğru
    OLUP OLMADIĞINI söyler."""
    if not 6.5 <= olcu["bas_orani"] <= 8.5:
        raise SystemExit(f"[HZ] HATA bas orani 1/{olcu['bas_orani']} — "
                         "yetiskin insan 1/7 ile 1/8 arasindadir.")
    if not 0.36 <= olcu["omuz_genisligi"] <= 0.50:
        raise SystemExit(f"[HZ] HATA omuz {olcu['omuz_genisligi']:.3f} m — "
                         "yetiskin erkek 0,38-0,48 m arasindadir.")
    if olcu["boyun_genisligi"] >= olcu["omuz_genisligi"] * 0.62:
        raise SystemExit(f"[HZ] HATA boyun {olcu['boyun_genisligi']:.3f} m, "
                         f"omuz {olcu['omuz_genisligi']:.3f} m — olcum "
                         "boynu bulamadi.")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out-dir", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Import"))
    ap.add_argument("--blend-dir", default=os.path.join("art", "blend", "karakter"))
    ap.add_argument("--catalog", default=os.path.join("art", "blend", "karakter",
                                                      "catalog.json"))
    ap.add_argument("--taban", default=None, help="Human Base Meshes .blend yolu")
    ap.add_argument("--taban-kaynak", default="mpfb", choices=("mpfb", "paket"),
                    help="Taban govde kaynagi: mpfb (parametrik) | paket (CC0 tek govde)")
    ap.add_argument("--no-textures", action="store_true")
    # Rig'siz bir karakteri prefab yapmak erken olur; `_Import` bos kalir
    # (CLAUDE.md: "_Import bos birakilir").
    ap.add_argument("--export", action="store_true",
                    help="FBX'i _Import'a yaz (varsayilan: yazma)")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    # --- 1) CIPLAK TABAN ----------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    mats, tex_sizes = kit.build_materials("default", textured=not args.no_textures)
    asset = "SK_Hezarfen_Govde"
    govde, aci, k = taban_kur(args, mats)
    govde.name = f"{asset}_LOD0"
    govde.data.name = govde.name
    olcu = kar.olcu_al(govde)
    hz.log(f"taban: boy {olcu['boy']:.3f} m, bas orani 1/{olcu['bas_orani']}, "
           f"omuz {olcu['omuz_genisligi']:.3f} m (boyun "
           f"{olcu['boyun_genisligi']:.3f} m), yon {aci * 57.2958:.0f} derece")
    denetle(olcu)

    lod1 = kar.desimasyon(govde, 0.35, f"{asset}_LOD1")
    hz.link(lod1, col)
    hz.assign(lod1, mats["skin"])
    catalog.append(dict(
        name="Hezarfen_Govde", prefab=None,
        prefab_notu="Ara urun: rig ve kiyafet tamamlanmadan prefab uretilmez.",
        kind="karakter", state="base", status="draft", accuracy="D3",
        tier="T3", source=SOURCE_GOVDE,
        # Taban kaydi KAYNAGA GORE yazilir. Once burada sabit olarak
        # "human-base-meshes-bundle-v1.4.1 / CC0" yaziyordu ve taban
        # MPFB2'ye gectikten sonra da oyle yazmaya devam etti: katalog
        # kullanmadigimiz bir paketi kaynak gosteriyordu. Ticari
        # yayinda yanlis lisans kaydi kusurun en pahalisidir.
        **(dict(taban_paket="mpfb-v2.0.17-cekirdek",
                taban_obje="MPFB2 parametrik (HEZARFEN_MAKRO)",
                taban_lisans="CC0-1.0 (uretilen model) / GPL-3.0 (arac)",
                taban_makro=dict(HEZARFEN_MAKRO))
           if getattr(args, "taban_kaynak", "mpfb") == "mpfb" else
           dict(taban_paket="human-base-meshes-bundle-v1.4.1",
                taban_obje=kar.TABAN_OBJE, taban_lisans="CC0-1.0")),
        yon_duzeltme_derece=round(aci * 57.2958, 2), olcek_carpani=round(k, 5),
        tris_lod0=kar.hz_tri(govde), tris_lod1=kar.hz_tri(lod1),
        uv_var=kar.uv_var_mi(govde), **olcu))
    hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))

    # --- 2) GIYINIK VARYANTLAR ------------------------------------------------
    for ad, etek_orani, dizlik_var, why in VARIANTS:
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        mats, tex_sizes = kit.build_materials("default",
                                              textured=not args.no_textures)
        asset = f"SK_{ad}"
        govde, _, _ = taban_kur(args, mats)
        giysi, kol_esik, z_kol_alt = giydir(govde, col, mats, etek_orani, dizlik_var)
        # Adlar BIRLESTIRMEDEN once alinir: birlestirme parca nesnelerini
        # siler ve sonradan okumak "StructRNA has been removed" verir.
        giysi_adlari = sorted({o.name.split(".")[0] for o in giysi})
        giysi_sayisi = len(giysi)

        # Eklemler CIPLAK govdeden olculur, giyinikten degil: entari
        # omzu 3,4 cm kalinlastirir ve omuz eklemini o kadar disari
        # atardi. Rig tenin altindadir.
        eklem = rk.eklemleri_olc(govde, kol_esik, z_kol_alt)
        rig_hata = rk.uzuv_denetimi(eklem, kar.HEDEF_BOY)
        if rig_hata:
            raise SystemExit(f"[HZ] HATA {ad} rig: " + "; ".join(rig_hata))

        govde.name = f"{asset}_ten"

        # BIRLESTIR, BAGLA, SONRA BOSLUGU DOLDUR.
        #
        # Olculdu: `ARMATURE_AUTO` birlestirilmis agda 27.624 kosenin
        # **2.964'unu** (%10,7) hicbir kemige baglamiyor. Sebep isi
        # yayilimi: kapali ve AYRIK kabuklarda (gomlek, entari, kusak —
        # hepsi `kopya_kabuk` + kalinlik) isi tenden giysiye atlayamiyor.
        # O koseler Unity'de kemik 0'a duser ve giysi adalari govde
        # oynarken kalcaya cakili kalir — oyun ici karelerde gomlek
        # govdenin onunde ayri bir levha olarak duruyordu.
        #
        # ONCE parcalari ayri baglayip sonra birlestirmeyi denedim ve
        # OLCUM REDDETTI: 2.964 yerine 27.624, yani HEPSI agirliksiz
        # kaldi. Birlestirme, ayri ayri kurulmus zirh iliskilerini
        # korumuyor.
        #
        # Kalan yol boslugu dogrudan doldurmak: agirliksiz her koseyi
        # EN YAKIN kemige tam agirlikla bagla. Kaba ama dogru — giysi
        # kabugu zaten govdeden kopyalandigi icin en yakin kemik, o
        # kosenin takip etmesi gereken kemiktir.
        arm = rk.iskelet_kur(f"AR_{ad}", eklem, col)
        lod0 = kit.join_parts([govde] + giysi, f"{asset}_LOD0", col)
        lod1 = kar.desimasyon(lod0, 0.30, f"{asset}_LOD1")
        hz.link(lod1, col)
        for m in (lod0, lod1):
            rk.deri_bagla(m, arm)
            rk.agirliklari_tamamla(m, arm)

        # AGIRLIKSIZ KOSE SAYILIR — VE SIFIR DEGILSE URETIM DURUR.
        #
        # Oyun ici karelerde gomlek govdenin onunde ayri bir levha,
        # entari etegi bacagin yaninda bagimsiz bir tabaka olarak
        # duruyordu. Blender'in bind-poz kontak sayfasi tertemiz
        # oldugu icin kusur uzun sure "modelde bir sey var" diye
        # arandi; oysa kusur BAGLAMADAYDI ve bind pozunda gorunmez —
        # ancak animasyon oynayinca ortaya cikar.
        #
        # Bir kusurun gorulmedigi yerde olculmesi gerekir.
        agirliksiz, toplam = rk.agirliksiz_kose(lod0)
        hz.log(f"agirlik: {toplam - agirliksiz}/{toplam} kose bagli")
        if agirliksiz > 0:
            raise SystemExit(
                f"[HZ] HATA {ad}: LOD0'da {agirliksiz}/{toplam} kose "
                "hicbir kemige bagli degil. Unity'de bu koseler kemik "
                "0'a (kok) duser ve o giysi adalari govde oynarken "
                "kalcaya cakili kalir.")

        mn, mx = hz.bounds(lod0)
        bilgi = dict(
            name=ad, prefab=None,
            prefab_notu="Ara urun: rig tamamlanmadan prefab uretilmez.",
            kind="karakter", state="dressed", status="draft", accuracy="D3",
            # Varlik EN DUSUK bileseninden etiketlenir: govde T3, kiyafet T2
            # -> giyinik karakter T3. Kiyafeti T2 diye etiketleyip govdeyi
            # gormemek, bilinmeyeni bilinir gostermek olurdu.
            tier="T3", kiyafet_tier="T2", source=SOURCE_KIYAFET, why=why,
            boy=round(mx[2] - mn[2], 4),
            en_genis=round(mx[0] - mn[0], 4),
            derinlik=round(mx[1] - mn[1], 4),
            etek_kotu=round(kar.HEDEF_BOY * etek_orani, 3),
            dizlik=dizlik_var, giysi_parca=giysi_sayisi,
            giysi_adlari=giysi_adlari,
            tris_lod0=kar.hz_tri(lod0), tris_lod1=kar.hz_tri(lod1),
            # AGIRLIK KAYDA GIRER — CUNKU KATALOG GORMEDIGINI KORUYAMAZ.
            #
            # CLAUDE.md: "git status ikili bir dosyayi degismis
            # gosteriyorsa once catalog.json diff'ine bak — ucgen sayisi
            # ve olculer degismediyse o dosya geri alinir."
            #
            # Deri baglama duzeltildiginde tam bu durum olustu:
            # geometri bir mikron oynamadi, `catalog.json` diff'i BOSTU,
            # ve kural gercek bir duzeltmeyi cope atmayi soyluyordu.
            # Degisen sey kosele agirliklariydi ve katalog onu
            # kaydetmiyordu.
            #
            # Kural yanlis degil; kayit eksikti. Bir kaydin isi,
            # onemli olan her seyi gorunur kilmaktir.
            agirliksiz_kose=agirliksiz,
            kose_lod0=toplam,
            kemik=len(arm.data.bones),
            kemikler=rk.kemik_raporu(arm, kar.HEDEF_BOY))

        # Genislik denetimi: giyinik adam ciplaktan en fazla ~%25 genis
        # olur (kollar entariyle kalinlasir, etek acilir). Ilk turda 3,29 m
        # cikti — cunku govde paketin kendi konumunda kalmisti ve giysiler
        # baska yerde duruyordu. Bounding box yalan soylemez: bir sayi
        # insana ait olamayacak kadar buyukse once o sorulur.
        if bilgi["en_genis"] > olcu["en_genis"] * 1.25:
            raise SystemExit(
                f"[HZ] HATA {ad}: giyinik genislik {bilgi['en_genis']:.3f} m, "
                f"ciplak {olcu['en_genis']:.3f} m — bir parca govdeden "
                "kopmus olmali.")
        # Unity Humanoid eksik kemikle avatar kurmaz ve hata mesaji
        # hangi kemigin eksik oldugunu soylemez; burada soyluyoruz.
        eksik = [b for b, _ in rk.HUMANOID if b not in arm.data.bones]
        if eksik:
            raise SystemExit(
                f"[HZ] HATA {ad}: Humanoid kemikleri eksik: {eksik}")
        # Sarik boyu artirir ama 1,70 + sarik hala insan boyudur.
        if bilgi["boy"] > kar.HEDEF_BOY * 1.10:
            raise SystemExit(
                f"[HZ] HATA {ad}: giyinik boy {bilgi['boy']:.3f} m — ciplak "
                f"{kar.HEDEF_BOY} m'nin %10'undan fazla ustunde.")
        # Bir kabuk secimi bos donerse giysi sessizce eksik kalir ve
        # karakter yari ciplak cikar.
        #
        # Bunu ONCE bir SAYI ile denetliyordum ("en az 48 parca"). Sayi
        # yanlis seyi olcuyordu: sakal 54 karttan tek bir kabuga
        # dusunce denetim, giysinin tamami yerinde oldugu halde uretimi
        # reddetti. Bir esik, saydigi seyin ne oldugunu bilmiyorsa
        # yalan soyler — burada gereken sayi degil, ADLARDIR.
        zorunlu = {"Gomlek", "Salvar", "Entari_Ust", "Entari_Etek",
                   "Kusak", "Sakal", "Kavuk", "Sarik",
                   "Mest_-1", "Mest_1"}
        if dizlik_var:
            zorunlu |= {"Dizlik_-1", "Dizlik_1"}
        eksik_giysi = sorted(zorunlu - set(bilgi["giysi_adlari"]))
        if eksik_giysi:
            raise SystemExit(
                f"[HZ] HATA {ad}: giysi parcasi eksik: {eksik_giysi} — "
                "bir kabuk secimi bos donmus olabilir.")

        catalog.append(bilgi)
        hz.log(f"{ad:16s} boy {bilgi['boy']:.3f} m, etek {bilgi['etek_kotu']:.2f} m, "
               f"{bilgi['giysi_parca']} parca, {bilgi['kemik']} kemik, "
               f"{bilgi['tris_lod0']:6d} / {bilgi['tris_lod1']:5d} ucgen")

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        if args.export:
            export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                       collection_name=COLLECTION, skinned=True)

    if not args.export:
        hz.log("FBX yazilmadi (--export ile): karakter rig'siz Unity'ye gitmez.")

    catalog.sort(key=lambda v: v["name"])
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} karakter durumu; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
