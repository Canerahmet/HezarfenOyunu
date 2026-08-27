# İnceleme — yeşil doku (Faz 1c)

**Üretim:** 2026-08-21 · **sınırlar toptan ele alındı** 2026-08-23
**ADR:** 0026 + **0029** · **Kaynak:** RESEARCH.md §4.5

> **2026-08-23 — Karar 8 büyük ölçüde kapandı.** "Toptan yapalım" dedin ve
> on bir sınır birden ele alındı. Sonuç aşağıda; **Karar 9 hâlâ açık.**

## Toptan turun bulduğu şey

Aramada **iki sert çapa** çıktı ve ikincisi beni şaşırttı:

| Alan | Taslağım | Belgeli | Fark |
|---|---|---|---|
| Okmeydanı | 274 ha | **490 ha** | yarı yarıya küçük |
| **Galata surları içi** | **216 ha** | **37 ha** | **altı kat büyük** |

Yani bir alanda yaptığım hata bir başkasında **ters yönde ve altı kat**
büyüktü. Tek tek düzeltmenin neden yetmeyeceğinin kanıtı bu satır.

**Sınırlar artık çizilmiyor, türetiliyor.** Her alan dayanağını taşıyor:

- **Sur içi** = kara + Marmara + Haliç surlarının kapattığı halka. Ayrı bir
  kutu yok; iki geometri aynı kaynaktan geliyor, ayrışamazlar. (1334 ha —
  bugünkü Fatih 1562 ha; fark 20. yy kıyı dolgusu, yani beklenen fark.)
- **Galata** = surun kendisi, belgeli 37 ha'ya oturtuldu. `walls_build` de.
- **Yedikule bostanları** = "Yedikule ile Topkapı arasında" cümlesi artık sur
  verisindeki iki **kapıdan** okunuyor; şerit sur çizgisinden türetiliyor.
- **Kağıthane** = kutu değil, **DEM'den izlenen vadi tabanı**. Elle çizilmiş
  kutunun ortalama kotu 46 m çıkmıştı — yani çayırda değil yamaçtaydı.
- **Üsküdar** = tek "çapası yok" alan, ve öyle yazıyor.

