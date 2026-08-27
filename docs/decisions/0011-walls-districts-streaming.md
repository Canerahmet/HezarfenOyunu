# ADR 0011 — Sur hatları, semtler ve bölge yayını

**Tarih:** 2026-08-19
**Durum:** Kabul edildi — çıktılar **TASLAK**, Caner onayı bekliyor
**İlgili:** plan Faz 1 madde 3 (footprint'ler) ve madde 6 (bölge yayını); ADR 0007, 0008

---

## 1. Sur hatları — üç kanıt sınıfı, üç ayrı üretim yöntemi

`refs/maps/walls_1632.geojson` dört hat ve 23 kapı içerir. Hepsini tek yöntemle
üretmek, en zayıf halkanın güvenini en güçlüsüne yamamak olurdu. Bu yüzden:

| Hat | Kanıt | Yöntem | Ölçülen |
|---|---|---|---|
| `wall_land` | **Bugün ayakta** | elle izlendi, DEM ile kuru zemin denetimi | **5,82 km** (yaygın verilen ~5,7 km) |
| `wall_sea_marmara` | 1632'de ayakta (T1); çizgi T2 | çapa → `shoreline_1632`e yapıştır → 15 m karaya it | **7,55 km** (~8,5 km) |
| `wall_sea_halic` | 1632'de ayakta (T1); çizgi T2 | aynı | **5,25 km** (~5,5 km) |
| `wall_galata` | 1632'de ayakta; **güzergâh bilinmiyor** | kaba çevre poligonu | çevre 3,05 km, kapalı alan 53 ha |

Toplam çevre **18,6 km**. Ölçülen değerler yaygın verilenlerin altında kalıyor
çünkü çizgilerimiz sadeleştirilmiş — koy girintileri ve burç çıkıntıları yok.
Bu bilerek böyle: **üst** eşik aşılırsa üretim durur (izim bozuk demektir),
alt eşiğin altı yalnızca uyarı üretir.

### Neden deniz surları elle çizilmiyor

Bir deniz suru tanımı gereği kıyıyı izler. Elle nokta girmek, kıyı çizgimiz
düzeldiğinde surla arasında sessizce açılan bir fark bırakırdı. Onun yerine her
çapa `shoreline_1632`ye yapıştırılır; **yapıştırma kayması raporlanır** ve o
kayma, elle izimin ne kadar tuttuğunun ölçüsüdür.

Bu ölçü işe yaradı: ilk koşuda Haliç kesiminin kayması ortanca **241 m**, en çok
668 m çıktı ve denetim üretimi durdurdu. Sebep kıyı değil, benim izimimdi —
Cibali/Fener/Balat'ı kıyıdan içeri, güneye yazmıştım. Düzeltilince ortanca
kayma **35 m**'ye indi.

İki ek düzeltme aynı incelemeden çıktı:

* **En yakın köşeye değil, en yakın noktaya yapıştırma.** `shoreline_1632`
  Douglas-Peucker ile sadeleştirilmiştir ve köşe aralığı ortanca ~150 m'dir.
  Köşeye yapıştırmak hiçbir uyarı vermeden ~75 m hata ekler ve suru kıyının
  köşelerine basamaklandırırdı.
* **Kara yönü tahmin edilmez, ölçülür.** Kıyı teğetinin iki normali de örneklenir
  ve arazisi yüksek olan seçilir. Sabit bir "sağ taraf kara" varsayımı,
  Sarayburnu ve Ayvansaray gibi kıyının döndüğü yerlerde ters çalışırdı.

### Kapalı çevre

Kara suru iki ucundan deniz surlarına bağlanır. Deniz suru uçları kıyıya
yapıştırıldığı için bu bağın kendiliğinden tutması beklenemez ve tutmadı:
kara↔Marmara **313 m**, kara↔Haliç **108 m** açıktı. Uçlar kara surunun
terminallerine zorlandı (ikisinden güvenilir olan odur), fakat **zorlamadan önce
ölçülen açıklık `junction_gaps_m` olarak dosyaya yazıldı**. O sayı, elle izim ile
DEM'den türemiş kıyımızın o noktada ne kadar anlaşmadığının ölçüsüdür ve
gizlenmemelidir.

### Galata surları — bilerek eksik bırakıldı

1860'larda yıkıldı; elimizde georeferanslı dönem planı **yok**. CLAUDE.md açık:
*"Kaynak niteliksel olduğunda metrik geometri UYDURMA."* Bu yüzden güzergâh değil,
kaba bir çevre poligonu üretildi; `status: draft`, T2, ve sahnede **pembe** (diğer
surlardan ayrı renk) çizilir. Çevre 3,05 km / 53 ha ölçüldü ve Caner'e soruluyor.
Tek otomatik denetim: **Galata Kulesi poligonun içinde olmalı** (kule kuzey surun
tepesindeydi).

---

## 2. Semtler — bunlar tarihsel mahalle sınırı DEĞİLDİR

`refs/maps/districts.geojson` sekiz poligon içerir. Dosya adı yanıltıcı
olabileceği için bu en başa yazılıyor.

1632 İstanbul'unun mahalleleri **kadastral değildi**. 1546 ve 1600 tarihli Vakıf
Tahrir Defterleri mahalle *adlarını* ve vakıf kayıtlarını verir, sınır çizgisi
vermez (RESEARCH.md, "Mahalleler"). Mahalle bir alan değil, bir mescit çevresinde
toplanmış hane topluluğu ve bir kefalet birimiydi. "1632 mahalle sınırları" diye
bir metrik veri **yoktur ve üretilemez**.

Bu yüzden buradaki poligonlar **oyun bölgeleridir** — plan Faz 1 madde 6'nın
yayın hücreleri. Hepsi `tier: Graybox` ve `historical_claim: none` taşır; özet
metinlerinde "tarihsel mahalle sınırı DEĞİL" ibaresi vardır ve bir test bunu korur.

| Bölge | Öncelik | Tür | Tekil kara |
|---|---|---|---|
| D_Galata | 1 | kara | 409 ha |
| D_Okmeydani | 1 | kara | 580 ha |
| D_Surici_Dogu | 1 | kara | 410 ha |
| D_Uskudar | 1 | kara | 388 ha |
| D_Bogaz | 1 | su | — |
| D_Surici_Bati | 2 | kara | 1068 ha |
| D_Halic | 2 | su | — |
| D_Eyup | 3 | kara | 433 ha |

**Öncelik-1 toplamı 1786 ha** — dikey dilimin içerik bütçesi budur.

### Çakışmasız alan bütçesi

Bölgeler bilerek çakışır: aynı anda birden çok bölgenin yüklü olması, yükleme
ekransız geçişin ta kendisidir. Ama her bölge kendi içindeki karayı sayarsa Faz
4'ün yerleştirme bütçesi aynı araziyi iki kez sayar. İlk ölçümde `D_Halic` **%62
kara** raporladı — su bölgesi olmasına rağmen iki yakayı da yutuyordu.

İki düzeltme: su poligonları suya daraltıldı (Haliç %62 → %25), ve alan ölçümü
tek paylaşımlı 40 m ızgaraya taşındı. Her kara hücresinin **sahibi**, onu içeren
en yüksek öncelikli **kara** bölgesidir; su bölgeleri kara sahiplenmez. GeoJSON
hem `land_area_ha` (toplanamaz) hem `exclusive_land_area_ha` (bütçe) yayınlar.

Aynı ölçüm ikinci bir hatayı da gösterdi: `D_Okmeydani` fazla geniş çizilmiş ve
öncelik-1 olduğu için `D_Eyup`un 268 ha karasını sessizce sahipleniyordu.
Poligon daraltıldı.

### Bağımsız çapraz kontrol

Suriçi Doğu + Batı = **1478 ha**. Gerçek tarihi yarımada ~1400 ha. Bu sayı hiçbir
yerden kopyalanmadı; DEM örneklemesinden çıktı ve bağımsız olarak tuttu. Bir
EditMode testi bunu 1100–1800 ha aralığında bağlıyor.

### Otomatik denetimler

* Her bölge, adını taşıdığı landmark'ları **içermek zorunda** (`requires` listesi
  `landmarks_1632.geojson`e karşı doğrulanır).
