# ADR 0050 — Yedikule: bir çelişkiyi tahminle değil ölçümle kapatmak

- **Tarih**: 2026-08-27
- **Durum**: Kabul (Caner onayı bekliyor — `docs/feedback/yedikule.md`)
- **Bağlam**: Faz 3, A-kademe. Kara surlarının (ADR 0049) eksik kalan
  parçaları: hisar ve yedi kapı.

## Bulgu — iki kaynağım birbirini tutmuyordu

`LM_Yedikule` (landmark koordinatı, daha önce **SURVEYED** işaretlenmişti)
ile `wall_land` hattının Yedikule noktası **186 m** açıktı. Biri yanlıştı
ve hangisi olduğunu bilmiyordum.

Tahmin etmek yerine **üçüncü bir ölçü** alındı. Haritada:

| | konum | LM_Yedikule'ye | sur hattına |
|---|---|---|---|
| "Yedikule Zindanları" (`historic=castle`) | 28,923209 / 40,993040 | **74 m** | 152 m |
| "Yedi Kule Hisarı Müzesi" | 28,923931 / 40,993569 | **15 m** | 222 m |

Yani **landmark doğru, hat yanlıştı**. Elle izlenmiş sur çizgisinin güney
ucu düzeltildi ve Yedikule'nin hatta uzaklığı **186 m → 53 m** oldu (kalan
53 m hisarın kendi 160 m'lik gövdesinin içindedir).

Bu, koordinat denetiminin (ADR 0043) bulamayacağı bir hataydı: denetim
"tepeyi taçlandırıyor mu, kıyıda mı" diye sorar, "iki çizimim birbirini
tutuyor mu" diye sormaz. **İki bağımsız kaynak çeliştiğinde üçüncüsünü
getir** — kural bu.

Mermerkule (Marmara ucu) aynı izim hatasını paylaşıyor ve aynı kadar
kaydırıldı; onun için **bağımsız ölçüm yok**, düzeltme türetilmiştir ve
öyle işaretlidir.

## Yan etki: bir test bunu yakaladı

Sur hattı değişince `GreeneryTests.WallBackedBoundariesMatchTheWalls`
patladı: suriçi yerleşim sınırının bir köşesi hattan **155 m** uzaktaydı.

Sınır zaten surdan **türetiliyordu** (`basis=walls`), yalnızca yeniden
üretilmemişti. Ama testin işi tam buydu — "sınır surun kendisidir"
iddiası, sur değişince sessizce yalan olmuştu. `greenery_build` yeniden
koşturuldu.

## Karar 1 — Beşgenin yarıçapı **ölçülen alandan** türer

Belgeli açık alan **15 000 m²**. Düzgün beşgen alanı 2,378·R² →
**R = 79,4 m**, kenar ≈ 93 m. Elle girilen bir "hisar büyüklüğü" yok —
kara surlarındaki burç aralığıyla aynı ilke (ADR 0049).

## Karar 2 — Yedi kule **birbirinin aynı değil**

