# ADR 0007 — DEM → Unity Terrain ve Dünya Koordinat Çerçevesi

**Tarih:** 2026-08-18
**Durum:** Kabul edildi
**Karar veren:** Claude (Caner projeyi tamamen devretti)
**İlgili:** plan Görev 9, Faz 1 madde 1 ve 4; ADR 0005 (varlık hattı)

## Bağlam

Arazi yalnızca bir görsel katman değil, **şehrin tüm koordinat sisteminin taşıyıcısıdır**.
Ölçek ya da orijin yanlışsa her landmark, her sokak ve her uçuş mesafesi yanlış olur —
ve hata ancak binlerce varlık yerleştirildikten sonra ortaya çıkar. Gözle bakınca yarım
hücre kaymış bir İstanbul da İstanbul'a benzer; bu yüzden kabul **ölçümle** verilir.

## Karar

### Kaynak: Copernicus DEM GLO-30

Plan zaten bunu öneriyordu. Seçimi doğrulayan üç sebep:

1. **Kimlik doğrulaması yok.** AWS Open Data üzerinden anonim erişim. SRTM (LP DAAC) ve
   ASTER GDEM, NASA Earthdata hesabı ister — otomatik boru hattında insan müdahalesi
   demektir ve rolü bozar.
2. **COG (Cloud-Optimized GeoTIFF).** GDAL yalnızca gereken pencereyi HTTP range
   isteğiyle çeker: 4 karo × ~20 MB yerine birkaç yüz KB iner. Tüm indirme + projeksiyon
   **1,6 saniye** sürüyor.
3. **30 m çözünürlük** tepe siluetleri için yeter (planın kendi değerlendirmesi).

**Atıf zorunludur** — metin `refs/LICENSES.md`de, yükümlülük üç yerde kayıtlı.

### Projeksiyon: UTM 35N (EPSG:32635)

bbox tamamen 24°–30° D aralığında; tek zon yeter, dilim sınırı sorunu yok. Metrik ve
eksen hizalı olduğu için "1 birim = 1 metre" sözleşmesi doğrudan karşılanır.

### Dünya orijini: Galata Kulesi tabanı

Plan Faz 1 madde 4'ün kuralı. `28.974017 D, 41.025637 K` sabiti **tek yerde** yaşar
(`dem_fetch.py`); `dem_probe.py` ve Unity aynı değeri meta dosyasından okur.

> İlk denemede iki dosyada iki farklı yuvarlama kullanılmıştı (`28.9744` vs `28.974017`)
> ve Galata `(+32, −3)` çıktı. Sahte bir kayma izlenimi — 0,000383° tam olarak 32 m eder.
> Sabit tekilleştirildi.

Sonuç: arazinin güneybatı köşesi Unity'de `(−6306, 0, −8260)`.

### Y ekseni: 0 = deniz seviyesi

Taban en düşük araziye kaydırılmaz. Uçuş oyununda irtifa okuması deniz seviyesine
göredir; HUD'daki her irtifa buna dayanır. Deniz altı değerler (Copernicus'ta −13,4 m'ye
kadar gürültü var) 0'a kırpılır.

### Alan kareye tamamlanır

Planın bbox'ı 15,1 × 8,9 km, yani dikdörtgen. Unity heightmap'i **karedir**; dikdörtgen
alanı kare ızgaraya sığdırmak X ve Z'de farklı metre/örnek üretir — detay bir yönde
ezilir ve düzeltmesi sonradan çok pahalıdır. Kısa eksen simetrik büyütülür:
**15 338 × 15 338 m, 2049², 7,49 m/örnek.**

> **Yaşanan hata:** kareye tamamlama hedef ızgarada yapıldı ama mozaik hâlâ ham bbox
> için indirildi; büyütülen kuzey/güney şeritleri veri bulamadı. Örneklerin **%42,88'i**
> boş çıktı — oran, kareye tamamlamanın eklediği alanla birebir örtüşüyordu ve hatayı
> tam olarak bu örtüşme ele verdi. Artık indirme bbox'ı hedef alandan türetiliyor.

### Heightmap biçimi

`uint16 little-endian, row-major, satır 0 = güney, sütun 0 = batı`.

Unity'nin `TerrainData.SetHeights` çağrısında y ekseni +Z (kuzey) yönündedir; dosyayı bu
düzende yazmak Unity tarafında ters çevirme gerektirmez ve **"arazi aynalanmış" hatasını
baştan imkânsız kılar**. Unity'nin "Import Raw" penceresi yerine kendi okuyucumuzu
yazmamızın sebebi de bu: bayt düzeni, satır sırası ve normalizasyon kodda açıkça yazılı,
her seferinde doğru seçilmesine güvenilen üç ayar değil.

## Ölçüm sonuçları

`tools/gis/dem_probe.py` (Python, kaynak veriden) ve Unity `Terrain.SampleHeight`
(içe alınmış araziden) **birebir aynı** değerleri veriyor — boru hattının uçtan uca
sadık olduğunun kanıtı:

