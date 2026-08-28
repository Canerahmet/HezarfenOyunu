"""
Hezarfen: 1632 — Katman 2 ambiyans replik korpusu (PLAN Bölüm 11.3).

    "Anahtar NPC diyalog ağaçları ve binlerce satırlık ambiyans repliği
    korpusu (satıcı bağırışları — Evliya'da belgeli, mahalle dedikoduları,
    dönem olaylarına göndermeler, hafif dönem Türkçesi tınısı) Claude
    tarafından offline üretilir, gözden geçirilir ve statik veri olarak
    gemiye konur."

## Neden bir ÜRETİCİ, neden elle yazılmış bir metin dosyası değil

"Binlerce satır" elle yazılırsa gözden geçirilemez: kimse üç bin repliği
tek tek okuyup "bunda dönem hatası var mı" diye bakmaz. Oysa **şablon ve
dolgu listesi okunabilir** — kırk şablon, altmış mal adı, on mahalle.
Korpus onların çarpımıdır.

Yani gözden geçirilen şey bu dosyadır; korpus onun genişlemesi. Aynı
sebeple dönem denetimi de burada, üretim anında koşar: yasaklı bir sözcük
korpusa girerse üretim **durur**, sessizce geçmez.

## Çalışma zamanında bulut LLM YOK

CLAUDE.md kuralı. Bu dosya geliştirme zamanında koşar, çıktısı statik
veridir. Oyun çalışırken hiçbir şey üretilmez, yalnızca seçilir.

Kullanım:
  python tools/content/gen_bark_korpusu.py
"""

import argparse
import itertools
import json
import os
import random
import re
import sys
import unicodedata

# --- MESLEK ve VAKIT: Unity tarafındaki enum'ların AYNISI -----------------
#
# Sayilar NPCMeslek.Tip ve VakitHesabi.Vakit ile birebir. Ikisi ayrisirsa
# korpus yanlis kisiye yanlis vakitte konusturur ve bu SESSIZ bir hatadir;
# bu yuzden Unity tarafinda bir test iki listeyi karsilastirir.
MESLEK = ["Esnaf", "Hamal", "Kayikci", "Yeniceri", "Ases",
          "SuSaticisi", "Dilenci", "Cocuk", "Imam", "Medreseli"]

VAKIT = ["Fecr", "Sabah", "Gunes", "Ogle", "Ikindi", "Aksam", "Yatsi"]

# Replik turleri — Unity tarafinda BarkTuru.
TUR = ["Satis", "Dedikodu", "Selam", "Is", "Uyari", "Dua"]

# Aranma kosulu: 0 farketmez, 1 yalniz aranirken, 2 yalniz aranmiyorken.
FARKETMEZ, ARANIRKEN, TEMIZKEN = 0, 1, 2

# --- KRONOLOJI ESIKLERI (Kronoloji.cs ile ayni) ---------------------------
KAHVE_YASAGI = (1633, 245)      # 2 Eylul 1633 fermani
CIBALI_YANGINI = (1633, 238)    # 26 Agustos 1633

# 1632'nin belgeli olaylari (RESEARCH §7) — dedikodu bunlara gonderme yapar.
ZORBA_ISYANI = (1632, 38)       # 7 Subat 1632, Atmeydani
HAFIZ_PASA = (1632, 41)         # 10 Subat 1632


# ------------------------------------------------------------------ DOLGU

# Mal adlari — hepsi 1632 Istanbul'unda BULUNUR.
#
# Yeni Dunya bitkileri (patates, domates, misir, biber) ve cay LISTEDE
# YOKTUR: Osmanli mutfagina cok daha gec girerler ve bir satici bagirisinda
# gecmeleri en gorunur donem hatasi olurdu. Denetim de onlari ariyor.
MALLAR_YIYECEK = [
    ("ekmek", "somun"), ("fodla", "fodla"), ("simit", "simit"),
    ("pide", "pide"), ("boregi", "borek"), ("helva", "helva"),
    ("pekmez", "pekmez"), ("bal", "bal"), ("peynir", "peynir"),
    ("zeytin", "zeytin"), ("zeytinyagi", "yag"), ("pirinc", "pirinc"),
    ("bulgur", "bulgur"), ("nohut", "nohut"), ("mercimek", "mercimek"),
    ("kuru uzum", "uzum"), ("incir", "incir"), ("hurma", "hurma"),
    ("badem", "badem"), ("ceviz", "ceviz"), ("findik", "findik"),
    ("kestane", "kestane"), ("leblebi", "leblebi"), ("erik", "erik"),
    ("kavun", "kavun"), ("karpuz", "karpuz"), ("uzum", "uzum"),
    ("elma", "elma"), ("ayva", "ayva"), ("nar", "nar"),
    ("balik", "balik"), ("uskumru", "uskumru"), ("palamut", "palamut"),
    ("midye", "midye"), ("yogurt", "yogurt"), ("ayran", "ayran"),
    ("serbet", "serbet"), ("sirke", "sirke"), ("tuz", "tuz"),
]

