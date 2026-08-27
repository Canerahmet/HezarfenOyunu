# Geri bildirim günlüğü — 1632 Kıyı Çizgisi

Üretici: `tools/gis/coastline_build.py` · Artefakt: `refs/maps/coastline_1632.geojson`
Sahne: `Assets/_Project/Scenes/Faz1_Terrain.unity` → `GIS_1632`
Karar kaydı: [ADR 0008](../decisions/0008-coastline-1632.md)

---

## v1 — 2026-08-18 · **Caner'in kararı gerekiyor**

Önizleme: `data/gis/istanbul/preview_coastline.png`
(beyaz = modern kıyı, siyah kutular = 1632 düzeltme alanları)

### Ne yapıldı

Modern kıyı çizgisi **ölçüldü** ve araziyle tutarlı: 64,1 km, kıyı noktalarının 107/108'i
arazide ≤1,5 m. Bu katman sağlam.

### Neden takıldım

`docs/RESEARCH.md` §4 kıyı hakkında **tek satır** söylüyor:

> Eminönü–Sirkeci dolguları YOK; Haliç ve Marmara kıyısı bugünkünden içeride.
> Langa/Vlanga bostanları (eski Theodosius limanı dolmuş alanı) yeşil/bostan alanı.

Bu satır **"içeride" diyor, kaç metre içeride demiyor.** Alanların *yeri* güvenli;
*sınırları* değil. Metrik bir 1632 kıyısı çizersem, uydurduğum sayıları belgelenmiş
tarih gibi göstermiş olurum — planın Bölüm 2'deki kimlik ilkesine (T1/T2/T3 dürüstlüğü)
doğrudan aykırı.

Bu yüzden 5 alanı **kaba kutu** olarak bıraktım, hepsi T2 + `status: draft`.

### Taslak düzeltme alanları

| Alan | Eylem | Merkez (Galata'ya göre, m) | Güven |
|---|---|---|---|
| Eminönü–Sirkeci dolgusu | dolguyu geri al | (+156, −989) | kaba taslak |
| Galata / Karaköy kıyısı | dolguyu geri al | (+225, −354) | kaba taslak |
| Unkapanı kıyısı | dolguyu geri al | (−1267, −588) | kaba taslak |
| Langa / Vlanga | bostana çevir | (−1908, −2746) | **yeri güvenli**, sınırı taslak |
| Marmara kıyısı (Ahırkapı–Narlıkapı) | yalnızca inceleme | (−2126, −2784) | geometri düzeltmesi yok |

---

## Senden istediğim — üç seçenekten biri

**A) "Yaklaşık olsun, ilerle."**
Dolguları makul bir tahminle (ör. Eminönü–Sirkeci için 100–150 m içeri) geri alırım,
hepsi T2 kalır ve oyun içi Kodeks'te "rekonstrüksiyon" olarak gösterilir. En hızlısı.
Faz 2'nin şehir yerleşimi bunu bekliyor.

**B) "Önce kaynak bul."**
Plan Faz 1 madde 3 zaten dönem haritalarının georeferanslanmasını öngörüyor
(Müller-Wiener planı **yalnızca başvuru** — telifli, kopyalanmaz). Kıyıyı o adımdan
sonra kesinleştiririm. Daha doğru ama Faz 2'yi geciktirir.

**C) Elinde kaynak varsa ver.**
1632'ye yakın bir kıyı çizimi, dolgu tarihleri ya da Theodosius limanının dolma
sınırları hakkında bildiğin bir şey varsa, doğrudan uygularım.

> **Önerim: A.** Faz 2 (yapı kiti) kıyı hattına *yaklaşık* olarak bile bağlı değil —
> evler kıyıdan yüzlerce metre içeride. Kıyının kesinleşmesi asıl olarak rıhtım, iskele
> ve deniz surları yerleştirilirken (Faz 3) gerekiyor. O zamana kadar B'yi paralel
> yürütebiliriz.

