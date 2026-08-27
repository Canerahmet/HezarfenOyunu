# ADR 0034 — Galata surları: hat boyunca bir landmark

**Tarih:** 2026-08-24
**Durum:** Kabul edildi — üretildi, testli; **Karar 15 kapandı** (ölçü tezden bulundu)
**İlgili:** ADR 0033 (Galata Kulesi), 0029 (sınırların dayanağı), 0031 §2 (sarım)

---

## 1. Bu landmark bir YAPI değil, bir HAT

Kule tekildi; sur **2,5 km**. Bu, üretim biçimini belirledi:

* **Perde duvar Unity'de tek mesh** olarak, GIS hattı boyunca üretiliyor.
  8 m'lik bir prefabla döşemek 300+ örnek ve 300+ çizim çağrısı ederdi;
  duvar hareket etmiyor. Kaldırım ve taş kaidelerde verilen kararın aynısı.
* **Burç ve kapı prefab** — onlar sayılı yapılar, bakılır ve incelenir.

Sonuç: **41 burç, 2 kapı, 1 050 mazgal**, 2 543 m hat.

## 2. Karar 15: taslak yerine RÖLÖVE

İlk turda duvar yüksekliği **9,0 m taslaktı** ve CLAUDE.md'nin kuralı gereği
`status: draft`, **D3** ile işaretlenip Caner'e soruldu. Caner *"tezden
bulmaya çalışalım"* dedi — ve tez bulundu:

> **Erdoğan, Batuhan Burhan (2013)**, *Galata Kent Surları ve Koruma
> Önerileri*, YL tezi, İTÜ FBE, dan. **Zeynep Ahunbay**. 442 sayfa, açık
> erişim; ayakta kalan sur, burç ve kapıların **2010 arazi ölçümleri**.

Ölçüler RESEARCH.md §5.2(b)'de tabloda. Sonuç: **taslak sayıların hepsi
ölçüyle değişti**, doğruluk **D3 → D2**, `status: measured`.

Bu turun asıl dersi şu: *"kaynak nitel"* bir **durum tespiti** değil, bir
**arama emri**. Nitel kaynak bulmak, aramanın bittiği anlamına gelmiyor.

### Rölöve modeli ÇÜRÜTTÜ: burçlar kare değil

İlk yazımda burç kare bir kuleydi. Tez ayakta kalan iki burcu da *"U planlı…
giriş cephesi dairesel"* diye tarif ediyor ve ölçüyor. Model yeniden yazıldı:
arkası dikdörtgen, öne bakan yüzü **yarım daire**, ve **iki ayrı boyda**
(9,80 × 7,70 / 16,16 m ve 7,02 × 5,84 / ~10 m). Yerleştirici ikisini
dönüşümlü koyuyor — hepsini tek boy yapmak ölçünün söylediğini görmezden
gelmek olurdu. Sahnede **20 büyük, 21 küçük** burç.

Kapı da ölçüye geçti: açıklık 3,60 → **2,70 m**, kemer üzengisi türetim
yerine **ölçülen 3,60 m**. Kural netleşti: **ölçü varsa çıkarım kullanılmaz.**

### İki bağımsız doğrulama

* Tez çevreyi **2 800 m**, alanı **~37 ha** veriyor — ADR 0029'da Galata
  halkasını ölçeklemek için kullanılan çapaların **ta kendisi**.
* Tez Galata Kulesi'nin dış çapını **~16 m** veriyor — ADR 0033'te TDV'den
  alınan **16,45 m**'yi doğruluyor, internette dolaşan "26,45 m" iddiasını
  çürütüyor.

Hâlâ ölçüsüz kalan tek şey **burçlar arası mesafe** (60 m, taslak).

## 3. AYNI HATAYI TEKRAR YAPTIM — ve ölçü yakaladı

Kaldırımda 698 üçgenin 697'si ters sarılmıştı (ADR 0031 §2), ders yazılmıştı,
SETUP.md'ye uyarı konmuştu. Sur perdesini yazarken **aynı hatayı yaptım**:

```
perde: YUKARI=1   ASAGI=4198
```

Üstelik bunu, sarımın doğru olduğunu söyleyen **kendi yorumumun altında**
yaptım. Yorum kanıt değildir; sayı kanıttır.

