# ADR 0009 — Deniz Yüzeyi, Deniz Tabanı ve HDRP Aydınlatma Temeli

**Tarih:** 2026-08-19
**Durum:** Kabul edildi
**Karar veren:** Claude (Caner projeyi tamamen devretti)
**İlgili:** plan Faz 1 madde 5; ADR 0007 (DEM), ADR 0008 (kıyı)

## Bağlam

Faz 1'in kabul kriteri: *"Gerçek topoğrafyada Galata sırtından kalkıp Üsküdar'a
inilebiliyor."* Arazi (ADR 0007) ve kıyı (ADR 0008) hazırdı; eksik olan **su** ve
**görülebilir bir sahne**ydi. İlk denemede kare simsiyah çıktı ve peş peşe dört ayrı
kusur ortaya döküldü. Bu ADR onları ve alınan kararları kaydeder.

## Karar 1 — Deniz tabanı, deniz seviyesinin ALTINDADIR

`dem_fetch.py` deniz hücrelerini artık 0'a değil **−12 m**'ye indirir.

**Neden:** su düzlemi `y = 0`'dadır. Deniz tabanı da 0 olsaydı iki yüzey aynı kotta
çakışırdı — su ya hiç görünmez ya z-fighting yapardı, ayrıca denizin *derinliği*
olmazdı. İlk Faz 1 karesinde Boğaz'ın yerinde kara rengi düz bir zemin vardı; kusur
tam olarak buydu.

Sözleşme değişmedi: **y = 0 hâlâ deniz seviyesidir.** Değişen, Terrain nesnesinin
yerleştirildiği kot:

