# ADR 0002 — Unity MCP Köprüsü Seçimi

**Tarih:** 2026-08-17
**Durum:** Karar verildi — CoplayDev
**Plan referansı:** Bölüm 4.1, Görev 3 ("iki aday, duman testinde seçilir")

## Bağlam
Plan iki aday arasında duman testi öngörüyordu: (1) resmî Unity MCP Server, (2) MCP for Unity
(CoplayDev). Kurulum aşamasında **karşılaştırmanın öncülü çöktü** — adaylar aynı kategoride
değilmiş.

## Bulgu: "Resmî Unity MCP" bağımsız bir köprü değil

Unity kayıt sunucusu tarandı:

| Aranan paket | Sonuç |
|---|---|
| `com.unity.ai.mcp` | 404 — yok |
| `com.unity.mcp` | 404 — yok |
| `com.unity.ai.assistant` | **VAR** — MCP sunucusu bunun içinde |

`com.unity.ai.assistant` (Assistant), Unity'nin kendi **üretken yapay zekâ ürünü**. Paket
açıklamasından: "generative AI tool integrated into the Unity Editor… Agent mode: Assistant can
create, modify, or remove objects and assets."

Üç engel:

1. **Kararlı sürüm yok.** 45 sürümün tamamı pre-release; `latest` = `2.18.0-pre.1`. Plan
   "sürümü sabitle, asla ara sürüm atlama" diyor — sabitlenecek kararlı sürüm mevcut değil.
