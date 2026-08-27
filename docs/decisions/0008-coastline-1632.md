# ADR 0008 — 1632 Kıyı Çizgisi ve GeoJSON → Sahne Aktarımı

**Tarih:** 2026-08-18
**Durum:** Kabul edildi · geometri **T2 rekonstrüksiyon**
**Güncelleme 2026-08-18:** Caner *"makul bir tahminle geri alabilirsin"* dedi (A seçeneği);
dolgular geri alındı — bkz. aşağıdaki "Düzeltmelerin uygulanması" bölümü.
**Karar veren:** Claude (Caner projeyi tamamen devretti)
**İlgili:** plan Görev 10, Faz 1 madde 2 ve 4; ADR 0007

## Bağlam

Plan Faz 1 madde 2: *"Modern kıyıdan başla (OSM), RESEARCH.md kaynaklarına göre düzelt:
Eminönü/Sirkeci/Unkapanı dolgularını geri al, Langa'yı bostan yap, Haliç kıyısını daralt."*

Elimizdeki tarihsel dayanak `docs/RESEARCH.md` §4'ün **tek satırıdır**:

> Kıyı çizgisi: Eminönü–Sirkeci dolguları YOK; Haliç ve Marmara kıyısı bugünkünden
> içeride. Langa/Vlanga bostanları (eski Theodosius limanı dolmuş alanı) yeşil/bostan alanı.

Bu satır **nitelikseldir**. "İçeride" der, kaç metre içeride demez. Dolayısıyla metrik bir
1632 kıyı çizgisi üretmek, elimizdeki kaynakla mümkün değildir. Bu ADR'nin asıl kararı
budur: **uydurmuyoruz, sınırı işaretliyoruz.**

## Karar

İki katman üretilir ve **kesin olarak ayrı tutulur**:

| Katman | Ne | Dayanak |
|---|---|---|
| `modern_shoreline` | Bugünün kıyısı, 64,1 km | Copernicus DEM, ölçüm |
| `correction_zone` | 1632'de farklı olduğu **bilinen** 5 alan | RESEARCH.md §4 — niteliksel |

`correction_zone` geometrileri **kaba kutulardır**, `status: draft`, hepsi T2. Alanların
*yeri* güvenlidir (Eminönü, Unkapanı, Galata/Karaköy, Langa, Marmara kıyısı); *sınırları*
değildir. Kesinleştirme yolu: dönem haritalarının georeferanslanması (plan Faz 1 madde 3).

### Neden OSM değil, DEM konturu (plandan sapma)

Plan "modern kıyıdan başla (OSM)" diyor. OSM daha keskin bir kıyı verir. Yine de DEM
konturu seçildi:

**Kıyı çizgisi ile arazi aynı kaynaktan gelmezse birbirini tutmaz.** Deniz düzlemi karayı
keser ya da kıyıda görünmez bir uçurum kalır. Oyuncu suyun üstünde uçtuğu için bu
tutarsızlık doğrudan görünür — ve düzeltmesi, iki ayrı veri kümesini elle hizalamak
demektir.

Ölçüldü: **kıyı noktalarının 107/108'i arazide ≤1,5 m** yükseklikte (en kötü 1,62 m).
OSM ile bu garanti edilemezdi.

Ek fayda: ODbL atıf yükümlülüğü henüz doğmadı. OSM rafinasyon yolu olarak açık;
o gün geldiğinde `refs/LICENSES.md`e satır eklenir.

### Deniz, "alçak her yer" değildir

İlk sürüm 0,5 m eş yükselti eğrisini doğrudan kıyı saydı. Sonuç: çizgi Haliç'in başından
kuzeye, Kâğıthane deresi vadisi boyunca kilometrelerce içeri uzadı; denizle hiç bağlantısı
olmayan alçak düzlükler de "kıyı" oldu.

Düzeltme: Boğaz ortasından **taşkın doldurma** (tarama-satırı) ile bağlantılı deniz kütlesi
bulunur, kontur o ikili maskeden çıkarılır. Kıyı çizgisinin tanımı zaten budur: *"buraya
sudan yüzerek gidilebilir mi?"*

