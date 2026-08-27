# ADR 0029 — Her sınırın dayanağı yazılır; sınırlar türetilir

**Tarih:** 2026-08-23
**Durum:** Kabul edildi — üretildi, testli, Caner onayı bekliyor
**Tetikleyen:** Caner: *"daha tutarlı olması bakımından toptan yapalım"* (Karar 11)
**İlgili:** ADR 0026 (yeşil doku), 0028 (menzil yönü), 0008 (GeoJSON), 0007 (GIS)

---

## 1. Sorun tek bir poligon değildi, YÖNTEMDİ

ADR 0026'da on bir alanın hepsini aynı şekilde çizdim: kaynak varlıklarını
belgeliyor ama sınırlarını belgelemiyor, ben de kaba kutu geçtim ve
`status: draft` işaretledim. Kural gereğiydi (CLAUDE.md: *"kaynak niteliksel
olduğunda metrik geometri UYDURMA"*) ve o kadarıyla doğruydu.

Sonra bir tanesinde gerçekten ölçülmüş bir sayı çıktı (ADR 0028): Okmeydanı
≈4,9 km², benim taslağım 2,74. Yöntem tek sınandığı yerde **yarı yarıya**
şaşmıştı. Kalan on için sınama yoktu.

Bu tur hepsini birden ele aldı. Ve iyi ki: ikinci çapa çıktığında hata **ters
yöndeydi ve çok daha büyüktü.**

## 2. Bulunan çapalar

| Alan | Taslak | Belgeli | Fark |
|---|---|---|---|
| Okmeydanı | 274 ha | **490 ha** | yarı yarıya küçük |
| **Galata surları içi** | **216 ha** | **37 ha** | **altı kat büyük** |
| Sur içi | 1097 ha (elle kutu) | 1334 ha (sur çizgisi) | sur ile bağımsızdı |
| Karacaahmet | 76 ha | 75 ha (üst sınır) | zaten tavanda |

Galata rakamı bu turun gerekçesinin kendisi: Ceneviz surları ~2800 m çevre ile
~37 ha'lık bir alanı çevreliyordu; benim "Galata surları içi" kutum 216 ha'ydı.
Bir alanda yaptığım hatanın (yarım) bir başkasında **ters yönde ve altı kat**
olması, tek tek düzeltmenin neden yetmeyeceğini gösteriyor.

`walls_build` da aynı çapaya oturtuldu: elle çizilen halka 53 ha ölçülüyordu,
belgeli 37 ha'ya ölçeklendi. Poligonun **biçimi** hâlâ kaba; düzeltilen şey
**büyüklüğü**. Sınırın şekli bize, ölçüsü kaynağa ait.

## 3. Sınırlar artık ÇİZİLMİYOR, TÜRETİLİYOR

Asıl yapısal değişiklik bu. Her alan bir `basis` taşıyor:

| `basis` | ne demek | kaç alan |
|---|---|---|
| `documented` | Yayımlanmış alan ölçüsüne **oturtulur** (ağırlık merkezine göre ölçeklenip doğrulanır) | 2 |
| `walls` | Sınır **sur çizgisinin kendisidir** — ayrı bir çizim yok | 3 |
| `terrain` | Sınırı arazi tanımlar; iddia **ölçülür** | 5 |
| `drawn` | Çapası yok; kaba kutudur ve öyle olduğunu söyler | 1 |

**Sur içi** artık elle çizilmiş bir kutu değil: kara surları + Marmara ve Haliç
deniz surlarının birlikte kapattığı halka. İki geometri aynı kaynaktan geliyor,
yani bir gün ayrışmaları imkânsız. **Yedikule bostanları** da öyle — "Yedikule
ile Topkapı arasında" cümlesi artık sur verisindeki iki KAPIDAN okunuyor ve
şerit sur çizgisinden türetiliyor.

`GreeneryTests.WallBackedBoundariesMatchTheWalls` bu iddiayı ölçüyor: `walls`
diyen her poligonun her köşesi sur hattından **1 m'den yakın** olmalı. İki
dosya ayrı üretiliyor; iddia bir gün sessizce yalan olabilirdi.

### Sur içi neden bugünkü Fatih'ten küçük

Ölçülen 1334 ha, bugünkü Fatih ilçesi 1562 ha. Bu bir hata değil **beklenen
fark**: Marmara ve Haliç kıyıları 20. yüzyılda dolduruldu. Yani iki sayının
tutmaması değil, tutması şaşırtıcı olurdu.

## 4. "Arazi tanımlıyor" demek bedava değil

`terrain` dayanağı bir iddiadır ve iddia ölçülüyor:

* **Eyüp** — kaynak "bir tepenin iki yamacı" diyor → kot farkı ≥ 40 m.
* **Kağıthane** — dere boyu **çayır** → ortalama kot ≤ 45 m.
* **Göksu** — iki dere arası 500–600 m → dar kenar ölçülüp bandın içinde mi.
* **Langa** — dolmuş liman havzası → alçak (≤12 m) **ve sur içinde**.
* **Pera bağları** — "Galata'nın üst surlarının ötesi" → hiçbir köşe sur
  poligonunun içinde olmamalı.

**İkisi ilk koşuda düştü** ve alet tam bunun için vardı:

**(a) Kağıthane ortalama kotu 46 m çıktı.** Kutu çayırda değil, vadinin
yamaçlarındaydı. "Vadi tabanında" demek, kutuyu vadi tabanına koymakla aynı
şey değilmiş. Sınır artık çizilmiyor, **DEM'den izleniyor**: her enlemde
vadinin en alçak *kara* noktası bulunup iki yana 290 m açılıyor. Sonuç
kutudan çok daha iyi okunuyor — dere boyunca akan bir çınar şeridi.

**(b) Langa'nın iki köşesi surun dışında,** yani denizdeydi. Marmara deniz
suru o boylamlarda ölçüldü ve poligon içeri çekildi.

## 5. En alçak nokta ile en alçak KARA farkı

Vadi izleme ilk yazımda "en alçak DEM değeri"ni arıyordu ve Kağıthane
vadisinde **DEM'in taban kotuna (−12 m) oturmuş 60 × 80 m'lik bir yamaya**
kilitlendi: deniz doldurması dere ağzından yukarı kaçmış. Mesire bir su
birikintisinin etrafına diziliyordu.

Çayır karadadır — eksen artık yalnızca ≥1 m kotundaki noktalara bakıyor.

**Ama havuz duruyor.** O bir ARAZİ kusurudur, sınır kusuru değil (28,95632 D /
41,06725 K, ~60 × 80 m, kot −12 m) ve sahibi ADR 0007'dir. Susmak yerine
sayıya döküldü: `greenery_build` artık her alanın içindeki deniz seviyesi altı
hücreyi sayıp `[ICINDE n SU HUCRESI]` diye yazıyor. Kıyı alanlarında bu normal
(Eyüp 233, Göksu 136); Kağıthane'de 10 kalması ise anormal ve orada duruyor.

## 6. Yan kazanç: bütün GIS araçları geri geldi

`walls_build` bu turda gerektiği için rasterio'dan koparıldı — ve aynı tek
satır `coastline_build`, `districts_build`, `landmarks_build`, `dem_probe`
için de geçerliydi. Beşi de artık çalışıyor.

Bunun için `geodesy`'ye **ters** dönüşüm eklendi (`from_utm35n`): metrik
düzeltmeyi coğrafi koordinata geri yazabilmek şarttı. Doğruluğu kendi kendini
sınıyor — ileri-geri giden bir nokta başladığı yere dönmeli; şehrin dört
köşesinde ölçülen kapanma **0,29 mm**.

Somut sonuç: bir yıldır koşulamayan `dem_probe` koştu ve DEM'in
georeferansını yedi noktada doğruladı (hepsi toleransta). `[İNSAN]` maddesi
artık yalnızca `dem_fetch` (COG indirme) ve `map_overlay` (raster yazma) için
geçerli.

## 7. Kalan boşluklar

- **Üsküdar** için çapa bulunamadı; tek `drawn` alan odur ve öyle yazıyor.
- **Mesire, bostan ve bağ** için alan ölçüsü yok ve arama sonuç vermedi.
  Bostan literatürünün kendi ifadesi: kayıtlar kira geliri ve adet tutar, alan
  tutmaz. Bunların büyüklüğü hâlâ tahmindir; değişen şey, neyin tuttuğunun
  yazılı ve ölçülür olması.
- **Karacaahmet bugünkü alanına oturtuldu** — bu 1632 ölçüsü değil ÜST
  SINIRDIR; oradaki servi sayısı bir tavandır.
- **Kağıthane havuzu** (ADR 0007'nin işi) duruyor.
- Yedikule bostan şeridinin **eni** (190 m) tahmindir; güzergâhı belgeli.
