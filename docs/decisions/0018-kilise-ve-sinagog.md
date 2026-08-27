# ADR 0018 — Kilise ve sinagog: tek yapı değil, üç tip

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — üretildi, prefablandı, Galata ve Balat sahnelerine yerleşti; Karar 5 cevaplandı (haç yok)
**Tetikleyen:** ADR 0017'nin kendi tespiti: *"Kilise/sinagog eksikliği Galata ve
Balat için doğrudan engeldir."*
**İlgili:** RESEARCH.md §4.2 (yeni), PLAN.md §7.1, ADR 0016, ADR 0017

---

## 1. Karar

Tek bir "kilise" varlığı üretilmedi. **Üç tip** üretildi, çünkü 1632
İstanbul'unda **iki ayrı hukukî durum iki ayrı mimarî** doğurur ve fark
siluetten okunur:

| Tip | Nerede | İmzası |
|---|---|---|
| `kilise_latin` | **Galata** | üç nefli bazilika, orta nef yükseltisi, **kare çan kulesi** |
| `kilise_orthodox` | Suriçi, Fener, Balat, Kumkapı | üç nef **tek beşik çatı** altında, **kulesiz**, alçak |
| `sinagog` | Balat, Hasköy | dikdörtgen salon, **kemersiz**, avlu içinde |

Gerekçe ve kaynaklar RESEARCH.md §4.2'de. Kısaca: Galata 1453'te
**antlaşmayla** teslim oldu, Latin kiliseleri biçimlerini korudu (San Domenico
= bugünkü Arap Camii, kulesi minareye çevrildi). Suriçinde zimmî kısıtı işler.
Sinagogun "kendine özgü bir tipolojisi yoktur" — yüksek duvarlı avlu içinde,
sokaktan **ev gibi** okunur.

`KiliseParams.validate()` bunu **zorlar**: `kind="orthodox"` iken kule
istenirse hata verir. Dönem hatası bir yorum satırıyla değil, çalışmayan bir
kodla engellenir.

## 2. Bilinçli boşluk: yarı gömük zemin

Kaynaklar zimmî kiliselerinin kısıtı aşmak için "ahşap ve yarı gömük"
kaldığını söyler — iç yükseklik zemin kazılarak kazanılır.

Bu bir **iç mekân** özelliğidir. İç mekânlar henüz yok ve arazide çukur açmak
taş kaide sistemiyle (ADR 0016, Kural 8) doğrudan çelişir: kaide dolgu yapar,
kazı yapmaz. Bu yüzden yalnızca **dış sonucu** modellendi — planına göre fazla
alçak duran gövde (25 m uzunluğunda bir bazilikada 5,6 m duvar). `sink` alanı
katalogda **bilgi olarak** taşınır, geometriye girmez.

İç mekânlar geldiğinde bu alan hazır bekliyor olacak.

## 3. Ölçülen ve düzeltilenler

### 3.1 Kiremitten alınlık

Beşik çatı önce **dolu prizma** olarak üretildi. Prizmanın iki ucu düşey
üçgendir ve kiremit malzemesi alır: cephe **kiremitten bir alınlıkla** çıktı.
Beşik çatının alınlığı **duvardır**; çatı yalnızca onun üstünden aşar. Çözüm
ikiye ayırmaktı — `_gable_wall` (kâgir üçgen) + `gable_roof` (iki eğimli levha).

### 3.2 Bütünüyle boş cephe

İlk üretimde 15 m genişliğindeki cephede tek bir kapı vardı, başka hiçbir şey
yoktu. Bazilikanın cephesi **iki sıradır**: altta taç kapı, üstte pencere.
Tek panelde ikisi olamaz (`arched_panel` bir panelde tek basma kotu kabul eder
— gerekçesi orada), bu yüzden cephe iki panele bölündü. Bölmenin mimarî
karşılığı da doğrudur. Yükseklik yetmeyen küçük mahalle kilisesinde üst sıra
düşer; gerçekten sade cepheye sahiptirler.

