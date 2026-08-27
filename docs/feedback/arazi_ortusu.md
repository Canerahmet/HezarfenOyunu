# İnceleme — arazi örtüsü (Faz 1 geri dönüşü)

**Üretim:** 2026-08-21 · **ADR:** 0024 · **Kaynak:** RESEARCH.md §4.4

Arazi Faz 1'de doğru ölçekte gelmişti ama **hiç doku katmanı yoktu** — zemin
tek düz bir yüzeydi. Karanlıkta görünmüyordu; ADR 0023'te ışık gelince ortaya
çıktı ve en zayıf halka oldu.

## Bakılacak kareler

| Kare | Ne gösteriyor |
|---|---|
| `unity/HezarfenGame/Captures/faz1_arazi_yakin.png` | ayağın bastığı yer, göz hizası, 1,5–6 m |
| `unity/HezarfenGame/Captures/faz1_arazi_sokak.png` | sokaktan, ölçek okuması |
| `unity/HezarfenGame/Captures/faz1_arazi_yamac.png` | 60 m yukarıdan — kural doğru yere düşüyor mu |
| `unity/HezarfenGame/Captures/faz1_arazi_kusbakisi.png` | 400 m — **karo tekrarı** bu mesafede görünür |
| `unity/HezarfenGame/Captures/faz1_arazi_haritasi.png` | örtü haritası: hangi katman nerede |

Dördü de gerekli, çünkü arazi örtüsünün kusurları **mesafeye göre ayrışır**:
yakında ayrıntı yokluğu, sokakta ölçek yanlışlığı, yamaçta kuralın yanlış yere
düşmesi, havadan karo tekrarı. Tek kare hangisini seçerse öbür üçünü gizler.
Menü: **Hezarfen → GIS → Arazi ortusu inceleme paketi**.

## Dört katman

| Katman | Ne | Karo | Arazi payı |
|---|---|---|---|
| Toprak | işlenmiş/çiğnenmiş düzlük | 6 m | %15,9 |
| Ot | **taze ot + maki** — yamacın varsayılanı | 5 m | %46,8 |
| Kaya | dik yamaçta çıkan anakaya | 9 m | %1,3 |
| Kıyı | deniz seviyesi bandı ve deniz tabanı | 4 m | %36,0 |

Dördü prosedürel, **kendi telifimiz**; hiçbir görsel indirilmedi.

Dört olmasının sebebi zevk değil: splatmap RGBA taşır, **4 katman = 1 doku**.
Beşincisi belleği ikiye katlar.

## Ölçülen

| | ayrıntı enerjisi | ortalama |
|---|---|---|
| Dokusuz arazi (öncesi) | **0,45** | 110/255 |
| Dört katmanlı örtü | **3,75** | 103/255 |

Parlaklık neredeyse aynı, ayrıntı sekiz kat: değişen şey ışık değil yüzeyin
kendisi.

## Örtünün DAĞILIMI kuraldır, RENGİ yorumdur

Nereye ne düşeceği arazinin kendi verisinden çıkıyor: **kot** (deniz seviyesine
göre), **eğim**, ve **arazi eğriliği** (dışbükey sırt toprağını kaybeder,
içbükey çukur toprak ve nem tutar). Renkler ise belgeli değil — T3.

Bir şeyi bilerek yapmadım: **mahalle maskesi yok.** "Yerleşim yerinde çiğnenmiş
toprak" doğru bir kural ama uygulanacak veri yok. Semt poligonları
kullanılabilirdi; kullanılmadı, çünkü o dosya kendi içinde
*"bu bir OYUN bölgesidir, mahalle sınırı değildir"* yazıyor. Toprak bunun
yerine düz ve alçak yerde çıkıyor — insanların yerleştiği yer de zaten orası.

## Öğrendiğim şey: sabit açı yazılamaz

"Kaya 26°'nin üstünde başlar" dedim ve **kaya %0,0** çıktı. Sebep jeoloji değil
ölçek: 7,5 m örnek aralıklı bir DEM'de karanın %99'u 24°'nin altında kalıyor,
çünkü gerçek bir kaya yarı ortalamayla siliniyor. Doğru kural açı değil oran —
*"karanın en dik ~%5'i çıplak anakayadır"* — ve açı bundan türetiliyor.

## Karar 7 — mevsim: **İLKBAHAR** ✅ (Caner, 2026-08-21)

Palet ilkbahara çevrildi: taze ot + maki, nemli ve az çatlaklı toprak, kaya
çatlağında yosun, ıslak kıyı çakılı. `DryGrass` katmanı `Grass` oldu.

Karar zevkten öte bir sebeple doğru çıktı: oyunun birinci tasarım direği
**lodos**tur ve lodos yılın soğuk yarısının rüzgârıdır; yaz sonuna hâkim olan
poyraz kuzeydoğudandır. "Yaz sonu" paleti, uçuşu taşıyan rüzgâr sistemiyle
çelişiyordu. Ayrıntı: **ADR 0025**.

### Yanında çıkan kusur: güneş imkânsız bir yerdeydi

Mevsimi uygularken görüldü: ışık 205°'ye doğru yol alıyordu, yani güneş 25°
azimutta — kuzeykuzeydoğuda. 41° kuzeyde güneş oraya hiç gelmez. Güneş artık
tarih ve saatten **hesaplanıyor** (1 Mayıs, 15:00 → yükseklik 43,2°, azimut
249,6°) ve bir test bunu kilitliyor.

Saat öğleden sonra, çünkü uçuş **doğuya**: sabah güneşi bütün uçuş boyunca
oyuncunun gözüne gelirdi. Ölçüldü — öğleden sonra her kadrajda daha okunur:

| | gölgedeki cephe | sokak zemini | kuşbakışı |
|---|---|---|---|
| eski (imkânsız güneş) | 2,29 | 1,97 | 0,78 |
| 1 Mayıs 09:00 | 1,92 | 2,45 | 0,77 |
| **1 Mayıs 15:00** | **3,84** | **2,88** | **1,24** |

## Bildiğim eksikler

- **Bitki örtüsü yok** ve manzaranın çıplak görünmesinin asıl sebebi bu:
  arazi artık dokulu ama üstünde ne servi kütlesi, ne bahçe, ne bostan var.
  **Yeni en zayıf halka.**
- Kıyı çizgisi havadan **basamaklı** okunuyor (splat texel'i 7,5 m).
- Karo tekrarının asıl çözümü doku bombalama — gölgelendirici işi.
- Kaldırım yalnız ana sokakta.
- Örtü yapılara tepki vermiyor: evin dibi de 200 m ötesi de aynı kural.

## Onay

```
OK v1        (ya da: düzeltme istekleri)
```
