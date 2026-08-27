# ADR 0028 — Menzilin yönü rüzgârdan çıkar; taş mermerdir

**Tarih:** 2026-08-23
**Durum:** Kabul edildi — üretildi, testli, Caner onayı bekliyor
**Tetikleyen:** Caner: *"bilgim yok ara"* (ADR 0027'deki **Karar 10**)
**Değiştirdiği karar:** ADR 0027 §3 — "menzillerin yönleri kaynakta yok"
**İlgili:** ADR 0026 (yeşil doku), 0027 (Okmeydanı), 0019 (prosedürel doku)

---

## 1. Sorduğum soru yanlıştı

ADR 0027'de dört menzil taşını kuzeybatı yelpazesine (305°–342°) dağıtmış ve
"azimutlar taslaktır, kaynakta yok" diye not düşmüştüm. Kaynakta **vardı**.
Yalnızca derece olarak değil, **rüzgâr adıyla** yazılıydı — ben "yön" diye
aradığım için bulamamıştım.

> "Rüzgâr, menzil okçuluğunda atış vaktinin ve **atış yönünün**
> belirlenmesindeki temel unsurdu. **Her menzil için belirlenen bir rüzgâr
> vardı**… Böylece hem **rüzgâr arkaya alınıp** atıcının rüzgârdan
> faydalanması sağlanır…" (Kaya & Şahin 2022, HÜTAD)

Menziller zaten rüzgârla anılır: *Lodos Menzili*, *Poyraz Menzili*, *Yıldız
Menzili*. Yani menzilin adı çoğu zaman **yönünün kendisidir**.

### İşaret, tek başına yanlış olabilecek yerdi

Türkçe rüzgâr adları rüzgârın **geldiği** yönü söyler. Rüzgâr arkaya
alındığına göre ok **ters** yöne gider:

```
ok azimutu = rüzgârın geldiği azimut + 180°
```

Bunu fizikten çıkarmak yetmezdi (menzil için elbette rüzgâr arkadan olur) —
kaynağın kendisi doğruluyor: Yıldız Menzili'nde poyrazla atışa izin
verilmesinin gerekçesi "**iki rüzgârın da kuzeyden esmesi**"dir. Yani yıldız
ve poyraz *kuzeyden eser*, adları kaynağı gösterir.

Yanlış işaret sessiz bir hatadır: her şeyi 180° döndürür, hiçbir şey kırılmaz,
sahne "çalışır". `OkmeydaniTests.ShotDirectionIsTheWindReversed` bu yüzden üç
rüzgârı tek tek kilitliyor.

## 2. Yapı değişti: her menzilin KENDİ ayak taşı var

ADR 0027'de tek bir ortak ayak taşı vardı ve dört taş oradan yelpaze gibi
açılıyordu. Bu benim uydurmamdı. Kaynak açık: *"Menzildeki atışların
yapılması için atıcının konumlandığı yeri belirlemek adına ayak taşı
dikilirdi"* — her menzil için, o menzile ait.

Bir menzil bu yüzden bir **koridordur**: ayak yeri ile hava yeri arasında
uzanan, 17. yüzyıl kaidesinde her yandan **40 gez** genişliğinde bir şerit.
Dışına düşen ok sayılmaz — "salkı düştü".

Farklı rüzgârların koridorlarının birbirini kesmesi sakıncalı değil: atış
yalnızca o menzilin rüzgârı estiği gün yapılırdı, iki koridor aynı anda
kullanılmazdı.

## 3. Sahnedeki beş menzil ve nereden geldikleri

| Menzil | Hava | Ok | Taş | Gez |
|---|---|---|---|---|
| Havandelen Solak Bali | yıldız | 180° | Bursalı Şüca | 1251,5 |
| Tozkoparan (Delikli Kaya) | yıldız | 180° | Tozkoparan İskender | 1279,5 |
| Yıldız | yıldız | 180° | Mîrî Âlem Ahmed Ağa | 1146 |
| Arkurı | gündoğusu | 270° | Tozkoparan İskender | **1281,5** |
| Lodos | lodos | 45° | Mîrî Âlem Ahmed Ağa | 1271 |

Hepsinin adı, havası ve mesafesi belgelidir; hepsi 1632'den önce açılmıştır.

### Delikli Kaya'nın 80 gezi bir süs değil

Tozkoparan, Şüca'nın Havandelen'deki taşını 28 gez geçti ama oku ana taşın
**80 gez şastına** düştü — koridorun iki katı dışına. Şüca taraftarları "aşırı
salkı" diye itiraz etti, tartışma II. Bayezid'e gitti, ve Şeyhülmeydan
Hamdullah Efendi'nin kararıyla taş **ayrı bir menzil** sayıldı.

Sahnede bu yüzden iki menzil **aynı ayak taşını** paylaşır ve Tozkoparan'ın
taşı eksenden tam 80 gez yanda durur. Biri "düzgün dursun" diye taşı eksene
çekerse menzilin var olma sebebi de gider — test bunu bekliyor.

## 4. Sahneye KONMAYAN şey

ADR 0027'nin başlık sayısı olan **"IV. Murad'ın ~706 m'lik taşı" kaldırıldı.**
Üç sebep, üçü de aynı disiplinin parçası:

* Sayı akademik olmayan bir kaynaktan geliyordu.
* Havası — yani yönü — bilinmiyor.
* **Tarihlenemiyor.** IV. Murad 1623–1640 hüküm sürdü; taşın 1632'den sonra
  dikilmiş olma ihtimali yarı yarıya.

Tarihlendiremediğim bir taşı sahneye koymak, tekkeye minare koymakla aynı
hatadır. Aynı sebeple 19. yüzyıl Sultan II. Mahmud Menzili de yok.

Ayrıca ADR 0027'deki 588 m'lik "menzil" **kaideye göre menzil bile değildi**:
menzil açmanın alt sınırı **900 gez**tir (≈594 m) ve 588 m bunun altında.
`EveryMenzilIsLongEnoughToBeOne` artık bunu ölçüyor.

## 5. Gez kaç metre — cevabı olmayan soru, sayısı olan çözüm

Kaynaklar **0,60–0,66 m** arasında dağılıyor ve birim yüzyıllar içinde
**küçülmüştür**: aynı mesafe 15–16. yy'da 1236 gez, 19. yy'da 1279,5 gez.
Acar'ın 19. yy taşları üzerindeki *ölçümü* 60,74 cm, literatürün kullandığı
değer 66 cm.

**0,66 seçildi**, çünkü çapa kaynağımızın (TDV) yayımlanmış rekoru —
845,66 m — ancak bu değerle çıkıyor: 1281,5 × 0,6599 = 845,66.
Ölçüm zincirlerini kendi kafama göre harmanlamaktansa bir kaynakla tutarlı
kalmak yeğdir.

Çözümün asıl kısmı şu: **taş gezi taşır, metreyi değil.** `GezM` tek bir
sabittir; karar değişirse o değişir ve beş menzil birlikte kayar. Test
`RecordStoneMatchesTheEncyclopaediaMetre` sabiti çapa kaynağa bağlıyor.

## 6. Koridor araziye oturtuluyor — ölçülen kusur

Menzilleri meydanın merkezine göre simetrik yerleştirmiştim. Ölçüm,
Arkurı'nın baş taşının kot **−12 m**'ye, yani **Haliç'e** düştüğünü gösterdi.
Poligonun içinde olmak yetmiyor: alanın ~%8'i su.

Koridor artık eksen boyunca ve yanal olarak kaydırılarak aranıyor; ölçüt,
koridor boyunca ölçülen **en düşük kotu** büyütmek, eşitlikte merkeze yakın
kalmak. Beş koridorun tamamı 80–99 m kotunda, Okmeydanı platosunda.
`NoStoneStandsInWater` bunu kilitliyor.

## 7. Taş MERMERDİR — ve bunu iki ölçüm söyledi

İnceleme karesi iki kusur gösterdi; ikisi de gözle değil sayıyla saptandı:

* Sütunun ortalama parlaklığı **36,7/255**, yanındaki çayır **162,5** — taş
  zeminden **4,4 kat koyu**. Mermer, ışıkta duran en açık şeydir.
* Sütun boyunca satır ortalamalarının baskın dikey periyodu **0,95 m** —
  yani duvar dokusunun **taş sırası** düzeni. Kaynak ise "tek parça mermer
  sütun" diyor. Doku, taşın tek parça olmadığını söylüyordu.

`cutstone` bir **duvar** malzemesidir. Poly Haven'da lisanslı mermer yok,
lisanssız görsel indirmek yasak (CLAUDE.md) — kurşun ve yaprakta olduğu gibi
çözüm üretmek: `tools/textures/gen_marble_texture.py`. Damar, bant sınırlı
döşenebilir bir gürültünün **eş seviye eğrisidir**; kıvrımlı, çatallanan,
periyodik olmayan çizgiler verir.

## 8. Ölçemediğim şeyi ölçüyormuş gibi yapmadım

"Taş sırası var mı" sorusunu bir eşiğe bağlamayı **üç formülasyonla** denedim
ve üçü de bilinen-kusurluyu (`large_sandstone_blocks`) bilinen-iyiden
(onaylanmış arazi dokuları) ayıramadı:

| ölçü | kumtaşı (kusurlu) | arazi toprağı (iyi) |
|---|---|---|
| en güçlü frekans / ortalama | 96,4 | 38,2 |
| tek frekansın enerji payı | 0,449 | **0,536** |
| özilinti tepesi | 0,677 | **0,710** |

Son ikisi **ters** ayırıyor. İlki ayırıyor gibi duruyor ama ayırmıyor: bant
sınırlı üretilmiş her doku (bu mermer dahil) yüksek çıkıyor, çünkü ölçü
aslında "tayfta boş bant var mı" diye soruyor, periyodiklik değil.

Ağaç maliyeti ölçümünde konan kural burada da geçerli: **ölçemeyen alet sayı
üretmez, ölçemediğini söyler.** Bant oranı eşik değil, bilgi olarak yazılıyor.
Yerine geçen koruma piksel istatistiği değil **boru hattı** kuralıdır:
`OkmeydaniTests.StonesAreMarbleNotMasonry` taşın mermer rolünü kullandığını
sınıyor.

Buna karşılık, **ödünç alınan iki eşik ölçülerek yeniden konuldu.** Kaba enerji
sınırı arazi dokularından geliyordu (3,0) ve orada "havadan bakılan zemin
lekeli görünmesin" demekti; mermerin bütün meselesi ise metre ölçeğindeki
damardır. Gerçek taş dokuları ölçüldü — sıva 1,04 · moloz 2,99 · kumtaşı
4,79 · kaldırım 5,05 — ve sınır derzli her dokunun altına, **4,0**'a kondu.

## 9. Kitabe: üç kez yanlış, üçünü de farklı alet gösterdi

**(a) Pano gövdeden fazla taşıyordu** → sütuna dayanmış levha gibi. (ADR 0027)

**(b) Pano oturduğu yüzden GENİŞTİ** — 0,248 m'lik levha, sekizgenin 0,142
m'lik düz yüzüne sığmıyordu; kenarları gövdenin siluetinden taşıyor ve ince
bir dil gibi görünüyordu. Üstelik `make_tube` varsayılanında sekizgenin
**−Y'sinde bir köşe** durur, düz yüz değil. İkisi birden panoyu yüzeye teğet
bırakıyordu. Artık `phase = π/n` ile düz yüz öne getiriliyor ve panonun hem
genişliği hem derinliği o yükseklikteki **gerçek yarıçaptan** hesaplanıyor.

Bunu **sahne karesi göstermedi** — taşın iki tarafından da "kitabe yok" gibi
okunuyordu. Gösteren şey Blender inceleme paketi oldu (ADR 0006).

**(c) Kitabe Unity'de görünmüyordu** çünkü mermerle **aynı dokuyu**
kullanıyordu: dokulu bir malzemede taban rengi dokudan gelir, paletteki albedo
taşınmaz — iki malzeme Unity'de birebir aynıydı. Kitabenin doku rolü
kaldırıldı; düz renk, oyulmuş harflerin gölgesini taşıyan bir alan olarak
doğru okunur. Albedosu da ölçüldü: 0,262'de kitabe/gövde oranı **0,39** idi,
yani beyaz mermerde bir delik. 0,40'ta oran **0,50** — gölge, delik değil.

## 10. Kalan boşluklar

- Taşlarda **yazı yok**: kitabe bir alan, harf değil (mezar taşlarında da öyle).
- Menzil **koridorlarının gerçek yerleri** bilinmiyor; hava (yön) belgeli,
  ayak yerinin meydandaki konumu değil. Koridorlar araziye göre yerleştirildi.
- Meydan poligonu **2,74 km²**, 17. yüzyıl ölçümü ise ≈ **4,9 km²** — sınır
  muhtemelen dar (Karar 8).
- Meydan boş ve bu doğru; ama okçu, hedef, havacı gibi donatı yok.
- Faz 2b'de kalan altı madde: imaret, arasta, bozahane, değirmen, su terazisi,
  muvakkithane.