### 3.3 Haki çatı — palet kaymasi

Ölçüm: varsayılan palet çatısı **R/G 1,82, doygunluk 0,69** (kil kırmızısı);
gayrimüslim palet çatısı **R/G 1,24, doygunluk 0,41** — hakiye çalan bir ton.
İki palet farklı Poly Haven varlığı kullanıyordu ve gayrimüslim olanına boya
uygulanmıyordu.

Bu **uydurma bir ayrım**dı. Kesme taş için zaten yazılı olan ilke burada da
geçerlidir: *"taşın kendisi mahalleye göre değişmez."* Kiremit de aynı ocaktan
çıkar; boya kısıtı zimmînin **duvarına** konur, çatısına değil. Doku deseni
farklı bırakıldı (çeşitlilik iyidir), rengi COLOR karışımıyla kil ailesine
çekildi. **Bu düzeltme fazla gitti — bkz. §10.2.**

### 3.4 Kilise nereye oturur — üç yanlış kural

Bu, ADR 0016 §6.2'deki "mahalle çekirdeği sokağın en düz yerine kurulur"
kuralının 30 metrelik hâliydi ve **üç kez** yanlış çözdüm:

| Deneme | Kural | Ölçüm |
|---|---|---|
| 1 | "cepheni sokağa dön" (ev/mescit kuralı) | **5,60 m** kot farkı |
| 2 | apsis doğuya (yön mimarîden gelir) | 5,22 m |
| 3 | + sokaktan içeri çekilebilir, en düzü seç | 2,22 m ama **45 m yarıçapta 0 ev** |
| 4 | + düzlük bir **eşik**tir, eşiği geçenler arasında en yakın kazanır | 2,50 m, hâlâ 38 m geride |
| **5** | **+ cemaat arazinin kaldırdığı büyüklükte kilise yapar** | **2,58 m, 12 m geride, 45 m'de 21 ev** |

Öğrenilen üç şey:

