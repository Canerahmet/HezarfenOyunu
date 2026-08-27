# ADR 0033 — Galata Kulesi (1632): Faz 3'ün ilk landmark'ı

**Tarih:** 2026-08-24
**Durum:** Kabul edildi — üretildi, testli, Caner onayı bekliyor
**Tetikleyen:** Caner: *"devam edelim."* (Faz 2b kapandı, Faz 3 başladı)
**İlgili:** PLAN §8 / §8.1 (doğruluk merdiveni), ADR 0007 (dünya orijini)

---

## 1. Neden ilk bu

Galata Kulesi **dünya orijinidir** (28,974017 D / 41,025637 K), uçuş oradan
başlar ve oyunun simgesidir. S-kademede ilk sırada.

Landmark'ın kamusal kitten farkı ilkesel: kit üyesi *çok ve tipolojiktir*
(T2), landmark *tek ve belgelidir* (T1). Bir mescidin ölçüsü uydurulabilir;
Galata Kulesi'ninki uydurulamaz.

## 2. Yaygın bir iddia YANLIŞ çıktı — proje notu doğruymuş

Birçok popüler İngilizce kaynak konik çatıyı **II. Mahmud'un 1832'de
eklediğini**, Ceneviz kulesinin düz damlı/mazgallı olduğunu söylüyor. İlk
aramada tam bunu buldum ve projenin kendi notuyla ("sivri ahşap/kurşun
külahlı") çeliştiği için kaynağa indim.

İki belge iddiayı çürütüyor:

* **Evliya Çelebi** — çağdaş tanık — kuleyi *"tepesinde kurşun kaplı bir
  külah"* ile tarif eder (TDV).
* **1794 yangınında** *"kulenin ahşap külahı ve üst katları tamamen
  yanmıştır"* (T.C. Kültür ve Turizm Bakanlığı). Yanan bir şey vardı.

II. Mahmud külahı **eklemedi, yenisini yaptı**. Proje notu doğruydu; İngilizce
özet yanlıştı.

## 3. Kütle iki KOTTAN türedi — uydurulmadı

| Belge | Kot |
|---|---|
| 1509 depremi sonrası Mimar Murad bin Hayreddin kuleyi **13,20 m'den yukarı** yeniden inşa etti | 13,20 m |
| 1794 yangını onarımında boy **1,90 m kısaltıldı** | −1,90 m |
| II. Mahmud 1831 sonrası **32,60 m'den yukarısını tamamen yıktırdı** | 32,60 m |

Son ikisi birlikte 1632'yi verir: yıkılan kot 32,60 m ve o kot 1794'te zaten
1,90 m alçaltılmıştı → **1632'de kâgir gövde ≈ 34,50 m**. Üstünde kurşun kaplı
külah. Model **46,00 m** (saçaklı varyant) — bugünkü **62,59 m**'nin altında,
çünkü 1831 sofası ve 1875 sekizgen gözlem katları 1632'de yok.

**13,20 m'deki tuğla kuşak** ayrıca güzel bir doğruluk taşıyor: o kot 1509
onarımının **dikişidir**. Kuşak bir süs değil, iki yapım evresinin sınırı.

## 4. Evliya'nın SAYISI kullanılmadı

Evliya kuleyi **"118 mimarî arşın"** diye verir. 1 mimar arşını = 75,8 cm →
**≈89 m**, ki bugünkü 62,6 m'nin bile üstündedir. Evliya'nın abartısı bilinir.

Tanıklığı **külahın varlığı** için kullanıldı, **boyu** için kullanılmadı. Bu
ayrım, aynı kaynağın bir cümlesine güvenip ötekine güvenmemenin gerekçesidir
ve kodda da yazılıdır.

## 5. Külahın BİÇİMİ D3 — bu yüzden iki varyant

**Sağlam** (*YILLIK: Annual of Istanbul Studies*) iki dönem tasvirini
karşılaştırır:

* birinde külah **"dar ve yüksekçe"** konidir ve **mazgallı bir siperle
  çevrilidir**;
* ötekinde çatı **"çok daha basık ve geniş"**tir ve **saçakları mazgallardan
  dışarı taşar**.

Hangisinin 1632'ye ait olduğu kaynaktan çıkmıyor. İkisi de üretildi:

| Varyant | Külah | Toplam | LOD0 |
|---|---|---|---|
| `GalataKulesi` (saçaklı, **önerilen**) | 8,50 m basık, 0,95 m saçak | 46,00 m | 1 012 |
| `GalataKulesi_Mazgalli` | 14,00 m dar ve yüksek | 48,50 m | 884 |

Test ikisinin **gerçekten ayırt edilebilir** olduğunu ölçüyor: külah boyları
1,4 kattan fazla ayrışmalı, yoksa seçim sunmanın anlamı kalmaz.

Önerim **saçaklı**: 1632 kulesi 1510 Osmanlı onarımını taşır ve saçaklı
varyant Sağlam'ın *ikinci* (yani daha geç) tasvirine karşılık gelir.

## 6. Doğruluk basamağı: **D2**, ve neden D1 değil

Ölçüler var (16,45 / 8,95 / 3,75 m) ama **kamu malı ölçülü çizim yok**. PLAN
§8.1'in kuralı açık: D1 iddia eden landmark'ın `refs/` altında kamu malı
ölçülü kaynağı olmalı. Yok, o hâlde **D2**.

Bu turda `refs/` altına **hiçbir şey indirilmedi**: model metinsel ve metrik
kaynaklardan kuruldu, bir görselin üstünden çizilmedi. Lorck panoraması
Galata'dan **şehre bakar** — kule ressamın arkasındadır, dolayısıyla o kaynak
bu yapı için işe yaramaz. Kule için kullanılabilir kamu malı tasvirler
(Matrakçı Nasuh 1537, Braun-Hogenberg 1572, Pîrî Reis 1629 nüshası) bir
sonraki turun işi.

## 7. Render'ın gösterdiği iki kusur

**(a) 24 mazgalın hiçbiri görünmüyordu.** Külah doğrudan siperin üstüne
oturuyordu ve 0,95 m'lik saçak mazgalları tümüyle yutuyordu — geometri vardı,
görüntü yoktu. Oysa kaynak *"saçakları mazgallardan dışarı taşar"* diyor,
yani mazgal **görünür**. Külah artık kendi ahşap **kasnağının** üstünde
duruyor (1,30 m); gerçekte de kâgir bir siperin içine kurulan ahşap külah
böyle oturur.

**(b) Pencere düzeni sarhoş okunuyordu.** Üç ayrı kusur birden vardı:
sıralar tuğla kuşakların **üstüne biniyordu** (13,20 ve 17,17 hem kuşak hem
kat başlangıcıdır), her sıra farklı faz alıyordu, ve belgeli kat kotları
20,80'de bittiği için gövdenin üst **12 metresi bomboştu**. Artık kuşağa
0,9 m'den yakın sıra atlanıyor, faz yarım adım dönüşümlü, ve üst sıralar
belgeli aralıkların daralan ritminden (4,52 → 3,63 m) sürüyor — bu son kısım
**çıkarımdır** ve kodda öyle yazıyor.

Bir de kendi aletim yanlış şeyi ölçtü: üreticinin çap denetimi **ayak izine**
bakıyordu ve saçaklı varyantta 18,35 m gördüğü için haksız yere hata verdi.
Ölçülecek şey **gövde** çapıdır; `shaft_d` birleştirmeden önce ölçülüyor.

## 8. Çizici SİLİNDİRİK

Kutu collider, 16,45 m çaplı bir kulenin dibinde köşe başına ~3,4 m'lik
görünmez bir alan bırakırdı — oyuncu kuleye çarpar ama havada durur. UCX bir
12 kenarlı prizma.

## 9. Tuğla dokusu — kapandı (aynı turda)

v2'de kuşaklar `cutstone` ile üretiliyordu ve render kusuru gösterdi: kuşak
tuğla olarak değil, **gövdeye dolanmış ince bir gölge çizgisi** olarak
okunuyordu. Kuşağın anlamı rengindedir.

`tools/textures/gen_brick_texture.py` yazıldı (mermer/kurşun/yaprak
precedent'i; Poly Haven'dan tuğla indirilmedi, lisanssız görsel yasak).

**Karo boyu seçilmedi, hesaplandı.** Osmanlı almaşık duvarında tuğla
**35 × 35 × 4,5 cm**, derz **2,5–3 cm**. Sıra adımı 4,5 + 3,0 = **7,5 cm**,
tuğla adımı 35 + 2,5 = **37,5 cm**; **0,75 m** ikisine de tam bölünür ve iki
derz de belgeli aralıkta kalır. Başka bir boy ya dikişi gösterirdi ya da
derzi kaynağın dışına çıkarırdı.

### Ölçüt kusurun kendisidir — ve bir tuzak gösterdi

Ana eşik parlaklık değil, düzeltilen kusur: **kuşak yanındaki moloz taştan
ayırt edilebilmeli**, yani `old_stone_wall`dan CIELAB **ΔE ≥ 20**.

İlk denemede ölçü bir tuzağı ortaya çıkardı: tuğla tek başına taştan
**ΔE 30,8**, harç tek başına **23,3** — ama **karışımın ortalaması ΔE 12,3**
çıktı. Koyu kırmızı ile açık harcın ortalaması, taşın sıcak grisinin ta
kendisiydi. İki bileşeni de apayrı olan bir doku, uzaktan bakıldığında
ayırt edilemez olabiliyor — ve uzaktan bakıldığında bant zaten ortalamasına
iner, yani düzeltilen kusurun aynısı geri geliyordu.

Eşik indirilmedi; doku düzeltildi. Harç yaşlandırıldı (horasan zaten
pembedir, kireç beyazı değil), fırın değişkenliği gerçekçi genişletildi,
kenar aşınmasının harç payı 0,75 → 0,45 indirildi.

**Geçen ölçüler:** taştan ayrım **ΔE 21,1** (≥20) · sıra aralığı **7,50 cm**
(hedef 7,50 ±%15) · ince **1,31** (≥1,2) · kaba **4,22** (≥3,0).

Kaba enerji burada **taban**dır, mermerdeki gibi tavan değil: derzli bir
yüzey kaba olmalı. Kıyas aynı tablodan — sıva 1,04 · moloz taş 2,99 · derzli
kumtaşı 4,79 · arnavut kaldırımı 5,05.

Rol `brick` olarak palete girdi (her iki palette aynı: tuğla cemaate göre
değişmez) ve `M_Brick_Band` Unity'ye üretildi.

## 10. Sahneye yerleşti — ve yerleştirici GENEL

`LandmarkPlacer` (menü: **Hezarfen → GIS → Landmark'ları sahneye yerleştir**)
`landmarks_1632_local.json`'u okur ve **üretilmiş** olanları koyar. Katalogda
22 landmark var, biri üretildi; kalan 21 "henüz üretilmedi" diye loglanır ve
bu bir hata değil, kaydıdır. Faz 3 ilerledikçe `Built` sözlüğüne satır eklenir.

* **Konum kataloğdan:** Galata Kulesi tam **(0, 0)** — dünya orijininin
  tanımı (ADR 0007), tercih değil.
* **Kot araziden:** ayak izinin **en yüksek** köşesi (mahalle yerleştiricisinin
  8. kuralının aynısı). Ölçüldü: kot farkı **0,37 m** — tepe orada neredeyse
  düz.
* **Yön eğimden:** kulenin kapısı **yokuş aşağı**, yani şehre ve limana bakar.
  Sabit bir açı yazmak arazi değişince sessizce yanlışa dönerdi. Ölçülen:
  **205°** (güney-güneybatı) — Haliç ve liman tarafı.

Yerleştirilen kot **52,2 m**. Bu sayı ADR 0007'nin DSM şişmesini taşır
(Copernicus GLO-30 bir yüzey modelidir; Galata tepesinde yapılar irtifaya
karışır). Ölçtüm: orijin çevresinde **izole bir tümsek yok** — şişme bütün
tepeye yayılmış, yani yalnız kulenin altını düzeltmek çukur açardı. Bütün
şehir aynı yüzeyde durduğu için göreli geometri korunuyor; mutlak kot ADR
0007'nin bilinen kusurudur.

> ⚠️ **`JsonUtility` iç içe generic listeleri çözemez.** İlk yazımda
> yerleştirici kendi `[Serializable]` kaplarını taşıyordu; `List<List<Pt>>`
> sessizce **boş** geldi ve kule "konum yok" diye atlandı. `GeoJsonImporter`
> bu tuzağı zaten biliyor ve el yazımı bir ayrıştırıcıyla çözmüş — ikinci bir
> nüsha yazmak, tuzağın da ikinci nüshasını yazmak olurdu. Ayrıştırıcı
> paylaşıldı.

## 11. Açık kalan
- **Kapı düz bir dikdörtgen** — kemer ve söve yok. 1832 kitâbesi **YOK** ve
  öyle kalmalı.
- **İç mekân yok** (bütün kitte olduğu gibi). 1632'de kule tersane levazım
  ambarı ve zindandı.
- **Kule surların üstünde durur**; Galata surları ayrı bir S-kademe landmark'ı
  ve henüz üretilmedi. Kule şu an **tek başına** duruyor.
- **`refs/` altına hâlâ görsel indirilmedi.** D1'e çıkmanın tek yolu kamu malı
  ölçülü çizim. Kule için kullanılabilir kamu malı **tasvirler** (Matrakçı
  Nasuh 1537, Braun-Hogenberg 1572, Pîrî Reis 1629 nüshası) bir sonraki tur;
  Lorck panoraması bu yapı için işe yaramaz (Lorck Galata'dan **şehre** bakar,
  kule arkasında kalır).
