# ADR 0017 — Kamusal yapı kiti ve landmark doğruluk merdiveni

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — mescit üretildi, gerisi **plana yazıldı**, Caner onayı bekliyor
**Tetikleyen:** Caner, 2026-08-20: *"cami, çeşme, dükkânlar vs düşünmemiştik…
ayrıca şehirdeki temel mimari yapıları, Ayasofya, Kapalıçarşı vs bunları tamamen
gerçeğe uygun şekilde modelleyelim… başka neler eklenebilir o tarihte, bilinen
araştır ve plana koy."*
**İlgili:** PLAN.md §7.1 (yeni), §8.1 (yeni), RESEARCH.md §4.1, ADR 0016

---

## 1. Önce plan denetlendi — kısmen haklıydı

Boşluk iddiası doğruydu ama tamamı değil:

| | Planda var mıydı? |
|---|---|
| Çeşme, kuyu, dükkân cephesi, avlu duvarı, iskele, mezar taşı + servi | **Vardı** — Faz 2 kit listesi |
| Ayasofya, Süleymaniye, Sultanahmet, Kapalıçarşı bedestenleri | **Vardı** — Faz 3 A-kademe |
| **Cami / mescit** | **YOKTU** |
| Hamam, han, medrese, tekke, sıbyan mektebi, türbe, imaret | **YOKTU** |
| **Kilise ve sinagog** | **YOKTU** — gayrimüslim mahalle paleti vardı ama ibadet yapısı yoktu |
| Fırın, kahvehane, bozahane, değirmen, arasta | **YOKTU** |
| Sebil, şadırvan, su terazisi, muvakkithane, namazgâh | **YOKTU** (namazgâh yalnızca Okmeydanı için) |

En ağır boşluk **mescit**tir. RESEARCH.md §4.1(g) mahallenin mescitten
dallandığını söylüyor; ADR 0016'nın yerleştiricisi çalışıyor ama merkezi boş.
Mescitsiz mahalle üretmek dokunun çekirdeğini eksik bırakmaktır.

Eksikler PLAN.md'ye **§7.1 Faz 2b — Kamusal Yapı Kiti** olarak yazıldı; her yapı
için 1632 notu ve kademesi var.

## 2. Ayrım: kamusal kit ≠ landmark

- **Landmark** tekdir ve belgelidir (T1) → Faz 3.
- **Kamusal kit üyesi** çoktur ve tipolojiktir (T2) → Faz 2b.

Bu ayrım yapılmazsa her mescit "hero varlık" muamelesi görür ve şehir asla
dolmaz; ya da tersine, Ayasofya bir parametre kümesi sanılır.

## 3. Mescit: varsayılan çatı AHŞAP, kubbe değil

Osmanlı klasik dönem cami tipolojisi dört sınıfa ayrılır: merkezî kubbeli,
tek birim kubbeli, **ahşap çatılı**, melez. Mahalle mescidi mütevazı uçtadır.

Varsayılanı ahşap çatı yapmak bir kolaycılık değil **tipolojik doğru**: kurşun
kubbe vakıflı büyük caminin işaretidir; mahalle mescidi komşusu olan evlerle
aynı alaturka kiremidi taşır. Kubbe `--roof dome` ile orta ölçek cami için
üretilir.

Yan fayda: ahşap çatı, ev kitinin dokularını kullanır — mahalle **bütünlüklü**
görünür, mescit sokağa yapıştırılmış yabancı bir nesne gibi durmaz.

### Minare oranı türetilir, yazılmaz

Gövde yarıçapı toplam yükseklikten (`H × 0,032`), şerefe yeri `H × 0,66`,
külah `H × 0,16`. Elle verilen oran, `minaret_h` değiştiğinde sessizce bozulan
orandır.

## 4. Ölçüm/render üç kusuru yakaladı

