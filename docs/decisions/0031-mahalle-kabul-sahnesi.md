# ADR 0031 — Mahalle kabul sahnesi ve inceleme paketinin aleti

**Tarih:** 2026-08-24
**Durum:** Kabul edildi — üretildi, testli, Caner onayı bekliyor
**Tetikleyen:** Faz 2b kabul ölçütü (PLAN.md §7.1)
**İlgili:** ADR 0016 (sokak dokusu), 0023/0025 (aydınlatma, güneş), 0030 (üretim yapıları)

---

## 1. Ölçüt neydi

> *"Mescidi merkez alan bir mahalle: mescit + şadırvan + çeşme + birkaç dükkân +
> mezarlık; sokak yerleştiricisi (ADR 0016) mescidi **çekirdek** olarak kullanır
> ve doku ondan dallanır. HDRP öğle ve gün batımı inceleme paketi."*

Sahne büyük ölçüde vardı (ADR 0016). Eksik olan **paketin kendisiydi** — ve
paketi üretmeye çalışmak, sahnede üç ciddi kusur ortaya çıkardı. İnceleme
paketi burada bir sunum aracı değil, bir **ölçü aleti** olarak çalıştı.

## 2. KALDIRIM TERSTİ — üç turdur görünmeyen kusur

`SM_Kaldirim`'in 698 yatay üçgeninin **697'si aşağı bakıyordu**. Kıyas taşı
aynı sahnedeydi: `SM_Kaideler`'de 166 yukarı, **0 aşağı**.

Üç sonucu birden vardı:

* Üstten bakınca yüzey **ışıksız/siyah** okunuyordu — inceleme karesinde
  sokağın ortasından geçen o bant buydu.
* Unity ışın sorguları arka yüzü **görmez**; yani kaldırımın çarpıcısı fiilen
  **yoktu**. Oyuncu kaldırımın içinden düşerdi.
* Sokak **çimen** görünüyordu, çünkü görünen tek yatay yüzey araziydi.

Kusur ADR 0016 turundan beri duruyordu ve **hiçbir kare onu göstermedi.**
Sebebi acı: o turun bütün kareleri kaldırımın *altından* alınmıştı ve alttan
bakınca yüzey gayet doğru görünüyor. Yakalayan şey göz değil, üçgen
normallerinin sayılması oldu.

`Strip`'in sarımı (0,2,1)+(0,3,2) → (0,1,2)+(0,2,3). Basamak rıhtı da aynı
düzeltmeyle yürüyene döner. Bordür ölçüldü, **zaten doğruydu**, dokunulmadı.
`OttomanStreetTests.PavementWalkingSurfaceFacesUp` normalleri sayarak
kilitliyor.

## 3. Göz hizası ARAZİDEN ölçülemez

Kadrajlar `FrameMetric.OnGround` ile kuruluyordu — arazi yüksekliği. Ama
mahallede yaya araziye **basmaz**: kaldırım kesitin en yüksek noktasından
alınır, ev taş kaidenin üstüne oturur. Yamaçta fark metrelerle ölçülür ve
kareler kaldırımın **altında** çıktı.

`FrameMetric.OnSurface` eklendi: araziden 3 m yukarıdan aşağı ışın atar
(saçak altı, kaldırım üstü) ve **basılan** yüzeyi bulur. Aynı hata
`InterimLighting.Measure`'da da duruyordu; aynı aletle düzeltildi.

## 4. Kadraj tahminle kurulmaz — ÖLÇÜLEREK kurulur

Kadrajları beş tur elle düzelttim ve her seferinde başka bir duvara çarptım.
Sayılar hep makul çıkıyordu, çünkü **duvar da bir dokudur**: bir evin içinden
alınan kare AYRINTI 4,9 / ort 133 veriyordu.

İki alet bunu bitirdi:

