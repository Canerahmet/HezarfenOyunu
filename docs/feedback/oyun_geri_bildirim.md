# Oynarken gelen geri bildirim — Faz 8

Caner build'i çalıştırıp oynuyor; gelen her madde buraya, geldiği hâliyle
yazılıyor. Onay formatı "OK vN".

---

## 001 — Menüden oyuna girilemiyor

**Caner (2026-08-29, build 547 MB):**
> "baslangic menusunde tuslara basmama ragmen oyuna giremedim."

**Durum:** düzeltildi (v2 build bekliyor).

### Ne olmuştu

İki ayrı kusur vardı ve ikisi cümlenin iki yarısına birebir oturuyor.

**1. Fare — düğmelerin hiçbiri bağlı değildi.** Açılış sahnesi koddan
kuruluyor ve düğmeler `onClick.AddListener(acilis.Basla)` ile
bağlanmıştı. Bu çağrı bir **çalışma zamanı** dinleyicisi ekler: kurulum
anında düğme çalışır, ama sahne kaydedilirken dinleyici serileştirilmez.
Sahne bir daha açıldığında dördü de boşa basar.

Ölçüm: gönderilen `Acilis.unity` dosyasında kalıcı çağrı sayısı
**sıfırdı** (`git show HEAD:… | grep -c "m_MethodName"` → 0). Düzeltmeden
sonra **9**.

**2. Klavye — seçili hiçbir nesne yoktu.** `Submit` eylemi EventSystem'in
seçili nesnesine gider; seçili nesne yoksa hiçbir yere gitmez.
`firstSelectedGameObject` hiç atanmamıştı. Yani tıklama çalışsaydı bile
"tuşlara basmak" yine bir işe yaramayacaktı.

### Neden 382 test bunu yakalamadı

Çünkü hiçbiri **düğmeye basmıyordu**. Menüyü "doğrularken" panelleri
koddan çağırmıştım (`m.KredileriAc()`) ve ekran görüntüsüne bakmıştım —
bu, tıklama yolunun hiçbir adımına dokunmuyor: ne dinleyiciye, ne
EventSystem'e, ne seçime.

Bu, bu projede üçüncü kez aynı biçim: *ölçtüğün şey değil, ölçme biçimin
bozuktu.* Ağaçlarda sayaç "3.175 ağaç çizildi" derken her ağacın yaprağı
eksikti; gövde sayımında test kendi artığını sayıyordu; burada da
"menü çalışıyor" diyen gözlem menünün çalışmayan yarısını hiç geçmiyordu.

### Şimdi ne tutuyor

- `AcilisMenusuTests` (EditMode, 6 test) — sahneyi **diskten** açar ve
  her düğmenin kalıcı dinleyicisini, hedefini ve metodunun var olduğunu
  sınar; `firstSelectedGameObject`u ve raycast yolunu ölçer.
- `AcilisTiklamaTests` (PlayMode, 4 test) — olayı **EventSystem
  üzerinden** gönderir, yani oyuncunun bastığı yolun kendisini yürütür.

Kurucu artık `UnityEventTools.AddPersistentListener` kullanıyor ve
`AcilisMenusu` seçim düşerse geri koyuyor (fareyle boşluğa tıklamak da
seçimi düşürür).


---

## 002 — Kamera açısı değiştirilebilsin

**Caner (2026-08-29):**
> "oyunun kamera acisini degistirmeye izin versin. karakterin gozlerinden
> veya gta rdr ac gibi karakterin ustunden bir kamera olsun."

**Durum:** yapıldı (v3 build).

`V` ile göz ↔ omuz üstü. Omuz üstü kadraj küre taramasıyla engele göre
kısalıyor — 4,6 m'lik sokakta (ADR 0016) sabit bir kol duvarın içine
girerdi. Tekerlekle 1,4–6 m. Birinci şahısta gövde silinmiyor,
`ShadowsOnly`'ye düşüyor: gölgesiz yürüyen bir adam dikkat çekerdi.

Oyuncuya görünür karakter (`PF_Hezarfen_Sivil`) de takıldı — **daha önce
hiç yoktu**, oyuncu görünmez bir kapsüldü.

---

## 003 — Bazı evler yere temas etmiyor

**Caner (2026-08-29):**
> "bazi evler yere temas etmiyor, merdivenler yanlis yerlere koyulmus vs."

**Durum:** yapılar için **sıfır**; kaldırımda %5'lik açıklanamayan kuyruk
kaldı.

### Üç gerçek kusur

| yerleştirici | ne oluyordu | sonrası |
|---|---|---|
| `TryPlaceChurch` | `goto placed` bütün **kilise olmayan** yapıları kaide üretiminin üstünden atlatıyordu | hamam %100 → 0, fırın %94 → 0, mektep %88 → 0, kahvehane %83 → 0 |
| `PlaceProp` | hiç kaide üretmiyordu | dükkân %62 → 0, sebil %73 → 0, çeşme %65 → 0 |
| `PlaceTurbe` | hiç kaide üretmiyordu | `PF_Turbe_B` %100 → 0 |

