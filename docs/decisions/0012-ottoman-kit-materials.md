# ADR 0012 — Osmanlı konut kiti ve gerçekçi malzeme hattı

**Tarih:** 2026-08-20
**Durum:** Kabul edildi — çıktı **TASLAK**, Caner onayı bekliyor
**Talep:** Caner, 2026-08-20: *"gerçekçi bir model oluşturmanı istiyorum. doku vs gerçekçi olsun."*
**İlgili:** plan Faz 2; ADR 0005 (varlık hattı), ADR 0006 (inceleme paketi)

---

## 1. Evi nereden üretiyoruz

**Hiçbir yerden indirilmiyor.** Geometri tamamen kod üretimidir:

```
tools/blender/lib/ottoman_kit.py   parametrik üretici (bmesh; kutu, kırma çatı, üçgen payanda)
tools/blender/gen_ottoman_house.py sürücü (arayüzsüz Blender)
        ↓
art/blend/SM_House_A.blend         kanonik kaynak
        ↓ export_fbx.py
Assets/_Import/                    Unity iniş alanı
```

Biçimi belirleyen şey RESEARCH.md'nin **T2** kurallarıdır: taş subasman, ahşap
karkas cumbalı üst kat, kafes pencere, geniş saçak, alaturka kiremit, aşı boyası.
Ev-ev tarihsel kayıt yoktur, dolayısıyla varlığın tamamı T2'dir.

Planda hazır CC0 geometri yalnızca **karakter** için öngörülüdür (Blender Studio
Human Base Meshes); konut için kullanılmadı.

## 2. Plan Faz 2 parametre listesi karşılandı

| Parametre | Nasıl |
|---|---|
| Kat sayısı (1–3) | `--floors` |
| Cephe genişliği / derinlik | `--width --depth` |
| **Cumba tipi ve derinliği** | `--cumba-type none\|flat\|corbel\|corner`, `--cumba` |
| **Kafes pencere yoğunluğu** | `--window-detail none\|recess\|kafes`, `--window-density` |
| Saçak derinliği | `--eave` |
| **Çatı eğimi** | `--roof-pitch` (yükseklik EĞİMDEN türetilir) |
| Taş subasman | `--plinth` |
| Renk paleti + gayrimüslim varyantı | `--palette default\|nonmuslim` |

Çatı **eğimle** tanımlanır, yükseklikle değil: alaturka kiremit ancak belirli bir
eğim aralığında tutar; sabit yükseklik, ev genişledikçe eğimi sessizce düşürürdü.

`palette=nonmuslim` yalnızca renk değiştirmez — RESEARCH.md o evleri "daha koyu
**ve alçak**" diye anar, bu yüzden kat yüksekliği ve çıkma da kısılır. Kuralın
yarısını renge indirgemek onu sessizce düşürmek olurdu.

> **Güncelleme (2026-08-20):** §3'ün "açıklık = cepheye yapıştırılmış panel"
> çözümü ve §4.2'de anlatılan `facade_y()` yardımcısı **yalnızca `--detail mass`
> kipi için geçerlidir**. Caner yakın plana geçilmesini istedi; yaya seviyesi
> yapımı gerçek delikli duvar kullanır ve işaret güvenliği artık `_wall_axes`
> içinde yaşar. Bkz. [ADR 0013](0013-near-detail-construction.md).

## 3. Pencere neden çoğunlukla geometri değil

Plan doku stratejisini "2–3 trim sheet + 1 atlas" diye kilitliyor. 8 000 ev
ölçeğinde her pencereyi modellemek üçgen bütçesini yer. Pencere **kademelidir**:
`none` / `recess` (varsayılan) / `kafes`. Aynı jeneratör hem kalabalık dokuyu
hem yakın plan evini üretir.

Gerçek niş **boolean** ister; o ölçekte boolean hem yavaş hem kırılgandır
(dejenere yüz üretir). Bunun yerine koyu panel cepheden 2 cm, söve 5 cm taşar;
göz bunu girinti olarak okur ve doku geldiğinde tam oturacağı yer orasıdır.

