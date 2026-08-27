# ADR 0062 — Semt doldurma: mahalle çoğaltılır, kural çoğaltılmaz

**Tarih:** 2026-08-27
**Durum:** Kabul edildi (Galata dolduruldu, ölçüldü)
**Bağlam:** Faz 4 — Şehri Doldurma, ilk adım

## Durum

Faz 4'ün bir kısmı Faz 2'de öne çekilmişti: arazi dokusu
(`TerrainCoverBuilder`), doğal örtü (`GreeneryBuilder`), semt sınırları
(`DistrictImporter`) hazırdı. Eksik olan çekirdekti: `OttomanStreetBuilder`
tek bir mahalleyi **doğru** kuruyordu — yedi kuralı belgeliydi (eş yükselti
takibi, sokak çizgisine oturan cephe, çıkmazlar, cumba taşması) — ama
yalnızca **örnek sahne** üretiyordu. `FlightSlice`'ta sıfır ev vardı.

## Karar

### 1. Kural çoğaltılmaz, mahalle çoğaltılır

`DistrictFiller` sokağın nasıl kurulacağına dair **hiçbir kuralı yeniden
yazmaz**. Yaptığı tek şey mahalle çekirdeklerini yerleştirip mevcut kurucuyu
çağırmak. Bunun için kurucu ikiye ayrıldı:

- `Build(QuarterSpec, seed)` — sahneyi açar, kurar, kaydeder (Faz 2 menüleri
  değişmedi)
- `BuildInto(parent, terrain, spec, seed)` — yalnızca geometriyi kurar
- `ResetQuarterState()` — çakışma listesini temizler; **semtin başında bir
  kez** çağrılır, yoksa ikinci mahalle birincinin üstüne kurulur

### 2. Çekirdek yeri araziye sorulur

Dört eleme ölçütünün dördü de ölçülür, elle çizilmez: kot
(`minElevationMeters` — su ve kıyı şeridi), eğim (`maxSlopeDegrees`),
landmark uzaklığı (külliyenin kendi alanı vardır), semt sınırı.

