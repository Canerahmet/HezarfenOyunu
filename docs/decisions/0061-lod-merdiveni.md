# ADR 0061 — LOD merdiveni: ayrıntı, görüntülendiği yerde olmalı

**Tarih:** 2026-08-27
**Durum:** Kabul edildi (ölçüldü, teste bağlandı)
**Bağlam:** Ayrıntı geçişinin (ADR 0057) ölçülen yan etkisi

## Bulgu

Ayrıntı geçişi LOD0'ı altı katına çıkardı ve **LOD1'e hiç dokunmadı**.
Sonuç ölçüldü, tahmin edilmedi:

| yapı | LOD0 | LOD1 | tek adımda düşüş |
|---|---:|---:|---:|
| Sultanahmet | 102 952 | 550 | **187×** |
| Süleymaniye | 89 668 | 456 | **197×** |
| Mihrimah Medresesi | 7 234 | 36 | **201×** |

Sağlıklı bir zincirde kademe başına 2–4 kat düşülür.

Ama asıl mesele oran değil **mesafe** çıktı. LODGroup eşiği ekran
yüksekliğinin oranıdır; FOV 40°'de mesafeye şu formülle çevrilir:

```
d = boy / (eşik × 2·tan(FOV/2))
```

Varsayılan eşik `0,25` ile:

| yapı | LOD0 şu mesafeye kadar |
|---|---:|
| Sultanahmet | 661 m |
| Süleymaniye | 573 m |
| Ayasofya | 640 m |
| Sultan Ahmed türbesi | 141 m |

**Hezarfen'in uçuşu Galata'dan Doğancılar'a 3 336 m.**

Yani üretilen ayrıntının tamamı, oyunun **merkez sahnesinde hiç
görüntülenmiyordu**. Uçuş boyunca her anıt kendi blok siluetiydi.

Gözle bulunması imkânsız bir kusurdu: yakından bakınca ayrıntı gerçekten
oradaydı. Onu bir **formül** ele verdi.

## Neden filtreleyerek çözülmedi

İlk fikir "orta kademeyi LOD0'dan küçük parçaları atarak üret"ti. Ölçüldü:

| eşik | kalan üçgen |
|---|---:|
| ≥ 0,6 m | 85,7 % |
| ≥ 1,2 m | 57,2 % |
| ≥ 4,0 m | **33,2 %** |

4 metrenin altındaki her şey atılsa bile üçgenlerin üçte biri kalıyor,
çünkü **yük küçük süslerde değil, çok bölütlü kubbelerde ve
kütlelerdedir**. Aynı sebeple decimate de işe yaramaz: bir kutu birleşimini
kenar-çökertmeyle indirmek siluetleri bozar.

Bir dome `segments=28` ile kurulduktan **sonra** ucuzlatılamaz. Orta kademe
bu yüzden aynı üreteçten **daha az bölütle yeniden kurulur**.

## Karar

### 1. Kademe anahtarı — `hz.set_detail(scale, min_size)`

- Eğri ilkellerin bölütleri `scale` ile ölçeklenir (`hz.seg`).
- `detay_kit`in **gölge dokusu** öğeleri `min_size` altında düşer:
  mukarnas hücresi → tek bilezik, kubbe kaburgası → yok, konsol dizisi →
  tek bilezik, silme → tek basamak, demir şebeke → yok, şerefe korkuluğu →
  seyrelir, kemer adımları → yarıya.
- **Siluete giren hiçbir şey düşmez.** Mukarnas boş dönmez, çünkü işi bir
  çıkmayı taşımaktır ve o taşıma siluete girer; kaybolan şey hücrelerin
  gölge dokusudur.

`seg()` bir hatayla yazıldı ve **sayı yakaladı**: `max(alt, n·DETAIL)` tam
ayrıntıda hiçbir şeyi değiştirmemeliyken zaten düşük bölütlü ilkelleri
*yükseltiyordu*. Doğrusu `max(min(n, alt), n·DETAIL)` — kademe altyapısı
tam ayrıntıda **görünmez** olmalı.

### 2. Üç kademeli üretim — `ottoman_kit.build_with_mid_lod`

Yapı iki kez kurulur; eldeki LOD1 **LOD2** olur, orta kademe **LOD1**.
LOD0'ı 20 binden ağır olan varlıklara uygulandı (dokuz üreteç).

| yapı | LOD0 | LOD1 | LOD2 |
|---|---:|---:|---:|
| Sultanahmet | 103 144 | 50 878 | 550 |
| Süleymaniye | 89 812 | 40 838 | 456 |
| Beyazıt | 55 404 | 22 412 | 296 |
| Fâtih | 48 950 | 18 602 | 262 |
| Ayasofya | 39 682 | 23 879 | 492 |
| Sandal Bedesteni | 20 880 | 3 224 | 812 |
| Sultan Ahmed türbesi | 9 942 | 3 202 | 78 |

### 3. Eşikler Unity'nin varsayılanına bırakılmaz

`ImportLanding.SetLodThresholds` merdiveni yazar:

| kademe | eşik | Süleymaniye'de |
|---|---:|---|
| LOD0 tam ayrıntı | 0,25 | 0 – 573 m |
| LOD1 orta kademe | 0,03 | 573 m – 4 777 m |
| LOD2 blok siluet | 0,004 | 4 777 m – 35 834 m |

Uçuşun 3 336 m'si artık **tümüyle LOD1'in içinde**.

Merdiven eklendiğinde depoda 150'den fazla prefab vardı ve boru hattı
yalnızca `_Import`'a düşeni işliyor. Onları yenilemek için bütün FBX'leri
yeniden aktarmak, **içeriği hiç değişmeyen 150 ikili dosyayı LFS'e ikinci
kez yazmak** olurdu (ADR 0059, yeniden üretim gürültüsü). Bunun yerine
`Hezarfen → Boru Hatti → LOD merdivenini uygula` prefab'ı yerinde düzeltir:
**122 prefab güncellendi.**

### 4. Bekçi

`HeavyLandmarksHaveAMidLodAndAnExplicitLadder` iki olguyu tutar: LOD0'ı
20 binden ağır her varlık üç kademeli olmalı, ve eşikler merdivenden
sapmamalı. Test yazıldığı anda **işini yaptı** — yalnızca yeniden
aktardığım 14 varlığın eşik aldığını, kalan 122'sinin varsayılanda
kaldığını gösterdi.

## Kabul edilen bedel

- Orta mesafede daha çok üçgen: ~20 anıt görünürken ~800 bin üçgen.
  RTX 4070 Laptop için sorun değil; ölçülecek yer Bench sahneleridir.
- Ayasofya (1,7×) ve Mihrimah Medresesi (1,1×) zayıf indirgendi; yükleri
  benim kapılarımın dokunmadığı yerde (`arched_panel` cepheleri, hücre
  kubbeleri). İkinci tur işi.
- Mescit tabanlı varlıklar (Doğancılar, Hüdayi tekkesi) hâlâ iki gerçek
  kademeli: LOD0 4 476 → LOD1 52. Küçük oldukları için sıraya alındı.

İlgili: [ADR 0057](0057-ayrinti-gecisi.md), [ADR 0059](0059-git-gecisi.md),
[ADR 0005](0005-asset-pipeline.md)
