# ADR 0001 — Araç Sürümleri ve Ortam Kilidi

**Tarih:** 2026-08-17
**Durum:** Kısmen tamamlandı — Unity Editor geldi; proje oluşturma ve uv bekliyor

## Bağlam
Plan (docs/PLAN.md, Görev 2) tüm araç sürümlerinin sabitlenmesini ve bu ADR'e yazılmasını
şart koşar. Ara sürüm atlamak yasaktır.

## Doğrulanan kurulumlar (bu makine, Windows 11 Home 26200)

| Araç | Sürüm | Yol | Not |
|---|---|---|---|
| Blender | **5.2.0 LTS** (build fbe6228777e7, 2026-07-14) | `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` | Plan "4.5 LTS veya üstü" diyor — karşılanıyor. **Risk aşağıda.** |
| **Unity Editor** | **6000.5.8f1** (hash 5cb7df797b7d) | `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe` | **SÜRÜM KİLİTLENDİ.** Ara sürüm atlanmaz. |
| Unity Hub | kurulu | `C:\Program Files\Unity Hub\Unity Hub.exe` | — |
| Python | 3.13.14 | `python` (PATH'te) | MCP for Unity 3.10+ istiyor — karşılanıyor |
| Git | kurulu | `C:\Program Files\Git\cmd\git.exe` | Depo henüz init edilmedi (lokal-önce kararı) |
| winget | v1.29.280 | — | uv kurulumu için |
| **uv / uvx** | **0.12.3** (507230998, 2026-08-07) | `C:\Users\ahmet\AppData\Local\Microsoft\WinGet\Packages\astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe\uv.exe` | winget ile kuruldu. PATH'e kaydedildi; **yeni kabuk gerekir**. |
| **blender-mcp eklentisi** | **bl_info 1.2** (kaynak: main @ 2026-08-11) | `C:\Users\ahmet\AppData\Roaming\Blender Foundation\Blender\5.2\scripts\addons\addon.py` | Headless kuruldu + etkinleştirildi. Modül adı: `addon`. SHA256: `CA6955BB584D78E229F020A8B9D7011440ADC6E94DAB0AC8E01AB2794DB19DC0` |
| **blender-mcp sunucusu** | `uvx blender-mcp` (39 paket çözüldü) | uv cache | Ayağa kalkıyor; Blender'a bağlanmayı deniyor. |

**Unity modülleri (doğrulandı):** `windowsstandalonesupport` ✅, `il2cpp` ✅ (Faz 8 build'i hazır),
`WebGLSupport` (fazladan kurulmuş — zararsız, sadece disk).

**HDRP şablonu mevcut:** `Editor\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.3d-high-end-17.0.7.tgz`

**Donanım (build hedefleri için referans):** NVIDIA GeForce RTX 4070 Laptop GPU, 8 GB VRAM
(sürücü 32.0.15.9636), D3D12 feature level 12.2. Plan hedefi 1080p/60 "orta segment GPU" —
bu makine üst-orta segment, yani burada tutan FPS hedef kitlede de tutar sayılmaz; profil
ölçümleri buna göre yorumlanmalı.

## Kurulan proje

| Öğe | Durum |
|---|---|
| `unity/HezarfenGame` | ✅ HDRP şablonundan kuruldu, 43 paket çözüldü, 0 derleme hatası. Ayrıntı: **ADR 0004** |
| EditMode testleri | ✅ 6/6 geçti (batchmode, MCP'siz) |

## Eksik kurulumlar — `[İNSAN]` (Caner)

| Araç | Neden gerekli | Etki |
|---|---|---|
| Unity MCP adayları (resmî + CoplayDev) | Köprü yarışı | Görev 3 duman testi. **Onay diyalogları Caner'de.** |

## Güvenlik/gizlilik kararı: blender-mcp telemetrisi KAPALI

Kod incelemesinde (`src/blender_mcp/telemetry.py`, `addon.py`) bulunanlar:

- Sunucu açılışta ve her araç çağrısında `https://yzasssndwqceclzilcdu.supabase.co` adresine
  telemetri gönderir. **Varsayılan açıktır.**
- Onaysız gönderilen: anonim kullanım olayları (araç adı, başarı/süre, oturum kimliği, kalıcı UUID).
- **Onay verilirse gönderilen: prompt metinleri, kod parçaları, sahne bilgisi ve viewport ekran
  görüntüleri** (Supabase Storage'a PNG yüklemesi — `upload_screenshot`).
- Eklentideki onay varsayılanı `False`'tur (`telemetry_consent`), **ancak** `get_telemetry_consent`
  içinde tercihler okunamazsa fallback `consent = True`'dur — yani hata durumunda açık kalır.

**Karar:** Telemetri sunucu tarafında tamamen kapatılır. `.mcp.json`da üç ortam değişkeni birden
set edilir (`DISABLE_TELEMETRY`, `BLENDER_MCP_DISABLE_TELEMETRY`, `MCP_DISABLE_TELEMETRY` = `true`).
Bu, `config.enabled = False` yapar ve onay kontrolüne hiç gelmeden tüm gönderimi keser — yani
eklentideki fail-open dalı devre dışı kalır.

**Gerekçe:** Viewport render'larımız ve prompt'larımız yayınlanmamış oyun içeriğidir; üçüncü tarafa
gitmesi kabul edilemez. Ek olarak Blender eklentisi tercihlerinde "Allow Telemetry" kutusu
işaretlenmemiş kalmalıdır (varsayılan böyle — değiştirme).

## Risk: Blender 5.2 vs blender-mcp uyumu — ÇÖZÜLDÜ ✅
Endişe: `blender-mcp` ağırlıkla Blender 4.x döneminde geliştirildi; Blender 5.0 eklenti/uzantı
sistemi ve bazı `bpy` API'leri değişti.

**Doğrulama sonucu (2026-08-17, Blender 5.2.0 LTS / Python 3.13.13):**
- `bpy.ops.preferences.addon_install` ve `addon_enable` hâlâ mevcut (legacy eklenti yolu çalışıyor)
- Eklentinin bağımlılıkları Blender'ın Python'unda var: `requests` ✅ `numpy` ✅ `mathutils` ✅
- Kurulum + etkinleştirme + tercih kaydı sorunsuz; `BLENDERMCP_PT_Panel` kaydoldu,
  `blendermcp.start_server` / `stop_server` operatörleri erişilebilir
- Yedek planlar (4.5 LTS yan kurulum / eklenti yaması) **gerekmedi**

**Bilinen sınır (tasarım gereği, hata değil):** Eklenti arka plan modunu (`blender -b`) açıkça
reddediyor — "commands would never execute". Komutlar Blender'ın ana thread'inde yürütüldüğü için
olay döngüsü şart. Yani MCP her zaman GUI oturumu ister. Başlatma: `tools/blender/start_mcp_server.py`.

Bu sınır plan Bölüm 4.1'deki iş bölümüyle zaten uyumlu: **MCP laboratuvardır (GUI, etkileşimli),
üretim bandı headless scriptte yaşar (CI, deterministik).**

## Karar
Blender 5.2.0 LTS ile devam — uyum doğrulandı. Unity sürümü `6000.5.8f1` olarak kilitlendi;
proje oluşturulduğunda `ProjectVersion.txt` ile teyit edilecek.