1. **Minare binadan kopuktu.** Yandan 0,9 m açığa konmuştu ve "yanına dikilmiş"
   duruyordu. Minare serbest kule değildir; kaidesi duvara girer. Kaide kenarı
   iki yerde ayrı hesaplanıyordu — `minaret_base_side()` ile tek yere alındı,
   kaide kenarının üçte biri kadar bindirildi.
   **LOD1/LOD2 de aynı merkezi okuyor** (`_minaret_center`); ayrışsalardı LOD
   geçişinde minare yana zıplardı.
2. **Açıklık arkasındaki karanlık panel 25 cm fazla içerdeydi.** Panel origini
   mid-kalınlıkta olduğu için `-t + 0.03` yazmak paneli iç yüzden içeri itiyordu.
   Aritmetikle kesin, render'a bakmadan bulunur bir hata.
3. **Kubbe fazetliydi.** Düz gölgelendirmede 24 segmentin her biri ayrı ayrı
   okunuyordu. Eğri yüzeyler artık yumuşak gölgelendiriliyor; **sekizgen kasnak
   ve pabuç bilerek düz kaldı** — yumuşatılsalardı köşeleri erir ve kubbeyle
   tek bir şişman kütle olurlardı.

Ayrıca ölçüldü: 0,55 m kâgir duvarda söve derin ve **parlak** okuyor (duvar
bölgesi L≈145, sapma 33) — açıklık uzaktan beyaz bir dikdörtgen gibi duruyordu.
Eksik olan doku değil **mimariydi**: cami pencerelerinin alt sırası demir
şebekelidir. Şebeke eklendi.

## 5. Ölçülen durum

| | Mescit_A (ahşap çatı) | Cami_Kubbe (kubbe) |
|---|---|---|
| Ayak izi × yükseklik | 11,5 × 13,3 × 19,9 m | 14,9 × 16,2 × 26,9 m |
| Üçgen LOD0 / LOD1 / LOD2 | **1 920** / 50 / 36 | **2 298** / 150 / 68 |
| Pivot | taban merkez ✅ | ✅ |

İnceleme paketleri: `renders/review/Mescit_A_v3/`, `Cami_Kubbe_v2/`.

## 6. "Tamamen gerçeğe uygun" — doğruluk merdiveni (PLAN.md §8.1)

Caner'in landmark isteği doğru hedef ama tek bir şey demiyor. PLAN.md'ye üç
basamak yazıldı: **D1 ölçülü** (kamu malı ölçülü çizimle, sayıyla doğrulanabilir),
**D2 görsel** (dönem gravüründen oran çıkarımı), **D3 tipolojik**.

> **Telif kapısı bir üretim kısıtıdır.** Müller-Wiener planları telifli, SALT
> görselleri CC BY-NC-ND. D1'e çıkmanın tek yolu **kamu malı ölçülü çizim**.
> Şansımız var: Ayasofya'nın ilk bilimsel plan ve kesitlerini **Grelot (1680)**
> çizdi ve kamu malı (Gallica bpt6k73264x). 1632'ye 38 yıl uzaklıkta.

**Kapalıçarşı için kritik uyarı** plana yazıldı: bugünkü kâgir hâl **1894
sonrasıdır**. 1632'de bedestenler kâgir, **çevresi ahşap**tı. Bugünkü çarşıyı
modellemek bu projede yapılabilecek en büyük tarihsel hatalardan biri olurdu.

Kural: bir landmark D1 iddia ediyorsa kamu malı ölçülü kaynağı `refs/` altında
ve `LICENSES.md`'de kayıtlı olmalı. Yoksa iddia D2'ye düşer — kaynaksız ama
"gerçeğe uygun" sanılan bir model, yanlış olduğunda kimsenin fark edemeyeceği
bir hatadır.

## 6.1 Sokak donatısı ve mahalle çekirdeği (aynı gün, ikinci tur)

### Çeşme

Klasik duvar çeşmesinin imzası **sivri kemerli niş**tir ve niş **gerçek bir
boşluk** olmalıdır — cepheye çizilmiş dikdörtgen değil. Boolean kullanılmadı:
kemer eğrisi boyunca şerit örüldü, altında iki ayak, üstünde alınlık. Kemer
**iki merkezlidir** ve tepe yüksekliği açıklıktan **türetilir**
(`a·√(1+2c)`), böylece açıklık değişince kemerin karakteri korunur.

