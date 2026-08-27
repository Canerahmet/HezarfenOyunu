"""
Hezarfen: 1632 — Landmark kataloğu (plan Görev 10 / Faz 1 madde 3).

`refs/maps/landmarks_1632.geojson` üretir: uçuş ekseninin S-kademesi ve suriçinin
A-kademesi (plan Bölüm 8), her biri **iki ayrı güven ekseniyle**:

  * `tier`            — 1632'deki VARLIĞI ve DURUMU ne kadar sağlam? (T1/T2/T3)
                        Kaynak: docs/RESEARCH.md §3.
  * `position_confidence` — KONUMU ne kadar kesin?

Bu ikisini karıştırmamak önemlidir. Ayasofya'nın 1632'de ayakta olduğu **belgelidir**
(T1) ama bu dosyadaki koordinatı elle girilmiş, ~100 m mertebesinde yaklaşıktır.
Tek bir "güven" alanı kullansaydım, ya tarihi zayıf ya konumu fazla iddialı
göstermek zorunda kalırdım.

Konumların kesinleştirilmesi plan Faz 1 madde 3'ün işidir (dönem haritalarının
georeferanslanması + OSM ayak izleri). O gelene kadar hepsi `approx`.

Kullanım:
    tools/gis/.venv/Scripts/python.exe tools/gis/landmarks_build.py --dir data/gis/istanbul
"""

import argparse
import json
import math
import os
import sys

import numpy as np

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

from geodesy import (from_utm35n as _from_utm35n,  # noqa: E402
                     to_utm35n as _to_utm35n)


# rasterio.warp.transform yerine gecen kabuk — imza AYNI, gerekce geodesy.py
# basliginda: bu makinede rasterio'nun DLL'i Windows uygulama denetimince
# bloklu ve butun GIS araclarini birden calistirilamaz kiliyordu.
# Yalnizca WGS84 <-> UTM 35N (EPSG:32635) yonu desteklenir; bu boru hattinin
# kullandigi tek donusum odur ve baska bir CRS istenirse SESSIZCE yanlis
# sonuc vermek yerine yukselir.
def warp_transform(src_crs, dst_crs, xs, ys):
    wgs, utm = "EPSG:4326", "EPSG:32635"
    if (src_crs, dst_crs) == (wgs, utm):
        pairs = [_to_utm35n(x, y) for x, y in zip(xs, ys)]
    elif (src_crs, dst_crs) == (utm, wgs):
        pairs = [_from_utm35n(x, y) for x, y in zip(xs, ys)]
    else:
        raise ValueError(f"desteklenmeyen donusum: {src_crs} -> {dst_crs}")
    return [p[0] for p in pairs], [p[1] for p in pairs]

# DEM okumasi GEODESY'DEN alinir, coastline_build'den DEGIL.
#
# coastline_build -> dem_fetch -> rasterio zinciri bu makinede
# "Application Control policy" ile engelli (SETUP.md [INSAN] maddesi) ve
# `geodesy` tam bu yuzden yazildi. Eski satir import zincirini rasterio'ya
# geri baglayarak modulu calistirilamaz kiliyordu.
from geodesy import load_dem, utm_to_grid   # noqa: E402

# Konum guveni etiketleri
APPROX = "approx (~100 m; elle girildi, Faz 1 madde 3'te georeferanslanacak)"
ANCHOR = "anchor (dunya orijini — dem_fetch.py ile ayni sabit)"
SURVEYED = "olculu (Kultur Envanteri kaydindaki koordinat)."

