# refs/ — Kaynak ve Lisans Kaydı

> **KURAL (plan Bölüm 14):** Bu listede kaydı olmayan hiçbir dosya `refs/` altına giremez.
> İhlal = görev reddi. Her indirmede önce buraya satır eklenir, sonra dosya indirilir.

**Durum:** Henüz hiçbir referans **görseli** indirilmedi. Bir **veri** kümesi indirildi
(Copernicus DEM — aşağıda). (2026-08-18)

## Kayıt formatı

| Dosya | Kaynak (URL) | Eser / Tarih | Lisans | Oyunda kullanım | İndirme tarihi |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

## İndirilen veri kümeleri

| Veri | Kaynak | Lisans | Durum | Tarih |
|---|---|---|---|---|
| Copernicus DEM GLO-30 (4 karo: N40/N41 × E028/E029) | `copernicus-dem-30m.s3.amazonaws.com` (AWS Open Data) | Serbest kullanım, **atıf zorunlu** | Kullanımda — `data/gis/istanbul/` | 2026-08-18 |

### ⚠️ Copernicus DEM — ZORUNLU ATIF METNİ

Aşağıdaki metin **oyun içi "Krediler" ekranına aynen** girecektir. Bu bir tercih değil,
lisans şartıdır:

> Produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and
> Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA;
> all rights reserved.

Yükümlülük üç yerde kayıtlıdır ki sessizce kaybolmasın:
`data/gis/istanbul/dem_meta.json` (`attribution_required: true`), arazi nesnesinin
`HistoricalTag.sourceNote` alanı, ve `TerrainPipelineTests` içindeki iki test.

**Not:** Copernicus GLO-30 bir **DSM**'dir (yüzey modeli) — modern binalar ve ağaçlar
irtifaya karışır. 1632 için bu bir tarihsel kusurdur; gerekçe ve azaltma yöntemi
[ADR 0007](../docs/decisions/0007-dem-terrain.md).

### ⚠️ OpenStreetMap — ZORUNLU ATIF METNİ

**Ne alındı:** Ayasofya'nın **eksen azimutu**, kütle basamaklarının yükseklik ve
plan boyutları, dört minarenin **gövde çapları ve konumları** (ADR 0045). Veri
dosyası depoya **girmedi**; Overpass'tan sorgulandı, ölçüler okundu, sayılar
`tools/blender/lib/ayasofya_kit.py` içine sabit olarak yazıldı.

Bunlar **olgu**dur, veritabanı alıntısı değil — ama üretilen eser ODbL'in
"produced work" tanımına girer ve **atıf ister**. Aşağıdaki metin oyun içi
"Krediler" ekranına, Copernicus'un yanına girecektir:

> Contains information from OpenStreetMap and OpenStreetMap Foundation, which is
> made available under the Open Database License (ODbL).

**Neden ölçüldü:** kaynaklar çelişiyordu. Ayasofya'nın tuğla minaresinin hangi
köşede olduğu konusunda TDV güneybatı, iki popüler kaynak güneydoğu ve kuzeydoğu
diyor. Plandan okunan gövde çapları (doğu çifti Ø3,6 / batı çifti Ø4,0) TDV'nin
iddiasını eledi, çünkü batı çifti **ikizdir** ve tuğla minare tektir. Ölçü,
kaynak seçmenin yerine geçti.

**Sınır:** OSM'den **geometri kopyalanmaz**. Bir yapının kütlesi ondan
türetilebilir (ölçü okumak), ama poligonları doğrudan mesh'e çevirmek "türev
veritabanı" tartışmasını açar. Şüphede kal → ADR yaz.

## Kendi çizimlerimiz (`refs/maps/`)

Hiçbiri telifli bir haritadan kopyalanmadı; hepsi kaynak METİNLERİNDEN ve
DEM verisinden türetildi, dolayısıyla **kendi eserimizdir**.

| Dosya | Ne | Kademe |
|---|---|---|
| `coastline_1632.geojson` | 1632 kıyı çizgisi taslağı (DEM konturu + düzeltme bölgeleri) | T2 |
| `landmarks_1632.geojson` | Landmark noktaları | T1/T2 |
| `walls_1632.geojson` | Sur hatları | T2 |
| `districts.geojson` | **Oyun** bölgeleri — tarihsel iddia YOK | Graybox |
| `greenery_1632.geojson` | Mezarlık / mesire / bağ / bostan + **ağaçsız** alanlar. Sınırlar **kaba kutudur**: Osmanlı kaynakları alan ölçüsü vermez (RESEARCH.md §4.5(a)). Hepsi `status: draft` | T1 (Okmeydanı yasağı) / T2 |

## İzin verilen kaynaklar (RESEARCH.md Bölüm 2 ve 8'den)