MALLAR_ESYA = [
    ("cömlek", "cömlek"), ("testi", "testi"), ("bakrac", "bakrac"),
    ("hasir", "hasir"), ("kilim", "kilim"), ("keçe", "keçe"),
    ("ip", "ip"), ("halat", "halat"), ("mum", "mum"), ("fener", "fener"),
    ("sabun", "sabun"), ("cuval", "cuval"), ("sepet", "sepet"),
    ("bicak", "bicak"), ("nal", "nal"), ("semer", "semer"),
    ("kürk", "kürk"), ("aba", "aba"), ("bez", "bez"), ("ignelik", "ignelik"),
]

# Yalniz YASAKTAN ONCE bagirilabilir.
MALLAR_YASAKLI = [("kahve", "kahve"), ("tütün", "tütün")]

# Bozahane de bir zaman isareti: IV. Murad doneminde kapatildi (RESEARCH
# §5(c)). Kahveyle ayni esige baglandi — ikisi birlikte kalkar.
MALLAR_BOZA = [("boza", "boza")]

SIFAT_TAZE = ["taze", "sicacik", "yeni", "gunun ilki", "firindan yeni"]
SIFAT_IYI = ["ala", "hasi", "temiz", "kokusu yerinde", "el degmemis"]

SEMTLER = ["Galata", "Tophane", "Kasimpasa", "Eminonu", "Unkapani",
           "Eyup", "Uskudar", "Balat", "Fener", "Cibali", "Ayvansaray"]

ISKELELER = ["Karakoy", "Eminonu", "Kasimpasa", "Unkapani", "Eyup",
             "Uskudar"]


def _yok(*_a, **_k):
    raise SystemExit("kullanilmiyor")


# ---------------------------------------------------------------- SABLON
#
# Her sablon: (tur, meslek listesi, vakit listesi, metin, kaynak notu,
#              aranma kosulu, tarih araligi)
#
# Metindeki `{...}` yuvalar dolgudan doldurulur. Ayni sablon farkli
# dolgularla onlarca replik uretir; okunmasi gereken sey SABLONdur.

HEPSI = list(range(len(VAKIT)))
GUNDUZ = [VAKIT.index(v) for v in ("Gunes", "Ogle", "Ikindi")]
SABAH = [VAKIT.index(v) for v in ("Sabah", "Gunes")]
AKSAMUSTU = [VAKIT.index(v) for v in ("Ikindi", "Aksam")]
GECE = [VAKIT.index(v) for v in ("Aksam", "Yatsi")]


