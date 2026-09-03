# refs/ — Kaynak ve Lisans Kaydı

> **KURAL (plan Bölüm 14):** Bu listede kaydı olmayan hiçbir dosya `refs/` altına giremez.
> İhlal = görev reddi. Her indirmede önce buraya satır eklenir, sonra dosya indirilir.

**Durum:** İlk referans **görselleri** indirildi (Rålamb kıyafet albümü, kamu malı —
aşağıda). Bir **veri** kümesi ve bir **CC0 taban geometri** de kayıtlı. (2026-08-28)

## ⚠️ TİCARİ YAYIN KOŞULU (Caner, 2026-08-30)

Oyun **Steam'de satılacak**. Bu, kayda giren her satır için tek bir soruyu
zorunlu kılıyor: *bu varlık ticari bir üründe kullanılabilir mi?*

- **Girebilir:** CC0, kamu malı, MIT/BSD benzeri, "royalty-free commercial".
- **GİREMEZ:** CC BY-NC (ticari değil), CC ND (türev yok), "yalnız kişisel
  kullanım", telifli kitap/arşiv taraması.
- **Koşullu girer:** atıf zorunlu olanlar (CC BY, Copernicus, ODbL) — atıf
  metni **aynı turda** `Krediler.Metin`'e de yazılır.

**Denetim otomatik:** `KredilerTests.EveryAttributionTheRegisterDemandsIsOnScreen`
bu dosyada "atıf zorunlu" diye işaretli her satırı okur ve krediler ekranında
karşılığını arar. Karşılıksız bir satır kalırsa test kırmızı yanar.

> Bu denetim yazıldığı gün bir kusur buldu: **Copernicus DEM GLO-30** burada
> "atıf zorunlu" yazılıydı, şart koşulan metin `tools/gis/dem_fetch.py` içinde
> duruyordu, ama krediler ekranında yalnızca *"kamu erişimli DEM kaynakları"*
> vardı — arazinin tamamı o veriden türetilmiş olmasına rağmen. Test o gün
> yazılmasaydı oyun eksik atıfla yayınlanacaktı.

## Kayıt formatı

| Dosya | Kaynak (URL) | Eser / Tarih | Lisans | Oyunda kullanım | İndirme tarihi |
|---|---|---|---|---|---|
| `refs/ralamb/Ralamb-*.jpg` (12 plaka) | Wikimedia Commons, `Category:Rålamb Costume Book` — `upload.wikimedia.org/wikipedia/commons/...` | Claes Rålamb, *Rålambska dräktboken*, **1657** | **Kamu malı** (PD-US; eser 1657) | Faz 5 kıyafet **referansı** — çizim kopyalanmaz, giyim dilbilgisi (katman, boy, kuşak yeri, başlık) okunur | 2026-08-28 |

### Rålamb hakkında iki uyarı

**1. Tarih.** Albüm **1657–58**, oyun **1632**. Yirmi beş yıl. Osmanlı erkek
kıyafetinin ana hatları (şalvar–gömlek–entari–kuşak–kavuk) bu aralıkta
değişmedi, ama ayrıntı — kavuk biçimi, kaftan kesimi — değişebilir. Bu yüzden
kıyafet **T2 (yeniden kurgu)**, T1 değil. Peter Mundy albümü (1618) öbür
yandan yaklaşıyor; 1632 ikisinin arasında kalıyor ve **tam ortasında bir
kaynak yok.**

**2. Ne kopyalanır, ne kopyalanmaz.** Bu proje mimaride "fotoğraftaki gibi
değil, fotoğraftaki dil kadar" kuralını izledi. Kıyafette de aynısı: minyatür
**kopyalanmaz**, okunan şey oranlardır — entari nerede biter, kuşak nereye
oturur, kavuk başa göre ne kadar büyüktür. Eser kamu malı olduğu için kopyalamak
hukuken serbest olurdu; kural hukuki değil, yöntemsel.

## İndirilen veri kümeleri