| Kaynak | Lisans | Kullanım sınırı |
|---|---|---|
| Melchior Lorck panoraması (1559) — Wikimedia Commons | Kamu malı | Serbest; kodeks görseli olarak da kullanılabilir |
| Erdoğan, B. B. (2013), *Galata Kent Surları ve Koruma Önerileri*, YL tezi, İTÜ FBE (dan. Z. Ahunbay) — polen.itu.edu.tr, açık erişim | Telifli (öğretim amaçlı görüntüleme/indirme izinli) | **Yalnızca BİLGİ kaynağı**: ölçü ve tarif okunur, görsel/çizim repoya **girmez**. Müller-Wiener'la aynı kural. Alınan ölçüler RESEARCH.md §5.2(b)'de kaynağıyla yazılıdır |
| G. J. Grelot gravürleri (1680) — Gallica bpt6k73264x / Heidelberg diglit | Kamu malı | Serbest |
| Ralamb Kıyafet Albümü (1657–58) — Library of Congress 2021668152 | Kamu malı | Serbest |
| Braun & Hogenberg, Civitates (1572–) | Kamu malı | Serbest |
| Nicolas de Nicolay, Navigations (1567/68) | Kamu malı | Serbest |
| Pîrî Reis Kitâb-ı Bahriye, 1629 Cündî nüshası | Kamu malı (yazma) | Nüsha sahibinin dijitalleştirme koşulu kontrol edilmeli |
| Poly Haven doku/HDRI | CC0 | Serbest; yine de krediler ekranına yazılır |
| Blender Studio Human Base Meshes | CC0 | Karakter/NPC taban geometrisi |
| OpenStreetMap verisi | ODbL | Footprint türetimi OK — **oyun içi atıf ZORUNLU** |

## YASAK kaynaklar

| Kaynak | Lisans | Neden |
|---|---|---|
| SALT Araştırma görselleri | CC BY-NC-ND | Ticari oyunda kullanılamaz. **Yalnızca insan gözüyle bakılır, repoya inmez.** |
| Müller-Wiener, *İstanbul'un Tarihsel Topografyası* planları | Telifli | Bilgi kaynağı olarak başvurulur; taranmış görsel repoya giremez, birebir kopyalanmaz |
| Modern kitap/edisyon taramaları (YKY, TTK vb.) | Telifli | Aynı |
| Assassin's Creed / GTA vb. oyun içerikleri | Telifli | Repoya giremez; yalnızca mekanik ilham |

### Prosedürel (kendi ürettiğimiz) dokular

Üçüncü taraf hakkı **yok**: girdi kullanılmaz, doku koddan üretilir. Kaynak
script depoda; çıktı `art/textures/generated/` altındadır.

| Doku | Üreten | Kullanım | Neden indirilmedi |
|---|---|---|---|
| `foliage_servi` (BC/N/R/ARM, 1024²) | `tools/textures/gen_foliage_texture.py` | Servi tacı (M_Foliage_Servi) | Poly Haven'da yaprak **alfa atlası** yok; lisanssız görsel indirmek yasak |
| `foliage_cinar` (BC/N/R/ARM, 1024²) | aynı script | Çınar tacı (M_Foliage_Cinar) | aynı |
| `lead_sheet` (BC/N/R/AO/ARM, 1024²) | `tools/textures/gen_lead_texture.py` | Kubbe ve külah kurşun örtüsü (M_Lead_Sheet) | Poly Haven'da **kurşun örtü** yok (sac levha, paslı çelik, dövme demir var); kubbe üstü uçuş oyununda en çok bakılan yüzey — düz gri renk bırakılamazdı (ADR 0021 §1) |
| `marble_white` (BC/N/R/AO/ARM, 1024²) | `tools/textures/gen_marble_texture.py` | Menzil taşı mermeri (M_Marble_White) | Poly Haven'da **mermer** yok. Menzil taşları kaynakta "tek parça mermer sütun"dur; kesme taş dokusu sütuna **taş sırası** koyuyordu (ölçülen dikey periyot 0,95 m) ve taşı çayırdan 4,4 kat koyu bırakıyordu (36,7 / 162,5) — ADR 0028 §7 |
| `brick_band` (BC/N/R/AO/ARM, 1024²) | `tools/textures/gen_brick_texture.py` | Tuğla kuşağı — Galata Kulesi gövdesi 13,20 ve 17,17 m (M_Brick_Band); tuğla-taş almaşık örgü | Poly Haven'dan tuğla **indirilmedi**; kuşaklar `cutstone` ile üretilince render'da tuğla değil "gövdeye dolanmış ince bir **gölge çizgisi**" olarak okunuyordu — kuşağın anlamı rengindedir. Ölçülen ayrım `old_stone_wall`dan **ΔE 21,1**. Karo boyu (0,75 m) seçilmedi, belgeli tuğla (35×35×4,5 cm) ve derz (2,5–3 cm) ölçülerinden **hesaplandı**. ADR 0033 §9 |
| `TerrainEarth` / `TerrainGrass` / `TerrainRock` / `TerrainShore` (BC/N/MASK, 1024²) | `tools/textures/gen_terrain_textures.py` | Arazi örtüsü katmanları (`TL_Terrain*.terrainlayer`) | Arazi Faz 1'den beri **katmansızdı**; zemin tek düz yüzeydi (ADR 0024). Bu dokular Blender'a gitmez — tek tüketici `TerrainLit` — bu yüzden `art/textures/generated/` altında değil, doğrudan `Assets/_Project/Art/Textures/Terrain/` altındadır |