def sablonlar():
    """Şablon listesi. Okunacak yer burasıdır."""
    S = []

    def ekle(tur, meslekler, vakitler, metin, kaynak,
             aranma=FARKETMEZ, bas=None, son=None):
        S.append(dict(tur=tur, meslekler=meslekler, vakitler=vakitler,
                      metin=metin, kaynak=kaynak, aranma=aranma,
                      bas=bas, son=son))

    # --- SATICI BAGIRISLARI ------------------------------------------
    #
    # Evliya'nin 1638 Esnaf Alayi'nda ~1100 lonca tek tek sayilir; carsinin
    # sesi bagiristir. Bicimler donem Turkcesinin tinisini tasir ama
    # anlasilir kalir — oyuncu sozluk acmak zorunda kalmamali.
    satis_bicim = [
        "{sifat} {mal}! {sifat} {mal}!",
        "Buyur {mal}, {sifat}!",
        "Bre {mal}ci geldi! {sifat} {mal}!",
        "{mal}im var, {sifat}!",
        "Alan gitsin, {sifat} {mal}!",
        "Hey! {sifat} {mal}, kalmadi kalmayacak!",
        "{mal}! Sabahin {mal}i!",
        "Gel bakalim, {sifat} {mal} burada!",
    ]
    for b in satis_bicim:
        ekle("Satis", ["Esnaf"], GUNDUZ, b,
             "Evliya Celebi 1638 Esnaf Alayi — carsinin sesi bagiristir "
             "(RESEARCH.md 6). Bicim T2: tini rekonstruksiyon.")

    # Su saticisi ayri bir meslek ve ayri bir bagiris.
    for b in ["Su! Buz gibi su!", "Sebil! Icene helal!",
              "Suyum var, {semt} suyu!", "Susayan gelsin, su!",
              "Allah icin bir tas su!"]:
        ekle("Satis", ["SuSaticisi"], HEPSI, b,
             "Su saticisi Evliya'nin lonca listesinde; sebil ve su "
             "dagitimi vakif duzeninin parcasi (RESEARCH.md 6).")

    # --- KAYIKCI: HALIC'TE KOPRU YOK ---------------------------------
    #
    # Bu bagiris bir renk degil bir MEKANIK duyurusu: 1632'de karsiya
    # yuruyerek gecilmez, ulasim kayikladir.
    for b in ["Karsiya! {iskele}'ye kalkiyor!",
              "Pereme! {iskele}! Bir kisi daha!",
              "Kayik kalkiyor, {iskele}'ye!",
              "Gecen gelsin! {iskele}!",
              "Bosuna bekleme, kopru yok — kayiga bin!"]:
        ekle("Satis", ["Kayikci"], HEPSI, b,
             "Halic'te kopru YOK; ulasim kayik ve peremedir, iskeleler "
             "tarifelidir (RESEARCH.md 6).")

    # --- HAMAL: YUK ISKELEDEN CARSIYA --------------------------------
    for b in ["Savul! Yuk geliyor!", "Destur! Yol ver!",
              "Cekil kenara, sirtimda {mal} var!",
              "{iskele}'den {semt}'e, iki kap daha!",
              "Bu yuk benim degil, sirtim benim."]:
        ekle("Is", ["Hamal"], GUNDUZ, b,
             "Yuk akisi iskele-carsi hattindadir; tekerlekli araba nadir "
             "(RESEARCH.md 6).")

    # --- SELAM ve DUA -------------------------------------------------
    for b in ["Selamunaleykum.", "Aleykumselam.", "Sabahin hayr olsun.",
              "Hayirli isler.", "Allah kolaylik versin.",
              "Vakit yaklasti, hazir ol."]:
        ekle("Selam", ["Esnaf", "Hamal", "Kayikci", "Imam", "Medreseli",
                       "SuSaticisi"], HEPSI, b,
             "Gundelik selamlasma. T3 — tini, belge degil.")

    for b in ["Allah kabul etsin.", "Ecrini Mevla versin.",
              "Rabbim afiyet versin.", "Sifa Allah'tan."]:
        ekle("Dua", ["Imam", "Medreseli", "Dilenci"], HEPSI, b,
             "T3 — tini.")

    # --- DILENCI ------------------------------------------------------
    for b in ["Allah rizasi icin bir akce...",
              "Bir lokma, bir hirka...",
              "Sadaka belayi def eder.",
              "Aç degilim ama üç gündür yemedim."]:
        ekle("Satis", ["Dilenci"], HEPSI, b,
             "Dilenci Evliya'nin lonca listesinde ayri bir kalemdir. T3 tini.")

    # --- COCUK --------------------------------------------------------
    for b in ["Mektep bitti! Kostuk!", "Bana da ver!",
              "Gordun mu? Kayiga bindim!", "Anne bekliyor, geç kaldım!"]:
        ekle("Selam", ["Cocuk"], GUNDUZ, b, "T3 tini.")

    # --- IMAM: VAKIT ve MAHALLE DUZENI --------------------------------
    for b in ["Vakit girdi, buyurun.", "Cemaat bekliyor.",
              "Mahalle kefildir, unutma.",
              "Kim kime kefil, defterde yazili."]:
        ekle("Is", ["Imam"], HEPSI, b,
             "Mahalle imami kayit/kefalet sorumlusudur (RESEARCH.md 6).")

    # --- ASES ve YENICERI: KOLLUK -------------------------------------
    for b in ["Yolun acik olsun.", "Gec bakalim.",
              "Bu saatte nereye?", "Durma, yuru."]:
        ekle("Selam", ["Ases", "Yeniceri"], HEPSI, b,
             "Subasi/asesbasi/yeniceri kollugu (RESEARCH.md 6).",
             aranma=TEMIZKEN)

    # Aranirken kolluk baska konusur.
    for b in ["Dur! Sen dur!", "Yakalayin sunu!",
              "Kacma! Daha beter olur!", "Subasiya goturecegiz seni."]:
        ekle("Uyari", ["Ases", "Yeniceri"], HEPSI, b,
             "Kolluk aranma durumunda mudahale eder.", aranma=ARANIRKEN)

    # FENER: yalniz yasak yururlukteyken ve YALNIZ GECE.
    for b in ["Fenerin nerede senin?", "Fenersiz gezilmez, bilmiyor musun?",
              "Isik yok, hesap var."]:
        ekle("Uyari", ["Ases"], GECE, b,
             "Geceleri fenersiz dolasmak yasak (RESEARCH.md 6); yasak "
             "kahve/tutun sertlesmesiyle ayni esige baglandi (ADR: "
             "Kronoloji.FenerZorunlu).",
             bas=KAHVE_YASAGI)

    # Halk da fenerden bahseder.
    for b in ["Fenerini yak, ases geziyor.",
              "Gece yurumek zorlasti.",
              "Eskiden bu saatte carsi dolu olurdu."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Kayikci"], GECE, b,
             "Yasak sonrasi gundelik hayatin degismesi.", bas=KAHVE_YASAGI)

    # --- DEDIKODU: DONEM OLAYLARINA GONDERME --------------------------
    #
    # Hepsi tarih KAPILI: bir olay olmadan once ondan bahsedilmez. Bu,
    # kronolojinin diyalogda da gorunmesi demek.
    for b in ["Atmeydani'nda ne oldu duydun mu?",
              "Sipahiler ayaklandi diyorlar.",
              "Kelle isteyenler saraya dayanmis."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Kayikci", "Medreseli"],
             HEPSI, b,
             "7 Subat 1632 zorba (sipahi+yeniceri) isyani, Atmeydani "
             "(RESEARCH.md 7). T1 olay, replik T3 tini.",
             bas=ZORBA_ISYANI)

    for b in ["Pasayi padisahin gozu onunde parcalamislar.",
              "Hafiz Pasa'ya yazik oldu.",
              "Yerine Topal Recep Pasa gelmis."]:
        ekle("Dedikodu", ["Esnaf", "Medreseli", "Yeniceri"], HEPSI, b,
             "10 Subat 1632: Sadrazam Hafiz Ahmed Pasa saray avlusunda "
             "oldurulur; yerine Topal Recep Pasa (RESEARCH.md 7, TDV). "
             "T1 olay.",
             bas=HAFIZ_PASA)

    for b in ["Cibali'de yangin varmis!",
              "Kalafatcinin atesinden ciktigini soyluyorlar.",
              "Yarisi kul oldu diyorlar, kimi ceyregi diyor."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Kayikci", "Imam"], HEPSI, b,
             "26 Agustos 1633 Cibali yangini; ORAN TARTISMALI — Katip "
             "Celebi beste bir, baskalari dortte bir/beste dort "
             "(RESEARCH.md 6). Replik de tartismayi tasir.",
             bas=CIBALI_YANGINI)

    for b in ["Kahvehaneleri kapattilar.",
              "Eyup'te yuz yirmi dukkan yikilmis.",
              "Ferman cikti, artik yok.",
              "Sohbet kaldi, kahve kalmadi."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Medreseli", "Kayikci"],
             HEPSI, b,
             "2 Eylul 1633 fermani; Eyup ve civarinda 120 kahve dukkani "
             "yiktirildi (TDV 'Kahve', BA A.DVN nr. 25/47). T1.",
             bas=KAHVE_YASAGI)

    # Yasaktan ONCE kahvehane siradan bir yerdir.
    for b in ["Aksam kahvehanede miyiz?",
              "Meddah gelecekmis kahveye.",
              "Kahvede oturur konusuruz."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Medreseli"], AKSAMUSTU, b,
             "1632'de kahvehaneler ACIK; eglence meddah/Karagoz/kahvehane "
             "sohbetidir (RESEARCH.md 6).",
             son=KAHVE_YASAGI)

    # Veba: 1630'larda endemik.
    for b in ["Yine hastalik varmis {semt}'te.",
              "Komsunun oglu yatiyormus.",
              "Allah korusun, kapiyi kapat."]:
        ekle("Dedikodu", ["Esnaf", "Imam", "Hamal"], HEPSI, b,
             "1630'larda tekrarlayan veba salginlari; veba Istanbul'da "
             "endemikti (RESEARCH.md 6).")

    # Gundelik, olaysiz dedikodu — korpusun govdesi.
    for b in ["{semt} tarafi bugun kalabalik.",
              "Narh yine degismis diyorlar.",
              "Iskelede sira uzun.",
              "Lodos var, kayik sallanir bugun.",
              "Bu yil uzum bol.",
              "Ustad {semt}'e tasinmis.",
              "Dun gece kopekler susmadi.",
              "Cesme yine akmiyor.",
              "Firinci erken kapatti."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Kayikci", "SuSaticisi",
                          "Cocuk"], HEPSI, b, "T3 — gundelik tini.")

    # --- INCE MESLEKLERIN DERINLESTIRILMESI --------------------------
    #
    # Ilk turda toplam 4676 replik vardi ve bu sayi bir seyi GIZLIYORDU:
    # 4234'u esnafindi. Dilencinin sekiz repligi vardi. Oyuncu bir
    # dakikada hepsini duyar ve sehir bir teybe doner.
    #
    # Onemli olan toplam degil KISI BASINA cesitlilik; asagidaki taban
    # denetimi de onu olcuyor.

    # ASES — gece kollugunun kendi dili.
    for b in ["Kim var orada?", "Yuzunu goreyim.",
              "Bu mahallede tanimadigim adam gezmez.",
              "Ne isin var bu saatte disarida?",
              "Evine git, gece uzun.",
              "Subasi bizi bosuna gezdirmiyor.",
              "Gecen hafta {semt}'te iki kisi tuttuk.",
              "Sabaha kadar buradayiz.",
              "Sesini alcalt.", "Yurumeye devam.",
              "Bir daha gorursem konusuruz.",
              "Mahalle imami kefil mi sana?",
              "Adin ne, kimin nesisin?",
              "Karanlikta is olmaz."]:
        ekle("Selam", ["Ases"], GECE, b,
             "Subasi/asesbasi kollugu gece devriyesi yurutur; mahalle "
             "kefalet duzeni imam uzerindendir (RESEARCH.md 6).",
             aranma=TEMIZKEN)

    # YENICERI — ocak mensubu, kolluktan farkli konusur.
    for b in ["Ocak duruyor, biz duruyoruz.",
              "Sefere cikacagiz derler, ciktigimiz yok.",
              "Ulufe gecikti yine.",
              "{semt} kolu bugun bizde.",
              "Cekil, gecelim.",
              "Agalar ne derse.",
              "Bu isin sonu iyi degil.",
              "Kazan kalkarsa duyulur.",
              "Bekle, sirasi gelir.",
              "Burada durma, dagil.",
              "Sen kendi isine bak."]:
        ekle("Selam", ["Yeniceri"], HEPSI, b,
             "Yeniceri ocagi kolluk gorevi de yapar; 1632 zorba isyani "
             "sipahi+yeniceri hareketidir (RESEARCH.md 6-7). T3 tini.",
             aranma=TEMIZKEN)

    for b in ["Atmeydani'nda ne oldugunu unutmadik.",
              "O gun meydanda biz de vardik.",
              "Kelle isteyen kalabalik dagilmadi kolay."]:
        ekle("Dedikodu", ["Yeniceri"], HEPSI, b,
             "7 Subat 1632 zorba isyani, Atmeydani (RESEARCH.md 7). "
             "Olay OLMADAN once anilamaz.",
             bas=ZORBA_ISYANI)

    # DILENCI — kapi kapi degil, gecit basinda.
    for b in ["Allah rizasi icin...",
              "Bir lokma yeter bana.",
              "Sadaka omur uzatir.",
              "Ben de bir zamanlar calisirdim.",
              "Elim ayagim tutmuyor evladim.",
              "Cesme basinda beklerim, bilirsin.",
              "Kimseye yuk olmak istemem ama...",
              "Kis yaklasiyor.",
              "Imam efendi bilir beni.",
              "Bugun kimse durmadi.",
              "Sag olasin, Allah artirsin.",
              "Bir tas corba...",
              "Gozum gormuyor, sesini taniyorum.",
              "Yolun acik olsun, bana bakma."]:
        ekle("Satis", ["Dilenci"], HEPSI, b,
             "Dilenci Evliya'nin lonca listesinde ayri bir kalemdir; "
             "sadaka ve vakif duzeni gundelik hayatin parcasi. T3 tini.")

    # COCUK — mektep, sokak, oyun.
    for b in ["Hoca gormeden kacalim!",
              "Bugun ders zor geldi.",
              "Sen kimsin, burali degilsin.",
              "Babam kayikci, biliyor musun?",
              "Kuleye cikan var mi?",
              "Bana bir akce ver, isini yapayim.",
              "Kosalim, gec kaldik!",
              "Annem cagiriyor.",
              "Cesmeden su getirecegim.",
              "Sen ucabilir misin?",
              "Dun bir adam ucmus diyorlar!",
              "Simit alacaktim ama param yok.",
              "Kedi gordun mu, benimki kayboldu.",
              "Burada oynuyoruz, dikkat et."]:
        ekle("Selam", ["Cocuk"], GUNDUZ, b,
             "Cocuklar sabah mektebe gider (NPCMeslek cizelgesi); "
             "Hezarfen anlatisi sehirde bilinir. T3 tini.")

    # IMAM — mahallenin kaydi ve duzeni ondadir.
    for b in ["Mahalleye yeni gelen var mi, kayda gecelim.",
              "Kefilsiz kimse oda tutamaz.",
              "Cemaat azaldi bu aralar.",
              "Cesmenin vakfi bozulmus, bakacagiz.",
              "Yetim hakki agirdir.",
              "Komsu hakki once gelir.",
              "Bu isin sonu mahkemeye varir.",
              "Kadi efendiye danisalim.",
              "Vakit girmeden gelin.",
              "Sadakayi gizli ver.",
              "Su meselesini konusalim.",
              "Defterde adin yazili mi?"]:
        ekle("Is", ["Imam"], HEPSI, b,
             "Mahalle imami kayit ve kefalet sorumlusudur; anlasmazlik "
             "kadi mahkemesine gider (RESEARCH.md 6, Istanbul Kadi "
             "Sicilleri).")

    # MEDRESELI — kitap, ders, sehrin entelektuel sesi.
    for b in ["Ders bitti, kutuphaneye gidiyorum.",
              "Hocanin dedigini anlamadim.",
              "Katip Celebi'yi okudun mu?",
              "Hesap ilmi zor is.",
              "Muvakkit vakti nasil buluyor, hic dusundun mu?",
              "Golgeye bak, ikindi yaklasti.",
              "Kitap pahali, istinsah edecegim.",
              "Medresede yer kalmadi.",
              "Bu mesele fetvalik.",
              "Ilim Cin'de de olsa.",
              "Bir soru soracaktim.",
              "Yazacak kagit bulamadim."]:
        ekle("Dedikodu", ["Medreseli"], GUNDUZ, b,
             "Muvakkit vakitleri hesaplayan matematik/astronomi egitimli "
             "gorevlidir (RESEARCH.md 6); medrese sehrin ilim duzeni. "
             "T3 tini.")

    # KAYIKCI — suyun kendi dili.
    for b in ["Lodos kalkti, dikkat.",
              "Akinti bugun ters.",
              "Poyraz olursa gecemeyiz.",
              "Bu havada karsiya gecilmez.",
              "Kurekci mi ariyorsun?",
              "Yukun agirsa iki kayik lazim.",
              "Iskelede sira var, bekle.",
              "Gece tarife baska.",
              "Uskudar uzak, ucret ona gore.",
              "Denizi bilmeyen kayiga binmesin.",
              "Tut kenari, sallanacak.",
              "Bugun on sefer yaptim."]:
        ekle("Is", ["Kayikci"], HEPSI, b,
             "Kayik/pereme ana ulasim; iskeleler tarifeli, ucret mesafeye "
             "gore (RESEARCH.md 6, Ekonomi.Ucret). Lodos oyunun ucus "
             "geri beslemesi (PLAN 12).")

    # HAMAL — sirt, yuk, yol.
    for b in ["Bu yuk bana agir gelmez.",
              "Gunde on sefer, sirtim benim degil artik.",
              "Iskeleden hana, handan carsiya.",
              "Yol ver, dokulecek!",
              "Ucretini pesin al derler, almadim.",
              "Arkadaki de benim.",
              "Merdiven varsa iki kat ucret.",
              "Yagmurda yuk kayar.",
              "Bugun {iskele}'ye uc sefer.",
              "Ustad nerede, bekliyorum.",
              "Sirtimda yuz okka var."]:
        ekle("Is", ["Hamal"], GUNDUZ, b,
             "Yuk iskele-han-carsi hattinda akar; tekerlekli araba nadir "
             "(RESEARCH.md 6).")

    # CUMA — haftanin tek ozel gunu diyalogda da duyulmali (ADR 0071).
    for b in ["Cuma bugun, camiye gidiyoruz.",
              "Kepenkleri erken kapatacagim, Cuma var.",
              "Cemaat kalabalik olur bugun.",
              "Hutbeye yetiselim.",
              "Cuma namazi mescitte kilinmaz, camiye."]:
        ekle("Dedikodu", ["Esnaf", "Hamal", "Imam", "Medreseli",
                          "Kayikci"], [VAKIT.index("Ogle")], b,
             "Cuma namazi minberi olan camide, cemaatle kilinir "
             "(ADR 0071).")

    # Yuvali replikler: ayni cumle sehrin farkli yerlerini anarak
    # cogalir. Bir bekci "Kasimpasa kolu bugun bizde" derken oyuncuya
    # sehrin buyuklugunu de soyluyor.
    for b in ["{semt} tarafinda isler nasil?",
              "{semt}'e giden var mi?",
              "Bu gece {semt} kolundayim.",
              "{semt}'te bir hadise olmus.",
              "{semt} yolunu bilir misin?"]:
        ekle("Selam", ["Ases", "Yeniceri"], HEPSI, b,
             "Kolluk mahalle mahalle kol gezer (RESEARCH.md 6). T3 tini.",
             aranma=TEMIZKEN)

    for b in ["{semt}'ten geliyorum, yol uzun.",
              "{semt}'te kimse durmadi bugun.",
              "Vaktiyle {semt}'te evim vardi."]:
        ekle("Satis", ["Dilenci"], HEPSI, b,
             "Dilenci Evliya'nin lonca listesinde ayri bir kalemdir. "
             "T3 tini.")

    for b in ["Bir {mal} versene, aç kaldım.",
              "{mal} kokusu geliyor, dayanamiyorum."]:
        ekle("Satis", ["Dilenci"], GUNDUZ, b,
             "Sadaka ve vakif duzeni gundelik hayatin parcasi. T3 tini.")

    # Aranan biri gecerken halk baska konusur.
    for b in ["Bu kim, ne yaptı?", "Uzak dur sundan.",
              "Ases pesinde galiba.", "Baska yerde dolas evladim."]:
        ekle("Uyari", ["Esnaf", "Hamal", "Cocuk", "SuSaticisi"], HEPSI, b,
             "Kalabalik aranma durumuna tepki verir.", aranma=ARANIRKEN)

    return S


