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

### Caner'e soru (iki seçenek + öneri)

1. **Güneşi `Mixed` yap ve turun saatinde pişir.** Sıçrama o saat için
   doğru olur, günün öteki saatlerinde yanlış yönden gelir. Gün
   döngüsü görsel olarak bozulmaz ama sıçrama onu izlemez.
2. **Pişirme için ayrı bir gök**: sabit, açıkça parlaklığı verilmiş bir
   statik aydınlatma göğü (HDRI ya da gradyan), çalışma zamanındaki PBR
   göğünden bağımsız. Sıçrama saatten bağımsız ve tutarlı olur;
   bedeli, gece ile gündüzün aynı dolaylı ışığı almasıdır.

**Önerim (2).** Sıçrama ışığı bu oyunda sokağın okunabilmesi için var;
saatle dönen bir sıçramanın kazandırdığı doğruluk, gece sokağın yine
simsiyah olmasının yanında küçük kalır. Ayrıca (2) tek bir pişirmeyle
bütün günü kapatır — bu makinede pişirme süresi başlı başına bir kısıt.

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