Bir de `FootprintHeights` ayak izi köşelerini **döndürmeden**
örnekliyordu; ev sokağa dönük durduğu için yamaçta gerçek en alçak köşe
ıskalanıyor ve kaide kısa kalıyordu.

Son ölçüm: **18.338 yapı, sıfır görünür boşluk.**

### Merdivenler

Bu şehirde merdiven elle konmuyor — kaldırım şeridi araziyi izliyor ve
kot farkı bir rıht (0,17 m) biriktiğinde kendiliğinden basamaklanıyor.
Yani "yanlış yere konmuş merdiven" diye bir şey yok; olan şey
**bordürün yere inmemesi**di: bordür kesitin *en yüksek* noktasına kadar
iniyordu, oysa yol yamacı yanlamasına keser. %28,7 → **%5,0**, medyan
+0,04 m → −0,83 m.

Kalan %5 için iki açıklama denedim, ikisi de ölçümle yanlışlandı
(örnekler arası çukur; basamak rıhtları). Sebebi **bilinmiyor** ve kodda
öyle yazıyor.

### Bu maddede kendi payım

Beş kez yanlış cetvelle ölçtüm ve beşinde de sayı gerçekte olduğundan
kötü göründü: kayıkları deniz tabanına göre, evleri dünya eksenli kutuyla
(dönmüş bir evin kutusunun "içi" iki ev arasındaki boşluğa düşüyor),
kaldırımı "yüzey arazinin kaç metre üstünde" diye, ağaç filtresini hiç
eşleşmeyen bir adla (`Cinar` ile başlayan yok, `PF_Cinar_A` var), ve
build'in bittiğini `.exe` zaman damgasından anlamaya çalışarak (`.exe`
yalnız başlatıcı, değişmiyor).

İlk verdiğim **"%12,4 ev havada"** rakamı bu yüzden şişmişti. Düzeltmelerin
gerçekliğini aynı cetvelle önce/sonra kıyaslayarak doğruladım.

---

## 004 — Karakterin hızı yavaş

**Durum:** yapıldı. 1,4 → **2,2 m/s**, koşu 3,6 → **6,0**.

Eski sayı doğruydu (ortalama insan yürüyüşü) ama Galata'dan Beyazıt'a 40
dakika sürüyordu. Animator karışım eşikleri de bu sayılardan türüyor —
ve türemiyordu: `AnimatorKur` 1,4 ve 3,6'yı **elle** yazmıştı, yorumu ise
"WalkController ile aynı" diyordu. Hız değişince yorum hâlâ doğru
görünüyordu, sayı değil. Test yakaladı; hızın artık tek sahibi var.

---

## 005 — Modellerin kenarlarında titreme

**Caner:** *"isiksal mi yoksa baska bir pronblem mi var?"*

**Durum:** ışıksal değil. Sahnedeki kameranın `antialiasing` değeri
**None**'dı ve kod tabanında AA'ya dokunan tek satır yoktu. TAA/High
açıldı (SMAA değil — SMAA tek karelik bir filtredir ve **hareket eden**
ince geometride kaynamayı durdurmaz; şikâyet tam olarak harekette).

---

## 006 — Dünya boş duruyor; ırmak olsun

**Durum:** dereler kondu, **bostan ve yollar yapılmadı**.

Dereler için ADR 0074 şunu önermişti: *"yatak elle çizilmez, DEM'in en
alçak çizgisinden türetilir — bu bir ölçüm, bir çizim değil."* Sınadım,
**yanlış çıktı**: denize ulaşan en büyük havza 0,83 km² (Kağıthane
gerçekte 100 km²+ — havzalar haritanın dışında), kenardan giren yolların
oyuk derinliği **0,4 m**. Bu DEM'de vadi yok.

Caner ölçümü görüp C2'yi seçti: yatak oyuldu. Kağıthane 4.575 m, Alibey
3.293 m, Lykos 7.092 m. **Ağızlar** coğrafyadan (Haliç'in başı −3277,
2591; Marmara kıyısı z ≈ −2800), **aradaki güzergâh** en-az-tırmanış
yolundan. Üçü de T2; Alibey ve Lykos'un kaynak satırı olmadığı
`sourceNote`'larında yazıyor.

**Eksik kalan:** kapı yolları, bostan parselleri, bağ/meyvelik ve servili
mezarlık dokusu — yani ADR 0074'ün A seçeneğinin geri kalanı. "Boş
duruyor" şikâyetinin asıl gövdesi orası ve sıradaki iş bu.

---