Ters sarımın üç sonucu: yüzey üstten ışıksız okunur, ışın sorguları arka yüzü
görmez (**çarpıcı fiilen yoktur**, oyuncu duvardan geçer), ve altta kalan şey
zemin sanılır. Düzeltildikten sonra: **4 198 yukarı, 1 aşağı**.

`LandmarkTests.WallCurtainTopFacesUp` artık üçgen normallerini sayıyor. Ders
şu: bu hata sınıfı yorumla önlenemiyor — **her elle üretilen mesh için normal
sayan bir test** gerekiyor.

## 4. Kapılar hattın üstünde DEĞİL — ölçülüp yazılıyor

Kapı noktaları (`GT_Azapkapi`, `GT_KuleKapisi`) ile sur halkası **ayrı taslak
kaynaklardan** geliyor ve çakışmıyorlar. Yerleştirici kapıyı hatta en yakın
noktaya taşıyor **ve taşıma mesafesini loglıyor**: bir kapının surdan kaç metre
uzakta çizildiği, düzeltilmesi gereken bir sayıdır — sessizce yutulacak bir
ayrıntı değil.

Duvar kapının olduğu yerde **kesiliyor** (±6 m), yoksa kapı duvarın içine
gömülürdü.

## 5. Kapı gerçek bir KEMERDİR

v1'de kapı iki paye + bir lento + koyu bir kutuydu ve render kusuru gösterdi:
açıklık **kare bir delik** olarak okunuyordu, üstelik 2,9 m yüksekliğinde.
Bir sur kapısını kapı yapan şey kemeridir.

Çözüm mevcut aletti: `street_kit.arched_panel` — mahallenin bütün kemerlerini
üreten fonksiyon (çeşme nişi, avlu kapısı, kilise penceresi). Sur kapısı da
aynı kemer karakterini taşıyor, yoksa şehirde iki ayrı mimarî dil olurdu.
Panel kalınlığı geçit derinliğidir; açıklık yapının **içinden geçiyor**.