* **Uçuş koridoru:** Galata Kulesi → Doğancılar doğrusu 100 m'de bir örneklenir;
  38 örneğin tamamı bir öncelik-1 bölgenin içinde. Oyunun omurgasında yüklü
  olmayan hücre kalamaz.

---

## 3. Bölge yayını — kenar uzaklığı + histerezis

**Yükleme ölçütü poligonun kenarına uzaklıktır, merkezine değil.** Merkez+yarıçap
kolaydır ama uzun/bükük bölgelerde yanlıştır: `D_Halic` ince bir su şerididir,
merkezine göre yarıçapı ~2,9 km çıkar ve bir ucundayken öbür ucu da yüklü tutardı.

**Histerezis şart:** `unload = load × 1,30` (700 m / 910 m). Tek eşikle, sınırda
gidip gelen oyuncu sahneyi sürekli yükleyip boşaltır ve "yükleme ekranı yok"
vaadi bir takılma olarak geri döner. Bir EditMode testi 50 salınım boyunca tam
1 yükleme / 0 boşaltma olduğunu doğrular.

**Karar mantığı Addressables'tan ayrı.** `DistrictStreamingPlan` saf bir sınıftır;
doğruluğu bir build ya da oyun oturumu gerektirmeden kanıtlanır. `DistrictStreamer`
yalnızca kararı çağrılara çevirir ve tutamaçları sahiplenir.

