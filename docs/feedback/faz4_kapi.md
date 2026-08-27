# Faz 4 — Kapı Paketi

**Durum:** onay bekliyor
**Tarih:** 2026-08-27
**Ne için:** PLAN'ın kuralı — her faz kapısında kabul kriterlerinin
karşılandığına dair **kanıt** sunulur. Bu, o kanıt.

---

## 1. Kabul kriterleri — dördü de karşılandı

Plan Bölüm 9 dört şey istiyor. Sırayla:

### ✅ "Galata + Üsküdar semtleri dolu"

Beş semt dolduruldu, ikisi değil:

| semt | mahalle | ev | mescit | kilise | sinagog |
|---|---:|---:|---:|---:|---:|
| Galata | 34 | 2 931 | 30 | 5 | 5 |
| Üsküdar | 30 | 2 632 | 30 | — | — |
| Suriçi Doğu | 43 | 3 571 | 37 | 3 | 8 |
| Suriçi Batı | 26 | 2 324 | 24 | 2 | 2 |
| Eyüp | 9 | 790 | 9 | — | — |
| **toplam** | **142** | **12 248** | | | |

Okmeydanı **bilerek boş**: orası talim alanıdır, yerleşim değil.

Ayrıca **371 tekne** suya kondu — Haliç'te köprü olmadığı için tekne bir
donatı değil, ulaşımın kendisi.

### ✅ "Kule tepesinden 360° bakışta FPS hedefi tutuyor"

**FPS ölçmedim, üçgen saydım** — ve bu bilinçli. FPS tekrarlanabilir
değildir: editörün yükü, arka plandaki içe aktarım, pencere boyutu sayıya
karışır. `CityBudget` analitik sayar (her LODGroup için kamera uzaklığının
seçtiği kademe + frustum), yani aynı sahne + aynı bakış = aynı sayı.
Sekiz yön × beş eğim taranır, en pahalı kare bütçeyi belirler.

| bütçe | ölçülen | sınır | kullanım |
|---|---:|---:|---:|
| üçgen | **407 785** | 2 500 000 | %16 |
| draw call tabanı | **333** benzersiz (mesh,malzeme) | 1 500 | %22 |
| doku belleği | **293 MB** | 4 096 MB | %7 |

Draw call sayısı **instancing'in inebileceği taban**: aynı mesh + aynı
malzeme tek çağrıda basılır ve şehir 17 ev varyantından kuruludur. Ham
sayı 10 034; tabanın 333 olması, hiçbir optimizasyonun kurtaramayacağı bir
durumda olmadığımızı gösterir.

Uzak semt **impostor'ı ve doku atlası yapılmadı** — ve bu bir eksik değil,
ölçümün sonucu: bütçenin %16'sındayken o işlerin karşılığı yok.

### ✅ "İki farklı seed 'aynı şehir gramerinde farklı sokaklar' üretiyor"

İki test bağlandı ve ikisi de **gerçekten koşuyor**:

- `SameSeedGivesTheSameQuarterCores` — aynı tohum aynı çekirdekleri verir.
- `ADifferentSeedMovesTheStreetsButKeepsTheGrammar` — başka tohum
  çekirdeklerin %65'inden fazlasını oynatır ama **mahalle sayısını**
  ±%33 içinde tutar. Çünkü sayıyı belirleyen şey tohum değil, semtin
  **alanı** ve arazinin elemesi.

İlk yazımda ikisi de `Assert.Ignore` ile atlanıyordu (arazi açık sahnede
yoktu) ve koşumda **atlandılar**. Bu projede o hata üç kez yakalandı
(ADR 0041/0043/0044): *atlanan test geçen test gibi görünür.* Testler
artık araziyi kendileri yüklüyor.

### ✅ "Sur dışı ve Boğaz yamaçları çıplak arazi değil"

- **42 857** arazi ağacı, 7 prototip
- Dört katman gerçekten karışmış: çim %46,7 · kıyı %36,3 · toprak %15,7 ·
  kaya %1,3
- Sahnelerde ayrıca **2 292** servi/çınar (hazireler)

