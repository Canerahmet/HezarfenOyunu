# ADR 0087 — APV pişirme mimarisi: semt hacmi, açık su, aralık ve dondurma

- Tarih: 2026-09-04
- Durum: kabul (ölçümle)
- Bağlam: şehrin dolaylı ışığı yoktu; bu turda fırın dört kez
  "başarılı" dönüp diske hiçbir şey yazmadı

## Sorun

Turun her karesinde bina varsa gölge siyahtı. `tools/olcum/golge_orani.py`
bunu tek cümleye indirdi:

| kare | gölge mavi/kırmızı | saçılım |
|---|---:|---:|
| çıplak araziye bakanlar (01, 02, 03, 05, 08, 10) | 0,26 – 0,30 | 0,10 – 0,17 |
| şehre bakanlar (04, 06, 07, 09) | 0,000 – 0,016 | 0,006 – 0,10 |

Sûriçi sokağının kendisinde (sabit dikdörtgen) mavi/kırmızı **0,000**,
saçılım **0,001** — o alandaki her karanlık piksel aynı renk. Değişmeyen
karanlık gölge değil, **ışık almayan yüzey**tir.

## Fırın bitti ve problar SİYAH pişti — kapanmamış halka

`D_Galata` 106 dakikada pişti: 62 hücre, 157 MB. Çalışma zamanı sağlam
(tur raporu `apv: kurulu/kume var`). Gölge yine **0,000**.

Diskteki verinin kendisi okundu ve hangi yarının boş olduğunu söylüyor:

| dosya | içerik | okunan |
|---|---|---|
| `CellData` | L0 (ışınım) | `(0, 0, 0, 0.5)` half dörtlüleri — **sıfır** |
| `CellOptionalData` | L1 (yön) | `0x7f` dolusu — orta nokta, yani **yönsüz** |
| `CellSharedData` | geçerlilik | `0xff` — problar geçerli |
| `CellSupportData` | konum | gerçek koordinatlar (162, −414, 42…) |

Yani problar **yerinde ve geçerli, ama ışıksız**.

### Sebep zinciri

* Fırında ışık yok — bilinçli: güneş `Realtime Only`, `ZamanSistemi`
  onu saate göre döndürüyor.
* Geriye gökyüzü kalıyor ve fırın gökyüzünü
  `RenderSettings.ambientProbe` üzerinden görüyor. Ölçüldü:
  **0,0370 / 0,0421 / 0,0546** — mavimsi, yani gerçekten gökten geliyor,
  ama bir gün ışığı göğü için çok karanlık.
* Sahnedeki güneş +43,4° yükseklikte ve 100.000 lux. Ama
  **PhysicallyBasedSky'ın parlaklığı güneş ışığının atmosferde
  saçılmasından gelir** — güneş fırının dışındaysa saçılacak bir şey de
  yok. Gök, kendi saçılma terimi kadar kalıyor.
* 0,037'lik bir ortamdan tek sıçrama, L0'ın kodlama çözünürlüğünün
  altına düşüyor ve sıfır olarak yazılıyor.

Yani **"güneş gerçek zamanlı kalsın, APV gök sıçramasını taşısın"
kurgusu PhysicallyBasedSky ile kendi içinde tutarsız.** Gök, güneşsiz
parlamıyor.

### Karar: güneş fırına girer (Caner bıraktı, ölçüm seçti)

Caner ışık kararını bana bıraktı. **Önerim (2) idi** — pişirme için
ayrı, sabit, açıkça parlaklığı verilmiş bir gök — ve **ölçüm onu
eledi.** Üç deney, her biri `D_Okmeydani` üzerinde on dakika, tek
değişkenli:

| # | değişen | `CellData` sonucu |
|---|---|---|
| 1 | fırının kendi gök profili, Lux kipinde 20.000 | **2 desen** — L0 tam sıfır |
| 2 | fırına gerçek bir skybox malzemesi (ortam probu 0,037 → 0,18/0,23/0,30) | **2 desen** — L0 tam sıfır |
| 3 | yönlü güneş `Mixed` | **12.106 desen** — problar ilk kez ışık taşıyor |