| Nokta | Unity dünya (X, Z) | İrtifa | Not |
|---|---|---|---|
| Galata Kulesi | `(0, 0)` | 52,0 m | dünya orijini |
| Ayasofya | `(+563, −1880)` | 48,0 m | 1. tepe |
| Süleymaniye | `(−828, −1034)` | 59,2 m | 3. tepe |
| Üsküdar Doğancılar | `(+3709, −41)` | 15,3 m | Hezarfen'in iniş noktası |
| Büyük Çamlıca | `(+8066, +361)` | 252,8 m | bölgenin en yükseği |
| Boğaz ortası | `(+4249, +1693)` | **0,0 m** | deniz |
| Haliç ortası | `(−1189, +458)` | **0,0 m** | deniz |

**Efsaneyle örtüşme:** Galata Kulesi'nden Doğancılar'a yatay mesafe **≈3709 m**;
`GameUnits.LegendaryGlideDistanceMeters` 3358 m. Aynı mertebede — kesin sayı, Doğancılar'ın
tam koordinatı Görev 10'da GeoJSON'a girince netleşecek.

**Kule yüksekliği tutarlılığı:** Galata sırtı 52 m (DSM, bina payı dahil; çıplak zemin
~35 m) + kule ~67 m ≈ **100 m** — Faz 0 graybox'ındaki 100 m'lik kule tesadüfen değil,
doğru bir tahminmiş.

## Dürüstlük notu: bu bir DSM'dir

Copernicus GLO-30 **yüzey modelidir**, çıplak zemin (DTM) değil. Modern binalar ve ağaçlar
irtifaya karışır. Ölçüm bunu açıkça gösteriyor: Galata ve Ayasofya beklenen zemin
irtifasının **~15 m üstünde** çıkıyor — aradaki fark, üzerlerindeki yapı.

1632 şehri için bu bir tarihsel kusurdur. Azaltma: **97 m'lik medyan filtresi**
(`--smooth 90`), bina ölçeğindeki sivrilikleri siler, tepe ölçeğindeki formu korur.
Bedeli ölçüldü: Çamlıca 268 → 253 m (zirve 12 m törpülendi).

Bu **tam bir DTM değildir ve öyle sunulmamalıdır.** Arazi bu yüzden `HistoricalTag`
üzerinde **T2 (Reconstruction)** etiketlidir — veri belgelidir ama modern topografyadır.
1632 kıyı çizgisi düzeltmesi (dolguların geri alınması, Langa'nın bostan yapılması)
Görev 10'un işidir; o düzeltme geldiğinde etiket notu güncellenir.

## Depoya ne girer

| Şey | Depoda? | Gerekçe |
|---|---|---|
| `tools/gis/*.py`, `requirements.txt` | ✅ | Üretim bandı scriptte yaşar |
| `data/gis/istanbul/` (heightmap, önizleme) | ❌ gitignore | Türetilmiş; sürümler pinli, birebir yeniden üretilir |
| `Assets/.../TD_Istanbul.asset` | ✅ | Kalıcı Unity varlığı |
| `Assets/.../Faz1_Terrain.unity` | ✅ | Çalışır demo (DoD) |

`Faz1_Terrain` **ayrı bir sahnedir**; `FlightSlice` kasten ellenmedi — Faz 0'ın öznel
kapısı hâlâ açık ve o sahne değerlendirilecek durumda kalmalı.

## Öğrenilen: Unity arazi yüksekliği 15-bit'tir

`TerrainData` yükseklikleri içerde **0..32766** aralığında saklar, 16-bit değil.
Yuvarlama adımı `1/32766 ≈ 3,05e-5`. Kaynak dosyayla karşılaştıran test önce `1e-5`
toleransıyla yazılmıştı ve haklı olarak kırıldı — o tolerans ölçüm değil temenniydi.
Bu arazide pratik anlamı: **291 m / 32766 ≈ 9 mm dikey çözünürlük.**

## Komutlar

```powershell
# Kurulum (bir kez)
uv venv tools/gis/.venv --python 3.13
uv pip install --python tools/gis/.venv -r tools/gis/requirements.txt

# DEM indir + isle
tools\gis\.venv\Scripts\python.exe tools\gis\dem_fetch.py --out data\gis\istanbul

# Georeferans denetimi (regresyon testi)
tools\gis\.venv\Scripts\python.exe tools\gis\dem_probe.py --dir data\gis\istanbul
```

Ardından Unity'de: **Hezarfen → GIS → DEM'den Terrain uret**, sonra
**Hezarfen → GIS → Terrain'i sahneye yerlestir**.

## Kanıt

`TerrainPipelineTests` — 8 test yeşil (EditMode toplamı **55/55**). Testlerin çoğu DEM'in
*içeriğinden* bağımsızdır: kaynak `.r16` ile Unity'deki arazi karşılaştırılır, böylece DEM
yeniden üretildiğinde testler kırılmaz ama bozuk bir import yine yakalanır.
