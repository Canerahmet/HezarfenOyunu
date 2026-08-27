# ADR 0027 — Okmeydanı: yerleştirmenin kuralı vakfiyeden çıkar

**Tarih:** 2026-08-22
**Durum:** Kısmen **geçersiz** — §3'ün "menzillerin yönleri kaynakta yok"
hükmü yanlıştı ve **ADR 0028** ile değiştirildi. Yön rüzgârdan çıkar; taşın
adı "nişan taşı" değil **baş taşı**dır; 706 m'lik taş sahneden kaldırıldı.
Vakfiye kuralı (§2), minaresizlik (§4), minber (§5) ve ölçülen kusurlar (§6)
geçerliliğini koruyor.
**Tetikleyen:** Faz 2b'de kalan sekiz maddeden ikisi (namazgâh, tekke) buraya aitti.
**İlgili:** ADR 0024 (arazi örtüsü), 0025 (mevsim/güneş), 0026 (yeşil doku)

---

## 1. Neden burası, neden şimdi

Faz 2b'nin kalan listesinde namazgâh ve tekke vardı ve ikisi de aynı yere ait:
**Okmeydanı** — RESEARCH.md'ye göre Hezarfen'in talim yaptığı yer. ADR 0026'da
bu alanı zaten belgeye dayanarak **ağaçsız** bir poligon olarak işaretlemiştik;
şimdi içini doğru şeyle dolduruyoruz.

Üretilenler: `Namazgah_Okmeydani`, `Namazgah_Kucuk`, `Tekke_Okcular`,
`Tekke_Kucuk`, `MenzilTasi_Nisan`, `MenzilTasi_Ayak`, `MenzilTasi_Buyuk`.

## 2. Yerleştirme kuralı belgeden okundu

II. Bayezid'in vakfiyesi meydana *"bir karış tecavüz edilmemesi, **yapı,
mezar, su yolu, bağ ve bahçe** yapılmaması"*nı kesin olarak yasaklar. Buradan
iki kural çıktı ve ikisi de testle kilitlendi:

- **Yapılar meydanın DIŞINDA** — tekke de namazgâh da çepere konur.
- **Menzil taşları meydanın İÇİNDE** — ve bu çelişki değil: taş ne yapıdır,
  ne mezar, ne su yolu, ne bağ. Meydanın kendi donanımıdır.

İkinci madde bir **yorumdur**, vakfiyede yazmaz. Ama taşların meydanda dikili
olduğu RESEARCH.md'de kayıtlı, yani yorum kaynakla çelişmiyor, onu açıklıyor.
Test iki yönü de ölçüyor: yalnızca "yapı dışarıda" demek, taşları da dışarı
atan bir hatayı geçirirdi.

### Ölçülen: yarıçapla kenar bulunamaz

İlk yazımda tekke `merkez + yarıçap × 0,86` ile konuyordu ve **poligonun
içinde kaldı**. Sebep: `radius_m` çevrel dairenin yarıçapıdır; çokgenin kenarı
merkeze çok daha yakın olabilir. Nokta artık merkezden dışarı **yürünerek**
bulunuyor — poligondan çıkana kadar, sonra bir pay daha.

## 3. Menzil taşı: dünyadaki nesne belgedeki sayıyı ölçüyor

Menzil taşları tek parça **mermer sütunlardır** ve üstlerinde okçunun adı,
mesleği, atış yönü, **mesafesi** ve tarihi yazar. **İkişer dikilirler**:
okçunun durduğu yerde *ayak taşı*, okun düştüğü yerde *nişan taşı*. Yani bir
çift taş aradaki mesafeyi **ölçer**.

RESEARCH.md'de belgeli tek mesafe: IV. Murad devrinde dikilen ~**706 m**'lik
taş. Sahnede ölçülen:

```
MenzilTasi_706m   706,0 m      <- BELGELI
MenzilTasi_665m   665,0 m      tipolojik
MenzilTasi_624m   624,0 m      tipolojik
MenzilTasi_588m   588,0 m      tipolojik
```

