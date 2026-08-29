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

---

# İkinci tur — kule turu teşhis edildi

Kule turu tek kalan başarısızlıktı ve sebebi bulundu: **ağaçlar**.

## Yöne göre ölçmek

Tek bir p95 yetmiyordu; örnekleme penceresi değişince 15,6 ms'den
17,9 ms'ye kayıyordu — aynı sahne, aynı tur. Bir sayı "nerede pahalı"
sorusuna cevap vermiyor, dolayısıyla geçti/kaldı demeye de yetmiyor.

Alet artık turu **12 × 30° kovaya** ayırıyor. Tablo hemen konuştu:

| yön | 30° | 60° | 90° | 150° | **180°** | **210°** | 270° |
|---|---|---|---|---|---|---|---|
| ms | 6,8 | 7,0 | 6,9 | 14,2 | **17,0** | **16,6** | 12,4 |

Yönler arası **2,5 kat** fark. Medyan 91 FPS; p95'i tek başına
150°–240° sektörü belirliyor. Deniz yönü ucuz, kara yönü pahalı.

## Değişkenler tek tek elendi

| şüpheli | sonuç |
|---|---|
| gölge (güneş gölgesi) | 17,55 → 17,35 — **değil** |
| su yüzeyi (`WATER_Bogaz_Halic`) | 17,03 → 16,7 — **değil** |
| **ağaçlar** (`treeDistance = 0`) | **17,03 → 6,99** |

Ağaçlar kapalıyken **yön farkı da tümüyle kayboluyor** (5,1–6,6 ms, düz).
Yani hem maliyetin hem de yöne bağlılığın tek kaynağı onlar — mantıklı,
çünkü ağaçlar karada ve dağılımları yöne göre değişen tek büyük yük.

| | ağaçlı | ağaçsız |
|---|---|---|
| p95 | 17,03 ms | 6,99 ms |
| çizim çağrısı | 30 361 | 12 233 |
| üçgen | 1,93 M | 0,49 M |

Yani "boş arazide 19 600 çizim çağrısı" bulgusunun cevabı da bu: o
çağrılar ağaçlardı.

## Sorun üçgen değil, çizim çağrısı

Ağaç prefablarının son LOD'u zaten **80–84 üçgen** — uzakta üçgen
maliyeti yok. 42 857 ağacın getirdiği şey ~18 100 **çizim çağrısı**.

Denenen ve **işe yaramayan**: dört ağaç malzemesinde GPU örneklemesini
açmak. Çizim çağrısı kımıldamadı (30 361 → 30 361). p95 bir koşumda
15,90 çıktı ama tekrarında **17,54** — yani o "iyileşme" koşum
değişkenliğiydi. Mekanizma çizim çağrısında görünmediği için zaten
şüpheliydi.

**Bu adımın koşumlar arası değişkenliği ±1 ms.** Bunun altındaki hiçbir
fark iyileşme sayılamaz. (Aynı koşum içindeki iki özdeş adım ±0,1 ms
veriyor; belirsizlik koşumlar arasında.)

## Doğru çözüm: silmek değil, ucuzlatmak

Ağaçları uzakta çizdirmemek FPS verir ama bu bir kez denendi ve yanlıştı
(yukarıya bakınız — orman kayboluyordu). Doğru iş, siluet dururken
maliyeti düşürmek: uzak ağaçlar için **gerçek impostor/billboard LOD'u**
üretmek. Unity bunu SpeedTree olmayan varlıklar için kendiliğinden
yapmıyor; atlas ve son LOD kartı bizim üretmemiz gerekiyor.

Bu bir araç işi ve **sıradaki iş**.

---

# Üçüncü tur — örnekleme, on iki adımın on ikisi geçti

Ağaçlar GPU örneklemesiyle çiziliyor; arazinin kendi ağaç çizimi
kapatıldı. **Hiçbir ağaç silinmedi** ve **diske hiçbir şey yazılmadı** —
42 857 konumun hepsi arazi verisinden okunuyor (ADR 0073).

| adım | önce | sonra |
|---|---|---|
| boş arazi 1080p | 8,31 | **4,81** |
| boş arazi 1440p | 10,03 | **7,15** |
| 8000 yapı 1080p | 10,37 | **5,15** |
| SOKAK 1080p | 13,46 | **5,80** |
| çarşı 1080p | 14,46 | **6,99** |
| çarşı 1440p | 14,30 | **9,14** |
| **kule turu 1080p** | **17,83 ❌** | **7,23 ✅** |
| **kule turu 1440p** | **17,92 ❌** | **8,31 ✅** |

