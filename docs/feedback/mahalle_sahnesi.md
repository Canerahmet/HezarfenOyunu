# İnceleme — Galata mahallesi (Faz 2b KABUL sahnesi)

**Üretim:** 2026-08-24 · **ADR:** 0031 · **Sahne:** `Faz2_GalataSokagi.unity`

Bu, **Faz 2b'nin kabul ölçütü**. Onaylarsan Faz 2b kapanır.

## Bakılacak — 8 kadraj, iki ışıkta

Aynı kadrajlar iki anda birden çekildi; karşılaştırılabilirler.

| Kare | Ne gösteriyor |
|---|---|
| `01_cekirdek` | sokaktan avlu kapısı + merdiven + mescit |
| `02_sadirvan` | avlu içi: şadırvan + mescit cephesi |
| `03_cesme` | çeşme, göz hizasından |
| `04_dukkanlar` | dört dükkân, mescidin karşı sırası |
| `05_hazire` | 12 mezar taşı + servi + türbe, duvarlı |
| `06_kahvehane` | 1632 işareti: sundurma + seki + çınar |
| `07_sokak` | sokak koridoru — 40 m sonra kıvrılıp kapanıyor |
| `08_mahalle` | kuşbakışı: doku çekirdekten dallanıyor mu |

Dosyalar: `unity/HezarfenGame/Captures/mahalle/ogle_*.png` ve
`.../gunbatimi_*.png`.

**Öğle:** güneş 63,9°, tam güneyde, poz 12,5 EV.
**Gün batımı:** güneş 6,0°, azimut 284,6° (batı-kuzeybatı), poz 9,5 EV.
Gün batımı bir saat değil bir *yükseklik*; tarihten hesaplanıyor.

## Paketi üretmek üç kusur buldu

**(1) Kaldırım TERSTİ.** Yürünen yüzeyin 698 üçgeninin 697'si aşağı bakıyordu.
Sonucu üç katlı: yüzey üstten siyah okunuyordu, ışın sorguları arka yüzü
görmediği için **çarpıcı fiilen yoktu** (oyuncu kaldırımdan düşerdi) ve sokak
çimen görünüyordu. ADR 0016 turundan beri duruyordu; hiçbir kare göstermedi,
çünkü o turun bütün kareleri kaldırımın **altından** alınmıştı ve alttan
bakınca yüzey doğru görünüyor. Düzeltildi, teste bağlandı.

**(2) Göz hizası arazidan ölçülüyordu.** Mahallede yaya araziye basmaz —
kaldırıma ve taş kaideye basar, ikisi de arazinin üstünde. Kareler bu yüzden
kaldırımın altında çıkıyordu.

**(3) Dükkân sırası dört yerine ikiydi.** Dört slot deneniyor, elenen slot
kayboluyordu. Sebep çakışma değil sıra: sebil ve çeşme çekirdeğin çevresine
daha önce yerleşip ilk iki slotu kaplıyor. Artık dördü yerleşene kadar sokak
boyunca ilerleniyor.

## Sahneye eklenen tek yapı

**Bozahane.** 1638 sayımında şehirde 300 tane var — külliye yapısı değil,
mahalle dükkânı. Kahvehanenin yanına kondu, böylece oyunun **iki zaman
işareti** aynı sahnede duruyor: ikisi de 1632'de açık, ikisi de IV. Murad
döneminde kapanıyor. Bir 1633 sahnesi kurulursa ikisi birlikte kalkar.

Faz 2b'nin öteki altı yapısı bilerek girmedi: muvakkithane selâtin camisine
aittir, imaret külliyenin mutfağıdır, arasta bir sokak tipolojisidir, değirmen
dere ister, su terazisi Kırkçeşme hattını ister.

## Bildiğim eksikler — senin kararını isteyenler

**~~Karar 12~~ — KAPANDI (2026-08-24, Caner: "senin önerine bırakıyorum").**
**(a)** uygulandı: yerleştirici mahalleyi kurduğu anda, koyduğu yapıların
çakışma dairelerinden bir **yerleşim maskesi** yazıyor ve arazi örtüsünü o
bölgede yeniden boyuyor. Ölçüldü: çekirdekte toprak **1,00**, 60 m'de 0,89,
100 m'de 0,11 — doğal kurala dönüyor.

Maske bir **sınır iddiası değil**: kaynağı `districts.geojson` değil, sahneye
fiilen koyduğumuz yapılar. ADR 0024'ün reddettiği şey buydu ve reddi hâlâ
geçerli. Ayrıntı: **ADR 0032**.

Bu tur, bir satırlık sessiz bir felaketi de ortaya çıkardı: `alphamapResolution`
atamak — aynı değeri atasan bile — bütün splatmap'i siliyor. Kısmi boyama
eklenince bütün İstanbul toprağa düştü ve **kuşbakışı kare makul görünüyordu**;
yakalayan şey örtü testleri oldu (ot %0,02, kaya %0, kıyı %0).

**Diğerleri (karar istemeyen, kayda geçen):**

- Dükkân ve kahvehane **içleri kapkara**. Kutu kapalı; HDRP'de pişirilmemiş
  ışıkla kapalı iç mekân siyahtır. Kalıcı ışık pasının (Faz 5) işi.
- **Avlu payı dar:** şadırvanla kapı arası ölçüldü, **2,2 m**. `02` karesinin
  dar olması kadraj kusuru değil, avlunun ölçüsü. Genişletilebilir.
- **Şadırvanın musluk sırası yok** — külah ve tekne var. Yakın plan donatısı.
- **Geçici pozun kalibrasyonu eskimiş:** pozometre öğle için 12,5 EV diyor,
  takım 13,0 taşıyor. Kalıcı ışık pasında yeniden süpürülecek.
- **Sokak koridoru 40 m'de kapanıyor.** Bu bir kusur değil ölçü; kıvrımın
  sayısal karşılığı. Fazla mı az mı, kareye bakıp söyle.

## Onay

```
OK v1   — Caner, 2026-08-24: "devam edelim." (Faz 3'e geçiş onayı)
```

Bu, kabul ölçütünün yazılı onayı yerine geçiyor. Not **açık kalıyor**: yukarıdaki
kareler için sonradan düzeltme istersen v2 açılır ve Faz 3 buna engel değildir.

*(Karar 12 kapandı — 2026-08-24, ADR 0032.)*
