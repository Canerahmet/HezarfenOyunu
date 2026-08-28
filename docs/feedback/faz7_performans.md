# Faz 7 — performans ölçümü (ilk tur)

**Tarih:** 2026-08-29
**Sahne:** `Bench_Galata_Ottoman` (8000 dağıtılmış + 400 yoğun sokak yapısı,
80 kişilik çarşı kalabalığı, gerçek `PF_House_A` ve `PF_Hezarfen_Sivil`)
**Donanım:** RTX 4070 Laptop, 8 GB VRAM (üst-orta segment)
**Işık:** kalıcı pas (APV + sis + otomatik poz), SSGI **kapalı**
**Ölçüt (PLAN Bölüm 12):** 1080p/60 ve 1440p/60 → **p95 ≤ 16,67 ms**

Ölçüm Editor Play modunda yapıldı; bu **kötümser**dir — geçen bir adım
gerçek build'de de geçer.

---

## Sonuçlar (p95, düşük iyi)

| adım | 1080p | 1440p |
|---|---|---|
| boş arazi + su | 9,16 ✅ | 12,24 ✅ |
| 8000 yapı | 11,18 ✅ | 11,74 ✅ |
| SOKAK 8000+400 | 15,04 ✅ | — |
| çarşı kalabalığı (80 kişi) | 15,05 ✅ | **17,50 ❌** |
| **kule turu 360°** | **18,29 ❌** | **18,86 ❌** |

**1080p:** kule turu dışında hepsi geçiyor.
**1440p:** kule turu ve çarşı kalabalığı kalıyor.

---

## Bulgu 1 — SSGI bütçenin yarısını yiyor

Aynı sahne, tek fark SSGI:

| adım | SSGI açık | SSGI kapalı | fark |
|---|---|---|---|
| boş arazi 1440p | 16,94 | 12,24 | **−4,7 ms** |
| 8000 yapı 1440p | 17,24 | 11,74 | **−5,5 ms** |
| çarşı 1440p | 26,01 | 17,50 | **−8,5 ms** |

SSGI açıkken **boş arazi bile** 1440p/60'ı kaçırıyordu (16,94). Yani
maliyet içerikten değil, efektin kendisinden geliyor.

**Karar:** SSGI yalnız **High Fidelity** kademesinde. Varsayılan, Balanced
ve Performant'ta kapalı — PLAN zaten *"SSGI/RT seçenekleri donanıma göre
kademeli"* diyordu; ölçüm o cümlenin neden yazıldığını gösterdi. Volume
profili SSGI'ı istemeye devam ediyor (sanat niyeti orada duruyor);
kademeyi boru hattı varlığı belirliyor.

## Bulgu 2 — kule turu ÇÖZÜNÜRLÜKTEN bağımsız

| | medyan | p95 |
|---|---|---|
| kule turu 1080p | 11,56 | 18,29 |
| kule turu 1440p | 11,82 | 18,86 |

1440p, 1080p'nin 1,8 katı piksel demek. Kule turunda fark **%3**. Bu, işin
piksel tarafında olmadığını söylüyor: darboğaz **CPU**, muhtemelen dönen
kameranın her karede yeni nesneleri görüş alanına sokması (culling, LOD
geçişleri, gölge haritası güncellemesi).

Medyan iyi (86 FPS); sorun **takılmalar**. Yani çözüm çözünürlük düşürmek
değil; görünürlük ve gölge tarafında.

Bu bir sonraki turun işi ve **açık madde** olarak duruyor.

## Bulgu 3 — kalabalık üçgen bütçesini patlatıyor

Çarşı adımında üçgen sayısı 2,74 M → **7,66 M**. Seksen kişi için
+4,92 M, yani **kişi başına ~62 000 üçgen**. Bir kalabalık figürü için
çok fazla; karakterin LOD merdiveni kalabalık için ayrıca kurulmalı.

## Bulgu 4 — boş arazi 19 825 çizim çağrısı

Sahnede tek bir bina yokken bile ~19 800 çizim çağrısı var (19 543'ü SRP
Batcher'da, yani ucuz). Bina eklemek bunu 26 576'ya çıkarıyor — yani
**tabanın kendisi** şehirden pahalı. Kaynağı arazi ayrıntısı/bitki
örtüsü; incelenecek.

---

## Aletin kendisi bir kez yanlış ölçtü

Kule turu ilk yazımda "saniyede 18 derece" dönüyordu ve iki koşumda aynı
adımın çizim çağrısı **28 652** ve **13 858** çıktı — yani ölçüm
tekrarlanamazdı.

Sebep: dönüş **saate**, örnek sayısı **kareye** bağlıydı. Kare hızı
değişince taranan yay da değişiyordu (360° değil ~216°) ve başlangıç açısı
ısınmanın ne kadar sürdüğüne göre kayıyordu. İki koşum şehrin iki farklı
diliminden bakıyor, sonra aynı sayıymış gibi karşılaştırılıyordu.

Şimdi açı **örnek ilerlemesinden** türüyor: pencere tam bir tur, her
koşumda aynı tur. Düzeltmeden sonra iki çözünürlüğün çizim çağrısı 30 583
ve 30 665 — aynı turu ölçtüklerinin kanıtı.

## Açık maddeler

1. **Kule turu p95** — CPU/görünürlük tarafı; 1080p'de bile 18,29.
2. **Çarşı 1440p** — 17,50; karakter LOD'u ve kalabalık üçgen bütçesi.
3. **Taban çizim çağrısı** — boş arazide 19 825.
4. **Gerçek süreli oturum** — bu ölçüm 12 adım × ~1200 kare. Otuz
   dakikalık kesintisiz oturum ayrı bir koşum ister ve build üzerinde
   yapılmalı.