Yön farkı düzleşti: **4,7–7,1 ms** (önce 6,8–17,0). Boş arazide çizim
çağrısı 19 607 → **419**, üçgen 2,03 M → **0,53 M**.

Diskte üretilen: **sıfır bayt**. İlk iki deneme geometri üretiyordu ve
biri sahneyi 805 MB'a şişirdi (commit'lendi, geri alındı), öteki 900 MB
varlık yazdı. Ayrıntı ADR 0073'te.

## Açık maddeler

1. ~~Kule turu p95~~ — **çözüldü** (ağaç kümeleme, ADR 0073).
2. **Yakın ağaç LOD'u** — kümeler kaba LOD taşıdığı için yakındaki
   ağaçlar da kaba görünüyor. Yakın/uzak karışımı denendi, yürümedi
   (`treeDistance` ara değerlerde etkisiz; sebep açıklanamadı).
2. **Kalabalık üçgen bütçesi** — kişi başına 62 000.
3. **Taban çizim çağrısı** — boş arazide 19 600.
4. **Gerçek süreli oturum** — bu ölçüm 12 adım × 1200 kare. Otuz
   dakikalık kesintisiz oturum ayrı bir koşum ister ve build üzerinde
   yapılmalı.

---

# Post: film grain + ton eğrisi

PLAN Bölüm 12: *"hafif film grain + ton eğrisi (dönem gravür esintisi
**çok** hafif)"* — vurgu planın kendisinde.

## Grain ölçüyü şişiriyor mu — hayır, ama sorulmalıydı

Okunabilirlik ölçüsü (`SokakOkunabilirligi`) her pikselin 3×3 komşu
ortalamasından **sapmasını** sayıyor. Film grain tam olarak odur. Yani
ağır bir grain testi **sahte biçimde geçirebilirdi**.

Ölçüldü:

| grain | ayrıntı | ortalama |
|---|---|---|
| 0,00 | 2,24 | 79,2 |
| 0,12 | **2,25** | 79,0 |

Fark **+0,01** — bu şiddette grain ölçüyü kirletmiyor. Değer gözle değil
bu ölçümle seçildi.

## Ton eğrisi gölgeleri belirgin açtı

Neutral tonemapper eklendikten sonra:

| | önce | sonra |
|---|---|---|
| ortalama | 50,6 | **79,2** |
| koyu piksel (<30) | %29,8 | **%0,6** |

ACES seçilmedi: filmik ve kontrastlı, gravür değil sinema hissi verir.

**Gölge derinliği bir sanat kararıdır** ve öğle karesinde şu an epey
açık. İnceleme paketi üretildi (`Captures/mahalle/`, 8 kadraj × 2 an);
Caner'in notuyla yakınsanacak.

## Alçak güneşte gölge eziliyor mu — kalıcı ışıkta HAYIR

Gün batımı inceleme karesinde gölgeli kaldırım neredeyse siyah çıktı ve
ilk şüphem kalıcı pasın alçak güneşte çökmesiydi — geçici takımın
`FillScaleForAltitude`'u tam bunu telafi ediyordu ve onu silmiştim.

Ölçüldü, şüphe yanlış çıktı:

| güneş yüksekliği | ayrıntı | koyu |
|---|---|---|
| 63,9° (öğle) | 2,25 | %0,6 |
| **15,3° (alçak)** | **1,98** | %2,8 |

Eşik 1,2; alçak güneşte de geçiyor. Siyah kaldırım **inceleme aracının
kendi sabit poz taramasından** geliyor — çevrimdışı kare tekrarlanabilir
olsun diye poz çivileniyor (oyunda otomatik poz uyum sağlar). Yani
paketin gölgeleri gerçekte olduğundan **karanlık** gösteriyor; abartılı
değil, kötümser.

## Sessizce hiç çalışmayan kod

Grain ve ton eğrisi eklendikten sonra profilde **görünmediler**.
`Profil()` "bileşen varsa olduğu gibi döndür" diyordu; profil zaten üç
bileşenle vardı, yeni kod hiç koşmadı. Menü yine "kuruldu" dedi.

`Ensure<T>` zaten fikirsiz — varsa bulur, yoksa kurar. Artık her çağrıda
hepsi geçiliyor ve profil kendini onarıyor.

## Kaybolan alet

Ölçüm menüsü (`Sokak parlakligini olc`) geçici takım silinirken
**onunla birlikte gitti**: `Measure` taşındı ama menü sarmalayıcısı
taşınmadı. Tanı aracı sessizce yok oldu ve ancak ihtiyaç duyulunca
farkedildi. Geri kondu: `Hezarfen → Aydinlatma → Sokak okunabilirligini
olc`.