Ortak parçalar `tools/textures/proclib.py` içinde. Girdi yalnızca tohumlanmış
sayı üretecidir; hiçbir çıktıda üçüncü taraf verisi yoktur.

<!-- POLYHAVEN:BEGIN (otomatik — fetch_polyhaven.py üretir) -->

### Poly Haven dokuları (CC0)

Poly Haven **CC0**'dır: hukuken atıf zorunlu değildir. Yine de plan gereği krediler ekranına yazılır ve üreticiler burada kayıtlıdır.

| Dosya kökü | Kullanım | Gerçek ölçü | Üretici(ler) | Kaynak |
|---|---|---|---|---|
| `art/textures/polyhaven/bark_brown_01/` | Servi gövdesi (M_Bark) | 1.00×1.00 m | Rob Tuytel | [polyhaven.com/a/bark_brown_01](https://polyhaven.com/a/bark_brown_01) |
| `art/textures/polyhaven/bark_platanus/` | Çınar gövdesi (M_Bark_Cinar) | 1.50×1.50 m | Dimitrios Savva | [polyhaven.com/a/bark_platanus](https://polyhaven.com/a/bark_platanus) |
| `art/textures/polyhaven/large_sandstone_blocks/` | Kesme taş: çeşme gövdesi, ayna taşı, kitabe, mescit duvarı (M_Stone_Cut) | 3.00×3.00 m | Rob Tuytel | [polyhaven.com/a/large_sandstone_blocks](https://polyhaven.com/a/large_sandstone_blocks) |
| `art/textures/polyhaven/cobblestone_floor_001/` | Sokak kaldırımı (M_Paving_Kaldirim) | 2.40×2.40 m | Rob Tuytel | [polyhaven.com/a/cobblestone_floor_001](https://polyhaven.com/a/cobblestone_floor_001) |
| `art/textures/polyhaven/painted_plaster_wall/` | Kireç badanalı kâgir kat (M_Plaster_Lime) | 2.00×2.00 m | Amal Kumar | [polyhaven.com/a/painted_plaster_wall](https://polyhaven.com/a/painted_plaster_wall) |
| `art/textures/polyhaven/grey_plaster/` | Gayrimüslim mahalle varyantı (M_Plaster_Grey) | 1.00×1.00 m | Rob Tuytel | [polyhaven.com/a/grey_plaster](https://polyhaven.com/a/grey_plaster) |
| `art/textures/polyhaven/clay_roof_tiles_02/` | Alaturka kiremit (M_Roof_Alaturka) | 2.50×2.50 m | Amal Kumar | [polyhaven.com/a/clay_roof_tiles_02](https://polyhaven.com/a/clay_roof_tiles_02) |
| `art/textures/polyhaven/ceramic_roof_01/` | Yaşlanmış çatı varyantı (M_Roof_Alaturka_Aged) | 3.50×3.50 m | Rob Tuytel | [polyhaven.com/a/ceramic_roof_01](https://polyhaven.com/a/ceramic_roof_01) |
| `art/textures/polyhaven/old_stone_wall/` | Taş subasman ve avlu duvarı (M_Stone_Rubble) | 2.00×2.00 m | Charlotte Baglioni | [polyhaven.com/a/old_stone_wall](https://polyhaven.com/a/old_stone_wall) |
| `art/textures/polyhaven/weathered_planks/` | Ahşap karkas üst kat / cumba (M_Timber_AsiRed) | 2.00×2.00 m | Dario Barresi, Dimitrios Savva | [polyhaven.com/a/weathered_planks](https://polyhaven.com/a/weathered_planks) |

### Poly Haven HDRI'ları (CC0)

İnceleme render'ının **gerçekçi** aydınlatma kipi için (nötr kip ADR 0006'da tanımlıdır ve değişmez).

| Dosya | Kullanım | Üretici(ler) | Kaynak |
|---|---|---|---|
| `art/textures/hdri/kloofendal_48d_partly_cloudy_puresky_4k.hdr` | Gündüz inceleme aydınlatması (güneş 48°, hafif bulutlu) | Greg Zaal, Jarod Guest | [polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky) |

Yeniden indirme: `python tools/textures/fetch_polyhaven.py --res 2k --hdris`

<!-- POLYHAVEN:END -->
