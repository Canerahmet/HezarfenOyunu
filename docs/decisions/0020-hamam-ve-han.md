# ADR 0020 — Hamam ve han: imza çatıdadır

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — üretildi, Galata ve Balat'a yerleşti; Caner onayı bekliyor
**Tetikleyen:** ADR 0017'nin Faz 2b listesi; sıradaki iki yapı.
**İlgili:** ADR 0016, 0017, 0018, 0019; PLAN.md §7.1; RESEARCH.md §3

---

## 1. İkisinin de imzası çatıda

| | Ayak izi (m) | Yükseklik | LOD0 |
|---|---|---|---|
| `Hamam_A` | 16,3 × 30,9 | 9,80 | 3 558 |
| `Hamam_B` | 13,1 × 25,9 | 8,71 | 3 558 |
| `Han_A` | 32,3 × 26,8 | 10,20 | 7 858 |
| `Han_B` | 24,3 × 20,8 | 6,70 | 2 442 |

### Hamam — fil gözü

Hamamı hamam yapan şey planı değil, **kurşun kubbe kümesi** ve o kubbeleri
delen **fil gözü** camlarıdır. Sıcaklıkta pencere olmaz — buhar kaçar,
mahremiyet bozulur; ışık yukarıdan, kubbeye açılmış küçük yuvarlak camlardan
gelir. Fil gözü olmadan hamam, kubbeli bir depodur.

Kütle sırası **soğuktan sıcağa** dizilir ve değişmez:
soğukluk/camekân (en büyük kubbe, giriş) → ılıklık → sıcaklık (+ köşelerde
halvet hücreleri) → külhan. Külhan **arkadadır**: kirli işin yeridir, giriş
cephesine bakmaz. Bacası siluetin ikinci işaretidir.

### Han — avlu ve sağır dış duvar

Han bir **avlu** yapısıdır ve dışarıya **kapalıdır**: han aynı zamanda
kasadır. Bütün ışık ve hayat avluya bakar; avlu cephesinde iki kat **revak**
döner. Tek kapı vardır — taçkapı — ve geceleri kapanır. Üst kat odalarının her
biri bir kubbe ve bir baca taşır; hanın silueti damındaki o ritimdir.

## 2. Ölçülen ve düzeltilenler

### 2.1 Kapı vardı ama arkasında duvar duruyordu

Hamamın ön cephesi delikli panel olarak kuruldu, **ama kâgir kutu da olduğu
gibi bırakıldı**: açıklık gerçekti, arkası doluydu. Render'da "kapı yok" diye
okundu. `_domed_hall` artık `front_gap` alıyor — kutunun ön yüzünden panel
kalınlığı kadarı boş bırakılıyor.

Bu, delikli panel yaklaşımının tekrar eden tuzağı: **panel deliyor, kütle
kapatıyor.** İkisi aynı hacmi paylaşamaz.

### 2.2 Hanın avlusu kapatılmıştı

Kat silmesi ve dam, `(W, D)` boyutunda **tam plakaydı** ve avlunun üstünden
geçiyordu — han avlulu olmaktan çıkıp ambara dönmüştü. Üstten bakınca hata
apaçıktı: revaklar içeride duruyor ama gökyüzü görünmüyordu.

Silme ve dam artık **halka**: dört kenar kutusu, ortası boş. Hanı han yapan
şey avludur; onu örten her yüzey yapıyı başka bir şey yapar.

### 2.3 Sabit taçkapı ölçüsü, tek katlı hanı üretilemez yaptı

Basma kotu 3,60 m yazılıydı. Tek katlı `Han_B` (H = 4,20 m) üretilirken kemer
tepesi 5,29 m çıktı ve `arched_panel` **hata fırlattı** — iyi ki fırlattı.

Ölçü artık binadan türetiliyor ve gereklilik `HanParams.validate()`'te:
`h ≥ (0,652·w + 0,45) / 0,38`. 2,80 m'lik bir kapı en az **5,99 m** yükseklik
ister. Fıkıhtaki *"yüklü deve geçebilmeli"* ölçütünün han kapısındaki
karşılığı budur; alçak kapılı han işe yaramaz. `Han_B` tek katlı ama **6,40 m
yüksek** yapıldı — tarihsel olarak da doğrusu bu.

### 2.4 Han mahalleye değil, ÇARŞIYA aittir