Arazi iddiaları da ölçülüyor artık: "bir tepenin iki yamacı" → kot farkı,
"dere boyu çayır" → ortalama kot, "iki dere arası 500–600 m" → dar kenar,
"dolmuş liman" → alçak **ve sur içinde**. İkisi ilk koşuda düştü ve düzeltildi
(Kağıthane yamaçtaydı, **Langa'nın iki köşesi denizdeydi**).

## Bakılacak kareler

| Kare | Ne |
|---|---|
| `unity/HezarfenGame/Captures/yesil_karacaahmet.png` | Karacaahmet servi ormanı |
| `unity/HezarfenGame/Captures/yesil_eyupmezarligi.png` | Eyüp mezarlığı, yamaçta |
| `unity/HezarfenGame/Captures/yesil_perabaglari.png` | Pera bağları — Galata'nın hemen kuzeyi |
| `unity/HezarfenGame/Captures/yesil_kagithane.png` | Kağıthane mesiresi |
| `unity/HezarfenGame/Captures/yesil_goksu.png` | Göksu mesiresi |

Menü: **Hezarfen → GIS → Yesil doku inceleme paketi**

## Araştırma ne verdi

Tekrar araştırma istemiştin; iyi ki istemişsin — **çıktı**. Ama beklenen şey
çıkmadı: kaynaklar bu alanların **varlığını ve yerini** belgeliyor, **dönüm
ölçüsünü vermiyor**. Bostan literatürünün kendi ifadesi bu; Osmanlı kayıtları
kira geliri ve **adet** tutuyor, alan tutmuyor.

Eldeki dönem rakamları buna iyi bir örnek: 1719'da 195 bostan, 1733'te sur
içinde 126 + dışarıda 167, ve **IV. Murad devrinde** bostancıbaşının emrinde
**"dört bin bostancı"** (Du Loir) — tam bizim yılımıza denk gelen bir tanık.
Sayı var, sınır yok.

Bulunan **yer** bilgileri:

| Alan | Ne bulundu |
|---|---|
| Karacaahmet | I. Murad'dan beri; **servileriyle meşhur**; en eski taş 1520 → 1632'de var |
| Eyüp mezarlığı | "Haliç'in kuzey yakasında, **Eyüp yamaçlarına yayılmış**"; 16. yy definleri var |
| Pera bağları | Galata'nın **üst surlarının ötesi**; yapılaşma **18. yy ortasından** sonra |
| Kağıthane | Evliya 17. yy: ünlü mesire. **Sâdâbâd 1722'dir** — 1632'de kasır YOK |
| Göksu | Göksu ile Küçüksu dereleri arası **500–600 m**; bulabildiğim **tek ölçü** |
| Langa | Süleymaniye Vakfı **1583–86** kira kaydı → bizim yılımızdan öncesi belgeli |
| Yedikule | "**Yedikule ile Topkapı arasında**"; 1719'da 77 bostan |

## En sağlam kural bir YOKLUK

**Okmeydanı'na bağ ve bahçe dikilmesi yasak.** II. Bayezid'in vakfiyesi
meydana *"bir karış tecavüz edilmemesi, yapı, mezar, su yolu, bağ ve bahçe
yapılmaması"*nı kesin olarak yasaklıyor. Orası bilinçle boş tutulmuş bir
talim alanı — ve **Hezarfen'in talim yaptığı yer**.

Oraya bir ağaç düşerse bu bir görsel kusur değil, belgeye aykırılık olur; ve
45 bin ağacın içinde gözle bulunamaz. O yüzden testle kilitledim.

## Ne dikildi

**42 857 ağaç** — 37 580'i adlı alanlarda, 5 277'si genel yamaçta.
(Sınırlar düzeltilmeden önce 45 296'ydı; Galata'nın küçülmesi ve Kağıthane'nin
vadi tabanına çekilmesi sayıyı aşağı çekti.)

Genel yamaç kuralı belgeden değil **araziden**: ağaç yalnız suyun durduğu
**içbükey** yerde ve otun baskın olduğu yamaçta. İstanbul'un yakın çevresi
1632'de orman değil makilikti; ormanlar kuzeydeydi. O yüzden serpme seyrek.

## Sana iki soru

**Karar 8 — sınırlar (güncellendi).** Beş alanın büyüklüğü artık kaynağa ya da
geometriye bağlı. Ölçüsü hâlâ tahmin olanlar: **Eyüp, Pera bağları, Göksu,
Langa, Yedikule şeridinin eni, Üsküdar**. Bunlar için arama sonuç vermedi —
Osmanlı kayıtları bostan/mesire alanı tutmuyor. Kareye bakıp "şu fazla büyük"
dersen düzeltirim; özellikle **Göksu** hâlâ bana bir burnun üstünde duruyor
gibi geliyor, ama dar kenarı ölçtüm ve kaynağın verdiği 500–600 m bandında.

**Karar 9 — bağ ve bostan.** Elimizde **asma ve sebze varlığı yok**. Şu an
bağlar seyrek çınarla temsil ediliyor ve bostanlarda hiç bitki yok, yalnız
toprak örtüsü. İki seçenek:

- **A:** şimdilik böyle kalsın, Faz 4'te (şehri doldurma) asma sırası ve
  bostan tarhı üretilsin.
- **B:** şimdi üreteyim — asma sırası ve sebze tarhı basit geometridir,
  yarım gün.

**Önerim A** — çünkü bunlar yakından bakılan şeyler ve yakın plan Faz 4'ün işi.

## Bildiğim eksikler

- **Performans doğrulanmadı.** Kabul ölçütü kare süresiydi; editörde
  ölçülemedi (saçılma farkın on katı) ve gerçek yargı bir oyuncu yapısı
  istiyor — o da bu makinede bloklu. Ölçü aleti artık "ölçülemedi" diyor,
  uydurma sayı üretmiyor.
- ~~rasterio bütün GIS araçlarını bloklamıştı~~ → **çözüldü.** Ters UTM
  dönüşümü yazıldı (kapanma 0,29 mm) ve beş araç birden kurtarıldı:
  `walls_build`, `coastline_build`, `districts_build`, `landmarks_build`,
  `dem_probe`. Bir yıldır koşulamayan georeferans denetimi koştu ve DEM'i
  yedi noktada doğruladı. `[İNSAN]` maddesi artık yalnız `dem_fetch`
  (COG indirme) ve `map_overlay` için geçerli.
- **Kağıthane vadisinde bir su birikintisi var** ve bu bir ARAZİ kusuru:
  28,95632 D / 41,06725 K'de ~60 × 80 m'lik bir yama DEM'in taban kotunda
  (−12 m) duruyor — deniz doldurması dere ağzından yukarı kaçmış. Mesire
  artık etrafına dizilmiyor ama havuz duruyor; sahibi ADR 0007 ve o araç
  şimdi tekrar çalışıyor, yani düzeltilebilir. Kareye bakarsan görürsün.
- Karacaahmet'te servi var, **mezar taşı yok** — o ölçekte hazîre üretilmedi.
- Karacaahmet bugünkü alanına oturtuldu; bu **üst sınırdır**, 1632 ölçüsü
  değil. Oradaki servi sayısı bir tavan.
- Çınar dokusu ilkbahara göre ayarlanmadı.

## Onay

```
OK v2        (ya da: düzeltme istekleri)
Karar 8: sınırlar OK / şu alan düzeltilsin: ...
Karar 9: A / B
```
