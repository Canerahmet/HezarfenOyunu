# Faz 7 — performans ölçümü (ilk tur)

**Tarih:** 2026-08-29
**Sahne:** `Bench_Galata_Ottoman` (8000 dağıtılmış + 400 yoğun sokak yapısı,
80 kişilik çarşı kalabalığı, gerçek `PF_House_A` ve `PF_Hezarfen_Sivil`)
**Donanım:** RTX 4070 Laptop, 8 GB VRAM (üst-orta segment)
**Kademe:** **Balanced** — planın "orta segment GPU" hedefi
**Ölçüt (PLAN Bölüm 12):** 1080p/60 ve 1440p/60 → **p95 ≤ 16,67 ms**

Ölçüm Editor Play modunda; bu **kötümser**dir — geçen bir adım gerçek
build'de de geçer.

---

## Sonuçlar (p95, düşük iyi)

| adım | 1080p | 1440p |
|---|---|---|
| boş arazi + su | 8,31 ✅ | 10,03 ✅ |
| 8000 yapı | 10,37 ✅ | 10,64 ✅ |
| SOKAK 8000+400 | 13,46 ✅ | — |
| çarşı kalabalığı (80 kişi) | 14,46 ✅ | 14,30 ✅ |
| **kule turu 360°** | **17,83 ❌** | **17,92 ❌** |

**On iki adımın on biri geçiyor.** Tek kalan kule turu.

---

## Bulgu 1 — SSGI tek başına bütçenin yarısı

Aynı sahne, tek fark SSGI:

| adım | SSGI açık | SSGI kapalı | fark |
|---|---|---|---|
| boş arazi 1440p | 16,94 | 10,03 | **−6,9 ms** |
| 8000 yapı 1440p | 17,24 | 10,64 | **−6,6 ms** |
| çarşı 1440p | 26,01 | 14,30 | **−11,7 ms** |

SSGI açıkken **boş bir yamaç bile** 1440p/60'ı kaçırıyordu. Maliyet
içerikten değil efektin kendisinden geliyor.

**Karar:** SSGI yalnız **High Fidelity**'de. Varsayılan, Balanced ve
Performant'ta kapalı. PLAN zaten *"SSGI/RT seçenekleri donanıma göre
kademeli"* diyordu; ölçüm o cümlenin bedelini gösterdi. Volume profili
SSGI'ı istemeye devam ediyor — sanat niyeti orada; kademeyi boru hattı
varlığı belirliyor. High Fidelity 1440p/60 **vermez** ve bu bilinerek
öyle.

## Bulgu 2 — kule turu çözünürlükten BAĞIMSIZ

| | medyan | p95 |
|---|---|---|
| kule turu 1080p | 10,62 | 17,83 |
| kule turu 1440p | 10,85 | 17,92 |

1440p, 1080p'nin 1,8 katı piksel; kule turunda fark **%0,5**. İş piksel
tarafında değil. Medyan 94 FPS, yani kapasite var — kaybeden şey
**takılmalar**: dönen kamera her karede görüş alanına yeni geometri
sokuyor (culling, LOD geçişi, gölge haritası).

Çözüm çözünürlük düşürmek olamaz. **Açık madde.**

## Bulgu 3 — kalabalık üçgen bütçesi

Çarşı adımında üçgen 2,74 M → **7,66 M**. Seksen kişi için +4,92 M, yani
**kişi başına ~62 000 üçgen**. Kare süresi şu an geçiyor ama bu bir
kalabalık figürü için fazla; karakterin kalabalık LOD'u ayrıca kurulmalı.

## Bulgu 4 — taban çizim çağrısı

Boş arazide ~19 600 çizim çağrısı (19 543'ü SRP Batcher'da, yani ucuz).
42 857 ağaç örneği var. GPU örnekleme açıldı (`drawInstanced`), görsel
bedeli yok. Çizim çağrısı sayısı buna rağmen kımıldamadı; kare süresi
zaten geçtiği için **şimdilik açık madde**.

---

## Yanlış kazanç: ormanı silerek hızlanmak

Bir tur boyunca ağaç mesafelerini kısalttım (`treeBillboardDistance`
3000 → 180 m) ve sayılar iyileşti. **İyileşme sahteydi.**

`GreeneryBuilder`'da bunun neden 3000 olduğu zaten yazılıydı ve o not da
bir ölçümün sonucuydu: bizim ağaçlar SpeedTree/Tree Creator değil, LOD
Group'lu normal prefablar. Unity onlar için billboard **üretmez** — o
mesafenin ötesinde billboard'a geçmezler, **tamamen kaybolurlar**. Yani
kare süresini ormanı çizdirmeyerek kazanmışım.

Geri alındığında sayılar **kımıldamadı**:

| | ağaç kısıtlıyken | geri alındıktan sonra |
|---|---|---|
| boş arazi 1440p | 10,05 | 10,03 |
| çarşı 1440p | 14,25 | 14,30 |
| kule turu 1080p | 17,99 | 17,83 |

Yani kazanç hiç orada değilmiş; **SSGI'yi kapatan kademe geçişinden**
geliyormuş. İki değişkeni aynı anda oynattığım için bir tur boyunca
yanlış şeye teşekkür ettim.

*Bir düzeltmenin hiçbir şeyi değiştirmemesi de bir ölçümdür* — burada
düzeltmenin geri alınması hiçbir şeyi değiştirmedi ve kazancın sahte
olduğunu böyle söyledi.

## Aletin kendisi de bir kez yanlış ölçtü

Kule turu ilk yazımda "saniyede 18 derece" dönüyordu; iki koşumda aynı
adım **28 652** ve **13 858** çizim çağrısı verdi.

Sebep: dönüş **saate**, örnek sayısı **kareye** bağlıydı. Kare hızı
değişince taranan yay da değişiyordu (360° değil ~216°) ve başlangıç
açısı ısınmanın süresine göre kayıyordu — iki koşum şehrin iki farklı
dilimine bakıp aynı sayı gibi karşılaştırılıyordu.

Şimdi açı **örnek ilerlemesinden** türüyor: pencere tam bir tur, her
koşumda aynı tur. Düzeltmeden sonra iki çözünürlüğün çizim çağrısı
30 365 ve 30 368 — aynı turu ölçtüklerinin kanıtı.

## Açık maddeler

1. **Kule turu p95** — 17,8-17,9 ms, iki çözünürlükte de. CPU/görünürlük
   tarafı; gölge ve culling incelenecek.
2. **Kalabalık üçgen bütçesi** — kişi başına 62 000.
3. **Taban çizim çağrısı** — boş arazide 19 600.
4. **Gerçek süreli oturum** — bu ölçüm 12 adım × 1200 kare. Otuz
   dakikalık kesintisiz oturum ayrı bir koşum ister ve build üzerinde
   yapılmalı.