# ---------------------------------------------------------------- DENETIM

# Korpusa ASLA girmemesi gerekenler.
#
# Bu bir uslup listesi degil bir DONEM KAPISIdir. Her biri bir sebeple
# burada; sebep yaninda yazili ki ileride kimse "neden yasak" diye
# sormasin ve sessizce silmesin.
YASAK = {
    "tulumba": "ilk tulumba teskilati 1720'ler (Gercek Davud)",
    "patates": "Yeni Dunya bitkisi; Osmanli mutfagina cok gec girer",
    "domates": "Yeni Dunya bitkisi",
    "misir": "Yeni Dunya tahili (sehir adi degil, bitki)",
    "cay": "Osmanli'da yayginlasmasi 19-20. yy",
    "saat basi": "alaturka saatte 'saat basi' kavrami boyle isle­mez",
    "dakika": "gundelik dilde olcu birimi degil",
    "kibrit": "modern kibrit 19. yy",
    "gazete": "Osmanli'da ilk gazete 19. yy",
    "vapur": "buharli gemi 19. yy",
    "kopru": None,   # ozel kural: asagida
    "polis": "kolluk subasi/asesbasi/yeniceridir; 'polis' 19. yy",
    "karakol": "19. yy kurumu",
    "hastane": "donem karsiligi darussifa/bimarhane",
    "pastane": "gec donem",
    "lokanta": "gec donem",
    "banka": "gec donem",
    "sigara": "tutun sarilarak icilir; 'sigara' gec donem",
}


