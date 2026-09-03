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
import math
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
import sakin_kit as skn             # noqa: E402
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
#: Her varyant bir sozluk: ad, etek kotu, dizlik, giysi tipi, MPFB2
#: makrosu, hedef boy, ve NEDEN.
#:
#: Sehirdeki 40.000 sakinin hepsi ayni adamdi ve bir oyuncu bunu
#: yazdi: *"cocuklar minik sakalli adamlar, kadin hic yok."* Tek
#: govdeyi tonlayip olceklemek cesitlilik degil tekrardir; ayirt eden
#: sey renk degil SILUET. MPFB2 zaten parametrik (`gender`, `age`,
#: `height`, `weight`) ve bugune kadar tek bir makroyla kosuyordu.
VARIANTS = [
    dict(ad="Hezarfen_Sivil", etek=kiy.BILEK_ORAN, dizlik=False,
         tip="erkek", makro=None, boy=None,
         why="yerdeki Hezarfen — uzun entari, oturan adamin boyu"),
    dict(ad="Hezarfen_Ucus", etek=kiy.BALDIR_ORAN, dizlik=True,
         tip="erkek", makro=None, boy=None,
         why="kuleye cikan ve ucan Hezarfen — kisa entari + dizlik"),
]

for _ad, _makro, _boy, _tip, _why in skn.ARKETIPLER:
    VARIANTS.append(dict(ad=_ad, etek=skn.etek_orani(_tip), dizlik=False,
                         tip=_tip, makro=_makro, boy=_boy, why=_why))


#: Son olculen kol donusu — `giydir` icinden kataloga tasinir.
#: Modul duzeyinde tek hucre: giydir bir sozluk dondurmuyor ve
#: imzasini degistirmek bes cagiran yeri de degistirirdi.
_KOL_DONUS = [0.0]


def _kendi_uvsi_var(obj, esik=1e-6):
    """Parçanın UV katmanında **veri** var mı (boş katman sayılmaz)."""
    me = obj.data
    if not me.uv_layers:
        return False
    veri = me.uv_layers[0].data
    for i in range(min(len(veri), 64)):
        u, v = veri[i].uv
        if abs(u) > esik or abs(v) > esik:
            return True
    return False