`OkmeydaniTests.EachStonePairMeasuresItsWrittenDistance` her çiftin mesafesini
adındaki sayıyla karşılaştırıyor ve 706'lık taşın **varlığını** ayrıca şart
koşuyor. Taş bir süs değil bir ölçüdür; ölçmüyorsa yalan söylüyor demektir.

Menzillerin gerçek **yönleri** kaynakta yok; azimutlar taslaktır.

## 4. İki yokluk, ikisi de testle korunuyor

**Tekke 1632'de MİNARESİZDİR** — minare ancak 1770–71'de eklendi. `TekkeParams`
sınıfında `minare` diye bir parametre **yoktur**: olmayan bir şeyi
kapatılabilir kılmak, bir gün yanlışlıkla açılmasına davetiyedir. Test
yüksekliğin ayak izine oranını sınıyor (minare eklenirse oran hemen bozulur)
ve etiketteki "MINARESIZ" notunu arıyor.

**Kağıthane'de 1632'de kasır yoktur** (Sâdâbâd 1722) — ADR 0026'da mesire
olarak işaretlendi, yapı konmadı.

## 5. Minber yedi yaşında

Okmeydanı namazgâhı **minberlidir** ve minberini **Gürcü Mehmed Paşa 1624–25**'te
eklemiştir. Yani oyunun geçtiği yılda minber yenidir. Bu, kahvehaneden sonra
oyunun ikinci zaman işareti: 1632'de *var ve yeni*.

Namazgâhın biçimi TDV'den: zeminden **seki** ile ayrılmış platform + kıble
yönünde **mihrap taşı** (niş değil, arkasında mekân olmayan bir **taş**) +
minber. Ölçü çapası Gelibolu Azebler Namazgâhı'nın belgelenmiş **12 × 8 m**'si;
Okmeydanı'nınki için ölçü yok.

## 6. Ölçülen kusurlar

**(a) Tekke "bir mescit ve iki baraka" gibi okunuyordu.** Hücreler mescidin
yanına diziliyordu. Tekkeyi tekke yapan şey **avludur**: mescit arkada,
hücreler avlunun iki yanında, kapıları içeri. Yeniden kuruldu.

**(b) Hücre sırası bir hücre boyu dışarı kaymıştı** — merkez hesabında işaret
hatası. Avluyla hücreler arasında boş bir şerit kalıyor ve revak önlerinde
ayrı duran bir pergola gibi okunuyordu.

**(c) Revak yüksekliği ayrı bir sayıydı** ve hücre damının altında kalıyordu.
Artık hücre yüksekliğinden **türetiliyor**; iki ayrı sayı bir gün ayrışır.

**(d) Avlu kapısı içi dolu bir bloktu** — girişin ortasında duran bir duvar
parçası gibi. İki paye + lento oldu: kapı, içeri girilebildiği için kapıdır.

**(e) Menzil taşının kitabesi sütuna dayanmış ayrı bir levha gibiydi.**
Sekizgen gövdenin düz yüzü `r·cos 22,5°`'de durur; pano artık onun birkaç
milimetre dışına oturuyor — yazının okunduğu bir oyuk kadar.

## 7. Kalan boşluklar

- Menzil **azimutları** uydurma; gerçek menzil yönleri kaynakta yok.
- Taşlarda **yazı yok** — kitabe bir oyuk, harf değil. Aynı boşluk mezar
  taşlarında da duruyor (ADR 0019).
- Tekkede **meydan şeyhinin odası** ayrı bir mekân olarak yok; hücrelerden
  biri sayılıyor. IV. Murad devrinde meydan şeyhi Hacı Süleyman padişahın
  hocasıydı — bu oda bir görev mekânı olabilir (Faz 6).
- Okmeydanı sahnesi **yalnız bu üç yapıyı** taşıyor; alanın kendisi boş ve
  bu doğru, ama menzil taşları 300'e çıkacaksa (19. yy) o Faz 4'ün işi.
- Faz 2b'de kalan altı madde: imaret, arasta, bozahane, değirmen, su terazisi,
  muvakkithane.
