
---

## Ek — 2026-09-01, 10. tur ölçümleri

Kapı hâlâ **0/21**, ama sebebi değişti ve artık üç sayıyla biliniyor.

### Süzülüş yarısı kapandı

Ortalama yatay mesafe **210 m → 1.437 m** (uçuş başına, 21 uçuş).
Sebep: iki tork terimi aynı anda çalışıyor — `pitchAuthority` (2,2)
hedefe çevirir, `pitchStability` (0,8) açıyı sıfıra geri çeker — ve
denge noktası `α = hedef × 2,2/3,0`. Yani kanat komut edilen açının
**%73'ünü** uçuyordu ve `BestGlideRatio`'nun 6,23°'sine hiç
ulaşmamıştı. Komut artık 1,364 ile ön-telafi ediliyor.

**Düzeltme:** 9. turda "menzil %15 arttı" diye kaydettim; gerçek kazanç
568 → 580 m, yani **12 metre**. İddiayı komut edilen açıların
L/D'sinden hesaplamıştım ve kanat o açıların hiçbirini uçmuyordu.

### Termik yarısı: kaldıraç var, ama yeri ve kotu uygun değil

İlk kez ölçüldü (kule çevresi 960×960 m, 1.156 örnek):

| ölçü | değer |
|---|---|
| En güçlü dikey rüzgâr | **1,99 m/s** @ (−480, 80, 120) |
| Ortalama | 0,36 m/s |
| Dönmeye değer eşik (türetilmiş) | **1,23 m/s** |
| Dönüşün net kazancı | **+0,76 m/s** |

Yani yükselmek **mümkün**. Üç şey engelliyor:

1. Kaldıraç hedefin **tersi** yönde (480 m batıda); uçuş önce geriye
   gitmeli.
2. En güçlü nokta **80 m irtifada** — yamacı sıyıran bir kot. Kuleden
   (100 m) oraya süzülmek 42 m yiyor, varışta 58 m kalıyor.
3. +0,77 m/s ile 430 m'ye çıkmak **8 dakika**. Denemenin süre tavanı
   300 s.

### Ölçüm aracının kendisinde bulunan dört kusur

- Pilot "havaya göre tırmanıyor muyum" diye soruyordu; süzülen kanat
  havaya göre **her zaman** batar (en az 0,94 m/s). Doğru soru "hava
  yükseliyor mu".
- Dönüş eşiği `const 2.12f` idi ve kendi belgesi "elle yazılmıyor
  olması önemli" diyordu. Türetilince **1,23** çıktı.
- Arama yarıçapı 120 m, kaldıraç 480 m ötede.
- Alan raporu süslü parantez eksikliğinden **doğru sayıları toplayıp
  yanlış sonucu yazıyordu** ("termikle yükselmek mümkün değil").

Deneme artık toplu kipten koşuyor; önceki hâlinde kapıyı tutan sayı,
kapıyı değiştiren commit'ten **üç commit eskiydi**.

### Caner'in kararı hâlâ bekliyor — ama seçenekler değişti

**(a) Kaldıracı güçlendir.** `TerrainThermal.peakLift` 2,6; ölçülen en
iyi 1,99. Tavanı yükseltmek ADR 0037'nin "kaldıraç arazinin
sonucudur" ilkesini zorlar.

**(b) Hedefi yaklaştır.** Doğancılar yerine Sarayburnu (~1.400 m):
bugünkü 1.437 m ile **kaldıraçsız geçilir**.

**(c) Uzun süzülüşü kabul et.** Sekiz dakikalık bir termik tırmanışı
oyunun doruk noktası olarak tasarlanır; deneme süresi 600 s'ye çıkar.

**Önerim (b).** Uçuşun kendisi artık çalışıyor ve 1.437 m gerçek bir
süzülüş; 3.336 m'yi tutturmak için ya arazinin fiziğini ya oyuncunun
sabrını zorlamak gerekiyor. Sarayburnu tarihsel olarak da savunulabilir
— rivayetin varış noktası tartışmalı ve `TepkiKodeksi` bunu zaten
söylüyor.

### Pilotu iyileştirmek kapıyı açmıyor — ölçüldü

Üç ayrı pilot düzeltmesinin ortalama yatay mesafeye etkisi:

| pilot | ortalama yatay | kazanç |
|---|---:|---:|
| Kaldıraç aramayan (eski, hatalı tırmanış testi) | **1.437 m** | 0 m |
| Düzeltilmiş tırmanış testi + türetilmiş eşik | 717 m | 0 m |
| + geniş arama (550 m) | 662 m | 0 m |
| + yavaşlama da eşiğe bağlı | **764 m** | 0 m |

Yani **doğru uçan bir pilot, yanlış uçan pilottan daha kısa gidiyor** —
çünkü bu arazide ulaşılabilir kaldıraç, ona gitmenin bedelini 300
saniyelik bütçe içinde geri ödemiyor. Kaldıracın var olduğu ölçüldü
(1,99 m/s, net +0,77), ama yeri (480 m batıda), kotu (80 m) ve
tırmanış süresi (8 dk) onu bu rota için kullanılamaz kılıyor.

**Sonuç:** kapı bir mühendislik sorunu değil, bir tasarım sorusu.
Yukarıdaki (a)/(b)/(c) seçeneklerinden biri seçilmeden 0/21 kalır ve
bu doğrudur — perde, olmayan bir başarıyı olmuş saymamalı.
