"""
Hezarfen: 1632 — Modüler Osmanlı konut kiti (plan Faz 2).

`gen_box_house.py` boru hattının çalıştığını kanıtlıyordu; bu modül **şehri kuran**
üreticidir. Plan Faz 2'nin parametre listesini karşılar: kat sayısı, cephe genişliği,
**cumba tipi ve derinliği**, **kafes pencere yoğunluğu**, saçak derinliği,
**çatı eğimi**, taş subasman yüksekliği, renk paleti (gayrimüslim mahalle varyantı
dahil).

## Tarihsel katman

Konut dokusu **T2**'dir: 1632 İstanbul'unda ev-ev kayıt yoktur (RESEARCH.md
"Mahalleler"). Kurallar RESEARCH.md §"Sokak dokusu" ve plan Bölüm 2'nin T2
tanımından gelir: ahşap karkas üst kat, taş subasman, cumba/çıkma, kafes pencere,
geniş saçak, alaturka kiremit, aşı boyası tonları.

**Gayrimüslim mahalle varyantı** (RESEARCH.md, plan Bölüm 7): daha alçak ve daha
koyu. Bu kural dönemin boy/renk sınırlamalarına dayanır; kaynak niteliksel olduğu
için oranlar T2'dir ve `palette="nonmuslim"` ile açıkça işaretlenir — sessiz bir
varsayım olarak gömülmez.

## Neden pencereler çoğunlukla GEOMETRİ DEĞİL

Plan doku stratejisini "2–3 trim sheet + 1 atlas" diye kilitliyor. 8 000 ev
ölçeğinde her pencereyi modellemek üçgen bütçesini yer. Bu yüzden pencere
**kademelidir** (`window_detail`):

  none    — düz cephe; en uzak LOD ve arka sokaklar
  recess  — girintili niş + söve (VARSAYILAN): oran okunur, ~12 üçgen
  kafes   — niş + söve + kafes çıtaları; yalnızca kamera yakınına giren evler

Böylece aynı jeneratör hem kalabalık dokuyu hem yakın plan evini üretir; seçim
üretim anında yapılır, sonradan mesh temizliğiyle değil.

## Eksen sözleşmesi

Genişlik +X, derinlik +Y, yükseklik +Z. **Sokak cephesi −Y**; cumba oraya taşar.
Orijin taban merkezindedir, böylece Unity'de zemine oturtmak ofset istemez
(ADR 0005).
"""

import math
import os

import bmesh
import bpy

from mathutils import Vector

import hz_blender as hz
import mobilya_kit as mob
import materials as mtl

# Palet anahtari -> Poly Haven varlik kimligi (ve varsa boya tonu).
#
# `tint` BOYALI yuzeyler icindir: asi boyasi ahsabin uzerine surulur, ahsabin
# yerine gecmez. Bu yuzden karisim COLOR kipinde yapilir (bkz. materials.py) —
# damar deseni kalir, rengi boya belirler.
# Asi boyasi (kirmizi asi topragi). Blender DOGRUSAL uzayda calisir; degerler
# sRGB (200,105,80) ve (120,66,52) karsiliklaridir. Boya rengi elle "koyu
# kirmizi" diye secilmedi — render'da olculup hedefe (parlaklik ~100/255,
# R/G ~1.9) gore ayarlandi.
ASI_RED = (0.578, 0.144, 0.085)
# Oyun karesinde aşı boyalı cephe **0,917/0,345/0,201** (R/G 2,66,
# doygunluk 0,78) okuyor ve "fazla kırmızı" görünüyor. Palet SUÇLU
# DEĞİL: pişmiş dokunun kendisi ışıksız ölçüldüğünde 144/56/42, yani
# **R/G 2,57** — demir oksit (aşı) pigmentinin bilinen yeri. Karedeki
# doygunluğu yapan şey albedo değil, güneşli yüzeyde kırmızının 0,92'ye
# dayanması: pozlama ve tonemap.
#
# Bu yüzden ton BURADAN düzeltilmiyor. Bir karede görülen renk kusuru
# önce dokunun kendisinde aranır (çatı kiremidinde tam bunun için
# ölçüldü, birkaç satır aşağıda); doku hedefindeyse kusur ışıktadır ve
# paleti oynatmak resmi düzeltirken malzemeyi bozar. Işık oturunca
# yeniden ölçülecek.
ASI_DARK = (0.181, 0.055, 0.035)        # gayrimuslim varyanti: daha koyu

#: **Boyasız** ahşap. Aşı kırmızısı bir EV boyasıdır (ADR 0030 §5c); nöbet
#: kulesi, iskele, değirmen gibi yapısal ahşap boyanmaz. Tuzlu havada yıllarca
#: duran kereste kırmızıya değil **gri-kahveye** döner — bu yüzden renk aşı
#: ailesinin dışındadır ve doygunluğu düşüktür.
WEATHERED_TIMBER = (0.285, 0.262, 0.228)

#: **Ham deri** — kayış, kuşak, ayakkabı. Faz 5'te hem kanat aygıtının
#: bağlarında hem kıyafette gerekiyor; iki yerde iki ayrı kahverengi
#: tutmak yerine tek rol.
LEATHER = (0.226, 0.132, 0.074)

#: --- KARAKTER RENKLERI (Faz 5) ------------------------------------------
#: Rålamb plakalarindan OKUNDU, uydurulmadi — hangi plakadan geldigi
#: yaninda yazili. Kumas rolleri bilerek TEXTURE_ROLES'a girmiyor: bu
#: rollerin dokusu yok ve `build_materials` dokusuz rolu duz renge dusurur.
#: Tahta dokusunu kumasa giydirmek yanlis olurdu.
SKIN = (0.560, 0.395, 0.305)
CLOTH_ENTARI = (0.105, 0.255, 0.295)   # plaka 20 ve 35: mavi-yesil entari
CLOTH_SALVAR = (0.480, 0.105, 0.085)   # plaka 35: kirmizi caksir
CLOTH_GOMLEK = (0.790, 0.760, 0.680)   # keten ic gomlek
CLOTH_KUSAK = (0.450, 0.085, 0.150)    # plaka 35: dar kirmizi kusak
CLOTH_SARIK = (0.845, 0.830, 0.780)    # plaka 35 ve 50: beyaz sarik
CLOTH_KAVUK = (0.360, 0.075, 0.090)    # plaka 35/50: sarigin altindaki kirmizi tepe
MEST = (0.560, 0.395, 0.115)           # plaka 35 ve 50: sari mest

#: --- SEHIR SAKINLERI (kadin, cocuk, yasli) ------------------------------
#: Kaynak ayni albumun kadin plakalari. Ferace koyu ve sade bir DIS
#: giysidir: sokakta dikkat cekmemek icindir, bu yuzden entarinin
#: mavi-yesilinden daha kisik. Yasmak keten gomlekle ayni beyaz DEGIL —
#: yuzu ortenin daha soguk ve daha acik olmasi gerekiyor ki siluette
#: bas ile govde ayrilsin (ilk turda ikisi ayni renkti ve kadinin basi
#: omzunun icinde kayboluyordu).
CLOTH_FERACE = (0.148, 0.118, 0.185)   # koyu mor-lacivert dis giysi
CLOTH_YASMAK = (0.855, 0.865, 0.870)   # ince beyaz ortu, hafif soguk
CLOTH_TAKKE = (0.315, 0.130, 0.105)    # cocugun kirmizi keceden takkesi

#: --- GOZ ----------------------------------------------------------------
#: Goz kuresi MakeHuman taban mesh'inin kendi geometrisi (`helper-l-eye`);
#: renkler bizim. Ak SAF BEYAZ DEGIL — beyaz bir goz akı plastik okur ve
#: hicbir insanda yoktur: damarli, hafif sicak, gri-krem. Iris kahve,
#: cunku bolgede en yaygin olan o; bebek tam siyah degil cunku tam siyah
#: bir yuzey isik almaz ve gozun icindeki derinlik kaybolur.
EYE_AK = (0.640, 0.612, 0.585)
EYE_IRIS = (0.118, 0.072, 0.042)
EYE_BEBEK = (0.020, 0.018, 0.017)

#: **Kartal tüyü** — kanat yüzeyi. Koyu kahve gövde, uçlarda soluk.
#: Tek bir renkle verilir; kanadın alacalığı GEOMETRIDEN gelir
#: (üst üste binen tüy dizileri), dokudan değil.
FEATHER = (0.238, 0.183, 0.128)

# Kiremit tonu. Kesme tas gibi, KIREMIT DE MAHALLEYE GORE DEGISMEZ: aynı
# ocaktan, aynı fırından çıkar. Boya kısıtı zimmînin DUVARINA konur, çatısına
# değil. Ama iki paletin çatı dokuları farklı varlıklardı ve ölçüm ikisinin
# ayrı renk sınıfına düştüğünü gösterdi: varsayılan R/G 1,82 doygunluk 0,69'a
# karşı gayrimüslim R/G 1,24 doygunluk 0,41 — yani hakiye/haki bir çatı.
# Balat'ın bütün damlarını farklı renk yapmak uydurma bir ayrımdır. Doku
# DESENI farklı kalır (çeşitlilik iyidir), rengi COLOR karışımıyla kil ailesine
# çekilir.
#
# `tint_factor` ÖLÇÜLEREK seçildi, hesapla değil. İlk değer 0,85'ti ve Blender
# render'ında makul görünüyordu (R/G 1,65) — ama Unity'ye PİŞİRİLEN doku
# R/G 2,78, doygunluk 0,84 çıktı ve Balat kan kırmızısı oldu. Gölgelendirici
# zinciriyle pişirme arasındaki fark, aydınlatmalı bir render üstünden
# görülemiyordu. Ölçü artık dokunun KENDİSİ (ışıksız):
#
#   hedef  (T_ClayRoofTiles02_BC) : R/G 1,82  doygunluk 0,70  parlaklik 90,5
#   0,85   -> R/G 2,78  doygunluk 0,84  parlaklik 70,4   (fazla kirmizi)
#   0,35   -> R/G 1,57  doygunluk 0,59  parlaklik 101,9  (hakiye geri dondu)
#   0,50   -> R/G 1,81  doygunluk 0,66  parlaklik 92,4   ✓
ROOF_CLAY = (0.305, 0.084, 0.026)