| Veri | Kaynak | Lisans | Durum | Tarih |
|---|---|---|---|---|
| Copernicus DEM GLO-30 (4 karo: N40/N41 × E028/E029) | `copernicus-dem-30m.s3.amazonaws.com` (AWS Open Data) | Serbest kullanım, **atıf zorunlu** | Kullanımda — `data/gis/istanbul/` | 2026-08-18 |
| Blender Studio **Human Base Meshes** bundle v1.4.1 | `download.blender.org/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.4.1.zip` | **CC0** (kamu malına bırakılmış; atıf zorunlu değil, ticari kullanım serbest) | Faz 5 taban geometrisi — `art/base/blender-studio/` (depoya girmez, `meta.json` girer) | 2026-08-28 |
| **MPFB 2.0.17** (MakeHuman Plugin For Blender) — eklenti + çekirdek varlıklar | `extensions.blender.org/download/sha256:4f0a879d64a39bf646fbf5f53601ac678855da329d650617dca5737548239a87/add-on-mpfb-v2.0.17.zip` | Eklenti kodu: **GPL-3.0-or-later** (Blender eklenti platformu, SPDX). Çekirdek varlıklar (taban mesh, hedefler, deriler) ve **dışa aktarılan modeller: CC0** | Faz 1 taban gövde üretimi. Blender ≥ 4.2 (bizde 5.2). **Yalnız çekirdek varlıklar** — üçüncü taraf asset pack'leri ayrı lisans ister, kullanılmaz | 2026-08-30 |

| **Mixamo** animasyon klipleri (Adobe) | `mixamo.com` — Adobe ID ile, ücretsiz | Adobe SSS: Mixamo karakter ve animasyonları **telifsiz**; ticari projelerde kullanılabilir, **atıf zorunlu değil**. Kısıt: klipler bir animasyon **kütüphanesi/asset paketi olarak yeniden satılamaz** — oyunun içinde kullanılır. | Faz II.A locomotion + Faz II.G kalabalık. İndirilen ham FBX `Assets/_Project/Art/Animation/Mixamo/` altına iner (Unity okuduğu için depoya girer) | 2026-08-30 |


### Mixamo hakkında not — neden ticari yayına uygun

Adobe'un Mixamo SSS'i klipleri **telifsiz (royalty-free)** sayar ve
ticari projelerde kullanıma açar; atıf istemez. Bizim için önemli olan
tek kısıt şudur: klipler **kendileri bir ürün olarak** yeniden
satılamaz — yani bir "animasyon paketi" çıkaramayız. Oyunun içinde
oynatmak tam olarak amaçlanan kullanımdır.

**Ne indirilmeyecek:** Mixamo'nun karakter modelleri (X Bot, Y Bot ve
diğerleri) oyuna **girmez**. Onlar yalnız indirme sırasında iskelet
taşıyıcısıdır ve indirme "Without Skin" seçilerek yapılır; gelen
dosyada mesh yoktur. Karakterimiz MPFB2'den kendi hattımızda üretiliyor
(ADR 0079) ve öyle kalacak.

**Kayıt disiplini:** her klip `art/mixamo/meta.json`'a adı, indirme
tarihi ve indirme ayarlarıyla yazılır. Bir klip kayıtta yoksa oyunda
kullanılmaz.

### Human Base Meshes hakkında not

Plan Bölüm 10 taban geometriyi bu kaynağa bağlıyor. **CC0 olduğu için
atıf hukuken zorunlu değil** — ama künyeye yine de yazılacak, çünkü bu
proje kullandığı her şeyin nereden geldiğini söylemeyi kural edindi ve
"zorunlu değil" ile "söylemeye değmez" aynı şey değil.

`.blend` dosyası depoya **girmez** (yeniden indirilebilir üçüncü taraf
kaynağı); ondan türeyen Hezarfen gövdesi girer. Kayıt `meta.json`da:
sürüm, SHA-256 ve indirme tarihi. Sürüm değişirse tabanın değiştiğini
o dosyadan anlarız.

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

### MPFB / MakeHuman — GPL araç, CC0 çıktı