Yani **fırın, içinde hiç ışık yoksa prob aydınlatması üretmiyor.**
Gökyüzünü ne kadar parlatırsan parlat sonuç değişmiyor; seçenek (2)
tek başına çalışmıyordu.

**Karar: seçenek (1) — güneş `Mixed`, karma kip `IndirectOnly`.**

* `IndirectOnly` (Baked Indirect) yalnız **sıçramayı** pişirir, gölge
  haritası üretmez. `ZamanSistemi` güneşi saate göre döndürmeye devam
  eder; doğrudan ışık ve gölgeler onu izler.
* Donan tek şey **dolaylı terim**: sıçrama, pişirildiği saatin
  güneşine göre gelir. Bir saat sapmış bir sıçrama, kapkara bir
  sokağın yanında küçük kalır.
* Seçenek (2)'nin iki parçası **kaldı**, çünkü ikisi de gerçek:
  fırının kendi gök profili (20.000 lux, oyunun göğünden ayrı, oyunun
  görüntüsüne dokunmadan) ve güneş diski **kapalı** bir skybox
  malzemesi — güneş artık fırının içinde, iki kez sayılmamalı.

İleride birden çok saat isteniyorsa yol açık: APV'nin *lighting
scenario* mekanizması (`supportProbeVolumeScenarios` şu an 0) her saat
için ayrı bir pişirme demek — bu makinede saat başına iki saat.

## Fırın ışık aldı, kare almadı — ve A/B yönü tersine çevirdi

Güneş fırına girdikten sonra `D_Galata` yeniden pişti: 95,8 dk, 62
hücre, 155 MB, `CellData` **38.238 farklı desen** (ışıksız pişirmede 2).
Problar ışık taşıyor.

Kare değişmedi. Galata sokağının gölgesi: `0,0202 / 0,0061 / 0,0001` —
mavi kanal sıfır.

Zincirin okunabilen her halkası "açık" diyor:

| halka | okunan |
|---|---|
| diskteki veri | 62 hücre, 155 MB, 38.238 desen |
| çalışma zamanı | `kurulu / kume var` |
| çiziciler | `m_LightProbeUsage: 1` (BlendProbes) |
| boru hattı | dört varlıkta da `lightProbeSystem: 1` |
| kamera kare ayarı | açıkça `AdaptiveProbeVolume = true` yazıldı |

### A/B ve okunuşu

| koşum | gölge (r/g/b) |
|---|---|
| APV açık | 0,0202 / 0,0061 / 0,0001 |
| APV **kapalı** (dört varlıkta da) | 0,0217 / 0,0075 / 0,0001 |
| kamera kare ayarı zorla açık | 0,0203 / 0,0062 / 0,0001 |