#: <b>Karakter rolleri her iki cemaat icin AYNI.</b>
#:
#: Once yalnizca `default` paletinde yaziliydi ve `selftest.py` bunu
#: yakaladi: `M_Skin` iki farkli tanim gosteriyor — `default.skin`
#: dokulu, `nonmuslim.skin` dokusuz. Ayni malzeme adinin iki tanimi
#: olmasi Blender tarafinda sessizce `.001` uretir; hata ancak Unity
#: malzemesi yazilirken cikar.
#:
#: Kural zaten dogruydu: bir Rum'un sakali da sakaldir, teni de
#: tendir. Cemaate gore degisen sey EVDIR — badana, ahsap boyasi,
#: kiremit.
KARAKTER_ROLLERI = {
    # --- TEN ---------------------------------------------------------
    #
    # `M_Skin` de dokusuzdu ve dokusuz ten HDRP'de MUM gibi okur.
    # Doku MPFB2'nin kendi bolge maskelerinden bestelendi
    # (`gen_deri_texture.py`): dudak, goz kapagi, kulak ve gozenek.
    # `tinted` burada iki kat onemli — ten rengi kisiden kisiye
    # degismek ZORUNDA, yoksa sehirdeki herkes ayni tende olur.
    #
    # UV: govde MakeHuman taban mesh'inden geliyor ve onun kendi
    # yerlesimini tasiyor; bu yuzden `apply_uvs` govdeye UYGULANMAZ
    # (dunya yansitmasi bu yerlesimi ezer ve dudak alna duserdi).
    "skin":   dict(asset="deri_insan", tinted=True,
                   root=os.path.join("art", "textures", "generated")),

    "gomlek": dict(asset="kumas_keten", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "sarik":  dict(asset="kumas_keten", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "yasmak": dict(asset="kumas_keten", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "entari": dict(asset="kumas_cuha", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "ferace": dict(asset="kumas_cuha", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "salvar": dict(asset="kumas_cuha", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "kusak":  dict(asset="kumas_ipek", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "kavuk":  dict(asset="kumas_kece", tinted=True,
                   root=os.path.join("art", "textures", "generated")),
    "takke":  dict(asset="kumas_kece", tinted=True,
                   root=os.path.join("art", "textures", "generated")),

    # SAKAL: DOKUSUZ ALBEDO PLASTIK OKUR — KUMASTA OGRENILDI.
    #
    # `M_Beard`in taban rengi haritasi YOKTU (`_BaseColorMap`
    # fileID 0) ve yakin plan karesinde sakal ceneye gecirilmis
    # kahverengi bir MASKE gibi duruyordu: tek parca, tek renk,
    # hic kirilma yok. Kart atlasi (`gen_hair_texture.py`) bu isi
    # goremez — o bir ALFA atlasi ve dosenmez; sakal ise kart
    # degil KABUK (bkz. gen_hezarfen.py "SAKAL: KART DEGIL
    # KABUK") ve kabuk dosenebilir bir YUZEY ister.
    #
    # `tinted`: renk paletten gelir. Ayni doku hem kestane sakali
    # (beard) hem ak sakali (beard_ak) tasiyor; albedo notr
    # oldugu icin ayni yuzey iki yasi da anlatabiliyor.
    "beard":    dict(asset="sakal", tinted=True,
                     root=os.path.join("art", "textures", "generated")),
    "beard_ak": dict(asset="sakal", tinted=True,
                     root=os.path.join("art", "textures", "generated")),
    "sac":      dict(asset="sac_yuzey", tinted=True,
                     root=os.path.join("art", "textures", "generated")),

    # MEST: HER SAKININ AYAGINDA VE DOKUSUZDU.
    #
    # `M_Leather_Mest`in taban rengi haritasi yoktu. Sari mest
    # sehirde en cok tekrar eden kucuk yuzeylerden biri — altmis
    # govdede yuz yirmi tane — ve dokusuz oldugu icin plastik
    # okuyordu. Kosele ile ayni doku, farkli renk.
    "mest":     dict(asset="kosele", tinted=True,
                     root=os.path.join("art", "textures", "generated")),

    # KILIM: IC MEKANIN DOSEMESI VE SON DOKUSUZ KUMAS.
    #
    # Doku ATKI YUZLU duz dokuma — kilimi kilim yapan yuzey budur:
    # dugum yok, atki cozguyu tamamen orter, yuzey enine kaburgali
    # okunur. MOTIF YOK ve bu bilincli: bir kilimin motifi kaynak
    # ister; uydurmak, kilimi kilim yapan seyi uydurmak olurdu
    # (CLAUDE.md: kaynak niteliksel oldugunda metrik geometri
    # uydurma). Motif belgeyle birlikte gelir.
    "cloth":    dict(asset="kumas_kilim", tinted=True,
                     root=os.path.join("art", "textures", "generated")),

}

TEXTURE_ROLES = {
    "default": {
        "stone":   dict(asset="old_stone_wall"),
        # Kesme tas: moloz TASIYICI duvar icindir, kesme tas ISLENMIS yuzey.
        # Cesmenin ayna tasi ve kitabesi oyma tastir; moloz doku oraya konunca
        # yapi "duvar parcasi" gibi okunur, eser gibi degil.
        "cutstone": dict(asset="large_sandstone_blocks"),
        "plaster": dict(asset="painted_plaster_wall"),
        # Boyali ahsap: MIX kipi (boya orter), hafif gamma ile alttaki damar
        # tamamen kararmasin. Olcum: COLOR ile 40/255, MIX ile hedef ~100.
        "timber":  dict(asset="weathered_planks", tint=ASI_RED, tint_factor=0.78,
                        value_gamma=0.55, tint_blend="MIX"),
        "roof":    dict(asset="clay_roof_tiles_02"),
        "trim":    dict(asset="weathered_planks", tint=ASI_DARK, tint_factor=0.70,
                        value_gamma=0.75, tint_blend="MIX"),
        # BOYASIZ ahsap: tint yok denecek kadar az ve COLOR kipinde —
        # damarin kendi parlakligi kalir, yalnizca kirmizi tonu cekilir.
        # value_gamma OLCULEREK secildi: COLOR kipi tonu cikarirken degeri de
        # goturdu (kayaya oran 0,43 -> 0,30, yani govde neredeyse siyah).
        # Boyasiz kereste tastan koyudur ama dort kat koyu degil; gamma
        # degeri geri kaldirir, rengi bozmaz.
        "timber_bare": dict(asset="weathered_planks", tint=WEATHERED_TIMBER,
                            tint_factor=0.45, tint_blend="COLOR",
                            value_gamma=0.66),
        # Kaldirim, kabuk ve yaprak MAHALLEYE GORE DEGISMEZ — kesme tasla
        # ayni ilke. Sokak tasi da agac da cemaate ait degildir.
        "paving": dict(asset="cobblestone_floor_001"),
        "bark":   dict(asset="bark_brown_01"),
        "bark_cinar": dict(asset="bark_platanus"),
        # DERI ve TUY: Faz 5 (kanat aygiti + kiyafet) icin. Ikisi de
        # cemaate gore degismez — deri deridir. Doku olarak asinmis
        # kereste kullanilir cunku elde CC0 deri/tuy dokusu yok; rengi
        # tint tasir, yuzey dokusunu lif yonu verir.
        # DERI ARTIK DERIDIR.
        #
        # Bu satir kereste dokusu kullaniyordu ve gerekcesi de
        # yazilmisti: "elde CC0 deri/tuy dokusu yok". Dogruydu — ta
        # ki uretilene kadar. `gen_kosele_texture.py` kosele tanesini
        # ve kirik agini uretiyor; artik bir kayisa tahta damari
        # giydirmek icin sebep yok.
        "leather": dict(asset="kosele", tinted=True,
                        root=os.path.join("art", "textures", "generated")),
        # KANAT YUZEYI KERESTE DEGIL, TUY.
        #
        # Bu satir da `weathered_planks` kullaniyordu ve gerekcesi
        # derininkiyle ayniydi: "elde CC0 tuy dokusu yok". Dogruydu, ta
        # ki uretilene kadar. Inceleme karesinde 9,71 m'lik kanat tahta
        # kaplama bir guverte gibi okunuyordu; oysa varligin kendi
        # kaynak notu yuzeyi zaten yaziyor — "ahsap cita iskelet +
        # KARTAL TUYU YUZEY + deri kayis" (`gen_kanat.py`, SOURCE).
        # `cift_tarafli`: kanat zari TEK YUZLU bir yamuktur.
        #
        # Oyun turunda olculdu: karakter arkadan gorunurken sirtindaki
        # kanatlardan yalniz citalar ciziliyor, zar yok — "sirtinda
        # merdiven tasiyan adam". Sebep eksik mesh degil, ARKA YUZ
        # ELEMESI: `M_Feather` `_CullMode: 2` ile geliyordu, zar da tek
        # yuzlu. Yani yuzey diskte de, sahnede de vardi; yalnizca alt
        # tarafindan bakilinca cizilmiyordu.
        #
        # Kalinlik vermek yerine iki yuzlu isaretleniyor: gercek bir
        # kanat bezi de iki yuzlu ince bir yuzeydir, ve ikinci bir kabuk
        # ucgeni ikiye katlayip z-cakismasi riski getirirdi.
        "feather": dict(asset="tuy", tinted=True, cift_tarafli=True,
                        root=os.path.join("art", "textures", "generated")),
        "foliage_servi": dict(asset="foliage_servi",
                              root=os.path.join("art", "textures", "generated")),
        "foliage_cinar": dict(asset="foliage_cinar",
                              root=os.path.join("art", "textures", "generated")),
        # Kursun: kubbenin ve kulahin ortusu — ucus oyununda EN COK bakilan
        # yuzey. Prosedurel (Poly Haven'da kursun ortu yok); metaliklik ARM'in
        # B kanalindan piksel piksel okunur, cunku oksit ortusu duzeltilmis bir
        # sayi degil bir DESENDIR. Gerekce: tools/textures/gen_lead_texture.py.
        "lead":   dict(asset="lead_sheet", metallic="arm",
                       root=os.path.join("art", "textures", "generated")),
        # Mermer: menzil tasi, mezar tasi, mihrap tasi. Kesme tas DUVAR
        # malzemesidir; bir sutuna sarildiginda derzleri "tek parca mermer
        # sutun" iddiasini yalanliyordu (olculen dikey periyot 0,95 m) ve
        # tasi cayirdan 4,4 kat koyu birakiyordu. Prosedurel — Poly Haven'da
        # lisansli mermer yok. Gerekce: tools/textures/gen_marble_texture.py.
        "marble": dict(asset="marble_white",
                       root=os.path.join("art", "textures", "generated")),
        # Tugla: almasik orgunun kusaklari — Galata Kulesi'nin govdesinde
        # 13,20 ve 17,17 m'de, ve ilki 1509 onariminin DIKISI. Kusaklar
        # `cutstone` ile uretilmisti ve render'da tugla degil "govdeye
        # dolanmis ince bir golge cizgisi" olarak okunuyordu; kusagin anlami
        # RENGINDEDIR. Prosedurel — Poly Haven'dan tugla indirilmedi.
        # Gerekce: tools/textures/gen_brick_texture.py.
        "brick":  dict(asset="brick_band",
                       root=os.path.join("art", "textures", "generated")),
        # --- KUMAS ROLLERI ---------------------------------------------
        #
        # Bu blogun ustunde yillarca "kumas rolleri bilerek
        # TEXTURE_ROLES'a girmiyor: bu rollerin dokusu yok" yaziyordu ve
        # o gun dogruydu. Bugun degil: `gen_kumas_texture.py` dort
        # dokumayi uretiyor ve hepsi bizim eserimiz.
        #
        # Anahtarlar PALETIN anahtarlariyla ayni olmak zorunda
        # (`build_unity_maps` `roles.get(key)` diyor); bu yuzden rol adi
        # kumasin adi degil GIYSININ adidir.
        #
        # `tinted=True` YENI ve tam da asagidaki kitabe notunun tarif
        # ettigi kusuru cozuyor: "dokulu bir malzemede taban rengi
        # dokudan gelir — paletteki albedo tasinmaz." Kumasta bu kabul
        # edilemez, cunku rengin kaynagi Ralamb plakalari ve kisiden
        # kisiye ton `_BaseColor` ile CARPILIYOR. Kumas dokularinin
        # albedosu bu yuzden NOTR uretildi; isaretli roller paletteki
        # rengi `_BaseColor` olarak tasimaya devam ediyor.
        #
        # Hangi giysi hangi kumastan — kaynak degil MALZEME KURALI:
        #   keten: ten'e degen ve agartilan her sey (gomlek, sarik, yasmak)
        #   cuha : dinklenmis yun dis giysi (entari, ferace, salvar)
        #   ipek : kusak — tek parlak parca, ve o parlaklik rutbe degil
        #          dokuma (atlas atlamalari isigi tek yonde toplar)
        #   kece : dovulmus baslik cekirdegi (kavuk, takke)
        **KARAKTER_ROLLERI,
        # Kitabe'nin BURADA doku rolu YOK ve bu bilincli. Once mermerle ayni
        # dokuyu veriyordu; sonuc Unity'de iki AYNI malzemeydi, cunku dokulu
        # bir malzemede taban rengi dokudan gelir — paletteki albedo tasinmaz.
        # Kitabe sahnede hic gorunmuyordu. Duz renk, oyulmus harflerin
        # golgesini tasiyan bir alan olarak dogru okunur.
    },
    "nonmuslim": {
        "stone":   dict(asset="old_stone_wall"),
        "cutstone": dict(asset="large_sandstone_blocks"),
        "plaster": dict(asset="grey_plaster"),
        "timber":  dict(asset="weathered_planks", tint=ASI_DARK, tint_factor=0.80,
                        value_gamma=0.70),
        "roof":    dict(asset="ceramic_roof_01", tint=ROOF_CLAY,
                        tint_factor=0.50, tint_blend="COLOR"),
        "trim":    dict(asset="weathered_planks", tint=ASI_DARK, tint_factor=0.85,
                        value_gamma=0.85),
        # Boyasiz ahsap HER IKI PALETTE AYNI (kesme tas / mermer / tugla
        # ile ayni ilke): boyanmamis kereste cemaate gore degismez.
        # value_gamma OLCULEREK secildi: COLOR kipi tonu cikarirken degeri de
        # goturdu (kayaya oran 0,43 -> 0,30, yani govde neredeyse siyah).
        # Boyasiz kereste tastan koyudur ama dort kat koyu degil; gamma
        # degeri geri kaldirir, rengi bozmaz.
        "timber_bare": dict(asset="weathered_planks", tint=WEATHERED_TIMBER,
                            tint_factor=0.45, tint_blend="COLOR",
                            value_gamma=0.66),
        # Kaldirim, kabuk ve yaprak MAHALLEYE GORE DEGISMEZ — kesme tasla
        # ayni ilke. Sokak tasi da agac da cemaate ait degildir.
        "paving": dict(asset="cobblestone_floor_001"),
        "bark":   dict(asset="bark_brown_01"),
        "bark_cinar": dict(asset="bark_platanus"),
        # DERI ve TUY: Faz 5 (kanat aygiti + kiyafet) icin. Ikisi de
        # cemaate gore degismez — deri deridir. Doku olarak asinmis
        # kereste kullanilir cunku elde CC0 deri/tuy dokusu yok; rengi
        # tint tasir, yuzey dokusunu lif yonu verir.
        # DERI ARTIK DERIDIR.
        #
        # Bu satir kereste dokusu kullaniyordu ve gerekcesi de
        # yazilmisti: "elde CC0 deri/tuy dokusu yok". Dogruydu — ta
        # ki uretilene kadar. `gen_kosele_texture.py` kosele tanesini
        # ve kirik agini uretiyor; artik bir kayisa tahta damari
        # giydirmek icin sebep yok.
        "leather": dict(asset="kosele", tinted=True,
                        root=os.path.join("art", "textures", "generated")),
        # KANAT YUZEYI KERESTE DEGIL, TUY.
        #
        # Bu satir da `weathered_planks` kullaniyordu ve gerekcesi
        # derininkiyle ayniydi: "elde CC0 tuy dokusu yok". Dogruydu, ta
        # ki uretilene kadar. Inceleme karesinde 9,71 m'lik kanat tahta
        # kaplama bir guverte gibi okunuyordu; oysa varligin kendi
        # kaynak notu yuzeyi zaten yaziyor — "ahsap cita iskelet +
        # KARTAL TUYU YUZEY + deri kayis" (`gen_kanat.py`, SOURCE).
        # `cift_tarafli`: kanat zari TEK YUZLU bir yamuktur.
        #
        # Oyun turunda olculdu: karakter arkadan gorunurken sirtindaki
        # kanatlardan yalniz citalar ciziliyor, zar yok — "sirtinda
        # merdiven tasiyan adam". Sebep eksik mesh degil, ARKA YUZ
        # ELEMESI: `M_Feather` `_CullMode: 2` ile geliyordu, zar da tek
        # yuzlu. Yani yuzey diskte de, sahnede de vardi; yalnizca alt
        # tarafindan bakilinca cizilmiyordu.
        #
        # Kalinlik vermek yerine iki yuzlu isaretleniyor: gercek bir
        # kanat bezi de iki yuzlu ince bir yuzeydir, ve ikinci bir kabuk
        # ucgeni ikiye katlayip z-cakismasi riski getirirdi.
        "feather": dict(asset="tuy", tinted=True, cift_tarafli=True,
                        root=os.path.join("art", "textures", "generated")),
        "foliage_servi": dict(asset="foliage_servi",
                              root=os.path.join("art", "textures", "generated")),
        "foliage_cinar": dict(asset="foliage_cinar",
                              root=os.path.join("art", "textures", "generated")),
        # Kursun: kubbenin ve kulahin ortusu — ucus oyununda EN COK bakilan
        # yuzey. Prosedurel (Poly Haven'da kursun ortu yok); metaliklik ARM'in
        # B kanalindan piksel piksel okunur, cunku oksit ortusu duzeltilmis bir
        # sayi degil bir DESENDIR. Gerekce: tools/textures/gen_lead_texture.py.
        "lead":   dict(asset="lead_sheet", metallic="arm",
                       root=os.path.join("art", "textures", "generated")),
        # Mermer: menzil tasi, mezar tasi, mihrap tasi. Kesme tas DUVAR
        # malzemesidir; bir sutuna sarildiginda derzleri "tek parca mermer
        # sutun" iddiasini yalanliyordu (olculen dikey periyot 0,95 m) ve
        # tasi cayirdan 4,4 kat koyu birakiyordu. Prosedurel — Poly Haven'da
        # lisansli mermer yok. Gerekce: tools/textures/gen_marble_texture.py.
        "marble": dict(asset="marble_white",
                       root=os.path.join("art", "textures", "generated")),
        # Tugla: almasik orgunun kusaklari — Galata Kulesi'nin govdesinde
        # 13,20 ve 17,17 m'de, ve ilki 1509 onariminin DIKISI. Kusaklar
        # `cutstone` ile uretilmisti ve render'da tugla degil "govdeye
        # dolanmis ince bir golge cizgisi" olarak okunuyordu; kusagin anlami
        # RENGINDEDIR. Prosedurel — Poly Haven'dan tugla indirilmedi.
        # Gerekce: tools/textures/gen_brick_texture.py.
        "brick":  dict(asset="brick_band",
                       root=os.path.join("art", "textures", "generated")),
        # Kitabe'nin BURADA doku rolu YOK ve bu bilincli. Once mermerle ayni
        # dokuyu veriyordu; sonuc Unity'de iki AYNI malzemeydi, cunku dokulu
        # bir malzemede taban rengi dokudan gelir — paletteki albedo tasinmaz.
        # Kitabe sahnede hic gorunmuyordu. Duz renk, oyulmus harflerin
        # golgesini tasiyan bir alan olarak dogru okunur.
        **KARAKTER_ROLLERI,
    },
}

# --------------------------------------------------------------------- palet
#
# Graybox seviyesinde doku yok; renkler yalnizca kutlelerin okunmasi ve inceleme
# paketinde oranlarin ayirt edilmesi icin. Faz 2'nin doku pasi bunlarin yerine
# trim sheet koyacak — isimler o zaman da ayni kalsin diye M_ sozlesmesine uygun.
#
# ADLAR BENZERSIZ OLMAK ZORUNDA: bir malzeme adi HER ZAMAN ayni doku ve ayni
# boya parametrelerini gostermeli. Ilk yazimda uc cakisma vardi — 'M_Timber_Dark'
# hem varsayilan paletin trim'i hem gayrimuslim paletin ahsabi ve trim'iydi
# (uc farkli parametre kumesi), 'M_Roof_Alaturka' ise iki farkli kiremit
# dokusuydu. Blender bunu sessizce '.001' ekleyerek gecistirir; hata ancak
# Unity'ye malzeme yazarken ortaya cikar. selftest.py artik bunu kilitliyor.
PALETTES = {
    # Musluman mahallesi: kirec badana + asi kirmizisi ahsap.
    "default": {
        "stone":   ((0.42, 0.40, 0.37), 0.90, "M_Stone_Rubble"),
        "cutstone": ((0.50, 0.48, 0.44), 0.82, "M_Stone_Cut"),
        "plaster": ((0.86, 0.84, 0.78), 0.85, "M_Plaster_Lime"),
        "timber":  ((0.55, 0.24, 0.18), 0.80, "M_Timber_AsiRed"),
        "roof":    ((0.52, 0.27, 0.19), 0.75, "M_Roof_Alaturka"),
        "shadow":  ((0.08, 0.07, 0.06), 0.95, "M_Opening_Shadow"),
        "trim":    ((0.30, 0.22, 0.16), 0.80, "M_Timber_Trim"),
        "timber_bare": ((0.265, 0.245, 0.212), 0.88, "M_Timber_Weathered"),
        "paving":  ((0.30, 0.29, 0.27), 0.88, "M_Paving_Kaldirim"),
        # IC MEKAN (Faz II.5). Kilim ve minderin rengi dokuma boyasidir:
        # kok boya kirmizisi donemin en yaygin ic tekstil rengi. Mangal
        # ve kaplar bakir.
        "cloth":   ((0.42, 0.15, 0.13), 0.88, "M_Cloth_Kilim"),
        "metal":   ((0.45, 0.26, 0.14), 0.42, "M_Metal_Bakir"),
        "bark":    ((0.115, 0.092, 0.070), 0.92, "M_Bark"),
        "bark_cinar": ((0.175, 0.160, 0.130), 0.88, "M_Bark_Cinar"),
        # Deri ve tuy: Faz 5 (kanat aygiti + kiyafet). Cemaate gore
        # DEGISMEZ — deri deridir. Deri parlak degil ama tastan pürüzsüz;
        # tuy daha da pürüzsüz cunku yagli.
        "leather": (LEATHER, 0.74, "M_Leather"),
        "skin":    (SKIN, 0.58, "M_Skin"),
        # Goz: uc malzeme, cunku uc yuzey. Purzuluk ak ve iriste DUSUK
        # (nemli kure), bebekte anlamsiz ama tutarli olsun diye ayni.
        "goz_ak":    (EYE_AK, 0.22, "M_Eye_Sclera"),
        "goz_iris":  (EYE_IRIS, 0.18, "M_Eye_Iris"),
        "goz_bebek": (EYE_BEBEK, 0.18, "M_Eye_Pupil"),
        # SAKAL PALETE GIRER — CUNKU ARTIK OPAK.
        #
        # Sac palete giremiyor: alfa kesme istiyor ve `hair_material`
        # ayri dugumlerle kuruluyor. Sakal ise kart olmaktan cikip
        # cene bolgesinden kopyalanan bir KABUK oldu, yani opak.
        #
        # Girmemis olmasinin bedeli olculdu: FBX'in icinde `M_Beard`
        # adli bir malzeme var ama `Art/Materials/Ottoman/M_Beard.mat`
        # yoktu; `ModelImportPolicy.OnAssignMaterialModel` ada gore
        # arayip null donuyor ve Unity gomulu varsayilani birakiyordu —
        # maskesiz, normalsiz, sozlesme disi ve her yeniden import'ta
        # sessizce yeniden uretilen bir malzeme.
        #
        # Renk `sac_kit.sakal_material` ile ayni: tam siyah bir sakal
        # isik almadigi icin yuzde delik gibi okunuyordu.
        "beard":   ((0.105, 0.072, 0.052), 0.72, "M_Beard"),
        # Yaslinin AK sakali. Ayni malzemeyi acik renkle kullanmak
        # olmazdi: `hz.assign` malzemeyi ADA gore paylastirir, yani
        # yaslinin sakalini beyazlatmak genc adamin sakalini da
        # beyazlatirdi. Ayri ad = ayri malzeme.
        "beard_ak": ((0.760, 0.745, 0.720), 0.70, "M_Beard_Ak"),
        # SAC KABUGU: KART DEGIL, SAKALLA AYNI KARAR.
        #
        # Sarigin/takkenin altindan cikan sac kart diziliyordu ve
        # oglanin yakin planinda ne oldugu goruldu: kulaklarin iki
        # yaninda TEL, boynun cevresinde FIRFIR. Ayni kusur bu depoda
        # besinci kez; sakalda cozumu zaten bulunmustu.
        "sac":     ((0.106, 0.070, 0.048), 0.55, "M_Hair_Kabuk"),
        "entari":  (CLOTH_ENTARI, 0.80, "M_Cloth_Entari"),
        "salvar":  (CLOTH_SALVAR, 0.82, "M_Cloth_Salvar"),
        "gomlek":  (CLOTH_GOMLEK, 0.86, "M_Cloth_Gomlek"),
        "kusak":   (CLOTH_KUSAK, 0.78, "M_Cloth_Kusak"),
        "sarik":   (CLOTH_SARIK, 0.84, "M_Cloth_Sarik"),
        "kavuk":   (CLOTH_KAVUK, 0.80, "M_Cloth_Kavuk"),
        "ferace":  (CLOTH_FERACE, 0.86, "M_Cloth_Ferace"),
        "yasmak":  (CLOTH_YASMAK, 0.88, "M_Cloth_Yasmak"),
        "takke":   (CLOTH_TAKKE, 0.82, "M_Cloth_Takke"),
        "mest":    (MEST, 0.66, "M_Leather_Mest"),
        "feather": (FEATHER, 0.62, "M_Feather"),
        "foliage_servi": ((0.038, 0.068, 0.034), 0.78, "M_Foliage_Servi"),
        "foliage_cinar": ((0.086, 0.125, 0.048), 0.82, "M_Foliage_Cinar"),
        # Kursunun dokusu artik VAR (prosedurel — ADR 0021); buradaki renk
        # yalnizca graybox yedegi. Palette durmasi sart: `build_unity_maps.py`
        # yalnizca palet + rol tarar; kitin kendi icinde tanimlanan bir malzeme
        # Unity'ye hic ulasmaz (nature_kit'te tam bu olmustu).
        "lead":    ((0.196, 0.203, 0.210), 0.42, "M_Lead_Sheet"),
        # Mermer HER IKI PALETTE AYNI: taş cemaate göre değişmez (kesme taşla
        # aynı ilke). Albedo yüksek — ölçülen kusur taşın koyu olmasıydı.
        "marble":  ((0.700, 0.688, 0.655), 0.45, "M_Marble_White"),
        # Albedo ÖLÇÜLEREK seçildi: 0,262 iken sahnede kitabe 56/255, gövde
        # 143,7/255 çıkıyordu — oran 0,39, yani beyaz mermerde bir DELİK.
        # Oyulmuş harflerin gölgesi taşı karartır, delmez.
        "kitabe":  ((0.400, 0.386, 0.356), 0.62, "M_Marble_Kitabe"),
        # Tuğla da HER İKİ PALETTE AYNI: tuğla cemaate göre değişmez.
        # Renk, üretilen dokunun ölçülen ortalamasıdır (167, 92, 69).
        "brick":   ((0.655, 0.361, 0.271), 0.80, "M_Brick_Band"),
        # Cam dokusuzdur ve OYLE KALIR. Fil gozu 20 cm'lik bir kabarciktir;
        # onu cam yapan sey albedo deseni degil PURUZSUZLUK ve saydamliktir.
        # Buraya doku koymak, olcusu okunmayan bir deseni 20 cm'ye sikistirmak
        # olurdu. "Her malzemenin dokusu olmali" testi bunu bildirimden
        # ogrenir (kind == "untextured"), elle tutulan listeden degil.
        "glass":   ((0.42, 0.50, 0.52), 0.12, "M_Glass_Filgozu"),
    },
    # Gayrimuslim mahalle varyanti (T2): daha KOYU ve daha ALCAK.
    # Alcaklik `HouseParams.apply_palette_rules` icinde kat yuksekligine islenir.
    "nonmuslim": {
        "stone":   ((0.38, 0.36, 0.34), 0.90, "M_Stone_Rubble"),
        # Kesme tas her iki palette AYNI: tasin kendisi mahalleye gore degismez.
        "cutstone": ((0.50, 0.48, 0.44), 0.82, "M_Stone_Cut"),
        "plaster": ((0.58, 0.55, 0.50), 0.88, "M_Plaster_Grey"),
        "timber":  ((0.30, 0.21, 0.16), 0.82, "M_Timber_Dark"),
        "roof":    ((0.42, 0.24, 0.18), 0.78, "M_Roof_Ceramic"),
        "shadow":  ((0.06, 0.05, 0.05), 0.95, "M_Opening_Shadow"),
        "trim":    ((0.22, 0.16, 0.12), 0.82, "M_Timber_TrimDark"),
        "timber_bare": ((0.265, 0.245, 0.212), 0.88, "M_Timber_Weathered"),
        "paving":  ((0.30, 0.29, 0.27), 0.88, "M_Paving_Kaldirim"),
        # IC MEKAN (Faz II.5). Kilim ve minderin rengi dokuma boyasidir:
        # kok boya kirmizisi donemin en yaygin ic tekstil rengi. Mangal
        # ve kaplar bakir.
        "cloth":   ((0.42, 0.15, 0.13), 0.88, "M_Cloth_Kilim"),
        "metal":   ((0.45, 0.26, 0.14), 0.42, "M_Metal_Bakir"),
        "bark":    ((0.115, 0.092, 0.070), 0.92, "M_Bark"),
        "bark_cinar": ((0.175, 0.160, 0.130), 0.88, "M_Bark_Cinar"),
        # Deri ve tuy: Faz 5 (kanat aygiti + kiyafet). Cemaate gore
        # DEGISMEZ — deri deridir. Deri parlak degil ama tastan pürüzsüz;
        # tuy daha da pürüzsüz cunku yagli.
        "leather": (LEATHER, 0.74, "M_Leather"),
        "skin":    (SKIN, 0.58, "M_Skin"),
        # Goz: uc malzeme, cunku uc yuzey. Purzuluk ak ve iriste DUSUK
        # (nemli kure), bebekte anlamsiz ama tutarli olsun diye ayni.
        "goz_ak":    (EYE_AK, 0.22, "M_Eye_Sclera"),
        "goz_iris":  (EYE_IRIS, 0.18, "M_Eye_Iris"),
        "goz_bebek": (EYE_BEBEK, 0.18, "M_Eye_Pupil"),
        # SAKAL PALETE GIRER — CUNKU ARTIK OPAK.
        #
        # Sac palete giremiyor: alfa kesme istiyor ve `hair_material`
        # ayri dugumlerle kuruluyor. Sakal ise kart olmaktan cikip
        # cene bolgesinden kopyalanan bir KABUK oldu, yani opak.
        #
        # Girmemis olmasinin bedeli olculdu: FBX'in icinde `M_Beard`
        # adli bir malzeme var ama `Art/Materials/Ottoman/M_Beard.mat`
        # yoktu; `ModelImportPolicy.OnAssignMaterialModel` ada gore
        # arayip null donuyor ve Unity gomulu varsayilani birakiyordu —
        # maskesiz, normalsiz, sozlesme disi ve her yeniden import'ta
        # sessizce yeniden uretilen bir malzeme.
        #
        # Renk `sac_kit.sakal_material` ile ayni: tam siyah bir sakal
        # isik almadigi icin yuzde delik gibi okunuyordu.
        "beard":   ((0.105, 0.072, 0.052), 0.72, "M_Beard"),
        # Yaslinin AK sakali. Ayni malzemeyi acik renkle kullanmak
        # olmazdi: `hz.assign` malzemeyi ADA gore paylastirir, yani
        # yaslinin sakalini beyazlatmak genc adamin sakalini da
        # beyazlatirdi. Ayri ad = ayri malzeme.
        "beard_ak": ((0.760, 0.745, 0.720), 0.70, "M_Beard_Ak"),
        # SAC KABUGU: KART DEGIL, SAKALLA AYNI KARAR.
        #
        # Sarigin/takkenin altindan cikan sac kart diziliyordu ve
        # oglanin yakin planinda ne oldugu goruldu: kulaklarin iki
        # yaninda TEL, boynun cevresinde FIRFIR. Ayni kusur bu depoda
        # besinci kez; sakalda cozumu zaten bulunmustu.
        "sac":     ((0.106, 0.070, 0.048), 0.55, "M_Hair_Kabuk"),
        "entari":  (CLOTH_ENTARI, 0.80, "M_Cloth_Entari"),
        "salvar":  (CLOTH_SALVAR, 0.82, "M_Cloth_Salvar"),
        "gomlek":  (CLOTH_GOMLEK, 0.86, "M_Cloth_Gomlek"),
        "kusak":   (CLOTH_KUSAK, 0.78, "M_Cloth_Kusak"),
        "sarik":   (CLOTH_SARIK, 0.84, "M_Cloth_Sarik"),
        "kavuk":   (CLOTH_KAVUK, 0.80, "M_Cloth_Kavuk"),
        "ferace":  (CLOTH_FERACE, 0.86, "M_Cloth_Ferace"),
        "yasmak":  (CLOTH_YASMAK, 0.88, "M_Cloth_Yasmak"),
        "takke":   (CLOTH_TAKKE, 0.82, "M_Cloth_Takke"),
        "mest":    (MEST, 0.66, "M_Leather_Mest"),
        "feather": (FEATHER, 0.62, "M_Feather"),
        "foliage_servi": ((0.038, 0.068, 0.034), 0.78, "M_Foliage_Servi"),
        "foliage_cinar": ((0.086, 0.125, 0.048), 0.82, "M_Foliage_Cinar"),
        # Kursunun dokusu artik VAR (prosedurel — ADR 0021); buradaki renk
        # yalnizca graybox yedegi. Palette durmasi sart: `build_unity_maps.py`
        # yalnizca palet + rol tarar; kitin kendi icinde tanimlanan bir malzeme
        # Unity'ye hic ulasmaz (nature_kit'te tam bu olmustu).
        "lead":    ((0.196, 0.203, 0.210), 0.42, "M_Lead_Sheet"),
        # Mermer HER IKI PALETTE AYNI: taş cemaate göre değişmez (kesme taşla
        # aynı ilke). Albedo yüksek — ölçülen kusur taşın koyu olmasıydı.
        "marble":  ((0.700, 0.688, 0.655), 0.45, "M_Marble_White"),
        # Albedo ÖLÇÜLEREK seçildi: 0,262 iken sahnede kitabe 56/255, gövde
        # 143,7/255 çıkıyordu — oran 0,39, yani beyaz mermerde bir DELİK.
        # Oyulmuş harflerin gölgesi taşı karartır, delmez.
        "kitabe":  ((0.400, 0.386, 0.356), 0.62, "M_Marble_Kitabe"),
        # Tuğla da HER İKİ PALETTE AYNI: tuğla cemaate göre değişmez.
        # Renk, üretilen dokunun ölçülen ortalamasıdır (167, 92, 69).
        "brick":   ((0.655, 0.361, 0.271), 0.80, "M_Brick_Band"),
        # Cam dokusuzdur ve OYLE KALIR. Fil gozu 20 cm'lik bir kabarciktir;
        # onu cam yapan sey albedo deseni degil PURUZSUZLUK ve saydamliktir.
        # Buraya doku koymak, olcusu okunmayan bir deseni 20 cm'ye sikistirmak
        # olurdu. "Her malzemenin dokusu olmali" testi bunu bildirimden
        # ogrenir (kind == "untextured"), elle tutulan listeden degil.
        "glass":   ((0.42, 0.50, 0.52), 0.12, "M_Glass_Filgozu"),
    },
}

CUMBA_TYPES = ("none", "flat", "corbel", "corner")
WINDOW_DETAILS = ("none", "recess", "kafes")

# Yapim kademesi. Ikisi ayni parametrelerden ayni evi uretir, farkli DERINLIKTE.
#
#   mass — duvar tek kutle, aciklik cepheye yapistirilmis koyu panel.
#          Kalabalik doku icin: ~900 ucgen, 30 m'den ayirt edilemez.
#   near — duvar DELIKLI orulur; sove derinligi, denizlik, sacak mertekleri,
#          esik, kapi kanadi, mahya. Yaya seviyesi icin: ~3 000 ucgen.
#
# Karar (Caner, 2026-08-20): "karakter sokaklarda da gezecek, atmosfer gercekci
# olmali" -> sokak evleri 'near'. Kalabalik yine 'mass' kalir; LOD1/LOD2
# degismez, yani yakin plan detayi uzak siluete hic bedel odetmez.
DETAIL_LEVELS = ("mass", "near")

# Hangi cephelerde aciklik olur. Sikisik mahallede evler bitisik nizamdir ve
# yan duvarlar komsuyla paylasilir (RESEARCH.md "dar, cikmaz sokaklar") — bu
# yuzden varsayilan yalnizca SOKAK cephesidir. Kose evi bir bayrak uzaktir.
FACADE_MODES = ("street", "sides", "all")

# Cumba payandasi bu derinligin altinda gorunmez; altinda 'corbel' 'flat'a duser.
MIN_CORBEL_DEPTH = 0.35

# Kapi tas bir ESIGIN ustune oturur, zemine sifirlanmaz. Hem donemin dogru
# detayi hem de duvar panelini kapali (manifold) tutan sey — bkz. make_wall_panel.
THRESHOLD_H = 0.14


class HouseParams(object):
    """Plan Faz 2'nin parametre listesi, tek yerde."""

    def __init__(self, **kw):
        self.floors = kw.get("floors", 2)
        self.width = kw.get("width", 7.0)
        self.depth = kw.get("depth", 6.5)
        self.floor_height = kw.get("floor_height", 2.7)
        self.plinth = kw.get("plinth", 0.6)

        self.cumba_type = kw.get("cumba_type", "flat")
        self.cumba = kw.get("cumba", 0.8)
        self.jetty_side = kw.get("jetty_side", 0.25)

        self.window_detail = kw.get("window_detail", "recess")
        self.window_density = kw.get("window_density", 0.55)
        self.window_width = kw.get("window_width", 0.75)
        self.window_height = kw.get("window_height", 1.35)
        self.kafes_bars = kw.get("kafes_bars", 4)

        self.eave = kw.get("eave", 0.7)
        self.roof_pitch_deg = kw.get("roof_pitch_deg", 30.0)

        # --- yakin plan ---
        self.detail = kw.get("detail", "mass")
        self.facades = kw.get("facades", "street")
        # Kagir duvar kalinligi = sove derinligi. Yaya gozu acikligin gercekten
        # delik oldugunu bu derinlikten anlar; 1632 kagir duvari icin 30 cm makul.
        self.wall_thickness = kw.get("wall_thickness", 0.30)
        # Ahsap karkas kat daha incedir (~18 cm dolgu), oran olarak tutulur ki
        # duvar kalinligi tek yerden ayarlansin.
        self.timber_ratio = kw.get("timber_ratio", 0.6)
        self.rafter_spacing = kw.get("rafter_spacing", 0.75)

        self.palette = kw.get("palette", "default")
        self.seed = kw.get("seed", 0)

    # --------------------------------------------------------------- kurallar

    def apply_palette_rules(self):
        """
        Palet yalnızca renk değil, **tipoloji** taşır.

        RESEARCH.md gayrimüslim mahalle evlerini "daha koyu ve alçak" diye anar.
        Bunu yalnızca renge indirgemek kuralın yarısını sessizce düşürmek olurdu;
        kat yüksekliği ve çıkma da kısılır. Oranlar T2'dir — kaynak niteliksel.
        """
        if self.palette == "nonmuslim":
            self.floor_height = min(self.floor_height, 2.45)
            self.cumba = min(self.cumba, 0.55)
            self.floors = min(self.floors, 2)
        return self

    def validate(self):
        """Üretimden ÖNCE tutarsızlığı yakalar; sessiz saçma geometri üretmeyiz."""
        errs = []
        if not (1 <= self.floors <= 3):
            errs.append(f"floors={self.floors} (1-3 olmali)")
        if self.cumba_type not in CUMBA_TYPES:
            errs.append(f"cumba_type={self.cumba_type} (secenekler: {CUMBA_TYPES})")
        if self.window_detail not in WINDOW_DETAILS:
            errs.append(f"window_detail={self.window_detail} (secenekler: {WINDOW_DETAILS})")
        if self.detail not in DETAIL_LEVELS:
            errs.append(f"detail={self.detail} (secenekler: {DETAIL_LEVELS})")
        if self.facades not in FACADE_MODES:
            errs.append(f"facades={self.facades} (secenekler: {FACADE_MODES})")
        # Duvar kalinligi derinligin ucte birini gecerse ev ic mekansiz kalir.
        if not (0.10 <= self.wall_thickness <= min(self.depth, self.width) / 3.0):
            errs.append(f"wall_thickness={self.wall_thickness} makul degil")
        # Aciklik kat yuksekligine sigmali; sigmazsa panel kenarina degip
        # uretimi patlatir (make_wall_panel). Burada, sebebi anlasilir sekilde.
        if self.window_height > self.floor_height - 0.7:
            errs.append(f"window_height={self.window_height} kat yuksekligine "
                        f"({self.floor_height}) sigmiyor; en fazla "
                        f"{self.floor_height - 0.7:.2f}")
        if self.palette not in PALETTES:
            errs.append(f"palette={self.palette} (secenekler: {tuple(PALETTES)})")
        if not (5.0 <= self.roof_pitch_deg <= 55.0):
            errs.append(f"roof_pitch_deg={self.roof_pitch_deg} (5-55 olmali)")
        if self.width < 3.0 or self.depth < 3.0:
            errs.append(f"ayak izi {self.width}x{self.depth} m cok kucuk")
        # Cumba, ust katin altindaki bosluga tasar; derinligin yarisindan fazlasi
        # yapisal olarak sacma ve siluet olarak da okunmaz.
        if self.cumba > self.depth * 0.5:
            errs.append(f"cumba={self.cumba} derinligin yarisindan buyuk ({self.depth})")
        if errs:
            raise ValueError("HouseParams gecersiz: " + "; ".join(errs))
        return self

    @property
    def wall_height(self):
        """Subasman + katlar (çatı hariç)."""
        return self.plinth + self.floors * self.floor_height

    def roof_height(self, roof_w, roof_d):
        """
        Kırma çatı yüksekliği, **eğimden** türetilir.

        Plan çatıyı eğimle tanımlıyor (yükseklikle değil) çünkü alaturka kiremit
        ancak belirli bir eğim aralığında tutar; sabit yükseklik, ev genişledikçe
        eğimi sessizce düşürürdü.
        """
        half = 0.5 * min(roof_w, roof_d)
        return half * math.tan(math.radians(self.roof_pitch_deg))


# ------------------------------------------------------------------ malzeme

def build_materials(palette_name, textured=False):
    """
    Malzeme kümesini kurar. Dönüş: `(mats, size_by_name)`.

    `size_by_name` her malzemenin GERÇEK DÜNYA doku boyunu (metre) taşır; UV
    izdüşümü bunu okur. Graybox modunda boyut kullanılmaz ama sözlük yine de
    döner ki çağıran iki koda ayrılmasın.

    Dokusu indirilmemiş bir rol sessizce graybox'a düşer **ve söylenir** —
    yarısı dokulu, yarısı düz renkli bir ev, sebebi anlaşılmadan "bozuk" görünür.
    """
    pal = PALETTES[palette_name]
    roles = TEXTURE_ROLES.get(palette_name, {})
    mats, sizes = {}, {}
    missing = []

    for key, (color, rough, name) in pal.items():
        role = roles.get(key) if textured else None
        # `root`: dokusu Poly Haven'dan gelmeyen roller icin (yaprak dokusu
        # prosedureldir ve `art/textures/generated/` altindadir — ADR 0019 §3).
        meta = mtl.load_meta(role["asset"],
                             root=role.get("root", mtl.TEXTURE_ROOT)) if role else None

        if meta is None:
            if role is not None:
                missing.append(f"{key}({role['asset']})")
            mats[key] = hz.make_material(name, color, roughness=rough)
            sizes[name] = (2.0, 2.0)
            continue

        # `tinted`: RENGI PALETTEN GELEN DOKULU MALZEME.
        #
        # Blender tarafi bunu Unity tarafiyla AYNI sekilde yapmali;
        # yoksa inceleme render'i ile oyun ici goruntu iki farkli
        # yuzeyden konusur — bu depoda "bir sayinin iki sahibi" diye
        # ucuncu kez odenen kusurun doku hali olurdu. Unity'de islem
        # `_BaseColor` x `_BaseColorMap`; Blender'daki karsiligi
        # MULTIPLY. Ahsapta COLOR kullanilmasinin sebebi dokunun kendi
        # rengini de tasimasiydi; kumas dokusu NOTR uretildigi icin
        # burada dogru olan carpmadir.
        if role.get("tinted"):
            mats[key] = mtl.make_pbr_material(
                name, meta, tint=color, tint_factor=1.0,
                value_gamma=role.get("value_gamma", 1.0),
                tint_blend="MULTIPLY",
                metallic=role.get("metallic", 0.0))
            sizes[name] = mtl.material_size(meta)
            continue


        mats[key] = mtl.make_pbr_material(
            name, meta,
            tint=role.get("tint"), tint_factor=role.get("tint_factor", 0.0),
            value_gamma=role.get("value_gamma", 1.0),
            tint_blend=role.get("tint_blend", "COLOR"),
            metallic=role.get("metallic", 0.0))
        sizes[name] = mtl.material_size(meta)

    if missing:
        hz.log(f"UYARI dokusu bulunamayan rol(ler): {', '.join(missing)} — "
               f"graybox renge dusuldu. Once: "
               f"python tools/textures/fetch_polyhaven.py --res 2k")
    return mats, sizes


#: Boru hattının UV katmanı adı — **tek** ad, çünkü birleştirme ada bakar.
UV_KATMANI = "UVMap"


def uv_adini_duzelt(obj, ad=UV_KATMANI):
    """
    Nesnenin UV katmanını tek ve doğru adla bırakır.

    ## Neden gerekti — sessiz ve pahalı bir kusur

    Blender, bmesh'ten kurulan bir ağa UV katmanını bazen `Float2`
    adıyla açıyor; MPFB2 gövdesininki ise `UVMap`. `join_parts`
    katmanları **ada göre** eşleştirir, o yüzden iki ad iki ayrı katman
    demek: birleşen ağda `Float2` ve `UVMap` yan yana duruyor, etkin
    olan `Float2` ve gövde köşelerinin orada hiç verisi yok.

    Sonuç ölçüldü: birleşmiş ağda ten yüzlerinin UV kutusu
    **u 0,000-0,000 v 0,000-0,000**. Yani bütün deri, dokunun tek bir
    köşe texel'ini örnekliyordu — dudak, göz, gözenek hepsi dokunun
    içinde duruyor ve hiçbiri ekrana çıkmıyordu. Doku "bozuk" gibi
    görünmüyordu; **düz renk** gibi görünüyordu, yani dokusuz hâlinin
    aynısı. Bir kusurun en pahalısı, düzeltilmiş hâline benzeyenidir.

    Ad birliği bu yüzden üslup değil sözleşme.
    """
    me = obj.data
    if not me.uv_layers:
        return obj
    # SILME REFERANSI GECERSIZ KILAR.
    #
    # Ilk yazimda etkin katman bir degiskene alinip otekiler `k is not
    # etkin` diye siliniyordu; ilk silme `etkin` isaretcisini gecersiz
    # kildi, kiyas her katman icin dogru dondu ve HEPSI silindi
    # ("index 0 out of range, size 0"). Silinen bir koleksiyonda
    # tutulan isaretci, tuttugu sey degildir.
    #
    # Ad ile calisiliyor ve koleksiyon her adimda yeniden okunuyor.
    aktif_ad = (me.uv_layers.active or me.uv_layers[0]).name
    while len(me.uv_layers) > 1:
        fazla = next((x.name for x in me.uv_layers if x.name != aktif_ad),
                     None)
        if fazla is None:
            break
        me.uv_layers.remove(me.uv_layers[fazla])
    me.uv_layers[0].name = ad
    me.uv_layers.active_index = 0

    return obj



def apply_uvs(obj, size_by_name):
    """Nesnenin malzeme sırasına göre dünya ölçekli UV üretir."""
    idx = {}
    for i, m in enumerate(obj.data.materials):
        if m is not None:
            idx[i] = size_by_name.get(m.name, (2.0, 2.0))
    mtl.uv_project(obj, idx)
    uv_adini_duzelt(obj)


# ------------------------------------------------------------------ parçalar

def _corbel_bracket(name, width, depth, height, center, col):
    """
    Cumbayı taşıyan üçgen payanda (konsol).

    Kutu değil üçgen prizma: payandanın işlevi yükü duvara aktarmaktır ve siluetin
    okunması bu eğik alt yüze bağlıdır. Kutu koymak "kalın bir raf" gibi görünürdü.
    """
    cx, cy, cz = center
    hw = width * 0.5
    bm = bmesh.new()
    # Ucgen kesit: duvarda tam yukseklik, disa dogru sifira iner.
    #  y=+depth/2 duvar tarafi, y=-depth/2 dis uc
    prof = [(-hw, depth * 0.5, height * 0.5), (hw, depth * 0.5, height * 0.5),
            (hw, depth * 0.5, -height * 0.5), (-hw, depth * 0.5, -height * 0.5),
            (-hw, -depth * 0.5, height * 0.5), (hw, -depth * 0.5, height * 0.5)]
    vs = [bm.verts.new((cx + x, cy + y, cz + z)) for x, y, z in prof]
    bm.verts.ensure_lookup_table()
    faces = [(0, 1, 2, 3), (4, 5, 1, 0), (3, 2, 5, 4), (0, 3, 4), (1, 5, 2)]
    for f in faces:
        bm.faces.new([vs[i] for i in f])
    bm.normal_update()
    return hz.mesh_from_bmesh(name, bm, col)


# --------------------------------------------------------- cephe çerçevesi
#
# Her cephe kendi (u, v, n) çerçevesinde çalışır: `u` duvar boyunca, `v` kat
# tabanından yukarı, `n` **dışarı**. Bütün parçalar bu çerçevede konumlanır ve
# dünya koordinatına tek yerde çevrilir.
#
# Sebebi acı deneyim: dışa yön işaretini her çağrıda elle yazmak hataya
# davetiyedir. Sokak cephesi −Y olduğu için işaret negatiftir; ters yazmak
# parçayı duvarın İÇİNE gömer ve parça **hiç görünmez** — hata sessizdir,
# render'a bakana kadar fark edilmez. İlk yazımda tam olarak bu oldu, üç yerde.
# Artık işaret `_wall_axes` içinde bir kez yaşıyor ve `n > 0` her zaman dışarısı.

def _wall_axes(face, w, d, cy):
    """
    Cephe adı → `(u_axis, n_axis, dış yüz orta noktası, duvar genişliği)`.

    `front` sokak cephesidir (−Y). `u_axis` her cephede dışarıdan bakan birinin
    **soldan sağa** yönüdür; böylece asimetrik bir yerleşim eklendiğinde bütün
    cepheler aynı okunur.
    """
    if face == "front":
        return (1.0, 0.0), (0.0, -1.0), (0.0, cy - d * 0.5), w
    if face == "back":
        return (-1.0, 0.0), (0.0, 1.0), (0.0, cy + d * 0.5), w
    if face == "left":
        return (0.0, -1.0), (-1.0, 0.0), (-w * 0.5, cy), d
    if face == "right":
        return (0.0, 1.0), (1.0, 0.0), (w * 0.5, cy), d
    raise ValueError(f"bilinmeyen cephe: {face}")


def _frame(u_axis, n_axis, origin):
    """
    `(u, v, n)` → dünya dönüşümü ve eksen hizalı kutu boyutu eşlemesi.

    `origin` duvarın **dış yüzünün** taban ortasıdır; yani `n = 0` cephe düzlemi,
    `n > 0` dışarısı, `n < 0` duvarın içi. Taşma miktarları her cephede aynı
    sayıyla yazılır.
    """
    ux, uy = u_axis
    nx, ny = n_axis
    ox, oy, oz = origin

    def pos(u, v, n):
        return (ox + ux * u + nx * n, oy + uy * u + ny * n, oz + v)

    def size(du, dv, dn):
        return (du, dn, dv) if abs(ux) > 0.5 else (dn, du, dv)

    return pos, size


def _kafes_bars(name, ow, oh, bars, pos, size, n, col):
    """Kafes çıtaları — düşey ağırlıklı ince ızgara, açıklığın önünde."""
    out = []
    t = 0.035
    for i in range(bars):
        u = (i + 0.5) / bars - 0.5
        out.append(hz.make_box(f"{name}_V{i}", size(t, oh * 0.96, t),
                               pos(u * ow, 0.0, n), col))
    rows = max(1, bars // 2)
    for j in range(rows):
        v = (j + 0.5) / rows - 0.5
        out.append(hz.make_box(f"{name}_H{j}", size(ow * 0.96, t, t),
                               pos(0.0, v * oh, n), col))
    return out


# ------------------------------------------------------- açıklık yerleşimi

def _opening_layout(p, span_w, ground):
    """
    Bir cephe katındaki açıklıkların yerleşimi — **geometriden bağımsız**.

    Dönüş: `[{kind, cu, w, v0, v1}]`; `cu` cephe ortasına göre yatay, `v` kat
    tabanından yukarı. Aynı yerleşimi hem `mass` hem `near` yapımı okur; ayrı
    hesaplasalardı iki kademe zamanla sessizce ayrışır ve LOD geçişinde kapı
    yerinden oynardı.

    Pencere SAYISI yoğunluktan ve cephe genişliğinden türetilir, elle verilmez:
    aynı yoğunluk dar ve geniş evde farklı sayı üretmeli, yoksa dar evde
    pencereler birbirine girer.
    """
    windows = p.window_detail != "none"
    pitch = p.window_width / max(p.window_density, 0.05)
    count = int(span_w / pitch) if windows else 0

    # Zemin katta kapi ORTA AKSA oturur ve o bolme pencereye kapalidir.
    #
    # Ilk yazimda kapi bagimsiz olarak x=0'a konuyordu ve cift sayili bolmede
    # iki pencereyle ust uste biniyordu (olculdu: 4 pencereden 2'si cakisiyor).
    # Bolme sayisini TEK yapip ortasini kapiya ayirmak, hem cakismayi yapisal
    # olarak imkansiz kilar hem de cephenin ritmini dogru verir: Osmanli
    # konutunda kapi cephenin bir aksidir, pencerelerin arasina sikistirilmis
    # bir bosluk degil.
    door_bay = -1
    if ground:
        if count < 3:
            count = 1            # dar cephe (ya da penceresiz kip): yalnizca kapi
        elif count % 2 == 0:
            count -= 1           # tek yap; daraltmak genisletmekten guvenli
        door_bay = count // 2
    if count < 1:
        return []

    cv = p.floor_height * 0.55
    wv0, wv1 = cv - p.window_height * 0.5, cv + p.window_height * 0.5
    bay = span_w / count

    out = []
    for i in range(count):
        cu = ((i + 0.5) / count - 0.5) * span_w
        if i == door_bay:
            # Kapi lentosu pencere lentosuyla AYNI HIZADA. Osmanli cephesinde
            # bu hiza kuraldir; ayrica birkac santimlik fark, delikli duvarda
            # kagit inceliginde serit yuzler dogururdu.
            v1 = wv1 if windows else min(p.floor_height - 0.45, THRESHOLD_H + 2.05)
            out.append(dict(kind="door", cu=cu, w=min(1.05, bay * 0.82),
                            v0=THRESHOLD_H, v1=v1))
        elif windows:
            out.append(dict(kind="window", cu=cu, w=p.window_width,
                            v0=wv0, v1=wv1))
    return out


# -------------------------------------------------------- cephe: 'mass' kipi

def _dress_mass(p, mats, col, tag, o, pos, size):
    """
    Açıklığın **ucuz** hâli: cepheye yapıştırılmış koyu panel + ince söve.

    30 m'den gerçek nişten ayırt edilemez ve ~12 üçgen tutar. Kalabalık şehir
    dokusu bunu kullanır; yaya seviyesinde `near` devreye girer.
    """
    parts = []
    cu, cv = o["cu"], (o["v0"] + o["v1"]) * 0.5
    ow, oh = o["w"], o["v1"] - o["v0"]
    depth = 0.16 if o["kind"] == "door" else 0.14

    panel = hz.make_box(f"{o['kind'].title()}_{tag}", size(ow, oh, depth),
                        pos(cu, cv, 0.02 - depth * 0.5), col)
    hz.assign(panel, mats["shadow"])
    parts.append(panel)

    ft = 0.09
    if o["kind"] == "door":
        edges = [(0.0, oh * 0.5 + ft * 0.5, ow + 0.22, 0.12)]
    else:
        edges = [(0.0, oh * 0.5, ow + 2 * ft, ft), (0.0, -oh * 0.5, ow + 2 * ft, ft),
                 (-ow * 0.5, 0.0, ft, oh), (ow * 0.5, 0.0, ft, oh)]
    for du, dv, sw, sh in edges:
        f = hz.make_box(f"{tag}_Frame", size(sw, sh, 0.10),
                        pos(cu + du, cv + dv, 0.0), col)
        hz.assign(f, mats["trim"])
        parts.append(f)

    if o["kind"] == "window" and p.window_detail == "kafes":
        def at(du, dv, dn):
            return pos(cu + du, cv + dv, dn)
        for bar in _kafes_bars(f"Kafes_{tag}", ow, oh, p.kafes_bars,
                               at, size, 0.10, col):
            hz.assign(bar, mats["trim"])
            parts.append(bar)
    return parts


# -------------------------------------------------------- cephe: 'near' kipi

def _dress_near(p, mats, col, tag, o, pos, size, thick, sill_mat):
    """
    Açıklığın **yaya seviyesi** hâli.

    Duvar zaten delinmiştir (`make_wall_panel`); burada eklenen şey açıklığın
    çevresindeki gerçek yapı elemanlarıdır. Yakın planda binayı "model" olmaktan
    çıkaran bunların gölge çizgileridir, doku çözünürlüğü değil:

    * **denizlik** — pencerenin altından dışarı taşan silme; yağmuru cepheden
      uzaklaştırır ve sokaktan bakan gözün ilk yakaladığı yatay gölge odur.
    * **söve** — açıklığı çerçeveleyen, cepheden 6 cm taşan ahşap.
    * **kepenk/karanlık** — söve boşluğunun DİBİNDE durur, cepheye yapışmaz;
      aradaki boşluk açıklığın derinliğini okutan şeydir.
    * **eşik** — kapı taş bir eşiğin üstüne oturur (bkz. `THRESHOLD_H`).
    * **kanat** — kapı kanadı nişin içine çekilir; söve derinliği görünür kalır.

    `sill_mat` duvarla birlikte değişir: kâgir katta taş denizlik, ahşap karkas
    katta ahşap. İlk yazımda taş sabitlenmişti ve render'da ahşap üst katın
    pencerelerinde taş denizlikler çıktı — taşıyıcısı ahşap olan bir duvara taş
    denizlik oturmaz.
    """
    parts = []
    cu, cv = o["cu"], (o["v0"] + o["v1"]) * 0.5
    ow, oh = o["w"], o["v1"] - o["v0"]
    inner = -thick                                 # duvarin ic yuzu

    def add(name, sz, p3, mat):
        obj = hz.make_box(name, sz, p3, col)
        hz.assign(obj, mat)
        parts.append(obj)

    if o["kind"] == "door":
        add(f"Door_{tag}_Dark", size(ow, oh, 0.04), pos(cu, cv, inner + 0.02),
            mats["shadow"])
        # Kanat nisin icinde: on yuzu duvarin 10 cm gerisinde.
        add(f"Door_{tag}_Leaf", size(ow - 0.05, oh - 0.04, 0.07),
            pos(cu, cv, inner + 0.13), mats["trim"])
        # Esik: tas, cepheden 10 cm tasar. Yaya gozunun tam hizasinda.
        add(f"Door_{tag}_Threshold", size(ow + 0.26, THRESHOLD_H, 0.26),
            pos(cu, THRESHOLD_H * 0.5, -0.03), mats["stone"])
        for du in (-1.0, 1.0):
            add(f"Door_{tag}_Jamb", size(0.13, oh + 0.13, 0.14),
                pos(cu + du * (ow + 0.13) * 0.5, cv + 0.065, 0.01), mats["trim"])
        add(f"Door_{tag}_Lintel", size(ow + 0.34, 0.15, 0.16),
            pos(cu, o["v1"] + 0.075, 0.01), mats["trim"])
        return parts

    add(f"Win_{tag}_Dark", size(ow, oh, 0.04), pos(cu, cv, inner + 0.02),
        mats["shadow"])
    # Denizlik: disari 7 cm tasar, duvara 12 cm girer.
    add(f"Win_{tag}_Sill", size(ow + 0.26, 0.08, 0.19),
        pos(cu, o["v0"] - 0.04, -0.025), sill_mat)
    for du in (-1.0, 1.0):
        add(f"Win_{tag}_Jamb", size(0.10, oh + 0.10, 0.12),
            pos(cu + du * (ow + 0.10) * 0.5, cv, 0.0), mats["trim"])
    add(f"Win_{tag}_Head", size(ow + 0.22, 0.11, 0.14),
        pos(cu, o["v1"] + 0.055, 0.01), mats["trim"])

    if p.window_detail == "kafes":
        def at(du, dv, dn):
            return pos(cu + du, cv + dv, dn)
        for bar in _kafes_bars(f"Kafes_{tag}", ow, oh, p.kafes_bars,
                               at, size, -0.07, col):
            hz.assign(bar, mats["trim"])
            parts.append(bar)
    return parts


# ------------------------------------------------------------- kat kabuğu

def _build_floor(p, mats, col, asset_name, tag, level_z, w, d, cy, body_mat,
                 ground, bosluk=None):
    """
    Bir katın dört cephesini kurar.

    `mass` kipinde gövde tek kutledir ve açıklıklar üstüne yapıştırılır.
    `near` kipinde her cephe **ayrı ve delikli** bir panel olarak örülür; ev
    içi boşalır, açıklıktan bakınca söve derinliği görünür. Yan paneller
    kalınlık kadar kısaltılır, böylece köşeleri ön/arka paneller kapatır ve
    köşede çift duvar oluşmaz.
    """
    near = p.detail == "near"
    thick = p.wall_thickness if body_mat is not mats["timber"] \
        else p.wall_thickness * p.timber_ratio
    faces = {"street": ("front",),
             "sides": ("front", "left", "right"),
             "all": ("front", "back", "left", "right")}[p.facades]

    parts = []
    if not near:
        body = hz.make_box(f"{asset_name}_{tag}", (w, d, p.floor_height),
                           (0.0, cy, level_z + p.floor_height * 0.5), col)
        hz.assign(body, body_mat)
        parts.append(body)
    else:
        # KATIN TAVANI.
        #
        # `near` kipinde kat yalniz dort duvar paneliydi; arada doseme
        # yoktu. Ev disaridan dogru gorunuyordu cunku kimse icine
        # girmiyordu — collider dolu bir kutuydu. Zemin kat acilinca
        # eksik goruldu: kapidan girip yukari bakinca ust katin ici ve
        # catinin ici goruluyor.
        #
        # Doseme ayni zamanda ust katin ZEMINI. Iki ayri levha koymak
        # ayni yuzeyi iki kez cizmek olurdu; bir tanesi iki isi de
        # goruyor ve 12 ucgen tutuyor.
        for son, boyut, merkez in _tavan_parcalari(
                w, d, cy, 0.18, level_z + p.floor_height - 0.09, bosluk):
            tavan = hz.make_box(f"{asset_name}_{tag}_Doseme{son}",
                                boyut, merkez, col)
            hz.assign(tavan, mats["timber"])
            parts.append(tavan)

    for face in ("front", "back", "left", "right"):
        u_axis, n_axis, (fx, fy), span = _wall_axes(face, w, d, cy)
        if near and face in ("left", "right"):
            span -= 2.0 * thick
        layout = _opening_layout(p, span * 0.86, ground and face == "front") \
            if face in faces else []

        if near:
            origin = (fx, fy, level_z)
            panel_o = (fx - n_axis[0] * thick * 0.5, fy - n_axis[1] * thick * 0.5,
                       level_z)
            ops = [(o["cu"] - o["w"] * 0.5, o["cu"] + o["w"] * 0.5, o["v0"], o["v1"])
                   for o in layout]
            wall = hz.make_wall_panel(f"{asset_name}_{tag}_{face}", span,
                                      p.floor_height, thick, panel_o,
                                      u_axis, n_axis, openings=ops, col=col)
            hz.assign(wall, body_mat)
            parts.append(wall)

        if not layout:
            continue
        pos, size = _frame(u_axis, n_axis, (fx, fy, level_z))
        # Denizlik duvarin malzemesini izler: kagir -> tas, ahsap karkas -> ahsap.
        sill_mat = mats["trim"] if body_mat is mats["timber"] else mats["stone"]
        for i, o in enumerate(layout):
            t = f"{tag}{face[0].upper()}{i}"
            parts += (_dress_near(p, mats, col, t, o, pos, size, thick, sill_mat)
                      if near else _dress_mass(p, mats, col, t, o, pos, size))

        # Kapinin onune tas basamak. Kapi esigi subasmanin USTUNDE durur; bu
        # OLCULDU (subasman 0,60 + esik 0,14 = sokaktan 0,74 m) ve basamaksiz
        # hali yaya seviyesinde apacik kirik gorunuyordu: kapi havada asili.
        # Basamak sayisi yukseltiden TURETILIR — subasman degistiginde elle
        # duzeltilmesi gereken bir sabit birakmak, ayni hatanin geri gelmesidir.
        if ground and face == "front":
            doors = [o for o in layout if o["kind"] == "door"]
            if doors:
                parts += _door_steps(p, mats, col, doors[0], pos, size, level_z)
    return parts


def _door_steps(p, mats, col, door, pos, size, level_z):
    """Kapı önü taş basamak (sahanlık). Yükselti = subasman."""
    rise_total = level_z
    if rise_total < 0.12:
        return []
    n = max(1, int(round(rise_total / 0.20)))
    rise, tread = rise_total / n, 0.28
    out = []
    for i in range(n):
        # Basamak i: ustu (i+1)*rise'da, duvardan (n-i)*tread disari uzanir.
        depth = (n - i) * tread
        obj = hz.make_box(f"Step{i}", size(door["w"] + 0.50, rise, depth),
                          pos(door["cu"], -rise_total + (i + 0.5) * rise,
                              depth * 0.5), col)
        hz.assign(obj, mats["stone"])
        out.append(obj)
    return out


# ---------------------------------------------------------- yakın plan ekleri

def _eave_rafters(p, mats, col, asset_name, top_w, top_d, cy, roof_w, roof_d,
                  z_top, deck_h):
    """
    Saçak altı **mertekleri** — yakın planın en büyük tek kazancı.

    Sokakta yürüyen göz geniş saçağın ALTINI görür; orası evin en büyük tek
    yüzeyidir ve boş bırakılırsa bina anında "kutu" olur. Osmanlı konutunda
    mertek uçları açıkta bırakılır; ritmik gölgesi tipolojinin imzasıdır.

    Mertekler kaplamanın **altında** durur (mertek → kaplama → kiremit); üstüne
    konsaydı yapım sırası tersine dönerdi. Yalnızca saçak boşluğunda, yani
    duvar hizasından çatı ucuna kadar üretilirler — duvarın üstünde kalan kısım
    zaten görünmez. Köşelerde ön/arka ile yan mertekler çakışmasın diye her
    kenar kendi kat ayak izi boyunca dağıtılır; köşeyi kırma çatının mahyası
    çözer.
    """
    parts = []
    sec_w, sec_h = 0.09, 0.13
    top = z_top - deck_h
    cvz = top - sec_h * 0.5
    ov_y, ov_x = (roof_d - top_d) * 0.5, (roof_w - top_w) * 0.5

    def row(span, count_len, axis):
        n = max(2, int(count_len / p.rafter_spacing))
        return [((i + 0.5) / n - 0.5) * span for i in range(n)]

    for sign in (-1.0, 1.0):                       # ön (−Y) ve arka (+Y) saçak
        length = ov_y + 0.12
        y_out = cy + sign * roof_d * 0.5
        y_c = y_out - sign * length * 0.5
        for x in row(top_w - sec_w, top_w, "x"):
            parts.append(hz.make_box(f"{asset_name}_Rafter",
                                     (sec_w, length, sec_h), (x, y_c, cvz), col))
    for sign in (-1.0, 1.0):                       # yan saçaklar
        length = ov_x + 0.12
        x_out = sign * roof_w * 0.5
        x_c = x_out - sign * length * 0.5
        for y in row(top_d - sec_w, top_d, "y"):
            parts.append(hz.make_box(f"{asset_name}_Rafter",
                                     (length, sec_w, sec_h), (x_c, cy + y, cvz), col))

    for obj in parts:
        hz.assign(obj, mats["trim"])
    return parts


def _near_extras(p, mats, col, asset_name, top_w, top_d, cy, roof_w, roof_d,
                 rh, z_top, wall_top):
    """
    Yakın planda okunan küçük yapı elemanları: subasman silmesi, ahşap karkas
    köşe dikmeleri ve hatıl, mahya.

    Hepsi ucuz (12 üçgen) ama **gölge çizgisi** üretir; yakın planda gerçekçilik
    hissi malzemeden çok bu çizgilerden gelir.
    """
    parts = []

    def add(name, sz, p3, mat):
        obj = hz.make_box(name, sz, p3, col)
        hz.assign(obj, mat)
        parts.append(obj)

    # Subasman silmesi: tastan ahsaba gecis. Su damlasini duvardan ayirir.
    add(f"{asset_name}_PlinthCourse", (p.width + 0.14, p.depth + 0.14, 0.10),
        (0.0, 0.0, p.plinth - 0.05), mats["stone"])

    # Ahsap karkas: kose dikmeleri + ust hatil. Kose merkezine oturur, yani
    # iki cepheye birden tasar — himis duvarin dogru okunusu budur.
    if p.floors > 1:
        post = 0.17
        z0 = wall_top - p.floor_height
        for sx in (-1.0, 1.0):
            for sy in (-1.0, 1.0):
                add(f"{asset_name}_Post", (post, post, p.floor_height),
                    (sx * top_w * 0.5, cy + sy * top_d * 0.5,
                     z0 + p.floor_height * 0.5), mats["timber"])
        add(f"{asset_name}_Beam", (top_w + post, 0.16, 0.17),
            (0.0, cy - top_d * 0.5, wall_top - 0.10), mats["trim"])
        add(f"{asset_name}_Beam", (top_w + post, 0.16, 0.17),
            (0.0, cy + top_d * 0.5, wall_top - 0.10), mats["trim"])
        add(f"{asset_name}_Beam", (0.16, top_d, 0.17),
            (-top_w * 0.5, cy, wall_top - 0.10), mats["trim"])
        add(f"{asset_name}_Beam", (0.16, top_d, 0.17),
            (top_w * 0.5, cy, wall_top - 0.10), mats["trim"])

    # Mahya: kirma catinin tepe cizgisi. Alaturka kiremitte mahya ayri bir
    # kiremit siralamasidir ve siluetin en ust cizgisini kalinlastirir.
    if roof_w >= roof_d:
        length = max(0.4, roof_w - 2.0 * min(roof_d * 0.5, roof_w * 0.45))
        add(f"{asset_name}_Ridge", (length + 0.30, 0.26, 0.17),
            (0.0, cy, z_top + rh - 0.05), mats["roof"])
    else:
        length = max(0.4, roof_d - 2.0 * min(roof_w * 0.5, roof_d * 0.45))
        add(f"{asset_name}_Ridge", (0.26, length + 0.30, 0.17),
            (0.0, cy, z_top + rh - 0.05), mats["roof"])
    return parts


# -------------------------------------------------------------------- ev

def build_house(p, col, asset_name, textured=False):
    """
    Tam evi kurar. Dönüş: `(lod0, lod1, lod2, ucx, info)`.

    `info` ölçülen değerleri taşır (ayak izi, yükseklik, üçgen sayıları) — inceleme
    paketinin ve testlerin okuduğu şey odur; log satırından metin ayıklanmaz.
    """
    p.validate()
    mats, tex_sizes = build_materials(p.palette, textured=textured)
    parts = []

    z = 0.0

    # 1) Tas subasman — nemi ve sokak suyunu ahsap katlardan ayirir.
    plinth = hz.make_box(f"{asset_name}_Plinth", (p.width, p.depth, p.plinth),
                         (0.0, 0.0, p.plinth * 0.5), col)
    hz.assign(plinth, mats["stone"])
    parts.append(plinth)
    z += p.plinth

    # 2) Alt katlar — tam ayak izi, kagir + kirec badana.
    merdiven = merdiven_plani(p) if p.detail == "near" else None
    for i in range(p.floors - 1):
        parts += _build_floor(p, mats, col, asset_name, f"L{i}", z,
                              p.width, p.depth, 0.0, mats["plaster"],
                              ground=(i == 0),
                              bosluk=merdiven if i == 0 else None)
        if i == 0 and p.detail == "near":
            parts += _ic_bolme_geometri(p, mats, col, asset_name, z)
            parts += _mobilya(p, mats, col, asset_name, z)
            if merdiven is not None:
                for son, boyut, merkez in _merdiven_parcalari(merdiven, p, z):
                    o = hz.make_box(f"{asset_name}_Merdiven_{son}",
                                    boyut, merkez, col)
                    hz.assign(o, mats["timber"])
                    parts.append(o)
        z += p.floor_height

    # 3) Ust kat — cumbali. Cumba bu tipolojinin imzasi: alt ayak izini
    #    buyutmeden ust katta yer kazandirir, sokagi daraltip golgeler.
    cumba_d = 0.0 if p.cumba_type == "none" else p.cumba
    side = 0.0 if p.cumba_type == "none" else p.jetty_side
    if p.cumba_type == "corner":
        side = max(side, p.cumba * 0.6)     # kose cumbasi yanlara da doner

    top_w = p.width + 2.0 * side
    top_d = p.depth + cumba_d
    top_cy = -cumba_d * 0.5
    top_street_y = top_cy - top_d * 0.5

    # Cumbanin ALTI kapatilir. 'near' kipinde duvarlar kabuktur; kapatilmazsa
    # cikmanin altindan gecen oyuncu evin icini gorur. Ayrica bu doseme, cumba
    # ucunda gozun aradigi ahsap kalinlik cizgisini verir.
    if p.detail == "near":
        slab = hz.make_box(f"{asset_name}_JettySlab", (top_w, top_d, 0.18),
                           (0.0, top_cy, z + 0.09), col)
        hz.assign(slab, mats["trim"])
        parts.append(slab)

    parts += _build_floor(p, mats, col, asset_name, "Top", z, top_w, top_d, top_cy,
                          mats["timber"] if p.floors > 1 else mats["plaster"],
                          ground=(p.floors == 1))
    # Tek katli evde zemin kat "Top" olarak kuruluyor; bolme oraya gelir.
    if p.floors == 1 and p.detail == "near":
        parts += _ic_bolme_geometri(p, mats, col, asset_name, z)
        parts += _mobilya(p, mats, col, asset_name, z)

    # 3b) Payandalar — yalnizca 'corbel' ve yeterli derinlikte.
    if p.cumba_type == "corbel" and cumba_d >= MIN_CORBEL_DEPTH:
        bh = min(0.6, p.floor_height * 0.28)
        for u in (-0.34, 0.0, 0.34):
            br = _corbel_bracket(f"{asset_name}_Corbel", 0.18, cumba_d, bh,
                                 (u * p.width, top_street_y + cumba_d * 0.5,
                                  z - bh * 0.5), col)
            hz.assign(br, mats["timber"])
            parts.append(br)

    z += p.floor_height
    wall_top = z

    # 4) Kirma cati — EGIMDEN yukseklik. Sacak ust ayak izini her yonde asar;
    #    genis sacak hem yagmuru cepheden uzaklastirir hem sokagi golgeler.
    roof_w = top_w + 2.0 * p.eave
    roof_d = top_d + 2.0 * p.eave
    rh = p.roof_height(roof_w, roof_d)
    roof = hz.make_hip_roof(f"{asset_name}_Roof", roof_w, roof_d, rh,
                            center_xy=(0.0, top_cy), base_z=z,
                            ridge_axis="X" if roof_w >= roof_d else "Y", col=col)
    hz.assign(roof, mats["roof"])
    parts.append(roof)

    # Sacak alinligi (fascia) + soffit kalinligi.
    #
    # Bu 12 ucgen, silueti "oyun kiti" olmaktan cikaran tek en buyuk parcadir:
    # kalinliksiz bir cati sacakta KAGIT gibi biter ve goz bunu hemen yakalar.
    # Gercekte kiremit bir asik-kaplama katmaninin uzerinde oturur ve o katman
    # sacak ucunda ahsap bir alinlik tahtasiyla kapanir.
    fascia_h = max(0.14, rh * 0.07)
    fascia = hz.make_box(f"{asset_name}_Fascia", (roof_w, roof_d, fascia_h),
                         (0.0, top_cy, z - fascia_h * 0.5), col)
    hz.assign(fascia, mats["trim"])
    parts.append(fascia)

    # 5) Baca — kagir, catinin arka yamacindan cikar.
    ch = rh * 0.9 + 0.5
    chim = hz.make_box(f"{asset_name}_Chimney", (0.5, 0.5, ch),
                       (p.width * 0.28, top_cy + top_d * 0.22, z + ch * 0.5), col)
    hz.assign(chim, mats["stone"])
    parts.append(chim)

    # Baca kulahi: bacanin ustunu kapatan tasma. Kulahsiz baca gokyuzune karsi
    # cizilmis bir dikdortgen gibi durur; siluetin okundugu tek yer orasidir.
    if p.detail == "near":
        cap = hz.make_box(f"{asset_name}_ChimneyCap", (0.68, 0.68, 0.13),
                          (p.width * 0.28, top_cy + top_d * 0.22, z + ch + 0.02), col)
        hz.assign(cap, mats["stone"])
        parts.append(cap)

    # 6) Yakin plan ekleri — yalnizca 'near'. LOD1/LOD2 bunlari HIC gormez,
    #    yani yaya detayi uzak silueti hicbir sey odetmez.
    if p.detail == "near":
        parts += _eave_rafters(p, mats, col, asset_name, top_w, top_d, top_cy,
                               roof_w, roof_d, z, fascia_h)
        parts += _near_extras(p, mats, col, asset_name, top_w, top_d, top_cy,
                              roof_w, roof_d, rh, z, wall_top)

    total_h = z + rh

    # --- LOD0: her sey tek mesh ---
    lod0 = hz.join(parts, f"SM_{asset_name}_LOD0", col)
    _purge(parts)

    # --- LOD1: acikliklar ve payandalar dusuyor, cumba ve baca KALIYOR ---
    # Cumba silueti belirler; onu LOD1'de dusurmek pop-in yaratir. Pencereler
    # ise 30 m'den sonra zaten okunmaz.
    l1 = []
    l1.append(_solid(f"{asset_name}_L1_Base", (p.width, p.depth, wall_top - p.floor_height),
                     (0.0, 0.0, (wall_top - p.floor_height) * 0.5), col, mats["plaster"]))
    l1.append(_solid(f"{asset_name}_L1_Top", (top_w, top_d, p.floor_height),
                     (0.0, top_cy, wall_top - p.floor_height * 0.5), col, mats["timber"]))
    r1 = hz.make_hip_roof(f"{asset_name}_L1_Roof", roof_w, roof_d, rh,
                          center_xy=(0.0, top_cy), base_z=wall_top,
                          ridge_axis="X" if roof_w >= roof_d else "Y", col=col)
    hz.assign(r1, mats["roof"])
    l1.append(r1)
    # Alinlik LOD1'de de kalir: silueti belirleyen kenar odur.
    l1.append(_solid(f"{asset_name}_L1_Fascia", (roof_w, roof_d, fascia_h),
                     (0.0, top_cy, wall_top - fascia_h * 0.5), col, mats["trim"]))
    l1.append(_solid(f"{asset_name}_L1_Chimney", (0.5, 0.5, ch),
                     (p.width * 0.28, top_cy + top_d * 0.22, wall_top + ch * 0.5),
                     col, mats["stone"]))
    lod1 = hz.join(l1, f"SM_{asset_name}_LOD1", col)
    _purge(l1)

    # --- LOD2: yalnizca siluet. Tek kutle + tek cati; baca yok. ---
    # Plan Faz 4'un impostor esigine kadar bu kullanilir.
    l2 = []
    l2.append(_solid(f"{asset_name}_L2_Mass", (top_w, top_d, wall_top),
                     (0.0, top_cy * 0.5, wall_top * 0.5), col, mats["plaster"]))
    r2 = hz.make_hip_roof(f"{asset_name}_L2_Roof", roof_w, roof_d, rh,
                          center_xy=(0.0, top_cy), base_z=wall_top,
                          ridge_axis="X" if roof_w >= roof_d else "Y", col=col)
    hz.assign(r2, mats["roof"])
    l2.append(r2)
    lod2 = hz.join(l2, f"SM_{asset_name}_LOD2", col)
    _purge(l2)

    # --- Carpisma kutlesi ---
    # Siluetten DAR: ucus oyununda oyuncu "degmedim ama carpistim" hissini
    # affetmez. Sacak ve cumba collider'a girmez.
    ucx = _carpisma(p, col, mats, asset_name, total_h)

    # UV yalnizca gorunen mesh'lere. UCX cizilmez; UV'si bos kalabilir.
    # Sira onemli: UV, LOD'lar BIRLESTIRILDIKTEN sonra uretilir cunku malzeme
    # indeksleri ancak o zaman nihai hale gelir (hz.join yeniden esler).
    for obj in (lod0, lod1, lod2):
        apply_uvs(obj, tex_sizes)

    mn, mx = hz.bounds(lod0)
    info = {
        "footprint_x": round(mx[0] - mn[0], 3),
        "footprint_y": round(mx[1] - mn[1], 3),
        "height": round(total_h, 3),
        "roof_height": round(rh, 3),
        "wall_top": round(wall_top, 3),
        "pivot_min_z": round(mn[2], 4),
        "tris_lod0": _tris(lod0),
        "tris_lod1": _tris(lod1),
        "tris_lod2": _tris(lod2),
        # Ayak izi CATI SACAGINI icerir; sokak yerlestiricisi ise DUVAR hattini
        # bilmek zorundadir — ev sokak cizgisine duvariyla oturur, sacak ve
        # cumba sokagin USTUNE tasar (RESEARCH.md §4.1(d)). Ikisini karistirmak
        # evleri sokaktan yarim metre geri iter ve doku gevser.
        "wall_width": round(p.width, 3),
        "wall_depth": round(p.depth, 3),
        "floors": p.floors,
        "cumba_type": p.cumba_type,
        "window_detail": p.window_detail,
        "detail": p.detail,
        "facades": p.facades,
        "wall_thickness": round(p.wall_thickness, 3),
        "palette": p.palette,
    }
    return lod0, lod1, lod2, ucx, info


def ic_bolmeler(p):
    """
    Zemin katın bölme duvarları. **Tek sahip.**

    Aynı liste hem görünen geometriyi hem çarpışma kütlesini besler.
    İki ayrı yerde iki plan olsaydı, biri ötekinden birkaç santim
    kayardı ve o kayma ancak oyuncu görünmez bir duvara toslayınca fark
    edilirdi — bu oturumda tam bu tür üç kusur ölçüldü.

    Dönüş: `[{eksen, konum, kapi, kapi_en}]`
      * `eksen` — duvarın uzandığı eksen: `"x"` (enine) ya da `"y"` (boyuna)
      * `konum` — duvarın öteki eksendeki yeri (yerel, ev merkezine göre)
      * `kapi` — kapı boşluğunun merkezi (duvarın kendi ekseninde)
      * `kapi_en` — boşluğun eni (m)

    ## Plan nereden geliyor

    RESEARCH.md §4.1(e): 17. yy evinde **ortalama 4,12 oda**. Bu, evin
    tamamı için — zemin katta iki-üç hacim, üst katta gerisi. Buradaki
    plan zemin katı kuruyor.

    Osmanlı konutunun planı odaların bir koridora dizilmesi değil,
    **hayat**ın merkezde olması ve odaların ona açılmasıdır (RESEARCH
    §4.1: *hayatlı*). O yüzden geniş evde iki boyuna bölme kapı
    aksından geçer ve ortada bir hayat bırakır; odalar hayata açılır.

    Dar evde bu olmaz: 6,2 m'nin altında iki bölme koyunca hayat
    1,5 m'ye, odalar 1,8 m'ye iner — koridor genişliğinde "oda" oda
    değildir. Orada tek enine bölme kullanılır: sokağa bakan ön hacim,
    arkada ikinci hacim. Bu da dönemin dar parsel evidir.
    """
    #: Hayatın eni (m). Kapı 1,05 m; hayat ondan dar olamaz.
    HAYAT = 2.10
    #: Bölme kalınlığı (m) — ahşap bölme, taşıyıcı değil.
    KALIN = 0.12
    #: İç kapı boşluğu (m).
    #:
    #: 0,95 ile başladı ve dar evlerde arka odayı — dolayısıyla
    #: merdiveni — gezinme ağından koparıyor gibi görünüyor: başarısız
    #: bir evin zemin kat navmesh'i 53 köşe, başarılı olanınki 139.
    #: Ajan yarıçapı (0,30 m) iki yandan yiyince 0,95'lik kapıdan
    #: geriye 0,35 m kalıyor — sınırda.
    #:
    #: 1,15 m dönem için de doğru: iç kapı sokak kapısından dar ama
    #: sedir taşınacak kadar geniştir.
    KAPI = 1.15

    out = []

    # ÇOK DAR EVDE BÖLME YOK.
    #
    # 5,8 m'nin altında iç genişlik 5,2 m'ye iner; bir enine bölme
    # koyduğunuzda önde 2,3 m, arkada merdivenle paylaşılan 2,4 m
    # kalır. Ölçüm de bunu söylüyor: üst kata çıkılamayan evlerin
    # ortanca eni 6,13 m ve en dar uçta yığılıyorlar — bölme, arka
    # odayı ve dolayısıyla merdiveni gezinme ağından koparıyor.
    #
    # Dönem için de doğru: RESEARCH'teki 4,12 oda ortalaması evin
    # TAMAMI içindir; arka sokağın küçük evi tek hacim + merdiven
    # sahanlığıdır, bölünmüş değil.
    if p.width < 5.8:
        return out

    # PLAN TOHUMDAN TÜRER.
    #
    # Önce yalnız enden türüyordu: aynı genişlikteki her evin içi
    # birbirinin aynısıydı. Dışarıda 201 varyant ve örnek başına ton
    # varken içeride tek bir plan olması, kapı açıldığı anda tekrarı
    # görünür kılardı.
    #
    # Tohum evin **kendi** parametrelerinden gelir (en, derinlik, kat
    # yüksekliği): aynı varyant her üretimde aynı planı taşır — bu
    # projenin her yerindeki determinizm kuralı.
    import random
    tohum = int(round(p.width * 1000) * 31 + round(p.depth * 1000) * 17
                + round(p.floor_height * 1000) * 7)
    rng = random.Random(tohum)

    if p.width >= 6.2:
        # Geniş ev: hayat ortada, iki yanında oda.
        #
        # BOLME MERDIVENDEN ONCE BITER. Ilk yazimda boydan boya
        # uzaniyordu ve merdiven kolunu kesiyordu: iki plan ayni hacmi
        # paylasamaz. Bolmenin arka ucu, merdiven kolunun on yuzunde
        # durur; arkasi merdiven sahanligidir.
        arka_sinir = p.depth * 0.5 - p.wall_thickness - MERDIVEN_EN - 0.15

        # Hayat eni: dar hayat (2,0) ile geniş sofa (2,9) arasında.
        # RESEARCH §4.1 "hayatlı" der, ölçü vermez — T2.
        hayat = HAYAT + rng.uniform(-0.10, 0.80)
        # Çok geniş evde hayat da genişler; oda 2,2 m'nin altına inmesin.
        en_cok = (p.width - 2.0 * p.wall_thickness) - 2.0 * 2.2
        hayat = min(hayat, max(2.0, en_cok))

        # Oda kapıları BAĞIMSIZ yerleşir: iki oda aynı hizada
        # açılmak zorunda değil.
        for sx in (-1.0, 1.0):
            out.append(dict(eksen="y", konum=sx * hayat * 0.5,
                            kapi=p.depth * rng.uniform(0.02, 0.22),
                            kapi_en=KAPI, kalin=KALIN, y_son=arka_sinir))

        # EK BÖLME: derin evde ön oda ikiye ayrılır.
        #
        # RESEARCH §4.1(e): ortalama **4,12 oda**. İki yan oda + hayat
        # üç hacim eder; dördüncüsü ya üst kattadır ya da derin evde
        # ön odanın bölünmesiyle. Derinlik 8 m'yi geçince bir oda
        # 8 x 2,5 m olurdu — oda değil koridor.
        if p.depth >= 8.0 and rng.random() < 0.55:
            yan = -1.0 if rng.random() < 0.5 else 1.0
            out.append(dict(eksen="x", konum=-p.depth * 0.10,
                            kapi=yan * (hayat * 0.5 + 1.1),
                            kapi_en=KAPI, kalin=KALIN,
                            x_bas=yan * hayat * 0.5,
                            x_son=yan * p.width * 0.5))
    else:
        # Dar ev: tek enine bölme. Kapı hayat aksında, yani ortada.
        out.append(dict(eksen="x", konum=p.depth * rng.uniform(0.02, 0.20),
                        kapi=rng.uniform(-0.5, 0.5),
                        kapi_en=KAPI, kalin=KALIN))
    return out


def _bolme_parcalari(p, bolme, z0, z1):
    """Bir bölmeyi kapı boşluğuyla birlikte kutu listesine çevirir.

    Dönüş: `[(ad_soneki, (sx, sy, sz), (cx, cy, cz))]`
    """
    kalin = bolme["kalin"]
    kapi_en = bolme["kapi_en"]
    yuk = z1 - z0
    cz = (z0 + z1) * 0.5
    #: Kapı boşluğunun üstü kapalı: lento hizası.
    lento = min(2.05, yuk - 0.25)

    parca = []
    if bolme["eksen"] == "y":
        # Duvar y boyunca uzanir; konum x'te.
        cx = bolme["konum"]
        boy = p.depth
        # Duvarin arka ucu: verilmisse merdivenden once biter.
        y_son = bolme.get("y_son", boy * 0.5)
        k0 = bolme["kapi"] - kapi_en * 0.5
        k1 = bolme["kapi"] + kapi_en * 0.5
        alt = k0 + boy * 0.5                    # -boy/2 referansindan uzunluk
        ust = max(0.0, y_son - k1)
        if alt > 0.05:
            parca.append(("a", (kalin, alt, yuk),
                          (cx, -boy * 0.5 + alt * 0.5, cz)))
        if ust > 0.05:
            parca.append(("b", (kalin, ust, yuk),
                          (cx, y_son - ust * 0.5, cz)))
        if yuk - lento > 0.05:
            parca.append(("l", (kalin, kapi_en, yuk - lento),
                          (cx, bolme["kapi"], z0 + (lento + yuk) * 0.5)))
    else:
        cy = bolme["konum"]
        boy = p.width
        # Kismi duvar: yalnizca x_bas..x_son arasinda uzanir. Ek oda
        # bolmesi hayatin bir yanini boler, karsi yani acik birakir.
        x_bas = bolme.get("x_bas", -boy * 0.5)
        x_son = bolme.get("x_son", boy * 0.5)
        if x_bas > x_son:
            x_bas, x_son = x_son, x_bas
        k0 = bolme["kapi"] - kapi_en * 0.5
        k1 = bolme["kapi"] + kapi_en * 0.5
        sol = max(0.0, min(k0, x_son) - x_bas)
        sag = max(0.0, x_son - max(k1, x_bas))
        if sol > 0.05:
            parca.append(("a", (sol, kalin, yuk),
                          (x_bas + sol * 0.5, cy, cz)))
        if sag > 0.05:
            parca.append(("b", (sag, kalin, yuk),
                          (x_son - sag * 0.5, cy, cz)))
        if yuk - lento > 0.05:
            parca.append(("l", (kapi_en, kalin, yuk - lento),
                          (bolme["kapi"], cy, z0 + (lento + yuk) * 0.5)))
    return parca


#: Basamak yüksekliği (m).
#:
#: 0,22 ile başladı — Osmanlı evinin merdiveni gerçekten diktir. Ama
#: ölçüm gösterdi ki o diklikte merdiven **yürünebilir bir yüzey
#: bırakmıyor**: basamak başına 1-5 hücre kalıyor ve zincir zeminden
#: kopuyordu. Dönem doğruluğu, çıkılamayan bir merdivende bir işe
#: yaramaz.
#:
#: 0,19 hâlâ dik (modern yönetmelik 0,175 ister) ama basamak yüzeyi
#: gövdeye yer bırakıyor.
BASAMAK_YUKSEK = 0.19
#: Basamak genişliği (m). Ayak boyundan geniş olmalı.
BASAMAK_DERIN = 0.30
#: Merdiven kolunun eni (m).
#:
#: Ölçülen en iyi değer 1,35. Trend, sahanlık ve duvar payı ayrı ayrı
#: ayrıldıktan SONRA ortaya çıktı:
#:
#: | kol eni | duvar payı | üst kata çıkılabilen |
#: |---|---|---|
#: | 1,10 | 0,00 | %0 |
#: | 1,10 | 0,40 | %17,1 |
#: | **1,35** | **0,50** | **%58,5** |
#: | 1,60 | 0,60 | %46,3 |
#:
#: Daha önce 1,10 → 1,40 denendiğinde kötüleşmişti (%3,9 → %0) ve
#: "kısıt en değil" diye yazmıştım. Yanlıştı: o sırada sahanlık ve
#: duvar payı yoktu, geniş kol onları yiyordu. Bir değişken tek
#: başına denendiğinde yanıltır; kısıt üçünün birleşimiydi.
MERDIVEN_EN = 1.35


def merdiven_plani(p):
    """
    Üst kata çıkan merdiven. Tek katlı evde `None`.

    **Tek sahip:** basamakların görünen hâli, çarpışma kütlesi ve
    tavandaki boşluk hep buradan türer. Üçü ayrı hesaplansaydı, biri
    ötekinden kayınca oyuncu ya havada yürür ya da görünmeyen bir
    tavana çarpardı.

    ## Merdiven arkada, bölmeler ondan önce biter

    Kol arka duvara dayalı ve **enine** (x) uzanır. Geniş evde bölme
    duvarları boyuna (y) gidiyor, yani merdivene **dik**; ilk yazımda
    bölmeler boydan boya uzanıyor ve basamakları kesiyordu — ölçüm
    "üst kata çıkılabilen %5" dedi ve sebep ölçümde değildi, iki plan
    aynı hacmi paylaşıyordu.

    Merdiveni hayatın içine almak da denendi ve daha kötüydü: hayatı
    ikiye bölünce zemin katta erişilen hacim %99,7'den %82,8'e düştü.
    Bir sorunu çözerken ötekini açmak çözüm değil.

    Doğrusu iş bölümü: **odalar önde, merdiven sahanlığı arkada.**
    Bölmeler merdiven kolunun önünde biter (bkz. `ic_bolmeler`), kol
    arka duvar boyunca serbest kalır. Osmanlı planında da merdiven
    hayatın arka ucundan çıkar.

    Dönüş: `dict(eksen, x0, x1, y0, y1, n, kosu)`.
    """
    if p.floors < 2:
        return None
    n = max(2, int(round(p.floor_height / BASAMAK_YUKSEK)))
    kosu = n * BASAMAK_DERIN

    # ARKA DUVARDA, ENINE — her iki planda da.
    #
    # SAHANLIK: kolun ucunda basacak yer BIRAKILIR.
    #
    # Once yalniz 0,30 m pay birakiliyordu ve merdivenin ucu duvara
    # dayaniyordu. Tepeden bakinca gorundu: merdiven boslugu tam kolun
    # ayak izi kadar, kolun ucuyla duvar arasi 0,8 m. Ajan yaricapi
    # (0,30 m) iki yandan yeyince sahanlik 0,2 m'ye iniyor — yani
    # merdiveni cikan kisinin basacagi yer yok. Unity'nin kendi
    # gezinme agi da tam bunu soyledi: navmesh basamaklari 2,0 m'ye
    # kadar tirmaniyor ve orada bitiyor, ust katta hic yuzey yok.
    #
    # Kolu GENISLETMEK bu yuzden kotulestirmisti (%3,9 -> %0): genis
    # kol sahanligi yiyor.
    #: Kolun yan duvardan uzakligi (m).
    #:
    #: Merdiven duvara BITISIK basliyordu ve Unity'nin gezinme agi ilk
    #: basamagi hic gormuyordu: Recast yurunebilir alani ajan yaricapi
    #: (0,30 m) kadar asindirir, basamak derinligi ise 0,26 m — yani
    #: ilk basamak butunuyle asinip yok oluyordu. Merdiven navmesh'i
    #: zemin kattan kopuk kaliyor, yol da kapidan sokaga iniyordu.
    DUVAR_PAYI = 0.60

    #: Kolun ucundaki sahanlık (m).
    #:
    #: 1,30 → 1,55 ölçümü %70,7'den %75,6'ya taşıdı. 1,80 denendi ve
    #: **aynı** sonucu verdi (%75,6, aynı evler) — yani kaldıraç 1,55'te
    #: tükeniyor. Daha büyüğü yalnız zemin kattan yer yer.
    SAHANLIK = 1.55
    ic_en = p.width - 2.0 * p.wall_thickness
    if kosu > ic_en - SAHANLIK - DUVAR_PAYI:
        kosu = ic_en - SAHANLIK - DUVAR_PAYI
    if kosu < 1.2:
        return None

    # BASAMAK DERINLIGINE ALT SINIR.
    #
    # Kosu kirpilinca basamak inceliyor ve merdiven dikleşiyor. Olculdu:
    # ust kata cikilan evlerin ortanca eni 7,50 m, cikilamayanlarin
    # 6,55 m — yani dar evde kirpilan kosu basamagi 0,30 m'nin altina
    # indiriyor. Basamak sayisini azaltmak, yani riht yuksekligini
    # artirmak, ayni kotu daha az ve daha genis basamakla cikar.
    #
    # Riht ust siniri 0,28 m: bunun ustu insanin adiminin degil
    # tirmanmanin isi olurdu ve gezinme agi da (adim payi 0,30 m)
    # sinirda kalirdi.
    EN_AZ_BASAMAK = 0.32
    EN_COK_RIHT = 0.28
    while n > 2 and kosu / n < EN_AZ_BASAMAK:
        if p.floor_height / (n - 1) > EN_COK_RIHT:
            break
        n -= 1
    y1 = p.depth * 0.5 - p.wall_thickness
    y0 = y1 - MERDIVEN_EN
    x0 = -ic_en * 0.5 + DUVAR_PAYI
    return dict(eksen="x", x0=x0, x1=x0 + kosu, y0=y0, y1=y1,
                n=n, kosu=kosu)


def _merdiven_parcalari(m, p, z0):
    """Basamakları kutu listesine çevirir: `[(sonek, boyut, merkez)]`.

    Rıhtlı merdiven: her basamak tabandan kendi kotuna kadar dolu bir
    kutudur. İç içe geçen kutular birleşince tek kütle olur ve altında
    boşluk kalmaz — oyuncunun basamağın içine düşmesi mümkün değil.
    """
    out = []
    derin = m["kosu"] / m["n"]
    yuk = p.floor_height / m["n"]
    for i in range(m["n"]):
        h = (i + 1) * yuk
        if m["eksen"] == "x":
            cx = m["x0"] + (i + 0.5) * derin
            cy = (m["y0"] + m["y1"]) * 0.5
            boyut = (derin, m["y1"] - m["y0"], h)
        else:
            # Boyuna kol: basamaklar ondan ARKAYA dogru yukselir,
            # yani cikis arka duvarda biter ve hayatin onu acik kalir.
            cx = (m["x0"] + m["x1"]) * 0.5
            cy = m["y1"] - (i + 0.5) * derin
            boyut = (m["x1"] - m["x0"], derin, h)
        out.append((f"b{i:02d}", boyut, (cx, cy, z0 + h * 0.5)))
    return out


def _tavan_parcalari(w, d, cy, kalin, cz, bosluk):
    """Döşemeyi (varsa) merdiven boşluğunu bırakarak kutulara böler."""
    if bosluk is None:
        return [("", (w, d, kalin), (0.0, cy, cz))]
    bx0, bx1 = bosluk["x0"], bosluk["x1"]
    by0, by1 = bosluk["y0"] + cy, bosluk["y1"] + cy
    y0, y1 = cy - d * 0.5, cy + d * 0.5
    x0, x1 = -w * 0.5, w * 0.5
    out = []

    def ekle(ad, ax0, ax1, ay0, ay1):
        if ax1 - ax0 > 0.02 and ay1 - ay0 > 0.02:
            out.append((ad, (ax1 - ax0, ay1 - ay0, kalin),
                        ((ax0 + ax1) * 0.5, (ay0 + ay1) * 0.5, cz)))

    ekle("on", x0, x1, y0, by0)
    ekle("arka", x0, x1, by1, y1)
    ekle("sol", x0, bx0, by0, by1)
    ekle("sag", bx1, x1, by0, by1)
    return out


def _ic_bolme_geometri(p, mats, col, asset_name, level_z):
    """Bölme duvarlarının görünen hâli. Plan: :func:`ic_bolmeler`.

    Yalnızca `near` kipinde kurulur, yani yalnız LOD0'da. Bu, planın
    "iç mekân 40 m içinde" bütçesini ayrı bir çalışma zamanı sistemi
    yazmadan verir: LOD zaten mesafeye göre eliyor. Uzaktaki ev
    bölmesini taşımaz çünkü LOD1'e düşmüştür.
    """
    z0 = level_z
    z1 = level_z + p.floor_height
    out = []
    for i, b in enumerate(ic_bolmeler(p)):
        for son, boyut, merkez in _bolme_parcalari(p, b, z0, z1):
            o = hz.make_box(f"{asset_name}_Bolme{i}{son}", boyut, merkez, col)
            hz.assign(o, mats["plaster"])
            out.append(o)
    return out


def _mobilya(p, mats, col, asset_name, level_z):
    """
    Zemin katın döşenmesi. Parçalar: :mod:`mobilya_kit`.

    ## Neden duvar boyunca

    Osmanlı odasında mobilya ortada durmaz; **duvara yaslanır** ve
    orta boş kalır — oturulan yer sedirdir, sofra yere serilir. Bu
    yalnız bir yerleşim tercihi değil, odanın nasıl kullanıldığının
    kendisi: aynı hacim gündüz oturma, gece yatak odası olur ve bunu
    ancak ortası boş bir oda yapabilir (yatak takımı yüklüğe girer).

    ## Neden LOD0

    Bölme duvarlarıyla aynı sebep: `near` kipinde kurulur, yani
    yalnız yakındaki evde çizilir. Planın "iç mekân 40 m içinde"
    bütçesini ayrı bir çalışma zamanı sistemi yazmadan verir.

    ## Tohum

    Plan hangi tohumdan türüyorsa döşeme de ondan (`ic_bolmeler`).
    Aynı ev her açılışta aynı şekilde döşenir.
    """
    import random
    tohum = int(round(p.width * 1000) * 31 + round(p.depth * 1000) * 17
                + round(p.floor_height * 1000) * 7)
    rng = random.Random(tohum + 4242)

    t = p.wall_thickness
    ic_w = p.width - 2.0 * t
    ic_d = p.depth - 2.0 * t
    z = level_z
    out = []
    n = 0

    def ad(kok):
        return f"{asset_name}_Mob_{kok}{n:02d}"

    # --- OCAK: arka duvarda, merdiven kolunun karşı ucunda -----------
    # Ocak bacaya bağlıdır ve baca duvardadır; ortada ocak olmaz.
    m = merdiven_plani(p)
    ocak_x = (-ic_w * 0.5 + 0.9) if (m is None or m["x0"] > 0) else (ic_w * 0.5 - 0.9)
    out += mob.ocak(ad("Ocak"), col, mats,
                    (ocak_x, ic_d * 0.5 - mob.YUKLUK_DERIN * 0.5, z),
                    (0.0, 1.0), p.floor_height)
    n += 1

    # --- SEDIR: yan duvar boyunca, cephe tarafında --------------------
    # Sokağa bakan pencerenin altı: oturan kişi sokağı görür. Kafes
    # pencerenin varlık sebebi de budur.
    sedir_boy = min(ic_d * 0.55, 2.6)
    for sx in (-1.0, 1.0):
        if rng.random() > 0.75:
            continue
        out += mob.sedir(ad("Sedir"), col, mats, sedir_boy,
                         (sx * (ic_w * 0.5 - mob.SEDIR_DERIN * 0.5),
                          -ic_d * 0.5 + sedir_boy * 0.5 + 0.3, z),
                         (sx, 0.0))
        n += 1

    # --- YÜKLÜK: arka duvar, ocağın karşı yanı ------------------------
    out += mob.yukluk(ad("Yukluk"), col, mats,
                      (-ocak_x, ic_d * 0.5 - mob.YUKLUK_DERIN * 0.5, z),
                      min(1.8, ic_w * 0.35), (0.0, 1.0), p.floor_height)
    n += 1

    # --- KİLİM: odanın ortası. Mobilyanın yokluğunu görünür kılan şey.
    kil_w = min(ic_w * 0.5, 2.6)
    kil_d = min(ic_d * 0.42, 3.2)
    out += mob.kilim(ad("Kilim"), col, mats, (0.0, -ic_d * 0.10, z),
                     (kil_w, kil_d))
    n += 1

    # --- SANDIK, RAHLE, MANGAL: tohuma göre --------------------------
    if rng.random() < 0.8:
        out += mob.sandik(ad("Sandik"), col, mats,
                          (rng.uniform(-0.3, 0.3) + ic_w * 0.25,
                           ic_d * 0.5 - 0.75, z))
        n += 1
    if rng.random() < 0.55:
        out += mob.rahle(ad("Rahle"), col, mats,
                         (rng.uniform(-0.6, 0.6), -ic_d * 0.10, z))
        n += 1
    if rng.random() < 0.65:
        out += mob.mangal(ad("Mangal"), col, mats,
                          (rng.uniform(-0.5, 0.5), -ic_d * 0.05, z))
        n += 1
    return out


def _mobilya_kutulari(p, asset_name, level_z):
    """Çarpışacak mobilya kutuları — görünen döşemeyle **aynı plandan**.

    `_mobilya` geometriyi kurar; bu, aynı yerleşimi kutu listesi olarak
    döndürür. İkisi ayrı hesaplansaydı oyuncu görünmeyen bir sedire
    çarpardı — bu oturumda tam bu türden üç kusur ölçüldü.

    Kilim ve mangal DIŞARIDA: biri yerde 1 cm, öteki küçük ve odanın
    ortasında; ikisini de çarpıştırmak dolaşımı gereksiz daraltırdı.
    """
    import random
    tohum = int(round(p.width * 1000) * 31 + round(p.depth * 1000) * 17
                + round(p.floor_height * 1000) * 7)
    rng = random.Random(tohum + 4242)

    t = p.wall_thickness
    ic_w = p.width - 2.0 * t
    ic_d = p.depth - 2.0 * t
    out = []
    m = merdiven_plani(p)
    ocak_x = (-ic_w * 0.5 + 0.9) if (m is None or m["x0"] > 0) else (ic_w * 0.5 - 0.9)

    # ARKA DUVARDAKI MOBILYA CARPISMAZ.
    #
    # Ocak, yukluk ve sandik arka duvarda durur — merdivenin de
    # bulundugu duvarda. Hepsine carpisma verilince ust kata cikilabilen
    # %75,6'dan **%56,1'e** dustu: mobilya merdivenin onunu kapatiyor.
    #
    # Secim acik: bir sedirin icinden gecmek kotu, ama merdiveni
    # kapatmak daha kotu. Yan duvardaki sedir carpisir (dolasima
    # engel degil), arka duvardakiler gorunur ama gecirir.
    # Sedirler — geometriyle AYNI kur'a sirasi
    sedir_boy = min(ic_d * 0.55, 2.6)
    for sx in (-1.0, 1.0):
        if rng.random() > 0.75:
            continue
        out.append((f"{asset_name}_c_sedir",
                    (0.70, sedir_boy, 0.50),
                    (sx * (ic_w * 0.5 - 0.35),
                     -ic_d * 0.5 + sedir_boy * 0.5 + 0.3,
                     level_z + 0.25)))
    return out


def _carpisma(p, col, mats, asset_name, total_h):
    """
    Çarpışma kütlesi: **zemin katı boş, üstü dolu.**

    ## Neden hiç girilemiyordu

    Collider tek bir dolu kutuydu. Cephe `near` kipinde zaten delikti —
    kapı gerçek bir açıklıktı, sövesi ve nişi vardı — ama oyuncu o
    açıklıktan geçemiyordu, çünkü fizik evi katı bir blok sanıyordu.
    Caner'in isteği (*"evlerin içi de erişilebilir olsun"*) bir geometri
    işi gibi görünüyor; asıl engel fizikteydi.

    ## Neden yalnız zemin kat boşaltılıyor

    Üst katları da boşaltmak, süzülen oyuncunun evin **içinden geçmesine**
    izin verirdi. Uçuş çarpışması bu oyunun yarısı. Zemin kat girilir,
    üstü tek kütle kalır: iki gereksinim de karşılanır ve collider yedi
    kutuya çıkar, yüzlerceye değil.

    ## Kapı boşluğu ölçüden gelir

    Kapının eni ve yeri `_opening_layout`'tan **okunur**, burada yeniden
    hesaplanmaz. İki yerde iki formül olsaydı, cephedeki delikle
    collider'daki boşluk er ya da geç birbirinden kayardı — ve o kayma
    ancak oyuncu görünmez bir duvara toslayınca fark edilirdi.
    """
    t = max(p.wall_thickness, 0.25)
    w, d = p.width, p.depth
    # Ust katin ayak izi: cumba varsa buyur. build_house ile AYNI
    # formul; ikisi ayrilirsa collider cumbanin altini bos birakir.
    cumba_d = 0.0 if p.cumba_type == "none" else p.cumba
    yan = 0.0 if p.cumba_type == "none" else p.jetty_side
    if p.cumba_type == "corner":
        yan = max(yan, p.cumba * 0.6)
    ust_w = p.width + 2.0 * yan
    ust_d = p.depth + cumba_d
    ust_cy = -cumba_d * 0.5
    z0 = p.plinth                      # zemin katin dosemesi
    z1 = z0 + p.floor_height           # tavani
    ust = total_h * 0.98

    kapi = None
    for o in _opening_layout(p, w * 0.86, True):
        if o["kind"] == "door":
            kapi = o
            break

    kutular = []

    def kutu(ad, boyut, merkez):
        kutular.append(hz.make_box(ad, boyut, merkez, col))

    # 1) Subasman: dolu. Zemin dosemesi de budur.
    kutu(f"{asset_name}_c_taban", (w, d, z0), (0.0, 0.0, z0 * 0.5))

    # 2) On duvar: kapinin iki yani. Bosluk DOSEMEDEN baslar; esik
    #    yalnizca 14 cm ve karakter denetleyicisi onu adim olarak gecer.
    #    Boslugu esikten baslatmak, gorunmeyen 14 cm'lik bir bariyer
    #    birakirdi.
    yon = -d * 0.5 + t * 0.5
    if kapi is None:
        kutu(f"{asset_name}_c_on", (w, t, z1 - z0), (0.0, yon, (z0 + z1) * 0.5))
    else:
        gk = kapi["w"] + 0.10          # 5 cm pay, iki yanda
        sol_en = (w - gk) * 0.5 + kapi["cu"]
        sag_en = (w - gk) * 0.5 - kapi["cu"]
        if sol_en > 0.05:
            kutu(f"{asset_name}_c_on_sol", (sol_en, t, z1 - z0),
                 (-w * 0.5 + sol_en * 0.5, yon, (z0 + z1) * 0.5))
        if sag_en > 0.05:
            kutu(f"{asset_name}_c_on_sag", (sag_en, t, z1 - z0),
                 (w * 0.5 - sag_en * 0.5, yon, (z0 + z1) * 0.5))
        # Kapinin USTU kapali: lento hizasindan tavana kadar duvar var.
        lento = z0 + kapi["v1"]
        if z1 - lento > 0.05:
            kutu(f"{asset_name}_c_on_lento", (gk, t, z1 - lento),
                 (kapi["cu"], yon, (lento + z1) * 0.5))

    # 3) Arka ve yan duvarlar: tam.
    kutu(f"{asset_name}_c_arka", (w, t, z1 - z0),
         (0.0, d * 0.5 - t * 0.5, (z0 + z1) * 0.5))
    for sx in (-1.0, 1.0):
        kutu(f"{asset_name}_c_yan", (t, d - 2.0 * t, z1 - z0),
             (sx * (w * 0.5 - t * 0.5), 0.0, (z0 + z1) * 0.5))

    # 4) IC BOLMELER — gorunen geometriyle AYNI listeden.
    for i, b in enumerate(ic_bolmeler(p)):
        for son, boyut, merkez in _bolme_parcalari(p, b, z0, z1):
            kutu(f"{asset_name}_c_bolme{i}{son}", boyut, merkez)

    # 5) MERDIVEN — gorunen basamaklarla AYNI plandan.
    m = merdiven_plani(p)
    if m is not None:
        for son, boyut, merkez in _merdiven_parcalari(m, p, z0):
            kutu(f"{asset_name}_c_mrd_{son}", boyut, merkez)

    # 5a) MOBILYA CARPISMAZ — ve bu bilincli bir odun.
    #
    # Sedirin icinden gecmek kotu. Ama olculdu: mobilyaya carpisma
    # verince ust kata cikilabilen **%75,6'dan %56,1'e** dustu; yalniz
    # yan duvardaki sedire verince **%61,0**. Arka duvardaki ocak ve
    # yukluk merdivenin onunu, yandaki sedir de merdivenin duvar payini
    # yiyor.
    #
    # Iki kusurdan birini secmek gerekiyorsa, gecilemeyen bir merdiven
    # gecilebilen bir sedirden kotudur. Mobilya gorunur, gecirir; kayit
    # ADR 0081'de.
    #
    # Kalici cozum mobilyayi merdiven ve kapi hatlarina gore
    # yerlestirmek — yani `_mobilya`nin yerlesim kurallarini `merdiven_plani`
    # ile konusturmak. O, Faz II.D'nin ikinci turudur.

    # 5b) ZEMIN KATIN TAVANI — merdiven bosluguyla birlikte.
    #
    # Bu atlanmisti ve kesit acikca gosterdi: collider'da zemin katla
    # ust kat arasinda HICBIR SEY yoktu, y=1,10'dan 5,35'e kadar
    # kesintisiz bosluk. Gorunen geometride doseme vardi. Yani ust kat
    # bakilinca vardi, basilinca yoktu.
    #
    # Bolme ve merdivende uygulanan kural burada da gecerli: doseme
    # parcalari gorunen tavanla AYNI fonksiyondan (`_tavan_parcalari`)
    # ve AYNI bosluktan turer.
    for son, boyut, merkez in _tavan_parcalari(w, d, 0.0, 0.20,
                                               z1 - 0.10, m):
        kutu(f"{asset_name}_c_tavan{son}", boyut, merkez)

    # 6) UST KATLAR DA BOSALTILIR.
    #
    # Ilk yazimda ust kutle tek dolu blok, icinden yalniz merdiven
    # boslugu geciyordu. Yani oyuncu basamaklari cikip **kati bir
    # blogun icindeki bir safta** sikisirdi — merdivensiz halinden
    # daha kotusu. Kat, cikilabiliyorsa yasanabilir de olmali.
    #
    # Ucus carpismasi bundan zarar gormuyor: dis duvarlar duruyor,
    # bosalan sey yalniz onlarin arasi. Suzulen oyuncu eve pencereden
    # girebilir — bu bir kusur degil, `near` kipinde pencerenin
    # gercekten delik olmasinin sonucu.
    z_alt = z1
    for kat in range(1, p.floors):
        z_ust_kat = min(ust, z_alt + p.floor_height)
        if z_ust_kat - z_alt < 0.3:
            break
        # Son kat cumbali olabilir: kendi ayak izini kullanir.
        son_kat = (kat == p.floors - 1)
        kw = ust_w if son_kat else w
        kd = ust_d if son_kat else d
        kcy = ust_cy if son_kat else 0.0

        # Duvarlar
        kutu(f"{asset_name}_c_k{kat}_on", (kw, t, z_ust_kat - z_alt),
             (0.0, kcy - kd * 0.5 + t * 0.5, (z_alt + z_ust_kat) * 0.5))
        kutu(f"{asset_name}_c_k{kat}_arka", (kw, t, z_ust_kat - z_alt),
             (0.0, kcy + kd * 0.5 - t * 0.5, (z_alt + z_ust_kat) * 0.5))
        for sx in (-1.0, 1.0):
            kutu(f"{asset_name}_c_k{kat}_yan", (t, kd - 2.0 * t,
                                                z_ust_kat - z_alt),
                 (sx * (kw * 0.5 - t * 0.5), kcy,
                  (z_alt + z_ust_kat) * 0.5))
        # Tavan/doseme — bu katin ustu. Merdiven boslugu yalnizca
        # ZEMIN katin tavaninda; ust katlar arasi bosluk yok (tek
        # merdiven kolu var).
        if z_ust_kat < ust - 0.05:
            kutu(f"{asset_name}_c_k{kat}_doseme", (kw, kd, 0.20),
                 (0.0, kcy, z_ust_kat - 0.10))
        z_alt = z_ust_kat

    # 7) Cati kutlesi: son katin ustunden tepeye kadar DOLU.
    if ust > z_alt + 0.05:
        kutu(f"{asset_name}_c_cati", (ust_w, ust_d, ust - z_alt),
             (0.0, ust_cy, (z_alt + ust) * 0.5))

    ucx = hz.join(kutular, f"UCXB_{asset_name}", col)
    # Parcalar birlestirmeden SONRA sahnede kaliyor ve kalirsa ihrac
    # edilirler: ev basina sekiz gereksiz mesh. LOD kodu ayni sebeple
    # `_purge` cagiriyor; burada da cagrilmali.
    _purge(kutular)
    hz.assign(ucx, mats["stone"])
    return ucx


# ------------------------------------------------------------------ yardımcı

def join_parts(parts, name, col):
    """
    Parçaları tek mesh'te birleştirir ve ara parçaları temizler.

    Kit dışından da kullanılır (`mosque_kit`): birleştirme + temizlik her zaman
    birlikte yapılmalı, yoksa ara parçalar sahnede kalır ve FBX'e sızar.
    """
    _donus_denetimi(parts, name)
    obj = hz.join(parts, name, col)
    _purge(parts)
    return obj


#: `_donus_denetimi` için eşik (m) — dönüşün mesh merkezini **kaçırdığı**
#: mesafe. Merkezin orijine uzaklığı değil: bir kutu merkezi dönüş
#: EKSENİ üzerindeyse (ör. z=7,5'te duran ve yalnız Z ekseninde dönen bir
#: duvar) dönüş onu hiç kıpırdatmaz ve bu meşrudur. İlk yazdığım bekçi
#: uzaklığa bakıyordu ve iki meşru parçayı suçladı.
ROT_TOLERANS = 0.35


def _donus_denetimi(parts, name):
    """
    **Yerine koyup sonra döndürme** tuzağına karşı bekçi.

    `hz.make_box` (ve kardeşleri) köşe koordinatlarını doğrudan **mesh
    verisine** yazar; nesne dönüşümü kimliktir. Bu bilinçli bir karardır —
    "uygulanmamış ölçek" diye bir durum hiç doğmaz. Ama sinsi bir sonucu
    var: kutuyu yerine koyup **sonra** `rotation_euler` vermek, onu kendi
    merkezi etrafında değil **dünya orijini** etrafında döndürür.

    Bu hata iki kez yazıldı ve ikisi de renderda **görüldüğü halde yanlış
    teşhis edildi** (bkz. ADR 0057). Sonunda onu bir sayı ele verdi:
    Yedikule'nin ayak izi 7×13 m büyümüştü.

    Bekçi **dönüşün merkezi ne kadar kaçırdığını** ölçer (`|R·c − c|`),
    merkezin orijine uzaklığını değil. Fark önemli: dönüş ekseni üzerinde
    duran bir merkez hiç kıpırdamaz ve o meşrudur — ilk yazdığım bekçi
    uzaklığa bakıyordu ve iki masum parçayı (Yedikule beden duvarı,
    Mihrimah sundurması) suçladı. Bir bekçinin kendisi de ölçülmeli.

    Doğru sıra: **orijinde kur → döndür → yerine taşı.**
    """
    for o in parts:
        try:
            rot = o.rotation_euler
            verts = o.data.vertices
        except AttributeError:
            continue
        if abs(rot[0]) < 1e-6 and abs(rot[1]) < 1e-6 and abs(rot[2]) < 1e-6:
            continue
        if not verts:
            continue
        n = len(verts)
        c = Vector((sum(v.co[0] for v in verts) / n,
                    sum(v.co[1] for v in verts) / n,
                    sum(v.co[2] for v in verts) / n))
        d = (rot.to_matrix() @ c - c).length
        if d > ROT_TOLERANS:
            raise ValueError(
                f"{name}: '{o.name}' donunce mesh merkezi {d:.2f} m "
                f"kayiyor (sinir {ROT_TOLERANS}). Bu parca kendi merkezi "
                "etrafinda degil DUNYA ORIJINI etrafinda donuyor. Once "
                "orijinde kur, sonra dondur, en son yerine tasi "
                "(detay_kit.donuk_kutu).")


def tri_count(obj):
    """Üçgen sayısı — inceleme ve testlerin okuduğu ölçü."""
    return _tris(obj)


def _solid(name, size, center, col, mat):
    obj = hz.make_box(name, size, center, col)
    hz.assign(obj, mat)
    return obj


def _tris(obj):
    """Üçgen sayısı — n köşeli yüz (n−2) üçgendir."""
    return sum(max(0, len(f.vertices) - 2) for f in obj.data.polygons)



# ------------------------------------------------------------- orta kademe

#: Orta kademenin bölüt çarpanı ve ayrıntı alt sınırı (m).
MID_DETAIL, MID_MIN = 0.5, 0.55


def build_with_mid_lod(build_fn, p, col, asset_name, textured=False,
                       detail=MID_DETAIL, min_size=MID_MIN):
    """
    Yapıyı **iki kez** kurar: tam ayrıntı ve orta kademe.

    ## Neden

    Ayrıntı geçişi LOD0'ı altı katına çıkardı, LOD1'e dokunmadı ve arada
    kalan boşluk ölçüldü: Süleymaniye'nin LOD0'ı yalnızca **573 m**'ye kadar
    görüntüleniyor (LODGroup eşiği 0,25 ekran yüksekliği, FOV 40°),
    ötesinde 456 üçgenlik blok geliyor. **Hezarfen'in uçuşu 3336 m.** Yani
    ayrıntının tamamı, oyunun merkez sahnesinde hiç görünmüyordu — ve
    LOD0'dan LOD1'e geçiş tek adımda **197 kat** düşüyordu.

    ## Neden filtreleyerek değil, yeniden kurarak

    Ölçüldü: 4 m'nin altındaki her parça atılsa bile üçgenlerin **%33'ü**
    kalıyor, çünkü yük küçük süslerde değil **çok bölütlü kubbelerde ve
    kütlelerdedir**. Sonradan filtrelemek ya da decimate etmek bu yüzden
    işe yaramaz; orta kademe aynı üreteçten daha az bölütle kurulmalı.

    Kademeyi taşıyan şey `hz.set_detail`: eğri ilkellerin bölütlerini
    ölçekler, `detay_kit`in gölge-dokusu öğelerini (mukarnas hücresi, kubbe
    kaburgası, konsol dizisi, silme basamağı) eşiğin altında düşürür.
    Siluete giren hiçbir şey düşmez.

    Adlandırma kayar: eldeki LOD1 **LOD2** olur, yeni orta kademe **LOD1**.
    Unity LODGroup'u `_LOD0/_LOD1/_LOD2` adlarından kendisi kurar.
    """
    lod0, lod1, ucx, info = build_fn(p, col, asset_name, textured=textured)

    lod0.name = f"SM_{asset_name}_LOD0"
    lod1.name = f"SM_{asset_name}_LOD2"

    tmp = hz.collection("_MidLOD")
    hz.set_detail(detail, min_size)
    try:
        m0, m1, mucx, _ = build_fn(p, tmp, asset_name, textured=textured)
    finally:
        hz.set_detail(1.0, 0.0)

    # Orta kademeden YALNIZCA LOD0 kalir; digerlerinin ikizi zaten var.
    _purge([o for o in (m1, mucx) if o is not None])
    for c in list(m0.users_collection):
        c.objects.unlink(m0)
    col.objects.link(m0)
    m0.name = f"SM_{asset_name}_LOD1"

    for o in list(tmp.objects):
        _purge([o])
    bpy.data.collections.remove(tmp)

    info["tris_lod1"] = tri_count(m0)
    info["tris_lod2"] = tri_count(lod1)
    info["lod_detail"] = detail
    return lod0, m0, lod1, ucx, info

def _purge(objects):
    """Ara parçaları sahneden ve veriden temizler — FBX'e sızmasınlar."""
    for obj in objects:
        me = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if me.users == 0:
            bpy.data.meshes.remove(me)