## Tur 4 — 2026-08-30, "hatalarin bazilari devam ediyor"

Caner: *"hatalarin bazilari devam ediyor. duzelt. duzeltene kadar durma
ayrica bina ve evler birbirine cok yakin. daha genis olabilir yolar."*

51 ajanlı bir tarama 72 iddia üretti, 44'ü çürütüldü, **28'i ayakta
kaldı**. Bu turda kapatılanlar ve ölçüleri:

| kusur | önce | sonra |
|---|---|---|
| Evler komşusunun duvarından geçiyor | %20,0 (en kötü 2,34 m) | **%0,0** |
| Kaldırım altında hava kalan hücre | %3,0 | **%0,6** |
| Yapıların altında görünür boşluk | 0/17.060 | **0/17.220** |
| Sokakta açık genişlik (ortanca) | 4,6 m hedef | **7,67 m ölçülen** |
| Koşarken ayak kayması | döngü başına ~2,4 m | **2,0 cm** |
| NPC gövdesi yüzeye oturuyor mu | ortalama 0,63 m havada, 15/60 gömülü | **0,00 m, 0/60 sapma** |
| Vakit değişiminde donma | 40.000 A* tek karede | 400/kare, ~1,7 s'ye yayıldı |
| Görünür 60 gövde | liste sırasına göre | **en yakın 60** |
| Tekneler (373) | hiç yüklenmiyordu | Haliç ve Boğaz sahnelerinde |
| Güneş azimutu | 180° ters (batıdan doğuyordu) | 113° — doğu-güneydoğu |
| ESC | iki ayrı sahibi vardı, "Devam et" fare bakışını öldürüyordu | tek sahip: duraklatma menüsü |
| Uçuşta çarpıştırıcı | **yok** — dünyadan düşülüyordu | uçuş kapsülü + sürekli temas taraması |
| Uçuşta aerodinamik | `tuning` boş, serbest düşüş | WT_Faz0_Default bağlı |
| Uçuşta girdi | `PlayerFlightInput` yok | eklendi |
| Uçuşta imleç | ekranda kalıyordu | kilitli kalıyor |
| Kayıt: aranma seviyesi | yazılıyor, okunmuyordu | geri yükleniyor |
| Kayıt: Perde 2 ilerlemesi | alan vardı, dolduran yoktu | yazılıyor ve okunuyor |
| Yükleme sonrası NPC | 60 donmuş gövde birikiyordu | havuza dönüyor |
| Mahalle zemini | sekiz semtte hiç boyanmamıştı | yerleşim maskesi 5 semtte, 3.668 daireye kadar |

Bu turda **kendi açtığım** iki kusur da ölçümle yakalandı ve kapatıldı:
klipleri toptan yeniden üretmek karakteri parçaladı (ADR 0076), ve
kalabalık dilimlemesinin tavanı normal bir kareden küçüktü.

### Hâlâ açık

- Şehir içi zemin: mahallenin 200 m'lik karesinde %90 çıplak arazi,
  %82'sinin 4 m yakınında hiçbir şey yok. Zemin artık **boyanıyor** ama
  avlu içi nesne (odun yığını, küp, çardak, kuyu) yok. Bir sonraki tur.
- Kalan 28 bulgudan yükleme-ışınlaması (bulgu 17) ve görev üretimi
  (bulgu 15'in görev kısmı) kapatılmadı.

### Tur 4b — mahalle hayatı donatısı

Şehir içi boşluk için sekiz T2 varlık üretildi (odunluk ×2, su küpü ×2,
sepet, çardak, kuyu, çit — 36–1968 üçgen) ve **19.992 tanesi** binaların
9 m yakınına, semt sahnelerine kondu.

| ölçü | önce | sonra |
|---|---|---|
| 4 m içinde hiçbir şey olmayan zemin | %81,7 | **%69,1** |
| kare süresi (mahallede) | — | 11,4 ms (~88 FPS) |
| yapı-zemin teması | 0/17.220 | **0/37.212** |

İlk denemede yarıçap 26 m'ydi ve **gözle bakınca yanlış olduğu görüldü**:
eşyalar evin arkasındaki açık düzlüğe dağılıyor, çölde duran sandıklar
gibi okunuyordu — boşluğu doldurmak yerine görünür kılıyordu. 9 m'ye
çekmek ölçüde 3,4 puana mal oldu ama yerleşimi doğru yaptı.

**Asıl sebep bu değil.** Mahallenin arkasında *parsel* yok: evler sokağa
dizili bir şerit ve arkaları çitsiz, duvarsız açık arazi. Osmanlı evinin
bahçesi arkadadır ve **çevrilidir**. Boşluğun gerçek çözümü ev sırasının
arkasına parsel sınırı koymak; donatı ancak o zaman "avlu eşyası" gibi
okunur. Bir sonraki turun işi bu.