**(a) `InFrontOf` nesnenin KENDİ ekseninde ölçer.** İlk hâli `Renderer.bounds`
(dünya hizalı kutu) kullanıyordu; 27° dönmüş bir ev için erim gerçek 2,5 m
yerine **7,95 m** çıktı — kamera 4,6 m'lik sokağı aşıp karşı evin içine girdi.
Artık mesh köşeleri yapının yerel çerçevesine taşınıp en büyük yerel +Z
alınıyor; dönme ne olursa olsun aynı sayı.

**(b) `TryEye` görüş hattını SINAR.** Aday göz noktaları denenir; `CheckSphere`
gözün bir çizicinin içinde olup olmadığını, `Linecast` hedefin görünüp
görünmediğini söyler. Hiçbir aday geçmezse **kare üretilmez ve loglanır** —
sessizce bir duvar yayımlamaktansa eksik paket yayımlamak yeğdir. Log ayrıca
her karenin önünde ne olduğunu metreyle yazar.

Bu alet çalışırken üç ayrı gerçek kusur da buldu:

* Sokak koridoru **iki evin arasına** nişan alıyordu; ölçüm her adayı reddetti
  ve sebebini yazdı: *"görüş hattı 5–8 m'de komşu evin kendisine çarpıyor."*
  Eğri bir sırada iki ucu birleştiren kiriş aradaki evin **arkasından** geçer —
  sokak ne kadar düz olursa olsun. Bakış artık **yerel teğet** boyunca;
  koridorun nerede kapandığı da ölçülüp kareye yazılıyor (**40 m**).
* Hazire karesi **iki mezarlığın ortalamasını** alıyordu (cami haziresi +
  kilise mezarlığı) ve ikisinin arasındaki boş çimene bakıyordu. Artık
  çekirdeğe en yakın mezardan kümeleniyor.
* Dükkân karesi sıranın **ortasına** bakıyordu; bir sıranın ortasını en yakın
  dükkânın kendisi kapatır. Bakış sıranın ucundan, hedef yapının **cephesi**.

## 5. Poz ÖLÇÜLEREK seçilir, iki an için ayrı

Geçici aydınlatmanın 13,0 EV'si 43° yükseklikteki güneşe göre süpürülmüştü
(ADR 0023). Öğle güneşi **63,9°**'de, gün batımı **6,0°**'de; aynı pozu ikisine
de uygulamak birini karartır.

Her an için poz, sokak koridoru karesi üstünde bir merdiven süpürülerek
seçiliyor. Ölçüt **iki basamaklı**, ağırlıklı toplam değil: önce patlak piksel
oranı %0,5'in altında olacak (patlamış kare geri döndürülemez), sonra kalanlar
arasında ayrıntı enerjisi en yüksek olan kazanacak. Fotoğrafçının pozometreyle
yaptığı iş.

Sonuç: **öğle 12,5 EV**, **gün batımı 9,5 EV** — üç durak fark.

Dolgu ışıkları da güneşe bağlandı: iki dolgu da **gök** terimini taklit ediyor
ve gök aydınlığı güneş alçaldıkça düşer (`sin(yükseklik)` ile). Sabit
bırakılsaydı gün batımı karesi "yanlış yönden gelen bir öğle ışığı" olurdu:
kadraj doğru, ışık yalan. Ölçek öğlede 1,31×, gün batımında 0,15×.

**"Gün batımı" bir saat değil bir YÜKSEKLİKtir** ve tarihten hesaplanıyor
(`SunPlacement.AfternoonHourAtAltitude`). Ufkun dibi (0°) kare vermez; 6° hâlâ
gün batımıdır ve mimarî okunur. 1 Mayıs için güneş saati 18,34, azimut 284,6°.

## 6. Sahneye eklenen tek yapı: bozahane

Faz 2b'nin yedi yeni yapısından **yalnız bozahane** mahalleye girdi, ve
gerekçesi kaynakta: 1638 esnaf sayımında İstanbul'da **300 bozahane** var
(RESEARCH.md §4.7c). Bu bir külliye yapısı değil, mahalle dükkânıdır ve çarşı
ucunda, kahvehanenin yanında durur. Böylece oyunun **iki zaman işareti** aynı
sahnede: ikisi de 1632'de açık, ikisi de IV. Murad döneminde kapanıyor.