def _sadelestir(s):
    """Türkçe aksanları düşürerek karşılaştırma için sadeleştirir."""
    s = s.lower()
    s = (s.replace("ı", "i").replace("ş", "s").replace("ğ", "g")
          .replace("ü", "u").replace("ö", "o").replace("ç", "c")
          .replace("İ", "i"))
    return unicodedata.normalize("NFKD", s)


def denetle(metin, kaynak):
    """
    Dönem denetimi. Bulursa üretimi DURDURUR.

    Sessizce atlamak, denetimi olmayan bir denetimdir: korpus yine üretilir
    ve kimse eksiği görmez.
    """
    d = _sadelestir(metin)
    for kelime, sebep in YASAK.items():
        if kelime == "kopru":
            # "kopru yok" DOGRUDUR ve bilerek yazilmistir; "kopruden gec"
            # donem hatasidir. Yani yasak olan sozcuk degil IDDIA.
            if re.search(r"kopru(den|ye|yu|nun)\b", d):
                raise SystemExit(
                    f"[HZ] DONEM HATASI: {metin!r} — 1632'de Halic'te "
                    "kopru YOK; gecis kayikladir.")
            continue
        if re.search(r"\b" + re.escape(_sadelestir(kelime)), d):
            raise SystemExit(
                f"[HZ] DONEM HATASI: {metin!r} icinde {kelime!r} — {sebep}")
    if not kaynak:
        raise SystemExit(f"[HZ] kaynaksiz replik: {metin!r}")


