# Hezarfen: 1632 — Çalışma Sözleşmesi

## Ne yapıyoruz
1632 İstanbul'unda geçen 3D açık dünya uçuş/keşif oyunu. Plan: docs/PLAN.md. Tarih: docs/RESEARCH.md.
Fazların kabul kriterleri karşılanmadan sonraki faza GEÇME.

## Rol
Tüm üretim (kod + 3D + animasyon + ışık + NPC içerikleri) bende. Caner yalnızca kurulum/onay
yapar ve yazılı geri bildirim verir. Ona üretim görevi atama; kararsızsan inceleme paketi üret,
notunu bekle. Notları docs/feedback/<varlık>.md'ye logla. Onay formatı: "OK vN".

## Ortam (bu makine — doğrulandı 2026-08-17)
- Blender: `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` (5.2.0 LTS)
- Unity: `C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe` — **sürüm kilitli**
- Proje: `unity/HezarfenGame` (HDRP, Cinemachine 3.1.7) — bkz. ADR 0004
- Python 3.13.14, Git, uv/uvx 0.12.3 — hepsi kurulu
- GPU: RTX 4070 Laptop, 8 GB VRAM (üst-orta segment — FPS ölçümlerini buna göre yorumla)
- Ayrıntı ve sürüm kilidi: docs/decisions/0001-versions.md

## Sürüm kontrolü — GIT + LFS (2026-08-27'den beri)
Depo: `https://github.com/Canerahmet/HezarfenOyunu.git`, tek dal **`main`**. Sürüm kontrolünü
Claude yürütür (ADR 0059; ADR 0003'ün yerine geçti). Commit mesajı: **İngilizce, kısa, emir kipi**
başlık + **gerekçeyi** anlatan gövde — "ne" diff'te zaten var, gövde "niçin"i yazar. Faz kabulleri
etiketle işaretlenir.

**İkili varlık politikası:** Unity'nin okuduğu her şey depoya girer (GUID'ler `.meta` dosyalarında
yaşar; varlığı `.meta`sız yeniden üretmek bütün referansları sessizce kırar). Yeniden indirilebilir
üçüncü taraf kaynakları (`art/textures/polyhaven`, `hdri`) girmez — ama `meta.json` kayıtları girer.
`data/` ve `renders/` de girmez. Kural: **türetilmiş veri girmez, kaydı girer.**

Bu, "sadece sohbette var olan varlık yasak" kuralını GEVŞETMEZ — her kalıcı çıktı dosyaya yazılır.

## Araçlar
- MCP: `.mcp.json` — blender (blender-mcp) + unity (MCP for Unity / CoplayDev v10.1.2, ADR 0002).
  İkisinin de telemetrisi ortam değişkenleriyle kapalı — DEĞİŞTİRME (gerekçe: ADR 0001).
  Blender MCP GUI oturumu ister; Unity MCP Editor açıkken çalışır. İkisi de laboratuvardır.
- Headless Blender: `& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python tools/blender/<script>.py -- <argümanlar>`
- Önizleme/inceleme paketi (ADR 0006): `... render_preview.py -- --in <blend/fbx> --asset <ad>`
  → `renders/review/<ad>_vN/` (sürüm otomatik artar); `contact_sheet.png`'yi OKU, referansla
  kıyasla, düzelt, tekrar üret. (Temel döngü budur.) Her karede 1,70 m ölçek figürü vardır.
  **Render bir gözlemdir, kanıt değil:** gördüğün kusuru düzeltmeden önce ölç — bir kez
  aydınlatma kusuru geometri hatası sanıldı.
- Export: SADECE tools/blender/export_fbx.py. Elle export yasak.
- Varlık hattı (ADR 0005): `gen_*.py` → `art/blend/` (kanonik) → `export_fbx.py` →
  `Assets/_Import/` → Unity menüsü **Hezarfen → Boru Hatti → _Import'u yerlestir ve prefab uret**.
  `_Import` boş bırakılır. Eksen: Unity(x,y,z) = Blender(-x, z, -y); **evin önü +Z**.
  `SM_AxisCalibration.fbx` ölçü aletidir — silme, ayar değişince yeniden üret + testleri koştur.
- GIS (ADR 0007): `tools/gis/.venv/Scripts/python.exe tools/gis/dem_fetch.py` →
  `dem_probe.py` (georeferans denetimi) → Unity menüsü **Hezarfen → GIS**.
  **Dünya orijini = Galata Kulesi tabanı** (28.974017 D, 41.025637 K); **y=0 deniz seviyesi**;
  UTM 35N. `data/` türetilmiştir, depoya girmez.
- GeoJSON (ADR 0008): projeksiyon dönüşümü **yalnızca Python'da**; Unity yerel metre okur.
  `refs/maps/*.geojson` WGS84 ve kendi telifimizdir. Kaynak niteliksel olduğunda metrik
  geometri UYDURMA — alanı kaba kutu + T2 + `status: draft` olarak işaretle ve Caner'e sor.
- Unity testleri: `Unity.exe -batchmode -projectPath unity/HezarfenGame -runTests -testResults results.xml -quit`
  (MCP'siz geçmek zorunda.)
- Unity build (Faz 7+): `-batchmode -executeMethod BuildPipelineEntry.BuildWindows -quit`

## Kurallar
- 1 birim = 1 metre. Eksen/ölçek doğrulaması Editor testiyle zorunlu.
- MCP oturumunda doğan kalıcı değişiklik ya scripte taşınır ya kanonik .blend/sahne olarak
  kaydedilir. Sadece sohbette var olan varlık yasak.
- Assets/_Project dışına dosya koyma; _Import sadece iniş alanı.
- Her yeni sahne öğesine HistoricalTag (T1/T2/T3) ata; T1 için RESEARCH.md'den kaynak satırı yaz.
- Çalışma zamanında bulut LLM çağrısı YOK (v1.0). NPC içerikleri offline üretilir ve statik gemiye konur.
- refs/ altına lisansı LICENSES.md'de belgelenmemiş HİÇBİR görsel indirme.
- Şüphede kal → docs/decisions/ altına kısa ADR yaz, iki seçenek + öneri sun, Caner'e sor.

## Adlandırma
`SM_GalataTower_LOD0`, `SK_Hezarfen`, `M_Plaster_Worn`, `T_Plaster_Worn_BC/_N/_ORM`, `PF_House_A2`.
Collider mesh: `UCX_` öneki. Commit mesajları İngilizce, kısa, emir kipi.

## Tanım: Bitti (Definition of Done)
Kod: derleniyor + testler yeşil + sahnede çalışır demo. Model: FBX importlu, ölçek testi
geçmiş, LOD'lu, prefab'lı, HistoricalTag'li, inceleme paketi renders/review/ altında ve
Caner onayı ("OK vN") docs/feedback/'te kayıtlı.