Üçü Fatih'in **dairesel** kuleleri, dördü Bizans'tan: Altın Kapı'nın iki
**mermer** kulesi ve Theodosius surunun iki burcu. Kaynak bunu açıkça
ayırır (*"Altın Kapı ve Roma surları tarafından oluşturulan batı bölümü
hariç, Fatih döneminde yapılan, dairesel planlı üç büyük kule ve onları
birleştiren üç uzun beden duvarı"*).

"Yedi kule" deyip hepsini aynı yapmak **adı korur, yapıyı siler**.

## Karar 3 — Altın Kapı **üç kemerlidir** ve üçü aynı değil

Ortadaki büyük kemer yalnızca imparatorlara, iki yanındaki küçükler
halka. `arched_panel` bütün açıklıkların aynı ölçüde olmasını ister
(T-kavşağı yok) — aracın kendi notu *"farklı ölçü isteyen yer ayrı panel
ister"* diyor. Üç ayrı panel kuruldu.

## Karar 4 — Kule **taşmadıkça** kule olmaz

Mermer kuleler ilk kurulumda duvarla aynı düzlemdeydi ve ondan yalnızca
3 m yüksekti; aynı malzemeyle birlikte render'da "duvarın kalın yeri"
gibi okunuyorlardı. Bir kuleyi kule yapan şey yükselmesi kadar
**taşmasıdır** — Galata burcunda ölçülen şey de buydu (ADR 0034).
3 m dışa taşırıldı ve tam boya çıkarıldı.

Ayrıca kuleler kenarın **uçlarındaydı** ve köşe kulelerine 6,3 m
mesafedeydi: ikisi tek yığın gibi okunuyordu. Altın Kapı'nın mermer
kuleleri **kompozisyonu kucaklar**, köşede durmaz.

## Karar 5 — Kara kapısı Galata'nın kapısı **değildir**

Galata'nın Harup Kapı rölövesi 2,70 m açıklık verir ama o **2 m**
kalınlığında bir duvarındır. Burada duvar **5 m**, burçlar **25 m**; aynı
açıklık bu kütlede bir mazgal deliği gibi okunurdu.

ADR 0049'da "Galata kapısını buraya koymadım" demiştim; **ölçüsünü de
almadım**. Açıklık duvar kalınlığından türedi (4,5 m). Ve kapı **kendi iki
burcuyla** gelir: gerçek kara sur kapıları iki burcun arasındadır ve
kapıyı kapı yapan şey o iki kütledir.

Kapı bloğunun genişliği ilk denemede 20 m'ydi ve **doğrulama reddetti**:
4,5 + 2×9,0 = 22,5 sığmıyordu. 24 m oldu.

## Karar 6 — Yönü elle yazmadım

Hisar surun içindedir ve Altın Kapı **dışa** açılır. `face_deg` sur
hattının Yedikule'deki **dış normalinden** hesaplandı: **261,2°**.
Şehrin içi de elle yazılmadı — deniz surlarının ağırlık merkezidir
(ADR 0049).

## Sonuç

- `Yedikule` LOD0 2 634; ayak izi 165,9 × 161,2 m, yükseklik 27,40 m.
  Yerleşim (−4147, 22,2, −3655), yön **261,2°**, tepe 49,6 m; merkezi
  sur mesh'ine **0 m** — hisar surun üstündedir.
- `KaraSurKapisi` LOD0 650; 24,7 × 11,7 m, 22,0 m.
- Sahnede **19 landmark**; surda **7 kapı** (hepsi hatta 0 m kaydırmayla)
  ve 192 burç.
- EditMode **223/223**.

## 1632 için bu yapı bir haberdir

Kulelerden birinin adı **Genç Osman Kulesi**'dir ve sebebi taze: **II.
Osman 1622'de burada öldürüldü**. Oyunun geçtiği yıl olaydan **on yıl**
sonrasıdır ve tahttaki IV. Murad onun kardeşidir. Bir başkası **Hazine
Kulesi** (hisar devletin hazinesini tutar), bir başkası **Zindan
Kulesi**.

**III. Ahmed Kulesi adı 1632'de yoktur** — III. Ahmed 1703-1730 arasında
hüküm sürer. Kule vardır, ad sonradandır. Katalog kule **adlarını** değil
**sayısını** taşır: ad bir yorumdur, yedi bir olgudur.

## Açık kalanlar

- Kule ve duvar **yükseklikleri** kaynakta yok; Theodosius burçlarının
  25 m'sinden türedi (**D3**).
- Hisarın **içi** boş: 15 000 m²'lik avluda cephanelik, zindan ve hazine
  yapıları vardı.
- Yedi kapının hepsi **aynı** prefabla kondu. Gerçekte Topkapı,
  Edirnekapı ve Silivrikapı birbirinden farklıdır; ayrım henüz yok.