Graybox dama tahtası yok.

---

## 2. Bu fazın bulduğu yanlışlar

Faz 3 gibi, kapıda asıl bakılması gereken şey bunlar. Hepsi ölçümle
bulundu:

- **Nadir kurumlar mahalle başına konuyordu.** Tek örnek sokakta doğruydu
  (o mahalle semti temsil ediyordu); semt 34 mahalleye bölününce **22
  hamam, 22 medrese, 22 Latin kilisesi** çıktı. Kural artık: mahalle ne
  söylenirse onu kurar, kaç tane olacağına semt karar verir.
- **Bütçe sayısı yokluğu gizledi.** "%6,7 kullanım" verimlilik gibi
  okunuyordu; aynı karede yalnızca **472 nesne** çiziliyordu. Ev 55 m'de
  orta kademeye, **3 434 m'de küle** düşüyordu — oysa planör 50-100 m'de
  uçuyor ve Hezarfen'in uçuşu **3 336 m**, yani varış semti uçuş sırasında
  yoktan var oluyordu. Boyuta duyarlı merdivenle çizilen nesne 472 → 3 924.
  **Ders: ucuz bir kare, boş bir kare olabilir.**
- **Semt içeriği arazi sahnesine yazılıyordu** ve sahne 932 KB'dan 15 MB'a
  çıktı. Streaming tasarımı zaten semt başına sahne öngörüyor.
- **Sebil her mahalleye konuyordu** (34 tane). Çeşme mahallenin suyudur;
  sebil hayır kurumudur.
- **Tekneler konfeti gibi dağıldı** (618'in 600'ü açık suya). Su
  bölgelerinin kendi sınırı var; tekne Haliç'e ve iskele önüne aittir.
- Ölçüm aracının kendisinde iki hata: yalnız ufka bakmak ve eğim işaretinin
  ters olması.

---

## 3. Senden gereken

### 3a. Kapı onayı

```
OK Faz 4
```

### 3b. Hâlâ bekleyen dört tasarım kararı

Faz 3'ten devrediyorlar ve kapı onayı bunları kapatmıyor. **ADR 0037 artık
kritik**: Faz 5 (Hezarfen karakteri) uçuşun oynanabilir olmasına bağlı.

| ADR | soru | önerim |
|---|---|---|
| **0037** | Doğancılar'a 3336 m için gereken süzülme 64,6:1, gerçek 11,56:1. Rüzgâr çözmüyor (205 km/h gerekir); gereken ortalama yükselen hava ~0,9 m/s | Yükselen havayı **mekanik** yap |
| **0039** | İncili Köşk örtüsü — TDV kubbe, Eldem ahşap; iki varyant hazır | Kararı sen ver |
| **0046** | 1632 kıblesi 133,70° | Onayla |
| **0051** | Beyazıt şadırvan kubbesi | Konmasın |

### 3c. Bilmen gereken taslak sayılar

Mahalle sayıları, yoğunluklar ve nadir kurum bütçeleri **T2/taslaktır**:
1632'nin mahalle sınırları kayıtlı değil. Hepsi `DistrictDef`
ScriptableObject'lerinde durur — düzeltmek bir alan değiştirmektir, kod
değişikliği değil.

---

## 4. Faz 4'te bilerek yapılmayanlar

- **Çamaşır ipi** — plan sayıyor ama kaynakta yok, ve riskli: müslüman
  mahallesinde çamaşır **avluya** asılır. Sokak üstü çamaşır daha çok
  Napoli imgesidir; uydurmadım.
- **Kuş sürüsü** — çalışma zamanı VFX'i, Blender varlığı değil.
- **Mahalle kuyusu** — `_kuyu` avlu yapısı olarak var; sokak kuyusu için
  kaynak yok ve çeşme + şadırvan zaten kaynaklı su altyapısını veriyor.
- **Occlusion culling, impostor, doku atlası** — ölçüldü, gerekmiyor.

---

## 5. Doğrulama

- EditMode **244 / 244 yeşil**, **sıfır atlanan**
- `Assets/_Import` boş
- git + LFS, `main` güncel
- 63 ADR