def giydir(govde, col, mats, etek_orani, dizlik_var, tip="erkek"):
    """Gövdeye kıyafeti giydirir; parça listesi döner.

    `tip` giysiyi degistirir, gövdeyi degil: kadin sokakta FERACE ve
    yasmak tasir, cocuk takke tasir ve sakali yoktur, yaslinin sarigi
    daha buyuk ve sakali aktir. Bunlar sus degil — Rålamb albumunde
    sokaktaki kadin her zaman ortuludur ve baslik RUTBE gosterir, yani
    bir cocuga sarik sarmak tarihsel olarak yanlistir.
    """
    parts = []
    # Ferace entarinin baska rengi degil, ayri bir malzemedir: `hz.assign`
    # malzemeyi ADA gore paylastirir, yani entariyi kadinda boyasaydim
    # sehirdeki butun erkeklerin entarisi de renk degistirirdi.
    ust_mat = mats["ferace"] if tip == "kadin" else mats["entari"]
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
        # GOMLEGIN YAKASI ENTARININ ICINDE KALIR.
        #
        # Ikisi de `z_boyun`da bitiyordu ve kabuk kesimi govdenin
        # ucgen kenarlarini izledigi icin agiz TIRTIKLI cikiyor.
        # Oglanin yakin planinda sonucu goruldu: entarinin yakasinin
        # icinde soluk, firfirli bir halka — gomlek yakasi degil, bir
        # kesim izi.
        #
        # Pay 2 cm: entarinin agzi (ayni kotta, ama disarida ve
        # yumusatilmis) gomlegin agzini tamamen orter. Gorunen tek
        # kenar entarininki olur ve o zaten yaka gibi okunuyor.
        tut=lambda c: ((z_bel - boy * 0.035) <= c.z
                       <= z_boyun - boy * 0.020 and not kol(c)),
        sisme=0.008, kalinlik=kiy.GOMLEK_KAL)
    if gomlek:
        parts.append(hz.assign(kiy.yumusat(gomlek, 9), mats["gomlek"]))

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
        parts.append(hz.assign(kiy.yumusat(salvar, 11), mats["salvar"]))

    # --- ENTARI: govde + kollar --------------------------------------------
    #
    # Etek ARTIK BELDEN basliyor (asagida), o yuzden ust kabuk kalcaya
    # kadar inmiyor: belin biraz altinda biter ve gerisi konidir. Once
    # kalcaya kadar iniyordu ve o parca eteğin ICINDE kaliyordu — hic
    # gorunmeyen ~7 bin ucgen. Bir katman gorunmuyorsa katman degildir.
    # FERACE BIR MANTODUR — BELDE BITMEZ.
    #
    # Kadinin silueti UC UST USTE SILINDIR okunuyordu: govde, bel bandi,
    # etek. Sebep yapisal — kabuk belin 3,5 cm altinda bitiyor, etek
    # belden basliyor ve aradaki dikisi bir BANT ortuyor. Bant bir
    # kusaktir; ferace onden kapali bir dis giysidir ve kusak tasimaz.
    #
    # Dogru cozum dikisi ortmek degil, dikisi KALCAYA tasimak. Eski
    # yorumun sikayeti ("etegin ust halkasi entarinin yuzeyinden 2,4 cm
    # disarida") bir kot uyusmazligiydi: kabuk BELDE bitiyor ama etegin
    # ust yaricapi KALCAYI (govdenin en genis yeri) icermek zorunda. Iki
    # sayi iki farkli kotta olculuyordu.
    #
    # Ikisi de kalcada olculdugunde halkalar ayni yaricapi tasir ve
    # yuzey suregelir: omuzdan etek ucuna tek bir hat.
    # KABUK ETEGIN AGZININ **ALTINA** INER.
    #
    # Ayni ilke kolda zaten yazili (`KOL_PAYI`): bir kabugun agzi hep
    # baska bir parcanin ICINDE kalmali, yoksa kenari kenar olarak
    # okunur. Burada dikey karsiligi — kabuk kalcanin 4 cm altinda
    # biter, etek kalcadan baslar, yani kabugun eteği etegin icinde
    # kalir ve disaridan hicbir aciden gorunmez.
    #
    # DIKEYDE BU ILKE OLCULDU VE **SINIRI BULUNDU**.
    #
    # Once iki halkayi esitlemeyi denedim: etegin ust yaricapini kabugun
    # dis yaricapina cakip (`ust_sabit`) basamagi tam sifira indirdim —
    # 0,218/0,150 -> 0,218/0,150. Inceleme karesi hipotezi curuttu:
    # KIRMIZI SALVAR etegin icinden ciktı, kalcada bir bant ve etek
    # onunde lekeler halinde. Yani 8 mm'lik fark suslemesizdi; salvar
    # kalcada kabugun dis yuzeyinden GENIS ve etek onu icermek
    # zorunda.
    #
    # Kalan tek dogru kurulum bu: etek kalcada baslar ve 8 mm daha
    # genistir; kabuk 4 cm ASAGI iner, yani agzi etegin icinde kalir ve
    # ustten bakildiginda halka bosluğu degil kabugun duvarı gorunur.
    kadin_mi = (tip == "kadin")
    z_ust_alt = ((z_kalca - boy * 0.040) if kadin_mi else (z_bel - boy * 0.035))
    # KOL ARTIK KABUK DEGIL, KENDI HACMI OLAN BIR GIYSI.
    #
    # Once kol da bu kabugun icindeydi (`kol(c) and c.z >= z_bilek`):
    # govde kabugu kopyalanip 3,4 cm sisiriliyordu, yani kumas kola
    # YAPISIYORDU. Bir oyuncu kusuru tek cumleyle soyledi — kiyafet
    # "giysi degil, biraz buyuk bir vucut" gibi duruyor.
    #
    # Osmanli entarisinin kolu bilege dogru GENISLER ve sarkar (Ralamb
    # 1657-58). Silueti bir ofset degil bir profil; o yuzden kol artik
    # kendi cizgisi boyunca lofting ile uretiliyor ve kabuk yalniz
    # govdeyi tasiyor.
    # KABUK KOLUN ALTINA GIRER — DIKIS ORADA GIZLENIR.
    #
    # Kabugun kol bolgesini disarida birakmasi, koltuk altinda ACIK BIR
    # AGIZ birakiyor: inceleme karesinde iki omuzda da kumasin kenari
    # gorunuyor ve arasindan tenle birlikte kabugun ICI okunuyor.
    # Katilastirma ona kalinlik verir ama kapak koymaz; kenar kenardir.
    #
    # Pay 2 cm (1,78 m'de): kolun olculen yaricapinin (~4,2 cm) yarisi.
    # Yani kabugun agzi her zaman kol tupunun ICINDE kalir ve disaridan
    # hicbir aciden gorunmez.
    KOL_PAYI = boy * 0.022
    entari_ust = kiy.kopya_kabuk(
        govde, "Entari_Ust", col,
        tut=lambda c: z_ust_alt <= c.z <= z_boyun and not (
            abs(c.x) >= kol_esik + KOL_PAYI and c.z >= z_kol_alt),
        sisme=ENTARI_SIS, kalinlik=kiy.ENTARI_KAL)
    if entari_ust:
        parts.append(hz.assign(kiy.yumusat(entari_ust, 13), ust_mat))

    # --- ENTARI KOLLARI -----------------------------------------------------
    kol_sayisi = 0
    olculen_log = "yok"
    kol_donus = 0.0
    for isaret in (+1.0, -1.0):
        def _kol_filtre(c, s=isaret):
            return kol(c) and (c.x * s) > 0.0

        # KOL X EKSENINDE DILIMLENIR — GEREKCESI `uzuv_cizgisi`de.
        #
        # Omuzdan (kol esigi) parmak ucuna (govdenin en genis noktasi)
        # dogru yurunur. Ucu ISARETLIDIR: sag kol +x, sol kol -x.
        _mn, _mx = hz.bounds(govde)
        _x_ic = kol_esik * isaret
        _x_dis = (_mx[0] if isaret > 0 else _mn[0])
        # TOHUM: kol OMUZDAN baslar, kalcadan degil. Govde iki
        # bacakli oldugu icin `abs(x) >= kol_esik` sartini uyluk da
        # gecer; capasiz bir tarayici oradan baslayabiliyor.
        _tohum = Vector((_x_ic, 0.0, z_boyun - boy * 0.045))
        cizgi = rk.uzuv_cizgisi(govde, _x_ic, _x_dis, _kol_filtre,
                                adim=24, eksen=0, tohum=_tohum)
        if not cizgi or len(cizgi) < 3:
            continue

        # Bilekten sonrasi ele girer: cizgiyi bilekte kes.
        cizgi = [p for p in cizgi if p.z >= z_bilek] or cizgi

        # CIZGI OMUZA KADAR UZATILIR.
        #
        # Ilk denemede kol, izleme filtresinin kolu ayirt edebildigi
        # yerden basliyordu — yani omuzdan bir karis asagidan. Renderda
        # omuzla kol arasinda **cıplak ten** goruldu: kabuk kolu
        # birakmis, kol da omza yetismemisti. Iki katmanin arasindaki
        # bosluk, katmanlarin kendisinden daha cok goze carpar.
        #
        # ONCE BILEGI BUL, SONRA CIZGIYI ORADA KES.
        #
        # Yukarida cizgi `z_bilek = z_parmak + boy * 0,105` ile
        # kesiliyor — yani "el 10,5 cm'dir" varsayimiyla. Olcum bunu
        # reddetti: kesilen cizginin ucundaki yaricap 13 cm cikti,
        # bir bilek degil bir EL. Bilek, kolun en ince yeridir ve
        # aranabilir.
        t_bilek, r_bilek_olcu = kiy.bilek_olc(govde, cizgi, _kol_filtre,
                                              en_cok=boy * 0.09)
        cizgi = kiy.cizgi_kes(cizgi, t_bilek)

        # KOL KALINLIGI OLCULUR — uzatmadan ONCE, cunku uzatilan parca
        # govdenin ICINDE ve orada kol yoktur.
        # PENCERE DAR: genis pencere uzvun BASKA bir yerini olcer.
        #
        # 0,15 ile bilek kovasi t 0,77-1,07 arasini topluyordu, yani
        # onkolun yarisini; %86'lik degeri o araligin en kalin yerini
        # veriyor ve "bilek 12,7 cm" diye yaziliyordu. Bir noktanin
        # kalinligini olcerken pencere o noktanin kadar dar olmali.
        olculen = kiy.cizgi_yaricapi(govde, cizgi, _kol_filtre,
                                     oranlar=(0.10, 0.55),
                                     pencere=0.055, en_cok=boy * 0.09)
        # OMUZ AYRI OLCULUR — %10'DAKI KOL DELTOIDI BILMIYOR.
        #
        # Koltuk alti kapandi ama omzun USTU acik kaldi: inceleme
        # karesinde deltoidin uzerinde koyu bir yarik ve arasindan ten.
        # Sebep sayidaydi: `olculen[0]` cizginin %10'undaki yaricap,
        # yani kolun INCE yeri. Omuzun kendisi deltoidle birlikte
        # bundan belirgin kalin ve tup oraya yetismiyordu.
        #
        # Omuz kendi orani (%2) ile olculur; pencere de dar tutulur ki
        # gogse tasmasin.
        _omuz_olcu = kiy.cizgi_yaricapi(govde, cizgi, _kol_filtre,
                                        oranlar=(0.02,),
                                        pencere=0.045,
                                        en_cok=boy * 0.11)
        olculen_log = ("/".join("-" if o is None else f"{o*100:.1f}"
                                for o in list(olculen) + [r_bilek_olcu])
                       + f" cm, bilek t={t_bilek:.2f}")

        # Cizgi kendi ekseninde govdeye dogru uzatilir; boylece kol
        # entarinin govde kabuguna GIRER ve dikis gorunmez.
        # UZATMA YATAY OLMALI, KOL EKSENINDE DEGIL.
        #
        # Ilk denemede uzatma kolun kendi yonundeydi ve omuz hizasinin
        # USTUNE tasti: renderda omuzlarda apolet gibi iki tup cikti.
        # Kolun omuza yaklasan ucu yukari-ice bakar; o yonde uzatmak
        # kumasi omzun ustune kaldirir.
        #
        # Istenen sey kumasi govdenin ICINE sokmak, yukari degil:
        # dikey bilesen sifirlanir.
        # UZATMA GOVDEYE DOGRU OLDUGUNU GARANTI ETMELI.
        #
        # Yon olculen kol ekseninden turuyordu ve inceleme karesinde
        # sonucu ASIMETRIK cikti: sol omuzda dikis kapandi, SAG omuzda
        # kumasin agzi acik kaldi ve icerisi goruldu. Iki kol ayni kodla
        # uretiliyor; farki yaratan sey olculen cizginin ilk iki
        # noktasi — omuz ucunda birbirine cok yakin olduklarinda yon
        # gurultuye kaliyor ve disari bakabiliyor.
        #
        # Govdeye dogru olmak bir tercih degil sart: yon ne olursa
        # olsun x bileseni orta duzleme bakmali. Bakmiyorsa dogrudan
        # orta duzleme cevrilir.
        if len(cizgi) >= 2:
            ic = (cizgi[0] - cizgi[1])
            ic.z = 0.0
            if ic.length < 1e-6 or (ic.x * isaret) >= 0.0:
                ic = Vector((-isaret, 0.0, 0.0))
            # UZATMA 4,5 -> 7,5 cm: DIKIS OMZUN USTUNDE ACIK KALIYORDU.
            #
            # Koltuk alti kapandi ama omzun USTU kapanmadi: inceleme
            # karesinde deltoidin uzerinde koyu bir yarik ve arasindan
            # ten. Sebep olculebilir — kol tupunun yaricapi omuzda
            # olculen kol + entarinin sismesi (~7 cm), oysa deltoidin
            # tepesi kol EKSENINDEN daha yukarida. Tup oraya
            # YETISMIYOR.
            #
            # Yaricabi buyutmek omuzda bir TOP yapardi (bir kez oldu,
            # yorumu asagida). Dogru olan tupu govdenin daha ICINE
            # sokmak: ilk halka gogsun icinde kalir, loft deltoidin
            # uzerinden geceR ve dikis kabugun altinda kaybolur.
            cizgi = [cizgi[0] + ic.normalized() * (boy * 0.075)] + cizgi
            hz.log(f"kol uzatmasi {'sag' if isaret > 0 else 'sol'}: "
                   f"yon ({ic.x:+.3f}, {ic.y:+.3f})")

        # Profil BOY'a gore: omuzda kolu sarar, bilekte iki katina cikar.
        # YARICAP = OLCULEN KOL + KUMAS PAYI.
        #
        # Once uc sayi dogrudan boydan turuyordu (0,052 / 0,054 / 0,072)
        # ve bir kez renderdan asagi cekilmisti — ama hala kolun
        # kendisini olcmuyordu. Sonucu inceleme paketinde gorundu:
        # omuzda 8,8 cm yaricap, altindaki kol ~5 cm; entari degil
        # balon. Ustelik yedi arketip gelince ayni oran cocuga kendi
        # kolunun iki kati bir kol giydiriyordu.
        #
        # Pay da boydan turer, sabit santim degil: 1,70 m'de omuzda
        # 1,7 cm, dirsekte 2,2 cm, bilekte 3,7 cm. Ilk ikisi kumasin
        # bollugu, ucuncusu entari kolunun bilekte SARKMASI (plaka 20
        # ve 35) — genistir ama koldan iki kat kalin degildir. Boylece
        # kol omuzda 8,4, dirsekte 7,1, bilekte 8,2 cm: dirsekte
        # daralip bilekte hafifce acilan bir kumas.
        # Omuzdaki pay EN KUCUK olan: orada kolun uzerinde zaten
        # entarinin GOVDE kabugu var. Once 2,0 sonra 1,7 cm verdim ve
        # ikisinde de omuzda bir TOP olustu — kabuk ile kolun ust uste
        # bindigi yerde iki kat kumas. 1,0 cm ile kol kabugun hemen
        # ustune oturuyor.
        # OMUZDA KOL, ENTARININ KENDI PAYI KADAR GENIS OLMALI.
        #
        # Kusur yandan bakinca goruldu: omuzdan koltuk altina kadar
        # uzanan bir YARIK, arasindan ten ve kabugun ici. Sebep iki
        # sayinin birbirini bilmemesiydi — entari kabugu govdeden
        # 3,4 cm sisiriliyor, kol tupu ise olculen kolun 0,6 cm
        # disindan geciyordu. Dikis yerinde 2,8 cm'lik bir basamak var
        # ve kabugun agzi acikta kaliyor.
        #
        # Gercek bir entaride omuz, govdenin kumasi kadar genistir ve
        # kol dirsege dogru DARALIR. Yani omuz yaricapinin payi bir
        # zevk sayisi degil: entarinin kendi sismesi. Ikisi ayni
        # sayidan turdugu icin bir daha ayrisamazlar.
        r_om = max(olculen[0] or boy * 0.032,
                   (_omuz_olcu[0] or 0.0)) + ENTARI_SIS
        r_dir = (olculen[1] or boy * 0.026) + boy * 0.013
        r_bil = (r_bilek_olcu or boy * 0.020) + boy * 0.022
        # Cizginin toplam donusu: ardisik uc nokta arasindaki en
        # buyuk aci. Duz bir kolda birkac derece; katlanan bir cizgide
        # doksani asar.
        for _i in range(1, len(cizgi) - 1):
            _a = (cizgi[_i] - cizgi[_i - 1])
            _b = (cizgi[_i + 1] - cizgi[_i])
            if _a.length < 1e-6 or _b.length < 1e-6:
                continue
            _ac = math.degrees(_a.normalized().angle(_b.normalized()))
            kol_donus = max(kol_donus, _ac)

        kolu = kiy.giysi_kolu(
            f"Entari_Kol_{'Sag' if isaret > 0 else 'Sol'}", col, cizgi,
            r_omuz=r_om, r_dirsek=r_dir,
            r_bilek=r_bil, kalinlik=kiy.ENTARI_KAL)
        if kolu is not None:
            parts.append(hz.assign(kolu, ust_mat))
            kol_sayisi += 1
    # KOL CIZGISI DUZ OLMALI — DONUS ACISI OLCULUR.
    #
    # Yaslinin inceleme karesinde kollar dirsekten GERIYE kirilmis
    # duruyordu ve sayilar da bunu soyluyordu: bilek t=0,67 (yetiskinde
    # 0,95) ve bilek yaricapi 2,3 cm (yetiskinde 4,5). Yani cizgi kolu
    # izlemiyor, bir yerde katlaniyor.
    #
    # `uzuv_cizgisi` dilimleri Z ekseninde aliyor; kol yataya yakin
    # oldugunda ayni dilim hem ust kolu hem on kolu yakalar ve merkez
    # ikisinin ortasina, yani govdeye dogru ziplar. Bir uzvun merkez
    # cizgisi neredeyse duzdur; donus acisi bunu SAYIYLA soyler ve
    # katalog kaydeder.
    hz.log(f"entari kolu: {kol_sayisi} parca (lofted, kabuk degil) — "
           f"olculen kol {olculen_log}, kol donusu {kol_donus:.0f} derece")
    _KOL_DONUS[0] = kol_donus

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
    # ETEK NEREDE BASLAR: kadinda KALCA, erkekte BEL.
    #
    # Erkekte kusak beldedir ve dikisi o orter. Kadinda kusak yok, o
    # yuzden dikis kabugun bittigi yerde — kalcada — olmali; oradaki
    # halka kabugun kendi dis yaricapiyla ayni cikar ve gorunmez.
    etek_ust_z = z_kalca if kadin_mi else z_bel
    ust_k = (kiy.kesit_merkezli(govde, etek_ust_z, dislama=kol)
             or bel_k) if kadin_mi else bel_k
    bel_cy = ust_k[2]
    r_ust = (ust_k[0] + ENTARI_SIS + kiy.ENTARI_KAL + 0.002,
             ust_k[1] + ENTARI_SIS + kiy.ENTARI_KAL + 0.002)
    r_kabuk = r_ust

    # Kisa entari daha cok acilir: hareket eden adamin adimina yer birakir.
    acilma = 1.34 if etek_orani < 0.15 else 1.52
    # Etek SALVARI ortmek zorundadir, ayagi degil: mest ve ayak eteğin
    # altindan gorunur. O yuzden zarf salvarin kot araliginda olculur.
    zarf = kiy.alt_zarf(govde, max(z_etek, z0), etek_ust_z, salvar_sis,
                        dislama=kol)
    # ETEK-SALVAR PAYI: erkekte 1,6 cm, kadinda 0,6 cm.
    #
    # Buyuk pay bir kusuru kapatiyordu: etegin ust halkasi ile entarinin
    # yuzeyi arasindaki ACIKLIKTAN kirmizi salvar goruluyordu. Kadinda o
    # aciklik yok — dikis kalcada ve kabugun kendi yuzeyi onu ortuyor.
    # Payi buyuk tutmak burada bedava degil: etegin ust yaricapini
    # kabuktan 1,8 cm disari itiyor ve tam da kaldirmaya calistigimiz
    # BASAMAGI uretiyor. Sayi olculdu, seçilmedi.
    etek_payi = ((kiy.GOMLEK_KAL + 0.002) if kadin_mi
                 else (kiy.GOMLEK_KAL + 0.012))
    r_ust, r_alt, bel_cy, etek_cy_alt = kiy.etek_acikligi(
        r_ust, bel_cy, etek_ust_z, z_etek, zarf,
        etek_payi, acilma)
    hz.log(f"etek: ust {r_ust[0]:.3f}/{r_ust[1]:.3f} @cy {bel_cy:+.3f} -> "
           f"alt {r_alt[0]:.3f}/{r_alt[1]:.3f} @cy {etek_cy_alt:+.3f}")
    if kadin_mi:
        # BASAMAK OLCULUR, VARSAYILMAZ.
        #
        # `etek_acikligi` ust ucu yalniz BUYUTUR. Kalcada olculen kabuk
        # yaricapiyla etegin ust yaricapi ayrilirsa aradaki fark bir
        # basamaktir ve siluet yine ikiye bolunur. Sayiyi kayda
        # geciriyorum ki bir sonraki tur "goze oyle geldi" ile degil
        # bununla konussun.
        hz.log(f"ferace dikisi: kabuk {r_kabuk[0]:.3f}/{r_kabuk[1]:.3f}"
               f" -> etek {r_ust[0]:.3f}/{r_ust[1]:.3f}"
               f" (basamak {r_ust[0] - r_kabuk[0]:+.3f}/"
               f"{r_ust[1] - r_kabuk[1]:+.3f} m)")

    parts.append(hz.assign(kiy.etek(
        "Entari_Etek", col, etek_ust_z, z_etek,
        r_ust, r_alt, kiy.ENTARI_KAL, yarik=True, cy=bel_cy,
        cy_alt=etek_cy_alt), ust_mat))

    # --- KUSAK / FERACE BELI -------------------------------------------------
    #
    # Kusagi kadinda hic uretmemistim (ferace onden kapali bir DIS
    # giysidir, ustune kusak baglanmaz) ve inceleme paketi bunun bedelini
    # gosterdi: etegin ust halkasi ile govde kabugunun bittigi yer
    # arasindaki DIKIS aciga cikti — belden bakinca figurun ici gorunen
    # bir kova. Kusak yalnizca bir suslemedeymis gibi kaldirilmisti;
    # oysa bu hattaki isi o dikisi ortmekti ve kodun kendi yorumu bunu
    # zaten yaziyordu ("etek kusagin altinda baslarsa dikis kusakla
    # ortulur").
    #
    # Dogru cozum kusagi geri koymak degil, ayni isi feracenin KENDI
    # kumasiyla yapmak: daha genis, ayni renk, bagsiz bir bel bandi.
    # KADINDA BANT YOK.
    #
    # Bant, kabuk belde bitip etek belden basladigi icin ortada kalan
    # dikisi ortmek uzere konmustu. Dikis artik kalcada ve kabugun
    # kendi yuzeyi orada bitiyor; ortulecek bir sey kalmadi. Kalan tek
    # islevi "kusak gibi gorunmek"ti ve ferace kusak tasimaz.
    kusak_var = not kadin_mi
    bel_adi = "Ferace_Bel" if tip == "kadin" else "Kusak"
    bel_mat = ust_mat if tip == "kadin" else mats["kusak"]
    bel_yuk = 0.086 if tip == "kadin" else 0.055
    bel = bel_k
    # Kusak entarinin USTUNDE baglanir; ic katman degildir. Ilk turda
    # yaricapi entarininkinden kucuktu (0,024 < 0,034) ve kusak entarinin
    # ICINDE kaldi — yalnizca belin ve sirtin CUKUR yerlerinde bir damla
    # gibi disari sizdi. Bir kusak gorunmuyorsa kusak degildir.
    # Kusak entarinin YUZEYINE oturur; disari cikmasini fici bicimi
    # (kiy.band) saglar. Once buraya 12 mm daha ekliyordum ve kusak
    # giysiden ayri, ortasi bos bir cember gibi duruyordu.
    # PAY DEGISTIRILMEDI — VE SEBEBI OLCULDU.
    #
    # Inceleme karesinde kadinin belinde iki yanda koyu birer oyuk var
    # ve bunu "bandin ic duvari goruniyor" diye tesis ettim; payi 4 mm
    # ICERI cektim. Sonra kesitleri olctum ve hipotez coktu:
    #
    #   z 0,96-1,10'da  Ferace ic yaricap 0,092
    #                   Gomlek dis yaricap 0,128-0,146
    #
    # Yani band zaten govde kabugunun ICINDE; gorunen sey bir yarik
    # degil, iki kumas arasindaki dar aralikta biriken ORTAM ORTMESI.
    # Olcum hipotezi curutunce degisiklik geri alindi: yanlis bir
    # gerekceyle konulan dogru gorunumlu bir sayi, sonraki turda
    # yanlis yerde aranan bir kusur olur.
    kusak_pay = ENTARI_SIS + kiy.ENTARI_KAL + 0.002
    if kusak_var:
        parts.append(hz.assign(kiy.band(
            bel_adi, col, z_bel, (bel[0] + kusak_pay, bel[1] + kusak_pay),
            boy * bel_yuk, kiy.KUSAK_KAL, cy=bel_cy), bel_mat))

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
    # BAS OLCUSU: EN GENIS DILIM ve KENDI MERKEZI.
    #
    # Once tek bir kotta (0,955) ve `kesit` ile olculuyordu; `kesit`
    # yari-derinligi `max(|y|)` diye verir ve govdenin y ekseni
    # ortasindan degil ONUNDEN gectigi icin bu, basin nerede oldugunu
    # degil eksenin ne kadar kenarda oldugunu olcer. Ayni kusur etekte
    # ve kusakta bir kez odenmisti; baslikta duruyordu.
    bas_r = boy * 0.048
    bas_cy = 0.0
    for _o in (0.905, 0.925, 0.945, 0.965):
        _k = kiy.kesit_merkezli(govde, boy * _o)
        if _k and _k[0] > bas_r:
            bas_r, bas_cy = _k[0], _k[2]
    # KAVUK: sarigin altindaki cekirdek. Plaka 35 ve 50'de sarigin
    # tepesinden kirmizi bir tepe gorunur — sarik bir kupa degil, bir
    # cekirdegin uzerine sarilmis bezdir. Cekirdek olmadan sarigin
    # tepesindeki delikten bosluk gorunuyordu.
    # Baslik RUTBE gosterir: sokaktaki adam sarik, cocuk ve calisan genc
    # takke, kadin yasmak tasir. Yaslinin sarigi buyuktur — yasi degil
    # sehirdeki yerini anlatir.
    if tip in ("kadin", "kiz"):
        # Yasmak sac ve boyun cizgisini yutar; altina sac karti koymak
        # hem gorunmez hem de ortunun icinden batardi.
        # Kotlar basin kendi olculerinden: omuz, cene, goz, alin, tepe.
        kotlar = (boy * (0.800 if tip == "kadin" else 0.788),
                  boy * 0.885, boy * 0.928, boy * 0.958, boy * 1.008)
        y = skn.yasmak("Yasmak", col, bas_r, kotlar, cy=bas_cy,
                       yuz_acik=(tip == "kiz"))
        if y:
            parts.append(hz.assign(y, mats["yasmak"]))
    elif tip in ("cocuk", "genc"):
        t = skn.takke("Takke", col, boy * 0.965, bas_r * 1.03,
                      boy * 0.062, cy=bas_cy)
        if t:
            parts.append(hz.assign(t, mats["takke"]))
    else:
        buyuk = 1.16 if tip == "yasli" else 1.0
        # KOTLAR BASIN USTUNDE DEGIL, BASIN UZERINDE.
        #
        # Once taban 0,948 ve tepe 1,052 idi: sarik gozlerin hizasindan
        # basliyor ve basin tepesinin 9 cm USTUNE cikiyordu. Renderda
        # ne oldugu gorundu — kafanin tepesine tunemis, one dogru gozleri
        # ortmus bir silindir. Bir sarik basa GECIRILIR: alindan (kasin
        # biraz ustunden) baslar ve tepede kapanir.
        parts.append(hz.assign(kiy.band(
            "Kavuk", col, boy * 0.982, (bas_r * 0.84, bas_r * 0.78),
            boy * 0.055, 0.006, cy=bas_cy), mats["kavuk"]))
        parts.append(hz.assign(kiy.sarik(
            "Sarik", col, boy * 0.946, boy * (1.040 + 0.014 * (buyuk - 1.0)),
            bas_r * buyuk, cy=bas_cy), mats["sarik"]))

    # --- SAKAL ve SAC ----------------------------------------------------------
    #
    # AZ kart, cunku Hezarfen sarikli: sarik sacin cogunu orter ve
    # gorunen sey sakal, sakak, ense. Kafanin tamamini kaplamak hic
    # gorunmeyecek 40 kartin bedelini odemek olurdu.
    ak = (tip == "yasli")
    sac_mat = mats["beard_ak"] if ak else sk.hair_material()
    # Sac KABUGU ayri bir malzeme: kart malzemesi alfa keser (biyik ve
    # sakal ucu hala kart), kabuk ise opak olmali.
    sac_kabuk_mat = mats["beard_ak"] if ak else mats["sac"]
    # SAKAL MALZEMESI PALETTEN GELIR — IKINCI SAHIP KALDIRILDI.
    #
    # Ak sakal paletten (`beard_ak`), kestane sakal ise
    # `sac_kit.sakal_material()`ten geliyordu. Ikisi ayni rengi ve ayni
    # puruzlulugu yaziyordu, yani bir sayinin iki sahibi vardi — ve
    # olculunce ayrildiklari yer bulundu: palete sakal DOKUSU eklendi,
    # `sakal_material` dokusuz kaldi. Sonuc yakin plan karesinde
    # goruldu: yaslinin sakali dokulu, yetiskininki hala ceneye
    # gecirilmis duz bir maske.
    #
    # Tek sahip: palet. `beard` ve `beard_ak` ayni dokuyu, farkli rengi
    # tasiyor.
    sakal_mat = mats["beard_ak"] if ak else mats["beard"]
    # Sakalsizda cene hatti bos donerse hem kabuk hem tutam dongusu
    # kendiliginden bosa doner — ayri bir bayrak tutmaya gerek yok.
    hat = sk.cene_hatti(govde, boy) if skn.sakalli(tip) else []

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

        # Sakalin y merkezi: cene yayinin kendi ortalamasi. Sabit bir
        # sayi yazmak yine "govde y=0'da ortali" varsayimi olurdu.
        _cene_cy = sum(p.y for p in hat_p) / len(hat_p)

        def sakal_bolge(c):
            if c.z > z_agiz or c.z < z_dip:
                return False
            # SAKAL ENSEYE DOLANMAZ.
            #
            # Olculdu: sakal malzemesinin y siniri +0,200'e kadar
            # gidiyordu, yani ensenin arkasina. Cene yayina uzaklik tek
            # basina yetmiyor cunku yay AYNALANMIS iki yaridan olusuyor
            # ve boynun arkasi da bir yariya yakin dusebiliyor. Sakal
            # tanimi geregi ONDEDIR: cene ortasinin gerisinde kalan
            # koseler disarida.
            if c.y > _cene_cy + sakal_menzil * 0.9:
                return False
            return min((c - p).length for p in hat_p) < sakal_menzil

        sakal = kiy.kopya_kabuk(
            govde, "Sakal", col, tut=sakal_bolge,
            sisme=lambda c: 0.006 + 0.016 * min(
                1.0, max(0.0, (z_agiz - c.z) / (z_agiz - z_dip))),
            kalinlik=0.004)
        if sakal:
            # SAKALIN UV'SI GOVDEDEN GELIR VE ONA GORE YANLIS.
            #
            # Kabuk govdeden kopyalandigi icin MakeHuman'in BUTUN VUCUT
            # yerlesimini de kopyaliyor. O yerlesim ten dokusu icindir
            # ve 1:1 acilmistir; ustune 6 cm'lik dosenebilir bir sakal
            # dokusu koyunca doku yuze YAYILIYOR — renderda teller
            # birkac santimlik siyah yarik oldu.
            #
            # Sakal kendi dokusunu istiyorsa kendi yansitmasini da
            # istemeli. UV katmani silinince `_kendi_uvsi_var` False
            # doner ve parca kumaslarla ayni dunya yansitmasindan
            # gecer.
            while sakal.data.uv_layers:
                sakal.data.uv_layers.remove(sakal.data.uv_layers[0])
            parts.append(hz.assign(kiy.yumusat(sakal, 6), sakal_mat))

    # Cene ucundan sarkan tutam: kabuk yuzeye oturur, sakalin UCU
    # yuzeyden ayrilir. Birkac kart bu silueti verir.
    # SAKAL UCU KARTLARI DA KALDIRILDI.
    #
    # Sakal kabuga cevrilirken bu dort kart "silueti versin" diye
    # birakilmisti. Ayni kusuru tasiyorlar: kenardan bakildiginda
    # cizgi, ve kabuk zaten cenenin bicimini veriyor. Bir kararin
    # yarisini uygulamak, kusurun yarisini birakmaktir.
    for i, (p, yon) in enumerate(()):
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

    # --- BIYIK: KART DEGIL KABUK ----------------------------------------
    #
    # Biyik iki kartti ve karelerde ne oldugu goruldu: yandan bakinca
    # yuzun ONUNDE asili duran kahverengi bir CUBUK. Bir kart kenardan
    # bakildiginda bir cizgidir — sakalda ve sacta ayni sebeple kabuga
    # gecildi; bu ucuncusu ve ayni kusurun ALTINCI ornegi.
    if skn.sakalli(tip):
        _mk = kiy.kesit_merkezli(govde, boy * 0.905)
        _mrx, _mry, _mcy = _mk if _mk else (boy * 0.05, boy * 0.06, 0.0)

        def _biyik_bolge(c):
            # Ust dudak seridi: agzin hemen ustu, yalniz yuzun ONU.
            # DAR VE KISA: biyik dudagi orter, burnu degil. Ilk
            # denemede 0,897-0,921 ve %62 genislikti; renderda sakalla
            # birlesip agzi tamamen kapatan koyu bir DIKDORTGEN oldu.
            if not (boy * 0.899 <= c.z <= boy * 0.915):
                return False
            if abs(c.x) > _mrx * 0.50:
                return False
            return c.y < _mcy - _mry * 0.45

        _biyik = kiy.kopya_kabuk(
            govde, "Biyik", col, tut=_biyik_bolge,
            sisme=lambda c: 0.004 + 0.008 * min(
                1.0, max(0.0, (c.z - boy * 0.897) / (boy * 0.024))),
            kalinlik=0.003)
        if _biyik:
            while _biyik.data.uv_layers:
                _biyik.data.uv_layers.remove(_biyik.data.uv_layers[0])
            # Yumusatma 2 -> 6: kabuk govdenin ucgenlerini birebir
            # kopyaladigi icin kenarlari basamakli cikiyor ve karede
            # "blok" okunuyordu. Sakalda da ayni sayi kullanildi.
            parts.append(hz.assign(kiy.yumusat(_biyik, 6), sakal_mat))

    # Eski biyik kartlari KALDIRILDI; gerekcesi yukarida.
    for sx in ():
        # BIYIK DUDAGIN ONUNE KONUR, MUTLAK BIR Y'YE DEGIL.
        #
        # Once `-boy * 0,052` yaziliyordu, yani y = -8,8 cm. Olculdu:
        # kafanin ON yuzu y = -0,031. Biyik yuzun bes santim ONUNDE
        # duruyordu ve UV kusuru duzelip alfa calisinca cenenin
        # yaninda ince bir tel olarak gorundu.
        #
        # Dogru yer agiz kotundaki kesitin kendi onudur — govdenin y
        # ekseni ortasindan gecmedigi icin `kesit_merkezli` sart.
        _bk = kiy.kesit_merkezli(govde, boy * 0.905)
        _brx, _bry, _bcy = _bk if _bk else (boy * 0.05, boy * 0.06, 0.0)
        b = sk.kart(f"Biyik_{sx}", (sx * boy * 0.010,
                                    _bcy - _bry * 0.92,
                                    boy * 0.905),
                    (sx * 0.85, -0.30, -0.42), (0.0, -1.0, 0.25),
                    boy * 0.026, boy * 0.020, col, serit=2, egim=0.10)
        parts.append(hz.assign(b, sac_mat))

    # --- SAC: KART DEGIL KABUK ------------------------------------------
    #
    # Sarigin/takkenin altindan cikan sac uc karta diziliyordu. Oglanin
    # yakin plani ne oldugunu gosterdi: kulaklarin iki yaninda ince
    # TELLER, boynun cevresinde bir FIRFIR. Bir kart kenardan
    # bakildiginda bir cizgidir ve kafanin bicimini izlemez.
    #
    # Ayni kusur bu depoda BESINCI kez ve cozumu sakalda zaten
    # bulunmustu ("SAKAL: KART DEGIL KABUK"): kafanin kendi yuzleri
    # kopyalanip disari itilince sac kafanin bicimini kendiliginden
    # alir. Kart dizmek gerekmez; kafa degisirse sac da degisir.
    #
    # Yasmagin/ferace ortusunun altinda gorunmez, orada hic uretilmez.
    if tip not in ("kadin", "kiz"):
        _bk = kiy.kesit_merkezli(govde, boy * 0.900)
        _bas_rx, _bas_ry, _bas_cy = _bk if _bk else (boy * 0.05,
                                                     boy * 0.06, 0.0)
        # Baslik tabani: sarik boy*0,946'dan, takke daha asagidan
        # baslar. Sacin ustu bunun ALTINDA kalir; ustu zaten ortulu.
        # Baslik tabani turden turer: sarik boy*0,946'dan, takke
        # boy*0,965'ten baslar ama kenari daha yukari oturur. Sacin
        # ustu her ikisinin de ALTINDA kalir — orasi zaten ortulu.
        _sac_ust = boy * (0.912 if tip in ("cocuk", "genc") else 0.925)
        # SAKALSIZDA SAC KULAKTAN ASAGI INMEZ.
        #
        # Sinir 0,858 sakalliya gore secilmisti: orada sacin alt ucu
        # zaten sakala karisiyor. Sakalsiz gencte ayni sinir, yuzun iki
        # yanindan ceneye inen uzun ZULUFLER birakti — karede ince,
        # koyu iki serit olarak okundu. Sakalsizda sac kulak hizasinda
        # biter.
        _sac_alt = boy * (0.858 if skn.sakalli(tip) else 0.878)

        def _sac_bolge(c):
            if not (_sac_alt <= c.z <= _sac_ust):
                return False
            # SAC YUZE INMEZ. Yuz -y'de (kafanin on yuzu olculdu);
            # sac kafanin arka ve yan yuzudur. Sinir kafanin KENDI
            # merkezinden turer, sabit bir sayidan degil.
            # Sakalsizda sac yanaga da inmez: on sinir geri cekilir.
            return c.y > _bas_cy - _bas_ry * (
                0.30 if skn.sakalli(tip) else 0.02)

        _sac = kiy.kopya_kabuk(
            govde, "Sac", col, tut=_sac_bolge,
            # Kokte ince, ensede kalin: sac asagi dogru toplanir.
            sisme=lambda c: 0.004 + 0.010 * min(
                1.0, max(0.0, (_sac_ust - c.z) / (_sac_ust - _sac_alt))),
            kalinlik=0.004)
        if _sac:
            # UV govdeden gelir ve dosenebilir dokuya gore yanlistir —
            # sakaldaki ayni gerekce.
            while _sac.data.uv_layers:
                _sac.data.uv_layers.remove(_sac.data.uv_layers[0])
            parts.append(hz.assign(kiy.yumusat(_sac, 6), sac_kabuk_mat))

    # Eski kart dongusu KALDIRILDI; gerekcesi yukarida.
    for sx in ():
        for i, (dy, dz, ser) in enumerate((
                (0.30, 0.905, 0), (0.10, 0.895, 3), (-0.34, 0.900, 1))):
            # KESIT DEGIL KESIT_MERKEZLI.
            #
            # `kesit` yari-derinligi `max(|y|)` diye verir ve bu ancak
            # govde y=0'da ortaliysa dogrudur — degil. Ayni kusur bu
            # depoda etekte, kusakta ve sarikta ucer kez odendi; burada
            # dorduncusuydu ve olculdu: sac malzemesinin y sinirlari
            # -0,105..+0,075, oysa yuzun onu y = -0,035. Yani kartlar
            # yuzun YEDI SANTIM ONUNDE, cenenin iki yanindan omza inen
            # ince teller olarak asili duruyordu — UV kusuru duzelip
            # alfa calismaya baslayinca gorunur oldular.
            _km = kiy.kesit_merkezli(govde, boy * dz)
            if _km is None:
                _km = (boy * 0.05, boy * 0.06, 0.0)
            _rx, _ry, _cy = _km
            # TUTAM KAFAYA YAPISIK VE GENIS OLMALI.
            #
            # Kok kafanin %94'unde ve kart uzun+dar+cok egimliydi;
            # sonuc yuzun iki yaninda asili duran iki ince TEL oldu.
            # Bir kart, kenardan bakildiginda bir cizgidir — sac gibi
            # okunmasi icin genis ve yuzeye yapisik olmali.
            #
            # Kok %80'e cekildi (tutamin dibi kafanin ICINDE kalir),
            # boy kisaldi, en iki katina cikti, egim ucte bire indi.
            k = sk.kart(f"Sac_{sx}_{i}",
                        (sx * _rx * 0.80, _cy + dy * _ry, boy * dz),
                        (sx * 0.35, dy * 0.3, -1.0), (sx, dy * 0.4, 0.25),
                        boy * 0.026, boy * 0.058, col, serit=ser,
                        egim=0.10)
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