`spring_z` önce açıklıktan türetiliyordu (ADR 0030'un sabit-sayı yasağı);
rölöve gelince **ölçülen 3,60 m** onun yerini aldı. Kural: türetim, ölçünün
yokluğunda geçerlidir.

## 6. Doğrulama kuralları

* **Burç Galata Kulesi'nden ince olmalı** (16,45 m): kaynak kuleyi "burçların
  hepsinden kalın" diye anıyor.
* **Burç duvardan yüksek** olmalı, yoksa duvarın bir parçası olur.
* **Burç dışarı taşmalı** (duvar kalınlığından fazla), yoksa duvarın önünü
  süpüremez — yani burç değildir.
* **Kapı açıklığı 2,0–4,6 m**: Harup Kapı ölçüsü **2,70 m**; mahalle sokağı
  4,6 m (ADR 0016) üst sınır, 2,0 m'den dar olursa geçit değil delik olur.
* **Burç U planlı**: derinlik genişlikten küçük olmalı (ön yüz yarım daire).
* **İki yanda gerçek paye kalmalı** (≥5 m toplam), yoksa kapı bir yapı değil
  duvarda bir boşluk olur.

## 7. HAT DÜZELTİLDİ — yelpaze, tepesi kule (2026-08-25)

Caner: *"sur hattını düzeltelim."* Aynı tez hattın **biçimini** de veriyordu
ve eski taslağı çürüttü:

> *"…kuzeyde **Galata Kulesi merkez olmak üzere** güneybatı ve güneydoğu
> yönlerinde iki noktaya doğru açılarak bir **yelpaze** biçiminde…"*
> *"…batıda **Azap Kapı**, kuzeyde **Galata Kulesi**, kuzeydoğuda
> **Tophane**…"* · *"**deniz tarafında Haliç**…"*

### (a) Kule surun İÇİNDE değil ÜSTÜNDE

Eski halka kuleyi 80 m güneyde bırakıp **içine alıyordu**; hatta eski denetim
*"kule poligonun içinde mi"* diye soruyor ve geçiyordu. Oysa kule yelpazenin
**tepe noktasıdır**. Denetim tersine çevrildi: ölçülen şey artık kulenin
**hatta olan uzaklığı** ve 5 m'yi aşarsa hata. Şu an **0,0 m**.

Bunun için `fit_ring_area` bir `about` parametresi aldı: ölçekleme ağırlık
merkezine göre yapılırsa kule belgeli koordinatından kayardı (ölçüldü: 27 m).

### (b) Deniz kenarı ayrı bir çizgi DEĞİL — kıyının kendisi

Güney kenarı elle çizmek **iki kez** başarısız oldu ve ikisini de ölçüm
gösterdi: hattın önce **%35'i**, kıyıya yapıştırma denemesinden sonra
**%44'ü** deniz seviyesinin altında kalıyordu.

Sorun yerleştirme değil **modeldi**. Halka artık iki parçadan kuruluyor:
**kara kolu kaynaktan** (Azapkapı → kule → Tophane), **deniz kolu kendi 1632
kıyı çizgimizden** (`shore_arc`). Su altında kalan oran **%20**'ye indi ve
kalan kısım kıyı şeridinin kendisi.

### (c) Ölçekleme KALDIRILDI — ve bu bir geri adım değil

Halka belgeli 37 ha'ya ölçekleniyordu, çünkü biçim bize, büyüklük kaynağa
aitti. Artık **biçmin iki parçası da belgeli**; ölçeklemek halkayı kıyıdan
koparırdı (37 ha'ya zorlandığında deniz kenarının %44'ü suya giriyordu).

Alan artık bir **sonuç**: ölçülen **30 ha**, belgeli **37 ha**. Aradaki 7 ha
sur hakkında değil **kendi kıyı çizgimiz** hakkında bir şey söylüyor ve öyle
raporlanıyor. Çevre ise **2,51 km** (belgeli ~2,80 km).

### (d) Azapkapı Haliç'in ortasındaydı

Ölçüldü: kapı noktası **−12,0 m** kotta, yani suyun içinde — koordinat ~300 m
fazla batıdaydı. Düzeltildi; şimdi **iki kapı da** yerleşiyor (önce biri
sessizce düşüyordu).

### (e) Burçlarda AŞIRI DÜZELTMEMİ geri aldım

Bir önceki turda ayakta kalan iki U planlı burcu okuyup *"kare yanlış"*
demiştim. Aynı tez şunu da yazıyor: *"…**dörtgen ve U planlı** burçlar ile
güçlendirilmiştir."* **Hayatta kalan örnek, örneklem değildir.** Üçüncü bir
varyant (`SurBurcu_Dortgen`, D3 — varlığı belgeli, ölçüsü değil) eklendi ve
yerleştirici üç tipi dönüşümlü koyuyor: **10 büyük U + 11 küçük U + 12
dörtgen**.

### (f) Yan etki: yeşil örtü yeniden üretildi

`G_Galata_Yerlesim` sınırı *"surun kendisidir"* diye tanımlı ve sur değişince
test onu yakaladı (bir köşe hattan 181 m uzakta). Yeşil örtü yeniden üretildi;
Galata yerleşimi **29,7 ha**.

## 8. Açık kalan

- **Alan farkı 30 ha / 37 ha.** Kendi 1632 kıyı çizgimiz muhtemelen fazla
  içeride. Kıyıyı düzeltmek ayrı bir tur (ADR 0008).
- **Burçlar arası mesafe** hâlâ taslak (60 m); tez burç *aralığı* vermiyor.
- **Ara noktalar hâlâ bizim.** Launay bölüm uzunluklarını (279 / 190 / 335 m)
  ve sokak adlarını veriyor; metrik güzergâh OSM geometrisi ister (ODbL) ve
  modern sokağı 1632 suruyla özdeşleştirmek ayrı bir iddiadır.
- **Hendek yok.** 15 m genişliğinde olduğu belgeli ama derinliği değil, ve
  hendek arazi kazısı ister (terrain heightmap işi) — ayrı tur.
- **Deniz surları yok.** Galata'nın kıyı kesimi ayrı; şu an halka karada
  kapanıyor.
- **Kule surla bağlanmadı.** Galata Kulesi surun üstünde durur; şu an ikisi
  ayrı duruyor ve kule halkanın içinde kalıyor.
