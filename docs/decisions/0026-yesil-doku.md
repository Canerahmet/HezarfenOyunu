# ADR 0026 — Yeşil doku: ağaç belgeye dikilir, ağaçsızlık da belgelidir

**Tarih:** 2026-08-21
**Durum:** Kabul edildi — üretildi ve dikildi; Caner onayı bekliyor (sınırlar **taslak**)
**Tetikleyen:** ADR 0024 §10 — "bitki örtüsü yok, yeni en zayıf halka bu".
**İlgili:** ADR 0007 (DEM), 0008 (GeoJSON), 0024 (arazi örtüsü), 0025 (mevsim/güneş)

---

## 1. Neden gerekliydi

Arazi örtüsü bittiğinde manzara hâlâ çıplak görünüyordu ve sebep doku değildi:
zeminin **üstünde hiçbir şey yoktu**. RESEARCH.md §4 ise İstanbul'u yeşil
kütleleriyle anlatır — servi mezarlıkları, mesireler, bostanlar. Yeşil doku
süs değil, siluetin parçası.

## 2. Araştırma ne verdi, ne vermedi

Caner tekrar araştırma istedi; doğru istekti. Sonuç: kaynaklar bu alanların
**varlığını** ve çoğu zaman **nerede olduğunu** belgeliyor, **dönüm ölçüsünü
vermiyor**. Bostan literatürünün kendi ifadesi: *"precise acreage measurements
are largely absent from Ottoman sources"* — kayıtlar kira geliri ve **adet**
tutar, alan tutmaz.

Bulunanlar (ayrıntı ve tam alıntılar: RESEARCH.md §4.5):

| Alan | Ne bulundu | Kademe |
|---|---|---|
| **Karacaahmet** | TDV: I. Murad devrinden beri; **servileriyle meşhur**; en eski taş 1520 → 1632'de çoktan var. Bugünkü ~750 dönüm bizim için **üst sınır** | T2 |
| **Eyüp mezarlığı** | TDV: "Haliç'in kuzey yakasında, **Eyüp yamaçlarına yayılmış**"; Gümüşsuyu bir tepenin **iki yamacı**; 16. yy definleri Kâşgarî tekkesinden aşağı | T2 |
| **Pera bağları** | Galata'nın **üst surlarının ötesi** bağ, bahçe, mezarlık ve koru; yapılaşma **18. yy ortasından** sonra | T2 |
| **Kağıthane** | Evliya 17. yy'da ünlü mesire ve çayır olarak anlatır. **Sâdâbâd 1722'dir** — 1632'de kasır YOK | T2 |
| **Göksu** | Evliya (17. yy): kayıkla gezilen mesire. Göksu ile Küçüksu dereleri arası **500–600 m**; aradaki çayırın iki yanı yüksek ağaç ve bağ | T2 + tek metrik ipucu |
| **Langa bostanı** | Süleymaniye Vakfı **1583–1586** kayıtlarında kiralık bostan (29 290 akçe) — bizim yılımızdan **önceye** ait belge; konum Yenikapı kazılarıyla bilinir | T2 |
| **Yedikule bostanları** | "**Yedikule ile Topkapı arasında**"; 1719'da 77 vakıf ve özel bostan | T2 |
| **Okmeydanı — AĞAÇSIZ** | II. Bayezid vakfiyesi: meydanda *"yapı, mezar, su yolu, **bağ ve bahçe**"* yapılması kesin yasak | **T1 (yasak metni)** |

Dönem rakamları da elde: 1719'da 195 bostan, 1733'te sur içinde 126 + dışarıda
167, IV. Murad devrinde bostancıbaşının emrinde **"dört bin bostancı"** (Du
Loir — tam bizim yılımıza denk gelen bir tanık).

Bu yüzden CLAUDE.md kuralı uygulandı: **metrik geometri uydurulmadı.** Her alan
kaba bir kutu, `tier` T2, `status: draft`. Alanlar hesaplanıp raporlanıyor ki
"çizdik" ile "biliyoruz" karışmasın.

## 3. En güçlü kural bir YOKLUK

Okmeydanı vakfiyesi bahçe ve bağ dikilmesini **yasaklıyor**. Yani orası
bilinçle boş tutulmuş bir talim alanı — ve **Hezarfen'in talim yaptığı yer**.
Oraya bir ağaç düşerse bu görsel bir kusur değil, belgeye aykırılıktır; ve
45 bin ağacın içinde gözle bulunamaz.

Bu yüzden testi yazdım: `GreeneryTests.NoTreesOnOkmeydaniTrainingGround`
sıfır sayar, ve aynı test mezarlıkta ağaç **sayabildiğini** de kanıtlar —
"geçti" ile "doğru" aynı şey değil.

## 4. İki kaynak, iki farklı güven