| Değer | Önce | Sonra |
|---|---|---|
| `base_elevation_m` (Terrain nesnesinin y'si) | 0 | **−12** |
| `sea_level_m` (yeni) | — | 0 |
| `seabed_depth_m` (yeni) | — | 12 |
| Terrain yükseklik aralığı | 291 m | 304 m |

Kara kotları **kaymadı** — ölçümle doğrulandı: Galata 52,0 m, Çamlıca 252,8 m,
Doğancılar 15,3 m (ADR 0007'deki değerlerin aynısı). `Terrain_LandStaysAboveSeaLevel`
testi bunu kilitler.

12 m bir **oyun değeridir, ölçüm değil.** Copernicus DEM batimetri taşımaz; gerçek
Boğaz derinliği 30–100 m'dir. Gerekirse Faz 3'te batimetri eklenir.

## Karar 2 — Su yüzeyi tek ve sonsuzdur

`WaterSurface`, `OceanSeaLake` + **`Infinite`** geometri, `y = 0`.

Boğaz, Haliç ve Marmara tek su kütlesidir ve dünya 15 km'dir; ayrı quad'lar kıyı
kesişimlerinde dikiş bırakırdı. Dalga yönü lodosla hizalıdır
(`WindTuning.globalWind = (9,0,0)` → HDRP `largeOrientationValue = 90°`), böylece su
ile uçuş **aynı havayı** anlatır.

## Yaşanan dört kusur

Hepsi yalnızca **karelere bakıldığı** için bulundu; hiçbiri hata/uyarı üretmedi.

### 1. Işık şiddeti built-in ölçeğindeydi

Sahne varsayılan güneşi `intensity = 3.2` idi. **HDRP yönlü ışıkları Lux ile ölçer**;
3,2 lux alacakaranlıktan koyudur. Gökyüzü (prosedürel) render oluyordu ama tüm geometri
**simsiyahtı**. Düzeltme: `HDAdditionalLightData.SetIntensity(100000f, LightUnit.Lux)`.

Bu, "sahne bozuk" gibi görünüp aslında tek bir birim hatası olan türden bir kusurdur.

### 2. `skyAmbientMode = Static`, fırınlanmış ışık yok

Paylaşılan gökyüzü profili statik ortam ışığı istiyordu; fırınlanmamış sahnede ortam
**siyah** kalıyor ve volümetrik sis her şeyi yutuyordu. Ölçüm:

| Deney | Ortalama parlaklık |
|---|---|
| Hacimsiz | 0,072 |
| Mevcut hacim (Static + oto pozlama) | **0,000** |
| Dynamic ambient + sabit EV13 | **0,259** |

Düzeltme: Faz 1'e **özel** profil (`VP_Faz1_Sky`) — paylaşılan profile dokunulmadı,
çünkü `FlightSlice`'ta Caner'in uçuş kapısı hâlâ açık ve o sahne bozulmamalı.

### 3. Otomatik pozlama tek karede yakınsamaz

`AutomaticHistogram`, edit-mode'daki tek `cam.Render()` çağrısında adapte olamaz.
İnceleme kareleri için **sabit pozlama** kullanılır; oyunda otomatik kalır.

### 4. Suyu YANLIŞ HDRP asset'inde açtım

En sinsi olanı. Projede **dört** HDRP asset'i var:

```
Assets/Settings/HDRPDefaultResources/HDRenderPipelineAsset.asset   <- burada actim
Assets/Settings/HDRP Balanced.asset
Assets/Settings/HDRP High Fidelity.asset      <- GERCEKTE KULLANILAN
Assets/Settings/HDRP Performant.asset
```

`QualitySettings.renderPipeline`, `GraphicsSettings.defaultRenderPipeline`'ı **ezer**.
`supportWater`'ı varsayılan asset'te açmak hiçbir şey yapmadı: su, Play modunda 3350
kare boyunca bile render edilmedi ve **tek bir uyarı bile üretmedi**.

Teşhis, çalışan pipeline'ı `QualitySettings.renderPipeline` üzerinden okuyunca çıktı.
Düzeltme: **tüm** HDRP asset'lerinde açılır. Mavilik ölçüsü `−0,038 → +0,080` sıçradı.

> **Ders:** Unity'de "ayarı açtım" demek, ayarı *kullanılan* asset'te açtığın anlamına
> gelmez. Bir görsel özellik sessizce çalışmıyorsa, önce hangi asset'in yürürlükte
> olduğunu doğrula.

## Kanıt

`unity/HezarfenGame/Captures/` — depoya girmez (.gitignore), yeniden üretilebilir.

| Kare | Ne gösteriyor |
|---|---|
| `faz1_bogaz.png` | Boğaz suyu, dalgalar, Asya yakası kıyı basamağı |
| `faz1_galata_to_uskudar.png` | **Uçuş ekseni** — Galata sırtından Haliç/Boğaz ve Üsküdar |

Testler: **EditMode 67/67**. Yeni: `Terrain_SeaLevelIsWorldZero`,
`Terrain_LandStaysAboveSeaLevel`.

## Açık iş

- Arazi grafiği **fazla pozlanmış** görünüyor; graybox doku + sabit EV13'ün birleşimi.
  Nihai aydınlatma **Faz 7'nin** işidir (plan Bölüm 12), şimdilik okunabilirlik yeterli.
- Su rengi/saydamlığı ayarlanmadı — Marmara/Boğaz suyu için referans temelli çalışma
  **Faz 7**'ye ait.
- **Arazi yüzey dokusu planda adreslenmemiş.** Şu an tek graybox katmanı var; eğim ve
  irtifaya göre katman karışımı (kayalık/toprak/kuru ot) bir boşluktur — bkz. aşağıdaki
  not.

## Planda görülen boşluk: arazi yüzeyi ve doğal örtü

Plan, **yapılı çevreyi** ayrıntılı kurguluyor (Faz 2 kit, Faz 3 landmark, Faz 4 yerleşim)
ama şunları hiçbir faza bağlamıyor:

- **Arazi yüzey dokusu** — splatmap/katman karışımı. Şu an checkerboard graybox.
- **Kayalar, çalılar, kırsal örtü** — plan yalnızca *servi + mezarlık kütlesi* ve
  *bostan* diyor (Faz 2 kit parçası, Faz 4 yerleştirme). Okmeydanı, sur dışı, Boğaz
  yamaçları ve Kâğıthane için doğal örtü tanımlı değil.

Öneri: ikisi de **Faz 4'e** (Şehri Doldurma) eklensin — orada zaten `DistrictDef` ile
kural tabanlı yerleştirme ve performans altyapısı kuruluyor; doğal örtü aynı sistemin
doğal bir uzantısıdır. Karar Caner'in.
- Batimetri yok (yukarıda).