Karışması kolay olduğu için ayrıca yazıyorum: **GPL eklentiyi bağlar,
ürettiğini değil.** Blender'ın kendisi GPL'dir ve Blender'da yapılan eser
sahibinindir; MPFB de aynı.

MakeHuman'ın kendi SSS'i birebir şöyle diyor:

> *"All core assets (the base mesh, targets, skins…) are shared under CC0."*
> — [Can I sell models made with MPFB?](https://static.makehumancommunity.org/mpfb/faq/can_i_sell_models.html)

Kapalı kaynak ticari oyunda kullanım açıkça serbest; GPL yalnızca eklenti
koduna uygulanır.

**Sınır — ve bu bizim için bağlayıcı:**

> *"Note that if you use a third party asset shared under a different
> license it is your responsibility to fulfill the obligations of that
> license."*

Yani **üçüncü taraf asset pack'i indirilmez.** Yalnız MPFB'nin çekirdek
varlıkları kullanılır. Bir gün bir pack gerekirse önce buraya satır
yazılır, sonra indirilir.

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

### Ortam sesi — kendi üretimimiz

`tools/audio/gen_ortam.py` dört ortam yatağını **sentezle** üretir
(deniz, rüzgâr, gece, çarşı). İndirilen ses dosyası yoktur; izlenecek
lisans da yoktur. Doku hattının kararının aynısı: ticari yayın her
varlığı bağlıyor ve indirilen bir dalga sesinin lisansını takip etmek,
üretmekten pahalı.

| varlık | kaynak | lisans | ticari |
|---|---|---|---|
| `Assets/_Project/Audio/Ortam/*.wav` | `tools/audio/gen_ortam.py` — kendi işimiz | proje telifi | ✔ |


### Kumaş ve ten dokuları — prosedürel, kendi eserimiz (2026-09-02)

`art/textures/generated/kumas_{keten,cuha,ipek,kece,kilim}`,
`art/textures/generated/deri_insan`, `art/textures/generated/sakal`,
`art/textures/generated/kosele` ve `art/textures/generated/tuy`
**bizim ürettiğimiz** dokulardır; üçüncü taraf hakkı yoktur.
Üreteçler: `tools/textures/gen_kumas_texture.py`,
`tools/textures/gen_deri_texture.py`,
`tools/textures/gen_sakal_texture.py`,
`tools/textures/gen_kosele_texture.py` ve
`tools/textures/gen_tuy_texture.py`.

Tüy dokusunun da **girdisi yoktur**: bindirme, omurga ve tel doğrudan
ölçüden çiziliyor (kartal birincil tüyü 5-8 cm en, tel aralığı
0,5-1 mm). Kanat yüzeyi bu dokudan önce `weathered_planks` —
yani **kereste** — kullanıyordu; aynı kusur köselede de vardı ve
aynı şekilde kapandı.

Sakal dokusunun **girdisi yoktur**: teller doğrudan sayıdan çiziliyor
(yön, kümelenme, uzunluk). Kart atlası (`gen_hair_texture.py`) da
aynı ailedendir ve aynı şekilde kendi eserimizdir.

Ten dokusunun **girdisi** MPFB2'nin kendi bölge maskeleridir
(`data/textures/mpfb_{face,lips,eyelids,ears}.jpg`) — yukarıdaki
MPFB 2.0.17 satırının kapsadığı **CC0 çekirdek varlık** kümesi. Maskeler
"nerede ne var" bilgisini taşır (yüz, dudak, göz kapağı, kulak);
tenin rengi, oranları, gözeneği ve gözün çizimi bize aittir. Maske
dosyaları depoya **girmez**; çıktı girer.

Kilim dokusu **motif taşımaz** ve bu bilinçlidir: bir kilimin
motifi kaynak ister; uydurulmuş bir motif, kilimi kilim yapan şeyi
uydurmak olurdu. Doku yalnızca **atkı yüzlü düz dokumanın** yüzeyini
taşır. Motif belgeyle birlikte gelir.

Ticari kullanım: sorun yok. Kaynak CC0, çıktı bizim.
