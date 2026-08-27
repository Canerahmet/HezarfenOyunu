# ADR 0004 — Unity Proje Yapılandırması

**Tarih:** 2026-08-17
**Durum:** Kabul edildi
**Karar veren:** Claude (Caner projeyi tamamen devretti)

## Bağlam
`unity/HezarfenGame` projesi Unity Hub GUI'si yerine editor'ün içindeki HDRP şablonundan
(`com.unity.template.3d-high-end-17.0.7`) deterministik biçimde kuruldu. Sebep: her adımın
scriptlenebilir ve tekrar üretilebilir olması — plan Bölüm 4'ün "üretim bandı scriptte yaşar"
kuralı proje kurulumu için de geçerli.

## Paket sürümleri (sabitlendi)

| Paket | Sürüm | Gerekçe |
|---|---|---|
| `com.unity.render-pipelines.high-definition` | 17.5.0 | Editor 6000.5.8f1'in önerdiği sürüm (şablon 17.0.1 diyordu — güncellendi) |
| **`com.unity.cinemachine`** | **3.1.7** | **Editor 2.10.7 öneriyordu; plan 3.x şart koşuyor.** 3.x yeni API (CinemachineCamera), 2.x eski. Kayıttan `latest` = 3.1.7, Unity 2022.3+ uyumlu. |
| `com.unity.inputsystem` | 1.20.0 | Yeni girdi sistemi; gamepad + klavye çift destek |
| `com.unity.splines` | 2.9.0 | Cinemachine bağımlılığı; ayrıca sokak/kıyı spline'ları için işimize yarayacak |
| `com.unity.addressables` | 2.9.1 | Semt bazlı sahne yayını (Faz 1, Bölüm 6.6) |
| `com.unity.timeline` | 1.8.12 | Sinematik sahneler (Perde 2 zirve sekansı, epilog) |
| `com.unity.ugui` | 2.5.0 | UI |
| `com.unity.editorcoroutines` | 1.1.0 | Editor araçları (GIS import, toplu işlem) |
| `com.unity.test-framework` | 1.7.0 | Kabul kriterlerinin kanıtı |

**Çıkarılan şablon paketleri:**
- `com.unity.visualscripting` — C# yazıyoruz, görsel scripting ölü ağırlık
- `com.unity.collab-proxy` — Unity sürüm kontrolü kullanmıyoruz (git, ADR 0003)
- `com.unity.feature.development` — feature paketi yerine bileşenleri açıkça pinlendi;
  belirsiz geçişli bağımlılık yerine deterministik liste

**Doğrulama:** `packages-lock.json` hepsini istenen sürümde çözdü, 43 paket, sıfır derleme hatası.

## Proje ayarları

| Ayar | Değer | Not |
|---|---|---|
| Renk uzayı | **Linear** | Şablon zaten doğru getirdi. HDRP için şart; Gamma'ya düşerse tüm ışık çalışması geçersiz. Testle kilitlendi. |
| Aktif girdi | **Yalnız yeni Input System** (`activeInputHandler: 1`) | Eski sistemle karışık mod istemiyoruz |
| API uyum düzeyi | .NET Standard 2.1 | Varsayılan, uygun |
| Varsayılan çözünürlük | 1920×1080 | Plan hedefi 1080p/60 (şablon 1024×768 getiriyordu) |
| Şirket / Ürün | Hezarfen / Hezarfen 1632 | Şablon `com.unity.template.hdrp-blank` diyordu |

## Klasör ve assembly yapısı

```
Assets/
├─ _Project/               # bize ait her şey (plan Bölüm 3)
│  ├─ Art/{Models,Materials,Textures}
│  ├─ Code/
│  │  ├─ Runtime/          # Hezarfen.Runtime
│  │  ├─ Editor/           # Hezarfen.Editor      (Editor-only)
│  │  └─ Tests/{EditMode,PlayMode}  # Hezarfen.Tests.*
│  ├─ Scenes/{Districts,Sandbox}
│  └─ Data/{Input,WindProfiles,DistrictDefs,QuestDefs,NPCSchedules,HistoricalTags}
├─ _Import/                # Blender FBX iniş alanı — SADECE geçiş noktası
└─ Settings/               # HDRP asset'leri (Unity'nin beklediği yer, taşınmadı)
```

**Neden asmdef?** Assembly ayrımı olmadan tek bir script değişikliği tüm projeyi yeniden
derletir. Şehir ölçeğinde bir projede bu iterasyon hızını öldürür. Ayrıca Editor kodunun
build'e sızmasını yapısal olarak imkânsız kılar.

Şablonun demo sahnesi `_Project/Scenes/Sandbox/OutdoorsScene.unity`e taşındı (silinmedi —
HDRP ışık/hacim kurulumu için çalışan bir referans). GUID korunduğu için bağlantılar sağlam.

## Kilitlenen sözleşmeler
`Assets/_Project/Code/Runtime/Core/GameUnits.cs` — **1 birim = 1 metre**. Efsanevi süzülüş
sabitleri (3358 m / 62 m) burada, kaynak notlarıyla; koda dağılmış sihirli sayı yok.

`ProjectConventionsTests.cs` bunları teste bağlar: Linear renk uzayı, HDRP'nin aktif pipeline
olması, birim küpün 1 metre olması, `_Project`/`_Import` klasörlerinin varlığı. Bu testler
"güzel olsa iyi olur" değil — bozulduklarında varlık hattı sessizce yanlış çalışır.

## Not: IL2CPP
Build backend şimdilik Mono (varsayılan) — iterasyon hızı için. Faz 8'de Windows sürümü
IL2CPP'ye alınacak (modül kurulu ve hazır).