# tier: RESEARCH.md §3'teki 1632 DURUMUNA gore.
#   Documented     = donem kaynaklariyla ayakta oldugu belgeli
#   Reconstruction = yeri/varligi makul ama bicim veya sinir kurgusal
#   Legend         = tek kaynak (Evliya Celebi)
LANDMARKS = [
    # --- S-kademe: ucus ekseni (plan Bolum 8) ---
    dict(id="LM_GalataKulesi", name="Galata Kulesi", grade="S",
         lon=28.974017, lat=41.025637, tier="Documented", position_confidence=ANCHOR,
         state_1632="Ayakta ama bugunkunden ALCAK ve farkli: 1509 depremi sonrasi onarimli, "
                    "sivri ahsap/kursun kulahli; tersane ambari ve zindan islevli; "
                    "yangin gozetleme kulesi DEGIL (o islev 18. yy).",
         research_ref="RESEARCH.md §3 Galata; kule yuksekligi notu (1794'te ~1,9 m indirildi)"),

    dict(id="LM_GalataSurlari", name="Galata surları ve kapıları", grade="S",
         lon=28.9735, lat=41.0230, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta; Azapkapi, Karakoy/Kule Kapisi vb. Kule etrafinda hendek ve burclar.",
         research_ref="RESEARCH.md §3 Galata (Eremya Celebi)"),

    dict(id="LM_Okmeydani", name="Okmeydanı", grade="S",
         lon=28.961319, lat=41.055858, tier="Documented",
         position_confidence="OLCULDU (kendi arazimizde). Onceki deger "
                             "yesil poligonun agirlik merkeziydi ve YAMACA "
                             "dusuyordu: 400x400 m icinde 94,1 m kot "
                             "yayilimi — 845,66 m'lik menzil rekoru orada "
                             "atilamaz. 2x2 km taranarak en duz nokta "
                             "bulundu: 700 m doguda, kot 94,5 m, 300x300 m "
                             "yayilim 10,1 m; 30 derece yonunde 900 m'lik "
                             "koridorda yalnizca 5,6 m. Nokta mevcut yesil "
                             "poligonun ICINDEDIR, yani poligon dogru, "
                             "yalnizca merkezi temsil etmiyordu.",
         state_1632="Acik talim alani. II. Bayezid vakfiyesi meydanda YAPI, "
                    "MEZAR, SU YOLU, BAG ve BAHCE yapilmasini yasaklar — "
                    "yani meydan bir YOKLUK kaydidir. Okcular (Kemankes) "
                    "Tekkesi ve MINBERLI NAMAZGAH mevcut; menzil taslari "
                    "dikili (132 abide tespit edilmis, meydan rekoru "
                    "845,66 m). Hezarfen'in talim yaptigi yer.",
         research_ref="RESEARCH.md §5.8; ADR 0041"),

    dict(id="LM_OkcularTekkesi", name="Okçular (Kemankeş) Tekkesi",
         grade="A", lon=28.961319, lat=41.055858, tier="Documented",
         position_confidence="turetilmis: olculen PLATOYA oturtuldu; "
                             "tekkenin 1632'deki tam yeri icin olculu "
                             "koordinat bulunamadi.",
         state_1632="Ayakta. 1624-25'te GURCU MEHMED PASA mescidi onartip "
                    "MINBER ekletti — yani 1632'de mescit yeni onarilmis "
                    "ve minberlidir. **MINARESIZ**: tekke mescidinin "
                    "minaresi ancak 1770-71'de eklendi. IV. Murad devrinde "
                    "meydan seyhi Haci Suleyman padisahin hocasiydi.",
         research_ref="RESEARCH.md §5.8; ADR 0041"),

    dict(id="LM_OkmeydaniNamazgah", name="Okmeydanı namazgâhı (minberli)",
         grade="A", lon=28.962050, lat=41.056420, tier="Documented",
         position_confidence="turetilmis: platoda, tekkenin ~80 m "
                             "kuzeydogusunda; ikisinin arasindaki mesafe "
                             "BELGELI DEGILDIR.",
         state_1632="Ayakta ve minberli. Acik hava namazgahi; meydanin "
                    "vakif duzeninin parcasi.",
         research_ref="RESEARCH.md §5.8; ADR 0041"),


    dict(id="LM_IncliKosk", name="İncili Köşk (Sinan Paşa Köşkü)", grade="S",
         lon=28.988070, lat=41.014361, tier="Documented",
         # SU USTUNDE: kosk Bizans deniz surunun onune eklenen kemerli alt
         # yapiya oturur ve cumbasi denize tasar. "Kara landmark'i denizde
         # olamaz" denetimi bu yuzden atlanir — kural degil, ISTISNA ve
         # gerekcesi kaynakta.
         on_water=True,
         position_confidence="turetilmis: kaynak konumu 'Sarayburnu'ndan "
                             "kiyi boyunca ~300 m, Soter Filantropos "
                             "kalintisi ile Ahirkapi arasi' diye verir; "
                             "nokta KENDI 1632 kiyi cizgimizde o mesafede "
                             "olculdu (kot 0,1 m). ONCEKI DEGER 156 m "
                             "YANLISTI (28,9866/41,0135): denizden 125 m "
                             "iceride ve 14,7 m yukarida kaliyordu, oysa "
                             "kosk denize TASAR.",
         state_1632="AYAKTA, 41 yasinda. 998/1590'da baslandi, 999/1590-91'de "
                    "tamamlandi; bani Koca Sinan Pasa, mimar DAVUD AGA. "
                    "Marmara tarafindaki Bizans DENIZ SURU uzerinde, kesme "
                    "tas kemerli alt yapiya oturur. Evliya'ya gore IV. Murad "
                    "Hezarfen'in ucusunu BURADAN izledi; Lagari de onune "
                    "indi (anlati T3, yapinin kendisi T1). 1632'DE YOK: "
                    "1871-72 yikimi ve sahil demiryolu.",
         research_ref="RESEARCH.md §5.6; ADR 0039"),

    # on_water: DENIZDE olmasi BEKLENEN tek landmark. Kayalik adacik 30 m'lik DEM
    # hucresinden kucuktur, dolayisiyla arazi orada 0 m okur — bu bir veri hatasi
    # degil, cozunurluk sinirdir. Adacik Faz 3'te elle modellenecek.
    dict(id="LM_KizKulesi", name="Kız Kulesi", grade="S", on_water=True,
         lon=29.0041, lat=41.0211, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta, kayalik adacik uzerinde. Donem siluetiyle modellenecek. "
                    "Adacik DEM'de yok (30 m cozunurluk) — Faz 3'te elle eklenecek.",
         research_ref="RESEARCH.md §3 (Kiz Kulesi, Yedikule, Hisarlar ayakta)"),

    dict(id="LM_UskudarMihrimah", name="Üsküdar Mihrimah (İskele) Camii", grade="S",
         lon=29.0160674, lat=41.0267985, tier="Documented",
         position_confidence=SURVEYED
         + " ONCEKI DEGER ~164 m YANLISTI (29,0148/41,0257, elle girilmisti)."
           " Hatayi KULLIYENIN KENDISI ele verdi: medrese ile sibyan mektebi"
           " olculu koordinatlariyla eklenince, belgeli goreli konumlar"
           " ('medrese caminin dogusunda', 'mektep kible tarafinda')"
           " tutmadi.",
         state_1632="Ayakta. Kitabe 954/1548, Mimar Sinan. (Onceki kayit "
                    "\"1560'lar\" diyordu; o Edirnekapi Mihrimah'idir, 1566.) "
                    "Kubbe dis cap 11,40 m, kilit 24,20 m; UC yarim kubbeli "
                    "planin Istanbul'daki ilk ve tek ornegi. Iskele ve "
                    "kayiklar. 1632'de meydana TEK BASINA hakim: Yeni Valide "
                    "Camii (1710) ve III. Ahmed Meydan Cesmesi (1728) YOK.",
         research_ref="RESEARCH.md §5.4; ADR 0036"),

    dict(id="LM_Dogancilar", name="Doğancılar Meydanı", grade="S",
         lon=29.012677, lat=41.018907, tier="Legend", position_confidence=SURVEYED
         + " ONCEKI DEGER 771 m YANLISTI (29,0181/41,0245, elle girilmisti) ve "
           "Galata'ya 3709 m veriyordu; hicbir modern rakamla uyusmuyordu.",
         state_1632="Hezarfen'in INIS NOKTASI. Tek kaynak (Evliya) — T3. "
                    "Meydan 1632'de faal; adi imparatorluk DOGANCI ocagindan "
                    "gelir. Cevrede 1632'de ayakta: Cakircibasi Hasan Pasa "
                    "(Dogancilar) Camii ve ~400 m kuzeydogusunda Aziz Mahmud "
                    "Hudayi tekke-camii. Mesafe/kot rakamlari modern "
                    "yorumculara ait ve celiskili.",
         research_ref="RESEARCH.md §5.5; Caveats (3358/3400/3558 m celiskisi)"),

    dict(id="LM_MihrimahMedrese",
         name="Üsküdar Mihrimah Sultan Medresesi", grade="A",
         lon=29.016325, lat=41.027229, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="Ayakta ve faal; 1548, Mimar Sinan, caminin DOGUSUNDA. "
                    "Kubbeli bir dershane ve ON ALTI ogrenci hucresi (TDV; "
                    "IBB Kulturel Miras). 1632'de 84 yasinda. 1632'DE YOK: "
                    "1961 onarimi — o tarihte saglik ocagina cevrildi ve ic "
                    "ozelliklerini yitirdi.",
         research_ref="RESEARCH.md §5.4; ADR 0038"),

    dict(id="LM_MihrimahMektebi",
         name="Üsküdar Mihrimah Sultan Sıbyan Mektebi", grade="A",
         lon=29.016231, lat=41.026531, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="Ayakta ve faal; yapim 1547-48, Mimar Sinan. Caminin "
                    "KIBLE tarafinda, aradan kucuk bir yol gecer. Kubbeli "
                    "dershane + kubbeli ACIK EYVAN (kislik ve yazlik "
                    "bolumler); dikdortgen planli. YAMACTA oldugu icin "
                    "altina DUKKAN eklenmistir. 1632'DE YOK: bugunku cocuk "
                    "kutuphanesi islevi.",
         research_ref="RESEARCH.md §5.4; ADR 0038"),

    dict(id="LM_DogancilarCamii",
         name="Çakırcıbaşı Hasan Paşa (Doğancılar) Camii", grade="S",
         lon=29.012054, lat=41.019186, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="Ayakta. 1548'de Mimar Sinan yapti (bani Cakircibasi "
                    "Hasan Pasa); 1580'lerde harap dusunce Haci Ahmed Pasa "
                    "yeniden yaptirdi ve avluya kendi turbesini ekledi — "
                    "1632'de gorulen 1580'ler yapisidir. Duvarlar KAGIR, "
                    "cati AHSAP, TEK minare. 1857 duzenlemesi ve 1858-59 "
                    "Sayeste Kadinefendi onarimi 1632'de YOK.",
         research_ref="RESEARCH.md §5.5; ADR 0037"),

    dict(id="LM_HudayiTekkesi", name="Aziz Mahmud Hüdâyî tekke-camii",
         grade="S", lon=29.014431, lat=41.022267, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="Ayakta. Hudayi arsayi 1589'da aldi; ilk tekke "
                    "1003/1595'te tamamlandi, 1007/1598-99'da minber "
                    "eklenerek camiye cevrildi. 1632'de 37 yasinda ve IV. "
                    "Murad doneminin en etkili seyh tekkesi. Ahsap catili. "
                    "1850 yangini ve 1272/1855-56 yeniden insasi 1632'de YOK.",
         research_ref="RESEARCH.md §5.5; ADR 0037"),

    dict(id="LM_HudayiTurbesi",
         name="Aziz Mahmud Hüdâyî türbesi (açık türbe)", grade="A",
         lon=29.014380, lat=41.022180, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="AYAKTA ve YENI. Hudayi Safer 1038'de (Ekim 1628) oldu; "
                    "turbe ayni hicri yil icinde, 1038'de (1628-29) "
                    "yapildi — 1632'de yapi UC-DORT yasindadir. ACIK "
                    "(baldaken) turbe: TDV 1850 yangini oncesi ayakta kalan "
                    "yapiyi oyle tanimlar; bugunku kubbe DORT MERMER SUTUN "
                    "uzerine oturur. 1632'DE YOK: bugunku kapali kagir "
                    "kabuk, 7,40x8,80 m plan, on uc dilimli kubbe ve yedi "
                    "pencere — 1272/1855-56 yeniden insasi.",
         research_ref="RESEARCH.md §5.5; ADR 0037"),

    dict(id="LM_TopkapiAdaletKulesi", name="Topkapı — Adalet Kulesi",
         grade="S", lon=28.9832592, lat=41.0123787, tier="Documented",
         position_confidence=SURVEYED,
         state_1632="AYAKTA ama bugunkunden ALCAK. Fatih'in duzeninde "
                    "tasarlandi; KANUNI 1527-29'da tas bolumu ekletti ve "
                    "Kubbealti'na bakan hunkar penceresi acildi. 1632'deki "
                    "bicim: UC tas kat + AHSAP ust kat + KURSUN PIRAMIDAL "
                    "kulah. 1632'DE YOK: II. Mahmud'un (1819-20) ekledigi "
                    "dorduncu tas kat, ahsap seyir bolumu ve yukseltilmis "
                    "kulah; Abdulaziz'in bugunku SIVRI kulahi. Galata "
                    "Kulesi'ndekiyle ayni hata ailesi.",
         research_ref="RESEARCH.md §5.7; ADR 0040"),

    dict(id="LM_TopkapiBabusselam", name="Topkapı — Bâbüsselâm (Orta Kapı)",
         grade="S", lon=28.9830036, lat=41.0113068, tier="Documented",
         position_confidence=SURVEYED + " (onundeki binek tasi kaydi).",
         state_1632="AYAKTA. CIFTE KONIK KULAHLI kapi; ikinci avluya (Divan "
                    "Meydani) acilir. Kuleler 1632'de vardir — tartisma "
                    "yalnizca kimin ekledigidir (Necipoglu Fatih, yaygin "
                    "gorus Kanuni) ve iki ihtimal de 1632'den oncedir.",
         research_ref="RESEARCH.md §5.7; ADR 0040"),

    dict(id="LM_TopkapiSiluet", name="Topkapı Sarayı (siluet)", grade="S",
         lon=28.9834, lat=41.0115, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta. Revan (1636) ve Bagdat (1639) koskleri HENUZ YOK; Alay Koksu var. "
                    "Sofa-i Humayun genislemesi tamamlanmamis.",
         research_ref="RESEARCH.md §3 Saraylar ve koskler"),

    # --- A-kademe: suriçi (plan Bolum 8) ---
    dict(id="LM_Ayasofya", name="Ayasofya", grade="A",
         lon=28.9800538, lat=41.0085237, tier="Documented",
         position_confidence="turetilmis: nokta yapinin ANA KUBBE MERKEZIDIR "
                             "(prefabin pivotu da orasi). Onceki elle girilmis "
                             "deger 15 m otedeydi — hata degil ama daha iyi bir "
                             "sayi vardi. Yapinin ekseni de olculdu: apsis "
                             "123,5 derece; izgara kiblesi 150,40, yani "
                             "AYASOFYA KIBLEYE DONUK DEGILDIR ve katalog "
                             "face_deg=303,5 bildirir.",
         state_1632="Ayakta, cami. DORT minarenin dordu de yerinde (batidaki "
                    "ikizler III. Murad'in ilk yillarinda tamam). Fatih'in "
                    "YARIM KUBBE UZERINDEKI AHSAP minaresi 1574'te SOKULDU. "
                    "Vaftizhane ayakta ama YAGHANEDIR: I. Mustafa (1639) ve "
                    "Sultan Ibrahim (1648) oraya sonra gomulur. "
                    "1632'DE YOK: I. Mahmud'un sadirvani, kutuphanesi, sibyan "
                    "mektebi ve imareti (1739-40); III. Ahmed'in hunkar "
                    "mahfili (1728); Fossati'nin (1847-49) sivasi ve KIRMIZI "
                    "YATAY SERITLERI — bugunku okra renk ondan da sonradir.",
         research_ref="RESEARCH.md §5.11, ADR 0045 (TDV; Muller-Wiener; Grelot)"),

    dict(id="LM_Suleymaniye", name="Süleymaniye Külliyesi", grade="A",
         lon=28.9639, lat=41.0165, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta, tam faal. 3. tepe.",
         research_ref="RESEARCH.md §3 (TDV; Muller-Wiener)"),

    dict(id="LM_Sultanahmet", name="Sultanahmet Camii", grade="A",
         lon=28.9768842, lat=41.0052538, tier="Documented",
         position_confidence="turetilmis: nokta ANA KUBBE MERKEZIDIR "
                             "(prefabin pivotu da orasi); onceki elle "
                             "girilmis deger 17,7 m otedeydi. Yapinin ekseni "
                             "de olculdu — plandan YEDI bagimsiz yolla "
                             "133,6 derece; bu olcum 1632 kiblesinin "
                             "(133,7) cikis noktasidir, ADR 0046.",
         state_1632="Ayakta ve YENI: 1616'da ibadete acildi, yani 1632'de "
                    "ON ALTI yasinda. IV. Murad'in Istanbul'unda sehrin en "
                    "taninan silueti daha bir kusak eskimemistir. "
                    "1632'DE TAMAM: cami (1616), arasta + hamam (1617), "
                    "SULTAN AHMED TURBESI (1619, II. Osman tamamlatti), "
                    "medrese-darussifa-imaret (1620) — kulliye butunuyle "
                    "ayakta. 1632'DE YOK: III. Selim'in su haznesi (1802 "
                    "sonrasi). Bu yapida 'sonradan eklendi' listesi KISADIR.",
         research_ref="RESEARCH.md §5.13, ADR 0047 (TDV)"),

    dict(id="LM_YeniCamiHarabe", name="Yeni Cami harabesi (\"Zulmiyye\")", grade="A",
         lon=28.9722347, lat=41.0168787, tier="Documented",
         position_confidence=SURVEYED
         + " ONCEKI DEGER ~148 m yanlisti (28,9705/41,0166, elle girilmisti)"
           " ve yapiyi YAMACA koyuyordu: kot 12,1 m. Oysa Yeni Cami Halic"
           " kiyisinda, BATAKLIK zemine kuruldu ve ozel temel isi gerekti.",
         state_1632="YARIM/TERK. 1603'te ilk pencere taklarina kadar yukselmis, insaat durmus. "
                    "IV. Murad 1637'de surdurmeyi dusunup vazgecti. Halk 'Zulmiye' dedi; "
                    "cevresi sikisik gayrimuslim mahallesi ve mezbelelik. "
                    "1632'de gorkemli cami DEGIL, denize yakin kagir bir HARABE.",
         research_ref="RESEARCH.md §3 'Yeni Cami (kritik detay)' — TDV, Thys-Senocak"),

    dict(id="LM_FatihCamii", name="Fatih Camii (1766 öncesi özgün şema)", grade="A",
         lon=28.9497, lat=41.0192, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta, OZGUN plan: mihrap yonunde tek yarim kubbeli, disardan Edirne "
                    "Uc Serefeli'ye benzer erken klasik sema. Bugunku barok yapi 1767-71'dir. "
                    "SALT gorselleri CC BY-NC-ND — YALNIZCA BAKILIR, kopyalanmaz.",
         research_ref="RESEARCH.md §3 'Fatih Camii ozgun plani'"),

    dict(id="LM_Beyazit", name="Beyazıt Camii", grade="A",
         lon=28.9647, lat=41.0104, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta (1501-1506).", research_ref="RESEARCH.md §3 (TDV)"),

    dict(id="LM_Sehzade", name="Şehzade Camii", grade="A",
         lon=28.9575, lat=41.0128, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta (1543-1548).", research_ref="RESEARCH.md §3 (TDV)"),

    dict(id="LM_YavuzSelim", name="Yavuz Selim Camii", grade="A",
         lon=28.9513855, lat=41.0265312, tier="Documented", position_confidence=SURVEYED + " ONCEKI DEGER ~150 m yanlisti: cami BESINCI TEPEYI taclandirir ama nokta yerel zirvenin 27,7 m ALTINDA kaliyordu.",
         state_1632="Ayakta (1522). 5. tepe.", research_ref="RESEARCH.md §3 (TDV)"),

    dict(id="LM_RustemPasa", name="Rüstem Paşa Camii", grade="A",
         lon=28.9683, lat=41.0178, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta (1563). Tahtakale.", research_ref="RESEARCH.md §3 (TDV)"),

    dict(id="LM_EskiSaray", name="Eski Saray (Beyazıt)", grade="A",
         lon=28.9640, lat=41.0110, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta, gozden dusmus cariye/valide ikametgahi.",
         research_ref="RESEARCH.md §3 (Muller-Wiener)"),

    dict(id="LM_ArapCamii", name="Arap Camii (Galata)", grade="A",
         lon=28.9714, lat=41.0231, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta.", research_ref="RESEARCH.md §3 Galata (TDV)"),

    dict(id="LM_Tophane", name="Tophane", grade="A",
         lon=28.9822, lat=41.0270, tier="Documented", position_confidence=APPROX,
         state_1632="Ayakta, faal top dokumu.", research_ref="RESEARCH.md §3 Galata (Evliya)"),

    dict(id="LM_Tersane", name="Kasımpaşa Tersanesi", grade="A",
         lon=28.965299, lat=41.030839, tier="Documented", position_confidence="turetilmis: TERSANE bir kiyi tesisidir ve onceki nokta denizden 255 m iceride, 14,3 m yukarida kaliyordu. Nokta KENDI 1632 kiyi cizgimizde olculdu (kot 1,5 m); kayma 247 m. Tersane bir nokta degil bir HAT olduğu icin bu konum onun kiyi ucudur.",
         state_1632="Ayakta, faal.", research_ref="RESEARCH.md §3 Galata (Evliya)"),

    # `on_water`: iskele SUDA OLMALI ve bu bir hata degil TANIMI.
    # Uretici kara landmark'larini kuru zeminde arar ve bu noktayi
    # "arazi -5,4 m" diye reddetti — hakli reddetti; bayrak o denetimi
    # bilerek gecmek icin var (Kiz Kulesi ve Incili Kosk ile ayni).
    dict(id="LM_UskudarIskele", name="Üsküdar İskelesi", grade="S",
         lon=29.0143490, lat=41.0272111, tier="Documented", on_water=True,
         position_confidence="turetilmis: Uskudar Mihrimah'a EN YAKIN kiyi "
                             "noktasi kendi 1632 kiyi cizgimizde bulundu "
                             "(134 m) ve oradan ~20 m denize alindi. "
                             "Iskelenin kendi olculu koordinati yok — ama "
                             "yeri camiden turer, cunku CAMININ ADI "
                             "iskeleden gelir.",
         state_1632="Ayakta ve **AHSAP**. Uskudar Mihrimah Sultan "
                    "Camii'nin yaygin adi **'ISKELE CAMII'**dir ve sebebi "
                    "yani basindaki iskeledir; yani iskele camiden bagimsiz "
                    "bir ayrinti degil, CAMININ ADININ KAYNAGI. Kagir "
                    "rihtimlar 19. yuzyildir.",
         research_ref="RESEARCH.md §5.20, ADR 0055"),

    dict(id="LM_AlayKosku", name="Alay Köşkü", grade="S",
         lon=28.98139, lat=41.01175, tier="Documented",
         position_confidence=APPROX,
         state_1632="Ayakta ve **AHSAP**. Sur-i Sultani uzerinde, sokaga "
                    "tasan seyir kosku; padisah devlet ricalinin ALAYLARINI "
                    "buradan izlerdi. **BUGUNKU KAGIR KOSK 1810 ya da "
                    "1819-20**, II. Mahmud'undur — ve o yapi **DAHA YUKSEK** "
                    "bir koskun/kulenin yerine gecti. Yani 1632 yapisi "
                    "bugunkunden ALCAK DEGIL, YUKSEKTIR: Galata Kulesi ve "
                    "Adalet Kulesi'nin TERSI. 1632'DE YOK: 1855 "
                    "Telgrafhane-i Amire.",
         research_ref="RESEARCH.md §5.20, ADR 0055"),

    # --- Padisah turbeleri (ADR 0054) ---
    #
    # Konumlar OLCULU (harita izlerinin merkezleri). Ucu Ayasofya
    # haziresinde, biri Sultanahmet'te. **1632'de Ayasofya haziresinde
    # DORT turbe vardir, BES degil**: I. Mustafa ve Ibrahim turbesi
    # 1639'dur ve o tarihte vaftizhane hala YAGHANEDIR.
    dict(id="LM_TurbeSelimII", name="II. Selim Türbesi", grade="A",
         lon=28.9797150, lat=41.0079253, tier="Documented",
         position_confidence="olculu: harita izinin merkezi.",
         state_1632="Ayakta, 55 yasinda. **1577, MIMAR SINAN.** Plan KARE, "
                    "disten koseleri PAHLI; ici sekizgen galerili. Sinan "
                    "burada da Kanuni turbesindeki gibi CIFT KABUKLU ortu "
                    "kullandi.",
         research_ref="RESEARCH.md §5.19, ADR 0054"),

    dict(id="LM_TurbeMuradIII", name="III. Murad Türbesi", grade="A",
         lon=28.9794475, lat=41.0079049, tier="Documented",
         position_confidence="olculu: harita izinin merkezi.",
         state_1632="Ayakta, 33 yasinda. **1599, DAVUD AGA** ve yardimcisi "
                    "Dalgic Ahmed Aga. Plan ALTIGEN, cift kubbeli, DISTAN "
                    "MERMER kapli, onunde REVAKLI bolum — Osmanli'nin en "
                    "buyuk turbelerinden. III. Murad 1595'te oldu; turbe "
                    "II. Selim ile Sehzadeler turbeleri arasindadir.",
         research_ref="RESEARCH.md §5.19, ADR 0054"),

    dict(id="LM_TurbeMehmedIII", name="III. Mehmed Türbesi", grade="A",
         lon=28.9799026, lat=41.0077754, tier="Documented",
         position_confidence="olculu: harita izinin merkezi.",
         state_1632="Ayakta, 24 yasinda. **1604-1608**: mimarbasi DALGIC "
                    "AHMED AGA basladi, SEDEFKAR MEHMED AGA (Sultanahmet'in "
                    "mimari) tamamladi. Plan SEKIZGEN, cift kubbeli.",
         research_ref="RESEARCH.md §5.19, ADR 0054"),

    dict(id="LM_TurbeSultanAhmed", name="Sultan Ahmed Türbesi", grade="A",
         lon=28.9769854, lat=41.0067894, tier="Documented",
         position_confidence="olculu: harita izinin merkezi.",
         state_1632="Ayakta, 13 yasinda. **1619**; I. Ahmed 1617'de oldu ve "
                    "turbeyi oglu II. OSMAN tamamlatti. Yatanlarin arasinda "
                    "II. Osman'in kendisi de vardir: 1622'de Yedikule'de "
                    "oldurulup buraya gomuldu — oyunun yilindan ON YIL "
                    "once ve tahttaki IV. Murad onun kardesidir.",
         research_ref="RESEARCH.md §5.19, ADR 0054"),

    dict(id="LM_CevahirBedesteni", name="Cevahir (İç) Bedesteni", grade="A",
         lon=28.968509, lat=41.010665, tier="Documented",
         position_confidence="olculu: haritada 'Ic Bedesten' izinin merkezi.",
         state_1632="Ayakta. Fatih vakfi (~1461). **1632'de KAPALICARSI "
                    "BUGUNKU DEGILDIR**: kagir tonozlu sokaklar agi "
                    "sonradir (1701 yangini ve 1894 depremi onarimlari); "
                    "17. yuzyilda bedestenlerin arasi AHSAP ortuluydu. "
                    "Ustelik 1618 YANGINI 1632'den yalnizca 14 yil once — "
                    "carsi o yil YAKIN ZAMANDA YENIDEN KURULMUS bir yer.",
         research_ref="RESEARCH.md §5.18, ADR 0053"),

    dict(id="LM_SandalBedesteni", name="Sandal Bedesteni", grade="A",
         lon=28.969606, lat=41.010387, tier="Documented",
         position_confidence="olculu: haritada 'Sandal Bedesten' izinin "
                             "merkezi. Cevahir'in ~95 m dogusunda.",
         state_1632="Ayakta. Fatih donemine tarihlenir (Edirne ve Bursa "
                    "bedestenlerine benzerliginden).",
         research_ref="RESEARCH.md §5.18, ADR 0053"),

    dict(id="LM_Yedikule", name="Yedikule", grade="A",
         lon=28.9237576, lat=40.9935646, tier="Documented", position_confidence=SURVEYED + " ONCEKI DEGER ~160 m yanlisti; hisar denizden 520 m iceride kaliyordu.",
         state_1632="Ayakta.", research_ref="RESEARCH.md §3"),
]

# 1632'de HENUZ OLMAYAN yapilar — sahneye GIRMEZ, ama listede tutulur ki
# yanlislikla eklenmesinler. Plan Bolum 2'nin durustluk ilkesi bunu gerektirir.
ABSENT_1632 = [
    dict(id="LM_Nuruosmaniye", name="Nuruosmaniye Camii", built="1748-1755",
         research_ref="RESEARCH.md §3 — HENUZ YOK"),
    dict(id="LM_RevanKosku", name="Revan Köşkü", built="1636",
         research_ref="RESEARCH.md §3 — Revan seferi (1635) donusunde, 1636 tamam"),
    dict(id="LM_BagdatKosku", name="Bağdat Köşkü", built="1639",
         research_ref="RESEARCH.md §3 — '1638'den az sonra, insasi 1639 icinde' (Naima)"),
    dict(id="LM_BuyukValideHan", name="Büyük Valide Han", built="tartışmalı (1640-1651?)",
         research_ref="RESEARCH.md Caveats — 'ihtiyatla muhtemelen yok'"),
]

# Efsanevi ucus: RESEARCH.md Caveats'a gore rakamlar CELISKILIDIR ve
# Evliya'nin metninde metrik olarak GECMEZ. Tek deger olarak sunulamaz.
LEGEND_FIGURES = [
    dict(distance_m=3358, drop_m=62),
    dict(distance_m=3400, drop_m=None),
    dict(distance_m=3558, drop_m=86),
]


def log(msg):
    # Konsol cp1252 olabilir ve mesajlarda Turkce harfler var. Ilk yazimda
    # DENIZDE hatasi tam bu yuzden coktu: gercek hata gorunmedi, yerine bir
    # UnicodeEncodeError okundu. Kodlanamayan karakter yerine "?" konur.
    try:
        print(f"[HZ] {msg}", flush=True)
    except UnicodeEncodeError:
        enc = sys.stdout.encoding or "ascii"
        print(f"[HZ] {msg}".encode(enc, "replace").decode(enc), flush=True)


def sample_elevation(meta, heights, lon, lat):
    e, n = warp_transform("EPSG:4326", meta["crs"], [lon], [lat])
    # geodesy.utm_to_grid TEK nokta alir (e, n, meta); coastline_build'inki
    # liste aliyordu. Imza farki sessiz degil, TypeError verdi.
    x, y = utm_to_grid(e[0], n[0], meta)
    nres = meta["resolution"]
    if not (0 <= x <= nres - 1 and 0 <= y <= nres - 1):
        return None, (e[0], n[0])
    x0, y0 = int(x), int(y)
    x1, y1 = min(x0 + 1, nres - 1), min(y0 + 1, nres - 1)
    tx, ty = x - x0, y - y0
    v = (heights[y0, x0] * (1 - tx) * (1 - ty) + heights[y0, x1] * tx * (1 - ty) +
         heights[y1, x0] * (1 - tx) * ty + heights[y1, x1] * tx * ty)
    return float(v), (e[0], n[0])


def main():
    p = argparse.ArgumentParser(description="1632 landmark katalogu")
    p.add_argument("--dir", default="data/gis/istanbul")
    p.add_argument("--geojson", default="refs/maps/landmarks_1632.geojson")
    args = p.parse_args()

    meta, heights = load_dem(args.dir)
    gx = meta["world_origin"]["utm_easting"]
    gz = meta["world_origin"]["utm_northing"]

    features = []
    local = {"world_origin": meta["world_origin"], "features": []}
    anchor = {}

    for lm in LANDMARKS:
        elev, (e, n) = sample_elevation(meta, heights, lm["lon"], lm["lat"])
        if elev is None:
            log(f"WARN {lm['id']} dunya disinda, atlandi")
            continue

        x, z = e - gx, n - gz
        anchor[lm["id"]] = (x, z, elev)

        props = {
            "layer": "landmark",
            "id": lm["id"], "name": lm["name"], "grade": lm["grade"],
            "tier": lm["tier"],
            "position_confidence": lm["position_confidence"],
            "state_1632": lm["state_1632"],
            "research_ref": lm["research_ref"],
            "terrain_elevation_m": round(elev, 1),
            "unity_x": round(x, 1), "unity_z": round(z, 1),
        }
        features.append({
            "type": "Feature",
            "geometry": {"type": "Point",
                         "coordinates": [round(lm["lon"], 6), round(lm["lat"], 6)]},
            "properties": props,
        })
        local["features"].append({
            "layer": "landmark", "id": lm["id"], "name": lm["name"],
            "tier": lm["tier"], "action": lm["grade"],
            "note": f"{lm['state_1632']} [{lm['research_ref']}]",
            "closed": False,
            "rings": [[{"x": round(x, 2), "z": round(z, 2)}]],
        })

    log(f"{len(features)} landmark")

    # --- Denetim: bir YAPI denizde duramaz ---
    # Konumlar elle girildi ve ~100 m yaklasik. Bir koordinat suya dustugunde
    # sahnede sessizce "denizin ortasinda cami" olarak durur; sayilara bakarak
    # fark edilmez. Bu denetim onu uretim aninda yakalar.
    drowned = [lm for lm in LANDMARKS
               if not lm.get("on_water")
               and lm["id"] in anchor and anchor[lm["id"]][2] <= 1.0]
    if drowned:
        for lm in drowned:
            log(f"HATA {lm['id']} ({lm['name']}) DENIZDE — arazi "
                f"{anchor[lm['id']][2]:.1f} m. Koordinati duzelt ya da on_water=True ver.")
        raise SystemExit(f"[HZ] {len(drowned)} landmark suda. Katalog yazilmadi.")
    log("denetim OK: kara landmark'larinin hepsi kuru zeminde")

    # --- Efsanevi ucus: GERCEK cografya ile CELISKILI rakamlari karsilastir ---
    gxz = anchor.get("LM_GalataKulesi")
    dxz = anchor.get("LM_Dogancilar")
    flight = None
    if gxz and dxz:
        real_d = math.dist((gxz[0], gxz[1]), (dxz[0], dxz[1]))
        log(f"Galata -> Dogancilar (bu koordinatlarla): {real_d:.0f} m, "
            f"arazi kotu {gxz[2]:.1f} -> {dxz[2]:.1f} m")
        for f in LEGEND_FIGURES:
            log(f"  modern yorum: {f['distance_m']} m"
                + (f" / kot {f['drop_m']} m" if f["drop_m"] else ""))
        flight = {
            "measured_distance_m": round(real_d, 1),
            "measured_terrain_galata_m": round(gxz[2], 1),
            "measured_terrain_dogancilar_m": round(dxz[2], 1),
            "legend_figures": LEGEND_FIGURES,
            "warning": ("Bu rakamlar Evliya Celebi'nin metninde METRIK OLARAK GECMEZ; "
                        "modern ikincil kaynaklarin yorumudur ve birbiriyle celisir "
                        "(RESEARCH.md Caveats). Oyunda TEK kesin deger sunulmamalidir."),
        }

    collection = {
        "type": "FeatureCollection",
        "name": "landmarks_1632",
        "metadata": {
            "title": "1632 İstanbul landmark kataloğu",
            "status": "TASLAK — konumlar yaklaşık (Faz 1 madde 3'te georeferanslanacak)",
            "two_axes": ("'tier' 1632'deki VARLIĞI/DURUMU niteler (RESEARCH.md §3); "
                         "'position_confidence' KONUM kesinliğini. İkisi ayrıdır: bir yapının "
                         "ayakta olduğu belgeli olabilirken koordinatı yaklaşık olabilir."),
            "world_origin": meta["world_origin"],
            "legendary_flight": flight,
            "absent_in_1632": ABSENT_1632,
            "copyright": "Bu derleme bize aittir; konumlar RESEARCH.md ve genel coğrafyadan.",
        },
        "features": features,
    }

    geo_path = os.path.abspath(args.geojson)
    os.makedirs(os.path.dirname(geo_path), exist_ok=True)
    with open(geo_path, "w", encoding="utf-8") as fh:
        json.dump(collection, fh, indent=1, ensure_ascii=False)
    log(f"wrote {args.geojson} ({os.path.getsize(geo_path)//1024} KB)")

    local_path = os.path.join(os.path.abspath(args.dir), "landmarks_1632_local.json")
    with open(local_path, "w", encoding="utf-8") as fh:
        json.dump(local, fh, ensure_ascii=False)
    log(f"wrote landmarks_1632_local.json")
    log("landmarks_build OK")


if __name__ == "__main__":
    main()