Parçalar: teknelik, ayna taşı, lüle, kitabe, silme.

### Dükkân

Arasta biriminin karakteri **kepenk**tir: alt kanat aşağı katlanınca **tezgâh**,
üst kanat yukarı kalkınca **sundurma**. Kapalı bir kutu çizmek dükkânı depo
yapar; dükkânı dükkân yapan şey bu iki kanattır.

### Yeni doku rolü: kesme taş

Render'da ayna taşı ve kitabe **kırmızı ahşap** çıktı — malzeme hatasıydı, ikisi
de oyma taştır. Ama elimizde yalnızca **moloz taş** vardı ve moloz, işlenmiş bir
yüzeye konunca yapı "duvar parçası" gibi okunur, eser gibi değil. Poly Haven'dan
`large_sandstone_blocks` eklendi (**M_Stone_Cut**); teknelik, silme, ayna taşı ve
kitabe artık kesme taş.

> Kalan pürüz: 3 m'lik doku 0,42 m'lik kitabe levhasına düştüğünde bloğun
> rastgele bir parçası görünüyor. Dünya ölçekli UV'nin küçük trim parçalarındaki
> bilinen bedeli; çözümü ayrı bir trim dokusu.

### Çekirdek evlerden ÖNCE yerleşir

RESEARCH.md §4.1(g) mahallenin mescitten dallandığını söylüyor. Bu yüzden mescit,
çeşme ve dükkânlar ev yerleştirmesinden **önce** konur ve yerlerini rezerve eder.
Sonra konsalardı ya evlerin arasına sıkışırlardı ya da ev yerleştirmeyi geriye
dönük bozmak gerekirdi. Çakışma listesi (`taken`) artık yerleştirici genelinde
ortak.

Mescit sokaktan **geri çekilir** (3,5 m avlu payı) — mahalle mescidi cephe
hattına dizilmez. Çeşme sokak kenarında, dükkânlar mescidin karşı sırasında:
Osmanlı mahallesinde ticari çekirdek dinî çekirdeğin yanındadır.

| | |
|---|---|
| Donatı | 3 çeşme + 3 dükkân varyantı, **132-268** üçgen |
| Mahalle | 89 ev, **6 çekirdek yapısı**, 3 çıkmaz, 84 taş kaide |
| Testler | EditMode **101/101** |

Yakalamalar: `Captures/faz2_cekirdek_ust.png`, `faz2_mahalle_merkez.png`,
`faz2_cesme_dukkan.png`.

## 6.2 Avlu, şadırvan ve "çekirdek düzlüğe kurulur" kuralı (üçüncü tur)

Üretilenler: **avlu duvarı** (harpuştalı, 4 m ve 2 m), **kemerli avlu kapısı**,
**şadırvan**. Kapı, çeşmenin kemer kodunu yeniden kullanır — aynı mahallede iki
farklı kemer karakteri olması, gözün fark ettiği ama sebebini söyleyemediği
türden bir tutarsızlık olurdu.

Avlu Unity'de kurulur: **teras + merdiven + duvar halkası + kapı + şadırvan**.
Teras ve basamaklar **kaide mesh'ini yeniden kullanır** (ADR 0016) — ayrı bir
sistem kurmak iki farklı taş dokusu yoğunluğu demek olurdu.

### Kural 9: mahalle çekirdeği sokağın EN DÜZ yerine kurulur

