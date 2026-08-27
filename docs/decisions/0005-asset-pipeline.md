# ADR 0005 — Varlık Boru Hattı: Blender → FBX → Unity

**Tarih:** 2026-08-17
**Durum:** Kabul edildi
**Karar veren:** Claude (Caner projeyi tamamen devretti)
**İlgili:** plan Görev 7, ADR 0001 (sürüm kilidi), ADR 0002 (MCP)

## Bağlam

Faz 2'den itibaren binlerce yapı üretilecek. Ölçek ve eksen hataları **sessizdir**:
yan yatmış bir ev fark edilir, %1,27 büyük bir ev fark edilmez ve ancak şehrin
tamamı üretildikten sonra ortaya çıkar. Bu yüzden boru hattının her iki ucu da
scriptte yaşamalı ve bir **ölçü aletiyle** doğrulanmalı.

Plan Görev 7 bunu şöyle soruyor: "1 m küp tam 1 m mi?"

## Karar

Boru hattı beş adımdır ve her adım dosyada yaşar:

```
tools/blender/gen_*.py          jeneratör (bpy, headless)
        ↓
art/blend/<varlık>.blend        kanonik kaynak (LFS)
        ↓
tools/blender/export_fbx.py     TEK yetkili ihraç yolu
        ↓
Assets/_Import/<varlık>.fbx     iniş alanı (geçici)
        ↓  Hezarfen/Boru Hatti/_Import'u yerlestir
Assets/_Project/Art/Models/     kalıcı model + Prefabs/PF_<ad>.prefab
```

### İhraç ayarları (`export_fbx.py`, statik varlıklar)

| Ayar | Değer | Gerekçe |
|---|---|---|
| `axis_forward` / `axis_up` | `-Z` / `Y` | Blender Z-up → Unity Y-up |
| `global_scale` | `1.0` | — |
| `apply_unit_scale` | `True` | 1 Blender metresi = 1 Unity birimi |
| `apply_scale_options` | `FBX_SCALE_NONE` | Ölçek mesh verisinde, dönüşümde değil |
| **`bake_space_transform`** | **`True`** | **Eksen dönüşümünü mesh verisine işler.** Kapalıyken Unity kök nesneyi `(-89.98, 0, 0)` rotasyonuyla getirir; o zaman her prefab'a elle düzeltme rotasyonu girmek gerekir. |
| `mesh_smooth_type` | `FACE` | Normaller Blender'dan gelir |
| `use_tspace` | `True` | — |
| `bake_anim` | `False` | Statik varlıkta animasyon yok |

**İskeletli varlıklar (`--skinned`) için `bake_space_transform=False`.** Deforme
olan mesh'te uzay bakma bozulmaya yol açar; bu ayrım koda `_STATIC` / `_SKINNED`
sözlükleri olarak yazıldı.

### İçe alma ayarları (`ModelImportPolicy`)

İhraç tarafını kilitlemek yarım çözümdür: Inspector'dan değiştirilen bir ölçek
çarpanı aynı hatayı öbür uçtan geri getirir. `AssetPostprocessor`, `_Import/` ve
`_Project/Art/Models/` altındaki her FBX için ayarları zorlar. Politika değişince
`GetVersion()` artırılır; yoksa diskteki varlıklar eski ayarlarla kalır.

Kritik iki seçim:
- `materialImportMode = ImportViaMaterialDescription` — `ImportStandard`, HDRP'de
  **macenta** malzeme üretir ve tüm inceleme paketlerini baştan çöpe atar.
- `importTangents = CalculateMikk` — graybox mesh'lerinde UV yok, dolayısıyla
  FBX'te teğet de yok; `Import` seçilirse Unity uyarı basıp boş teğet üretir.

## Ölçüm: eksen eşlemesi

Kalibrasyon varlığı `SM_AxisCalibration.fbx` bir sanat varlığı değil **ölçü
aletidir**. Üç işaretçi ÜÇ FARKLI uzaklıkta durur (2 / 3 / 4 m) — eşit uzaklıkta
olsalardı eksen *takası* ile eksen *çevrimi* birbirinden ayırt edilemezdi.

Ölçülen sonuç (Blender 5.2.0 + Unity 6000.5.8f1, 2026-08-17):

| Blender | Uzaklık | Unity | Anlamı |
|---|---|---|---|
| `+X` | 2 m | `(-2, 0, 0)` | **-X** |
| `+Y` | 3 m | `(0, 0, -3)` | **-Z** |
| `+Z` | 4 m | `(0, 4, 0)` | **+Y** |

Yani **Unity(x, y, z) = Blender(-x, z, -y)**.