Ötekiler bilerek dışarıda ve her birinin gerekçesi ayrı: muvakkithane selâtin
camisine aittir (ADR 0030 §2); imaret bir külliyenin mutfağıdır; arasta bir
**sokak tipolojisidir**, tek prefab olarak mahalleye tıkılmaz; su değirmeni
dere, su terazisi Kırkçeşme hattı ister. Elde var diye koymak her birinin
kendi tezini bozardı.

## 7. Dükkân sırası slot değil HEDEF sayıyor

Dört slot deneniyor, elenen slot kayboluyordu ve sahnede **iki** dükkân
kalmıştı — "birkaç dükkân" sessizce "iki dükkân" olmuş. Sebep çakışma değil
**sıra**ydı: sebil ve çeşme çekirdeğin çevresine daha önce yerleşip ilk iki
slotu kaplıyor. Artık dördü yerleşene kadar sokak boyunca ilerleniyor; bir
dükkân sırası zaten böyle uzar.

## 8. Denetim turunda çıkan iki kusur daha

Paket bittikten sonra Caner'in istediği baştan-sona denetim turu koştu
(`docs/feedback/denetim_turu.md`). İkisi bu ADR'nin konusuna değiyor:

**İki mahalle birbirinin kaldırımını siliyordu.** Galata ve Balat üretilen
mesh'i aynı varlık yoluna yazıyordu; Balat kurulunca Galata'nın kaldırımı ve
bütün taş kaideleri gidiyor, yerine 2 km ötedeki geometri geçiyordu. Ölçüldü:
Galata sahnesindeki kaldırım mesh'inin merkezi **x = −1976**. Yol artık semte
göre; test `GeneratedMeshesBelongToThisQuarter` mesafeyi ölçüyor.

**İki prefab malzemesizdi.** `PF_Cami_Kubbe` kurşun malzemesi eklenmeden bir
gün önce üretilmiş ve hiç yenilenmemişti (kubbesi macenta çıkardı);
`PF_BoxHouse` boru hattının ilk varlığıydı. İkisi de yeniden üretildi. Cami'nin
geometrisi bit bit aynı çıktı — kayıp yalnız malzemeydi.

## 9. Ölçülen ve açık kalan

Paket: `Captures/mahalle/{ogle,gunbatimi}_01..08_*.png`, 8 kadraj × 2 an.
Her karenin AYRINTI, parlaklık dağılımı ve "önünde ne var, kaç metrede"
satırı log'da.

Açık kalanlar:

* **Mahalle çimenin üstünde duruyor.** Kaldırım düzeldi ama kaldırım dışındaki
  zemin — hazire tabanı, avlu çevresi, dükkân önü — arazi örtüsünün çayırı.
  Basılmış toprak olmalı. Arazi örtüsü işi (ADR 0026), Faz 4'e ait.
* **Dükkân ve kahvehane içleri kapkara.** Kutu kapalı; HDRP'de pişirilmemiş
  ışıkla kapalı bir iç mekân siyahtır. Kalıcı ışık pasının (Faz 5) işi,
  modelleme kusuru değil.
* **Avlu payı 3,5 m dar.** Ölçüldü: şadırvanla kapı arası **2,2 m**. Mahalle
  mescidinin avlusu küçüktür ama bu kadar değil; kadrajın darlığı kusur değil,
  avlunun kendi ölçüsü.
* **Geçici pozun kalibrasyonu eskimiş.** Pozometre öğle için 12,5 EV diyor,
  geçici takım 13,0 EV taşıyor. Ölçüt farklı (ADR 0023 "okunmaz &lt; %25" eşiğini
  geçen İLK değeri aldı; buradaki ölçüt en yüksek ayrıntı), o yüzden çelişki
  değil — ama kalıcı ışık pasında 13,0 yeniden süpürülmeli.
* **Şadırvanın musluk sırası yok.** Kurşun külah ve tekne var, musluk yok.
  Yakın plan donatısı.