### Notun

> **Caner, 2026-08-18:** *"makul bir tahminle geri alabilirsin."* → **A seçildi.**

---

## v2 — 2026-08-18 · Dolgular geri alındı

Paket: `data/gis/istanbul/preview_coastline.png`
(soluk gri = bugünkü kıyı, **parlak beyaz = 1632 kıyısı**, siyah kutular = düzeltme alanları)

### Yöntem — sabit metre ofseti kullanmadım

"Makul tahmin" için akla ilk gelen şey, kıyıyı sabit bir mesafe içeri kaydırmaktı.
Bunu yapmadım çünkü o sayı tamamen benim uydurmam olurdu ve haritanın hiçbir yerinden
denetlenemezdi.

Onun yerine tahmini **araziden türettim**: modern dolgu alanları yapay olarak düz ve
alçaktır; doğal kıyı, arazinin yükselmeye başladığı yerdir. Dolgu bölgelerinde "deniz
sayılan irtifa" 0,5 m'den **5,0 m**'ye çıkarıldı ve kıyı doğal yamacın eteğine geri
çekildi. Aynı tek değer bütün alanlarda kullanıldı — alan alan ayar yapmak, kaynağı
olmayan sayılara sahte bir kesinlik verirdi.

**Kayma miktarı seçilmedi, ÖLÇÜLDÜ:**

| Alan | Geri alınan | Kıyı ne kadar içeri çekildi |
|---|---|---|
| Eminönü–Sirkeci | 18,6 ha | **~98 m** |
| Unkapanı | 12,7 ha | **~99 m** |
| Marmara kıyısı | 30,9 ha | **~49 m** |
| Galata / Karaköy | 7,4 ha | **~35 m** |

Karaköy'ün az çıkması doğru: Galata sırtı dik, doğal yamaç suya yakın başlıyor.

### Langa'ya dokunmadım — ve bu bir hatayı yakaladı

Langa modern bir dolgu **değildir**: Theodosius limanı Osmanlı döneminden çok önce dolup
bostana dönüşmüştü. Ölçüm de bunu doğruluyor (medyan irtifa 4,6 m — dolmuş liman tabanı).
Buraya dolgu eşiği uygulasaydım, 1632'de bostan olan alanı **yeniden denize** çevirirdim.

Bunu baştan işaretlemiştim ama ilk koşuda yine de su bastı: **Langa, Marmara düzeltme
alanının tamamen içinde**, ve eşik alanı maksimum aldığı için Marmara'nın 5 m'si benim
muafiyetimi eziyordu. Önizlemede Langa kutusunun içinde kapalı bir su halkası belirdi.
Düzeltildi (koruma pası birleştirmeden sonra çalışıyor) ve regresyon testi yazıldı.

### Katmanlar

`refs/maps/coastline_1632.geojson` artık üç katman taşıyor:

| Katman | Ne | Uzunluk |
|---|---|---|
| `modern_shoreline` | bugünkü kıyı — kıyas için saklanıyor | 64,1 km |
| `shoreline_1632` | **oyunun kıyısı** (T2) | 65,0 km |
| `correction_zone` | 5 alan, her biri ölçülen kayma değeriyle | — |

Her düzeltme alanı yöntemi, eşiği ve ölçülen kaymayı kendi içinde taşıyor; dosya elden
ele geçtiğinde "bu 1632 kıyısı nereden geldi" sorusu dosyanın kendisinden cevaplanıyor.

### Hâlâ T2, hâlâ taslak

Bu bir **rekonstrüksiyondur**, belge değil. RESEARCH.md hâlâ metrik ofset vermiyor;
yalnızca tahminin *yöntemi* artık savunulabilir ve denetlenebilir. Plan Faz 1 madde 3'teki
dönem haritası georeferanslaması geldiğinde bu sayılar sınanacak.

### Notun

<!-- Onay: "OK v2" -->

_(bekliyor — ama beni bloke etmiyor)_
