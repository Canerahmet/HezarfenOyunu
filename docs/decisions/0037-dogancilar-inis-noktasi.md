# ADR 0037 — Doğancılar: iniş noktasının georeferansı, yapıları ve uçuşun fiziği

- **Tarih**: 2026-08-25
- **Durum**: ✅ **KARARA BAĞLANDI** (Caner, 2026-08-27) — **gerçek termik simülasyonu**

> ## Caner'in kararı: gerçek termik, mekanik kaldıraç değil
>
> Ben **mekanik** yükselen hava önermiştim (tasarlanmış, deterministik bir
> alan) çünkü ölçüm gereken ortalamayı **~0,9 m/s** veriyordu ve bu zayıf
> termiğin bile altında görünüyordu. Caner gerçek simülasyonu seçti.
>
> **Ve seçim benim sunduğumdan daha iyi çıkıyor.** Uyarım düz bir süzülüşü
> varsayıyordu; oysa fizik başka bir şey söylüyor:
>
> **Termik kara üstünde doğar, su üstünde doğmaz.** Uçuş Boğaz'ı geçiyor,
> yani süzülüş sırasında zaten kaldıraç olmayacak. Doğru model şu:
> **önce Galata yamacında yüksel, sonra geçişe bağlan.**
>
> | | |
> |---|---:|
> | süzülme oranı (ölçülen kanat) | 11,56 : 1 |
> | yatay menzil | 3 336 m |
> | gereken irtifa | **~289 m** |
> | kule tepesinin verdiği | 52 m |
> | tırmanılması gereken | **~240 m** |
> | 2 m/s termikte süre | **~2 dakika** |
>
> Yani uçuş bir *şans* değil bir **beceri** olur: kaldıracı bul, sarmal çiz,
> yeterli irtifayı topla, sonra karşıya bağlan. Bu hem dürüst fizik hem de
> oyunun çekirdek mekaniği için daha iyi — düz süzülüş tek tuşluk bir
> sahneyken, termik okumak öğrenilebilir bir zanaat.
>
> **Uygulanacak model:** güneşle ısınan kara yamaçlarında yükselen hava
> (eğim yönü + günün saati + yüzey tipiyle), su üstünde alçalan hava,
> yamaç kaldıracı (lodos rüzgârının yamaca çarpması). Kodekse dürüstlük
> notu: Hezarfen'in gerçekte nasıl uçtuğu bilinmiyor; oyun bunu dönemin
> havasıyla mümkün kılıyor.
>
> ---
>
> ## Uygulandı ve ölçüldü (2026-08-27)
>
> `TerrainThermal` araziden dört terim türetir ve hiçbiri elle konmaz:
> güneşe bakan yamacın termiği (bakı + eğim + güneş yüksekliği), **su
> üstünde çökelme**, rüzgârın yamaca çarpmasından doğan yamaç kaldıracı, ve
> bulut tabanı tavanı. `WindField` bunu elle konan hacimlerin *üstüne*
> ekler — hacimler artık istisna aracı.
>
> `ThermalFlightSim` uçuşu **adım adım** simüle eder. Kapalı formül
> yazmadım çünkü yanlış olurdu: koridorun bir kısmı su, bir kısmı kara ve
> ortalama almak suyun eksisini karaya yayarak gizlerdi.
>
> **Ölçülen koridor** (kule → Doğancılar, 200 m kotta):
>
> | yol | zemin | dikey hava |
> |---|---:|---:|
> | %0 (kule) | 52 m | **+0,30** m/s |
> | %20–%80 (Boğaz) | −12 m | **−0,49** m/s |
> | %90 (Üsküdar kıyısı) | 7 m | **+1,07** m/s |
>
> Model doğru davranıyor: kaldıraç karada, çökelme suda. Koridorun **%70'i
> inen hava** — yani süzülüş sırasında yardım yok, tırmanış geçişten önce
> bitmeli.
>
> **Sınav sonucu** (9 m/s rüzgâr, süzülme 11,48:1, alçalma 1,08 m/s):
>
> | | |
> |---|---:|
> | en iyi kaldıraç | **1,79 m/s**, kuleden 380 m |
> | net tırmanış | 0,71 m/s |
> | gereken irtifa | **246 m** |
> | tırmanış | **298 s (5,0 dk)** |
> | geçiş | **163 s (2,7 dk)** |
> | varış kotu | 50,3 m (hedef 46,1 m) |
>
> **Uçuş yapılabiliyor.** Ama pay ince: asgari tırmanışla yalnızca **4,2 m**.
> Bu bir kusur değil, simülasyonun kendini sınırlaması — tam gerekene kadar
> tırmanıp duruyor. Gerçek oyuncu tavana yakın çıkar: tavan **570 m**,
> gereken 246 m, yani 324 m fazladan irtifa ≈ **3 700 m fazladan menzil**.
> Yani "yeterince yüksel" öğrenilebilir bir beceri, "şans" değil.
>
> **Açık kalan ayar:** 5 dakikalık tırmanış oyunun açılışı için uzun
> olabilir. Tek düğme `peakLift` (şu an 2,6 m/s — yaz öğleden sonrası
> İstanbul'unda 3-4 m/s savunulabilir); 3,5'e çıkarsa net tırmanış 0,71 →
> 1,6 m/s ve süre 5 dakikadan ~2,5 dakikaya iner. Yapıyı değiştirmez, bir
> alan değiştirir. **Caner'e sorulacak: tırmanış ne kadar sürmeli?**
- **Bağlam**: Faz 3, S-kademe. Hezarfen'in **iniş noktası**; oyunun finali.

## Karar 1 — Doğancılar'ın koordinatı düzeltildi (771 m)

`LM_Dogancilar` elle girilmiş bir koordinat taşıyordu (29,0181 / 41,0245) ve
Galata Kulesi'ne **3709 m** veriyordu. Modern kaynakların verdiği
**3358 / 3400 / 3558 m**'nin hiçbirine uymuyordu ve bu uyumsuzluk
`landmarks_build.py`'nin kendi raporunda her koşuşta yazılıyordu — kimse
bakmamıştı.

Kültür Envanteri kaydındaki koordinat **41,018907 K / 29,012677 D**; fark
**771 m**. Düzeltilmiş mesafe **3336 m**: en düşük modern değere %0,7 yakın.

İkinci sonuç kot: eski nokta kıyıya yakındı ve DEM **15,3 m** okuyordu;
gerçek meydan yamaçtadır, DEM **46,6 m** okur.

Yeni test `FlightGeometryMatchesTheDocumentedDistance` bunu kilitliyor.

### Yan bulgu — `landmarks_build.py` çalışmıyordu

Modül `coastline_build`ten import ediyordu, o da `dem_fetch` üzerinden
**rasterio**'ya bağlanıyordu; rasterio bu makinede "Application Control
policy" ile engelli (SETUP.md `[İNSAN]`) ve `geodesy.py` tam bu yüzden
yazılmıştı. Yani GIS boru hattının landmark ayağı bir süredir
**çalıştırılamaz** durumdaydı. Import `geodesy`ye çevrildi; iki modülün
`utm_to_grid` imzası farklı olduğu için (liste vs tek nokta) çağrı da
uyarlandı.

## Karar 2 — Meydanın 1632 yapıları üretildi, **taslak** olarak

**Çakırcıbaşı Hasan Paşa (Doğancılar) Camii** — 1548, Mimar Sinan;
1580'lerde Hacı Ahmed Paşa yeniledi, yani 1632'de görülen odur.
**Aziz Mahmud Hüdâyî tekke-camii** — 1589'da başlandı, 1595'te tamamlandı,
1598-99'da minber eklenerek camiye çevrildi.

İkisinin de 1632 hâlinin **ölçülü çizimi yok**: bugünkü Doğancılar Camii
büyük ölçüde **1857**, Hüdâyî Külliyesi **1855-56**'dır. Bu yüzden ölçüler
**tipolojik varsayılan**dır, ölçüm değil; kütleler **D3 / `status: draft`**
taşır ve yuvarlak sayılarla kuruldu — ondalıklı bir sayı burada olmayan bir
kesinlik iddia ederdi.

Kaynağın kesin söylediği tek biçim niteliği kullanıldı: duvarlar **kâgir**,
çatı **ahşap**, **tek** minare. Yeni bir kütle uydurmak yerine `mosque_kit`
kullanıldı ve bir üretici denetimi çatının ahşap kalmasını zorluyor — kubbe
"daha gösterişli" göründüğü için sessizce kayması kolay bir hatadır.

### `portico_material="stone"`

İlk kurulumda revak direkleri mahalle mescidinin **aşı kırmızısı ahşabıyla**
çıktı ve render'da yapı kâgir bir cami gibi değil, boyalı ahşap saçaklı bir
mescit gibi okundu. Oysa ayakta kalan özgün parçalar "mermer çerçeveli kapı"
ile "ince kesme taş minare kaidesi"dir. `mosque_kit`e tipolojik bir seçenek
eklendi; mahalle mescidinin varsayılanı **değişmedi**.

## Karar 3 — Hüdâyî türbesi üretildi ama **sahneye konmadı**

Hüdâyî **Ekim 1628**'de öldü ve vasiyeti üzerine dergâhının bahçesine
gömüldü; 1632'de mezar dört yaşındadır. Ama türbe **yapısının** ne zaman
kurulduğunu kaynaklarda bulamadım. Kesin bilinen, 1850 yangınında *"Hüdâyî
Türbesi dışında kalan binalar ortadan kalktığı"* — yani 1850 öncesi kâgirdi
ve TDV onu **açık (baldaken) türbe** diye tanımlar.

Landmark kataloğunun sözleşmesi **T1 = varlığı belgeli**dir. T1 yazmak yalan,
T2 yazmak sözleşmeyi bozmak olurdu. Üçüncü yol seçildi: **varlık üretildi ve
saklandı, ama onay gelene kadar yerleştirilmiyor.** Test
`HudayiTombIsNotPlacedUntilItsExistenceIsSettled` bu kararı kilitliyor.

---

## Soru 1 (Caner) — Uçuş fiziksel olarak mümkün değil; ne yapalım?

Ölçüldü:

| | |
|---|---|
| Galata Kulesi 1632 tepesi | 98,2 m |
| Doğancılar arazi kotu (DEM) | 46,6 m |
| Yatay mesafe | 3336 m |
| Düşüş | 51,7 m |
| **Gereken süzülme oranı** | **64,6 : 1** |

Modern rakamlarla da değişmiyor: 3358/62 → 54:1, 3558/86 → 41:1.
Karşılaştırma: iyi bir yamaç paraşütü ~10:1; yarış planörü ~50-60:1. 17. yy
kanadıyla sakin havada 4-6:1'den fazlası beklenemez.

### Önerimi ölçüm düzeltti: sorun rüzgâr değil, **yükselen hava**

İlk yazımda "rüzgârı mekanik yap" demiştim. Sonra gerçek aerodinamik ayarla
ölçtüm ve öneri çürüdü. `Hezarfen → Uçuş → Uçuş bütçesini ölç`:

```
kanat: 100 kg / 15 m2, en iyi suzulme 11,56 : 1 (alfa 6,2 derece)
trim 12,4 m/s (45 km/h), alcalma 1,08 m/s
kule tepesi 98,2 m -> Dogancilar 46,6 m = dusus 51,7 m
mesafe 3336 m; sakin hava menzili 597 m; EKSIK 2739 m
```

**Arkadan rüzgâr tek başına çözmüyor**: 2739 m'lik açığı 48 saniyelik bir
süzülüşte kapatmak **57 m/s (205 km/h)** rüzgâr isterdi. Çünkü rüzgâr uçuş
*süresini* kısaltır, *alçalmayı* değil — bağlayıcı kısıt süzülme oranı değil
**alçalma hızıdır**.

Doğru büyüklük yükselen havadır ve şaşırtıcı biçimde küçüktür:

| arkadan rüzgâr | yer hızı | süre | **gereken yükselen hava** |
|---|---|---|---|
| 0 m/s | 12,4 | 268 s | **0,88 m/s** |
| 3 m/s | 15,4 | 216 s | 0,84 m/s |
| 6 m/s | 18,4 | 181 s | 0,79 m/s |
| 9 m/s | 21,4 | 156 s | 0,74 m/s |
| 12 m/s | 24,4 | 137 s | **0,70 m/s** |

Zayıf bir termik 1-2 m/s, güçlü 3-5 m/s, yamaç rüzgârı 1-3 m/s. Yani uçuş
**ortalama 0,9 m/s'lik bir tırmanmayla mümkün** — zayıf bir termiğin bile
altında. Rüzgâr da yardımcı olur ama belirleyici değildir.

**Seçenek A — Yükselen havayı mekanik yap (önerim, düzeltilmiş).**
Oyuncu Boğaz'ı geçerken yükselen havayı bulup içinde kalmak zorunda. Fizik
dürüst kalır, kanat 11,6:1'de kalır (zaten 17. yy için cömert), final bir
beceriye dönüşür. `WindTuning` zaten rüzgâr alanı taşıyor; gereken şey
alanın *dikey* bileşeni.

**Seçenek B — Mesafeyi oyun için kısalt.** Dürüst ama pahalı: Üsküdar'ı
yaklaştırmak bütün georeferansı bozar, ADR 0007'yi çöpe atar.

**Seçenek C — Fiziği görmezden gel**, 65:1 süzülen bir kanat ver. En kolayı;
ve oyunun bütün iddiası ölçmek üzerine kurulu olduğu için en pahalısı. Bu
seçeneği zorlaştırmak adına `GlideRatioStaysHistoricallyDefensible` testi
oranı 15:1'in altında tutuyor — C seçilirse test bilerek gevşetilecek, yani
karar kayda geçecek.

*Not: DEM'in 46,6 m'si de tartışmalı olabilir — Copernicus GLO-30 bir
**yüzey** modelidir ve yoğun yapılı alanda binaları içerir; modern "62 m kot
farkı" rakamı Doğancılar'ı ~36 m'de varsayar. O kot bile gereken tırmanmayı
yalnızca ~0,85 m/s'ye indirir; sonucu değiştirmez.*

## Karar 4b — Hüdâyî türbesi **1632'de duruyor** (Soru 2 kapandı)

Caner kararı bana bıraktı: *"hüdayi türbesine sen karar ver tarihi
kaynaklara göre"*. Kaynağa doğru soru sorulunca cevap çıktı.

**Tarih belgeli**: Hüdâyî **Safer 1038**'de (Ekim 1628) öldü ve türbe
**aynı hicrî yıl içinde, 1038'de (1628-29)** yapıldı — yani ölümünden
aylar sonra (Kültür Envanteri, "Aziz Mahmud Hüdayi Türbesi"). 1632'de yapı
**üç-dört yaşındadır**. Tier **T1**'e çıktı: varlığı belgeli.

**Biçim ayrı bir soru ve ayrı taşınıyor.** TDV, 1850 yangını öncesi ayakta
kalan yapıyı **açık türbe** diye tanımlar; bugünkü kubbe **dört mermer
sütun** üzerine oturur ve o baldaken çekirdek, kapatılmadan önceki hâlin
izidir. Model bu yüzden dört ayaklı bir baldakendir. Oranlar **D3** —
1632 hâlinin ölçülü çizimi yok. 1632'de olmayan her şey (kapalı kâgir
kabuk, 7,40×8,80 m plan, on üç dilimli kubbe, yedi pencere) 1272/1855-56
yeniden inşasıdır.