## 4. Ölçüm neyi yakaladı

### 4.1 Kapı, pencerelerin üstüne biniyordu

İlk üretimde kapı bağımsız olarak cephe ortasına konuyordu. Ölçüldü: **4
pencereden 2'si kapıyla çakışıyordu** (kapı ±0,61 m, pencereler ±0,29 m'den
başlıyor). Düzeltme mimari: cephe **tek sayıda** bölmeye ayrılır ve ortası kapıya
ayrılır. Böylece çakışma yapısal olarak imkânsız hâle gelir — kapı, Osmanlı
konutunda cephenin bir aksıdır, pencereler arasına sıkıştırılmış bir boşluk değil.

### 4.2 Cephe parçaları duvarın İÇİNE düşüyordu

Sokak cephesi −Y olduğu için `outward = −1`. İşareti elle yazdığım üç yerde ters
kurmuşum: pencere paneli, söve ve kafes çıtaları duvarın içine gömülüyordu — yani
**hiç görünmüyorlardı**. Hata sessizdi. Düzeltme: tek bir `facade_y()` yardımcısı;
işaret artık bir yerde yaşıyor.

### 4.3 Aşı boyası çok koyuydu — ve sebebi model hatasıydı

Render'dan ölçüldü: boyalı ahşabın parlaklığı **40/255**, hedef ~100. İlk refleks
gamma ile aydınlatmaktı (40 → 63) ama hedefe zorlamak için gereken gamma (~0,19)
ahşabın damarını eziyordu.

Asıl sebep karışım kipiydi. `COLOR` karışımı **parlaklığı taban dokudan alır**;
koyu bir ahşap dokusuna açık bir boya sürülse bile sonuç koyu kalır. Oysa boya,
altındaki tahtanın koyuluğunu taşımaz — **örter**. Karışım `MIX`e çevrildi:
albedo büyük ölçüde boyanın kendisi, doku yıpranma ve damar katkısı verir.

Aşı boyası rengi de göz kararı seçilmedi: sRGB (200,105,80) karşılığı doğrusal
değer, render'da ölçülen hedefe (parlaklık ~100, R/G ~1,9) göre ayarlandı.

### 4.4 Çatı kâğıt gibi inceydi

Kalınlıksız bir çatı saçakta kâğıt gibi biter ve göz bunu hemen yakalar. **12
üçgenlik** bir alınlık tahtası (fascia) silueti "oyun kiti" olmaktan çıkaran tek
en büyük parçaydı. LOD1'de de korunur — silueti belirleyen kenar odur.

## 5. Doku: Poly Haven (CC0), dünya ölçekli UV

`tools/textures/fetch_polyhaven.py` indirmeyi **tekrarlanabilir** kılar ve iki
şeyi kaydeder:

* **Gerçek dünya ölçüsü.** Poly Haven her dokunun kaç metre kapladığını verir.
  UV doğrudan metreden hesaplanır (`u = mesafe / doku_boyu`); hiçbir yerde katsayı
  elle ayarlanmaz. Bir dokunun "ucuz" görünmesinin bir numaralı sebebi yanlış
  texel yoğunluğudur ve buna bakan kişi "bir tuhaf" der, sebebini söyleyemez.
* **Atıf.** CC0 hukuken atıf istemez ama plan krediler ekranına yazılmasını
  istiyor; üreticilerin adı ancak indirme anında kaydedilirse elde kalır.
  `refs/LICENSES.md` otomatik güncellenir.

Seçimler **isimden değil bakılarak** yapıldı: adaylar tek tabakaya dizilip
karşılaştırıldı. `plastered_stone_wall` neredeyse siyahtı, `rock_wall_02` duvar
değil doğal kayaydı — ikisi de elendi.

