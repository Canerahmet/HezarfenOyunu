# ADR 0063 — Boyuta duyarlı LOD merdiveni: bütçe sayısı yokluğu gizledi

**Tarih:** 2026-08-27
**Durum:** Kabul edildi (ölçüldü, teste bağlandı)
**Bağlam:** Faz 4 bütçe ölçümünün ortaya çıkardığı kusur

## Bulgu

Faz 4'ün bütçe ölçümü (ADR 0062) kule tepesinden 360° tarayıp **173 217
üçgen** verdi — 2,5 milyonluk bütçenin **%6,7'si**. İlk okuyuşta bu iyi bir
haberdi.

Değildi. Aynı karede yalnızca **472 renderer** çiziliyordu. 5 524 evi olan
iki semtte 472 nesne, verimlilik değil **yokluk** demekti.

Sebep, ADR 0061'in merdiveninin tek olmasıydı: `0,25 / 0,03 / 0,004`. Eşik
**ekran yüksekliği oranıdır** ve bu büyük yapılar için doğru davranır ama
ölçüldüğünde küçükler için felakettir:

| eşik | 10 m ev | 20 m mescit | 104 m cami |
|---|---:|---:|---:|
| 0,25 | 55 m | 110 m | 571 m |
| 0,03 | 458 m | 916 m | 4 762 m |
| 0,004 | **3 434 m** | 6 869 m | 35 717 m |

İki sayı bunu hemen mahkûm ediyor:

- **Planör 50–100 m'de uçuyor.** Ev yalnızca **55 m**'ye kadar tam
  ayrıntılı, yani şehir uçuş boyunca hep orta kademede.
- **Hezarfen'in uçuşu 3 336 m.** Ev **3 434 m**'de kül ediliyor — yani varış
  semti (Üsküdar) tam sınırda; uçuş sırasında yoktan var oluyor.

## Neden bütçe sayısı bunu gizledi

Çünkü ölçtüğüm şey **maliyetti**, görünürlük değil. Kül edilen nesne sıfır
üçgen katar ve bütçeyi rahatlatır. "%6,7 kullanım" cümlesi teknik olarak
doğruydu ve tamamen yanıltıcıydı.

Ders: **bir bütçe ölçümü tek başına yeterli değildir; yanında ne kadarının
çizildiği de sayılmalı.** Ucuz bir kare, boş bir kare olabilir.

## Ölçüm

Adaylar prefab'lara **uygulanmadan** ölçüldü (`CityBudget.Sweep` eşikleri
yalnız hesapta kullanır — 126 prefab'ı ölçüm için değiştirmek ve yanlış
çıkarsa geri almak gerekmesin diye):

| küçük eşik (boy < 40 m) | üçgen | çizilen | bütçe |
|---|---:|---:|---:|
| 0,25 / 0,03 / 0,004 *(eski)* | 168 073 | **472** | %6,7 |
| 0,15 / 0,02 / 0,0025 | 193 884 | 2 902 | %7,8 |
| **0,08 / 0,012 / 0,0015** | 214 708 | **3 194** | %8,6 |
| 0,05 / 0,008 / 0,001 | 337 520 | 3 238 | %13,5 |

472 → 2 902 sıçraması, kaç nesnenin **tümüyle kül edildiğini** gösteriyor.

## Karar

`ImportLanding.SetLodThresholds` eşiği artık nesnenin **boyuna göre** seçer:

| | LOD0 | LOD1 | kül |
|---|---|---|---|
| boy < 40 m (ev, dükkân, mezar, ağaç) | 0,08 | 0,012 | 0,0015 |
| boy ≥ 40 m (cami, sur, bedesten) | 0,25 | 0,03 | 0,004 |

Bir ev için bu, tam ayrıntının **172 m**'ye, orta kademenin **1 145 m**'ye
çıkması ve külün **9 158 m**'ye ötelenmesi demek. Planörün irtifası ve
Hezarfen'in menzili artık merdivenin içinde.

Neden 0,05 seçilmedi: 337 bin üçgen tek başına sorun değil ama ölçüm
**yalnızca iki semt** doluyken yapıldı. Suriçi tek başına Galata'nın 2,6
katı (1 067 ha); bütün semtler dolunca sayı büyüyecek. %8,6 o büyümeye yer
bırakır, %13,5 daha az bırakırdı.

Uygulama sonrası ölçüm: **242 176 üçgen, 3 924 renderer, %9,7**. Yani şehrin
5,4 katı çiziliyor, maliyeti %40 artış.

## Bekçi

`HeavyLandmarksHaveAMidLodAndAnExplicitLadder` iki merdiveni de tanıyor;
prefab'ın boyuna göre hangisini beklediğini bilir. Eşikler elle
düzenlenirse ya da boyut sınırı kayarsa kırılır.

İlgili: [ADR 0061](0061-lod-merdiveni.md), [ADR 0062](0062-semt-doldurma.md)