2. **Bulut servisi.** Proje bağlamını Unity'nin sunucularına gönderir; Unity hesabı ve kredi
   gerektirir. Proje ilkesi (ADR 0001 telemetri kararı, CLAUDE.md "çalışma zamanında bulut LLM
   yok") geliştirme masasında da aynı yönde: varlıklarımız yayınlanmamış içerik.
3. **İşlev çakışması.** Agent modu sahneyi ve varlıkları düzenliyor — yani üretimi yapan ikinci
   bir ajan. Tek üretici modeli (plan Bölüm 4) buna aykırı; iki ajanın aynı projeye yazması
   sürüm kontrolü olmayan bir ortamda özellikle riskli.

**Sonuç:** Bu bir MCP köprüsü değil, MCP arayüzü de sunan bir AI asistanı. Plandaki
"resmî Unity MCP Server" tanımı gerçeğe karşılık gelmiyor.

## Karar
**MCP for Unity (CoplayDev)** — tek aday olarak kuruldu.

| Ölçüt | Değer |
|---|---|
| Paket | `com.coplaydev.unity-mcp` |
| Sürüm | **v10.1.2** (2026-08-02; depo 2026-08-07'de güncel, arşivlenmemiş) |
| Lisans | MIT |
| Unity uyumu | 2021.3+ → 6.x |
| Yıldız | 13.4k |
| Kurulum | `manifest.json` git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.1.2` |

Plan `v10.0.0` diyordu; kayıttaki güncel kararlı sürüm `v10.1.2` olduğu için o pinlendi.

**Kurulum doğrulaması (2026-08-17):** `packages-lock.json`a git kaynağı olarak yazıldı,
commit hash `4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50` ile kilitli. Derleme hatasız.
Geçişli bağımlılık: `com.unity.nuget.newtonsoft-json` 3.2.2.

### Kod incelemesi
- **Editor assembly** (`MCPForUnity.Editor`) — `includePlatforms: ["Editor"]`, build'e giremez. ✅
- **Runtime assembly** (`MCPForUnity.Runtime`) — **platform kısıtı yok.** İçeriği incelendi:
  ekran görüntüsü yardımcısı, Unity sürüm uyumluluk shim'leri ve JSON tip dönüştürücüleri.
  Soket yok, `Process.Start` yok, `RuntimeInitializeOnLoadMethod` yok; tek `MonoBehaviour`
  (`ScreenshotCapturer`) talep üzerine yaratılan geçici bir yardımcı. Yani **pasif** — kendi
  başına çalışmıyor, sunucu açmıyor.
- Sunucu tarafı `uvx --from <kaynak> mcp-for-unity` ile başlatılıyor (uv bu yüzden gerekliydi).

### Faz 8 için not — SÜRÜM ÖNCESİ ÇIKAR
Runtime assembly'si platform kısıtsız olduğu için release build'e derlenir. Zararsız ve pasif,
ama teslim edilen oyunda geliştirme aracının kodunun bulunmasının hiçbir gerekçesi yok.
**Faz 8 build hattında `com.coplaydev.unity-mcp` paketi `manifest.json`dan çıkarılacak** ve
build bu paketsiz doğrulanacak. Bu satır bir hatırlatma değil, kabul kriteridir.

## Duman testi — GEÇTİ ✅ (2026-08-17)

Bağlantı: `HezarfenGame@882a2764`, Unity 6000.5.8f1, stdio, `127.0.0.1:6400`.

| Yetenek | Sonuç |
|---|---|
| Editör durumu okuma (resource) | ✅ `ready_for_tools: true` |
| Sahne hiyerarşisi okuma | ✅ 2 kök nesne; ışıkta `HDAdditionalLightData` (HDRP teyidi) |
| GameObject oluşturma | ✅ `MCP_SmokeTest_Cube`, konum/ölçek doğru döndü |
| GameObject silme | ✅ |
| Sahne kaydetme | ✅ `Assets/_Project/Scenes/FlightSlice.unity` |
| Konsol okuma | ✅ |
| Test koşusu + sonuç alma | ✅ **5/5 geçti**, 0.061 s |

### Öğrenilen tuzak: kaydedilmemiş sahne komut kuyruğunu kilitliyor
İlk test koşusu başarısız oldu. Konsol nedeni söylüyordu:

```
[TestRunnerService] Skipping unsaved scene '': save it manually before running tests.
```

**Asıl sorun mesaj değil, sonrası:** test koşucusu reddettikten sonra iş askıda kaldı ve
ardından gönderilen **her komut** zaman aşımına uğradı — Editor log'unda
`Command TCS timed out (N consecutive)` sayacı 31'e kadar tırmandı. Yani hata sessiz
değil ama sonucu yanıltıcı: köprü çökmüş gibi görünüyor, oysa tek sebep kaydedilmemiş sahne.

**Kurtarma:** Komut göndermeyi kesince tıkanma kendiliğinden dindi (yeni zaman aşımı
üretilmedi, port 6400 açık kaldı, Unity `Responding=True`). Sahne kaydedilip tekrar
koşulduğunda sorunsuz geçti. Editor'ü yeniden başlatmak **gerekmedi**.

**Kural:** MCP üzerinden test koşmadan önce aktif sahne kaydedilmiş olmalı. Askıda iş
kalırsa `run_tests(clear_stuck=true)` ile temizlenir.

## Yapılandırma — elle yazıldı, otomatik yol kullanılmadı
Paketin otomatik istemci yapılandırması (`Configure All Detected Clients`) Claude Code için
`claude mcp add` CLI komutuna dayanıyor. Bu makinede Claude Code **VS Code eklentisi** olarak
çalışıyor ve standalone CLI PATH'te yok — MCP penceresi de bunu doğruluyor:
`Claude CLI Path: Not found`. Dolayısıyla otomatik yol bizde çalışmaz.

Bunun yerine `.mcp.json` elle yazıldı; sunucu sürümü PyPI'dan doğrulandı
(`mcpforunityserver==10.1.2` — Unity paketiyle birebir aynı sürüm):

```json
"unity": {
  "command": "uvx",
  "args": ["--from", "mcpforunityserver==10.1.2", "mcp-for-unity"],
  "env": { "DISABLE_TELEMETRY": "true", "UNITY_MCP_DISABLE_TELEMETRY": "true", "MCP_DISABLE_TELEMETRY": "true" }
}
```

**MCP penceresindeki iki kırmızı gösterge normaldir:**
- `Not Configured` — paketin *kendi* kayıt mekanizmasının durumu. Biz onu kullanmıyoruz.
- `No Session` — hiçbir istemci bağlı değilken görünür.

### Politika kod ile uygulanıyor
Paket varsayılanları projeye uymuyordu; üç ayar `McpProjectPolicy.cs` ile sabitlendi
(menü: **Hezarfen → MCP → Proje politikasini uygula**). EditorPrefs makinede yaşadığı için
elle tıklanan ayar "sadece sohbette var olan varlık" olurdu — CLAUDE.md bunu yasaklıyor.

| Ayar | Paket varsayılanı | Bizim değer | Gerekçe |
|---|---|---|---|
| `UseHttpTransport` | `true` (HTTP Local:8080) | **`false` (stdio)** | `.mcp.json` sunucuyu Claude Code'un kendisi başlatıyor; ayrı port/oturum yönetimi yok. Paketin kendi CI önyükleyicisi de stdio seçiyor. |
| `TelemetryDisabled` | `false` (telemetri açık) | **`true`** | ADR 0001 ile aynı gerekçe |
| `ClientProjectDir` | Unity proje klasörü | **workspace kökü** | Claude Code `Hezarfen_Oyunu`'ndan çalışıyor, Unity bir alt klasörde |

## Kaybeden aday — yedek olarak belgelendi
Unity Assistant tamamen dışlanmıyor: Unity ileride kararlı ve yerel bir MCP sunucusu ayrıştırırsa
yeniden değerlendirilir. Bugünkü haliyle projeye alınmıyor. Caner isterse (kendi hesabı ve
kredisiyle) ayrıca kurabilir — ancak bulut veri akışı yukarıdaki gerekçelerle önerilmiyor.

## Sınır (plan Bölüm 4.1 ile aynı)
MCP köprüsü `-batchmode`/CI'da kullanılmaz. Deterministik testler ve build MCP'siz koşmak
zorundadır ve koşuyor — EditMode testleri (6/6) MCP kurulmadan önce batchmode'da geçti.
MCP laboratuvardır; üretim bandı scriptte yaşar.
