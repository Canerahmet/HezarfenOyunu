# ADR 0019 — Servi, çınar, hazire: yeşil doku mimarî kadar taşıyıcı

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — üretildi, iki semte de yerleşti; §10 ikinci turda genişletildi
**Tetikleyen:** Caner, 2026-08-20: *"devam edelim, eksiklikleri de tamamla."*
**İlgili:** ADR 0016 (mahalle dokusu), ADR 0017 (kamusal yapı kiti), ADR 0018,
RESEARCH.md §4

---

## 1. Neden ağaç, mimarî kadar önemli

RESEARCH.md §4 yeşil dokuyu şöyle tarif eder: *"mezarlıklar **servi
alanlarıyla** kent içi büyük yeşil kütleler."* 1632 İstanbul'unun siluetinde
servi, minare kadar taşıyıcı bir öğedir — ikisi de dikey, ikisi de tekrar eder,
ikisi de uzaktan tanınır.

Cami avlusunun ağaçsız olması bir eksik değil, **yanlış**tı: avlu taş bir
meydan gibi okunuyordu, oysa oraya gölgelenmek için oturulur. Bu boşluk üç
turdur listede yazılıydı; bu turda kapandı.

| | |
|---|---|
| `Servi_A/B/C` | 7 / 10 / 13 m, sütunsu, **172 üçgen** |
| `Cinar_A/B` | 12 / 16 m, yayvan taçlı, **356 üçgen** |
| `Mezar_Erkek/ErkekB/Kadin` | şahide + ayak taşı, 36-72 üçgen |

## 2. Ölçülen: servi ile kavağı ayıran şey orandır

İlk üretimde 13 m'lik servi **3,7 m genişlikte** çıktı — boy/en oranı 3,5.
O bir servi değil, kavaktır. Sebep basit bir birim hatasıydı: `spread`
yarıçaptır, ben çapla karıştırdım. Sütunsu servide oran **6-10** arasıdır;
düzeltme sonrası 13 m × 1,78 m → **7,2**.

Çınarda oran terstir: taç boya yakın ya da ondan geniştir (16,3 m × 15,6 m).
İki ağacı da "yeşil kütle" diye aynı orana koymak, İstanbul siluetinin en
tanınır karşıtlığını silerdi.

## 3. Bilinçli boşluk: dokusuz yaprak

Gövde ve yaprak **dokusuz PBR**dir. Poly Haven'da kabuk dokusu var ama yaprak
**alfa atlası** yok; yapraklı ağaç alfa kartı ister. Lisansı LICENSES.md'de
belgelenmemiş görsel indirmek yasak (CLAUDE.md). Bu yüzden taç katı geometri
olarak, ölçülmüş renkle üretiliyor.

Karşılığı kabul edilebilir: servinin siluet değeri **kütlesindedir**, yaprak
detayında değil. Yakın planda ağaç bir koni gibi okunur; kayıtlı boşluk.

Çınarın tacı üç bindirmeli lobdur. İki kapalı kabuk siluetin kenarında
kesişirse o kesişim **çentik** olarak okunuyordu; boolean birleşim olmadan tek
çare yan lobları ana lobun içine gömmek oldu.

## 4. Mezar ekseni inançtan gelir

Kilisenin apsisini doğuya döndüren kuralın (ADR 0018 §3.4a) mezar ölçeği:

| | Eksen | Neden |
|---|---|---|
| Müslüman hazire | kıbleye **dik** (azimut 151,6° + 90°) | ölü sağ yanına, yüzü kıbleye dönük yatar |
| Hristiyan mezarlığı | **batı-doğu** | baş batıda, ayak doğuda |

İki mezarlık üstten bile ayırt edilir. Servi taşların **arasına** değil
kenarına dikilir: kökü mezarı bozar, gölgesi yolu gölgeler.

**Sinagogun yanına mezarlık KONMAZ.** İlk yazımda Balat'ın çekirdeğine de
hazire koydum — dönem ve gelenek hatası: Yahudi defni yerleşimin **dışında**
yapılır, İstanbul'da Hasköy ve Kuzguncuk mezarlıkları mahallelerin dışındadır.
Cami yanındaki hazire ise Osmanlı pratiğinin kendisidir. Kilisenin mezarlığı
`PlaceChurch` içinde ayrıca kurulur.

