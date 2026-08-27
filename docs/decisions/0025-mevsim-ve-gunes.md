# ADR 0025 — Mevsim ilkbahar; güneş hesaptan gelir

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — Caner kararı ("ilkbahar daha iyi olabilir", 2026-08-21)
**Tetikleyen:** ADR 0024 §9, Karar 7 (mevsim); ve o kararı uygularken bulunan güneş kusuru.
**İlgili:** ADR 0007 (dünya orijini), 0023 (geçici aydınlatma), 0024 (arazi örtüsü)

---

## 1. Mevsim: ilkbahar

Uçuşun günü kaynaklarda **yok** (RESEARCH.md §4.4(f)). Palet önce "yaz sonu
kuru" kurulmuştu; Caner ilkbaharı sordu ve haklı çıktı — üstelik zevkten öte
bir sebeple.

**Oyunun birinci tasarım direği lodostur** (PLAN.md §0: *"Lodos bir hava durumu
değil, bir oynanış sistemidir"*); `WindField` v0 global lodosu 9 m/s taşır.
Lodos güneybatıdan eser ve İstanbul'da **yılın soğuk yarısının** rüzgârıdır;
yaz sonuna hâkim olan poyraz kuzeydoğudandır. Yani "yaz sonu" paleti, uçuşu
taşıyan rüzgâr sistemiyle çelişiyordu. İlkbahar ikisini uzlaştırıyor.

İkinci sebep RESEARCH.md §4'ün kendisi: kaynaklar İstanbul'u **yeşil
kütleleriyle** anlatır (servi mezarlıkları, mesireler, bostanlar). Baştan sona
sarı bir İstanbul, kaynakların anlattığı şehir değil.

Palet buna göre yeniden kuruldu: taze ot + maki, nemli ve az çatlaklı toprak,
kaya çatlağında yosun, ıslak kıyı çakılı. `DryGrass` katmanı `Grass` oldu.

## 2. Güneş imkânsız bir yerdeydi

Mevsimi uygularken şu ortaya çıktı: sahnedeki güneşin ışığı **205°'ye doğru**
yol alıyordu, yani güneş **25° azimutta** — kuzeykuzeydoğuda. 41° kuzey
enleminde güneş oraya **hiçbir gün, hiçbir saat gelmez**.

Kusur neden görülmedi: yükseklik (42°) makuldü, gölgeler bir yöne düşüyordu ve
kare makul görünüyordu. Gözle yakalanabilecek bir şey değildi — tam da projenin
kaçınmaya çalıştığı hata türü: **gözle doğrulanmış, ölçülmemiş bir sayı.**

Sınır hesaplanabilir: güneşin en kuzey azimutu, en büyük deklinasyonda gün
doğumunda olur — `cos(A) = sin(23,45°)/cos(φ)`. 41,03° için **58,2°**. Yani
güneş 58,2°–301,8° dışına çıkamaz; 25° bu aralığın dışındaydı.

## 3. Güneş artık bir hesabın çıktısı

`SunPlacement`: enlem/boylam + gün + güneş saati → yükseklik ve azimut. Elle
döndürülmüş bir açı yok. Yanlış bir güneş kurmak artık yanlış bir **tarih**
yazmayı gerektirir, ki o göze çarpar.

`LightingTests.SunIsAstronomicallyPossibleForIstanbul` bir açıyı değil bir
**gerekliliği** kilitliyor: mevsim ya da saat değişebilir, güneşin gökyüzünde
bulunabileceği yer değişmez. (Ayırt etme gücü sınandı: eski 25°'lik güneş bu
testten geçemezdi.)

## 4. Saat: 15:00, çünkü uçuş DOĞUYA

1 Mayıs, güneş saati 15:00 → yükseklik **43,2°**, azimut **249,6°**
(batı-güneybatı).

Aynı yüksekliği sabah 09:00 da verir (azimut 110°) ve ilk denemem oydu. Yanlış
taraf: Hezarfen Galata'dan Üsküdar'a, yani **doğuya** uçar. Sabah güneşi bütün
uçuş boyunca gözüne gelir ve önündeki şehir kontr-ışıkta silüete iner. Öğleden
sonra güneş arkada kalır; hedef kıyı, Kız Kulesi ve iniş alanı önde ve
aydınlıktır. Lodosla da uyumlu: rüzgâr güneybatıdan, güneş batı-güneybatıdan —
ikisi de arkadan.

Yükseklik eskisinin neredeyse aynısı (42° → 43,2°), bu yüzden ADR 0023'ün poz
kalibrasyonu (13,0 EV) **korundu**; değişen yalnızca pusula yönü.

### Ölçülen — üç güneşin karşılaştırması

| | gölgedeki cephe | sokak zemini | kuşbakışı |
|---|---|---|---|
| eski (imkânsız, azimut 25°) | 2,29 | 1,97 | 0,78 |
| 1 Mayıs 09:00 (azimut 110°) | 1,92 | 2,45 | 0,77 |
| **1 Mayıs 15:00 (azimut 250°)** | **3,84** | **2,88** | **1,24** |

Öğleden sonra her ölçüde önde. Sebebi tahmin değil geometri: batıdan gelen ışık
dar sokağın iki yakasını da yalar, doğudan gelen ışık ise sabah saatinde bir
yakayı tümüyle gölgede bırakır.

## 5. Yan etki: dolgu ışıkları kendiliğinden döndü

Geçici aydınlatma takımının dolgu yönleri güneşten türetiliyordu (ADR 0023 §3),
elle yazılmamıştı. Güneş dönünce dolgular da döndü; takımı yeniden kurmak
yetti. İki yerde tutulan bir açı bir gün ayrışır — burada ayrışmadı.

## 6. Kalan boşluklar

- Mevsim tek: kar/yağmur/sonbahar yok ve v1.0 için planlanmıyor.
- Saat sabit. Gün döngüsü Faz 6'nın "yaşayan İstanbul" işi; `SunPlacement`
  zaten saatten hesapladığı için altyapısı hazır.
- Ağaçların ve bitkilerin mevsimi yok: `PF_Servi` ve `PF_Cinar` dokuları
  ilkbahara göre ayarlanmadı (servi her dem yeşil, sorun değil; **çınar**
  ilkbaharda daha açık yeşil olurdu).
- Zamanla ilgili tek tarihsel işaret hâlâ kahvehane (1633 fermanı); mevsim
  bir tarih işareti taşımıyor.
