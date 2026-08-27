# Kurulum Kontrol Listesi — `[İNSAN]` (Caner)

Plan Görev 2'nin karşılığı. Sırayla git; her adımın sonundaki **doğrulama** satırını bana bildir,
sürümleri `docs/decisions/0001-versions.md`e ben işlerim.

Hâlihazırda kurulu ve doğrulanmış: **Blender 5.2.0 LTS**, **Python 3.13.14**, **Git**, **winget**.

---

## 0. (Opsiyonel ama tavsiye) Git'i şimdi aç
Unity projesi oluşturulduktan sonra `Library/` klasörü on binlerce dosya olur. `.gitignore` zaten
hazır, o yüzden şimdi geçmek maliyetsiz — sonra geçmek sancılı. Lokal kalmak istersen atla
(bkz. ADR 0003); uzak depo gerekmez, sadece lokal geçmiş.

```powershell
cd d:\ClaudeCodeProjects\Hezarfen_Oyunu
git init
git lfs install
git add .gitattributes .gitignore
git commit -m "Add version control config"
git add .
git commit -m "Initial project skeleton"
```

---

## 1. Unity Hub
İndir: https://unity.com/download → Unity Hub'ı kur, Unity hesabınla giriş yap, lisansı etkinleştir
(kişisel kullanım için **Personal** lisansı yeterli).

**Doğrulama:** Hub açılıyor ve "Installs" sekmesi görünüyor.

## 2. Unity 6 LTS Editor
Hub → **Installs** → **Install Editor** → **Unity 6 LTS (6000.x)** listesindeki **en güncel** sürüm.

Seçilecek modüller:
- ✅ **Windows Build Support (IL2CPP)** — Faz 8 build'i için
- ⬜ Diğer platformlar (Android/iOS/WebGL) — **kurma**, gereksiz yer kaplar
- ⬜ Visual Studio — **gerekmez** (kodu ben yazıyorum, Unity kendi derleyicisiyle gelir).
  İstersen kur, zararı yok.
- ✅ Documentation — opsiyonel, faydalı

**Doğrulama:** Hub → Installs altında tam sürüm numarası görünüyor (ör. `6000.x.yfz`). **Bu numarayı bana yaz.**

> ⚠️ Sürüm bir kez seçilir ve kilitlenir. Plan kuralı: ara sürüm atlama yok.

## 3. Proje oluştur
Hub → **Projects** → **New project**:

| Alan | Değer |
|---|---|
| Şablon | **High Definition 3D (HDRP)** — "Universal 3D" DEĞİL |
| Project name | `HezarfenGame` |
| Location | `d:\ClaudeCodeProjects\Hezarfen_Oyunu\unity` |

Sonuç yol: `d:\ClaudeCodeProjects\Hezarfen_Oyunu\unity\HezarfenGame`

İlk açılışta HDRP Wizard çıkarsa varsayılanları kabul et. Proje açıldıktan sonra Unity'yi
**kapatabilirsin** — paket eklemelerini (Input System, Cinemachine 3.x, Addressables, Test
Framework) ben `Packages/manifest.json` üzerinden yaparım.

**Doğrulama:** Yukarıdaki klasörde `ProjectSettings/ProjectVersion.txt` var.

## 4. uv — ✅ TAMAM (Claude kurdu, 2026-08-17)
`winget install --id=astral-sh.uv -e` ile **uv 0.12.3** kuruldu ve doğrulandı.
PATH'e kaydedildi — **açık terminallerinde görünmez, yeni terminal gerekir.**

## 5. blender-mcp eklentisi — ✅ TAMAM (Claude kurdu, 2026-08-17)
Eklenti headless kuruldu, etkinleştirildi ve tercihler kaydedildi:
`%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\addon.py` (bl_info 1.2)

Doğrulandı: panel kaydoldu, `start_server`/`stop_server` operatörleri erişilebilir,
`uvx blender-mcp` sunucusu ayağa kalkıyor. Blender 5.2 uyum riski kapandı (ADR 0001).

**Sunucuyu başlatmak (her Blender oturumunda gerekir):**
```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --python tools\blender\start_mcp_server.py
```
Alternatif elle yol: Blender'da 3D görünüm → **N** tuşu → **BlenderMCP** sekmesi → **Connect to Claude**.

> ⚠️ **Telemetri:** Paket varsayılan olarak dışarı veri gönderiyor. `.mcp.json.example` içinde üç
> ortam değişkeniyle tamamen kapattım. Blender tercihlerindeki **"Allow Telemetry" kutusunu
> İŞARETLEME** — açık bırakılırsa prompt'lar, kod ve viewport ekran görüntüleri yükleniyor.
> Gerekçe ve kod incelemesi: ADR 0001.

> ⚠️ **Güvenlik:** Sunucu `localhost:9876`ya bağlanıyor (kodda doğrulandı, dışarı açılma seçeneği
> yok). Eklenti Blender içinde keyfî Python çalıştırabildiği için bu önemli.

## 6. Unity MCP adayları (ikisi de kurulur, sonra yarışır)

**Aday 1 — Resmî Unity MCP Server:**
Unity Editor → **Edit → Project Settings → AI → Unity MCP** → etkinleştir.
(Unity 6 sürümüne göre menü konumu değişebilir; bulamazsan bana ekran görüntüsü yolla.)

**Aday 2 — MCP for Unity (CoplayDev):**
Bu paketi ben `manifest.json`a ekleyebilirim — sen sadece Unity'yi açıp paketin inmesini bekle.
Sonra: **Window → MCP for Unity → Configure All Detected Clients**.

**Doğrulama:** Her ikisi de bağlantı için Editor'de bir **onay diyaloğu** gösterecek — onayla.
Bu diyaloglar senin görevin; ben onaylayamam.

---

---

## Durum özeti (2026-08-17)

| Adım | Durum |
|---|---|
| 0. Git | ⬜ Ertelendi (ADR 0003 — lokal önce) |
| 1. Unity Hub | ✅ Caner |
| 2. Unity Editor 6000.5.8f1 (+IL2CPP) | ✅ Caner |
| 3. HDRP projesi `unity\HezarfenGame` | ✅ Claude — ADR 0004, testler 6/6 |
| 4. uv 0.12.3 | ✅ Claude |
| 5. blender-mcp eklentisi | ✅ Claude |
| 6. Unity MCP paketi (CoplayDev v10.1.2) | ✅ Claude — ADR 0002 |
| 7. MCP politikası (stdio + telemetri kapalı + workspace dizini) | ✅ `Hezarfen → MCP` menüsünden uygulandı |
| 8. Claude Code yeniden başlatma + bağlantı | ✅ **Duman testi geçti — ADR 0002** |

**Kurulum tamamlandı.** Görev 2 ve Görev 3 kapandı; Faz 0'ın önünde engel yok.

### Doğrulanmış bağlantı durumu (2026-08-17)
- Unity köprüsü **`127.0.0.1:6400` dinliyor** (yalnız localhost)
- MCP penceresi: Transport = **Stdio** ✅
- "No Session" ve "Not Configured" **beklenen** — istemci henüz bağlanmadı ve paketin
  otomatik kayıt yolunu (Claude CLI) kullanmıyoruz; `.mcp.json` elle yazıldı.

### MCP kullanırken bilinmesi gerekenler
- **Unity açık kalmalı** — kapanırsa 6400 kapanır, köprü düşer.
- **Test koşmadan önce aktif sahne kaydedilmiş olmalı.** Kaydedilmemiş sahnede test
  koşucusu reddediyor ve ardından *tüm* komutlar zaman aşımına uğruyor
  (`Command TCS timed out (N consecutive)`). Köprü çökmüş gibi görünür ama çökmez;
  komut göndermeyi kesince kendiliğinden düzelir. Ayrıntı: ADR 0002.
- Askıda test işi kalırsa: `run_tests(clear_stuck=true)`.

### Faz 0 durumu
1. ✅ **Görev 4: graybox sahne** — `Assets/_Project/Scenes/FlightSlice.unity`
   | Öğe | Değer |
   |---|---|
   | `GB_Ground` | 5000×5000 m düz zemin |
   | `GB_Tower_Galata` | ⌀9 m, **100 m** yüksek; tepe y=100 |
   | `GB_LaunchPlatform` | 14×14 m, y=101 |
   | `GB_Target_Dogancilar` | 60×60 m, **x=3358 m** |
   | Işık | Yön ışığı (50°, −30°) + HDRP Sky/Fog global hacmi |

   Ölçüler `GrayboxSceneTests` ile kilitli. Tüm öğeler `HistoricalTag` (Graybox) taşıyor.
2. ✅ **Görev 5: `GlideController` v0 + `WindTuning` + Cinemachine kamera**

   Ölçülen davranış (simülasyondan, teoriden değil):

   | Girdi | alpha | hız | L/D | Davranış |
   |---|---|---|---|---|
   | Nötr | 9,2° | 10,6 m/s | **11,39:1** | Kararlı seyir |
   | Tam burun aşağı | 0,7° | 23,0 m/s | 5,0:1 | Hız için verim takası |
   | Tam burun yukarı | 18,5° | — | 2,4:1 | Stall, verim çöküyor |
   | Yarım sağ yatış | — | — | — | Bank 23,9°, net sağa dönüş |

   Teori 11,56:1 diyordu, simülasyon 11,39:1 üretti — model tutarlı.