## 5. Aynı tuzağa iki kez düştüm: `taken` yanlış ölçüttür

Avlu ağaçları ve hazire ilk denemede **hiç yerleşmedi** ve hiçbir uyarı
çıkmadı. Sebep ikisinde de aynıydı: çekirdek kendi çevresini
`depth × 0,75 + 2` yarıçapla `taken` listesine yazıyor, ağaç ve hazire de
`Overlaps(taken, …)` ile sınanıyordu — yani **kendi avlularına çarpıyorlardı**.

Doğru ölçüt rezervasyon değil **yapının kendisi**dir: ağaç ile hazirenin
kaçınması gereken tek şey caminin gövdesidir; kendi yerlerini kendileri
rezerve eder ve evler onlara çarpmaz.

Ders: `taken` "burası doludur" demez, "**buraya EV konmasın**" der. Çekirdeğin
parçaları o kuralın istisnasıdır.

## 6. Çeşme serbest durmaz

"Duvar çeşmesi" adı tesadüfi değil: çeşme bir bahçe, avlu ya da yapı duvarına
gömülür. Kanatsız üretilen ilk sürüm sokakta **anıt gibi** duruyordu — tek
başına duran taş kütle, çeyrek yüzyıl sonrasının meydan çeşmesidir.
`CesmeParams.wings` eklendi: harpuştalı iki duvar kanadı. `Cesme_B` bilerek
kanatsız kaldı (zaten bir yapıya bitişik konur).

## 7. Çeşme tek denemeyle bırakılmaz

Balat **susuz** kurulmuştu ve hiçbir uyarı çıkmamıştı: tek aday nokta elendi,
`PlaceProp` 0 döndü, kimse bakmadı. Oysa mahallenin toplanma sebebi sudur;
çeşmesiz mahalle, mescitsiz mahalle kadar eksiktir. Artık sokak boyunca altı
konum × iki yan denenir ve hepsi elenirse **loglanır**.

## 8. Apsis örtüsü yarım kubbedir

ADR 0018 §7'de "yarım koni; gerçekte yarım kubbe olurdu" diye kayıtlıydı.
Kapatıldı: `hz.make_dome` ile konka. Konik külah apsisi kule kaidesine
benzetiyordu; kubbenin yarısı zaten duvarın içinde kaldığı için bedeli de yok.

## 9. Sonuç

| | Galata | Balat |
|---|---|---|
| Ev | 100 (17 varyant) | 88 (9 varyant) |
| Çekirdek yapısı | 37 | 20 |
| Hazire | **var** (12 mezar + 4 servi) | yok *(doğru)* |
| Avlu ağacı | 4 servi | 4 servi |
| Kilise mezarlığı | var | var |
| Çeşme | var | var |

Testler: EditMode **103/103**, Blender öz-testi **7/7**.
Sahne: `Captures/faz2_galata_hazire.png`, `faz2_balat_sinagog_2.png`.
İnceleme: `renders/review/Servi_A_v1/`, `Cinar_A_v1/`, `Mezar_Erkek_v1/`,
`Cesme_A_v3/`.

## 10. İkinci tur — §3, §10'daki boşluklar kapatıldı (2026-08-21)

Caner: *"eksiklikleri tamamlayıp devam edelim."*

### 10.1 Yaprak dokusu: indirilemiyorsa ÜRETİLİR

§3'te "Poly Haven'da yaprak alfa atlası yok, lisanssız görsel indirmek yasak"
diye kayıtlıydı. Yanlış olan sonuçtu: kısıtın etrafından dolaşmak yerine
**kaldırmak** mümkündü. `tools/textures/gen_foliage_texture.py` yaprak
dokusunu prosedürel üretir — çıktı bizim eserimizdir, üçüncü taraf hakkı yok.

Yükseklik alanı = rastgele döndürülmüş elips "yaprak öbekleri"nin toplamı,
koordinatlar modülo ile sarılır (döşenebilir). Ondan BC / N / ARM türetilir;
dosya düzeni Poly Haven'la **birebir aynı** (`meta.json` + `T_<id>_*`), böylece
`materials.py` ve `build_unity_maps.py` özel durum bilmez. Tek fark kök klasör
— rol artık kendi kökünü söyleyebiliyor (`root=`).

