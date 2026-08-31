# ADR 0083 — Yatışta sarmal dalış: kanat sağlam, dönüş bozuk

- **Tarih:** 2026-09-01
- **Durum:** kısmen çözüldü (batış 2,49 → 2,12 m/s), açık kalan yazılı
- **Bağlam:** yorumcu turu 3 (uçuş ve fizik)

## Karar

Yatmış uçuşta hedef hücum açısı **yük katsayısıyla** telafi edilir
(`targetAlpha *= min(2.5, 1/cos φ)`). Teşhisin önerdiği ikinci değişiklik
— kararlılık terimini komut edilen açıya itmek — **denendi ve ölçümle
reddedildi**, geri alındı.

Kapanmamış kalan açık bir **cırcır** testiyle taşınır
(`SustainedBank_DoesNotSpiralDive`): bugünkü değer tavan, hedef yazılı.

## Sebep

Uçuş denemesi 0/20 diyordu ve ilk şüphe termiğe gitti. Yorumcu fizik
motorunu Python'da yeniden kurdu ve iki bağımsız çapayla doğruladı
(`Aerodynamics.BestGlideRatio` = 11,56:1 tam olarak, ADR 0037'nin termik
sayıları %4 içinde). Kontrol ölçümü şunu dedi:

| komut | L/D | alfa |
|---|---:|---:|
| eller serbest, pitch 0 | **11,22 : 1** | 9,2° |
| pitch −0,35 | **11,57 : 1** | 6,2° |

**Kanat sağlam.** `Aerodynamics` ve `GlideController.Step()` teoriyi
veriyor. Kusur sonrasında.

### Sarmal ıraksaması

`GlideController.ApplyPilotControl` pilotun hedef **hücum açısını** komut
ediyordu ve o hedef **yatıştan habersizdi**. Yatmış uçuşta taşımanın
dikey bileşeni `cos φ` kadar azalır; düz uçuşu sürdürmek için gereken
taşıma `W / cos φ`'dir (55°'de 1,74 katı). Model bunu hiç istemiyordu.
Dahası nose-up torku **yatmış** `transform.right` ekseni etrafında
uygulanıyor, yani 55°'de dikey bileşeni yalnızca 0,57.

Sonuç ders kitabı: uçuş yolu gövdenin dönebileceğinden hızlı dikleşiyor →
alfa çöküyor → taşıma çöküyor → dalış dikleşiyor.

| yatış | ölçülen batış | teorik | oran |
|---:|---:|---:|---:|
| 0° | 0,93 | 1,08 | — |
| 23,7° | 1,37 | 1,20 | 1,14× |
| 34,2° | 2,49 | 1,40 | 1,78× |
| 43,4° | 5,16 | 1,76 | 2,93× |
| 51,2° | 10,39 | 2,48 | 4,19× |

51°'de hücum açısı **−0,1°**. Yani **termikte dönmek hiçbir yatış
açısında mümkün değildi** — 33°'de bile net −0,62 m/s.

### Neden hiçbir test görmedi

`GlideSimulationTests` süzülme oranını ölçüyor ama yalnız **roll = 0**
ile uçuyor. `RollRight_TurnsRight` dönüyor ama **batışa hiç bakmıyor**.
Aradaki boşlukta bu kusur yaşıyordu. Bu, ADR 0082'nin uçuş hâlinin
küçüğü: ölçülen şey doğru, ölçülmeyen şey oyunun kendisi.

### Termik yok değil — yanlış yönde

Gerçek yükseklik haritasından hesaplandı (2049 örnek): **en iyi kaldıraç
+1,87 m/s, kuleden 160 m batı-güneybatıda** (zemin 38 m, eğim 15,9°, bakı
227°). `bestSlopeDeg = 17` ve `sunAzimuthDeg = 225` bu nokta için
neredeyse mükemmel ayarlanmış. Ama hedef **doğuda** (101°) ve otomatik
pilot ilk kareden itibaren oraya yöneliyordu. Doğu kadranı tam sıfır:
yamaç güneşe sırtını dönmüş (`TerrainThermal.cs:151`) ve lodosun rüzgâr
altında (`:186`). 600 m sonrası zaten su, `waterSink` −0,46.

**"Termik sıfır kazanç veriyor" doğru bir gözlem, yanlış bir teşhisti.**

### Otomatik pilot ve ölçüm aracı

`Roll = Clamp(aci / 45f, -1, 1)`: kalkış yönleri −40…+40, hedef 101° →
başlangıç yön hatası 62–142°, yani roll **ilk karede** doyuyor ve 55°
yatış komut ediliyor. Ölçülen ortalama L/D 4,04, ortalama |yatış| 33°.

`tirmaniyor = linearVelocity.y > 0.15f` **yer eksenliydi**: sarmal
dalıştan çıkışta dikey hız +7,9 m/s'ye çıkıyor ve pilot bunu termik
sanıp dönüyordu. Uçuş bir dal-zoom-dön limit çevrimiydi. Doğru sinyal
hava eksenli: `vy − windField.Sample(p).y`.

**Ve araç 20 uçuş rapor ediyordu, gerçekte 5'i tekrarlıyordu** — sahnede
türbülans yok, zaman adımı sabit; tekrarları farklılaştıracak hiçbir
kaynak yoktu. 15 satır bayt bayt kopyaydı.

## Ne yapıldı, ne yapılmadı

**Yapıldı:**
- Yatış telafisi (`targetAlpha *= min(2.5, 1/cos φ)`). Ölçüm: 33° yatışta
  batış **2,49 → 2,12 m/s**, alfa **2,4° → 3,18°**. Yatışsız uçuşta
  `cos 0 = 1`, yani etkisiz — yedi mevcut test aynen geçti.
- Pilot yatışı ±0,4'te kırpılır (~22°); tırmanış hava ekseninde ölçülür.
- Kontrol denemesi (eller serbest) rapora sabit satır olarak eklendi.
- 20 uçuş gerçekten 20 ayrı yön.

**Denendi ve reddedildi:** kararlılık terimini komut edilen açıya itmek
(`(alfa − targetAlpha) * pitchStability`). Düz uçuş çöktü: süzülme oranı
**11,2:1 → 2,57:1**, nötr girdide alfa 15,5° (stall), dönüş 20° → 9,7°.
Beş test birden kırmızı. Sebep: alfa-orantılı geri yükleme eğimi bu
modelde pitch döngüsünün tek kararlılık kaynağı.

> **Bir teşhis, doğru kısmıyla birlikte yanlış kısmını da taşıyabilir.**
> İkisi aynı raporda, aynı güvenle yazılmıştı; ayıran şey ölçüm oldu.

**Yapılmayacak (ölçülerek elendi):**
- `pitchAuthority` 2,2 → 4,0: düz uçuş bozuluyor (batış 0,93 → 4,55).
- `peakLift` 2,6 → 3,5: kusuru düzeltmez, **örter**; ADR 0037'nin "uçuş
  şans değil beceri" iddiasını sessizce zayıflatır.

## Açık kalan

33° yatışta batış hâlâ teorinin 1,78 katı (hedef 1,30–1,60). Doğru yol
muhtemelen sabit hücum açısı yerine **sabit hava hızı** trimi: asılı
planör gerçekte alfa değil hız trimler ve ağırlık aktarımı trim hızını
değiştirir. Ayrı bir tur, ayrı bir ölçüm.

Cırcır eşikleri: batış oranı **2,30** (hedef 1,60), yatışta alfa
**3,0°** (hedef 4,0°).