Önce "hiç uygulanmıyor" diye okundu. Ama sayı ondan fazlasını söylüyor:
**APV kapatılınca gölge AYDINLANIYOR.** Yani APV uygulanıyor ve
uygulandığında sahneyi *karartıyor* — pişmiş problar, onların yerine
geçtiği yedek ortamdan (skybox'lı ambient probe, 0,18/0,23/0,30) daha
karanlık.

Bu, sorunun yerini değiştirir: yol açık, **probların kendisi karanlık**.

### Albedo sınandı ve elendi

`D_Okmeydani`, `albedoBoost = 8` ile pişirildi. Desen sayısı
**12.106 → 12.291** — yüzde bir buçuk. Tek sıçramalı bir fırında probun
değerini ona **doğrudan** ulaşan ışık belirliyor; sıçrama terimini sekiz
katına çıkarmak toplamda görünmüyor. Albedo, sıçramayı aşağıda tutan
şey değil.

### Kalan okuma: problar makul, YEDEK ortam fazla parlak

Üç ölçüm bir araya gelince tablo değişiyor:

* APV kapatılınca gölge **aydınlanıyor** (0,0217 > 0,0202) — yani APV
  uygulanıyor ve uygulandığında sahneyi karartıyor.
* Fırının ortamı skybox malzemesinden geliyor ve ortam probu
  **0,18 / 0,23 / 0,30** okuyor. APV kapalıyken kareyi aydınlatan da bu.
* Problar bu ortamdan ve 100.000 lux'lük güneşten pişiyor, ama sokak
  seviyesinde gökyüzünün büyük kısmı **kapalı** — probun gördüğü şey
  dar bir gök parçası.

Yani mesele "APV çalışmıyor" değil: **APV çalışıyor ve gölgeyi, yedek
ortamın verdiği yapay parlaklıktan daha karanlık yapıyor.** Gölgenin
mavisiz olması da buradan: dar gök parçası + sıcak sıva sıçraması.

### Hedefi düzeltiyorum: parlaklık değil, GÖĞÜN PAYI

Bir saat önce hedefi "pişmiş problar yedek ortamdan karanlık olmasın"
diye yazmıştım. **Yanlış hedef.** Yedek ortam gökyüzünü her yere,
sokağın içine bile aynı şiddette uyguluyor; APV ise probun gerçekten
gördüğü gök parçasını uyguluyor. Sokakta ikincisinin daha karanlık
olması **doğru** — gölgeli bir sokak açık bir tarladan karanlıktır.

Yanlış olan şey parlaklık değil **renk**. Ölçüm:

```
golge  rgb 0,0202 / 0,0061 / 0,0001    mavi/kirmizi 0,000
gunes  rgb 0,3237 / 0,2547 / 0,1767    mavi/kirmizi 0,407
```

Gölgenin mavisi **tam sıfır**. Gerçek bir gölge, üstündeki gök şeridinden
mavi alır; bizimki yalnızca sıcak sıvadan sıçrayan güneşi taşıyor.

Sebep bir **oran** meselesi ve sayılarla söylenebilir: fırındaki güneş
100.000 lux, fırının gördüğü gök ise ortam probunda 0,18-0,30. Gerçek
gün ışığında gökyüzünün payı toplamın **%10-20'sidir**; burada
milyonda birler mertebesinde. Yani gök, güneşin yanında yok
hükmünde ve probun rengini tek başına güneş belirliyor.

**Doğru hedef:** gölgenin mavi/kırmızı oranı, aynı sahnede çıplak
araziye bakan karelerin ailesine (0,26-0,30) yaklaşsın. Kaldıraç
`FirinGokyuzu.SkyboxPozu`, ama hareket ettirilecek miktar küçük değil —
oran argümanı büyük bir çarpan istiyor, ve o çarpan **ölçümle**
bulunacak: her denemede pişirme kaydındaki ortam probu satırı ve
`golge_orani.py` birlikte okunur.

**Uyarı:** güneşi kısmak da aynı oranı düzeltir ama sahnenin pozunu
bozar (`Exposure` sabit 14,5 EV'de). Değiştirilecek olan gök.

## Açık soru: semtler tek kümede birikiyor mu?

Bir turda `D_Okmeydani` pişip beş hücre yazdı, ardından `D_Eyup` yirmi
altı dakika pişip diske **hiç dokunmadı**, ve bu "kısmi pişirme
dondurulmuş yerleşime yazamıyor" diye okundu. Şimdi ikinci bir açıklama
var ve daha basit: **o pişirmede zaten ışık yoktu.** Işıksız bir fırın
sıfır veri üretir; imza değişmez; sonuç "hiçbir şey yazmadı" görünür.

Yani `freezePlacement` hakkında çıkarılan sonuç, ışık kusuru
düzeldikten sonra **yeniden sınanmalı**. Deney açık ve tek adım:

1. `D_Galata` pişmiş hâldeyken (temizlemeden),
2. `D_Uskudar`'ı `-hezarfenDonuk` ile, **`-hezarfenTemizle` OLMADAN**
   pişir,
3. `SemtProblari.HucreSayisi()` büyüdü mü diye bak — imza denetimi
   zaten bunu yazıyor.

Büyüyorsa semtler tek kümede birikiyor ve şehrin tamamı gece boyunca
sırayla pişebilir. Büyümüyorsa kısıt gerçek ve o zaman ya tam pişirme
(bu makinede sığmıyor) ya da semt başına ayrı küme (çalışma zamanında
tek küme etkin olabildiği için ayrı bir tasarım işi) gerekir.

Bu soru buraya yazıldı çünkü cevabı **iki saatlik bir pişirmeye**
mal oluyor ve bir sonraki tur onu yeniden keşfetmemeli.

## Şu an koşan iş ve sabah ne yapılacak

`D_Galata`, **gök pozu 90** ile pişiyor (04:24'te başladı, ~96 dk).
Ölçülen kaldıraç doğrusal: poz 1,3'te ortam probu 0,18/0,23/0,30,
poz 90'da 12,62/15,78/20,87 — tam 69,2 kat, 90/1,3 ile aynı.

Bittiğinde üç komut, sırayla:

```powershell
# 1) Diskteki veri: desen sayisi 2'den buyuk olmali (isiksiz pisirmede 2).
python tools/olcum/prob_isigi.py

# 2) Tur — kareler yeniden cekilir.
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" `
  -batchmode -projectPath unity/HezarfenGame `
  -executeMethod Hezarfen.Editor.Diagnostics.OyunTuru.TopluKos `
  -logFile tur.log

# 3) KAPI: golgenin mavi/kirmizi orani.
python tools/olcum/golge_orani.py --gok-yok renders/tur/03_galata_sokak.png
python tools/olcum/golge_orani.py --dilim 0.5 --bolge 650,455,1010,505 `
  renders/tur/03_galata_sokak.png
```

### Tahmin yazıldı, koşuldu, **tutmadı** — ve okumam yanlışmış

Poz 90 için beklenti sonuç görülmeden yazılmıştı: gölgenin mavi/kırmızı
oranı **0,15-0,35** bandına çıkacaktı. Fırın koştu (95,2 dk, 62 hücre,
155 MB, 38.237 desen) ve ölçüm:

```
poz  1,3 : golge 0,0202 / 0,0061 / 0,0001   mavi/kirmizi 0,000
poz 90   : golge 0,0201 / 0,0061 / 0,0001   mavi/kirmizi 0,000
```

Gökyüzü fırında **69 kat** parlatıldı ve karede **hiçbir şey**
değişmedi.

Bu, bir saat önceki okumamı da çürütüyor. *"APV kapatılınca gölge
aydınlanıyor (0,0217 > 0,0202), demek ki uygulanıyor"* demiştim; 69
katlık bir değişimin sıfır fark yaptığını görünce o %7'nin koşumlar
arası gürültü olduğu anlaşılıyor. **Doğru okuma ilkiydi: pişmiş APV
verisi kareye hiç ulaşmıyor.**

> **Bu cümle sonradan ölçümle düzeltildi** — bkz. *"Kaldıracı
> çevirdim"*. Veri kareye ulaşıyor; payı ölçülemeyecek kadar küçük
> olduğu için ulaşmıyor gibi okundu. Satır, nasıl yanıldığımın kaydı
> olarak duruyor.

### Elenenler — hepsi okundu, hepsi "açık" diyor

| halka | okunan |
|---|---|
| diskteki veri | 62 hücre, 155 MB, 38.237 desen |
| çalışma zamanı | `kurulu` |
| pişirme kümesi | `Faz1_Terrain Baking Set` bağlı |
| aydınlatma senaryosu | `'Default'` — diskteki `-Default` ile aynı |
| çizici `m_ReceiveGI` | semt sahnelerinde 24.367 kez **2 = LightProbes** |
| çizici `m_LightProbeUsage` | 1 = BlendProbes |
| boru hattı | dört varlıkta da `lightProbeSystem: 1` |
| kamera kare ayarı | `AdaptiveProbeVolume` açıkça yazıldı |
| `ProbeVolumesOptions` | `intensityMultiplier: 1`, `active: 1` |
| gölgeli **duvar** (arazi değil) | mavi 0,0001 — kusur araziye özgü değil |
| albedo | `albedoBoost 8` → %1,5 |
| fırının gök parlaklığı | 69 kat → sıfır fark |

Okunabilir her anahtar açık ve sonuç değişmiyor. Elemenin kendisi bir
bulgu: sebep bunların hiçbirinde değil.

### Sıradaki adım GUI ister — Caner'e istek

Toplu kipten okunamayan tek alet **Rendering Debugger'ın Probe Volume
sekmesi**: yüklü hücreleri ve probları sahnede gösteriyor. Bir kez
Editor açıp Galata durağında (300, 100) bakmak *"hücreler yükleniyor mu,
problar orada mı, değerleri ne"* sorusunu doğrudan cevaplar.

CLAUDE.md, ben toplu iş koştururken Editor'ü kapalı tutuyor; bu yüzden
bu adım Caner'in. Yol: **Window → Rendering → Rendering Debugger →
Probe Volume**, `Display Probes` açık, `Probe Shading Mode: SH`.

**Okunuş.** Fırın öncesi gölge `0,0202 / 0,0061 / 0,0001`, mavi/kırmızı
**0,000**. Aynı sahnede çıplak araziye bakan kareler 0,26-0,30 okuyor.
Gölgenin oranı o aileye yaklaştıysa poz 90 doğru mertebedir ve
`FirinGokyuzu.SkyboxPozu` ölçümle güncellenir. Hâlâ 0'a yakınsa çarpan
yetmemiştir; kaldıraç doğrusal olduğu için bir sonraki deneme doğrudan
hesaplanabilir.

**Not:** `SkyboxPozu` sabiti hâlâ **1,3**. 90 bir deney ve ölçüm onu
doğrulayana kadar sabit yazılmadı — bu depoda bir sayı, onu doğrulayan
ölçüm olmadan yazılmaz.

### Kaldıracı çevirdim: APV **ulaşıyor**, yalnızca ~150 kat sönük

Elemenin sonunda geriye tek okunmamış kaldıraç kaldı:
`ProbeVolumesOptions.intensityMultiplier`. Onu 1'den **50**'ye aldım —
başka hiçbir şeye dokunmadan, aynı fırın verisiyle, aynı durakta.

```
carpan  1 : golge 0,0201 / 0,0061 / 0,0001   gunes 0,3216 / 0,2525 / 0,1747
carpan 50 : golge 0,0263 / 0,0092 / 0,0002   gunes 0,4310 / 0,3191 / 0,2134
```

Bu, APV tarafından yapılan bir değişikliğin kareyi **ilk kez**
oynattığı an. Ve tek koşumluk bir gürültü olmadığını söyleyen şey
farkın büyüklüğü değil, **iki bölgenin aynı oranda oynaması**:

| bölge | çarpan 1 | çarpan 50 | artış | çözülen APV payı |
|---|---|---|---|---|
| gölge (kırmızı) | 0,0201 | 0,0263 | %30,8 | %0,63 |
| güneş (kırmızı) | 0,3216 | 0,4310 | %34,0 | %0,69 |

`kare = taban + çarpan × apv` kurup çözünce iki bağımsız bölge
**%0,63** ve **%0,69** veriyor. Koşumlar arası gürültü iki bölgeyi
birbirinden bağımsız oynatırdı; aynı oranı iki kez vermezdi. (Kare
tonemap'ten geçtiği için bu çözüm tam doğrusal değil — mertebe
doğrudur, ondalık değil.)

**Yani ADR'nin yukarıdaki cümlesi yanlış:** *"pişmiş APV verisi kareye
hiç ulaşmıyor"* değil, **ulaşıyor ve karenin ~%0,65'ini kuruyor.**
Doğru olması için ~%100 mertebesinde olmalıydı; kabaca **150 kat**
sönük.

Bu, poz-90 deneyinin neden sıfır fark verdiğini de açıklıyor: göğü 69
kat parlatmak, karenin %0,65'ini kuran bir terimin **gökten gelen
kısmını** büyütür — ve o kısım ölçüm eşiğinin altında kalır. İki sonuç
çelişmiyor; ikincisi birincinin sebebini veriyor.

**Çarpan bir çözüm değil, bir termometre.** 50 yazıp geçmek, ölçümün
yerine sabit koymaktır — bu depoda üç kez çıkan kusurun tam kendisi.
Sabit 1'e geri alındı. Soru artık şu: probların içindeki sayı neden
150 kat küçük?

### Sıradaki tek değişkenli deney: albedo, bu kez doğru aletle

Yukarıdaki tabloda `albedoBoost 8 → %1,5` yazıyor ve **o ölçüm yanlış
aletle yapılmıştı**: karşılaştırılan şey `CellData` içindeki farklı
desen sayısıydı. Desen sayısı verinin *çeşitliliğini* ölçer,
*büyüklüğünü* değil — sekiz kat parlak bir fırın da hemen hemen aynı
sayıda farklı desen üretir. Yani albedo elenmedi; ölçülmedi.

Şimdi doğru alet var: çarpan deneyinin kurduğu "APV payı" hesabı.
D_Galata `albedoBoost 8` ile yeniden pişiyor. Beklenti **sonuç
görülmeden** yazılıyor: sıçrama ışığı albedoyla doğrusal olduğu için
pay %0,65'ten **%5 mertebesine** çıkmalı, gölgenin kırmızısı
0,0201 → **0,021 civarına**. Çıkmazsa albedo gerçekten elenir ve geriye
fırının güneşinin probda hangi ölçekte durduğu kalır.

## Ölçülen dört sebep

1. **Prob hacimleri dünya boyuydu.** Her semtin hacmi `Mode.Global`'dı ve
   `Global` sahnenin değil **yüklü olan her şeyin** sınırını alır; kurulum
   sekiz semti birlikte açıyor. Kümenin kaydı: her semt için
   `m_Extent: {x: 7776, y: 364.5, z: 7897.5}` — 15,5 × 15,8 km ve
   **sekizi de aynı kutu**. Bedeli: *"the number of APV probes exceeds
   the current system limit of 67.180.350"*, yerleştirme daha başta düştü.
2. **Sanal kaydırma GPU'daydı.** Fırın CPU'ya alınmıştı (7,25 GB sahne
   girdisi, 8 GB kart) ama `VirtualOffsetBake` karta gidiyordu:
   `d3d12: Unrecoverable GPU device error`, 100 MB'lık istek 20 MB'lık
   tampona.
3. **Kısmi pişirmede her koşum kendi ızgarasını üretiyordu.** Tek semt
   yüklüyken hücre ızgarası başka çıkıyor ve Unity sonucu *"partially
   baking the set with an incompatible cell layout"* diyerek **sessizce
   atıyor**. `partialBakeSceneList` "yalnız bunu YÜKLE" değil, "yalnız
   bunu PİŞİR" demek.
4. **`freezePlacement` ızgarayı değil, PİŞMİŞ YERLEŞİMİ dondurur.**
   D_Okmeydani pişip beş hücre yazdı; sonra D_Eyup yirmi altı dakika
   pişti ve diske hiç dokunmadı — donuk yerleşim o beş hücreydi.

## Karar

* **Hacim semtin kendi çizicilerinden türer** (`SemtProblari.SemtSiniri`),
  `Mode.Local`. Harita boyu yüzeyler dışarıda: ölçülen iki topluluk —
  birleşik bloklar ~340 m'de biter, harita boyu yüzeyler kilometrelerde —
  arasındaki eşik **1000 m** (`EnBuyukYapi`).
* **Açık suya prob hacmi konmaz.** `D_Bogaz` ve `D_Halic`'in tek kök
  nesnesi `TEKNELER_1632`: 15 km'ye serpilmiş tekneler. Prob hacmi
  gökyüzünün kapandığı yerler içindir. Semt yayılımları da iki topluluk:
  gerçek mahalleler 2045–3184 m, bu ikisi 7478–7913 m; eşik **5000 m**
  (`EnBuyukSemt`). Testi: `SemtProblariTests.AScatterOverOpenWaterCarriesNoProbeVolume`.
* **Sanal kaydırma kapalı.** İşi CPU'ya vermek, işin **tamamını** vermek.
  Bedeli: geometrinin içinde kalan birkaç prob dışarı itilmez ve orada
  geçersiz kalır; APV'nin geçerlilik karışımı onları zaten dışarıda
  bırakıyor.
* **Prob aralığı 6 m, örnek 16.** 3 m 67 milyon prob sınırını aşıyor
  (`-hezarfenYerlesimDene` ile dakikalar içinde ölçüldü: 3 geçmedi,
  4 geçti, 6 geçti). 4 m sınıra sığıyor ama **saate sığmıyor**: D_Galata
  altmış dakikada %21,9. 6 m prob sayısını 3,4 kat azaltır; 7 m'lik bir
  sokağa enine tek prob düşer ve sıçrama sokak boyunca yumuşar. Sıçrama
  zaten alçak frekanslıdır; bitmeyen bir fırının çözünürlüğü sıfırdır.
* **Fırın gökyüzü sıçramasını taşır, güneşinkini değil.** Kayıt:
  `0 lights`. İki yönlü ışık da `Realtime Only` ve öyle kalmalı —
  `ZamanSistemi` güneşi saate göre döndürüyor; sabit bir güneşi pişirmek
  günün her saatinde yanlış yönden gelen bir sıçrama demek olurdu.
  Güneşin sıçraması ekran uzayından (SSGI) gelir.
* **Referans semt D_Galata** (ADR 0078). Kısmi pişirme önce **tam** bir
  pişirme ister, tam pişirme bu makineye sığmıyor; o yüzden katman önce
  Galata'da bitirilir ve **orada ölçülür**.

## Denetimler (bu turda doğdu)

* **Yerleştirme hatası eşzamanlı yakalanır.** Hata
  `Lightmapping.BakeAsync` daha dönmeden düşüyor; kayıt dinleniyor ve
  koşum orada biter (çıkış 6) — on bir dakika boşa gitmez.
* **Ürün denetimi imza karşılaştırır**: hücre sayısı, diskteki toplam
  bayt ve son yazılma anı, pişirmeden önce ve sonra. Yalnız hücre
  sayısına bakmak yetmiyordu — D_Eyup'un boş pişirmesi, D_Okmeydani'nin
  yazdığı beş hücreyle "başarılı" görünüyordu. *Başkasının işiyle
  karşılanabilen bir denetim, denetim değildir.*
* **İlerleme okuması doğrulanır.** `Lightmapping.buildProgress` ikinci
  evrede **%44.366.093,8** döndü; aralık dışı okuma ilerleme değil bilgi
  yokluğudur ve takılma denetimini sağlıklı bir fırına karşı çevirirdi.
* **Takılma sınırı iki parçalı**: ilk adıma kadar 75 dk (prob
  yerleştirme boyunca ilerleme sıfırda durur ve hiçbir şey bozuk
  değildir), sonraki adımlar arası 25 dk. Süre tavanı 6 saat, ve o
  yalnız son çare.

## Alternatifler ve neden elenmedi/elendi

* **Araziyi fırından çıkarmak** — sahne girdisinin 4,17 GB'ının neredeyse
  tamamı 15 km'lik arazi. **Elendi:** arazi aynı zamanda yerdeki probun
  ALT yarım küresini kapatan şey; çıkarılsaydı sokak karanlık yerine
  gerçekdışı biçimde aydınlık okurdu. Maliyet israf değil, örtmenin bedeli.
* **Semt başına ayrı pişirme kümesi** — her semt kendi ızgarasını alırdı.
  Elendi: çalışma zamanında tek küme etkin olabiliyor ve semt akışıyla
  küme değiştirmek APV'nin yapmadığı bir şey.
* **Yüksekliği "yürünen bant" kadar kırpmak** — daha önce denenip
  elenmişti ve gerekçesi `SemtProblari` içinde duruyor: tek bir kutunun
  alt sınırı sahnenin en alçak noktasıdır ve Galata bir yamaç.

## Ölçü

Kapıyı tutan sayı tek: `tools/olcum/golge_orani.py --gok-yok --bolge
430,180,700,660 renders/tur/04_surici_kalabalik.png`. Fırın öncesi
mavi/kırmızı **0,000**, saçılım **0,001**. Fırın işini yaptıysa kapalı
kareler, aynı sahnede ölçülen sağlıklı komşularının (0,26–0,30)
ailesine katılır.