| Rol | Poly Haven | Ölçü |
|---|---|---|
| Kireç badana | `painted_plaster_wall` | 2,00 m |
| Gayrimüslim varyantı | `grey_plaster` | 1,00 m |
| Taş subasman | `old_stone_wall` | 2,00 m |
| Ahşap (aşı boyalı) | `weathered_planks` | 2,00 m |
| Alaturka kiremit | `clay_roof_tiles_02` | 2,50 m |
| Yaşlanmış çatı | `ceramic_roof_01` | 3,50 m |

**UV: yüzeye hizalı, dominant eksen değil.** Basit kutu izdüşümü eğik yüzeylerde
dokuyu kısaltır (30° çatıda ~%13). Her yüzün kendi düzleminde ortonormal taban
kurulur; eğim ne olursa olsun texel yoğunluğu sabit kalır. Bedeli farklı yönlü
yüzler arasında dikiştir — mimari sert yüzeylerde normaldir.

**Normal haritası `nor_gl`** (OpenGL, Y+): Blender ve Unity aynı yönü bekler.
`nor_dx` alınsaydı girintiler çıkıntı olurdu — sessiz ve çok yaygın bir hata.

## 6. İnceleme aydınlatması: iki kip, biri diğerini bozmaz

ADR 0006'nın stüdyo aydınlatması **oranları** yargılamak içindir ve bilerek
yansız/düz tutulur. Ama PBR bir malzeme gerçek ortam ışığı olmadan
değerlendirilemez: düz gri bir dünyada her şey plastikleşir, kusur da erdem de
görünmez olur.

Bu yüzden `--hdri` **ayrı bir kip** olarak eklendi; nötr kip değişmedi.

* HDRI: `kloofendal_48d_partly_cloudy_puresky` (CC0). `puresky` serisi bilerek
  seçildi — yalnızca gökyüzü içerir, çevredeki ağaç/binadan gelen yabancı renk
  yansımaları malzemeyi yanlış gösterirdi.
* HDRI kipinde **ek ışık eklenmez**. Yapay dolgu, HDRI'nin doğru okunan
  gölge/yansıma dengesini bozar; gerçekçilik tam da oradan gelir.

## 7. Ölçülen durum (House_A, kafes + corbel)

| | Değer |
|---|---|
| Ayak izi × yükseklik | 8,90 × 8,70 × 8,51 m |
| Üçgen LOD0 / LOD1 / LOD2 | **908 / 56 / 20** |
| Pivot | taban merkez (otomatik denetim) |
| Çatı eğimi | 30° → 2,51 m |

## 8. Bu ADR'nin SÖYLEMEDİĞİ

- **Unity tarafı henüz yok.** Blender malzemeleri kuruldu; HDRP `Lit` malzemeleri
  ve `_BC/_N/_ORM` maske paketlemesi yapılmadı. HDRP maske düzeni ARM'dan
  farklıdır, yeniden paketleme gerekir.
- **Trim sheet/atlas yok.** Şu an her rol kendi 2K dokusunu kullanıyor. Plan
  "2–3 trim sheet + 1 atlas" diyor; 8 000 ev ölçeğinde çizim çağrısı ve bellek
  için bu **zorunlu** ve yapılmadı.
- **Yakın plan geometrisi eksik:** saçak altı mertekleri, pencere denizliği,
  kapı çerçevesi derinliği yok. Uzaktan sorun değil, yakında okunur.
- **Yan ve arka cepheler penceresiz.** Sıkışık mahalle dokusunda bitişik nizam
  makuldür (T2) ama serbest duran evde eksik görünür — Caner'e sorulacak.
- **20 parametre kombinasyonu ve "Galata sokağı" test sahnesi yapılmadı**;
  Faz 2 kabulü onu istiyor.

## Yeniden üretim

```powershell
python tools\textures\fetch_polyhaven.py --res 2k --hdris
& $blender --background --factory-startup --python tools\blender\gen_ottoman_house.py -- `
    --asset House_A --textured --window-detail kafes --cumba-type corbel `
    --out-blend art\blend\SM_House_A.blend
& $blender --background --factory-startup --python tools\blender\render_preview.py -- `
    --in art\blend\SM_House_A.blend --asset House_A_HDRI --hdri --samples 96
```