Sokağın **yönü de seçilmez**: arazinin o noktadaki eğim gradyanına dik yön
alınır, yani yamacı yanlamasına tarayan yön (Kural 1'in gereği). Düz zeminde
gradyan yön bildirmez ve orada hücre tohumundan bir açı gelir — bu bir eksik
değil, dürüstlük: düz zeminde sokağın yönünü belirleyen şey arazi değildir.

### 3. Deterministik olmak zorunda

Plan bunu açıkça istiyor: *"Aynı seed = aynı şehir (test edilebilirlik)"*.
Bu yüzden çekirdekler rastgele serpilmez; semtin sınır kutusu üzerinde sabit
bir ızgara taranır ve **her hücre kendi indisinden türeyen** bir sarsıntı
alır. Tarama sırası, liste sırası ya da sahnedeki nesne sırası sonucu
değiştiremez. Her mahallenin tohumu da semt tohumu + sırasından türer:
mahalle 7 her zaman aynı mahalle 7'dir, komşuları değişse bile.

### 4. Nadir kurumlar mahalle başına DEĞİL, semt başına sayılır

Bu, ilk koşumun **ölçülerek** ortaya çıkardığı kusurdur.

Tek örnek sokak sahnesinde hamam, medrese ve kilise koşulsuz konuyordu ve
o bağlamda doğruydu: o mahalle semtin tamamını temsil ediyordu. Semt gerçekten
34 mahalleye bölününce aynı kod **22 hamam, 22 medrese ve 22 Latin kilisesi**
üretti. Galata'da o kadar hamam yoktu; mahalle sayısı kadar medrese hiç yoktu.

Kural artık şu: **mahalle ne söylenirse onu kurar; kaç tane olacağına semt
karar verir.** `QuarterSpec`e bayraklar eklendi, `DistrictDef`e semt bütçesi.
Dağıtım Fisher-Yates'in ilk `adet` adımıyla yapılır — rastgele indis seçip
tekrar elemekten farkı, `adet ≥ mahalle` olduğunda da doğru davranması.

Bayrakların `SpecFor` içindeki varsayılanı **kapalı**dır: dağıtım unutulursa
fazla değil **eksik** üretsin.

### 5. Semt içeriği kendi sahnesine yazılır

İlk denemede `Faz1_Terrain.unity`'ye yazıldı ve sahne 932 KB'dan **15 MB**'a
çıktı. İki sebeple yanlıştı: streaming tasarımı zaten semt başına bir sahne
öngörüyor (`DistrictDef.sceneAddress`), ve Faz 4 boyunca semt defalarca
yeniden kurulacak — her kurulum 15 MB'lık bambaşka bir YAML üretecekti.
ADR 0059'un yeniden üretim gürültüsü, sahne ölçeğinde.

## Ölçülen sonuç — Galata ve Üsküdar

| ne | değer |
|---|---:|
| mahalle | 34 (Galata) + 30 (Üsküdar) |
| ev | 2 892 + 2 632 = **5 524** |
| mescit / mektep / çeşme / şadırvan | 34 (mahalle başına) |
| kilise / hamam / medrese / han | 6 / 5 / 2 / 1 (semt bütçesi) |
| fırın / kahvehane / bozahane | 10 / 8 / 3 |
| servi + mezar (hazire) | 290 + 406 |
| LOD0 üçgen toplamı | 6 877 504 |

Bütçe birebir tuttu.

## Açık — Caner'e

**Nadir kurum sayıları TASLAKTIR (T2).** Kaynaklarda 1632 Galata'sının hamam
ya da medrese sayısı yok; buradakiler yapı tipinin şehirdeki yaygınlığına
göre seçilmiş, **ölçülmemiş** değerlerdir. Kaynak bulunursa değişir. Sayılar
`DistrictDef` üzerinde durur, kodda değil — yani düzeltmek bir alan
değiştirmektir.

## Ölçüm — kule tepesinden 360°

Kabul kriteri *"kule tepesinden 360° bakışta FPS hedefi tutuyor"* diyor.
**FPS ölçülmedi, üçgen sayıldı** ve bu bilinçli: FPS tekrarlanabilir değildir
(editörün yükü, arka plandaki içe aktarım, pencere boyutu sayıya karışır).
`CityBudget` analitik sayar — her LODGroup için kameranın uzaklığına göre
hangi kademenin etkin olacağı Unity'nin kendi formülüyle bulunur, sonra
frustum içindekiler toplanır. Aynı sahne + aynı bakış = aynı sayı.

Sekiz yön × beş eğim taranır ve **en pahalı kare** bütçeyi belirler.

| | |
|---|---:|
| en kötü kare | yaw 45°, yatay |
| üçgen | **173 217** |
| renderer | 726 |
| taranan LODGroup | 9 930 |
| bütçe | 2 500 000 |
| kullanım | **%7** |

İki şey ölçümü düzeltti. **Yalnız yatay bakmak** yetmiyordu: kule
tepesindeki oyuncu şehre aşağı bakar, o yüzden eğim taraması eklendi. Ve
eğim işareti terstiydi — Unity'de pozitif X aşağı baktırır, benim
değerlerim negatifti, yani kamerayı gökyüzüne çeviriyordum.

En pahalı karenin **yatayda** çıkması ilk bakışta şüpheliydi ama doğru:
aşağı bakan kamera yere yakın küçük bir alan görür, ufka bakan şehrin
tamamını.

**Bütçenin yalnızca %7'sinin kullanılması bir başarı değil, bir bulgudur:**
LOD merdiveni (ADR 0061) selâtin camileri için ayarlandı ve eşik ekran
yüksekliği oranı olduğu için 10 m'lik bir eve uygulandığında çok daha kısa
mesafelere denk geliyor — ev 458 m'de orta kademeye, 800 m'de bloğa düşüyor.
Kule tepesinden bakınca şehrin çoğu blok. Bütçe tutuyor ama **kalite
bırakılmış olabilir**; bir sonraki tur bunu ölçmeli.

## Bitmeyenler

- ~~Sebil mahalle başına~~ — **düzeltildi**, semt bütçesine alındı (4).
- **Draw call ölçülmedi.** Bütçenin ikinci yarısı (≤1500 draw call) GPU
  Resident Drawer'a bağlı ve çalışma zamanı ölçümü ister.
- **Ev LOD'ları fazla agresif olabilir** — yukarıdaki %7 bulgusu.
- Donatı geçişinden **kayık ve pereme yapıldı**; kuyu, dükkân kepengi,
  çamaşır ipi ve kuş sürüsü kaldı.

İlgili: [ADR 0011](0011-walls-districts-streaming.md),
[ADR 0024](0024-arazi-ortusu.md), [ADR 0059](0059-git-gecisi.md)