**Aynı anda en fazla 1 yükleme uçuşta.** Üç semt sınırının kesiştiği yerde üç
sahne birden çözülürse kare düşer. `activateOnLoad: false` de bilinçlidir: sahne
arka planda çözülür, etkinleştirme ayrı bir kareye bırakılır.

> **Ölçü tuzağı (yaşandı):** Yük tavanı testinin ilk hâli "yerleşik ama sahnesi
> henüz `isLoaded` değil" sayıyordu ve 2 gördü. `activateOnLoad: false` yüzünden
> çözülmüş-ama-etkinleşmemiş sahne de o sayıya girer. Kod doğruydu, **ölçü**
> yanlıştı. `LoadsInFlight` eklendi ve gerçek değişmez ölçülüyor.

---

## 4. Addressables yapılandırması — `_Project` kuralının bilinçli istisnası

CLAUDE.md *"Assets/_Project dışına dosya koyma"* der. Addressables yapılandırması
`Assets/AddressableAssetsData` altına iner.

Ayarlar varlığının klasörü `AddressableAssetSettings.Create` ile değiştirilebilir —
ilk denemem buydu. Ama paket kaynağına bakınca `DefaultObject.asset` yolunun
**sabit kodlu** olduğu görüldü (`AddressableAssetSettingsDefaultObject.kDefaultConfigFolder`),
yani yapılandırmanın bir parçası her hâlükârda dışarıda kalıyor. Yarısı bir yerde
yarısı başka yerde duran bir kurulum, tek yerde ve çerçevenin beklediği yerde
durandan daha kafa karıştırıcıdır.

`Assets/Settings/` (HDRP) zaten aynı türde bir çerçeve klasörüdür; emsal var.
**Karar:** Unity varsayılanı kullanılıyor, istisna burada ve kodda belgeleniyor.

---

## 5. Yan bulgu: inceleme bindirmeleri dikeyde tersti

Bu oturumda üretilen ilk bindirmede Marmara denizi **kuzeyde** göründü.

Sebep: DEM dizisinde satır 0 = **güney** (`heightmap_format: "row0=south"`), ama
vektör çizimi `n-1-y` ile satır 0 = kuzey varsayıyor. Taban rasteri çevrilmediği
için arazi ile çizgiler ters oturuyordu.

**Aynı hata `coastline_build.py`nin `draw_overlay` fonksiyonunda da vardı**, yani
daha önce üretilen `preview_coastline.png` de tersti. İkisi de düzeltildi ve
yeniden üretildi.

Yakalanma sebebi: bindirmeye bakıp "bir şey ters" dedikten **sonra düzeltmeye
değil ölçmeye** gitmek — DEM satırlarının kara oranı örneklendi (satır 0: %0 kara,
satır n−1: %95 kara). CLAUDE.md'deki kural bir kez daha işledi.

Bindirmede ikinci bir okunabilirlik hatası daha vardı: 1632 kıyı çizgisi denizle
aynı maviye çiziliyordu. Zemindeki mavi **bugünkü** su hattıdır (DEM'de dolgular
duruyor); incelemenin bütün anlamı iki hattın farkını görmektir. 1632 çizgisi
**mora** alındı.

---

## Yeniden üretim

```powershell
tools\gis\.venv\Scripts\python.exe tools\gis\walls_build.py --dir data\gis\istanbul
tools\gis\.venv\Scripts\python.exe tools\gis\districts_build.py --dir data\gis\istanbul
tools\gis\.venv\Scripts\python.exe tools\gis\map_overlay.py --dir data\gis\istanbul
```
Ardından Unity: **Hezarfen → GIS → Semtleri ice aktar**, sonra
**Hezarfen → GIS → Kiyi + landmark + sur + semtleri sahneye al**.

## Onay bekleyen

`renders/review/Map1632_v1/` → notlar `docs/feedback/walls_districts.md`.
Özellikle **wall_galata** (kaba taslak) ve **semt sınırları** (oynanış kararı).