İlk denemede çekirdek sabit bir noktaya (sokak uzunluğunun %42'si) konuyordu ve
orası dik bir yamaca denk geldi:

| | ilk | sonra |
|---|---|---|
| Avlu ayak izi altında kot farkı | ~5,8 m | **1,02 m** |
| Mescidin sokaktan yüksekliği | 5,8 m | **1,48 m** |

Teras doğru çalışıyordu; **yanlış olan yerleştirme kuralıydı**. 5,8 m'lik istinat
duvarı mahalleyi kale gibi gösteriyordu. Mahalle merkezi gerçekte de düzlüğe
kurulur: cami, çeşme, dükkân ve toplanma yeri düz zemin ister — ev tek başına
yamaca oturabilir, **meydan oturamaz**. Yerleştirici artık 20 aday nokta tarayıp
avlu ayak izi altındaki kot farkı en küçük olanı seçiyor ve seçimi **logluyor**.

| | |
|---|---|
| Yeni donatı | avlu duvarı 24, kapı 208, şadırvan 200 üçgen |
| Mahalle | 87 ev, **17 çekirdek yapısı**, 3 çıkmaz, 90 taş kaide |
| Testler | EditMode **101/101** |

> **Yan bulgu:** şadırvanın sekizgen kapakları n-gon olarak yazılıyordu ve FBX
> ihracı "4'ten fazla köşeli yüz, teğet uzayı hesaplanamıyor" diye uyardı —
> normal haritası o yüzde sessizce yanlış okunurdu. Kapaklar üçgen yelpazeye
> çevrildi; uyarı kayboldu.

## 7. Bu ADR'nin SÖYLEMEDİĞİ

- **Mescit, çeşme ve dükkân üretildi.** Hamam, han, medrese, türbe, **kilise,
  sinagog**, fırın, kahvehane, sebil, şadırvan, namazgâh — hepsi **plana
  yazıldı, üretilmedi**. Kilise/sinagog eksikliği Galata ve Balat için
  doğrudan engeldir.
- **Avlu ağaçsız.** Servi ve çınar yok; Osmanlı cami avlusunun ayrılmaz parçası.
  Doğal örtü Faz 4'ün işi ama avlu ağacı çekirdekle birlikte gelmeliydi.
- **Çeşme serbest duruyor.** Gerçekte mahalle çeşmesi çoğu zaman bir duvara
  gömülüdür; yerleştirici onu duvara oturtmayı bilmiyor.
- **Sokak yüzeyi hâlâ çıplak arazi** — kaldırım, merdiven basamağı yok
  (avlu merdiveni hariç).
- **Kurşun dokusu yok.** Poly Haven'ın 25 metal dokusunun hepsi paslı sac;
  kurşun paslanmaz, mat açık griye oksitlenir. Kubbe ve külah şu an **dokusuz**
  PBR ile veriliyor. Gerçek kurşun ayrıca **dikey kenet dikişleri** taşır —
  o da yok. Çözüm ya kendi dokumuzu üretmek ya da başka CC0 kaynak aramak.
- **Mescit henüz mahalleye yerleştirilmedi.** Yerleştirici (ADR 0016) mescidi
  çekirdek olarak kullanmıyor; doku hâlâ ondan dallanmıyor.
- **Şerefe mukarnası yok** — konsol bandı düz koni. Yakın planda eksik okunur.
- **Kubbe geçişi (pandantif/tromp) yok** — kasnak doğrudan duvara oturuyor.
- **İç mekân yok** (plan zaten istemiyor; görev mekânları ayrı üretilecek).

## Yeniden üretim

```powershell
$b = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
& $b --background --factory-startup --python tools\blender\gen_mescit.py -- `
    --asset Mescit_A --textured --roof timber `
    --out-blend art\blend\SM_Mescit_A.blend `
    --out-fbx  unity\HezarfenGame\Assets\_Import\SM_Mescit_A.fbx
& $b --background --factory-startup --python tools\blender\gen_mescit.py -- `
    --asset Cami_Kubbe --textured --roof dome --hall 12.0 --wall-h 7.0 --minaret-h 26.0 `
    --out-blend art\blend\SM_Cami_Kubbe.blend
```

---

## Sonraki turlar

- **ADR 0018** — kilise ve sinagog üretildi (üç tip), Galata ve Balat sahneleri.
- **ADR 0019** — servi, çınar ve hazire; cami avlusunun ağaçsızlığı kapandı,
  çeşmeye duvar kanatları eklendi.