3. ✅ **Görev 6: `WindField` v0 + rüzgâr hacimleri + görünür rüzgâr**

   Global lodos **9 m/s (+X)** + 6 adet elle yerleştirilmiş hacim (gizmo'lu).
   Hacim tipleri: `Sphere`, `Box`, `Column` (termik sütunu).

   **Ölçülen beceri eğrisi** (kalkış 102 m → hedef 3358 m):

   | pitch | ulaştı | süre | varış irtifası |
   |---|---|---|---|
   | 0,0 (nötr) | ✅ | 171 s | 125 m |
   | −0,5 | ✅ | **149 s** | 82 m |
   | −0,7 | ✅ | **136 s** | 34 m |
   | −0,8 | ❌ | 2891 m'de düştü | — |

   Hızlanmak erken vardırıyor ama payı eritiyor; eşikten sonra yetişmiyor.
   Üç rota da hedefe ulaşıyor, farklı paylarla: direkt 125 m, kuzey 67 m, güney 13 m.

**Test durumu: 32 EditMode + 7 PlayMode = 39 test, hepsi geçiyor.**

### Nasıl oynanır
`Assets/_Project/Scenes/FlightSlice.unity` aç → **Play**.
- **W/S** veya **↑/↓** — burun aşağı / yukarı (hız / yavaşlama)
- **A/D** veya **←/→** — yatış (dönüş)
- Gamepad: sol çubuk
- Hedef +X yönünde 3358 m ötede. HUD'da **HEDEFE** mesafeyi, **DİKEY**'de yükseliş/batışı gör.
- Variometre çubuğu mavi = yükseliyorsun, kırmızı = batıyorsun. Mavi bulduğun yerde kal.

### Bir kez düşülen tuzaklar (tekrarlanmasın)
- **`Physics.simulationMode` `Script`te bırakılmamalı.** Edit-mode ölçüm scriptleri bunu
  değiştirir; hata verip çıkarlarsa ayar `DynamicsManager.asset`e yazılı kalır ve
  **Play'e basıldığında hiçbir şey hareket etmez.** Ölçüm scriptleri modu `try/finally`
  ile geri almalı.
- **Edit-mode `Physics.Simulate` sahnedeki GERÇEK nesneleri hareket ettirir.** Ölçümden
  sonra sahne kaydedilirse planör yerde kalmış olur. Ölçüm ya ayrı nesnelerle yapılmalı
  ya da sonrasında sahne diskten yeniden yüklenmeli.
- **`EditorUtility.CopySerialized` asset'in AD alanını da kopyalar** — `WT_Faz0_Default`
  bir kez adsız kaldı.
- **`refresh_unity` "başarılı" dönse de derleme başarısız olabilir.** Her refresh'ten
  sonra konsol hatalarını oku; aksi halde eski assembly ile çalışıp saatlerce yanlış yerde
  hata ararsın (bir kez oldu).
- **`run_tests` "failed to initialize" dese de testler koşmuş olabilir.** Sahne
  değiştirdikten sonra MCP iş yöneticisi koşuyu kaydedemeyip 120 sn sonra kendini
  başarısız sayabilir; konsolda `IPrebuildSetup` → `Saving results to:` satırları varsa
  testler gerçekten çalışmıştır. Çözüm: **aynı komutu tekrar çalıştır** (ikincisi
  saniyeler içinde döner). Kodda hata arama.
- **`execute_code` C# 6'dır (CodeDom).** Metot gövdesi olarak derlenir: `using`
  yönergesi yazılamaz, `Object` belirsizdir (`UnityEngine.Object` yaz), LINQ ve
  `out var` yoktur. Roslyn kurulu değilse `compiler: "roslyn"` istemek işe yaramaz.
- **Projede DÖRT HDRP asset'i var ve `QualitySettings` olanı kazanır.**
  `GraphicsSettings.defaultRenderPipeline`'da bir ayarı açmak hiçbir şey yapmayabilir;
  gerçekte kullanılan `Assets/Settings/HDRP High Fidelity.asset`. Su tam olarak böyle
  "sessizce çalışmadı" — tek uyarı bile çıkmadan. Görsel bir özellik açılmıyorsa önce
  `QualitySettings.renderPipeline`'ı oku (ADR 0009).
- **HDRP yönlü ışığı LUX ile ölçülür.** Built-in ölçeğinden gelen `intensity = 3.2`
  alacakaranlıktan koyudur ve tüm geometriyi siyah bırakır. Açık gün ~100 000 Lux.
- **`Terrain.SampleHeight` TERRAIN-YEREL değer verir.** Arazi deniz tabanına (−12 m)
  yerleştirildiği için dünya kotu = `SampleHeight(...) + terrain.transform.position.y`.
  Eklemeyi unutmak bütün kıyıyı "12 m yüksekte" gösterir.
- **`skyAmbientMode = Static` fırınlanmış ışık ister.** Fırınlanmamış sahnede ortam
  siyah kalır. Edit-mode tek kare yakalamada otomatik pozlama da yakınsamaz — inceleme
  kareleri için sabit pozlama kullan.
- **`VolumeProfile.Add<T>()` diske YAZMAZ.** Bileşeni bellekte kurar; asset'e
  `AssetDatabase.AddObjectToAsset` ile alt-nesne olarak eklenmezse ilk domain reload'da
  sessizce kaybolur. Profil "var" görünür, içi boştur, sahne varsayılana düşer. Bir
  performans ölçümü bu yüzden sis kapalıyken koşturuldu. Kur: `SkyProfileBuilder.cs`.
- **Ölçüm koşarken Editor'e dokunma.** MCP üzerinden yoklamak en kötü kare metriğini
  kirletir (bir adımda 943 ms'lik sahte takılma göründü). Medyan ve p95 dayanıklıdır,
  maksimum değildir.
- **Domain reload, kaydedilmemiş SAHNE değişikliklerini atar.** Script derlemesi
  (her `refresh_unity --compile`, her PlayMode koşusu) sahneyi diskten yeniden yükler.
  Sahneyi değiştiren bir içe aktarımı çalıştırıp sonra derleme tetiklersen, aradaki
  iş sessizce kaybolur — ve sonradan "kaydet" demek zaten geri alınmış sahneyi
  kaydeder, yani hata kaydetme adımında görünmez. **Kural: sahneyi değiştiren menü
  komutlarını son derlemeden SONRA çalıştır ve hemen kaydet.** Kapatmadan önce
  `GIS_1632` altındaki grup sayısını (10 olmalı) doğrula.
- **DEM dizisinde satır 0 = GÜNEY, görüntüde satır 0 = KUZEY.** Bindirme çizerken
  taban rasteri `np.flipud` ile çevrilmezse arazi ile vektörler **dikeyde ters** oturur —
  ilk çıktıda Marmara kuzeyde göründü. Aynı hata `coastline_build.draw_overlay`da da
  vardı (ADR 0011 §5). Bindirme "bir tuhaf" görünüyorsa önce DEM satırlarının kara
  oranını ölç, düzeltmeye sonra geç.
- **İnceleme bindirmesinde zemindeki mavi BUGÜNKÜ su hattıdır.** DEM'de dolgular
  duruyor. 1632 kıyısını da maviye çizmek, incelemede görülmesi gereken tek şeyi —
  iki hattın farkını — gizler. Kanıt sınıfı ayrı renk demektir.
- **Addressables yapılandırması `_Project` dışına iner.** `DefaultObject.asset` yolu
  paket içinde sabit kodludur; ayarlar varlığını taşımak yapılandırmayı ikiye böler.
  Bilinçli istisna: ADR 0011 §4.
- **`activateOnLoad: false` ile yüklenen sahne bir süre `isLoaded` değildir.**
  "Yerleşik ama yüklü değil" saymak eşzamanlı yükleme tavanını ölçmez; uçuştaki
  yükleme sayısını (`DistrictStreamer.LoadsInFlight`) ölç. Bir test tam olarak bu
  yüzden yanlış düştü — kod doğruydu, ölçü yanlıştı.

### Faz 0 kabul kriterleri — durum
| Kriter | Durum |
|---|---|
| Uçuş 90-150 sn | ✅ 136-149 sn (aktif uçuşla) |
| En az 3 rota | ✅ direkt / kuzey / güney |
| Rüzgârsız oran 8-12:1 | ✅ 11,39:1 (ölçüldü) |
| Akıntılarla efsane mesafesi kapanıyor | ✅ 1156 m → 3358 m |
| WASD / gamepad girdisi tepki veriyor | ✅ Caner doğruladı (2026-08-17) |
| **Caner: "10 kez uçtum, hâlâ zevkli"** | 🕒 **ERTELENDİ — Caner kararı (2026-08-17)** |

**Ertelenen kapı hakkında.** Caner: *"bunu bence en son bakalım çünkü oyun grafikleri,
çevre, animasyon falan eklenince o his değişir."* Karar kendisinin; kayıt altındadır.

Plan Bölüm 5 bu kapıyı buraya bilinçli koymuştu ("bu faz başarısızsa sanat üretimine hiç
girilmez"). Bu yüzden kapı **kapatılmadı, açık bırakıldı** ve şu sınır çizildi:

- ✅ **Serbest:** alet/boru hattı yapımı (Görev 7-10) — yanlış çıkarsa atılan şey bir
  scripttir, aylarca modellenmiş bir şehir değil.
- ⛔ **Kilitli:** Faz 2 gerçek sanat üretimi (Osmanlı yapı kiti seri üretimi) bu kapı
  geçilmeden başlamaz.

Kapıya dönüş noktası: `render_preview.py` (Görev 8) hazır olduğunda, çok daha iyi bir
inceleme paketiyle.

### Faz 0'da bilinen pürüz — playtest'te karar verilecek
Tam burun yukarı komutunda derin stall sırasında hücum açısı anlık olarak 130-145°'ye
fırlıyor (aygıt takla atıyor), sonra kendini toparlıyor. Ortalama davranış sağlıklı
(bank ~0, kurtarıyor) ama zirve değer sert. Asılı planörlerde tumble gerçek bir olaydır;
bunun "adil ceza" mı yoksa "sinir bozucu" mu olduğu **oynayarak** karara bağlanmalı.
Ayar noktaları: `stallBreakMoment`, `minStabilityAuthority`, `angularDamping`.

Ekran yakalamaları `unity/HezarfenGame/Captures/` (Assets dışında — Assets'e düşerse
doku olarak import edilir; .gitignore'da).

Faz 0 kapısı: uçuş **eğlenceli** mi? Değilse sanat üretimine geçilmez (plan Bölüm 5).

### Görev 7 durumu — varlık boru hattı ✅ (2026-08-17)

Blender → FBX → Unity hattı uçtan uca çalışıyor ve **ölçümle** doğrulandı. Ayrıntı ve
eksen tablosu: [ADR 0005](decisions/0005-asset-pipeline.md).

| Kanıt | Sonuç |
|---|---|
| 1 m küp Unity'de | `(1.0000, 1.0000, 1.0000)` |
| Kök nesne rotasyonu | `(0, 0, 0)` — `-89.98°` tuzağı yok |
| Eksen eşlemesi | Unity(x,y,z) = Blender(-x, z, -y); aynalanma yok |
| Testler | EditMode 47/47, PlayMode 7/7 |
| Üretilen varlık | `PF_BoxHouse` — LOD0/LOD1 + convex collider + HistoricalTag |

Ölçü aleti `SM_AxisCalibration.fbx` kalıcıdır; **silme**. Boru hattı ayarı değişirse
yeniden üretip testleri koştur.

### Görev 8 durumu — inceleme paketi üreticisi ✅ (2026-08-17)

`tools/blender/render_preview.py` tek komutla 8 açı + kontak sayfası + ölçü tablosu
üretiyor. Ayrıntı: [ADR 0006](decisions/0006-review-package.md).

**Sende olan iş — inceleme.** İlk paket hazır:
`renders/review/BoxHouse_v3/contact_sheet.png`

Not yazacağın yer: [docs/feedback/box_house.md](feedback/box_house.md). Serbest metin
yeter ("cumba %20 daha derin", "kat alçak", "çatı fazla dik"). Onay formatı: **"OK v3"**.

Bu bir **graybox**tır — doku/pencere/kapı yok ve olmayacak; amacı boru hattını
kanıtlamaktı. Ama oranlar hakkındaki notun Görev 11'deki gerçek Osmanlı evi kitine
taşınacağı için şimdi verilse de değerli.

### Görev 9 durumu — gerçek arazi ✅ (2026-08-18)

İstanbul'un gerçek topoğrafyası Unity'de. Ayrıntı: [ADR 0007](decisions/0007-dem-terrain.md).

| Kanıt | Sonuç |
|---|---|
| Kaynak | Copernicus DEM GLO-30 (kimlik doğrulamasız, COG range-request) |
| İndirme + işleme | 1,6 sn |
| Dünya | 15 338 × 15 338 m, 2049², **7,49 m/örnek** |
| Orijin | Galata Kulesi tabanı = Unity `(0, 0)` |
| Deniz seviyesi | y = 0 (Boğaz ve Haliç tam 0,0 m ölçüldü) |
| Çapraz doğrulama | Python ölçümü ile Unity `SampleHeight` **birebir aynı** |
| Testler | EditMode 55/55 |

Sahne: `Assets/_Project/Scenes/Faz1_Terrain.unity` (FlightSlice kasten ellenmedi).

> ⚠️ **Krediler ekranı borcu:** Copernicus DEM atıf metni zorunlu.
> Tam metin [refs/LICENSES.md](../refs/LICENSES.md)'de.

### Görev 10 durumu — 1632 kıyı çizgisi ✅ (2026-08-18)

Caner kararı: *"makul bir tahminle geri alabilirsin."* Dolgular geri alındı.
Ayrıntı: [ADR 0008](decisions/0008-coastline-1632.md).

| Katman | Ne | Uzunluk |
|---|---|---|
| `modern_shoreline` | bugünkü kıyı (kıyas) | 64,1 km |
| **`shoreline_1632`** | **oyunun kıyısı** (T2) | 65,0 km |
| `correction_zone` | 5 alan, ölçülen kayma değerleriyle | — |

Yöntem: sabit metre ofseti **yok**. Dolgu alanlarında deniz eşiği 0,5 → 5,0 m çıkarılıp
kıyı doğal yamacın eteğine çekildi; kayma **ölçüldü** — Eminönü ~98 m, Unkapanı ~99 m,
Marmara ~49 m, Karaköy ~35 m. Langa dokunulmadı (dolmuş liman zaten karaydı).

Önizleme: `data/gis/istanbul/preview_coastline.png`
(soluk gri = bugün, parlak beyaz = 1632)

### Faz 1 — landmark'lar, deniz ve görülebilir sahne ✅ (2026-08-19)

Ayrıntı: [ADR 0009](decisions/0009-water-and-lighting.md).

| Ne | Durum |
|---|---|
| Landmark kataloğu | 22 öğe (S-kademe 8 + A-kademe 14), `refs/maps/landmarks_1632.geojson` |
| 1632'de olmayanlar | ayrıca listelendi (Revan/Bağdat köşkü, Nuruosmaniye, Büyük Valide Han) |
| Deniz yüzeyi | HDRP Water, `y = 0`, dalga yönü lodosla hizalı |
| Deniz tabanı | −12 m (su derinlik kazandı; öncesinde suyla çakışıyordu) |
| Aydınlatma | güneş 100 000 Lux, `VP_Faz1_Sky` profili (Dynamic ambient) |
| Testler | EditMode 67/67, PlayMode 7/7 |

Kareler: `unity/HezarfenGame/Captures/faz1_bogaz.png` ve `faz1_galata_to_uskudar.png`.

**Landmark kataloğu iki ayrı güven ekseni taşır** — `tier` yapının 1632'deki
*varlığını/durumunu* (RESEARCH.md §3), `position_confidence` ise *koordinat*
kesinliğini niteler. Ayasofya'nın ayakta olduğu belgelidir ama koordinatı ~100 m
yaklaşıktır; tek bir güven alanı bu ikisinden birini yanlış gösterirdi.

> ⚠️ Konumların hepsi `approx`. Kesinleştirme plan Faz 1 madde 3'ün işi
> (dönem haritalarının georeferanslanması).

### HDRP / URP karar kapısı — **HDRP'de kalınıyor** ✅ (2026-08-19)

Plan bu kapıyı "tek yönlü, geç kalma" diye işaretlemişti. Ölçüldü, karar verildi:
[ADR 0010](decisions/0010-hdrp-vs-urp.md).

| Yapılandırma | p95 | 60 fps bütçesinin |
|---|---|---|
| Boş arazi + deniz, 1080p | 5,08 ms | %30 |
| **8 000 yapı, 1080p** | **4,75 ms** | **%28** |
| 8 000 yapı, 1440p | 8,66 ms | %52 |

Tam atmosfer yığınıyla (fiziksel gökyüzü + volümetrik sis + HDRP Water + gölge).
Çizim çağrısı 151 / bütçe 1500. **Performans kısıt değil** ve HDRP Water URP'de yok.

Yeniden üretim: **Hezarfen → Olcum → Benchmark sahnesi kur**, sonra Play (~25 sn).

> Bu ölçüm **içerik ağırlığını kanıtlamaz** — `PF_BoxHouse` 44 üçgen; gerçek kit evi
> 20–50 kat ağır olacak. LOD/impostor/atlas (Faz 4) yine zorunlu.

### Faz 1 madde 3 + 6 — sur hatları, semtler, bölge yayını ✅ (2026-08-19)

Karar kaydı: [ADR 0011](decisions/0011-walls-districts-streaming.md).
İnceleme: `renders/review/Map1632_v1/` → notlar `docs/feedback/walls_districts.md`.

**Surlar** (`refs/maps/walls_1632.geojson`) — üç kanıt sınıfı, üç ayrı yöntem:
kara surları elle izlendi (**5,82 km**, bugün ayakta), deniz surları kendi
`shoreline_1632`mize yapıştırıldı (**7,55 + 5,25 km**), Galata **kaba taslak**
(3,05 km çevre / 53 ha — georeferanslı dönem planı yok). Toplam çevre 18,6 km,
23 kapı. Denetimler: kuru zemin, uzunluk aralığı, kapalı çevre, kule içeride.

**Semtler** (`refs/maps/districts.geojson`) — 8 **oyun bölgesi**. Bunlar tarihsel
mahalle sınırı DEĞİLDİR (1632 mahalleleri kadastral değildi); hepsi `Graybox`.
Öncelik-1 çakışmasız kara **1786 ha** = dikey dilimin içerik bütçesi.
Suriçi Doğu+Batı 1478 ha ≈ gerçek yarımada ~1400 ha (bağımsız çapraz kontrol).
Uçuş koridorunun 38/38 örneği öncelik-1 kapsamasında.

**Yayın iskeleti** — Addressables + semt başına sahne. Yükleme ölçütü poligon
**kenarına** uzaklık (merkez değil), histerezis 700/910 m, aynı anda en fazla
1 yükleme uçuşta. Karar mantığı (`DistrictStreamingPlan`) Addressables'tan ayrı
ve saf; PlayMode testi gerçekten yüklenip boşaltıldığını doğruluyor.

Komutlar:
```powershell
tools\gis\.venv\Scripts\python.exe tools\gis\walls_build.py --dir data\gis\istanbul
tools\gis\.venv\Scripts\python.exe tools\gis\districts_build.py --dir data\gis\istanbul
tools\gis\.venv\Scripts\python.exe tools\gis\map_overlay.py --dir data\gis\istanbul
```
Ardından Unity: **Hezarfen → GIS → Semtleri ice aktar**, sonra
**Hezarfen → GIS → Kiyi + landmark + sur + semtleri sahneye al**.

### 📏 Faz 1 kabulünün açık kalan tek maddesi — uçuş menzili (ÖLÇÜLDÜ, 2026-08-20)

Plan Faz 1 kabulü: *"Gerçek topoğrafyada Galata sırtından kalkıp Üsküdar'a inilebiliyor."*
Caner uçuş tarafını bilerek erteledi ("uçurma kısmını en son yapalım"), o yüzden **ayar
yapılmadı** — ama açığın büyüklüğü ölçüldü, çünkü Faz 6/7'nin rüzgâr tasarımını bu belirler.

Gerçek arazi ve `WT_Faz0_Default` ile:

| Büyüklük | Değer |
|---|---|
| En iyi süzülme oranı | **11,56 : 1** @ α 6,2° (plan hedefi 8–12:1 ✅) |
| Trim hızı / batış | 12,4 m/s / 1,08 m/s |
| Galata Kulesi tepesi (arazi 52 m + gövde ~66 m) | 118 m |
| Doğancılar arazi kotu | 15,3 m → düşüş **103 m** |
| **Rüzgârsız menzil** | **1 187 m** (uçuş süresi ~95 sn) |
| Gereken mesafe | **3 709 m** → gereken oran **36,1 : 1** |
| **Açık** | **2 522 m — mesafenin %68'i** |

**Faz1_Terrain'de şu an 0 adet `WindVolume` ve `WindField` YOK.** Yani uçuş ekseninde
hiçbir rüzgâr desteği kurulmamış durumda; kriter bugün karşılanmıyor ve az farkla değil.

Bu bir sürpriz değil, planın kendi teşhisi: efsanenin rakamları da 39–57:1 gerektiriyor
(RESEARCH.md Caveats) ve plan Bölüm 2 cevabı "rüzgâr sistemleriyle mesafe kapatılır"
diye koymuş. Ölçüm o cümleye **sayı** veriyor: yamaç yükselticisi + su üstü termikleri
uçuşun **üçte ikisini** taşımak zorunda.

Yan doğrulama: Unity arazisi Galata'da 52,0 m, Doğancılar'da 15,3 m okuyor —
`landmarks_1632.geojson`teki DEM türevi değerlerle **birebir** aynı. GIS boru hattı
ile Unity arazisi ayrışmamış.

### Faz 2 — Osmanlı konut kiti: yakın plan kademesi ✅ (2026-08-20)

Karar kaydı: [ADR 0012](decisions/0012-ottoman-kit-materials.md) (kit + malzeme),
[ADR 0013](decisions/0013-near-detail-construction.md) (yakın plan).
İnceleme: `renders/review/House_A_Eye_v3/` ve `House_B_Corner_v1/` →
notlar `docs/feedback/ottoman_house.md`.

Caner'in kararı: *"karakter sokaklarda da gezecek, atmosfer gerçekçi olmalı."*
Kit artık **iki yapım kademesi** üretiyor:

| | `--detail mass` | `--detail near` |
|---|---|---|
| İçin | kalabalık şehir dokusu | yaya seviyesi |
| Duvar | tek kütle | **delikli panel** (gerçek söve derinliği) |
| Üçgen LOD0 | **944** | **1 980** (köşe evi: 4 540) |
| LOD1 / LOD2 | 56 / 20 | **56 / 20 — aynı** |

Son satır önemli: yakın plan detayı **uzak siluete hiçbir şey ödetmiyor**.
Öz-test bunu kilitliyor.

**Blender tarafında artık öz-test var** — 5 test, hepsi geçiyor:
```powershell
& $blender --background --factory-startup --python tools\blender\selftest.py
```
Duvar panelinin su geçirmezliği **açık kenar sayısıyla** (tam sayı) ölçülür.
İlk yazımda hacim karşılaştırmasıydı ve float32 birikme hatası (1,1e-4) yüzünden
kalıyordu. *Tolerans gevşetmek testin dişini söker; ölçüyü değiştirmek doğrusudur.*

> ⚠️ **Tuzak — `recalc_face_normals` kapalı bir kabuğu bütünüyle ters çevirebilir.**
> Blender bunu **göstermez** (arka yüzleri de çizer); Unity arka yüzü eler ve duvar
> orada *görünmez* olur. `make_wall_panel` işaretli hacmi ölçüp gerekirse çevirir
> **ve söyler**.

> ⚠️ **Tuzak — açıklık panel kenarına değemez.** Değen açıklık kabuğu açık kenarlı
> bırakır. Üretim bunu reddeder; kapı bu yüzden taş bir **eşiğin** üstüne oturur
> (zaten doğru mimari).

Yeni araçlar:
```powershell
# Yaya seviyesi inceleme kadrajlari (1,65 m) - yakin plan yargisi icin
& $blender --background --factory-startup --python tools\blender\render_preview.py -- `
    --in art\blend\SM_House_A.blend --asset House_A_Eye --eye --hdri --samples 96

# Render olcumu: once --grid ile kadrajin neresinde ne var diye bak, sonra --rect
& $blender --background --factory-startup --python tools\blender\measure_render.py -- `
    --in renders\review\House_A_Eye_v3\02_sokak_gecis.png --grid 4
```

> `measure_render.py` **sapma** (std) sütunu basar. Ortalama tek başına dokunun
> var olup olmadığını söylemez: düz renk ile dokulu yüzey aynı ortalamayı
> verebilir. Bu sütun bir kez beni kendi yanlış izlenimimden döndürdü — sıvayı
> "düz" sanmıştım, sapma 24,4 çıktı.

### Faz 2 — ev Unity'de: HDRP malzemeleri, LOD, prefab ✅ (2026-08-20)

Karar kaydı: [ADR 0014](decisions/0014-unity-hdrp-materials.md).
Sahne yakalamaları: `unity/HezarfenGame/Captures/faz2_house_*.png`.

Sıra **önemlidir** — malzeme bağlama import anında olur:

```powershell
# 1) Dokulari HDRP duzenine cevir (ARM -> maske, boyali albedo pisir)
& $blender --background --factory-startup --python tools\textures\build_unity_maps.py
# 2) Evi uret + FBX
& $blender --background --factory-startup --python tools\blender\gen_ottoman_house.py -- `
    --asset House_A --textured --detail near --out-fbx unity\HezarfenGame\Assets\_Import\SM_House_A.fbx
```
Sonra Unity'de sırayla: **Hezarfen → Boru Hatti → Osmanli malzemelerini uret**,
ardından **Hezarfen → Boru Hatti → _Import'u yerlestir ve prefab uret**.

| Ölçülen (Unity) | Değer |
|---|---|
| Ayak izi | 8,900 × 8,700 m — Blender'la birebir |
| Pivot | `bounds.min.y = 0` |
| LOD0/1/2 tepe | 8,8453 / 8,7603 / 8,5115 m |
| Cumba asimetrisi | +Z yönünde 0,800 m ("evin önü +Z") |
| Malzeme | 10 adet, hepsi `HDRP/Lit`, hiçbiri gömülü değil |
| **Testler** | **EditMode 95/95, PlayMode 9/9** |

> ⚠️ **Tuzak — `MaterialLocation.External` Unity 6'da KALDIRILDI.** Obsolete
> uyarısı verir ve çalışmaz. Bağlama artık
> `ModelImportPolicy.OnAssignMaterialModel` üzerinden yapılıyor.

> ⚠️ **Tuzak — ARM ≠ MaskMap.** Poly Haven ARM = (AO, Roughness, Metallic);
> HDRP maskesi = (Metallic, AO, Detay, **Smoothness**). Kanallar yer değiştirir
> *ve* pürüzlülük tersine döner. Yapılmazsa uyarı çıkmaz; sadece duvarlar
> metalik olur ve mat yüzeyler parlar.

> ⚠️ **Tuzak — yeni örneklenen nesne aynı çağrıda render edilmez.** Prefab'ı
> örnekleyip hemen `Camera.Render()` çağırınca ev görünmedi, yalnızca gölgesi
> düştü (renderer'lar etkin ve kadrajın ortasındaydı — ölçüldü). Örnekleme ile
> render'ı ayrı çağrılara böl.

### Faz 2 — 20 varyant + Galata sokağı ✅ (2026-08-20)

Karar kaydı: [ADR 0016](decisions/0016-mahalle-dokusu.md).
Araştırma: **RESEARCH.md §4.1** (yeni bölüm — sokak dokusu ve parsel, kaynaklı).
Yakalamalar: `unity/HezarfenGame/Captures/faz2_sokak_*.png`, `faz2_mahalle_ust.png`.

Caner'in itirazı (*"tek bir kusursuz çizgi… doğallık bozuluyormuş gibi"*) doğruydu.
Yerleştirici artık **ızgara değil organik**: sokak ekseni arazinin **eş yükselti
eğrisini** izler, ev cephesi eksene *yerel* olarak dik durur, cephe hattında
düzensizlik vardır, ana sokaktan **çıkmazlar** dallanır.

```powershell
& $blender --background --factory-startup --python tools\blender\gen_house_variants.py
```
Unity: **Boru Hatti → _Import'u yerlestir**, sonra **GIS → Galata sokagi sahnesi kur**.

| Ölçülen | Değer |
|---|---|
| Varyant | **20**, LOD0 ortalama **2 424** üçgen |
| Mahalle | 108 ev, 4 çıkmaz, 102 taş kaide |
| Taş kaide maliyeti | **1 020 üçgen**, tek mesh |
| **Testler** | **EditMode 101/101**, PlayMode 9/9 |

> ⚠️ **Tuzak — eş yükselti izleyen sokak, evleri yamacın EN DİK yönüne oturtur.**
> Sokak yatay gider ama evler ona diktir. Ölçüldü: ayak izi altında medyan
> **3,22 m** kot farkı; 108 evin **89'u** hem havada hem gömülüydü. Çözüm
> tarihsel: ev **en yüksek köşeye** oturur, altındaki boşluk **taş kaideyle**
> dolar (istinat/subasman duvarı). Gömülen ev 89 → **0**.

> ⚠️ **Eğim gerçek mi, DEM gürültüsü mü — ölç, ayır.** İkisi tamamen farklı
> düzeltme ister. Yöntem: eğimi **iki farklı adımda** ölç. 4 m'de %14,2, 20 m'de
> %14,3 çıktı → gerçek arazi. Ayrışsalardı gürültü olurdu ve DEM yumuşatılırdı.

> ⚠️ **Yerleştirmede ayak izi değil `wall_depth` kullan.** Ayak izi saçağı
> içerir; onu sokak çizgisi sanmak evleri yarım metre geri iter ve doku gevşer.
> Saçak ve cumba zaten sokağın **üstüne** taşmalıdır.

### Faz 2b — Kamusal yapı kiti başladı: mescit ✅ (2026-08-20)

Karar kaydı: [ADR 0017](decisions/0017-kamusal-yapi-kiti.md).
Plan eklendi: **PLAN.md §7.1** (kamusal yapı kiti) ve **§8.1** (landmark doğruluk
merdiveni D1/D2/D3 + telif kapısı).
İnceleme: `renders/review/Mescit_A_v3/`, `Cami_Kubbe_v2/`.

```powershell
& $blender --background --factory-startup --python tools\blender\gen_mescit.py -- `
    --asset Mescit_A --textured --roof timber --out-blend art\blend\SM_Mescit_A.blend
# kubbeli orta olcek cami:  --roof dome --hall 12 --wall-h 7 --minaret-h 26
```

| | Mescit_A (ahşap çatı) | Cami_Kubbe |
|---|---|---|
| Ölçü | 11,5 × 13,3 × 19,9 m | 14,9 × 16,2 × 26,9 m |
| Üçgen LOD0/1/2 | 1 920 / 50 / 36 | 2 298 / 150 / 68 |

> ⚠️ **Mahalle mescidinin varsayılan çatısı AHŞAP'tır, kubbe değil.** Kurşun
> kubbe vakıflı büyük caminin işaretidir. Bu tipolojik doğru aynı zamanda
> mahalleyi bütünlüklü yapar: mescit, komşusu evlerle aynı kiremidi taşır.

> ⚠️ **Eğri yüzeyleri yumuşak gölgelendir, kasnağı DEĞİL.** Kubbe düz
> gölgelendirmede fazetli çıkar. Ama sekizgen kasnak ve pabuç bilerek düzdür;
> yumuşatılırsa köşeleri erir ve kubbeyle tek şişman kütle olur.

> ⚠️ **Kapalıçarşı'nın bugünkü kâgir hâli 1894 SONRASI.** 1632'de bedestenler
> kâgir, çevresi **ahşap**tı. Bugünkü çarşıyı modellemek yapılabilecek en büyük
> tarihsel hatalardan biri olur. (PLAN.md §8.1)

> ⚠️ **Kurşun dokusu YOK.** Poly Haven'ın 25 metal dokusunun hepsi paslı sac;
> kurşun paslanmaz. Kubbe ve külah şu an dokusuz PBR. Bilinen boşluk.

**Sokak donatısı** (çeşme + dükkân) ve **mahalle çekirdeği** de eklendi:

```powershell
& $blender --background --factory-startup --python tools\blender\gen_street_kit.py
```
Unity: **Boru Hatti → Osmanli malzemelerini uret** → **_Import'u yerlestir** →
**GIS → Galata sokagi sahnesi kur**.

| Ölçülen | Değer |
|---|---|
| Donatı | 3 çeşme + 3 dükkân, 132-268 üçgen |
| Mahalle | 89 ev, **6 çekirdek yapısı**, 3 çıkmaz, 84 taş kaide |
| Testler | EditMode **101/101** |

> ⚠️ **Çekirdek evlerden ÖNCE yerleşir.** Mahalle mescitten dallanır
> (RESEARCH.md §4.1(g)); mescit/çeşme/dükkân yerlerini önce rezerve eder.
> Sonra konsalardı ya evlerin arasına sıkışırlardı ya da ev yerleştirmeyi
> geriye dönük bozmak gerekirdi.

> ⚠️ **Yeni doku rolü: kesme taş (M_Stone_Cut).** Moloz taş TAŞIYICI duvar
> içindir. Çeşmenin ayna taşı ve kitabesi oyma taştır; moloz doku oraya konunca
> yapı "duvar parçası" gibi okunur, eser gibi değil.

**Avlu, şadırvan ve çekirdek konumu** (üçüncü tur): mescit avlusu artık
teras + merdiven + duvar halkası + kemerli kapı + şadırvan olarak kuruluyor.
Yakalamalar: `Captures/faz2_avlu_ust.png`.

> ⚠️ **Mahalle çekirdeği sokağın EN DÜZ yerine kurulur.** Sabit bir noktaya
> konunca dik yamaca denk geldi: teras 5,8 m yükseldi ve mahalle kale gibi
> göründü. Teras doğru çalışıyordu, **yanlış olan yerleştirme kuralıydı**.
> Meydan düz zemin ister; ev tek başına yamaca oturabilir, meydan oturamaz.
> 20 aday nokta taranıyor → kot farkı **5,8 m → 1,02 m**.

> ⚠️ **n-gon kapak = sessiz normal hatası.** Sekizgen bir kapak tek yüz olarak
> yazılınca FBX "4'ten fazla köşeli yüz, teğet uzayı hesaplanamıyor" der ve
> normal haritası o yüzde yanlış okunur. `make_tube` kapakları artık üçgen
> yelpaze.

### 📏 Atlas kararı — ÖLÇÜLDÜ (2026-08-20)

Karar kaydı: [ADR 0015](decisions/0015-atlas-olculdu.md) — **plandan sapma,
Caner onayı bekliyor.**

Gerçek kit eviyle (`PF_House_A`, 6 malzeme) 8 000 + 400 yapı, iki kadraj:

| Sokak seviyesi, 1080p | boş | 8 000 + 400 ev |
|---|---|---|
| medyan | 4,83 ms | **5,39 ms** |
| p95 | 7,48 ms | **6,85 ms** — 60 fps bütçesinin %41'i |
| çizim / SRP Batcher | 281 / 0 | 2 958 / **2 677** |
| **setPass** | 31 | **43** |
| üçgen | 0,29 M | 0,40 M |

**Bulgu:** 8 400 ev × 6 malzeme, naif beklentiyle on binlerce bağlama; ölçülen
fark **+12 setPass**. 10 malzemenin hepsi aynı shader'ı (`HDRP/Lit`) kullandığı
için SRP Batcher malzeme değişimini neredeyse bedava yapıyor. Ev çizimlerinin
**tamamı** SRP Batcher'a düştü.

> ⚠️ Karar bugünün yapılandırmasına ait. Farklı shader gerekirse (cam, bitki,
> saydam kafes), ev başına malzeme artarsa, VRAM sıkışırsa ya da hedef donanım
> düşerse **yeniden ölç**. SRP Batcher bir CPU kazancıdır.

Yerine yapılan: **doku tekilleştirme** — maske ve normal kaynak dokuya aittir,
role değil. `weathered_planks` dört rolde kullanıldığı için aynı 2K maske dört
kez yazılıyormuş. **27 dosya / 271,5 MiB → 21 dosya / 188,2 MiB (−%31).**
Bir evin çalışma zamanı maliyeti: 13 benzersiz doku, **109,8 MiB VRAM**.

> ⚠️ **Tuzak — malzeme adı değişirse FBX'i YENİDEN İHRAÇ et.** FBX, Blender'daki
> malzeme adlarını taşır. Ad değişip FBX yenilenmezse model eski ada bağlı kalır;
> eski ad hâlâ var olan **başka** bir malzemeyi gösteriyorsa hata tamamen
> sessizdir — "malzeme bulundu, HDRP, maskesi var" testlerinin hepsi geçer ama
> ev yanlış boyayı giyer. Bu yaşandı. Artık
> `OttomanHouseTests.House_UsesExactlyTheDefaultPalette` LOD0'ın malzeme
> **kümesini** paletle karşılaştırıyor.

Yeniden üretim: **Hezarfen → Olcum → Benchmark sahnesi kur (Osmanli evi)**, Play (~90 sn).

> ⚠️ **Stale test tuzağı — sahneye yeni katman girdiğinde eski testler patlar.**
> `CoastlinePipelineTests` "landmark dışındaki her şey çizgidir" varsayıyordu;
> sur kapıları (ADR 0011) sahneye girince patladı çünkü kapı da bir noktadır.
> Test artık nokta katmanlarını açıkça listeliyor ve **tek nokta olduklarını da**
> doğruluyor.

### Faz 2b — kilise ve sinagog ✅ (2026-08-20)

Üç tip, altı varlık: `Kilise_Latin_A/B` (Galata, çan kuleli), `Kilise_Rum_A/B`
(suriçi, kulesiz ve alçak), `Sinagog_A/B` (Balat, kemersiz). Gerekçe ADR 0018,
kaynaklar RESEARCH.md §4.2. Üretim:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\gen_church_kit.py --
```

> ⚠️ **Tuzak — beşik çatıyı dolu prizma olarak üretme.** Prizmanın iki ucu düşey
> üçgendir ve **kiremit** malzemesi alır: cephe kiremitten bir alınlıkla çıkar.
> Beşik çatının alınlığı **duvardır**, çatı onun üstünden aşar. `church_kit`te
> ikiye ayrıldı: `_gable_wall` (kâgir üçgen) + `gable_roof` (iki eğimli levha).

> ⚠️ **Tuzak — `make_wall_panel` sütun başına TEK açıklık varsayıyordu.** İki
> katlı bir cephede pencereler üst üste hizalanır (kâgir yapıda kural budur).
> Eski kod aynı yüzeyi iki kez yazıyordu ve Blender `faces.new(): face already
> exists` ile reddediyordu. Artık açıklıklar sütunlara gruplanıyor; kısmî
> çakışma ve dikey çakışma açık hatayla reddediliyor.

> ⚠️ **Tuzak — kemer tepesi `top` seviyesinde kendi üstüne kapanır.** Kemer
> şeridini tepe kotunda düz kesince apsisteki dörtgen dejenere olur (aynı köşe
> iki kez). Blender reddeder; etseydi de sıfır alanlı yüz olarak FBX'e sızardı.
> `arched_panel` orada **üçgen** yazıyor.

> ⚠️ **Tuzak — yapı yerleşimi "sokağa dön" kuralına körü körüne uymaz.** Sokak
> eş yükselti eğrisini izler; sokağa **dik** yön yamacın **en dik** yönüdür.
> 29 m derinliğindeki bazilika oraya dikilince 5,60 m kot farkına oturdu.
> Kilisenin yönü mimarîden gelir (**apsis doğuya**), topoğrafyadan değil.
> Ayrıntı ve üç yanlış denemenin tamamı ADR 0018 §3.4'te.

> ⚠️ **Tuzak — HistoricalTag prefaba ELLE konulamaz.** `ImportLanding` prefabı
> her koşuşta sıfırdan yazar; el yazısı ilk yeniden üretimde sessizce kaybolur.
> Bu yüzden 40 prefabın hepsi `Graybox` kalmıştı. Kademe artık
> `art/blend/**/catalog.json` içinde durur, Unity `AssetCatalog` ile okur.
> Yeni bir jeneratör yazarken **kataloğa `tier` ve `source` yazmayı unutma** —
> unutulursa prefab Graybox kalır ve boru hattı uyarı basar.
> `AssetPipelineTests.CataloguedPrefabs_CarryTheirHistoricalTier` kilitler.

Sahne: **Hezarfen → GIS → Galata sokagi sahnesi kur** artık Latin kilisesini de
yerleştiriyor (Galata tek cemaatli değildir). Sinagog **yerleşmiyor** — Balat'a
aittir, Galata'ya değil; Balat sahnesi henüz yok.

### Faz 2b — Balat: semt bir parametre oldu ✅ (2026-08-20)

`OttomanStreetBuilder` artık **`QuarterSpec`** alıyor. Doku kuralları her semtte
aynı; değişen şey *kimin oturduğu*: çekirdek yapı, ibadet yapıları, ev paleti.

```
Hezarfen → GIS → Galata sokagi sahnesi kur     (mescit + Latin kilisesi, 87 ev)
Hezarfen → GIS → Balat sokagi sahnesi kur      (avlulu sinagog + Rum kilisesi, 98 ev)
```

> ⚠️ **Tuzak — semt parametreleştirilirken KAYDETME YOLUNU da parametreleştir.**
> `Build` semte göre ayrıldı ama `SaveScene` hâlâ `ScenePath` sabitini
> kullanıyordu: Balat kurulunca **Galata sahnesi silindi**. Kurucu kod hiç hata
> vermedi, log bile doğru göründü; yakalayan şey `OttomanStreetTests`'in beş
> testi oldu (*"MAHALLE_Galata yok"*).

> ⚠️ **Tuzak — malzeme üreticisi eksik dokuyu SESSİZCE geçiyordu.** Çatı boyası
> değişince doku yeni adla yazıldı, malzeme onu bulamadı, `_BaseColorMap` NULL
> kaldı ve konsol *"11 malzeme üretildi, 1 uyarı"* dedi. Sonuç: **bembeyaz
> Balat**. İki kör nokta kapatıldı: denetim artık albedo ve normali de arıyor
> (en görünür harita denetlenmeyen tek haritaydı) ve sorunlar `LogError`.
> Testler de tek bir evi geziyordu; `EveryOttomanMaterial_CarriesAllThreeMaps`
> malzeme klasörünün tamamını gezer.

> ⚠️ **Tuzak — doku boyasını AYDINLATMALI RENDER üstünden ayarlama.** Blender
> render'ı R/G 1,65 gösteriyordu, Unity'ye pişirilen dokunun gerçeği 2,78'di.
> Ölçü **dokunun kendisi** olmalı:
> `measure_render.py --in <...>/T_*_BC.png --rect 0.2 0.2 0.8 0.8`.
> Blender öz-testi artık iki paletin çatısını karşılaştırıyor.

### Faz 2b — servi, çınar, hazire ✅ (2026-08-20)

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2lender.exe" --background --factory-startup `
    --python toolslender\gen_nature_kit.py --
```

Servi 3 boy, çınar 2 boy, mezar taşı 3 tip. Gerekçe ADR 0019.

> ⚠️ **Tuzak — `spread` YARIÇAPtır, çap değil.** 13 m'lik servi 3,7 m genişlikte
> çıktı (boy/en 3,5): o bir servi değil, kavaktır. Sütunsu servide oran 6-10
> arasıdır. Ağaç oranı gözle "olmuş" görünebilir; ölç.

> ⚠️ **Tuzak — `taken` "burası doludur" DEMEZ, "buraya EV konmasın" der.**
> Avlu ağaçları ve hazire ilk denemede hiç yerleşmedi ve hiçbir uyarı çıkmadı:
> çekirdek kendi çevresini `depth×0,75+2` yarıçapla rezerve ediyor, ağaç da
> `Overlaps(taken, …)` ile sınanıyordu — yani kendi avlusuna çarpıyordu.
> Çekirdeğin PARÇALARI o kuralın istisnasıdır; ölçüt rezervasyon değil
> **yapının gövdesi** olmalı. Aynı tuzağa üst üste iki kez düştüm.

> ⚠️ **Tuzak — "yerleşemedi" sessiz kalmasın.** Balat susuz kurulmuştu:
> çeşmenin tek aday noktası elendi, `PlaceProp` 0 döndü, kimse bakmadı.
> Çekirdeğin zorunlu parçaları (çeşme, hazire) artık birden çok konum dener ve
> hepsi elenirse `LogWarning` basar.

### Faz 2b — sokak kaldırımı + prosedürel yaprak dokusu ✅ (2026-08-21)

```powershell
# yaprak dokusu (kendi telifimiz — indirilmiyor, URETILIYOR)
python tools	extures\gen_foliage_texture.py --res 1024
# kabuk + kaldirim dokusu
python tools	exturesetch_polyhaven.py --res 2k --only paving bark bark_cinar
```
Sonra: **Hezarfen → Boru Hatti → Osmanli malzemelerini uret**, ardından sahneler.

> ⚠️ **Doku Poly Haven'da yoksa ÜRETİLEBİLİR.** Yaprak alfa atlası yok diye
> "dokusuz PBR" olarak bırakmıştım — kısıtın etrafından dolaşmak yerine
> kaldırmak mümkündü. Prosedürel çıktı bizim eserimizdir, `refs/LICENSES.md`de
> kayıtlıdır. Dosya düzeni Poly Haven'la aynı tutulursa boru hattı özel durum
> bilmez; roller artık `root=` ile kendi köklerini söyleyebiliyor.

> ⚠️ **Kaldırım merdiveni "eklenmez", eğimden DOĞAR.** Yürünen yüzey düz olmak
> zorundadır: kot farkı bir rıht (0,17 m) biriktiğinde bir basamak atılır.
> Düz yerde hiç basamak çıkmaz. Galata 67, Balat 76 basamak.

> ⚠️ **Sokak seviyesi KAPKARANLIK — ölçüldü: 3/255.** Sebep kaldırım değil,
> aydınlatma: güneş 42° yükseklikte ama sahnede **dolaylı aydınlatma yok**
> (GI pişirilmemiş). Malzeme, doku ve UV'lerin hepsi doğrulandı. Aydınlatma
> fazına ait bir iş; "kaldırım dokusuz" diye yanlış yerde arama.

### Faz 2b — hamam ve han ✅ (2026-08-21)

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2lender.exe" --background --factory-startup `
    --python toolslender\gen_civic_kit.py --
```

> ⚠️ **Tuzak — panel deliyor, KÜTLE KAPATIYOR.** Hamamın ön cephesi delikli
> panel olarak kuruldu ama kâgir kutu olduğu gibi bırakıldı: açıklık gerçekti,
> arkası doluydu, render'da "kapı yok" diye okundu. Delikli panel ile arkasındaki
> kütle **aynı hacmi paylaşamaz** (`_domed_hall(front_gap=…)`).

> ⚠️ **Tuzak — avlulu yapıda silme/dam HALKA olmalı, plaka değil.** Hanın kat
> silmesi ve damı tam plakaydı ve avlunun üstünden geçti: han ambara döndü.
> Üstten bakmadan fark edilmiyordu.

> ⚠️ **Tuzak — sabit kapı ölçüsü.** Taçkapı basma kotu 3,60 m yazılıydı; tek
> katlı han üretilirken kemer tepesi duvarı aştı. Sivri kemerli kapı yüksek yer
> ister: `h ≥ (0,652·w + 0,45)/0,38`. 2,80 m kapı en az 5,99 m ister — fıkıhtaki
> "yüklü deve geçebilmeli" ölçütünün han kapısındaki karşılığı.

> ⚠️ **Tuzak — muafiyet listesi ELLE tutulmaz.** "Her malzemenin üç haritası
> olmalı" testi, bilerek dokusuz olan kurşun ve camı düşürdü. Muafiyet artık
> bildirimden geliyor (`PbrMaterialNames()`, `kind == "pbr"`); elle yazılan
> liste zamanla mutlaka yalancı olur.

> ⚠️ **Tuzak — yapı türü, yerleştirme kuralını belirler.** Hanı "çekirdekten
> uzak dur" kuralıyla koydum, 46,8 m uzağa düştü. Han konut mahallesinin değil
> **çarşının** yapısıdır: `nearCore` bayrağı kuralı tersine çevirir.

### Faz 2b — kurşun dokusu, kubbe UV'si, türbe/mektep/kahvehane ✅ (2026-08-21)

```powershell
python tools\textures\gen_lead_texture.py
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\gen_mahalle_kit.py --
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\textures\build_unity_maps.py
```

> ⚠️ **Tuzak — eğri yüzeyde yüze hizalı UV KOPAR.** Kurşunun dikiş ızgarası
> kubbeye giydiğinde kubbe "kırılmış fayans" çıktı. Sebep geometri değil UV:
> `uv_project` her yüzü kendi teğet düzlemine yansıtıyor. Eğri yüzeyler artık
> UV'lerini kendileri kuruyor (`hz_blender.UV_METRIC`, birim **metre**);
> `uv_project` işaretli yüzü yeniden yansıtmaz, yalnızca doku ölçüsüne böler.
> Öz-test: `t_dome_uv_continuous`.

> ⚠️ **Tuzak — bmesh'e sonradan katman eklemek referansları öldürür.**
> `ReferenceError: BMFace has been removed`. `hz.metric_layers(bm)` **yüzler
> kurulmadan önce** çağrılmalı.

> ⚠️ **Tuzak — HDRP `_Metallic` maskeyi ÇARPAR.** Kitte metal yokken `0f`
> yazmak doğruydu; kurşun gelince aynı satır maskeyi tamamen etkisiz bıraktı ve
> hiçbir uyarı çıkmadı. Çarpan artık bildirimden geliyor ve `Verify` geri okuyup
> karşılaştırıyor.

> ⚠️ **Tuzak — albedo uzantısı SABİT yazılmıştı.** `build_unity_maps.py`
> boyasız albedoları `T_<ad>_BC.jpg` diye kopyalıyordu; prosedürel dokular PNG
> gelince dosyalar ".jpg" adıyla PNG içeriği taşıdı. Unity uzantıya bakar.

> ⚠️ **Tuzak — sütun ayağı ÖRTMEZSE görünmez.** Han revağında ayak 0,44 m,
> sütun çapı 0,40 m'ydi: silindir düz ayak yüzünün arkasında kaldı. İlişki artık
> `civic_kit`te yazılı: `2·COL_R > PIER_W` ve `2·COL_R > REVAK_T`.

> ⚠️ **Tuzak — `n_axis` yönü taşırken uzaklığa bir de işaret çarpma.**
> Kahvehanenin iki çatı levhası aynı tarafa düştü, biri yok oldu. `_shed` imzası
> artık uzaklık–kot **çiftleri** alıyor; hata imzada imkânsız.

> ⚠️ **Tuzak — ağaç yapı değildir.** "Hiçbir köşe arazinin altında kalmasın"
> testi çınarı düşürdü; kural kaidesi olan **yapılar** içindir. Muafiyet ad
> listesiyle değil katalogdan (`AssetCatalog.IsBuilding`, `kind` alanı); test
> ayrıca kaç varlığın muaf tutulduğunu da sınıyor.

> 📏 **Ölçüldü — sokakta ışık hâlâ yok.** Gölgedeki sıva duvar ~30/255,
> kaldırım 3/255. Yaya seviyesinden inceleme paketi ÜRETİLEMİYOR: güneşli
> yüzeyler patlıyor, gölgedekiler siyaha düşüyor. Aydınlatma fazının ilk işi
> (ADR 0019 §11, ADR 0021 §7).

### Faz 2b — medrese, sebil, fırın ✅ (2026-08-21)

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\gen_civic_kit.py --     # medrese
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\gen_mahalle_kit.py --   # sebil, firin
```

> ⚠️ **Tuzak — kısıt reddettiğinde, kısıtın hangi NESNEYE ait olduğunu sor.**
> Han'ın "sivri kemerli kapı yüksek yer ister" kuralı medreseyi reddetti
> (tek katlı, 3,90 m). Doğru cevap kapıyı daraltmak değil, kapıya kendi
> kütlesini vermekti: taçkapı öne taşar ve **damı aşar** — adının "tâc" olma
> sebebi. Kısıt artık `MedreseParams.portal_block`a uygulanıyor.

> ⚠️ **Tuzak — yön, parçanın KENDİSİNDEN gelmeli.** Medrese bacaları hep +X'e
> kaydırılıyordu; sağ sıradakiler duvarın dışına taştı ve dam bir çit gibi
> okundu. Her hücre artık kendi dış duvarının yönünü taşıyor.

> ⚠️ **Tuzak — sabit kemer basma kotu (ikinci kez).** Fırının cephe kemeri
> 2,20 m'den basıyordu ve 2,20 m'lik açıklıkta duvarı aştı. `FirinParams.
> arch_spring` artık genişlikten türetiliyor: `rise = 0,652·w`.

> ⚠️ **Tuzak — yağmur alan yüzey kubbeyle aynı malzemedendir.** Sebilin
> saçağının üstü ahşap (`trim`) yapılmıştı; render'da yapının tepesinde kırmızı
> bir tabak gibi duruyordu. Üstü kurşun, altı ahşap; 0,95 m'lik çıkma ahşap
> konsollara oturur.

> ⚠️ **Tuzak — sessiz başarı.** Sebil yerleşti ama hiçbir şey loglanmadı;
> "60 çekirdek yapısı" sayısı değişmediği için yerleşmediğini sandım. Başarı
> da başarısızlık kadar loglanmalı.

### Faz 2b — GEÇİCİ aydınlatma ✅ (2026-08-21)

Unity menüsü: **Hezarfen → Aydınlatma → Geçici aydınlatmayı kur / kaldır /
Sokak parlaklığını ölç**. Takım `Faz1_Terrain`'de yaşar; iki mahalle sahnesi
yeniden kurulunca oraya taşınır.

> ⚠️ **GEÇİCİDİR.** Faz 7'nin ilk işi `Geçici aydınlatmayı kaldır`. Fizikî
> değil: eksik **sıçrama** terimini gök çarpanı + iki gölgesiz dolgu ışığıyla
> taklit eder, pozu 14,5 → 13,0 EV çeker. Gerekçe ve ölçümler ADR 0023.

> ⚠️ **Tuzak — `VolumeProfile.Add<T>()` diske YAZMAZ.** Bileşen yalnız bellekte
> kurulur; kalıcı olması için `AssetDatabase.AddObjectToAsset` şart. Yapılmayınca
> profil diskte **bomboş** kaydedildi, sahne yeniden açılınca poz ve dolaylı
> çarpan kayboldu — ama ışıklar durduğu için sahne "aydınlatılmış" görünüyordu.
> `HistoricalTag` ile aynı aile: *oturumda çalışan, yeniden yüklendiğinde
> sessizce kaybolan durum.*

> ⚠️ **Tuzak — sahne diskten açılınca Volume'lar henüz kayıtlı değil.**
> Bir güncelleme tıkı geçmeden yapılan `Camera.Render()` o sahnenin
> Volume'larını görmüyor: aynı sahne 18,8/255 ve 73,2/255 ölçüldü. Ölçüm artık
> render'dan önce Volume'ları yeniden kaydediyor + 8 ısınma karesi çiziyor.

> ⚠️ **Tuzak — render ölçen test EK (additive) sahne açamaz.** İki sahnenin
> gökyüzü aynı öncelikte yarışıyor; aynı sokak %12 yerine %52 okunmaz çıktı.
> Geometri testleri etkilenmez, render testi yalan söyler.

> 📏 **Ölçü parlaklık DEĞİL, ayrıntı.** "30/255 altındaki piksel oranı" yanlış
> aletti: Balat'ın paleti bilerek koyu ve okunabilir bir cephe %56 "okunmaz"
> çıkıyordu. Ölçü artık ayrıntı enerjisi (3×3 komşu ortalamasından sapma) —
> palete kör, ışığa duyarlı. Takımsız 0,5 → takımla 2,2 (iki semtte de).

### Faz 1 geri dönüşü — arazi örtüsü ✅ (2026-08-21)

Arazi Faz 1'de doğru ölçekte gelmişti ama **hiç doku katmanı yoktu**; zemin tek
düz bir yüzeydi. Işık gelene kadar görünmedi (ADR 0023 → **ADR 0024**).

```powershell
# 1) Dort prosedurel arazi dokusu (kendi telifimiz) + bildirim
python tools\textures\gen_terrain_textures.py --res 1024
#    -> Assets\_Project\Art\Textures\Terrain\  + terrain_layers.json
#    Cikis kodu 1 ise doku esikleri tutmamis demektir; log hangisini soyler.

# 2) Unity: katmanlari kur, splatmap'i boya
#    Hezarfen -> GIS -> Arazi ortusunu kur
# 3) Inceleme paketi (dort mesafe) -> Captures\faz1_arazi_*.png
#    Hezarfen -> GIS -> Arazi ortusu inceleme paketi
```

> ⚠️ **Tuzak — `TerrainData.terrainLayers` ataması splatmap'i SIFIRLAR.**
> Öncesi/sonrası ölçmek için katmanları boşaltıp geri koydum; katmanlar döndü,
> **örtü dönmedi**. Arazi baştan sona tek katman kaldı, sonraki inceleme paketi
> o bozuk hâli gösterdi ve bir süre kuralı suçladım. Katman dizisine
> dokunulduysa **"Arazi ortusunu kur"u yeniden çalıştır.**

> ⚠️ **Tuzak — eğim eşiği SABİT AÇI olarak yazılamaz.** "Kaya 26° üstünde
> başlar" dedim, kaya %0,0 çıktı: 7,49 m örnekli bir DEM'de eğim 15 m tabanla
> ölçülür ve karanın %99'u 24°'nin altında kalır. Eşikler arazinin kendi
> yüzdeliklerinden türetiliyor (`SlopeQuantiles`, yalnız kara üstünden —
> Boğaz kenarı DEM'in en dik yeridir ve yüzdelikleri yukarı çeker).

> ⚠️ **Tuzak — `TerrainLit` maskesinde B kanalı YÜKSEKLİKTİR**, `Lit`teki
> gibi "detay maskesi" değil. `build_unity_maps.py`'nin `build_mask`'ı burada
> kullanılamaz: B'ye 0 yazılsaydı yükseklik harmanı açıkken hiçbir katman
> diğerini yenemezdi.

> 📏 **Ölçü aleti üç kez değişti, üçünde de aynı ders.** (a) "20 cm komşu
> ortalamasından sapma" ince taneyi de sayıyordu — önce bloklara indirgemek
> gerekiyordu; (b) tek bir "ince/kaba oranı" iki ayrı derdi tek sayıya
> sıkıştırıyordu; (c) parlaklık ölçütü **renk** kusurunu göremedi (yeşil maki
> lekeleri karo ızgarası gibi tekrar ediyordu, iki ölçüt de bunu geçirdi).
> Üçüncüsü Lab'da kuruldu. **Bir bandı ölçmeyen alet o bandın kusurunu görmez.**

> 📏 **Palet ayrımı da ölçülür.** İlk üretimde dört katmanın da ton açısı
> 32°–43° arasındaydı: yakından dördü de doğru, **havadan manzara tek renk bir
> çöl**. Uzakta doku mip ortalamasına iner, geriye yalnız ortalama renk kalır.
> Eşik: her çift için CIE76 ΔE ≥ 12.

### Mevsim ilkbahar + güneş hesaptan ✅ (2026-08-21)

Caner mevsimi ilkbahar seçti (ADR 0025). Palet yeniden üretildi; uygularken
**güneşin imkânsız bir yerde olduğu** ortaya çıktı ve düzeltildi.

```powershell
python tools\textures\gen_terrain_textures.py --res 1024   # ilkbahar paleti
# Unity:
#   Hezarfen -> Aydinlatma -> Gunesi tarihten yerlestir
#   Hezarfen -> Aydinlatma -> Gecici aydinlatmayi kur      (dolgular gunesten turer)
#   Hezarfen -> GIS -> Arazi ortusunu kur
#   ...sonra iki mahalle sahnesini yeniden kur
```

> ⚠️ **Tuzak — güneş kuzeye konabiliyor ve kimse fark etmiyor.** Sahnedeki
> ışık 205°'ye doğru yol alıyordu, yani güneş 25° azimutta (kuzeykuzeydoğu).
> 41° kuzeyde güneş oraya **hiçbir gün, hiçbir saat gelmez**. Yükseklik makuldü,
> gölgeler bir yöne düşüyordu, kare makul görünüyordu. Güneş artık tarih ve
> saatten hesaplanıyor (`SunPlacement`) ve `LightingTests` bunu kilitliyor.

> 📏 **Saat öğleden sonra, çünkü uçuş DOĞUYA.** Aynı yüksekliği sabah 09:00 da
> verir ama o zaman güneş bütün uçuş boyunca oyuncunun gözüne gelir. Ölçüldü —
> gölgedeki cephe: imkânsız güneş 2,29 · sabah 1,92 · **öğleden sonra 3,84**.

> ⚠️ **Tuzak — katman adı değişince eski `.terrainlayer` kırık kalır.**
> `DryGrass` → `Grass` olunca eski varlık klasörde kaldı, dokuları silinmişti.
> Sessizdi; bir gün biri onu araziye sürüklerse arazi mor olur. Hem üretici hem
> test artık artıkları yakalıyor.

### Faz 1c — yeşil doku ✅ (2026-08-21)

```powershell
# 1) Yesil alan poligonlari (kendi cizimimiz) -> refs/maps + data/gis
tools\gis\.venv\Scripts\python.exe tools\gis\greenery_build.py --dir data\gis\istanbul
# 2) Unity:
#   Hezarfen -> GIS -> Yesil dokuyu dik
#   Hezarfen -> GIS -> Yesil doku inceleme paketi   -> Captures\yesil_*.png
#   Hezarfen -> Olcum -> Agac maliyetini olc
#   ...sonra iki mahalle sahnesini yeniden kur
```

> ✅ **~~`[İNSAN]` — rasterio bu makinede yüklenemiyor~~ — 2026-08-23'te
> ÇÖZÜLDÜ (ADR 0029 §6).**
> Engel duruyor (`ImportError: DLL load failed … An Application Control
> policy has blocked this file` — Windows uygulama denetimi, kodla ilgisi
> yok) ama artık hiçbir üretim aracı rasterio'ya bağlı değil.
> `tools/gis/geodesy.py` ileri **ve ters** UTM dönüşümünü, DEM okumayı ve
> ızgara dönüşümünü bağımsız yapıyor; doğruluğu boru hattının kendi kaydına
> karşı sınanıyor (ileri **0,05 mm**, ters kapanma **0,29 mm**).
> `coastline_build`, `walls_build`, `districts_build`, `landmarks_build` ve
> `dem_probe` beşi de koşuyor.
>
> **Kalan `[İNSAN]` iş:** yalnız `dem_fetch` (COG indirme — gerçekten
> rasterio ister) ve `map_overlay` (raster PNG yazma). İkisi de DEM zaten
> indirilmiş olduğu için günlük akışta gerekmiyor.

> ⚠️ **Tuzak — prefab ağaçlar billboard mesafesinin ötesinde KAYBOLUR.**
> Unity billboard'ı yalnız SpeedTree ve Tree Creator varlıkları için üretir;
> LOD Group'lu normal prefablar billboard'a geçmez, **hiç çizilmez**.
> `treeBillboardDistance` 160 m'de bırakılmıştı ve 400 m'den alınan karede tek
> ağaç görünmüyordu. Mesafe `treeDistance`e eşitlendi.

> ⚠️ **Poligon kenarı düz çizgi olarak okunur.** Karacaahmet ilk üretimde
> düzgün bir altıgendi. Kenar 80 m'lik bir bantta **gürültüyle** seyreltiliyor
> — düz bir gradyan bu sefer yumuşak bir altıgen verirdi.

> 📏 **Ağaç maliyeti ÖLÇÜLEMEDİ ve alet bunu söylüyor.** Editör render'ı
> kararlı bir ölçüm ortamı değil: saçılma (±13–16 ms) farkın (~1 ms) on katı.
> İlk iki denemede ağaçlı kare ağaçsızdan *hızlı* çıkmıştı. Ölçüm dönüşümlü
> hâle getirildi ve fark saçılmanın altındaysa sayı yerine **"OLCULEMEDI"**
> yazıyor. Gerçek FPS yargısı bir oyuncu yapısı ister.

### Faz 2b — üretim ve su yapıları: kalan yedi ✅ (2026-08-23)

```powershell
& "$blender" --background --python tools/blender/gen_works_kit.py
& "$blender" --background --factory-startup --python tools/blender/gen_mescit.py -- `
    --asset Cami_Orta --textured --roof dome --hall 13.0 --wall-h 8.2 `
    --portico-depth 4.2 --portico-bays 5 --minaret-h 27.0 --wall-thickness 0.85 `
    --out-blend art/blend/mosque/SM_Cami_Orta.blend `
    --out-fbx  unity/HezarfenGame/Assets/_Import/SM_Cami_Orta.fbx
# Unity: Hezarfen -> Boru Hatti -> _Import'u yerlestir ve prefab uret
```

> ⚠️ **Tuzak — `join` nesne DÖNÜŞÜMÜNÜ yok sayıyordu.** `hz.join` parçaları
> `bm.from_mesh(obj.data)` ile okur; nesnenin `location`/`rotation_euler`ini
> görmez. "Döndür, sonra birleştir" sessizce çalışmıyordu ve arasta tonozunun
> on parçası üst üste yığıldı. Artık `matrix_basis` birim değilse mesh'in bir
> kopyası dönüştürülüp okunuyor — birim olduğunda hiçbir şey değişmez, yani
> onaylanmış varlıklar bit bit aynı. (`matrix_world` değil `matrix_basis`:
> ilki depsgraph çevrimi ister.)

> ⚠️ **Tuzak — kemer başlangıcı SABİT SAYI olmamalı.** `arched_panel` iki kez
> "kemer tepesi duvarı aşıyor" diye reddetti (arasta 3,75/3,60 · bozahane
> 4,60/3,30). Açıklık genişliği değişince kemer de değişir; `spring_z` artık
> `wall_h`den türüyor ve `validate()` üretim anında hata veriyor.

> 📏 **`make_tube` çemberi DOLDURUR.** `cap_top` varsayılanı kapalı; değirmen
> çarkının çemberi bu yüzden dolu bir disk çıktı. Halka isteyen
> `cap_top=False` vermeli.

### Faz 2b — Okmeydanı: namazgâh, tekke, menzil taşı ✅ (2026-08-22)

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2lender.exe" --background `
    --factory-startup --python toolslender\gen_okmeydani_kit.py
# Unity: Hezarfen -> Boru Hatti -> _Import'u yerlestir ve prefab uret
#        Hezarfen -> GIS -> Okmeydani sahnesi kur
```

> ⚠️ **Tuzak — poligonun kenarı YARIÇAPTAN bulunamaz.** Tekke
> `merkez + yarıçap × 0,86` ile konuyordu ve alanın **içinde** kaldı:
> `radius_m` çevrel dairenin yarıçapıdır, çokgenin kenarı merkeze çok daha
> yakın olabilir. Nokta artık dışarı **yürünerek** bulunuyor.

> ⚠️ **Tuzak — EK açılan sahnede test bütün sahneyi görür.** `OkmeydaniTests`
> ilk yazımda arazi sahnesinden gelen GIS öğelerinde (kıyı çizgisi) de
> `HistoricalTag` arıyordu. Kapsam açıkça daraltılmalı.

> 📏 **Menzil taşı bir ÖLÇÜDÜR.** Ayak taşı ile baş taşı arasındaki mesafe
> taşın üstünde yazan sayıdır ve test bunu ölçüyor. Sayılar ADR 0028 ile
> değişti (aşağıya bak).

### Faz 2b — menzilin yönü rüzgârdan ✅ (2026-08-23)

Doku değiştiyse **sıra önemlidir** — bu turda iki kez atlandı:

```powershell
python tools/textures/gen_marble_texture.py           # 1. dokuyu üret
& "$blender" --background --python tools/textures/build_unity_maps.py   # 2. bildirim
& "$blender" --background --python tools/blender/gen_okmeydani_kit.py   # 3. FBX
# Unity: Hezarfen -> Boru Hatti -> Osmanli malzemelerini uret            # 4. malzeme
#        Hezarfen -> Boru Hatti -> _Import'u yerlestir ve prefab uret    # 5. prefab
#        Hezarfen -> GIS -> Okmeydani sahnesi kur
#        Hezarfen -> GIS -> Okmeydani inceleme paketi
```

> ⚠️ **Tuzak — 4. adım atlanınca model MAGENTA olur.** Yeni bir malzeme adı
> (`M_Marble_White`) FBX'e girdiğinde Unity'de karşılığı yoksa import onu
> bağlayamaz. Malzemeler üretildikten **sonra** modeli yeniden import etmek
> gerekir; yalnız malzemeyi üretmek yetmez.

> ⚠️ **Tuzak — dokulu malzemede PALET RENGİ taşınmaz.** Kitabe ile mermer
> aynı doku rolünü kullanıyordu ve Unity'de **birebir aynı** iki malzeme
> çıktı: `kind == "pbr"` olan bir malzemede taban rengi dokudan gelir,
> `baseColor` yalnızca dokusuz (`untextured`) rollerde kullanılır. Kitabe
> sahnede hiç görünmüyordu.

> ⚠️ **Tuzak — az segmentli gövdede −Y'de KÖŞE vardır, düz yüz değil.**
> `make_tube` köşeleri 0°, 45°, … koyar. Kitabe panosu −Y'ye konunca yüzeye
> teğet kalıyordu; üstelik pano oturduğu düz yüzden **geniş**ti (0,248 m
> pano, 0,142 m yüz) ve kenarları siluetten taşıyordu. `phase = π/n` düz yüzü
> öne getirir; panonun ölçüsü artık o yükseklikteki gerçek yarıçaptan
> hesaplanıyor.

> 🔍 **Sahne karesi her kusuru göstermez.** Kitabe hatası taşın **iki
> tarafından da** "kitabe yok" gibi okunuyordu; gösteren şey Blender inceleme
> paketi oldu. İki alet iki farklı şeyi görüyor — biri sahneyi, öteki modeli.

> 📏 **Menzilin yönü rüzgârdır.** `ok azimutu = rüzgârın geldiği azimut + 180`
> — rüzgâr arkaya alınır. İşaret hatası sessizdir: her şeyi 180° döndürür ve
> sahne "çalışır". Test üç rüzgârı tek tek kilitliyor.

### Faz 1c geri dönüşü — sınırlar toptan ✅ (2026-08-23)

```powershell
$py = "tools/gis/.venv/Scripts/python.exe"
& $py tools/gis/walls_build.py --dir data/gis/istanbul      # sur (Galata capaya oturur)
& $py tools/gis/greenery_build.py --dir data/gis/istanbul   # sinirlar surdan/DEM'den turer
# Unity: Hezarfen -> GIS -> Yesil dokuyu dik
#        Hezarfen -> GIS -> Okmeydani sahnesi kur     (poligon degisti!)
#        Hezarfen -> GIS -> Yesil doku inceleme paketi
```

> ✅ **rasterio engeli AŞILDI.** `geodesy`'ye ters UTM dönüşümü eklendi
> (`from_utm35n`, kapanma 0,29 mm) ve beş araç rasterio'dan koparıldı:
> `walls_build`, `coastline_build`, `districts_build`, `landmarks_build`,
> `dem_probe`. `[İNSAN]` maddesi artık yalnız `dem_fetch` (COG indirme) ve
> `map_overlay` (raster yazma) için geçerli.

> ⚠️ **Tuzak — `walls_build` çalıştıysa greenery de çalıştırılmalı.** Sur
> içi ve Galata sınırları artık surun **kendisinden** okunuyor; sur değişip
> greenery değişmezse iki dosya ayrışır. `GreeneryTests.WallBackedBoundaries‑
> MatchTheWalls` bunu 1 m toleransla yakalar.

> ⚠️ **Tuzak — Okmeydanı poligonu değişince SAHNE de yeniden kurulmalı.**
> Menzil koridorları poligonun içine yerleştiriliyor; poligon 274→490 ha
> büyüdüğünde koridorlar da yer değiştirir.

> 📏 **"En alçak" ile "en alçak KARA" aynı şey değil.** Kağıthane vadi
> izlemesi ilk yazımda DEM'in mutlak minimumunu arıyordu ve deniz
> doldurmasının dereden yukarı kaçtığı −12 m'lik yamaya kilitlendi; mesire
> bir su birikintisinin etrafına diziliyordu. Eksen artık ≥1 m kotuna bakıyor.

> 🔎 **Bilinen arazi kusuru:** 28,95632 D / 41,06725 K'de ~60 × 80 m'lik bir
> yama DEM taban kotunda (−12 m). Kağıthane vadisinde bir "havuz" olarak
> görünür. Sınır değil ARAZİ işi (ADR 0007); `greenery_build` artık her
> alanın içindeki su hücresini sayıp raporluyor.

### Faz 2b KABUL — mahalle sahnesi + öğle/gün batımı paketi ✅ (2026-08-24)

Faz 2b'nin kabul ölçütü kapandı (Caner onayı bekliyor). Menü:
**Hezarfen → GIS → Mahalle inceleme paketi (ogle + gun batimi)** →
`Captures/mahalle/`, 8 kadraj × 2 an. ADR 0031,
`docs/feedback/mahalle_sahnesi.md`.

> 🔁 **AYNI SARIM HATASINI İKİNCİ KEZ YAPTIM** (2026-08-24, sur perdesi).
> Aşağıdaki uyarı yazılıydı, SETUP.md'de duruyordu — ve sur duvarını yazarken
> yine ters sardım: **4 199 yatay üçgenin 4 198'i aşağı**. Üstelik sarımın
> doğru olduğunu söyleyen kendi yorumumun altında. Sonuç: **yorumla
> önlenemiyor.** Elle mesh üreten her yer için üçgen normallerini **sayan bir
> test** şart (`OttomanStreetTests.PavementWalkingSurfaceFacesUp`,
> `LandmarkTests.WallCurtainTopFacesUp`).

> ⚠️ **MESH SARIMINI GÖZLE DENETLEYEMEZSİN.** `SM_Kaldirim`'in yürünen
> yüzeyinin 698 üçgeninden **697'si aşağı bakıyordu** ve kusur üç tur
> görünmedi — çünkü o turların bütün kareleri kaldırımın **altından**
> alınmıştı ve alttan bakınca yüzey doğru görünür. Ters sarımın üç sonucu
> var: yüzey üstten ışıksız/siyah okunur, Unity ışın sorguları arka yüzü
> **görmez** (yani çarpıcı fiilen yoktur, oyuncu düşer) ve altta kalan şey
> (arazi) zemin sanılır. Elle mesh üreten her yerde üçgen normallerini
> **say**; `OttomanStreetTests.PavementWalkingSurfaceFacesUp` örnektir.

> 📏 **Yaya araziye basmaz.** Mahallede basılan yüzey kaldırım ve taş
> kaidedir; ikisi de arazinin üstünde ve yamaçta fark metrelerle ölçülür.
> Göz hizası kadrajı `FrameMetric.OnGround` ile kurulursa kare kaldırımın
> **altında** çıkar. `FrameMetric.OnSurface` kullan (araziden 3 m yukarıdan
> aşağı ışın: saçak altı, kaldırım üstü).

> 📏 **`Renderer.bounds` DÖNMÜŞ nesnede yalan söyler.** Dünya hizalı kutu,
> 27° dönmüş bir ev için ileri erimi gerçek 2,5 m yerine **7,95 m** verdi;
> "evin 2,3 m önü" diye hesaplanan nokta 4,6 m'lik sokağı aşıp karşı evin
> içine düştü. Mesh köşelerini nesnenin **yerel** çerçevesine taşı
> (`MahalleReview.InFrontOf`).

> ⚠️ **`alphamapResolution` ATAMAK SPLATMAP'İ SİLER** — aynı değeri atasan
> bile. Tam boyamada zararsızdı (ardından her texel yeniden yazılıyordu);
> kısmi boyama eklenince bütün İstanbul toprağa düştü ve geri yazılan tek şey
> 400 m'lik mahalle dikdörtgeni oldu. **Kuşbakışı kare makul görünüyordu** —
> kahverengi bir yamaç yanlış durmuyor. Yakalayan şey örtü testleri oldu:
> ot %0,02, kaya %0, kıyı %0. Çözünürlük artık yalnız *değişiyorsa* atanıyor.
> (ADR 0032)

> ⚠️ **ÜRETİLEN VARLIK YOLU ÜRETENİ ANMALI.** Galata ve Balat, ürettikleri
> mesh'i aynı dosyaya yazıyordu (`SM_Kaldirim.asset`): Balat kurulunca
> Galata'nın kaldırımı ve bütün taş kaideleri **siliniyor**, yerlerine 2 km
> ötedeki geometri geçiyordu. Sahne bozuk görünmüyordu — eksik olan şey
> sessizce başka bir yerdeydi. `AssetDatabase.CreateAsset` sabit bir yola
> yazan her kod, aynı kodu iki kez çalıştıran her kullanıcıya bu tuzağı
> kurar. Yol artık `SM_<ad>_<semt>.asset`;
> `OttomanStreetTests.GeneratedMeshesBelongToThisQuarter` mesafeyi ölçüyor.

> 🔎 **Kadrajı tahminle değil ölçerek kur.** Bir duvarın içinden alınan kare
> AYRINTI 4,9 / ort 133 verir — sayı "makul", kare çöp. `CheckSphere`
> gözün çizici içinde olup olmadığını, `Linecast` hedefin görünüp
> görünmediğini söyler. Hiçbir aday geçmiyorsa kare **üretme ve logla**.

### ⚠️ Doğrulanmamış: batchmode `-executeMethod` bu makinede çalışmıyor

2026-08-19'da denendi, Unity **dönüş kodu 1** ile proje yolunu değiştirdikten hemen
sonra çıktı. Kilit dosyası yok, artık süreç yok, proje hatası yok. Logda lisans
uyarıları var:

```
[Licensing::Client] Code 10 while verifying Licensing Client signature
[Licensing::Module] LicensingClient has failed validation; ignoring
[Licensing::Module] Error: Access token is unavailable; failed to update
```

**Neden önemli:** CLAUDE.md'deki test omurgası (`-batchmode -runTests -quit`) ve
Faz 7 build komutu (`-executeMethod BuildPipelineEntry.BuildWindows`) aynı yola
dayanıyor. Şu ana kadar testler hep MCP üzerinden koştu, yani bu yol hiç
doğrulanmamıştı. **Faz 7'den önce çözülmeli** — muhtemelen Unity Hub'da bir kez
oturum açmak yeterli. `[İNSAN]` işi olabilir.

### `[İNSAN]` iş — şu an yok

Unity MCP köprüsü kurulu ve çalışıyor. Yalnızca Unity Editor'ün **açık** olması yeterli;
köprü kapalı Editor'de çalışmaz.

> **Not:** Resmî Unity MCP adayı elendi — ayrı bir paket olarak mevcut değil, Unity'nin bulut
> tabanlı AI Assistant ürününün (tamamı pre-release) içinde geliyor. Gerekçe: ADR 0002.

## Günlük kullanım komutları

```powershell
# Blender MCP sunucusu (GUI acilir, sunucu otomatik baslar)
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --python tools\blender\start_mcp_server.py

# Unity testleri (MCP'siz gecmek zorunda - CI omurgasi)
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode `
    -projectPath unity\HezarfenGame -runTests -testPlatform EditMode `
    -testResults results.xml -logFile tests.log

# Varlik uretimi (ADR 0005 + 0012 + 0013). Osmanli konutu, yaya seviyesi kademe
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\gen_ottoman_house.py -- --asset House_A --textured `
    --detail near --window-detail kafes --cumba-type corbel `
    --out-blend art\blend\SM_House_A.blend `
    --out-fbx  unity\HezarfenGame\Assets\_Import\SM_House_A.fbx
# ...ardindan Unity'de: Hezarfen -> Boru Hatti -> _Import'u yerlestir ve prefab uret

# Inceleme paketi (ADR 0006) -> renders\review\<ad>_vN\contact_sheet.png
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\render_preview.py -- `
    --in art\blend\SM_House_A.blend --asset House_A --hdri     # + --eye : yaya kadraji

# Blender oz-testi (delikli duvar, pivot, kademe tutarliligi)
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup `
    --python tools\blender\selftest.py
```

Sırada: trim sheet / atlas + Unity HDRP malzemeleri, sonra 20 parametre
kombinasyonu ve "Galata sokağı" test sahnesi (Faz 2 kabulü).