Hamam ve hanı kiliseyle aynı kuralla yerleştirdim: *"çekirdekten uzak dur."*
Ölçüm hanın **46,8 m** uzağa düştüğünü gösterdi — boş yamaçta tek başına bir
han. Hata mesafede değil, kuralın kendisindeydi.

Han konut mahallesinin değil **ticaret çekirdeğinin** yapısıdır ve dükkân
sırasının yanında durur. `nearCore` bayrağı kuralı tersine çevirir: han
çekirdeğin 60 m yakınında aranır. Sonuç: mescide **33 m**, en yakın eve
25 m — mahallenin çarşı ucunda.

Hamam için kural aynı kaldı (mahallesi vardır ama meydanı mescitle
paylaşmaz), ama yakın taramanın menzili {2, 8} → **{2, 8, 16} m** yapıldı:
tek sokağa dört büyük yapı dizilince sonrakiler yakın düz yer bulamayıp
30-38 m geriye düşüyordu. Dokuya ait olmayan bir hamam, hamam değildir.

## 3. Kurşun ve cam paletin kendisine taşındı

`mosque_kit` kurşunu kendi içinde tanımlıyordu. Bu, `nature_kit`te bir kez
yaşanan hatanın aynısıydı: **`build_unity_maps.py` yalnızca palet + rol tarar**,
kitin içinde tanımlanan bir malzeme Unity'ye hiç ulaşmaz. Kurşun (`M_Lead_Sheet`)
ve cam (`M_Glass_Filgozu`) artık `ottoman_kit.PALETTES` içinde.

İkisi de **bilerek dokusuzdur** (uygun CC0 dokusu yok — ADR 0017).

## 4. Test kendi muafiyetini artık ELLE tutmuyor

`EveryOttomanMaterial_CarriesAllThreeMaps` kurşun ve camı haksız yere düşürdü —
doğru davrandı, çünkü muafiyet listesi `M_Opening_Shadow` diye **elle**
yazılmıştı ve yeni dokusuz malzemeler eklenince yalancı oldu.

Düzeltme: hangi malzemenin dokusu **olması gerektiğini** bildirim söyler
(`OttomanMaterialBuilder.PbrMaterialNames()`, `kind == "pbr"`). Elle tutulan
muafiyet listesi zamanla mutlaka yalancı olur; tek doğru kaynak bildirimdir.

## 5. Sonuç

| | Galata | Balat |
|---|---|---|
| Çekirdek | mescit + avlu + hazire | avlulu sinagog |
| İbadet (2. cemaat) | **Latin bazilikası** (büyük boy yerleşti) | Rum kilisesi |
| Hamam | var | var |
| Han | **var** (çarşı ucunda) | yok *(doğru)* |
| Kaldırım | 67 basamak | 76 basamak |

Testler: EditMode **103/103**, Blender öz-testi **7/7**. 18 HDRP malzemesi.
İnceleme: `renders/review/Hamam_A_v2/`, `Han_A_v2/`.
Sahne: `Captures/faz2_han_mahalle.png`.

## 6. Kalan boşluklar

> **Güncelleme 2026-08-21 (ADR 0021):** ilk üçü kapandı.

- ~~Hanın **avlu zemini yok**~~ → kaldırım taşı + kuyu döşendi (ADR 0021 §4).
- ~~Han revağının **sütunları yok**~~ → kürsü + gövde + başlık; ayak sütunun
  içinde kalacak kadar inceltildi, avlu köşeleri masif bırakıldı (ADR 0021 §4).
- ~~**Kurşun** dokusuz~~ → prosedürel kurşun örtü dokusu üretildi (ADR 0021 §1).
- Hamamda **göbek taşı ve kurna yok** — bu bir **iç mekân** boşluğudur ve
  yapıya özgü değildir; hiçbir yapıda iç mekân yok.
- **Cam** dokusuz ve öyle kalacak: fil gözü 20 cm'lik bir kabarcıktır, onu cam
  yapan şey albedo deseni değil pürüzsüzlüktür (ADR 0021 §7).
- Faz 2b listesinden ÜRETİLENLER: türbe, sıbyan mektebi, kahvehane (ADR 0021).
  Kalanlar: medrese, tekke, imaret, fırın, bozahane, değirmen, arasta, sebil,
  su terazisi, muvakkithane, namazgâh.