**(a) Yön mimarîden gelir, topoğrafyadan değil.** Sokak eş yükselti eğrisini
izler; sokağa **dik** yön yamacın **en dik** yönüdür. 29 m derinliğindeki bir
bazilikayı oraya dikmek olabilecek en kötü seçimdi. Hristiyan kilisesi zaten
**apsisi doğuya** bakacak şekilde kurulur — yön sabittir, tarama yalnızca
konumu arar. (Arazi gerçek UTM'dir, +X doğudur.)

**(b) Düzlük bir eşiktir, ölçek değil.** "Kot farkı + geri çekilme × katsayı"
biçiminde ağırlıklı bir puan denendi ve kilise 38 m içeri kaçıp boş yamaçta
kaldı. Gerçek gereklilik şudur: 5 m'lik istinat duvarı kale gibi görünür,
3 m'lik görünmez. Eşiği geçen adaylar arasında **sokağa en yakın** olan kazanır.

**(c) Asıl hata yapı seçiminin sabit olmasıydı.** Bütün mahalle kutusu
tarandığında 29 × 20 m'lik ayak izi için **0,20 m**'lik yerler var — ama hiçbiri
sokağın yanında değil. "Dokunun içinde kal" ile "düz zemin" burada çelişir.
Çözüm yerleştirmeyi değil **seçimi** esnetmekti: kiliseler büyükten küçüğe
denenir, arazinin kaldırdığı ilk boy kazanır. Mimarlık tarihinde de olan budur.

## 4. Yan kazanç: HistoricalTag artık katalogdan geliyor

Bu turda 40 prefabın **hepsinin** `Graybox` etiketli olduğu fark edildi —
CLAUDE.md'nin *"her sahne öğesine HistoricalTag ata"* kuralı kâğıt üstünde
kalmıştı. Sebep: `ImportLanding` prefabı **her koşuşta sıfırdan yazar**; elle
konan etiket ilk yeniden üretimde sessizce kaybolur.

Çözüm: kademe ve kaynak notu, biçimi üreten scriptin yanında — **katalogda**
durur (`art/blend/**/catalog.json`). Unity yalnızca okur (`AssetCatalog`).
Karşılığı olmayan model Graybox kalır ama **loglanır**: sessizce doğru
görünmesindense gürültüyle eksik görünmesi yeğdir.

Sonuç: **38/40** prefab T2 (`Reconstruction`) + kaynak notlu. Kalan ikisi
(`PF_BoxHouse`, `PF_House_A`) gerçek Faz 1 graybox prototipleridir.

Test `CataloguedPrefabs_CarryTheirHistoricalTier` bunu kilitler ve dişi
vardır: yalnızca "Graybox değil" demez, **kaynak notunun dolu** olmasını da
arar — boş notlu bir T2 etiketi, iddiayı doğrulanabilir kılmadığı için
etiketsizden farksızdır.

## 5. Üretilenler

| Varlık | Ayak izi (m) | Yükseklik | LOD0 üçgen |
|---|---|---|---|
| `Kilise_Latin_A` | 19,77 × 29,37 | 19,46 | 6 852 |
| `Kilise_Latin_B` | 14,40 × 21,80 | 11,59 | 4 844 |
| `Kilise_Rum_A` | 15,20 × 25,09 | 9,55 | 3 692 |
| `Kilise_Rum_B` | 12,80 × 17,62 | 8,33 | 2 226 |
| `Sinagog_A` | 12,90 × 17,36 | 10,47 | 1 772 |
| `Sinagog_B` | 10,40 × 13,36 | 8,32 | 716 |

İnceleme paketleri: `renders/review/Kilise_Latin_A_v3/`,
`Kilise_Rum_A_v3/`, `Sinagog_A_v2/`.
Sahneler: `Captures/faz2_galata_kilise_1.png`, `Captures/faz2_balat_sinagog_2.png`.

## 6. Ortak katmana taşınanlar

- **`street_kit.arched_panel`** — sivri kemerli gerçek açıklıkları olan duvar
  paneli, `hz.make_wall_panel` ile aynı eksen sözleşmesinde. Çeşme nişi, avlu
  kapısı, kilise penceresi ve çan kulesi açıklığı artık **tek kemer kodundan**
  çıkıyor. Kısıtı: bir panelde bütün açıklıklar aynı ölçüde olmalı — sebebi
  T-kavşağı, mimarî karşılığı revağın **ritim** olması.
- **`street_kit.iron_grille`** — mescitten taşındı. Demir işçiliği mahalleye
  aittir, cemaate değil: mescit, kilise ve sinagog aynı şebekeyi kullanır.
- **`hz.ensure_outward`** — işaretli hacim denetimi tek yerde. Ağırlık
  merkezine göre toplanır; kabuk orijinden uzaktaysa ham koordinatla toplam
  iki büyük sayının farkı olur ve işaret float gürültüsüne kalır.
- **`hz.make_wall_panel`** artık **dikey hizalı iki pencere sırasını** destekler
  (sinagogun kadınlar mahfili). Sütun başına tek delik varsayan eski kod aynı
  yüzeyi iki kez yazıyordu.

## 7. Kalan boşluklar

- ~~Balat'ta çeşme yerleşmedi~~ ✅ kapatıldı — ADR 0019 §7.
- **Kilise avlusu yok** (mezarlık, servi, çan kulesi kapısı).
- **İç mekân yok**: kapı ardında karanlık levha var, mekân yok. Tevah/ehal,
  apsis içi, ikonostasis — hepsi iç mekân turunda.
- ~~Apsis yarım koni~~ ✅ kapatıldı — yarım kubbe (konka), ADR 0019 §8.

## 8. Karar 5 — haç: **B** (Caner, 2026-08-20)

> *"haç olmasın"*

`KiliseParams.cross` varsayılanı **kapalı**. Parametre kaldı — belge çıkarsa
karar geri alınabilir olmalı — ama artık onu istemek gerekir, sessizce gelmez.
`Kilise_Latin_A` 20,76 → **19,46 m**, 6 876 → 6 852 üçgen.

## 9. Balat: semt bir parametredir

Sinagogun kendi tezi ("sinagogu sinagog yapan şey cephesi değil, avlu
duvarıdır") Balat sahnesi olmadan anlatılamıyordu. `OttomanStreetBuilder`
artık **`QuarterSpec`** alıyor: doku kuralları (ADR 0016) her semtte aynı,
değişen şey *kimin oturduğu* — çekirdek yapı, ibadet yapıları, ev paleti.

| | Galata | Balat |
|---|---|---|
| Çekirdek | mescit + şadırvanlı avlu | **avlulu sinagog**, şadırvansız |
| İkinci cemaat | Latin bazilikası, çan kuleli | Rum kilisesi, kulesiz |
| Ev paleti | `default` (17 varyant) | `nonmuslim` (**3 → 9 varyant**) |
| Sonuç | 87 ev | 98 ev |

Şadırvan **yalnızca camide**: abdest içindir, sinagog avlusuna "elimizde
vardı" diye konmaz. Gayrimüslim varyantlar 3'ten 9'a çıkarıldı — 80 evi 3
kalıptan üretmek, dokunun organikliği için yapılan her şeyi tek başına bozardı.

## 10. Üç sessiz hata daha (hepsi ölçümle yakalandı)

### 10.1 Bembeyaz Balat — denetlenmeyen en görünür harita

Çatı boyası değişince doku yeni bir adla yazıldı; malzeme henüz import
edilmemiş dosyayı bulamadı ve `_BaseColorMap` **NULL** kaldı. Konsol
*"11 malzeme üretildi, 1 uyarı"* dedi. Hata ancak Balat sahnesinde **bembeyaz
evler** olarak görüldü.

İki kör nokta vardı: (a) `Verify` maskeyi arıyordu, **albedoyu aramıyordu** —
en görünür harita denetlenmeyen tek haritaydı; (b) eksik doku `LogWarning`'di,
geçmiş sayılan bir adımın içinde kaybolur. İkisi de düzeltildi: albedo ve
normal de denetleniyor, sorunlar `LogError`.

Testler de aynı kör noktaya sahipti: hepsi **tek bir evi** (varsayılan palet)
geziyordu, gayrimüslim paletin malzemelerine hiç dokunmuyordu. Yeni
`EveryOttomanMaterial_CarriesAllThreeMaps` malzeme klasörünün tamamını gezer.

### 10.2 Kan kırmızısı çatı — render ile pişirme ayrışması

§3.3'teki düzeltme fazla gitmişti. Blender **render**'ında R/G 1,65
görünüyordu; Unity'ye **pişirilen doku** ise R/G 2,78, doygunluk 0,84 —
aydınlatmalı bir render bu farkı gizliyordu. Ölçü artık dokunun kendisidir:

| `tint_factor` | R/G | doygunluk | parlaklık |
|---|---|---|---|
| hedef (`T_ClayRoofTiles02_BC`) | 1,82 | 0,70 | 90,5 |
| 0,85 | 2,78 | 0,84 | 70,4 |
| 0,35 | 1,57 | 0,59 | 101,9 |
| **0,50** | **1,81** | **0,66** | **92,4** ✓ |

Blender öz-testine `t_roof_textures_same_colour_family` eklendi: iki paletin
çatı dokusu aynı renk ailesinde kalmak zorunda. Test hem 1,24'ü (haki) hem
2,78'i (kan kırmızısı) reddeder.

### 10.3 Balat, Galata'nın üstüne yazdı

`Build` semte göre parametreleştirilirken sahne kaydetme yolu `ScenePath`
sabitinde kalmıştı; Balat kurulunca **Galata sahnesi silindi**.
`OttomanStreetTests`'in beş testi bunu anında yakaladı
(*"MAHALLE_Galata yok"*). Testlerin bedelini ödediği yer burasıdır: kurucu
kodun kendisi hiç hata vermedi, log bile doğru göründü.