# ---------------------------------------------------------------- URETIM

def _yuvalar(metin):
    return set(re.findall(r"\{(\w+)\}", metin))


def uret(tohum=1632):
    rng = random.Random(tohum)
    replikler = []
    gorulen = set()

    for s in sablonlar():
        yuva = _yuvalar(s["metin"])

        # Yuvalarin degerleri: her yuvanin kendi havuzu.
        havuzlar = {}
        if "mal" in yuva:
            mal = MALLAR_YIYECEK + MALLAR_ESYA
            # Yasakli mallar kendi tarih araliklarinda AYRI uretilir.
            havuzlar["mal"] = [m[1] for m in mal]
        if "sifat" in yuva:
            havuzlar["sifat"] = SIFAT_TAZE + SIFAT_IYI
        if "semt" in yuva:
            havuzlar["semt"] = SEMTLER
        if "iskele" in yuva:
            havuzlar["iskele"] = ISKELELER

        anahtarlar = sorted(havuzlar)
        kombinasyonlar = (list(itertools.product(
            *[havuzlar[a] for a in anahtarlar])) if anahtarlar else [()])

        for kombo in kombinasyonlar:
            degerler = dict(zip(anahtarlar, kombo))
            metin = s["metin"].format(**degerler)
            denetle(metin, s["kaynak"])
            for meslek in s["meslekler"]:
                anahtar = (metin, meslek)
                if anahtar in gorulen:
                    continue
                gorulen.add(anahtar)
                replikler.append(_replik(metin, meslek, s))

    # Yasakli mallar: yalniz yasaktan ONCE bagirilir.
    for mal, _ in MALLAR_YASAKLI + MALLAR_BOZA:
        for b in ["{sifat} {mal}!", "Buyur {mal}!", "{mal}im var!"]:
            for sifat in SIFAT_IYI:
                metin = b.format(mal=mal, sifat=sifat)
                denetle(metin, "x")
                replikler.append(_replik(
                    metin, "Esnaf",
                    dict(tur="Satis", vakitler=GUNDUZ, aranma=FARKETMEZ,
                         bas=None, son=KAHVE_YASAGI,
                         kaynak=("Kahve/tutun 2 Eylul 1633 fermaniyla, boza "
                                 "IV. Murad doneminde kapatildi — ucu de "
                                 "1632'de ACIK, sonra YOK (RESEARCH.md 6, "
                                 "5(c))."))))

    rng.shuffle(replikler)
    for i, r in enumerate(replikler):
        r["id"] = f"BK{i:05d}"
    return replikler