**Adlı alanlar** yukarıdaki tabloya dayanır. **Genel yamaç** ise belgeye değil
**araziye** dayanır: ağaç yalnız suyun durduğu **içbükey** yerde ve otun baskın
olduğu yamaçta. İstanbul'un yakın çevresi 1632'de orman değil makilikti;
ormanlar kuzeydeydi. Bu yüzden genel serpme seyrektir (34 m ızgara) ve vadi
tabanlarını izler — arazi örtüsündeki **sırt kuralının tersi** (ADR 0024 §5).

Dikilen: 45 296 ağaç — 39 860'ı adlı alanlarda, 5 436'sı yamaçta.

## 5. Ölçülen kusurlar

**(a) Poligon kenarı düz çizgi olarak okundu.** Karacaahmet düzgün bir altıgen
gibi duruyordu. Poligon bir çizim aracıdır, doğada karşılığı yoktur; koru
kenardan seyrelir. Kenar 80 m'lik bir bantta gürültüyle tüylendirildi —
gürültüsüz bir gradyan bu sefer *yumuşak* bir altıgen verirdi.

**(b) Ağaçlar 400 m'den GÖRÜNMÜYORDU.** Sebep: Unity billboard'ı yalnız
SpeedTree ve Tree Creator varlıkları için üretir. Bizimkiler LOD Group'lu
normal prefablar; `treeBillboardDistance` ötesinde billboard'a geçmiyor,
**tamamen kayboluyorlar**. 160 m'de bırakılmıştı. Mesafe görüntüleme
mesafesine eşitlendi (3 000 m), ağaçlar LOD'lu mesh olarak sonuna kadar
çiziliyor.

## 6. Performans ÖLÇÜLEMEDİ — ve alet bunu söylüyor

Faz 1c'nin kabul ölçütü kare süresiydi. Editörde ölçmeye çalıştım; alet üç
farklı sonuç verdi ve ikisinde **ağaçlı kare ağaçsızdan hızlı** çıktı. Sıra
etkisini kaldırmak için ölçüm dönüşümlü hâle getirildi (aynı kamera, sırayla
açık/kapalı, ortancalar). Sonuç:

```
kusbakisi 400 m   agacli 11,4±15,6 ms | agacsiz 10,5±13,1 ms | OLCULEMEDI
mezarlik 500 m    agacli 13,5±12,5 ms | agacsiz 12,2±16,4 ms | OLCULEMEDI
```

Saçılma farkın on katı. Editör render'ı kararlı bir ölçüm ortamı değil
(asenkron shader derlemesi, arka plan varlık işi). **Alet artık bunu kendisi
söylüyor** — "0,67x" gibi inanmadığım bir sayı raporlamaktansa "ölçülemedi"
demek doğru.

Gerçek FPS yargısı bir **oyuncu yapısı** ister; batchmode build bu makinede
bloklu (SETUP.md). Kabul ölçütü bu yüzden **açık kalıyor**.

## 7. Yan bulgu: rasterio bu makinede çalışmıyor

`greenery_build.py` yazılırken çıktı:

```
ImportError: DLL load failed while importing _base:
An Application Control policy has blocked this file.
```

Bu **bütün** GIS araçlarını etkiler (coastline, walls, districts, landmarks
hepsi `rasterio.warp` kullanıyor) ve kodla ilgili değil — Windows uygulama
denetimi engeli.

Boru hattı rasterio'dan yalnız iki şey istiyordu: bir koordinat dönüşümü ve
bir raster okuma. İkisi de `tools/gis/geodesy.py`de bağımsız yazıldı. Dönüşüm
**uydurulmuş bir sayı olamaz**: `dem_meta.json` Galata Kulesi'nin hem
enlem/boylamını hem de daha önce rasterio ile hesaplanmış UTM karşılığını
tutuyor; `self_check()` ikisini karşılaştırıyor. Ölçülen sapma **0,05 mm**.

Yani modül, yerini aldığı kütüphaneye karşı doğrulanıyor. Öteki araçlar hâlâ
bloklu — `[İNSAN]` maddesi.

## 8. Kalan boşluklar

- **Asma ve sebze varlığı YOK.** Bağ sıraları ve bostan tarhı ayrı varlık
  ister; şimdilik bağ/bahçe seyrek çınarla temsil ediliyor ve bu bir
  **eksiktir, çözüm değil**. Bostanlarda hiç bitki yok — yalnız `Earth` örtüsü.
- Sınırlar **taslak**: hepsi kaba kutu, Caner onayı bekliyor.
- Göksu poligonu bir burnun üstüne oturuyor gibi görünüyor; iki dere arasındaki
  çayır daha dar olabilir.
- Ağaçların **mevsimi yok**: çınar dokusu ilkbahara göre ayarlanmadı (ADR 0025 §6).
- Mezarlıkta **mezar taşı yok** — servi var, hazîre yok. Mahalle ölçeğinde
  hazîre üretiliyor (ADR 0019) ama Karacaahmet ölçeğinde değil.
- Performans doğrulanmadı (§6).