Ölçüm ilginç çıktı: alçak alanların **%99,9'u** zaten denize bağlıydı. Yani Kâğıthane
uzantısı bir kusur değil, gerçek bir haliçtir — 1632'de de kayıkla çıkılan, mesire olan bir
sulak alan (RESEARCH.md §4). Doldurma yalnızca izole iç çukurları eledi, ama tanımı
sağlamlaştırdı.

### Projeksiyon dönüşümü Unity'de YAPILMAZ

`refs/maps/coastline_1632.geojson` WGS84'tür (RFC 7946) — insan ve QGIS için doğru biçim.
Unity onu okumaz: enlem-boylamı metreye çevirmek bir projeksiyon kütüphanesi ister ve
Unity'de böyle bir şey yoktur. Dönüşüm, zaten rasterio'ya sahip Python tarafında yapılır;
Unity `data/gis/istanbul/coastline_1632_local.json` (yerel metre) okur.

Böylece projeksiyon mantığı **tek yerde** yaşar. İki yerde yaşasaydı, ikisi kaçınılmaz
olarak ayrışırdı (ADR 0007'deki Galata koordinatı vakası aynı hatanın küçük hâliydi).

### Görselleştirme gizmo iledir

`GisFeature` gizmo çizer, `LineRenderer` kullanmaz. Bunlar oyuncunun göreceği geometri
değil, **üretim referansıdır**. LineRenderer HDRP'de malzeme ister, build'e sızar ve
yanlışlıkla oynanışa karışabilir; gizmo yalnızca Editor'de yaşar.

Spline'a şimdilik çevrilmedi. Plan "marker/spline/bölge" diyor ama spline'ın tüketicisi
Faz 2'nin sokak/rıhtım yerleştiricisidir. Tüketicisi olmayan bir soyutlamayı şimdi kurmak
spekülatif iş olurdu; noktalar `GisFeature.points` içinde duruyor, spline gerektiğinde
oradan üretilir.

## Yaşanan hata: sessizce sıfırlanan geometri

Unity'de tüm noktalar `(0,0,0)` çıktı. Sahne "başarıyla" kuruldu, 7 öğe oluştu, hata yok.

Sebep: Python'un `json.dump`ı varsayılan olarak `"x": -6306.5` yazar — iki nokta üstünden
sonra **boşluk** vardır. Elle yazılmış okuyucu boşluğu atlamıyordu; sayı taraması hemen
duruyor, `float.TryParse("")` başarısız oluyor ve değer **sessizce 0** oluyordu.

İki ders:
1. Ayrıştırıcı hatası gürültü çıkarmaz, **sıfır** çıkarır. `Parser_SkipsWhitespaceAfterColon`
   testi tam olarak bunun için var.
2. Aynı fonksiyonda ikinci bir tuzak daha vardı ve önlendi: bu makinenin yerel ayarı
   ondalık ayırıcı olarak **virgül** kullanıyor. `CultureInfo.InvariantCulture` verilmeseydi
   `-6306.5` değeri `-63065` olur, kıyı çizgisi on kat büyüyüp dünyanın dışına taşardı.
   Bu da testle kilitlendi.

## Depoya ne girer

| Şey | Depoda? | Gerekçe |
|---|---|---|
| `tools/gis/coastline_build.py` | ✅ | Üretim bandı |
| `refs/maps/coastline_1632.geojson` | ✅ | **Kendi çizimimiz = kendi telifimiz** (plan Faz 1 madde 2) |
| `data/gis/istanbul/coastline_1632_local.json` | ❌ | Türetilmiş |
| `Faz1_Terrain.unity` (arazi + GIS) | ✅ | Çalışır demo |

## Kanıt

`CoastlinePipelineTests` — 8 test yeşil (EditMode toplamı **63/63**).

| Ölçüm | Sonuç |
|---|---|
| Kıyı çizgisi | 64,1 km, 2 halka, 348 nokta |
| Düzeltme alanı | 5 (hepsi T2, `status: draft`) |
| Kıyı–arazi uyumu | 107/108 nokta ≤1,5 m |
| Denize bağlı alçak alan | %99,9 |

## Düzeltmelerin uygulanması (2026-08-18)

Caner: *"makul bir tahminle geri alabilirsin."* Uygulandı — ama **sabit metre ofsetiyle
değil**.

### Neden sabit ofset değil

Kıyıyı "150 m içeri kaydır" demek, tamamen uydurulmuş bir sayıyı haritanın hiçbir yerinden
denetlenemez biçimde gömmek olurdu. Onun yerine tahmin **araziden türetildi**:

> Modern dolgu alanları yapay olarak düz ve alçaktır; doğal kıyı, arazinin yükselmeye
> başladığı yerdir.

Dolgu bölgelerinde "deniz sayılan irtifa" `0,5 m → 5,0 m` çıkarılır; kıyı doğal yamacın
eteğine çekilir. Alan sınırında 150 m yumuşak geçiş (sert basamak, kıyıda alan sınırını
izleyen yapay bir sıçrama bırakırdı). **Tek ve aynı eşik** bütün alanlarda kullanılır —
alan alan ayar, kaynağı olmayan sayılara sahte kesinlik verirdi.

Kayma miktarı **seçilmez, ölçülür ve GeoJSON'a yazılır**:

| Alan | Geri alınan | Ölçülen kayma |
|---|---|---|
| Eminönü–Sirkeci | 18,6 ha | ~98 m |
| Unkapanı | 12,7 ha | ~99 m |
| Marmara kıyısı | 30,9 ha | ~49 m |
| Galata / Karaköy | 7,4 ha | ~35 m |

Karaköy'ün düşük çıkması doğrudur: Galata sırtı diktir, doğal yamaç suya yakın başlar.

### Langa: iç içe geçmiş alanlar ve muafiyetin ezilmesi

Langa modern bir dolgu **değildir** — Theodosius limanı Osmanlı döneminden çok önce dolup
bostana dönüşmüştür (RESEARCH.md §4). Ölçüm doğruluyor: medyan irtifa 4,6 m. Bu yüzden
`sea_threshold_m: None` ile açıkça muaf tutuldu.

**Yine de su bastı.** Langa, Marmara düzeltme alanının *tamamen içindedir* ve eşik alanı
alanlar üzerinde maksimum aldığı için Marmara'nın 5 m'si muafiyeti eziyordu. Önizlemede
Langa kutusunun içinde kapalı bir su halkası olarak göründü.

Düzeltme: muafiyet **birleştirmeden sonra** geri yazılır (koruma pası). Ders: *iç içe
geçebilen alanlarda `max` yeterli değildir; muafiyetin son sözü olmalıdır.*

### Katmanlar

| Katman | Ne | Uzunluk |
|---|---|---|
| `modern_shoreline` | bugünkü kıyı, kıyas için saklanır | 64,1 km |
| `shoreline_1632` | **oyunun kıyısı** (T2) | 65,0 km |
| `correction_zone` | 5 alan; eşik + ölçülen kayma her birinin içinde | — |

### Test ikizi

`Langa_IsNotFlooded` düzeltmenin **fazla** iş yapmadığını, `FillZones_ActuallyMovedTheShoreline`
**hiç** iş yapmadığını yakalar. İkisi olmadan sessizce devre dışı kalmış bir düzeltme
yeşil testlerin arkasında fark edilmeden geçerdi. Ölçülen ayrım nettir: Langa kutusunda
1820 → 1785 m (%−2), Eminönü kutusunda 1904 → 2180 m (%+14).

## Hâlâ açık

Bu bir **T2 rekonstrüksiyondur**, belge değil. RESEARCH.md hâlâ metrik ofset vermiyor;
değişen tek şey, tahminin artık *savunulabilir ve denetlenebilir bir yöntemi* olması.
Plan Faz 1 madde 3'teki dönem haritası georeferanslaması geldiğinde bu sayılar sınanacak.