def taban_kur(args, mats, makro=None, hedef_boy=None):
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
        # GOZ KURESI OLCULDU VE REDDEDILDI.
        #
        # MakeHuman taban mesh'i `helper-l-eye` / `helper-r-eye`
        # gruplarini tasiyor ve ilk bakista bunlar goz kuresi gibi
        # duruyor. Olcum aksini soyledi: kurenin merkezine EN YAKIN
        # govde kosesi 100,7 mm — yani kafes yuzun on santim ONUNDE.
        # Adi "helper" olan sey gercekten yardimci: MakeHuman'in kendi
        # goz VARLIGININ oturacagi kafes, ve o varlik kurulu degil
        # (sistem varliklari indirilmemis; `AssetService` bos donuyor).
        #
        # Kafesi kucultup yuvaya oturtmayi denedim; her denemede baska
        # bir sey kaydi (patlak goz, sonra kasin hizasinda beyaz halka).
        # Bir seyi dogru yere koyabilmek icin once o yerin NEREDE
        # oldugunu bilmek gerekiyor ve govde onu soylemiyor.
        #
        # Gozun yeri BILINEN tek yer UV uzayi: MPFB2'nin kendi
        # `mpfb_eyelids` maskesi goz kapagi adalarini tam olarak
        # isaretliyor. Bu yuzden goz artik geometri degil, deri
        # dokusunun icine ciziliyor (`gen_deri_texture.py`) — bilinen
        # bir olcuye, bilinmeyen bir tahmine degil.
        govde = mp.taban_getir_mpfb(
            col=hz.collection(COLLECTION),
            makro=makro or HEZARFEN_MAKRO,
            hedef_boy=hedef_boy)
        goz = None      # gerekce yukarida: kafes goz degil
        hz.log(f"taban: MPFB2 parametrik — {mp.olc(govde)}")
    else:
        govde = kar.taban_getir(args.taban, col=hz.collection(COLLECTION))
        goz = None      # paket govdesinde goz kuresi yok
        hz.log("taban: Blender Studio CC0 paketi")
    kar.temiz_ag(govde)
    # Goz GOVDEYLE AYNI donusumleri alir — ayri hesaplanmaz.
    aci = kar.one_cevir(govde, birlikte=(goz,))
    # Normalizasyon HEDEF BOYA gore yapilir, sabite gore degil. Ilk
    # kosuda burasi her zaman 1,70 m'ye normalize ediyordu: MPFB2
    # kadini dogru sekilde 1,58 m uretiyor, sonraki satir onu tekrar
    # 1,70'e cekiyor ve olcum "boy 1,58, hedef 1,7" diye patliyordu.
    # Yani cesitlilik uretiliyor, bir satir sonra siliniyordu.
    k = kar.normalize(govde, hedef_boy or kar.HEDEF_BOY, birlikte=(goz,))
    hz.assign(govde, mats["skin"])
    # Kure yuvasina OTURTULUR — buyuklugu varsayilmaz, yuva olculur.
    mp.goz_yuvaya_otur(goz, govde)
    skn.goz_boya(goz, mats)
    return govde, aci, k, goz


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
    # Dokuz varyantin hepsini kosmak ~dort dakika; tek bir arketipin
    # silueti uzerinde donerken bunu dokuz kez odemenin anlami yok.
    # Katalog kismi kosuda YAZILMAZ (asagida), yoksa filtre kataloglu
    # varyantlari siler.
    ap.add_argument("--only", default=None,
                    help="Yalniz bu varyantlari uret (virgulle ayrik ad)")
    args = ap.parse_args(hz.argv_after_dashes())

    os.makedirs(args.out_dir, exist_ok=True)
    os.makedirs(args.blend_dir, exist_ok=True)
    catalog = []

    # --- 1) CIPLAK TABAN ----------------------------------------------------
    hz.reset_scene()
    col = hz.collection(COLLECTION)
    mats, tex_sizes = kit.build_materials("default", textured=not args.no_textures)
    asset = "SK_Hezarfen_Govde"
    govde, aci, k, _goz = taban_kur(args, mats)
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
    secili = None
    if args.only:
        secili = {a.strip() for a in args.only.split(",") if a.strip()}
        bilinmeyen = secili - {v["ad"] for v in VARIANTS}
        if bilinmeyen:
            raise SystemExit(f"[HZ] HATA: bilinmeyen varyant: "
                             f"{sorted(bilinmeyen)}")

    for _v in VARIANTS:
        if secili is not None and _v["ad"] not in secili:
            continue
        ad = _v["ad"]
        etek_orani = _v["etek"]
        dizlik_var = _v["dizlik"]
        tip = _v["tip"]
        why = _v["why"]
        hz.reset_scene()
        col = hz.collection(COLLECTION)
        mats, tex_sizes = kit.build_materials("default",
                                              textured=not args.no_textures)
        asset = f"SK_{ad}"
        govde, _, _, goz = taban_kur(args, mats, makro=_v["makro"],
                                     hedef_boy=_v["boy"])
        giysi, kol_esik, z_kol_alt = giydir(govde, col, mats, etek_orani,
                                            dizlik_var, tip=tip)

        # ETEK KOTLARI BIRLESTIRMEDEN ONCE OLCULUR.
        #
        # `join_parts` gövdeyi TUKETIR; sonra `hz.bounds(govde)` demek
        # "StructRNA of type Object has been removed" verir. Olcum,
        # olctugu sey hala varken alinir.
        #
        # Oranlar `giydir` ile ayni kaynaktan: bir sayinin iki sahibi
        # olmamali (bu depoda uc kez bedeli odendi).
        _mn, _mx = hz.bounds(govde)
        _boy = _mx[2] - _mn[2]
        z_bel_k = _boy * kiy.BEL_ORAN
        z_etek_k = _boy * etek_orani
        # Adlar BIRLESTIRMEDEN once alinir: birlestirme parca nesnelerini
        # siler ve sonradan okumak "StructRNA has been removed" verir.
        giysi_adlari = sorted({o.name.split(".")[0] for o in giysi})
        giysi_sayisi = len(giysi)

        # Eklemler CIPLAK govdeden olculur, giyinikten degil: entari
        # omzu 3,4 cm kalinlastirir ve omuz eklemini o kadar disari
        # atardi. Rig tenin altindadir.
        eklem = rk.eklemleri_olc(govde, kol_esik, z_kol_alt)
        # Denetim, OLCULEN govdenin boyuna gore boler; sabite gore degil.
        # Burada `kar.HEDEF_BOY` (1,70) yaziyordu ve tek boy varken bu
        # gorunmuyordu: 1,24 m'lik cocuk uretilir uretilmez dizi 0,216
        # oraninda buldu ve "0,24-0,32 olmali" diye reddetti. Cocugun
        # dizi dogru yerdeydi — 0,216 x (1,70/1,24) = 0,296 — bolen
        # yanlisti. Bir oran, payini kimden aldigini bilmiyorsa oran
        # degildir.
        _mn0, _mx0 = hz.bounds(govde)
        rig_hata = rk.uzuv_denetimi(eklem, _mx0[2] - _mn0[2])
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
        # GIYSIYE UV VER — BIRLESTIRMEDEN ONCE, GOVDEYE DOKUNMADAN.
        #
        # Bu satir yoktu ve zincirin basindaki kusur buydu: giysi
        # parcalari bmesh'ten kuruluyor, hicbir UV uretilmiyor, dolayisiyla
        # takilacak bir doku da olamiyordu. Olculdu: on iki kumas
        # malzemesinin HEPSI `kind=untextured` — duz albedo, normal yok,
        # purzuluk yok. HDRP'de dokusuz albedo her zaman plastik okur;
        # karakterin "gercekci degil" gorunmesinin tek buyuk sebebi
        # modelin bicimi degil YUZEYIYDI.
        #
        # Yansitma parca parca ve yuzun kendi duzleminde (`uv_project`);
        # kumas icin dogru olan bu, cunku dokuma bir DESEN degil bir
        # yuzey ozelligidir — nereden bakilirsa bakilsin iplik sikligi
        # ayni olmali. Elle acilmis bir UV yerlesimi burada daha iyi bir
        # sey vermezdi, yalnizca elle yapilan bir adim eklerdi.
        #
        # GOVDE DISARIDA: MPFB2 govdesi kendi insan UV yerlesimini
        # tasiyor ve deri dokusu (Faz B) ona baglanacak. Dunya yansitmasi
        # onu ezerdi.
        # KENDI UV'SIYLE GELEN PARCAYA DOKUNULMAZ.
        #
        # Ilk yazimda dunya yansitmasi butun parcalara uygulaniyordu ve
        # sac kartlarinin ELLE kurulmus UV'sini eziyordu. Renderda ne
        # oldugu gorundu: sakal tutamlari ve sakak kartlari cenenin
        # altinda kahverengi CUBUKLAR olarak asili kaldi — alfa kesme
        # atlasin yanlis yerini ornekledigi icin kart saydam olmayi
        # birakti.
        #
        # Kural: bir parca kendi UV'sini getiriyorsa onu daha iyi bilir.
        # Denetim "katman var mi" degil "katmanda VERI var mi" — bmesh
        # ile kurulan her ag bos bir katmanla geliyor ve varlik sinamasi
        # butun giysiyi atlardi.
        for _p in giysi:
            if _kendi_uvsi_var(_p):
                kit.uv_adini_duzelt(_p)
                continue
            kit.apply_uvs(_p, tex_sizes)
        # Govdenin katmani da ayni adla — birlestirme ada bakiyor.
        kit.uv_adini_duzelt(govde)

        # GOZ DE BIRLESIR: ayri nesne kalsaydi deriye baglanmaz ve
        # kafa donerken yerinde kalirdi.
        _parcalar = [govde] + giysi + ([goz] if goz is not None else [])
        lod0 = kit.join_parts(_parcalar, f"{asset}_LOD0", col)
        # UCUNCU KADEME — MERDIVEN ZATEN VARDI, BASAMAK YOKTU.
        #
        # `ImportLanding.KarakterUc` uc kademelik bir esik merdiveni
        # tasiyor (0,22 / 0,04 / 0,010) ama karakterler iki kademeyle
        # geliyordu, yani ucuncu basamak olu koddu — bu depoda tekrar
        # eden "yazildi, baglanmadi" kusurunun bir baskasi.
        #
        # Bedeli olculebilir: kalabalikta ayni anda 60 govde ciziliyor
        # ve her biri LOD1'de ~17.500 ucgen — yalniz insanlar icin
        # 1,05 milyon ucgen.
        #
        # Oran 0,08 comert, agresif degil: LOD2 esigi 0,04 ekran
        # yuksekligi, yani 1080p'de ~43 piksellik bir insan. 4.700
        # ucgen o boyda hala piksel basina birden fazla ucgen demek.
        lod2 = kar.desimasyon(lod0, 0.08, f"{asset}_LOD2")
        lod1 = kar.desimasyon(lod0, 0.30, f"{asset}_LOD1")
        # BIRLESMIS AGDA DA TEK KATMAN.
        #
        # Parcalarin adi duzeltildi ama birlestirme yine ikinci bir
        # katman birakabiliyor (kart ve mest gibi `apply_uvs`'e
        # ugramayan parcalar kendi `Float2`siyle geliyor). Bos bir
        # ikinci katman FBX'e de gider ve Unity'de hangisinin
        # kullanildigi ice aktarma ayarina kalir — yani kusur geri
        # gelebilecegi bir kapi acik kalir.
        for _m in (lod0, lod1, lod2):
            kit.uv_adini_duzelt(_m)
        hz.link(lod1, col)
        hz.link(lod2, col)
        for m in (lod0, lod1, lod2):
            rk.deri_bagla(m, arm)
            rk.agirliklari_tamamla(m, arm)

        # GIYSI, ALTINDAKI TENIN KEMIGINI TAKIP ETSIN.
        #
        # Oyun karesinde Hezarfen'in SIRTI CIPLAK cikiyordu: kollar
        # giyinik, kusak yerinde, ama omuzlarla kusak arasinda ten
        # goruluyor — omuz kemigi ve omurga cizgisiyle. Blender'in
        # bind-poz karesi tertemiz giyinik; aradaki tek fark HAREKET.
        #
        # Olculdu: birlesik agda giysi koselerinin %67'si, hemen
        # altindaki ten kosesinden 0,30'dan fazla farkli agirlik
        # tasiyor (ortalama fark 1,20, en buyugu 2,20 — tamamen baska
        # kemikler). Iki kabuk ayri hareket edince gomlegin 8 mm'lik
        # payi govdeyi iceride tutmuyor.
        #
        # `agirliklari_tamamla`nin kendi aciklamasi bu cozumu adiyla
        # aniyordu: "daha zarif bir cozum ... olurdu ama olculebilir
        # farki belirsiz". Belirsizligi olcum kapatti.
        agirlik_once = 0.0
        for m in (lod0, lod1, lod2):
            _degisen, _once = rk.agirliklari_govdeden_al(m)
            if m is lod0:
                agirlik_once = _once
                hz.log(f"giysi agirligi tenden alindi: {_degisen} kose "
                       f"(onceki ortalama fark {_once:.2f})")

        # ETEK SALINIM KEMIKLERI.
        #
        # Once baglanan agirliklarin USTUNE yazilir: etek koseleri
        # belde govdeye, ucta salinim kemigine bagli olacak sekilde
        # karisir. Sirasi onemli — `agirliklari_tamamla` her koseyi
        # en yakin kemige tam agirlikla baglar ve once kosarsa etek
        # kemikleri hicbir sey almaz.
        salinim = 0
        for m in (lod0, lod1, lod2):
            salinim += rk.etek_kemikleri(arm, m, z_bel_k, z_etek_k)
        hz.log(f"etek salinimi: {salinim} kose 4 zincire baglandi")

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
            # Etek kotu bu GOVDENIN boyundan turer. Once `kar.HEDEF_BOY`
            # (1,70) yaziyordu: 1,24 m'lik oglanin etegi katalogda 0,54 m
            # gorunuyordu — cocugun gogus hizasi. Kaydin isi olculen seyi
            # yazmaktir, sabiti degil.
            etek_kotu=round(_boy * etek_orani, 3),
            # Arketipin TIPI ve CIPLAK boyu. Unity tarafi NPC govdesini
            # buradan taniyor: hangi tohum hangi govdeyi alacak ve o
            # govde kac metreye olceklenecek. Sayinin tek sahibi bu
            # satir — Unity'de ikinci bir tablo yazilmaz.
            tip=tip, taban_boy=round(_boy, 4),
            cinsiyet=skn.cinsiyet(_v["makro"]),
            yas_bandi=skn.yas_bandi(_v["makro"]),
            dizlik=dizlik_var, giysi_parca=giysi_sayisi,
            giysi_adlari=giysi_adlari,
            tris_lod0=kar.hz_tri(lod0), tris_lod1=kar.hz_tri(lod1),
            tris_lod2=kar.hz_tri(lod2),
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
            # GIYSI-TEN AGIRLIK FARKI: kusurun sayisi.
            #
            # 0 = giysi altindaki tenle birebir ayni kemigi takip
            # eder. Buyudukce iki kabuk hareket ederken ayrisir ve
            # govde giysinin icinden cikar.
            # KOL CIZGISININ EN BUYUK DONUSU (derece).
            #
            # Bir uzvun merkez cizgisi neredeyse duzdur. Yaslinin
            # cizgisi 171 derece donuyordu — cunku BACAKLA basliyor,
            # sonra omza atliyordu. Karede kollar dirsekten geriye
            # kirilmis duruyordu ve hicbir sayi bunu soylemiyordu.
            kol_donusu=round(_KOL_DONUS[0], 1),
            agirlik_farki_once=round(agirlik_once, 4),
            agirlik_farki=round(rk.agirlik_farki(lod0)[1], 4),
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
        # Esik arketipin KENDI hedefidir: bir cocugun 1,24 m'yi degil
        # 1,70 m'yi asmasini beklemek, cocugun bozuk cikmasini gormezden
        # gelmek demekti.
        hedef = _v["boy"] or kar.HEDEF_BOY
        if bilgi["boy"] > hedef * 1.10:
            raise SystemExit(
                f"[HZ] HATA {ad}: giyinik boy {bilgi['boy']:.3f} m — ciplak "
                f"{hedef} m'nin %10'undan fazla ustunde.")
        # Bir kabuk secimi bos donerse giysi sessizce eksik kalir ve
        # karakter yari ciplak cikar.
        #
        # Bunu ONCE bir SAYI ile denetliyordum ("en az 48 parca"). Sayi
        # yanlis seyi olcuyordu: sakal 54 karttan tek bir kabuga
        # dusunce denetim, giysinin tamami yerinde oldugu halde uretimi
        # reddetti. Bir esik, saydigi seyin ne oldugunu bilmiyorsa
        # yalan soyler — burada gereken sayi degil, ADLARDIR.
        #
        # Liste TIPE gore degisir. Ilk denemede sabit kalmisti ve yedi
        # arketipin besi uretimden "Kusak, Kavuk, Sarik, Sakal eksik"
        # diye dondu — oysa kadinda kusak, cocukta sarik, kizda sakal
        # OLMAMASI gerekiyordu. Bir denetim, dogru olani yanlis sayiyorsa
        # denetim bozuktur.
        zorunlu = {"Gomlek", "Salvar", "Entari_Ust", "Entari_Etek",
                   "Mest_-1", "Mest_1"}
        if tip == "kadin":
            # `Ferace_Bel` LISTEDEN CIKTI: ferace kusak tasimaz ve dikis
            # artik kalcada, kabugun kendi yuzeyinin altinda. Ortulecek
            # bir sey kalmayinca ortu de kalmadi.
            zorunlu |= {"Yasmak"}
        elif tip == "kiz":
            zorunlu |= {"Yasmak", "Kusak"}
        elif tip in ("cocuk", "genc"):
            zorunlu |= {"Kusak", "Takke"}
        else:
            zorunlu |= {"Kusak", "Sakal", "Kavuk", "Sarik"}
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
               f"{bilgi['tris_lod0']:6d} / {bilgi['tris_lod1']:5d} / "
               f"{bilgi.get('tris_lod2', 0):5d} ucgen")

        hz.save_blend(os.path.join(args.blend_dir, f"{asset}.blend"))
        if args.export:
            export_fbx(os.path.join(args.out_dir, f"{asset}.fbx"),
                       collection_name=COLLECTION, skinned=True)

    if not args.export:
        hz.log("FBX yazilmadi (--export ile): karakter rig'siz Unity'ye gitmez.")

    catalog.sort(key=lambda v: v["name"])
    # Kismi kosuda katalog YAZILMAZ: `--only` ile bir varyanti uretip
    # katalogu ustune yazmak, uretilmeyen sekizinin kaydini silerdi —
    # ve kayit silinince "hangi varyant kac ucgen" sorusunun cevabi
    # sessizce kaybolurdu.
    if args.only:
        hz.log(f"kismi kosu ({args.only}) — katalog yazilmadi")
        return
    os.makedirs(os.path.dirname(os.path.abspath(args.catalog)), exist_ok=True)
    with open(args.catalog, "w", encoding="utf-8") as fh:
        json.dump({"variants": catalog}, fh, ensure_ascii=False, indent=1)
    hz.log(f"{len(catalog)} karakter durumu; katalog: {args.catalog}")


if __name__ == "__main__":
    main()