Birim küp Unity'de tam `(1.0000, 1.0000, 1.0000)`; kök nesne rotasyonu `(0, 0, 0)`,
ölçeği `(1, 1, 1)`.

### "+X neden ters?" — aynalanma değil, el değişimi

Sayısal eşlemenin determinantı **-1**'dir. Tek başına bakıldığında bu aynalanma
gibi görünür; değildir. Blender sağ-elli, Unity sol-ellidir; işaret çevrimi tam
olarak bu el değişimini karşılar. Nesnenin kendi üçlüsü korunur:

| Nesnenin yönü | Blender | Unity |
|---|---|---|
| sağ | `-X` | `+X` |
| yukarı | `+Z` | `+Y` |
| ileri | `-Y` | `+Z` |

`AxisMapping_PreservesHandedness` testi bunu `right × up = forward` ile doğrular.
Model aynalanmış olsaydı bu çarpımın işareti dönerdi. Aynalanma dokular gelene
kadar fark edilmeyen, sonra da her yazıyı ters gösteren bir hatadır.

### Türeyen sözleşme: **evin önü +Z'dir**

Blender'da sokak cephesi `-Y`'dir (cumba oraya taşar) → Unity'de `+Z`. Faz 2'nin
sokak yerleştiricisi bu kurala dayanacak.

## Kabul edilen ekler / sapmalar

1. **`art/blend/` klasörü eklendi.** Plan Bölüm 3'ün ağacında kanonik `.blend`
   dosyalarına yer yoktu; ağaç güncellenmelidir. `.gitattributes` `*.blend` ve
   `*.fbx`'i zaten LFS'e yönlendiriyor.
2. **`Assets/_Project/Art/Prefabs/`** — plan ağacı `Art/` altında Models,
   Materials, Textures sayıyor; prefab'lara yer yok. `PF_` öneki (CLAUDE.md)
   bir prefab klasörü gerektiriyor.
3. **`_Import` boş bırakılır.** İniş alanı depo değildir; içinde varlık
   unutulursa hangi kopyanın kanonik olduğu belirsizleşir. `ImportLanding_IsEmpty`
   testi bunu korur. Taşıma `AssetDatabase.MoveAsset` ile yapılır — GUID korunur;
   Explorer'dan taşımak sahne referanslarını koparır.
4. **Collider silüetten dardır.** `UCX_` mesh'i convex `MeshCollider`'a çevrilir.
   Uçuş oyununda oyuncu "değmedim ama çarpıştım" hissini affetmez; saçak altından
   geçmek mümkün kalmalı.

## Kanıt

`AssetPipelineTests` — 15 test, hepsi yeşil (EditMode toplamı 47/47).
Yukarıdaki tüm sayılar bu testlerde sabittir; boru hattı değişirse test kırılır.

Doğrulanan varlık: `PF_BoxHouse` — 2 katlı, 7,0 × 6,5 m ayak izi, 8,20 m yükseklik,
LOD0 44 üçgen / LOD1 20 üçgen, LODGroup otomatik (Unity `_LOD0`/`_LOD1`
adlandırmasından kurar), convex MeshCollider, `HistoricalTag = Graybox`.

## Reddedilen seçenek

**`bake_space_transform=False` + Unity tarafında düzeltme rotasyonu.** Blender'ın
varsayılanı budur ve iskeletli varlıklarda zorunludur. Statikte reddedildi: kök
rotasyonu `(-89.98, 0, 0)` olan bir prefab'ı yerleştirmek, döndürmek ve hizalamak
her seferinde zihinsel dönüşüm gerektirir; 20.000 binada bu hata üretir.

## Komutlar

```powershell
$b = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"

# Kalibrasyon (boru hattı değişince yeniden üret)
& $b --background --factory-startup --python tools/blender/gen_axis_calibration.py -- `
    --out-blend art/blend/SM_AxisCalibration.blend `
    --out-fbx  unity/HezarfenGame/Assets/_Project/Art/Models/Calibration/SM_AxisCalibration.fbx

# Kutu ev (parametrik)
& $b --background --factory-startup --python tools/blender/gen_box_house.py -- `
    --floors 2 --width 7.0 --depth 6.5 --cumba 0.8 `
    --out-blend art/blend/SM_BoxHouse.blend `
    --out-fbx  unity/HezarfenGame/Assets/_Import/SM_BoxHouse.fbx

# Mevcut .blend'i yeniden ihraç et
& $b --background --python tools/blender/export_fbx.py -- `
    --in art/blend/SM_BoxHouse.blend --out unity/HezarfenGame/Assets/_Import/SM_BoxHouse.fbx
```

Ardından Unity'de: **Hezarfen → Boru Hatti → _Import'u yerlestir ve prefab uret**
