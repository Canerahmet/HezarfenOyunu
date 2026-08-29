# Faz 7 kapısı — görsel cila ve performans

**Tarih:** 2026-08-29
**Ölçüm:** 340 EditMode + 32 PlayMode testi, hepsi yeşil
**Donanım:** RTX 4070 Laptop, 8 GB VRAM (üst-orta segment)
**Kademe:** Balanced (planın "orta segment GPU" hedefi)
**Kapıyı tutan:** ölçüm (onay akışı: geri bildirim tüm fazlardan sonra,
oyun oynanırken)

---

## PLAN Bölüm 12 madde madde

| # | İstenen | Sonuç |
|---|---|---|
| 1 | **İLK İŞ:** geçici aydınlatma takımını SİL | ✅ silindi; kalıcı pas yerine kuruldu (ADR 0072) |
| 2 | Fiziksel gökyüzü + saat sistemi | ✅ güneş `ZamanSistemi`'nden, poz otomatik/fizikî |
| 3 | Lodoslu hava profili (bulut, dalga, ağaç senkron) | ✅ tek vektör, Osmanlı rüzgâr gülü |
| 4 | Volumetrik sis (Haliç sabahı) | ✅ taban 0 m, tavan 140 m |
| 5 | SSGI/RT kademeli | ✅ yalnız High Fidelity; bedeli ölçüldü |
| 6 | Post: hafif grain + ton eğrisi | ✅ grain 0,12 (ölçülerek) |
| 7 | 1080p/60 ve 1440p/60 | ✅ 12/12 adım |
| 8 | Otomatik FPS raporu (benchmark sahneleri) | ✅ kule turu + çarşı kalabalığı |
| 9 | Açılış akışı + build sahne listesi | ✅ menü → yükleme → şehir |

---

## Performans (p95, hedef ≤ 16,67 ms)

| adım | 1080p | 1440p |
|---|---|---|
| boş arazi + su | 4,81 | 7,15 |
| 8000 yapı | 5,15 | 7,36 |
| SOKAK 8000+400 | 5,80 | — |
| çarşı kalabalığı (80 kişi) | 6,99 | 9,14 |
| kule turu 360° | **7,23** | **8,31** |

Başlangıçta kule turu 17,83 ve 17,92 ile **kalıyordu**. Tek adım değil,
on iki adımın on ikisi geçiyor.

## Uzun oturum (10 dakika, 60 000 kare, her şey açık)

| ölçü | değer |
|---|---|
| medyan | 7,44 ms (134 FPS) |
| p95 | **9,68 ms** (103 FPS) |
| en kötü kare | 34,26 ms (tek takılma) |
| **sürüklenme** | 6,92 → 7,70 ms (**+0,78 ms**) |
| yön yayılımı | 6,3–9,4 ms (düz) |

Sürüklenme on dakikada %11. Bir **sızıntı** olsaydı p95 ve en kötü kare
de birlikte büyürdü; büyümediler. Muhtemelen dizüstü GPU'nun ısı
davranışı. Doğrusal uzatılırsa otuz dakikada medyan ~9,7 ms — hedef
yine tutar.

**Bu hâlâ gerçek bir oturum değil:** Editor Play modunda, oyuncu girdisi
olmadan, tek sahnede. Otuz dakikalık elle oynanan oturum Faz 8'in
(paketleme) build'i üzerinde yapılmalı ve onay akışı da zaten oraya
işaret ediyor.

---

## Ölçümün bulduğu şeyler

**SSGI bütçenin yarısını yiyordu.** Boş arazide 6,9 ms, kalabalıkta
11,7 ms. Açıkken **boş bir yamaç bile** 1440p/60'ı kaçırıyordu. Artık
yalnız High Fidelity'de ve o kademenin 1440p/60 vermediği bilinerek öyle.

**Kule turunun tamamı ağaçlardı.** Gölge değil (17,55 → 17,35), su değil
(17,03 → 16,7), ağaçlar (17,03 → **6,99**). 42 857 ağaç, ~18 100 çizim
çağrısı. Üçgen değil, çizim sayısı — son LOD zaten 80 üçgen.

**Ağaçlar artık örneklemeyle çiziliyor**, diskte sıfır bayt üretilerek
(ADR 0073). Boş arazide çizim çağrısı 19 607 → **419**.

**Boş bir profil, olmayan profilden kötüdür.** Kalıcı pasın ilk
kurulumunda `VolumeProfile.Add<T>` bileşenleri diske yazmıyordu; dosya
oluştu, menü "kuruldu" dedi, içi boştu. Ölçüm 2,62'den 0,55'e düştü ve
sebebi sıçrama sandım.

---

## Kaydedilen hatalar

Bu fazda dört kez yanlış şeye baktım; hepsi ölçümle düzeldi.

1. **Ormanı silerek hızlandım.** Ağaç çizim mesafesini kısalttım, sayılar
   iyileşti, "iyileştirme" diye yazdım. Ağaçlarımız SpeedTree değil;
   billboard'a geçmiyorlar, **kayboluyorlar**. Geri alınca sayılar
   kımıldamadı — kazanç zaten kademe geçişindenmiş.
2. **Sahneyi 805 MB'a şişirdim ve commit ettim.** Birleştirilmiş
   mesh'ler varlık değildi, Unity onları sahneye gömdü. Push edilmemişti;
   `reset` ile çıktı. İkinci deneme (varlık olarak yazmak) aynı şişkinliği
   klasöre taşıdı (~900 MB). Doğrusu hiç üretmemekti.
3. **Alet 360° yerine ~216° tarıyordu.** Dönüş saate, örnekleme kareye
   bağlıydı; iki koşum şehrin iki farklı dilimine bakıp aynı sayı gibi
   karşılaştırılıyordu. Açı artık örnek ilerlemesinden türüyor.
4. **Genel sayaca bakan test** yeni bir test sınıfı eklenince patladı —
   saydığı şey kendi sızıntısı değil, önceki testlerden kalanlardı.

---

## Açık maddeler (Faz 8'e)

1. **Yakın ağaç LOD'u** — çizici her mesafede kaba LOD kullanıyor.
   Hücre başına LOD seçimi mümkün, yapılmadı.
2. **Gölge derinliği** — ton eğrisi öğle gölgelerini epey açtı (koyu
   piksel %29,8 → %0,6). Sanat kararı; inceleme paketi `Captures/mahalle/`
   altında, Caner'in notuyla yakınsanacak.
3. **Ağaç savrulması** — rüzgâr kancası yayınlıyor, malzeme okumuyor.
   Vertex shader gerekiyor.
4. **Kalabalık üçgen bütçesi** — kişi başına ~62 000 üçgen.
5. **Gerçek 30 dk oturum** — build üzerinde, elle.
6. **Arap Camii yönü** — araziden geliyor, kaynaktan değil (ADR 0071).
7. **Eyüp'ün Cuma camisi yok** (ADR 0071).
8. **Üç iskele yalnız kayıkla erişilebiliyor** (Faz 6'dan devir).

## Onay

Caner: *(bekliyor — onay akışı tüm fazlardan sonra, oyun oynanırken)*