Bir tur önceki tereddüt yanlış değildi ama **eksikti**: soruyu "türbe ne
zaman yapıldı" diye sormak yerine "türbe var mıydı" diye sormuştum.

### Bayrak tuzağı: `acik` okunmuyordu

Türbe ilk kez kurulduğunda `TurbeParams.acik=True` verilmişti ve
üreticinin denetimi `if not p.acik: raise` idi — geçti. Ama `acik`
**kitte hiç kullanılmıyordu**: kapalı bir türbe kurulup "açık" diye
kataloglanıyordu. Bayrağın değerini sınamak, bayrağın okunduğunu
varsaymaktır. Açık türbe artık gerçekten kuruluyor (`_build_acik_turbe`),
denetim **üretilen yapının bildirdiğine** bakıyor ve test de öyle.

Yan bulgu: yeni kütlede sekiz kabuk ters çıktı ve `ensure_outward` onları
sessizce çevirdi. Ağ yakaladı diye neden bırakılmaz — çerçeve sağ elli
olacak biçimde düzeltildi.

## Sonuç

- Yerleşen: Galata Kulesi, Kız Kulesi, Üsküdar Mihrimah, Doğancılar Camii,
  Hüdâyî tekkesi. Boş/gömülü malzeme yok; iki cami de kıbleye dönük.
- `landmarks_build.py` yeniden çalışır durumda; katalog 25 landmark.
- Yeni araç: **`Hezarfen → Uçuş → Uçuş bütçesini ölç`** — bütçeyi gerçek
  arazi, gerçek kule ve gerçek ayardan hesaplar. Elle hesaplanıp belgeye
  yazılan bir sayı ilk değişiklikte sessizce yanlışa dönerdi.
- Yeni bekçi: `GlideRatioStaysHistoricallyDefensible` (oran < 15:1) ve
  `FlightBudgetIsMeasurableAndMatchesTheRecordedFigures`.
- Bekleyen: meydanın kendisi (zemin, çınarlar,
  doğancı ocağı), Doğancılar Camii avlusundaki Hacı Ahmed Paşa türbesi.