Servi ince ve koyu (1,6 m ölçek), çınar iri loblu ve açık (2,4 m). Taç artık
düz yeşil bir blob değil.

**Kabuk indirildi:** `bark_brown_01` (servi) ve **`bark_platanus`** (çınar).
İkincisi doğrudan platanus kabuğudur — çınarın alacalı, pul pul dökülen kabuğu
ağacın tanınma işaretlerinden biridir; genel kabukla değiştirilemez.

### 10.2 Sokak yüzeyi: kaldırım kendiliğinden merdivenlenir

Mahalle kurulmuştu ama yaya hâlâ **çıplak arazi** üstünde yürüyordu.
`AddPaving`/`BuildPaving`: kaldırım şeridi + kenar bordürü, tek mesh, dünya
ölçekli UV, `M_Paving_Kaldirim`.

Yüzey araziyi birebir izlemez, **basamaklara yuvarlanır** — yürünen yüzey düz
olmak zorundadır. Kot farkı bir rıht (0,17 m) biriktiğinde bir basamak atılır.
Böylece merdiven "eklenmez", eğimden **doğar**: düz yerde hiç çıkmaz, dikte
kendiliğinden görünür. RESEARCH.md §4 zaten *"dar, çıkmaz ve **merdivenli**
sokaklar"* diyordu.

Ölçüm: Galata **67 basamak**, Balat **76**; kaldırım mesh'i 2 258 üçgen.

Doku seçimi önizlemeler karşılaştırılarak yapıldı (isimden değil):
`cobblestone_05` düzgün derzli Avrupa parkesi — fazla modern; `brick_pavement_03`
tuğla — yanlış malzeme; `cobblestone_02` yuvarlak dere taşı, arnavut
kaldırımına yakın ama 4,6 m sokakta çakıl yolu gibi okuyor. Seçilen
**`cobblestone_floor_001`**: çamura oturmuş düzensiz yassı taş, "medieval"
etiketli.

### 10.3 Kitabe ve hazire duvarı

Şahideye **oyulmuş yazı panosu** eklendi: yazının kendisi doku işidir (henüz
yok), buradaki ışığın tutunacağı **girinti**dir — panonun kenarı gölge çizgisi
bırakır ve taş yakın planda boş kalmaz. 72 → 132 üçgen.

Hazire artık **duvarlı**: alçak harpuştalı duvar halkası (22 parça). Duvarsız
bırakılınca taşlar araziye serpilmiş gibi duruyordu; hazireyi hazire yapan şey
o sınırdır.

## 11. Ölçülen ama ÇÖZÜLMEYEN: sokak kapkaranlık

Kaldırım döşendikten sonra sokak seviyesinden bakıldığında yüzey **3/255**
parlaklıkta ölçüldü — okunabilir bir doku değil, siyah bir koridor.

Sebep aydınlatmadır, kaldırım değil: güneş 42° yükseklikte (alçak değil), ama
sahnede **dolaylı aydınlatma yok** — GI pişirilmemiş, gökten gelen ambient
dar sokağa ulaşmıyor. Cephe malzemesi, kaldırım dokusu ve UV'lerin hepsi
doğrulandı (albedo bağlı, UV dünya ölçekli).

Bu bir **aydınlatma fazı** işidir ve planda yeri vardır; burada kaydediliyor ki
"kaldırım dokusuz görünüyor" diye yanlış yerde aranmasın.

## 12. Kalan boşluklar

- **Sokakta dolaylı ışık yok** (§11) — aydınlatma fazı.
- Yaprak dokusu var ama taç hâlâ **katı geometri**; siluetin kenarı keskin.
  Alfa kartı ayrı bir iş.
- Mezar taşında **yazı yok** (pano var, harf yok).
- Ağaç **rüzgârda kıpırdamıyor** (vertex animasyonu yok).
- Çıkmazlar da taş döşeli; gerçekte arka sokakların çoğu **toprak**tı.