def _replik(metin, meslek, s):
    vakit_maske = 0
    for v in s["vakitler"]:
        vakit_maske |= 1 << v
    bas = s.get("bas") or (0, 0)
    son = s.get("son") or (0, 0)
    return dict(
        id="", metin=metin,
        meslek=MESLEK.index(meslek),
        vakit=vakit_maske,
        tur=TUR.index(s["tur"]),
        aranma=s.get("aranma", FARKETMEZ),
        enErkenYil=bas[0], enErkenGun=bas[1],
        enGecYil=son[0], enGecGun=son[1],
        kaynak=s["kaynak"],
    )


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--out", default=os.path.join(
        "unity", "HezarfenGame", "Assets", "_Project", "Resources", "Bark",
        "bark_korpusu.json"))
    args = ap.parse_args()

    replikler = uret()
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    with open(args.out, "w", encoding="utf-8") as fh:
        json.dump({"replikler": replikler}, fh, ensure_ascii=False, indent=0)

    # Kapsam raporu: hangi meslek kac replik aldi. Bir meslek SIFIR alirsa
    # o kisi oyunda hic konusmaz ve bu sessizce olur.
    say = {m: 0 for m in MESLEK}
    for r in replikler:
        say[MESLEK[r["meslek"]]] += 1
    print(f"[HZ] {len(replikler)} replik -> {args.out}")
    for m in MESLEK:
        isaret = "  " if say[m] else "! "
        print(f"[HZ] {isaret}{m:12s} {say[m]:5d}")
    # TABAN: toplam sayi bir seyi gizler.
    #
    # Ilk turda 4676 replik vardi ve "binlerce satir" olcusu karsilanmis
    # gorunuyordu — ama 4234'u esnafindi ve dilencinin sekiz repligi
    # vardi. Onemli olan toplam degil KISI BASINA cesitlilik: oyuncu bir
    # meslegin butun repliklerini bir dakikada duyuyorsa o meslek
    # konusmuyor, TEKRAR EDIYOR.
    TABAN = 40
    ince = [f"{m}={say[m]}" for m in MESLEK if say[m] < TABAN]
    if ince:
        raise SystemExit(f"[HZ] cesitlilik tabani {TABAN} altinda: "
                         f"{', '.join(ince)} — bu meslekler tekrar eder.")


if __name__ == "__main__":
    main()
